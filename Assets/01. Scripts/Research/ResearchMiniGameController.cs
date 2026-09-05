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
        private const float NoInputReactionSeconds = 9f;
        private const float PerfectJudgementThreshold = 0.02f;
        private const float GreatJudgementThreshold = 0.08f;
        private const float GoodJudgementThreshold = 0.16f;
        private const int FuelAttemptCount = 3;
        private const int CoolingRoundCount = 4;
        private const float CoolingPerfectReactionSeconds = 0.35f;
        private const float CoolingReactionWindowSeconds = 0.9f;
        private const int OutputStageCount = 3;
        private const int IgnitionRoundCount = 3;

        private readonly float[] fuelErrors = new float[FuelAttemptCount];
        private readonly float[] outputErrors = new float[OutputStageCount];
        private readonly int[] ignitionSequence = new int[4];
        private readonly Button[] coolingButtons = new Button[4];
        private readonly Button[] ignitionButtons = new Button[4];

        [SerializeField] private GameObject miniGameScreenPrefab;

        private EnginePresetId presetId;
        private EngineStatId statId;
        private bool focused;
        private Action<ResearchMiniGameResult> completedCallback;
        private ResearchMiniGameResult pendingResult;
        private System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        private bool initialized;
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
        private int activeValveIndex;
        private int fuelAttemptIndex;
        private int outputStageIndex;
        private int ignitionInputIndex;
        private int coolingCorrectCount;
        private int coolingWrongCount;
        private int ignitionCorrectInputs;
        private int ignitionTotalInputs;
        private float coolingReactionTotal;
        private float ignitionReactionTotal;
        private float fuelGaugeValue;
        private float fuelTargetValue;
        private float outputGaugeValue;
        private bool fuelFilling;
        private bool ignitionShowingSequence;
        private Tween feedbackTween;

        private TMP_Text titleText;
        private TMP_Text instructionText;
        private TMP_Text timerText;
        private TMP_Text stateText;
        private TMP_Text fuelStatusText;
        private TMP_Text fuelTargetText;
        private TMP_Text fuelJudgementText;
        private RectTransform fuelGameGroup;
        private RectTransform coolingGameGroup;
        private RectTransform outputGameGroup;
        private RectTransform ignitionGameGroup;
        private RectTransform resultGroup;
        private Image fuelFillImage;
        private Image fuelTargetImage;
        private Image fuelCurrentMarkerImage;
        private Image fuelOuterBandImage;
        private Image fuelPerfectBandImage;
        private Image outputFillImage;
        private TMP_Text outputLabelText;
        private TMP_Text outputJudgementText;
        private RectTransform outputSafeZone;
        private Image coolingHotspotImage;
        private RectTransform playArea;
        private Button primaryButton;
        private TMP_Text resultScoreText;
        private TMP_Text resultDetailText;
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
            if (initialized)
            {
                return;
            }

            presetId = nextPresetId;
            statId = nextStatId;
            focused = nextFocused;
            completedCallback = onCompleted;

            if (!BuildInterface())
            {
                return;
            }

            StartStatGame();
            initialized = true;
        }

        private void OnDestroy()
        {
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
            RecordFuelAttempt();
        }

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
            return fuelTargetValue;
        }

        public int GetActiveValveIndexForTests()
        {
            return activeValveIndex;
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
            return showExample ? $"{baseText}\n예시를 보고 같은 리듬으로 입력하세요." : baseText;
        }

        public static int CalculateFuelCapacityScore(params float[] normalizedErrors)
        {
            if (normalizedErrors == null || normalizedErrors.Length == 0)
            {
                return 0;
            }

            float total = 0f;
            for (int i = 0; i < normalizedErrors.Length; i++)
            {
                total += Mathf.Clamp01(Mathf.Abs(normalizedErrors[i]));
            }

            float average = total / normalizedErrors.Length;
            return ResearchPrototypeModel.ClampInt(Mathf.RoundToInt(100f - average * 140f), 0, 100);
        }

        public static string GetFuelJudgementText(float normalizedError)
        {
            return GetAccuracyJudgementText(normalizedError);
        }

        public static string GetOutputJudgementText(float normalizedError)
        {
            return GetAccuracyJudgementText(normalizedError);
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

        public static int CalculateCoolingScore(int correctCount, int wrongCount, float averageReactionSeconds)
        {
            int correct = ResearchPrototypeModel.ClampInt(correctCount, 0, CoolingRoundCount);
            int accuracyScore = correct * 15;
            float reactionQuality = 1f - Mathf.InverseLerp(CoolingPerfectReactionSeconds, CoolingReactionWindowSeconds, averageReactionSeconds);
            int reactionScore = Mathf.RoundToInt(reactionQuality * 40f * correct / CoolingRoundCount);
            int penalty = Math.Max(0, wrongCount) * 18;
            return ResearchPrototypeModel.ClampInt(accuracyScore + reactionScore - penalty, 0, 100);
        }

        public static int CalculateMaxOutputScore(params float[] normalizedErrors)
        {
            if (normalizedErrors == null || normalizedErrors.Length == 0)
            {
                return 0;
            }

            float total = 0f;
            for (int i = 0; i < normalizedErrors.Length; i++)
            {
                total += Mathf.Clamp01(Mathf.Abs(normalizedErrors[i]));
            }

            float average = total / normalizedErrors.Length;
            return ResearchPrototypeModel.ClampInt(Mathf.RoundToInt(100f - average * 160f), 0, 100);
        }

        public static int CalculateMaxOutputScoreFromFills(params float[] normalizedFillValues)
        {
            if (normalizedFillValues == null || normalizedFillValues.Length == 0)
            {
                return 0;
            }

            int length = Math.Min(normalizedFillValues.Length, OutputStageCount);
            var errors = new float[length];
            for (int i = 0; i < length; i++)
            {
                errors[i] = Mathf.Abs(Mathf.Clamp01(normalizedFillValues[i]) - GetOutputTargetCenter(i));
            }

            return CalculateMaxOutputScore(errors);
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
            if (!initialized)
            {
                return;
            }

            if (resultShowing)
            {
                resultElapsedSeconds += Time.deltaTime;
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

            elapsedSeconds += Time.deltaTime;
            roundElapsedSeconds += Time.deltaTime;
            UpdateTimerText();

            if (fuelJudgementShowing)
            {
                fuelJudgementElapsedSeconds += Time.deltaTime;
                if (fuelJudgementElapsedSeconds >= FuelJudgementSeconds)
                {
                    AdvanceFuelAfterJudgement();
                }

                return;
            }

            if (outputJudgementShowing)
            {
                outputJudgementElapsedSeconds += Time.deltaTime;
                if (outputJudgementElapsedSeconds >= OutputJudgementSeconds)
                {
                    AdvanceOutputAfterJudgement();
                }

                return;
            }

            UpdateActiveGame();

            if (!gameCompleted && statId == EngineStatId.Cooling && elapsedSeconds >= CoolingDurationSeconds)
            {
                Complete(CalculateCurrentScore());
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

            GameObject instance = Instantiate(prefab, transform);
            instance.name = "ResearchMiniGameCanvas";
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
            fuelTargetText = FindRequiredText(canvasTransform, "FuelGaugeLabel");
            fuelJudgementText = FindRequiredText(canvasTransform, "FuelJudgementText");
            fuelFillImage = FindRequiredImage(canvasTransform, "FuelFill");
            fuelTargetImage = FindRequiredImage(canvasTransform, "FuelTarget");
            fuelCurrentMarkerImage = FindRequiredImage(canvasTransform, "FuelCurrentMarker");
            fuelOuterBandImage = FindRequiredImage(canvasTransform, "FuelOuterBand");
            fuelPerfectBandImage = FindRequiredImage(canvasTransform, "FuelPerfectBand");
            coolingHotspotImage = FindRequiredImage(canvasTransform, "CoolingHotspot");
            outputLabelText = FindRequiredText(canvasTransform, "OutputLabel");
            outputJudgementText = FindRequiredText(canvasTransform, "OutputJudgementText");
            outputFillImage = FindRequiredImage(canvasTransform, "OutputFill");
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
                || fuelTargetText == null
                || fuelJudgementText == null
                || fuelFillImage == null
                || fuelTargetImage == null
                || fuelCurrentMarkerImage == null
                || fuelOuterBandImage == null
                || fuelPerfectBandImage == null
                || coolingHotspotImage == null
                || outputLabelText == null
                || outputJudgementText == null
                || outputFillImage == null
                || outputSafeZone == null
                || resultScoreText == null
                || resultDetailText == null)
            {
                DestroyUnityObject(instance);
                Debug.LogError("Research mini game UI prefab is invalid. Check required child names in ResearchMiniGameScreen.");
                return false;
            }

            for (int i = 0; i < coolingButtons.Length; i++)
            {
                coolingButtons[i] = FindRequiredButton(canvasTransform, $"CoolingValve_{i}");
                if (coolingButtons[i] == null)
                {
                    DestroyUnityObject(instance);
                    Debug.LogError($"Research mini game UI prefab is invalid. Missing CoolingValve_{i}.");
                    return false;
                }
            }

            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                ignitionButtons[i] = FindRequiredButton(canvasTransform, $"Igniter_{i}");
                if (ignitionButtons[i] == null)
                {
                    DestroyUnityObject(instance);
                    Debug.LogError($"Research mini game UI prefab is invalid. Missing Igniter_{i}.");
                    return false;
                }
            }

            SetActiveGameGroup(null);
            return true;
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
                    instructionText.text = "목표 용량까지 연료를 채우세요.";
                    BuildFuelGame();
                    break;
                case EngineStatId.Cooling:
                    instructionText.text = "뜨거워진 부분의 냉각 밸브를 여세요.";
                    BuildCoolingGame();
                    break;
                case EngineStatId.MaxOutput:
                    instructionText.text = "안전 영역에서 출력을 올리세요.";
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
            fuelAttemptIndex = 0;
            SetupFuelAttempt();
            SetFuelTargetLabel();
            UpdateFuelStatusText();
            fuelJudgementText.gameObject.SetActive(false);

            primaryButton.gameObject.SetActive(true);
            primaryButton.interactable = true;
            primaryButton.onClick.RemoveAllListeners();
            RemovePointerHandlers(primaryButton.gameObject);
            AddPointer(primaryButton.gameObject, EventTriggerType.PointerDown, BeginFuelFill);
            AddPointer(primaryButton.gameObject, EventTriggerType.PointerUp, RecordFuelAttempt);
            primaryButton.GetComponentInChildren<TMP_Text>().text = "누르고 있다가 목표선에서 놓기";
        }

        private void BuildCoolingGame()
        {
            SetActiveGameGroup(coolingGameGroup);
            primaryButton.gameObject.SetActive(false);

            for (int i = 0; i < coolingButtons.Length; i++)
            {
                int valveIndex = i;
                Button button = coolingButtons[i];
                button.onClick.RemoveAllListeners();
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = GetValveLabel(i);
                }

                button.GetComponent<Image>().color = GetValveColor(i, false);
                button.interactable = true;
                button.onClick.AddListener(() => PressCoolingValve(valveIndex));
            }

            activeValveIndex = -1;
            SetupCoolingRound();
        }

        private void BuildOutputGame()
        {
            SetActiveGameGroup(outputGameGroup);
            UpdateOutputSafeZone();
            SetHorizontalFill(outputFillImage.rectTransform, 0f);
            outputJudgementText.gameObject.SetActive(false);

            outputStageIndex = 0;
            primaryButton.gameObject.SetActive(true);
            primaryButton.interactable = true;
            primaryButton.onClick.RemoveAllListeners();
            RemovePointerHandlers(primaryButton.gameObject);
            primaryButton.GetComponentInChildren<TMP_Text>().text = "안전 영역에서 출력 올리기";
            primaryButton.onClick.AddListener(RecordOutputStage);
        }

        private void BuildIgnitionGame()
        {
            SetActiveGameGroup(ignitionGameGroup);
            primaryButton.gameObject.SetActive(false);

            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                int igniterIndex = i;
                Button button = ignitionButtons[i];
                button.onClick.RemoveAllListeners();
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

        private void UpdateActiveGame()
        {
            switch (statId)
            {
                case EngineStatId.FuelCapacity:
                    UpdateFuelGame();
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

        private void UpdateFuelGame()
        {
            if (fuelFilling)
            {
                fuelGaugeValue = Mathf.Clamp01(fuelGaugeValue + Time.deltaTime * 0.55f);
                SetFuelGaugeValue(fuelGaugeValue);
            }

            UpdateFuelStatusText();
            SetStateText($"시도 {fuelAttemptIndex + 1}/{FuelAttemptCount}", false);
        }

        private void UpdateCoolingGame()
        {
            SetStateText($"밸브 {roundIndex + 1}/{CoolingRoundCount}", false);
        }

        private void UpdateOutputGame()
        {
            float stageDuration = GetOutputStageDuration(outputStageIndex);
            outputGaugeValue = Mathf.Clamp01(roundElapsedSeconds / stageDuration);
            SetHorizontalFill(outputFillImage.rectTransform, outputGaugeValue);
            outputLabelText.text = $"{GetOutputStageLabel(outputStageIndex)} 단계  안전 영역 안에서 한 번 클릭";

            SetStateText(roundElapsedSeconds >= stageDuration
                ? $"출력 단계 {GetOutputStageLabel(outputStageIndex)}  게이지가 끝에 닿았습니다. 클릭해서 기록하세요."
                : $"출력 단계 {GetOutputStageLabel(outputStageIndex)}  게이지가 안전 영역에 들어오면 클릭",
                false);
        }

        private void UpdateIgnitionGame()
        {
            if (!ignitionShowingSequence)
            {
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
            fuelFilling = false;
            fuelTargetValue = NextFloat(0.38f, 0.84f);
            if (fuelTargetImage != null)
            {
                SetVerticalMarker(fuelTargetImage.rectTransform, fuelTargetValue, 7f);
            }

            SetFuelTargetLabel();
            SetFuelJudgementBands();

            if (fuelFillImage != null)
            {
                SetFuelGaugeValue(0f);
            }

            if (fuelJudgementText != null)
            {
                fuelJudgementText.gameObject.SetActive(false);
            }

            UpdateFuelStatusText();
        }

        private void BeginFuelFill()
        {
            if (gameCompleted
                || fuelJudgementShowing
                || statId != EngineStatId.FuelCapacity
                || fuelAttemptIndex >= FuelAttemptCount)
            {
                return;
            }

            fuelFilling = true;
        }

        private void RecordFuelAttempt()
        {
            if (gameCompleted
                || fuelJudgementShowing
                || statId != EngineStatId.FuelCapacity
                || fuelAttemptIndex >= FuelAttemptCount)
            {
                fuelFilling = false;
                return;
            }

            fuelFilling = false;
            float error = Mathf.Abs(fuelGaugeValue - fuelTargetValue);
            fuelErrors[fuelAttemptIndex] = error;
            fuelAttemptIndex++;
            ShowFuelJudgement(error);
        }

        private void ShowFuelJudgement(float normalizedError)
        {
            fuelJudgementShowing = true;
            fuelJudgementElapsedSeconds = 0f;
            primaryButton.interactable = false;
            if (fuelJudgementText != null)
            {
                fuelJudgementText.text = GetFuelJudgementText(normalizedError);
                fuelJudgementText.color = GetJudgementColor(fuelJudgementText.text);
                fuelJudgementText.gameObject.SetActive(true);
                PlayJudgementFeedback(fuelJudgementText);
            }

            SetStateText($"판정 {fuelAttemptIndex}/{FuelAttemptCount}", false);
        }

        private void AdvanceFuelAfterJudgement()
        {
            fuelJudgementShowing = false;
            if (fuelAttemptIndex >= FuelAttemptCount)
            {
                primaryButton.interactable = false;
                Complete(CalculateFuelCapacityScore(fuelErrors));
                return;
            }

            primaryButton.interactable = true;
            SetupFuelAttempt();
        }

        private void SetupCoolingRound()
        {
            roundElapsedSeconds = 0f;
            activeValveIndex = NextIndex(coolingButtons.Length, activeValveIndex);
            coolingHotspotImage.color = GetValveColor(activeValveIndex, true);
            for (int i = 0; i < coolingButtons.Length; i++)
            {
                coolingButtons[i].interactable = true;
                coolingButtons[i].GetComponent<Image>().color = GetValveColor(i, i == activeValveIndex);
            }

            SetStateText($"밸브 {roundIndex + 1}/{CoolingRoundCount}", false);
        }

        private void PressCoolingValve(int valveIndex)
        {
            if (gameCompleted || statId != EngineStatId.Cooling)
            {
                return;
            }

            if (valveIndex == activeValveIndex)
            {
                coolingCorrectCount++;
                coolingReactionTotal += roundElapsedSeconds;
                roundIndex++;
                if (roundIndex >= CoolingRoundCount)
                {
                    float averageReaction = coolingCorrectCount == 0 ? CoolingDurationSeconds : coolingReactionTotal / coolingCorrectCount;
                    Complete(CalculateCoolingScore(coolingCorrectCount, coolingWrongCount, averageReaction));
                    return;
                }

                SetupCoolingRound();
            }
            else
            {
                coolingWrongCount++;
                SetStateText("잘못된 밸브입니다.", false);
            }
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

            float error = Mathf.Abs(outputGaugeValue - GetOutputTargetCenter(outputStageIndex));
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
            roundElapsedSeconds = 0f;
            outputGaugeValue = 0f;
            SetHorizontalFill(outputFillImage.rectTransform, 0f);
            UpdateOutputSafeZone();
        }

        private void SetupIgnitionRound()
        {
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
            if (gameCompleted || statId != EngineStatId.IgnitionReliability || ignitionShowingSequence)
            {
                return;
            }

            int length = GetIgnitionRoundLength(roundIndex);
            ignitionTotalInputs++;
            ignitionReactionTotal += roundElapsedSeconds;
            roundElapsedSeconds = 0f;

            if (ignitionSequence[ignitionInputIndex] == igniterIndex)
            {
                ignitionCorrectInputs++;
                ignitionInputIndex++;
                if (ignitionInputIndex >= length)
                {
                    AdvanceIgnitionRound();
                }

                return;
            }

            AdvanceIgnitionRound();
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
                case EngineStatId.FuelCapacity:
                    if (fuelAttemptIndex == 0)
                    {
                        return 0;
                    }

                    var attemptedFuelErrors = new float[fuelAttemptIndex];
                    Array.Copy(fuelErrors, attemptedFuelErrors, fuelAttemptIndex);
                    return CalculateFuelCapacityScore(attemptedFuelErrors);
                case EngineStatId.Cooling:
                    float coolingAverage = coolingCorrectCount == 0 ? CoolingDurationSeconds : coolingReactionTotal / coolingCorrectCount;
                    return CalculateCoolingScore(coolingCorrectCount, coolingWrongCount, coolingAverage);
                case EngineStatId.MaxOutput:
                    if (outputStageIndex == 0)
                    {
                        return 0;
                    }

                    var attemptedOutputErrors = new float[outputStageIndex];
                    Array.Copy(outputErrors, attemptedOutputErrors, outputStageIndex);
                    return CalculateMaxOutputScore(attemptedOutputErrors);
                case EngineStatId.IgnitionReliability:
                    float ignitionAverage = ignitionTotalInputs == 0 ? NoInputReactionSeconds : ignitionReactionTotal / ignitionTotalInputs;
                    return CalculateIgnitionReliabilityScore(ignitionCorrectInputs, ignitionTotalInputs, ignitionAverage);
                default:
                    return 0;
            }
        }

        private void Complete(int score)
        {
            if (gameCompleted)
            {
                return;
            }

            gameCompleted = true;
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
            resultDetailText.text = BuildResultDetailText();
            PlayResultFeedback();

            stateText.text = "개발 완료. 곧 연구 화면으로 돌아갑니다.";
            primaryButton.gameObject.SetActive(true);
            primaryButton.onClick.RemoveAllListeners();
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
            if (statId != EngineStatId.Cooling)
            {
                timerText.gameObject.SetActive(false);
                timerText.text = string.Empty;
                return;
            }

            timerText.gameObject.SetActive(true);
            int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, CoolingDurationSeconds - elapsedSeconds));
            timerText.text = $"남은 시간 {secondsLeft}초";
        }

        private void UpdateOutputSafeZone()
        {
            float center = GetOutputTargetCenter(outputStageIndex);
            outputSafeZone.anchorMin = new Vector2(Mathf.Clamp01(center - 0.08f), 0f);
            outputSafeZone.anchorMax = new Vector2(Mathf.Clamp01(center + 0.08f), 1f);
            outputSafeZone.offsetMin = Vector2.zero;
            outputSafeZone.offsetMax = Vector2.zero;
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

        private static float GetOutputTargetCenter(int index)
        {
            switch (index)
            {
                case 0:
                    return 0.35f;
                case 1:
                    return 0.6f;
                default:
                    return 0.85f;
            }
        }

        private static float GetOutputStageDuration(int index)
        {
            switch (index)
            {
                case 0:
                    return 2.1f;
                case 1:
                    return 1.85f;
                default:
                    return 1.6f;
            }
        }

        private static string GetValveLabel(int index)
        {
            switch (index)
            {
                case 0:
                    return "청색 밸브";
                case 1:
                    return "녹색 밸브";
                case 2:
                    return "황색 밸브";
                default:
                    return "적색 밸브";
            }
        }

        private static Color GetValveColor(int index, bool active)
        {
            Color color;
            switch (index)
            {
                case 0:
                    color = new Color(0.22f, 0.46f, 0.78f, 1f);
                    break;
                case 1:
                    color = new Color(0.24f, 0.64f, 0.34f, 1f);
                    break;
                case 2:
                    color = new Color(0.82f, 0.62f, 0.2f, 1f);
                    break;
                default:
                    color = new Color(0.78f, 0.26f, 0.24f, 1f);
                    break;
            }

            return active ? Color.Lerp(color, Color.white, 0.35f) : color;
        }

        private static Color GetIgniterColor(int index, bool active)
        {
            Color color = index % 2 == 0 ? new Color(0.32f, 0.42f, 0.58f, 1f) : new Color(0.45f, 0.34f, 0.54f, 1f);
            return active ? new Color(1f, 0.82f, 0.24f, 1f) : color;
        }

        private void SetFuelGaugeValue(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            if (fuelFillImage != null)
            {
                SetHorizontalFill(fuelFillImage.rectTransform, clampedValue);
            }

            if (fuelCurrentMarkerImage != null)
            {
                SetVerticalMarker(fuelCurrentMarkerImage.rectTransform, clampedValue, 4f);
            }
        }

        private void UpdateFuelStatusText()
        {
            if (fuelStatusText == null)
            {
                return;
            }

            int current = Mathf.RoundToInt(fuelGaugeValue * 100f);
            int target = Mathf.RoundToInt(fuelTargetValue * 100f);
            int remaining = Mathf.Max(0, FuelAttemptCount - fuelAttemptIndex);
            fuelStatusText.text = $"현재 {current}% / 목표 {target}% / 남은 시도 {remaining}";
        }

        private void SetFuelTargetLabel()
        {
            if (fuelTargetText == null)
            {
                return;
            }

            float left = Mathf.Clamp01(fuelTargetValue - 0.1f);
            float right = Mathf.Clamp01(fuelTargetValue + 0.1f);
            fuelTargetText.rectTransform.anchorMin = new Vector2(left, 0.56f);
            fuelTargetText.rectTransform.anchorMax = new Vector2(right, 0.98f);
            fuelTargetText.rectTransform.offsetMin = Vector2.zero;
            fuelTargetText.rectTransform.offsetMax = Vector2.zero;
        }

        private void SetFuelJudgementBands()
        {
            if (fuelOuterBandImage != null)
            {
                SetHorizontalBand(fuelOuterBandImage.rectTransform, fuelTargetValue, GoodJudgementThreshold);
                fuelOuterBandImage.rectTransform.SetAsLastSibling();
            }

            if (fuelPerfectBandImage != null)
            {
                SetHorizontalBand(fuelPerfectBandImage.rectTransform, fuelTargetValue, PerfectJudgementThreshold);
                fuelPerfectBandImage.rectTransform.SetAsLastSibling();
            }

            if (fuelTargetImage != null)
            {
                fuelTargetImage.rectTransform.SetAsLastSibling();
            }

            if (fuelCurrentMarkerImage != null)
            {
                fuelCurrentMarkerImage.rectTransform.SetAsLastSibling();
            }
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

        private static void SetHorizontalFill(RectTransform target, float fill)
        {
            target.anchorMin = new Vector2(0f, 0f);
            target.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static void SetHorizontalBand(RectTransform target, float center, float halfWidth)
        {
            target.anchorMin = new Vector2(Mathf.Clamp01(center - halfWidth), 0f);
            target.anchorMax = new Vector2(Mathf.Clamp01(center + halfWidth), 1f);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private static void SetVerticalMarker(RectTransform target, float normalizedPosition, float width)
        {
            float position = Mathf.Clamp01(normalizedPosition);
            target.anchorMin = new Vector2(position, 0f);
            target.anchorMax = new Vector2(position, 1f);
            target.sizeDelta = new Vector2(width, 0f);
            target.anchoredPosition = Vector2.zero;
        }

        private static void AddPointer(GameObject target, EventTriggerType triggerType, Action callback)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = triggerType };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }

        private static void RemovePointerHandlers(GameObject target)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger != null)
            {
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

#if UNITY_EDITOR
        private static bool MiniGamePrefabHasRequiredChildren(GameObject prefab)
        {
            return PrefabHasChild(prefab, "FuelOuterBand")
                && PrefabHasChild(prefab, "FuelPerfectBand")
                && PrefabHasChild(prefab, "OutputJudgementText");
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
            System.Reflection.MethodInfo method = builderType?.GetMethod("RebuildUiPrefabs", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
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
