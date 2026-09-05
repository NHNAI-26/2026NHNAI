#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Border.Audio;
using Border.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Border.UI.Tests
{
    public sealed class ButtonClickSoundTests
    {
        private GameObject audioRoot;
        private GameObject buttonRoot;
        private GameObject events;
        private Button button;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsNull(SoundManager.Instance);
            audioRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            audioRoot.AddComponent<AudioListener>();
            events = new GameObject("button test events", typeof(EventSystem));
            buttonRoot = new GameObject("runtime button", typeof(RectTransform), typeof(Button));
            button = buttonRoot.GetComponent<Button>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(buttonRoot);
            Object.Destroy(events);
            Object.Destroy(audioRoot);
            yield return null;
        }

        [Test]
        public void PointerClick_PlaysOnce_AfterRepeatedBinding_AndListenerReset()
        {
            UISelectableSoundHook.Bind(button);
            UISelectableSoundHook.Bind(button);
            UISelectableSoundHook.ClearListeners(button);
            int calls = 0;
            button.onClick.AddListener(() => calls++);
            Click(PointerEventData.InputButton.Left);
            Assert.AreEqual(1, Clicks());
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void DisabledAndRightClick_DoNotPlay()
        {
            Click(PointerEventData.InputButton.Right);
            button.interactable = false;
            Click(PointerEventData.InputButton.Left);
            Assert.AreEqual(0, Clicks());
        }

        [Test]
        public void Submit_PlaysEvenWhenActionClosesItsOwnButton()
        {
            UISelectableSoundHook.ClearListeners(button);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => buttonRoot.SetActive(false));
            UISelectableSoundHook.Bind(button);
            button.OnSubmit(new BaseEventData(EventSystem.current));
            Assert.AreEqual(1, Clicks());
        }

        [UnityTest]
        public IEnumerator InitiallyInactiveButton_IsBoundWhenShown()
        {
            Object.Destroy(buttonRoot);
            yield return null;
            buttonRoot = new GameObject("late button", typeof(RectTransform));
            buttonRoot.SetActive(false);
            button = buttonRoot.AddComponent<Button>();
            yield return null;
            buttonRoot.SetActive(true);
            yield return null;
            Click(PointerEventData.InputButton.Left);
            Assert.AreEqual(1, Clicks());
        }

        private int Clicks() => audioRoot.GetComponentsInChildren<AudioSource>()
            .Count(source => source.clip != null && source.clip.name == "click");
        private void Click(PointerEventData.InputButton mouseButton) => button.OnPointerClick(
            new PointerEventData(EventSystem.current) { button = mouseButton });
    }
}
#endif
