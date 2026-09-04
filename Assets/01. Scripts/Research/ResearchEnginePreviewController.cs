using UnityEngine;

namespace Border.Research
{
    [DisallowMultipleComponent]
    public sealed class ResearchEnginePreviewController : MonoBehaviour
    {
        [SerializeField] private Transform previewRoot;
        [SerializeField] private EnginePresetVisualLibrarySO visualLibrary;
        [SerializeField] private GameObject defaultPreviewPrefab;

        private GameObject activeInstance;
        private EnginePresetId activePresetId;
        private bool hasActivePreset;

        public GameObject ActiveInstance => activeInstance;
        public bool HasActivePreset => hasActivePreset;
        public EnginePresetId ActivePresetId => activePresetId;

        public void Show(EnginePresetId presetId)
        {
            GameObject prefab = ResolvePrefab(presetId);
            if (prefab == null)
            {
                Hide();
                return;
            }

            if (hasActivePreset && activePresetId == presetId && activeInstance != null)
            {
                return;
            }

            DestroyActiveInstance();

            Transform parent = previewRoot != null ? previewRoot : transform;
            activeInstance = Instantiate(prefab, parent, false);
            activeInstance.name = $"{prefab.name}_Preview";
            activeInstance.transform.localPosition = Vector3.zero;
            activeInstance.transform.localRotation = Quaternion.identity;
            activeInstance.transform.localScale = Vector3.one;
            activePresetId = presetId;
            hasActivePreset = true;
        }

        public void Hide()
        {
            DestroyActiveInstance();
            hasActivePreset = false;
        }

        private GameObject ResolvePrefab(EnginePresetId presetId)
        {
            GameObject prefab = visualLibrary != null ? visualLibrary.GetPreviewPrefab(presetId) : null;
            return prefab != null ? prefab : defaultPreviewPrefab;
        }

        private void DestroyActiveInstance()
        {
            if (activeInstance == null)
            {
                return;
            }

            DestroyUnityObject(activeInstance);
            activeInstance = null;
        }

        private static void DestroyUnityObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
