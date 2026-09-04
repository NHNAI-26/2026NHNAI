using System;
using System.Collections.Generic;
using Border.Core;
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
        [Tooltip("발사 뷰가 로켓에서 떨어져 있는 거리(m).")]
        [SerializeField] private float launchDistance = 40f;
        [SerializeField] private float launchBlendSeconds = 1.5f;

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

        [Header("Alignment guides")]
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
        private const float HandleGrabPixels = 14f;
        private const float DragSlopPixels = 4f;

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

        public Camera Cam => cam;
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

            _ring = CreateGuide("AlignmentRing", rocket.transform, RingSegments, true, guideColor);
            _axis = CreateGuide("AlignmentAxis", rocket.transform, 2, false, guideColor);

            CacheBodyShape();
            BuildGizmo();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame && !rocket.Launched)
                {
                    rocket.Launch();
                    // 발사한 뒤에는 편집 상태가 남으면 안 된다 — 기즈모가 날아가는 부품을 계속 따라다니고
                    // 이동·회전 버튼도 켜진 채로 남는다. 선택이 없을 때도 UI 가 갱신되도록 직접 알린다.
                    Select(null);
                    Changed?.Invoke();
                    PlaceLaunchCamera();
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
        /// 발사 뷰를 플레이어가 마지막으로 보고 있던 방위각에 세운다 — 반대편에서 컷하면 방향 감각이 끊긴다.
        /// 피치는 0 이라 로켓이 화면 한가운데 놓이고, 이후 <see cref="LateUpdate"/> 가 높이만 갱신한다.
        /// </summary>
        private void PlaceLaunchCamera()
        {
            Quaternion rotation = Quaternion.Euler(0f, _yaw, 0f);
            launchCam.transform.SetPositionAndRotation(
                rocket.transform.position + rotation * new Vector3(0f, 0f, -launchDistance), rotation);
            launchCam.Priority = 20; // PrioritySettings 는 int 암시 변환
        }

        private void LateUpdate()
        {
            if (rocket.Launched)
            {
                // 발사 뷰는 Y 만 따라간다 — X/Z 와 자세를 고정해야 상승이 상승으로 읽힌다.
                Vector3 position = launchCam.transform.position;
                position.y = rocket.transform.position.y;
                launchCam.transform.position = position;
            }
            else
            {
                Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                designCam.transform.SetPositionAndRotation(
                    rocket.transform.position + rotation * new Vector3(0f, 0f, -_distance), rotation);
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
            _grabAxis = -1;
            _mode = mode;
            if (mode != EditMode.Move) HideGuides(); // 이동을 끝내면 가이드도 같이 사라져야 한다
            Changed?.Invoke();
        }

        public void DeleteSelected()
        {
            if (_selected == null || rocket.Launched) return;

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
        /// 엔진은 중심이 표면에 놓여 절반이 본체에 파묻히므로, 실루엣 근처를 정확히 찍어도
        /// 본체 캡슐이 먼저 맞아 집기가 실패했다. 클릭 한 번뿐인 경로라 RaycastAll 로 충분하다.
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
            _overRocket = false;

            // 커서가 가리키는 표면이 깊이를 결정하므로 카메라를 어느 각도로 돌려도 보이는 자리에 놓인다.
            // 자세는 건드리지 않는다 — 회전시킨 자세가 곧 추력 방향이라 임의로 세우면 힘이 바뀐다.
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.GetComponentInParent<Rocket>() == rocket)
                {
                    _overRocket = true;
                    _attachPoint = SnapToGuides(hit.point, _dragged);
                    _dragged.transform.position = _attachPoint;
                }
                else
                {
                    // 지면이든 다른 물체든 붙을 곳은 아니다 — 커서만 따라가다 놓으면 사라진다.
                    _dragged.transform.position = hit.point;
                    HideGuides();
                }

                return;
            }

            HideGuides();
            if (_dragPlane.Raycast(ray, out float distance))
                _dragged.transform.position = ray.GetPoint(distance);
        }

        private void EndDrag()
        {
            RocketPart part = _dragged;
            _dragged = null;
            _draggedCollider.enabled = true;
            _draggedCollider = null;
            HideGuides();

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
                SetMode(EditMode.None); // 핸들을 빗나간 클릭이 곧 확정이다
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
                part.position = ProjectOntoBody(SnapToGuides(wanted, _selected));
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
            float bestDistance = HandleGrabPixels;

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
                    // 32점 폴리라인을 훑지 않는다. 링 평면과의 교점 각도를 구해 그 각도의 링 위
                    // 점 하나만 화면에 찍어 비교한다 — 결과는 같고 비스듬히 볼 때 더 정확하다.
                    Vector3 reference = part.rotation * Axis((i + 1) % 3);
                    if (!AngleOnPlane(origin, axis, reference, ray, out float angle)) continue;
                    if (!ScreenPoint(origin + Quaternion.AngleAxis(angle, axis) * reference * scale, out Vector2 p))
                        continue;
                    distance = Vector2.Distance(screen, p);
                }

                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
            }

            return best;
        }

        private bool ScreenPoint(Vector3 worldPoint, out Vector2 screen)
        {
            Vector3 point = cam.WorldToScreenPoint(worldPoint);
            screen = point;
            return point.z > 0f; // 카메라 뒤 좌표는 좌우가 뒤집혀 나온다
        }

        private Vector3 ProjectOntoBody(Vector3 worldPoint) =>
            rocket.transform.TransformPoint(ProjectOntoCapsule(
                rocket.transform.InverseTransformPoint(worldPoint), _bodyHalfSegment, _bodyRadius,
                rocket.transform.InverseTransformPoint(_grabPosition)));

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

            Vector3 scale = body.transform.lossyScale;
            _bodyRadius = body.radius * Mathf.Max(scale.x, scale.z); // 유니티의 캡슐 스케일 규칙
            _bodyHalfSegment = Mathf.Max(0f, body.height * 0.5f * scale.y - _bodyRadius);
        }

        private void BuildGizmo()
        {
            _gizmoRoot = new GameObject("PartGizmo").transform;
            _gizmoRoot.SetParent(rocket.transform, false); // 로켓 스케일이 1 이라 localScale 이 곧 월드 배율
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
            _gizmoRoot.localScale = Vector3.one * scale;

            // 선 굵기는 트랜스폼 스케일을 따라가지 않는다 — 맞춰 주지 않으면 줌아웃에서 머리카락이 된다.
            for (int i = 0; i < _gizmo.Length; i++) _gizmo[i].widthMultiplier = guideWidth * scale;
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
        /// 회전이 스냅에 걸린 프레임에만 기준선을 띄운다 — 이동 가이드와 같은 규약이라, 선이 보이면
        /// 곧 "지금 각도가 보정됐다"는 뜻이다. 회전 모드에서는 링이 꺼져 있어 세로선을 재사용한다.
        /// </summary>
        private void ShowRotationGuide(float snapped, bool active)
        {
            _axis.enabled = active;
            if (!active) return;

            Vector3 origin = _selected.transform.position;
            Vector3 direction = Quaternion.AngleAxis(snapped, _grabAxisWorld) * _grabReference;

            // _axis 는 로켓의 자식이고 useWorldSpace = false 다(로켓 스케일 1).
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
            if (_selected == part) return;

            _selected = part;
            _mode = EditMode.None;
            _grabAxis = -1;
            Changed?.Invoke();
        }
    }
}
