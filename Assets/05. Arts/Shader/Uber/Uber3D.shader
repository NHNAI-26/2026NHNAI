Shader "Shader/Uber/3D Object"
{
    Properties
    {
        [Main(Surface, _, on, off)] _SurfaceOptions("Surface", Float) = 1
        [Title(Surface, _)] [KWEnum(Surface, Opaque, _, Transparent, _SURFACE_TYPE_TRANSPARENT)] _Surface("Surface Type", Float) = 0
        [KWEnum(Surface, Alpha, _, Premultiply, _ALPHAPREMULTIPLY_ON, Additive, _, Multiply, _ALPHAMODULATE_ON)] _Blend("Blend Mode", Float) = 0
        [KWEnum(Surface, Auto, _, On, _, Off, _)] _ZWriteControl("Depth Write", Float) = 0
        [SubToggle(Surface, _)] _AlphaClip("Alpha Clipping", Float) = 0
        [Sub(Surface)] _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [KWEnum(Surface, Lit, _, Unlit, _UNLIT_ON)] _LightingMode("Lighting Mode", Float) = 0
        [SubToggle(Surface, _)] _ReceiveShadows("Receive Shadows", Float) = 1
        [SubToggle(Surface, _)] _CastShadows("Cast Shadows", Float) = 1
        [KWEnum(Surface, Off, _, Front, _, Back, _)] _Cull("Render Face", Float) = 2
        [KWEnum(Surface, High, _, Low, _UBER_QUALITY_LOW)] _UberQuality("Effect Quality", Float) = 0
        [KWEnum(Surface, Auto, _, Custom, _)] _QueueControl("Queue Control", Float) = 0
        [Sub(Surface)] _QueueOffset("Queue Offset", Range(-50, 50)) = 0

        [Main(SurfaceInputs, _, on, off)] _SurfaceInputs("Surface Inputs", Float) = 1
        [Title(SurfaceInputs, _)] [UberGroup(SurfaceInputs)] [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [Sub(SurfaceInputs)] [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [KWEnum(SurfaceInputs, UV, _, Triplanar, _BASE_MAP_TRIPLANAR)] _BaseMapMapping("Base Map Mapping", Float) = 0
        [UberVector3(SurfaceInputs)] _BaseMap3DTiling("Base Map 3D Tiling", Vector) = (1, 1, 1, 0)
        [Sub(SurfaceInputs)] _BaseMap3DBlendSharpness("Base Map 3D Blend Sharpness", Range(1, 16)) = 4
        [SubToggle(SurfaceInputs, _METALLICMAP)] _MetallicMapEnabled("Use Metallic Map", Float) = 0
        [Tex(SurfaceInputs_METALLICMAP)] [NoScaleOffset] _MetallicMap("Metallic Map (R)", 2D) = "white" {}
        [Sub(SurfaceInputs)] _Metallic("Metallic", Range(0, 1)) = 0
        [SubToggle(SurfaceInputs, _ROUGHNESSMAP)] _RoughnessMapEnabled("Use Roughness Map", Float) = 0
        [Tex(SurfaceInputs_ROUGHNESSMAP)] [NoScaleOffset] _RoughnessMap("Roughness Map (R)", 2D) = "black" {}
        [Sub(SurfaceInputs)] _Smoothness("Smoothness", Range(0, 1)) = 0.5
        [SubToggle(SurfaceInputs, _)] _NormalMapEnabled("Normal Map", Float) = 0
        [Tex(SurfaceInputs)] [Normal] [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        [Sub(SurfaceInputs)] _BumpScale("Normal Scale", Range(0, 2)) = 1

        [Main(TextureBlend, _TEXTURE_BLEND_ON, on)] _TextureBlendEnabled("Texture Blend", Float) = 0
        [Title(TextureBlend, _)] [Tex(TextureBlend)] [NoScaleOffset] _BlendMap("Blend Map", 2D) = "white" {}
        [UberVector2(TextureBlend)] _BlendTiling("Blend Tiling", Vector) = (1, 1, 0, 0)
        [Sub(TextureBlend)] _BlendColor("Blend Color", Color) = (1, 1, 1, 1)
        [Sub(TextureBlend)] _BlendThreshold("Upward Threshold", Range(-1, 1)) = 0.6
        [Sub(TextureBlend)] _BlendSmoothness("Blend Smoothness", Range(0.01, 1)) = 0.25

        [Main(ColorAdjust, _COLOR_ADJUST_ON, on)] _ColorAdjustEnabled("Color Adjustment", Float) = 0
        [Title(ColorAdjust, _)] [Sub(ColorAdjust)] _HueShift("Hue Shift", Range(-180, 180)) = 0
        [Sub(ColorAdjust)] _Saturation("Saturation", Range(0, 2)) = 1
        [Sub(ColorAdjust)] _Brightness("Brightness", Range(0, 2)) = 1
        [Sub(ColorAdjust)] _Contrast("Contrast", Range(0, 2)) = 1

        [Main(Emission, _EMISSION, on)] _EmissionEnabled("Emission", Float) = 0
        [Title(Emission, _)] [Tex(Emission)] [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "white" {}
        [Sub(Emission)] [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        [Sub(Emission)] _EmissionIntensity("Emission Intensity", Range(0, 16)) = 1

        [Main(Rim, _RIM_ON, on)] _RimEnabled("Fresnel Rim", Float) = 0
        [Title(Rim, _)] [Sub(Rim)] [HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        [Sub(Rim)] _RimPower("Rim Power", Range(0.1, 16)) = 4
        [Sub(Rim)] _RimIntensity("Rim Intensity", Range(0, 16)) = 1

        [Main(HeightFade, _HEIGHT_FADE_ON, on)] _HeightFadeEnabled("World Height Fade", Float) = 0
        [Title(HeightFade, _)] [Sub(HeightFade)] _HeightFadeLower("Lower Height", Float) = 0
        [Sub(HeightFade)] _HeightFadeUpper("Upper Height", Float) = 1
        [Sub(HeightFade)] _HeightFadeOffset("Height Offset", Float) = 0
        [Sub(HeightFade)] _HeightFadeColor("Lower Color", Color) = (0.25, 0.25, 0.25, 1)

        [Main(GlassGlow, _GLASS_GLOW_ON, on)] _GlassGlowEnabled("Glass Glow", Float) = 0
        [Title(GlassGlow, _)] [Sub(GlassGlow)] [HDR] _GlassGlowColor("Glow Color", Color) = (1, 1, 1, 1)
        [Sub(GlassGlow)] _GlassGlowThreshold("Luminance Threshold", Range(0, 1)) = 0.5
        [Sub(GlassGlow)] _GlassGlowIntensity("Glow Intensity", Range(0, 16)) = 1

        [Main(Hologram, _HOLOGRAM_ON, on)] _HologramEnabled("Hologram", Float) = 0
        [Title(Hologram, _)] [Sub(Hologram)] [HDR] _HologramColor("Color", Color) = (0, 1, 1, 1)
        [Sub(Hologram)] _HologramOpacity("Opacity", Range(0, 1)) = 0.35
        [Sub(Hologram)] _HologramFresnelPower("Fresnel Power", Range(0.1, 16)) = 4
        [Sub(Hologram)] _HologramFresnelIntensity("Fresnel Intensity", Range(0, 16)) = 2
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
        [KWEnum(Glitch, View, _, Object, _GLITCH_OBJECT_SPACE, World, _GLITCH_WORLD_SPACE)] _GlitchSpace("Space", Float) = 0
        [UberVector3(Glitch)] _GlitchUpVector("Up Vector", Vector) = (0, 1, 0, 0)
        [UberMinMaxVector(Glitch)] _GlitchBandSizeRange("Band Size Range (Pixels)", Vector) = (4, 12, 0, 0)

        [Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled("Dissolve", Float) = 0
        [Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap("Noise Map", 2D) = "white" {}
        [Sub(Dissolve)] _DissolveTilingOffset("Tiling XY / Offset ZW", Vector) = (1, 1, 0, 0)
        [Sub(Dissolve)] _DissolvePanning("Panning XY", Vector) = (0, 0, 0, 0)
        [KWEnum(Dissolve, UV, _, ObjectSpace, _DISSOLVE_OBJECT_SPACE)] _DissolveSpace("Space", Float) = 0
        [Sub(Dissolve)] _DissolveAmount("Amount", Range(0, 1)) = 0
        [UberVector2(Dissolve)] _DissolveNoiseAmountMovement("Noise Movement", Vector) = (1, 0, 0, 0)
        [UberVector3(Dissolve)] _DissolveObjectUpVector("Up Vector", Vector) = (0, 1, 0, 0)
        [UberMinMaxVector(Dissolve)] _DissolveObjectRange("Range", Vector) = (0, 1, 0, 0)
        [Sub(Dissolve)] _DissolveObjectNoiseScale("Object Noise Size", Range(0.05, 4)) = 1
        [Sub(Dissolve)] _DissolveObjectNoiseStrength("Object Noise Strength", Range(0, 1)) = 0.15
        [Sub(Dissolve)] _DissolveEdgeWidth("Edge Width", Range(0, 1)) = 0.05
        [Sub(Dissolve)] [HDR] _DissolveEdgeColor("Edge Color", Color) = (1, 0.5, 0, 1)
        [Sub(Dissolve)] _DissolveEdgeIntensity("Edge Intensity", Range(0, 16)) = 1

        [Main(DitherFade, _DITHER_FADE_ON, on)] _DitherFadeEnabled("Dither Fade", Float) = 0
        [Title(DitherFade, _)] [Sub(DitherFade)] _DitherFade("Visibility", Range(0, 1)) = 1

        [Main(StencilOutline, _STENCIL_OUTLINE_ON, on)] _StencilOutlineEnabled("Stencil Outline", Float) = 0
        [Title(StencilOutline, _)] [Sub(StencilOutline)] [HDR] _StencilOutlineColor("Outline Color", Color) = (1, 0.72, 0.08, 1)
        [Sub(StencilOutline)] _StencilOutlineWidth("Width (Pixels)", Range(0, 16)) = 2

        [Main(Wobble, _WOBBLE_ON, on)] _WobbleEnabled("Wobble", Float) = 0
        [Title(Wobble, _)] [Sub(Wobble)] _WobbleAmplitude("Amplitude", Range(0, 0.5)) = 0
        [Sub(Wobble)] _WobbleHeight("Bottom Height", Range(0.01, 1)) = 0.35
        [Sub(Wobble)] _WobbleHalfHeight("Mesh Half Height (Object)", Float) = 0.5
        [Sub(Wobble)] _WobbleFrequency("Frequency", Range(0, 30)) = 6
        [Sub(Wobble)] _WobbleWaves("Waves Along Axis", Range(0, 6)) = 1.5
        [UberVector3(Wobble)] _WobbleAxis("Up Vector", Vector) = (0, 1, 0, 0)

        [HideInInspector] _SrcBlend("__src", Float) = 1
        [HideInInspector] _DstBlend("__dst", Float) = 0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0
        [HideInInspector] _ZWrite("__zwrite", Float) = 1
        [HideInInspector] _BlendModePreserveSpecular("__preserveSpecular", Float) = 1
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0
        [HideInInspector] [NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector] [NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector] [NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
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
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
                WriteMask [_StencilOutlineEnabled]
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberForwardVertex
            #pragma fragment UberForwardFragment

            #pragma multi_compile_local _ _NORMALMAP
            #pragma multi_compile_local _ _BASE_MAP_TRIPLANAR
            #pragma multi_compile_local_fragment _ _METALLICMAP
            #pragma multi_compile_local_fragment _ _ROUGHNESSMAP
            #pragma multi_compile_local_fragment _ _TEXTURE_BLEND_ON
            #pragma multi_compile_local_fragment _ _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma multi_compile_local_fragment _ _UNLIT_ON
            #pragma multi_compile_local _ _RECEIVE_SHADOWS_OFF
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW

            #pragma shader_feature_local_fragment _ _COLOR_ADJUST_ON
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _RIM_ON
            #pragma shader_feature_local_fragment _ _HEIGHT_FADE_ON
            #pragma shader_feature_local_fragment _ _GLASS_GLOW_ON
            #pragma shader_feature_local_fragment _ _HOLOGRAM_ON
            #pragma shader_feature_local_fragment _ _HOLOGRAM_WORLD_SPACE _HOLOGRAM_SCREEN_SPACE
            #pragma shader_feature_local _ _GLITCH_ON
            #pragma shader_feature_local_vertex _ _WOBBLE_ON
            #pragma multi_compile_local _ _GLITCH_OBJECT_SPACE _GLITCH_WORLD_SPACE
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_OBJECT_SPACE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define UBER_FORWARD_PASS
            #include "Uber3D.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "StencilOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front
            Stencil
            {
                Ref 1
                ReadMask 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberOutlineVertex
            #pragma fragment UberOutlineFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma multi_compile_local _ _BASE_MAP_TRIPLANAR
            #pragma shader_feature_local_fragment _ _STENCIL_OUTLINE_ON
            #pragma shader_feature_local _ _GLITCH_ON
            #pragma shader_feature_local_vertex _ _WOBBLE_ON
            #pragma multi_compile_local _ _GLITCH_OBJECT_SPACE _GLITCH_WORLD_SPACE
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_OBJECT_SPACE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _HEIGHT_FADE_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #define UBER_OUTLINE_PASS
            #include "Uber3D.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberShadowVertex
            #pragma fragment UberSilhouetteFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma multi_compile_local _ _BASE_MAP_TRIPLANAR
            #pragma shader_feature_local _ _GLITCH_ON
            #pragma shader_feature_local_vertex _ _WOBBLE_ON
            #pragma multi_compile_local _ _GLITCH_OBJECT_SPACE _GLITCH_WORLD_SPACE
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_OBJECT_SPACE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _HEIGHT_FADE_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #define UBER_SHADOW_PASS
            #include "Uber3D.hlsl"
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
            #pragma target 3.5
            #pragma vertex UberDepthVertex
            #pragma fragment UberSilhouetteFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma multi_compile_local _ _BASE_MAP_TRIPLANAR
            #pragma shader_feature_local _ _GLITCH_ON
            #pragma shader_feature_local_vertex _ _WOBBLE_ON
            #pragma multi_compile_local _ _GLITCH_OBJECT_SPACE _GLITCH_WORLD_SPACE
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_OBJECT_SPACE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _HEIGHT_FADE_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #define UBER_DEPTH_PASS
            #include "Uber3D.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberDepthNormalsVertex
            #pragma fragment UberDepthNormalsFragment
            #pragma multi_compile_local _ _NORMALMAP
            #pragma multi_compile_local _ _BASE_MAP_TRIPLANAR
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _UBER_QUALITY_LOW
            #pragma shader_feature_local _ _GLITCH_ON
            #pragma shader_feature_local_vertex _ _WOBBLE_ON
            #pragma multi_compile_local _ _GLITCH_OBJECT_SPACE _GLITCH_WORLD_SPACE
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_OBJECT_SPACE
            #pragma shader_feature_local_fragment _ _DITHER_FADE_ON
            #pragma shader_feature_local_fragment _ _HEIGHT_FADE_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #define UBER_DEPTH_NORMALS_PASS
            #include "Uber3D.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberMetaVertex
            #pragma fragment UberMetaFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _UNLIT_ON
            #pragma multi_compile_local _ _BASE_MAP_TRIPLANAR
            #pragma multi_compile_local_fragment _ _METALLICMAP
            #pragma multi_compile_local_fragment _ _ROUGHNESSMAP
            #pragma multi_compile_local_fragment _ _TEXTURE_BLEND_ON
            #pragma shader_feature_local_fragment _ _COLOR_ADJUST_ON
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature EDITOR_VISUALIZATION
            #define UBER_META_PASS
            #include "Uber3D.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UberShaderGUI"
}
