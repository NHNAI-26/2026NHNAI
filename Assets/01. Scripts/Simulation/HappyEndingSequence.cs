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
    /// 앞뒤 비트는 프롤로그와 같은 문법이다(검은 화면 + 페이드 텍스트 + 클릭 스킵).
    /// 무대는 전부 런타임에 세운다 — 씬이나 프리팹을 건드리지 않으므로 `01_Main` 이 더러워지지 않는다.
    /// 그 대가로 발사대·달은 프리미티브 자리표시자다. 룩은 에디터에서 교체할 몫이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HappyEndingSequence : MonoBehaviour, IPointerClickHandler
    {
        public const string TitleSceneName = "00_Title";

        /// <summary>무대를 세울 자리. `01_Main` 의 연구실과 겹치지 않게 멀리 잡는다.</summary>
        private static readonly Vector3 StageOrigin = new(0f, -20000f, 0f);

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
        [SerializeField, Min(0f)] private float dateHoldSeconds = 2.2f;
        [SerializeField, Min(0f)] private float lineHoldSeconds = 2f;
        [SerializeField, Min(0f)] private float revealSeconds = 1.5f;
        [SerializeField, Min(0f)] private float padHoldSeconds = 1.5f;
        [SerializeField, Min(0f)] private float launchSeconds = 6f;
        [SerializeField, Min(0f)] private float moonHoldSeconds = 1f;
        [SerializeField, Min(0f)] private float transitSeconds = 10f;
        [SerializeField, Min(0f)] private float finalFadeSeconds = 1.2f;

        [Header("사운드 (비우면 무음으로 진행한다)")]
        [SerializeField] private string phoneSfxId = string.Empty;
        [SerializeField] private string launchSfxId = string.Empty;

        private ResearchOperationUIController research;
        private ResearchLaunchResultData result;
        private SimulationCrtScreen crt;

        private CanvasGroup overlay;
        private TMP_Text lineText;
        private GameObject stage;
        private Camera stageCamera;
        private GameObject rocket;
        private Transform pad;
        private Transform moon;
        private readonly List<Camera> silencedCameras = new();

        private Coroutine routine;
        private bool skipRequested;
        private bool newspaperDismissed;
        private bool leaving;

        private Color ambientBackup;
        private UnityEngine.Rendering.AmbientMode ambientModeBackup;
        private bool fogBackup;
        private bool ambientStored;

        /// <summary>
        /// 발사에 쓰인 실제 로켓의 시각 계층만 복제해 둔다. 시뮬레이션 씬을 내리기 <b>전에</b> 불러야 한다 —
        /// 파트 배치는 어디에도 직렬화되지 않아서 씬과 함께 사라진다.
        /// </summary>
        public static GameObject PreserveRocket(Rocket source)
        {
            if (source == null) return null;

            GameObject clone = Instantiate(source.gameObject);
            clone.name = "Happy Ending Rocket";
            clone.transform.SetParent(null, true);

            // 물리와 게임 로직을 전부 떼어 낸다. 남기면 엔딩 도중 로켓이 스스로 떨어지거나 폭발한다.
            foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(behaviour);
            foreach (Rigidbody body in clone.GetComponentsInChildren<Rigidbody>(true)) Destroy(body);
            foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach (AudioSource audio in clone.GetComponentsInChildren<AudioSource>(true)) Destroy(audio);

            clone.SetActive(false);
            DontDestroyOnLoad(clone);
            return clone;
        }

        public static HappyEndingSequence Play(
            GameObject preservedRocket,
            ResearchOperationUIController research,
            ResearchLaunchResultData result,
            SimulationCrtScreen crt)
        {
            var host = new GameObject("Happy Ending").AddComponent<HappyEndingSequence>();
            host.rocket = preservedRocket;
            host.research = research;
            host.result = result;
            host.crt = crt;
            host.routine = host.StartCoroutine(host.PlayRoutine());
            return host;
        }

        /// <summary>배경 Image 의 클릭이 부모인 이 컴포넌트까지 버블링돼 들어온다.</summary>
        public void OnPointerClick(PointerEventData eventData) => skipRequested = true;

        private IEnumerator PlayRoutine()
        {
            BuildOverlay();

            // 시뮬레이션을 덮고 있던 CRT 커튼을 걷는다. 그 밑에 이미 우리 검은 오버레이가 서 있어서
            // 화면은 검은 채로 이어진다. 걷지 않으면 커튼이 sortingOrder 최대값으로 엔딩을 통째로 가린다.
            if (crt != null) crt.Uncover();

            if (Application.isPlaying) SoundManager.Instance?.StopBgm();

            // B1 — 날짜 카드
            yield return ShowLine(dateCard, dateHoldSeconds, string.Empty);

            // B2 — 전화 대사
            if (phoneLines != null)
            {
                foreach (string line in phoneLines)
                {
                    if (skipRequested) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    yield return ShowLine(line, lineHoldSeconds, phoneSfxId);
                }
            }

            if (!skipRequested)
            {
                yield return StageRoutine();
            }

            // B6 — 결과 신문. 기존 UI 를 그대로 쓴다.
            yield return NewspaperRoutine();

            // B7 — 페이드 후 타이틀
            yield return FadeOverlay(1f, finalFadeSeconds);
            LeaveToTitle();
        }

        /// <summary>B3~B5. 무대를 세우고 발사 → 달 컷 → 달 항행을 돌린 뒤 무대를 걷는다.</summary>
        private IEnumerator StageRoutine()
        {
            BuildStage();
            if (stageCamera == null)
            {
                // 무대를 못 세웠으면 3D 구간만 버리고 신문으로 넘어간다. 검은 화면에 갇히지 않는 쪽이 먼저다.
                Log.W("[HappyEnding] 무대를 세우지 못해 3D 구간을 건너뛴다.", this);
                yield break;
            }

            float rocketHeight = Mathf.Max(1f, VisualBounds(rocket).size.y);
            float viewDistance = Mathf.Max(18f, rocketHeight * 2.4f);

            // B3 — 야간 발사
            Vector3 padTop = pad.position + Vector3.up * 0.5f;
            SeatOnPad(padTop);
            stageCamera.transform.SetPositionAndRotation(
                padTop + new Vector3(viewDistance * 0.55f, rocketHeight * 0.6f, -viewDistance),
                Quaternion.identity);
            stageCamera.transform.LookAt(padTop + Vector3.up * rocketHeight * 0.5f);

            yield return FadeOverlay(0f, revealSeconds);
            yield return WaitOrSkip(padHoldSeconds);

            PlaySfx(launchSfxId);
            PlayParticles();

            // 정지 상태에서 가속하는 등가속 상승. 6초면 화면을 벗어날 만큼 올라간다.
            float acceleration = rocketHeight * 4f;
            for (float elapsed = 0f; elapsed < launchSeconds && !skipRequested; elapsed += Time.unscaledDeltaTime)
            {
                float rise = 0.5f * acceleration * elapsed * elapsed;
                rocket.transform.position = padTop + Vector3.up * (rise + rocketHeight * 0.5f);
                stageCamera.transform.LookAt(rocket.transform.position);
                yield return null;
            }

            if (skipRequested) yield break;

            // B4 — 달 컷. 카메라를 옮겨 붙이는 컷 전환이라 블렌드하지 않는다.
            pad.gameObject.SetActive(false);
            Vector3 spaceOrigin = StageOrigin + new Vector3(0f, 0f, 4000f);
            stageCamera.transform.SetPositionAndRotation(spaceOrigin, Quaternion.identity);
            moon.gameObject.SetActive(true);
            moon.position = spaceOrigin + new Vector3(-60f, 30f, 900f);

            Vector3 toMoon = (moon.position - spaceOrigin).normalized;
            rocket.transform.rotation = Quaternion.FromToRotation(Vector3.up, toMoon);

            // 시작은 우하단 프레임 밖, 끝은 화면 가운데 위쪽 — 로켓이 달 쪽으로 멀어지며 뒷모습이 드러난다.
            Vector3 near = spaceOrigin
                + stageCamera.transform.forward * (rocketHeight * 1.6f)
                + stageCamera.transform.right * (rocketHeight * 0.9f)
                + stageCamera.transform.up * (-rocketHeight * 1.1f);
            Vector3 far = spaceOrigin
                + stageCamera.transform.forward * (rocketHeight * 9f)
                + stageCamera.transform.right * (rocketHeight * 0.2f)
                + stageCamera.transform.up * (-rocketHeight * 0.15f);

            rocket.transform.position = near;
            yield return WaitOrSkip(moonHoldSeconds);

            // B5 — 달 항행
            for (float elapsed = 0f; elapsed < transitSeconds && !skipRequested; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitSeconds);
                rocket.transform.position = Vector3.Lerp(near, far, t);
                yield return null;
            }

            yield return FadeOverlay(1f, revealSeconds);
        }

        private IEnumerator NewspaperRoutine()
        {
            yield return FadeOverlay(1f, skipRequested ? 0.35f : 0f);
            TearDownStage();

            if (research == null)
            {
                // 신문을 띄울 곳이 없다. 연출만 버리고 타이틀로 나간다.
                Log.W("[HappyEnding] 연구 화면이 없어 결과 신문을 건너뛴다.", this);
                yield break;
            }

            if (!ResearchFlowSession.GetOrCreate().HasUnacknowledgedLaunchResult)
            {
                // 확인 대기 중인 결과가 없으면 띄울 기사도 없다. 디버그 재생이 이 길로 온다.
                Log.W("[HappyEnding] 확인 대기 중인 발사 결과가 없어 신문 비트를 건너뛴다.", this);
                yield break;
            }

            newspaperDismissed = false;
            // 신문을 닫는 경로가 여럿이라(OnEnable 의 Refresh, 보고서 콜백, 설계 화면 복귀) 그것들이
            // 모두 모이는 ShowEndingScreen 한 곳을 가로챈다. 그러지 않으면 기존 MISSION COMPLETE
            // 패널이 엔딩을 가로채고 타이틀로 나가지 못한다.
            research.SetEndingOverride(() => newspaperDismissed = true);
            if (research.gameObject.activeSelf)
            {
                research.ShowLaunchResultOverlay(result, null);
            }
            else
            {
                // OnEnable 의 Refresh 가 미확인 결과를 보고 스스로 신문을 띄운다.
                research.gameObject.SetActive(true);
            }

            yield return null;

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

            // 보존한 로켓은 타이틀로 넘어가지 않는다 — DontDestroyOnLoad 라 명시적으로 지워야 한다.
            if (rocket != null) Destroy(rocket);
            rocket = null;
            RestoreEnvironment();

            // 세션 초기화는 하지 않는다. TitleMenu.NewGame 이 PrepareNewGame 으로 이미 처리한다.
            SceneManager.LoadScene(TitleSceneName);
        }

        // ── 오버레이 ────────────────────────────────────────────────────────────

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
            backdrop.raycastTarget = true; // 알파 0 이어도 클릭을 받는다 — 스킵이 3D 구간에서도 살아 있어야 한다.
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

        private IEnumerator ShowLine(string line, float hold, string sfxId)
        {
            if (string.IsNullOrEmpty(line)) yield break;

            lineText.text = line;
            PlaySfx(sfxId);
            yield return FadeText(0f, 1f, lineFadeSeconds);
            yield return WaitOrSkip(hold);
            yield return FadeText(1f, 0f, lineFadeSeconds);
        }

        private IEnumerator FadeText(float from, float to, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds && !skipRequested; elapsed += Time.unscaledDeltaTime)
            {
                lineText.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }

            lineText.alpha = skipRequested ? 0f : to;
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

        /// <summary>대기 중에도 스킵이 먹어야 하므로 <see cref="WaitForSecondsRealtime"/> 대신 직접 센다.</summary>
        private IEnumerator WaitOrSkip(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds && !skipRequested; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        // ── 무대 ────────────────────────────────────────────────────────────────

        private void BuildStage()
        {
            stage = new GameObject("Happy Ending Stage");
            stage.transform.position = StageOrigin;

            StoreEnvironment();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.03f, 0.06f);
            RenderSettings.fog = false;

            stageCamera = new GameObject("Happy Ending Camera", typeof(Camera)).GetComponent<Camera>();
            stageCamera.transform.SetParent(stage.transform, false);
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            stageCamera.depth = 100f; // 연구 화면 카메라 위에 그린다
            stageCamera.farClipPlane = 20000f;

            // 01_Main 의 카메라는 잠시 꺼 둔다. 신문을 띄울 때 되돌린다 — 결과 UI 캔버스가
            // ScreenSpaceCamera 로 걸려 있으면 카메라 없이는 아무것도 그려지지 않는다.
            silencedCameras.Clear();
            foreach (Camera other in Camera.allCameras)
            {
                if (other == stageCamera) continue;
                other.enabled = false;
                silencedCameras.Add(other);
            }

            pad = BuildPad();
            moon = BuildMoon();
            moon.gameObject.SetActive(false);

            if (rocket == null) rocket = BuildFallbackRocket();
            rocket.SetActive(true);
            rocket.transform.SetParent(stage.transform, true);
            rocket.transform.rotation = Quaternion.identity;
        }

        private Transform BuildPad()
        {
            var root = new GameObject("Pad").transform;
            root.SetParent(stage.transform, false);
            root.position = StageOrigin;

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deck.name = "Deck";
            deck.transform.SetParent(root, false);
            deck.transform.localScale = new Vector3(24f, 0.5f, 24f);
            Paint(deck, new Color(0.10f, 0.11f, 0.13f));
            Destroy(deck.GetComponent<Collider>());

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(root, false);
            ground.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            Paint(ground, new Color(0.03f, 0.035f, 0.045f));
            Destroy(ground.GetComponent<Collider>());

            // 달빛 한 장과 발사대 조명 둘. 밤이라는 것은 조명 세기로만 말한다.
            var moonlight = new GameObject("Moonlight", typeof(Light)).GetComponent<Light>();
            moonlight.transform.SetParent(root, false);
            moonlight.type = LightType.Directional;
            moonlight.color = new Color(0.55f, 0.65f, 0.95f);
            moonlight.intensity = 0.35f;
            moonlight.transform.rotation = Quaternion.Euler(35f, 200f, 0f);

            AddFloodlight(root, new Vector3(14f, 6f, -12f));
            AddFloodlight(root, new Vector3(-13f, 6f, -10f));

            return root;
        }

        private static void AddFloodlight(Transform parent, Vector3 localPosition)
        {
            var light = new GameObject("Floodlight", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = localPosition;
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.93f, 0.80f);
            light.intensity = 40f;
            light.range = 90f;
            light.spotAngle = 55f;
            light.transform.LookAt(parent.position + Vector3.up * 4f);
        }

        private Transform BuildMoon()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Moon";
            sphere.transform.SetParent(stage.transform, false);
            sphere.transform.localScale = Vector3.one * 320f;
            Destroy(sphere.GetComponent<Collider>());
            Paint(sphere, new Color(0.86f, 0.86f, 0.82f), unlit: true);
            return sphere.transform;
        }

        private GameObject BuildFallbackRocket()
        {
            // 보존에 실패했을 때만 쓰는 대역. 연출을 멈추는 것보다 낫다.
            Log.W("[HappyEnding] 보존된 로켓이 없어 대역 형상으로 재생한다.", this);
            var root = new GameObject("Fallback Rocket");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(1.4f, 4f, 1.4f);
            Destroy(body.GetComponent<Collider>());
            Paint(body, new Color(0.82f, 0.83f, 0.85f));
            return root;
        }

        private static void Paint(GameObject target, Color color, bool unlit = false)
        {
            if (!target.TryGetComponent(out Renderer renderer)) return;

            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) return;

            var material = new Material(shader) { color = color };
            if (!unlit)
            {
                material.SetFloat("_Smoothness", 0.15f);
            }

            renderer.sharedMaterial = material;
        }

        /// <summary>로켓 밑면을 발사대 위에 맞춘다. 로켓마다 크기가 달라서 바운즈로 계산한다.</summary>
        private void SeatOnPad(Vector3 padTop)
        {
            rocket.transform.position = padTop;
            Bounds bounds = VisualBounds(rocket);
            float sink = bounds.min.y - padTop.y;
            rocket.transform.position = padTop - Vector3.up * sink;
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

        private void PlayParticles()
        {
            if (rocket == null) return;
            foreach (ParticleSystem particles in rocket.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Play(true);
            }
        }

        private void TearDownStage()
        {
            if (rocket != null) rocket.transform.SetParent(null, true);
            if (stage != null) Destroy(stage);
            stage = null;
            stageCamera = null;
            pad = null;
            moon = null;

            foreach (Camera camera in silencedCameras)
            {
                if (camera != null) camera.enabled = true;
            }
            silencedCameras.Clear();

            RestoreEnvironment();
        }

        private void StoreEnvironment()
        {
            if (ambientStored) return;
            ambientModeBackup = RenderSettings.ambientMode;
            ambientBackup = RenderSettings.ambientLight;
            fogBackup = RenderSettings.fog;
            ambientStored = true;
        }

        private void RestoreEnvironment()
        {
            if (!ambientStored) return;
            RenderSettings.ambientMode = ambientModeBackup;
            RenderSettings.ambientLight = ambientBackup;
            RenderSettings.fog = fogBackup;
            ambientStored = false;
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
            if (rocket != null) Destroy(rocket);
            rocket = null;
        }
    }
}
