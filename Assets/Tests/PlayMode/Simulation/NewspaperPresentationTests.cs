#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using Border.Research;
using Border.UI;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Simulation.Tests
{
    public sealed class NewspaperPresentationTests
    {
        [UnityTest]
        public IEnumerator NewspaperRevealsContentsInOrder_WithoutChangingLayoutOrKeepingOldContent()
        {
            ResearchFlowSession.ResetForTests();
            yield return null;
            GameObject host = null;
            try
            {
                var session = ResearchFlowSession.GetOrCreate();
                // 새 게임은 프리셋 0개로 시작한다.
                session.Model.CreateNewEnginePreset(out _);
                session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
                session.TryBeginPendingDesignLaunch();
                session.SetLaunchPhoto(new Texture2D(4, 3), session.LaunchPhotoGeneration);
                session.CompleteActiveLaunch(true, out var result);
                host = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchResultReport.prefab"));
                var report = host.GetComponent<ResearchResultReportController>();
                var newspaper = host.GetComponentInChildren<NewspaperReveal>(true);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = typeof(NewspaperReveal);
                type.GetField("flyDuration", flags).SetValue(newspaper, 0.1f);
                type.GetField("settleDuration", flags).SetValue(newspaper, 0.1f);
                type.GetField("headlineCharacterSeconds", flags).SetValue(newspaper, 0.02f);
                type.GetField("articleCharacterSeconds", flags).SetValue(newspaper, 0.005f);
                type.GetField("resultLineSeconds", flags).SetValue(newspaper, 0.1f);
                type.GetField("sectionPauseSeconds", flags).SetValue(newspaper, 0.05f);
                int shown = 0;
                newspaper.OnShown.AddListener(() => shown++);
                report.Initialize(session, result, null);
                var title = (TMP_Text)type.GetField("headlineText", flags).GetValue(newspaper);
                var body = (TMP_Text)type.GetField("articleText", flags).GetValue(newspaper);
                var effects = (TMP_Text)type.GetField("effectsText", flags).GetValue(newspaper);
                var photo = (RawImage)type.GetField("photoImage", flags).GetValue(newspaper);
                var timeline = (Sequence)type.GetField("sequence", flags).GetValue(newspaper);
                timeline.SetUpdate(UpdateType.Manual, true);
                float simulatedTime = 0f;
                void AdvanceTo(float time)
                {
                    float delta = time - simulatedTime;
                    DOTween.ManualUpdate(delta, delta);
                    simulatedTime = time;
                }
                Assert.That(title.maxVisibleCharacters, Is.Zero);
                Assert.That(body.maxVisibleCharacters, Is.Zero);
                Assert.That(effects.maxVisibleCharacters, Is.Zero);
                Assert.That(photo.gameObject.activeSelf, Is.False);
                Assert.That(shown, Is.Zero);
                Vector2 bodySize = body.rectTransform.rect.size;
                float titleDuration = title.textInfo.characterCount * 0.02f;
                AdvanceTo(0.2f + titleDuration * 0.5f);
                Assert.That(title.maxVisibleCharacters, Is.InRange(1, title.textInfo.characterCount - 1));
                Assert.That(body.maxVisibleCharacters, Is.Zero);
                Assert.That(photo.gameObject.activeSelf, Is.False);
                float bodyStart = 0.2f + titleDuration + 0.05f;
                float bodyDuration = body.textInfo.characterCount * 0.005f;
                AdvanceTo(bodyStart + bodyDuration * 0.5f);
                Assert.That(photo.gameObject.activeSelf, Is.True);
                Assert.That(body.maxVisibleCharacters, Is.InRange(1, body.textInfo.characterCount - 1));
                Assert.That(effects.maxVisibleCharacters, Is.Zero);
                Assert.That(body.rectTransform.rect.size, Is.EqualTo(bodySize));
                float resultStart = bodyStart + bodyDuration + 0.05f;
                AdvanceTo(resultStart + 0.05f);
                int firstLine = effects.maxVisibleCharacters;
                Assert.That(firstLine, Is.EqualTo(effects.text.IndexOf('\n') + 1));
                AdvanceTo(resultStart + 0.15f);
                Assert.That(effects.maxVisibleCharacters, Is.GreaterThan(firstLine));
                timeline.Complete(true);
                Assert.That(shown, Is.EqualTo(1));
                Assert.That(newspaper.IsAnimating, Is.False);
                Assert.That(effects.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
                report.Initialize(session, result, null);
                Assert.That(title.maxVisibleCharacters, Is.Zero);
                Assert.That(body.maxVisibleCharacters, Is.Zero);
                Assert.That(effects.maxVisibleCharacters, Is.Zero);
                Assert.That(photo.gameObject.activeSelf, Is.False);
                report.Hide();
                Assert.That(newspaper.IsAnimating, Is.False);
                Assert.That(shown, Is.EqualTo(1));
            }
            finally
            {
                if (host != null) Object.Destroy(host);
                ResearchFlowSession.ResetForTests();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SplashdownStageCompletion_PreservesResultAndPhotoForNewspaper()
        {
            ResearchFlowSession.ResetForTests();
            yield return null;
            GameObject stageObject = null;
            GameObject rocketObject = null;
            GameObject reportObject = null;
            try
            {
                var session = ResearchFlowSession.GetOrCreate();
                // 새 게임은 프리셋 0개로 시작한다.
                session.Model.CreateNewEnginePreset(out _);
                session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
                session.TryBeginPendingDesignLaunch();
                var photo = new Texture2D(4, 3);
                session.SetLaunchPhoto(photo, session.LaunchPhotoGeneration);
                rocketObject = new GameObject("Splashdown result test");
                var mission = rocketObject.AddComponent<LaunchMissionController>();
                mission.Initialize(LaunchMissionId.LowAltitude, () => true, _ => { });
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var evaluator = (LaunchMissionEvaluator)typeof(LaunchMissionController).GetField("evaluator", flags).GetValue(mission);
                evaluator.Step(4f, 0f, 0f, 0f, 0f, 0f, hasSplashed: true);
                Assert.That(mission.TerminationReason, Is.EqualTo(LaunchTerminationReason.Splashdown));
                stageObject = new GameObject("Splashdown stage test");
                var stage = stageObject.AddComponent<SimulationStageHost>();
                typeof(SimulationStageHost).GetField("mission", flags).SetValue(stage, mission);
                typeof(SimulationStageHost).GetField("launchResultHoldSeconds", flags).SetValue(stage, 60f);
                var complete = typeof(SimulationStageHost).GetMethod("CompleteLaunch", flags);
                complete.Invoke(stage, new object[] { false });
                yield return null;
                Assert.That(session.HasUnacknowledgedLaunchResult, Is.True);
                Assert.That(session.LastLaunchResult.TerminationReason, Is.EqualTo(LaunchTerminationReason.Splashdown));
                Assert.That(session.LaunchPhoto, Is.SameAs(photo));
                Assert.That(photo != null, Is.True);
                int funds = session.Model.Funds;
                complete.Invoke(stage, new object[] { false });
                Assert.That(session.Model.Funds, Is.EqualTo(funds));
                reportObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchResultReport.prefab"));
                reportObject.GetComponent<ResearchResultReportController>().Initialize(session, session.LastLaunchResult, session.AcknowledgeLaunchResult);
                Assert.That(reportObject.GetComponentInChildren<NewspaperReveal>(true).IsShowing, Is.True);
            }
            finally
            {
                if (reportObject != null) Object.Destroy(reportObject);
                if (stageObject != null) Object.Destroy(stageObject);
                if (rocketObject != null) Object.Destroy(rocketObject);
                ResearchFlowSession.ResetForTests();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewspaperClosesOnceAfterIntro_AndReleasesPhotoWithoutAnotherReward()
        {
            ResearchFlowSession.ResetForTests();
            yield return null;
            GameObject host = null;
            try
            {
                var session = ResearchFlowSession.GetOrCreate();
                // 새 게임은 프리셋 0개로 시작한다.
                session.Model.CreateNewEnginePreset(out _);
                session.TryEnterDesign(LaunchMissionId.LowAltitude, out _);
                session.TryBeginPendingDesignLaunch();
                var photo = new Texture2D(4, 3);
                session.SetLaunchPhoto(photo, session.LaunchPhotoGeneration);
                session.CompleteActiveLaunch(true, LaunchTerminationReason.Succeeded.ToString(), LaunchTerminationReason.Succeeded, out var result);
                int funds = session.Model.Funds;
                int turns = session.Model.RemainingTurns;
                host = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchResultReport.prefab"));
                var report = host.GetComponent<ResearchResultReportController>();
                var newspaper = host.GetComponentInChildren<NewspaperReveal>(true);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(NewspaperReveal).GetField("flyDuration", flags).SetValue(newspaper, 0.02f);
                typeof(NewspaperReveal).GetField("settleDuration", flags).SetValue(newspaper, 0.02f);
                typeof(NewspaperReveal).GetField("headlineCharacterSeconds", flags).SetValue(newspaper, 0f);
                typeof(NewspaperReveal).GetField("articleCharacterSeconds", flags).SetValue(newspaper, 0f);
                typeof(NewspaperReveal).GetField("resultLineSeconds", flags).SetValue(newspaper, 0f);
                typeof(NewspaperReveal).GetField("sectionPauseSeconds", flags).SetValue(newspaper, 0f);
                int closed = 0;
                report.Initialize(session, result, () => { closed++; session.AcknowledgeLaunchResult(); });
                newspaper.Hide();
                Assert.That(newspaper.IsShowing, Is.True);
                Assert.That(closed, Is.Zero);
                for (int i = 0; i < 60 && newspaper.IsAnimating; i++) yield return null;
                Assert.That(newspaper.IsAnimating, Is.False);
                newspaper.Hide();
                newspaper.Hide();
                for (int i = 0; i < 60 && newspaper.IsAnimating; i++) yield return null;
                yield return null;
                Assert.That(closed, Is.EqualTo(1));
                Assert.That(session.HasUnacknowledgedLaunchResult, Is.False);
                Assert.That(session.LaunchPhoto == null, Is.True);
                Assert.That(photo == null, Is.True);
                Assert.That(host.activeSelf, Is.False);
                session.PublishPendingLaunchOutcome();
                session.PublishPendingLaunchOutcome();
                Assert.That(session.Model.Funds, Is.EqualTo(funds));
                Assert.That(session.Model.RemainingTurns, Is.EqualTo(turns));
            }
            finally
            {
                if (host != null) Object.Destroy(host);
                ResearchFlowSession.ResetForTests();
            }
            yield return null;
        }
    }
}
#endif
