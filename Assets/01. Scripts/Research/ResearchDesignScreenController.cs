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
        private TMP_Text headerText;
        private TMP_Text designDataText;
        private TMP_Text statusText;
        private TMP_Text launchButtonText;

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

            Refresh(session.PendingDesignEntry);
        }

        public void ReturnToResearch()
        {
            session.ClearPendingDesignEntry();
            RequestedResearchReturn = true;
            returnToResearchCallback?.Invoke();
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

            RectTransform background = CreatePanel("Background", canvasTransform, new Color(0.07f, 0.08f, 0.11f, 0.92f));
            Stretch(background, 0f);

            RectTransform panel = CreatePanel("DesignBoundaryPanel", canvasTransform, new Color(0.15f, 0.18f, 0.23f, 0.97f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(1100f, 640f);
            AddVerticalLayout(panel, 18f, 18f, 16f, 14f);

            headerText = CreateText("Header", panel, 30, FontStyles.Bold, TextAlignmentOptions.Left, "로켓 설계 테스트");

            RectTransform columns = CreateGroup("Columns", panel);
            AddHorizontalLayout(columns, 0f, 0f, 0f, 16f);
            columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            BuildMapPanel(columns);
            BuildInfoPanel(columns);

            RectTransform actions = CreateGroup("Actions", panel);
            AddHorizontalLayout(actions, 0f, 0f, 0f, 12f);
            actions.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            Button backButton = CreateButton("ReturnToResearchButton", actions, "연구 단계로 돌아가기", 0f, 56f);
            backButton.onClick.AddListener(ReturnToResearch);

            Button launchButton = CreateButton("LaunchButton", actions, string.Empty, 0f, 56f);
            launchButton.onClick.AddListener(Launch);
            launchButtonText = launchButton.GetComponentInChildren<TMP_Text>();
        }

        private void BuildMapPanel(RectTransform parent)
        {
            RectTransform mapPanel = CreatePanel("MapPanel", parent, new Color(0.08f, 0.11f, 0.15f, 1f));
            AddVerticalLayout(mapPanel, 14f, 14f, 12f, 10f);
            mapPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.2f;

            CreateText("MapTitle", mapPanel, 20, FontStyles.Bold, TextAlignmentOptions.Left, "맵 / 목표 경로");

            RectTransform viewport = CreatePanel("MapViewport", mapPanel, new Color(0.04f, 0.06f, 0.08f, 1f));
            viewport.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

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
            path.rectTransform.anchoredPosition = Vector2.zero;
            path.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            path.rectTransform.offsetMin = Vector2.zero;
            path.rectTransform.offsetMax = Vector2.zero;

            TMP_Text start = CreateText("StartPoint", viewport, 16, FontStyles.Bold, TextAlignmentOptions.Left, "START");
            start.rectTransform.anchorMin = new Vector2(0.12f, 0.13f);
            start.rectTransform.anchorMax = new Vector2(0.32f, 0.23f);
            start.rectTransform.offsetMin = Vector2.zero;
            start.rectTransform.offsetMax = Vector2.zero;

            TMP_Text target = CreateText("TargetPoint", viewport, 16, FontStyles.Bold, TextAlignmentOptions.Right, "TARGET");
            target.rectTransform.anchorMin = new Vector2(0.67f, 0.75f);
            target.rectTransform.anchorMax = new Vector2(0.9f, 0.85f);
            target.rectTransform.offsetMin = Vector2.zero;
            target.rectTransform.offsetMax = Vector2.zero;
        }

        private void BuildInfoPanel(RectTransform parent)
        {
            RectTransform infoPanel = CreatePanel("InfoPanel", parent, new Color(0.12f, 0.15f, 0.2f, 1f));
            AddVerticalLayout(infoPanel, 14f, 14f, 12f, 10f);
            infoPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateText("InfoTitle", infoPanel, 20, FontStyles.Bold, TextAlignmentOptions.Left, "설계 진입 정보");
            designDataText = CreateText("DesignDataText", infoPanel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
            statusText = CreateText("StatusText", infoPanel, 15, FontStyles.Normal, TextAlignmentOptions.Left, string.Empty);
        }

        private void Refresh(ResearchDesignEntryData data)
        {
            ResearchPrototypeModel model = session.Model;
            headerText.text = $"{data.StageId} 설계 테스트";
            designDataText.text = $"날짜: {data.Year} Q{data.Quarter}\n"
                + $"맵 시드: {data.MapSeed}\n"
                + $"목표 경로: {data.TargetPathId}\n"
                + $"현재 진행도: {data.CurrentProgress}/100\n"
                + $"이전 단계 평균: {data.PrerequisiteAverage:0.0}\n"
                + $"발사 경험 보정: +{data.ExperienceBonus}%p\n"
                + $"연구 기준 성공률: {model.CalculateSuccessChance(data.StageId)}%";
            statusText.text = "발사 전입니다. 연구 단계로 돌아가도 비용, 분기, 발사 횟수는 변하지 않습니다.";
            launchButtonText.text = $"발사\n비용 {ResearchPrototypeModel.GetStageConfig(data.StageId).TestCost} / 1분기";
        }

        public ResearchActionResult LaunchForTests(out ResearchLaunchResultData result)
        {
            return LaunchInternal(out result);
        }

        private void Launch()
        {
            LaunchInternal(out _);
        }

        private ResearchActionResult LaunchInternal(out ResearchLaunchResultData result)
        {
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
                    return "발사비가 부족합니다.";
                case ResearchActionResult.DeadlineReached:
                    return "마감에 도달해 발사할 수 없습니다.";
                default:
                    return "현재 상태에서는 발사할 수 없습니다.";
            }
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

            TMP_Text label = CreateText("Label", rectTransform, 16, FontStyles.Bold, TextAlignmentOptions.Center, text);
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
    }
}
