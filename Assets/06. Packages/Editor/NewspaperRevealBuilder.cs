using Border.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NewspaperRevealBuilder
{
    private const string PrefabPath = "Assets/03. Prefabs/UI/NewspaperReveal.prefab";

    [MenuItem("Border/UI/Create Newspaper Reveal")]
    public static void Create()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            throw new System.InvalidOperationException("Newspaper prefab already exists; edit it directly.");
        Scene previous = SceneManager.GetActiveScene();
        Scene main = SceneManager.GetSceneByPath("Assets/00. Scenes/01_Main.unity");
        bool opened = !main.isLoaded;
        if (opened) main = EditorSceneManager.OpenScene("Assets/00. Scenes/01_Main.unity", OpenSceneMode.Additive);
        SceneManager.SetActiveScene(main);
        try
        {
            var host = new GameObject("Newspaper Reveal");
            var effect = host.AddComponent<NewspaperReveal>();
            RectTransform view = Rect("View", host.transform, Vector2.zero, Vector2.one);
            var canvas = view.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = view.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            view.gameObject.AddComponent<GraphicRaycaster>();
            RectTransform dim = Rect("Backdrop", view, Vector2.zero, Vector2.one);
            dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            CanvasGroup backdrop = dim.gameObject.AddComponent<CanvasGroup>();
            RectTransform paper = Rect("PaperMotion", view, new Vector2(0.18f, 0.09f), new Vector2(0.82f, 0.91f));
            CanvasGroup group = paper.gameObject.AddComponent<CanvasGroup>();
            RectTransform content = Rect("Content", paper, Vector2.zero, Vector2.one);
            Image spriteImage = content.gameObject.AddComponent<Image>();
            spriteImage.preserveAspect = true;
            Button button = content.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(button.onClick, effect.Hide);
            var serialized = new SerializedObject(effect);
            serialized.FindProperty("view").objectReferenceValue = view.gameObject;
            serialized.FindProperty("paper").objectReferenceValue = paper;
            serialized.FindProperty("contentGroup").objectReferenceValue = group;
            serialized.FindProperty("backdrop").objectReferenceValue = backdrop;
            serialized.FindProperty("newspaperImage").objectReferenceValue = spriteImage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(host, PrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(main);
            Selection.activeGameObject = host;
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            if (opened) EditorSceneManager.CloseScene(main, true);
        }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

}
