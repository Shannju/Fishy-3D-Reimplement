Shader "Custom/URP/IntersectionOutline_Unity6"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.7, 1, 0.35)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _EdgeWidth ("Edge Width", Range(0.0001, 0.5)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.0001, 0.2)) = 0.03

        // 水面运动参数
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.5)) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 2.0
        _WaveDirection ("Wave Direction", Vector) = (1, 0, 0, 0)
        _WaveIntensity ("Wave Intensity", Range(0, 1)) = 0.8

        // 基础颜色动画参数
        _ColorWaveIntensity ("Color Wave Intensity", Range(0, 1)) = 0.3
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

                // 水面运动参数
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float4 _WaveDirection;
                float _WaveIntensity;

                // 基础颜色动画参数
                float _ColorWaveIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionVS  : TEXCOORD0; // view space position
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2; // world position for color animation
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                // 获取原始顶点位置
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);

                // 计算水面波浪动画
                float waveOffset = 0.0;

                // 主要波浪
                float wave1 = sin(_Time.y * _WaveSpeed + dot(worldPos.xz, _WaveDirection.xz) * _WaveFrequency);
                waveOffset += wave1 * _WaveAmplitude;

                // 次要波浪 - 不同方向和频率，创建更自然的效果
                float wave2 = sin(_Time.y * _WaveSpeed * 0.7 + dot(worldPos.xz, float2(_WaveDirection.z, -_WaveDirection.x)) * _WaveFrequency * 0.5);
                waveOffset += wave2 * _WaveAmplitude * 0.3;

                // 应用波浪强度控制
                waveOffset *= _WaveIntensity;

                // 应用波浪到Y轴（垂直位移）
                worldPos.y += waveOffset;

                // 转换回对象空间进行渲染
                float3 objectPos = TransformWorldToObject(worldPos);

                VertexPositionInputs pos = GetVertexPositionInputs(objectPos);
                o.positionHCS = pos.positionCS;
                o.positionVS  = pos.positionVS.xyz;
                o.uv = v.uv;
                o.worldPos = worldPos; // 传递世界坐标用于颜色动画
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

                // 创建水面颜色动画效果
                float waveColor = sin(_Time.y * _WaveSpeed * 1.2 + dot(i.worldPos.xz, _WaveDirection.xz) * _WaveFrequency * 0.8);
                waveColor = waveColor * 0.5 + 0.5; // 转换为0-1范围

                // 微调基础颜色，让它随波浪轻微变化
                half4 baseColor = _BaseColor;
                baseColor.rgb *= (0.9 + waveColor * 0.2 * _ColorWaveIntensity); // 颜色亮度轻微变化
                baseColor.a *= (0.8 + waveColor * 0.4 * _ColorWaveIntensity); // 透明度轻微变化

                // 在边缘区域使用outline颜色，在水面区域使用基础颜色
                half4 col = lerp(baseColor, half4(_OutlineColor.rgb, _OutlineColor.a), edge);
                col.a *= step(0.0, d); // 确保只有在水面上方才显示

                return col;
            }
            ENDHLSL
        }
    }
}
