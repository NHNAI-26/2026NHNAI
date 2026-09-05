using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Border.Research;
using Border.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Border.Audio.Tests
{
    public sealed class IgnitionPolishAudioTests
    {
        private GameObject host;
        private GameObject soundRoot;
        private SoundDatabaseSO database;
        private readonly List<AudioClip> clips = new();
        private ResearchMiniGameController controller;
        private Button[] buttons;
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private int Playing(string id) => soundRoot.GetComponentsInChildren<AudioSource>(true)
            .Count(s => s.isPlaying && s.clip != null && s.clip.name == id);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (SoundManager.Instance != null) Object.Destroy(SoundManager.Instance.gameObject);
            yield return null;
            soundRoot = new GameObject("Ignition Audio Test");
            soundRoot.AddComponent<AudioListener>();
            var pool = soundRoot.AddComponent<SfxPool>();
            database = ScriptableObject.CreateInstance<SoundDatabaseSO>();
            var entries = new List<SfxEntry>();
            foreach (string id in new[] { "button8", "wrong", "click" })
            {
                var clip = AudioClip.Create(id, 44100, 1, 44100, false);
                clips.Add(clip);
                entries.Add(new SfxEntry(id, clip));
            }
            Set(database, "sfxEntries", entries);
            database.RebuildLookup();
            var manager = soundRoot.AddComponent<SoundManager>();
            Set(manager, "database", database);
            Set(manager, "sfxPool", pool);
            host = new GameObject("Ignition Audio Host");
            controller = host.AddComponent<ResearchMiniGameController>();
            controller.InitializeForTests(EnginePresetId.Engine01, EngineStatId.IgnitionReliability, false, 80, _ => { });
            buttons = host.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("Igniter_")).OrderBy(b => b.name).ToArray();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(host);
            yield return null;
            Object.Destroy(soundRoot);
            Object.Destroy(database);
            foreach (var clip in clips) Object.Destroy(clip);
            clips.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CorrectUsesButton8_WrongUsesWrong_NoDefaultClickOrDuplicateInput()
        {
            buttons[0].onClick.Invoke();
            Assert.That(Playing("click") + Playing("button8") + Playing("wrong"), Is.Zero);
            controller.AdvanceTimeForTests(0.5f);
            controller.AdvanceTimeForTests(0.901f);
            controller.AdvanceTimeForTests(0.5f);
            int[] sequence = controller.GetIgnitionSequenceForTests();
            foreach (var button in buttons) UISelectableSoundHook.Bind(button);
            buttons[sequence[0]].onClick.Invoke();
            buttons[sequence[0]].onClick.Invoke();
            Assert.That(Playing("button8"), Is.EqualTo(1));
            Assert.That(Playing("wrong") + Playing("click"), Is.Zero);
            yield return null;
            Assert.That(host.GetComponentsInChildren<ParticleSystem>().Any(p => p.IsAlive()), Is.True);
            controller.AdvanceTimeForTests(0.126f);
            buttons[(sequence[1] + 1) % 4].onClick.Invoke();
            Assert.That(Playing("wrong"), Is.EqualTo(1));
            Assert.That(Playing("button8") + Playing("click"), Is.Zero);
            controller.HideForReuse();
            Assert.That(Playing("wrong"), Is.Zero);
            Assert.That(host.GetComponentsInChildren<ParticleSystem>(true).All(p => !p.IsAlive()), Is.True);
        }
    }
}
