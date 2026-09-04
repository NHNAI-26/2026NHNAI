using Border.Research;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Border.Editor.Research
{
    [InitializeOnLoad]
    public static class ResearchPrototypeSceneBootstrap
    {
        private const string TargetSceneName = "ResearchTestScene";
        private const string HostName = "Research Operation UI Controller";

        static ResearchPrototypeSceneBootstrap()
        {
            EditorApplication.delayCall += TryInstallInActiveScene;
            EditorSceneManager.sceneOpened += (_, _) => TryInstallInActiveScene();
        }

        [MenuItem("Border/Research/Install Research Prototype In Active Scene")]
        public static void TryInstallInActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != TargetSceneName)
            {
                return;
            }

            if (Object.FindFirstObjectByType<ResearchOperationUIController>() != null)
            {
                return;
            }

            var host = new GameObject(HostName);
            Undo.RegisterCreatedObjectUndo(host, "Install Research Prototype");
            host.AddComponent<ResearchOperationUIController>();
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
