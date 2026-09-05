using UnityEngine;
using UnityEngine.Events;

namespace Border.Research
{
    public readonly struct ResearchLaunchOutcomeData
    {
        public ResearchLaunchOutcomeData(ResearchLaunchResultData result, string reason)
        {
            Result = result;
            Reason = string.IsNullOrWhiteSpace(reason)
                ? result.Grade <= ResearchGrade.B ? "미션 성공" : result.Grade == ResearchGrade.C ? "부분 성공" : "미션 실패"
                : reason;
        }

        public ResearchLaunchResultData Result { get; }
        public string Reason { get; }
    }

    [CreateAssetMenu(fileName = "ResearchLaunchOutcome", menuName = "Border/Events/Research Launch Outcome")]
    public sealed class ResearchLaunchOutcomeEventChannelSO : ScriptableObject
    {
        public UnityAction<ResearchLaunchOutcomeData> OnEventRaised = delegate { };

        public void RaiseEvent(ResearchLaunchOutcomeData outcome) => OnEventRaised?.Invoke(outcome);
    }
}
