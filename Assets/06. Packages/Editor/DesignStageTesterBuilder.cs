using Simulation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DesignStageTesterBuilder
{
    public const string ScenePath = "Assets/00. Scenes/DesignStageTester.unity";
    public const string PrefabPath = "Assets/03. Prefabs/UI/DesignStageTesterUI.prefab";

    [MenuItem("Border/Simulation/Create Design Stage Tester")]
    public static void Create()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new System.InvalidOperationException("Tester scene already exists; edit it directly.");
        AssetDatabase.CopyAsset("Assets/00. Scenes/SimulationTest.unity", ScenePath);
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            Rocket rocket = null;
            RocketBuilder builder = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (rocket == null) rocket = root.GetComponentInChildren<Rocket>(true);
                if (builder == null) builder = root.GetComponentInChildren<RocketBuilder>(true);
            }
            if (rocket == null || builder == null) throw new System.InvalidOperationException("Simulation scene is missing rocket/builder.");
            var library = AssetDatabase.LoadAssetAtPath<EnginePresetLibrarySO>(
                "Assets/02. ScriptableObjects/Simulation/EnginePresetLibrary.asset");
            builder.SetPresetLibrary(library);
            bool hasEventSystem = false;
            foreach (GameObject root in scene.GetRootGameObjects())
                hasEventSystem |= root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null;
            if (!hasEventSystem)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", true));
            new GameObject("Design Stage Tester").AddComponent<DesignStageTester>().Configure(rocket, builder, library);
            var host = new GameObject("Design Stage Tester UI");
            host.SetActive(false);
            host.AddComponent<RocketDesignUI>().BakeTesterInterface(builder);
            foreach (TMP_Text text in host.GetComponentsInChildren<TMP_Text>(true))
                if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
            host.SetActive(true);
            PrefabUtility.SaveAsPrefabAssetAndConnect(host, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
