#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Border.Audio;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class HappyEndingAudioTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject audioRoot;
        private GameObject root;
        private HappyEndingSequence ending;
        private TMP_Text text;

        [SetUp]
        public void SetUp()
        {
            Assert.That(SoundManager.Instance, Is.Null, "Tests require an isolated scene.");
            audioRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            audioRoot.AddComponent<AudioListener>();
            root = new GameObject("Ending audio test");
            ending = root.AddComponent<HappyEndingSequence>();
            text = new GameObject("Line", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.transform.SetParent(root.transform);
            Set("lineText", text);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            Object.Destroy(audioRoot);
            yield return null;
        }

        [Test]
        public void CharacterReveal_IgnoresWhitespace_AndDisablePreservesOtherSounds()
        {
            var other = SoundManager.Instance.PlaySfx("EngineStop");
            Call("PlayTypingSound", "ABC", 0, 0);
            Call("PlayTypingSound", " \n", 0, 2);
            Assert.That(Keys(), Is.Zero);
            Call("PlayTypingSound", "ABC", 0, 3);
            Assert.That(Keys(), Is.EqualTo(1), "Several letters revealed together play one key.");
            root.SetActive(false);
            Assert.That(Keys(), Is.Zero);
            Assert.That(other.IsValid, Is.True);
        }

        [UnityTest]
        public IEnumerator Click_CompletesCurrentLine_AndStopsKeys()
        {
            text.text = "발사 승인 났습니다.";
            Set("typeSecondsPerChar", 0.001f);
            yield return null;
            var typing = (IEnumerator)Call("TypeText", text.text);
            Assert.That(typing.MoveNext(), Is.True);
            Assert.That(Keys(), Is.GreaterThan(0));
            ending.OnPointerClick(null);
            Assert.That(typing.MoveNext(), Is.False);
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            Assert.That(Keys(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator Completion_StopsKeys_AndInstantRevealIsSilent()
        {
            text.text = "발사";
            Set("typeSecondsPerChar", 0.001f);
            yield return null;
            var typing = (IEnumerator)Call("TypeText", text.text);
            int frames = 0;
            while (typing.MoveNext())
            {
                Assert.That(++frames, Is.LessThan(100));
                yield return null;
            }
            Assert.That(Keys(), Is.Zero);
            Set("typeSecondsPerChar", 0f);
            typing = (IEnumerator)Call("TypeText", text.text);
            Assert.That(typing.MoveNext(), Is.False);
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            Assert.That(Keys(), Is.Zero);
        }

        private int Keys() => audioRoot.GetComponentsInChildren<AudioSource>()
            .Count(source => source.clip != null && source.clip.name.StartsWith("keyboard"));
        private object Call(string name, params object[] args) => typeof(HappyEndingSequence)
            .GetMethod(name, PrivateInstance).Invoke(ending, args);
        private void Set(string name, object value) => typeof(HappyEndingSequence)
            .GetField(name, PrivateInstance).SetValue(ending, value);
    }
}
#endif
