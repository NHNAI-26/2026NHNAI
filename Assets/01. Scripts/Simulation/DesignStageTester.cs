using UnityEngine;

namespace Simulation
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class DesignStageTester : MonoBehaviour
    {
        [SerializeField] private Rocket rocket;
        [SerializeField] private RocketBuilder builder;
        [SerializeField] private EnginePresetLibrarySO presets;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public void Configure(Rocket target, RocketBuilder targetBuilder, EnginePresetLibrarySO library)
        {
            rocket = target;
            builder = targetBuilder;
            presets = library;
        }

        private void Awake()
        {
            initialPosition = rocket.transform.position;
            initialRotation = rocket.transform.rotation;
            builder.SetPresetLibrary(presets);
            rocket.AuthorizeLaunch = () => rocket.GetComponentInChildren<RocketPart>() != null;
        }

        public void ReturnToDesign()
        {
            if (!rocket.Launched) return;
            rocket.ResetFlight(initialPosition, initialRotation);
            builder.ReturnToDesign();
        }
    }
}
