using System.Collections;
using System.Collections.Generic;
using Border.Audio;
using Border.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Border.Prologue
{
    /// <summary>
    /// 검은 전체화면 오버레이 위에서 프롤로그 컷을 순서대로 재생하고, 끝나면 스스로 파괴되어
    /// 그동안 뒤에서 조립을 끝낸 운영 화면(<c>ResearchOperationUIController</c>, sortingOrder 0)을 드러낸다.
    /// 운영 화면을 끄지 않는다 — sortingOrder 200 의 불투명 배경이 그리기와 입력을 동시에 막기 때문이다.
    /// (<c>SimulationStageHost</c> 가 운영 화면 루트를 끄는 것은 3D 씬을 <i>밑에</i> 깔기 때문이고, 여기와 다르다.)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrologueController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private PrologueSequenceSO sequence;
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private TMP_Text lineText;

        private Coroutine routine;
        private readonly List<SoundHandle> typingSounds = new();
        private int typingSoundIndex;
        private static readonly string[] TypingSoundIds =
            { "keyboard01", "keyboard02", "keyboard03", "keyboard04" };

        private void Awake()
        {
            if (sequence == null || sequence.Beats.Count == 0 || overlayGroup == null || lineText == null)
            {
                // 참조가 비면 검은 오버레이가 화면을 영구히 덮어 게임이 잠긴다. 그럴 바엔 프롤로그를 버린다.
                Log.W("[Prologue] 시퀀스나 참조가 비어 있어 프롤로그를 건너뛴다.", this);
                Destroy(gameObject);
                return;
            }

            overlayGroup.alpha = 1f;
            lineText.text = string.Empty;
            lineText.alpha = 0f;
            routine = StartCoroutine(PlayRoutine());
        }

        /// <summary>
        /// 배경 Image 위의 클릭이 부모인 이 컴포넌트까지 버블링돼 들어온다. uGUI 는 핸들러를 찾을 때까지
        /// 부모를 거슬러 올라가므로 Backdrop 에 Button 을 붙이지 않아도 된다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData) => Skip();

        /// <summary>
        /// GDD 02 §2 의 "클릭으로 건너뛸 수 있다" 요구. 남은 컷을 버리고 곧바로 오버레이를 걷는다.
        /// </summary>
        public void Skip()
        {
            if (routine == null)
            {
                return; // 이미 오버레이를 걷는 중이다.
            }

            StopTypingSound();
            StopCoroutine(routine);
            routine = StartCoroutine(RevealRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            PlayBgm();

            foreach (PrologueBeat beat in sequence.Beats)
            {
                PlaySfx(beat.SfxId);

                lineText.text = beat.Line;
                lineText.fontSize = beat.FontSize;

                if (beat.UsesTyping)
                {
                    yield return TypeText(beat);
                }
                else
                {
                    lineText.maxVisibleCharacters = int.MaxValue;
                    yield return FadeText(0f, 1f, beat.FadeInSeconds);
                }

                yield return new WaitForSecondsRealtime(beat.HoldSeconds);
                yield return FadeText(1f, 0f, beat.FadeOutSeconds);
            }

            yield return RevealRoutine();
        }

        private IEnumerator RevealRoutine()
        {
            StopTypingSound();
            routine = null; // Skip() 재진입 차단
            lineText.alpha = 0f;
            lineText.maxVisibleCharacters = int.MaxValue;

            if (SoundManager.Instance != null && !string.IsNullOrEmpty(sequence.BgmId))
            {
                SoundManager.Instance.StopBgm(sequence.RevealSeconds);
            }

            float from = overlayGroup.alpha;
            for (float elapsed = 0f; elapsed < sequence.RevealSeconds; elapsed += Time.unscaledDeltaTime)
            {
                overlayGroup.alpha = Mathf.Lerp(from, 0f, elapsed / sequence.RevealSeconds);
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// 글자를 하나씩 드러낸다. 알파를 건드리지 않고 <see cref="TMP_Text.maxVisibleCharacters"/> 만 올리므로
        /// 레이아웃이 처음부터 확정돼 줄이 늘어날 때 텍스트가 위아래로 튀지 않는다.
        /// </summary>
        private IEnumerator TypeText(PrologueBeat beat)
        {
            lineText.alpha = 1f;
            lineText.maxVisibleCharacters = 0;

            // 리치 텍스트를 쓰지 않으므로 파싱된 글자 수와 문자열 길이가 같다.
            int total = beat.Line.Length;
            float elapsed = 0f;

            while (lineText.maxVisibleCharacters < total)
            {
                elapsed += Time.unscaledDeltaTime;
                int previous = lineText.maxVisibleCharacters;
                int visible = Mathf.Min(total, Mathf.FloorToInt(elapsed / beat.TypeSecondsPerChar));
                lineText.maxVisibleCharacters = visible;
                PlayTypingSound(beat.Line, previous, visible);
                yield return null;
            }

            lineText.maxVisibleCharacters = total;
            StopTypingSound();
        }

        private void PlayTypingSound(string text, int previous, int visible)
        {
            if (visible <= previous) return;
            typingSounds.RemoveAll(handle => !handle.IsValid);
            // A slow frame may reveal several characters; play only one key for that frame.
            for (int i = previous; i < visible; i++)
            {
                if (char.IsWhiteSpace(text[i])) continue;
                if (SoundManager.Instance != null)
                {
                    typingSounds.Add(SoundManager.Instance.PlaySfx(TypingSoundIds[typingSoundIndex]));
                    typingSoundIndex = (typingSoundIndex + 1) % TypingSoundIds.Length;
                }
                break;
            }
        }

        private void StopTypingSound()
        {
            foreach (SoundHandle sound in typingSounds) sound.Stop();
            typingSounds.Clear();
        }

        private void OnDisable()
        {
            StopTypingSound();
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        /// <summary>
        /// 시간이 0 이면 루프에 들어가지 않고 마지막 대입만 남아 값이 확정된다.
        /// </summary>
        private IEnumerator FadeText(float from, float to, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                lineText.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }

            lineText.alpha = to;
        }

        // 사운드는 연출의 장식이다. 매니저가 없거나 id 가 비어도 타이밍은 SO 데이터대로 그대로 흘러간다.
        private void PlayBgm()
        {
            if (SoundManager.Instance == null || string.IsNullOrEmpty(sequence.BgmId))
            {
                return;
            }

            SoundManager.Instance.PlayBgm(sequence.BgmId, sequence.BgmFadeInSeconds);
        }

        private void PlaySfx(string sfxId)
        {
            if (SoundManager.Instance == null || string.IsNullOrEmpty(sfxId))
            {
                return;
            }

            SoundManager.Instance.PlaySfx(sfxId);
        }
    }
}
