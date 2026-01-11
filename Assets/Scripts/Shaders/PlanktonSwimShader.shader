Shader "Custom/PlanktonSwim"
{
    Properties
    {
        _MainColor ("Lit Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.5,0.5,0.5,1)
        _MainTex ("Texture", 2D) = "white" {}
        _SwimSpeed ("Swim Speed", Float) = 5.0
        _ForwardAmp ("Forward Amplitude", Float) = 2.0
        _SideAmp ("Side Amplitude", Float) = 0.3
        _Frequency ("Wave Frequency", Float) = 6.0
        _Intensity ("Animation Intensity", Range(0, 1)) = 0.8
    }
    SubShader
    {
        // 渲染管线标签，适配 URP
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
            float _ForwardAmp;
            float _SideAmp;
            float _Frequency;
            float _Intensity;

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 pos = v.positionOS.xyz;
                float3 finalPos = pos;

                // 只有当 _Intensity > 0 时才进行摆动计算
                if (_Intensity > 0.001f)
                {
                    // 参考鱼 shader：前半截保持不变，后半截延迟跟上
                    float tailWeight = 1.0f - smoothstep(-0.3f, 0.3f, pos.z); // Z 值大的顶点（前部）权重为0，Z 值小的顶点（后部）权重为1

                    // 前后波浪：基于时间 + 顶点 Z 轴位置
                    float forwardWave = sin(_Time.y * _SwimSpeed + pos.z * _Frequency) * _ForwardAmp * _Intensity * tailWeight;

                    // 左右轻微摆动（可选，幅度很小）
                    float sideWave = sin(_Time.y * _SwimSpeed * 0.7f + pos.y * _Frequency * 0.5f) * _SideAmp * _Intensity;

                    float3 distortedPos = pos;
                    distortedPos.z += forwardWave;  // 前后摆动（主要效果）
                    distortedPos.x += sideWave;    // 左右轻微摆动

                    // 基于强度在原始形状和扭曲形状之间插值
                    finalPos = lerp(pos, distortedPos, _Intensity);
                }
                // 当 _Intensity <= 0.001f 时，finalPos 保持为 pos，完全静止

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
            float _ForwardAmp;
            float _SideAmp;
            float _Frequency;
            float _Intensity;

            Varyings ShadowPassVertex(Attributes v)
            {
                Varyings o;

                float3 pos = v.positionOS.xyz;
                float3 finalPos = pos;

                // 应用与主Pass相同的动画逻辑
                if (_Intensity > 0.001f)
                {
                    float tailWeight = 1.0f - smoothstep(-0.3f, 0.3f, pos.z);
                    float forwardWave = sin(_Time.y * _SwimSpeed + pos.z * _Frequency) * _ForwardAmp * _Intensity * tailWeight;
                    float sideWave = sin(_Time.y * _SwimSpeed * 0.7f + pos.y * _Frequency * 0.5f) * _SideAmp * _Intensity;

                    float3 distortedPos = pos;
                    distortedPos.z += forwardWave;
                    distortedPos.x += sideWave;

                    finalPos = lerp(pos, distortedPos, _Intensity);
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