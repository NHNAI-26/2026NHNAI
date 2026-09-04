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

        private readonly List<RocketPart> _engines = new();
        private readonly DeterministicRng _rng = new();
        private Rigidbody _body;
        private int _liveEngines;

        public bool Launched { get; private set; }

        /// <summary>과열로 발사가 끝났는지. 한 발사에 주요 사고는 하나뿐이라 이후 추력을 걸지 않는다.</summary>
        public bool Overheated { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
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
            for (int i = 0; i < _engines.Count; i++)
            {
                _engines[i].Prepare(_rng);
                if (_engines[i].Ignited) _liveEngines++;
            }

            Log.D($"Launch: {_liveEngines}/{_engines.Count} engine(s) ignited", this);
        }

        private void FixedUpdate()
        {
            if (!Launched || Overheated) return;

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
