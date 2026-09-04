using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace Border.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PooledSfxVoice : MonoBehaviour
    {
        private AudioSource source;
        private SfxPool owner;
        private Transform followTarget;
        private Tween fadeTween;
        private int generation;
        private bool active;
        private bool followsTarget;
        private bool stopping;

        internal bool IsActive => active;

        private void Awake()
        {
            source ??= GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            if (followsTarget)
            {
                if (followTarget == null)
                {
                    ReleaseNow();
                    return;
                }

                transform.position = followTarget.position;
            }

            if (!source.loop && !source.isPlaying)
            {
                ReleaseNow();
            }
        }

        private void OnDestroy()
        {
            KillFade();
        }

        internal void Initialize(SfxPool pool, AudioSource audioSource)
        {
            owner = pool;
            source = audioSource;
            ResetSource();
        }

        internal void OnTaken(Transform root)
        {
            gameObject.SetActive(true);
            transform.SetParent(root, false);
            ResetSource();
            generation = generation == int.MaxValue ? 1 : generation + 1;
            active = true;
        }

        internal void OnReturned(Transform root)
        {
            active = false;
            ResetSource();
            transform.SetParent(root, false);
            transform.localPosition = Vector3.zero;
            gameObject.SetActive(false);
        }

        internal SoundHandle Play(
            SfxEntry entry,
            AudioMixerGroup output,
            Vector3 position,
            Transform target,
            bool follow,
            float fadeInSeconds)
        {
            transform.position = target != null ? target.position : position;
            followTarget = target;
            followsTarget = follow;

            source.clip = entry.Clip;
            source.outputAudioMixerGroup = output;
            source.pitch = Mathf.Clamp(entry.Pitch, -3f, 3f);
            source.loop = entry.Loop;
            source.spatialBlend = entry.UseSpatialAudio ? 1f : 0f;
            source.spatialize = entry.UseSpatialAudio;
            source.minDistance = entry.MinDistance;
            source.maxDistance = entry.MaxDistance;

            float targetVolume = Mathf.Clamp01(entry.Volume);
            source.volume = fadeInSeconds > 0f ? 0f : targetVolume;
            source.Play();
            if (fadeInSeconds > 0f)
            {
                fadeTween = CreateVolumeTween(targetVolume, fadeInSeconds, null);
            }

            return new SoundHandle(this, generation);
        }

        internal bool IsGenerationValid(int token)
        {
            return active && token == generation;
        }

        internal bool IsPlaying(int token)
        {
            return IsGenerationValid(token) && source != null && source.isPlaying;
        }

        internal void Stop(int token, float fadeSeconds)
        {
            if (!IsGenerationValid(token))
            {
                return;
            }

            KillFade();
            if (fadeSeconds <= 0f || !source.isPlaying)
            {
                ReleaseNow();
                return;
            }

            stopping = true;
            fadeTween = CreateVolumeTween(0f, fadeSeconds, () =>
            {
                if (IsGenerationValid(token))
                {
                    ReleaseNow();
                }
            });
        }

        internal void SetVolume(int token, float volume)
        {
            if (!IsGenerationValid(token) || stopping) return;
            KillFade();
            source.volume = Mathf.Clamp01(volume);
        }

        internal void SetPitch(int token, float pitch)
        {
            if (IsGenerationValid(token)) source.pitch = Mathf.Clamp(pitch, -3f, 3f);
        }

        internal void SetLoop(int token, bool loop)
        {
            if (IsGenerationValid(token)) source.loop = loop;
        }

        private Tween CreateVolumeTween(float target, float duration, TweenCallback onComplete)
        {
            Tween tween = DOTween.To(() => source.volume, value => source.volume = value, target, duration);
            tween.SetUpdate(true);
            if (onComplete != null)
            {
                tween.OnComplete(onComplete);
            }

            return tween;
        }

        private void ReleaseNow()
        {
            if (!active)
            {
                return;
            }

            if (owner != null)
            {
                owner.Release(this);
            }
            else
            {
                OnReturned(transform.parent);
            }
        }

        private void ResetSource()
        {
            KillFade();
            followTarget = null;
            followsTarget = false;
            stopping = false;
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.playOnAwake = false;
            source.volume = 1f;
            source.pitch = 1f;
            source.loop = false;
            source.spatialBlend = 0f;
            source.spatialize = false;
            source.minDistance = 1f;
            source.maxDistance = 500f;
        }

        private void KillFade()
        {
            if (fadeTween == null)
            {
                return;
            }

            fadeTween.Kill();
            fadeTween = null;
        }
    }
}
