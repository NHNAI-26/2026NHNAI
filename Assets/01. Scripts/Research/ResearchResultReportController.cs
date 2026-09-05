using System;
using Border.UI;
using UnityEngine;

namespace Border.Research
{
    public sealed class ResearchResultReportController : MonoBehaviour
    {
        [SerializeField] private NewspaperReveal newspaper;

        private ResearchFlowSession session;
        private Action responseCallback;
        private bool awaitingResponse;

        public void Initialize(ResearchFlowSession activeSession, ResearchLaunchResultData launchResult, Action onClose)
        {
            Hide();
            if (newspaper == null)
            {
                Debug.LogError("ResearchResultReportController prefab must reference NewspaperReveal.", this);
                return;
            }

            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            responseCallback = onClose;
            awaitingResponse = true;
            gameObject.SetActive(true);
            newspaper.gameObject.SetActive(true);

            LaunchMissionConfig missionConfig = session.Model.GetConfiguredMissionConfig(launchResult.MissionId);
            LaunchNewspaperArticle article = LaunchNewspaperArticle.Create(launchResult, missionConfig.DisplayName);
            newspaper.Present(article, session.LaunchPhoto, Respond);
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
            if (newspaper != null) newspaper.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            awaitingResponse = false;
            responseCallback = null;
        }
    }
}
