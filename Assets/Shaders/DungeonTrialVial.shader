Shader "ChromaAccess/DungeonTrialVial"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _PreviewMode("Developer preview: 0 none, 1 deutan, 2 protan, 3 tritanomaly", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _PreviewMode;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float3 SimulateMachado(float3 source, float previewMode)
            {
                // Machado, Oliveira and Fernandes (2009), Table 1,
                // severity 1.0. The project is in Linear RGB/sRGB primaries.
                if (previewMode < 1.5)
                {
                    // Deuteranopia.
                    return float3(
                        dot(float3(0.367322, 0.860646, -0.227968), source),
                        dot(float3(0.280085, 0.672501, 0.047413), source),
                        dot(float3(-0.011820, 0.042940, 0.968881), source));
                }

                if (previewMode < 2.5)
                {
                    // Protanopia.
                    return float3(
                        dot(float3(0.152286, 1.052583, -0.204868), source),
                        dot(float3(0.114503, 0.786281, 0.099216), source),
                        dot(float3(-0.003882, -0.048116, 1.051998), source));
                }

                // Tritanomaly severity 1.0 approximation, not tritanopia.
                return float3(
                    dot(float3(1.255528, -0.076749, -0.178779), source),
                    dot(float3(-0.078411, 0.930809, 0.147602), source),
                    dot(float3(0.004733, 0.691367, 0.303900), source));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 source = _BaseColor.rgb;
                float3 output = _PreviewMode < 0.5
                    ? source
                    : saturate(SimulateMachado(source, _PreviewMode));

                // Preview is limited to trial markers, not the complete scene.
                return half4(output, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
