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
        public void UiDissolveMatchesSpriteAtlasLocalModeRangeAndEdgeContract()
        {
            string shader = Read(UberDirectory + "UberUI.shader");
            string include = Read(UberDirectory + "UberUI.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");

            foreach (string row in new[]
                     {
                         "[Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap (\"Noise Map\", 2D) = \"white\" {}",
                         "[Sub(Dissolve)] _DissolveTilingOffset (\"Tiling XY / Offset ZW\", Vector) = (1,1,0,0)",
                         "[Sub(Dissolve)] _DissolvePanning (\"Panning XY\", Vector) = (0,0,0,0)",
                         "[KWEnum(Dissolve, Noise, _, Radial, _DISSOLVE_RADIAL, Swipe, _DISSOLVE_SWIPE)] _DissolveMode (\"Mode\", Float) = 0",
                         "[UberMinMaxVector(Dissolve_)] _DissolveNoiseRange (\"Noise Range\", Vector) = (0,1,0,0)",
                         "[UberVector2(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialCenter (\"Radial Center\", Vector) = (0.5,0.5,0,0)",
                         "[UberMinMaxVector(Dissolve_DISSOLVE_RADIAL, _DissolveAmount)] _DissolveRadialRange (\"Radial Range\", Vector) = (0,0.7071,0,0)",
                         "[Sub(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialNoiseStrength (\"Radial Noise Strength\", Range(0,1)) = 0.15",
                         "[UberVector2(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeCenter (\"Swipe Center\", Vector) = (0.5,0.5,0,0)",
                         "[Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRotation (\"Swipe Rotation\", Range(-180,180)) = 0",
                         "[UberMinMaxVector(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRange (\"Swipe Range\", Vector) = (-0.5,0.5,0,0)",
                         "[Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeNoiseStrength (\"Swipe Noise Strength\", Range(0,1)) = 0.15",
                         "[KWEnum(Dissolve, Single Color, _, Gradient, _DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeColorMode (\"Edge Color Mode\", Float) = 0",
                         "[UberGradient(Dissolve_DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeGradientColor0 (\"Edge Gradient\", Vector) = (1,0.5,0,0)",
                     })
            {
                StringAssert.Contains(row, shader);
            }

            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE",
            }, PragmaRows(shader, "_DISSOLVE_SWIPE"));
            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _DISSOLVE_EDGE_GRADIENT",
            }, PragmaRows(shader, "_DISSOLVE_EDGE_GRADIENT"));

            foreach (string field in new[]
                     {
                         "float4 _DissolveTilingOffset;",
                         "float4 _DissolvePanning;",
                         "float4 _DissolveNoiseRange;",
                         "float4 _DissolveRadialCenter;",
                         "float4 _DissolveRadialRange;",
                         "float4 _DissolveSwipeCenter;",
                         "float4 _DissolveSwipeRange;",
                         "half _DissolveRadialNoiseStrength;",
                         "float _DissolveSwipeRotation;",
                         "half _DissolveSwipeNoiseStrength;",
                         "float4 _DissolveEdgeGradientMetadata;",
                     })
            {
                StringAssert.Contains(field, include);
            }

            foreach (string contract in new[]
                     {
                         "saturate(UberNormalizeUV(uv, _BaseSpriteUVRect))",
                         "UberSafeInverseLerp(_DissolveNoiseRange.x,",
                         "UberSafeInverseLerp(_DissolveRadialRange.x,",
                         "length(localUV - _DissolveRadialCenter.xy)",
                         "radians(fmod(_DissolveSwipeRotation, 360.0))",
                         "dot(localUV - _DissolveSwipeCenter.xy, direction)",
                         "UberSafeInverseLerp(_DissolveSwipeRange.x,",
                         "half threshold = saturate(_DissolveAmount);",
                         "clip(dissolveValue - threshold);",
                         "color.rgb *= UberUIEvaluateDissolveEdgeMultiplier(dissolveEdge);",
                     })
            {
                StringAssert.Contains(contract, include);
            }
            foreach (string removedContract in new[]
                     {
                         "TEXTURE2D(_DissolveTex);",
                         "float4 _DissolveTiling;",
                         "float4 _DissolveScroll;",
                         "_DissolveSoftness",
                         "color.rgb += _DissolveEdgeColor",
                         "silhouette *= dissolve;",
                     })
            {
                StringAssert.DoesNotContain(removedContract, shader + include);
            }

            StringAssert.Contains(
                "new KeywordBinding(\"_DissolveMode\", \"_DISSOLVE_SWIPE\", 2,",
                gui);
            StringAssert.Contains(
                "new KeywordBinding(\"_DissolveEdgeColorMode\", " +
                "\"_DISSOLVE_EDGE_GRADIENT\", 1,", gui);

            Material material = new Material(Shader.Find(UIShaderName));
            try
            {
                Assert.That(material.HasProperty("_DissolveTex"), Is.False);
                Assert.That(material.HasProperty("_DissolveSoftness"), Is.False);
                AssertVector(material.GetVector("_DissolveNoiseRange"),
                    new Vector4(0f, 1f, 0f, 0f));
                AssertVector(material.GetVector("_DissolveRadialRange"),
                    new Vector4(0f, 0.7071f, 0f, 0f));
                AssertVector(material.GetVector("_DissolveSwipeCenter"),
                    new Vector4(0.5f, 0.5f, 0f, 0f));
                AssertVector(material.GetVector("_DissolveSwipeRange"),
                    new Vector4(-0.5f, 0.5f, 0f, 0f));

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

                material.SetFloat("_DissolveEdgeColorMode", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_EDGE_GRADIENT"),
                    Is.True);
                material.SetFloat("_DissolveEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_RADIAL"), Is.False);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_SWIPE"), Is.False);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_EDGE_GRADIENT"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void UiLightSweepMatchesSpriteAtlasLocalProfilesAndBlendModes()
        {
            string shader = Read(UberDirectory + "UberUI.shader");
            string include = Read(UberDirectory + "UberUI.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            string variants = Read(VariantPath);

            foreach (string row in new[]
                     {
                         "[Main(LightSweep, _LIGHT_SWEEP_ON, on)] _LightSweepEnabled (\"Light Sweep\", Float) = 0",
                         "[Title(LightSweep, _)] [KWEnum(LightSweep, Soft, _, Sharp, _LIGHT_SWEEP_SHARP)] _LightSweepMode (\"Type\", Float) = 0",
                         "[KWEnum(LightSweep, Add, _, Multiply, _LIGHT_SWEEP_MULTIPLY)] _LightSweepBlendMode (\"Blend Mode\", Float) = 0",
                         "[Sub(LightSweep)] [HDR] _LightSweepColor (\"Color\", Color) = (1,1,1,1)",
                         "[Sub(LightSweep)] _LightSweepIntensity (\"Intensity\", Range(0,16)) = 2",
                         "[Sub(LightSweep)] _LightSweepAmount (\"Amount\", Range(0,1)) = 0",
                         "[UberVector2(LightSweep)] _LightSweepCenter (\"Center\", Vector) = (0.5,0.5,0,0)",
                         "[Sub(LightSweep)] _LightSweepRotation (\"Rotation\", Range(-180,180)) = 0",
                         "[UberMinMaxVector(LightSweep)] _LightSweepRange (\"Range\", Vector) = (-0.5,0.5,0,0)",
                         "[Sub(LightSweep)] _LightSweepWidth (\"Width\", Range(0.001,1)) = 0.15",
                     })
                StringAssert.Contains(row, shader);

            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _LIGHT_SWEEP_ON",
            }, PragmaRows(shader, "_LIGHT_SWEEP_ON"));
            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _LIGHT_SWEEP_SHARP",
            }, PragmaRows(shader, "_LIGHT_SWEEP_SHARP"));
            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _LIGHT_SWEEP_MULTIPLY",
            }, PragmaRows(shader, "_LIGHT_SWEEP_MULTIPLY"));
            StringAssert.DoesNotContain(
                "#pragma multi_compile_local_fragment _ _LIGHT_SWEEP", shader);

            foreach (string field in new[]
                     {
                         "float4 _LightSweepCenter;",
                         "float4 _LightSweepRange;",
                         "half4 _LightSweepColor;",
                         "half _LightSweepAmount;",
                         "float _LightSweepRotation;",
                         "half _LightSweepWidth;",
                         "half _LightSweepIntensity;",
                         "half _LightSweepEnabled;",
                         "half _LightSweepMode;",
                         "half _LightSweepBlendMode;",
                     })
                StringAssert.Contains(field, include);
            foreach (string contract in new[]
                     {
                         "radians(fmod(_LightSweepRotation, 360.0))",
                         "dot(localUV - _LightSweepCenter.xy, direction)",
                         "lerp(_LightSweepRange.x, _LightSweepRange.y,",
                         "1.0h - smoothstep(0.0, halfWidth, distanceToSweep)",
                         "halfWidth - edgeAA,",
                         "albedo *= 1.0h + sweepColor * influence;",
                         "emission += sweepColor * influence;",
                         "float2 localEffectUV = saturate(UberNormalizeUV(effectUV,",
                         "UberUIApplyLightSweep(localEffectUV, color.a, color.rgb, emission);",
                     })
                StringAssert.Contains(contract, include);
            StringAssert.Contains(
                "new KeywordBinding(\"_LightSweepEnabled\", \"_LIGHT_SWEEP_ON\", 1)",
                gui);

            int uiVariantsStart = variants.IndexOf(
                "guid: 1aad80d3fa14854488c67ee35f470633", StringComparison.Ordinal);
            int particleVariantsStart = variants.IndexOf(
                "guid: 23426744f12288344b3e94900b3f7cc9", StringComparison.Ordinal);
            Assert.That(uiVariantsStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(particleVariantsStart, Is.GreaterThan(uiVariantsStart));
            string uiVariants = variants.Substring(uiVariantsStart,
                particleVariantsStart - uiVariantsStart);
            string[] serialized = Regex.Matches(uiVariants,
                    @"(?m)^\s*- keywords:\s*(?<value>[^\r\n]*_LIGHT_SWEEP[^\r\n]*)\r?\n" +
                    @"\s*passType:\s*14\s*$").Cast<Match>()
                .Select(match => match.Groups["value"].Value.Trim()).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "_LIGHT_SWEEP_MULTIPLY _LIGHT_SWEEP_ON",
                "_LIGHT_SWEEP_MULTIPLY _LIGHT_SWEEP_ON _LIGHT_SWEEP_SHARP",
                "_LIGHT_SWEEP_ON",
                "_LIGHT_SWEEP_ON _LIGHT_SWEEP_SHARP",
            }, serialized);

            Material material = new Material(Shader.Find(UIShaderName));
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
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_SHARP"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_MULTIPLY"),
                    Is.False);

                material.SetFloat("_LightSweepMode", 1f);
                material.SetFloat("_LightSweepBlendMode", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_LIGHT_SWEEP_SHARP"),
                    Is.True);
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
        public void UiLightSweepGpuMovesRotatesChangesProfileAndBlendMode()
        {
            const int size = 64;
            Vector4 previousTextureSampleAdd =
                Shader.GetGlobalVector("_TextureSampleAdd");
            Texture2D source = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                Shader.SetGlobalVector("_TextureSampleAdd", Vector4.zero);
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

                material = new Material(Shader.Find(UIShaderName));
                material.SetColor("_Color", Color.white);
                material.SetFloat("_AlphaMultiplier", 1f);
                material.SetVector("_BaseSpriteUVRect",
                    new Vector4(0f, 0f, 1f, 1f));
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
                material.EnableKeyword("_LIGHT_SWEEP_ON");

                material.SetFloat("_LightSweepAmount", 0f);
                Color[] left = RenderSpriteDissolve(source, material, target,
                    readback);
                material.SetFloat("_LightSweepAmount", 1f);
                Color[] right = RenderSpriteDissolve(source, material, target,
                    readback);
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
                Shader.SetGlobalVector("_TextureSampleAdd",
                    previousTextureSampleAdd);
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
        public void UiHologramAndGlitchAreIndependentAtlasSafeEffects()
        {
            string shader = Read(UberDirectory + "UberUI.shader");
            string include = Read(UberDirectory + "UberUI.hlsl");
            string variants = Read(VariantPath);

            foreach (string row in new[]
                     {
                         "[Main(Hologram, _HOLOGRAM_ON, on)] _HologramEnabled (\"Hologram\", Float) = 0",
                         "[Title(Hologram, _)] [Sub(Hologram)] [HDR] _HologramColor (\"Color\", Color) = (0,1,1,1)",
                         "[Sub(Hologram)] _HologramOpacity (\"Opacity\", Range(0,1)) = 0.35",
                         "[Sub(Hologram)] _HologramFresnelPower (\"Edge Width (Pixels)\", Range(0.5,16)) = 4",
                         "[Sub(Hologram)] _HologramFresnelIntensity (\"Edge Intensity\", Range(0,16)) = 2",
                         "[Sub(Hologram)] _HologramEdgeSoftnessPixels (\"Edge Softness (Pixels)\", Range(0,32)) = 8",
                         "[KWEnum(Hologram, Object, _, World, _HOLOGRAM_WORLD_SPACE, Screen, _HOLOGRAM_SCREEN_SPACE)] _HologramSpace (\"Space\", Float) = 0",
                         "[UberVector3(Hologram)] _HologramObjectUpVector (\"Object Up Vector\", Vector) = (0,1,0,0)",
                         "[Sub(Hologram)] _HologramScanlineDensity (\"Scanline Density\", Range(0.1,128)) = 24",
                         "[Sub(Hologram)] _HologramScanlineSpeed (\"Scanline Speed\", Range(-10,10)) = 1",
                         "[Sub(Hologram)] _HologramScanlineWidth (\"Scanline Width\", Range(0.01,1)) = 0.12",
                         "[Sub(Hologram)] _HologramScanlineIntensity (\"Scanline Intensity\", Range(0,16)) = 2",
                         "[Sub(Hologram)] _HologramNoiseScale (\"Noise Scale\", Range(0.01,64)) = 4",
                         "[Sub(Hologram)] _HologramNoiseStrength (\"Noise Strength\", Range(0,2)) = 0.35",
                         "[Sub(Hologram)] _HologramNoiseSpeed (\"Noise Speed\", Range(-10,10)) = 0.5",
                         "[Main(Glitch, _GLITCH_ON, on)] _GlitchEnabled (\"Glitch\", Float) = 0",
                         "[Title(Glitch, _)] [Sub(Glitch)] _GlitchStrength (\"Strength (Pixels)\", Range(0,64)) = 8",
                         "[Sub(Glitch)] _GlitchRGBSplit (\"RGB Split (Pixels)\", Range(0,16)) = 2",
                         "[Sub(Glitch)] _GlitchFrequency (\"Frequency\", Range(0,1)) = 0.25",
                         "[Sub(Glitch)] _GlitchSpeed (\"Speed\", Range(0,30)) = 8",
                         "[UberMinMaxVector(Glitch)] _GlitchBandSizeRange (\"Band Size Range (Pixels)\", Vector) = (4,12,0,0)",
                         "[KWEnum(SurfaceInputs, High, _, Low, _UBER_QUALITY_LOW)] _UberQuality (\"Effect Quality\", Float) = 0",
                     })
            {
                StringAssert.Contains(row, shader);
            }

            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _HOLOGRAM_ON",
            }, PragmaRows(shader, "_HOLOGRAM_ON"));
            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _HOLOGRAM_WORLD_SPACE _HOLOGRAM_SCREEN_SPACE",
            }, PragmaRows(shader, "_HOLOGRAM_WORLD_SPACE"));
            CollectionAssert.AreEqual(new[]
            {
                "#pragma shader_feature_local_fragment _ _GLITCH_ON",
            }, PragmaRows(shader, "_GLITCH_ON"));
            CollectionAssert.AreEqual(new[]
            {
                "#pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW",
            }, PragmaRows(shader, "_UBER_QUALITY_LOW"));
            StringAssert.DoesNotContain(
                "multi_compile_local_fragment _ _HOLOGRAM", shader);

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
                         "half _GlitchEnabled;",
                         "half _GlitchStrength;",
                         "half _GlitchRGBSplit;",
                         "half _GlitchFrequency;",
                         "half _GlitchSpeed;",
                         "float4 _GlitchBandSizeRange;",
                         "float3 positionWS : TEXCOORD2;",
                     })
            {
                StringAssert.Contains(field, include);
            }

            foreach (string contract in new[]
                     {
                         "UberSampleUISpriteAlpha(rawUV +",
                         "const int edgeCoarseSteps = 4;",
                         "const int edgeRefinementSteps = 3;",
                         "const float diagonal = 0.70710678;",
                         "half softMask = 1.0h - smoothstep(edgeWidth, searchRadius, searchFar);",
                         "float2 localUV = saturate(UberNormalizeUV(rawUV, _BaseSpriteUVRect));",
                         "return dot(float3(localUV - 0.5, 0.0),",
                         "return positionWS.y;",
                         "GetNormalizedScreenSpaceUV(positionCS).y",
                         "UberUIHologramValueNoise(noiseCoordinate)",
                         "float2 effectUV = UberUIApplyGlitchUV(input.uv,",
                         "#if defined(_GLITCH_ON)",
                         "float minBandSize = clamp(min(_GlitchBandSizeRange.x,",
                         "float maxBandSize = clamp(max(_GlitchBandSizeRange.x,",
                         "inline float UberUIGlitchBandBoundary(float boundaryIndex, float frame,",
                         "UberEvaluateGlitchBandBoundary(boundaryIndex, frame,",
                         "float averageBandSize = (minBandSize + maxBandSize) * 0.5;",
                         "float bandSizeVariation = maxBandSize - minBandSize;",
                         "bandIndex += step(upperBoundary, pixelY);",
                         "bandIndex -= 1.0 - step(lowerBoundary, pixelY);",
                         "float activation = step(1.0 - saturate(_GlitchFrequency),",
                         "shiftPixels * abs(_MainTex_TexelSize.x)",
                         "if (glitchActivation > 0.0h && _GlitchRGBSplit > 0.0001h)",
                         "center.r = UberSampleUISprite(effectUV + splitUV).r;",
                         "center.b = UberSampleUISprite(effectUV - splitUV).b;",
                         "sprite = UberUIApplyGlitchRGBSplit(effectUV, sprite,",
                         "max(fwidth(phase), 0.0001)",
                         "color.rgb *= saturate(_HologramColor.rgb);",
                         "color.a *= saturate(_HologramOpacity);",
                         "half hologramEdge = UberUIEvaluateHologramEdge(effectUV, sprite.a);",
                         "UberUIDissolve(effectUV, dissolveEdge);",
                         "UberUIOutlineMasks(effectUV, sprite.a, outline, glow);",
                         "half3 emission = UberUIEvaluateHologramEmission(hologramEdge,",
                         "color.rgb += emission;",
                     })
            {
                StringAssert.Contains(contract, include);
            }

            int uiVariantsStart = variants.IndexOf(
                "guid: 1aad80d3fa14854488c67ee35f470633", StringComparison.Ordinal);
            int particleVariantsStart = variants.IndexOf(
                "guid: 23426744f12288344b3e94900b3f7cc9", StringComparison.Ordinal);
            Assert.That(uiVariantsStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(particleVariantsStart, Is.GreaterThan(uiVariantsStart));
            string uiVariants = variants.Substring(uiVariantsStart,
                particleVariantsStart - uiVariantsStart);
            string[] serialized = Regex.Matches(uiVariants,
                    @"(?m)^\s*- keywords:\s*(?<value>[^\r\n]*_HOLOGRAM[^\r\n]*)\r?\n" +
                    @"\s*passType:\s*14\s*$").Cast<Match>()
                .Select(match => match.Groups["value"].Value.Trim()).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "_GLITCH_ON _HOLOGRAM_ON",
                "_HOLOGRAM_ON",
                "_HOLOGRAM_ON _HOLOGRAM_SCREEN_SPACE",
                "_HOLOGRAM_ON _HOLOGRAM_WORLD_SPACE",
            }, serialized);
            Assert.That(Regex.IsMatch(uiVariants,
                @"(?m)^\s*- keywords: _GLITCH_ON\r?$"), Is.True);

            Material material = new Material(Shader.Find(UIShaderName));
            try
            {
                Assert.That(material.GetFloat("_HologramEnabled"), Is.Zero);
                Assert.That(material.GetFloat("_GlitchEnabled"), Is.Zero);
                Assert.That(material.GetFloat("_GlitchRGBSplit"),
                    Is.EqualTo(2f));
                AssertVector(material.GetVector("_GlitchBandSizeRange"),
                    new Vector4(4f, 12f, 0f, 0f));
                Assert.That(material.GetFloat("_HologramSpace"), Is.Zero);
                Assert.That(material.GetFloat("_UberQuality"), Is.Zero);
                Assert.That(material.GetColor("_HologramColor"),
                    Is.EqualTo(new Color(0f, 1f, 1f, 1f)));
                AssertVector(material.GetVector("_HologramObjectUpVector"),
                    new Vector4(0f, 1f, 0f, 0f));

                UberShaderGUI inspector = new UberShaderGUI();
                material.SetFloat("_GlitchEnabled", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.False);

                material.SetFloat("_HologramEnabled", 1f);
                for (int mode = 0; mode < 3; ++mode)
                {
                    material.EnableKeyword("_HOLOGRAM_WORLD_SPACE");
                    material.EnableKeyword("_HOLOGRAM_SCREEN_SPACE");
                    material.SetFloat("_HologramSpace", mode);
                    inspector.ValidateMaterial(material);
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.True);
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_WORLD_SPACE"),
                        Is.EqualTo(mode == 1));
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_SCREEN_SPACE"),
                        Is.EqualTo(mode == 2));
                }

                material.SetFloat("_UberQuality", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_UBER_QUALITY_LOW"), Is.True);
                material.SetFloat("_HologramEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_WORLD_SPACE"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_SCREEN_SPACE"),
                    Is.False);
                material.SetFloat("_GlitchEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void UiBinderOwnsOneMaterialPreservesStencilAndCleansUp()
        {
            Texture2D texture = new Texture2D(8, 8);
            Sprite sprite = Sprite.Create(texture, new Rect(2, 0, 4, 8),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Material baseMaterial = new Material(Shader.Find(UIShaderName));
            Material stencilMaterial = null;
            GameObject gameObject = new GameObject("Uber UI Binder Test",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                Image image = gameObject.GetComponent<Image>();
                image.rectTransform.sizeDelta = new Vector2(100f, 100f);
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.useSpriteMesh = false;
                image.material = baseMaterial;
                baseMaterial.SetFloat("_PixelOutlineEnabled", 1f);
                baseMaterial.SetFloat("_PixelOutlineWidth", 1f);
                baseMaterial.SetFloat("_PixelGlowWidth", 2f);
                baseMaterial.EnableKeyword("UNITY_UI_CLIP_RECT");

                UberUIMaterialBinder binder =
                    gameObject.AddComponent<UberUIMaterialBinder>();
                Material first = binder.GetModifiedMaterial(baseMaterial);
                Material second = binder.GetModifiedMaterial(baseMaterial);
                Assert.That(first, Is.Not.SameAs(baseMaterial));
                Assert.That(second, Is.SameAs(first));
                AssertVector(first.GetVector("_BaseSpriteUVRect"),
                    new Vector4(0.25f, 0f, 0.5f, 1f));
                AssertVector(baseMaterial.GetVector("_BaseSpriteUVRect"),
                    new Vector4(0f, 0f, 1f, 1f));

                stencilMaterial = StencilMaterial.Add(baseMaterial, 5,
                    StencilOp.Replace, CompareFunction.Equal, ColorWriteMask.All,
                    0x7f, 0x40);
                Assert.That(stencilMaterial, Is.Not.SameAs(baseMaterial));
                Assert.That(stencilMaterial.GetFloat("_Stencil"), Is.EqualTo(5f));
                Assert.That(stencilMaterial.GetFloat("_StencilOp"),
                    Is.EqualTo((float)StencilOp.Replace));
                Assert.That(stencilMaterial.GetFloat("_StencilComp"),
                    Is.EqualTo((float)CompareFunction.Equal));
                Assert.That(stencilMaterial.GetFloat("_StencilReadMask"),
                    Is.EqualTo(0x7f));
                Assert.That(stencilMaterial.GetFloat("_StencilWriteMask"),
                    Is.EqualTo(0x40));
                Assert.That(stencilMaterial.GetFloat("_UseUIAlphaClip"),
                    Is.EqualTo(1f));
                Assert.That(stencilMaterial.IsKeywordEnabled("UNITY_UI_ALPHACLIP"),
                    Is.True);

                Material owned = binder.GetModifiedMaterial(stencilMaterial);
                Assert.That(first == null, Is.True, "Replaced owned material leaked");
                Assert.That(owned, Is.Not.SameAs(stencilMaterial));
                Vector4 initialPadding = owned.GetVector("_PixelOutlineMeshPadding");
                Assert.That(initialPadding.z, Is.EqualTo(0.25f).Within(0.0001f));

                int initialCrc = baseMaterial.ComputeCRC();
                baseMaterial.SetFloat("_AlphaMultiplier", 0.35f);
                baseMaterial.SetFloat("_PixelOutlineWidth", 3f);
                baseMaterial.SetFloat("_PixelGlowWidth", 6f);
                Assert.That(baseMaterial.ComputeCRC(), Is.Not.EqualTo(initialCrc));
                Assert.That(stencilMaterial.GetFloat("_AlphaMultiplier"),
                    Is.EqualTo(1f), "Stencil cache unexpectedly refreshed itself");
                Assert.That(stencilMaterial.GetFloat("_PixelGlowWidth"),
                    Is.EqualTo(2f), "Stencil cache unexpectedly refreshed itself");
                Assert.That(stencilMaterial.GetFloat("_PixelOutlineWidth"),
                    Is.EqualTo(1f), "Stencil cache unexpectedly refreshed itself");

                InvokeNonPublic(binder, "LateUpdate");
                Material refreshed = binder.GetModifiedMaterial(stencilMaterial);
                Assert.That(refreshed, Is.SameAs(owned));
                Assert.That(refreshed.GetFloat("_AlphaMultiplier"),
                    Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(refreshed.GetFloat("_PixelGlowWidth"), Is.EqualTo(6f));
                Assert.That(refreshed.GetFloat("_PixelOutlineWidth"), Is.EqualTo(3f));
                Assert.That(refreshed.GetVector("_PixelOutlineMeshPadding").z,
                    Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(refreshed.GetFloat("_Stencil"), Is.EqualTo(5f));
                Assert.That(refreshed.GetFloat("_StencilOp"),
                    Is.EqualTo((float)StencilOp.Replace));
                Assert.That(refreshed.GetFloat("_StencilComp"),
                    Is.EqualTo((float)CompareFunction.Equal));
                Assert.That(refreshed.GetFloat("_StencilReadMask"),
                    Is.EqualTo(0x7f));
                Assert.That(refreshed.GetFloat("_StencilWriteMask"),
                    Is.EqualTo(0x40));
                Assert.That(refreshed.GetFloat("_UseUIAlphaClip"), Is.EqualTo(1f));
                Assert.That(refreshed.IsKeywordEnabled("UNITY_UI_CLIP_RECT"), Is.True);
                Assert.That(refreshed.IsKeywordEnabled("UNITY_UI_ALPHACLIP"), Is.True);

                binder.enabled = false;
                Assert.That(owned == null, Is.True, "Owned material survived disable");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                if (stencilMaterial != null)
                    StencilMaterial.Remove(stencilMaterial);
                UnityEngine.Object.DestroyImmediate(baseMaterial);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }

            string binderSource = Read(
                "Assets/01. Scripts/UI/UberUIMaterialBinder.cs");
            StringAssert.Contains("image.type != Image.Type.Simple", binderSource);
            StringAssert.Contains("image.useSpriteMesh", binderSource);
            StringAssert.Contains("MaximumPaddingPixels", binderSource);
            StringAssert.Contains("ComputeCRC()", binderSource);
            StringAssert.Contains("ReleaseModifiedMaterial();", binderSource);
            Match onDestroy = Regex.Match(binderSource,
                @"(?s)protected override void OnDestroy\(\)\s*\{(?<body>.*?)\}");
            StringAssert.Contains("ReleaseModifiedMaterial();",
                onDestroy.Groups["body"].Value);
        }

        [Test]
        public void UiBinderReleasesOwnedMaterialWhenDestroyedWhileActive()
        {
            Material baseMaterial = new Material(Shader.Find(UIShaderName));
            GameObject gameObject = new GameObject("Uber UI Destroy Test",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                Image image = gameObject.GetComponent<Image>();
                image.material = baseMaterial;
                UberUIMaterialBinder binder =
                    gameObject.AddComponent<UberUIMaterialBinder>();
                Material owned = binder.GetModifiedMaterial(baseMaterial);
                Assert.That(binder.isActiveAndEnabled, Is.True);
                Assert.That(owned, Is.Not.SameAs(baseMaterial));

                UnityEngine.Object.DestroyImmediate(binder);
                Assert.That(owned == null, Is.True,
                    "Owned material survived active component destruction");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(baseMaterial);
            }
        }

        private static void InvokeNonPublic(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

    }
}
