using UnityEngine;
using UnityEngine.Rendering;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class LaunchTargetZoneGuide : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int HologramEnabledId = Shader.PropertyToID("_HologramEnabled");
        private static readonly int HologramColorId = Shader.PropertyToID("_HologramColor");
        private static readonly int HologramOpacityId = Shader.PropertyToID("_HologramOpacity");

        private const string HologramKeyword = "_HOLOGRAM_ON";
        private const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";

        private readonly Color idleColor = new(1f, 0.86f, 0.22f, 0.18f);
        private readonly Color activeColor = new(1f, 1f, 0.38f, 0.38f);

        private Transform targetRoot;
        private MeshRenderer targetRenderer;
        private Transform arrow;
        private MeshRenderer arrowRenderer;
        private Mesh arrowMesh;
        private Material targetMaterial;
        private Material arrowMaterial;
        private Transform rocket;
        private Bounds targetBounds;

        public Bounds TargetBounds => targetBounds;
        public bool IsVisible => targetRoot != null && targetRoot.gameObject.activeSelf;

        public void Initialize(Transform rocketTransform, Vector3 origin, Bounds localTargetBounds)
        {
            rocket = rocketTransform;
            targetBounds = new Bounds(origin + localTargetBounds.center, localTargetBounds.size);
            CreateTargetBox();
            CreateArrow();
            SetInside(false);
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
            arrow.localScale = Vector3.one * Mathf.Clamp(toTarget.magnitude * 0.015f, 1.2f, 4f);
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

        private void CreateTargetBox()
        {
            var host = GameObject.CreatePrimitive(PrimitiveType.Cube);
            host.name = "Launch Target Zone";
            Collider collider = host.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                DestroyUnityObject(collider);
            }
            host.transform.SetPositionAndRotation(targetBounds.center, Quaternion.identity);
            host.transform.localScale = targetBounds.size;
            targetRoot = host.transform;
            targetRenderer = host.GetComponent<MeshRenderer>();
            targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
            targetRenderer.receiveShadows = false;
            targetMaterial = CreateHologramMaterial(idleColor);
            targetRenderer.material = targetMaterial;
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
            arrowMaterial = CreateHologramMaterial(idleColor);
            arrowRenderer.material = arrowMaterial;
        }

        private void SetInside(bool inside)
        {
            Color color = inside ? activeColor : idleColor;
            ApplyColor(targetMaterial, color);
            ApplyColor(arrowMaterial, color);
        }

        private static Material CreateHologramMaterial(Color color)
        {
            Shader shader = Shader.Find("Shader/Uber/3D Object");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

            var material = new Material(shader) { name = "Launch Target Hologram" };
            material.SetColor(BaseColorId, color);
            material.color = color;
            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 1f);
                material.EnableKeyword(TransparentKeyword);
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            if (material.HasProperty(HologramEnabledId))
            {
                material.SetFloat(HologramEnabledId, 1f);
                material.EnableKeyword(HologramKeyword);
            }
            if (material.HasProperty(HologramOpacityId)) material.SetFloat(HologramOpacityId, color.a);
            if (material.HasProperty(HologramColorId)) material.SetColor(HologramColorId, color);
            return material;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material == null) return;
            material.color = color;
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            if (material.HasProperty(HologramColorId)) material.SetColor(HologramColorId, color);
            if (material.HasProperty(HologramOpacityId)) material.SetFloat(HologramOpacityId, color.a);
        }

        private static Mesh CreateArrowMesh()
        {
            var mesh = new Mesh { name = "LaunchTargetDirectionTriangle" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 1.2f),
                new Vector3(-0.55f, 0f, -0.6f),
                new Vector3(0.55f, 0f, -0.6f),
                new Vector3(0f, 0.18f, 1.2f),
                new Vector3(-0.55f, 0.18f, -0.6f),
                new Vector3(0.55f, 0.18f, -0.6f),
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
