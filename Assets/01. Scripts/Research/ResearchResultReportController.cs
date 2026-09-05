using System;
using Border.UI;
using UnityEngine;

namespace Border.Research
{
    public sealed class ResearchResultReportController : MonoBehaviour
    {
        [SerializeField] private NewspaperReveal newspaper;
        [SerializeField] private NewspaperReveal mail;

        private ResearchFlowSession session;
        private Action responseCallback;
        private bool awaitingResponse;
        private NewspaperReveal activeReveal;

        public void Initialize(ResearchFlowSession activeSession, ResearchLaunchResultData launchResult, Action onClose)
        {
            Hide();
            session = activeSession ?? ResearchFlowSession.GetOrCreate();
            LaunchNewspaperArticle article = LaunchNewspaperArticle.Create(
                launchResult,
                session.Model.GetConfiguredMissionConfig(launchResult.MissionId).DisplayName);
            activeReveal = article.Medium == LaunchResultMedium.Mail ? mail : newspaper;
            if (activeReveal == null)
            {
                Debug.LogError($"ResearchResultReportController prefab must reference a {article.Medium} reveal.", this);
                return;
            }

            responseCallback = onClose;
            awaitingResponse = true;
            gameObject.SetActive(true);
            if (newspaper != null) newspaper.gameObject.SetActive(activeReveal == newspaper);
            if (mail != null) mail.gameObject.SetActive(activeReveal == mail);
            activeReveal.Present(article, session.LaunchPhoto, Respond);
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
            if (mail != null) mail.gameObject.SetActive(false);
            activeReveal = null;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            awaitingResponse = false;
            responseCallback = null;
        }
    }
}
