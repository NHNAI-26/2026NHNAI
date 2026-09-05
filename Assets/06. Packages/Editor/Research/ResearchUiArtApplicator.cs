using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Editor
{
    public static class ResearchUiArtApplicator
    {
        private const string SheetPath = "Assets/05. Arts/UI/Resources/engine_ui_01.psd";
        private const string PrefabFolder = "Assets/03. Prefabs/UI/Resources/ResearchUI/";

        [MenuItem("Border/Research/Apply Engine UI Art")]
        public static void ApplyToPrefabAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (LoadSprites() == null)
            {
                Debug.LogError("Import engine_ui_01.psd with its seven sprite slices before applying research UI art.");
                return;
            }
            UpdatePrefab("EnginePresetCard", ApplyCard);
            UpdatePrefab("ResearchOperationScreen", ApplyOperation);
            UpdatePrefab("ResearchMiniGameScreen", ApplyMiniGame);
        }

        [MenuItem("Border/Research/Apply Mini Game UI Art")]
        public static void ApplyMiniGameToPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (LoadSprites() == null)
            {
                Debug.LogError("Import engine_ui_01.psd before applying mini game UI art.");
                return;
            }
            UpdatePrefab("ResearchMiniGameScreen", ApplyMiniGame);
        }

        private static void UpdatePrefab(string name, System.Action<GameObject> apply)
        {
            string path = PrefabFolder + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void ApplyMiniGame(GameObject root)
        {
            Sprite[] sprites = LoadSprites();
            if (sprites == null) return;
            LayoutElement topRow = Find(root.transform, "TopRow")?.GetComponent<LayoutElement>();
            if (topRow != null)
            {
                topRow.minHeight = topRow.preferredHeight = 48f;
                topRow.flexibleHeight = 0f;
            }
            Skin(Find(root.transform, "MiniGamePanel")?.GetComponent<Image>(), sprites[0]);
            SkinButton(Find(root.transform, "PrimaryActionButton")?.GetComponent<Button>(), sprites[5]);

            foreach (string name in new[] { "FuelGaugeFrame", "OutputGaugeFrame" })
            {
                Image track = Find(root.transform, name)?.GetComponent<Image>();
                Skin(track, sprites[2]);
                if (track != null) track.raycastTarget = false;
            }
            Image fuelFill = Find(root.transform, "FuelFill")?.GetComponent<Image>();
            Skin(fuelFill, sprites[1]);
            if (fuelFill != null) fuelFill.raycastTarget = false;

            // Keep runtime-driven valve colours, ignition flashes and judgement bands unobscured.
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (button.name.StartsWith("CoolingValve_") || button.name.StartsWith("Igniter_"))
                    AddFrame(button.transform, sprites[4]);
            Transform hotspot = Find(root.transform, "CoolingHotspot");
            if (hotspot != null) AddFrame(hotspot, sprites[5]);
            foreach (string name in new[] { "FuelJudgementText", "OutputJudgementText", "ResultDetailText" })
            {
                TMP_Text text = Find(root.transform, name)?.GetComponent<TMP_Text>();
                if (text == null) continue;
                text.enableAutoSizing = true;
                text.fontSizeMin = name == "ResultDetailText" ? 14f : 24f;
                text.fontSizeMax = name == "ResultDetailText" ? 21f : 38f;
            }
        }

        private static void AddFrame(Transform parent, Sprite sprite)
        {
            Transform frame = parent.Find("UiArtFrame");
            if (frame == null)
            {
                var frameObject = new GameObject("UiArtFrame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                frameObject.transform.SetParent(parent, false);
                frame = frameObject.transform;
            }
            frame.SetAsFirstSibling();
            var rect = (RectTransform)frame;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = frame.GetComponent<Image>();
            Skin(image, sprite);
            image.fillCenter = false;
            image.raycastTarget = false;
            frame.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        public static void ApplyOperation(GameObject root)
        {
            Sprite[] sprites = LoadSprites();
            if (sprites == null) return;
            foreach (string name in new[] { "TopInfoBar", "EnginePresetColumn", "DetailColumn" })
                Skin(Find(root.transform, name)?.GetComponent<Image>(), sprites[0]);

            // The outer frames carry the artwork; keep the inner content groups unframed.
            foreach (string name in new[] { "SelectedPanel", "ActionPanel", "DesignEntryPanel", "StatusPanel", "DateChip", "RemainingTurnsChip", "FundsChip", "QuarterlyFundingChip" })
            {
                Image image = Find(root.transform, name)?.GetComponent<Image>();
                if (image == null) continue;
                image.color = Color.clear;
                image.raycastTarget = false;
            }

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("EngineCard_")) ApplyCard(button.gameObject, sprites);
                else SkinButton(button, sprites[button.name.StartsWith("StatButton_") ? 4
                    : button.name == "NormalResearchButton" || button.name == "FocusedResearchButton" || button.name == "EnterDesignButton" ? 5 : 6]);
            }
            Transform selectedPanel = Find(root.transform, "SelectedPanel");
            if (selectedPanel != null)
            {
                AddCompletionGauge(selectedPanel, sprites);
                selectedPanel.GetComponent<LayoutElement>().preferredHeight = 190f;
            }
            LayoutElement statRow = Find(root.transform, "StatButtons")?.GetComponent<LayoutElement>();
            if (statRow != null) statRow.flexibleHeight = 0f;
            VerticalLayoutGroup cards = Find(root.transform, "EnginePresetCards")?.GetComponent<VerticalLayoutGroup>();
            if (cards != null) cards.spacing = 5f;
            foreach (string name in new[] { "EngineColumnTitle", "ActionTitle", "DesignEntryTitle", "StatusTitle" })
            {
                Transform title = Find(root.transform, name);
                if (title == null) continue;
                LayoutElement layout = title.GetComponent<LayoutElement>();
                if (layout == null) layout = title.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = layout.preferredHeight = 24f;
                layout.flexibleHeight = 0f;
            }
        }

        public static void ApplyCard(GameObject root)
        {
            Sprite[] sprites = LoadSprites();
            if (sprites != null) ApplyCard(root, sprites);
        }

        private static void ApplyCard(GameObject root, Sprite[] sprites)
        {
            SkinButton(root.GetComponent<Button>(), sprites[6]);
            Transform content = Find(root.transform, "Content");
            if (content == null) return;
            Transform iconTransform = content.Find("EngineIcon");
            if (iconTransform == null)
            {
                var iconObject = new GameObject("EngineIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(content, false);
                iconTransform = iconObject.transform;
            }
            iconTransform.SetAsFirstSibling();
            Image icon = iconTransform.GetComponent<Image>();
            icon.sprite = sprites[3];
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            LayoutElement iconLayout = icon.GetComponent<LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = 30f;
            iconLayout.minHeight = iconLayout.preferredHeight = 30f;
            iconLayout.flexibleWidth = iconLayout.flexibleHeight = 0f;

            TMP_Text title = Find(content, "Title")?.GetComponent<TMP_Text>();
            TMP_Text detail = Find(content, "Detail")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                LayoutElement layout = title.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.minWidth = layout.preferredWidth = 62f;
                    layout.flexibleWidth = 0f;
                }
            }
            if (detail != null)
            {
                LayoutElement layout = detail.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.minWidth = 0f;
                    layout.preferredWidth = -1f;
                    layout.flexibleWidth = 1f;
                }
                detail.enableAutoSizing = true;
                detail.fontSizeMin = 10f;
                detail.fontSizeMax = 12f;
            }
        }

        private static void AddCompletionGauge(Transform parent, Sprite[] sprites)
        {
            Transform existing = parent.Find("SelectedEngineCompletion");
            GameObject root = existing != null ? existing.gameObject
                : new GameObject("SelectedEngineCompletion", typeof(RectTransform), typeof(Image), typeof(Slider), typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            root.transform.SetSiblingIndex(1);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 18f;
            layout.flexibleWidth = 1f;
            Image track = root.GetComponent<Image>();
            Skin(track, sprites[2]);
            track.pixelsPerUnitMultiplier = 44f / 18f;
            track.raycastTarget = false;

            Transform fillTransform = root.transform.Find("Fill");
            if (fillTransform == null)
            {
                var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(root.transform, false);
                fillTransform = fillObject.transform;
            }
            var fillRect = (RectTransform)fillTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            Image fill = fillTransform.GetComponent<Image>();
            Skin(fill, sprites[1]);
            fill.pixelsPerUnitMultiplier = track.pixelsPerUnitMultiplier;
            fill.raycastTarget = false;
            Slider slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = ResearchPrototypeModel.MaxEngineCompletion;
            slider.wholeNumbers = true;
            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.interactable = false;
            slider.targetGraphic = null;
            slider.SetValueWithoutNotify(0f);
        }

        private static void SkinButton(Button button, Sprite sprite)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            Skin(image, sprite);
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = colors.selectedColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.85f, 0.9f);
            colors.disabledColor = new Color(0.55f, 0.58f, 0.6f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                label.raycastTarget = false;
                label.enableAutoSizing = true;
                label.fontSizeMax = label.fontSize;
                label.fontSizeMin = Mathf.Min(10f, label.fontSize);
            }
        }

        private static void Skin(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static Sprite[] LoadSprites()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SheetPath);
            var sprites = new Sprite[7];
            foreach (Object asset in assets)
                if (asset is Sprite sprite)
                    for (int i = 0; i < sprites.Length; i++)
                        if (sprite.name == "engine_ui_01_" + i) sprites[i] = sprite;
            foreach (Sprite sprite in sprites) if (sprite == null) return null;
            return sprites;
        }
    }
}
