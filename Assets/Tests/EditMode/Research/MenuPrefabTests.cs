using System.Linq;
using System.Reflection;
using Border.Settings;
using Border.Title;
using Border.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Tests
{
    public sealed class MenuPrefabTests
    {
        [Test]
        public void TitlePrefab_HasMatchingSettingsButtonAndWiredMenu()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/TitleScreen.prefab");
            var title = new SerializedObject(prefab.GetComponent<TitleMenu>());
            Button start = (Button)title.FindProperty("newGameButton").objectReferenceValue;
            Button settings = (Button)title.FindProperty("settingsButton").objectReferenceValue;
            Assert.That(settings, Is.Not.Null);
            Assert.That(((RectTransform)settings.transform).sizeDelta, Is.EqualTo(((RectTransform)start.transform).sizeDelta));
            Assert.That(settings.GetComponentInChildren<TMP_Text>(true).text, Is.EqualTo("설정"));
            Assert.That(title.FindProperty("settingsMenu").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void SettingsPrefab_OpenCloseReusesExistingUiAndRendersText()
        {
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/SettingsMenu.prefab"));
            try
            {
                var menu = root.GetComponent<SimpleSettingsMenuController>();
                int count = root.GetComponentsInChildren<Transform>(true).Length;
                menu.Open();
                Canvas.ForceUpdateCanvases();
                TMP_Dropdown dropdown = root.GetComponentInChildren<TMP_Dropdown>();
                Assert.That(dropdown.options.Count, Is.GreaterThan(1));
                Assert.That(dropdown.captionText.text, Is.Not.Empty);
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>())
                {
                    text.ForceMeshUpdate();
                    Assert.That(text.font, Is.Not.Null, text.name);
                    Assert.That(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(character => character.isVisible), Is.True, text.name);
                    Assert.That(text.rectTransform.rect.height, Is.GreaterThan(0), text.name);
                }
                menu.Close();
                Assert.That(root.GetComponentInChildren<Canvas>(true).gameObject.activeSelf, Is.False);
                menu.Open();
                Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void TitleSettings_BlockerCoversParentCanvas()
        {
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/TitleScreen.prefab"));
            try
            {
                var menu = root.GetComponentInChildren<SimpleSettingsMenuController>(true);
                menu.Open();
                Canvas.ForceUpdateCanvases();
                var blocker = (RectTransform)menu.transform.Find("SettingsCanvas/ModalBlocker");
                var canvas = (RectTransform)root.transform;
                Assert.That(blocker.rect.width, Is.GreaterThan(0));
                Assert.That(blocker.rect.size, Is.EqualTo(canvas.rect.size));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Resolutions_SingleNativeModeStillOffersSmallerSizesWithoutDuplicates()
        {
            var desktop = new Resolution { width = 1920, height = 1080, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } };
            var options = SettingsGraphicsUtility.BuildResolutionList(new[] { desktop, desktop }, desktop);
            Assert.That(options.Select(option => (option.width, option.height)), Is.EquivalentTo(new[] { (1920, 1080), (1600, 900), (1280, 720) }));
        }

        [Test]
        public void PausePrefab_ResumeButtonRestoresTimeAndKeepsUiInstance()
        {
            float originalTimeScale = Time.timeScale;
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/PauseMenu.prefab"));
            try
            {
                var menu = root.GetComponent<PauseMenuController>();
                typeof(PauseMenuController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, null);
                Time.timeScale = 0.5f;
                int count = root.GetComponentsInChildren<Transform>(true).Length;
                menu.Open();
                menu.Open();
                Assert.That(Time.timeScale, Is.Zero);
                root.GetComponentsInChildren<Button>(true).Single(button => button.name == "ResumeButton").onClick.Invoke();
                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
                Assert.That(root.GetComponentInChildren<Canvas>(true).gameObject.activeSelf, Is.False);
                Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Time.timeScale = originalTimeScale;
            }
        }
    }
}
