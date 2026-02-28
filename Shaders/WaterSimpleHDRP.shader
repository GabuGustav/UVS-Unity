Shader "UVS/WaterSimpleHDRP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.04, 0.2, 0.35, 0.72)
        _FresnelColor("Fresnel Color", Color) = (0.8, 0.92, 1, 1)
        _FresnelPower("Fresnel Power", Float) = 4
        _WaveAmpA("Wave Amp A", Float) = 0.45
        _WaveFreqA("Wave Freq A", Float) = 0.2
        _WaveSpeedA("Wave Speed A", Float) = 1
        _WaveDirA("Wave Dir A", Vector) = (1, 0, 0, 0)
        _BaseHeight("Base Height", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Forward"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _FresnelColor;
            float _FresnelPower;
            float _WaveAmpA;
            float _WaveFreqA;
            float _WaveSpeedA;
            float4 _WaveDirA;
            float _BaseHeight;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 dir = normalize(_WaveDirA.xy);
                float phase = (worldPos.x * dir.x + worldPos.z * dir.y) * _WaveFreqA + _Time.y * _WaveSpeedA;
                worldPos.y = _BaseHeight + sin(phase) * _WaveAmpA;
                float slope = cos(phase) * _WaveAmpA * _WaveFreqA;
                o.normalWS = normalize(float3(-dir.x * slope, 1, -dir.y * slope));
                o.worldPos = worldPos;
                o.pos = UnityWorldToClipPos(float4(worldPos, 1));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fresnel = pow(1.0 - saturate(dot(viewDir, normalize(i.normalWS))), _FresnelPower);
                float3 col = lerp(_BaseColor.rgb, _FresnelColor.rgb, fresnel);
                return fixed4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
