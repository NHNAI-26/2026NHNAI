using System;
using System.Collections.Generic;
using UnityEngine;

namespace Border.Audio
{
    [Serializable]
    public sealed class BgmEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(-3f, 3f)] private float pitch = 1f;
        [SerializeField] private bool loop;

        public string Id => id;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float Pitch => pitch;
        public bool Loop => loop;
        public bool IsValid => !string.IsNullOrWhiteSpace(id) && clip != null;
        public BgmEntry()
        {
        }

        public BgmEntry(string id, AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            this.id = id;
            this.clip = clip;
            this.volume = volume;
            this.pitch = pitch;
            this.loop = loop;
        }
    }

    [Serializable]
    public sealed class SfxEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(-3f, 3f)] private float pitch = 1f;
        [SerializeField] private bool loop;
        [SerializeField] private bool useSpatialAudio;
        [SerializeField, Min(0f)] private float minDistance = 1f;
        [SerializeField, Min(0f)] private float maxDistance = 50f;

        public string Id => id;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float Pitch => pitch;
        public bool Loop => loop;
        public bool UseSpatialAudio => useSpatialAudio;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public bool HasValidDistance => IsFinite(minDistance) && IsFinite(maxDistance)
            && minDistance >= 0f && maxDistance >= minDistance;
        public bool IsValid => !string.IsNullOrWhiteSpace(id) && clip != null && HasValidDistance;

        public SfxEntry()
        {
        }

        public SfxEntry(
            string id,
            AudioClip clip,
            float volume = 1f,
            float pitch = 1f,
            bool loop = false,
            bool useSpatialAudio = false,
            float minDistance = 1f,
            float maxDistance = 50f)
        {
            this.id = id;
            this.clip = clip;
            this.volume = volume;
            this.pitch = pitch;
            this.loop = loop;
            this.useSpatialAudio = useSpatialAudio;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "Border/Audio/Sound Database")]
    public sealed class SoundDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<BgmEntry> bgmEntries = new();
        [SerializeField] private List<SfxEntry> sfxEntries = new();

        [NonSerialized] private Dictionary<string, BgmEntry> bgmLookup;
        [NonSerialized] private Dictionary<string, SfxEntry> sfxLookup;
        public IReadOnlyList<BgmEntry> BgmEntries => bgmEntries ??= new List<BgmEntry>();
        public IReadOnlyList<SfxEntry> SfxEntries => sfxEntries ??= new List<SfxEntry>();

        public bool TryGetBgm(string id, out BgmEntry entry)
        {
            EnsureLookup();
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && bgmLookup.TryGetValue(id, out entry);
        }

        public bool TryGetSfx(string id, out SfxEntry entry)
        {
            EnsureLookup();
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && sfxLookup.TryGetValue(id, out entry);
        }

        public void RebuildLookup()
        {
            bgmLookup = new Dictionary<string, BgmEntry>(StringComparer.Ordinal);
            sfxLookup = new Dictionary<string, SfxEntry>(StringComparer.Ordinal);

            if (bgmEntries != null)
            {
                foreach (BgmEntry entry in bgmEntries)
                {
                    if (entry != null && entry.IsValid && !bgmLookup.ContainsKey(entry.Id))
                    {
                        bgmLookup.Add(entry.Id, entry);
                    }
                }
            }

            if (sfxEntries != null)
            {
                foreach (SfxEntry entry in sfxEntries)
                {
                    if (entry != null && entry.IsValid && !sfxLookup.ContainsKey(entry.Id))
                    {
                        sfxLookup.Add(entry.Id, entry);
                    }
                }
            }
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        private void EnsureLookup()
        {
            if (bgmLookup == null || sfxLookup == null)
            {
                RebuildLookup();
            }
        }
    }
}
