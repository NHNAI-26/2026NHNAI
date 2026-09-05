using System.Collections.Generic;
using System.Reflection;
using Border.Research;
using NUnit.Framework;
using UnityEngine;

namespace Simulation.Tests
{
    public sealed class LaunchMissionControllerTests
    {
        private GameObject _object;
        private Rigidbody _body;
        private Rocket _rocket;
        private LaunchMissionController _controller;
        private readonly List<bool> _results = new();
        private int _explosions;

        [SetUp]
        public void SetUp()
        {
            _results.Clear();
            _explosions = 0;
            _object = new GameObject("launch mission controller test");
            _body = _object.AddComponent<Rigidbody>();
            _rocket = _object.AddComponent<Rocket>();
            // EditMode does not invoke MonoBehaviour.Awake automatically.
            Invoke(_rocket, "Awake");
            // 이 클래스는 클램프가 풀린 뒤의 판정만 본다. 홀드 자체는 RocketSimulationTests 가 덮는다.
            SetField(_rocket, "holdSeconds", 0f);
            _controller = _object.AddComponent<LaunchMissionController>();
            _controller.Initialize(LaunchMissionId.LowAltitude, () => true, success => _results.Add(success));
            _controller.ExplosionRequested.AddListener(() => _explosions++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_object);
        }

        [Test]
        public void BeforeLaunch_SelfDestructAndCompletionDoNothing()
        {
            Assert.That(_controller.CanSelfDestruct, Is.False);
            _controller.SelfDestruct();
            _controller.CompleteSelfDestruction();
            Assert.That(_controller.IsExploding, Is.False);
            Assert.That(_rocket.FlightStopped, Is.False);
            Assert.That(_explosions, Is.Zero);
            Assert.That(_results, Is.Empty);
        }

        [Test]
        public void DeniedLaunch_DoesNotEnableSelfDestruct()
        {
            _rocket.AuthorizeLaunch = () => false;
            _rocket.Launch();
            _controller.SelfDestruct();
            Assert.That(_rocket.Launched, Is.False);
            Assert.That(_controller.CanSelfDestruct, Is.False);
            Assert.That(_explosions, Is.Zero);
            Assert.That(_results, Is.Empty);
        }

        [Test]
        public void SelfDestruct_StopsImmediatelyAndDefersFailureUntilAnimationCompletion()
        {
            _rocket.Launch();
            Assert.That(_controller.CanSelfDestruct, Is.True);
            Assert.That(_body.isKinematic, Is.False);
            _body.linearVelocity = Vector3.up * 10f;

            _controller.SelfDestruct();

            Assert.That(_rocket.FlightStopped, Is.True);
            Assert.That(_body.isKinematic, Is.True);
            Assert.That(_controller.IsExploding, Is.True);
            Assert.That(_controller.CanSelfDestruct, Is.False);
            Assert.That(_explosions, Is.EqualTo(1));
            Assert.That(_results, Is.Empty);

            _controller.CompleteSelfDestruction();

            Assert.That(_results, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void RepeatedSelfDestructAndCompletion_NotifyExactlyOnce()
        {
            _rocket.Launch();
            _controller.SelfDestruct();
            _controller.SelfDestruct();
            _controller.CompleteSelfDestruction();
            _controller.CompleteSelfDestruction();
            _controller.SelfDestruct();
            Invoke(_controller, "Update");

            Assert.That(_explosions, Is.EqualTo(1));
            Assert.That(_results, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void WaitingForAnimation_IgnoresPlaceholderTimeout()
        {
            SetField(_controller, "waitForExplosionCompletion", true);
            SetField(_controller, "placeholderExplosionSeconds", 0f);
            _rocket.Launch();
            _controller.SelfDestruct();
            Invoke(_controller, "Update");
            Invoke(_controller, "Update");

            Assert.That(_results, Is.Empty);
            _controller.CompleteSelfDestruction();
            Assert.That(_results, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void PlaceholderWithoutAnimation_CompletesWhenTimerExpires()
        {
            SetField(_controller, "placeholderExplosionSeconds", 0f);
            _rocket.Launch();
            _controller.SelfDestruct();
            Assert.That(_results, Is.Empty);

            Invoke(_controller, "Update");
            Invoke(_controller, "Update");

            Assert.That(_results, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void Exploding_DoesNotReportTelemetrySuccess()
        {
            SetField(_controller, "waitForExplosionCompletion", true);
            _rocket.Launch();
            _controller.SelfDestruct();
            _object.transform.position = Vector3.up * 100f;
            Invoke(_controller, "FixedUpdate");
            Assert.That(_results, Is.Empty);
            _controller.CompleteSelfDestruction();
            Assert.That(_results, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void MaxTelemetry_KeepsPeakAfterDescending()
        {
            _rocket.Launch();
            _body.linearVelocity = Vector3.up * 10f;
            _object.transform.position = new Vector3(30f, 50f, 40f); // 고도 50, 수평 거리 50
            Invoke(_controller, "FixedUpdate");
            Assert.That(_controller.MaxAltitude, Is.EqualTo(50f).Within(0.01f));
            Assert.That(_controller.MaxDistance, Is.EqualTo(50f).Within(0.01f));

            _object.transform.position = new Vector3(3f, 10f, 4f); // 고도 10, 수평 거리 5
            Invoke(_controller, "FixedUpdate");

            Assert.That(_controller.MaxAltitude, Is.EqualTo(50f).Within(0.01f));
            Assert.That(_controller.MaxDistance, Is.EqualTo(50f).Within(0.01f));
            Assert.That(_results, Is.Empty);
        }

        [Test]
        public void ReachingObjective_CompletesSuccessfullyOnceWithoutExplosion()
        {
            _rocket.Launch();
            _object.transform.position = Vector3.up * 100f;
            Invoke(_controller, "FixedUpdate");
            Invoke(_controller, "FixedUpdate");
            _controller.SelfDestruct();
            _controller.CompleteSelfDestruction();

            Assert.That(_results, Is.EqualTo(new[] { true }));
            Assert.That(_rocket.FlightStopped, Is.True);
            Assert.That(_explosions, Is.Zero);
            Assert.That(_controller.CanSelfDestruct, Is.False);
        }

        [Test]
        public void TargetBoxMission_CreatesRuntimeGuide()
        {
            ReinitializeController(LaunchMissionId.TargetZone);

            Assert.That(_controller.UsesTargetBox, Is.True);
            Assert.That(_controller.TargetZoneCenter, Is.EqualTo(new Vector3(0f, 260f, 100f)));
            Assert.That(_controller.TargetZoneRadius, Is.EqualTo(60f));
            LaunchTargetZoneGuide guide = _object.GetComponent<LaunchTargetZoneGuide>();
            Assert.That(guide, Is.Not.Null);
            Assert.That(guide.IsVisible, Is.True);
            Assert.That(guide.TargetBounds.size, Is.EqualTo(Vector3.one * 120f));
            Assert.That(guide.TargetMaterial.shader.name, Is.EqualTo("Shader/Uber/3D Object"));
            Assert.That(guide.TargetMaterial.GetFloat("_HologramEnabled"), Is.EqualTo(1f));
            Assert.That(guide.TargetMaterial.GetFloat("_Surface"), Is.EqualTo(1f));
            Assert.That(guide.TargetMaterial.renderQueue, Is.EqualTo((int)UnityEngine.Rendering.RenderQueue.Transparent));
            Assert.That(guide.TargetMaterial.GetColor("_BaseColor").r, Is.GreaterThan(0.9f));
            Assert.That(guide.TargetMaterial.GetColor("_BaseColor").g, Is.GreaterThan(0.7f));
            Assert.That(guide.TargetMaterial.GetColor("_BaseColor").a, Is.LessThan(0.5f));
        }

        [Test]
        public void AltitudeMission_DoesNotCreateRuntimeGuide()
        {
            Assert.That(_controller.UsesTargetBox, Is.False);
            Assert.That(_object.GetComponent<LaunchTargetZoneGuide>(), Is.Null);
        }

        [Test]
        public void TargetBoxMission_SucceedsWhenRocketBoundsTouchZone()
        {
            ReinitializeController(LaunchMissionId.TargetZone);
            var collider = _object.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 4f, 4f);

            _rocket.Launch();
            _object.transform.position = new Vector3(0f, 198f, 100f);
            Physics.SyncTransforms();
            Invoke(_controller, "FixedUpdate");

            Assert.That(_controller.IsInTargetBox, Is.True);
            Assert.That(_results, Is.EqualTo(new[] { true }));
            Assert.That(_object.GetComponent<LaunchTargetZoneGuide>().IsVisible, Is.False);
        }

        [Test]
        public void TargetBoxMission_StaysRunningOutsideZone()
        {
            ReinitializeController(LaunchMissionId.TargetZone);
            _object.AddComponent<BoxCollider>().size = Vector3.one;

            _rocket.Launch();
            _object.transform.position = new Vector3(0f, 190f, 100f);
            Physics.SyncTransforms();
            Invoke(_controller, "FixedUpdate");

            Assert.That(_controller.IsInTargetBox, Is.False);
            Assert.That(_results, Is.Empty);
        }

        [Test]
        public void LowSpeedAfterMovement_KeepsFlyingAndCanStillSucceed()
        {
            _rocket.Launch();
            _body.linearVelocity = Vector3.up * 2f;
            Invoke(_controller, "FixedUpdate");
            Assert.That(_results, Is.Empty);

            _body.linearVelocity = Vector3.up;
            Invoke(_controller, "FixedUpdate");
            Invoke(_controller, "FixedUpdate");

            Assert.That(_results, Is.Empty);
            Assert.That(_rocket.FlightStopped, Is.False);
            Assert.That(_explosions, Is.Zero);

            _object.transform.position = Vector3.up * 100f;
            Invoke(_controller, "FixedUpdate");
            Assert.That(_results, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void NoLiftoff_WithoutGroundContactDoesNotFailAfterTenSeconds()
        {
            _rocket.Launch();
            for (int i = 0; i < Mathf.FloorToInt(9f / Time.fixedDeltaTime); i++) Invoke(_controller, "FixedUpdate");

            Assert.That(_results, Is.Empty);
            Assert.That(_rocket.FlightStopped, Is.False);
            Assert.That(_controller.CanSelfDestruct, Is.True);
            for (int i = 0; i < Mathf.CeilToInt(2f / Time.fixedDeltaTime); i++) Invoke(_controller, "FixedUpdate");
            Assert.That(_results, Is.Empty);
            Assert.That(_rocket.FlightStopped, Is.False);
        }

        [Test]
        public void WaterEntry_StillAppliesDampingWithMissionControllerAttached()
        {
            _rocket.Launch();
            _object.transform.position = Vector3.down * 10f;
            Invoke(_rocket, "FixedUpdate");

            Assert.That(_rocket.Splashed, Is.True);
            Assert.That(_body.linearDamping, Is.EqualTo(4f));
            Assert.That(_body.angularDamping, Is.EqualTo(4f));
            Assert.That(_body.isKinematic, Is.False);
            Assert.That(_results, Is.Empty);
        }

        [Test]
        public void DeepWater_StillStopsSinkingWithMissionControllerAttached()
        {
            _rocket.Launch();
            _object.transform.position = Vector3.down * 40f;
            Invoke(_rocket, "FixedUpdate");

            Assert.That(_rocket.Splashed, Is.True);
            Assert.That(_body.isKinematic, Is.True);
            Assert.That(_results, Is.Empty);
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void Invoke(object target, string name) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        private void ReinitializeController(LaunchMissionId mission)
        {
            Object.DestroyImmediate(_controller);
            _controller = _object.AddComponent<LaunchMissionController>();
            _controller.Initialize(mission, () => true, success => _results.Add(success));
            _controller.ExplosionRequested.AddListener(() => _explosions++);
        }
    }
}
