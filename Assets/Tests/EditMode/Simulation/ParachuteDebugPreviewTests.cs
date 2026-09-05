using Border.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Simulation.Tests
{
    public sealed class ParachuteDebugPreviewTests
    {
        [TestCase(0f)]
        [TestCase(200f)]
        [TestCase(10000f)]
        public void Timeline_EntersFromAbove_ExitsBelow_AndWaitsAtMissionAltitude(float altitude)
        {
            using var preview = new ParachutePreviewSession(null, altitude, 10f);
            preview.Seek(0f);
            Assert.That(preview.Camera.WorldToViewportPoint(preview.AssemblyBounds.min).y, Is.GreaterThan(1f));
            preview.Seek(1f);
            Assert.That(Mathf.DeltaAngle(0f, preview.Socket.parent.parent.localEulerAngles.z), Is.EqualTo(9f).Within(0.02f));
            Assert.That(Quaternion.Angle(preview.Socket.rotation, preview.Socket.parent.parent.rotation), Is.GreaterThan(12f));
            preview.Seek(5.75f);
            float middle = preview.Camera.WorldToViewportPoint(preview.AssemblyBounds.center).y;
            Assert.That(middle, Is.InRange(0.1f, 0.9f));
            preview.Seek(preview.Duration);
            Assert.That(preview.Camera.WorldToViewportPoint(preview.AssemblyBounds.max).y, Is.LessThan(0f));
            Assert.That(preview.WaitingForReward, Is.True);
            preview.Tick(100f);
            Assert.That(preview.WaitingForReward, Is.True);
            Assert.That(preview.Completed, Is.False);
        }

        [Test]
        public void RocketSwing_LagsBehindCanopy_StaysConnected_AndLoopsWithoutAPop()
        {
            using var preview = new ParachutePreviewSession(null, 200f, 10f);
            Transform swing = preview.Socket.parent;
            Transform canopy = swing.parent;
            preview.Seek(0f);
            Quaternion initialRotation = swing.localRotation;
            Assert.That(Mathf.DeltaAngle(0f, canopy.localEulerAngles.z), Is.EqualTo(0f).Within(0.02f));
            Assert.That(Mathf.DeltaAngle(0f, swing.localEulerAngles.z), Is.GreaterThan(7f));
            preview.Seek(0.35f);
            Assert.That(Mathf.DeltaAngle(0f, swing.localEulerAngles.z), Is.EqualTo(0f).Within(0.02f));
            Assert.That(Mathf.DeltaAngle(0f, canopy.localEulerAngles.z), Is.GreaterThan(4f));
            for (int frame = 0; frame <= 40; frame++)
            {
                preview.Seek(frame * 0.1f);
                Assert.That(Vector3.Distance(preview.Socket.position, swing.position), Is.LessThan(0.001f));
                Assert.That(preview.Socket.childCount, Is.EqualTo(1));
            }
            Assert.That(Quaternion.Angle(swing.localRotation, initialRotation), Is.LessThan(0.02f));
        }

        [Test]
        public void Controls_PauseScrubFinishAndReplay()
        {
            using var preview = new ParachutePreviewSession(null, 200f, 10f);
            preview.Restart();
            preview.Tick(2f);
            preview.TogglePause();
            preview.Tick(3f);
            Assert.That(preview.Time, Is.EqualTo(2f));
            preview.Seek(3f);
            Assert.That(preview.Playing, Is.False);
            preview.TogglePause();
            preview.Tick(1f);
            Assert.That(preview.Time, Is.EqualTo(4f));
            preview.FinishReward();
            Assert.That(preview.Completed, Is.False);
            preview.Seek(preview.Duration);
            preview.FinishReward();
            Assert.That(preview.Completed, Is.True);
            Assert.That(preview.WaitingForReward, Is.False);
            preview.Restart();
            Assert.That(preview.Time, Is.Zero);
            Assert.That(preview.Playing, Is.True);
            Assert.That(preview.Completed, Is.False);
        }

        [Test]
        public void Snapshot_PreservesSceneRocketAndEngine_WithoutCopyingGameplayComponents()
        {
            var source = EditorUtility.CreateGameObjectWithHideFlags("source rocket", HideFlags.HideAndDontSave, typeof(Rocket));
            var body = EditorUtility.CreateGameObjectWithHideFlags("body", HideFlags.HideAndDontSave, typeof(MeshFilter), typeof(MeshRenderer));
            var engine = EditorUtility.CreateGameObjectWithHideFlags("engine", HideFlags.HideAndDontSave, typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/3D/RocketBody.prefab");
                foreach (var visual in new[] { body, engine })
                {
                    visual.transform.SetParent(source.transform, false);
                    visual.transform.localScale = Vector3.one * 400f;
                    visual.transform.localRotation = asset.transform.localRotation;
                    visual.GetComponent<MeshFilter>().sharedMesh = asset.GetComponent<MeshFilter>().sharedMesh;
                    visual.GetComponent<MeshRenderer>().sharedMaterials = asset.GetComponent<MeshRenderer>().sharedMaterials;
                }
                engine.transform.localPosition = new Vector3(1f, -1f, 0f);
                source.transform.SetPositionAndRotation(new Vector3(20f, 60f, 5f), Quaternion.Euler(0f, 0f, 15f));
                source.transform.localScale = Vector3.one * 1.5f;
                bool wasDirty = SceneManager.GetActiveScene().isDirty;
                using (var preview = new ParachutePreviewSession(source, 200f, 10f))
                {
                    Assert.That(preview.RocketVisual.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(2));
                    Assert.That(preview.RocketVisual.GetComponentsInChildren<MonoBehaviour>(), Is.Empty);
                    Assert.That(preview.RocketVisual.GetComponentsInChildren<Rigidbody>(), Is.Empty);
                    preview.Restart();
                    preview.Tick(3f);
                }
                Assert.That(source.transform.position, Is.EqualTo(new Vector3(20f, 60f, 5f)));
                Assert.That(Quaternion.Angle(source.transform.rotation, Quaternion.Euler(0f, 0f, 15f)), Is.LessThan(0.01f));
                Assert.That(source.transform.localScale, Is.EqualTo(Vector3.one * 1.5f));
                Assert.That(source.transform.parent, Is.Null);
                Assert.That(engine.transform.parent, Is.EqualTo(source.transform));
                Assert.That(engine.transform.localPosition, Is.EqualTo(new Vector3(1f, -1f, 0f)));
                Assert.That(SceneManager.GetActiveScene().isDirty, Is.EqualTo(wasDirty));
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void Dispose_RemovesPreviewObjects_AndAllowsAnotherSession()
        {
            var first = new ParachutePreviewSession(null, 200f, 10f);
            Transform socket = first.Socket;
            Camera camera = first.Camera;
            first.Dispose();
            first.Dispose();
            Assert.That(socket == null, Is.True);
            Assert.That(camera == null, Is.True);
            using var second = new ParachutePreviewSession(null, 200f, 10f);
            Assert.That(second.Socket, Is.Not.Null);
        }
    }
}
