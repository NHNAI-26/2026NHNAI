using UnityEngine;

namespace Border.Simulation
{
    /// <summary>부착 가능한 엔진 부품. 추력은 뉴턴 단위이며 이 트랜스폼 위치에 걸린다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RocketPart : MonoBehaviour
    {
        [SerializeField] private float thrust = 1200f;
        [SerializeField] private float fuel = 100f;
        [SerializeField] private float burnRate = 20f;
        [SerializeField] private ParticleSystem flame;

        private float _remaining;

        public float Thrust => thrust;
        public float Fuel => fuel;
        public float BurnRate => burnRate;
        public float Remaining => _remaining;
        public bool HasFuel => _remaining > 0f;

        private void Awake() => _remaining = fuel;

        /// <summary>발사 시점에 연료를 만탱크로 되돌린다.</summary>
        public void Refill() => _remaining = fuel;

        /// <summary>
        /// deltaTime 만큼 연료를 태운다. 추력을 낼 수 있었으면 true.
        /// 소진 프레임은 남은 양보다 조금 더 태울 수 있지만 한 프레임 오차라 무시한다.
        /// </summary>
        public bool TryBurn(float deltaTime)
        {
            if (_remaining <= 0f)
            {
                SetFlame(false);
                return false;
            }

            _remaining = Mathf.Max(0f, _remaining - burnRate * deltaTime);
            SetFlame(true);
            return true;
        }

        /// <summary>
        /// 불꽃은 추력이 실제로 나오는 동안에만 켜진다. 발사 전에는 <c>TryBurn</c> 이 호출되지 않고
        /// 파티클의 Play On Awake 도 꺼져 있으므로 자동으로 꺼진 상태다.
        /// </summary>
        private void SetFlame(bool on)
        {
            if (flame == null || flame.isEmitting == on) return;

            if (on) flame.Play();
            else flame.Stop(true, ParticleSystemStopBehavior.StopEmitting); // 남은 입자는 수명대로 사라진다
        }
    }
}
