using System;
using DG.Tweening;
using UnityEngine;

namespace Border.Research
{
    [DisallowMultipleComponent]
    public sealed class ResearchOperationTransitionAnimator : MonoBehaviour
    {
        private const string TopPanelName = "TopInfoBar";
        private const string BottomPanelName = "HubActionBar";
        private const string LeftPanelName = "EnginePresetColumn";
        private const string RightPanelName = "DetailColumn";

        /// <summary>Which panels one transition moves — the hub row and the two columns never travel together.</summary>
        [Flags]
        public enum PanelGroup
        {
            Top = 1,
            Bottom = 2,
            Left = 4,
            Right = 8,
            Hub = Top | Bottom,
            Columns = Left | Right,
            All = Hub | Columns
        }

        [SerializeField] private RectTransform topPanel;
        [SerializeField] private RectTransform bottomPanel;
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private RectTransform rightPanel;
        [SerializeField, Min(0f)] private float enterDuration = 0.55f;
        [SerializeField, Min(0f)] private float exitDuration = 0.4f;
        [SerializeField, Min(0f)] private float offscreenPadding = 24f;

        private PanelMotion topMotion;
        private PanelMotion bottomMotion;
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
            bottomPanel ??= FindChildRectTransform(root, BottomPanelName);
            leftPanel ??= FindChildRectTransform(root, LeftPanelName);
            rightPanel ??= FindChildRectTransform(root, RightPanelName);
            CacheFinalPositions();
        }

        public Sequence PlayEnter(PanelGroup group, Action onComplete = null)
        {
            CacheFinalPositions();
            FinishActiveSequence();
            ForEach(group, motion => SetPanelState(motion, motion.StartPosition, 0f, false));
            activeSequence = CreateSequence(group, enterDuration, Ease.OutCubic, useStartPosition: false, targetAlpha: 1f, blocksRaycasts: true, onComplete);
            return activeSequence;
        }

        public Sequence PlayExit(PanelGroup group, Action onComplete = null)
        {
            CacheFinalPositions();
            FinishActiveSequence();
            activeSequence = CreateSequence(group, exitDuration, Ease.InCubic, useStartPosition: true, targetAlpha: 0f, blocksRaycasts: false, onComplete);
            return activeSequence;
        }

        public void ResetToFinalPositions()
        {
            CacheFinalPositions();
            KillActiveSequence();
            ForEach(PanelGroup.All, motion => SetPanelState(motion, motion.FinalPosition, 1f, true));
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
            FinishActiveSequence();
        }

        private Sequence CreateSequence(
            PanelGroup group,
            float duration,
            Ease ease,
            bool useStartPosition,
            float targetAlpha,
            bool blocksRaycasts,
            Action onComplete)
        {
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            ForEach(group, motion => AddPanelTween(sequence, motion, duration, ease, useStartPosition, targetAlpha, blocksRaycasts));
            // DOTween replaces the completion callback instead of chaining it, so the caller's callback has
            // to be folded in here — a second OnComplete on the returned sequence would drop the raycast restore.
            sequence.OnComplete(() =>
            {
                ForEach(group, motion => SetPanelRaycast(motion, blocksRaycasts));
                activeSequence = null;
                onComplete?.Invoke();
            });

            return sequence;
        }

        private void ForEach(PanelGroup group, Action<PanelMotion> action)
        {
            if ((group & PanelGroup.Top) != 0) action(topMotion);
            if ((group & PanelGroup.Bottom) != 0) action(bottomMotion);
            if ((group & PanelGroup.Left) != 0) action(leftMotion);
            if ((group & PanelGroup.Right) != 0) action(rightMotion);
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
            bottomMotion = CreatePanelMotion(bottomPanel, Direction.Bottom);
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
            // The resting offset counts as travel: HubActionBar rests 40px above its bottom anchor, so a
            // plain height+padding slide would leave a strip of it on screen.
            Vector2 offset = direction switch
            {
                Direction.Top => Vector2.up * (Mathf.Abs(size.y) + offscreenPadding + Mathf.Abs(finalPosition.y)),
                Direction.Bottom => Vector2.down * (Mathf.Abs(size.y) + offscreenPadding + Mathf.Abs(finalPosition.y)),
                Direction.Left => Vector2.left * (Mathf.Abs(size.x) + offscreenPadding + Mathf.Abs(finalPosition.x)),
                Direction.Right => Vector2.right * (Mathf.Abs(size.x) + offscreenPadding + Mathf.Abs(finalPosition.x)),
                _ => Vector2.zero
            };

            return new PanelMotion(rectTransform, group, finalPosition, finalPosition + offset);
        }

        /// <summary>
        /// Snaps a still-running transition to its end before starting the next one. Killing it instead would
        /// strand every panel the new group does not touch — open the part development panel while the hub is
        /// still sliding in and TopInfoBar would sit half-transparent off-screen with nothing to finish it.
        /// </summary>
        private void FinishActiveSequence()
        {
            if (activeSequence == null)
            {
                return;
            }

            Sequence previous = activeSequence;
            activeSequence = null;
            previous.Complete();
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
            Bottom,
            Left,
            Right
        }
    }
}
