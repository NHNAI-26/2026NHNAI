using NUnit.Framework;

namespace Border.Research.Tests
{
    public sealed class EngineInstallPricingTests
    {
        [TestCase(40, 350)]
        [TestCase(50, 362)]
        [TestCase(60, 373)]
        [TestCase(80, 397)]
        [TestCase(100, 420)]
        public void BalancedStats_OnlyAddUpToTwentyPercent(int stat, int expected)
        {
            var state = new EnginePresetState { FuelCapacity = stat, Cooling = stat, MaxOutput = stat, IgnitionReliability = stat };
            Assert.AreEqual(expected, ResearchPrototypeModel.CalculateEngineInstallCost(state));
        }

        [Test]
        public void SingleStatUpgrade_IsSmall_AndCompletionDoesNotAffectPrice()
        {
            var model = new ResearchPrototypeModel();
            var state = model.GetEnginePreset(EnginePresetId.Engine01);
            state.FuelCapacity += 10;
            Assert.AreEqual(353, model.GetEngineInstallCost(EnginePresetId.Engine01));
            state.Completion = 100;
            Assert.AreEqual(353, model.GetEngineInstallCost(EnginePresetId.Engine01));
            Assert.AreEqual(504, ResearchPrototypeModel.CalculateEngineInstallCost(state, 500));
        }

        [Test]
        public void MixedInstallations_ChargeEachPresetsOwnPrice()
        {
            var model = new ResearchPrototypeModel();
            model.CreateNewEnginePreset(out var second);
            var state = model.GetEnginePreset(second);
            state.FuelCapacity = state.Cooling = state.MaxOutput = state.IgnitionReliability = 100;
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            counts[0] = 2;
            counts[1] = 3;
            var entry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, counts, 100, TestVisibility.Private);
            Assert.AreEqual(1960, entry.ReservedInstallCost);
        }
    }
}
