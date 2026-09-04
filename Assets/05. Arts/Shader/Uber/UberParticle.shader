Shader "Shader/Uber/Particle"
{
    Properties
    {
        [Main(Surface, _, on, off)] _SurfaceOptions("Surface", Float) = 1
        [Title(Surface, _)] [KWEnum(Surface, Opaque, _, Transparent, _SURFACE_TYPE_TRANSPARENT)] _Surface("Surface Type", Float) = 1
        [KWEnum(Surface, Alpha, _, Premultiply, _ALPHAPREMULTIPLY_ON, Additive, _, Multiply, _ALPHAMODULATE_ON)] _Blend("Blend Mode", Float) = 0
        [SubToggle(Surface, _ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        [Sub(Surface_ALPHATEST_ON)] _Cutoff("Threshold", Range(0, 1)) = 0.5
        [KWEnum(Surface, Off, _, Front, _, Back, _)] _Cull("Render Face", Float) = 0
        [KWEnum(Surface, Auto, _, Custom, _)] _QueueControl("Queue Control", Float) = 0
        [Sub(Surface)] _QueueOffset("Queue Offset", Range(-50, 50)) = 0
        [Sub(Surface)] _ZTest("Z Test (Compare Function)", Range(0, 8)) = 4
        [Sub(Surface)] _ColorMask("Color Mask (None 0 / RGB 7 / Alpha 8 / All 15)", Range(0, 15)) = 15
        [Sub(Surface)] _StencilRef("Stencil Reference", Range(0, 255)) = 0
        [Sub(Surface)] _StencilReadMask("Stencil Read Mask", Range(0, 255)) = 255
        [Sub(Surface)] _StencilWriteMask("Stencil Write Mask", Range(0, 255)) = 255
        [Sub(Surface)] _StencilComp("Stencil Comparison", Range(0, 8)) = 8
        [Sub(Surface)] _StencilPass("Stencil Pass", Range(0, 7)) = 0

        [Main(Base, _, on, off)] _BaseOptions("Base", Float) = 1
        [Title(Base, _)] [Tex(Base)] [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [Sub(Base)] [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [UberVector2(Base)] _BasePanning("UV Panning", Vector) = (0, 0, 0, 0)
        [Sub(Base)] _AlphaMultiplier("Alpha Multiplier", Range(0, 4)) = 1
        [Sub(Base)] _AlphaPower("Alpha Power", Range(0.01, 8)) = 1
        [Sub(Base)] _AlphaBias("Alpha Bias", Range(-1, 1)) = 0
        [SubToggle(Base, _FLIPBOOKBLENDING_ON)] _FlipbookBlending("Flipbook Blending", Float) = 0

        [Main(BaseNoiseClip, _, on, on)] _BaseNoiseClipEnabled("Base Noise Clip", Float) = 0
        [Title(BaseNoiseClip, _)] [UberParticleStream(BaseNoiseClip)] _BaseNoiseClipStream("Normalized Clip Threshold Stream", Float) = 2
        [UberParticleNoiseChannel(BaseNoiseClip)] _BaseNoiseClipChannel("Noise Channel", Float) = 0
        [UberParticleCurve(BaseNoiseClip)] _BaseNoiseClipCurveValues("Threshold Curve", Vector) = (0, 1, 1, 1)
        [HideInInspector] _BaseNoiseClipCurveTimes("Base Noise Clip Curve Times", Vector) = (0, 1, 1, 1)
        [HideInInspector] _BaseNoiseClipCurveInTangents("Base Noise Clip Curve In Tangents", Vector) = (1, 1, 1, 1)
        [HideInInspector] _BaseNoiseClipCurveOutTangents("Base Noise Clip Curve Out Tangents", Vector) = (1, 1, 1, 1)
        [HideInInspector] _BaseNoiseClipCurveMetadata("Base Noise Clip Curve Metadata", Vector) = (2, 0, 0, 0)

        [Main(LifetimeGradient, _, on, on)] _LifetimeGradientEnabled("Lifetime HDR Gradient", Float) = 0
        [Title(LifetimeGradient, _)] [UberParticleStream(LifetimeGradient)] _LifetimeStream("Normalized Lifetime Stream", Float) = 2
        [UberGradient(LifetimeGradient)] _LifetimeGradientColor0("HDR Gradient", Vector) = (1, 1, 1, 0)
        [HideInInspector] _LifetimeGradientColor1("Lifetime Gradient Color 1", Vector) = (1, 1, 1, 1)
        [HideInInspector] _LifetimeGradientColor2("Lifetime Gradient Color 2", Vector) = (1, 1, 1, 1)
        [HideInInspector] _LifetimeGradientColor3("Lifetime Gradient Color 3", Vector) = (1, 1, 1, 1)
        [HideInInspector] _LifetimeGradientAlphas("Lifetime Gradient Alphas", Vector) = (1, 1, 1, 1)
        [HideInInspector] _LifetimeGradientAlphaTimes("Lifetime Gradient Alpha Times", Vector) = (0, 1, 1, 1)
        [HideInInspector] _LifetimeGradientMetadata("Lifetime Gradient Metadata", Vector) = (2, 2, 0, 0)

        [Main(Fading, _, on, off)] _FadingOptions("Fading", Float) = 1
        [Title(Fading, _)] [SubToggle(Fading, _SOFTPARTICLES_ON)] _SoftParticlesEnabled("Soft Particles", Float) = 0
        [Sub(Fading_SOFTPARTICLES_ON)] _SoftParticlesNearFadeDistance("Near Distance", Float) = 0
        [Sub(Fading_SOFTPARTICLES_ON)] _SoftParticlesFarFadeDistance("Far Distance", Float) = 1
        [SubToggle(Fading, _FADING_ON)] _CameraFadingEnabled("Camera Fade", Float) = 0
        [Sub(Fading_FADING_ON)] _CameraNearFadeDistance("Near Distance", Float) = 1
        [Sub(Fading_FADING_ON)] _CameraFarFadeDistance("Far Distance", Float) = 2

        [Main(Mask, _MASK_ON, on)] _MaskEnabled("Mask", Float) = 0
        [Title(Mask, _)] [Tex(Mask)] _MaskMap("Mask Map", 2D) = "white" {}
        [UberVector2(Mask)] _MaskPanning("UV Panning", Vector) = (0, 0, 0, 0)
        [Sub(Mask)] _MaskChannel("Channel (R 0 / G 1 / B 2 / A 3)", Range(0, 3)) = 0
        [SubToggle(Mask)] _MaskInvert("Invert", Float) = 0
        [Sub(Mask)] _MaskStrength("Strength", Range(0, 1)) = 1

        [Main(UVDistortion, _UV_DISTORTION_ON, on)] _UVDistortionEnabled("UV Distortion", Float) = 0
        [Title(UVDistortion, _)] [Tex(UVDistortion)] _UVDistortionMap("Flow Map", 2D) = "gray" {}
        [UberVector2(UVDistortion)] _UVDistortionDirection("Flow Direction", Vector) = (1, 0, 0, 0)
        [Sub(UVDistortion)] _UVDistortionStrength("Strength", Range(-1, 1)) = 0
        [Sub(UVDistortion)] _UVDistortionSpeed("Speed", Range(-10, 10)) = 1

        [Main(Dissolve, _DISSOLVE_ON, on)] _DissolveEnabled("Dissolve", Float) = 0
        [Title(Dissolve, _)] [Tex(Dissolve)] [NoScaleOffset] _DissolveNoiseMap("Noise Map", 2D) = "white" {}
        [Sub(Dissolve)] _DissolveTilingOffset("Tiling XY / Offset ZW", Vector) = (1, 1, 0, 0)
        [UberVector2(Dissolve)] _DissolvePanning("Panning", Vector) = (0, 0, 0, 0)
        [KWEnum(Dissolve, Noise, _, Radial, _DISSOLVE_RADIAL, Swipe, _DISSOLVE_SWIPE)] _DissolveMode("Mode", Float) = 0
        [Sub(Dissolve)] _DissolveAmount("Amount", Range(0, 1)) = 0
        [UberMinMaxVector(Dissolve_)] _DissolveNoiseRange("Noise Range", Vector) = (0, 1, 0, 0)
        [UberVector2(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialCenter("Radial Center", Vector) = (0.5, 0.5, 0, 0)
        [UberMinMaxVector(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialRange("Radial Range", Vector) = (0, 0.707, 0, 0)
        [Sub(Dissolve_DISSOLVE_RADIAL)] _DissolveRadialNoiseStrength("Radial Noise", Range(0, 1)) = 0
        [UberVector2(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeCenter("Swipe Center", Vector) = (0.5, 0.5, 0, 0)
        [UberMinMaxVector(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRange("Swipe Range", Vector) = (-0.707, 0.707, 0, 0)
        [Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeRotation("Swipe Rotation", Range(-180, 180)) = 0
        [Sub(Dissolve_DISSOLVE_SWIPE)] _DissolveSwipeNoiseStrength("Swipe Noise", Range(0, 1)) = 0
        [Sub(Dissolve)] _DissolveEdgeWidth("Edge Width", Range(0, 1)) = 0.1
        [Sub(Dissolve)] [HDR] _DissolveEdgeColor0("Edge Inner Color", Color) = (1, 0.25, 0, 1)
        [Sub(Dissolve)] [HDR] _DissolveEdgeColor1("Edge Outer Color", Color) = (1, 1, 0, 1)
        [Sub(Dissolve)] _DissolveEdgeEmission("Edge Emission", Range(0, 16)) = 1

        [Main(ColorAdjust, _COLOR_ADJUST_ON, on)] _ColorAdjustEnabled("Color Adjustment", Float) = 0
        [Title(ColorAdjust, _)] [Sub(ColorAdjust)] _HueShift("Hue Shift", Range(-180, 180)) = 0
        [Sub(ColorAdjust)] _Saturation("Saturation", Range(0, 2)) = 1
        [Sub(ColorAdjust)] _Brightness("Brightness", Range(0, 2)) = 1
        [Sub(ColorAdjust)] _Contrast("Contrast", Range(0, 2)) = 1

        [Main(Emission, _EMISSION, on)] _EmissionEnabled("Emission", Float) = 0
        [Title(Emission, _)] [Tex(Emission)] _EmissionMap("Emission Map", 2D) = "white" {}
        [Sub(Emission)] [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        [Sub(Emission)] _EmissionIntensity("Intensity", Range(0, 16)) = 1

        [Main(Rim, _RIM_ON, on)] _RimEnabled("Fresnel Rim", Float) = 0
        [Title(Rim, _)] [KWEnum(Rim, Geometry Normal, _, Radial UV, _RIM_RADIAL_UV)] _RimMode("Mode", Float) = 0
        [Sub(Rim)] [HDR] _RimColor("Color", Color) = (1, 1, 1, 1)
        [Sub(Rim)] _RimPower("Power", Range(0.1, 16)) = 4
        [Sub(Rim)] _RimIntensity("Intensity", Range(0, 16)) = 1
        [UberVector2(Rim_RIM_RADIAL_UV)] _RimRadialCenter("Radial Center", Vector) = (0.5, 0.5, 0, 0)

        [Main(VertexOffset, _VERTEX_OFFSET_ON, on)] _VertexOffsetEnabled("Vertex Offset", Float) = 0
        [Title(VertexOffset, _)] [Sub(VertexOffset)] _VertexOffsetDirection("Direction", Vector) = (0, 1, 0, 0)
        [Sub(VertexOffset)] _VertexOffsetAmplitude("Amplitude", Float) = 0
        [Sub(VertexOffset)] _VertexOffsetFrequency("Frequency", Float) = 1
        [Sub(VertexOffset)] _VertexOffsetSpeed("Speed", Float) = 1

        [Main(CustomData, _CUSTOM_DATA_ON, on)] _CustomDataEnabled("Custom Data", Float) = 0
        [Title(CustomData, _)] [Sub(CustomData)] _CustomDissolveWeight("Custom1 X / Dissolve", Range(0, 1)) = 0
        [Sub(CustomData)] _CustomEmissionWeight("Custom1 Y / Emission", Range(0, 1)) = 0
        [Sub(CustomData)] _CustomUVDistortionWeight("Custom1 Z / UV Distortion", Range(0, 1)) = 0
        [Sub(CustomData)] _CustomVertexOffsetWeight("Custom1 W / Vertex Offset", Range(0, 1)) = 0

        // URP BaseShaderGUI render-state contract.
        [HideInInspector] _SrcBlend("__src", Float) = 5
        [HideInInspector] _DstBlend("__dst", Float) = 10
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 10
        [HideInInspector] _ZWrite("__zwrite", Float) = 0
        [HideInInspector] _BlendModePreserveSpecular("__preserveSpecular", Float) = 0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0
    }

    HLSLINCLUDE
    // Procedural particle instancing writes matrix constant buffers.
    #pragma never_use_dxc
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "PerformanceChecks" = "False"
        }
        LOD 100

        Stencil
        {
            Ref [_StencilRef]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
            Comp [_StencilComp]
            Pass [_StencilPass]
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            ColorMask [_ColorMask]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberParticleForwardVertex
            #pragma fragment UberParticleForwardFragment
            #pragma multi_compile_local_fragment _ _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _ _SOFTPARTICLES_ON
            #pragma shader_feature_local _ _FADING_ON
            #pragma shader_feature_local_fragment _ _MASK_ON
            #pragma shader_feature_local_fragment _ _UV_DISTORTION_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _COLOR_ADJUST_ON
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _RIM_ON
            #pragma shader_feature_local_fragment _ _RIM_RADIAL_UV
            #pragma shader_feature_local_vertex _ _VERTEX_OFFSET_ON
            #pragma shader_feature_local _ _CUSTOM_DATA_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #define UBER_PARTICLE_COLOR_PASS 1
            #include_with_pragmas "UberParticle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            ColorMask [_ColorMask]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberParticleForwardVertex
            #pragma fragment UberParticleForwardFragment
            #pragma multi_compile_local_fragment _ _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local _ _SOFTPARTICLES_ON
            #pragma shader_feature_local _ _FADING_ON
            #pragma shader_feature_local_fragment _ _MASK_ON
            #pragma shader_feature_local_fragment _ _UV_DISTORTION_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_fragment _ _COLOR_ADJUST_ON
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _RIM_ON
            #pragma shader_feature_local_fragment _ _RIM_RADIAL_UV
            #pragma shader_feature_local_vertex _ _VERTEX_OFFSET_ON
            #pragma shader_feature_local _ _CUSTOM_DATA_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #define UBER_PARTICLE_COLOR_PASS 1
            #include_with_pragmas "UberParticle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ZTest [_ZTest]
            Cull [_Cull]
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberParticleSilhouetteVertex
            #pragma fragment UberParticleDepthFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _ _MASK_ON
            #pragma shader_feature_local_fragment _ _UV_DISTORTION_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_vertex _ _VERTEX_OFFSET_ON
            #pragma shader_feature_local _ _CUSTOM_DATA_ON
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "UberParticle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On
            ZTest [_ZTest]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberParticleSilhouetteVertex
            #pragma fragment UberParticleDepthNormalsFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _ _MASK_ON
            #pragma shader_feature_local_fragment _ _UV_DISTORTION_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_vertex _ _VERTEX_OFFSET_ON
            #pragma shader_feature_local _ _CUSTOM_DATA_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "UberParticle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }
            Blend One Zero
            ZWrite On
            ZTest LEqual
            Cull Off
            Stencil
            {
                Ref 0
                ReadMask 255
                WriteMask 255
                Comp Always
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberParticleSilhouetteVertex
            #pragma fragment UberParticleSceneSelectionFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _ _MASK_ON
            #pragma shader_feature_local_fragment _ _UV_DISTORTION_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_vertex _ _VERTEX_OFFSET_ON
            #pragma shader_feature_local _ _CUSTOM_DATA_ON
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "UberParticle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ScenePickingPass"
            Tags { "LightMode" = "Picking" }
            Blend One Zero
            ZWrite On
            ZTest LEqual
            Cull Off
            Stencil
            {
                Ref 0
                ReadMask 255
                WriteMask 255
                Comp Always
                Pass Keep
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UberParticleSilhouetteVertex
            #pragma fragment UberParticleScenePickingFragment
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma shader_feature_local _ _FLIPBOOKBLENDING_ON
            #pragma shader_feature_local_fragment _ _MASK_ON
            #pragma shader_feature_local_fragment _ _UV_DISTORTION_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _DISSOLVE_RADIAL _DISSOLVE_SWIPE
            #pragma shader_feature_local_vertex _ _VERTEX_OFFSET_ON
            #pragma shader_feature_local _ _CUSTOM_DATA_ON
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ParticleInstancingSetup
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "UberParticle.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UberShaderGUI"
}
