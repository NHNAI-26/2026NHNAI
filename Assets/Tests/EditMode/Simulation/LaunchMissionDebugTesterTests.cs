#if UNITY_EDITOR
using Border.Research;
using NUnit.Framework;

namespace Simulation.Tests
{
    public sealed class LaunchMissionDebugTesterTests
    {
        [SetUp]
        public void SetUp()
        {
            ResearchFlowSession.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            ResearchFlowSession.ResetForTests();
        }

        [Test]
        public void PrepareMissionDesignEntry_ReadiesMissionThree()
        {
            ResearchActionResult result = LaunchMissionDebugTester.PrepareMissionDesignEntry(LaunchMissionId.TargetZone);

            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasPendingDesignEntry, Is.True);
            Assert.That(session.PendingDesignEntry.MissionId, Is.EqualTo(LaunchMissionId.TargetZone));
            Assert.That(session.Model.GetCurrentMission(), Is.EqualTo(LaunchMissionId.TargetZone));
            Assert.That(session.Model.GetMission(LaunchMissionId.LowAltitude).Unlocked, Is.True);
            Assert.That(session.Model.GetMission(LaunchMissionId.HighAltitude).Unlocked, Is.True);
            Assert.That(session.Model.GetMission(LaunchMissionId.TargetZone).Unlocked, Is.True);
            Assert.That(session.Model.GetMission(LaunchMissionId.ZoneHold).Unlocked, Is.False);
        }
    }
}
#endif
