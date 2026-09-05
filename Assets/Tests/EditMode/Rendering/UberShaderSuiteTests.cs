using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Border.Rendering;
using Border.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

namespace Border.Rendering.Tests
{
    public sealed partial class UberShaderSuiteTests
    {
        private const string UberDirectory = "Assets/05. Arts/Shader/Uber/";
        private const string ObjectShaderName = "Shader/Uber/3D Object";
        private const string SpriteShaderName = "Shader/Uber/2D Sprite";
        private const string UIShaderName = "Shader/Uber/UI";
        private const string PostShaderName = "Shader/Uber/Post Processing";
        private const string ParticleShaderName = "Shader/Uber/Particle";
        private const string VariantPath = UberDirectory +
            "UberShaderVariants.shadervariants";
        private const string VariantGuid = "cbe808f5d2e24a9285468e3acd57e39f";
        private const string PostMaterialGuid = "82661273f571973967386b3832ac0161";
        private const string EditorDirectory = "Assets/05. Arts/Shader/Editor/";
        private const string MaterialDirectory = "Assets/05. Arts/Material/";
        private const string PostMaterialPath = MaterialDirectory +
            "UberPostProcessing.mat";
        private const string AsciiFontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string AsciiFontGuid = "8f586378b4e144a9851e7b34d9b748ee";
        private const string PixelifyFontPath =
            "Assets/05. Arts/Fonts/PixelifySans-VariableFont_wght SDF.asset";
        private const string PixelifyFontGuid = "ada3081128df5d947b56bd7bc721fb79";
        private const string PixelifySourcePath =
            "Assets/05. Arts/Fonts/PixelifySans-VariableFont_wght.ttf";
        private const string PixelifySourceGuid = "75833b6a4be46914989f947314ffa964";
        private const string AsciiRamp = ".,:;ij?7IodS$#@";

        private static readonly ShaderCase[] Shaders =
        {
            new ShaderCase(ObjectShaderName, UberDirectory + "Uber3D.shader"),
            new ShaderCase(SpriteShaderName, UberDirectory + "UberSprite.shader"),
            new ShaderCase(UIShaderName, UberDirectory + "UberUI.shader"),
            new ShaderCase(PostShaderName, UberDirectory + "UberPostProcessing.shader"),
            new ShaderCase(ParticleShaderName, UberDirectory + "UberParticle.shader"),
        };

        private static readonly string[,] RenamedAssets =
        {
            { UberDirectory + "UberCommon.hlsl", "af9ea29136f7b7d41a83847084a278a4" },
            { UberDirectory + "Uber3D.shader", "d03bad68e5f94df47a2c30a8822ea41c" },
            { UberDirectory + "Uber3D.hlsl", "7934119631884194e8836230cb1ec727" },
            { UberDirectory + "UberSprite.shader", "795b3814d0dfe9242829795ff0608656" },
            { UberDirectory + "UberSprite.hlsl", "515531824a0c1574c83bee633a74a174" },
            { UberDirectory + "UberUI.shader", "1aad80d3fa14854488c67ee35f470633" },
            { UberDirectory + "UberUI.hlsl", "d9ec45f3a4181da419763eba4cc2795d" },
            { UberDirectory + "UberPostProcessing.shader", "30f88fdf99949b17e1187c24eba8ed93" },
            { UberDirectory + "UberPostProcessing.hlsl", "5b0c012e0d15a36fba62f7d9a0604382" },
            { VariantPath, VariantGuid },
            { EditorDirectory + "UberShaderGUI.cs", "52aa56fcdec5cff4f984a7556df7692e" },
            { EditorDirectory + "UberShader.Editor.asmdef", "fe602cbf7ed929142a91f383260919fb" },
            { EditorDirectory + "UberShader.Editor.AssemblyInfo.cs", "378b33dceb8f42cabf343e70ddf605c7" },
            { EditorDirectory + "UberShaderVariantManifest.cs", "131d6333c09946cd9d62ab89cf7fca89" },
            { EditorDirectory + "UberShaderVariantCollectionGenerator.cs", "b56e4858d11649beb320636ebd478ba3" },
            { PostMaterialPath, PostMaterialGuid },
        };

        private static readonly string[] PostFilterKeywords =
        {
            null,
            "_PIXELATION_ON", "_COLOR_ADJUST_ON", "_COLOR_SCREEN_BLEND_ON",
            "_ORDERED_DITHER_ON", "_COLOR_QUANTIZATION_ON", "_GRADIENT_MAP_ON",
            "_OLD_FILM_ON", "_EDGE_FILTER_ON", "_ASCII_FILTER_ON", "_CRT_FILTER_ON",
        };

        private static readonly string[] PostFilterLabels =
        {
            "None", "Pixelation", "Color Adjustment", "Color Screen Blend",
            "Ordered Dithering", "Color Quantization", "Gradient Map",
            "Old Film", "Edge Detection / Ink Outline", "ASCII Filter", "CRT",
        };

        private static readonly string[] EffectKeywords =
        {
            "_COLOR_ADJUST_ON", "_EMISSION", "_RIM_ON", "_RIM_MULTIPLY", "_HEIGHT_FADE_ON",
            "_GLASS_GLOW_ON", "_HOLOGRAM_ON", "_HOLOGRAM_WORLD_SPACE",
            "_HOLOGRAM_SCREEN_SPACE", "_GLITCH_ON", "_DISSOLVE_ON",
            "_DISSOLVE_OBJECT_SPACE",
            "_DISSOLVE_RADIAL", "_DISSOLVE_SWIPE", "_DISSOLVE_EDGE_GRADIENT",
            "_LIGHT_SWEEP_ON", "_LIGHT_SWEEP_SHARP", "_LIGHT_SWEEP_MULTIPLY",
            "_DITHER_FADE_ON",
            "_PIXEL_OUTLINE_ON",
            "_UV_FADE_ON", "_STENCIL_OUTLINE_ON", "_SECONDARY_LAYER_ON",
            "_RGB_OVERRIDE_ON", "_PIXELATION_ON", "_COLOR_SCREEN_BLEND_ON",
            "_ORDERED_DITHER_ON", "_COLOR_QUANTIZATION_ON", "_GRADIENT_MAP_ON",
            "_OLD_FILM_ON", "_EDGE_FILTER_ON", "_ASCII_FILTER_ON", "_CRT_FILTER_ON",
        };

        private static readonly string[] StructuralKeywords =
        {
            "_SURFACE_TYPE_TRANSPARENT", "_ALPHATEST_ON", "_ALPHAPREMULTIPLY_ON",
            "_ALPHAMODULATE_ON", "_NORMALMAP", "_METALLICMAP",
            "_SMOOTHNESSMAP", "_UNLIT_ON",
            "_RECEIVE_SHADOWS_OFF", "_UBER_QUALITY_LOW", "UNITY_UI_CLIP_RECT",
            "UNITY_UI_ALPHACLIP", "_GLITCH_OBJECT_SPACE", "_GLITCH_WORLD_SPACE",
            "_TEXTURE_BLEND_ON", "_BASE_MAP_TRIPLANAR",
        };

        private static readonly HashSet<string> AllowedGlobalKeywords =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE",
                "_MAIN_LIGHT_SHADOWS_SCREEN", "_ADDITIONAL_LIGHTS_VERTEX",
                "_ADDITIONAL_LIGHTS", "EVALUATE_SH_MIXED", "EVALUATE_SH_VERTEX",
                "_ADDITIONAL_LIGHT_SHADOWS", "_REFLECTION_PROBE_BLENDING",
                "_REFLECTION_PROBE_BOX_PROJECTION", "_REFLECTION_PROBE_ATLAS",
                "_SHADOWS_SOFT", "_SHADOWS_SOFT_LOW", "_SHADOWS_SOFT_MEDIUM",
                "_SHADOWS_SOFT_HIGH", "_SCREEN_SPACE_OCCLUSION",
                "_SCREEN_SPACE_IRRADIANCE", "_LIGHT_COOKIES", "_LIGHT_LAYERS",
                "_CLUSTER_LIGHT_LOOP", "LIGHTMAP_SHADOW_MIXING",
                "SHADOWS_SHADOWMASK", "DIRLIGHTMAP_COMBINED", "LIGHTMAP_ON",
                "LIGHTMAP_BICUBIC_SAMPLING", "REFLECTION_PROBE_ROTATION",
                "DYNAMICLIGHTMAP_ON", "USE_LEGACY_LIGHTMAPS",
                "LOD_FADE_CROSSFADE", "DEBUG_DISPLAY",
                "_CASTING_PUNCTUAL_LIGHT_SHADOW", "_GBUFFER_NORMALS_OCT",
                "SKINNED_SPRITE",
            };

        [Test]
        public void ShaderAssetsImportWithExactNamesAndNoCompilerErrors()
        {
            foreach (ShaderCase shaderCase in Shaders)
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderCase.Path);
                Assert.That(shader, Is.Not.Null, shaderCase.Path);
                Assert.That(shader.name, Is.EqualTo(shaderCase.Name));
                Assert.That(Shader.Find(shaderCase.Name), Is.SameAs(shader));

                string[] errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(message =>
                        message.severity == ShaderCompilerMessageSeverity.Error)
                    .Select(message => message.message)
                    .ToArray();
                Assert.That(errors, Is.Empty,
                    shaderCase.Name + ": " + string.Join(" | ", errors));
            }
        }

        [Test]
        public void InspectorSynchronizesSurfaceKeywordsQueuesAndPasses()
        {
            Shader shader = Shader.Find(ObjectShaderName);
            Material material = new Material(shader);
            try
            {
                UberShaderGUI gui = new UberShaderGUI();
                Assert.That(material.HasProperty("_ZWriteControl"), Is.True);
                Assert.That(material.GetFloat("_ZWriteControl"), Is.Zero);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWriteControl", 0f);
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_LightingMode", 1f);
                material.SetFloat("_CastShadows", 0f);
                material.SetFloat("_StencilOutlineEnabled", 1f);
                material.SetFloat("_QueueControl", 0f);
                gui.ValidateMaterial(material);

                Assert.That(material.IsKeywordEnabled("_UNLIT_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.True);
                Assert.That(material.GetTag("RenderType", false),
                    Is.EqualTo("Transparent"));
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));
                Assert.That(material.GetShaderPassEnabled("DepthOnly"), Is.False);
                Assert.That(material.renderQueue,
                    Is.GreaterThanOrEqualTo((int)RenderQueue.Transparent));
                Assert.That(material.GetShaderPassEnabled("ShadowCaster"), Is.False);
                Assert.That(material.GetShaderPassEnabled("StencilOutline"), Is.True);

                material.SetFloat("_ZWriteControl", 1f);
                gui.ValidateMaterial(material);
                Assert.That(material.GetTag("RenderType", false),
                    Is.EqualTo("Transparent"));
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(1f));
                Assert.That(material.GetShaderPassEnabled("DepthOnly"), Is.True);
                Assert.That(material.renderQueue,
                    Is.GreaterThanOrEqualTo((int)RenderQueue.Transparent));

                material.SetFloat("_ZWriteControl", 2f);
                gui.ValidateMaterial(material);
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));
                Assert.That(material.GetShaderPassEnabled("DepthOnly"), Is.False);

                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWriteControl", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_LightingMode", 0f);
                material.SetFloat("_CastShadows", 1f);
                material.SetFloat("_StencilOutlineEnabled", 0f);
                material.SetFloat("_QueueControl", 1f);
                material.renderQueue = 2123;
                gui.ValidateMaterial(material);

                Assert.That(material.IsKeywordEnabled("_UNLIT_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.False);
                Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Opaque"));
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(1f));
                Assert.That(material.renderQueue, Is.EqualTo(2123));
                Assert.That(material.GetShaderPassEnabled("ShadowCaster"), Is.True);
                Assert.That(material.GetShaderPassEnabled("StencilOutline"), Is.False);

                AssertPasses(material, "UniversalForward", "StencilOutline",
                    "ShadowCaster", "DepthOnly", "DepthNormals", "Meta");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            Material sprite = new Material(Shader.Find(SpriteShaderName));
            try
            {
                sprite.SetFloat("_Surface", 1f);
                sprite.SetFloat("_Blend", 1f);
                sprite.SetFloat("_BlendModePreserveSpecular", 1f);
                new UberShaderGUI().ValidateMaterial(sprite);
                Assert.That(sprite.GetFloat("_BlendModePreserveSpecular"),
                    Is.EqualTo(0f));
                AssertPasses(sprite, "UniversalForward", "Universal2D",
                    "NormalsRendering", "ShadowCaster", "DepthOnly", "DepthNormals");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
            }
        }

        [Test]
        public void SurfaceMapsUseIndependentChannelsWithoutPackedMap()
        {
            string shaderSource = Read(UberDirectory + "UberSprite.shader");
            string includeSource = Read(UberDirectory + "UberSprite.hlsl");
            string guiSource = Read(EditorDirectory + "UberShaderGUI.cs");
            string objectShaderSource = Read(UberDirectory + "Uber3D.shader");
            string objectIncludeSource = Read(UberDirectory + "Uber3D.hlsl");
            string uiSources = Read(UberDirectory + "UberUI.shader") +
                Read(UberDirectory + "UberUI.hlsl");
            string allSurfaceSources = shaderSource + includeSource + guiSource +
                objectShaderSource + objectIncludeSource + uiSources;

            foreach (string removedContract in new[]
                     {
                         "_MASKMAP", "_MaskMap", "Packed Surface Map",
                         "_OcclusionStrength", "UberSampleMask",
                         "UberSampleSpriteMask",
                     })
            {
                StringAssert.DoesNotContain(removedContract, allSurfaceSources);
            }
            foreach (string contract in new[]
                     {
                         "[SubToggle(SurfaceInputs, _METALLICMAP)] " +
                         "_MetallicMapEnabled(\"Use Metallic Map\", Float) = 0",
                         "[Tex(SurfaceInputs_METALLICMAP)] [NoScaleOffset] " +
                         "_MetallicMap(\"Metallic Map (R)\", 2D) = \"white\" {}",
                         "[SubToggle(SurfaceInputs, _ROUGHNESSMAP)] " +
                         "_RoughnessMapEnabled(\"Use Roughness Map\", Float) = 0",
                         "[Tex(SurfaceInputs_ROUGHNESSMAP)] [NoScaleOffset] " +
                         "_RoughnessMap(\"Roughness Map (R)\", 2D) = \"black\" {}",
                     })
            {
                StringAssert.Contains(contract, objectShaderSource);
            }
            foreach (string keyword in new[] { "_METALLICMAP", "_ROUGHNESSMAP" })
            {
                string[] pragmas = PragmaRows(objectShaderSource, keyword);
                Assert.That(pragmas.Length, Is.EqualTo(2), keyword);
                Assert.That(pragmas.All(row =>
                    row.Contains("multi_compile_local_fragment")), Is.True, keyword);
            }
            foreach (string contract in new[]
                     {
                         "TEXTURE2D(_MetallicMap);",
                         "SAMPLER(sampler_MetallicMap);",
                         "TEXTURE2D(_RoughnessMap);",
                         "SAMPLER(sampler_RoughnessMap);",
                         "surfaceData.metallic = saturate(_Metallic * metallicMask);",
                         "surfaceData.smoothness = saturate(_Smoothness * (1.0h - roughness));",
                         "InitializeBRDFData(albedo, saturate(_Metallic * metallicMask),",
                         "saturate(_Smoothness * (1.0h - roughness)), alpha, brdfData);",
                     })
            {
                StringAssert.Contains(contract, objectIncludeSource);
            }
            StringAssert.Contains("surfaceData.occlusion = 1.0h;",
                objectIncludeSource);

            foreach (string contract in new[]
                     {
                         "[SubToggle(SurfaceInputs, _METALLICMAP)] " +
                         "_MetallicMapEnabled(\"Use Metallic Map\", Float) = 0",
                         "[Tex(SurfaceInputs_METALLICMAP)] [NoScaleOffset] " +
                         "_MetallicMap(\"Metallic Map (R)\", 2D) = \"white\" {}",
                         "[SubToggle(SurfaceInputs, _SMOOTHNESSMAP)] " +
                         "_SmoothnessMapEnabled(\"Use Smoothness Map\", Float) = 0",
                         "[Tex(SurfaceInputs_SMOOTHNESSMAP)] [NoScaleOffset] " +
                         "_SmoothnessMap(\"Smoothness Map (R)\", 2D) = \"white\" {}",
                     })
            {
                StringAssert.Contains(contract, shaderSource);
            }

            foreach (string keyword in new[] { "_METALLICMAP", "_SMOOTHNESSMAP" })
            {
                string[] pragmas = PragmaRows(shaderSource, keyword);
                Assert.That(pragmas.Length, Is.EqualTo(1), keyword);
                StringAssert.Contains("multi_compile_local_fragment", pragmas[0]);
                Assert.That(shaderSource.IndexOf(pragmas[0], StringComparison.Ordinal),
                    Is.LessThan(shaderSource.IndexOf("Name \"Universal2D\"",
                        StringComparison.Ordinal)), keyword);
            }

            foreach (string contract in new[]
                     {
                         "TEXTURE2D(_MetallicMap);",
                         "SAMPLER(sampler_MetallicMap);",
                         "TEXTURE2D(_SmoothnessMap);",
                         "SAMPLER(sampler_SmoothnessMap);",
                         "return SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, baseAtlasUV).r;",
                         "return 1.0h;",
                         "return SAMPLE_TEXTURE2D(_SmoothnessMap, sampler_SmoothnessMap,",
                         "surfaceData.metallic = saturate(_Metallic * metallicMask);",
                         "surfaceData.smoothness = saturate(_Smoothness * smoothnessMask);",
                         "surfaceData.occlusion = 1.0h;",
                     })
            {
                StringAssert.Contains(contract, includeSource);
            }

            Match forwardFragment = Regex.Match(includeSource,
                @"(?s)half4 UberSpriteForwardFragment\(.*?(?=#elif defined\(UBER_SPRITE_2D_PASS\))");
            Assert.That(forwardFragment.Success, Is.True);
            StringAssert.Contains("UberSampleSpriteMetallic(", forwardFragment.Value);
            StringAssert.Contains("UberSampleSpriteSmoothness(", forwardFragment.Value);

            Match sprite2DFragment = Regex.Match(includeSource,
                @"(?s)half4 UberSprite2DFragment\(.*?(?=#elif|#endif)");
            Assert.That(sprite2DFragment.Success, Is.True);
            StringAssert.Contains("InitializeSurfaceData(spriteSurface.albedo, " +
                "spriteSurface.alpha,", sprite2DFragment.Value);
            StringAssert.Contains("half4(1.0h, 1.0h, 1.0h, 1.0h), " +
                "spriteSurface.normalTS",
                sprite2DFragment.Value);
            StringAssert.DoesNotContain("UberSampleSpriteMetallic(",
                sprite2DFragment.Value);
            StringAssert.DoesNotContain("UberSampleSpriteSmoothness(",
                sprite2DFragment.Value);

            StringAssert.Contains(
                "new KeywordBinding(\"_MetallicMapEnabled\", \"_METALLICMAP\", 1)",
                guiSource);
            StringAssert.Contains(
                "new KeywordBinding(\"_RoughnessMapEnabled\", \"_ROUGHNESSMAP\", 1)",
                guiSource);
            StringAssert.Contains(
                "new KeywordBinding(\"_SmoothnessMapEnabled\", \"_SMOOTHNESSMAP\", 1)",
                guiSource);

            Material objectMaterial = new Material(Shader.Find(ObjectShaderName));
            try
            {
                Assert.That(objectMaterial.HasProperty("_MetallicMapEnabled"), Is.True);
                Assert.That(objectMaterial.HasProperty("_MetallicMap"), Is.True);
                Assert.That(objectMaterial.HasProperty("_RoughnessMapEnabled"), Is.True);
                Assert.That(objectMaterial.HasProperty("_RoughnessMap"), Is.True);
                Assert.That(objectMaterial.GetFloat("_MetallicMapEnabled"), Is.Zero);
                Assert.That(objectMaterial.GetFloat("_RoughnessMapEnabled"), Is.Zero);

                objectMaterial.SetFloat("_MetallicMapEnabled", 1f);
                objectMaterial.SetFloat("_RoughnessMapEnabled", 1f);
                UberShaderGUI objectGui = new UberShaderGUI();
                objectGui.ValidateMaterial(objectMaterial);
                Assert.That(objectMaterial.IsKeywordEnabled("_METALLICMAP"), Is.True);
                Assert.That(objectMaterial.IsKeywordEnabled("_ROUGHNESSMAP"), Is.True);

                objectMaterial.SetFloat("_MetallicMapEnabled", 0f);
                objectMaterial.SetFloat("_RoughnessMapEnabled", 0f);
                objectGui.ValidateMaterial(objectMaterial);
                Assert.That(objectMaterial.IsKeywordEnabled("_METALLICMAP"), Is.False);
                Assert.That(objectMaterial.IsKeywordEnabled("_ROUGHNESSMAP"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(objectMaterial);
            }

            Material material = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Assert.That(material.HasProperty("_MetallicMapEnabled"), Is.True);
                Assert.That(material.HasProperty("_MetallicMap"), Is.True);
                Assert.That(material.HasProperty("_SmoothnessMapEnabled"), Is.True);
                Assert.That(material.HasProperty("_SmoothnessMap"), Is.True);
                Assert.That(material.HasProperty("_MaskMapEnabled"), Is.False);
                Assert.That(material.HasProperty("_MaskMap"), Is.False);
                Assert.That(material.HasProperty("_OcclusionStrength"), Is.False);
                Assert.That(material.GetFloat("_MetallicMapEnabled"), Is.Zero);
                Assert.That(material.GetFloat("_SmoothnessMapEnabled"), Is.Zero);

                material.SetFloat("_MetallicMapEnabled", 1f);
                material.SetFloat("_SmoothnessMapEnabled", 1f);
                UberShaderGUI gui = new UberShaderGUI();
                gui.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_METALLICMAP"), Is.True);
                Assert.That(material.IsKeywordEnabled("_SMOOTHNESSMAP"), Is.True);

                material.SetFloat("_MetallicMapEnabled", 0f);
                material.SetFloat("_SmoothnessMapEnabled", 0f);
                gui.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_METALLICMAP"), Is.False);
                Assert.That(material.IsKeywordEnabled("_SMOOTHNESSMAP"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            foreach (string shaderName in new[] { ObjectShaderName, UIShaderName })
            {
                Material surfaceMaterial = new Material(Shader.Find(shaderName));
                try
                {
                    Assert.That(surfaceMaterial.HasProperty("_MaskMapEnabled"),
                        Is.False, shaderName);
                    Assert.That(surfaceMaterial.HasProperty("_MaskMap"),
                        Is.False, shaderName);
                    Assert.That(surfaceMaterial.HasProperty("_OcclusionStrength"),
                        Is.False, shaderName);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(surfaceMaterial);
                }
            }
        }

        [Test]
        public void SpriteAndUiOutlineHologramGpuMatrixPreservesFunctionalOutput()
        {
            const int size = 32;
            Vector4 previousTextureSampleAdd =
                Shader.GetGlobalVector("_TextureSampleAdd");
            Color previousSpriteColor = Shader.GetGlobalColor("unity_SpriteColor");
            Vector4 previousSpriteProps = Shader.GetGlobalVector("unity_SpriteProps");
            Texture2D source = null;
            Texture2D secondary = null;
            Texture2D externalAlpha = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material sprite = null;
            Material ui = null;
            try
            {
                Shader.SetGlobalVector("_TextureSampleAdd", Vector4.zero);
                Shader.SetGlobalColor("unity_SpriteColor", Color.white);
                Shader.SetGlobalVector("unity_SpriteProps",
                    new Vector4(1f, 1f, 0f, 0f));

                source = new Texture2D(size, size, TextureFormat.RGBA32,
                    false, true) { filterMode = FilterMode.Point };
                Color32[] sourcePixels = Enumerable.Repeat(
                    new Color32(0, 0, 0, 0), size * size).ToArray();
                for (int y = 5; y < 27; ++y)
                for (int x = 2; x < 14; ++x)
                    sourcePixels[y * size + x] = new Color32(26, 38, 51, 255);
                sourcePixels[size + 1] = new Color32(26, 38, 51, 1);
                sourcePixels[size + 2] = new Color32(26, 38, 51, 1);
                for (int y = 0; y < size; ++y)
                for (int x = size / 2; x < size; ++x)
                    sourcePixels[y * size + x] =
                        new Color32(255, 0, 255, 255);
                source.SetPixels32(sourcePixels);
                source.Apply(false, false);

                secondary = new Texture2D(size, size, TextureFormat.RGBA32,
                    false, true) { filterMode = FilterMode.Point };
                Color32[] secondaryPixels = Enumerable.Repeat(
                    new Color32(0, 0, 0, 0), size * size).ToArray();
                for (int y = 9; y < 23; ++y)
                for (int x = 1; x < 10; ++x)
                    secondaryPixels[y * size + x] =
                        new Color32(13, 64, 102, 255);
                secondary.SetPixels32(secondaryPixels);
                secondary.Apply(false, false);

                externalAlpha = new Texture2D(size, size, TextureFormat.RGBA32,
                    false, true) { filterMode = FilterMode.Point };
                Color32[] alphaPixels = Enumerable.Repeat(
                    new Color32(0, 0, 0, 255), size * size).ToArray();
                for (int y = 7; y < 25; ++y)
                for (int x = 4; x < 15; ++x)
                    alphaPixels[y * size + x] =
                        new Color32(255, 0, 0, 255);
                externalAlpha.SetPixels32(alphaPixels);
                externalAlpha.Apply(false, false);

                readback = new Texture2D(size, size, TextureFormat.RGBA32,
                    false, true);
                target = new RenderTexture(size, size, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);
                Vector4 atlasRect = new Vector4(0f, 0f, 0.5f, 1f);
                var frames = new List<Color[]>();

                sprite = new Material(Shader.Find(SpriteShaderName));
                sprite.SetColor("_BaseColor", Color.white);
                sprite.SetColor("_Color", Color.white);
                sprite.SetVector("_BaseSpriteUVRect", atlasRect);
                sprite.SetFloat("_AlphaMultiplier", 1f);
                sprite.SetFloat("_Surface", 1f);
                sprite.SetFloat("_Blend", 0f);
                sprite.SetFloat("_LightingMode", 1f);
                sprite.SetFloat("_SrcBlend", 1f);
                sprite.SetFloat("_DstBlend", 0f);
                sprite.SetFloat("_SrcBlendAlpha", 1f);
                sprite.SetFloat("_DstBlendAlpha", 0f);
                sprite.SetColor("_PixelOutlineColor",
                    new Color(0.25f, 0.75f, 0.1f, 1f));
                sprite.SetFloat("_PixelOutlineAlphaThreshold", 0.5f);
                sprite.EnableKeyword("_UNLIT_ON");
                sprite.EnableKeyword("_PIXEL_OUTLINE_ON");

                Color[] spriteWidthZero = Capture(sprite, 0f);
                Color[] spriteBelowHalf = Capture(sprite, 0.499f);
                Color[] spriteAtHalf = Capture(sprite, 0.5f);
                Color[] spriteBelowOneHalf = Capture(sprite, 1.499f);
                Color[] spriteAtOneHalf = Capture(sprite, 1.5f);
                Color[] spriteHighMaximum = Capture(sprite, 4f);
                sprite.EnableKeyword("_UBER_QUALITY_LOW");
                Color[] spriteLowMaximum = Capture(sprite, 4f);
                sprite.DisableKeyword("_UBER_QUALITY_LOW");

                sprite.SetTexture("_AlphaTex", externalAlpha);
                sprite.SetFloat("_EnableExternalAlpha", 1f);
                frames.Add(RenderSpriteDissolve(source, sprite, target, readback));
                sprite.SetFloat("_EnableExternalAlpha", 0f);
                sprite.SetTexture("_SecondaryTex", secondary);
                sprite.SetVector("_SecondaryUVRect", atlasRect);
                sprite.SetFloat("_SecondaryBlendAmount", 1f);
                sprite.EnableKeyword("_SECONDARY_LAYER_ON");
                frames.Add(RenderSpriteDissolve(source, sprite, target, readback));
                sprite.DisableKeyword("_SECONDARY_LAYER_ON");
                sprite.DisableKeyword("_PIXEL_OUTLINE_ON");

                ConfigureHologram(sprite);
                Color[] spriteHologramHigh = RenderSpriteDissolve(source, sprite,
                    target, readback);
                frames.Add(spriteHologramHigh);
                sprite.EnableKeyword("_UBER_QUALITY_LOW");
                Color[] spriteHologramLow = RenderSpriteDissolve(source, sprite,
                    target, readback);
                frames.Add(spriteHologramLow);
                sprite.DisableKeyword("_UBER_QUALITY_LOW");
                sprite.SetFloat("_HologramFresnelIntensity", 0f);
                frames.Add(RenderSpriteDissolve(source, sprite, target, readback));
                sprite.SetFloat("_HologramFresnelIntensity", 1f);
                sprite.SetFloat("_EnableExternalAlpha", 1f);
                frames.Add(RenderSpriteDissolve(source, sprite, target, readback));
                sprite.SetFloat("_EnableExternalAlpha", 0f);
                sprite.EnableKeyword("_SECONDARY_LAYER_ON");
                frames.Add(RenderSpriteDissolve(source, sprite, target, readback));

                ui = new Material(Shader.Find(UIShaderName));
                ui.SetColor("_Color", Color.white);
                ui.SetFloat("_AlphaMultiplier", 1f);
                ui.SetVector("_BaseSpriteUVRect", atlasRect);
                ui.SetColor("_PixelOutlineColor",
                    new Color(0.75f, 0.2f, 0.1f, 1f));
                ui.SetFloat("_PixelOutlineAlphaThreshold", 0.5f);
                ui.SetColor("_PixelGlowColor", new Color(0.1f, 0.2f, 1f, 0f));
                ui.SetFloat("_PixelGlowWidth", 0f);
                ui.SetFloat("_PixelGlowIntensity", 1f);
                ui.EnableKeyword("_PIXEL_OUTLINE_ON");

                Color[] uiWidthZero = Capture(ui, 0f);
                Color[] uiBelowHalf = Capture(ui, 0.499f);
                Color[] uiAtHalf = Capture(ui, 0.5f);
                Color[] uiBelowOneHalf = Capture(ui, 1.499f);
                Color[] uiAtOneHalf = Capture(ui, 1.5f);
                Color[] uiOutlineMaximum = Capture(ui, 4f);
                ui.SetFloat("_PixelOutlineWidth", 0f);
                ui.SetColor("_PixelOutlineColor", new Color(1f, 1f, 1f, 0f));
                ui.SetColor("_PixelGlowColor", new Color(0.1f, 0.2f, 1f, 0.5f));
                foreach (float glowWidth in new[] { 0.499f, 0.5f, 7.499f, 7.5f, 8f })
                {
                    ui.SetFloat("_PixelGlowWidth", glowWidth);
                    frames.Add(RenderSpriteDissolve(source, ui, target, readback));
                }
                ui.DisableKeyword("_PIXEL_OUTLINE_ON");
                ConfigureHologram(ui);
                Color[] uiHologramHigh = RenderSpriteDissolve(source, ui,
                    target, readback);
                frames.Add(uiHologramHigh);
                ui.EnableKeyword("_UBER_QUALITY_LOW");
                Color[] uiHologramLow = RenderSpriteDissolve(source, ui,
                    target, readback);
                frames.Add(uiHologramLow);
                ui.DisableKeyword("_UBER_QUALITY_LOW");
                ui.SetFloat("_HologramFresnelIntensity", 0f);
                frames.Add(RenderSpriteDissolve(source, ui, target, readback));

                CollectionAssert.AreEqual(ToColor32(spriteWidthZero),
                    ToColor32(spriteBelowHalf));
                CollectionAssert.AreEqual(ToColor32(spriteAtHalf),
                    ToColor32(spriteBelowOneHalf));
                Assert.That(MaxRgbDifference(spriteAtHalf, spriteBelowHalf),
                    Is.GreaterThan(0.01f));
                Assert.That(MaxRgbDifference(spriteAtOneHalf, spriteAtHalf),
                    Is.GreaterThan(0.01f));
                Assert.That(MaxRgbDifference(spriteHighMaximum,
                    spriteLowMaximum), Is.GreaterThan(0.01f));
                CollectionAssert.AreEqual(ToColor32(uiWidthZero),
                    ToColor32(uiBelowHalf));
                CollectionAssert.AreEqual(ToColor32(uiAtHalf),
                    ToColor32(uiBelowOneHalf));
                Assert.That(MaxRgbDifference(uiAtHalf, uiBelowHalf),
                    Is.GreaterThan(0.01f));
                Assert.That(MaxRgbDifference(uiAtOneHalf, uiAtHalf),
                    Is.GreaterThan(0.01f));
                Assert.That(MaxRgbDifference(spriteHologramHigh,
                    spriteHologramLow), Is.GreaterThan(0.01f));
                Assert.That(MaxRgbDifference(uiHologramHigh, uiHologramLow),
                    Is.GreaterThan(0.01f));
                Assert.That(frames.SelectMany(frame => frame).All(IsFinite),
                    Is.True);
                Assert.That(HasPixelDifferentFrom(frames, new Color32(0, 0, 0, 0)),
                    Is.True, "The sprite/UI matrix must render visible output.");
                Assert.That(HasDistinctFrames(frames), Is.True,
                    "The sprite/UI matrix must exercise different visual states.");

                Color[] Capture(Material material, float outlineWidth)
                {
                    material.SetFloat("_PixelOutlineWidth", outlineWidth);
                    Color[] frame = RenderSpriteDissolve(source, material,
                        target, readback);
                    frames.Add(frame);
                    return frame;
                }

                void ConfigureHologram(Material material)
                {
                    material.SetColor("_HologramColor", Color.white);
                    material.SetFloat("_HologramOpacity", 1f);
                    material.SetFloat("_HologramFresnelPower", 2f);
                    material.SetFloat("_HologramFresnelIntensity", 1f);
                    material.SetFloat("_HologramEdgeSoftnessPixels", 4f);
                    material.SetFloat("_HologramScanlineIntensity", 0f);
                    material.SetFloat("_HologramNoiseStrength", 0f);
                    material.EnableKeyword("_HOLOGRAM_ON");
                }
            }
            finally
            {
                Shader.SetGlobalVector("_TextureSampleAdd",
                    previousTextureSampleAdd);
                Shader.SetGlobalColor("unity_SpriteColor", previousSpriteColor);
                Shader.SetGlobalVector("unity_SpriteProps", previousSpriteProps);
                if (sprite != null)
                    UnityEngine.Object.DestroyImmediate(sprite);
                if (ui != null)
                    UnityEngine.Object.DestroyImmediate(ui);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
                if (secondary != null)
                    UnityEngine.Object.DestroyImmediate(secondary);
                if (externalAlpha != null)
                    UnityEngine.Object.DestroyImmediate(externalAlpha);
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        [Test]
        public void SpriteAndUiOutlineHologramPruningIsUniformAndAccountsSamples()
        {
            string sprite = Read(UberDirectory + "UberSprite.hlsl");
            string ui = Read(UberDirectory + "UberUI.hlsl");
            string spriteOutline = Regex.Match(sprite,
                @"(?s)inline half UberEvaluatePixelOutline\(.*?" +
                @"(?=inline half UberEvaluateUVFade\()").Value;
            string uiOutline = Regex.Match(ui,
                @"(?s)inline void UberUIOutlineMasks\(.*?" +
                @"(?=inline void UberUICompositeStraightAlpha\()").Value;
            string spriteHologramDirection = Regex.Match(sprite,
                @"(?s)inline half UberEvaluateSpriteHologramEdgeDirection\(.*?" +
                @"(?=// A flat sprite)").Value;
            string spriteHologram = Regex.Match(sprite,
                @"(?s)inline half UberEvaluateSpriteHologramEdge\(.*?" +
                @"(?=inline float3 UberGetHologramUpVector\()").Value;
            string uiHologramDirection = Regex.Match(ui,
                @"(?s)inline half UberUIEvaluateHologramEdgeDirection\(.*?" +
                @"(?=// UI has no useful view-angle Fresnel term)").Value;
            string uiHologram = Regex.Match(ui,
                @"(?s)inline half UberUIEvaluateHologramEdge\(.*?" +
                @"(?=inline float3 UberUIGetHologramUpVector\()").Value;
            Assert.That(new[]
            {
                spriteOutline, uiOutline, spriteHologramDirection,
                spriteHologram, uiHologramDirection, uiHologram,
            }.All(value => value.Length > 0), Is.True);
            StringAssert.DoesNotContain("if (centerAlpha", spriteOutline);
            StringAssert.DoesNotContain("if (centerAlpha", uiOutline);
            StringAssert.DoesNotContain("if (outside", uiOutline);

            StringAssert.Contains(
                "half ringEnabled = step((half)ring - 0.5h, _PixelOutlineWidth);",
                spriteOutline);
            StringAssert.Contains("if (ringEnabled < 0.5h)", spriteOutline);
            Assert.That(spriteOutline.IndexOf("if (ringEnabled < 0.5h)",
                    StringComparison.Ordinal),
                Is.LessThan(spriteOutline.IndexOf(
                    "UberSampleSpriteLayerAlpha(rawUV +",
                    StringComparison.Ordinal)));
            int spriteMaxRings = int.Parse(Regex.Match(spriteOutline,
                @"ring\s*<=\s*(?<value>\d+)").Groups["value"].Value);
            int spriteLowRings = int.Parse(Regex.Match(spriteOutline,
                @"ring\s*>\s*(?<value>\d+)").Groups["value"].Value);
            int spriteHighDirections = Regex.Matches(spriteOutline,
                @"\bUberSampleSpriteLayerAlpha\s*\(").Count;
            string spriteLowBlock = spriteOutline.Substring(0,
                spriteOutline.IndexOf("#if !defined(_UBER_QUALITY_LOW)",
                    StringComparison.Ordinal));
            int spriteLowDirections = Regex.Matches(spriteLowBlock,
                @"\bUberSampleSpriteLayerAlpha\s*\(").Count;
            Assert.That(spriteHighDirections * spriteMaxRings, Is.EqualTo(32));
            Assert.That(spriteLowDirections * spriteLowRings, Is.EqualTo(8));

            StringAssert.Contains(
                "half outlineRingEnabled = step((half)ring - 0.5h, outlineWidth);",
                uiOutline);
            StringAssert.Contains(
                "half glowEnabled = step((half)ring - 0.5h, glowWidth);",
                uiOutline);
            StringAssert.Contains(
                "if (max(outlineRingEnabled, glowEnabled) < 0.5h)",
                uiOutline);
            Assert.That(uiOutline.IndexOf(
                    "if (max(outlineRingEnabled, glowEnabled) < 0.5h)",
                    StringComparison.Ordinal),
                Is.LessThan(uiOutline.IndexOf("UberSampleUISpriteAlpha(uv +",
                    StringComparison.Ordinal)));
            int uiMaxRings = int.Parse(Regex.Match(uiOutline,
                @"ring\s*<=\s*(?<value>\d+)").Groups["value"].Value);
            int uiDirections = Regex.Matches(uiOutline,
                @"\bUberSampleUISpriteAlpha\s*\(").Count;
            Assert.That(uiDirections * uiMaxRings, Is.EqualTo(64));

            foreach (string hologram in new[] { spriteHologram, uiHologram })
            {
                StringAssert.Contains(
                    "half edgeIntensity = max(_HologramFresnelIntensity, 0.0h);",
                    hologram);
                StringAssert.Contains("if (edgeIntensity == 0.0h)", hologram);
                Assert.That(hologram.IndexOf(
                        "if (edgeIntensity == 0.0h)",
                        StringComparison.Ordinal),
                    Is.LessThan(hologram.IndexOf("EdgeDirection(",
                        StringComparison.Ordinal)));
                StringAssert.DoesNotContain("if (centerAlpha", hologram);
            }
            foreach (string direction in new[]
                     {
                         spriteHologramDirection, uiHologramDirection,
                     })
            {
                StringAssert.DoesNotContain("if (centerAlpha", direction);
                StringAssert.DoesNotContain("if (boundaryFound", direction);
                StringAssert.Contains("const int edgeCoarseSteps = 2;",
                    direction);
                StringAssert.Contains("const int edgeRefinementSteps = 2;",
                    direction);
                StringAssert.Contains("const int edgeCoarseSteps = 4;",
                    direction);
                StringAssert.Contains("const int edgeRefinementSteps = 3;",
                    direction);
            }
            int hologramHighDirections = Regex.Matches(spriteHologram,
                @"\bUberEvaluateSpriteHologramEdgeDirection\s*\(").Count;
            string hologramLowBlock = spriteHologram.Substring(0,
                spriteHologram.IndexOf("#if !defined(_UBER_QUALITY_LOW)",
                    StringComparison.Ordinal));
            int hologramLowDirections = Regex.Matches(hologramLowBlock,
                @"\bUberEvaluateSpriteHologramEdgeDirection\s*\(").Count;
            Match stepCounts = Regex.Match(spriteHologramDirection,
                @"(?s)#if defined\(_UBER_QUALITY_LOW\).*?" +
                @"edgeCoarseSteps\s*=\s*(?<lowCoarse>\d+).*?" +
                @"edgeRefinementSteps\s*=\s*(?<lowRefine>\d+).*?#else.*?" +
                @"edgeCoarseSteps\s*=\s*(?<highCoarse>\d+).*?" +
                @"edgeRefinementSteps\s*=\s*(?<highRefine>\d+)");
            Assert.That(stepCounts.Success, Is.True);
            int lowSamplesPerDirection =
                int.Parse(stepCounts.Groups["lowCoarse"].Value) +
                int.Parse(stepCounts.Groups["lowRefine"].Value);
            int highSamplesPerDirection =
                int.Parse(stepCounts.Groups["highCoarse"].Value) +
                int.Parse(stepCounts.Groups["highRefine"].Value);
            Assert.That(hologramHighDirections * highSamplesPerDirection,
                Is.EqualTo(56));
            Assert.That(hologramLowDirections * lowSamplesPerDirection,
                Is.EqualTo(16));

            StringAssert.Contains("return UberSampleSpriteLayers(rawUV).a * inside;",
                sprite);
            StringAssert.Contains(
                "float2 safeUV = UberClampUV(uv, _BaseSpriteUVRect);", ui);
            StringAssert.Contains(
                "edgeMask * max(_HologramFresnelIntensity, 0.0h)", sprite);
            StringAssert.Contains(
                "edgeMask * max(_HologramFresnelIntensity, 0.0h)", ui);
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 1, 2, 4 },
                new[] { 0f, 0.499f, 0.5f, 1.499f, 1.5f, 4f }
                    .Select(width => Enumerable.Range(1, 4).Count(ring =>
                        width >= ring - 0.5f)).ToArray());
            Assert.That(Math.Max(
                    Enumerable.Range(1, 4).Count(ring => 1.5f >= ring - 0.5f),
                    Enumerable.Range(1, 8).Count(ring => 7.5f >= ring - 0.5f)),
                Is.EqualTo(8));
        }

        [Test]
        public void SpriteAndUiOutlineHologramAffectedVariantsCompileForWebGlGles3()
        {
            BuildTarget activeBuildTarget =
                EditorUserBuildSettings.activeBuildTarget;
            Shader spriteShader = AssetDatabase.LoadAssetAtPath<Shader>(
                UberDirectory + "UberSprite.shader");
            Shader uiShader = AssetDatabase.LoadAssetAtPath<Shader>(
                UberDirectory + "UberUI.shader");
            Assert.That(spriteShader, Is.Not.Null);
            Assert.That(uiShader, Is.Not.Null);
            ShaderData.Subshader spriteSubshader =
                ShaderUtil.GetShaderData(spriteShader).ActiveSubshader;
            ShaderData.Subshader uiSubshader =
                ShaderUtil.GetShaderData(uiShader).ActiveSubshader;
            string[][] combinedRows =
            {
                new[] { "_PIXEL_OUTLINE_ON", "_HOLOGRAM_ON" },
                new[]
                {
                    "_PIXEL_OUTLINE_ON", "_HOLOGRAM_ON",
                    "_UBER_QUALITY_LOW",
                },
            };
            string[][] outlineRows =
            {
                new[] { "_PIXEL_OUTLINE_ON" },
                new[] { "_PIXEL_OUTLINE_ON", "_UBER_QUALITY_LOW" },
            };
            string[][] rimRows =
            {
                new[] { "_RIM_ON" },
                new[] { "_RIM_ON", "_UBER_QUALITY_LOW" },
                new[] { "_RIM_MULTIPLY", "_RIM_ON" },
                new[]
                {
                    "_RIM_MULTIPLY", "_RIM_ON", "_UBER_QUALITY_LOW",
                },
            };
            int compiledVariantCount = 0;
            foreach (string passName in new[]
                     {
                          "UniversalForward", "Universal2D",
                     })
            foreach (string[] keywords in combinedRows)
                Compile(spriteSubshader, "UberSprite.shader", passName,
                    keywords);
            foreach (string passName in new[]
                     {
                         "UniversalForward", "Universal2D",
                     })
            foreach (string[] keywords in rimRows)
                Compile(spriteSubshader, "UberSprite.shader", passName,
                    keywords);
            foreach (string passName in new[]
                     {
                         "NormalsRendering", "ShadowCaster", "DepthOnly",
                         "DepthNormals",
                     })
            foreach (string[] keywords in outlineRows)
                Compile(spriteSubshader, "UberSprite.shader", passName,
                    keywords);
            foreach (string[] keywords in combinedRows)
                Compile(uiSubshader, "UberUI.shader", "Default", keywords);

            Assert.That(compiledVariantCount, Is.EqualTo(22));
            Assert.That(EditorUserBuildSettings.activeBuildTarget,
                Is.EqualTo(activeBuildTarget));

            void Compile(ShaderData.Subshader subshader, string shaderPath,
                string passName, string[] keywords)
            {
                ShaderData.Pass pass = Enumerable.Range(0,
                        subshader.PassCount)
                    .Select(subshader.GetPass)
                    .SingleOrDefault(candidate => candidate.Name == passName);
                Assert.That(pass, Is.Not.Null,
                    shaderPath + " missing pass " + passName);
                var compiled = pass.CompileVariant(ShaderType.Fragment,
                    keywords, ShaderCompilerPlatform.GLES3x,
                    BuildTarget.WebGL);
                string context = shaderPath + "/" + passName + " " +
                    string.Join(" ", keywords);
                string[] diagnostics = compiled.Messages.Where(message =>
                        message.severity == ShaderCompilerMessageSeverity.Warning ||
                        message.severity == ShaderCompilerMessageSeverity.Error)
                    .Select(message => message.severity + ": " +
                        message.message).ToArray();
                Assert.That(compiled.Success, Is.True,
                    context + ": " + string.Join(" | ", diagnostics));
                Assert.That(diagnostics, Is.Empty, context);
                ++compiledVariantCount;
            }
        }

        [Test]
        public void RemainingInspectorsUseSpacedCollapsibleLwguiGroups()
        {
            AssertGroupedInspector(UberDirectory + "UberSprite.shader", new[]
            {
                "[Main(Surface, _, on, off)] _SurfaceOptions(\"Surface\", Float) = 1",
                "[Main(SurfaceInputs, _, on, off)] _SurfaceInputs(\"Surface Inputs\", Float) = 1",
                "[Main(SecondaryLayer, _SECONDARY_LAYER_ON, on)] _SecondaryLayerEnabled(\"Secondary Layer\", Float) = 0",
                "[Main(ColorAdjust, _COLOR_ADJUST_ON, on)] _ColorAdjustEnabled(\"Color Adjustment\", Float) = 0",
                "[Main(Grayscale, _GRAYSCALE_ON, on)] _GrayscaleEnabled (\"Grayscale Mask\", Float) = 0",
                "[Main(UVFade, _UV_FADE_ON, on)] _UVFadeEnabled(\"UV Fade\", Float) = 0",
                "[Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled(\"Dissolve\", Float) = 0",
                "[Main(LightSweep, _LIGHT_SWEEP_ON, on)] _LightSweepEnabled(\"Light Sweep\", Float) = 0",
                "[Main(DitherFade, _DITHER_FADE_ON, on)] _DitherFadeEnabled(\"Dither Fade\", Float) = 0",
                "[Main(PixelOutline, _PIXEL_OUTLINE_ON, on)] _PixelOutlineEnabled(\"Pixel Outline\", Float) = 0",
                "[Main(Emission, _EMISSION, on)] _EmissionEnabled(\"Emission\", Float) = 0",
                "[Main(Rim, _RIM_ON, on)] _RimEnabled(\"Fresnel Rim\", Float) = 0",
                "[Main(Hologram, _HOLOGRAM_ON, on)] _HologramEnabled(\"Hologram\", Float) = 0",
                "[Main(Glitch, _GLITCH_ON, on)] _GlitchEnabled(\"Glitch\", Float) = 0",
            }, new[]
            {
                "Surface", "SurfaceInputs", "SecondaryLayer", "ColorAdjust", "Grayscale",
                "UVFade", "Dissolve", "LightSweep", "DitherFade", "PixelOutline", "Emission",
                "Rim", "Hologram", "Glitch",
            }, new[]
            {
                "[Title(Surface, _)] [KWEnum(Surface, Opaque, _, Transparent, _SURFACE_TYPE_TRANSPARENT)] _Surface(\"Surface Type\", Float) = 1",
                "[KWEnum(Surface, Off, _, Front, _, Back, _)] _Cull(\"Render Face\", Float) = 0",
                "[KWEnum(Surface, Off, _, Front, _, Back, _)] _ShadowCull(\"Shadow Face\", Float) = 2",
                "[Title(SurfaceInputs, _)] [Tex(SurfaceInputs)] [PerRendererData] [MainTexture] _MainTex(\"Sprite Texture\", 2D) = \"white\" {}",
                "[Sub(SurfaceInputs)] [MainColor] _BaseColor(\"Base Color\", Color) = (1, 1, 1, 1)",
                "[Tex(SurfaceInputs_NORMALMAP, _NormalScale)] [Normal] [NoScaleOffset] _NormalMap(\"Normal Map\", 2D) = \"bump\" {}",
                "[Tex(SurfaceInputs_METALLICMAP)] [NoScaleOffset] _MetallicMap(\"Metallic Map (R)\", 2D) = \"white\" {}",
                "[Tex(SurfaceInputs_SMOOTHNESSMAP)] [NoScaleOffset] _SmoothnessMap(\"Smoothness Map (R)\", 2D) = \"white\" {}",
                "[Title(SecondaryLayer, _)] [Tex(SecondaryLayer)] [NoScaleOffset] _SecondaryTex(\"Secondary Sprite\", 2D) = \"white\" {}",
                "[Title(UVFade, _)] [KWEnum(UVFade, U, _, V, _)] _UVFadeAxis(\"Axis\", Float) = 1",
                "[Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap(\"Noise Map\", 2D) = \"white\" {}",
                "[Title(LightSweep, _)] [KWEnum(LightSweep, Soft, _, Sharp, _LIGHT_SWEEP_SHARP)] _LightSweepMode(\"Type\", Float) = 0",
                "[KWEnum(LightSweep, Add, _, Multiply, _LIGHT_SWEEP_MULTIPLY)] _LightSweepBlendMode(\"Blend Mode\", Float) = 0",
                "[UberVector2(LightSweep)] _LightSweepCenter(\"Center\", Vector) = (0.5, 0.5, 0, 0)",
                "[UberMinMaxVector(LightSweep)] _LightSweepRange(\"Range\", Vector) = (-0.5, 0.5, 0, 0)",
                "[Title(Emission, _)] [Tex(Emission)] [NoScaleOffset] _EmissionMap(\"Emission Map\", 2D) = \"white\" {}",
            });

            AssertGroupedInspector(UberDirectory + "UberUI.shader", new[]
            {
                "[Main(SurfaceInputs, _, on, off)] _SurfaceInputs (\"Surface Inputs\", Float) = 1",
                "[Main(ColorAdjust, _COLOR_ADJUST_ON, on)] _ColorAdjustEnabled (\"Color Adjustment\", Float) = 0",
                "[Main(Grayscale, _GRAYSCALE_ON, on)] _GrayscaleEnabled (\"Grayscale Mask\", Float) = 0",
                "[Main(Emission, _EMISSION, on)] _EmissionEnabled (\"Emission\", Float) = 0",
                "[Main(RGBOverride, _RGB_OVERRIDE_ON, on)] _RGBOverrideEnabled (\"RGB Override\", Float) = 0",
                "[Main(UVFade, _UV_FADE_ON, on)] _UVFadeEnabled (\"UV Fade\", Float) = 0",
                "[Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled (\"Dissolve\", Float) = 0",
                "[Main(LightSweep, _LIGHT_SWEEP_ON, on)] _LightSweepEnabled (\"Light Sweep\", Float) = 0",
                "[Main(DitherFade, _DITHER_FADE_ON, on)] _DitherFadeEnabled (\"Dither Fade\", Float) = 0",
                "[Main(PixelOutline, _PIXEL_OUTLINE_ON, on)] _PixelOutlineEnabled (\"Pixel Outline / Glow\", Float) = 0",
                "[Main(Hologram, _HOLOGRAM_ON, on)] _HologramEnabled (\"Hologram\", Float) = 0",
                "[Main(Glitch, _GLITCH_ON, on)] _GlitchEnabled (\"Glitch\", Float) = 0",
                "[Main(StencilOptions, _, on, off)] _StencilOptions (\"Stencil / Mask\", Float) = 1",
            }, new[]
            {
                "SurfaceInputs", "ColorAdjust", "Grayscale", "Emission", "RGBOverride", "UVFade",
                "Dissolve", "LightSweep", "DitherFade", "PixelOutline", "Hologram", "Glitch",
                "StencilOptions",
            }, new[]
            {
                "[Title(SurfaceInputs, _)] [Tex(SurfaceInputs)] [PerRendererData] [MainTexture] _MainTex (\"Sprite Texture\", 2D) = \"white\" {}",
                "[Sub(SurfaceInputs)] [MainColor] _Color (\"Tint\", Color) = (1,1,1,1)",
                "[KWEnum(SurfaceInputs, High, _, Low, _UBER_QUALITY_LOW)] _UberQuality (\"Effect Quality\", Float) = 0",
                "[Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap (\"Noise Map\", 2D) = \"white\" {}",
                "[KWEnum(Dissolve, Noise, _, Radial, _DISSOLVE_RADIAL, Swipe, _DISSOLVE_SWIPE)] _DissolveMode (\"Mode\", Float) = 0",
                "[Title(LightSweep, _)] [KWEnum(LightSweep, Soft, _, Sharp, _LIGHT_SWEEP_SHARP)] _LightSweepMode (\"Type\", Float) = 0",
                "[KWEnum(LightSweep, Add, _, Multiply, _LIGHT_SWEEP_MULTIPLY)] _LightSweepBlendMode (\"Blend Mode\", Float) = 0",
                "[UberVector2(LightSweep)] _LightSweepCenter (\"Center\", Vector) = (0.5,0.5,0,0)",
                "[UberMinMaxVector(LightSweep)] _LightSweepRange (\"Range\", Vector) = (-0.5,0.5,0,0)",
                "[KWEnum(Hologram, Object, _, World, _HOLOGRAM_WORLD_SPACE, Screen, _HOLOGRAM_SCREEN_SPACE)] _HologramSpace (\"Space\", Float) = 0",
                "[UberVector3(Hologram)] _HologramObjectUpVector (\"Object Up Vector\", Vector) = (0,1,0,0)",
                "[SubToggle(StencilOptions, UNITY_UI_ALPHACLIP)] _UseUIAlphaClip (\"Use Alpha Clip\", Float) = 0",
                "[Sub(StencilOptionsUNITY_UI_ALPHACLIP)] _Cutoff (\"Alpha Clip Threshold\", Range(0,1)) = 0.001",
            });

            string postSource = Read(UberDirectory + "UberPostProcessing.shader");
            Match postPropertiesMatch = Regex.Match(postSource,
                @"(?s)\bProperties\s*\{(?<body>.*?)\r?\n\s*\}\s*\r?\n\s*SubShader\b");
            Assert.That(postPropertiesMatch.Success, Is.True);
            string postProperties = postPropertiesMatch.Groups["body"].Value;
            string[] postRows = Lines(postProperties).Where(line =>
                Regex.IsMatch(line, @"_[A-Za-z][A-Za-z0-9_]*\s*\(")).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "[Main(ScreenFilter, _, on, off)] _ScreenFilterOptions(\"Screen Filter\", Float) = 1",
            }, postRows.Where(row => row.Contains("[Main(")).ToArray());
            Assert.That(postRows, Does.Contain(
                "[Title(ScreenFilter, _)] [UberPostFilter(ScreenFilter)] _ScreenFilterMode(\"Filter\", Float) = 0"));

            string[] postOwners = PostFilterKeywords.Skip(1)
                .Select(keyword => "ScreenFilter" + keyword).ToArray();
            CollectionAssert.AreEqual(new[] { "ScreenFilter" }.Concat(postOwners),
                Regex.Matches(postProperties,
                        @"\[Title\((?<owner>[^,]+),\s*_\)\]").Cast<Match>()
                    .Select(match => match.Groups["owner"].Value));
            foreach (string owner in postOwners)
            {
                string[] ownedRows = postRows.Where(row =>
                    row.Contains("[UberAsciiFont(" + owner + ")]") ||
                    row.Contains("[Sub(" + owner + ")]") ||
                    row.Contains("[SubToggle(" + owner + ", _)]")).ToArray();
                Assert.That(ownedRows, Is.Not.Empty, owner);
                StringAssert.Contains("[Title(" + owner + ", _)]", ownedRows[0],
                    owner);
            }

            foreach (string obsoleteProperty in new[]
                     {
                         "_PixelationEnabled", "_ColorAdjustEnabled",
                         "_ColorScreenBlendEnabled", "_OrderedDitherEnabled",
                         "_ColorQuantizationEnabled",
                     })
                StringAssert.DoesNotContain(obsoleteProperty, postProperties);

            string postGui = Read(EditorDirectory + "UberShaderGUI.cs");
            StringAssert.Contains(
                "public sealed class UberPostFilterDrawer : LWGUI.SubDrawer",
                postGui);
            StringAssert.Contains(
                "public UberPostFilterDrawer(string group) : base(group)", postGui);
            StringAssert.Contains("foreach (Object target in property.targets)",
                postGui);
            StringAssert.Contains("SetPostFilterVisibility", postGui);
            StringAssert.Contains(
                "public sealed class UberAsciiFontDrawer : LWGUI.SubDrawer",
                postGui);
            StringAssert.Contains("typeof(TMP_FontAsset)", postGui);
            StringAssert.Contains("Undo.RecordObjects(targets", postGui);
            StringAssert.Contains(AsciiRamp, postGui);

            Match uiCbuffer = Regex.Match(Read(UberDirectory + "UberUI.hlsl"),
                @"(?s)CBUFFER_START\(UnityPerMaterial\)(?<body>.*?)CBUFFER_END");
            Assert.That(uiCbuffer.Success, Is.True);
            foreach (string proxy in new[] { "_SurfaceInputs", "_StencilOptions" })
            {
                Assert.That(Regex.Matches(uiCbuffer.Groups["body"].Value,
                    @"(?<![A-Za-z0-9_])" + Regex.Escape(proxy) +
                    @"(?![A-Za-z0-9_])").Count, Is.EqualTo(1), proxy);
            }
        }

        [Test]
        public void DisabledEffectsKeepTheirFoldoutParametersInspectable()
        {
            foreach (string path in new[]
                     {
                         UberDirectory + "Uber3D.shader",
                         UberDirectory + "UberSprite.shader",
                         UberDirectory + "UberUI.shader",
                     })
            {
                Match propertiesMatch = Regex.Match(Read(path),
                    @"(?s)\bProperties\s*\{(?<body>.*?)\r?\n\s*\}\s*\r?\n\s*SubShader\b");
                Assert.That(propertiesMatch.Success, Is.True, path);
                string properties = propertiesMatch.Groups["body"].Value;
                MatchCollection effectMains = Regex.Matches(properties,
                    @"\[Main\(\s*(?<group>[^,\)]+)\s*,\s*" +
                    @"(?<keyword>_[A-Z][A-Z0-9_]*)\s*,\s*on\s*\)\]");
                Assert.That(effectMains, Is.Not.Empty, path);

                foreach (Match effectMain in effectMains)
                {
                    string group = effectMain.Groups["group"].Value;
                    string keyword = effectMain.Groups["keyword"].Value;
                    int sectionStart = effectMain.Index + effectMain.Length;
                    int sectionEnd = properties.IndexOf("[Main(", sectionStart,
                        StringComparison.Ordinal);
                    if (sectionEnd < 0)
                        sectionEnd = properties.Length;
                    string section = properties.Substring(sectionStart,
                        sectionEnd - sectionStart);

                    Assert.That(Regex.IsMatch(section,
                            @"\[Title\(\s*" + Regex.Escape(group) +
                            @"\s*,\s*_\s*\)\]"),
                        Is.True, path + ": " + group);
                    StringAssert.DoesNotContain(group + keyword, section,
                        path + ": inactive " + keyword +
                        " must not hide the open " + group + " foldout");
                }
            }
        }

        [Test]
        public void InspectorHotPathsAvoidOwnedRepaintArraysAndKeywordWritesNoOp()
        {
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            Match layout = Regex.Match(gui,
                @"(?s)internal static class UberDrawerLayout.*?" +
                @"(?=public sealed class UberVector2Drawer)");
            Match vector2 = Regex.Match(gui,
                @"(?s)public sealed class UberVector2Drawer.*?" +
                @"(?=public sealed class UberVector3Drawer)");
            Match vector3 = Regex.Match(gui,
                @"(?s)public sealed class UberVector3Drawer.*?" +
                @"(?=public sealed class UberMinMaxVectorDrawer)");
            Match minMax = Regex.Match(gui,
                @"(?s)public sealed class UberMinMaxVectorDrawer.*?" +
                @"(?=public sealed class UberGradientDrawer)");
            foreach (Match section in new[] { layout, vector2, vector3, minMax })
            {
                Assert.That(section.Success, Is.True);
                StringAssert.DoesNotContain("float[]", section.Value);
            }

            Assert.That(Regex.IsMatch(layout.Value,
                @"internal static Vector4 DrawFloatComponents\s*\(\s*" +
                @"Rect position,\s*GUIContent\[\] labels,\s*Vector4 values,\s*" +
                @"int componentCount,"), Is.True);
            Type layoutType = typeof(UberShaderGUI).Assembly.GetType(
                "UberDrawerLayout");
            Assert.That(layoutType, Is.Not.Null);
            MethodInfo drawComponents = layoutType.GetMethod(
                "DrawFloatComponents",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(drawComponents, Is.Not.Null);
            Assert.That(drawComponents.ReturnType, Is.EqualTo(typeof(Vector4)));
            CollectionAssert.AreEqual(new[]
            {
                typeof(Rect), typeof(GUIContent[]), typeof(Vector4), typeof(int),
                typeof(float),
            }, drawComponents.GetParameters().Select(parameter =>
                parameter.ParameterType).ToArray());
            StringAssert.Contains(
                "Mathf.Min(Mathf.Clamp(componentCount, 0, 4),", layout.Value);
            StringAssert.Contains(
                "for (int index = 0; index < componentCount; ++index)",
                layout.Value);
            Match componentLoop = Regex.Match(layout.Value,
                @"(?s)for \(int index = 0; index < componentCount; \+\+index\)" +
                @"\s*\{(?<body>.*?)\n            \}");
            Assert.That(componentLoop.Success, Is.True);
            Assert.That(Regex.Matches(layout.Value,
                @"values\s*\[[^\]]+\]\s*=").Count, Is.EqualTo(1));
            Assert.That(Regex.Matches(componentLoop.Groups["body"].Value,
                @"values\s*\[index\]\s*=\s*EditorGUI\.FloatField").Count,
                Is.EqualTo(1));
            Assert.That(Regex.Matches(layout.Value,
                @"\breturn\s+values\s*;").Count, Is.EqualTo(2));
            StringAssert.Contains("ComponentLabels, storedValue, 2, 16.0f",
                vector2.Value);
            StringAssert.Contains("ComponentLabels, storedValue, 3, 16.0f",
                vector3.Value);
            StringAssert.Contains("ComponentLabels, storedValue, 2, 32.0f",
                minMax.Value);
            foreach (Match drawer in new[] { vector2, vector3, minMax })
            {
                StringAssert.Contains(
                    "Vector4 storedValue = property.vectorValue", drawer.Value);
                StringAssert.Contains("property.vectorValue = editedValue",
                    drawer.Value);
            }
            StringAssert.Contains(
                "private static readonly GUIContent CurrentBaseRadiusLabel",
                minMax.Value);
            Assert.That(Regex.Matches(minMax.Value,
                "new GUIContent\\(\"Current Base Radius\"\\)").Count,
                Is.EqualTo(1));
            StringAssert.Contains(
                "UberDrawerLayout.DrawPropertyLabel(row,\n            CurrentBaseRadiusLabel)",
                minMax.Value.Replace("\r\n", "\n"));

            StringAssert.Contains("private readonly struct PostFilterOption", gui);
            StringAssert.Contains(
                "private static readonly PostFilterOption[] PostFilterOptions", gui);
            StringAssert.DoesNotContain("PostFilterKeywords", gui);
            Match postDrawer = Regex.Match(gui,
                @"(?s)public sealed class UberPostFilterDrawer.*?" +
                @"(?=public sealed class UberAsciiFontDrawer)");
            Assert.That(postDrawer.Success, Is.True);
            StringAssert.Contains("UberShaderGUI.CreatePostFilterLabels()",
                postDrawer.Value);
            Assert.That(Regex.Matches(postDrawer.Value,
                @"new GUIContent\(").Count, Is.Zero);

            Match synchronizePost = Regex.Match(gui,
                @"(?s)internal static void SynchronizePostFilter\s*\(.*?" +
                @"(?=\n    internal static void SetPostFilterVisibility)");
            Assert.That(synchronizePost.Success, Is.True);
            StringAssert.Contains(
                "SetKeyword(material, PostFilterOptions[index].Keyword, index == mode)",
                synchronizePost.Value);
            StringAssert.DoesNotContain("material.EnableKeyword",
                synchronizePost.Value);
            StringAssert.DoesNotContain("material.DisableKeyword",
                synchronizePost.Value);

            Match setKeyword = Regex.Match(gui,
                @"(?s)private static void SetKeyword\s*\(Material material, " +
                @"string keyword, bool enabled\)\s*\{(?<body>.*?)\n    \}");
            Assert.That(setKeyword.Success, Is.True);
            string body = setKeyword.Groups["body"].Value;
            Assert.That(Regex.IsMatch(body,
                @"if\s*\(\s*material\.IsKeywordEnabled\(keyword\)\s*==\s*" +
                @"enabled\s*\)\s*return\s*;"), Is.True);
            int guard = body.IndexOf(
                "material.IsKeywordEnabled(keyword) == enabled",
                StringComparison.Ordinal);
            int enable = body.IndexOf("material.EnableKeyword(keyword)",
                StringComparison.Ordinal);
            int disable = body.IndexOf("material.DisableKeyword(keyword)",
                StringComparison.Ordinal);
            Assert.That(guard, Is.GreaterThanOrEqualTo(0));
            Assert.That(enable, Is.GreaterThan(guard));
            Assert.That(disable, Is.GreaterThan(guard));
        }

        [Test]
        public void KeywordPragmasUseReviewedLocalPolicyAndGlobalAllowList()
        {
            string source = string.Join("\n", Shaders.Select(item => Read(item.Path)));
            foreach (string keyword in EffectKeywords)
            {
                string[] rows = PragmaRows(source, keyword);
                Assert.That(rows, Is.Not.Empty, keyword);
                Assert.That(rows.All(row => row.Contains("shader_feature_local")),
                    Is.True, keyword + ": " + string.Join(" | ", rows));
            }

            foreach (string keyword in StructuralKeywords)
            {
                string[] rows = PragmaRows(source, keyword);
                Assert.That(rows, Is.Not.Empty, keyword);
                Assert.That(rows.All(row => row.Contains("multi_compile_local")),
                    Is.True, keyword + ": " + string.Join(" | ", rows));
            }

            string[] globalFeatureRows = Lines(source).Where(line =>
                    line.StartsWith("#pragma shader_feature", StringComparison.Ordinal) &&
                    !line.Contains("shader_feature_local"))
                .ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature EDITOR_VISUALIZATION",
            }, globalFeatureRows, "Unreviewed non-local shader_feature row");

            foreach (string row in Lines(source).Where(line =>
                         line.StartsWith("#pragma multi_compile", StringComparison.Ordinal) &&
                         !line.Contains("multi_compile_local")))
            {
                Assert.That(EffectKeywords.Any(keyword => ContainsToken(row, keyword)),
                    Is.False, "Effect keyword on global row: " + row);
                Match match = Regex.Match(row,
                    @"^#pragma\s+multi_compile(?:_[A-Za-z]+)?\s*(?<tokens>.*)$");
                Assert.That(match.Success, Is.True, row);
                foreach (string token in match.Groups["tokens"].Value.Split(
                             new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token == "_")
                        continue;
                    Assert.That(AllowedGlobalKeywords.Contains(token), Is.True,
                        "Unreviewed global keyword " + token + " on " + row);
                }
            }
        }

        [Test]
        public void MaterialCbuffersAreUnconditionalAndCommonIncludeIsPure()
        {
            string[,] pairs =
            {
                { "Uber3D.shader", "Uber3D.hlsl" },
                { "UberSprite.shader", "UberSprite.hlsl" },
                { "UberUI.shader", "UberUI.hlsl" },
                { "UberPostProcessing.shader", "UberPostProcessing.hlsl" },
                { "UberParticle.shader", "UberParticle.hlsl" },
            };
            Regex propertyPattern = new Regex(
                @"(?m)^\s*(?:\[[^\]]+\]\s*)*(?<name>_[A-Za-z0-9]+)\s*" +
                @"\([^\r\n]*?,\s*(?:Float|Color|Vector|Range\([^\)]*\))\s*\)\s*=");

            for (int index = 0; index < pairs.GetLength(0); ++index)
            {
                string shaderSource = Read(UberDirectory + pairs[index, 0]);
                string hlslSource = Read(UberDirectory + pairs[index, 1]);
                Match cbuffer = Regex.Match(hlslSource,
                    @"(?s)CBUFFER_START\(UnityPerMaterial\)(?<body>.*?)CBUFFER_END");
                Assert.That(cbuffer.Success, Is.True, pairs[index, 1]);
                string body = cbuffer.Groups["body"].Value;
                Assert.That(Regex.IsMatch(body, @"#\s*(?:if|ifdef|ifndef)"),
                    Is.False, pairs[index, 1]);

                string[] properties = propertyPattern.Matches(shaderSource)
                    .Cast<Match>().Select(match => match.Groups["name"].Value)
                    .Distinct().ToArray();
                Assert.That(properties, Is.Not.Empty, pairs[index, 0]);
                foreach (string property in properties)
                {
                    Assert.That(Regex.IsMatch(body,
                            @"(?<![A-Za-z0-9_])" + Regex.Escape(property) +
                            @"(?![A-Za-z0-9_])"),
                        Is.True, pairs[index, 1] + " missing " + property);
                }
            }

            string common = Read(UberDirectory + "UberCommon.hlsl");
            Assert.That(Regex.IsMatch(common,
                @"\b(?:TEXTURE[A-Za-z0-9_]*|SAMPLER|CBUFFER_START)\s*\("), Is.False);
            Assert.That(common.Contains("SurfaceData"), Is.False);
            Assert.That(Regex.IsMatch(common, @"\b_(?:BaseMap|MainTex|DissolveTex)\b"),
                Is.False);
        }

        [Test]
        public void CommonHlslOwnsReviewedFormulasAndSurfaceWrappersDelegate()
        {
            string common = Read(UberDirectory + "UberCommon.hlsl");
            string objectHlsl = Read(UberDirectory + "Uber3D.hlsl");
            string spriteHlsl = Read(UberDirectory + "UberSprite.hlsl");
            string uiHlsl = Read(UberDirectory + "UberUI.hlsl");
            string particleHlsl = Read(UberDirectory + "UberParticle.hlsl");
            string surfaceSources = string.Join("\n", new[]
            {
                objectHlsl, spriteHlsl, uiHlsl, particleHlsl,
            });

            string[,] commonOracles =
            {
                { "UberHash21", @"inline float UberHash21(float2 value)
{
    return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453);
}" },
                { "UberEvaluateGlitchBandBoundary", @"inline float UberEvaluateGlitchBandBoundary(float boundaryIndex, float frame,
    float averageBandSize, float bandSizeVariation)
{
    float jitter = UberHash21(float2(boundaryIndex + 83.17,
        frame + 29.41)) - 0.5;
    return boundaryIndex * averageBandSize +
        jitter * bandSizeVariation * 0.5;
}" },
                { "UberValueNoise1D", @"inline float UberValueNoise1D(float coordinate)
{
    float cell = floor(coordinate);
    float blend = frac(coordinate);
    blend = blend * blend * (3.0 - 2.0 * blend);
    float first = frac(sin(cell * 12.9898) * 43758.5453);
    float second = frac(sin((cell + 1.0) * 12.9898) * 43758.5453);
    return lerp(first, second, blend);
}" },
                { "UberSafeNormalizeFinite3", @"inline float3 UberSafeNormalizeFinite3(float3 value, float3 fallback)
{
    float lengthSquared = dot(value, value);
    if (!(lengthSquared > 0.000001) || lengthSquared > 1.0e20)
        return fallback;
    return value * rsqrt(lengthSquared);
}" },
                { "UberEvaluateGradient4Keys", @"inline half4 UberEvaluateGradient4Keys(float time, float4 color0,
    float4 color1, float4 color2, float4 color3, float4 alphas,
    float4 alphaTimes, float4 metadata)
{
    float3 color = color0.rgb;
    color = lerp(color, color1.rgb,
        UberSafeInverseLerp(color0.a, color1.a, time) *
        step(1.5, metadata.x));
    color = lerp(color, color2.rgb,
        UberSafeInverseLerp(color1.a, color2.a, time) *
        step(2.5, metadata.x));
    color = lerp(color, color3.rgb,
        UberSafeInverseLerp(color2.a, color3.a, time) *
        step(3.5, metadata.x));

    float alpha = alphas.x;
    alpha = lerp(alpha, alphas.y,
        UberSafeInverseLerp(alphaTimes.x, alphaTimes.y, time) *
        step(1.5, metadata.y));
    alpha = lerp(alpha, alphas.z,
        UberSafeInverseLerp(alphaTimes.y, alphaTimes.z, time) *
        step(2.5, metadata.y));
    alpha = lerp(alpha, alphas.w,
        UberSafeInverseLerp(alphaTimes.z, alphaTimes.w, time) *
        step(3.5, metadata.y));
    return half4(color, alpha);
}" },
            };
            for (int index = 0; index < commonOracles.GetLength(0); ++index)
            {
                string helper = commonOracles[index, 0];
                Assert.That(Regex.Matches(common,
                    @"(?m)^\s*inline\s+[^\r\n]+\b" + helper + @"\s*\(").Count,
                    Is.EqualTo(1), helper);
                Assert.That(Regex.Matches(surfaceSources,
                    @"(?m)^\s*inline\s+[^\r\n]+\b" + helper + @"\s*\(").Count,
                    Is.Zero, helper + " must remain common-owned");
                AssertNormalizedShaderSource(InlineFunctionSource(common, helper),
                    commonOracles[index, 1], helper + " canonical formula changed");
            }

            StringAssert.DoesNotContain("UberOrderedDitherMask",
                common + surfaceSources);
            StringAssert.Contains("UberBayer2x2", common);
            StringAssert.Contains("UberBayer4x4", common);

            string[,] wrapperBodies =
            {
                { objectHlsl, "UberGlitchHash", "return UberHash21(value);" },
                { objectHlsl, "UberGlitchBandBoundary",
                    "return UberEvaluateGlitchBandBoundary(boundaryIndex, frame, averageBandSize, bandSizeVariation);" },
                { objectHlsl, "UberGlitchSafeNormalize3",
                    "return UberSafeNormalizeFinite3(value, fallback);" },
                { objectHlsl, "UberGetHologramUpVector",
                    "return UberSafeNormalizeFinite3(_HologramObjectUpVector.xyz, float3(0.0, 1.0, 0.0));" },
                { objectHlsl, "UberHologramValueNoise",
                    "return UberValueNoise1D(coordinate);" },
                { spriteHlsl, "UberSpriteGlitchHash", "return UberHash21(value);" },
                { spriteHlsl, "UberSpriteGlitchBandBoundary",
                    "return UberEvaluateGlitchBandBoundary(boundaryIndex, frame, averageBandSize, bandSizeVariation);" },
                { spriteHlsl, "UberEvaluateDissolveEdgeGradient",
                    "return UberEvaluateGradient4Keys(time, _DissolveEdgeGradientColor0, _DissolveEdgeGradientColor1, _DissolveEdgeGradientColor2, _DissolveEdgeGradientColor3, _DissolveEdgeGradientAlphas, _DissolveEdgeGradientAlphaTimes, _DissolveEdgeGradientMetadata);" },
                { spriteHlsl, "UberGetHologramUpVector",
                    "return UberSafeNormalizeFinite3(_HologramObjectUpVector.xyz, float3(0.0, 1.0, 0.0));" },
                { spriteHlsl, "UberHologramValueNoise",
                    "return UberValueNoise1D(coordinate);" },
                { uiHlsl, "UberUIGlitchHash", "return UberHash21(value);" },
                { uiHlsl, "UberUIGlitchBandBoundary",
                    "return UberEvaluateGlitchBandBoundary(boundaryIndex, frame, averageBandSize, bandSizeVariation);" },
                { uiHlsl, "UberUIEvaluateDissolveEdgeGradient",
                    "return UberEvaluateGradient4Keys(time, _DissolveEdgeGradientColor0, _DissolveEdgeGradientColor1, _DissolveEdgeGradientColor2, _DissolveEdgeGradientColor3, _DissolveEdgeGradientAlphas, _DissolveEdgeGradientAlphaTimes, _DissolveEdgeGradientMetadata);" },
                { uiHlsl, "UberUIGetHologramUpVector",
                    "return UberSafeNormalizeFinite3(_HologramObjectUpVector.xyz, float3(0.0, 1.0, 0.0));" },
                { uiHlsl, "UberUIHologramValueNoise",
                    "return UberValueNoise1D(coordinate);" },
            };
            for (int index = 0; index < wrapperBodies.GetLength(0); ++index)
                AssertNormalizedShaderSource(InlineFunctionBody(
                        wrapperBodies[index, 0], wrapperBodies[index, 1]),
                    wrapperBodies[index, 2],
                    wrapperBodies[index, 1] + " must remain delegation-only");

            string particleGradient = InlineFunctionBody(particleHlsl,
                "UberParticleEvaluateLifetimeGradient");
            AssertNormalizedShaderSource(particleGradient,
                @"time = saturate(time);
return UberEvaluateGradient4Keys(time, _LifetimeGradientColor0,
    _LifetimeGradientColor1, _LifetimeGradientColor2,
    _LifetimeGradientColor3, _LifetimeGradientAlphas,
    _LifetimeGradientAlphaTimes, _LifetimeGradientMetadata);",
                "Particle must clamp age before delegating and do nothing else");
        }

        [Test]
        public void SurfacePassParityMetaAndGenericSpriteContractsRemainIntact()
        {
            string objectShader = Read(UberDirectory + "Uber3D.shader");
            string objectHlsl = Read(UberDirectory + "Uber3D.hlsl");
            string spriteShader = Read(UberDirectory + "UberSprite.shader");
            string spriteHlsl = Read(UberDirectory + "UberSprite.hlsl");
            string spriteBinder = Read(
                "Assets/01. Scripts/Rendering/UberSpritePropertyBinder.cs");

            Assert.That(Regex.Matches(objectShader,
                @"#pragma\s+fragment\s+UberSilhouetteFragment").Count, Is.EqualTo(2));
            Assert.That(Regex.Matches(objectShader,
                @"#pragma[^\r\n]*_HEIGHT_FADE_ON").Count, Is.EqualTo(5));
            Assert.That(Regex.Matches(objectHlsl,
                @"\bUberEvaluateSilhouette\s*\(").Count,
                Is.GreaterThanOrEqualTo(5));
            Assert.That(objectShader.Contains(
                "Tags { \"LightMode\" = \"SRPDefaultUnlit\" }"), Is.True);

            string meta = objectHlsl.Substring(
                objectHlsl.IndexOf("#elif defined(UBER_META_PASS)",
                    StringComparison.Ordinal));
            StringAssert.Contains("_UNLIT_ON", meta);
            StringAssert.Contains("_ALPHATEST_ON", meta);
            StringAssert.Contains("SampleEmission", meta);
            StringAssert.Contains("UnityMetaFragment", meta);
            StringAssert.Contains("EDITOR_VISUALIZATION", meta);
            StringAssert.DoesNotContain("UberEvaluateDissolve", meta);
            StringAssert.DoesNotContain("_DITHER_FADE_ON", meta);
            StringAssert.DoesNotContain("_STENCIL_OUTLINE_ON", meta);

            Assert.That(Regex.Matches(spriteShader,
                @"#pragma\s+fragment\s+UberSpriteSilhouetteFragment").Count,
                Is.EqualTo(2));
            Assert.That(Regex.Matches(spriteHlsl,
                @"\bUberEvaluateSpriteSilhouette\s*\(").Count,
                Is.GreaterThanOrEqualTo(4));
            foreach (string pass in new[] { "UniversalForward", "Universal2D",
                         "NormalsRendering", "ShadowCaster", "DepthOnly", "DepthNormals" })
                StringAssert.Contains("Name \"" + pass + "\"", spriteShader);

            string genericSource = spriteShader + spriteHlsl + spriteBinder;
            StringAssert.DoesNotContain("CardBlend", genericSource);
            StringAssert.DoesNotContain("_Card", genericSource);
            StringAssert.Contains("_SECONDARY_LAYER_ON", genericSource);
            StringAssert.Contains("_SecondaryTex", genericSource);
            StringAssert.Contains("_SecondaryUVRect", genericSource);
            StringAssert.Contains("MaterialPropertyBlock", spriteBinder);
            StringAssert.DoesNotContain("new Material(", spriteBinder);
            Assert.That(spriteHlsl.IndexOf(
                    "color = lerp(color, secondary, saturate(_SecondaryBlendAmount));",
                    StringComparison.Ordinal),
                Is.LessThan(spriteHlsl.IndexOf("albedo = UberAdjustColor",
                    StringComparison.Ordinal)));

            foreach (string source in new[] { objectShader, spriteShader })
            {
                string[] rows = PragmaRows(source, "_UNLIT_ON");
                Assert.That(rows, Is.Not.Empty);
                Assert.That(rows.All(row => row.Contains("multi_compile_local")),
                    Is.True, string.Join(" | ", rows));
            }
        }

        [Test]
        public void UiAndPostSourcesPreserveReviewedPlatformContracts()
        {
            string uiShader = Read(UberDirectory + "UberUI.shader");
            string uiHlsl = Read(UberDirectory + "UberUI.hlsl");
            string gui = Read("Assets/05. Arts/Shader/Editor/UberShaderGUI.cs");
            string variants = Read(VariantPath);
            string uiSources = uiShader + uiHlsl + gui + variants;
            StringAssert.DoesNotContain("_UV_FADE_RADIAL", uiSources);
            StringAssert.DoesNotContain("_UVFadeMode", uiSources);
            StringAssert.Contains("_DISSOLVE_RADIAL", uiShader);
            foreach (string contract in new[]
                     {
                         "_StencilComp", "_Stencil", "_StencilOp",
                         "_StencilWriteMask", "_StencilReadMask", "_ColorMask",
                         "ZTest [unity_GUIZTestMode]", "Cull Off", "ZWrite Off",
                         "Blend SrcAlpha OneMinusSrcAlpha", "UNITY_UI_CLIP_RECT",
                         "UNITY_UI_ALPHACLIP", "CanUseSpriteAtlas",
                     })
                StringAssert.Contains(contract, uiShader);
            StringAssert.Contains("_UIMaskSoftnessX", uiHlsl);
            StringAssert.Contains("_UIMaskSoftnessY", uiHlsl);
            StringAssert.Contains("UberClampUV", uiHlsl);

            string postShader = Read(UberDirectory + "UberPostProcessing.shader");
            string postHlsl = Read(UberDirectory + "UberPostProcessing.hlsl");
            StringAssert.Contains(
                "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl",
                postShader);
            StringAssert.Contains("UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX", postHlsl);
            StringAssert.Contains("sampler_PointClamp", postHlsl);
            StringAssert.Contains("sampler_LinearClamp", postHlsl);
            StringAssert.Contains("float sourceAlpha = source.a;", postHlsl);
            StringAssert.Contains("return float4(source.rgb, sourceAlpha);", postHlsl);
            Assert.That(postHlsl.IndexOf("UberPostPixelGridUV", StringComparison.Ordinal),
                Is.LessThan(postHlsl.IndexOf("UberPostSampleSource", StringComparison.Ordinal)));
            foreach (string contract in new[]
                     {
                         "UberPostGradientMap", "UberPostOldFilm",
                         "UberPostEdgeInk", "UberPostAscii",
                         "float midpoint = clamp(_GradientMidpoint, 0.0001, 0.9999);",
                         "float2 tap = max(abs(_BlitTexture_TexelSize.xy)",
                         "float feather = max(fwidth(magnitude), 0.0001);",
                         "float2 cellCenterUV = saturate(",
                         "#if defined(_COLOR_ADJUST_ON)",
                         "#elif defined(_GRADIENT_MAP_ON)",
                         "#elif defined(_OLD_FILM_ON)",
                         "#elif defined(_EDGE_FILTER_ON)",
                         "#elif defined(_ASCII_FILTER_ON)",
                     })
                StringAssert.Contains(contract, postHlsl);
            foreach (string forbiddenInput in new[]
                     {
                         "_CameraDepthTexture", "_CameraNormalsTexture",
                         "DeclareDepthTexture", "DeclareNormalsTexture",
                     })
                StringAssert.DoesNotContain(forbiddenInput, postShader + postHlsl);

            foreach (string rendererPath in new[]
                     {
                         "Assets/Settings/PC_Renderer.asset",
                         "Assets/Settings/Mobile_Renderer.asset",
                     })
            {
                string renderer = Read(rendererPath);
                MatchCollection names = Regex.Matches(renderer,
                    @"(?m)^\s*m_Name:\s+Uber Post Processing\s*$");
                Assert.That(names.Count, Is.EqualTo(1), rendererPath);
                Match block = Regex.Match(renderer,
                    @"(?s)m_Name:\s+Uber Post Processing(?<body>.*?)(?=--- !u!|\z)");
                Assert.That(block.Success, Is.True, rendererPath);
                Match activation = Regex.Match(block.Groups["body"].Value,
                    @"(?m)^\s*m_Active:\s*(?<value>[01])\s*$");
                Assert.That(activation.Success, Is.True, rendererPath);
                TestContext.Progress.WriteLine(rendererPath +
                    " deployment m_Active=" + activation.Groups["value"].Value);
                StringAssert.Contains("injectionPoint: 550", block.Groups["body"].Value);
                StringAssert.Contains("fetchColorBuffer: 1", block.Groups["body"].Value);
                StringAssert.Contains("requirements: 0", block.Groups["body"].Value);
                StringAssert.Contains("passIndex: 0", block.Groups["body"].Value);
                StringAssert.Contains("guid: " + PostMaterialGuid,
                    block.Groups["body"].Value);
            }

            string material = Read(
                "Assets/05. Arts/Material/UberPostProcessing.mat");
            foreach (string property in new[]
                     {
                         "_ScreenFilterMode", "_ScreenFilterOptions",
                         "_GradientMidpoint",
                         "_GradientStrength", "_OldFilmSepia", "_OldFilmGrain",
                         "_OldFilmScratch", "_OldFilmFlicker", "_OldFilmJitter",
                         "_OldFilmVignette", "_EdgeThreshold", "_EdgeWidth",
                         "_EdgeStrength", "_EdgeSourceMix", "_AsciiCellSize",
                         "_AsciiSourceColor", "_AsciiInvert", "_AsciiFontReady",
                         "_AsciiSdfThreshold", "_AsciiSdfSoftness",
                     })
                StringAssert.Contains("- " + property + ":", material);
            Material postMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                PostMaterialPath);
            Assert.That(postMaterial, Is.Not.Null);
            float filterModeValue = postMaterial.GetFloat("_ScreenFilterMode");
            Assert.That(float.IsNaN(filterModeValue) ||
                float.IsInfinity(filterModeValue), Is.False);
            int filterMode = Mathf.Clamp(Mathf.RoundToInt(filterModeValue), 0,
                PostFilterKeywords.Length - 1);
            Assert.That(filterModeValue, Is.EqualTo(filterMode));
            string[] enabledFilters = PostFilterKeywords.Skip(1)
                .Where(postMaterial.IsKeywordEnabled).ToArray();
            CollectionAssert.AreEquivalent(filterMode == 0
                ? Array.Empty<string>()
                : new[] { PostFilterKeywords[filterMode] }, enabledFilters);
            TestContext.Progress.WriteLine("Post material deployment filter mode=" +
                filterMode + ", keywords=" + string.Join(",", enabledFilters));
            StringAssert.Contains("fileID: -3501735882904893123, guid: " +
                PixelifyFontGuid, material);
            for (int index = 0; index < AsciiRamp.Length; ++index)
            {
                StringAssert.Contains("- _AsciiGlyphUV" + index + ":", material);
                StringAssert.Contains("- _AsciiGlyphPlacement" + index + ":",
                    material);
            }
            foreach (string obsoleteProperty in new[]
                     {
                         "_PixelationEnabled", "_ColorAdjustEnabled",
                         "_ColorScreenBlendEnabled", "_OrderedDitherEnabled",
                         "_ColorQuantizationEnabled",
                     })
                StringAssert.DoesNotContain(obsoleteProperty, material);
        }

        [Test]
        public void PostFilterImplementationIsColorOnlyAndReferenceIsolated()
        {
            string[] revisionPaths =
            {
                UberDirectory + "UberPostProcessing.shader",
                UberDirectory + "UberPostProcessing.hlsl",
                EditorDirectory + "UberShaderGUI.cs",
                PostMaterialPath,
                VariantPath,
                "Assets/Tests/EditMode/Rendering/UberShaderSuiteTests.cs",
            };
            string forbiddenReferenceName = "Null" + "Tale";
            string forbiddenHost = "github" + ".com";
            string forbiddenNotice = "copy" + "right";
            foreach (string path in revisionPaths)
            {
                string source = Read(path);
                StringAssert.DoesNotContain(forbiddenReferenceName, source, path);
                StringAssert.DoesNotContain(forbiddenHost, source, path);
                StringAssert.DoesNotContain(forbiddenNotice,
                    source.ToLowerInvariant(), path);
            }

            string postShader = Read(UberDirectory + "UberPostProcessing.shader");
            string postHlsl = Read(UberDirectory + "UberPostProcessing.hlsl");
            Assert.That(Regex.Matches(postShader, @",\s*2D\s*\)").Count,
                Is.EqualTo(1));
            StringAssert.Contains("_AsciiFontAtlas(\"Font Asset\", 2D)",
                postShader);
            CollectionAssert.AreEqual(new[]
            {
                "TEXTURE2D(_AsciiFontAtlas)",
                "SAMPLER(sampler_AsciiFontAtlas)",
            }, Regex.Matches(postHlsl,
                    @"\b(?:TEXTURE2D|TEXTURE2D_X|SAMPLER)\s*\([^\)]+\)")
                .Cast<Match>().Select(match => match.Value).ToArray());
            foreach (string forbiddenInput in new[]
                     {
                         "DepthTexture", "NormalsTexture", "LoadSceneDepth",
                         "SampleSceneDepth", "SampleSceneNormals",
                     })
                StringAssert.DoesNotContain(forbiddenInput, postShader + postHlsl);

            string[] includes = Regex.Matches(postShader + postHlsl,
                    @"#include\s+""(?<path>[^""]+)""").Cast<Match>()
                .Select(match => match.Groups["path"].Value).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl",
                "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl",
                "UberPostProcessing.hlsl",
                "UberCommon.hlsl",
            }, includes);
        }

        [Test]
        public void GenericAssetMigrationPreservesGuidsReferencesAndBrandFreeSources()
        {
            string forbiddenBrand = "N" + "HN";
            string duplicatedPrefix = "Uber" + "Uber";
            for (int index = 0; index < RenamedAssets.GetLength(0); ++index)
            {
                string path = RenamedAssets[index, 0];
                string expectedGuid = RenamedAssets[index, 1];
                StringAssert.DoesNotContain(forbiddenBrand, Path.GetFileName(path), path);
                StringAssert.DoesNotContain(duplicatedPrefix, Path.GetFileName(path), path);

                string source = Read(path);
                StringAssert.DoesNotContain(forbiddenBrand, source, path);
                StringAssert.DoesNotContain(duplicatedPrefix, source, path);
                StringAssert.Contains("guid: " + expectedGuid, Read(path + ".meta"), path);
                Assert.That(AssetDatabase.AssetPathToGUID(path),
                    Is.EqualTo(expectedGuid), path);
                Assert.That(AssetDatabase.GUIDToAssetPath(expectedGuid),
                    Is.EqualTo(path), expectedGuid);
            }

            foreach (string directory in new[] { UberDirectory, EditorDirectory })
            {
                foreach (string path in Directory.GetFiles(directory, "*",
                             SearchOption.TopDirectoryOnly))
                {
                    StringAssert.DoesNotContain(forbiddenBrand,
                        Path.GetFileName(path), path);
                    StringAssert.DoesNotContain(duplicatedPrefix,
                        Path.GetFileName(path), path);
                }
            }

            string legacyMaterial = MaterialDirectory + forbiddenBrand +
                "UberPostProcessing.mat";
            Assert.That(File.Exists(legacyMaterial), Is.False);
            Assert.That(File.Exists(legacyMaterial + ".meta"), Is.False);
            StringAssert.Contains("m_Name: UberShaderVariants", Read(VariantPath));
            StringAssert.Contains("m_Name: UberPostProcessing", Read(PostMaterialPath));

            foreach (string rendererPath in new[]
                     {
                         "Assets/Settings/PC_Renderer.asset",
                         "Assets/Settings/Mobile_Renderer.asset",
                     })
                StringAssert.Contains("guid: " + PostMaterialGuid, Read(rendererPath));
            StringAssert.Contains("guid: " + VariantGuid,
                Read("ProjectSettings/GraphicsSettings.asset"));
        }

        [Test]
        public void VariantCollectionContainsExactlyReviewedWhitelist()
        {
            IReadOnlyList<UberShaderVariantSpec> rows =
                UberShaderVariantManifest.Rows;
            ShaderVariantCollection collection =
                AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(VariantPath);
            Assert.That(collection, Is.Not.Null);
            UberShaderVariantManifest.ValidateRows(rows);
            UberShaderVariantCollectionGenerator.ValidateCollection(collection,
                rows, "Reviewed live collection");
            Assert.That(rows.Count, Is.EqualTo(112));
            UberShaderVariantSpec[] particleRows = rows.Where(item =>
                item.ShaderName == ParticleShaderName).ToArray();
            Assert.That(particleRows.Length, Is.EqualTo(18));
            Assert.That(rows.Count - particleRows.Length, Is.EqualTo(94));
            Assert.That(particleRows.All(item =>
                item.PassType == PassType.ScriptableRenderPipeline), Is.True);
            Assert.That(particleRows.Select(item => string.Join(" ", item.Keywords))
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(18));
            Assert.That(rows.Count(item =>
                item.PassType == PassType.ScriptableRenderPipeline), Is.EqualTo(72));
            Assert.That(rows.Count(item =>
                item.PassType == PassType.ScriptableRenderPipelineDefaultUnlit),
                Is.EqualTo(29));
            Assert.That(rows.Count(item => item.PassType == PassType.Normal),
                Is.EqualTo(11));
            Assert.That(rows.Any(item => item.PassType == PassType.ShadowCaster ||
                item.PassType == PassType.Meta), Is.False);

            Assert.That(rows.Count(item => item.RequiresUncheckedConstruction),
                Is.EqualTo(1));
            foreach (UberShaderVariantSpec item in rows)
            {
                Assert.That(item.Keywords.Distinct(StringComparer.Ordinal).Count(),
                    Is.EqualTo(item.Keywords.Length),
                    item.ShaderName + " keywords contain a duplicate");
                Assert.That(collection.Contains(item.ToVariant()), Is.True,
                    item.ShaderName + " [" + string.Join(" ", item.Keywords) + "]");
            }
            UberShaderVariantSpec mixedStageParticle = new UberShaderVariantSpec(
                ParticleShaderName,
                PassType.ScriptableRenderPipeline, "_CUSTOM_DATA_ON",
                "_UV_DISTORTION_ON", "_VERTEX_OFFSET_ON");
            Assert.That(mixedStageParticle.RequiresUncheckedConstruction, Is.True);
            Assert.Throws<ArgumentException>(() =>
                new ShaderVariantCollection.ShaderVariant(
                    Shader.Find(mixedStageParticle.ShaderName),
                    mixedStageParticle.PassType, mixedStageParticle.Keywords));
            Assert.That(collection.Contains(mixedStageParticle.ToVariant()), Is.True);

            Assert.That(collection.Contains(new UberShaderVariantSpec(ObjectShaderName,
                PassType.ScriptableRenderPipeline, "_COLOR_ADJUST_ON",
                "_GLASS_GLOW_ON").ToVariant()), Is.False);
            Assert.That(collection.Contains(new UberShaderVariantSpec(SpriteShaderName,
                PassType.ScriptableRenderPipeline, "_PIXEL_OUTLINE_ON",
                "_SECONDARY_LAYER_ON").ToVariant()), Is.False);
            Assert.That(collection.Contains(new UberShaderVariantSpec(SpriteShaderName,
                PassType.ScriptableRenderPipeline, "_RIM_MULTIPLY",
                "_RIM_ON").ToVariant()), Is.True);
            Assert.That(collection.Contains(new UberShaderVariantSpec(UIShaderName,
                PassType.ScriptableRenderPipelineDefaultUnlit, "_PIXEL_OUTLINE_ON",
                "_RGB_OVERRIDE_ON").ToVariant()), Is.False);
            for (int first = 1; first < PostFilterKeywords.Length; ++first)
            {
                for (int second = first + 1;
                     second < PostFilterKeywords.Length; ++second)
                {
                    Assert.Throws<ArgumentException>(() =>
                        new UberShaderVariantSpec(PostShaderName, PassType.Normal,
                            PostFilterKeywords[first],
                            PostFilterKeywords[second]).ToVariant(),
                        PostFilterKeywords[first] + " + " +
                        PostFilterKeywords[second]);
                }
            }

            string yaml = Read(VariantPath);
            MatchCollection serialized = Regex.Matches(yaml,
                @"(?m)^\s*- keywords:\s*(?<keywords>[^\r\n]*)\r?\n" +
                @"\s*passType:\s*(?<pass>\d+)\s*$");
            Assert.That(serialized.Count, Is.EqualTo(rows.Count));
            for (int index = 0; index < rows.Count; ++index)
            {
                Assert.That(serialized[index].Groups["keywords"].Value.Trim(),
                    Is.EqualTo(string.Join(" ", rows[index].Keywords)),
                    "Serialized whitelist index " + index);
                Assert.That(int.Parse(serialized[index].Groups["pass"].Value),
                    Is.EqualTo((int)rows[index].PassType));
            }

            string[] guids = Regex.Matches(yaml,
                    @"first:\s*\{fileID:\s*4800000,\s*guid:\s*(?<guid>[0-9a-f]{32})")
                .Cast<Match>().Select(match => match.Groups["guid"].Value).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "30f88fdf99949b17e1187c24eba8ed93",
                "d03bad68e5f94df47a2c30a8822ea41c",
                "795b3814d0dfe9242829795ff0608656",
                "1aad80d3fa14854488c67ee35f470633",
                "23426744f12288344b3e94900b3f7cc9",
            }, guids);
        }

        [Test]
        public void VariantManifestGeneratorIsTransientFirstExplicitAndByteNoOp()
        {
            const string expectedCollectionHash =
                "CA000C6743C3D5D3A6FFD43D0C45F43E7822CC71F11DB7CC691ABA7E9DD11D72";
            const string expectedMetaHash =
                "C4AA07D520559A09F8246F5B8C539E2E5509D6B9777F01FDE56E9F00F1D48D31";
            IReadOnlyList<UberShaderVariantSpec> rows =
                UberShaderVariantManifest.Rows;
            string collectionHash = FileSha256(VariantPath);
            string metaHash = FileSha256(VariantPath + ".meta");
            Assert.That(collectionHash, Is.EqualTo(expectedCollectionHash));
            Assert.That(metaHash, Is.EqualTo(expectedMetaHash));
            ShaderVariantCollection live =
                AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(VariantPath);
            Assert.That(live, Is.Not.Null);
            string collectionGuid = AssetDatabase.AssetPathToGUID(VariantPath);
            bool liveDirty = EditorUtility.IsDirty(live);
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            int undoGroup = Undo.GetCurrentGroup();
            string undoGroupName = Undo.GetCurrentGroupName();

            ShaderVariantCollection candidate =
                UberShaderVariantCollectionGenerator.CreateValidatedCandidate();
            try
            {
                UberShaderVariantCollectionGenerator.ValidateCollection(candidate,
                    rows, "Test transient candidate");
                Assert.That(candidate.shaderCount, Is.EqualTo(5));
                Assert.That(candidate.variantCount, Is.EqualTo(112));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(candidate);
            }
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "after valid transient candidate");

            UberShaderVariantSpec[] invalidSubordinate = rows.ToArray();
            invalidSubordinate[11] = new UberShaderVariantSpec(ObjectShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_OBJECT_SPACE");
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "before invalid subordinate candidate");
            Assert.Throws<InvalidOperationException>(() =>
                UberShaderVariantCollectionGenerator.CreateValidatedCandidate(
                    invalidSubordinate));
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "after invalid subordinate candidate");
            UberShaderVariantSpec[] invalidPost = rows.ToArray();
            invalidPost[0] = new UberShaderVariantSpec(PostShaderName,
                PassType.Normal, "_ASCII_FILTER_ON", "_CRT_FILTER_ON");
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "before invalid post candidate");
            Assert.Throws<InvalidOperationException>(() =>
                UberShaderVariantCollectionGenerator.CreateValidatedCandidate(
                    invalidPost));
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "after invalid post candidate");
            UberShaderVariantSpec[] invalidSelector = rows.ToArray();
            invalidSelector[20] = new UberShaderVariantSpec(ObjectShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_OBJECT_SPACE",
                "_GLITCH_ON", "_GLITCH_WORLD_SPACE");
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "before invalid selector candidate");
            Assert.Throws<InvalidOperationException>(() =>
                UberShaderVariantCollectionGenerator.CreateValidatedCandidate(
                    invalidSelector));
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "after invalid selector candidate");

            UberShaderVariantCollectionGenerator.Verify();
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "after Verify");
            Assert.That(UberShaderVariantCollectionGenerator.Rebuild(), Is.False);
            AssertVariantCollectionState(live, collectionHash, metaHash,
                collectionGuid, liveDirty, activeTarget, undoGroup, undoGroupName,
                "after exact Rebuild");
            Assert.That(collectionGuid, Is.EqualTo(VariantGuid));

            string generator = Read(EditorDirectory +
                "UberShaderVariantCollectionGenerator.cs");
            Assert.That(Regex.Matches(generator, @"\[MenuItem\(").Count,
                Is.EqualTo(2));
            foreach (string forbidden in new[]
                     {
                         "InitializeOnLoad", "AssetPostprocessor",
                         "IPreprocessBuild", "DidReloadScripts", "WarmUp(",
                         ".Clear(", "SwitchActiveBuildTarget", "activeBuildTarget",
                         "Undo.", "ImportAsset", "Refresh", "SaveAssets",
                         "IProcessSceneWithReport", "IPostprocessBuildWithReport",
                     })
                StringAssert.DoesNotContain(forbidden, generator);
            string rebuild = CSharpMethodBody(generator,
                "internal static bool Rebuild()");
            int candidateIndex = rebuild.IndexOf(
                "ShaderVariantCollection candidate = CreateValidatedCandidate();",
                StringComparison.Ordinal);
            int loadIndex = rebuild.IndexOf(
                "ShaderVariantCollection live = LoadLive();", StringComparison.Ordinal);
            int guardIndex = rebuild.IndexOf(
                "if (IsExact(live, UberShaderVariantManifest.Rows))",
                StringComparison.Ordinal);
            int backupIndex = rebuild.IndexOf(
                "ShaderVariantCollection backup =", StringComparison.Ordinal);
            int copyIndex = rebuild.IndexOf(
                "EditorUtility.CopySerialized(candidate, live)", StringComparison.Ordinal);
            int dirtyIndex = rebuild.IndexOf("EditorUtility.SetDirty(live)",
                StringComparison.Ordinal);
            int saveIndex = rebuild.IndexOf("AssetDatabase.SaveAssetIfDirty(live)",
                StringComparison.Ordinal);
            Assert.That(new[] { candidateIndex, loadIndex, guardIndex, backupIndex,
                    copyIndex, dirtyIndex, saveIndex }, Has.All.GreaterThanOrEqualTo(0));
            Assert.That(candidateIndex, Is.LessThan(loadIndex));
            Assert.That(loadIndex, Is.LessThan(guardIndex));
            Assert.That(guardIndex, Is.LessThan(backupIndex));
            Assert.That(backupIndex, Is.LessThan(copyIndex));
            Assert.That(copyIndex, Is.LessThan(dirtyIndex));
            Assert.That(dirtyIndex, Is.LessThan(saveIndex));
            Assert.That(Regex.IsMatch(rebuild,
                @"if\s*\(IsExact\(live,\s*UberShaderVariantManifest\.Rows\)\)\s*return\s+false;"),
                Is.True, "The exact-live guard must return immediately.");
            Assert.That(Regex.IsMatch(rebuild,
                @"(?s)catch\s*\{\s*EditorUtility\.CopySerialized\(backup,\s*live\);\s*" +
                @"EditorUtility\.SetDirty\(live\);\s*" +
                @"AssetDatabase\.SaveAssetIfDirty\(live\);\s*throw;\s*\}"),
                Is.True, "Rollback must restore, dirty, save, then rethrow.");
            string tests = Read("Assets/Tests/EditMode/Rendering/" +
                "UberShaderSuiteTests.cs");
            StringAssert.DoesNotContain("private static readonly " + "VariantCase[]", tests);
            StringAssert.DoesNotContain("private readonly struct " + "VariantCase", tests);
        }

        [Test]
        public void GraphicsSettingsPreloadsCollectionOnceAndPreservesAlwaysIncluded()
        {
            string graphics = Read("ProjectSettings/GraphicsSettings.asset");
            Match always = Regex.Match(graphics,
                @"(?s)m_AlwaysIncludedShaders:\s*\r?\n(?<body>.*?)(?=\s*m_PreloadedShaders:)");
            string[] entries = Regex.Matches(always.Groups["body"].Value,
                    @"(?m)^\s*-\s*(?<entry>\{[^\r\n]+\})\s*$")
                .Cast<Match>().Select(match => match.Groups["entry"].Value).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "{fileID: 7, guid: 0000000000000000f000000000000000, type: 0}",
                "{fileID: 15104, guid: 0000000000000000f000000000000000, type: 0}",
                "{fileID: 15105, guid: 0000000000000000f000000000000000, type: 0}",
                "{fileID: 15106, guid: 0000000000000000f000000000000000, type: 0}",
                "{fileID: 10753, guid: 0000000000000000f000000000000000, type: 0}",
                "{fileID: 10770, guid: 0000000000000000f000000000000000, type: 0}",
                "{fileID: 10783, guid: 0000000000000000f000000000000000, type: 0}",
            }, entries);

            foreach (string shaderGuid in new[]
                     {
                         "d03bad68e5f94df47a2c30a8822ea41c",
                         "795b3814d0dfe9242829795ff0608656",
                         "1aad80d3fa14854488c67ee35f470633",
                         "30f88fdf99949b17e1187c24eba8ed93",
                         "23426744f12288344b3e94900b3f7cc9",
                     })
                StringAssert.DoesNotContain(shaderGuid, always.Groups["body"].Value);

            Match preload = Regex.Match(graphics,
                @"(?s)m_PreloadedShaders:\s*\r?\n(?<body>.*?)(?=\s*m_PreloadShadersBatchTimeLimit:)");
            MatchCollection preloadEntries = Regex.Matches(
                preload.Groups["body"].Value, @"(?m)^\s*-\s*\{[^\r\n]+\}\s*$");
            Assert.That(preloadEntries.Count, Is.EqualTo(1));
            StringAssert.Contains("fileID: 20000000", preloadEntries[0].Value);
            StringAssert.Contains("guid: " + VariantGuid, preloadEntries[0].Value);
            StringAssert.Contains("type: 2", preloadEntries[0].Value);
            Assert.That(Regex.Matches(graphics, VariantGuid).Count, Is.EqualTo(1));
            Assert.That(AssetDatabase.AssetPathToGUID(VariantPath),
                Is.EqualTo(VariantGuid));
        }

        private static void AssertGroupedInspector(string path,
            string[] expectedMainRows, string[] childOwners, string[] exactRows)
        {
            Match propertiesMatch = Regex.Match(Read(path),
                @"(?s)\bProperties\s*\{(?<body>.*?)\r?\n\s*\}\s*" +
                @"(?:HLSLINCLUDE.*?ENDHLSL\s*)?SubShader\b");
            Assert.That(propertiesMatch.Success, Is.True, path);
            string properties = propertiesMatch.Groups["body"].Value;
            StringAssert.DoesNotContain("[Header(", properties, path);
            Assert.That(Regex.IsMatch(properties,
                @"\[Sub\([^\)]*\)\]\s*\[Enum\("), Is.False, path);

            string[] rows = Lines(properties).Where(line =>
                Regex.IsMatch(line, @"_[A-Za-z][A-Za-z0-9_]*\s*\(")).ToArray();
            CollectionAssert.AreEqual(expectedMainRows,
                rows.Where(row => row.Contains("[Main(")).ToArray(), path);
            CollectionAssert.AreEqual(childOwners, Regex.Matches(properties,
                    @"\[Title\((?<owner>[^,]+),\s*_\)\]").Cast<Match>()
                .Select(match => match.Groups["owner"].Value).ToArray(), path);
            foreach (string exactRow in exactRows)
                Assert.That(rows, Does.Contain(exactRow), path);

            int section = -1;
            bool firstChild = false;
            foreach (string row in rows)
            {
                if (row.Contains("[Main("))
                {
                    ++section;
                    firstChild = true;
                    continue;
                }
                if (row.Contains("[HideInInspector]"))
                    continue;

                string owner = childOwners[section];
                string[] groupDrawers =
                {
                    "[Sub(" + owner, "[SubToggle(" + owner,
                    "[KWEnum(" + owner, "[Tex(" + owner,
                    "[UberVector2(" + owner, "[UberVector3(" + owner,
                    "[UberMinMaxVector(" + owner,
                    "[UberGradient(" + owner,
                    "[UberParticleStream(" + owner,
                    "[UberParticleNoiseChannel(" + owner,
                    "[UberParticleCurve(" + owner,
                };
                Assert.That(groupDrawers.Any(drawer => row.Contains(drawer)),
                    Is.True, path + ": " + row);
                if (!firstChild)
                    continue;
                StringAssert.Contains("[Title(" + owner + ", _)]", row,
                    path + ": " + row);
                firstChild = false;
            }
            Assert.That(section, Is.EqualTo(childOwners.Length - 1), path);
        }

        private static Color[] RenderSpriteDissolve(Texture2D source,
            Material material, RenderTexture target, Texture2D readback)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
            Graphics.Blit(source, target, material, 0);
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, target.width, target.height),
                    0, 0, false);
                readback.Apply(false, false);
                return readback.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static bool IsFinite(Color color)
        {
            return !float.IsNaN(color.r) && !float.IsInfinity(color.r) &&
                !float.IsNaN(color.g) && !float.IsInfinity(color.g) &&
                !float.IsNaN(color.b) && !float.IsInfinity(color.b) &&
                !float.IsNaN(color.a) && !float.IsInfinity(color.a);
        }

        private static float MaxRgbDifference(Color[] first, Color[] second)
        {
            Assert.That(first.Length, Is.EqualTo(second.Length));
            float maximum = 0f;
            for (int index = 0; index < first.Length; ++index)
            {
                maximum = Mathf.Max(maximum,
                    Mathf.Abs(first[index].r - second[index].r),
                    Mathf.Abs(first[index].g - second[index].g),
                    Mathf.Abs(first[index].b - second[index].b));
            }
            return maximum;
        }

        private static Color32[] ToColor32(IEnumerable<Color> colors)
        {
            return colors.Select(color => (Color32)color).ToArray();
        }

        private static bool HasPixelDifferentFrom(IEnumerable<Color[]> frames,
            Color32 reference)
        {
            return frames.SelectMany(frame => frame)
                .Select(color => (Color32)color)
                .Any(color => !color.Equals(reference));
        }

        private static bool HasDistinctFrames(IReadOnlyList<Color[]> frames)
        {
            if (frames.Count < 2)
                return false;

            Color32[] baseline = ToColor32(frames[0]);
            return frames.Skip(1).Any(frame =>
                !baseline.SequenceEqual(ToColor32(frame)));
        }

        private static void AssertContainsFlexibleWhitespace(string expected,
            string actual, string context = null)
        {
            string pattern = string.Join(@"\s+",
                Regex.Split(expected.Trim(), @"\s+")
                    .Select(Regex.Escape));
            Assert.That(Regex.IsMatch(actual, pattern), Is.True,
                context ?? expected);
        }

        private static string FileSha256(string path)
        {
            using (System.Security.Cryptography.SHA256 sha =
                   System.Security.Cryptography.SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(stream)
                    .Select(value => value.ToString("X2")));
        }

        private static void AssertVariantCollectionState(
            ShaderVariantCollection live, string collectionHash, string metaHash,
            string guid, bool dirty, BuildTarget activeTarget, int undoGroup,
            string undoGroupName, string context)
        {
            Assert.That(FileSha256(VariantPath), Is.EqualTo(collectionHash), context);
            Assert.That(FileSha256(VariantPath + ".meta"), Is.EqualTo(metaHash), context);
            Assert.That(AssetDatabase.AssetPathToGUID(VariantPath), Is.EqualTo(guid),
                context);
            Assert.That(EditorUtility.IsDirty(live), Is.EqualTo(dirty), context);
            Assert.That(EditorUserBuildSettings.activeBuildTarget,
                Is.EqualTo(activeTarget), context);
            Assert.That(Undo.GetCurrentGroup(), Is.EqualTo(undoGroup), context);
            Assert.That(Undo.GetCurrentGroupName(), Is.EqualTo(undoGroupName), context);
        }

        private static string CSharpMethodBody(string source, string signature)
        {
            int signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
            Assert.That(signatureIndex, Is.GreaterThanOrEqualTo(0), signature);
            int openBrace = source.IndexOf('{', signatureIndex + signature.Length);
            Assert.That(openBrace, Is.GreaterThanOrEqualTo(0), signature);
            int depth = 0;
            for (int index = openBrace; index < source.Length; ++index)
            {
                if (source[index] == '{') ++depth;
                else if (source[index] == '}' && --depth == 0)
                    return source.Substring(openBrace + 1, index - openBrace - 1);
            }
            Assert.Fail("Unclosed method body: " + signature);
            return null;
        }

        private static void AssertPasses(Material material, params string[] passes)
        {
            foreach (string pass in passes)
                Assert.That(material.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass);
        }

        private static string InlineFunctionBody(string source, string functionName)
        {
            Match function = Regex.Match(source,
                @"(?sm)^\s*inline\s+[^\r\n{]+\b" +
                Regex.Escape(functionName) +
                @"\s*\([^)]*\)\s*\{(?<body>.*?)^\s*\}");
            Assert.That(function.Success, Is.True, functionName);
            return function.Groups["body"].Value;
        }

        private static string InlineFunctionSource(string source,
            string functionName)
        {
            Match function = Regex.Match(source,
                @"(?sm)^\s*inline\s+[^\r\n{]+\b" +
                Regex.Escape(functionName) +
                @"\s*\([^)]*\)\s*\{.*?^\s*\}");
            Assert.That(function.Success, Is.True, functionName);
            return function.Value;
        }

        private static void AssertNormalizedShaderSource(string actual,
            string expected, string message)
        {
            Assert.That(Regex.Replace(actual, @"\s+", string.Empty),
                Is.EqualTo(Regex.Replace(expected, @"\s+", string.Empty)),
                message);
        }

        private static string Read(string path)
        {
            Assert.That(File.Exists(path), Is.True, path);
            return File.ReadAllText(path);
        }

        private static IEnumerable<string> Lines(string source)
        {
            return source.Split(new[] { "\r\n", "\n" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim());
        }

        private static string[] PragmaRows(string source, string keyword)
        {
            return Lines(source).Where(line => line.StartsWith("#pragma ",
                    StringComparison.Ordinal) && ContainsToken(line, keyword))
                .ToArray();
        }

        private static bool ContainsToken(string source, string token)
        {
            return Regex.IsMatch(source,
                @"(?<![A-Za-z0-9_])" + Regex.Escape(token) +
                @"(?![A-Za-z0-9_])");
        }

        private static void AssertVector(Vector4 actual, Vector4 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
            Assert.That(actual.w, Is.EqualTo(expected.w).Within(0.0001f));
        }

        private readonly struct ShaderCase
        {
            public readonly string Name;
            public readonly string Path;

            public ShaderCase(string name, string path)
            {
                Name = name;
                Path = path;
            }
        }

    }
}
