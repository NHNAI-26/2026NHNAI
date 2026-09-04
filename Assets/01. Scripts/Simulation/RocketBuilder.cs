using UnityEngine;
using UnityEngine.InputSystem;

namespace Border.Simulation
{
    /// <summary>씬 컨트롤러: 좌클릭 부품 부착, 우클릭 궤도 회전, 발사 키.</summary>
    [DisallowMultipleComponent]
    public sealed class RocketBuilder : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private Rocket rocket;

        [Header("Orbit camera")]
        [SerializeField] private float orbitSensitivity = 0.3f; // 도/픽셀
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float zoomSpeed = 0.01f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 60f;

        private RocketPart _dragged;
        private Collider _draggedCollider;
        private Vector3 _dragOrigin;
        private Plane _dragPlane;
        private bool _overRocket;
        private Vector3 _attachPoint;

        private float _yaw;
        private float _pitch;
        private float _distance;
        private Vector2 _lastMouse;

        private void Start()
        {
            Vector3 offset = cam.transform.position - rocket.transform.position;
            _distance = offset.magnitude;
            _yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Asin(Mathf.Clamp(offset.y / _distance, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                rocket.Launch();

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 position = mouse.position.ReadValue();
            Orbit(mouse, position);
            _lastMouse = position;

            if (mouse.leftButton.wasPressedThisFrame) BeginDrag(position);
            else if (_dragged == null) return;
            else if (mouse.leftButton.isPressed) Drag(position);
            else EndDrag();
        }

        private void LateUpdate()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            cam.transform.position = rocket.transform.position + rotation * new Vector3(0f, 0f, -_distance);
            cam.transform.rotation = rotation;
        }

        // 우클릭 드래그로 로켓 주위를 돈다. 좌클릭 부품 드래그와 버튼이 갈려 동시에 성립하지 않는다.
        private void Orbit(Mouse mouse, Vector2 position)
        {
            if (mouse.rightButton.isPressed && !mouse.rightButton.wasPressedThisFrame)
            {
                Vector2 delta = position - _lastMouse;
                _yaw += delta.x * orbitSensitivity;
                _pitch = Mathf.Clamp(_pitch - delta.y * orbitSensitivity, minPitch, maxPitch);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
                _distance = Mathf.Clamp(_distance - scroll * zoomSpeed * _distance, minDistance, maxDistance);
        }

        private void BeginDrag(Vector2 screenPosition)
        {
            if (rocket.Launched) return;
            if (!Physics.Raycast(cam.ScreenPointToRay(screenPosition), out RaycastHit hit)) return;

            RocketPart part = hit.collider.GetComponentInParent<RocketPart>();
            if (part == null) return;

            _dragged = part;
            _dragOrigin = part.transform.position;
            _dragPlane = new Plane(-cam.transform.forward, _dragOrigin);
            _overRocket = false;

            // 자기 콜라이더가 표면 레이캐스트를 가로막지 않게 잠시 끈다.
            _draggedCollider = part.GetComponent<Collider>();
            _draggedCollider.enabled = false;

            part.transform.SetParent(null, true); // 이미 붙어 있었다면 떼어낸다
        }

        private void Drag(Vector2 screenPosition)
        {
            Ray ray = cam.ScreenPointToRay(screenPosition);

            // 커서가 로켓 표면을 가리키면 그 지점이 곧 부착 지점이다. 깊이를 표면이 결정하므로
            // 카메라를 어느 각도로 돌려도 보이는 자리에 그대로 붙는다.
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponentInParent<Rocket>() == rocket)
            {
                _overRocket = true;
                _attachPoint = hit.point;
                _dragged.transform.position = hit.point;
                _dragged.transform.rotation = rocket.transform.rotation;
                return;
            }

            _overRocket = false;
            if (_dragPlane.Raycast(ray, out float distance))
                _dragged.transform.position = ray.GetPoint(distance);
        }

        private void EndDrag()
        {
            if (_overRocket) rocket.Attach(_dragged, _attachPoint);
            else _dragged.transform.position = _dragOrigin;

            _draggedCollider.enabled = true;
            _draggedCollider = null;
            _dragged = null;
        }
    }
}
