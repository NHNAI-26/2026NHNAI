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
        private const int EngineCount = ResearchPrototypeModel.MaxEnginePresetCount;
        private const string OperationScreenPrefabPath = "ResearchUI/ResearchOperationScreen";
        private const string EnginePresetCardPrefabPath = "ResearchUI/EnginePresetCard";

        [SerializeField] private GameObject operationScreenPrefab;
        [SerializeField] private Button enginePresetCardPrefab;

        private readonly EngineCardView[] engineCards = new EngineCardView[EngineCount];

        private ResearchFlowSession session;
        private ResearchPrototypeModel model;
        private EnginePresetId selectedEnginePreset = EnginePresetId.Engine01;
        private EngineStatId selectedStat = EngineStatId.FuelCapacity;
        private LaunchStageId selectedStage = LaunchStageId.Engine;
        private bool initialized;
        private RectTransform canvasTransform;
        private ResearchDesignScreenController activeDesignController;
        private ResearchMiniGameController activeMiniGameController;

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
        private Button createEnginePresetButton;
        private TMP_Text createEnginePresetButtonText;
        private Button enterDesignButton;
        private TMP_Text enterDesignButtonText;
        private Button waitButton;
        private TMP_Text waitButtonText;

        public ResearchPrototypeModel Model => model;
        public LaunchStageId SelectedStage => selectedStage;
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

        public void ConfigureCardPrefabsForTests(Button enginePresetCardTemplate, Button launchTargetCardTemplate)
        {
            enginePresetCardPrefab = enginePresetCardTemplate;
        }

        public void ConfigureScreenPrefabForTests(GameObject screenTemplate)
        {
            operationScreenPrefab = screenTemplate;
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
            if (!BuildInterface())
            {
                return;
            }

            initialized = true;
            Refresh();
        }

        private bool BuildInterface()
        {
            EnsureEventSystem();
            EnsureDefaultPrefabs();

            if (TryBuildInterfaceFromPrefab())
            {
                return true;
            }

            Debug.LogError("Research operation UI prefab is missing or invalid. Expected Resources/ResearchUI/ResearchOperationScreen plus EnginePresetCard.");
            return false;
        }

        private bool TryBuildInterfaceFromPrefab()
        {
            GameObject prefab = operationScreenPrefab != null
                ? operationScreenPrefab
                : Resources.Load<GameObject>(OperationScreenPrefabPath);
            if (prefab == null)
            {
                return false;
            }

            GameObject instance = Instantiate(prefab, transform);
            instance.name = "ResearchOperationCanvas";
            canvasTransform = instance.GetComponent<RectTransform>();
            if (canvasTransform == null)
            {
                canvasTransform = instance.AddComponent<RectTransform>();
            }

            Canvas canvas = instance.GetComponent<Canvas>() ?? instance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (instance.GetComponent<GraphicRaycaster>() == null)
            {
                instance.AddComponent<GraphicRaycaster>();
            }

            CanvasScaler scaler = instance.GetComponent<CanvasScaler>() ?? instance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform engineColumn = FindChildRectTransform(canvasTransform, "EnginePresetCards")
                ?? FindChildRectTransform(canvasTransform, "EnginePresetColumn");
            RectTransform launchTargetColumn = FindChildRectTransform(canvasTransform, "LaunchTargetColumn");
            if (launchTargetColumn != null)
            {
                launchTargetColumn.gameObject.SetActive(false);
            }

            dateText = FindRequiredText(canvasTransform, "Date");
            remainingTurnsText = FindRequiredText(canvasTransform, "RemainingTurns");
            fundsText = FindRequiredText(canvasTransform, "Funds");
            quarterlyFundingText = FindRequiredText(canvasTransform, "QuarterlyFunding");
            selectedEngineText = FindRequiredText(canvasTransform, "SelectedEngineText");
            selectedStageText = FindRequiredText(canvasTransform, "SelectedStageText");
            selectedRequirementText = FindRequiredText(canvasTransform, "SelectedRequirementText");
            designEntryText = FindRequiredText(canvasTransform, "DesignEntryText");
            statusText = FindRequiredText(canvasTransform, "StatusText");
            normalResearchButton = FindRequiredButton(canvasTransform, "NormalResearchButton");
            focusedResearchButton = FindRequiredButton(canvasTransform, "FocusedResearchButton");
            createEnginePresetButton = FindRequiredButton(canvasTransform, "CreateEnginePresetButton");
            enterDesignButton = FindRequiredButton(canvasTransform, "EnterDesignButton");
            waitButton = FindRequiredButton(canvasTransform, "WaitQuarterButton");
            Button resetButton = FindRequiredButton(canvasTransform, "ResetButton");
            Button fuelCapacityButton = FindRequiredButton(canvasTransform, "StatButton_FuelCapacity");
            Button coolingButton = FindRequiredButton(canvasTransform, "StatButton_Cooling");
            Button maxOutputButton = FindRequiredButton(canvasTransform, "StatButton_MaxOutput");
            Button ignitionReliabilityButton = FindRequiredButton(canvasTransform, "StatButton_IgnitionReliability");

            if (enginePresetCardPrefab == null
                || engineColumn == null
                || dateText == null
                || remainingTurnsText == null
                || fundsText == null
                || quarterlyFundingText == null
                || selectedEngineText == null
                || selectedStageText == null
                || selectedRequirementText == null
                || designEntryText == null
                || statusText == null
                || normalResearchButton == null
                || focusedResearchButton == null
                || createEnginePresetButton == null
                || enterDesignButton == null
                || waitButton == null
                || resetButton == null
                || fuelCapacityButton == null
                || coolingButton == null
                || maxOutputButton == null
                || ignitionReliabilityButton == null)
            {
                DestroyUnityObject(instance);
                return false;
            }

            normalResearchButtonText = normalResearchButton.GetComponentInChildren<TMP_Text>(true);
            focusedResearchButtonText = focusedResearchButton.GetComponentInChildren<TMP_Text>(true);
            createEnginePresetButtonText = createEnginePresetButton.GetComponentInChildren<TMP_Text>(true);
            enterDesignButtonText = enterDesignButton.GetComponentInChildren<TMP_Text>(true);
            waitButtonText = waitButton.GetComponentInChildren<TMP_Text>(true);

            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                engineCards[(int)config.Id] = CreateEngineCard(engineColumn, config);
            }

            normalResearchButton.onClick.AddListener(() => StartEngineResearch(false));
            focusedResearchButton.onClick.AddListener(() => StartEngineResearch(true));
            createEnginePresetButton.onClick.AddListener(CreateNewEnginePreset);
            enterDesignButton.onClick.AddListener(EnterDesign);
            waitButton.onClick.AddListener(WaitQuarter);
            resetButton.onClick.AddListener(() =>
            {
                session.ResetResearch();
                selectedEnginePreset = EnginePresetId.Engine01;
                selectedStage = model.GetCurrentLaunchTarget();
                Refresh();
            });
            fuelCapacityButton.onClick.AddListener(() => SelectStat(EngineStatId.FuelCapacity));
            coolingButton.onClick.AddListener(() => SelectStat(EngineStatId.Cooling));
            maxOutputButton.onClick.AddListener(() => SelectStat(EngineStatId.MaxOutput));
            ignitionReliabilityButton.onClick.AddListener(() => SelectStat(EngineStatId.IgnitionReliability));
            return true;
        }

        private void EnsureDefaultPrefabs()
        {
            if (operationScreenPrefab == null)
            {
                operationScreenPrefab = Resources.Load<GameObject>(OperationScreenPrefabPath);
            }

            if (enginePresetCardPrefab == null)
            {
                enginePresetCardPrefab = Resources.Load<Button>(EnginePresetCardPrefabPath);
            }

        }

        private EngineCardView CreateEngineCard(RectTransform parent, EnginePresetConfig config)
        {
            Button button = CreateCardButton(enginePresetCardPrefab, $"EngineCard_{config.Id}", parent, 46f, out TMP_Text title, out TMP_Text detail);

            EnginePresetId presetId = config.Id;
            button.onClick.AddListener(() =>
            {
                selectedEnginePreset = presetId;
                session.ClearPendingDesignEntry();
                Refresh();
            });

            return new EngineCardView(button, title, detail);
        }

        private void SelectStat(EngineStatId statId)
        {
            selectedStat = statId;
            Refresh();
        }

        private void StartEngineResearch(bool focused)
        {
            ShowMiniGame(focused);
        }

        private void CreateNewEnginePreset()
        {
            ResearchActionResult result = model.CreateNewEnginePreset(out EnginePresetId newPresetId);
            if (result == ResearchActionResult.Success)
            {
                selectedEnginePreset = newPresetId;
            }

            session.ClearPendingDesignEntry();
            Refresh();
        }

        private void ShowMiniGame(bool focused)
        {
            if (activeMiniGameController != null)
            {
                return;
            }

            canvasTransform.gameObject.SetActive(false);
            var host = new GameObject("Research Mini Game Controller");
            host.transform.SetParent(transform, false);
            activeMiniGameController = host.AddComponent<ResearchMiniGameController>();
            activeMiniGameController.Initialize(selectedEnginePreset, selectedStat, focused, CompleteMiniGame);
        }

        private void CompleteMiniGame(ResearchMiniGameResult result)
        {
            if (activeMiniGameController != null)
            {
                DestroyUnityObject(activeMiniGameController.gameObject);
                activeMiniGameController = null;
            }

            model.ExecuteEngineResearch(result.PresetId, result.StatId, result.Focused, result.Score);
            session.ClearPendingDesignEntry();
            canvasTransform.gameObject.SetActive(true);
            Refresh();
        }

        private void EnterDesign()
        {
            selectedStage = model.GetCurrentLaunchTarget();
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
            EnsureSelectedEnginePresetUnlocked();
            selectedStage = model.GetCurrentLaunchTarget();
            dateText.text = $"날짜\n{model.Year} Q{model.Quarter}";
            remainingTurnsText.text = $"남은 분기\n{model.RemainingTurns}";
            fundsText.text = $"연구비\n{model.Funds}";
            quarterlyFundingText.text = $"분기 연구비\n+{model.QuarterlyFunding}";

            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                RefreshEngineCard(config);
            }

            EnginePresetConfig selectedEngineConfig = ResearchPrototypeModel.GetEnginePresetConfig(selectedEnginePreset);
            EnginePresetState selectedEngine = model.GetEnginePreset(selectedEnginePreset);
            LaunchStageConfig selectedStageConfig = ResearchPrototypeModel.GetStageConfig(selectedStage);
            LaunchStageState selectedStageState = model.GetStage(selectedStage);
            ResearchDesignEntryData preview = model.CreateDesignEntry(selectedStage, selectedEnginePreset, CreatePreviewInstalledCounts(selectedStage, selectedEnginePreset), 50, selectedStage == LaunchStageId.Moon ? TestVisibility.FinalMission : TestVisibility.Private);

            selectedEngineText.text = $"{selectedEngineConfig.DisplayName} Lv.{selectedEngine.Level}/{ResearchPrototypeModel.MaxEnginePresetLevel}  "
                + $"성능 {model.CalculateEnginePerformanceScore(selectedEnginePreset)}  설치 {selectedEngineConfig.InstallCost}\n"
                + $"연료 {selectedEngine.FuelCapacity} / 냉각 {selectedEngine.Cooling} / 출력 {selectedEngine.MaxOutput} / 점화 {selectedEngine.IgnitionReliability}\n"
                + $"선택 스탯: {ResearchPrototypeModel.GetStatDisplayName(selectedStat)} / 미니게임 점수로 보상 결정 / 시험 최고 {GetBestGradeText(selectedEngine)}";
            selectedStageText.text = $"현재 설계 목표  발사비 {selectedStageConfig.LaunchCost} / 예상 성공 {model.CalculateSuccessChance(preview)}% / 경험 +{preview.ExperienceBonus}%p";
            selectedRequirementText.text = GetDesignEntryRequirementText(selectedStageConfig, selectedStageState, selectedEngine);

            normalResearchButtonText.text = $"일반 연구\n{selectedEngineConfig.NormalResearchCost} / Lv +{ResearchPrototypeModel.NormalResearchLevelGain}";
            focusedResearchButtonText.text = $"집중 연구\n{selectedEngineConfig.FocusedResearchCost} / Lv +{ResearchPrototypeModel.FocusedResearchLevelGain}";
            createEnginePresetButtonText.text = model.ActiveEnginePresetCount >= ResearchPrototypeModel.MaxEnginePresetCount
                ? "새로운 엔진 개발\n최대 10개"
                : $"새로운 엔진 개발\n현재 {model.ActiveEnginePresetCount}/{ResearchPrototypeModel.MaxEnginePresetCount}";
            enterDesignButtonText.text = $"설계 진입\n발사비 {selectedStageConfig.LaunchCost}";
            waitButtonText.text = $"1분기 대기   비용 0 / 분기 연구비 +{model.QuarterlyFunding}";

            normalResearchButton.interactable = CanResearch(selectedEngine, selectedEngineConfig.NormalResearchCost);
            focusedResearchButton.interactable = CanResearch(selectedEngine, selectedEngineConfig.FocusedResearchCost);
            createEnginePresetButton.interactable = !model.DeadlineReached && model.ActiveEnginePresetCount < ResearchPrototypeModel.MaxEnginePresetCount;
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
            card.Button.gameObject.SetActive(model.IsEnginePresetUnlocked(config.Id));
            bool selected = selectedEnginePreset == config.Id;
            card.Button.GetComponent<Image>().color = selected ? new Color(0.28f, 0.35f, 0.42f, 1f) : new Color(0.19f, 0.23f, 0.28f, 1f);
            card.Title.text = config.DisplayName;
            card.Detail.text = $"Lv.{engine.Level} 성능 {model.CalculateEnginePerformanceScore(config.Id)} 최고 {GetBestGradeText(engine)}";
        }

        private bool CanResearch(EnginePresetState engine, int cost)
        {
            return !model.DeadlineReached
                && engine.Unlocked
                && engine.Level < ResearchPrototypeModel.MaxEnginePresetLevel
                && model.Funds >= cost;
        }

        private bool CanEnterDesign(LaunchStageState stage, LaunchStageConfig config, EnginePresetState engine)
        {
            return !model.DeadlineReached
                && engine.Unlocked
                && stage.Unlocked
                && (selectedStage != LaunchStageId.Engine || engine.Level >= 1)
                && model.Funds >= config.LaunchCost;
        }

        private string GetDesignEntryRequirementText(LaunchStageConfig config, LaunchStageState stage, EnginePresetState engine)
        {
            if (!stage.Unlocked)
            {
                return $"설계 진입 불가: {model.GetUnlockConditionText(config.Id)}";
            }

            if (!engine.Unlocked)
            {
                return $"설계 진입 불가: {ResearchPrototypeModel.GetEnginePresetConfig(selectedEnginePreset).DisplayName} 개발 필요";
            }

            if (selectedStage == LaunchStageId.Engine && engine.Level < 1)
            {
                return $"설계 진입 불가: {ResearchPrototypeModel.GetEnginePresetConfig(selectedEnginePreset).DisplayName} 레벨 1 필요";
            }

            if (model.Funds < config.LaunchCost)
            {
                return $"설계 진입 불가: 연구비 {model.Funds}/{config.LaunchCost}";
            }

            return "설계 진입 가능: 비용과 분기는 발사 전까지 소비하지 않음";
        }

        private int[] CreatePreviewInstalledCounts(LaunchStageId stageId, EnginePresetId presetId)
        {
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            if (stageId != LaunchStageId.Engine)
            {
                counts[(int)presetId] = 1;
            }

            return counts;
        }

        private void EnsureSelectedEnginePresetUnlocked()
        {
            if (model.IsEnginePresetUnlocked(selectedEnginePreset))
            {
                return;
            }

            selectedEnginePreset = EnginePresetId.Engine01;
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

        public ResearchMiniGameController GetActiveMiniGameControllerForTests()
        {
            return activeMiniGameController;
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
            selectedStage = model.GetCurrentLaunchTarget();
            model.PrepareDebugDesignEntryState(selectedStage, selectedEnginePreset);

            if (session.TryEnterDesign(selectedStage, selectedEnginePreset, out _) == ResearchActionResult.Success)
            {
                ShowDesignScreen();
            }
        }

#endif
        private static string GetBestGradeText(LaunchStageState stage)
        {
            return stage.HasBestGrade ? stage.BestGrade.ToString() : "-";
        }

        private static string GetBestGradeText(EnginePresetState engine)
        {
            return engine.HasBestGrade ? engine.BestGrade.ToString() : "-";
        }

        private static Button CreateCardButton(Button prefab, string name, Transform parent, float preferredHeight, out TMP_Text title, out TMP_Text detail)
        {
            title = null;
            detail = null;
            if (prefab == null)
            {
                return null;
            }

            Button button = Instantiate(prefab, parent);
            button.name = name;

            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.flexibleWidth = 1f;
            layout.preferredHeight = preferredHeight;

            title = FindChildText(button.transform, "Title");
            detail = FindChildText(button.transform, "Detail");
            if (title != null && detail != null)
            {
                return button;
            }

            return button;
        }

        private static TMP_Text FindChildText(Transform root, string name)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == name)
                {
                    return text;
                }
            }

            return null;
        }

        private static TMP_Text FindRequiredText(Transform root, string name)
        {
            return FindChildText(root, name);
        }

        private static Button FindRequiredButton(Transform root, string name)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            return null;
        }

        private static RectTransform FindChildRectTransform(Transform root, string name)
        {
            foreach (RectTransform rectTransform in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rectTransform.name == name)
                {
                    return rectTransform;
                }
            }

            return null;
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

    }
}
