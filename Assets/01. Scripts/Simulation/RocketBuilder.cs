using System;
using System.Collections.Generic;
using Border.Core;
using Border.Audio;
using Border.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Simulation
{
    /// <summary>
    /// 씬 컨트롤러: 좌클릭 부품 부착·선택, 우클릭 궤도 회전, 발사 키.
    /// 선택한 부품은 유니티 씬 뷰처럼 축 구속 기즈모로 옮기고 돌린다 — 자유 조작이 아니라
    /// 부품 로컬 축 하나에 묶인다.
    /// 좌측 프리셋 패널(<see cref="RocketDesignUI"/>)이 이 컴포넌트의 드래그·선택 상태를 그대로 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RocketBuilder : MonoBehaviour
    {
        /// <summary>선택한 부품을 버튼으로 조정하는 모드. <see cref="RocketDesignUI"/> 가 전환한다.</summary>
        public enum EditMode
        {
            None,
            Move,
            Rotate,
        }

        [SerializeField] private Camera cam;
        [SerializeField] private Rocket rocket;

        [Header("Preset panel")]
        [Tooltip("프리셋에서 꺼낼 때 복제할 엔진 프리팹. Assets/03. Prefabs/Simulation/RocketEngine.prefab")]
        [SerializeField] private RocketPart enginePrefab;
        [Tooltip("좌측 패널에 띄울 엔진 프리셋 목록.")]
        [SerializeField] private EnginePresetLibrarySO presetLibrary;

        [Header("Cinemachine")]
        [Tooltip("설계 단계 카메라. 이 트랜스폼이 궤도 회전·줌의 결과를 받는다.")]
        [SerializeField] private CinemachineCamera designCam;
        [Tooltip("발사 뒤 카메라. 발사 순간 배치되고 그 뒤로는 로켓 높이만 따라간다.")]
        [SerializeField] private CinemachineCamera launchCam;
        [SerializeField] private float launchBlendSeconds = 1.5f;

        // 아래 거리·고도는 전부 월드 유닛이다. SkyEnvironment 의 km 는 worldMetersPerUnit(250) 을 먹인
        // 연출용 표시라 여기 숫자와 단위가 다르다 — 섞어 읽으면 후퇴가 비행 내내 걸리지 않는다.
        [Header("Launch views")]
        [Tooltip("추적 뷰가 로켓에서 떨어져 있는 거리(유닛). 12 면 로켓이 화면 높이의 약 30% 를 채운다.")]
        [SerializeField] private float chaseDistance = 12f;
        [Tooltip("추적 카메라가 내려갈 수 있는 최저 높이(유닛). 수면(-8.9)보다 위여야 한다 — 로켓이 가라앉아도 카메라는 물 밖에 남는다.")]
        [SerializeField] private float cameraFloorY = -6f;
        [Tooltip("후퇴 뷰가 고도 0 에서 로켓과 떨어진 거리(유닛).")]
        [SerializeField] private float pullbackNearDistance = 40f;
        [Tooltip("고도 1 유닛마다 후퇴 뷰가 더 물러나는 거리(유닛). 3 이면 고도 100 에서 340 까지 빠진다.")]
        [SerializeField] private float pullbackGrowth = 3f;
        [Tooltip("후퇴 뷰 거리의 상한(유닛). far clip(1000) 안에 둔다.")]
        [SerializeField] private float pullbackFarDistance = 500f;
        [Tooltip("작은 화면(PiP)의 렌더 타깃 해상도. 표시 전용이라 낮아도 된다.")]
        [SerializeField] private Vector2Int pipResolution = new(320, 180);

        [Header("Trajectory trail")]
        [Tooltip("궤적 점에 쓸 머티리얼. 비우면 URP Unlit 을 런타임에 찾는다. 기본은 Sky/Star.mat.")]
        [SerializeField] private Material trailMaterial;
        [Tooltip("점 사이 간격(유닛). 로켓이 이만큼 움직일 때마다 점 하나가 남는다.")]
        [SerializeField] private float trailDotSpacing = 3f;
        [Tooltip("점 하나가 차지할 화면 높이 비율. 0.015 면 720p 에서 약 11 px — 카메라가 아무리 멀어져도 이 크기다.")]
        [SerializeField] private float trailDotScreenSize = 0.015f;
        [Tooltip("궤적 레이어. 이 레이어를 후퇴 뷰를 맡은 카메라만 그린다.")]
        [SerializeField] private int trailLayer = 8; // Trajectory

        [Header("Launch shake")]
        // ponytail: 설정의 카메라 흔들림 토글과는 아직 연결하지 않았다. SettingsSO.IsCameraShakeOn 은
        // 있지만 changeCameraShakeEvent 채널 에셋이 프로젝트에 없고 SettingsSystem 쪽도 비어 있다 —
        // 채널이 생기면 BoolEventChannelSO 를 하나 받아 여기서 구독한다.
        [Tooltip("연소 중 카메라가 흔들리는 폭. 화면 높이 비율이라 추적 뷰와 후퇴 뷰가 같은 세기로 보인다. 0 이면 끈다.")]
        [SerializeField] private float shakeScreenAmplitude = 0.006f;
        [Tooltip("흔들림 속도(Hz 에 가깝다). 낮을수록 저주파로 묵직하게 흔들린다.")]
        [SerializeField] private float shakeFrequency = 14f;

        /// <summary>흔들림 Y 축이 쓰는 Perlin 행. X 축(0)과 떨어져 있기만 하면 되는 임의값이다.</summary>
        private const float YNoiseRow = 137.3f;

        [Header("Orbit camera")]
        [SerializeField] private float orbitSensitivity = 0.3f; // 도/픽셀
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float zoomSpeed = 0.01f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 60f;

        [Header("Part gizmo")]
        [Tooltip("기즈모 반지름을 화면 절반 높이 대비 비율로 정한다. 0.2 면 화면 높이의 10%.")]
        [SerializeField] private float gizmoScreenSize = 0.2f;
        [Tooltip("핸들이 잡혔다고 칠 커서 거리(px). 링 선은 화면에서 2 px 남짓이라 그보다 한참 커야 한다.")]
        [SerializeField] private float handleGrabPixels = 22f;
        [Tooltip("커서 아래 핸들을 굵게 만드는 배율. 어디를 눌러야 잡히는지 손으로 찾지 않게 한다.")]
        [SerializeField] private float handleHoverWidth = 2.5f;

        [Header("Alignment guides")]
        [Tooltip("부품을 표면에 얹을 때 안으로 파묻을 비율. 0 이면 콜라이더 상자가 표면에 정확히 닿고, "
               + "1 이면 피봇이 표면에 놓인다. 상자 bounds 가 노즐 벨 최대폭이라 0 은 부품을 띄운다 — "
               + "0.4 가 벨 폭만큼 파묻어 옆면·마개 모두 얹힌 것처럼 보이는 값이다.")]
        [SerializeField, Range(0f, 1f)] private float partSeatSink = 0.4f;
        [Tooltip("본체 실루엣 밖에서도 부착으로 칠 여유. 본체 반지름의 배수다 — 이 값이 로켓 위·아래 "
               + "빈 곳을 부착 범위로 넣어, 콜라이더에 맞아야만 붙던 시절 못 잡던 꼭대기·바닥 마개를 잡게 한다.")]
        [SerializeField] private float attachReachRadii = 1.5f;
        [SerializeField] private float heightTolerance = 0.25f; // m
        [SerializeField] private float azimuthTolerance = 20f;  // 도
        [SerializeField] private float rotationSnapStep = 45f;      // 도
        [SerializeField] private float rotationSnapTolerance = 7f;  // 도
        [SerializeField] private float guideHalfLength = 2.2f;  // 세로선 절반 길이(m)
        [SerializeField] private float guideWidth = 0.02f;
        [SerializeField] private Color guideColor = new(0.2f, 0.9f, 1f);
        [SerializeField] private Material guideMaterial; // 비우면 URP Unlit 을 런타임에 찾는다

        private const int RingSegments = 32;
        private const float MinRadius = 1e-3f; // 축 위에서는 방위각이 정의되지 않는다
        private const float DragSlopPixels = 4f;

        // 비행은 약 13초다. 넉넉히 잡아 궤적이 도중에 사라지지 않게만 한다 — Mathf.Infinity 는
        // 파티클 수명에 그대로 넣을 값이 아니다.
        private const float TrailLifetimeSeconds = 600f;

        // 유니티 씬 뷰와 같은 배색. 초록(로컬 up)이 추력 방향이라 플레이어가 제일 자주 잡는 축이다.
        private static readonly Color[] AxisColors = { Color.red, Color.green, Color.blue };

        private readonly List<RocketPart> _attached = new();
        private readonly List<Vector3> _attachedLocal = new();
        private LineRenderer _ring;
        private LineRenderer _axis;

        private Transform _gizmoRoot;
        private LineRenderer[] _gizmo; // 0..2 이동 화살표, 3..5 회전 링
        private float _bodyRadius = 0.5f;
        private float _bodyHalfSegment = 1.5f;

        private int _grabAxis = -1;
        private Vector3 _grabAxisWorld;
        private Vector3 _grabReference;
        private Vector3 _grabPosition;
        private float _grabT;
        private float _grabAngle;
        private Quaternion _grabRotation;
        private float _rotationTotal;

        private RocketPart _dragged;
        private Collider _draggedCollider;
        private Transform _dragParent;
        private Vector3 _dragOrigin;
        private Vector2 _dragStart;
        private bool _dragMoved;
        private Plane _dragPlane;
        private bool _overRocket;
        private bool _spawnedFromPreset;
        private Vector3 _attachPoint;

        private RocketPart _selected;
        private EditMode _mode;

        private CinemachineBrain _brain;
        private float _yaw;
        private float _pitch;
        private float _distance;
        private Vector2 _lastMouse;
        private bool _orbiting;

        private Camera _pipCamera;
        private RenderTexture _pipTexture;
        private float _designStartYaw;
        private float _launchYaw;
        private float _launchAltitude;
        private bool _launchViewSwapped;
        private bool _hasPendingDesignTargetIntro;
        private bool _designTargetIntroActive;
        private Bounds _designTargetIntroBounds;
        private Vector3 _designTargetIntroStartPosition;
        private Vector3 _designTargetIntroEndPosition;
        private Quaternion _designTargetIntroStartRotation;
        private Quaternion _designTargetIntroEndRotation;
        private float _designTargetIntroStartTime;
        private float _designTargetIntroHoldSeconds = 3f;
        private float _designTargetIntroTravelSeconds = 1.1f;
        private ParticleSystem _trail;
        private ParticleSystem.Particle[] _trailParticles;

        public Camera Cam => cam;

        /// <summary>발사 후 작은 화면이 그릴 텍스처. 발사 전에는 <c>null</c> 이다.</summary>
        public RenderTexture LaunchPipTexture => _pipTexture;

        /// <summary>큰 화면이 후퇴 뷰인지. UI 가 두 화면의 이름표를 붙이는 용도.</summary>
        public bool LaunchViewSwapped => _launchViewSwapped;
        public EnginePresetLibrarySO PresetLibrary => presetLibrary;
        public RocketPart Selected => _selected;
        public EditMode Mode => _mode;

        public void SetPresetLibrary(EnginePresetLibrarySO library)
        {
            if (presetLibrary == library)
            {
                return;
            }

            presetLibrary = library;
            PresetLibraryChanged?.Invoke();
        }

        /// <summary>선택이 바뀌거나 모드가 바뀔 때. UI 가 버튼 표시를 갱신하는 용도.</summary>
        public event Action Changed;
        public event Action PresetLibraryChanged;

        private void Start()
        {
            // 프리팹 에셋은 씬 오브젝트 참조를 직렬화할 수 없다 — 프리팹으로 꺼내 놓은 Builder 는
            // cam/rocket 이 비어 들어오므로 씬에서 직접 찾는다. 씬 인스턴스는 오버라이드를 그대로 쓴다.
            if (cam == null) cam = Camera.main;
            if (rocket == null) rocket = FindFirstObjectByType<Rocket>();

            // 브레인은 코드로 붙인다 — 씬 YAML diff 를 늘리지 않고, additive 로 올라왔을 때
            // 어느 카메라가 잡히든 그 카메라가 브레인을 갖는다.
            if (!cam.TryGetComponent(out _brain)) _brain = cam.gameObject.AddComponent<CinemachineBrain>();
            // 브레인의 기본 마스크는 Everything 이라 전역 vcam 큐에서 우선순위만으로 고른다 —
            // 01_Main 위에 additive 로 올라오면 연구 vcam(Priority 20)이 DesignCam(10)을 눌러
            // 설계 카메라가 연구 랩 자세에 묶인다(궤도 회전이 화면에 안 나온다). 시뮬레이션 vcam 은
            // Channel01 전용으로 두고 이 브레인만 그 채널을 본다 — 발사 시 LaunchCam 20 동점도 없어진다.
            _brain.ChannelMask = OutputChannels.Channel01;
            // 자동 갱신은 순서가 없다 — CinemachineBrain 에는 DefaultExecutionOrder 가 없어서
            // 이 컴포넌트의 LateUpdate 와 앞뒤가 정해지지 않는다. 기즈모가 한 프레임 밀리지 않도록
            // 브레인을 직접 돌린다(LateUpdate 끝).
            _brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            _brain.DefaultBlend =
                new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, launchBlendSeconds);

            // 시작 각도의 소유자는 씬의 Main Camera 가 아니라 DesignCam 이다 — 브레인이 카메라를
            // 덮어쓰므로 카메라 트랜스폼에서 역산하면 첫 프레임 이후 기준이 사라진다.
            Vector3 offset = designCam.transform.position - rocket.transform.position;
            _distance = offset.magnitude;
            _yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Asin(Mathf.Clamp(offset.y / _distance, -1f, 1f)) * Mathf.Rad2Deg;
            // 발사 뷰가 쓸 기준 방위각. 씬에 배치된 DesignCam 각도가 로켓을 제일 잘 보여주도록
            // 잡아 둔 값이라, 플레이어가 궤도 회전으로 어디를 보고 있든 발사는 여기서 시작한다.
            _designStartYaw = _yaw;

            _ring = CreateGuide("AlignmentRing", rocket.transform, RingSegments, true, guideColor);
            _axis = CreateGuide("AlignmentAxis", rocket.transform, 2, false, guideColor);

            CacheBodyShape();
            BuildGizmo();
            if (_hasPendingDesignTargetIntro) BeginDesignTargetIntro();
        }

        private SoundHandle _gearSound;
        private float _lastRotationMotion = float.NegativeInfinity;
        private const float RotationSoundIdleSeconds = 0.12f;

        private void Update()
        {
            RocketPart part = _selected;
            Quaternion before = part != null ? part.transform.localRotation : Quaternion.identity;
            UpdateInput();
            bool dragging = part != null && part == _selected && _mode == EditMode.Rotate
                && _grabAxis >= 0 && rocket != null && !rocket.Launched
                && part.transform.IsChildOf(rocket.transform)
                && Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool moved = dragging && Quaternion.Angle(before, part.transform.localRotation) > 0.01f;
            UpdateRotationSoundForMotion(dragging, moved, Time.unscaledTime);
        }

        private void UpdateRotationSoundForMotion(bool dragging, bool moved, float now)
        {
            if (dragging && moved) _lastRotationMotion = now;
            // Input sampling and angle snapping leave short gaps even during a continuous drag.
            UpdateRotationSound(dragging && now - _lastRotationMotion < RotationSoundIdleSeconds);
        }

        private void UpdateRotationSound(bool rotating)
        {
            if (!rotating)
            {
                _gearSound.Stop();
                _gearSound = SoundHandle.Invalid;
                _lastRotationMotion = float.NegativeInfinity;
                return;
            }
            if (!_gearSound.IsValid && SoundManager.Instance != null)
                _gearSound = SoundManager.Instance.PlaySfx("gear");
        }

        private void OnDisable() => UpdateRotationSound(false);

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) UpdateRotationSound(false);
        }

        private void UpdateInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame && !rocket.Launched)
                {
                    RequestLaunch();
                    // 발사한 뒤에는 편집 상태가 남으면 안 된다 — 기즈모가 날아가는 부품을 계속 따라다니고
                    // 이동·회전 버튼도 켜진 채로 남는다. 선택이 없을 때도 UI 가 갱신되도록 직접 알린다.
                }
                if (keyboard.escapeKey.wasPressedThisFrame) SetMode(EditMode.None);
                if (keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
                    DeleteSelected();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 position = mouse.position.ReadValue();
            Vector2 delta = position - _lastMouse;
            // 패널 위에서 시작한 입력은 3D 로 새면 안 된다 — 패널을 드래그하면 카메라가 돌아버린다.
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            RequestCursor(overUI, position);

            if (_designTargetIntroActive && !rocket.Launched)
            {
                _lastMouse = position;
                return;
            }

            // 핸들을 잡은 동안에는 카메라를 묶는다. 잡을 때의 t·각도는 고정이지만 광선은 아니라,
            // 궤도 회전이나 휠 줌이 끼어들면 부품이 한 프레임에 튄다.
            Orbit(mouse, delta, overUI || _grabAxis >= 0);
            _lastMouse = position;

            if (_mode != EditMode.None)
            {
                EditSelected(mouse, position, overUI);
                return;
            }

            if (_dragged != null)
            {
                if (!mouse.leftButton.isPressed) EndDrag();
                // 누른 자리에서 몇 픽셀은 움직여야 드래그로 친다. 아니면 고르려고 누른 순간
                // 부품이 커서 아래 표면으로 옮겨진다 — 자기 콜라이더가 꺼져 뒤쪽 본체가 맞는다.
                else if (_dragMoved || (position - _dragStart).sqrMagnitude > DragSlopPixels * DragSlopPixels)
                {
                    _dragMoved = true;
                    Drag(position);
                }

                return;
            }

            if (overUI || !mouse.leftButton.wasPressedThisFrame) return;

            BeginDrag(position);
            if (_dragged == null) Select(null); // 빈 공간 클릭은 선택 해제
        }

        /// <summary>
        /// 발사 뷰를 씬이 정해 둔 기준 방위각(<c>_designStartYaw</c>)에 세운다 — 플레이어가 설계 중
        /// 어느 쪽으로 돌려놨든 발사는 로켓이 잘 보이는 각도에서 시작한다. 발사 고도도 여기서 잠근다:
        /// 발사 뒤에도 우클릭 궤도가 <c>_yaw</c> 를 계속 바꾸므로 두 발사 뷰가 같은 기준을 보려면
        /// 발사 순간 값이 따로 있어야 한다.
        /// </summary>
        public void RequestLaunch()
        {
            if (rocket.Launched) return;
            rocket.Launch();
            if (!rocket.Launched) return;
            Select(null);
            PlaceLaunchCamera();
            Changed?.Invoke();
        }

        private void PlaceLaunchCamera()
        {
            _launchYaw = _designStartYaw;
            _launchAltitude = rocket.transform.position.y;
            _launchViewSwapped = false;

            launchCam.Priority = 20; // PrioritySettings 는 int 암시 변환

            EnsurePipCamera();
            EnsureTrajectoryTrail();
            _pipCamera.enabled = true;
            _trail.Play();
            ApplyLaunchViews();
        }

        public void PreviewDesignTarget(Bounds targetBounds, float holdSeconds = 0.8f, float travelSeconds = 1.4f)
        {
            _designTargetIntroBounds = targetBounds;
            _designTargetIntroHoldSeconds = Mathf.Max(0f, holdSeconds);
            _designTargetIntroTravelSeconds = Mathf.Max(0.01f, travelSeconds);
            if (rocket == null || designCam == null)
            {
                _hasPendingDesignTargetIntro = true;
                return;
            }

            BeginDesignTargetIntro();
        }

        public void ReturnToDesign()
        {
            launchCam.Priority = 0;
            _launchViewSwapped = false;
            _designTargetIntroActive = false;
            _hasPendingDesignTargetIntro = false;
            if (_pipCamera != null) _pipCamera.enabled = false;
            if (_trail != null) _trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Select(null);
            Changed?.Invoke();
        }

        /// <summary>
        /// 작은 화면용 카메라. 발사 순간에 만든다 — 설계 단계에서는 그릴 것이 없다.
        /// <see cref="Camera.CopyFrom"/> 는 컴포넌트를 복사하지 않으므로 브레인도 오디오 리스너도
        /// 따라오지 않는다(원하는 바). 태그는 Untagged 로 남긴다 — MainCamera 가 되면
        /// <see cref="Camera.main"/> 이 이쪽으로 풀려 설계 조작 좌표계가 통째로 어긋난다.
        /// </summary>
        private void EnsurePipCamera()
        {
            if (_pipCamera != null) return;

            _pipTexture = new RenderTexture(pipResolution.x, pipResolution.y, 24) { name = "LaunchPip" };

            var host = new GameObject("LaunchPipCamera");
            host.transform.SetParent(transform, false); // 시뮬레이션 씬과 함께 언로드된다
            _pipCamera = host.AddComponent<Camera>();
            _pipCamera.CopyFrom(cam);
            // CopyFrom 은 미션 컨트롤 뷰포트 사각형까지 복사한다 — 렌더 타깃에 그릴 때는 전체를 써야 한다.
            _pipCamera.rect = new Rect(0f, 0f, 1f, 1f);
            _pipCamera.targetTexture = _pipTexture;

            // CopyFrom 이 Main Camera 컬링 마스크를 통째로 가져오는데, 먼지는 그 카메라를 감싸고 있다 —
            // 488 유닛 밖인 이쪽에서는 로켓에 붙은 각지름 14° 짜리 얼룩으로만 보인다. 뷰 역할이 스왑돼도
            // 먼지는 늘 Main Camera 쪽이라 매 프레임 토글이 아니라 여기서 한 번 끈다.
            _pipCamera.cullingMask &= ~(1 << SkyEnvironment.DustLayer);

            Log.D($"Launch views ready: pip {pipResolution.x}x{pipResolution.y}", this);
        }

        /// <summary>
        /// 궤적 점. 고정된 후퇴 카메라에서는 로켓이 화면의 1% 남짓이라 어디까지 올라갔는지가 안 읽힌다 —
        /// 지나온 자리에 점을 남겨 발사대에서 현재 위치까지를 눈으로 잇는다.
        /// 위치를 기록하는 코드는 두지 않는다: <see cref="ParticleSystem.EmissionModule.rateOverDistance"/>
        /// 가 "이만큼 움직일 때마다 하나" 를 그대로 해준다. 시간 기준으로 뿌리면 연소 구간은 성기고
        /// 정점에는 뭉치는데, 거리 기준이면 속도와 무관하게 간격이 균일하다.
        /// </summary>
        private void EnsureTrajectoryTrail()
        {
            if (_trail != null) return;

            var host = new GameObject("TrajectoryTrail") { layer = trailLayer };
            host.transform.SetParent(rocket.transform, false); // 로켓과 같이 움직여야 거리가 쌓인다

            _trail = host.AddComponent<ParticleSystem>();
            _trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _trail.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // 뿌린 점은 그 자리에 남는다
            main.startLifetime = TrailLifetimeSeconds;
            main.startSpeed = 0f;
            main.startSize = 1f; // 실제 크기는 매 프레임 UpdateTrailDotSize 가 정한다
            main.gravityModifier = 0f;
            main.playOnAwake = false;
            main.maxParticles = 4096;

            ParticleSystem.EmissionModule emission = _trail.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = trailDotSpacing > 0f ? 1f / trailDotSpacing : 0f;

            ParticleSystem.ShapeModule shape = _trail.shape;
            shape.enabled = false; // 로켓 위치 그대로 한 점

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            // ponytail: Shader.Find 는 에디터 프로토타입 한정 — 빌드에 넣으려면 trailMaterial 을 채운다.
            renderer.material = trailMaterial != null
                ? trailMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _trail.Play();
        }

        /// <summary>
        /// 두 발사 뷰를 이번 프레임 상태에 맞춘다. 스왑은 카메라가 아니라 <b>역할</b>을 맞바꾸는 것이라
        /// 큰 화면은 끝까지 <c>launchCam</c> 이다 — 화각을 쓰는 쪽이 vcam Lens 이고 다른 쪽이 순수
        /// Camera 라, 두 타입에 각각 써 준다.
        /// </summary>
        private void ApplyLaunchViews()
        {
            // 궤적은 후퇴 뷰를 맡은 카메라에만 보인다. 역할이 스왑되므로 카메라에 고정 배정할 수 없어
            // 매 프레임 다시 쓴다 — int 대입 두 번이라 캐시할 값도 아니다.
            cam.cullingMask = TrailCullingMask(cam.cullingMask, trailLayer, _launchViewSwapped);
            if (_pipCamera != null)
            {
                _pipCamera.cullingMask =
                    TrailCullingMask(_pipCamera.cullingMask, trailLayer, !_launchViewSwapped);
            }

            float pullback = PullbackDistance(rocket.transform.position.y - _launchAltitude,
                pullbackNearDistance, pullbackGrowth, pullbackFarDistance);

            PlaceLaunchCameraView(launchCam.transform, _launchViewSwapped ? pullback : chaseDistance);
            if (_pipCamera != null)
            {
                PlaceLaunchCameraView(_pipCamera.transform, _launchViewSwapped ? chaseDistance : pullback);
            }

            UpdateTrailDotSize(pullback);
        }

        /// <summary>
        /// 궤적 점을 화면에서 **항상 같은 크기**로 유지한다. 월드 크기를 고정하면 후퇴 뷰가 500 유닛까지
        /// 빠지는 동안 점이 1 px 아래로 줄어 궤적이 통째로 사라진다 — 거리에 비례해 월드 크기를 키운다.
        /// 기준 거리는 카메라에서 로켓까지다. 점은 발사대부터 로켓까지 흩어져 있지만 카메라가 그만큼
        /// 멀리 있어 거리 편차가 5% 안쪽이라, 점마다 따로 재지 않고 한 값으로 밀어도 눈에 안 띈다.
        /// </summary>
        private void UpdateTrailDotSize(float distance)
        {
            if (_trail == null) return;

            float size = TrailDotWorldSize(trailDotScreenSize, distance, cam.fieldOfView);

            ParticleSystem.MainModule main = _trail.main;
            main.startSize = size; // 앞으로 나올 점

            // 이미 나온 점은 startSize 로 바뀌지 않는다 — 살아 있는 것들을 매 프레임 다시 쓴다.
            _trailParticles ??= new ParticleSystem.Particle[main.maxParticles];

            int count = _trail.GetParticles(_trailParticles);
            for (int i = 0; i < count; i++) _trailParticles[i].startSize = size;
            _trail.SetParticles(_trailParticles, count);
        }

        /// <summary>
        /// 화면 높이의 <paramref name="screenFraction"/> 만큼을 차지하는 월드 크기.
        /// 거리 d 에서 화면이 담는 세로 길이는 <c>2 · d · tan(FOV/2)</c> 다.
        /// </summary>
        public static float TrailDotWorldSize(float screenFraction, float distance, float verticalFov)
        {
            return screenFraction * 2f * distance * Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 두 발사 뷰의 공통 배치. 자세는 발사 순간 방위각에 고정하고 위치는 로켓을 그대로 따라간다 —
        /// 고도뿐 아니라 <b>좌우 드리프트도</b> 따라가므로, 편심 추력으로 로켓이 옆으로 밀려도 화면
        /// 한가운데 남는다. 두 뷰의 차이는 거리 하나뿐이고, 상태를 두지 않으므로 스왑으로 담당 카메라가
        /// 바뀌어도 한 프레임에 맞는다.
        /// </summary>
        private void PlaceLaunchCameraView(Transform view, float distance)
        {
            Quaternion rotation = Quaternion.Euler(0f, _launchYaw, 0f);
            Vector3 target = rocket.transform.position;
            // 로켓은 물을 통과해 가라앉는다. 카메라까지 따라 들어가면 물을 아래에서 올려다보게 된다.
            target.y = Mathf.Max(target.y, cameraFloorY);
            // 진폭은 궤적 점과 같은 규칙으로 화면 기준이다 — 거리로 재면 후퇴 뷰(최대 500)에서
            // 흔들림이 통째로 사라진다. 세기는 실제로 걸린 추력을 따르므로 연료가 떨어지면 저절로 멎고,
            // 자세는 건드리지 않는다: 두 발사 뷰의 "피치 0 고정" 규약이 그대로 남아야 한다.
            Vector3 shake = LaunchShake(
                TrailDotWorldSize(shakeScreenAmplitude, distance, cam.fieldOfView) * rocket.ThrustFraction,
                Time.time, shakeFrequency);
            Vector3 position = target + rotation * (new Vector3(0f, 0f, -distance) + shake);
            view.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// 뷰 로컬 X·Y 흔들림 오프셋. 두 축을 Perlin 잡음의 <b>서로 다른 행</b>에서 뽑는다 —
        /// <c>PerlinNoise(t, 0)</c> 과 <c>PerlinNoise(0, t)</c> 는 대칭이라 값이 정확히 같고,
        /// 그러면 두 축이 완전히 상관되어 대각선으로만 움직인다(진동이 아니라 미끄러짐으로 읽힌다).
        /// <see cref="Mathf.PerlinNoise"/> 는 0~1 을 살짝 벗어날 수 있으므로 잘라서 진폭을 보장한다.
        /// </summary>
        public static Vector3 LaunchShake(float worldAmplitude, float time, float frequency)
        {
            if (worldAmplitude <= 0f) return Vector3.zero;

            float t = time * frequency;
            return new Vector3(
                Offset(t, 0f), Offset(t, YNoiseRow), 0f);

            float Offset(float x, float y) =>
                (Mathf.Clamp01(Mathf.PerlinNoise(x, y)) - 0.5f) * 2f * worldAmplitude;
        }

        /// <summary>
        /// 후퇴 뷰 거리. 고도에 비례해 계속 물러난다 — 궤적 점이 고도를 알려주므로 로켓이 작아져도
        /// 읽히고, 대신 지나온 궤적 전체가 프레임에 들어온다.
        /// 상한은 far clip(1000) 안에 둔다. 발사 고도 아래로 떨어져도 하한 밑으로는 붙지 않는다.
        /// </summary>
        public static float PullbackDistance(float altitude, float nearDistance, float growth,
            float farDistance)
        {
            return Mathf.Clamp(nearDistance + Mathf.Max(altitude, 0f) * growth, nearDistance, farDistance);
        }

        public static void TargetOverviewCameraPose(Vector3 launchPad, Bounds targetBounds,
            float nearDistance, float farDistance, out Vector3 position, out Quaternion rotation)
        {
            Vector3 targetCenter = targetBounds.center;
            Vector3 horizontal = targetCenter - launchPad;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Vector3.forward;

            Vector3 targetDirection = horizontal.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, targetDirection).normalized;
            Vector3 focus = Vector3.Lerp(launchPad, targetCenter, 0.5f);
            float span = Vector3.Distance(launchPad, targetCenter) + targetBounds.extents.magnitude;
            float distance = Mathf.Clamp(span * 1.35f, nearDistance, farDistance);
            float lift = Mathf.Max(span * 0.28f, 80f);

            position = focus
                - targetDirection * distance
                + side * (targetBounds.extents.x * 0.35f)
                + Vector3.up * lift;
            rotation = LookAt(position, focus);
        }

        /// <summary>궤적 레이어 비트만 켜고 끈 컬링 마스크. 나머지 비트는 건드리지 않는다.</summary>
        public static int TrailCullingMask(int cullingMask, int trailLayer, bool visible)
        {
            int bit = 1 << trailLayer;
            return visible ? cullingMask | bit : cullingMask & ~bit;
        }

        private void BeginDesignTargetIntro()
        {
            _hasPendingDesignTargetIntro = false;
            if (rocket == null || designCam == null || rocket.Launched)
            {
                return;
            }

            _designTargetIntroActive = true;
            _designTargetIntroStartTime = Time.time;
            _designTargetIntroEndRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _designTargetIntroEndPosition =
                rocket.transform.position + _designTargetIntroEndRotation * new Vector3(0f, 0f, -_distance);

            TargetOverviewCameraPose(rocket.transform.position, _designTargetIntroBounds,
                pullbackNearDistance, pullbackFarDistance,
                out _designTargetIntroStartPosition, out _designTargetIntroStartRotation);
        }

        private bool TryApplyDesignTargetIntro()
        {
            if (!_designTargetIntroActive) return false;
            if (rocket.Launched)
            {
                _designTargetIntroActive = false;
                return false;
            }

            float elapsed = Time.time - _designTargetIntroStartTime;
            if (elapsed < _designTargetIntroHoldSeconds)
            {
                designCam.transform.SetPositionAndRotation(_designTargetIntroStartPosition,
                    _designTargetIntroStartRotation);
                return true;
            }

            float travelElapsed = elapsed - _designTargetIntroHoldSeconds;
            if (travelElapsed >= _designTargetIntroTravelSeconds)
            {
                _designTargetIntroActive = false;
                return false;
            }

            float t = Mathf.SmoothStep(0f, 1f, travelElapsed / _designTargetIntroTravelSeconds);
            designCam.transform.SetPositionAndRotation(
                Vector3.Lerp(_designTargetIntroStartPosition, _designTargetIntroEndPosition, t),
                Quaternion.Slerp(_designTargetIntroStartRotation, _designTargetIntroEndRotation, t));
            return true;
        }

        private static Quaternion LookAt(Vector3 position, Vector3 target)
        {
            Vector3 direction = target - position;
            return direction.sqrMagnitude < 0.0001f
                ? Quaternion.identity
                : Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        /// <summary>큰 화면과 작은 화면의 역할을 맞바꾼다. 작은 화면 클릭이 이걸 부른다.</summary>
        public void ToggleLaunchView()
        {
            if (rocket != null && rocket.Launched) _launchViewSwapped = !_launchViewSwapped;
        }

        private void OnDestroy()
        {
            UpdateRotationSound(false);

            // 정적 레지스트리라 씬을 내려도 남는다 — 파괴된 렌더러를 물고 있지 않도록 여기서 비운다.
            SelectionOutlineFeature.Select(null, null);

            // RenderTexture 는 GC 가 회수하지 않는다 — 씬을 내렸다 올릴 때마다 GPU 메모리가 샌다.
            if (_pipTexture == null) return;

            _pipTexture.Release();
            Destroy(_pipTexture);
            _pipTexture = null;
        }

        private void LateUpdate()
        {
            if (rocket.Launched)
            {
                ApplyLaunchViews();
            }
            else
            {
                if (!TryApplyDesignTargetIntro())
                {
                    Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                    designCam.transform.SetPositionAndRotation(
                        rocket.transform.position + rotation * new Vector3(0f, 0f, -_distance), rotation);
                }
            }

            // vcam 을 옮긴 뒤에 돌려야 cam 이 이번 프레임 자세를 갖는다.
            _brain.ManualUpdate();
            // 발사 뒤에도 건너뛰지 않는다 — 발사 프레임의 Select(null) 을 반영하지 못하면
            // 기즈모 LineRenderer 가 켜진 채 남아 날아가는 로켓 옆에 유령으로 붙는다.
            UpdateGizmo(); // 카메라가 움직인 뒤여야 화면 고정 크기가 한 프레임 밀리지 않는다
        }

        // 우클릭 드래그로 로켓 주위를 돈다. 좌클릭 부품 드래그와 버튼이 갈려 동시에 성립하지 않는다.
        private void Orbit(Mouse mouse, Vector2 delta, bool overUI)
        {
            if (mouse.rightButton.wasPressedThisFrame) _orbiting = !overUI;
            else if (!mouse.rightButton.isPressed) _orbiting = false;
            else if (_orbiting)
            {
                _yaw += delta.x * orbitSensitivity;
                _pitch = Mathf.Clamp(_pitch - delta.y * orbitSensitivity, minPitch, maxPitch);
            }

            if (overUI) return; // 패널 위 휠은 목록 스크롤 몫이다

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
                _distance = Mathf.Clamp(_distance - scroll * zoomSpeed * _distance, minDistance, maxDistance);
        }

        // ---- 프리셋 패널에서 들어오는 경로 -------------------------------------------------

        /// <summary>
        /// 프리셋 항목을 끌기 시작했을 때 새 엔진을 만들어 드래그 상태에 얹는다. 이후 이동·부착·취소는
        /// 씬에 이미 있던 부품과 완전히 같은 경로를 탄다.
        /// </summary>
        public void BeginPresetDrag(EngineStatsSO preset, Vector2 screenPosition)
        {
            if (rocket.Launched || _dragged != null) return;

            if (enginePrefab == null)
            {
                Log.W("RocketBuilder: assign enginePrefab before dragging from the preset panel", this);
                return;
            }

            RocketPart part = Instantiate(enginePrefab);
            part.name = preset != null ? preset.name : enginePrefab.name;
            part.ApplyPreset(preset);
            part.transform.rotation = rocket.transform.rotation;

            _spawnedFromPreset = true;
            StartDragging(part, screenPosition);
        }

        /// <summary>이동·회전 버튼이 부르는 모드 전환. 같은 모드를 다시 누르면 해제된다.</summary>
        public void SetMode(EditMode mode)
        {
            if (_selected == null || rocket.Launched) mode = EditMode.None;
            if (mode == _mode) mode = EditMode.None;
            if (mode == _mode) return;

            // 모드 재진입은 반드시 여기를 지난다 — 삭제·선택 해제로 남은 잡기 상태를 여기서 끊는다.
            UpdateRotationSound(false);
            _grabAxis = -1;
            _mode = mode;
            if (mode != EditMode.Move) HideGuides(); // 이동을 끝내면 가이드도 같이 사라져야 한다
            Changed?.Invoke();
        }

        public void DeleteSelected()
        {
            if (_selected == null || rocket.Launched) return;

            UpdateRotationSound(false);
            Destroy(_selected.gameObject);
            _mode = EditMode.None;
            _grabAxis = -1;
            _selected = null;
            Changed?.Invoke();
        }

        // ---- 드래그 -------------------------------------------------------------------------

        private void BeginDrag(Vector2 screenPosition)
        {
            if (rocket.Launched) return;

            RocketPart part = PickPart(cam.ScreenPointToRay(screenPosition));
            if (part == null) return;

            // 프리셋 없는 엔진은 조용히 0추력으로 붙는 대신 아예 집히지 않게 막는다.
            if (!part.HasStats)
            {
                Log.W($"{part.name}: assign an EngineStatsSO before placing it", part);
                return;
            }

            _spawnedFromPreset = false;
            StartDragging(part, screenPosition);
        }

        private void StartDragging(RocketPart part, Vector2 screenPosition)
        {
            _dragged = part;
            _dragParent = part.transform.parent;
            _dragOrigin = part.transform.position;
            _dragPlane = new Plane(-cam.transform.forward, _dragOrigin);
            _dragStart = screenPosition;
            _dragMoved = _spawnedFromPreset; // 프리셋에서 꺼낸 엔진은 처음부터 커서를 따라야 한다
            _overRocket = false;

            // 자기 콜라이더가 표면 레이캐스트를 가로막지 않게 잠시 끈다.
            _draggedCollider = part.GetComponent<Collider>();
            _draggedCollider.enabled = false;

            part.transform.SetParent(null, true); // 이미 붙어 있었다면 떼어낸다
            Select(part);
            if (_dragMoved) Drag(screenPosition);
        }

        /// <summary>
        /// 광선에 맞은 부품 중 가장 가까운 것. 맨 <c>Physics.Raycast</c> 로는 집을 수 없다 —
        /// 붙어 있는 엔진은 본체 콜라이더와 맞닿아 있어, 실루엣 가장자리를 찍으면 본체 캡슐이
        /// 먼저 맞아 집기가 실패했다. 클릭과 호버가 같은 부품을 고르도록 판정을 공유한다.
        /// </summary>
        private RocketPart PickPart(Ray ray)
        {
            RocketPart best = null;
            float bestDistance = float.PositiveInfinity;

            foreach (RaycastHit hit in Physics.RaycastAll(ray))
            {
                if (hit.distance >= bestDistance) continue;

                RocketPart part = hit.collider.GetComponentInParent<RocketPart>();
                if (part == null) continue;

                best = part;
                bestDistance = hit.distance;
            }

            return best;
        }

        private void Drag(Vector2 screenPosition)
        {
            Ray ray = cam.ScreenPointToRay(screenPosition);
            bool hitAny = Physics.Raycast(ray, out RaycastHit hit);
            bool onSilhouette = hitAny && hit.collider.GetComponentInParent<Rocket>() == rocket;

            // 커서가 가리키는 표면이 깊이를 결정하므로 카메라를 어느 각도로 돌려도 보이는 자리에 놓인다.
            // 실루엣을 벗어나면 광선이 본체 축에 가장 가까워지는 점을 대신 쓴다 — 콜라이더에 맞아야만
            // 붙던 규칙은 캡슐 마개, 곧 로켓의 꼭대기와 바닥을 화면에서 몇 픽셀짜리 표적으로 만들었고
            // 아래쪽은 카메라 피치가 -20° 에 묶여 아예 볼 수도 없었다. 로켓 위나 아래 빈 곳으로 끌면
            // 여기서 마개 위 점이 나오고, 그 뒤 투영·스냅·부착은 옆면과 완전히 같은 경로다.
            // 자세는 건드리지 않는다 — 회전시킨 자세가 곧 추력 방향이라 임의로 세우면 힘이 바뀐다.
            Vector3 target = onSilhouette ? hit.point : Vector3.zero;
            _overRocket = onSilhouette || TryReachBody(ray, out target);

            if (_overRocket)
            {
                // 기즈모 이동과 같은 함수를 태운다 — 여기서만 hit.point 를 그대로 쓰면 드래그로
                // 놓은 자리와 화살표로 옮긴 자리가 서로 다른 규칙을 따른다.
                _attachPoint = ProjectOntoBody(SnapToGuides(target, _dragged), _dragged,
                    _dragged.transform.position);
                _dragged.transform.position = _attachPoint;
            }
            else
            {
                // 지면이든 다른 물체든 붙을 곳은 아니다 — 커서만 따라가다 놓으면 사라진다.
                HideGuides();
                if (hitAny) _dragged.transform.position = hit.point;
                else if (_dragPlane.Raycast(ray, out float distance))
                    _dragged.transform.position = ray.GetPoint(distance);
            }

            // 놓는 순간 사라질 자리에 있으면 홀로그램으로 알린다 — AttachInvalid 커서와 같은 뜻이다.
            _dragged.SetHologram(!_overRocket);
        }

        private void EndDrag()
        {
            RocketPart part = _dragged;
            _dragged = null;
            _draggedCollider.enabled = true;
            _draggedCollider = null;
            HideGuides();
            part.SetHologram(false);

            _spawnedFromPreset = false;

            // 엔진은 로켓에 붙어 있을 때만 존재한다. 로켓 밖에 놓으면 떼어낸 것으로 보고 지우고,
            // 프리셋에서 갓 꺼낸 것도 같은 규칙이라 "붙는 자리에 놓아야 생긴다"가 된다.
            if (!_dragMoved)
            {
                part.transform.SetParent(_dragParent, true); // 움직이지 않은 클릭은 선택일 뿐이다
            }
            else if (_overRocket)
            {
                rocket.Attach(part, _attachPoint);
            }
            else
            {
                Destroy(part.gameObject);
                Select(null);
            }
        }

        // ---- 선택 부품 편집 (축 구속 기즈모) --------------------------------------------------

        private void EditSelected(Mouse mouse, Vector2 position, bool overUI)
        {
            if (_selected == null)
            {
                SetMode(EditMode.None);
                return;
            }

            // 잡고 있는 동안에는 overUI 를 보지 않는다 — 버튼 바가 부품을 따라다녀서, 드래그 중
            // 커서가 그 위를 스치면 부품이 그 프레임만 멈춰 손이 미끄러진 것처럼 보인다.
            if (_grabAxis >= 0)
            {
                if (mouse.leftButton.isPressed)
                {
                    DragHandle(position);
                    return;
                }

                _grabAxis = -1;
                HideGuides();
                return;
            }

            if (overUI || !mouse.leftButton.wasPressedThisFrame) return;

            _grabAxis = PickHandle(position);
            if (_grabAxis < 0)
            {
                // 빗나간 클릭으로 모드를 버리지 않는다 — 링이 얇아 한 번 스치기만 해도 조작이 통째로 풀렸다.
                // 대신 평소 클릭과 같은 뜻으로 읽는다: 다른 부품을 누르면 그것을 고르고(Select 가 모드를 끈다),
                // 빈 공간이면 선택이 풀린다. 드래그는 시작하지 않는다 — 모드 중에는 축이 유일한 조작 수단이다.
                Select(PickPart(cam.ScreenPointToRay(position)));
                return;
            }

            Transform part = _selected.transform;
            Ray ray = cam.ScreenPointToRay(position);

            _grabPosition = part.position;
            // 축과 기준 방향은 잡는 순간 고정한다. 매 프레임 부품 자세에서 다시 읽으면 회전한 만큼
            // 기준도 같이 돌아 각도 변화가 정확히 상쇄되고, 링이 죽은 것처럼 보인다.
            _grabAxisWorld = part.rotation * Axis(_grabAxis);
            _grabReference = part.rotation * Axis((_grabAxis + 1) % 3);
            _grabRotation = part.rotation;
            _rotationTotal = 0f;

            if (_mode == EditMode.Move) ClosestPointOnAxis(_grabPosition, _grabAxisWorld, ray, out _grabT);
            else AngleOnPlane(_grabPosition, _grabAxisWorld, _grabReference, ray, out _grabAngle);
        }

        private void DragHandle(Vector2 position)
        {
            Ray ray = cam.ScreenPointToRay(position);
            Transform part = _selected.transform;

            if (_mode == EditMode.Move)
            {
                if (!ClosestPointOnAxis(_grabPosition, _grabAxisWorld, ray, out float t)) return;

                // 스냅·재투영 결과를 다음 프레임 입력으로 되먹이지 않는다 — 그러면 처음 닿은
                // 스냅점에 눌어붙어 아무리 끌어도 못 빠져나온다.
                Vector3 wanted = _grabPosition + _grabAxisWorld * (t - _grabT);
                part.position = ProjectOntoBody(SnapToGuides(wanted, _selected), _selected, _grabPosition);
                return;
            }

            if (!AngleOnPlane(_grabPosition, _grabAxisWorld, _grabReference, ray, out float angle)) return;

            // 누적은 보정 전 각도로만 하고, 스냅 결과는 잡을 때 자세에 절대각으로 얹는다.
            // 스냅된 자세를 다시 입력으로 먹이면(델타 누적) 첫 스냅점에 눌어붙어 빠져나오지 못한다.
            _rotationTotal += Mathf.DeltaAngle(_grabAngle, angle);
            _grabAngle = angle;

            float snapped = SnapAngle(_rotationTotal, rotationSnapStep, rotationSnapTolerance);
            part.rotation = Quaternion.AngleAxis(snapped, _grabAxisWorld) * _grabRotation;
            ShowRotationGuide(snapped, snapped != _rotationTotal);
        }

        /// <summary>
        /// 화면에서 커서에 가장 가까운 핸들의 축 번호. 없으면 -1.
        /// 콜라이더를 쓰지 않는다 — 부품 집기가 레이어 마스크 없는 맨 <c>Physics.Raycast</c> 라,
        /// 기즈모에 콜라이더를 달면 그쪽이 먼저 맞아 드래그·부착이 통째로 깨진다.
        /// </summary>
        private int PickHandle(Vector2 screen)
        {
            Transform part = _selected.transform;
            Vector3 origin = part.position;
            float scale = GizmoScale(origin);
            Ray ray = cam.ScreenPointToRay(screen);

            int best = -1;
            float bestDistance = handleGrabPixels;

            for (int i = 0; i < 3; i++)
            {
                Vector3 axis = part.rotation * Axis(i);
                float distance;

                if (_mode == EditMode.Move)
                {
                    if (!ScreenPoint(origin, out Vector2 a)) continue;
                    if (!ScreenPoint(origin + axis * scale, out Vector2 b)) continue;
                    distance = DistanceToSegment(screen, a, b);
                }
                else
                {
                    Vector3 reference = part.rotation * Axis((i + 1) % 3);
                    // 끌 수 없는 링은 고르지도 않는다 — 여기서 실패하는 자리를 잡으면 _grabAngle 이 0 인
                    // 채로 잡혀 첫 프레임에 부품이 튄다. EditSelected 가 같은 광선으로 곧바로 다시 부른다.
                    if (!AngleOnPlane(origin, axis, reference, ray, out _)) continue;

                    distance = RingDistance(screen, origin,
                        reference * scale, part.rotation * Axis((i + 2) % 3) * scale);
                }

                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
            }

            return best;
        }

        /// <summary>
        /// 그린 링 그대로를 화면에 투영해 커서까지 최단 픽셀 거리를 잰다. 평면 교점 각도로 링 위 한
        /// 점만 찍던 방식은 링을 비스듬히 볼 때 커서가 10 px 만 벗어나도 교점 방위각이 반대편으로
        /// 넘어가 <b>링 건너편</b> 점을 골랐다 — 실효 허용치가 14 px 가 아니라 사실상 0 px 이었고,
        /// 그래서 회전은 아예 안 잡히고 이동만 잡혔다. 클릭 한 번뿐인 경로라 32점을 훑어도 된다
        /// (<see cref="PickPart"/> 의 RaycastAll 과 같은 근거).
        /// </summary>
        private float RingDistance(Vector2 screen, Vector3 origin, Vector3 u, Vector3 v)
        {
            Vector2 previous = cam.WorldToScreenPoint(origin + u); // s = 0
            float best = float.PositiveInfinity;

            // s == RingSegments 에서 cos(2π)=1, sin(2π)=0 이라 링이 스스로 닫힌다.
            for (int s = 1; s <= RingSegments; s++)
            {
                float angle = s * 2f * Mathf.PI / RingSegments;
                Vector2 point = cam.WorldToScreenPoint(origin + u * Mathf.Cos(angle) + v * Mathf.Sin(angle));
                best = Mathf.Min(best, DistanceToSegment(screen, previous, point));
                previous = point;
            }

            return best;
        }

        private bool ScreenPoint(Vector3 worldPoint, out Vector2 screen)
        {
            Vector3 point = cam.WorldToScreenPoint(worldPoint);
            screen = point;
            return point.z > 0f; // 카메라 뒤 좌표는 좌우가 뒤집혀 나온다
        }

        /// <summary>
        /// 부품을 본체 표면에 <b>얹는다</b>. 캡슐 투영은 점 하나를 표면으로 끌어올 뿐이라, 피봇이
        /// 기하 중심인 부품을 그 점에 두면 절반이 파묻힌다 — 투영 결과를 바깥 방향으로 부품의 지지
        /// 반경만큼 더 민다. <paramref name="fallbackWorld"/> 는 축 위에서 방위각이 없을 때 쓸 기준점.
        /// </summary>
        private Vector3 ProjectOntoBody(Vector3 worldPoint, RocketPart part, Vector3 fallbackWorld)
        {
            Vector3 local = rocket.transform.InverseTransformPoint(worldPoint);
            Vector3 surface = ProjectOntoCapsule(local, _bodyHalfSegment, _bodyRadius,
                rocket.transform.InverseTransformPoint(fallbackWorld));

            // ProjectOntoCapsule 의 onAxis 와 같은 식이어야 바깥 방향이 정확히 되뽑힌다.
            var onAxis = new Vector3(0f, Mathf.Clamp(local.y, -_bodyHalfSegment, _bodyHalfSegment), 0f);
            Vector3 outward = rocket.transform.TransformDirection(surface - onAxis).normalized;

            return rocket.transform.TransformPoint(surface)
                + outward * (SupportRadius(HalfExtents(part), part.transform.rotation, outward)
                             * (1f - partSeatSink));
        }

        /// <summary>
        /// 실루엣을 벗어난 커서를 본체 표면으로 데려온다. 광선이 본체 캡슐 가까이 지나가면 축에 가장
        /// 가까워지는 <b>광선 위</b>의 점을 <paramref name="worldPoint"/> 로 준다. 로켓 스케일이 균일해야
        /// 로컬 광선이 월드 광선과 같은 직선이다(씬은 1.5 균일).
        /// </summary>
        private bool TryReachBody(Ray ray, out Vector3 worldPoint)
        {
            var local = new Ray(rocket.transform.InverseTransformPoint(ray.origin),
                rocket.transform.InverseTransformDirection(ray.direction));

            bool reached = TryReachCapsule(local, _bodyHalfSegment, _bodyRadius,
                _bodyRadius * attachReachRadii, out Vector3 point);

            worldPoint = rocket.transform.TransformPoint(point);
            return reached;
        }

        // ponytail: BoxCollider.center 0 가정 — 프리팹이 (0,0,0)이다. 오프셋 콜라이더 쓸 일 생기면 더해라.
        private static Vector3 HalfExtents(RocketPart part) =>
            part.TryGetComponent(out BoxCollider box)
                ? Vector3.Scale(box.size * 0.5f, box.transform.lossyScale)
                : Vector3.zero;

        // ---- 기즈모 -------------------------------------------------------------------------

        // 본체 캡슐을 로켓 로컬 치수로 한 번 환산해 둔다. 붙어 있는 엔진도
        // GetComponentInParent<Rocket>() 을 만족해서 레이캐스트로는 본체만 골라 맞힐 수 없다 —
        // 표면 재투영은 물리가 아니라 수치로 푼다.
        private void CacheBodyShape()
        {
            var body = rocket.GetComponentInChildren<CapsuleCollider>(); // 엔진은 BoxCollider 라 안 걸린다
            if (body == null)
            {
                Log.W("RocketBuilder: rocket has no CapsuleCollider body; using default 0.5 / 1.5", this);
                return;
            }

            // 치수는 로켓 로컬 단위여야 한다 — ProjectOntoBody 가 로켓 로컬 좌표로 투영하고 결과를
            // TransformPoint 로 되돌리므로, 월드 치수를 그대로 넣으면 로켓 스케일(1.5)이 한 번 더 곱해져
            // 표면이 그만큼 부풀었다. 옆면에서는 partSeatSink 로 가려졌지만 마개에서는 로켓이 끝난 자리
            // 위로 부품이 떠 버린다.
            Vector3 root = rocket.transform.lossyScale;
            Vector3 scale = body.transform.lossyScale;
            scale = new Vector3(scale.x / root.x, scale.y / root.y, scale.z / root.z);

            _bodyRadius = body.radius * Mathf.Max(scale.x, scale.z); // 유니티의 캡슐 스케일 규칙
            _bodyHalfSegment = Mathf.Max(0f, body.height * 0.5f * scale.y - _bodyRadius);
        }

        private void BuildGizmo()
        {
            _gizmoRoot = new GameObject("PartGizmo").transform;
            // 씬과 함께 언로드되도록 로켓 밑에 둔다. 배율은 매 프레임 부모 스케일을 나눠 넣는다 —
            // 로켓은 스케일 1 이 아니다(씬에서 1.5). 그냥 넣으면 그린 기즈모가 집는 기하보다 1.5 배
            // 커져서, 눈에 보이는 링을 정확히 눌러도 50% 바깥이라 영영 안 잡힌다.
            _gizmoRoot.SetParent(rocket.transform, false);
            _gizmo = new LineRenderer[6];

            // 단위 벡터로 한 번만 굽는다. 매 프레임 바뀌는 건 루트의 위치·자세·배율뿐이다.
            for (int i = 0; i < 3; i++)
            {
                _gizmo[i] = CreateGuide($"Axis{i}", _gizmoRoot, 2, false, AxisColors[i]);
                _gizmo[i].SetPosition(1, Axis(i));

                _gizmo[3 + i] = CreateGuide($"Ring{i}", _gizmoRoot, RingSegments, true, AxisColors[i]);
                for (int s = 0; s < RingSegments; s++)
                {
                    float angle = s * 2f * Mathf.PI / RingSegments;
                    // AngleOnPlane(origin, Axis(i), Axis(i+1), ...) 과 같은 규약이라
                    // 그린 링과 집는 링이 정확히 같은 원이다.
                    _gizmo[3 + i].SetPosition(s,
                        Axis((i + 1) % 3) * Mathf.Cos(angle) + Axis((i + 2) % 3) * Mathf.Sin(angle));
                }
            }
        }

        private void UpdateGizmo()
        {
            if (_gizmo == null) return;

            // 표시 여부를 모드에서 매 프레임 유도한다 — show/hide 를 밀어넣지 않으므로
            // 선택 해제가 유령 기즈모를 남길 수 없다.
            bool move = _mode == EditMode.Move;
            bool show = _selected != null && _mode != EditMode.None;
            for (int i = 0; i < _gizmo.Length; i++) _gizmo[i].enabled = show && (i < 3) == move;
            if (!show) return;

            float scale = GizmoScale(_selected.transform.position);
            _gizmoRoot.SetPositionAndRotation(_selected.transform.position, _selected.transform.rotation);

            // PickHandle 은 월드 크기 scale 로 집는다 — 부모 배율을 나눠야 그린 것과 집는 것이 같은 원이다.
            Vector3 parent = rocket.transform.lossyScale;
            _gizmoRoot.localScale = new Vector3(scale / parent.x, scale / parent.y, scale / parent.z);

            // 잡을 수 있는 자리를 손으로 찾게 두지 않는다 — 커서 아래 핸들을 굵게 해 미리 보여 준다.
            // 잡고 있는 동안에는 다시 찾지 않는다: 부품이 돌면 링도 같이 돌아 하이라이트가 옮겨 다닌다.
            int hovered = _grabAxis;
            if (hovered < 0 && Mouse.current != null) hovered = PickHandle(Mouse.current.position.ReadValue());

            // 선 굵기는 트랜스폼 스케일을 따라가지 않는다 — 맞춰 주지 않으면 줌아웃에서 머리카락이 된다.
            for (int i = 0; i < _gizmo.Length; i++)
                _gizmo[i].widthMultiplier = guideWidth * scale * (i % 3 == hovered ? handleHoverWidth : 1f);
        }

        /// <summary>
        /// 카메라에서 멀어져도 화면상 크기가 같게 만드는 배율. 거리가 아니라 뷰 깊이를 쓴다 —
        /// 거리로 재면 화면 가장자리에서 기즈모가 부풀어 오른다. 원근 카메라 전용.
        /// </summary>
        private float GizmoScale(Vector3 worldPoint)
        {
            float depth = Vector3.Dot(worldPoint - cam.transform.position, cam.transform.forward);
            return Mathf.Max(depth, 0.01f) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * gizmoScreenSize;
        }

        private static Vector3 Axis(int index) =>
            index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;

        // ---- 기즈모 수학 (씬 없이 테스트한다) --------------------------------------------------

        /// <summary>
        /// 마우스 광선에 가장 가까운 축 위의 점을 축 원점 기준 매개변수 <paramref name="t"/>(m)로 준다.
        /// 축을 정면으로 바라보면(광선 ∥ 축) 어느 점이든 똑같이 가까워 t 가 발산하므로 false 다 —
        /// 호출부는 그 프레임을 건너뛰어 부품이 무한대로 날아가는 것을 막는다.
        /// </summary>
        public static bool ClosestPointOnAxis(Vector3 origin, Vector3 axis, Ray ray, out float t)
        {
            t = 0f;
            Vector3 u = axis.normalized;
            Vector3 v = ray.direction.normalized;
            float b = Vector3.Dot(u, v);
            float denominator = 1f - b * b;
            if (denominator < 1e-5f) return false;

            Vector3 w = origin - ray.origin;
            t = (b * Vector3.Dot(v, w) - Vector3.Dot(u, w)) / denominator;
            return true;
        }

        /// <summary>
        /// 회전 링이 놓인 평면(<paramref name="origin"/> 을 지나고 법선 <paramref name="axis"/>)과
        /// 마우스 광선의 교점을 각도(도)로 준다. 0° 는 <paramref name="reference"/> 방향이고 부호는
        /// <c>Quaternion.AngleAxis(각도, axis)</c> 와 같다 — 어긋나면 링이 커서에서 도망간다.
        /// 링을 옆에서 볼 때(광선 ∥ 평면), 교점이 카메라 뒤일 때, 정확히 중심을 가리킬 때는 false.
        /// </summary>
        public static bool AngleOnPlane(Vector3 origin, Vector3 axis, Vector3 reference, Ray ray, out float degrees)
        {
            degrees = 0f;
            Vector3 normal = axis.normalized;
            Vector3 direction = ray.direction.normalized;
            float denominator = Vector3.Dot(normal, direction);
            if (Mathf.Abs(denominator) < 1e-5f) return false;

            float distance = Vector3.Dot(normal, origin - ray.origin) / denominator;
            if (distance <= 0f) return false;

            Vector3 radial = ray.origin + direction * distance - origin;
            Vector3 x = Vector3.ProjectOnPlane(reference, normal);
            if (radial.sqrMagnitude < MinRadius * MinRadius || x.sqrMagnitude < MinRadius * MinRadius) return false;

            x.Normalize();
            Vector3 y = Vector3.Cross(normal, x);
            degrees = Mathf.Atan2(Vector3.Dot(radial, y), Vector3.Dot(radial, x)) * Mathf.Rad2Deg;
            return true;
        }

        /// <summary>화면 좌표에서 점 <paramref name="p"/> 와 선분 ab 의 거리(픽셀).</summary>
        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared < 1e-6f) return Vector2.Distance(p, a); // 축이 카메라 정면이라 점으로 뭉쳤다

            return Vector2.Distance(p, a + ab * Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared));
        }

        /// <summary>
        /// 로켓 로컬 좌표의 점을 본체 캡슐 표면으로 끌어온다. 캡슐은 선분을 반지름만큼 부풀린
        /// 모양이라, 축 선분의 최근접점에서 바깥으로 반지름만큼 밀면 그게 곧 표면이다 — 안팎을
        /// 가리지 않는다(<c>Collider.ClosestPoint</c> 는 내부 점을 그대로 돌려줘서 부품이 파묻힌다).
        /// 축 위에서는 방위각이 정의되지 않아 <paramref name="fallbackRadial"/> 의 수평 성분을 쓴다.
        /// </summary>
        public static Vector3 ProjectOntoCapsule(Vector3 local, float halfSegment, float radius,
            Vector3 fallbackRadial)
        {
            var onAxis = new Vector3(0f, Mathf.Clamp(local.y, -halfSegment, halfSegment), 0f);
            Vector3 outward = local - onAxis;

            if (outward.sqrMagnitude < MinRadius * MinRadius)
            {
                // 중심을 지나 끌 때 부품이 아무 쪽으로나 튀지 않게, 잡기 시작한 방향을 유지한다.
                outward = new Vector3(fallbackRadial.x, 0f, fallbackRadial.z);
                if (outward.sqrMagnitude < MinRadius * MinRadius) outward = Vector3.right;
            }

            return onAxis + outward.normalized * radius;
        }

        /// <summary>
        /// 캡슐 로컬 좌표의 광선이 표면에서 <paramref name="reach"/> 안쪽을 지나가는지. 지나가면 축에
        /// 가장 가까워지는 <b>광선 위</b>의 점을 준다 — 캡슐과 교차할 필요가 없으므로 실루엣 바깥,
        /// 곧 로켓 위·아래 빈 곳에서도 점이 나오고 <see cref="ProjectOntoCapsule"/> 이 그것을 마개로
        /// 끌어온다. 광선이 축과 평행하면(정확히 위에서 내려다보기) 최근접점이 발산해 false 다.
        /// </summary>
        public static bool TryReachCapsule(Ray ray, float halfSegment, float radius, float reach,
            out Vector3 point)
        {
            point = default;
            if (!ClosestPointOnAxis(Vector3.zero, Vector3.up, ray, out float t)) return false;

            // 축 위 최근접점을 광선에 되투영하면 두 직선의 공통 수선 발이 나온다.
            Vector3 direction = ray.direction.normalized;
            var onAxis = new Vector3(0f, t, 0f);
            point = ray.origin + direction * Vector3.Dot(onAxis - ray.origin, direction);

            var onSegment = new Vector3(0f, Mathf.Clamp(point.y, -halfSegment, halfSegment), 0f);
            float limit = radius + reach;
            return (point - onSegment).sqrMagnitude <= limit * limit;
        }

        // ---- 정렬 가이드 ---------------------------------------------------------------------

        /// <summary>
        /// 부착점을 이미 붙어 있는 엔진 기준으로 정렬하고, 스냅된 축의 가이드를 켠다.
        /// <paramref name="ignore"/> 는 지금 옮기고 있는 부품이다 — 빼지 않으면 자기 자리에 자기가 스냅된다.
        /// </summary>
        private Vector3 SnapToGuides(Vector3 worldPoint, RocketPart ignore)
        {
            rocket.GetComponentsInChildren(_attached);
            _attachedLocal.Clear();
            for (int i = 0; i < _attached.Count; i++)
                if (_attached[i] != ignore)
                    _attachedLocal.Add(rocket.transform.InverseTransformPoint(_attached[i].transform.position));

            Alignment alignment = Align(rocket.transform.InverseTransformPoint(worldPoint),
                _attachedLocal, heightTolerance, azimuthTolerance);

            ShowGuides(alignment);
            return rocket.transform.TransformPoint(alignment.Local);
        }

        /// <summary>정렬 결과. 스냅된 축만 <c>true</c> 이고, 그 축의 가이드만 켜진다.</summary>
        public readonly struct Alignment
        {
            public readonly Vector3 Local;
            public readonly bool Height;
            public readonly bool Azimuth;

            public Alignment(Vector3 local, bool height, bool azimuth)
            {
                Local = local;
                Height = height;
                Azimuth = azimuth;
            }
        }

        /// <summary>
        /// 로켓 로컬 좌표에서 높이(<c>y</c>)와 방위각(<c>atan2(x, z)</c>)을 <b>독립적으로</b> 스냅한다 —
        /// Figma 가 x축·y축 가이드를 따로 잡아 주는 것과 같다. 반경은 표면이 정하므로 건드리지 않는다.
        /// 방위각 후보는 기존 엔진의 각도와 그 반대편(180°)이고, 기존 부품은 절대 움직이지 않는다.
        /// </summary>
        public static Alignment Align(Vector3 local, IReadOnlyList<Vector3> others,
            float heightTolerance, float azimuthTolerance)
        {
            float radius = Mathf.Sqrt(local.x * local.x + local.z * local.z);
            if (radius < MinRadius) return new Alignment(local, false, false);

            float sourceAzimuth = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            float height = local.y;
            float azimuth = sourceAzimuth;
            float heightGap = heightTolerance;
            float azimuthGap = azimuthTolerance;
            bool snappedHeight = false;
            bool snappedAzimuth = false;

            for (int i = 0; i < others.Count; i++)
            {
                Vector3 other = others[i];

                float gap = Mathf.Abs(other.y - local.y);
                if (gap < heightGap)
                {
                    heightGap = gap;
                    height = other.y;
                    snappedHeight = true;
                }

                if (other.x * other.x + other.z * other.z < MinRadius * MinRadius) continue;
                float theirs = Mathf.Atan2(other.x, other.z) * Mathf.Rad2Deg;

                // 같은 방위각(세로로 한 줄)과 반대편 방위각(대칭)이 둘 다 후보다.
                for (int side = 0; side < 2; side++)
                {
                    float candidate = theirs + side * 180f;
                    float delta = Mathf.Abs(Mathf.DeltaAngle(sourceAzimuth, candidate));
                    if (delta >= azimuthGap) continue;

                    azimuthGap = delta;
                    azimuth = candidate;
                    snappedAzimuth = true;
                }
            }

            // 아무 축도 안 걸렸으면 원래 좌표를 그대로 돌려준다 — 재구성 오차조차 남기지 않는다.
            if (!snappedHeight && !snappedAzimuth) return new Alignment(local, false, false);

            float rad = azimuth * Mathf.Deg2Rad;
            return new Alignment(new Vector3(Mathf.Sin(rad) * radius, height, Mathf.Cos(rad) * radius),
                snappedHeight, snappedAzimuth);
        }

        /// <summary>
        /// <paramref name="degrees"/> 를 <paramref name="step"/> 의 배수로 끌어당긴다. 허용치 밖이면
        /// 원본을 그대로 돌려준다 — 정렬 가이드와 같은 규약이라 "맞추려 했을 때만" 보정한다.
        /// </summary>
        public static float SnapAngle(float degrees, float step, float tolerance)
        {
            if (step <= 0f) return degrees;

            float nearest = Mathf.Round(degrees / step) * step;
            return Mathf.Abs(degrees - nearest) <= tolerance ? nearest : degrees;
        }

        /// <summary>
        /// 바깥 방향 <paramref name="outward"/>(단위 벡터) 로 OBB 를 밀어낼 거리. 부품 피봇이 기하
        /// 중심이라 표면 점에 그대로 놓으면 절반이 파묻힌다 — 이만큼 밀어야 표면에 얹힌다. 자세를
        /// 반영하므로 회전 기즈모로 눕힌 엔진도 파묻히지 않는다.
        /// </summary>
        public static float SupportRadius(Vector3 halfExtents, Quaternion rotation, Vector3 outward)
        {
            // 축을 돌려 내적한다 — Quaternion.Inverse 는 네이티브 호출이라 씬 없는 테스트에서 못 돈다.
            return Mathf.Abs(Vector3.Dot(rotation * Vector3.right, outward)) * halfExtents.x
                 + Mathf.Abs(Vector3.Dot(rotation * Vector3.up, outward)) * halfExtents.y
                 + Mathf.Abs(Vector3.Dot(rotation * Vector3.forward, outward)) * halfExtents.z;
        }

        /// <summary>
        /// 회전이 스냅에 걸린 프레임에만 기준선을 띄운다 — 이동 가이드와 같은 규약이라, 선이 보이면
        /// 곧 "지금 각도가 보정됐다"는 뜻이다. 회전 모드에서는 링이 꺼져 있어 세로선을 재사용한다.
        /// </summary>
        private void ShowRotationGuide(float snapped, bool active)
        {
            _axis.enabled = active;
            if (!active) return;

            Vector3 origin = _selected.transform.position;
            Vector3 direction = Quaternion.AngleAxis(snapped, _grabAxisWorld) * _grabReference;

            // _axis 는 로켓의 자식이고 useWorldSpace = false 다 — 로켓 로컬로 변환해 넣는다.
            _axis.SetPosition(0, rocket.transform.InverseTransformPoint(origin));
            _axis.SetPosition(1, rocket.transform.InverseTransformPoint(
                origin + direction * (GizmoScale(origin) * 1.3f)));
        }

        private void ShowGuides(Alignment alignment)
        {
            float radius = Mathf.Sqrt(alignment.Local.x * alignment.Local.x + alignment.Local.z * alignment.Local.z);

            _ring.enabled = alignment.Height;
            if (alignment.Height)
                for (int i = 0; i < RingSegments; i++)
                {
                    float angle = i * 2f * Mathf.PI / RingSegments;
                    _ring.SetPosition(i,
                        new Vector3(Mathf.Sin(angle) * radius, alignment.Local.y, Mathf.Cos(angle) * radius));
                }

            _axis.enabled = alignment.Azimuth;
            if (alignment.Azimuth)
            {
                _axis.SetPosition(0, new Vector3(alignment.Local.x, -guideHalfLength, alignment.Local.z));
                _axis.SetPosition(1, new Vector3(alignment.Local.x, guideHalfLength, alignment.Local.z));
            }
        }

        private void HideGuides()
        {
            if (_ring == null) return; // Start 전에 UI 가 모드를 바꾸는 경우

            _ring.enabled = false;
            _axis.enabled = false;
        }

        // 씬에 배치하지 않고 코드로 만든다 — 씬 YAML diff 를 늘리지 않기 위해서.
        private LineRenderer CreateGuide(string guideName, Transform parent, int points, bool loop, Color color)
        {
            var go = new GameObject(guideName);
            go.transform.SetParent(parent, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; // 부모 로컬 좌표로 그린다
            line.loop = loop;
            line.positionCount = points;
            line.widthMultiplier = guideWidth;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            // ponytail: Shader.Find 는 에디터 프로토타입 한정. 빌드에 넣으려면 guideMaterial 을 채운다.
            // URP Unlit 은 _ZTest 프로퍼티가 없어 ZTest LEqual 이 고정이다 — 기즈모를 부품 앞에
            // 항상 그리려면 Hidden/Internal-Colored + _ZTest Always 로 갈아타야 한다.
            // 인스턴스를 새로 만든다: 축마다 색이 달라 공유 머티리얼을 쓰면 마지막 색으로 통일된다.
            line.material = guideMaterial != null
                ? new Material(guideMaterial)
                : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            line.material.color = color;
            line.enabled = false;
            return line;
        }

        private void Select(RocketPart part)
        {
            UpdateRotationSound(false);
            if (_selected == part) return;

            // 아웃라인은 렌더러 피처가 화면 공간에서 그린다 — 부품 머티리얼은 건드리지 않는다.
            SelectionOutlineFeature.Select(cam, part != null ? part.gameObject : null);

            _selected = part;
            _mode = EditMode.None;
            _grabAxis = -1;
            Changed?.Invoke();
        }

        private void RequestCursor(bool overUI, Vector2 position)
        {
            if (rocket.Launched) return;

            if (_dragged != null)
            {
                // 슬롭을 넘겨 실제로 끌기 시작하면 부품이 커서 자리를 대신한다 — 커서가 겹치면 부착 지점을
                // 가린다. 붙는 자리인지 아닌지는 홀로그램이 알린다. 아직 안 움직인 클릭은 선택일 뿐이라
                // 커서를 감추면 누를 때마다 깜빡인다.
                if (_dragMoved) ArtemisCursor.Request(ArtemisCursor.Visual.Hidden, 20);
                else ArtemisCursor.Request(_overRocket ? ArtemisCursor.Visual.AttachValid : ArtemisCursor.Visual.AttachInvalid, 20);
                return;
            }

            if (_grabAxis < 0 && overUI) return;

            bool overHandle = _selected != null && _mode != EditMode.None &&
                (_grabAxis >= 0 || PickHandle(position) >= 0);
            if (overHandle && _mode == EditMode.Rotate)
            {
                ArtemisCursor.Request(ArtemisCursor.Visual.Rotate, 10);
                return;
            }

            if (overHandle && _mode == EditMode.Move)
            {
                ArtemisCursor.Request(ArtemisCursor.Visual.Drag, 10);
                return;
            }

            RocketPart part = PickPart(cam.ScreenPointToRay(position));
            if (part != null && (_mode != EditMode.None || part.HasStats))
            {
                ArtemisCursor.Request(ArtemisCursor.Visual.Hover);
            }
        }
    }
}
