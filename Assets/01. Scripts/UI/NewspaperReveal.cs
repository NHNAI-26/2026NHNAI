using System;
using Border.Events;
using Border.Research;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] private Sprite newspaperSprite;
        [SerializeField] private Sprite mailSprite;
        [SerializeField] private Sprite fallbackSprite;
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
        private LaunchResultMedium activeMedium = LaunchResultMedium.Newspaper;
        private bool layoutCached;
        private RectLayout headlineLayout;
        private RectLayout editionLayout;
        private RectLayout articleLayout;
        private RectLayout effectsLayout;
        private RectLayout effectsBackgroundLayout;
        private RectLayout photoLayout;
        private RectLayout photoFallbackLayout;
        private TextLayout headlineTextLayout;
        private TextLayout editionTextLayout;
        private TextLayout articleTextLayout;
        private TextLayout effectsTextLayout;
        private Material photoMaterial;

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
            activeMedium = LaunchResultMedium.Newspaper;
            ApplySprite();
            Show();
        }

        public void Present(LaunchNewspaperArticle article, Texture photo, Action onClosed)
        {
            activeMedium = article.Medium;
            activeSprite = ResolveSprite(article.Medium);
            ApplyPresentationLayout(article.Medium);
            ApplyArticle(article, photo);
            closeCallback = onClosed;
            closeCallbackArmed = onClosed != null;
            ShowInternal(clearCloseCallback: false);
        }

        private void OnValidate() => ApplySprite();

        private void ApplySprite()
        {
            if (newspaperImage == null) return;
            newspaperImage.sprite = activeSprite != null ? activeSprite : ResolveSprite(LaunchResultMedium.Newspaper);
            newspaperImage.preserveAspect = true;
        }

        private Sprite ResolveSprite(LaunchResultMedium medium)
        {
            if (medium == LaunchResultMedium.Mail && mailSprite != null) return mailSprite;
            if (newspaperSprite != null) return newspaperSprite;
            return fallbackSprite;
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
            activeMedium = LaunchResultMedium.Newspaper;
            ShowInternal(clearCloseCallback: true);
        }

        private void ShowInternal(bool clearCloseCallback)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !CachePose()) return;
            if (clearCloseCallback) ClearCloseCallback();
            ResetView();
            ApplyPresentationLayout(activeMedium);
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
            sequence.AppendCallback(() => onImpact.Invoke());
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
                    value => text.maxVisibleCharacters = value, count, count * secondsPerCharacter).SetEase(Ease.Linear));
            sequence.AppendCallback(() => text.maxVisibleCharacters = int.MaxValue);
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
                sequence.AppendCallback(() => effectsText.maxVisibleCharacters = end);
                sequence.AppendInterval(Mathf.Max(0f, resultLineSeconds));
            }
            sequence.AppendCallback(() => effectsText.maxVisibleCharacters = int.MaxValue);
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

        private void ApplyPresentationLayout(LaunchResultMedium medium)
        {
            CacheLayout();
            if (medium == LaunchResultMedium.Mail)
            {
                ApplyMailLayout();
                return;
            }

            RestoreNewspaperLayout();
        }

        private void CacheLayout()
        {
            if (layoutCached) return;

            headlineLayout = CaptureRect(headlineText != null ? headlineText.rectTransform : null);
            editionLayout = CaptureRect(editionText != null ? editionText.rectTransform : null);
            articleLayout = CaptureRect(articleText != null ? articleText.rectTransform : null);
            effectsLayout = CaptureRect(effectsText != null ? effectsText.rectTransform : null);
            effectsBackgroundLayout = CaptureRect(effectsBackground);
            photoLayout = CaptureRect(photoImage != null ? photoImage.rectTransform : null);
            photoFallbackLayout = CaptureRect(photoFallbackText != null ? photoFallbackText.rectTransform : null);
            headlineTextLayout = CaptureText(headlineText);
            editionTextLayout = CaptureText(editionText);
            articleTextLayout = CaptureText(articleText);
            effectsTextLayout = CaptureText(effectsText);
            photoMaterial = photoImage != null ? photoImage.material : null;
            layoutCached = true;
        }

        private void ApplyMailLayout()
        {
            SetRect(headlineText != null ? headlineText.rectTransform : null,
                new Vector2(0.36f, 0.715f), new Vector2(0.93f, 0.79f));
            SetRect(editionText != null ? editionText.rectTransform : null,
                new Vector2(0.36f, 0.665f), new Vector2(0.93f, 0.705f));
            SetRect(articleText != null ? articleText.rectTransform : null,
                new Vector2(0.36f, 0.445f), new Vector2(0.93f, 0.645f));
            SetRect(photoImage != null ? photoImage.rectTransform : null,
                new Vector2(0.37f, 0.285f), new Vector2(0.59f, 0.425f));
            SetRect(photoFallbackText != null ? photoFallbackText.rectTransform : null,
                new Vector2(0.37f, 0.285f), new Vector2(0.59f, 0.425f));
            SetRect(effectsBackground, new Vector2(0.35f, 0.13f), new Vector2(0.94f, 0.27f));
            SetRect(effectsText != null ? effectsText.rectTransform : null,
                new Vector2(0.37f, 0.15f), new Vector2(0.92f, 0.25f));

            SetText(headlineText, 16f, 13f, 16f, TextAlignmentOptions.Left);
            SetText(editionText, 11f, 9f, 11f, TextAlignmentOptions.Left);
            SetText(articleText, 12f, 10f, 12f, TextAlignmentOptions.TopLeft);
            SetText(effectsText, 11f, 9f, 11f, TextAlignmentOptions.TopLeft);

            if (photoImage != null) photoImage.material = null;
        }

        private void RestoreNewspaperLayout()
        {
            ApplyRect(headlineText != null ? headlineText.rectTransform : null, headlineLayout);
            ApplyRect(editionText != null ? editionText.rectTransform : null, editionLayout);
            ApplyRect(articleText != null ? articleText.rectTransform : null, articleLayout);
            ApplyRect(effectsText != null ? effectsText.rectTransform : null, effectsLayout);
            ApplyRect(effectsBackground, effectsBackgroundLayout);
            ApplyRect(photoImage != null ? photoImage.rectTransform : null, photoLayout);
            ApplyRect(photoFallbackText != null ? photoFallbackText.rectTransform : null, photoFallbackLayout);
            ApplyText(headlineText, headlineTextLayout);
            ApplyText(editionText, editionTextLayout);
            ApplyText(articleText, articleTextLayout);
            ApplyText(effectsText, effectsTextLayout);

            if (photoImage != null) photoImage.material = photoMaterial;
        }

        private static RectLayout CaptureRect(RectTransform rect)
        {
            if (rect == null) return default;

            return new RectLayout
            {
                AnchorMin = rect.anchorMin,
                AnchorMax = rect.anchorMax,
                AnchoredPosition = rect.anchoredPosition,
                SizeDelta = rect.sizeDelta,
                Pivot = rect.pivot
            };
        }

        private static void ApplyRect(RectTransform rect, RectLayout layout)
        {
            if (rect == null) return;

            rect.anchorMin = layout.AnchorMin;
            rect.anchorMax = layout.AnchorMax;
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.SizeDelta;
            rect.pivot = layout.Pivot;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null) return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static TextLayout CaptureText(TMP_Text text)
        {
            if (text == null) return default;

            return new TextLayout
            {
                FontSize = text.fontSize,
                FontSizeMin = text.fontSizeMin,
                FontSizeMax = text.fontSizeMax,
                Alignment = text.alignment
            };
        }

        private static void ApplyText(TMP_Text text, TextLayout layout)
        {
            if (text == null) return;

            text.fontSize = layout.FontSize;
            text.fontSizeMin = layout.FontSizeMin;
            text.fontSizeMax = layout.FontSizeMax;
            text.alignment = layout.Alignment;
        }

        private static void SetText(
            TMP_Text text,
            float fontSize,
            float fontSizeMin,
            float fontSizeMax,
            TextAlignmentOptions alignment)
        {
            if (text == null) return;

            text.fontSize = fontSize;
            text.fontSizeMin = fontSizeMin;
            text.fontSizeMax = fontSizeMax;
            text.alignment = alignment;
        }

        private struct RectLayout
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector2 Pivot;
        }

        private struct TextLayout
        {
            public float FontSize;
            public float FontSizeMin;
            public float FontSizeMax;
            public TextAlignmentOptions Alignment;
        }
    }
}
