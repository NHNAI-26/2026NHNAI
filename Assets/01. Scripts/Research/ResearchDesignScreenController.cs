using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchDesignScreenController : MonoBehaviour
    {
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

        public bool RequestedResearchReturn { get; private set; }

        public void InitializeForTests()
        {
            Initialize(ResearchFlowSession.GetOrCreate(), null);
        }

        public void Initialize(ResearchFlowSession activeSession, Action onReturnToResearch)
        {
            if (initialized)
            {
                return;
            }

            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            returnToResearchCallback = onReturnToResearch;
            BuildInterface();
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

        private void BuildInterface()
        {
            EnsureEventSystem();

            RectTransform canvasTransform = CreateGroup("ResearchDesignCanvas", transform);
            Canvas canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasTransform.gameObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreatePanel("Background", canvasTransform, new Color(0.07f, 0.08f, 0.11f, 0.93f));
            Stretch(background, 0f);

            RectTransform panel = CreatePanel("DesignBoundaryPanel", canvasTransform, new Color(0.15f, 0.18f, 0.23f, 0.98f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(1120f, 660f);
            AddVerticalLayout(panel, 16f, 16f, 14f, 12f);

            headerText = CreateText("Header", panel, 28, FontStyles.Bold, TextAlignmentOptions.Left, "설계 테스트");

            RectTransform columns = CreateGroup("Columns", panel);
            AddHorizontalLayout(columns, 0f, 0f, 0f, 14f);
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            BuildMapPanel(columns);
            BuildInfoPanel(columns);

            RectTransform actions = CreateGroup("Actions", panel);
            AddHorizontalLayout(actions, 0f, 0f, 0f, 10f);
            actions.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            Button backButton = CreateButton("ReturnToResearchButton", actions, "연구 단계로 돌아가기", 0f, 56f);
            backButton.onClick.AddListener(ReturnToResearch);

            launchButton = CreateButton("LaunchButton", actions, string.Empty, 0f, 56f);
            launchButton.onClick.AddListener(Launch);
            launchButtonText = launchButton.GetComponentInChildren<TMP_Text>();
        }

        private void BuildMapPanel(RectTransform parent)
        {
            RectTransform mapPanel = CreatePanel("MapPanel", parent, new Color(0.08f, 0.11f, 0.15f, 1f));
            AddVerticalLayout(mapPanel, 12f, 12f, 10f, 8f);
            mapPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0.9f;
            CreateText("MapTitle", mapPanel, 19, FontStyles.Bold, TextAlignmentOptions.Left, "맵 / 목표 경로");

            RectTransform viewport = CreatePanel("MapViewport", mapPanel, new Color(0.04f, 0.06f, 0.08f, 1f));
            viewport.gameObject.AddComponent<LayoutElement>().preferredHeight = 250f;

            Image gridA = CreatePanel("GridA", viewport, new Color(0.18f, 0.27f, 0.36f, 0.45f)).GetComponent<Image>();
            gridA.rectTransform.anchorMin = new Vector2(0.1f, 0.2f);
            gridA.rectTransform.anchorMax = new Vector2(0.9f, 0.22f);
            gridA.rectTransform.offsetMin = Vector2.zero;
            gridA.rectTransform.offsetMax = Vector2.zero;

            Image gridB = CreatePanel("GridB", viewport, new Color(0.18f, 0.27f, 0.36f, 0.45f)).GetComponent<Image>();
            gridB.rectTransform.anchorMin = new Vector2(0.12f, 0.55f);
            gridB.rectTransform.anchorMax = new Vector2(0.92f, 0.57f);
            gridB.rectTransform.offsetMin = Vector2.zero;
            gridB.rectTransform.offsetMax = Vector2.zero;

            Image path = CreatePanel("TargetPath", viewport, new Color(0.32f, 0.8f, 0.72f, 1f)).GetComponent<Image>();
            path.rectTransform.anchorMin = new Vector2(0.18f, 0.18f);
            path.rectTransform.anchorMax = new Vector2(0.82f, 0.25f);
            path.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            path.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            path.rectTransform.offsetMin = Vector2.zero;
            path.rectTransform.offsetMax = Vector2.zero;

            TMP_Text start = CreateText("StartPoint", viewport, 15, FontStyles.Bold, TextAlignmentOptions.Left, "START");
            start.rectTransform.anchorMin = new Vector2(0.12f, 0.13f);
            start.rectTransform.anchorMax = new Vector2(0.32f, 0.23f);
            start.rectTransform.offsetMin = Vector2.zero;
            start.rectTransform.offsetMax = Vector2.zero;

            TMP_Text target = CreateText("TargetPoint", viewport, 15, FontStyles.Bold, TextAlignmentOptions.Right, "TARGET");
            target.rectTransform.anchorMin = new Vector2(0.67f, 0.75f);
            target.rectTransform.anchorMax = new Vector2(0.9f, 0.85f);
            target.rectTransform.offsetMin = Vector2.zero;
            target.rectTransform.offsetMax = Vector2.zero;
        }

        private void BuildInfoPanel(RectTransform parent)
        {
            RectTransform infoPanel = CreatePanel("InfoPanel", parent, new Color(0.12f, 0.15f, 0.2f, 1f));
            AddVerticalLayout(infoPanel, 12f, 12f, 10f, 8f);
            infoPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.35f;

            CreateText("InfoTitle", infoPanel, 19, FontStyles.Bold, TextAlignmentOptions.Left, "설계 진입 정보");
            designDataText = CreateText("DesignDataText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);

            RectTransform presetRow = CreateGroup("PresetButtons", infoPanel);
            AddHorizontalLayout(presetRow, 0f, 0f, 0f, 5f);
            presetRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            for (int i = 0; i < ResearchPrototypeModel.MaxEnginePresetCount; i++)
            {
                EnginePresetId presetId = (EnginePresetId)i;
                Button button = CreateButton($"DesignPresetButton_{presetId}", presetRow, (i + 1).ToString("00"), 0f, 34f);
                button.onClick.AddListener(() =>
                {
                    selectedEnginePreset = presetId;
                    SaveDraft();
                    Refresh();
                });
            }

            RectTransform designRow = CreateGroup("DesignControls", infoPanel);
            AddHorizontalLayout(designRow, 0f, 0f, 0f, 6f);
            designRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            Button removeButton = CreateButton("RemoveEngineButton", designRow, "- 엔진", 0f, 38f);
            removeButton.onClick.AddListener(() => ChangeInstalledEngineCount(-1));
            Button addButton = CreateButton("AddEngineButton", designRow, "+ 엔진", 0f, 38f);
            addButton.onClick.AddListener(() => ChangeInstalledEngineCount(1));
            Button fitDownButton = CreateButton("DesignFitDownButton", designRow, "적합도 -10", 0f, 38f);
            fitDownButton.onClick.AddListener(() => ChangeDesignFit(-10));
            Button fitUpButton = CreateButton("DesignFitUpButton", designRow, "적합도 +10", 0f, 38f);
            fitUpButton.onClick.AddListener(() => ChangeDesignFit(10));

            RectTransform visibilityRow = CreateGroup("VisibilityControls", infoPanel);
            AddHorizontalLayout(visibilityRow, 0f, 0f, 0f, 6f);
            visibilityRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            publicButton = CreateButton("PublicTestButton", visibilityRow, "공개 테스트", 0f, 38f);
            publicButton.onClick.AddListener(() => SetVisibility(TestVisibility.Public));
            privateButton = CreateButton("PrivateTestButton", visibilityRow, "비공개 테스트", 0f, 38f);
            privateButton.onClick.AddListener(() => SetVisibility(TestVisibility.Private));

            installedEngineText = CreateText("InstalledEngineText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            statusText = CreateText("StatusText", infoPanel, 14, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private void LoadDraft(ResearchDesignEntryData data)
        {
            selectedEnginePreset = data.SelectedEnginePresetId;
            installedEngineCounts = CopyCounts(data.InstalledEngineCounts);
            designFit = data.DesignFit;
            visibility = data.Visibility;
        }

        private void SaveDraft()
        {
            if (!session.HasPendingDesignEntry)
            {
                return;
            }

            ResearchStageId stageId = session.PendingDesignEntry.StageId;
            ResearchDesignEntryData data = session.Model.CreateDesignEntry(stageId, selectedEnginePreset, installedEngineCounts, designFit, visibility);
            session.UpdatePendingDesignEntry(data);
        }

        private void ChangeInstalledEngineCount(int delta)
        {
            if (session.HasPendingDesignEntry && session.PendingDesignEntry.StageId == ResearchStageId.Engine)
            {
                statusText.text = "엔진 테스트는 정적 시험입니다. 설치 엔진을 쓰지 않습니다.";
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
            if (session.HasPendingDesignEntry && session.PendingDesignEntry.StageId == ResearchStageId.Moon)
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
            ResearchStageConfig stageConfig = ResearchPrototypeModel.GetStageConfig(data.StageId);
            EnginePresetConfig engineConfig = ResearchPrototypeModel.GetEnginePresetConfig(data.SelectedEnginePresetId);
            int successChance = model.CalculateSuccessChance(data);
            int partialChance = Math.Min(15, 95 - successChance);
            int failureChance = 100 - successChance - partialChance;

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

            bool finalMission = data.StageId == ResearchStageId.Moon;
            publicButton.interactable = !finalMission;
            privateButton.interactable = !finalMission;
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
            if (data.StageId == ResearchStageId.Engine)
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
                case ResearchActionResult.ProgressTooLow:
                    return "엔진 테스트 조건이 부족합니다.";
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
    }
}
