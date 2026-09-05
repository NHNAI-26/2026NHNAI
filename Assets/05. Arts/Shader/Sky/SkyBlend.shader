// 고도로 대기와 우주를 섞는 스카이박스. Uber 계열이 아니라 UberShaderVariantManifest 와 무관하다.
// 프로퍼티는 전부 SkyEnvironment 가 매 프레임 커브로 구동한다 — 이름을 바꾸면 조용히 죽으니
// SkyEnvironmentTests.SkyboxShader_CompilesAndKeepsDrivenProperties 가 잡아 준다.
Shader "Sky/AtmosphereNebulaBlend"
{
    Properties
    {
        _SkyTint("Zenith Color", Color) = (0.29, 0.51, 0.87, 1)
        _HorizonColor("Horizon Color", Color) = (0.96, 1.0, 1.0, 1)
        _Exposure("Atmosphere Exposure", Range(0, 8)) = 1
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 5)) = 1
        [NoScaleOffset] _SpaceCube("Space Cubemap", Cube) = "black" {}
        _SpaceBlend("Space Blend", Range(0, 1)) = 0
        _SpaceExposure("Space Exposure", Range(0, 8)) = 1
    }

    SubShader
    {
        // RenderPipeline 태그는 일부러 붙이지 않는다. 태그 없는 SubShader 는 어느 파이프라인에서도
        // 선택되고, URP 는 스카이박스를 엔진 네이티브 렌더러 리스트로 넘기므로(DrawSkyboxPass)
        // URP 전용으로 못 박아서 얻을 것이 없다.
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_SpaceCube);
            SAMPLER(sampler_SpaceCube);

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTint;
                half4 _HorizonColor;
                half _Exposure;
                half _AtmosphereThickness;
                half _SpaceBlend;
                half _SpaceExposure;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 빌트인 Skybox-Procedural 과 같은 방식. 모델 행렬이 항등이면 positionOS 와 결과가
                // 같고 아니면 이쪽만 맞다 — dir.y 가 월드 업이라는 이 셰이더의 전제를 공짜로 지킨다.
                OUT.dirWS = mul((float3x3)UNITY_MATRIX_M, IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.dirWS);

                // 지수가 크면 지평선 띠가 넓고(두꺼운 대기), 작으면 지평선에 딱 붙는다(얇은 대기).
                // saturate 가 아래 반구를 0 으로 눌러 지평선 아래는 전부 _HorizonColor 가 된다 —
                // 안개색이 지평선색과 지면색을 겸하므로 별도 지면색 프로퍼티가 필요 없다.
                // max 가드 덕에 지수가 0 이 될 수 없어 pow(0, 0) 의 NaN 도 나오지 않는다.
                float t = pow(saturate(dir.y), max(_AtmosphereThickness, 0.001) * 0.5);
                half3 atmosphere = lerp(_HorizonColor.rgb, _SkyTint.rgb, t) * _Exposure;

                half3 space = SAMPLE_TEXTURECUBE(_SpaceCube, sampler_SpaceCube, dir).rgb * _SpaceExposure;
                return half4(lerp(atmosphere, space, _SpaceBlend), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
