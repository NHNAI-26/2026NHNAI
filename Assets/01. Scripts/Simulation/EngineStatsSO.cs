using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 엔진 프리셋 한 개. 다섯 값을 모두 물리 단위로 저장하고, 설계와 발사는 변환 없이 그대로 쓴다.
    /// 기획 근거는 <c>docs/specs/engine-preset-stats-spec.md</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "EngineStats", menuName = "Simulation/Engine Stats")]
    public sealed class EngineStatsSO : ScriptableObject
    {
        /// <summary>모든 엔진이 공유하는 포화 한계 온도(°C). 넘으면 과열로 발사가 끝난다.</summary>
        public const float CriticalTemperature = 300f;

        /// <summary>출력 1N 당 초당 발열(°C/s). 기준 프리셋 1200N 이 60°C/s 를 내도록 잡았다.</summary>
        public const float HeatPerNewton = 0.05f;

        /// <summary>출력 1N 당 초당 연료 소모(kg/s). 기준 프리셋 1200N 이 20kg/s 를 태우도록 잡았다.</summary>
        public const float FuelPerNewton = 20f / 1200f;

        [SerializeField] private int price = 350;                                   // 표시·밸런스 전용, 자원을 차감하지 않는다
        [SerializeField] private float fuelCapacity = 100f;                         // kg
        [SerializeField] private float cooling = 60f;                               // °C/s, 열 방출량
        [SerializeField] private float maxOutput = 1200f;                           // N
        [SerializeField, Range(0f, 100f)] private float ignitionReliability = 100f; // %

        public int Price => price;
        public float FuelCapacity => fuelCapacity;
        public float Cooling => cooling;
        public float MaxOutput => maxOutput;
        public float IgnitionReliability => ignitionReliability;

        /// <summary>
        /// 연소율은 여섯 번째 필드로 두지 않고 출력에서 파생한다 — 추력이 크면 연료를 그만큼 빨리 쓴다.
        /// </summary>
        public float BurnRateAt(float output) => output * FuelPerNewton;

        /// <summary>
        /// 발열은 프리셋의 최대 출력이 아니라 <paramref name="output"/> — 실제로 내고 있는 출력 — 을 따른다.
        /// 힘을 낮추면 추력을 내주는 대신 열 여유를 얻는다.
        /// </summary>
        public float HeatRateAt(float output) => output * HeatPerNewton;

        private void OnValidate()
        {
            price = Mathf.Max(0, price);
            fuelCapacity = Mathf.Max(0f, fuelCapacity);
            cooling = Mathf.Max(0f, cooling);
            maxOutput = Mathf.Max(0f, maxOutput);
            ignitionReliability = Mathf.Clamp(ignitionReliability, 0f, 100f);
        }
    }
}
