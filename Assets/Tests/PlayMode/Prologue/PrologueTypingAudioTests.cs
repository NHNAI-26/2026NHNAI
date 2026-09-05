#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Border.Audio;
using Border.Prologue;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Border.Prologue.Tests
{
    public sealed class PrologueTypingAudioTests
    {
        private GameObject audioRoot;
        private GameObject root;
        private PrologueController controller;
        private PrologueSequenceSO sequence;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsNull(SoundManager.Instance, "Tests require an isolated scene.");
            audioRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            audioRoot.AddComponent<AudioListener>();
            root = new GameObject("typing test");
            root.SetActive(false);
            var overlay = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/UI/PrologueOverlay.prefab"), root.transform);
            controller = overlay.GetComponent<PrologueController>();
            sequence = ScriptableObject.CreateInstance<PrologueSequenceSO>();
            var beat = new PrologueBeat();
            Set(beat, "line", "ABC");
            Set(beat, "typeSecondsPerChar", 0.1f);
            Set(beat, "holdSeconds", 10f);
            Set(sequence, "beats", new List<PrologueBeat> { beat });
            Set(sequence, "revealSeconds", 0.1f);
            Set(controller, "sequence", sequence);
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            Object.Destroy(audioRoot);
            Object.Destroy(sequence);
            yield return null;
        }

        [Test]
        public void CharacterChanges_PlayOneKeyPerFrame_IgnoreWhitespace_AndKeepOtherSounds()
        {
            var other = SoundManager.Instance.PlaySfx("EngineStop");
            Call("PlayTypingSound", "ABC", 0, 0);
            Assert.AreEqual(0, Keys());
            Call("PlayTypingSound", " \n", 0, 2);
            Assert.AreEqual(0, Keys());
            Call("PlayTypingSound", "ABC", 0, 3);
            Assert.AreEqual(1, Keys(), "A delayed frame must not stack three key sounds.");
            Call("PlayTypingSound", "ABC", 0, 1);
            Assert.AreEqual(2, Keys(), "Short key tails may overlap.");
            Call("StopTypingSound");
            Assert.AreEqual(0, Keys());
            Assert.IsTrue(other.IsValid, "Typing cleanup must not stop unrelated sound effects.");
        }

        [UnityTest]
        public IEnumerator Skip_ImmediatelyStopsKeys_AndDoesNotRestartThem()
        {
            root.SetActive(true);
            Call("PlayTypingSound", "ABC", 0, 1);
            Assert.Greater(Keys(), 0);
            controller.Skip();
            Assert.AreEqual(0, Keys());
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsTrue(controller == null);
            Assert.AreEqual(0, Keys());
        }

        [UnityTest]
        public IEnumerator Completion_StopsKeysDuringHold_AndDisableStopsActiveKeys()
        {
            root.SetActive(true);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual(0, Keys());
            Assert.IsNotNull(controller, "The long hold should still be active.");
            Call("PlayTypingSound", "ABC", 0, 1);
            Assert.AreEqual(1, Keys());
            root.SetActive(false);
            Assert.AreEqual(0, Keys());
        }

        private int Keys() => audioRoot.GetComponentsInChildren<AudioSource>()
            .Count(source => source.clip != null && source.clip.name.StartsWith("keyboard"));
        private void Call(string name, params object[] args) => typeof(PrologueController)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, args);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
#endif
