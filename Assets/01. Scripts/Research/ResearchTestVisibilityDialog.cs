using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchTestVisibilityDialog : MonoBehaviour
    {
        private const float PopInSeconds = 0.22f;
        private const float PopInStartScale = 0.9f;


        [SerializeField] private Toggle publicToggle;
        [SerializeField] private Toggle privateToggle;
        [SerializeField] private TMP_Text missionText;
        [SerializeField] private TMP_Text publicDetails;
        [SerializeField] private TMP_Text privateDetails;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        private Func<TestVisibility, ResearchActionResult> confirm;
        private ResearchPrototypeModel model;
        private bool confirming;
        private CanvasGroup popGroup;
        private Sequence popSequence;
        public bool IsOpen { get; private set; }

        public void Open(ResearchPrototypeModel currentModel, LaunchMissionId missionId, Func<TestVisibility, ResearchActionResult> onConfirm)
        {
            if (IsOpen) return;
            if (publicToggle == null || privateToggle == null || missionText == null || publicDetails == null
                || privateDetails == null || errorText == null || confirmButton == null || cancelButton == null)
            {
                Debug.LogError("Research test visibility dialog prefab has missing bindings.", this);
                return;
            }
            // Activate first, then write all dynamic labels. This prevents the prefab's serialized
            // placeholder from being restored by UI enable callbacks when the dialog is reopened.
            gameObject.SetActive(true);
            model = currentModel;
            confirm = onConfirm;
            bool finalMission = missionId == LaunchMissionId.LowPowerZoneHold;
            publicToggle.SetIsOnWithoutNotify(finalMission);
            privateToggle.SetIsOnWithoutNotify(!finalMission);
            LaunchMissionConfig mission = model.GetConfiguredMissionConfig(missionId);
            missionText.text = $"MISSION {(int)missionId} : {mission.DisplayName}";
            missionText.ForceMeshUpdate();
            publicDetails.text = Describe(TestVisibility.Public, finalMission);
            privateDetails.text = Describe(TestVisibility.Private, finalMission);
            TMP_Text confirmLabel = confirmButton.GetComponentInChildren<TMP_Text>(true);
            if (confirmLabel != null)
                confirmLabel.text = $"설계 진입\n<size=15>-{model.GetDesignEntryCost(missionId)}$ / +1분기</size>";
            errorText.text = string.Empty;
            confirmButton.interactable = true;
            confirming = false;
            IsOpen = true;
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Hide);
            PlayPopIn();
        }

        private void PlayPopIn()
        {
            if (popGroup == null)
            {
                popGroup = GetComponent<CanvasGroup>();
                if (popGroup == null) popGroup = gameObject.AddComponent<CanvasGroup>();
            }

            popSequence?.Kill();
            popSequence = null;

            // Edit mode tests drive this dialog through onClick directly and never tick DOTween, so a
            // tween there would leave it stuck transparent and scaled down.
            if (!Application.isPlaying)
            {
                ResetPopState();
                return;
            }

            popGroup.alpha = 0f;
            transform.localScale = Vector3.one * PopInStartScale;
            popSequence = DOTween.Sequence()
                .SetTarget(this)
                .Join(DOTween.To(() => popGroup.alpha, value => popGroup.alpha = value, 1f, PopInSeconds).SetEase(Ease.OutQuad))
                .Join(transform.DOScale(Vector3.one, PopInSeconds).SetEase(Ease.OutBack))
                .OnComplete(() => popSequence = null);
        }

        private void ResetPopState()
        {
            popSequence?.Kill();
            popSequence = null;
            if (popGroup != null) popGroup.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        private static string Describe(TestVisibility visibility, bool finalMission)
        {
            if (visibility == TestVisibility.Private)
            {
                return finalMission
                    ? "사내에서 비공개로 발사 테스트를 진행합니다.\n\n마지막 미션은 공개 테스트가 필수입니다.\n비공개 테스트로는 미션을 진행할 수 없습니다."
                    : "사내에서 비공개로 발사 테스트를 진행합니다.\n\n성공 보수가 적지만 \n실패 시 위험 부담도 적습니다. ";
            }

            return finalMission
                ? "모두의 앞에서 공개로 발사 테스트를 진행합니다.\n\n마지막 미션은 공개 테스트가 필수입니다.\n이 선택으로만 최종 검증에 진입할 수 있습니다."
                : "모두의 앞에서 공개로 발사 테스트를 진행합니다. \n\n성공 시 투자 혹은 연구비 지원을 \n받을 수 있습니다.\n실패 시 연구비가 줄어들수도..?";
        }

        private void Confirm()
        {
            if (!IsOpen || confirming || confirm == null) return;
            confirming = true;
            confirmButton.interactable = false;
            ResearchActionResult result = confirm(publicToggle.isOn ? TestVisibility.Public : TestVisibility.Private);
            if (result == ResearchActionResult.Success) Hide();
            else
            {
                errorText.text = result switch
                {
                    ResearchActionResult.DeadlineReached => "마감에 도달해 설계에 진입할 수 없습니다.",
                    ResearchActionResult.GameEnded => "이미 종료된 게임입니다.",
                    ResearchActionResult.LaunchInProgress => "발사 진행 중에는 설계에 진입할 수 없습니다.",
                    _ => model.LastMessage
                };
                confirming = false;
                confirmButton.interactable = true;
            }
        }

        public void Hide()
        {
            ClearBindings();
            gameObject.SetActive(false);
        }

        private void OnDisable() => ClearBindings();

        private void ClearBindings()
        {
            ResetPopState();
            IsOpen = false;
            confirming = false;
            confirm = null;
            model = null;
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Hide);
        }
    }
}
