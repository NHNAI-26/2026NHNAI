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
        private const string TargetSceneName = "ResearchTestScene";
        private const int StageCount = 4;

        private readonly StageCardView[] stageCards = new StageCardView[StageCount];

        private ResearchPrototypeModel model;
        private ResearchStageId selectedStage;
        private ResearchDesignEntryData? pendingDesignEntry;

        private TMP_Text titleText;
        private TMP_Text dateText;
        private TMP_Text remainingTurnsText;
        private TMP_Text fundsText;
        private TMP_Text quarterlyFundingText;
        private TMP_Text selectedStageTitleText;
        private TMP_Text selectedStageMetaText;
        private TMP_Text selectedStageChanceText;
        private TMP_Text selectedStageRequirementText;
        private TMP_Text designEntryText;
        private TMP_Text statusText;
        private Image selectedStageProgressFill;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SpawnInResearchTestScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != TargetSceneName || FindFirstObjectByType<ResearchOperationUIController>() != null)
            {
                return;
            }

            var host = new GameObject("Research Operation UI Controller");
            host.AddComponent<ResearchOperationUIController>();
        }

        private void Awake()
        {
            model ??= new ResearchPrototypeModel();
            RemoveLegacyPrototypeControllers();
            BuildInterface();
            Refresh();
        }

        public void RefreshForTests()
        {
            Refresh();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            RectTransform canvasTransform = CreateGroup("ResearchOperationCanvas", transform);
            Canvas canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasTransform.gameObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var background = CreatePanel("Background", canvasTransform, new Color(0.08f, 0.1f, 0.13f, 0.86f));
            Stretch(background, 0f);

            RectTransform panel = CreatePanel("ResearchOperationPanel", canvasTransform, new Color(0.15f, 0.18f, 0.22f, 0.96f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(1120f, 640f);
            AddVerticalLayout(panel, 18f, 18f, 16f, 12f);

            BuildTopBar(panel);
            BuildMainColumns(panel);
        }

        private void BuildTopBar(RectTransform parent)
        {
            RectTransform topBar = CreatePanel("TopInfoBar", parent, new Color(0.2f, 0.25f, 0.31f, 1f));
            AddHorizontalLayout(topBar, 14f, 14f, 10f, 10f);
            topBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;

            RectTransform titleGroup = CreateGroup("ProjectTitleGroup", topBar);
            AddVerticalLayout(titleGroup, 0f, 0f, 0f, 2f);
            titleGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            titleText = CreateText("Title", titleGroup, 30, FontStyles.Bold, TextAlignmentOptions.Left, "ARTEMIS: 2026");
            CreateText("Subtitle", titleGroup, 15, FontStyles.Normal, TextAlignmentOptions.Left, "연구비와 마감 사이에서 다음 행동을 고른다.");

            dateText = CreateInfoChip("Date", topBar);
            remainingTurnsText = CreateInfoChip("RemainingTurns", topBar);
            fundsText = CreateInfoChip("Funds", topBar);
            quarterlyFundingText = CreateInfoChip("QuarterlyFunding", topBar);

            Button resetButton = CreateButton("ResetButton", topBar, "초기화", 92f, 52f);
            resetButton.onClick.AddListener(() =>
            {
                model.Reset();
                selectedStage = ResearchStageId.Engine;
                pendingDesignEntry = null;
                Refresh();
            });
        }

        private void BuildMainColumns(RectTransform parent)
        {
            RectTransform columns = CreateGroup("MainColumns", parent);
            AddHorizontalLayout(columns, 0f, 0f, 0f, 16f);
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            RectTransform stageColumn = CreatePanel("StageColumn", columns, new Color(0.12f, 0.15f, 0.19f, 1f));
            AddVerticalLayout(stageColumn, 14f, 14f, 14f, 10f);
            stageColumn.gameObject.AddComponent<LayoutElement>().preferredWidth = 360f;
            CreateText("StageColumnTitle", stageColumn, 20, FontStyles.Bold, TextAlignmentOptions.Left, "프로젝트 단계");

            foreach (ResearchStageConfig config in ResearchPrototypeModel.GetStageConfigs())
            {
                stageCards[(int)config.Id] = CreateStageCard(stageColumn, config);
            }

            RectTransform detailColumn = CreatePanel("DetailColumn", columns, new Color(0.12f, 0.15f, 0.19f, 1f));
            AddVerticalLayout(detailColumn, 16f, 16f, 14f, 12f);
            detailColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            BuildSelectedStagePanel(detailColumn);
            BuildActionPanel(detailColumn);
            BuildDesignEntryPanel(detailColumn);
            BuildStatusPanel(detailColumn);
        }

        private void BuildSelectedStagePanel(RectTransform parent)
        {
            RectTransform panel = CreatePanel("SelectedStagePanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(panel, 14f, 14f, 12f, 8f);
            panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 185f;

            selectedStageTitleText = CreateText("SelectedStageTitle", panel, 24, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);

            RectTransform progressRoot = CreatePanel("SelectedStageProgress", panel, new Color(0.05f, 0.06f, 0.08f, 1f));
            progressRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            selectedStageProgressFill = CreatePanel("SelectedStageProgressFill", progressRoot, new Color(0.28f, 0.78f, 0.62f, 1f)).GetComponent<Image>();
            selectedStageProgressFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            selectedStageProgressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            selectedStageProgressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            selectedStageProgressFill.rectTransform.offsetMin = Vector2.zero;
            selectedStageProgressFill.rectTransform.offsetMax = Vector2.zero;

            selectedStageMetaText = CreateText("SelectedStageMeta", panel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            selectedStageChanceText = CreateText("SelectedStageChance", panel, 16, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            selectedStageRequirementText = CreateText("SelectedStageRequirement", panel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private void BuildActionPanel(RectTransform parent)
        {
            RectTransform panel = CreatePanel("ActionPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(panel, 14f, 14f, 12f, 10f);
            panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 172f;

            CreateText("ActionPanelTitle", panel, 20, FontStyles.Bold, TextAlignmentOptions.Left, "행동");

            RectTransform row = CreateGroup("PrimaryActions", panel);
            AddHorizontalLayout(row, 0f, 0f, 0f, 10f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
            normalResearchButton = CreateButton("NormalResearchButton", row, string.Empty, 0f, 62f);
            normalResearchButtonText = normalResearchButton.GetComponentInChildren<TMP_Text>();
            focusedResearchButton = CreateButton("FocusedResearchButton", row, string.Empty, 0f, 62f);
            focusedResearchButtonText = focusedResearchButton.GetComponentInChildren<TMP_Text>();
            enterDesignButton = CreateButton("EnterDesignButton", row, string.Empty, 0f, 62f);
            enterDesignButtonText = enterDesignButton.GetComponentInChildren<TMP_Text>();

            waitButton = CreateButton("WaitQuarterButton", panel, string.Empty, 0f, 44f);
            waitButtonText = waitButton.GetComponentInChildren<TMP_Text>();

            normalResearchButton.onClick.AddListener(() => ExecuteResearch(false));
            focusedResearchButton.onClick.AddListener(() => ExecuteResearch(true));
            enterDesignButton.onClick.AddListener(EnterDesign);
            waitButton.onClick.AddListener(WaitQuarter);
        }

        private void BuildDesignEntryPanel(RectTransform parent)
        {
            RectTransform panel = CreatePanel("DesignEntryPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(panel, 14f, 14f, 10f, 6f);
            panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 102f;
            CreateText("DesignEntryTitle", panel, 18, FontStyles.Bold, TextAlignmentOptions.Left, "설계 진입 데이터");
            designEntryText = CreateText("DesignEntryText", panel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private void BuildStatusPanel(RectTransform parent)
        {
            RectTransform panel = CreatePanel("StatusPanel", parent, new Color(0.18f, 0.22f, 0.27f, 1f));
            AddVerticalLayout(panel, 14f, 14f, 10f, 6f);
            panel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            CreateText("StatusTitle", panel, 18, FontStyles.Bold, TextAlignmentOptions.Left, "상태");
            statusText = CreateText("StatusText", panel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private StageCardView CreateStageCard(RectTransform parent, ResearchStageConfig config)
        {
            Button button = CreateButton($"StageCard_{config.Id}", parent, string.Empty, 0f, 88f);
            button.GetComponent<Image>().color = new Color(0.19f, 0.23f, 0.28f, 1f);
            DestroyUnityObject(button.GetComponentInChildren<TMP_Text>().gameObject);

            RectTransform content = CreateGroup("Content", button.transform);
            Stretch(content, 10f);
            AddVerticalLayout(content, 0f, 0f, 0f, 5f);

            TMP_Text title = CreateText("Title", content, 17, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            TMP_Text progress = CreateText("Progress", content, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            RectTransform progressRoot = CreatePanel("ProgressBar", content, new Color(0.05f, 0.06f, 0.08f, 1f));
            progressRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 10f;
            Image progressFill = CreatePanel("ProgressFill", progressRoot, new Color(0.28f, 0.78f, 0.62f, 1f)).GetComponent<Image>();
            progressFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            progressFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            progressFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            progressFill.rectTransform.offsetMin = Vector2.zero;
            progressFill.rectTransform.offsetMax = Vector2.zero;
            TMP_Text details = CreateText("Details", content, 12, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);

            ResearchStageId stageId = config.Id;
            button.onClick.AddListener(() =>
            {
                if (!model.GetStage(stageId).Unlocked)
                {
                    return;
                }

                selectedStage = stageId;
                pendingDesignEntry = null;
                Refresh();
            });

            return new StageCardView(button, title, progress, details, progressFill);
        }

        private void ExecuteResearch(bool focused)
        {
            model.ExecuteResearch(selectedStage, focused);
            pendingDesignEntry = null;
            Refresh();
        }

        private void EnterDesign()
        {
            if (model.TryEnterDesign(selectedStage, out ResearchDesignEntryData data) == ResearchActionResult.Success)
            {
                pendingDesignEntry = data;
            }
            else
            {
                pendingDesignEntry = null;
            }

            Refresh();
        }

        private void WaitQuarter()
        {
            model.WaitQuarter();
            pendingDesignEntry = null;
            Refresh();
        }

        private void Refresh()
        {
            ResearchStageConfig selectedConfig = ResearchPrototypeModel.GetStageConfig(selectedStage);
            ResearchStageState selectedState = model.GetStage(selectedStage);

            titleText.text = "ARTEMIS: 2026";
            dateText.text = $"날짜\n{model.Year} Q{model.Quarter}";
            remainingTurnsText.text = $"남은 분기\n{model.RemainingTurns}";
            fundsText.text = $"연구비\n{model.Funds}";
            quarterlyFundingText.text = $"분기 연구비\n+{model.QuarterlyFunding}";

            foreach (ResearchStageConfig config in ResearchPrototypeModel.GetStageConfigs())
            {
                RefreshStageCard(config);
            }

            float progressRatio = Mathf.Clamp01(selectedState.Progress / 100f);
            selectedStageProgressFill.rectTransform.anchorMax = new Vector2(progressRatio, 1f);
            selectedStageProgressFill.color = selectedState.Unlocked ? new Color(0.28f, 0.78f, 0.62f, 1f) : new Color(0.35f, 0.38f, 0.42f, 1f);
            selectedStageTitleText.text = $"선택 단계: {selectedConfig.DisplayName}";
            selectedStageMetaText.text = $"진행도 {selectedState.Progress}/100   최고 등급 {GetBestGradeText(selectedState)}   발사 {selectedState.AttemptCount}회";
            selectedStageChanceText.text = $"연구 기준 성공률 {model.CalculateSuccessChance(selectedStage)}%";
            selectedStageRequirementText.text = $"{GetDesignEntryRequirementText(selectedConfig, selectedState)}\n공개/비공개 테스트 보정은 설계 화면에서 선택";

            normalResearchButtonText.text = $"일반 연구\n비용 {selectedConfig.NormalResearchCost} / 진행도 +{ResearchPrototypeModel.NormalResearchGain}\n1분기";
            focusedResearchButtonText.text = $"집중 연구\n비용 {selectedConfig.FocusedResearchCost} / 진행도 +{ResearchPrototypeModel.FocusedResearchGain}\n1분기";
            enterDesignButtonText.text = $"설계 진입\n발사 비용 {selectedConfig.TestCost} 필요\n비용/분기 미소비";
            waitButtonText.text = $"1분기 대기   비용 0 / 분기 연구비 +{model.QuarterlyFunding}";

            normalResearchButton.interactable = CanExecuteResearch(selectedState, selectedConfig.NormalResearchCost);
            focusedResearchButton.interactable = CanExecuteResearch(selectedState, selectedConfig.FocusedResearchCost);
            enterDesignButton.interactable = CanEnterDesign(selectedState, selectedConfig);
            waitButton.interactable = !model.DeadlineReached;

            designEntryText.text = pendingDesignEntry.HasValue
                ? FormatDesignEntry(pendingDesignEntry.Value)
                : "아직 생성된 설계 진입 데이터가 없습니다.";

            statusText.text = model.DeadlineReached
                ? $"{model.LastMessage}\n2026 Q4 종료. 연구 단계 프로토타입 종료."
                : model.LastMessage;
        }

        private void RefreshStageCard(ResearchStageConfig config)
        {
            ResearchStageState stage = model.GetStage(config.Id);
            StageCardView card = stageCards[(int)config.Id];
            bool isSelected = selectedStage == config.Id;
            Color activeColor = isSelected ? new Color(0.27f, 0.34f, 0.42f, 1f) : new Color(0.19f, 0.23f, 0.28f, 1f);
            Color lockedColor = new Color(0.12f, 0.13f, 0.15f, 1f);

            card.Button.interactable = stage.Unlocked && !model.DeadlineReached;
            card.Button.GetComponent<Image>().color = stage.Unlocked ? activeColor : lockedColor;
            card.Title.text = stage.Unlocked ? config.DisplayName : $"{config.DisplayName}  LOCK";
            card.Progress.text = stage.Unlocked ? $"진행도 {stage.Progress}/100" : GetUnlockConditionText(config.Id);
            card.Details.text = stage.Unlocked
                ? $"최고 {GetBestGradeText(stage)} / 발사 {stage.AttemptCount}회 / 다음 해금 {config.UnlockProgressRequirement}"
                : "잠긴 단계. 조건 충족 후 선택 가능.";
            card.ProgressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(stage.Progress / 100f), 1f);
            card.ProgressFill.color = stage.Unlocked ? new Color(0.28f, 0.78f, 0.62f, 1f) : new Color(0.35f, 0.38f, 0.42f, 1f);
        }

        private bool CanExecuteResearch(ResearchStageState stage, int cost)
        {
            return stage.Unlocked && !model.DeadlineReached && model.Funds >= cost;
        }

        private bool CanEnterDesign(ResearchStageState stage, ResearchStageConfig config)
        {
            return stage.Unlocked
                && !model.DeadlineReached
                && stage.Progress >= config.MinimumTestProgress
                && model.Funds >= config.TestCost;
        }

        private string GetUnlockConditionText(ResearchStageId stageId)
        {
            if (stageId == ResearchStageId.Engine)
            {
                return "기본 해금";
            }

            ResearchStageId previousId = (ResearchStageId)((int)stageId - 1);
            ResearchStageConfig previousConfig = ResearchPrototypeModel.GetStageConfig(previousId);
            ResearchStageState previousStage = model.GetStage(previousId);
            string bestGrade = previousStage.HasBestGrade ? previousStage.BestGrade.ToString() : "없음";
            return $"{previousConfig.DisplayName} {previousStage.Progress}/{previousConfig.UnlockProgressRequirement}, 최고 C 이상: {bestGrade}";
        }

        private string GetDesignEntryRequirementText(ResearchStageConfig config, ResearchStageState stage)
        {
            if (!stage.Unlocked)
            {
                return $"설계 진입 불가: {GetUnlockConditionText(config.Id)}";
            }

            if (stage.Progress < config.MinimumTestProgress && model.Funds < config.TestCost)
            {
                return $"설계 진입 불가: 진행도 {stage.Progress}/{config.MinimumTestProgress}, 연구비 {model.Funds}/{config.TestCost}";
            }

            if (stage.Progress < config.MinimumTestProgress)
            {
                return $"설계 진입 불가: 진행도 {stage.Progress}/{config.MinimumTestProgress}";
            }

            if (model.Funds < config.TestCost)
            {
                return $"설계 진입 불가: 연구비 {model.Funds}/{config.TestCost}";
            }

            return "설계 진입 가능: 비용과 분기는 발사 전까지 소비하지 않음";
        }

        private string FormatDesignEntry(ResearchDesignEntryData data)
        {
            return $"단계 {data.StageId} / 날짜 {data.Year} Q{data.Quarter} / 맵 시드 {data.MapSeed}\n"
                + $"목표 경로 {data.TargetPathId} / 진행도 {data.CurrentProgress}/100 / 이전 평균 {data.PrerequisiteAverage:0.0} / 경험 +{data.ExperienceBonus}%p\n"
                + $"연구 기준 성공률 {model.CalculateSuccessChance(data.StageId)}%. 비용, 분기, 발사 횟수, 결과는 아직 변하지 않습니다.";
        }

        private static string GetBestGradeText(ResearchStageState stage)
        {
            return stage.HasBestGrade ? stage.BestGrade.ToString() : "-";
        }

        private static TMP_Text CreateInfoChip(string name, Transform parent)
        {
            RectTransform chip = CreatePanel(name, parent, new Color(0.11f, 0.13f, 0.17f, 1f));
            AddVerticalLayout(chip, 8f, 8f, 6f, 4f);
            chip.gameObject.AddComponent<LayoutElement>().preferredWidth = 118f;
            return CreateText("Text", chip, 14, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
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

            TMP_Text label = CreateText("Label", rectTransform, 14, FontStyles.Bold, TextAlignmentOptions.Center, text);
            Stretch(label.rectTransform, 8f);
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

        private readonly struct StageCardView
        {
            public StageCardView(Button button, TMP_Text title, TMP_Text progress, TMP_Text details, Image progressFill)
            {
                Button = button;
                Title = title;
                Progress = progress;
                Details = details;
                ProgressFill = progressFill;
            }

            public Button Button { get; }
            public TMP_Text Title { get; }
            public TMP_Text Progress { get; }
            public TMP_Text Details { get; }
            public Image ProgressFill { get; }
        }
    }
}
