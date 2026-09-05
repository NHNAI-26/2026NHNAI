#ifndef UBER_3D_INCLUDED
#define UBER_3D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "UberCommon.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap3DTiling;
    float4 _BlendTiling;
    float4 _DissolveTilingOffset;
    float4 _DissolvePanning;
    float4 _DissolveNoiseAmountMovement;
    float4 _GlitchBandSizeRange;
    float4 _GlitchUpVector;
    float4 _WobbleAxis;
    half4 _BaseColor;
    half4 _BlendColor;
    half4 _EmissionColor;
    half4 _RimColor;
    half4 _HeightFadeColor;
    half4 _GlassGlowColor;
    half4 _HologramColor;
    half4 _DissolveEdgeColor;
    half4 _StencilOutlineColor;
    half _Cutoff;
    half _Metallic;
    half _Smoothness;
    half _BumpScale;
    half _BaseMap3DBlendSharpness;
    half _BlendThreshold;
    half _BlendSmoothness;
    half _HueShift;
    half _Saturation;
    half _Brightness;
    half _Contrast;
    half _EmissionIntensity;
    half _RimPower;
    half _RimIntensity;
    float _HeightFadeLower;
    float _HeightFadeUpper;
    float _HeightFadeOffset;
    half _GlassGlowThreshold;
    half _GlassGlowIntensity;
    float4 _HologramObjectUpVector;
    half _HologramOpacity;
    half _HologramFresnelPower;
    half _HologramFresnelIntensity;
    half _HologramScanlineDensity;
    half _HologramScanlineSpeed;
    half _HologramScanlineWidth;
    half _HologramScanlineIntensity;
    half _HologramNoiseScale;
    half _HologramNoiseStrength;
    half _HologramNoiseSpeed;
    half _GlitchStrength;
    half _GlitchRGBSplit;
    half _GlitchFrequency;
    half _GlitchSpeed;
    half _WobbleAmplitude;
    half _WobbleHeight;
    half _WobbleFrequency;
    half _WobbleWaves;
    float _WobbleHalfHeight;
    half _DissolveAmount;
    float4 _DissolveObjectUpVector;
    float4 _DissolveObjectRange;
    float _DissolveObjectNoiseScale;
    half _DissolveObjectNoiseStrength;
    half _DissolveEdgeWidth;
    half _DissolveEdgeIntensity;
    half _DitherFade;
    half _StencilOutlineWidth;
    half _SurfaceOptions;
    half _Surface;
    half _Blend;
    half _ZWriteControl;
    half _AlphaClip;
    half _LightingMode;
    half _SurfaceInputs;
    half _BaseMapMapping;
    half _MetallicMapEnabled;
    half _RoughnessMapEnabled;
    half _NormalMapEnabled;
    half _TextureBlendEnabled;
    half _ReceiveShadows;
    half _UberQuality;
    half _CastShadows;
    half _StencilOutlineEnabled;
    half _ColorAdjustEnabled;
    half _EmissionEnabled;
    half _RimEnabled;
    half _HeightFadeEnabled;
    half _GlassGlowEnabled;
    half _HologramEnabled;
    half _HologramSpace;
    half _GlitchEnabled;
    half _GlitchSpace;
    half _DissolveEnabled;
    half _DissolveSpace;
    half _DitherFadeEnabled;
    half _WobbleEnabled;
    half _SrcBlend;
    half _DstBlend;
    half _SrcBlendAlpha;
    half _DstBlendAlpha;
    half _ZWrite;
    half _Cull;
    half _BlendModePreserveSpecular;
    half _AlphaToMask;
    half _QueueOffset;
    half _QueueControl;
CBUFFER_END

TEXTURE2D(_DissolveNoiseMap);
SAMPLER(sampler_DissolveNoiseMap);
TEXTURE2D(_BlendMap);
SAMPLER(sampler_BlendMap);
TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);
TEXTURE2D(_RoughnessMap);
SAMPLER(sampler_RoughnessMap);

float3 _LightDirection;
float3 _LightPosition;

inline half4 UberSampleBase(float2 rawUV, out float2 surfaceUV)
{
    surfaceUV = TRANSFORM_TEX(rawUV, _BaseMap);
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, surfaceUV);
}

inline half4 UberSampleBaseMapped(float2 rawUV, float3 positionWS,
    half3 geometricNormalWS, out float2 surfaceUV)
{
    surfaceUV = TRANSFORM_TEX(rawUV, _BaseMap);
#if defined(_BASE_MAP_TRIPLANAR)
    half3 blendWeights = pow(abs(normalize(geometricNormalWS)),
        max(_BaseMap3DBlendSharpness, 1.0h));
    blendWeights /= max(blendWeights.x + blendWeights.y + blendWeights.z,
        0.0001h);

    float3 mappingTiling = max(abs(_BaseMap3DTiling.xyz), 0.0001);
    float3 mappingPosition = positionWS * mappingTiling;
    half3 normalSign = sign(geometricNormalWS);
    float2 xUV = mappingPosition.zy * float2(normalSign.x, 1.0);
    float2 yUV = mappingPosition.xz * float2(normalSign.y, 1.0);
    float2 zUV = mappingPosition.xy * float2(-normalSign.z, 1.0);
    half4 xSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, xUV);
    half4 ySample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, yUV);
    half4 zSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, zUV);
    return xSample * blendWeights.x + ySample * blendWeights.y +
        zSample * blendWeights.z;
#else
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, surfaceUV);
#endif
}

inline half UberSampleMetallicMapped(float2 surfaceUV, float3 positionWS,
    half3 geometricNormalWS)
{
#if defined(_METALLICMAP)
    #if defined(_BASE_MAP_TRIPLANAR)
    half3 blendWeights = pow(abs(normalize(geometricNormalWS)),
        max(_BaseMap3DBlendSharpness, 1.0h));
    blendWeights /= max(blendWeights.x + blendWeights.y + blendWeights.z,
        0.0001h);
    float3 mappingPosition = positionWS *
        max(abs(_BaseMap3DTiling.xyz), 0.0001);
    half3 normalSign = sign(geometricNormalWS);
    half xSample = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap,
        mappingPosition.zy * float2(normalSign.x, 1.0)).r;
    half ySample = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap,
        mappingPosition.xz * float2(normalSign.y, 1.0)).r;
    half zSample = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap,
        mappingPosition.xy * float2(-normalSign.z, 1.0)).r;
    return xSample * blendWeights.x + ySample * blendWeights.y +
        zSample * blendWeights.z;
    #else
    return SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, surfaceUV).r;
    #endif
#else
    return 1.0h;
#endif
}

inline half UberSampleRoughnessMapped(float2 surfaceUV, float3 positionWS,
    half3 geometricNormalWS)
{
#if defined(_ROUGHNESSMAP)
    #if defined(_BASE_MAP_TRIPLANAR)
    half3 blendWeights = pow(abs(normalize(geometricNormalWS)),
        max(_BaseMap3DBlendSharpness, 1.0h));
    blendWeights /= max(blendWeights.x + blendWeights.y + blendWeights.z,
        0.0001h);
    float3 mappingPosition = positionWS *
        max(abs(_BaseMap3DTiling.xyz), 0.0001);
    half3 normalSign = sign(geometricNormalWS);
    half xSample = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap,
        mappingPosition.zy * float2(normalSign.x, 1.0)).r;
    half ySample = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap,
        mappingPosition.xz * float2(normalSign.y, 1.0)).r;
    half zSample = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap,
        mappingPosition.xy * float2(-normalSign.z, 1.0)).r;
    return xSample * blendWeights.x + ySample * blendWeights.y +
        zSample * blendWeights.z;
    #else
    return SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, surfaceUV).r;
    #endif
#else
    return 0.0h;
#endif
}

inline half3 UberApplyTextureBlend(float2 rawUV, half3 geometricNormalWS,
    half3 baseAlbedo, half3 vertexColor)
{
#if defined(_TEXTURE_BLEND_ON)
    float2 blendUV = rawUV * _BlendTiling.xy;
    half3 blendAlbedo = SAMPLE_TEXTURE2D(_BlendMap, sampler_BlendMap,
        blendUV).rgb * _BlendColor.rgb * vertexColor;
    half normalUp = normalize(geometricNormalWS).y;
    half blendWidth = max(_BlendSmoothness, 0.0001h);
    half blendWeight = smoothstep(_BlendThreshold - blendWidth,
        _BlendThreshold + blendWidth, normalUp);
    return lerp(baseAlbedo, blendAlbedo, blendWeight);
#else
    return baseAlbedo;
#endif
}

inline float UberGlitchHash(float2 value)
{
    return UberHash21(value);
}

inline float UberGlitchBandBoundary(float boundaryIndex, float frame,
    float averageBandSize, float bandSizeVariation)
{
    return UberEvaluateGlitchBandBoundary(boundaryIndex, frame,
        averageBandSize, bandSizeVariation);
}

inline float2 UberGlitchClipToPixel(float4 positionCS)
{
    float4 screenPosition = ComputeScreenPos(positionCS);
    return screenPosition.xy /
        max(abs(screenPosition.w), 0.000001) * _ScreenParams.xy;
}

inline float2 UberGlitchSafeNormalize(float2 value, float2 fallback)
{
    float lengthSquared = dot(value, value);
    if (!(lengthSquared > 0.000001) || lengthSquared > 1.0e20)
        return fallback;
    return value * rsqrt(lengthSquared);
}

inline float3 UberGlitchSafeNormalize3(float3 value, float3 fallback)
{
    return UberSafeNormalizeFinite3(value, fallback);
}

inline float3 UberGetGlitchUpVector()
{
    return UberGlitchSafeNormalize3(_GlitchUpVector.xyz,
        float3(0.0, 1.0, 0.0));
}

inline float3 UberGetGlitchPlaneTangent(float3 upVector)
{
    float3 referenceAxis = abs(upVector.z) < 0.999
        ? float3(0.0, 0.0, 1.0) : float3(1.0, 0.0, 0.0);
    return UberGlitchSafeNormalize3(cross(upVector, referenceAxis),
        float3(1.0, 0.0, 0.0));
}

inline float2 UberGetGlitchPlanePixelDirection(float2 tangentPixelDelta,
    float2 bitangentPixelDelta)
{
    float tangentLengthSquared = dot(tangentPixelDelta, tangentPixelDelta);
    float bitangentLengthSquared = dot(bitangentPixelDelta,
        bitangentPixelDelta);
    float2 planePixelDelta = tangentLengthSquared >= bitangentLengthSquared
        ? tangentPixelDelta : bitangentPixelDelta;
    return UberGlitchSafeNormalize(planePixelDelta, float2(1.0, 0.0));
}

inline void UberGetGlitchSpaceData(float3 positionOS,
    out float bandPixelCoordinate, out float2 shiftPixelDirection)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float2 positionPixel = UberGlitchClipToPixel(
        TransformWorldToHClip(positionWS));
#if defined(_GLITCH_OBJECT_SPACE)
    float3 upVector = UberGetGlitchUpVector();
    float3 planeTangent = UberGetGlitchPlaneTangent(upVector);
    float3 planeBitangent = cross(upVector, planeTangent);
    float2 upPixel = UberGlitchClipToPixel(TransformWorldToHClip(
        TransformObjectToWorld(positionOS + upVector)));
    float2 tangentPixel = UberGlitchClipToPixel(TransformWorldToHClip(
        TransformObjectToWorld(positionOS + planeTangent)));
    float2 bitangentPixel = UberGlitchClipToPixel(TransformWorldToHClip(
        TransformObjectToWorld(positionOS + planeBitangent)));
    bandPixelCoordinate = dot(positionOS, upVector) *
        max(length(upPixel - positionPixel), 0.0001);
    shiftPixelDirection = UberGetGlitchPlanePixelDirection(
        tangentPixel - positionPixel, bitangentPixel - positionPixel);
#elif defined(_GLITCH_WORLD_SPACE)
    float3 upVector = UberGetGlitchUpVector();
    float3 planeTangent = UberGetGlitchPlaneTangent(upVector);
    float3 planeBitangent = cross(upVector, planeTangent);
    float2 upPixel = UberGlitchClipToPixel(TransformWorldToHClip(
        positionWS + upVector));
    float2 tangentPixel = UberGlitchClipToPixel(TransformWorldToHClip(
        positionWS + planeTangent));
    float2 bitangentPixel = UberGlitchClipToPixel(TransformWorldToHClip(
        positionWS + planeBitangent));
    bandPixelCoordinate = dot(positionWS, upVector) *
        max(length(upPixel - positionPixel), 0.0001);
    shiftPixelDirection = UberGetGlitchPlanePixelDirection(
        tangentPixel - positionPixel, bitangentPixel - positionPixel);
#else
    bandPixelCoordinate = positionPixel.y;
    shiftPixelDirection = float2(1.0, 0.0);
#endif
}

inline void UberEvaluateGlitchBand(float bandPixelCoordinate, float frame,
    out half glitchActivation, out float glitchDirection)
{
    glitchActivation = 0.0h;
    glitchDirection = 1.0;
#if defined(_GLITCH_ON)
    float minBandSize = clamp(min(_GlitchBandSizeRange.x,
        _GlitchBandSizeRange.y), 1.0, 64.0);
    float maxBandSize = clamp(max(_GlitchBandSizeRange.x,
        _GlitchBandSizeRange.y), minBandSize, 64.0);
    float averageBandSize = (minBandSize + maxBandSize) * 0.5;
    float bandSizeVariation = maxBandSize - minBandSize;
    float bandIndex = floor(bandPixelCoordinate / averageBandSize);
    float lowerBoundary = UberGlitchBandBoundary(bandIndex, frame,
        averageBandSize, bandSizeVariation);
    float upperBoundary = UberGlitchBandBoundary(bandIndex + 1.0, frame,
        averageBandSize, bandSizeVariation);
    bandIndex += step(upperBoundary, bandPixelCoordinate);
    bandIndex -= 1.0 - step(lowerBoundary, bandPixelCoordinate);
    float activation = step(1.0 - saturate(_GlitchFrequency),
        UberGlitchHash(float2(bandIndex, frame)));
    glitchDirection = UberGlitchHash(float2(bandIndex + 19.19,
        frame + 47.47)) * 2.0 - 1.0;
    glitchActivation = (half)activation;
#endif
}

inline void UberApplyGlitchVertexPosition(float3 positionOS,
    inout float4 positionCS)
{
#if defined(_GLITCH_ON)
    float bandPixelCoordinate;
    float2 shiftPixelDirection;
    UberGetGlitchSpaceData(positionOS, bandPixelCoordinate,
        shiftPixelDirection);
    float frame = floor(_Time.y * max(_GlitchSpeed, 0.0h));
    half glitchActivation;
    float glitchDirection;
    UberEvaluateGlitchBand(bandPixelCoordinate, frame, glitchActivation,
        glitchDirection);
    float shiftPixels = glitchDirection * max(_GlitchStrength, 0.0h) *
        glitchActivation;
    float2 clipPixelDirection = shiftPixelDirection *
        float2(1.0, _ProjectionParams.x);
    positionCS.xy += clipPixelDirection * shiftPixels *
        (2.0 / max(_ScreenParams.xy, float2(1.0, 1.0))) * positionCS.w;
#endif
}

// Radial wobble that only bites near the bottom of the mesh, so a rocket can churn
// on its pad without its nose moving. The mesh keeps its length: only the radius
// swells and shrinks, and the wave travels up the axis, which reads as something
// surging inside a tube rather than the whole body being squashed.
// _WobbleHalfHeight carries the mesh extent along the axis in object units, so
// _WobbleHeight stays a normalised 0..1 fraction whatever the import scale is.
// Meshes reach Unity at wildly different local sizes.
// Normals are left untouched: at the amplitudes this ships with the lighting error
// is invisible, and doing it properly means a jacobian in all five passes.
inline void UberApplyWobble(inout float3 positionOS)
{
#if defined(_WOBBLE_ON)
    float3 axis = UberSafeNormalizeFinite3(_WobbleAxis.xyz, float3(0.0, 1.0, 0.0));
    float halfHeight = max(abs(_WobbleHalfHeight), 1e-6);
    float height = saturate((dot(positionOS, axis) + halfHeight) / (2.0 * halfHeight));
    float weight = saturate((_WobbleHeight - height) / max(_WobbleHeight, 1e-4));
    weight *= weight;
    float phase = (_Time.y * _WobbleFrequency - height * _WobbleWaves) * 6.2831853;
    // 0..1, never negative: the body swells outward and settles back. A signed wave
    // pinches the hull inward on the other half of the cycle, which reads as damage.
    float swell = 0.5 - 0.5 * cos(phase);
    // Amplitude is a fraction of the local radius, so it needs no unit conversion.
    float3 lateral = positionOS - axis * dot(positionOS, axis);
    positionOS += lateral * (weight * _WobbleAmplitude * swell);
#endif
}

inline float2 UberApplyGlitchUV(float2 rawUV, float3 positionOS,
    out half glitchActivation, out float glitchDirection,
    out float2 rawPixelStep)
{
    rawPixelStep = ddx(rawUV);
    glitchActivation = 0.0h;
    glitchDirection = 1.0;
#if defined(_GLITCH_ON)
    float bandPixelCoordinate;
    float2 shiftPixelDirection;
    UberGetGlitchSpaceData(positionOS, bandPixelCoordinate,
        shiftPixelDirection);
    rawPixelStep = ddx(rawUV) * shiftPixelDirection.x +
        ddy(rawUV) * shiftPixelDirection.y;
    float frame = floor(_Time.y * max(_GlitchSpeed, 0.0h));
    UberEvaluateGlitchBand(bandPixelCoordinate, frame, glitchActivation,
        glitchDirection);
    float shiftPixels = glitchDirection * max(_GlitchStrength, 0.0h) *
        glitchActivation;
    return rawUV + rawPixelStep * shiftPixels;
#else
    return rawUV;
#endif
}

inline half4 UberApplyGlitchRGBSplit(float2 effectUV, half4 center,
    half glitchActivation, float glitchDirection, float2 rawPixelStep)
{
#if defined(_GLITCH_ON) && !defined(_BASE_MAP_TRIPLANAR)
    [branch]
    if (glitchActivation > 0.0h && _GlitchRGBSplit > 0.0001h)
    {
        float splitPixels = max(_GlitchRGBSplit, 0.0h);
        float splitDirection = glitchDirection < 0.0 ? -1.0 : 1.0;
        float2 splitUV = rawPixelStep * splitPixels * splitDirection;
        float2 splitSurfaceUV;
        center.r = UberSampleBase(effectUV + splitUV, splitSurfaceUV).r;
        center.b = UberSampleBase(effectUV - splitUV, splitSurfaceUV).b;
    }
#endif
    return center;
}

inline half3 UberApplyBaseColorAdjustment(half3 color)
{
#if defined(_COLOR_ADJUST_ON)
    return UberAdjustColor(color, _HueShift, _Saturation, _Brightness, _Contrast);
#else
    return color;
#endif
}

inline half UberGetDitherThreshold(float4 positionCS)
{
    float2 pixelPosition = GetNormalizedScreenSpaceUV(positionCS) * _ScreenParams.xy;
#if defined(_UBER_QUALITY_LOW)
    float2 pixel = floor(frac(pixelPosition * 0.5) * 2.0);
    return (UberBayer2x2(pixel) + 0.5h) * 0.25h;
#else
    return UberBayer4x4(pixelPosition);
#endif
}

inline float3 UberGetDissolveUpVector()
{
    float3 upVector = _DissolveObjectUpVector.xyz;
    float lengthSquared = dot(upVector, upVector);
    if (!(lengthSquared > 0.000001) || lengthSquared > 1.0e20)
        return float3(0.0, 1.0, 0.0);

    return upVector * rsqrt(lengthSquared);
}

inline float2 UberGetObjectNoiseUV(float3 positionOS, float3 upVector)
{
    float3 referenceAxis = abs(upVector.y) < 0.999
        ? float3(0.0, 1.0, 0.0)
        : float3(0.0, 0.0, 1.0);
    float3 right = normalize(cross(upVector, referenceAxis));
    float3 forward = cross(right, upVector);
    float rangeSize = max(abs(
        _DissolveObjectRange.y - _DissolveObjectRange.x), 0.0001);
    float noiseSize = max(abs(_DissolveObjectNoiseScale), 0.0001);
    float coordinateScale = rcp(rangeSize * noiseSize);
    return float2(dot(positionOS, right), dot(positionOS, forward))
        * coordinateScale;
}

inline float2 UberTransformDissolveNoiseUV(float2 noiseUV)
{
    return noiseUV * _DissolveTilingOffset.xy + _DissolveTilingOffset.zw;
}

inline half UberEvaluateDissolve(float2 rawUV, float3 positionOS, out half edge)
{
    edge = 0.0h;
#if defined(_DISSOLVE_ON)
    float2 pan = _DissolvePanning.xy * _Time.y;
    float2 amountMovement = _DissolveNoiseAmountMovement.xy
        * saturate(_DissolveAmount);
    half field;
#if defined(_DISSOLVE_OBJECT_SPACE)
    float3 upVector = UberGetDissolveUpVector();
    float upCoordinate = dot(positionOS, upVector);
    half gradient = UberSafeInverseLerp(_DissolveObjectRange.x,
        _DissolveObjectRange.y, upCoordinate);
    float2 noiseUV = UberTransformDissolveNoiseUV(
        UberGetObjectNoiseUV(positionOS, upVector)) + pan + amountMovement;
    half noise = SAMPLE_TEXTURE2D(_DissolveNoiseMap, sampler_DissolveNoiseMap,
        noiseUV).r;
    field = saturate(gradient +
        (noise - 0.5h) * _DissolveObjectNoiseStrength);
#else
    float2 noiseUV = UberTransformDissolveNoiseUV(rawUV)
        + pan + amountMovement;
    half noise = SAMPLE_TEXTURE2D(_DissolveNoiseMap, sampler_DissolveNoiseMap, noiseUV).r;
    field = saturate(noise);
#endif
    half distanceToEdge = field - saturate(_DissolveAmount);
    half width = max(_DissolveEdgeWidth, 0.0001h);
    edge = saturate(1.0h - distanceToEdge / width) * step(0.0h, distanceToEdge);
    clip(distanceToEdge - 0.0001h);
#endif
    return edge;
}

inline half4 UberEvaluateHeightTint(float3 positionWS)
{
#if defined(_HEIGHT_FADE_ON)
    half factor = UberSafeInverseLerp(_HeightFadeLower, _HeightFadeUpper,
        positionWS.y - _HeightFadeOffset);
    return lerp(_HeightFadeColor, half4(1.0h, 1.0h, 1.0h, 1.0h), factor);
#else
    return half4(1.0h, 1.0h, 1.0h, 1.0h);
#endif
}
inline half UberEvaluateSilhouette(float2 rawUV, float3 positionOS, half baseAlpha,
    float4 positionCS, out half dissolveEdge)
{
    half alpha = saturate(baseAlpha);
#if defined(_HEIGHT_FADE_ON)
    alpha *= UberEvaluateHeightTint(TransformObjectToWorld(positionOS)).a;
    clip(alpha - 0.0001h);
#endif
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
    UberEvaluateDissolve(rawUV, positionOS, dissolveEdge);
#if defined(_DITHER_FADE_ON)
    alpha *= saturate(_DitherFade);
    clip(alpha - UberGetDitherThreshold(positionCS));
#endif
    return alpha;
}
inline half3 UberEvaluateGlassGlow(half3 albedo)
{
#if defined(_GLASS_GLOW_ON)
    half glow = UberSafeInverseLerp(_GlassGlowThreshold, 1.0h, Luminance(albedo));
    return _GlassGlowColor.rgb * glow * max(_GlassGlowIntensity, 0.0h);
#else
    return 0.0h;
#endif
}

inline half3 UberEvaluateRim(half3 normalWS, half3 viewDirectionWS)
{
#if defined(_RIM_ON)
    half fresnel = pow(saturate(1.0h - dot(normalize(normalWS),
        normalize(viewDirectionWS))), max(_RimPower, 0.0001h));
    return _RimColor.rgb * fresnel * max(_RimIntensity, 0.0h);
#else
    return 0.0h;
#endif
}

inline float3 UberGetHologramUpVector()
{
    return UberSafeNormalizeFinite3(_HologramObjectUpVector.xyz,
        float3(0.0, 1.0, 0.0));
}

inline float UberHologramValueNoise(float coordinate)
{
    return UberValueNoise1D(coordinate);
}

inline float UberGetHologramCoordinate(float3 positionOS, float3 positionWS,
    float4 positionCS)
{
#if defined(_HOLOGRAM_SCREEN_SPACE)
    return GetNormalizedScreenSpaceUV(positionCS).y;
#elif defined(_HOLOGRAM_WORLD_SPACE)
    return positionWS.y;
#else
    return dot(positionOS, UberGetHologramUpVector());
#endif
}

inline void UberApplyHologram(float3 positionOS, float3 positionWS,
    float4 positionCS, half3 normalWS, half3 viewDirectionWS,
    inout SurfaceData surfaceData)
{
#if defined(_HOLOGRAM_ON)
    float coordinate = UberGetHologramCoordinate(positionOS, positionWS, positionCS);
    float noiseCoordinate = coordinate * max(abs(_HologramNoiseScale), 0.0001) +
        _Time.y * _HologramNoiseSpeed;
    float noise = UberHologramValueNoise(noiseCoordinate) * 2.0 - 1.0;
    float phase = coordinate * max(abs(_HologramScanlineDensity), 0.0001) +
        _Time.y * _HologramScanlineSpeed + noise * _HologramNoiseStrength;
    float distanceToLine = abs(frac(phase + 0.5) - 0.5);
    float halfWidth = saturate(_HologramScanlineWidth) * 0.5;
    half scanline = 1.0h - smoothstep(halfWidth,
        halfWidth + max(fwidth(phase), 0.0001), distanceToLine);
    half fresnel = pow(saturate(1.0h - dot(normalize(normalWS),
        normalize(viewDirectionWS))), max(_HologramFresnelPower, 0.0001h));
    surfaceData.albedo *= saturate(_HologramColor.rgb);
    surfaceData.alpha *= saturate(_HologramOpacity);
    surfaceData.emission += _HologramColor.rgb *
        (fresnel * max(_HologramFresnelIntensity, 0.0h) +
         scanline * max(_HologramScanlineIntensity, 0.0h));
#endif
}

inline void UberInitializeSurface(float2 rawUV, float3 positionOS, float3 positionWS,
    half3 geometricNormalWS, half4 vertexColor, float4 positionCS,
    out SurfaceData surfaceData, out half dissolveEdge)
{
    half glitchActivation;
    float glitchDirection;
    float2 rawPixelStep;
    float2 effectUV = UberApplyGlitchUV(rawUV, positionOS,
        glitchActivation, glitchDirection, rawPixelStep);
    float2 surfaceUV;
    half4 baseSample = UberSampleBaseMapped(effectUV, positionWS,
        geometricNormalWS, surfaceUV);
    baseSample = UberApplyGlitchRGBSplit(effectUV, baseSample,
        glitchActivation, glitchDirection, rawPixelStep);
    half4 baseColor = baseSample * _BaseColor * vertexColor;
    half3 blendedAlbedo = UberApplyTextureBlend(effectUV, geometricNormalWS,
        baseColor.rgb, vertexColor.rgb);
    half3 albedo = UberApplyBaseColorAdjustment(blendedAlbedo);
    half alpha = UberEvaluateSilhouette(effectUV, positionOS, baseColor.a, positionCS,
        dissolveEdge);
    half4 heightTint = UberEvaluateHeightTint(positionWS);
    albedo *= heightTint.rgb;

    surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.alpha = alpha;
    half metallicMask = UberSampleMetallicMapped(surfaceUV, positionWS,
        geometricNormalWS);
    half roughness = UberSampleRoughnessMapped(surfaceUV, positionWS,
        geometricNormalWS);
    surfaceData.metallic = saturate(_Metallic * metallicMask);
    surfaceData.specular = 0.0h;
    surfaceData.smoothness = saturate(_Smoothness * (1.0h - roughness));
    surfaceData.normalTS = SampleNormal(surfaceUV,
        TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    surfaceData.occlusion = 1.0h;
    surfaceData.emission = SampleEmission(surfaceUV,
        _EmissionColor.rgb * max(_EmissionIntensity, 0.0h),
        TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
#if defined(_DISSOLVE_ON)
    surfaceData.emission += _DissolveEdgeColor.rgb *
        max(_DissolveEdgeIntensity, 0.0h) * dissolveEdge;
#endif
    surfaceData.emission += UberEvaluateGlassGlow(albedo);
    surfaceData.clearCoatMask = 0.0h;
    surfaceData.clearCoatSmoothness = 0.0h;
}

#if defined(UBER_FORWARD_PASS)

struct UberForwardAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UberForwardVaryings
{
    float2 rawUV : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
#if defined(_NORMALMAP)
    half4 tangentWS : TEXCOORD3;
#endif
    half4 fogAndVertexLight : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#if defined(DYNAMICLIGHTMAP_ON)
    float2 dynamicLightmapUV : TEXCOORD6;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
    float4 probeOcclusion : TEXCOORD7;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD8;
#endif
    float3 positionOS : TEXCOORD9;
    half4 color : COLOR;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

UberForwardVaryings UberForwardVertex(UberForwardAttributes input)
{
    UberForwardVaryings output = (UberForwardVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 wobbleedOS = input.positionOS.xyz;
    UberApplyWobble(wobbleedOS);
    VertexPositionInputs positionInputs = GetVertexPositionInputs(wobbleedOS);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
    output.rawUV = input.uv;
    output.positionOS = input.positionOS.xyz;
    output.positionWS = positionInputs.positionWS;
    output.positionCS = positionInputs.positionCS;
    UberApplyGlitchVertexPosition(input.positionOS.xyz, output.positionCS);
    output.normalWS = normalInputs.normalWS;
#if defined(_NORMALMAP)
    output.tangentWS = half4(normalInputs.tangentWS,
        input.tangentOS.w * GetOddNegativeScale());
#endif
    output.color = input.color;
    output.fogAndVertexLight = half4(ComputeFogFactor(positionInputs.positionCS.z),
        VertexLighting(positionInputs.positionWS, normalInputs.normalWS));
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST,
        output.staticLightmapUV);
#if defined(DYNAMICLIGHTMAP_ON)
    output.dynamicLightmapUV = input.dynamicLightmapUV * unity_DynamicLightmapST.xy +
        unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(positionInputs.positionWS, normalInputs.normalWS, viewDirectionWS,
        output.vertexSH, output.probeOcclusion);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(positionInputs);
#endif
    return output;
}

inline void UberInitializeInputData(UberForwardVaryings input, half3 normalTS,
    out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(_NORMALMAP)
    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS,
        input.tangentWS.xyz);
    inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS,
        input.normalWS);
    inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
    inputData.shadowCoord = 0.0;
#endif
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0),
        input.fogAndVertexLight.x);
    inputData.vertexLighting = input.fogAndVertexLight.yzw;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

#if defined(_SCREEN_SPACE_IRRADIANCE)
    inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionCS.xy);
#elif defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV,
        input.vertexSH, inputData.normalWS);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS,
        inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH,
        inputData.normalWS);
#endif

#if defined(DEBUG_DISPLAY)
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
#else
    inputData.vertexSH = input.vertexSH;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
#endif
#endif
}
struct UberForwardOutput
{
    half4 color : SV_Target0;
#if defined(_WRITE_RENDERING_LAYERS)
    uint renderingLayers : SV_Target1;
#endif
};

UberForwardOutput UberForwardFragment(UberForwardVaryings input)
{
    UberForwardOutput output = (UberForwardOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    SurfaceData surfaceData;
    half dissolveEdge;
    UberInitializeSurface(input.rawUV, input.positionOS, input.positionWS,
        input.normalWS, input.color, input.positionCS, surfaceData, dissolveEdge);
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif
    InputData inputData;
    UberInitializeInputData(input, surfaceData.normalTS, inputData);
    UberApplyHologram(input.positionOS, input.positionWS, input.positionCS,
        inputData.normalWS, inputData.viewDirectionWS, surfaceData);
    surfaceData.albedo = AlphaModulate(surfaceData.albedo, surfaceData.alpha);
#if defined(_UNLIT_ON)
    half4 color = UniversalFragmentUnlit(inputData, surfaceData);
#else
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
#endif
    color.rgb += UberEvaluateRim(inputData.normalWS, inputData.viewDirectionWS);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
#if defined(_UNLIT_ON) && defined(_ALPHAPREMULTIPLY_ON)
    color.rgb *= surfaceData.alpha;
#endif
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
    output.color = color;
#if defined(_WRITE_RENDERING_LAYERS)
    output.renderingLayers = EncodeMeshRenderingLayer();
#endif
    return output;
}

#elif defined(UBER_SHADOW_PASS) || defined(UBER_DEPTH_PASS)

struct UberDepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UberSilhouetteVaryings
{
    float2 rawUV : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    half vertexAlpha : TEXCOORD2;
#if defined(_BASE_MAP_TRIPLANAR)
    half3 normalWS : TEXCOORD3;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#if defined(UBER_SHADOW_PASS)
UberSilhouetteVaryings UberShadowVertex(UberDepthAttributes input)
{
    UberSilhouetteVaryings output = (UberSilhouetteVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 wobbleedOS = input.positionOS.xyz;
    UberApplyWobble(wobbleedOS);
    float3 positionWS = TransformObjectToWorld(wobbleedOS);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS,
        lightDirectionWS));
    output.positionCS = ApplyShadowClamping(output.positionCS);
    UberApplyGlitchVertexPosition(input.positionOS.xyz, output.positionCS);
    output.rawUV = input.uv;
    output.positionOS = input.positionOS.xyz;
    output.vertexAlpha = input.color.a;
#if defined(_BASE_MAP_TRIPLANAR)
    output.normalWS = normalWS;
#endif
    return output;
}
#else
UberSilhouetteVaryings UberDepthVertex(UberDepthAttributes input)
{
    UberSilhouetteVaryings output = (UberSilhouetteVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 wobbleedOS = input.positionOS.xyz;
    UberApplyWobble(wobbleedOS);
    output.positionCS = TransformObjectToHClip(wobbleedOS);
    UberApplyGlitchVertexPosition(input.positionOS.xyz, output.positionCS);
    output.rawUV = input.uv;
    output.positionOS = input.positionOS.xyz;
    output.vertexAlpha = input.color.a;
#if defined(_BASE_MAP_TRIPLANAR)
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif
    return output;
}
#endif

half4 UberSilhouetteFragment(UberSilhouetteVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half glitchActivation;
    float glitchDirection;
    float2 rawPixelStep;
    float2 effectUV = UberApplyGlitchUV(input.rawUV, input.positionOS,
        glitchActivation, glitchDirection, rawPixelStep);
#if defined(_ALPHATEST_ON) || defined(_DITHER_FADE_ON)
    float2 surfaceUV;
#if defined(_BASE_MAP_TRIPLANAR)
    half baseAlpha = UberSampleBaseMapped(effectUV,
        TransformObjectToWorld(input.positionOS), input.normalWS,
        surfaceUV).a * _BaseColor.a * input.vertexAlpha;
#else
    half baseAlpha = UberSampleBase(effectUV, surfaceUV).a * _BaseColor.a *
        input.vertexAlpha;
#endif
#else
    half baseAlpha = 1.0h;
#endif
    half dissolveEdge;
    UberEvaluateSilhouette(effectUV, input.positionOS, baseAlpha,
        input.positionCS, dissolveEdge);
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif
    return 0.0h;
}

#elif defined(UBER_DEPTH_NORMALS_PASS)

struct UberDepthNormalsAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UberDepthNormalsVaryings
{
    float2 rawUV : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
#if defined(_NORMALMAP)
    half4 tangentWS : TEXCOORD3;
#endif
    half vertexAlpha : TEXCOORD4;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

UberDepthNormalsVaryings UberDepthNormalsVertex(UberDepthNormalsAttributes input)
{
    UberDepthNormalsVaryings output = (UberDepthNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS,
        input.tangentOS);
    float3 wobbleedOS = input.positionOS.xyz;
    UberApplyWobble(wobbleedOS);
    output.positionCS = TransformObjectToHClip(wobbleedOS);
    UberApplyGlitchVertexPosition(input.positionOS.xyz, output.positionCS);
    output.rawUV = input.uv;
    output.positionOS = input.positionOS.xyz;
    output.normalWS = normalInputs.normalWS;
#if defined(_NORMALMAP)
    output.tangentWS = half4(normalInputs.tangentWS,
        input.tangentOS.w * GetOddNegativeScale());
#endif
    output.vertexAlpha = input.color.a;
    return output;
}

void UberDepthNormalsFragment(UberDepthNormalsVaryings input,
    out half4 outNormalWS : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half glitchActivation;
    float glitchDirection;
    float2 rawPixelStep;
    float2 effectUV = UberApplyGlitchUV(input.rawUV, input.positionOS,
        glitchActivation, glitchDirection, rawPixelStep);
    float2 surfaceUV;
    half4 baseSample = UberSampleBaseMapped(effectUV,
        TransformObjectToWorld(input.positionOS), input.normalWS, surfaceUV);
    half dissolveEdge;
    UberEvaluateSilhouette(effectUV, input.positionOS,
        baseSample.a * _BaseColor.a * input.vertexAlpha, input.positionCS,
        dissolveEdge);
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif
#if defined(_NORMALMAP)
    half3 normalTS = SampleNormal(surfaceUV,
        TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS,
        input.tangentWS.xyz);
    half3 normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS));
#else
    half3 normalWS = input.normalWS;
#endif
    normalWS = NormalizeNormalPerPixel(normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormal = PackNormalOctQuadEncode(normalWS);
    half3 packedNormal = PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
    outNormalWS = half4(packedNormal, 0.0h);
#else
    outNormalWS = half4(normalWS, 0.0h);
#endif
#if defined(_WRITE_RENDERING_LAYERS)
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#elif defined(UBER_OUTLINE_PASS)

struct UberOutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UberOutlineVaryings
{
    float2 rawUV : TEXCOORD0;
    float3 positionOS : TEXCOORD1;
    half vertexAlpha : TEXCOORD2;
#if defined(_BASE_MAP_TRIPLANAR)
    half3 normalWS : TEXCOORD3;
#endif
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

UberOutlineVaryings UberOutlineVertex(UberOutlineAttributes input)
{
    UberOutlineVaryings output = (UberOutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 wobbleedOS = input.positionOS.xyz;
    UberApplyWobble(wobbleedOS);
    float3 positionWS = TransformObjectToWorld(wobbleedOS);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 normalVS = TransformWorldToViewDir(normalWS, true);
    float2 direction = normalVS.xy / max(length(normalVS.xy), 0.0001);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.positionCS.xy += direction * _StencilOutlineWidth *
        (2.0 / _ScreenParams.xy) * output.positionCS.w;
    UberApplyGlitchVertexPosition(input.positionOS.xyz, output.positionCS);
    output.rawUV = input.uv;
    output.positionOS = input.positionOS.xyz;
    output.vertexAlpha = input.color.a;
#if defined(_BASE_MAP_TRIPLANAR)
    output.normalWS = normalWS;
#endif
    return output;
}

half4 UberOutlineFragment(UberOutlineVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if !defined(_STENCIL_OUTLINE_ON)
    clip(-1.0h);
#endif
    half glitchActivation;
    float glitchDirection;
    float2 rawPixelStep;
    float2 effectUV = UberApplyGlitchUV(input.rawUV, input.positionOS,
        glitchActivation, glitchDirection, rawPixelStep);
    float2 surfaceUV;
#if defined(_BASE_MAP_TRIPLANAR)
    half baseAlpha = UberSampleBaseMapped(effectUV,
        TransformObjectToWorld(input.positionOS), input.normalWS,
        surfaceUV).a * _BaseColor.a * input.vertexAlpha;
#else
    half baseAlpha = UberSampleBase(effectUV, surfaceUV).a * _BaseColor.a *
        input.vertexAlpha;
#endif
    half dissolveEdge;
    half alpha = UberEvaluateSilhouette(effectUV, input.positionOS, baseAlpha,
        input.positionCS, dissolveEdge);
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif
    return half4(_StencilOutlineColor.rgb, _StencilOutlineColor.a * alpha);
}

#elif defined(UBER_META_PASS)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"
struct UberMetaAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; float2 uv2 : TEXCOORD2;
    half4 color : COLOR;
};
struct UberMetaVaryings
{
    float4 positionCS : SV_POSITION;
    float2 rawUV : TEXCOORD0;
    half3 normalWS : TEXCOORD3;
#if defined(_BASE_MAP_TRIPLANAR)
    float3 positionOS : TEXCOORD4;
#endif
    half4 color : COLOR;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV : TEXCOORD1; float4 LightCoord : TEXCOORD2;
#endif
};
UberMetaVaryings UberMetaVertex(UberMetaAttributes input)
{
    UberMetaVaryings output = (UberMetaVaryings)0;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.rawUV = input.uv0;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_BASE_MAP_TRIPLANAR)
    output.positionOS = input.positionOS.xyz;
#endif
    output.color = input.color;
#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
    return output;
}
half4 UberMetaFragment(UberMetaVaryings input) : SV_Target
{
    float2 surfaceUV;
#if defined(_BASE_MAP_TRIPLANAR)
    float3 positionWS = TransformObjectToWorld(input.positionOS);
#else
    float3 positionWS = 0.0;
#endif
    half4 baseSample = UberSampleBaseMapped(input.rawUV,
        positionWS,
        input.normalWS, surfaceUV);
    half4 baseColor = baseSample * _BaseColor * input.color;
    half alpha = saturate(baseColor.a);
#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif
    half3 blendedAlbedo = UberApplyTextureBlend(input.rawUV, input.normalWS,
        baseColor.rgb, input.color.rgb);
    half3 albedo = UberApplyBaseColorAdjustment(blendedAlbedo);
    half3 emission = SampleEmission(surfaceUV,
        _EmissionColor.rgb * max(_EmissionIntensity, 0.0h),
        TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    MetaInput metaInput = (MetaInput)0;
#if defined(_UNLIT_ON)
    metaInput.Albedo = albedo;
#else
    half metallicMask = UberSampleMetallicMapped(surfaceUV, positionWS,
        input.normalWS);
    half roughness = UberSampleRoughnessMapped(surfaceUV, positionWS,
        input.normalWS);
    BRDFData brdfData;
    InitializeBRDFData(albedo, saturate(_Metallic * metallicMask), 0.0h,
        saturate(_Smoothness * (1.0h - roughness)), alpha, brdfData);
    metaInput.Albedo = brdfData.diffuse + brdfData.specular *
        brdfData.roughness * 0.5h;
#endif
    metaInput.Emission = emission;
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = input.VizUV;
    metaInput.LightCoord = input.LightCoord;
#endif
    return UnityMetaFragment(metaInput);
}

#endif

#endif // UBER_3D_INCLUDED
