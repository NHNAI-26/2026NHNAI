using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Border.Research;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Border.Audio.Tests
{
    public sealed class ResearchMiniGameAudioTests
    {
        private GameObject soundRoot;
        private GameObject host;
        private SoundDatabaseSO database;
        private readonly List<AudioClip> clips = new();
        private ResearchMiniGameController controller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (SoundManager.Instance != null)
            {
                Object.Destroy(SoundManager.Instance.gameObject);
                yield return null;
            }
            soundRoot = new GameObject("Output Audio Test Manager");
            soundRoot.AddComponent<AudioListener>();
            var pool = soundRoot.AddComponent<SfxPool>();
            database = ScriptableObject.CreateInstance<SoundDatabaseSO>();
            var entries = new List<SfxEntry>();
            foreach (string id in new[] { "Gauge", "hit", "miss", "warning", "stone push", "steam", "success" })
            {
                var clip = AudioClip.Create(id, 44100, 1, 44100, false);
                clips.Add(clip);
                entries.Add(new SfxEntry(id, clip, loop: id != "hit" && id != "miss"));
            }
            SetField(database, "sfxEntries", entries);
            database.RebuildLookup();
            var manager = soundRoot.AddComponent<SoundManager>();
            SetField(manager, "database", database);
            SetField(manager, "sfxPool", pool);
            host = new GameObject("Output Audio Test");
            controller = host.AddComponent<ResearchMiniGameController>();
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.MaxOutput, false, 77, _ => { });
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (host != null) Object.Destroy(host);
            yield return null;
            if (soundRoot != null) Object.Destroy(soundRoot);
            if (database != null) Object.Destroy(database);
            foreach (AudioClip clip in clips) Object.Destroy(clip);
            clips.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GaugeLoopsStopsOnHitAndRestartsWithoutDuplicates()
        {
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            Assert.That(Playing("Gauge")[0].loop, Is.True);
            yield return new WaitForSeconds(1.1f);
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1), "The gauge must survive the end of its clip.");
            controller.RecordOutputStageForTests(controller.GetOutputTargetForTests());
            Assert.That(Playing("Gauge"), Is.Empty);
            Assert.That(Playing("hit").Length, Is.EqualTo(1));
            Assert.That(Playing("hit")[0].loop, Is.False);
            controller.RecordOutputStageForTests(0f);
            Assert.That(Playing("hit").Length, Is.EqualTo(1));
            Assert.That(Playing("miss"), Is.Empty);
            controller.ForceAdvanceOutputJudgementForTests();
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            controller.AdvanceTimeForTests(0.1f);
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            controller.HideForReuse();
            Assert.That(Playing("Gauge"), Is.Empty);
            Assert.That(Playing("hit"), Is.Empty);
        }

        [UnityTest]
        public IEnumerator MissTimeoutDisableAndCompletionReleaseAudio()
        {
            float target = controller.GetOutputTargetForTests();
            controller.RecordOutputStageForTests(target < 0.5f ? 1f : 0f);
            Assert.That(Playing("Gauge"), Is.Empty);
            Assert.That(Playing("miss").Length, Is.EqualTo(1));
            controller.ForceAdvanceOutputJudgementForTests();
            controller.AdvanceTimeForTests(5.1f);
            Assert.That(Playing("Gauge"), Is.Empty);
            Assert.That(Playing("miss").Length, Is.EqualTo(1));
            Assert.That(Playing("miss")[0].loop, Is.False);
            controller.ForceAdvanceOutputJudgementForTests();
            host.SetActive(false);
            Assert.That(Playing("Gauge"), Is.Empty);
            host.SetActive(true);
            yield return null;
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            controller.ForceCompleteForTests(100);
            Assert.That(Playing("Gauge"), Is.Empty);
            Assert.That(Playing("miss"), Is.Empty);
        }

        [UnityTest]
        public IEnumerator CoolingRotationRequiresMovementAndRestoresPipeOnRelease()
        {
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 77, _ => { });
            var pipe = host.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "CoolingPipe");
            var button = host.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "CoolingValve");
            Vector2 rest = pipe.anchoredPosition;
            Vector2 valvePosition = button.anchoredPosition;
            controller.RotateValveForTests(Vector2.right * 200f, true);
            Assert.That(Playing("stone push"), Is.Empty);
            Assert.That(Playing("steam"), Is.Empty);
            controller.RotateValveForTests(Vector2.down * 200f);
            Assert.That(Playing("stone push").Length, Is.EqualTo(1));
            Assert.That(Playing("steam").Length, Is.EqualTo(1));
            Assert.That(Playing("steam")[0].loop, Is.True);
            Assert.That(pipe.anchoredPosition, Is.Not.EqualTo(rest));
            Assert.That(button.anchoredPosition, Is.EqualTo(valvePosition));
            controller.AdvanceTimeForTests(0.13f);
            Assert.That(Playing("stone push"), Is.Empty);
            Assert.That(Playing("steam"), Is.Empty);
            Assert.That(pipe.anchoredPosition, Is.EqualTo(rest));
            controller.RotateValveForTests(Vector2.right * 200f);
            Assert.That(Playing("stone push").Length, Is.EqualTo(1));
            Assert.That(Playing("steam"), Is.Empty, "Closing the valve must not release steam.");
            controller.ReleaseValveForTests();
            Assert.That(Playing("stone push"), Is.Empty);
            Assert.That(pipe.anchoredPosition, Is.EqualTo(rest));
            controller.RotateValveForTests(Vector2.right * 200f, true);
            controller.RotateValveForTests(Vector2.down * 200f);
            controller.HideForReuse();
            Assert.That(Playing("steam"), Is.Empty);
            Assert.That(Playing("stone push"), Is.Empty);
            Assert.That(pipe.anchoredPosition, Is.EqualTo(rest));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoolingWarningUsesHeatThresholdAndStopsOnCompletion()
        {
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 77, _ => { });
            controller.AdvanceTimeForTests(2.9f);
            Assert.That(Playing("warning"), Is.Empty);
            controller.AdvanceTimeForTests(0.2f);
            Assert.That(Playing("warning").Length, Is.EqualTo(1));
            Assert.That(Playing("warning")[0].loop, Is.True);
            controller.RotateValveForTests(Vector2.right * 200f, true);
            controller.RotateValveForTests(Vector2.down * 200f);
            Assert.That(Playing("warning").Length, Is.EqualTo(1), "Cooling just below 70% must not chatter.");
            for (int step = 2; step <= 6; step++)
            {
                float angle = -90f * step * Mathf.Deg2Rad;
                controller.RotateValveForTests(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 200f);
            }
            Assert.That(controller.GetCoolingHeatForTests(), Is.LessThan(0.6f));
            Assert.That(Playing("warning"), Is.Empty);
            controller.AdvanceTimeForTests(2f);
            Assert.That(Playing("warning").Length, Is.EqualTo(1));
            controller.ForceCompleteForTests(80);
            Assert.That(Playing("warning"), Is.Empty);
            Assert.That(Playing("steam"), Is.Empty);
            Assert.That(Playing("stone push"), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FuelPromptAndGaugeFollowHoldAndCleanup()
        {
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => { });
            var dial = host.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(image => image.name == "FuelDial");
            var instruction = host.GetComponentsInChildren<Component>(true).First(label => label.name == "Instruction" && label.GetType().GetProperty("text") != null);
            Assert.That(instruction.GetType().GetProperty("text").GetValue(instruction), Is.EqualTo("최대게이지까지 채우세요"));
            Assert.That(Playing("Gauge"), Is.Empty);
            Color bright = dial.color;
            controller.AdvanceTimeForTests(0.45f);
            Assert.That(dial.color.g, Is.LessThan(bright.g));
            controller.BeginFuelFillForTests();
            Assert.That(dial.color, Is.EqualTo(bright));
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            Assert.That(Playing("Gauge")[0].loop, Is.True);
            controller.BeginFuelFillForTests();
            controller.AdvanceTimeForTests(0.1f);
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            controller.AdvanceTimeForTests(controller.GetFuelDurationForTests());
            Assert.That(Playing("Gauge"), Is.Empty, "Audio stops when the gauge reaches maximum.");
            controller.ReleaseFuelForTests();
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => { });
            controller.BeginFuelFillForTests();
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            controller.ReleaseFuelForTests();
            Assert.That(Playing("Gauge"), Is.Empty);
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.FuelCapacity, false, 77, _ => { });
            controller.BeginFuelFillForTests();
            Assert.That(Playing("Gauge").Length, Is.EqualTo(1));
            host.SetActive(false);
            Assert.That(Playing("Gauge"), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FinalSuccessPlaysOnceForEveryGameAndSkipsFailure()
        {
            foreach (var stat in new[] { EngineStatId.FuelCapacity, EngineStatId.Cooling, EngineStatId.MaxOutput, EngineStatId.IgnitionReliability })
            {
                foreach (int score in new[] { 49, 50, 80 })
                {
                    controller.InitializeForTests(EnginePresetId.Engine01, stat, false, 77, _ => { });
                    controller.ForceCompleteForTests(score);
                    Assert.That(Playing("success").Length, Is.EqualTo(score >= 50 ? 1 : 0), stat + " score " + score);
                    controller.ForceCompleteForTests(score);
                    Assert.That(Playing("success").Length, Is.EqualTo(score >= 50 ? 1 : 0));
                    if (score >= 50) Assert.That(Playing("success")[0].loop, Is.False);
                    controller.ForceDismissForTests();
                    Assert.That(Playing("success"), Is.Empty);
                }
            }
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.Cooling, false, 77, _ => { });
            SetField(controller, "coolingHeat", 1f);
            controller.ForceCompleteForTests(100);
            Assert.That(Playing("success"), Is.Empty, "Overheating is not a success.");
            yield return null;
        }

        private AudioSource[] Playing(string clipName) => soundRoot.GetComponentsInChildren<AudioSource>(true)
            .Where(source => source.isPlaying && source.clip != null && source.clip.name == clipName).ToArray();

        private static void SetField(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
