using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Tests
{
    public sealed class ResearchVisibilityOutcomeTests
    {
        private GameObject dialogObject;
        private GameObject listenerObject;
        private GameObject operationObject;
        private ResearchLaunchOutcomeEventChannelSO channel;

        [SetUp]
        public void SetUp() => ResearchFlowSession.ResetForTests();

        [TearDown]
        public void TearDown()
        {
            if (operationObject != null) Object.DestroyImmediate(operationObject);
            if (dialogObject != null) Object.DestroyImmediate(dialogObject);
            if (listenerObject != null)
            {
                var listener = listenerObject.GetComponent<ResearchLaunchOutcomeListener>();
                typeof(ResearchLaunchOutcomeListener).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(listener, null);
                Object.DestroyImmediate(listenerObject);
            }
            ResearchFlowSession.ResetForTests();
            if (channel != null) Object.DestroyImmediate(channel);
        }

        [TestCase(TestVisibility.Public, true)]
        [TestCase(TestVisibility.Private, true)]
        [TestCase(TestVisibility.Public, false)]
        [TestCase(TestVisibility.Private, false)]
        public void PhysicalResult_UsesEventSettlementOnlyForRegularVisibility(TestVisibility visibility, bool success)
        {
            var model = new ResearchPrototypeModel();
            Assert.That(model.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, visibility, out var entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(entry.Visibility, Is.EqualTo(visibility));
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            counts[0] = 2;
            entry = model.CreateDesignEntry(entry.MissionId, entry.SelectedEnginePresetId, counts, entry.DesignFit, entry.Visibility, entry.LaunchCostPaid);
            Assert.That(model.BeginLaunch(entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(model.CompleteLaunch(success, out var result), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(result.Grade, Is.EqualTo(success ? ResearchGrade.B : ResearchGrade.F));
            Assert.That(result.ImmediateFunding, Is.Zero);
            Assert.That(result.QuarterlyFundingDelta, Is.Zero);
            Assert.That(result.OutcomeEvent, Is.Not.Null);
        }

        [Test]
        public void FinalMission_RequiresPublicVisibility()
        {
            var model = new ResearchPrototypeModel();
            model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
            Assert.That(model.TryEnterDesign(LaunchMissionId.LowPowerZoneHold, EnginePresetId.Engine01, TestVisibility.Public, out var entry), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(entry.Visibility, Is.EqualTo(TestVisibility.Public));

            model = new ResearchPrototypeModel();
            model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
            Assert.That(model.TryEnterDesign(LaunchMissionId.LowPowerZoneHold, EnginePresetId.Engine01, TestVisibility.Private, out _), Is.EqualTo(ResearchActionResult.RequirementNotMet));
            Assert.That(model.LastMessage, Does.Contain("공개 테스트"));
        }

        [Test]
        public void FinalMission_PrivateDesignEntryCannotBeginLaunch()
        {
            var model = new ResearchPrototypeModel();
            model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
            var entry = model.CreateDesignEntry(LaunchMissionId.LowPowerZoneHold,
                EnginePresetId.Engine01, new[] { 1 }, 50, TestVisibility.Private);

            Assert.That(model.BeginLaunch(entry), Is.EqualTo(ResearchActionResult.RequirementNotMet));
            Assert.That(model.LastMessage, Does.Contain("공개 테스트"));
            Assert.That(model.HasActiveLaunch, Is.False);
        }

        [Test]
        public void Dialog_CancelLeavesStateUntouchedAndDefaultsToPrivate()
        {
            var model = new ResearchPrototypeModel();
            var dialog = CreateDialog();
            int calls = 0;
            int funds = model.Funds;
            int turns = model.RemainingTurns;
            int objects = dialogObject.GetComponentsInChildren<Transform>(true).Length;
            dialog.Open(model, model.GetCurrentMission(), _ => { calls++; return ResearchActionResult.Success; });
            Assert.That(Find<Toggle>("PrivateToggle").isOn, Is.True);
            Find<Toggle>("PublicToggle").isOn = true;
            Find<Button>("CancelButton").onClick.Invoke();
            Assert.That(dialog.IsOpen, Is.False);
            Assert.That(model.Funds, Is.EqualTo(funds));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns));
            Assert.That(calls, Is.Zero);
            dialog.Open(model, model.GetCurrentMission(), _ => ResearchActionResult.Success);
            Assert.That(Find<Toggle>("PrivateToggle").isOn, Is.True);
            Assert.That(dialogObject.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
        }

        [Test]
        public void Dialog_DescribesEventDrivenPublicAndPrivateOutcomes()
        {
            var model = new ResearchPrototypeModel();
            var dialog = CreateDialog();

            dialog.Open(model, model.GetCurrentMission(), _ => ResearchActionResult.Success);

            Assert.That(Find<TMP_Text>("PublicDetails").text, Does.Contain("투자 혹은 연구비 지원"));
            Assert.That(Find<TMP_Text>("PublicDetails").text, Does.Contain("연구비가 줄어들수도"));
            Assert.That(Find<TMP_Text>("PrivateDetails").text, Does.Contain("성공 보수가 적지만"));
            Assert.That(Find<TMP_Text>("PrivateDetails").text, Does.Contain("위험 부담도 적습니다"));
            Assert.That(Find<TMP_Text>("PublicDetails").text, Does.Not.Contain("보상 ×"));
            Assert.That(Find<TMP_Text>("PrivateDetails").text, Does.Not.Contain("실패 시 분기 연구비"));
        }

        [Test]
        public void Dialog_ReopenForNextMission_RefreshesMissionTitle()
        {
            var model = new ResearchPrototypeModel();
            var dialog = CreateDialog();

            dialog.Open(model, LaunchMissionId.LowAltitude, _ => ResearchActionResult.Success);
            Assert.That(Find<TMP_Text>("Mission").text, Is.EqualTo("MISSION 1 : 낮은 고도 도달"));

            dialog.Hide();
            dialog.Open(model, LaunchMissionId.HighAltitude, _ => ResearchActionResult.Success);

            Assert.That(Find<TMP_Text>("Mission").text, Is.EqualTo("MISSION 2 : 높은 고도 도달"));
        }

        [Test]
        public void Dialog_FinalMissionDefaultsToPublicAndExplainsPublicRequirement()
        {
            var model = new ResearchPrototypeModel();
            model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
            var dialog = CreateDialog();

            dialog.Open(model, LaunchMissionId.LowPowerZoneHold, _ => ResearchActionResult.Success);

            Assert.That(Find<Toggle>("PublicToggle").isOn, Is.True);
            Assert.That(Find<Toggle>("PrivateToggle").isOn, Is.False);
            Assert.That(Find<TMP_Text>("PublicDetails").text, Does.Contain("마지막 미션은 공개 테스트가 필수"));
            Assert.That(Find<TMP_Text>("PrivateDetails").text, Does.Contain("비공개 테스트로는 미션을 진행할 수 없습니다"));
        }

        [Test]
        public void Operation_FinalMissionOpensChoiceDialog()
        {
            var session = ResearchFlowSession.GetOrCreate();
            session.Model.GetMission(LaunchMissionId.LowPowerZoneHold).Unlocked = true;
            operationObject = new GameObject("Final Mission Entry Test");
            var operation = operationObject.AddComponent<ResearchOperationUIController>();
            var dialog = CreateDialog();
            SetReference(operation, "visibilityDialog", dialog);
            operation.InitializeForTests();
            operationObject.GetComponentsInChildren<Button>(true).Single(button => button.name == "EnterDesignButton").onClick.Invoke();
            Assert.That(operation.IsTransitioningToDesignForTests(), Is.False);
            Assert.That(dialog.IsOpen, Is.True);
        }

        [Test]
        public void MainScene_HasDialogAndMatchingOutcomeChannel()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/00. Scenes/01_Main.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                var session = roots.SelectMany(root => root.GetComponentsInChildren<ResearchFlowSession>(true)).Single();
                var operation = roots.SelectMany(root => root.GetComponentsInChildren<ResearchOperationUIController>(true)).Single();
                var listener = roots.SelectMany(root => root.GetComponentsInChildren<ResearchLaunchOutcomeListener>(true)).Single();
                var source = new SerializedObject(session).FindProperty("outcomeChannel").objectReferenceValue;
                Assert.That(source, Is.Not.Null);
                Assert.That(new SerializedObject(listener).FindProperty("channel").objectReferenceValue, Is.SameAs(source));
                var dialog = (ResearchTestVisibilityDialog)new SerializedObject(operation).FindProperty("visibilityDialog").objectReferenceValue;
                Assert.That(dialog, Is.Not.Null);
                Assert.That(dialog.gameObject.activeSelf, Is.False);
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(dialog), Is.True);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void Dialog_PublicConfirmChargesOnce()
        {
            var model = new ResearchPrototypeModel();
            var dialog = CreateDialog();
            int funds = model.Funds;
            int turns = model.RemainingTurns;
            TestVisibility selected = TestVisibility.Private;
            dialog.Open(model, model.GetCurrentMission(), visibility =>
            {
                selected = visibility;
                return model.TryEnterDesign(model.GetCurrentMission(), EnginePresetId.Engine01, visibility, out _);
            });
            Find<Toggle>("PublicToggle").isOn = true;
            Button confirm = Find<Button>("ConfirmButton");
            confirm.onClick.Invoke();
            confirm.onClick.Invoke();
            Assert.That(selected, Is.EqualTo(TestVisibility.Public));
            Assert.That(model.Funds, Is.EqualTo(funds - model.GetConfiguredMissionConfig(model.GetCurrentMission()).LaunchCost));
            Assert.That(model.RemainingTurns, Is.EqualTo(turns));
            Assert.That(dialog.IsOpen, Is.False);
        }

        [Test]
        public void Dialog_FailedEntryStaysOpenAndDoesNotCharge()
        {
            var model = new ResearchPrototypeModel();
            var dialog = CreateDialog();
            while (!model.DeadlineReached) model.WaitQuarter();
            int funds = model.Funds;
            dialog.Open(model, model.GetCurrentMission(), visibility => model.TryEnterDesign(model.GetCurrentMission(), EnginePresetId.Engine01, visibility, out _));
            Find<Button>("ConfirmButton").onClick.Invoke();
            Assert.That(dialog.IsOpen, Is.True);
            Assert.That(Find<Button>("ConfirmButton").interactable, Is.True);
            Assert.That(Find<TMP_Text>("Error").text, Is.Not.Empty);
            Assert.That(model.Funds, Is.EqualTo(funds));
        }

        [Test]
        public void Outcome_QueuedUntilReturnAndPublishedOnceEvenAfterAcknowledgement()
        {
            var session = ResearchFlowSession.GetOrCreate();
            channel = ScriptableObject.CreateInstance<ResearchLaunchOutcomeEventChannelSO>();
            SetReference(session, "outcomeChannel", channel);
            int calls = 0;
            ResearchLaunchOutcomeData observed = default;
            channel.OnEventRaised += outcome => { calls++; observed = outcome; };
            session.TryEnterDesign(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, TestVisibility.Public, out _);
            session.TryBeginPendingDesignLaunch();
            session.CompleteActiveLaunch(false, "자폭", out _);
            session.CompleteActiveLaunch(false, "중복", out _);
            Assert.That(calls, Is.Zero);
            session.AcknowledgeLaunchResult();
            Assert.That(session.HasPendingOutcomeNotification, Is.True);
            session.PublishPendingLaunchOutcome();
            session.PublishPendingLaunchOutcome();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(observed.Reason, Is.EqualTo("자폭"));
            Assert.That(observed.Result.Visibility, Is.EqualTo(TestVisibility.Public));
            Assert.That(observed.Result.Grade, Is.EqualTo(ResearchGrade.F));
        }

        [Test]
        public void Outcome_ResetDropsOldNotificationAndMissingListenerIsAllowed()
        {
            var session = ResearchFlowSession.GetOrCreate();
            session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
            session.TryBeginPendingDesignLaunch();
            session.CompleteActiveLaunch(true, out _);
            session.ResetResearch();
            Assert.That(session.HasPendingOutcomeNotification, Is.False);
            Assert.DoesNotThrow(session.PublishPendingLaunchOutcome);
            session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
            session.TryBeginPendingDesignLaunch();
            session.CompleteActiveLaunch(true, out _);
            Assert.DoesNotThrow(session.PublishPendingLaunchOutcome);
            Assert.That(session.HasPendingOutcomeNotification, Is.False);
        }

        [TestCase(ResearchGrade.S, 0)]
        [TestCase(ResearchGrade.B, 0)]
        [TestCase(ResearchGrade.C, 1)]
        [TestCase(ResearchGrade.F, 2)]
        public void OutcomeListener_RoutesGradeToMatchingUnityEvent(ResearchGrade grade, int expected)
        {
            channel = ScriptableObject.CreateInstance<ResearchLaunchOutcomeEventChannelSO>();
            listenerObject = new GameObject("Outcome Listener Test");
            var listener = listenerObject.AddComponent<ResearchLaunchOutcomeListener>();
            SetReference(listener, "channel", channel);
            typeof(ResearchLaunchOutcomeListener).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(listener, null);
            int[] counts = new int[3];
            listener.Succeeded.AddListener(() => counts[0]++);
            listener.PartiallySucceeded.AddListener(() => counts[1]++);
            listener.Failed.AddListener(() => counts[2]++);
            var result = new ResearchLaunchResultData(LaunchMissionId.LowAltitude, EnginePresetId.Engine01, 2020, 1, 50, 0, TestVisibility.Public, 50, 40, 40, 0, 0, 0, 0, grade, 0, 0, false, false);
            channel.RaiseEvent(new ResearchLaunchOutcomeData(result, "sample"));
            Assert.That(counts.Sum(), Is.EqualTo(1));
            Assert.That(counts[expected], Is.EqualTo(1));
            Assert.That(listener.LastOutcome.Result.Grade, Is.EqualTo(grade));
        }

        private ResearchTestVisibilityDialog CreateDialog()
        {
            dialogObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchTestVisibilityDialog.prefab"));
            return dialogObject.GetComponent<ResearchTestVisibilityDialog>();
        }

        private T Find<T>(string name) where T : Component => dialogObject.GetComponentsInChildren<T>(true).Single(item => item.name == name);

        private static void SetReference(Object target, string field, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
