Shader "ChromaAccess/Protanopia"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Protanopia"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 source = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, input.texcoord.xy, 0);
                // Machado et al. (2009), supplementary Table 1, protanopia severity 1.0.
                float3 simulated = float3(
                    dot(float3(0.152286, 1.052583, -0.204868), source.rgb),
                    dot(float3(0.114503, 0.786281, 0.099216), source.rgb),
                    dot(float3(-0.003882, -0.048116, 1.051998), source.rgb));
                return float4(saturate(simulated), source.a);
            }
            ENDHLSL
        }
    }
}
