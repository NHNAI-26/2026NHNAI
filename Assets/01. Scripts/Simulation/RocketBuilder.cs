using System;
using System.Collections.Generic;
using Border.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Simulation
{
    /// <summary>
    /// 씬 컨트롤러: 좌클릭 부품 부착·선택, 우클릭 궤도 회전, 발사 키.
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

        [Header("Orbit camera")]
        [SerializeField] private float orbitSensitivity = 0.3f; // 도/픽셀
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float zoomSpeed = 0.01f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 60f;

        [Header("Part editing")]
        [SerializeField] private float rotateSensitivity = 0.5f; // 도/픽셀

        [Header("Alignment guides")]
        [SerializeField] private float heightTolerance = 0.25f; // m
        [SerializeField] private float azimuthTolerance = 20f;  // 도
        [SerializeField] private float guideHalfLength = 2.2f;  // 세로선 절반 길이(m)
        [SerializeField] private float guideWidth = 0.02f;
        [SerializeField] private Color guideColor = new(0.2f, 0.9f, 1f);
        [SerializeField] private Material guideMaterial; // 비우면 URP Unlit 을 런타임에 찾는다

        private const int RingSegments = 32;
        private const float MinRadius = 1e-3f; // 축 위에서는 방위각이 정의되지 않는다

        private readonly List<RocketPart> _attached = new();
        private readonly List<Vector3> _attachedLocal = new();
        private LineRenderer _ring;
        private LineRenderer _axis;

        private RocketPart _dragged;
        private Collider _draggedCollider;
        private Transform _dragParent;
        private Vector3 _dragOrigin;
        private Quaternion _dragOriginRotation;
        private Plane _dragPlane;
        private bool _overRocket;
        private bool _overGround;
        private bool _spawnedFromPreset;
        private Vector3 _attachPoint;

        private RocketPart _selected;
        private EditMode _mode;

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
            Vector3 offset = cam.transform.position - rocket.transform.position;
            _distance = offset.magnitude;
            _yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Asin(Mathf.Clamp(offset.y / _distance, -1f, 1f)) * Mathf.Rad2Deg;

            _ring = CreateGuide("AlignmentRing", RingSegments, true);
            _axis = CreateGuide("AlignmentAxis", 2, false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame) rocket.Launch();
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

            Orbit(mouse, delta, overUI);
            _lastMouse = position;

            if (_mode != EditMode.None)
            {
                EditSelected(mouse, position, delta, overUI);
                return;
            }

            if (_dragged != null)
            {
                if (mouse.leftButton.isPressed) Drag(position);
                else EndDrag();
                return;
            }

            if (overUI || !mouse.leftButton.wasPressedThisFrame) return;

            BeginDrag(position);
            if (_dragged == null) Select(null); // 빈 공간 클릭은 선택 해제
        }

        private void LateUpdate()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            cam.transform.position = rocket.transform.position + rotation * new Vector3(0f, 0f, -_distance);
            cam.transform.rotation = rotation;
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
            if (_selected == null) mode = EditMode.None;
            if (mode == _mode) mode = EditMode.None;
            if (mode == _mode) return;

            // 이동 중에는 자기 콜라이더가 표면 레이캐스트를 가로막는다.
            if (_selected != null && _selected.TryGetComponent(out Collider collider))
                collider.enabled = mode != EditMode.Move;

            _mode = mode;
            if (mode != EditMode.Move) HideGuides(); // 이동을 끝내면 가이드도 같이 사라져야 한다
            Changed?.Invoke();
        }

        public void DeleteSelected()
        {
            if (_selected == null || rocket.Launched) return;

            Destroy(_selected.gameObject);
            _mode = EditMode.None;
            _selected = null;
            Changed?.Invoke();
        }

        // ---- 드래그 -------------------------------------------------------------------------

        private void BeginDrag(Vector2 screenPosition)
        {
            if (rocket.Launched) return;
            if (!Physics.Raycast(cam.ScreenPointToRay(screenPosition), out RaycastHit hit)) return;

            RocketPart part = hit.collider.GetComponentInParent<RocketPart>();
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
            _dragOriginRotation = part.transform.rotation;
            _dragPlane = new Plane(-cam.transform.forward, _dragOrigin);
            _overRocket = false;
            _overGround = false;

            // 자기 콜라이더가 표면 레이캐스트를 가로막지 않게 잠시 끈다.
            _draggedCollider = part.GetComponent<Collider>();
            _draggedCollider.enabled = false;

            part.transform.SetParent(null, true); // 이미 붙어 있었다면 떼어낸다
            Select(part);
            Drag(screenPosition);
        }

        private void Drag(Vector2 screenPosition)
        {
            Ray ray = cam.ScreenPointToRay(screenPosition);
            _overRocket = false;
            _overGround = false;

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
                    _overGround = true; // 지면이든 다른 물체든, 놓을 자리는 있다
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

            if (_overRocket)
            {
                rocket.Attach(part, _attachPoint);
            }
            else if (_overGround)
            {
                // 바닥에 그대로 둔다. 로켓의 자식이 아니므로 발사해도 따라가지 않는다.
            }
            else if (_spawnedFromPreset)
            {
                // 허공에 놓은 새 엔진은 회수할 방법이 없다 — 꺼낸 적 없던 것으로 되돌린다.
                Destroy(part.gameObject);
                _spawnedFromPreset = false;
                Select(null);
                return;
            }
            else
            {
                part.transform.SetParent(_dragParent, true);
                part.transform.position = _dragOrigin;
                part.transform.rotation = _dragOriginRotation;
            }

            _spawnedFromPreset = false;
        }

        // ---- 선택 부품 편집 -----------------------------------------------------------------

        private void EditSelected(Mouse mouse, Vector2 position, Vector2 delta, bool overUI)
        {
            if (_selected == null)
            {
                SetMode(EditMode.None);
                return;
            }

            if (_mode == EditMode.Move)
            {
                if (Physics.Raycast(cam.ScreenPointToRay(position), out RaycastHit hit) &&
                    hit.collider.GetComponentInParent<Rocket>() == rocket)
                    _selected.transform.position = SnapToGuides(hit.point, _selected);
                else
                    HideGuides();

                if (mouse.leftButton.wasPressedThisFrame && !overUI) SetMode(EditMode.None); // 클릭으로 확정
                return;
            }

            HideGuides();

            // 회전: 화면 기준으로 돌린다. 가로 드래그는 카메라 up, 세로 드래그는 카메라 right 축.
            // 축·각도 제한을 두지 않으므로 뒤집힌 배치도 그대로 허용된다(추력이 그 방향으로 나간다).
            if (!mouse.leftButton.isPressed || overUI) return;

            _selected.transform.Rotate(cam.transform.up, delta.x * rotateSensitivity, Space.World);
            _selected.transform.Rotate(cam.transform.right, -delta.y * rotateSensitivity, Space.World);
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
        private LineRenderer CreateGuide(string guideName, int points, bool loop)
        {
            var go = new GameObject(guideName);
            go.transform.SetParent(rocket.transform, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; // 로켓 로컬 좌표로 그린다
            line.loop = loop;
            line.positionCount = points;
            line.widthMultiplier = guideWidth;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            // ponytail: Shader.Find 는 에디터 프로토타입 한정. 빌드에 넣으려면 guideMaterial 을 채운다.
            line.material = guideMaterial != null
                ? guideMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            line.material.color = guideColor;
            line.enabled = false;
            return line;
        }

        private void Select(RocketPart part)
        {
            if (_selected == part) return;

            if (_mode == EditMode.Move && _selected != null && _selected.TryGetComponent(out Collider collider))
                collider.enabled = true;

            _selected = part;
            _mode = EditMode.None;
            Changed?.Invoke();
        }
    }
}
