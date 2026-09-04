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
        [Test]
        public void SpriteHologramMatchesObjectSelectableSpaceContract()
        {
            string shader = Read(UberDirectory + "UberSprite.shader");
            string include = Read(UberDirectory + "UberSprite.hlsl");
            string variants = Read(VariantPath);

            foreach (string row in new[]
                     {
                         "[Main(Hologram, _HOLOGRAM_ON, on)] _HologramEnabled(\"Hologram\", Float) = 0",
                         "[Title(Hologram, _)] [Sub(Hologram)] [HDR] _HologramColor(\"Color\", Color) = (0, 1, 1, 1)",
                         "[Sub(Hologram)] _HologramOpacity(\"Opacity\", Range(0, 1)) = 0.35",
                         "[Sub(Hologram)] _HologramFresnelPower(\"Edge Width (Pixels)\", Range(0.5, 16)) = 4",
                         "[Sub(Hologram)] _HologramFresnelIntensity(\"Edge Intensity\", Range(0, 16)) = 2",
                         "[Sub(Hologram)] _HologramEdgeSoftnessPixels(\"Edge Softness (Pixels)\", Range(0, 32)) = 8",
                         "[KWEnum(Hologram, Object, _, World, _HOLOGRAM_WORLD_SPACE, Screen, _HOLOGRAM_SCREEN_SPACE)] _HologramSpace(\"Space\", Float) = 0",
                         "[UberVector3(Hologram)] _HologramObjectUpVector(\"Object Up Vector\", Vector) = (0, 1, 0, 0)",
                         "[Sub(Hologram)] _HologramScanlineDensity(\"Scanline Density\", Range(0.1, 128)) = 24",
                         "[Sub(Hologram)] _HologramScanlineSpeed(\"Scanline Speed\", Range(-10, 10)) = 1",
                         "[Sub(Hologram)] _HologramScanlineWidth(\"Scanline Width\", Range(0.01, 1)) = 0.12",
                         "[Sub(Hologram)] _HologramScanlineIntensity(\"Scanline Intensity\", Range(0, 16)) = 2",
                         "[Sub(Hologram)] _HologramNoiseScale(\"Noise Scale\", Range(0.01, 64)) = 4",
                         "[Sub(Hologram)] _HologramNoiseStrength(\"Noise Strength\", Range(0, 2)) = 0.35",
                         "[Sub(Hologram)] _HologramNoiseSpeed(\"Noise Speed\", Range(-10, 10)) = 0.5",
                     })
                StringAssert.Contains(row, shader);

            const string enablePragma =
                "#pragma shader_feature_local_fragment _ _HOLOGRAM_ON";
            const string spacePragma = "#pragma shader_feature_local_fragment _ " +
                "_HOLOGRAM_WORLD_SPACE _HOLOGRAM_SCREEN_SPACE";
            CollectionAssert.AreEqual(new[] { enablePragma, enablePragma },
                PragmaRows(shader, "_HOLOGRAM_ON"));
            CollectionAssert.AreEqual(new[] { spacePragma, spacePragma },
                PragmaRows(shader, "_HOLOGRAM_WORLD_SPACE"));
            int normalsPass = shader.IndexOf("Name \"NormalsRendering\"",
                StringComparison.Ordinal);
            Assert.That(shader.LastIndexOf("_HOLOGRAM_ON", StringComparison.Ordinal),
                Is.LessThan(normalsPass));
            StringAssert.DoesNotContain("multi_compile_local_fragment _ _HOLOGRAM", shader);

            foreach (string field in new[]
                     {
                         "float4 _HologramObjectUpVector;",
                         "half4 _HologramColor;",
                         "half _HologramOpacity;",
                         "half _HologramFresnelPower;",
                         "half _HologramFresnelIntensity;",
                         "half _HologramEdgeSoftnessPixels;",
                         "half _HologramScanlineDensity;",
                         "half _HologramScanlineSpeed;",
                         "half _HologramScanlineWidth;",
                         "half _HologramScanlineIntensity;",
                         "half _HologramNoiseScale;",
                         "half _HologramNoiseStrength;",
                         "half _HologramNoiseSpeed;",
                     })
                StringAssert.Contains(field, include);
            StringAssert.Contains("TransformWorldToObject(positionWS)", include);
            StringAssert.Contains("return dot(positionOS, UberGetHologramUpVector());",
                include);
            StringAssert.Contains("return positionWS.y;", include);
            StringAssert.Contains("GetNormalizedScreenSpaceUV(positionCS).y", include);
            StringAssert.Contains("albedo *= saturate(_HologramColor.rgb);", include);
            StringAssert.Contains("alpha *= saturate(_HologramOpacity);", include);
            StringAssert.Contains("half hologramEdge;", include);
            StringAssert.Contains(
                "const int edgeCoarseSteps = 4;",
                include);
            StringAssert.Contains("const int edgeRefinementSteps = 3;", include);
            StringAssert.Contains("texelDirection * middleRadius", include);
            StringAssert.Contains("const float diagonal = 0.70710678;",
                include);
            StringAssert.Contains(
                "half softMask = 1.0h - smoothstep(edgeWidth, searchRadius, searchFar);",
                include);
            StringAssert.Contains(
                "surface.hologramEdge = UberEvaluateSpriteHologramEdge(effectUV, layers.a);",
                include);
            Match hologramEmission = Regex.Match(include,
                @"(?s)inline half3 UberEvaluateSpriteHologramEmission\(.*?\r?\n\}");
            Assert.That(hologramEmission.Success, Is.True);
            StringAssert.Contains("edgeMask * max(_HologramFresnelIntensity",
                hologramEmission.Value);
            StringAssert.DoesNotContain("normalWS", hologramEmission.Value);
            StringAssert.DoesNotContain("viewDirectionWS", hologramEmission.Value);
            Assert.That(Regex.Matches(include,
                @"UberEvaluateSpriteHologramEmission\(\s*\r?\n\s*" +
                @"spriteSurface\.hologramEdge").Count,
                Is.EqualTo(2));

            int spriteVariantsStart = variants.IndexOf(
                "guid: 795b3814d0dfe9242829795ff0608656", StringComparison.Ordinal);
            int uiVariantsStart = variants.IndexOf(
                "guid: 1aad80d3fa14854488c67ee35f470633", StringComparison.Ordinal);
            Assert.That(spriteVariantsStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(uiVariantsStart, Is.GreaterThan(spriteVariantsStart));
            string spriteVariants = variants.Substring(spriteVariantsStart,
                uiVariantsStart - spriteVariantsStart);
            string[] serialized = Regex.Matches(spriteVariants,
                    @"(?m)^\s*- keywords:\s*(?<value>[^\r\n]*_HOLOGRAM[^\r\n]*)\r?\n" +
                    @"\s*passType:\s*13\s*$").Cast<Match>()
                .Select(match => match.Groups["value"].Value.Trim()).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "_GLITCH_ON _HOLOGRAM_ON",
                "_HOLOGRAM_ON",
                "_HOLOGRAM_ON _HOLOGRAM_SCREEN_SPACE",
                "_HOLOGRAM_ON _HOLOGRAM_WORLD_SPACE",
            }, serialized);

            Material material = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Assert.That(material.GetFloat("_HologramEnabled"), Is.Zero);
                Assert.That(material.GetFloat("_HologramSpace"), Is.Zero);
                Assert.That(material.GetColor("_HologramColor"),
                    Is.EqualTo(new Color(0f, 1f, 1f, 1f)));
                Assert.That(material.GetVector("_HologramObjectUpVector"),
                    Is.EqualTo(new Vector4(0f, 1f, 0f, 0f)));
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_HologramEnabled", 1f);
                UberShaderGUI inspector = new UberShaderGUI();
                for (int mode = 0; mode < 3; ++mode)
                {
                    material.EnableKeyword("_HOLOGRAM_WORLD_SPACE");
                    material.EnableKeyword("_HOLOGRAM_SCREEN_SPACE");
                    material.SetFloat("_HologramSpace", mode);
                    inspector.ValidateMaterial(material);
                    Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.True);
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_WORLD_SPACE"),
                        Is.EqualTo(mode == 1));
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_SCREEN_SPACE"),
                        Is.EqualTo(mode == 2));
                }

                material.SetFloat("_HologramEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_WORLD_SPACE"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_SCREEN_SPACE"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SpriteHologramGpuAppliesOpacityTintAndScanlines()
        {
            const int size = 32;
            Color previousSpriteColor = Shader.GetGlobalColor("unity_SpriteColor");
            Vector4 previousSpriteProps = Shader.GetGlobalVector("unity_SpriteProps");
            Texture2D source = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                Shader.SetGlobalColor("unity_SpriteColor", Color.white);
                Shader.SetGlobalVector("unity_SpriteProps",
                    new Vector4(1f, 1f, 0f, 0f));
                source = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true) { filterMode = FilterMode.Point };
                source.SetPixels(Enumerable.Repeat(Color.white, size * size).ToArray());
                source.Apply(false, false);
                readback = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true);
                target = new RenderTexture(size, size, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);

                material = new Material(Shader.Find(SpriteShaderName));
                material.SetColor("_BaseColor", Color.white);
                material.SetColor("_Color", Color.white);
                material.SetVector("_BaseSpriteUVRect", new Vector4(0f, 0f, 1f, 1f));
                material.SetFloat("_AlphaMultiplier", 1f);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_LightingMode", 1f);
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", 0f);
                material.EnableKeyword("_UNLIT_ON");
                Color[] baseline = RenderSpriteDissolve(source, material, target,
                    readback);

                material.SetFloat("_HologramEnabled", 1f);
                material.SetFloat("_HologramSpace", 2f);
                material.SetColor("_HologramColor", new Color(0f, 1f, 1f, 1f));
                material.SetFloat("_HologramOpacity", 0.25f);
                material.SetFloat("_HologramFresnelPower", 4f);
                material.SetFloat("_HologramFresnelIntensity", 0f);
                material.SetFloat("_HologramScanlineDensity", 8f);
                material.SetFloat("_HologramScanlineSpeed", 0f);
                material.SetFloat("_HologramScanlineWidth", 0.25f);
                material.SetFloat("_HologramScanlineIntensity", 2f);
                material.SetFloat("_HologramNoiseScale", 4f);
                material.SetFloat("_HologramNoiseStrength", 0f);
                material.SetFloat("_HologramNoiseSpeed", 0f);
                material.EnableKeyword("_HOLOGRAM_ON");
                material.EnableKeyword("_HOLOGRAM_SCREEN_SPACE");
                Color[] hologram = RenderSpriteDissolve(source, material, target,
                    readback);

                Assert.That(hologram.All(IsFinite), Is.True);
                Assert.That(MaxRgbDifference(hologram, baseline),
                    Is.GreaterThan(0.1f));
                Assert.That(hologram[size / 2].a,
                    Is.EqualTo(0.25f).Within(0.02f));
                Assert.That(hologram.Max(color => color.g) -
                    hologram.Min(color => color.g), Is.GreaterThan(0.1f));
                Assert.That(hologram.Average(color => color.r), Is.LessThan(0.01f));

                // Representative active GPU coverage for the shared value-noise
                // formula; 3D/UI wrappers are source-oracle checked below.
                material.SetFloat("_HologramNoiseStrength", 0.75f);
                Color[] noisyHologram = RenderSpriteDissolve(source, material,
                    target, readback);
                Color[] noisyHologramRepeat = RenderSpriteDissolve(source,
                    material, target, readback);
                Assert.That(MaxRgbDifference(noisyHologram, hologram),
                    Is.GreaterThan(0.02f));
                CollectionAssert.AreEqual(
                    noisyHologram.Select(color => (Color32)color),
                    noisyHologramRepeat.Select(color => (Color32)color));
            }
            finally
            {
                Shader.SetGlobalColor("unity_SpriteColor", previousSpriteColor);
                Shader.SetGlobalVector("unity_SpriteProps", previousSpriteProps);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        [Test]
        public void SpriteHologramGpuUsesTextureAlphaEdgeWidthAndSoftFalloff()
        {
            const int size = 32;
            Color previousSpriteColor = Shader.GetGlobalColor("unity_SpriteColor");
            Vector4 previousSpriteProps = Shader.GetGlobalVector("unity_SpriteProps");
            Texture2D source = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                Shader.SetGlobalColor("unity_SpriteColor", Color.white);
                Shader.SetGlobalVector("unity_SpriteProps",
                    new Vector4(1f, 1f, 0f, 0f));
                source = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true) { filterMode = FilterMode.Point };
                Color[] pixels = Enumerable.Repeat(Color.clear, size * size).ToArray();
                Color opaque = new Color(0.1f, 0.1f, 0.1f, 1f);
                for (int y = 8; y < 24; ++y)
                for (int x = 8; x < 24; ++x)
                    pixels[y * size + x] = opaque;
                source.SetPixels(pixels);
                source.Apply(false, false);

                readback = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true);
                target = new RenderTexture(size, size, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);

                material = new Material(Shader.Find(SpriteShaderName));
                material.SetColor("_BaseColor", Color.white);
                material.SetColor("_Color", Color.white);
                material.SetVector("_BaseSpriteUVRect",
                    new Vector4(0f, 0f, 1f, 1f));
                material.SetFloat("_AlphaMultiplier", 1f);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_LightingMode", 1f);
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", 0f);
                material.SetFloat("_HologramEnabled", 1f);
                material.SetColor("_HologramColor", Color.white);
                material.SetFloat("_HologramOpacity", 1f);
                material.SetFloat("_HologramFresnelIntensity", 1f);
                material.SetFloat("_HologramEdgeSoftnessPixels", 0f);
                material.SetFloat("_HologramScanlineIntensity", 0f);
                material.EnableKeyword("_UNLIT_ON");
                material.EnableKeyword("_HOLOGRAM_ON");

                material.SetFloat("_HologramFresnelPower", 1f);
                Color[] hard = RenderSpriteDissolve(source, material, target,
                    readback);
                material.SetFloat("_HologramEdgeSoftnessPixels", 6f);
                Color[] soft = RenderSpriteDissolve(source, material, target,
                    readback);

                int boundary = 16 * size + 8;
                int nearEdge = 16 * size + 10;
                int inner = 16 * size + 12;
                int farEdge = 16 * size + 14;
                int center = 16 * size + 16;
                Assert.That(hard[boundary].r,
                    Is.GreaterThan(hard[center].r + 0.5f));
                Assert.That(soft[nearEdge].r,
                    Is.GreaterThan(hard[nearEdge].r + 0.25f));
                Assert.That(soft[boundary].r,
                    Is.GreaterThan(soft[nearEdge].r + 0.05f));
                Assert.That(soft[nearEdge].r,
                    Is.GreaterThan(soft[inner].r + 0.1f));
                Assert.That(soft[inner].r,
                    Is.GreaterThan(soft[farEdge].r + 0.1f));
                Assert.That(soft[farEdge].r,
                    Is.GreaterThan(soft[center].r + 0.01f));
                Assert.That(Mathf.Abs(soft[center].r - hard[center].r),
                    Is.LessThan(0.02f));
                Assert.That(soft[center].r, Is.EqualTo(0.1f).Within(0.02f));
                Assert.That(soft[center].a, Is.EqualTo(1f).Within(0.02f));
                Assert.That(soft[16 * size + 4].a, Is.LessThan(0.02f));

                // Representative active GPU coverage for the shared hash and
                // band-boundary formulas used by all three surface glitches.
                material.DisableKeyword("_HOLOGRAM_ON");
                Color[] glitchBaseline = RenderSpriteDissolve(source, material,
                    target, readback);
                material.SetFloat("_GlitchStrength", 8f);
                material.SetFloat("_GlitchRGBSplit", 0f);
                material.SetFloat("_GlitchFrequency", 1f);
                material.SetFloat("_GlitchSpeed", 0f);
                material.SetVector("_GlitchBandSizeRange",
                    new Vector4(4f, 12f, 0f, 0f));
                material.EnableKeyword("_GLITCH_ON");
                Color[] glitch = RenderSpriteDissolve(source, material, target,
                    readback);
                Color[] glitchRepeat = RenderSpriteDissolve(source, material,
                    target, readback);
                Assert.That(MaxRgbDifference(glitch, glitchBaseline),
                    Is.GreaterThan(0.02f));
                CollectionAssert.AreEqual(
                    glitch.Select(color => (Color32)color),
                    glitchRepeat.Select(color => (Color32)color));
            }
            finally
            {
                Shader.SetGlobalColor("unity_SpriteColor", previousSpriteColor);
                Shader.SetGlobalVector("unity_SpriteProps", previousSpriteProps);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        [Test]
        public void SpriteRimUsesAtlasAlphaEdgeInsteadOfViewAngle()
        {
            string shader = Read(UberDirectory + "UberSprite.shader");
            string include = Read(UberDirectory + "UberSprite.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");

            StringAssert.Contains(
                "[Title(Rim, _)] [KWEnum(Rim, Add, _, Multiply, _RIM_MULTIPLY)] _RimBlendMode(\"Blend Mode\", Float) = 0",
                shader);
            StringAssert.Contains(
                "[Sub(Rim)] _RimPower(\"Edge Width (Pixels)\", Range(0.5, 16)) = 4",
                shader);
            StringAssert.Contains(
                "[Sub(Rim)] _RimEdgeSoftnessPixels(\"Edge Softness (Pixels)\", Range(0, 32)) = 8",
                shader);
            StringAssert.Contains("half _RimEdgeSoftnessPixels;", include);
            StringAssert.Contains(
                "#if defined(_HOLOGRAM_ON) || defined(_RIM_ON)", include);
            StringAssert.Contains(
                "surface.rimEdge = UberEvaluateSpriteRimEdge(effectUV, layers.a);",
                include);
            Assert.That(Regex.Matches(include,
                @"UberEvaluateSpriteRim\(color\.rgb, spriteSurface\.rimEdge\)").Count,
                Is.EqualTo(2));
            Assert.That(PragmaRows(shader, "_RIM_MULTIPLY"),
                Has.Length.EqualTo(2));
            StringAssert.Contains(
                "new KeywordBinding(\"_RimBlendMode\", \"_RIM_MULTIPLY\", 1,",
                gui);

            Match rimEdge = Regex.Match(include,
                @"(?s)inline half UberEvaluateSpriteRimEdge\(.*?\r?\n\}");
            Assert.That(rimEdge.Success, Is.True);
            StringAssert.Contains("float edgeWidth = clamp(_RimPower, 0.5h, 16.0h);",
                rimEdge.Value);
            StringAssert.Contains(
                "float edgeSoftness = clamp(_RimEdgeSoftnessPixels, 0.0h, 32.0h);",
                rimEdge.Value);
            Assert.That(Regex.Matches(rimEdge.Value,
                @"\bUberEvaluateSpriteHologramEdgeDirection\s*\(").Count,
                Is.EqualTo(8));
            StringAssert.DoesNotContain("normalWS", rimEdge.Value);
            StringAssert.DoesNotContain("viewDirectionWS", rimEdge.Value);

            Match rimColor = Regex.Match(include,
                @"(?s)inline half3 UberEvaluateSpriteRim\(.*?\r?\n\}");
            Assert.That(rimColor.Success, Is.True);
            StringAssert.Contains(
                "half3 rimContribution = _RimColor.rgb *",
                rimColor.Value);
            StringAssert.Contains("#if defined(_RIM_MULTIPLY)", rimColor.Value);
            StringAssert.Contains(
                "return sourceColor * (1.0h + rimContribution);",
                rimColor.Value);
            StringAssert.Contains("return sourceColor + rimContribution;",
                rimColor.Value);
            StringAssert.DoesNotContain("dot(", rimColor.Value);

            Material material = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Assert.That(material.GetFloat("_RimPower"), Is.EqualTo(4f));
                Assert.That(material.GetFloat("_RimBlendMode"), Is.Zero);
                Assert.That(material.GetFloat("_RimEdgeSoftnessPixels"),
                    Is.EqualTo(8f));
                Assert.That(material.GetFloat("_RimIntensity"), Is.EqualTo(1f));

                UberShaderGUI inspector = new UberShaderGUI();
                material.SetFloat("_RimEnabled", 1f);
                material.SetFloat("_RimBlendMode", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_RIM_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_RIM_MULTIPLY"), Is.True);

                material.SetFloat("_RimEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_RIM_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_RIM_MULTIPLY"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SpriteRimGpuUsesTextureAlphaEdgeWidthAndSoftFalloff()
        {
            const int size = 32;
            Color previousSpriteColor = Shader.GetGlobalColor("unity_SpriteColor");
            Vector4 previousSpriteProps = Shader.GetGlobalVector("unity_SpriteProps");
            Texture2D source = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                Shader.SetGlobalColor("unity_SpriteColor", Color.white);
                Shader.SetGlobalVector("unity_SpriteProps",
                    new Vector4(1f, 1f, 0f, 0f));
                source = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true) { filterMode = FilterMode.Point };
                Color[] pixels = Enumerable.Repeat(Color.clear, size * size).ToArray();
                Color opaque = new Color(0.1f, 0.1f, 0.1f, 1f);
                for (int y = 8; y < 24; ++y)
                for (int x = 8; x < 24; ++x)
                    pixels[y * size + x] = opaque;
                source.SetPixels(pixels);
                source.Apply(false, false);

                readback = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true);
                target = new RenderTexture(size, size, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);

                material = new Material(Shader.Find(SpriteShaderName));
                material.SetColor("_BaseColor", Color.white);
                material.SetColor("_Color", Color.white);
                material.SetVector("_BaseSpriteUVRect",
                    new Vector4(0f, 0f, 1f, 1f));
                material.SetFloat("_AlphaMultiplier", 1f);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_LightingMode", 1f);
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", 0f);
                material.SetFloat("_RimEnabled", 1f);
                material.SetColor("_RimColor", Color.white);
                material.SetFloat("_RimIntensity", 1f);
                material.SetFloat("_RimPower", 1f);
                material.SetFloat("_RimEdgeSoftnessPixels", 0f);
                material.EnableKeyword("_UNLIT_ON");
                material.EnableKeyword("_RIM_ON");

                Color[] hard = RenderSpriteDissolve(source, material, target,
                    readback);
                material.SetFloat("_RimEdgeSoftnessPixels", 6f);
                Color[] soft = RenderSpriteDissolve(source, material, target,
                    readback);
                material.SetFloat("_RimEdgeSoftnessPixels", 0f);
                material.SetFloat("_RimBlendMode", 1f);
                material.EnableKeyword("_RIM_MULTIPLY");
                Color[] multiply = RenderSpriteDissolve(source, material, target,
                    readback);

                int boundary = 16 * size + 8;
                int nearEdge = 16 * size + 10;
                int inner = 16 * size + 12;
                int farEdge = 16 * size + 14;
                int center = 16 * size + 16;
                Assert.That(hard[boundary].r,
                    Is.GreaterThan(hard[center].r + 0.5f));
                Assert.That(soft[nearEdge].r,
                    Is.GreaterThan(hard[nearEdge].r + 0.25f));
                Assert.That(soft[boundary].r,
                    Is.GreaterThan(soft[nearEdge].r + 0.05f));
                Assert.That(soft[nearEdge].r,
                    Is.GreaterThan(soft[inner].r + 0.1f));
                Assert.That(soft[inner].r,
                    Is.GreaterThan(soft[farEdge].r + 0.1f));
                Assert.That(soft[farEdge].r,
                    Is.GreaterThan(soft[center].r + 0.01f));
                Assert.That(soft[center].r, Is.EqualTo(0.1f).Within(0.02f));
                Assert.That(soft[center].a, Is.EqualTo(1f).Within(0.02f));
                Assert.That(soft[16 * size + 4].a, Is.LessThan(0.02f));
                Assert.That(multiply[boundary].r,
                    Is.GreaterThan(multiply[center].r + 0.05f));
                Assert.That(multiply[boundary].r,
                    Is.LessThan(hard[boundary].r - 0.5f));
                Assert.That(multiply[center].r,
                    Is.EqualTo(hard[center].r).Within(0.02f));
            }
            finally
            {
                Shader.SetGlobalColor("unity_SpriteColor", previousSpriteColor);
                Shader.SetGlobalVector("unity_SpriteProps", previousSpriteProps);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        [Test]
        public void SpriteDissolveUsesAtlasLocalModeRangesAndSwipeContract()
        {
            string shader = Read(UberDirectory + "UberSprite.shader");
            string include = Read(UberDirectory + "UberSprite.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            string variants = Read(VariantPath);

            foreach (string row in new[]
                     {
                         "[KWEnum(Dissolve, Noise, _, Radial, _DISSOLVE_RADIAL, Swipe, _DISSOLVE_SWIPE)] _DissolveMode(\"Mode\", Float) = 0",
                         "[UberMinMaxVector(Dissolve_)] _DissolveNoiseRange(\"Noise Range\", Vector) = (0, 1, 0, 0)",
                         "[UberVector2(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialCenter(\"Radial Center\", Vector) = (0.5, 0.5, 0, 0)",
                         "[UberMinMaxVector(Dissolve_DISSOLVE_RADIAL, _DissolveAmount)] _DissolveRadialRange(\"Radial Range\", Vector) = (0, 0.7071, 0, 0)",
                         "[Sub(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialNoiseStrength(\"Radial Noise Strength\", Range(0, 1)) = 0.15",
                         "[UberVector2(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeCenter(\"Swipe Center\", Vector) = (0.5, 0.5, 0, 0)",
                         "[Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRotation(\"Swipe Rotation\", Range(-180, 180)) = 0",
                         "[UberMinMaxVector(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRange(\"Swipe Range\", Vector) = (-0.5, 0.5, 0, 0)",
                         "[Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeNoiseStrength(\"Swipe Noise Strength\", Range(0, 1)) = 0.15",
                     })
                StringAssert.Contains(row, shader);
            StringAssert.DoesNotContain("_DissolveRadialRadius", shader + include);

            const string modePragma = "#pragma shader_feature_local_fragment _ " +
                "_DISSOLVE_RADIAL _DISSOLVE_SWIPE";
            string[] modePragmas = PragmaRows(shader, "_DISSOLVE_SWIPE");
            Assert.That(modePragmas, Has.Length.EqualTo(6));
            Assert.That(modePragmas.All(row => row == modePragma), Is.True,
                string.Join(" | ", modePragmas));
            Assert.That(PragmaRows(shader, "_DISSOLVE_ON"), Has.Length.EqualTo(6));
            StringAssert.DoesNotContain("multi_compile_local_fragment _ " +
                "_DISSOLVE_RADIAL", shader);

            foreach (string field in new[]
                     {
                         "float4 _DissolveNoiseRange;",
                         "float4 _DissolveRadialCenter;",
                         "float4 _DissolveRadialRange;",
                         "float4 _DissolveSwipeCenter;",
                         "float4 _DissolveSwipeRange;",
                         "float _DissolveSwipeRotation;",
                         "half _DissolveSwipeNoiseStrength;",
                     })
                StringAssert.Contains(field, include);
            StringAssert.Contains(
                "saturate(UberNormalizeUV(rawUV, _BaseSpriteUVRect))", include);
            StringAssert.Contains(
                "UberSafeInverseLerp(_DissolveNoiseRange.x,", include);
            StringAssert.Contains(
                "UberSafeInverseLerp(_DissolveRadialRange.x,", include);
            StringAssert.Contains(
                "UberSafeInverseLerp(_DissolveSwipeRange.x,", include);
            StringAssert.Contains(
                "radians(fmod(_DissolveSwipeRotation, 360.0))", include);
            StringAssert.Contains(
                "float2 direction = float2(cos(rotation), sin(rotation));", include);
            StringAssert.Contains(
                "dot(localUV - _DissolveSwipeCenter.xy, direction)", include);
            StringAssert.Contains("saturate(_DissolveSwipeNoiseStrength)", include);
            StringAssert.Contains("half threshold = saturate(_DissolveAmount);",
                include);
            StringAssert.Contains(
                "new KeywordBinding(\"_DissolveMode\", \"_DISSOLVE_SWIPE\", 2,",
                gui);

            string spriteGuid = AssetDatabase.AssetPathToGUID(
                UberDirectory + "UberSprite.shader");
            Match spriteVariantBlock = Regex.Match(variants,
                @"(?sm)^\s*- first:\s*\{fileID:\s*4800000,\s*guid:\s*" +
                Regex.Escape(spriteGuid) + @".*?(?=^\s*- first:|\z)");
            Assert.That(spriteVariantBlock.Success, Is.True, spriteGuid);
            string[] swipeVariants = Regex.Matches(spriteVariantBlock.Value,
                    @"(?m)^\s*- keywords:\s*(?<value>[^\r\n]*_DISSOLVE_SWIPE[^\r\n]*)\r?\n" +
                    @"\s*passType:\s*13\s*$").Cast<Match>()
                .Select(match => match.Groups["value"].Value.Trim()).ToArray();
            CollectionAssert.AreEqual(new[] { "_DISSOLVE_ON _DISSOLVE_SWIPE" },
                swipeVariants);
            StringAssert.DoesNotContain("_DISSOLVE_SWIPE",
                Read(UberDirectory + "Uber3D.shader") +
                Read(UberDirectory + "Uber3D.hlsl"));

            Material material = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Assert.That(material.HasProperty("_DissolveRadialRadius"), Is.False);
                AssertVector(material.GetVector("_DissolveNoiseRange"),
                    new Vector4(0f, 1f, 0f, 0f));
                AssertVector(material.GetVector("_DissolveRadialCenter"),
                    new Vector4(0.5f, 0.5f, 0f, 0f));
                AssertVector(material.GetVector("_DissolveRadialRange"),
                    new Vector4(0f, 0.7071f, 0f, 0f));
                Assert.That(material.GetFloat("_DissolveRadialNoiseStrength"),
                    Is.EqualTo(0.15f).Within(0.0001f));
                AssertVector(material.GetVector("_DissolveSwipeCenter"),
                    new Vector4(0.5f, 0.5f, 0f, 0f));
                AssertVector(material.GetVector("_DissolveSwipeRange"),
                    new Vector4(-0.5f, 0.5f, 0f, 0f));
                Assert.That(material.GetFloat("_DissolveSwipeRotation"),
                    Is.EqualTo(0f));
                Assert.That(material.GetFloat("_DissolveSwipeNoiseStrength"),
                    Is.EqualTo(0.15f).Within(0.0001f));

                material.SetFloat("_DissolveEnabled", 1f);
                UberShaderGUI inspector = new UberShaderGUI();
                for (int mode = 0; mode < 3; ++mode)
                {
                    material.EnableKeyword("_DISSOLVE_RADIAL");
                    material.EnableKeyword("_DISSOLVE_SWIPE");
                    material.SetFloat("_DissolveMode", mode);
                    inspector.ValidateMaterial(material);
                    Assert.That(material.IsKeywordEnabled("_DISSOLVE_ON"), Is.True);
                    Assert.That(material.IsKeywordEnabled("_DISSOLVE_RADIAL"),
                        Is.EqualTo(mode == 1));
                    Assert.That(material.IsKeywordEnabled("_DISSOLVE_SWIPE"),
                        Is.EqualTo(mode == 2));
                }

                material.SetFloat("_DissolveEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_RADIAL"), Is.False);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_SWIPE"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SpriteLightSweepUsesAtlasLocalSwipeRangeAndSelectableProfiles()
        {
            string shader = Read(UberDirectory + "UberSprite.shader");
            string include = Read(UberDirectory + "UberSprite.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            string variants = Read(VariantPath);

            foreach (string row in new[]
                     {
                         "[Main(LightSweep, _LIGHT_SWEEP_ON, on)] _LightSweepEnabled(\"Light Sweep\", Float) = 0",
                         "[Title(LightSweep, _)] [KWEnum(LightSweep, Soft, _, Sharp, _LIGHT_SWEEP_SHARP)] _LightSweepMode(\"Type\", Float) = 0",
                         "[KWEnum(LightSweep, Add, _, Multiply, _LIGHT_SWEEP_MULTIPLY)] _LightSweepBlendMode(\"Blend Mode\", Float) = 0",
                         "[Sub(LightSweep)] [HDR] _LightSweepColor(\"Color\", Color) = (1, 1, 1, 1)",
                         "[Sub(LightSweep)] _LightSweepIntensity(\"Intensity\", Range(0, 16)) = 2",
                         "[Sub(LightSweep)] _LightSweepAmount(\"Amount\", Range(0, 1)) = 0",
                         "[UberVector2(LightSweep)] _LightSweepCenter(\"Center\", Vector) = (0.5, 0.5, 0, 0)",
                         "[Sub(LightSweep)] _LightSweepRotation(\"Rotation\", Range(-180, 180)) = 0",
                         "[UberMinMaxVector(LightSweep)] _LightSweepRange(\"Range\", Vector) = (-0.5, 0.5, 0, 0)",
                         "[Sub(LightSweep)] _LightSweepWidth(\"Width\", Range(0.001, 1)) = 0.15",
                     })
                StringAssert.Contains(row, shader);

            const string enabledPragma =
                "#pragma shader_feature_local_fragment _ _LIGHT_SWEEP_ON";
            const string sharpPragma =
                "#pragma shader_feature_local_fragment _ _LIGHT_SWEEP_SHARP";
            const string multiplyPragma =
                "#pragma shader_feature_local_fragment _ _LIGHT_SWEEP_MULTIPLY";
            CollectionAssert.AreEqual(new[] { enabledPragma, enabledPragma },
                PragmaRows(shader, "_LIGHT_SWEEP_ON"));
            CollectionAssert.AreEqual(new[] { sharpPragma, sharpPragma },
                PragmaRows(shader, "_LIGHT_SWEEP_SHARP"));
            CollectionAssert.AreEqual(new[] { multiplyPragma, multiplyPragma },
                PragmaRows(shader, "_LIGHT_SWEEP_MULTIPLY"));
            StringAssert.DoesNotContain(
                "#pragma multi_compile_local_fragment _ _LIGHT_SWEEP", shader);
            int normalsPass = shader.IndexOf("Name \"NormalsRendering\"",
                StringComparison.Ordinal);
            Assert.That(normalsPass, Is.GreaterThanOrEqualTo(0));
            StringAssert.DoesNotContain("_LIGHT_SWEEP_",
                shader.Substring(normalsPass));

            foreach (string field in new[]
                     {
                         "float4 _LightSweepCenter;",
                         "float4 _LightSweepRange;",
                         "half4 _LightSweepColor;",
                         "half _LightSweepAmount;",
                         "float _LightSweepRotation;",
                         "half _LightSweepWidth;",
                         "half _LightSweepIntensity;",
                         "half _LightSweepEnabled, _LightSweepMode, _LightSweepBlendMode;",
                     })
                StringAssert.Contains(field, include);
            foreach (string contract in new[]
                     {
                         "radians(fmod(_LightSweepRotation, 360.0))",
                         "dot(localUV - _LightSweepCenter.xy, direction)",
                         "lerp(_LightSweepRange.x, _LightSweepRange.y,",
                         "saturate(_LightSweepAmount)",
                         "1.0h - smoothstep(0.0, halfWidth, distanceToSweep)",
                         "halfWidth - edgeAA,",
                         "#if defined(_LIGHT_SWEEP_MULTIPLY)",
                         "albedo *= 1.0h + sweepColor * influence;",
                         "emission += sweepColor * influence;",
                         "UberApplySpriteLightSweep(surface.localUV, surface.alpha, surface.albedo,",
                     })
                StringAssert.Contains(contract, include);
            StringAssert.Contains(
                "new KeywordBinding(\"_LightSweepEnabled\", \"_LIGHT_SWEEP_ON\", 1)",
                gui);
            StringAssert.Contains(
                "new KeywordBinding(\"_LightSweepMode\", \"_LIGHT_SWEEP_SHARP\", 1,",
                gui);
            StringAssert.Contains(
                "new KeywordBinding(\"_LightSweepBlendMode\", \"_LIGHT_SWEEP_MULTIPLY\", 1,",
                gui);

            string[] serialized = Regex.Matches(variants,
                    @"(?m)^\s*- keywords:\s*(?<value>[^\r\n]*_LIGHT_SWEEP[^\r\n]*)\r?\n" +
                    @"\s*passType:\s*13\s*$").Cast<Match>()
                .Select(match => match.Groups["value"].Value.Trim()).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "_LIGHT_SWEEP_MULTIPLY _LIGHT_SWEEP_ON",
                "_LIGHT_SWEEP_MULTIPLY _LIGHT_SWEEP_ON _LIGHT_SWEEP_SHARP",
                "_LIGHT_SWEEP_ON",
                "_LIGHT_SWEEP_ON _LIGHT_SWEEP_SHARP",
            }, serialized);

            Material material = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Assert.That(material.GetFloat("_LightSweepEnabled"), Is.Zero);
                Assert.That(material.GetFloat("_LightSweepMode"), Is.Zero);
                Assert.That(material.GetFloat("_LightSweepBlendMode"), Is.Zero);
                Assert.That(material.GetColor("_LightSweepColor"),
                    Is.EqualTo(Color.white));
                Assert.That(material.GetFloat("_LightSweepIntensity"),
                    Is.EqualTo(2f));
                Assert.That(material.GetFloat("_LightSweepAmount"), Is.Zero);
                AssertVector(material.GetVector("_LightSweepCenter"),
                    new Vector4(0.5f, 0.5f, 0f, 0f));
                Assert.That(material.GetFloat("_LightSweepRotation"), Is.Zero);
                AssertVector(material.GetVector("_LightSweepRange"),
                    new Vector4(-0.5f, 0.5f, 0f, 0f));
                Assert.That(material.GetFloat("_LightSweepWidth"),
                    Is.EqualTo(0.15f).Within(0.0001f));

                UberShaderGUI inspector = new UberShaderGUI();
                material.SetFloat("_LightSweepEnabled", 1f);
                material.SetFloat("_LightSweepMode", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_SHARP"),
                    Is.False);

                material.SetFloat("_LightSweepMode", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_SHARP"),
                    Is.True);

                material.SetFloat("_LightSweepBlendMode", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_MULTIPLY"),
                    Is.True);

                material.SetFloat("_LightSweepEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_SHARP"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_MULTIPLY"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SpriteLightSweepGpuMovesRotatesChangesProfileAndBlendMode()
        {
            const int size = 64;
            Color previousSpriteColor = Shader.GetGlobalColor("unity_SpriteColor");
            Vector4 previousSpriteProps = Shader.GetGlobalVector("unity_SpriteProps");
            Texture2D source = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                Shader.SetGlobalColor("unity_SpriteColor", Color.white);
                Shader.SetGlobalVector("unity_SpriteProps",
                    new Vector4(1f, 1f, 0f, 0f));
                source = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true) { filterMode = FilterMode.Point };
                source.SetPixels(Enumerable.Repeat(
                    new Color(0.1f, 0.1f, 0.1f, 1f), size * size).ToArray());
                source.Apply(false, false);
                readback = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true);
                target = new RenderTexture(size, size, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);

                material = new Material(Shader.Find(SpriteShaderName));
                material.SetColor("_BaseColor", Color.white);
                material.SetColor("_Color", Color.white);
                material.SetVector("_BaseSpriteUVRect",
                    new Vector4(0f, 0f, 1f, 1f));
                material.SetFloat("_AlphaMultiplier", 1f);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_LightingMode", 1f);
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", 0f);
                material.SetFloat("_LightSweepEnabled", 1f);
                material.SetFloat("_LightSweepBlendMode", 0f);
                material.SetColor("_LightSweepColor", Color.white);
                material.SetFloat("_LightSweepIntensity", 2f);
                material.SetVector("_LightSweepCenter",
                    new Vector4(0.5f, 0.5f, 0f, 0f));
                material.SetVector("_LightSweepRange",
                    new Vector4(-0.3f, 0.3f, 0f, 0f));
                material.SetFloat("_LightSweepWidth", 0.16f);
                material.SetFloat("_LightSweepRotation", 0f);
                material.EnableKeyword("_UNLIT_ON");
                material.EnableKeyword("_LIGHT_SWEEP_ON");

                material.SetFloat("_LightSweepAmount", 0f);
                Color[] left = RenderSpriteDissolve(source, material, target,
                    readback);
                material.SetFloat("_LightSweepAmount", 1f);
                Color[] right = RenderSpriteDissolve(source, material, target,
                    readback);
                Assert.That(left.Max(color => color.r) - left.Min(color => color.r),
                    Is.GreaterThan(0.5f),
                    $"left min/max {left.Min(color => color.r)}/" +
                    $"{left.Max(color => color.r)}; keywords " +
                    string.Join(",", material.shaderKeywords));
                Assert.That(right.Max(color => color.r) - right.Min(color => color.r),
                    Is.GreaterThan(0.5f),
                    $"right min/max {right.Min(color => color.r)}/" +
                    $"{right.Max(color => color.r)}; keywords " +
                    string.Join(",", material.shaderKeywords));
                int leftPeak = Enumerable.Range(0, size).OrderByDescending(x =>
                    Enumerable.Range(0, size).Average(y => left[y * size + x].r))
                    .First();
                int rightPeak = Enumerable.Range(0, size).OrderByDescending(x =>
                    Enumerable.Range(0, size).Average(y => right[y * size + x].r))
                    .First();
                Assert.That(leftPeak, Is.LessThan(size / 2 - 8));
                Assert.That(rightPeak, Is.GreaterThan(size / 2 + 8));
                Assert.That(rightPeak - leftPeak, Is.GreaterThan(size / 3));

                material.SetFloat("_LightSweepAmount", 0.5f);
                material.SetFloat("_LightSweepRotation", 0f);
                Color[] horizontal = RenderSpriteDissolve(source, material,
                    target, readback);
                material.SetFloat("_LightSweepRotation", 90f);
                Color[] vertical = RenderSpriteDissolve(source, material,
                    target, readback);
                int center = size / 2;
                int quarter = size / 4;
                Assert.That(horizontal[quarter * size + center].r,
                    Is.GreaterThan(horizontal[center * size + quarter].r + 1f));
                Assert.That(vertical[center * size + quarter].r,
                    Is.GreaterThan(vertical[quarter * size + center].r + 1f));

                material.SetFloat("_LightSweepRotation", 0f);
                material.SetFloat("_LightSweepWidth", 0.4f);
                material.DisableKeyword("_LIGHT_SWEEP_SHARP");
                Color[] soft = RenderSpriteDissolve(source, material, target,
                    readback);
                material.EnableKeyword("_LIGHT_SWEEP_SHARP");
                Color[] sharp = RenderSpriteDissolve(source, material, target,
                    readback);
                int shoulder = center * size + center + 5;
                Assert.That(sharp[shoulder].r,
                    Is.GreaterThan(soft[shoulder].r + 0.2f));
                Assert.That(Mathf.Abs(sharp[center * size + center].r -
                    soft[center * size + center].r), Is.LessThan(0.05f));

                material.DisableKeyword("_LIGHT_SWEEP_SHARP");
                material.SetFloat("_LightSweepBlendMode", 1f);
                material.EnableKeyword("_LIGHT_SWEEP_MULTIPLY");
                Color[] multiply = RenderSpriteDissolve(source, material, target,
                    readback);
                int centerIndex = center * size + center;
                int outsideIndex = center * size + quarter;
                Assert.That(multiply[centerIndex].r,
                    Is.GreaterThan(multiply[outsideIndex].r + 0.15f));
                Assert.That(multiply[centerIndex].r,
                    Is.EqualTo(0.3f).Within(0.05f));
                Assert.That(soft[centerIndex].r,
                    Is.GreaterThan(multiply[centerIndex].r + 1f));
                Assert.That(new[]
                    {
                        left, right, horizontal, vertical, soft, sharp, multiply,
                    }
                    .SelectMany(colors => colors).All(IsFinite), Is.True);
                Assert.That(multiply.All(color =>
                    Mathf.Abs(color.a - 1f) < 0.02f), Is.True);
            }
            finally
            {
                Shader.SetGlobalColor("unity_SpriteColor", previousSpriteColor);
                Shader.SetGlobalVector("unity_SpriteProps", previousSpriteProps);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        [Test]
        public void SpriteGlitchMatchesUiIndependentAtlasSafeContract()
        {
            string shader = Read(UberDirectory + "UberSprite.shader");
            string include = Read(UberDirectory + "UberSprite.hlsl");

            foreach (string row in new[]
                     {
                         "[Main(Glitch, _GLITCH_ON, on)] _GlitchEnabled(\"Glitch\", Float) = 0",
                         "[Title(Glitch, _)] [Sub(Glitch)] _GlitchStrength(\"Strength (Pixels)\", Range(0, 64)) = 8",
                         "[Sub(Glitch)] _GlitchRGBSplit(\"RGB Split (Pixels)\", Range(0, 16)) = 2",
                         "[Sub(Glitch)] _GlitchFrequency(\"Frequency\", Range(0, 1)) = 0.25",
                         "[Sub(Glitch)] _GlitchSpeed(\"Speed\", Range(0, 30)) = 8",
                         "[UberMinMaxVector(Glitch)] _GlitchBandSizeRange(\"Band Size Range (Pixels)\", Vector) = (4, 12, 0, 0)",
                     })
            {
                StringAssert.Contains(row, shader);
            }

            CollectionAssert.AreEqual(Enumerable.Repeat(
                "#pragma shader_feature_local_fragment _ _GLITCH_ON", 6).ToArray(),
                PragmaRows(shader, "_GLITCH_ON"));

            foreach (string field in new[]
                     {
                         "float4 _GlitchBandSizeRange;",
                         "half _GlitchEnabled;",
                         "half _GlitchStrength;",
                         "half _GlitchRGBSplit;",
                         "half _GlitchFrequency;",
                         "half _GlitchSpeed;",
                     })
            {
                StringAssert.Contains(field, include);
            }

            foreach (string contract in new[]
                     {
                         "inline float2 UberSpriteApplyGlitchUV(float2 rawUV,",
                         "float minBandSize = clamp(min(_GlitchBandSizeRange.x,",
                         "float maxBandSize = clamp(max(_GlitchBandSizeRange.x,",
                         "UberEvaluateGlitchBandBoundary(boundaryIndex, frame,",
                         "float activation = step(1.0 - saturate(_GlitchFrequency),",
                         "shiftPixels * abs(_MainTex_TexelSize.x)",
                         "if (glitchActivation > 0.0h && _GlitchRGBSplit > 0.0001h)",
                         "center.r = UberSampleSpriteLayers(effectUV + splitUV).r;",
                         "center.b = UberSampleSpriteLayers(effectUV - splitUV).b;",
                         "layers = UberSpriteApplyGlitchRGBSplit(effectUV, layers,",
                         "surface.alpha = UberEvaluateSpriteSilhouette(effectUV, rawUV, layers.a,",
                         "surface.hologramEdge = UberEvaluateSpriteHologramEdge(effectUV, layers.a);",
                         "alpha *= UberEvaluateUVFade(sourceUV);",
                     })
            {
                StringAssert.Contains(contract, include);
            }

            Assert.That(Regex.Matches(include,
                @"\bUberSpriteApplyGlitchUV\s*\(").Count, Is.GreaterThanOrEqualTo(4));
            StringAssert.DoesNotContain(
                "defined(_HOLOGRAM_ON) && defined(_GLITCH_ON)", include);

            ShaderVariantCollection collection =
                AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(VariantPath);
            Assert.That(collection.Contains(new UberShaderVariantSpec(SpriteShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_ON").ToVariant()),
                Is.True);
            Assert.That(collection.Contains(new UberShaderVariantSpec(SpriteShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_ON",
                "_HOLOGRAM_ON").ToVariant()), Is.True);

            Material material = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Assert.That(material.GetFloat("_GlitchEnabled"), Is.Zero);
                Assert.That(material.GetFloat("_GlitchRGBSplit"), Is.EqualTo(2f));
                AssertVector(material.GetVector("_GlitchBandSizeRange"),
                    new Vector4(4f, 12f, 0f, 0f));

                UberShaderGUI inspector = new UberShaderGUI();
                material.SetFloat("_GlitchEnabled", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.False);

                material.SetFloat("_HologramEnabled", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.True);

                material.SetFloat("_HologramEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SpriteRadialRangeAuthoringPreviewsIndependentEndpoints()
        {
            string shader = Read(UberDirectory + "UberSprite.shader");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            string runtime = Read(UberDirectory + "UberSprite.hlsl");
            const string radialAttribute =
                "[UberMinMaxVector(Dissolve_DISSOLVE_RADIAL, _DissolveAmount)] " +
                "_DissolveRadialRange(\"Radial Range\", Vector)";

            StringAssert.Contains(radialAttribute, shader);
            Assert.That(Lines(shader).Count(line =>
                    line.Contains("[UberMinMaxVector(") &&
                    line.Contains("_DissolveAmount")), Is.EqualTo(1));
            StringAssert.Contains(
                "public UberMinMaxVectorDrawer(string group, string amountPropertyName)",
                gui);
            StringAssert.Contains(
                "private static readonly GUIContent CurrentBaseRadiusLabel", gui);
            StringAssert.Contains(
                "UberDrawerLayout.DrawPropertyLabel(row,\n            CurrentBaseRadiusLabel)",
                gui.Replace("\r\n", "\n"));
            StringAssert.Contains("rangeProperty.hasMixedValue || amountProperty.hasMixedValue",
                gui);
            StringAssert.Contains("Mathf.Lerp(range.x, range.y, Mathf.Clamp01(amount))",
                gui);
            StringAssert.Contains("SetPreviewAmount(amountProperty, 0.0f)", gui);
            StringAssert.Contains("SetPreviewAmount(amountProperty, 1.0f)", gui);
            StringAssert.DoesNotContain("Current Base Radius", runtime);
            StringAssert.Contains(
                "UberSafeInverseLerp(_DissolveRadialRange.x,", runtime);

            UberMinMaxVectorDrawer basicDrawer =
                new UberMinMaxVectorDrawer("Dissolve_");
            UberMinMaxVectorDrawer radialDrawer = new UberMinMaxVectorDrawer(
                "Dissolve_DISSOLVE_RADIAL", "_DissolveAmount");
            MethodInfo height = typeof(UberMinMaxVectorDrawer).GetMethod(
                "GetVisibleHeight", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(height, Is.Not.Null);
            Assert.That((float)height.Invoke(basicDrawer, null), Is.EqualTo(18f));
            Assert.That((float)height.Invoke(radialDrawer, null), Is.EqualTo(38f));

            MethodInfo calculate = typeof(UberMinMaxVectorDrawer).GetMethod(
                "CalculateCurrentBaseRadius",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(calculate, Is.Not.Null);
            Assert.That((float)calculate.Invoke(null, new object[]
                { new Vector4(2f, 6f, 0f, 0f), 0.5f }), Is.EqualTo(4f));
            Assert.That((float)calculate.Invoke(null, new object[]
                { new Vector4(8f, 2f, 0f, 0f), 0.25f }), Is.EqualTo(6.5f));
            Assert.That((float)calculate.Invoke(null, new object[]
                { new Vector4(-2f, 3f, 0f, 0f), -4f }), Is.EqualTo(-2f));
            Assert.That((float)calculate.Invoke(null, new object[]
                { new Vector4(-2f, 3f, 0f, 0f), 4f }), Is.EqualTo(3f));

            Material first = new Material(Shader.Find(SpriteShaderName));
            Material second = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Vector4 firstRange = new Vector4(-2f, 5f, 7f, 9f);
                Vector4 secondRange = new Vector4(8f, 1f, 4f, 6f);
                first.SetVector("_DissolveRadialRange", firstRange);
                second.SetVector("_DissolveRadialRange", secondRange);
                first.SetFloat("_DissolveAmount", 0.2f);
                second.SetFloat("_DissolveAmount", 0.8f);
                MaterialProperty[] properties = MaterialEditor.GetMaterialProperties(
                    new UnityEngine.Object[] { first, second });
                MaterialProperty amount = properties.Single(property =>
                    property.name == "_DissolveAmount");
                Assert.That(amount.hasMixedValue, Is.True);

                MethodInfo preview = typeof(UberMinMaxVectorDrawer).GetMethod(
                    "SetPreviewAmount", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(preview, Is.Not.Null);
                preview.Invoke(null, new object[] { amount, 0f });
                Assert.That(first.GetFloat("_DissolveAmount"), Is.EqualTo(0f));
                Assert.That(second.GetFloat("_DissolveAmount"), Is.EqualTo(0f));
                AssertVector(first.GetVector("_DissolveRadialRange"), firstRange);
                AssertVector(second.GetVector("_DissolveRadialRange"), secondRange);
                preview.Invoke(null, new object[] { amount, 1f });
                Assert.That(first.GetFloat("_DissolveAmount"), Is.EqualTo(1f));
                Assert.That(second.GetFloat("_DissolveAmount"), Is.EqualTo(1f));
                AssertVector(first.GetVector("_DissolveRadialRange"), firstRange);
                AssertVector(second.GetVector("_DissolveRadialRange"), secondRange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void SpriteDissolveEdgeGradientUsesReviewedInlineRuntimeContract()
        {
            string shader = Read(UberDirectory + "UberSprite.shader");
            string include = Read(UberDirectory + "UberSprite.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            string variants = Read(VariantPath);

            foreach (string row in new[]
                     {
                         "[KWEnum(Dissolve, Single Color, _, Gradient, _DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeColorMode(\"Edge Color Mode\", Float) = 0",
                         "[Sub(Dissolve_)] [HDR] _DissolveEdgeColor(\"Edge Color\", Color) = (1, 0.5, 0, 1)",
                         "[UberGradient(Dissolve_DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeGradientColor0(\"Edge Gradient\", Vector) = (1, 0.5, 0, 0)",
                         "[HideInInspector] _DissolveEdgeGradientColor1(\"Edge Gradient Color 1\", Vector) = (1, 0.5, 0, 1)",
                         "[HideInInspector] _DissolveEdgeGradientAlphas(\"Edge Gradient Alphas\", Vector) = (1, 1, 1, 1)",
                         "[HideInInspector] _DissolveEdgeGradientAlphaTimes(\"Edge Gradient Alpha Times\", Vector) = (0, 1, 1, 1)",
                         "[HideInInspector] _DissolveEdgeGradientMetadata(\"Edge Gradient Metadata\", Vector) = (2, 2, 0, 0)",
                     })
                StringAssert.Contains(row, shader);

            string[] gradientPragmas = PragmaRows(shader,
                "_DISSOLVE_EDGE_GRADIENT");
            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _DISSOLVE_EDGE_GRADIENT",
                "#pragma shader_feature_local_fragment _ _DISSOLVE_EDGE_GRADIENT",
            }, gradientPragmas);
            int normalsPass = shader.IndexOf("Name \"NormalsRendering\"",
                StringComparison.Ordinal);
            Assert.That(shader.LastIndexOf("_DISSOLVE_EDGE_GRADIENT",
                    StringComparison.Ordinal), Is.LessThan(normalsPass));
            StringAssert.DoesNotContain("multi_compile_local_fragment _ " +
                "_DISSOLVE_EDGE_GRADIENT", shader);
            Assert.That(Regex.IsMatch(shader,
                @"_DissolveEdgeGradient\w+\([^\r\n]*,\s*2D\)"), Is.False);

            foreach (string field in new[]
                     {
                         "float4 _DissolveEdgeGradientColor0;",
                         "float4 _DissolveEdgeGradientColor1;",
                         "float4 _DissolveEdgeGradientColor2;",
                         "float4 _DissolveEdgeGradientColor3;",
                         "float4 _DissolveEdgeGradientAlphas;",
                         "float4 _DissolveEdgeGradientAlphaTimes;",
                         "float4 _DissolveEdgeGradientMetadata;",
                         "_DissolveEdgeColorMode",
                     })
                StringAssert.Contains(field, include);
            StringAssert.Contains(
                "UberEvaluateDissolveEdgeGradient(1.0h - dissolveEdge)", include);
            StringAssert.Contains(
                "surface.albedo *= UberEvaluateDissolveEdgeMultiplier(dissolveEdge);",
                include);
            StringAssert.Contains("return lerp(half3(1.0h, 1.0h, 1.0h), edgeColor.rgb, strength);",
                include);
            StringAssert.Contains("UberSampleSpriteEmission(float2 baseAtlasUV)", include);
            StringAssert.DoesNotContain("UberSampleSpriteEmission(float2 baseAtlasUV, half dissolveEdge)",
                include);
            Match silhouette = Regex.Match(include,
                @"(?s)inline half UberEvaluateSpriteSilhouette.*?\n\}");
            Assert.That(silhouette.Success, Is.True);
            StringAssert.DoesNotContain("Gradient", silhouette.Value);

            StringAssert.Contains(
                "public sealed class UberGradientDrawer : LWGUI.SubDrawer", gui);
            StringAssert.Contains("EditorGUI.GradientField(fieldPosition, label, gradient,",
                gui);
            StringAssert.Contains("true, ColorSpace.Linear", gui);
            StringAssert.Contains("editor.RegisterPropertyChangeUndo", gui);
            StringAssert.Contains(
                "new KeywordBinding(\"_DissolveEdgeColorMode\", \"_DISSOLVE_EDGE_GRADIENT\", 1,",
                gui);
            string[] rows = Regex.Matches(variants,
                    @"(?m)^\s*- keywords:\s*(?<value>[^\r\n]*_DISSOLVE_EDGE_GRADIENT[^\r\n]*)\r?\n" +
                    @"\s*passType:\s*13\s*$").Cast<Match>()
                .Select(match => match.Groups["value"].Value.Trim()).ToArray();
            CollectionAssert.AreEqual(new[]
                { "_DISSOLVE_EDGE_GRADIENT _DISSOLVE_ON" }, rows);

            Material material = new Material(Shader.Find(SpriteShaderName));
            try
            {
                Assert.That(material.GetFloat("_DissolveEdgeColorMode"), Is.Zero);
                AssertVector(material.GetVector("_DissolveEdgeGradientColor0"),
                    new Vector4(1f, 0.5f, 0f, 0f));
                AssertVector(material.GetVector("_DissolveEdgeGradientColor1"),
                    new Vector4(1f, 0.5f, 0f, 1f));
                AssertVector(material.GetVector("_DissolveEdgeGradientAlphas"),
                    Vector4.one);
                AssertVector(material.GetVector("_DissolveEdgeGradientAlphaTimes"),
                    new Vector4(0f, 1f, 1f, 1f));
                AssertVector(material.GetVector("_DissolveEdgeGradientMetadata"),
                    new Vector4(2f, 2f, 0f, 0f));

                UberShaderGUI inspector = new UberShaderGUI();
                material.SetFloat("_DissolveEnabled", 1f);
                for (int mode = 0; mode < 2; ++mode)
                {
                    material.EnableKeyword("_DISSOLVE_EDGE_GRADIENT");
                    material.SetFloat("_DissolveEdgeColorMode", mode);
                    inspector.ValidateMaterial(material);
                    Assert.That(material.IsKeywordEnabled(
                        "_DISSOLVE_EDGE_GRADIENT"), Is.EqualTo(mode == 1));
                }
                material.SetFloat("_DissolveEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_EDGE_GRADIENT"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void SpriteDissolveEdgeGradientPersistsBlendKeysWithUndoAndLimits()
        {
            MethodInfo findPacked = typeof(UberGradientDrawer).GetMethod(
                "FindPackedProperties", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo readGradient = typeof(UberGradientDrawer).GetMethod(
                "ReadGradient", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo writeGradient = typeof(UberGradientDrawer).GetMethod(
                "TryWriteGradient", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo hasMixed = typeof(UberGradientDrawer).GetMethod(
                "HasMixedValue", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(findPacked, Is.Not.Null);
            Assert.That(readGradient, Is.Not.Null);
            Assert.That(writeGradient, Is.Not.Null);
            Assert.That(hasMixed, Is.Not.Null);

            Material first = new Material(Shader.Find(SpriteShaderName));
            Material second = new Material(Shader.Find(SpriteShaderName));
            MaterialEditor editor = null;
            try
            {
                second.SetVector("_DissolveEdgeGradientColor0",
                    new Vector4(0f, 0f, 0f, 0f));
                MaterialProperty[] properties = MaterialEditor.GetMaterialProperties(
                    new UnityEngine.Object[] { first, second });
                MaterialProperty[] packed = (MaterialProperty[])findPacked.Invoke(null,
                    new object[] { properties });
                Assert.That((bool)hasMixed.Invoke(null, new object[] { packed }), Is.True);

                Gradient authored = new Gradient { mode = GradientMode.Fixed };
                authored.SetKeys(new[]
                {
                    new GradientColorKey(new Color(2f, 0f, 0f), 0f),
                    new GradientColorKey(new Color(0f, 3f, 0f), 0.25f),
                    new GradientColorKey(new Color(0f, 0f, 4f), 0.75f),
                    new GradientColorKey(new Color(2f, 2f, 2f), 1f),
                }, new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.25f, 0.25f),
                    new GradientAlphaKey(0.75f, 0.75f),
                    new GradientAlphaKey(1f, 1f),
                });
                editor = Editor.CreateEditor(new UnityEngine.Object[] { first, second })
                    as MaterialEditor;
                Assert.That(editor, Is.Not.Null);
                int undoGroup = Undo.GetCurrentGroup();
                Assert.That((bool)writeGradient.Invoke(null,
                    new object[] { authored, packed, editor }), Is.True);
                Undo.CollapseUndoOperations(undoGroup);

                MaterialProperty[] firstProperties = MaterialEditor.GetMaterialProperties(
                    new UnityEngine.Object[] { first });
                MaterialProperty[] firstPacked = (MaterialProperty[])findPacked.Invoke(null,
                    new object[] { firstProperties });
                Gradient stored = (Gradient)readGradient.Invoke(null,
                    new object[] { firstPacked });
                Assert.That(stored.mode, Is.EqualTo(GradientMode.Blend));
                Assert.That(stored.colorKeys, Has.Length.EqualTo(4));
                Assert.That(stored.alphaKeys, Has.Length.EqualTo(4));
                Assert.That(stored.colorKeys[1].color.g, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(stored.colorKeys[1].time,
                    Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(stored.alphaKeys[2].alpha, Is.EqualTo(0.75f));
                Assert.That(stored.alphaKeys[2].time,
                    Is.EqualTo(0.75f).Within(0.0001f));
                AssertVector(first.GetVector("_DissolveEdgeGradientColor2"),
                    second.GetVector("_DissolveEdgeGradientColor2"));
                for (int sample = 0; sample <= 4; ++sample)
                {
                    float time = sample * 0.25f;
                    AssertColor(EvaluatePackedEdgeGradient(first, time),
                        stored.Evaluate(time), 0.0002f);
                }

                Vector4 retained = first.GetVector("_DissolveEdgeGradientColor0");
                Gradient excess = new Gradient();
                excess.SetKeys(Enumerable.Range(0, 5).Select(index =>
                        new GradientColorKey(Color.white, index * 0.25f)).ToArray(),
                    new[] { new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f) });
                Assert.That((bool)writeGradient.Invoke(null,
                    new object[] { excess, firstPacked, editor }), Is.False);
                AssertVector(first.GetVector("_DissolveEdgeGradientColor0"), retained);

                first.SetVector("_DissolveEdgeGradientColor0",
                    new Vector4(1f, 0f, 0f, 0.5f));
                first.SetVector("_DissolveEdgeGradientColor1",
                    new Vector4(0f, 0f, 1f, 0.5f));
                first.SetVector("_DissolveEdgeGradientAlphas",
                    new Vector4(0f, 1f, 1f, 1f));
                first.SetVector("_DissolveEdgeGradientAlphaTimes",
                    new Vector4(0.5f, 0.5f, 1f, 1f));
                first.SetVector("_DissolveEdgeGradientMetadata",
                    new Vector4(2f, 2f, 0f, 0f));
                Color coincident = EvaluatePackedEdgeGradient(first, 0.5f);
                Assert.That(IsFinite(coincident), Is.True);

                Undo.PerformUndo();
                AssertVector(first.GetVector("_DissolveEdgeGradientColor0"),
                    new Vector4(1f, 0.5f, 0f, 0f));
                AssertVector(second.GetVector("_DissolveEdgeGradientColor0"),
                    new Vector4(0f, 0f, 0f, 0f));
            }
            finally
            {
                Undo.ClearUndo(first);
                Undo.ClearUndo(second);
                if (editor != null)
                    UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void SpriteDissolveEdgeGradientGpuPreservesSingleAndSilhouette()
        {
            const int width = 32;
            Color previousSpriteColor = Shader.GetGlobalColor("unity_SpriteColor");
            Vector4 previousSpriteProps = Shader.GetGlobalVector("unity_SpriteProps");
            Texture2D source = null;
            Texture2D noise = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                Shader.SetGlobalColor("unity_SpriteColor", Color.white);
                Shader.SetGlobalVector("unity_SpriteProps",
                    new Vector4(1f, 1f, 0f, 0f));
                source = new Texture2D(width, 1, TextureFormat.RGBAFloat,
                    false, true) { filterMode = FilterMode.Point };
                noise = new Texture2D(width, 1, TextureFormat.RGBAFloat,
                    false, true) { filterMode = FilterMode.Point };
                Color[] sourcePixels = new Color[width];
                Color[] noisePixels = new Color[width];
                for (int x = 0; x < width; ++x)
                {
                    sourcePixels[x] = Color.white;
                    float value = (x + 0.5f) / width;
                    noisePixels[x] = new Color(value, value, value, 1f);
                }
                source.SetPixels(sourcePixels);
                source.Apply(false, false);
                noise.SetPixels(noisePixels);
                noise.Apply(false, false);
                readback = new Texture2D(width, 1, TextureFormat.RGBAFloat,
                    false, true);
                target = new RenderTexture(width, 1, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);

                material = new Material(Shader.Find(SpriteShaderName));
                material.SetTexture("_DissolveNoiseMap", noise);
                material.SetVector("_BaseSpriteUVRect", new Vector4(0f, 0f, 1f, 1f));
                material.SetVector("_DissolveTilingOffset",
                    new Vector4(1f, 1f, 0f, 0f));
                material.SetVector("_DissolvePanning", Vector4.zero);
                material.SetVector("_DissolveNoiseRange", new Vector4(0f, 1f, 0f, 0f));
                material.SetColor("_BaseColor", Color.white);
                material.SetColor("_Color", Color.white);
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_LightingMode", 1f);
                material.SetFloat("_DissolveEnabled", 1f);
                material.SetFloat("_DissolveAmount", 0.2f);
                material.SetFloat("_DissolveEdgeWidth", 0.8f);
                material.SetFloat("_DissolveEdgeIntensity", 0f);
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_SrcBlendAlpha", 1f);
                material.SetFloat("_DstBlendAlpha", 0f);
                material.EnableKeyword("_UNLIT_ON");
                material.EnableKeyword("_DISSOLVE_ON");

                Color[] identity = RenderSpriteDissolve(source, material, target,
                    readback);
                material.SetFloat("_DissolveEdgeIntensity", 1f);
                material.SetColor("_DissolveEdgeColor", new Color(1f, 0.5f, 0f));
                Color[] single = RenderSpriteDissolve(source, material, target,
                    readback);
                Assert.That(single[8].r, Is.LessThanOrEqualTo(identity[8].r + 0.002f));
                Assert.That(single[8].g, Is.LessThan(identity[8].g - 0.1f));
                Assert.That(single[8].b, Is.LessThan(identity[8].b - 0.1f));
                material.EnableKeyword("_DISSOLVE_EDGE_GRADIENT");
                Color[] flatGradient = RenderSpriteDissolve(source, material,
                    target, readback);
                Assert.That(MaxRgbDifference(single, flatGradient),
                    Is.LessThan(0.002f));
                CollectionAssert.AreEqual(single.Select(color => (Color32)color),
                    flatGradient.Select(color => (Color32)color));

                material.SetVector("_DissolveEdgeGradientColor0",
                    new Vector4(1f, 0f, 0f, 0f));
                material.SetVector("_DissolveEdgeGradientColor1",
                    new Vector4(0f, 0f, 1f, 1f));
                Color[] fullAlpha = RenderSpriteDissolve(source, material, target,
                    readback);
                Color[] fullAlphaRepeat = RenderSpriteDissolve(source, material,
                    target, readback);
                CollectionAssert.AreEqual(
                    fullAlpha.Select(color => (Color32)color),
                    fullAlphaRepeat.Select(color => (Color32)color));
                float startRedTint = Mathf.Abs(identity[8].b - fullAlpha[8].b);
                float startBlueTint = Mathf.Abs(identity[8].r - fullAlpha[8].r);
                float startRatio = startRedTint /
                    Mathf.Max(startRedTint + startBlueTint, 0.0001f);
                float middleRedTint = Mathf.Abs(identity[19].b - fullAlpha[19].b);
                float middleBlueTint = Mathf.Abs(identity[19].r - fullAlpha[19].r);
                float middleRatio = middleRedTint /
                    Mathf.Max(middleRedTint + middleBlueTint, 0.0001f);
                float endRedTint = Mathf.Abs(identity[29].b - fullAlpha[29].b);
                float endBlueTint = Mathf.Abs(identity[29].r - fullAlpha[29].r);
                float endRatio = endRedTint /
                    Mathf.Max(endRedTint + endBlueTint, 0.0001f);
                Assert.That(startRatio, Is.GreaterThan(middleRatio));
                Assert.That(middleRatio, Is.GreaterThan(endRatio));
                Assert.That(startRatio, Is.GreaterThan(0.7f));
                Assert.That(endRatio, Is.LessThan(0.3f));

                material.SetVector("_DissolveEdgeGradientAlphas",
                    new Vector4(0f, 1f, 1f, 1f));
                Color[] alphaGradient = RenderSpriteDissolve(source, material,
                    target, readback);
                float fullTint = Mathf.Abs(fullAlpha[8].r - identity[8].r) +
                    Mathf.Abs(fullAlpha[8].g - identity[8].g) +
                    Mathf.Abs(fullAlpha[8].b - identity[8].b);
                float alphaTint = Mathf.Abs(alphaGradient[8].r - identity[8].r) +
                    Mathf.Abs(alphaGradient[8].g - identity[8].g) +
                    Mathf.Abs(alphaGradient[8].b - identity[8].b);
                Assert.That(alphaTint, Is.LessThan(fullTint * 0.25f));
                for (int index = 0; index < width; ++index)
                    Assert.That(alphaGradient[index].a,
                        Is.EqualTo(fullAlpha[index].a).Within(0.0001f),
                        "silhouette pixel " + index);
            }
            finally
            {
                Shader.SetGlobalColor("unity_SpriteColor", previousSpriteColor);
                Shader.SetGlobalVector("unity_SpriteProps", previousSpriteProps);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
                if (noise != null)
                    UnityEngine.Object.DestroyImmediate(noise);
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        [Test]
        public void SpriteBinderUsesPropertyBlockAndPreservesSharedMaterialAndFlip()
        {
            Texture2D texture = new Texture2D(8, 8);
            Sprite baseSprite = Sprite.Create(texture, new Rect(0, 0, 4, 8),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Sprite replacement = Sprite.Create(texture, new Rect(0, 0, 8, 4),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Sprite secondary = Sprite.Create(texture, new Rect(4, 0, 4, 8),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Material material = new Material(Shader.Find(SpriteShaderName));
            GameObject gameObject = new GameObject("Uber Sprite Binder Test");
            try
            {
                SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = material;
                renderer.sprite = baseSprite;
                renderer.flipX = true;
                renderer.flipY = true;
                int sentinelId = Shader.PropertyToID("_UberTestSentinel");
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetFloat(sentinelId, 17f);
                renderer.SetPropertyBlock(block);

                UberSpritePropertyBinder binder =
                    gameObject.AddComponent<UberSpritePropertyBinder>();
                binder.SecondarySprite = secondary;
                binder.SecondaryBlendAmount = 0.75f;
                binder.Refresh();

                renderer.GetPropertyBlock(block);
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                Assert.That(renderer.flipX, Is.True);
                Assert.That(renderer.flipY, Is.True);
                Assert.That(block.GetFloat(sentinelId), Is.EqualTo(17f));
                Assert.That(block.GetTexture("_MainTex"), Is.SameAs(texture));
                Assert.That(block.GetTexture("_SecondaryTex"), Is.SameAs(texture));
                AssertVector(block.GetVector("_BaseSpriteUVRect"),
                    new Vector4(0f, 0f, 0.5f, 1f));
                AssertVector(block.GetVector("_SecondaryUVRect"),
                    new Vector4(0.5f, 0f, 0.5f, 1f));
                Assert.That(block.GetFloat("_SecondaryBlendAmount"),
                    Is.EqualTo(0.75f).Within(0.0001f));

                renderer.sprite = replacement;
                binder.SecondarySprite = null;
                binder.Refresh();
                renderer.GetPropertyBlock(block);
                AssertVector(block.GetVector("_BaseSpriteUVRect"),
                    new Vector4(0f, 0f, 1f, 0.5f));
                AssertVector(block.GetVector("_SecondaryUVRect"),
                    new Vector4(0f, 0f, 1f, 1f));
                Assert.That(block.GetTexture("_SecondaryTex"),
                    Is.SameAs(Texture2D.whiteTexture));
                Assert.That(block.GetFloat("_SecondaryBlendAmount"), Is.EqualTo(0f));
                Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(baseSprite);
                UnityEngine.Object.DestroyImmediate(replacement);
                UnityEngine.Object.DestroyImmediate(secondary);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Color EvaluatePackedEdgeGradient(Material material,
            float time)
        {
            int colorCount = Mathf.Clamp(Mathf.RoundToInt(material.GetVector(
                "_DissolveEdgeGradientMetadata").x), 1, 4);
            Vector4 colorKey = material.GetVector("_DissolveEdgeGradientColor0");
            Color color = new Color(colorKey.x, colorKey.y, colorKey.z, 1f);
            for (int index = 1; index < colorCount; ++index)
            {
                Vector4 next = material.GetVector(
                    "_DissolveEdgeGradientColor" + index);
                float weight = SafeInverseLerp(colorKey.w, next.w, time);
                color = Color.LerpUnclamped(color,
                    new Color(next.x, next.y, next.z, 1f), weight);
                colorKey = next;
            }

            Vector4 alphaValues = material.GetVector(
                "_DissolveEdgeGradientAlphas");
            Vector4 alphaTimes = material.GetVector(
                "_DissolveEdgeGradientAlphaTimes");
            int alphaCount = Mathf.Clamp(Mathf.RoundToInt(material.GetVector(
                "_DissolveEdgeGradientMetadata").y), 1, 4);
            float alpha = alphaValues.x;
            for (int index = 1; index < alphaCount; ++index)
                alpha = Mathf.LerpUnclamped(alpha, alphaValues[index],
                    SafeInverseLerp(alphaTimes[index - 1], alphaTimes[index], time));
            color.a = alpha;
            return color;
        }

        private static float SafeInverseLerp(float lower, float upper, float value)
        {
            float range = upper - lower;
            if (Mathf.Abs(range) < 0.0001f)
                range = range < 0f ? -0.0001f : 0.0001f;
            return Mathf.Clamp01((value - lower) / range);
        }

        private static void AssertColor(Color actual, Color expected, float tolerance)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

    }
}
