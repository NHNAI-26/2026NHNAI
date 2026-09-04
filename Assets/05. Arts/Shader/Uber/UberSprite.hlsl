#ifndef UBER_SPRITE_INCLUDED
#define UBER_SPRITE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "UberCommon.hlsl"

#if defined(UBER_SPRITE_FORWARD_PASS)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#elif defined(UBER_SPRITE_2D_PASS)
    #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
#elif defined(UBER_SPRITE_NORMALS_PASS)
    #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
#elif defined(UBER_SPRITE_SHADOW_PASS)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#endif

// This layout is deliberately unconditional so every local-keyword variant
// remains compatible with the SRP Batcher.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseSpriteUVRect;
    float4 _SecondaryUVRect;
    float4 _DissolveTilingOffset;
    float4 _DissolvePanning;
    float4 _DissolveNoiseRange;
    float4 _DissolveRadialCenter;
    float4 _DissolveRadialRange;
    float4 _DissolveSwipeCenter;
    float4 _DissolveSwipeRange;
    float4 _LightSweepCenter;
    float4 _LightSweepRange;
    float4 _HologramObjectUpVector;
    float4 _GlitchBandSizeRange;
    float4 _DissolveEdgeGradientColor0;
    float4 _DissolveEdgeGradientColor1;
    float4 _DissolveEdgeGradientColor2;
    float4 _DissolveEdgeGradientColor3;
    float4 _DissolveEdgeGradientAlphas;
    float4 _DissolveEdgeGradientAlphaTimes;
    float4 _DissolveEdgeGradientMetadata;
    half4 _BaseColor;
    half4 _Color;
    half4 _DissolveEdgeColor;
    half4 _PixelOutlineColor;
    half4 _EmissionColor;
    half4 _RimColor;
    half4 _HologramColor;
    half4 _LightSweepColor;
    half4 _RendererColor;
    half _Surface;
    half _Blend;
    half _Cutoff;
    half _AlphaMultiplier;
    half _SecondaryBlendAmount;
    half _HueShift;
    half _Saturation;
    half _Brightness;
    half _Contrast;
    half _UVFadeAxis;
    half _UVFadeOpaque;
    half _UVFadeTransparent;
    half _DissolveAmount;
    half _DissolveRadialNoiseStrength;
    float _DissolveSwipeRotation;
    half _DissolveSwipeNoiseStrength;
    half _DissolveEdgeWidth;
    half _DissolveEdgeIntensity;
    half _LightSweepAmount;
    float _LightSweepRotation;
    half _LightSweepWidth;
    half _LightSweepIntensity;
    half _DitherFade;
    half _PixelOutlineWidth;
    half _PixelOutlineAlphaThreshold;
    half _NormalScale;
    half _Metallic;
    half _Smoothness;
    half _EmissionIntensity;
    half _RimPower;
    half _RimEdgeSoftnessPixels;
    half _RimIntensity;
    half _HologramOpacity;
    half _HologramFresnelPower;
    half _HologramFresnelIntensity;
    half _HologramEdgeSoftnessPixels;
    half _HologramScanlineDensity;
    half _HologramScanlineSpeed;
    half _HologramScanlineWidth;
    half _HologramScanlineIntensity;
    half _HologramNoiseScale;
    half _HologramNoiseStrength;
    half _HologramNoiseSpeed;
    half _GlitchEnabled;
    half _GlitchStrength;
    half _GlitchRGBSplit;
    half _GlitchFrequency;
    half _GlitchSpeed;
    half _EnableExternalAlpha;
    half _Cull;
    half _SurfaceOptions, _LightingMode, _AlphaClip, _ReceiveShadows, _CastShadows, _ShadowCull, _UberQuality;
    half _SurfaceInputs, _NormalMapEnabled, _MetallicMapEnabled, _SmoothnessMapEnabled;
    half _SecondaryLayerEnabled, _ColorAdjustEnabled, _UVFadeEnabled;
    half _DissolveEnabled, _DissolveMode, _DissolveEdgeColorMode, _DitherFadeEnabled, _PixelOutlineEnabled, _EmissionEnabled, _RimEnabled, _RimBlendMode;
    half _LightSweepEnabled, _LightSweepMode, _LightSweepBlendMode;
    half _HologramEnabled, _HologramSpace;
    half _SrcBlend, _DstBlend, _SrcBlendAlpha, _DstBlendAlpha, _ZWrite, _BlendModePreserveSpecular, _AlphaToMask, _QueueControl, _QueueOffset;
CBUFFER_END

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;
TEXTURE2D(_AlphaTex);
SAMPLER(sampler_AlphaTex);
TEXTURE2D(_SecondaryTex);
SAMPLER(sampler_SecondaryTex);
float4 _SecondaryTex_TexelSize;
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);
TEXTURE2D(_SmoothnessMap);
SAMPLER(sampler_SmoothnessMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);
TEXTURE2D(_DissolveNoiseMap);
SAMPLER(sampler_DissolveNoiseMap);

struct UberSpriteSurface
{
    half3 albedo;
    half alpha;
    half rimEdge;
    half hologramEdge;
    half3 normalTS;
    half3 emission;
    float2 localUV;
    float2 baseAtlasUV;
};

inline float2 UberSpriteAtlasUV(float2 localUV, float4 uvRect, float2 texelSize)
{
    float2 inset = min(abs(texelSize) * 0.5, abs(uvRect.zw) * 0.499);
    float2 minimum = uvRect.xy + inset;
    float2 maximum = uvRect.xy + uvRect.zw - inset;
    return clamp(UberRemapUV(saturate(localUV), uvRect), minimum, maximum);
}

inline half4 UberSampleMainSprite(float2 atlasUV)
{
    half4 sampleValue = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUV);
    UNITY_BRANCH
    if (_EnableExternalAlpha > 0.5h)
        sampleValue.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, atlasUV).r;

    return sampleValue;
}

inline half4 UberSampleSpriteLayers(float2 rawUV, out float2 localUV,
    out float2 baseAtlasUV)
{
    localUV = saturate(UberNormalizeUV(rawUV, _BaseSpriteUVRect));
    baseAtlasUV = UberSpriteAtlasUV(localUV, _BaseSpriteUVRect,
        _MainTex_TexelSize.xy);
    half4 color = UberSampleMainSprite(baseAtlasUV);

#if defined(_SECONDARY_LAYER_ON)
    float2 secondaryUV = UberSpriteAtlasUV(localUV, _SecondaryUVRect,
        _SecondaryTex_TexelSize.xy);
    half4 secondary = SAMPLE_TEXTURE2D(_SecondaryTex, sampler_SecondaryTex,
        secondaryUV);
    color = lerp(color, secondary, saturate(_SecondaryBlendAmount));
#endif

    return color;
}

inline half4 UberSampleSpriteLayers(float2 rawUV)
{
    float2 localUV;
    float2 baseAtlasUV;
    return UberSampleSpriteLayers(rawUV, localUV, baseAtlasUV);
}

inline float UberSpriteGlitchHash(float2 value)
{
    return UberHash21(value);
}

inline float UberSpriteGlitchBandBoundary(float boundaryIndex, float frame,
    float averageBandSize, float bandSizeVariation)
{
    return UberEvaluateGlitchBandBoundary(boundaryIndex, frame,
        averageBandSize, bandSizeVariation);
}

inline float2 UberSpriteApplyGlitchUV(float2 rawUV,
    out half glitchActivation, out float glitchDirection)
{
    glitchActivation = 0.0h;
    glitchDirection = 1.0;
#if defined(_GLITCH_ON)
    float2 localUV = UberNormalizeUV(rawUV, _BaseSpriteUVRect);
    float spriteHeightPixels = max(abs(_BaseSpriteUVRect.w) /
        max(abs(_MainTex_TexelSize.y), 0.000001), 1.0);
    float frame = floor(_Time.y * max(_GlitchSpeed, 0.0h));
    float minBandSize = clamp(min(_GlitchBandSizeRange.x,
        _GlitchBandSizeRange.y), 1.0, 64.0);
    float maxBandSize = clamp(max(_GlitchBandSizeRange.x,
        _GlitchBandSizeRange.y), minBandSize, 64.0);
    float averageBandSize = (minBandSize + maxBandSize) * 0.5;
    float bandSizeVariation = maxBandSize - minBandSize;
    float pixelY = saturate(localUV.y) * spriteHeightPixels;
    float bandIndex = floor(pixelY / averageBandSize);
    float lowerBoundary = UberSpriteGlitchBandBoundary(bandIndex, frame,
        averageBandSize, bandSizeVariation);
    float upperBoundary = UberSpriteGlitchBandBoundary(bandIndex + 1.0, frame,
        averageBandSize, bandSizeVariation);
    bandIndex += step(upperBoundary, pixelY);
    bandIndex -= 1.0 - step(lowerBoundary, pixelY);
    float activation = step(1.0 - saturate(_GlitchFrequency),
        UberSpriteGlitchHash(float2(bandIndex, frame)));
    float direction = UberSpriteGlitchHash(float2(bandIndex + 19.19,
        frame + 47.47)) * 2.0 - 1.0;
    float shiftPixels = direction * max(_GlitchStrength, 0.0h) * activation;
    glitchActivation = (half)activation;
    glitchDirection = direction < 0.0 ? -1.0 : 1.0;
    return rawUV + float2(shiftPixels * abs(_MainTex_TexelSize.x), 0.0);
#else
    return rawUV;
#endif
}

inline half4 UberSpriteApplyGlitchRGBSplit(float2 effectUV, half4 center,
    half glitchActivation, float glitchDirection)
{
#if defined(_GLITCH_ON)
    [branch]
    if (glitchActivation > 0.0h && _GlitchRGBSplit > 0.0001h)
    {
        float splitPixels = max(_GlitchRGBSplit, 0.0h) * glitchDirection;
        float2 splitUV = float2(splitPixels * abs(_MainTex_TexelSize.x), 0.0);
        center.r = UberSampleSpriteLayers(effectUV + splitUV).r;
        center.b = UberSampleSpriteLayers(effectUV - splitUV).b;
    }
#endif
    return center;
}

inline half UberSampleSpriteLayerAlpha(float2 rawUV)
{
    float2 unclampedLocalUV = UberNormalizeUV(rawUV, _BaseSpriteUVRect);
    half inside = UberUnitUVMask(unclampedLocalUV);
    return UberSampleSpriteLayers(rawUV).a * inside;
}

inline half UberEvaluatePixelOutline(float2 rawUV, half centerAlpha)
{
#if defined(_PIXEL_OUTLINE_ON)
    half threshold = saturate(_PixelOutlineAlphaThreshold);
    half neighborAlpha = 0.0h;
    float2 texel = abs(_MainTex_TexelSize.xy);

    [unroll]
    for (int ring = 1; ring <= 4; ++ring)
    {
#if defined(_UBER_QUALITY_LOW)
        if (ring > 2)
            break;
#endif
        half ringEnabled = step((half)ring - 0.5h, _PixelOutlineWidth);
        UNITY_BRANCH
        if (ringEnabled < 0.5h)
            break;
        float2 offset = texel * (float)ring;
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV + float2(offset.x, 0.0)) * ringEnabled);
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV - float2(offset.x, 0.0)) * ringEnabled);
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV + float2(0.0, offset.y)) * ringEnabled);
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV - float2(0.0, offset.y)) * ringEnabled);

#if !defined(_UBER_QUALITY_LOW)
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV + offset) * ringEnabled);
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV - offset) * ringEnabled);
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV + float2(offset.x, -offset.y)) * ringEnabled);
        neighborAlpha = max(neighborAlpha,
            UberSampleSpriteLayerAlpha(rawUV + float2(-offset.x, offset.y)) * ringEnabled);
#endif
    }

    half centerOpaque = step(threshold, centerAlpha);
    return (1.0h - centerOpaque) * step(threshold, neighborAlpha);
#else
    return 0.0h;
#endif
}

inline half UberEvaluateUVFade(float2 rawUV)
{
#if defined(_UV_FADE_ON)
    float2 localUV = saturate(UberNormalizeUV(rawUV, _BaseSpriteUVRect));
    float coordinate = lerp(localUV.x, localUV.y, step(0.5h, _UVFadeAxis));
    return 1.0h - UberSafeInverseLerp(_UVFadeOpaque,
        _UVFadeTransparent, coordinate);
#else
    return 1.0h;
#endif
}

inline half UberEvaluateDissolve(float2 rawUV, out half edge)
{
#if defined(_DISSOLVE_ON)
    float2 localUV = saturate(UberNormalizeUV(rawUV, _BaseSpriteUVRect));
    float2 noiseUV = localUV * _DissolveTilingOffset.xy +
        _DissolveTilingOffset.zw + _DissolvePanning.xy * _Time.y;
    half noise = SAMPLE_TEXTURE2D(_DissolveNoiseMap, sampler_DissolveNoiseMap,
        noiseUV).r;

#if defined(_DISSOLVE_RADIAL)
    half radial = UberSafeInverseLerp(_DissolveRadialRange.x,
        _DissolveRadialRange.y, length(localUV - _DissolveRadialCenter.xy));
    half dissolveValue = saturate(radial + (noise - 0.5h) *
        saturate(_DissolveRadialNoiseStrength));
#elif defined(_DISSOLVE_SWIPE)
    float rotation = radians(fmod(_DissolveSwipeRotation, 360.0));
    float2 direction = float2(cos(rotation), sin(rotation));
    float projection = dot(localUV - _DissolveSwipeCenter.xy, direction);
    half swipe = UberSafeInverseLerp(_DissolveSwipeRange.x,
        _DissolveSwipeRange.y, projection);
    half dissolveValue = saturate(swipe + (noise - 0.5h) *
        saturate(_DissolveSwipeNoiseStrength));
#else
    half dissolveValue = saturate(UberSafeInverseLerp(_DissolveNoiseRange.x,
        _DissolveNoiseRange.y, noise));
#endif

    half threshold = saturate(_DissolveAmount);
    clip(dissolveValue - threshold);
    edge = 1.0h - saturate((dissolveValue - threshold) /
        max(_DissolveEdgeWidth, 0.0001h));
    return dissolveValue;
#else
    edge = 0.0h;
    return 1.0h;
#endif
}

// Forward, Universal2D, NormalsRendering, ShadowCaster, DepthOnly, and
// DepthNormals all call this exact silhouette function.
inline half UberEvaluateSpriteSilhouette(float2 effectUV, float2 sourceUV,
    half baseAlpha,
    half vertexAlpha, float4 positionCS, out half outlineMask, out half dissolveEdge)
{
    outlineMask = UberEvaluatePixelOutline(effectUV, baseAlpha);
    half alpha = max(baseAlpha, outlineMask * _PixelOutlineColor.a) *
        _BaseColor.a * vertexAlpha * saturate(_AlphaMultiplier);
    alpha *= UberEvaluateUVFade(sourceUV);

#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif

    UberEvaluateDissolve(effectUV, dissolveEdge);

#if defined(_DITHER_FADE_ON)
    clip(saturate(_DitherFade) - UberBayer4x4(positionCS.xy));
#endif

    return alpha;
}

inline half3 UberSampleSpriteNormal(float2 baseAtlasUV)
{
#if defined(_NORMALMAP)
    return UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,
        baseAtlasUV), _NormalScale);
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

inline half UberSampleSpriteMetallic(float2 baseAtlasUV)
{
#if defined(_METALLICMAP)
    return SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, baseAtlasUV).r;
#else
    return 1.0h;
#endif
}

inline half UberSampleSpriteSmoothness(float2 baseAtlasUV)
{
#if defined(_SMOOTHNESSMAP)
    return SAMPLE_TEXTURE2D(_SmoothnessMap, sampler_SmoothnessMap,
        baseAtlasUV).r;
#else
    return 1.0h;
#endif
}

inline half4 UberEvaluateDissolveEdgeGradient(float time)
{
    return UberEvaluateGradient4Keys(time, _DissolveEdgeGradientColor0,
        _DissolveEdgeGradientColor1, _DissolveEdgeGradientColor2,
        _DissolveEdgeGradientColor3, _DissolveEdgeGradientAlphas,
        _DissolveEdgeGradientAlphaTimes, _DissolveEdgeGradientMetadata);
}

inline half3 UberEvaluateDissolveEdgeMultiplier(half dissolveEdge)
{
#if defined(_DISSOLVE_EDGE_GRADIENT)
    half4 edgeColor = UberEvaluateDissolveEdgeGradient(1.0h - dissolveEdge);
    half strength = saturate(dissolveEdge *
        max(_DissolveEdgeIntensity, 0.0h) * saturate(edgeColor.a));
    return lerp(half3(1.0h, 1.0h, 1.0h), edgeColor.rgb, strength);
#else
    half strength = saturate(dissolveEdge *
        max(_DissolveEdgeIntensity, 0.0h));
    return lerp(half3(1.0h, 1.0h, 1.0h), _DissolveEdgeColor.rgb,
        strength);
#endif
}

inline half3 UberSampleSpriteEmission(float2 baseAtlasUV)
{
#if defined(_EMISSION)
    return SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap,
        baseAtlasUV).rgb * _EmissionColor.rgb * max(_EmissionIntensity, 0.0h);
#else
    return 0.0h;
#endif
}

inline half UberEvaluateSpriteHologramEdgeDirection(float2 rawUV,
    half centerAlpha, float2 direction, float edgeWidth, float edgeSoftness)
{
#if defined(_HOLOGRAM_ON) || defined(_RIM_ON)
#if defined(_UBER_QUALITY_LOW)
    const int edgeCoarseSteps = 2;
    const int edgeRefinementSteps = 2;
#else
    const int edgeCoarseSteps = 4;
    const int edgeRefinementSteps = 3;
#endif
    const half alphaDifferenceThreshold = 1.0h / 255.0h;
    float searchRadius = max(edgeWidth + max(edgeSoftness, 0.0001), 0.5);
    float2 texelDirection = abs(_MainTex_TexelSize.xy) * direction;
    float previousRadius = 0.0;
    float searchNear = 0.0;
    float searchFar = searchRadius;
    half boundaryFound = 0.0h;

    [unroll]
    for (int stepIndex = 1; stepIndex <= edgeCoarseSteps; ++stepIndex)
    {
        float sampleRadius = searchRadius * (float)stepIndex /
            (float)edgeCoarseSteps;
        half neighborAlpha = UberSampleSpriteLayerAlpha(rawUV +
            texelDirection * sampleRadius);
        half boundaryHit = step(alphaDifferenceThreshold,
            centerAlpha - neighborAlpha);
        half firstHit = boundaryHit * (1.0h - boundaryFound);
        searchNear = lerp(searchNear, previousRadius, firstHit);
        searchFar = lerp(searchFar, sampleRadius, firstHit);
        boundaryFound = max(boundaryFound, boundaryHit);
        previousRadius = sampleRadius;
    }

    [unroll]
    for (int refinement = 0; refinement < edgeRefinementSteps; ++refinement)
    {
        float middleRadius = (searchNear + searchFar) * 0.5;
        half middleAlpha = UberSampleSpriteLayerAlpha(rawUV +
            texelDirection * middleRadius);
        half middleHit = step(alphaDifferenceThreshold,
            centerAlpha - middleAlpha);
        searchFar = lerp(searchFar, middleRadius, middleHit);
        searchNear = lerp(middleRadius, searchNear, middleHit);
    }

    half hardMask = step(searchFar, edgeWidth);
    half softMask = 1.0h - smoothstep(edgeWidth, searchRadius, searchFar);
    half useSoftness = step(0.0001h, edgeSoftness);
    return boundaryFound * lerp(hardMask, softMask, useSoftness);
#else
    return 0.0h;
#endif
}

// A flat sprite has an almost constant view-angle Fresnel term. Approximate an
// atlas-safe alpha distance field so its hologram rim follows the silhouette
// and fades smoothly toward the sprite interior.
inline half UberEvaluateSpriteHologramEdge(float2 rawUV, half centerAlpha)
{
#if defined(_HOLOGRAM_ON)
    half edgeIntensity = max(_HologramFresnelIntensity, 0.0h);
    UNITY_BRANCH
    if (edgeIntensity == 0.0h)
        return 0.0h;

    float edgeWidth = clamp(_HologramFresnelPower, 0.5h, 16.0h);
    float edgeSoftness = clamp(_HologramEdgeSoftnessPixels, 0.0h, 32.0h);
    half edgeMask = 0.0h;
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(1.0, 0.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(-1.0, 0.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(0.0, 1.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(0.0, -1.0), edgeWidth, edgeSoftness));

#if !defined(_UBER_QUALITY_LOW)
    const float diagonal = 0.70710678;
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(diagonal, diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(-diagonal, -diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(diagonal, -diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(-diagonal, diagonal), edgeWidth,
        edgeSoftness));
#endif

    return saturate(edgeMask);
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

inline void UberApplySpriteHologramSurface(inout half3 albedo, inout half alpha)
{
#if defined(_HOLOGRAM_ON)
    albedo *= saturate(_HologramColor.rgb);
    alpha *= saturate(_HologramOpacity);
#endif
}

inline half3 UberEvaluateSpriteHologramEmission(half edgeMask,
    float3 positionWS, float4 positionCS)
{
#if defined(_HOLOGRAM_ON)
    float3 positionOS = TransformWorldToObject(positionWS);
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
    return _HologramColor.rgb *
        (edgeMask * max(_HologramFresnelIntensity, 0.0h) +
         scanline * max(_HologramScanlineIntensity, 0.0h));
#else
    return 0.0h;
#endif
}

// A flat sprite cannot produce a useful view-angle Fresnel term. Reuse the
// atlas-safe alpha silhouette search used by Hologram so Rim follows the
// visible sprite boundary in both Forward and Universal2D passes.
inline half UberEvaluateSpriteRimEdge(float2 rawUV, half centerAlpha)
{
#if defined(_RIM_ON)
    half rimIntensity = max(_RimIntensity, 0.0h);
    UNITY_BRANCH
    if (rimIntensity == 0.0h)
        return 0.0h;

    float edgeWidth = clamp(_RimPower, 0.5h, 16.0h);
    float edgeSoftness = clamp(_RimEdgeSoftnessPixels, 0.0h, 32.0h);
    half edgeMask = 0.0h;
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(1.0, 0.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(-1.0, 0.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(0.0, 1.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(0.0, -1.0), edgeWidth, edgeSoftness));

#if !defined(_UBER_QUALITY_LOW)
    const float diagonal = 0.70710678;
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(diagonal, diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(-diagonal, -diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(diagonal, -diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberEvaluateSpriteHologramEdgeDirection(
        rawUV, centerAlpha, float2(-diagonal, diagonal), edgeWidth,
        edgeSoftness));
#endif

    return saturate(edgeMask);
#else
    return 0.0h;
#endif
}

inline half3 UberEvaluateSpriteRim(half3 sourceColor, half edgeMask)
{
#if defined(_RIM_ON)
    half3 rimContribution = _RimColor.rgb *
        max(_RimIntensity, 0.0h) * edgeMask;
#if defined(_RIM_MULTIPLY)
    // Match Light Sweep's multiply contract: retain the shaded sprite detail
    // and modulate it with a neutral factor when the rim has no influence.
    return sourceColor * (1.0h + rimContribution);
#else
    return sourceColor + rimContribution;
#endif
#else
    return sourceColor;
#endif
}

inline void UberApplySpriteLightSweep(float2 localUV, half sourceAlpha,
    inout half3 albedo, inout half3 emission)
{
#if defined(_LIGHT_SWEEP_ON)
    // Match Dissolve Swipe's local-atlas angle convention. Amount moves the
    // highlight from Range Min to Range Max without reordering either value.
    float rotation = radians(fmod(_LightSweepRotation, 360.0));
    float2 direction = float2(cos(rotation), sin(rotation));
    float projection = dot(localUV - _LightSweepCenter.xy, direction);
    float sweepPosition = lerp(_LightSweepRange.x, _LightSweepRange.y,
        saturate(_LightSweepAmount));
    float halfWidth = max(abs(_LightSweepWidth) * 0.5,
        max(fwidth(projection), 0.0001));
    float distanceToSweep = abs(projection - sweepPosition);

#if defined(_LIGHT_SWEEP_SHARP)
    // The sharp profile keeps a solid core while retaining derivative-based
    // anti-aliasing at the two band boundaries.
    float edgeAA = max(fwidth(projection), 0.0001);
    half sweepMask = 1.0h - smoothstep(halfWidth - edgeAA,
        halfWidth + edgeAA, distanceToSweep);
#else
    // The soft profile fades smoothly from the center across the whole width.
    half sweepMask = 1.0h - smoothstep(0.0, halfWidth, distanceToSweep);
#endif

    half influence = sweepMask * saturate(sourceAlpha) *
        saturate(_LightSweepColor.a);
    half3 sweepColor = _LightSweepColor.rgb *
        max(_LightSweepIntensity, 0.0h);

#if defined(_LIGHT_SWEEP_MULTIPLY)
    // A neutral factor of one keeps Amount/Intensity zero non-destructive.
    // Multiplication modulates the source so its texture detail is preserved.
    albedo *= 1.0h + sweepColor * influence;
#else
    emission += sweepColor * influence;
#endif
#endif
}

inline void UberEvaluateSpriteSurface(float2 rawUV, half4 vertexTint,
    float4 positionCS, out UberSpriteSurface surface)
{
    half glitchActivation;
    float glitchDirection;
    float2 effectUV = UberSpriteApplyGlitchUV(rawUV, glitchActivation,
        glitchDirection);
    half4 layers = UberSampleSpriteLayers(effectUV, surface.localUV,
        surface.baseAtlasUV);
    layers = UberSpriteApplyGlitchRGBSplit(effectUV, layers,
        glitchActivation, glitchDirection);
    half3 albedo = layers.rgb * _BaseColor.rgb * vertexTint.rgb;

#if defined(_COLOR_ADJUST_ON)
    albedo = UberAdjustColor(albedo, _HueShift, _Saturation, _Brightness,
        _Contrast);
#endif

    half outlineMask;
    half dissolveEdge;
    surface.alpha = UberEvaluateSpriteSilhouette(effectUV, rawUV, layers.a,
        vertexTint.a, positionCS, outlineMask, dissolveEdge);
    surface.rimEdge = UberEvaluateSpriteRimEdge(effectUV, layers.a);
    surface.hologramEdge = UberEvaluateSpriteHologramEdge(effectUV, layers.a);
    surface.albedo = lerp(albedo, _PixelOutlineColor.rgb, outlineMask);
    UberApplySpriteHologramSurface(surface.albedo, surface.alpha);
    surface.albedo = AlphaModulate(surface.albedo, surface.alpha);
    surface.albedo *= UberEvaluateDissolveEdgeMultiplier(dissolveEdge);
    surface.normalTS = UberSampleSpriteNormal(surface.baseAtlasUV);
    surface.emission = UberSampleSpriteEmission(surface.baseAtlasUV);
    UberApplySpriteLightSweep(surface.localUV, surface.alpha, surface.albedo,
        surface.emission);
}

inline half4 UberFinalizeSpriteOutput(half4 color, half alpha)
{
    color.a = OutputAlpha(alpha, IsSurfaceTypeTransparent(_Surface));

    // Sprite output never preserves lit/specular RGB at zero alpha. Premultiply
    // mode therefore applies alpha to the complete direct output.
    if (_Surface > 0.5h && _Blend > 0.5h && _Blend < 1.5h)
        color.rgb *= color.a;

    return color;
}

struct UberSpriteAttributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_SKINNED_VERTEX_INPUTS
};

#if defined(UBER_SPRITE_FORWARD_PASS)

struct UberSpriteForwardAttributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_SKINNED_VERTEX_INPUTS
};

struct UberSpriteForwardVaryings
{
    float2 rawUV : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    half4 fogAndVertexLight : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD6;
#endif
    half4 color : COLOR;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

UberSpriteForwardVaryings UberSpriteForwardVertex(UberSpriteForwardAttributes input)
{
    UberSpriteForwardVaryings output = (UberSpriteForwardVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    SetUpSpriteInstanceProperties();
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
    const float3 normalOS = float3(0.0, 0.0, -1.0);
    const float4 tangentOS = float4(1.0, 0.0, 0.0, -1.0);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

    output.rawUV = input.uv;
    output.positionWS = positionInputs.positionWS;
    output.positionCS = positionInputs.positionCS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = half4(normalInputs.tangentWS,
        tangentOS.w * GetOddNegativeScale());
    output.color = input.color * _Color * unity_SpriteColor;
    output.fogAndVertexLight = half4(ComputeFogFactor(positionInputs.positionCS.z),
        VertexLighting(positionInputs.positionWS, normalInputs.normalWS));
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST,
        output.staticLightmapUV);
    OUTPUT_SH(normalInputs.normalWS, output.vertexSH);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(positionInputs);
#endif
    return output;
}

inline void UberInitializeSpriteInputData(UberSpriteForwardVaryings input,
    half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;
    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS,
        input.tangentWS.xyz);
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS)));
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0),
        input.fogAndVertexLight.x);
    inputData.vertexLighting = input.fogAndVertexLight.yzw;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH,
        inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

half4 UberSpriteForwardFragment(UberSpriteForwardVaryings input,
    FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    UberSpriteSurface spriteSurface;
    UberEvaluateSpriteSurface(input.rawUV, input.color, input.positionCS,
        spriteSurface);
    spriteSurface.normalTS.xy *= unity_SpriteProps.xy;

    half faceSign = (_Cull < 0.5h)
        ? IS_FRONT_VFACE(frontFace, 1.0h, -1.0h)
        : ((_Cull < 1.5h) ? -1.0h : 1.0h);
    input.normalWS *= faceSign;
    input.tangentWS.w *= faceSign;

    InputData inputData;
    UberInitializeSpriteInputData(input, spriteSurface.normalTS, inputData);
    spriteSurface.emission += UberEvaluateSpriteHologramEmission(
        spriteSurface.hologramEdge, input.positionWS, input.positionCS);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = spriteSurface.albedo;
    surfaceData.alpha = spriteSurface.alpha;
    half metallicMask = UberSampleSpriteMetallic(spriteSurface.baseAtlasUV);
    half smoothnessMask = UberSampleSpriteSmoothness(spriteSurface.baseAtlasUV);
    surfaceData.metallic = saturate(_Metallic * metallicMask);
    surfaceData.specular = 0.0h;
    surfaceData.smoothness = saturate(_Smoothness * smoothnessMask);
    surfaceData.normalTS = spriteSurface.normalTS;
    surfaceData.occlusion = 1.0h;
    surfaceData.emission = spriteSurface.emission;

#if defined(_UNLIT_ON)
    half4 color = UniversalFragmentUnlit(inputData, surfaceData);
#else
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
#endif
    color.rgb = UberEvaluateSpriteRim(color.rgb, spriteSurface.rimEdge);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return UberFinalizeSpriteOutput(color, spriteSurface.alpha);
}

#elif defined(UBER_SPRITE_2D_PASS)

struct UberSprite2DVaryings
{
    float2 rawUV : TEXCOORD0;
    half2 lightingUV : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    half3 normalWS : TEXCOORD3;
    half4 tangentWS : TEXCOORD4;
    half4 color : COLOR;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

UberSprite2DVaryings UberSprite2DVertex(UberSpriteAttributes input)
{
    UberSprite2DVaryings output = (UberSprite2DVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    SetUpSpriteInstanceProperties();
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
    const float3 normalOS = float3(0.0, 0.0, -1.0);
    const float4 tangentOS = float4(1.0, 0.0, 0.0, -1.0);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);

    output.rawUV = input.uv;
    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = half4(normalInputs.tangentWS,
        tangentOS.w * GetOddNegativeScale());
    output.lightingUV = half2(ComputeScreenPos(output.positionCS /
        output.positionCS.w).xy);
    output.color = input.color * _Color * unity_SpriteColor;
    return output;
}

half4 UberSprite2DFragment(UberSprite2DVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    UberSpriteSurface spriteSurface;
    UberEvaluateSpriteSurface(input.rawUV, input.color, input.positionCS,
        spriteSurface);
    spriteSurface.normalTS.xy *= unity_SpriteProps.xy;
    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS,
        input.tangentWS.xyz);
    half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(
        spriteSurface.normalTS,
        half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS)));
    spriteSurface.emission += UberEvaluateSpriteHologramEmission(
        spriteSurface.hologramEdge, input.positionWS, input.positionCS);

#if defined(_UNLIT_ON)
    half4 color = half4(spriteSurface.albedo, spriteSurface.alpha);
#else
    SurfaceData2D surfaceData;
    InputData2D inputData;
    InitializeSurfaceData(spriteSurface.albedo, spriteSurface.alpha,
        half4(1.0h, 1.0h, 1.0h, 1.0h), spriteSurface.normalTS, surfaceData);
    InitializeInputData(input.rawUV, input.lightingUV, inputData);
#if defined(DEBUG_DISPLAY)
    surfaceData.normalWS = normalWS;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
#endif
    half4 color = CombinedShapeLightShared(surfaceData, inputData);
#endif

    color.rgb += spriteSurface.emission;
    color.rgb = UberEvaluateSpriteRim(color.rgb, spriteSurface.rimEdge);
    return UberFinalizeSpriteOutput(color, spriteSurface.alpha);
}

#elif defined(UBER_SPRITE_NORMALS_PASS) || defined(UBER_SPRITE_DEPTH_NORMALS_PASS)

struct UberSpriteNormalsVaryings
{
    float2 rawUV : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    half3 tangentWS : TEXCOORD2;
    half3 bitangentWS : TEXCOORD3;
    half4 color : COLOR;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

UberSpriteNormalsVaryings UberSpriteNormalsVertex(UberSpriteAttributes input)
{
    UberSpriteNormalsVaryings output = (UberSpriteNormalsVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    SetUpSpriteInstanceProperties();
    UNITY_SKINNED_VERTEX_COMPUTE(input);
    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    const float3 normalOS = float3(0.0, 0.0, -1.0);
    const float4 tangentOS = float4(1.0, 0.0, 0.0, -1.0);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);
    output.rawUV = input.uv;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = normalInputs.tangentWS;
    output.bitangentWS = normalInputs.bitangentWS;
    output.color = input.color * _Color * unity_SpriteColor;
    return output;
}

inline half UberEvaluateNormalsSilhouette(UberSpriteNormalsVaryings input,
    out float2 baseAtlasUV)
{
    half glitchActivation;
    float glitchDirection;
    float2 effectUV = UberSpriteApplyGlitchUV(input.rawUV, glitchActivation,
        glitchDirection);
    float2 localUV;
    half4 layers = UberSampleSpriteLayers(effectUV, localUV, baseAtlasUV);
    half outlineMask;
    half dissolveEdge;
    return UberEvaluateSpriteSilhouette(effectUV, input.rawUV, layers.a,
        input.color.a, input.positionCS, outlineMask, dissolveEdge);
}

#if defined(UBER_SPRITE_NORMALS_PASS)
half4 UberSpriteNormalsFragment(UberSpriteNormalsVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 baseAtlasUV;
    half alpha = UberEvaluateNormalsSilhouette(input, baseAtlasUV);
    half3 normalTS = UberSampleSpriteNormal(baseAtlasUV);
    return NormalsRenderingShared(half4(1.0h, 1.0h, 1.0h, alpha), normalTS,
        input.tangentWS, input.bitangentWS, input.normalWS);
}
#else
half4 UberSpriteDepthNormalsFragment(UberSpriteNormalsVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 baseAtlasUV;
    UberEvaluateNormalsSilhouette(input, baseAtlasUV);
    half3 normalTS = UberSampleSpriteNormal(baseAtlasUV);
    normalTS.xy *= unity_SpriteProps.xy;
    half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS, input.bitangentWS, input.normalWS)));

#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormal = PackNormalOctQuadEncode(normalWS);
    half3 packedNormal = PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
    return half4(packedNormal, 0.0h);
#else
    return half4(normalWS, 0.0h);
#endif
}
#endif

#elif defined(UBER_SPRITE_SHADOW_PASS) || defined(UBER_SPRITE_DEPTH_PASS)

#if defined(UBER_SPRITE_SHADOW_PASS)
float3 _LightDirection;
float3 _LightPosition;
#endif

struct UberSpriteSilhouetteVaryings
{
    float2 rawUV : TEXCOORD0;
    half vertexAlpha : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

inline void UberInitializeSilhouetteVertex(UberSpriteAttributes input,
    out UberSpriteAttributes preparedInput, out half4 tint)
{
    preparedInput = input;
    SetUpSpriteInstanceProperties();
    UNITY_SKINNED_VERTEX_COMPUTE(preparedInput);
    preparedInput.positionOS = UnityFlipSprite(preparedInput.positionOS,
        unity_SpriteProps.xy);
    tint = preparedInput.color * _Color * unity_SpriteColor;
}

#if defined(UBER_SPRITE_SHADOW_PASS)
UberSpriteSilhouetteVaryings UberSpriteShadowVertex(UberSpriteAttributes input)
{
    UberSpriteSilhouetteVaryings output = (UberSpriteSilhouetteVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UberSpriteAttributes preparedInput;
    half4 tint;
    UberInitializeSilhouetteVertex(input, preparedInput, tint);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(preparedInput.positionOS);
    float3 normalWS = TransformObjectToWorldNormal(float3(0.0, 0.0, -1.0));
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS,
        normalWS, lightDirectionWS));
    output.positionCS = ApplyShadowClamping(output.positionCS);
    output.rawUV = preparedInput.uv;
    output.vertexAlpha = tint.a;
    return output;
}
#else
UberSpriteSilhouetteVaryings UberSpriteDepthVertex(UberSpriteAttributes input)
{
    UberSpriteSilhouetteVaryings output = (UberSpriteSilhouetteVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UberSpriteAttributes preparedInput;
    half4 tint;
    UberInitializeSilhouetteVertex(input, preparedInput, tint);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(preparedInput.positionOS);
    output.rawUV = preparedInput.uv;
    output.vertexAlpha = tint.a;
    return output;
}
#endif

half4 UberSpriteSilhouetteFragment(UberSpriteSilhouetteVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half glitchActivation;
    float glitchDirection;
    float2 effectUV = UberSpriteApplyGlitchUV(input.rawUV, glitchActivation,
        glitchDirection);
    half4 layers = UberSampleSpriteLayers(effectUV);
    half outlineMask;
    half dissolveEdge;
    UberEvaluateSpriteSilhouette(effectUV, input.rawUV, layers.a,
        input.vertexAlpha, input.positionCS, outlineMask, dissolveEdge);
    return 0.0h;
}

#endif

#endif // UBER_SPRITE_INCLUDED
