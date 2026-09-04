using System;
using System.Collections.Generic;
using UnityEngine;

namespace Border.Prologue
{
    /// <summary>
    /// 프롤로그의 한 컷. <see cref="Line"/> 이 비어 있으면 텍스트 없이 검은 화면만 유지한다.
    /// </summary>
    [Serializable]
    public sealed class PrologueBeat
    {
        [SerializeField, TextArea(1, 3)] private string line = string.Empty;
        [Tooltip("글자당 노출 시간. 0 이면 타이핑 없이 fadeInSeconds 로 서서히 나타난다.")]
        [SerializeField, Min(0f)] private float typeSecondsPerChar = 0.06f;

        [SerializeField, Min(0f)] private float fadeInSeconds = 0.8f;
        [SerializeField, Min(0f)] private float holdSeconds = 2.5f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.8f;
        [SerializeField, Min(1f)] private float fontSize = 48f;
        [SerializeField] private string sfxId = string.Empty;

        public string Line => line;
        public float TypeSecondsPerChar => typeSecondsPerChar;
        public float FadeInSeconds => fadeInSeconds;
        public float HoldSeconds => holdSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
        public float FontSize => fontSize;
        public string SfxId => sfxId;

        /// <summary>글자를 하나씩 찍어 보여주는 컷인지. 빈 줄이면 찍을 것이 없어 항상 false 다.</summary>
        public bool UsesTyping => typeSecondsPerChar > 0f && line.Length > 0;

        public float TypeSeconds => typeSecondsPerChar * line.Length;

        // 타이핑 컷은 fadeIn 대신 타이핑 시간이 등장 구간을 차지한다.
        public float TotalSeconds => (UsesTyping ? TypeSeconds : fadeInSeconds) + holdSeconds + fadeOutSeconds;
    }

    /// <summary>
    /// 프롤로그 연출 데이터. 대사·타이밍·사운드 id 를 전부 인스펙터에서 편집한다.
    /// 컷 수가 고정 필드가 아니라 리스트인 이유는 컷 추가·삭제·순서 변경을 코드 수정 없이 하기 위함이다.
    /// 재생 길이는 오디오 클립 길이가 아니라 이 데이터에서만 나온다 — 클립이 없어도 연출이 정상 진행된다.
    /// </summary>
    [CreateAssetMenu(fileName = "PrologueSequence", menuName = "Border/Prologue/Prologue Sequence")]
    public sealed class PrologueSequenceSO : ScriptableObject
    {
        [SerializeField] private List<PrologueBeat> beats = new();
        [SerializeField] private string bgmId = string.Empty;
        [SerializeField, Min(0f)] private float bgmFadeInSeconds = 2f;

        [Tooltip("마지막 컷 뒤 검은 오버레이를 걷어 운영 화면을 드러내는 시간.")]
        [SerializeField, Min(0f)] private float revealSeconds = 1.5f;

        public IReadOnlyList<PrologueBeat> Beats => beats;
        public string BgmId => bgmId;
        public float BgmFadeInSeconds => bgmFadeInSeconds;
        public float RevealSeconds => revealSeconds;

        /// <summary>
        /// 전체 재생 길이. GDD 02 의 20~30초 예산을 테스트에서 검사하는 데 쓴다.
        /// </summary>
        public float TotalSeconds
        {
            get
            {
                float total = revealSeconds;
                foreach (PrologueBeat beat in beats)
                {
                    total += beat.TotalSeconds;
                }

                return total;
            }
        }
    }
}
