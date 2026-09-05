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

        // 지평선과 천정 사이에 색을 하나 더 끼우는 선택 경로. _MidBlend 기본값이 0 이라
        // 켜지 않으면 아래 프래그먼트는 기존 2색 lerp 와 비트 단위로 같다 — 인게임 하늘은
        // 하나도 변하지 않는다. 해피엔딩의 핑크~보라~남색 밤하늘만 이것을 1 로 켠다.
        _MidColor("Mid Color", Color) = (0.5, 0.5, 0.5, 1)
        _MidBlend("Mid Color Blend", Range(0, 1)) = 0

        // 절차 별밭. 큐브맵과 별개로 하늘 위에 더한다 — 성운 큐브맵은 천정이 비어 있어
        // 밤하늘 위쪽이 민무늬가 된다. _StarBlend 기본값 0 이라 켜지 않으면 기존 결과와 같다.
        _StarBlend("Star Blend", Range(0, 4)) = 0
        _StarDensity("Star Density", Range(20, 400)) = 140
        _StarSize("Star Size", Range(0.01, 0.5)) = 0.12
        _StarRate("Star Rate", Range(0, 1)) = 0.06
        // 1 = 지평선 별이 투명하고 천정으로 갈수록 진해진다. 0 = 온 하늘에 고르게.
        _StarUpFade("Star Up Fade", Range(0, 1)) = 1
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
                half4 _MidColor;
                half _MidBlend;
                half _StarBlend;
                float _StarDensity;
                half _StarSize;
                half _StarRate;
                half _StarUpFade;
            CBUFFER_END

            // 셀 좌표 하나에서 0..1 난수 둘. 방향에서만 나오므로 카메라가 어디로 돌아도 같은 별이
            // 같은 자리에 있다 — 텍스처도 버퍼도 필요 없다.
            float2 StarHash(float3 cell)
            {
                float3 p = frac(cell * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac(float2((p.x + p.y) * p.z, (p.x + p.z) * p.y));
            }

            // 방향 공간을 격자로 잘라 셀마다 최대 한 점을 찍는다. 셀 중심에서의 거리로 부드럽게
            // 떨어뜨려 픽셀 하나짜리 반짝임(에일리어싱)이 되지 않게 한다.
            half3 StarField(float3 dir)
            {
                float3 s = dir * _StarDensity;
                float3 cell = floor(s);
                float3 offset = frac(s) - 0.5;

                float2 h = StarHash(cell);
                // h.x 가 문턱을 넘은 셀만 별이다. h.y 는 밝기 편차 — 다 같은 밝기면 격자가 보인다.
                float present = step(1.0 - _StarRate, h.x);
                float spark = smoothstep(_StarSize, 0.0, length(offset)) * present * (0.35 + 0.65 * h.y);

                // 지평선 쪽을 눌러 대기광에 묻히게 한다. _StarUpFade 0 이면 온 하늘이 고르다.
                float up = lerp(1.0, saturate(dir.y) * saturate(dir.y), _StarUpFade);
                return spark * up;
            }

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

                // 2색은 지금까지의 하늘, 3색은 중간색을 t=0.5 에 끼운 하늘. _MidBlend 가 0 이면
                // 두 번째 항이 통째로 무시되므로 기존 결과가 그대로 나온다.
                half3 twoTone = lerp(_HorizonColor.rgb, _SkyTint.rgb, t);
                half3 threeTone = t < 0.5
                    ? lerp(_HorizonColor.rgb, _MidColor.rgb, t * 2.0)
                    : lerp(_MidColor.rgb, _SkyTint.rgb, (t - 0.5) * 2.0);
                half3 atmosphere = lerp(twoTone, threeTone, _MidBlend) * _Exposure;

                half3 space = SAMPLE_TEXTURECUBE(_SpaceCube, sampler_SpaceCube, dir).rgb * _SpaceExposure;
                half3 color = lerp(atmosphere, space, _SpaceBlend);
                // 별은 섞지 않고 더한다 — 하늘색을 덮는 것이 아니라 그 위에서 빛나야 한다.
                return half4(color + StarField(dir) * _StarBlend, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
