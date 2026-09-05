using System.Collections.Generic;
using Border.Core;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 부착 가능한 엔진 부품. 성능은 전부 <see cref="EngineStatsSO"/> 프리셋에서 읽고, 부품은 값을
    /// 바꾸지 않는다. 추력은 뉴턴 단위이며 이 트랜스폼 위치에 걸린다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class RocketPart : MonoBehaviour
    {
        [SerializeField] private EngineStatsSO stats;
        [SerializeField, Range(0f, 1f)] private float throttle = 1f; // 설계 단계의 힘 슬라이더 자리
        [SerializeField] private ParticleSystem flame;

        // Uber 3D Object 셰이더의 두 기능을 부품 단위로 켠다. MaterialPropertyBlock 은 못 쓴다 —
        // _StencilOutlineEnabled 는 포워드 패스의 스텐실 WriteMask 라 렌더 스테이트고, 키워드도
        // 블록으로는 못 바꾼다. 그래서 렌더러당 머티리얼 인스턴스를 하나 만들어 들고 있는다.
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_StencilOutlineEnabled");
        private static readonly int HologramEnabledId = Shader.PropertyToID("_HologramEnabled");
        private const string OutlineKeyword = "_STENCIL_OUTLINE_ON";
        private const string HologramKeyword = "_HOLOGRAM_ON";
        private const string OutlinePass = "StencilOutline";

        private float _remaining;
        private float _temperature;
        private bool _ignited;
        private Material[] _uberMaterials;

        public EngineStatsSO Stats => stats;
        public bool HasStats => stats != null;

        /// <summary>
        /// 설계 화면의 프리셋 패널에서 꺼낸 인스턴스에 프리셋을 심는다. 씬 인스턴스에만 쓰는 경로이며
        /// 프리셋 에셋 자체는 건드리지 않는다 — 스탯은 여전히 SO 쪽이 원본이다.
        /// </summary>
        public void ApplyPreset(EngineStatsSO preset) => stats = preset;

        /// <summary>실제로 내고 있는 출력(N). 발열과 연료 소모가 모두 이 값을 따른다.</summary>
        public float Output => stats == null ? 0f : stats.MaxOutput * throttle;

        public float Remaining => _remaining;
        public float Temperature => _temperature;
        public bool Ignited => _ignited;
        public bool HasFuel => _remaining > 0f;
        public bool Overheated => _temperature >= EngineStatsSO.CriticalTemperature;

        public void Shutdown()
        {
            _ignited = false;
            SetFlame(false);
        }

        /// <summary>
        /// 발사 시점에 연료를 채우고 온도를 0으로 되돌린 뒤 점화 신뢰도로 점화를 판정한다.
        /// 점화에 실패한 엔진은 이번 발사 내내 추력을 내지 않는다.
        /// </summary>
        public void Prepare(DeterministicRng rng)
        {
            _temperature = 0f;

            if (stats == null)
            {
                _remaining = 0f;
                _ignited = false;
                SetFlame(false);
                Log.W($"{name}: no engine stats assigned, engine stays cold", this);
                return;
            }

            _remaining = stats.FuelCapacity;
            _ignited = rng.Next(1, 101) <= stats.IgnitionReliability;
            if (!_ignited)
            {
                SetFlame(false);
                Log.D($"Ignition failed: {name} ({stats.IgnitionReliability}%)", this);
            }
        }

        /// <summary>
        /// deltaTime 만큼 연료를 태우고 온도를 갱신한다. 추력을 낼 수 있었으면 true.
        /// 소진 프레임은 남은 양보다 조금 더 태울 수 있지만 한 프레임 오차라 무시한다.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (stats == null) return false;

            bool burning = _ignited && _remaining > 0f;
            if (burning)
                _remaining = Mathf.Max(0f, _remaining - stats.BurnRateAt(Output) * deltaTime);

            // 꺼진 엔진은 발열이 0이라 냉각만 남는다 — ON/OFF 타이밍이 곧 과열 관리 수단이다.
            float heat = burning ? stats.HeatRateAt(Output) : 0f;
            _temperature = Mathf.Max(0f, _temperature + (heat - stats.Cooling) * deltaTime);

            SetFlame(burning);
            return burning;
        }

        /// <summary>
        /// 선택 표시. 셰이더의 스텐실 아웃라인을 켠다. 머티리얼 에셋(<c>MAT_BaseEngine</c>)은
        /// <c>disabledShaderPasses</c> 에 아웃라인 패스를 꺼둔 상태라 패스도 같이 켜야 한다.
        /// </summary>
        public void SetOutline(bool on)
        {
            foreach (Material material in UberMaterials)
            {
                material.SetFloat(OutlineEnabledId, on ? 1f : 0f);
                if (on) material.EnableKeyword(OutlineKeyword);
                else material.DisableKeyword(OutlineKeyword);
                material.SetShaderPassEnabled(OutlinePass, on);
            }
        }

        /// <summary>
        /// 로켓에서 떨어져 있는 동안의 표시. 머티리얼이 이미 Transparent 라 블렌드나 렌더 큐는
        /// 건드릴 필요가 없고, 홀로그램 수치도 에셋에 이미 들어 있다 — 스위치만 올린다.
        /// </summary>
        public void SetHologram(bool on)
        {
            foreach (Material material in UberMaterials)
            {
                material.SetFloat(HologramEnabledId, on ? 1f : 0f);
                if (on) material.EnableKeyword(HologramKeyword);
                else material.DisableKeyword(HologramKeyword);
            }
        }

        /// <summary>
        /// Uber 셰이더를 쓰는 렌더러의 머티리얼 인스턴스. 불꽃 파티클처럼 다른 셰이더를 쓰는
        /// 렌더러는 프로퍼티가 없어 자동으로 빠진다.
        /// </summary>
        private Material[] UberMaterials
        {
            get
            {
                if (_uberMaterials != null) return _uberMaterials;

                var found = new List<Material>();
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.materials) // 여기서 인스턴스가 만들어진다
                    if (material != null && material.HasProperty(OutlineEnabledId))
                        found.Add(material);

                _uberMaterials = found.ToArray();
                return _uberMaterials;
            }
        }

        private void OnDestroy()
        {
            if (_uberMaterials == null) return;

            // 인스턴스는 우리가 만들었으니 우리가 지운다. EditMode 테스트에서도 도는 경로라 분기한다.
            foreach (Material material in _uberMaterials)
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);

            _uberMaterials = null;
        }

        /// <summary>
        /// 불꽃은 추력이 실제로 나오는 동안에만 켜진다. 발사 전에는 <c>Tick</c> 이 호출되지 않고
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
