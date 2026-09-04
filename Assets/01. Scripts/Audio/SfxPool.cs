using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

namespace Border.Audio
{
    public sealed class SfxPool : MonoBehaviour
    {
        private const int DefaultPrewarmCount = 16;
        private const int DefaultMaxInactive = 32;

        [SerializeField] private AudioMixerGroup outputMixerGroup;
        [SerializeField] private Transform voiceRoot;
        [SerializeField, Min(0)] private int prewarmCount = DefaultPrewarmCount;
        [SerializeField, Min(1)] private int maxInactive = DefaultMaxInactive;

        private ObjectPool<PooledSfxVoice> pool;
        private int createdCount;

        public int CountAll => pool?.CountAll ?? 0;
        public int CountActive => pool?.CountActive ?? 0;
        public int CountInactive => pool?.CountInactive ?? 0;

        private void Awake()
        {
            EnsurePool();
        }

        private void OnDestroy()
        {
            pool?.Clear();
            pool = null;
        }

        internal SoundHandle Play(
            SfxEntry entry,
            Vector3 position,
            Transform target,
            bool follow,
            float fadeInSeconds)
        {
            EnsurePool();
            PooledSfxVoice voice = pool.Get();
            return voice.Play(entry, outputMixerGroup, position, target, follow, Mathf.Max(0f, fadeInSeconds));
        }

        internal void Release(PooledSfxVoice voice)
        {
            if (pool != null && voice != null && voice.IsActive)
            {
                pool.Release(voice);
            }
        }

        private void EnsurePool()
        {
            if (pool != null)
            {
                return;
            }

            Transform root = voiceRoot != null ? voiceRoot : transform;
            pool = new ObjectPool<PooledSfxVoice>(
                CreateVoice,
                voice => voice.OnTaken(root),
                voice => voice.OnReturned(root),
                DestroyVoice,
                true,
                Mathf.Max(1, prewarmCount),
                Mathf.Max(1, maxInactive));

            int count = Mathf.Min(Mathf.Max(0, prewarmCount), Mathf.Max(1, maxInactive));
            var voices = new List<PooledSfxVoice>(count);
            for (int index = 0; index < count; index++)
            {
                voices.Add(pool.Get());
            }

            foreach (PooledSfxVoice voice in voices)
            {
                pool.Release(voice);
            }
        }

        private PooledSfxVoice CreateVoice()
        {
            Transform root = voiceRoot != null ? voiceRoot : transform;
            var voiceObject = new GameObject($"SFX Voice {createdCount++}");
            voiceObject.transform.SetParent(root, false);
            AudioSource source = voiceObject.AddComponent<AudioSource>();
            PooledSfxVoice voice = voiceObject.AddComponent<PooledSfxVoice>();
            voice.Initialize(this, source);
            voiceObject.SetActive(false);
            return voice;
        }

        private static void DestroyVoice(PooledSfxVoice voice)
        {
            if (voice != null)
            {
                Destroy(voice.gameObject);
            }
        }
    }
}
