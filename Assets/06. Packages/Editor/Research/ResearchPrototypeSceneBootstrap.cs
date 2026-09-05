using System;
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
        private const string PreviewPlacementCubeName = "Cube";
        private const string ResearchCinemachineCameraName = "Research Cinemachine Camera";
        private const string MainScenePath = "Assets/00. Scenes/01_Main.unity";
        private const string ResearchLabPrefabPath = "Assets/03. Prefabs/3D/room.prefab";
        private const string DefaultEnginePreviewPrefabPath = "Assets/05. Arts/FBX/Engine/BaseEngine/Meshy_AI__0904142514_texture.prefab";
        private const string BalancedEnginePreviewPrefabPath = "Assets/05. Arts/FBX/Engine/Full/Full.prefab";
        private const string FuelCapacityEnginePreviewPrefabPath = "Assets/03. Prefabs/3D/Engine_01.prefab";
        private const string CoolingEnginePreviewPrefabPath = "Assets/05. Arts/FBX/Engine/Cold/Cold.prefab";
        private const string MaxOutputEnginePreviewPrefabPath = "Assets/05. Arts/FBX/Engine/Power/Power.prefab";
        private const string IgnitionReliabilityEnginePreviewPrefabPath = "Assets/05. Arts/FBX/Engine/Reliability/Reliability.prefab";
        private const string VisualLibraryFolderPath = "Assets/02. ScriptableObjects/Research";
        private const string VisualLibraryAssetPath = VisualLibraryFolderPath + "/EnginePresetVisualLibrary.asset";
        private static readonly Vector3 DefaultPreviewLocalPosition = new(-0.81f, 0.65f, 0f);

        [MenuItem("Border/Research/Install Session and Result Screens")]
        public static void InstallSessionAndResultScreens()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Install research session and screens in Edit Mode only.");

            ResearchBalanceConfigSO balance = AssetDatabase.LoadAssetAtPath<ResearchBalanceConfigSO>("Assets/02. ScriptableObjects/Research/ResearchBalanceConfig.asset");
            GameObject reportPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchResultReport.prefab");
            GameObject endingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03. Prefabs/UI/ResearchEnding.prefab");
            if (balance == null || reportPrefab == null || endingPrefab == null)
                throw new InvalidOperationException("Research balance and outcome prefabs must exist before installation.");

            Scene activeScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(MainScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
            try
            {
                ResearchOperationUIController controller = FindComponentInScene<ResearchOperationUIController>(scene);
                if (controller == null) throw new InvalidOperationException("Main scene has no research operation controller.");
                ResearchFlowSession session = FindComponentInScene<ResearchFlowSession>(scene);
                if (session == null)
                {
                    var host = new GameObject("Research Flow Session");
                    SceneManager.MoveGameObjectToScene(host, scene);
                    session = host.AddComponent<ResearchFlowSession>();
                }
                var sessionData = new SerializedObject(session);
                sessionData.FindProperty("balanceConfig").objectReferenceValue = balance;
                sessionData.ApplyModifiedPropertiesWithoutUndo();

                ResearchResultReportController report = FindComponentInScene<ResearchResultReportController>(scene);
                if (report == null)
                    report = ((GameObject)PrefabUtility.InstantiatePrefab(reportPrefab, scene)).GetComponent<ResearchResultReportController>();
                ResearchEndingController ending = FindComponentInScene<ResearchEndingController>(scene);
                if (ending == null)
                    ending = ((GameObject)PrefabUtility.InstantiatePrefab(endingPrefab, scene)).GetComponent<ResearchEndingController>();
                report.Hide();
                ending.Hide();
                var controllerData = new SerializedObject(controller);
                controllerData.FindProperty("resultReport").objectReferenceValue = report;
                controllerData.FindProperty("endingScreen").objectReferenceValue = ending;
                controllerData.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save research scene connections.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
            }
        }

        static ResearchPrototypeSceneBootstrap()
        {
            EditorApplication.delayCall += TryInstallInActiveScene;
            EditorSceneManager.sceneOpened += (_, _) => TryInstallInActiveScene();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += TryInstallInActiveScene;
            }
        }

        [MenuItem("Border/Research/Install Research Prototype In Active Scene")]
        public static void TryInstallInActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            if (scene.name == ResearchFlowSession.MainSceneName)
            {
                InstallResearchController(scene, installEngineLab: true);
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

            ResearchOperationUIController controller = UnityEngine.Object.FindFirstObjectByType<ResearchOperationUIController>();
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
                changed |= EnsureResearchCinemachineCamera(scene, registerUndo, out Transform researchCameraTransform);
                changed |= ConfigureResearchController(controller, preview, researchLabRoot, researchCameraTransform);
                EnsureVisibleEditModePreview(preview);
            }

            if (changed && !EditorApplication.isPlayingOrWillChangePlaymode)
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

            Transform researchLabTransform = FindRoot(scene, ResearchLabName)?.transform;
            Transform placementCube = FindPreviewPlacementCube(researchLabTransform);
            Transform previewRoot = FindPreviewRoot(scene, placementCube);
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

            if (placementCube != null)
            {
                if (previewRoot.parent != placementCube.parent)
                {
                    previewRoot.SetParent(placementCube.parent, false);
                    changed = true;
                }

                if (previewRoot.localPosition != placementCube.localPosition || previewRoot.localRotation != placementCube.localRotation)
                {
                    previewRoot.SetLocalPositionAndRotation(placementCube.localPosition, placementCube.localRotation);
                    changed = true;
                }
            }
            else
            {
                Quaternion fallbackRotation = Quaternion.identity;
                if (researchLabTransform != null)
                {
                    if (previewRoot.parent != researchLabTransform)
                    {
                        previewRoot.SetParent(researchLabTransform, false);
                        changed = true;
                    }

                    if (previewRoot.localPosition != DefaultPreviewLocalPosition || previewRoot.localRotation != fallbackRotation)
                    {
                        previewRoot.SetLocalPositionAndRotation(DefaultPreviewLocalPosition, fallbackRotation);
                        changed = true;
                    }
                }
                else
                {
                    if (previewRoot.parent != null)
                    {
                        previewRoot.SetParent(null, false);
                        changed = true;
                    }

                    if (previewRoot.position != DefaultPreviewLocalPosition || previewRoot.rotation != fallbackRotation)
                    {
                        previewRoot.SetPositionAndRotation(DefaultPreviewLocalPosition, fallbackRotation);
                        changed = true;
                    }
                }
            }

            if (previewRoot.localScale != Vector3.one)
            {
                previewRoot.localScale = Vector3.one;
                changed = true;
            }

            EnginePresetVisualLibrarySO visualLibrary = EnsureVisualLibraryAsset();
            GameObject defaultPreviewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultEnginePreviewPrefabPath);
            changed |= EnsureDefaultArchetypePrefabs(visualLibrary, defaultPreviewPrefab);

            var serializedPreview = new SerializedObject(preview);
            changed |= SetObjectReference(serializedPreview, "previewRoot", previewRoot);
            changed |= SetObjectReference(serializedPreview, "visualLibrary", visualLibrary);
            changed |= SetObjectReference(serializedPreview, "defaultPreviewPrefab", defaultPreviewPrefab);
            changed |= SetBool(serializedPreview, "normalizePreviewBounds", true);
            changed |= SetFloat(serializedPreview, "targetPreviewHeight", 1.25f);
            changed |= SetFloat(serializedPreview, "targetPreviewGroundY", -0.5f);
            changed |= SetVector3(serializedPreview, "previewLocalEulerAngles", new Vector3(-90f, 0f, 0f));
            changed |= SetBool(serializedPreview, "showEditModePreview", true);
            if (serializedPreview.ApplyModifiedProperties())
            {
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(previewRoot);
                EditorUtility.SetDirty(preview);
            }

            return preview;
        }

        private static void EnsureVisibleEditModePreview(ResearchEnginePreviewController preview)
        {
            if (preview == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            preview.ShowHologram(EnginePresetId.Engine01, EngineVisualArchetype.Balanced);
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

        private static bool EnsureDefaultArchetypePrefabs(EnginePresetVisualLibrarySO library, GameObject defaultPreviewPrefab)
        {
            if (library == null || defaultPreviewPrefab == null)
            {
                return false;
            }

            bool changed = false;
            changed |= SetArchetypePreviewPrefab(
                library,
                EngineVisualArchetype.Balanced,
                LoadEnginePreviewPrefab(BalancedEnginePreviewPrefabPath, defaultPreviewPrefab));
            changed |= SetArchetypePreviewPrefab(
                library,
                EngineVisualArchetype.FuelCapacity,
                LoadEnginePreviewPrefab(FuelCapacityEnginePreviewPrefabPath, defaultPreviewPrefab));
            changed |= SetArchetypePreviewPrefab(
                library,
                EngineVisualArchetype.Cooling,
                LoadEnginePreviewPrefab(CoolingEnginePreviewPrefabPath, defaultPreviewPrefab));
            changed |= SetArchetypePreviewPrefab(
                library,
                EngineVisualArchetype.MaxOutput,
                LoadEnginePreviewPrefab(MaxOutputEnginePreviewPrefabPath, defaultPreviewPrefab));
            changed |= SetArchetypePreviewPrefab(
                library,
                EngineVisualArchetype.IgnitionReliability,
                LoadEnginePreviewPrefab(IgnitionReliabilityEnginePreviewPrefabPath, defaultPreviewPrefab));

            if (changed)
            {
                EditorUtility.SetDirty(library);
            }

            return changed;
        }

        private static bool SetArchetypePreviewPrefab(
            EnginePresetVisualLibrarySO library,
            EngineVisualArchetype archetype,
            GameObject prefab)
        {
            if (library.GetPreviewPrefab(archetype) == prefab)
            {
                return false;
            }

            library.SetArchetypePreviewPrefab(archetype, prefab);
            return true;
        }

        private static GameObject LoadEnginePreviewPrefab(string path, GameObject fallback)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab : fallback;
        }

        private static bool ConfigureResearchController(
            ResearchOperationUIController controller,
            ResearchEnginePreviewController preview,
            Transform researchLabRoot,
            Transform researchCameraTransform)
        {
            if (controller == null || preview == null)
            {
                return false;
            }

            var serializedController = new SerializedObject(controller);
            bool changed = SetObjectReference(serializedController, "enginePreview", preview);
            changed |= SetObjectReference(serializedController, "researchLabRoot", researchLabRoot);
            changed |= SetObjectReference(serializedController, "researchCameraTransform", researchCameraTransform);
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

        private static bool EnsureResearchCinemachineCamera(Scene scene, bool registerUndo, out Transform researchCameraTransform)
        {
            researchCameraTransform = null;
            Type brainType = Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
            Type cameraType = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            if (brainType == null || cameraType == null)
            {
                Debug.LogWarning("Cinemachine package is installed in manifest, but runtime types were not available.");
                return false;
            }

            Camera unityCamera = FindComponentInScene<Camera>(scene);
            if (unityCamera == null)
            {
                return false;
            }

            bool changed = false;
            Component brain = unityCamera.GetComponent(brainType);
            if (brain == null)
            {
                brain = unityCamera.gameObject.AddComponent(brainType);
                if (registerUndo)
                {
                    Undo.RegisterCreatedObjectUndo(brain, "Add Cinemachine Brain");
                }

                changed = true;
            }

            GameObject virtualCameraObject = FindRoot(scene, ResearchCinemachineCameraName);
            Component virtualCamera;
            if (virtualCameraObject == null)
            {
                virtualCameraObject = new GameObject(ResearchCinemachineCameraName);
                if (registerUndo)
                {
                    Undo.RegisterCreatedObjectUndo(virtualCameraObject, "Install Research Cinemachine Camera");
                }

                SceneManager.MoveGameObjectToScene(virtualCameraObject, scene);
                virtualCameraObject.transform.SetPositionAndRotation(unityCamera.transform.position, unityCamera.transform.rotation);
                virtualCamera = virtualCameraObject.AddComponent(cameraType);
                changed = true;
            }
            else
            {
                virtualCamera = virtualCameraObject.GetComponent(cameraType);
                if (virtualCamera == null)
                {
                    virtualCamera = virtualCameraObject.AddComponent(cameraType);
                    if (registerUndo)
                    {
                        Undo.RegisterCreatedObjectUndo(virtualCamera, "Add Research Cinemachine Camera");
                    }

                    changed = true;
                }
            }

            var serializedCamera = new SerializedObject(virtualCamera);
            changed |= SetInt(serializedCamera, "Priority.m_Value", 20);
            changed |= SetBool(serializedCamera, "Priority.Enabled", true);
            changed |= SetFloat(serializedCamera, "Lens.FieldOfView", unityCamera.fieldOfView);
            if (serializedCamera.ApplyModifiedProperties())
            {
                changed = true;
            }

            researchCameraTransform = virtualCameraObject.transform;

            if (changed)
            {
                EditorUtility.SetDirty(unityCamera);
                EditorUtility.SetDirty(brain);
                EditorUtility.SetDirty(virtualCameraObject);
                EditorUtility.SetDirty(virtualCamera);
            }

            return changed;
        }

        private static bool SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
            {
                return false;
            }

            property.boolValue = value;
            return true;
        }

        private static bool SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || Mathf.Approximately(property.floatValue, value))
            {
                return false;
            }

            property.floatValue = value;
            return true;
        }

        private static bool SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.intValue == value)
            {
                return false;
            }

            property.intValue = value;
            return true;
        }

        private static bool SetVector3(SerializedObject serializedObject, string propertyName, Vector3 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.vector3Value == value)
            {
                return false;
            }

            property.vector3Value = value;
            return true;
        }

        private static bool SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
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

        private static Transform FindPreviewRoot(Scene scene, Transform placementCube)
        {
            Transform firstMatch = null;
            Transform preferredMatch = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != PreviewRootName)
                    {
                        continue;
                    }

                    firstMatch ??= child;
                    if (placementCube != null && child.parent == placementCube.parent)
                    {
                        preferredMatch ??= child;
                    }
                }
            }

            return preferredMatch != null ? preferredMatch : firstMatch;
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

        private static Transform FindPreviewPlacementCube(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != PreviewPlacementCubeName)
                {
                    continue;
                }

                if (!PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                {
                    return child;
                }
            }

            return null;
        }
    }
}
