#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class RocketExplosionVfxTests
    {
        [Test]
        public void ExplosionSmoke_HasSmallerFasterParticles_ThatFallAndExpire()
        {
            var effect = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ParticleSystem>(
                "Assets/03. Prefabs/Simulation/Explosion/RocketExplosion.prefab"));
            try
            {
                var smoke = effect.transform.Find("SmokeBurst/Smoke").GetComponent<ParticleSystem>();
                var fast = effect.transform.Find("SmokeBurst/SmokeFast").GetComponent<ParticleSystem>();
                smoke.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                fast.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                smoke.useAutoRandomSeed = fast.useAutoRandomSeed = false;
                smoke.randomSeed = fast.randomSeed = 42;
                var ordinary = Sample(smoke, 0.1f);
                var small = Sample(fast, 0.1f);
                Assert.That(ordinary.count, Is.EqualTo(18));
                Assert.That(small.count, Is.EqualTo(7));
                Assert.That(small.size, Is.LessThan(ordinary.size * 0.6f));
                Assert.That(small.distance, Is.GreaterThan(ordinary.distance * 1.8f));
                Assert.That(Sample(smoke, 0.6f).velocityY, Is.LessThan(0f));
                Assert.That(Sample(fast, 0.6f).velocityY, Is.LessThan(0f));
                Assert.That(Sample(smoke, 1f).count + Sample(fast, 1f).count, Is.Zero);
            }
            finally { Object.Destroy(effect.gameObject); }
        }

        private static (int count, float size, float distance, float velocityY) Sample(ParticleSystem system, float time)
        {
            system.Simulate(time, false, true, true);
            var particles = new ParticleSystem.Particle[system.main.maxParticles];
            int count = system.GetParticles(particles);
            float size = 0f, distance = 0f, velocityY = 0f;
            for (int i = 0; i < count; i++)
            {
                size += particles[i].GetCurrentSize(system);
                distance += Vector3.Distance(particles[i].position, system.transform.position);
                velocityY += particles[i].velocity.y;
            }
            return count == 0 ? (0, 0f, 0f, 0f) : (count, size / count, distance / count, velocityY / count);
        }

        [Test]
        public void Imprint_IsVisibleImmediately_AndGoneBeforeTheSmokeTail()
        {
            var effect = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ParticleSystem>(
                "Assets/03. Prefabs/Simulation/Explosion/RocketExplosion.prefab"));
            try
            {
                var imprint = effect.transform.Find("Imprint").GetComponent<ParticleSystem>();
                var particles = new ParticleSystem.Particle[1];
                effect.Simulate(0.01f, true, true, false);
                Assert.That(imprint.GetParticles(particles), Is.EqualTo(1));
                Assert.That(particles[0].GetCurrentColor(imprint).a, Is.GreaterThan(160));
                Assert.That(particles[0].GetCurrentSize(imprint), Is.GreaterThan(5f));
                effect.Simulate(0.3f, true, true, false);
                Assert.That(imprint.GetParticles(particles), Is.EqualTo(1));
                Assert.That(particles[0].GetCurrentColor(imprint).a, Is.LessThan(65));
                effect.Simulate(0.45f, true, true, false);
                Assert.That(imprint.particleCount, Is.Zero);
                Assert.That(effect.transform.Find("Smoke").GetComponent<ParticleSystem>().particleCount,
                    Is.GreaterThan(0), "Only the imprint should finish early.");
            }
            finally { Object.Destroy(effect.gameObject); }
        }

        [UnityTest]
        public IEnumerator ImportedExplosion_PlaysOnce_KeepsSmokeTail_CleansUpAndResets()
        {
            var host = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            host.name = "explosion visual test rocket";
            host.transform.position = new Vector3(3f, 6f, 2f);
            var rocket = host.AddComponent<Rocket>();
            rocket.enabled = false;
            var renderer = host.GetComponent<Renderer>();
            var prefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(
                "Assets/03. Prefabs/Simulation/Explosion/RocketExplosion.prefab");
            Assert.That(prefab, Is.Not.Null);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(Rocket).GetField("explosionPrefab", flags).SetValue(rocket, prefab);
            var launched = typeof(Rocket).GetField("<Launched>k__BackingField", flags);
            var effectField = typeof(Rocket).GetField("activeExplosion", flags);
            ParticleSystem effect = null;
            try
            {
                rocket.Explode();
                Assert.That(effectField.GetValue(rocket), Is.Null, "Before launch there must be no explosion.");
                launched.SetValue(rocket, true);
                rocket.Explode();
                effect = (ParticleSystem)effectField.GetValue(rocket);
                Assert.That(effect, Is.Not.Null);
                Assert.That(renderer.enabled, Is.False);
                Assert.That(rocket.FlightStopped, Is.True);
                Assert.That(effect.transform.parent, Is.Null);
                Assert.That(effect.gameObject.scene, Is.EqualTo(host.scene));
                var smoke = effect.transform.Find("SmokeBurst/Smoke").GetComponent<ParticleSystem>();
                var fast = effect.transform.Find("SmokeBurst/SmokeFast").GetComponent<ParticleSystem>();
                Assert.That(smoke.isPlaying && fast.isPlaying, Is.True,
                    "Rocket.Explode must start both falling smoke bursts.");
                var authoredSmoke = effect.transform.Find("Smoke_Explosion").GetComponentsInChildren<ParticleSystem>();
                Assert.That(authoredSmoke, Has.Length.EqualTo(6));
                foreach (var particles in authoredSmoke)
                    Assert.That(particles.isPlaying, Is.True, "Smoke_Explosion must start with the explosion.");
                yield return new WaitForSecondsRealtime(0.05f);
                Assert.That(smoke.particleCount, Is.GreaterThan(0));
                Assert.That(fast.particleCount, Is.GreaterThan(0));
                foreach (var particles in authoredSmoke)
                    Assert.That(particles.particleCount, Is.GreaterThan(0));
                Vector3 position = effect.transform.position;
                rocket.Explode();
                Assert.That(effectField.GetValue(rocket), Is.SameAs(effect), "Repeated requests must not spawn another effect.");
                host.transform.position += Vector3.right * 20f;
                Assert.That(effect.transform.position, Is.EqualTo(position), "The blast must stay at the impact position.");

                yield return new WaitForSecondsRealtime(0.75f);
                Assert.That(effect, Is.Not.Null);
                Assert.That(effect.particleCount, Is.Zero, "The short fire phase must have ended.");
                int tailParticles = 0;
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
                    tailParticles += particles.particleCount;
                Assert.That(tailParticles, Is.GreaterThan(0), "Smoke must remain after the fire.");

                yield return new WaitForSecondsRealtime(2.35f);
                Assert.That(effect, Is.Not.Null, "The authored smoke tail must not be cut off at three seconds.");
                int authoredTail = 0;
                foreach (var particles in authoredSmoke) authoredTail += particles.particleCount;
                Assert.That(authoredTail, Is.GreaterThan(0));
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(effect == null, Is.True, "Rocket must remove the finished explosion after four seconds.");
                rocket.ResetFlight(Vector3.zero, Quaternion.identity);
                Assert.That(renderer.enabled, Is.True);
                launched.SetValue(rocket, true);
                rocket.Explode();
                effect = (ParticleSystem)effectField.GetValue(rocket);
                Assert.That(effect, Is.Not.Null, "A new launch must be able to explode again.");
                rocket.ResetFlight(Vector3.zero, Quaternion.identity);
                yield return null;
                Assert.That(effect == null, Is.True, "Reset must clear an unfinished effect.");
                Assert.That(renderer.enabled, Is.True);
            }
            finally
            {
                if (effect != null) Object.Destroy(effect.gameObject);
                Object.Destroy(host);
            }
        }
    }
}
#endif
