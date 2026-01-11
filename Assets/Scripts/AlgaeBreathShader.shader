Shader "Custom/AlgaeBreath"
{
    Properties
    {
        _MainColor ("Algae Color", Color) = (0.2, 0.8, 0.3, 1)
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _MainColor;
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
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv) * _MainColor;
                return col;
            }
            ENDHLSL
        }
    }
}