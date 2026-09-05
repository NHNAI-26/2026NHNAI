#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Border.Audio;
using Border.UI;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class NewspaperRevealAudioTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject audioRoot;
        private GameObject root;
        private NewspaperReveal reveal;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            Assert.That(SoundManager.Instance, Is.Null, "Tests require an isolated scene.");
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            audioRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            audioRoot.AddComponent<AudioListener>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.SetActive(false);
            Object.Destroy(root);
            Object.Destroy(audioRoot);
            Time.timeScale = previousTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Newspaper_PlaysFlightHammerAndKeysAtRevealTimes()
        {
            yield return VerifyTimeline("NewspaperReveal", "hammer_collision_sound", "email");
        }

        [UnityTest]
        public IEnumerator Mail_PlaysFlightEmailAndKeysAtRevealTimes()
        {
            yield return VerifyTimeline("MailReveal", "email", "hammer_collision_sound");
        }

        private IEnumerator VerifyTimeline(string prefab, string impactId, string otherImpactId)
        {
            CreateReveal(prefab);
            int impacts = 0;
            int shown = 0;
            reveal.OnImpact.AddListener(() => impacts++);
            reveal.OnShown.AddListener(() => shown++);
            var unrelated = SoundManager.Instance.PlaySfx("EngineStop");
            unrelated.SetLoop(true);
            Sequence timeline = ShowManually();
            Advance(0.05f);
            Assert.That(Voices("woosh"), Is.EqualTo(1));
            Assert.That(Voices(impactId), Is.Zero);
            Assert.That(Keys(), Is.Zero);
            Advance(0.06f);
            Assert.That(impacts, Is.EqualTo(1));
            Assert.That(Voices("woosh"), Is.Zero);
            Assert.That(Voices(impactId), Is.EqualTo(1));
            Assert.That(Voices(otherImpactId), Is.Zero);
            Assert.That(Keys(), Is.Zero);
            Advance(0.11f);
            Assert.That(Keys(), Is.EqualTo(1), "A frame revealing several letters plays only one key.");
            Assert.That(Get<TMP_Text>("articleText").maxVisibleCharacters, Is.Zero);
            yield return null;
            Advance(0.24f);
            Assert.That(Get<TMP_Text>("articleText").maxVisibleCharacters, Is.GreaterThan(0));
            Assert.That(Keys(), Is.GreaterThan(0));
            Assert.That(Get<TMP_Text>("effectsText").maxVisibleCharacters, Is.Zero);
            yield return null;
            Advance(0.29f);
            Assert.That(Get<TMP_Text>("effectsText").maxVisibleCharacters, Is.EqualTo(4));
            Assert.That(Keys(), Is.GreaterThan(0));
            timeline.Complete(true);
            Assert.That(shown, Is.EqualTo(1));
            Assert.That(impacts, Is.EqualTo(1));
            Assert.That(reveal.IsAnimating, Is.False);
            reveal.Hide();
            Get<Sequence>("sequence").Complete(true);
            Assert.That(Keys(), Is.Zero);
            Assert.That(Voices(impactId), Is.Zero);
            Assert.That(unrelated.IsValid, Is.True, "Closing a report must preserve unrelated effects.");
        }

        [UnityTest]
        public IEnumerator RestartAndDisable_StopOwnedSoundsWithoutDelayedImpact()
        {
            CreateReveal("MailReveal");
            ShowManually();
            Advance(0.05f);
            Assert.That(Voices("woosh"), Is.EqualTo(1));
            ShowManually();
            Assert.That(Voices("woosh"), Is.Zero);
            Advance(0.23f);
            Assert.That(Voices("email"), Is.EqualTo(1));
            Assert.That(Keys(), Is.EqualTo(1));
            ShowManually();
            Assert.That(Voices("email"), Is.Zero);
            Assert.That(Keys(), Is.Zero);
            Advance(0.05f);
            root.SetActive(false);
            Assert.That(Voices("woosh"), Is.Zero);
            Advance(1f);
            yield return null;
            Assert.That(Voices("email"), Is.Zero);
            Assert.That(Keys(), Is.Zero);
            Assert.That(reveal.IsAnimating, Is.False);
        }

        [Test]
        public void WhitespaceAndRichText_DoNotAddExtraKeysWithinAFrame()
        {
            CreateReveal("NewspaperReveal");
            ShowManually();
            var title = Get<TMP_Text>("headlineText");
            title.text = " \n<b>AB</b>";
            title.ForceMeshUpdate();
            typeof(NewspaperReveal).GetMethod("RevealCharacters", PrivateInstance)
                .Invoke(reveal, new object[] { title, 2 });
            Assert.That(Keys(), Is.Zero);
            typeof(NewspaperReveal).GetMethod("RevealCharacters", PrivateInstance)
                .Invoke(reveal, new object[] { title, 3 });
            typeof(NewspaperReveal).GetMethod("RevealCharacters", PrivateInstance)
                .Invoke(reveal, new object[] { title, 4 });
            Assert.That(Keys(), Is.EqualTo(1));
            Assert.That(title.textInfo.characterCount, Is.EqualTo(4));
        }

        [TestCase("woosh")]
        [TestCase("email")]
        [TestCase("hammer_collision_sound")]
        public void RevealClips_AreRegisteredAsPreloadedNonLooping2DSounds(string id)
        {
            var database = AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>(
                "Assets/02. ScriptableObjects/Audio/SoundDatabase.asset");
            Assert.That(database.TryGetSfx(id, out var entry), Is.True);
            Assert.That(entry.Loop, Is.False);
            Assert.That(entry.UseSpatialAudio, Is.False);
            Assert.That(entry.Volume, Is.GreaterThan(0f));
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(entry.Clip));
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.True);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        }

        private void CreateReveal(string prefab)
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/03. Prefabs/UI/{prefab}.prefab"));
            reveal = root.GetComponent<NewspaperReveal>();
            Set("flyDuration", 0.1f);
            Set("settleDuration", 0.1f);
            Set("headlineCharacterSeconds", 0.01f);
            Set("articleCharacterSeconds", 0.01f);
            Set("sectionPauseSeconds", 0.2f);
            Set("resultLineSeconds", 0.2f);
            Get<TMP_Text>("headlineText").richText = true;
            Get<TMP_Text>("headlineText").text = "<b>ABCD</b>";
            Get<TMP_Text>("articleText").text = "BODY TEXT";
            Get<TMP_Text>("effectsText").text = "ONE\nTWO";
        }

        private Sequence ShowManually()
        {
            reveal.Show();
            var timeline = Get<Sequence>("sequence");
            timeline.SetUpdate(UpdateType.Manual, true);
            return timeline;
        }

        private static void Advance(float seconds) => DOTween.ManualUpdate(seconds, seconds);
        private int Voices(string id) => audioRoot.GetComponentsInChildren<AudioSource>()
            .Count(source => source.clip != null && source.clip.name == id);
        private int Keys() => audioRoot.GetComponentsInChildren<AudioSource>()
            .Count(source => source.clip != null && source.clip.name.StartsWith("keyboard"));
        private T Get<T>(string name) => (T)typeof(NewspaperReveal).GetField(name, PrivateInstance).GetValue(reveal);
        private void Set(string name, object value) => typeof(NewspaperReveal).GetField(name, PrivateInstance).SetValue(reveal, value);
    }
}
#endif
