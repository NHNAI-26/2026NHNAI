#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Border.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class MissionSuccessPresentationTests
    {
        private GameObject rocketObject;
        private Camera camera;
        private MissionSuccessPresentation presentation;
        private Transform engine;
        private GameObject scenery;
        private GameObject soundRoot;

        [SetUp]
        public void SetUp()
        {
            rocketObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rocketObject.name = "success presentation test rocket";
            rocketObject.transform.position = new Vector3(300f, 500f, 300f);
            rocketObject.transform.localScale = new Vector3(2f, 6f, 2f);
            rocketObject.AddComponent<Rocket>();
            engine = new GameObject("attached engine").transform;
            engine.SetParent(rocketObject.transform, false);
            var presenter = new GameObject("body presentation");
            presenter.transform.SetParent(rocketObject.transform, false);
            presentation = presenter.AddComponent<MissionSuccessPresentation>();
            scenery = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SetField("parachutePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Simulation/Success/MissionSuccessParachute.prefab"));
            SetField("swayClip", AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/03. Prefabs/Simulation/Success/ParachuteSway.anim"));
            camera = new GameObject("success test camera").AddComponent<Camera>();
            camera.aspect = 16f / 9f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (presentation != null) presentation.End();
            if (rocketObject != null) Object.Destroy(rocketObject);
            if (camera != null) Object.Destroy(camera.gameObject);
            if (scenery != null) Object.Destroy(scenery);
            if (soundRoot != null) Object.Destroy(soundRoot);
            yield return null;
        }

        [Test]
        public void SixSecondDescent_EntersFromAbove_LeavesBelow_AndPreservesRocket()
        {
            Vector3 position = rocketObject.transform.position;
            Vector3 scale = rocketObject.transform.lossyScale;
            Assert.That(presentation.Begin(camera), Is.True);
            presentation.Evaluate(MissionSuccessPresentation.CameraReleaseDuration);
            Assert.That(camera.enabled, Is.False);
            Assert.That(scenery.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(MissionSuccessPresentation.DescentDuration, Is.EqualTo(6f));
            Assert.That(MissionSuccessPresentation.Duration, Is.EqualTo(7f));
            Assert.That(rocketObject.GetComponent<Rocket>().FlightStopped, Is.True);
            Assert.That(rocketObject.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(rocketObject.transform.parent.name, Is.EqualTo("RocketSocket"));
            Assert.That(engine.parent, Is.EqualTo(rocketObject.transform));
            Assert.That(Vector3.Distance(rocketObject.transform.lossyScale, scale), Is.LessThan(0.001f));
            AssertAllOutside(true);

            presentation.Evaluate(4f);
            float middle = presentation.PresentationCamera.WorldToViewportPoint(rocketObject.transform.position).y;
            Assert.That(middle, Is.InRange(0f, 1f));
            presentation.Evaluate(7f);
            AssertAllOutside(false);
            presentation.End();
            Assert.That(camera.enabled, Is.True);
            Assert.That(scenery.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(rocketObject.transform.parent, Is.Null);
            Assert.That(rocketObject.transform.position, Is.EqualTo(position));
            Assert.That(Vector3.Distance(rocketObject.transform.lossyScale, scale), Is.LessThan(0.001f));
            Assert.That(engine.parent, Is.EqualTo(rocketObject.transform));
        }

        [UnityTest]
        public IEnumerator CameraRelease_HoldsCameraForOneSecond_WhileRocketKeepsMoving()
        {
            var brain = (Behaviour)camera.gameObject.AddComponent(
                System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine"));
            Vector3 position = rocketObject.transform.position;
            Vector3 cameraPosition = camera.transform.position;
            Quaternion cameraRotation = camera.transform.rotation;
            Vector3 velocity = new Vector3(5f, 30f, -2f);
            float timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                Assert.That(presentation.Begin(camera, velocity), Is.True);
                Assert.That(brain.enabled, Is.False);
                presentation.Evaluate(0.5f);
                yield return null;
                Assert.That(rocketObject.transform.position, Is.EqualTo(position + velocity * 0.5f));
                Assert.That(rocketObject.transform.parent, Is.Null);
                Assert.That(camera.enabled, Is.True);
                Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
                Assert.That(camera.transform.rotation, Is.EqualTo(cameraRotation));
                Assert.That(scenery.GetComponent<Renderer>().enabled, Is.True);
                presentation.Evaluate(0.999f);
                Assert.That(presentation.PresentationCamera, Is.Null);
                presentation.Evaluate(1f);
                Assert.That(presentation.PresentationCamera, Is.Not.Null);
                Assert.That(camera.enabled, Is.False);
                AssertAllOutside(true);
                presentation.End();
                Assert.That(brain.enabled, Is.True);
                Assert.That(rocketObject.transform.position, Is.EqualTo(position));
            }
            finally
            {
                Time.timeScale = timeScale;
            }
        }

        [Test]
        public void DisableDuringCameraRelease_RestoresControllersAndRocket()
        {
            var brain = (Behaviour)camera.gameObject.AddComponent(
                System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine"));
            Vector3 position = rocketObject.transform.position;
            Assert.That(presentation.Begin(camera, Vector3.up * 30f), Is.True);
            presentation.Evaluate(0.5f);
            presentation.enabled = false;
            Assert.That(presentation.IsPlaying, Is.False);
            Assert.That(presentation.PresentationCamera, Is.Null);
            Assert.That(brain.enabled, Is.True);
            Assert.That(camera.enabled, Is.True);
            Assert.That(rocketObject.transform.position, Is.EqualTo(position));
            Assert.That(scenery.GetComponent<Renderer>().enabled, Is.True);
        }

        [TestCase(16f / 9f, 6f)]
        [TestCase(9f / 16f, 12f)]
        public void ParachuteSound_PlaysJustBeforeRocketEntry_Once_AndStopsOnEnd(float aspect, float height)
        {
            CreateSoundManager(out _);
            rocketObject.transform.localScale = new Vector3(2f, height, 2f);
            camera.aspect = aspect;
            Assert.That(presentation.Begin(camera, Vector3.up * 20f), Is.True);
            presentation.Evaluate(0.99f);
            Assert.That(ParachuteVoiceCount(), Is.Zero);
            presentation.Evaluate(1f);
            Assert.That(ParachuteVoiceCount(), Is.Zero);

            float soundTime = -1f;
            float entryTime = -1f;
            for (int frame = 1; frame <= 240; frame++)
            {
                float elapsed = 1f + frame / 120f;
                presentation.Evaluate(elapsed);
                if (soundTime < 0f && ParachuteVoiceCount() > 0) soundTime = elapsed;
                float bottom = presentation.PresentationCamera.WorldToViewportPoint(
                    rocketObject.GetComponent<Renderer>().bounds.min).y;
                if (entryTime < 0f && bottom <= 1f) entryTime = elapsed;
            }
            Assert.That(soundTime, Is.GreaterThan(1f));
            Assert.That(entryTime - soundTime, Is.InRange(0.13f, 0.17f));
            presentation.Evaluate(5f);
            Assert.That(ParachuteVoiceCount(), Is.EqualTo(1));
            var source = soundRoot.GetComponentsInChildren<AudioSource>()
                .Single(s => s.clip != null && s.clip.name == "parachute");
            Assert.That(source.loop, Is.False);
            Assert.That(source.spatialBlend, Is.Zero);
            presentation.End();
            Assert.That(ParachuteVoiceCount(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator ParachuteMusic_StartsWhenTrackingStops_AndContinuesAcrossCameraCut()
        {
            var manager = CreateSoundManager(out BgmPlayer bgm);
            var brain = (Behaviour)camera.gameObject.AddComponent(
                System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine"));
            Assert.That(manager.PlayBgm("Launch", 0f), Is.True);
            AudioSource flightMusic = bgm.CurrentSource;

            Assert.That(presentation.Begin(camera, Vector3.up * 20f), Is.True);
            Assert.That(brain.enabled, Is.False);
            Assert.That(camera.enabled, Is.True);
            Assert.That(presentation.PresentationCamera, Is.Null);
            Assert.That(bgm.CurrentId, Is.EqualTo("parachuteBGM"));
            AudioSource music = bgm.CurrentSource;
            Assert.That(music.clip.name, Is.EqualTo("parachuteBGM"));
            Assert.That(music.loop, Is.True);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(flightMusic.isPlaying, Is.False);
            Assert.That(music.volume, Is.EqualTo(1f).Within(0.01f));
            float playbackTime = music.time;

            Assert.That(presentation.Begin(camera), Is.True);
            presentation.Evaluate(1f);
            Assert.That(camera.enabled, Is.False);
            Assert.That(bgm.CurrentSource, Is.SameAs(music));
            Assert.That(music.time, Is.GreaterThanOrEqualTo(playbackTime));
            presentation.Evaluate(3f);
            Assert.That(bgm.CurrentId, Is.EqualTo("parachuteBGM"));
            Assert.That(ParachuteVoiceCount(), Is.EqualTo(1));
            presentation.End();
            Assert.That(bgm.CurrentSource, Is.SameAs(music));
        }

        [TestCase(false, "failBGM")]
        [TestCase(true, "Launch")]
        public void ResultHold_StartsFailureMusicBeforeWaiting_OnlyForFailure(bool succeeded, string expectedMusic)
        {
            var manager = CreateSoundManager(out BgmPlayer bgm);
            Assert.That(manager.PlayBgm("Launch", 0f), Is.True);
            var host = scenery.AddComponent<SimulationStageHost>();
            var routine = (IEnumerator)typeof(SimulationStageHost)
                .GetMethod("HoldThenUnload", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(host, new object[] { succeeded });
            try
            {
                Assert.That(routine.MoveNext(), Is.True);
                Assert.That(routine.Current, Is.TypeOf<WaitForSecondsRealtime>());
                Assert.That(bgm.CurrentId, Is.EqualTo(expectedMusic));
                if (!succeeded)
                {
                    Assert.That(bgm.CurrentSource.clip.name, Is.EqualTo("failBGM"));
                    Assert.That(bgm.CurrentSource.loop, Is.True);
                }
            }
            finally
            {
                (routine as System.IDisposable)?.Dispose();
            }
        }

        private SoundManager CreateSoundManager(out BgmPlayer bgm)
        {
            Assert.That(SoundManager.Instance, Is.Null);
            soundRoot = new GameObject("parachute audio test");
            soundRoot.AddComponent<AudioListener>();
            var pool = soundRoot.AddComponent<SfxPool>();
            bgm = soundRoot.AddComponent<BgmPlayer>();
            typeof(BgmPlayer).GetField("sourceA", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bgm, soundRoot.AddComponent<AudioSource>());
            typeof(BgmPlayer).GetField("sourceB", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bgm, soundRoot.AddComponent<AudioSource>());
            var manager = soundRoot.AddComponent<SoundManager>();
            typeof(SoundManager).GetField("sfxPool", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, pool);
            typeof(SoundManager).GetField("bgmPlayer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, bgm);
            typeof(SoundManager).GetField("database", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>(
                    "Assets/02. ScriptableObjects/Audio/SoundDatabase.asset"));
            return manager;
        }

        private int ParachuteVoiceCount() => soundRoot.GetComponentsInChildren<AudioSource>()
            .Count(source => source.clip != null && source.clip.name == "parachute");

        [Test]
        public void DisableMidDescent_RestoresCameraAndDetachesBeforeRigDestruction()
        {
            Assert.That(presentation.Begin(camera), Is.True);
            presentation.Evaluate(2f);
            presentation.enabled = false;
            Assert.That(presentation.IsPlaying, Is.False);
            Assert.That(rocketObject.transform.parent, Is.Null);
            Assert.That(camera.enabled, Is.True);
        }

        [Test]
        public void Descent_RendersAboveCrtCameraFromAnotherScene()
        {
            var overlayScene = UnityEngine.SceneManagement.SceneManager.CreateScene("success CRT overlay test");
            var overlay = new GameObject("CRT output camera").AddComponent<Camera>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlay.gameObject, overlayScene);
            overlay.depth = 20f;
            try
            {
                Assert.That(presentation.Begin(camera), Is.True);
                presentation.Evaluate(1f);
                Assert.That(overlay.enabled, Is.True);
                Assert.That(presentation.PresentationCamera.depth, Is.GreaterThan(overlay.depth));
                Assert.That(presentation.PresentationCamera.targetTexture, Is.Null);
                presentation.End();
                Assert.That(overlay.enabled, Is.True);
            }
            finally
            {
                Object.Destroy(overlay.gameObject);
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(overlayScene);
            }
        }

        [Test]
        public void RocketPrefab_HasRuntimePresentationReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/3D/RocketBody.prefab");
            var component = prefab.GetComponent<MissionSuccessPresentation>();
            Assert.That(component, Is.Not.Null);
            Assert.That(prefab.GetComponent<Rocket>(), Is.Null, "Body prefab must use the scene's parent Rocket.");
            Assert.That(prefab.GetComponent<Rigidbody>(), Is.Null);
            var so = new SerializedObject(component);
            Assert.That(so.FindProperty("parachutePrefab").objectReferenceValue, Is.Not.Null);
            Assert.That(so.FindProperty("swayClip").objectReferenceValue, Is.Not.Null);
        }

        private void AssertAllOutside(bool above)
        {
            Transform rig = rocketObject.transform.root;
            foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;
                float y = presentation.PresentationCamera.WorldToViewportPoint(
                    above ? bounds.min : bounds.max).y;
                if (above) Assert.That(y, Is.GreaterThan(1f));
                else Assert.That(y, Is.LessThan(0f));
            }
        }

        private void SetField(string name, Object value) => typeof(MissionSuccessPresentation)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presentation, value);
    }
}
#endif
