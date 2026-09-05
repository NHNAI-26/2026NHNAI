using Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RocketExplosionBuilder
{
    private const string Folder = "Assets/03. Prefabs/Simulation/Explosion";
    private const string SourceFolder = "Assets/05. Arts/VFX/BreakShootExplosion";
    private const float RocketScale = 1.5f;
    public const string PrefabPath = Folder + "/RocketExplosion.prefab";

    [InitializeOnLoadMethod]
    private static void ScheduleInstall() => EditorApplication.delayCall += InstallIfMissing;

    private static void InstallIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= InstallAfterPlay;
            EditorApplication.playModeStateChanged += InstallAfterPlay;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<ParticleSystem>(PrefabPath) == null) Install();
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
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Exit Play Mode first.");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/03. Prefabs/Simulation", "Explosion");
        ParticleSystem prefab = BuildPrefab();
        Scene previous = SceneManager.GetActiveScene();
        try
        {
            foreach (string name in new[] { "SimulationTest", "DesignStageTester" })
            {
                string path = "Assets/00. Scenes/" + name + ".unity";
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    bool changed = false;
                    foreach (GameObject root in scene.GetRootGameObjects())
                        foreach (Rocket rocket in root.GetComponentsInChildren<Rocket>(true))
                        {
                            var serialized = new SerializedObject(rocket);
                            var reference = serialized.FindProperty("explosionPrefab");
                            if (reference.objectReferenceValue == prefab) continue;
                            reference.objectReferenceValue = prefab;
                            serialized.ApplyModifiedPropertiesWithoutUndo();
                            changed = true;
                        }
                    // Loaded scenes may contain unrelated unsaved work.
                    if (changed && opened) EditorSceneManager.SaveScene(scene);
                    else if (changed) EditorSceneManager.MarkSceneDirty(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { SceneManager.SetActiveScene(previous); }
    }

    private static ParticleSystem BuildPrefab()
    {
        var profileAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(SourceFolder + "/ExplosionProfile.json");
        if (profileAsset == null) throw new System.InvalidOperationException("Break Shoot explosion profile is missing.");
        var profile = JsonUtility.FromJson<ExplosionProfile>(profileAsset.text);
        bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        GameObject host = existing ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("RocketExplosion");
        try
        {
            // Preserve the root component ID already referenced by the launch scenes.
            var root = host.GetComponent<ParticleSystem>() ?? host.AddComponent<ParticleSystem>();
            root.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = host.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(host.transform.GetChild(i).gameObject);
            host.transform.localScale = Vector3.one;
            foreach (SourceSystem source in profile.systems)
            {
                ParticleSystem particles = root;
                if (source.name != "Fire")
                {
                    var child = new GameObject(source.name);
                    child.transform.SetParent(host.transform, false);
                    particles = child.AddComponent<ParticleSystem>();
                }
                Configure(particles, source);
            }
            var smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/SmokeBurst.prefab");
            if (smokePrefab == null)
                throw new System.InvalidOperationException("Explosion smoke burst prefab is missing.");
            var smokeBurst = (GameObject)PrefabUtility.InstantiatePrefab(smokePrefab, host.scene);
            smokeBurst.transform.SetParent(host.transform, false);
            var smokeExplosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/05. Arts/Effect/Smoke/Smoke_Explosion.prefab");
            if (smokeExplosionPrefab == null)
                throw new System.InvalidOperationException("Smoke_Explosion prefab is missing.");
            var smokeExplosion = (GameObject)PrefabUtility.InstantiatePrefab(smokeExplosionPrefab, host.scene);
            smokeExplosion.transform.SetParent(host.transform, false);
            smokeExplosion.transform.localPosition = Vector3.zero;
            foreach (var particles in smokeExplosion.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                main.playOnAwake = false;
                main.useUnscaledTime = true;
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(host, PrefabPath);
            return prefab.GetComponent<ParticleSystem>();
        }
        finally
        {
            if (existing) PrefabUtility.UnloadPrefabContents(host);
            else Object.DestroyImmediate(host);
        }
    }

    private static void Configure(ParticleSystem particles, SourceSystem source)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = 2.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(source.lifetimeMin, source.lifetimeMax);
        main.startSpeed = source.name == "Sparks" ? new ParticleSystem.MinMaxCurve(3f, 12f) : 0f;
        main.startSize3D = false;
        main.startSize = source.size * RocketScale;
        main.startRotation3D = false;
        main.startRotation = source.name == "Smoke" ? 0f : new ParticleSystem.MinMaxCurve(0f, Mathf.PI);
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.maxParticles = source.count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.useUnscaledTime = true;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)source.count) });
        var shape = particles.shape;
        shape.enabled = source.name == "Sparks";
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = false;
        var limit = particles.limitVelocityOverLifetime;
        limit.enabled = false;
        var sheet = particles.textureSheetAnimation;
        sheet.enabled = false;
        var rotation = particles.rotationOverLifetime;
        rotation.enabled = source.name == "Flash";
        rotation.z = 0.5f;
        var noise = particles.noise;
        noise.enabled = source.name == "Sparks";
        noise.strength = 0.8f;
        noise.frequency = 1f;
        noise.scrollSpeed = 1f;
        noise.quality = ParticleSystemNoiseQuality.Low;

        var keys = new Keyframe[source.frames.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            SourceFrame key = source.frames[i];
            keys[i] = new Keyframe(key.time, key.value, key.inTangent, key.outTangent);
        }
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));

        // Particle vertex colors have a limited range; keep the source HDR gain in the material.
        float gain = 1f;
        foreach (SourceColorKey key in source.colorKeys)
            gain = Mathf.Max(gain, key.color.r, key.color.g, key.color.b);
        var colors = new GradientColorKey[source.colorKeys.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            Color color = source.colorKeys[i].color;
            colors[i] = new GradientColorKey(new Color(color.r / gain, color.g / gain, color.b / gain), source.colorKeys[i].time);
        }
        var alphas = new GradientAlphaKey[source.alphaKeys.Length];
        for (int i = 0; i < alphas.Length; i++)
            alphas[i] = new GradientAlphaKey(source.alphaKeys[i].alpha, source.alphaKeys[i].time);
        var gradient = new Gradient();
        gradient.SetKeys(colors, alphas);
        var colorModule = particles.colorOverLifetime;
        colorModule.enabled = true;
        colorModule.color = gradient;

        if (source.name == "Imprint")
        {
            // 잔상은 폭발과 동시에 보이고 짧게 사라진다. 원본의 느린 페이드인은 사용하지 않는다.
            main.duration = 0.4f;
            main.startLifetime = 0.4f;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.04f));
            gradient.SetKeys(colors, new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.65f, 0.15f),
                new GradientAlphaKey(0f, 1f),
            });
            colorModule.color = gradient;
        }

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = BuildMaterial(source, gain);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        // The original top-down graph separated these layers along camera depth.
        renderer.sortingFudge = source.name == "Imprint" ? 2f : source.name == "Smoke" ? 1f : -1f;
        renderer.maxParticleSize = 1f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Material BuildMaterial(SourceSystem source, float gain)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceFolder + "/" + source.texture);
        if (texture == null) throw new System.InvalidOperationException("Missing Break Shoot texture: " + source.texture);
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new System.InvalidOperationException("URP particle shader missing.");
        string name = source.name == "Fire" ? "RocketExplosion" : "BreakShoot" + source.name;
        string path = Folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", new Color(gain, gain, gain, 1f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetFloat("_ColorMode", 0f);
        material.SetFloat("_FlipbookBlending", 0f);
        material.renderQueue = 3000;
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetShaderPassEnabled("ShadowCaster", false);
        material.SetShaderPassEnabled("DepthOnly", false);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    [System.Serializable]
    private sealed class ExplosionProfile { public SourceSystem[] systems = null; }

    [System.Serializable]
    private sealed class SourceSystem
    {
        public string name = null;
        public string texture = null;
        public int count = 0;
        public float lifetimeMin = 0f;
        public float lifetimeMax = 0f;
        public float size = 0f;
        public SourceFrame[] frames = null;
        public SourceColorKey[] colorKeys = null;
        public SourceAlphaKey[] alphaKeys = null;
    }

    [System.Serializable]
    private sealed class SourceFrame { public float time = 0f, value = 0f, inTangent = 0f, outTangent = 0f; }

    [System.Serializable]
    private sealed class SourceColorKey { public Color color = Color.white; public float time = 0f; }

    [System.Serializable]
    private sealed class SourceAlphaKey { public float alpha = 0f, time = 0f; }
}
