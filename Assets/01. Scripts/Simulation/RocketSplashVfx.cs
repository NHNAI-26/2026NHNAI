using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class RocketSplashVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem splashPrefab;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.15f;
        private Rocket rocket;

        private void OnEnable() => BindRocket();

        private void OnTransformParentChanged() => BindRocket();

        private void BindRocket()
        {
            if (rocket != null) rocket.SplashdownStarted -= PlaySplash;
            rocket = isActiveAndEnabled ? GetComponentInParent<Rocket>() : null;
            if (rocket != null) rocket.SplashdownStarted += PlaySplash;
        }

        private void OnDisable()
        {
            if (rocket != null) rocket.SplashdownStarted -= PlaySplash;
            rocket = null;
        }

        private void PlaySplash(Vector3 impactPoint)
        {
            if (splashPrefab == null) return;
            ParticleSystem splash = Instantiate(splashPrefab,
                impactPoint + Vector3.up * surfaceOffset, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(splash.gameObject, gameObject.scene);
            splash.Play(true);
        }
    }
}
