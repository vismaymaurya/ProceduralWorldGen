Shader "ProceduralTerrain/StylizedWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.28, 0.85, 0.82, 0.75)
        _DeepColor ("Deep Color", Color) = (0.08, 0.32, 0.58, 0.92)
        _FoamColor ("Foam Color", Color) = (1.0, 1.0, 1.0, 0.95)
        _DepthDistance ("Depth Gradient Distance", Range(0.1, 10.0)) = 2.5
        _FoamThreshold ("Foam Width", Range(0.01, 1.5)) = 0.45
        _WaveSpeed ("Wave Speed", Float) = 0.8
        _WaveFrequency ("Wave Frequency", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 uv           : TEXCOORD1;
                float4 screenPos    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _DepthDistance;
                float _FoamThreshold;
                float _WaveSpeed;
                float _WaveFrequency;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Gentle vertical wave undulating
                float wave = sin(_Time.y * _WaveSpeed + (input.positionOS.x + input.positionOS.z) * 0.15) * 0.08;
                float3 posOS = input.positionOS.xyz;
                posOS.y += wave;

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Screen depth calculation for shoreline foam and depth gradient
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterDepth = input.screenPos.w;
                float depthDiff = sceneDepth - waterDepth;

                // Subtle animated ripple
                float ripple = sin(_Time.y * 2.0 + (input.uv.x + input.uv.y) * _WaveFrequency) * 0.05;
                float adjustedDepth = max(0.0, depthDiff + ripple);

                // Depth color gradient
                float depthFactor = saturate(adjustedDepth / _DepthDistance);
                half4 waterCol = lerp(_ShallowColor, _DeepColor, depthFactor);

                // Shoreline foam line
                float foamWave = sin(_Time.y * 3.0 + input.positionWS.x * 0.8 + input.positionWS.z * 0.8) * 0.08;
                float foamFactor = 1.0 - saturate((adjustedDepth + foamWave) / _FoamThreshold);
                foamFactor = smoothstep(0.1, 0.9, foamFactor);

                half4 finalColor = lerp(waterCol, _FoamColor, foamFactor);

                // Stylized sun specular reflection
                Light mainLight = GetMainLight();
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(float3(0, 1, 0), halfDir)), 32.0);

                finalColor.rgb += spec * 0.35 * mainLight.color;

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
