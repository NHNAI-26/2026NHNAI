using NUnit.Framework;

namespace Border.Research.Tests
{
    public sealed class EnginePresetNameTests
    {
        [TestCase("abcdefghijklmnop", "abcdefghijkl")]
        [TestCase("가나다라마바사아자차", "가나다라마바사아")]
        [TestCase("ABCD가나다라마바", "ABCD가나다라마")]
        [TestCase("  My Engine  ", "My Engine")]
        [TestCase("\u1100\u1161", "가")]
        [TestCase(" \n\t ", "")]
        public void Normalize_EnforcesMixedLengthWithoutSplittingText(string value, string expected)
        {
            Assert.AreEqual(expected, ResearchPrototypeModel.NormalizeEnginePresetName(value));
        }

        [Test]
        public void Rename_OnlyChangesName_AndBlankPreservesIt()
        {
            var model = new ResearchPrototypeModel();
            int funds = model.Funds;
            int turns = model.RemainingTurns;
            Assert.IsTrue(model.RenameEnginePreset(EnginePresetId.Engine01, "내 엔진"));
            Assert.AreEqual("내 엔진", model.GetEnginePresetName(EnginePresetId.Engine01));
            Assert.AreEqual(funds, model.Funds);
            Assert.AreEqual(turns, model.RemainingTurns);
            Assert.AreEqual(0, model.GetEnginePreset(EnginePresetId.Engine01).Completion);
            Assert.IsFalse(model.RenameEnginePreset(EnginePresetId.Engine01, "   "));
            Assert.AreEqual("내 엔진", model.GetEnginePresetName(EnginePresetId.Engine01));
            Assert.IsFalse(model.RenameEnginePreset(EnginePresetId.Engine02, "locked"));
        }

        [Test]
        public void Reset_RestoresDefaultName_AndCompletedEngineCanBeRenamed()
        {
            var model = new ResearchPrototypeModel();
            model.GetEnginePreset(EnginePresetId.Engine01).Completion = 100;
            Assert.IsTrue(model.RenameEnginePreset(EnginePresetId.Engine01, "Final"));
            model.Reset();
            Assert.AreEqual(ResearchPrototypeModel.GetEnginePresetConfig(EnginePresetId.Engine01).DisplayName,
                model.GetEnginePresetName(EnginePresetId.Engine01));
        }
    }
}
