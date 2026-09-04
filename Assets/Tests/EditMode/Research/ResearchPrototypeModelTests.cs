using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Tests
{
    public sealed class ResearchPrototypeModelTests
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
        public void Reset_CreatesTenIdenticalEnginePresets()
        {
            var model = new ResearchPrototypeModel();

            Assert.That(ResearchPrototypeModel.GetEnginePresetConfigs(), Has.Count.EqualTo(ResearchPrototypeModel.MaxEnginePresetCount));
            Assert.That(model.EnginePresets, Has.Length.EqualTo(ResearchPrototypeModel.MaxEnginePresetCount));

            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                Assert.That(config.NormalResearchCost, Is.EqualTo(ResearchPrototypeModel.EngineNormalResearchCost));
                Assert.That(config.FocusedResearchCost, Is.EqualTo(ResearchPrototypeModel.EngineFocusedResearchCost));
                Assert.That(config.InstallCost, Is.EqualTo(ResearchPrototypeModel.EngineInstallCost));
            }

            foreach (EnginePresetState preset in model.EnginePresets)
            {
                Assert.That(preset.Level, Is.EqualTo(0));
                Assert.That(preset.FuelCapacity, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
                Assert.That(preset.Cooling, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
                Assert.That(preset.MaxOutput, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
                Assert.That(preset.IgnitionReliability, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat));
            }
        }

        [Test]
        public void ExecuteEngineResearch_Normal_IncreasesSelectedPresetOnly()
        {
            var model = new ResearchPrototypeModel();
            EnginePresetState selected = model.GetEnginePreset(EnginePresetId.Engine04);
            EnginePresetState untouched = model.GetEnginePreset(EnginePresetId.Engine05);
            int untouchedLevel = untouched.Level;
            int untouchedStat = untouched.Cooling;

            ResearchActionResult result = model.ExecuteEngineResearch(EnginePresetId.Engine04, EngineStatId.Cooling, false, 65);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(selected.Level, Is.EqualTo(1));
            Assert.That(selected.Cooling, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat + 13));
            Assert.That(untouched.Level, Is.EqualTo(untouchedLevel));
            Assert.That(untouched.Cooling, Is.EqualTo(untouchedStat));
            Assert.That(model.Funds, Is.EqualTo(ResearchPrototypeModel.InitialFunds - ResearchPrototypeModel.EngineNormalResearchCost + ResearchPrototypeModel.InitialQuarterlyFunding));
            Assert.That(model.Quarter, Is.EqualTo(2));
            Assert.That(model.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns - 1));
        }

        [Test]
        public void ExecuteEngineResearch_Focused_UsesHighScoreRewardAndLevelGain()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.ExecuteEngineResearch(EnginePresetId.Engine02, EngineStatId.MaxOutput, true, 85);

            EnginePresetState selected = model.GetEnginePreset(EnginePresetId.Engine02);
            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(selected.Level, Is.EqualTo(2));
            Assert.That(selected.MaxOutput, Is.EqualTo(ResearchPrototypeModel.InitialEngineStat + 26));
        }

        [Test]
        public void ExecuteEngineResearch_WhenLevelMaxed_ReturnsEngineLevelMaxed()
        {
            var model = new ResearchPrototypeModel();
            model.GetEnginePreset(EnginePresetId.Engine01).Level = ResearchPrototypeModel.MaxEnginePresetLevel;
            int funds = model.Funds;

            ResearchActionResult result = model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 100);

            Assert.That(result, Is.EqualTo(ResearchActionResult.EngineLevelMaxed));
            Assert.That(model.Funds, Is.EqualTo(funds));
        }

        [Test]
        public void ExecuteResearch_ForRocketOrbitMoon_IsNotAvailable()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.ExecuteResearch(ResearchStageId.Rocket, false);

            Assert.That(result, Is.EqualTo(ResearchActionResult.StageLocked));
            Assert.That(model.GetStage(ResearchStageId.Rocket).Progress, Is.EqualTo(0));
        }

        [Test]
        public void WaitQuarter_KeepsFundingAndQuarterBehavior()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.WaitQuarter();

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(ResearchPrototypeModel.InitialFunds + ResearchPrototypeModel.InitialQuarterlyFunding));
            Assert.That(model.Year, Is.EqualTo(2018));
            Assert.That(model.Quarter, Is.EqualTo(2));
            Assert.That(model.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns - 1));
        }

        [Test]
        public void TryEnterDesign_EngineLevelZero_ReturnsProgressTooLow()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Engine, EnginePresetId.Engine01, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.ProgressTooLow));
        }

        [Test]
        public void TryEnterDesign_WhenReady_DoesNotConsumeState()
        {
            var model = new ResearchPrototypeModel();
            model.ExecuteEngineResearch(EnginePresetId.Engine03, EngineStatId.FuelCapacity, false, 80);
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine03);
            int funds = model.Funds;
            int year = model.Year;
            int quarter = model.Quarter;
            int remainingTurns = model.RemainingTurns;
            int attemptCount = engine.AttemptCount;
            int level = engine.Level;

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Engine, EnginePresetId.Engine03, out ResearchDesignEntryData data);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.Year, Is.EqualTo(year));
            Assert.That(model.Quarter, Is.EqualTo(quarter));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns));
            Assert.That(engine.AttemptCount, Is.EqualTo(attemptCount));
            Assert.That(engine.Level, Is.EqualTo(level));
            Assert.That(data.StageId, Is.EqualTo(ResearchStageId.Engine));
            Assert.That(data.SelectedEnginePresetId, Is.EqualTo(EnginePresetId.Engine03));
            Assert.That(data.LaunchCost, Is.EqualTo(600));
            Assert.That(data.ReservedInstallCost, Is.EqualTo(0));
            Assert.That(data.TargetPathId, Is.Not.Empty);
        }

        [Test]
        public void TryEnterDesign_WhenStageLocked_ReturnsStageLocked()
        {
            var model = new ResearchPrototypeModel();

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Rocket, EnginePresetId.Engine01, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.StageLocked));
        }

        [Test]
        public void TryEnterDesign_WhenLaunchCostMissing_ReturnsNotEnoughFunds()
        {
            var model = new ResearchPrototypeModel();
            model.GetEnginePreset(EnginePresetId.Engine01).Level = 1;
            SetFunds(model, ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).LaunchCost - 1);

            ResearchActionResult result = model.TryEnterDesign(ResearchStageId.Engine, EnginePresetId.Engine01, out _);

            Assert.That(result, Is.EqualTo(ResearchActionResult.NotEnoughFunds));
        }

        [Test]
        public void Visibility_ChangesSuccessChanceByTwentyPoints()
        {
            var model = new ResearchPrototypeModel();
            model.GetEnginePreset(EnginePresetId.Engine01).Level = 1;
            ResearchDesignEntryData publicEntry = model.CreateDesignEntry(ResearchStageId.Engine, EnginePresetId.Engine01, new int[ResearchPrototypeModel.MaxEnginePresetCount], 50, TestVisibility.Public);
            ResearchDesignEntryData privateEntry = model.CreateDesignEntry(ResearchStageId.Engine, EnginePresetId.Engine01, new int[ResearchPrototypeModel.MaxEnginePresetCount], 50, TestVisibility.Private);

            Assert.That(model.CalculateSuccessChance(privateEntry) - model.CalculateSuccessChance(publicEntry), Is.EqualTo(20));
        }

        [Test]
        public void CommitLaunch_WhenReady_ConsumesLaunchAndInstallCostOnlyOnLaunch()
        {
            var model = new ResearchPrototypeModel();
            model.GetEnginePreset(EnginePresetId.Engine01).Level = 1;
            int[] installed = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            installed[(int)EnginePresetId.Engine01] = 2;
            model.GetStage(ResearchStageId.Rocket).Unlocked = true;
            ResearchDesignEntryData entry = model.CreateDesignEntry(ResearchStageId.Rocket, EnginePresetId.Engine01, installed, 70, TestVisibility.Private);
            int funds = model.Funds;
            int quarterlyFunding = model.QuarterlyFunding;
            int remainingTurns = model.RemainingTurns;

            ResearchActionResult result = model.CommitLaunch(entry, out ResearchLaunchResultData launchResult);

            int expectedQuarterlyFunding = Mathf.Clamp(
                quarterlyFunding + launchResult.QuarterlyFundingDelta,
                ResearchPrototypeModel.MinQuarterlyFunding,
                ResearchPrototypeModel.MaxQuarterlyFunding);
            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(launchResult.TotalCost, Is.EqualTo(ResearchPrototypeModel.GetStageConfig(ResearchStageId.Rocket).LaunchCost + ResearchPrototypeModel.EngineInstallCost * 2));
            Assert.That(launchResult.SuccessChance + launchResult.PartialChance + launchResult.FailureChance, Is.EqualTo(100));
            Assert.That(model.Funds, Is.EqualTo(funds - launchResult.TotalCost + launchResult.ImmediateFunding + expectedQuarterlyFunding));
            Assert.That(model.QuarterlyFunding, Is.EqualTo(expectedQuarterlyFunding));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns - 1));
            Assert.That(model.GetStage(ResearchStageId.Rocket).AttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void EngineTest_CGradeOrBetter_UnlocksRocket()
        {
            var model = new ResearchPrototypeModel(1);
            EnginePresetState engine = model.GetEnginePreset(EnginePresetId.Engine01);
            engine.Level = 5;
            engine.FuelCapacity = 100;
            engine.Cooling = 100;
            engine.MaxOutput = 100;
            engine.IgnitionReliability = 100;
            ResearchDesignEntryData entry = model.CreateDesignEntry(ResearchStageId.Engine, EnginePresetId.Engine01, new int[ResearchPrototypeModel.MaxEnginePresetCount], 100, TestVisibility.Private);

            model.CommitLaunch(entry, out ResearchLaunchResultData result);

            Assume.That(result.Grade, Is.LessThanOrEqualTo(ResearchGrade.C));
            Assert.That(model.GetStage(ResearchStageId.Rocket).Unlocked, Is.True);
            Assert.That(engine.HasBestGrade, Is.True);
        }

        [Test]
        public void MiniGameScoring_ClampsAllScoresToValidRange()
        {
            Assert.That(ResearchMiniGameController.CalculateFuelCapacityScore(2f, -2f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateCoolingScore(10, 10, -1f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateMaxOutputScore(2f, -2f), Is.InRange(0, 100));
            Assert.That(ResearchMiniGameController.CalculateIgnitionReliabilityScore(20, 4, -1f), Is.InRange(0, 100));
        }

        [Test]
        public void MiniGameScoring_FuelCapacity_RewardAccuracy()
        {
            int accurate = ResearchMiniGameController.CalculateFuelCapacityScore(0.01f, 0.02f, 0.01f);
            int inaccurate = ResearchMiniGameController.CalculateFuelCapacityScore(0.35f, 0.4f, 0.3f);

            Assert.That(accurate, Is.GreaterThan(inaccurate));
            Assert.That(accurate, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameScoring_Cooling_RewardsCorrectFastInput()
        {
            int good = ResearchMiniGameController.CalculateCoolingScore(4, 0, 0.25f);
            int poor = ResearchMiniGameController.CalculateCoolingScore(1, 3, 1.5f);

            Assert.That(good, Is.GreaterThan(poor));
            Assert.That(good, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameScoring_MaxOutput_RewardsSafeZoneCenter()
        {
            int centered = ResearchMiniGameController.CalculateMaxOutputScoreFromFills(0.35f, 0.6f, 0.85f);
            int outside = ResearchMiniGameController.CalculateMaxOutputScoreFromFills(0.05f, 0.95f, 0.2f);

            Assert.That(centered, Is.GreaterThan(outside));
            Assert.That(centered, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameScoring_IgnitionReliability_RewardsSequenceAccuracy()
        {
            int correct = ResearchMiniGameController.CalculateIgnitionReliabilityScore(9, 9, 0.35f);
            int wrong = ResearchMiniGameController.CalculateIgnitionReliabilityScore(3, 9, 1.4f);

            Assert.That(correct, Is.GreaterThan(wrong));
            Assert.That(correct, Is.GreaterThanOrEqualTo(80));
        }

        [Test]
        public void MiniGameController_ForceComplete_ShowsResultBeforeCallback()
        {
            var host = new GameObject("Mini Game Test Host");
            ResearchMiniGameResult completedResult = default;
            bool completed = false;

            try
            {
                ResearchMiniGameController controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine03, EngineStatId.Cooling, true, result =>
                {
                    completedResult = result;
                    completed = true;
                });

                controller.ForceCompleteForTests(125);

                Assert.That(completed, Is.False);
                Assert.That(controller.IsShowingResult, Is.True);

                controller.ForceDismissForTests();

                Assert.That(completed, Is.True);
                Assert.That(completedResult.PresetId, Is.EqualTo(EnginePresetId.Engine03));
                Assert.That(completedResult.StatId, Is.EqualTo(EngineStatId.Cooling));
                Assert.That(completedResult.Focused, Is.True);
                Assert.That(completedResult.Score, Is.EqualTo(100));
                Assert.That(completedResult.CompletedByTimeout, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameController_DismissesZeroScoreAsValidCompletion()
        {
            var host = new GameObject("Mini Game Test Host");
            ResearchMiniGameResult completedResult = default;
            bool completed = false;

            try
            {
                ResearchMiniGameController controller = host.AddComponent<ResearchMiniGameController>();
                controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, result =>
                {
                    completedResult = result;
                    completed = true;
                });

                controller.ForceCompleteForTests(0);
                controller.ForceDismissForTests();

                Assert.That(completed, Is.True);
                Assert.That(completedResult.Score, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MiniGameStateText_AddsExampleLineWithoutAccumulating()
        {
            string firstFrame = ResearchMiniGameController.FormatStateText("밸브 1/4", true);
            string nextFrame = ResearchMiniGameController.FormatStateText("밸브 1/4", true);

            Assert.That(firstFrame, Is.EqualTo(nextFrame));
            Assert.That(firstFrame.Split('\n'), Has.Length.EqualTo(2));
        }

        [Test]
        public void FlowSession_StoresUpdatesAndClearsPendingDesignEntry()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.Model.GetEnginePreset(EnginePresetId.Engine02).Level = 1;

            ResearchActionResult result = session.TryEnterDesign(ResearchStageId.Engine, EnginePresetId.Engine02, out ResearchDesignEntryData data);
            ResearchDesignEntryData updated = session.Model.CreateDesignEntry(data.StageId, data.SelectedEnginePresetId, data.InstalledEngineCounts, 80, TestVisibility.Public);
            session.UpdatePendingDesignEntry(updated);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasPendingDesignEntry, Is.True);
            Assert.That(session.PendingDesignEntry.DesignFit, Is.EqualTo(80));

            session.ClearPendingDesignEntry();
            Assert.That(session.HasPendingDesignEntry, Is.False);
        }

        [Test]
        public void FlowSession_CommitPendingDesignLaunch_ClearsPendingAndStoresLaunchResult()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.Model.GetEnginePreset(EnginePresetId.Engine01).Level = 1;
            session.TryEnterDesign(ResearchStageId.Engine, EnginePresetId.Engine01, out _);

            ResearchActionResult result = session.CommitPendingDesignLaunch(out ResearchLaunchResultData launchResult);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasPendingDesignEntry, Is.False);
            Assert.That(session.HasLastLaunchResult, Is.True);
            Assert.That(session.LastLaunchResult.Roll, Is.EqualTo(launchResult.Roll));
        }

        [Test]
        public void OperationUI_InitialRender_ShowsTenEnginesAndOnlyEngineStageSelectable()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                Assert.That(FindButton(host.transform, "EngineCard_Engine01").interactable, Is.True);
                Assert.That(FindButton(host.transform, "EngineCard_Engine10").interactable, Is.True);
                Assert.That(FindButton(host.transform, "StageCard_Engine").interactable, Is.True);
                Assert.That(FindButton(host.transform, "StageCard_Rocket").interactable, Is.False);
                Assert.That(FindButton(host.transform, "StageCard_Orbit").interactable, Is.False);
                Assert.That(FindButton(host.transform, "StageCard_Moon").interactable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_ResearchButtonStartsMiniGameAndCompletionUpdatesSelectedEngine()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                FindButton(host.transform, "NormalResearchButton").onClick.Invoke();

                Assert.That(controller.Model.GetEnginePreset(EnginePresetId.Engine01).Level, Is.EqualTo(0));
                Assert.That(controller.GetActiveMiniGameControllerForTests(), Is.Not.Null);

                controller.GetActiveMiniGameControllerForTests().ForceCompleteForTests(65);

                Assert.That(controller.Model.GetEnginePreset(EnginePresetId.Engine01).Level, Is.EqualTo(0));

                controller.GetActiveMiniGameControllerForTests().ForceDismissForTests();

                Assert.That(controller.Model.GetEnginePreset(EnginePresetId.Engine01).Level, Is.EqualTo(1));
                Assert.That(GetText(FindText(host.transform, "SelectedRequirementText")), Does.Contain("설계 진입 가능"));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_UsesFlowSessionAndShowsDesignScreenOnEnterDesign()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.Model.GetEnginePreset(EnginePresetId.Engine01).Level = 1;
            int funds = session.Model.Funds;
            int remainingTurns = session.Model.RemainingTurns;
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                FindButton(host.transform, "EnterDesignButton").onClick.Invoke();

                Assert.That(controller.Model, Is.SameAs(session.Model));
                Assert.That(controller.RequestedScreenName, Is.EqualTo(ResearchFlowSession.DesignScreenName));
                Assert.That(controller.GetActiveDesignControllerForTests(), Is.Not.Null);
                Assert.That(session.HasPendingDesignEntry, Is.True);
                Assert.That(session.Model.Funds, Is.EqualTo(funds));
                Assert.That(session.Model.RemainingTurns, Is.EqualTo(remainingTurns));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void OperationUI_DebugEnterDesignBypassesResearchGateAndShowsDesignScreen()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                controller.EnterDesignDebugForEditor();

                Assert.That(controller.RequestedScreenName, Is.EqualTo(ResearchFlowSession.DesignScreenName));
                Assert.That(controller.GetActiveDesignControllerForTests(), Is.Not.Null);
                Assert.That(session.HasPendingDesignEntry, Is.True);
                Assert.That(session.Model.GetEnginePreset(EnginePresetId.Engine01).Level, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

#endif
        [Test]
        public void DesignScreenController_WithoutPendingData_RequestsResearchReturn()
        {
            var host = new GameObject("Design UI Test Host");

            try
            {
                ResearchDesignScreenController controller = host.AddComponent<ResearchDesignScreenController>();
                controller.InitializeForTests();

                Assert.That(controller.RequestedResearchReturn, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DesignScreenController_ReturnToResearch_ClearsOnlyPendingData()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.Model.GetEnginePreset(EnginePresetId.Engine01).Level = 1;
            session.TryEnterDesign(ResearchStageId.Engine, EnginePresetId.Engine01, out _);
            int funds = session.Model.Funds;
            int remainingTurns = session.Model.RemainingTurns;
            bool returned = false;
            var host = new GameObject("Design UI Test Host");

            try
            {
                ResearchDesignScreenController controller = host.AddComponent<ResearchDesignScreenController>();
                controller.Initialize(session, () => returned = true);

                controller.ReturnToResearch();

                Assert.That(session.HasPendingDesignEntry, Is.False);
                Assert.That(controller.RequestedResearchReturn, Is.True);
                Assert.That(returned, Is.True);
                Assert.That(session.Model.Funds, Is.EqualTo(funds));
                Assert.That(session.Model.RemainingTurns, Is.EqualTo(remainingTurns));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void DesignScreenController_LaunchCommitsResultAndRequestsResearchReturn()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.Model.GetEnginePreset(EnginePresetId.Engine01).Level = 1;
            session.TryEnterDesign(ResearchStageId.Engine, EnginePresetId.Engine01, out _);
            int remainingTurns = session.Model.RemainingTurns;
            bool returned = false;
            var host = new GameObject("Design UI Test Host");

            try
            {
                ResearchDesignScreenController controller = host.AddComponent<ResearchDesignScreenController>();
                controller.Initialize(session, () => returned = true);

                Assert.That(FindButton(host.transform, "LaunchButton").interactable, Is.True);
                ResearchActionResult result = controller.LaunchForTests(out ResearchLaunchResultData launchResult);

                Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
                Assert.That(returned, Is.True);
                Assert.That(controller.RequestedResearchReturn, Is.True);
                Assert.That(session.HasPendingDesignEntry, Is.False);
                Assert.That(session.HasLastLaunchResult, Is.True);
                Assert.That(session.LastLaunchResult.Roll, Is.EqualTo(launchResult.Roll));
                Assert.That(session.Model.RemainingTurns, Is.EqualTo(remainingTurns - 1));
                Assert.That(session.Model.GetEnginePreset(EnginePresetId.Engine01).AttemptCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void SetFunds(ResearchPrototypeModel model, int funds)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(ResearchPrototypeModel).GetField("<Funds>k__BackingField", Flags);
            Assert.That(field, Is.Not.Null);
            field.SetValue(model, funds);
        }

        private static Button FindButton(Transform root, string name)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail($"Button not found: {name}");
            return null;
        }

        private static Component FindText(Transform root, string name)
        {
            foreach (Component text in root.GetComponentsInChildren<Component>(true))
            {
                if (text.name == name && text.GetType().FullName == "TMPro.TextMeshProUGUI")
                {
                    return text;
                }
            }

            Assert.Fail($"Text not found: {name}");
            return null;
        }

        private static string GetText(Component text)
        {
            PropertyInfo property = text.GetType().GetProperty("text");
            Assert.That(property, Is.Not.Null);
            return (string)property.GetValue(text);
        }
    }
}
