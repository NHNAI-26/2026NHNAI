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

    private const string TesterName = "Design Stage Tester";
    private const string TesterUiName = "Design Stage Tester UI";

    [MenuItem("Border/Simulation/Create Design Stage Tester")]
    public static void Create()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new System.InvalidOperationException(
                "Tester scene already exists; use Border/UI/Debug/Rebake Design Stage Tester UI.");
        AssetDatabase.CopyAsset("Assets/00. Scenes/SimulationTest.unity", ScenePath);
        OpenAndBake();
    }

    // 로켓 설계 UI 는 전부 코드 생성이라 손으로 배치할 프리팹이 없다. 미리보기 = 베이크본을 코드 기준으로
    // 다시 굽고 프리팹 스테이지에서 여는 것.
    [MenuItem("Border/UI/Debug/Rebake Design Stage Tester UI")]
    public static void Rebake()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            throw new System.InvalidOperationException(
                "Tester scene is missing; run Border/Simulation/Create Design Stage Tester first.");
        OpenAndBake();
        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
    }

    private static void OpenAndBake()
    {
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

            // 재굽기일 때 지난 베이크가 남아 있으면 중복된다.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == TesterName || root.name == TesterUiName) Object.DestroyImmediate(root);

            bool hasEventSystem = false;
            foreach (GameObject root in scene.GetRootGameObjects())
                hasEventSystem |= root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null;
            if (!hasEventSystem)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", true));
            new GameObject(TesterName).AddComponent<DesignStageTester>().Configure(rocket, builder, library);
            var host = new GameObject(TesterUiName);
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
