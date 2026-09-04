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
        public void ParticleCoreImportsWithReviewedDefaultsAndSixPasses()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                UberDirectory + "UberParticle.shader");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo(ParticleShaderName));
            Assert.That(Shader.Find(ParticleShaderName), Is.SameAs(shader));
            Assert.That(shader.isSupported, Is.True);

            string[] messages = ShaderUtil.GetShaderMessages(shader)
                .Where(message =>
                    message.severity == ShaderCompilerMessageSeverity.Warning ||
                    message.severity == ShaderCompilerMessageSeverity.Error)
                .Select(message => message.severity + ": " + message.message)
                .ToArray();
            Assert.That(messages, Is.Empty, string.Join(" | ", messages));

            Material material = new Material(shader);
            try
            {
                Assert.That(material.passCount, Is.EqualTo(6));
                AssertPasses(material, "UniversalForward", "Universal2D",
                    "DepthOnly", "DepthNormalsOnly", "SceneSelectionPass",
                    "ScenePickingPass");
                foreach (string excluded in new[]
                         {
                             "GBuffer", "Meta", "ShadowCaster", "ForwardLit",
                         })
                    Assert.That(material.FindPass(excluded), Is.LessThan(0), excluded);

                Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
                Assert.That(material.GetFloat("_Blend"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_Cull"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_ZTest"), Is.EqualTo(4f));
                Assert.That(material.GetFloat("_ColorMask"), Is.EqualTo(15f));
                Assert.That(material.GetFloat("_SrcBlend"),
                    Is.EqualTo((float)UnityEngine.Rendering.BlendMode.SrcAlpha));
                Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo(
                    (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                Assert.That(material.GetFloat("_AlphaMultiplier"), Is.EqualTo(1f));
                Assert.That(material.GetFloat("_AlphaPower"), Is.EqualTo(1f));
                Assert.That(material.GetFloat("_AlphaBias"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_BaseNoiseClipEnabled"),
                    Is.EqualTo(0f));
                Assert.That(material.GetFloat("_BaseNoiseClipStream"),
                    Is.EqualTo(2f));
                Assert.That(material.GetFloat("_BaseNoiseClipChannel"),
                    Is.EqualTo(0f));
                AssertVector(material.GetVector("_BaseNoiseClipCurveValues"),
                    new Vector4(0f, 1f, 1f, 1f));
                AssertVector(material.GetVector("_BaseNoiseClipCurveTimes"),
                    new Vector4(0f, 1f, 1f, 1f));
                AssertVector(material.GetVector("_BaseNoiseClipCurveInTangents"),
                    Vector4.one);
                AssertVector(material.GetVector("_BaseNoiseClipCurveOutTangents"),
                    Vector4.one);
                AssertVector(material.GetVector("_BaseNoiseClipCurveMetadata"),
                    new Vector4(2f, 0f, 0f, 0f));
                Assert.That(material.GetFloat("_LifetimeGradientEnabled"),
                    Is.EqualTo(0f));
                Assert.That(material.GetFloat("_LifetimeStream"), Is.EqualTo(2f));
                AssertVector(material.GetVector("_LifetimeGradientColor0"),
                    new Vector4(1f, 1f, 1f, 0f));
                AssertVector(material.GetVector("_LifetimeGradientColor1"),
                    new Vector4(1f, 1f, 1f, 1f));
                AssertVector(material.GetVector("_LifetimeGradientAlphas"),
                    Vector4.one);
                AssertVector(material.GetVector("_LifetimeGradientAlphaTimes"),
                    new Vector4(0f, 1f, 1f, 1f));
                AssertVector(material.GetVector("_LifetimeGradientMetadata"),
                    new Vector4(2f, 2f, 0f, 0f));
                Assert.That(material.GetFloat("_SoftParticlesNearFadeDistance"),
                    Is.EqualTo(0f));
                Assert.That(material.GetFloat("_SoftParticlesFarFadeDistance"),
                    Is.EqualTo(1f));
                Assert.That(material.GetFloat("_CameraNearFadeDistance"),
                    Is.EqualTo(1f));
                Assert.That(material.GetFloat("_CameraFarFadeDistance"),
                    Is.EqualTo(2f));
                Assert.That(material.GetTag("RenderType", false),
                    Is.EqualTo("Transparent"));
                Assert.That(material.renderQueue, Is.EqualTo(
                    (int)RenderQueue.Transparent));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ParticleInspectorSynchronizesCoreKeywordsAndBlendStates()
        {
            Material material = new Material(Shader.Find(ParticleShaderName));
            try
            {
                UberShaderGUI gui = new UberShaderGUI();
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_FlipbookBlending", 1f);
                material.SetFloat("_SoftParticlesEnabled", 1f);
                material.SetFloat("_CameraFadingEnabled", 1f);
                material.SetFloat("_ZTest", 8.7f);
                material.SetFloat("_StencilComp", -2.2f);
                material.SetFloat("_StencilPass", 6.6f);
                material.SetFloat("_StencilRef", 255.8f);
                material.SetFloat("_StencilReadMask", -1f);
                material.SetFloat("_StencilWriteMask", 127.6f);
                material.SetFloat("_ColorMask", 6.6f);

                var expectedSource = new[]
                {
                    UnityEngine.Rendering.BlendMode.SrcAlpha,
                    UnityEngine.Rendering.BlendMode.One,
                    UnityEngine.Rendering.BlendMode.SrcAlpha,
                    UnityEngine.Rendering.BlendMode.DstColor,
                };
                var expectedDestination = new[]
                {
                    UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha,
                    UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha,
                    UnityEngine.Rendering.BlendMode.One,
                    UnityEngine.Rendering.BlendMode.Zero,
                };
                for (int blend = 0; blend < expectedSource.Length; ++blend)
                {
                    material.SetFloat("_Blend", blend);
                    gui.ValidateMaterial(material);
                    Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo(
                        (float)expectedSource[blend]), "source " + blend);
                    Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo(
                        (float)expectedDestination[blend]), "destination " + blend);
                    Assert.That(material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"),
                        Is.EqualTo(blend == 1), "premultiply " + blend);
                    Assert.That(material.IsKeywordEnabled("_ALPHAMODULATE_ON"),
                        Is.EqualTo(blend == 3), "multiply " + blend);
                }

                Assert.That(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"),
                    Is.True);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_FLIPBOOKBLENDING_ON"),
                    Is.True);
                Assert.That(material.IsKeywordEnabled("_SOFTPARTICLES_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_FADING_ON"), Is.True);
                Assert.That(material.GetFloat("_BlendModePreserveSpecular"),
                    Is.EqualTo(0f));
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_ZTest"), Is.EqualTo(8f));
                Assert.That(material.GetFloat("_StencilComp"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_StencilPass"), Is.EqualTo(7f));
                Assert.That(material.GetFloat("_StencilRef"), Is.EqualTo(255f));
                Assert.That(material.GetFloat("_StencilReadMask"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_StencilWriteMask"), Is.EqualTo(128f));
                Assert.That(material.GetFloat("_ColorMask"), Is.EqualTo(7f));

                material.SetFloat("_QueueControl", 1f);
                material.renderQueue = 3141;
                gui.ValidateMaterial(material);
                Assert.That(material.renderQueue, Is.EqualTo(3141));

                material.SetFloat("_Surface", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_FlipbookBlending", 0f);
                material.SetFloat("_SoftParticlesEnabled", 0f);
                material.SetFloat("_CameraFadingEnabled", 0f);
                material.SetFloat("_QueueControl", 0f);
                gui.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_FLIPBOOKBLENDING_ON"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_SOFTPARTICLES_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_FADING_ON"), Is.False);
                Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(1f));
                Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Opaque"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            Material ui = new Material(Shader.Find(UIShaderName));
            try
            {
                ui.SetFloat("_ColorMask", 6.6f);
                ui.SetFloat("_StencilComp", 5.4f);
                new UberShaderGUI().ValidateMaterial(ui);
                Assert.That(ui.GetFloat("_ColorMask"), Is.EqualTo(6.6f));
                Assert.That(ui.GetFloat("_StencilComp"), Is.EqualTo(5.4f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ui);
            }
        }

        [Test]
        public void ParticleCoreSourcesPreserveUrpAndInspectorContracts()
        {
            string shader = Read(UberDirectory + "UberParticle.shader");
            string hlsl = Read(UberDirectory + "UberParticle.hlsl");

            AssertGroupedInspector(UberDirectory + "UberParticle.shader", new[]
            {
                "[Main(Surface, _, on, off)] _SurfaceOptions(\"Surface\", Float) = 1",
                "[Main(Base, _, on, off)] _BaseOptions(\"Base\", Float) = 1",
                "[Main(BaseNoiseClip, _, on, on)] _BaseNoiseClipEnabled(\"Base Noise Clip\", Float) = 0",
                "[Main(LifetimeGradient, _, on, on)] _LifetimeGradientEnabled(\"Lifetime HDR Gradient\", Float) = 0",
                "[Main(Fading, _, on, off)] _FadingOptions(\"Fading\", Float) = 1",
                "[Main(Mask, _MASK_ON, on)] _MaskEnabled(\"Mask\", Float) = 0",
                "[Main(UVDistortion, _UV_DISTORTION_ON, on)] _UVDistortionEnabled(\"UV Distortion\", Float) = 0",
                "[Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled(\"Dissolve\", Float) = 0",
                "[Main(ColorAdjust, _COLOR_ADJUST_ON, on)] _ColorAdjustEnabled(\"Color Adjustment\", Float) = 0",
                "[Main(Emission, _EMISSION, on)] _EmissionEnabled(\"Emission\", Float) = 0",
                "[Main(Rim, _RIM_ON, on)] _RimEnabled(\"Fresnel Rim\", Float) = 0",
                "[Main(VertexOffset, _VERTEX_OFFSET_ON, on)] _VertexOffsetEnabled(\"Vertex Offset\", Float) = 0",
                "[Main(CustomData, _CUSTOM_DATA_ON, on)] _CustomDataEnabled(\"Custom Data\", Float) = 0",
            }, new[]
            {
                "Surface", "Base", "BaseNoiseClip", "LifetimeGradient",
                "Fading", "Mask", "UVDistortion", "Dissolve", "ColorAdjust",
                "Emission", "Rim", "VertexOffset", "CustomData",
            }, new[]
            {
                "[Title(Surface, _)] [KWEnum(Surface, Opaque, _, Transparent, _SURFACE_TYPE_TRANSPARENT)] _Surface(\"Surface Type\", Float) = 1",
                "[KWEnum(Surface, Alpha, _, Premultiply, _ALPHAPREMULTIPLY_ON, Additive, _, Multiply, _ALPHAMODULATE_ON)] _Blend(\"Blend Mode\", Float) = 0",
                "[Title(Base, _)] [Tex(Base)] [MainTexture] _BaseMap(\"Base Map\", 2D) = \"white\" {}",
                "[SubToggle(Base, _FLIPBOOKBLENDING_ON)] _FlipbookBlending(\"Flipbook Blending\", Float) = 0",
                "[Title(BaseNoiseClip, _)] [UberParticleStream(BaseNoiseClip)] _BaseNoiseClipStream(\"Normalized Clip Threshold Stream\", Float) = 2",
                "[UberParticleNoiseChannel(BaseNoiseClip)] _BaseNoiseClipChannel(\"Noise Channel\", Float) = 0",
                "[UberParticleCurve(BaseNoiseClip)] _BaseNoiseClipCurveValues(\"Threshold Curve\", Vector) = (0, 1, 1, 1)",
                "[Title(LifetimeGradient, _)] [UberParticleStream(LifetimeGradient)] _LifetimeStream(\"Normalized Lifetime Stream\", Float) = 2",
                "[UberGradient(LifetimeGradient)] _LifetimeGradientColor0(\"HDR Gradient\", Vector) = (1, 1, 1, 0)",
                "[Title(Fading, _)] [SubToggle(Fading, _SOFTPARTICLES_ON)] _SoftParticlesEnabled(\"Soft Particles\", Float) = 0",
                "[SubToggle(Fading, _FADING_ON)] _CameraFadingEnabled(\"Camera Fade\", Float) = 0",
                "[Title(Mask, _)] [Tex(Mask)] _MaskMap(\"Mask Map\", 2D) = \"white\" {}",
                "[Title(UVDistortion, _)] [Tex(UVDistortion)] _UVDistortionMap(\"Flow Map\", 2D) = \"gray\" {}",
                "[KWEnum(Dissolve, Noise, _, Radial, _DISSOLVE_RADIAL, Swipe, _DISSOLVE_SWIPE)] _DissolveMode(\"Mode\", Float) = 0",
                "[Title(ColorAdjust, _)] [Sub(ColorAdjust)] _HueShift(\"Hue Shift\", Range(-180, 180)) = 0",
                "[Title(Emission, _)] [Tex(Emission)] _EmissionMap(\"Emission Map\", 2D) = \"white\" {}",
                "[Title(Rim, _)] [KWEnum(Rim, Geometry Normal, _, Radial UV, _RIM_RADIAL_UV)] _RimMode(\"Mode\", Float) = 0",
                "[Title(VertexOffset, _)] [Sub(VertexOffset)] _VertexOffsetDirection(\"Direction\", Vector) = (0, 1, 0, 0)",
                "[Title(CustomData, _)] [Sub(CustomData)] _CustomDissolveWeight(\"Custom1 X / Dissolve\", Range(0, 1)) = 0",
            });

            Assert.That(Regex.Matches(shader, @"\bName\s+""").Count,
                Is.EqualTo(6));
            foreach (string pass in new[]
                     {
                         "UniversalForward", "Universal2D", "DepthOnly",
                         "DepthNormalsOnly", "SceneSelectionPass", "ScenePickingPass",
                     })
                StringAssert.Contains("Name \"" + pass + "\"", shader);
            foreach (string excluded in new[]
                     {
                         "GBuffer", "Meta", "ShadowCaster", "ForwardLit",
                     })
                StringAssert.DoesNotContain("Name \"" + excluded + "\"", shader);

            StringAssert.Contains("#include_with_pragmas \"UberParticle.hlsl\"",
                shader);
            StringAssert.Contains("ShaderLibrary/ParticlesInstancing.hlsl", hlsl);
            StringAssert.Contains("ShaderLibrary/Particles.hlsl", hlsl);
            StringAssert.Contains("GetParticleColor(input.color)", hlsl);
            StringAssert.Contains("GetParticleTexcoords", hlsl);
            StringAssert.Contains("UNITY_VERTEX_OUTPUT_STEREO", hlsl);
            StringAssert.Contains("UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX", hlsl);
            StringAssert.Contains("UberSafeInverseLerp", hlsl);
            StringAssert.Contains("UberSafeSignedRange(projection.w", hlsl);
            Assert.That(Regex.Matches(hlsl,
                @"CBUFFER_START\(UnityPerMaterial\)").Count, Is.EqualTo(1));
            Match cbuffer = Regex.Match(hlsl,
                @"(?s)CBUFFER_START\(UnityPerMaterial\)(?<body>.*?)CBUFFER_END");
            Assert.That(cbuffer.Success, Is.True);
            Assert.That(Regex.IsMatch(cbuffer.Groups["body"].Value,
                @"#\s*(?:if|ifdef|ifndef)"), Is.False);

            foreach (string keyword in new[]
                     {
                         "_FLIPBOOKBLENDING_ON", "_SOFTPARTICLES_ON", "_FADING_ON",
                     })
            {
                string[] rows = PragmaRows(shader, keyword);
                Assert.That(rows, Is.Not.Empty, keyword);
                Assert.That(rows.All(row => row.Contains("shader_feature_local")),
                    Is.True, keyword + ": " + string.Join(" | ", rows));
            }
            Assert.That(PragmaRows(shader, "_SOFTPARTICLES_ON").Length,
                Is.EqualTo(2));
            Assert.That(PragmaRows(shader, "_FADING_ON").Length, Is.EqualTo(2));
            StringAssert.Contains("#if defined(_SOFTPARTICLES_ON)", hlsl);
            Assert.That(hlsl.IndexOf("#if defined(_SOFTPARTICLES_ON)",
                    StringComparison.Ordinal),
                Is.LessThan(hlsl.IndexOf("_CameraDepthTexture",
                    StringComparison.Ordinal)));
            StringAssert.Contains(
                "#pragma instancing_options procedural:ParticleInstancingSetup", shader);
            StringAssert.Contains("#pragma never_use_dxc", shader);
            Assert.That(shader.IndexOf("#pragma never_use_dxc",
                    StringComparison.Ordinal),
                Is.LessThan(shader.IndexOf("procedural:ParticleInstancingSetup",
                    StringComparison.Ordinal)));
            StringAssert.Contains("#pragma multi_compile_fog", shader);
            StringAssert.DoesNotContain("fogFactor : TEXCOORD", hlsl);
            StringAssert.Contains("output.positionWS.w = ComputeFogFactor", hlsl);
            StringAssert.Contains("MixFog(color.rgb, input.positionWS.w)", hlsl);

            int selectionStart = shader.IndexOf("Name \"SceneSelectionPass\"",
                StringComparison.Ordinal);
            int pickingStart = shader.IndexOf("Name \"ScenePickingPass\"",
                StringComparison.Ordinal);
            int fallbackStart = shader.IndexOf("FallBack", pickingStart,
                StringComparison.Ordinal);
            Assert.That(selectionStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(pickingStart, Is.GreaterThan(selectionStart));
            Assert.That(fallbackStart, Is.GreaterThan(pickingStart));
            foreach (string editorPass in new[]
                     {
                         shader.Substring(selectionStart,
                             pickingStart - selectionStart),
                         shader.Substring(pickingStart,
                             fallbackStart - pickingStart),
                     })
            {
                StringAssert.Contains("Cull Off", editorPass);
                StringAssert.Contains("ZTest LEqual", editorPass);
                StringAssert.Contains("Ref 0", editorPass);
                StringAssert.Contains("ReadMask 255", editorPass);
                StringAssert.Contains("WriteMask 255", editorPass);
                StringAssert.Contains("Comp Always", editorPass);
                StringAssert.Contains("Pass Keep", editorPass);
                StringAssert.DoesNotContain("Cull [_Cull]", editorPass);
                StringAssert.DoesNotContain("ZTest [_ZTest]", editorPass);
                StringAssert.DoesNotContain("Ref [_StencilRef]", editorPass);
            }
        }

        [Test]
        public void ParticleLifetimeHdrGradientUsesSelectableAgePercentStream()
        {
            string shader = Read(UberDirectory + "UberParticle.shader");
            string hlsl = Read(UberDirectory + "UberParticle.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");

            foreach (string row in new[]
                     {
                         "[Main(LifetimeGradient, _, on, on)] _LifetimeGradientEnabled(\"Lifetime HDR Gradient\", Float) = 0",
                         "[Title(LifetimeGradient, _)] [UberParticleStream(LifetimeGradient)] _LifetimeStream(\"Normalized Lifetime Stream\", Float) = 2",
                         "[UberGradient(LifetimeGradient)] _LifetimeGradientColor0(\"HDR Gradient\", Vector) = (1, 1, 1, 0)",
                         "[HideInInspector] _LifetimeGradientColor1(\"Lifetime Gradient Color 1\", Vector) = (1, 1, 1, 1)",
                         "[HideInInspector] _LifetimeGradientAlphas(\"Lifetime Gradient Alphas\", Vector) = (1, 1, 1, 1)",
                         "[HideInInspector] _LifetimeGradientAlphaTimes(\"Lifetime Gradient Alpha Times\", Vector) = (0, 1, 1, 1)",
                         "[HideInInspector] _LifetimeGradientMetadata(\"Lifetime Gradient Metadata\", Vector) = (2, 2, 0, 0)",
                     })
                AssertContainsFlexibleWhitespace(row, shader);

            foreach (string field in new[]
                     {
                         "float4 _LifetimeGradientColor0;",
                         "float4 _LifetimeGradientColor1;",
                         "float4 _LifetimeGradientColor2;",
                         "float4 _LifetimeGradientColor3;",
                         "float4 _LifetimeGradientAlphas;",
                         "float4 _LifetimeGradientAlphaTimes;",
                         "float4 _LifetimeGradientMetadata;",
                         "half _LifetimeGradientEnabled;",
                         "float _LifetimeStream;",
                     })
                AssertContainsFlexibleWhitespace(field, hlsl);
            StringAssert.Contains(
                "UV + AgePercent -> TEXCOORD0.xy/z (the default lifetime layout)",
                hlsl);
            foreach (string semantic in new[]
                     {
                         "float4 texcoords : TEXCOORD0;",
                         "float4 streamTexcoord1 : TEXCOORD1;",
                         "float4 streamTexcoord2 : TEXCOORD2;",
                         "float4 streamTexcoord3 : TEXCOORD3;",
                         "float2 normalizedControlStreams : TEXCOORD4;",
                     })
                StringAssert.Contains(semantic, hlsl);
            StringAssert.Contains("UberParticleReadLifetimeTime", hlsl);
            StringAssert.Contains("clamp(round(streamSelector), 0.0, 15.0)",
                hlsl);
            StringAssert.Contains(
                "UberParticleReadNormalizedStream(input, _LifetimeStream)", hlsl);
            StringAssert.Contains("UberParticleEvaluateLifetimeGradient", hlsl);
            AssertContainsFlexibleWhitespace(
                "result.color *= UberParticleEvaluateLifetimeMultiplier(\n" +
                "        input.normalizedControlStreams.x)", hlsl);
            StringAssert.Contains(
                "public sealed class UberParticleStreamDrawer : LWGUI.SubDrawer",
                gui);
            StringAssert.Contains("new GUIContent(\"TEXCOORD0.z\")", gui);
            StringAssert.Contains("new GUIContent(\"TEXCOORD3.w\")", gui);
            StringAssert.Contains("AgePercent", gui);
            StringAssert.Contains("GPU Instancing must be disabled", gui);
            StringAssert.DoesNotContain("_LIFETIME_GRADIENT_ON", shader);

            Material material = new Material(Shader.Find(ParticleShaderName));
            try
            {
                Color disabled = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_LifetimeGradientEnabled", 1f);
                material.SetVector("_LifetimeGradientColor0",
                    new Vector4(1f, 0f, 0f, 0f));
                material.SetVector("_LifetimeGradientColor1",
                    new Vector4(0f, 0f, 1f, 1f));
                material.SetVector("_LifetimeGradientAlphas", Vector4.one);
                material.SetVector("_LifetimeGradientAlphaTimes",
                    new Vector4(0f, 1f, 1f, 1f));
                material.SetVector("_LifetimeGradientMetadata",
                    new Vector4(2f, 2f, 0f, 0f));

                material.SetFloat("_LifetimeStream", 2f);
                Color texcoord0Z = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_LifetimeStream", 3f);
                Color texcoord0W = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_LifetimeStream", 2f);
                Color texcoord0ZRepeat = RenderParticleCore(material, 0f,
                    Color.white);
                material.SetFloat("_LifetimeStream", 3f);
                Color texcoord0WRepeat = RenderParticleCore(material, 0f,
                    Color.white);
                Assert.That(IsFinite(disabled), Is.True);
                Assert.That(IsFinite(texcoord0Z), Is.True);
                Assert.That(IsFinite(texcoord0W), Is.True);
                Assert.That(disabled.r, Is.GreaterThan(0.95f));
                Assert.That(disabled.g, Is.GreaterThan(0.95f));
                Assert.That(disabled.b, Is.GreaterThan(0.95f));
                Assert.That(texcoord0Z.b,
                    Is.GreaterThan(texcoord0W.b + 0.15f));
                Assert.That(texcoord0W.r,
                    Is.GreaterThan(texcoord0Z.r + 0.15f));
                Assert.That((Color32)texcoord0ZRepeat,
                    Is.EqualTo((Color32)texcoord0Z));
                Assert.That((Color32)texcoord0WRepeat,
                    Is.EqualTo((Color32)texcoord0W));

                material.SetVector("_LifetimeGradientColor0",
                    new Vector4(1f, 1f, 1f, 0f));
                material.SetVector("_LifetimeGradientColor1",
                    new Vector4(1f, 1f, 1f, 1f));
                material.SetVector("_LifetimeGradientAlphas",
                    new Vector4(0f, 1f, 1f, 1f));
                material.SetFloat("_LifetimeStream", 2f);
                Color alphaAtThreeQuarters = RenderParticleCore(material, 0f,
                    Color.white);
                Color alphaAtThreeQuartersRepeat = RenderParticleCore(material,
                    0f, Color.white);
                Assert.That(alphaAtThreeQuarters.a,
                    Is.EqualTo(0.75f).Within(0.04f));
                Assert.That((Color32)alphaAtThreeQuartersRepeat,
                    Is.EqualTo((Color32)alphaAtThreeQuarters));

                material.SetVector("_LifetimeGradientColor0",
                    new Vector4(4f, 2f, 1f, 0f));
                AssertVector(material.GetVector("_LifetimeGradientColor0"),
                    new Vector4(4f, 2f, 1f, 0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ParticleBaseNoiseClipPreservesHdrAlphaAndUsesSelectableStream()
        {
            string shader = Read(UberDirectory + "UberParticle.shader");
            string hlsl = Read(UberDirectory + "UberParticle.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");

            foreach (string row in new[]
                     {
                         "[Main(BaseNoiseClip, _, on, on)] _BaseNoiseClipEnabled(\"Base Noise Clip\", Float) = 0",
                         "[Title(BaseNoiseClip, _)] [UberParticleStream(BaseNoiseClip)] _BaseNoiseClipStream(\"Normalized Clip Threshold Stream\", Float) = 2",
                         "[UberParticleNoiseChannel(BaseNoiseClip)] _BaseNoiseClipChannel(\"Noise Channel\", Float) = 0",
                         "[UberParticleCurve(BaseNoiseClip)] _BaseNoiseClipCurveValues(\"Threshold Curve\", Vector) = (0, 1, 1, 1)",
                         "[HideInInspector] _BaseNoiseClipCurveTimes(\"Base Noise Clip Curve Times\", Vector) = (0, 1, 1, 1)",
                         "[HideInInspector] _BaseNoiseClipCurveInTangents(\"Base Noise Clip Curve In Tangents\", Vector) = (1, 1, 1, 1)",
                         "[HideInInspector] _BaseNoiseClipCurveOutTangents(\"Base Noise Clip Curve Out Tangents\", Vector) = (1, 1, 1, 1)",
                         "[HideInInspector] _BaseNoiseClipCurveMetadata(\"Base Noise Clip Curve Metadata\", Vector) = (2, 0, 0, 0)",
                     })
                StringAssert.Contains(row, shader);

            foreach (string field in new[]
                     {
                         "half _BaseNoiseClipEnabled;",
                         "half _BaseNoiseClipChannel;",
                         "float _BaseNoiseClipStream;",
                         "float4 _BaseNoiseClipCurveValues;",
                         "float4 _BaseNoiseClipCurveTimes;",
                         "float4 _BaseNoiseClipCurveInTangents;",
                         "float4 _BaseNoiseClipCurveOutTangents;",
                         "float4 _BaseNoiseClipCurveMetadata;",
                     })
                StringAssert.Contains(field, hlsl);

            StringAssert.Contains("UberParticleReadBaseNoiseClipThreshold", hlsl);
            StringAssert.Contains(
                "UberParticleReadNormalizedStream(input, _BaseNoiseClipStream)",
                hlsl);
            StringAssert.Contains("UberParticleApplyBaseNoiseClip", hlsl);
            StringAssert.Contains("UberParticleEvaluateBaseNoiseClipCurve", hlsl);
            StringAssert.Contains("UberParticleEvaluateCurveSegment", hlsl);
            StringAssert.Contains(
                "clip(lerp(1.0h, noise - curvedThreshold, enabled))",
                hlsl);
            StringAssert.Contains(
                "public sealed class UberParticleNoiseChannelDrawer", gui);
            StringAssert.Contains(
                "public sealed class UberParticleCurveDrawer", gui);
            StringAssert.Contains("EditorGUI.CurveField(fieldPosition, label,", gui);
            StringAssert.Contains("Edit Base Noise Clip Curve", gui);
            StringAssert.Contains("a normalized clip threshold", gui);
            StringAssert.Contains("GPU Instancing must be disabled", gui);

            int baseClip = hlsl.IndexOf(
                "color = UberParticleApplyBaseNoiseClip", StringComparison.Ordinal);
            int lifetimeGradient = hlsl.IndexOf(
                "result.color *= UberParticleEvaluateLifetimeMultiplier",
                StringComparison.Ordinal);
            Assert.That(baseClip, Is.GreaterThanOrEqualTo(0));
            Assert.That(lifetimeGradient, Is.GreaterThan(baseClip));

            Texture2D noise = CreateSolidTexture(1, 1, new[]
            {
                new Color(0.6f, 0.2f, 0.9f, 0.8f),
            });
            Material material = new Material(Shader.Find(ParticleShaderName));
            try
            {
                material.SetTexture("_BaseMap", noise);
                material.SetFloat("_BaseNoiseClipEnabled", 1f);
                material.SetFloat("_BaseNoiseClipChannel", 0f);
                material.SetFloat("_BaseNoiseClipStream", 3f);
                material.SetFloat("_LifetimeGradientEnabled", 1f);
                material.SetVector("_LifetimeGradientColor0",
                    new Vector4(0.2f, 0.8f, 0.4f, 0f));
                material.SetVector("_LifetimeGradientColor1",
                    new Vector4(0.2f, 0.8f, 0.4f, 1f));
                material.SetVector("_LifetimeGradientAlphas",
                    new Vector4(1f, 0f, 1f, 1f));
                material.SetVector("_LifetimeGradientAlphaTimes",
                    new Vector4(0f, 1f, 1f, 1f));
                material.SetVector("_LifetimeGradientMetadata",
                    new Vector4(2f, 2f, 0f, 0f));

                Color whiteThenGradient = RenderParticleCore(material, 0f,
                    Color.white);
                Assert.That(whiteThenGradient.r,
                    Is.EqualTo(0.05f).Within(0.04f));
                Assert.That(whiteThenGradient.g,
                    Is.EqualTo(0.2f).Within(0.04f));
                Assert.That(whiteThenGradient.b,
                    Is.EqualTo(0.1f).Within(0.04f));
                Assert.That(whiteThenGradient.a,
                    Is.EqualTo(0.25f).Within(0.04f));

                material.SetVector("_BaseNoiseClipCurveValues",
                    new Vector4(0.8f, 0.8f, 0.8f, 0.8f));
                material.SetVector("_BaseNoiseClipCurveInTangents", Vector4.zero);
                material.SetVector("_BaseNoiseClipCurveOutTangents", Vector4.zero);
                Color clippedByCurve = RenderParticleCore(material, 0f,
                    Color.white);
                Assert.That(clippedByCurve.a, Is.LessThan(0.02f));

                material.SetVector("_BaseNoiseClipCurveValues",
                    new Vector4(0.4f, 0.4f, 0.4f, 0.4f));
                Color passedByCurve = RenderParticleCore(material, 0f,
                    Color.white);
                Assert.That(passedByCurve.a,
                    Is.EqualTo(0.25f).Within(0.04f));

                material.SetVector("_BaseNoiseClipCurveValues",
                    new Vector4(0f, 1f, 1f, 1f));
                material.SetVector("_BaseNoiseClipCurveInTangents", Vector4.one);
                material.SetVector("_BaseNoiseClipCurveOutTangents", Vector4.one);
                material.SetFloat("_BaseNoiseClipStream", 2f);
                Color clippedByTexcoord0Z = RenderParticleCore(material, 0f,
                    Color.white);
                Assert.That(clippedByTexcoord0Z.a, Is.LessThan(0.02f));

                material.SetFloat("_BaseNoiseClipChannel", 2f);
                Color blueChannelPasses = RenderParticleCore(material, 0f,
                    Color.white);
                Assert.That(blueChannelPasses.a,
                    Is.EqualTo(0.25f).Within(0.04f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(noise);
            }
        }

        [Test]
        public void ParticleBaseNoiseClipCurvePersistsHermiteKeysWithUndoAndLimits()
        {
            MethodInfo findPacked = typeof(UberParticleCurveDrawer).GetMethod(
                "FindPackedProperties", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo readCurve = typeof(UberParticleCurveDrawer).GetMethod(
                "ReadCurve", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo writeCurve = typeof(UberParticleCurveDrawer).GetMethod(
                "TryWriteCurve", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo hasMixed = typeof(UberParticleCurveDrawer).GetMethod(
                "HasMixedValue", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(findPacked, Is.Not.Null);
            Assert.That(readCurve, Is.Not.Null);
            Assert.That(writeCurve, Is.Not.Null);
            Assert.That(hasMixed, Is.Not.Null);

            Material first = new Material(Shader.Find(ParticleShaderName));
            Material second = new Material(Shader.Find(ParticleShaderName));
            MaterialEditor editor = null;
            try
            {
                second.SetVector("_BaseNoiseClipCurveValues", Vector4.zero);
                MaterialProperty[] properties = MaterialEditor.GetMaterialProperties(
                    new UnityEngine.Object[] { first, second });
                MaterialProperty[] packed = (MaterialProperty[])findPacked.Invoke(null,
                    new object[] { properties });
                Assert.That((bool)hasMixed.Invoke(null, new object[] { packed }),
                    Is.True);

                AnimationCurve authored = new AnimationCurve(
                    new Keyframe(0f, 0f, 0.5f, 0.5f),
                    new Keyframe(0.4f, 0.75f, 1.25f, -0.25f),
                    new Keyframe(1f, 1f, 0.2f, 0.2f));
                editor = Editor.CreateEditor(new UnityEngine.Object[] { first, second })
                    as MaterialEditor;
                Assert.That(editor, Is.Not.Null);
                int undoGroup = Undo.GetCurrentGroup();
                Assert.That((bool)writeCurve.Invoke(null,
                    new object[] { authored, packed, editor }), Is.True);
                Undo.CollapseUndoOperations(undoGroup);

                MaterialProperty[] firstProperties = MaterialEditor.GetMaterialProperties(
                    new UnityEngine.Object[] { first });
                MaterialProperty[] firstPacked = (MaterialProperty[])findPacked.Invoke(null,
                    new object[] { firstProperties });
                AnimationCurve stored = (AnimationCurve)readCurve.Invoke(null,
                    new object[] { firstPacked });
                Assert.That(stored.keys, Has.Length.EqualTo(3));
                Assert.That(stored.keys[1].time, Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(stored.keys[1].value,
                    Is.EqualTo(0.75f).Within(0.0001f));
                Assert.That(stored.keys[1].inTangent,
                    Is.EqualTo(1.25f).Within(0.0001f));
                Assert.That(stored.keys[1].outTangent,
                    Is.EqualTo(-0.25f).Within(0.0001f));
                Assert.That(stored.preWrapMode, Is.EqualTo(WrapMode.ClampForever));
                Assert.That(stored.postWrapMode, Is.EqualTo(WrapMode.ClampForever));
                AssertVector(first.GetVector("_BaseNoiseClipCurveValues"),
                    new Vector4(0f, 0.75f, 1f, 1f));
                AssertVector(first.GetVector("_BaseNoiseClipCurveTimes"),
                    new Vector4(0f, 0.4f, 1f, 1f));
                AssertVector(first.GetVector("_BaseNoiseClipCurveMetadata"),
                    new Vector4(3f, 0f, 0f, 0f));
                AssertVector(first.GetVector("_BaseNoiseClipCurveValues"),
                    second.GetVector("_BaseNoiseClipCurveValues"));

                Vector4 retained = first.GetVector("_BaseNoiseClipCurveValues");
                AnimationCurve excess = new AnimationCurve(
                    Enumerable.Range(0, 5).Select(index =>
                        new Keyframe(index * 0.25f, index * 0.25f)).ToArray());
                Assert.That((bool)writeCurve.Invoke(null,
                    new object[] { excess, firstPacked, editor }), Is.False);
                AssertVector(first.GetVector("_BaseNoiseClipCurveValues"), retained);

                Undo.PerformUndo();
                AssertVector(first.GetVector("_BaseNoiseClipCurveValues"),
                    new Vector4(0f, 1f, 1f, 1f));
                AssertVector(second.GetVector("_BaseNoiseClipCurveValues"),
                    Vector4.zero);
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
        public void ParticleCoreGpuBlendsFlipbookBaseAndVertexColor()
        {
            Texture2D texture = new Texture2D(2, 1, TextureFormat.RGBA32,
                false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels(new[]
            {
                new Color(1f, 0f, 0f, 1f),
                new Color(0f, 0f, 1f, 0.5f),
            });
            texture.Apply(false, false);
            Material material = new Material(Shader.Find(ParticleShaderName));
            try
            {
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", new Color(0.5f, 1f, 0.5f, 0.8f));
                material.SetFloat("_FlipbookBlending", 0f);
                new UberShaderGUI().ValidateMaterial(material);
                Color firstFrame = RenderParticleCore(material, 0f,
                    new Color(1f, 0.5f, 1f, 0.5f));

                material.SetFloat("_FlipbookBlending", 1f);
                new UberShaderGUI().ValidateMaterial(material);
                Color blended = RenderParticleCore(material, 0.5f,
                    new Color(1f, 0.5f, 1f, 0.5f));

                material.SetFloat("_AlphaMultiplier", 0.5f);
                material.SetFloat("_AlphaPower", 2f);
                material.SetFloat("_AlphaBias", 0.1f);
                Color remapped = RenderParticleCore(material, 0.5f,
                    new Color(1f, 0.5f, 1f, 0.5f));

                Assert.That(IsFinite(firstFrame), Is.True);
                Assert.That(IsFinite(blended), Is.True);
                Assert.That(IsFinite(remapped), Is.True);
                Assert.That(firstFrame.r, Is.GreaterThan(blended.r + 0.025f));
                Assert.That(firstFrame.b, Is.LessThan(0.01f));
                Assert.That(blended.r, Is.GreaterThan(0.03f));
                Assert.That(blended.b, Is.GreaterThan(0.03f));
                Assert.That(blended.g, Is.LessThan(0.01f));
                Assert.That(firstFrame.a, Is.EqualTo(0.4f).Within(0.03f));
                Assert.That(blended.a, Is.EqualTo(0.3f).Within(0.03f));
                Assert.That(remapped.a, Is.EqualTo(0.145f).Within(0.03f));
                Assert.That(remapped.a, Is.LessThan(blended.a - 0.1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ParticleCoreGpuAppliesFiniteCameraAndSoftFades()
        {
            const string depthName = "_CameraDepthTexture";
            const string zBufferName = "_ZBufferParams";
            const string orthoName = "unity_OrthoParams";
            const string projectionName = "_ProjectionParams";
            Texture previousDepth = Shader.GetGlobalTexture(depthName);
            Vector4 previousZBuffer = Shader.GetGlobalVector(zBufferName);
            Vector4 previousOrtho = Shader.GetGlobalVector(orthoName);
            Vector4 previousProjection = Shader.GetGlobalVector(projectionName);
            Texture2D depth = new Texture2D(1, 1, TextureFormat.RGBA32,
                false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            Material material = new Material(Shader.Find(ParticleShaderName));
            try
            {
                depth.SetPixel(0, 0, Color.white);
                depth.Apply(false, false);
                Shader.SetGlobalTexture(depthName, depth);
                Shader.SetGlobalVector(zBufferName,
                    new Vector4(0f, 0f, -1f, 2f));
                Shader.SetGlobalVector(orthoName, Vector4.zero);
                Shader.SetGlobalVector(projectionName,
                    new Vector4(1f, 1f, 1f, 1f));

                material.SetFloat("_SoftParticlesEnabled", 0f);
                material.SetFloat("_CameraFadingEnabled", 0f);
                new UberShaderGUI().ValidateMaterial(material);
                Color baseline = RenderParticleCore(material, 0f, Color.white);

                material.SetFloat("_CameraFadingEnabled", 1f);
                material.SetFloat("_CameraNearFadeDistance", 0f);
                material.SetFloat("_CameraFarFadeDistance", 1f);
                new UberShaderGUI().ValidateMaterial(material);
                Color camera = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_CameraNearFadeDistance", 0.5f);
                material.SetFloat("_CameraFarFadeDistance", 0.5f);
                Color cameraEqual = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_CameraNearFadeDistance", 1f);
                material.SetFloat("_CameraFarFadeDistance", 0f);
                Color cameraReversed = RenderParticleCore(material, 0f,
                    Color.white);

                material.SetFloat("_CameraFadingEnabled", 0f);
                material.SetFloat("_SoftParticlesEnabled", 1f);
                material.SetFloat("_SoftParticlesNearFadeDistance", 0f);
                material.SetFloat("_SoftParticlesFarFadeDistance", 1f);
                new UberShaderGUI().ValidateMaterial(material);
                Color soft = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_SoftParticlesNearFadeDistance", 0.5f);
                material.SetFloat("_SoftParticlesFarFadeDistance", 0.5f);
                Color softEqual = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_SoftParticlesNearFadeDistance", 1f);
                material.SetFloat("_SoftParticlesFarFadeDistance", 0f);
                Color softReversed = RenderParticleCore(material, 0f,
                    Color.white);

                Assert.That(IsFinite(baseline), Is.True);
                Assert.That(IsFinite(camera), Is.True);
                Assert.That(IsFinite(cameraEqual), Is.True);
                Assert.That(IsFinite(cameraReversed), Is.True);
                Assert.That(IsFinite(soft), Is.True);
                Assert.That(IsFinite(softEqual), Is.True);
                Assert.That(IsFinite(softReversed), Is.True);
                Assert.That(camera.a, Is.LessThan(baseline.a - 0.15f));
                Assert.That(camera.a, Is.GreaterThan(0.15f));
                Assert.That(soft.a, Is.LessThan(baseline.a - 0.15f));
                Assert.That(soft.a, Is.GreaterThan(0.15f));
            }
            finally
            {
                Shader.SetGlobalTexture(depthName, previousDepth);
                Shader.SetGlobalVector(zBufferName, previousZBuffer);
                Shader.SetGlobalVector(orthoName, previousOrtho);
                Shader.SetGlobalVector(projectionName, previousProjection);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(depth);
            }
        }

        [Test]
        public void ParticleAdvancedInspectorSynchronizesEveryKeywordAndDefault()
        {
            Material material = new Material(Shader.Find(ParticleShaderName));
            try
            {
                foreach (string property in new[]
                         {
                             "_MaskMap", "_MaskChannel", "_MaskInvert",
                             "_MaskStrength", "_UVDistortionMap",
                             "_UVDistortionDirection", "_UVDistortionStrength",
                             "_UVDistortionSpeed", "_DissolveNoiseMap",
                             "_DissolveAmount", "_DissolveNoiseRange",
                             "_DissolveRadialRange", "_DissolveSwipeRange",
                             "_DissolveEdgeColor0", "_DissolveEdgeColor1",
                             "_DissolveEdgeEmission", "_HueShift", "_Saturation",
                             "_Brightness", "_Contrast", "_EmissionMap",
                             "_EmissionColor", "_EmissionIntensity", "_RimMode",
                             "_RimColor", "_RimPower", "_RimIntensity",
                             "_VertexOffsetDirection",
                             "_VertexOffsetAmplitude", "_VertexOffsetFrequency",
                             "_VertexOffsetSpeed", "_CustomDissolveWeight",
                             "_CustomEmissionWeight",
                             "_CustomUVDistortionWeight",
                             "_CustomVertexOffsetWeight",
                         })
                    Assert.That(material.HasProperty(property), Is.True, property);

                Assert.That(material.GetFloat("_MaskStrength"), Is.EqualTo(1f));
                Assert.That(material.GetFloat("_UVDistortionStrength"),
                    Is.EqualTo(0f));
                Assert.That(material.GetFloat("_DissolveAmount"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_DissolveEdgeWidth"),
                    Is.EqualTo(0.1f).Within(0.0001f));
                Assert.That(material.GetFloat("_EmissionIntensity"), Is.EqualTo(1f));
                foreach (string weight in new[]
                         {
                             "_CustomDissolveWeight", "_CustomEmissionWeight",
                             "_CustomUVDistortionWeight",
                             "_CustomVertexOffsetWeight",
                         })
                    Assert.That(material.GetFloat(weight), Is.EqualTo(0f), weight);

                var gui = new UberShaderGUI();
                foreach (string toggle in new[]
                         {
                             "_MaskEnabled", "_UVDistortionEnabled",
                             "_DissolveEnabled", "_ColorAdjustEnabled",
                             "_EmissionEnabled", "_RimEnabled",
                             "_VertexOffsetEnabled",
                             "_CustomDataEnabled",
                         })
                    material.SetFloat(toggle, 1f);
                material.SetFloat("_DissolveMode", 1f);
                material.SetFloat("_RimMode", 1f);
                gui.ValidateMaterial(material);

                foreach (string keyword in new[]
                         {
                             "_MASK_ON", "_UV_DISTORTION_ON", "_DISSOLVE_ON",
                             "_DISSOLVE_RADIAL", "_COLOR_ADJUST_ON", "_EMISSION",
                             "_RIM_ON", "_RIM_RADIAL_UV",
                             "_VERTEX_OFFSET_ON", "_CUSTOM_DATA_ON",
                         })
                    Assert.That(material.IsKeywordEnabled(keyword), Is.True,
                        keyword);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_SWIPE"),
                    Is.False);

                material.SetFloat("_DissolveMode", 2f);
                material.SetFloat("_DissolveEnabled", 0f);
                material.SetFloat("_RimEnabled", 0f);
                gui.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_RADIAL"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_DISSOLVE_SWIPE"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_RIM_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_RIM_RADIAL_UV"),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            Material otherUber = new Material(Shader.Find(ObjectShaderName));
            try
            {
                new UberShaderGUI().ValidateMaterial(otherUber);
                foreach (string keyword in new[]
                         {
                             "_MASK_ON", "_UV_DISTORTION_ON",
                             "_VERTEX_OFFSET_ON", "_CUSTOM_DATA_ON",
                             "_RIM_RADIAL_UV",
                         })
                    Assert.That(otherUber.IsKeywordEnabled(keyword), Is.False,
                        keyword);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(otherUber);
            }
        }

        [Test]
        public void ParticleAdvancedSourcesGateSamplesShareSilhouetteAndPackCustom1()
        {
            string shader = Read(UberDirectory + "UberParticle.shader");
            string hlsl = Read(UberDirectory + "UberParticle.hlsl");

            var fragmentStage = new Dictionary<string, int>
            {
                { "_MASK_ON", 6 },
                { "_UV_DISTORTION_ON", 6 },
                { "_DISSOLVE_ON", 6 },
                { "_DISSOLVE_RADIAL", 6 },
                { "_DISSOLVE_SWIPE", 6 },
                { "_COLOR_ADJUST_ON", 2 },
                { "_EMISSION", 2 },
                { "_RIM_ON", 2 },
                { "_RIM_RADIAL_UV", 2 },
            };
            foreach (KeyValuePair<string, int> pair in fragmentStage)
            {
                string[] rows = PragmaRows(shader, pair.Key);
                Assert.That(rows.Length, Is.EqualTo(pair.Value), pair.Key);
                Assert.That(rows.All(row =>
                        row.Contains("shader_feature_local_fragment")),
                    Is.True, pair.Key + ": " + string.Join(" | ", rows));
            }
            string[] vertexRows = PragmaRows(shader, "_VERTEX_OFFSET_ON");
            Assert.That(vertexRows.Length, Is.EqualTo(6));
            Assert.That(vertexRows.All(row =>
                    row.Contains("shader_feature_local_vertex")),
                Is.True, string.Join(" | ", vertexRows));
            var allStage = new Dictionary<string, int>
            {
                { "_FLIPBOOKBLENDING_ON", 6 },
                { "_SOFTPARTICLES_ON", 2 },
                { "_FADING_ON", 2 },
                { "_CUSTOM_DATA_ON", 6 },
            };
            foreach (KeyValuePair<string, int> pair in allStage)
            {
                string[] rows = PragmaRows(shader, pair.Key);
                Assert.That(rows.Length, Is.EqualTo(pair.Value), pair.Key);
                Assert.That(rows.All(row => Regex.IsMatch(row,
                        @"#pragma\s+shader_feature_local\s+")),
                    Is.True, pair.Key + ": " + string.Join(" | ", rows));
                Assert.That(rows.Any(row =>
                        row.Contains("shader_feature_local_fragment") ||
                        row.Contains("shader_feature_local_vertex")),
                    Is.False, pair.Key);
            }
            Assert.That(Regex.Matches(shader,
                @"#define\s+UBER_PARTICLE_COLOR_PASS\s+1").Count,
                Is.EqualTo(2));
            Assert.That(hlsl.IndexOf("#if defined(UBER_PARTICLE_COLOR_PASS)",
                    StringComparison.Ordinal),
                Is.LessThan(hlsl.IndexOf(
                    "ShaderLibrary/Particles.hlsl", StringComparison.Ordinal)));
            StringAssert.Contains(
                "Only color passes import URP's depth texture declarations",
                hlsl);

            Match cbuffer = Regex.Match(hlsl,
                @"(?s)CBUFFER_START\(UnityPerMaterial\)(?<body>.*?)CBUFFER_END");
            Assert.That(cbuffer.Success, Is.True);
            Assert.That(Regex.IsMatch(cbuffer.Groups["body"].Value,
                @"#\s*(?:if|ifdef|ifndef)"), Is.False);
            foreach (string field in new[]
                     {
                         "_MaskMap_ST", "_UVDistortionMap_ST",
                         "_DissolveTilingOffset", "_DissolveEdgeColor0",
                         "_DissolveEdgeColor1", "_EmissionMap_ST", "_RimMode",
                         "_VertexOffsetDirection",
                         "_CustomDissolveWeight", "_CustomEmissionWeight",
                         "_CustomUVDistortionWeight", "_CustomVertexOffsetWeight",
                         "_BaseNoiseClipEnabled", "_BaseNoiseClipChannel",
                         "_BaseNoiseClipStream", "_BaseNoiseClipCurveValues",
                         "_BaseNoiseClipCurveTimes",
                         "_BaseNoiseClipCurveInTangents",
                         "_BaseNoiseClipCurveOutTangents",
                         "_BaseNoiseClipCurveMetadata",
                         "_LifetimeGradientColor0", "_LifetimeGradientAlphas",
                         "_LifetimeGradientEnabled", "_LifetimeStream",
                     })
                StringAssert.Contains(field, cbuffer.Groups["body"].Value, field);

            StringAssert.Contains(
                "UV -> TEXCOORD0.xy; Custom1.xy -> TEXCOORD0.zw", hlsl);
            StringAssert.Contains("Custom1.zw -> TEXCOORD1.xy", hlsl);
            StringAssert.Contains("UV/UV2 -> TEXCOORD0.xy/zw", hlsl);
            StringAssert.Contains(
                "AnimBlend -> TEXCOORD1.x; Custom1.xyz -> TEXCOORD1.yzw", hlsl);
            StringAssert.Contains("Custom1.w -> TEXCOORD2.x", hlsl);
            StringAssert.Contains(
                "#if defined(_CUSTOM_DATA_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)",
                hlsl);
            StringAssert.Contains(
                "Instanced and Custom-disabled variants retain authored material values",
                hlsl);
            StringAssert.Contains(
                "input.streamTexcoord1.yzw, input.streamTexcoord2.x", hlsl);
            StringAssert.Contains(
                "float4(input.texcoords.zw, input.streamTexcoord1.xy)", hlsl);

            foreach (string sample in new[]
                     {
                         "SAMPLE_TEXTURE2D(_MaskMap",
                         "SAMPLE_TEXTURE2D(_UVDistortionMap",
                         "SAMPLE_TEXTURE2D(_DissolveNoiseMap",
                         "SAMPLE_TEXTURE2D(_EmissionMap",
                     })
                Assert.That(hlsl.IndexOf(sample, StringComparison.Ordinal),
                    Is.GreaterThanOrEqualTo(0), sample);
            Assert.That(hlsl.IndexOf("#if defined(_MASK_ON)",
                    StringComparison.Ordinal),
                Is.LessThan(hlsl.IndexOf("SAMPLE_TEXTURE2D(_MaskMap",
                    StringComparison.Ordinal)));
            Assert.That(hlsl.IndexOf("#if defined(_UV_DISTORTION_ON)",
                    StringComparison.Ordinal),
                Is.LessThan(hlsl.IndexOf("SAMPLE_TEXTURE2D(_UVDistortionMap",
                    StringComparison.Ordinal)));
            Assert.That(hlsl.IndexOf("#if defined(_DISSOLVE_ON)",
                    StringComparison.Ordinal),
                Is.LessThan(hlsl.IndexOf("SAMPLE_TEXTURE2D(_DissolveNoiseMap",
                    StringComparison.Ordinal)));
            StringAssert.Contains("UberSafeInverseLerp(_DissolveNoiseRange.x",
                hlsl);
            StringAssert.Contains("max(abs(_DissolveEdgeWidth), 0.0001h)", hlsl);
            StringAssert.Contains("UberParticleSafeDirection", hlsl);
            StringAssert.Contains("UberAdjustColor(color.rgb", hlsl);
            StringAssert.Contains("UberParticleEvaluateSilhouette(input)", hlsl);
            Assert.That(Regex.Matches(hlsl,
                @"UberParticleEvaluateSilhouette\(input\)").Count,
                Is.EqualTo(5));
            StringAssert.Contains(
                "positionOS = UberParticleOffsetPosition(input.positionOS.xyz",
                hlsl);
            StringAssert.Contains(
                "output.projectedPosition = positionInputs.positionNDC", hlsl);

            string strictSilhouette = Regex.Match(hlsl,
                @"(?s)inline UberParticleSilhouette UberParticleEvaluateSilhouette\(.*?" +
                @"(?=inline half UberParticleSoftFade\()").Value;
            StringAssert.Contains(
                "#if !defined(UBER_PARTICLE_COLOR_PASS) && !defined(_ALPHATEST_ON)",
                strictSilhouette);
            StringAssert.Contains(
                "half baseNoiseClipEnabled = step(0.5h, _BaseNoiseClipEnabled);",
                strictSilhouette);
            Assert.That(strictSilhouette.IndexOf(
                    "if (baseNoiseClipEnabled > 0.0h)", StringComparison.Ordinal),
                Is.LessThan(strictSilhouette.IndexOf(
                    "UberParticleSampleCore(input, custom1)", StringComparison.Ordinal)));
            Match guardedRuntime = Regex.Match(strictSilhouette,
                @"(?s)if \(baseNoiseClipEnabled > 0\.0h\)\s*\{\s*#endif" +
                @"(?<body>.*?)#if !defined\(UBER_PARTICLE_COLOR_PASS\) && " +
                @"!defined\(_ALPHATEST_ON\)\s*\}");
            Assert.That(guardedRuntime.Success, Is.True);
            string guardedBody = guardedRuntime.Groups["body"].Value;
            int sampleCoreIndex = guardedBody.IndexOf(
                "UberParticleSampleCore(input, custom1)", StringComparison.Ordinal);
            int lifetimeIndex = guardedBody.IndexOf(
                "UberParticleEvaluateLifetimeMultiplier", StringComparison.Ordinal);
            int maskIndex = guardedBody.IndexOf(
                "UberParticleEvaluateMask(input.rawUV)", StringComparison.Ordinal);
            Assert.That(sampleCoreIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(lifetimeIndex, Is.GreaterThan(sampleCoreIndex));
            Assert.That(maskIndex, Is.GreaterThan(lifetimeIndex));
            StringAssert.DoesNotContain("UberParticleEvaluateDissolve", guardedBody);
            Assert.That(Regex.Matches(strictSilhouette,
                @"\bUberParticleEvaluateDissolve\s*\(").Count, Is.EqualTo(1));
            Assert.That(strictSilhouette.IndexOf("UberParticleEvaluateDissolve",
                    StringComparison.Ordinal), Is.GreaterThan(
                    guardedRuntime.Index + guardedRuntime.Length));
            string sampleCore = Regex.Match(hlsl,
                @"(?s)inline half4 UberParticleSampleCore\(.*?" +
                @"(?=inline half UberParticleEvaluateMask\()").Value;
            string distortUv = Regex.Match(hlsl,
                @"(?s)inline float2 UberParticleDistortUV\(.*?" +
                @"(?=inline half UberParticleReadBaseNoiseChannel\()").Value;
            string maskSource = Regex.Match(hlsl,
                @"(?s)inline half UberParticleEvaluateMask\(.*?" +
                @"(?=inline void UberParticleEvaluateDissolve\()").Value;
            int maximumSkippedSamples = Regex.Matches(sampleCore,
                    @"SAMPLE_TEXTURE2D\(_BaseMap").Count +
                Regex.Matches(sampleCore,
                    @"\bUberParticleDistortUV\s*\(").Count *
                Regex.Matches(distortUv,
                    @"SAMPLE_TEXTURE2D\(_UVDistortionMap").Count +
                Regex.Matches(maskSource,
                    @"SAMPLE_TEXTURE2D\(_MaskMap").Count;
            Assert.That(maximumSkippedSamples, Is.EqualTo(7));
        }

        [Test]
        public void ParticleStrictNonColorVariantsCompileForWebGlGles3()
        {
            BuildTarget activeBuildTarget =
                EditorUserBuildSettings.activeBuildTarget;
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                UberDirectory + "UberParticle.shader");
            Assert.That(shader, Is.Not.Null);
            ShaderData.Subshader subshader =
                ShaderUtil.GetShaderData(shader).ActiveSubshader;
            string[][] keywordRows =
            {
                Array.Empty<string>(),
                new[] { "_ALPHATEST_ON" },
                new[]
                {
                    "_CUSTOM_DATA_ON", "_DISSOLVE_ON",
                    "_FLIPBOOKBLENDING_ON", "_MASK_ON",
                    "_UV_DISTORTION_ON",
                },
                new[]
                {
                    "_CUSTOM_DATA_ON", "_DISSOLVE_ON", "_DISSOLVE_RADIAL",
                    "_FLIPBOOKBLENDING_ON", "_MASK_ON",
                    "_UV_DISTORTION_ON",
                },
                new[]
                {
                    "_CUSTOM_DATA_ON", "_DISSOLVE_ON", "_DISSOLVE_SWIPE",
                    "_FLIPBOOKBLENDING_ON", "_MASK_ON",
                    "_UV_DISTORTION_ON",
                },
            };
            int compiledVariantCount = 0;
            foreach (string passName in new[]
                     {
                         "DepthOnly", "DepthNormalsOnly",
                         "SceneSelectionPass", "ScenePickingPass",
                     })
            foreach (string[] keywords in keywordRows)
            {
                ShaderData.Pass pass = Enumerable.Range(0, subshader.PassCount)
                    .Select(subshader.GetPass)
                    .SingleOrDefault(candidate => candidate.Name == passName);
                Assert.That(pass, Is.Not.Null, passName);
                var compiled = pass.CompileVariant(ShaderType.Fragment,
                    keywords, ShaderCompilerPlatform.GLES3x,
                    BuildTarget.WebGL);
                string context = passName + " " + string.Join(" ", keywords);
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
            Assert.That(compiledVariantCount, Is.EqualTo(20));
            Assert.That(EditorUserBuildSettings.activeBuildTarget,
                Is.EqualTo(activeBuildTarget));
        }

        [Test]
        public void ParticleAdvancedGpuExercisesMaskFlowDissolveColorEmissionAndRim()
        {
            Vector4 previousCameraPosition = Shader.GetGlobalVector(
                "_WorldSpaceCameraPos");
            float previousObjectId = Shader.GetGlobalFloat("_ObjectId");
            float previousPassValue = Shader.GetGlobalFloat("_PassValue");
            Vector4 previousSelectionId = Shader.GetGlobalVector(
                "_SelectionID");
            Texture2D baseMap = CreateSolidTexture(2, 1, new[]
            {
                Color.red, Color.blue,
            });
            Texture2D mask = CreateSolidTexture(1, 1, new[]
            {
                new Color(0.2f, 0.4f, 0.6f, 0.8f),
            });
            Texture2D flow = CreateSolidTexture(1, 1, new[]
            {
                new Color(1f, 0.5f, 0f, 1f),
            });
            Texture2D noise = CreateSolidTexture(1, 1, new[]
            {
                new Color(0.55f, 0.55f, 0.55f, 1f),
            });
            Texture2D white = CreateSolidTexture(1, 1, new[] { Color.white });
            Material material = new Material(Shader.Find(ParticleShaderName));
            var gui = new UberShaderGUI();
            try
            {
                // CommandBuffer identity matrices do not own Unity's camera
                // position. Pin it so the geometry-rim normal/view relation is
                // independent of the Scene/Game camera that rendered last.
                Shader.SetGlobalVector("_WorldSpaceCameraPos",
                    new Vector4(0f, 0f, 1f, 1f));
                material.SetTexture("_BaseMap", baseMap);
                Color baseline = RenderParticleCore(material, 0f, Color.white);

                material.SetFloat("_MaskEnabled", 1f);
                material.SetTexture("_MaskMap", mask);
                material.SetFloat("_MaskChannel", 1f);
                material.SetFloat("_MaskStrength", 1f);
                gui.ValidateMaterial(material);
                Color masked = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_MaskInvert", 1f);
                Color inverted = RenderParticleCore(material, 0f, Color.white);
                Assert.That(masked.a, Is.EqualTo(0.4f).Within(0.04f));
                Assert.That(inverted.a, Is.EqualTo(0.6f).Within(0.04f));

                material.SetFloat("_MaskEnabled", 0f);
                material.SetFloat("_MaskInvert", 0f);
                material.SetFloat("_UVDistortionEnabled", 1f);
                material.SetTexture("_UVDistortionMap", flow);
                material.SetFloat("_UVDistortionStrength", 0.5f);
                material.SetFloat("_UVDistortionSpeed", 0f);
                material.SetVector("_UVDistortionDirection",
                    new Vector4(1f, 0f, 0f, 0f));
                gui.ValidateMaterial(material);
                Color flowed = RenderParticleCore(material, 0f, Color.white);
                Assert.That(flowed.b, Is.GreaterThan(baseline.b + 0.5f));
                Assert.That(flowed.r, Is.LessThan(baseline.r - 0.5f));

                material.SetFloat("_CustomDataEnabled", 1f);
                material.SetFloat("_CustomUVDistortionWeight", 1f);
                gui.ValidateMaterial(material);
                Color customFlowOff = RenderParticleCore(material, 0f,
                    Color.white, Vector4.zero);
                Color customFlowOn = RenderParticleCore(material, 0f,
                    Color.white, new Vector4(0f, 0f, 1f, 0f));
                Assert.That(customFlowOff.r,
                    Is.GreaterThan(customFlowOn.r + 0.5f));
                Assert.That(customFlowOn.b,
                    Is.GreaterThan(customFlowOff.b + 0.5f));

                material.SetFloat("_UVDistortionEnabled", 0f);
                material.SetFloat("_CustomUVDistortionWeight", 0f);
                material.SetFloat("_DissolveEnabled", 1f);
                material.SetTexture("_DissolveNoiseMap", noise);
                material.SetFloat("_DissolveMode", 0f);
                material.SetFloat("_DissolveAmount", 0.5f);
                material.SetFloat("_DissolveEdgeWidth", 0.2f);
                material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 1f));
                material.SetColor("_DissolveEdgeColor0",
                    new Color(0.4f, 0f, 0f, 1f));
                material.SetColor("_DissolveEdgeColor1",
                    new Color(0f, 0f, 0.4f, 1f));
                material.SetFloat("_DissolveEdgeEmission", 0f);
                gui.ValidateMaterial(material);
                noise.SetPixel(0, 0, new Color(0.51f, 0.51f, 0.51f, 1f));
                noise.Apply(false, false);
                Color innerEdge = RenderParticleCore(material, 0f, Color.white,
                    Vector4.zero);
                noise.SetPixel(0, 0, new Color(0.65f, 0.65f, 0.65f, 1f));
                noise.Apply(false, false);
                Color outerEdge = RenderParticleCore(material, 0f, Color.white,
                    Vector4.zero);
                Assert.That(innerEdge.r,
                    Is.GreaterThan(innerEdge.b + 0.25f));
                Assert.That(outerEdge.b,
                    Is.GreaterThan(outerEdge.r + 0.025f));
                noise.SetPixel(0, 0, new Color(0.51f, 0.51f, 0.51f, 1f));
                noise.Apply(false, false);
                material.SetFloat("_DissolveEdgeEmission", 1f);
                Color emittedEdge = RenderParticleCore(material, 0f, Color.white,
                    Vector4.zero);
                Assert.That(emittedEdge.r,
                    Is.GreaterThan(innerEdge.r + 0.25f));

                material.SetFloat("_DissolveEdgeEmission", 0f);
                noise.SetPixel(0, 0, new Color(0.75f, 0.75f, 0.75f, 1f));
                noise.Apply(false, false);
                material.SetFloat("_DissolveMode", 0f);
                gui.ValidateMaterial(material);
                Color noiseVisible = RenderParticleCore(material, 0f,
                    Color.white, Vector4.zero);
                material.SetFloat("_DissolveMode", 1f);
                material.SetVector("_DissolveRadialCenter",
                    new Vector4(0.25f, 0.5f, 0f, 0f));
                material.SetVector("_DissolveRadialRange",
                    new Vector4(0f, 1f, 0f, 0f));
                gui.ValidateMaterial(material);
                Color radialClipped = RenderParticleCore(material, 0f,
                    Color.white, Vector4.zero);
                noise.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 1f));
                noise.Apply(false, false);
                material.SetFloat("_DissolveMode", 2f);
                material.SetVector("_DissolveSwipeCenter",
                    new Vector4(0f, 0.5f, 0f, 0f));
                material.SetVector("_DissolveSwipeRange",
                    new Vector4(-0.1f, 0.1f, 0f, 0f));
                gui.ValidateMaterial(material);
                Color swipeVisible = RenderParticleCore(material, 0f,
                    Color.white, Vector4.zero);
                Assert.That(noiseVisible.a, Is.GreaterThan(0.9f));
                Assert.That(radialClipped.a, Is.LessThan(0.01f));
                Assert.That(swipeVisible.a, Is.GreaterThan(0.9f));

                noise.SetPixel(0, 0, new Color(0.55f, 0.55f, 0.55f, 1f));
                noise.Apply(false, false);
                material.SetFloat("_DissolveMode", 0f);
                material.SetFloat("_DissolveAmount", 0.8f);
                gui.ValidateMaterial(material);
                Color clipped = RenderParticleCore(material, 0f, Color.white,
                    Vector4.zero);
                Assert.That(clipped.a, Is.LessThan(0.01f));
                material.SetFloat("_CustomDissolveWeight", 1f);
                Color customVisible = RenderParticleCore(material, 0f,
                    Color.white, new Vector4(0.4f, 0f, 0f, 0f));
                Assert.That(customVisible.a, Is.GreaterThan(0.9f));

                material.SetFloat("_DissolveEnabled", 0f);
                material.SetFloat("_CustomDataEnabled", 0f);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_ColorAdjustEnabled", 1f);
                material.SetFloat("_HueShift", 120f);
                gui.ValidateMaterial(material);
                Color adjusted = RenderParticleCore(material, 0f, Color.white);
                Assert.That(adjusted.g, Is.GreaterThan(adjusted.r + 0.5f));

                material.SetFloat("_ColorAdjustEnabled", 0f);
                material.SetFloat("_FlipbookBlending", 1f);
                material.SetFloat("_CustomDataEnabled", 1f);
                material.SetColor("_BaseColor", Color.white);
                gui.ValidateMaterial(material);
                Color packedFlipbookCustom = RenderParticleCore(material, 0.5f,
                    Color.white, new Vector4(0.4f, 1f, 1f, 1f));
                Assert.That(packedFlipbookCustom.r, Is.GreaterThan(0.35f));
                Assert.That(packedFlipbookCustom.b, Is.GreaterThan(0.35f));
                Assert.That(Mathf.Abs(packedFlipbookCustom.r -
                    packedFlipbookCustom.b), Is.LessThan(0.15f));

                material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 1f));
                material.SetFloat("_EmissionEnabled", 1f);
                material.SetTexture("_EmissionMap", white);
                material.SetColor("_EmissionColor", Color.blue);
                material.SetFloat("_EmissionIntensity", 1f);
                material.SetFloat("_CustomDataEnabled", 1f);
                material.SetFloat("_CustomEmissionWeight", 1f);
                gui.ValidateMaterial(material);
                Color emissionOff = RenderParticleCore(material, 0f,
                    Color.white, Vector4.zero);
                Color emissionOn = RenderParticleCore(material, 0f,
                    Color.white, new Vector4(0f, 1f, 0f, 0f));
                Assert.That(emissionOn.b,
                    Is.GreaterThan(emissionOff.b + 0.5f));

                material.SetFloat("_EmissionEnabled", 0f);
                material.SetFloat("_FlipbookBlending", 0f);
                material.SetFloat("_CustomDataEnabled", 0f);
                material.SetFloat("_RimEnabled", 1f);
                material.SetColor("_RimColor", Color.red);
                material.SetFloat("_RimPower", 1f);
                material.SetFloat("_RimIntensity", 1f);
                material.SetFloat("_RimMode", 0f);
                gui.ValidateMaterial(material);
                Color geometryRim = RenderParticleCore(material, 0f, Color.white);
                material.SetFloat("_RimMode", 1f);
                material.SetVector("_RimRadialCenter",
                    new Vector4(0.25f, 0.5f, 0f, 0f));
                gui.ValidateMaterial(material);
                Color radialRim = RenderParticleCore(material, 0f, Color.white);
                Assert.That(geometryRim.r,
                    Is.GreaterThan(radialRim.r + 0.25f));

                material.SetTexture("_BaseMap", mask);
                material.SetTexture("_MaskMap", mask);
                material.SetTexture("_UVDistortionMap", flow);
                material.SetTexture("_DissolveNoiseMap", noise);
                material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.25f));
                material.SetFloat("_Cutoff", 0.5f);
                material.SetVector("_BaseNoiseClipCurveValues",
                    new Vector4(0.5f, 0.5f, 0.5f, 0.5f));
                material.SetVector("_BaseNoiseClipCurveMetadata", Vector4.one);
                material.SetFloat("_LifetimeGradientEnabled", 1f);
                material.SetFloat("_CustomDissolveWeight", 1f);
                foreach (string keyword in new[]
                         {
                             "_FLIPBOOKBLENDING_ON", "_MASK_ON",
                             "_UV_DISTORTION_ON", "_CUSTOM_DATA_ON",
                         })
                    material.EnableKeyword(keyword);
                material.SetFloat("_UVDistortionSpeed", 0f);
                material.SetFloat("_MaskInvert", 0f);
                material.SetFloat("_MaskStrength", 1f);
                Shader.SetGlobalFloat("_ObjectId", 0.25f);
                Shader.SetGlobalFloat("_PassValue", 0.5f);
                Shader.SetGlobalVector("_SelectionID",
                    new Vector4(0.2f, 0.4f, 0.6f, 0.8f));
                string[] coveragePasses =
                {
                    "DepthOnly", "DepthNormalsOnly", "SceneSelectionPass",
                    "ScenePickingPass",
                };
                Color coverageClear = new Color32(17, 43, 91, 137);
                Color32 coverageClearBytes = (Color32)coverageClear;
                Assert.That(coveragePasses.All(name => material.FindPass(name) >= 0),
                    Is.True);
                var coverageFrames = new List<Color[]>();
                var coverageByName = new Dictionary<string, Color[]>();
                CaptureCoverage("BaseNoise0", 0f, false, -1);
                CaptureCoverage("BaseNoise0.499", 0.499f, false, -1);
                CaptureCoverage("BaseNoise0.5", 0.5f, false, -1);
                CaptureCoverage("BaseNoise1", 1f, false, -1);
                CaptureCoverage("AlphaTest", 0f, true, -1);
                CaptureCoverage("DissolveOff", 0f, false, -1);
                CaptureCoverage("DissolveNoise", 0f, false, 0);
                CaptureCoverage("DissolveRadial", 0f, false, 1);
                CaptureCoverage("DissolveSwipe", 0f, false, 2);
                Assert.That(coverageFrames.SelectMany(frame => frame).All(IsFinite),
                    Is.True);
                Assert.That(HasPixelDifferentFrom(coverageFrames,
                        coverageClearBytes),
                    Is.True, "Coverage passes must produce drawn output.");
                Assert.That(HasDistinctFrames(coverageFrames), Is.True,
                    "Coverage scenarios must not all render identically.");
                CollectionAssert.AreEqual(Bytes("BaseNoise0"),
                    Bytes("BaseNoise0.499"));
                CollectionAssert.AreEqual(Bytes("BaseNoise0.5"),
                    Bytes("BaseNoise1"));
                for (int passIndex = 0; passIndex < coveragePasses.Length;
                     ++passIndex)
                    Assert.That(Bytes("BaseNoise0")[passIndex], Is.Not.EqualTo(
                        Bytes("BaseNoise0.5")[passIndex]),
                        coveragePasses[passIndex]);
                foreach (string clearFrame in new[]
                         {
                             "AlphaTest", "DissolveRadial",
                         })
                    Assert.That(Bytes(clearFrame).All(IsClear), Is.True,
                        clearFrame);
                foreach (string drawnFrame in new[]
                         {
                             "DissolveOff", "DissolveNoise",
                             "DissolveSwipe",
                         })
                    Assert.That(Bytes(drawnFrame).All(color => !IsClear(color)),
                        Is.True, drawnFrame);

                void CaptureCoverage(string name, float baseNoiseClip,
                    bool alphaTest, int dissolveMode)
                {
                    material.SetFloat("_BaseNoiseClipEnabled", baseNoiseClip);
                    SetKeyword(material, "_ALPHATEST_ON", alphaTest);
                    SetKeyword(material, "_DISSOLVE_ON", dissolveMode >= 0);
                    SetKeyword(material, "_DISSOLVE_RADIAL", dissolveMode == 1);
                    SetKeyword(material, "_DISSOLVE_SWIPE", dissolveMode == 2);
                    Color[] frame = coveragePasses.Select(passName =>
                        RenderParticleCore(material, 0.5f, Color.white,
                            new Vector4(0.4f, 0f, 1f, 1f), 16, 16,
                            material.FindPass(passName), coverageClear)).ToArray();
                    coverageFrames.Add(frame);
                    coverageByName.Add(name, frame);
                }

                Color32[] Bytes(string name) => coverageByName[name]
                    .Select(color => (Color32)color).ToArray();

                bool IsClear(Color32 color) => color.Equals(coverageClearBytes);

                void SetKeyword(Material target, string keyword, bool enabled)
                {
                    if (enabled)
                        target.EnableKeyword(keyword);
                    else
                        target.DisableKeyword(keyword);
                }
            }
            finally
            {
                Shader.SetGlobalVector("_WorldSpaceCameraPos",
                    previousCameraPosition);
                Shader.SetGlobalFloat("_ObjectId", previousObjectId);
                Shader.SetGlobalFloat("_PassValue", previousPassValue);
                Shader.SetGlobalVector("_SelectionID", previousSelectionId);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(baseMap);
                UnityEngine.Object.DestroyImmediate(mask);
                UnityEngine.Object.DestroyImmediate(flow);
                UnityEngine.Object.DestroyImmediate(noise);
                UnityEngine.Object.DestroyImmediate(white);
            }
        }

        [Test]
        public void ParticleAdvancedGpuPreservesCustomAndVertexFallbacks()
        {
            Texture2D white = CreateSolidTexture(1, 1, new[] { Color.white });
            Material material = new Material(Shader.Find(ParticleShaderName));
            var gui = new UberShaderGUI();
            try
            {
                material.SetTexture("_BaseMap", white);
                material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 1f));
                material.SetFloat("_EmissionEnabled", 1f);
                material.SetTexture("_EmissionMap", white);
                material.SetColor("_EmissionColor", Color.blue);
                material.SetFloat("_EmissionIntensity", 1f);
                material.SetFloat("_CustomDataEnabled", 1f);
                material.SetFloat("_CustomEmissionWeight", 1f);
                gui.ValidateMaterial(material);
                Color nonInstancedCustomZero = RenderParticleCore(material, 0f,
                    Color.white, Vector4.zero);
                Color proceduralFallback = RenderParticleProcedural(material,
                    Color.white);
                Assert.That(nonInstancedCustomZero.b, Is.LessThan(0.05f));
                Assert.That(proceduralFallback.b, Is.GreaterThan(0.5f));

                material.SetFloat("_EmissionEnabled", 0f);
                material.SetFloat("_CustomEmissionWeight", 0f);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_VertexOffsetEnabled", 1f);
                material.SetVector("_VertexOffsetDirection",
                    new Vector4(1f, 0f, 0f, 0f));
                material.SetFloat("_VertexOffsetAmplitude", 0.3f);
                material.SetFloat("_VertexOffsetFrequency",
                    Mathf.PI / (2f * 0.8f));
                material.SetFloat("_VertexOffsetSpeed", 0f);
                material.SetFloat("_CustomDataEnabled", 1f);
                material.SetFloat("_CustomVertexOffsetWeight", 1f);
                gui.ValidateMaterial(material);
                Color customZero = RenderParticleCore(material, 0f, Color.white,
                    Vector4.zero, 30, 16);
                Color customOne = RenderParticleCore(material, 0f, Color.white,
                    new Vector4(0f, 0f, 0f, 1f), 30, 16);
                Assert.That(IsFinite(customZero), Is.True);
                Assert.That(IsFinite(customOne), Is.True);
                Assert.That(customOne.a, Is.GreaterThan(customZero.a + 0.5f));

                material.SetFloat("_CustomDataEnabled", 0f);
                gui.ValidateMaterial(material);
                Color materialFallback = RenderParticleCore(material, 0f,
                    Color.white, Vector4.zero, 30, 16);
                Assert.That(materialFallback.a, Is.GreaterThan(0.5f));

                material.SetVector("_VertexOffsetDirection", Vector4.zero);
                material.SetFloat("_VertexOffsetAmplitude", 100f);
                material.SetFloat("_VertexOffsetFrequency", 0f);
                Color degenerate = RenderParticleCore(material, 0f, Color.white);
                Assert.That(IsFinite(degenerate), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(white);
            }
        }

        [Test]
        public void ParticleCustomVertexStreamProbesMatchBothDocumentedOrders()
        {
            GameObject gameObject = new GameObject("Transient Particle Streams");
            GameObject cameraObject = new GameObject("Transient Particle Camera");
            Mesh withoutFlipbookMesh = new Mesh
            {
                name = "Transient Particle Streams Without Flipbook",
            };
            Mesh withFlipbookMesh = new Mesh
            {
                name = "Transient Particle Streams With Flipbook",
            };
            try
            {
                ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
                ParticleSystemRenderer renderer =
                    gameObject.GetComponent<ParticleSystemRenderer>();
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = 10f;
                main.startSpeed = 0f;
                main.startSize = 1f;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.enabled = false;
                ParticleSystem.CustomDataModule customData = particles.customData;
                customData.enabled = true;
                customData.SetMode(ParticleSystemCustomData.Custom1,
                    ParticleSystemCustomDataMode.Vector);
                customData.SetVectorComponentCount(
                    ParticleSystemCustomData.Custom1, 4);
                float[] expectedCustom = { 0.11f, 0.22f, 0.33f, 0.44f };
                for (int component = 0; component < expectedCustom.Length;
                     ++component)
                {
                    customData.SetVector(ParticleSystemCustomData.Custom1,
                        component, new ParticleSystem.MinMaxCurve(
                            expectedCustom[component]));
                }

                particles.Stop(true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play();
                particles.Emit(new ParticleSystem.EmitParams
                {
                    position = Vector3.zero,
                    velocity = Vector3.zero,
                    startColor = Color.white,
                    startLifetime = 10f,
                    startSize = 1f,
                }, 1);
                particles.Simulate(0.1f, true, false, false);
                Assert.That(particles.particleCount, Is.EqualTo(1));
                var actual = new List<ParticleSystemVertexStream>();

                var withoutFlipbook = new List<ParticleSystemVertexStream>
                {
                    ParticleSystemVertexStream.Position,
                    ParticleSystemVertexStream.Normal,
                    ParticleSystemVertexStream.Color,
                    ParticleSystemVertexStream.UV,
                    ParticleSystemVertexStream.Custom1XYZW,
                };
                renderer.SetActiveVertexStreams(withoutFlipbook);
                renderer.GetActiveVertexStreams(actual);
                CollectionAssert.AreEqual(withoutFlipbook, actual);
                renderer.BakeMesh(withoutFlipbookMesh, camera,
                    ParticleSystemBakeMeshOptions.Default);
                Assert.That(withoutFlipbookMesh.vertexCount, Is.GreaterThan(0));
                Assert.That(withoutFlipbookMesh.GetVertexAttributeDimension(
                    VertexAttribute.TexCoord0), Is.EqualTo(4));
                Assert.That(withoutFlipbookMesh.GetVertexAttributeDimension(
                    VertexAttribute.TexCoord1), Is.EqualTo(2));
                var withoutUv0 = new List<Vector4>();
                var withoutUv1 = new List<Vector4>();
                withoutFlipbookMesh.GetUVs(0, withoutUv0);
                withoutFlipbookMesh.GetUVs(1, withoutUv1);
                Assert.That(withoutUv0.Count,
                    Is.EqualTo(withoutFlipbookMesh.vertexCount));
                Assert.That(withoutUv1.Count,
                    Is.EqualTo(withoutFlipbookMesh.vertexCount));
                foreach (Vector4 uv in withoutUv0)
                {
                    Assert.That(IsFinite(uv), Is.True);
                    Assert.That(uv.x, Is.InRange(-0.001f, 1.001f));
                    Assert.That(uv.y, Is.InRange(-0.001f, 1.001f));
                    Assert.That(uv.z, Is.EqualTo(expectedCustom[0]).Within(0.001f));
                    Assert.That(uv.w, Is.EqualTo(expectedCustom[1]).Within(0.001f));
                }
                Assert.That(withoutUv0.Min(uv => uv.x), Is.EqualTo(0f).Within(0.001f));
                Assert.That(withoutUv0.Max(uv => uv.x), Is.EqualTo(1f).Within(0.001f));
                Assert.That(withoutUv0.Min(uv => uv.y), Is.EqualTo(0f).Within(0.001f));
                Assert.That(withoutUv0.Max(uv => uv.y), Is.EqualTo(1f).Within(0.001f));
                foreach (Vector4 uv in withoutUv1)
                {
                    Assert.That(uv.x, Is.EqualTo(expectedCustom[2]).Within(0.001f));
                    Assert.That(uv.y, Is.EqualTo(expectedCustom[3]).Within(0.001f));
                }

                var withFlipbook = new List<ParticleSystemVertexStream>
                {
                    ParticleSystemVertexStream.Position,
                    ParticleSystemVertexStream.Normal,
                    ParticleSystemVertexStream.Color,
                    ParticleSystemVertexStream.UV,
                    ParticleSystemVertexStream.UV2,
                    ParticleSystemVertexStream.AnimBlend,
                    ParticleSystemVertexStream.Custom1XYZW,
                };
                ParticleSystem.TextureSheetAnimationModule textureSheet =
                    particles.textureSheetAnimation;
                textureSheet.enabled = true;
                textureSheet.mode = ParticleSystemAnimationMode.Grid;
                textureSheet.numTilesX = 2;
                textureSheet.numTilesY = 1;
                textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(0.25f);
                renderer.SetActiveVertexStreams(withFlipbook);
                actual.Clear();
                renderer.GetActiveVertexStreams(actual);
                CollectionAssert.AreEqual(withFlipbook, actual);
                renderer.BakeMesh(withFlipbookMesh, camera,
                    ParticleSystemBakeMeshOptions.Default);
                Assert.That(withFlipbookMesh.vertexCount, Is.GreaterThan(0));
                Assert.That(withFlipbookMesh.GetVertexAttributeDimension(
                    VertexAttribute.TexCoord0), Is.EqualTo(4));
                Assert.That(withFlipbookMesh.GetVertexAttributeDimension(
                    VertexAttribute.TexCoord1), Is.EqualTo(4));
                Assert.That(withFlipbookMesh.GetVertexAttributeDimension(
                    VertexAttribute.TexCoord2), Is.EqualTo(1));
                var withUv1 = new List<Vector4>();
                var withUv2 = new List<Vector4>();
                withFlipbookMesh.GetUVs(1, withUv1);
                withFlipbookMesh.GetUVs(2, withUv2);
                Assert.That(withUv1.Count, Is.EqualTo(withFlipbookMesh.vertexCount));
                Assert.That(withUv2.Count, Is.EqualTo(withFlipbookMesh.vertexCount));
                foreach (Vector4 uv in withUv1)
                {
                    Assert.That(float.IsNaN(uv.x) || float.IsInfinity(uv.x),
                        Is.False);
                    Assert.That(uv.y, Is.EqualTo(expectedCustom[0]).Within(0.001f));
                    Assert.That(uv.z, Is.EqualTo(expectedCustom[1]).Within(0.001f));
                    Assert.That(uv.w, Is.EqualTo(expectedCustom[2]).Within(0.001f));
                }
                foreach (Vector4 uv in withUv2)
                    Assert.That(uv.x, Is.EqualTo(expectedCustom[3]).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(withoutFlipbookMesh);
                UnityEngine.Object.DestroyImmediate(withFlipbookMesh);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ParticleAgePercentBakesToDefaultTexcoord0Z()
        {
            GameObject gameObject = new GameObject(
                "Transient Particle AgePercent Stream");
            GameObject cameraObject = new GameObject(
                "Transient Particle AgePercent Camera");
            Mesh mesh = new Mesh
            {
                name = "Transient Particle AgePercent Mesh",
            };
            try
            {
                ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
                ParticleSystemRenderer renderer =
                    gameObject.GetComponent<ParticleSystemRenderer>();
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = 10f;
                main.startSpeed = 0f;
                main.startSize = 1f;
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.enabled = false;
                particles.Stop(true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play();
                particles.Emit(new ParticleSystem.EmitParams
                {
                    position = Vector3.zero,
                    velocity = Vector3.zero,
                    startColor = Color.white,
                    startLifetime = 10f,
                    startSize = 1f,
                }, 1);
                particles.Simulate(2.5f, true, false, false);

                var expectedStreams = new List<ParticleSystemVertexStream>
                {
                    ParticleSystemVertexStream.Position,
                    ParticleSystemVertexStream.Normal,
                    ParticleSystemVertexStream.Color,
                    ParticleSystemVertexStream.UV,
                    ParticleSystemVertexStream.AgePercent,
                };
                renderer.SetActiveVertexStreams(expectedStreams);
                var actualStreams = new List<ParticleSystemVertexStream>();
                renderer.GetActiveVertexStreams(actualStreams);
                CollectionAssert.AreEqual(expectedStreams, actualStreams);

                renderer.BakeMesh(mesh, camera,
                    ParticleSystemBakeMeshOptions.Default);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                Assert.That(mesh.GetVertexAttributeDimension(
                    VertexAttribute.TexCoord0), Is.EqualTo(3));
                var uv0 = new List<Vector4>();
                mesh.GetUVs(0, uv0);
                Assert.That(uv0.Count, Is.EqualTo(mesh.vertexCount));
                foreach (Vector4 uv in uv0)
                {
                    Assert.That(IsFinite(uv), Is.True);
                    Assert.That(uv.z, Is.EqualTo(0.25f).Within(0.03f));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static Texture2D CreateSolidTexture(int width, int height,
            Color[] colors)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32,
                false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Transient Particle Test Texture",
            };
            texture.SetPixels(colors);
            texture.Apply(false, false);
            return texture;
        }

        private struct TransientParticleInstanceData
        {
            public Vector3 Column0;
            public Vector3 Column1;
            public Vector3 Column2;
            public Vector3 Column3;
            public uint Color;
            public float AnimFrame;
        }

        private static Color RenderParticleProcedural(Material material,
            Color vertexColor)
        {
            Mesh mesh = new Mesh { name = "Transient Procedural Particle Quad" };
            RenderTexture target = new RenderTexture(32, 32, 24,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D readback = new Texture2D(32, 32, TextureFormat.RGBA32,
                false, true);
            ComputeBuffer instances = new ComputeBuffer(1, 56);
            CommandBuffer commands = new CommandBuffer
            {
                name = "Transient Procedural Particle Fallback Render",
            };
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            RenderTexture previous = RenderTexture.active;
            bool previousInstancing = material.enableInstancing;
            try
            {
                mesh.vertices = new[]
                {
                    new Vector3(-0.8f, -0.8f, 0f),
                    new Vector3(0.8f, -0.8f, 0f),
                    new Vector3(0.8f, 0.8f, 0f),
                    new Vector3(-0.8f, 0.8f, 0f),
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
                mesh.colors = Enumerable.Repeat(vertexColor, 4).ToArray();
                mesh.uv = Enumerable.Repeat(new Vector2(0.25f, 0.5f), 4)
                    .ToArray();
                mesh.UploadMeshData(false);

                instances.SetData(new[]
                {
                    new TransientParticleInstanceData
                    {
                        Column0 = new Vector3(1f, 0f, 0f),
                        Column1 = new Vector3(0f, 1f, 0f),
                        Column2 = new Vector3(0f, 0f, 1f),
                        Column3 = Vector3.zero,
                        Color = 0xffffffffu,
                        AnimFrame = 0f,
                    },
                });
                properties.SetBuffer("unity_ParticleInstanceData", instances);
                properties.SetVector("unity_ParticleUVShiftData", Vector4.zero);
                properties.SetFloat("unity_ParticleUseMeshColors", 1f);
                material.enableInstancing = true;
                target.Create();
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, Color.clear);
                commands.SetViewProjectionMatrices(Matrix4x4.identity,
                    Matrix4x4.identity);
                commands.DrawMeshInstancedProcedural(mesh, 0, material, 0, 1,
                    properties);
                Graphics.ExecuteCommandBuffer(commands);

                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 32, 32), 0, 0, false);
                readback.Apply(false, false);
                return readback.GetPixel(16, 16);
            }
            finally
            {
                material.enableInstancing = previousInstancing;
                RenderTexture.active = previous;
                commands.Release();
                instances.Release();
                target.Release();
                UnityEngine.Object.DestroyImmediate(readback);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Color RenderParticleCore(Material material, float blend,
            Color vertexColor)
        {
            return RenderParticleCore(material, blend, vertexColor, Vector4.zero,
                16, 16);
        }

        private static Color RenderParticleCore(Material material, float blend,
            Color vertexColor, Vector4 custom1)
        {
            return RenderParticleCore(material, blend, vertexColor, custom1,
                16, 16);
        }

        private static Color RenderParticleCore(Material material, float blend,
            Color vertexColor, Vector4 custom1, int sampleX, int sampleY,
            int shaderPass = 0, Color? clearColor = null)
        {
            Mesh mesh = new Mesh { name = "Transient Uber Particle Quad" };
            RenderTexture target = new RenderTexture(32, 32, 24,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D readback = new Texture2D(32, 32, TextureFormat.RGBA32,
                false, true);
            CommandBuffer commands = new CommandBuffer
            {
                name = "Transient Uber Particle Core Render",
            };
            RenderTexture previous = RenderTexture.active;
            try
            {
                mesh.vertices = new[]
                {
                    new Vector3(-0.8f, -0.8f, 0f),
                    new Vector3(0.8f, -0.8f, 0f),
                    new Vector3(0.8f, 0.8f, 0f),
                    new Vector3(-0.8f, 0.8f, 0f),
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
                mesh.colors = Enumerable.Repeat(vertexColor, 4).ToArray();
                bool customData = material.IsKeywordEnabled("_CUSTOM_DATA_ON");
                bool flipbook = material.IsKeywordEnabled(
                    "_FLIPBOOKBLENDING_ON");
                if (customData && flipbook)
                {
                    mesh.SetUVs(0, Enumerable.Repeat(
                        new Vector4(0.25f, 0.5f, 0.75f, 0.5f), 4).ToList());
                    mesh.SetUVs(1, Enumerable.Repeat(new Vector4(blend,
                        custom1.x, custom1.y, custom1.z), 4).ToList());
                    mesh.SetUVs(2, Enumerable.Repeat(
                        new Vector2(custom1.w, 0f), 4).ToList());
                }
                else if (customData)
                {
                    mesh.SetUVs(0, Enumerable.Repeat(new Vector4(0.25f, 0.5f,
                        custom1.x, custom1.y), 4).ToList());
                    mesh.SetUVs(1, Enumerable.Repeat(
                        new Vector2(custom1.z, custom1.w), 4).ToList());
                }
                else
                {
                    mesh.SetUVs(0, Enumerable.Repeat(
                        new Vector4(0.25f, 0.5f, 0.75f, 0.5f), 4).ToList());
                    mesh.SetUVs(1, Enumerable.Repeat(new Vector2(blend, 0f), 4)
                        .ToList());
                }
                mesh.UploadMeshData(false);

                target.Create();
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true,
                    clearColor ?? Color.clear);
                commands.SetViewProjectionMatrices(Matrix4x4.identity,
                    Matrix4x4.identity);
                commands.DrawMesh(mesh, Matrix4x4.identity, material, 0,
                    shaderPass);
                Graphics.ExecuteCommandBuffer(commands);

                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 32, 32), 0, 0, false);
                readback.Apply(false, false);
                return readback.GetPixel(sampleX, sampleY);
            }
            finally
            {
                RenderTexture.active = previous;
                commands.Release();
                target.Release();
                UnityEngine.Object.DestroyImmediate(readback);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

    }
}
