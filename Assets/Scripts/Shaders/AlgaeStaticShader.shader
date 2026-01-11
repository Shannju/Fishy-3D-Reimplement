Shader "Custom/Still"
{
    Properties
    {
        _MainColor ("Lit Color", Color) = (0.2, 0.8, 0.3, 1)
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.4, 0.15, 1)
        _MainTex ("Texture", 2D) = "white" {}
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

            Varyings vert(Attributes v)
            {
                Varyings o;

                // 静态版本：移除呼吸动效，直接使用原始顶点位置
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;

                // 计算世界空间法线
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                // 计算阴影坐标
                o.shadowCoord = TransformWorldToShadowCoord(TransformObjectToWorld(v.positionOS.xyz));

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 获取主光源方向
                Light mainLight = GetMainLight();

                // 计算光照强度：法线和光源方向的点积
                float NdotL = dot(normalize(i.normalWS), mainLight.direction);
                // 卡通风格：使用阶梯函数创建高对比度
                float lightIntensity = step(0.0, NdotL); // 简单的二值化：亮或暗

                // 计算阴影衰减
                float shadowAttenuation = MainLightRealtimeShadow(i.shadowCoord);

                // 将阴影衰减应用到光照强度
                lightIntensity *= shadowAttenuation;

                // 根据光照强度混合颜色
                half4 baseColor = tex2D(_MainTex, i.uv);
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

            Varyings ShadowPassVertex(Attributes v)
            {
                Varyings o;

                // 静态版本：直接使用原始顶点位置
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
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