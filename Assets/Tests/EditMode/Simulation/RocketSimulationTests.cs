using System.Collections.Generic;
using System.Reflection;
using Border.Core;
using NUnit.Framework;
using UnityEngine;

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

        private EngineStatsSO Stats(float fuel, float cooling, float output, float ignition)
        {
            var stats = Track(ScriptableObject.CreateInstance<EngineStatsSO>());
            SetField(stats, "fuelCapacity", fuel);
            SetField(stats, "cooling", cooling);
            SetField(stats, "maxOutput", output);
            SetField(stats, "ignitionReliability", ignition);
            return stats;
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
