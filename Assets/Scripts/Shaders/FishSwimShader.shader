Shader "Custom/FishSwim"
{
    Properties
    {
        _MainColor ("Lit Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.5,0.5,0.5,1)
        _MainTex ("Texture", 2D) = "white" {}
        _SwimSpeed ("Swim Speed", Float) = 5.0
        _WaveFreq ("Wave Frequency", Float) = 2.0
        _WaveAmp ("Wave Amplitude", Float) = 0.1
        _LerpFactor ("Lerp Factor (Velocity Control)", Range(0, 1)) = 0.0
    }
    SubShader
    {
        // 渲染管线标签，适配 URP
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
            };

            float4 _MainColor;
            float4 _ShadowColor;
            sampler2D _MainTex;
            float _SwimSpeed;
            float _WaveFreq;
            float _WaveAmp;
            float _LerpFactor;

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 pos = v.positionOS.xyz;

                float3 finalPos = pos;

                // 只有当 _LerpFactor > 0 时才进行摆动计算
                if (_LerpFactor > 0.001f)
                {
                    // 计算波浪：基于时间 + 顶点在该物体坐标系下的 Z 轴位置
                    // 前半截保持不变，后半截延迟跟上
                    float tailWeight = 1.0f - smoothstep(-0.5f, 0.5f, pos.z); // Z 值大的顶点（鱼头）权重为0，Z 值小的顶点（鱼尾）权重为1
                    float wave = sin(_Time.y * _SwimSpeed + pos.z * _WaveFreq) * _WaveAmp * _LerpFactor * tailWeight;

                // 只有在 Z 轴上发生偏移
                float3 distortedPos = pos;
                distortedPos.z += wave;

                    // 基于 _LerpFactor 在原始形状和扭曲形状之间插值
                    finalPos = lerp(pos, distortedPos, _LerpFactor);
                }
                // 当 _LerpFactor <= 0.001f 时，finalPos 保持为 pos，完全静止

                o.positionHCS = TransformObjectToHClip(finalPos);
                o.uv = v.uv;

                // 计算世界空间法线
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                // 计算阴影坐标
                o.shadowCoord = TransformWorldToShadowCoord(TransformObjectToWorld(finalPos));

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 获取主光源
                Light mainLight = GetMainLight();

                // 计算光照强度：法线和光源方向的点积
                float NdotL = dot(normalize(i.normalWS), mainLight.direction);
                // 卡通风格：使用阶梯函数创建高对比度
                float lightIntensity = step(0.0, NdotL); // 简单的二值化：亮或暗

                // 计算阴影衰减
                float shadowAttenuation = MainLightRealtimeShadow(i.shadowCoord);

                // 将阴影衰减应用到光照强度
                lightIntensity *= shadowAttenuation;

                // 采样纹理
                half4 baseColor = tex2D(_MainTex, i.uv);

                // 根据光照强度混合颜色
                half4 litColor = baseColor * _MainColor;
                half4 shadowColor = baseColor * _ShadowColor;

                // 卡通风格混合（包含阴影效果）
                half4 finalColor = lerp(shadowColor, litColor, lightIntensity);

                return finalColor;
            }
            ENDHLSL
        }

        // 阴影投射Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float _SwimSpeed;
            float _WaveFreq;
            float _WaveAmp;
            float _LerpFactor;

            Varyings ShadowPassVertex(Attributes v)
            {
                Varyings o;

                float3 pos = v.positionOS.xyz;
                float3 finalPos = pos;

                // 应用与主Pass相同的游泳动画逻辑
                if (_LerpFactor > 0.001f)
                {
                    float tailWeight = 1.0f - smoothstep(-0.5f, 0.5f, pos.z);
                    float wave = sin(_Time.y * _SwimSpeed + pos.z * _WaveFreq) * _WaveAmp * _LerpFactor * tailWeight;

                    float3 distortedPos = pos;
                    distortedPos.z += wave;

                    finalPos = lerp(pos, distortedPos, _LerpFactor);
                }

                o.positionHCS = TransformObjectToHClip(finalPos);
                return o;
            }

            half4 ShadowPassFragment(Varyings i) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}