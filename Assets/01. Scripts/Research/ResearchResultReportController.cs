using System;
using Border.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchResultReportController : MonoBehaviour
    {
        private ResearchFlowSession session;
        private ResearchLaunchResultData result;
        private Action closeCallback;
        private Button closeButton;

        public void Initialize(ResearchFlowSession activeSession, ResearchLaunchResultData launchResult, Action onClose)
        {
            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            result = launchResult;
            closeCallback = onClose;
            BuildInterface();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("ResearchResultReportCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreatePanel(canvasObject.transform, "ReportPanel", new Vector2(620f, 420f));
            TMP_Text title = CreateText(panel, "Title", new Vector2(560f, 52f), 28, FontStyles.Bold, TextAlignmentOptions.Center);
            TMP_Text body = CreateText(panel, "Body", new Vector2(560f, 270f), 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            closeButton = CreateButton(panel, "CloseButton", "확인", new Vector2(220f, 48f));

            title.rectTransform.anchoredPosition = new Vector2(0f, 160f);
            body.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -160f);

            LaunchMissionConfig missionConfig = session.Model.GetConfiguredMissionConfig(result.MissionId);
            title.text = $"{missionConfig.DisplayName} 결과 보고서";
            body.text = BuildBody(missionConfig);
            closeButton.onClick.AddListener(Close);
        }

        private string BuildBody(LaunchMissionConfig missionConfig)
        {
            string outcome = result.Grade <= ResearchGrade.B ? "성공" : result.Grade == ResearchGrade.C ? "부분 성공" : "실패";
            string nextUnlock = CreateNextUnlockText();
            EngineRiskInfo[] risks = session.Model.GetTopEngineRisks(result.SelectedEnginePresetId, 2);
            string guidance = risks.Length > 0 ? risks[0].Description : "현재 엔진은 큰 약점 없이 균형형에 가깝습니다.";

            return $"등급: {result.Grade} / 판정: {outcome}\n"
                + $"미션: {missionConfig.DisplayName} / {ResearchPrototypeModel.GetVisibilityDisplayName(result.Visibility)}\n"
                + $"비용: {result.TotalCost}  즉시 지원금: +{result.ImmediateFunding}  분기 연구비: {result.QuarterlyFundingDelta:+#;-#;0}\n"
                + $"성공 {result.SuccessChance}% / 부분 {result.PartialChance}% / 실패 {result.FailureChance}% / 굴림 {result.Roll}\n"
                + $"다음 해금: {nextUnlock}\n"
                + $"안내: {guidance}";
        }

        private string CreateNextUnlockText()
        {
            if (result.FinalMissionWon)
            {
                return "최종 미션 성공";
            }

            if (result.MissionId == LaunchMissionId.LowPowerZoneHold)
            {
                return result.Grade <= ResearchGrade.C ? "최종 검증 완료, B 이상 필요" : "최종 검증 실패";
            }

            LaunchMissionId next = ResearchPrototypeModel.GetNextMission(result.MissionId);
            LaunchMissionConfig nextConfig = session.Model.GetConfiguredMissionConfig(next);
            return result.Grade <= ResearchGrade.C ? $"{nextConfig.DisplayName} 해금" : $"{nextConfig.DisplayName} 조건 미충족";
        }

        private void Close()
        {
            closeButton.onClick.RemoveListener(Close);
            closeCallback?.Invoke();
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            Image image = panelObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.94f);
            return rect;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 size, int fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.22f, 0.32f, 0.42f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            TMP_Text text = CreateText(buttonObject.transform, "Label", size, 20, FontStyles.Bold, TextAlignmentOptions.Center);
            text.text = label;
            return button;
        }

        private static void EnsureEventSystem()
        {
            UiEventSystemUtility.Ensure();
        }
    }
}
