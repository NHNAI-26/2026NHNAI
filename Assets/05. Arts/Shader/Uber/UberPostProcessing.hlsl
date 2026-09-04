#ifndef UBER_POST_PROCESSING_INCLUDED
#define UBER_POST_PROCESSING_INCLUDED

#include "UberCommon.hlsl"

TEXTURE2D(_AsciiFontAtlas);
SAMPLER(sampler_AsciiFontAtlas);

// Keep every material property unconditional so all local variants share one layout.
CBUFFER_START(UnityPerMaterial)
    float _ScreenFilterOptions;
    float _ScreenFilterMode;
    float _PixelSize;
    float _HueShift;
    float _Saturation;
    float _Brightness;
    float _Contrast;
    float4 _ColorScreen;
    float _BlendStrength;
    float _DitherStrength;
    float _ColorLevels;
    float4 _GradientShadowColor;
    float4 _GradientMidtoneColor;
    float4 _GradientHighlightColor;
    float _GradientMidpoint;
    float _GradientStrength;
    float4 _OldFilmTint;
    float _OldFilmSepia;
    float _OldFilmGrain;
    float _OldFilmScratch;
    float _OldFilmFlicker;
    float _OldFilmJitter;
    float _OldFilmVignette;
    float4 _EdgeColor;
    float4 _EdgeBackgroundColor;
    float _EdgeThreshold;
    float _EdgeWidth;
    float _EdgeStrength;
    float _EdgeSourceMix;
    float _AsciiFontReady;
    float _AsciiSdfThreshold;
    float _AsciiSdfSoftness;
    float4 _AsciiGlyphUV0, _AsciiGlyphUV1, _AsciiGlyphUV2, _AsciiGlyphUV3;
    float4 _AsciiGlyphUV4, _AsciiGlyphUV5, _AsciiGlyphUV6, _AsciiGlyphUV7;
    float4 _AsciiGlyphUV8, _AsciiGlyphUV9, _AsciiGlyphUV10, _AsciiGlyphUV11;
    float4 _AsciiGlyphUV12, _AsciiGlyphUV13, _AsciiGlyphUV14;
    float4 _AsciiGlyphPlacement0, _AsciiGlyphPlacement1, _AsciiGlyphPlacement2;
    float4 _AsciiGlyphPlacement3, _AsciiGlyphPlacement4, _AsciiGlyphPlacement5;
    float4 _AsciiGlyphPlacement6, _AsciiGlyphPlacement7, _AsciiGlyphPlacement8;
    float4 _AsciiGlyphPlacement9, _AsciiGlyphPlacement10, _AsciiGlyphPlacement11;
    float4 _AsciiGlyphPlacement12, _AsciiGlyphPlacement13, _AsciiGlyphPlacement14;
    float _AsciiCellSize;
    float _AsciiCharacterSpacing;
    float _AsciiLineSpacing;
    float4 _AsciiForegroundColor;
    float4 _AsciiBackgroundColor;
    float _AsciiSourceColor;
    float _AsciiInvert;
    float _CRTStrength;
    float _CRTCurvature;
    float _CRTScanlineDensity;
    float _CRTScanlineStrength;
    float _CRTMaskScale;
    float _CRTMaskStrength;
    float _CRTChromaticAberration;
    float _CRTVignetteStrength;
    float _CRTSignalNoise;
    float _CRTHorizontalJitter;
    float _CRTRollingBand;
    float _CRTAnimationSpeed;
    float _CRTPowerOffAmount;
    float _CRTPowerBloomIntensity;
CBUFFER_END

inline float2 UberPostPixelGridUV(float2 uv)
{
    float pixelSize = round(abs(_PixelSize));
    pixelSize = pixelSize == pixelSize ? clamp(pixelSize, 1.0, 4096.0) : 1.0;
    float2 blockSize = max(
        abs(_BlitTexture_TexelSize.xy) * pixelSize, float2(0.000001, 0.000001));
    return (floor(uv / blockSize) + 0.5) * blockSize;
}

inline float4 UberPostSampleSource(float2 uv)
{
#if defined(_PIXELATION_ON)
    return SAMPLE_TEXTURE2D_X_LOD(
        _BlitTexture, sampler_PointClamp, UberPostPixelGridUV(uv), _BlitMipLevel);
#else
    return SAMPLE_TEXTURE2D_X_LOD(
        _BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
#endif
}

inline float4 UberPostSampleLinear(float2 uv)
{
    return SAMPLE_TEXTURE2D_X_LOD(
        _BlitTexture, sampler_LinearClamp, saturate(uv), _BlitMipLevel);
}

inline float UberPostLuminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

inline float3 UberPostScreenBlend(float3 color)
{
    float3 screenColor = 1.0 - (1.0 - color) * (1.0 - _ColorScreen.rgb);
    return lerp(color, screenColor, saturate(_BlendStrength));
}

inline float3 UberPostOrderedDither(float3 color, float2 pixelPosition)
{
    float centeredThreshold = UberBayer4x4(pixelPosition) - 0.5;
    return color + centeredThreshold * saturate(_DitherStrength) / 16.0;
}

inline float3 UberPostQuantize(float3 color)
{
    float levelCount = round(abs(_ColorLevels));
    levelCount = levelCount == levelCount ? clamp(levelCount, 2.0, 256.0) : 2.0;
    float stepCount = max(levelCount - 1.0, 1.0);
    return round(saturate(color) * stepCount) / stepCount;
}

inline float3 UberPostGradientMap(float3 color)
{
    float luminance = saturate(UberPostLuminance(color));
    float midpoint = clamp(_GradientMidpoint, 0.0001, 0.9999);
    float3 lower = lerp(_GradientShadowColor.rgb, _GradientMidtoneColor.rgb,
        saturate(luminance / midpoint));
    float3 upper = lerp(_GradientMidtoneColor.rgb, _GradientHighlightColor.rgb,
        saturate((luminance - midpoint) / max(1.0 - midpoint, 0.0001)));
    float3 mapped = luminance < midpoint ? lower : upper;
    return lerp(color, mapped, saturate(_GradientStrength));
}

inline float UberPostHash21(float2 value)
{
    return frac(sin(dot(value, float2(41.37, 289.11))) * 951.1357);
}

inline float3 UberPostOldFilm(float2 uv, float2 pixelPosition)
{
    float frame = floor(_Time.y * 12.0);
    float jitter = (UberPostHash21(float2(frame, 17.0)) * 2.0 - 1.0) *
        max(abs(_OldFilmJitter), 0.0) * abs(_BlitTexture_TexelSize.x);
    float3 color = UberPostSampleLinear(uv + float2(jitter, 0.0)).rgb;

    float3 sepia = float3(
        dot(color, float3(0.393, 0.769, 0.189)),
        dot(color, float3(0.349, 0.686, 0.168)),
        dot(color, float3(0.272, 0.534, 0.131)));
    color = lerp(color, sepia, saturate(_OldFilmSepia));
    color = lerp(color, color * _OldFilmTint.rgb, saturate(_OldFilmTint.a));

    float grain = UberPostHash21(floor(pixelPosition) + frame * 0.73) - 0.5;
    color += grain * max(abs(_OldFilmGrain), 0.0);

    float scratchColumn = floor(saturate(uv.x) * 320.0);
    float scratchSeed = UberPostHash21(float2(scratchColumn, floor(_Time.y * 2.0)));
    float scratch = smoothstep(0.985, 0.998, scratchSeed) *
        (0.35 + 0.65 * UberPostHash21(float2(scratchColumn, frame)));
    color += scratch * saturate(_OldFilmScratch) * 0.35;

    float flicker = (UberPostHash21(float2(frame, 83.0)) * 2.0 - 1.0) *
        max(abs(_OldFilmFlicker), 0.0);
    color *= max(1.0 + flicker, 0.0);

    float2 centeredUV = uv * 2.0 - 1.0;
    float vignette = saturate(1.0 - dot(centeredUV, centeredUV) *
        saturate(_OldFilmVignette));
    return saturate(color * vignette);
}

inline float UberPostSampleLuminance(float2 uv)
{
    return UberPostLuminance(UberPostSampleLinear(uv).rgb);
}

inline float3 UberPostEdgeInk(float2 uv, float3 sourceColor)
{
    float2 tap = max(abs(_BlitTexture_TexelSize.xy), float2(0.000001, 0.000001)) *
        max(abs(_EdgeWidth), 0.0001);
    float topLeft = UberPostSampleLuminance(uv + tap * float2(-1.0, 1.0));
    float top = UberPostSampleLuminance(uv + tap * float2(0.0, 1.0));
    float topRight = UberPostSampleLuminance(uv + tap * float2(1.0, 1.0));
    float left = UberPostSampleLuminance(uv + tap * float2(-1.0, 0.0));
    float right = UberPostSampleLuminance(uv + tap * float2(1.0, 0.0));
    float bottomLeft = UberPostSampleLuminance(uv + tap * float2(-1.0, -1.0));
    float bottom = UberPostSampleLuminance(uv + tap * float2(0.0, -1.0));
    float bottomRight = UberPostSampleLuminance(uv + tap * float2(1.0, -1.0));

    float gradientX = -topLeft - 2.0 * left - bottomLeft +
        topRight + 2.0 * right + bottomRight;
    float gradientY = topLeft + 2.0 * top + topRight -
        bottomLeft - 2.0 * bottom - bottomRight;
    float magnitude = length(float2(gradientX, gradientY));
    float threshold = saturate(_EdgeThreshold);
    float feather = max(fwidth(magnitude), 0.0001);
    float edge = smoothstep(threshold, threshold + feather, magnitude) *
        saturate(_EdgeStrength);
    float3 background = lerp(_EdgeBackgroundColor.rgb, sourceColor,
        saturate(_EdgeSourceMix));
    return lerp(background, _EdgeColor.rgb, saturate(edge));
}

inline float UberPostAsciiPackedGlyph(float2 localPosition, float glyphIndex,
    float softness)
{
    // Fifteen project-owned 4x5 fallback masks follow the font ramp order.
    // All packed values stay below 2^20 and are exact in float precision.
    float code = 0.0;
    if (glyphIndex < 0.5) code = 0.0;
    else if (glyphIndex < 1.5) code = 2.0;
    else if (glyphIndex < 2.5) code = 66.0;
    else if (glyphIndex < 3.5) code = 8224.0;
    else if (glyphIndex < 4.5) code = 8225.0;
    else if (glyphIndex < 5.5) code = 131618.0;
    else if (glyphIndex < 6.5) code = 263235.0;
    else if (glyphIndex < 7.5) code = 492548.0;
    else if (glyphIndex < 8.5) code = 1016864.0;
    else if (glyphIndex < 9.5) code = 934982.0;
    else if (glyphIndex < 10.5) code = 432534.0;
    else if (glyphIndex < 11.5) code = 560798.0;
    else if (glyphIndex < 12.5) code = 923271.0;
    else if (glyphIndex < 13.5) code = 189922.0;
    else if (glyphIndex < 14.5) code = 390645.0;
    else if (glyphIndex < 15.5) code = 458078.0;

    float2 gridUV = saturate(localPosition * 0.5 + 0.5);
    float2 gridSize = float2(4.0, 5.0);
    float2 cell = min(floor(gridUV * gridSize), gridSize - 1.0);
    float bitIndex = cell.x + cell.y * gridSize.x;
    float bitValue = fmod(floor(code / exp2(bitIndex)), 2.0);
    float2 cellUV = frac(gridUV * gridSize);
    float edge = min(min(cellUV.x, 1.0 - cellUV.x),
        min(cellUV.y, 1.0 - cellUV.y));
    return bitValue * smoothstep(0.0, min(softness, 0.25), edge);
}

inline void UberPostAsciiGlyphMetadata(float glyphIndex, out float4 glyphUV,
    out float4 placement)
{
    if (glyphIndex < 1.5) { glyphUV = _AsciiGlyphUV0; placement = _AsciiGlyphPlacement0; }
    else if (glyphIndex < 2.5) { glyphUV = _AsciiGlyphUV1; placement = _AsciiGlyphPlacement1; }
    else if (glyphIndex < 3.5) { glyphUV = _AsciiGlyphUV2; placement = _AsciiGlyphPlacement2; }
    else if (glyphIndex < 4.5) { glyphUV = _AsciiGlyphUV3; placement = _AsciiGlyphPlacement3; }
    else if (glyphIndex < 5.5) { glyphUV = _AsciiGlyphUV4; placement = _AsciiGlyphPlacement4; }
    else if (glyphIndex < 6.5) { glyphUV = _AsciiGlyphUV5; placement = _AsciiGlyphPlacement5; }
    else if (glyphIndex < 7.5) { glyphUV = _AsciiGlyphUV6; placement = _AsciiGlyphPlacement6; }
    else if (glyphIndex < 8.5) { glyphUV = _AsciiGlyphUV7; placement = _AsciiGlyphPlacement7; }
    else if (glyphIndex < 9.5) { glyphUV = _AsciiGlyphUV8; placement = _AsciiGlyphPlacement8; }
    else if (glyphIndex < 10.5) { glyphUV = _AsciiGlyphUV9; placement = _AsciiGlyphPlacement9; }
    else if (glyphIndex < 11.5) { glyphUV = _AsciiGlyphUV10; placement = _AsciiGlyphPlacement10; }
    else if (glyphIndex < 12.5) { glyphUV = _AsciiGlyphUV11; placement = _AsciiGlyphPlacement11; }
    else if (glyphIndex < 13.5) { glyphUV = _AsciiGlyphUV12; placement = _AsciiGlyphPlacement12; }
    else if (glyphIndex < 14.5) { glyphUV = _AsciiGlyphUV13; placement = _AsciiGlyphPlacement13; }
    else { glyphUV = _AsciiGlyphUV14; placement = _AsciiGlyphPlacement14; }
}

inline float UberPostAsciiGlyph(float2 cellUV, float glyphIndex, float softness)
{
    if (glyphIndex < 0.5) return 0.0;
    if (_AsciiFontReady < 0.5)
        return UberPostAsciiPackedGlyph(
            cellUV * 2.0 - 1.0, glyphIndex, softness);

    float4 glyphUV;
    float4 placement;
    UberPostAsciiGlyphMetadata(glyphIndex, glyphUV, placement);
    float2 placementSize = float2(
        placement.z == placement.z ? max(abs(placement.z), 0.00001) : 1.0,
        placement.w == placement.w ? max(abs(placement.w), 0.00001) : 1.0);
    float2 glyphPosition = (cellUV - placement.xy) / placementSize;
    float inside = step(0.0, glyphPosition.x) * step(glyphPosition.x, 1.0) *
        step(0.0, glyphPosition.y) * step(glyphPosition.y, 1.0);
    float sdf = SAMPLE_TEXTURE2D(_AsciiFontAtlas, sampler_AsciiFontAtlas,
        glyphUV.xy + saturate(glyphPosition) * glyphUV.zw).a;
    float threshold = _AsciiSdfThreshold == _AsciiSdfThreshold
        ? saturate(_AsciiSdfThreshold) : 0.5;
    float sdfSoftness = _AsciiSdfSoftness == _AsciiSdfSoftness
        ? clamp(abs(_AsciiSdfSoftness), 0.0001, 0.5) : 0.08;
    return inside * smoothstep(
        threshold - sdfSoftness, threshold + sdfSoftness, sdf);
}

inline float3 UberPostAscii(float2 uv)
{
    float cellSize = round(abs(_AsciiCellSize));
    cellSize = cellSize == cellSize ? clamp(cellSize, 1.0, 512.0) : 1.0;
    float2 screenSize = max(abs(_BlitTexture_TexelSize.zw), float2(1.0, 1.0));
    float2 pixelPosition = saturate(uv) * screenSize;
    float horizontalSpacing = _AsciiCharacterSpacing == _AsciiCharacterSpacing
        ? clamp(_AsciiCharacterSpacing, -0.75, 0.75) : -0.35;
    float verticalSpacing = _AsciiLineSpacing == _AsciiLineSpacing
        ? clamp(_AsciiLineSpacing, -0.75, 0.75) : 0.0;
    float2 glyphCellSize = float2(cellSize, cellSize);
    float2 cellPitch = max(glyphCellSize *
        (1.0 + float2(horizontalSpacing, verticalSpacing)), float2(1.0, 1.0));
    float2 cellIndex = floor(pixelPosition / cellPitch);
    float2 cellCenterPixels = (cellIndex + 0.5) * cellPitch;
    float2 cellCenterUV = saturate(cellCenterPixels / screenSize);
    float3 cellColor = UberPostSampleLinear(cellCenterUV).rgb;

    float luminance = saturate(UberPostLuminance(cellColor));
    luminance = lerp(luminance, 1.0 - luminance, step(0.5, _AsciiInvert));
    float glyphIndex = min(floor(luminance * 16.0), 15.0);
    float2 glyphOrigin = cellCenterPixels - glyphCellSize * 0.5;
    float2 cellUV = (pixelPosition - glyphOrigin) / glyphCellSize;
    float cellBounds = step(0.0, cellUV.x) * step(cellUV.x, 1.0) *
        step(0.0, cellUV.y) * step(cellUV.y, 1.0);
    float softness = clamp(2.0 / max(cellSize, 1.0), 0.02, 0.35);
    float glyph = cellBounds * saturate(UberPostAsciiGlyph(
        saturate(cellUV), glyphIndex, softness));
    float3 foreground = lerp(_AsciiForegroundColor.rgb,
        cellColor * _AsciiForegroundColor.rgb, saturate(_AsciiSourceColor));
    return lerp(_AsciiBackgroundColor.rgb, foreground, glyph);
}

inline float3 UberPostCRT(float2 uv, float2 pixelPosition, float3 sourceColor)
{
    float strength = _CRTStrength == _CRTStrength ? saturate(_CRTStrength) : 0.0;
    if (strength <= 0.0)
        return sourceColor;

    float width = abs(_BlitTexture_TexelSize.z);
    float height = abs(_BlitTexture_TexelSize.w);
    width = width == width ? clamp(width, 1.0, 65536.0) : 1.0;
    height = height == height ? clamp(height, 1.0, 65536.0) : 1.0;
    float2 sourceSize = float2(width, height);
    float2 texelSize = rcp(sourceSize);

    // Match the referenced slot-machine display: the first half collapses the
    // image into a horizontal line, then the second half closes that line.
    // Reversing this single value produces the power-on animation.
    float powerOffAmount = _CRTPowerOffAmount == _CRTPowerOffAmount
        ? saturate(_CRTPowerOffAmount) : 0.0;
    float powerCollapseY = saturate(powerOffAmount * 2.0);
    float powerCollapseX = saturate((powerOffAmount - 0.5) * 2.0);
    float2 minimumPowerScale = min(texelSize * 2.0, 1.0);
    float2 powerScale = max(1.0 - float2(powerCollapseX, powerCollapseY),
        minimumPowerScale);
    float2 powerUV = (uv - 0.5) / powerScale + 0.5;
    float2 powerHalfExtent = powerScale * 0.5;
    float2 powerDistance = powerHalfExtent - abs(uv - 0.5);
    float2 powerAA = max(fwidth(uv) * 0.5, texelSize * 0.5);
    float2 powerBoundary = smoothstep(-powerAA, powerAA, powerDistance);
    float powerMask = powerBoundary.x * powerBoundary.y;
    float powerVisibility = 1.0 - smoothstep(0.98, 1.0, powerOffAmount);

    float speed = _CRTAnimationSpeed == _CRTAnimationSpeed
        ? clamp(_CRTAnimationSpeed, -5.0, 5.0) : 0.0;
    float rawTime = _Time.y * speed;
    float finiteTime = rawTime == rawTime && abs(rawTime) < 1.0e20 ? rawTime : 0.0;
    float time = fmod(finiteTime, 3600.0);
    float jitterStrength = _CRTHorizontalJitter == _CRTHorizontalJitter
        ? clamp(abs(_CRTHorizontalJitter), 0.0, 4.0) : 0.0;
    float jitterLine = floor(saturate(powerUV.y) * height * 0.25);
    float jitterFrame = floor(time * 60.0);
    float jitter = (UberPostHash21(float2(jitterLine, jitterFrame)) * 2.0 - 1.0) *
        jitterStrength * texelSize.x;

    float curvature = _CRTCurvature == _CRTCurvature
        ? clamp(abs(_CRTCurvature), 0.0, 0.5) : 0.0;
    float2 centered = powerUV * 2.0 - 1.0;
    float2 axisSquared = centered * centered;
    centered *= 1.0 + curvature * axisSquared.yx;
    float2 curvedUV = centered * 0.5 + 0.5;
    float2 warpedUV = curvedUV + float2(jitter, 0.0);

    float2 outsideUV = max(max(-curvedUV, curvedUV - 1.0), 0.0);
    float2 boundary = 1.0 - smoothstep(0.0, texelSize, outsideUV);
    float insideMask = boundary.x * boundary.y;

    float chromatic = _CRTChromaticAberration == _CRTChromaticAberration
        ? clamp(abs(_CRTChromaticAberration), 0.0, 8.0) : 0.0;
    float2 chromaticDirection = centered / max(length(centered), 0.0001);
    float2 chromaticOffset = chromaticDirection * texelSize * chromatic;
    float3 color = float3(
        UberPostSampleLinear(warpedUV + chromaticOffset).r,
        UberPostSampleLinear(warpedUV).g,
        UberPostSampleLinear(warpedUV - chromaticOffset).b);

    float density = _CRTScanlineDensity == _CRTScanlineDensity
        ? clamp(abs(_CRTScanlineDensity), 1.0, 4096.0) : 480.0;
    float scanCoordinate = warpedUV.y * density;
    float scanDerivative = max(fwidth(scanCoordinate), 0.0001);
    float scanAA = min(scanDerivative * 0.5, 0.25);
    float scanDistance = abs(frac(scanCoordinate) - 0.5);
    float scanShape = smoothstep(0.25 - scanAA, 0.25 + scanAA, scanDistance);
    scanShape = lerp(scanShape, 0.5,
        saturate((scanDerivative - 0.5) * 2.0));
    float scanStrength = _CRTScanlineStrength == _CRTScanlineStrength
        ? saturate(_CRTScanlineStrength) : 0.0;
    color *= lerp(1.0, scanShape, scanStrength);

    float maskScale = _CRTMaskScale == _CRTMaskScale
        ? clamp(abs(_CRTMaskScale), 1.0, 8.0) : 1.0;
    float maskPhase = pixelPosition.x * 2.09439510239 / maskScale;
    float3 phosphorMask = 1.0 + 0.35 * cos(maskPhase +
        float3(0.0, -2.09439510239, -4.18879020479));
    float maskStrength = _CRTMaskStrength == _CRTMaskStrength
        ? saturate(_CRTMaskStrength) : 0.0;
    color *= lerp(1.0, phosphorMask, maskStrength);

    float2 vignetteAxis = saturate(curvedUV * (1.0 - curvedUV) * 4.0);
    float vignette = smoothstep(0.0, 1.0, vignetteAxis.x * vignetteAxis.y);
    float vignetteStrength = _CRTVignetteStrength == _CRTVignetteStrength
        ? saturate(_CRTVignetteStrength) : 0.0;
    color *= lerp(1.0, vignette, vignetteStrength);

    float signalStrength = _CRTSignalNoise == _CRTSignalNoise
        ? clamp(abs(_CRTSignalNoise), 0.0, 0.25) : 0.0;
    float2 noisePixel = floor(saturate(warpedUV) * sourceSize);
    float pixelNoise = UberPostHash21(noisePixel + jitterFrame * float2(17.0, 7.0)) - 0.5;
    float lineNoise = UberPostHash21(float2(floor(noisePixel.y * 0.5),
        floor(time * 30.0) + 29.0)) - 0.5;
    float flicker = UberPostHash21(float2(floor(time * 24.0), 71.0)) - 0.5;
    color += (pixelNoise * 0.75 + lineNoise * 0.25) * signalStrength;
    color *= max(1.0 + flicker * signalStrength * 0.5, 0.0);

    float rollingStrength = _CRTRollingBand == _CRTRollingBand
        ? saturate(_CRTRollingBand) : 0.0;
    float rollingDistance = abs(frac(warpedUV.y - time * 0.2 + 0.5) - 0.5);
    float rollingBand = 1.0 - smoothstep(0.0, 0.12, rollingDistance);
    color *= 1.0 + rollingBand * rollingStrength * 0.25;

    float powerTransition = powerCollapseY * (1.0 - powerCollapseX);
    float powerFlash = powerTransition * 0.5;
    float powerBloomIntensity = _CRTPowerBloomIntensity == _CRTPowerBloomIntensity
        ? clamp(abs(_CRTPowerBloomIntensity), 0.0, 8.0) : 0.0;
    float powerBloom = powerTransition * powerBloomIntensity;
    float3 poweredColor = (saturate(color + powerFlash) + powerBloom) * insideMask *
        powerMask * powerVisibility;
    return lerp(sourceColor, poweredColor, strength);
}

float4 UberPostFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float4 source = UberPostSampleSource(input.texcoord.xy);
    float sourceAlpha = source.a;

#if defined(_COLOR_ADJUST_ON)
    source.rgb = UberAdjustColor(
        source.rgb, _HueShift, _Saturation, _Brightness, _Contrast);
#elif defined(_COLOR_SCREEN_BLEND_ON)
    source.rgb = UberPostScreenBlend(source.rgb);
#elif defined(_ORDERED_DITHER_ON)
    source.rgb = UberPostOrderedDither(source.rgb, input.positionCS.xy);
#elif defined(_COLOR_QUANTIZATION_ON)
    source.rgb = UberPostQuantize(source.rgb);
#elif defined(_GRADIENT_MAP_ON)
    source.rgb = UberPostGradientMap(source.rgb);
#elif defined(_OLD_FILM_ON)
    source.rgb = UberPostOldFilm(input.texcoord.xy, input.positionCS.xy);
#elif defined(_EDGE_FILTER_ON)
    source.rgb = UberPostEdgeInk(input.texcoord.xy, source.rgb);
#elif defined(_ASCII_FILTER_ON)
    source.rgb = UberPostAscii(input.texcoord.xy);
#elif defined(_CRT_FILTER_ON)
    source.rgb = UberPostCRT(input.texcoord.xy, input.positionCS.xy, source.rgb);
#endif

    return float4(source.rgb, sourceAlpha);
}

#endif // UBER_POST_PROCESSING_INCLUDED
