using System.Collections.Generic;
using Border.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Border.UI
{
    // Bind before the EventSystem processes input, including buttons created at runtime.
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class UISelectableSoundHook : MonoBehaviour
    {
        private static readonly UnityAction ClickAction = PlayClick;
        private readonly HashSet<Button> boundButtons = new();
        private Selectable[] selectables = new Selectable[64];

        private void Update()
        {
            if (selectables.Length < Selectable.allSelectableCount)
                selectables = new Selectable[Selectable.allSelectableCount * 2];
            int count = Selectable.AllSelectablesNoAlloc(selectables);
            for (int i = 0; i < count; i++)
            {
                if (selectables[i] is Button button && boundButtons.Add(button)) Bind(button);
                selectables[i] = null;
            }
            boundButtons.RemoveWhere(button => button == null);
        }

        public static void Bind(Button button)
        {
            if (button == null) return;
            button.onClick.RemoveListener(ClickAction);
            if (button.GetComponent<UIManualClickSound>() == null)
                button.onClick.AddListener(ClickAction);
        }

        public static void ClearListeners(Button button)
        {
            button.onClick.RemoveAllListeners();
            Bind(button);
        }

        private static void PlayClick() => SoundManager.Instance?.PlaySfx("click");

        private void OnDisable()
        {
            foreach (Button button in boundButtons)
                if (button != null) button.onClick.RemoveListener(ClickAction);
            boundButtons.Clear();
        }
    }
}
