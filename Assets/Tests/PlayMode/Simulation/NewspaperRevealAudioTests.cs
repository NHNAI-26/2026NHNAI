#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Border.Audio;
using Border.Research;
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
            float stampImpact = timeline.Duration() - Get<float>("stampSettleSeconds");
            Advance(stampImpact - 0.02f - timeline.Elapsed());
            var stamp = Get<UnityEngine.UI.Image>("stampImage");
            Assert.That(stamp.gameObject.activeSelf, Is.True);
            Assert.That(Get<TMP_Text>("effectsText").maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            Assert.That(Voices("stamp"), Is.Zero, "The stamp sound belongs to contact, not its approach.");
            Advance(stampImpact + 0.01f - timeline.Elapsed());
            Assert.That(Voices("stamp"), Is.EqualTo(1));
            Assert.That(stamp.rectTransform.localScale.x, Is.LessThan(1f));
            timeline.Complete(true);
            Assert.That(shown, Is.EqualTo(1));
            Assert.That(impacts, Is.EqualTo(1));
            Assert.That(reveal.IsAnimating, Is.False);
            reveal.Hide();
            Get<Sequence>("sequence").Complete(true);
            Assert.That(Keys(), Is.Zero);
            Assert.That(Voices("stamp"), Is.Zero);
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

        [TestCase("stamp")]
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

        [TestCase("NewspaperReveal")]
        [TestCase("MailReveal")]
        public void BothImpacts_ShakeCameraAndOverlay_AndDisableRestoresPose(string prefab)
        {
            var cameraObject = new GameObject("Report shake camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Vector3 origin = new Vector3(3f, 4f, 5f);
            cameraObject.transform.localPosition = origin;
            try
            {
                CreateReveal(prefab);
                Sequence timeline = ShowManually();
                var screen = Get<RectTransform>("screenMotion");
                Vector2 screenOrigin = screen.anchoredPosition;
                Advance(0.11f);
                Tween shake = Get<Tween>("shakeTween");
                Assert.That(shake, Is.Not.Null);
                shake.Goto(0.03f, true);
                Assert.That(cameraObject.transform.localPosition, Is.Not.EqualTo(origin));
                Assert.That(screen.anchoredPosition, Is.Not.EqualTo(screenOrigin));
                shake.Complete();
                Assert.That(cameraObject.transform.localPosition, Is.EqualTo(origin));
                Assert.That(screen.anchoredPosition, Is.EqualTo(screenOrigin));
                Advance(timeline.Duration() - Get<float>("stampSettleSeconds") + 0.01f - timeline.Elapsed());
                shake = Get<Tween>("shakeTween");
                Assert.That(shake, Is.Not.Null);
                shake.Goto(0.03f, true);
                Assert.That(screen.anchoredPosition, Is.Not.EqualTo(screenOrigin));
                root.SetActive(false);
                Assert.That(cameraObject.transform.localPosition, Is.EqualTo(origin));
                Assert.That(screen.anchoredPosition, Is.EqualTo(screenOrigin));
                Assert.That(Voices("stamp"), Is.Zero);
                Assert.That(Get<UnityEngine.UI.Image>("stampImage").gameObject.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(cameraObject); }
        }

        [TestCase(ResearchGrade.S, false, TestVisibility.Public, "success-original-pixel-512")]
        [TestCase(ResearchGrade.B, false, TestVisibility.Private, "success-original-pixel-512")]
        [TestCase(ResearchGrade.C, false, TestVisibility.Public, "fail-original-pixel-512")]
        [TestCase(ResearchGrade.F, false, TestVisibility.Private, "fail-original-pixel-512")]
        [TestCase(ResearchGrade.S, true, TestVisibility.Public, "clear-blue-pixel-512")]
        public void Result_SelectsMatchingStampAndResetsPreviousReveal(ResearchGrade grade, bool finalWon,
            TestVisibility visibility, string spriteName)
        {
            var result = new ResearchLaunchResultData(LaunchMissionId.LowAltitude, EnginePresetId.Engine01,
                2024, 2, 800, 350, visibility, 50, 80, 70, 80, 10, 10, 42, grade, 600, 75, finalWon, false);
            var article = LaunchNewspaperArticle.Create(result, "시험");
            CreateReveal(article.Medium == LaunchResultMedium.Mail ? "MailReveal" : "NewspaperReveal");
            reveal.Present(article, null, null);
            var stamp = Get<UnityEngine.UI.Image>("stampImage");
            Assert.That(stamp.sprite.name, Is.EqualTo(spriteName));
            Assert.That(stamp.gameObject.activeSelf, Is.False);
            Get<Sequence>("sequence").Complete(true);
            Assert.That(stamp.gameObject.activeSelf, Is.True);
            reveal.Present(article, null, null);
            Assert.That(stamp.gameObject.activeSelf, Is.False);
            Assert.That(Voices("stamp"), Is.Zero);
            var effects = Get<TMP_Text>("effectsText");
            Assert.That(effects.fontStyle.HasFlag(FontStyles.Bold), Is.True);
            Assert.That(effects.fontSharedMaterial.GetFloat("_FaceDilate"), Is.GreaterThanOrEqualTo(0.1f));
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
