Shader "Shader/Uber/2D Sprite"
{
    Properties
    {
        [Main(Surface, _, on, off)] _SurfaceOptions("Surface", Float) = 1
        [Title(Surface, _)] [KWEnum(Surface, Opaque, _, Transparent, _SURFACE_TYPE_TRANSPARENT)] _Surface("Surface Type", Float) = 1
        [KWEnum(Surface, Alpha, _, Premultiply, _, Additive, _, Multiply, _ALPHAMODULATE_ON)] _Blend("Blend Mode", Float) = 0
        [KWEnum(Surface, Lit, _, Unlit, _UNLIT_ON)] _LightingMode("Lighting Mode", Float) = 0
        [SubToggle(Surface, _ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        [Sub(Surface_ALPHATEST_ON)] _Cutoff("Threshold", Range(0, 1)) = 0.5
        [SubToggle(Surface, _)] _ReceiveShadows("Receive Shadows", Float) = 1
        [SubToggle(Surface, _)] _CastShadows("Cast Shadows", Float) = 1
        [KWEnum(Surface, Off, _, Front, _, Back, _)] _Cull("Render Face", Float) = 0
        [KWEnum(Surface, Off, _, Front, _, Back, _)] _ShadowCull("Shadow Face", Float) = 2
        [KWEnum(Surface, High, _, Low, _UBER_QUALITY_LOW)] _UberQuality("Quality", Float) = 0

        [Main(SurfaceInputs, _, on, off)] _SurfaceInputs("Surface Inputs", Float) = 1
        [Title(SurfaceInputs, _)] [Tex(SurfaceInputs)] [PerRendererData] [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [Sub(SurfaceInputs)] [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Sub(SurfaceInputs)] _AlphaMultiplier("Alpha Multiplier", Range(0, 1)) = 1
        [SubToggle(SurfaceInputs, _TINT_MASK_ON)] _TintMaskEnabled ("Use Tint Mask", Float) = 0
        [Tex(SurfaceInputs_TINT_MASK_ON)] [NoScaleOffset] _TintMask ("Tint Mask (R)", 2D) = "white" {}
        [Sub(SurfaceInputs_TINT_MASK_ON)] _TintMaskStrength ("Tint Strength", Range(0,1)) = 1
        [SubToggle(SurfaceInputs_TINT_MASK_ON, _)] _TintMaskInvert ("Invert Tint Mask", Float) = 0
        [HideInInspector] [PerRendererData] _BaseSpriteUVRect("Base Sprite UV Rect", Vector) = (0, 0, 1, 1)

        [SubToggle(SurfaceInputs, _NORMALMAP)] _NormalMapEnabled("Normal Map", Float) = 0
        [Tex(SurfaceInputs_NORMALMAP, _NormalScale)] [Normal] [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
        [HideInInspector] _NormalScale("Normal Scale", Range(0, 2)) = 1
        [SubToggle(SurfaceInputs, _METALLICMAP)] _MetallicMapEnabled("Use Metallic Map", Float) = 0
        [Tex(SurfaceInputs_METALLICMAP)] [NoScaleOffset] _MetallicMap("Metallic Map (R)", 2D) = "white" {}
        [Sub(SurfaceInputs)] _Metallic("Metallic", Range(0, 1)) = 0
        [SubToggle(SurfaceInputs, _SMOOTHNESSMAP)] _SmoothnessMapEnabled("Use Smoothness Map", Float) = 0
        [Tex(SurfaceInputs_SMOOTHNESSMAP)] [NoScaleOffset] _SmoothnessMap("Smoothness Map (R)", 2D) = "white" {}
        [Sub(SurfaceInputs)] _Smoothness("Smoothness", Range(0, 1)) = 0.5

        [Main(SecondaryLayer, _SECONDARY_LAYER_ON, on)] _SecondaryLayerEnabled("Secondary Layer", Float) = 0
        [Title(SecondaryLayer, _)] [Tex(SecondaryLayer)] [NoScaleOffset] _SecondaryTex("Secondary Sprite", 2D) = "white" {}
        [Sub(SecondaryLayer)] _SecondaryBlendAmount("Blend Amount", Range(0, 1)) = 0
        [HideInInspector] [PerRendererData] _SecondaryUVRect("Secondary Sprite UV Rect", Vector) = (0, 0, 1, 1)

        [Main(ColorAdjust, _COLOR_ADJUST_ON, on)] _ColorAdjustEnabled("Color Adjustment", Float) = 0
        [Title(ColorAdjust, _)] [Sub(ColorAdjust)] _HueShift("Hue Shift", Range(-180, 180)) = 0
        [Sub(ColorAdjust)] _Saturation("Saturation", Range(0, 2)) = 1
        [Sub(ColorAdjust)] _Brightness("Brightness", Range(0, 2)) = 1
        [Sub(ColorAdjust)] _Contrast("Contrast", Range(0, 2)) = 1

        [Main(Grayscale, _GRAYSCALE_ON, on)] _GrayscaleEnabled ("Grayscale Mask", Float) = 0
        [Title(Grayscale, _)] [Tex(Grayscale)] [NoScaleOffset] _GrayscaleMask ("Mask (R)", 2D) = "white" {}
        [Sub(Grayscale)] _GrayscaleStrength ("Strength", Range(0,1)) = 1
        [SubToggle(Grayscale, _)] _GrayscaleInvert ("Invert Mask", Float) = 0

        [Main(UVFade, _UV_FADE_ON, on)] _UVFadeEnabled("UV Fade", Float) = 0
        [Title(UVFade, _)] [KWEnum(UVFade, U, _, V, _)] _UVFadeAxis("Axis", Float) = 1
        [Sub(UVFade)] _UVFadeOpaque("Opaque UV", Range(0, 1)) = 0
        [Sub(UVFade)] _UVFadeTransparent("Transparent UV", Range(0, 1)) = 1

        [Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled("Dissolve", Float) = 0
        [Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap("Noise Map", 2D) = "white" {}
        [Sub(Dissolve)] _DissolveTilingOffset("Tiling XY / Offset ZW", Vector) = (1, 1, 0, 0)
        [Sub(Dissolve)] _DissolvePanning("Panning XY", Vector) = (0, 0, 0, 0)
        [KWEnum(Dissolve, Noise, _, Radial, _DISSOLVE_RADIAL, Swipe, _DISSOLVE_SWIPE)] _DissolveMode("Mode", Float) = 0
        [Sub(Dissolve)] _DissolveAmount("Amount", Range(0, 1)) = 0
        [UberMinMaxVector(Dissolve_)] _DissolveNoiseRange("Noise Range", Vector) = (0, 1, 0, 0)
        [UberVector2(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialCenter("Radial Center", Vector) = (0.5, 0.5, 0, 0)
        [UberMinMaxVector(Dissolve_DISSOLVE_RADIAL, _DissolveAmount)] _DissolveRadialRange("Radial Range", Vector) = (0, 0.7071, 0, 0)
        [Sub(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialNoiseStrength("Radial Noise Strength", Range(0, 1)) = 0.15
        [UberVector2(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeCenter("Swipe Center", Vector) = (0.5, 0.5, 0, 0)
        [Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRotation("Swipe Rotation", Range(-180, 180)) = 0
        [UberMinMaxVector(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRange("Swipe Range", Vector) = (-0.5, 0.5, 0, 0)
        [Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeNoiseStrength("Swipe Noise Strength", Range(0, 1)) = 0.15
        [Sub(Dissolve)] _DissolveEdgeWidth("Edge Width", Range(0, 1)) = 0.05
        [KWEnum(Dissolve, Single Color, _, Gradient, _DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeColorMode("Edge Color Mode", Float) = 0
        [Sub(Dissolve_)] [HDR] _DissolveEdgeColor("Edge Color", Color) = (1, 0.5, 0, 1)
        [UberGradient(Dissolve_DISSOLVE_EDGE_GRADIENT)] _DissolveEdgeGradientColor0("Edge Gradient", Vector) = (1, 0.5, 0, 0)
        [HideInInspector] _DissolveEdgeGradientColor1("Edge Gradient Color 1", Vector) = (1, 0.5, 0, 1)
        [HideInInspector] _DissolveEdgeGradientColor2("Edge Gradient Color 2", Vector) = (1, 0.5, 0, 1)
        [HideInInspector] _DissolveEdgeGradientColor3("Edge Gradient Color 3", Vector) = (1, 0.5, 0, 1)
        [HideInInspector] _DissolveEdgeGradientAlphas("Edge Gradient Alphas", Vector) = (1, 1, 1, 1)
        [HideInInspector] _DissolveEdgeGradientAlphaTimes("Edge Gradient Alpha Times", Vector) = (0, 1, 1, 1)
        [HideInInspector] _DissolveEdgeGradientMetadata("Edge Gradient Metadata", Vector) = (2, 2, 0, 0)
        [Sub(Dissolve)] _DissolveEdgeIntensity("Edge Intensity", Range(0, 16)) = 1

        [Main(LightSweep, _LIGHT_SWEEP_ON, on)] _LightSweepEnabled("Light Sweep", Float) = 0
        [Title(LightSweep, _)] [KWEnum(LightSweep, Soft, _, Sharp, _LIGHT_SWEEP_SHARP)] _LightSweepMode("Type", Float) = 0
        [KWEnum(LightSweep, Add, _, Multiply, _LIGHT_SWEEP_MULTIPLY)] _LightSweepBlendMode("Blend Mode", Float) = 0
        [Sub(LightSweep)] [HDR] _LightSweepColor("Color", Color) = (1, 1, 1, 1)
        [Sub(LightSweep)] _LightSweepIntensity("Intensity", Range(0, 16)) = 2
        [Sub(LightSweep)] _LightSweepAmount("Amount", Range(0, 1)) = 0
        [UberVector2(LightSweep)] _LightSweepCenter("Center", Vector) = (0.5, 0.5, 0, 0)
        [Sub(LightSweep)] _LightSweepRotation("Rotation", Range(-180, 180)) = 0
        [UberMinMaxVector(LightSweep)] _LightSweepRange("Range", Vector) = (-0.5, 0.5, 0, 0)
        [Sub(LightSweep)] _LightSweepWidth("Width", Range(0.001, 1)) = 0.15

        [Main(DitherFade, _DITHER_FADE_ON, on)] _DitherFadeEnabled("Dither Fade", Float) = 0
        [Title(DitherFade, _)] [Sub(DitherFade)] _DitherFade("Visibility", Range(0, 1)) = 1

        [Main(PixelOutline, _PIXEL_OUTLINE_ON, on)] _PixelOutlineEnabled("Pixel Outline", Float) = 0
        [Title(PixelOutline, _)] [Sub(PixelOutline)] [HDR] _PixelOutlineColor("Outline Color", Color) = (0.08, 0.06, 0.05, 1)
        [Sub(PixelOutline)] _PixelOutlineWidth("Width (Pixels)", Range(0, 4)) = 1
        [Sub(PixelOutline)] _PixelOutlineAlphaThreshold("Alpha Threshold", Range(0, 1)) = 0.5

        [Main(Emission, _EMISSION, on)] _EmissionEnabled("Emission", Float) = 0
        [Title(Emission, _)] [Tex(Emission)] [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}
        [Sub(Emission)] [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        [Sub(Emission)] _EmissionIntensity("Intensity", Range(0, 16)) = 1

        [Main(Rim, _RIM_ON, on)] _RimEnabled("Fresnel Rim", Float) = 0
        [Title(Rim, _)] [KWEnum(Rim, Add, _, Multiply, _RIM_MULTIPLY)] _RimBlendMode("Blend Mode", Float) = 0
        [Sub(Rim)] [HDR] _RimColor("Color", Color) = (1, 1, 1, 1)
        [Sub(Rim)] _RimPower("Edge Width (Pixels)", Range(0.5, 16)) = 4
        [Sub(Rim)] _RimEdgeSoftnessPixels("Edge Softness (Pixels)", Range(0, 32)) = 8
        [Sub(Rim)] _RimIntensity("Intensity", Range(0, 16)) = 1

        [Main(Hologram, _HOLOGRAM_ON, on)] _HologramEnabled("Hologram", Float) = 0
        [Title(Hologram, _)] [Sub(Hologram)] [HDR] _HologramColor("Color", Color) = (0, 1, 1, 1)
        [Sub(Hologram)] _HologramOpacity("Opacity", Range(0, 1)) = 0.35
        [Sub(Hologram)] _HologramFresnelPower("Edge Width (Pixels)", Range(0.5, 16)) = 4
        [Sub(Hologram)] _HologramFresnelIntensity("Edge Intensity", Range(0, 16)) = 2
        [Sub(Hologram)] _HologramEdgeSoftnessPixels("Edge Softness (Pixels)", Range(0, 32)) = 8
        [KWEnum(Hologram, Object, _, World, _HOLOGRAM_WORLD_SPACE, Screen, _HOLOGRAM_SCREEN_SPACE)] _HologramSpace("Space", Float) = 0
        [UberVector3(Hologram)] _HologramObjectUpVector("Object Up Vector", Vector) = (0, 1, 0, 0)
        [Sub(Hologram)] _HologramScanlineDensity("Scanline Density", Range(0.1, 128)) = 24
        [Sub(Hologram)] _HologramScanlineSpeed("Scanline Speed", Range(-10, 10)) = 1
        [Sub(Hologram)] _HologramScanlineWidth("Scanline Width", Range(0.01, 1)) = 0.12
        [Sub(Hologram)] _HologramScanlineIntensity("Scanline Intensity", Range(0, 16)) = 2
        [Sub(Hologram)] _HologramNoiseScale("Noise Scale", Range(0.01, 64)) = 4
        [Sub(Hologram)] _HologramNoiseStrength("Noise Strength", Range(0, 2)) = 0.35
        [Sub(Hologram)] _HologramNoiseSpeed("Noise Speed", Range(-10, 10)) = 0.5

        [Main(Glitch, _GLITCH_ON, on)] _GlitchEnabled("Glitch", Float) = 0
        [Title(Glitch, _)] [Sub(Glitch)] _GlitchStrength("Strength (Pixels)", Range(0, 64)) = 8
        [Sub(Glitch)] _GlitchRGBSplit("RGB Split (Pixels)", Range(0, 16)) = 2
        [Sub(Glitch)] _GlitchFrequency("Frequency", Range(0, 1)) = 0.25
        [Sub(Glitch)] _GlitchSpeed("Speed", Range(0, 30)) = 8
        [UberMinMaxVector(Glitch)] _GlitchBandSizeRange("Band Size Range (Pixels)", Vector) = (4, 12, 0, 0)

        // Native SpriteRenderer compatibility properties.
        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0

        // URP BaseShaderGUI render-state contract.
        [HideInInspector] _SrcBlend("__src", Float) = 5
        [HideInInspector] _DstBlend("__dst", Float) = 10
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 10
        [HideInInspector] _ZWrite("__zwrite", Float) = 0
        [HideInInspector] _BlendModePreserveSpecular("__preserveSpecular", Float) = 0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0
        [HideInInspector] _QueueControl("__queueControl", Float) = 0
        [HideInInspector] _QueueOffset("__queueOffset", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }
        LOD 300

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #define UBER_SPRITE_FORWARD_PASS 1
            #pragma target 3.0
            #pragma vertex UberSpriteForwardVertex
            #pragma fragment UberSpriteForwardFragment

            #pragma multi_compile_local _ _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma multi_compile_local_fragment _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _METALLICMAP
            #pragma multi_compile_local_fragment _ _SMOOTHNESSMAP
            #pragma multi_compile_local_fragment _ _UNLIT_ON
            #pragma multi_compile_local _ _RECEIVE_SHADOWS_OFF
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW

            #pragma shader_feature_local_fragment _ _SECONDARY_LAYER_ON
            #pragma shader_feature_local_fragment _ _COLOR_ADJUST_ON
            #pragma shader_feature_local_fragment _ _GRAYSCALE_ON
            #pragma shader_feature_local_fragment _ _TINT_MASK_ON
            #pragma shader_feature_local_fragment _ _UV_FADE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _DISSOLVE_EDGE_GRADIENT
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_ON
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_SHARP
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_MULTIPLY
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _RIM_ON
            #pragma shader_feature_local_fragment _ _RIM_MULTIPLY
            #pragma shader_feature_local_fragment _ _HOLOGRAM_ON
            #pragma shader_feature_local_fragment _ _HOLOGRAM_WORLD_SPACE _HOLOGRAM_SCREEN_SPACE
            #pragma shader_feature_local_fragment _ _GLITCH_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ SKINNED_SPRITE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "UberSprite.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #define UBER_SPRITE_2D_PASS 1
            #pragma target 3.0
            #pragma vertex UberSprite2DVertex
            #pragma fragment UberSprite2DFragment

            #pragma multi_compile_local _ _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma multi_compile_local_fragment _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _UNLIT_ON
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW

            #pragma shader_feature_local_fragment _ _SECONDARY_LAYER_ON
            #pragma shader_feature_local_fragment _ _COLOR_ADJUST_ON
            #pragma shader_feature_local_fragment _ _GRAYSCALE_ON
            #pragma shader_feature_local_fragment _ _TINT_MASK_ON
            #pragma shader_feature_local_fragment _ _UV_FADE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _DISSOLVE_EDGE_GRADIENT
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_ON
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_SHARP
            #pragma shader_feature_local_fragment _ _LIGHT_SWEEP_MULTIPLY
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _RIM_ON
            #pragma shader_feature_local_fragment _ _RIM_MULTIPLY
            #pragma shader_feature_local_fragment _ _HOLOGRAM_ON
            #pragma shader_feature_local_fragment _ _HOLOGRAM_WORLD_SPACE _HOLOGRAM_SCREEN_SPACE
            #pragma shader_feature_local_fragment _ _GLITCH_ON

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE
            #include "UberSprite.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "NormalsRendering"
            Tags { "LightMode" = "NormalsRendering" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #define UBER_SPRITE_NORMALS_PASS 1
            #pragma target 3.0
            #pragma vertex UberSpriteNormalsVertex
            #pragma fragment UberSpriteNormalsFragment

            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma shader_feature_local_fragment _ _SECONDARY_LAYER_ON
            #pragma shader_feature_local_fragment _ _UV_FADE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _ _GLITCH_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #include "UberSprite.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_ShadowCull]

            HLSLPROGRAM
            #define UBER_SPRITE_SHADOW_PASS 1
            #pragma target 3.0
            #pragma vertex UberSpriteShadowVertex
            #pragma fragment UberSpriteSilhouetteFragment

            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma shader_feature_local_fragment _ _SECONDARY_LAYER_ON
            #pragma shader_feature_local_fragment _ _UV_FADE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _ _GLITCH_ON

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "UberSprite.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #define UBER_SPRITE_DEPTH_PASS 1
            #pragma target 3.0
            #pragma vertex UberSpriteDepthVertex
            #pragma fragment UberSpriteSilhouetteFragment

            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma shader_feature_local_fragment _ _SECONDARY_LAYER_ON
            #pragma shader_feature_local_fragment _ _UV_FADE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _ _GLITCH_ON

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "UberSprite.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #define UBER_SPRITE_DEPTH_NORMALS_PASS 1
            #pragma target 3.0
            #pragma vertex UberSpriteNormalsVertex
            #pragma fragment UberSpriteDepthNormalsFragment

            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma shader_feature_local_fragment _ _SECONDARY_LAYER_ON
            #pragma shader_feature_local_fragment _ _UV_FADE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _PIXEL_OUTLINE_ON
            #pragma shader_feature_local_fragment _ _GLITCH_ON

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "UberSprite.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UberShaderGUI"
}
