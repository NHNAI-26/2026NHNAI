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
    /// 합성에는 카메라 스택을 쓸 수 없다 — URP 스택은 Base 카메라의 뷰포트 사각형을 스택 전체가 공유하는데
    /// 시뮬레이션 카메라는 <see cref="RocketDesignUI"/> 가 화면 가운데 사각형으로 잡아 준다.
    /// 대신 3D 카메라 둘을 화면 크기 <see cref="RenderTexture"/> 한 장에 합성하고, 그 결과를 UI 뒤에
    /// 깐 <see cref="RawImage"/> 로 되돌린 다음, 그 UI 캔버스를 그리는 카메라 하나에만
    /// 풀스크린 CRT 패스(<c>Assets/Settings/CRT_Renderer.asset</c>)를 건다. 최종 화면을 그리는 카메라가
    /// 하나뿐이라 UI 든 3D 든 빠짐없이 한 번씩만 필터를 받는다.
    /// RenderTexture 가 화면과 같은 크기라 <see cref="Camera.pixelRect"/> 는 그대로다 —
    /// <see cref="RocketBuilder"/> 의 집기·기즈모 좌표계는 손대지 않아도 된다(docs/rocket-simulation.md).
    /// 진입 연출은 모니터를 손으로 들어 올리는 모양이다: 바닥에 누워 있던 베젤 메시가 아래 모서리를
    /// 축으로 세워져 화면을 채우고, 그 다음 확대되어 화면 밖으로 완전히 밀려난 뒤, 마지막에 브라운관이
    /// 풀스크린으로 켜진다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationCrtScreen : MonoBehaviour
    {
        // 렌더러 **에셋 파일** 이름으로 찾는다. 피처(서브에셋)의 이름은 키로 못 쓴다 — 실제로
        // 재직렬화되면서 형제 렌더러의 피처 이름("Uber Post Processing")으로 덮여 버린 적이 있다.
        private const string RendererName = "CRT_Renderer";
        private const float PowerOnSeconds = 0.6f;
        private const float PowerOffSeconds = 0.5f;

        // 가운데가 뚫린 16:9 베젤 메시. 이 컴포넌트도 호스트도 런타임 AddComponent 로 생겨서
        // SerializeField 로는 프리팹을 못 받는다 — 이 코드베이스의 다른 런타임 프리팹처럼 Resources 로 문다.
        private const string FramePrefabPath = "displayFrame";
        private const float RiseSeconds = 0.5f;
        private const float ZoomSeconds = 0.5f;

        // 베젤은 아래 모서리를 축으로 눕혔다 세운다. 90도가 바닥에 완전히 누운 상태다.
        private const float LiftDegrees = 90f;

        // 베젤 카메라만 원근이다. 직교로 세우면 회전이 세로로 눌리는 것으로만 보여 블라인드가 열리는
        // 모양이 되고, 손목으로 들어 올리는 느낌이 나지 않는다. 화각은 눈으로 맞추는 값 —
        // 좁히면 왜곡이 줄지만 들어 올리는 맛도 같이 준다.
        private const float FrameFieldOfView = 40f;

        // 확대가 끝났을 때 베젤이 화면 밖으로 완전히 밀려나는 배율. 메시의 구멍/바깥 비율에 달린
        // 아트 값이라 눈으로 맞춘다 — 베젤이 아직 보이면 올린다. 확대 직후 바로 점등이 시작하므로
        // 테두리가 한 조각이라도 남으면 점등 첫 프레임에 그대로 잡힌다.
        private const float ZoomScale = 1.8f;

        // CRT 카메라는 씬에서 멀리 떨어뜨린다. Screen Space - Camera 캔버스는 월드 지오메트리라
        // 시뮬레이션 카메라 시야에 들어오면 3D 위에 한 번 더 그려진다.
        private static readonly Vector3 CrtCameraPosition = new(0f, 100000f, 0f);

        // 베젤도 같은 수법으로 격리한다. 두 카메라의 farClipPlane 이 100 이라 서로를 보지 못하므로
        // 레이어를 새로 파거나 컬링 마스크를 손댈 필요가 없다.
        private static readonly Vector3 FrameCameraPosition = new(0f, 200000f, 0f);

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
        private Image curtainFill;
        private Camera frameCamera;
        private Transform frameHinge;
        private Transform frame;
        private float frameWidth;
        private float frameHeight;
        private float power;
        private bool active;

        /// <summary>
        /// 그 자리에서 화면을 덮는다. 슬라이드로 덮지 않는 이유는 이 시점에 연구 화면이 이미
        /// 꺼져 있어서다(<c>ResearchOperationUIController.ShowDesignScreen</c>) — 덮는 데 시간을 쓰면
        /// 빈 방이 그만큼 노출된다.
        /// </summary>
        public void Cover()
        {
            EnsureCurtain();
            curtain.gameObject.SetActive(true);
            curtain.anchoredPosition = Vector2.zero;
            SetCurtainAlpha(1f);
        }

        /// <summary>
        /// 덮개를 그 자리에서 걷는다. 슬라이드로 걷지 않는다 — 내려오는 검은 판이 베젤을 따라오는
        /// 또 하나의 판으로 보이고, 그러는 사이 연구 화면의 등장 애니메이션
        /// (<c>ResearchOperationTransitionAnimator.PlayEnter</c>, 0.55초)이 그 밑에서 다 소모된다.
        /// </summary>
        public void Uncover()
        {
            if (curtain == null) return;

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

            CreateFrame();

            active = true;

            // 연출이 끝날 때까지 설계 UI 를 멈춘다 — UpdateViewportRect 가 캔버스를 따라가며
            // 시뮬레이션 카메라 사각형을 다시 잡으면 RenderTexture 안의 3D 가 화면과 따로 움직인다.
            designUI.enabled = false;
        }

        /// <summary>
        /// 모니터를 손으로 들어 올리는 순서: 바닥에 누워 있던 베젤이 아래 모서리를 축으로 세워져
        /// (오버슛 후 안착) 화면을 채우고, 그다음 확대되어 화면 밖으로 완전히 사라진 뒤,
        /// 마지막에 브라운관이 풀스크린으로 켜진다. 점등이 맨 뒤인 이유는 브라운관이 켜지는 그림이
        /// 베젤에 가려지지 않고 화면 전체로 보여야 해서다.
        /// 세 단계를 겹치지 않는 이유는 순서 자체가 연출이라서다 — 겹치면 페이드처럼 읽힌다.
        /// </summary>
        public IEnumerator PowerOnRoutine()
        {
            EnsureCurtain();

            // 덮개는 투명해지되 살아 있는다. 화면은 전원 값이 1 이라 어차피 검고,
            // raycastTarget 이 남아 있어야 연출 중 클릭이 뒤로 새지 않는다.
            SetCurtainAlpha(0f);

            yield return TweenFrameAngle(LiftDegrees, 0f, RiseSeconds, Back);
            yield return TweenFrameScale(1f, ZoomScale, ZoomSeconds);
            yield return TweenPower(1f, 0f, PowerOnSeconds);

            curtain.gameObject.SetActive(false);
            SetCurtainAlpha(1f);
            // 확대가 끝나면 베젤은 화면 밖이다. 파괴하지 않고 카메라만 끈다 — 퇴장에서 역방향으로 쓴다.
            if (frameCamera != null) frameCamera.gameObject.SetActive(false);
            if (designUI != null) designUI.enabled = true;
        }

        /// <summary>
        /// 진입의 정확한 역순: 브라운관이 먼저 꺼지고, 검은 화면 위로 베젤이 축소되어 돌아오고,
        /// 마지막에 베젤이 아래로 내려간다.
        /// 내리는 동작에는 오버슛을 쓰지 않는다 — 들어 올릴 때만 손맛이 있어야 한다.
        /// </summary>
        public IEnumerator PowerOffRoutine()
        {
            if (active)
            {
                EnsureCurtain();
                curtain.gameObject.SetActive(true);
                curtain.anchoredPosition = Vector2.zero;
                SetCurtainAlpha(0f);
                if (frameCamera != null) frameCamera.gameObject.SetActive(true);
                if (designUI != null) designUI.enabled = false;

                yield return TweenPower(0f, 1f, PowerOffSeconds);
                yield return TweenFrameScale(ZoomScale, 1f, ZoomSeconds);
                yield return TweenFrameAngle(0f, LiftDegrees, RiseSeconds, SmoothStep);

                // 여기서부터 화면은 검다. 덮개를 다시 검게 만들어 씬 교체를 가린다 —
                // 연구 화면이 돌아오면 호스트가 <see cref="Uncover"/> 로 그 자리에서 걷는다.
                SetCurtainAlpha(1f);
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
            if (frameCamera != null) Destroy(frameCamera.gameObject); // 베젤은 이 카메라의 자식이다
            if (crtCamera != null) Destroy(crtCamera.gameObject);
            if (screen != null) { screen.Release(); Destroy(screen); }

            present = null;
            frameCamera = null;
            frameHinge = null;
            frame = null;
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

            FitFrameCamera(); // 화면 비율이 바뀌면 베젤이 화면을 덮는 배율도 달라진다
        }

        private static RenderTexture CreateScreenTexture()
        {
            return new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 24,
                RenderTextureFormat.DefaultHDR) { name = "CRT Screen" };
        }

        /// <summary>
        /// 베젤을 그릴 전용 카메라를 세우고 그 앞에 프리팹을 놓는다. 이 카메라는 CRT 카메라의 스택에
        /// Overlay 로 들어간다 — 스택이 여기서만 성립하는 이유는 베젤이 Base 카메라와 같은 전체 화면
        /// 사각형을 쓰기 때문이다(시뮬레이션 카메라처럼 자기 사각형을 요구하지 않는다).
        /// Overlay 는 Base 의 렌더러 피처(=CRT 풀스크린 패스)가 끝난 뒤에 그려지므로 베젤에는 스캔라인과
        /// 곡률이 얹히지 않고, 뚫린 가운데로는 필터를 먹은 화면이 그대로 비친다.
        /// </summary>
        private void CreateFrame()
        {
            GameObject prefab = Resources.Load<GameObject>(FramePrefabPath);
            if (prefab == null)
            {
                // 베젤이 없어도 전원 연출만으로 진입은 성립한다.
                Debug.LogWarning($"CRT screen could not load Resources/{FramePrefabPath}.", this);
                return;
            }

            frameCamera = new GameObject("CRT Frame Camera").AddComponent<Camera>();
            frameCamera.transform.position = FrameCameraPosition;
            frameCamera.orthographic = false;
            frameCamera.fieldOfView = FrameFieldOfView;
            frameCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            crtCamera.GetUniversalAdditionalCameraData().cameraStack.Add(frameCamera);

            // 피벗이 둘이다. 힌지는 베젤 아래 모서리에 놓여 눕혔다 세우는 축이 되고, 그 자식인
            // frame 은 메시의 경계 상자 중심에 놓여 확대의 기준이 된다 — 메시 원점이 가운데라는
            // 보장이 없고, 확대는 가운데를 기준으로 해야 한다.
            frameHinge = new GameObject("CRT Frame Hinge").transform;
            frameHinge.SetParent(frameCamera.transform, false);

            frame = new GameObject("CRT Frame").transform;
            frame.SetParent(frameHinge, false);

            Transform mesh = Instantiate(prefab, frame).transform;

            // FBX 가 어느 축으로 누워 있는지 모르므로 경계 상자로 판단한다. 화면과 나란한 면이
            // XY 가 아니면(=세로 두께가 앞뒤 두께보다 얇으면) 눕혀진 것이라 세워 준다.
            Bounds bounds = MeshBounds(mesh);
            if (bounds.size.y < bounds.size.z)
            {
                mesh.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                bounds = MeshBounds(mesh);
            }

            mesh.position += frame.position - bounds.center; // 경계 상자 중심을 피벗에 맞춘다
            frameWidth = Mathf.Max(0.0001f, bounds.size.x);
            frameHeight = Mathf.Max(0.0001f, bounds.size.y);

            FitFrameCamera();
            frame.localScale = Vector3.one;
            SetFrameAngle(LiftDegrees);
        }

        private static Bounds MeshBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>
        /// 다 세웠을 때 베젤 바깥 테두리가 화면을 덮도록 카메라 거리를 잡고, 힌지를 베젤 아래 모서리에
        /// 둔다. 16:9 창이면 정확히 딱 맞고, 그보다 넓거나 좁은 창에서도 옆이나 위아래에 틈이 없다.
        /// </summary>
        private void FitFrameCamera()
        {
            if (frameCamera == null || frameHinge == null) return;

            float aspect = (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
            float half = Mathf.Tan(FrameFieldOfView * 0.5f * Mathf.Deg2Rad);
            float distance = Mathf.Min(frameHeight, frameWidth / aspect) * 0.5f / half;

            // 눕혔을 때 베젤 위쪽 끝이 카메라에서 멀어지고, 오버슛에서는 반대로 가까워진다.
            frameCamera.nearClipPlane = Mathf.Max(0.01f, distance * 0.05f);
            frameCamera.farClipPlane = distance + frameHeight * 2f;

            frameHinge.localPosition = new Vector3(0f, -frameHeight * 0.5f, distance);
            frame.localPosition = new Vector3(0f, frameHeight * 0.5f, 0f);
        }

        private void SetFrameAngle(float degrees)
        {
            if (frameHinge != null) frameHinge.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }

        private IEnumerator TweenFrameAngle(float from, float to, float seconds, Func<float, float> ease)
        {
            if (frameHinge == null) yield break;

            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                // 오버슛이 목표를 넘어가야 하므로 Unclamped 다 — 0 도를 지나 화면 쪽으로 살짝 젖혀진다.
                SetFrameAngle(Mathf.LerpUnclamped(from, to, ease(elapsed / seconds)));
                yield return null;
            }

            SetFrameAngle(to);
        }

        private IEnumerator TweenFrameScale(float from, float to, float seconds)
        {
            if (frame == null) yield break;

            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                frame.localScale = Vector3.one * Smooth(from, to, elapsed / seconds);
                yield return null;
            }

            frame.localScale = Vector3.one * to;
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
            return Mathf.Lerp(from, to, SmoothStep(t));
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>목표를 조금 지나쳤다 돌아오는 이징(OutBack). 들어 올려 탁 놓는 손맛이 여기서 나온다.</summary>
        private static float Back(float t)
        {
            const float overshoot = 1.70158f;
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
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

        private void SetCurtainAlpha(float alpha)
        {
            if (curtainFill != null) curtainFill.color = new Color(0f, 0f, 0f, alpha);
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

            curtainFill = new GameObject("Fill", typeof(Image)).GetComponent<Image>();
            curtainFill.color = Color.black;
            curtainFill.raycastTarget = true; // 전환 중 클릭이 뒤로 새지 않게 막는다(알파 0 이어도 막힌다)
            curtain = (RectTransform)curtainFill.transform;
            curtain.SetParent(canvasObject.transform, false);
            curtain.anchorMin = Vector2.zero;
            curtain.anchorMax = Vector2.one;
            curtain.offsetMin = Vector2.zero;
            curtain.offsetMax = Vector2.zero;
        }
    }
}
