using Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RocketExplosionBuilder
{
    private const string Folder = "Assets/03. Prefabs/Simulation/Explosion";

    [InitializeOnLoadMethod]
    private static void ScheduleInstall()
    {
        EditorApplication.delayCall += InstallIfMissing;
    }

    private static void InstallIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= InstallAfterPlay;
            EditorApplication.playModeStateChanged += InstallAfterPlay;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<ParticleSystem>(Folder + "/RocketExplosion.prefab") == null)
            Install();
    }

    private static void InstallAfterPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= InstallAfterPlay;
        EditorApplication.delayCall += InstallIfMissing;
    }

    [MenuItem("Border/Simulation/Install Rocket Explosion")]
    public static void Install()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/03. Prefabs/Simulation", "Explosion");
        ParticleSystem prefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(Folder + "/RocketExplosion.prefab");
        if (prefab == null) prefab = BuildPrefab();
        Scene previous = SceneManager.GetActiveScene();
        foreach (string name in new[] { "SimulationTest", "DesignStageTester" })
        {
            string path = "Assets/00. Scenes/" + name + ".unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Rocket rocket in root.GetComponentsInChildren<Rocket>(true))
                    {
                        var serialized = new SerializedObject(rocket);
                        serialized.FindProperty("explosionPrefab").objectReferenceValue = prefab;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        SceneManager.SetActiveScene(previous);
    }

    private static ParticleSystem BuildPrefab()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new System.InvalidOperationException("URP particle shader missing.");
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.name = "ExplosionSoftParticle";
        var pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - radius), 1.5f));
            }
        texture.SetPixels(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, Folder + "/ExplosionSoftParticle.asset");
        var material = new Material(shader) { name = "RocketExplosion", renderQueue = 3000 };
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Surface", 1);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        AssetDatabase.CreateAsset(material, Folder + "/RocketExplosion.mat");
        var host = new GameObject("RocketExplosion");
        try
        {
            var particles = host.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 100) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.45f), 0f),
                new GradientColorKey(new Color(1f, 0.22f, 0.02f), 0.25f),
                new GradientColorKey(new Color(0.13f, 0.13f, 0.13f), 0.65f)
            }, new[] {new GradientAlphaKey(1, 0), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0, 1)});
            color.color = gradient;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(host, Folder + "/RocketExplosion.prefab");
            return prefab.GetComponent<ParticleSystem>();
        }
        finally { Object.DestroyImmediate(host); }
    }
}
