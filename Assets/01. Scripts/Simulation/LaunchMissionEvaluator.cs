using System;
using Border.Research;

namespace Simulation
{
    public enum LaunchMissionOutcome
    {
        Running,
        Succeeded,
        Failed
    }

    public sealed class LaunchMissionRules
    {
        public float LowAltitude { get; set; } = 100f;
        public float HighAltitude { get; set; } = 300f;
        public float TargetAltitude { get; set; } = 200f;
        public float TargetHorizontalMin { get; set; } = 80f;
        public float TargetHorizontalMax { get; set; } = 120f;
        public float RequiredHoldSeconds { get; set; } = 3f;
        public float MaxAttitudeError { get; set; } = 30f;
        public float MaxHoldSpeed { get; set; } = 50f;
        public float MaxBurnSeconds { get; set; } = 8f;
        public float FailureSpeed { get; set; } = 1f;
        public float NoLiftoffTimeout { get; set; } = 3f;

        internal LaunchMissionRules Snapshot()
        {
            var copy = (LaunchMissionRules)MemberwiseClone();
            RequireNonnegative(copy.LowAltitude, nameof(LowAltitude));
            RequireNonnegative(copy.HighAltitude, nameof(HighAltitude));
            RequireNonnegative(copy.TargetAltitude, nameof(TargetAltitude));
            RequireNonnegative(copy.TargetHorizontalMin, nameof(TargetHorizontalMin));
            RequireNonnegative(copy.TargetHorizontalMax, nameof(TargetHorizontalMax));
            RequireNonnegative(copy.RequiredHoldSeconds, nameof(RequiredHoldSeconds));
            RequireNonnegative(copy.MaxAttitudeError, nameof(MaxAttitudeError));
            RequireNonnegative(copy.MaxHoldSpeed, nameof(MaxHoldSpeed));
            RequireNonnegative(copy.MaxBurnSeconds, nameof(MaxBurnSeconds));
            RequireNonnegative(copy.FailureSpeed, nameof(FailureSpeed));
            RequireNonnegative(copy.NoLiftoffTimeout, nameof(NoLiftoffTimeout));
            if (copy.TargetHorizontalMax < copy.TargetHorizontalMin)
                throw new ArgumentException("Target horizontal maximum must be at least the minimum.");
            if (copy.HighAltitude < copy.LowAltitude)
                throw new ArgumentException("High altitude must be at least low altitude.");
            if (copy.RequiredHoldSeconds == 0f || copy.NoLiftoffTimeout == 0f)
                throw new ArgumentException("Hold duration and liftoff timeout must be positive.");
            return copy;
        }

        internal static void RequireNonnegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and nonnegative.");
        }
    }

    /// <summary>Evaluates launch-relative telemetry; distances use metres, time seconds and angles degrees.</summary>
    public sealed class LaunchMissionEvaluator
    {
        private readonly LaunchMissionId _missionId;
        private readonly LaunchMissionRules _rules;
        private float _elapsedSeconds;
        private bool _hasExceededFailureSpeed;

        public LaunchMissionOutcome Outcome { get; private set; }
        public float HoldSeconds { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;

        public LaunchMissionEvaluator(LaunchMissionId missionId, LaunchMissionRules rules = null)
        {
            ValidateMission(missionId);
            _missionId = missionId;
            _rules = (rules ?? new LaunchMissionRules()).Snapshot();
        }

        public LaunchMissionOutcome Step(float deltaTime, float altitude, float horizontalDistance,
            float speed, float attitudeError, float totalBurnSeconds, bool evaluateFailure = true)
        {
            if (Outcome != LaunchMissionOutcome.Running)
                return Outcome;

            LaunchMissionRules.RequireNonnegative(deltaTime, nameof(deltaTime));
            if (float.IsNaN(altitude) || float.IsInfinity(altitude))
                throw new ArgumentOutOfRangeException(nameof(altitude));
            LaunchMissionRules.RequireNonnegative(horizontalDistance, nameof(horizontalDistance));
            LaunchMissionRules.RequireNonnegative(speed, nameof(speed));
            LaunchMissionRules.RequireNonnegative(attitudeError, nameof(attitudeError));
            LaunchMissionRules.RequireNonnegative(totalBurnSeconds, nameof(totalBurnSeconds));

            _elapsedSeconds += deltaTime;
            _hasExceededFailureSpeed |= speed > _rules.FailureSpeed;
            bool inZone = altitude >= _rules.TargetAltitude
                && horizontalDistance >= _rules.TargetHorizontalMin
                && horizontalDistance <= _rules.TargetHorizontalMax;
            bool holdMission = _missionId == LaunchMissionId.ZoneHold
                || _missionId == LaunchMissionId.LowPowerZoneHold;
            bool holding = holdMission && inZone && attitudeError <= _rules.MaxAttitudeError
                && speed <= _rules.MaxHoldSpeed
                && (_missionId != LaunchMissionId.LowPowerZoneHold || totalBurnSeconds <= _rules.MaxBurnSeconds);
            HoldSeconds = holding ? HoldSeconds + deltaTime : 0f;

            bool succeeded;
            switch (_missionId)
            {
                case LaunchMissionId.LowAltitude:
                    succeeded = altitude >= _rules.LowAltitude;
                    break;
                case LaunchMissionId.HighAltitude:
                    succeeded = altitude >= _rules.HighAltitude;
                    break;
                case LaunchMissionId.TargetZone:
                    succeeded = inZone;
                    break;
                default:
                    succeeded = HoldSeconds >= _rules.RequiredHoldSeconds;
                    break;
            }

            if (succeeded)
                Outcome = LaunchMissionOutcome.Succeeded;
            else if (evaluateFailure && !holding && _hasExceededFailureSpeed && speed <= _rules.FailureSpeed)
                Fail("로켓 속력이 실패 기준 이하로 떨어졌습니다.");
            else if (evaluateFailure && !holding && !_hasExceededFailureSpeed && _elapsedSeconds >= _rules.NoLiftoffTimeout)
                Fail("제한 시간 안에 이륙하지 못했습니다.");
            return Outcome;
        }

        public LaunchMissionOutcome SelfDestruct()
        {
            if (Outcome == LaunchMissionOutcome.Running)
                Fail("자폭으로 미션을 종료했습니다.");
            return Outcome;
        }

        public static string GetObjectiveDescription(LaunchMissionId missionId, LaunchMissionRules rules = null)
        {
            ValidateMission(missionId);
            LaunchMissionRules r = (rules ?? new LaunchMissionRules()).Snapshot();
            switch (missionId)
            {
                case LaunchMissionId.LowAltitude:
                    return $"고도 {r.LowAltitude:0.##} m 도달";
                case LaunchMissionId.HighAltitude:
                    return $"고도 {r.HighAltitude:0.##} m 도달";
                case LaunchMissionId.TargetZone:
                    return $"고도 {r.TargetAltitude:0.##} m 이상, 수평 거리 {r.TargetHorizontalMin:0.##}~{r.TargetHorizontalMax:0.##} m 진입";
                default:
                    string hold = $"고도 {r.TargetAltitude:0.##} m 이상, 수평 거리 {r.TargetHorizontalMin:0.##}~{r.TargetHorizontalMax:0.##} m에서 자세 오차 {r.MaxAttitudeError:0.##}° 이하, 속력 {r.MaxHoldSpeed:0.##} m/s 이하로 {r.RequiredHoldSeconds:0.##}초 연속 유지";
                    return missionId == LaunchMissionId.LowPowerZoneHold
                        ? hold + $" (전체 엔진 누적 연소 시간 {r.MaxBurnSeconds:0.##}초 이하)" : hold;
            }
        }

        private void Fail(string reason)
        {
            FailureReason = reason;
            Outcome = LaunchMissionOutcome.Failed;
        }

        private static void ValidateMission(LaunchMissionId missionId)
        {
            if ((int)missionId < 1 || (int)missionId > 5)
                throw new ArgumentOutOfRangeException(nameof(missionId));
        }
    }
}
