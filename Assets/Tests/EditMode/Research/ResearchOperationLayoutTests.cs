using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Research.Tests
{
    public sealed class ResearchOperationLayoutTests
    {
        [TestCase("EnginePresetCard")]
        [TestCase("ResearchOperationScreen")]
        public void NameEditorIcons_HaveFixedSizeAndDoNotOverlapText(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/03. Prefabs/UI/Resources/ResearchUI/{prefabName}.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var cards = instance.GetComponentsInChildren<EnginePresetNameEditor>(true);
                Assert.That(cards.Length, Is.EqualTo(prefabName == "EnginePresetCard" ? 1 : 10));
                foreach (var card in cards)
                {
                    var root = (RectTransform)card.transform;
                    root.sizeDelta = new Vector2(280f, 46f);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                    var content = root.Find("Content");
                    var icon = (RectTransform)content.Find("EngineIcon");
                    var title = (RectTransform)content.Find("Title");
                    Assert.That(icon.rect.width, Is.EqualTo(30f).Within(0.01f));
                    Assert.That(icon.rect.height, Is.EqualTo(30f).Within(0.01f));
                    var corners = new Vector3[4];
                    icon.GetWorldCorners(corners);
                    float iconRight = root.InverseTransformPoint(corners[2]).x;
                    title.GetWorldCorners(corners);
                    float titleLeft = root.InverseTransformPoint(corners[0]).x;
                    Assert.That(titleLeft - iconRight, Is.GreaterThanOrEqualTo(7.9f));
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void Initialize_ReusesPrefabNamedScreenWithoutCreatingAnotherCanvas()
        {
            ResearchFlowSession.ResetForTests();
            var host = new GameObject("Operation layout test");
            try
            {
                var prefab = Resources.Load<GameObject>("ResearchUI/ResearchOperationScreen");
                var existing = Object.Instantiate(prefab, host.transform);
                existing.name = "ResearchOperationScreen";
                var controller = host.AddComponent<ResearchOperationUIController>();
                controller.InitializeForTests();
                Assert.That(host.GetComponentsInChildren<Canvas>(true).Length, Is.EqualTo(1));
                Assert.That(host.transform.Find("ResearchOperationCanvas").gameObject, Is.SameAs(existing));
                Assert.That(host.transform.Find("ResearchOperationScreen"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
                ResearchFlowSession.ResetForTests();
            }
        }
    }
}
