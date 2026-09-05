using UnityEngine;

namespace Border.Title
{
    /// <summary>
    /// 발사 진동 느낌의 미세한 흔들림. 카메라와 텍스트가 같은 컴포넌트를 공유한다 — 카메라는 진폭을
    /// 크게 잡고 회전까지 흔들고, 텍스트는 위치만 픽셀 단위로 떤다.
    /// 랜덤 대신 <see cref="Mathf.PerlinNoise"/> 를 쓰는 이유는 프레임마다 튀지 않고 이어지는 떨림이
    /// 나와야 "부들부들"로 읽히기 때문이다. 인스턴스마다 시드를 달리해 여러 텍스트가 한 몸처럼 움직이지
    /// 않게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleJitter : MonoBehaviour
    {
        [SerializeField] private Vector3 positionAmplitude = new(1.6f, 1.6f, 0f);
        [SerializeField] private Vector3 rotationAmplitude = Vector3.zero;
        [SerializeField] private float frequency = 8f;

        private Vector3 _basePosition;
        private Vector3 _baseEuler;
        private float _seed;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _baseEuler = transform.localEulerAngles;
            _seed = Random.value * 100f;
        }

        // 레이아웃과 애니메이션이 끝난 뒤에 얹어야 오프셋이 덮이지 않는다.
        private void LateUpdate()
        {
            float t = Time.unscaledTime * frequency + _seed;
            transform.localPosition = _basePosition + Scale(positionAmplitude, Noise(t));
            if (rotationAmplitude != Vector3.zero)
                transform.localEulerAngles = _baseEuler + Scale(rotationAmplitude, Noise(t + 37f));
        }

        /// <summary>축마다 위상이 다른 −1..1 노이즈 벡터.</summary>
        private static Vector3 Noise(float t) => new(
            Mathf.PerlinNoise(t, 0f) * 2f - 1f,
            Mathf.PerlinNoise(0f, t) * 2f - 1f,
            Mathf.PerlinNoise(t, t) * 2f - 1f);

        private static Vector3 Scale(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
    }
}
