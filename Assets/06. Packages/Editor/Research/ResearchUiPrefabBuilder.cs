using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Editor
{
    public static class ResearchUiPrefabBuilder
    {
        private const string ResourceFolder = "Assets/03. Prefabs/UI/Resources/ResearchUI";

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
                && FindChild(miniGameScreen.transform, "FuelOuterBand") != null
                && FindChild(miniGameScreen.transform, "FuelPerfectBand") != null
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
            return button.gameObject;
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

            RectTransform topBar = CreatePanel("TopInfoBar", root, new Color(0.2f, 0.25f, 0.31f, 0.94f));
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = Vector2.one;
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.offsetMin = new Vector2(16f, -82f);
            topBar.offsetMax = new Vector2(-16f, -16f);
            AddHorizontalLayout(topBar, 12f, 12f, 8f, 8f);

            RectTransform titleGroup = CreateGroup("ProjectTitleGroup", topBar);
            AddVerticalLayout(titleGroup, 0f, 0f, 0f, 2f);
            titleGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateText("Title", titleGroup, 27, FontStyles.Bold, TextAlignmentOptions.Left, "ARTEMIS: 2026 연구 단계");
            CreateText("Subtitle", titleGroup, 14, FontStyles.Normal, TextAlignmentOptions.Left, "첫 엔진에서 시작해 새 엔진 프리셋을 열고, 준비되면 설계로 진입");
            CreateInfoChip("Date", topBar);
            CreateInfoChip("RemainingTurns", topBar);
            CreateInfoChip("Funds", topBar);
            CreateInfoChip("QuarterlyFunding", topBar);
            CreateButton("ResetButton", topBar, "초기화", 86f, 50f);

            RectTransform previewReservedArea = CreateGroup("EnginePreviewReservedArea", root);
            previewReservedArea.anchorMin = Vector2.zero;
            previewReservedArea.anchorMax = Vector2.one;
            previewReservedArea.offsetMin = new Vector2(332f, 16f);
            previewReservedArea.offsetMax = new Vector2(-432f, -98f);

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
            Center(panel, new Vector2(1040f, 560f));
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
            return root.gameObject;
        }

        private static void CreateFuelGame(RectTransform parent)
        {
            RectTransform group = CreateGroup("FuelGame", parent);
            Stretch(group, 0f);
            TMP_Text status = CreateText("FuelStatusText", group, 18, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(status.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.84f));

            RectTransform gaugeFrame = CreatePanel("FuelGaugeFrame", group, new Color(0.14f, 0.18f, 0.23f, 1f));
            Anchor(gaugeFrame, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.62f));
            Image outerBand = CreatePanel("FuelOuterBand", gaugeFrame, new Color(0.28f, 0.78f, 0.42f, 0.38f)).GetComponent<Image>();
            Anchor(outerBand.rectTransform, new Vector2(0.34f, 0f), new Vector2(0.66f, 1f));
            Image perfectBand = CreatePanel("FuelPerfectBand", gaugeFrame, new Color(1f, 0.9f, 0.26f, 0.86f)).GetComponent<Image>();
            Anchor(perfectBand.rectTransform, new Vector2(0.48f, 0f), new Vector2(0.52f, 1f));
            Image fill = CreatePanel("FuelFill", gaugeFrame, new Color(0.26f, 0.74f, 0.88f, 1f)).GetComponent<Image>();
            Anchor(fill.rectTransform, Vector2.zero, new Vector2(0f, 1f));
            Image currentMarker = CreatePanel("FuelCurrentMarker", gaugeFrame, new Color(0.92f, 0.98f, 1f, 1f)).GetComponent<Image>();
            currentMarker.rectTransform.sizeDelta = new Vector2(4f, 0f);
            Image targetMarker = CreatePanel("FuelTarget", gaugeFrame, new Color(1f, 0.88f, 0.24f, 1f)).GetComponent<Image>();
            targetMarker.rectTransform.sizeDelta = new Vector2(7f, 0f);

            TMP_Text label = CreateText("FuelGaugeLabel", gaugeFrame, 15, FontStyles.Bold, TextAlignmentOptions.Center, "목표선");
            label.color = new Color(1f, 0.94f, 0.42f, 1f);
            TMP_Text judgement = CreateText("FuelJudgementText", group, 38, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(judgement.rectTransform, new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.25f));
            group.gameObject.SetActive(false);
        }

        private static void CreateCoolingGame(RectTransform parent)
        {
            RectTransform group = CreateGroup("CoolingGame", parent);
            Stretch(group, 0f);
            RectTransform hotspot = CreatePanel("CoolingHotspot", group, new Color(0.88f, 0.36f, 0.24f, 1f));
            Anchor(hotspot, new Vector2(0.34f, 0.70f), new Vector2(0.66f, 0.90f));
            TMP_Text hotspotLabel = CreateText("CoolingHotspotLabel", hotspot, 16, FontStyles.Bold, TextAlignmentOptions.Center, "뜨거운 엔진 위치");
            Stretch(hotspotLabel.rectTransform, 6f);

            RectTransform valveGrid = CreateGroup("CoolingValveGrid", group);
            Anchor(valveGrid, new Vector2(0.20f, 0.06f), new Vector2(0.80f, 0.55f));
            AddGrid(valveGrid, 2, 16f, 170f, 64f);
            for (int i = 0; i < 4; i++)
            {
                CreateButton($"CoolingValve_{i}", valveGrid, string.Empty, 0f, 0f);
            }

            group.gameObject.SetActive(false);
        }

        private static void CreateOutputGame(RectTransform parent)
        {
            RectTransform group = CreateGroup("OutputGame", parent);
            Stretch(group, 0f);
            TMP_Text label = CreateText("OutputLabel", group, 18, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(label.rectTransform, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.86f));
            RectTransform gaugeFrame = CreatePanel("OutputGaugeFrame", group, new Color(0.14f, 0.18f, 0.23f, 1f));
            Anchor(gaugeFrame, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.58f));
            RectTransform safeZone = CreatePanel("SafeZone", gaugeFrame, new Color(0.22f, 0.72f, 0.38f, 0.82f));
            Anchor(safeZone, new Vector2(0.27f, 0f), new Vector2(0.43f, 1f));
            RectTransform fill = CreatePanel("OutputFill", gaugeFrame, new Color(0.88f, 0.5f, 0.2f, 0.92f));
            Anchor(fill, Vector2.zero, new Vector2(0f, 1f));
            TMP_Text judgement = CreateText("OutputJudgementText", group, 38, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            Anchor(judgement.rectTransform, new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.28f));
            group.gameObject.SetActive(false);
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

        private static void CreateOperationDetails(RectTransform parent)
        {
            RectTransform selectedPanel = CreatePanel("SelectedPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(selectedPanel, 12f, 12f, 10f, 7f);
            selectedPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 238f;
            CreateText("SelectedEngineText", selectedPanel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);

            RectTransform statRow = CreateGroup("StatButtons", selectedPanel);
            AddHorizontalLayout(statRow, 0f, 0f, 0f, 6f);
            statRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            CreateButton("StatButton_FuelCapacity", statRow, "연료량", 0f, 36f);
            CreateButton("StatButton_Cooling", statRow, "냉각", 0f, 36f);
            CreateButton("StatButton_MaxOutput", statRow, "최대 출력", 0f, 36f);
            CreateButton("StatButton_IgnitionReliability", statRow, "점화 신뢰도", 0f, 36f);
            RectTransform actionPanel = CreatePanel("ActionPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(actionPanel, 12f, 12f, 10f, 8f);
            actionPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;
            CreateText("ActionTitle", actionPanel, 18, FontStyles.Bold, TextAlignmentOptions.Left, "행동");
            RectTransform actionRow = CreateGroup("ActionRow", actionPanel);
            AddHorizontalLayout(actionRow, 0f, 0f, 0f, 8f);
            actionRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            CreateButton("NormalResearchButton", actionRow, string.Empty, 0f, 58f);
            CreateButton("FocusedResearchButton", actionRow, string.Empty, 0f, 58f);
            CreateButton("EnterDesignButton", actionRow, string.Empty, 0f, 58f);
            CreateButton("WaitQuarterButton", actionPanel, string.Empty, 0f, 42f);

            RectTransform designPanel = CreatePanel("DesignEntryPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(designPanel, 12f, 12f, 10f, 6f);
            designPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 102f;
            CreateText("DesignEntryTitle", designPanel, 17, FontStyles.Bold, TextAlignmentOptions.Left, "미션 결과 요약");
            CreateText("DesignEntryText", designPanel, 13, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);

            RectTransform statusPanel = CreatePanel("StatusPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(statusPanel, 12f, 12f, 10f, 6f);
            statusPanel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            CreateText("StatusTitle", statusPanel, 17, FontStyles.Bold, TextAlignmentOptions.Left, "상태");
            CreateText("StatusText", statusPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
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

            RectTransform visibilityRow = CreateGroup("VisibilityControls", infoPanel);
            AddHorizontalLayout(visibilityRow, 0f, 0f, 0f, 6f);
            visibilityRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            CreateButton("PublicTestButton", visibilityRow, "공개 테스트", 0f, 38f);
            CreateButton("PrivateTestButton", visibilityRow, "비공개 테스트", 0f, 38f);
            CreateText("InstalledEngineText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            CreateText("StatusText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private static TMP_Text CreateInfoChip(string name, Transform parent)
        {
            RectTransform chip = CreatePanel($"{name}Chip", parent, new Color(0.11f, 0.13f, 0.17f, 1f));
            AddVerticalLayout(chip, 6f, 6f, 5f, 2f);
            chip.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;
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
