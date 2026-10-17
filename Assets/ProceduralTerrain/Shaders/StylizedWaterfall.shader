Shader "ProceduralTerrain/StylizedWaterfall"
{
    Properties
    {
        _WaterColor ("Water Base Color", Color) = (0.22, 0.72, 0.90, 0.85)
        _FoamColor ("Foam Color", Color) = (0.95, 0.98, 1.0, 0.95)
        _ScrollSpeed ("Vertical Flow Speed", Float) = 1.8
        _StreakFrequency ("Vertical Streak Count", Float) = 8.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "WaterfallForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float4 _FoamColor;
                float _ScrollSpeed;
                float _StreakFrequency;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = posInputs.positionCS;
                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Support circular splash foam discs when uv is configured as radial
                if (input.color.r > 1.5) // flag for splash disc
                {
                    float distFromCenter = length(uv - float2(0.5, 0.5)) * 2.0;
                    if (distFromCenter > 1.0) discard;
                    float ring = sin(distFromCenter * 12.0 - _Time.y * 3.0) * 0.5 + 0.5;
                    float alpha = smoothstep(1.0, 0.2, distFromCenter) * (0.6 + 0.4 * ring);
                    return half4(_FoamColor.rgb, alpha * 0.85);
                }

                // Vertical downward flow
                float flowTime = _Time.y * _ScrollSpeed;
                float vFlow = uv.y * 2.5 - flowTime;

                // Organic vertical streaks (dominant X variation, subtle Y variation for natural flow)
                float streak1 = sin(uv.x * _StreakFrequency * 3.14159 + sin(vFlow * 0.8) * 0.6) * 0.5 + 0.5;
                float streak2 = cos((uv.x + 0.25) * (_StreakFrequency * 1.5) * 3.14159 + vFlow * 0.4) * 0.5 + 0.5;
                float foamIntensity = saturate(streak1 * 0.6 + streak2 * 0.4);

                // Vertical gradient: clearer at top lip, frothier towards bottom plunge
                float bottomFroth = smoothstep(0.2, 0.95, uv.y) * 0.45;
                foamIntensity = saturate(foamIntensity + bottomFroth);

                // Smooth edge fade across ribbon width
                float edgeDist = abs(uv.x - 0.5) * 2.0;
                float edgeFade = smoothstep(1.0, 0.65, edgeDist);

                // Blend water and foam smoothly
                half4 col = lerp(_WaterColor, _FoamColor, foamIntensity * 0.7);
                col.a = saturate(col.a * edgeFade * input.color.a);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
