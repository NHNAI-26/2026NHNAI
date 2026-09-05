using System.Collections.Generic;
using Border.Research;
using Border.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Simulation
{
    /// <summary>
    /// 설계 화면 UI: 좌측 엔진 프리셋 패널(호버 스탯 + 드래그로 꺼내기)과 선택 부품의 이동·회전 버튼.
    ///
    /// 화면 자체는 <c>Assets/03. Prefabs/UI/Simulation/RocketDesignUI.prefab</c> 에 저작돼 있고
    /// <c>SimulationTest</c> 씬에 인스턴스로 놓여 있다. 이 컴포넌트는 그것을 짓지 않는다 —
    /// <see cref="BindInterface"/> 로 자식을 이름으로 잡고 <see cref="WireInterface"/> 로 리스너·
    /// 커서 아이콘·프리셋 목록만 채운다. 셋 다 씬 객체나 런타임 상태를 가리켜 프리팹에 못 담기는 것들이다.
    /// 예외는 <c>DesignStageTester</c> 용 화면으로, 그쪽만 아직 <see cref="BakeTesterInterface"/> 가
    /// 코드로 굽는다. 기획 근거는 <c>docs/specs/rocket-design-ui-spec.md</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RocketDesignUI : MonoBehaviour
    {
        private const string CanvasName = "RocketDesignCanvas";

        // 버튼 바를 부품 중심 위로 띄우는 세로 간격(캔버스 단위). 기즈모가 화면상 고정
        // 크기(720 기준 반경 약 72)라 상수 하나로 모든 줌에서 통한다 — 90 이면 +Y 핸들 끝을
        // 넘어가서 바가 핸들 클릭을 먹지 않는다. 이 컴포넌트는 RuntimeInitializeOnLoadMethod
        // 로 스폰돼 인스펙터에서 값을 넣을 사람이 없으므로 SerializeField 가 아니라 const 다.
        private const float ToolsGap = 90f;

        // 관제 배치의 치수(캔버스 단위, 기준 해상도 1280x720). 하나만 고치면 상·하단 바, 좌측 패널,
        // 뷰포트, 단계 스테퍼가 함께 따라온다 — 같은 숫자를 여러 곳에 흩어 두면 어긋난다.
        //
        // 미션 컨트롤에서는 패널을 화면 가장자리와 서로에게 딱 붙인다. 카메라가 뷰포트 사각형만
        // 그리므로 그 밖은 아무도 지우지 않고 이전 프레임 픽셀이 그대로 남는다 — 여백을 두면 그 틈으로
        // 찌꺼기가 보인다. 오버레이 캔버스는 3D 뷰 위에 그려지므로 전체 화면 배경으로 덮을 수도 없다.
        private const float Margin = 16f;      // 테스터 화면의 가장자리 여백
        private const float BarHeight = 64f;   // 상·하단 바 높이 — 프리팹과 같은 값이어야 한다
        private const float DockWidth = 200f;  // 좌측 도크 폭 — 같음
        private const float ViewportLeft = DockWidth;

        // 프로젝트 기본 UI 아트 시트. 연구·메뉴 화면은 에디터에서 프리팹에 구워 넣지만
        // (ResearchUiArtApplicator, MenuUiPrefabBuilder) 이 화면은 런타임에 만들어지므로
        // 같은 시트를 Resources 에서 직접 읽는다 — 두 화면이 다른 껍데기를 쓰면 같은 게임으로 안 읽힌다.
        private const string ArtSheetName = "engine_ui_01";
        private const int PanelSprite = 0;  // 패널·바 배경
        private const int BoxSprite = 4;    // 작은 상자(스탯 툴팁)
        private const int ButtonSprite = 5; // 버튼
        private const int CardSprite = 6;   // 목록 카드(프리셋 한 줄)
        private const string ArtMaterialName = "UI_general";
        private static Sprite[] artSprites;
        private static Material artMaterial;

        // 아트를 입힌 그래픽은 색이 아니라 틴트로 상태를 알린다 — 스프라이트에 어두운 색을 곱하면 그림이 죽는다.
        private static readonly Color TintIdle = Color.white;
        private static readonly Color TintActive = new(0.55f, 0.95f, 1f, 1f);

        // 자금이 모자라 못 꺼내는 카드. 어두운 색을 곱하지 않고 알파만 낮춘다 — 위 주석대로
        // 곱하면 아트가 죽는다.
        private static readonly Color TintDisabled = new(1f, 1f, 1f, 0.35f);

        // 단계 스테퍼는 시트에 맞는 조각이 없어 색만 쓴다. 강조색은 ArtemisCursor 의 시안과 같은 자리다.
        private static readonly Color StageActiveColor = new(0.11f, 0.91f, 0.93f, 1f);
        private static readonly Color StagePendingColor = new(0.30f, 0.38f, 0.46f, 1f);
        private static readonly Color StageIdleColor = new(0.22f, 0.26f, 0.31f, 1f);

        // 미션 컨트롤 모드에서만 만드는 것들. 01_Main 위에 얹었을 때(SimulationStageHost)만 켜지고,
        // SimulationTest 씬을 직접 재생하면 예전 그대로 좌측 패널만 뜬다.
        [SerializeField] private bool testerInterface;
        private Rocket rocket;
        private RectTransform viewport;
        private TMP_Text dateText;
        private TMP_Text fundsText;
        private readonly int[] installedPresetCounts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
        private TMP_Text missionText;
        private RectTransform taskPanel;
        private TMP_Text taskText;
        private RectTransform stageStrip;
        private Image[] stageDots;
        private Image[] stageLines;
        private TMP_Text[] stageLabels;
        private SimulationStageHost stageHost;
        private Button launchButton;
        private Button destructButton;
        private Button exitButton;
        private LaunchMissionController mission;
        private readonly List<RocketPart> placedParts = new();
        private readonly Vector3[] viewportCorners = new Vector3[4];

        // 프리팹의 줄 이름(`Row_{라벨}`)과 같아야 한다 — BindMissionControl 이 이 배열로 찾는다.
        private static readonly string[] FlightInfoLabels =
            { "미션", "최고 고도", "최대 거리", "총 연소", "체류", "남은 연료", "결과" };

        private RocketBuilder builder;
        private Canvas canvas;

        /// <summary><see cref="SimulationCrtScreen"/> 이 렌더 모드를 잠시 바꿔 쓴다.</summary>
        internal Canvas Canvas => canvas;

        private RectTransform canvasRect;
        private RectTransform presetPanel;

        // 자금 판정을 매 프레임 돌리므로 행을 들고 있는다 — GetComponentsInChildren 을 반복하지 않는다.
        private readonly List<PresetEntry> presetEntries = new();
        private RectTransform flightInfoPanel;
        private TMP_Text[] flightInfoValues;
        private string launchGradeLabel;
        private RectTransform statBox;
        private TMP_Text statText;
        private RectTransform partTools;
        private RectTransform launchPip;
        private RawImage pipImage;
        private TMP_Text pipLabel;
        private Button moveButton;
        private Button rotateButton;

        private void Awake()
        {
            builder = FindFirstObjectByType<RocketBuilder>();
            if (builder == null)
            {
                enabled = false;
                return;
            }

            rocket = FindFirstObjectByType<Rocket>();

            EnsureEventSystem();
            // 구운 프리팹으로 들어왔으면 캔버스가 이미 자식으로 있다 — 다시 짓지 않고 참조만 잡는다.
            if (transform.Find(CanvasName) != null) BindInterface();
            else BuildInterface();
            WireInterface();

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
            // LaunchMissionController 는 SimulationStageHost 가 씬을 올린 **뒤에** 로켓에 붙인다.
            // 이 화면은 그 씬에 함께 들어오므로 Awake 시점에는 아직 없다 — 붙을 때까지 매 프레임 본다.
            if (mission == null && rocket != null) mission = rocket.GetComponent<LaunchMissionController>();

            if (testerInterface)
            {
                launchButton.gameObject.SetActive(!rocket.Launched);
                RefreshTools();
            }
            else
            {
                UpdateViewportRect();
                UpdateTopBar();
                UpdateFlightInfo();
                UpdateStageStrip();
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

        /// <summary>
        /// 테스터 화면만 코드로 짓는다(<see cref="BakeTesterInterface"/>). 리스너, 커서 아이콘,
        /// 프리셋 목록처럼 씬·연구 상태에 매인 것은 <see cref="WireInterface"/> 몫이라 여기 없다.
        /// </summary>
        private void BuildInterface()
        {
            RectTransform canvasTransform = CreateGroup(CanvasName, transform);
            canvasRect = canvasTransform; // 버튼 바 클램프에서 매 프레임 쓴다 — 캐스팅을 반복하지 않는다
            canvas = canvasTransform.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasTransform.gameObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasTransform.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildPresetPanelShell(canvasTransform);
            BuildStatBox(canvasTransform);
            BuildPartTools(canvasTransform);
            BuildLaunchPip(canvasTransform);
        }

        /// <summary>
        /// 프리셋 패널의 껍데기(배경·레이아웃·제목)만 만든다. 목록은 연구가 무엇을 열었는지에 따라
        /// 달라져 구울 수 없으므로 <see cref="FillPresetPanel"/> 이 런타임에 채운다.
        /// </summary>
        private void BuildPresetPanelShell(RectTransform canvasTransform)
        {
            presetPanel = CreateArtPanel("PresetPanel", canvasTransform, PanelSprite);
            presetPanel.SetAsFirstSibling();
            presetPanel.anchorMin = new Vector2(0f, 0f);
            presetPanel.anchorMax = new Vector2(0f, 1f);
            presetPanel.pivot = new Vector2(0f, 0.5f);
            // 미션 컨트롤 모드에서는 상·하단 바 사이에 딱 끼우고, SimulationTest 단독 재생에서는 예전처럼 띄운다.
            presetPanel.offsetMin = new Vector2(Margin, Margin);
            presetPanel.offsetMax = new Vector2(Margin + DockWidth, -Margin);

            var layout = presetPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            CreateText("Title", presetPanel, 18, FontStyles.Bold, "엔진 프리셋");
        }

        /// <summary>
        /// 제목 아래 목록을 새로 깐다. 지운 자식은 같은 프레임에 사라지지 않으므로
        /// (<see cref="Object.Destroy"/> 는 지연된다) 먼저 부모에서 떼어 낸다 — 안 그러면
        /// 세로 레이아웃이 한 프레임 동안 옛 줄과 새 줄을 같이 늘어놓는다.
        /// </summary>
        private void FillPresetPanel()
        {
            presetEntries.Clear();
            for (int i = presetPanel.childCount - 1; i >= 1; i--)
            {
                Transform child = presetPanel.GetChild(i);
                child.SetParent(null, false);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            IReadOnlyList<EngineStatsSO> presets = builder.PresetLibrary != null
                ? builder.PresetLibrary.Slots
                : null;

            // 연구가 아직 열지 않은 슬롯은 목록에서 아예 뺀다 — GDD 07 §4 와 엔진 프리셋 스펙이
            // "개발된 슬롯만 보여준다"로 확정했다. 라이브러리 자체는 계속 10칸이다 — 슬롯 인덱스가
            // EnginePresetId 로의 유일한 매핑이라(ResearchEnginePresetRuntimeBridge) 거기서 빼면 안 된다.
            ResearchPrototypeModel model = testerInterface ? null : ResearchFlowSession.GetOrCreate().Model;
            int shown = 0;

            for (int i = 0; presets != null && i < presets.Count; i++)
            {
                EngineStatsSO preset = presets[i];
                if (preset == null || (!testerInterface && !IsDeveloped(preset, model))) continue;

                shown++;

                RectTransform row = CreateArtPanel($"Preset_{i}", presetPanel, CardSprite);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 44f;

                TMP_Text label = CreateText("Label", row, 14, FontStyles.Bold, DisplayName(preset));
                label.raycastTarget = false;
                label.alignment = TextAlignmentOptions.Left;
                Fill((RectTransform)label.transform, 10f);

                if (testerInterface)
                {
                    row.gameObject.AddComponent<DesignTesterPresetEntry>().SetPreset(preset);
                }
                else
                {
                    var entry = row.gameObject.AddComponent<PresetEntry>();
                    entry.Bind(this, preset, row.GetComponent<Image>(), label);
                    presetEntries.Add(entry);
                }
            }

            if (shown == 0)
            {
                CreateText("Empty", presetPanel, 13, FontStyles.Normal,
                    "프리셋이 없다.\nRocketBuilder 의 Preset Library 에\nEnginePresetLibrarySO 를 연결하라.");
            }
        }

        private void BuildStatBox(RectTransform canvasTransform)
        {
            statBox = CreateArtPanel("StatBox", canvasTransform, BoxSprite);
            statBox.anchorMin = Vector2.zero;
            statBox.anchorMax = Vector2.zero;
            statBox.pivot = new Vector2(0f, 0.5f);
            statBox.sizeDelta = new Vector2(230f, 152f); // 물리 6줄 + 연구 진행 1줄
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

            partTools.gameObject.SetActive(false);
        }

        /// <summary>
        /// 발사 후에만 뜨는 작은 화면. 두 번째 카메라가 RenderTexture 에 그린 것을 그대로 얹고,
        /// 누르면 큰 화면과 역할이 바뀐다(<see cref="RocketBuilder.ToggleLaunchView"/>).
        /// 테스터 화면에서는 캔버스 우하단에 둔다. 저작 프리팹 쪽은 <c>Viewport</c> 의 자식이라
        /// 3D 뷰 안 우하단에 앉는다 — 캔버스에 그대로 두면 하단 바 뒤로 들어간다.
        /// </summary>
        private void BuildLaunchPip(RectTransform canvasTransform)
        {
            launchPip = CreateArtPanel("LaunchPip", canvasTransform, PanelSprite); // 테두리 겸 배경
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

            launchPip.gameObject.SetActive(false);
        }

        // ---- 갱신 ---------------------------------------------------------------------------

        private void RefreshTools()
        {
            // 발사 뒤에는 설계 조작이 전부 막힌다(RocketBuilder) — 눌러도 아무 일 없는 UI 를 남기지 않는다.
            bool launched = rocket != null && rocket.Launched;
            presetPanel.gameObject.SetActive(!launched);
            // 관제 모드가 아니면 정보 패널이 없다 — 예전처럼 도크가 비고 뷰가 넘겨받는다.
            if (flightInfoPanel != null) flightInfoPanel.gameObject.SetActive(launched);

            bool hasSelection = !launched && builder.Selected != null;
            partTools.gameObject.SetActive(hasSelection);
            if (!hasSelection) return;

            // 진행 중인 모드를 배경색으로 알린다 — 회전 모드에서는 좌클릭 드래그가 카메라가 아니라 부품을 돌린다.
            // Button 의 기본 ColorTint 는 normalColor 가 흰색이라 Image.color 를 그대로 곱해 내보낸다.
            moveButton.targetGraphic.color =
                builder.Mode == RocketBuilder.EditMode.Move ? TintActive : TintIdle;
            rotateButton.targetGraphic.color =
                builder.Mode == RocketBuilder.EditMode.Rotate ? TintActive : TintIdle;
        }

        /// <summary>
        /// 뷰포트 RectTransform 을 카메라의 정규화 사각형으로 옮긴다. 창 크기나 화면 비율이 바뀌어도
        /// 한 프레임 뒤에 따라온다. <see cref="RectTransform.GetWorldCorners"/> 는 오버레이 캔버스에서만
        /// 화면 픽셀이라(StatBox 도 같은 가정), CRT 화면이 캔버스를 Screen Space - Camera 로 돌려놓는
        /// 동안에는 <see cref="SimulationCrtScreen"/> 이 물려 준 카메라로 환산해야 한다.
        /// </summary>
        private void UpdateViewportRect()
        {
            // 발사 여부를 다시 판단하지 않고 패널이 실제로 켜져 있는지를 본다 — 두 조건이 어긋나면 패널과 뷰가 겹친다.
            // 관제 모드에서는 프리셋 패널과 발사 정보 패널이 같은 도크를 번갈아 쓰므로 폭이 그대로 유지된다.
            bool dockOccupied = presetPanel.gameObject.activeSelf
                || (flightInfoPanel != null && flightInfoPanel.gameObject.activeSelf);
            float left = dockOccupied ? ViewportLeft : 0f;
            if (!Mathf.Approximately(viewport.offsetMin.x, left))
            {
                viewport.offsetMin = new Vector2(left, viewport.offsetMin.y);
                stageStrip.offsetMin = new Vector2(left, stageStrip.offsetMin.y);
            }

            viewport.GetWorldCorners(viewportCorners);

            // 오버레이 모드에서는 카메라가 null 이고 이 호출이 월드 좌표를 그대로 돌려준다 — 예전과 같은 값이다.
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, viewportCorners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, viewportCorners[2]);

            var rect = new Rect(
                min.x / Screen.width,
                min.y / Screen.height,
                (max.x - min.x) / Screen.width,
                (max.y - min.y) / Screen.height);

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
            bool launched = rocket != null && rocket.Launched;
            launchButton.gameObject.SetActive(!launched);
            destructButton.gameObject.SetActive(launched);
            destructButton.interactable = mission != null && mission.CanSelfDestruct;

            // builder.Changed 는 부착 때 발생하지 않으므로(EndDrag 가 Attach 만 부른다) 캐시하면
            // 낡은 값이 남는다. 부품 몇 개짜리 합이라 매 프레임 다시 세는 편이 싸다.
            int installed = 0;
            System.Array.Clear(installedPresetCounts, 0, installedPresetCounts.Length);
            if (rocket != null)
            {
                rocket.GetComponentsInChildren(placedParts);
                for (int i = 0; i < placedParts.Count; i++)
                {
                    if (placedParts[i].Stats != null) installed += placedParts[i].Stats.Price;
                    if (placedParts[i].Stats != null && TryGetPresetId(placedParts[i].Stats, out EnginePresetId presetId))
                        installedPresetCounts[(int)presetId]++;
                }
            }

            // 설치비는 발사 순간에야 실제로 빠져나가므로(ResearchPrototypeModel.BeginLaunch), 설계 중에는
            // 예산에서 미리 뺀 값을 보여 준다 — 엔진을 붙일 때마다 그 자리에서 줄어드는 것이 읽혀야 한다.
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            ResearchPrototypeModel model = session.Model;
            if (!launched && session.HasPendingDesignEntry)
            {
                ResearchDesignEntryData entry = session.PendingDesignEntry;
                ResearchDesignEntryData quote = model.CreateDesignEntry(entry.MissionId, entry.SelectedEnginePresetId,
                    installedPresetCounts, entry.DesignFit, entry.Visibility, entry.LaunchCostPaid, entry.LaunchCost);
                installed = quote.ReservedInstallCost;
                launchButton.interactable = placedParts.Count > 0 && model.Funds >= model.GetLaunchPaymentCost(quote);
            }
            // 설계 진입 중이 아니면(SimulationTest 단독 재생) 연구 예산 자체가 없어 아무것도 막지 않는다.
            RefreshPresetAffordability(model, installed, !launched && session.HasPendingDesignEntry);

            // 연구 운영 화면(ResearchOperationUIController)과 같은 문자열이다 — 두 화면이 같은 값을
            // 다르게 적으면 같은 숫자라는 것이 안 읽힌다. 고칠 때 양쪽을 같이 고친다.
            dateText.text = $"{model.Year}.Q{model.Quarter} / 남은 분기 : {model.RemainingTurns}";
            fundsText.text = $"보유 자금 : {model.Funds:N0} $\n설치 : {installed:N0} $";

            // 목표는 뷰 안 상단 TASK 패널이 맡는다. 번호는 LaunchMissionId 값 그대로다 —
            // StaticFire=0 은 직렬화 호환용 죽은 값이라 실제 미션은 1부터 시작한다.
            LaunchMissionId missionId = session.HasPendingDesignEntry
                ? session.PendingDesignEntry.MissionId
                : model.GetCurrentMission();
            bool hasObjective = mission != null && !string.IsNullOrEmpty(mission.Objective);
            taskPanel.gameObject.SetActive(hasObjective);
            if (hasObjective) taskText.text = $"TASK {(int)missionId} : {mission.Objective}";

            // 하단 바는 발사 전에는 남은 이벤트 효과와 발사 거부 사유를, 발사 뒤에는 비행 상태를 쓴다.
            // 셋은 서로 다른 시점에만 나와 자리를 다투지 않는다. 거부 사유만은 다음 발사 시도까지
            // 지워지지 않으므로 이벤트 아래에 붙인다 — 위에 두면 한 번 거부당한 뒤로 이벤트가 영영 가린다.
            missionText.text = launched && mission != null
                ? mission.Status
                : JoinLines(model.PendingLaunchEffectsText, stageHost != null ? stageHost.LaunchMessage : null);
        }

        private static string JoinLines(string first, string second)
        {
            if (string.IsNullOrEmpty(first)) return second ?? string.Empty;
            if (string.IsNullOrEmpty(second)) return first;
            return first + "\n" + second;
        }

        /// <summary>
        /// 살 수 없는 엔진 카드를 흐리게 하고 드래그를 막는다. 기준은 <b>예약 설치비 + 이 엔진 1개</b>
        /// 이고 발사 비용은 보지 않는다. 한 개 더 붙일 때의 값은 프리셋마다 다르다 —
        /// <see cref="EngineStatsSO.Price"/> 는 표시 전용이고 실제 설치비는
        /// <c>ResearchPrototypeModel.GetEngineInstallCost</c> 가 스탯 평균으로 최대 +20% 가산한다.
        ///
        /// 이벤트로 갱신할 수 없어 매 프레임 돌린다 — <c>builder.Changed</c> 는 부착 때 발생하지 않는다.
        /// </summary>
        // ponytail: 미션 할인(GetDiscountedInstallCost)이 걸린 동안 실제 한계비용은 이 값의 4/5 라
        // 최대 70 만큼 일찍 막힌다. 정확히 맞추려면 프리셋마다 CreateDesignEntry 로 재견적해야 하는데
        // 매 프레임 int[10] 열 벌이다 — 눈에 띄면 그때 견적으로 바꾼다.
        private void RefreshPresetAffordability(ResearchPrototypeModel model, int reserved, bool gate)
        {
            for (int i = 0; i < presetEntries.Count; i++)
            {
                PresetEntry entry = presetEntries[i];
                entry.SetAffordable(!gate || !entry.HasPresetId
                                    || model.Funds >= reserved + model.GetEngineInstallCost(entry.PresetId));
            }
        }

        /// <summary>
        /// 연구 쪽이 확정한 등급을 받아 결과 줄에 붙인다. 등급은 발사가 끝나야 계산되므로
        /// (<see cref="SimulationStageHost"/>) 패널이 스스로 알아낼 방법이 없다.
        /// </summary>
        internal void ShowLaunchResult(ResearchGrade grade)
        {
            launchGradeLabel = grade.ToString();
        }

        /// <summary>
        /// 좌측 발사 정보 패널을 매 프레임 채운다. 고도·거리는 최대치라 하강해도 줄지 않는다.
        /// <see cref="UpdateTopBar"/> 가 이미 이 프레임의 <see cref="placedParts"/> 를 채웠으므로 다시 수집하지 않는다.
        /// </summary>
        private void UpdateFlightInfo()
        {
            if (flightInfoPanel == null || !flightInfoPanel.gameObject.activeSelf) return;

            float fuel = 0f;
            for (int i = 0; i < placedParts.Count; i++) fuel += placedParts[i].Remaining;

            string objective = mission != null ? mission.Objective : string.Empty;
            int lineBreak = objective.IndexOf('\n');
            if (lineBreak >= 0) objective = objective[..lineBreak];

            string result = mission == null ? "-"
                : !mission.Finished ? "비행 중"
                : mission.Succeeded ? "성공" : "실패";
            if (mission != null && mission.Finished && !string.IsNullOrEmpty(launchGradeLabel))
            {
                result += $" · 등급 {launchGradeLabel}";
            }

            flightInfoValues[0].text = objective;
            flightInfoValues[1].text = $"{(mission != null ? mission.MaxAltitude : 0f):0.0} m";
            flightInfoValues[2].text = $"{(mission != null ? mission.MaxDistance : 0f):0.0} m";
            flightInfoValues[3].text = $"{(rocket != null ? rocket.TotalBurnSeconds : 0f):0.0} s";
            flightInfoValues[4].text = $"{(mission != null ? mission.HoldSeconds : 0f):0.0} s";
            flightInfoValues[5].text = $"{fuel:0.0} kg";
            flightInfoValues[6].text = result;
        }

        /// <summary>
        /// 단계 점을 상태에서 그린다. 끝난 단계는 채운 점, 지금 진행 중인 단계는 빈 점에 강조색,
        /// 이 미션이 쓰지 않는 단계는 흐리게 남긴다 — 다섯 칸은 미션과 무관하게 늘 같은 자리에 있다.
        /// </summary>
        private void UpdateStageStrip()
        {
            bool launched = rocket != null && rocket.Launched;
            int done = mission != null ? mission.Stage : 0;
            int used = mission != null ? mission.StageCount : stageDots.Length;

            for (int i = 0; i < stageDots.Length; i++)
            {
                bool unused = i >= used;
                bool complete = i < done;
                bool current = launched && i == done && !unused;

                stageDots[i].sprite = ArtemisCursor.IconSprite(complete
                    ? ArtemisCursor.Icon.StageDot
                    : ArtemisCursor.Icon.StageDotHollow);
                stageDots[i].color = unused ? StageIdleColor
                    : complete || current ? StageActiveColor : StagePendingColor;
                stageLabels[i].color = unused ? StageIdleColor
                    : complete || current ? Color.white : StagePendingColor;
                // stageLines[i] 는 점 i 오른쪽 선이다 — 다음 점까지 갔을 때 이어진 것으로 본다.
                if (stageLines[i] != null) stageLines[i].color = i + 1 < done ? StageActiveColor : StageIdleColor;
            }
        }

        private void RebuildPresetPanel()
        {
            if (testerInterface) return;
            HideStats();
            FillPresetPanel();
        }

        /// <summary>
        /// 연구가 만든 프리셋은 연구 화면 카드와 같은 이름(`엔진 01`~)을 쓴다 — 두 화면이
        /// 다른 이름을 부르면 연동됐다는 신호 자체가 사라진다. 저작 에셋은 `EngineStats_`
        /// 접두사만 떼어 목록 폭에 들어가게 한다.
        /// </summary>
        private static string DisplayName(EngineStatsSO preset)
        {
            if (TryGetPresetId(preset, out EnginePresetId presetId))
            {
                return ResearchFlowSession.GetOrCreate().Model.GetEnginePresetName(presetId);
            }

            const string Prefix = "EngineStats_";
            string name = preset.name;
            return name.StartsWith(Prefix) ? name[Prefix.Length..] : name;
        }

        /// <summary>
        /// 연구가 만든 런타임 사본만 연구 상태를 가진다 — <see cref="EngineStatsSO.CreateRuntimeCopy"/>
        /// 가 슬롯 인덱스를 채운다. 저작 에셋은 그 값이 `-1` 이라 여기서 걸러진다:
        /// `SimulationTest` 를 단독 재생할 때 연구 세션과 무관하게 프리셋 10개가 그대로 뜨는 이유다.
        /// </summary>
        private static bool TryGetPresetId(EngineStatsSO preset, out EnginePresetId presetId)
        {
            int index = preset.PresetIndex;
            presetId = (EnginePresetId)index;
            return index >= 0 && index < ResearchPrototypeModel.MaxEnginePresetCount;
        }

        /// <summary>연구가 아직 열지 않은 슬롯은 목록에 내지 않는다(GDD 07 §4).</summary>
        private static bool IsDeveloped(EngineStatsSO preset, ResearchPrototypeModel model)
        {
            return !TryGetPresetId(preset, out EnginePresetId presetId)
                   || model.IsEnginePresetUnlocked(presetId);
        }

        internal void ShowStats(EngineStatsSO preset, RectTransform anchor)
        {
            if (preset == null) return;

            statText.text =
                $"<b>{DisplayName(preset)}</b>\n" +
                (testerInterface ? string.Empty : $"가격 {preset.Price}\n") +
                $"연료 탱크 {preset.FuelCapacity:0} kg\n" +
                $"냉각 {preset.Cooling:0} °C/s\n" +
                $"최대 출력 {preset.MaxOutput:0} N\n" +
                $"점화 신뢰도 {preset.IgnitionReliability:0} %";

            // 물리값만 보면 이 숫자가 연구로 올라간 값인지 저작 기본값인지 구분이 안 된다.
            if (!testerInterface && TryGetPresetId(preset, out EnginePresetId presetId))
            {
                EnginePresetState state = ResearchFlowSession.GetOrCreate().Model.GetEnginePreset(presetId);
                statText.text += $"\n완성도 {state.Completion} / 최고 등급 "
                                 + (state.HasBestGrade ? state.BestGrade.ToString() : "-");
            }

            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            statBox.position = new Vector3(corners[2].x + 8f, (corners[1].y + corners[0].y) * 0.5f, 0f);
            statBox.gameObject.SetActive(true);
        }

        internal void HideStats() => statBox.gameObject.SetActive(false);

        public void BakeTesterInterface(RocketBuilder source)
        {
            testerInterface = true;
            builder = source;
            BuildInterface();
            FillPresetPanel();
            launchButton = CreateButton("LaunchButton", canvasRect, "발사", out _);
            RectTransform launchRect = (RectTransform)launchButton.transform;
            launchRect.anchorMin = launchRect.anchorMax = Vector2.right;
            launchRect.pivot = Vector2.right;
            launchRect.anchoredPosition = new Vector2(-16, 16);
            launchRect.sizeDelta = new Vector2(120, 44);
            Button reset = CreateButton("ResetButton", canvasRect, "다시 설계", out _);
            RectTransform resetRect = (RectTransform)reset.transform;
            resetRect.anchorMin = resetRect.anchorMax = Vector2.one;
            resetRect.pivot = Vector2.one;
            resetRect.anchoredPosition = new Vector2(-16, -16);
            resetRect.sizeDelta = new Vector2(120, 44);
        }

        /// <summary>
        /// 구운 화면의 자식들을 이름으로 다시 잡는다. <see cref="BuildInterface"/> 가 붙인 이름이
        /// 그대로 경로라, 프리팹에서 오브젝트 이름을 바꾸면 여기서 끊긴다.
        /// </summary>
        private void BindInterface()
        {
            canvasRect = (RectTransform)transform.Find(CanvasName);
            canvas = canvasRect.GetComponent<Canvas>();
            presetPanel = (RectTransform)canvasRect.Find("PresetPanel");
            statBox = (RectTransform)canvasRect.Find("StatBox");
            statText = statBox.GetComponentInChildren<TMP_Text>(true);
            partTools = (RectTransform)canvasRect.Find("PartTools");
            moveButton = partTools.Find("MoveButton").GetComponent<Button>();
            rotateButton = partTools.Find("RotateButton").GetComponent<Button>();

            // 저작 프리팹은 PiP 를 뷰포트 안에 두고, 테스터 화면은 캔버스 바로 아래에 둔다.
            launchPip = (RectTransform)(canvasRect.Find("LaunchPip") ?? canvasRect.Find("Viewport/LaunchPip"));
            pipImage = launchPip.GetComponentInChildren<RawImage>(true);
            pipLabel = launchPip.GetComponentInChildren<TMP_Text>(true);

            if (testerInterface)
            {
                launchButton = canvasRect.Find("LaunchButton").GetComponent<Button>();
                return;
            }

            BindMissionControl();
        }

        private void BindMissionControl()
        {
            Transform topBar = canvasRect.Find("TopBar");
            dateText = topBar.Find("DateCell/Date").GetComponent<TMP_Text>();
            fundsText = topBar.Find("FundsCell/Funds").GetComponent<TMP_Text>();

            viewport = (RectTransform)canvasRect.Find("Viewport");
            taskPanel = (RectTransform)viewport.Find("taskPanel");
            taskText = taskPanel.GetComponentInChildren<TMP_Text>(true);

            stageStrip = (RectTransform)canvasRect.Find("StageStrip");
            int count = LaunchMissionEvaluator.StageNames.Length;
            stageDots = new Image[count];
            stageLines = new Image[count];
            stageLabels = new TMP_Text[count];
            for (int i = 0; i < count; i++)
            {
                Transform cell = stageStrip.Find($"Stage_{i}");
                stageDots[i] = cell.Find("Dot").GetComponent<Image>();
                stageLabels[i] = cell.Find("Label").GetComponent<TMP_Text>();
                Transform line = cell.Find("Line/Fill");
                stageLines[i] = line != null ? line.GetComponent<Image>() : null;
            }

            Transform bottomBar = canvasRect.Find("BottomBar");
            missionText = bottomBar.Find("Mission").GetComponent<TMP_Text>();
            launchButton = bottomBar.Find("LaunchButton").GetComponent<Button>();
            destructButton = bottomBar.Find("SelfDestructButton").GetComponent<Button>();
            exitButton = bottomBar.Find("ExitButton").GetComponent<Button>();

            flightInfoPanel = (RectTransform)canvasRect.Find("FlightInfoPanel");
            flightInfoValues = new TMP_Text[FlightInfoLabels.Length];
            for (int i = 0; i < FlightInfoLabels.Length; i++)
            {
                flightInfoValues[i] = flightInfoPanel.Find($"Row_{FlightInfoLabels[i]}/Value").GetComponent<TMP_Text>();
            }
        }

        /// <summary>
        /// 구운 화면이든 코드로 지은 화면이든 똑같이 필요한 뒷정리. 버튼 리스너는 씬 객체를,
        /// 커서 아이콘은 런타임에 그린 텍스처를 가리켜 어느 쪽도 프리팹에 직렬화되지 않는다.
        /// </summary>
        private void WireInterface()
        {
            SetIcon(moveButton, ArtemisCursor.Icon.Move);
            SetIcon(rotateButton, ArtemisCursor.Icon.Rotate);
            moveButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Move));
            rotateButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Rotate));
            launchPip.GetComponent<Button>().onClick.AddListener(builder.ToggleLaunchView);

            if (testerInterface)
            {
                launchButton.onClick.AddListener(builder.RequestLaunch);
                DesignStageTester tester = FindFirstObjectByType<DesignStageTester>();
                canvasRect.Find("ResetButton").GetComponent<Button>().onClick.AddListener(tester.ReturnToDesign);
                foreach (DesignTesterPresetEntry entry in presetPanel.GetComponentsInChildren<DesignTesterPresetEntry>(true))
                    entry.Bind(this);
                return;
            }

            FillPresetPanel();

            stageHost = FindFirstObjectByType<SimulationStageHost>();
            launchButton.onClick.AddListener(builder.RequestLaunch);
            destructButton.onClick.AddListener(() => mission?.SelfDestruct());
            exitButton.onClick.AddListener(SimulationStageHost.CloseDesignStage);
        }

        private static void SetIcon(Button button, ArtemisCursor.Icon icon)
        {
            Transform child = button.transform.Find("Icon");
            if (child != null) child.GetComponent<Image>().sprite = ArtemisCursor.IconSprite(icon);
        }

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
            private TMP_Text label;
            private bool affordable = true;

            /// <summary>연구가 만든 런타임 프리셋만 슬롯 번호를 가진다 — 저작 에셋은 설치비가 없다.</summary>
            public bool HasPresetId { get; private set; }

            public EnginePresetId PresetId { get; private set; }

            public void Bind(RocketDesignUI ui, EngineStatsSO stats, Image image, TMP_Text text)
            {
                owner = ui;
                preset = stats;
                background = image;
                label = text;
                HasPresetId = TryGetPresetId(stats, out EnginePresetId id);
                PresetId = id;
            }

            /// <summary>자금이 되는지에 따라 카드를 켜고 끈다. 매 프레임 불리므로 값이 같으면 즉시 빠진다.</summary>
            public void SetAffordable(bool value)
            {
                if (affordable == value) return;

                affordable = value;
                background.color = Idle;
                label.color = value ? Color.white : TintDisabled;
            }

            /// <summary>가리키지 않을 때의 배경색. 못 사는 카드는 흐린 쪽이 기본이다.</summary>
            private Color Idle => affordable ? TintIdle : TintDisabled;

            public void OnPointerEnter(PointerEventData eventData)
            {
                // 못 사는 카드도 스탯은 보여 준다 — 왜 못 사는지는 가격을 봐야 판단이 선다.
                if (affordable)
                {
                    background.color = TintActive;
                    ArtemisCursor.Request(ArtemisCursor.Visual.Hover);
                }

                owner.ShowStats(preset, (RectTransform)transform);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                background.color = Idle;
                owner.HideStats();
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (!affordable) return;

                background.color = Idle;
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

        /// <summary>기본 UI 아트를 입힌 패널. 9-슬라이스라 어떤 크기로 늘려도 테두리 두께가 유지된다.</summary>
        private static RectTransform CreateArtPanel(string name, Transform parent, int spriteIndex)
        {
            RectTransform panel = CreateGroup(name, parent);
            Skin(panel.gameObject.AddComponent<Image>(), spriteIndex);
            return panel;
        }

        private static void Skin(Image image, int spriteIndex)
        {
            Sprite sprite = Art(spriteIndex);
            image.color = TintIdle;
            // 프리팹 쪽 UI 는 인스펙터에서 같은 머티리얼을 물린다 — 이 화면만 기본 UI 머티리얼로
            // 남으면 껍데기가 다르게 보인다. 못 읽어도 스프라이트는 그대로 입힌다.
            artMaterial ??= Resources.Load<Material>(ArtMaterialName);
            if (artMaterial != null) image.material = artMaterial;
            if (sprite == null) return; // 시트를 못 읽으면 흰 사각형으로 남는다 — 화면이 사라지는 것보다 낫다

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        private static Sprite Art(int index)
        {
            artSprites ??= Resources.LoadAll<Sprite>(ArtSheetName);
            string wanted = $"{ArtSheetName}_{index}";
            for (int i = 0; i < artSprites.Length; i++)
            {
                if (artSprites[i].name == wanted) return artSprites[i];
            }

            return null;
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
            RectTransform rect = CreateArtPanel(name, parent, ButtonSprite);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            label = CreateText("Label", rect, 16, FontStyles.Bold, text);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Fill((RectTransform)label.transform, 0f);
            return button;
        }

        private static Button CreateButton(string name, Transform parent, Sprite icon)
        {
            RectTransform rect = CreateArtPanel(name, parent, ButtonSprite);
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
