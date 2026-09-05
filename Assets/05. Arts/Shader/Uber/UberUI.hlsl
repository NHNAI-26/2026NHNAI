#ifndef UBER_UI_INCLUDED
#define UBER_UI_INCLUDED

#include "UberCommon.hlsl"

TEXTURE2D(_TintMask);
SAMPLER(sampler_TintMask);
TEXTURE2D(_GrayscaleMask);
SAMPLER(sampler_GrayscaleMask);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_DissolveNoiseMap);
SAMPLER(sampler_DissolveNoiseMap);

// Keep this layout unconditional so every local variant is SRP Batcher stable.
CBUFFER_START(UnityPerMaterial)
    half _TintMaskEnabled;
    half _TintMaskStrength;
    half _TintMaskInvert;
    half _GrayscaleEnabled;
    half _GrayscaleStrength;
    half _GrayscaleInvert;
    half _EmissionEnabled;
    half4 _EmissionColor;
    half _EmissionIntensity;
    half _SurfaceInputs;
    half4 _Color;
    half _AlphaMultiplier;
    half _UberQuality;
    half _ColorAdjustEnabled;
    half _HueShift;
    half _Saturation;
    half _Brightness;
    half _Contrast;
    half _RGBOverrideEnabled;
    half4 _RGBOverrideColor;
    half _RGBOverrideStrength;
    half _UVFadeEnabled;
    float4 _UVFadeDirection;
    half _UVFadeStart;
    half _UVFadeEnd;
    half _DissolveEnabled;
    half _DissolveMode;
    float4 _DissolveTilingOffset;
    float4 _DissolvePanning;
    float4 _DissolveNoiseRange;
    float4 _DissolveRadialCenter;
    float4 _DissolveRadialRange;
    float4 _DissolveSwipeCenter;
    float4 _DissolveSwipeRange;
    float4 _LightSweepCenter;
    float4 _LightSweepRange;
    half _DissolveAmount;
    half _DissolveRadialNoiseStrength;
    float _DissolveSwipeRotation;
    half _DissolveSwipeNoiseStrength;
    half _DissolveEdgeWidth;
    half _DissolveEdgeColorMode;
    half4 _DissolveEdgeColor;
    float4 _DissolveEdgeGradientColor0;
    float4 _DissolveEdgeGradientColor1;
    float4 _DissolveEdgeGradientColor2;
    float4 _DissolveEdgeGradientColor3;
    float4 _DissolveEdgeGradientAlphas;
    float4 _DissolveEdgeGradientAlphaTimes;
    float4 _DissolveEdgeGradientMetadata;
    half _DissolveEdgeIntensity;
    half4 _LightSweepColor;
    half _LightSweepAmount;
    float _LightSweepRotation;
    half _LightSweepWidth;
    half _LightSweepIntensity;
    half _LightSweepEnabled;
    half _LightSweepMode;
    half _LightSweepBlendMode;
    half _DitherFadeEnabled;
    half _DitherFade;
    half _PixelOutlineEnabled;
    half4 _PixelOutlineColor;
    half _PixelOutlineWidth;
    half _PixelOutlineAlphaThreshold;
    half4 _PixelGlowColor;
    half _PixelGlowWidth;
    half _PixelGlowIntensity;
    float4 _HologramObjectUpVector;
    half4 _HologramColor;
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
    float4 _GlitchBandSizeRange;
    half _HologramEnabled;
    half _HologramSpace;
    float4 _BaseSpriteUVRect;
    float4 _PixelOutlineMeshPadding;
    half _StencilOptions;
    half _StencilComp, _Stencil, _StencilOp;
    half _StencilWriteMask, _StencilReadMask, _ColorMask;
    half _UseUIAlphaClip;
    half _Cutoff;
CBUFFER_END

// CanvasRenderer owns these values per draw. They must not become material state.
float4 _MainTex_TexelSize;
half4 _TextureSampleAdd;
float4 _ClipRect;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;

struct UberUIAttributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UberUIVaryings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
#if defined(UNITY_UI_CLIP_RECT)
    half4 mask : TEXCOORD1;
#endif
    float3 positionWS : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

UberUIVaryings UberUIVert(UberUIAttributes input)
{
    UberUIVaryings output = (UberUIVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if defined(_PIXEL_OUTLINE_ON)
    // The binder supplies non-zero padding only for a rectangular Simple Image.
    float2 localUV = UberNormalizeUV(input.uv, _BaseSpriteUVRect);
    float2 expandDirection = sign(localUV - 0.5);
    input.positionOS.xy += expandDirection * _PixelOutlineMeshPadding.xy;
    input.uv += expandDirection * _PixelOutlineMeshPadding.zw;
#endif

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.uv = input.uv;
    output.color = input.color * _Color;

#if defined(UNITY_UI_CLIP_RECT)
    float2 pixelSize = output.positionCS.w;
    pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
    output.mask = half4(
        input.positionOS.xy * 2.0 - clampedRect.xy - clampedRect.zw,
        0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) +
        abs(pixelSize.xy)));
#endif

    return output;
}

inline half4 UberSampleUISprite(float2 uv)
{
    float2 localUV = UberNormalizeUV(uv, _BaseSpriteUVRect);
    half inside = UberUnitUVMask(localUV);
    float2 safeUV = UberClampUV(uv, _BaseSpriteUVRect);
    return (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, safeUV) +
        _TextureSampleAdd) * inside;
}

inline half UberSampleUISpriteAlpha(float2 uv)
{
    return UberSampleUISprite(uv).a;
}

inline half UberUIUVFade(float2 uv)
{
    float2 localUV = saturate(UberNormalizeUV(uv, _BaseSpriteUVRect));
    float2 direction = _UVFadeDirection.xy;
    direction /= max(length(direction), 0.0001);
    float coordinate = dot(localUV - 0.5, direction) + 0.5;
    return UberSafeInverseLerp(_UVFadeStart, _UVFadeEnd, coordinate);
}

inline half UberUIDissolve(float2 uv, out half edge)
{
#if defined(_DISSOLVE_ON)
    float2 localUV = saturate(UberNormalizeUV(uv, _BaseSpriteUVRect));
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

inline half4 UberUIEvaluateDissolveEdgeGradient(float time)
{
    return UberEvaluateGradient4Keys(time, _DissolveEdgeGradientColor0,
        _DissolveEdgeGradientColor1, _DissolveEdgeGradientColor2,
        _DissolveEdgeGradientColor3, _DissolveEdgeGradientAlphas,
        _DissolveEdgeGradientAlphaTimes, _DissolveEdgeGradientMetadata);
}

inline half3 UberUIEvaluateDissolveEdgeMultiplier(half dissolveEdge)
{
#if defined(_DISSOLVE_EDGE_GRADIENT)
    half4 edgeColor = UberUIEvaluateDissolveEdgeGradient(
        1.0h - dissolveEdge);
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

inline void UberUIApplyLightSweep(float2 localUV, half sourceAlpha,
    inout half3 albedo, inout half3 emission)
{
#if defined(_LIGHT_SWEEP_ON)
    // Match Dissolve Swipe's atlas-local angle convention. Amount moves the
    // highlight from Range Min to Range Max without changing either endpoint.
    float rotation = radians(fmod(_LightSweepRotation, 360.0));
    float2 direction = float2(cos(rotation), sin(rotation));
    float projection = dot(localUV - _LightSweepCenter.xy, direction);
    float sweepPosition = lerp(_LightSweepRange.x, _LightSweepRange.y,
        saturate(_LightSweepAmount));
    float halfWidth = max(abs(_LightSweepWidth) * 0.5,
        max(fwidth(projection), 0.0001));
    float distanceToSweep = abs(projection - sweepPosition);

#if defined(_LIGHT_SWEEP_SHARP)
    // Sharp keeps a solid core and anti-aliases only the two band boundaries.
    float edgeAA = max(fwidth(projection), 0.0001);
    half sweepMask = 1.0h - smoothstep(halfWidth - edgeAA,
        halfWidth + edgeAA, distanceToSweep);
#else
    // Soft fades smoothly from the center across the entire band width.
    half sweepMask = 1.0h - smoothstep(0.0, halfWidth, distanceToSweep);
#endif

    half influence = sweepMask * saturate(sourceAlpha) *
        saturate(_LightSweepColor.a);
    half3 sweepColor = _LightSweepColor.rgb *
        max(_LightSweepIntensity, 0.0h);

#if defined(_LIGHT_SWEEP_MULTIPLY)
    // One is the neutral factor, so zero intensity remains non-destructive.
    albedo *= 1.0h + sweepColor * influence;
#else
    emission += sweepColor * influence;
#endif
#endif
}

inline half UberUIEvaluateHologramEdgeDirection(float2 rawUV,
    half centerAlpha, float2 direction, float edgeWidth, float edgeSoftness)
{
#if defined(_HOLOGRAM_ON)
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
        half neighborAlpha = UberSampleUISpriteAlpha(rawUV +
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
        half middleAlpha = UberSampleUISpriteAlpha(rawUV +
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

// UI has no useful view-angle Fresnel term. Follow the atlas-safe alpha
// silhouette just like the 2D Sprite hologram and smooth toward the interior.
inline half UberUIEvaluateHologramEdge(float2 rawUV, half centerAlpha)
{
#if defined(_HOLOGRAM_ON)
    half edgeIntensity = max(_HologramFresnelIntensity, 0.0h);
    UNITY_BRANCH
    if (edgeIntensity == 0.0h)
        return 0.0h;

    float edgeWidth = clamp(_HologramFresnelPower, 0.5h, 16.0h);
    float edgeSoftness = clamp(_HologramEdgeSoftnessPixels, 0.0h, 32.0h);
    half edgeMask = 0.0h;
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(1.0, 0.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(-1.0, 0.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(0.0, 1.0), edgeWidth, edgeSoftness));
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(0.0, -1.0), edgeWidth, edgeSoftness));

#if !defined(_UBER_QUALITY_LOW)
    const float diagonal = 0.70710678;
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(diagonal, diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(-diagonal, -diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(diagonal, -diagonal), edgeWidth,
        edgeSoftness));
    edgeMask = max(edgeMask, UberUIEvaluateHologramEdgeDirection(
        rawUV, centerAlpha, float2(-diagonal, diagonal), edgeWidth,
        edgeSoftness));
#endif

    return saturate(edgeMask);
#else
    return 0.0h;
#endif
}

inline float3 UberUIGetHologramUpVector()
{
    return UberSafeNormalizeFinite3(_HologramObjectUpVector.xyz,
        float3(0.0, 1.0, 0.0));
}

inline float UberUIHologramValueNoise(float coordinate)
{
    return UberValueNoise1D(coordinate);
}

inline float UberUIGlitchHash(float2 value)
{
    return UberHash21(value);
}

inline float UberUIGlitchBandBoundary(float boundaryIndex, float frame,
    float averageBandSize, float bandSizeVariation)
{
    return UberEvaluateGlitchBandBoundary(boundaryIndex, frame,
        averageBandSize, bandSizeVariation);
}

inline float2 UberUIApplyGlitchUV(float2 rawUV,
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
    float lowerBoundary = UberUIGlitchBandBoundary(bandIndex, frame,
        averageBandSize, bandSizeVariation);
    float upperBoundary = UberUIGlitchBandBoundary(bandIndex + 1.0, frame,
        averageBandSize, bandSizeVariation);
    bandIndex += step(upperBoundary, pixelY);
    bandIndex -= 1.0 - step(lowerBoundary, pixelY);
    float activation = step(1.0 - saturate(_GlitchFrequency),
        UberUIGlitchHash(float2(bandIndex, frame)));
    float direction = UberUIGlitchHash(float2(bandIndex + 19.19,
        frame + 47.47)) * 2.0 - 1.0;
    float shiftPixels = direction * max(_GlitchStrength, 0.0h) *
        activation;
    glitchActivation = (half)activation;
    glitchDirection = direction < 0.0 ? -1.0 : 1.0;
    return rawUV + float2(shiftPixels * abs(_MainTex_TexelSize.x), 0.0);
#else
    return rawUV;
#endif
}

inline half4 UberUIApplyGlitchRGBSplit(float2 effectUV, half4 center,
    half glitchActivation, float glitchDirection)
{
#if defined(_GLITCH_ON)
    [branch]
    if (glitchActivation > 0.0h && _GlitchRGBSplit > 0.0001h)
    {
        float splitPixels = max(_GlitchRGBSplit, 0.0h) *
            glitchDirection;
        float2 splitUV = float2(splitPixels * abs(_MainTex_TexelSize.x), 0.0);
        center.r = UberSampleUISprite(effectUV + splitUV).r;
        center.b = UberSampleUISprite(effectUV - splitUV).b;
    }
#endif
    return center;
}

inline float UberUIGetHologramCoordinate(float2 localUV,
    float3 positionWS, float4 positionCS)
{
#if defined(_HOLOGRAM_SCREEN_SPACE)
    return GetNormalizedScreenSpaceUV(positionCS).y;
#elif defined(_HOLOGRAM_WORLD_SPACE)
    return positionWS.y;
#else
    // RectTransform vertices are commonly authored in pixels. Normalize the
    // local plane so Object mode keeps Sprite-like scanline density at any UI size.
    return dot(float3(localUV - 0.5, 0.0), UberUIGetHologramUpVector());
#endif
}

inline void UberUIApplyHologramSurface(inout half4 color)
{
#if defined(_HOLOGRAM_ON)
    color.rgb *= saturate(_HologramColor.rgb);
    color.a *= saturate(_HologramOpacity);
#endif
}

inline half3 UberUIEvaluateHologramEmission(half edgeMask, float2 rawUV,
    float3 positionWS, float4 positionCS)
{
#if defined(_HOLOGRAM_ON)
    float2 localUV = saturate(UberNormalizeUV(rawUV, _BaseSpriteUVRect));
    float coordinate = UberUIGetHologramCoordinate(localUV, positionWS,
        positionCS);
    float noiseCoordinate = coordinate * max(abs(_HologramNoiseScale),
        0.0001) + _Time.y * _HologramNoiseSpeed;
    float noise = UberUIHologramValueNoise(noiseCoordinate) * 2.0 - 1.0;
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

inline void UberUIOutlineMasks(float2 uv, half centerAlpha,
    out half outline, out half glow)
{
    outline = 0.0h;
    glow = 0.0h;
#if defined(_PIXEL_OUTLINE_ON)
    half threshold = saturate(_PixelOutlineAlphaThreshold);
    half outside = 1.0h - step(threshold, centerAlpha);
    half outlineWidth = min(max(_PixelOutlineWidth, 0.0h), 4.0h);
    half glowWidth = min(max(_PixelGlowWidth, 0.0h), 8.0h);

    [unroll]
    for (int ring = 1; ring <= 8; ring++)
    {
        half outlineRingEnabled = step((half)ring - 0.5h, outlineWidth);
        half glowEnabled = step((half)ring - 0.5h, glowWidth);
        UNITY_BRANCH
        if (max(outlineRingEnabled, glowEnabled) < 0.5h)
            break;

        float2 offset = _MainTex_TexelSize.xy * (float)ring;
        half ringAlpha = 0.0h;
        ringAlpha = max(ringAlpha, UberSampleUISpriteAlpha(uv + float2(offset.x, 0.0)));
        ringAlpha = max(ringAlpha, UberSampleUISpriteAlpha(uv - float2(offset.x, 0.0)));
        ringAlpha = max(ringAlpha, UberSampleUISpriteAlpha(uv + float2(0.0, offset.y)));
        ringAlpha = max(ringAlpha, UberSampleUISpriteAlpha(uv - float2(0.0, offset.y)));
        ringAlpha = max(ringAlpha, UberSampleUISpriteAlpha(uv + offset));
        ringAlpha = max(ringAlpha, UberSampleUISpriteAlpha(uv - offset));
        ringAlpha = max(ringAlpha,
            UberSampleUISpriteAlpha(uv + float2(offset.x, -offset.y)));
        ringAlpha = max(ringAlpha,
            UberSampleUISpriteAlpha(uv + float2(-offset.x, offset.y)));

        half ringMask = outside * step(threshold, ringAlpha);
        outline = max(outline,
            ringMask * outlineRingEnabled);
        half glowFalloff = saturate(1.0h - (half)ring / (glowWidth + 1.0h));
        glow = max(glow, ringMask * glowEnabled * glowFalloff);
    }

    glow *= 1.0h - outline;
#endif
}

inline void UberUICompositeStraightAlpha(
    inout half4 color, half3 layerRGB, half layerAlpha)
{
    layerAlpha = saturate(layerAlpha);
    half outputAlpha = layerAlpha + color.a * (1.0h - layerAlpha);
    half3 premultipliedRGB = layerRGB * layerAlpha +
        color.rgb * color.a * (1.0h - layerAlpha);
    color.rgb = premultipliedRGB / max(outputAlpha, 0.0001h);
    color.a = outputAlpha;
}

half4 UberUIFrag(UberUIVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half glitchActivation;
    float glitchDirection;
    float2 effectUV = UberUIApplyGlitchUV(input.uv,
        glitchActivation, glitchDirection);
    half4 sprite = UberSampleUISprite(effectUV);
    sprite = UberUIApplyGlitchRGBSplit(effectUV, sprite,
        glitchActivation, glitchDirection);
    half hologramEdge = UberUIEvaluateHologramEdge(effectUV, sprite.a);
    half4 color = sprite * input.color;
#if defined(_TINT_MASK_ON)
    float2 tintUV = saturate(UberNormalizeUV(effectUV, _BaseSpriteUVRect));
    half tintMask = SAMPLE_TEXTURE2D(_TintMask, sampler_TintMask, tintUV).r;
    half tintInfluence = saturate(_TintMaskStrength) *
        lerp(saturate(tintMask), 1.0h - saturate(tintMask),
            step(0.5h, _TintMaskInvert));
    color.rgb = lerp(sprite.rgb, color.rgb, tintInfluence);
#endif
    color.a *= saturate(_AlphaMultiplier);

#if defined(_COLOR_ADJUST_ON)
    color.rgb = UberAdjustColor(
        color.rgb, _HueShift, _Saturation, _Brightness, _Contrast);
#endif

#if defined(_RGB_OVERRIDE_ON)
    half overrideStrength = saturate(_RGBOverrideStrength);
    half3 overrideRGB = _RGBOverrideColor.rgb * input.color.rgb;
    color.rgb = lerp(color.rgb, overrideRGB, overrideStrength);
    color.a *= lerp(1.0h, _RGBOverrideColor.a, overrideStrength);
#endif

#if defined(_GRAYSCALE_ON)
    float2 grayscaleUV = saturate(UberNormalizeUV(effectUV, _BaseSpriteUVRect));
    half grayscaleMask = SAMPLE_TEXTURE2D(_GrayscaleMask,
        sampler_GrayscaleMask, grayscaleUV).r;
    color.rgb = UberApplyGrayscaleMask(color.rgb, grayscaleMask,
        _GrayscaleStrength, _GrayscaleInvert);
#endif

    half silhouette = 1.0h;
#if defined(_UV_FADE_ON)
    silhouette *= UberUIUVFade(input.uv);
#endif

#if defined(_DISSOLVE_ON)
    half dissolveEdge;
    UberUIDissolve(effectUV, dissolveEdge);
    color.rgb *= UberUIEvaluateDissolveEdgeMultiplier(dissolveEdge);
#endif

#if defined(_EMISSION)
    float2 emissionUV = saturate(UberNormalizeUV(effectUV, _BaseSpriteUVRect));
    color.rgb += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap,
        emissionUV).rgb * _EmissionColor.rgb * max(_EmissionIntensity, 0.0h);
#endif

    half outline;
    half glow;
    UberUIOutlineMasks(effectUV, sprite.a, outline, glow);
    half outlineAlpha = outline * _PixelOutlineColor.a * input.color.a *
        _AlphaMultiplier;
    half glowAlpha = glow * _PixelGlowColor.a * input.color.a *
        _AlphaMultiplier;
    UberUICompositeStraightAlpha(color,
        _PixelGlowColor.rgb * _PixelGlowIntensity, glowAlpha);
    UberUICompositeStraightAlpha(color, _PixelOutlineColor.rgb, outlineAlpha);
    UberUIApplyHologramSurface(color);
    half3 emission = UberUIEvaluateHologramEmission(hologramEdge,
        effectUV, input.positionWS, input.positionCS);
    float2 localEffectUV = saturate(UberNormalizeUV(effectUV,
        _BaseSpriteUVRect));
    UberUIApplyLightSweep(localEffectUV, color.a, color.rgb, emission);
    color.rgb += emission;
    color.a *= silhouette;

#if defined(_DITHER_FADE_ON)
    clip(saturate(_DitherFade) - UberBayer4x4(input.positionCS.xy));
#endif

#if defined(UNITY_UI_CLIP_RECT)
    half2 mask = saturate(
        (_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
    color.a *= mask.x * mask.y;
#endif

#if defined(UNITY_UI_ALPHACLIP)
    clip(color.a - _Cutoff);
#endif

    return color;
}

#endif // UBER_UI_INCLUDED
