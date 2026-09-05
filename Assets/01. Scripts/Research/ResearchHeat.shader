Shader "Border/UI/ResearchHeat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _EmissionMask ("Heat Mask", 2D) = "white" {}
        _Heat ("Heat", Range(0, 1)) = 0
        _HeatColor ("Heat Color", Color) = (1, 0.06, 0.01, 1)
        _EmissionStrength ("Emission Strength", Range(0, 4)) = 1.8
        _MaskUVRect ("Sprite UV Bounds", Vector) = (0, 0, 1, 1)
        _RadialMask ("Valve Ring Mask", Float) = 0
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
            sampler2D _MainTex, _EmissionMask;
            float4 _TextureSampleAdd, _ClipRect, _HeatColor, _MaskUVRect;
            float _Heat, _EmissionStrength, _RadialMask;
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
                float4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                // The pipe mask uses original texture UVs; the valve ring uses its cropped sprite bounds.
                float2 localUV = (i.uv - _MaskUVRect.xy) / max(_MaskUVRect.zw, float2(0.0001, 0.0001));
                float radius = length(localUV - 0.5);
                float ring = smoothstep(0.13, 0.20, radius) * (1 - smoothstep(0.43, 0.50, radius));
                float mask = tex2D(_EmissionMask, i.uv).r * lerp(1, ring, saturate(_RadialMask));
                float heat = saturate(_Heat) * mask;
                color.rgb = lerp(color.rgb, color.rgb * _HeatColor.rgb, heat * 0.8);
                color.rgb += _HeatColor.rgb * heat * heat * _EmissionStrength * i.color.a;
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
