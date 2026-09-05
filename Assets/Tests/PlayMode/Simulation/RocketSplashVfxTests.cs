#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class RocketSplashVfxTests
    {
        [UnityTest]
        public IEnumerator WaterEntry_SpawnsWorldSpaceParticlesOnce_ThenCleansUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/3D/RocketBody.prefab");
            var source = prefab.GetComponent<RocketSplashVfx>();
            Assert.IsNotNull(source);
            var field = typeof(RocketSplashVfx).GetField("splashPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            var splashPrefab = (ParticleSystem)field.GetValue(source);
            Assert.IsNotNull(splashPrefab);
            var host = new GameObject("splash vfx test rocket");
            var rocket = host.AddComponent<Rocket>();
            rocket.enabled = false;
            var component = host.AddComponent<RocketSplashVfx>();
            field.SetValue(component, splashPrefab);
            typeof(Rocket).GetField("waterLevel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(rocket, 0f);
            var tick = typeof(Rocket).GetMethod("TickWater", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                host.transform.position = new Vector3(30f, -1f, 50f);
                tick.Invoke(rocket, null);
                var effect = Effects().Single();
                Assert.Less(Vector3.Distance(new Vector3(30f, 0.15f, 50f), effect.transform.position), 0.001f);
                Assert.IsNull(effect.transform.parent);
                Assert.AreEqual(host.scene, effect.gameObject.scene);
                Assert.AreEqual(5, effect.GetComponentsInChildren<ParticleSystem>().Length);
                host.transform.position = Vector3.down * 10f;
                tick.Invoke(rocket, null);
                Assert.AreEqual(1, Effects().Length);
                yield return new WaitForSeconds(0.12f);
                Assert.Greater(effect.GetComponentsInChildren<ParticleSystem>().Sum(p => p.particleCount), 0);
                var droplets = effect.transform.Find("Water Droplets").GetComponent<ParticleSystem>();
                var particles = new ParticleSystem.Particle[droplets.main.maxParticles];
                int count = droplets.GetParticles(particles);
                Assert.Greater(count, 0, "The splash burst must be visible immediately after impact.");
                Assert.IsTrue(particles.Take(count).Any(p => p.position.y > 0.65f),
                    "Water droplets must rise above the surface, not lie flat with the waves.");
                Assert.Less(Vector3.Distance(new Vector3(30f, 0.15f, 50f), effect.transform.position), 0.001f);
                rocket.ResetFlight(Vector3.up, Quaternion.identity);
                component.enabled = false;
                component.enabled = true;
                host.transform.position = new Vector3(40f, -1f, 50f);
                tick.Invoke(rocket, null);
                Assert.AreEqual(2, Effects().Length, "Re-enabling must not double-subscribe.");
                yield return new WaitForSeconds(1.6f);
                Assert.AreEqual(0, Effects().Length, "One-shot effects must clean themselves up.");
            }
            finally
            {
                Object.Destroy(host);
                foreach (var effect in Effects()) Object.Destroy(effect.gameObject);
            }
        }

        private static ParticleSystem[] Effects() => Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
            .Where(p => p.gameObject.name == "RocketSplash(Clone)").ToArray();
    }
}
#endif
