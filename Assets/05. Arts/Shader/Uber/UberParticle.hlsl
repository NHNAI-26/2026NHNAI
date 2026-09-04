#ifndef UBER_PARTICLE_INCLUDED
#define UBER_PARTICLE_INCLUDED

#include "UberCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticlesInstancing.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BasePanning;
    float4 _MaskMap_ST;
    float4 _MaskPanning;
    float4 _UVDistortionMap_ST;
    float4 _UVDistortionDirection;
    float4 _DissolveTilingOffset;
    float4 _DissolvePanning;
    float4 _DissolveNoiseRange;
    float4 _DissolveRadialCenter;
    float4 _DissolveRadialRange;
    float4 _DissolveSwipeCenter;
    float4 _DissolveSwipeRange;
    float4 _EmissionMap_ST;
    float4 _RimRadialCenter;
    float4 _VertexOffsetDirection;
    float4 _BaseNoiseClipCurveValues;
    float4 _BaseNoiseClipCurveTimes;
    float4 _BaseNoiseClipCurveInTangents;
    float4 _BaseNoiseClipCurveOutTangents;
    float4 _BaseNoiseClipCurveMetadata;
    float4 _LifetimeGradientColor0;
    float4 _LifetimeGradientColor1;
    float4 _LifetimeGradientColor2;
    float4 _LifetimeGradientColor3;
    float4 _LifetimeGradientAlphas;
    float4 _LifetimeGradientAlphaTimes;
    float4 _LifetimeGradientMetadata;
    half4 _BaseColor;
    half4 _DissolveEdgeColor0;
    half4 _DissolveEdgeColor1;
    half4 _EmissionColor;
    half4 _RimColor;
    half _SurfaceOptions;
    half _Surface;
    half _Blend;
    half _AlphaClip;
    half _Cutoff;
    half _Cull;
    half _QueueControl;
    half _QueueOffset;
    half _ZTest;
    half _ColorMask;
    half _StencilRef;
    half _StencilReadMask;
    half _StencilWriteMask;
    half _StencilComp;
    half _StencilPass;
    half _BaseOptions;
    half _AlphaMultiplier;
    half _AlphaPower;
    half _AlphaBias;
    half _FlipbookBlending;
    half _BaseNoiseClipEnabled;
    half _BaseNoiseClipChannel;
    float _BaseNoiseClipStream;
    half _LifetimeGradientEnabled;
    float _LifetimeStream;
    half _FadingOptions;
    half _SoftParticlesEnabled;
    float _SoftParticlesNearFadeDistance;
    float _SoftParticlesFarFadeDistance;
    half _CameraFadingEnabled;
    float _CameraNearFadeDistance;
    float _CameraFarFadeDistance;
    half _MaskEnabled;
    half _MaskChannel;
    half _MaskInvert;
    half _MaskStrength;
    half _UVDistortionEnabled;
    half _UVDistortionStrength;
    half _UVDistortionSpeed;
    half _DissolveEnabled;
    half _DissolveMode;
    half _DissolveAmount;
    half _DissolveRadialNoiseStrength;
    float _DissolveSwipeRotation;
    half _DissolveSwipeNoiseStrength;
    half _DissolveEdgeWidth;
    half _DissolveEdgeEmission;
    half _ColorAdjustEnabled;
    half _HueShift;
    half _Saturation;
    half _Brightness;
    half _Contrast;
    half _EmissionEnabled;
    half _EmissionIntensity;
    half _RimEnabled;
    half _RimMode;
    half _RimPower;
    half _RimIntensity;
    half _VertexOffsetEnabled;
    float _VertexOffsetAmplitude;
    float _VertexOffsetFrequency;
    float _VertexOffsetSpeed;
    half _CustomDataEnabled;
    half _CustomDissolveWeight;
    half _CustomEmissionWeight;
    half _CustomUVDistortionWeight;
    half _CustomVertexOffsetWeight;
    half _SrcBlend;
    half _DstBlend;
    half _SrcBlendAlpha;
    half _DstBlendAlpha;
    half _ZWrite;
    half _BlendModePreserveSpecular;
    half _AlphaToMask;
CBUFFER_END

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
TEXTURE2D(_UVDistortionMap);
SAMPLER(sampler_UVDistortionMap);
TEXTURE2D(_DissolveNoiseMap);
SAMPLER(sampler_DissolveNoiseMap);
struct UberParticleAttributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    // Custom Vertex Streams pack values sequentially across TEXCOORD0..3.
    // UV + AgePercent -> TEXCOORD0.xy/z (the default lifetime layout).
    // UV/UV2 -> TEXCOORD0.xy/zw.
    // AnimBlend -> TEXCOORD1.x; Custom1.xyz -> TEXCOORD1.yzw.
    // Custom1.w -> TEXCOORD2.x.
    // UV -> TEXCOORD0.xy; Custom1.xy -> TEXCOORD0.zw.
    // Custom1.zw -> TEXCOORD1.xy.
    float4 texcoords : TEXCOORD0;
    float4 streamTexcoord1 : TEXCOORD1;
    float4 streamTexcoord2 : TEXCOORD2;
    float4 streamTexcoord3 : TEXCOORD3;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// The name and core fields intentionally match URP's particle helpers.
struct VaryingsParticle
{
    float4 clipPos : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    half4 color : COLOR;
    float4 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    float2 rawUV : TEXCOORD3;
    float2 normalizedControlStreams : TEXCOORD4;
#if defined(_FLIPBOOKBLENDING_ON)
    float3 texcoord2AndBlend : TEXCOORD5;
#endif
#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON)
    float4 projectedPosition : TEXCOORD6;
#endif
#if defined(_CUSTOM_DATA_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    float4 custom1 : TEXCOORD7;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#if defined(UBER_PARTICLE_COLOR_PASS)
    // Only color passes import URP's depth texture declarations.
    #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"
#else
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

    // URP particle helper subset for non-color silhouette passes.
    half4 GetParticleColor(half4 color)
    {
    #if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        #if !defined(UNITY_PARTICLE_INSTANCE_DATA_NO_COLOR)
            UNITY_PARTICLE_INSTANCE_DATA data =
                unity_ParticleInstanceData[unity_InstanceID];
            color = lerp(half4(1.0h, 1.0h, 1.0h, 1.0h), color,
                unity_ParticleUseMeshColors);
            color *= half4(UnpackFromR8G8B8A8(data.color));
        #endif
    #endif
        return color;
    }

    void GetParticleTexcoords(out float2 outputTexcoord,
        out float3 outputTexcoord2AndBlend, float4 inputTexcoords,
        float inputBlend)
    {
    #if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        if (unity_ParticleUVShiftData.x != 0.0)
        {
            UNITY_PARTICLE_INSTANCE_DATA data =
                unity_ParticleInstanceData[unity_InstanceID];
            float tilesX = unity_ParticleUVShiftData.y;
            float2 scale = unity_ParticleUVShiftData.zw;
            #if defined(UNITY_PARTICLE_INSTANCE_DATA_NO_ANIM_FRAME)
                float sheetIndex = 0.0;
            #else
                float sheetIndex = data.animFrame;
            #endif
            float index0 = floor(sheetIndex);
            float row0 = floor(index0 / tilesX);
            float column0 = floor(index0 - row0 * tilesX);
            float2 offset0 = float2(column0 * scale.x,
                (1.0 - scale.y) - row0 * scale.y);
            outputTexcoord = inputTexcoords.xy * scale + offset0;
            #if defined(_FLIPBOOKBLENDING_ON)
                float index1 = floor(sheetIndex + 1.0);
                float row1 = floor(index1 / tilesX);
                float column1 = floor(index1 - row1 * tilesX);
                float2 offset1 = float2(column1 * scale.x,
                    (1.0 - scale.y) - row1 * scale.y);
                outputTexcoord2AndBlend.xy =
                    inputTexcoords.xy * scale + offset1;
                outputTexcoord2AndBlend.z = frac(sheetIndex);
            #endif
        }
        else
    #endif
        {
            outputTexcoord = inputTexcoords.xy;
        #if defined(_FLIPBOOKBLENDING_ON)
            outputTexcoord2AndBlend.xy = inputTexcoords.zw;
            outputTexcoord2AndBlend.z = inputBlend;
        #endif
        }

    #if !defined(_FLIPBOOKBLENDING_ON)
        outputTexcoord2AndBlend = float3(inputTexcoords.xy, 0.5);
    #endif
    }

    void GetParticleTexcoords(out float2 outputTexcoord, float2 inputTexcoord)
    {
        float3 unused = 0.0;
        GetParticleTexcoords(outputTexcoord, unused, inputTexcoord.xyxy, 0.0);
    }
#endif

float _ObjectId;
float _PassValue;
float4 _SelectionID;

struct UberParticleSilhouette
{
    half4 color;
    half dissolveEdge;
    half4 dissolveEdgeColor;
};

inline float2 UberParticleTransformUV(float2 uv)
{
    return uv * _BaseMap_ST.xy + _BaseMap_ST.zw + _BasePanning.xy * _Time.y;
}

inline half UberParticleRemapAlpha(half alpha)
{
    half power = max(_AlphaPower, 0.0001h);
    return saturate(pow(saturate(alpha), power) * max(_AlphaMultiplier, 0.0h) +
        _AlphaBias);
}

inline float4 UberParticleReadCustom1(UberParticleAttributes input)
{
#if defined(_CUSTOM_DATA_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    #if defined(_FLIPBOOKBLENDING_ON)
        return float4(input.streamTexcoord1.yzw, input.streamTexcoord2.x);
    #else
        return float4(input.texcoords.zw, input.streamTexcoord1.xy);
    #endif
#else
    // Procedural particle instances do not expose the documented Custom1 stream.
    return 0.0;
#endif
}

inline float UberParticleReadNormalizedStream(UberParticleAttributes input,
    float streamSelector)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    // Procedural instance data contains no Custom Vertex Stream values.
    return 0.0;
#else
    float selector = clamp(round(streamSelector), 0.0, 15.0);
    float4 stream = selector < 4.0 ? input.texcoords :
        selector < 8.0 ? input.streamTexcoord1 :
        selector < 12.0 ? input.streamTexcoord2 : input.streamTexcoord3;
    float component = fmod(selector, 4.0);
    float value = component < 0.5 ? stream.x :
        component < 1.5 ? stream.y :
        component < 2.5 ? stream.z : stream.w;
    return saturate(value);
#endif
}

inline float UberParticleReadLifetimeTime(UberParticleAttributes input)
{
    return UberParticleReadNormalizedStream(input, _LifetimeStream);
}

inline float UberParticleReadBaseNoiseClipThreshold(
    UberParticleAttributes input)
{
    return UberParticleReadNormalizedStream(input, _BaseNoiseClipStream);
}

inline float4 UberParticleGetCustom1(VaryingsParticle input)
{
#if defined(_CUSTOM_DATA_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    return input.custom1;
#else
    return 0.0;
#endif
}

inline half UberParticleCustomBlendWeight(half weight)
{
#if defined(_CUSTOM_DATA_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    return saturate(weight);
#else
    // Instanced and Custom-disabled variants retain authored material values.
    return 0.0h;
#endif
}

inline half UberParticleCustomMultiplier(half materialWeight, float component)
{
    return lerp(1.0h, max((half)component, 0.0h),
        UberParticleCustomBlendWeight(materialWeight));
}

inline float3 UberParticleSafeDirection(float3 direction, float3 fallbackAxis)
{
    float lengthSquared = dot(direction, direction);
    return lengthSquared > 0.00000001
        ? direction * rsqrt(lengthSquared)
        : fallbackAxis;
}

inline float2 UberParticleSafeDirection(float2 direction, float2 fallbackAxis)
{
    float lengthSquared = dot(direction, direction);
    return lengthSquared > 0.00000001
        ? direction * rsqrt(lengthSquared)
        : fallbackAxis;
}

inline float3 UberParticleOffsetPosition(float3 positionOS, float4 custom1)
{
#if defined(_VERTEX_OFFSET_ON)
    float3 direction = UberParticleSafeDirection(_VertexOffsetDirection.xyz,
        float3(0.0, 1.0, 0.0));
    float phase = dot(positionOS, direction) * _VertexOffsetFrequency +
        _Time.y * _VertexOffsetSpeed;
    float customScale = UberParticleCustomMultiplier(_CustomVertexOffsetWeight,
        custom1.w);
    positionOS += direction * (sin(phase) * _VertexOffsetAmplitude * customScale);
#endif
    return positionOS;
}

inline void UberParticleGetUVs(UberParticleAttributes input,
    out float2 uv, out float3 blendUV, out float2 rawUV)
{
#if defined(_FLIPBOOKBLENDING_ON)
    #if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
        GetParticleTexcoords(uv, blendUV, input.texcoords.xyxy, 0.0);
    #else
        GetParticleTexcoords(uv, blendUV, input.texcoords,
            input.streamTexcoord1.x);
    #endif
    rawUV = uv;
    blendUV.xy = UberParticleTransformUV(blendUV.xy);
#else
    GetParticleTexcoords(uv, input.texcoords.xy);
    rawUV = uv;
    blendUV = float3(uv, 0.0);
#endif
    uv = UberParticleTransformUV(uv);
}

inline half4 UberParticleEvaluateLifetimeGradient(float time)
{
    time = saturate(time);
    return UberEvaluateGradient4Keys(time, _LifetimeGradientColor0,
        _LifetimeGradientColor1, _LifetimeGradientColor2,
        _LifetimeGradientColor3, _LifetimeGradientAlphas,
        _LifetimeGradientAlphaTimes, _LifetimeGradientMetadata);
}

inline half4 UberParticleEvaluateLifetimeMultiplier(float time)
{
    half enabled = saturate(_LifetimeGradientEnabled);
    return lerp(half4(1.0h, 1.0h, 1.0h, 1.0h),
        UberParticleEvaluateLifetimeGradient(time), enabled);
}

inline float2 UberParticleDistortUV(float2 uv, float4 custom1)
{
#if defined(_UV_DISTORTION_ON)
    float2 flowDirection = UberParticleSafeDirection(
        _UVDistortionDirection.xy, float2(1.0, 0.0));
    float phase0 = frac(_Time.y * _UVDistortionSpeed);
    float phase1 = frac(phase0 + 0.5);
    float2 flowUV = uv * _UVDistortionMap_ST.xy + _UVDistortionMap_ST.zw;
    half2 flow0 = SAMPLE_TEXTURE2D(_UVDistortionMap,
        sampler_UVDistortionMap, flowUV + flowDirection * phase0).rg * 2.0h -
        1.0h;
    half2 flow1 = SAMPLE_TEXTURE2D(_UVDistortionMap,
        sampler_UVDistortionMap, flowUV + flowDirection * phase1).rg * 2.0h -
        1.0h;
    half flowBlend = abs((half)phase0 * 2.0h - 1.0h);
    half customScale = UberParticleCustomMultiplier(
        _CustomUVDistortionWeight, custom1.z);
    return uv + lerp(flow0, flow1, flowBlend) *
        (_UVDistortionStrength * customScale);
#else
    return uv;
#endif
}

inline half UberParticleReadBaseNoiseChannel(half4 color)
{
    half channel = clamp(round(_BaseNoiseClipChannel), 0.0h, 4.0h);
    return channel < 0.5h ? color.r :
        channel < 1.5h ? color.g :
        channel < 2.5h ? color.b :
        channel < 3.5h ? color.a :
        dot(color.rgb, half3(0.2126h, 0.7152h, 0.0722h));
}

inline float UberParticleEvaluateCurveSegment(float time, float time0,
    float time1, float value0, float value1, float outTangent0,
    float inTangent1)
{
    float duration = max(time1 - time0, 1e-5);
    float t = saturate((time - time0) / duration);
    float t2 = t * t;
    float t3 = t2 * t;
    float h00 = 2.0 * t3 - 3.0 * t2 + 1.0;
    float h10 = t3 - 2.0 * t2 + t;
    float h01 = -2.0 * t3 + 3.0 * t2;
    float h11 = t3 - t2;
    return h00 * value0 + h10 * duration * outTangent0 +
        h01 * value1 + h11 * duration * inTangent1;
}

inline float UberParticleEvaluateBaseNoiseClipCurve(float time)
{
    float keyCount = clamp(round(_BaseNoiseClipCurveMetadata.x), 1.0, 4.0);
    float curveTime = saturate(time);
    if (keyCount < 1.5 || curveTime <= _BaseNoiseClipCurveTimes.x)
        return saturate(_BaseNoiseClipCurveValues.x);

    if (curveTime <= _BaseNoiseClipCurveTimes.y)
        return saturate(UberParticleEvaluateCurveSegment(curveTime,
            _BaseNoiseClipCurveTimes.x, _BaseNoiseClipCurveTimes.y,
            _BaseNoiseClipCurveValues.x, _BaseNoiseClipCurveValues.y,
            _BaseNoiseClipCurveOutTangents.x,
            _BaseNoiseClipCurveInTangents.y));
    if (keyCount < 2.5)
        return saturate(_BaseNoiseClipCurveValues.y);

    if (curveTime <= _BaseNoiseClipCurveTimes.z)
        return saturate(UberParticleEvaluateCurveSegment(curveTime,
            _BaseNoiseClipCurveTimes.y, _BaseNoiseClipCurveTimes.z,
            _BaseNoiseClipCurveValues.y, _BaseNoiseClipCurveValues.z,
            _BaseNoiseClipCurveOutTangents.y,
            _BaseNoiseClipCurveInTangents.z));
    if (keyCount < 3.5)
        return saturate(_BaseNoiseClipCurveValues.z);

    if (curveTime <= _BaseNoiseClipCurveTimes.w)
        return saturate(UberParticleEvaluateCurveSegment(curveTime,
            _BaseNoiseClipCurveTimes.z, _BaseNoiseClipCurveTimes.w,
            _BaseNoiseClipCurveValues.z, _BaseNoiseClipCurveValues.w,
            _BaseNoiseClipCurveOutTangents.z,
            _BaseNoiseClipCurveInTangents.w));
    return saturate(_BaseNoiseClipCurveValues.w);
}

inline half4 UberParticleApplyBaseNoiseClip(half4 color, float threshold)
{
    half enabled = step(0.5h, _BaseNoiseClipEnabled);
    half noise = UberParticleReadBaseNoiseChannel(color);
    half curvedThreshold = (half)UberParticleEvaluateBaseNoiseClipCurve(
        threshold);
    clip(lerp(1.0h, noise - curvedThreshold, enabled));
    return lerp(color, half4(1.0h, 1.0h, 1.0h, 1.0h), enabled);
}

inline half4 UberParticleSampleCore(VaryingsParticle input, float4 custom1)
{
    float2 baseUV = UberParticleDistortUV(input.texcoord, custom1);
    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
#if defined(_FLIPBOOKBLENDING_ON)
    float2 nextUV = UberParticleDistortUV(input.texcoord2AndBlend.xy, custom1);
    half4 nextColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, nextUV);
    color = lerp(color, nextColor, saturate(input.texcoord2AndBlend.z));
#endif
    color = UberParticleApplyBaseNoiseClip(color,
        input.normalizedControlStreams.y);
    color *= _BaseColor * input.color;
    color.a = UberParticleRemapAlpha(color.a);
    return color;
}

inline half UberParticleEvaluateMask(float2 rawUV)
{
#if defined(_MASK_ON)
    float2 maskUV = rawUV * _MaskMap_ST.xy + _MaskMap_ST.zw +
        _MaskPanning.xy * _Time.y;
    half4 channels = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, maskUV);
    half channel = clamp(round(_MaskChannel), 0.0h, 3.0h);
    half mask = channel < 0.5h ? channels.r :
        channel < 1.5h ? channels.g :
        channel < 2.5h ? channels.b : channels.a;
    mask = lerp(mask, 1.0h - mask, saturate(_MaskInvert));
    return lerp(1.0h, saturate(mask), saturate(_MaskStrength));
#else
    return 1.0h;
#endif
}

inline void UberParticleEvaluateDissolve(float2 rawUV, float4 custom1,
    out half edge, out half4 edgeColor)
{
#if defined(_DISSOLVE_ON)
    float2 noiseUV = rawUV * _DissolveTilingOffset.xy +
        _DissolveTilingOffset.zw + _DissolvePanning.xy * _Time.y;
    half noise = SAMPLE_TEXTURE2D(_DissolveNoiseMap,
        sampler_DissolveNoiseMap, noiseUV).r;

    #if defined(_DISSOLVE_RADIAL)
        half radial = (half)UberSafeInverseLerp(_DissolveRadialRange.x,
            _DissolveRadialRange.y,
            length(rawUV - _DissolveRadialCenter.xy));
        half dissolveValue = saturate(radial + (noise - 0.5h) *
            saturate(_DissolveRadialNoiseStrength));
    #elif defined(_DISSOLVE_SWIPE)
        float rotation = radians(fmod(_DissolveSwipeRotation, 360.0));
        float2 direction = float2(cos(rotation), sin(rotation));
        float projection = dot(rawUV - _DissolveSwipeCenter.xy, direction);
        half swipe = (half)UberSafeInverseLerp(_DissolveSwipeRange.x,
            _DissolveSwipeRange.y, projection);
        half dissolveValue = saturate(swipe + (noise - 0.5h) *
            saturate(_DissolveSwipeNoiseStrength));
    #else
        half dissolveValue = (half)UberSafeInverseLerp(_DissolveNoiseRange.x,
            _DissolveNoiseRange.y, noise);
    #endif

    half customWeight = UberParticleCustomBlendWeight(_CustomDissolveWeight);
    half threshold = saturate(lerp(_DissolveAmount,
        saturate((half)custom1.x), customWeight));
    clip(dissolveValue - threshold);
    half edgePosition = saturate((dissolveValue - threshold) /
        max(abs(_DissolveEdgeWidth), 0.0001h));
    edge = 1.0h - edgePosition;
    edgeColor = lerp(_DissolveEdgeColor0, _DissolveEdgeColor1, edgePosition);
#else
    edge = 0.0h;
    edgeColor = 0.0h;
#endif
}

inline UberParticleSilhouette UberParticleEvaluateSilhouette(
    VaryingsParticle input)
{
    UberParticleSilhouette result = (UberParticleSilhouette)0;
    float4 custom1 = UberParticleGetCustom1(input);
#if !defined(UBER_PARTICLE_COLOR_PASS) && !defined(_ALPHATEST_ON)
    half baseNoiseClipEnabled = step(0.5h, _BaseNoiseClipEnabled);
    UNITY_BRANCH
    if (baseNoiseClipEnabled > 0.0h)
    {
#endif
    result.color = UberParticleSampleCore(input, custom1);
    result.color *= UberParticleEvaluateLifetimeMultiplier(
        input.normalizedControlStreams.x);
    result.color.a *= UberParticleEvaluateMask(input.rawUV);
#if !defined(UBER_PARTICLE_COLOR_PASS) && !defined(_ALPHATEST_ON)
    }
#endif
#if defined(_ALPHATEST_ON)
    clip(result.color.a - _Cutoff);
#endif
    UberParticleEvaluateDissolve(input.rawUV, custom1, result.dissolveEdge,
        result.dissolveEdgeColor);
    return result;
}

inline half UberParticleSoftFade(float4 projection)
{
#if defined(_SOFTPARTICLES_ON)
    float safeW = UberSafeSignedRange(projection.w, 0.000001);
    float2 screenUV = UnityStereoTransformScreenSpaceTex(projection.xy / safeW);
    screenUV = FoveatedRemapLinearToNonUniform(screenUV);
    float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp,
        screenUV).r;
    float sceneDepth = unity_OrthoParams.w == 0.0
        ? LinearEyeDepth(rawDepth, _ZBufferParams)
        : LinearDepthToEyeDepth(rawDepth);
    float particleDepth = LinearEyeDepth(projection.z / safeW, _ZBufferParams);
    return (half)UberSafeInverseLerp(_SoftParticlesNearFadeDistance,
        _SoftParticlesFarFadeDistance, sceneDepth - particleDepth);
#else
    return 1.0h;
#endif
}

inline half UberParticleCameraFade(float4 projection)
{
    float safeW = UberSafeSignedRange(projection.w, 0.000001);
    float particleDepth = LinearEyeDepth(projection.z / safeW, _ZBufferParams);
    return (half)UberSafeInverseLerp(_CameraNearFadeDistance,
        _CameraFarFadeDistance, particleDepth);
}

inline half3 UberParticleEvaluateEmission(float2 rawUV, float4 custom1)
{
#if defined(_EMISSION)
    float2 uv = rawUV * _EmissionMap_ST.xy + _EmissionMap_ST.zw;
    half3 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;
    half customScale = UberParticleCustomMultiplier(_CustomEmissionWeight,
        custom1.y);
    return map * _EmissionColor.rgb * max(_EmissionIntensity, 0.0h) *
        customScale;
#else
    return 0.0h;
#endif
}

inline half UberParticleEvaluateRim(VaryingsParticle input)
{
#if defined(_RIM_ON)
    #if defined(_RIM_RADIAL_UV)
        half rim = saturate((half)length(input.rawUV -
            _RimRadialCenter.xy) * 2.0h);
    #else
        float3 normalWS = UberParticleSafeDirection(input.normalWS,
            float3(0.0, 0.0, 1.0));
        float3 viewWS = UberParticleSafeDirection(
            GetWorldSpaceViewDir(input.positionWS.xyz),
            float3(0.0, 0.0, 1.0));
        half rim = 1.0h - saturate((half)dot(normalWS, viewWS));
    #endif
    return pow(saturate(rim), max(_RimPower, 0.0001h));
#else
    return 0.0h;
#endif
}

VaryingsParticle UberParticleBuildVaryings(UberParticleAttributes input)
{
    VaryingsParticle output = (VaryingsParticle)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 custom1 = UberParticleReadCustom1(input);
    float3 positionOS = UberParticleOffsetPosition(input.positionOS.xyz, custom1);
    VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
    output.clipPos = positionInputs.positionCS;
    output.positionWS.xyz = positionInputs.positionWS;
    output.positionWS.w = ComputeFogFactor(positionInputs.positionCS.z);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.color = GetParticleColor(input.color);
    output.normalizedControlStreams = float2(
        UberParticleReadLifetimeTime(input),
        UberParticleReadBaseNoiseClipThreshold(input));

    float3 blendUV;
    UberParticleGetUVs(input, output.texcoord, blendUV, output.rawUV);
#if defined(_FLIPBOOKBLENDING_ON)
    output.texcoord2AndBlend = blendUV;
#endif
#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON)
    output.projectedPosition = positionInputs.positionNDC;
#endif
#if defined(_CUSTOM_DATA_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    output.custom1 = custom1;
#endif
    return output;
}

VaryingsParticle UberParticleForwardVertex(UberParticleAttributes input)
{
    return UberParticleBuildVaryings(input);
}

VaryingsParticle UberParticleSilhouetteVertex(UberParticleAttributes input)
{
    return UberParticleBuildVaryings(input);
}

half4 UberParticleForwardFragment(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    UberParticleSilhouette surface = UberParticleEvaluateSilhouette(input);
    float4 custom1 = UberParticleGetCustom1(input);
    half4 color = surface.color;
#if defined(_COLOR_ADJUST_ON)
    color.rgb = UberAdjustColor(color.rgb, _HueShift, _Saturation,
        _Brightness, _Contrast);
#endif
#if defined(_DISSOLVE_ON)
    half edgeBlend = surface.dissolveEdge *
        saturate(surface.dissolveEdgeColor.a);
    color.rgb = lerp(color.rgb, surface.dissolveEdgeColor.rgb, edgeBlend);
    color.rgb += surface.dissolveEdgeColor.rgb * surface.dissolveEdge *
        max(_DissolveEdgeEmission, 0.0h);
#endif
    color.rgb += UberParticleEvaluateEmission(input.rawUV, custom1);
#if defined(_RIM_ON)
    color.rgb += _RimColor.rgb * UberParticleEvaluateRim(input) *
        max(_RimIntensity, 0.0h);
#endif
#if defined(_SOFTPARTICLES_ON)
    color.a *= UberParticleSoftFade(input.projectedPosition);
#endif
#if defined(_FADING_ON)
    color.a *= UberParticleCameraFade(input.projectedPosition);
#endif
#if defined(_ALPHAPREMULTIPLY_ON)
    color.rgb *= color.a;
#elif defined(_ALPHAMODULATE_ON)
    color.rgb = lerp(half3(1.0h, 1.0h, 1.0h), color.rgb, color.a);
#endif
    color.rgb = MixFog(color.rgb, input.positionWS.w);
#if !defined(_SURFACE_TYPE_TRANSPARENT)
    color.a = 1.0h;
#endif
    return color;
}

half4 UberParticleDepthFragment(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    UberParticleEvaluateSilhouette(input);
    return 0.0h;
}

half4 UberParticleDepthNormalsFragment(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    UberParticleEvaluateSilhouette(input);
    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormal = PackNormalOctQuadEncode(normalWS);
    half3 packedNormal = PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
    return half4(packedNormal, 0.0h);
#else
    return half4(normalWS, 0.0h);
#endif
}

half4 UberParticleSceneSelectionFragment(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    UberParticleEvaluateSilhouette(input);
    return float4(_ObjectId, _PassValue, 1.0, 1.0);
}

half4 UberParticleScenePickingFragment(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    UberParticleEvaluateSilhouette(input);
    return _SelectionID;
}

#endif // UBER_PARTICLE_INCLUDED
