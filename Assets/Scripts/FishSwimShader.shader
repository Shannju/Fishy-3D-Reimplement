Shader "Custom/FishSwim"
{
    Properties
    {
        _MainColor ("Fish Color", Color) = (1,1,1,1)
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