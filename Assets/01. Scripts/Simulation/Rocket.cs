using System.Collections.Generic;
using Border.Core;
using UnityEngine;

namespace Simulation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Rocket : MonoBehaviour
    {
        [SerializeField] private int launchSeed = 20260904;
        [SerializeField] private ParticleSystem explosionPrefab;
        private ParticleSystem activeExplosion;
        private Renderer[] explosionRenderers;
        private bool[] rendererVisibility;
        public bool Exploded { get; private set; }
        public event System.Action OverheatExplosionStarted;
        public event System.Action<bool> ExplosionPhotoRequested;
        // ponytail: 계수 하나를 모든 엔진이 공유한다. 프리셋마다 탱크 밀도를 다르게 하고 싶어지면 그때
        // EngineStatsSO 필드로 내린다 — CreateRuntimeCopy 와 리서치 브리지도 같이 넓어진다.
        [SerializeField] private float tankMassPerFuel = 0.25f; // 연료 1kg 당 탱크 무게(kg)

        [Tooltip("점화부터 최대 추력까지 걸리는 시간(초). 0 이면 발사 첫 프레임에 최대 추력이다.")]
        [SerializeField, Min(0f)] private float ignitionRampSeconds = 1.2f;

        [Header("Launch hold")]
        [Tooltip("발사 승인 후 발사대에 붙들려 있는 시간(초). 이 동안 힘도 연료도 쓰지 않는다 — 연출 구간이다.")]
        [SerializeField, Min(0f)] private float holdSeconds = 2.5f;
        [Tooltip("홀드 동안 꿀렁일 로켓 몸통 렌더러. Uber 3D Object 셰이더여야 한다. 비우면 변형하지 않는다.")]
        [SerializeField] private Renderer bounceRenderer;
        [Tooltip("몸통 아래쪽이 부푸는 폭. 그 높이의 반지름에 대한 비율이라 메시 크기와 무관하다.")]
        [SerializeField, Range(0f, 0.5f)] private float bounceAmplitude = 0.25f;

        [Header("Assisted liftoff")]
        [Tooltip("클램프 해제 위치에서 월드 위쪽으로 유도 상승할 높이. 0이면 바로 물리를 적용한다.")]
        [SerializeField, Min(0f)] private float assistedLiftHeight = 3f;
        [Tooltip("목표 높이까지 정지 상태에서 서서히 가속하는 시간.")]
        [SerializeField, Min(0f)] private float assistedLiftSeconds = 2.5f;
        [Tooltip("유도 상승 후 엔진 힘과 중력을 완전히 적용하기까지 걸리는 시간.")]
        [SerializeField, Min(0f)] private float physicsBlendSeconds = 1f;

        [Header("Splashdown")]
        [Tooltip("수면 높이(월드 y). 씬의 Ground 와 같은 값이어야 한다.")]
        [SerializeField] private float waterLevel = -6.71f;
        [SerializeField] private float waterDamping = 4f;
        [Tooltip("수면 아래 이만큼 내려가면 멈춘다.")]
        [SerializeField] private float sinkDepth = 30f;

        // 키워드는 MaterialPropertyBlock 으로 못 켠다(RocketPart 의 아웃라인과 같은 이유) —
        // 렌더러당 머티리얼 인스턴스를 하나 만들어 들고 있다가 파괴 때 반납한다.
        private static readonly int WobbleAmplitudeId = Shader.PropertyToID("_WobbleAmplitude");
        private static readonly int WobbleAxisId = Shader.PropertyToID("_WobbleAxis");
        private static readonly int WobbleHalfHeightId = Shader.PropertyToID("_WobbleHalfHeight");
        private const string WobbleKeyword = "_WOBBLE_ON";

        private readonly List<RocketPart> _engines = new();
        private readonly HashSet<(Collider Self, Collider Surface)> _groundContacts = new();
        private readonly DeterministicRng _rng = new();
        private Rigidbody _body;
        private float _bodyMass;
        private float initialLinearDamping;
        private float initialAngularDamping;
        private int _liveEngines;
        private float _sinceLaunch;
        private float _maxThrust;
        private float _holdElapsed;
        private float _ignitedFraction;
        private Material _bounceMaterial;
        private enum LiftPhase { None, Guided, Blending }
        private LiftPhase _liftPhase;
        private Vector3 _liftOrigin;
        private Vector3 _liftVelocity;
        private float _liftElapsed;
        private float _physicsBlendElapsed;

        public bool LiftAssistActive => _liftPhase != LiftPhase.None;

        public bool Launched { get; private set; }
        public System.Func<bool> AuthorizeLaunch { get; set; }
        public event System.Action LaunchStarted;
        public event System.Action LiftoffStarted;
        public bool FlightStopped { get; private set; }
        public float TotalBurnSeconds { get; private set; }

        /// <summary>
        /// 발사대에 붙들려 있는 중인지. 홀드는 <b>연출</b>이라 힘도 연료도 발열도 쓰지 않는다 —
        /// 미션 시계와 연료 밸런스는 클램프가 풀리는 순간부터 돈다.
        /// </summary>
        public bool Holding { get; private set; }

        /// <summary>클램프가 풀렸는지. 비행 판정이 읽는 게이트다.</summary>
        public bool Lifted => Launched && !Holding;

        /// <summary>홀드 진행도 0..1. 연출이 읽는 값이다.</summary>
        public float HoldProgress { get; private set; }

        /// <summary>
        /// 이번 스텝에 실제로 건 추력 ÷ 전 엔진 최대 추력. 연출이 읽는 값이다(발사 카메라 흔들림).
        /// 점화에 실패한 엔진이 있으면 1 에 닿지 않는다 — 반만 점화한 발사는 반만 흔들린다.
        /// </summary>
        public float ThrustFraction { get; private set; }

        /// <summary>과열로 발사가 끝났는지. 한 발사에 주요 사고는 하나뿐이라 이후 추력을 걸지 않는다.</summary>
        public bool Overheated { get; private set; }

        /// <summary>수면 아래로 내려갔는지. 추력은 여기서 끝난다.</summary>
        public bool Splashed { get; private set; }

        public event System.Action<Vector3> SplashdownStarted;

        /// <summary>
        /// 점화 후 <paramref name="elapsedSeconds"/> 시점의 추력 배율. 램프 시계는 로켓에 하나뿐이다 —
        /// 엔진은 전부 같은 순간에 점화하므로 부품마다 두면 시계만 엔진 수만큼 늘어난다. 그리고
        /// <see cref="RocketPart.Output"/> 은 "프리셋 최대치 × 스로틀"로 테스트가 잠가 둔 계약이라
        /// 거기에 램프를 섞을 수 없다.
        /// </summary>
        public static float RampFactor(float elapsedSeconds, float rampSeconds)
        {
            return rampSeconds <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, elapsedSeconds / rampSeconds);
        }

        public bool IsGrounded
        {
            get
            {
                _groundContacts.RemoveWhere(pair => pair.Self == null || pair.Surface == null
                    || !pair.Self.enabled || !pair.Surface.enabled
                    || !pair.Self.gameObject.activeInHierarchy || !pair.Surface.gameObject.activeInHierarchy);
                return _groundContacts.Count > 0;
            }
        }

        private void OnCollisionEnter(Collision collision) => UpdateGroundContact(collision);
        private void OnCollisionStay(Collision collision) => UpdateGroundContact(collision);

        private void OnCollisionExit(Collision collision)
        {
            _groundContacts.RemoveWhere(pair => pair.Surface == collision.collider);
        }

        private void UpdateGroundContact(Collision collision)
        {
            _groundContacts.RemoveWhere(pair => pair.Surface == collision.collider);
            if (collision.rigidbody == _body) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                // Side impacts are not ground support. Keep each collider pair separately;
                // sleeping rigidbodies stop sending Stay but remain supported until Exit.
                ContactPoint contact = collision.GetContact(i);
                if (Vector3.Dot(contact.normal, Vector3.up) < 0.5f) continue;
                _groundContacts.Add((contact.thisCollider, contact.otherCollider));
            }
        }

        private void OnDisable() => _groundContacts.Clear();

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            // 본체 무게의 원본은 씬의 Rigidbody 다 — 코드에 복제하지 않는다. 발사 때 mass 를 덮어쓰므로
            // 덮어쓰기 전 값을 여기서 잡아 둔다.
            _bodyMass = _body.mass;
            initialLinearDamping = _body.linearDamping;
            initialAngularDamping = _body.angularDamping;
            _body.isKinematic = true; // 발사 전에는 발사대에 고정
        }

        /// <summary>
        /// 로켓 표면의 worldPoint 에 부품을 붙인다. 자세는 부품이 가진 것을 그대로 둔다 —
        /// 추력이 부품의 up 을 따르므로(FixedUpdate) 눕힌 자세가 곧 힘 방향이다.
        /// </summary>
        public void Attach(RocketPart part, Vector3 worldPoint)
        {
            part.transform.SetParent(transform, true);
            part.transform.position = worldPoint;
        }

        public void Launch()
        {
            if (Launched) return;
            if (AuthorizeLaunch != null && !AuthorizeLaunch()) return;

            Launched = true;
            _groundContacts.Clear();
            Overheated = false;
            _sinceLaunch = 0f;
            _holdElapsed = 0f;
            HoldProgress = 0f;
            // ponytail: engine list frozen at launch; re-collect if parts ever detach mid-flight
            GetComponentsInChildren(_engines);

            // 같은 시드면 같은 점화 결과가 나온다.
            _rng.Reseed(launchSeed);

            _liveEngines = 0;
            _maxThrust = 0f;
            float ignitedThrust = 0f;
            float mass = _bodyMass;
            for (int i = 0; i < _engines.Count; i++)
            {
                _engines[i].Prepare(_rng);
                if (_engines[i].Ignited)
                {
                    _liveEngines++;
                    ignitedThrust += _engines[i].Output;
                }

                if (_engines[i].HasStats) mass += _engines[i].Stats.FuelCapacity * tankMassPerFuel;
                _maxThrust += _engines[i].Output; // ThrustFraction 의 분모 — 점화 실패분도 들어간다.
            }

            // 탱크가 클수록 오래 타지만 그만큼 무겁다. 점화에 실패한 엔진의 탱크도 무게는 그대로 싣고 간다.
            // 연소 중에는 줄지 않는다 — 발사 순간에 한 번 정하고 끝이다.
            _body.mass = mass;
            _ignitedFraction = _maxThrust > 0f ? ignitedThrust / _maxThrust : 0f;

            Log.D($"Launch: {_liveEngines}/{_engines.Count} engine(s) ignited, {mass:0.#} kg", this);
            LaunchStarted?.Invoke();

            Holding = holdSeconds > 0f;
            if (!Holding) BeginLiftoff();
        }

        /// <summary>
        /// 클램프 홀드 한 스텝. 배기와 흔들림만 올리고 물리는 건드리지 않는다 — 로켓은 kinematic 인 채로
        /// 발사대에 앉아 있다. 램프 시계도 여기서는 돌지 않는다: 이륙이 시작될 때 0 부터 다시 센다.
        /// </summary>
        private void TickHold()
        {
            _holdElapsed += Time.fixedDeltaTime;
            HoldProgress = Mathf.Clamp01(_holdElapsed / holdSeconds);

            foreach (RocketPart engine in _engines) engine.HoldExhaust(HoldProgress);
            SetWobble(bounceAmplitude * HoldProgress);
            // 카메라 흔들림이 읽는 값(RocketBuilder.PlaceView). 힘은 안 걸지만 화면에는 점화가 보여야
            // 하고, 반만 점화한 발사는 여기서도 반만 흔들린다.
            ThrustFraction = HoldProgress * _ignitedFraction;

            if (_holdElapsed < holdSeconds) return;

            Holding = false;
            BeginLiftoff();
        }

        /// <summary>클램프 해제. 여기서부터가 시뮬레이션이다 — 램프도 미션 시계도 이 순간이 0 이다.</summary>
        private void BeginLiftoff()
        {
            LiftoffStarted?.Invoke();
            SetWobble(0f);
            _sinceLaunch = 0f;
            _liftElapsed = _physicsBlendElapsed = 0f;
            _liftVelocity = Vector3.zero;
            _liftOrigin = _body.position;
            bool hasUpwardEngine = _engines.Exists(engine => engine.Ignited && engine.HasFuel
                && engine.Output > 0f && Vector3.Dot(engine.transform.up, Vector3.up) > 0f);
            if (assistedLiftHeight > 0f && assistedLiftSeconds > 0f && hasUpwardEngine)
            {
                _liftPhase = LiftPhase.Guided;
                _body.isKinematic = true;
                return;
            }

            _liftPhase = LiftPhase.None;
            ReleaseLift();
        }

        private void ReleaseLift()
        {
            _body.isKinematic = false;
            // 접지 속도가 90 m/s 를 넘는다. 0.02초 스텝이면 한 번에 1.8 m 이동이라
            // Discrete 판정으로는 두께 0 인 지면 평면을 그대로 통과한다.
            _body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _body.linearVelocity = _liftVelocity;
            _body.angularVelocity = Vector3.zero;
        }

        private float UpdateLiftBlend(float deltaTime)
        {
            if (_liftPhase == LiftPhase.Guided && _liftElapsed >= assistedLiftSeconds)
            {
                ReleaseLift();
                _liftPhase = physicsBlendSeconds > 0f ? LiftPhase.Blending : LiftPhase.None;
            }

            if (_liftPhase == LiftPhase.Guided) return 0f;
            if (_liftPhase != LiftPhase.Blending) return 1f;

            _physicsBlendElapsed += deltaTime;
            float blend = RampFactor(_physicsBlendElapsed, physicsBlendSeconds);
            if (_physicsBlendElapsed >= physicsBlendSeconds) _liftPhase = LiftPhase.None;
            return blend;
        }

        private void ApplyLiftAssist(float deltaTime, float physicsBlend, bool hasUpwardEngine)
        {
            if (_liftPhase == LiftPhase.None) return;
            if (!hasUpwardEngine)
            {
                if (_liftPhase == LiftPhase.Guided) ReleaseLift();
                _liftPhase = LiftPhase.None;
                return;
            }

            float acceleration = 2f * assistedLiftHeight / (assistedLiftSeconds * assistedLiftSeconds);
            if (_liftPhase == LiftPhase.Guided)
            {
                _liftElapsed = Mathf.Min(_liftElapsed + deltaTime, assistedLiftSeconds);
                float progress = _liftElapsed / assistedLiftSeconds;
                _body.MovePosition(_liftOrigin + Vector3.up * (assistedLiftHeight * progress * progress));
                _liftVelocity = Vector3.up * (acceleration * _liftElapsed);
                return;
            }

            // 엔진 힘과 중력은 같은 배율로 넘긴다. 기존 상승 속도를 유지한 채 보조 가속만 줄인다.
            Vector3 gravity = _body.useGravity ? Physics.gravity : Vector3.zero;
            _body.AddForce((Vector3.up * acceleration - gravity) * (1f - physicsBlend), ForceMode.Acceleration);
        }

        /// <summary>
        /// 몸통 아래쪽 wobble 진폭. 진동 위상은 셰이더가 <c>_Time</c> 으로 돌리므로 코드가 미는 값은
        /// 세기 하나뿐이다. 0 이면 키워드까지 꺼서 정점 변형이 아예 컴파일에서 빠진다.
        /// </summary>
        private void SetWobble(float amplitude)
        {
            if (bounceRenderer == null) return;

            if (_bounceMaterial == null)
            {
                _bounceMaterial = bounceRenderer.material; // 여기서 인스턴스가 만들어진다
                // 메시가 로컬 단위로 얼마나 큰지는 셰이더가 알 수 없다. 임포트 스케일이 메시마다
                // 다르므로(이 로켓 몸통은 반높이가 0.01 남짓이다) 축 방향 반높이를 여기서 재서 넘긴다 —
                // 그래야 진폭과 높이 비율이 메시 크기와 무관하게 0..1 로 읽힌다.
                Vector3 axis = _bounceMaterial.GetVector(WobbleAxisId);
                Vector3 extents = bounceRenderer.localBounds.extents;
                _bounceMaterial.SetFloat(WobbleHalfHeightId, Mathf.Max(
                    Mathf.Abs(Vector3.Dot(extents, axis.normalized)), 1e-4f));
            }

            _bounceMaterial.SetFloat(WobbleAmplitudeId, amplitude);
            if (amplitude > 0f) _bounceMaterial.EnableKeyword(WobbleKeyword);
            else _bounceMaterial.DisableKeyword(WobbleKeyword);
        }

        /// <summary>인스턴스는 우리가 만들었으니 우리가 지운다 — <c>RocketPart.ReleaseMaterials</c> 와 같다.</summary>
        private void OnDestroy()
        {
            if (_bounceMaterial == null) return;

            if (Application.isPlaying) Destroy(_bounceMaterial);
            else DestroyImmediate(_bounceMaterial);
            _bounceMaterial = null;
        }

        /// <summary>
        /// 수면 아래에서는 추력을 끊고 저항을 걸어 가라앉힌다. 물은 콜라이더가 없어 로켓이 그대로 통과한다.
        /// </summary>
        // ponytail: 저항만으로 침강을 흥내 낸다 — 잠긴 부피에 비례하는 부력이 필요해지면 그때 손댈다.
        private void TickWater()
        {
            float y = transform.position.y;
            if (y >= waterLevel) return;

            if (!Splashed)
            {
                Splashed = true;
                Vector3 impactPoint = transform.position;
                impactPoint.y = waterLevel;
                SplashdownStarted?.Invoke(impactPoint);
                _body.linearDamping = waterDamping;
                _body.angularDamping = waterDamping;
                Log.D($"Splashdown at y={y:0.#}", this);
            }

            // 물속에서 무한히 떨어지지 않게 한계 깊이에서 세운다.
            if (y < waterLevel - sinkDepth) _body.isKinematic = true;
        }

        public void StopFlight()
        {
            FlightStopped = true;
            _liftPhase = LiftPhase.None;
            _liftVelocity = Vector3.zero;
            // 홀드 중 자폭도 여기를 지난다 — 끊지 않으면 로켓이 클램프에 갇힌 채로 남는다.
            Holding = false;
            HoldProgress = 0f;
            SetWobble(0f);
            ThrustFraction = 0f;
            foreach (RocketPart engine in _engines) engine.Shutdown();
            _body.isKinematic = true;
        }

        public void Explode()
        {
            if (Exploded || !Launched) return;
            Exploded = true;
            if (explosionPrefab == null) ExplosionPhotoRequested?.Invoke(false);
            StopFlight();
            ThrustFraction = 0f;
            explosionRenderers = GetComponentsInChildren<Renderer>(true);
            rendererVisibility = new bool[explosionRenderers.Length];
            for (int i = 0; i < explosionRenderers.Length; i++)
            {
                rendererVisibility[i] = explosionRenderers[i].enabled;
                explosionRenderers[i].enabled = false;
            }
            if (explosionPrefab != null)
            {
                activeExplosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(activeExplosion.gameObject, gameObject.scene);
                activeExplosion.Play(true);
                ExplosionPhotoRequested?.Invoke(true);
                Destroy(activeExplosion.gameObject, 3f);
            }
        }

        public void ResetFlight(Vector3 position, Quaternion rotation)
        {
            if (activeExplosion != null) Destroy(activeExplosion.gameObject);
            if (explosionRenderers != null)
                for (int i = 0; i < explosionRenderers.Length; i++)
                    if (explosionRenderers[i] != null) explosionRenderers[i].enabled = rendererVisibility[i];
            explosionRenderers = null;
            rendererVisibility = null;
            Exploded = false;
            _groundContacts.Clear();
            foreach (RocketPart engine in _engines) engine.Shutdown();
            _body.isKinematic = false;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            _body.isKinematic = true;
            _body.position = position;
            _body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            _body.mass = _bodyMass;
            _body.linearDamping = initialLinearDamping;
            _body.angularDamping = initialAngularDamping;
            Launched = FlightStopped = Overheated = Splashed = Holding = false;
            TotalBurnSeconds = 0f;
            ThrustFraction = 0f;
            HoldProgress = 0f;
            SetWobble(0f);
            _sinceLaunch = 0f;
            _holdElapsed = 0f;
            _ignitedFraction = 0f;
            _maxThrust = 0f;
            _liveEngines = 0;
            _liftPhase = LiftPhase.None;
            _liftElapsed = _physicsBlendElapsed = 0f;
            _liftVelocity = Vector3.zero;
            _engines.Clear();
        }

        private void FixedUpdate()
        {
            if (Holding)
            {
                TickHold();
                return;
            }

            if (Launched && !_body.isKinematic) TickWater();
            if (!Launched || Overheated || Splashed || FlightStopped)
            {
                ThrustFraction = 0f;
                return;
            }

            float physicsBlend = UpdateLiftBlend(Time.fixedDeltaTime);
            _sinceLaunch += Time.fixedDeltaTime;
            float ramp = RampFactor(_sinceLaunch, ignitionRampSeconds);
            float applied = 0f;
            bool hasUpwardEngine = false;

            for (int i = 0; i < _engines.Count; i++)
            {
                RocketPart engine = _engines[i];
                bool burned = engine.Tick(Time.fixedDeltaTime, ramp);
                if (burned) TotalBurnSeconds += Time.fixedDeltaTime;

                if (engine.Overheated)
                {
                    Overheated = true;
                    ThrustFraction = 0f;
                    Log.D($"Overheat: {engine.name} hit {EngineStatsSO.CriticalTemperature} °C", this);
                    Explode();
                    OverheatExplosionStarted?.Invoke();
                    return;
                }

                if (!burned) continue;

                // 무게중심이 아니라 엔진 위치에 힘을 건다. 비대칭 배치가 그대로 토크가 된다.
                // 방향은 로켓이 아니라 엔진 자신의 up — 설계 단계에서 회전시킨 자세가 곧 추력 방향이다.
                float output = engine.OutputAt(ramp);
                if (!_body.isKinematic)
                    _body.AddForceAtPosition(engine.transform.up * (output * physicsBlend), engine.transform.position);
                applied += output;
                hasUpwardEngine |= engine.HasFuel && output > 0f
                    && Vector3.Dot(engine.transform.up, Vector3.up) > 0f;

                if (engine.HasFuel) continue;

                _liveEngines--;
                Log.D(_liveEngines > 0
                    ? $"Fuel out: {engine.name}, {_liveEngines} engine(s) left"
                    : $"Fuel out: {engine.name}, all engines dry", this);
            }

            ApplyLiftAssist(Time.fixedDeltaTime, physicsBlend, hasUpwardEngine);
            ThrustFraction = _maxThrust > 0f ? applied / _maxThrust : 0f;
        }
    }
}
