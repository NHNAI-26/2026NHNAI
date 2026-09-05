using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Border.UI
{
    /// <summary>
    /// ARTEMIS: 2026 공용 PNG 마우스 커서와 런타임 툴바 아이콘.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArtemisCursor : MonoBehaviour
    {
        public enum Visual
        {
            Default,
            Hover,
            Drag,
            AttachValid,
            AttachInvalid,
            Rotate,
        }

        /// <summary>런타임에 그리는 UI 아이콘. 커서와 팔레트를 공유한다.</summary>
        public enum Icon
        {
            Move,
            Rotate,
            StageDot,
            StageDotHollow,
        }

        private const int Size = 64;
        private static readonly Color32 Clear = new(0, 0, 0, 0);
        private static readonly Color32 Ink = new(2, 7, 11, 255);
        private static readonly Color32 Metal = new(238, 243, 243, 255);
        private static readonly Color32 MetalLight = new(255, 255, 255, 255);
        private static readonly Color32 Cyan = new(27, 232, 238, 255);

        private static ArtemisCursor instance;
        private static int requestFrame = -1;
        private static int requestPriority = int.MinValue;
        private static Visual requestedVisual = Visual.Default;

        private static readonly Dictionary<Icon, Sprite> icons = new();

        private readonly Dictionary<Visual, CursorSprite> sprites = new();
        private readonly List<RaycastResult> pointerHits = new();
        private PointerEventData pointerData;
        private EventSystem pointerEventSystem;
        private Visual currentVisual = (Visual)(-1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            requestFrame = -1;
            requestPriority = int.MinValue;
            requestedVisual = Visual.Default;
            icons.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;

            var host = new GameObject("Artemis Cursor");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<ArtemisCursor>();
        }

        public static void Request(Visual visual, int priority = 0)
        {
            Bootstrap();

            int frame = Time.frameCount;
            if (requestFrame != frame)
            {
                requestFrame = frame;
                requestPriority = int.MinValue;
            }

            if (priority < requestPriority) return;

            requestedVisual = visual;
            requestPriority = priority;
        }

        /// <summary>
        /// 버튼에 붙일 수 있는 아이콘 스프라이트. 아이콘만 런타임에 그리므로 별도 에셋도
        /// 인스펙터 참조도 필요 없다 — 코드로 스폰되는 UI(<c>RocketDesignUI</c>)가 이 경로를 쓴다.
        /// </summary>
        public static Sprite IconSprite(Icon icon)
        {
            if (icons.TryGetValue(icon, out Sprite cached) && cached != null) return cached;

            Texture2D texture = icon switch
            {
                Icon.Move => DrawMove(),
                Icon.Rotate => DrawRotateIcon(),
                Icon.StageDot => DrawStageDot(true),
                _ => DrawStageDot(false),
            };
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            sprite.name = $"ArtemisIcon_{icon}";
            icons[icon] = sprite;
            return sprite;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildSprites();
            Apply(Visual.Default);
        }

        private void LateUpdate()
        {
            Visual visual = requestFrame == Time.frameCount
                ? requestedVisual
                : Mouse.current != null && IsPointerOverInteractiveTarget(Mouse.current.position.ReadValue())
                    ? Visual.Hover
                    : Visual.Default;

            Apply(visual);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                instance = null;
            }
        }

        private void BuildSprites()
        {
            LoadCursor(Visual.Default, "artemis_cursor_default", new Vector2(8f, 5f));
            LoadCursor(Visual.Hover, "artemis_cursor_hover", new Vector2(8f, 5f));
            LoadCursor(Visual.Drag, "artemis_cursor_drag", new Vector2(31f, 38f));
            LoadCursor(Visual.AttachValid, "artemis_cursor_attach_valid", new Vector2(18f, 18f));
            LoadCursor(Visual.AttachInvalid, "artemis_cursor_attach_invalid", new Vector2(18f, 18f));
            LoadCursor(Visual.Rotate, "artemis_cursor_rotate", new Vector2(32f, 32f));
        }

        private void LoadCursor(Visual visual, string resourceName, Vector2 hotspot)
        {
            Texture2D texture = Resources.Load<Texture2D>($"Cursors/{resourceName}");
            if (texture != null)
                sprites[visual] = new CursorSprite(texture, hotspot);
            else
                Debug.LogWarning($"Missing cursor PNG: Resources/Cursors/{resourceName}", this);
        }

        private void Apply(Visual visual)
        {
            if (currentVisual == visual) return;
            if (!sprites.TryGetValue(visual, out CursorSprite sprite))
                sprites.TryGetValue(Visual.Default, out sprite);

            Cursor.SetCursor(sprite.Texture, sprite.Hotspot, CursorMode.Auto);
            currentVisual = visual;
        }

        private bool IsPointerOverInteractiveTarget(Vector2 position)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (pointerEventSystem != eventSystem)
            {
                pointerEventSystem = eventSystem;
                pointerData = new PointerEventData(eventSystem);
            }

            pointerData.Reset();
            pointerData.position = position;
            pointerHits.Clear();
            eventSystem.RaycastAll(pointerData, pointerHits);

            // Only the first hit receives input; a decorative overlay can block a button below it.
            if (pointerHits.Count == 0) return false;
            GameObject target = pointerHits[0].gameObject;
            Selectable selectable = target.GetComponentInParent<Selectable>();
            if (selectable != null && (!selectable.IsActive() || !selectable.IsInteractable())) return false;
            GameObject press = ExecuteEvents.GetEventHandler<IPointerDownHandler>(target);
            if (press == null) press = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            if (press != null) return IsInteractiveHandler(press);

            GameObject drag = ExecuteEvents.GetEventHandler<IDragHandler>(target);
            return drag != null && drag.GetComponent<ScrollRect>() == null && IsInteractiveHandler(drag);
        }

        private static bool IsInteractiveHandler(GameObject target)
        {
            Selectable selectable = target.GetComponent<Selectable>();
            if (selectable != null) return selectable.IsActive() && selectable.IsInteractable();

            // EventTrigger implements every pointer interface, even when it has no click action.
            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                foreach (EventTrigger.Entry entry in trigger.triggers)
                    if (entry.eventID == EventTriggerType.PointerClick ||
                        entry.eventID == EventTriggerType.PointerDown ||
                        entry.eventID == EventTriggerType.BeginDrag ||
                        entry.eventID == EventTriggerType.Drag)
                        return true;
                return false;
            }

            return true;
        }

        // 아이콘은 커서와 달리 안에 마우스 화살표를 넣지 않는다 — 34px 버튼에서는 잡음이 된다.
        private static Texture2D DrawMove()
        {
            Texture2D texture = CreateTexture();
            DrawLine(texture, 32, 14, 32, 50, Ink, 15);
            DrawLine(texture, 14, 32, 50, 32, Ink, 15);
            FillPolygon(texture, new[] { new Vector2(32, 2), new Vector2(47, 19), new Vector2(17, 19) }, Ink);
            FillPolygon(texture, new[] { new Vector2(32, 62), new Vector2(47, 45), new Vector2(17, 45) }, Ink);
            FillPolygon(texture, new[] { new Vector2(2, 32), new Vector2(19, 17), new Vector2(19, 47) }, Ink);
            FillPolygon(texture, new[] { new Vector2(62, 32), new Vector2(45, 17), new Vector2(45, 47) }, Ink);
            DrawLine(texture, 32, 17, 32, 47, Metal, 8);
            DrawLine(texture, 17, 32, 47, 32, Metal, 8);
            FillPolygon(texture, new[] { new Vector2(32, 8), new Vector2(43, 20), new Vector2(21, 20) }, Metal);
            FillPolygon(texture, new[] { new Vector2(32, 56), new Vector2(43, 44), new Vector2(21, 44) }, Metal);
            FillPolygon(texture, new[] { new Vector2(8, 32), new Vector2(20, 21), new Vector2(20, 43) }, Metal);
            FillPolygon(texture, new[] { new Vector2(56, 32), new Vector2(44, 21), new Vector2(44, 43) }, Metal);
            FillCircle(texture, 32, 32, 4, Cyan);
            return Finalize(texture);
        }

        /// <summary>
        /// 비행 단계 표시의 점. 흰색으로만 그려 <see cref="UnityEngine.UI.Image.color"/> 가 색을 정하게 한다 —
        /// 상태별로 텍스처를 따로 만들지 않는다.
        /// </summary>
        private static Texture2D DrawStageDot(bool filled)
        {
            Texture2D texture = CreateTexture();
            if (filled) FillCircle(texture, 32, 32, 22, MetalLight);
            else DrawCircle(texture, 32, 32, 20, MetalLight, 5);
            return Finalize(texture);
        }

        private static Texture2D DrawRotateIcon()
        {
            Texture2D texture = CreateTexture();
            DrawArc(texture, 32, 32, 21, 20f, 320f, Ink, 15);
            DrawArc(texture, 32, 32, 21, 20f, 320f, Metal, 8);
            FillPolygon(texture, new[] { new Vector2(52, 10), new Vector2(64, 27), new Vector2(42, 29) }, Ink);
            FillPolygon(texture, new[] { new Vector2(52, 15), new Vector2(60, 26), new Vector2(45, 27) }, Cyan);
            return Finalize(texture);
        }

        private static Texture2D CreateTexture()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                texture.SetPixel(x, y, Clear);
            return texture;
        }

        private static Texture2D Finalize(Texture2D texture)
        {
            texture.Apply(false, true);
            return texture;
        }

        private static void FillPolygon(Texture2D texture, IReadOnlyList<Vector2> points, Color32 color)
        {
            int minX = Size;
            int minY = Size;
            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < points.Count; i++)
            {
                minX = Mathf.Min(minX, Mathf.FloorToInt(points[i].x));
                minY = Mathf.Min(minY, Mathf.FloorToInt(points[i].y));
                maxX = Mathf.Max(maxX, Mathf.CeilToInt(points[i].x));
                maxY = Mathf.Max(maxY, Mathf.CeilToInt(points[i].y));
            }

            minX = Mathf.Clamp(minX, 0, Size - 1);
            minY = Mathf.Clamp(minY, 0, Size - 1);
            maxX = Mathf.Clamp(maxX, 0, Size - 1);
            maxY = Mathf.Clamp(maxY, 0, Size - 1);

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                if (Contains(points, new Vector2(x + 0.5f, y + 0.5f)))
                    texture.SetPixel(x, Size - 1 - y, color);
        }

        private static bool Contains(IReadOnlyList<Vector2> points, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                bool crosses = points[i].y > point.y != points[j].y > point.y
                    && point.x < (points[j].x - points[i].x) * (point.y - points[i].y) /
                    (points[j].y - points[i].y) + points[i].x;
                if (crosses) inside = !inside;
            }

            return inside;
        }

        private static void FillCircle(Texture2D texture, int cx, int cy, int radius, Color32 color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                    Set(texture, x, y, color);
        }

        private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color32 color, int width)
        {
            int half = Mathf.Max(1, width / 2);
            int inner = (radius - half) * (radius - half);
            int outer = (radius + half) * (radius + half);
            for (int y = cy - radius - half; y <= cy + radius + half; y++)
            for (int x = cx - radius - half; x <= cx + radius + half; x++)
            {
                int d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d >= inner && d <= outer) Set(texture, x, y, color);
            }
        }

        private static void DrawArc(Texture2D texture, int cx, int cy, int radius, float from, float to,
            Color32 color, int width)
        {
            for (float angle = from; angle <= to; angle += 2f)
            {
                float rad = angle * Mathf.Deg2Rad;
                DrawDisc(texture, Mathf.RoundToInt(cx + Mathf.Cos(rad) * radius),
                    Mathf.RoundToInt(cy + Mathf.Sin(rad) * radius), width, color);
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int width)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps == 0)
            {
                DrawDisc(texture, x0, y0, width, color);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                DrawDisc(texture, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)),
                    Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), width, color);
            }
        }

        private static void DrawDisc(Texture2D texture, int cx, int cy, int width, Color32 color)
        {
            int radius = Mathf.Max(1, width / 2);
            FillCircle(texture, cx, cy, radius, color);
        }

        private static void Set(Texture2D texture, int x, int y, Color32 color)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return;
            texture.SetPixel(x, Size - 1 - y, color);
        }

        private readonly struct CursorSprite
        {
            public CursorSprite(Texture2D texture, Vector2 hotspot)
            {
                Texture = texture;
                Hotspot = hotspot;
            }

            public Texture2D Texture { get; }
            public Vector2 Hotspot { get; }
        }
    }
}
