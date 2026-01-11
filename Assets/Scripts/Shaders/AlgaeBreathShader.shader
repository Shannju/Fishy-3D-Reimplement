Shader "Custom/AlgaeBreath"
{
    Properties
    {
        _MainColor ("Lit Color", Color) = (0.2, 0.8, 0.3, 1)
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.4, 0.15, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _BreathSpeed ("Breath Speed", Float) = 1.0
        _BreathScale ("Breath Scale Amount", Range(0.8, 1.2)) = 1.05
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
            float _BreathSpeed;
            float _BreathScale;

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 pos = v.positionOS.xyz;

                // 呼吸效果：基于时间的缓慢缩放
                // 使用sin函数创建周期性变化，范围从0.8到1.2（通过_BreathScale控制）
                float breathFactor = (_BreathScale - 1.0) * sin(_Time.y * _BreathSpeed) + 1.0;

                // 对所有轴进行均匀缩放，创建呼吸般的膨胀收缩效果
                float3 finalPos = pos * breathFactor;

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

            float _BreathSpeed;
            float _BreathScale;

            Varyings ShadowPassVertex(Attributes v)
            {
                Varyings o;

                float3 pos = v.positionOS.xyz;

                // 应用与主Pass相同的呼吸动画逻辑
                float breathFactor = (_BreathScale - 1.0) * sin(_Time.y * _BreathSpeed) + 1.0;
                float3 finalPos = pos * breathFactor;

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