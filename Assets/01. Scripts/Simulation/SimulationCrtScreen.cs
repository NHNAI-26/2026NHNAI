using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Simulation
{
    /// <summary>
    /// 설계·발사 화면이 떠 있는 동안 화면 전체를 CRT 모니터처럼 보이게 한다.
    /// 카메라 스택은 쓸 수 없다 — URP 스택은 Base 카메라의 뷰포트 사각형을 스택 전체가 공유하는데
    /// 시뮬레이션 카메라는 <see cref="RocketDesignUI"/> 가 화면 가운데 사각형으로 잡아 준다.
    /// 대신 3D 카메라 둘을 화면 크기 <see cref="RenderTexture"/> 한 장에 합성하고, 그 결과를 UI 뒤에
    /// 깐 <see cref="RawImage"/> 로 되돌린 다음, 그 UI 캔버스를 그리는 카메라 하나에만
    /// 풀스크린 CRT 패스(<c>Assets/Settings/CRT_Renderer.asset</c>)를 건다. 최종 화면을 그리는 카메라가
    /// 하나뿐이라 UI 든 3D 든 빠짐없이 한 번씩만 필터를 받는다.
    /// RenderTexture 가 화면과 같은 크기라 <see cref="Camera.pixelRect"/> 는 그대로다 —
    /// <see cref="RocketBuilder"/> 의 집기·기즈모 좌표계는 손대지 않아도 된다(docs/rocket-simulation.md).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationCrtScreen : MonoBehaviour
    {
        // 렌더러 **에셋 파일** 이름으로 찾는다. 피처(서브에셋)의 이름은 키로 못 쓴다 — 실제로
        // 재직렬화되면서 형제 렌더러의 피처 이름("Uber Post Processing")으로 덮여 버린 적이 있다.
        private const string RendererName = "CRT_Renderer";
        private const float CurtainSeconds = 0.4f;
        private const float PowerOnSeconds = 0.6f;
        private const float PowerOffSeconds = 0.5f;

        // CRT 카메라는 씬에서 멀리 떨어뜨린다. Screen Space - Camera 캔버스는 월드 지오메트리라
        // 시뮬레이션 카메라 시야에 들어오면 3D 위에 한 번 더 그려진다.
        private static readonly Vector3 CrtCameraPosition = new(0f, 100000f, 0f);

        private static readonly int PowerOffAmountId = Shader.PropertyToID("_CRTPowerOffAmount");

        private Camera mainCamera;
        private Camera simulationCamera;
        private RenderTexture screen;
        private Camera crtCamera;
        private RocketDesignUI designUI;
        private Canvas designCanvas;
        private RenderMode designCanvasMode;
        private Camera designCanvasCamera;
        private RawImage present;
        private FullScreenPassRendererFeature feature;
        private Material featureMaterial;
        private int rendererIndex = -1;
        private RectTransform curtain;
        private float power;
        private bool active;

        /// <summary>
        /// 그 자리에서 화면을 덮는다. 슬라이드로 덮지 않는 이유는 이 시점에 연구 화면이 이미
        /// 꺼져 있어서다(<c>ResearchOperationUIController.ShowDesignScreen</c>) — 덮는 데 시간을 쓰면
        /// 빈 방이 그만큼 노출된다. 밑에서 위로 올라오는 연출은 걷어낼 때(<see cref="PowerOnRoutine"/>) 한다.
        /// </summary>
        public void Cover()
        {
            EnsureCurtain();
            curtain.gameObject.SetActive(true);
            SetScreenOffset(-1f);
        }

        /// <summary>덮개를 아래로 마저 내려보내 그 아래(돌아온 연구 화면)를 드러낸다.</summary>
        public IEnumerator UncoverRoutine()
        {
            if (curtain == null || !curtain.gameObject.activeSelf) yield break;

            float height = Screen.height;
            for (float elapsed = 0f; elapsed < CurtainSeconds; elapsed += Time.unscaledDeltaTime)
            {
                curtain.anchoredPosition = new Vector2(0f, Smooth(0f, -height, elapsed / CurtainSeconds));
                yield return null;
            }

            curtain.gameObject.SetActive(false);
            curtain.anchoredPosition = Vector2.zero;
        }

        /// <summary>합성 경로를 세운다. 이 시점의 전원 값은 1(꺼짐)이라 화면은 아직 검다.</summary>
        public void Begin(Camera main, Camera simulation, RocketDesignUI ui)
        {
            if (active || ui == null || ui.Canvas == null) return;
            if (!TryResolveFeature()) return;

            mainCamera = main;
            simulationCamera = simulation;
            designUI = ui;
            designCanvas = ui.Canvas;

            screen = CreateScreenTexture();
            if (mainCamera != null) mainCamera.targetTexture = screen;
            if (simulationCamera != null) simulationCamera.targetTexture = screen;

            crtCamera = new GameObject("CRT Screen Camera").AddComponent<Camera>();
            crtCamera.transform.position = CrtCameraPosition;
            crtCamera.orthographic = true;
            crtCamera.nearClipPlane = 0.1f;
            crtCamera.farClipPlane = 100f;
            crtCamera.clearFlags = CameraClearFlags.SolidColor;
            crtCamera.backgroundColor = Color.black;
            crtCamera.depth = 20f;
            crtCamera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);

            designCanvasMode = designCanvas.renderMode;
            designCanvasCamera = designCanvas.worldCamera;
            designCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            designCanvas.worldCamera = crtCamera;
            designCanvas.planeDistance = 10f;

            present = new GameObject("CRT Screen Source", typeof(RawImage)).GetComponent<RawImage>();
            var presentRect = (RectTransform)present.transform;
            presentRect.SetParent(designCanvas.transform, false);
            presentRect.SetAsFirstSibling(); // UI 보다 뒤에 깔린다
            presentRect.anchorMin = Vector2.zero;
            presentRect.anchorMax = Vector2.one;
            presentRect.offsetMin = Vector2.zero;
            presentRect.offsetMax = Vector2.zero;
            present.texture = screen;
            present.raycastTarget = false;

            // 피처가 가리키는 MAT_CRT 를 그대로 쓴다. 복제본을 꽂아 두면 에셋을 하나라도 다시 임포트하는
            // 순간 피처가 직렬화된 원본으로 되돌아가 연출이 통째로 끊기고, 인스펙터로 값을 만져도 화면에
            // 반영되지 않는다. 전원 값은 에셋에 저장된 값이 0 이고 End 에서 0 으로 되돌리므로 남지 않는다.
            SetPower(1f);

            active = true;

            // 화면을 아래로 내려놓고 시작한다. 여기서부터 슬라이드가 끝날 때까지 설계 UI 를 멈춘다 —
            // UpdateViewportRect 가 캔버스를 따라가며 시뮬레이션 카메라 사각형을 다시 잡으면
            // RenderTexture 안의 3D 가 화면과 따로 움직여 두 겹으로 밀린다.
            designUI.enabled = false;
            SetScreenOffset(-1f);
        }

        /// <summary>
        /// 덮개를 위로 걷어내며(화면이 밑에서 위로 드러난다) 동시에 브라운관을 켠다(1 → 0).
        /// 두 동작을 겹치는 이유는 전원 값이 1 이면 화면이 완전히 검어서다 — 따로 하면 걷어내는
        /// 동안 보이는 것이 없다.
        /// </summary>
        public IEnumerator PowerOnRoutine()
        {
            EnsureCurtain();
            for (float elapsed = 0f; elapsed < PowerOnSeconds; elapsed += Time.unscaledDeltaTime)
            {
                SetScreenOffset(Smooth(-1f, 0f, elapsed / CurtainSeconds));
                SetPower(Mathf.Lerp(1f, 0f, elapsed / PowerOnSeconds));
                yield return null;
            }

            SetScreenOffset(0f);
            SetPower(0f);
            curtain.gameObject.SetActive(false);
            if (designUI != null) designUI.enabled = true;
        }

        /// <summary>
        /// 브라운관을 끄고(0 → 1) 덮개를 위에서 아래로 내려 덮은 뒤 합성 경로를 걷는다.
        /// 덮개는 <see cref="UncoverRoutine"/> 에서 같은 방향으로 마저 내려가 한 동작으로 이어진다.
        /// </summary>
        public IEnumerator PowerOffRoutine()
        {
            if (active)
            {
                yield return TweenPower(0f, 1f, PowerOffSeconds);

                EnsureCurtain();
                curtain.gameObject.SetActive(true);
                if (designUI != null) designUI.enabled = false;
                for (float elapsed = 0f; elapsed < CurtainSeconds; elapsed += Time.unscaledDeltaTime)
                {
                    SetScreenOffset(Smooth(0f, -1f, elapsed / CurtainSeconds));
                    yield return null;
                }

                SetScreenOffset(-1f);
            }

            End();
        }

        private void End()
        {
            if (!active) return;
            active = false;

            SetPower(0f); // 에셋에 저장된 값으로 되돌린다
            if (mainCamera != null) mainCamera.targetTexture = null;
            if (simulationCamera != null) simulationCamera.targetTexture = null;

            if (designCanvas != null)
            {
                designCanvas.renderMode = designCanvasMode;
                designCanvas.worldCamera = designCanvasCamera;
            }
            if (designUI != null) designUI.enabled = true;

            if (present != null) Destroy(present.gameObject);
            if (crtCamera != null) Destroy(crtCamera.gameObject);
            if (screen != null) { screen.Release(); Destroy(screen); }

            present = null;
            crtCamera = null;
            screen = null;
            designUI = null;
            designCanvas = null;
            mainCamera = null;
            simulationCamera = null;
        }

        private void OnDisable() => End();

        private void LateUpdate()
        {
            if (!active || screen == null) return;

            // 창 크기가 바뀌면 합성 타깃을 다시 만든다 — 크기가 어긋나면 뷰포트 사각형이 밀린다.
            if (screen.width == Screen.width && screen.height == Screen.height) return;

            RenderTexture stale = screen;
            screen = CreateScreenTexture();
            if (mainCamera != null) mainCamera.targetTexture = screen;
            if (simulationCamera != null) simulationCamera.targetTexture = screen;
            if (present != null) present.texture = screen;
            stale.Release();
            Destroy(stale);
        }

        private static RenderTexture CreateScreenTexture()
        {
            return new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 24,
                RenderTextureFormat.DefaultHDR) { name = "CRT Screen" };
        }

        /// <summary>
        /// 파이프라인 에셋에 매달린 렌더러들 중 <c>CRT_Renderer</c> 를 찾아 그 풀스크린 패스를 집는다.
        /// 인덱스를 상수로 박지 않는 이유는 렌더러 목록 순서가 에셋 편집으로 바뀔 수 있어서다.
        /// </summary>
        private bool TryResolveFeature()
        {
            if (feature != null) return featureMaterial != null;
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline)
            {
                Debug.LogWarning("CRT screen needs the Universal Render Pipeline.", this);
                return false;
            }

            ReadOnlySpan<ScriptableRendererData> renderers = pipeline.rendererDataList;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].name != RendererName) continue;
                foreach (ScriptableRendererFeature candidate in renderers[i].rendererFeatures)
                {
                    if (candidate is not FullScreenPassRendererFeature pass || pass.passMaterial == null) continue;
                    feature = pass;
                    featureMaterial = pass.passMaterial;
                    rendererIndex = i;
                    return true;
                }
            }

            Debug.LogWarning(
                $"CRT screen needs '{RendererName}' with a full screen pass in the renderer list of "
                + $"'{pipeline.name}'. Add Assets/Settings/{RendererName}.asset to its Renderer List.", this);
            return false;
        }

        private void SetPower(float amount)
        {
            power = amount;
            if (featureMaterial != null) featureMaterial.SetFloat(PowerOffAmountId, amount);
        }

        private static float Smooth(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Lerp(from, to, t * t * (3f - 2f * t));
        }

        private IEnumerator TweenPower(float from, float to, float seconds)
        {
            if (featureMaterial == null) yield break;

            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                SetPower(Mathf.Lerp(from, to, elapsed / seconds));
                yield return null;
            }

            SetPower(to);
        }

        /// <summary>
        /// CRT 화면(UI 포함 합성 결과 전체)을 세로로 민다. <paramref name="offset"/> 은 화면 높이 기준으로
        /// -1 이면 화면 밖 아래, 0 이면 제자리다. 덮개는 화면이 아직 닿지 않은 위쪽만 검게 채운다 —
        /// 카메라는 자기 사각형 밖을 지우지 않기 때문이다.
        /// </summary>
        private void SetScreenOffset(float offset)
        {
            offset = Mathf.Clamp(offset, -1f, 0f);
            if (crtCamera != null) crtCamera.rect = new Rect(0f, offset, 1f, 1f);

            curtain.anchorMin = new Vector2(0f, offset + 1f);
            curtain.anchorMax = Vector2.one;
            curtain.offsetMin = Vector2.zero;
            curtain.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 커튼은 두 씬 어느 쪽에도 두지 않는다 — 씬이 교체되는 그 순간을 덮는 것이 일이라
        /// 01_Main 에 사는 <see cref="SimulationStageHost"/> 옆에 붙여 둔다.
        /// </summary>
        private void EnsureCurtain()
        {
            if (curtain != null) return;

            var canvasObject = new GameObject("CRT Transition Curtain", typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var image = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true; // 전환 중 클릭이 뒤로 새지 않게 막는다
            curtain = (RectTransform)image.transform;
            curtain.SetParent(canvasObject.transform, false);
            curtain.anchorMin = Vector2.zero;
            curtain.anchorMax = Vector2.one;
            curtain.offsetMin = Vector2.zero;
            curtain.offsetMax = Vector2.zero;
        }
    }
}
