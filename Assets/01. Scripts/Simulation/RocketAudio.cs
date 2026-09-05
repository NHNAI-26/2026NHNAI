using System.Collections;
using System.Collections.Generic;
using Border.Audio;
using UnityEngine;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class RocketAudio : MonoBehaviour
    {
        [SerializeField] private SoundManager soundManagerPrefab;
        [SerializeField, Min(0.1f)] private float engineStopInterval = 0.6f;

        private Rocket rocket;
        private readonly Dictionary<RocketPart, SoundHandle> loops = new();
        private readonly List<RocketPart> stoppedEngines = new();
        private readonly List<SoundHandle> alerts = new();
        private SoundHandle spark;
        private SoundHandle launch;
        private SkyEnvironment skyEnvironment;
        private bool launchMusicStarted;
        private bool spaceMusicStarted;

        private void Start()
        {
            if (rocket != null && !rocket.Launched)
                SoundManager.Instance?.PlayBgm("LaunchPanelLoop");
        }

        private void OnEnable()
        {
            rocket = GetComponentInParent<Rocket>();
            if (rocket == null) return;
            if (SoundManager.Instance == null && soundManagerPrefab != null)
                Instantiate(soundManagerPrefab);
            rocket.LaunchStarted += OnLaunch;
            rocket.LiftoffStarted += OnLiftoff;
            rocket.SplashdownStarted += OnSplashdown;
            rocket.ExplosionStarted += OnExplosion;
        }

        private void OnLaunch()
        {
            ClearAudio();
            launchMusicStarted = true;
            spaceMusicStarted = false;
            skyEnvironment = null;
            foreach (var environment in FindObjectsByType<SkyEnvironment>(FindObjectsSortMode.None))
            {
                if (environment.Target == rocket.transform)
                {
                    skyEnvironment = environment;
                    break;
                }
            }
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBgm("Launch");
                spark = SoundManager.Instance.PlaySfx("SparkStart");
            }
        }

        private void OnExplosion()
        {
            ClearAudio();
            for (int i = 0; i < 4; i++)
                SoundManager.Instance?.PlaySfx("RocketExplosion");
        }

        private void OnSplashdown(Vector3 impactPoint)
        {
            // Keep the 2D impact alive after the rocket sinks or the scene unloads.
            SoundManager.Instance?.PlaySfx("HeavyWave");
        }

        private void OnLiftoff()
        {
            spark.Stop();
            if (SoundManager.Instance == null) return;
            foreach (RocketPart engine in rocket.GetComponentsInChildren<RocketPart>())
            {
                if (!engine.Ignited || !engine.HasFuel) continue;
                loops[engine] = SoundManager.Instance.PlaySfxAttached("RocketLoop", engine.transform);
            }
            if (loops.Count > 0) launch = SoundManager.Instance.PlaySfx("RocketLaunch");
        }

        private void LateUpdate()
        {
            if (rocket == null || !rocket.Launched)
            {
                if (rocket != null && launchMusicStarted)
                    SoundManager.Instance?.PlayBgm("LaunchPanelLoop");
                launchMusicStarted = false;
                spaceMusicStarted = false;
                ClearAudio();
                return;
            }

            if (!spaceMusicStarted && !rocket.FlightStopped && !rocket.Splashed
                && skyEnvironment != null && skyEnvironment.IsInSpace && SoundManager.Instance != null)
                spaceMusicStarted = SoundManager.Instance.PlayBgm("ToSpace");

            if (rocket.FlightStopped || rocket.Splashed)
            {
                spark.Stop();
                launch.Stop();
            }
            stoppedEngines.Clear();
            foreach (var pair in loops)
            {
                RocketPart engine = pair.Key;
                if (engine != null && engine.isActiveAndEnabled && engine.Ignited && engine.HasFuel
                    && !rocket.FlightStopped && !rocket.Splashed) continue;
                pair.Value.Stop();
                stoppedEngines.Add(engine);
            }
            foreach (RocketPart engine in stoppedEngines)
            {
                loops.Remove(engine);
                StartCoroutine(PlayStopAlert());
            }
            alerts.RemoveAll(handle => !handle.IsValid);
        }

        private IEnumerator PlayStopAlert()
        {
            for (int i = 0; i < 3; i++)
            {
                if (SoundManager.Instance != null)
                    alerts.Add(SoundManager.Instance.PlaySfx("EngineStop"));
                if (i < 2) yield return new WaitForSeconds(Mathf.Max(0.1f, engineStopInterval));
            }
        }

        private void OnDisable()
        {
            if (rocket != null)
            {
                rocket.LaunchStarted -= OnLaunch;
                rocket.LiftoffStarted -= OnLiftoff;
                rocket.SplashdownStarted -= OnSplashdown;
                rocket.ExplosionStarted -= OnExplosion;
            }
            ClearAudio();
        }

        private void ClearAudio()
        {
            StopAllCoroutines();
            spark.Stop();
            launch.Stop();
            foreach (SoundHandle handle in loops.Values) handle.Stop();
            loops.Clear();
            foreach (SoundHandle handle in alerts) handle.Stop();
            alerts.Clear();
        }
    }
}
