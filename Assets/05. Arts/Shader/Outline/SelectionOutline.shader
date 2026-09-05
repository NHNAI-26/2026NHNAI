// 설계 스테이지 선택 표시. 마스크 패스가 선택된 렌더러의 실루엣을 R8 텍스처에 채우고,
// 합성 패스가 그것을 화면 픽셀 단위로 팽창시켜 카메라 컬러 위에 얹는다. 깊이를 쓰지 않으므로
// 부품이 가려져도 아웃라인은 보인다. 두 패스 모두 SelectionOutlineFeature 가 직접 그린다 —
// LightMode 태그가 없어 URP 가 알아서 그리는 일은 없다.
Shader "Shader/Selection Outline"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Color", Color) = (1, 0.72, 0.08, 1)
        _OutlineWidth("Width (Pixels)", Range(0, 4)) = 3
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Mask"
            ZWrite Off
            ZTest Always
            Cull Back
            Blend Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex SelectionMaskVertex
            #pragma fragment SelectionMaskFragment
            #pragma shader_feature _ INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct MaskAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct MaskVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            MaskVaryings SelectionMaskVertex(MaskAttributes input)
            {
                MaskVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 SelectionMaskFragment(MaskVaryings input) : SV_Target
            {
                return 1.0h;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SelectionCompositeVertex
            #pragma fragment SelectionCompositeFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_OutlineMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half _OutlineWidth;
            CBUFFER_END

            struct CompositeVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            CompositeVaryings SelectionCompositeVertex(uint vertexID : SV_VertexID)
            {
                CompositeVaryings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                return output;
            }

            half4 SelectionCompositeFragment(CompositeVaryings input) : SV_Target
            {
                // SV_POSITION 은 어태치먼트 절대 픽셀 좌표다. 마스크를 카메라 컬러와 같은 크기로
                // 만들었으므로 UV·_BlitScaleBias·RTHandle 스케일 보정이 전부 필요 없다.
                int2 px = int2(input.positionCS.xy);
                half center = LOAD_TEXTURE2D(_OutlineMask, px).r;

                // ponytail: 링당 8탭 — UberSprite 의 _PIXEL_OUTLINE_ON 과 같은 모양이다.
                // 바깥 링에서 탭 사이가 살짝 별 모양으로 벌어지지만 4px 에서는 안 보인다.
                // 더 두껍게 갈 일이 생기면 사각 박스 순회 + 반지름 검사로 바꾼다.
                half neighbor = 0.0h;
                [unroll]
                for (int ring = 1; ring <= 4; ++ring)
                {
                    if (step((half)ring - 0.5h, _OutlineWidth) < 0.5h) break;
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2( ring,     0)).r);
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2(-ring,     0)).r);
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2(    0,  ring)).r);
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2(    0, -ring)).r);
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2( ring,  ring)).r);
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2( ring, -ring)).r);
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2(-ring,  ring)).r);
                    neighbor = max(neighbor, LOAD_TEXTURE2D(_OutlineMask, px + int2(-ring, -ring)).r);
                }

                // 실루엣 안쪽은 비운다 — 테두리만 남는다.
                return half4(_OutlineColor.rgb, _OutlineColor.a * saturate(neighbor - center));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
