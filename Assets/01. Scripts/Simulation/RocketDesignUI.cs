using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Simulation
{
    /// <summary>
    /// 설계 화면 UI: 좌측 엔진 프리셋 패널(호버 스탯 + 드래그로 꺼내기)과 선택 부품의 이동·회전 버튼.
    /// 연구 운영 화면과 같은 방식으로 UGUI 를 코드에서 만들고 런타임에 스폰한다 — 씬을 건드리지 않는다.
    /// 기획 근거는 <c>docs/specs/rocket-design-ui-spec.md</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RocketDesignUI : MonoBehaviour
    {
        private const string TargetSceneName = "SimulationTest";

        private static readonly Color PanelColor = new(0.15f, 0.18f, 0.22f, 0.96f);
        private static readonly Color EntryColor = new(0.22f, 0.26f, 0.31f, 1f);
        private static readonly Color EntryHoverColor = new(0.30f, 0.38f, 0.46f, 1f);

        private RocketBuilder builder;
        private Canvas canvas;
        private RectTransform statBox;
        private TMP_Text statText;
        private RectTransform partTools;
        private Button moveButton;
        private Button rotateButton;
        private TMP_Text moveLabel;
        private TMP_Text rotateLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SpawnInSimulationScene()
        {
            if (SceneManager.GetActiveScene().name != TargetSceneName) return;
            if (FindFirstObjectByType<RocketDesignUI>() != null) return;

            new GameObject("Rocket Design UI").AddComponent<RocketDesignUI>();
        }

        private void Awake()
        {
            builder = FindFirstObjectByType<RocketBuilder>();
            if (builder == null)
            {
                enabled = false;
                return;
            }

            BuildInterface();
            builder.Changed += RefreshTools;
            RefreshTools();
        }

        private void OnDestroy()
        {
            if (builder != null) builder.Changed -= RefreshTools;
        }

        private void LateUpdate()
        {
            if (builder.Selected == null) return;

            // 버튼은 선택한 부품을 화면에서 따라다닌다. 카메라를 돌려도 같은 부품 옆에 붙어 있다.
            // WorldToScreenPoint 는 픽셀이고 anchoredPosition 은 캔버스 단위라, 스케일러 배율로 나눈다.
            Vector3 screen = builder.Cam.WorldToScreenPoint(builder.Selected.transform.position);
            partTools.gameObject.SetActive(screen.z > 0f);
            partTools.anchoredPosition = new Vector2(screen.x, screen.y) / canvas.scaleFactor;
        }

        // ---- 구성 ---------------------------------------------------------------------------

        private void BuildInterface()
        {
            EnsureEventSystem();

            RectTransform canvasTransform = CreateGroup("RocketDesignCanvas", transform);
            canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasTransform.gameObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildPresetPanel(canvasTransform);
            BuildStatBox(canvasTransform);
            BuildPartTools(canvasTransform);
        }

        private void BuildPresetPanel(RectTransform canvasTransform)
        {
            RectTransform panel = CreatePanel("PresetPanel", canvasTransform, PanelColor);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.offsetMin = new Vector2(16f, 16f);
            panel.offsetMax = new Vector2(216f, -16f);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            CreateText("Title", panel, 18, FontStyles.Bold, "엔진 프리셋");

            IReadOnlyList<EngineStatsSO> presets = builder.PresetLibrary != null
                ? builder.PresetLibrary.Slots
                : null;

            if (presets == null || presets.Count == 0)
            {
                CreateText("Empty", panel, 13, FontStyles.Normal,
                    "프리셋이 없다.\nRocketBuilder 의 Preset Library 에\nEnginePresetLibrarySO 를 연결하라.");
                return;
            }

            for (int i = 0; i < presets.Count; i++)
            {
                EngineStatsSO preset = presets[i];
                if (preset == null) continue;

                RectTransform row = CreatePanel($"Preset_{i}", panel, EntryColor);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 44f;

                TMP_Text label = CreateText("Label", row, 14, FontStyles.Bold, DisplayName(preset));
                label.raycastTarget = false;
                label.alignment = TextAlignmentOptions.Left;
                Fill((RectTransform)label.transform, 10f);

                PresetEntry entry = row.gameObject.AddComponent<PresetEntry>();
                entry.Bind(this, preset, row.GetComponent<Image>());
            }
        }

        private void BuildStatBox(RectTransform canvasTransform)
        {
            statBox = CreatePanel("StatBox", canvasTransform, PanelColor);
            statBox.anchorMin = Vector2.zero;
            statBox.anchorMax = Vector2.zero;
            statBox.pivot = new Vector2(0f, 0.5f);
            statBox.sizeDelta = new Vector2(230f, 130f);
            statBox.GetComponent<Image>().raycastTarget = false;

            statText = CreateText("StatText", statBox, 13, FontStyles.Normal, string.Empty);
            Fill((RectTransform)statText.transform, 10f);

            statBox.gameObject.SetActive(false);
        }

        private void BuildPartTools(RectTransform canvasTransform)
        {
            partTools = CreateGroup("PartTools", canvasTransform);
            partTools.anchorMin = Vector2.zero;
            partTools.anchorMax = Vector2.zero;
            partTools.pivot = new Vector2(0f, 0.5f);
            partTools.sizeDelta = new Vector2(160f, 34f);

            var layout = partTools.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            moveButton = CreateButton("MoveButton", partTools, "이동", out moveLabel);
            rotateButton = CreateButton("RotateButton", partTools, "회전", out rotateLabel);

            moveButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Move));
            rotateButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Rotate));

            partTools.gameObject.SetActive(false);
        }

        // ---- 갱신 ---------------------------------------------------------------------------

        private void RefreshTools()
        {
            bool hasSelection = builder.Selected != null;
            partTools.gameObject.SetActive(hasSelection);
            if (!hasSelection) return;

            // 진행 중인 모드를 라벨로 알린다 — 회전 모드에서는 좌클릭 드래그가 카메라가 아니라 부품을 돌린다.
            moveLabel.text = builder.Mode == RocketBuilder.EditMode.Move ? "이동 중" : "이동";
            rotateLabel.text = builder.Mode == RocketBuilder.EditMode.Rotate ? "회전 중" : "회전";
        }

        /// <summary>에셋 이름에서 `EngineStats_` 접두사를 떼어 목록 폭에 들어가게 한다.</summary>
        private static string DisplayName(EngineStatsSO preset)
        {
            const string Prefix = "EngineStats_";
            string name = preset.name;
            return name.StartsWith(Prefix) ? name[Prefix.Length..] : name;
        }

        internal void ShowStats(EngineStatsSO preset, RectTransform anchor)
        {
            if (preset == null) return;

            statText.text =
                $"<b>{DisplayName(preset)}</b>\n" +
                $"가격 {preset.Price}\n" +
                $"연료 탱크 {preset.FuelCapacity:0} kg\n" +
                $"냉각 {preset.Cooling:0} °C/s\n" +
                $"최대 출력 {preset.MaxOutput:0} N\n" +
                $"점화 신뢰도 {preset.IgnitionReliability:0} %";

            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            statBox.position = new Vector3(corners[2].x + 8f, (corners[1].y + corners[0].y) * 0.5f, 0f);
            statBox.gameObject.SetActive(true);
        }

        internal void HideStats() => statBox.gameObject.SetActive(false);

        internal void BeginPresetDrag(EngineStatsSO preset, Vector2 screenPosition)
        {
            HideStats();
            builder.BeginPresetDrag(preset, screenPosition);
        }

        // ---- 프리셋 항목 --------------------------------------------------------------------

        private sealed class PresetEntry : MonoBehaviour,
            IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
        {
            private RocketDesignUI owner;
            private EngineStatsSO preset;
            private Image background;

            public void Bind(RocketDesignUI ui, EngineStatsSO stats, Image image)
            {
                owner = ui;
                preset = stats;
                background = image;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                background.color = EntryHoverColor;
                owner.ShowStats(preset, (RectTransform)transform);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                background.color = EntryColor;
                owner.HideStats();
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                background.color = EntryColor;
                owner.BeginPresetDrag(preset, eventData.position);
            }
        }

        // ---- 생성 헬퍼 ----------------------------------------------------------------------

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var host = new GameObject("EventSystem", typeof(EventSystem));
            host.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        /// <summary>부모를 꽉 채우도록 늘린다. 레이아웃 그룹 안의 라벨은 이걸 안 하면 부모 밖으로 넘친다.</summary>
        private static void Fill(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static RectTransform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            return (RectTransform)group.transform;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform panel = CreateGroup(name, parent);
            panel.gameObject.AddComponent<Image>().color = color;
            return panel;
        }

        private static TMP_Text CreateText(string name, Transform parent, int fontSize, FontStyles style, string text)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Left;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, string text, out TMP_Text label)
        {
            RectTransform rect = CreatePanel(name, parent, EntryColor);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            label = CreateText("Label", rect, 14, FontStyles.Bold, text);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Fill((RectTransform)label.transform, 0f);

            return button;
        }
    }
}
