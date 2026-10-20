Shader "DesktopFish/ApronAtlas"
{
    Properties
    {
        _MainTex ("Original atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _MascotMode ("Redesigned desktop details", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Color;
            float _Cutoff, _MascotMode;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            float Box(float2 uv, float4 box)
            {
                return step(box.x, uv.x) * step(uv.x, box.z) * step(box.y, uv.y) * step(uv.y, box.w);
            }
            float Ink(float3 rgb)
            {
                return saturate((rgb.b-rgb.r)*10) * saturate((rgb.b-rgb.g)*12) *
                    (1-smoothstep(0.40,0.70,rgb.r));
            }
            float Disc(float2 uv,float2 center,float2 radius)
            {
                float d=length((uv-center)/radius);
                return 1-smoothstep(0.85,1.15,d);
            }
            fixed4 frag(Output input) : SV_Target
            {
                float2 uv = input.uv;
                float4 color = tex2D(_MainTex, uv);
                // Reuse only the blue ink of the existing motif. Keep the atlas's
                // white cloth, lace and seams at their original UV coordinates.
                float4 logoBox = float4(0.517,0.172,0.583,0.242);
                float oldInk = smoothstep(0.517,0.520,uv.x) * (1-smoothstep(0.580,0.583,uv.x)) *
                    smoothstep(0.172,0.175,uv.y) * (1-smoothstep(0.239,0.242,uv.y));
                float3 blank = tex2D(_MainTex,uv-float2(0.08,0)).rgb;
                color.rgb = lerp(color.rgb,blank,oldInk);
                // Small detached pastel marks below the old motif are separate
                // islands in the source atlas; remove them without touching lace.
                float oldMarks = saturate(Box(uv,float4(0.500,0.135,0.512,0.145)) +
                    Box(uv,float4(0.511,0.137,0.523,0.147)) + Box(uv,float4(0.544,0.149,0.557,0.160)));
                color.rgb = lerp(color.rgb,tex2D(_MainTex,float2(0.49,uv.y+0.02)).rgb,oldMarks);
                if (_MascotMode > 0.5)
                {
                    float3 rgb = color.rgb;
                    #ifndef UNITY_COLORSPACE_GAMMA
                    rgb = LinearToGammaSpace(rgb);
                    #endif
                    float neutral = smoothstep(0.40,0.70,min(rgb.r,min(rgb.g,rgb.b)));
                    // Preserve the illustrated fabric and lace. Rebuild the shirt
                    // as two broad pleat shadows and three larger buttons.
                    rgb=lerp(rgb,float3(0.96,0.95,0.93),neutral*0.22);
                    float panel=Box(uv,float4(0.445,0.543,0.555,0.704));
                    float folds=exp(-pow((uv.x-0.474)/0.003,2))+exp(-pow((uv.x-0.526)/0.003,2));
                    float3 shirt=float3(0.96,0.95,0.93)-folds*float3(0.065,0.060,0.040);
                    rgb=lerp(rgb,shirt,panel*neutral);
                    float buttons=max(Disc(uv,float2(0.500,0.608),float2(0.0035,0.0050)),
                        max(Disc(uv,float2(0.500,0.581),float2(0.0035,0.0050)),
                            Disc(uv,float2(0.500,0.557),float2(0.0035,0.0050))));
                    rgb=lerp(rgb,float3(0.17,0.22,0.37),buttons);
                    #ifndef UNITY_COLORSPACE_GAMMA
                    rgb = GammaToLinearSpace(rgb);
                    #endif
                    color.rgb = rgb;
                }
                float2 logoUV = float2(0.550,0.207) + (uv-float2(0.500,0.290))/lerp(1.50,2.40,_MascotMode);
                float4 logo = tex2D(_MainTex,logoUV);
                float coverage = Box(logoUV,logoBox) * Ink(logo.rgb);
                float3 mascotInk=float3(0.35,0.54,0.75);
                #ifndef UNITY_COLORSPACE_GAMMA
                mascotInk=GammaToLinearSpace(mascotInk);
                #endif
                logo.rgb=lerp(logo.rgb,mascotInk,_MascotMode*0.75);
                color.rgb = lerp(color.rgb,logo.rgb,coverage);
                color *= _Color;
                clip(color.a-_Cutoff);
                return color;
            }
            ENDCG
        }
    }
}
