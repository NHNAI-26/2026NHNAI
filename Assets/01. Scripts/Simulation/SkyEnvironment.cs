using Border.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Simulation
{
    /// <summary>
    /// 로켓 고도에 따라 하늘·안개·태양·구름·수면을 바꾼다. 씬에는 프리팹 인스턴스 하나로만 들어간다.
    /// Volume 없이 내장 <see cref="RenderSettings"/> 와 구름 파티클, 그리고 대기 그라디언트·우주 큐브맵·
    /// 절차 별밭을 한 패스에서 섞는 스카이박스 셰이더 하나(<c>Sky/AtmosphereNebulaBlend</c>)만 쓴다.
    /// 별은 셰이더가 그린다 — 여기서 구동하는 프로퍼티는 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkyEnvironment : MonoBehaviour
    {
        /// <summary>
        /// 먼지 전용 레이어. 먼지는 늘 <see cref="cam"/>(큰 화면) 을 감싸므로 488 유닛 밖의 PiP 에서는
        /// 로켓에 붙은 얼룩으로만 보인다 — <see cref="RocketBuilder"/> 가 PiP 를 만들 때 이 비트를 끈다.
        /// 직렬화 필드가 아니라 상수인 이유는 두 컴포넌트가 같은 값을 알아야 하기 때문이다.
        /// </summary>
        public const int DustLayer = 9; // SpaceDust

        private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        private static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int HorizonColorId = Shader.PropertyToID("_HorizonColor");
        private static readonly int SpaceBlendId = Shader.PropertyToID("_SpaceBlend");

        [Header("씬 참조")]
        [SerializeField] private Transform target;
        [SerializeField] private Camera cam;
        [SerializeField] private Light sun;
        [SerializeField] private Renderer groundRenderer;

        [Header("구성 요소")]
        [SerializeField] private Material skyboxSource;
        [SerializeField] private ParticleSystem clouds;
        // 비면 Unity 기본 파티클 머티리얼로 그린다. 기본은 Sky/Star.mat — 궤적 점과 같은 것을 쓴다.
        [SerializeField] private Material dustMaterial;

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
        // 대기 그라디언트와 우주 큐브맵을 스카이박스 안에서 섞는 비율. 스카이박스는 far depth 에 그려져
        // 바다를 가리지 않으므로 구간과 상한을 자유롭게 그으면 된다.
        [SerializeField] private AnimationCurve spaceBlend = AnimationCurve.EaseInOut(0.4f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.6f);

        [Header("수면 곡률")]
        // 지평선까지의 수평 거리. 이 거리에서 수면이 카메라 높이만큼 내려가도록 반지름을 역산한다.
        // 상한은 far clip(1000)에서 후퇴 뷰의 최대 이격(500)을 뺀 값이다 — 구의 중심축은 발사대에
        // 고정이고 카메라는 그만큼 뒤에 서므로, 먼 쪽 지평선까지 거리가 450 + 500 이 된다.
        // 더 키우면 지평선이 far plane 뒤로 밀려 다시 직선으로 잘린다.
        [SerializeField] private float horizonDistance = 450f;
        // 수면 격자 한 변의 정점 수. 굽힌 지평선 실루엣이 각져 보이면 올린다.
        [SerializeField] private int oceanResolution = 96;

        [Header("우주 먼지")]
        // 우주에서는 수면도 구름도 꺼지고 별은 스카이박스라 무한원이다 — 카메라 대비 움직이는 것이
        // 하나도 없다. 가까이 뿌린 먼지만이 시차를 만든다. 속도를 주입하지 않는다: 먼지는 월드에
        // 서 있고 카메라가 뚫고 지나간다. rateOverDistance 라 정점에서 카메라가 서면 방출도 멎는다.
        [SerializeField] private float dustRadius = 60f;
        // 0.5 = 안쪽 30 유닛을 비운다. 더 가까우면 각속도가 커져 프레임당 60 px 씩 튀고 스트로빙한다.
        [SerializeField, Range(0f, 1f)] private float dustHollow = 0.5f;
        [SerializeField] private float dustPerUnit = 0.9f; // 카메라가 1 유닛 움직일 때마다 방출할 수
        [SerializeField] private float dustLifetime = 2.5f;
        [SerializeField] private Vector2 dustSize = new(0.10f, 0.25f);
        [SerializeField, Range(0f, 1f)] private float dustAlpha = 0.25f;
        // 카메라 속도 → 스트릭 길이. 모션 블러가 없으므로 스트릭이 프레임당 이동 픽셀보다 길어야
        // 끊긴 점이 아니라 흐르는 선으로 읽힌다. 55 유닛/s 에서 1.65 유닛 = 30 유닛 거리에서 51 px,
        // 같은 거리의 프레임 점프가 31 px 다.
        [SerializeField] private float dustStretch = 0.03f;

        private Material _sky;
        private Material _skyBefore;
        private bool _fogBefore;
        private FogMode _fogModeBefore;
        private ParticleSystemRenderer _cloudRenderer;
        private ParticleSystem.Particle[] _cloudParticles;
        private Vector3 _cloudBox; // 랩 주기. ShapeModule 박스가 단일 출처다
        private Vector3 _cloudCenter = new(float.NaN, float.NaN, float.NaN);
        private ParticleSystem _dust;
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
                // 이미터 박스가 곧 랩 주기다 — 최초 버스트가 채우는 넓이와 되돌리는 간격이 같아야
                // 이음매에서 밀도가 튀지 않는다. 넓이를 바꾸려면 프리팹의 Shape 박스 하나만 만진다.
                _cloudBox = clouds.shape.scale;
                _cloudParticles = new ParticleSystem.Particle[clouds.main.maxParticles];
            }

            BuildDust();
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

            if (_dust != null)
            {
                if (Application.isPlaying) Destroy(_dust.gameObject);
                else DestroyImmediate(_dust.gameObject);
                _dust = null;
            }

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
                // Gradient 는 인스펙터에서 sRGB 로 authoring 하지만 Material.SetColor 는 값을 변환 없이
                // 셰이더로 넘긴다. 프로젝트가 Linear 색공간이라 .linear 를 거쳐야 스와치와 화면이 맞는다.
                _sky.SetColor(SkyTintId, skyTint.Evaluate(t).linear);
                _sky.SetFloat(AtmosphereThicknessId, atmosphereThickness.Evaluate(t));
                _sky.SetFloat(ExposureId, skyExposure.Evaluate(t));
                _sky.SetFloat(SpaceBlendId, spaceBlend.Evaluate(t));
            }

            Color haze = fogColor.Evaluate(t);
            RenderSettings.fogColor = haze;
            RenderSettings.fogDensity = fogDensity.Evaluate(t);
            // 지평선색은 안개색 그대로다. 스카이박스가 지평선 아래도 이 색으로 채우므로, 수면이 굽어
            // 내려가 그 아래가 드러나도 먼 바다의 헤이즈로 읽힌다.
            if (_sky != null) _sky.SetColor(HorizonColorId, haze.linear);

            // 앰비언트는 갱신하지 않는다 — Skybox 앰비언트를 따라가게 하려면 매 프레임 DynamicGI.UpdateEnvironment()
            // 가 필요하고 그 비용이 이 연출값보다 크다. 하늘색 반사가 필요해지면 그때 단계적으로 부른다.
            if (sun != null) sun.intensity = sunIntensity.Evaluate(t);

            bool inSpace = AltitudeKm >= spaceKm;
            if (groundRenderer != null) groundRenderer.enabled = !inSpace;
            if (!inSpace) CurveGround();
            // 우주에서 내려다보면 구름 판이 허공에 떠 있는 덩어리로 보인다. 렌더러만 끈다 —
            // 오브젝트를 껐다 켜면 한 번뿐인 버스트가 다시 터진다.
            if (_cloudRenderer != null) _cloudRenderer.enabled = !inSpace;
            if (!inSpace) WrapClouds();

            // 대기권에서는 구름과 수면이 이미 속도를 말해 준다. 먼지는 그 둘이 꺼지는 우주부터다.
            // 모듈은 구조체 사본이라 바뀔 때만 쓴다.
            if (_dust != null && _dust.emission.enabled != inSpace)
            {
                ParticleSystem.EmissionModule emission = _dust.emission;
                emission.enabled = inSpace;
            }
        }

        /// <summary>
        /// 뒤로 흘려보낸 구름을 반대편 끝으로 되돌려, 고정 개수로 끝없는 구름 바다를 만든다.
        /// 버스트는 한 번뿐이고 파티클은 World 공간이라 이미터를 옮겨도 따라오지 않는다 —
        /// 살아 있는 입자를 직접 손댄다(<see cref="RocketBuilder.UpdateTrailDotSize"/> 와 같은 패턴).
        /// 접는 기준은 로켓이 아니라 카메라다: 보이는 범위를 정하는 것이 카메라이고, 후퇴 뷰와
        /// PiP 의 최대 이격 500 은 타일 반폭(1000)보다 작아 어느 뷰에서도 가장자리가 안 나온다.
        /// 되돌아간 구름은 카메라에서 반폭만큼 떨어진 곳에 나타난다 — far clip(1000) 밖이라 안 보인다.
        /// </summary>
        private void WrapClouds()
        {
            if (clouds == null || _cloudParticles == null) return;

            // 구름은 속도가 0 이라 카메라가 서 있으면 칸을 넘어갈 입자도 없다. 조립 화면에서
            // 매 프레임 파티클 배열을 통째로 복사하지 않으려고 먼저 걸러 낸다.
            Vector3 center = cam.transform.position;
            if (center == _cloudCenter) return;
            _cloudCenter = center;

            int count = clouds.GetParticles(_cloudParticles);
            bool moved = false;

            for (int i = 0; i < count; i++)
            {
                Vector3 p = _cloudParticles[i].position;
                // 고도는 건드리지 않는다 — 구름은 한 겹이고 수직으로 접으면 층이 겹쳐 보인다.
                float x = WrapAxis(p.x, center.x, _cloudBox.x);
                float z = WrapAxis(p.z, center.z, _cloudBox.z);
                if (x.Equals(p.x) && z.Equals(p.z)) continue;

                _cloudParticles[i].position = new Vector3(x, p.y, z);
                moved = true;
            }

            if (moved) clouds.SetParticles(_cloudParticles, count);
        }

        /// <summary>
        /// <paramref name="center"/> 를 한가운데로 하는 폭 <paramref name="span"/> 의 칸 안으로 접는다.
        /// <c>Round</c> 라 한 프레임에 몇 칸을 건너뛰어도 한 번에 맞는다 — 반복문이 필요 없다.
        /// 폭이 0 이면 그대로 둔다. 접었다가는 구름 전체가 카메라 위 한 점으로 뭉친다.
        /// </summary>
        public static float WrapAxis(float value, float center, float span) =>
            span <= 0f ? value : value - span * Mathf.Round((value - center) / span);

        /// <summary>
        /// 카메라 둘레의 먼지. 프리팹이 아니라 코드로 조립한다 —
        /// <see cref="RocketBuilder.EnsureTrajectoryTrail"/> 이 같은 선례이고, 이 값들은 화면에서
        /// 튜닝할 것이 아니라 계산으로 정해진 것이라 인스펙터에 흩어 둘 이유가 없다.
        /// </summary>
        private void BuildDust()
        {
            var host = new GameObject("SpaceDust") { layer = DustLayer };
            // 카메라의 자식이라 매 프레임 위치를 쓰는 코드가 없다. 직접 대입하면 Cinemachine 이
            // LateUpdate 에서 카메라를 옮긴 뒤라 한 프레임 밀린다.
            host.transform.SetParent(cam.transform, false);

            _dust = host.AddComponent<ParticleSystem>();
            _dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _dust.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // 뿌린 먼지는 그 자리에 남는다
            main.startLifetime = dustLifetime;
            main.startSpeed = 0f; // 움직이는 것은 카메라뿐이다
            main.startSize = new ParticleSystem.MinMaxCurve(dustSize.x, dustSize.y);
            main.startColor = new Color(1f, 1f, 1f, dustAlpha);
            main.gravityModifier = 0f;
            main.playOnAwake = false;
            main.maxParticles = 400;

            ParticleSystem.EmissionModule emission = _dust.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = dustPerUnit;
            emission.enabled = false; // 우주에서만 켠다

            ParticleSystem.ShapeModule shape = _dust.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = dustRadius;
            shape.radiusThickness = dustHollow;

            // 껍질 안에서 태어나므로 시야 한가운데에 톡 나타난다. 수명 양끝을 알파로 눌러 가린다.
            ParticleSystem.ColorOverLifetimeModule fade = _dust.colorOverLifetime;
            fade.enabled = true;
            Gradient ramp = new();
            ramp.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f)
                });
            fade.color = new ParticleSystem.MinMaxGradient(ramp);

            var dustRenderer = host.GetComponent<ParticleSystemRenderer>();
            // 입자 속도는 0 이라 늘어남은 전적으로 카메라 속도에서 온다. lengthScale 2 가 기본 길이라
            // 카메라가 멈춘 정점에서도 사각형이 찌그러지지 않는다.
            dustRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            dustRenderer.velocityScale = 0f;
            dustRenderer.cameraVelocityScale = dustStretch;
            dustRenderer.lengthScale = 2f;
            // ponytail: 비면 Unity 기본 파티클 머티리얼로 그린다. 궤적 점처럼 Shader.Find 로 새 Material 을
            // 만들지 않는 이유는 EditMode 테스트에서 그 인스턴스가 그대로 새기 때문이다.
            if (dustMaterial != null) dustRenderer.sharedMaterial = dustMaterial;
            dustRenderer.shadowCastingMode = ShadowCastingMode.Off;
            dustRenderer.receiveShadows = false;

            _dust.Play();
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
