using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Border.Core;
using Border.Research;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class RocketSimulationTests
    {
        private const float BaselineOutput = 1200f; // 발열 60 °C/s, 연소 20 kg/s

        private readonly List<Object> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);

            _spawned.Clear();
        }

        [Test]
        public void Tick_ConsumesFuel_ThenRefusesWhenDry()
        {
            // 냉각을 크게 잡아 이 테스트에서는 열이 개입하지 않게 한다.
            RocketPart part = CreateEngine(Stats(fuel: 10f, cooling: 1000f, output: BaselineOutput, ignition: 100f));
            part.Prepare(new DeterministicRng()); // EditMode 에서는 Awake 가 돌지 않는다

            Assert.IsTrue(part.Tick(0.25f), "연료가 남아 있으면 태울 수 있어야 한다.");
            Assert.AreEqual(5f, part.Remaining, 1e-4f, "0.25초 × 20/초 = 5 만큼 줄어야 한다.");

            Assert.IsTrue(part.Tick(0.25f), "마지막 연료를 태우는 프레임도 추력을 내야 한다.");
            Assert.AreEqual(0f, part.Remaining, 1e-4f, "연료는 음수로 내려가지 않는다.");
            Assert.IsFalse(part.HasFuel);

            Assert.IsFalse(part.Tick(0.25f), "연료가 0이면 추력이 없어야 한다 — 여기서부터 낙하한다.");
        }

        [Test]
        public void Output_ScalesWithThrottle_NotJustPresetMaximum()
        {
            RocketPart part = CreateEngine(Stats(fuel: 100f, cooling: 10f, output: BaselineOutput, ignition: 100f));

            Assert.AreEqual(BaselineOutput, part.Output, 1e-3f);

            SetField(part, "throttle", 0.5f);
            Assert.AreEqual(600f, part.Output, 1e-3f, "실제 출력은 프리셋 최대치 × 스로틀이다.");
        }

        [Test]
        public void Temperature_RisesByHeatMinusCooling_ThenOverheats()
        {
            // 발열 60, 냉각 10 → 순증 50 °C/s. 300 °C 임계까지 6초.
            RocketPart part = CreateEngine(Stats(fuel: 200f, cooling: 10f, output: BaselineOutput, ignition: 100f));
            part.Prepare(new DeterministicRng());

            part.Tick(1f);
            Assert.AreEqual(50f, part.Temperature, 1e-3f, "초당 발열 − 냉각 만큼 쌓여야 한다.");
            Assert.IsFalse(part.Overheated);

            for (int i = 0; i < 5; i++) part.Tick(1f);

            Assert.GreaterOrEqual(part.Temperature, EngineStatsSO.CriticalTemperature);
            Assert.IsTrue(part.Overheated, "임계 온도를 넘으면 과열이어야 한다.");
        }

        [Test]
        public void Temperature_FallsWhenEngineIsOff_AndFloorsAtZero()
        {
            // 연료 40 이면 20/초로 2초만 탄다. 그 뒤에는 발열이 0 이라 냉각만 남는다.
            RocketPart part = CreateEngine(Stats(fuel: 40f, cooling: 10f, output: BaselineOutput, ignition: 100f));
            part.Prepare(new DeterministicRng());

            part.Tick(1f);
            part.Tick(1f);
            Assert.AreEqual(100f, part.Temperature, 1e-3f);
            Assert.IsFalse(part.HasFuel);

            Assert.IsFalse(part.Tick(1f), "연료가 없으면 추력이 없다.");
            Assert.AreEqual(90f, part.Temperature, 1e-3f, "꺼진 엔진은 발열이 0 이라 냉각만큼 식는다.");

            for (int i = 0; i < 20; i++) part.Tick(1f);
            Assert.AreEqual(0f, part.Temperature, 1e-4f, "온도는 0 아래로 내려가지 않는다.");
        }

        [Test]
        public void Prepare_IgnitesAtFullReliability_AndNeverAtZero()
        {
            RocketPart sure = CreateEngine(Stats(fuel: 100f, cooling: 60f, output: BaselineOutput, ignition: 100f));
            RocketPart dud = CreateEngine(Stats(fuel: 100f, cooling: 60f, output: BaselineOutput, ignition: 0f));

            var rng = new DeterministicRng();
            for (int seed = 1; seed <= 50; seed++)
            {
                rng.Reseed(seed);
                sure.Prepare(rng);
                Assert.IsTrue(sure.Ignited, $"100% 는 항상 점화되어야 한다 (seed {seed}).");

                rng.Reseed(seed);
                dud.Prepare(rng);
                Assert.IsFalse(dud.Ignited, $"0% 는 절대 점화되지 않아야 한다 (seed {seed}).");
                Assert.IsFalse(dud.Tick(0.25f), "점화하지 못한 엔진은 추력을 내지 않는다.");
            }
        }

        [Test]
        public void Attach_KeepsWorldPoint_AndLeavesPartRotation()
        {
            var rocketGo = Track(new GameObject("rocket"));
            rocketGo.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            var rocket = rocketGo.AddComponent<Rocket>();

            var partGo = Track(new GameObject("engine"));
            var partRotation = Quaternion.Euler(45f, 45f, 45f);
            partGo.transform.rotation = partRotation;
            partGo.AddComponent<BoxCollider>();
            var part = partGo.AddComponent<RocketPart>();

            var surfacePoint = new Vector3(0.5f, 1.2f, -0.3f); // 하단이 아닌 측면 지점
            rocket.Attach(part, surfacePoint);

            Assert.AreSame(rocketGo.transform, partGo.transform.parent);
            Assert.That(Vector3.Distance(surfacePoint, partGo.transform.position), Is.LessThan(1e-4f),
                "부착 지점은 레이캐스트가 맞은 표면 좌표 그대로여야 한다.");
            Assert.That(Quaternion.Angle(partRotation, partGo.transform.rotation), Is.LessThan(1e-3f),
                "Attach 는 부품 자세를 건드리지 않는다 — 자세는 붙이기 전에 정해져 있다.");
        }

        [Test]
        public void Launch_AddsTankMassPerEngine_OnTopOfBodyMass()
        {
            var rocketGo = Track(new GameObject("rocket"));
            var rocket = rocketGo.AddComponent<Rocket>(); // RequireComponent 가 Rigidbody 를 같이 붙인다
            var body = rocketGo.GetComponent<Rigidbody>();

            // EditMode 에서는 Awake 가 돌지 않는다 — Awake 가 하던 캐싱을 손으로 심는다.
            SetField(rocket, "_body", body);
            SetField(rocket, "_bodyMass", 100f);
            SetField(rocket, "tankMassPerFuel", 0.25f);

            for (int i = 0; i < 2; i++)
            {
                RocketPart engine = CreateEngine(Stats(fuel: 100f, cooling: 60f, output: BaselineOutput, ignition: 100f));
                engine.transform.SetParent(rocketGo.transform); // GetComponentsInChildren 가 찾도록
            }

            rocket.Launch();

            // 본체 100 + 연료 100 × 0.25 × 2기 = 150. 탱크 용량이 그대로 무게가 된다.
            Assert.AreEqual(150f, body.mass, 1e-3f, "엔진마다 연료 용량 × 계수 만큼 무거워져야 한다.");
        }

        [Test]
        public void Align_SnapsHeightAndAzimuthIndependently_OnlyWithinTolerance()
        {
            // 방위각 90°, 높이 1 에 엔진 하나가 붙어 있는 상태.
            var others = new List<Vector3> { new(0.5f, 1f, 0f) };

            // 반대편(270°) 과 같은 높이 근처 → 두 축 모두 스냅되고 반경은 표면 값을 유지한다.
            var near = new Vector3(-0.49f, 1.1f, 0.08f);
            RocketBuilder.Alignment mirrored = RocketBuilder.Align(near, others, 0.25f, 20f);

            Assert.IsTrue(mirrored.Height, "높이가 임계값 안이면 스냅되어야 한다.");
            Assert.IsTrue(mirrored.Azimuth, "반대편 방위각이 임계값 안이면 스냅되어야 한다.");
            Assert.AreEqual(1f, mirrored.Local.y, 1e-4f, "높이는 기존 엔진 높이와 정확히 같아야 한다.");
            Assert.AreEqual(-90f, Mathf.DeltaAngle(0f, Mathf.Atan2(mirrored.Local.x, mirrored.Local.z) * Mathf.Rad2Deg),
                1e-3f, "방위각은 기존 엔진의 정확히 반대편이어야 한다.");
            Assert.AreEqual(new Vector2(near.x, near.z).magnitude,
                new Vector2(mirrored.Local.x, mirrored.Local.z).magnitude, 1e-4f,
                "반경은 표면이 정하므로 보정이 건드리지 않는다.");

            // 두 축 모두 임계값 밖 → 좌표를 그대로 돌려준다.
            var far = new Vector3(0.35f, 2.5f, 0.35f); // 방위각 45°, 높이 2.5
            RocketBuilder.Alignment kept = RocketBuilder.Align(far, others, 0.25f, 20f);

            Assert.IsFalse(kept.Height);
            Assert.IsFalse(kept.Azimuth);
            Assert.AreEqual(far, kept.Local, "임계값 밖에서는 의도적 비대칭이 그대로 남아야 한다.");

            // 엔진이 3개 이상이어도 후보는 붙어 있는 모든 엔진에서 나온다 — 기존 부품은 움직이지 않는다.
            others.Add(new Vector3(0f, -1.5f, 0.5f)); // 방위각 0°, 높이 −1.5
            RocketBuilder.Alignment third = RocketBuilder.Align(new Vector3(0.06f, -1.45f, 0.49f), others, 0.25f, 20f);

            Assert.IsTrue(third.Height);
            Assert.IsTrue(third.Azimuth);
            Assert.AreEqual(-1.5f, third.Local.y, 1e-4f, "가장 가까운 높이 후보는 두 번째 엔진 것이다.");
            Assert.AreEqual(0f, Mathf.Atan2(third.Local.x, third.Local.z) * Mathf.Rad2Deg, 1e-3f,
                "가장 가까운 방위각 후보는 두 번째 엔진과 같은 줄이다.");
            Assert.AreEqual(new Vector3(0.5f, 1f, 0f), others[0], "기존 엔진 좌표는 보정으로 바뀌지 않는다.");
        }

        [Test]
        public void ClosestPointOnAxis_TracksTheRayAlongTheAxis_AndRefusesWhenParallel()
        {
            var origin = new Vector3(0f, 1f, 0f);

            // +X 축을 +Z 로 가로지르는 광선. 축과 직교하므로 t 는 광선의 x 오프셋 그대로다.
            Assert.IsTrue(RocketBuilder.ClosestPointOnAxis(origin, Vector3.right,
                new Ray(new Vector3(5f, 1f, -10f), Vector3.forward), out float t));
            Assert.AreEqual(5f, t, 1e-4f, "직교 광선이면 t 는 광선 원점의 축 방향 오프셋이다.");

            // 비스듬한 광선은 정답을 손으로 못 적는다 — 최소점이라는 성질로 확인한다.
            var oblique = new Ray(new Vector3(3f, 4f, -6f), new Vector3(0.3f, -0.4f, 1f));
            Assert.IsTrue(RocketBuilder.ClosestPointOnAxis(origin, Vector3.right, oblique, out float best));
            Assert.Less(Gap(origin + Vector3.right * best, oblique),
                Gap(origin + Vector3.right * (best + 0.01f), oblique), "t 보다 큰 쪽이 더 가까우면 안 된다.");
            Assert.Less(Gap(origin + Vector3.right * best, oblique),
                Gap(origin + Vector3.right * (best - 0.01f), oblique), "t 보다 작은 쪽이 더 가까우면 안 된다.");

            // 축을 정면으로 바라보면 어느 점이든 똑같이 가까워 t 가 발산한다 — 그 프레임은 버려야 한다.
            Assert.IsFalse(RocketBuilder.ClosestPointOnAxis(origin, Vector3.right,
                new Ray(new Vector3(-9f, 1f, 0f), Vector3.right), out _),
                "광선이 축과 평행하면 t 를 정할 수 없다.");
        }

        [Test]
        public void AngleOnPlane_MatchesAngleAxis_AndRefusesEdgeOnRays()
        {
            var origin = new Vector3(0f, 1f, 0f);

            // xz 평면(법선 +Y), 기준 +X. (0, 1, -2) 로 내리꽂으면 기준에서 90° 떨어진 지점이다.
            Assert.IsTrue(RocketBuilder.AngleOnPlane(origin, Vector3.up, Vector3.right,
                new Ray(new Vector3(0f, 6f, -2f), Vector3.down), out float degrees));
            Assert.AreEqual(90f, degrees, 1e-3f);

            // 부호 규약 잠금: 같은 각도를 AngleAxis 로 다시 만들면 실제 교점이 나와야 한다.
            // 여기서 외적 순서가 뒤집히면 링이 커서를 따라오는 대신 반대로 도망간다.
            Vector3 reconstructed = origin + Quaternion.AngleAxis(degrees, Vector3.up) * Vector3.right * 2f;
            Assert.That(Vector3.Distance(new Vector3(0f, 1f, -2f), reconstructed), Is.LessThan(1e-3f),
                "AngleOnPlane 의 부호는 Quaternion.AngleAxis 와 같은 방향이어야 한다.");

            Assert.IsFalse(RocketBuilder.AngleOnPlane(origin, Vector3.up, Vector3.right,
                new Ray(new Vector3(5f, 1f, 0f), Vector3.left), out _), "링을 옆에서 보면 교점이 없다.");
            Assert.IsFalse(RocketBuilder.AngleOnPlane(origin, Vector3.up, Vector3.right,
                new Ray(new Vector3(0f, -6f, -2f), Vector3.down), out _), "교점이 카메라 뒤면 버린다.");
            Assert.IsFalse(RocketBuilder.AngleOnPlane(origin, Vector3.up, Vector3.right,
                new Ray(new Vector3(0f, 6f, 0f), Vector3.down), out _), "중심을 정조준하면 각도가 정의되지 않는다.");
        }

        [Test]
        public void SnapAngle_PullsToMultiplesOfStep_AndPassesThroughOutsideTolerance()
        {
            const float Step = 45f;
            const float Tolerance = 7f;

            // 허용치 안: 가장 가까운 45° 배수로 끌려간다. 부호와 0/180 경계도 같이 잠근다.
            Assert.AreEqual(45f, RocketBuilder.SnapAngle(43f, Step, Tolerance), 1e-4f);
            Assert.AreEqual(45f, RocketBuilder.SnapAngle(47f, Step, Tolerance), 1e-4f);
            Assert.AreEqual(0f, RocketBuilder.SnapAngle(2f, Step, Tolerance), 1e-4f);
            Assert.AreEqual(180f, RocketBuilder.SnapAngle(176f, Step, Tolerance), 1e-4f);
            Assert.AreEqual(-45f, RocketBuilder.SnapAngle(-44f, Step, Tolerance), 1e-4f);

            // 허용치 밖에서는 아무것도 보정하지 않는다 — 정렬 가이드와 같은 규약이다.
            Assert.AreEqual(36f, RocketBuilder.SnapAngle(36f, Step, Tolerance), 1e-4f);
            Assert.AreEqual(22.5f, RocketBuilder.SnapAngle(22.5f, Step, Tolerance), 1e-4f);

            // 경계는 포함이다(<=). 여러 바퀴 돌려도 배수는 유지된다.
            Assert.AreEqual(90f, RocketBuilder.SnapAngle(83f, Step, Tolerance), 1e-4f);
            Assert.AreEqual(405f, RocketBuilder.SnapAngle(404f, Step, Tolerance), 1e-4f);

            // 실제 쓰임: 보정된 각도를 잡을 때 자세에 얹으면 정확히 45° 배수 자세가 나온다.
            Quaternion grabbed = Quaternion.Euler(10f, 20f, 30f);
            Quaternion snapped = Quaternion.AngleAxis(
                RocketBuilder.SnapAngle(88f, Step, Tolerance), Vector3.up) * grabbed;
            Assert.That(Quaternion.Angle(Quaternion.AngleAxis(90f, Vector3.up) * grabbed, snapped),
                Is.LessThan(1e-3f), "스냅된 각도는 잡을 때 자세 기준 절대각이어야 한다.");
        }

        [Test]
        public void DistanceToSegment_ClampsToTheEndpoints_AndSurvivesDegenerateSegments()
        {
            var a = new Vector2(100f, 100f);
            var b = new Vector2(200f, 100f);

            Assert.AreEqual(30f, RocketBuilder.DistanceToSegment(new Vector2(150f, 130f), a, b), 1e-3f,
                "선분 안쪽에서는 수선의 발까지의 거리다.");

            // 끝점 바깥이 그 끝점으로 클램프되는 것이 회전 링 집기의 근거다 — 32분할 폴리라인을
            // 이어 붙일 때 클램프가 없으면 이음매마다 거리가 튀어 링에 구멍이 생긴다.
            Assert.AreEqual(50f, RocketBuilder.DistanceToSegment(new Vector2(50f, 100f), a, b), 1e-3f,
                "시작점 바깥은 시작점까지의 거리로 클램프된다.");
            Assert.AreEqual(50f, RocketBuilder.DistanceToSegment(new Vector2(250f, 100f), a, b), 1e-3f,
                "끝점 바깥은 끝점까지의 거리로 클램프된다.");

            // 링을 정확히 옆에서 보면 이웃한 두 점이 한 픽셀로 뭉친다.
            Assert.AreEqual(50f, RocketBuilder.DistanceToSegment(new Vector2(150f, 100f), a, a), 1e-3f,
                "길이 0 인 선분은 그 점까지의 거리다.");
        }

        [Test]
        public void SupportRadius_PushesPartOffTheSurface_AndFollowsItsRotation()
        {
            // RocketEngine.prefab 의 BoxCollider size (0.547, 1, 0.541) 의 절반.
            var half = new Vector3(0.2735f, 0.5f, 0.2705f);

            Assert.AreEqual(0.2735f,
                RocketBuilder.SupportRadius(half, Quaternion.identity, Vector3.right), 1e-4f,
                "세워 둔 엔진은 반폭만큼만 밀린다 — 이만큼 밀어야 절반이 파묻히지 않는다.");

            Assert.AreEqual(0.5f,
                RocketBuilder.SupportRadius(half, Quaternion.Euler(0f, 0f, 90f), Vector3.right), 1e-4f,
                "눕히면 긴 축이 바깥을 향하므로 절반 길이만큼 밀어야 한다.");
        }

        [Test]
        public void ProjectOntoCapsule_LandsOnTheSurface_FromInsideOutsideAndTheCaps()
        {
            // SimulationTest 씬의 본체 값: 반지름 0.5, 축 선분 절반 1.5 (전체 높이 4).
            const float HalfSegment = 1.5f;
            const float Radius = 0.5f;

            Vector3 outside = RocketBuilder.ProjectOntoCapsule(
                new Vector3(3f, 0.7f, 0f), HalfSegment, Radius, Vector3.right);
            Assert.AreEqual(new Vector3(0.5f, 0.7f, 0f), outside, "원통 구간에서는 높이·방위각이 그대로다.");

            // Collider.ClosestPoint 가 틀리는 경우 — 내부 점은 입력을 그대로 돌려줘서 부품이 파묻힌다.
            Vector3 inside = RocketBuilder.ProjectOntoCapsule(
                new Vector3(0.05f, -1f, 0f), HalfSegment, Radius, Vector3.right);
            Assert.AreEqual(new Vector3(0.5f, -1f, 0f), inside, "본체 안쪽 점도 표면으로 밀려나야 한다.");

            Vector3 cap = RocketBuilder.ProjectOntoCapsule(
                new Vector3(0f, 3f, 0f), HalfSegment, Radius, Vector3.right);
            Assert.AreEqual(new Vector3(0f, 2f, 0f), cap, "축 위 캡 바깥은 캡 꼭대기로 내려온다.");

            // 축 위에서는 방위각이 없다 — 잡기 시작한 방향의 수평 성분만 쓴다(높이 성분은 버린다).
            Vector3 onAxis = RocketBuilder.ProjectOntoCapsule(
                new Vector3(0f, 0.5f, 0f), HalfSegment, Radius, new Vector3(0f, 9f, -2f));
            Assert.AreEqual(new Vector3(0f, 0.5f, -0.5f), onAxis, "축 위에서는 fallback 의 수평 방향을 쓴다.");
        }

        [Test]
        public void MovedPart_SnapsThenProjects_SoItNeverLeavesTheBodySurface()
        {
            const float HalfSegment = 1.5f;
            const float Radius = 0.5f;

            // 기즈모 이동은 정렬 스냅을 먼저 걸고 표면 투영을 마지막에 한다. 순서를 뒤집으면
            // 스냅이 마지막 말을 하게 되어 캡 구간에서 부품이 표면 밖으로 떠버린다.
            var others = new List<Vector3> { new(0.5f, 1f, 0f) };

            RocketBuilder.Alignment onBody = RocketBuilder.Align(new Vector3(2f, 1.05f, 0.1f), others, 0.25f, 20f);
            Vector3 cylinder = RocketBuilder.ProjectOntoCapsule(onBody.Local, HalfSegment, Radius, Vector3.right);
            Assert.That(Vector3.Distance(new Vector3(0.5f, 1f, 0f), cylinder), Is.LessThan(1e-4f),
                "원통 구간에서는 투영이 스냅된 높이·방위각을 그대로 살려 둔다.");

            // 캡 구간: 스냅이 높이를 1.95 로 올려도 반경 2 인 점은 표면 밖이라 투영이 끌어내린다.
            var high = new List<Vector3> { new(0.4f, 1.95f, 0f) };
            RocketBuilder.Alignment onCap = RocketBuilder.Align(new Vector3(2f, 1.9f, 0f), high, 0.25f, 20f);
            Vector3 capped = RocketBuilder.ProjectOntoCapsule(onCap.Local, HalfSegment, Radius, Vector3.right);

            Assert.AreEqual(Radius, AxisGap(capped, HalfSegment), 1e-4f, "결과는 언제나 정확히 표면 위다.");
            Assert.Less(capped.y, onCap.Local.y, "캡에서는 투영이 스냅된 높이를 끌어내리는 게 맞다.");
        }

        /// <summary>광선과 점 사이의 수직 거리. 최근접점 테스트에서 최소성만 확인하는 용도.</summary>
        private static float Gap(Vector3 point, Ray ray) =>
            Vector3.Cross(ray.direction.normalized, point - ray.origin).magnitude;

        /// <summary>캡슐 축 선분까지의 거리. 표면 위라면 정확히 반지름이 나온다.</summary>
        private static float AxisGap(Vector3 local, float halfSegment) =>
            Vector3.Distance(local, new Vector3(0f, Mathf.Clamp(local.y, -halfSegment, halfSegment), 0f));

        [Test]
        public void PullbackDistance_GrowsWithAltitude_ThenClampsAtBothEnds()
        {
            const float Near = 40f;
            const float Growth = 3f;
            const float Far = 500f;

            // 발사 순간, 그리고 발사 고도 아래로 떨어진 뒤에도 하한보다 붙지 않는다.
            Assert.AreEqual(Near, RocketBuilder.PullbackDistance(0f, Near, Growth, Far), 1e-4f);
            Assert.AreEqual(Near, RocketBuilder.PullbackDistance(-80f, Near, Growth, Far), 1e-4f);

            // 본 구간: 고도 1 유닛마다 Growth 만큼 물러난다.
            Assert.AreEqual(Near + 100f * Growth,
                RocketBuilder.PullbackDistance(100f, Near, Growth, Far), 1e-3f);
            Assert.Greater(RocketBuilder.PullbackDistance(120f, Near, Growth, Far),
                RocketBuilder.PullbackDistance(100f, Near, Growth, Far),
                "거리는 고도에 대해 단조 증가해야 한다.");

            // 상한. far clip 1000 안이라 로켓이 클립 밖으로 나가지 않는다.
            Assert.AreEqual(Far, RocketBuilder.PullbackDistance(10000f, Near, Growth, Far), 1e-4f);
            Assert.Less(Far, 1000f);
        }

        [Test]
        public void TrailDotWorldSize_HoldsTheSameScreenFraction_AtEveryDistance()
        {
            const float Fraction = 0.015f;
            const float Fov = 60f;

            // 거리에 정비례해야 화면에서 같은 크기로 남는다. 월드 크기를 고정하면 멀어질수록 사라진다.
            float near = RocketBuilder.TrailDotWorldSize(Fraction, 40f, Fov);
            float far = RocketBuilder.TrailDotWorldSize(Fraction, 400f, Fov);
            Assert.AreEqual(near * 10f, far, 1e-3f, "거리가 10배면 월드 크기도 10배여야 한다.");

            // 계약 자체: 그 거리에서 화면이 담는 세로 길이 대비 비율이 항상 Fraction 이다.
            foreach (float distance in new[] { 40f, 150f, 500f })
            {
                float size = RocketBuilder.TrailDotWorldSize(Fraction, distance, Fov);
                float screenHeight = 2f * distance * Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
                Assert.AreEqual(Fraction, size / screenHeight, 1e-5f, $"거리 {distance}");
            }

            // 화각이 넓어지면 같은 비율을 채우는 데 더 큰 점이 필요하다 — FOV 를 상수로 박으면 안 된다.
            Assert.Greater(RocketBuilder.TrailDotWorldSize(Fraction, 150f, 90f),
                RocketBuilder.TrailDotWorldSize(Fraction, 150f, 60f));
        }

        [Test]
        public void TrailCullingMask_ShowsTheTrailToExactlyOneView_AndFollowsTheSwap()
        {
            const int Layer = 8;                 // Trajectory
            int bit = 1 << Layer;
            const int Everything = ~0;

            // 켜고 끄는 것은 그 비트 하나뿐 — 다른 레이어를 같이 잘라내면 3D 가 통째로 사라진다.
            Assert.AreEqual(Everything, RocketBuilder.TrailCullingMask(Everything, Layer, true));
            Assert.AreEqual(Everything & ~bit, RocketBuilder.TrailCullingMask(Everything, Layer, false));
            Assert.AreEqual(bit, RocketBuilder.TrailCullingMask(0, Layer, true));
            Assert.AreEqual(0, RocketBuilder.TrailCullingMask(0, Layer, false));

            // 실제 쓰임: 두 카메라가 반대 인자를 받으므로 스왑 양쪽에서 정확히 하나만 궤적을 본다.
            // bool 이 뒤집히면 둘 다 보이거나 둘 다 안 보인다.
            foreach (bool swapped in new[] { false, true })
            {
                int main = RocketBuilder.TrailCullingMask(Everything, Layer, swapped);
                int pip = RocketBuilder.TrailCullingMask(Everything, Layer, !swapped);

                Assert.AreNotEqual((main & bit) != 0, (pip & bit) != 0,
                    $"swapped={swapped}: 후퇴 뷰를 맡은 카메라 하나만 궤적을 봐야 한다.");
            }
        }

        [Test]
        public void Flame_TurnsOnWhileBurning_AndOffWhenDry()
        {
            RocketPart part = CreateEngine(Stats(fuel: 10f, cooling: 1000f, output: BaselineOutput, ignition: 100f));

            var flameGo = Track(new GameObject("flame"));
            flameGo.transform.SetParent(part.transform);
            var flame = flameGo.AddComponent<ParticleSystem>();
            ParticleSystem.EmissionModule emission = flame.emission; // 구조체 프로퍼티라 지역 변수로 받아 쓴다
            flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SetField(part, "flame", flame);
            part.Prepare(new DeterministicRng());

            Assert.IsFalse(flame.isEmitting, "발사 전에는 불꽃이 꺼져 있어야 한다.");

            part.Tick(0.25f);
            Assert.IsTrue(flame.isEmitting, "연료를 태우는 동안에는 불꽃이 켜져 있어야 한다.");
            Assert.IsTrue(emission.enabled, "emission 모듈 자체는 켜진 상태여야 한다.");

            part.Tick(0.25f); // 여기서 연료가 0 이 된다
            part.Tick(0.25f); // 소진 후 첫 호출에서 꺼진다
            Assert.IsFalse(part.HasFuel);
            Assert.IsFalse(flame.isEmitting, "연료가 떨어지면 불꽃이 꺼져야 한다.");
        }

        [Test]
        public void PresetLibrary_CapsSlotsAtTen()
        {
            var library = Track(ScriptableObject.CreateInstance<EnginePresetLibrarySO>());

            var slots = new List<EngineStatsSO>();
            for (int i = 0; i < EnginePresetLibrarySO.MaxSlots + 3; i++)
                slots.Add(Stats(fuel: 100f, cooling: 60f, output: BaselineOutput, ignition: 100f));

            SetField(library, "slots", slots);
            Invoke(library, "OnValidate");

            Assert.AreEqual(EnginePresetLibrarySO.MaxSlots, library.Slots.Count, "슬롯은 10개를 넘을 수 없다.");
        }

        [Test]
        public void RuntimeBridge_BuildRuntimeLibrary_MapsResearchBySlotIndex()
        {
            var model = new ResearchPrototypeModel();
            EngineStatsSO base0 = Stats(fuel: 100f, cooling: 60f, output: 1200f, ignition: 100f);
            EngineStatsSO base1 = Stats(fuel: 200f, cooling: 80f, output: 1400f, ignition: 50f);
            var library = Track(ScriptableObject.CreateInstance<EnginePresetLibrarySO>());
            SetField(library, "slots", new List<EngineStatsSO> { base0, base1 });

            model.ExecuteEngineResearch(EnginePresetId.Engine02, EngineStatId.MaxOutput, focused: true, score: 85);

            EnginePresetLibrarySO runtimeLibrary = TrackRuntimeLibrary(
                ResearchEnginePresetRuntimeBridge.BuildRuntimeLibrary(model, library));

            Assert.AreEqual(EnginePresetLibrarySO.MaxSlots, runtimeLibrary.Slots.Count);
            Assert.AreEqual(0, runtimeLibrary.Slots[0].PresetIndex);
            Assert.AreEqual(1, runtimeLibrary.Slots[1].PresetIndex);
            Assert.AreEqual(base0.MaxOutput, runtimeLibrary.Slots[0].MaxOutput, 0.001f);

            EnginePresetState researched = model.GetEnginePreset(EnginePresetId.Engine02);
            float expectedOutput = base1.MaxOutput * researched.MaxOutput / ResearchPrototypeModel.InitialEngineStat;
            Assert.AreEqual(expectedOutput, runtimeLibrary.Slots[1].MaxOutput, 0.001f);
            Assert.AreEqual(ResearchPrototypeModel.EngineInstallCost, runtimeLibrary.Slots[1].Price);
            Assert.AreEqual(1400f, base1.MaxOutput, 0.001f, "원본 SO 값은 연구 반영으로 바뀌면 안 된다.");
        }

        [Test]
        public void RuntimeBridge_BuildRuntimeLibrary_FillsMissingSlotsWithDefaults()
        {
            var model = new ResearchPrototypeModel();

            EnginePresetLibrarySO runtimeLibrary = TrackRuntimeLibrary(
                ResearchEnginePresetRuntimeBridge.BuildRuntimeLibrary(model, null));

            Assert.AreEqual(EnginePresetLibrarySO.MaxSlots, runtimeLibrary.Slots.Count);
            Assert.AreEqual(5, runtimeLibrary.Slots[5].PresetIndex);
            Assert.AreEqual(ResearchPrototypeModel.EngineInstallCost, runtimeLibrary.Slots[5].Price);
            Assert.AreEqual(100f, runtimeLibrary.Slots[5].FuelCapacity, 0.001f);
            Assert.AreEqual(60f, runtimeLibrary.Slots[5].Cooling, 0.001f);
            Assert.AreEqual(BaselineOutput, runtimeLibrary.Slots[5].MaxOutput, 0.001f);
            Assert.AreEqual(100f, runtimeLibrary.Slots[5].IgnitionReliability, 0.001f);
        }

        [Test]
        public void RocketBuilder_SetPresetLibrary_AcceptsRuntimeLibrary()
        {
            var model = new ResearchPrototypeModel();
            EnginePresetLibrarySO runtimeLibrary = TrackRuntimeLibrary(
                ResearchEnginePresetRuntimeBridge.BuildRuntimeLibrary(model, null));
            RocketBuilder builder = Track(new GameObject("builder")).AddComponent<RocketBuilder>();

            builder.SetPresetLibrary(runtimeLibrary);

            Assert.AreSame(runtimeLibrary, builder.PresetLibrary);
        }

        [Test]
        public void PresetEntry_ImplementsIDragHandler_SoBeginDragActuallyFires()
        {
            // 입력 모듈은 pointerDrag 를 IDragHandler 로만 찾는다. IBeginDragHandler 만 달면 컴파일도
            // 되고 경고도 안 나는데 OnBeginDrag 가 영영 안 불린다 — 조용히 깨지는 종류라 잠가 둔다.
            var entry = typeof(RocketDesignUI).GetNestedType("PresetEntry", BindingFlags.NonPublic);
            Assert.IsNotNull(entry, "RocketDesignUI.PresetEntry 를 찾지 못했다.");
            Assert.IsTrue(typeof(IDragHandler).IsAssignableFrom(entry),
                "PresetEntry 가 IDragHandler 를 구현하지 않으면 프리셋 드래그가 시작되지 않는다.");
        }

        private EngineStatsSO Stats(float fuel, float cooling, float output, float ignition)
        {
            var stats = Track(ScriptableObject.CreateInstance<EngineStatsSO>());
            SetField(stats, "fuelCapacity", fuel);
            SetField(stats, "cooling", cooling);
            SetField(stats, "maxOutput", output);
            SetField(stats, "ignitionReliability", ignition);
            return stats;
        }

        [Test]
        public void OutlineAndHologram_ToggleShaderStateOnTheInstancedMaterial()
        {
            RocketPart part = CreateEngine(Stats(fuel: 10f, cooling: 10f, output: BaselineOutput, ignition: 100f));
            Material shared = Track(new Material(Shader.Find("Shader/Uber/3D Object")));
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = shared;

            // EditMode 에서는 renderer.materials 인스턴스화가 에러 로그를 남긴다. 플레이 중에는 정상 경로다.
            LogAssert.Expect(LogType.Error, new Regex("Instantiating material"));

            part.SetOutline(true);
            part.SetHologram(true);

            Material instance = part.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.AreNotSame(shared, instance, "공유 머티리얼을 건드리면 붙어 있는 엔진 전부가 같이 켜진다.");
            Assert.IsTrue(instance.IsKeywordEnabled("_STENCIL_OUTLINE_ON"));
            Assert.AreEqual(1f, instance.GetFloat("_StencilOutlineEnabled"), 1e-4f,
                "이 값은 포워드 패스의 스텐실 WriteMask 다 — 0 이면 아웃라인 패스가 아무것도 못 그린다.");
            Assert.IsTrue(instance.GetShaderPassEnabled("StencilOutline"),
                "머티리얼 에셋이 아웃라인 패스를 꺼둔 채로 들어오므로 패스도 같이 켜야 한다.");
            Assert.IsTrue(instance.IsKeywordEnabled("_HOLOGRAM_ON"));
            Assert.AreEqual(1f, instance.GetFloat("_HologramEnabled"), 1e-4f);

            part.SetOutline(false);
            part.SetHologram(false);

            Assert.IsFalse(instance.IsKeywordEnabled("_STENCIL_OUTLINE_ON"));
            Assert.AreEqual(0f, instance.GetFloat("_StencilOutlineEnabled"), 1e-4f);
            Assert.IsFalse(instance.GetShaderPassEnabled("StencilOutline"));
            Assert.IsFalse(instance.IsKeywordEnabled("_HOLOGRAM_ON"));
            Assert.AreEqual(0f, instance.GetFloat("_HologramEnabled"), 1e-4f);
        }

        private RocketPart CreateEngine(EngineStatsSO stats)
        {
            // RocketPart 의 RequireComponent(Collider) 는 추상 타입이라 자동 추가되지 않는다.
            var go = Track(new GameObject("engine"));
            go.AddComponent<BoxCollider>();

            var part = go.AddComponent<RocketPart>();
            SetField(part, "stats", stats);
            return part;
        }

        private EnginePresetLibrarySO TrackRuntimeLibrary(EnginePresetLibrarySO library)
        {
            Track(library);
            for (int i = 0; i < library.Slots.Count; i++)
                if (library.Slots[i] != null)
                    Track(library.Slots[i]);

            return library;
        }

        private T Track<T>(T target) where T : Object
        {
            _spawned.Add(target);
            return target;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);

        private static void Invoke(object target, string name) =>
            target.GetType()
                .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
    }
}
