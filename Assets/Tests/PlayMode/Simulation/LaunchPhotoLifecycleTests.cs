using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Border.Research;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class LaunchPhotoLifecycleTests
    {
        private readonly List<Object> spawned = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ResearchFlowSession.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DestroyPhotoExplosionClones();
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null)
                    Object.Destroy(spawned[i]);
            spawned.Clear();
            ResearchFlowSession.ResetForTests();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyingSessionReleasesLaunchPhoto()
        {
            ResearchFlowSession session = BeginSessionLaunch();
            Texture2D photo = CreatePhoto();
            session.SetLaunchPhoto(photo, session.LaunchPhotoGeneration);

            Object.Destroy(session.gameObject);
            yield return null;
            yield return null;

            Assert.That(session == null, Is.True);
            Assert.That(photo == null, Is.True);
        }

        [UnityTest]
        public IEnumerator VfxExplosionCapturesOnNextFrame()
        {
            ResearchFlowSession session = BeginSessionLaunch();
            Rocket rocket = CreateRocketWithCapture(session, out LaunchPhotoCapture capture, out _);
            SetExplosionPrefab(rocket, CreateExplosionPrefab());

            rocket.Launch();
            rocket.Explode();

            Assert.That(capture.IsCapturing, Is.True);
            Assert.That(session.LaunchPhoto, Is.Null);

            yield return null;

            Assert.That(capture.IsCapturing, Is.False);
            Assert.That(session.LaunchPhoto, Is.Not.Null);
            Assert.That(System.Array.Exists(session.LaunchPhoto.GetPixels32(),
                pixel => pixel.r > 90 && pixel.g < 50 && pixel.b < 50), Is.True,
                "The photograph must contain the explosion particles, not an empty background.");
        }

        [UnityTest]
        public IEnumerator PendingVfxCaptureRejectsStaleGenerationAfterSessionReset()
        {
            ResearchFlowSession session = BeginSessionLaunch();
            Rocket rocket = CreateRocketWithCapture(session, out LaunchPhotoCapture capture, out _);
            SetExplosionPrefab(rocket, CreateExplosionPrefab());

            rocket.Launch();
            rocket.Explode();
            Assert.That(capture.IsCapturing, Is.True);

            session.ResetResearch();
            yield return null;

            Assert.That(capture.IsCapturing, Is.False);
            Assert.That(session.LaunchPhoto, Is.Null);
        }

        [UnityTest]
        public IEnumerator SuccessCompletionCallbackSeesPhotoBeforeSceneCleanup()
        {
            ResearchFlowSession session = BeginSessionLaunch();
            Rocket rocket = CreateRocketWithCapture(session, out _, out _);
            // 결과 사진의 전달 순서를 검사하므로 준비 홀드를 생략한다.
            typeof(Rocket).GetField("holdSeconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(rocket, 0f);
            var controller = rocket.gameObject.AddComponent<LaunchMissionController>();
            bool callbackObservedPhoto = false;
            controller.Initialize(LaunchMissionId.LowAltitude, () => true, success =>
            {
                Assert.That(success, Is.True);
                Assert.That(session.LaunchPhoto, Is.Not.Null);
                callbackObservedPhoto = true;
            });

            rocket.Launch();
            rocket.transform.position = Vector3.up * 100f;
            Invoke(controller, "FixedUpdate");

            Assert.That(callbackObservedPhoto, Is.True);
            Assert.That(session.HasUnacknowledgedLaunchResult, Is.False);
            Assert.That(session.LaunchPhoto, Is.Not.Null);
            yield return null;
        }

        private static ResearchFlowSession BeginSessionLaunch()
        {
            ResearchFlowSession session = ResearchFlowSession.GetOrCreate();
            // 새 게임은 프리셋 0개로 시작한다 — 설계에 들어가려면 먼저 하나 만들어야 한다.
            session.Model.CreateNewEnginePreset(out _);
            Assert.That(session.TryEnterDesign(LaunchMissionId.LowAltitude, out _), Is.EqualTo(ResearchActionResult.Success));
            Assert.That(session.TryBeginPendingDesignLaunch(), Is.EqualTo(ResearchActionResult.Success));
            return session;
        }

        private Rocket CreateRocketWithCapture(ResearchFlowSession session, out LaunchPhotoCapture capture, out Camera camera)
        {
            var rocketObject = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            rocketObject.name = "playmode photo rocket";
            rocketObject.transform.position = Vector3.zero;
            rocketObject.GetComponent<Renderer>().sharedMaterial = CreateMaterial(Color.red);
            rocketObject.AddComponent<Rigidbody>();
            Rocket rocket = rocketObject.AddComponent<Rocket>();
            camera = CreateCamera();
            capture = rocketObject.AddComponent<LaunchPhotoCapture>();
            capture.Initialize(rocket, camera, session);
            rocket.AuthorizeLaunch = () => true;
            return rocket;
        }

        private Camera CreateCamera()
        {
            var cameraObject = Track(new GameObject("playmode photo camera"));
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -6f), Quaternion.identity);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            return camera;
        }

        private ParticleSystem CreateExplosionPrefab()
        {
            var effect = Track(new GameObject("photo explosion prefab"));
            effect.transform.position = Vector3.one * 10000f;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 3f;
            main.startColor = Color.red;
            main.maxParticles = 8;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = CreateMaterial(Color.red);
            return particles;
        }

        private Material CreateMaterial(Color color)
        {
            var material = Track(new Material(FindUnlitShader()));
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        private static Shader FindUnlitShader()
        {
            return Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
        }

        private static Texture2D CreatePhoto()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "playmode session photo" };
            texture.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.white });
            texture.Apply(false, false);
            return texture;
        }

        private static void SetExplosionPrefab(Rocket rocket, ParticleSystem prefab) =>
            typeof(Rocket).GetField("explosionPrefab", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(rocket, prefab);

        private T Track<T>(T target) where T : Object
        {
            if (target != null) spawned.Add(target);
            return target;
        }

        private static void Invoke(object target, string name) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        private static void DestroyPhotoExplosionClones()
        {
            foreach (ParticleSystem particles in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (particles != null && particles.name.StartsWith("photo explosion prefab"))
                    Object.Destroy(particles.gameObject);
        }
    }
}
