using System.Collections.Generic;
using System.Reflection;
using Border.Research;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Simulation.Tests
{
    public sealed class LaunchPhotoCaptureTests
    {
        private readonly List<Object> spawned = new();

        [SetUp]
        public void SetUp() => ResearchFlowSession.ResetForTests();

        [TearDown]
        public void TearDown()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null)
                    Object.DestroyImmediate(spawned[i]);

            spawned.Clear();
            ResearchFlowSession.ResetForTests();
        }

        [Test]
        public void RenderPhoto_MissingCameraOrSubjectReturnsNull()
        {
            GameObject subject = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            Camera camera = CreateCamera();

            Assert.That(LaunchPhotoCapture.RenderPhoto(null, subject.transform), Is.Null);
            Assert.That(LaunchPhotoCapture.RenderPhoto(camera, null), Is.Null);
        }

        [Test]
        public void RenderPhoto_CapturesFourByThreeNonBlankSubjectAndRestoresCanvas()
        {
            GameObject subject = CreateColoredSphere(Color.red);
            Camera camera = CreateCamera();
            Canvas canvas = CreateWorldCanvas(Color.blue);

            Texture2D photo = Track(LaunchPhotoCapture.RenderPhoto(camera, subject.transform));

            Assert.That(photo, Is.Not.Null);
            Assert.That(photo.width, Is.EqualTo(1024));
            Assert.That(photo.height, Is.EqualTo(768));
            Assert.That(canvas.enabled, Is.True);
            Assert.That(ContainsColor(photo, color => color.r > 0.35f && color.g < 0.2f && color.b < 0.2f), Is.True);
            Assert.That(ContainsColor(photo, color => color.b > 0.75f && color.r < 0.15f && color.g < 0.15f), Is.False);
        }

        [Test]
        public void RenderPhoto_WorksWithoutUi()
        {
            GameObject subject = CreateColoredSphere(Color.green);
            Camera camera = CreateCamera();

            Texture2D photo = Track(LaunchPhotoCapture.RenderPhoto(camera, subject.transform));

            Assert.That(photo, Is.Not.Null);
            Assert.That(photo.width, Is.EqualTo(1024));
            Assert.That(photo.height, Is.EqualTo(768));
            Assert.That(ContainsColor(photo, color => color.g > 0.35f && color.r < 0.25f && color.b < 0.25f), Is.True);
        }

        [Test]
        public void ExplosionWithoutVfx_CapturesBeforeRocketRenderersAreHiddenAndDoesNotReplace()
        {
            ResearchFlowSession session = BeginSessionLaunch();
            GameObject rocketObject = CreateColoredSphere(Color.red);
            rocketObject.name = "photo rocket";
            var body = rocketObject.AddComponent<Rigidbody>();
            var rocket = rocketObject.AddComponent<Rocket>();
            Invoke(rocket, "Awake");
            Camera camera = CreateCamera();
            var capture = rocketObject.AddComponent<LaunchPhotoCapture>();
            capture.Initialize(rocket, camera, session);
            rocket.AuthorizeLaunch = () => true;

            rocket.Launch();
            rocket.Explode();

            Texture2D photo = session.LaunchPhoto;
            Assert.That(photo, Is.Not.Null);
            Assert.That(ContainsColor(photo, color => color.r > 0.35f && color.g < 0.25f && color.b < 0.25f), Is.True);
            Assert.That(rocketObject.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(body.isKinematic, Is.True);

            capture.CaptureOutcome();
            Assert.That(session.LaunchPhoto, Is.SameAs(photo));
        }

        private ResearchFlowSession BeginSessionLaunch()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            Assert.That(session.TryEnterDesign(LaunchMissionId.LowAltitude, out _), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.TryBeginPendingDesignLaunch(), Is.EqualTo(ResearchActionResult.Success));
            return session;
        }

        private GameObject CreateColoredSphere(Color color)
        {
            GameObject subject = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            subject.transform.position = Vector3.zero;
            Material material = Track(new Material(FindUnlitShader()));
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            subject.GetComponent<Renderer>().sharedMaterial = material;
            return subject;
        }

        private Camera CreateCamera()
        {
            GameObject cameraObject = Track(new GameObject("test camera"));
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -6f), Quaternion.identity);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            return camera;
        }

        private Canvas CreateWorldCanvas(Color color)
        {
            GameObject canvasObject = Track(new GameObject("world canvas"));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -0.3f), Quaternion.identity);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(3f, 3f);
            GameObject imageObject = Track(new GameObject("blocking image"));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RectTransform imageRect = image.GetComponent<RectTransform>();
            imageRect.sizeDelta = new Vector2(3f, 3f);
            return canvas;
        }

        private static Shader FindUnlitShader()
        {
            return Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
        }

        private T Track<T>(T target) where T : Object
        {
            if (target != null) spawned.Add(target);
            return target;
        }

        private static bool ContainsColor(Texture2D texture, System.Predicate<Color> predicate)
        {
            Color32[] pixels = texture.GetPixels32();
            int step = Mathf.Max(1, pixels.Length / 4096);
            for (int i = 0; i < pixels.Length; i += step)
                if (predicate(pixels[i]))
                    return true;
            return false;
        }

        private static void Invoke(object target, string name) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
