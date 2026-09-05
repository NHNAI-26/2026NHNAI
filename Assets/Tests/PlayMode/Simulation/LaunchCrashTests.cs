using System.Collections;
using System.Reflection;
using Border.Research;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Simulation.Tests
{
    public sealed class LaunchCrashTests
    {
        private static readonly MethodInfo RocketTick = typeof(Rocket).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MissionTick = typeof(LaunchMissionController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        private Scene scene;
        private PhysicsScene physics;
        private Rocket rocket;
        private Rigidbody body;
        private LaunchMissionController mission;
        private int completions;
        private bool succeeded;

        [SetUp]
        public void SetUp()
        {
            scene = SceneManager.CreateScene("LaunchCrashTests", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            physics = scene.GetPhysicsScene();
            var root = CreateObject("Rocket", new Vector3(0f, 0.6f, 0f));
            body = root.AddComponent<Rigidbody>();
            var shape = CreateObject("Body", root.transform.position);
            shape.transform.SetParent(root.transform, true);
            shape.AddComponent<BoxCollider>();
            rocket = root.AddComponent<Rocket>();
            mission = root.AddComponent<LaunchMissionController>();
            mission.Initialize(LaunchMissionId.HighAltitude, () => true, success =>
            {
                completions++;
                succeeded = success;
            });
            completions = 0;
            succeeded = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [Test]
        public void ApexThenGroundedSleep_WaitsThreeSecondsAndCompletesOnce()
        {
            CreateGround(Vector3.zero, new Vector3(20f, 1f, 20f));
            rocket.Launch();
            body.linearVelocity = Vector3.up * 12f;
            bool rose = false;
            for (int i = 0; i < 300; i++)
            {
                Tick();
                rose |= mission.Altitude >= 3f;
                Assert.That(completions, Is.Zero, "Apex and descent must remain visible.");
                if (rose && rocket.IsGrounded && body.linearVelocity.magnitude <= 1f) break;
            }
            Assert.That(rose, Is.True);
            Assert.That(rocket.IsGrounded, Is.True);
            body.Sleep();
            TickFor(2.5f);
            Assert.That(rocket.IsGrounded, Is.True, "Sleeping bodies do not send CollisionStay.");
            Assert.That(completions, Is.Zero);
            TickFor(0.6f);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(succeeded, Is.False);
            Assert.That(mission.Status, Does.Contain("지면"));
            TickFor(1f);
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void NoLiftoff_OnGroundWaitsTenSecondsBeforeCompleting()
        {
            CreateGround(Vector3.zero, new Vector3(20f, 1f, 20f));
            rocket.Launch();
            TickFor(9f);
            Assert.That(rocket.IsGrounded, Is.True);
            Assert.That(completions, Is.Zero);
            TickFor(1.1f);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(mission.Status, Does.Contain("이륙"));
        }

        [Test]
        public void Splashdown_ContinuesSinkingForThreeSecondsBeforeStopping()
        {
            rocket.Launch();
            body.position = new Vector3(0f, -9f, 0f);
            rocket.transform.position = body.position;
            Tick();
            Assert.That(rocket.Splashed, Is.True);
            float entryHeight = body.position.y;
            TickFor(2.5f);
            Assert.That(body.position.y, Is.LessThan(entryHeight));
            Assert.That(body.isKinematic, Is.False);
            Assert.That(rocket.FlightStopped, Is.False);
            Assert.That(completions, Is.Zero);
            TickFor(0.6f);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(succeeded, Is.False);
            Assert.That(mission.Status, Does.Contain("바다"));
            Assert.That(rocket.FlightStopped, Is.True);
        }

        [Test]
        public void SeparateSupports_BounceAndResetClearOnlyAppropriateContacts()
        {
            var left = CreateGround(new Vector3(-0.5f, 0f, 0f), Vector3.one);
            CreateGround(new Vector3(0.5f, 0f, 0f), Vector3.one);
            rocket.Launch();
            TickFor(0.5f);
            Assert.That(rocket.IsGrounded, Is.True);
            Object.DestroyImmediate(left);
            TickFor(0.1f);
            Assert.That(rocket.IsGrounded, Is.True, "Remaining support must survive another collider's exit.");
            body.linearVelocity = Vector3.up * 8f;
            TickFor(0.3f);
            Assert.That(rocket.IsGrounded, Is.False);
            rocket.ResetFlight(new Vector3(0f, 5f, 0f), Quaternion.identity);
            Assert.That(rocket.IsGrounded, Is.False);
            Assert.That(rocket.Splashed, Is.False);
        }

        [Test]
        public void WallContact_IsNotGroundAndDoesNotFailAtTimeout()
        {
            var wall = CreateObject("Wall", new Vector3(0.9f, 0.6f, 0f));
            wall.AddComponent<BoxCollider>().size = new Vector3(1f, 20f, 20f);
            rocket.Launch();
            body.useGravity = false;
            body.linearVelocity = Vector3.right;
            TickFor(11f);
            Assert.That(rocket.IsGrounded, Is.False);
            Assert.That(completions, Is.Zero);
        }

        private GameObject CreateGround(Vector3 position, Vector3 size)
        {
            var ground = CreateObject("Ground", position - Vector3.up * size.y * 0.5f);
            ground.AddComponent<BoxCollider>().size = size;
            return ground;
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.transform.position = position;
            return obj;
        }

        private void TickFor(float seconds)
        {
            int steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (int i = 0; i < steps; i++) Tick();
        }

        private void Tick()
        {
            // Local physics isolates these synchronous tests from gameplay and frame timing.
            RocketTick.Invoke(rocket, null);
            MissionTick.Invoke(mission, null);
            physics.Simulate(Time.fixedDeltaTime);
        }
    }
}
