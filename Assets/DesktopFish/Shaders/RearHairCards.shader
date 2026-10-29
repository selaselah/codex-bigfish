Shader "DesktopFish/Rear Hair Alpha Cards" {
    Properties { _MainTex ("Authored rear hair", 2D)="white" {} _Flat ("Flat illustration",Float)=0 }
    SubShader {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass {
            Cull Off ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _Flat;
            struct app {float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct vary {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float4 color:COLOR;float2 uv:TEXCOORD1;};
            vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;o.uv=v.uv;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb=GammaToLinearSpace(o.color.rgb);
                #endif
                return o;}
            fixed4 frag(vary i,float facing:VFACE):SV_Target{
                fixed4 paint=tex2D(_MainTex,i.uv);clip(paint.a-.5);
                float3 n=normalize(i.normal)*(facing>=0?1:-1);
                float light=dot(n,normalize(float3(-.35,.65,.80)));
                float shade=lerp(.76,1.04,smoothstep(-.45,.95,light));
                return float4(i.color.rgb*paint.rgb*lerp(shade,1,_Flat),1);
            }
            ENDCG
        }
    }
}
