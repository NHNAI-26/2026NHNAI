using System;
using Border.Research;
using NUnit.Framework;

namespace Simulation.Tests
{
    public sealed class LaunchMissionEvaluatorTests
    {
        [TestCase(LaunchMissionId.LowAltitude, 100f)]
        [TestCase(LaunchMissionId.HighAltitude, 300f)]
        public void AltitudeMission_SucceedsAtInclusiveThreshold(LaunchMissionId mission, float target)
        {
            var evaluator = new LaunchMissionEvaluator(mission);
            Assert.That(evaluator.Step(0.1f, target - 0.01f, 0f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(0.1f, target, 0f, 0f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Succeeded),
                "Success wins when the speed failure threshold is reached in the same sample.");
        }

        [TestCase(80f)]
        [TestCase(120f)]
        public void TargetZone_IncludesBothHorizontalBoundaries(float distance)
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.TargetZone);
            Assert.That(evaluator.Step(0.1f, 200f, distance, 10f, 180f, 20f), Is.EqualTo(LaunchMissionOutcome.Succeeded));
        }

        [Test]
        public void TargetZone_RequiresAltitudeAndDistanceInTheSameSample()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.TargetZone);
            Assert.That(evaluator.Step(0.1f, 200f, 79f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(0.1f, 199f, 100f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(0.1f, 200f, 121f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
        }

        [TestCase(LaunchMissionId.ZoneHold)]
        [TestCase(LaunchMissionId.LowPowerZoneHold)]
        public void HoldMission_RequiresThreeContinuousSeconds(LaunchMissionId mission)
        {
            var evaluator = new LaunchMissionEvaluator(mission);
            Assert.That(evaluator.Step(2f, 200f, 80f, 50f, 30f, 8f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.HoldSeconds, Is.EqualTo(2f));
            Assert.That(evaluator.Step(1f, 200f, 120f, 50f, 30f, 8f), Is.EqualTo(LaunchMissionOutcome.Succeeded));
        }

        [TestCase(199f, 100f, 10f, 0f)]
        [TestCase(200f, 79f, 10f, 0f)]
        [TestCase(200f, 121f, 10f, 0f)]
        [TestCase(200f, 100f, 50.01f, 0f)]
        [TestCase(200f, 100f, 10f, 30.01f)]
        public void HoldMission_ResetsOnAnyBrokenCondition(float altitude, float distance, float speed, float angle)
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.ZoneHold);
            evaluator.Step(2f, 200f, 100f, 10f, 0f, 0f);
            Assert.That(evaluator.Step(0.1f, altitude, distance, speed, angle, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.HoldSeconds, Is.Zero);
            Assert.That(evaluator.Step(2f, 200f, 100f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(1f, 200f, 100f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Succeeded));
        }

        [TestCase(LaunchMissionId.ZoneHold, LaunchMissionOutcome.Succeeded)]
        [TestCase(LaunchMissionId.LowPowerZoneHold, LaunchMissionOutcome.Running)]
        public void OnlyLowPowerMission_EnforcesAggregateBurnBudget(LaunchMissionId mission, LaunchMissionOutcome expected)
        {
            var evaluator = new LaunchMissionEvaluator(mission);
            Assert.That(evaluator.Step(3f, 200f, 100f, 10f, 0f, 8.01f), Is.EqualTo(expected));
        }

        [Test]
        public void LowPowerMission_BurnBudgetExceededDuringHoldResetsProgress()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowPowerZoneHold);
            evaluator.Step(2f, 200f, 100f, 10f, 0f, 8f);
            Assert.That(evaluator.Step(1f, 200f, 100f, 10f, 0f, 8.01f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.HoldSeconds, Is.Zero);
        }

        [Test]
        public void Launch_ZeroInitialSpeedHasGracePeriod()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);
            Assert.That(evaluator.Step(1f, 0f, 0f, 0f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(1f, 0.1f, 0f, 1f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(1f, 1f, 0f, 1.01f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(10f, 20f, 0f, 2f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
        }

        [Test]
        public void Launch_FailsAtNoLiftoffTimeout()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);
            evaluator.Step(2f, 0f, 0f, 0f, 0f, 0f);
            Assert.That(evaluator.Step(1f, 0f, 0f, 1f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Failed));
            Assert.That(evaluator.FailureReason, Is.Not.Empty);
        }

        [Test]
        public void Flight_FailsWhenSpeedFallsToInclusiveThreshold()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.HighAltitude);
            evaluator.Step(0.1f, 1f, 0f, 2f, 0f, 0f);
            Assert.That(evaluator.Step(0.1f, 50f, 0f, 1f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Failed));
        }

        [TestCase(LaunchMissionId.ZoneHold)]
        [TestCase(LaunchMissionId.LowPowerZoneHold)]
        public void ValidHold_DoesNotFailAtZeroSpeed(LaunchMissionId mission)
        {
            var evaluator = new LaunchMissionEvaluator(mission);
            evaluator.Step(0.1f, 10f, 0f, 10f, 0f, 0f);
            Assert.That(evaluator.Step(2f, 200f, 100f, 0f, 0f, 8f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(1f, 200f, 100f, 0f, 0f, 8f), Is.EqualTo(LaunchMissionOutcome.Succeeded));
        }

        [Test]
        public void ValidHold_DoesNotFailAtLiftoffTimeout()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.ZoneHold,
                new LaunchMissionRules { RequiredHoldSeconds = 4f });
            Assert.That(evaluator.Step(3f, 200f, 100f, 0f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
            Assert.That(evaluator.Step(1f, 200f, 100f, 0f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Succeeded));
        }

        [Test]
        public void BrokenHold_AtLowSpeedFails()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.ZoneHold);
            evaluator.Step(1f, 200f, 100f, 10f, 0f, 0f);
            Assert.That(evaluator.Step(1f, 199f, 100f, 0f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Failed));
        }

        [Test]
        public void Success_IsStickyIncludingSelfDestruct()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);
            evaluator.Step(0.1f, 100f, 0f, 10f, 0f, 0f);
            Assert.That(evaluator.SelfDestruct(), Is.EqualTo(LaunchMissionOutcome.Succeeded));
            Assert.That(evaluator.Step(100f, 0f, 0f, 0f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Succeeded));
        }

        [Test]
        public void SelfDestruct_FailsImmediatelyAndFailureIsSticky()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);
            Assert.That(evaluator.SelfDestruct(), Is.EqualTo(LaunchMissionOutcome.Failed));
            Assert.That(evaluator.FailureReason, Is.Not.Empty);
            Assert.That(evaluator.Step(1f, 100f, 0f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Failed));
        }

        [Test]
        public void Rules_AreCapturedAtConstruction()
        {
            var rules = new LaunchMissionRules { LowAltitude = 20f };
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude, rules);
            rules.LowAltitude = 100f;
            Assert.That(evaluator.Step(1f, 20f, 0f, 10f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Succeeded));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void RemovedOrUnknownMission_IsRejected(int mission)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchMissionEvaluator((LaunchMissionId)mission));
        }

        [Test]
        public void InvalidRules_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new LaunchMissionEvaluator(LaunchMissionId.TargetZone,
                new LaunchMissionRules { TargetHorizontalMin = 130f }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchMissionEvaluator(LaunchMissionId.TargetZone,
                new LaunchMissionRules { FailureSpeed = float.NaN }));
            Assert.Throws<ArgumentException>(() => new LaunchMissionEvaluator(LaunchMissionId.ZoneHold,
                new LaunchMissionRules { RequiredHoldSeconds = 0f }));
        }

        [Test]
        public void InvalidTelemetry_IsRejectedWithoutAdvancingTime()
        {
            var evaluator = new LaunchMissionEvaluator(LaunchMissionId.LowAltitude);
            Assert.Throws<ArgumentOutOfRangeException>(() => evaluator.Step(3f, float.NaN, 0f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => evaluator.Step(-1f, 0f, 0f, 0f, 0f, 0f));
            Assert.That(evaluator.Step(1f, 0f, 0f, 0f, 0f, 0f), Is.EqualTo(LaunchMissionOutcome.Running));
        }
    }
}
