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

        // 관제 배치의 치수(캔버스 단위, 기준 해상도 1280x720). 하나만 고치면 상·하단 바, 좌측 패널,
        // 뷰포트, 단계 스테퍼가 함께 따라온다 — 같은 숫자를 여러 곳에 흩어 두면 어긋난다.
        //
        // 미션 컨트롤에서는 패널을 화면 가장자리와 서로에게 딱 붙인다. 카메라가 뷰포트 사각형만
        // 그리므로 그 밖은 아무도 지우지 않고 이전 프레임 픽셀이 그대로 남는다 — 여백을 두면 그 틈으로
        // 찌꺼기가 보인다. 오버레이 캔버스는 3D 뷰 위에 그려지므로 전체 화면 배경으로 덮을 수도 없다.
        private const float Margin = 16f; // SimulationTest 단독 재생 전용
        private const float BarHeight = 64f;
        private const float DockWidth = 200f;
        private const float ViewportLeft = DockWidth;
        private const float StripHeight = 32f;
        private const float StageDotSize = 18f;
        private const float CellGap = 4f;      // 상단 바 칸 사이 간격
        private const float InfoRowHeight = 34f; // 발사 정보 한 줄

        // 하단 바 오른쪽 끝 복귀 버튼과 발사·자폭 버튼의 가로 폭. 미션 텍스트의 오른쪽 여백도 여기서 나온다.
        private const float ExitButtonWidth = 96f;
        private const float ActionButtonWidth = 120f;
        private const float BarButtonHeight = 40f;

        // 프로젝트 기본 UI 아트 시트. 연구·메뉴 화면은 에디터에서 프리팹에 구워 넣지만
        // (ResearchUiArtApplicator, MenuUiPrefabBuilder) 이 화면은 런타임에 만들어지므로
        // 같은 시트를 Resources 에서 직접 읽는다 — 두 화면이 다른 껍데기를 쓰면 같은 게임으로 안 읽힌다.
        private const string ArtSheetName = "engine_ui_01";
        private const int PanelSprite = 0;  // 패널·바 배경
        private const int BoxSprite = 4;    // 작은 상자(스탯 툴팁)
        private const int ButtonSprite = 5; // 버튼
        private const int CardSprite = 6;   // 목록 카드(프리셋 한 줄)
        private static Sprite[] artSprites;

        // 아트를 입힌 그래픽은 색이 아니라 틴트로 상태를 알린다 — 스프라이트에 어두운 색을 곱하면 그림이 죽는다.
        private static readonly Color TintIdle = Color.white;
        private static readonly Color TintActive = new(0.55f, 0.95f, 1f, 1f);

        // 단계 스테퍼는 시트에 맞는 조각이 없어 색만 쓴다. 강조색은 ArtemisCursor 의 시안과 같은 자리다.
        private static readonly Color StageActiveColor = new(0.11f, 0.91f, 0.93f, 1f);
        private static readonly Color StagePendingColor = new(0.30f, 0.38f, 0.46f, 1f);
        private static readonly Color StageIdleColor = new(0.22f, 0.26f, 0.31f, 1f);

        // 미션 컨트롤 모드에서만 만드는 것들. 01_Main 위에 얹었을 때(SimulationStageHost)만 켜지고,
        // SimulationTest 씬을 직접 재생하면 예전 그대로 좌측 패널만 뜬다.
        private bool missionControl;
        [SerializeField] private bool testerInterface;
        private Rocket rocket;
        private RectTransform viewport;
        private TMP_Text dateText;
        private TMP_Text fundsText;
        private TMP_Text pendingEffectsText;
        private RectTransform pendingEffectsPanel;
        private readonly int[] installedPresetCounts = new int[ResearchPrototypeModel.MaxEnginePresetCount];
        private TMP_Text missionText;
        private RectTransform stageStrip;
        private Image[] stageDots;
        private Image[] stageLines;
        private TMP_Text[] stageLabels;
        private SimulationStageHost stageHost;
        private Button launchButton;
        private Button destructButton;
        private LaunchMissionController mission;
        private readonly List<RocketPart> placedParts = new();
        private readonly Vector3[] viewportCorners = new Vector3[4];

        private RocketBuilder builder;
        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform presetPanel;
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

            if (testerInterface) BindTesterInterface();
            else BuildInterface();
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
            if (testerInterface)
            {
                launchButton.gameObject.SetActive(!rocket.Launched);
                RefreshTools();
            }
            if (missionControl)
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
        /// 관제 배치: 상단 정보 바, 좌측 프리셋 패널(BuildPresetPanel), 그 오른쪽을 꽉 채우는 3D 뷰포트,
        /// 뷰포트 아래 비행 단계 스테퍼, 미션 문구와 버튼을 담은 하단 바.
        /// 뷰포트는 <b>Image 가 없는 빈 RectTransform</b> 이어야 한다 — 그래픽을 붙이면
        /// <see cref="EventSystem.IsPointerOverGameObject"/> 가 뷰포트 전체를 UI 로 판정해서
        /// <see cref="RocketBuilder"/> 의 3D 입력이 통째로 막힌다. 같은 이유로 전체 화면 배경 패널도 만들지 않는다.
        /// </summary>
        private void BuildMissionControl(RectTransform canvasTransform)
        {
            mission = rocket != null ? rocket.GetComponent<LaunchMissionController>() : null;
            stageHost = FindFirstObjectByType<SimulationStageHost>();

            BuildTopBar(canvasTransform);
            BuildFlightInfoPanel(canvasTransform);

            pendingEffectsPanel = CreateArtPanel("PendingLaunchEffects", canvasTransform, PanelSprite);
            pendingEffectsPanel.anchorMin = new Vector2(0f, 1f);
            pendingEffectsPanel.anchorMax = Vector2.one;
            pendingEffectsPanel.pivot = new Vector2(0.5f, 1f);
            pendingEffectsText = CreateText("Effects", pendingEffectsPanel, 16, FontStyles.Normal, string.Empty);
            pendingEffectsText.raycastTarget = false;
            pendingEffectsText.textWrappingMode = TextWrappingModes.Normal;
            Fill((RectTransform)pendingEffectsText.transform, 12f);

            // 카메라 뷰포트의 유일한 원천. 정규화 상수를 따로 두면 CanvasScaler 가 늘어나는 비율에서
            // 좌측 패널과 어긋난다 — 매 프레임 이 사각형을 읽어 카메라에 먹인다.
            viewport = CreateGroup("Viewport", canvasTransform);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(ViewportLeft, BarHeight + StripHeight);
            viewport.offsetMax = new Vector2(0f, -BarHeight);

            BuildStageStrip(canvasTransform);
            BuildBottomBar(canvasTransform);

            // 작은 화면은 뷰 안 우하단에 앉힌다. 캔버스에 그대로 두면 하단 바 뒤로 들어간다 —
            // 뷰포트에는 Graphic 이 없으므로 자식 버튼을 붙여도 위의 입력 규칙은 깨지지 않는다.
            launchPip.SetParent(viewport, false);
            launchPip.anchoredPosition = new Vector2(-12f, 12f);

            // 떠다니는 두 가지는 바보다 나중에 그려야 한다 — 오버레이 캔버스는 형제 순서가 곧 그리는 순서다.
            statBox.SetAsLastSibling();
            partTools.SetAsLastSibling();
        }

        /// <summary>
        /// 제목·날짜·잔여 자금 세 칸. 값은 <see cref="UpdateTopBar"/> 가 매 프레임 채운다.
        /// 바 자체는 Graphic 이 없는 껍데기고 칸마다 패널을 따로 깐다 — 한 장 위에 글자만 셋 얹으면
        /// 어디부터 어디까지가 한 항목인지 안 읽힌다. 바는 뷰포트 위쪽 바깥이라 Image 를 빼도 3D 입력과 무관하다.
        /// </summary>
        private void BuildTopBar(RectTransform canvasTransform)
        {
            RectTransform bar = CreateGroup("TopBar", canvasTransform);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -BarHeight);
            bar.offsetMax = Vector2.zero;

            TMP_Text title = CreateText("Title", DockCell("TitleCell", bar, 0f, 0.32f), 20, FontStyles.Bold, "ARTEMIS CONTROL");
            title.raycastTarget = false;
            Fill((RectTransform)title.transform, 10f);

            dateText = CreateText("Date", DockCell("DateCell", bar, 0.32f, 0.62f), 16, FontStyles.Bold, string.Empty);
            dateText.alignment = TextAlignmentOptions.Center;
            dateText.raycastTarget = false;
            Fill((RectTransform)dateText.transform, 10f);

            fundsText = CreateText("Funds", DockCell("FundsCell", bar, 0.62f, 1f), 16, FontStyles.Bold, string.Empty);
            fundsText.alignment = TextAlignmentOptions.Right;
            fundsText.raycastTarget = false;
            Fill((RectTransform)fundsText.transform, 10f);
        }

        /// <summary>상단 바의 한 칸. 칸 사이 <see cref="CellGap"/> 만큼 벌려 패널 경계가 보이게 한다.</summary>
        private static RectTransform DockCell(string name, RectTransform bar, float min, float max)
        {
            RectTransform cell = CreateArtPanel(name, bar, PanelSprite);
            cell.GetComponent<Image>().raycastTarget = false;
            cell.anchorMin = new Vector2(min, 0f);
            cell.anchorMax = new Vector2(max, 1f);
            cell.offsetMin = new Vector2(min > 0f ? CellGap : 0f, 0f);
            cell.offsetMax = new Vector2(max < 1f ? -CellGap : 0f, 0f);
            return cell;
        }

        /// <summary>현재 미션 문구와 발사·자폭·복귀 버튼. 발사 버튼과 자폭 버튼은 같은 자리를 교대로 쓴다.</summary>
        private void BuildBottomBar(RectTransform canvasTransform)
        {
            RectTransform bar = CreateArtPanel("BottomBar", canvasTransform, PanelSprite);
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = new Vector2(0f, BarHeight);

            missionText = CreateText("Mission", bar, 14, FontStyles.Normal, string.Empty);
            missionText.raycastTarget = false;
            var missionRect = (RectTransform)missionText.transform;
            missionRect.anchorMin = Vector2.zero;
            missionRect.anchorMax = Vector2.one;
            missionRect.offsetMin = new Vector2(12f, 6f);
            missionRect.offsetMax = new Vector2(-(ExitButtonWidth + ActionButtonWidth + 36f), -6f);

            launchButton = CreateButton("LaunchButton", bar, "발사", out _);
            PlaceBarButton((RectTransform)launchButton.transform, -12f, ActionButtonWidth);
            launchButton.onClick.AddListener(builder.RequestLaunch);

            destructButton = CreateButton("SelfDestructButton", bar, "자폭", out _);
            PlaceBarButton((RectTransform)destructButton.transform, -12f, ActionButtonWidth);
            destructButton.onClick.AddListener(() => mission?.SelfDestruct());

            BuildExitButton(bar);
        }

        /// <summary>
        /// 비행 단계 스테퍼. 이름과 단계 수는 <see cref="LaunchMissionEvaluator.StageNames"/> 하나에서만 온다 —
        /// UI 가 같은 문자열을 복제하면 미션 규칙이 바뀔 때 화면만 낡은 채로 남는다.
        /// 연결선은 각 칸이 자기 오른쪽에 하나씩 들고 있어(마지막 칸만 없다) 라벨 위를 가로지르지 않는다.
        /// </summary>
        private void BuildStageStrip(RectTransform canvasTransform)
        {
            RectTransform strip = CreateArtPanel("StageStrip", canvasTransform, PanelSprite);
            strip.GetComponent<Image>().raycastTarget = false;
            stageStrip = strip;
            strip.anchorMin = Vector2.zero;
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.offsetMin = new Vector2(ViewportLeft, BarHeight);
            strip.offsetMax = new Vector2(0f, BarHeight + StripHeight);

            var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            int count = LaunchMissionEvaluator.StageNames.Length;
            stageDots = new Image[count];
            stageLines = new Image[count];
            stageLabels = new TMP_Text[count];

            for (int i = 0; i < count; i++)
            {
                RectTransform cell = CreateGroup($"Stage_{i}", strip);
                var cellLayout = cell.gameObject.AddComponent<HorizontalLayoutGroup>();
                cellLayout.spacing = 6f;
                cellLayout.childAlignment = TextAnchor.MiddleLeft;
                cellLayout.childControlWidth = true;
                cellLayout.childControlHeight = true;
                cellLayout.childForceExpandWidth = false;

                stageDots[i] = CreateStageDot(cell);

                TMP_Text label = CreateText("Label", cell, 12, FontStyles.Bold,
                    LaunchMissionEvaluator.StageNames[i]);
                label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                stageLabels[i] = label;

                // 연결선은 점·라벨 뒤에 온다. 앞에 두면 남는 폭을 먼저 먹어 점과 라벨이 칸 오른쪽 끝으로
                // 밀리고, 마지막 칸의 라벨이 화면 밖으로 나간다. 마지막 칸은 이을 다음 점이 없다.
                if (i < count - 1) stageLines[i] = CreateStageLine(cell);
            }
        }

        /// <summary>칸의 남는 폭을 전부 먹는 가로줄. 두께는 부모 높이와 무관하게 2 로 고정한다.</summary>
        private static Image CreateStageLine(RectTransform cell)
        {
            RectTransform holder = CreateGroup("Line", cell);
            LayoutElement element = holder.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.minWidth = 0f;
            element.preferredWidth = 0f;

            RectTransform line = CreatePanel("Fill", holder, StageIdleColor);
            line.anchorMin = new Vector2(0f, 0.5f);
            line.anchorMax = new Vector2(1f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.sizeDelta = new Vector2(0f, 2f);

            Image image = line.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateStageDot(RectTransform cell)
        {
            RectTransform dot = CreateGroup("Dot", cell);
            dot.gameObject.AddComponent<LayoutElement>().preferredWidth = StageDotSize;

            var image = dot.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// 설계 화면에서 연구 화면으로 나가는 유일한 문. 미션 컨트롤 모드에서만 만든다 —
        /// SimulationTest 를 단독 재생할 때는 돌아갈 연구 화면 자체가 없다.
        /// </summary>
        private static void BuildExitButton(RectTransform bottomBar)
        {
            RectTransform rect = CreateArtPanel("ExitButton", bottomBar, ButtonSprite);
            PlaceBarButton(rect, -(ActionButtonWidth + 24f), ExitButtonWidth);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(SimulationStageHost.CloseDesignStage);

            TMP_Text label = CreateText("Label", rect, 15, FontStyles.Bold, "연구 화면");
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            Fill((RectTransform)label.transform, 4f);
        }

        /// <summary>바 오른쪽 끝에서 <paramref name="x"/> 만큼 안쪽으로 붙인다(음수가 안쪽).</summary>
        private static void PlaceBarButton(RectTransform rect, float x, float width)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, BarButtonHeight);
        }

        /// <summary>상단 바 안에서 가로 구간(0~1)을 잡아 앉힌다.</summary>
        private void BuildPresetPanel(RectTransform canvasTransform)
        {
            if (presetPanel != null)
            {
                Destroy(presetPanel.gameObject);
            }

            presetPanel = CreateArtPanel("PresetPanel", canvasTransform, PanelSprite);
            presetPanel.SetAsFirstSibling();
            presetPanel.anchorMin = new Vector2(0f, 0f);
            presetPanel.anchorMax = new Vector2(0f, 1f);
            presetPanel.pivot = new Vector2(0f, 0.5f);
            // 미션 컨트롤 모드에서는 상·하단 바 사이에 딱 끼우고, SimulationTest 단독 재생에서는 예전처럼 띄운다.
            float edge = missionControl ? 0f : Margin;
            float dock = missionControl ? BarHeight : Margin;
            presetPanel.offsetMin = new Vector2(edge, dock);
            presetPanel.offsetMax = new Vector2(edge + DockWidth, -dock);

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
                    row.gameObject.AddComponent<DesignTesterPresetEntry>().SetPreset(preset);
                else
                    row.gameObject.AddComponent<PresetEntry>().Bind(this, preset, row.GetComponent<Image>());
            }

            if (shown == 0)
            {
                CreateText("Empty", presetPanel, 13, FontStyles.Normal,
                    "프리셋이 없다.\nRocketBuilder 의 Preset Library 에\nEnginePresetLibrarySO 를 연결하라.");
            }
        }

        /// <summary>
        /// 발사 뒤 좌측 도크를 넘겨받는 비행 정보 패널. 프리셋 패널과 같은 자리·같은 폭이라
        /// 발사 순간 뷰포트 폭이 흔들리지 않는다. 프리셋 목록을 부수고 다시 짓는 대신 둘을 번갈아 켠다.
        /// </summary>
        private void BuildFlightInfoPanel(RectTransform canvasTransform)
        {
            flightInfoPanel = CreateArtPanel("FlightInfoPanel", canvasTransform, PanelSprite);
            flightInfoPanel.SetAsFirstSibling();
            flightInfoPanel.anchorMin = new Vector2(0f, 0f);
            flightInfoPanel.anchorMax = new Vector2(0f, 1f);
            flightInfoPanel.pivot = new Vector2(0f, 0.5f);
            flightInfoPanel.offsetMin = new Vector2(0f, BarHeight);
            flightInfoPanel.offsetMax = new Vector2(DockWidth, -BarHeight);

            var layout = flightInfoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            CreateText("Title", flightInfoPanel, 18, FontStyles.Bold, "발사 정보");

            string[] labels = { "미션", "최고 고도", "최대 거리", "총 연소", "체류", "남은 연료", "결과" };
            flightInfoValues = new TMP_Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                flightInfoValues[i] = CreateInfoRow(labels[i]);
            }

            flightInfoPanel.gameObject.SetActive(false);
        }

        /// <summary>라벨과 값을 좌우로 나눠 담은 한 줄. 값 텍스트만 돌려준다 — 갱신은 그쪽만 한다.</summary>
        private TMP_Text CreateInfoRow(string label)
        {
            RectTransform row = CreateArtPanel($"Row_{label}", flightInfoPanel, CardSprite);
            row.GetComponent<Image>().raycastTarget = false;
            // 가로 레이아웃 그룹은 자식의 flexibleHeight 를 부모에게 그대로 올린다 — 0 으로 눌러 두지 않으면
            // 줄이 패널 높이를 나눠 먹어 카드 일곱 장이 늘어난다.
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = InfoRowHeight;
            rowLayout.flexibleHeight = 0f;

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 0, 0);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            TMP_Text caption = CreateText("Label", row, 12, FontStyles.Bold, label);
            caption.raycastTarget = false;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.color = StagePendingColor;
            caption.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;

            TMP_Text value = CreateText("Value", row, 13, FontStyles.Bold, string.Empty);
            value.raycastTarget = false;
            value.alignment = TextAlignmentOptions.Right;
            value.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return value;
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

            moveButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Move));
            rotateButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Rotate));

            partTools.gameObject.SetActive(false);
        }

        /// <summary>
        /// 발사 후에만 뜨는 작은 화면. 두 번째 카메라가 RenderTexture 에 그린 것을 그대로 얹고,
        /// 누르면 큰 화면과 역할이 바뀐다(<see cref="RocketBuilder.ToggleLaunchView"/>).
        /// 여기서는 캔버스 우하단에 두고, 미션 컨트롤 모드는 <see cref="BuildMissionControl"/> 에서
        /// 부모를 <c>Viewport</c> 로 옮겨 3D 뷰 안에 앉힌다.
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
            button.onClick.AddListener(builder.ToggleLaunchView);

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
        /// 뷰포트 RectTransform 을 카메라의 정규화 사각형으로 옮긴다. 오버레이 캔버스의
        /// <see cref="RectTransform.GetWorldCorners"/> 는 화면 픽셀이라(StatBox 도 같은 가정) 화면 크기로
        /// 나누기만 하면 된다. 창 크기나 화면 비율이 바뀌어도 한 프레임 뒤에 따라온다.
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
            string pending = launched ? string.Empty : model.PendingLaunchEffectsText;
            pendingEffectsPanel.gameObject.SetActive(!string.IsNullOrEmpty(pending));
            float effectsHeight = 0f;
            if (!string.IsNullOrEmpty(pending))
            {
                pendingEffectsText.text = "남은 이벤트 효과\n" + pending;
                float width = Mathf.Max(100f, canvasRect.rect.width - ViewportLeft - 24f);
                effectsHeight = pendingEffectsText.GetPreferredValues(pendingEffectsText.text, width, 0f).y + 24f;
                pendingEffectsPanel.offsetMin = new Vector2(ViewportLeft, -BarHeight - effectsHeight);
                pendingEffectsPanel.offsetMax = new Vector2(0f, -BarHeight);
            }
            viewport.offsetMax = new Vector2(0f, -BarHeight - effectsHeight);
            dateText.text = $"{model.Year}.{model.Quarter}분기   ·   남은 {model.RemainingTurns}분기";
            fundsText.text = $"잔여 자금 {model.Funds - installed:N0}"
                             + $"   (설치 {installed:N0} · 분기 +{model.QuarterlyFunding:N0})";

            missionText.text = launched && mission != null
                ? mission.Objective + "\n" + mission.Status
                : stageHost != null && !string.IsNullOrEmpty(stageHost.LaunchMessage)
                    ? stageHost.LaunchMessage
                    : mission != null ? mission.Objective : string.Empty;
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
            BuildPresetPanel((RectTransform)canvas.transform);
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

        private void BindTesterInterface()
        {
            canvasRect = (RectTransform)transform.Find("RocketDesignCanvas");
            canvas = canvasRect.GetComponent<Canvas>();
            presetPanel = (RectTransform)canvasRect.Find("PresetPanel");
            statBox = (RectTransform)canvasRect.Find("StatBox");
            statText = statBox.GetComponentInChildren<TMP_Text>(true);
            partTools = (RectTransform)canvasRect.Find("PartTools");
            moveButton = partTools.Find("MoveButton").GetComponent<Button>();
            rotateButton = partTools.Find("RotateButton").GetComponent<Button>();
            moveButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Move));
            rotateButton.onClick.AddListener(() => builder.SetMode(RocketBuilder.EditMode.Rotate));
            launchPip = (RectTransform)canvasRect.Find("LaunchPip");
            pipImage = launchPip.GetComponentInChildren<RawImage>(true);
            pipLabel = launchPip.GetComponentInChildren<TMP_Text>(true);
            launchPip.GetComponent<Button>().onClick.AddListener(builder.ToggleLaunchView);
            launchButton = canvasRect.Find("LaunchButton").GetComponent<Button>();
            launchButton.onClick.AddListener(builder.RequestLaunch);
            DesignStageTester tester = FindFirstObjectByType<DesignStageTester>();
            canvasRect.Find("ResetButton").GetComponent<Button>().onClick.AddListener(tester.ReturnToDesign);
            foreach (DesignTesterPresetEntry entry in presetPanel.GetComponentsInChildren<DesignTesterPresetEntry>(true))
                entry.Bind(this);
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

            public void Bind(RocketDesignUI ui, EngineStatsSO stats, Image image)
            {
                owner = ui;
                preset = stats;
                background = image;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                background.color = TintActive;
                ArtemisCursor.Request(ArtemisCursor.Visual.Hover);
                owner.ShowStats(preset, (RectTransform)transform);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                background.color = TintIdle;
                owner.HideStats();
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                background.color = TintIdle;
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
