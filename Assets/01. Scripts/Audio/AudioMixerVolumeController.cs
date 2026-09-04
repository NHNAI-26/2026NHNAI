using Border.Core;
using Border.Events;
using UnityEngine;
using UnityEngine.Audio;

namespace Border.Audio
{
    public sealed class AudioMixerVolumeController : MonoBehaviour
    {
        public const string MasterVolumeParameter = "MasterVolume";
        public const string BgmVolumeParameter = "BgmVolume";
        public const string SfxVolumeParameter = "SfxVolume";
        public const float MinimumDecibels = -80f;

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private FloatEventChannelSO changeMasterVolumeEvent;
        [SerializeField] private FloatEventChannelSO changeMusicVolumeEvent;
        [SerializeField] private FloatEventChannelSO changeSfxVolumeEvent;

        private void OnEnable()
        {
            Subscribe(changeMasterVolumeEvent, SetMasterVolume);
            Subscribe(changeMusicVolumeEvent, SetBgmVolume);
            Subscribe(changeSfxVolumeEvent, SetSfxVolume);
        }

        private void OnDisable()
        {
            Unsubscribe(changeMasterVolumeEvent, SetMasterVolume);
            Unsubscribe(changeMusicVolumeEvent, SetBgmVolume);
            Unsubscribe(changeSfxVolumeEvent, SetSfxVolume);
        }

        public void SetMasterVolume(float linear)
        {
            SetLinearVolume(MasterVolumeParameter, linear);
        }

        public void SetBgmVolume(float linear)
        {
            SetLinearVolume(BgmVolumeParameter, linear);
        }

        public void SetSfxVolume(float linear)
        {
            SetLinearVolume(SfxVolumeParameter, linear);
        }

        public static float LinearToDecibels(float linear)
        {
            float clamped = Mathf.Clamp01(linear);
            return clamped <= 0f
                ? MinimumDecibels
                : Mathf.Max(MinimumDecibels, 20f * Mathf.Log10(clamped));
        }

        private void SetLinearVolume(string parameter, float linear)
        {
            if (mixer == null)
            {
                Log.W($"[AudioMixerVolumeController] Mixer is not assigned for '{parameter}'.", this);
                return;
            }

            if (!mixer.SetFloat(parameter, LinearToDecibels(linear)))
            {
                Log.W($"[AudioMixerVolumeController] Mixer parameter '{parameter}' is not exposed.", this);
            }
        }

        private static void Subscribe(FloatEventChannelSO channel, UnityEngine.Events.UnityAction<float> listener)
        {
            if (channel != null) channel.OnEventRaised += listener;
        }

        private static void Unsubscribe(FloatEventChannelSO channel, UnityEngine.Events.UnityAction<float> listener)
        {
            if (channel != null) channel.OnEventRaised -= listener;
        }
    }
}
