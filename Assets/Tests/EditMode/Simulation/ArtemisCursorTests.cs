using System.Collections.Generic;
using System.Reflection;
using Border.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Simulation.Tests
{
    public sealed class ArtemisCursorTests
    {
        private readonly List<GameObject> roots = new();
        private EventSystem previousEventSystem;
        private bool previousCursorVisible;
        private EventSystem testEventSystem;
        private ArtemisCursor cursor;
        private Canvas canvas;
        private GraphicRaycaster raycaster;
        private Camera camera;
        private RenderTexture renderTexture;

        [SetUp]
        public void SetUp()
        {
            previousEventSystem = EventSystem.current;
            previousCursorVisible = Cursor.visible;
            testEventSystem = Root("CursorTestEvents").AddComponent<EventSystem>();
            // EventSystem does not run its lifecycle automatically in EditMode.
            typeof(EventSystem).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(testEventSystem, null);
            EventSystem.current = testEventSystem;
            GameObject host = Root("CursorTestHost");
            host.SetActive(false);
            cursor = host.AddComponent<ArtemisCursor>();
            GameObject canvasObject = Root("CursorTestCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            camera = Root("CursorTestCamera").AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 31;
            renderTexture = new RenderTexture(128, 128, 24);
            camera.targetTexture = renderTexture;
            canvasObject.layer = 31;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 32767;
            raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            typeof(BaseRaycaster).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(raycaster, null);
        }

        [TearDown]
        public void TearDown()
        {
            typeof(BaseRaycaster).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(raycaster, null);
            typeof(EventSystem).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(testEventSystem, null);
            for (int i = roots.Count - 1; i >= 0; i--) Object.DestroyImmediate(roots[i]);
            roots.Clear();
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Cursor.visible = previousCursorVisible;
            if (previousEventSystem != null) EventSystem.current = previousEventSystem;
        }

        [Test]
        public void DecorativePanel_DoesNotHover()
        {
            Graphic("Panel");
            Assert.That(Hovers(), Is.False);
        }

        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        public void Button_RequiresEnabledAndInteractable(bool enabled, bool interactable, bool expected)
        {
            Button button = Graphic("Button").AddComponent<Button>();
            button.enabled = enabled;
            button.interactable = interactable;
            Graphic("Label", button.transform);
            Assert.That(Hovers(), Is.EqualTo(expected));
        }

        [Test]
        public void MissingPointerData_WithCachedEventSystem_RebuildsDataAndStillHovers()
        {
            Graphic("Button").AddComponent<Button>();
            Assert.That(Hovers(), Is.True);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ArtemisCursor).GetField("pointerData", flags).SetValue(cursor, null);

            Assert.That(Hovers(), Is.True);
            Assert.That(typeof(ArtemisCursor).GetField("pointerData", flags).GetValue(cursor), Is.Not.Null);
        }

        [Test]
        public void DisabledCanvasGroup_DoesNotHoverButton()
        {
            GameObject panel = Graphic("Group");
            panel.AddComponent<CanvasGroup>().interactable = false;
            Graphic("Button", panel.transform).AddComponent<Button>();
            Assert.That(Hovers(), Is.False);
        }

        [Test]
        public void BlockingOverlay_DoesNotHoverButtonBehindIt()
        {
            Graphic("Button").AddComponent<Button>();
            GameObject overlay = Graphic("Overlay");
            Assert.That(Hovers(), Is.False);
            overlay.GetComponent<Image>().raycastTarget = false;
            Assert.That(Hovers(), Is.True);
        }

        [TestCase(EventTriggerType.PointerEnter, false)]
        [TestCase(EventTriggerType.PointerClick, true)]
        [TestCase(EventTriggerType.PointerDown, true)]
        [TestCase(EventTriggerType.BeginDrag, true)]
        public void EventTrigger_RequiresAnInteractiveEntry(EventTriggerType type, bool expected)
        {
            EventTrigger trigger = Graphic("Trigger").AddComponent<EventTrigger>();
            trigger.triggers.Add(new EventTrigger.Entry { eventID = type });
            Assert.That(Hovers(), Is.EqualTo(expected));
        }

        [Test]
        public void ScrollArea_DoesNotHover()
        {
            Graphic("ScrollArea").AddComponent<ScrollRect>();
            Assert.That(Hovers(), Is.False);
        }

        [Test]
        public void DragOnlyPresetEntry_HoversWhilePointerRemainsOverIt()
        {
            var type = typeof(RocketDesignUI).GetNestedType("PresetEntry", BindingFlags.NonPublic);
            Graphic("PresetEntry").AddComponent(type);
            Assert.That(Hovers(), Is.True);
            Assert.That(Hovers(), Is.True);
        }

        [TestCase("default", ArtemisCursor.Visual.Default)]
        [TestCase("hover", ArtemisCursor.Visual.Hover)]
        [TestCase("click_hold", ArtemisCursor.Visual.ClickHold)]
        [TestCase("drag", ArtemisCursor.Visual.Drag)]
        [TestCase("attach_valid", ArtemisCursor.Visual.AttachValid)]
        [TestCase("attach_invalid", ArtemisCursor.Visual.AttachInvalid)]
        [TestCase("rotate", ArtemisCursor.Visual.Rotate)]
        public void CursorPng_IsLoadedForItsStateAndReadable(string state, ArtemisCursor.Visual visual)
        {
            Texture2D texture = Resources.Load<Texture2D>($"Cursors/artemis_cursor_{state}");
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(64));
            Assert.That(texture.height, Is.EqualTo(64));
            Assert.That(texture.isReadable, Is.True);
            Assert.That(texture.mipmapCount, Is.EqualTo(1));
            typeof(ArtemisCursor).GetMethod("BuildSprites", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(cursor, null);
            var sprites = (System.Collections.IDictionary)typeof(ArtemisCursor)
                .GetField("sprites", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cursor);
            object sprite = sprites[visual];
            Assert.That(sprite.GetType().GetProperty("Texture").GetValue(sprite), Is.SameAs(texture));
        }

        [TestCase(true, false, ArtemisCursor.Visual.ClickHold)]
        [TestCase(true, true, ArtemisCursor.Visual.ClickHold)]
        [TestCase(false, true, ArtemisCursor.Visual.Hover)]
        [TestCase(false, false, ArtemisCursor.Visual.Default)]
        public void UnrequestedVisual_PrefersHeldClickOverHover(
            bool primaryPressed, bool overInteractive, ArtemisCursor.Visual expected)
        {
            object actual = typeof(ArtemisCursor)
                .GetMethod("ResolveUnrequestedVisual", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { primaryPressed, overInteractive });

            Assert.That(actual, Is.EqualTo(expected));
        }

        /// <summary>
        /// 엔진 드래그는 전용 PNG 대신 OS 커서를 감춘다 — 커서가 돌아오는 경로가
        /// <see cref="ArtemisCursor"/> 안에만 있어야 드래그가 어떻게 끝나든 새지 않는다.
        /// </summary>
        [Test]
        public void HiddenVisual_HidesSystemCursorAndAnyOtherVisualRestoresIt()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ArtemisCursor).GetMethod("BuildSprites", flags).Invoke(cursor, null);
            MethodInfo apply = typeof(ArtemisCursor).GetMethod("Apply", flags);

            apply.Invoke(cursor, new object[] { ArtemisCursor.Visual.Hidden });
            Assert.That(Cursor.visible, Is.False);

            apply.Invoke(cursor, new object[] { ArtemisCursor.Visual.Default });
            Assert.That(Cursor.visible, Is.True);
        }

        private bool Hovers()
        {
            Canvas.ForceUpdateCanvases();
            // GraphicRaycaster ignores graphics with depth -1 until the canvas has rendered.
            camera.Render();
            Vector2 position = RectTransformUtility.WorldToScreenPoint(camera,
                ((RectTransform)canvas.transform).TransformPoint(((RectTransform)canvas.transform).rect.center));
            return (bool)typeof(ArtemisCursor)
                .GetMethod("IsPointerOverInteractiveTarget", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(cursor, new object[] { position });
        }

        private GameObject Root(string name)
        {
            var root = new GameObject(name);
            roots.Add(root);
            return root;
        }

        private GameObject Graphic(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = 31;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent != null ? parent : canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return go;
        }
    }
}
