using System;
using Border.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchEndingController : MonoBehaviour
    {
        private ResearchFlowSession session;
        private Action restartCallback;
        private Button restartButton;

        public void Initialize(ResearchFlowSession activeSession, Action onRestart)
        {
            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            restartCallback = onRestart;
            BuildInterface();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("ResearchEndingCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreatePanel(canvasObject.transform, "EndingPanel", new Vector2(640f, 430f));
            TMP_Text title = CreateText(panel, "Title", new Vector2(560f, 58f), 32, FontStyles.Bold, TextAlignmentOptions.Center);
            TMP_Text body = CreateText(panel, "Body", new Vector2(560f, 260f), 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            restartButton = CreateButton(panel, "RestartButton", "다시 시작", new Vector2(220f, 48f));

            title.rectTransform.anchoredPosition = new Vector2(0f, 165f);
            body.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -160f);

            title.text = session.Model.GameWon ? "MISSION COMPLETE" : "MISSION FAILED";
            body.text = BuildBody();
            restartButton.onClick.AddListener(Restart);
        }

        private string BuildBody()
        {
            ResearchPrototypeModel model = session.Model;
            LaunchMissionState finalMission = model.GetMission(LaunchMissionId.LowPowerZoneHold);
            string finalGrade = finalMission.HasBestGrade ? finalMission.BestGrade.ToString() : "없음";

            return $"최종 날짜: {model.Year} Q{model.Quarter}\n"
                + $"총 발사 횟수: {model.TotalLaunches}\n"
                + $"실패 횟수: {model.FailedLaunches}\n"
                + $"최고 분기 연구비: {model.HighestQuarterlyFunding}\n"
                + $"최종 미션 등급: {finalGrade}\n"
                + $"사용 연구비: {model.TotalSpentFunds}";
        }

        private void Restart()
        {
            restartButton.onClick.RemoveListener(Restart);
            restartCallback?.Invoke();
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
            image.color = new Color(0.07f, 0.08f, 0.09f, 0.96f);
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
            image.color = new Color(0.28f, 0.34f, 0.28f, 1f);
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
