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
    public sealed class RocketSplashAudioTests
    {
        private GameObject soundRoot;
        private GameObject host;
        private Rocket rocket;
        private RocketAudio audio;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsNull(SoundManager.Instance);
            soundRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            soundRoot.AddComponent<AudioListener>();
            host = new GameObject("splash test");
            rocket = host.AddComponent<Rocket>();
            rocket.enabled = false;
            typeof(Rocket).GetField("waterLevel", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rocket, 0f);
            audio = host.AddComponent<RocketAudio>();
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(host);
            Object.Destroy(soundRoot);
            yield return null;
        }

        [Test]
        public void WaterEntry_PlaysOnceWithoutSpatialization_ContinuesWhenRocketMoves()
        {
            host.transform.position = new Vector3(12f, 1f, 34f);
            TickWater();
            Assert.AreEqual(0, Sources().Length);
            host.transform.position = new Vector3(12f, -1f, 34f);
            TickWater();
            AudioSource source = Sources().Single();
            Assert.IsFalse(source.spatialize);
            Assert.AreEqual(0f, source.spatialBlend);
            Assert.IsFalse(source.loop);
            host.transform.position = new Vector3(30f, -5f, 60f);
            TickWater();
            Assert.AreEqual(1, Sources().Length);
            audio.enabled = false;
            Assert.IsNotNull(source.clip, "The impact tail must survive rocket audio cleanup.");
        }

        [Test]
        public void Reset_AllowsNextSplash_AndReenableDoesNotDuplicateSubscription()
        {
            host.transform.position = Vector3.down;
            TickWater();
            Assert.AreEqual(1, Sources().Length);
            rocket.ResetFlight(Vector3.up, Quaternion.identity);
            audio.enabled = false;
            audio.enabled = true;
            host.transform.position = new Vector3(5f, -1f, 10f);
            TickWater();
            Assert.AreEqual(2, Sources().Length);
        }

        private AudioSource[] Sources() => soundRoot.GetComponentsInChildren<AudioSource>()
            .Where(source => source.clip != null && source.clip.name == "Heavy_water_impact_2").ToArray();
        private void TickWater() => typeof(Rocket)
            .GetMethod("TickWater", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rocket, null);
    }
}
#endif
