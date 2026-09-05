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
            // 발열 120, 냉각 10: 초당 110도 상승.
            RocketPart part = CreateEngine(Stats(fuel: 200f, cooling: 10f, output: BaselineOutput, ignition: 100f));
            part.Prepare(new DeterministicRng());

            part.Tick(1f);
            Assert.AreEqual(110f, part.Temperature, 1e-3f, "초당 발열 − 냉각 만큼 쌓여야 한다.");
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
            Assert.AreEqual(220f, part.Temperature, 1e-3f);
            Assert.IsFalse(part.HasFuel);

            Assert.IsFalse(part.Tick(1f), "연료가 없으면 추력이 없다.");
            Assert.AreEqual(210f, part.Temperature, 1e-3f, "꺼진 엔진은 발열이 0 이라 냉각만큼 식는다.");

            for (int i = 0; i < 25; i++) part.Tick(1f);
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
        public void Hold_KeepsRocketClamped_AndRestartsRampOnLiftoff()
        {
            var rocketGo = Track(new GameObject("hold rocket"));
            var rocket = rocketGo.AddComponent<Rocket>();
            var body = rocketGo.GetComponent<Rigidbody>();
            Invoke(rocket, "Awake");
            SetField(rocket, "holdSeconds", 0.1f);

            RocketPart engine = CreateEngine(
                Stats(fuel: 100f, cooling: 60f, output: BaselineOutput, ignition: 100f));
            engine.transform.SetParent(rocketGo.transform);

            rocket.Launch();
            Assert.That(rocket.Holding, Is.True, "홀드가 있으면 발사는 클램프에서 시작한다.");
            Assert.That(rocket.Lifted, Is.False);

            Invoke(rocket, "FixedUpdate");

            // 홀드는 연출이다 — 배기와 흔들림만 오르고 시뮬레이션은 아직 아무것도 소비하지 않는다.
            Assert.That(body.isKinematic, Is.True, "홀드 중에는 발사대에 고정돼 있어야 한다.");
            Assert.That(rocket.TotalBurnSeconds, Is.Zero, "홀드는 연료를 태우지 않는다.");
            Assert.That(engine.Remaining, Is.EqualTo(100f).Within(1e-4f), "홀드 중 연료는 그대로다.");
            Assert.That(engine.Temperature, Is.Zero, "홀드는 발열도 없다.");
            Assert.That(rocket.ThrustFraction, Is.GreaterThan(0f), "화면에는 점화가 보여야 한다.");

            for (int i = 0; i < 20 && rocket.Holding; i++) Invoke(rocket, "FixedUpdate");

            Assert.That(rocket.Holding, Is.False, "홀드는 제한 시간 안에 끝나야 한다.");
            Assert.That(rocket.Lifted, Is.True);
            Assert.That(body.isKinematic, Is.False, "클램프가 풀리면 물리가 로켓을 넘겨받는다.");

            // 클램프가 풀린 첫 스텝은 램프가 0 부터 다시 오르므로 홀드 끝의 세기보다 훨씬 약하다.
            float atRelease = rocket.ThrustFraction;
            Invoke(rocket, "FixedUpdate");
            Assert.That(rocket.ThrustFraction, Is.LessThan(atRelease * 0.1f),
                "이륙 추력 램프는 클램프 해제 시점부터 다시 시작한다.");
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
        public void TryReachCapsule_ReachesPastTheSilhouette_SoTheCapsAreAttachable()
        {
            const float HalfSegment = 1.5f;
            const float Radius = 0.5f;
            const float Reach = 0.75f; // 반지름의 1.5 배 — 프리팹 기본값

            // 로켓 꼭대기 위 빈 곳을 가리킨 커서. 콜라이더에는 안 맞지만 부착으로 쳐야 하고,
            // 투영은 그 점을 위쪽 캡으로 끌어온다 — 이 경로가 없으면 마개가 몇 픽셀짜리 표적이었다.
            var above = new Ray(new Vector3(-10f, 2.4f, 0f), Vector3.right);
            Assert.IsTrue(RocketBuilder.TryReachCapsule(above, HalfSegment, Radius, Reach, out Vector3 top),
                "표면에서 reach 안쪽을 지나는 광선은 실루엣 밖이어도 부착이다.");
            Assert.AreEqual(2.4f, top.y, 1e-4f, "최근접점은 광선 위에 있다.");
            Vector3 seated = RocketBuilder.ProjectOntoCapsule(top, HalfSegment, Radius, Vector3.right);
            Assert.Greater(seated.y, HalfSegment, "캡 위에 얹힌다.");
            Assert.AreEqual(Radius, AxisGap(seated, HalfSegment), 1e-4f, "결과는 정확히 표면 위다.");

            // 바닥 아래도 같은 규칙이다. 카메라 피치가 -20° 에 묶여 있어 이쪽은 볼 수조차 없었다.
            var below = new Ray(new Vector3(-10f, -2.4f, 0f), Vector3.right);
            Assert.IsTrue(RocketBuilder.TryReachCapsule(below, HalfSegment, Radius, Reach, out Vector3 bottom));
            Assert.Less(RocketBuilder.ProjectOntoCapsule(bottom, HalfSegment, Radius, Vector3.right).y,
                -HalfSegment, "아래쪽 캡도 잡힌다.");

            // 여유 밖은 여전히 "놓으면 사라지는" 자리다 — 손을 뻗는 것이지 아무 데나 붙는 게 아니다.
            var far = new Ray(new Vector3(-10f, 0f, 3f), Vector3.right);
            Assert.IsFalse(RocketBuilder.TryReachCapsule(far, HalfSegment, Radius, Reach, out _),
                "reach 밖 광선은 부착이 아니다.");

            // 축과 평행한 광선은 최근접점이 발산한다. 그 각도에서는 콜라이더 레이캐스트가 캡에 맞는다.
            Assert.IsFalse(RocketBuilder.TryReachCapsule(
                new Ray(new Vector3(0f, 10f, 0f), Vector3.down), HalfSegment, Radius, Reach, out _));
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
        public void RampFactor_StartsAtZero_ReachesFull_AndIsOffWhenRampIsZero()
        {
            const float Ramp = 1.2f;

            Assert.AreEqual(0f, Rocket.RampFactor(0f, Ramp), 1e-5f, "점화 순간에는 추력이 없다.");
            Assert.AreEqual(1f, Rocket.RampFactor(Ramp, Ramp), 1e-5f);
            Assert.AreEqual(1f, Rocket.RampFactor(Ramp * 10f, Ramp), 1e-5f, "램프가 끝나면 계속 최대다.");
            Assert.AreEqual(0.5f, Rocket.RampFactor(Ramp * 0.5f, Ramp), 1e-5f, "SmoothStep 은 중간에서 0.5 다.");

            // 단조 증가여야 한다 — 중간에 꺼지면 발사대에서 주저앉는 것처럼 보인다.
            float previous = -1f;
            for (int i = 0; i <= 20; i++)
            {
                float value = Rocket.RampFactor(Ramp * i / 20f, Ramp);
                Assert.GreaterOrEqual(value, previous);
                previous = value;
            }

            // 0 이면 램프 자체가 없다 — 예전 동작(첫 프레임에 최대 추력)으로 돌아가는 손잡이다.
            Assert.AreEqual(1f, Rocket.RampFactor(0f, 0f), 1e-5f);
        }

        [Test]
        public void Tick_ScalesFuelAndHeatByThrust_SoTheRampDoesNotWasteFuelOnThePad()
        {
            // 냉각 0 이면 온도가 곧 누적 발열이라 배율이 그대로 보인다.
            RocketPart full = CreateEngine(Stats(fuel: 100f, cooling: 0f, output: BaselineOutput, ignition: 100f));
            RocketPart half = CreateEngine(Stats(fuel: 100f, cooling: 0f, output: BaselineOutput, ignition: 100f));
            full.Prepare(new DeterministicRng());
            half.Prepare(new DeterministicRng());

            Assert.AreEqual(BaselineOutput * 0.5f, half.OutputAt(0.5f), 1e-3f);
            Assert.AreEqual(BaselineOutput, half.OutputAt(1f), 1e-3f, "배율 1 은 램프 없음과 같아야 한다.");
            Assert.AreEqual(0f, half.OutputAt(-3f), 1e-3f, "배율은 0~1 로 잘린다.");

            Assert.IsTrue(full.Tick(1f));
            Assert.IsTrue(half.Tick(1f, 0.5f));

            Assert.AreEqual(20f, 100f - full.Remaining, 1e-3f);
            Assert.AreEqual(10f, 100f - half.Remaining, 1e-3f, "반만 내는 추력은 연료도 반만 태운다.");
            Assert.AreEqual(120f, full.Temperature, 1e-3f);
            Assert.AreEqual(60f, half.Temperature, 1e-3f, "발열도 같은 배율을 타야 램프가 열 이득이 된다.");
        }

        [Test]
        public void LaunchShake_StaysInsideAmplitude_AndStopsWhenThereIsNoThrust()
        {
            Assert.AreEqual(Vector3.zero, RocketBuilder.LaunchShake(0f, 3f, 14f),
                "추력이 0 이면(연료 소진·과열·발사 전) 흔들리지 않는다.");

            const float Amplitude = 0.25f;
            bool moved = false;
            for (int i = 0; i < 200; i++)
            {
                Vector3 shake = RocketBuilder.LaunchShake(Amplitude, i * 0.02f, 14f);

                Assert.LessOrEqual(Mathf.Abs(shake.x), Amplitude + 1e-5f);
                Assert.LessOrEqual(Mathf.Abs(shake.y), Amplitude + 1e-5f);
                Assert.AreEqual(0f, shake.z, 1e-6f, "흔들림은 뷰 로컬 X·Y 뿐이다 — 거리는 건드리지 않는다.");
                moved |= shake.sqrMagnitude > 1e-6f;
            }

            Assert.IsTrue(moved, "진폭이 있으면 실제로 흔들려야 한다.");

            // 두 축이 Perlin 의 같은 선에서 나오면 대각선으로만 움직여 진동으로 안 읽힌다.
            Vector3 sample = RocketBuilder.LaunchShake(Amplitude, 1.7f, 14f);
            Assert.Greater(Mathf.Abs(sample.x - sample.y), 1e-4f, "두 축은 서로 다른 값이어야 한다.");
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

        private ParticleSystem AddSmoke(RocketPart part, string field, bool loop)
        {
            var go = new GameObject(field);
            go.transform.SetParent(part.transform);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = loop;
            SetField(part, field, system);
            return system;
        }

        [Test]
        public void SmokeSingle_HoldAndBurn_StopOnFuelExhaustionWithoutRestart()
        {
            RocketPart part = CreateEngine(Stats(10f, 1000f, BaselineOutput, 100f));
            var single = AddSmoke(part, "smokeSingle", true);
            var fail = AddSmoke(part, "smokeFail", false);
            part.Prepare(new DeterministicRng());
            Assert.IsFalse(single.isEmitting);
            part.HoldExhaust(0.5f);
            Assert.IsTrue(single.isEmitting);
            Assert.AreEqual(10f, part.Remaining);
            part.Tick(0.25f);
            Assert.IsTrue(single.isEmitting);
            Assert.IsTrue(part.Tick(0.25f), "마지막 연료의 추력은 유지한다.");
            Assert.IsFalse(single.isEmitting, "연료 소진 프레임에 방출을 중지한다.");
            part.HoldExhaust(1f);
            part.Tick(0.25f);
            Assert.IsFalse(single.isEmitting);
            Assert.IsFalse(fail.isEmitting, "연료 소진은 시동 실패가 아니다.");
        }

        [Test]
        public void SmokeFail_PlaysOnceOnFailedPrepare_AndClearsOnSuccessfulRetry()
        {
            RocketPart part = CreateEngine(Stats(10f, 1000f, BaselineOutput, 0f));
            var single = AddSmoke(part, "smokeSingle", true);
            var fail = AddSmoke(part, "smokeFail", false);
            part.Prepare(new DeterministicRng());
            Assert.IsTrue(fail.isEmitting);
            Assert.IsFalse(single.isEmitting);
            fail.Simulate(12f, true, false, true);
            Assert.IsFalse(fail.isEmitting);
            Assert.AreEqual(0, fail.particleCount);
            part.HoldExhaust(1f);
            part.Tick(0.25f);
            Assert.IsFalse(fail.isEmitting, "Tick과 홀드는 실패 연출을 재시작하지 않는다.");
            Assert.IsFalse(single.isEmitting);
            part.Prepare(new DeterministicRng());
            Assert.IsTrue(fail.isEmitting, "새 발사의 실패는 다시 연출한다.");
            part.ApplyPreset(Stats(10f, 1000f, BaselineOutput, 100f));
            part.Prepare(new DeterministicRng());
            Assert.IsFalse(fail.IsAlive(true));
            part.Tick(0.1f);
            Assert.IsTrue(single.isEmitting);
        }

        [Test]
        public void Smoke_ShutdownStopsBothEffects_AndEmptyTankCannotStartSingle()
        {
            RocketPart part = CreateEngine(Stats(0f, 1000f, BaselineOutput, 100f));
            var single = AddSmoke(part, "smokeSingle", true);
            var fail = AddSmoke(part, "smokeFail", false);
            part.Prepare(new DeterministicRng());
            part.HoldExhaust(1f);
            Assert.IsFalse(single.isEmitting);
            Assert.IsFalse(fail.isEmitting);
            single.Play();
            fail.Play();
            part.Shutdown();
            Assert.IsFalse(single.isEmitting);
            Assert.IsFalse(fail.isEmitting);
        }

        [Test]
        public void Smoke_MeshResizeScalesBothEffects_WithoutAccumulatingOnRefit()
        {
            RocketPart part = CreateEngine(Stats(10f, 1000f, BaselineOutput, 100f));
            var box = part.GetComponent<BoxCollider>();
            box.size = Vector3.one;
            var single = AddSmoke(part, "smokeSingle", true);
            var fail = AddSmoke(part, "smokeFail", false);
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.transform.SetParent(part.transform);
            mesh.transform.localScale = new Vector3(2f, 4f, 2f);
            SetField(part, "meshRoot", mesh.transform);
            float initialSize = single.main.startSizeMultiplier;
            float initialFailRadius = fail.shape.radius;
            Invoke(part, "FitToMesh");
            Assert.AreEqual(initialSize * 2f, single.main.startSizeMultiplier, 0.001f);
            Assert.AreEqual(initialFailRadius * 2f, fail.shape.radius, 0.001f);
            Assert.AreEqual(new Vector3(0f, -2f, 0f), single.transform.localPosition);
            Assert.AreEqual(single.transform.localPosition, fail.transform.localPosition);
            Invoke(part, "FitToMesh");
            Assert.AreEqual(initialSize * 2f, single.main.startSizeMultiplier, 0.001f);
        }

        [Test]
        public void RocketEnginePrefab_BindsSmokeEffects_WithControlledPlayback()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Simulation/RocketEngine.prefab");
            var part = prefab.GetComponent<RocketPart>();
            var serialized = new UnityEditor.SerializedObject(part);
            var single = serialized.FindProperty("smokeSingle").objectReferenceValue as ParticleSystem;
            var fail = serialized.FindProperty("smokeFail").objectReferenceValue as ParticleSystem;
            Assert.IsNotNull(single);
            Assert.IsNotNull(fail);
            Assert.AreEqual("Smoke_Single", single.name);
            Assert.AreEqual("Smoke_Fail", fail.name);
            Assert.IsFalse(single.main.playOnAwake);
            Assert.IsFalse(fail.main.playOnAwake);
            Assert.IsTrue(single.main.loop);
            Assert.IsFalse(fail.main.loop);
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

            Assert.AreEqual(ResearchActionResult.Success, model.CreateNewEnginePreset(out EnginePresetId created));
            Assert.AreEqual(EnginePresetId.Engine02, created);
            int outputBefore = model.GetEnginePreset(created).MaxOutput;
            Assert.AreEqual(ResearchActionResult.Success,
                model.ExecuteEngineResearch(created, EngineStatId.MaxOutput, focused: true, score: 85));
            Assert.Greater(model.GetEnginePreset(created).MaxOutput, outputBefore);

            EnginePresetLibrarySO runtimeLibrary = TrackRuntimeLibrary(
                ResearchEnginePresetRuntimeBridge.BuildRuntimeLibrary(model, library));

            Assert.AreEqual(EnginePresetLibrarySO.MaxSlots, runtimeLibrary.Slots.Count);
            Assert.AreEqual(0, runtimeLibrary.Slots[0].PresetIndex);
            Assert.AreEqual(1, runtimeLibrary.Slots[1].PresetIndex);
            Assert.AreEqual(base0.MaxOutput, runtimeLibrary.Slots[0].MaxOutput, 0.001f);

            EnginePresetState researched = model.GetEnginePreset(EnginePresetId.Engine02);
            float expectedOutput = base1.MaxOutput * researched.MaxOutput / ResearchPrototypeModel.InitialEngineStat;
            Assert.AreEqual(expectedOutput, runtimeLibrary.Slots[1].MaxOutput, 0.001f);
            Assert.AreEqual(model.GetEngineInstallCost(EnginePresetId.Engine02), runtimeLibrary.Slots[1].Price);
            Assert.Greater(runtimeLibrary.Slots[1].Price, ResearchPrototypeModel.EngineInstallCost);
            Assert.AreEqual(1400f, base1.MaxOutput, 0.001f, "원본 SO 값은 연구 반영으로 바뀌면 안 된다.");
        }

        [TestCase(0, 0f)]
        [TestCase(40, 40f)]
        [TestCase(66, 66f)]
        [TestCase(100, 100f)]
        [TestCase(-10, 0f)]
        [TestCase(110, 100f)]
        public void RuntimeBridge_IgnitionUsesResearchPercent(int researchValue, float expected)
        {
            var state = new EnginePresetState { IgnitionReliability = researchValue };
            foreach (float baseIgnition in new[] { 0f, 40f, 100f })
            {
                EngineStatsSO source = Stats(100f, 60f, 1200f, baseIgnition);
                EngineStatsSO runtime = Track(ResearchEnginePresetRuntimeBridge.BuildRuntimePreset(0, source, state));
                Assert.AreEqual(expected, runtime.IgnitionReliability);
                Assert.AreEqual(baseIgnition, source.IgnitionReliability);
            }
            Assert.AreEqual(expected,
                Track(ResearchEnginePresetRuntimeBridge.BuildRuntimePreset(0, null, state)).IgnitionReliability);
        }

        [TestCase(EngineStatId.FuelCapacity)]
        [TestCase(EngineStatId.Cooling)]
        [TestCase(EngineStatId.MaxOutput)]
        [TestCase(EngineStatId.IgnitionReliability)]
        public void RuntimeBridge_ResearchReachesPartAndPhysicalEffects(EngineStatId stat)
        {
            var model = new ResearchPrototypeModel();
            // Isolate fuel/heat behavior from random ignition; the ignition case starts at 40%.
            if (stat != EngineStatId.IgnitionReliability)
                model.GetEnginePreset(EnginePresetId.Engine01).IgnitionReliability = 100;
            EngineStatsSO source = Stats(100f, 10f, 1200f, 100f);
            var library = Track(ScriptableObject.CreateInstance<EnginePresetLibrarySO>());
            SetField(library, "slots", new List<EngineStatsSO> { source, source });
            EnginePresetLibrarySO before = TrackRuntimeLibrary(
                ResearchEnginePresetRuntimeBridge.BuildRuntimeLibrary(model, library));
            int oldValue = model.GetEnginePreset(EnginePresetId.Engine01).GetStat(stat);

            Assert.AreEqual(ResearchActionResult.Success,
                model.ExecuteEngineResearch(EnginePresetId.Engine01, stat, focused: true, score: 100));
            Assert.AreEqual(oldValue + 26, model.GetEnginePreset(EnginePresetId.Engine01).GetStat(stat));
            EnginePresetLibrarySO after = TrackRuntimeLibrary(
                ResearchEnginePresetRuntimeBridge.BuildRuntimeLibrary(model, library));
            RocketPart original = CreateEngine(null);
            RocketPart upgraded = CreateEngine(null);
            original.ApplyPreset(before.Slots[0]);
            upgraded.ApplyPreset(after.Slots[0]);
            Assert.AreSame(after.Slots[0], upgraded.Stats);
            Assert.AreEqual(stat == EngineStatId.FuelCapacity ? 165f : 100f, upgraded.Stats.FuelCapacity, 0.001f);
            Assert.AreEqual(stat == EngineStatId.Cooling ? 16.5f : 10f, upgraded.Stats.Cooling, 0.001f);
            Assert.AreEqual(stat == EngineStatId.MaxOutput ? 1980f : 1200f, upgraded.Output, 0.001f);
            Assert.AreEqual(stat == EngineStatId.IgnitionReliability ? 66f : 100f, upgraded.Stats.IgnitionReliability);

            Assert.AreEqual(100f, source.FuelCapacity);
            Assert.AreEqual(10f, source.Cooling);
            Assert.AreEqual(1200f, source.MaxOutput);
            Assert.AreEqual(100f, source.IgnitionReliability);
            Assert.AreEqual(before.Slots[1].FuelCapacity, after.Slots[1].FuelCapacity);
            Assert.AreEqual(before.Slots[1].Cooling, after.Slots[1].Cooling);
            Assert.AreEqual(before.Slots[1].MaxOutput, after.Slots[1].MaxOutput);
            Assert.AreEqual(before.Slots[1].IgnitionReliability, after.Slots[1].IgnitionReliability);

            var rng = new DeterministicRng();
            rng.Reseed(5); // First roll is 46: fails at 40%, succeeds at 66%.
            Assert.AreEqual(46, rng.Next(1, 101));
            rng.Reseed(5);
            original.Prepare(rng);
            rng.Reseed(5);
            upgraded.Prepare(rng);
            Assert.IsTrue(upgraded.Ignited);
            if (stat == EngineStatId.IgnitionReliability)
            {
                Assert.IsFalse(original.Ignited);
                Assert.IsFalse(original.Tick(1f));
                Assert.IsTrue(upgraded.Tick(1f));
                return;
            }

            Assert.IsTrue(original.Tick(1f));
            Assert.IsTrue(upgraded.Tick(1f));
            if (stat == EngineStatId.Cooling)
                Assert.Less(upgraded.Temperature, original.Temperature);
            else if (stat == EngineStatId.MaxOutput)
            {
                Assert.Greater(upgraded.Output, original.Output);
                Assert.Less(upgraded.Remaining, original.Remaining);
                Assert.Greater(upgraded.Temperature, original.Temperature);
            }
            else
            {
                original.Tick(4f);
                upgraded.Tick(4f);
                Assert.IsFalse(original.HasFuel);
                Assert.IsTrue(upgraded.HasFuel);
                float originalMass = LaunchMass(original);
                Assert.AreEqual(65f * 0.25f, LaunchMass(upgraded) - originalMass, 0.001f);
            }
        }

        private float LaunchMass(RocketPart part)
        {
            var rocket = Track(new GameObject("research mass test")).AddComponent<Rocket>();
            Invoke(rocket, "Awake");
            rocket.Attach(part, rocket.transform.position);
            rocket.Launch();
            return rocket.GetComponent<Rigidbody>().mass;
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
            Assert.AreEqual(40f, runtimeLibrary.Slots[5].IgnitionReliability, 0.001f);
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
        public void PresetPanel_ListsOnlyDevelopedPresets()
        {
            // 설계 패널은 연구가 열지 않은 슬롯을 내지 않는다(GDD 07 §4). 라이브러리는 계속 10칸이라
            // 여기가 유일한 거름망이다 — 빠지면 아직 없는 엔진을 끌어다 붙일 수 있게 된다.
            MethodInfo isDeveloped = typeof(RocketDesignUI).GetMethod(
                "IsDeveloped", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(isDeveloped, "RocketDesignUI.IsDeveloped 를 찾지 못했다.");

            var model = new ResearchPrototypeModel();
            EngineStatsSO slot0 = Stats(100f, 60f, BaselineOutput, 100f, presetIndex: 0);
            EngineStatsSO slot1 = Stats(100f, 60f, BaselineOutput, 100f, presetIndex: 1);
            EngineStatsSO authored = Stats(100f, 60f, BaselineOutput, 100f); // PresetIndex -1

            Assert.IsTrue((bool)isDeveloped.Invoke(null, new object[] { slot0, model }),
                "새 게임은 엔진 01 이 열려 있다.");
            Assert.IsFalse((bool)isDeveloped.Invoke(null, new object[] { slot1, model }),
                "아직 개발하지 않은 슬롯은 목록에 뜨면 안 된다.");
            Assert.IsTrue((bool)isDeveloped.Invoke(null, new object[] { authored, model }),
                "PresetIndex -1 은 저작 에셋이다 — SimulationTest 단독 재생이 간다.");

            Assert.AreEqual(ResearchActionResult.Success, model.CreateNewEnginePreset(out _),
                "시작 예산 2200 은 새 프리셋 비용 150 을 감당한다.");

            Assert.IsTrue((bool)isDeveloped.Invoke(null, new object[] { slot1, model }),
                "새 엔진을 개발하면 그 슬롯이 설계 목록에 나타난다.");
        }

        [Test]
        public void PresetPanel_LabelsUseResearchDisplayNames()
        {
            // 연구 화면 카드는 `엔진 01`, 설계 패널은 `Baseline_Runtime` — 같은 엔진이 두 이름이면
            // 연동됐다는 신호 자체가 사라진다.
            MethodInfo displayName = typeof(RocketDesignUI).GetMethod(
                "DisplayName", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(displayName, "RocketDesignUI.DisplayName 을 찾지 못했다.");

            EngineStatsSO researched = Stats(100f, 60f, BaselineOutput, 100f, presetIndex: 0);
            researched.name = "EngineStats_Baseline_Runtime";
            EngineStatsSO authored = Stats(100f, 60f, BaselineOutput, 100f);
            authored.name = "EngineStats_Baseline";

            Assert.AreEqual(
                ResearchPrototypeModel.GetEnginePresetConfig(EnginePresetId.Engine01).DisplayName,
                (string)displayName.Invoke(null, new object[] { researched }));
            Assert.AreEqual("Baseline", (string)displayName.Invoke(null, new object[] { authored }),
                "저작 에셋은 접두사만 떼어낸다.");
        }

        [Test]
        public void RuntimeBridge_Checksum_ChangesWhenAPresetIsUnlocked()
        {
            // 체크섬이 잠금을 안 보면 새 엔진을 개발해도 런타임 라이브러리가 다시 안 만들어진다.
            MethodInfo checksum = typeof(ResearchEnginePresetRuntimeBridge).GetMethod(
                "CalculateResearchChecksum", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(checksum, "CalculateResearchChecksum 을 찾지 못했다.");

            var model = new ResearchPrototypeModel();
            int before = (int)checksum.Invoke(null, new object[] { model });

            Assert.AreEqual(ResearchActionResult.Success, model.CreateNewEnginePreset(out _),
                "시작 예산 2200 은 새 프리셋 비용 150 을 감당한다.");

            Assert.AreNotEqual(before, (int)checksum.Invoke(null, new object[] { model }),
                "프리셋 해금은 체크섬을 바꿔야 한다.");
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

        [Test]
        public void PresetMesh_PicksArchetypePrefab_AndLeavesAuthoredAssetsAlone()
        {
            // 연구 화면과 설계 화면이 같은 프리셋에 다른 엔진을 보여주면 연동됐다는 신호가 사라진다.
            // 저작 에셋(PresetIndex -1)이 null 로 떨어지는 것도 같이 잠근다 — SimulationTest 단독 재생 경로다.
            MethodInfo resolve = typeof(RocketPart).GetMethod(
                "ResolveMeshPrefab", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(resolve, "RocketPart.ResolveMeshPrefab 을 찾지 못했다.");

            GameObject balanced = Track(new GameObject("Balanced"));
            GameObject fuel = Track(new GameObject("FuelCapacity"));
            GameObject cooling = Track(new GameObject("Cooling"));
            GameObject power = Track(new GameObject("MaxOutput"));
            GameObject reliability = Track(new GameObject("IgnitionReliability"));
            EnginePresetVisualLibrarySO library = Track(EnginePresetVisualLibrarySO.CreateRuntime(
                null, new[] { balanced, fuel, cooling, power, reliability }));

            var model = new ResearchPrototypeModel();
            EngineStatsSO slot0 = Stats(100f, 60f, BaselineOutput, 100f, presetIndex: 0);
            EngineStatsSO authored = Stats(100f, 60f, BaselineOutput, 100f);

            Assert.AreSame(balanced, resolve.Invoke(null, new object[] { slot0, model, library }),
                "새 게임의 네 스탯은 같으므로 균형형이다.");

            EnginePresetState state = model.GetEnginePreset(EnginePresetId.Engine01);
            state.MaxOutput += EngineVisualClassifier.SpecializationLeadThreshold;

            Assert.AreSame(power, resolve.Invoke(null, new object[] { slot0, model, library }),
                "출력이 임계치만큼 앞서면 출력 특화 메시로 바뀐다.");
            Assert.IsNull(resolve.Invoke(null, new object[] { authored, model, library }),
                "저작 에셋은 프리팹 기본 메시를 그대로 쓴다.");
            Assert.IsNull(resolve.Invoke(null, new object[] { slot0, model, null }),
                "라이브러리가 없으면 교체하지 않는다.");
        }

        [Test]
        public void PresetMesh_SwapRefitsColliderAndFlame_AndKeepsFlameAlive()
        {
            // 아트 원본 스케일을 그대로 쓰기로 했으므로 프리팹마다 치수가 다르다. 콜라이더가 따라가지
            // 않으면 집기 레이캐스트가 어긋나고, 불꽃이 따라가지 않으면 노즐 밖에서 탄다.
            // 불꽃은 메시의 자식이 아니라 형제다 — "자식 전부 지우기" 로 구현하면 여기서 죽는다.
            MethodInfo setMesh = typeof(RocketPart).GetMethod(
                "SetMesh", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(setMesh, "RocketPart.SetMesh 를 찾지 못했다.");

            RocketPart part = CreateEngine(null);
            BoxCollider box = part.GetComponent<BoxCollider>();
            box.size = new Vector3(0.547f, 1f, 0.541f); // 기본 메시(Engine_01)의 값
            box.center = Vector3.zero;

            GameObject oldMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            oldMesh.transform.SetParent(part.transform, false);
            SetField(part, "meshRoot", oldMesh.transform);

            var flameGo = new GameObject("Flame");
            flameGo.transform.SetParent(part.transform, false);
            flameGo.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            ParticleSystem flame = flameGo.AddComponent<ParticleSystem>();
            SetField(part, "flame", flame);

            // 렌더러가 통째로 바뀌므로 지연 캐시는 반납돼야 한다. 빈 배열이면 머티리얼 인스턴스를
            // 만들지 않고도 반납 경로를 지난다.
            SetField(part, "_uberMaterials", new Material[0]);

            GameObject prefab = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            prefab.transform.localScale = new Vector3(2f, 4f, 2f); // 1유닛 큐브 × 스케일 = bounds

            setMesh.Invoke(part, new object[] { prefab });

            Assert.IsTrue(oldMesh == null, "기본 메시는 교체와 함께 사라져야 한다.");
            Assert.AreEqual(2, part.transform.childCount, "새 메시와 Flame 둘만 남아야 한다.");
            Assert.IsTrue(flame != null, "Flame 은 메시의 형제라 교체가 건드리면 안 된다.");

            Assert.AreEqual(new Vector3(2f, 4f, 2f), box.size);
            Assert.AreEqual(Vector3.zero, box.center, "RocketBuilder.HalfExtents 가 center 0 을 가정한다.");
            Assert.AreEqual(-2f, flame.transform.localPosition.y, 1e-4f, "불꽃은 새 메시 바닥으로 내려간다.");
            Assert.IsNull(typeof(RocketPart)
                .GetField("_uberMaterials", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(part), "머티리얼 캐시가 남으면 사라진 렌더러의 인스턴스를 계속 붙든다.");
        }

        private EngineStatsSO Stats(float fuel, float cooling, float output, float ignition, int presetIndex = -1)
        {
            var stats = Track(ScriptableObject.CreateInstance<EngineStatsSO>());
            SetField(stats, "presetIndex", presetIndex);
            SetField(stats, "fuelCapacity", fuel);
            SetField(stats, "cooling", cooling);
            SetField(stats, "maxOutput", output);
            SetField(stats, "ignitionReliability", ignition);
            return stats;
        }

        [TestCase(100f, 60f, 1200f, false)]
        [TestCase(90f, 55f, 1800f, true)]
        [TestCase(120f, 80f, 2400f, true)]
        [TestCase(90f, 100f, 1800f, false)]
        public void HeatBalance_WithIgnitionRamp_CoolingPreventsExplosion(float fuel, float cooling, float output, bool expected)
        {
            RocketPart part = CreateEngine(Stats(fuel, cooling, output, 100f));
            part.Prepare(new DeterministicRng());
            for (int i = 1; i <= 2000 && part.HasFuel && !part.Overheated; i++)
                part.Tick(0.02f, Rocket.RampFactor(i * 0.02f, 1.2f));
            Assert.AreEqual(expected, part.Overheated);
            if (expected) Assert.IsTrue(part.HasFuel);
        }

        [Test]
        public void Overheat_ExplodesOnce_ReportsDistinctFailure_AndResetsVisuals()
        {
            var host = Track(new GameObject("overheat rocket"));
            var rocket = host.AddComponent<Rocket>();
            Invoke(rocket, "Awake");
            var renderer = host.AddComponent<MeshRenderer>();
            var part = CreateEngine(Stats(200f, 0f, 2400f, 100f));
            rocket.Attach(part, Vector3.zero);
            var mission = host.AddComponent<LaunchMissionController>();
            int completed = 0, explosions = 0;
            mission.Initialize(LaunchMissionId.HighAltitude, () => true, success =>
            {
                Assert.IsFalse(success);
                completed++;
            });
            mission.ExplosionRequested.AddListener(() => explosions++);
            rocket.Launch();
            for (int i = 0; i < 250; i++) Invoke(rocket, "FixedUpdate");
            Assert.IsTrue(rocket.Overheated);
            Assert.IsTrue(rocket.Exploded);
            Assert.IsFalse(renderer.enabled);
            Assert.IsTrue(mission.IsExploding);
            Assert.AreEqual(LaunchTerminationReason.Overheat, mission.TerminationReason);
            Assert.AreEqual(1, explosions);
            Assert.AreEqual(0, completed);
            mission.CompleteSelfDestruction();
            mission.CompleteSelfDestruction();
            Assert.AreEqual(1, completed);
            rocket.ResetFlight(Vector3.zero, Quaternion.identity);
            Assert.IsFalse(rocket.Exploded);
            Assert.IsTrue(renderer.enabled);
        }

        [Test]
        public void ResetFlight_RestoresPoseAndPhysics_AndAllowsRepeatedLaunch()
        {
            var host = Track(new GameObject("tester rocket"));
            var rocket = host.AddComponent<Rocket>();
            var body = host.GetComponent<Rigidbody>();
            body.mass = 12f;
            body.linearDamping = 0.2f;
            Invoke(rocket, "Awake");
            RocketPart part = CreateEngine(Stats(100f, 1000f, BaselineOutput, 100f));
            rocket.Attach(part, Vector3.right);
            Vector3 localPosition = part.transform.localPosition;
            Quaternion localRotation = part.transform.localRotation;
            for (int i = 0; i < 3; i++)
            {
                rocket.Launch();
                Assert.IsTrue(rocket.Launched);
                part.Tick(1f);
                host.transform.position = new Vector3(0, -50, 0);
                Invoke(rocket, "TickWater");
                rocket.StopFlight();
                rocket.ResetFlight(Vector3.zero, Quaternion.identity);
                Assert.IsFalse(rocket.Launched);
                Assert.IsFalse(rocket.FlightStopped);
                Assert.IsFalse(rocket.Splashed);
                Assert.IsFalse(part.Ignited);
                Assert.AreEqual(localPosition, part.transform.localPosition);
                Assert.AreEqual(localRotation, part.transform.localRotation);
                Assert.AreEqual(12f, body.mass);
                Assert.AreEqual(0.2f, body.linearDamping);
                Assert.IsTrue(body.isKinematic);
            }
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
