#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace Simulation.Tests
{
    public sealed class ParachutePresentationTests
    {
        private const string AssetFolder = "Assets/03. Prefabs/Simulation/Success/";
        private GameObject rig;
        private GameObject rocketObject;
        private Transform descent;
        private Transform pivot;
        private Transform rocketSwing;
        private Transform socket;
        private PlayableDirector director;
        private TimelineAsset timeline;
        private AnimationTrack track;
        private AnimationPlayableAsset animation;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetFolder + "MissionSuccessParachute.prefab");
            Assert.That(prefab, Is.Not.Null);
            rig = Object.Instantiate(prefab);
            rig.transform.position = new Vector3(200f, 500f, 300f);
            descent = rig.transform.Find("DescentRoot");
            pivot = descent.Find("SwayPivot");
            rocketSwing = pivot.Find("RocketSwingPivot");
            socket = rocketSwing.Find("RocketSocket");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (director != null) director.Stop();
            if (rocketObject != null) Object.Destroy(rocketObject);
            if (rig != null) Object.Destroy(rig);
            if (animation != null) Object.Destroy(animation);
            if (track != null) Object.Destroy(track);
            if (timeline != null) Object.Destroy(timeline);
            yield return null;
        }

        [Test]
        public void Prefab_HasEmptySocketBelowCanopy_AndValidMaterials()
        {
            Assert.That(socket.childCount, Is.Zero);
            Assert.That(rig.GetComponentsInChildren<Rocket>(true), Is.Empty);
            Assert.That(rig.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(socket.lossyScale, Is.EqualTo(Vector3.one));
            var renderers = rig.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.bounds.min.y, Is.GreaterThan(socket.position.y));
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(material.shader, Is.Not.Null);
                    Assert.That(material.shader.isSupported, Is.True);
                }
            }
            var animator = descent.GetComponent<Animator>();
            Assert.That(animator.runtimeAnimatorController, Is.Null);
            Assert.That(animator.cullingMode, Is.EqualTo(AnimatorCullingMode.AlwaysAnimate));
        }

        [UnityTest]
        public IEnumerator Timeline_RepeatsWhileOffscreen_WithoutMovingMissionOrigin()
        {
            CreateTimeline();
            Vector3 origin = rig.transform.position;
            Vector3 descentPosition = descent.localPosition;
            float minimum = 0f;
            float maximum = 0f;
            bool repeated = false;
            bool independentSwing = false;
            double deadline = Time.realtimeSinceStartupAsDouble + 15;
            director.Play();
            while (director.time < 5.2 && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
                float angle = Mathf.DeltaAngle(0f, pivot.localEulerAngles.z);
                minimum = Mathf.Min(minimum, angle);
                maximum = Mathf.Max(maximum, angle);
                if (director.time > 4.75 && angle > 8.5f) repeated = true;
                if (Quaternion.Angle(pivot.rotation, socket.rotation) > 12f) independentSwing = true;
                Assert.That(Mathf.Abs(angle), Is.LessThanOrEqualTo(9.01f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, rocketSwing.localEulerAngles.z)), Is.LessThanOrEqualTo(16.01f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, rocketSwing.localEulerAngles.x)), Is.LessThanOrEqualTo(3.01f));
                Assert.That(Vector3.Distance(socket.position, rocketSwing.position), Is.LessThan(0.001f));
                Assert.That(rig.transform.position, Is.EqualTo(origin));
                Assert.That(descent.localPosition, Is.EqualTo(descentPosition));
            }
            Assert.That(director.time, Is.GreaterThanOrEqualTo(5.2), "Timeline did not advance.");
            Assert.That(maximum, Is.GreaterThan(8.5f));
            Assert.That(minimum, Is.LessThan(-8.5f));
            Assert.That(repeated, Is.True, "The clip must keep swaying after its first four seconds.");
            Assert.That(independentSwing, Is.True, "The rocket must swing relative to the canopy.");
        }

        [UnityTest]
        public IEnumerator StoppedRocket_FollowsSocket_AndSurvivesRigRemoval()
        {
            CreateTimeline();
            rocketObject = new GameObject("existing rocket attachment test");
            var rocket = rocketObject.AddComponent<Rocket>();
            var body = rocketObject.GetComponent<Rigidbody>();
            var engine = new GameObject("existing engine").transform;
            engine.SetParent(rocketObject.transform, false);
            engine.localPosition = new Vector3(1f, -2f, 0f);
            int rocketId = rocket.GetInstanceID();
            rocketObject.transform.localScale = Vector3.one * 1.5f;
            Vector3 scale = rocketObject.transform.lossyScale;
            rocket.Launch();
            body.isKinematic = false;
            body.linearVelocity = Vector3.up * 20f;
            rocket.StopFlight();
            rocketObject.transform.SetParent(socket, true);
            rocketObject.transform.localPosition = Vector3.down * 3f;
            rocketObject.transform.localRotation = Quaternion.identity;
            Vector3 mountedPosition = rocketObject.transform.localPosition;
            director.time = 1;
            director.Evaluate();
            Vector3 expected = socket.TransformPoint(mountedPosition);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.That(rocket.FlightStopped, Is.True);
            Assert.That(body.isKinematic, Is.True);
            Assert.That(Vector3.Distance(rocketObject.transform.position, expected), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(rocketObject.transform.rotation, socket.rotation), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(rocketObject.transform.lossyScale, scale), Is.LessThan(0.001f));
            Assert.That(engine.parent, Is.EqualTo(rocketObject.transform));
            Assert.That(rocket.GetInstanceID(), Is.EqualTo(rocketId));

            rocketObject.transform.SetParent(null, true);
            director.Stop();
            Object.Destroy(rig);
            yield return null;
            Assert.That(rocket != null, Is.True, "Detach the current rocket before destroying the presentation rig.");
            Assert.That(engine != null, Is.True);
        }

        private void CreateTimeline()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetFolder + "ParachuteSway.anim");
            Assert.That(clip, Is.Not.Null);
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            track = timeline.CreateTrack<AnimationTrack>(null, "Parachute sway");
            track.trackOffset = TrackOffset.ApplySceneOffsets;
            var timelineClip = track.CreateClip<AnimationPlayableAsset>();
            animation = (AnimationPlayableAsset)timelineClip.asset;
            animation.clip = clip;
            timelineClip.duration = 8;
            director = rig.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.playableAsset = timeline;
            director.SetGenericBinding(track, descent.GetComponent<Animator>());
        }
    }
}
#endif
