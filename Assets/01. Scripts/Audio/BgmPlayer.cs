using DG.Tweening;
using UnityEngine;

namespace Border.Audio
{
    public sealed class BgmPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource sourceA;
        [SerializeField] private AudioSource sourceB;

        private AudioSource currentSource;
        private Tween tweenA;
        private Tween tweenB;
        private string currentId;
        private string sourceAId;
        private string sourceBId;

        public bool IsConfigured => sourceA != null && sourceB != null && sourceA != sourceB;
        public bool IsPlaying => currentSource != null && currentSource.isPlaying;
        public string CurrentId => IsPlaying ? currentId : null;
        public AudioSource CurrentSource => IsPlaying ? currentSource : null;

        private void OnDestroy()
        {
            KillTweens();
        }

        internal bool Play(BgmEntry entry, float fadeSeconds)
        {
            if (!IsConfigured || entry == null || !entry.IsValid)
            {
                return false;
            }

            if (string.Equals(currentId, entry.Id, System.StringComparison.Ordinal) && IsPlaying)
            {
                return true;
            }

            float duration = Mathf.Max(0f, fadeSeconds);
            AudioSource incoming = GetPlayingSource(entry.Id);
            bool reuseExisting = incoming != null;
            AudioSource outgoing;
            if (reuseExisting)
            {
                outgoing = incoming == sourceA ? sourceB : sourceA;
            }
            else
            {
                outgoing = GetLouderPlayingSource();
                incoming = outgoing == sourceA ? sourceB : sourceA;
            }

            KillTweens();
            if (!reuseExisting)
            {
                StopAndClear(incoming);
                Configure(incoming, entry, duration > 0f ? 0f : entry.Volume);
                if (incoming == sourceA) sourceAId = entry.Id;
                else sourceBId = entry.Id;
                incoming.Play();
            }

            if (outgoing != null && outgoing != incoming) StopSource(outgoing, duration);

            if (duration > 0f)
            {
                SetTween(incoming, CreateVolumeTween(incoming, Mathf.Clamp01(entry.Volume), duration, null));
            }
            else
            {
                incoming.volume = Mathf.Clamp01(entry.Volume);
            }

            currentSource = incoming;
            currentId = entry.Id;
            return true;
        }

        internal void Stop(float fadeSeconds)
        {
            if (!IsConfigured)
            {
                return;
            }

            float duration = Mathf.Max(0f, fadeSeconds);
            KillTweens();
            currentSource = null;
            currentId = null;
            StopSource(sourceA, duration);
            StopSource(sourceB, duration);
        }

        private void StopSource(AudioSource source, float duration)
        {
            if (!source.isPlaying)
            {
                StopAndClear(source);
                return;
            }

            if (duration <= 0f)
            {
                StopAndClear(source);
            }
            else
            {
                SetTween(source, CreateVolumeTween(source, 0f, duration, () => StopAndClear(source)));
            }
        }

        private AudioSource GetLouderPlayingSource()
        {
            bool aPlaying = sourceA.isPlaying;
            bool bPlaying = sourceB.isPlaying;
            if (aPlaying && bPlaying) return sourceA.volume >= sourceB.volume ? sourceA : sourceB;
            if (aPlaying) return sourceA;
            if (bPlaying) return sourceB;
            return null;
        }

        private AudioSource GetPlayingSource(string id)
        {
            if (sourceA.isPlaying && string.Equals(sourceAId, id, System.StringComparison.Ordinal)) return sourceA;
            if (sourceB.isPlaying && string.Equals(sourceBId, id, System.StringComparison.Ordinal)) return sourceB;
            return null;
        }

        private static void Configure(AudioSource source, BgmEntry entry, float volume)
        {
            source.clip = entry.Clip;
            source.playOnAwake = false;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(entry.Pitch, -3f, 3f);
            source.loop = entry.Loop;
            source.spatialBlend = 0f;
            source.spatialize = false;
        }

        private static Tween CreateVolumeTween(AudioSource source, float target, float duration, TweenCallback onComplete)
        {
            Tween tween = DOTween.To(() => source.volume, value => source.volume = value, target, duration);
            tween.SetUpdate(true);
            if (onComplete != null) tween.OnComplete(onComplete);
            return tween;
        }

        private void SetTween(AudioSource source, Tween tween)
        {
            if (source == sourceA) tweenA = tween;
            else tweenB = tween;
        }

        private void KillTweens()
        {
            KillTween(ref tweenA);
            KillTween(ref tweenB);
        }

        private static void KillTween(ref Tween tween)
        {
            if (tween == null) return;
            tween.Kill();
            tween = null;
        }

        private void StopAndClear(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            if (source == sourceA) sourceAId = null;
            else if (source == sourceB) sourceBId = null;
        }
    }
}
