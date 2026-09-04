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
        private const string ResearchLabName = "Engine Research Lab";
        private const string PreviewHostName = "Research Engine Preview Controller";
        private const string PreviewRootName = "EnginePreviewRoot";
        private const string MainScenePath = "Assets/00. Scenes/01_Main.unity";
        private const string ResearchLabPrefabPath = "Assets/03. Prefabs/3D/room.prefab";
        private const string DefaultEnginePreviewPrefabPath = "Assets/05. Arts/FBX/Engine/BaseEngine/Meshy_AI__0904142514_texture.prefab";
        private const string VisualLibraryFolderPath = "Assets/02. ScriptableObjects/Research";
        private const string VisualLibraryAssetPath = VisualLibraryFolderPath + "/EnginePresetVisualLibrary.asset";

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
                InstallResearchController(scene, installEngineLab: false);
            }
        }

        [MenuItem("Border/Research/Apply Engine Lab To 01_Main")]
        public static void ApplyEngineLabToMainScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != ResearchFlowSession.MainSceneName)
            {
                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }

            InstallResearchController(scene, registerUndo: false, installEngineLab: true);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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

        private static void InstallResearchController(Scene scene, bool registerUndo = true, bool installEngineLab = false)
        {
            if (!scene.IsValid())
            {
                return;
            }

            bool changed = false;
            ResearchOperationUIController controller = FindComponentInScene<ResearchOperationUIController>(scene);
            if (controller == null)
            {
                var host = new GameObject(ResearchHostName);
                if (registerUndo)
                {
                    Undo.RegisterCreatedObjectUndo(host, "Install Research Operation UI");
                }

                SceneManager.MoveGameObjectToScene(host, scene);
                controller = host.AddComponent<ResearchOperationUIController>();
                changed = true;
            }

            if (installEngineLab)
            {
                Transform researchLabRoot = EnsureResearchLab(scene, registerUndo, out bool researchLabChanged);
                changed |= researchLabChanged;
                ResearchEnginePreviewController preview = EnsureEnginePreview(scene, registerUndo, out bool previewChanged);
                changed |= previewChanged;
                changed |= ConfigureResearchController(controller, preview, researchLabRoot);
                changed |= ConfigureSceneCamera(scene);
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static Transform EnsureResearchLab(Scene scene, bool registerUndo, out bool changed)
        {
            changed = false;
            Transform labRoot = FindRoot(scene, ResearchLabName)?.transform;
            if (labRoot == null)
            {
                GameObject labPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResearchLabPrefabPath);
                if (labPrefab == null)
                {
                    Debug.LogWarning($"Engine research lab prefab was not found: {ResearchLabPrefabPath}");
                    return null;
                }

                GameObject lab = (GameObject)PrefabUtility.InstantiatePrefab(labPrefab, scene);
                lab.name = ResearchLabName;
                labRoot = lab.transform;
                if (registerUndo)
                {
                    Undo.RegisterCreatedObjectUndo(lab, "Install Engine Research Lab");
                }

                changed = true;
            }

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.Euler(0f, 180f, 0f);
            if (labRoot.position != position || labRoot.rotation != rotation)
            {
                labRoot.SetPositionAndRotation(position, rotation);
                changed = true;
            }

            if (labRoot.localScale != Vector3.one)
            {
                labRoot.localScale = Vector3.one;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(labRoot);
            }

            return labRoot;
        }

        private static ResearchEnginePreviewController EnsureEnginePreview(Scene scene, bool registerUndo, out bool changed)
        {
            changed = false;
            ResearchEnginePreviewController preview = FindComponentInScene<ResearchEnginePreviewController>(scene);
            if (preview == null)
            {
                var host = new GameObject(PreviewHostName);
                if (registerUndo)
                {
                    Undo.RegisterCreatedObjectUndo(host, "Install Engine Preview Controller");
                }

                SceneManager.MoveGameObjectToScene(host, scene);
                preview = host.AddComponent<ResearchEnginePreviewController>();
                changed = true;
            }

            Transform previewRoot = FindRoot(scene, PreviewRootName)?.transform;
            if (previewRoot == null)
            {
                var root = new GameObject(PreviewRootName);
                if (registerUndo)
                {
                    Undo.RegisterCreatedObjectUndo(root, "Install Engine Preview Root");
                }

                SceneManager.MoveGameObjectToScene(root, scene);
                previewRoot = root.transform;
                changed = true;
            }

            previewRoot.SetPositionAndRotation(new Vector3(0f, 0.9f, -1.5f), Quaternion.Euler(0f, 180f, 0f));
            previewRoot.localScale = Vector3.one;

            EnginePresetVisualLibrarySO visualLibrary = EnsureVisualLibraryAsset();
            GameObject defaultPreviewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultEnginePreviewPrefabPath);

            var serializedPreview = new SerializedObject(preview);
            changed |= SetObjectReference(serializedPreview, "previewRoot", previewRoot);
            changed |= SetObjectReference(serializedPreview, "visualLibrary", visualLibrary);
            changed |= SetObjectReference(serializedPreview, "defaultPreviewPrefab", defaultPreviewPrefab);
            if (serializedPreview.ApplyModifiedProperties())
            {
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(preview);
            }

            return preview;
        }

        private static EnginePresetVisualLibrarySO EnsureVisualLibraryAsset()
        {
            EnginePresetVisualLibrarySO library = AssetDatabase.LoadAssetAtPath<EnginePresetVisualLibrarySO>(VisualLibraryAssetPath);
            if (library != null)
            {
                return library;
            }

            EnsureFolder(VisualLibraryFolderPath);
            library = ScriptableObject.CreateInstance<EnginePresetVisualLibrarySO>();
            AssetDatabase.CreateAsset(library, VisualLibraryAssetPath);
            return library;
        }

        private static bool ConfigureResearchController(ResearchOperationUIController controller, ResearchEnginePreviewController preview, Transform researchLabRoot)
        {
            if (controller == null || preview == null)
            {
                return false;
            }

            var serializedController = new SerializedObject(controller);
            bool changed = SetObjectReference(serializedController, "enginePreview", preview);
            changed |= SetObjectReference(serializedController, "researchLabRoot", researchLabRoot);
            if (serializedController.ApplyModifiedProperties())
            {
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(controller);
            }

            return changed;
        }

        private static bool ConfigureSceneCamera(Scene scene)
        {
            Camera camera = FindComponentInScene<Camera>(scene);
            if (camera == null)
            {
                return false;
            }

            bool changed = false;
            Vector3 position = new(2.6f, 1.75f, -4.8f);
            Quaternion rotation = Quaternion.Euler(8f, -30f, 0f);
            if (camera.transform.position != position || camera.transform.rotation != rotation)
            {
                camera.transform.SetPositionAndRotation(position, rotation);
                changed = true;
            }

            if (!Mathf.Approximately(camera.fieldOfView, 62f))
            {
                camera.fieldOfView = 62f;
                changed = true;
            }

            if (camera.clearFlags != CameraClearFlags.SolidColor)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(camera);
                EditorUtility.SetDirty(camera.transform);
            }

            return changed;
        }

        private static bool SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene)
            where T : Component
        {
            if (!scene.IsValid())
            {
                return null;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(includeInactive: true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
