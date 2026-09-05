using Border.Events;
using DG.Tweening;
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
        [SerializeField] private VoidEventChannelSO showEvent;
        [SerializeField, Min(0.01f)] private float flyDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float settleDuration = 0.22f;
        [SerializeField] private float spinDegrees = 1080f;
        [SerializeField, Range(0.01f, 1f)] private float startScale = 0.06f;
        [SerializeField] private Vector2 startOffset = new Vector2(-300f, -180f);
        [SerializeField, Range(1f, 1.3f)] private float overshootScale = 1.08f;
        [SerializeField] private UnityEvent onImpact = new UnityEvent();
        [SerializeField] private UnityEvent onShown = new UnityEvent();
        [SerializeField] private UnityEvent onHidden = new UnityEvent();

        private Sequence sequence;
        private Vector2 restPosition;
        private Vector3 restScale;
        private Quaternion restRotation;
        private bool cached;
        public bool IsShowing => view != null && view.activeSelf;
        public bool IsAnimating => sequence != null && sequence.IsActive();
        public UnityEvent OnImpact => onImpact;
        public UnityEvent OnShown => onShown;
        public UnityEvent OnHidden => onHidden;

        public void SetSprite(Sprite sprite)
        {
            newspaperSprite = sprite;
            ApplySprite();
        }

        public void ShowSprite(Sprite sprite)
        {
            SetSprite(sprite);
            Show();
        }

        private void OnValidate() => ApplySprite();

        private void ApplySprite()
        {
            if (newspaperImage == null) return;
            newspaperImage.sprite = newspaperSprite;
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
            ResetView();
        }

        [ContextMenu("Preview Reveal (Play Mode)")]
        public void Show()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !CachePose()) return;
            ResetView();
            ApplySprite();
            view.SetActive(true);
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
            bool wasShowing = IsShowing;
            ResetView();
            if (wasShowing) onHidden.Invoke();
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
            if (!CachePose()) return;
            RestorePose();
            contentGroup.interactable = false;
            contentGroup.blocksRaycasts = false;
            backdrop.alpha = 0f;
            view.SetActive(false);
        }
    }
}
