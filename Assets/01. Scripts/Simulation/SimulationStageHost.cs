using System.Collections;
using Border.Research;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulation
{
    /// <summary>
    /// 테스트용 미션 컨트롤 뷰: `01_Main` 위에 `SimulationTest` 씬을 additive 로 얹었다 내린다.
    /// 3D 는 시뮬레이션 카메라의 뷰포트 사각형(<see cref="Camera.rect"/>)으로 화면 가운데에만 그리고,
    /// 가장자리는 그 씬에 함께 들어오는 <see cref="RocketDesignUI"/> 프리팹 인스턴스가 채운다.
    /// 이 큰 화면이 RenderTexture 를 쓰지 않는 이유는 <c>docs/rocket-simulation.md</c> 참고 —
    /// 뷰포트 방식이면 <see cref="RocketBuilder"/> 의 집기·기즈모 좌표계가 손대지 않고 그대로 맞는다.
    /// 발사 후 우하단 작은 화면은 예외로 RenderTexture 다 — 입력을 전혀 받지 않는 표시 전용이라
    /// 그 근거가 걸리지 않는다.
    /// 들고 나는 문은 둘 다 밖에 있다: 연구 화면의 `설계 진입` 버튼이 <see cref="OpenDesignStage"/> 를,
    /// 미션 컨트롤 상단바의 `연구 화면` 버튼이 <see cref="CloseDesignStage"/> 를 부른다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationStageHost : MonoBehaviour
    {
        private const string SimulationSceneName = "SimulationTest";
        private const string MainCameraTag = "MainCamera";
        private const string UntaggedTag = "Untagged";

        // URP 는 카메라를 (int)depth 로 정렬한다(UniversalRenderPipelineCore). 소수점은 0 과 같은 값이 된다.
        private const float SimulationCameraDepth = 10f;

        [SerializeField, Min(0f)] private float launchResultHoldSeconds = 3f;

        private ResearchOperationUIController research;
        private Camera mainCamera;
        private int mainCameraCullingMask;
        private CameraClearFlags mainCameraClearFlags;
        private RocketDesignUI designUI;
        private bool loaded;
        private bool busy;
        private LaunchMissionController mission;
        private LaunchPhotoCapture photoCapture;
        private SimulationCrtScreen crt;
        public string LaunchMessage { get; private set; }

        public static bool OpenDesignStage()
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            SimulationStageHost host = FindFirstObjectByType<SimulationStageHost>();
            if (host == null)
            {
                host = new GameObject("Simulation Stage Host").AddComponent<SimulationStageHost>();
            }

            return host.OpenDesignStageInternal();
        }

        /// <summary>
        /// 설계 화면을 내리고 연구 화면으로 돌아간다. 코루틴은 호스트가 돌린다 —
        /// <see cref="UnloadRoutine"/> 이 버튼을 품은 씬을 내리므로 버튼 쪽에서 돌리면 첫 프레임에 끊긴다.
        /// </summary>
        public static void CloseDesignStage()
        {
            SimulationStageHost host = FindFirstObjectByType<SimulationStageHost>();
            if (host == null || !host.loaded || host.busy)
            {
                return;
            }

            host.StartCoroutine(host.UnloadRoutine());
        }

        private bool OpenDesignStageInternal()
        {
            if (busy)
            {
                return false;
            }

            if (loaded)
            {
                return true;
            }

            StartCoroutine(LoadRoutine());
            return true;
        }

        private IEnumerator LoadRoutine()
        {
            busy = true;

            // 화면부터 덮는다 — 연구 화면이 꺼지고 시뮬레이션 씬이 올라오는 사이가 그대로 보이면
            // CRT 가 켜지기도 전에 화면이 한 번 튄다.
            if (crt == null) crt = gameObject.AddComponent<SimulationCrtScreen>();
            crt.Cover();

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

            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            Rocket rocket = FindFirstObjectByType<Rocket>();
            if (rocket != null)
            {
                photoCapture = rocket.gameObject.AddComponent<LaunchPhotoCapture>();
                photoCapture.Initialize(rocket, simulationCamera, session);
                mission = rocket.gameObject.AddComponent<LaunchMissionController>();
                mission.Initialize(session.HasPendingDesignEntry ? session.PendingDesignEntry.MissionId : session.Model.GetCurrentMission(),
                    () => BeginLaunch(rocket), CompleteLaunch);
            }
            // 설계 UI 는 SimulationTest 씬에 프리팹 인스턴스로 놓여 있다 — 씬과 함께 들어오고 나간다.
            designUI = FindFirstObjectByType<RocketDesignUI>();

            // 합성 경로를 세운 뒤 베젤을 밑에서 들어 올리고, 안착한 다음 브라운관을 켜고, 마지막에
            // 베젤을 확대해 화면 밖으로 밀어낸다.
            if (designUI != null) crt.Begin(mainCamera, simulationCamera, designUI);
            yield return crt.PowerOnRoutine();

            loaded = true;
            busy = false;
        }

        private IEnumerator UnloadRoutine()
        {
            busy = true;
            while (photoCapture != null && photoCapture.IsCapturing) yield return null;

            // 진입의 역순으로(베젤 축소 → 소등 → 베젤 하강) 덮는다. 합성 경로가 시뮬레이션 씬의 카메라와 캔버스를 물고 있어서
            // 씬을 내리기 전에 풀어야 한다.
            if (crt != null) yield return crt.PowerOffRoutine();

            // 씬을 내리면 UI 도 같이 사라진다 — 따로 파괴하지 않는다.
            designUI = null;

            Scene scene = SceneManager.GetSceneByName(SimulationSceneName);
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);

            if (mainCamera != null)
            {
                mainCamera.tag = MainCameraTag;
                mainCamera.cullingMask = mainCameraCullingMask;
                mainCamera.clearFlags = mainCameraClearFlags;
            }
            if (research != null)
            {
                research.gameObject.SetActive(true);
                // 여기서 연구 화면의 등장 애니메이션(PlayEnter(Hub), 0.55초)이 시작된다.
                research.ReturnFromDesignScreen();
            }

            // 복귀한 화면이 한 프레임 제자리를 잡은 뒤 덮개를 걷는다. 같은 프레임에 걷으면 아직 갱신되지
            // 않은 화면이 한 장 스치고, 슬라이드로 걷으면 등장 애니메이션이 그 검은 판 밑에서 다 소모된다.
            // 결과 UI 는 씬을 내리기 전에 이미 닫혔으므로 결과 이벤트를 여기서 다시 발행하지 않는다.
            yield return null;
            if (crt != null) crt.Uncover();

            ResearchFlowSession.GetOrCreate().ClearPendingDesignEntry();
            mission = null;
            photoCapture = null;

            loaded = false;
            busy = false;
        }

        private bool BeginLaunch(Rocket rocket)
        {
            var session = ResearchFlowSession.GetOrCreate();
            if (!session.HasPendingDesignEntry)
            {
                LaunchMessage = "연구 화면에서 설계에 진입해주세요.";
                return false;
            }
            var builder = FindFirstObjectByType<RocketBuilder>();
            var counts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
            var parts = rocket.GetComponentsInChildren<RocketPart>();
            if (parts.Length == 0)
            {
                LaunchMessage = "엔진을 1개 이상 설치해주세요.";
                return false;
            }
            foreach (RocketPart part in parts)
            {
                int slot = -1;
                if (part.Stats != null && builder.PresetLibrary != null)
                    for (int i = 0; i < builder.PresetLibrary.Slots.Count && i < counts.Length; i++)
                        if (builder.PresetLibrary.Slots[i] == part.Stats) { slot = i; break; }
                if (slot < 0)
                {
                    LaunchMessage = "연구 프리셋으로 엔진을 설치해주세요.";
                    return false;
                }
                counts[slot]++;
            }
            var entry = session.PendingDesignEntry;
            session.UpdatePendingDesignEntry(session.Model.CreateDesignEntry(entry.MissionId, entry.SelectedEnginePresetId,
                counts, entry.DesignFit, entry.Visibility, entry.LaunchCostPaid, entry.LaunchCost));
            ResearchActionResult result = session.TryBeginPendingDesignLaunch();
            LaunchMessage = session.Model.LastMessage;
            return result == ResearchActionResult.Success;
        }

        private void CompleteLaunch(bool succeeded)
        {
            var session = ResearchFlowSession.GetOrCreate();
            if (session.CompleteActiveLaunch(succeeded, mission != null ? mission.Status : null,
                mission != null ? mission.TerminationReason : LaunchTerminationReason.Unknown,
                out ResearchLaunchResultData result) != ResearchActionResult.Success) return;
            // Keep the result and photograph until the player dismisses its newspaper.
            // 등급은 여기서만 확정된다 — 발사 정보 패널이 스스로 알 수 없으므로 건네준다.
            if (designUI != null) designUI.ShowLaunchResult(result.Grade);
            // 최종 미션을 통과한 발사만 여기서 갈린다. 낙하산 6초와 결과 신문을 건너뛰고 곧바로
            // 해피엔딩으로 들어간다 — 신문은 엔딩의 마지막 비트에서 한 번만 나온다.
            // docs/specs/happy-ending-cinematic-spec.md (UD-004, R-014)
            if (result.FinalMissionWon && research != null)
            {
                StartCoroutine(HappyEndingRoutine(result));
                return;
            }
            StartCoroutine(HoldThenUnload(succeeded));
        }

        /// <summary>
        /// 시뮬레이션 씬을 내리되 연구 화면을 되살리지 않고 엔딩에 넘긴다. 로켓의 시각 계층은
        /// 언로드 <b>전에</b> 복제해 둔다 — 파트 배치는 직렬화되지 않아 씬과 함께 사라진다.
        /// </summary>
        private IEnumerator HappyEndingRoutine(ResearchLaunchResultData result)
        {
            busy = true;
            while (photoCapture != null && photoCapture.IsCapturing) yield return null;

            GameObject rocketVisual = HappyEndingSequence.PreserveRocket(FindFirstObjectByType<Rocket>());

            if (crt != null) yield return crt.PowerOffRoutine();
            designUI = null;

            Scene scene = SceneManager.GetSceneByName(SimulationSceneName);
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);

            if (mainCamera != null)
            {
                mainCamera.tag = MainCameraTag;
                mainCamera.cullingMask = mainCameraCullingMask;
                mainCamera.clearFlags = mainCameraClearFlags;
            }

            ResearchFlowSession.GetOrCreate().ClearPendingDesignEntry();
            mission = null;
            photoCapture = null;
            loaded = false;
            busy = false;

            // 커튼은 걷지 않는다 — 엔딩이 자기 검은 화면을 세운 뒤 직접 걷는다.
            HappyEndingSequence.Play(rocketVisual, research, result, crt);
        }

        /// <summary>
        /// 최종 수치를 읽을 시간을 준 뒤 정리한다. 곧바로 언로드하면 발사 정보 패널의 마지막 값과
        /// 성공·등급이 한 프레임 스치고 사라진다.
        /// </summary>
        private IEnumerator HoldThenUnload(bool succeeded)
        {
            // 대기 중에도 복귀 버튼이 살아 있으면 CloseDesignStage 가 두 번째 언로드를 띄운다.
            busy = true;
            var presentation = succeeded && mission != null
                ? mission.GetComponentInChildren<MissionSuccessPresentation>() : null;
            Camera camera = FindSceneCamera(SceneManager.GetSceneByName(SimulationSceneName));
            if (presentation != null && presentation.Begin(camera, mission.CompletionVelocity))
            {
                try
                {
                    double startedAt = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - startedAt < MissionSuccessPresentation.Duration)
                    {
                        presentation.Evaluate((float)(Time.realtimeSinceStartupAsDouble - startedAt));
                        yield return null;
                    }
                    presentation.Evaluate(MissionSuccessPresentation.Duration);
                }
                finally
                {
                    if (presentation != null) presentation.End();
                }
                yield return ShowResultBeforeUnload();
                yield break;
            }
            yield return new WaitForSecondsRealtime(launchResultHoldSeconds);
            yield return ShowResultBeforeUnload();
        }

        private IEnumerator ShowResultBeforeUnload()
        {
            while (photoCapture != null && photoCapture.IsCapturing) yield return null;

            if (research == null)
            {
                yield return UnloadRoutine();
                yield break;
            }

            research.ShowLaunchResultOverlay(
                ResearchFlowSession.GetOrCreate().LastLaunchResult,
                ContinueUnloadAfterResult);
        }

        private void ContinueUnloadAfterResult()
        {
            if (!loaded) return;
            StartCoroutine(UnloadRoutine());
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
