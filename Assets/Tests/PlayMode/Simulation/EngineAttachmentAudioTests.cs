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
    public sealed class EngineAttachmentAudioTests
    {
        private GameObject soundRoot, host, partHost;
        private RocketBuilder builder;
        private Rocket rocket;
        private RocketPart part;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsNull(SoundManager.Instance);
            soundRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Systems/SoundManager.prefab"));
            soundRoot.AddComponent<AudioListener>();
            host = new GameObject("attachment test");
            rocket = host.AddComponent<Rocket>();
            rocket.enabled = false;
            builder = host.AddComponent<RocketBuilder>();
            builder.enabled = false;
            Set("rocket", rocket);
            partHost = new GameObject("engine", typeof(BoxCollider));
            part = partHost.AddComponent<RocketPart>();
            part.enabled = false;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(partHost);
            Object.Destroy(host);
            Object.Destroy(soundRoot);
            yield return null;
        }

        [Test]
        public void ExistingEngine_FirstMovementDetachesOnce_ReleasePlaysTwoAttachVoices()
        {
            Set("_dragParent", host.transform);
            Call("BeginDragMovement");
            Call("BeginDragMovement");
            Assert.AreEqual(1, Sounds("engine_detach"));
            PrepareRelease(true, true);
            Call("EndDrag");
            Assert.AreEqual(2, Sounds("engine_attach"));
            Assert.AreEqual(host.transform, part.transform.parent);
        }

        [Test]
        public void SelectionAndInvalidDrop_AreSilent_NewPresetHasNoDetach()
        {
            Set("_dragParent", host.transform);
            PrepareRelease(false, false);
            Call("EndDrag");
            Assert.AreEqual(0, Sounds("engine_attach"));
            Assert.AreEqual(0, Sounds("engine_detach"));
            Set("_dragMoved", true); // New preset follows the mouse from the first frame.
            Call("BeginDragMovement");
            Assert.AreEqual(0, Sounds("engine_detach"));
            PrepareRelease(true, false);
            Call("EndDrag");
            Assert.AreEqual(0, Sounds("engine_attach"));
        }

        private void PrepareRelease(bool moved, bool valid)
        {
            Set("_dragged", part);
            Set("_draggedCollider", partHost.GetComponent<Collider>());
            Set("_dragMoved", moved);
            Set("_overRocket", valid);
            Set("_attachPoint", Vector3.right);
        }
        private int Sounds(string id) => soundRoot.GetComponentsInChildren<AudioSource>()
            .Count(source => source.clip != null && source.clip.name == id);
        private void Set(string name, object value) => typeof(RocketBuilder)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(builder, value);
        private void Call(string name) => typeof(RocketBuilder)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(builder, null);
    }
}
#endif
