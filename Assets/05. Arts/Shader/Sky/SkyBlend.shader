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

        // 별은 파티클 껍질이 아니라 여기서 그린다. 유한한 껍질은 큰 화면과 후퇴 뷰가 최대 488 유닛
        // 떨어져 있어 한쪽에서 반드시 '공'으로 보인다 — 스카이박스는 무한원이라 카메라마다 맞는다.
        _StarBrightness("Star Brightness", Range(0, 4)) = 1
        _StarDensity("Star Density", Range(20, 400)) = 180
        _StarCoverage("Star Coverage", Range(0, 0.2)) = 0.02
        _StarGlow("Star Glow", Range(1, 400)) = 90
        _StarTwinkle("Star Twinkle", Range(0, 1)) = 0.3
        _StarWashout("Star Washout", Range(0, 8)) = 3
        _StarWarm("Star Warm Tint", Color) = (1, 0.87, 0.74, 1)
        _StarCool("Star Cool Tint", Color) = (0.78, 0.87, 1, 1)
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
                half _StarBrightness;
                half _StarDensity;
                half _StarCoverage;
                half _StarGlow;
                half _StarTwinkle;
                half _StarWashout;
                half4 _StarWarm;
                half4 _StarCool;
            CBUFFER_END

            // Dave Hoskins hash33. 격자 칸 하나에서 밝기·칸 안 위치·색·반짝임 위상을 한 번에 뽑는다.
            float3 Hash3(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yzz) * p.zyx);
            }

            // 방향을 큐브 면 uv 로 편다. 방향 벡터를 그대로 격자에 넣으면 극에서 별이 뭉치고 면 경계에서
            // 격자가 끊긴다. 면 번호를 해시에 같이 넣어야 이웃 면이 같은 별을 복제하지 않는다.
            half3 StarField(float3 dir)
            {
                float3 a = abs(dir);
                float m = max(a.x, max(a.y, a.z));
                float3 s = dir / max(m, 1e-5);

                float2 uv;
                float face;
                if (m == a.x)      { uv = s.zy; face = s.x > 0 ? 0.0 : 1.0; }
                else if (m == a.y) { uv = s.xz; face = s.y > 0 ? 2.0 : 3.0; }
                else               { uv = s.xy; face = s.z > 0 ? 4.0 : 5.0; }
                uv = uv * 0.5 + 0.5;

                float2 grid = uv * _StarDensity;
                float3 h = Hash3(float3(floor(grid), face));

                // 상위 _StarCoverage 비율의 칸만 별이 된다. 세제곱으로 치우쳐 밝은 별은 소수,
                // 흐린 별이 다수가 되게 한다 — 균일한 밝기는 밤하늘이 아니라 점 무늬로 보인다.
                float mag = saturate((h.x - (1.0 - _StarCoverage)) / max(_StarCoverage, 1e-4));
                mag = mag * mag * mag;

                // 칸 정중앙에 찍으면 격자가 그대로 드러난다. 칸 안에서 흔든다.
                float2 offset = frac(grid) - (0.5 + (h.yz - 0.5) * 0.6);
                float d2 = dot(offset, offset);
                // 프로젝트에 블룸이 없다(유효 볼륨 프로파일 intensity 0). 발광은 전적으로 이 감쇠가
                // 만든다 — 좁은 코어에 넓고 약한 헤일로를 겹친다.
                float glow = exp2(-d2 * _StarGlow) + exp2(-d2 * _StarGlow * 0.06) * 0.2;

                // 별마다 주기와 위상이 달라야 개별로 반짝인다. 하나로 묶으면 하늘이 통째로 깜빡인다.
                float twinkle = 1.0 - _StarTwinkle
                    * (0.5 + 0.5 * sin(_Time.y * (1.2 + h.y * 2.6) + h.z * 6.2831853));

                return lerp(_StarWarm.rgb, _StarCool.rgb, h.z)
                       * (glow * mag * twinkle * _StarBrightness);
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
                half3 atmosphere = lerp(_HorizonColor.rgb, _SkyTint.rgb, t) * _Exposure;

                half3 space = SAMPLE_TEXTURECUBE(_SpaceCube, sampler_SpaceCube, dir).rgb * _SpaceExposure;
                half3 sky = lerp(atmosphere, space, _SpaceBlend);

                // 별은 늘 그 자리에 있다. 알파로 켜지 않고 밝은 하늘이 씻어내게 둔다 — 고도가 오르며
                // skyExposure 가 0 으로 가면 저절로 드러나서, 해질녘에 하나둘 보이는 그림이 된다.
                // 점 전체를 동시에 올리는 알파 램프가 '페이드로 생긴다'로 읽히던 것을 여기서 없앤다.
                half skyLum = dot(atmosphere, half3(0.2126, 0.7152, 0.0722));
                half3 stars = StarField(dir) * saturate(1.0 - skyLum * _StarWashout);

                return half4(sky + stars, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
