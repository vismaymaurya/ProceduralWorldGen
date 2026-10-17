Shader "ProceduralTerrain/StylizedTerrain"
{
    Properties
    {
        _GrassTint ("Grass Tint", Color) = (0.38, 0.76, 0.28, 1.0)
        _SandTint ("Sand Tint", Color) = (0.92, 0.85, 0.66, 1.0)
        _CliffTint ("Cliff Tint", Color) = (0.55, 0.52, 0.48, 1.0)
        _SnowTint ("Snow Tint", Color) = (0.95, 0.98, 1.0, 1.0)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _ShadowSoftness ("Shadow Wrap", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 color        : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GrassTint;
                float4 _SandTint;
                float4 _CliffTint;
                float4 _SnowTint;
                float _Smoothness;
                float _ShadowSoftness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight();

                // Stylized warm half-lambert wrap lighting
                float NdotL = dot(normal, mainLight.direction);
                float wrap = _ShadowSoftness;
                float lightIntensity = saturate((NdotL + wrap) / (1.0 + wrap));

                // Ambient light
                half3 ambient = half3(0.35, 0.38, 0.44);

                // Combined color from vertex baked palette and tints
                half3 albedo = input.color.rgb;

                half3 finalColor = albedo * (mainLight.color * lightIntensity + ambient);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
