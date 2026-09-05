using UnityEngine;
using UnityEngine.Rendering;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class LaunchTargetZoneGuide : MonoBehaviour
    {
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

        private const string HologramKeyword = "_HOLOGRAM_ON";
        private const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";
        private const string UnlitKeyword = "_UNLIT_ON";

        private readonly Color idleColor = new(1f, 0.86f, 0.22f, 0.28f);
        private readonly Color activeColor = new(1f, 1f, 0.38f, 0.52f);

        private Transform targetRoot;
        private MeshRenderer targetRenderer;
        private Transform arrow;
        private MeshRenderer arrowRenderer;
        private Mesh arrowMesh;
        private Material targetMaterial;
        private Material arrowMaterial;
        private Transform rocket;
        private Bounds targetBounds;
        private float targetRadius;

        public Bounds TargetBounds => targetBounds;
        public bool IsVisible => targetRoot != null && targetRoot.gameObject.activeSelf;
        public Material TargetMaterial => targetMaterial;
        public Material ArrowMaterial => arrowMaterial;
        public Mesh ArrowMesh => arrowMesh;

        public void Initialize(Transform rocketTransform, Vector3 center, float radius)
        {
            rocket = rocketTransform;
            targetRadius = Mathf.Max(radius, 0.01f);
            targetBounds = new Bounds(center, Vector3.one * (targetRadius * 2f));
            CreateTargetSphere();
            CreateArrow();
            Tick(false);
        }

        public void Tick(bool inside)
        {
            if (rocket == null || targetRoot == null || arrow == null) return;

            SetInside(inside);
            Vector3 toTarget = targetBounds.center - rocket.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                arrow.gameObject.SetActive(false);
                return;
            }

            arrow.gameObject.SetActive(true);
            float distance = Mathf.Clamp(toTarget.magnitude * 0.08f, 3f, 9f);
            arrow.position = rocket.position + toTarget.normalized * distance;
            arrow.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            arrow.localScale = Vector3.one * Mathf.Clamp(toTarget.magnitude * 0.009f, 0.7f, 2.2f);
        }

        public void Dispose()
        {
            DestroyUnityObject(targetRoot != null ? targetRoot.gameObject : null);
            DestroyUnityObject(arrow != null ? arrow.gameObject : null);
            DestroyUnityObject(arrowMesh);
            DestroyUnityObject(targetMaterial);
            DestroyUnityObject(arrowMaterial);
            targetRoot = null;
            targetRenderer = null;
            arrow = null;
            arrowRenderer = null;
            arrowMesh = null;
            targetMaterial = null;
            arrowMaterial = null;
        }

        private void OnDestroy() => Dispose();

        private void CreateTargetSphere()
        {
            var host = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            host.name = "Launch Target Zone";
            Collider collider = host.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                DestroyUnityObject(collider);
            }
            host.transform.SetPositionAndRotation(targetBounds.center, Quaternion.identity);
            host.transform.localScale = Vector3.one * (targetRadius * 2f);
            targetRoot = host.transform;
            targetRenderer = host.GetComponent<MeshRenderer>();
            targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
            targetRenderer.receiveShadows = false;
            targetMaterial = CreateHologramMaterial(idleColor);
            targetRenderer.sharedMaterial = targetMaterial;
        }

        private void CreateArrow()
        {
            var host = new GameObject("Launch Target Direction");
            arrow = host.transform;
            var filter = host.AddComponent<MeshFilter>();
            arrowMesh = CreateArrowMesh();
            filter.sharedMesh = arrowMesh;
            arrowRenderer = host.AddComponent<MeshRenderer>();
            arrowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            arrowRenderer.receiveShadows = false;
            arrowMaterial = CreateSolidMaterial(activeColor);
            arrowRenderer.sharedMaterial = arrowMaterial;
        }

        private void SetInside(bool inside)
        {
            Color color = inside ? activeColor : idleColor;
            ApplyColor(targetMaterial, color);
            ApplyColor(arrowMaterial, new Color(1f, 0.92f, 0.1f, 1f));
        }

        private static Material CreateHologramMaterial(Color color)
        {
            Shader shader = Shader.Find("Shader/Uber/3D Object");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

            var material = new Material(shader) { name = "Launch Target Hologram" };
            ApplyColor(material, color);
            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 1f);
                material.EnableKeyword(TransparentKeyword);
            }
            if (material.HasProperty(BlendId)) material.SetFloat(BlendId, 2f);
            if (material.HasProperty(LightingModeId))
            {
                material.SetFloat(LightingModeId, 1f);
                material.EnableKeyword(UnlitKeyword);
            }
            if (material.HasProperty(SrcBlendId)) material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            if (material.HasProperty(DstBlendId)) material.SetFloat(DstBlendId, (float)BlendMode.One);
            if (material.HasProperty(SrcBlendAlphaId)) material.SetFloat(SrcBlendAlphaId, (float)BlendMode.One);
            if (material.HasProperty(DstBlendAlphaId)) material.SetFloat(DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty(ZWriteId)) material.SetFloat(ZWriteId, 0f);
            if (material.HasProperty(CastShadowsId)) material.SetFloat(CastShadowsId, 0f);
            if (material.HasProperty(ReceiveShadowsId)) material.SetFloat(ReceiveShadowsId, 0f);
            if (material.HasProperty(HologramEnabledId))
            {
                material.SetFloat(HologramEnabledId, 1f);
                material.EnableKeyword(HologramKeyword);
            }
            if (material.HasProperty(HologramFresnelPowerId)) material.SetFloat(HologramFresnelPowerId, 2.1f);
            if (material.HasProperty(HologramFresnelIntensityId)) material.SetFloat(HologramFresnelIntensityId, 3.6f);
            if (material.HasProperty(HologramScanlineDensityId)) material.SetFloat(HologramScanlineDensityId, 34f);
            if (material.HasProperty(HologramScanlineSpeedId)) material.SetFloat(HologramScanlineSpeedId, 0f);
            if (material.HasProperty(HologramScanlineWidthId)) material.SetFloat(HologramScanlineWidthId, 0.16f);
            if (material.HasProperty(HologramScanlineIntensityId)) material.SetFloat(HologramScanlineIntensityId, 2.6f);
            if (material.HasProperty(HologramNoiseScaleId)) material.SetFloat(HologramNoiseScaleId, 7f);
            if (material.HasProperty(HologramNoiseStrengthId)) material.SetFloat(HologramNoiseStrengthId, 0.28f);
            if (material.HasProperty(HologramNoiseSpeedId)) material.SetFloat(HologramNoiseSpeedId, 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static Material CreateSolidMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Shader/Uber/3D Object");

            var material = new Material(shader) { name = "Launch Target Direction Solid" };
            ApplyColor(material, new Color(color.r, color.g, color.b, 1f));
            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 0f);
                material.DisableKeyword(TransparentKeyword);
            }
            if (material.HasProperty(BlendId)) material.SetFloat(BlendId, 0f);
            if (material.HasProperty(LightingModeId))
            {
                material.SetFloat(LightingModeId, 1f);
                material.EnableKeyword(UnlitKeyword);
            }
            if (material.HasProperty(SrcBlendId)) material.SetFloat(SrcBlendId, (float)BlendMode.One);
            if (material.HasProperty(DstBlendId)) material.SetFloat(DstBlendId, (float)BlendMode.Zero);
            if (material.HasProperty(SrcBlendAlphaId)) material.SetFloat(SrcBlendAlphaId, (float)BlendMode.One);
            if (material.HasProperty(DstBlendAlphaId)) material.SetFloat(DstBlendAlphaId, (float)BlendMode.Zero);
            if (material.HasProperty(ZWriteId)) material.SetFloat(ZWriteId, 1f);
            if (material.HasProperty(CastShadowsId)) material.SetFloat(CastShadowsId, 0f);
            if (material.HasProperty(ReceiveShadowsId)) material.SetFloat(ReceiveShadowsId, 0f);
            if (material.HasProperty(HologramEnabledId))
            {
                material.SetFloat(HologramEnabledId, 0f);
                material.DisableKeyword(HologramKeyword);
            }

            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = (int)RenderQueue.Geometry;
            return material;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material == null) return;
            material.color = color;
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
            if (material.HasProperty(HologramColorId))
            {
                material.SetColor(HologramColorId, new Color(
                    Mathf.Min(color.r * 1.45f, 2f),
                    Mathf.Min(color.g * 1.45f, 2f),
                    Mathf.Min(color.b * 1.1f, 2f),
                    1f));
            }
            if (material.HasProperty(HologramOpacityId)) material.SetFloat(HologramOpacityId, Mathf.Clamp01(color.a * 0.68f));
        }

        private static Mesh CreateArrowMesh()
        {
            var mesh = new Mesh { name = "LaunchTargetDirectionTriangle" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0.72f),
                new Vector3(-0.58f, 0f, -0.42f),
                new Vector3(0.58f, 0f, -0.42f),
                new Vector3(0f, 0.14f, 0.72f),
                new Vector3(-0.58f, 0.14f, -0.42f),
                new Vector3(0.58f, 0.14f, -0.42f),
            };
            mesh.triangles = new[]
            {
                0, 1, 2,
                3, 5, 4,
                0, 3, 4, 0, 4, 1,
                1, 4, 5, 1, 5, 2,
                2, 5, 3, 2, 3, 0,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
