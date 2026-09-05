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
