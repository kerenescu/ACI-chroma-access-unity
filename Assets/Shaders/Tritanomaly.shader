Shader "ChromaAccess/Tritanomaly"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Tritanomaly"
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
                // Machado et al. (2009), supplementary Table 1, tritanomaly severity 1.0 (approximation, not tritanopia).
                float3 simulated = float3(
                    dot(float3(1.255528, -0.076749, -0.178779), source.rgb),
                    dot(float3(-0.078411, 0.930809, 0.147602), source.rgb),
                    dot(float3(0.004733, 0.691367, 0.303900), source.rgb));
                return float4(saturate(simulated), source.a);
            }
            ENDHLSL
        }
    }
}
