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

        [Test]
        public void BriefInputGaps_KeepTheSameVoice_ReleaseAndIdleStopIt()
        {
            Motion(true, false, 0f);
            Assert.AreEqual(0, GearSources().Length, "Holding a stationary handle must be silent.");
            Motion(true, true, 0f);
            var source = GearSources().Single();
            Motion(true, false, 0.016f);
            Motion(true, false, 0.08f);
            Assert.AreSame(source, GearSources().Single(), "The voice must outlast the clip's initial silence.");
            Motion(true, true, 0.09f);
            Assert.AreSame(source, GearSources().Single());
            Motion(true, false, 0.22f);
            Assert.AreEqual(0, GearSources().Length, "A held but stationary handle must stop after 120 ms.");
            Motion(true, true, 0.23f);
            Motion(false, false, 0.231f);
            Assert.AreEqual(0, GearSources().Length, "Release must stop immediately, without the idle grace period.");
            Motion(true, false, 0.24f);
            Assert.AreEqual(0, GearSources().Length, "Re-grabbing must not replay stale movement.");
        }

        private void Motion(bool dragging, bool moved, float now) => typeof(RocketBuilder)
            .GetMethod("UpdateRotationSoundForMotion", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(builder, new object[] { dragging, moved, now });

        private AudioSource[] GearSources() => audioRoot.GetComponentsInChildren<AudioSource>()
            .Where(source => source.clip != null && source.clip.name == "gear").ToArray();
        private void SetRotating(bool value) => typeof(RocketBuilder)
            .GetMethod("UpdateRotationSound", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(builder, new object[] { value });
    }
}
#endif
