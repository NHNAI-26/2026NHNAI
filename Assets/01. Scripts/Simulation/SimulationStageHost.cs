using System.Collections;
using Border.Research;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Simulation
{
    /// <summary>
    /// 테스트용 미션 컨트롤 뷰: `01_Main` 위에 `SimulationTest` 씬을 additive 로 얹었다 내린다.
    /// 3D 는 시뮬레이션 카메라의 뷰포트 사각형(<see cref="Camera.rect"/>)으로 화면 가운데에만 그리고,
    /// 가장자리는 <see cref="RocketDesignUI"/> 가 미션 컨트롤 모드로 만드는 UI 가 채운다.
    /// RenderTexture 를 쓰지 않는 이유는 <c>docs/rocket-simulation.md</c> 참고 — 뷰포트 방식이면
    /// <see cref="RocketBuilder"/> 의 집기·기즈모 좌표계가 손대지 않고 그대로 맞는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationStageHost : MonoBehaviour
    {
        private const string SimulationSceneName = "SimulationTest";
        private const string MainCameraTag = "MainCamera";
        private const string UntaggedTag = "Untagged";

        // URP 는 카메라를 (int)depth 로 정렬한다(UniversalRenderPipelineCore). 소수점은 0 과 같은 값이 된다.
        private const float SimulationCameraDepth = 10f;

        private static readonly Color ButtonColor = new(0.22f, 0.26f, 0.31f, 1f);

        private TMP_Text toggleLabel;
        private ResearchOperationUIController research;
        private Camera mainCamera;
        private int mainCameraCullingMask;
        private CameraClearFlags mainCameraClearFlags;
        private RocketDesignUI designUI;
        private bool loaded;
        private bool busy;

        /// <summary>
        /// <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/> 는 플레이 세션당 첫 씬 하나에만 걸린다.
        /// 타이틀에서 시작하면 그때 활성 씬이 `00_Title` 이라 여기서 걸러지고, 뒤늦게 `01_Main` 을
        /// 로드해도 다시 불리지 않아 토글 버튼이 아예 안 생겼다 — 그래서 로드마다 다시 본다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // 도메인 리로드를 끈 설정(Enter Play Mode Options)에서는 정적 구독이 남아 쌓인다.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SpawnInMainScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SpawnInMainScene();

        private static void SpawnInMainScene()
        {
            // 시뮬레이션 씬을 additive 로 얹을 때도 이 콜백이 돈다. 비활성 오브젝트까지 찾지 않으면
            // 잠시 꺼 둔 호스트를 놓쳐 두 번째가 생긴다.
            if (SceneManager.GetActiveScene().name != ResearchFlowSession.MainSceneName
                || FindFirstObjectByType<SimulationStageHost>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            new GameObject("Simulation Stage Host").AddComponent<SimulationStageHost>();
        }

        private void Awake()
        {
            BuildToggle();
        }

        private void BuildToggle()
        {
            var canvasObject = new GameObject("SimulationToggleCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 연구 화면과 미션 컨트롤 UI 모두 위
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var buttonObject = new GameObject("SimulationToggleButton", typeof(RectTransform));
            buttonObject.transform.SetParent(canvasObject.transform, false);

            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-16f, -16f);
            rect.sizeDelta = new Vector2(110f, 40f);

            var image = buttonObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Toggle);

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            toggleLabel = labelObject.AddComponent<TextMeshProUGUI>();
            toggleLabel.text = "시뮬레이션";
            toggleLabel.fontSize = 15;
            toggleLabel.fontStyle = FontStyles.Bold;
            toggleLabel.alignment = TextAlignmentOptions.Center;
            toggleLabel.color = Color.white;
            toggleLabel.raycastTarget = false;

            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void Toggle()
        {
            if (busy) return;

            StartCoroutine(loaded ? UnloadRoutine() : LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            busy = true;

            research = FindFirstObjectByType<ResearchOperationUIController>();
            // 캔버스가 아니라 루트를 끈다 — 미니게임·설계 화면 컨트롤러가 캔버스의 형제로 붙어 있어서
            // 캔버스만 끄면 그것들이 시뮬레이션 위에 남는다.
            if (research != null) research.gameObject.SetActive(false);

            // 두 씬의 카메라가 모두 MainCamera 태그다. 태그를 잠시 떼어 Camera.main 이 시뮬레이션
            // 카메라로만 풀리게 한다 — 잘못 물리면 RocketBuilder.LateUpdate 가 연구 화면 카메라를
            // 매 프레임 로켓 주위로 돌려버린다.
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.tag = UntaggedTag;

                // 이 카메라는 뷰포트 바깥을 채우는 클리어 전용으로 쓴다. 마스크를 비우지 않으면
                // additive 로 올라온 시뮬레이션 씬을 전체 화면으로 한 번 더 그린다 — 배경에 3D 가
                // 비치고 지오메트리도 두 번 그려진다. 끄지는 않는다: 끄면 사각형 바깥이 안 지워진다.
                mainCameraCullingMask = mainCamera.cullingMask;
                mainCameraClearFlags = mainCamera.clearFlags;
                mainCamera.cullingMask = 0;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }

            yield return SceneManager.LoadSceneAsync(SimulationSceneName, LoadSceneMode.Additive);

            Camera simulationCamera = FindSceneCamera(SceneManager.GetSceneByName(SimulationSceneName));
            if (simulationCamera != null)
            {
                simulationCamera.depth = SimulationCameraDepth;
                // 01_Main 카메라의 리스너를 그대로 두고 이쪽을 끈다 — 씬과 함께 사라져 복원할 것이 없다.
                if (simulationCamera.TryGetComponent(out AudioListener listener)) listener.enabled = false;
            }

            designUI = RocketDesignUI.Spawn(true);

            loaded = true;
            busy = false;
            toggleLabel.text = "연구 화면";
        }

        private IEnumerator UnloadRoutine()
        {
            busy = true;

            if (designUI != null) Destroy(designUI.gameObject);
            designUI = null;

            Scene scene = SceneManager.GetSceneByName(SimulationSceneName);
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);

            if (mainCamera != null)
            {
                mainCamera.tag = MainCameraTag;
                mainCamera.cullingMask = mainCameraCullingMask;
                mainCamera.clearFlags = mainCameraClearFlags;
            }
            if (research != null) research.gameObject.SetActive(true);

            loaded = false;
            busy = false;
            toggleLabel.text = "시뮬레이션";
        }

        private static Camera FindSceneCamera(Scene scene)
        {
            if (!scene.isLoaded) return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera camera = root.GetComponentInChildren<Camera>();
                if (camera != null) return camera;
            }

            return null;
        }
    }
}
