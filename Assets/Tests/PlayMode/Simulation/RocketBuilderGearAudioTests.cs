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
    public sealed class RocketBuilderGearAudioTests
    {
        private GameObject audioRoot;
        private GameObject host;
        private RocketBuilder builder;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsNull(SoundManager.Instance);
            audioRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            audioRoot.AddComponent<AudioListener>();
            host = new GameObject("gear sound test");
            builder = host.AddComponent<RocketBuilder>();
            builder.enabled = false; // Test audio lifetime without bootstrapping design cameras.
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(host);
            Object.Destroy(audioRoot);
            yield return null;
        }

        [Test]
        public void Rotation_ReusesOneLoop_StopsImmediately_AndCanRestart()
        {
            var other = SoundManager.Instance.PlaySfx("click");
            SetRotating(true);
            var source = GearSources().Single();
            Assert.IsTrue(source.loop);
            Assert.AreEqual(0f, source.spatialBlend);
            for (int i = 0; i < 20; i++) SetRotating(true);
            Assert.AreSame(source, GearSources().Single());
            SetRotating(false);
            Assert.AreEqual(0, GearSources().Length);
            Assert.IsTrue(other.IsValid);
            SetRotating(true);
            Assert.AreEqual(1, GearSources().Length);
        }

        [Test]
        public void DisableOrFocusLoss_StopsTheOwnedLoop()
        {
            builder.enabled = true;
            SetRotating(true);
            builder.enabled = false;
            Assert.AreEqual(0, GearSources().Length);
            SetRotating(true);
            typeof(RocketBuilder).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(builder, new object[] { false });
            Assert.AreEqual(0, GearSources().Length);
        }

        private AudioSource[] GearSources() => audioRoot.GetComponentsInChildren<AudioSource>()
            .Where(source => source.clip != null && source.clip.name == "gear").ToArray();
        private void SetRotating(bool value) => typeof(RocketBuilder)
            .GetMethod("UpdateRotationSound", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(builder, new object[] { value });
    }
}
#endif
