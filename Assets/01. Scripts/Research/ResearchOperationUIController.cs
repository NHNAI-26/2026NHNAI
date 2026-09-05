using System;
using System.Reflection;
using System.Collections;
using Border.Audio;
using Border.Prologue;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PanelGroup = Border.Research.ResearchOperationTransitionAnimator.PanelGroup;

namespace Border.Research
{
    public sealed class ResearchOperationUIController : MonoBehaviour
    {
        private const int EngineCount = ResearchPrototypeModel.MaxEnginePresetCount;
        private const int StatCount = 4;
        private const string OperationScreenPrefabPath = "ResearchUI/ResearchOperationScreen";
        private const string EnginePresetCardPrefabPath = "ResearchUI/EnginePresetCard";
        private const float DesignTransitionDelaySeconds = 1f;
        private const float WaitFadeOutSeconds = 0.18f;
        private const float WaitFadeInSeconds = 0.22f;
        private const string ResearchCinemachineCameraName = "Research Cinemachine Camera";
        private const int ResearchCinemachineCameraPriority = 20;

        [SerializeField] private GameObject operationScreenPrefab;
        [SerializeField] private SoundManager soundManagerPrefab;
        [SerializeField] private Button enginePresetCardPrefab;
        [SerializeField] private ResearchResultReportController resultReport;
        [SerializeField] private ResearchEndingController endingScreen;
        [SerializeField] private ResearchTestVisibilityDialog visibilityDialog;
        [SerializeField] private ResearchEnginePreviewController enginePreview;
        [SerializeField] private ResearchMiniGameController miniGameController;
        [SerializeField] private ResearchDesignScreenController designScreenController;
        [SerializeField] private Transform researchLabRoot;
        [SerializeField] private Transform researchCameraTransform;
        [SerializeField, Min(0f)] private float cameraPitchDriftDegrees = 0.35f;
        [SerializeField, Min(0f)] private float cameraYawOrbitDegrees = 12f;
        [SerializeField, Min(0.1f)] private float cameraYawOrbitCycleSeconds = 24f;
        [SerializeField, Min(0f)] private float cameraReturnSeconds = 0.5f;

        private readonly EngineCardView[] engineCards = new EngineCardView[EngineCount];

        private ResearchFlowSession session;
        private ResearchPrototypeModel model;
        private EnginePresetId selectedEnginePreset = EnginePresetId.Engine01;
        private EngineStatId selectedStat = EngineStatId.FuelCapacity;
        private LaunchMissionId selectedMission = LaunchMissionId.LowAltitude;
        private bool initialized;
        private RectTransform canvasTransform;
        private CanvasGroup canvasGroup;
        private ResearchOperationTransitionAnimator operationTransitionAnimator;
        private ResearchDesignScreenController activeDesignController;
        private ResearchMiniGameController activeMiniGameController;
        private Sequence designTransitionSequence;
        private Sequence waitFadeSequence;
        private Tween researchCameraDriftTween;
        private Tween researchCameraReturnTween;
        private Quaternion researchCameraBaseLocalRotation;
        private Vector3 researchCameraBasePosition;
        private Vector3 researchCameraOrbitPivot;
        private bool hasResearchCameraBaseLocalRotation;
        private bool isTransitioningToDesign;
        private bool partDevelopmentOpen;
        private bool closingPartDevelopment;
        private bool focusedResearchSelected;

        private readonly Button[] statButtons = new Button[StatCount];
        private readonly TMP_Text[] statRowLabels = new TMP_Text[StatCount];
        private readonly Slider[] statGauges = new Slider[StatCount];

        private GameObject hubActionBar;
        private GameObject enginePresetColumnRoot;
        private GameObject detailColumnRoot;
        private TMP_Text dateText;
        private TMP_Text fundsText;
        private TMP_Text selectedEngineNameText;
        private TMP_Text selectedEnginePerformanceText;
        private TMP_Text selectedEngineInstallCostText;
        private TMP_Text selectedEngineText;
        private TMP_Text selectedStatText;
        private TMP_Text statusText;
        private Slider selectedEngineCompletion;
        private Button partDevelopmentButton;
        private TMP_Text partDevelopmentButtonText;
        private Button startDevelopmentButton;
        private Button cancelDevelopmentButton;
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
        public LaunchMissionId SelectedMission => selectedMission;
        public EnginePresetId SelectedEnginePreset => selectedEnginePreset;
        public string RequestedScreenName { get; private set; } = ResearchFlowSession.ResearchScreenName;

        /// <summary>Suppresses the pause menu while the part development panel owns the escape key.</summary>
        public static bool IsPartDevelopmentOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SpawnInMainScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != ResearchFlowSession.MainSceneName || FindSceneObject<ResearchOperationUIController>() != null)
            {
                return;
            }

            Debug.LogError("01_Main has no preplaced ResearchOperationUIController.");
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Initialize();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!initialized || session == null || model == null)
            {
                initialized = false;
                Initialize();
                return;
            }

            if (initialized)
            {
                Refresh();
            }
        }

        private IEnumerator Start()
        {
            var prologue = FindFirstObjectByType<PrologueController>();
            while (prologue != null && prologue.gameObject.activeInHierarchy)
                yield return null;

            if (initialized && RequestedScreenName == ResearchFlowSession.ResearchScreenName)
                SoundManager.Instance?.PlayBgm("EngineBGM");
        }

        private void OnDisable()
        {
            if (visibilityDialog != null) visibilityDialog.Hide();
            IsPartDevelopmentOpen = false;
            // The animator kills its sequence on disable, so the close callback never lands — clear the guard
            // here or every later click on 부품 개발 / 그만두기 is swallowed.
            closingPartDevelopment = false;
            if (initialized)
            {
                KillWaitFade();
                KillDesignTransition();
                KillResearchCameraDrift(resetRotation: true);
                HideEnginePreview();
            }
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

            if (Application.isPlaying)
            {
                EnsureResearchCameraRuntime();
            }

            initialized = true;
            if (Application.isPlaying)
            {
                if (SoundManager.Instance == null && soundManagerPrefab != null)
                    Instantiate(soundManagerPrefab);
            }
            Refresh();
            PlayResearchEntryAnimation(PanelGroup.Hub);
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

            GameObject instance;
            bool createdInstance = false;
            Transform existingCanvas = transform.Find("ResearchOperationCanvas") ?? transform.Find("ResearchOperationScreen");
            if (existingCanvas != null)
            {
                instance = existingCanvas.gameObject;
                instance.name = "ResearchOperationCanvas";
            }
            else if (CanCreateRuntimeUiFallback())
            {
                instance = Instantiate(prefab, transform);
                instance.name = "ResearchOperationCanvas";
                createdInstance = true;
            }
            else
            {
                Debug.LogError("Research operation UI must be preplaced in 01_Main.", this);
                return false;
            }

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

            // Whole-screen fades (건너뛰기) ride on this group; the animator only owns the per-panel groups.
            // ?? would accept Unity's fake-null from a missing component, so test the Unity way.
            canvasGroup = instance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = instance.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;

            CanvasScaler scaler = instance.GetComponent<CanvasScaler>() ?? instance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            operationTransitionAnimator = instance.GetComponent<ResearchOperationTransitionAnimator>() ?? instance.AddComponent<ResearchOperationTransitionAnimator>();
            operationTransitionAnimator.Bind(canvasTransform);

            RectTransform engineColumn = FindChildRectTransform(canvasTransform, "EnginePresetCards")
                ?? FindChildRectTransform(canvasTransform, "EnginePresetColumn");
            hubActionBar = FindChildRectTransform(canvasTransform, "HubActionBar")?.gameObject;
            enginePresetColumnRoot = FindChildRectTransform(canvasTransform, "EnginePresetColumn")?.gameObject;
            detailColumnRoot = FindChildRectTransform(canvasTransform, "DetailColumn")?.gameObject;
            dateText = FindRequiredText(canvasTransform, "Date");
            fundsText = FindRequiredText(canvasTransform, "Funds");
            selectedEngineNameText = FindRequiredText(canvasTransform, "SelectedEngineName");
            selectedEnginePerformanceText = FindRequiredText(canvasTransform, "SelectedEnginePerformance");
            selectedEngineInstallCostText = FindRequiredText(canvasTransform, "SelectedEngineInstallCost");
            selectedEngineText = FindRequiredText(canvasTransform, "SelectedEngineText");
            selectedStatText = FindRequiredText(canvasTransform, "SelectedStatText");
            RectTransform completionGauge = FindChildRectTransform(canvasTransform, "SelectedEngineCompletion");
            selectedEngineCompletion = completionGauge != null ? completionGauge.GetComponent<Slider>() : null;
            partDevelopmentButton = FindRequiredButton(canvasTransform, "PartDevelopmentButton");
            startDevelopmentButton = FindRequiredButton(canvasTransform, "StartDevelopmentButton");
            cancelDevelopmentButton = FindRequiredButton(canvasTransform, "CancelDevelopmentButton");
            normalResearchButton = FindRequiredButton(canvasTransform, "NormalResearchButton");
            focusedResearchButton = FindRequiredButton(canvasTransform, "FocusedResearchButton");
            createEnginePresetButton = FindRequiredButton(canvasTransform, "CreateEnginePresetButton");
            enterDesignButton = FindRequiredButton(canvasTransform, "EnterDesignButton");
            waitButton = FindRequiredButton(canvasTransform, "WaitQuarterButton");
            for (int index = 0; index < StatCount; index++)
            {
                var statId = (EngineStatId)index;
                statButtons[index] = FindRequiredButton(canvasTransform, $"StatButton_{statId}");
                statRowLabels[index] = FindRequiredText(canvasTransform, $"StatRowLabel_{statId}");
                RectTransform statGauge = FindChildRectTransform(canvasTransform, $"StatGauge_{statId}");
                statGauges[index] = statGauge != null ? statGauge.GetComponent<Slider>() : null;
            }

            if (enginePresetCardPrefab == null
                || engineColumn == null
                || hubActionBar == null
                || enginePresetColumnRoot == null
                || detailColumnRoot == null
                || dateText == null
                || fundsText == null
                || selectedEngineNameText == null
                || selectedEnginePerformanceText == null
                || selectedEngineInstallCostText == null
                || selectedEngineText == null
                || selectedStatText == null
                || partDevelopmentButton == null
                || startDevelopmentButton == null
                || cancelDevelopmentButton == null
                || normalResearchButton == null
                || focusedResearchButton == null
                || createEnginePresetButton == null
                || enterDesignButton == null
                || waitButton == null
                || Array.Exists(statButtons, button => button == null)
                || Array.Exists(statRowLabels, label => label == null))
            {
                if (createdInstance)
                {
                    DestroyUnityObject(instance);
                }

                return false;
            }

            partDevelopmentButtonText = partDevelopmentButton.GetComponentInChildren<TMP_Text>(true);
            normalResearchButtonText = normalResearchButton.GetComponentInChildren<TMP_Text>(true);
            focusedResearchButtonText = focusedResearchButton.GetComponentInChildren<TMP_Text>(true);
            createEnginePresetButtonText = createEnginePresetButton.GetComponentInChildren<TMP_Text>(true);
            enterDesignButtonText = enterDesignButton.GetComponentInChildren<TMP_Text>(true);
            waitButtonText = waitButton.GetComponentInChildren<TMP_Text>(true);
            ConfigureDynamicMultilineText(selectedEngineText);
            var effectsObject = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            effectsObject.transform.SetParent(canvasTransform, false);
            statusText = effectsObject.GetComponent<TMP_Text>();
            statusText.font = selectedEngineText.font;
            statusText.fontSize = 14f;
            statusText.color = selectedEngineText.color;
            statusText.raycastTarget = false;
            statusText.alignment = TextAlignmentOptions.BottomLeft;
            ConfigureDynamicMultilineText(statusText);
            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                engineCards[(int)config.Id] = CreateEngineCard(engineColumn, config);
            }

            partDevelopmentButton.onClick.AddListener(() => SetPartDevelopmentOpen(true));
            startDevelopmentButton.onClick.AddListener(() => StartEngineResearch(focusedResearchSelected));
            cancelDevelopmentButton.onClick.AddListener(() => SetPartDevelopmentOpen(false));
            normalResearchButton.onClick.AddListener(() => SelectResearchMode(false));
            focusedResearchButton.onClick.AddListener(() => SelectResearchMode(true));
            createEnginePresetButton.onClick.AddListener(CreateNewEnginePreset);
            enterDesignButton.onClick.AddListener(EnterDesign);
            waitButton.onClick.AddListener(WaitQuarter);
            for (int index = 0; index < StatCount; index++)
            {
                var statId = (EngineStatId)index;
                statButtons[index].onClick.AddListener(() => SelectStat(statId));
            }

            // Bind() has cached the panel positions by now, so the columns can be hidden safely.
            SetPartDevelopmentOpen(false);
            return true;
        }

        private void SetPartDevelopmentOpen(bool open)
        {
            if (closingPartDevelopment) return;

            // Closing runs the columns back out the way they came, then slides the hub row up from the bottom.
            // The build path and edit-mode tests fall through to the immediate branch instead.
            if (!open && partDevelopmentOpen && Application.isPlaying && operationTransitionAnimator != null)
            {
                closingPartDevelopment = true;
                partDevelopmentOpen = false;
                IsPartDevelopmentOpen = false;
                operationTransitionAnimator.PlayExit(PanelGroup.Columns, () =>
                {
                    closingPartDevelopment = false;
                    ApplyPartDevelopmentState(false);
                    PlayResearchEntryAnimation(PanelGroup.Bottom);
                });
                return;
            }

            ApplyPartDevelopmentState(open);
            if (open) PlayResearchEntryAnimation(PanelGroup.Columns);
        }

        private void ApplyPartDevelopmentState(bool open)
        {
            partDevelopmentOpen = open;
            IsPartDevelopmentOpen = open;
            if (hubActionBar != null) hubActionBar.SetActive(!open);
            if (enginePresetColumnRoot != null) enginePresetColumnRoot.SetActive(open);
            if (detailColumnRoot != null) detailColumnRoot.SetActive(open);
            RefreshPendingEffects();
        }

        private void SelectResearchMode(bool focused)
        {
            if (isTransitioningToDesign) return;
            focusedResearchSelected = focused;
            Refresh();
        }

        private void LateUpdate()
        {
            // Runs after PauseMenuController.Update, which stands down while this panel owns escape.
            // The panel only owns it while actually on screen — the mini game hides the canvas without closing it.
            if (!initialized || isTransitioningToDesign || closingPartDevelopment) return;
            bool ownsEscape = partDevelopmentOpen && canvasTransform != null && canvasTransform.gameObject.activeInHierarchy;
            IsPartDevelopmentOpen = ownsEscape;
            if (ownsEscape && Border.UI.PauseMenuController.WasEscapePressed()) SetPartDevelopmentOpen(false);
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
            string cardName = $"EngineCard_{config.Id}";
            Button button = FindRequiredButton(parent, cardName);
            if (button == null)
            {
                button = CreateCardButton(enginePresetCardPrefab, cardName, parent, 46f, out TMP_Text title, out TMP_Text detail);
                if (button == null)
                {
                    return new EngineCardView(null, null, null);
                }

                Border.UI.UISelectableSoundHook.ClearListeners(button);
                return BindEngineCardButton(button, title, detail, config.Id);
            }

            ConfigureCardButton(button, 46f, out TMP_Text existingTitle, out TMP_Text existingDetail);
            Border.UI.UISelectableSoundHook.ClearListeners(button);
            return BindEngineCardButton(button, existingTitle, existingDetail, config.Id);
        }

        private EngineCardView BindEngineCardButton(Button button, TMP_Text title, TMP_Text detail, EnginePresetId presetId)
        {
            button.GetComponent<EnginePresetNameEditor>()?.Bind(model, presetId, Refresh, () => !isTransitioningToDesign);
            button.onClick.AddListener(() =>
            {
                if (isTransitioningToDesign)
                {
                    return;
                }

                selectedEnginePreset = presetId;
                session.ClearPendingDesignEntry();
                Refresh();
            });

            return new EngineCardView(button, title, detail);
        }

        private void SelectStat(EngineStatId statId)
        {
            if (isTransitioningToDesign)
            {
                return;
            }

            selectedStat = statId;
            Refresh();
        }

        private void StartEngineResearch(bool focused)
        {
            if (isTransitioningToDesign || model.HasGameEnded)
            {
                return;
            }

            ShowMiniGame(focused);
        }

        private void CreateNewEnginePreset()
        {
            if (isTransitioningToDesign)
            {
                return;
            }

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
            KillResearchCameraDrift(resetRotation: true);
            HideEnginePreview();
            HideResearchLab();
            activeMiniGameController = ResolveMiniGameController();
            if (activeMiniGameController == null)
            {
                Debug.LogError("Research mini game controller must be preplaced in 01_Main.", this);
                canvasTransform.gameObject.SetActive(true);
                ShowResearchLab();
                Refresh();
                return;
            }

            activeMiniGameController.gameObject.SetActive(true);
            activeMiniGameController.enabled = true;
            activeMiniGameController.Initialize(selectedEnginePreset, selectedStat, focused, CompleteMiniGame);
        }

        private void CompleteMiniGame(ResearchMiniGameResult result)
        {
            if (activeMiniGameController != null)
            {
                activeMiniGameController.HideForReuse();
                activeMiniGameController = null;
            }

            model.ExecuteEngineResearch(result.PresetId, result.StatId, result.Focused, result.Score);
            session.ClearPendingDesignEntry();
            if (model.HasGameEnded)
            {
                session.QueueDeadlineFailureReportIfNeeded();
                Refresh();
                return;
            }
            canvasTransform.gameObject.SetActive(true);
            ShowResearchLab();
            PlayResearchCameraDrift();
            Refresh();
            PlayResearchEntryAnimation(partDevelopmentOpen ? PanelGroup.Columns : PanelGroup.Hub);
        }

        private void EnterDesign()
        {
            if (isTransitioningToDesign || model.HasGameEnded || session.HasActiveLaunch || (visibilityDialog != null && visibilityDialog.IsOpen))
            {
                return;
            }

            selectedMission = model.GetCurrentMission();
            if (visibilityDialog == null)
            {
                Debug.LogError("Research test visibility dialog must be assigned in the scene.", this);
                return;
            }
            LaunchMissionId mission = selectedMission;
            EnginePresetId preset = selectedEnginePreset;
            visibilityDialog.Open(model, mission, visibility => ConfirmDesignEntry(mission, preset, visibility));
        }

        private ResearchActionResult ConfirmDesignEntry(LaunchMissionId mission, EnginePresetId preset, TestVisibility visibility)
        {
            if (isTransitioningToDesign) return ResearchActionResult.RequirementNotMet;
            ResearchActionResult result = session.TryEnterDesign(mission, preset, visibility, out _);
            if (result == ResearchActionResult.Success) BeginDesignTransition();
            return result;
        }

        private void WaitQuarter()
        {
            if (isTransitioningToDesign || waitFadeSequence != null)
            {
                return;
            }

            // The quarter is spent on the click, not when the fade finishes — edit-mode tests read the
            // model straight after invoking the button, and the ending screen must not wait on a tween.
            model.WaitQuarter();
            session.ClearPendingDesignEntry();
            if (!Application.isPlaying || model.HasGameEnded || canvasGroup == null)
            {
                Refresh();
                return;
            }

            waitFadeSequence = DOTween.Sequence()
                .SetTarget(this)
                .Append(FadeCanvas(0f, WaitFadeOutSeconds))
                .AppendCallback(Refresh)
                .Append(FadeCanvas(1f, WaitFadeInSeconds))
                .OnComplete(() => waitFadeSequence = null);
        }

        private Tween FadeCanvas(float targetAlpha, float duration)
        {
            return DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, targetAlpha, duration)
                .SetEase(Ease.InOutSine);
        }

        private void KillWaitFade()
        {
            if (waitFadeSequence != null)
            {
                waitFadeSequence.Kill();
                waitFadeSequence = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public void CompleteWaitFadeForTests()
        {
            waitFadeSequence?.Complete();
        }

        private void Refresh()
        {
            if (resultReport != null && resultReport.gameObject.activeSelf) return;
            session.QueueDeadlineFailureReportIfNeeded();
            if (session.HasUnacknowledgedLaunchResult)
            {
                ShowResultReport(session.LastLaunchResult);
                return;
            }
            if (model.HasGameEnded)
            {
                ShowEndingScreen();
                return;
            }
            EnsureSelectedEnginePresetUnlocked();
            selectedMission = model.GetCurrentMission();
            ShowResearchLab();
            PlayResearchCameraDrift();
            ShowEnginePreview();
            dateText.text = $"{model.Year}.Q{model.Quarter} / 남은 분기 : {model.RemainingTurns}";
            // 다음 분기 예산은 자기 노드를 잃고 보유 자금 텍스트의 둘째 줄이 됐다 — 프리팹에
            // "QuarterlyFunding" 노드가 더는 없으므로 여기서 찾으면 화면 초기화가 실패한다.
            fundsText.text = $"보유 자금 : {model.Funds} $\n다음 분기 : {model.QuarterlyFunding:+#;-#;0} $";

            foreach (EnginePresetConfig config in ResearchPrototypeModel.GetEnginePresetConfigs())
            {
                RefreshEngineCard(config);
            }

            EnginePresetState selectedEngine = model.GetEnginePreset(selectedEnginePreset);
            if (selectedEngineCompletion != null) selectedEngineCompletion.SetValueWithoutNotify(selectedEngine.Completion);
            LaunchMissionConfig selectedMissionConfig = model.GetConfiguredMissionConfig(selectedMission);
            LaunchMissionState selectedMissionState = model.GetMission(selectedMission);
            int designEntryCost = model.GetDesignEntryCost(selectedMission);

            // 한 덩어리였던 세 줄을 노드로 나눈다 — 이름은 좌상단, 성능은 우상단으로 갈라 붙이려면
            // 문자열 하나로는 정렬을 나눌 수 없다. 기본·집중 연구 비용은 여기서 빼고 연구 버튼 라벨에만
            // 남긴다(같은 값을 두 군데 쓰면 한쪽이 낡는다).
            selectedEngineNameText.text = model.GetEnginePresetName(selectedEnginePreset);
            selectedEnginePerformanceText.text = $"성능 {model.CalculateEnginePerformanceScore(selectedEnginePreset)}";
            selectedEngineInstallCostText.text = $"설치 {model.GetEngineInstallCost(selectedEnginePreset)}";
            selectedEngineText.text = $"완성도 {selectedEngine.Completion}/{ResearchPrototypeModel.MaxEngineCompletion}";
            selectedStatText.text = $"선택 스탯: {ResearchPrototypeModel.GetStatDisplayName(selectedStat)}";

            for (int index = 0; index < StatCount; index++)
            {
                var statId = (EngineStatId)index;
                int statValue = selectedEngine.GetStat(statId);
                statRowLabels[index].text = $"{ResearchPrototypeModel.GetStatDisplayName(statId)} {statValue}";
                if (statGauges[index] != null) statGauges[index].SetValueWithoutNotify(statValue);
                SetSelectedTint(statButtons[index], selectedStat == statId);
            }

            // Never strand the player in a mode they can no longer pay for.
            if (focusedResearchSelected && !CanResearch(selectedEngine, model.ConfiguredEngineFocusedResearchCost))
            {
                focusedResearchSelected = false;
            }

            // 둘째 줄(비용/소요 분기)은 리치텍스트 size 태그로 줄인다 — 라벨 하나 안에서 줄마다 크기를
            // 달리 하려면 TMP 태그밖에 방법이 없다. 태그가 먹으려면 해당 Label의 Auto Size가 꺼져 있어야 한다.
            string normalResearchTime = model.HasFreeNormalResearch(selectedEnginePreset) ? "시간 0분기" : "+1분기";
            normalResearchButtonText.text = $"기본연구\n<size=13>-{model.ConfiguredEngineNormalResearchCost}$ / {normalResearchTime}</size>";
            focusedResearchButtonText.text = $"집중연구\n<size=13>-{model.ConfiguredEngineFocusedResearchCost}$ / +1분기</size>";
            SetSelectedTint(normalResearchButton, !focusedResearchSelected);
            SetSelectedTint(focusedResearchButton, focusedResearchSelected);
            createEnginePresetButtonText.text = model.ActiveEnginePresetCount >= ResearchPrototypeModel.MaxEnginePresetCount
                ? "새로운 엔진 개발 최대 10개"
                : $"새로운 엔진 개발 -{model.ConfiguredNewEnginePresetCost}$";
            partDevelopmentButtonText.text = $"부품 개발\n<size=15>-{model.ConfiguredEngineNormalResearchCost}$~</size>";
            enterDesignButtonText.text = $"로켓 설계\n<size=15>-{designEntryCost}$</size>";
            waitButtonText.text = $"건너뛰기\n<size=15>+{model.NextWaitFunding}$ / +1분기</size>";

            RefreshPendingEffects();

            partDevelopmentButton.interactable = !model.DeadlineReached && model.Funds >= model.ConfiguredEngineNormalResearchCost;
            startDevelopmentButton.interactable = CanResearch(selectedEngine, focusedResearchSelected
                ? model.ConfiguredEngineFocusedResearchCost
                : model.ConfiguredEngineNormalResearchCost);
            normalResearchButton.interactable = CanResearch(selectedEngine, model.ConfiguredEngineNormalResearchCost);
            focusedResearchButton.interactable = CanResearch(selectedEngine, model.ConfiguredEngineFocusedResearchCost);
            createEnginePresetButton.interactable = !model.DeadlineReached && model.ActiveEnginePresetCount < ResearchPrototypeModel.MaxEnginePresetCount;
            enterDesignButton.interactable = CanEnterDesign(selectedMissionState, selectedMissionConfig, selectedEngine);
            waitButton.interactable = !model.DeadlineReached;
            if (isTransitioningToDesign)
            {
                SetResearchControlsInteractable(false);
            }
        }

        private static void SetSelectedTint(Button button, bool selected)
        {
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = selected ? Color.white : new Color(0.6f, 0.66f, 0.72f, 1f);
        }

        private string FormatResearchStatusText(ResearchPrototypeModel source)
        {
            return string.IsNullOrEmpty(source.PendingLaunchEffectsText)
                ? string.Empty : $"남은 이벤트 효과:\n{source.PendingLaunchEffectsText}";
        }

        private void RefreshPendingEffects()
        {
            if (statusText == null || model == null) return;
            statusText.text = FormatResearchStatusText(model);
            statusText.gameObject.SetActive(!string.IsNullOrEmpty(statusText.text));
            RectTransform effectsRect = statusText.rectTransform;
            effectsRect.anchorMin = effectsRect.anchorMax = new Vector2(0.5f, 0f);
            effectsRect.pivot = new Vector2(0.5f, 0f);
            effectsRect.anchoredPosition = partDevelopmentOpen ? new Vector2(-50f, 20f) : new Vector2(0f, 136f);
            effectsRect.sizeDelta = new Vector2(partDevelopmentOpen ? 480f : 720f, 100f);
        }

        private void RefreshEngineCard(EnginePresetConfig config)
        {
            EnginePresetState engine = model.GetEnginePreset(config.Id);
            EngineCardView card = engineCards[(int)config.Id];
            card.Button.gameObject.SetActive(model.IsEnginePresetUnlocked(config.Id));
            bool selected = selectedEnginePreset == config.Id;
            Image background = card.Button.GetComponent<Image>();
            background.color = background.sprite != null
                ? selected ? Color.white : new Color(0.65f, 0.75f, 0.8f, 1f)
                : selected ? new Color(0.28f, 0.35f, 0.42f, 1f) : new Color(0.19f, 0.23f, 0.28f, 1f);
            card.Title.text = model.GetEnginePresetName(config.Id);
            card.Detail.text = $"완성도 {engine.Completion} 성능 {model.CalculateEnginePerformanceScore(config.Id)}";
        }

        private bool CanResearch(EnginePresetState engine, int cost)
        {
            return !model.DeadlineReached
                && engine.Unlocked
                && engine.Completion < ResearchPrototypeModel.MaxEngineCompletion
                && model.Funds >= cost;
        }

        private bool CanEnterDesign(LaunchMissionState mission, LaunchMissionConfig config, EnginePresetState engine)
        {
            return !model.DeadlineReached
                && engine.Unlocked
                && mission.Unlocked
                && model.Funds >= model.GetDesignEntryCost(config.Id);
        }

        private void EnsureSelectedEnginePresetUnlocked()
        {
            if (model.IsEnginePresetUnlocked(selectedEnginePreset))
            {
                return;
            }

            selectedEnginePreset = EnginePresetId.Engine01;
        }

        private void ShowDesignScreen()
        {
            if (Application.isPlaying) SoundManager.Instance?.PlayBgm("LaunchPanelLoop");
            RequestedScreenName = ResearchFlowSession.DesignScreenName;
            isTransitioningToDesign = false;
            KillResearchCameraDrift(resetRotation: true);
            HideEnginePreview();
            HideResearchLab();
            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(false);
            }

            if (activeDesignController != null)
            {
                activeDesignController.HideForReuse();
                activeDesignController = null;
            }

            if (TryOpenSimulationDesignStage())
            {
                return;
            }

            activeDesignController = ResolveDesignScreenController();
            if (activeDesignController == null)
            {
                Debug.LogError("Research design screen controller must be preplaced in 01_Main.", this);
                ReturnFromDesignScreen();
                return;
            }

            activeDesignController.gameObject.SetActive(true);
            activeDesignController.enabled = true;
            activeDesignController.Initialize(session, ReturnFromDesignScreen, ShowResultReport);
        }

        public void ReturnFromDesignScreen()
        {
            if (activeDesignController != null)
            {
                activeDesignController.HideForReuse();
                activeDesignController = null;
            }

            if (session.HasUnacknowledgedLaunchResult)
            {
                ShowResultReport(session.LastLaunchResult);
                return;
            }
            if (model.HasGameEnded)
            {
                ShowEndingScreen();
                return;
            }

            RequestedScreenName = ResearchFlowSession.ResearchScreenName;
            if (Application.isPlaying) SoundManager.Instance?.PlayBgm("EngineBGM");
            isTransitioningToDesign = false;
            // A hard reset back to the hub, not an animated close — the canvas is still off at this point.
            closingPartDevelopment = false;
            ApplyPartDevelopmentState(false);
            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(true);
            }

            ShowResearchLab();
            if (initialized)
            {
                Refresh();
                PlayResearchEntryAnimation(PanelGroup.Hub);
            }
        }

        private void ShowResultReport(ResearchLaunchResultData result)
        {
            if (resultReport == null)
            {
                Debug.LogError("Research result report prefab must be assigned in the scene.", this);
                return;
            }
            if (resultReport.gameObject.activeSelf) return;
            if (activeDesignController != null)
            {
                activeDesignController.HideForReuse();
                activeDesignController = null;
            }

            RequestedScreenName = "ResultReport";
            if (Application.isPlaying) SoundManager.Instance?.StopBgm();
            if (canvasTransform != null) canvasTransform.gameObject.SetActive(false);
            HideEnginePreview();
            HideResearchLab();
            KillResearchCameraDrift(resetRotation: true);
            resultReport.Initialize(session, result, () =>
            {
                session.AcknowledgeLaunchResult();
                if (model.HasGameEnded)
                {
                    if (session.QueueDeadlineFailureReportIfNeeded())
                    {
                        ShowResultReport(session.LastLaunchResult);
                        return;
                    }

                    ShowEndingScreen();
                    return;
                }

                ReturnFromDesignScreen();
            });
            session.PublishPendingLaunchOutcome();
        }

        private void ShowEndingScreen()
        {
            if (endingScreen == null)
            {
                Debug.LogError("Research ending prefab must be assigned in the scene.", this);
                return;
            }
            if (endingScreen.gameObject.activeSelf) return;
            RequestedScreenName = "Ending";
            if (Application.isPlaying) SoundManager.Instance?.StopBgm();
            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(false);
            }

            HideEnginePreview();
            HideResearchLab();
            KillResearchCameraDrift(resetRotation: true);
            endingScreen.Initialize(session, () =>
            {
                ResetResearchState();
                ReturnFromDesignScreen();
            });
        }

        private void ResetResearchState()
        {
            if (visibilityDialog != null) visibilityDialog.Hide();
            KillDesignTransition();
            KillWaitFade();
            isTransitioningToDesign = false;
            focusedResearchSelected = false;
            closingPartDevelopment = false;
            ApplyPartDevelopmentState(false);
            resultReport?.Hide();
            endingScreen?.Hide();
            session.ResetResearch();
            model = session.Model;
            selectedEnginePreset = EnginePresetId.Engine01;
            selectedStat = EngineStatId.FuelCapacity;
            selectedMission = model.GetCurrentMission();
        }

        /// <summary>
        /// The reset used to hang off a 초기화 button in the top bar; the bar now carries only
        /// title, date and funds, so this is the remaining entry point into the same state reset.
        /// </summary>
        public void ResetResearchForTests()
        {
            ResetResearchState();
            Refresh();
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
            ResetResearchState();
            model.PrepareDebugDesignEntryState(selectedMission, selectedEnginePreset);

            if (session.TryEnterDesign(selectedMission, selectedEnginePreset, out _) == ResearchActionResult.Success)
            {
                BeginDesignTransition();
            }
        }

#endif
        private static bool TryOpenSimulationDesignStage()
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            Type hostType = Type.GetType("Simulation.SimulationStageHost, Simulation");
            MethodInfo method = hostType?.GetMethod("OpenDesignStage", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                return false;
            }

            object result = method.Invoke(null, null);
            return result is bool opened && opened;
        }

        private void ShowEnginePreview()
        {
            ResolveEnginePreview();
            if (enginePreview != null)
            {
                enginePreview.ShowHologram(selectedEnginePreset, GetSelectedEngineArchetype());
            }
        }

        private void HideEnginePreview()
        {
            ResolveEnginePreview();
            if (enginePreview != null)
            {
                enginePreview.Hide();
            }
        }

        private void ShowResearchLab()
        {
            ResolveResearchLabRoot();
            if (researchLabRoot != null)
            {
                researchLabRoot.gameObject.SetActive(true);
            }
        }

        private void HideResearchLab()
        {
            ResolveResearchLabRoot();
            if (researchLabRoot != null)
            {
                researchLabRoot.gameObject.SetActive(false);
            }
        }

        private void BeginDesignTransition()
        {
            KillDesignTransition();
            KillWaitFade();
            isTransitioningToDesign = true;
            SetResearchControlsInteractable(false);
            ShowResearchLab();
            ResolveEnginePreview();
            ReturnResearchCameraToBase();

            EngineVisualArchetype archetype = GetSelectedEngineArchetype();
            int pendingAnimations = 0;
            bool delayStarted = false;

            void RegisterAnimation()
            {
                pendingAnimations++;
            }

            void CompleteAnimation()
            {
                pendingAnimations--;
                if (pendingAnimations <= 0 && !delayStarted)
                {
                    delayStarted = true;
                    PlayDesignOpenDelay();
                }
            }

            if (operationTransitionAnimator != null)
            {
                RegisterAnimation();
                operationTransitionAnimator.PlayExit(PanelGroup.All, CompleteAnimation);
            }

            if (enginePreview != null)
            {
                RegisterAnimation();
                enginePreview.PlayMaterialize(selectedEnginePreset, archetype, CompleteAnimation);
            }

            if (pendingAnimations == 0)
            {
                PlayDesignOpenDelay();
            }
        }

        private void PlayDesignOpenDelay()
        {
            designTransitionSequence = DOTween.Sequence()
                .SetTarget(this)
                .AppendInterval(DesignTransitionDelaySeconds)
                .OnComplete(() =>
                {
                    designTransitionSequence = null;
                    ShowDesignScreen();
                });
        }

        private void PlayResearchEntryAnimation(PanelGroup group)
        {
            if (operationTransitionAnimator == null || canvasTransform == null || !canvasTransform.gameObject.activeInHierarchy)
            {
                return;
            }

            operationTransitionAnimator.PlayEnter(group);
        }

        private void PlayResearchCameraDrift()
        {
            ResolveResearchCameraTransform();
            if (researchCameraTransform == null || (cameraPitchDriftDegrees <= 0f && cameraYawOrbitDegrees <= 0f))
            {
                return;
            }

            if (researchCameraDriftTween != null && researchCameraDriftTween.IsActive())
            {
                return;
            }

            KillResearchCameraDrift(resetRotation: false);
            if (!hasResearchCameraBaseLocalRotation)
            {
                researchCameraBaseLocalRotation = researchCameraTransform.localRotation;
                researchCameraBasePosition = researchCameraTransform.position;
                hasResearchCameraBaseLocalRotation = true;
            }

            researchCameraOrbitPivot = ResolveCameraOrbitPivot();

            // One tween drives both yaw and pitch: they share localRotation, so separate
            // tweens would overwrite each other. t is absolute in [-1, 1], which is Yoyo-safe.
            float t = -1f;
            ApplyResearchCameraOrbit(t);
            researchCameraDriftTween = DOTween.To(
                    () => t,
                    value =>
                    {
                        t = value;
                        ApplyResearchCameraOrbit(value);
                    },
                    1f,
                    cameraYawOrbitCycleSeconds * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }

        private void ApplyResearchCameraOrbit(float t)
        {
            if (researchCameraTransform == null)
            {
                return;
            }

            Quaternion yaw = Quaternion.AngleAxis(t * cameraYawOrbitDegrees, Vector3.up);
            researchCameraTransform.SetPositionAndRotation(
                researchCameraOrbitPivot + yaw * (researchCameraBasePosition - researchCameraOrbitPivot),
                yaw * researchCameraBaseLocalRotation * Quaternion.Euler(t * cameraPitchDriftDegrees, 0f, 0f));
        }

        private Vector3 ResolveCameraOrbitPivot()
        {
            ResolveEnginePreview();
            // No engine to orbit: pivot on the camera itself, so yaw degenerates to an in-place pan.
            return enginePreview != null && enginePreview.PreviewRoot != null
                ? enginePreview.PreviewRoot.position
                : researchCameraTransform.position;
        }

        private void ReturnResearchCameraToBase()
        {
            KillResearchCameraDrift(resetRotation: false);
            if (!hasResearchCameraBaseLocalRotation || researchCameraTransform == null)
            {
                return;
            }

            if (cameraReturnSeconds <= 0f)
            {
                researchCameraTransform.SetPositionAndRotation(researchCameraBasePosition, researchCameraBaseLocalRotation);
                return;
            }

            researchCameraReturnTween = DOTween.Sequence()
                .SetTarget(this)
                .Join(researchCameraTransform.DOMove(researchCameraBasePosition, cameraReturnSeconds).SetEase(Ease.InOutSine))
                .Join(researchCameraTransform.DORotateQuaternion(researchCameraBaseLocalRotation, cameraReturnSeconds).SetEase(Ease.InOutSine))
                .OnComplete(() => researchCameraReturnTween = null);
        }

        private void KillResearchCameraDrift(bool resetRotation)
        {
            if (researchCameraDriftTween != null)
            {
                researchCameraDriftTween.Kill();
                researchCameraDriftTween = null;
            }

            if (researchCameraReturnTween != null)
            {
                researchCameraReturnTween.Kill();
                researchCameraReturnTween = null;
            }

            if (resetRotation && hasResearchCameraBaseLocalRotation && researchCameraTransform != null)
            {
                researchCameraTransform.SetPositionAndRotation(researchCameraBasePosition, researchCameraBaseLocalRotation);
            }
        }

        private EngineVisualArchetype GetSelectedEngineArchetype()
        {
            return model != null ? EngineVisualClassifier.Classify(model.GetEnginePreset(selectedEnginePreset)) : EngineVisualArchetype.Balanced;
        }

        private void KillDesignTransition()
        {
            if (designTransitionSequence == null)
            {
                return;
            }

            designTransitionSequence.Kill();
            designTransitionSequence = null;
        }

        private void SetResearchControlsInteractable(bool interactable)
        {
            for (int i = 0; i < engineCards.Length; i++)
            {
                if (engineCards[i].Button != null)
                {
                    engineCards[i].Button.interactable = interactable;
                }
            }

            if (partDevelopmentButton != null)
            {
                partDevelopmentButton.interactable = interactable;
            }

            if (startDevelopmentButton != null)
            {
                startDevelopmentButton.interactable = interactable;
            }

            if (cancelDevelopmentButton != null)
            {
                cancelDevelopmentButton.interactable = interactable;
            }

            if (normalResearchButton != null)
            {
                normalResearchButton.interactable = interactable;
            }

            if (focusedResearchButton != null)
            {
                focusedResearchButton.interactable = interactable;
            }

            if (createEnginePresetButton != null)
            {
                createEnginePresetButton.interactable = interactable;
            }

            if (enterDesignButton != null)
            {
                enterDesignButton.interactable = interactable;
            }

            if (waitButton != null)
            {
                waitButton.interactable = interactable;
            }
        }

        public bool IsTransitioningToDesignForTests()
        {
            return isTransitioningToDesign;
        }

        public void CompleteDesignTransitionForTests()
        {
            operationTransitionAnimator?.CompleteActiveSequenceForTests();
            enginePreview?.CompleteMaterializeForTests();

            if (designTransitionSequence != null)
            {
                designTransitionSequence.Complete();
            }
        }

        private void ResolveEnginePreview()
        {
            if (enginePreview != null)
            {
                return;
            }

            enginePreview = GetComponentInChildren<ResearchEnginePreviewController>(true);
            if (enginePreview != null)
            {
                return;
            }

            foreach (ResearchEnginePreviewController preview in FindObjectsByType<ResearchEnginePreviewController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                enginePreview = preview;
                return;
            }
        }

        private void ResolveResearchLabRoot()
        {
            if (researchLabRoot != null)
            {
                return;
            }

            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == "Engine Research Lab")
                {
                    researchLabRoot = candidate;
                    return;
                }
            }
        }

        private void ResolveResearchCameraTransform()
        {
            if (researchCameraTransform != null)
            {
                return;
            }

            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == ResearchCinemachineCameraName)
                {
                    researchCameraTransform = candidate;
                    return;
                }
            }
        }

        private ResearchMiniGameController ResolveMiniGameController()
        {
            if (miniGameController != null)
            {
                return miniGameController;
            }

            miniGameController = GetComponentInChildren<ResearchMiniGameController>(true);
            if (miniGameController != null)
            {
                return miniGameController;
            }

            miniGameController = FindSceneObject<ResearchMiniGameController>();
            if (miniGameController != null || !CanCreateRuntimeUiFallback())
            {
                return miniGameController;
            }

            var host = new GameObject("Research Mini Game Controller");
            host.transform.SetParent(transform, false);
            miniGameController = host.AddComponent<ResearchMiniGameController>();
            return miniGameController;
        }

        private ResearchDesignScreenController ResolveDesignScreenController()
        {
            if (designScreenController != null)
            {
                return designScreenController;
            }

            designScreenController = GetComponentInChildren<ResearchDesignScreenController>(true);
            if (designScreenController != null)
            {
                return designScreenController;
            }

            designScreenController = FindSceneObject<ResearchDesignScreenController>();
            if (designScreenController != null || !CanCreateRuntimeUiFallback())
            {
                return designScreenController;
            }

            var host = new GameObject("Research Design Screen Controller");
            host.transform.SetParent(transform, false);
            designScreenController = host.AddComponent<ResearchDesignScreenController>();
            return designScreenController;
        }

        private bool CanCreateRuntimeUiFallback()
        {
            return !Application.isPlaying || gameObject.scene.name != ResearchFlowSession.MainSceneName;
        }

        private static T FindSceneObject<T>()
            where T : Component
        {
            foreach (T component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                return component;
            }

            return null;
        }

        private void EnsureResearchCameraRuntime()
        {
            ResolveResearchCameraTransform();
            if (researchCameraTransform != null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            Type brainType = Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
            Type cameraType = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            if (brainType == null || cameraType == null)
            {
                return;
            }

            if (mainCamera.GetComponent(brainType) == null)
            {
                mainCamera.gameObject.AddComponent(brainType);
            }

            var virtualCameraObject = new GameObject(ResearchCinemachineCameraName);
            virtualCameraObject.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
            Component virtualCamera = virtualCameraObject.AddComponent(cameraType);
            ConfigureRuntimeCinemachineCamera(virtualCamera, mainCamera);
            researchCameraTransform = virtualCameraObject.transform;
        }

        private static void ConfigureRuntimeCinemachineCamera(Component virtualCamera, Camera sourceCamera)
        {
            if (virtualCamera == null || sourceCamera == null)
            {
                return;
            }

            FieldInfo priorityField = FindFieldInTypeHierarchy(virtualCamera.GetType(), "Priority");
            if (priorityField != null)
            {
                object priority = priorityField.GetValue(virtualCamera);
                SetFieldValue(priority, "Enabled", true);
                SetFieldValue(priority, "m_Value", ResearchCinemachineCameraPriority);
                priorityField.SetValue(virtualCamera, priority);
            }

            FieldInfo lensField = FindFieldInTypeHierarchy(virtualCamera.GetType(), "Lens");
            if (lensField != null)
            {
                object lens = lensField.GetValue(virtualCamera);
                SetFieldValue(lens, "FieldOfView", sourceCamera.fieldOfView);
                SetFieldValue(lens, "NearClipPlane", sourceCamera.nearClipPlane);
                SetFieldValue(lens, "FarClipPlane", sourceCamera.farClipPlane);
                lensField.SetValue(virtualCamera, lens);
            }
        }

        private static bool SetFieldValue(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return false;
            }

            FieldInfo field = FindFieldInTypeHierarchy(target.GetType(), fieldName);
            if (field == null)
            {
                return false;
            }

            field.SetValue(target, value);
            return true;
        }

        private static FieldInfo FindFieldInTypeHierarchy(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
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
            ConfigureCardButton(button, preferredHeight, out title, out detail);
            return button;
        }

        private static void ConfigureCardButton(Button button, float preferredHeight, out TMP_Text title, out TMP_Text detail)
        {
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = button.gameObject.AddComponent<LayoutElement>();
            }

            layout.flexibleWidth = 1f;
            layout.preferredHeight = preferredHeight;

            title = FindChildText(button.transform, "Title");
            detail = FindChildText(button.transform, "Detail");
        }

        private static void ConfigureDynamicMultilineText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMin = text.fontSizeMin > 0f ? Math.Min(text.fontSizeMin, 12f) : 12f;
            text.fontSizeMax = text.fontSize;
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
