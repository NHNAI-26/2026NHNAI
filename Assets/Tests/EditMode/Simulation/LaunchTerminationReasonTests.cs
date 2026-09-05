using System.Collections.Generic;
using System.Reflection;
using Border.Research;
using NUnit.Framework;
using UnityEngine;

namespace Simulation.Tests
{
    public sealed class LaunchTerminationReasonTests
    {
        [Test]
        public void Evaluator_ReportsSucceededReasonWithoutChangingOutcome()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);

            Assert.That(evaluator.TerminationReason, Is.EqualTo(LaunchTerminationReason.Unknown));
            Assert.That(evaluator.Step(0.1f, 100f, 0f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Succeeded));

            Assert.That(evaluator.TerminationReason, Is.EqualTo(LaunchTerminationReason.Succeeded));
            Assert.That(evaluator.FailureReason, Is.Empty);
        }

        [Test]
        public void Evaluator_ReportsNoLiftoffReason()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);

            evaluator.Step(9f, 0f, 0f, 0f, 0f, 0f, isGrounded: true);
            Assert.That(evaluator.Step(1f, 0f, 0f, 1f, 0f, 0f, isGrounded: true),
                Is.EqualTo(LaunchMissionOutcome.Failed));

            Assert.That(evaluator.TerminationReason, Is.EqualTo(LaunchTerminationReason.NoLiftoff));
            Assert.That(evaluator.FailureReason, Does.Contain("이륙"));
        }

        [Test]
        public void Evaluator_ReportsGroundCrashReason()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.HighAltitude);

            evaluator.Step(1f, 50f, 0f, 10f, 0f, 0f);
            Assert.That(evaluator.Step(3f, 0f, 0f, 1f, 0f, 0f, isGrounded: true),
                Is.EqualTo(LaunchMissionOutcome.Failed));

            Assert.That(evaluator.TerminationReason, Is.EqualTo(LaunchTerminationReason.GroundCrash));
            Assert.That(evaluator.FailureReason, Does.Contain("지면"));
        }

        [Test]
        public void Evaluator_ReportsSplashdownReason()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.HighAltitude);

            Assert.That(evaluator.Step(3f, -10f, 0f, 20f, 90f, 0f, hasSplashed: true),
                Is.EqualTo(LaunchMissionOutcome.Failed));

            Assert.That(evaluator.TerminationReason, Is.EqualTo(LaunchTerminationReason.Splashdown));
            Assert.That(evaluator.FailureReason, Does.Contain("바다"));
        }

        [Test]
        public void Evaluator_ReportsSelfDestructReasonAndKeepsActionBoolSemantics()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);

            Assert.That(evaluator.SelfDestruct(), Is.EqualTo(LaunchMissionOutcome.Failed));

            Assert.That(evaluator.TerminationReason, Is.EqualTo(LaunchTerminationReason.SelfDestruct));
            Assert.That(evaluator.Step(1f, 100f, 0f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Failed));
        }

        [Test]
        public void Controller_ForwardsEvaluatorTerminationReason()
        {
            var results = new List<bool>();
            var go = new GameObject("launch termination reason controller test");
            try
            {
                var body = go.AddComponent<Rigidbody>();
                var rocket = go.AddComponent<Rocket>();
                Invoke(rocket, "Awake");
                var controller = go.AddComponent<LaunchMissionController>();
                controller.Initialize(LaunchMissionId.LowAltitude, () => true, results.Add);

                Assert.That(controller.TerminationReason, Is.EqualTo(LaunchTerminationReason.Unknown));
                rocket.Launch();
                go.transform.position = Vector3.up * 100f;
                Invoke(controller, "FixedUpdate");

                Assert.That(controller.TerminationReason, Is.EqualTo(LaunchTerminationReason.Succeeded));
                Assert.That(results, Is.EqualTo(new[] { true }));
                Assert.That(body.isKinematic, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void Invoke(object target, string name) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
