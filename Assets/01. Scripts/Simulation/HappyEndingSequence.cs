using System.Collections;
using System.Collections.Generic;
using Border.Audio;
using Border.Core;
using Border.Research;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Simulation
{
    /// <summary>
    /// 최종 미션(<c>LowPowerZoneHold</c>)을 B 이상으로 통과했을 때만 도는 해피엔딩.
    /// 일곱 비트다 — 날짜 카드 → 전화 대사 → 야간 발사 → 달 컷 → 달 항행 → 결과 신문 → 페이드 후 타이틀.
    /// 근거와 결정 이력은 <c>docs/specs/happy-ending-cinematic-spec.md</c>.
    ///
    /// 앞뒤 비트는 프롤로그와 같은 문법이다(검은 화면 + 타이핑 텍스트).
    /// 3D 구간은 <b>`SimulationTest` 씬을 그대로 쓴다</b> — 진짜 발사대에서 진짜 로켓을 올린다.
    /// 그 씬은 <see cref="RocketBuilder"/>·<see cref="RocketDesignUI"/>·<see cref="SkyEnvironment"/> 가
    /// 매 프레임 카메라와 하늘을 덮어쓰므로, <see cref="MissionSuccessPresentation"/> 이 낙하산 연출에서
    /// 쓰는 것과 같은 방식으로 전부 재운 뒤 우리 카메라를 얹는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HappyEndingSequence : MonoBehaviour, IPointerClickHandler
    {
        public const string TitleSceneName = "00_Title";

        /// <summary>`Assets/05. Arts/Texture/Resources` 로 옮겨 둔 크레이터 노이즈 맵.</summary>
        private const string MoonTextureName = "Craters_03-512x512";

        /// <summary>
        /// 발사 시작부터 배기를 고정할 때까지. 씬 값 기준 홀드 2.5 + 보조 상승 2.5 + 물리 전환 1 = 6 초에
        /// 여유를 조금 더한 값이다. 이보다 일찍 고정하면 리프트 연기가 영원히 나온다.
        /// </summary>
        private const float ExhaustFreezeSeconds = 6.5f;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        /// <summary>
        /// `SimulationTest` 씬의 로켓 저작 위치. 발사대(`RocketBase`)가 (0.16, −1, 0.74) 에 있고
        /// 로켓은 그 위 (0.22, 3.61, 0) 에서 시작한다. 재발사는 이 자리로 되돌린 뒤 올린다.
        /// </summary>
        private static readonly Vector3 RocketPadPosition = new(0.22f, 3.61f, 0f);

        private static readonly string[] DefaultPhoneLines =
        {
            "관제소, 여기는 본부입니다.",
            "8년이었습니다. 수고 많으셨습니다.",
            "발사 승인 났습니다.",
            "…이제, 올려보내면 됩니다."
        };

        [SerializeField] private string dateCard = "2026.04";
        [SerializeField, TextArea(1, 3)] private string[] phoneLines = DefaultPhoneLines;

        [Header("타이밍 (총 40~60초 예산)")]
        [SerializeField, Min(0f)] private float lineFadeSeconds = 0.9f;
        [SerializeField, Min(0f)] private float typeSecondsPerChar = 0.06f;
        [SerializeField, Min(0f)] private float dateHoldSeconds = 2.2f;
        [SerializeField, Min(0f)] private float lineHoldSeconds = 2f;
        [SerializeField, Min(0f)] private float revealSeconds = 1.5f;
        // 로켓 자신의 클램프 홀드(2.5초)까지 포함한 발사 비트 전체 길이.
        [SerializeField, Min(0f)] private float launchSeconds = 10f;
        [SerializeField, Min(0f)] private float moonHoldSeconds = 1f;
        [SerializeField, Min(0f)] private float transitSeconds = 10f;
        [SerializeField, Min(0f)] private float finalFadeSeconds = 1.2f;

        // 발사 구간의 소리는 RocketAudio 가 인게임과 똑같이 낸다 — 여기서 따로 재생하지 않는다.
        [Header("사운드 (비우면 무음으로 진행한다)")]
        [SerializeField] private string phoneSfxId = string.Empty;

        private ResearchOperationUIController research;
        private ResearchResultReportController report;
        private ResearchLaunchResultData result;
        private SimulationCrtScreen crt;

        private CanvasGroup overlay;
        private TMP_Text lineText;
        private Camera stageCamera;
        private Transform moon;
        private Rocket rocket;
        private HappyEndingFlight flight;

        private readonly List<Behaviour> suspended = new();
        private readonly List<ParticleSystem> exhaust = new();
        private bool researchWasActive;

        private Coroutine routine;
        private bool advanceRequested;
        private bool newspaperDismissed;
        private bool leaving;

        private Material skyMaterial;
        private Light sun;
        private Color sunColorBackup;
        private float sunIntensityBackup;
        private Color ambientBackup;
        private UnityEngine.Rendering.AmbientMode ambientModeBackup;
        private bool fogBackup;
        private Color fogColorBackup;
        private float fogDensityBackup;
        private FogMode fogModeBackup;
        private bool environmentStored;

        public static HappyEndingSequence Play(
            ResearchOperationUIController research,
            ResearchLaunchResultData result,
            SimulationCrtScreen crt)
        {
            var host = new GameObject("Happy Ending").AddComponent<HappyEndingSequence>();
            host.research = research;
            host.result = result;
            host.crt = crt;
            host.routine = host.StartCoroutine(host.PlayRoutine());
            return host;
        }

        /// <summary>
        /// 배경 Image 의 클릭이 부모인 이 컴포넌트까지 버블링돼 들어온다.
        /// 대사 구간에서만 소비된다 — 3D 구간과 신문은 이 클릭으로 넘어가지 않는다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData) => advanceRequested = true;

        private IEnumerator PlayRoutine()
        {
            BuildOverlay();

            // 이미 떠 있던 신문은 우리 것이 아니다. 결과 보고서는 연구 화면과 <b>별도 루트</b>라
            // 연구 화면을 꺼도 살아남아 3D 구간을 덮는다. 여기서 먼저 닫는다.
            report = FindFirstObjectByType<ResearchResultReportController>(FindObjectsInactive.Include);
            if (report != null) report.Hide();

            // 시뮬레이션을 덮고 있던 CRT 커튼을 걷는다. 그 밑에 이미 우리 검은 오버레이가 서 있어서
            // 화면은 검은 채로 이어진다. 걷지 않으면 커튼이 sortingOrder 최대값으로 엔딩을 통째로 가린다.
            if (crt != null) crt.Uncover();

            if (Application.isPlaying) SoundManager.Instance?.StopBgm();

            // B1 — 날짜 카드. 프롤로그의 `2017.12` 와 짝이라 타이핑 없이 페이드로 뜬다.
            yield return ShowLine(dateCard, dateHoldSeconds, string.Empty, typewriter: false);

            // B2 — 전화 대사. 한 글자씩 찍고, 클릭은 이 구간에서만 먹는다.
            if (phoneLines != null)
            {
                foreach (string line in phoneLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    yield return ShowLine(line, lineHoldSeconds, phoneSfxId, typewriter: true);
                }
            }

            // B3~B5 — 발사대, 달 컷, 달 항행
            yield return StageRoutine();

            // 3D 가 끝났으니 시뮬레이션 씬을 내린다. 신문은 그 위에 뜬다.
            yield return UnloadSimulationScene();

            // B6 — 결과 신문. 기존 UI 를 그대로 쓴다.
            yield return NewspaperRoutine();

            // B7 — 페이드 후 타이틀
            yield return FadeOverlay(1f, finalFadeSeconds);
            LeaveToTitle();
        }

        // ── 3D 구간 ─────────────────────────────────────────────────────────────

        /// <summary>B3~B5. `SimulationTest` 씬을 재운 뒤 그 발사대에서 다시 올린다.</summary>
        private IEnumerator StageRoutine()
        {
            Scene simulation = SceneManager.GetSceneByName(SimulationStageHost.SimulationSceneName);
            if (!simulation.isLoaded)
            {
                // 발사대가 없으면 3D 구간만 버리고 신문으로 넘어간다. 검은 화면에 갇히지 않는 쪽이 먼저다.
                Log.W("[HappyEnding] 시뮬레이션 씬이 없어 3D 구간을 건너뛴다.", this);
                yield break;
            }

            SuspendScene(simulation);
            BuildStageCamera();
            StoreEnvironment();
            ApplyNightSky();
            PrepareRocket();

            float height = Mathf.Max(1f, VisualBounds(rocket != null ? rocket.gameObject : null).size.y);
            Vector3 padTop = rocket != null ? rocket.transform.position : RocketPadPosition;

            // B3 — 야간 발사. 발사대를 옆에서 올려다보는 자리.
            float distance = Mathf.Max(14f, height * 2.2f);
            stageCamera.transform.position = padTop + new Vector3(distance * 0.6f, height * 0.4f, -distance);
            stageCamera.transform.LookAt(padTop + Vector3.up * (height * 0.5f));

            yield return FadeOverlay(0f, revealSeconds);

            // 인게임과 똑같은 발사 절차를 그대로 탄다. 홀드 2.5초 동안 배기가 서서히 세지고 몸통이
            // 꿀렁이며 SparkStart 가 울리고, 이륙과 함께 리프트 연기와 엔진음이 붙는다.
            // 궤적만 HappyEndingFlight 가 가로챈다.
            if (rocket != null)
            {
                flight = HappyEndingFlight.Attach(rocket, -stageCamera.transform.forward);
                rocket.Launch();
            }

            bool frozen = false;
            for (float elapsed = 0f; elapsed < launchSeconds; elapsed += Time.deltaTime)
            {
                // 리프트 구간(홀드 2.5초 + 보조 상승 2.5초 + 물리 전환 1초)이 지나면 배기를 고정한다.
                // 여기까지가 인게임과 같아야 하는 그림이고, 그 뒤로는 연료도 발열도 진행하지 않아
                // 엔딩이 끝날 때까지 불이 꺼지지도 과열로 폭발하지도 않는다.
                if (!frozen && elapsed >= ExhaustFreezeSeconds)
                {
                    frozen = true;
                    FreezeExhaust();
                }

                if (rocket != null) stageCamera.transform.LookAt(rocket.transform.position);
                yield return null;
            }

            if (!frozen) FreezeExhaust();

            // B4 — 달 컷. 카메라를 옮겨 붙이는 컷 전환이라 블렌드하지 않는다.
            yield return MoonCut(height);
        }

        private IEnumerator MoonCut(float rocketHeight)
        {
            // 씬 지오메트리(바다·발사대)가 한 점도 안 보이도록 충분히 위로 올라간다.
            Vector3 spaceOrigin = new(0f, 60000f, 0f);
            stageCamera.transform.SetPositionAndRotation(spaceOrigin, Quaternion.identity);

            // 우주에서는 스카이박스를 성운 큐브맵 쪽으로 완전히 넘긴다 — 별이 여기서 나온다.
            // 지면이 꺼져 아래 반구가 통째로 지평선색이 되므로, 핑크 바닥을 성운이 덮게 한다.
            if (skyMaterial != null)
            {
                skyMaterial.SetFloat("_SpaceBlend", 1f);
                skyMaterial.SetFloat("_SpaceExposure", 1.6f);
                skyMaterial.SetFloat("_MidBlend", 0f);
            }
            RenderSettings.fog = false;

            moon = BuildMoon();
            moon.position = spaceOrigin + new Vector3(-90f, 45f, 1200f);

            Vector3 toMoon = (moon.position - spaceOrigin).normalized;
            EnsureSpaceLight(toMoon);

            // 우주선이 카메라에 훨씬 가깝다. 시작은 우하단에서 화면을 크게 물고 들어오고,
            // 끝나도 여전히 가까운 채로 달 쪽으로 멀어진다.
            Vector3 near = spaceOrigin
                + stageCamera.transform.forward * (rocketHeight * 0.8f)
                + stageCamera.transform.right * (rocketHeight * 0.55f)
                + stageCamera.transform.up * (-rocketHeight * 0.75f);
            Vector3 far = spaceOrigin
                + stageCamera.transform.forward * (rocketHeight * 3.5f)
                + stageCamera.transform.right * (rocketHeight * 0.1f)
                + stageCamera.transform.up * (-rocketHeight * 0.1f);

            if (rocket != null)
            {
                // 여기서부터는 경로를 밖에서 쥔다. 다만 핀은 놓지 않는다 — 놓으면 ReleaseLift 가
                // kinematic 을 풀어 로켓이 제 갈 길로 날아간다.
                if (flight != null) flight.PinPhysicsOnly();

                // 우주에서는 소리를 재운다. OnDisable 이 구독 해제와 재생 중인 루프 정리까지 해 준다.
                foreach (RocketAudio audio in rocket.GetComponentsInChildren<RocketAudio>(true)) Suspend(audio);

                rocket.transform.SetPositionAndRotation(near, Quaternion.FromToRotation(Vector3.up, toMoon));
                ClearParticleTrails();
                KeepExhaustAlive();
            }

            yield return Wait(moonHoldSeconds);

            // B5 — 달 항행
            for (float elapsed = 0f; elapsed < transitSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitSeconds);
                if (rocket != null) rocket.transform.position = Vector3.Lerp(near, far, t);
                yield return null;
            }

            yield return FadeOverlay(1f, revealSeconds);
        }

        /// <summary>
        /// 카메라·캔버스·빌더·하늘을 전부 재운다. 전부 매 프레임 덮어쓰는 것들이라 한 번 세팅으로는
        /// 못 이긴다. <see cref="MissionSuccessPresentation"/> 이 같은 씬에서 쓰는 방식 그대로다.
        /// </summary>
        private void SuspendScene(Scene simulation)
        {
            foreach (GameObject root in simulation.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true)) Suspend(camera);
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true)) Suspend(canvas);
                foreach (RocketBuilder builder in root.GetComponentsInChildren<RocketBuilder>(true)) Suspend(builder);
                foreach (RocketDesignUI ui in root.GetComponentsInChildren<RocketDesignUI>(true)) Suspend(ui);
                foreach (SkyEnvironment sky in root.GetComponentsInChildren<SkyEnvironment>(true)) Suspend(sky);
                foreach (LaunchMissionController mission in root.GetComponentsInChildren<LaunchMissionController>(true)) Suspend(mission);
                foreach (LaunchPhotoCapture photo in root.GetComponentsInChildren<LaunchPhotoCapture>(true)) Suspend(photo);
            }

            // 연구 화면은 Screen Space Overlay 라 카메라를 꺼도 그려진다. 루트째로 꺼야 한다.
            if (research != null && research.gameObject.activeSelf)
            {
                researchWasActive = true;
                research.gameObject.SetActive(false);
            }

            // 01_Main 쪽 카메라도 재운다.
            foreach (Camera camera in Camera.allCameras) Suspend(camera);
        }

        private void Suspend(Behaviour behaviour)
        {
            if (behaviour == null || !behaviour.enabled || behaviour == stageCamera) return;
            suspended.Add(behaviour);
            behaviour.enabled = false;
        }

        private void BuildStageCamera()
        {
            // Untagged 로 둔다. MainCamera 를 달면 `Camera.main` 이 이쪽으로 풀려 설계 조작
            // 좌표계가 어긋난다(RocketBuilder 의 같은 이유 주석 참고).
            stageCamera = new GameObject("Happy Ending Camera", typeof(Camera)).GetComponent<Camera>();
            stageCamera.clearFlags = CameraClearFlags.Skybox; // 밤하늘을 그대로 보여준다
            stageCamera.depth = 100f;
            stageCamera.fieldOfView = 55f;
            stageCamera.nearClipPlane = 0.1f;
            stageCamera.farClipPlane = 30000f;
        }

        /// <summary>
        /// <see cref="SkyEnvironment"/> 는 <c>OnDisable</c> 이 없고 <c>Unbind</c> 는 <c>OnDestroy</c> 에서만
        /// 돈다. 재우면 마지막 값이 그대로 굳으므로, 그 뒤에 직접 넣은 값을 아무도 덮어쓰지 않는다.
        /// 값은 프로젝트에 이미 있는 밤 프리셋 `ResearchLabNightSky.mat` 을 좌표로 삼았다.
        /// </summary>
        private void ApplyNightSky()
        {
            skyMaterial = RenderSettings.skybox;
            if (skyMaterial != null)
            {
                // 지평선 핑크 → 중간 보라 → 천정 남색. 중간색은 셰이더의 선택 경로라 _MidBlend 를
                // 켜야 나온다.
                skyMaterial.SetColor("_HorizonColor", new Color(0.95f, 0.78f, 0.63f).linear);
                skyMaterial.SetColor("_MidColor", new Color(0.56f, 0.53f, 0.75f).linear);
                skyMaterial.SetColor("_SkyTint", new Color(0.14f, 0.14f, 0.31f).linear);
                skyMaterial.SetFloat("_MidBlend", 1f);
                // 발사대 카메라는 거의 수평이라 화면에 들어오는 dir.y 가 0~0.3 뿐이다. 두께를 키우면
                // 그 좁은 띠가 통째로 지평선색이 되어 하늘이 한 색으로 뭉갠다. 1 보다 낮춰서
                // 그라디언트를 지평선 쪽으로 압축해야 보라와 남색이 화면 안으로 들어온다.
                skyMaterial.SetFloat("_AtmosphereThickness", 1f);
                skyMaterial.SetFloat("_Exposure", 0.85f);
                // 큐브맵이 파란 성운이라 많이 섞으면 핑크를 먹는다. 별만 남을 만큼만 섞는다.
                skyMaterial.SetFloat("_SpaceBlend", 0.15f);
                skyMaterial.SetFloat("_SpaceExposure", 2.2f);
            }

            if (sun != null)
            {
                // 해가 막 넘어간 박명. 완전한 달빛 청색보다 살짝 따뜻하게 둬야 지평선 복숭아빛과 붙는다.
                sun.color = new Color(0.78f, 0.72f, 0.86f);
                sun.intensity = 0.2f;
            }

            // 씬 앰비언트는 Skybox 모드인데 기본 스카이박스에서 구워진 값에 고정돼 있고 아무도
            // 갱신하지 않는다. 이걸 안 바꾸면 하늘만 밤이고 지면은 대낮이다.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.11f, 0.16f);
            // 안개는 직접 켠다. SkyEnvironment 를 재우기 전에 그쪽이 써 둔 값에 기대면, 로켓이 높이
            // 떠 있는 상태에서 엔딩에 들어왔을 때 밀도 커브가 이미 0 이라 안개가 아예 없다.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.007f;
            // 지평선색과 맞춘다. 안 맞추면 먼 수면만 딴 색 안개로 남는다.
            RenderSettings.fogColor = new Color(0.50f, 0.38f, 0.38f);
        }

        private void PrepareRocket()
        {
            rocket = FindFirstObjectByType<Rocket>();
            if (rocket == null)
            {
                Log.W("[HappyEnding] 로켓을 찾지 못했다. 발사 없이 카메라만 돈다.", this);
                return;
            }

            // 델리게이트가 false 를 돌려주면 Launch 가 무시된다.
            rocket.AuthorizeLaunch = null;
            rocket.StopFlight();
            rocket.ResetFlight(RocketPadPosition, Quaternion.identity);

            // Rocket 도 RocketPart 도 재우지 않는다. 화염·흔들림·리프트 연기·사운드가 전부 이쪽에서
            // 나오고, 특히 엔진 점화는 Rocket.Launch() 안의 Prepare 에서만 일어난다.
            // 물리는 HappyEndingFlight 가 매 스텝 kinematic 으로 묶어 막는다.
        }

        private void EnsureSpaceLight(Vector3 towardMoon)
        {
            // 발사대 조명(씬의 태양)은 저 아래에 있어 우주까지 오지 않는다. 여기 키 하나로 달과
            // 로켓을 같이 비춘다.
            //
            // 빛이 나아가는 방향은 Light 의 forward 다. 달 반대쪽(-towardMoon)을 보게 하면 빛이
            // 달 뒤통수를 때려 우리가 보는 앞면이 통째로 그림자가 된다. 달 쪽(+towardMoon)을 보되
            // 옆으로 35도 틀어, 앞면을 비추면서 명암 경계가 남게 한다.
            var key = new GameObject("Happy Ending Moon Key", typeof(Light)).GetComponent<Light>();
            key.transform.SetParent(stageCamera.transform, false);
            key.type = LightType.Directional;
            key.color = new Color(0.95f, 0.96f, 1f);
            key.intensity = 1.6f;
            key.transform.rotation = Quaternion.LookRotation(
                Quaternion.AngleAxis(35f, Vector3.up) * towardMoon);
        }

        /// <summary>
        /// 표면은 프로젝트에 이미 있던 크레이터 노이즈 맵을 쓴다(`05. Arts/Texture/Noise/Craters`
        /// 에서 Resources 로 옮겨 둔 것). Lit 이라 <see cref="EnsureSpaceLight"/> 의 키 라이트가
        /// 명암 경계를 만들어 평면 원이 아니라 구체로 보인다.
        /// </summary>
        private Transform BuildMoon()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Happy Ending Moon";
            sphere.transform.localScale = Vector3.one * 420f;
            Destroy(sphere.GetComponent<Collider>());

            var surface = Resources.Load<Texture2D>(MoonTextureName);
            Shader shader = Shader.Find(surface != null
                ? "Universal Render Pipeline/Lit"
                : "Universal Render Pipeline/Unlit");
            if (shader == null) return sphere.transform;

            var material = new Material(shader) { color = new Color(0.82f, 0.80f, 0.76f) };
            if (surface != null)
            {
                material.SetTexture(BaseMapId, surface);
                material.SetFloat(SmoothnessId, 0.05f);
                material.SetFloat(MetallicId, 0f);
            }
            else
            {
                Log.W($"[HappyEnding] 달 표면 텍스처 '{MoonTextureName}' 를 찾지 못해 단색으로 그린다.", this);
            }

            sphere.GetComponent<Renderer>().sharedMaterial = material;
            return sphere.transform;
        }

        /// <summary>
        /// 지금 뿜고 있는 배기를 기억해 두고 <see cref="Rocket"/> 을 재운다. 그 순간부터 연료도 발열도
        /// 진행하지 않으므로 화염이 꺼지지도, 과열로 폭발하지도 않는다 — 엔딩이 끝날 때까지 계속 탄다.
        ///
        /// 이걸 이륙 직후가 아니라 리프트 구간이 끝난 뒤에 부르는 이유: <see cref="RocketLiftSmoke"/> 가
        /// <c>LiftAssistActive</c> 를 보는데, 그 전에 재우면 값이 true 로 굳어 리프트 연기가 영원히 나온다.
        ///
        /// 뿜는 중인 것만 골라 담으므로 점화 실패용 <c>Smoke_Fail</c> 같은 것은 딸려오지 않는다.
        /// </summary>
        private void FreezeExhaust()
        {
            if (rocket == null) return;

            exhaust.Clear();
            foreach (ParticleSystem particles in rocket.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particles.isEmitting) exhaust.Add(particles);
            }

            Suspend(rocket);
        }

        /// <summary>컷 전환이나 순간이동 뒤에 배기가 멎었으면 다시 켠다.</summary>
        private void KeepExhaustAlive()
        {
            foreach (ParticleSystem particles in exhaust)
            {
                if (particles != null && !particles.isEmitting) particles.Play(true);
            }
        }

        /// <summary>
        /// 컷 전환에서 로켓을 순간이동시키므로 잔상만 지운다. 재생 상태는 건드리지 않는다 —
        /// 켜고 끄는 판단은 <see cref="FreezeExhaust"/> 이전에는 <see cref="RocketPart"/> 의 몫이다.
        /// </summary>
        private void ClearParticleTrails()
        {
            if (rocket == null) return;
            foreach (ParticleSystem particles in rocket.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Clear(true);
            }
        }

        private IEnumerator UnloadSimulationScene()
        {
            Scene simulation = SceneManager.GetSceneByName(SimulationStageHost.SimulationSceneName);
            if (simulation.isLoaded) yield return SceneManager.UnloadSceneAsync(simulation);

            // 씬과 함께 사라진 것들은 리스트에서 조용히 빠진다(널 검사로 거른다).
            TearDownStage();
        }

        private void TearDownStage()
        {
            if (flight != null) Destroy(flight);
            flight = null;
            exhaust.Clear();
            if (moon != null) Destroy(moon.gameObject);
            moon = null;
            if (stageCamera != null) Destroy(stageCamera.gameObject);
            stageCamera = null;
            rocket = null;

            foreach (Behaviour behaviour in suspended)
            {
                if (behaviour != null) behaviour.enabled = true;
            }
            suspended.Clear();

            RestoreEnvironment();
        }

        // ── 신문과 종료 ─────────────────────────────────────────────────────────

        private IEnumerator NewspaperRoutine()
        {
            yield return FadeOverlay(1f, 0f);

            if (research == null)
            {
                Log.W("[HappyEnding] 연구 화면이 없어 결과 신문을 건너뛴다.", this);
                yield break;
            }

            newspaperDismissed = false;
            // 신문을 닫는 경로가 여럿이라(OnEnable 의 Refresh, 보고서 콜백, 설계 화면 복귀) 그것들이
            // 모두 모이는 ShowEndingScreen 한 곳을 가로챈다. 그러지 않으면 기존 MISSION COMPLETE
            // 패널이 엔딩을 가로채고 타이틀로 나가지 못한다.
            research.SetEndingOverride(() => newspaperDismissed = true);

            if (!research.gameObject.activeSelf) research.gameObject.SetActive(true);
            researchWasActive = false;

            // 루트를 켜면 OnEnable 의 Refresh 가 미확인 결과를 보고 스스로 신문을 연다. 그건 <b>이전
            // 발사</b>의 기사다. 닫지 않으면 ShowResultReport 가 "이미 떠 있음" 으로 조기 반환해서
            // 성공 기사로 바뀌지도, 등장 애니메이션이 다시 돌지도 않는다.
            if (report != null) report.Hide();

            // 이제 우리 결과로 연다. Initialize 가 기사를 다시 만들고 글자 등장 연출을 처음부터 돌린다.
            research.ShowLaunchResultOverlay(result, () => newspaperDismissed = true);

            yield return null;

            // 신문이 끝내 안 열렸으면 기다리지 않는다. 여기서 멈추면 플레이어가 빠져나갈 길이 없다.
            if (report != null && !report.gameObject.activeSelf)
            {
                Log.W("[HappyEnding] 결과 신문이 열리지 않아 신문 비트를 건너뛴다.", this);
                yield break;
            }

            // 신문을 덮지 않도록 오버레이를 걷고, 클릭도 신문으로 흘려보낸다.
            overlay.blocksRaycasts = false;
            yield return FadeOverlay(0f, 0.6f);

            while (!newspaperDismissed) yield return null;

            overlay.blocksRaycasts = true;
        }

        private void LeaveToTitle()
        {
            if (leaving) return;
            leaving = true;
            routine = null;
            RestoreEnvironment();

            // 세션 초기화는 하지 않는다. TitleMenu.NewGame 이 PrepareNewGame 으로 이미 처리한다.
            SceneManager.LoadScene(TitleSceneName);
        }

        // ── 오버레이와 텍스트 ───────────────────────────────────────────────────

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("Happy Ending Overlay", typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // CRT 커튼(short.MaxValue) 바로 아래. 커튼을 걷으면 이 검은 화면이 그대로 이어받는다.
            canvas.sortingOrder = short.MaxValue - 1;

            overlay = canvasObject.AddComponent<CanvasGroup>();
            overlay.alpha = 1f;
            overlay.blocksRaycasts = true;

            var backdrop = new GameObject("Backdrop", typeof(Image)).GetComponent<Image>();
            backdrop.color = Color.black;
            backdrop.raycastTarget = true; // 알파 0 이어도 클릭을 받는다
            Stretch((RectTransform)backdrop.transform, canvasObject.transform);

            lineText = new GameObject("Line", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            lineText.alignment = TextAlignmentOptions.Center;
            lineText.fontSize = 48f;
            lineText.color = Color.white;
            lineText.alpha = 0f;
            lineText.raycastTarget = false;
            Stretch((RectTransform)lineText.transform, canvasObject.transform);
        }

        private static void Stretch(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private IEnumerator ShowLine(string line, float hold, string sfxId, bool typewriter)
        {
            if (string.IsNullOrEmpty(line)) yield break;

            advanceRequested = false; // 이전 줄에서 남은 클릭이 이 줄을 넘기지 않게 한다
            lineText.text = line;
            PlaySfx(sfxId);

            if (typewriter)
            {
                lineText.alpha = 1f;
                yield return TypeText(line);
            }
            else
            {
                lineText.maxVisibleCharacters = int.MaxValue;
                yield return FadeText(0f, 1f, lineFadeSeconds);
            }

            yield return WaitOrAdvance(hold);
            yield return FadeText(1f, 0f, lineFadeSeconds);
        }

        /// <summary>
        /// 글자를 하나씩 드러낸다. 알파가 아니라 <see cref="TMP_Text.maxVisibleCharacters"/> 만 올리므로
        /// 레이아웃이 처음부터 확정돼 줄이 늘어날 때 텍스트가 위아래로 튀지 않는다 — 프롤로그가 같은
        /// 이유로 이 방식을 쓴다. 다만 타건음은 깔지 않는다. 해피엔딩의 전화 대사는 무음으로 간다.
        /// </summary>
        private IEnumerator TypeText(string line)
        {
            lineText.maxVisibleCharacters = 0;

            if (typeSecondsPerChar > 0f)
            {
                // 리치 텍스트를 쓰지 않으므로 파싱된 글자 수와 문자열 길이가 같다.
                int total = line.Length;
                float elapsed = 0f;

                while (lineText.maxVisibleCharacters < total)
                {
                    // 타이핑 중 클릭은 그 줄을 즉시 다 드러낸다. 뒤의 유지 시간은 그대로 남는다.
                    if (ConsumeAdvance()) break;
                    elapsed += Time.unscaledDeltaTime;
                    lineText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(elapsed / typeSecondsPerChar));
                    yield return null;
                }
            }

            lineText.maxVisibleCharacters = int.MaxValue;
        }

        private IEnumerator FadeText(float from, float to, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                lineText.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }

            lineText.alpha = to;
        }

        private IEnumerator FadeOverlay(float to, float seconds)
        {
            float from = overlay.alpha;
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                overlay.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }

            overlay.alpha = to;
        }

        /// <summary>대사 구간의 유지 시간. 클릭이 들어오면 즉시 다음 줄로 넘어간다.</summary>
        private IEnumerator WaitOrAdvance(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                if (ConsumeAdvance()) yield break;
                yield return null;
            }
        }

        /// <summary>3D 구간의 대기. 클릭을 보지 않는다 — 연출은 끝까지 재생된다.</summary>
        private static IEnumerator Wait(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        private bool ConsumeAdvance()
        {
            if (!advanceRequested) return false;
            advanceRequested = false;
            return true;
        }

        // ── 환경 백업 ───────────────────────────────────────────────────────────

        private void StoreEnvironment()
        {
            if (environmentStored) return;

            sun = FindSun();
            if (sun != null)
            {
                sunColorBackup = sun.color;
                sunIntensityBackup = sun.intensity;
            }

            ambientModeBackup = RenderSettings.ambientMode;
            ambientBackup = RenderSettings.ambientLight;
            fogBackup = RenderSettings.fog;
            fogColorBackup = RenderSettings.fogColor;
            fogDensityBackup = RenderSettings.fogDensity;
            fogModeBackup = RenderSettings.fogMode;
            environmentStored = true;
        }

        private void RestoreEnvironment()
        {
            if (!environmentStored) return;

            if (sun != null)
            {
                sun.color = sunColorBackup;
                sun.intensity = sunIntensityBackup;
            }
            sun = null;

            RenderSettings.ambientMode = ambientModeBackup;
            RenderSettings.ambientLight = ambientBackup;
            RenderSettings.fog = fogBackup;
            RenderSettings.fogColor = fogColorBackup;
            RenderSettings.fogDensity = fogDensityBackup;
            RenderSettings.fogMode = fogModeBackup;
            skyMaterial = null;
            environmentStored = false;
        }

        private static Light FindSun()
        {
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional && light.enabled) return light;
            }

            return null;
        }

        private static Bounds VisualBounds(GameObject target)
        {
            if (target == null) return new Bounds(Vector3.zero, Vector3.one);

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(target.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void PlaySfx(string id)
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(id)) return;
            SoundManager.Instance?.PlaySfx(id);
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            TearDownStage();

            if (researchWasActive && research != null)
            {
                researchWasActive = false;
                research.gameObject.SetActive(true);
            }
        }
    }
}
