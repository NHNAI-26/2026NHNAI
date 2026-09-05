Shader "Border/UI/NewspaperPhoto"
{
    Properties
    {
        [PerRendererData] _MainTex ("Photograph", 2D) = "white" {}
        _PaperColor ("Paper Color", Color) = (0.94, 0.90, 0.80, 1)
        _InkColor ("Ink Color", Color) = (0.10, 0.085, 0.06, 1)
        _Strength ("Print Tone", Range(0, 1)) = 1
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.8
        _DitherPixelSize ("Dither Cell Size", Range(1, 12)) = 4
        _DitherLevels ("Ink Levels", Range(2, 8)) = 4
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 localPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float4 _TextureSampleAdd, _ClipRect, _PaperColor, _InkColor;
            float4 _MainTex_TexelSize;
            float _Strength, _DitherStrength, _DitherPixelSize, _DitherLevels;
            float Bayer2(float2 p)
            {
                return lerp(lerp(0.0, 2.0, p.x), lerp(3.0, 1.0, p.x), p.y);
            }
            float DitherThreshold(float2 uv)
            {
                // Anchor the ink pattern to the photo, not the moving screen coordinates.
                float2 cell = floor(uv * _MainTex_TexelSize.zw / max(1.0, _DitherPixelSize));
                float2 low = fmod(cell, 2.0);
                float2 high = fmod(floor(cell / 2.0), 2.0);
                return (4.0 * Bayer2(low) + Bayer2(high) + 0.5) / 16.0;
            }
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                float luminance = saturate(dot(color.rgb, float3(0.2126, 0.7152, 0.0722)));
                float intervals = max(1.0, floor(_DitherLevels + 0.5) - 1.0);
                float scaled = luminance * intervals;
                float quantized = (floor(scaled) + step(DitherThreshold(i.uv), frac(scaled))) / intervals;
                luminance = lerp(luminance, saturate(quantized), saturate(_DitherStrength));
                float3 printColor = lerp(_InkColor.rgb, _PaperColor.rgb, luminance);
                color.rgb = lerp(color.rgb, printColor, _Strength);
                color *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
