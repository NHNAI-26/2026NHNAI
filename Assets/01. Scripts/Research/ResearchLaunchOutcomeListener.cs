using UnityEngine;
using UnityEngine.Events;

namespace Border.Research
{
    public sealed class ResearchLaunchOutcomeListener : MonoBehaviour
    {
        [SerializeField] private ResearchLaunchOutcomeEventChannelSO channel;
        [SerializeField] private UnityEvent succeeded = new();
        [SerializeField] private UnityEvent partiallySucceeded = new();
        [SerializeField] private UnityEvent failed = new();

        public ResearchLaunchOutcomeData LastOutcome { get; private set; }
        public UnityEvent Succeeded => succeeded;
        public UnityEvent PartiallySucceeded => partiallySucceeded;
        public UnityEvent Failed => failed;

        private void OnEnable()
        {
            if (channel == null) return;
            channel.OnEventRaised -= OnOutcome;
            channel.OnEventRaised += OnOutcome;
        }

        private void OnDisable()
        {
            if (channel != null) channel.OnEventRaised -= OnOutcome;
        }

        private void OnOutcome(ResearchLaunchOutcomeData outcome)
        {
            LastOutcome = outcome;
            if (outcome.Result.Grade <= ResearchGrade.B) succeeded.Invoke();
            else if (outcome.Result.Grade == ResearchGrade.C) partiallySucceeded.Invoke();
            else failed.Invoke();
        }
    }
}
