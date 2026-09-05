using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.Research
{
    public readonly struct ResearchMiniGameResult
    {
        public ResearchMiniGameResult(EnginePresetId presetId, EngineStatId statId, bool focused, int score)
        {
            PresetId = presetId;
            StatId = statId;
            Focused = focused;
            Score = ResearchPrototypeModel.ClampInt(score, 0, 100);
        }

        public EnginePresetId PresetId { get; }
        public EngineStatId StatId { get; }
        public bool Focused { get; }
        public int Score { get; }
    }

    public sealed class ResearchMiniGameController : MonoBehaviour
    {
        private const string MiniGameScreenPrefabPath = "ResearchUI/ResearchMiniGameScreen";
        private const float ResultDismissSeconds = 2f;
        private const float FuelJudgementSeconds = 2f;
        private const float OutputJudgementSeconds = 2f;
        private const float CoolingDurationSeconds = 9f;
        private const float CoolingInitialHeat = 0.4f;
        private const float CoolingHeatPerSecond = 0.1f;
        private const float CoolingHeatPerTargetRotation = 1.2f;
        private const float NoInputReactionSeconds = 9f;
        private const float PerfectJudgementThreshold = 0.02f;
        private const float GreatJudgementThreshold = 0.08f;
        private const float GoodJudgementThreshold = 0.16f;
        private const int FuelAttemptCount = 1;
        private const float FuelOverfillSeconds = 2f;
        private const float FuelMinimumFillSeconds = 1.8f;
        private const float FuelMaximumFillSeconds = 4.2f;
        // Measured from gage.png around the hub at pixel (64, 85).
        public const float FuelMinimumAngle = 69f;
        public const float FuelMaximumAngle = -68f;
        public const float FuelPassStart = (69f + 39.4f) / 137f;
        public const float FuelPassEnd = (69f + 62.7f) / 137f;
        private const float CoolingMinimumTurns = 10f;
        private const float CoolingMaximumTurns = 12f;
        private const float OutputDurationSeconds = 5f;
        private const float ValveCenterDeadZone = 0.12f;
        private const int OutputStageCount = 3;
        private const int IgnitionRoundCount = 3;
        private const float IgnitionClickFlashSeconds = 0.18f;

        private readonly float[] fuelScores = new float[FuelAttemptCount];
        private readonly float[] outputErrors = new float[OutputStageCount];
        private readonly int[] ignitionSequence = new int[4];
        private readonly Button[] ignitionButtons = new Button[4];

        [SerializeField] private GameObject miniGameScreenPrefab;

        private EnginePresetId presetId;
        private EngineStatId statId;
        private bool focused;
        private Action<ResearchMiniGameResult> completedCallback;
        private ResearchMiniGameResult pendingResult;
        private System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        private bool initialized;
        private bool interfaceBuilt;
        private bool gameCompleted;
        private bool resultShowing;
        private bool resultDismissed;
        private bool fuelJudgementShowing;
        private bool outputJudgementShowing;
        private float elapsedSeconds;
        private float roundElapsedSeconds;
        private float resultElapsedSeconds;
        private float fuelJudgementElapsedSeconds;
        private float outputJudgementElapsedSeconds;
        private int roundIndex;
        private int fuelAttemptIndex;
        private int outputStageIndex;
        private int ignitionInputIndex;
        private int ignitionCorrectInputs;
        private int ignitionTotalInputs;
        private float ignitionReactionTotal;
        private float fuelGaugeValue;
        private float fuelFillDuration;
        private float fuelHoldSeconds;
        // Calibrates cooling per turn; reaching this rotation never ends the game early.
        private float coolingTargetDegrees;
        private float coolingDegrees;
        private float coolingHeat;
        private bool coolingDragging;
        private bool coolingHasAngle;
        private float coolingPreviousAngle;
        private float outputTargetValue;
        private float outputGaugeValue;
        private bool fuelFilling;
        private bool ignitionShowingSequence;
        private int ignitionClickedIndex = -1;
        private bool ignitionAdvancePending;
        private Tween feedbackTween;

        private TMP_Text titleText;
        private TMP_Text instructionText;
        private TMP_Text timerText;
        private TMP_Text stateText;
        private TMP_Text fuelStatusText;
        private TMP_Text fuelJudgementText;
        private RectTransform fuelGameGroup;
        private RectTransform coolingGameGroup;
        private RectTransform outputGameGroup;
        private RectTransform ignitionGameGroup;
        private RectTransform resultGroup;
        private Image fuelFillImage;
        private Image fuelDialImage;
        private Image fuelReadoutImage;
        private RectTransform fuelNeedle;
        private Image outputCursorImage;
        private TMP_Text outputLabelText;
        private TMP_Text outputJudgementText;
        private RectTransform outputSafeZone;
        private Image coolingPipeImage;
        private Image coolingValveImage;
        private TMP_Text coolingProgressText;
        private Material coolingPipeMaterial;
        private Material coolingValveMaterial;
        private Material fuelReadoutMaterial;
        private RectTransform playArea;
        private Button primaryButton;
        private TMP_Text resultScoreText;
        private TMP_Text resultDetailText;
        private GameObject interfaceRoot;
        private Camera fallbackRenderingCamera;

        public EngineStatId StatId => statId;
        public bool IsCompleted => gameCompleted;
        public bool IsShowingResult => resultShowing;

        public void InitializeForTests(EnginePresetId nextPresetId, EngineStatId nextStatId, bool nextFocused, Action<ResearchMiniGameResult> onCompleted)
        {
            Initialize(nextPresetId, nextStatId, nextFocused, onCompleted);
        }

        public void InitializeForTests(EnginePresetId nextPresetId, EngineStatId nextStatId, bool nextFocused, int randomSeed, Action<ResearchMiniGameResult> onCompleted)
        {
            random = new System.Random(randomSeed);
            Initialize(nextPresetId, nextStatId, nextFocused, onCompleted);
        }

        public void Initialize(EnginePresetId nextPresetId, EngineStatId nextStatId, bool nextFocused, Action<ResearchMiniGameResult> onCompleted)
        {
            presetId = nextPresetId;
            statId = nextStatId;
            focused = nextFocused;
            completedCallback = onCompleted;

            if (!interfaceBuilt && !BuildInterface())
            {
                return;
            }

            ResetRunState();
            if (interfaceRoot != null)
            {
                interfaceRoot.SetActive(true);
            }

            StartStatGame();
            initialized = true;
        }

        public void HideForReuse()
        {
            feedbackTween?.Kill();
            feedbackTween = null;
            completedCallback = null;
            if (interfaceRoot != null)
            {
                interfaceRoot.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (coolingPipeMaterial != null) DestroyUnityObject(coolingPipeMaterial);
            if (coolingValveMaterial != null) DestroyUnityObject(coolingValveMaterial);
            if (fuelReadoutMaterial != null) DestroyUnityObject(fuelReadoutMaterial);
            if (feedbackTween != null)
            {
                feedbackTween.Kill();
                feedbackTween = null;
            }
        }

        public void ConfigureScreenPrefabForTests(GameObject screenTemplate)
        {
            miniGameScreenPrefab = screenTemplate;
        }

        public void ForceCompleteForTests(int score)
        {
            Complete(score);
        }

        public void ForceDismissForTests()
        {
            DismissResult();
        }

        public void RecordFuelAttemptForTests(float normalizedFillValue)
        {
            fuelGaugeValue = Mathf.Clamp01(normalizedFillValue);
            fuelHoldSeconds = Mathf.Max(0f, normalizedFillValue) * fuelFillDuration;
            fuelFilling = true;
            RecordFuelAttempt();
        }

        public void AdvanceTimeForTests(float seconds) => Tick(Mathf.Max(0f, seconds));
        public void BeginFuelFillForTests() => BeginFuelFill();
        public void ReleaseFuelForTests() => RecordFuelAttempt();
        public void RotateValveForTests(Vector2 position, bool begin = false)
        {
            if (begin) { coolingDragging = true; coolingHasAngle = false; }
            DragValveLocal(position);
        }
        public void ReleaseValveForTests() { coolingDragging = false; coolingHasAngle = false; }
        public float GetFuelDurationForTests() => fuelFillDuration;
        public float GetCoolingTargetForTests() => coolingTargetDegrees;
        public float GetCoolingDegreesForTests() => coolingDegrees;
        public float GetCoolingHeatForTests() => coolingHeat;
        public float GetOutputTargetForTests() => outputTargetValue;
        public float GetOutputCursorForTests() => outputGaugeValue;

        public void ForceAdvanceFuelJudgementForTests()
        {
            AdvanceFuelAfterJudgement();
        }

        public void RecordOutputStageForTests(float normalizedFillValue)
        {
            outputGaugeValue = Mathf.Clamp01(normalizedFillValue);
            RecordOutputStage();
        }

        public void ForceAdvanceOutputJudgementForTests()
        {
            AdvanceOutputAfterJudgement();
        }

        public string GetStateTextForTests()
        {
            return stateText == null ? string.Empty : stateText.text;
        }

        public string GetTimerTextForTests()
        {
            return timerText == null || !timerText.gameObject.activeSelf ? string.Empty : timerText.text;
        }

        public bool IsShowingFuelJudgementForTests => fuelJudgementShowing;
        public bool IsShowingOutputJudgementForTests => outputJudgementShowing;

        public float GetFuelTargetForTests()
        {
            return FuelPassEnd;
        }

        public int[] GetIgnitionSequenceForTests()
        {
            int length = GetIgnitionRoundLength(roundIndex);
            var sequence = new int[length];
            Array.Copy(ignitionSequence, sequence, length);
            return sequence;
        }

        public static string FormatStateText(string baseText, bool showExample)
        {
            return baseText;
        }

        public static int CalculateFuelAttemptScore(float fill, float overflowSeconds)
        {
            float value = Mathf.Clamp01(fill);
            float score = value >= FuelPassStart && value <= FuelPassEnd
                ? Mathf.Lerp(80f, 100f, Mathf.InverseLerp(FuelPassStart, FuelPassEnd, value))
                : Mathf.Clamp(49f * (1f - Mathf.Min(
                    Mathf.Abs(value - FuelPassStart),
                    Mathf.Abs(value - FuelPassEnd)) / 0.25f), 0f, 49f);
            return Mathf.Clamp(Mathf.RoundToInt(score - 100f * Mathf.Max(0f, overflowSeconds) / FuelOverfillSeconds), 0, 100);
        }

        public static string GetFuelJudgementText(float normalizedError)
        {
            float error = Mathf.Abs(normalizedError);
            return error <= PerfectJudgementThreshold + 0.000001f ? "Perfect!"
                : error <= 0.2f + 0.000001f ? "Great" : "Miss";
        }

        public static float GetFuelNeedleAngle(float fill) => Mathf.Lerp(FuelMinimumAngle, FuelMaximumAngle, Mathf.Clamp01(fill));

        public static string GetOutputJudgementText(float normalizedError)
        {
            float error = Mathf.Abs(normalizedError);
            return error <= PerfectJudgementThreshold + 0.000001f ? "Perfect"
                : error <= GreatJudgementThreshold + 0.000001f ? "Great" : "Miss";
        }

        private static string GetAccuracyJudgementText(float normalizedError)
        {
            float error = Mathf.Abs(normalizedError);
            if (error <= PerfectJudgementThreshold)
            {
                return "Perfect!";
            }

            if (error <= GreatJudgementThreshold)
            {
                return "Great";
            }

            return error <= GoodJudgementThreshold ? "Good" : "Miss";
        }

        public static int CalculateCoolingScore(float heat)
        {
            return Mathf.RoundToInt(100f * (1f - Mathf.Clamp01(heat)));
        }

        public static int CalculateOutputAttemptScore(float cursor, float target)
        {
            string judgement = GetOutputJudgementText(Mathf.Abs(cursor - target));
            return judgement == "Perfect" ? 100 : judgement == "Great" ? 80 : 0;
        }

        public static int CalculateMaxOutputScore(params float[] normalizedErrors)
        {
            if (normalizedErrors == null || normalizedErrors.Length == 0) return 0;
            float total = 0f;
            foreach (float error in normalizedErrors) total += CalculateOutputAttemptScore(error, 0f);
            return Mathf.Clamp(Mathf.RoundToInt(total / normalizedErrors.Length), 0, 100);
        }

        private int AverageFuelScore()
        {
            float total = 0f;
            foreach (float score in fuelScores) total += score;
            return Mathf.Clamp(Mathf.RoundToInt(total / FuelAttemptCount), 0, 100);
        }

        public static int CalculateIgnitionReliabilityScore(int correctInputs, int totalInputs, float averageReactionSeconds)
        {
            if (totalInputs <= 0)
            {
                return 0;
            }

            float accuracy = Mathf.Clamp01(correctInputs / (float)totalInputs);
            int accuracyScore = Mathf.RoundToInt(accuracy * 85f);
            int reactionScore = Mathf.RoundToInt(Mathf.Clamp01(1.35f - Mathf.Max(0f, averageReactionSeconds)) * 15f);
            return ResearchPrototypeModel.ClampInt(accuracyScore + reactionScore, 0, 100);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaSeconds)
        {
            if (!initialized)
            {
                return;
            }

            if (resultShowing)
            {
                resultElapsedSeconds += deltaSeconds;
                if (resultElapsedSeconds >= ResultDismissSeconds)
                {
                    DismissResult();
                }

                return;
            }

            if (gameCompleted)
            {
                return;
            }

            if (statId == EngineStatId.Cooling)
            {
                float activeSeconds = Mathf.Min(deltaSeconds, Mathf.Max(0f, CoolingDurationSeconds - elapsedSeconds));
                coolingHeat = Mathf.Clamp01(coolingHeat + activeSeconds * CoolingHeatPerSecond);
            }
            elapsedSeconds += deltaSeconds;
            roundElapsedSeconds += deltaSeconds;
            UpdateTimerText();

            if (fuelJudgementShowing)
            {
                fuelJudgementElapsedSeconds += deltaSeconds;
                if (fuelJudgementElapsedSeconds >= FuelJudgementSeconds)
                {
                    AdvanceFuelAfterJudgement();
                }

                return;
            }

            if (outputJudgementShowing)
            {
                outputJudgementElapsedSeconds += deltaSeconds;
                if (outputJudgementElapsedSeconds >= OutputJudgementSeconds)
                {
                    AdvanceOutputAfterJudgement();
                }

                return;
            }

            UpdateActiveGame(deltaSeconds);

            if (!gameCompleted && statId == EngineStatId.Cooling
                && (coolingHeat >= 1f || elapsedSeconds >= CoolingDurationSeconds))
            {
                Complete(CalculateCoolingScore(coolingHeat));
            }
        }

        private bool BuildInterface()
        {
            EnsureEventSystem();

#if UNITY_EDITOR
            if (miniGameScreenPrefab == null)
            {
                GameObject existingPrefab = Resources.Load<GameObject>(MiniGameScreenPrefabPath);
                if (existingPrefab == null || !MiniGamePrefabHasRequiredChildren(existingPrefab))
                {
                    RebuildDefaultPrefabsForEditor();
                }
            }
#endif

            GameObject prefab = miniGameScreenPrefab != null
                ? miniGameScreenPrefab
                : Resources.Load<GameObject>(MiniGameScreenPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("Research mini game UI prefab is missing. Expected Resources/ResearchUI/ResearchMiniGameScreen.");
                return false;
            }

            GameObject instance;
            bool createdInstance = false;
            Transform existingCanvas = transform.Find("ResearchMiniGameCanvas");
            if (existingCanvas != null)
            {
                instance = existingCanvas.gameObject;
            }
            else if (CanCreateRuntimeUiFallback())
            {
                instance = Instantiate(prefab, transform);
                instance.name = "ResearchMiniGameCanvas";
                createdInstance = true;
            }
            else
            {
                Debug.LogError("Research mini game UI must be preplaced in 01_Main.", this);
                return false;
            }

            interfaceRoot = instance;
            interfaceRoot.SetActive(true);
            RectTransform canvasTransform = instance.GetComponent<RectTransform>();
            if (canvasTransform == null)
            {
                canvasTransform = instance.AddComponent<RectTransform>();
            }

            Canvas canvas = instance.GetComponent<Canvas>() ?? instance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            if (instance.GetComponent<GraphicRaycaster>() == null)
            {
                instance.AddComponent<GraphicRaycaster>();
            }

            CanvasScaler scaler = instance.GetComponent<CanvasScaler>() ?? instance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureRenderingCamera();

            titleText = FindRequiredText(canvasTransform, "Title");
            timerText = FindRequiredText(canvasTransform, "Timer");
            instructionText = FindRequiredText(canvasTransform, "Instruction");
            stateText = FindRequiredText(canvasTransform, "State");
            primaryButton = FindRequiredButton(canvasTransform, "PrimaryActionButton");
            playArea = FindRequiredRectTransform(canvasTransform, "PlayArea");
            fuelGameGroup = FindRequiredRectTransform(canvasTransform, "FuelGame");
            coolingGameGroup = FindRequiredRectTransform(canvasTransform, "CoolingGame");
            outputGameGroup = FindRequiredRectTransform(canvasTransform, "OutputGame");
            ignitionGameGroup = FindRequiredRectTransform(canvasTransform, "IgnitionGame");
            resultGroup = FindRequiredRectTransform(canvasTransform, "ResultGame");
            fuelStatusText = FindRequiredText(canvasTransform, "FuelStatusText");
            fuelJudgementText = FindRequiredText(canvasTransform, "FuelJudgementText");
            fuelFillImage = FindRequiredImage(canvasTransform, "FuelFill");
            fuelDialImage = FindRequiredImage(canvasTransform, "FuelDial");
            fuelReadoutImage = FindRequiredImage(canvasTransform, "FuelReadout");
            fuelNeedle = FindRequiredRectTransform(canvasTransform, "FuelNeedle");
            coolingPipeImage = FindRequiredImage(canvasTransform, "CoolingPipe");
            coolingValveImage = FindRequiredImage(canvasTransform, "CoolingValve");
            coolingProgressText = FindRequiredText(canvasTransform, "CoolingProgressText");
            outputLabelText = FindRequiredText(canvasTransform, "OutputLabel");
            outputJudgementText = FindRequiredText(canvasTransform, "OutputJudgementText");
            outputCursorImage = FindRequiredImage(canvasTransform, "OutputCursor");
            outputSafeZone = FindRequiredRectTransform(canvasTransform, "SafeZone");
            resultScoreText = FindRequiredText(canvasTransform, "ResultScoreText");
            resultDetailText = FindRequiredText(canvasTransform, "ResultDetailText");

            if (titleText == null
                || timerText == null
                || instructionText == null
                || stateText == null
                || primaryButton == null
                || playArea == null
                || fuelGameGroup == null
                || coolingGameGroup == null
                || outputGameGroup == null
                || ignitionGameGroup == null
                || resultGroup == null
                || fuelStatusText == null
                || fuelJudgementText == null
                || fuelFillImage == null
                || fuelDialImage == null
                || fuelReadoutImage == null
                || fuelNeedle == null
                || coolingPipeImage == null
                || coolingValveImage == null
                || coolingProgressText == null
                || outputLabelText == null
                || outputJudgementText == null
                || outputCursorImage == null
                || outputSafeZone == null
                || resultScoreText == null
                || resultDetailText == null)
            {
                if (createdInstance)
                {
                    DestroyUnityObject(instance);
                }

                Debug.LogError("Research mini game UI prefab is invalid. Check required child names in ResearchMiniGameScreen.");
                return false;
            }

            coolingPipeMaterial = new Material(coolingPipeImage.material);
            coolingValveMaterial = new Material(coolingValveImage.material);
            coolingPipeImage.material = coolingPipeMaterial;
            coolingValveImage.material = coolingValveMaterial;
            fuelReadoutMaterial = new Material(fuelReadoutImage.material);
            fuelReadoutImage.material = fuelReadoutMaterial;

            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                ignitionButtons[i] = FindRequiredButton(canvasTransform, $"Igniter_{i}");
                if (ignitionButtons[i] == null)
                {
                    if (createdInstance)
                    {
                        DestroyUnityObject(instance);
                    }

                    Debug.LogError($"Research mini game UI prefab is invalid. Missing Igniter_{i}.");
                    return false;
                }
            }

            SetActiveGameGroup(null);
            interfaceBuilt = true;
            return true;
        }

        private void ResetRunState()
        {
            feedbackTween?.Kill();
            feedbackTween = null;
            gameCompleted = false;
            resultShowing = false;
            resultDismissed = false;
            fuelJudgementShowing = false;
            outputJudgementShowing = false;
            elapsedSeconds = 0f;
            roundElapsedSeconds = 0f;
            resultElapsedSeconds = 0f;
            fuelJudgementElapsedSeconds = 0f;
            outputJudgementElapsedSeconds = 0f;
            roundIndex = 0;
            fuelAttemptIndex = 0;
            outputStageIndex = 0;
            ignitionInputIndex = 0;
            ignitionCorrectInputs = 0;
            ignitionTotalInputs = 0;
            ignitionReactionTotal = 0f;
            fuelGaugeValue = 0f;
            fuelHoldSeconds = 0f;
            coolingDegrees = 0f;
            coolingHeat = CoolingInitialHeat;
            coolingDragging = false;
            coolingHasAngle = false;
            outputGaugeValue = 0f;
            fuelFilling = false;
            ignitionShowingSequence = false;
            ignitionClickedIndex = -1;
            ignitionAdvancePending = false;
            Array.Clear(fuelScores, 0, fuelScores.Length);
            for (int i = 0; i < outputErrors.Length; i++) outputErrors[i] = 1f;
            Array.Clear(ignitionSequence, 0, ignitionSequence.Length);
            SetActiveGameGroup(null);
        }

        private void StartStatGame()
        {
            titleText.text = $"{ResearchPrototypeModel.GetStatDisplayName(statId)} {(focused ? "집중" : "일반")} 개발";
            elapsedSeconds = 0f;
            roundElapsedSeconds = 0f;
            roundIndex = 0;
            timerText.gameObject.SetActive(statId == EngineStatId.Cooling);
            UpdateTimerText();

            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    instructionText.text = "계기판을 누르고 있다가 바늘이 하늘색 구간의 가장 오른쪽 흰색 선에 닿으면 놓으세요.";
                    BuildFuelGame();
                    break;
                case EngineStatId.Cooling:
                    instructionText.text = "시간이 끝날 때까지 핸들을 시계 방향으로 돌려 파이프를 식히세요.";
                    BuildCoolingGame();
                    break;
                case EngineStatId.MaxOutput:
                    instructionText.text = "커서가 초록색이나 노란색 목표에 들어오면 멈추세요.";
                    BuildOutputGame();
                    break;
                case EngineStatId.IgnitionReliability:
                    instructionText.text = "빛난 순서대로 점화 장치를 누르세요.";
                    BuildIgnitionGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void BuildFuelGame()
        {
            SetActiveGameGroup(fuelGameGroup);
            primaryButton.gameObject.SetActive(false);
            SetupFuelAttempt();
            ClearPointerHandlers(fuelDialImage.gameObject);
            AddPointer(fuelDialImage.gameObject, EventTriggerType.PointerDown, BeginFuelFill);
            AddPointer(fuelDialImage.gameObject, EventTriggerType.PointerUp, RecordFuelAttempt);
        }

        private void BuildCoolingGame()
        {
            SetActiveGameGroup(coolingGameGroup);
            primaryButton.gameObject.SetActive(false);
            coolingTargetDegrees = NextFloat(CoolingMinimumTurns, CoolingMaximumTurns) * 360f;
            coolingDegrees = 0f;
            coolingHeat = CoolingInitialHeat;
            coolingDragging = false;
            coolingHasAngle = false;
            coolingValveImage.rectTransform.localRotation = Quaternion.identity;
            ClearPointerHandlers(coolingValveImage.gameObject);
            AddPointerEvent(coolingValveImage.gameObject, EventTriggerType.PointerDown, data =>
            {
                if (data.button != PointerEventData.InputButton.Left) return;
                coolingDragging = true;
                coolingHasAngle = false;
                DragValve(data);
            });
            AddPointerEvent(coolingValveImage.gameObject, EventTriggerType.Drag, DragValve);
            AddPointerEvent(coolingValveImage.gameObject, EventTriggerType.PointerUp, _ =>
            {
                coolingDragging = false;
                coolingHasAngle = false;
            });
            UpdateCoolingGame();
        }

        private void BuildOutputGame()
        {
            SetActiveGameGroup(outputGameGroup);
            outputStageIndex = 0;
            SetupOutputStage();
            primaryButton.gameObject.SetActive(true);
            primaryButton.interactable = true;
            Border.UI.UISelectableSoundHook.ClearListeners(primaryButton);
            ClearPointerHandlers(primaryButton.gameObject);
            primaryButton.GetComponentInChildren<TMP_Text>().text = "출력 고정";
            AddPointer(primaryButton.gameObject, EventTriggerType.PointerDown, RecordOutputStage);
            ClearPointerHandlers(outputGameGroup.gameObject);
            AddPointer(outputGameGroup.gameObject, EventTriggerType.PointerDown, RecordOutputStage);
        }

        private void BuildIgnitionGame()
        {
            SetActiveGameGroup(ignitionGameGroup);
            primaryButton.gameObject.SetActive(false);

            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                int igniterIndex = i;
                Button button = ignitionButtons[i];
                Border.UI.UISelectableSoundHook.ClearListeners(button);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = (i + 1).ToString();
                }

                button.GetComponent<Image>().color = GetIgniterColor(i, false);
                button.onClick.AddListener(() => PressIgniter(igniterIndex));
            }

            SetupIgnitionRound();
        }

        private void UpdateActiveGame(float deltaSeconds)
        {
            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    UpdateFuelGame(deltaSeconds);
                    break;
                case EngineStatId.Cooling:
                    UpdateCoolingGame();
                    break;
                case EngineStatId.MaxOutput:
                    UpdateOutputGame();
                    break;
                case EngineStatId.IgnitionReliability:
                    UpdateIgnitionGame();
                    break;
            }
        }

        private void UpdateFuelGame(float deltaSeconds)
        {
            if (fuelFilling)
            {
                fuelHoldSeconds += deltaSeconds;
                fuelGaugeValue = Mathf.Clamp01(fuelHoldSeconds / fuelFillDuration);
                SetFuelGaugeValue(fuelGaugeValue);
                if (fuelHoldSeconds >= fuelFillDuration + FuelOverfillSeconds) RecordFuelAttempt();
            }
            UpdateFuelStatusText();
        }

        private void UpdateCoolingGame()
        {
            coolingPipeMaterial.SetFloat("_Heat", coolingHeat);
            coolingValveMaterial.SetFloat("_Heat", 0f);
            coolingProgressText.text = $"과열 {Mathf.FloorToInt(coolingHeat * 100f)}%";
            SetStateText($"회전 {coolingDegrees / 360f:0.0}바퀴", false);
        }

        private void UpdateOutputGame()
        {
            outputGaugeValue = Mathf.PingPong(roundElapsedSeconds / GetOutputStageDuration(outputStageIndex), 1f);
            SetOutputCursor();
            outputLabelText.text = $"{GetOutputStageLabel(outputStageIndex)} 단계";
            SetStateText("초록 Great · 노랑 Perfect", false);
            if (roundElapsedSeconds >= OutputDurationSeconds) RecordMissedOutputStage();
        }

        private void SetOutputCursor()
        {
            RectTransform cursor = outputCursorImage.rectTransform;
            cursor.anchorMin = new Vector2(outputGaugeValue, cursor.anchorMin.y);
            cursor.anchorMax = new Vector2(outputGaugeValue, cursor.anchorMax.y);
            cursor.anchoredPosition = new Vector2(0f, cursor.anchoredPosition.y);
        }

        private void SetupOutputStage()
        {
            roundElapsedSeconds = 0f;
            outputGaugeValue = 0f;
            outputTargetValue = NextFloat(GreatJudgementThreshold, 1f - GreatJudgementThreshold);
            UpdateOutputSafeZone();
            SetOutputCursor();
            outputJudgementText.gameObject.SetActive(false);
            outputLabelText.text = $"{GetOutputStageLabel(outputStageIndex)} 단계";
        }

        private void UpdateIgnitionGame()
        {
            if (!ignitionShowingSequence)
            {
                bool flashing = roundElapsedSeconds < IgnitionClickFlashSeconds;
                for (int i = 0; i < ignitionButtons.Length; i++)
                {
                    ignitionButtons[i].GetComponent<Image>().color = GetIgniterColor(i, flashing && i == ignitionClickedIndex);
                }

                if (ignitionAdvancePending && !flashing)
                {
                    ignitionAdvancePending = false;
                    AdvanceIgnitionRound();
                    return;
                }

                SetStateText($"입력 {roundIndex + 1}/{IgnitionRoundCount}", false);
                return;
            }

            int length = GetIgnitionRoundLength(roundIndex);
            int visibleIndex = Mathf.FloorToInt(roundElapsedSeconds / 0.45f);
            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                ignitionButtons[i].GetComponent<Image>().color = GetIgniterColor(i, visibleIndex < length && ignitionSequence[visibleIndex] == i);
                ignitionButtons[i].interactable = false;
            }

            if (visibleIndex >= length)
            {
                ignitionShowingSequence = false;
                ignitionInputIndex = 0;
                roundElapsedSeconds = 0f;
                for (int i = 0; i < ignitionButtons.Length; i++)
                {
                    ignitionButtons[i].GetComponent<Image>().color = GetIgniterColor(i, false);
                    ignitionButtons[i].interactable = true;
                }
            }

            SetStateText(ignitionShowingSequence ? $"순서 보기 {roundIndex + 1}/{IgnitionRoundCount}" : $"입력 {roundIndex + 1}/{IgnitionRoundCount}", false);
        }

        private void SetupFuelAttempt()
        {
            fuelGaugeValue = 0f;
            fuelHoldSeconds = 0f;
            fuelFilling = false;
            fuelFillDuration = NextFloat(FuelMinimumFillSeconds, FuelMaximumFillSeconds);
            SetFuelGaugeValue(0f);
            fuelJudgementText.gameObject.SetActive(false);
            UpdateFuelStatusText();
        }

        private void BeginFuelFill()
        {
            if (gameCompleted || fuelJudgementShowing || statId != EngineStatId.FuelCapacity
                || fuelAttemptIndex >= FuelAttemptCount || fuelFilling) return;
            fuelFilling = true;
        }

        private void RecordFuelAttempt()
        {
            if (gameCompleted || fuelJudgementShowing || statId != EngineStatId.FuelCapacity
                || fuelAttemptIndex >= FuelAttemptCount || !fuelFilling) return;
            fuelFilling = false;
            int score = CalculateFuelAttemptScore(fuelGaugeValue, Mathf.Max(0f, fuelHoldSeconds - fuelFillDuration));
            fuelScores[fuelAttemptIndex++] = score;
            ShowFuelJudgement(1f - score / 100f);
        }

        private void ShowFuelJudgement(float normalizedError)
        {
            fuelJudgementShowing = true;
            fuelJudgementElapsedSeconds = 0f;
            fuelJudgementText.text = GetFuelJudgementText(normalizedError);
            fuelJudgementText.color = GetJudgementColor(fuelJudgementText.text);
            fuelJudgementText.gameObject.SetActive(true);
            PlayJudgementFeedback(fuelJudgementText);
            SetStateText($"판정 {fuelAttemptIndex}/{FuelAttemptCount}", false);
        }

        private void AdvanceFuelAfterJudgement()
        {
            if (!fuelJudgementShowing) return;
            fuelJudgementShowing = false;
            if (fuelAttemptIndex >= FuelAttemptCount)
            {
                Complete(AverageFuelScore());
                return;
            }
            SetupFuelAttempt();
        }

        private void DragValve(PointerEventData data)
        {
            // Measure against the stationary parent, not the rotating wheel's axes.
            RectTransform valve = coolingValveImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)valve.parent, data.position, data.pressEventCamera, out Vector2 point)) return;
            DragValveLocal(point - (Vector2)valve.localPosition);
        }

        private void DragValveLocal(Vector2 position)
        {
            if (!coolingDragging || gameCompleted || statId != EngineStatId.Cooling) return;
            if (elapsedSeconds >= CoolingDurationSeconds) return;
            Rect rect = coolingValveImage.rectTransform.rect;
            float deadZone = Mathf.Min(rect.width, rect.height) * ValveCenterDeadZone;
            if (position.sqrMagnitude <= deadZone * deadZone)
            {
                coolingHasAngle = false;
                return;
            }
            float angle = Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
            if (coolingHasAngle)
            {
                float previousDegrees = coolingDegrees;
                coolingDegrees = Mathf.Max(0f, coolingDegrees + Mathf.DeltaAngle(angle, coolingPreviousAngle));
                float rotation = coolingDegrees - previousDegrees;
                coolingHeat = Mathf.Clamp01(coolingHeat - rotation / coolingTargetDegrees * CoolingHeatPerTargetRotation);
                coolingValveImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -coolingDegrees);
            }
            coolingPreviousAngle = angle;
            coolingHasAngle = true;
            UpdateCoolingGame();
            if (coolingHeat >= 1f) Complete(0);
        }

        private void RecordOutputStage()
        {
            if (gameCompleted
                || outputJudgementShowing
                || statId != EngineStatId.MaxOutput
                || outputStageIndex >= OutputStageCount)
            {
                return;
            }

            if (roundElapsedSeconds >= OutputDurationSeconds) { RecordMissedOutputStage(); return; }
            // Freeze the position the player saw on pointer-down, before judgement animation starts.
            SetOutputCursor();
            float error = Mathf.Abs(outputGaugeValue - outputTargetValue);
            outputErrors[outputStageIndex] = error;
            outputStageIndex++;
            ShowOutputJudgement(error);
        }

        private void RecordMissedOutputStage()
        {
            if (gameCompleted
                || outputJudgementShowing
                || statId != EngineStatId.MaxOutput
                || outputStageIndex >= OutputStageCount)
            {
                return;
            }

            outputErrors[outputStageIndex] = 1f;
            outputStageIndex++;
            ShowOutputJudgement(1f);
        }

        private void ShowOutputJudgement(float normalizedError)
        {
            outputJudgementShowing = true;
            outputJudgementElapsedSeconds = 0f;
            primaryButton.interactable = false;
            if (outputJudgementText != null)
            {
                outputJudgementText.text = GetOutputJudgementText(normalizedError);
                outputJudgementText.color = GetJudgementColor(outputJudgementText.text);
                outputJudgementText.gameObject.SetActive(true);
                PlayJudgementFeedback(outputJudgementText);
            }

            SetStateText($"판정 {outputStageIndex}/{OutputStageCount}", false);
        }

        private void AdvanceOutputAfterJudgement()
        {
            if (!outputJudgementShowing) return;
            outputJudgementShowing = false;
            if (outputStageIndex >= OutputStageCount)
            {
                primaryButton.interactable = false;
                Complete(CalculateMaxOutputScore(outputErrors));
                return;
            }

            if (outputJudgementText != null)
            {
                outputJudgementText.gameObject.SetActive(false);
            }

            primaryButton.interactable = true;
            SetupOutputStage();
        }

        private void SetupIgnitionRound()
        {
            ignitionClickedIndex = -1;
            ignitionAdvancePending = false;
            roundElapsedSeconds = 0f;
            ignitionShowingSequence = true;
            int length = GetIgnitionRoundLength(roundIndex);
            for (int i = 0; i < length; i++)
            {
                int previousIndex = i == 0 ? -1 : ignitionSequence[i - 1];
                ignitionSequence[i] = NextIndex(ignitionButtons.Length, previousIndex);
            }

            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                ignitionButtons[i].interactable = false;
            }
        }

        private void PressIgniter(int igniterIndex)
        {
            if (gameCompleted || statId != EngineStatId.IgnitionReliability || ignitionShowingSequence || ignitionAdvancePending)
            {
                return;
            }

            int length = GetIgnitionRoundLength(roundIndex);
            ignitionTotalInputs++;
            ignitionReactionTotal += roundElapsedSeconds;
            roundElapsedSeconds = 0f;
            ignitionClickedIndex = igniterIndex;
            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                ignitionButtons[i].GetComponent<Image>().color = GetIgniterColor(i, i == igniterIndex);
            }

            if (ignitionSequence[ignitionInputIndex] == igniterIndex)
            {
                ignitionCorrectInputs++;
                ignitionInputIndex++;
                if (ignitionInputIndex >= length)
                {
                    QueueIgnitionRoundAdvance();
                }

                return;
            }

            QueueIgnitionRoundAdvance();
        }

        private void QueueIgnitionRoundAdvance()
        {
            ignitionAdvancePending = true;
            foreach (Button button in ignitionButtons)
            {
                button.interactable = false;
            }
        }

        private void AdvanceIgnitionRound()
        {
            roundIndex++;
            if (roundIndex >= IgnitionRoundCount)
            {
                float averageReaction = ignitionTotalInputs == 0 ? NoInputReactionSeconds : ignitionReactionTotal / ignitionTotalInputs;
                Complete(CalculateIgnitionReliabilityScore(ignitionCorrectInputs, ignitionTotalInputs, averageReaction));
                return;
            }

            SetupIgnitionRound();
        }

        private int CalculateCurrentScore()
        {
            switch (statId)
            {
                case EngineStatId.FuelCapacity: return AverageFuelScore();
                case EngineStatId.Cooling: return CalculateCoolingScore(coolingHeat);
                case EngineStatId.MaxOutput: return CalculateMaxOutputScore(outputErrors);
                case EngineStatId.IgnitionReliability:
                    float average = ignitionTotalInputs == 0 ? NoInputReactionSeconds : ignitionReactionTotal / ignitionTotalInputs;
                    return CalculateIgnitionReliabilityScore(ignitionCorrectInputs, ignitionTotalInputs, average);
                default: return 0;
            }
        }

        private void Complete(int score)
        {
            if (gameCompleted)
            {
                return;
            }

            gameCompleted = true;
            fuelFilling = false;
            coolingDragging = false;
            pendingResult = new ResearchMiniGameResult(presetId, statId, focused, score);
            ShowResult();
        }

        private void ShowResult()
        {
            resultShowing = true;
            resultElapsedSeconds = 0f;
            timerText.gameObject.SetActive(false);
            timerText.text = string.Empty;
            instructionText.text = "개발 결과를 확인하세요.";
            SetActiveGameGroup(resultGroup);
            resultScoreText.text = $"{ResearchPrototypeModel.GetStatDisplayName(statId)} 개발 {GetEvaluationText(pendingResult.Score)}";
            if (statId == EngineStatId.Cooling && coolingHeat >= 1f)
            {
                resultScoreText.text = "과열 · 게임오버";
            }
            resultDetailText.text = BuildResultDetailText();
            PlayResultFeedback();

            stateText.text = "개발 완료. 곧 연구 화면으로 돌아갑니다.";
            primaryButton.gameObject.SetActive(true);
            primaryButton.interactable = true;
            Border.UI.UISelectableSoundHook.ClearListeners(primaryButton);
            RemovePointerHandlers(primaryButton.gameObject);
            primaryButton.GetComponentInChildren<TMP_Text>().text = "결과 닫기";
            primaryButton.onClick.AddListener(DismissResult);
        }

        private void DismissResult()
        {
            if (!resultShowing || resultDismissed)
            {
                return;
            }

            resultDismissed = true;
            resultShowing = false;
            completedCallback?.Invoke(pendingResult);
        }

        private void SetStateText(string text, bool showExample)
        {
            stateText.text = FormatStateText(text, showExample || elapsedSeconds < 2f);
        }

        private string BuildResultDetailText()
        {
            ResearchPrototypeModel model = ResearchFlowSession.GetOrCreate().Model;
            int gain = model.CalculateResearchStatGain(focused, pendingResult.Score);
            EnginePresetState preset = model.GetEnginePreset(presetId);
            int oldStat = preset.GetStat(statId);
            int oldCompletion = preset.Completion;
            int nextStat = ResearchPrototypeModel.ClampInt(oldStat + gain, 0, 100);
            int completionGain = model.ConfiguredResearchCompletionGain;
            int nextCompletion = Math.Min(ResearchPrototypeModel.MaxEngineCompletion, oldCompletion + completionGain);
            return $"미니게임 점수 {pendingResult.Score}\n"
                + $"스탯 {oldStat}->{nextStat} (+{gain})\n"
                + $"완성도 {oldCompletion}->{nextCompletion} (+{completionGain})";
        }

        private void PlayJudgementFeedback(TMP_Text target)
        {
            if (target == null)
            {
                return;
            }

            feedbackTween?.Kill();
            target.alpha = 1f;
            target.transform.localScale = Vector3.one * 1.18f;
            feedbackTween = DOTween.Sequence()
                .SetTarget(target)
                .SetLink(target.gameObject)
                .Append(target.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack))
                .AppendInterval(0.35f)
                .Append(DOTween.To(() => target.alpha, value => target.alpha = value, 0.2f, 0.55f).SetEase(Ease.OutCubic));
        }

        private void PlayResultFeedback()
        {
            if (resultGroup == null)
            {
                return;
            }

            feedbackTween?.Kill();
            CanvasGroup canvasGroup = resultGroup.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = resultGroup.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            resultGroup.localScale = Vector3.one * 0.96f;
            feedbackTween = DOTween.Sequence()
                .SetTarget(resultGroup)
                .SetLink(resultGroup.gameObject)
                .Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 1f, 0.2f))
                .Join(resultGroup.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack));
        }

        private void UpdateTimerText()
        {
            bool visible = statId == EngineStatId.Cooling || statId == EngineStatId.MaxOutput;
            timerText.gameObject.SetActive(visible);
            float remaining = statId == EngineStatId.Cooling ? CoolingDurationSeconds - elapsedSeconds
                : OutputDurationSeconds - roundElapsedSeconds;
            timerText.text = visible ? $"남은 시간 {Mathf.CeilToInt(Mathf.Max(0f, remaining))}초" : string.Empty;
        }

        private void UpdateOutputSafeZone()
        {
            outputSafeZone.anchorMin = new Vector2(outputTargetValue - GreatJudgementThreshold, outputSafeZone.anchorMin.y);
            outputSafeZone.anchorMax = new Vector2(outputTargetValue + GreatJudgementThreshold, outputSafeZone.anchorMax.y);
            outputSafeZone.offsetMin = new Vector2(0f, outputSafeZone.offsetMin.y);
            outputSafeZone.offsetMax = new Vector2(0f, outputSafeZone.offsetMax.y);
        }

        private static string GetEvaluationText(int score)
        {
            if (score < 50)
            {
                return "완료";
            }

            return score < 80 ? "성공" : "훌륭한 개발";
        }

        private static Color GetJudgementColor(string judgement)
        {
            switch (judgement)
            {
                case "Perfect":
                case "Perfect!":
                    return new Color(0.35f, 0.95f, 1f, 1f);
                case "Great":
                    return new Color(0.5f, 0.9f, 0.45f, 1f);
                case "Good":
                    return new Color(1f, 0.78f, 0.3f, 1f);
                default:
                    return new Color(1f, 0.36f, 0.32f, 1f);
            }
        }

        private static int GetIgnitionRoundLength(int index)
        {
            return Mathf.Clamp(index + 2, 2, 4);
        }

        private static string GetOutputStageLabel(int index)
        {
            switch (index)
            {
                case 0:
                    return "30%";
                case 1:
                    return "60%";
                default:
                    return "100%";
            }
        }

        private static float GetOutputStageDuration(int index)
        {
            return index == 0 ? 0.9f : index == 1 ? 0.75f : 0.6f;
        }

        private static Color GetIgniterColor(int index, bool active)
        {
            switch (index)
            {
                case 0: return active ? new Color(1f, 0.38f, 0.18f) : new Color(0.62f, 0.12f, 0.08f);
                case 1: return active ? new Color(1f, 0.88f, 0.12f) : new Color(0.75f, 0.35f, 0.06f);
                case 2: return active ? new Color(0.25f, 1f, 0.82f) : new Color(0.08f, 0.48f, 0.43f);
                default: return active ? new Color(0.35f, 0.7f, 1f) : new Color(0.12f, 0.32f, 0.58f);
            }
        }

        private void SetFuelGaugeValue(float value)
        {
            float fill = Mathf.Clamp01(value);
            float overfill = Mathf.Clamp01((fuelHoldSeconds - fuelFillDuration) / FuelOverfillSeconds);
            Color color = Color.Lerp(new Color(0.2f, 0.9f, 0.48f), new Color(1f, 0.08f, 0.03f), overfill);
            fuelFillImage.fillAmount = fill <= 0f ? 0f : (90f - GetFuelNeedleAngle(fill)) / 180f;
            fuelFillImage.color = color;
            fuelReadoutImage.color = color;
            fuelReadoutMaterial.SetFloat("_Fill", fill);
            fuelNeedle.localRotation = Quaternion.Euler(0f, 0f, GetFuelNeedleAngle(fill));
        }

        private void UpdateFuelStatusText()
        {
            int current = Mathf.RoundToInt(fuelGaugeValue * 100f);
            fuelStatusText.text = fuelHoldSeconds > fuelFillDuration ? $"과충전 {fuelHoldSeconds - fuelFillDuration:0.0}초"
                : $"눈금 {current}% · 오른쪽 흰색 선에서 놓기";
            if (!fuelJudgementShowing) SetStateText($"시도 {fuelAttemptIndex + 1}/{FuelAttemptCount}", false);
        }

        private int NextIndex(int length, int excludedIndex = -1)
        {
            if (length <= 1)
            {
                return 0;
            }

            if (excludedIndex < 0 || excludedIndex >= length)
            {
                return random.Next(0, length);
            }

            int value = random.Next(0, length - 1);
            return value >= excludedIndex ? value + 1 : value;
        }

        private float NextFloat(float minInclusive, float maxInclusive)
        {
            return minInclusive + (float)random.NextDouble() * (maxInclusive - minInclusive);
        }

        private static void AddPointer(GameObject target, EventTriggerType triggerType, Action callback)
        {
            AddPointerEvent(target, triggerType, data =>
            {
                if (data.button == PointerEventData.InputButton.Left) callback();
            });
        }

        private static void AddPointerEvent(GameObject target, EventTriggerType triggerType, Action<PointerEventData> callback)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = triggerType };
            entry.callback.AddListener(data => { if (data is PointerEventData pointer) callback(pointer); });
            trigger.triggers.Add(entry);
        }

        private static void ClearPointerHandlers(GameObject target)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger != null) trigger.triggers.Clear();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ReleaseHeldInput();
        }

        private void OnDisable()
        {
            ReleaseHeldInput();
        }

        private void ReleaseHeldInput()
        {
            if (initialized && fuelFilling) RecordFuelAttempt();
            coolingDragging = false;
            coolingHasAngle = false;
        }

        private static void RemovePointerHandlers(GameObject target)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
                DestroyUnityObject(trigger);
            }
        }

        private void SetActiveGameGroup(RectTransform activeGroup)
        {
            SetActiveIfPresent(fuelGameGroup, activeGroup);
            SetActiveIfPresent(coolingGameGroup, activeGroup);
            SetActiveIfPresent(outputGameGroup, activeGroup);
            SetActiveIfPresent(ignitionGameGroup, activeGroup);
            SetActiveIfPresent(resultGroup, activeGroup);
        }

        private static void SetActiveIfPresent(RectTransform group, RectTransform activeGroup)
        {
            if (group != null)
            {
                group.gameObject.SetActive(group == activeGroup);
            }
        }

        private static TMP_Text FindRequiredText(Transform root, string name)
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

        private static Image FindRequiredImage(Transform root, string name)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name == name)
                {
                    return image;
                }
            }

            return null;
        }

        private static RectTransform FindRequiredRectTransform(Transform root, string name)
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

        private bool CanCreateRuntimeUiFallback()
        {
            return !Application.isPlaying || gameObject.scene.name != ResearchFlowSession.MainSceneName;
        }

#if UNITY_EDITOR
        private static bool MiniGamePrefabHasRequiredChildren(GameObject prefab)
        {
            return PrefabHasChild(prefab, "FuelDial")
                && PrefabHasChild(prefab, "CoolingValve")
                && PrefabHasChild(prefab, "OutputCursor");
        }

        private static bool PrefabHasChild(GameObject prefab, string childName)
        {
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RebuildDefaultPrefabsForEditor()
        {
            Type builderType = Type.GetType("Border.Research.Editor.ResearchUiPrefabBuilder, Border.Editor");
            System.Reflection.MethodInfo method = builderType?.GetMethod("RebuildMiniGamePrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }

#endif
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
                Debug.LogWarning("InputSystemUIInputModule type was not found. Research mini game UI can render, but pointer input may not work.");
                return;
            }

            if (eventSystem.GetComponent(inputSystemUiModuleType) == null)
            {
                eventSystem.gameObject.AddComponent(inputSystemUiModuleType);
            }
        }

        private void EnsureRenderingCamera()
        {
            if (Camera.allCamerasCount > 0)
            {
                return;
            }

            var cameraObject = new GameObject("Research Mini Game Fallback Camera");
            cameraObject.transform.SetParent(transform, false);
            fallbackRenderingCamera = cameraObject.AddComponent<Camera>();
            fallbackRenderingCamera.clearFlags = CameraClearFlags.SolidColor;
            fallbackRenderingCamera.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
            fallbackRenderingCamera.cullingMask = 0;
            fallbackRenderingCamera.depth = -100f;
            fallbackRenderingCamera.orthographic = true;
            fallbackRenderingCamera.nearClipPlane = 0.1f;
            fallbackRenderingCamera.farClipPlane = 10f;
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
    }
}
