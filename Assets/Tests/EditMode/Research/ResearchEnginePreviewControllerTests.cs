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
                Assert.That(controller.ActiveInstance.name, Is.EqualTo("Engine Two_Preview"));
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
                Assert.That(controller.ActiveInstance.name, Is.EqualTo("Default Engine_Preview"));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(fallbackPrefab);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
