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
        [SerializeField, Min(0f)] private float placeholderExplosionSeconds = 1.2f;
        private Rocket rocket;
        private Rigidbody body;
        private Vector3 origin;
        private LaunchMissionEvaluator evaluator;
        private LaunchTargetZoneGuide targetGuide;
        private Vector3 targetZoneCenter;
        private float targetZoneRadius;
        private Action<bool> completed;
        private bool returning;
        private float explosionTime;
        private bool inTargetZone;
        public bool IsExploding { get; private set; }
        public bool CanSelfDestruct => rocket != null && rocket.Launched && !returning && !IsExploding;
        public string Objective { get; private set; }
        public string Status { get; private set; } = "발사 대기";
        public LaunchTerminationReason TerminationReason =>
            evaluator == null ? LaunchTerminationReason.Unknown : evaluator.TerminationReason;
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
        public bool UsesTargetBox => evaluator != null && evaluator.UsesTargetBox;
        public bool IsInTargetBox => inTargetZone;
        public Bounds TargetBoxBounds => new(targetZoneCenter, Vector3.one * (targetZoneRadius * 2f));
        public Vector3 TargetZoneCenter => targetZoneCenter;
        public float TargetZoneRadius => targetZoneRadius;

        public void Initialize(LaunchMissionId mission, Func<bool> authorize, Action<bool> onCompleted)
        {
            rocket = GetComponent<Rocket>();
            rocket.OverheatExplosionStarted -= HandleOverheat;
            rocket.OverheatExplosionStarted += HandleOverheat;
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

            if (evaluator.UsesTargetBox)
            {
                targetZoneCenter = origin + evaluator.TargetZoneCenterOffset;
                targetZoneRadius = evaluator.TargetZoneRadius;
                targetGuide = gameObject.AddComponent<LaunchTargetZoneGuide>();
                targetGuide.Initialize(transform, targetZoneCenter, targetZoneRadius);
                FindFirstObjectByType<RocketBuilder>()?.PreviewDesignTarget(TargetBoxBounds);
            }
        }

        private void FixedUpdate()
        {
            if (evaluator == null || returning || IsExploding) return;
            UpdateTargetBoxState();

            // 클램프에 붙들려 있는 동안은 시계를 세운다 — 홀드가 이륙 타임아웃과 접지 실패 판정을
            // 먹으면 준비 시간을 늘릴 때마다 미션 난이도가 같이 바뀐다.
            if (!rocket.Lifted)
            {
                if (rocket.Holding) Status = "점화 · 클램프 유지";
                return;
            }

            Vector3 offset = transform.position - origin;
            float distance = new Vector2(offset.x, offset.z).magnitude;
            MaxAltitude = Mathf.Max(MaxAltitude, Altitude);
            MaxDistance = Mathf.Max(MaxDistance, distance);
            var outcome = evaluator.Step(Time.fixedDeltaTime, Altitude, distance, Speed,
                Vector3.Angle(transform.up, Vector3.up), rocket.TotalBurnSeconds, enableAutomaticFailure,
                rocket.IsGrounded, rocket.Splashed, body.angularVelocity.magnitude * Mathf.Rad2Deg, inTargetZone);
            Status = $"고도 {Altitude:0.0}m / 속력 {Speed:0.0}m/s\n거리 {distance:0.0}m / 체류 {evaluator.HoldSeconds:0.0}s / 총 연소 {rocket.TotalBurnSeconds:0.0}s";
            if (outcome != LaunchMissionOutcome.Running) Finish(outcome == LaunchMissionOutcome.Succeeded);
        }

        private void Update()
        {
            if (evaluator != null && !returning && !IsExploding && (rocket == null || !rocket.Launched))
            {
                UpdateTargetBoxState();
            }

            if (!IsExploding || returning || waitForExplosionCompletion) return;
            explosionTime += Time.unscaledDeltaTime;
            if (explosionTime >= placeholderExplosionSeconds) CompleteSelfDestruction();
        }

        public void SelfDestruct()
        {
            if (!CanSelfDestruct) return;
            evaluator.SelfDestruct();
            BeginExplosion("자폭 · 미션 실패");
        }

        private void HandleOverheat()
        {
            if (returning || IsExploding || evaluator == null) return;
            evaluator.Overheat();
            BeginExplosion("과열 폭발 · 미션 실패");
        }

        private void OnDestroy()
        {
            if (rocket != null) rocket.OverheatExplosionStarted -= HandleOverheat;
            if (targetGuide != null) targetGuide.Dispose();
        }

        private void BeginExplosion(string status)
        {
            IsExploding = true;
            explosionTime = 0f;
            Status = status;
            rocket.Explode();
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
            if (targetGuide != null) targetGuide.Dispose();
            GetComponent<LaunchPhotoCapture>()?.CaptureOutcome();
            rocket.StopFlight();
            Status = success ? "미션 성공" : evaluator.FailureReason;
            completed?.Invoke(success);
        }

        private bool CalculateRocketBounds(out Bounds bounds)
        {
            bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (collider == null || !collider.enabled) continue;
                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else bounds.Encapsulate(collider.bounds);
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || !renderer.enabled) continue;
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else bounds.Encapsulate(renderer.bounds);
            }

            if (hasBounds) return true;

            bounds = new Bounds(transform.position, Vector3.one);
            return true;
        }

        private void UpdateTargetBoxState()
        {
            if (!UsesTargetBox)
            {
                return;
            }

            inTargetZone = CalculateRocketBounds(out Bounds rocketBounds)
                && TouchesSphere(rocketBounds, targetZoneCenter, targetZoneRadius);
            if (targetGuide != null) targetGuide.Tick(inTargetZone);
        }

        private static bool TouchesSphere(Bounds bounds, Vector3 center, float radius)
        {
            Vector3 closest = bounds.ClosestPoint(center);
            return (closest - center).sqrMagnitude <= radius * radius;
        }
    }
}
