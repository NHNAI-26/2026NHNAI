using System.Collections.Generic;
using Border.Core;
using UnityEngine;

namespace Border.Simulation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Rocket : MonoBehaviour
    {
        private readonly List<RocketPart> _engines = new();
        private Rigidbody _body;
        private int _liveEngines;

        public bool Launched { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true; // 발사 전에는 발사대에 고정
        }

        /// <summary>
        /// 로켓 표면의 worldPoint 에 부품을 붙인다. 자세는 로켓 기준을 유지한다 —
        /// 추력 방향이 로켓의 up 고정이라, 표면 법선에 눕히면 보이는 방향과 힘 방향이 어긋난다.
        /// </summary>
        public void Attach(RocketPart part, Vector3 worldPoint)
        {
            part.transform.SetParent(transform, true);
            part.transform.position = worldPoint;
            part.transform.rotation = transform.rotation;
        }

        public void Launch()
        {
            if (Launched) return;

            Launched = true;
            _body.isKinematic = false;
            // 접지 속도가 90 m/s 를 넘는다. 0.02초 스텝이면 한 번에 1.8 m 이동이라
            // Discrete 판정으로는 두께 0 인 지면 평면을 그대로 통과한다.
            _body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            // ponytail: engine list frozen at launch; re-collect if parts ever detach mid-flight
            GetComponentsInChildren(_engines);

            for (int i = 0; i < _engines.Count; i++)
                _engines[i].Refill();

            _liveEngines = _engines.Count;
            Log.D($"Launch: {_engines.Count} engine(s)", this);
        }

        private void FixedUpdate()
        {
            if (!Launched) return;

            for (int i = 0; i < _engines.Count; i++)
            {
                RocketPart engine = _engines[i];
                if (!engine.TryBurn(Time.fixedDeltaTime)) continue;

                // 무게중심이 아니라 엔진 위치에 힘을 건다. 비대칭 배치가 그대로 토크가 된다.
                _body.AddForceAtPosition(transform.up * engine.Thrust, engine.transform.position);

                if (engine.HasFuel) continue;

                _liveEngines--;
                Log.D(_liveEngines > 0
                    ? $"Fuel out: {engine.name}, {_liveEngines} engine(s) left"
                    : $"Fuel out: {engine.name}, all engines dry", this);
            }
        }
    }
}
