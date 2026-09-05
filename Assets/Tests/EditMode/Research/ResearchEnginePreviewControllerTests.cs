using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Border.Research.Tests
{
    public sealed class ResearchEnginePreviewControllerTests
    {
        [Test]
        public void Show_ChangesPreset_ReplacesPreviousInstance()
        {
            GameObject host = new GameObject("Preview Host");
            GameObject root = new GameObject("Preview Root");
            GameObject firstPrefab = new GameObject("Engine One");
            GameObject secondPrefab = new GameObject("Engine Two");
            EnginePresetVisualLibrarySO library = EnginePresetVisualLibrarySO.CreateRuntime(new[] { firstPrefab, secondPrefab });

            try
            {
                root.transform.SetParent(host.transform, false);
                ResearchEnginePreviewController controller = host.AddComponent<ResearchEnginePreviewController>();
                SetPrivateField(controller, "previewRoot", root.transform);
                SetPrivateField(controller, "visualLibrary", library);

                controller.Show(EnginePresetId.Engine01);
                GameObject firstInstance = controller.ActiveInstance;

                controller.Show(EnginePresetId.Engine02);

                Assert.That(firstInstance == null, Is.True);
                Assert.That(controller.ActiveInstance, Is.Not.Null);
                Assert.That(controller.ActiveInstance.name, Is.EqualTo("Engine Two_HologramPreview"));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                Assert.That(controller.ActivePresetId, Is.EqualTo(EnginePresetId.Engine02));
            }
            finally
            {
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(firstPrefab);
                Object.DestroyImmediate(secondPrefab);
            }
        }

        [Test]
        public void Show_SamePreset_DoesNotDuplicateInstance()
        {
            GameObject host = new GameObject("Preview Host");
            GameObject root = new GameObject("Preview Root");
            GameObject prefab = new GameObject("Engine One");
            EnginePresetVisualLibrarySO library = EnginePresetVisualLibrarySO.CreateRuntime(new[] { prefab });

            try
            {
                root.transform.SetParent(host.transform, false);
                ResearchEnginePreviewController controller = host.AddComponent<ResearchEnginePreviewController>();
                SetPrivateField(controller, "previewRoot", root.transform);
                SetPrivateField(controller, "visualLibrary", library);

                controller.Show(EnginePresetId.Engine01);
                GameObject firstInstance = controller.ActiveInstance;
                controller.Show(EnginePresetId.Engine01);

                Assert.That(controller.ActiveInstance, Is.SameAs(firstInstance));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void Show_MissingPresetVisual_UsesDefaultPrefab()
        {
            GameObject host = new GameObject("Preview Host");
            GameObject root = new GameObject("Preview Root");
            GameObject fallbackPrefab = new GameObject("Default Engine");
            EnginePresetVisualLibrarySO library = EnginePresetVisualLibrarySO.CreateRuntime(new GameObject[] { null });

            try
            {
                root.transform.SetParent(host.transform, false);
                ResearchEnginePreviewController controller = host.AddComponent<ResearchEnginePreviewController>();
                SetPrivateField(controller, "previewRoot", root.transform);
                SetPrivateField(controller, "visualLibrary", library);
                SetPrivateField(controller, "defaultPreviewPrefab", fallbackPrefab);

                controller.Show(EnginePresetId.Engine01);

                Assert.That(controller.ActiveInstance, Is.Not.Null);
                Assert.That(controller.ActiveInstance.name, Is.EqualTo("Default Engine_HologramPreview"));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(fallbackPrefab);
            }
        }

        [Test]
        public void ShowHologram_UsesArchetypePrefabBeforePresetPrefab()
        {
            GameObject host = new GameObject("Preview Host");
            GameObject root = new GameObject("Preview Root");
            GameObject presetPrefab = new GameObject("Preset Engine");
            GameObject fuelPrefab = new GameObject("Fuel Engine");
            EnginePresetVisualLibrarySO library = EnginePresetVisualLibrarySO.CreateRuntime(
                new[] { presetPrefab },
                new GameObject[] { null, fuelPrefab });

            try
            {
                root.transform.SetParent(host.transform, false);
                ResearchEnginePreviewController controller = host.AddComponent<ResearchEnginePreviewController>();
                SetPrivateField(controller, "previewRoot", root.transform);
                SetPrivateField(controller, "visualLibrary", library);

                controller.ShowHologram(EnginePresetId.Engine01, EngineVisualArchetype.FuelCapacity);

                Assert.That(controller.ActiveInstance, Is.Not.Null);
                Assert.That(controller.ActiveInstance.name, Is.EqualTo("Fuel Engine_HologramPreview"));
                Assert.That(controller.ActiveArchetype, Is.EqualTo(EngineVisualArchetype.FuelCapacity));
                Assert.That(controller.ActiveInstance.transform.localEulerAngles.x, Is.EqualTo(270f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(presetPrefab);
                Object.DestroyImmediate(fuelPrefab);
            }
        }

        [Test]
        public void PlayMaterialize_ZeroDuration_ReplacesHologramWithSolidPreviewAndInvokesCallback()
        {
            GameObject host = new GameObject("Preview Host");
            GameObject root = new GameObject("Preview Root");
            GameObject prefab = new GameObject("Engine One");
            EnginePresetVisualLibrarySO library = EnginePresetVisualLibrarySO.CreateRuntime(new[] { prefab });
            bool completed = false;

            try
            {
                root.transform.SetParent(host.transform, false);
                ResearchEnginePreviewController controller = host.AddComponent<ResearchEnginePreviewController>();
                SetPrivateField(controller, "previewRoot", root.transform);
                SetPrivateField(controller, "visualLibrary", library);
                SetPrivateField(controller, "materializeDuration", 0f);

                controller.ShowHologram(EnginePresetId.Engine01, EngineVisualArchetype.Balanced);
                GameObject hologramInstance = controller.ActiveInstance;

                controller.PlayMaterialize(EnginePresetId.Engine01, EngineVisualArchetype.Balanced, () => completed = true);

                Assert.That(completed, Is.True);
                Assert.That(hologramInstance == null, Is.True);
                Assert.That(controller.ActiveInstance, Is.Not.Null);
                Assert.That(controller.ActiveInstance.name, Is.EqualTo("Engine One_Preview"));
                Assert.That(controller.ActiveInstance.transform.localEulerAngles.x, Is.EqualTo(270f).Within(0.001f));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void ShowHologram_WithoutAssignedMaterial_UsesUberHologramFallback()
        {
            GameObject host = new GameObject("Preview Host");
            GameObject root = new GameObject("Preview Root");
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);

            try
            {
                root.transform.SetParent(host.transform, false);
                ResearchEnginePreviewController controller = host.AddComponent<ResearchEnginePreviewController>();
                SetPrivateField(controller, "previewRoot", root.transform);
                SetPrivateField(controller, "defaultPreviewPrefab", prefab);
                SetPrivateField(controller, "normalizePreviewBounds", false);

                controller.ShowHologram(EnginePresetId.Engine01, EngineVisualArchetype.Balanced);

                Renderer renderer = controller.ActiveInstance.GetComponent<Renderer>();
                Material material = renderer.sharedMaterial;
                Assert.That(material.shader.name, Is.EqualTo("Shader/Uber/3D Object"));
                Assert.That(material.GetFloat("_HologramEnabled"), Is.EqualTo(1f));
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.True);
                Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
                Assert.That(material.GetFloat("_Blend"), Is.EqualTo(2f));
                Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo((float)BlendMode.One));
                Assert.That(material.GetFloat("_HologramScanlineSpeed"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_HologramNoiseSpeed"), Is.EqualTo(0f));
                Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                Assert.That(renderer.receiveShadows, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void ShowHologram_DoesNotStartRuntimePulse()
        {
            string sourcePath = Path.Combine(Application.dataPath,
                "01. Scripts/Research/ResearchEnginePreviewController.cs");
            string source = File.ReadAllText(sourcePath);
            Match showHologram = Regex.Match(source,
                @"(?s)public void ShowHologram\(EnginePresetId presetId, EngineVisualArchetype archetype\).*?\r?\n        }\r?\n\r?\n        public void PlayMaterialize");

            Assert.That(showHologram.Success, Is.True);
            StringAssert.DoesNotContain("StartHologramPulse", showHologram.Value);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

    }
}
