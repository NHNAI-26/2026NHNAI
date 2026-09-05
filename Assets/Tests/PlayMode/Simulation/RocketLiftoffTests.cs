using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class RocketLiftoffTests
    {
        private GameObject host;
        private EngineStatsSO stats;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (host != null) Object.Destroy(host);
            if (stats != null) Object.Destroy(stats);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuidedLift_RisesFromLaunchOrigin_HandsOffWithVelocity_ThenUsesEnginePhysics()
        {
            host = new GameObject("assisted liftoff physics test");
            host.transform.position = new Vector3(5000f, 20f, 5000f);
            var body = host.AddComponent<Rigidbody>();
            body.mass = 50f;
            body.linearDamping = 0f;
            var rocket = host.AddComponent<Rocket>();
            SetField(rocket, "holdSeconds", 0f);
            SetField(rocket, "assistedLiftHeight", 3f);
            SetField(rocket, "assistedLiftSeconds", 2.5f);
            SetField(rocket, "physicsBlendSeconds", 1f);
            var engineObject = new GameObject("engine");
            engineObject.transform.SetParent(host.transform, false);
            engineObject.AddComponent<BoxCollider>();
            var engine = engineObject.AddComponent<RocketPart>();
            stats = EngineStatsSO.CreateRuntimeCopy(-1, null, 0, 100f, 1000f, 1200f, 100f);
            engine.ApplyPreset(stats);
            float origin = body.position.y;
            rocket.Launch();
            float previousY = origin;
            float previousVelocity = 0f;
            bool sawDynamic = false;
            int steps = Mathf.CeilToInt(4f / Time.fixedDeltaTime);
            for (int i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.GreaterOrEqual(body.position.y, previousY - 0.001f, "출발점 아래로 처지면 안 된다.");
                if (body.isKinematic)
                {
                    Assert.LessOrEqual(body.position.y, origin + 3.001f);
                    if (i < Mathf.FloorToInt(0.5f / Time.fixedDeltaTime))
                        Assert.Less(body.position.y - origin, 0.2f, "초반에는 무게감 있게 천천히 떠야 한다.");
                }
                else if (!sawDynamic)
                {
                    sawDynamic = true;
                    Assert.That(body.position.y, Is.InRange(origin + 2.99f, origin + 3.2f));
                    Assert.That(body.linearVelocity.y, Is.InRange(2.3f, 2.6f), "유도 상승 끝 속도를 물리에 전달한다.");
                }
                else
                {
                    Assert.Less(Mathf.Abs(body.linearVelocity.y - previousVelocity), 0.4f,
                        "전환 중 속도가 갑자기 튀면 안 된다.");
                }
                previousY = body.position.y;
                previousVelocity = body.linearVelocity.y;
            }
            Assert.IsTrue(sawDynamic);
            Assert.IsFalse(rocket.LiftAssistActive);
            Assert.Less(engine.Remaining, 100f, "유도 상승 중에도 연료를 소비한다.");
            float before = body.linearVelocity.y;
            yield return new WaitForFixedUpdate();
            float expectedAcceleration = engine.Output / body.mass + Physics.gravity.y;
            Assert.AreEqual(expectedAcceleration, (body.linearVelocity.y - before) / Time.fixedDeltaTime, 0.1f,
                "전환 후에는 보조력 없이 엔진 힘과 중력만 적용한다.");
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
