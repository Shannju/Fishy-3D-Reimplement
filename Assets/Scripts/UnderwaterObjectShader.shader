Shader "Custom/UnderwaterObject"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)
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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainColor;
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
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 采样基础纹理
                half4 baseColor = tex2D(_MainTex, i.uv) * _MainColor;

                // 计算水下影响
                float waterInfluence = 0.0;

                if (i.worldPos.y < _WaterHeight)
                {
                    // 水下：根据深度计算影响程度
                    float depth = _WaterHeight - i.worldPos.y;
                    waterInfluence = saturate(depth / _WaterDepthFade) * _WaterInfluence;
                }

                // 混合颜色：原本颜色 * (1 - 水影响) + 水颜色 * 水影响
                half4 finalColor = lerp(baseColor, _WaterColor, waterInfluence);

                // 保持不透明度（如果需要透明效果可以调整）
                finalColor.a = baseColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }
}