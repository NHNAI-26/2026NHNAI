using UnityEngine;

namespace Border.Research
{
    public sealed class ResearchPrototypeController : MonoBehaviour
    {
        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        private void OnGUI()
        {
            EnsureStyles();

            int panelWidth = Mathf.Min(560, Screen.width - 32);
            int panelHeight = 150;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none, boxStyle);
            GUILayout.Label("레거시 연구 IMGUI 비활성", titleStyle);
            GUILayout.Space(8);
            GUILayout.Label("최신 연구 화면은 01_Main.unity에서 UGUI로 자동 생성됩니다. 이 화면은 옛 진행도 기반 연구 조작을 막기 위한 안내만 표시합니다.", bodyStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (boxStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };
        }
    }
}
