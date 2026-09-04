using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.Research
{
    public readonly struct ResearchMiniGameResult
    {
        public ResearchMiniGameResult(EnginePresetId presetId, EngineStatId statId, bool focused, int score, bool completedByTimeout)
        {
            PresetId = presetId;
            StatId = statId;
            Focused = focused;
            Score = ResearchPrototypeModel.ClampInt(score, 0, 100);
            CompletedByTimeout = completedByTimeout;
        }

        public EnginePresetId PresetId { get; }
        public EngineStatId StatId { get; }
        public bool Focused { get; }
        public int Score { get; }
        public bool CompletedByTimeout { get; }
    }

    public sealed class ResearchMiniGameController : MonoBehaviour
    {
        private const float DefaultDurationSeconds = 9f;
        private const float ResultDismissSeconds = 2f;
        private const float CoolingExampleSeconds = 2f;
        private const int FuelAttemptCount = 3;
        private const int CoolingRoundCount = 4;
        private const int OutputStageCount = 3;
        private const int IgnitionRoundCount = 3;

        private readonly float[] fuelErrors = new float[FuelAttemptCount];
        private readonly float[] outputErrors = new float[OutputStageCount];
        private readonly int[] ignitionSequence = new int[4];
        private readonly Button[] coolingButtons = new Button[4];
        private readonly Button[] ignitionButtons = new Button[4];

        private EnginePresetId presetId;
        private EngineStatId statId;
        private bool focused;
        private Action<ResearchMiniGameResult> completedCallback;
        private ResearchMiniGameResult pendingResult;
        private bool initialized;
        private bool gameCompleted;
        private bool resultShowing;
        private bool resultDismissed;
        private float durationSeconds = DefaultDurationSeconds;
        private float elapsedSeconds;
        private float roundElapsedSeconds;
        private float resultElapsedSeconds;
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
        private bool coolingExampleActive;
        private bool ignitionShowingSequence;

        private TMP_Text titleText;
        private TMP_Text instructionText;
        private TMP_Text timerText;
        private TMP_Text stateText;
        private Image fuelFillImage;
        private Image fuelTargetImage;
        private Image outputFillImage;
        private RectTransform outputSafeZone;
        private Image coolingHotspotImage;
        private RectTransform playArea;
        private Button primaryButton;

        public EngineStatId StatId => statId;
        public bool IsCompleted => gameCompleted;
        public bool IsShowingResult => resultShowing;

        public void InitializeForTests(EnginePresetId nextPresetId, EngineStatId nextStatId, bool nextFocused, Action<ResearchMiniGameResult> onCompleted)
        {
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

            BuildInterface();
            StartStatGame();
            initialized = true;
        }

        public void ForceCompleteForTests(int score)
        {
            Complete(score, false);
        }

        public void ForceDismissForTests()
        {
            DismissResult();
        }

        public string GetStateTextForTests()
        {
            return stateText == null ? string.Empty : stateText.text;
        }

        public static string FormatStateText(string baseText, bool showExample)
        {
            return showExample ? $"{baseText}\n예시 표시 중" : baseText;
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

        public static int CalculateCoolingScore(int correctCount, int wrongCount, float averageReactionSeconds)
        {
            int accuracyScore = ResearchPrototypeModel.ClampInt(correctCount, 0, CoolingRoundCount) * 22;
            int reactionScore = Mathf.RoundToInt(Mathf.Clamp01(1.25f - Mathf.Max(0f, averageReactionSeconds)) * 12f);
            int penalty = Math.Max(0, wrongCount) * 12;
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
            timerText.text = $"남은 시간 {Mathf.CeilToInt(Mathf.Max(0f, durationSeconds - elapsedSeconds))}초";

            UpdateActiveGame();

            if (elapsedSeconds >= durationSeconds)
            {
                Complete(CalculateCurrentScore(), true);
            }
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            RectTransform canvasTransform = CreateGroup("ResearchMiniGameCanvas", transform);
            Canvas canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasTransform.gameObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreatePanel("Background", canvasTransform, new Color(0.04f, 0.05f, 0.07f, 0.96f));
            Stretch(background, 0f);

            RectTransform panel = CreatePanel("MiniGamePanel", canvasTransform, new Color(0.13f, 0.16f, 0.2f, 0.98f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(980f, 590f);
            AddVerticalLayout(panel, 18f, 18f, 16f, 12f);

            RectTransform topRow = CreateGroup("TopRow", panel);
            AddHorizontalLayout(topRow, 0f, 0f, 0f, 10f);
            topRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            titleText = CreateText("Title", topRow, 24, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            timerText = CreateText("Timer", topRow, 18, FontStyles.Bold, TextAlignmentOptions.Right, string.Empty);
            timerText.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

            instructionText = CreateText("Instruction", panel, 16, FontStyles.Bold, TextAlignmentOptions.Left, string.Empty);
            instructionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            playArea = CreatePanel("PlayArea", panel, new Color(0.07f, 0.09f, 0.12f, 1f));
            playArea.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            stateText = CreateText("State", panel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            stateText.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

            primaryButton = CreateButton("PrimaryActionButton", panel, string.Empty, 0f, 54f);
        }

        private void StartStatGame()
        {
            titleText.text = $"{ResearchPrototypeModel.GetStatDisplayName(statId)} {(focused ? "집중" : "일반")} 개발";
            elapsedSeconds = 0f;
            roundElapsedSeconds = 0f;
            roundIndex = 0;

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
            fuelAttemptIndex = 0;
            SetupFuelAttempt();

            RectTransform gaugeFrame = CreatePanel("FuelGaugeFrame", playArea, new Color(0.14f, 0.18f, 0.23f, 1f));
            gaugeFrame.anchorMin = new Vector2(0.08f, 0.36f);
            gaugeFrame.anchorMax = new Vector2(0.92f, 0.62f);
            gaugeFrame.offsetMin = Vector2.zero;
            gaugeFrame.offsetMax = Vector2.zero;

            fuelFillImage = CreatePanel("FuelFill", gaugeFrame, new Color(0.26f, 0.74f, 0.88f, 1f)).GetComponent<Image>();
            SetHorizontalFill(fuelFillImage.rectTransform, 0f);

            fuelTargetImage = CreatePanel("FuelTarget", gaugeFrame, new Color(1f, 0.88f, 0.24f, 1f)).GetComponent<Image>();
            fuelTargetImage.rectTransform.anchorMin = new Vector2(fuelTargetValue, 0f);
            fuelTargetImage.rectTransform.anchorMax = new Vector2(fuelTargetValue, 1f);
            fuelTargetImage.rectTransform.sizeDelta = new Vector2(6f, 0f);
            fuelTargetImage.rectTransform.anchoredPosition = Vector2.zero;

            TMP_Text markerText = CreateText("FuelGaugeLabels", gaugeFrame, 15, FontStyles.Bold, TextAlignmentOptions.Center, "현재 주입량                      목표선");
            Stretch(markerText.rectTransform, 6f);

            AddPointer(primaryButton.gameObject, EventTriggerType.PointerDown, () => fuelFilling = true);
            AddPointer(primaryButton.gameObject, EventTriggerType.PointerUp, RecordFuelAttempt);
            primaryButton.GetComponentInChildren<TMP_Text>().text = "누르고 있다가 목표선에서 놓기";
        }

        private void BuildCoolingGame()
        {
            primaryButton.gameObject.SetActive(false);
            activeValveIndex = 1;
            coolingExampleActive = true;

            coolingHotspotImage = CreatePanel("CoolingHotspot", playArea, GetValveColor(activeValveIndex, true)).GetComponent<Image>();
            coolingHotspotImage.rectTransform.anchorMin = new Vector2(0.34f, 0.62f);
            coolingHotspotImage.rectTransform.anchorMax = new Vector2(0.66f, 0.84f);
            coolingHotspotImage.rectTransform.offsetMin = Vector2.zero;
            coolingHotspotImage.rectTransform.offsetMax = Vector2.zero;

            TMP_Text hotspotLabel = CreateText("CoolingHotspotLabel", coolingHotspotImage.transform, 16, FontStyles.Bold, TextAlignmentOptions.Center, "뜨거운 엔진 위치");
            Stretch(hotspotLabel.rectTransform, 6f);

            RectTransform valveGrid = CreateGroup("CoolingValveGrid", playArea);
            valveGrid.anchorMin = new Vector2(0.16f, 0.08f);
            valveGrid.anchorMax = new Vector2(0.84f, 0.55f);
            valveGrid.offsetMin = Vector2.zero;
            valveGrid.offsetMax = Vector2.zero;
            AddGrid(valveGrid, 2, 2, 14f, 190f, 74f);

            for (int i = 0; i < coolingButtons.Length; i++)
            {
                int valveIndex = i;
                Button button = CreateButton($"CoolingValve_{i}", valveGrid, GetValveLabel(i), 0f, 0f);
                button.GetComponent<Image>().color = GetValveColor(i, i == activeValveIndex);
                button.interactable = false;
                button.onClick.AddListener(() => PressCoolingValve(valveIndex));
                coolingButtons[i] = button;
            }
        }

        private void BuildOutputGame()
        {
            RectTransform gaugeFrame = CreatePanel("OutputGaugeFrame", playArea, new Color(0.14f, 0.18f, 0.23f, 1f));
            gaugeFrame.anchorMin = new Vector2(0.08f, 0.36f);
            gaugeFrame.anchorMax = new Vector2(0.92f, 0.6f);
            gaugeFrame.offsetMin = Vector2.zero;
            gaugeFrame.offsetMax = Vector2.zero;

            outputFillImage = CreatePanel("OutputFill", gaugeFrame, new Color(0.88f, 0.5f, 0.2f, 1f)).GetComponent<Image>();
            SetHorizontalFill(outputFillImage.rectTransform, 0f);

            outputSafeZone = CreatePanel("SafeZone", gaugeFrame, new Color(0.22f, 0.72f, 0.38f, 0.82f));
            UpdateOutputSafeZone();

            outputStageIndex = 0;
            primaryButton.GetComponentInChildren<TMP_Text>().text = "안전 영역에서 출력 올리기";
            primaryButton.onClick.AddListener(RecordOutputStage);
        }

        private void BuildIgnitionGame()
        {
            primaryButton.gameObject.SetActive(false);

            RectTransform igniterGrid = CreateGroup("IgniterGrid", playArea);
            igniterGrid.anchorMin = new Vector2(0.19f, 0.18f);
            igniterGrid.anchorMax = new Vector2(0.81f, 0.78f);
            igniterGrid.offsetMin = Vector2.zero;
            igniterGrid.offsetMax = Vector2.zero;
            AddGrid(igniterGrid, 2, 2, 16f, 150f, 100f);

            for (int i = 0; i < ignitionButtons.Length; i++)
            {
                int igniterIndex = i;
                Button button = CreateButton($"Igniter_{i}", igniterGrid, (i + 1).ToString(), 0f, 0f);
                button.GetComponent<Image>().color = GetIgniterColor(i, false);
                button.onClick.AddListener(() => PressIgniter(igniterIndex));
                ignitionButtons[i] = button;
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
                SetHorizontalFill(fuelFillImage.rectTransform, fuelGaugeValue);
            }

            SetStateText($"시도 {fuelAttemptIndex + 1}/{FuelAttemptCount}  목표 {Mathf.RoundToInt(fuelTargetValue * 100f)}  현재 {Mathf.RoundToInt(fuelGaugeValue * 100f)}", false);
        }

        private void UpdateCoolingGame()
        {
            if (coolingExampleActive)
            {
                float pulse = Mathf.PingPong(elapsedSeconds * 3f, 1f);
                coolingHotspotImage.color = Color.Lerp(GetValveColor(activeValveIndex, false), GetValveColor(activeValveIndex, true), pulse);
                coolingButtons[activeValveIndex].GetComponent<Image>().color = coolingHotspotImage.color;
                SetStateText("예시: 뜨거운 위치와 같은 색 밸브가 함께 빛납니다.", false);

                if (elapsedSeconds >= CoolingExampleSeconds)
                {
                    coolingExampleActive = false;
                    SetupCoolingRound();
                }

                return;
            }

            SetStateText($"밸브 {roundIndex + 1}/{CoolingRoundCount}", false);
        }

        private void UpdateOutputGame()
        {
            float stageDuration = GetOutputStageDuration(outputStageIndex);
            outputGaugeValue = Mathf.Clamp01(roundElapsedSeconds / stageDuration);
            SetHorizontalFill(outputFillImage.rectTransform, outputGaugeValue);

            if (roundElapsedSeconds >= stageDuration)
            {
                RecordMissedOutputStage();
                return;
            }

            SetStateText($"출력 단계 {GetOutputStageLabel(outputStageIndex)}  게이지가 안전 영역에 들어오면 클릭", false);
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
            fuelTargetValue = 0.45f + fuelAttemptIndex * 0.17f;
            if (fuelTargetImage != null)
            {
                fuelTargetImage.rectTransform.anchorMin = new Vector2(fuelTargetValue, 0f);
                fuelTargetImage.rectTransform.anchorMax = new Vector2(fuelTargetValue, 1f);
            }

            if (fuelFillImage != null)
            {
                SetHorizontalFill(fuelFillImage.rectTransform, 0f);
            }
        }

        private void RecordFuelAttempt()
        {
            if (gameCompleted || statId != EngineStatId.FuelCapacity)
            {
                return;
            }

            fuelFilling = false;
            fuelErrors[fuelAttemptIndex] = Mathf.Abs(fuelGaugeValue - fuelTargetValue);
            fuelAttemptIndex++;
            if (fuelAttemptIndex >= FuelAttemptCount)
            {
                Complete(CalculateFuelCapacityScore(fuelErrors), false);
                return;
            }

            SetupFuelAttempt();
        }

        private void SetupCoolingRound()
        {
            roundElapsedSeconds = 0f;
            activeValveIndex = (roundIndex * 3 + 1) % coolingButtons.Length;
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
            if (gameCompleted || coolingExampleActive || statId != EngineStatId.Cooling)
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
                    float averageReaction = coolingCorrectCount == 0 ? durationSeconds : coolingReactionTotal / coolingCorrectCount;
                    Complete(CalculateCoolingScore(coolingCorrectCount, coolingWrongCount, averageReaction), false);
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
            if (gameCompleted || statId != EngineStatId.MaxOutput || outputStageIndex >= OutputStageCount)
            {
                return;
            }

            outputErrors[outputStageIndex] = Mathf.Abs(outputGaugeValue - GetOutputTargetCenter(outputStageIndex));
            AdvanceOutputStage();
        }

        private void RecordMissedOutputStage()
        {
            if (gameCompleted || statId != EngineStatId.MaxOutput || outputStageIndex >= OutputStageCount)
            {
                return;
            }

            outputErrors[outputStageIndex] = 1f;
            AdvanceOutputStage();
        }

        private void AdvanceOutputStage()
        {
            outputStageIndex++;
            if (outputStageIndex >= OutputStageCount)
            {
                Complete(CalculateMaxOutputScore(outputErrors), false);
                return;
            }

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
                ignitionSequence[i] = (roundIndex + i * 2) % ignitionButtons.Length;
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
                float averageReaction = ignitionTotalInputs == 0 ? durationSeconds : ignitionReactionTotal / ignitionTotalInputs;
                Complete(CalculateIgnitionReliabilityScore(ignitionCorrectInputs, ignitionTotalInputs, averageReaction), false);
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
                    float coolingAverage = coolingCorrectCount == 0 ? durationSeconds : coolingReactionTotal / coolingCorrectCount;
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
                    float ignitionAverage = ignitionTotalInputs == 0 ? durationSeconds : ignitionReactionTotal / ignitionTotalInputs;
                    return CalculateIgnitionReliabilityScore(ignitionCorrectInputs, ignitionTotalInputs, ignitionAverage);
                default:
                    return 0;
            }
        }

        private void Complete(int score, bool completedByTimeout)
        {
            if (gameCompleted)
            {
                return;
            }

            gameCompleted = true;
            pendingResult = new ResearchMiniGameResult(presetId, statId, focused, score, completedByTimeout);
            ShowResult();
        }

        private void ShowResult()
        {
            resultShowing = true;
            resultElapsedSeconds = 0f;
            timerText.text = "결과 확인";
            instructionText.text = "개발 결과를 확인하세요.";
            ClearPlayArea();

            TMP_Text scoreText = CreateText("ResultScoreText", playArea, 30, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            scoreText.rectTransform.anchorMin = new Vector2(0.08f, 0.58f);
            scoreText.rectTransform.anchorMax = new Vector2(0.92f, 0.82f);
            scoreText.rectTransform.offsetMin = Vector2.zero;
            scoreText.rectTransform.offsetMax = Vector2.zero;
            scoreText.text = $"{ResearchPrototypeModel.GetStatDisplayName(statId)} 개발 {GetEvaluationText(pendingResult.Score)}";

            TMP_Text resultText = CreateText("ResultDetailText", playArea, 21, FontStyles.Bold, TextAlignmentOptions.Center, string.Empty);
            resultText.rectTransform.anchorMin = new Vector2(0.1f, 0.28f);
            resultText.rectTransform.anchorMax = new Vector2(0.9f, 0.56f);
            resultText.rectTransform.offsetMin = Vector2.zero;
            resultText.rectTransform.offsetMax = Vector2.zero;
            resultText.text = $"미니게임 점수 {pendingResult.Score}\n스탯 +{CalculateResearchStatGain(focused, pendingResult.Score)} / 레벨 +{(focused ? ResearchPrototypeModel.FocusedResearchLevelGain : ResearchPrototypeModel.NormalResearchLevelGain)}";

            stateText.text = pendingResult.CompletedByTimeout ? "시간 종료. 현재 점수로 개발을 완료합니다." : "개발 완료. 곧 연구 화면으로 돌아갑니다.";
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
            stateText.text = FormatStateText(text, showExample);
        }

        private void UpdateOutputSafeZone()
        {
            float center = GetOutputTargetCenter(outputStageIndex);
            outputSafeZone.anchorMin = new Vector2(Mathf.Clamp01(center - 0.08f), 0f);
            outputSafeZone.anchorMax = new Vector2(Mathf.Clamp01(center + 0.08f), 1f);
            outputSafeZone.offsetMin = Vector2.zero;
            outputSafeZone.offsetMax = Vector2.zero;
        }

        private void ClearPlayArea()
        {
            for (int i = playArea.childCount - 1; i >= 0; i--)
            {
                DestroyUnityObject(playArea.GetChild(i).gameObject);
            }
        }

        private static int CalculateResearchStatGain(bool focused, int score)
        {
            int clampedScore = ResearchPrototypeModel.ClampInt(score, 0, 100);
            if (clampedScore < 50)
            {
                return focused ? 16 : 10;
            }

            if (clampedScore < 80)
            {
                return focused ? 21 : 13;
            }

            return focused ? 26 : 16;
        }

        private static string GetEvaluationText(int score)
        {
            if (score < 50)
            {
                return "완료";
            }

            return score < 80 ? "성공" : "훌륭한 개발";
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

        private static void SetHorizontalFill(RectTransform target, float fill)
        {
            target.anchorMin = new Vector2(0f, 0f);
            target.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
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

        private static void AddGrid(RectTransform target, int rows, int columns, float spacing, float width, float height)
        {
            GridLayoutGroup grid = target.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(width, height);
            grid.spacing = new Vector2(spacing, spacing);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(24, 24, 28, 28);
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

            if (preferredHeight > 0f)
            {
                layout.preferredHeight = preferredHeight;
            }
            else
            {
                layout.flexibleHeight = 1f;
            }

            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = rectTransform.GetComponent<Image>();
            button.colors = CreateButtonColors();

            TMP_Text label = CreateText("Label", rectTransform, 14, FontStyles.Bold, TextAlignmentOptions.Center, text);
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
                Debug.LogWarning("InputSystemUIInputModule type was not found. Research mini game UI can render, but pointer input may not work.");
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
    }
}
