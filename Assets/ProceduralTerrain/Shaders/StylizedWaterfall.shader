Shader "ProceduralTerrain/StylizedWaterfall"
{
    Properties
    {
        _WaterColor ("Water Base Color", Color) = (0.28, 0.82, 0.88, 0.85)
        _FoamColor ("Foam Color", Color) = (1.0, 1.0, 1.0, 0.95)
        _ScrollSpeed ("Vertical Flow Speed", Float) = 2.4
        _FoamDensity ("Foam Density", Float) = 6.0
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
                float _FoamDensity;
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

                // Scrolling vertical flow
                float flowY = uv.y - _Time.y * _ScrollSpeed;

                // Procedural vertical streak noise
                float streak1 = sin(uv.x * _FoamDensity * 3.14 + flowY * 5.0);
                float streak2 = cos((uv.x + 0.3) * _FoamDensity * 2.5 + flowY * 8.0);
                float foamPattern = saturate((streak1 + streak2) * 0.5 + 0.4);

                // Soft edge fade on sides
                float edgeFade = 1.0 - pow(abs(uv.x - 0.5) * 2.0, 3.0);

                // Blend water and foam
                half4 col = lerp(_WaterColor, _FoamColor, foamPattern);
                col.a *= edgeFade * input.color.a;

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
