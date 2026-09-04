using DG.Tweening;
using UnityEngine;

namespace Border.Research
{
    [DisallowMultipleComponent]
    public sealed class ResearchOperationTransitionAnimator : MonoBehaviour
    {
        private const string TopPanelName = "TopInfoBar";
        private const string LeftPanelName = "EnginePresetColumn";
        private const string RightPanelName = "DetailColumn";

        [SerializeField] private RectTransform topPanel;
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private RectTransform rightPanel;
        [SerializeField, Min(0f)] private float enterDuration = 0.55f;
        [SerializeField, Min(0f)] private float exitDuration = 0.4f;
        [SerializeField, Min(0f)] private float offscreenPadding = 24f;

        private PanelMotion topMotion;
        private PanelMotion leftMotion;
        private PanelMotion rightMotion;
        private bool hasCachedPositions;
        private Sequence activeSequence;

        public void Bind(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            topPanel ??= FindChildRectTransform(root, TopPanelName);
            leftPanel ??= FindChildRectTransform(root, LeftPanelName);
            rightPanel ??= FindChildRectTransform(root, RightPanelName);
            CacheFinalPositions();
        }

        public Sequence PlayEnter()
        {
            CacheFinalPositions();
            KillActiveSequence();
            SetPanelState(topMotion, topMotion.StartPosition, 0f, false);
            SetPanelState(leftMotion, leftMotion.StartPosition, 0f, false);
            SetPanelState(rightMotion, rightMotion.StartPosition, 0f, false);
            activeSequence = CreateSequence(enterDuration, Ease.OutCubic, useStartPosition: false, targetAlpha: 1f, blocksRaycasts: true);
            return activeSequence;
        }

        public Sequence PlayExit()
        {
            CacheFinalPositions();
            KillActiveSequence();
            activeSequence = CreateSequence(exitDuration, Ease.InCubic, useStartPosition: true, targetAlpha: 0f, blocksRaycasts: false);
            return activeSequence;
        }

        public void ResetToFinalPositions()
        {
            CacheFinalPositions();
            KillActiveSequence();
            SetPanelState(topMotion, topMotion.FinalPosition, 1f, true);
            SetPanelState(leftMotion, leftMotion.FinalPosition, 1f, true);
            SetPanelState(rightMotion, rightMotion.FinalPosition, 1f, true);
        }

        public void CompleteActiveSequenceForTests()
        {
            if (activeSequence != null)
            {
                activeSequence.Complete();
            }
        }

        private void OnDisable()
        {
            KillActiveSequence();
        }

        private Sequence CreateSequence(float duration, Ease ease, bool useStartPosition, float targetAlpha, bool blocksRaycasts)
        {
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            AddPanelTween(sequence, topMotion, duration, ease, useStartPosition, targetAlpha, blocksRaycasts);
            AddPanelTween(sequence, leftMotion, duration, ease, useStartPosition, targetAlpha, blocksRaycasts);
            AddPanelTween(sequence, rightMotion, duration, ease, useStartPosition, targetAlpha, blocksRaycasts);
            sequence.OnComplete(() =>
            {
                SetPanelRaycast(topMotion, blocksRaycasts);
                SetPanelRaycast(leftMotion, blocksRaycasts);
                SetPanelRaycast(rightMotion, blocksRaycasts);
                activeSequence = null;
            });

            return sequence;
        }

        private static void AddPanelTween(
            Sequence sequence,
            PanelMotion motion,
            float duration,
            Ease ease,
            bool useStartPosition,
            float targetAlpha,
            bool blocksRaycasts)
        {
            if (!motion.IsValid)
            {
                return;
            }

            Vector2 targetPosition = useStartPosition ? motion.StartPosition : motion.FinalPosition;
            motion.Group.blocksRaycasts = blocksRaycasts;
            motion.Group.interactable = blocksRaycasts;

            if (duration <= 0f)
            {
                SetPanelState(motion, targetPosition, targetAlpha, blocksRaycasts);
                return;
            }

            sequence.Join(DOTween.To(
                () => motion.RectTransform.anchoredPosition,
                value => motion.RectTransform.anchoredPosition = value,
                targetPosition,
                duration).SetEase(ease));
            sequence.Join(DOTween.To(
                () => motion.Group.alpha,
                value => motion.Group.alpha = value,
                targetAlpha,
                duration).SetEase(ease));
        }

        private void CacheFinalPositions()
        {
            if (hasCachedPositions)
            {
                return;
            }

            topMotion = CreatePanelMotion(topPanel, Direction.Top);
            leftMotion = CreatePanelMotion(leftPanel, Direction.Left);
            rightMotion = CreatePanelMotion(rightPanel, Direction.Right);
            hasCachedPositions = true;
        }

        private PanelMotion CreatePanelMotion(RectTransform rectTransform, Direction direction)
        {
            if (rectTransform == null)
            {
                return default;
            }

            CanvasGroup group = rectTransform.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = rectTransform.gameObject.AddComponent<CanvasGroup>();
            }

            Vector2 finalPosition = rectTransform.anchoredPosition;
            Vector2 size = rectTransform.rect.size;
            Vector2 offset = direction switch
            {
                Direction.Top => Vector2.up * (Mathf.Abs(size.y) + offscreenPadding),
                Direction.Left => Vector2.left * (Mathf.Abs(size.x) + offscreenPadding),
                Direction.Right => Vector2.right * (Mathf.Abs(size.x) + offscreenPadding),
                _ => Vector2.zero
            };

            return new PanelMotion(rectTransform, group, finalPosition, finalPosition + offset);
        }

        private void KillActiveSequence()
        {
            if (activeSequence == null)
            {
                return;
            }

            activeSequence.Kill();
            activeSequence = null;
        }

        private static void SetPanelState(PanelMotion motion, Vector2 anchoredPosition, float alpha, bool blocksRaycasts)
        {
            if (!motion.IsValid)
            {
                return;
            }

            motion.RectTransform.anchoredPosition = anchoredPosition;
            motion.Group.alpha = alpha;
            SetPanelRaycast(motion, blocksRaycasts);
        }

        private static void SetPanelRaycast(PanelMotion motion, bool blocksRaycasts)
        {
            if (!motion.IsValid)
            {
                return;
            }

            motion.Group.blocksRaycasts = blocksRaycasts;
            motion.Group.interactable = blocksRaycasts;
        }

        private static RectTransform FindChildRectTransform(Transform root, string name)
        {
            foreach (RectTransform rectTransform in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rectTransform.name == name)
                {
                    return rectTransform;
                }
            }

            return null;
        }

        private readonly struct PanelMotion
        {
            public PanelMotion(RectTransform rectTransform, CanvasGroup group, Vector2 finalPosition, Vector2 startPosition)
            {
                RectTransform = rectTransform;
                Group = group;
                FinalPosition = finalPosition;
                StartPosition = startPosition;
            }

            public RectTransform RectTransform { get; }
            public CanvasGroup Group { get; }
            public Vector2 FinalPosition { get; }
            public Vector2 StartPosition { get; }
            public bool IsValid => RectTransform != null && Group != null;
        }

        private enum Direction
        {
            Top,
            Left,
            Right
        }
    }
}
