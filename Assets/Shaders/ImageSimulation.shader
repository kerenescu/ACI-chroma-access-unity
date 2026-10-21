Shader "Hidden/ChromaAccess/ImageSimulation"
{
    Properties { _MainTex ("Image", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Row0, _Row1, _Row2;

            float DecodeSrgb(float value)
            {
                return value <= 0.04045 ? value / 12.92 : pow((value + 0.055) / 1.055, 2.4);
            }

            float4 Frag(v2f_img input) : SV_Target
            {
                float4 source = tex2D(_MainTex, input.uv);
                // Encoded bytes are uploaded without hardware sRGB sampling so that
                // the piecewise decode remains explicit, including near-black output.
                float3 sourceLinear = float3(DecodeSrgb(source.r), DecodeSrgb(source.g), DecodeSrgb(source.b));
                float3 result = float3(dot(_Row0.xyz, sourceLinear),
                    dot(_Row1.xyz, sourceLinear), dot(_Row2.xyz, sourceLinear));
                return float4(saturate(result), source.a);
            }
            ENDHLSL
        }
    }
}
