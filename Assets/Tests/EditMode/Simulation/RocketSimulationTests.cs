using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Border.Simulation.Tests
{
    public sealed class RocketSimulationTests
    {
        [Test]
        public void TryBurn_ConsumesFuel_ThenRefusesWhenDry()
        {
            var go = new GameObject("engine");
            go.AddComponent<BoxCollider>(); // RocketPart 의 RequireComponent(Collider) 는 추상 타입이라 자동 추가되지 않는다
            var part = go.AddComponent<RocketPart>();
            SetField(part, "fuel", 10f);
            SetField(part, "burnRate", 20f);
            part.Refill(); // EditMode 에서는 Awake 가 돌지 않는다

            Assert.IsTrue(part.TryBurn(0.25f), "연료가 남아 있으면 태울 수 있어야 한다.");
            Assert.AreEqual(5f, part.Remaining, 1e-4f, "0.25초 × 20/초 = 5 만큼 줄어야 한다.");

            Assert.IsTrue(part.TryBurn(0.25f), "마지막 연료를 태우는 프레임도 추력을 내야 한다.");
            Assert.AreEqual(0f, part.Remaining, 1e-4f, "연료는 음수로 내려가지 않는다.");
            Assert.IsFalse(part.HasFuel);

            Assert.IsFalse(part.TryBurn(0.25f), "연료가 0이면 추력이 없어야 한다 — 여기서부터 낙하한다.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Attach_KeepsWorldPoint_AndAlignsToRocket()
        {
            var rocketGo = new GameObject("rocket");
            rocketGo.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            var rocket = rocketGo.AddComponent<Rocket>();

            var partGo = new GameObject("engine");
            partGo.transform.rotation = Quaternion.Euler(45f, 45f, 45f);
            partGo.AddComponent<BoxCollider>();
            var part = partGo.AddComponent<RocketPart>();

            var surfacePoint = new Vector3(0.5f, 1.2f, -0.3f); // 하단이 아닌 측면 지점
            rocket.Attach(part, surfacePoint);

            Assert.AreSame(rocketGo.transform, partGo.transform.parent);
            Assert.That(Vector3.Distance(surfacePoint, partGo.transform.position), Is.LessThan(1e-4f),
                "부착 지점은 레이캐스트가 맞은 표면 좌표 그대로여야 한다.");
            Assert.That(Quaternion.Angle(rocketGo.transform.rotation, partGo.transform.rotation),
                Is.LessThan(1e-3f),
                "부품 자세는 로켓 기준을 따라야 한다 — 추력 방향이 로켓의 up 고정이기 때문.");

            Object.DestroyImmediate(partGo);
            Object.DestroyImmediate(rocketGo);
        }

        [Test]
        public void Flame_TurnsOnWhileBurning_AndOffWhenDry()
        {
            var go = new GameObject("engine");
            go.AddComponent<BoxCollider>();
            var part = go.AddComponent<RocketPart>();

            var flameGo = new GameObject("flame");
            flameGo.transform.SetParent(go.transform);
            var flame = flameGo.AddComponent<ParticleSystem>();
            ParticleSystem.EmissionModule emission = flame.emission; // 구조체 프로퍼티라 지역 변수로 받아 쓴다
            flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SetField(part, "flame", flame);
            SetField(part, "fuel", 10f);
            SetField(part, "burnRate", 20f);
            part.Refill();

            Assert.IsFalse(flame.isEmitting, "발사 전에는 불꽃이 꺼져 있어야 한다.");

            part.TryBurn(0.25f);
            Assert.IsTrue(flame.isEmitting, "연료를 태우는 동안에는 불꽃이 켜져 있어야 한다.");
            Assert.IsTrue(emission.enabled, "emission 모듈 자체는 켜진 상태여야 한다.");

            part.TryBurn(0.25f); // 여기서 연료가 0 이 된다
            part.TryBurn(0.25f); // 소진 후 첫 호출에서 꺼진다
            Assert.IsFalse(part.HasFuel);
            Assert.IsFalse(flame.isEmitting, "연료가 떨어지면 불꽃이 꺼져야 한다.");

            Object.DestroyImmediate(go);
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
