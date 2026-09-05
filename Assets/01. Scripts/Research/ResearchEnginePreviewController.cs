using System;
using System.Collections.Generic;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace Border.Research
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ResearchEnginePreviewController : MonoBehaviour
    {
        private const string Uber3DObjectShaderName = "Shader/Uber/3D Object";
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int LightingModeId = Shader.PropertyToID("_LightingMode");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int CastShadowsId = Shader.PropertyToID("_CastShadows");
        private static readonly int ReceiveShadowsId = Shader.PropertyToID("_ReceiveShadows");
        private static readonly int HologramEnabledId = Shader.PropertyToID("_HologramEnabled");
        private static readonly int HologramColorId = Shader.PropertyToID("_HologramColor");
        private static readonly int HologramOpacityId = Shader.PropertyToID("_HologramOpacity");
        private static readonly int HologramFresnelPowerId = Shader.PropertyToID("_HologramFresnelPower");
        private static readonly int HologramFresnelIntensityId = Shader.PropertyToID("_HologramFresnelIntensity");
        private static readonly int HologramScanlineDensityId = Shader.PropertyToID("_HologramScanlineDensity");
        private static readonly int HologramScanlineSpeedId = Shader.PropertyToID("_HologramScanlineSpeed");
        private static readonly int HologramScanlineWidthId = Shader.PropertyToID("_HologramScanlineWidth");
        private static readonly int HologramScanlineIntensityId = Shader.PropertyToID("_HologramScanlineIntensity");
        private static readonly int HologramNoiseScaleId = Shader.PropertyToID("_HologramNoiseScale");
        private static readonly int HologramNoiseStrengthId = Shader.PropertyToID("_HologramNoiseStrength");
        private static readonly int HologramNoiseSpeedId = Shader.PropertyToID("_HologramNoiseSpeed");

        [SerializeField] private Transform previewRoot;
        [SerializeField] private EnginePresetVisualLibrarySO visualLibrary;
        [SerializeField] private GameObject defaultPreviewPrefab;
        [SerializeField] private Material hologramMaterial;
        [SerializeField] private Shader hologramFallbackShader;
        [SerializeField] private Color hologramColor = new(0.16f, 0.9f, 1f, 0.72f);
        [SerializeField] private Color hologramEmissionColor = new(0.35f, 1.2f, 1.4f, 1f);
        [SerializeField, Min(0f)] private float materializeDuration = 0.7f;
        [SerializeField, Min(0.05f)] private float hologramPulseDuration = 1.2f;
        [SerializeField] private bool normalizePreviewBounds = true;
        [SerializeField, Min(0.01f)] private float targetPreviewHeight = 1.25f;
        [SerializeField] private float targetPreviewGroundY;
        [SerializeField] private Vector3 previewLocalEulerAngles = new(-90f, 0f, 0f);
        [SerializeField] private float engineSpinDegreesPerSecond = 16f;
        [SerializeField] private bool showEditModePreview = true;
        [SerializeField] private EnginePresetId editModePreviewPresetId = EnginePresetId.Engine01;
        [SerializeField] private EngineVisualArchetype editModePreviewArchetype = EngineVisualArchetype.Balanced;

        private GameObject activeInstance;
        private EnginePresetId activePresetId;
        private EngineVisualArchetype activeArchetype;
        private bool hasActivePreset;
        private Sequence activePulseSequence;
        private Sequence activeMaterializeSequence;
        private GameObject retiringInstance;
#if UNITY_EDITOR
        private bool editModePreviewRefreshQueued;
#endif

        public Transform PreviewRoot => previewRoot != null ? previewRoot : transform;
        public GameObject ActiveInstance => activeInstance;
        public bool HasActivePreset => hasActivePreset;
        public EnginePresetId ActivePresetId => activePresetId;
        public EngineVisualArchetype ActiveArchetype => activeArchetype;
        public bool IsMaterializing => activeMaterializeSequence != null && activeMaterializeSequence.IsActive();

        public void Show(EnginePresetId presetId)
        {
            ShowHologram(presetId, EngineVisualArchetype.Balanced);
        }

        public void ShowHologram(EnginePresetId presetId, EngineVisualArchetype archetype)
        {
            GameObject prefab = ResolvePrefab(presetId, archetype);
            if (prefab == null)
            {
                Hide();
                return;
            }

            if (hasActivePreset && activePresetId == presetId && activeArchetype == archetype && activeInstance != null)
            {
                return;
            }

            KillActiveTweens();
            DestroyActiveInstance();

            Transform parent = previewRoot != null ? previewRoot : transform;
            DestroyOrphanPreviewChildren(parent);
            activeInstance = Instantiate(prefab, parent, false);
            activeInstance.name = $"{prefab.name}_HologramPreview";
            ResetPreviewTransform(activeInstance.transform);
            NormalizePreviewInstance(activeInstance);
            activePresetId = presetId;
            activeArchetype = archetype;
            hasActivePreset = true;

            if (!Application.isPlaying)
            {
                ApplyDontSaveInEditor(activeInstance);
            }

            ApplyHologramVisuals(activeInstance, hologramColor);
        }

        public void PlayMaterialize(EnginePresetId presetId, EngineVisualArchetype archetype, Action onComplete = null)
        {
            GameObject prefab = ResolvePrefab(presetId, archetype);
            if (prefab == null)
            {
                Hide();
                onComplete?.Invoke();
                return;
            }

            KillActiveTweens();

            if (activeInstance == null || !hasActivePreset || activePresetId != presetId || activeArchetype != archetype)
            {
                ShowHologram(presetId, archetype);
                KillPulse();
            }

            Transform parent = previewRoot != null ? previewRoot : transform;
            GameObject hologramInstance = activeInstance;
            DestroyOrphanPreviewChildren(parent);
            GameObject solidInstance = Instantiate(prefab, parent, false);
            solidInstance.name = $"{prefab.name}_Preview";
            if (hologramInstance != null)
            {
                solidInstance.transform.SetLocalPositionAndRotation(hologramInstance.transform.localPosition, hologramInstance.transform.localRotation);
                solidInstance.transform.localScale = hologramInstance.transform.localScale;
            }
            else
            {
                ResetPreviewTransform(solidInstance.transform);
                NormalizePreviewInstance(solidInstance);
            }

            Vector3 solidTargetScale = solidInstance.transform.localScale;
            Vector3 hologramTargetScale = hologramInstance != null ? hologramInstance.transform.localScale * 1.08f : solidTargetScale * 1.08f;
            solidInstance.transform.localScale = solidTargetScale * 0.86f;

            MaterialColorBinding[] solidBindings = PrepareAlphaBindings(solidInstance, 0.1f);
            MaterialColorBinding[] hologramBindings = PrepareAlphaBindings(hologramInstance, hologramColor.a);

            activeInstance = solidInstance;
            retiringInstance = hologramInstance;
            activePresetId = presetId;
            activeArchetype = archetype;
            hasActivePreset = true;

            if (materializeDuration <= 0f)
            {
                ApplyTargetColors(solidBindings);
                DestroyUnityObject(hologramInstance);
                retiringInstance = null;
                onComplete?.Invoke();
                return;
            }

            activeMaterializeSequence = DOTween.Sequence()
                .SetTarget(this)
                .Join(solidInstance.transform.DOScale(solidTargetScale, materializeDuration).SetEase(Ease.OutCubic));
            if (hologramInstance != null)
            {
                activeMaterializeSequence.Join(hologramInstance.transform.DOScale(hologramTargetScale, materializeDuration).SetEase(Ease.InCubic));
            }

            AddMaterialColorTweens(activeMaterializeSequence, solidBindings, materializeDuration);
            AddMaterialAlphaTweens(activeMaterializeSequence, hologramBindings, 0f, materializeDuration);
            activeMaterializeSequence.OnComplete(() =>
            {
                if (hologramInstance != null)
                {
                    DestroyUnityObject(hologramInstance);
                }

                retiringInstance = null;
                activeMaterializeSequence = null;
                onComplete?.Invoke();
            });
        }

        public void Hide()
        {
            KillActiveTweens();
            DestroyActiveInstance();
            hasActivePreset = false;
            if (Application.isPlaying && previewRoot != null)
            {
                previewRoot.localRotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            // Idle spin for the engine room preview. previewRoot is the pivot because
            // NormalizePreviewInstance centers the model bounds on its origin.
            if (!Application.isPlaying || previewRoot == null || activeInstance == null || engineSpinDegreesPerSecond == 0f)
            {
                return;
            }

            previewRoot.Rotate(0f, engineSpinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
        }

        private void OnDisable()
        {
            KillActiveTweens();
            if (!Application.isPlaying)
            {
                DestroyActiveInstance();
                hasActivePreset = false;
            }
        }

        private void OnValidate()
        {
            QueueEditModePreviewRefresh();
        }

        private void OnEnable()
        {
            QueueEditModePreviewRefresh();
        }

        private void Start()
        {
            if (Application.isPlaying && showEditModePreview && activeInstance == null)
            {
                ShowHologram(editModePreviewPresetId, editModePreviewArchetype);
            }
        }

        private void RefreshEditModePreview()
        {
            if (!showEditModePreview)
            {
                Hide();
                return;
            }

            ShowHologram(editModePreviewPresetId, editModePreviewArchetype);
        }

        private void QueueEditModePreviewRefresh()
        {
#if UNITY_EDITOR
            if (Application.isPlaying || editModePreviewRefreshQueued)
            {
                return;
            }

            editModePreviewRefreshQueued = true;
            EditorApplication.delayCall += RefreshQueuedEditModePreview;
#endif
        }

#if UNITY_EDITOR
        private void RefreshQueuedEditModePreview()
        {
            editModePreviewRefreshQueued = false;
            if (this == null || Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            RefreshEditModePreview();
        }
#endif

        private GameObject ResolvePrefab(EnginePresetId presetId)
        {
            GameObject prefab = visualLibrary != null ? visualLibrary.GetPreviewPrefab(presetId) : null;
            return prefab != null ? prefab : defaultPreviewPrefab;
        }

        private GameObject ResolvePrefab(EnginePresetId presetId, EngineVisualArchetype archetype)
        {
            GameObject prefab = visualLibrary != null ? visualLibrary.GetPreviewPrefab(presetId, archetype) : null;
            return prefab != null ? prefab : defaultPreviewPrefab;
        }

        private void DestroyActiveInstance()
        {
            if (activeInstance != null)
            {
                DestroyUnityObject(activeInstance);
                activeInstance = null;
            }

            if (retiringInstance != null)
            {
                DestroyUnityObject(retiringInstance);
                retiringInstance = null;
            }
        }

        private void DestroyOrphanPreviewChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (child == activeInstance || child == retiringInstance)
                {
                    continue;
                }

                if (IsPreviewInstanceName(child.name))
                {
                    DestroyUnityObject(child);
                }
            }
        }

        private static bool IsPreviewInstanceName(string objectName)
        {
            return objectName.EndsWith("_HologramPreview", StringComparison.Ordinal)
                || objectName.EndsWith("_Preview", StringComparison.Ordinal);
        }

        private void KillActiveTweens()
        {
            KillPulse();
            if (activeMaterializeSequence != null)
            {
                activeMaterializeSequence.Kill();
                activeMaterializeSequence = null;
            }
        }

        private void KillPulse()
        {
            if (activePulseSequence != null)
            {
                activePulseSequence.Kill();
                activePulseSequence = null;
            }
        }

        private void StartHologramPulse(GameObject instance)
        {
            if (instance == null || hologramPulseDuration <= 0f)
            {
                return;
            }

            float halfDuration = hologramPulseDuration * 0.5f;
            Material[] materials = CollectHologramMaterials(instance);
            if (materials.Length == 0)
            {
                return;
            }

            activePulseSequence = DOTween.Sequence().SetTarget(instance);
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || !material.HasProperty(HologramScanlineIntensityId))
                {
                    continue;
                }

                float baseIntensity = material.GetFloat(HologramScanlineIntensityId);
                float peakIntensity = baseIntensity * 1.28f;
                activePulseSequence.Join(DOTween.To(
                    () => material.GetFloat(HologramScanlineIntensityId),
                    value => material.SetFloat(HologramScanlineIntensityId, value),
                    peakIntensity,
                    halfDuration).SetEase(Ease.InOutSine));
                activePulseSequence.Insert(halfDuration, DOTween.To(
                    () => material.GetFloat(HologramScanlineIntensityId),
                    value => material.SetFloat(HologramScanlineIntensityId, value),
                    baseIntensity,
                    halfDuration).SetEase(Ease.InOutSine));
            }

            activePulseSequence.SetLoops(-1, LoopType.Restart);
        }

        private void NormalizePreviewInstance(GameObject instance)
        {
            if (!normalizePreviewBounds || instance == null)
            {
                return;
            }

            Transform parent = previewRoot != null ? previewRoot : transform;
            if (!TryCalculateLocalRendererBounds(instance, parent, out Bounds localBounds))
            {
                return;
            }

            if (localBounds.size.y > 0.001f && targetPreviewHeight > 0.001f)
            {
                float scaleFactor = targetPreviewHeight / localBounds.size.y;
                instance.transform.localScale *= scaleFactor;
                if (!TryCalculateLocalRendererBounds(instance, parent, out localBounds))
                {
                    return;
                }
            }

            Vector3 offset = new(
                -localBounds.center.x,
                targetPreviewGroundY - localBounds.min.y,
                -localBounds.center.z);
            instance.transform.localPosition += offset;
        }

        private void ResetPreviewTransform(Transform target)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.Euler(previewLocalEulerAngles);
            target.localScale = Vector3.one;
        }

        private static bool TryCalculateLocalRendererBounds(GameObject instance, Transform parent, out Bounds localBounds)
        {
            localBounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds rendererBounds = renderer.bounds;
                EncapsulateLocalPoint(parent, rendererBounds.min, ref localBounds, ref hasBounds);
                EncapsulateLocalPoint(parent, rendererBounds.max, ref localBounds, ref hasBounds);
                EncapsulateLocalPoint(parent, new Vector3(rendererBounds.min.x, rendererBounds.min.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
                EncapsulateLocalPoint(parent, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
                EncapsulateLocalPoint(parent, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
                EncapsulateLocalPoint(parent, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
                EncapsulateLocalPoint(parent, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
                EncapsulateLocalPoint(parent, new Vector3(rendererBounds.max.x, rendererBounds.max.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
            }

            return hasBounds;
        }

        private static void EncapsulateLocalPoint(Transform parent, Vector3 worldPoint, ref Bounds localBounds, ref bool hasBounds)
        {
            Vector3 localPoint = parent != null ? parent.InverseTransformPoint(worldPoint) : worldPoint;
            if (!hasBounds)
            {
                localBounds = new Bounds(localPoint, Vector3.zero);
                hasBounds = true;
                return;
            }

            localBounds.Encapsulate(localPoint);
        }

        private void ApplyHologramVisuals(GameObject instance, Color color)
        {
            if (instance == null)
            {
                return;
            }

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (hologramMaterial != null)
                    {
                        // Authored hologram material: clone per renderer and keep its tuned values.
                        materials[i] = new Material(hologramMaterial);
                    }
                    else if (materials[i] != null)
                    {
                        materials[i] = CreateFallbackHologramMaterial(materials[i]);
                        ApplyTransparentMaterialState(materials[i], color, hologramEmissionColor);
                    }
                    else
                    {
                        continue;
                    }
                }

                renderer.materials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private Material CreateFallbackHologramMaterial(Material source)
        {
            Shader shader = hologramFallbackShader != null
                ? hologramFallbackShader
                : Shader.Find(Uber3DObjectShaderName);
            if (shader == null)
            {
                return new Material(source);
            }

            Material material = new(shader)
            {
                name = $"{source.name}_Hologram",
            };
            CopyMainTexture(source, material);
            return material;
        }

        private static void CopyMainTexture(Material source, Material target)
        {
            if (source == null || target == null || !target.HasProperty(BaseMapId))
            {
                return;
            }

            Texture texture = null;
            if (source.HasProperty(BaseMapId))
            {
                texture = source.GetTexture(BaseMapId);
            }
            else if (source.HasProperty(MainTexId))
            {
                texture = source.GetTexture(MainTexId);
            }

            if (texture != null)
            {
                target.SetTexture(BaseMapId, texture);
            }
        }

        private static Material[] CollectHologramMaterials(GameObject instance)
        {
            if (instance == null)
            {
                return Array.Empty<Material>();
            }

            var materials = new List<Material>();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                materials.AddRange(renderer.materials);
            }

            return materials.ToArray();
        }

        private static MaterialColorBinding[] PrepareAlphaBindings(GameObject instance, float alpha)
        {
            if (instance == null)
            {
                return Array.Empty<MaterialColorBinding>();
            }

            var bindings = new List<MaterialColorBinding>();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.materials;
                foreach (Material material in materials)
                {
                    if (material == null || !TryGetColorProperty(material, out string propertyName))
                    {
                        continue;
                    }

                    Color targetColor = material.GetColor(propertyName);
                    Color startColor = targetColor;
                    startColor.a = alpha;
                    material.SetColor(propertyName, startColor);
                    bindings.Add(new MaterialColorBinding(material, propertyName, targetColor));
                }
            }

            return bindings.ToArray();
        }

        private static void AddMaterialColorTweens(Sequence sequence, MaterialColorBinding[] bindings, float duration)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                MaterialColorBinding binding = bindings[i];
                sequence.Join(DOTween.To(
                    () => binding.Material.GetColor(binding.PropertyName),
                    value => binding.Material.SetColor(binding.PropertyName, value),
                    binding.TargetColor,
                    duration));
            }
        }

        private static void AddMaterialAlphaTweens(Sequence sequence, MaterialColorBinding[] bindings, float alpha, float duration)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                MaterialColorBinding binding = bindings[i];
                Color targetColor = binding.TargetColor;
                targetColor.a = alpha;
                sequence.Join(DOTween.To(
                    () => binding.Material.GetColor(binding.PropertyName),
                    value => binding.Material.SetColor(binding.PropertyName, value),
                    targetColor,
                    duration));
            }
        }

        private static void ApplyTargetColors(MaterialColorBinding[] bindings)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                MaterialColorBinding binding = bindings[i];
                binding.Material.SetColor(binding.PropertyName, binding.TargetColor);
            }
        }

        private static void ApplyTransparentMaterialState(Material material, Color color, Color emissionColor)
        {
            if (material == null)
            {
                return;
            }

            if (TryGetColorProperty(material, out string propertyName))
            {
                material.SetColor(propertyName, color);
            }

            if (material.HasProperty(HologramEnabledId))
            {
                material.SetFloat(HologramEnabledId, 1f);
                material.EnableKeyword("_HOLOGRAM_ON");
            }

            if (material.HasProperty(HologramColorId))
            {
                material.SetColor(HologramColorId, emissionColor);
            }

            if (material.HasProperty(HologramOpacityId))
            {
                material.SetFloat(HologramOpacityId, Mathf.Clamp01(color.a * 0.68f));
            }

            if (material.HasProperty(HologramFresnelPowerId))
            {
                material.SetFloat(HologramFresnelPowerId, 2.1f);
            }

            if (material.HasProperty(HologramFresnelIntensityId))
            {
                material.SetFloat(HologramFresnelIntensityId, 3.4f);
            }

            if (material.HasProperty(HologramScanlineDensityId))
            {
                material.SetFloat(HologramScanlineDensityId, 34f);
            }

            if (material.HasProperty(HologramScanlineSpeedId))
            {
                material.SetFloat(HologramScanlineSpeedId, 0f);
            }

            if (material.HasProperty(HologramScanlineWidthId))
            {
                material.SetFloat(HologramScanlineWidthId, 0.16f);
            }

            if (material.HasProperty(HologramScanlineIntensityId))
            {
                material.SetFloat(HologramScanlineIntensityId, 2.4f);
            }

            if (material.HasProperty(HologramNoiseScaleId))
            {
                material.SetFloat(HologramNoiseScaleId, 7f);
            }

            if (material.HasProperty(HologramNoiseStrengthId))
            {
                material.SetFloat(HologramNoiseStrengthId, 0.28f);
            }

            if (material.HasProperty(HologramNoiseSpeedId))
            {
                material.SetFloat(HologramNoiseSpeedId, 0f);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
            }

            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 1f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (material.HasProperty(BlendId))
            {
                material.SetFloat(BlendId, 2f);
            }

            if (material.HasProperty(LightingModeId))
            {
                material.SetFloat(LightingModeId, 1f);
                material.EnableKeyword("_UNLIT_ON");
            }

            if (material.HasProperty(SrcBlendId))
            {
                material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty(DstBlendId))
            {
                material.SetFloat(DstBlendId, (float)BlendMode.One);
            }

            if (material.HasProperty(SrcBlendAlphaId))
            {
                material.SetFloat(SrcBlendAlphaId, (float)BlendMode.One);
            }

            if (material.HasProperty(DstBlendAlphaId))
            {
                material.SetFloat(DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(ZWriteId))
            {
                material.SetFloat(ZWriteId, 0f);
            }

            if (material.HasProperty(CastShadowsId))
            {
                material.SetFloat(CastShadowsId, 0f);
            }

            if (material.HasProperty(ReceiveShadowsId))
            {
                material.SetFloat(ReceiveShadowsId, 0f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static bool TryGetColorProperty(Material material, out string propertyName)
        {
            if (material.HasProperty("_BaseColor"))
            {
                propertyName = "_BaseColor";
                return true;
            }

            if (material.HasProperty("_Color"))
            {
                propertyName = "_Color";
                return true;
            }

            propertyName = null;
            return false;
        }

        private static void ApplyDontSaveInEditor(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            }
        }

        public void CompleteMaterializeForTests()
        {
            if (activeMaterializeSequence != null)
            {
                activeMaterializeSequence.Complete();
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
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

        private readonly struct MaterialColorBinding
        {
            public MaterialColorBinding(Material material, string propertyName, Color targetColor)
            {
                Material = material;
                PropertyName = propertyName;
                TargetColor = targetColor;
            }

            public Material Material { get; }
            public string PropertyName { get; }
            public Color TargetColor { get; }
        }
    }
}
