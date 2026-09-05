using System;
using System.Collections.Generic;
using Border.Audio;
using Border.Events;
using Border.Research;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Border.UI
{
    [DisallowMultipleComponent]
    public sealed class NewspaperReveal : MonoBehaviour
    {
        [SerializeField] private GameObject view;
        [SerializeField] private RectTransform paper;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private CanvasGroup backdrop;
        [SerializeField] private Image newspaperImage;
        [SerializeField] private LaunchResultMedium medium;
        [FormerlySerializedAs("presentationSprite")]
        [SerializeField] private Sprite newspaperSprite;
        [SerializeField] private TMP_Text headlineText;
        [SerializeField] private TMP_Text editionText;
        [SerializeField] private TMP_Text articleText;
        [SerializeField] private TMP_Text effectsText;
        [SerializeField] private RectTransform effectsBackground;
        [SerializeField] private RawImage photoImage;
        [SerializeField] private TMP_Text photoFallbackText;
        [SerializeField] private VoidEventChannelSO showEvent;
        [SerializeField, Min(0.01f)] private float flyDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float settleDuration = 0.22f;
        [SerializeField] private float spinDegrees = 1080f;
        [SerializeField, Range(0.01f, 1f)] private float startScale = 0.06f;
        [SerializeField] private Vector2 startOffset = new Vector2(-300f, -180f);
        [SerializeField, Range(1f, 1.3f)] private float overshootScale = 1.08f;
        [Header("Article Reveal")]
        [SerializeField, Min(0f)] private float headlineCharacterSeconds = 0.055f;
        [SerializeField, Min(0f)] private float articleCharacterSeconds = 0.012f;
        [SerializeField, Min(0f)] private float resultLineSeconds = 0.3f;
        [SerializeField, Min(0f)] private float sectionPauseSeconds = 0.15f;
        [SerializeField] private UnityEvent onImpact = new UnityEvent();
        [SerializeField] private UnityEvent onShown = new UnityEvent();
        [SerializeField] private UnityEvent onHidden = new UnityEvent();

        private Sequence sequence;
        private Vector2 restPosition;
        private Vector3 restScale;
        private Quaternion restRotation;
        private bool cached;
        private Action closeCallback;
        private bool closeCallbackArmed;
        private Sprite activeSprite;
        private SoundHandle flySound;
        private SoundHandle impactSound;
        private readonly List<SoundHandle> typingSounds = new();
        private int typingSoundIndex;
        private int lastTypingSoundFrame = -1;
        private static readonly string[] TypingSoundIds =
            { "keyboard01", "keyboard02", "keyboard03", "keyboard04" };

        public bool IsShowing => view != null && view.activeSelf;
        public bool IsAnimating => sequence != null && sequence.IsActive();
        public UnityEvent OnImpact => onImpact;
        public UnityEvent OnShown => onShown;
        public UnityEvent OnHidden => onHidden;

        public void SetSprite(Sprite sprite)
        {
            newspaperSprite = sprite;
            activeSprite = null;
            ApplySprite();
        }

        public void ShowSprite(Sprite sprite)
        {
            activeSprite = sprite;
            ApplySprite();
            ShowInternal(clearCloseCallback: true);
        }

        public void Present(LaunchNewspaperArticle article, Texture photo, Action onClosed)
        {
            if (article.Medium != medium)
            {
                Debug.LogError($"{name} is configured for {medium} but received {article.Medium}.", this);
                return;
            }

            activeSprite = newspaperSprite;
            ApplyArticle(article, photo);
            closeCallback = onClosed;
            closeCallbackArmed = onClosed != null;
            ShowInternal(clearCloseCallback: false);
        }

        private void OnValidate() => ApplySprite();

        private void ApplySprite()
        {
            if (newspaperImage == null) return;
            Sprite sprite = activeSprite != null ? activeSprite : newspaperSprite;
            if (sprite != null) newspaperImage.sprite = sprite;
            newspaperImage.preserveAspect = true;
        }

        private void Awake() => ResetView();
        private void OnEnable()
        {
            if (showEvent != null) showEvent.OnEventRaised += Show;
        }

        private void OnDisable()
        {
            if (showEvent != null) showEvent.OnEventRaised -= Show;
            ClearCloseCallback();
            ClearPhoto();
            ResetView();
        }

        [ContextMenu("Preview Reveal (Play Mode)")]
        public void Show()
        {
            activeSprite = newspaperSprite;
            ShowInternal(clearCloseCallback: true);
        }

        private void ShowInternal(bool clearCloseCallback)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !CachePose()) return;
            if (clearCloseCallback) ClearCloseCallback();
            ResetView();
            ApplySprite();
            view.SetActive(true);
            Canvas.ForceUpdateCanvases();
            contentGroup.alpha = 1f;
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = true;
            paper.anchoredPosition = restPosition + startOffset;
            paper.localScale = restScale * startScale;
            float angle = -spinDegrees;
            paper.localRotation = restRotation * Quaternion.Euler(0f, 0f, angle);
            sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.OnStart(() => flySound = PlaySfx("woosh"));
            sequence.Append(DOTween.To(() => paper.anchoredPosition,
                value => paper.anchoredPosition = value, restPosition, flyDuration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => paper.localScale,
                value => paper.localScale = value, restScale * overshootScale, flyDuration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => angle, value =>
            {
                angle = value;
                paper.localRotation = restRotation * Quaternion.Euler(0f, 0f, value);
            }, 0f, flyDuration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => backdrop.alpha, value => backdrop.alpha = value,
                1f, Mathf.Min(0.2f, flyDuration)));
            sequence.AppendCallback(() =>
            {
                flySound.Stop();
                impactSound = PlaySfx(medium == LaunchResultMedium.Mail ? "email" : "hammer_collision_sound");
                onImpact.Invoke();
            });
            sequence.Append(DOTween.To(() => paper.localScale, value => paper.localScale = value,
                restScale * 0.97f, settleDuration * 0.35f).SetEase(Ease.OutQuad));
            sequence.Append(DOTween.To(() => paper.localScale, value => paper.localScale = value,
                restScale, settleDuration * 0.65f).SetEase(Ease.OutBack));
            sequence.AppendCallback(() =>
            {
                RestorePose();
                if (editionText != null) editionText.maxVisibleCharacters = int.MaxValue;
            });
            AppendTypewriter(headlineText, headlineCharacterSeconds);
            sequence.AppendInterval(Mathf.Max(0f, sectionPauseSeconds));
            sequence.AppendCallback(RevealPhoto);
            AppendTypewriter(articleText, articleCharacterSeconds);
            sequence.AppendInterval(Mathf.Max(0f, sectionPauseSeconds));
            AppendResultLines();
            sequence.OnComplete(() =>
            {
                sequence = null;
                RestorePose();
                contentGroup.interactable = true;
                onShown.Invoke();
            });
        }

        public void Hide()
        {
            if (!IsShowing || IsAnimating) return;
            sequence?.Kill();
            sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
            sequence.Append(DOTween.To(() => contentGroup.alpha, value => contentGroup.alpha = value,
                0f, Mathf.Min(0.2f, settleDuration)).SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => backdrop.alpha, value => backdrop.alpha = value,
                0f, Mathf.Min(0.2f, settleDuration)).SetEase(Ease.OutQuad));
            sequence.OnComplete(() =>
            {
                sequence = null;
                bool shouldInvoke = closeCallbackArmed;
                Action callback = closeCallback;
                ClearCloseCallback();
                ResetView();
                ClearPhoto();
                onHidden.Invoke();
                if (shouldInvoke) callback?.Invoke();
            });
        }

        private bool CachePose()
        {
            if (paper == null || view == null || contentGroup == null || backdrop == null) return false;
            if (cached) return true;
            restPosition = paper.anchoredPosition;
            restScale = paper.localScale;
            restRotation = paper.localRotation;
            cached = true;
            return true;
        }

        private void RestorePose()
        {
            paper.anchoredPosition = restPosition;
            paper.localScale = restScale;
            paper.localRotation = restRotation;
        }

        private void ResetView()
        {
            sequence?.Kill();
            sequence = null;
            StopRevealSounds();
            HideArticle();
            if (!CachePose()) return;
            RestorePose();
            contentGroup.alpha = 1f;
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
            backdrop.alpha = 0f;
            view.SetActive(false);
        }

        private void HideArticle()
        {
            if (headlineText != null) headlineText.maxVisibleCharacters = 0;
            if (editionText != null) editionText.maxVisibleCharacters = 0;
            if (articleText != null) articleText.maxVisibleCharacters = 0;
            if (effectsText != null) effectsText.maxVisibleCharacters = 0;
            if (photoImage != null) photoImage.gameObject.SetActive(false);
            if (photoFallbackText != null) photoFallbackText.gameObject.SetActive(false);
        }

        private void RevealPhoto()
        {
            bool hasPhoto = photoImage != null && photoImage.texture != null;
            if (photoImage != null) photoImage.gameObject.SetActive(hasPhoto);
            if (photoFallbackText != null) photoFallbackText.gameObject.SetActive(!hasPhoto);
        }

        private void AppendTypewriter(TMP_Text text, float secondsPerCharacter)
        {
            if (text == null) return;
            text.ForceMeshUpdate();
            int count = text.textInfo.characterCount;
            if (count > 0 && secondsPerCharacter > 0f)
                sequence.Append(DOTween.To(() => text.maxVisibleCharacters,
                    value => RevealCharacters(text, value), count, count * secondsPerCharacter).SetEase(Ease.Linear));
            sequence.AppendCallback(() =>
            {
                RevealCharacters(text, count);
                text.maxVisibleCharacters = int.MaxValue;
            });
        }

        private void AppendResultLines()
        {
            if (effectsText == null) return;
            effectsText.ForceMeshUpdate();
            int count = effectsText.textInfo.characterCount;
            // Reveal each result as a unit, including any visual wraps inside that result.
            for (int i = 0; i < count; i++)
            {
                if (effectsText.textInfo.characterInfo[i].character != '\n' && i != count - 1) continue;
                int end = i + 1;
                sequence.AppendCallback(() => RevealCharacters(effectsText, end));
                sequence.AppendInterval(Mathf.Max(0f, resultLineSeconds));
            }
            sequence.AppendCallback(() => effectsText.maxVisibleCharacters = int.MaxValue);
        }

        private void RevealCharacters(TMP_Text text, int visible)
        {
            int previous = text.maxVisibleCharacters;
            text.maxVisibleCharacters = visible;
            if (visible <= previous || lastTypingSoundFrame == Time.frameCount) return;

            // TMP character data excludes rich-text tags. A slow frame or a whole result line plays one key.
            int end = Mathf.Min(visible, text.textInfo.characterCount);
            for (int i = Mathf.Max(0, previous); i < end; i++)
            {
                if (char.IsWhiteSpace(text.textInfo.characterInfo[i].character)) continue;
                SoundHandle sound = PlaySfx(TypingSoundIds[typingSoundIndex]);
                if (!sound.IsValid) return;
                typingSounds.RemoveAll(handle => !handle.IsValid);
                typingSounds.Add(sound);
                typingSoundIndex = (typingSoundIndex + 1) % TypingSoundIds.Length;
                lastTypingSoundFrame = Time.frameCount;
                break;
            }
        }

        private static SoundHandle PlaySfx(string id) => SoundManager.Instance != null
            ? SoundManager.Instance.PlaySfx(id)
            : SoundHandle.Invalid;

        private void StopRevealSounds()
        {
            flySound.Stop();
            impactSound.Stop();
            foreach (SoundHandle sound in typingSounds) sound.Stop();
            typingSounds.Clear();
            typingSoundIndex = 0;
            lastTypingSoundFrame = -1;
        }

        private void ApplyArticle(LaunchNewspaperArticle article, Texture photo)
        {
            if (headlineText != null)
            {
                headlineText.text = article.Medium == LaunchResultMedium.Mail
                    ? article.Heading
                    : article.Heading?.Replace(", ", ",\n");
            }

            if (editionText != null) editionText.text = article.Edition;
            if (articleText != null) articleText.text = article.Body;
            if (effectsText != null) effectsText.text = article.Effects;
            if (headlineText != null) headlineText.maxVisibleCharacters = int.MaxValue;
            if (editionText != null) editionText.maxVisibleCharacters = int.MaxValue;
            if (articleText != null) articleText.maxVisibleCharacters = int.MaxValue;
            if (effectsText != null) effectsText.maxVisibleCharacters = int.MaxValue;

            if (photoImage != null)
            {
                photoImage.texture = photo;
                photoImage.gameObject.SetActive(photo != null);
            }

            if (photoFallbackText != null)
            {
                photoFallbackText.gameObject.SetActive(photo == null);
            }
        }

        private void ClearCloseCallback()
        {
            closeCallback = null;
            closeCallbackArmed = false;
        }

        private void ClearPhoto()
        {
            if (photoImage != null)
            {
                photoImage.texture = null;
                photoImage.gameObject.SetActive(false);
            }

            if (photoFallbackText != null)
            {
                photoFallbackText.gameObject.SetActive(true);
            }
        }

    }
}
