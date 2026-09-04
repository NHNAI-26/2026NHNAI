using System.Collections.Generic;
using UnityEngine;

namespace Border.Research
{
    [CreateAssetMenu(fileName = "EnginePresetVisualLibrary", menuName = "Research/Engine Preset Visual Library")]
    public sealed class EnginePresetVisualLibrarySO : ScriptableObject
    {
        [SerializeField] private List<GameObject> previewPrefabs = new();
        [SerializeField] private List<EngineArchetypePreviewPrefab> archetypePreviewPrefabs = new();

        public IReadOnlyList<GameObject> PreviewPrefabs => previewPrefabs;
        public IReadOnlyList<EngineArchetypePreviewPrefab> ArchetypePreviewPrefabs => archetypePreviewPrefabs;

        public GameObject GetPreviewPrefab(EnginePresetId presetId)
        {
            int index = (int)presetId;
            if (index < 0 || index >= previewPrefabs.Count)
            {
                return null;
            }

            return previewPrefabs[index];
        }

        public GameObject GetPreviewPrefab(EngineVisualArchetype archetype)
        {
            for (int i = 0; i < archetypePreviewPrefabs.Count; i++)
            {
                EngineArchetypePreviewPrefab entry = archetypePreviewPrefabs[i];
                if (entry.Archetype == archetype)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }

        public GameObject GetPreviewPrefab(EnginePresetId presetId, EngineVisualArchetype archetype)
        {
            GameObject archetypePrefab = GetPreviewPrefab(archetype);
            return archetypePrefab != null ? archetypePrefab : GetPreviewPrefab(presetId);
        }

        public void SetArchetypePreviewPrefab(EngineVisualArchetype archetype, GameObject prefab)
        {
            for (int i = 0; i < archetypePreviewPrefabs.Count; i++)
            {
                if (archetypePreviewPrefabs[i].Archetype == archetype)
                {
                    archetypePreviewPrefabs[i] = new EngineArchetypePreviewPrefab(archetype, prefab);
                    return;
                }
            }

            archetypePreviewPrefabs.Add(new EngineArchetypePreviewPrefab(archetype, prefab));
        }

        public static EnginePresetVisualLibrarySO CreateRuntime(
            IReadOnlyList<GameObject> runtimePrefabs,
            IReadOnlyList<GameObject> runtimeArchetypePrefabs = null)
        {
            EnginePresetVisualLibrarySO library = CreateInstance<EnginePresetVisualLibrarySO>();
            library.hideFlags = HideFlags.DontSave;
            library.name = "EnginePresetVisualLibrary_Runtime";
            library.previewPrefabs = new List<GameObject>();
            library.archetypePreviewPrefabs = new List<EngineArchetypePreviewPrefab>();

            if (runtimePrefabs != null)
            {
                int count = Mathf.Min(runtimePrefabs.Count, ResearchPrototypeModel.MaxEnginePresetCount);
                for (int i = 0; i < count; i++)
                {
                    library.previewPrefabs.Add(runtimePrefabs[i]);
                }
            }

            if (runtimeArchetypePrefabs == null)
            {
                return library;
            }

            int archetypeCount = Mathf.Min(runtimeArchetypePrefabs.Count, EngineVisualArchetypeCount);
            for (int i = 0; i < archetypeCount; i++)
            {
                library.archetypePreviewPrefabs.Add(new EngineArchetypePreviewPrefab((EngineVisualArchetype)i, runtimeArchetypePrefabs[i]));
            }

            return library;
        }

        private void OnValidate()
        {
            if (previewPrefabs.Count > ResearchPrototypeModel.MaxEnginePresetCount)
            {
                previewPrefabs.RemoveRange(
                    ResearchPrototypeModel.MaxEnginePresetCount,
                    previewPrefabs.Count - ResearchPrototypeModel.MaxEnginePresetCount);
            }

            if (archetypePreviewPrefabs.Count > EngineVisualArchetypeCount)
            {
                archetypePreviewPrefabs.RemoveRange(
                    EngineVisualArchetypeCount,
                    archetypePreviewPrefabs.Count - EngineVisualArchetypeCount);
            }
        }

        private const int EngineVisualArchetypeCount = 5;

        [System.Serializable]
        public struct EngineArchetypePreviewPrefab
        {
            public EngineArchetypePreviewPrefab(EngineVisualArchetype archetype, GameObject prefab)
            {
                Archetype = archetype;
                Prefab = prefab;
            }

            public EngineVisualArchetype Archetype;
            public GameObject Prefab;
        }
    }
}
