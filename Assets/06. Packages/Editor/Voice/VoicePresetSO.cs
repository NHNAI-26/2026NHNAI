using UnityEngine;

namespace Border.Voice.Editor
{
    /// <summary>
    /// 캐릭터 한 명의 지껄임(gibberish) 목소리 설정. 굽는 시점에만 쓰이므로 에디터 전용 어셈블리에 있다.
    /// 런타임은 구워진 .wav 만 보고 이 에셋을 모른다 — 런타임 스크립트에서 참조하면 플레이어 빌드에서 깨진다.
    /// </summary>
    [CreateAssetMenu(fileName = "VoicePreset", menuName = "Voice/Voice Preset")]
    public sealed class VoicePresetSO : ScriptableObject
    {
        [SerializeField, Tooltip("무의미 음절 샘플 풀. 80~150ms 짜리 4~8개면 충분하다.")]
        private AudioClip[] syllables = new AudioClip[0];

        [SerializeField, Tooltip("글자마다 뽑는 재생 배속. 속도와 피치가 같이 움직인다(Audacity 의 Change Speed). " +
                                 "0.8 근처는 아저씨, 1.4 근처는 꼬마.")]
        private Vector2 pitchRange = new Vector2(0.9f, 1.15f);

        [SerializeField, Min(0.001f), Tooltip("글자 하나당 시간. 타자기 연출 속도와 맞춘다.")]
        private float secondsPerChar = 0.06f;

        [SerializeField, Range(0f, 1f)]
        private float volume = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("글자마다 볼륨을 흔드는 비율. 0 이면 균일하다.")]
        private float volumeJitter = 0.1f;

        public AudioClip[] Syllables => syllables;
        public Vector2 PitchRange => pitchRange;
        public float SecondsPerChar => secondsPerChar;
        public float Volume => volume;
        public float VolumeJitter => volumeJitter;
    }
}
