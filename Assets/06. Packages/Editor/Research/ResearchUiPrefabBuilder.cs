using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Editor
{
    public static class ResearchUiPrefabBuilder
    {
        private const string ResourceFolder = "Assets/03. Prefabs/UI/Resources/ResearchUI";

        // 밑색. ResearchUiArtApplicator 가 스프라이트를 입히면서 흰색으로 덮어쓴다 —
        // 시트를 읽지 못해 적용기가 통째로 넘어갔을 때 패널이 구분되도록 남겨 둔다.
        private static readonly Color TopChipColor = new(0.11f, 0.13f, 0.17f, 1f);
        private static readonly Color DetailPanelColor = new(0.18f, 0.22f, 0.27f, 1f);

        [InitializeOnLoadMethod]
        private static void RebuildMissingDefaultPrefabs()
        {
            EditorApplication.delayCall += UpdateDefaultPrefabs;
        }

        private static void UpdateDefaultPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= UpdatePrefabsAfterPlayMode;
                EditorApplication.playModeStateChanged += UpdatePrefabsAfterPlayMode;
                return;
            }

            EnsureDefaultPrefabsCurrent();
        }

        private static void UpdatePrefabsAfterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= UpdatePrefabsAfterPlayMode;
            EditorApplication.delayCall += UpdateDefaultPrefabs;
        }

        public static void RemoveObsoleteOperationDetails()
        {
            string path = $"{ResourceFolder}/ResearchOperationScreen.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || EditorApplication.isPlayingOrWillChangePlaymode) return;

            string[] obsoleteNames = { "SelectedMissionText", "SelectedStageText", "SelectedRequirementText", "RiskText", "StatInsightText" };
            bool needsUpdate = false;
            foreach (string name in obsoleteNames)
                needsUpdate |= FindChild(prefab.transform, name) != null;
            if (!needsUpdate) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (string name in obsoleteNames)
                {
                    Transform child = FindChild(contents.transform, name);
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [MenuItem("Border/Research/Rebuild UI Prefabs")]
        public static void RebuildUiPrefabs()
        {
            EnsureFolder(ResourceFolder);
            SavePrefab(CreateEnginePresetCard(), "EnginePresetCard");
            SavePrefab(CreateDesignEnginePresetButton(), "DesignEnginePresetButton");
            SavePrefab(CreateOperationScreen(), "ResearchOperationScreen");
            SavePrefab(CreateDesignScreen(), "ResearchDesignScreen");
            SavePrefab(CreateMiniGameScreen(), "ResearchMiniGameScreen");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void EnsureDefaultPrefabsCurrent()
        {
            RemoveObsoleteOperationDetails();
            if (!DefaultPrefabsAreCurrent())
            {
                RebuildUiPrefabs();
            }
        }

        [MenuItem("Border/Research/Rebuild Mini Game UI Prefab")]
        public static void RebuildMiniGamePrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureFolder(ResourceFolder);
            ResearchUiArtApplicator.PrepareMiniGameArt();
            string path = $"{ResourceFolder}/ResearchMiniGameScreen.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                SavePrefab(CreateMiniGameScreen(), "ResearchMiniGameScreen");
            }
            else
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    RectTransform playArea = FindChild(contents.transform, "PlayArea") as RectTransform;
                    if (playArea == null) throw new System.InvalidOperationException("Mini game prefab has no PlayArea.");
                    foreach (string name in new[] { "FuelGame", "CoolingGame", "OutputGame" })
                    {
                        Transform previous = FindChild(playArea, name);
                        if (previous != null) Object.DestroyImmediate(previous.gameObject);
                    }
                    CreateFuelGame(playArea);
                    CreateCoolingGame(playArea);
                    CreateOutputGame(playArea);
                    RectTransform panel = FindChild(contents.transform, "MiniGamePanel") as RectTransform;
                    if (panel != null) Anchor(panel, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
                    ResearchUiArtApplicator.ApplyEngineMiniGameArt(contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
        }

        [MenuItem("Border/Research/Rebuild Operation UI Prefab")]
        public static void RebuildOperationUiPrefab()
        {
            EnsureFolder(ResourceFolder);
            SavePrefab(CreateOperationScreen(), "ResearchOperationScreen");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool DefaultPrefabsAreCurrent()
        {
            GameObject operationScreen = AssetDatabase.LoadAssetAtPath<GameObject>($"{ResourceFolder}/ResearchOperationScreen.prefab");
            GameObject miniGameScreen = AssetDatabase.LoadAssetAtPath<GameObject>($"{ResourceFolder}/ResearchMiniGameScreen.prefab");
            GameObject designScreen = AssetDatabase.LoadAssetAtPath<GameObject>($"{ResourceFolder}/ResearchDesignScreen.prefab");
            return operationScreen != null
                && designScreen != null
                && miniGameScreen != null
                && AssetDatabase.LoadAssetAtPath<GameObject>($"{ResourceFolder}/EnginePresetCard.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>($"{ResourceFolder}/DesignEnginePresetButton.prefab") != null
                && FindChild(operationScreen.transform, "Background") == null
                && FindChild(operationScreen.transform, "EngineCard_Engine01") != null
                && FindChild(designScreen.transform, "DesignPresetButton_Engine01") != null
                && FindChild(operationScreen.transform, "EnginePreviewReservedArea") != null
                && FindChild(operationScreen.transform, "HubActionBar") != null
                && FindChild(operationScreen.transform, "EngineHeaderPanel") != null
                && FindChild(operationScreen.transform, "ResetButton") == null
                && FindChild(operationScreen.transform, "StatusPanel") == null
                && FindChild(miniGameScreen.transform, "OutputJudgementText") != null;
        }

        private static GameObject CreateEnginePresetCard()
        {
            Button button = CreateButtonRoot("EnginePresetCard", 0f, 46f);
            RectTransform content = CreateGroup("Content", button.transform);
            Stretch(content, 7f);
            AddHorizontalLayout(content, 0f, 0f, 0f, 4f);
            TMP_Text title = CreateText("Title", content, 13, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            TMP_Text detail = CreateText("Detail", content, 12, FontStyles.Normal, TextAlignmentOptions.Right, string.Empty);
            detail.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
            ResearchUiArtApplicator.ApplyCard(button.gameObject);
            AddEngineNameEditor(button.gameObject);
            return button.gameObject;
        }

        [MenuItem("Border/Research/Add Preset Name Editors")]
        public static void AddPresetNameEditors()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (string name in new[] { "EnginePresetCard", "ResearchOperationScreen" })
            {
                string path = $"{ResourceFolder}/{name}.prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (name == "EnginePresetCard") AddEngineNameEditor(contents);
                    else
                        foreach (Button button in contents.GetComponentsInChildren<Button>(true))
                            if (button.name.StartsWith("EngineCard_")) AddEngineNameEditor(button.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
        }

        private static void AddEngineNameEditor(GameObject card)
        {
            if (card.GetComponent<EnginePresetNameEditor>() != null)
            {
                LayoutEngineNameEditor(card);
                return;
            }
            Transform content = card.transform.Find("Content");
            HorizontalLayoutGroup horizontal = content.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null) Object.DestroyImmediate(horizontal);
            TMP_Text title = content.Find("Title").GetComponent<TMP_Text>();
            TMP_Text detail = content.Find("Detail").GetComponent<TMP_Text>();
            SetNameRect(title.rectTransform, new Vector2(0, 0.42f), Vector2.one, -48f);
            SetNameRect(detail.rectTransform, Vector2.zero, new Vector2(1, 0.42f), -48f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 10f;
            title.fontSizeMax = 13f;
            title.richText = false;
            detail.alignment = TextAlignmentOptions.Left;
            RectTransform inputRect = CreatePanel("NameInput", content, new Color(0.08f, 0.12f, 0.15f, 1f));
            SetNameRect(inputRect, new Vector2(0, 0.42f), Vector2.one, -48f);
            RectTransform viewport = CreateGroup("Viewport", inputRect);
            Stretch(viewport, 3f);
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text inputText = CreateText("Text", viewport, 13, FontStyles.Normal, TextAlignmentOptions.Left, "");
            Stretch(inputText.rectTransform, 0f);
            inputText.richText = false;
            var input = inputRect.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = inputText;
            input.targetGraphic = inputRect.GetComponent<Image>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            input.characterLimit = 0;
            input.restoreOriginalTextOnEscape = true;
            Button rename = CreateButton("RenameButton", content, "변경", 40f, 28f);
            var renameRect = (RectTransform)rename.transform;
            renameRect.anchorMin = renameRect.anchorMax = new Vector2(1f, 0.5f);
            renameRect.pivot = new Vector2(1f, 0.5f);
            renameRect.anchoredPosition = Vector2.zero;
            renameRect.sizeDelta = new Vector2(40f, 28f);
            TMP_Text label = rename.GetComponentInChildren<TMP_Text>();
            label.fontSize = 11f;
            card.AddComponent<EnginePresetNameEditor>().Configure(title, input, rename, label);
            inputRect.gameObject.SetActive(false);
            LayoutEngineNameEditor(card);
        }

        public static void LayoutEngineNameEditor(GameObject card)
        {
            if (card.GetComponent<EnginePresetNameEditor>() == null) return;
            Transform content = card.transform.Find("Content");
            if (content == null) return;
            HorizontalLayoutGroup horizontal = content.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null) Object.DestroyImmediate(horizontal);
            Stretch((RectTransform)content, 7f);
            RectTransform icon = content.Find("EngineIcon") as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0f, 0.5f);
                icon.anchoredPosition = Vector2.zero;
                icon.sizeDelta = new Vector2(30f, 30f);
            }
            foreach (string name in new[] { "Title", "Detail", "NameInput" })
            {
                RectTransform rect = content.Find(name) as RectTransform;
                if (rect == null) continue;
                bool detail = name == "Detail";
                SetNameRect(rect, detail ? Vector2.zero : new Vector2(0f, 0.42f),
                    detail ? new Vector2(1f, 0.42f) : Vector2.one, -48f);
                rect.offsetMin = new Vector2(38f, 0f);
            }
        }

        private static void SetNameRect(RectTransform rect, Vector2 min, Vector2 max, float right)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(right, 0f);
        }

        private static GameObject CreateDesignEnginePresetButton()
        {
            Button button = CreateButtonRoot("DesignEnginePresetButton", 0f, 34f);
            TMP_Text label = CreateText("Label", button.transform, 13, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Stretch(label.rectTransform, 6f);
            return button.gameObject;
        }

        private static GameObject CreateOperationScreen()
        {
            RectTransform root = CreateCanvasRoot("ResearchOperationScreen", new Vector2(1280f, 720f));

            // 상단 바는 제목·날짜·자금 세 조각으로 나뉜다. 껍데기는 세 조각이 각자 갖고, 이 줄 자체는
            // Image 가 없다 — 배경을 주면 조각을 나눈 것이 다시 한 판으로 보인다. 이름은 유지해야 한다:
            // ResearchOperationTransitionAnimator 가 "TopInfoBar" 로 찾아 슬라이드·페이드한다.
            RectTransform topBar = CreateGroup("TopInfoBar", root);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = Vector2.one;
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.offsetMin = new Vector2(16f, -82f);
            topBar.offsetMax = new Vector2(-16f, -16f);
            AddHorizontalLayout(topBar, 0f, 0f, 0f, 8f);

            RectTransform titleGroup = CreatePanel("ProjectTitleGroup", topBar, TopChipColor);
            AddVerticalLayout(titleGroup, 14f, 14f, 8f, 2f);
            titleGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateText("Title", titleGroup, 24, FontStyles.Bold, TextAlignmentOptions.Left, "ARTEMIS : 2026 연구실");

            CreateInfoChip("Date", topBar, 210f);

            // 다음 분기 예산은 자기 노드 없이 "Funds" 텍스트의 둘째 줄로 들어간다 — 컨트롤러가 두 줄을
            // 한 문자열로 채운다. 노드를 다시 나누면 컨트롤러도 같이 고쳐야 한다.
            CreateInfoChip("Funds", topBar, 200f);

            RectTransform previewReservedArea = CreateGroup("EnginePreviewReservedArea", root);
            previewReservedArea.anchorMin = Vector2.zero;
            previewReservedArea.anchorMax = Vector2.one;
            previewReservedArea.offsetMin = new Vector2(332f, 132f);
            previewReservedArea.offsetMax = new Vector2(-432f, -98f);

            // Hub row. No Image, so it needs no art-applicator entry and never eats raycasts.
            RectTransform hubActionBar = CreateGroup("HubActionBar", root);
            hubActionBar.anchorMin = new Vector2(0.5f, 0f);
            hubActionBar.anchorMax = new Vector2(0.5f, 0f);
            hubActionBar.pivot = new Vector2(0.5f, 0f);
            hubActionBar.anchoredPosition = new Vector2(0f, 40f);
            hubActionBar.sizeDelta = new Vector2(720f, 84f);
            AddHorizontalLayout(hubActionBar, 12f, 12f, 10f, 16f);
            CreateButton("PartDevelopmentButton", hubActionBar, string.Empty, 0f, 64f);
            CreateButton("EnterDesignButton", hubActionBar, string.Empty, 0f, 64f);
            CreateButton("WaitQuarterButton", hubActionBar, string.Empty, 0f, 64f);

            RectTransform engineColumn = CreatePanel("EnginePresetColumn", root, new Color(0.11f, 0.14f, 0.18f, 0.94f));
            engineColumn.anchorMin = Vector2.zero;
            engineColumn.anchorMax = new Vector2(0f, 1f);
            engineColumn.pivot = new Vector2(0f, 0.5f);
            engineColumn.offsetMin = new Vector2(16f, 16f);
            engineColumn.offsetMax = new Vector2(316f, -98f);
            AddVerticalLayout(engineColumn, 10f, 10f, 10f, 6f);
            CreateText("EngineColumnTitle", engineColumn, 18, FontStyles.Bold, TextAlignmentOptions.Left, "엔진 프리셋");
            RectTransform engineCards = CreateGroup("EnginePresetCards", engineColumn);
            AddVerticalLayout(engineCards, 0f, 0f, 0f, 6f);
            engineCards.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                GameObject card = CreateEnginePresetCard();
                card.name = $"EngineCard_{config.Id}";
                card.transform.SetParent(engineCards, false);
            }

            CreateButton("CreateEnginePresetButton", engineColumn, string.Empty, 0f, 42f);

            RectTransform detailColumn = CreatePanel("DetailColumn", root, new Color(0.12f, 0.15f, 0.19f, 0.94f));
            detailColumn.anchorMin = new Vector2(1f, 0f);
            detailColumn.anchorMax = Vector2.one;
            detailColumn.pivot = new Vector2(1f, 0.5f);
            detailColumn.offsetMin = new Vector2(-416f, 16f);
            detailColumn.offsetMax = new Vector2(-16f, -98f);
            AddVerticalLayout(detailColumn, 14f, 14f, 12f, 10f);
            CreateOperationDetails(detailColumn);
            ResearchUiArtApplicator.ApplyOperation(root.gameObject);
            return root.gameObject;
        }

        private static GameObject CreateDesignScreen()
        {
            RectTransform root = CreateCanvasRoot("ResearchDesignScreen", new Vector2(1280f, 720f));
            RectTransform background = CreatePanel("Background", root, new Color(0.07f, 0.08f, 0.11f, 0.93f));
            Stretch(background, 0f);

            RectTransform panel = CreatePanel("DesignBoundaryPanel", root, new Color(0.15f, 0.18f, 0.23f, 0.98f));
            Center(panel, new Vector2(1120f, 660f));
            AddVerticalLayout(panel, 16f, 16f, 14f, 12f);
            CreateText("Header", panel, 28, FontStyles.Bold, TextAlignmentOptions.Left, "설계 테스트");

            RectTransform columns = CreateGroup("Columns", panel);
            AddHorizontalLayout(columns, 0f, 0f, 0f, 14f);
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            CreateMapPanel(columns);
            CreateDesignInfoPanel(columns);

            RectTransform actions = CreateGroup("Actions", panel);
            AddHorizontalLayout(actions, 0f, 0f, 0f, 10f);
            actions.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            CreateButton("ReturnToResearchButton", actions, "연구 단계로 돌아가기", 0f, 56f);
            CreateButton("LaunchButton", actions, string.Empty, 0f, 56f);
            return root.gameObject;
        }

        private static GameObject CreateMiniGameScreen()
        {
            RectTransform root = CreateCanvasRoot("ResearchMiniGameScreen", new Vector2(1280f, 720f));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.sortingOrder = 20;
            RectTransform background = CreatePanel("Background", root, new Color(0.04f, 0.05f, 0.07f, 0.96f));
            Stretch(background, 0f);

            RectTransform panel = CreatePanel("MiniGamePanel", root, new Color(0.13f, 0.16f, 0.2f, 0.98f));
            Anchor(panel, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
            AddVerticalLayout(panel, 20f, 20f, 18f, 10f);

            RectTransform topRow = CreateGroup("TopRow", panel);
            AddHorizontalLayout(topRow, 0f, 0f, 0f, 10f);
            topRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            TMP_Text title = CreateText("Title", topRow, 24, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            TMP_Text timer = CreateText("Timer", topRow, 18, FontStyles.Bold, TextAlignmentOptions.Right, string.Empty);
            timer.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

            TMP_Text instruction = CreateText("Instruction", panel, 16, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            instruction.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

            RectTransform playArea = CreatePanel("PlayArea", panel, new Color(0.07f, 0.09f, 0.12f, 1f));
            playArea.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            CreateFuelGame(playArea);
            CreateCoolingGame(playArea);
            CreateOutputGame(playArea);
            CreateIgnitionGame(playArea);
            CreateResultGame(playArea);

            TMP_Text state = CreateText("State", panel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            state.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            CreateButton("PrimaryActionButton", panel, string.Empty, 0f, 54f);
            ResearchUiArtApplicator.ApplyMiniGame(root.gameObject);
            return root.gameObject;
        }

        private static void CreateFuelGame(RectTransform parent)
        {
            RectTransform group = CreateGroup("FuelGame", parent);
            Stretch(group, 0f);
            TMP_Text status = CreateText("FuelStatusText", group, 18, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(status.rectTransform, new Vector2(0.08f, 0.11f), new Vector2(0.92f, 0.20f));
            RectTransform area = CreateGroup("FuelDialArea", group);
            Anchor(area, new Vector2(0.05f, 0.23f), new Vector2(0.95f, 0.98f));
            RectTransform dial = CreateArtSurface("FuelDial", area, Vector2.zero, Vector2.one, true);
            FitAspect(dial, 1f);
            CreateArtSurface("FuelFill", dial, new Vector2(0.16f, 0.328125f), new Vector2(0.84f, 0.668125f));
            RectTransform needle = CreateArtSurface("FuelNeedle", dial, new Vector2(0.467f, 0.328125f), new Vector2(0.533f, 0.63f));
            needle.pivot = new Vector2(0.5f, 0f);
            needle.localRotation = Quaternion.Euler(0f, 0f, ResearchMiniGameController.FuelMinimumAngle);
            CreateArtSurface("FuelHub", dial, new Vector2(0.43f, 0.258125f), new Vector2(0.57f, 0.398125f));
            CreateArtSurface("FuelReadout", dial, Vector2.zero, Vector2.one);
            TMP_Text judgement = CreateText("FuelJudgementText", group, 28, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(judgement.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.92f, 0.10f));
            group.gameObject.SetActive(false);
        }

        private static void CreateCoolingGame(RectTransform parent)
        {
            RectTransform group = CreateGroup("CoolingGame", parent);
            Stretch(group, 0f);
            RectTransform pipeArea = CreateGroup("CoolingPipeArea", group);
            Anchor(pipeArea, new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.83f));
            RectTransform pipe = CreateArtSurface("CoolingPipe", pipeArea, Vector2.zero, Vector2.one);
            FitAspect(pipe, 6.4f);
            RectTransform valveArea = CreateGroup("CoolingValveArea", group);
            Anchor(valveArea, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.88f));
            RectTransform valve = CreateArtSurface("CoolingValve", valveArea, Vector2.zero, Vector2.one, true);
            FitAspect(valve, 1f);
            TMP_Text progress = CreateText("CoolingProgressText", group, 22, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(progress.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.16f));
            group.gameObject.SetActive(false);
        }

        private static void CreateOutputGame(RectTransform parent)
        {
            // The runtime pointer handler lives on this group, including the space outside the track.
            RectTransform group = CreatePanel("OutputGame", parent, Color.clear);
            group.GetComponent<Image>().raycastTarget = true;
            Stretch(group, 0f);
            RectTransform background = CreateArtSurface("OutputBackground", group, new Vector2(0.035f, 0.04f), new Vector2(0.965f, 0.96f));
            background.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.18f, 1f);
            TMP_Text label = CreateText("OutputLabel", group, 18, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(label.rectTransform, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.87f));
            RectTransform trackArea = CreateGroup("OutputTrackArea", group);
            Anchor(trackArea, new Vector2(0.09f, 0.37f), new Vector2(0.91f, 0.66f));
            RectTransform track = CreateArtSurface("OutputTrack", trackArea, Vector2.zero, Vector2.one, true);
            FitAspect(track, 128f / 18f);
            CreateArtSurface("SafeZone", track, new Vector2(0.42f, 0.11f), new Vector2(0.58f, 0.89f));
            RectTransform safeZone = (RectTransform)FindChild(track, "SafeZone");
            RectTransform perfect = CreateArtSurface("PerfectZone", safeZone, new Vector2(0.375f, 0f), new Vector2(0.625f, 1f));
            perfect.GetComponent<Image>().color = new Color(1f, 0.9f, 0.15f, 1f);
            RectTransform cursor = CreateArtSurface("OutputCursor", track, new Vector2(0f, 0.05f), new Vector2(0f, 0.95f));
            cursor.sizeDelta = new Vector2(18f, 0f);
            AspectRatioFitter cursorAspect = cursor.gameObject.AddComponent<AspectRatioFitter>();
            cursorAspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            cursorAspect.aspectRatio = 7f / 16f;
            TMP_Text judgement = CreateText("OutputJudgementText", group, 32, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(judgement.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.29f));
            group.gameObject.SetActive(false);
        }

        private static RectTransform CreateArtSurface(string name, Transform parent, Vector2 min, Vector2 max, bool raycast = false)
        {
            RectTransform rect = CreatePanel(name, parent, Color.white);
            Anchor(rect, min, max);
            rect.GetComponent<Image>().raycastTarget = raycast;
            return rect;
        }

        private static void FitAspect(RectTransform rect, float ratio)
        {
            AspectRatioFitter aspect = rect.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = ratio;
        }

        private static void CreateIgnitionGame(RectTransform parent)
        {
            RectTransform group = CreateGroup("IgnitionGame", parent);
            Stretch(group, 0f);
            RectTransform igniterGrid = CreateGroup("IgniterGrid", group);
            Anchor(igniterGrid, new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.78f));
            AddGrid(igniterGrid, 2, 18f, 136f, 88f);
            for (int i = 0; i < 4; i++)
            {
                CreateButton($"Igniter_{i}", igniterGrid, (i + 1).ToString(), 0f, 0f);
            }

            group.gameObject.SetActive(false);
        }

        private static void CreateResultGame(RectTransform parent)
        {
            RectTransform group = CreateGroup("ResultGame", parent);
            Stretch(group, 0f);
            TMP_Text score = CreateText("ResultScoreText", group, 30, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(score.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.82f));
            TMP_Text detail = CreateText("ResultDetailText", group, 21, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(detail.rectTransform, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.56f));
            group.gameObject.SetActive(false);
        }

        /// <summary>
        /// 엔진 상세를 세 패널로 나눈다: 머리말(이름·성능·설치비·완성도), 스탯 슬라이더, 연구 조작.
        /// 예전에는 SelectedPanel 한 장이 전부를 담았고 이름·성능·비용이 TMP 하나에 문자열로 뭉쳐 있어
        /// 좌우 정렬을 나눌 수 없었다. 노드 이름은 곧 계약이다 — 컨트롤러가 이름으로만 찾는다.
        /// </summary>
        private static void CreateOperationDetails(RectTransform parent)
        {
            RectTransform headerPanel = CreatePanel("EngineHeaderPanel", parent, DetailPanelColor);
            AddVerticalLayout(headerPanel, 12f, 12f, 10f, 6f);

            RectTransform headerRow = CreateGroup("EngineHeaderRow", headerPanel);
            AddHorizontalLayout(headerRow, 0f, 0f, 0f, 8f);
            LayoutElement headerRowLayout = headerRow.gameObject.AddComponent<LayoutElement>();
            headerRowLayout.preferredHeight = 26f;
            headerRowLayout.flexibleHeight = 0f;
            TMP_Text engineName = CreateText("SelectedEngineName", headerRow, 18, FontStyles.Bold,
                TextAlignmentOptions.Left, string.Empty);
            engineName.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateText("SelectedEnginePerformance", headerRow, 15, FontStyles.Bold,
                TextAlignmentOptions.Right, string.Empty);

            CreateText("SelectedEngineInstallCost", headerPanel, 12, FontStyles.Normal,
                TextAlignmentOptions.Left, string.Empty);
            CreateText("SelectedEngineText", headerPanel, 13, FontStyles.Normal,
                TextAlignmentOptions.Left, string.Empty);
            // SelectedEngineCompletion 슬라이더는 ResearchUiArtApplicator 가 이 패널의 마지막 자식으로 넣는다.

            RectTransform statsPanel = CreatePanel("EngineStatsPanel", parent, DetailPanelColor);
            AddVerticalLayout(statsPanel, 12f, 12f, 10f, 4f);
            RectTransform statRows = CreateGroup("StatRows", statsPanel);
            AddVerticalLayout(statRows, 0f, 0f, 0f, 4f);
            LayoutElement statRowsLayout = statRows.gameObject.AddComponent<LayoutElement>();
            statRowsLayout.preferredHeight = 100f;
            statRowsLayout.flexibleHeight = 0f;
            foreach (EngineStatId stat in System.Enum.GetValues(typeof(EngineStatId)))
            {
                RectTransform statGaugeRow = CreateGroup($"StatRow_{stat}", statRows);
                AddHorizontalLayout(statGaugeRow, 0f, 0f, 0f, 8f);
                LayoutElement statGaugeRowLayout = statGaugeRow.gameObject.AddComponent<LayoutElement>();
                statGaugeRowLayout.preferredHeight = 22f;
                statGaugeRowLayout.flexibleHeight = 0f;
                TMP_Text statLabel = CreateText($"StatRowLabel_{stat}", statGaugeRow, 12, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
                LayoutElement statLabelLayout = statLabel.gameObject.AddComponent<LayoutElement>();
                statLabelLayout.preferredWidth = 120f;
                statLabelLayout.flexibleWidth = 0f;
            }

            RectTransform researchPanel = CreatePanel("EngineResearchPanel", parent, DetailPanelColor);
            AddVerticalLayout(researchPanel, 12f, 12f, 10f, 7f);
            CreateText("SelectedStatText", researchPanel, 12, FontStyles.Normal,
                TextAlignmentOptions.Left, string.Empty);

            RectTransform statRow = CreateGroup("StatButtons", researchPanel);
            AddHorizontalLayout(statRow, 0f, 0f, 0f, 6f);
            LayoutElement statRowLayout = statRow.gameObject.AddComponent<LayoutElement>();
            statRowLayout.preferredHeight = 36f;
            statRowLayout.flexibleHeight = 0f;
            CreateButton("StatButton_FuelCapacity", statRow, "연료량", 0f, 36f);
            CreateButton("StatButton_Cooling", statRow, "냉각", 0f, 36f);
            CreateButton("StatButton_MaxOutput", statRow, "최대 출력", 0f, 36f);
            CreateButton("StatButton_IgnitionReliability", statRow, "점화 신뢰도", 0f, 36f);

            RectTransform modeRow = CreateGroup("ResearchModeButtons", researchPanel);
            AddHorizontalLayout(modeRow, 0f, 0f, 0f, 8f);
            LayoutElement modeRowLayout = modeRow.gameObject.AddComponent<LayoutElement>();
            modeRowLayout.preferredHeight = 52f;
            modeRowLayout.flexibleHeight = 0f;
            CreateButton("NormalResearchButton", modeRow, string.Empty, 0f, 52f);
            CreateButton("FocusedResearchButton", modeRow, string.Empty, 0f, 52f);

            CreateGroup("DetailSpacer", parent).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            RectTransform startRow = CreateGroup("StartDevelopmentRow", parent);
            AddHorizontalLayout(startRow, 0f, 0f, 0f, 8f);
            startRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            LayoutElement startRowLayout = startRow.gameObject.AddComponent<LayoutElement>();
            startRowLayout.preferredHeight = 52f;
            startRowLayout.flexibleHeight = 0f;
            CreateButton("StartDevelopmentButton", startRow, "개발 시작", 168f, 52f);
            // 부품 개발 화면을 닫는 유일한 버튼 — 컨트롤러가 필수로 찾으므로 빠지면 화면이 뜨지 않는다.
            CreateButton("CancelDevelopmentButton", startRow, "그만두기", 168f, 52f);
        }

        private static void CreateMapPanel(RectTransform parent)
        {
            RectTransform mapPanel = CreatePanel("MapPanel", parent, new Color(0.08f, 0.11f, 0.15f, 1f));
            AddVerticalLayout(mapPanel, 12f, 12f, 10f, 8f);
            mapPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0.9f;
            CreateText("MapTitle", mapPanel, 19, FontStyles.Bold, TextAlignmentOptions.Left, "맵 / 목표 경로");

            RectTransform viewport = CreatePanel("MapViewport", mapPanel, new Color(0.04f, 0.06f, 0.08f, 1f));
            viewport.gameObject.AddComponent<LayoutElement>().preferredHeight = 250f;
            Anchor(CreatePanel("GridA", viewport, new Color(0.18f, 0.27f, 0.36f, 0.45f)), new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.22f));
            Anchor(CreatePanel("GridB", viewport, new Color(0.18f, 0.27f, 0.36f, 0.45f)), new Vector2(0.12f, 0.55f), new Vector2(0.92f, 0.57f));
            RectTransform path = CreatePanel("TargetPath", viewport, new Color(0.32f, 0.8f, 0.72f, 1f));
            Anchor(path, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.25f));
            path.pivot = new Vector2(0.5f, 0.5f);
            path.localRotation = Quaternion.Euler(0f, 0f, 22f);

            TMP_Text start = CreateText("StartPoint", viewport, 15, FontStyles.Bold, TextAlignmentOptions.Left, "START");
            Anchor(start.rectTransform, new Vector2(0.12f, 0.13f), new Vector2(0.32f, 0.23f));
            TMP_Text target = CreateText("TargetPoint", viewport, 15, FontStyles.Bold, TextAlignmentOptions.Right, "TARGET");
            Anchor(target.rectTransform, new Vector2(0.67f, 0.75f), new Vector2(0.9f, 0.85f));
        }

        private static void CreateDesignInfoPanel(RectTransform parent)
        {
            RectTransform infoPanel = CreatePanel("InfoPanel", parent, new Color(0.12f, 0.15f, 0.2f, 1f));
            AddVerticalLayout(infoPanel, 12f, 12f, 10f, 8f);
            infoPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.35f;

            CreateText("InfoTitle", infoPanel, 19, FontStyles.Bold, TextAlignmentOptions.Left, "설계 진입 정보");
            CreateText("DesignDataText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            RectTransform presetRow = CreateGroup("PresetButtons", infoPanel);
            AddHorizontalLayout(presetRow, 0f, 0f, 0f, 5f);
            presetRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            for (int i = 0; i < ResearchPrototypeModel.MaxEnginePresetCount; i++)
            {
                CreateButton($"DesignPresetButton_{(EnginePresetId)i}", presetRow, (i + 1).ToString("00"), 0f, 34f);
            }

            RectTransform designRow = CreateGroup("DesignControls", infoPanel);
            AddHorizontalLayout(designRow, 0f, 0f, 0f, 6f);
            designRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            CreateButton("RemoveEngineButton", designRow, "- 엔진", 0f, 38f);
            CreateButton("AddEngineButton", designRow, "+ 엔진", 0f, 38f);
            CreateButton("DesignFitDownButton", designRow, "적합도 -10", 0f, 38f);
            CreateButton("DesignFitUpButton", designRow, "적합도 +10", 0f, 38f);

            CreateText("InstalledEngineText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            CreateText("StatusText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private static TMP_Text CreateInfoChip(string name, Transform parent, float preferredWidth)
        {
            RectTransform chip = CreatePanel($"{name}Chip", parent, TopChipColor);
            AddVerticalLayout(chip, 10f, 10f, 8f, 2f);
            chip.gameObject.AddComponent<LayoutElement>().preferredWidth = preferredWidth;
            return CreateText(name, chip, 13, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
        }

        private static Button CreateButton(string name, Transform parent, string text, float preferredWidth, float preferredHeight)
        {
            Button button = CreateButtonRoot(name, preferredWidth, preferredHeight);
            button.transform.SetParent(parent, false);
            TMP_Text label = CreateText("Label", button.transform, 13, FontStyles.Bold, TextAlignmentOptions.Center, text);
            Stretch(label.rectTransform, 6f);
            return button;
        }

        private static Button CreateButtonRoot(string name, float preferredWidth, float preferredHeight)
        {
            RectTransform rectTransform = CreatePanel(name, null, new Color(0.24f, 0.29f, 0.36f, 1f));
            LayoutElement layout = rectTransform.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth > 0f)
            {
                layout.preferredWidth = preferredWidth;
            }
            else
            {
                layout.flexibleWidth = 1f;
            }

            layout.preferredHeight = preferredHeight;
            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = rectTransform.GetComponent<Image>();
            button.colors = CreateButtonColors();
            return button;
        }

        private static RectTransform CreateCanvasRoot(string name, Vector2 referenceResolution)
        {
            var rootObject = new GameObject(name, typeof(RectTransform));
            RectTransform root = (RectTransform)rootObject.transform;
            Canvas canvas = rootObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = rootObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                panel.transform.SetParent(parent, false);
            }

            Image image = panel.AddComponent<Image>();
            image.color = color;
            return (RectTransform)panel.transform;
        }

        private static RectTransform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            return (RectTransform)group.transform;
        }

        private static TMP_Text CreateText(string name, Transform parent, int fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, string text)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static void AddVerticalLayout(RectTransform target, float left, float right, float top, float spacing)
        {
            VerticalLayoutGroup layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)left, (int)right, (int)top, (int)top);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void AddHorizontalLayout(RectTransform target, float left, float right, float top, float spacing)
        {
            HorizontalLayoutGroup layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)left, (int)right, (int)top, (int)top);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void AddGrid(RectTransform target, int columns, float spacing, float width, float height)
        {
            GridLayoutGroup grid = target.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(width, height);
            grid.spacing = new Vector2(spacing, spacing);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(24, 24, 28, 28);
        }

        private static ColorBlock CreateButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.86f, 0.94f, 1f);
            colors.selectedColor = new Color(0.9f, 0.95f, 1f, 1f);
            colors.disabledColor = new Color(0.42f, 0.45f, 0.48f, 0.72f);
            return colors;
        }

        private static void Stretch(RectTransform target, float padding)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = new Vector2(padding, padding);
            target.offsetMax = new Vector2(-padding, -padding);
        }

        private static void Center(RectTransform target, Vector2 size)
        {
            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = Vector2.zero;
            target.sizeDelta = size;
        }

        private static void Anchor(RectTransform target, Vector2 min, Vector2 max)
        {
            target.anchorMin = min;
            target.anchorMax = max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static void SavePrefab(GameObject root, string name)
        {
            string path = $"{ResourceFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
