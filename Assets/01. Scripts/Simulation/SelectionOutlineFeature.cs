using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Simulation
{
    /// <summary>
    /// 설계 스테이지에서 선택한 부품의 아웃라인을 화면 공간에서 그린다. 선택된 렌더러의 실루엣을
    /// 마스크 텍스처에 채운 뒤 픽셀 단위로 팽창시켜 카메라 컬러 위에 얹는다. 인버티드 헐과 달리
    /// 두께가 거리와 무관하게 일정하고, 깊이를 쓰지 않으므로 다른 부품에 가려져도 보인다.
    ///
    /// 대상은 정적 레지스트리로 정한다 — 레이어를 쓰면 <see cref="RocketBuilder"/> 의 먼지/트레일
    /// 컬링 마스크와 물리 매트릭스까지 건드려야 하고, 렌더링 레이어를 쓰면 RendererList 설정이
    /// 통째로 따라온다. 선택 지점은 <c>RocketBuilder.Select</c> 한 곳뿐이라 등록이 한 줄로 끝난다.
    /// </summary>
    public sealed class SelectionOutlineFeature : ScriptableRendererFeature
    {
        private static readonly MeshRenderer[] NoRenderers = new MeshRenderer[0];
        private static readonly int[] NoSubMeshes = new int[0];
        private static readonly int OutlineMaskId = Shader.PropertyToID("_OutlineMask");

        /// <summary>아웃라인을 그릴 카메라. 이것과 다른 카메라는 패스를 건너뛴다.</summary>
        public static Camera Camera { get; private set; }

        /// <summary>마스크에 채울 렌더러. 비어 있으면 패스가 아예 큐에 들어가지 않는다.</summary>
        public static MeshRenderer[] Renderers { get; private set; } = NoRenderers;

        private static int[] _subMeshes = NoSubMeshes;

        [SerializeField] private Material outlineMaterial;

        private SelectionOutlinePass _pass;

        /// <summary>
        /// 선택 갱신. <paramref name="part"/> 가 null 이면 아웃라인을 끈다.
        /// <see cref="MeshRenderer"/> 만 모으므로 배기 파티클과 가이드 라인은 마스크에 들어오지 않는다.
        /// </summary>
        public static void Select(Camera camera, GameObject part)
        {
            if (part == null)
            {
                Camera = null;
                Renderers = NoRenderers;
                _subMeshes = NoSubMeshes;
                return;
            }

            Camera = camera;
            Renderers = part.GetComponentsInChildren<MeshRenderer>();

            // 서브메시 개수는 선택할 때 한 번만 센다 — 매 프레임 sharedMaterials 를 읽으면 할당이 난다.
            _subMeshes = new int[Renderers.Length];
            for (int i = 0; i < Renderers.Length; i++)
            {
                var filter = Renderers[i].GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                _subMeshes[i] = mesh != null ? mesh.subMeshCount : 1;
            }
        }

        public override void Create()
        {
            _pass = new SelectionOutlinePass
            {
                // Uber Post Processing 이 550 이라 그보다 앞에 둔다 — 아웃라인도 같이 후처리를 탄다.
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 선택이 없으면 어느 카메라에서도 비용이 0 이다 — 연구 화면 포함.
            if (outlineMaterial == null || Camera == null || Renderers.Length == 0) return;

            _pass.Setup(outlineMaterial);
            renderer.EnqueuePass(_pass);
        }

        private sealed class SelectionOutlinePass : ScriptableRenderPass
        {
            private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

            private Material _material;

            public void Setup(Material material) => _material = material;

            private class MaskPassData
            {
                public Material Material;
                public Rect Viewport;
            }

            private class CompositePassData
            {
                public Material Material;
                public TextureHandle Mask;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();

                // 설계 카메라와 연구 카메라가 같은 RenderTexture 로 그리고(SimulationCrtScreen),
                // DrawRenderer 는 컬링 마스크를 무시한다 — 이 비교가 없으면 뷰포트 밖으로 샌다.
                if (cameraData.camera != Camera) return;

                var resourceData = frameData.Get<UniversalResourceData>();

                TextureDesc desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                desc.name = "_SelectionOutlineMask";
                desc.format = GraphicsFormat.R8_UNorm;
                desc.msaaSamples = MSAASamples.None;
                desc.bindTextureMS = false;
                // 클리어는 렌더 함수에서 직접 한다 — 풀에서 돌려받은 텍스처에 지난 프레임 실루엣이
                // 남으면 궤적이 화면에 쌓이고, 누적된 영역 안쪽은 테두리가 아예 사라진다.
                desc.clearBuffer = false;
                TextureHandle mask = renderGraph.CreateTexture(desc);

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                           "Selection Outline Mask", out var passData))
                {
                    passData.Material = _material;

                    // 설계 카메라는 화면 일부만 쓰는 rect 를 가진다(RocketDesignUI.UpdateViewportRect).
                    // 씬이 중간 컬러 텍스처로 갈 때 그 텍스처는 이미 뷰포트 크기라 오프셋이 없다 —
                    // 그때 rect 를 태우면 아웃라인이 엔진에서 밀려 나간다. 카메라 타깃에 직접 그릴
                    // 때만 rect 를 픽셀로 환산한다.
                    Rect rect = cameraData.camera.rect;
                    passData.Viewport = desc.width == cameraData.scaledWidth
                                        && desc.height == cameraData.scaledHeight
                        ? new Rect(0f, 0f, desc.width, desc.height)
                        : new Rect(rect.x * desc.width, rect.y * desc.height,
                            rect.width * desc.width, rect.height * desc.height);

                    builder.SetRenderAttachment(mask, 0, AccessFlags.WriteAll);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                    {
                        // 뷰포트를 좁히기 전에 어태치먼트 전체를 지운다. 합성 패스는 화면 전체를 읽으므로
                        // 뷰포트 밖에 남은 값도 그대로 아웃라인이 된다.
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1f, 0);
                        context.cmd.SetViewport(data.Viewport);
                        MeshRenderer[] renderers = Renderers;
                        for (int i = 0; i < renderers.Length; i++)
                        {
                            MeshRenderer renderer = renderers[i];
                            if (renderer == null) continue;
                            for (int submesh = 0; submesh < _subMeshes[i]; submesh++)
                                context.cmd.DrawRenderer(renderer, data.Material, submesh, 0);
                        }
                    });
                }

                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                           "Selection Outline Composite", out var passData))
                {
                    passData.Material = _material;
                    passData.Mask = mask;

                    builder.UseTexture(mask, AccessFlags.Read);
                    // Write 는 기존 내용을 버리지 않는다 — 카메라 컬러 위에 알파 블렌딩된다.
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                    {
                        Block.Clear();
                        Block.SetTexture(OutlineMaskId, data.Mask);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 1,
                            MeshTopology.Triangles, 3, 1, Block);
                    });
                }
            }
        }
    }
}
