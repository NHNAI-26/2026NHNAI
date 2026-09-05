using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
internal readonly struct UberShaderVariantSpec
{
    internal readonly string ShaderName;
    internal readonly PassType PassType;
    internal readonly string[] Keywords;
    internal UberShaderVariantSpec(string shaderName, PassType passType,
        params string[] keywords)
    {
        ShaderName = shaderName;
        PassType = passType;
        Keywords = keywords ?? Array.Empty<string>();
    }
    internal bool RequiresUncheckedConstruction =>
        ShaderName == UberShaderVariantManifest.ParticleShaderName &&
        PassType == PassType.ScriptableRenderPipeline &&
        Keywords.Length == 3 && Keywords[0] == "_CUSTOM_DATA_ON" &&
        Keywords[1] == "_UV_DISTORTION_ON" &&
        Keywords[2] == "_VERTEX_OFFSET_ON";
    internal ShaderVariantCollection.ShaderVariant ToVariant()
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
            throw new InvalidOperationException("Shader not found: " + ShaderName);
        if (RequiresUncheckedConstruction)
        {
            return new ShaderVariantCollection.ShaderVariant
            {
                shader = shader,
                passType = PassType,
                keywords = Keywords,
            };
        }
        return new ShaderVariantCollection.ShaderVariant(shader, PassType, Keywords);
    }
}

internal static class UberShaderVariantManifest
{
    internal const string CollectionPath =
        "Assets/05. Arts/Shader/Uber/UberShaderVariants.shadervariants";
    internal const string CollectionGuid = "cbe808f5d2e24a9285468e3acd57e39f";
    internal const string CollectionName = "UberShaderVariants";
    internal const string PostShaderName = "Shader/Uber/Post Processing";
    internal const string ObjectShaderName = "Shader/Uber/3D Object";
    internal const string SpriteShaderName = "Shader/Uber/2D Sprite";
    internal const string UIShaderName = "Shader/Uber/UI";
    internal const string ParticleShaderName = "Shader/Uber/Particle";
    private static readonly UberShaderVariantSpec[] ManifestRows =
    {
        new UberShaderVariantSpec(PostShaderName, PassType.Normal),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_ASCII_FILTER_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_COLOR_ADJUST_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_COLOR_QUANTIZATION_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_COLOR_SCREEN_BLEND_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_CRT_FILTER_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_EDGE_FILTER_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_GRADIENT_MAP_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_OLD_FILM_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_ORDERED_DITHER_ON"),
        new UberShaderVariantSpec(PostShaderName, PassType.Normal, "_PIXELATION_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_BASE_MAP_TRIPLANAR"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_BASE_MAP_TRIPLANAR", "_TEXTURE_BLEND_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_COLOR_ADJUST_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_COLOR_ADJUST_ON", "_EMISSION", "_HEIGHT_FADE_ON", "_RIM_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_OBJECT_SPACE", "_DISSOLVE_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON", "_EMISSION", "_RIM_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_DITHER_FADE_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_EMISSION"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_GLASS_GLOW_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_GLITCH_OBJECT_SPACE", "_GLITCH_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_GLITCH_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_GLITCH_ON", "_GLITCH_WORLD_SPACE"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_GLITCH_ON", "_HOLOGRAM_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_HEIGHT_FADE_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_HOLOGRAM_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_HOLOGRAM_ON", "_HOLOGRAM_SCREEN_SPACE"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_HOLOGRAM_ON", "_HOLOGRAM_WORLD_SPACE"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_METALLICMAP"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_METALLICMAP", "_ROUGHNESSMAP"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_RIM_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_ROUGHNESSMAP"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipeline, "_TEXTURE_BLEND_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_DITHER_FADE_ON", "_STENCIL_OUTLINE_ON"),
        new UberShaderVariantSpec(ObjectShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_STENCIL_OUTLINE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_COLOR_ADJUST_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_COLOR_ADJUST_ON", "_EMISSION", "_PIXEL_OUTLINE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_COLOR_ADJUST_ON", "_EMISSION", "_RIM_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_EDGE_GRADIENT", "_DISSOLVE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON", "_DISSOLVE_RADIAL"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON", "_DISSOLVE_SWIPE"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON", "_SECONDARY_LAYER_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_DITHER_FADE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_DITHER_FADE_ON", "_UV_FADE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_EMISSION"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_EMISSION", "_GRAYSCALE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_EMISSION", "_GRAYSCALE_ON", "_TINT_MASK_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_GLITCH_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_GLITCH_ON", "_HOLOGRAM_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_GRAYSCALE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_HOLOGRAM_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_HOLOGRAM_ON", "_HOLOGRAM_SCREEN_SPACE"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_HOLOGRAM_ON", "_HOLOGRAM_WORLD_SPACE"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_LIGHT_SWEEP_MULTIPLY", "_LIGHT_SWEEP_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_LIGHT_SWEEP_MULTIPLY", "_LIGHT_SWEEP_ON", "_LIGHT_SWEEP_SHARP"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_LIGHT_SWEEP_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_LIGHT_SWEEP_ON", "_LIGHT_SWEEP_SHARP"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_PIXEL_OUTLINE_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_RIM_MULTIPLY", "_RIM_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_RIM_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_SECONDARY_LAYER_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_TINT_MASK_ON"),
        new UberShaderVariantSpec(SpriteShaderName, PassType.ScriptableRenderPipeline, "_UV_FADE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_COLOR_ADJUST_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_COLOR_ADJUST_ON", "_RGB_OVERRIDE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_DISSOLVE_EDGE_GRADIENT", "_DISSOLVE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_DISSOLVE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_DISSOLVE_ON", "_DISSOLVE_RADIAL"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_DISSOLVE_ON", "_DISSOLVE_SWIPE"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_DISSOLVE_ON", "_DITHER_FADE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_DITHER_FADE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_EMISSION"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_EMISSION", "_GRAYSCALE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_EMISSION", "_GRAYSCALE_ON", "_TINT_MASK_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_GLITCH_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_GLITCH_ON", "_HOLOGRAM_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_GRAYSCALE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_HOLOGRAM_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_HOLOGRAM_ON", "_HOLOGRAM_SCREEN_SPACE"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_HOLOGRAM_ON", "_HOLOGRAM_WORLD_SPACE"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_LIGHT_SWEEP_MULTIPLY", "_LIGHT_SWEEP_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_LIGHT_SWEEP_MULTIPLY", "_LIGHT_SWEEP_ON", "_LIGHT_SWEEP_SHARP"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_LIGHT_SWEEP_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_LIGHT_SWEEP_ON", "_LIGHT_SWEEP_SHARP"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_PIXEL_OUTLINE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_PIXEL_OUTLINE_ON", "_UV_FADE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_RGB_OVERRIDE_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_TINT_MASK_ON"),
        new UberShaderVariantSpec(UIShaderName, PassType.ScriptableRenderPipelineDefaultUnlit, "_UV_FADE_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_COLOR_ADJUST_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_CUSTOM_DATA_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_CUSTOM_DATA_ON", "_DISSOLVE_ON", "_EMISSION", "_UV_DISTORTION_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_CUSTOM_DATA_ON", "_UV_DISTORTION_ON", "_VERTEX_OFFSET_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON", "_DISSOLVE_RADIAL"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON", "_DISSOLVE_SWIPE"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_DISSOLVE_ON", "_EMISSION", "_RIM_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_EMISSION"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_FADING_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_FLIPBOOKBLENDING_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_FLIPBOOKBLENDING_ON", "_MASK_ON", "_UV_DISTORTION_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_MASK_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_RIM_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_SOFTPARTICLES_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_UV_DISTORTION_ON"),
        new UberShaderVariantSpec(ParticleShaderName, PassType.ScriptableRenderPipeline, "_VERTEX_OFFSET_ON"),
    };

    private static readonly IReadOnlyList<UberShaderVariantSpec> ReadOnlyRows = Array.AsReadOnly(ManifestRows);
    internal static IReadOnlyList<UberShaderVariantSpec> Rows => ReadOnlyRows;
    internal static void ValidateRows(IReadOnlyList<UberShaderVariantSpec> rows)
    {
        if (rows == null || rows.Count != 112)
            throw new InvalidOperationException("The Uber variant manifest must contain 112 rows.");
        var uniqueRows = new HashSet<string>(StringComparer.Ordinal);
        int uncheckedCount = 0;
        for (int index = 0; index < rows.Count; ++index)
        {
            UberShaderVariantSpec row = rows[index];
            if (row.ShaderName != ExpectedShader(index) ||
                row.PassType != ExpectedPass(index))
                throw new InvalidOperationException("Variant group/order mismatch at " + index + ".");
            for (int keyword = 0; keyword < row.Keywords.Length; ++keyword)
            {
                if (keyword > 0 && string.CompareOrdinal(row.Keywords[keyword - 1],
                        row.Keywords[keyword]) >= 0)
                    throw new InvalidOperationException("Keywords must be unique and ordered at " + index + ".");
                string master = RequiredMaster(row.Keywords[keyword]);
                if (master != null && !HasKeyword(row, master))
                    throw new InvalidOperationException(row.Keywords[keyword] + " requires " + master + ".");
            }
            if (row.ShaderName == PostShaderName && row.Keywords.Length > 1)
                throw new InvalidOperationException("Post variants may select only one filter.");
            if (HasKeyword(row, "_GLITCH_OBJECT_SPACE") &&
                    HasKeyword(row, "_GLITCH_WORLD_SPACE") ||
                HasKeyword(row, "_DISSOLVE_RADIAL") &&
                    HasKeyword(row, "_DISSOLVE_SWIPE") ||
                HasKeyword(row, "_HOLOGRAM_SCREEN_SPACE") &&
                    HasKeyword(row, "_HOLOGRAM_WORLD_SPACE"))
                throw new InvalidOperationException("Variant modes from one selector are mutually exclusive.");
            string key = row.ShaderName + "\n" + (int)row.PassType + "\n" +
                string.Join(" ", row.Keywords);
            if (!uniqueRows.Add(key))
                throw new InvalidOperationException("Duplicate variant row at " + index + ".");
            if (row.RequiresUncheckedConstruction)
                ++uncheckedCount;
        }
        if (uncheckedCount != 1)
            throw new InvalidOperationException("Exactly one Particle row requires unchecked construction.");
    }
    private static string ExpectedShader(int index) => index < 11
        ? PostShaderName : index < 37 ? ObjectShaderName : index < 67
            ? SpriteShaderName : index < 94 ? UIShaderName : ParticleShaderName;
    private static PassType ExpectedPass(int index)
    {
        if (index < 11) return PassType.Normal;
        if (index < 35 || index >= 37 && index < 67 || index >= 94)
            return PassType.ScriptableRenderPipeline;
        return PassType.ScriptableRenderPipelineDefaultUnlit;
    }
    private static string RequiredMaster(string keyword)
    {
        switch (keyword)
        {
            case "_GLITCH_OBJECT_SPACE": case "_GLITCH_WORLD_SPACE": return "_GLITCH_ON";
            case "_DISSOLVE_OBJECT_SPACE": case "_DISSOLVE_RADIAL":
            case "_DISSOLVE_SWIPE": case "_DISSOLVE_EDGE_GRADIENT": return "_DISSOLVE_ON";
            case "_LIGHT_SWEEP_SHARP": case "_LIGHT_SWEEP_MULTIPLY": return "_LIGHT_SWEEP_ON";
            case "_RIM_MULTIPLY": case "_RIM_RADIAL_UV": return "_RIM_ON";
            case "_HOLOGRAM_WORLD_SPACE": case "_HOLOGRAM_SCREEN_SPACE": return "_HOLOGRAM_ON";
            default: return null;
        }
    }
    private static bool HasKeyword(UberShaderVariantSpec row, string keyword) =>
        Array.IndexOf(row.Keywords, keyword) >= 0;
}
