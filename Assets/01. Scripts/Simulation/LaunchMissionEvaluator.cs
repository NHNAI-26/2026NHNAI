using System;
using Border.Research;
using UnityEngine;

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
        public Vector3 TargetBoxCenterOffset { get; set; } = new(0f, 260f, 100f);
        public Vector3 TargetBoxSize { get; set; } = new(100f, 120f, 100f);
        public float RequiredHoldSeconds { get; set; } = 3f;
        public float MaxAttitudeError { get; set; } = 30f;
        public float MaxHoldSpeed { get; set; } = 50f;
        public float MaxBurnSeconds { get; set; } = 8f;
        public float FailureSpeed { get; set; } = 1f;
        public float NoLiftoffTimeout { get; set; } = 10f;
        public float LiftoffAltitude { get; set; } = 3f;
        public float MaxSettledAngularSpeed { get; set; } = 5f;
        public float GroundedFailureSeconds { get; set; } = 3f;
        public float SplashdownFailureSeconds { get; set; } = 3f;

        internal LaunchMissionRules Snapshot()
        {
            var copy = (LaunchMissionRules)MemberwiseClone();
            RequireNonnegative(copy.LowAltitude, nameof(LowAltitude));
            RequireNonnegative(copy.HighAltitude, nameof(HighAltitude));
            RequireNonnegative(copy.TargetAltitude, nameof(TargetAltitude));
            RequireNonnegative(copy.TargetHorizontalMin, nameof(TargetHorizontalMin));
            RequireNonnegative(copy.TargetHorizontalMax, nameof(TargetHorizontalMax));
            RequireFinite(copy.TargetBoxCenterOffset, nameof(TargetBoxCenterOffset));
            RequirePositive(copy.TargetBoxSize.x, nameof(TargetBoxSize));
            RequirePositive(copy.TargetBoxSize.y, nameof(TargetBoxSize));
            RequirePositive(copy.TargetBoxSize.z, nameof(TargetBoxSize));
            RequireNonnegative(copy.RequiredHoldSeconds, nameof(RequiredHoldSeconds));
            RequireNonnegative(copy.MaxAttitudeError, nameof(MaxAttitudeError));
            RequireNonnegative(copy.MaxHoldSpeed, nameof(MaxHoldSpeed));
            RequireNonnegative(copy.MaxBurnSeconds, nameof(MaxBurnSeconds));
            RequireNonnegative(copy.FailureSpeed, nameof(FailureSpeed));
            RequireNonnegative(copy.NoLiftoffTimeout, nameof(NoLiftoffTimeout));
            RequireNonnegative(copy.LiftoffAltitude, nameof(LiftoffAltitude));
            RequireNonnegative(copy.MaxSettledAngularSpeed, nameof(MaxSettledAngularSpeed));
            RequireNonnegative(copy.GroundedFailureSeconds, nameof(GroundedFailureSeconds));
            RequireNonnegative(copy.SplashdownFailureSeconds, nameof(SplashdownFailureSeconds));
            if (copy.TargetHorizontalMax < copy.TargetHorizontalMin)
                throw new ArgumentException("Target horizontal maximum must be at least the minimum.");
            if (copy.HighAltitude < copy.LowAltitude)
                throw new ArgumentException("High altitude must be at least low altitude.");
            if (copy.RequiredHoldSeconds == 0f || copy.NoLiftoffTimeout == 0f
                || copy.GroundedFailureSeconds == 0f || copy.SplashdownFailureSeconds == 0f)
                throw new ArgumentException("Hold duration, failure delays and liftoff timeout must be positive.");
            return copy;
        }

        internal static void RequireNonnegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and nonnegative.");
        }

        private static void RequirePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and positive.");
        }

        private static void RequireFinite(Vector3 value, string name)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x)
                || float.IsNaN(value.y) || float.IsInfinity(value.y)
                || float.IsNaN(value.z) || float.IsInfinity(value.z))
                throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }
    }

    /// <summary>Evaluates launch-relative telemetry; distances use metres, time seconds and angles degrees.</summary>
    public sealed class LaunchMissionEvaluator
    {
        private readonly LaunchMissionId _missionId;
        private readonly LaunchMissionRules _rules;
        private float _elapsedSeconds;
        private bool _hasLiftedOff;
        private float _groundedSeconds;
        private float _splashedSeconds;

        public LaunchMissionOutcome Outcome { get; private set; }
        public float HoldSeconds { get; private set; }
        public string FailureReason { get; private set; } = string.Empty;
        public LaunchTerminationReason TerminationReason { get; private set; } = LaunchTerminationReason.Unknown;

        /// <summary>비행 단계의 이름. 관제 화면 스테퍼가 이 배열을 그대로 읽는다.</summary>
        public static readonly string[] StageNames = { "점화", "이륙", "상승", "목표 구역", "체류" };

        /// <summary>
        /// 끝난 단계 수(0..<see cref="StageCount"/>). 조건이 깨져도 되돌아가지 않는다 — 스테퍼가
        /// 체류 판정이 리셋될 때마다 뒤로 튀면 어디까지 갔었는지 읽을 수 없다.
        /// </summary>
        public int StageIndex { get; private set; }

        /// <summary>이 미션이 실제로 쓰는 단계 수. 나머지 단계는 화면에서 흐리게 나온다.</summary>
        public int StageCount { get; }
        public Bounds TargetBoxBounds => new(_rules.TargetBoxCenterOffset, _rules.TargetBoxSize);
        public bool UsesTargetBox => UsesTargetBoxMission(_missionId);

        public LaunchMissionEvaluator(LaunchMissionId missionId, LaunchMissionRules rules = null)
        {
            ValidateMission(missionId);
            _missionId = missionId;
            _rules = (rules ?? new LaunchMissionRules()).Snapshot();
            StageCount = missionId switch
            {
                LaunchMissionId.LowAltitude or LaunchMissionId.HighAltitude => 3, // 점화·이륙·상승
                LaunchMissionId.TargetZone => 4,                                  // + 목표 구역
                _ => StageNames.Length,                                           // + 체류
            };
        }

        public LaunchMissionOutcome Step(float deltaTime, float altitude, float horizontalDistance,
            float speed, float attitudeError, float totalBurnSeconds, bool evaluateFailure = true,
            bool isGrounded = false, bool hasSplashed = false, float angularSpeed = 0f, bool inTargetBox = false)
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
            LaunchMissionRules.RequireNonnegative(angularSpeed, nameof(angularSpeed));

            _elapsedSeconds += deltaTime;
            _hasLiftedOff |= altitude >= _rules.LiftoffAltitude;
            bool inZone = UsesTargetBox ? inTargetBox : altitude >= _rules.TargetAltitude
                && horizontalDistance >= _rules.TargetHorizontalMin
                && horizontalDistance <= _rules.TargetHorizontalMax;
            bool holdMission = _missionId == LaunchMissionId.ZoneHold
                || _missionId == LaunchMissionId.LowPowerZoneHold;
            bool holding = holdMission && inZone && attitudeError <= _rules.MaxAttitudeError
                && speed <= _rules.MaxHoldSpeed
                && (_missionId != LaunchMissionId.LowPowerZoneHold || totalBurnSeconds <= _rules.MaxBurnSeconds);
            HoldSeconds = holding ? HoldSeconds + deltaTime : 0f;
            bool settled = evaluateFailure && !holding && !hasSplashed && isGrounded
                && speed <= _rules.FailureSpeed && angularSpeed <= _rules.MaxSettledAngularSpeed;
            _groundedSeconds = settled ? _groundedSeconds + deltaTime : 0f;
            _splashedSeconds = evaluateFailure && hasSplashed ? _splashedSeconds + deltaTime : 0f;

            // 단계는 판정에 쓰인 값에서 그대로 유도한다. Step 이 불렸다는 것 자체가 점화가 끝났다는 뜻이다.
            int stage = 1;
            if (_hasLiftedOff) stage = 2;
            if (altitude >= AscentAltitude) stage = 3;
            // 이 미션이 안 쓰는 단계는 조건이 우연히 맞아도 세지 않는다 —
            // 고도 미션은 목표 구역 안을 지나가더라도 그것으로 진행했다고 보지 않는다.
            if (StageCount > 3 && inZone) stage = Math.Max(stage, 4);
            if (StageCount > 4 && HoldSeconds > 0f) stage = 5;
            StageIndex = Math.Max(StageIndex, stage);

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
            {
                Outcome = LaunchMissionOutcome.Succeeded;
                TerminationReason = LaunchTerminationReason.Succeeded;
            }
            else if (evaluateFailure && hasSplashed && _splashedSeconds >= _rules.SplashdownFailureSeconds)
                Fail("로켓이 바다에 추락했습니다.", LaunchTerminationReason.Splashdown);
            else if (settled && _groundedSeconds >= _rules.GroundedFailureSeconds
                && (_hasLiftedOff || _elapsedSeconds >= _rules.NoLiftoffTimeout))
                Fail(_hasLiftedOff ? "로켓이 지면에 추락해 멈췄습니다." : "제한 시간 안에 이륙하지 못했습니다.",
                    _hasLiftedOff ? LaunchTerminationReason.GroundCrash : LaunchTerminationReason.NoLiftoff);
            return Outcome;
        }

        /// <summary>상승 단계의 기준 고도. 미션의 목표 고도와 같아서 고도 미션은 성공 순간 스테퍼가 찬다.</summary>
        private float AscentAltitude => _missionId switch
        {
            LaunchMissionId.LowAltitude => _rules.LowAltitude,
            LaunchMissionId.HighAltitude => _rules.HighAltitude,
            _ => _rules.TargetAltitude,
        };

        public LaunchMissionOutcome SelfDestruct()
        {
            if (Outcome == LaunchMissionOutcome.Running)
                Fail("자폭으로 미션을 종료했습니다.", LaunchTerminationReason.SelfDestruct);
            return Outcome;
        }

        public LaunchMissionOutcome Overheat()
        {
            if (Outcome == LaunchMissionOutcome.Running)
                Fail("엔진 과열로 로켓이 폭발했습니다.", LaunchTerminationReason.Overheat);
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
                    return $"목표 구역 진입 ({r.TargetBoxSize.x:0.##}×{r.TargetBoxSize.y:0.##}×{r.TargetBoxSize.z:0.##} m)";
                default:
                    string hold = $"목표 구역 안에서 자세 오차 {r.MaxAttitudeError:0.##}° 이하, 속력 {r.MaxHoldSpeed:0.##} m/s 이하로 {r.RequiredHoldSeconds:0.##}초 연속 유지";
                    return missionId == LaunchMissionId.LowPowerZoneHold
                        ? hold + $" (전체 엔진 누적 연소 시간 {r.MaxBurnSeconds:0.##}초 이하)" : hold;
            }
        }

        private void Fail(string reason, LaunchTerminationReason terminationReason)
        {
            FailureReason = reason;
            TerminationReason = terminationReason;
            Outcome = LaunchMissionOutcome.Failed;
        }

        private static void ValidateMission(LaunchMissionId missionId)
        {
            if ((int)missionId < 1 || (int)missionId > 5)
                throw new ArgumentOutOfRangeException(nameof(missionId));
        }

        private static bool UsesTargetBoxMission(LaunchMissionId missionId) =>
            missionId == LaunchMissionId.TargetZone
            || missionId == LaunchMissionId.ZoneHold
            || missionId == LaunchMissionId.LowPowerZoneHold;
    }
}
