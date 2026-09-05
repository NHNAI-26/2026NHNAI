using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchEndingController : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button restartButton;

        private ResearchFlowSession session;
        private Action responseCallback;
        private bool awaitingResponse;

        public void Initialize(ResearchFlowSession activeSession, Action onRestart)
        {
            Hide();
            if (titleText == null || bodyText == null || restartButton == null)
            {
                Debug.LogError("ResearchEndingController prefab has missing UI references.", this);
                return;
            }
            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            titleText.text = session.Model.GameWon ? "MISSION COMPLETE" : "MISSION FAILED";
            bodyText.text = BuildBody();
            responseCallback = onRestart;
            awaitingResponse = true;
            restartButton.interactable = true;
            restartButton.onClick.AddListener(Respond);
            gameObject.SetActive(true);
        }

        private string BuildBody()
        {
            ResearchPrototypeModel model = session.Model;
            LaunchMissionState finalMission = model.GetMission(LaunchMissionId.LowPowerZoneHold);
            string finalGrade = finalMission.HasBestGrade ? finalMission.BestGrade.ToString() : "없음";

            return $"최종 날짜: {(model.HasGameEnded ? model.FinalYear : model.Year)} Q{(model.HasGameEnded ? model.FinalQuarter : model.Quarter)}\n"
                + $"총 발사 횟수: {model.TotalLaunches}\n"
                + $"실패 횟수: {model.FailedLaunches}\n"
                + $"최고 분기 연구비: {model.HighestQuarterlyFunding}\n"
                + $"최종 미션 등급: {finalGrade}\n"
                + $"사용 연구비: {model.TotalSpentFunds}";
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
            if (restartButton != null) restartButton.onClick.RemoveListener(Respond);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            awaitingResponse = false;
            responseCallback = null;
            if (restartButton != null) restartButton.onClick.RemoveListener(Respond);
        }
    }
}
