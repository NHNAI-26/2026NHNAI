using System;
using Border.Research;
using UnityEngine;
using UnityEngine.Events;

namespace Simulation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rocket))]
    public sealed class LaunchMissionController : MonoBehaviour
    {
        [SerializeField] private bool enableAutomaticFailure = true;
        [SerializeField, Min(0f)] private float failureSpeed = 1f;
        [SerializeField, Min(0f)] private float maxSettledAngularSpeed = 5f;
        [SerializeField, Min(0.1f)] private float groundedFailureSeconds = 3f;
        [SerializeField, Min(0.1f)] private float splashdownFailureSeconds = 3f;
        [SerializeField, Min(0.1f)] private float noLiftoffTimeout = 10f;
        [SerializeField] private UnityEvent explosionRequested = new();
        [SerializeField] private bool waitForExplosionCompletion;
        [SerializeField, Min(0f)] private float placeholderExplosionSeconds = 0.5f;
        private Rocket rocket;
        private Rigidbody body;
        private Vector3 origin;
        private LaunchMissionEvaluator evaluator;
        private Action<bool> completed;
        private bool returning;
        private float explosionTime;
        public bool IsExploding { get; private set; }
        public bool CanSelfDestruct => rocket != null && rocket.Launched && !returning && !IsExploding;
        public string Objective { get; private set; }
        public string Status { get; private set; } = "발사 대기";
        public float Altitude => Mathf.Max(0f, transform.position.y - origin.y);

        /// <summary>
        /// 발사 정보 패널이 읽는 비행 최대치. FixedUpdate 가 종료 뒤에는 돌지 않으므로 마지막 값이 그대로 남는다 —
        /// 현재 고도를 그대로 보여주면 하강할 때 같이 줄어들어 "얼마나 올라갔나"를 못 읽는다.
        /// </summary>
        public float MaxAltitude { get; private set; }
        public float MaxDistance { get; private set; }

        /// <summary>목표 구역 체류 시간. 판정은 평가기가 하고 화면은 여기서만 읽는다.</summary>
        public float HoldSeconds => evaluator != null ? evaluator.HoldSeconds : 0f;

        /// <summary>미션이 끝났는지, 끝났다면 성공인지. <see cref="Status"/> 문자열을 파싱하지 않기 위한 것이다.</summary>
        public bool Finished { get; private set; }
        public bool Succeeded { get; private set; }

        /// <summary>관제 화면 스테퍼가 읽는 진행 단계. 발사 전에는 0 이다.</summary>
        public int Stage => evaluator != null ? evaluator.StageIndex : 0;

        /// <summary>이 미션이 쓰는 단계 수. 초기화 전에는 전체 단계로 본다.</summary>
        public int StageCount => evaluator != null ? evaluator.StageCount : LaunchMissionEvaluator.StageNames.Length;
        public float Speed => body.linearVelocity.magnitude;
        public UnityEvent ExplosionRequested => explosionRequested;

        public void Initialize(LaunchMissionId mission, Func<bool> authorize, Action<bool> onCompleted)
        {
            rocket = GetComponent<Rocket>();
            body = GetComponent<Rigidbody>();
            origin = transform.position;
            var rules = new LaunchMissionRules
            {
                FailureSpeed = failureSpeed,
                MaxSettledAngularSpeed = maxSettledAngularSpeed,
                GroundedFailureSeconds = groundedFailureSeconds,
                SplashdownFailureSeconds = splashdownFailureSeconds,
                NoLiftoffTimeout = noLiftoffTimeout
            };
            evaluator = new LaunchMissionEvaluator(mission, rules);
            Objective = LaunchMissionEvaluator.GetObjectiveDescription(mission, rules);
            completed = onCompleted;
            rocket.AuthorizeLaunch = authorize;
        }

        private void FixedUpdate()
        {
            if (evaluator == null || !rocket.Launched || returning || IsExploding) return;
            Vector3 offset = transform.position - origin;
            float distance = new Vector2(offset.x, offset.z).magnitude;
            MaxAltitude = Mathf.Max(MaxAltitude, Altitude);
            MaxDistance = Mathf.Max(MaxDistance, distance);
            var outcome = evaluator.Step(Time.fixedDeltaTime, Altitude, distance, Speed,
                Vector3.Angle(transform.up, Vector3.up), rocket.TotalBurnSeconds, enableAutomaticFailure,
                rocket.IsGrounded, rocket.Splashed, body.angularVelocity.magnitude * Mathf.Rad2Deg);
            Status = $"고도 {Altitude:0.0}m / 속력 {Speed:0.0}m/s\n거리 {distance:0.0}m / 체류 {evaluator.HoldSeconds:0.0}s / 총 연소 {rocket.TotalBurnSeconds:0.0}s";
            if (outcome != LaunchMissionOutcome.Running) Finish(outcome == LaunchMissionOutcome.Succeeded);
        }

        private void Update()
        {
            if (!IsExploding || returning || waitForExplosionCompletion) return;
            explosionTime += Time.unscaledDeltaTime;
            if (explosionTime >= placeholderExplosionSeconds) CompleteSelfDestruction();
        }

        public void SelfDestruct()
        {
            if (!CanSelfDestruct) return;
            evaluator.SelfDestruct();
            IsExploding = true;
            Status = "자폭 · 미션 실패";
            rocket.StopFlight();
            explosionRequested.Invoke();
        }

        // Connect the future explosion animation's completion event here.
        public void CompleteSelfDestruction()
        {
            if (IsExploding && !returning) Finish(false);
        }

        private void Finish(bool success)
        {
            if (returning) return;
            returning = true;
            Finished = true;
            Succeeded = success;
            rocket.StopFlight();
            Status = success ? "미션 성공" : evaluator.FailureReason;
            completed?.Invoke(success);
        }
    }
}
