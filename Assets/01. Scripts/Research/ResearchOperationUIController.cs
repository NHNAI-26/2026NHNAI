using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchOperationUIController : MonoBehaviour
    {
        private const int StageCount = 4;
        private const int EngineCount = ResearchPrototypeModel.MaxEnginePresetCount;

        private readonly EngineCardView[] engineCards = new EngineCardView[EngineCount];
        private readonly StageCardView[] stageCards = new StageCardView[StageCount];

        private ResearchFlowSession session;
        private ResearchPrototypeModel model;
        private EnginePresetId selectedEnginePreset = EnginePresetId.Engine01;
        private EngineStatId selectedStat = EngineStatId.FuelCapacity;
        private ResearchStageId selectedStage = ResearchStageId.Engine;
        private int selectedResearchScore = 80;
        private bool initialized;
        private RectTransform canvasTransform;
        private ResearchDesignScreenController activeDesignController;

        private TMP_Text dateText;
        private TMP_Text remainingTurnsText;
        private TMP_Text fundsText;
        private TMP_Text quarterlyFundingText;
        private TMP_Text selectedEngineText;
        private TMP_Text selectedStageText;
        private TMP_Text selectedRequirementText;
        private TMP_Text designEntryText;
        private TMP_Text statusText;
        private Button normalResearchButton;
        private TMP_Text normalResearchButtonText;
        private Button focusedResearchButton;
        private TMP_Text focusedResearchButtonText;
        private Button enterDesignButton;
        private TMP_Text enterDesignButtonText;
        private Button waitButton;
        private TMP_Text waitButtonText;

        public ResearchPrototypeModel Model => model;
        public ResearchStageId SelectedStage => selectedStage;
        public EnginePresetId SelectedEnginePreset => selectedEnginePreset;
        public string RequestedScreenName { get; private set; } = ResearchFlowSession.ResearchScreenName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SpawnInMainScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != ResearchFlowSession.MainSceneName || FindFirstObjectByType<ResearchOperationUIController>() != null)
            {
                return;
            }

            var host = new GameObject("Research Operation UI Controller");
            host.AddComponent<ResearchOperationUIController>();
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Initialize();
        }

        public void InitializeForTests()
        {
            Initialize();
        }

        public void RefreshForTests()
        {
            Initialize();
            Refresh();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            session = ResearchFlowSession.GetOrCreate();
            model = session.Model;
            RemoveLegacyPrototypeControllers();
            BuildInterface();
            initialized = true;
            Refresh();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            canvasTransform = CreateGroup("ResearchOperationCanvas", transform);
            Canvas canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasTransform.gameObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreatePanel("Background", canvasTransform, new Color(0.08f, 0.1f, 0.13f, 0.88f));
            Stretch(background, 0f);

            RectTransform panel = CreatePanel("ResearchOperationPanel", canvasTransform, new Color(0.15f, 0.18f, 0.22f, 0.97f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(1180f, 660f);
            AddVerticalLayout(panel, 16f, 16f, 14f, 12f);

            BuildTopBar(panel);
            BuildBody(panel);
        }

        private void BuildTopBar(RectTransform parent)
        {
            RectTransform topBar = CreatePanel("TopInfoBar", parent, new Color(0.2f, 0.25f, 0.31f, 1f));
            AddHorizontalLayout(topBar, 12f, 12f, 8f, 8f);
            topBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;

            RectTransform titleGroup = CreateGroup("ProjectTitleGroup", topBar);
            AddVerticalLayout(titleGroup, 0f, 0f, 0f, 2f);
            titleGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateText("Title", titleGroup, 27, FontStyles.Bold, TextAlignmentOptions.Left, "ARTEMIS: 2026 연구 단계");
            CreateText("Subtitle", titleGroup, 14, FontStyles.Normal, TextAlignmentOptions.Left, "10개 엔진 프리셋을 연구하고, 해금된 발사 단계로 설계 진입");

            dateText = CreateInfoChip("Date", topBar);
            remainingTurnsText = CreateInfoChip("RemainingTurns", topBar);
            fundsText = CreateInfoChip("Funds", topBar);
            quarterlyFundingText = CreateInfoChip("QuarterlyFunding", topBar);

            Button resetButton = CreateButton("ResetButton", topBar, "초기화", 86f, 50f);
            resetButton.onClick.AddListener(() =>
            {
                session.ResetResearch();
                selectedEnginePreset = EnginePresetId.Engine01;
                selectedStage = ResearchStageId.Engine;
                Refresh();
            });
        }

        private void BuildBody(RectTransform parent)
        {
            RectTransform columns = CreateGroup("MainColumns", parent);
            AddHorizontalLayout(columns, 0f, 0f, 0f, 12f);
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            RectTransform engineColumn = CreatePanel("EnginePresetColumn", columns, new Color(0.11f, 0.14f, 0.18f, 1f));
            AddVerticalLayout(engineColumn, 10f, 10f, 10f, 6f);
            engineColumn.gameObject.AddComponent<LayoutElement>().preferredWidth = 300f;
            CreateText("EngineColumnTitle", engineColumn, 18, FontStyles.Bold, TextAlignmentOptions.Left, "엔진 프리셋");
            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                engineCards[(int)config.Id] = CreateEngineCard(engineColumn, config);
            }

            RectTransform stageColumn = CreatePanel("StageColumn", columns, new Color(0.11f, 0.14f, 0.18f, 1f));
            AddVerticalLayout(stageColumn, 10f, 10f, 10f, 8f);
            stageColumn.gameObject.AddComponent<LayoutElement>().preferredWidth = 250f;
            CreateText("StageColumnTitle", stageColumn, 18, FontStyles.Bold, TextAlignmentOptions.Left, "발사 단계");
            foreach (ResearchStageConfig config in ResearchPrototypeModel.GetStageConfigs())
            {
                stageCards[(int)config.Id] = CreateStageCard(stageColumn, config);
            }

            RectTransform detailColumn = CreatePanel("DetailColumn", columns, new Color(0.12f, 0.15f, 0.19f, 1f));
            AddVerticalLayout(detailColumn, 14f, 14f, 12f, 10f);
            detailColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            BuildDetails(detailColumn);
        }

        private void BuildDetails(RectTransform parent)
        {
            RectTransform selectedPanel = CreatePanel("SelectedPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(selectedPanel, 12f, 12f, 10f, 7f);
            selectedPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 215f;
            selectedEngineText = CreateText("SelectedEngineText", selectedPanel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);

            RectTransform statRow = CreateGroup("StatButtons", selectedPanel);
            AddHorizontalLayout(statRow, 0f, 0f, 0f, 6f);
            statRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            CreateStatButton(statRow, EngineStatId.FuelCapacity);
            CreateStatButton(statRow, EngineStatId.Cooling);
            CreateStatButton(statRow, EngineStatId.MaxOutput);
            CreateStatButton(statRow, EngineStatId.IgnitionReliability);

            RectTransform scoreRow = CreateGroup("ScoreButtons", selectedPanel);
            AddHorizontalLayout(scoreRow, 0f, 0f, 0f, 6f);
            scoreRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            CreateScoreButton(scoreRow, "낮음 35", 35);
            CreateScoreButton(scoreRow, "보통 65", 65);
            CreateScoreButton(scoreRow, "높음 85", 85);

            selectedStageText = CreateText("SelectedStageText", selectedPanel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            selectedRequirementText = CreateText("SelectedRequirementText", selectedPanel, 14, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);

            RectTransform actionPanel = CreatePanel("ActionPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(actionPanel, 12f, 12f, 10f, 8f);
            actionPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;
            CreateText("ActionTitle", actionPanel, 18, FontStyles.Bold, TextAlignmentOptions.Left, "행동");
            RectTransform actionRow = CreateGroup("ActionRow", actionPanel);
            AddHorizontalLayout(actionRow, 0f, 0f, 0f, 8f);
            actionRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            normalResearchButton = CreateButton("NormalResearchButton", actionRow, string.Empty, 0f, 58f);
            normalResearchButtonText = normalResearchButton.GetComponentInChildren<TMP_Text>();
            focusedResearchButton = CreateButton("FocusedResearchButton", actionRow, string.Empty, 0f, 58f);
            focusedResearchButtonText = focusedResearchButton.GetComponentInChildren<TMP_Text>();
            enterDesignButton = CreateButton("EnterDesignButton", actionRow, string.Empty, 0f, 58f);
            enterDesignButtonText = enterDesignButton.GetComponentInChildren<TMP_Text>();
            waitButton = CreateButton("WaitQuarterButton", actionPanel, string.Empty, 0f, 42f);
            waitButtonText = waitButton.GetComponentInChildren<TMP_Text>();

            normalResearchButton.onClick.AddListener(() => ExecuteResearch(false));
            focusedResearchButton.onClick.AddListener(() => ExecuteResearch(true));
            enterDesignButton.onClick.AddListener(EnterDesign);
            waitButton.onClick.AddListener(WaitQuarter);

            RectTransform designPanel = CreatePanel("DesignEntryPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(designPanel, 12f, 12f, 10f, 6f);
            designPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 102f;
            CreateText("DesignEntryTitle", designPanel, 17, FontStyles.Bold, TextAlignmentOptions.Left, "설계/발사 결과");
            designEntryText = CreateText("DesignEntryText", designPanel, 13, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);

            RectTransform statusPanel = CreatePanel("StatusPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(statusPanel, 12f, 12f, 10f, 6f);
            statusPanel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            CreateText("StatusTitle", statusPanel, 17, FontStyles.Bold, TextAlignmentOptions.Left, "상태");
            statusText = CreateText("StatusText", statusPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private EngineCardView CreateEngineCard(RectTransform parent, EnginePresetConfig config)
        {
            Button button = CreateButton($"EngineCard_{config.Id}", parent, string.Empty, 0f, 46f);
            DestroyUnityObject(button.GetComponentInChildren<TMP_Text>().gameObject);

            RectTransform content = CreateGroup("Content", button.transform);
            Stretch(content, 7f);
            AddHorizontalLayout(content, 0f, 0f, 0f, 4f);
            TMP_Text title = CreateText("Title", content, 13, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            TMP_Text detail = CreateText("Detail", content, 12, FontStyles.Normal, TextAlignmentOptions.Right, string.Empty);
            detail.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;

            EnginePresetId presetId = config.Id;
            button.onClick.AddListener(() =>
            {
                selectedEnginePreset = presetId;
                session.ClearPendingDesignEntry();
                Refresh();
            });

            return new EngineCardView(button, title, detail);
        }

        private StageCardView CreateStageCard(RectTransform parent, ResearchStageConfig config)
        {
            Button button = CreateButton($"StageCard_{config.Id}", parent, string.Empty, 0f, 86f);
            DestroyUnityObject(button.GetComponentInChildren<TMP_Text>().gameObject);

            RectTransform content = CreateGroup("Content", button.transform);
            Stretch(content, 8f);
            AddVerticalLayout(content, 0f, 0f, 0f, 4f);
            TMP_Text title = CreateText("Title", content, 15, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            TMP_Text requirement = CreateText("Requirement", content, 12, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            TMP_Text detail = CreateText("Detail", content, 12, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);

            ResearchStageId stageId = config.Id;
            button.onClick.AddListener(() =>
            {
                if (!model.GetStage(stageId).Unlocked)
                {
                    return;
                }

                selectedStage = stageId;
                session.ClearPendingDesignEntry();
                Refresh();
            });

            return new StageCardView(button, title, requirement, detail);
        }

        private void CreateStatButton(RectTransform parent, EngineStatId statId)
        {
            Button button = CreateButton($"StatButton_{statId}", parent, ResearchPrototypeModel.GetStatDisplayName(statId), 0f, 36f);
            button.onClick.AddListener(() =>
            {
                selectedStat = statId;
                Refresh();
            });
        }

        private void CreateScoreButton(RectTransform parent, string label, int score)
        {
            Button button = CreateButton($"ScoreButton_{score}", parent, label, 0f, 36f);
            button.onClick.AddListener(() =>
            {
                selectedResearchScore = score;
                Refresh();
            });
        }

        private void ExecuteResearch(bool focused)
        {
            model.ExecuteEngineResearch(selectedEnginePreset, selectedStat, focused, selectedResearchScore);
            session.ClearPendingDesignEntry();
            Refresh();
        }

        private void EnterDesign()
        {
            if (session.TryEnterDesign(selectedStage, selectedEnginePreset, out _) == ResearchActionResult.Success)
            {
                ShowDesignScreen();
                return;
            }

            Refresh();
        }

        private void WaitQuarter()
        {
            model.WaitQuarter();
            session.ClearPendingDesignEntry();
            Refresh();
        }

        private void Refresh()
        {
            dateText.text = $"날짜\n{model.Year} Q{model.Quarter}";
            remainingTurnsText.text = $"남은 분기\n{model.RemainingTurns}";
            fundsText.text = $"연구비\n{model.Funds}";
            quarterlyFundingText.text = $"분기 연구비\n+{model.QuarterlyFunding}";

            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                RefreshEngineCard(config);
            }

            foreach (ResearchStageConfig config in ResearchPrototypeModel.GetStageConfigs())
            {
                RefreshStageCard(config);
            }

            EnginePresetConfig selectedEngineConfig = ResearchPrototypeModel.GetEnginePresetConfig(selectedEnginePreset);
            EnginePresetState selectedEngine = model.GetEnginePreset(selectedEnginePreset);
            ResearchStageConfig selectedStageConfig = ResearchPrototypeModel.GetStageConfig(selectedStage);
            ResearchStageState selectedStageState = model.GetStage(selectedStage);
            ResearchDesignEntryData preview = model.CreateDesignEntry(selectedStage, selectedEnginePreset, CreatePreviewInstalledCounts(selectedStage, selectedEnginePreset), 50, selectedStage == ResearchStageId.Moon ? TestVisibility.FinalMission : TestVisibility.Private);

            selectedEngineText.text = $"{selectedEngineConfig.DisplayName} Lv.{selectedEngine.Level}/{ResearchPrototypeModel.MaxEnginePresetLevel}  "
                + $"성능 {model.CalculateEnginePerformanceScore(selectedEnginePreset)}  설치 {selectedEngineConfig.InstallCost}\n"
                + $"연료 {selectedEngine.FuelCapacity} / 냉각 {selectedEngine.Cooling} / 출력 {selectedEngine.MaxOutput} / 점화 {selectedEngine.IgnitionReliability}\n"
                + $"선택 스탯: {ResearchPrototypeModel.GetStatDisplayName(selectedStat)} / 임시 점수 {selectedResearchScore} / 시험 최고 {GetBestGradeText(selectedEngine)}";
            selectedStageText.text = $"{selectedStageConfig.DisplayName}  발사비 {selectedStageConfig.LaunchCost} / 예상 성공 {model.CalculateSuccessChance(preview)}% / 경험 +{preview.ExperienceBonus}%p";
            selectedRequirementText.text = GetDesignEntryRequirementText(selectedStageConfig, selectedStageState, selectedEngine);

            normalResearchButtonText.text = $"일반 연구\n{selectedEngineConfig.NormalResearchCost} / Lv +{ResearchPrototypeModel.NormalResearchLevelGain}";
            focusedResearchButtonText.text = $"집중 연구\n{selectedEngineConfig.FocusedResearchCost} / Lv +{ResearchPrototypeModel.FocusedResearchLevelGain}";
            enterDesignButtonText.text = $"설계 진입\n발사비 {selectedStageConfig.LaunchCost}";
            waitButtonText.text = $"1분기 대기   비용 0 / 분기 연구비 +{model.QuarterlyFunding}";

            normalResearchButton.interactable = CanResearch(selectedEngine, selectedEngineConfig.NormalResearchCost);
            focusedResearchButton.interactable = CanResearch(selectedEngine, selectedEngineConfig.FocusedResearchCost);
            enterDesignButton.interactable = CanEnterDesign(selectedStageState, selectedStageConfig, selectedEngine);
            waitButton.interactable = !model.DeadlineReached;

            if (session.HasPendingDesignEntry)
            {
                designEntryText.text = FormatDesignEntry(session.PendingDesignEntry);
            }
            else if (session.HasLastLaunchResult)
            {
                designEntryText.text = FormatLaunchResult(session.LastLaunchResult);
            }
            else
            {
                designEntryText.text = "아직 설계 진입 또는 발사 결과가 없습니다.";
            }

            statusText.text = model.DeadlineReached
                ? $"{model.LastMessage}\n2026 Q4 종료. 목표를 확인하세요."
                : model.LastMessage;
        }

        private void RefreshEngineCard(EnginePresetConfig config)
        {
            EnginePresetState engine = model.GetEnginePreset(config.Id);
            EngineCardView card = engineCards[(int)config.Id];
            bool selected = selectedEnginePreset == config.Id;
            card.Button.GetComponent<Image>().color = selected ? new Color(0.28f, 0.35f, 0.42f, 1f) : new Color(0.19f, 0.23f, 0.28f, 1f);
            card.Title.text = config.DisplayName;
            card.Detail.text = $"Lv.{engine.Level} 성능 {model.CalculateEnginePerformanceScore(config.Id)} 최고 {GetBestGradeText(engine)}";
        }

        private void RefreshStageCard(ResearchStageConfig config)
        {
            ResearchStageState stage = model.GetStage(config.Id);
            StageCardView card = stageCards[(int)config.Id];
            bool selected = selectedStage == config.Id;
            bool unlocked = stage.Unlocked;
            card.Button.interactable = unlocked && !model.DeadlineReached;
            card.Button.GetComponent<Image>().color = !unlocked
                ? new Color(0.12f, 0.13f, 0.15f, 1f)
                : selected
                    ? new Color(0.28f, 0.35f, 0.42f, 1f)
                    : new Color(0.19f, 0.23f, 0.28f, 1f);
            card.Title.text = unlocked ? config.DisplayName : $"{config.DisplayName} LOCK";
            card.Requirement.text = unlocked ? "설계/발사 가능 단계" : model.GetUnlockConditionText(config.Id);
            card.Detail.text = $"최고 {GetBestGradeText(stage)} / 발사 {stage.AttemptCount}회";
        }

        private bool CanResearch(EnginePresetState engine, int cost)
        {
            return !model.DeadlineReached && engine.Level < ResearchPrototypeModel.MaxEnginePresetLevel && model.Funds >= cost;
        }

        private bool CanEnterDesign(ResearchStageState stage, ResearchStageConfig config, EnginePresetState engine)
        {
            return !model.DeadlineReached
                && stage.Unlocked
                && (selectedStage != ResearchStageId.Engine || engine.Level >= 1)
                && model.Funds >= config.LaunchCost;
        }

        private string GetDesignEntryRequirementText(ResearchStageConfig config, ResearchStageState stage, EnginePresetState engine)
        {
            if (!stage.Unlocked)
            {
                return $"설계 진입 불가: {model.GetUnlockConditionText(config.Id)}";
            }

            if (selectedStage == ResearchStageId.Engine && engine.Level < 1)
            {
                return $"설계 진입 불가: {ResearchPrototypeModel.GetEnginePresetConfig(selectedEnginePreset).DisplayName} 레벨 1 필요";
            }

            if (model.Funds < config.LaunchCost)
            {
                return $"설계 진입 불가: 연구비 {model.Funds}/{config.LaunchCost}";
            }

            return "설계 진입 가능: 비용과 분기는 발사 전까지 소비하지 않음";
        }

        private int[] CreatePreviewInstalledCounts(ResearchStageId stageId, EnginePresetId presetId)
        {
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            if (stageId != ResearchStageId.Engine)
            {
                counts[(int)presetId] = 1;
            }

            return counts;
        }

        private string FormatDesignEntry(ResearchDesignEntryData data)
        {
            return $"{data.StageId} / {ResearchPrototypeModel.GetEnginePresetConfig(data.SelectedEnginePresetId).DisplayName} / {data.Year} Q{data.Quarter}\n"
                + $"발사비 {data.LaunchCost}, 예약 설치비 {data.ReservedInstallCost}, 설계 적합도 {data.DesignFit}, {ResearchPrototypeModel.GetVisibilityDisplayName(data.Visibility)}\n"
                + $"예상 성공 {model.CalculateSuccessChance(data)}%. 설계 진입은 아직 비용/분기/결과를 소비하지 않음.";
        }

        private static string FormatLaunchResult(ResearchLaunchResultData result)
        {
            return $"{result.StageId} 결과 {result.Grade} / 성공 {result.SuccessChance}% / 굴림 {result.Roll}\n"
                + $"총 비용 {result.TotalCost}, 지원금 +{result.ImmediateFunding}, 분기 연구비 {result.QuarterlyFundingDelta:+#;-#;0}";
        }

        private void ShowDesignScreen()
        {
            RequestedScreenName = ResearchFlowSession.DesignScreenName;
            canvasTransform.gameObject.SetActive(false);

            if (activeDesignController != null)
            {
                DestroyUnityObject(activeDesignController.gameObject);
                activeDesignController = null;
            }

            var host = new GameObject("Research Design Screen Controller");
            host.transform.SetParent(transform, false);
            activeDesignController = host.AddComponent<ResearchDesignScreenController>();
            activeDesignController.Initialize(session, ReturnFromDesignScreen);
        }

        private void ReturnFromDesignScreen()
        {
            if (activeDesignController != null)
            {
                DestroyUnityObject(activeDesignController.gameObject);
                activeDesignController = null;
            }

            RequestedScreenName = ResearchFlowSession.ResearchScreenName;
            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(true);
            }

            if (initialized)
            {
                Refresh();
            }
        }

        public ResearchDesignScreenController GetActiveDesignControllerForTests()
        {
            return activeDesignController;
        }

        public void ReturnFromDesignScreenForTests()
        {
            ReturnFromDesignScreen();
        }

#if UNITY_EDITOR
        public void EnterDesignDebugForEditor()
        {
            Initialize();
            session.ResetResearch();
            selectedEnginePreset = EnginePresetId.Engine01;
            selectedStage = ResearchStageId.Engine;
            model.PrepareDebugDesignEntryState(selectedStage, selectedEnginePreset);

            if (session.TryEnterDesign(selectedStage, selectedEnginePreset, out _) == ResearchActionResult.Success)
            {
                ShowDesignScreen();
            }
        }

#endif
        private static string GetBestGradeText(ResearchStageState stage)
        {
            return stage.HasBestGrade ? stage.BestGrade.ToString() : "-";
        }

        private static string GetBestGradeText(EnginePresetState engine)
        {
            return engine.HasBestGrade ? engine.BestGrade.ToString() : "-";
        }

        private static TMP_Text CreateInfoChip(string name, Transform parent)
        {
            RectTransform chip = CreatePanel(name, parent, new Color(0.11f, 0.13f, 0.17f, 1f));
            AddVerticalLayout(chip, 6f, 6f, 5f, 2f);
            chip.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;
            return CreateText("Text", chip, 13, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
        }

        private static Button CreateButton(string name, Transform parent, string text, float preferredWidth, float preferredHeight)
        {
            RectTransform rectTransform = CreatePanel(name, parent, new Color(0.24f, 0.29f, 0.36f, 1f));
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

            TMP_Text label = CreateText("Label", rectTransform, 13, FontStyles.Bold, TextAlignmentOptions.Center, text);
            Stretch(label.rectTransform, 6f);
            return button;
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

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
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

        private static void Stretch(RectTransform target, float padding)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = new Vector2(padding, padding);
            target.offsetMax = new Vector2(-padding, -padding);
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

            foreach (StandaloneInputModule oldModule in eventSystem.GetComponents<StandaloneInputModule>())
            {
                oldModule.enabled = false;
                DestroyUnityObject(oldModule);
            }

            Type inputSystemUiModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiModuleType == null)
            {
                Debug.LogWarning("InputSystemUIInputModule type was not found. Research UGUI can render, but pointer input may not work.");
                return;
            }

            if (eventSystem.GetComponent(inputSystemUiModuleType) == null)
            {
                eventSystem.gameObject.AddComponent(inputSystemUiModuleType);
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void RemoveLegacyPrototypeControllers()
        {
            foreach (ResearchPrototypeController legacyController in FindObjectsByType<ResearchPrototypeController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                DestroyUnityObject(legacyController.gameObject);
            }
        }

        private readonly struct EngineCardView
        {
            public EngineCardView(Button button, TMP_Text title, TMP_Text detail)
            {
                Button = button;
                Title = title;
                Detail = detail;
            }

            public Button Button { get; }
            public TMP_Text Title { get; }
            public TMP_Text Detail { get; }
        }

        private readonly struct StageCardView
        {
            public StageCardView(Button button, TMP_Text title, TMP_Text requirement, TMP_Text detail)
            {
                Button = button;
                Title = title;
                Requirement = requirement;
                Detail = detail;
            }

            public Button Button { get; }
            public TMP_Text Title { get; }
            public TMP_Text Requirement { get; }
            public TMP_Text Detail { get; }
        }
    }
}
