using Border.Settings;
using Border.Title;
using Border.UI;
using Border.Research;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Editor
{
    public static class MenuUiPrefabBuilder
    {
        private const string Folder = "Assets/03. Prefabs/UI";

        [MenuItem("Border/UI/Rebuild Settings and Pause Prefabs")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Save(CreateSettings(), "SettingsMenu");
            Save(CreatePause(), "PauseMenu");
            InstallTitleSettings();
        }

        [MenuItem("Border/Research/Rebuild Result and Ending Prefabs")]
        public static void RebuildResearchOutcomePrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Save(CreateResearchOutcome(true), "ResearchResultReport");
            Save(CreateResearchOutcome(false), "ResearchEnding");
        }

        [MenuItem("Border/Research/Rebuild Test Visibility Dialog")]
        public static void RebuildTestVisibilityDialog()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var root = new GameObject("ResearchTestVisibilityDialog");
            root.SetActive(false);
            var controller = root.AddComponent<ResearchTestVisibilityDialog>();
            RectTransform canvas = CanvasRoot(root.transform, "VisibilityCanvas", 35);
            RectTransform panel = Panel(canvas, "VisibilityPanel", new Vector2(800, 460));
            Sprite frame = FindEngineSprite("engine_ui_01_0");
            panel.GetComponent<Image>().sprite = frame;
            panel.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            panel.GetComponent<Image>().color = Color.white;
            Text(panel, "Title", "테스트 방식 선택", new Vector2(740, 44), new Vector2(0, 190), 28);
            TMP_Text mission = Text(panel, "Mission", string.Empty, new Vector2(740, 34), new Vector2(0, 142), 18);
            var group = panel.gameObject.AddComponent<ToggleGroup>();
            Toggle privateToggle = VisibilityCard(panel, group, "PrivateToggle", "비공개 테스트", "PrivateDetails", -185, true, out TMP_Text privateDetails);
            Toggle publicToggle = VisibilityCard(panel, group, "PublicToggle", "공개 테스트", "PublicDetails", 185, false, out TMP_Text publicDetails);
            TMP_Text error = Text(panel, "Error", string.Empty, new Vector2(740, 36), new Vector2(0, -126), 16);
            error.color = new Color(1f, 0.65f, 0.4f);
            Button cancel = Button(panel, "CancelButton", "취소", new Vector2(190, 48), new Vector2(-108, -180));
            Button confirm = Button(panel, "ConfirmButton", "설계 진입", new Vector2(190, 48), new Vector2(108, -180));
            foreach (Button button in new[] { cancel, confirm })
            {
                Image image = button.GetComponent<Image>();
                image.sprite = FindEngineSprite("engine_ui_01_5");
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.color = Color.white;
            }
            Bind(controller, "publicToggle", publicToggle);
            Bind(controller, "privateToggle", privateToggle);
            Bind(controller, "missionText", mission);
            Bind(controller, "publicDetails", publicDetails);
            Bind(controller, "privateDetails", privateDetails);
            Bind(controller, "errorText", error);
            Bind(controller, "confirmButton", confirm);
            Bind(controller, "cancelButton", cancel);
            Save(root, "ResearchTestVisibilityDialog");
        }

        private static Toggle VisibilityCard(Transform parent, ToggleGroup group, string name, string label,
            string detailsName, float x, bool selected, out TMP_Text details)
        {
            RectTransform card = Image(parent, name, new Vector2(350, 210), new Vector2(x, 10), Color.white);
            Image cardImage = card.GetComponent<Image>();
            cardImage.sprite = FindEngineSprite("engine_ui_01_0");
            cardImage.type = UnityEngine.UI.Image.Type.Sliced;
            var toggle = card.gameObject.AddComponent<Toggle>();
            RectTransform box = Image(card, "Box", new Vector2(24, 24), new Vector2(-150, 76), Color.white);
            Image boxImage = box.GetComponent<Image>();
            boxImage.sprite = FindEngineSprite("engine_ui_01_4");
            boxImage.type = UnityEngine.UI.Image.Type.Sliced;
            boxImage.raycastTarget = false;
            TMP_Text title = Text(card, "CardTitle", label, new Vector2(260, 34), new Vector2(12, 76), 22);
            title.alignment = TextAlignmentOptions.Left;
            details = Text(card, detailsName, string.Empty, new Vector2(310, 130), new Vector2(0, -30), 17);
            details.textWrappingMode = TextWrappingModes.Normal;
            RectTransform highlight = Image(card, "Selected", Vector2.zero, Vector2.zero, new Color(0.15f, 0.95f, 1f, 0.14f));
            Stretch(highlight, 0);
            highlight.GetComponent<Image>().raycastTarget = false;
            RectTransform mark = Image(highlight, "Mark", new Vector2(12, 12), new Vector2(-150, 76), new Color(0.15f, 0.95f, 1f));
            mark.GetComponent<Image>().raycastTarget = false;
            toggle.targetGraphic = cardImage;
            toggle.graphic = highlight.GetComponent<Image>();
            toggle.group = group;
            toggle.SetIsOnWithoutNotify(selected);
            return toggle;
        }

        private static Sprite FindEngineSprite(string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath("Assets/05. Arts/UI/Resources/engine_ui_01.psd"))
                if (asset is Sprite sprite && sprite.name == name) return sprite;
            return null;
        }

        private static GameObject CreateResearchOutcome(bool report)
        {
            var root = new GameObject(report ? "ResearchResultReport" : "ResearchEnding");
            root.SetActive(false);
            Component controller = report ? (Component)root.AddComponent<ResearchResultReportController>()
                : root.AddComponent<ResearchEndingController>();
            RectTransform canvas = CanvasRoot(root.transform, report ? "ResearchResultReportCanvas" : "ResearchEndingCanvas", report ? 30 : 40);
            RectTransform panel = Panel(canvas, report ? "ReportPanel" : "EndingPanel", new Vector2(800, 550));
            panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.1f, 1f);
            TMP_Text title = Text(panel, "Title", report ? "결과 보고서" : "MISSION COMPLETE", new Vector2(736, 64), new Vector2(0, 211), 28);
            TMP_Text body = Text(panel, "Body", string.Empty, new Vector2(736, 344), Vector2.zero, 20);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            Button button = Button(panel, report ? "CloseButton" : "RestartButton", report ? "확인" : "다시 시작", new Vector2(240, 48), new Vector2(0, -219));
            Bind(controller, "titleText", title);
            Bind(controller, "bodyText", body);
            Bind(controller, report ? "closeButton" : "restartButton", button);
            return root;
        }

        private static GameObject CreateSettings()
        {
            var root = new GameObject("SettingsMenu", typeof(RectTransform));
            Stretch((RectTransform)root.transform, 0);
            root.SetActive(false);
            var controller = root.AddComponent<SimpleSettingsMenuController>();
            RectTransform canvas = CanvasRoot(root.transform, "SettingsCanvas", 80);
            RectTransform panel = Panel(canvas, "SettingsPanel", new Vector2(520, 420));
            Text(panel, "Title", "설정", new Vector2(440, 48), new Vector2(0, 160), 28);
            TMP_Dropdown dropdown = ResolutionDropdown(panel);
            Slider master = VolumeSlider(panel, "MasterVolume", "전체", 34);
            Slider bgm = VolumeSlider(panel, "BgmVolume", "BGM", -34);
            Slider sfx = VolumeSlider(panel, "SfxVolume", "SFX", -102);
            Button close = Button(panel, "CloseButton", "닫기", new Vector2(180, 44), new Vector2(0, -166));
            Bind(controller, "panelRoot", canvas.gameObject);
            Bind(controller, "resolutionDropdown", dropdown);
            Bind(controller, "masterSlider", master);
            Bind(controller, "bgmSlider", bgm);
            Bind(controller, "sfxSlider", sfx);
            Bind(controller, "closeButton", close);
            canvas.gameObject.SetActive(false);
            root.SetActive(true);
            return root;
        }

        private static GameObject CreatePause()
        {
            var root = new GameObject("PauseMenu");
            root.SetActive(false);
            var controller = root.AddComponent<PauseMenuController>();
            RectTransform canvas = CanvasRoot(root.transform, "PauseMenuCanvas", 90);
            RectTransform panel = Panel(canvas, "PausePanel", new Vector2(380, 320));
            Text(panel, "Title", "일시정지", new Vector2(320, 44), new Vector2(0, 114), 28);
            Bind(controller, "panelRoot", canvas.gameObject);
            Bind(controller, "resumeButton", Button(panel, "ResumeButton", "게임으로 돌아가기", new Vector2(280, 48), new Vector2(0, 42)));
            Bind(controller, "titleButton", Button(panel, "TitleButton", "타이틀로", new Vector2(280, 48), new Vector2(0, -24)));
            Bind(controller, "quitButton", Button(panel, "QuitButton", "게임 종료", new Vector2(280, 48), new Vector2(0, -90)));
            canvas.gameObject.SetActive(false);
            root.SetActive(true);
            return root;
        }

        private static void InstallTitleSettings()
        {
            string path = $"{Folder}/TitleScreen.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Button start = root.transform.Find("NewGameButton").GetComponent<Button>();
                Transform old = root.transform.Find("SettingsButton");
                Button settings = old != null ? old.GetComponent<Button>() : Object.Instantiate(start, start.transform.parent);
                settings.name = "SettingsButton";
                settings.onClick = new Button.ButtonClickedEvent();
                var sourceRect = (RectTransform)start.transform;
                var targetRect = (RectTransform)settings.transform;
                targetRect.anchorMin = sourceRect.anchorMin;
                targetRect.anchorMax = sourceRect.anchorMax;
                targetRect.pivot = sourceRect.pivot;
                targetRect.sizeDelta = sourceRect.sizeDelta;
                targetRect.localScale = sourceRect.localScale;
                targetRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0, -sourceRect.sizeDelta.y - 20);
                TMP_Text label = settings.GetComponentInChildren<TMP_Text>(true);
                label.text = "설정";
                label.font = TMP_Settings.defaultFontAsset;
                label.raycastTarget = false;

                SimpleSettingsMenuController menu = root.GetComponentInChildren<SimpleSettingsMenuController>(true);
                if (menu == null)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/SettingsMenu.prefab");
                    menu = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform)).GetComponent<SimpleSettingsMenuController>();
                }
                TitleMenu title = root.GetComponent<TitleMenu>();
                Bind(title, "newGameButton", start);
                Bind(title, "settingsButton", settings);
                Bind(title, "settingsMenu", menu);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static TMP_Dropdown ResolutionDropdown(Transform parent)
        {
            RectTransform rect = Image(parent, "ResolutionDropdown", new Vector2(400, 42), new Vector2(0, 92), new Color(0.18f, 0.2f, 0.22f));
            var dropdown = rect.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = rect.GetComponent<Image>();
            TMP_Text caption = Text(rect, "Label", "1920 x 1080", new Vector2(340, 36), new Vector2(-10, 0), 18);
            caption.alignment = TextAlignmentOptions.Left;
            dropdown.captionText = caption;
            Text(rect, "Arrow", "v", new Vector2(24, 36), new Vector2(180, 0), 18);
            RectTransform template = Image(rect, "Template", new Vector2(0, 176), Vector2.zero, new Color(0.1f, 0.11f, 0.12f));
            template.anchorMin = Vector2.zero;
            template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(0.5f, 1);
            template.anchoredPosition = new Vector2(0, -4);
            RectTransform viewport = Image(template, "Viewport", Vector2.zero, Vector2.zero, Color.white);
            Stretch(viewport, 4);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform content = Rect(viewport, "Content", new Vector2(0, 36), Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            RectTransform item = Image(content, "Item", new Vector2(0, 36), Vector2.zero, new Color(0.18f, 0.2f, 0.22f));
            item.anchorMin = new Vector2(0, 0.5f);
            item.anchorMax = new Vector2(1, 0.5f);
            Toggle toggle = item.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = item.GetComponent<Image>();
            TMP_Text itemText = Text(item, "Item Label", "1920 x 1080", Vector2.zero, Vector2.zero, 18);
            Stretch(itemText.rectTransform, 12);
            itemText.rectTransform.offsetMin = new Vector2(12, 2);
            itemText.rectTransform.offsetMax = new Vector2(-12, -2);
            itemText.alignment = TextAlignmentOptions.Left;
            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            dropdown.template = template;
            dropdown.itemText = itemText;
            dropdown.options.Add(new TMP_Dropdown.OptionData("1920 x 1080"));
            template.gameObject.SetActive(false);
            return dropdown;
        }

        private static Slider VolumeSlider(Transform parent, string name, string label, float y)
        {
            RectTransform row = Rect(parent, name, new Vector2(400, 44), new Vector2(0, y));
            TMP_Text text = Text(row, "Label", label, new Vector2(90, 36), new Vector2(-155, 0), 20);
            text.alignment = TextAlignmentOptions.Left;
            RectTransform rect = Rect(row, "Slider", new Vector2(270, 28), new Vector2(65, 0));
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            Image(rect, "Background", new Vector2(270, 8), Vector2.zero, new Color(0.25f, 0.27f, 0.29f));
            RectTransform fillArea = Rect(rect, "Fill Area", Vector2.zero, Vector2.zero);
            fillArea.anchorMin = new Vector2(0, 0.5f);
            fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(0, 8);
            RectTransform fill = Image(fillArea, "Fill", Vector2.zero, Vector2.zero, new Color(0.35f, 0.78f, 0.85f));
            Stretch(fill, 0);
            RectTransform handleArea = Rect(rect, "Handle Area", new Vector2(-18, 0), Vector2.zero);
            handleArea.anchorMin = new Vector2(0, 0.5f);
            handleArea.anchorMax = new Vector2(1, 0.5f);
            RectTransform handle = Image(handleArea, "Handle", new Vector2(18, 24), Vector2.zero, Color.white);
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.value = 1;
            return slider;
        }

        private static RectTransform CanvasRoot(Transform parent, string name, int order)
        {
            RectTransform root = Rect(parent, name, Vector2.zero, Vector2.zero);
            Stretch(root, 0);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform blocker = Image(root, "ModalBlocker", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.6f));
            Stretch(blocker, 0);
            return root;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 size) =>
            Image(parent, name, size, Vector2.zero, new Color(0.08f, 0.09f, 0.1f, 0.99f));

        private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static RectTransform Image(Transform parent, string name, Vector2 size, Vector2 position, Color color)
        {
            RectTransform rect = Rect(parent, name, size, position);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text Text(Transform parent, string name, string value, Vector2 size, Vector2 position, int fontSize)
        {
            RectTransform rect = Rect(parent, name, size, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.text = value;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button Button(Transform parent, string name, string label, Vector2 size, Vector2 position)
        {
            RectTransform rect = Image(parent, name, size, position, new Color(0.22f, 0.28f, 0.3f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Text(rect, "Label", label, size, Vector2.zero, 20);
            return button;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = Vector2.one * -padding;
        }

        private static void Bind(Object component, string field, Object value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(GameObject root, string name)
        {
            try { PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab"); }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
