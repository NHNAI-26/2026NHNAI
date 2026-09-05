using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class RocketSplashVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem splashPrefab;
        private Rocket rocket;

        private void OnEnable()
        {
            rocket = GetComponentInParent<Rocket>();
            if (rocket != null) rocket.SplashdownStarted += PlaySplash;
        }

        private void OnDisable()
        {
            if (rocket != null) rocket.SplashdownStarted -= PlaySplash;
        }

        private void PlaySplash(Vector3 impactPoint)
        {
            if (splashPrefab == null) return;
            var splash = Instantiate(splashPrefab, impactPoint + Vector3.up * 0.15f, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(splash.gameObject, gameObject.scene);
            splash.Play(true);
        }
    }
}
