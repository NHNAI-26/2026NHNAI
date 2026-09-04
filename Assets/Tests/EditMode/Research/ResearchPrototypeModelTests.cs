using System.Reflection;
using NUnit.Framework;

namespace Border.Research.Tests
{
    public sealed class ResearchPrototypeModelTests
    {
        [Test]
        public void TryEnterDesign_WhenReady_DoesNotConsumeResearchState()
        {
            var model = new ResearchPrototypeModel();
            ResearchStageState stage = model.GetStage(ResearchStageId.Engine);
            stage.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;

            int funds = model.Funds;
            int year = model.Year;
            int quarter = model.Quarter;
            int remainingTurns = model.RemainingTurns;
            int attemptCount = stage.AttemptCount;
            int progress = stage.Progress;

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Engine, out ResearchDesignEntryData data);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.Year, Is.EqualTo(year));
            Assert.That(model.Quarter, Is.EqualTo(quarter));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns));
            Assert.That(stage.AttemptCount, Is.EqualTo(attemptCount));
            Assert.That(stage.Progress, Is.EqualTo(progress));
            Assert.That(data.StageId, Is.EqualTo(ResearchStageId.Engine));
            Assert.That(data.Year, Is.EqualTo(year));
            Assert.That(data.Quarter, Is.EqualTo(quarter));
            Assert.That(data.CurrentProgress, Is.EqualTo(progress));
            Assert.That(data.TargetPathId, Is.Not.Empty);
        }

        [Test]
        public void TryEnterDesign_WhenStageLocked_ReturnsStageLocked()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Rocket, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.StageLocked));
        }

        [Test]
        public void TryEnterDesign_WhenProgressTooLow_ReturnsProgressTooLow()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Engine, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.ProgressTooLow));
        }

        [Test]
        public void TryEnterDesign_WhenLaunchCostMissing_ReturnsNotEnoughFunds()
        {
            var model = new ResearchPrototypeModel();
            ResearchStageState stage = model.GetStage(ResearchStageId.Engine);
            ResearchStageConfig config = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine);
            stage.Progress = config.MinimumTestProgress;
            SetFunds(model, config.TestCost - 1);

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Engine, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.NotEnoughFunds));
        }

        [Test]
        public void ExecuteResearch_KeepsExistingProgressAndQuarterBehavior()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.ExecuteResearch(ResearchStageId.Engine, false);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.GetStage(ResearchStageId.Engine).Progress, Is.EqualTo(ResearchPrototypeModel.NormalResearchGain));
            Assert.That(model.Funds, Is.EqualTo(ResearchPrototypeModel.InitialFunds
                - ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).NormalResearchCost
                + ResearchPrototypeModel.InitialQuarterlyFunding));
            Assert.That(model.Year, Is.EqualTo(2018));
            Assert.That(model.Quarter, Is.EqualTo(2));
            Assert.That(model.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns - 1));
        }

        [Test]
        public void ExecuteFocusedResearch_KeepsExistingProgressGain()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.ExecuteResearch(ResearchStageId.Engine, true);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.GetStage(ResearchStageId.Engine).Progress, Is.EqualTo(ResearchPrototypeModel.FocusedResearchGain));
        }

        [Test]
        public void WaitQuarter_KeepsExistingFundingAndQuarterBehavior()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.WaitQuarter();

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(ResearchPrototypeModel.InitialFunds + ResearchPrototypeModel.InitialQuarterlyFunding));
            Assert.That(model.Year, Is.EqualTo(2018));
            Assert.That(model.Quarter, Is.EqualTo(2));
            Assert.That(model.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns - 1));
        }

        private static void SetFunds(ResearchPrototypeModel model, int funds)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(ResearchPrototypeModel).GetField("<Funds>k__BackingField", Flags);
            Assert.That(field, Is.Not.Null);
            field.SetValue(model, funds);
        }
    }
}
