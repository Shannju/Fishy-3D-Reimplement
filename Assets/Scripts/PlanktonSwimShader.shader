Shader "Custom/PlanktonSwim"
{
    Properties
    {
        _MainColor ("Plankton Color", Color) = (1,1,1,1)
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