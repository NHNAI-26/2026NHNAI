using System.Collections.Generic;
using Border.Core;
using Border.Research;
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

        // 프리셋 외형. 연구가 만든 프리셋은 스탯 구성에서 아키타입이 정해지고(EngineVisualClassifier),
        // 그 아키타입의 메시가 기본 메시(meshRoot)를 대체한다. 라이브러리가 비어 있으면 교체 자체가
        // 일어나지 않는다 — 프리팹을 거치지 않고 만든 부품(EditMode 테스트)이 그 경로다.
        [SerializeField] private Transform meshRoot;
        [SerializeField] private EnginePresetVisualLibrarySO visualLibrary;

        // Uber 3D Object 셰이더의 두 기능을 부품 단위로 켠다. MaterialPropertyBlock 은 못 쓴다 —
        // _StencilOutlineEnabled 는 포워드 패스의 스텐실 WriteMask 라 렌더 스테이트고, 키워드도
        // 블록으로는 못 바꾼다. 그래서 렌더러당 머티리얼 인스턴스를 하나 만들어 들고 있는다.
        private static readonly int OutlineEnabledId = Shader.PropertyToID("_StencilOutlineEnabled");
        private static readonly int HologramEnabledId = Shader.PropertyToID("_HologramEnabled");
        private const string OutlineKeyword = "_STENCIL_OUTLINE_ON";
        private const string HologramKeyword = "_HOLOGRAM_ON";
        private const string OutlinePass = "StencilOutline";

        // 로컬 bounds 여덟 꼭짓점의 부호. 회전에 안전한 bounds 환산에 쓴다.
        private static readonly Vector3[] BoundsCorners =
        {
            new(-1f, -1f, -1f), new(-1f, -1f, 1f), new(-1f, 1f, -1f), new(-1f, 1f, 1f),
            new(1f, -1f, -1f), new(1f, -1f, 1f), new(1f, 1f, -1f), new(1f, 1f, 1f),
        };

        private float _remaining;
        private float _flameSpeed;
        private float _flameRate;
        private bool _flameCaptured;
        private float _temperature;
        private bool _ignited;
        private Material[] _uberMaterials;

        public EngineStatsSO Stats => stats;
        public bool HasStats => stats != null;

        /// <summary>
        /// 설계 화면의 프리셋 패널에서 꺼낸 인스턴스에 프리셋을 심는다. 씬 인스턴스에만 쓰는 경로이며
        /// 프리셋 에셋 자체는 건드리지 않는다 — 스탯은 여전히 SO 쪽이 원본이다.
        /// 외형도 여기서 갈린다: 스탯이 정해지는 지점이 곧 아키타입이 정해지는 지점이다.
        /// </summary>
        public void ApplyPreset(EngineStatsSO preset)
        {
            stats = preset;

            // 라이브러리가 없으면 외형 교체를 통째로 건너뛴다. ResearchFlowSession.GetOrCreate() 는
            // 이름 그대로 오브젝트를 만드는 호출이라, 이 가드가 EditMode 테스트를 연구 세션에서 떼어 놓는다.
            // SimulationTest 단독 재생은 여기를 지나지만 저작 에셋의 PresetIndex 가 -1 이라 아래에서 걸린다.
            if (visualLibrary == null) return;

            SetMesh(ResolveMeshPrefab(preset, ResearchFlowSession.GetOrCreate().Model, visualLibrary));
        }

        /// <summary>
        /// 프리셋에 맞는 메시 프리팹. 연구 런타임 사본만 슬롯 인덱스 0..9 를 가지므로
        /// (<see cref="EngineStatsSO.CreateRuntimeCopy"/>) 저작 에셋(-1)은 여기서 null 로 떨어져 프리팹
        /// 기본 메시를 그대로 쓴다 — <c>RocketDesignUI.TryGetPresetId</c> 와 같은 판별이다.
        /// 아키타입 해석은 연구 프리뷰와 같은 <see cref="EnginePresetVisualLibrarySO.GetPreviewPrefab"/>
        /// 규칙을 타므로 두 화면의 외형이 어긋나지 않는다.
        /// </summary>
        private static GameObject ResolveMeshPrefab(
            EngineStatsSO preset, ResearchPrototypeModel model, EnginePresetVisualLibrarySO library)
        {
            if (preset == null || model == null || library == null) return null;

            int index = preset.PresetIndex;
            if (index < 0 || index >= ResearchPrototypeModel.MaxEnginePresetCount) return null;

            var presetId = (EnginePresetId)index;
            return library.GetPreviewPrefab(
                presetId, EngineVisualClassifier.Classify(model.GetEnginePreset(presetId)));
        }

        /// <summary>
        /// 기본 메시를 아키타입 메시로 갈아끼운다. 엔진 프리팹은 전부 루트에 X −90° 를 이미 들고 있으므로
        /// 회전은 복사하지 않는다. 아트 원본 스케일을 그대로 쓰기로 했으므로 치수가 프리팹마다 다르고,
        /// 콜라이더와 불꽃은 <see cref="FitToMesh"/> 가 뒤따라 맞춘다.
        /// </summary>
        private void SetMesh(GameObject prefab)
        {
            if (prefab == null) return; // 매핑이 비면 프리팹 기본 메시를 그대로 둔다

            ReleaseMaterials(); // 렌더러가 통째로 바뀐다 — 우리가 만든 인스턴스를 여기서 반납한다

            if (meshRoot != null)
            {
                // Destroy 는 프레임 끝으로 미뤄진다. 꺼두지 않으면 머티리얼이 사라진 렌더러가 한 프레임 그려진다.
                meshRoot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(meshRoot.gameObject);
                else DestroyImmediate(meshRoot.gameObject);
            }

            meshRoot = Instantiate(prefab, transform, false).transform;
            meshRoot.localPosition = Vector3.zero; // Engine_01 프리팹만 씬에서 딴 좌표가 박혀 있다
            FitToMesh();
        }

        /// <summary>
        /// 콜라이더와 불꽃을 새 메시 치수에 맞춘다. 프리팹에 박힌 <c>(0.547, 1, 0.541)</c> 은 이제 기본
        /// 메시의 값일 뿐 계약이 아니다 — 아트 원본 스케일을 쓰기로 한 대가다.
        /// 메시를 옮겨 기하 중심을 부품 원점으로 끌어오는 것이 핵심이다: <c>RocketBuilder.HalfExtents</c> 가
        /// <c>BoxCollider.center</c> 를 0 으로 가정한다. 콜라이더는 <b>BoxCollider 로 남아야</b> 한다 —
        /// <c>Rocket.CacheBodyShape()</c> 가 <c>GetComponentInChildren&lt;CapsuleCollider&gt;()</c> 로 몸통을 찾는다.
        /// </summary>
        private void FitToMesh()
        {
            if (!TryGetComponent(out BoxCollider box) || !TryLocalBounds(meshRoot, out Bounds bounds)) return;

            meshRoot.localPosition -= bounds.center;
            box.center = Vector3.zero;
            box.size = bounds.size;

            // 불꽃은 노즐 바닥에서 나온다. 프리팹 기본값 −0.5 는 길이 1 짜리 기본 메시의 바닥이었다.
            if (flame != null) flame.transform.localPosition = new Vector3(0f, -bounds.extents.y, 0f);
        }

        /// <summary>
        /// 부품 로컬 공간의 렌더러 합 bounds. <see cref="Renderer.bounds"/>(월드 AABB)를 되돌리면 부품이
        /// 돌아가 있을 때 부풀어 오르므로, 로컬 bounds 의 여덟 꼭짓점을 행렬로 직접 옮긴다.
        /// </summary>
        private bool TryLocalBounds(Transform mesh, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (Renderer renderer in mesh.GetComponentsInChildren<Renderer>(true))
            {
                Matrix4x4 toPart = transform.worldToLocalMatrix * renderer.localToWorldMatrix;
                Bounds local = renderer.localBounds;

                foreach (Vector3 sign in BoundsCorners)
                {
                    Vector3 point = toPart.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));
                    if (!any)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                        continue;
                    }

                    bounds.Encapsulate(point);
                }
            }

            return any;
        }

        /// <summary>실제로 내고 있는 출력(N). 발열과 연료 소모가 모두 이 값을 따른다.</summary>
        public float Output => stats == null ? 0f : stats.MaxOutput * throttle;

        /// <summary>
        /// 이륙 램프가 걸린 출력. 램프 시계는 <see cref="Rocket"/> 에 하나뿐이라 배율을 인자로 받는다.
        /// 힘·연소·발열·불꽃이 전부 이 값 하나를 따라야 패드 위에서 연료만 버리는 일이 없다.
        /// </summary>
        public float OutputAt(float thrustScale) => Output * Mathf.Clamp01(thrustScale);

        public float Remaining => _remaining;
        public float Temperature => _temperature;
        public bool Ignited => _ignited;
        public bool HasFuel => _remaining > 0f;
        public bool Overheated => _temperature >= EngineStatsSO.CriticalTemperature;

        public void Shutdown()
        {
            _ignited = false;
            SetFlame(false, 0f);
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
                SetFlame(false, 0f);
                Log.W($"{name}: no engine stats assigned, engine stays cold", this);
                return;
            }

            _remaining = stats.FuelCapacity;
            _ignited = rng.Next(1, 101) <= stats.IgnitionReliability;
            if (!_ignited)
            {
                SetFlame(false, 0f);
                Log.D($"Ignition failed: {name} ({stats.IgnitionReliability}%)", this);
            }
        }

        /// <summary>
        /// deltaTime 만큼 연료를 태우고 온도를 갱신한다. 추력을 낼 수 있었으면 true.
        /// 소진 프레임은 남은 양보다 조금 더 태울 수 있지만 한 프레임 오차라 무시한다.
        /// </summary>
        public bool Tick(float deltaTime, float thrustScale = 1f)
        {
            if (stats == null) return false;

            bool burning = _ignited && _remaining > 0f;
            // 한 스텝 안에서는 출력이 하나여야 한다 — 연소와 발열이 다른 값을 보면 램프가 반만 걸린다.
            float output = OutputAt(thrustScale);
            if (burning)
                _remaining = Mathf.Max(0f, _remaining - stats.BurnRateAt(output) * deltaTime);

            // 꺼진 엔진은 발열이 0이라 냉각만 남는다 — ON/OFF 타이밍이 곧 과열 관리 수단이다.
            float heat = burning ? stats.HeatRateAt(output) : 0f;
            _temperature = Mathf.Max(0f, _temperature + (heat - stats.Cooling) * deltaTime);

            SetFlame(burning, thrustScale);
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

        private void OnDestroy() => ReleaseMaterials();

        /// <summary>
        /// 인스턴스는 우리가 만들었으니 우리가 지운다. 부품 파괴와 메시 교체가 둘 다 여기를 지난다 —
        /// 교체 때 반납하지 않으면 갈아끼울 때마다 머티리얼이 샌다.
        /// EditMode 테스트에서도 도는 경로라 분기한다.
        /// </summary>
        private void ReleaseMaterials()
        {
            if (_uberMaterials == null) return;

            foreach (Material material in _uberMaterials)
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);

            _uberMaterials = null;
        }

        /// <summary>
        /// 불꽃은 추력이 실제로 나오는 동안에만 켜진다. 발사 전에는 <c>Tick</c> 이 호출되지 않고
        /// 파티클의 Play On Awake 도 꺼져 있으므로 자동으로 꺼진 상태다.
        /// </summary>
        private void SetFlame(bool on, float scale)
        {
            if (flame == null) return;

            // 배기는 추력을 따라 자란다 — 이륙 램프가 화면에 보이는 유일한 신호다. 파티클 트랜스폼
            // 스케일이 아니라 배수를 쓴다: 루트 스케일은 노즐 위치·입자 크기와 얽혀 있어 손대면
            // 예전의 비균등 스케일 상쇄 문제가 그대로 돌아온다.
            if (on)
            {
                CaptureFlameDefaults();
                ParticleSystem.MainModule main = flame.main;
                main.startSpeedMultiplier = _flameSpeed * scale;
                ParticleSystem.EmissionModule emission = flame.emission;
                emission.rateOverTimeMultiplier = _flameRate * scale;
            }

            if (flame.isEmitting == on) return;

            if (on) flame.Play();
            else flame.Stop(true, ParticleSystemStopBehavior.StopEmitting); // 남은 입자는 수명대로 사라진다
        }

        /// <summary>
        /// 프리팹이 저작한 불꽃 세기. 배수를 덮어쓰기 전에 한 번만 잡는다 — 이 컴포넌트에는 Awake 가
        /// 없고 EditMode 테스트도 Awake 를 돌리지 않으므로, 지연 캡처가 두 경로를 같이 만족시킨다.
        /// </summary>
        private void CaptureFlameDefaults()
        {
            if (_flameCaptured) return;

            _flameCaptured = true;
            _flameSpeed = flame.main.startSpeedMultiplier;
            _flameRate = flame.emission.rateOverTimeMultiplier;
        }
    }
}
