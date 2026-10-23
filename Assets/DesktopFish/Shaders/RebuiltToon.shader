Shader "DesktopFish/Rebuilt Toon"
{
    Properties {
        _MainTex ("Authored surface paint", 2D) = "white" {}
        _Flat ("Flat illustration", Float) = 0
        _LightDirection ("Reviewable light direction", Vector) = (-0.35,0.65,0.80,0)
        _OutlineWidth ("Outline width", Float) = 0.0012
    }
    SubShader {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            sampler2D _MainTex;
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(app v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex+float4(v.normal*_OutlineWidth,0));o.uv=v.uv;return o; }
            fixed4 frag(v2f i):SV_Target { clip(tex2D(_MainTex,i.uv).a-.5);return fixed4(0.035,0.047,0.095,1); }
            ENDCG
        }
        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Flat;
            float4 _LightDirection;
            sampler2D _MainTex;
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float4 color:COLOR; float2 uv:TEXCOORD1; float skin:TEXCOORD2; };
            v2f vert(app v) {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal); o.color=v.color;
                o.uv=v.uv;
                // Classify the authored warm skin before color-space conversion.
                // Ivory cloth stays softly lit, while skin keeps more roundness.
                float2 pixel=v.uv*float2(1024,2048);
                bool headSkin=pixel.x>=648&&pixel.x<=752&&pixel.y>=784&&pixel.y<=1080;
                bool socketSkin=pixel.x>=512&&pixel.x<=568&&
                    ((pixel.y>=1696&&pixel.y<=1824)||(pixel.y>=1856&&pixel.y<=1984));
                o.skin=max(smoothstep(.025,.050,v.color.r-v.color.g),(headSkin||socketSkin)?1.0:0.0);
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb=GammaToLinearSpace(o.color.rgb);
                #endif
                return o;
            }
            fixed4 frag(v2f i,float facing:VFACE):SV_Target {
                // Single-surface hair and lace use the same material from both
                // sides. Orient lighting consistently with the visible side.
                float3 normal=normalize(i.normal)*(facing>=0?1:-1);
                float light=dot(normal,normalize(_LightDirection.xyz));
                float shade=lerp(0.76,1.04,smoothstep(-0.45,0.95,light));
                fixed4 paint=tex2D(_MainTex,i.uv);
                clip(paint.a-.5);
                float3 albedo=i.color.rgb*paint.rgb;
                float ivory=smoothstep(.60,.85,min(albedo.r,min(albedo.g,albedo.b)));
                shade=lerp(shade,.99,ivory*.75*(1-.55*i.skin));
                return float4(albedo*lerp(shade,1,_Flat),1);
            }
            ENDCG
        }
    }
}
