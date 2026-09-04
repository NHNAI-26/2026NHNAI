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
        private bool initialized;
        private EnginePresetId selectedEnginePreset;
        private int[] installedEngineCounts;
        private int designFit;
        private TestVisibility visibility;

        private TMP_Text headerText;
        private TMP_Text designDataText;
        private TMP_Text installedEngineText;
        private TMP_Text statusText;
        private TMP_Text launchButtonText;
        private Button publicButton;
        private Button privateButton;
        private Button launchButton;
        private Button[] presetButtons;

        public bool RequestedResearchReturn { get; private set; }

        public void InitializeForTests()
        {
            Initialize(ResearchFlowSession.GetOrCreate(), null);
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
            if (initialized)
            {
                return;
            }

            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            returnToResearchCallback = onReturnToResearch;
            if (!BuildInterface())
            {
                return;
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

            GameObject instance = Instantiate(prefab, transform);
            instance.name = "ResearchDesignCanvas";
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
            publicButton = FindRequiredButton(root, "PublicTestButton");
            privateButton = FindRequiredButton(root, "PrivateTestButton");
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
                || publicButton == null
                || privateButton == null
                || launchButton == null
                || backButton == null
                || removeButton == null
                || addButton == null
                || fitDownButton == null
                || fitUpButton == null)
            {
                DestroyUnityObject(instance);
                return false;
            }

            launchButtonText = launchButton.GetComponentInChildren<TMP_Text>(true);
            BuildPresetButtons(presetRow);
            backButton.onClick.AddListener(ReturnToResearch);
            launchButton.onClick.AddListener(Launch);
            removeButton.onClick.AddListener(() => ChangeInstalledEngineCount(-1));
            addButton.onClick.AddListener(() => ChangeInstalledEngineCount(1));
            fitDownButton.onClick.AddListener(() => ChangeDesignFit(-10));
            fitUpButton.onClick.AddListener(() => ChangeDesignFit(10));
            publicButton.onClick.AddListener(() => SetVisibility(TestVisibility.Public));
            privateButton.onClick.AddListener(() => SetVisibility(TestVisibility.Private));
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
                Button button = CreateButtonFromPrefab(enginePresetButtonPrefab, $"DesignPresetButton_{presetId}", presetRow, (i + 1).ToString("00"), 0f, 34f);
                presetButtons[i] = button;
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

            LaunchStageId stageId = session.PendingDesignEntry.StageId;
            ClearLockedEngineCounts(installedEngineCounts);
            ResearchDesignEntryData data = session.Model.CreateDesignEntry(stageId, selectedEnginePreset, installedEngineCounts, designFit, visibility);
            session.UpdatePendingDesignEntry(data);
        }

        private void ChangeInstalledEngineCount(int delta)
        {
            if (session.HasPendingDesignEntry && session.PendingDesignEntry.StageId == LaunchStageId.Engine)
            {
                statusText.text = "초기 목표는 정적 검증입니다. 설치 엔진을 쓰지 않습니다.";
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

        private void SetVisibility(TestVisibility nextVisibility)
        {
            if (session.HasPendingDesignEntry && session.PendingDesignEntry.StageId == LaunchStageId.Moon)
            {
                visibility = TestVisibility.FinalMission;
            }
            else
            {
                visibility = nextVisibility;
            }

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
            LaunchStageConfig stageConfig = ResearchPrototypeModel.GetStageConfig(data.StageId);
            EnginePresetConfig engineConfig = ResearchPrototypeModel.GetEnginePresetConfig(data.SelectedEnginePresetId);
            int successChance = model.CalculateSuccessChance(data);
            int partialChance = Math.Min(15, 95 - successChance);
            int failureChance = 100 - successChance - partialChance;
            RefreshPresetButtons(model);

            headerText.text = $"{stageConfig.DisplayName} 설계";
            designDataText.text = $"날짜: {data.Year} Q{data.Quarter} / 맵 시드: {data.MapSeed} / 목표: {data.TargetPathId}\n"
                + $"선택 프리셋: {engineConfig.DisplayName} Lv.{data.SelectedEngineLevel} / 성능 {data.SelectedEngineScore}\n"
                + $"설계 적합도: {data.DesignFit} ({ResearchPrototypeModel.CalculateDesignFitModifier(data.DesignFit):+#;-#;0}%p) / {ResearchPrototypeModel.GetVisibilityDisplayName(data.Visibility)} ({ResearchPrototypeModel.GetVisibilitySuccessModifier(data.Visibility):+#;-#;0}%p)\n"
                + $"성공 {successChance}% / 부분 {partialChance}% / 실패 {failureChance}%";
            installedEngineText.text = $"설치 엔진: {FormatInstalledEngines(data)}\n"
                + $"설치 엔진 점수 {data.InstalledEngineScore} / 발사비 {data.LaunchCost} / 예약 설치비 {data.ReservedInstallCost} / 총 확정 비용 {data.LaunchCost + data.ReservedInstallCost}";
            statusText.text = "발사 전입니다. 연구 단계로 돌아가면 예약 설치 비용은 버려지고 연구비/분기/발사 횟수는 변하지 않습니다.";
            launchButtonText.text = $"발사\n총 비용 {data.LaunchCost + data.ReservedInstallCost} / 1분기";
            launchButton.interactable = model.Funds >= data.LaunchCost + data.ReservedInstallCost && !model.DeadlineReached;

            bool finalMission = data.StageId == LaunchStageId.Moon;
            publicButton.interactable = !finalMission;
            privateButton.interactable = !finalMission;
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
                returnToResearchCallback?.Invoke();
                return actionResult;
            }

            statusText.text = GetLaunchFailureText(actionResult);
            return actionResult;
        }

        private static string FormatInstalledEngines(ResearchDesignEntryData data)
        {
            if (data.StageId == LaunchStageId.Engine)
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

                string name = ResearchPrototypeModel.GetEnginePresetConfig((EnginePresetId)i).DisplayName;
                text += string.IsNullOrEmpty(text) ? $"{name} x{count}" : $", {name} x{count}";
            }

            return string.IsNullOrEmpty(text) ? "없음" : text;
        }

        private static string FormatLaunchResult(ResearchLaunchResultData result)
        {
            string ending = result.MoonMissionWon
                ? "달 착륙 성공"
                : result.DeadlineMissed
                    ? "마감 실패"
                    : "연구 화면 복귀";
            return $"{result.StageId} 발사 결과 {result.Grade}. 성공 {result.SuccessChance}%, 부분 {result.PartialChance}%, 실패 {result.FailureChance}%, 굴림 {result.Roll}. {ending}.";
        }

        private static string GetLaunchFailureText(ResearchActionResult actionResult)
        {
            switch (actionResult)
            {
                case ResearchActionResult.NoPendingDesignEntry:
                    return "발사할 설계 데이터가 없습니다.";
                case ResearchActionResult.NotEnoughFunds:
                    return "발사비와 설치비가 부족합니다.";
                case ResearchActionResult.DeadlineReached:
                    return "마감에 도달해 발사할 수 없습니다.";
                case ResearchActionResult.RequirementNotMet:
                    return "초기 목표 조건이 부족합니다.";
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

            return button;
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
