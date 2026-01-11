Shader "Custom/UnderwaterObject"
{
    Properties
    {
        _MainColor ("Lit Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _WaterHeight ("Water Height", Float) = 0.0
        _WaterColor ("Water Color", Color) = (0.2, 0.4, 0.6, 0.8)
        _WaterInfluence ("Water Influence", Range(0, 1)) = 0.5
        _WaterDepthFade ("Water Depth Fade", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" } // ✅ 关键：让URP识别这个pass
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainColor;
            float4 _ShadowColor;
            float _WaterHeight;
            float4 _WaterColor;
            float _WaterInfluence;
            float _WaterDepthFade;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.worldPos = TransformObjectToWorld(v.positionOS.xyz);

                // 计算世界空间法线
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

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

                // 采样基础纹理
                half4 baseColor = tex2D(_MainTex, i.uv);

                // 根据光照强度混合颜色
                half4 litColor = baseColor * _MainColor;
                half4 shadowColor = baseColor * _ShadowColor;

                // 卡通风格光照混合
                half4 objectColor = lerp(shadowColor, litColor, lightIntensity);

                // 计算水下影响
                float waterInfluence = 0.0;

                if (i.worldPos.y < _WaterHeight)
                {
                    // 水下：根据深度计算影响程度
                    float depth = _WaterHeight - i.worldPos.y;
                    waterInfluence = saturate(depth / _WaterDepthFade) * _WaterInfluence;
                }

                // 混合颜色：光照后的颜色 * (1 - 水影响) + 水颜色 * 水影响
                half4 finalColor = lerp(objectColor, _WaterColor, waterInfluence);

                // 保持不透明度
                finalColor.a = objectColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }
}