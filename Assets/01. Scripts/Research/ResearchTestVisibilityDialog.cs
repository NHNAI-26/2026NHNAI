using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class ResearchTestVisibilityDialog : MonoBehaviour
    {
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
            model = currentModel;
            confirm = onConfirm;
            publicToggle.SetIsOnWithoutNotify(false);
            privateToggle.SetIsOnWithoutNotify(true);
            LaunchMissionConfig mission = model.GetConfiguredMissionConfig(missionId);
            missionText.text = $"{mission.DisplayName} / 설계 진입 비용 {mission.LaunchCost}";
            publicDetails.text = Describe(TestVisibility.Public);
            privateDetails.text = Describe(TestVisibility.Private);
            errorText.text = string.Empty;
            confirmButton.interactable = true;
            confirming = false;
            IsOpen = true;
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Hide);
            gameObject.SetActive(true);
        }

        private static string Describe(TestVisibility visibility)
        {
            switch (visibility)
            {
                case TestVisibility.Public:
                    return "큰 변동 이벤트\n성공 시 대형 후원 가능\n\n실패 시 여론 역풍과 시설 손실 위험";
                case TestVisibility.Private:
                    return "안정적 학습 이벤트\n성공 시 작은 지원과 엔진 개선\n\n실패해도 손실보다 회수와 분석 중심";
                default:
                    return "최종 검증";
            }
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
            IsOpen = false;
            confirming = false;
            confirm = null;
            model = null;
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Hide);
        }
    }
}
