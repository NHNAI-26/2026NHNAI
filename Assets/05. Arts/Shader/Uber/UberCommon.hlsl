#ifndef UBER_COMMON_INCLUDED
#define UBER_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Extension contract:
// - Keep only stateless helpers used unchanged by at least two shader families.
// - Keep textures, material buffers, surface structs, keywords, and effect order surface-owned.

inline float UberSafeSignedRange(float range, float epsilon)
{
    float safeEpsilon = max(abs(epsilon), 0.0000001);
    if (abs(range) >= safeEpsilon)
        return range;

    return range < 0.0 ? -safeEpsilon : safeEpsilon;
}

inline float2 UberSafeSignedRange(float2 range, float epsilon)
{
    return float2(
        UberSafeSignedRange(range.x, epsilon),
        UberSafeSignedRange(range.y, epsilon));
}

inline float UberSafeInverseLerp(float lower, float upper, float value)
{
    return saturate((value - lower) / UberSafeSignedRange(upper - lower, 0.0001));
}

// UV rectangles use xy as atlas origin and zw as atlas size.
inline float2 UberNormalizeUV(float2 atlasUV, float4 uvRect)
{
    return (atlasUV - uvRect.xy) / UberSafeSignedRange(uvRect.zw, 0.00001);
}

inline float2 UberRemapUV(float2 normalizedUV, float4 uvRect)
{
    return uvRect.xy + normalizedUV * uvRect.zw;
}

inline float2 UberClampUV(float2 atlasUV, float4 uvRect)
{
    return UberRemapUV(saturate(UberNormalizeUV(atlasUV, uvRect)), uvRect);
}

inline half UberUnitUVMask(float2 normalizedUV)
{
    half2 lower = step(half2(0.0h, 0.0h), normalizedUV);
    half2 upper = step(normalizedUV, half2(1.0h, 1.0h));
    return lower.x * lower.y * upper.x * upper.y;
}

inline float UberHash21(float2 value)
{
    return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453);
}

inline float UberEvaluateGlitchBandBoundary(float boundaryIndex, float frame,
    float averageBandSize, float bandSizeVariation)
{
    float jitter = UberHash21(float2(boundaryIndex + 83.17,
        frame + 29.41)) - 0.5;
    return boundaryIndex * averageBandSize +
        jitter * bandSizeVariation * 0.5;
}

inline float UberValueNoise1D(float coordinate)
{
    float cell = floor(coordinate);
    float blend = frac(coordinate);
    blend = blend * blend * (3.0 - 2.0 * blend);
    float first = frac(sin(cell * 12.9898) * 43758.5453);
    float second = frac(sin((cell + 1.0) * 12.9898) * 43758.5453);
    return lerp(first, second, blend);
}

inline float3 UberSafeNormalizeFinite3(float3 value, float3 fallback)
{
    float lengthSquared = dot(value, value);
    if (!(lengthSquared > 0.000001) || lengthSquared > 1.0e20)
        return fallback;
    return value * rsqrt(lengthSquared);
}

inline half4 UberEvaluateGradient4Keys(float time, float4 color0,
    float4 color1, float4 color2, float4 color3, float4 alphas,
    float4 alphaTimes, float4 metadata)
{
    float3 color = color0.rgb;
    color = lerp(color, color1.rgb,
        UberSafeInverseLerp(color0.a, color1.a, time) *
        step(1.5, metadata.x));
    color = lerp(color, color2.rgb,
        UberSafeInverseLerp(color1.a, color2.a, time) *
        step(2.5, metadata.x));
    color = lerp(color, color3.rgb,
        UberSafeInverseLerp(color2.a, color3.a, time) *
        step(3.5, metadata.x));

    float alpha = alphas.x;
    alpha = lerp(alpha, alphas.y,
        UberSafeInverseLerp(alphaTimes.x, alphaTimes.y, time) *
        step(1.5, metadata.y));
    alpha = lerp(alpha, alphas.z,
        UberSafeInverseLerp(alphaTimes.y, alphaTimes.z, time) *
        step(2.5, metadata.y));
    alpha = lerp(alpha, alphas.w,
        UberSafeInverseLerp(alphaTimes.z, alphaTimes.w, time) *
        step(3.5, metadata.y));
    return half4(color, alpha);
}

inline half3 UberAdjustColor(half3 color, half hueShiftDegrees, half saturation,
    half brightness, half contrast)
{
    half3 hsv = RgbToHsv(saturate(color));
    hsv.x = frac(hsv.x + hueShiftDegrees / 360.0h);
    hsv.y = saturate(hsv.y * max(saturation, 0.0h));
    hsv.z *= max(brightness, 0.0h);

    half3 adjusted = HsvToRgb(hsv);
    adjusted = (adjusted - 0.5h) * max(contrast, 0.0h) + 0.5h;
    return saturate(adjusted);
}

inline half UberBayer2x2(float2 pixel)
{
    if (pixel.y < 1.0)
        return pixel.x < 1.0 ? 0.0h : 2.0h;

    return pixel.x < 1.0 ? 3.0h : 1.0h;
}

inline half UberBayer4x4(float2 pixelPosition)
{
    float2 pixel = floor(frac(pixelPosition * 0.25) * 4.0);
    half coarse = UberBayer2x2(floor(pixel * 0.5));
    half fine = UberBayer2x2(pixel - floor(pixel * 0.5) * 2.0);
    return (fine * 4.0h + coarse + 0.5h) / 16.0h;
}

#endif // UBER_COMMON_INCLUDED
