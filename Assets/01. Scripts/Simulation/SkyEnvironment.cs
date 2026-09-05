using Border.Core;
using UnityEngine;
using UnityEngine.Rendering;

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
        private static readonly int GroundColorId = Shader.PropertyToID("_GroundColor");

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

        [Header("수면 곡률")]
        // 지평선까지의 수평 거리. 이 거리에서 수면이 카메라 높이만큼 내려가도록 반지름을 역산한다.
        // 상한은 far clip(1000)에서 후퇴 뷰의 최대 이격(500)을 뺀 값이다 — 구의 중심축은 발사대에
        // 고정이고 카메라는 그만큼 뒤에 서므로, 먼 쪽 지평선까지 거리가 450 + 500 이 된다.
        // 더 키우면 지평선이 far plane 뒤로 밀려 다시 직선으로 잘린다.
        [SerializeField] private float horizonDistance = 450f;
        // 수면 격자 한 변의 정점 수. 굽힌 지평선 실루엣이 각져 보이면 올린다.
        [SerializeField] private int oceanResolution = 96;

        private Material _sky;
        private Material _skyBefore;
        private bool _fogBefore;
        private FogMode _fogModeBefore;
        private ParticleSystemRenderer _starRenderer;
        private ParticleSystemRenderer _cloudRenderer;
        private MaterialPropertyBlock _starBlock;
        private float _zeroY;
        private bool _bound;
        private MeshFilter _groundFilter;
        private Mesh _groundMeshBefore;
        private Mesh _groundMesh;
        private Vector3[] _flatVerts;
        private Vector3[] _curvedVerts;
        private Vector3 _groundScale;
        private float _curvedRadius = float.NaN;

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

            BindGroundMesh();
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

            if (_groundMesh != null)
            {
                if (_groundFilter != null) _groundFilter.sharedMesh = _groundMeshBefore;
                if (Application.isPlaying) Destroy(_groundMesh);
                else DestroyImmediate(_groundMesh);
                _groundMesh = null;
                _curvedRadius = float.NaN;
            }

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

            Color haze = fogColor.Evaluate(t);
            RenderSettings.fogColor = haze;
            RenderSettings.fogDensity = fogDensity.Evaluate(t);
            // 수면이 굽어 내려가면 그 아래로 스카이박스의 지면 반구가 드러난다. 기본 회색을 그대로 두면
            // 바다 밑에 회색 판이 깔린 것처럼 보인다 — 안개색을 따라가게 해서 먼 바다·우주로 읽히게 한다.
            if (_sky != null) _sky.SetColor(GroundColorId, haze);

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
            if (!inSpace) CurveGround();
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

        /// <summary>
        /// 수면을 코드로 만든 격자 메시로 갈아 끼운다. 원본 <c>WaterBlock_50m.mesh</c> 는
        /// <c>isReadable = false</c> 라 정점을 쓸 수 없다 — 복제해도 마찬가지다. 어차피 평평한 격자
        /// 하나라 직접 만드는 편이 임포트 설정에 매이지 않고, 밀도도 우리가 정한다.
        /// 로컬 크기는 원본 bounds 그대로라 트랜스폼 스케일 42 와 머티리얼 노멀 타일링이 안 틀어진다.
        /// </summary>
        private void BindGroundMesh()
        {
            if (groundRenderer == null) return;

            _groundFilter = groundRenderer.GetComponent<MeshFilter>();
            if (_groundFilter == null || _groundFilter.sharedMesh == null)
            {
                _groundFilter = null; // 곡률만 빠지고 하늘·안개·별은 그대로 돈다
                return;
            }

            _groundMeshBefore = _groundFilter.sharedMesh;
            _groundScale = _groundFilter.transform.lossyScale;
            if (Mathf.Approximately(_groundScale.y, 0f)) _groundScale.y = 1f;

            Bounds local = _groundMeshBefore.bounds;
            _groundMesh = BuildOceanGrid(local.center, local.size.x, local.size.z, oceanResolution);
            _groundFilter.sharedMesh = _groundMesh;

            _flatVerts = _groundMesh.vertices; // 런타임 생성 메시라 읽을 수 있다
            _curvedVerts = new Vector3[_flatVerts.Length];
        }

        /// <summary>XZ 평면 격자 하나. UV 는 원본과 같은 0..1 이라 물 머티리얼 타일링이 그대로 맞는다.</summary>
        private static Mesh BuildOceanGrid(Vector3 center, float width, float depth, int resolution)
        {
            int n = Mathf.Clamp(resolution, 2, 512);
            Vector3[] verts = new Vector3[n * n];
            Vector2[] uvs = new Vector2[n * n];
            Vector3[] normals = new Vector3[n * n];
            int[] tris = new int[(n - 1) * (n - 1) * 6];

            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)(n - 1);
                    float v = z / (float)(n - 1);
                    int i = z * n + x;
                    verts[i] = new Vector3(center.x + (u - 0.5f) * width, center.y,
                        center.z + (v - 0.5f) * depth);
                    uvs[i] = new Vector2(u, v);
                    normals[i] = Vector3.up;
                }
            }

            int t = 0;
            for (int z = 0; z < n - 1; z++)
            {
                for (int x = 0; x < n - 1; x++)
                {
                    int i = z * n + x;
                    tris[t++] = i;
                    tris[t++] = i + n;
                    tris[t++] = i + n + 1;
                    tris[t++] = i;
                    tris[t++] = i + n + 1;
                    tris[t++] = i + 1;
                }
            }

            Mesh mesh = new() { name = "Ocean Grid", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateTangents(); // 물 셰이더의 노멀맵이 탄젠트를 쓴다
            return mesh;
        }

        /// <summary>
        /// 평평한 수면을 발사대 축을 꼭대기로 하는 구면 캡으로 굽힌다. 카메라가 올라갈수록 반지름이
        /// 줄어 바다가 지평선 아래로 말려 내려간다 — far clip 에 잘린 직선 끝이 화면에 들어오기 전에
        /// 지평선이 먼저 닫힌다.
        /// ponytail: 노멀은 다시 계산하지 않는다. 물결은 노멀맵이 만들고 곡률은 정점 간격에 걸쳐
        /// 완만해 라이팅 차이가 안 보인다. 반사가 틀어져 보이면 그때 RecalculateNormals 를 켠다.
        /// ponytail: 물리는 평면 그대로다(<see cref="Rocket"/> 의 waterLevel). 발사대 근처는 낙차가
        /// 0 이라 같고, 옆으로 수백 유닛 밀려 착수할 때만 화면과 어긋난다.
        /// </summary>
        private void CurveGround()
        {
            if (_groundMesh == null) return;

            float height = cam.transform.position.y - _groundFilter.transform.position.y;
            float radius = CurvatureRadius(horizonDistance, height);
            // 카메라가 서 있는 동안(발사 전 조립 화면)은 정점을 다시 쓸 이유가 없다.
            if (radius.Equals(_curvedRadius)) return;
            _curvedRadius = radius;

            for (int i = 0; i < _flatVerts.Length; i++)
            {
                Vector3 v = _flatVerts[i];
                // 메시는 로컬 좌표, 곡률은 월드 거리 기준이다. 스케일 42 를 곱해 재고 되돌린다.
                float x = v.x * _groundScale.x;
                float z = v.z * _groundScale.z;
                v.y -= SphereDrop(Mathf.Sqrt(x * x + z * z), radius) / _groundScale.y;
                _curvedVerts[i] = v;
            }

            _groundMesh.SetVertices(_curvedVerts);
            // 정점이 내려간 만큼 bounds 가 커진다. 갱신하지 않으면 옛 bounds 로 컬링돼 통째로 사라진다.
            _groundMesh.RecalculateBounds();
        }

        /// <summary>
        /// 수평거리 <paramref name="horizonDistance"/> 에서 수면이 카메라 높이만큼 내려가는 구 반지름.
        /// 그 지점이 곧 눈에 보이는 지평선이다. 반지름을 고정값으로 두면 발사대에서
        /// 바다가 웅덩이가 되거나 고고도에서 곡률이 안 보인다 — 거리를 고정하고 반지름을 역산하면
        /// 값 하나로 전 고도가 덮인다. 높이가 0 이하면 무한대, 즉 평면이다.
        /// </summary>
        public static float CurvatureRadius(float horizonDistance, float cameraHeight)
        {
            if (cameraHeight <= 0f) return float.PositiveInfinity;
            return (horizonDistance * horizonDistance + cameraHeight * cameraHeight)
                   / (2f * cameraHeight);
        }

        /// <summary>
        /// 구 꼭대기에서 수평으로 <paramref name="horizontalDistance"/> 떨어진 점이 내려가는 양.
        /// 포물선 근사 <c>d²/2R</c> 를 쓰지 않는다 — 수면 반폭 1050 이 고고도에서 반지름을 넘어서고
        /// 그 구간에서 근사는 발산한다. 넘어간 점은 적도(R)에서 멈추며, 어차피 지평선 아래라 안 보인다.
        /// </summary>
        public static float SphereDrop(float horizontalDistance, float radius)
        {
            if (radius <= 0f || float.IsInfinity(radius)) return 0f;

            float d = Mathf.Min(Mathf.Abs(horizontalDistance), radius);
            return radius - Mathf.Sqrt(radius * radius - d * d);
        }
    }
}
