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

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 获取主光源方向
                Light mainLight = GetMainLight();

                // 计算光照强度：法线和光源方向的点积
                float NdotL = dot(normalize(i.normalWS), mainLight.direction);
                // 将范围从 [-1, 1] 映射到 [0, 1]
                float lightIntensity = (NdotL + 1.0) * 0.5;

                // 根据光照强度混合颜色
                half4 baseColor = tex2D(_MainTex, i.uv);
                half4 litColor = baseColor * _MainColor;
                half4 shadowColor = baseColor * _ShadowColor;

                // 简单的线性插值混合
                half4 finalColor = lerp(shadowColor, litColor, lightIntensity);

                return finalColor;
            }
            ENDHLSL
        }
    }
}