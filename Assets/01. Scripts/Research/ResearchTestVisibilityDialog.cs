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
            // 진입 예산은 ConfirmButton 라벨이 이미 보여준다 — 여기서 빼지 않으면 같은 값이 두 군데 남는다.
            missionText.text = $"MISSION {(int)missionId} : {mission.DisplayName}";
            publicDetails.text = Describe(TestVisibility.Public);
            privateDetails.text = Describe(TestVisibility.Private);
            // 진입 비용은 미션·보유 효과마다 달라진다 — 프리팹에 박아둔 정적 라벨은 여기서 덮어쓴다.
            TMP_Text confirmLabel = confirmButton.GetComponentInChildren<TMP_Text>(true);
            if (confirmLabel != null)
                confirmLabel.text = $"설계 진입\n<size=15>-{model.GetDesignEntryCost(missionId)}$ / +1분기</size>";
            errorText.text = string.Empty;
            confirmButton.interactable = true;
            confirming = false;
            IsOpen = true;
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Hide);
            gameObject.SetActive(true);
        }

        // 배수·증감액 수치를 그대로 노출하던 자리 — 플레이어가 읽는 것은 "어느 쪽이 안전한가"뿐이라
        // 밸런스 값 대신 고정 설명문을 쓴다. 수치가 바뀌어도 이 문구는 따라 바뀌지 않는다.
        private static string Describe(TestVisibility visibility) => visibility == TestVisibility.Private
            ? "사내에서 비공개로 발사 테스트를 진행합니다.\n\n성공 보수가 적지만 \n실패 시 위험 부담도 적습니다. "
            : "모두의 앞에서 공개로 발사 테스트를 진행합니다. \n\n성공 시 투자 혹은 연구비 지원을 \n받을 수 있습니다.\n실패 시 연구비가 줄어들수도..?";

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
