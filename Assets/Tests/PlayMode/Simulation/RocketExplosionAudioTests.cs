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
    public sealed class RocketExplosionAudioTests
    {
        [UnityTest]
        public IEnumerator Explosion_PlaysFourVoicesOnce_ResetAllowsReplay_AndCleanupKeepsTail()
        {
            Assert.IsNull(SoundManager.Instance);
            var manager = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            manager.AddComponent<AudioListener>();
            var host = new GameObject("explosion audio test");
            var rocket = host.AddComponent<Rocket>();
            rocket.enabled = false;
            var audio = host.AddComponent<RocketAudio>();
            var launched = typeof(Rocket).GetField("<Launched>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                rocket.Explode();
                Assert.AreEqual(0, Count(manager));
                launched.SetValue(rocket, true);
                rocket.Explode();
                rocket.Explode();
                Assert.AreEqual(4, Count(manager));
                var source = manager.GetComponentsInChildren<AudioSource>()
                    .First(s => s.clip != null && s.clip.name == "SFX15_Terrorist");
                Assert.IsFalse(source.loop);
                Assert.IsFalse(source.spatialize);
                Assert.AreEqual(0f, source.spatialBlend);
                rocket.ResetFlight(Vector3.zero, Quaternion.identity);
                audio.enabled = false;
                audio.enabled = true;
                launched.SetValue(rocket, true);
                rocket.Explode();
                Assert.AreEqual(8, Count(manager));
                audio.enabled = false;
                Assert.AreEqual(8, Count(manager), "Explosion tails should finish after rocket cleanup.");
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(manager);
            }
            yield return null;
        }

        private static int Count(GameObject manager) => manager.GetComponentsInChildren<AudioSource>()
            .Count(s => s.clip != null && s.clip.name == "SFX15_Terrorist");
    }
}
#endif
