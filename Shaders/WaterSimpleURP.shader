Shader "UVS/WaterSimpleURP"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.06, 0.36, 0.48, 0.65)
        _DeepColor ("Deep Color", Color) = (0.01, 0.08, 0.18, 0.78)
        _FoamColor ("Foam Color", Color) = (0.92, 0.96, 1.0, 1.0)

        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 4
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 1
        _Smoothness ("Specular Smoothness", Range(0,1)) = 0.85
        _SpecularStrength ("Specular Strength", Range(0,2)) = 1

        _DepthMax ("Depth Blend Distance", Float) = 6
        _FoamThreshold ("Foam Threshold", Float) = 0.6
        _FoamIntensity ("Foam Intensity", Float) = 1.2

        [NoScaleOffset]_NormalA ("Normal A", 2D) = "bump" {}
        [NoScaleOffset]_NormalB ("Normal B", 2D) = "bump" {}
        _NormalTiling ("Normal Tiling", Float) = 0.16
        _NormalStrength ("Normal Strength", Range(0,2)) = 0.85
        _NormalSpeedA ("Normal Speed A", Vector) = (0.05, 0.03, 0, 0)
        _NormalSpeedB ("Normal Speed B", Vector) = (-0.03, 0.04, 0, 0)

        _WaveDirA ("Wave Direction A", Vector) = (1,0,0,0)
        _WaveAmpA ("Wave Amplitude A", Float) = 0.45
        _WaveFreqA ("Wave Frequency A", Float) = 0.2
        _WaveSpeedA ("Wave Speed A", Float) = 1.0

        _WaveDirB ("Wave Direction B", Vector) = (-0.7,0.7,0,0)
        _WaveAmpB ("Wave Amplitude B", Float) = 0.22
        _WaveFreqB ("Wave Frequency B", Float) = 0.4
        _WaveSpeedB ("Wave Speed B", Float) = 0.55

        _WaveDirC ("Wave Direction C", Vector) = (0.35,0.9,0,0)
        _WaveAmpC ("Wave Amplitude C", Float) = 0.12
        _WaveFreqC ("Wave Frequency C", Float) = 0.9
        _WaveSpeedC ("Wave Speed C", Float) = 1.35

        _BaseHeight ("Base Height", Float) = 0
        _QualityTier ("Quality Tier", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local __ UVS_WATER_FOAM
            #pragma multi_compile_local __ UVS_WATER_DEPTH
            #pragma multi_compile_local __ UVS_WATER_CAUSTICS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            TEXTURE2D(_NormalA);
            SAMPLER(sampler_NormalA);
            TEXTURE2D(_NormalB);
            SAMPLER(sampler_NormalB);

            float4 _ShallowColor;
            float4 _DeepColor;
            float4 _FoamColor;

            float _FresnelPower;
            float _FresnelStrength;
            float _Smoothness;
            float _SpecularStrength;

            float _DepthMax;
            float _FoamThreshold;
            float _FoamIntensity;

            float _NormalTiling;
            float _NormalStrength;
            float4 _NormalSpeedA;
            float4 _NormalSpeedB;

            float4 _WaveDirA;
            float _WaveAmpA;
            float _WaveFreqA;
            float _WaveSpeedA;

            float4 _WaveDirB;
            float _WaveAmpB;
            float _WaveFreqB;
            float _WaveSpeedB;

            float4 _WaveDirC;
            float _WaveAmpC;
            float _WaveFreqC;
            float _WaveSpeedC;

            float _BaseHeight;
            float _QualityTier;

            void AddWave(float3 worldPos, float2 dir, float amp, float freq, float speed, out float wave, out float2 gradient)
            {
                float phase = (worldPos.x * dir.x + worldPos.z * dir.y) * freq + _Time.y * speed;
                float s = sin(phase);
                float c = cos(phase);
                wave = s * amp;
                gradient = float2(c * amp * freq * dir.x, c * amp * freq * dir.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float2 dA = normalize(_WaveDirA.xy);
                float2 dB = normalize(_WaveDirB.xy);
                float2 dC = normalize(_WaveDirC.xy);

                float wA; float2 gA;
                float wB; float2 gB;
                float wC; float2 gC;
                AddWave(worldPos, dA, _WaveAmpA, _WaveFreqA, _WaveSpeedA, wA, gA);
                AddWave(worldPos, dB, _WaveAmpB, _WaveFreqB, _WaveSpeedB, wB, gB);
                AddWave(worldPos, dC, _WaveAmpC, _WaveFreqC, _WaveSpeedC, wC, gC);

                float waveOffset = wA + wB;
                float2 grad = gA + gB;

                if (_QualityTier > 1.4)
                {
                    waveOffset += wC;
                    grad += gC;
                }

                worldPos.y = _BaseHeight + waveOffset;

                float3 waveNormal = normalize(float3(-grad.x, 1.0, -grad.y));

                OUT.worldPos = worldPos;
                OUT.normalWS = waveNormal;
                OUT.uv = IN.uv;
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            float3 SampleNormal(float2 uv)
            {
                float2 uvA = uv * _NormalTiling + _NormalSpeedA.xy * _Time.y;
                float2 uvB = uv * (_NormalTiling * 1.35) + _NormalSpeedB.xy * _Time.y;

                float3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalA, sampler_NormalA, uvA));
                float3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalB, sampler_NormalB, uvB));
                float3 n = normalize(nA + nB);
                return float3(n.x * _NormalStrength, n.y, n.z * _NormalStrength);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);

                float3 waveNormal = normalize(IN.normalWS);
                if (_QualityTier > 0.4)
                {
                    float3 detail = SampleNormal(IN.uv);
                    waveNormal = normalize(float3(waveNormal.x + detail.x * 0.35, waveNormal.y, waveNormal.z + detail.z * 0.35));
                }

                float depthFactor = 0.65;
                #if defined(UVS_WATER_DEPTH)
                {
                    float2 uv = IN.screenPos.xy / IN.screenPos.w;
                    float rawDepth = SampleSceneDepth(uv);
                    float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float waterRawDepth = saturate((IN.positionHCS.z / IN.positionHCS.w) * 0.5 + 0.5);
                    float waterEye = LinearEyeDepth(waterRawDepth, _ZBufferParams);
                    float depthDiff = max(0.0, sceneEye - waterEye);
                    depthFactor = saturate(depthDiff / max(0.001, _DepthMax));
                }
                #endif

                float3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);

                float fresnel = pow(1.0 - saturate(dot(viewDir, waveNormal)), _FresnelPower) * _FresnelStrength;
                float3 fresnelCol = lerp(baseCol, float3(0.88, 0.95, 1.0), saturate(fresnel));

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(waveNormal, halfDir)), lerp(8.0, 256.0, _Smoothness)) * _SpecularStrength;

                float foamMask = 0.0;
                #if defined(UVS_WATER_FOAM)
                {
                    float crest = saturate((IN.worldPos.y - (_BaseHeight + _FoamThreshold)) * 4.0);
                    float shore = saturate(1.0 - depthFactor);
                    foamMask = saturate((crest + shore) * _FoamIntensity * 0.5);
                }
                #endif

                #if defined(UVS_WATER_CAUSTICS)
                if (_QualityTier > 1.4)
                {
                    float c = sin((IN.worldPos.x + IN.worldPos.z) * 1.8 + _Time.y * 2.0) * 0.5 + 0.5;
                    fresnelCol += c * 0.05 * (1.0 - depthFactor);
                }
                #endif

                float3 finalCol = fresnelCol + spec * mainLight.color.rgb;
                finalCol = lerp(finalCol, _FoamColor.rgb, foamMask);

                float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthFactor);
                alpha = saturate(alpha + fresnel * 0.2 + foamMask * 0.15);

                return half4(finalCol, alpha);
            }
            ENDHLSL
        }
    }
}
