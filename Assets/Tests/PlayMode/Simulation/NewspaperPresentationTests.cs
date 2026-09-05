#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using Border.Research;
using Border.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class NewspaperPresentationTests
    {
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
