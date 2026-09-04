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
        private const string ResearchHostName = "Research Operation UI Controller";

        static ResearchPrototypeSceneBootstrap()
        {
            EditorApplication.delayCall += TryInstallInActiveScene;
            EditorSceneManager.sceneOpened += (_, _) => TryInstallInActiveScene();
        }

        [MenuItem("Border/Research/Install Research Prototype In Active Scene")]
        public static void TryInstallInActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            if (scene.name == ResearchFlowSession.MainSceneName)
            {
                InstallResearchController(scene);
            }
        }

        [MenuItem("Border/Research/Debug/Enter Design In 01_Main")]
        public static void EnterDesignInMain()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Design In 01_Main은 Play 모드에서만 동작합니다.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != ResearchFlowSession.MainSceneName)
            {
                Debug.LogWarning("Enter Design In 01_Main은 01_Main Play 중에만 동작합니다.");
                return;
            }

            ResearchOperationUIController controller = Object.FindFirstObjectByType<ResearchOperationUIController>();
            if (controller == null)
            {
                var host = new GameObject(ResearchHostName);
                SceneManager.MoveGameObjectToScene(host, scene);
                controller = host.AddComponent<ResearchOperationUIController>();
            }

            controller.InitializeForTests();
            controller.EnterDesignDebugForEditor();
        }

        private static void InstallResearchController(Scene scene, bool registerUndo = true)
        {
            if (SceneHasComponent<ResearchOperationUIController>(scene))
            {
                return;
            }

            var host = new GameObject(ResearchHostName);
            if (registerUndo)
            {
                Undo.RegisterCreatedObjectUndo(host, "Install Research Operation UI");
            }

            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<ResearchOperationUIController>();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool SceneHasComponent<T>(Scene scene)
            where T : Component
        {
            if (!scene.IsValid())
            {
                return false;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<T>(includeInactive: true) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
