using Border.Core;
using UnityEngine;

namespace Border.Audio
{
    public sealed class SoundManager : MonoBehaviour
    {
        private const string SteamAudioSpatializerName = "Steam Audio Spatializer";

        [SerializeField] private SoundDatabaseSO database;
        [SerializeField] private BgmPlayer bgmPlayer;
        [SerializeField] private SfxPool sfxPool;
        [SerializeField] private AudioMixerVolumeController volumeController;

        private bool warnedMissingSpatializer;

        public static SoundManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (GetComponent<Border.UI.UISelectableSoundHook>() == null)
                gameObject.AddComponent<Border.UI.UISelectableSoundHook>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool PlayBgm(string id, float fadeSeconds = 1f)
        {
            if (!TryGetBgm(id, out BgmEntry entry))
            {
                return false;
            }

            if (bgmPlayer == null || !bgmPlayer.IsConfigured)
            {
                Log.W("[SoundManager] BGM player or its two AudioSources are not configured.", this);
                return false;
            }

            return bgmPlayer.Play(entry, fadeSeconds);
        }

        public void StopBgm(float fadeSeconds = 1f)
        {
            if (bgmPlayer == null)
            {
                Log.W("[SoundManager] BGM player is not configured.", this);
                return;
            }

            bgmPlayer.Stop(fadeSeconds);
        }

        public SoundHandle PlaySfx(string id, float fadeInSeconds = 0f)
        {
            if (!TryGetSfx(id, out SfxEntry entry))
            {
                return SoundHandle.Invalid;
            }

            if (entry.UseSpatialAudio)
            {
                Log.W($"[SoundManager] Spatial SFX '{id}' requires a position or Transform.", this);
                return SoundHandle.Invalid;
            }

            return PlaySfxEntry(entry, Vector3.zero, null, false, fadeInSeconds);
        }

        public SoundHandle PlaySfxAt(string id, Vector3 position, float fadeInSeconds = 0f)
        {
            if (!TryGetSfx(id, out SfxEntry entry))
            {
                return SoundHandle.Invalid;
            }

            WarnIfSpatializerMissing(entry);
            return PlaySfxEntry(entry, position, null, false, fadeInSeconds);
        }

        public SoundHandle PlaySfxAttached(string id, Transform target, float fadeInSeconds = 0f)
        {
            if (target == null)
            {
                Log.W($"[SoundManager] SFX '{id}' cannot attach to a null Transform.", this);
                return SoundHandle.Invalid;
            }

            if (!TryGetSfx(id, out SfxEntry entry))
            {
                return SoundHandle.Invalid;
            }

            WarnIfSpatializerMissing(entry);
            return PlaySfxEntry(entry, target.position, target, true, fadeInSeconds);
        }

        public void SetMasterVolume(float linear)
        {
            if (volumeController != null) volumeController.SetMasterVolume(linear);
            else Log.W("[SoundManager] Volume controller is not configured.", this);
        }

        public void SetBgmVolume(float linear)
        {
            if (volumeController != null) volumeController.SetBgmVolume(linear);
            else Log.W("[SoundManager] Volume controller is not configured.", this);
        }

        public void SetSfxVolume(float linear)
        {
            if (volumeController != null) volumeController.SetSfxVolume(linear);
            else Log.W("[SoundManager] Volume controller is not configured.", this);
        }

        private bool TryGetBgm(string id, out BgmEntry entry)
        {
            entry = null;
            if (database != null && database.TryGetBgm(id, out entry))
            {
                return true;
            }

            Log.W($"[SoundManager] Unknown or invalid BGM ID '{id}'.", this);
            return false;
        }

        private bool TryGetSfx(string id, out SfxEntry entry)
        {
            entry = null;
            if (database != null && database.TryGetSfx(id, out entry))
            {
                return true;
            }

            Log.W($"[SoundManager] Unknown or invalid SFX ID '{id}'.", this);
            return false;
        }

        private SoundHandle PlaySfxEntry(
            SfxEntry entry,
            Vector3 position,
            Transform target,
            bool follow,
            float fadeInSeconds)
        {
            if (sfxPool == null)
            {
                Log.W("[SoundManager] SFX pool is not configured.", this);
                return SoundHandle.Invalid;
            }

            return sfxPool.Play(entry, position, target, follow, fadeInSeconds);
        }

        private void WarnIfSpatializerMissing(SfxEntry entry)
        {
            if (!entry.UseSpatialAudio || warnedMissingSpatializer
                || string.Equals(AudioSettings.GetSpatializerPluginName(), SteamAudioSpatializerName))
            {
                return;
            }

            warnedMissingSpatializer = true;
            Log.W("[SoundManager] Steam Audio Spatializer is not selected; spatial playback will use Unity's available path.", this);
        }
    }
}
