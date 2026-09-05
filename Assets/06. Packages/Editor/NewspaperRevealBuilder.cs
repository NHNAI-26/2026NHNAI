using Border.UI;
using Border.Research;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NewspaperRevealBuilder
{
    private const string NewspaperPrefabPath = "Assets/03. Prefabs/UI/NewspaperReveal.prefab";
    private const string MailPrefabPath = "Assets/03. Prefabs/UI/MailReveal.prefab";

    [MenuItem("Border/UI/Apply Launch Newspaper")]
    public static void ApplyLaunchNewspaper()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play Mode first.");
        const string reportPath = "Assets/03. Prefabs/UI/ResearchResultReport.prefab";
        const string spritePath = "Assets/05. Arts/UI/Newspaper/newspaper-original-transparent.png";
        const string mailSpritePath = "Assets/05. Arts/UI/Email/Email.png";
        Sprite sprite = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(spritePath))
            if (asset is Sprite candidate) { sprite = candidate; break; }
        if (sprite == null) throw new System.InvalidOperationException("Newspaper sprite is not imported.");
        Sprite mailSprite = AssetDatabase.LoadAssetAtPath<Sprite>(mailSpritePath);
        if (mailSprite == null) throw new System.InvalidOperationException("Mail sprite is not imported.");
        GameObject reportAsset = AssetDatabase.LoadAssetAtPath<GameObject>(reportPath);
        TMP_FontAsset font = reportAsset.GetComponentInChildren<TMP_Text>(true)?.font ?? TMP_Settings.defaultFontAsset;
        GameObject newspaperRoot = PrefabUtility.LoadPrefabContents(NewspaperPrefabPath);
        try
        {
            var reveal = newspaperRoot.GetComponent<NewspaperReveal>();
            var serialized = new SerializedObject(reveal);
            // The saved newspaper prefab is the source of truth for its original appearance.
            // Only migrate the sprite binding; keep all layout, material and animation settings.
            serialized.FindProperty("medium").enumValueIndex = (int)LaunchResultMedium.Newspaper;
            serialized.FindProperty("presentationSprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            reveal.SetSprite(sprite);
            PrefabUtility.SaveAsPrefabAsset(newspaperRoot, NewspaperPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(newspaperRoot); }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(MailPrefabPath) == null
            && !AssetDatabase.CopyAsset(NewspaperPrefabPath, MailPrefabPath))
        {
            throw new System.InvalidOperationException("Mail prefab could not be created.");
        }

        GameObject mailRoot = PrefabUtility.LoadPrefabContents(MailPrefabPath);
        try
        {
            mailRoot.name = "MailReveal";
            var reveal = mailRoot.GetComponent<NewspaperReveal>();
            var serialized = new SerializedObject(reveal);
            var view = (GameObject)serialized.FindProperty("view").objectReferenceValue;
            view.GetComponent<Canvas>().sortingOrder = 30;
            var paper = (RectTransform)serialized.FindProperty("paper").objectReferenceValue;
            paper.anchorMin = new Vector2(0.04f, 0.04f);
            paper.anchorMax = new Vector2(0.96f, 0.96f);
            paper.offsetMin = paper.offsetMax = Vector2.zero;
            var content = (RectTransform)paper.Find("Content");
            var fitter = content.GetComponent<AspectRatioFitter>() ?? content.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = mailSprite.rect.width / mailSprite.rect.height;
            serialized.FindProperty("medium").enumValueIndex = (int)LaunchResultMedium.Mail;
            serialized.FindProperty("presentationSprite").objectReferenceValue = mailSprite;
            serialized.FindProperty("showEvent").objectReferenceValue = null;

            TMP_Text heading = PaperText("Headline", content, font, mailSprite, 430, 318, 760, 64, 20, 16);
            heading.fontStyle = FontStyles.Bold;
            heading.alignment = TextAlignmentOptions.Left;
            TMP_Text edition = PaperText("Edition", content, font, mailSprite, 430, 392, 760, 34, 13, 11);
            edition.alignment = TextAlignmentOptions.Left;
            TMP_Text body = PaperText("Body", content, font, mailSprite, 430, 448, 760, 240, 17, 13);
            body.alignment = TextAlignmentOptions.TopLeft;
            TMP_Text effects = PaperText("Effects", content, font, mailSprite, 770, 735, 420, 190, 15, 12);
            effects.alignment = TextAlignmentOptions.TopLeft;
            var effectsBackground = EnsureRect("EffectsBackground", content);
            PlaceOnPaper(effectsBackground, mailSprite, 750, 715, 460, 230);
            var background = effectsBackground.GetComponent<Image>() ?? effectsBackground.gameObject.AddComponent<Image>();
            background.color = new Color(0.92f, 0.96f, 1f, 0.92f);
            background.raycastTarget = false;
            effectsBackground.SetSiblingIndex(effects.transform.GetSiblingIndex());
            var photoRect = EnsureRect("Photo", content);
            PlaceOnPaper(photoRect, mailSprite, 430, 715, 300, 225);
            var photo = photoRect.GetComponent<RawImage>() ?? photoRect.gameObject.AddComponent<RawImage>();
            photo.material = null;
            photo.raycastTarget = false;
            var fallback = PaperText("PhotoFallback", content, font, mailSprite, 430, 715, 300, 225, 15, 12);
            fallback.alignment = TextAlignmentOptions.Center;
            fallback.text = "현장 사진 수신 실패";
            serialized.FindProperty("headlineText").objectReferenceValue = heading;
            serialized.FindProperty("editionText").objectReferenceValue = edition;
            serialized.FindProperty("articleText").objectReferenceValue = body;
            serialized.FindProperty("effectsText").objectReferenceValue = effects;
            serialized.FindProperty("effectsBackground").objectReferenceValue = effectsBackground;
            serialized.FindProperty("photoImage").objectReferenceValue = photo;
            serialized.FindProperty("photoFallbackText").objectReferenceValue = fallback;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            reveal.SetSprite(mailSprite);
            photo.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(mailRoot, MailPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(mailRoot); }

        GameObject report = PrefabUtility.LoadPrefabContents(reportPath);
        try
        {
            // Keep the report root/GUID so all existing scene result routes remain connected.
            for (int i = report.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(report.transform.GetChild(i).gameObject);
            var paperInstance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NewspaperPrefabPath), report.transform);
            var mailInstance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MailPrefabPath), report.transform);
            mailInstance.SetActive(false);
            var adapter = new SerializedObject(report.GetComponent<ResearchResultReportController>());
            adapter.FindProperty("newspaper").objectReferenceValue = paperInstance.GetComponent<NewspaperReveal>();
            adapter.FindProperty("mail").objectReferenceValue = mailInstance.GetComponent<NewspaperReveal>();
            adapter.ApplyModifiedPropertiesWithoutUndo();
            report.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(report, reportPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(report); }
        AssetDatabase.SaveAssets();
    }

    public static Material GetPhotoMaterial()
    {
        const string path = "Assets/05. Arts/Material/NewspaperPhoto.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Border/UI/NewspaperPhoto");
        if (shader == null) throw new System.InvalidOperationException("Newspaper photo shader is not imported.");
        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static RectTransform EnsureRect(string name, Transform parent)
    {
        return parent.Find(name) as RectTransform ?? Rect(name, parent, Vector2.zero, Vector2.one);
    }

    private static void PlaceOnPaper(RectTransform rect, Sprite sprite, float x, float y, float width, float height)
    {
        Rect crop = sprite.rect;
        float top = sprite.texture.height - crop.y;
        rect.anchorMin = new Vector2((x - crop.x) / crop.width, (top - y - height) / crop.height);
        rect.anchorMax = new Vector2((x + width - crop.x) / crop.width, (top - y) / crop.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static TMP_Text PaperText(string name, Transform parent, TMP_FontAsset font, Sprite sprite,
        float x, float y, float width, float height, float fontSize, float minimum)
    {
        var rect = EnsureRect(name, parent);
        PlaceOnPaper(rect, sprite, x, y, width, height);
        var text = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.color = new Color(0.08f, 0.075f, 0.065f, 1f);
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = minimum;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        text.richText = false;
        return text;
    }

    [MenuItem("Border/UI/Create Newspaper Reveal")]
    public static void Create()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(NewspaperPrefabPath) != null)
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
            PrefabUtility.SaveAsPrefabAssetAndConnect(host, NewspaperPrefabPath, InteractionMode.AutomatedAction);
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
