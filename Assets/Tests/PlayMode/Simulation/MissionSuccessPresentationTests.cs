#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
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
            yield return null;
        }

        [Test]
        public void SixSecondDescent_EntersFromAbove_LeavesBelow_AndPreservesRocket()
        {
            Vector3 position = rocketObject.transform.position;
            Vector3 scale = rocketObject.transform.lossyScale;
            Assert.That(presentation.Begin(camera), Is.True);
            Assert.That(camera.enabled, Is.False);
            Assert.That(scenery.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(MissionSuccessPresentation.Duration, Is.EqualTo(6f));
            Assert.That(rocketObject.GetComponent<Rocket>().FlightStopped, Is.True);
            Assert.That(rocketObject.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(rocketObject.transform.parent.name, Is.EqualTo("RocketSocket"));
            Assert.That(engine.parent, Is.EqualTo(rocketObject.transform));
            Assert.That(Vector3.Distance(rocketObject.transform.lossyScale, scale), Is.LessThan(0.001f));
            AssertAllOutside(true);

            presentation.Evaluate(3f);
            float middle = presentation.PresentationCamera.WorldToViewportPoint(rocketObject.transform.position).y;
            Assert.That(middle, Is.InRange(0f, 1f));
            presentation.Evaluate(6f);
            AssertAllOutside(false);
            presentation.End();
            Assert.That(camera.enabled, Is.True);
            Assert.That(scenery.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(rocketObject.transform.parent, Is.Null);
            Assert.That(rocketObject.transform.position, Is.EqualTo(position));
            Assert.That(Vector3.Distance(rocketObject.transform.lossyScale, scale), Is.LessThan(0.001f));
            Assert.That(engine.parent, Is.EqualTo(rocketObject.transform));
        }

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
