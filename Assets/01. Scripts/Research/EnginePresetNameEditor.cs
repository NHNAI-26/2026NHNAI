using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research
{
    public sealed class EnginePresetNameEditor : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button renameButton;
        [SerializeField] private TMP_Text buttonLabel;
        private ResearchPrototypeModel model;
        private EnginePresetId presetId;
        private Action refreshed;
        private Func<bool> canEdit;
        private bool editing;

        public void Configure(TMP_Text titleText, TMP_InputField field, Button button, TMP_Text label)
        {
            title = titleText;
            input = field;
            renameButton = button;
            buttonLabel = label;
        }

        public void Bind(ResearchPrototypeModel source, EnginePresetId id, Action refresh, Func<bool> allowed)
        {
            model = source;
            presetId = id;
            refreshed = refresh;
            canEdit = allowed;
            renameButton.onClick.RemoveListener(Toggle);
            renameButton.onClick.AddListener(Toggle);
            input.onSubmit.RemoveListener(Submit);
            input.onSubmit.AddListener(Submit);
            Cancel();
        }

        private void Toggle()
        {
            if (editing) { Submit(input.text); return; }
            if (model == null || !model.IsEnginePresetUnlocked(presetId) || !canEdit()) return;
            editing = true;
            title.gameObject.SetActive(false);
            input.gameObject.SetActive(true);
            input.SetTextWithoutNotify(model.GetEnginePresetName(presetId));
            buttonLabel.text = "확정";
            input.ActivateInputField();
            input.Select();
        }

        private void Submit(string value)
        {
            if (!editing) return;
            if (canEdit()) model.RenameEnginePreset(presetId, value);
            Cancel();
            refreshed?.Invoke();
        }

        private void OnDisable() => Cancel();

        private void Cancel()
        {
            editing = false;
            if (input != null) input.gameObject.SetActive(false);
            if (title != null) title.gameObject.SetActive(true);
            if (buttonLabel != null) buttonLabel.text = "변경";
        }
    }
}
