using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Simulation.Tests
{
    public sealed class RocketSplashVfxTests
    {
        private Scene scene;
        private Rocket rocket;
        private static readonly MethodInfo TickWater = typeof(Rocket).GetMethod("TickWater", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            scene = SceneManager.CreateScene("RocketSplashVfxTests", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var root = new GameObject("Rocket");
            SceneManager.MoveGameObjectToScene(root, scene);
            rocket = root.AddComponent<Rocket>();
            var body = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/3D/RocketBody.prefab"), root.transform);
            Assert.That(body.GetComponent<RocketSplashVfx>(), Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private ParticleSystem[] Splashes() => scene.GetRootGameObjects()
            .Where(go => go.name == "RocketSplash(Clone)").Select(go => go.GetComponent<ParticleSystem>()).ToArray();

        private void EnterWater()
        {
            rocket.transform.position = new Vector3(4, -7, 3);
            TickWater.Invoke(rocket, null);
        }

        [UnityTest]
        public IEnumerator WaterEntry_SpawnsOnceAtSurfaceAndOutlivesRocketUntilParticlesEnd()
        {
            EnterWater();
            TickWater.Invoke(rocket, null);
            Assert.That(Splashes(), Has.Length.EqualTo(1));
            var splash = Splashes()[0];
            Assert.That(splash.transform.position, Is.EqualTo(new Vector3(4, -6.56f, 3)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(splash.transform.parent, Is.Null);
            Assert.That(splash.GetComponentsInChildren<ParticleSystem>(), Has.Length.EqualTo(5));
            rocket.transform.position = Vector3.down * 20;
            Object.Destroy(rocket.gameObject);
            yield return null;
            Assert.That(splash != null, Is.True);
            Assert.That(splash.transform.position.y, Is.EqualTo(-6.56f).Within(.001f));
            yield return new WaitForSeconds(3f);
            Assert.That(splash == null, Is.True, "The effect must clean itself up.");
        }

        [Test]
        public void ResetAndReenable_AllowNextSplashWithoutDuplicateSubscriptions()
        {
            var vfx = rocket.GetComponentInChildren<RocketSplashVfx>();
            vfx.enabled = false;
            vfx.enabled = true;
            EnterWater();
            Assert.That(Splashes(), Has.Length.EqualTo(1));
            rocket.ResetFlight(Vector3.zero, Quaternion.identity);
            EnterWater();
            Assert.That(Splashes(), Has.Length.EqualTo(2));
        }
    }
}
