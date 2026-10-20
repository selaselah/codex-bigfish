Shader "Hidden/DesktopFish/IllustratedPalette"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv);
                if (color.a <= 0.0001) return 0;
                float3 rgb = color.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                rgb = LinearToGammaSpace(rgb);
                #endif
                float luminance = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                rgb = lerp(luminance.xxx, rgb, 0.80);
                rgb = saturate(((rgb - 0.5) * 0.97 + 0.5) * float3(1.015, 1.005, 0.985));
                #ifndef UNITY_COLORSPACE_GAMMA
                rgb = GammaToLinearSpace(rgb);
                #endif
                return float4(rgb, color.a);
            }
            ENDCG
        }
    }
}
