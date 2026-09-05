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
        public void ObjectInspectorUsesSpacedCollapsibleLwguiGroups()
        {
            string source = Read(UberDirectory + "Uber3D.shader");
            Match propertiesMatch = Regex.Match(source,
                @"(?s)\bProperties\s*\{(?<body>.*?)\r?\n\s*\}\s*\r?\n\s*SubShader\b");
            Assert.That(propertiesMatch.Success, Is.True);
            string properties = propertiesMatch.Groups["body"].Value;
            StringAssert.DoesNotContain("[Header(", properties);

            string[] mainSignatures =
            {
                "Surface, _, on, off", "SurfaceInputs, _, on, off",
                "TextureBlend, _TEXTURE_BLEND_ON, on",
                "ColorAdjust, _COLOR_ADJUST_ON, on", "Emission, _EMISSION, on",
                "Rim, _RIM_ON, on", "HeightFade, _HEIGHT_FADE_ON, on",
                "GlassGlow, _GLASS_GLOW_ON, on", "Hologram, _HOLOGRAM_ON, on",
                "Glitch, _GLITCH_ON, on",
                "Dissolve, _DISSOLVE_ON, on",
                "DitherFade, _DITHER_FADE_ON, on",
                "StencilOutline, _STENCIL_OUTLINE_ON, on",
            };
            string[] childOwners =
            {
                "Surface", "SurfaceInputs", "TextureBlend", "ColorAdjust", "Emission", "Rim",
                "HeightFade", "GlassGlow", "Hologram", "Glitch", "Dissolve",
                "DitherFade", "StencilOutline",
            };
            string[] rows = Lines(properties).Where(line =>
                Regex.IsMatch(line, @"_[A-Za-z][A-Za-z0-9_]*\s*\(")).ToArray();
            string[] actualMains = rows.Select(line => Regex.Match(line,
                    @"\[Main\((?<signature>[^\)]*)\)\]"))
                .Where(match => match.Success)
                .Select(match => match.Groups["signature"].Value).ToArray();
            CollectionAssert.AreEqual(mainSignatures, actualMains);
            CollectionAssert.AreEqual(childOwners, Regex.Matches(properties,
                    @"\[Title\((?<owner>[^,]+),\s*_\)\]").Cast<Match>()
                .Select(match => match.Groups["owner"].Value).ToArray());
            Assert.That(Regex.IsMatch(properties,
                @"\[Sub\([^\)]*\)\]\s*\[Enum\("), Is.False);

            string[,] specializedRows =
            {
                { "_Surface", "[Title(Surface, _)] [KWEnum(Surface, Opaque, _, Transparent, _SURFACE_TYPE_TRANSPARENT)] _Surface(\"Surface Type\", Float) = 0" },
                { "_Blend", "[KWEnum(Surface, Alpha, _, Premultiply, _ALPHAPREMULTIPLY_ON, Additive, _, Multiply, _ALPHAMODULATE_ON)] _Blend(\"Blend Mode\", Float) = 0" },
                { "_ZWriteControl", "[KWEnum(Surface, Auto, _, On, _, Off, _)] _ZWriteControl(\"Depth Write\", Float) = 0" },
                { "_LightingMode", "[KWEnum(Surface, Lit, _, Unlit, _UNLIT_ON)] _LightingMode(\"Lighting Mode\", Float) = 0" },
                { "_Cull", "[KWEnum(Surface, Off, _, Front, _, Back, _)] _Cull(\"Render Face\", Float) = 2" },
                { "_UberQuality", "[KWEnum(Surface, High, _, Low, _UBER_QUALITY_LOW)] _UberQuality(\"Effect Quality\", Float) = 0" },
                { "_QueueControl", "[KWEnum(Surface, Auto, _, Custom, _)] _QueueControl(\"Queue Control\", Float) = 0" },
                { "_BaseMap", "[Title(SurfaceInputs, _)] [UberGroup(SurfaceInputs)] [MainTexture] _BaseMap(\"Base Map\", 2D) = \"white\" {}" },
                { "_BaseMapMapping", "[KWEnum(SurfaceInputs, UV, _, Triplanar, _BASE_MAP_TRIPLANAR)] _BaseMapMapping(\"Base Map Mapping\", Float) = 0" },
                { "_BaseMap3DTiling", "[UberVector3(SurfaceInputs)] _BaseMap3DTiling(\"Base Map 3D Tiling\", Vector) = (1, 1, 1, 0)" },
                { "_MetallicMap", "[Tex(SurfaceInputs_METALLICMAP)] [NoScaleOffset] _MetallicMap(\"Metallic Map (R)\", 2D) = \"white\" {}" },
                { "_RoughnessMap", "[Tex(SurfaceInputs_ROUGHNESSMAP)] [NoScaleOffset] _RoughnessMap(\"Roughness Map (R)\", 2D) = \"black\" {}" },
                { "_BumpMap", "[Tex(SurfaceInputs)] [Normal] [NoScaleOffset] _BumpMap(\"Normal Map\", 2D) = \"bump\" {}" },
                { "_BlendMap", "[Title(TextureBlend, _)] [Tex(TextureBlend)] [NoScaleOffset] _BlendMap(\"Blend Map\", 2D) = \"white\" {}" },
                { "_BlendTiling", "[UberVector2(TextureBlend)] _BlendTiling(\"Blend Tiling\", Vector) = (1, 1, 0, 0)" },
                { "_EmissionMap", "[Title(Emission, _)] [Tex(Emission)] [NoScaleOffset] _EmissionMap(\"Emission Map\", 2D) = \"white\" {}" },
                { "_HologramSpace", "[KWEnum(Hologram, Object, _, World, _HOLOGRAM_WORLD_SPACE, Screen, _HOLOGRAM_SCREEN_SPACE)] _HologramSpace(\"Space\", Float) = 0" },
                { "_HologramObjectUpVector", "[UberVector3(Hologram)] _HologramObjectUpVector(\"Object Up Vector\", Vector) = (0, 1, 0, 0)" },
                { "_GlitchBandSizeRange", "[UberMinMaxVector(Glitch)] _GlitchBandSizeRange(\"Band Size Range (Pixels)\", Vector) = (4, 12, 0, 0)" },
                { "_DissolveNoiseMap", "[Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap(\"Noise Map\", 2D) = \"white\" {}" },
                { "_DissolveSpace", "[KWEnum(Dissolve, UV, _, ObjectSpace, _DISSOLVE_OBJECT_SPACE)] _DissolveSpace(\"Space\", Float) = 0" },
                { "_DissolveNoiseAmountMovement", "[UberVector2(Dissolve)] _DissolveNoiseAmountMovement(\"Noise Movement\", Vector) = (1, 0, 0, 0)" },
                { "_DissolveObjectUpVector", "[UberVector3(Dissolve)] _DissolveObjectUpVector(\"Up Vector\", Vector) = (0, 1, 0, 0)" },
                { "_DissolveObjectRange", "[UberMinMaxVector(Dissolve)] _DissolveObjectRange(\"Range\", Vector) = (0, 1, 0, 0)" },
                { "_DissolveObjectNoiseScale", "[Sub(Dissolve)] _DissolveObjectNoiseScale(\"Object Noise Size\", Range(0.05, 4)) = 1" },
            };
            for (int index = 0; index < specializedRows.GetLength(0); ++index)
            {
                string propertyName = specializedRows[index, 0];
                string row = rows.Single(candidate => Regex.IsMatch(candidate,
                    @"(?<![A-Za-z0-9_])" + Regex.Escape(propertyName) + @"\s*\("));
                Assert.That(row, Is.EqualTo(specializedRows[index, 1]), propertyName);
            }

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
                    "[Sub(" + owner + ")]", "[SubToggle(" + owner + ",",
                    "[KWEnum(" + owner + ",", "[Tex(" + owner + ")]",
                    "[Tex(" + owner + "_",
                    "[UberGroup(" + owner + ")]",
                    "[UberVector2(" + owner + ")]",
                    "[UberVector3(" + owner + ")]",
                    "[UberMinMaxVector(" + owner + ")]",
                };
                Assert.That(groupDrawers.Any(drawer => row.Contains(drawer)),
                    Is.True, row);
                if (!firstChild)
                    continue;
                StringAssert.Contains("[Title(" + owner + ", _)]", row, row);
                firstChild = false;
            }
            Assert.That(section, Is.EqualTo(childOwners.Length - 1));

            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            StringAssert.Contains(
                "public sealed class UberGroupDrawer : MaterialPropertyDrawer", gui);
            StringAssert.Contains("LWGUI.Helper.IsVisible(group)", gui);
            StringAssert.Contains("editor.DefaultShaderProperty(position, property",
                gui);
            StringAssert.Contains("MaterialEditor.GetDefaultPropertyHeight(property)",
                gui);
            StringAssert.Contains(
                "public sealed class UberVector2Drawer : LWGUI.SubDrawer", gui);
            StringAssert.Contains(
                "public sealed class UberVector3Drawer : LWGUI.SubDrawer", gui);
            StringAssert.Contains(
                "public sealed class UberMinMaxVectorDrawer : LWGUI.SubDrawer", gui);
            StringAssert.Contains(
                "UberDrawerLayout.DrawFloatComponents(", gui);
            StringAssert.Contains(
                "int indentLevel = EditorGUI.indentLevel;", gui);
            StringAssert.Contains(
                "EditorGUI.indentLevel = 0;", gui);
            StringAssert.Contains(
                "EditorGUI.indentLevel = indentLevel;", gui);
            StringAssert.Contains(
                "componentPosition, labels[index], values[index]", gui);
            StringAssert.DoesNotContain(
                "EditorGUI.LabelField(componentLabelPosition", gui);
            StringAssert.DoesNotContain(
                "EditorGUI.Vector3Field(position, label", gui);
            StringAssert.DoesNotContain(
                "EditorGUI.MultiFloatField(valuePosition", gui);
        }

        [Test]
        public void ObjectSurfaceMapVariantsCompileForWebGlGles3()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                UberDirectory + "Uber3D.shader");
            Assert.That(shader, Is.Not.Null);
            ShaderData.Subshader subshader =
                ShaderUtil.GetShaderData(shader).ActiveSubshader;
            string[][] keywordRows =
            {
                new[] { "_METALLICMAP" },
                new[] { "_ROUGHNESSMAP" },
                new[] { "_METALLICMAP", "_ROUGHNESSMAP" },
                new[] { "_BASE_MAP_TRIPLANAR", "_METALLICMAP" },
                new[] { "_BASE_MAP_TRIPLANAR", "_ROUGHNESSMAP" },
                new[]
                {
                    "_BASE_MAP_TRIPLANAR", "_METALLICMAP", "_ROUGHNESSMAP",
                },
            };

            foreach (string passName in new[] { "UniversalForward", "Meta" })
            foreach (string[] keywords in keywordRows)
            {
                ShaderData.Pass pass = Enumerable.Range(0, subshader.PassCount)
                    .Select(subshader.GetPass)
                    .Single(candidate => candidate.Name == passName);
                var compiled = pass.CompileVariant(ShaderType.Fragment, keywords,
                    ShaderCompilerPlatform.GLES3x, BuildTarget.WebGL);
                string context = passName + " " + string.Join(" ", keywords);
                string[] diagnostics = compiled.Messages.Where(message =>
                        message.severity == ShaderCompilerMessageSeverity.Warning ||
                        message.severity == ShaderCompilerMessageSeverity.Error)
                    .Select(message => message.severity + ": " + message.message)
                    .ToArray();
                Assert.That(compiled.Success, Is.True,
                    context + ": " + string.Join(" | ", diagnostics));
                Assert.That(diagnostics, Is.Empty, context);
            }
        }

        [Test]
        public void ObjectTextureBlendUsesUpwardWorldNormalAndLocalVariant()
        {
            string shader = Read(UberDirectory + "Uber3D.shader");
            string include = Read(UberDirectory + "Uber3D.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");

            Assert.That(Regex.Matches(shader,
                @"#pragma\s+multi_compile_local_fragment\s+_\s+_TEXTURE_BLEND_ON").Count,
                Is.EqualTo(2));
            Assert.That(Regex.Matches(shader,
                @"#pragma\s+multi_compile_local\s+_\s+_BASE_MAP_TRIPLANAR").Count,
                Is.EqualTo(6));
            StringAssert.Contains("float4 _BlendTiling;", include);
            StringAssert.Contains("float4 _BaseMap3DTiling;", include);
            StringAssert.Contains("TEXTURE2D(_BlendMap);", include);
            StringAssert.Contains("inline half4 UberSampleBaseMapped(", include);
            StringAssert.Contains("float3 mappingTiling = max(abs(_BaseMap3DTiling.xyz), 0.0001);", include);
            StringAssert.Contains("float3 mappingPosition = positionWS * mappingTiling;", include);
            StringAssert.Contains("xSample * blendWeights.x + ySample * blendWeights.y", include);
            StringAssert.Contains("float2 blendUV = rawUV * _BlendTiling.xy;", include);
            StringAssert.Contains("normalize(geometricNormalWS).y", include);
            StringAssert.Contains("smoothstep(_BlendThreshold - blendWidth,", include);
            StringAssert.Contains("return lerp(baseAlbedo, blendAlbedo, blendWeight);", include);
            StringAssert.Contains(
                "new KeywordBinding(\"_TextureBlendEnabled\", \"_TEXTURE_BLEND_ON\", 1)",
                gui);
            StringAssert.Contains(
                "new KeywordBinding(\"_BaseMapMapping\", \"_BASE_MAP_TRIPLANAR\", 1)",
                gui);

            Material material = new Material(Shader.Find(ObjectShaderName));
            try
            {
                Assert.That(material.HasProperty("_TextureBlendEnabled"), Is.True);
                Assert.That(material.HasProperty("_BlendMap"), Is.True);
                Assert.That(material.HasProperty("_BlendTiling"), Is.True);
                Assert.That(material.HasProperty("_BaseMapMapping"), Is.True);
                Assert.That(material.HasProperty("_BaseMap3DTiling"), Is.True);
                Assert.That(material.GetFloat("_TextureBlendEnabled"), Is.Zero);

                material.SetFloat("_BaseMapMapping", 1f);
                new UberShaderGUI().ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_BASE_MAP_TRIPLANAR"), Is.True);

                material.SetFloat("_TextureBlendEnabled", 1f);
                new UberShaderGUI().ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_TEXTURE_BLEND_ON"), Is.True);

                material.SetFloat("_TextureBlendEnabled", 0f);
                new UberShaderGUI().ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_TEXTURE_BLEND_ON"), Is.False);

                Material ground = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/05. Arts/Material/RocketBase/MAT_ground.mat");
                Texture2D dirt = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/05. Arts/Texture/RocketBase/dirt.png");
                Texture2D grass = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/05. Arts/Texture/RocketBase/grass.png");
                Assert.That(ground, Is.Not.Null);
                Assert.That(ground.GetTexture("_BaseMap"), Is.SameAs(dirt));
                Assert.That(ground.GetTexture("_BlendMap"), Is.SameAs(grass));
                Assert.That(ground.IsKeywordEnabled("_TEXTURE_BLEND_ON"), Is.True);
                Assert.That(ground.IsKeywordEnabled("_BASE_MAP_TRIPLANAR"), Is.True);
                Assert.That(ground.GetFloat("_BaseMapMapping"), Is.EqualTo(1f));
                Vector4 baseMap3DTiling = ground.GetVector("_BaseMap3DTiling");
                Assert.That(baseMap3DTiling.x, Is.GreaterThan(0f));
                Assert.That(baseMap3DTiling.y, Is.GreaterThan(0f));
                Assert.That(baseMap3DTiling.z, Is.GreaterThan(0f));
                Vector4 blendTiling = ground.GetVector("_BlendTiling");
                Assert.That(blendTiling.x, Is.GreaterThan(0f));
                Assert.That(blendTiling.y, Is.GreaterThan(0f));
                Assert.That(ground.GetFloat("_BlendThreshold"), Is.InRange(-1f, 1f));
                Assert.That(ground.GetFloat("_BlendSmoothness"), Is.InRange(0.01f, 1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ObjectSpaceDissolveUsesUpVectorRangeAndAmount()
        {
            string shader = Read(UberDirectory + "Uber3D.shader");
            string include = Read(UberDirectory + "Uber3D.hlsl");
            string combined = shader + include;

            StringAssert.DoesNotContain("_DissolveObjectAxis", combined);
            StringAssert.DoesNotContain("_DissolveObjectMin", combined);
            StringAssert.DoesNotContain("_DissolveObjectMax", combined);
            StringAssert.DoesNotContain("_DissolveMinOffset", combined);
            StringAssert.DoesNotContain("_DissolveMaxOffset", combined);
            StringAssert.Contains("float4 _DissolveObjectUpVector;", include);
            StringAssert.Contains("float4 _DissolveObjectRange;", include);
            StringAssert.Contains(
                "float4 _DissolveNoiseAmountMovement;", include);
            StringAssert.Contains(
                "float3 upVector = UberGetDissolveUpVector();", include);
            StringAssert.Contains(
                "float upCoordinate = dot(positionOS, upVector);", include);
            StringAssert.Contains(
                "UberSafeInverseLerp(_DissolveObjectRange.x,", include);
            StringAssert.Contains(
                "UberGetObjectNoiseUV(positionOS, upVector)", include);
            StringAssert.Contains(
                "_DissolveObjectRange.y - _DissolveObjectRange.x", include);
            StringAssert.Contains(
                "float coordinateScale = rcp(rangeSize * noiseSize);", include);
            StringAssert.Contains(
                "return noiseUV * _DissolveTilingOffset.xy + " +
                "_DissolveTilingOffset.zw;", include);
            Assert.That(Regex.IsMatch(include,
                @"UberTransformDissolveNoiseUV\s*\(\s*" +
                @"UberGetObjectNoiseUV\(positionOS,\s*upVector\)\)"),
                Is.True);
            Assert.That(Regex.Matches(include,
                @"\bUberTransformDissolveNoiseUV\s*\(").Count,
                Is.EqualTo(3));
            StringAssert.Contains(
                "float2 amountMovement = _DissolveNoiseAmountMovement.xy",
                include);
            Assert.That(Regex.Matches(include,
                @"\+\s*amountMovement").Count, Is.EqualTo(2));
            StringAssert.Contains(
                "(noise - 0.5h) * _DissolveObjectNoiseStrength", include);
            StringAssert.Contains("field = saturate(noise);", include);
            StringAssert.Contains(
                "field - saturate(_DissolveAmount)", include);

            Material material = new Material(Shader.Find(ObjectShaderName));
            try
            {
                Assert.That(material.HasProperty("_DissolveObjectUpVector"),
                    Is.True);
                Assert.That(material.GetVector("_DissolveObjectUpVector"),
                    Is.EqualTo(new Vector4(0f, 1f, 0f, 0f)));
                Assert.That(material.HasProperty("_DissolveObjectRange"), Is.True);
                Assert.That(material.GetVector("_DissolveObjectRange"),
                    Is.EqualTo(new Vector4(0f, 1f, 0f, 0f)));
                Assert.That(material.HasProperty("_DissolveAmount"), Is.True);
                Assert.That(material.HasProperty("_DissolveMinOffset"), Is.False);
                Assert.That(material.HasProperty("_DissolveMaxOffset"), Is.False);
                Assert.That(material.HasProperty(
                    "_DissolveNoiseAmountMovement"), Is.True);
                Assert.That(material.GetVector(
                    "_DissolveNoiseAmountMovement"),
                    Is.EqualTo(new Vector4(1f, 0f, 0f, 0f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void HologramUsesTransparentLocalSelectableSpaceContract()
        {
            string shader = Read(UberDirectory + "Uber3D.shader");
            string include = Read(UberDirectory + "Uber3D.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            string variants = Read(VariantPath);

            StringAssert.Contains(
                "#pragma shader_feature_local_fragment _ _HOLOGRAM_ON", shader);
            StringAssert.Contains("#pragma shader_feature_local_fragment _ " +
                "_HOLOGRAM_WORLD_SPACE _HOLOGRAM_SCREEN_SPACE", shader);
            StringAssert.DoesNotContain("multi_compile_local_fragment _ _HOLOGRAM", shader);
            StringAssert.Contains("float4 _HologramObjectUpVector;", include);
            StringAssert.Contains("return dot(positionOS, UberGetHologramUpVector());",
                include);
            StringAssert.Contains("return positionWS.y;", include);
            StringAssert.Contains("GetNormalizedScreenSpaceUV(positionCS).y", include);
            StringAssert.Contains("UberHologramValueNoise(noiseCoordinate)", include);
            StringAssert.Contains("max(fwidth(phase), 0.0001)", include);
            StringAssert.Contains("lengthSquared > 1.0e20", include);
            StringAssert.Contains("surfaceData.albedo *= saturate(_HologramColor.rgb);",
                include);
            StringAssert.Contains("surfaceData.alpha *= saturate(_HologramOpacity);",
                include);
            StringAssert.Contains("surfaceData.emission += _HologramColor.rgb", include);
            int forward = include.IndexOf("UberForwardOutput UberForwardFragment",
                StringComparison.Ordinal);
            int apply = include.IndexOf("UberApplyHologram(input.positionOS", forward,
                StringComparison.Ordinal);
            int alphaModulate = include.IndexOf(
                "surfaceData.albedo = AlphaModulate(surfaceData.albedo, " +
                "surfaceData.alpha);", apply, StringComparison.Ordinal);
            int lighting = include.IndexOf("#if defined(_UNLIT_ON)", alphaModulate,
                StringComparison.Ordinal);
            Assert.That(forward, Is.GreaterThanOrEqualTo(0));
            Assert.That(apply, Is.GreaterThan(forward));
            Assert.That(alphaModulate, Is.GreaterThan(apply));
            Assert.That(lighting, Is.GreaterThan(alphaModulate));
            StringAssert.Contains("surfaceData.albedo = albedo;", include);
            StringAssert.Contains("material.SetFloat(\"_Surface\", 1.0f);", gui);

            int objectVariantsStart = variants.IndexOf(
                "guid: d03bad68e5f94df47a2c30a8822ea41c", StringComparison.Ordinal);
            int spriteVariantsStart = variants.IndexOf(
                "guid: 795b3814d0dfe9242829795ff0608656", StringComparison.Ordinal);
            Assert.That(objectVariantsStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(spriteVariantsStart, Is.GreaterThan(objectVariantsStart));
            string objectVariants = variants.Substring(objectVariantsStart,
                spriteVariantsStart - objectVariantsStart);
            string[] serialized = Regex.Matches(objectVariants,
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

            Material material = new Material(Shader.Find(ObjectShaderName));
            try
            {
                Assert.That(material.GetFloat("_HologramEnabled"), Is.EqualTo(0f));
                Assert.That(material.GetFloat("_HologramSpace"), Is.EqualTo(0f));
                Assert.That(material.GetVector("_HologramObjectUpVector"),
                    Is.EqualTo(new Vector4(0f, 1f, 0f, 0f)));
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_Blend", 3f);
                material.SetFloat("_LightingMode", 1f);
                material.SetFloat("_CastShadows", 1f);
                material.SetFloat("_QueueControl", 1f);
                material.renderQueue = 2123;
                material.SetFloat("_HologramEnabled", 1f);
                UberShaderGUI inspector = new UberShaderGUI();
                for (int mode = 0; mode < 3; ++mode)
                {
                    material.EnableKeyword("_HOLOGRAM_WORLD_SPACE");
                    material.EnableKeyword("_HOLOGRAM_SCREEN_SPACE");
                    material.SetFloat("_HologramSpace", mode);
                    inspector.ValidateMaterial(material);
                    Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
                    Assert.That(material.GetFloat("_Blend"), Is.EqualTo(3f));
                    Assert.That(material.GetFloat("_LightingMode"), Is.EqualTo(1f));
                    Assert.That(material.GetFloat("_CastShadows"), Is.EqualTo(1f));
                    Assert.That(material.renderQueue, Is.EqualTo(2123));
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.True);
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_WORLD_SPACE"),
                        Is.EqualTo(mode == 1));
                    Assert.That(material.IsKeywordEnabled("_HOLOGRAM_SCREEN_SPACE"),
                        Is.EqualTo(mode == 2));
                }

                material.SetFloat("_HologramEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
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
        public void ObjectGlitchUsesSelectableViewObjectWorldSpaceAcrossRuntimePasses()
        {
            string shader = Read(UberDirectory + "Uber3D.shader");
            string include = Read(UberDirectory + "Uber3D.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");

            foreach (string row in new[]
                     {
                         "[Main(Glitch, _GLITCH_ON, on)] _GlitchEnabled(\"Glitch\", Float) = 0",
                         "[Title(Glitch, _)] [Sub(Glitch)] _GlitchStrength(\"Strength (Pixels)\", Range(0, 64)) = 8",
                         "[Sub(Glitch)] _GlitchRGBSplit(\"RGB Split (Pixels)\", Range(0, 16)) = 2",
                         "[Sub(Glitch)] _GlitchFrequency(\"Frequency\", Range(0, 1)) = 0.25",
                         "[Sub(Glitch)] _GlitchSpeed(\"Speed\", Range(0, 30)) = 8",
                         "[KWEnum(Glitch, View, _, Object, _GLITCH_OBJECT_SPACE, World, _GLITCH_WORLD_SPACE)] _GlitchSpace(\"Space\", Float) = 0",
                         "[UberVector3(Glitch)] _GlitchUpVector(\"Up Vector\", Vector) = (0, 1, 0, 0)",
                         "[UberMinMaxVector(Glitch)] _GlitchBandSizeRange(\"Band Size Range (Pixels)\", Vector) = (4, 12, 0, 0)",
                     })
            {
                StringAssert.Contains(row, shader);
            }

            CollectionAssert.AreEqual(Enumerable.Repeat(
                "#pragma shader_feature_local _ _GLITCH_ON", 5).ToArray(),
                PragmaRows(shader, "_GLITCH_ON"));
            const string spacePragma =
                "#pragma multi_compile_local _ _GLITCH_OBJECT_SPACE _GLITCH_WORLD_SPACE";
            CollectionAssert.AreEqual(Enumerable.Repeat(spacePragma, 5).ToArray(),
                PragmaRows(shader, "_GLITCH_OBJECT_SPACE"));
            CollectionAssert.AreEqual(Enumerable.Repeat(spacePragma, 5).ToArray(),
                PragmaRows(shader, "_GLITCH_WORLD_SPACE"));

            foreach (string field in new[]
                     {
                         "float4 _GlitchBandSizeRange;",
                         "float4 _GlitchUpVector;",
                         "half _GlitchEnabled;",
                         "half _GlitchSpace;",
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
                         "inline float3 UberGetGlitchUpVector()",
                         "inline float3 UberGetGlitchPlaneTangent(float3 upVector)",
                         "inline float2 UberGetGlitchPlanePixelDirection(float2 tangentPixelDelta,",
                         "float3 planeBitangent = cross(upVector, planeTangent);",
                         "inline void UberGetGlitchSpaceData(float3 positionOS,",
                         "#if defined(_GLITCH_OBJECT_SPACE)",
                         "#elif defined(_GLITCH_WORLD_SPACE)",
                         "bandPixelCoordinate = dot(positionOS, upVector) *",
                         "bandPixelCoordinate = dot(positionWS, upVector) *",
                         "bandPixelCoordinate = positionPixel.y;",
                         "shiftPixelDirection = UberGetGlitchPlanePixelDirection(",
                         "inline void UberEvaluateGlitchBand(float bandPixelCoordinate, float frame,",
                         "inline void UberApplyGlitchVertexPosition(float3 positionOS,",
                         "positionCS.xy += clipPixelDirection * shiftPixels *",
                         "inline float2 UberApplyGlitchUV(float2 rawUV, float3 positionOS,",
                         "rawPixelStep = ddx(rawUV) * shiftPixelDirection.x +",
                         "ddy(rawUV) * shiftPixelDirection.y;",
                         "float minBandSize = clamp(min(_GlitchBandSizeRange.x,",
                         "float maxBandSize = clamp(max(_GlitchBandSizeRange.x,",
                         "UberEvaluateGlitchBandBoundary(boundaryIndex, frame,",
                         "float activation = step(1.0 - saturate(_GlitchFrequency),",
                         "return rawUV + rawPixelStep * shiftPixels;",
                         "if (glitchActivation > 0.0h && _GlitchRGBSplit > 0.0001h)",
                         "float2 splitUV = rawPixelStep * splitPixels * splitDirection;",
                         "center.r = UberSampleBase(effectUV + splitUV, splitSurfaceUV).r;",
                         "center.b = UberSampleBase(effectUV - splitUV, splitSurfaceUV).b;",
                         "baseSample = UberApplyGlitchRGBSplit(effectUV, baseSample,",
                         "UberEvaluateSilhouette(effectUV, positionOS, baseColor.a, positionCS,",
                     })
            {
                StringAssert.Contains(contract, include);
            }

            Assert.That(Regex.Matches(include,
                @"\bUberApplyGlitchUV\s*\(").Count, Is.EqualTo(5));
            Assert.That(Regex.Matches(include,
                @"\bUberApplyGlitchVertexPosition\s*\(").Count, Is.EqualTo(6));
            StringAssert.DoesNotContain(
                "defined(_HOLOGRAM_ON) && defined(_GLITCH_ON)", include);
            StringAssert.Contains(
                "new KeywordBinding(\"_GlitchSpace\", \"_GLITCH_OBJECT_SPACE\", 1,",
                gui);
            StringAssert.Contains(
                "new KeywordBinding(\"_GlitchSpace\", \"_GLITCH_WORLD_SPACE\", 2,",
                gui);

            ShaderVariantCollection collection =
                AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(VariantPath);
            Assert.That(collection.Contains(new UberShaderVariantSpec(ObjectShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_ON").ToVariant()),
                Is.True);
            Assert.That(collection.Contains(new UberShaderVariantSpec(ObjectShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_ON",
                "_HOLOGRAM_ON").ToVariant()), Is.True);
            Assert.That(collection.Contains(new UberShaderVariantSpec(ObjectShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_OBJECT_SPACE",
                "_GLITCH_ON").ToVariant()), Is.True);
            Assert.That(collection.Contains(new UberShaderVariantSpec(ObjectShaderName,
                PassType.ScriptableRenderPipeline, "_GLITCH_ON",
                "_GLITCH_WORLD_SPACE").ToVariant()), Is.True);

            Material material = new Material(Shader.Find(ObjectShaderName));
            try
            {
                Assert.That(material.GetFloat("_GlitchEnabled"), Is.Zero);
                Assert.That(material.GetFloat("_GlitchSpace"), Is.Zero);
                AssertVector(material.GetVector("_GlitchUpVector"),
                    new Vector4(0f, 1f, 0f, 0f));
                Assert.That(material.GetFloat("_GlitchRGBSplit"), Is.EqualTo(2f));
                AssertVector(material.GetVector("_GlitchBandSizeRange"),
                    new Vector4(4f, 12f, 0f, 0f));

                UberShaderGUI inspector = new UberShaderGUI();
                material.SetFloat("_GlitchEnabled", 1f);
                for (int mode = 0; mode < 3; ++mode)
                {
                    material.EnableKeyword("_GLITCH_OBJECT_SPACE");
                    material.EnableKeyword("_GLITCH_WORLD_SPACE");
                    material.SetFloat("_GlitchSpace", mode);
                    inspector.ValidateMaterial(material);
                    Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.True);
                    Assert.That(material.IsKeywordEnabled("_GLITCH_OBJECT_SPACE"),
                        Is.EqualTo(mode == 1));
                    Assert.That(material.IsKeywordEnabled("_GLITCH_WORLD_SPACE"),
                        Is.EqualTo(mode == 2));
                }
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.False);

                material.SetFloat("_HologramEnabled", 1f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.True);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.True);

                material.SetFloat("_GlitchEnabled", 0f);
                inspector.ValidateMaterial(material);
                Assert.That(material.IsKeywordEnabled("_GLITCH_ON"), Is.False);
                Assert.That(material.IsKeywordEnabled("_GLITCH_OBJECT_SPACE"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_GLITCH_WORLD_SPACE"),
                    Is.False);
                Assert.That(material.IsKeywordEnabled("_HOLOGRAM_ON"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

    }
}
