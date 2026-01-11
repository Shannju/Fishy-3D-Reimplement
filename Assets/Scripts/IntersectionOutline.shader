Shader "Custom/URP/IntersectionOutline_Unity6"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.7, 1, 0.35)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _EdgeWidth ("Edge Width", Range(0.0001, 0.5)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.0001, 0.2)) = 0.03
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                float _EdgeWidth;
                float _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionVS  : TEXCOORD0; // view space position
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionHCS = pos.positionCS;
                o.positionVS  = pos.positionVS.xyz;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Unity 6 / URP 推荐的屏幕 UV 获取方式
                float2 uv = GetNormalizedScreenSpaceUV(i.positionHCS);

                // Scene depth (raw 0..1)
                float rawSceneDepth = SampleSceneDepth(uv);

                // Unity 6 文档：非 UNITY_REVERSED_Z 需要调整到 NDC 深度语义再线性化
                #if !UNITY_REVERSED_Z
                rawSceneDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawSceneDepth);
                #endif

                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);

                // Self eye depth：用 view space z 更稳（透视/正交都能用）
                float selfEyeDepth = -i.positionVS.z;

                float d = sceneEyeDepth - selfEyeDepth;

                float edge = 1.0 - smoothstep(_EdgeWidth, _EdgeWidth + _EdgeSoftness, d);
                edge *= step(0.0, d);

                // 只有边缘有颜色，中间完全透明
                half4 col = half4(_OutlineColor.rgb, edge * _OutlineColor.a);
                return col;
            }
            ENDHLSL
        }
    }
}
