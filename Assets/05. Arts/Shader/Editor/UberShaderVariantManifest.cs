using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

internal readonly struct UberShaderVariantSpec
{
    internal readonly string ShaderName;
    internal readonly PassType PassType;
    internal readonly string[] Keywords;
    internal UberShaderVariantSpec(string shaderName, PassType passType, params string[] keywords)
    {
        ShaderName = shaderName;
        PassType = passType;
        Keywords = keywords ?? Array.Empty<string>();
    }
    internal ShaderVariantCollection.ShaderVariant ToVariant()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null) throw new InvalidOperationException("Shader not found: " + ShaderName);
        // Unity's constructor checks stages separately; Wobble is vertex-only while probe keywords are fragment-only.
        if (ShaderName == UberShaderVariantManifest.ObjectShaderName &&
            Keywords.Contains("_WOBBLE_ON") && Keywords.Contains("_CLUSTER_LIGHT_LOOP"))
            return new ShaderVariantCollection.ShaderVariant { shader = shader, passType = PassType, keywords = Keywords };
        return new ShaderVariantCollection.ShaderVariant(shader, PassType, Keywords);
    }
}

internal static class UberShaderVariantManifest
{
    internal const string CollectionPath = "Assets/05. Arts/Shader/Uber/UberShaderVariants.shadervariants";
    internal const string CollectionGuid = "cbe808f5d2e24a9285468e3acd57e39f";
    internal const string CollectionName = "UberShaderVariants";
    internal const string PostShaderName = "Shader/Uber/Post Processing";
    internal const string ObjectShaderName = "Shader/Uber/3D Object";
    internal const string SpriteShaderName = "Shader/Uber/2D Sprite";
    internal const string UIShaderName = "Shader/Uber/UI";

    // Authored materials plus the tested RocketPart, preview, target and Wobble states.
    private static readonly string[][] ObjectMaterialKeywords =
    {
        Array.Empty<string>(),
        new[] { "_ALPHAPREMULTIPLY_ON", "_HOLOGRAM_ON", "_HOLOGRAM_WORLD_SPACE", "_NORMALMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_HOLOGRAM_ON", "_METALLICMAP", "_NORMALMAP", "_RIM_ON", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_HOLOGRAM_ON", "_METALLICMAP", "_NORMALMAP", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_HOLOGRAM_ON", "_NORMALMAP", "_RIM_ON", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_HOLOGRAM_ON", "_NORMALMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_METALLICMAP", "_NORMALMAP", "_RIM_ON", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_METALLICMAP", "_NORMALMAP", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_NORMALMAP", "_RIM_ON", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHAPREMULTIPLY_ON", "_NORMALMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_ALPHATEST_ON" },
        new[] { "_BASE_MAP_TRIPLANAR", "_COLOR_ADJUST_ON", "_TEXTURE_BLEND_ON" },
        new[] { "_COLOR_ADJUST_ON", "_ROUGHNESSMAP" },
        new[] { "_EMISSION", "_HOLOGRAM_ON", "_SURFACE_TYPE_TRANSPARENT", "_UNLIT_ON" },
        new[] { "_HEIGHT_FADE_ON" },
        new[] { "_HOLOGRAM_ON", "_HOLOGRAM_WORLD_SPACE", "_NORMALMAP", "_RIM_ON", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_HOLOGRAM_ON", "_HOLOGRAM_WORLD_SPACE", "_NORMALMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_HOLOGRAM_ON", "_METALLICMAP", "_NORMALMAP", "_RIM_ON", "_ROUGHNESSMAP" },
        new[] { "_HOLOGRAM_ON", "_METALLICMAP", "_NORMALMAP", "_RIM_ON", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_HOLOGRAM_ON", "_METALLICMAP", "_NORMALMAP", "_ROUGHNESSMAP" },
        new[] { "_HOLOGRAM_ON", "_METALLICMAP", "_NORMALMAP", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_HOLOGRAM_ON", "_SURFACE_TYPE_TRANSPARENT", "_UNLIT_ON" },
        new[] { "_HOLOGRAM_WORLD_SPACE", "_NORMALMAP", "_RIM_ON", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_HOLOGRAM_WORLD_SPACE", "_NORMALMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_METALLICMAP", "_NORMALMAP", "_RIM_ON", "_ROUGHNESSMAP" },
        new[] { "_METALLICMAP", "_NORMALMAP", "_RIM_ON", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_METALLICMAP", "_NORMALMAP", "_ROUGHNESSMAP" },
        new[] { "_METALLICMAP", "_NORMALMAP", "_ROUGHNESSMAP", "_SURFACE_TYPE_TRANSPARENT" },
        new[] { "_UNLIT_ON" },
        new[] { "_WOBBLE_ON" },
    };

    // PC_RPAsset uses Forward+, reflection probes and four shadow cascades.
    // Keep the no-shadow, hard-shadow and configured high-quality soft-shadow paths.
    private static readonly string[][] ObjectLightingKeywords =
    {
        Array.Empty<string>(),
        new[] { "_CLUSTER_LIGHT_LOOP", "_REFLECTION_PROBE_ATLAS",
            "_REFLECTION_PROBE_BLENDING", "_REFLECTION_PROBE_BOX_PROJECTION" },
        new[] { "_CLUSTER_LIGHT_LOOP", "_MAIN_LIGHT_SHADOWS_CASCADE",
            "_REFLECTION_PROBE_ATLAS", "_REFLECTION_PROBE_BLENDING", "_REFLECTION_PROBE_BOX_PROJECTION" },
        new[] { "_CLUSTER_LIGHT_LOOP", "_MAIN_LIGHT_SHADOWS_CASCADE", "_REFLECTION_PROBE_ATLAS",
            "_REFLECTION_PROBE_BLENDING", "_REFLECTION_PROBE_BOX_PROJECTION", "_SHADOWS_SOFT" },
        new[] { "_ADDITIONAL_LIGHT_SHADOWS", "_CLUSTER_LIGHT_LOOP",
            "_MAIN_LIGHT_SHADOWS_CASCADE", "_REFLECTION_PROBE_ATLAS", "_REFLECTION_PROBE_BLENDING",
            "_REFLECTION_PROBE_BOX_PROJECTION", "_SHADOWS_SOFT" },
    };

    private static readonly IReadOnlyList<UberShaderVariantSpec> ReadOnlyRows = CreateRows();
    internal static IReadOnlyList<UberShaderVariantSpec> Rows => ReadOnlyRows;

    private static IReadOnlyList<UberShaderVariantSpec> CreateRows()
    {
        var rows = new List<UberShaderVariantSpec>
        {
            new UberShaderVariantSpec(PostShaderName, PassType.Normal),
            new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_CRT_FILTER_ON"),
        };
        var objects = new List<UberShaderVariantSpec>();
        foreach (string[] material in ObjectMaterialKeywords)
        foreach (string[] lighting in ObjectLightingKeywords)
        {
            // URP strips the entire forward pass without its required renderer keywords,
            // including variants whose material disables lighting.
            string[] rendererKeywords = lighting.Length > 0
                ? new[] { "_LIGHT_LAYERS", "_SCREEN_SPACE_OCCLUSION" } : Array.Empty<string>();
            objects.Add(new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline,
                material.Concat(lighting).Concat(rendererKeywords).Distinct().OrderBy(k => k, StringComparer.Ordinal).ToArray()));
            // SkyEnvironment enables exponential-squared fog; HappyEndingSequence turns it off.
            if (lighting.Length > 0)
                objects.Add(new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline,
                    material.Concat(lighting).Concat(rendererKeywords).Concat(new[] { "FOG_EXP2" })
                        .Distinct().OrderBy(k => k, StringComparer.Ordinal).ToArray()));
        }
        rows.AddRange(objects.OrderBy(row => string.Join(" ", row.Keywords), StringComparer.Ordinal));
        var ui = new List<UberShaderVariantSpec>();
        foreach (string[] material in new[] { Array.Empty<string>(), new[] { "_DITHER_FADE_ON" },
                     new[] { "_EMISSION", "_TINT_MASK_ON" } })
        foreach (string[] clipping in new[] { Array.Empty<string>(), new[] { "UNITY_UI_CLIP_RECT" },
                     new[] { "UNITY_UI_ALPHACLIP" }, new[] { "UNITY_UI_ALPHACLIP", "UNITY_UI_CLIP_RECT" } })
            ui.Add(new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit,
                material.Concat(clipping).OrderBy(k => k, StringComparer.Ordinal).ToArray()));
        rows.AddRange(ui.OrderBy(row => string.Join(" ", row.Keywords), StringComparer.Ordinal));
        return rows.AsReadOnly();
    }

    internal static void ValidateRows(IReadOnlyList<UberShaderVariantSpec> rows)
    {
        if (rows == null || rows.Count != Rows.Count)
            throw new InvalidOperationException("The essential variant manifest row count differs.");
        var unique = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < rows.Count; ++i)
        {
            UberShaderVariantSpec row = rows[i];
            if (row.ShaderName != Rows[i].ShaderName || row.PassType != Rows[i].PassType)
                throw new InvalidOperationException("Variant group/order mismatch at " + i);
            if (!row.Keywords.SequenceEqual(row.Keywords.Distinct().OrderBy(k => k, StringComparer.Ordinal)))
                throw new InvalidOperationException("Keywords must be unique and ordered.");
            if (row.ShaderName == PostShaderName && row.Keywords.Length > 1)
                throw new InvalidOperationException("Post variants may select only one filter.");
            if (row.Keywords.Contains("_GLITCH_OBJECT_SPACE") && !row.Keywords.Contains("_GLITCH_ON"))
                throw new InvalidOperationException("Glitch space requires Glitch.");
            if (row.Keywords.Contains("_GLITCH_OBJECT_SPACE") && row.Keywords.Contains("_GLITCH_WORLD_SPACE") ||
                row.Keywords.Contains("_HOLOGRAM_SCREEN_SPACE") && row.Keywords.Contains("_HOLOGRAM_WORLD_SPACE"))
                throw new InvalidOperationException("Variant selector modes are mutually exclusive.");
            if (!unique.Add(row.ShaderName + "|" + (int)row.PassType + "|" + string.Join(" ", row.Keywords)))
                throw new InvalidOperationException("Duplicate variant row.");
        }
    }
}
