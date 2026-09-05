using System.Linq;
using System.Reflection;
using Border.Title;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Tests
{
    public sealed class ResearchCompletionFlowTests
    {
        private GameObject host;
        private ResearchOperationUIController operation;
        private ResearchResultReportController report;
        private ResearchEndingController ending;
        private ResearchBalanceConfigSO testBalance;
        private const string UiFolder = "Assets/03. Prefabs/UI/";

        [SetUp]
        public void SetUp() => ResearchFlowSession.ResetForTests();

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (report != null) Object.DestroyImmediate(report.gameObject);
            if (ending != null) Object.DestroyImmediate(ending.gameObject);
            ResearchFlowSession.ResetForTests();
            if (testBalance != null) Object.DestroyImmediate(testBalance);
        }

        [Test]
        public void PrepareNewGame_WithoutSessionDoesNotCreateOne()
        {
            ResearchFlowSession.PrepareNewGame();
            Assert.That(Object.FindFirstObjectByType<ResearchFlowSession>(), Is.Null);
        }

        [Test]
        public void PrepareNewGame_RebuildsModelAndClearsAllProgress()
        {
            var session = ResearchFlowSession.GetOrCreate();
            ResearchPrototypeModel old = session.Model;
            old.CreateNewEnginePreset(out _);
            old.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, false, 100);
            session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
            session.CommitPendingDesignLaunch(out _);
            session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
            ResearchFlowSession.PrepareNewGame();
            Assert.That(session.Model, Is.Not.SameAs(old));
            Assert.That(session.HasPendingDesignEntry, Is.False);
            Assert.That(session.HasLastLaunchResult, Is.False);
            Assert.That(session.Model.Year, Is.EqualTo(ResearchPrototypeModel.StartYear));
            Assert.That(session.Model.Quarter, Is.EqualTo(ResearchPrototypeModel.StartQuarter));
            Assert.That(session.Model.Funds, Is.EqualTo(ResearchPrototypeModel.InitialFunds));
            Assert.That(session.Model.TotalLaunches, Is.Zero);
            Assert.That(session.Model.ActiveEnginePresetCount, Is.Zero);
            Assert.That(session.Model.GetEnginePreset(EnginePresetId.Engine01).Completion, Is.Zero);
            Assert.That(session.Model.Missions.Count(mission => mission.Unlocked), Is.EqualTo(1));
        }

        [Test]
        public void Reset_RereadsBalanceAndOperationRebindsNewModel()
        {
            var session = ResearchFlowSession.GetOrCreate();
            testBalance = ScriptableObject.CreateInstance<ResearchBalanceConfigSO>();
            SetInt(testBalance, "initialFunds", 4321);
            SetInt(testBalance, "engineNormalResearchCost", 123);
            SetReference(session, "balanceConfig", testBalance);
            session.ResetResearch();
            CreateOperation();
            Assert.That(FindText(host, "Funds").text, Does.Contain("4321"));
            Assert.That(FindButton(host, "NormalResearchButton").GetComponentInChildren<TMP_Text>().text, Does.Contain("123"));
            ResearchPrototypeModel old = session.Model;
            SetInt(testBalance, "initialFunds", 5432);
            SetInt(testBalance, "engineNormalResearchCost", 234);
            Assert.That(old.ConfiguredEngineNormalResearchCost, Is.EqualTo(123));
            FindButton(host, "StatButton_Cooling").onClick.Invoke();
            FindButton(host, "CreateEnginePresetButton").onClick.Invoke();
            operation.ResetResearchForTests();
            Assert.That(operation.Model, Is.SameAs(session.Model).And.Not.SameAs(old));
            Assert.That(operation.Model.Funds, Is.EqualTo(5432));
            Assert.That(operation.SelectedEnginePreset, Is.EqualTo(EnginePresetId.Engine01));
            Assert.That(FindText(host, "SelectedStatText").text, Does.Contain("선택 스탯: " + ResearchPrototypeModel.GetStatDisplayName(EngineStatId.FuelCapacity)));
            Assert.That(FindButton(host, "NormalResearchButton").GetComponentInChildren<TMP_Text>().text, Does.Contain("234"));
        }

        [TestCase("wait")]
        [TestCase("research")]
        [TestCase("launch")]
        public void LastQuarter_AllActionsEndOnConsumedDate(string action)
        {
            var model = new ResearchPrototypeModel();
            model.CreateNewEnginePreset(out _);
            WaitUntilLastQuarter(model);
            if (action == "wait") model.WaitQuarter();
            else if (action == "research") model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, false, 100);
            else model.CommitLaunch(model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, new int[10], 50, TestVisibility.Private), out _);
            Assert.That(model.HasGameEnded, Is.True);
            Assert.That(model.GameWon, Is.False);
            Assert.That(model.FinalYear, Is.EqualTo(ResearchPrototypeModel.EndYear));
            Assert.That(model.FinalQuarter, Is.EqualTo(ResearchPrototypeModel.EndQuarter));
            Assert.That(model.RemainingTurns, Is.Zero);
            int funds = model.Funds;
            model.WaitQuarter();
            Assert.That(model.Funds, Is.EqualTo(funds));
        }

        [Test]
        public void BalanceSo_ResearchAndLaunchRewardsReachModelAndReport()
        {
            var session = ResearchFlowSession.GetOrCreate();
            testBalance = ScriptableObject.CreateInstance<ResearchBalanceConfigSO>();
            var data = new SerializedObject(testBalance);
            var bands = data.FindProperty("normalResearchStatRewards");
            bands.arraySize = 1;
            bands.GetArrayElementAtIndex(0).FindPropertyRelative("minScore").intValue = 0;
            bands.GetArrayElementAtIndex(0).FindPropertyRelative("gain").intValue = 7;
            var rewards = data.FindProperty("launchRewards");
            for (int i = 0; i < rewards.arraySize; i++)
            {
                rewards.GetArrayElementAtIndex(i).FindPropertyRelative("immediateFunding").intValue = 222;
                rewards.GetArrayElementAtIndex(i).FindPropertyRelative("quarterlyFundingDelta").intValue = 77;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            SetReference(session, "balanceConfig", testBalance);
            session.ResetResearch();
            var model = session.Model;
            model.CreateNewEnginePreset(out _);
            int oldStat = model.GetEnginePreset(EnginePresetId.Engine01).Cooling;
            model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, false, 100);
            Assert.That(model.GetEnginePreset(EnginePresetId.Engine01).Cooling, Is.EqualTo(oldStat + 7));
            var entry = model.CreateDesignEntry(model.GetCurrentMission(), EnginePresetId.Engine01, new int[10], 50, TestVisibility.Public);
            session.StoreDesignEntry(entry);
            Assert.That(session.TryBeginPendingDesignLaunch(), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.CompleteActiveLaunch(true, out var result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.ImmediateFunding, Is.EqualTo(222));
            Assert.That(result.QuarterlyFundingDelta, Is.EqualTo(77));
            CreateOperation();
            Assert.That(report.gameObject.activeSelf, Is.True);
            Assert.That(FindText(report.gameObject, "Effects").text, Does.Contain("222").And.Contain("+77"));
        }

        [TestCase(1, true, ResearchGrade.B)]
        [TestCase(7, false, ResearchGrade.C)]
        public void FinalLaunch_LastQuarterPrioritizesBGradeVictory(int seed, bool won, ResearchGrade grade)
        {
            var model = new ResearchPrototypeModel(seed);
            WaitUntilLastQuarter(model);
            PrepareFinalEngine(model);
            var counts = new int[10];
            counts[0] = 1;
            var entry = model.CreateDesignEntry(LaunchMissionId.LowPowerZoneHold, EnginePresetId.Engine01, counts, 100, TestVisibility.Public);
            Assert.That(model.CommitLaunch(entry, out var result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.Grade, Is.EqualTo(grade));
            Assert.That(result.FinalMissionWon, Is.EqualTo(won));
            Assert.That(result.DeadlineMissed, Is.EqualTo(!won));
            Assert.That(model.HasGameEnded, Is.True);
            Assert.That(model.GameWon, Is.EqualTo(won));
        }

        [Test]
        public void EarlyVictory_BlocksAllFurtherActionsAndKeepsEnding()
        {
            var model = new ResearchPrototypeModel();
            var mission = model.GetMission(LaunchMissionId.LowPowerZoneHold);
            mission.HasBestGrade = true;
            mission.BestGrade = ResearchGrade.B;
            model.WaitQuarter();
            int funds = model.Funds;
            int turns = model.RemainingTurns;
            Assert.That(model.DeadlineReached, Is.False);
            Assert.That(model.CreateNewEnginePreset(out _), Is.EqualTo(ResearchActionResult.GameEnded));
            Assert.That(model.ExecuteEngineResearch(EnginePresetId.Engine01, EngineStatId.Cooling, false, 100), Is.EqualTo(ResearchActionResult.GameEnded));
            Assert.That(model.TryEnterDesign(LaunchMissionId.LowAltitude, out _), Is.EqualTo(ResearchActionResult.GameEnded));
            var entry = model.CreateDesignEntry(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, new int[10], 50, TestVisibility.Private);
            Assert.That(model.CommitLaunch(entry, out _), Is.EqualTo(ResearchActionResult.GameEnded));
            Assert.That(model.WaitQuarter(), Is.EqualTo(ResearchActionResult.GameEnded));
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns));
            Assert.That(model.GameWon, Is.True);
        }

        [Test]
        public void Operation_LastResearchShowsFinalFailureReportBeforeEnding()
        {
            CreateOperation();
            operation.Model.CreateNewEnginePreset(out _);
            WaitUntilLastQuarter(operation.Model);
            operation.RefreshForTests();
            FindButton(host, "StartDevelopmentButton").onClick.Invoke();
            var game = operation.GetActiveMiniGameControllerForTests();
            game.ForceCompleteForTests(100);
            Assert.That(ending.gameObject.activeSelf, Is.False);
            Assert.That(operation.Model.HasGameEnded, Is.False);
            game.ForceDismissForTests();
            Assert.That(operation.RequestedScreenName, Is.EqualTo("ResultReport"));
            Assert.That(report.gameObject.activeSelf, Is.True);
            Assert.That(ending.gameObject.activeSelf, Is.False);
            Assert.That(ResearchFlowSession.GetOrCreate().LastLaunchResult.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.FinalFailure));
            Invoke(report, "Respond");
            Assert.That(ending.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Operation_FocusedModeButtonRoutesStartToFocusedResearch()
        {
            CreateOperation();
            operation.Model.CreateNewEnginePreset(out _);
            operation.RefreshForTests();
            FindButton(host, "FocusedResearchButton").onClick.Invoke();
            FindButton(host, "StartDevelopmentButton").onClick.Invoke();
            ResearchMiniGameController game = operation.GetActiveMiniGameControllerForTests();
            Assert.That(FindText(game.gameObject, "Title").text, Does.Contain("집중"));
        }

        [Test]
        public void Operation_LastWaitShowsFinalFailureReportAndRestartReusesScreens()
        {
            CreateOperation();
            WaitUntilLastQuarter(operation.Model);
            FindButton(host, "WaitQuarterButton").onClick.Invoke();
            Assert.That(report.gameObject.activeSelf, Is.True);
            Assert.That(ending.gameObject.activeSelf, Is.False);
            Assert.That(ResearchFlowSession.GetOrCreate().LastLaunchResult.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.FinalFailure));
            ResearchPrototypeModel old = operation.Model;
            Invoke(report, "Respond");
            Button restart = FindButton(ending.gameObject, "RestartButton");
            restart.onClick.Invoke();
            ResearchPrototypeModel fresh = operation.Model;
            restart.onClick.Invoke();
            Assert.That(fresh, Is.Not.SameAs(old));
            Assert.That(operation.Model, Is.SameAs(fresh));
            Assert.That(fresh.HasGameEnded, Is.False);
            Assert.That(fresh.RemainingTurns, Is.EqualTo(ResearchPrototypeModel.MaxTurns));
            Assert.That(operation.RequestedScreenName, Is.EqualTo("Research"));
            Assert.That(ending.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Operation_FinalLaunchShowsResultThenFinalFailureReportBeforeEndingWithoutDuplicateRewards()
        {
            CreateOperation();
            operation.Model.CreateNewEnginePreset(out _);
            WaitUntilLastQuarter(operation.Model);
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
            session.CommitPendingDesignLaunch(out var result);
            int funds = operation.Model.Funds;
            Invoke(operation, "ShowResultReport", result);
            Assert.That(report.gameObject.activeSelf, Is.True);
            Assert.That(ending.gameObject.activeSelf, Is.False);
            Invoke(report, "Respond");
            Invoke(report, "Respond");
            Assert.That(report.gameObject.activeSelf, Is.True);
            Assert.That(ending.gameObject.activeSelf, Is.False);
            Assert.That(session.LastLaunchResult.OutcomeEvent.Id, Is.EqualTo(LaunchOutcomeEventId.FinalFailure));
            Invoke(report, "Respond");
            Assert.That(ending.gameObject.activeSelf, Is.True);
            Assert.That(report.gameObject.activeSelf, Is.False);
            Assert.That(operation.Model.Funds, Is.EqualTo(funds));
            Assert.That(operation.Model.TotalLaunches, Is.EqualTo(1));
            Assert.That(session.HasUnacknowledgedLaunchResult, Is.False);
            operation.ReturnFromDesignScreen();
            Assert.That(ending.gameObject.activeSelf, Is.True);
            Assert.That(report.gameObject.activeSelf, Is.False);
            int callbacks = 0;
            report.Initialize(session, result, () => callbacks++);
            Invoke(report, "Respond");
            Invoke(report, "Respond");
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(operation.Model.Funds, Is.EqualTo(funds));
        }

        [Test]
        public void LaunchResultOverlay_WaitsForDismissalBeforeInvokingStageContinuation()
        {
            CreateOperation();
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
            session.CommitPendingDesignLaunch(out ResearchLaunchResultData result);
            bool continued = false;

            operation.ShowLaunchResultOverlay(result, () => continued = true);

            Assert.That(report.gameObject.activeSelf, Is.True);
            Assert.That(continued, Is.False);
            Invoke(report, "Respond");
            Assert.That(continued, Is.True);
            Assert.That(session.HasUnacknowledgedLaunchResult, Is.False);
            Assert.That(report.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MainScene_HasWiredSessionAndSeparateInactiveOutcomePrefabs()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/00. Scenes/01_Main.unity");
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var flow = all.Select(item => item.GetComponent<ResearchFlowSession>()).Single(item => item != null);
                var research = all.Select(item => item.GetComponent<ResearchOperationUIController>()).Single(item => item != null);
                Assert.That(flow.transform.parent, Is.Null);
                var config = new SerializedObject(flow).FindProperty("balanceConfig").objectReferenceValue;
                Assert.That(AssetDatabase.GetAssetPath(config), Is.EqualTo("Assets/02. ScriptableObjects/Research/ResearchBalanceConfig.asset"));
                foreach (string field in new[] { "resultReport", "endingScreen" })
                {
                    var screen = (Component)new SerializedObject(research).FindProperty(field).objectReferenceValue;
                    Assert.That(screen, Is.Not.Null);
                    Assert.That(screen.gameObject.activeSelf, Is.False);
                    Assert.That(screen.transform.parent, Is.Null);
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(screen), Is.True);
                    Assert.That(screen.GetComponentInChildren<Canvas>(true).sortingOrder, Is.LessThan(90));
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private void CreateOperation()
        {
            host = new GameObject("Research Completion Test");
            report = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UiFolder + "ResearchResultReport.prefab")).GetComponent<ResearchResultReportController>();
            ending = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UiFolder + "ResearchEnding.prefab")).GetComponent<ResearchEndingController>();
            operation = host.AddComponent<ResearchOperationUIController>();
            SetReference(operation, "resultReport", report);
            SetReference(operation, "endingScreen", ending);
            operation.InitializeForTests();
        }

        private static void WaitUntilLastQuarter(ResearchPrototypeModel model)
        {
            while (model.RemainingTurns > 1) model.WaitQuarter();
        }

        private static void PrepareFinalEngine(ResearchPrototypeModel model)
        {
            // 새 게임은 프리셋 0개로 시작한다.
            if (!model.IsEnginePresetUnlocked(EnginePresetId.Engine01)) model.CreateNewEnginePreset(out _);
            var engine = model.GetEnginePreset(EnginePresetId.Engine01);
            engine.FuelCapacity = engine.Cooling = engine.MaxOutput = engine.IgnitionReliability = 100;
            engine.Completion = 100;
            model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).intValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button FindButton(GameObject root, string name) => root.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
        private static TMP_Text FindText(GameObject root, string name) => root.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == name);
        private static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }
}
