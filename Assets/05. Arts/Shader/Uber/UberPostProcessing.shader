Shader "Shader/Uber/Post Processing"
{
    Properties
    {
        [Main(ScreenFilter, _, on, off)] _ScreenFilterOptions("Screen Filter", Float) = 1
        [Title(ScreenFilter, _)] [UberPostFilter(ScreenFilter)] _ScreenFilterMode("Filter", Float) = 0

        [Title(ScreenFilter_PIXELATION_ON, _)] [Sub(ScreenFilter_PIXELATION_ON)] _PixelSize("Pixel Size", Range(1, 64)) = 4

        [Title(ScreenFilter_COLOR_ADJUST_ON, _)] [Sub(ScreenFilter_COLOR_ADJUST_ON)] _HueShift("Hue Shift", Range(-180, 180)) = 0
        [Sub(ScreenFilter_COLOR_ADJUST_ON)] _Saturation("Saturation", Range(0, 2)) = 1
        [Sub(ScreenFilter_COLOR_ADJUST_ON)] _Brightness("Brightness", Range(0, 2)) = 1
        [Sub(ScreenFilter_COLOR_ADJUST_ON)] _Contrast("Contrast", Range(0, 2)) = 1

        [Title(ScreenFilter_COLOR_SCREEN_BLEND_ON, _)] [Sub(ScreenFilter_COLOR_SCREEN_BLEND_ON)] _ColorScreen("Screen Color", Color) = (1, 1, 1, 1)
        [Sub(ScreenFilter_COLOR_SCREEN_BLEND_ON)] _BlendStrength("Blend Strength", Range(0, 1)) = 0

        [Title(ScreenFilter_ORDERED_DITHER_ON, _)] [Sub(ScreenFilter_ORDERED_DITHER_ON)] _DitherStrength("Dither Strength", Range(0, 1)) = 0.65

        [Title(ScreenFilter_COLOR_QUANTIZATION_ON, _)] [Sub(ScreenFilter_COLOR_QUANTIZATION_ON)] _ColorLevels("Color Levels", Range(2, 64)) = 16

        [Title(ScreenFilter_GRADIENT_MAP_ON, _)] [Sub(ScreenFilter_GRADIENT_MAP_ON)] _GradientShadowColor("Shadow Color", Color) = (0.03, 0.015, 0.08, 1)
        [Sub(ScreenFilter_GRADIENT_MAP_ON)] _GradientMidtoneColor("Midtone Color", Color) = (0.35, 0.08, 0.45, 1)
        [Sub(ScreenFilter_GRADIENT_MAP_ON)] _GradientHighlightColor("Highlight Color", Color) = (1, 0.78, 0.35, 1)
        [Sub(ScreenFilter_GRADIENT_MAP_ON)] _GradientMidpoint("Midpoint", Range(0.01, 0.99)) = 0.5
        [Sub(ScreenFilter_GRADIENT_MAP_ON)] _GradientStrength("Strength", Range(0, 1)) = 1

        [Title(ScreenFilter_OLD_FILM_ON, _)] [Sub(ScreenFilter_OLD_FILM_ON)] _OldFilmTint("Tint", Color) = (1, 0.82, 0.55, 0.35)
        [Sub(ScreenFilter_OLD_FILM_ON)] _OldFilmSepia("Sepia", Range(0, 1)) = 0.65
        [Sub(ScreenFilter_OLD_FILM_ON)] _OldFilmGrain("Grain", Range(0, 0.5)) = 0.08
        [Sub(ScreenFilter_OLD_FILM_ON)] _OldFilmScratch("Scratches", Range(0, 1)) = 0.4
        [Sub(ScreenFilter_OLD_FILM_ON)] _OldFilmFlicker("Flicker", Range(0, 0.5)) = 0.08
        [Sub(ScreenFilter_OLD_FILM_ON)] _OldFilmJitter("Jitter", Range(0, 4)) = 1
        [Sub(ScreenFilter_OLD_FILM_ON)] _OldFilmVignette("Vignette", Range(0, 1)) = 0.45

        [Title(ScreenFilter_EDGE_FILTER_ON, _)] [Sub(ScreenFilter_EDGE_FILTER_ON)] _EdgeColor("Edge Color", Color) = (0.02, 0.02, 0.02, 1)
        [Sub(ScreenFilter_EDGE_FILTER_ON)] _EdgeBackgroundColor("Background Color", Color) = (1, 1, 1, 1)
        [Sub(ScreenFilter_EDGE_FILTER_ON)] _EdgeThreshold("Threshold", Range(0, 1)) = 0.2
        [Sub(ScreenFilter_EDGE_FILTER_ON)] _EdgeWidth("Width", Range(0.25, 4)) = 1
        [Sub(ScreenFilter_EDGE_FILTER_ON)] _EdgeStrength("Strength", Range(0, 1)) = 1
        [Sub(ScreenFilter_EDGE_FILTER_ON)] _EdgeSourceMix("Source / Background Mix", Range(0, 1)) = 1

        [Title(ScreenFilter_ASCII_FILTER_ON, _)] [UberAsciiFont(ScreenFilter_ASCII_FILTER_ON)] [NoScaleOffset] _AsciiFontAtlas("Font Asset", 2D) = "black" {}
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiSdfThreshold("SDF Threshold", Range(0, 1)) = 0.5
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiSdfSoftness("SDF Softness", Range(0.001, 0.5)) = 0.08
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiCellSize("Cell Size", Range(2, 64)) = 8
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiCharacterSpacing("Horizontal Spacing", Range(-0.75, 0.75)) = -0.35
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiLineSpacing("Vertical Spacing", Range(-0.75, 0.75)) = 0
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiForegroundColor("Foreground Color", Color) = (1, 1, 1, 1)
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiBackgroundColor("Background Color", Color) = (0, 0, 0, 1)
        [Sub(ScreenFilter_ASCII_FILTER_ON)] _AsciiSourceColor("Source Color Contribution", Range(0, 1)) = 0.75
        [SubToggle(ScreenFilter_ASCII_FILTER_ON, _)] _AsciiInvert("Invert", Float) = 0
        [HideInInspector] _AsciiFontReady("ASCII Font Ready", Float) = 0
        [HideInInspector] _AsciiGlyphUV0("ASCII Glyph UV 0", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV1("ASCII Glyph UV 1", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV2("ASCII Glyph UV 2", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV3("ASCII Glyph UV 3", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV4("ASCII Glyph UV 4", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV5("ASCII Glyph UV 5", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV6("ASCII Glyph UV 6", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV7("ASCII Glyph UV 7", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV8("ASCII Glyph UV 8", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV9("ASCII Glyph UV 9", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV10("ASCII Glyph UV 10", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV11("ASCII Glyph UV 11", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV12("ASCII Glyph UV 12", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV13("ASCII Glyph UV 13", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphUV14("ASCII Glyph UV 14", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement0("ASCII Glyph Placement 0", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement1("ASCII Glyph Placement 1", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement2("ASCII Glyph Placement 2", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement3("ASCII Glyph Placement 3", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement4("ASCII Glyph Placement 4", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement5("ASCII Glyph Placement 5", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement6("ASCII Glyph Placement 6", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement7("ASCII Glyph Placement 7", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement8("ASCII Glyph Placement 8", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement9("ASCII Glyph Placement 9", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement10("ASCII Glyph Placement 10", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement11("ASCII Glyph Placement 11", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement12("ASCII Glyph Placement 12", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement13("ASCII Glyph Placement 13", Vector) = (0, 0, 0, 0)
        [HideInInspector] _AsciiGlyphPlacement14("ASCII Glyph Placement 14", Vector) = (0, 0, 0, 0)

        [Title(ScreenFilter_CRT_FILTER_ON, _)] [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTStrength("Overall Strength", Range(0, 1)) = 1
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTCurvature("Curvature", Range(0, 0.5)) = 0.12
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTScanlineDensity("Scanline Density", Range(64, 1440)) = 480
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTScanlineStrength("Scanline Strength", Range(0, 1)) = 0.25
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTMaskScale("Phosphor Mask Scale", Range(1, 8)) = 1
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTMaskStrength("Phosphor Mask Strength", Range(0, 1)) = 0.2
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTChromaticAberration("Chromatic Aberration", Range(0, 8)) = 1
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTVignetteStrength("Vignette Strength", Range(0, 1)) = 0.35
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTSignalNoise("Signal Noise", Range(0, 0.25)) = 0.03
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTHorizontalJitter("Horizontal Jitter", Range(0, 4)) = 0.5
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTRollingBand("Rolling Band", Range(0, 1)) = 0.08
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTAnimationSpeed("Animation Speed", Range(-5, 5)) = 1
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTPowerOffAmount("Power Off Amount", Range(0, 1)) = 0
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTPowerBloomIntensity("Power Bloom Intensity", Range(0, 8)) = 2
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTBloomIntensity("Screen Bloom Intensity", Range(0, 4)) = 0
        [Sub(ScreenFilter_CRT_FILTER_ON)] _CRTBloomThreshold("Screen Bloom Threshold", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "Uber Post Processing"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment UberPostFragment

            #pragma shader_feature_local_fragment _ _PIXELATION_ON _COLOR_ADJUST_ON _COLOR_SCREEN_BLEND_ON _ORDERED_DITHER_ON _COLOR_QUANTIZATION_ON _GRADIENT_MAP_ON _OLD_FILM_ON _EDGE_FILTER_ON _ASCII_FILTER_ON _CRT_FILTER_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "UberPostProcessing.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "UberShaderGUI"
    FallBack Off
}
