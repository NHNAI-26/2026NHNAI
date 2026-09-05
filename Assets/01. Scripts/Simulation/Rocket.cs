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

        [Header("Splashdown")]
        [Tooltip("수면 높이(월드 y). 씬의 Ground 와 같은 값이어야 한다.")]
        [SerializeField] private float waterLevel = -8.9f;
        [SerializeField] private float waterDamping = 4f;
        [Tooltip("수면 아래 이만큼 내려가면 멈춘다.")]
        [SerializeField] private float sinkDepth = 30f;

        private readonly List<RocketPart> _engines = new();
        private readonly DeterministicRng _rng = new();
        private Rigidbody _body;
        private float _bodyMass;
        private int _liveEngines;

        public bool Launched { get; private set; }

        /// <summary>과열로 발사가 끝났는지. 한 발사에 주요 사고는 하나뿐이라 이후 추력을 걸지 않는다.</summary>
        public bool Overheated { get; private set; }

        /// <summary>수면 아래로 내려갔는지. 추력은 여기서 끝난다.</summary>
        public bool Splashed { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            // 본체 무게의 원본은 씬의 Rigidbody 다 — 코드에 복제하지 않는다. 발사 때 mass 를 덮어쓰므로
            // 덮어쓰기 전 값을 여기서 잡아 둔다.
            _bodyMass = _body.mass;
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

            Launched = true;
            Overheated = false;
            _body.isKinematic = false;
            // 접지 속도가 90 m/s 를 넘는다. 0.02초 스텝이면 한 번에 1.8 m 이동이라
            // Discrete 판정으로는 두께 0 인 지면 평면을 그대로 통과한다.
            _body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            // ponytail: engine list frozen at launch; re-collect if parts ever detach mid-flight
            GetComponentsInChildren(_engines);

            // 같은 시드면 같은 점화 결과가 나온다.
            _rng.Reseed(launchSeed);

            _liveEngines = 0;
            float mass = _bodyMass;
            for (int i = 0; i < _engines.Count; i++)
            {
                _engines[i].Prepare(_rng);
                if (_engines[i].Ignited) _liveEngines++;
                if (_engines[i].HasStats) mass += _engines[i].Stats.FuelCapacity * tankMassPerFuel;
            }

            // 탱크가 클수록 오래 타지만 그만큼 무겁다. 점화에 실패한 엔진의 탱크도 무게는 그대로 싣고 간다.
            // 연소 중에는 줄지 않는다 — 발사 순간에 한 번 정하고 끝이다.
            _body.mass = mass;

            Log.D($"Launch: {_liveEngines}/{_engines.Count} engine(s) ignited, {mass:0.#} kg", this);
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

        private void FixedUpdate()
        {
            if (Launched && !_body.isKinematic) TickWater();
            if (!Launched || Overheated || Splashed) return;

            for (int i = 0; i < _engines.Count; i++)
            {
                RocketPart engine = _engines[i];
                bool burned = engine.Tick(Time.fixedDeltaTime);

                if (engine.Overheated)
                {
                    Overheated = true;
                    Log.D($"Overheat: {engine.name} hit {EngineStatsSO.CriticalTemperature} °C", this);
                    return;
                }

                if (!burned) continue;

                // 무게중심이 아니라 엔진 위치에 힘을 건다. 비대칭 배치가 그대로 토크가 된다.
                // 방향은 로켓이 아니라 엔진 자신의 up — 설계 단계에서 회전시킨 자세가 곧 추력 방향이다.
                _body.AddForceAtPosition(engine.transform.up * engine.Output, engine.transform.position);

                if (engine.HasFuel) continue;

                _liveEngines--;
                Log.D(_liveEngines > 0
                    ? $"Fuel out: {engine.name}, {_liveEngines} engine(s) left"
                    : $"Fuel out: {engine.name}, all engines dry", this);
            }
        }
    }
}
