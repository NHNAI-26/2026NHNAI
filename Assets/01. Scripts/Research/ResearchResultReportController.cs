using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchResultReportController : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button closeButton;

        private ResearchFlowSession session;
        private ResearchLaunchResultData result;
        private Action responseCallback;
        private bool awaitingResponse;

        public void Initialize(ResearchFlowSession activeSession, ResearchLaunchResultData launchResult, Action onClose)
        {
            Hide();
            if (titleText == null || bodyText == null || closeButton == null)
            {
                Debug.LogError("ResearchResultReportController prefab has missing UI references.", this);
                return;
            }
            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            result = launchResult;
            LaunchMissionConfig missionConfig = session.Model.GetConfiguredMissionConfig(result.MissionId);
            titleText.text = $"{missionConfig.DisplayName} 결과 보고서";
            ConfigureReportText(bodyText);
            bodyText.text = BuildBody(missionConfig);
            responseCallback = onClose;
            awaitingResponse = true;
            closeButton.interactable = true;
            closeButton.onClick.AddListener(Respond);
            gameObject.SetActive(true);
        }

        private string BuildBody(LaunchMissionConfig missionConfig)
        {
            string outcome = result.Grade <= ResearchGrade.B ? "성공" : result.Grade == ResearchGrade.C ? "부분 성공" : "실패";
            string nextUnlock = CreateNextUnlockText();
            LaunchOutcomeEventResult outcomeEvent = result.OutcomeEvent;
            string eventText = outcomeEvent != null
                ? $"발생 이벤트: {outcomeEvent.Name}\n"
                    + $"이벤트 설명: {outcomeEvent.Description}\n"
                    + $"이벤트 효과: {outcomeEvent.EffectsText}\n"
                : "발생 이벤트: 없음\n";

            return $"등급: {result.Grade} / 실제 판정: {outcome}\n"
                + $"미션: {missionConfig.DisplayName} / {ResearchPrototypeModel.GetVisibilityDisplayName(result.Visibility)}\n"
                + $"비용: {result.TotalCost}\n"
                + $"기본 보상: 즉시 지원금 +{result.ImmediateFunding} / 분기 연구비 {result.QuarterlyFundingDelta:+#;-#;0}\n"
                + eventText
                + $"다음 해금: {nextUnlock}";
        }

        private static void ConfigureReportText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMin = text.fontSizeMin > 0f ? Math.Min(text.fontSizeMin, 14f) : 14f;
            text.fontSizeMax = text.fontSize;
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


        private void Respond()
        {
            if (!awaitingResponse) return;
            Action callback = responseCallback;
            Hide();
            callback?.Invoke();
        }

        public void Hide()
        {
            awaitingResponse = false;
            responseCallback = null;
            if (closeButton != null) closeButton.onClick.RemoveListener(Respond);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            awaitingResponse = false;
            responseCallback = null;
            if (closeButton != null) closeButton.onClick.RemoveListener(Respond);
        }
    }
}
