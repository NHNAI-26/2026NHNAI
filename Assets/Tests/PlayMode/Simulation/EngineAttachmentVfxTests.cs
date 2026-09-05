#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class EngineAttachmentVfxTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject host, partHost;
        private RocketBuilder builder;
        private Rocket rocket;
        private RocketPart part;
        private ParticleSystem sparks;
        private float timeScale;

        [SetUp]
        public void SetUp()
        {
            timeScale = Time.timeScale;
            host = new GameObject("attachment vfx test");
            rocket = host.AddComponent<Rocket>();
            rocket.enabled = false;
            builder = host.AddComponent<RocketBuilder>();
            builder.enabled = false;
            Set("rocket", rocket);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Simulation/RocketBuilder.prefab");
            var effect = (ParticleSystem)typeof(RocketBuilder).GetField("attachmentSparksPrefab", Fields)
                .GetValue(prefab.GetComponent<RocketBuilder>());
            Assert.That(effect, Is.Not.Null, "The shared builder prefab must supply the attachment effect.");
            Set("attachmentSparksPrefab", effect);
            partHost = new GameObject("test engine", typeof(BoxCollider));
            part = partHost.AddComponent<RocketPart>();
            part.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = timeScale;
            if (sparks != null) Object.Destroy(sparks.gameObject);
            if (partHost != null) Object.Destroy(partHost);
            Object.Destroy(host);
            yield return null;
        }

        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void Release_EmitsOnlyWhenAttachmentIsConfirmed(bool moved, bool valid, bool expected)
        {
            Release(moved, valid, Vector3.right);
            Assert.That(sparks != null, Is.EqualTo(expected));
            if (!expected) return;
            Assert.That(part.transform.parent, Is.EqualTo(host.transform));
            Assert.That(sparks.isPlaying, Is.True);
            Assert.That(sparks.GetComponentsInChildren<ParticleSystem>(), Has.Length.EqualTo(1),
                "The falling smoke burst belongs to explosions, not engine attachment.");
            Assert.That(Vector3.Dot(sparks.transform.forward, Vector3.right), Is.GreaterThan(0.999f));
        }

        [Test]
        public void Reattachment_OnRotatedCapsuleCap_UsesSurfacePositionAndOutwardDirection()
        {
            host.transform.SetPositionAndRotation(new Vector3(3f, 4f, -1f), Quaternion.Euler(0f, 0f, 35f));
            host.transform.localScale = Vector3.one * 1.5f;
            part.transform.SetParent(host.transform);
            Set("_dragParent", host.transform);
            Release(true, true, host.transform.TransformPoint(Vector3.up * 2.3f));
            Assert.That(sparks, Is.Not.Null);
            Vector3 expected = host.transform.TransformPoint(Vector3.up * 2f) + host.transform.up * 0.06f;
            Assert.That(Vector3.Distance(sparks.transform.position, expected), Is.LessThan(0.001f));
            Assert.That(Vector3.Dot(sparks.transform.forward, host.transform.up), Is.GreaterThan(0.999f));
        }

        [UnityTest]
        public IEnumerator Sparks_StayAtTheJoint_AndCleanUpWhenTimeScaleIsZero()
        {
            Time.timeScale = 0f;
            Release(true, true, Vector3.right);
            Assert.That(sparks, Is.Not.Null);
            Assert.That(sparks.transform.parent, Is.Null);
            Assert.That(sparks.gameObject.scene, Is.EqualTo(host.scene));
            Vector3 position = sparks.transform.position;
            host.transform.position += Vector3.up * 10f;
            yield return null;
            Assert.That(sparks.transform.position, Is.EqualTo(position));
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.That(sparks == null, Is.True, "The one-shot effect must remove itself even with scaled time stopped.");
        }

        [Test]
        public void SavedMaterial_KeepsHdrEmissionEnabledForBloom()
        {
            var effect = (ParticleSystem)typeof(RocketBuilder).GetField("attachmentSparksPrefab", Fields)
                .GetValue(builder);
            var material = effect.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.True,
                "An HDR color alone is insufficient if material validation disables emission.");
            Assert.That(material.globalIlluminationFlags.HasFlag(MaterialGlobalIlluminationFlags.EmissiveIsBlack), Is.False);
            Assert.That(material.GetColor("_EmissionColor").maxColorComponent, Is.GreaterThan(1f));
        }

        private void Release(bool moved, bool valid, Vector3 point)
        {
            Set("_dragged", part);
            Set("_draggedCollider", partHost.GetComponent<Collider>());
            Set("_dragMoved", moved);
            Set("_overRocket", valid);
            Set("_attachPoint", point);
            typeof(RocketBuilder).GetMethod("EndDrag", Fields).Invoke(builder, null);
            foreach (var candidate in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                if (candidate.name == "EngineAttachmentSparks(Clone)") sparks = candidate;
        }

        private void Set(string name, object value) => typeof(RocketBuilder)
            .GetField(name, Fields).SetValue(builder, value);
    }
}
#endif
