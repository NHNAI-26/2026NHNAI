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
        // ponytail: 계수 하나를 모든 엔진이 공유한다. 프리셋마다 탱크 밀도를 다르게 하고 싶어지면 그때
        // EngineStatsSO 필드로 내린다 — CreateRuntimeCopy 와 리서치 브리지도 같이 넓어진다.
        [SerializeField] private float tankMassPerFuel = 0.25f; // 연료 1kg 당 탱크 무게(kg)

        [Tooltip("점화부터 최대 추력까지 걸리는 시간(초). 0 이면 발사 첫 프레임에 최대 추력이다.")]
        [SerializeField, Min(0f)] private float ignitionRampSeconds = 1.2f;

        [Header("Splashdown")]
        [Tooltip("수면 높이(월드 y). 씬의 Ground 와 같은 값이어야 한다.")]
        [SerializeField] private float waterLevel = -6.71f;
        [SerializeField] private float waterDamping = 4f;
        [Tooltip("수면 아래 이만큼 내려가면 멈춘다.")]
        [SerializeField] private float sinkDepth = 30f;

        private readonly List<RocketPart> _engines = new();
        private readonly DeterministicRng _rng = new();
        private Rigidbody _body;
        private float _bodyMass;
        private float initialLinearDamping;
        private float initialAngularDamping;
        private int _liveEngines;
        private float _sinceLaunch;
        private float _maxThrust;

        public bool Launched { get; private set; }
        public System.Func<bool> AuthorizeLaunch { get; set; }
        public event System.Action LaunchStarted;
        public bool FlightStopped { get; private set; }
        public float TotalBurnSeconds { get; private set; }

        /// <summary>
        /// 이번 스텝에 실제로 건 추력 ÷ 전 엔진 최대 추력. 연출이 읽는 값이다(발사 카메라 흔들림).
        /// 점화에 실패한 엔진이 있으면 1 에 닿지 않는다 — 반만 점화한 발사는 반만 흔들린다.
        /// </summary>
        public float ThrustFraction { get; private set; }

        /// <summary>과열로 발사가 끝났는지. 한 발사에 주요 사고는 하나뿐이라 이후 추력을 걸지 않는다.</summary>
        public bool Overheated { get; private set; }

        /// <summary>수면 아래로 내려갔는지. 추력은 여기서 끝난다.</summary>
        public bool Splashed { get; private set; }

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
            Overheated = false;
            _sinceLaunch = 0f;
            _body.isKinematic = false;
            // 접지 속도가 90 m/s 를 넘는다. 0.02초 스텝이면 한 번에 1.8 m 이동이라
            // Discrete 판정으로는 두께 0 인 지면 평면을 그대로 통과한다.
            _body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            // ponytail: engine list frozen at launch; re-collect if parts ever detach mid-flight
            GetComponentsInChildren(_engines);

            // 같은 시드면 같은 점화 결과가 나온다.
            _rng.Reseed(launchSeed);

            _liveEngines = 0;
            _maxThrust = 0f;
            float mass = _bodyMass;
            for (int i = 0; i < _engines.Count; i++)
            {
                _engines[i].Prepare(_rng);
                if (_engines[i].Ignited) _liveEngines++;
                if (_engines[i].HasStats) mass += _engines[i].Stats.FuelCapacity * tankMassPerFuel;
                _maxThrust += _engines[i].Output; // ThrustFraction 의 분모 — 점화 실패분도 들어간다.
            }

            // 탱크가 클수록 오래 타지만 그만큼 무겁다. 점화에 실패한 엔진의 탱크도 무게는 그대로 싣고 간다.
            // 연소 중에는 줄지 않는다 — 발사 순간에 한 번 정하고 끝이다.
            _body.mass = mass;

            Log.D($"Launch: {_liveEngines}/{_engines.Count} engine(s) ignited, {mass:0.#} kg", this);
            LaunchStarted?.Invoke();
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
            ThrustFraction = 0f;
            foreach (RocketPart engine in _engines) engine.Shutdown();
            _body.isKinematic = true;
        }

        public void ResetFlight(Vector3 position, Quaternion rotation)
        {
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
            Launched = FlightStopped = Overheated = Splashed = false;
            TotalBurnSeconds = 0f;
            ThrustFraction = 0f;
            _sinceLaunch = 0f;
            _maxThrust = 0f;
            _liveEngines = 0;
            _engines.Clear();
        }

        private void FixedUpdate()
        {
            if (Launched && !_body.isKinematic) TickWater();
            if (!Launched || Overheated || Splashed || FlightStopped)
            {
                ThrustFraction = 0f;
                return;
            }

            // 점화 직후에는 추력이 0 에서 올라온다. 그동안 로켓은 발사대 데크 위에 그대로 앉아 있다 —
            // 연소와 발열도 같은 배율을 타므로(RocketPart.Tick) 패드 위에서 연료를 헛되이 버리지 않고,
            // 잃는 것은 늦게 뜬 만큼의 중력 손실뿐이다.
            _sinceLaunch += Time.fixedDeltaTime;
            float ramp = RampFactor(_sinceLaunch, ignitionRampSeconds);
            float applied = 0f;

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
                    return;
                }

                if (!burned) continue;

                // 무게중심이 아니라 엔진 위치에 힘을 건다. 비대칭 배치가 그대로 토크가 된다.
                // 방향은 로켓이 아니라 엔진 자신의 up — 설계 단계에서 회전시킨 자세가 곧 추력 방향이다.
                float output = engine.OutputAt(ramp);
                _body.AddForceAtPosition(engine.transform.up * output, engine.transform.position);
                applied += output;

                if (engine.HasFuel) continue;

                _liveEngines--;
                Log.D(_liveEngines > 0
                    ? $"Fuel out: {engine.name}, {_liveEngines} engine(s) left"
                    : $"Fuel out: {engine.name}, all engines dry", this);
            }

            ThrustFraction = _maxThrust > 0f ? applied / _maxThrust : 0f;
        }
    }
}
