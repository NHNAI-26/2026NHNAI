using UnityEngine;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class RocketLiftSmoke : MonoBehaviour
    {
        [SerializeField] private ParticleSystem smoke;
        private Rocket rocket;

        private void OnEnable()
        {
            rocket = GetComponentInParent<Rocket>();
            ClearSmoke();
        }

        private void OnTransformParentChanged() => rocket = GetComponentInParent<Rocket>();

        private void LateUpdate()
        {
            if (smoke == null) return;
            if (rocket == null || !rocket.Launched)
            {
                if (smoke.isPlaying || smoke.particleCount > 0) ClearSmoke();
                return;
            }

            bool emitting = rocket.LiftAssistActive;
            if (smoke.isEmitting == emitting) return;
            if (emitting) smoke.Play(true);
            else smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void OnDisable() => ClearSmoke();

        private void ClearSmoke()
        {
            if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
