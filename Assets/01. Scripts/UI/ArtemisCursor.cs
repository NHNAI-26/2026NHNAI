using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Border.UI
{
    /// <summary>
    /// ARTEMIS: 2026 공용 마우스 커서. PNG 리소스를 프로젝트 전역 커서로 적용한다.
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

        private const string ResourceRoot = "Cursors/";

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
            LoadSprites();
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
            if (instance != this) return;

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            instance = null;
        }

        private void LoadSprites()
        {
            Add(Visual.Default, "artemis_cursor_default", new Vector2(8f, 5f));
            Add(Visual.Hover, "artemis_cursor_hover", new Vector2(8f, 5f));
            Add(Visual.Drag, "artemis_cursor_drag", new Vector2(31f, 38f));
            Add(Visual.AttachValid, "artemis_cursor_attach_valid", new Vector2(18f, 18f));
            Add(Visual.AttachInvalid, "artemis_cursor_attach_invalid", new Vector2(18f, 18f));
            Add(Visual.Rotate, "artemis_cursor_rotate", new Vector2(32f, 32f));
        }

        private void Add(Visual visual, string name, Vector2 hotspot)
        {
            string path = ResourceRoot + name;
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning($"ArtemisCursor: missing cursor texture at Resources/{path}.", this);
                return;
            }

            sprites[visual] = new CursorSprite(texture, hotspot);
        }

        private void Apply(Visual visual)
        {
            if (currentVisual == visual) return;

            if (!sprites.TryGetValue(visual, out CursorSprite sprite) &&
                !sprites.TryGetValue(Visual.Default, out sprite))
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                currentVisual = visual;
                return;
            }

            Cursor.SetCursor(sprite.Texture, sprite.Hotspot, CursorMode.Auto);
            currentVisual = visual;
        }

        private static bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

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
