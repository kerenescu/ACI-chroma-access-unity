Shader "ChromaAccess/Deuteranopia"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Deuteranopia"
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
                // Machado et al. (2009), supplementary Table 1, deutan severity 1.0.
                float3 simulated = float3(
                    dot(float3(0.367322, 0.860646, -0.227968), source.rgb),
                    dot(float3(0.280085, 0.672501, 0.047413), source.rgb),
                    dot(float3(-0.011820, 0.042940, 0.968881), source.rgb));
                return float4(saturate(simulated), source.a);
            }
            ENDHLSL
        }
    }
}
