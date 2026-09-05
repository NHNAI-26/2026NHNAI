using Border.Core;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 로켓 고도에 따라 하늘·안개·태양·별·지구를 바꾼다. 씬에는 프리팹 인스턴스 하나로만 들어간다.
    /// 커스텀 셰이더나 Volume 없이 내장 <see cref="RenderSettings"/> 와 파티클만 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkyEnvironment : MonoBehaviour
    {
        private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        private static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("씬 참조")]
        [SerializeField] private Transform target;
        [SerializeField] private Camera cam;
        [SerializeField] private Light sun;
        [SerializeField] private Renderer groundRenderer;

        [Header("구성 요소")]
        [SerializeField] private Material skyboxSource;
        [SerializeField] private ParticleSystem clouds;
        [SerializeField] private ParticleSystem stars;
        [SerializeField] private Transform earth;

        [Header("고도 스케일")]
        // 1 유닛이 실제 몇 m 인가. 프로토타입 비행은 정점이 434 유닛뿐이라 250 으로 부풀려 108 km 로 읽는다.
        // 실제 스케일 비행이 붙으면 이 값만 1 로 바꾸면 된다.
        [SerializeField] private float worldMetersPerUnit = 250f;
        [SerializeField] private float maxKm = 120f;
        [SerializeField] private float cloudKm = 10f;
        [SerializeField] private float spaceKm = 70f;

        [Header("고도별 룩 (x = 고도 / maxKm)")]
        [SerializeField] private Gradient skyTint = new();
        [SerializeField] private Gradient fogColor = new();
        [SerializeField] private AnimationCurve fogDensity = AnimationCurve.EaseInOut(0f, 0.004f, 1f, 0f);
        [SerializeField] private AnimationCurve atmosphereThickness = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private AnimationCurve skyExposure = AnimationCurve.EaseInOut(0f, 1.3f, 1f, 0f);
        [SerializeField] private AnimationCurve starAlpha = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.6f);
        [SerializeField] private AnimationCurve earthDistance = AnimationCurve.Linear(0f, 800f, 1f, 850f);
        // 카메라에서 earthDistance 만큼 떨어진 구의 지름. 지름 < 거리라야 '공'으로 보인다 — 같으면 눈높이가
        // 표면에 붙어 지평선만 남는다.
        [SerializeField] private AnimationCurve earthScale = AnimationCurve.Linear(0f, 1200f, 1f, 600f);

        private Material _sky;
        private Material _skyBefore;
        private bool _fogBefore;
        private FogMode _fogModeBefore;
        private ParticleSystemRenderer _starRenderer;
        private ParticleSystemRenderer _cloudRenderer;
        private MaterialPropertyBlock _starBlock;
        private float _zeroY;
        private bool _bound;

        /// <summary>발사대를 0 으로 잡은 가상 고도. 텔레메트리 HUD 가 붙으면 여기서 읽으면 된다.</summary>
        public float AltitudeKm =>
            target == null ? 0f : (target.position.y - _zeroY) * worldMetersPerUnit * 0.001f;

        private void Awake() => Bind();

        /// <summary>발사대 높이를 고도 0 으로 잡고 하늘을 넘겨받는다. EditMode 테스트에서는 Awake 가 돌지 않아 직접 부른다.</summary>
        public void Bind()
        {
            if (_bound) return;
            if (target == null || cam == null)
            {
                Log.W("SkyEnvironment: target/cam 이 비어 있어 비활성화한다.", this);
                enabled = false;
                return;
            }

            _bound = true;
            _zeroY = target.position.y;

            _skyBefore = RenderSettings.skybox;
            if (skyboxSource != null)
            {
                // 공유 머티리얼을 런타임에 쓰면 에디터에서 .mat 에셋이 그대로 더러워진다. 복사본을 넘긴다.
                _sky = new Material(skyboxSource);
                RenderSettings.skybox = _sky;
            }

            _fogBefore = RenderSettings.fog;
            _fogModeBefore = RenderSettings.fogMode;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            if (clouds != null)
            {
                clouds.transform.position = new Vector3(0f, _zeroY + cloudKm * 1000f / worldMetersPerUnit, 0f);
                _cloudRenderer = clouds.GetComponent<ParticleSystemRenderer>();
            }

            if (stars != null) _starRenderer = stars.GetComponent<ParticleSystemRenderer>();

            if (earth != null) earth.gameObject.SetActive(false);
        }

        private void OnDestroy() => Unbind();

        /// <summary>Bind 이 손댄 전역 렌더 설정을 되돌린다. EditMode 에서는 OnDestroy 가 돌지 않아 직접 부른다.</summary>
        public void Unbind()
        {
            if (!_bound) return;
            _bound = false;

            RenderSettings.skybox = _skyBefore;
            RenderSettings.fog = _fogBefore;
            RenderSettings.fogMode = _fogModeBefore;

            if (_sky == null) return;
            if (Application.isPlaying) Destroy(_sky);
            else DestroyImmediate(_sky); // EditMode 테스트에서는 Destroy 가 끝내 실행되지 않아 새는 것을 막는다
        }

        private void Update()
        {
            float t = Mathf.Clamp01(AltitudeKm / Mathf.Max(maxKm, 0.001f));

            if (_sky != null)
            {
                _sky.SetColor(SkyTintId, skyTint.Evaluate(t));
                _sky.SetFloat(AtmosphereThicknessId, atmosphereThickness.Evaluate(t));
                _sky.SetFloat(ExposureId, skyExposure.Evaluate(t));
            }

            RenderSettings.fogColor = fogColor.Evaluate(t);
            RenderSettings.fogDensity = fogDensity.Evaluate(t);

            // 앰비언트는 갱신하지 않는다 — Skybox 앰비언트를 따라가게 하려면 매 프레임 DynamicGI.UpdateEnvironment()
            // 가 필요하고 그 비용이 이 연출값보다 크다. 하늘색 반사가 필요해지면 그때 단계적으로 부른다.
            if (sun != null) sun.intensity = sunIntensity.Evaluate(t);

            if (_starRenderer != null)
            {
                stars.transform.position = cam.transform.position;
                // 도메인 리로드는 _bound/_starRenderer 는 복원해도 직렬화 못 하는 이 블록은 null 로 되돌린다.
                // Bind 는 _bound 때문에 다시 돌지 않으므로 여기서 채운다.
                _starBlock ??= new MaterialPropertyBlock();
                _starRenderer.GetPropertyBlock(_starBlock);
                _starBlock.SetColor(BaseColorId, new Color(1f, 1f, 1f, starAlpha.Evaluate(t)));
                _starRenderer.SetPropertyBlock(_starBlock);
            }

            bool inSpace = AltitudeKm >= spaceKm;
            if (groundRenderer != null) groundRenderer.enabled = !inSpace;
            // 우주에서 내려다보면 구름 판이 지구 위에 떠 있는 덩어리로 보인다. 렌더러만 끈다 —
            // 오브젝트를 껐다 켜면 한 번뿐인 버스트가 다시 터진다.
            if (_cloudRenderer != null) _cloudRenderer.enabled = !inSpace;
            if (earth == null) return;

            if (earth.gameObject.activeSelf != inSpace) earth.gameObject.SetActive(inSpace);
            if (!inSpace) return;

            // ponytail: 지구는 실제 크기가 아니라 카메라 바로 아래 고정 거리에 놓인 가짜 구다. 카메라가 늘 로켓을
            // 궤도로 돌기 때문에 시차가 드러나지 않고, far clip 1000 안에 들어와 씬 카메라를 손댈 필요도 없다.
            // 실제 스케일 비행이 붙으면 진짜 반지름·위치를 가진 구로 교체할 것.
            earth.position = new Vector3(0f, cam.transform.position.y - earthDistance.Evaluate(t), 0f);
            earth.localScale = Vector3.one * earthScale.Evaluate(t);
        }
    }
}
