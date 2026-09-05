using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Border.Title
{
    /// <summary>
    /// 발사 진동 느낌의 미세한 흔들림. 붙는 대상에 따라 두 가지로 갈린다.
    /// 자식에 <see cref="TMP_Text"/> 가 있으면 <b>마우스를 올린 동안에만</b> 글자 하나하나를 따로 떨고
    /// (메시 정점을 건드린다), 없으면 트랜스폼 전체를 계속 흔든다 — 카메라가 이쪽이다.
    /// 랜덤 대신 <see cref="Mathf.PerlinNoise"/> 를 쓰는 이유는 프레임마다 튀지 않고 이어지는 떨림이
    /// 나와야 "부들부들"로 읽히기 때문이다. 글자마다, 인스턴스마다 위상을 어긋내 한 몸처럼 움직이지 않게 한다.
    /// 호버를 받으려면 레이캐스트 대상이 있는 오브젝트(버튼)에 붙어야 한다 — 라벨이 아니라.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleJitter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Vector3 positionAmplitude = new(3f, 3f, 0f);
        [SerializeField] private Vector3 rotationAmplitude = Vector3.zero;
        [SerializeField] private float frequency = 8f;

        private TMP_Text _text;
        private Vector3 _basePosition;
        private Vector3 _baseEuler;
        private float _seed;
        private bool _hovered;
        private bool _jittering;

        private void Awake()
        {
            _text = GetComponentInChildren<TMP_Text>(true);
            _basePosition = transform.localPosition;
            _baseEuler = transform.localEulerAngles;
            _seed = Random.value * 100f;
        }

        // 비활성화 중에는 OnPointerExit 이 오지 않는다. 다시 켜졌을 때 떨고 있으면 안 된다.
        private void OnDisable() => _hovered = false;

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

        public void OnPointerExit(PointerEventData eventData) => _hovered = false;

        // 레이아웃과 애니메이션이 끝난 뒤에 얹어야 오프셋이 덮이지 않는다.
        private void LateUpdate()
        {
            float t = Time.unscaledTime * frequency + _seed;
            if (_text == null)
            {
                JitterTransform(t);
                return;
            }

            if (_hovered)
            {
                JitterCharacters(t);
                _jittering = true;
            }
            else if (_jittering)
            {
                _text.ForceMeshUpdate(); // 호버가 끝난 프레임에 한 번만 원본 메시로 되돌린다
                _jittering = false;
            }
        }

        private void JitterTransform(float t)
        {
            transform.localPosition = _basePosition + Scale(positionAmplitude, Noise(t));
            if (rotationAmplitude != Vector3.zero)
                transform.localEulerAngles = _baseEuler + Scale(rotationAmplitude, Noise(t + 37f));
        }

        /// <summary>
        /// 글자별 오프셋. <see cref="TMP_Text.ForceMeshUpdate"/> 가 매 프레임 정점을 원본으로 되돌려 주므로
        /// 따로 캐시할 필요가 없다 — 호버 중인 버튼 하나뿐이라 비용도 문제되지 않는다.
        /// </summary>
        private void JitterCharacters(float t)
        {
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;

                Vector3 offset = Scale(positionAmplitude, Noise(t + i * 13.7f));
                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                int v = character.vertexIndex;
                for (int k = 0; k < 4; k++) vertices[v + k] += offset;
            }

            for (int i = 0; i < info.meshInfo.Length; i++)
            {
                info.meshInfo[i].mesh.vertices = info.meshInfo[i].vertices;
                _text.UpdateGeometry(info.meshInfo[i].mesh, i);
            }
        }

        /// <summary>축마다 위상이 다른 −1..1 노이즈 벡터.</summary>
        private static Vector3 Noise(float t) => new(
            Mathf.PerlinNoise(t, 0f) * 2f - 1f,
            Mathf.PerlinNoise(0f, t) * 2f - 1f,
            Mathf.PerlinNoise(t, t) * 2f - 1f);

        private static Vector3 Scale(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}
