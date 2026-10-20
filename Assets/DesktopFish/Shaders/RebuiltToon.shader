Shader "DesktopFish/Rebuilt Toon"
{
    Properties {
        _MainTex ("Authored surface paint", 2D) = "white" {}
        _Flat ("Flat illustration", Float) = 0
        _OutlineWidth ("Outline width", Float) = 0.0012
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; };
            float4 vert(app v):SV_POSITION { return UnityObjectToClipPos(v.vertex+float4(v.normal*_OutlineWidth,0)); }
            fixed4 frag():SV_Target { return fixed4(0.035,0.047,0.095,1); }
            ENDCG
        }
        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Flat;
            sampler2D _MainTex;
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float4 color:COLOR; float2 uv:TEXCOORD1; float skin:TEXCOORD2; };
            v2f vert(app v) {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal); o.color=v.color;
                o.uv=v.uv;
                // Classify the authored warm skin before color-space conversion.
                // Ivory cloth stays softly lit, while skin keeps more roundness.
                o.skin=smoothstep(.025,.050,v.color.r-v.color.g);
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb=GammaToLinearSpace(o.color.rgb);
                #endif
                return o;
            }
            fixed4 frag(v2f i):SV_Target {
                float light=dot(normalize(i.normal),normalize(float3(-0.35,0.65,0.80)));
                float shade=lerp(0.76,1.04,smoothstep(-0.45,0.95,light));
                float3 albedo=i.color.rgb*tex2D(_MainTex,i.uv).rgb;
                float ivory=smoothstep(.60,.85,min(albedo.r,min(albedo.g,albedo.b)));
                shade=lerp(shade,.99,ivory*.75*(1-.55*i.skin));
                return float4(albedo*lerp(shade,1,_Flat),1);
            }
            ENDCG
        }
    }
}
