using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Border.UI
{
    /// <summary>
    /// ARTEMIS: 2026 공용 마우스 커서. 런타임에 작은 RGBA 텍스처를 그려 프로젝트 전역에 적용한다.
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

        private const int Size = 64;
        private static readonly Color32 Clear = new(0, 0, 0, 0);
        private static readonly Color32 Ink = new(2, 7, 11, 255);
        private static readonly Color32 Metal = new(238, 243, 243, 255);
        private static readonly Color32 MetalLight = new(255, 255, 255, 255);
        private static readonly Color32 MetalDark = new(70, 83, 88, 255);
        private static readonly Color32 Cyan = new(27, 232, 238, 255);
        private static readonly Color32 Orange = new(255, 138, 22, 255);
        private static readonly Color32 Red = new(255, 39, 62, 255);

        private static ArtemisCursor instance;
        private static int requestFrame = -1;
        private static int requestPriority = int.MinValue;
        private static Visual requestedVisual = Visual.Default;

        private readonly Dictionary<Visual, CursorSprite> sprites = new();
        private Visual currentVisual = (Visual)(-1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            requestFrame = -1;
            requestPriority = int.MinValue;
            requestedVisual = Visual.Default;
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
                : IsPointerOverUi()
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
            sprites[Visual.Default] = new CursorSprite(DrawArrow(), new Vector2(8f, 5f));
            sprites[Visual.Hover] = new CursorSprite(DrawHover(), new Vector2(8f, 5f));
            sprites[Visual.Drag] = new CursorSprite(DrawGrabber(), new Vector2(31f, 38f));
            sprites[Visual.AttachValid] = new CursorSprite(DrawAttach(false), new Vector2(18f, 18f));
            sprites[Visual.AttachInvalid] = new CursorSprite(DrawAttach(true), new Vector2(18f, 18f));
            sprites[Visual.Rotate] = new CursorSprite(DrawRotate(), new Vector2(32f, 32f));
        }

        private void Apply(Visual visual)
        {
            if (currentVisual == visual) return;
            if (!sprites.TryGetValue(visual, out CursorSprite sprite)) return;

            Cursor.SetCursor(sprite.Texture, sprite.Hotspot, CursorMode.Auto);
            currentVisual = visual;
        }

        private static bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private static Texture2D DrawArrow()
        {
            Texture2D texture = CreateTexture();
            DrawArrow(texture, 0f, 0f, 1f);
            return Finalize(texture);
        }

        private static Texture2D DrawHover()
        {
            Texture2D texture = CreateTexture();
            DrawBracket(texture, 5, 5, 54, 54, Cyan);
            DrawArrow(texture, 4f, 3f, 0.88f);
            return Finalize(texture);
        }

        private static Texture2D DrawGrabber()
        {
            Texture2D texture = CreateTexture();
            DrawLine(texture, 31, 9, 31, 22, Ink, 10);
            DrawLine(texture, 31, 9, 31, 22, MetalDark, 6);
            FillCircle(texture, 31, 27, 12, Ink);
            FillCircle(texture, 31, 27, 7, MetalDark);
            FillCircle(texture, 31, 27, 3, Cyan);
            DrawLine(texture, 21, 35, 10, 52, Ink, 10);
            DrawLine(texture, 41, 35, 52, 52, Ink, 10);
            DrawLine(texture, 21, 35, 10, 52, Metal, 6);
            DrawLine(texture, 41, 35, 52, 52, Metal, 6);
            DrawLine(texture, 13, 49, 22, 57, Orange, 4);
            DrawLine(texture, 49, 49, 40, 57, Orange, 4);
            return Finalize(texture);
        }

        private static Texture2D DrawAttach(bool invalid)
        {
            Texture2D texture = CreateTexture();
            DrawSnapRing(texture, 18, 18, invalid ? Red : Cyan);
            DrawArrow(texture, 13f, 16f, 0.73f);
            if (invalid)
            {
                DrawLine(texture, 3, 4, 35, 36, Ink, 8);
                DrawLine(texture, 3, 4, 35, 36, Red, 5);
            }

            return Finalize(texture);
        }

        private static Texture2D DrawRotate()
        {
            Texture2D texture = CreateTexture();
            DrawCircle(texture, 32, 32, 24, Ink, 7);
            DrawArc(texture, 32, 32, 24, 205f, 335f, Orange, 5);
            DrawArc(texture, 32, 32, 24, 30f, 175f, Cyan, 5);
            FillPolygon(texture, new[] { new Vector2(53, 18), new Vector2(62, 31), new Vector2(47, 30) }, Ink);
            FillPolygon(texture, new[] { new Vector2(53, 18), new Vector2(60, 29), new Vector2(48, 28) }, Cyan);
            DrawArrow(texture, 17f, 20f, 0.58f);
            return Finalize(texture);
        }

        private static void DrawArrow(Texture2D texture, float offsetX, float offsetY, float scale)
        {
            Vector2[] outer =
            {
                Point(8, 5), Point(55, 50), Point(34, 53), Point(25, 63), Point(16, 59), Point(23, 46), Point(5, 59),
            };
            Vector2[] inner =
            {
                Point(8, 5), Point(55, 50), Point(34, 53), Point(25, 63), Point(16, 59), Point(23, 46), Point(5, 59),
            };
            Vector2[] shine =
            {
                Point(15, 14), Point(44, 45), Point(31, 46), Point(20, 55), Point(24, 42), Point(12, 51),
            };
            Vector2[] accent = { Point(35, 44), Point(49, 47), Point(43, 38) };

            FillPolygon(texture, outer, Ink);
            FillPolygon(texture, inner, Metal);
            FillPolygon(texture, shine, MetalLight);
            DrawLine(texture, Point(18, 17), Point(29, 47), Cyan, Mathf.Max(2, Mathf.RoundToInt(4 * scale)));
            FillPolygon(texture, accent, Orange);
            DrawLine(texture, Point(17, 15), Point(50, 48), MetalLight, 1);

            Vector2 Point(float x, float y) => new(offsetX + x * scale, offsetY + y * scale);
        }

        private static void DrawBracket(Texture2D texture, int left, int top, int right, int bottom, Color32 color)
        {
            const int length = 15;
            const int width = 4;
            DrawLine(texture, left, top, left + length, top, Ink, width + 4);
            DrawLine(texture, left, top, left, top + length, Ink, width + 4);
            DrawLine(texture, right, top, right - length, top, Ink, width + 4);
            DrawLine(texture, right, top, right, top + length, Ink, width + 4);
            DrawLine(texture, left, bottom, left + length, bottom, Ink, width + 4);
            DrawLine(texture, left, bottom, left, bottom - length, Ink, width + 4);
            DrawLine(texture, right, bottom, right - length, bottom, Ink, width + 4);
            DrawLine(texture, right, bottom, right, bottom - length, Ink, width + 4);

            DrawLine(texture, left, top, left + length, top, color, width);
            DrawLine(texture, left, top, left, top + length, color, width);
            DrawLine(texture, right, top, right - length, top, color, width);
            DrawLine(texture, right, top, right, top + length, color, width);
            DrawLine(texture, left, bottom, left + length, bottom, color, width);
            DrawLine(texture, left, bottom, left, bottom - length, color, width);
            DrawLine(texture, right, bottom, right - length, bottom, color, width);
            DrawLine(texture, right, bottom, right, bottom - length, color, width);
        }

        private static void DrawSnapRing(Texture2D texture, int x, int y, Color32 color)
        {
            DrawCircle(texture, x, y, 15, Ink, 7);
            DrawCircle(texture, x, y, 15, color, 4);
            DrawCircle(texture, x, y, 5, color, 3);
            DrawLine(texture, x, y - 26, x, y - 13, Ink, 7);
            DrawLine(texture, x, y + 13, x, y + 26, Ink, 7);
            DrawLine(texture, x - 26, y, x - 13, y, Ink, 7);
            DrawLine(texture, x + 13, y, x + 26, y, Ink, 7);
            DrawLine(texture, x, y - 26, x, y - 13, color, 4);
            DrawLine(texture, x, y + 13, x, y + 26, color, 4);
            DrawLine(texture, x - 26, y, x - 13, y, color, 4);
            DrawLine(texture, x + 13, y, x + 26, y, color, 4);
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

        private static void DrawLine(Texture2D texture, Vector2 a, Vector2 b, Color32 color, int width) =>
            DrawLine(texture, Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(b.x),
                Mathf.RoundToInt(b.y), color, width);

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
