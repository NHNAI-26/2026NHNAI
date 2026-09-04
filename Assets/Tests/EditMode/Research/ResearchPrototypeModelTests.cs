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
        public void Reset_UnlocksOnlyEngine()
        {
            var model = new ResearchPrototypeModel();

            Assert.That(model.GetStage(ResearchStageId.Engine).Unlocked, Is.True);
            Assert.That(model.GetStage(ResearchStageId.Rocket).Unlocked, Is.False);
            Assert.That(model.GetStage(ResearchStageId.Orbit).Unlocked, Is.False);
            Assert.That(model.GetStage(ResearchStageId.Moon).Unlocked, Is.False);
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

        [Test]
        public void Unlock_KeepsPreviousStageAvailableAfterNextStageOpens()
        {
            var model = new ResearchPrototypeModel();
            ResearchStageState engine = model.GetStage(ResearchStageId.Engine);
            engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).UnlockProgressRequirement;
            engine.HasBestGrade = true;
            engine.BestGrade = ResearchGrade.C;

            model.ExecuteResearch(ResearchStageId.Engine, false);

            Assert.That(model.GetStage(ResearchStageId.Engine).Unlocked, Is.True);
            Assert.That(model.GetStage(ResearchStageId.Rocket).Unlocked, Is.True);
        }

        [Test]
        public void FlowSession_TryEnterDesign_StoresPendingDataWithoutConsumingResearchState()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            ResearchPrototypeModel model = session.Model;
            ResearchStageState engine = model.GetStage(ResearchStageId.Engine);
            engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;

            int funds = model.Funds;
            int year = model.Year;
            int quarter = model.Quarter;
            int remainingTurns = model.RemainingTurns;
            int attemptCount = engine.AttemptCount;

            ResearchActionResult result = session.TryEnterDesign(ResearchStageId.Engine, out ResearchDesignEntryData data);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasPendingDesignEntry, Is.True);
            Assert.That(session.PendingDesignEntry.StageId, Is.EqualTo(data.StageId));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.Year, Is.EqualTo(year));
            Assert.That(model.Quarter, Is.EqualTo(quarter));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns));
            Assert.That(engine.AttemptCount, Is.EqualTo(attemptCount));
        }

        [Test]
        public void FlowSession_ClearPendingDesignEntry_PreservesResearchState()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            ResearchPrototypeModel model = session.Model;
            ResearchStageState engine = model.GetStage(ResearchStageId.Engine);
            engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;
            session.TryEnterDesign(ResearchStageId.Engine, out _);

            int funds = model.Funds;
            int year = model.Year;
            int quarter = model.Quarter;
            int remainingTurns = model.RemainingTurns;
            int progress = engine.Progress;

            session.ClearPendingDesignEntry();

            Assert.That(session.HasPendingDesignEntry, Is.False);
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.Year, Is.EqualTo(year));
            Assert.That(model.Quarter, Is.EqualTo(quarter));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns));
            Assert.That(engine.Progress, Is.EqualTo(progress));
        }

        [Test]
        public void CommitLaunch_WhenReady_ConsumesLaunchCostQuarterAndAttempt()
        {
            var model = new ResearchPrototypeModel();
            ResearchStageConfig config = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine);
            ResearchStageState engine = model.GetStage(ResearchStageId.Engine);
            engine.Progress = config.MinimumTestProgress;
            model.TryEnterDesign(ResearchStageId.Engine, out ResearchDesignEntryData entry);
            int funds = model.Funds;
            int quarterlyFunding = model.QuarterlyFunding;
            int remainingTurns = model.RemainingTurns;
            int attemptCount = engine.AttemptCount;

            ResearchActionResult result = model.CommitLaunch(entry, out ResearchLaunchResultData launchResult);

            int expectedQuarterlyFunding = Mathf.Clamp(
                quarterlyFunding + launchResult.QuarterlyFundingDelta,
                ResearchPrototypeModel.MinQuarterlyFunding,
                ResearchPrototypeModel.MaxQuarterlyFunding);
            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(launchResult.StageId, Is.EqualTo(ResearchStageId.Engine));
            Assert.That(launchResult.SuccessChance + launchResult.PartialChance + launchResult.FailureChance, Is.EqualTo(100));
            Assert.That(model.Funds, Is.EqualTo(funds - config.TestCost + launchResult.ImmediateFunding + expectedQuarterlyFunding));
            Assert.That(model.QuarterlyFunding, Is.EqualTo(expectedQuarterlyFunding));
            Assert.That(model.RemainingTurns, Is.EqualTo(remainingTurns - 1));
            Assert.That(engine.AttemptCount, Is.EqualTo(attemptCount + 1));
            Assert.That(engine.HasBestGrade, Is.True);
            Assert.That(engine.BestGrade, Is.EqualTo(launchResult.Grade));
        }

        [Test]
        public void FlowSession_CommitPendingDesignLaunch_ClearsPendingAndStoresLaunchResult()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            ResearchStageState engine = session.Model.GetStage(ResearchStageId.Engine);
            engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;
            session.TryEnterDesign(ResearchStageId.Engine, out _);

            ResearchActionResult result = session.CommitPendingDesignLaunch(out ResearchLaunchResultData launchResult);

            Assert.That(result, Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.HasPendingDesignEntry, Is.False);
            Assert.That(session.HasLastLaunchResult, Is.True);
            Assert.That(session.LastLaunchResult.Grade, Is.EqualTo(launchResult.Grade));
        }

        [Test]
        public void OperationUI_InitialRender_ShowsOnlyEngineAsSelectable()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                Assert.That(FindButton(host.transform, "StageCard_Engine").interactable, Is.True);
                Assert.That(FindButton(host.transform, "StageCard_Rocket").interactable, Is.False);
                Assert.That(FindButton(host.transform, "StageCard_Orbit").interactable, Is.False);
                Assert.That(FindButton(host.transform, "StageCard_Moon").interactable, Is.False);
                Assert.That(GetText(FindText(host.transform, "StageCard_Rocket", "Content", "Progress")), Does.Contain("Engine"));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void OperationUI_Refresh_UpdatesSelectedStagePanelAndActionState()
        {
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();
                ResearchStageState engine = controller.Model.GetStage(ResearchStageId.Engine);
                engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;
                controller.RefreshForTests();

                Assert.That(GetText(FindText(host.transform, "SelectedStageTitle")), Does.Contain("Engine"));
                Assert.That(GetText(FindText(host.transform, "SelectedStageRequirement")), Does.Contain("설계 진입 가능"));
                Assert.That(FindButton(host.transform, "EnterDesignButton").interactable, Is.True);
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
            ResearchStageState engine = session.Model.GetStage(ResearchStageId.Engine);
            engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;
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
            ResearchPrototypeModel model = session.Model;
            ResearchStageState engine = model.GetStage(ResearchStageId.Engine);
            int attemptCount = engine.AttemptCount;
            var host = new GameObject("Research UI Test Host");

            try
            {
                ResearchOperationUIController controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();

                controller.EnterDesignDebugForEditor();

                ResearchStageConfig engineConfig = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine);
                Assert.That(controller.RequestedScreenName, Is.EqualTo(ResearchFlowSession.DesignScreenName));
                Assert.That(controller.GetActiveDesignControllerForTests(), Is.Not.Null);
                Assert.That(session.HasPendingDesignEntry, Is.True);
                Assert.That(model.Year, Is.EqualTo(ResearchPrototypeModel.StartYear));
                Assert.That(model.Quarter, Is.EqualTo(ResearchPrototypeModel.StartQuarter));
                Assert.That(engine.Progress, Is.GreaterThanOrEqualTo(engineConfig.MinimumTestProgress));
                Assert.That(model.Funds, Is.GreaterThanOrEqualTo(engineConfig.TestCost));
                Assert.That(engine.AttemptCount, Is.EqualTo(attemptCount));
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
            ResearchStageState engine = session.Model.GetStage(ResearchStageId.Engine);
            engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;
            session.TryEnterDesign(ResearchStageId.Engine, out _);
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
            ResearchStageState engine = session.Model.GetStage(ResearchStageId.Engine);
            engine.Progress = ResearchPrototypeModel.GetStageConfig(ResearchStageId.Engine).MinimumTestProgress;
            session.TryEnterDesign(ResearchStageId.Engine, out _);
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
                Assert.That(engine.AttemptCount, Is.EqualTo(1));
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

        private static Component FindText(Transform root, params string[] path)
        {
            Transform current = root;
            foreach (string segment in path)
            {
                current = FindChild(current, segment);
                Assert.That(current, Is.Not.Null, $"Transform not found: {segment}");
            }

            Component text = null;
            foreach (Component component in current.GetComponents<Component>())
            {
                if (component.GetType().FullName == "TMPro.TextMeshProUGUI")
                {
                    text = component;
                    break;
                }
            }

            Assert.That(text, Is.Not.Null, $"TMP text not found at: {string.Join("/", path)}");
            return text;
        }

        private static string GetText(Component text)
        {
            PropertyInfo property = text.GetType().GetProperty("text");
            Assert.That(property, Is.Not.Null);
            return (string)property.GetValue(text);
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
