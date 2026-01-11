Shader "Custom/WaterSurface"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.6, 0.8, 0.9, 0.5)
        _FresnelColor ("Fresnel Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _FresnelPower ("Fresnel Power", Range(0.1, 5.0)) = 2.0
        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.02
        _WaveSpeed ("Wave Speed", Range(0, 2.0)) = 0.5
        _NoiseScale ("Noise Scale", Range(1, 20)) = 5.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 viewDirWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            float4 _BaseColor;
            float4 _FresnelColor;
            float _FresnelPower;
            float _WaveStrength;
            float _WaveSpeed;
            float _NoiseScale;

            // 简单的噪声函数
            float SimpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);

                // 添加轻微波动效果
                float time = _Time.y * _WaveSpeed;
                float noise1 = SimpleNoise(worldPos.xz * _NoiseScale + time);
                float noise2 = SimpleNoise(worldPos.xz * _NoiseScale * 0.5 - time * 0.7);

                // 扰动顶点位置（只在Y轴上）
                worldPos.y += (noise1 + noise2 - 1.0) * _WaveStrength;

                o.positionHCS = TransformWorldToHClip(worldPos);
                o.worldPos = worldPos;
                o.uv = v.uv;

                // 计算世界空间法线
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                // 计算世界空间视角方向
                float3 cameraPosWS = GetCameraPositionWS();
                o.viewDirWS = normalize(cameraPosWS - worldPos);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Fresnel效果：计算视角方向和法线的夹角
                float fresnel = pow(1.0 - saturate(dot(normalize(i.viewDirWS), normalize(i.normalWS))), _FresnelPower);

                // 基础颜色
                half4 baseColor = _BaseColor;

                // 根据Fresnel混合颜色
                // Fresnel越强，越接近Fresnel颜色；越弱，越接近基础颜色
                half4 finalColor = lerp(baseColor, _FresnelColor, fresnel);

                // 保持基础透明度
                finalColor.a = baseColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }
}