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
        public void PostShaderImportsWithoutWarningsOrErrors()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                UberDirectory + "UberPostProcessing.shader");
            Assert.That(shader, Is.Not.Null);

            string[] messages = ShaderUtil.GetShaderMessages(shader)
                .Where(message =>
                    message.severity == ShaderCompilerMessageSeverity.Warning ||
                    message.severity == ShaderCompilerMessageSeverity.Error)
                .Select(message => message.severity + ": " + message.message)
                .ToArray();
            Assert.That(messages, Is.Empty, string.Join(" | ", messages));
        }

        [Test]
        public void PostFilterSelectorUsesOneLocalSetAndRepairsMaterialKeywords()
        {
            string postShader = Read(UberDirectory + "UberPostProcessing.shader");
            Assert.That(PostFilterKeywords, Has.Length.EqualTo(11));
            Assert.That(PostFilterLabels, Has.Length.EqualTo(11));

            FieldInfo optionTable = typeof(UberShaderGUI).GetField(
                "PostFilterOptions", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(optionTable, Is.Not.Null);
            Array options = optionTable.GetValue(null) as Array;
            Assert.That(options, Is.Not.Null);
            Assert.That(options.Length, Is.EqualTo(PostFilterKeywords.Length));
            Type optionType = options.GetType().GetElementType();
            Assert.That(optionType, Is.Not.Null);
            FieldInfo labelField = optionType.GetField("Label",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo keywordField = optionType.GetField("Keyword",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(labelField, Is.Not.Null);
            Assert.That(keywordField, Is.Not.Null);
            CollectionAssert.AreEqual(PostFilterLabels, options.Cast<object>()
                .Select(option => (string)labelField.GetValue(option)).ToArray());
            CollectionAssert.AreEqual(PostFilterKeywords, options.Cast<object>()
                .Select(option => (string)keywordField.GetValue(option)).ToArray());

            FieldInfo cachedLabels = typeof(UberPostFilterDrawer).GetField(
                "Labels", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(cachedLabels, Is.Not.Null);
            GUIContent[] popupLabels = cachedLabels.GetValue(null) as GUIContent[];
            Assert.That(popupLabels, Is.Not.Null);
            CollectionAssert.AreEqual(PostFilterLabels,
                popupLabels.Select(content => content.text).ToArray());

            string expectedPragma =
                "#pragma shader_feature_local_fragment _ " +
                string.Join(" ", PostFilterKeywords.Skip(1));
            string[] filterPragmas = Lines(postShader).Where(row =>
                    row.StartsWith("#pragma ", StringComparison.Ordinal) &&
                    PostFilterKeywords.Skip(1).Any(keyword =>
                        ContainsToken(row, keyword)))
                .ToArray();
            CollectionAssert.AreEqual(new[] { expectedPragma }, filterPragmas);
            Assert.That(filterPragmas.Any(row =>
                row.Contains("multi_compile_local")), Is.False);

            Material material = new Material(Shader.Find(PostShaderName));
            try
            {
                UberShaderGUI gui = new UberShaderGUI();
                for (int mode = 0; mode < PostFilterKeywords.Length; ++mode)
                {
                    foreach (string keyword in PostFilterKeywords.Skip(1))
                        material.EnableKeyword(keyword);
                    material.SetFloat("_ScreenFilterMode", mode);
                    gui.ValidateMaterial(material);

                    Assert.That(material.GetFloat("_ScreenFilterMode"),
                        Is.EqualTo(mode), "mode " + mode);
                    for (int index = 1; index < PostFilterKeywords.Length; ++index)
                    {
                        Assert.That(material.IsKeywordEnabled(
                                PostFilterKeywords[index]),
                            Is.EqualTo(index == mode),
                            "mode " + mode + ": " + PostFilterKeywords[index]);
                    }
                }

                material.SetFloat("_ScreenFilterMode", 6.6f);
                material.EnableKeyword("_PIXELATION_ON");
                material.EnableKeyword("_ASCII_FILTER_ON");
                gui.ValidateMaterial(material);
                Assert.That(material.GetFloat("_ScreenFilterMode"), Is.EqualTo(7f));
                Assert.That(material.IsKeywordEnabled("_OLD_FILM_ON"), Is.True);
                Assert.That(PostFilterKeywords.Skip(1).Count(
                    material.IsKeywordEnabled), Is.EqualTo(1));

                material.SetFloat("_ScreenFilterMode", -100f);
                gui.ValidateMaterial(material);
                Assert.That(material.GetFloat("_ScreenFilterMode"), Is.EqualTo(0f));
                Assert.That(PostFilterKeywords.Skip(1).Any(
                    material.IsKeywordEnabled), Is.False);

                material.SetFloat("_ScreenFilterMode", 100f);
                gui.ValidateMaterial(material);
                Assert.That(material.GetFloat("_ScreenFilterMode"), Is.EqualTo(10f));
                Assert.That(material.IsKeywordEnabled("_CRT_FILTER_ON"), Is.True);

                material.SetFloat("_ScreenFilterMode", float.NaN);
                gui.ValidateMaterial(material);
                Assert.That(material.GetFloat("_ScreenFilterMode"), Is.EqualTo(0f));
                Assert.That(PostFilterKeywords.Skip(1).Any(
                    material.IsKeywordEnabled), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PostFilterMixedSelectionUndoRedoAndVisibilityRemainSynchronized()
        {
            Shader shader = Shader.Find(PostShaderName);
            Assert.That(shader, Is.Not.Null);
            Material first = new Material(shader);
            Material second = new Material(shader);
            MaterialEditor editor = null;
            const string group = "ScreenFilter";
            bool hadGroup = LWGUI.GUIData.group.TryGetValue(group,
                out bool previousGroup);
            var previousKeywords = new Dictionary<string, bool>();
            foreach (string keyword in PostFilterKeywords.Skip(1))
            {
                if (LWGUI.GUIData.keyWord.TryGetValue(keyword, out bool value))
                    previousKeywords.Add(keyword, value);
            }

            try
            {
                first.SetFloat("_ScreenFilterMode", 1f);
                second.SetFloat("_ScreenFilterMode", 10f);
                foreach (Material material in new[] { first, second })
                foreach (string keyword in PostFilterKeywords.Skip(1))
                    material.EnableKeyword(keyword);

                MaterialProperty[] properties = MaterialEditor.GetMaterialProperties(
                    new UnityEngine.Object[] { first, second });
                MaterialProperty mode = properties.Single(property =>
                    property.name == "_ScreenFilterMode");
                Assert.That(mode.hasMixedValue, Is.True);

                MethodInfo seedKeywords = typeof(UberShaderGUI).GetMethod(
                    "SeedKeywords", BindingFlags.Static | BindingFlags.NonPublic,
                    null, new[] { typeof(MaterialProperty[]) }, null);
                MethodInfo setVisibility = typeof(UberShaderGUI).GetMethod(
                    "SetPostFilterVisibility",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(seedKeywords, Is.Not.Null);
                Assert.That(setVisibility, Is.Not.Null);
                LWGUI.GUIData.group[group] = false;
                seedKeywords.Invoke(null, new object[] { properties });
                foreach (string keyword in PostFilterKeywords.Skip(1))
                {
                    Assert.That(LWGUI.GUIData.keyWord[keyword], Is.False,
                        "mixed: " + keyword);
                    Assert.That(LWGUI.Helper.IsVisible(group + keyword), Is.False,
                        "mixed: " + keyword);
                }

                UberPostFilterDrawer drawer = new UberPostFilterDrawer(group);
                drawer.Apply(mode);
                AssertPostState(first, 1);
                AssertPostState(second, 10);

                for (int selectedMode = 0;
                     selectedMode < PostFilterKeywords.Length; ++selectedMode)
                {
                    setVisibility.Invoke(null, new object[] { selectedMode });
                    for (int index = 1; index < PostFilterKeywords.Length; ++index)
                    {
                        string keyword = PostFilterKeywords[index];
                        bool expected = index == selectedMode;
                        Assert.That(LWGUI.GUIData.keyWord[keyword],
                            Is.EqualTo(expected), selectedMode + ": " + keyword);
                        Assert.That(LWGUI.Helper.IsVisible(group + keyword),
                            Is.EqualTo(expected), selectedMode + ": " + keyword);
                    }
                }

                LWGUI.GUIData.group[group] = true;
                setVisibility.Invoke(null, new object[] { 6 });
                foreach (string keyword in PostFilterKeywords.Skip(1))
                    Assert.That(LWGUI.Helper.IsVisible(group + keyword), Is.False,
                        "closed: " + keyword);
                LWGUI.GUIData.group[group] = false;

                editor = Editor.CreateEditor(
                    new UnityEngine.Object[] { first, second }) as MaterialEditor;
                Assert.That(editor, Is.Not.Null);
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                editor.RegisterPropertyChangeUndo("Change Post Filter");
                mode.floatValue = 6f;
                drawer.Apply(mode);
                Undo.CollapseUndoOperations(undoGroup);
                AssertPostState(first, 6);
                AssertPostState(second, 6);

                Undo.PerformUndo();
                AssertPostState(first, 1);
                AssertPostState(second, 10);
                Undo.PerformRedo();
                AssertPostState(first, 6);
                AssertPostState(second, 6);
            }
            finally
            {
                if (hadGroup)
                    LWGUI.GUIData.group[group] = previousGroup;
                else
                    LWGUI.GUIData.group.Remove(group);
                foreach (string keyword in PostFilterKeywords.Skip(1))
                {
                    if (previousKeywords.TryGetValue(keyword, out bool value))
                        LWGUI.GUIData.keyWord[keyword] = value;
                    else
                        LWGUI.GUIData.keyWord.Remove(keyword);
                }
                if (editor != null)
                    UnityEngine.Object.DestroyImmediate(editor);
                Undo.ClearUndo(first);
                Undo.ClearUndo(second);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }

            void AssertPostState(Material material, int expectedMode)
            {
                Assert.That(material.GetFloat("_ScreenFilterMode"),
                    Is.EqualTo((float)expectedMode));
                for (int index = 1; index < PostFilterKeywords.Length; ++index)
                {
                    Assert.That(material.IsKeywordEnabled(PostFilterKeywords[index]),
                        Is.EqualTo(index == expectedMode),
                        expectedMode + ": " + PostFilterKeywords[index]);
                }
            }
        }

        [Test]
        public void CrtFilterUsesReviewedPropertiesAndProceduralSourceContract()
        {
            string shader = Read(UberDirectory + "UberPostProcessing.shader");
            string hlsl = Read(UberDirectory + "UberPostProcessing.hlsl");
            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            string[] properties =
            {
                "_CRTStrength(\"Overall Strength\", Range(0, 1)) = 1",
                "_CRTCurvature(\"Curvature\", Range(0, 0.5)) = 0.12",
                "_CRTScanlineDensity(\"Scanline Density\", Range(64, 1440)) = 480",
                "_CRTScanlineStrength(\"Scanline Strength\", Range(0, 1)) = 0.25",
                "_CRTMaskScale(\"Phosphor Mask Scale\", Range(1, 8)) = 1",
                "_CRTMaskStrength(\"Phosphor Mask Strength\", Range(0, 1)) = 0.2",
                "_CRTChromaticAberration(\"Chromatic Aberration\", Range(0, 8)) = 1",
                "_CRTVignetteStrength(\"Vignette Strength\", Range(0, 1)) = 0.35",
                "_CRTSignalNoise(\"Signal Noise\", Range(0, 0.25)) = 0.03",
                "_CRTHorizontalJitter(\"Horizontal Jitter\", Range(0, 4)) = 0.5",
                "_CRTRollingBand(\"Rolling Band\", Range(0, 1)) = 0.08",
                "_CRTAnimationSpeed(\"Animation Speed\", Range(-5, 5)) = 1",
                "_CRTPowerOffAmount(\"Power Off Amount\", Range(0, 1)) = 0",
                "_CRTPowerBloomIntensity(\"Power Bloom Intensity\", Range(0, 8)) = 2",
            };
            foreach (string property in properties)
                StringAssert.Contains(property, shader);
            Assert.That(Lines(shader).Count(line =>
                    line.Contains("ScreenFilter_CRT_FILTER_ON")),
                Is.EqualTo(properties.Length));

            Match cbuffer = Regex.Match(hlsl,
                @"(?s)CBUFFER_START\(UnityPerMaterial\)(?<body>.*?)CBUFFER_END");
            Assert.That(cbuffer.Success, Is.True);
            foreach (string property in properties.Select(row =>
                         Regex.Match(row, @"^_[A-Za-z0-9]+").Value))
            {
                Assert.That(Regex.Matches(cbuffer.Groups["body"].Value,
                        @"(?<![A-Za-z0-9_])" + Regex.Escape(property) +
                        @"(?![A-Za-z0-9_])").Count,
                    Is.EqualTo(1), property);
            }

            Match crt = Regex.Match(hlsl,
                @"(?s)inline float3 UberPostCRT\(.*?\n\}\s*\n\s*float4 UberPostFragment");
            Assert.That(crt.Success, Is.True);
            foreach (string contract in new[]
                     {
                         "if (strength <= 0.0)",
                         "float2 axisSquared = centered * centered;",
                         "centered *= 1.0 + curvature * axisSquared.yx;",
                         "float2 curvedUV = centered * 0.5 + 0.5;",
                         "float2 warpedUV = curvedUV + float2(jitter, 0.0);",
                         "max(max(-curvedUV, curvedUV - 1.0), 0.0)",
                         "saturate(curvedUV * (1.0 - curvedUV) * 4.0)",
                         "chromaticDirection * texelSize * chromatic",
                         "fwidth(scanCoordinate)",
                         "smoothstep(0.25 - scanAA, 0.25 + scanAA",
                         "float3 phosphorMask", "float2 boundary = 1.0 - smoothstep",
                         "UberPostHash21", "float rawTime = _Time.y * speed;",
                         "float time = fmod(finiteTime, 3600.0);",
                         "float powerCollapseY = saturate(powerOffAmount * 2.0);",
                         "float powerCollapseX = saturate((powerOffAmount - 0.5) * 2.0);",
                         "float2 powerUV = (uv - 0.5) / powerScale + 0.5;",
                         "float powerBloom = powerTransition * powerBloomIntensity;",
                         "powerMask * powerVisibility",
                         "return lerp(sourceColor, poweredColor, strength);",
                     })
                StringAssert.Contains(contract, crt.Value);
            Assert.That(crt.Value, Does.Not.Contain("dot(centered, centered)"),
                "CRT curvature must keep the four axis endpoints on-screen.");
            Assert.That(crt.Value, Does.Not.Contain("centered.x *= aspect"),
                "CRT curvature must remain symmetric across normalized screen axes.");
            Assert.That(crt.Value, Does.Not.Contain("max(max(-warpedUV"),
                "CRT boundary must remain independent of horizontal jitter.");
            Assert.That(crt.Value, Does.Not.Contain("saturate(warpedUV * (1.0 - warpedUV) * 4.0)"),
                "CRT vignette envelope must remain independent of horizontal jitter.");
            Assert.That(Regex.Matches(hlsl,
                @"#elif defined\(_CRT_FILTER_ON\)").Count, Is.EqualTo(1));
            StringAssert.Contains(
                "source.rgb = UberPostCRT(input.texcoord.xy, input.positionCS.xy, source.rgb);",
                hlsl);

            Match drawer = Regex.Match(gui,
                @"(?s)public sealed class UberPostFilterDrawer.*?" +
                @"public sealed class UberAsciiFontDrawer");
            Assert.That(drawer.Success, Is.True);
            StringAssert.Contains("UberShaderGUI.CreatePostFilterLabels()",
                drawer.Value);
            Assert.That(Regex.Matches(drawer.Value, @"new GUIContent\(").Count,
                Is.Zero);
            CollectionAssert.AreEqual(new[] { "_CRT_FILTER_ON" },
                Regex.Matches(shader + hlsl + gui,
                        @"_CRT[A-Z0-9_]*_ON").Cast<Match>()
                    .Select(match => match.Value).Distinct().ToArray());
        }

        [Test]
        public void AsciiFontAuthoringBakesStaticCommonAtlasAndFallsBackSafely()
        {
            const string ramp = AsciiRamp;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                AsciiFontPath);
            Assert.That(font, Is.Not.Null);
            Assert.That(AssetDatabase.AssetPathToGUID(AsciiFontPath),
                Is.EqualTo(AsciiFontGuid));
            Assert.That(font.atlasPopulationMode,
                Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(font.atlasTextures, Has.Length.EqualTo(1));
            bool fontWasDirty = EditorUtility.IsDirty(font);
            TMP_Character[] originalCharacters = ramp.Select(character =>
                font.characterLookupTable[character]).ToArray();
            var originalGlyphs = originalCharacters.Select(character =>
                character.glyph).ToArray();
            var originalRects = originalGlyphs.Select(glyph =>
                glyph.glyphRect).ToArray();
            var originalMetrics = originalGlyphs.Select(glyph =>
                glyph.metrics).ToArray();
            int[] originalAtlasIndices = originalGlyphs.Select(glyph =>
                glyph.atlasIndex).ToArray();
            Hash128 fontHash = AssetDatabase.GetAssetDependencyHash(AsciiFontPath);
            Material first = new Material(Shader.Find(PostShaderName));
            Material second = new Material(Shader.Find(PostShaderName));
            TMP_FontAsset dynamicFont = null;
            TMP_FontAsset missingFont = null;
            TMP_FontAsset crossAtlasFont = null;
            Texture2D unresolvedAtlas = null;
            Texture2D crossAtlasB = null;
            try
            {
                foreach (Material material in new[] { first, second })
                {
                    material.SetTexture("_AsciiFontAtlas", font.atlasTextures[0]);
                    new UberShaderGUI().ValidateMaterial(material);
                }

                float emHeight = font.faceInfo.ascentLine -
                    font.faceInfo.descentLine;
                HashSet<Vector4> uvs = new HashSet<Vector4>();
                foreach (Material material in new[] { first, second })
                {
                    Assert.That(material.GetFloat("_AsciiFontReady"),
                        Is.EqualTo(1f));
                    Assert.That(material.GetTexture("_AsciiFontAtlas"),
                        Is.SameAs(font.atlasTextures[0]));
                    for (int index = 0; index < ramp.Length; ++index)
                    {
                        var glyph = font.characterLookupTable[ramp[index]].glyph;
                        var rect = glyph.glyphRect;
                        var metrics = glyph.metrics;
                        float scale = glyph.scale;
                        Vector4 expectedUv = new Vector4(
                            (float)rect.x / font.atlasTextures[0].width,
                            (float)rect.y / font.atlasTextures[0].height,
                            (float)rect.width / font.atlasTextures[0].width,
                            (float)rect.height / font.atlasTextures[0].height);
                        Vector4 expectedPlacement = new Vector4(
                            0.5f - metrics.horizontalAdvance * scale /
                                (2f * emHeight) +
                                metrics.horizontalBearingX * scale / emHeight,
                            (metrics.horizontalBearingY * scale -
                                metrics.height * scale - font.faceInfo.descentLine) /
                                emHeight,
                            metrics.width * scale / emHeight,
                            metrics.height * scale / emHeight);
                        Vector4 uv = material.GetVector("_AsciiGlyphUV" + index);
                        Vector4 placement = material.GetVector(
                            "_AsciiGlyphPlacement" + index);
                        AssertVector(uv, expectedUv);
                        AssertVector(placement, expectedPlacement);
                        if (material == first)
                            Assert.That(uvs.Add(uv), Is.True, "UV " + index);
                    }
                }

                MethodInfo synchronize = typeof(UberShaderGUI).GetMethod(
                    "SynchronizeAsciiFont",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(synchronize, Is.Not.Null);
                void AssertRejectedFont(TMP_FontAsset rejectedFont,
                    string expectedError)
                {
                    object[] arguments = { first, rejectedFont, true, null, true };
                    string status = (string)synchronize.Invoke(null, arguments);
                    StringAssert.Contains(expectedError, status);
                    Assert.That(first.GetTexture("_AsciiFontAtlas"),
                        Is.SameAs(rejectedFont.atlasTextures[0]));
                    Assert.That(first.GetFloat("_AsciiFontReady"), Is.EqualTo(0f));
                    for (int index = 0; index < ramp.Length; ++index)
                    {
                        Assert.That(first.GetVector("_AsciiGlyphUV" + index),
                            Is.EqualTo(Vector4.zero));
                        Assert.That(first.GetVector("_AsciiGlyphPlacement" + index),
                            Is.EqualTo(Vector4.zero));
                    }

                    EditorUtility.ClearDirty(first);
                    arguments[4] = false;
                    status = (string)synchronize.Invoke(null, arguments);
                    StringAssert.Contains(expectedError, status);
                    Assert.That(first.GetTexture("_AsciiFontAtlas"),
                        Is.SameAs(rejectedFont.atlasTextures[0]));
                    Assert.That(EditorUtility.IsDirty(first), Is.False);
                }

                dynamicFont = UnityEngine.Object.Instantiate(font);
                Assert.That(dynamicFont.atlasTextures,
                    Is.Not.SameAs(font.atlasTextures));
                dynamicFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                AssertRejectedFont(dynamicFont, "Static population");

                missingFont = UnityEngine.Object.Instantiate(font);
                TMP_Character missingCharacter =
                    missingFont.characterLookupTable[ramp[0]];
                Assert.That(missingFont.atlasTextures,
                    Is.Not.SameAs(font.atlasTextures));
                Assert.That(missingCharacter,
                    Is.Not.SameAs(originalCharacters[0]));
                Assert.That(missingCharacter.glyph,
                    Is.Not.SameAs(originalGlyphs[0]));
                missingFont.characterTable.RemoveAll(character =>
                    character.unicode == ramp[0]);
                missingFont.ReadFontAssetDefinition();
                AssertRejectedFont(missingFont, "Missing ramp character");

                crossAtlasFont = UnityEngine.Object.Instantiate(font);
                var splitGlyph = crossAtlasFont.characterLookupTable[ramp[1]].glyph;
                Assert.That(crossAtlasFont.atlasTextures,
                    Is.Not.SameAs(font.atlasTextures));
                Assert.That(splitGlyph, Is.Not.SameAs(originalGlyphs[1]));
                crossAtlasB = new Texture2D(1024, 1024);
                crossAtlasFont.atlasTextures = new[]
                    { font.atlasTextures[0], crossAtlasB };
                splitGlyph.atlasIndex = 1;
                AssertRejectedFont(crossAtlasFont, "share one atlas");

                object[] clearedArguments = { first, null, true, null, true };
                string clearedStatus = (string)synchronize.Invoke(null,
                    clearedArguments);
                StringAssert.Contains("No Font Asset selected", clearedStatus);
                AssertAsciiFontState(first, null, false);
                EditorUtility.ClearDirty(first);
                clearedArguments[2] = false;
                clearedArguments[4] = false;
                clearedStatus = (string)synchronize.Invoke(null,
                    clearedArguments);
                StringAssert.Contains("No Font Asset selected", clearedStatus);
                Assert.That(EditorUtility.IsDirty(first), Is.False);

                unresolvedAtlas = new Texture2D(2, 2);
                first.SetTexture("_AsciiFontAtlas", unresolvedAtlas);
                new UberShaderGUI().ValidateMaterial(first);
                Assert.That(first.GetFloat("_AsciiFontReady"), Is.EqualTo(0f));
                for (int index = 0; index < ramp.Length; ++index)
                {
                    Assert.That(first.GetVector("_AsciiGlyphUV" + index),
                        Is.EqualTo(Vector4.zero));
                    Assert.That(first.GetVector("_AsciiGlyphPlacement" + index),
                        Is.EqualTo(Vector4.zero));
                }
            }
            finally
            {
                first.SetTexture("_AsciiFontAtlas", null);
                if (crossAtlasFont != null)
                {
                    crossAtlasFont.atlasTextures = null;
                    crossAtlasFont.material = null;
                    UnityEngine.Object.DestroyImmediate(crossAtlasFont);
                }
                if (missingFont != null)
                {
                    missingFont.atlasTextures = null;
                    missingFont.material = null;
                    UnityEngine.Object.DestroyImmediate(missingFont);
                }
                if (crossAtlasB != null)
                    UnityEngine.Object.DestroyImmediate(crossAtlasB);
                if (unresolvedAtlas != null)
                    UnityEngine.Object.DestroyImmediate(unresolvedAtlas);
                if (dynamicFont != null)
                {
                    dynamicFont.atlasTextures = null;
                    dynamicFont.material = null;
                    UnityEngine.Object.DestroyImmediate(dynamicFont);
                }
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }

            Assert.That(EditorUtility.IsDirty(font), Is.EqualTo(fontWasDirty));
            Assert.That(AssetDatabase.GetAssetDependencyHash(AsciiFontPath),
                Is.EqualTo(fontHash));
            for (int index = 0; index < ramp.Length; ++index)
            {
                TMP_Character character = font.characterLookupTable[ramp[index]];
                Assert.That(character, Is.SameAs(originalCharacters[index]));
                Assert.That(character.glyph, Is.SameAs(originalGlyphs[index]));
                Assert.That(character.glyph.glyphRect,
                    Is.EqualTo(originalRects[index]));
                Assert.That(character.glyph.atlasIndex,
                    Is.EqualTo(originalAtlasIndices[index]));
                Assert.That(character.glyph.metrics,
                    Is.EqualTo(originalMetrics[index]));
            }

            string gui = Read(EditorDirectory + "UberShaderGUI.cs");
            foreach (string contract in new[]
                     {
                         "Font Asset must use Static population",
                         "Missing ramp character",
                         "Ramp characters must share one atlas",
                         "Atlas ownership is ambiguous",
                     })
                StringAssert.Contains(contract, gui);
            string shader = Read(UberDirectory + "UberPostProcessing.shader");
            string hlsl = Read(UberDirectory + "UberPostProcessing.hlsl");
            StringAssert.Contains(
                "[UberAsciiFont(ScreenFilter_ASCII_FILTER_ON)]", shader);
            StringAssert.Contains("_AsciiSdfThreshold", shader);
            StringAssert.Contains("_AsciiSdfSoftness", shader);
            StringAssert.Contains("if (glyphIndex < 0.5)", hlsl);
            StringAssert.Contains("if (_AsciiFontReady < 0.5)", hlsl);
            StringAssert.Contains(
                "SAMPLE_TEXTURE2D(_AsciiFontAtlas, sampler_AsciiFontAtlas", hlsl);
            int[] packedMasks = Regex.Matches(hlsl,
                    @"glyphIndex < [0-9]+\.5\) code = (?<code>[0-9]+)\.0;")
                .Cast<Match>().Select(match =>
                    int.Parse(match.Groups["code"].Value)).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                0, 2, 66, 8224, 8225, 131618, 263235, 492548,
                1016864, 934982, 432534, 560798, 923271, 189922,
                390645, 458078,
            }, packedMasks);
            CollectionAssert.AreEqual(new[]
                { 0, 1, 2, 2, 3, 4, 5, 6, 7, 8, 10, 10, 10, 10, 14, 14 },
                packedMasks.Select(code => Convert.ToString(code, 2)
                    .Count(bit => bit == '1')).ToArray());
            Assert.That(Regex.Matches(shader, @"_AsciiGlyphUV[0-9]+\(").Count,
                Is.EqualTo(AsciiRamp.Length));
            Assert.That(Regex.Matches(shader,
                @"_AsciiGlyphPlacement[0-9]+\(").Count,
                Is.EqualTo(AsciiRamp.Length));
            foreach (string asmdef in new[]
                     {
                         EditorDirectory + "UberShader.Editor.asmdef",
                         "Assets/Tests/EditMode/Rendering/Border.Rendering.EditModeTests.asmdef",
                     })
                Assert.That(Regex.Matches(Read(asmdef),
                    "\\\"Unity.TextMeshPro\\\"").Count, Is.EqualTo(1), asmdef);
        }

        [Test]
        public void AsciiFontAssignmentIsOneUndoAndNoOpValidationStaysClean()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                AsciiFontPath);
            Material first = new Material(Shader.Find(PostShaderName));
            Material second = new Material(Shader.Find(PostShaderName));
            MethodInfo assign = typeof(UberShaderGUI).GetMethod(
                "AssignAsciiFont", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(assign, Is.Not.Null);
            try
            {
                UberShaderGUI gui = new UberShaderGUI();
                foreach (Material material in new[] { first, second })
                {
                    material.SetTexture("_AsciiFontAtlas", null);
                    gui.ValidateMaterial(material);
                    AssertAsciiFontState(material, null, false);
                }

                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                string status = (string)assign.Invoke(null, new object[]
                {
                    new UnityEngine.Object[] { first, second }, font,
                });
                Undo.CollapseUndoOperations(undoGroup);
                StringAssert.StartsWith("Ready", status);
                foreach (Material material in new[] { first, second })
                    AssertAsciiFontState(material, font.atlasTextures[0], true);

                EditorUtility.ClearDirty(first);
                EditorUtility.ClearDirty(second);
                gui.ValidateMaterial(first);
                gui.ValidateMaterial(second);
                Assert.That(EditorUtility.IsDirty(first), Is.False);
                Assert.That(EditorUtility.IsDirty(second), Is.False);

                Undo.PerformUndo();
                foreach (Material material in new[] { first, second })
                    AssertAsciiFontState(material, null, false);
                Undo.PerformRedo();
                foreach (Material material in new[] { first, second })
                    AssertAsciiFontState(material, font.atlasTextures[0], true);

                first.SetVector("_AsciiGlyphUV0", Vector4.zero);
                EditorUtility.ClearDirty(first);
                gui.ValidateMaterial(first);
                Assert.That(first.GetVector("_AsciiGlyphUV0"),
                    Is.Not.EqualTo(Vector4.zero));
                Assert.That(EditorUtility.IsDirty(first), Is.True);
            }
            finally
            {
                Undo.ClearUndo(first);
                Undo.ClearUndo(second);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }

            string guiSource = Read(EditorDirectory + "UberShaderGUI.cs");
            Match drawer = Regex.Match(guiSource,
                @"(?s)public sealed class UberAsciiFontDrawer.*?public sealed class UberShaderGUI");
            Assert.That(drawer.Success, Is.True);
            Assert.That(Regex.Matches(drawer.Value, "SynchronizeAsciiFont").Count,
                Is.EqualTo(1));
            Assert.That(Regex.IsMatch(drawer.Value,
                @"SynchronizeAsciiFont\(\s*material, null, false, out font, false\)"),
                Is.True);
            StringAssert.DoesNotContain("ResolveAsciiFont", drawer.Value);
            Match onGui = Regex.Match(guiSource,
                @"(?s)public override void OnGUI\(MaterialEditor.*?public override void ValidateMaterial");
            Assert.That(onGui.Success, Is.True);
            Assert.That(onGui.Value.IndexOf("ValidateMaterial(selectedMaterial, false)",
                    StringComparison.Ordinal),
                Is.GreaterThan(onGui.Value.IndexOf("base.OnGUI", StringComparison.Ordinal)));
            StringAssert.Contains("ValidateMaterial(material, true)", guiSource);
            StringAssert.Contains("Undo.RecordObjects(targets", guiSource);
        }

        [Test]
        public void CurrentPostMaterialUsesExactStaticPixelifyDataAndTuning()
        {
            const string ramp = AsciiRamp;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                PostMaterialPath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                PixelifyFontPath);
            Assert.That(material, Is.Not.Null);
            Assert.That(font, Is.Not.Null);
            font.ReadFontAssetDefinition();
            Assert.That(AssetDatabase.AssetPathToGUID(PostMaterialPath),
                Is.EqualTo(PostMaterialGuid));
            Assert.That(AssetDatabase.AssetPathToGUID(PixelifyFontPath),
                Is.EqualTo(PixelifyFontGuid));
            Assert.That(AssetDatabase.AssetPathToGUID(PixelifySourcePath),
                Is.EqualTo(PixelifySourceGuid));
            Assert.That(font.creationSettings.sourceFontFileGUID,
                Is.EqualTo(PixelifySourceGuid));
            Assert.That(font.creationSettings.pointSize, Is.EqualTo(90));
            Assert.That(font.creationSettings.padding, Is.EqualTo(9));
            Assert.That(font.creationSettings.atlasWidth, Is.EqualTo(1024));
            Assert.That(font.creationSettings.atlasHeight, Is.EqualTo(1024));
            Assert.That(font.creationSettings.renderMode, Is.EqualTo(4165));
            Assert.That(font.atlasPopulationMode,
                Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(font.atlasTextures, Has.Length.EqualTo(1));
            Assert.That(font.atlasTextures[0].width, Is.EqualTo(1024));
            Assert.That(font.atlasTextures[0].height, Is.EqualTo(1024));
            Assert.That(font.characterTable, Has.Count.EqualTo(AsciiRamp.Length));
            Assert.That(font.glyphTable, Has.Count.EqualTo(AsciiRamp.Length));
            CollectionAssert.AreEquivalent(ramp.Select(character =>
                (uint)character), font.characterTable.Select(character =>
                character.unicode));
            Assert.That(font.glyphTable.All(glyph => glyph.atlasIndex == 0),
                Is.True);
            Assert.That(font.glyphTable.Select(glyph => glyph.index).Distinct()
                .Count(), Is.EqualTo(AsciiRamp.Length));
            PropertyInfo sourceReference = typeof(TMP_FontAsset).GetProperty(
                "SourceFont_EditorRef", BindingFlags.Instance |
                BindingFlags.NonPublic);
            Assert.That(sourceReference, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(
                    sourceReference.GetValue(font) as Font),
                Is.EqualTo(PixelifySourcePath));
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                font.atlasTextures[0], out string atlasGuid, out long atlasId),
                Is.True);
            Assert.That(atlasGuid, Is.EqualTo(PixelifyFontGuid));
            Assert.That(atlasId, Is.EqualTo(-3501735882904893123L));

            // Filter choice and tuning are artist-owned deployment state. Keep a
            // read-only snapshot so font inspection proves it does not rewrite
            // those values without pinning the material to one authored profile.
            string[] artistTuningProperties =
            {
                "_ScreenFilterMode", "_AsciiCellSize", "_AsciiSourceColor",
                "_AsciiInvert", "_EdgeThreshold", "_EdgeWidth",
                "_DitherStrength", "_AsciiSdfThreshold", "_AsciiSdfSoftness",
            };
            Dictionary<string, float> artistTuning = artistTuningProperties
                .ToDictionary(name => name, material.GetFloat);
            string[] artistKeywords = material.shaderKeywords
                .OrderBy(keyword => keyword, StringComparer.Ordinal).ToArray();
            Assert.That(material.GetFloat("_AsciiFontReady"), Is.EqualTo(1f));
            Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                material.GetTexture("_AsciiFontAtlas"), out string boundGuid,
                out long boundId), Is.True);
            Assert.That(boundGuid, Is.EqualTo(atlasGuid));
            Assert.That(boundId, Is.EqualTo(atlasId));

            HashSet<Vector4> uvs = new HashSet<Vector4>();
            float emHeight = font.faceInfo.ascentLine - font.faceInfo.descentLine;
            for (int index = 0; index < AsciiRamp.Length; ++index)
            {
                Vector4 uv = material.GetVector("_AsciiGlyphUV" + index);
                Vector4 placement = material.GetVector(
                    "_AsciiGlyphPlacement" + index);
                var glyph = font.characterLookupTable[ramp[index]].glyph;
                var rect = glyph.glyphRect;
                var metrics = glyph.metrics;
                float scale = glyph.scale;
                AssertVector(uv, new Vector4(
                    (float)rect.x / font.atlasTextures[0].width,
                    (float)rect.y / font.atlasTextures[0].height,
                    (float)rect.width / font.atlasTextures[0].width,
                    (float)rect.height / font.atlasTextures[0].height));
                AssertVector(placement, new Vector4(
                    0.5f - metrics.horizontalAdvance * scale /
                        (2f * emHeight) +
                        metrics.horizontalBearingX * scale / emHeight,
                    (metrics.horizontalBearingY * scale -
                        metrics.height * scale - font.faceInfo.descentLine) /
                        emHeight,
                    metrics.width * scale / emHeight,
                    metrics.height * scale / emHeight));
                Assert.That(uv, Is.Not.EqualTo(Vector4.zero));
                Assert.That(placement, Is.Not.EqualTo(Vector4.zero));
                Assert.That(uvs.Add(uv), Is.True, "UV " + index);
            }

            MethodInfo synchronize = typeof(UberShaderGUI).GetMethod(
                "SynchronizeAsciiFont", BindingFlags.Static |
                BindingFlags.NonPublic);
            Assert.That(synchronize, Is.Not.Null);
            object[] arguments = { material, null, false, null, false };
            string status = (string)synchronize.Invoke(null, arguments);
            StringAssert.StartsWith("Ready · " + AsciiRamp + " · PixelifySans",
                status);
            Assert.That(arguments[3], Is.SameAs(font));
            foreach (KeyValuePair<string, float> tuning in artistTuning)
                Assert.That(material.GetFloat(tuning.Key), Is.EqualTo(tuning.Value),
                    tuning.Key);
            CollectionAssert.AreEqual(artistKeywords, material.shaderKeywords
                .OrderBy(keyword => keyword, StringComparer.Ordinal).ToArray());
        }

        [Test]
        public void AsciiSpacingChangesGridPitchWithoutScalingGlyphs()
        {
            string shader = Read(UberDirectory + "UberPostProcessing.shader");
            string hlsl = Read(UberDirectory + "UberPostProcessing.hlsl");
            StringAssert.Contains(
                "[Sub(ScreenFilter_ASCII_FILTER_ON)] " +
                "_AsciiCharacterSpacing(\"Horizontal Spacing\", " +
                "Range(-0.75, 0.75)) = -0.35", shader);
            StringAssert.Contains(
                "[Sub(ScreenFilter_ASCII_FILTER_ON)] " +
                "_AsciiLineSpacing(\"Vertical Spacing\", " +
                "Range(-0.75, 0.75)) = 0", shader);
            Match cbuffer = Regex.Match(hlsl,
                @"(?s)CBUFFER_START\(UnityPerMaterial\)(?<body>.*?)CBUFFER_END");
            Assert.That(cbuffer.Success, Is.True);
            StringAssert.Contains("float _AsciiCharacterSpacing;",
                cbuffer.Groups["body"].Value);
            StringAssert.Contains("float _AsciiLineSpacing;",
                cbuffer.Groups["body"].Value);

            int glyphStart = hlsl.IndexOf("inline float UberPostAsciiGlyph",
                StringComparison.Ordinal);
            int gridStart = hlsl.IndexOf("inline float3 UberPostAscii",
                glyphStart, StringComparison.Ordinal);
            Assert.That(glyphStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(gridStart, Is.GreaterThan(glyphStart));
            string glyphFunction = hlsl.Substring(
                glyphStart, gridStart - glyphStart);
            StringAssert.Contains(
                "float2 glyphPosition = (cellUV - placement.xy) / placementSize;",
                glyphFunction);
            StringAssert.DoesNotContain("_AsciiCharacterSpacing", glyphFunction);
            StringAssert.DoesNotContain("_AsciiLineSpacing", glyphFunction);

            StringAssert.Contains(
                "float horizontalSpacing = " +
                "_AsciiCharacterSpacing == _AsciiCharacterSpacing",
                hlsl);
            StringAssert.Contains(
                "float verticalSpacing = _AsciiLineSpacing == _AsciiLineSpacing",
                hlsl);
            StringAssert.Contains(
                "float2 cellIndex = floor(pixelPosition / cellPitch);", hlsl);
            StringAssert.Contains(
                "float2 glyphOrigin = cellCenterPixels - glyphCellSize * 0.5;",
                hlsl);
            StringAssert.Contains(
                "float2 cellUV = (pixelPosition - glyphOrigin) / glyphCellSize;",
                hlsl);
            StringAssert.Contains(
                "float glyph = cellBounds * saturate(UberPostAsciiGlyph(", hlsl);
            StringAssert.Contains(
                "saturate(cellUV), glyphIndex, softness));", hlsl);

            const int width = 96;
            const int height = 64;
            Texture2D source = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                source = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);
                Color[] sourcePixels = new Color[width * height];
                for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    float luminance = x < width / 3 ? 0f : 0.95f;
                    float alpha = 0.2f + 0.6f * y / (height - 1f);
                    sourcePixels[y * width + x] = new Color(
                        luminance, luminance, luminance, alpha);
                }
                source.SetPixels(sourcePixels);
                source.Apply(false, false);
                target = new RenderTexture(width, height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);
                readback = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);
                material = new Material(Shader.Find(PostShaderName));
                Assert.That(material.GetFloat("_AsciiCharacterSpacing"),
                    Is.EqualTo(-0.35f).Within(0.0001f));
                Assert.That(material.GetFloat("_AsciiLineSpacing"),
                    Is.EqualTo(0f).Within(0.0001f));
                ConfigurePostFilter(material, 9, 0);
                material.SetFloat("_AsciiCellSize", 8f);
                material.SetFloat("_AsciiSourceColor", 0f);
                material.SetFloat("_AsciiInvert", 0f);
                material.SetFloat("_AsciiSdfThreshold", 0.54f);
                material.SetFloat("_AsciiSdfSoftness", 0.001f);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    PixelifyFontPath);
                Assert.That(font, Is.Not.Null);
                material.SetTexture("_AsciiFontAtlas", font.atlasTextures[0]);
                new UberShaderGUI().ValidateMaterial(material);
                Assert.That(material.GetFloat("_AsciiFontReady"), Is.EqualTo(1f));

                material.SetFloat("_AsciiCharacterSpacing", -0.25f);
                material.SetFloat("_AsciiLineSpacing", 0f);
                Color[] compactHorizontal = RenderPostFilter(
                    source, material, target, readback);
                material.SetFloat("_AsciiCharacterSpacing", 0.5f);
                Color[] wideHorizontal = RenderPostFilter(
                    source, material, target, readback);
                material.SetFloat("_AsciiCharacterSpacing", 0f);
                material.SetFloat("_AsciiLineSpacing", -0.25f);
                Color[] compactVertical = RenderPostFilter(
                    source, material, target, readback);
                material.SetFloat("_AsciiLineSpacing", 0.5f);
                Color[] wideVertical = RenderPostFilter(
                    source, material, target, readback);

                int compactHorizontalCoverage = 0;
                int wideHorizontalCoverage = 0;
                int compactVerticalCoverage = 0;
                int wideVerticalCoverage = 0;
                Color[][] outputs =
                {
                    compactHorizontal, wideHorizontal,
                    compactVertical, wideVertical,
                };
                for (int index = 0; index < sourcePixels.Length; ++index)
                {
                    int x = index % width;
                    foreach (Color[] output in outputs)
                    {
                        Assert.That(IsFinite(output[index]), Is.True,
                            "spacing pixel " + index);
                        Assert.That(output[index].a,
                            Is.EqualTo(sourcePixels[index].a).Within(0.0001f));
                        if (x < 8)
                            Assert.That(Mathf.Max(output[index].r,
                                output[index].g, output[index].b),
                                Is.LessThan(0.002f));
                    }
                    if (x >= width / 3 + 8)
                    {
                        if (compactHorizontal[index].r > 0.05f)
                            ++compactHorizontalCoverage;
                        if (wideHorizontal[index].r > 0.05f)
                            ++wideHorizontalCoverage;
                        if (compactVertical[index].r > 0.05f)
                            ++compactVerticalCoverage;
                        if (wideVertical[index].r > 0.05f)
                            ++wideVerticalCoverage;
                    }
                }
                Assert.That(compactHorizontalCoverage,
                    Is.GreaterThan(wideHorizontalCoverage));
                Assert.That(compactVerticalCoverage,
                    Is.GreaterThan(wideVerticalCoverage));
            }
            finally
            {
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void AsciiPixelifyAndFallbackRampsAreDistinctBlankAndAlphaPreserving()
        {
            const int width = 256;
            const int height = 64;
            int toneCount = AsciiRamp.Length + 1;
            Texture2D ramp = null;
            Texture2D black = null;
            Texture2D readback = null;
            RenderTexture target = null;
            Material material = null;
            try
            {
                ramp = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);
                black = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);
                Color[] rampPixels = new Color[width * height];
                Color[] blackPixels = new Color[width * height];
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        int band = Mathf.Min(x / (width / toneCount),
                            toneCount - 1);
                        float luminance = band == 0 ? 0f :
                            (band + 0.5f) / toneCount;
                        float alpha = 0.2f + 0.6f * x / (width - 1f);
                        rampPixels[y * width + x] = new Color(
                            luminance, luminance, luminance, alpha);
                        blackPixels[y * width + x] = new Color(0f, 0f, 0f, 0.43f);
                    }
                }
                ramp.SetPixels(rampPixels);
                ramp.Apply(false, false);
                black.SetPixels(blackPixels);
                black.Apply(false, false);
                target = new RenderTexture(width, height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                target.Create();
                readback = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);
                material = new Material(Shader.Find(PostShaderName));
                ConfigurePostFilter(material, 9, 0);
                material.SetFloat("_AsciiCellSize", width / (float)toneCount);
                material.SetFloat("_AsciiSourceColor", 0f);
                material.SetFloat("_AsciiInvert", 0f);
                material.SetFloat("_AsciiSdfThreshold", 0.54f);
                material.SetFloat("_AsciiSdfSoftness", 0.001f);

                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    PixelifyFontPath);
                Assert.That(font, Is.Not.Null);
                for (int fontMode = 0; fontMode < 2; ++fontMode)
                {
                    material.SetTexture("_AsciiFontAtlas",
                        fontMode == 0 ? font.atlasTextures[0] : null);
                    new UberShaderGUI().ValidateMaterial(material);
                    Assert.That(material.GetFloat("_AsciiFontReady"),
                        Is.EqualTo(fontMode == 0 ? 1f : 0f));
                    string context = fontMode == 0 ? "PixelifySans" : "fallback";
                    AssertAsciiRender(RenderPostFilter(
                            ramp, material, target, readback), rampPixels,
                        width, height, context, false);
                    AssertAsciiRender(RenderPostFilter(
                            black, material, target, readback), blackPixels,
                        width, height, context + " black", true);
                }
            }
            finally
            {
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (black != null)
                    UnityEngine.Object.DestroyImmediate(black);
                if (ramp != null)
                    UnityEngine.Object.DestroyImmediate(ramp);
            }
        }

        [Test]
        public void PostFiltersRenderFinitePreserveAlphaAndRemainDistinct()
        {
            const int size = 32;
            Texture2D source = null;
            Texture2D readback = null;
            Material material = null;
            RenderTexture target = null;
            Vector4 previousTime = Shader.GetGlobalVector("_Time");
            try
            {
                source = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true);
                Color[] sourcePixels = new Color[size * size];
                for (int y = 0; y < size; ++y)
                {
                    for (int x = 0; x < size; ++x)
                    {
                        float checker = ((x / 4 + y / 4) & 1) == 0 ? 0.15f : 0f;
                        float horizontal = x / (size - 1f);
                        float vertical = y / (size - 1f);
                        sourcePixels[y * size + x] = new Color(
                            Mathf.Clamp01(horizontal + checker),
                            Mathf.Clamp01(vertical * 0.8f + checker),
                            Mathf.Clamp01((1f - horizontal) * 0.7f + checker),
                            0.2f + vertical * 0.7f);
                    }
                }
                source.SetPixels(sourcePixels);
                source.Apply(false, false);

                material = new Material(Shader.Find(PostShaderName));
                target = new RenderTexture(size, size, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);
                readback = new Texture2D(size, size, TextureFormat.RGBAFloat,
                    false, true);
                Shader.SetGlobalVector("_Time", new Vector4(0.05f, 1f, 2f, 3f));

                ConfigurePostFilter(material, 0, 0);
                Color[] baseline = RenderPostFilter(
                    source, material, target, readback);
                Dictionary<int, Color[]> representative =
                    new Dictionary<int, Color[]>();

                foreach (int mode in new[] { 6, 7, 8, 9, 10 })
                {
                    foreach (int profile in new[] { -1, 0, 1 })
                    {
                        ConfigurePostFilter(material, mode, profile);
                        Color[] output = RenderPostFilter(
                            source, material, target, readback);
                        string context = "mode " + mode + ", profile " + profile;
                        for (int pixel = 0; pixel < output.Length; ++pixel)
                        {
                            Color color = output[pixel];
                            Assert.That(IsFinite(color), Is.True,
                                context + ", pixel " + pixel);
                            Assert.That(color.a,
                                Is.EqualTo(baseline[pixel].a).Within(0.00001f),
                                context + ", alpha pixel " + pixel);
                        }

                        if (profile == 0)
                        {
                            representative.Add(mode, output);
                            Assert.That(MaxRgbDifference(output, baseline),
                                Is.GreaterThan(0.001f), context + " vs baseline");
                        }
                    }
                }

                for (int first = 6; first <= 10; ++first)
                {
                    for (int second = first + 1; second <= 10; ++second)
                    {
                        Assert.That(MaxRgbDifference(
                                representative[first], representative[second]),
                            Is.GreaterThan(0.001f),
                            "representative modes " + first + " and " + second);
                    }
                }
            }
            finally
            {
                Shader.SetGlobalVector("_Time", previousTime);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void CrtFilterPreservesAlphaBlendsBoundaryAndFreezesAnimation()
        {
            const int width = 96;
            const int height = 64;
            Texture2D source = null;
            Texture2D readback = null;
            Material material = null;
            RenderTexture target = null;
            Vector4 previousTime = Shader.GetGlobalVector("_Time");
            try
            {
                source = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);
                Color[] sourcePixels = new Color[width * height];
                for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    float checker = ((x / 6 + y / 6) & 1) == 0 ? 0.18f : 0f;
                    sourcePixels[y * width + x] = new Color(
                        Mathf.Clamp01(x / (width - 1f) + checker),
                        Mathf.Clamp01(y / (height - 1f) * 0.8f + checker),
                        Mathf.Clamp01((1f - x / (width - 1f)) * 0.7f + checker),
                        0.15f + y / (height - 1f) * 0.75f);
                }
                source.SetPixels(sourcePixels);
                source.Apply(false, false);

                material = new Material(Shader.Find(PostShaderName));
                target = new RenderTexture(width, height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);
                readback = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);

                Shader.SetGlobalVector("_Time", new Vector4(0.05f, 1f, 2f, 3f));
                ConfigurePostFilter(material, 0, 0);
                Color[] baseline = RenderPostFilter(source, material, target, readback);
                ConfigurePostFilter(material, 10, 0);
                Color[] representative = RenderPostFilter(
                    source, material, target, readback);
                Assert.That(MaxRgbDifference(representative, baseline),
                    Is.GreaterThan(0.001f));
                for (int pixel = 0; pixel < representative.Length; ++pixel)
                {
                    Assert.That(IsFinite(representative[pixel]), Is.True,
                        "representative pixel " + pixel);
                    Assert.That(representative[pixel].a,
                        Is.EqualTo(baseline[pixel].a).Within(0.00001f),
                        "representative alpha " + pixel);
                }
                foreach (int corner in new[]
                         {
                             0, width - 1, (height - 1) * width,
                             width * height - 1,
                         })
                {
                    Assert.That(Mathf.Max(representative[corner].r,
                            representative[corner].g, representative[corner].b),
                        Is.LessThan(0.01f), "black CRT corner " + corner);
                }
                foreach (int edgeCenter in new[]
                         {
                             width / 2, (height / 2) * width,
                             (height / 2) * width + width - 1,
                             (height - 1) * width + width / 2,
                         })
                {
                    Assert.That(Mathf.Max(representative[edgeCenter].r,
                            representative[edgeCenter].g,
                            representative[edgeCenter].b),
                        Is.GreaterThan(0.05f),
                        "CRT axis endpoint must meet the screen edge " + edgeCenter);
                }
                Color center = representative[(height / 2) * width + width / 2];
                float maximum = representative.Max(color =>
                    Mathf.Max(color.r, color.g, color.b));
                Assert.That(Mathf.Max(center.r, center.g, center.b),
                    Is.GreaterThan(0.05f), "maximum CRT RGB " + maximum);

                material.SetFloat("_CRTStrength", 0f);
                Color[] strengthZero = RenderPostFilter(
                    source, material, target, readback);
                Assert.That(MaxRgbDifference(strengthZero, baseline),
                    Is.LessThan(0.00001f));

                ConfigurePostFilter(material, 10, 0);
                material.SetFloat("_CRTAnimationSpeed", 0f);
                Shader.SetGlobalVector("_Time", new Vector4(0.1f, 2f, 4f, 6f));
                Color[] frozenFirst = RenderPostFilter(
                    source, material, target, readback);
                Shader.SetGlobalVector("_Time", new Vector4(0.2f, 5f, 10f, 15f));
                Color[] frozenSecond = RenderPostFilter(
                    source, material, target, readback);
                Assert.That(MaxRgbDifference(frozenFirst, frozenSecond),
                    Is.LessThan(0.00001f));

                ConfigurePostFilter(material, 10, 0);
                Shader.SetGlobalVector("_Time", new Vector4(5000f, 100001f, 200002f, 300003f));
                Color[] longRuntimeFirst = RenderPostFilter(
                    source, material, target, readback);
                Shader.SetGlobalVector("_Time", new Vector4(5000.05f, 100002f, 200004f, 300006f));
                Color[] longRuntimeSecond = RenderPostFilter(
                    source, material, target, readback);
                Assert.That(MaxRgbDifference(longRuntimeFirst, longRuntimeSecond),
                    Is.GreaterThan(0.00001f), "CRT animation froze after a long runtime.");

                string[] temporalProperties =
                {
                    "_CRTSignalNoise", "_CRTHorizontalJitter", "_CRTRollingBand",
                };
                float[] temporalStrengths = { 0.18f, 2f, 0.7f };
                for (int index = 0; index < temporalProperties.Length; ++index)
                {
                    material.SetFloat("_CRTSignalNoise", 0f);
                    material.SetFloat("_CRTHorizontalJitter", 0f);
                    material.SetFloat("_CRTRollingBand", 0f);
                    material.SetFloat(temporalProperties[index], temporalStrengths[index]);
                    material.SetFloat("_CRTAnimationSpeed", 1f);
                    Shader.SetGlobalVector("_Time", new Vector4(0.05f, 1f, 2f, 3f));
                    Color[] animatedFirst = RenderPostFilter(
                        source, material, target, readback);
                    Shader.SetGlobalVector("_Time", new Vector4(0.1f, 2f, 4f, 6f));
                    Color[] animatedSecond = RenderPostFilter(
                        source, material, target, readback);
                    Assert.That(MaxRgbDifference(animatedFirst, animatedSecond),
                        Is.GreaterThan(0.00001f), temporalProperties[index]);
                }

                material.SetFloat("_CRTSignalNoise", 0f);
                material.SetFloat("_CRTHorizontalJitter", 0f);
                material.SetFloat("_CRTRollingBand", 0f);
                Shader.SetGlobalVector("_Time", new Vector4(0.05f, 1f, 2f, 3f));
                Color[] neutralFirst = RenderPostFilter(
                    source, material, target, readback);
                Shader.SetGlobalVector("_Time", new Vector4(0.1f, 2f, 4f, 6f));
                Color[] neutralSecond = RenderPostFilter(
                    source, material, target, readback);
                Assert.That(MaxRgbDifference(neutralFirst, neutralSecond),
                    Is.LessThan(0.00001f));
            }
            finally
            {
                Shader.SetGlobalVector("_Time", previousTime);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void CrtPowerOffAmountCollapsesVerticallyThenHorizontallyAndReverses()
        {
            const int width = 96;
            const int height = 64;
            Texture2D source = null;
            Texture2D readback = null;
            Material material = null;
            RenderTexture target = null;
            try
            {
                source = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);
                source.SetPixels(Enumerable.Repeat(
                    new Color(0.25f, 0.35f, 0.45f, 0.6f), width * height).ToArray());
                source.Apply(false, false);

                material = new Material(Shader.Find(PostShaderName));
                target = new RenderTexture(width, height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Assert.That(target.Create(), Is.True);
                readback = new Texture2D(width, height, TextureFormat.RGBAFloat,
                    false, true);

                ConfigurePostFilter(material, 10, 0);
                material.SetFloat("_CRTCurvature", 0f);
                material.SetFloat("_CRTScanlineStrength", 0f);
                material.SetFloat("_CRTMaskStrength", 0f);
                material.SetFloat("_CRTChromaticAberration", 0f);
                material.SetFloat("_CRTVignetteStrength", 0f);
                material.SetFloat("_CRTSignalNoise", 0f);
                material.SetFloat("_CRTHorizontalJitter", 0f);
                material.SetFloat("_CRTRollingBand", 0f);
                material.SetFloat("_CRTAnimationSpeed", 0f);
                material.SetFloat("_CRTPowerBloomIntensity", 2f);

                material.SetFloat("_CRTPowerOffAmount", 0f);
                Color[] poweredOn = RenderPostFilter(
                    source, material, target, readback);

                material.SetFloat("_CRTPowerOffAmount", 0.5f);
                Color[] collapsedLine = RenderPostFilter(
                    source, material, target, readback);
                int centerRowStart = (height / 2) * width;
                int lineWidth = collapsedLine.Skip(centerRowStart).Take(width)
                    .Count(color => Mathf.Max(color.r, color.g, color.b) > 0.05f);
                Assert.That(lineWidth, Is.GreaterThan(width * 0.9f),
                    "The first half must retain a wide horizontal line.");
                Assert.That(collapsedLine.Max(color =>
                        Mathf.Max(color.r, color.g, color.b)),
                    Is.GreaterThan(1f),
                    "The power transition must emit HDR values for URP Bloom.");
                Color upperCenter = collapsedLine[(height / 4) * width + width / 2];
                Assert.That(Mathf.Max(upperCenter.r, upperCenter.g, upperCenter.b),
                    Is.LessThan(0.01f),
                    "The first half must collapse the image vertically.");

                material.SetFloat("_CRTPowerOffAmount", 0.75f);
                Color[] collapsedPoint = RenderPostFilter(
                    source, material, target, readback);
                int pointWidth = collapsedPoint.Skip(centerRowStart).Take(width)
                    .Count(color => Mathf.Max(color.r, color.g, color.b) > 0.05f);
                Assert.That(pointWidth, Is.LessThan(lineWidth * 0.75f),
                    "The second half must close the horizontal line.");
                Assert.That(pointWidth, Is.GreaterThan(2),
                    "The second half must remain visible before fully powering off.");

                material.SetFloat("_CRTPowerOffAmount", 1f);
                Color[] poweredOff = RenderPostFilter(
                    source, material, target, readback);
                Assert.That(poweredOff.Max(color =>
                        Mathf.Max(color.r, color.g, color.b)),
                    Is.LessThan(0.0001f), "A value of one must be fully off.");

                material.SetFloat("_CRTPowerOffAmount", 0f);
                Color[] poweredOnAgain = RenderPostFilter(
                    source, material, target, readback);
                Assert.That(MaxRgbDifference(poweredOnAgain, poweredOn),
                    Is.LessThan(0.00001f),
                    "Animating the value in reverse must restore the powered-on image.");
            }
            finally
            {
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (readback != null)
                    UnityEngine.Object.DestroyImmediate(readback);
                if (material != null)
                    UnityEngine.Object.DestroyImmediate(material);
                if (source != null)
                    UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void ConfigurePostFilter(Material material, int mode,
            int profile)
        {
            material.SetFloat("_ScreenFilterMode", mode);
            new UberShaderGUI().ValidateMaterial(material);
            bool minimum = profile < 0;
            bool maximum = profile > 0;

            switch (mode)
            {
                case 6:
                    material.SetColor("_GradientShadowColor",
                        new Color(0.02f, 0.01f, 0.08f, 1f));
                    material.SetColor("_GradientMidtoneColor",
                        new Color(0.45f, 0.04f, 0.5f, 1f));
                    material.SetColor("_GradientHighlightColor",
                        new Color(1f, 0.75f, 0.25f, 1f));
                    material.SetFloat("_GradientMidpoint",
                        minimum ? 0.01f : maximum ? 0.99f : 0.45f);
                    material.SetFloat("_GradientStrength", minimum ? 0f : 1f);
                    break;
                case 7:
                    material.SetColor("_OldFilmTint", minimum
                        ? new Color(1f, 1f, 1f, 0f)
                        : new Color(1f, 0.72f, 0.35f, maximum ? 1f : 0.4f));
                    material.SetFloat("_OldFilmSepia",
                        minimum ? 0f : maximum ? 1f : 0.7f);
                    material.SetFloat("_OldFilmGrain",
                        minimum ? 0f : maximum ? 0.5f : 0.08f);
                    material.SetFloat("_OldFilmScratch",
                        minimum ? 0f : maximum ? 1f : 0.4f);
                    material.SetFloat("_OldFilmFlicker",
                        minimum ? 0f : maximum ? 0.5f : 0.08f);
                    material.SetFloat("_OldFilmJitter",
                        minimum ? 0f : maximum ? 4f : 1f);
                    material.SetFloat("_OldFilmVignette",
                        minimum ? 0f : maximum ? 1f : 0.45f);
                    break;
                case 8:
                    material.SetColor("_EdgeColor", new Color(0.01f, 0.01f, 0.01f));
                    material.SetColor("_EdgeBackgroundColor", Color.white);
                    material.SetFloat("_EdgeThreshold",
                        minimum ? 0f : maximum ? 1f : 0.12f);
                    material.SetFloat("_EdgeWidth",
                        minimum ? 0.25f : maximum ? 4f : 1f);
                    material.SetFloat("_EdgeStrength", minimum ? 0f : 1f);
                    material.SetFloat("_EdgeSourceMix",
                        minimum ? 0f : maximum ? 1f : 0.25f);
                    break;
                case 9:
                    material.SetFloat("_AsciiCellSize",
                        minimum ? 2f : maximum ? 64f : 8f);
                    material.SetColor("_AsciiForegroundColor", Color.white);
                    material.SetColor("_AsciiBackgroundColor", Color.black);
                    material.SetFloat("_AsciiSourceColor",
                        minimum ? 0f : maximum ? 1f : 0.7f);
                    material.SetFloat("_AsciiInvert", maximum ? 1f : 0f);
                    break;
                case 10:
                    material.SetFloat("_CRTStrength", minimum ? 0f : 1f);
                    material.SetFloat("_CRTCurvature",
                        minimum ? 0f : maximum ? 0.5f : 0.12f);
                    material.SetFloat("_CRTScanlineDensity",
                        minimum ? 64f : maximum ? 1440f : 480f);
                    material.SetFloat("_CRTScanlineStrength",
                        minimum ? 0f : maximum ? 1f : 0.25f);
                    material.SetFloat("_CRTMaskScale",
                        minimum ? 1f : maximum ? 8f : 1f);
                    material.SetFloat("_CRTMaskStrength",
                        minimum ? 0f : maximum ? 1f : 0.2f);
                    material.SetFloat("_CRTChromaticAberration",
                        minimum ? 0f : maximum ? 8f : 1f);
                    material.SetFloat("_CRTVignetteStrength",
                        minimum ? 0f : maximum ? 1f : 0.35f);
                    material.SetFloat("_CRTSignalNoise",
                        minimum ? 0f : maximum ? 0.25f : 0.03f);
                    material.SetFloat("_CRTHorizontalJitter",
                        minimum ? 0f : maximum ? 4f : 0.5f);
                    material.SetFloat("_CRTRollingBand",
                        minimum ? 0f : maximum ? 1f : 0.08f);
                    material.SetFloat("_CRTAnimationSpeed",
                        minimum ? 0f : maximum ? 5f : 1f);
                    material.SetFloat("_CRTPowerOffAmount", maximum ? 1f : 0f);
                    material.SetFloat("_CRTPowerBloomIntensity",
                        minimum ? 0f : maximum ? 8f : 2f);
                    break;
            }
        }

        private static Color[] RenderPostFilter(Texture2D source, Material material,
            RenderTexture target, Texture2D readback)
        {
            material.SetTexture("_BlitTexture", source);
            material.SetVector("_BlitTexture_TexelSize", new Vector4(
                1f / source.width, 1f / source.height, source.width, source.height));
            material.SetVector("_BlitScaleBias", new Vector4(1f, 1f, 0f, 0f));
            material.SetFloat("_BlitMipLevel", 0f);
            RenderTexture previous = RenderTexture.active;
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

        private static void AssertAsciiFontState(Material material,
            Texture expectedAtlas, bool ready)
        {
            Assert.That(material.GetFloat("_AsciiFontReady"),
                Is.EqualTo(ready ? 1f : 0f));
            Assert.That(material.GetTexture("_AsciiFontAtlas"),
                ready ? Is.SameAs(expectedAtlas) : Is.Null);
            for (int index = 0; index < AsciiRamp.Length; ++index)
            {
                Assert.That(material.GetVector("_AsciiGlyphUV" + index),
                    ready ? Is.Not.EqualTo(Vector4.zero) : Is.EqualTo(Vector4.zero));
                Assert.That(material.GetVector("_AsciiGlyphPlacement" + index),
                    ready ? Is.Not.EqualTo(Vector4.zero) : Is.EqualTo(Vector4.zero));
            }
        }

        private static void AssertAsciiRender(Color[] output, Color[] source,
            int width, int height, string context, bool solidBlack)
        {
            Assert.That(output, Has.Length.EqualTo(source.Length));
            for (int index = 0; index < output.Length; ++index)
            {
                Assert.That(IsFinite(output[index]), Is.True,
                    context + " pixel " + index);
                Assert.That(output[index].a,
                    Is.EqualTo(source[index].a).Within(0.0001f),
                    context + " alpha " + index);
                if (solidBlack)
                    Assert.That(Mathf.Max(output[index].r, output[index].g,
                        output[index].b), Is.LessThan(0.002f), context);
            }
            if (solidBlack)
                return;

            int toneCount = AsciiRamp.Length + 1;
            int cell = width / toneCount;
            for (int band = 0; band < toneCount; ++band)
            {
                float maximum = 0f;
                for (int y = 0; y < cell && y < height; ++y)
                for (int x = 0; x < cell; ++x)
                    maximum = Mathf.Max(maximum,
                        output[y * width + band * cell + x].r);
                Assert.That(maximum, band == 0 ? Is.LessThan(0.002f) :
                    Is.GreaterThan(0.05f), context + " band " + band);
            }
            for (int first = 1; first < toneCount; ++first)
            for (int second = first + 1; second < toneCount; ++second)
            {
                float difference = 0f;
                for (int y = 0; y < cell && y < height; ++y)
                for (int x = 0; x < cell; ++x)
                    difference = Mathf.Max(difference, Mathf.Abs(
                        output[y * width + first * cell + x].r -
                        output[y * width + second * cell + x].r));
                Assert.That(difference, Is.GreaterThan(0.02f),
                    context + " bands " + first + "/" + second);
            }
        }

    }
}
