using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
