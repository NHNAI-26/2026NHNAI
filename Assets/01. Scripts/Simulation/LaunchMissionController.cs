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
        public float Speed => body.linearVelocity.magnitude;
        public UnityEvent ExplosionRequested => explosionRequested;

        public void Initialize(LaunchMissionId mission, Func<bool> authorize, Action<bool> onCompleted)
        {
            rocket = GetComponent<Rocket>();
            body = GetComponent<Rigidbody>();
            origin = transform.position;
            var rules = new LaunchMissionRules { FailureSpeed = failureSpeed, NoLiftoffTimeout = noLiftoffTimeout };
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
            var outcome = evaluator.Step(Time.fixedDeltaTime, Altitude, distance, Speed,
                Vector3.Angle(transform.up, Vector3.up), rocket.TotalBurnSeconds, enableAutomaticFailure);
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
            rocket.StopFlight();
            Status = success ? "미션 성공" : evaluator.FailureReason;
            completed?.Invoke(success);
        }
    }
}
