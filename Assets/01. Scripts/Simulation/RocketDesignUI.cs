using System.Collections.Generic;
using Border.Research;
using Border.UI;
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

        // 버튼 바를 부품 중심 위로 띄우는 세로 간격(캔버스 단위). 기즈모가 화면상 고정
        // 크기(720 기준 반경 약 72)라 상수 하나로 모든 줌에서 통한다 — 90 이면 +Y 핸들 끝을
        // 넘어가서 바가 핸들 클릭을 먹지 않는다. 이 컴포넌트는 RuntimeInitializeOnLoadMethod
        // 로 스폰돼 인스펙터에서 값을 넣을 사람이 없으므로 SerializeField 가 아니라 const 다.
        private const float ToolsGap = 90f;

        private static readonly Color PanelColor = new(0.15f, 0.18f, 0.22f, 0.96f);
        private static readonly Color EntryColor = new(0.22f, 0.26f, 0.31f, 1f);
        private static readonly Color EntryHoverColor = new(0.30f, 0.38f, 0.46f, 1f);

        // 미션 컨트롤 모드에서만 만드는 것들. 01_Main 위에 얹었을 때(SimulationStageHost)만 켜지고,
        // SimulationTest 씬을 직접 재생하면 예전 그대로 좌측 패널만 뜬다.
        private bool missionControl;
        private Rocket rocket;
        private RectTransform viewport;
        private TMP_Text topBarText;
        private readonly List<RocketPart> placedParts = new();
        private readonly Vector3[] viewportCorners = new Vector3[4];

        private RocketBuilder builder;
        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform presetPanel;
        private RectTransform statBox;
        private TMP_Text statText;
        private RectTransform partTools;
        private RectTransform launchPip;
        private RawImage pipImage;
        private TMP_Text pipLabel;
        private Button moveButton;
        private Button rotateButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SpawnInSimulationScene()
        {
            if (SceneManager.GetActiveScene().name != TargetSceneName) return;
            if (FindFirstObjectByType<RocketDesignUI>() != null) return;

            Spawn(false);
        }

        /// <summary>
        /// Awake 는 AddComponent 시점에 돌아서 컴포넌트를 붙인 뒤에는 모드를 넣을 수 없다.
        /// 비활성 상태로 만들어 필드를 채운 뒤 켠다.
        /// </summary>
        internal static RocketDesignUI Spawn(bool missionControl)
        {
            var host = new GameObject("Rocket Design UI");
            host.SetActive(false);

            RocketDesignUI ui = host.AddComponent<RocketDesignUI>();
            ui.missionControl = missionControl;

            host.SetActive(true);
            return ui;
        }

        private void Awake()
        {
            builder = FindFirstObjectByType<RocketBuilder>();
            if (builder == null)
            {
                enabled = false;
                return;
            }

            rocket = FindFirstObjectByType<Rocket>();

            BuildInterface();
            builder.Changed += RefreshTools;
            builder.PresetLibraryChanged += RebuildPresetPanel;
            RefreshTools();
        }

        private void OnDestroy()
        {
            if (builder == null) return;

            builder.Changed -= RefreshTools;
            builder.PresetLibraryChanged -= RebuildPresetPanel;
        }

        private void LateUpdate()
        {
            if (missionControl)
            {
                UpdateViewportRect();
                UpdateTopBar();
            }

            UpdateLaunchPip();

            if (builder.Selected == null || (rocket != null && rocket.Launched)) return;

            // 버튼은 선택한 부품을 화면에서 따라다닌다. 카메라를 돌려도 같은 부품 옆에 붙어 있다.
            // WorldToScreenPoint 는 픽셀이고 anchoredPosition 은 캔버스 단위라, 스케일러 배율로 나눈다.
            Vector3 screen = builder.Cam.WorldToScreenPoint(builder.Selected.transform.position);
            partTools.gameObject.SetActive(screen.z > 0f);

            // 부품 위로 ToolsGap 만큼 띄운다(pivot 이 아래 가운데). 부품도 기즈모도 가리지 않는다.
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 half = partTools.sizeDelta * 0.5f;
            Vector2 point = new Vector2(screen.x, screen.y) / canvas.scaleFactor + new Vector2(0f, ToolsGap);

            // 화면 밖으로 나가면 버튼을 누를 수 없다 — 캔버스 안으로 가둔다.
            partTools.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x, half.x, Mathf.Max(half.x, canvasSize.x - half.x)),
                Mathf.Clamp(point.y, 0f, Mathf.Max(0f, canvasSize.y - partTools.sizeDelta.y)));
        }

        // ---- 구성 ---------------------------------------------------------------------------

        private void BuildInterface()
        {
            EnsureEventSystem();

            RectTransform canvasTransform = CreateGroup("RocketDesignCanvas", transform);
            canvasRect = canvasTransform; // 버튼 바 클램프에서 매 프레임 쓴다 — 캐스팅을 반복하지 않는다
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
            BuildLaunchPip(canvasTransform);

            if (missionControl) BuildMissionControl(canvasTransform);
        }

        /// <summary>
        /// 상단 정보 바와 3D 뷰포트 자리. 뷰포트는 <b>Image 가 없는 빈 RectTransform</b> 이어야 한다 —
        /// 그래픽을 붙이면 <see cref="EventSystem.IsPointerOverGameObject"/> 가 뷰포트 전체를 UI 로
        /// 판정해서 <see cref="RocketBuilder"/> 의 3D 입력이 통째로 막힌다. 같은 이유로 전체 화면
        /// 배경 패널도 만들지 않는다.
        /// </summary>
        private void BuildMissionControl(RectTransform canvasTransform)
        {
            RectTransform topBar = CreatePanel("TopBar", canvasTransform, PanelColor);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = Vector2.one;
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.offsetMin = new Vector2(232f, -88f); // 좌측 프리셋 패널(오른쪽 끝 216)에서 16 띄운다
            topBar.offsetMax = new Vector2(-16f, -16f);

            topBarText = CreateText("Info", topBar, 18, FontStyles.Bold, string.Empty);
            topBarText.alignment = TextAlignmentOptions.Left;
            topBarText.raycastTarget = false;
            Fill((RectTransform)topBarText.transform, 12f);

            // 카메라 뷰포트의 유일한 원천. 정규화 상수를 따로 두면 CanvasScaler 가 늘어나는 비율에서
            // 좌측 패널과 어긋난다 — 매 프레임 이 사각형을 읽어 카메라에 먹인다.
            viewport = CreateGroup("Viewport", canvasTransform);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(232f, 16f);
            viewport.offsetMax = new Vector2(-16f, -88f);
        }

        private void BuildPresetPanel(RectTransform canvasTransform)
        {
            if (presetPanel != null)
            {
                Destroy(presetPanel.gameObject);
            }

            presetPanel = CreatePanel("PresetPanel", canvasTransform, PanelColor);
            presetPanel.SetAsFirstSibling();
            presetPanel.anchorMin = new Vector2(0f, 0f);
            presetPanel.anchorMax = new Vector2(0f, 1f);
            presetPanel.pivot = new Vector2(0f, 0.5f);
            presetPanel.offsetMin = new Vector2(16f, 16f);
            presetPanel.offsetMax = new Vector2(216f, -16f);

            var layout = presetPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            CreateText("Title", presetPanel, 18, FontStyles.Bold, "엔진 프리셋");

            IReadOnlyList<EngineStatsSO> presets = builder.PresetLibrary != null
                ? builder.PresetLibrary.Slots
                : null;

            if (presets == null || presets.Count == 0)
            {
                CreateText("Empty", presetPanel, 13, FontStyles.Normal,
                    "프리셋이 없다.\nRocketBuilder 의 Preset Library 에\nEnginePresetLibrarySO 를 연결하라.");
                return;
            }

            for (int i = 0; i < presets.Count; i++)
            {
                EngineStatsSO preset = presets[i];
                if (preset == null) continue;

                RectTransform row = CreatePanel($"Preset_{i}", presetPanel, EntryColor);
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
            partTools.pivot = new Vector2(0.5f, 0f); // 아래 가운데를 기준점으로 잡아 부품 위에 세운다
            partTools.sizeDelta = new Vector2(80f, 34f); // 정사각 아이콘 버튼 두 개 + spacing

            var layout = partTools.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            moveButton = CreateButton("MoveButton", partTools, ArtemisCursor.IconSprite(ArtemisCursor.Icon.Move));
            rotateButton = CreateButton("RotateButton", partTools,
                ArtemisCursor.IconSprite(ArtemisCursor.Icon.Rotate));

            moveButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Move));
            rotateButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Rotate));

            partTools.gameObject.SetActive(false);
        }

        /// <summary>
        /// 발사 후에만 뜨는 작은 화면. 두 번째 카메라가 RenderTexture 에 그린 것을 그대로 얹고,
        /// 누르면 큰 화면과 역할이 바뀐다(<see cref="RocketBuilder.ToggleLaunchView"/>).
        /// 우하단 (-16, 16) 은 미션 컨트롤 모드의 <c>Viewport</c> 오른쪽·아래 여백과 같은 값이라
        /// 3D 뷰 안에 정확히 앉는다.
        /// </summary>
        private void BuildLaunchPip(RectTransform canvasTransform)
        {
            launchPip = CreatePanel("LaunchPip", canvasTransform, PanelColor); // 테두리 겸 배경
            launchPip.anchorMin = Vector2.right; // (1, 0)
            launchPip.anchorMax = Vector2.right;
            launchPip.pivot = Vector2.right;
            launchPip.anchoredPosition = new Vector2(-16f, 16f);
            launchPip.sizeDelta = new Vector2(324f, 184f);

            var view = new GameObject("View", typeof(RectTransform));
            view.transform.SetParent(launchPip, false);
            pipImage = view.AddComponent<RawImage>();
            pipImage.raycastTarget = false; // 클릭은 부모 버튼이 받는다
            Fill((RectTransform)view.transform, 2f);

            // 어느 뷰가 어느 화면에 있는지 글자로 박아 둔다 — 두 각도가 비슷해 보이는 저고도 구간에서도
            // 눌렀을 때 바뀌었다는 것이 즉시 읽힌다.
            pipLabel = CreateText("Label", launchPip, 13, FontStyles.Bold, string.Empty);
            pipLabel.raycastTarget = false;
            pipLabel.alignment = TextAlignmentOptions.BottomLeft;
            Fill((RectTransform)pipLabel.transform, 8f);

            var button = launchPip.gameObject.AddComponent<Button>();
            button.targetGraphic = launchPip.GetComponent<Image>();
            button.onClick.AddListener(builder.ToggleLaunchView);

            launchPip.gameObject.SetActive(false);
        }

        // ---- 갱신 ---------------------------------------------------------------------------

        private void RefreshTools()
        {
            // 발사 뒤에는 설계 조작이 전부 막힌다(RocketBuilder) — 눌러도 아무 일 없는 UI 를 남기지 않는다.
            bool launched = rocket != null && rocket.Launched;
            presetPanel.gameObject.SetActive(!launched);

            bool hasSelection = !launched && builder.Selected != null;
            partTools.gameObject.SetActive(hasSelection);
            if (!hasSelection) return;

            // 진행 중인 모드를 배경색으로 알린다 — 회전 모드에서는 좌클릭 드래그가 카메라가 아니라 부품을 돌린다.
            // Button 의 기본 ColorTint 는 normalColor 가 흰색이라 Image.color 를 그대로 곱해 내보낸다.
            moveButton.targetGraphic.color =
                builder.Mode == RocketBuilder.EditMode.Move ? EntryHoverColor : EntryColor;
            rotateButton.targetGraphic.color =
                builder.Mode == RocketBuilder.EditMode.Rotate ? EntryHoverColor : EntryColor;
        }

        /// <summary>
        /// 뷰포트 RectTransform 을 카메라의 정규화 사각형으로 옮긴다. 오버레이 캔버스의
        /// <see cref="RectTransform.GetWorldCorners"/> 는 화면 픽셀이라(StatBox 도 같은 가정) 화면 크기로
        /// 나누기만 하면 된다. 창 크기나 화면 비율이 바뀌어도 한 프레임 뒤에 따라온다.
        /// </summary>
        private void UpdateViewportRect()
        {
            viewport.GetWorldCorners(viewportCorners);

            var rect = new Rect(
                viewportCorners[0].x / Screen.width,
                viewportCorners[0].y / Screen.height,
                (viewportCorners[2].x - viewportCorners[0].x) / Screen.width,
                (viewportCorners[2].y - viewportCorners[0].y) / Screen.height);

            // rect 대입은 URP 렌더 타깃 재할당을 부른다 — 값이 그대로면 건드리지 않는다.
            if (builder.Cam.rect != rect) builder.Cam.rect = rect;
        }

        /// <summary>
        /// 작은 화면을 매 프레임 상태에서 유도한다. 이벤트(<c>builder.Changed</c>)로 켜면
        /// 발사 프레임에 선택이 이미 비어 있어 이벤트가 오지 않는 경우를 놓친다 — 상태를 그대로 읽는 편이 싸다.
        /// </summary>
        private void UpdateLaunchPip()
        {
            bool launched = rocket != null && rocket.Launched;
            if (launchPip.gameObject.activeSelf != launched) launchPip.gameObject.SetActive(launched);
            if (!launched) return;

            // 텍스처는 발사 순간 RocketBuilder 가 만든다 — 한 프레임 늦게 잡힐 수 있어 붙을 때까지 본다.
            if (pipImage.texture == null) pipImage.texture = builder.LaunchPipTexture;
            pipLabel.text = builder.LaunchViewSwapped ? "추적 뷰" : "후퇴 뷰";
        }

        private void UpdateTopBar()
        {
            // builder.Changed 는 부착 때 발생하지 않으므로(EndDrag 가 Attach 만 부른다) 캐시하면
            // 낡은 값이 남는다. 부품 몇 개짜리 합이라 매 프레임 다시 세는 편이 싸다.
            int installed = 0;
            if (rocket != null)
            {
                rocket.GetComponentsInChildren(placedParts);
                for (int i = 0; i < placedParts.Count; i++)
                {
                    if (placedParts[i].Stats != null) installed += placedParts[i].Stats.Price;
                }
            }

            ResearchPrototypeModel model = ResearchFlowSession.GetOrCreate().Model;
            topBarText.text =
                $"{model.Year}년 {model.Quarter}분기      예산 {model.Funds:N0}      설치 비용 {installed:N0}";
        }

        private void RebuildPresetPanel()
        {
            HideStats();
            BuildPresetPanel((RectTransform)canvas.transform);
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
            IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler
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
                ArtemisCursor.Request(ArtemisCursor.Visual.Hover);
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
                ArtemisCursor.Request(ArtemisCursor.Visual.Drag, 30);
                owner.BeginPresetDrag(preset, eventData.position);
            }

            /// <summary>
            /// 입력 모듈은 드래그 대상을 <see cref="IBeginDragHandler"/> 가 아니라
            /// <see cref="IDragHandler"/> 로 찾고(<c>eventData.pointerDrag</c>), 못 찾으면 드래그 처리에
            /// 들어가기도 전에 빠져나간다 — 이 빈 구현이 없으면 <see cref="OnBeginDrag"/> 가 아예 안 불린다.
            /// 실제 이동·부착·취소는 <see cref="RocketBuilder"/> 가 마우스를 직접 폴링해서 처리한다.
            /// </summary>
            public void OnDrag(PointerEventData eventData) { }
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

        private static Button CreateButton(string name, Transform parent, Sprite icon)
        {
            RectTransform rect = CreatePanel(name, parent, EntryColor);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            var iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(rect, false);
            var image = iconObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Fill((RectTransform)iconObject.transform, 7f);

            return button;
        }
    }
}
