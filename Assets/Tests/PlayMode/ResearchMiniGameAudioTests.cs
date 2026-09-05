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
