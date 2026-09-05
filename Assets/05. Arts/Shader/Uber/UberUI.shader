Shader "Shader/Uber/UI"
{
    Properties
    {
        [Main(SurfaceInputs, _, on, off)] _SurfaceInputs ("Surface Inputs", Float) = 1
        [Title(SurfaceInputs, _)] [Tex(SurfaceInputs)] [PerRendererData] [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [Sub(SurfaceInputs)] [MainColor] _Color ("Tint", Color) = (1,1,1,1)
        [Sub(SurfaceInputs)] _AlphaMultiplier ("Alpha Multiplier", Range(0,1)) = 1
        [SubToggle(SurfaceInputs, _TINT_MASK_ON)] _TintMaskEnabled ("Use Tint Mask", Float) = 0
        [Tex(SurfaceInputs_TINT_MASK_ON)] [NoScaleOffset] _TintMask ("Tint Mask (R)", 2D) = "white" {}
        [Sub(SurfaceInputs_TINT_MASK_ON)] _TintMaskStrength ("Tint Strength", Range(0,1)) = 1
        [SubToggle(SurfaceInputs_TINT_MASK_ON, _)] _TintMaskInvert ("Invert Tint Mask", Float) = 0
        [KWEnum(SurfaceInputs, High, _, Low, _UBER_QUALITY_LOW)] _UberQuality ("Effect Quality", Float) = 0

        [Main(ColorAdjust, _COLOR_ADJUST_ON, on)] _ColorAdjustEnabled ("Color Adjustment", Float) = 0
        [Title(ColorAdjust, _)] [Sub(ColorAdjust)] _HueShift ("Hue Shift", Range(-180,180)) = 0
        [Sub(ColorAdjust)] _Saturation ("Saturation", Range(0,2)) = 1
        [Sub(ColorAdjust)] _Brightness ("Brightness", Range(0,2)) = 1
        [Sub(ColorAdjust)] _Contrast ("Contrast", Range(0,2)) = 1

        [Main(Grayscale, _GRAYSCALE_ON, on)] _GrayscaleEnabled ("Grayscale Mask", Float) = 0
        [Title(Grayscale, _)] [Tex(Grayscale)] [NoScaleOffset] _GrayscaleMask ("Mask (R)", 2D) = "white" {}
        [Sub(Grayscale)] _GrayscaleStrength ("Strength", Range(0,1)) = 1
        [SubToggle(Grayscale, _)] _GrayscaleInvert ("Invert Mask", Float) = 0

        [Main(Emission, _EMISSION, on)] _EmissionEnabled ("Emission", Float) = 0
        [Title(Emission, _)] [Tex(Emission)] [NoScaleOffset] _EmissionMap ("Emission Map", 2D) = "white" {}
        [Sub(Emission)] [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        [Sub(Emission)] _EmissionIntensity ("Intensity", Range(0,16)) = 1

        [Main(RGBOverride, _RGB_OVERRIDE_ON, on)] _RGBOverrideEnabled ("RGB Override", Float) = 0
        [Title(RGBOverride, _)] [Sub(RGBOverride)] [HDR] _RGBOverrideColor ("RGB Override Color", Color) = (1,1,1,1)
        [Sub(RGBOverride)] _RGBOverrideStrength ("RGB Override Strength", Range(0,1)) = 1

        [Main(UVFade, _UV_FADE_ON, on)] _UVFadeEnabled ("UV Fade", Float) = 0
        [Title(UVFade, _)] [Sub(UVFade)] _UVFadeDirection ("UV Fade Direction", Vector) = (0,1,0,0)
        [Sub(UVFade)] _UVFadeStart ("UV Fade Start", Range(0,1)) = 0
        [Sub(UVFade)] _UVFadeEnd ("UV Fade End", Range(0,1)) = 1

        [Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled ("Dissolve", Float) = 0
        [Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap ("Noise Map", 2D) = "white" {}
        [Sub(Dissolve)] _DissolveTilingOffset ("Tiling XY / Offset ZW", Vector) = (1,1,0,0)
        [Sub(Dissolve)] _DissolvePanning ("Panning XY", Vector) = (0,0,0,0)
        [KWEnum(Dissolve, Noise, _, Radial, _DISSOLVE_RADIAL, Swipe, _DISSOLVE_SWIPE)] _DissolveMode ("Mode", Float) = 0
        [Sub(Dissolve)] _DissolveAmount ("Amount", Range(0,1)) = 0
        [UberMinMaxVector(Dissolve_)] _DissolveNoiseRange ("Noise Range", Vector) = (0,1,0,0)
        [UberVector2(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialCenter ("Radial Center", Vector) = (0.5,0.5,0,0)
        [UberMinMaxVector(Dissolve_DISSOLVE_RADIAL, _DissolveAmount)] _DissolveRadialRange ("Radial Range", Vector) = (0,0.7071,0,0)
        [Sub(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialNoiseStrength ("Radial Noise Strength", Range(0,1)) = 0.15
        [UberVector2(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeCenter ("Swipe Center", Vector) = (0.5,0.5,0,0)
        [Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRotation ("Swipe Rotation", Range(-180,180)) = 0
        [UberMinMaxVector(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRange ("Swipe Range", Vector) = (-0.5,0.5,0,0)
        [Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeNoiseStrength ("Swipe Noise Strength", Range(0,1)) = 0.15
        [Sub(Dissolve)] _DissolveEdgeWidth ("Edge Width", Range(0,1)) = 0.05
        [KWEnum(Dissolve, Single Color, _, Gradient, _DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeColorMode ("Edge Color Mode", Float) = 0
        [Sub(Dissolve_)] [HDR] _DissolveEdgeColor ("Edge Color", Color) = (1,0.5,0,1)
        [UberGradient(Dissolve_DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeGradientColor0 ("Edge Gradient", Vector) = (1,0.5,0,0)
        [HideInInspector] _DissolveEdgeGradientColor1 ("Edge Gradient Color 1", Vector) = (1,0.5,0,1)
        [HideInInspector] _DissolveEdgeGradientColor2 ("Edge Gradient Color 2", Vector) = (1,0.5,0,1)
        [HideInInspector] _DissolveEdgeGradientColor3 ("Edge Gradient Color 3", Vector) = (1,0.5,0,1)
        [HideInInspector] _DissolveEdgeGradientAlphas ("Edge Gradient Alphas", Vector) = (1,1,1,1)
        [HideInInspector] _DissolveEdgeGradientAlphaTimes ("Edge Gradient Alpha Times", Vector) = (0,1,1,1)
        [HideInInspector] _DissolveEdgeGradientMetadata ("Edge Gradient Metadata", Vector) = (2,2,0,0)
        [Sub(Dissolve)] _DissolveEdgeIntensity ("Edge Intensity", Range(0,16)) = 1

        [Main(LightSweep, _LIGHT_SWEEP_ON, on)] _LightSweepEnabled ("Light Sweep", Float) = 0
        [Title(LightSweep, _)] [KWEnum(LightSweep, Soft, _, Sharp, _LIGHT_SWEEP_SHARP)] _LightSweepMode ("Type", Float) = 0
        [KWEnum(LightSweep, Add, _, Multiply, _LIGHT_SWEEP_MULTIPLY)] _LightSweepBlendMode ("Blend Mode", Float) = 0
        [Sub(LightSweep)] [HDR] _LightSweepColor ("Color", Color) = (1,1,1,1)
        [Sub(LightSweep)] _LightSweepIntensity ("Intensity", Range(0,16)) = 2
        [Sub(LightSweep)] _LightSweepAmount ("Amount", Range(0,1)) = 0
        [UberVector2(LightSweep)] _LightSweepCenter ("Center", Vector) = (0.5,0.5,0,0)
        [Sub(LightSweep)] _LightSweepRotation ("Rotation", Range(-180,180)) = 0
        [UberMinMaxVector(LightSweep)] _LightSweepRange ("Range", Vector) = (-0.5,0.5,0,0)
        [Sub(LightSweep)] _LightSweepWidth ("Width", Range(0.001,1)) = 0.15

        [Main(DitherFade, _DITHER_FADE_ON, on)] _DitherFadeEnabled ("Dither Fade", Float) = 0
        [Title(DitherFade, _)] [Sub(DitherFade)] _DitherFade ("Dither Visibility", Range(0,1)) = 1

        [Main(PixelOutline, _PIXEL_OUTLINE_ON, on)] _PixelOutlineEnabled ("Pixel Outline / Glow", Float) = 0
        [Title(PixelOutline, _)] [Sub(PixelOutline)] [HDR] _PixelOutlineColor ("Outline Color", Color) = (1,1,1,1)
        [Sub(PixelOutline)] _PixelOutlineWidth ("Outline Width (Pixels)", Range(0,4)) = 1
        [Sub(PixelOutline)] _PixelOutlineAlphaThreshold ("Outline Alpha Threshold", Range(0,1)) = 0.5
        [Sub(PixelOutline)] [HDR] _PixelGlowColor ("Glow Color", Color) = (1,1,1,0.35)
        [Sub(PixelOutline)] _PixelGlowWidth ("Glow Width (Pixels)", Range(0,8)) = 4
        [Sub(PixelOutline)] _PixelGlowIntensity ("Glow Intensity", Range(0,8)) = 1
        [HideInInspector] _BaseSpriteUVRect ("Base Sprite UV Rect", Vector) = (0,0,1,1)
        [HideInInspector] _PixelOutlineMeshPadding ("Outline Mesh Padding", Vector) = (0,0,0,0)

        [Main(Hologram, _HOLOGRAM_ON, on)] _HologramEnabled ("Hologram", Float) = 0
        [Title(Hologram, _)] [Sub(Hologram)] [HDR] _HologramColor ("Color", Color) = (0,1,1,1)
        [Sub(Hologram)] _HologramOpacity ("Opacity", Range(0,1)) = 0.35
        [Sub(Hologram)] _HologramFresnelPower ("Edge Width (Pixels)", Range(0.5,16)) = 4
        [Sub(Hologram)] _HologramFresnelIntensity ("Edge Intensity", Range(0,16)) = 2
        [Sub(Hologram)] _HologramEdgeSoftnessPixels ("Edge Softness (Pixels)", Range(0,32)) = 8
        [KWEnum(Hologram, Object, _, World, _HOLOGRAM_WORLD_SPACE, Screen, _HOLOGRAM_SCREEN_SPACE)] _HologramSpace ("Space", Float) = 0
        [UberVector3(Hologram)] _HologramObjectUpVector ("Object Up Vector", Vector) = (0,1,0,0)
        [Sub(Hologram)] _HologramScanlineDensity ("Scanline Density", Range(0.1,128)) = 24
        [Sub(Hologram)] _HologramScanlineSpeed ("Scanline Speed", Range(-10,10)) = 1
        [Sub(Hologram)] _HologramScanlineWidth ("Scanline Width", Range(0.01,1)) = 0.12
        [Sub(Hologram)] _HologramScanlineIntensity ("Scanline Intensity", Range(0,16)) = 2
        [Sub(Hologram)] _HologramNoiseScale ("Noise Scale", Range(0.01,64)) = 4
        [Sub(Hologram)] _HologramNoiseStrength ("Noise Strength", Range(0,2)) = 0.35
        [Sub(Hologram)] _HologramNoiseSpeed ("Noise Speed", Range(-10,10)) = 0.5

        [Main(Glitch, _GLITCH_ON, on)] _GlitchEnabled ("Glitch", Float) = 0
        [Title(Glitch, _)] [Sub(Glitch)] _GlitchStrength ("Strength (Pixels)", Range(0,64)) = 8
        [Sub(Glitch)] _GlitchRGBSplit ("RGB Split (Pixels)", Range(0,16)) = 2
        [Sub(Glitch)] _GlitchFrequency ("Frequency", Range(0,1)) = 0.25
        [Sub(Glitch)] _GlitchSpeed ("Speed", Range(0,30)) = 8
        [UberMinMaxVector(Glitch)] _GlitchBandSizeRange ("Band Size Range (Pixels)", Vector) = (4,12,0,0)

        [Main(StencilOptions, _, on, off)] _StencilOptions ("Stencil / Mask", Float) = 1
        [Title(StencilOptions, _)] [Sub(StencilOptions)] _StencilComp ("Stencil Comparison", Float) = 8
        [Sub(StencilOptions)] _Stencil ("Stencil ID", Float) = 0
        [Sub(StencilOptions)] _StencilOp ("Stencil Operation", Float) = 0
        [Sub(StencilOptions)] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [Sub(StencilOptions)] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [Sub(StencilOptions)] _ColorMask ("Color Mask", Float) = 15
        [SubToggle(StencilOptions, UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Sub(StencilOptionsUNITY_UI_ALPHACLIP)] _Cutoff ("Alpha Clip Threshold", Range(0,1)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex UberUIVert
            #pragma fragment UberUIFrag

            // uGUI mask configuration is structural and must never be stripped.
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local_fragment _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW

            // Optional visual modules and their submodes remain local features.
            #pragma shader_feature_local_fragment _COLOR_ADJUST_ON
            #pragma shader_feature_local_fragment _ _GRAYSCALE_ON
            #pragma shader_feature_local_fragment _ _TINT_MASK_ON
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _RGB_OVERRIDE_ON
            #pragma shader_feature_local_fragment _UV_FADE_ON
            #pragma shader_feature_local_fragment _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _DISSOLVE_EDGE_GRADIENT
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_ON
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_SHARP
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_MULTIPLY
            #pragma shader_feature_local_fragment _DITHER_FADE_ON
            #pragma shader_feature_local _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _ _HOLOGRAM_ON
            #pragma shader_feature_local_fragment _ _HOLOGRAM_WORLD_SPACE _HOLOGRAM_SCREEN_SPACE
            #pragma shader_feature_local_fragment _ _GLITCH_ON

            #include "UberUI.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "UberShaderGUI"
}
