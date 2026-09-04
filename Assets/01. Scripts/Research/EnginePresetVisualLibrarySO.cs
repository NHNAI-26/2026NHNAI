using System.Collections.Generic;
using UnityEngine;

namespace Border.Research
{
    [CreateAssetMenu(fileName = "EnginePresetVisualLibrary", menuName = "Research/Engine Preset Visual Library")]
    public sealed class EnginePresetVisualLibrarySO : ScriptableObject
    {
        [SerializeField] private List<GameObject> previewPrefabs = new();

        public IReadOnlyList<GameObject> PreviewPrefabs => previewPrefabs;

        public GameObject GetPreviewPrefab(EnginePresetId presetId)
        {
            int index = (int)presetId;
            if (index < 0 || index >= previewPrefabs.Count)
            {
                return null;
            }

            return previewPrefabs[index];
        }

        public static EnginePresetVisualLibrarySO CreateRuntime(IReadOnlyList<GameObject> runtimePrefabs)
        {
            EnginePresetVisualLibrarySO library = CreateInstance<EnginePresetVisualLibrarySO>();
            library.hideFlags = HideFlags.DontSave;
            library.name = "EnginePresetVisualLibrary_Runtime";
            library.previewPrefabs = new List<GameObject>();

            if (runtimePrefabs == null)
            {
                return library;
            }

            int count = Mathf.Min(runtimePrefabs.Count, ResearchPrototypeModel.MaxEnginePresetCount);
            for (int i = 0; i < count; i++)
            {
                library.previewPrefabs.Add(runtimePrefabs[i]);
            }

            return library;
        }

        private void OnValidate()
        {
            if (previewPrefabs.Count <= ResearchPrototypeModel.MaxEnginePresetCount)
            {
                return;
            }

            previewPrefabs.RemoveRange(
                ResearchPrototypeModel.MaxEnginePresetCount,
                previewPrefabs.Count - ResearchPrototypeModel.MaxEnginePresetCount);
        }
    }
}
