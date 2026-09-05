using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchDesignScreenController : MonoBehaviour
    {
        private const string DesignScreenPrefabPath = "ResearchUI/ResearchDesignScreen";
        private const string DesignEnginePresetButtonPrefabPath = "ResearchUI/DesignEnginePresetButton";

        [SerializeField] private GameObject designScreenPrefab;
        [SerializeField] private Button enginePresetButtonPrefab;

        private ResearchFlowSession session;
        private Action returnToResearchCallback;
        private Action<ResearchLaunchResultData> launchCommittedCallback;
        private bool initialized;
        private bool interfaceBuilt;
        private EnginePresetId selectedEnginePreset;
        private int[] installedEngineCounts;
        private int designFit;
        private TestVisibility visibility;

        private TMP_Text headerText;
        private TMP_Text designDataText;
        private TMP_Text installedEngineText;
        private TMP_Text statusText;
        private TMP_Text launchButtonText;
        private Button launchButton;
        private Button[] presetButtons;
        private GameObject interfaceRoot;

        public bool RequestedResearchReturn { get; private set; }

        public void InitializeForTests()
        {
            Initialize(ResearchFlowSession.GetOrCreate(), null, null);
        }

        public void ConfigurePresetButtonPrefabForTests(Button enginePresetButtonTemplate)
        {
            enginePresetButtonPrefab = enginePresetButtonTemplate;
        }

        public void ConfigureScreenPrefabForTests(GameObject screenTemplate)
        {
            designScreenPrefab = screenTemplate;
        }

        public void Initialize(ResearchFlowSession activeSession, Action onReturnToResearch)
        {
            Initialize(activeSession, onReturnToResearch, null);
        }

        public void Initialize(ResearchFlowSession activeSession, Action onReturnToResearch, Action<ResearchLaunchResultData> onLaunchCommitted)
        {
            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            returnToResearchCallback = onReturnToResearch;
            launchCommittedCallback = onLaunchCommitted;
            RequestedResearchReturn = false;

            if (!interfaceBuilt && !BuildInterface())
            {
                return;
            }

            if (interfaceRoot != null)
            {
                interfaceRoot.SetActive(true);
            }

            initialized = true;

            if (!session.HasPendingDesignEntry)
            {
                RequestedResearchReturn = true;
                statusText.text = "설계 진입 데이터가 없습니다. 연구 화면으로 돌아갑니다.";
                returnToResearchCallback?.Invoke();
                return;
            }

            LoadDraft(session.PendingDesignEntry);
            Refresh();
        }

        public void HideForReuse()
        {
            returnToResearchCallback = null;
            launchCommittedCallback = null;
            if (interfaceRoot != null)
            {
                interfaceRoot.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        public void ReturnToResearch()
        {
            session.ClearPendingDesignEntry();
            RequestedResearchReturn = true;
            returnToResearchCallback?.Invoke();
        }

        public ResearchActionResult LaunchForTests(out ResearchLaunchResultData result)
        {
            return LaunchInternal(out result);
        }

        private bool BuildInterface()
        {
            EnsureEventSystem();
            EnsureDefaultPrefabs();

            if (TryBuildInterfaceFromPrefab())
            {
                return true;
            }

            Debug.LogError("Research design UI prefab is missing or invalid. Expected Resources/ResearchUI/ResearchDesignScreen plus DesignEnginePresetButton.");
            return false;
        }

        private bool TryBuildInterfaceFromPrefab()
        {
            GameObject prefab = designScreenPrefab != null
                ? designScreenPrefab
                : Resources.Load<GameObject>(DesignScreenPrefabPath);
            if (prefab == null)
            {
                return false;
            }

            GameObject instance;
            bool createdInstance = false;
            Transform existingCanvas = transform.Find("ResearchDesignCanvas");
            if (existingCanvas != null)
            {
                instance = existingCanvas.gameObject;
            }
            else if (CanCreateRuntimeUiFallback())
            {
                instance = Instantiate(prefab, transform);
                instance.name = "ResearchDesignCanvas";
                createdInstance = true;
            }
            else
            {
                Debug.LogError("Research design UI must be preplaced in 01_Main.", this);
                return false;
            }

            interfaceRoot = instance;
            interfaceRoot.SetActive(true);
            if (instance.GetComponent<RectTransform>() == null)
            {
                instance.AddComponent<RectTransform>();
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

            Transform root = instance.transform;
            RectTransform presetRow = FindChildRectTransform(root, "PresetButtons");
            headerText = FindRequiredText(root, "Header");
            designDataText = FindRequiredText(root, "DesignDataText");
            installedEngineText = FindRequiredText(root, "InstalledEngineText");
            statusText = FindRequiredText(root, "StatusText");
            launchButton = FindRequiredButton(root, "LaunchButton");
            Button backButton = FindRequiredButton(root, "ReturnToResearchButton");
            Button removeButton = FindRequiredButton(root, "RemoveEngineButton");
            Button addButton = FindRequiredButton(root, "AddEngineButton");
            Button fitDownButton = FindRequiredButton(root, "DesignFitDownButton");
            Button fitUpButton = FindRequiredButton(root, "DesignFitUpButton");

            if (enginePresetButtonPrefab == null
                || presetRow == null
                || headerText == null
                || designDataText == null
                || installedEngineText == null
                || statusText == null
                || launchButton == null
                || backButton == null
                || removeButton == null
                || addButton == null
                || fitDownButton == null
                || fitUpButton == null)
            {
                if (createdInstance)
                {
                    DestroyUnityObject(instance);
                }

                return false;
            }

            launchButtonText = launchButton.GetComponentInChildren<TMP_Text>(true);
            ConfigureDynamicMultilineText(designDataText);
            ConfigureDynamicMultilineText(installedEngineText);
            ConfigureDynamicMultilineText(statusText);
            BuildPresetButtons(presetRow);
            backButton.onClick.AddListener(ReturnToResearch);
            launchButton.onClick.AddListener(Launch);
            removeButton.onClick.AddListener(() => ChangeInstalledEngineCount(-1));
            addButton.onClick.AddListener(() => ChangeInstalledEngineCount(1));
            fitDownButton.onClick.AddListener(() => ChangeDesignFit(-10));
            fitUpButton.onClick.AddListener(() => ChangeDesignFit(10));
            interfaceBuilt = true;
            return true;
        }

        private void EnsureDefaultPrefabs()
        {
            if (designScreenPrefab == null)
            {
                designScreenPrefab = Resources.Load<GameObject>(DesignScreenPrefabPath);
            }

            if (enginePresetButtonPrefab == null)
            {
                enginePresetButtonPrefab = Resources.Load<Button>(DesignEnginePresetButtonPrefabPath);
            }
        }

        private void BuildPresetButtons(RectTransform presetRow)
        {
            presetButtons = new Button[ResearchPrototypeModel.MaxEnginePresetCount];
            for (int i = 0; i < ResearchPrototypeModel.MaxEnginePresetCount; i++)
            {
                EnginePresetId presetId = (EnginePresetId)i;
                string buttonName = $"DesignPresetButton_{presetId}";
                Button button = FindRequiredButton(presetRow, buttonName);
                if (button == null)
                {
                    button = CreateButtonFromPrefab(enginePresetButtonPrefab, buttonName, presetRow, (i + 1).ToString("00"), 0f, 34f);
                }
                else
                {
                    ConfigurePresetButton(button, (i + 1).ToString("00"), 0f, 34f);
                }

                presetButtons[i] = button;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (!session.Model.IsEnginePresetUnlocked(presetId))
                    {
                        return;
                    }

                    selectedEnginePreset = presetId;
                    SaveDraft();
                    Refresh();
                });
            }
        }

        private void LoadDraft(ResearchDesignEntryData data)
        {
            selectedEnginePreset = data.SelectedEnginePresetId;
            installedEngineCounts = CopyCounts(data.InstalledEngineCounts);
            ClearLockedEngineCounts(installedEngineCounts);
            designFit = data.DesignFit;
            visibility = data.Visibility;
        }

        private void SaveDraft()
        {
            if (!session.HasPendingDesignEntry)
            {
                return;
            }

            LaunchMissionId missionId = session.PendingDesignEntry.MissionId;
            bool launchCostPaid = session.PendingDesignEntry.LaunchCostPaid;
            int paidEntryCost = session.PendingDesignEntry.LaunchCost;
            ClearLockedEngineCounts(installedEngineCounts);
            ResearchDesignEntryData data = session.Model.CreateDesignEntry(missionId, selectedEnginePreset, installedEngineCounts, designFit, visibility, launchCostPaid, paidEntryCost);
            session.UpdatePendingDesignEntry(data);
        }

        private void ChangeInstalledEngineCount(int delta)
        {
            if (session.HasPendingDesignEntry && session.PendingDesignEntry.MissionId == LaunchMissionId.StaticFire)
            {
                statusText.text = "정적 연소 시험은 선택 엔진만 검증합니다. 설치 엔진을 쓰지 않습니다.";
                return;
            }

            if (!session.Model.IsEnginePresetUnlocked(selectedEnginePreset))
            {
                statusText.text = "아직 개발되지 않은 엔진입니다.";
                return;
            }

            int index = (int)selectedEnginePreset;
            installedEngineCounts[index] = Math.Max(0, installedEngineCounts[index] + delta);
            SaveDraft();
            Refresh();
        }

        private void ChangeDesignFit(int delta)
        {
            designFit = ResearchPrototypeModel.ClampInt(designFit + delta, ResearchPrototypeModel.MinDesignFit, ResearchPrototypeModel.MaxDesignFit);
            SaveDraft();
            Refresh();
        }

        private void Refresh()
        {
            if (!session.HasPendingDesignEntry)
            {
                return;
            }

            ResearchDesignEntryData data = session.PendingDesignEntry;
            ResearchPrototypeModel model = session.Model;
            LaunchMissionConfig missionConfig = model.GetConfiguredMissionConfig(data.MissionId);
            int designEntryCost = data.LaunchCostPaid ? data.LaunchCost : model.GetDesignEntryCost(data.MissionId);
            int remainingCost = model.GetLaunchPaymentCost(data);
            RefreshPresetButtons(model);

            headerText.text = $"{missionConfig.DisplayName} 설계";
            designDataText.text = $"날짜: {data.Year} Q{data.Quarter} / 맵 시드: {data.MapSeed} / 목표: {data.TargetPathId}\n"
                + $"선택 프리셋: {model.GetEnginePresetName(data.SelectedEnginePresetId)} / 완성도 {data.SelectedEngineCompletion} / 성능 {data.SelectedEngineScore}\n"
                + $"설계 적합도: {data.DesignFit} / {ResearchPrototypeModel.GetVisibilityDisplayName(data.Visibility)}\n"
                + $"설계 진입비 {designEntryCost} / 지불 확정비 {data.LaunchCost}";
            installedEngineText.text = $"설치 엔진: {FormatInstalledEngines(data)}\n"
                + $"설치 엔진 점수 {data.InstalledEngineScore} / 남은 발사비 {remainingCost} / 예약 설치비 {data.ReservedInstallCost}";
            statusText.text = FormatDesignStatusText(data.LaunchCostPaid, model.PendingLaunchEffectsText);
            launchButtonText.text = $"발사\n남은 비용 {remainingCost} / 1분기";
            launchButton.interactable = model.Funds >= remainingCost && !model.DeadlineReached;

        }

        private void RefreshPresetButtons(ResearchPrototypeModel model)
        {
            if (presetButtons == null)
            {
                return;
            }

            for (int i = 0; i < presetButtons.Length; i++)
            {
                Button button = presetButtons[i];
                if (button == null)
                {
                    continue;
                }

                bool unlocked = model.IsEnginePresetUnlocked((EnginePresetId)i);
                button.gameObject.SetActive(unlocked);
                button.interactable = unlocked;
            }
        }

        private void Launch()
        {
            LaunchInternal(out _);
        }

        private ResearchActionResult LaunchInternal(out ResearchLaunchResultData result)
        {
            SaveDraft();
            ResearchActionResult actionResult = session.CommitPendingDesignLaunch(out result);
            if (actionResult == ResearchActionResult.Success)
            {
                RequestedResearchReturn = true;
                statusText.text = FormatLaunchResult(result);
                if (launchCommittedCallback != null)
                {
                    launchCommittedCallback.Invoke(result);
                }
                else
                {
                    returnToResearchCallback?.Invoke();
                }
                return actionResult;
            }

            statusText.text = GetLaunchFailureText(actionResult);
            return actionResult;
        }

        private string FormatInstalledEngines(ResearchDesignEntryData data)
        {
            if (data.MissionId == LaunchMissionId.StaticFire)
            {
                return "정적 시험대";
            }

            string text = string.Empty;
            for (int i = 0; i < data.InstalledEngineCounts.Length; i++)
            {
                int count = data.InstalledEngineCounts[i];
                if (count <= 0)
                {
                    continue;
                }

                string name = session.Model.GetEnginePresetName((EnginePresetId)i);
                text += string.IsNullOrEmpty(text) ? $"{name} x{count}" : $", {name} x{count}";
            }

            return string.IsNullOrEmpty(text) ? "없음" : text;
        }

        private static string FormatLaunchResult(ResearchLaunchResultData result)
        {
            string outcome = result.Grade <= ResearchGrade.B ? "성공" : result.Grade == ResearchGrade.C ? "부분 성공" : "실패";
            string ending = result.FinalMissionWon
                ? "최종 미션 성공"
                : result.DeadlineMissed
                    ? "마감 실패"
                    : "연구 화면 복귀";
            return $"{result.MissionId} 발사 결과 {result.Grade}. 실제 판정 {outcome}. 기본 보상 +{result.ImmediateFunding}, 기본 분기 예산 {result.QuarterlyFundingDelta:+#;-#;0}. {FormatOutcomeEventSummary(result.OutcomeEvent)}. {ending}.";
        }

        private static string FormatDesignStatusText(bool launchCostPaid, string pendingEffectsText)
        {
            string baseText = launchCostPaid
                ? "발사 전입니다. 연구 단계로 돌아가면 예약 설치 비용은 버려지고 예산은 이미 지불된 상태입니다."
                : "발사 전입니다. 발사 확정 시 예산과 예약 설치 비용을 함께 지불합니다.";

            if (string.IsNullOrWhiteSpace(pendingEffectsText))
            {
                return baseText;
            }

            return $"{baseText}\n남은 이벤트 효과: {pendingEffectsText}";
        }

        private static string FormatOutcomeEventSummary(LaunchOutcomeEventResult outcomeEvent)
        {
            if (outcomeEvent == null)
            {
                return "이벤트 효과 없음";
            }

            return $"이벤트 {outcomeEvent.Name}: {outcomeEvent.EffectsText}";
        }

        private static string GetLaunchFailureText(ResearchActionResult actionResult)
        {
            switch (actionResult)
            {
                case ResearchActionResult.NoPendingDesignEntry:
                    return "발사할 설계 데이터가 없습니다.";
                case ResearchActionResult.NotEnoughFunds:
                    return "예산과 설치비가 부족합니다.";
                case ResearchActionResult.DeadlineReached:
                    return "마감에 도달해 발사할 수 없습니다.";
                case ResearchActionResult.RequirementNotMet:
                    return "미션 조건이 부족합니다.";
                default:
                    return "현재 상태에서는 발사할 수 없습니다.";
            }
        }

        private static int[] CopyCounts(int[] source)
        {
            var copy = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            if (source == null)
            {
                return copy;
            }

            int length = Math.Min(copy.Length, source.Length);
            for (int i = 0; i < length; i++)
            {
                copy[i] = Math.Max(0, source[i]);
            }

            return copy;
        }

        private void ClearLockedEngineCounts(int[] counts)
        {
            if (counts == null || session?.Model == null)
            {
                return;
            }

            for (int i = 0; i < counts.Length; i++)
            {
                if (!session.Model.IsEnginePresetUnlocked((EnginePresetId)i))
                {
                    counts[i] = 0;
                }
            }
        }

        private static Button CreateButtonFromPrefab(Button prefab, string name, Transform parent, string text, float preferredWidth, float preferredHeight)
        {
            if (prefab == null)
            {
                Debug.LogError($"Missing button prefab for {name}.");
                return null;
            }

            Button button = Instantiate(prefab, parent);
            button.name = name;
            ConfigurePresetButton(button, text, preferredWidth, preferredHeight);
            return button;
        }

        private static void ConfigurePresetButton(Button button, string text, float preferredWidth, float preferredHeight)
        {
            LayoutElement layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth > 0f)
            {
                layout.preferredWidth = preferredWidth;
            }
            else
            {
                layout.flexibleWidth = 1f;
            }

            layout.preferredHeight = preferredHeight;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
            }
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

        private bool CanCreateRuntimeUiFallback()
        {
            return !Application.isPlaying || gameObject.scene.name != ResearchFlowSession.MainSceneName;
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
    }
}
