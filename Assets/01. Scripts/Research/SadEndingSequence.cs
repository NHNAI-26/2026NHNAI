using System.Collections;
using System.Collections.Generic;
using Border.Audio;
using Border.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Border.Research
{
    /// <summary>
    /// 2026 Q4 마감까지 최종 미션(<c>LowPowerZoneHold</c>)을 통과하지 못했을 때 도는 배드엔딩.
    /// 세 비트다 — 실패 신문(기존 UI 가 이미 띄운다) 뒤에서 검은 화면으로 페이드 → 프롤로그와 같은
    /// 타이핑 대사 → 페이드 후 타이틀. 근거와 결정 이력은 <c>docs/sad-ending-cinematic.md</c>.
    ///
    /// 해피엔딩(<c>Simulation.HappyEndingSequence</c>)과 같은 문법으로 짰다
    /// (검은 오버레이 + 클릭 스킵 + 타이틀 퇴장). 무대는 없다 — 검은 화면과 글자뿐이라
    /// 씬도 프리팹도 건드리지 않는다. 3D 무대가 필요 없으니 `Simulation` 어셈블리에 둘 이유도 없고,
    /// 여기 있어야 `ResearchOperationUIController` 가 직접 부를 수 있다(의존은 Simulation → Border 한 방향뿐).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SadEndingSequence : MonoBehaviour, IPointerClickHandler
    {
        public const string TitleSceneName = "00_Title";

        private static readonly string[] DefaultLines =
        {
            "2026.12",
            "예산은 회수되었다.",
            "연구소의 불은 꺼졌고, 아무도 다시 켜지 않았다.",
            "달은 여전히 그 자리에 있었다.",
            "우리가 닿지 못했을 뿐이다."
        };

        // ponytail: 타이프라이터 세 번째 사본(PrologueController, NewspaperReveal 에 이어).
        // 네 번째 호출자가 생기면 그때 공용 헬퍼로 추출한다 — 앞의 둘은 PlayMode 테스트로 잠겨 있다.
        private static readonly string[] TypingSoundIds =
            { "keyboard01", "keyboard02", "keyboard03", "keyboard04" };

        [SerializeField, TextArea(1, 3)] private string[] lines = DefaultLines;

        [Header("타이밍 (총 20~30초 예산)")]
        [SerializeField, Min(0f)] private float enterFadeSeconds = 1.2f;
        [SerializeField, Min(0f)] private float typeSecondsPerChar = 0.06f;
        [SerializeField, Min(0f)] private float lineHoldSeconds = 2f;
        [SerializeField, Min(0f)] private float lineFadeOutSeconds = 0.8f;
        [SerializeField, Min(0f)] private float finalFadeSeconds = 1.5f;

        [Tooltip("신문 비트에서 결과 보고가 닫히기를 기다리는 최대 시간. 넘기면 경고를 남기고 대사로 넘어간다.")]
        [SerializeField, Min(1f)] private float newspaperTimeoutSeconds = 180f;

        private CanvasGroup overlay;
        private TMP_Text lineText;
        private Coroutine routine;
        private bool advanceRequested;
        private bool leaving;
        private readonly List<SoundHandle> typingSounds = new();
        private int typingSoundIndex;

        private ResearchOperationUIController research;
        private ResearchLaunchResultData newspaperResult;
        private bool showsNewspaper;
        private bool newspaperDismissed;

        /// <summary>
        /// 신문이 이미 나온 뒤 이어서 재생한다. 실제 게임 경로가 이쪽이다 — 실패 신문은
        /// <see cref="ResearchFlowSession.QueueDeadlineFailureReportIfNeeded"/> 가 이미 띄웠다.
        /// 플레이 모드가 아니면 <c>null</c> 을 돌려주므로 호출부가 기존 엔딩 패널로 떨어질 수 있다 —
        /// EditMode 테스트는 코루틴을 돌리지 못한다.
        /// </summary>
        public static SadEndingSequence Play() => Play(null, default, false);

        /// <summary>
        /// 실패 신문부터 띄우고 이어서 대사로 넘어간다. 앞선 결과 보고가 없는 자리 — 디버그 재생이 이쪽이다.
        /// </summary>
        public static SadEndingSequence Play(ResearchOperationUIController research, ResearchLaunchResultData result)
            => Play(research, result, true);

        private static SadEndingSequence Play(
            ResearchOperationUIController research,
            ResearchLaunchResultData result,
            bool showNewspaper)
        {
            if (!Application.isPlaying) return null;

            var host = new GameObject("Sad Ending").AddComponent<SadEndingSequence>();
            host.research = research;
            host.newspaperResult = result;
            host.showsNewspaper = showNewspaper && research != null;
            host.routine = host.StartCoroutine(host.PlayRoutine());
            return host;
        }

        /// <summary>
        /// 배경 Image 의 클릭이 부모인 이 컴포넌트까지 버블링돼 들어온다.
        /// 한 클릭은 <b>현재 대사 한 줄</b>만 앞당긴다 — 연출 전체를 건너뛰지 않는다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData) => advanceRequested = true;

        private IEnumerator PlayRoutine()
        {
            BuildOverlay();

            // B0 — 실패 신문. 실제 경로에서는 이미 나온 뒤라 건너뛴다.
            if (showsNewspaper) yield return NewspaperRoutine();

            SoundManager.Instance?.StopBgm(enterFadeSeconds);

            // B1 — 신문이 걷힌 화면을 검게 덮는다. 오버레이가 alpha 0 으로 서 있어서 컷이 아니라 페이드다.
            yield return FadeOverlay(1f, enterFadeSeconds);

            // B2 — 타이핑 대사
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    yield return ShowLine(line);
                }
            }

            // B3 — 여백을 두고 타이틀로. 이미 검은 화면이라 페이드할 것은 대사가 사라진 뒤의 정적뿐이다.
            yield return WaitOrAdvance(finalFadeSeconds);
            LeaveToTitle();
        }

        /// <summary>
        /// 기존 결과 보고 UI 로 실패 신문을 띄우고 플레이어가 닫을 때까지 기다린다. 연출을 다시 만들지 않는다.
        /// 닫힘은 <c>afterReports</c> 콜백으로 받는다 — <c>ShowResultReport</c> 는 게임 종료 여부와 무관하게
        /// 이 콜백을 부르므로, 해피엔딩이 쓰는 <c>SetEndingOverride</c> 와 달리 게임이 안 끝난 디버그 재생에서도 걸린다.
        /// </summary>
        private IEnumerator NewspaperRoutine()
        {
            newspaperDismissed = false;

            // 오버레이는 알파 0 이어도 레이캐스트를 먹는다. 걷어야 클릭이 신문까지 닿는다.
            overlay.blocksRaycasts = false;

            if (research.gameObject.activeSelf)
            {
                research.ShowLaunchResultOverlay(newspaperResult, () => newspaperDismissed = true);
            }
            else
            {
                // OnEnable 의 Refresh 가 미확인 결과를 보고 스스로 신문을 띄운다.
                research.gameObject.SetActive(true);
                research.ShowLaunchResultOverlay(newspaperResult, () => newspaperDismissed = true);
            }

            // 보고서가 이미 떠 있으면 ShowResultReport 가 조용히 되돌아가 콜백이 오지 않는다.
            // 검은 화면에 영원히 갇히느니 경고를 남기고 대사로 넘어간다.
            float waited = 0f;
            while (!newspaperDismissed && waited < newspaperTimeoutSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!newspaperDismissed) Log.W("[SadEnding] 실패 신문이 닫히지 않아 대사로 넘어간다.", this);

            overlay.blocksRaycasts = true;
            advanceRequested = false; // 신문을 닫은 클릭이 첫 대사로 새지 않게 한다.
        }

        /// <summary>
        /// 한 줄을 찍고, 머물고, 지운다. 각 구간 앞에서 클릭 플래그를 비우므로 클릭 한 번은 그 구간
        /// 하나만 끝낸다 — 타이핑 중 클릭은 남은 글자를 즉시 드러낼 뿐 다음 줄로 넘어가지 않는다.
        /// </summary>
        private IEnumerator ShowLine(string line)
        {
            advanceRequested = false;
            lineText.text = line;
            lineText.alpha = 1f;
            yield return TypeText(line);

            advanceRequested = false;
            yield return WaitOrAdvance(lineHoldSeconds);

            advanceRequested = false;
            yield return FadeText(1f, 0f, lineFadeOutSeconds);
        }

        /// <summary>
        /// 글자를 하나씩 드러낸다. 알파가 아니라 <see cref="TMP_Text.maxVisibleCharacters"/> 만 올리므로
        /// 레이아웃이 처음부터 확정돼 줄이 늘어날 때 텍스트가 위아래로 튀지 않는다.
        /// </summary>
        private IEnumerator TypeText(string line)
        {
            lineText.maxVisibleCharacters = 0;

            if (typeSecondsPerChar <= 0f)
            {
                lineText.maxVisibleCharacters = int.MaxValue;
                yield break;
            }

            // 리치 텍스트를 쓰지 않으므로 파싱된 글자 수와 문자열 길이가 같다.
            int total = line.Length;
            float elapsed = 0f;

            while (lineText.maxVisibleCharacters < total && !advanceRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                int previous = lineText.maxVisibleCharacters;
                int visible = Mathf.Min(total, Mathf.FloorToInt(elapsed / typeSecondsPerChar));
                lineText.maxVisibleCharacters = visible;
                PlayTypingSound(line, previous, visible);
                yield return null;
            }

            lineText.maxVisibleCharacters = total;
            StopTypingSound();
        }

        private void PlayTypingSound(string text, int previous, int visible)
        {
            if (visible <= previous) return;
            typingSounds.RemoveAll(handle => !handle.IsValid);
            // 프레임이 느리면 여러 글자가 한 번에 드러난다. 그래도 키 소리는 그 프레임에 한 번만 낸다.
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

        private void LeaveToTitle()
        {
            if (leaving) return;
            leaving = true;
            routine = null;
            StopTypingSound();

            // 세션 초기화는 하지 않는다. TitleMenu.NewGame 이 PrepareNewGame 으로 이미 처리한다.
            SceneManager.LoadScene(TitleSceneName);
        }

        // ── 오버레이 ────────────────────────────────────────────────────────────

        private void BuildOverlay()
        {
            var canvasObject = new GameObject("Sad Ending Overlay", typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 해피엔딩 오버레이와 같은 층. CRT 커튼(short.MaxValue) 바로 아래다.
            canvas.sortingOrder = short.MaxValue - 1;

            overlay = canvasObject.AddComponent<CanvasGroup>();
            overlay.alpha = 0f; // 신문이 걷힌 화면 위에서 페이드로 들어간다 — 컷이 아니다.
            overlay.blocksRaycasts = true;

            var backdrop = new GameObject("Backdrop", typeof(Image)).GetComponent<Image>();
            backdrop.color = Color.black;
            backdrop.raycastTarget = true; // alpha 0 이어도 클릭을 받는다 — 스킵이 처음부터 살아 있어야 한다.
            Stretch((RectTransform)backdrop.transform, canvasObject.transform);

            lineText = new GameObject("Line", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            lineText.alignment = TextAlignmentOptions.Center;
            lineText.fontSize = 48f;
            lineText.color = Color.white;
            lineText.alpha = 0f;
            lineText.raycastTarget = false;
            Stretch((RectTransform)lineText.transform, canvasObject.transform);
        }

        private static void Stretch(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private IEnumerator FadeText(float from, float to, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds && !advanceRequested; elapsed += Time.unscaledDeltaTime)
            {
                lineText.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }

            lineText.alpha = to;
        }

        /// <summary>검은 화면으로 들어가는 페이드는 클릭으로 끊지 않는다 — 끊어도 화면은 검어야 한다.</summary>
        private IEnumerator FadeOverlay(float to, float seconds)
        {
            float from = overlay.alpha;
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                overlay.alpha = Mathf.Lerp(from, to, elapsed / seconds);
                yield return null;
            }

            overlay.alpha = to;
        }

        /// <summary>대기 중에도 클릭이 먹어야 하므로 <see cref="WaitForSecondsRealtime"/> 대신 직접 센다.</summary>
        private IEnumerator WaitOrAdvance(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds && !advanceRequested; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        private void OnDisable()
        {
            StopTypingSound();
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }
    }
}
