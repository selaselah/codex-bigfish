Shader "DesktopFish/MascotSurface"
{
    Properties
    {
        _MainTex ("Source design", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _Kind ("Hair / cloth / headband / tail", Float) = 0
        _Decoration ("Skirt / cuff embroidery", Float) = 0
        _Glint ("Grouped fringe highlights", Float) = 0
        _EdgeWidth ("Illustrated edge width", Float) = 0.0006
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off ZWrite On AlphaToMask On
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex edgeVert
            #pragma fragment edgeFrag
            #include "UnityCG.cginc"
            float _EdgeWidth;
            struct EdgeInput { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct EdgeOutput { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            EdgeOutput edgeVert(EdgeInput v)
            {
                EdgeOutput o;
                v.vertex.xyz += normalize(v.normal)*_EdgeWidth;
                o.vertex = UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o;
            }
            sampler2D _MainTex;
            float _Cutoff;
            fixed4 edgeFrag(EdgeOutput i) : SV_Target
            {
                clip(tex2D(_MainTex,i.uv).a-_Cutoff);
                float3 rgb=float3(0.19,0.22,0.34);
                #ifndef UNITY_COLORSPACE_GAMMA
                rgb=GammaToLinearSpace(rgb);
                #endif
                return float4(rgb,1);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color;
            float _Cutoff, _Kind, _Decoration, _Glint;
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; };
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.normal = UnityObjectToWorldNormal(input.normal);
                return output;
            }
            float3 DisplayColor(float3 rgb)
            {
                #ifndef UNITY_COLORSPACE_GAMMA
                return LinearToGammaSpace(rgb);
                #else
                return rgb;
                #endif
            }
            float Mark(float distance, float width)
            {
                return 1-smoothstep(width,width+max(fwidth(distance),0.00025),distance);
            }
            float OvalRing(float2 p, float2 center, float2 radius, float angle)
            {
                p-=center;
                float2 q=float2(cos(angle)*p.x-sin(angle)*p.y,sin(angle)*p.x+cos(angle)*p.y);
                return Mark(abs(length(q/radius)-1)*min(radius.x,radius.y),0.0015);
            }
            float Embroidery(float2 uv)
            {
                // Four large ribbon repeats around the skirt, with one clear
                // four-point star between them. No subpixel dots or double lines.
                float2 p=float2((frac(uv.x*4)-0.5)/4,uv.y-0.126);
                float bow=max(OvalRing(p,float2(-0.010,0.003),float2(0.012,0.006),0.45),
                              OvalRing(p,float2(0.010,0.003),float2(0.012,0.006),-0.45));
                float knot=Mark(length(p/float2(0.0035,0.003)),1);
                float tails=(1-smoothstep(0.011,0.014,abs(p.y+0.009))) *
                    (1-smoothstep(0.0025,0.004,abs(abs(p.x)-(0.004-p.y*0.45)))) * step(p.y,0);
                float hemRule=Mark(abs(uv.y-(0.095+0.003*cos(uv.x*8*UNITY_PI))),0.0016);
                float2 star=float2((frac(uv.x*4+0.5)-0.5)/4,uv.y-0.126);
                float sparkle=1-smoothstep(0.90,1.15,sqrt(abs(star.x)/0.007)+sqrt(abs(star.y)/0.009));
                return saturate(max(max(bow,knot),max(tails,max(hemRule,sparkle))));
            }
            float SoftOval(float2 uv,float2 center,float2 radius)
            {
                return 1-smoothstep(0.72,1.05,length((uv-center)/radius));
            }
            fixed4 frag(Output input) : SV_Target
            {
                float4 source = tex2D(_MainTex, input.uv);
                clip(source.a * _Color.a - _Cutoff);
                float3 rgb = DisplayColor(source.rgb);
                if (_Kind < 0.5)
                {
                    // Keep the illustrated gradients, but group nearby shine marks
                    // into a few wider shapes like the 2D reference.
                    float3 a = DisplayColor(tex2D(_MainTex,float2(0.18,input.uv.y)).rgb);
                    float3 b = DisplayColor(tex2D(_MainTex,float2(0.50,input.uv.y)).rgb);
                    float3 c = DisplayColor(tex2D(_MainTex,float2(0.82,input.uv.y)).rgb);
                    float3 broad = a + b + c - min(a,min(b,c)) - max(a,max(b,c));
                    rgb = lerp(broad, rgb, 0.70);
                    float shine=0;
                    if(_Glint>0.5)
                    {
                        float zone=smoothstep(0.56,0.60,input.uv.y)*(1-smoothstep(0.79,0.83,input.uv.y));
                        float3 plain=DisplayColor((tex2D(_MainTex,float2(0.05,input.uv.y)).rgb+
                                                   tex2D(_MainTex,float2(0.95,input.uv.y)).rgb)*0.5);
                        rgb=lerp(rgb,plain,zone);
                        if(_Glint<1.5)
                        {
                            shine=max(max(SoftOval(input.uv,float2(0.326,0.714),float2(0.024,0.052)),
                                          SoftOval(input.uv,float2(0.378,0.690),float2(0.027,0.033))),
                                      max(SoftOval(input.uv,float2(0.674,0.714),float2(0.024,0.052)),
                                          SoftOval(input.uv,float2(0.622,0.690),float2(0.027,0.033))));
                        }
                        else
                        {
                            shine=max(SoftOval(input.uv,float2(0.45,0.655),float2(0.070,0.028)),
                                      max(SoftOval(input.uv,float2(0.20,0.665),float2(0.027,0.043)),
                                          SoftOval(input.uv,float2(0.80,0.665),float2(0.027,0.043))));
                        }
                    }
                    rgb=lerp(rgb,float3(0.43,0.55,0.75),shine*0.65);
                    float cyan=smoothstep(0.40,0.58,rgb.g)*smoothstep(0.12,0.25,rgb.g-rgb.r);
                    rgb=lerp(rgb,float3(0.32,0.53,0.70),cyan*0.45);
                    rgb = rgb * 0.94 + float3(0.020,0.026,0.035);
                }
                else if (_Kind < 1.5)
                {
                    float white=smoothstep(0.50,0.80,min(rgb.r,min(rgb.g,rgb.b)));
                    rgb=lerp(rgb,float3(0.95,0.94,0.92),white*0.20);
                    if(_Decoration>0.5 && _Decoration<1.5 && input.uv.y<0.17)
                    {
                        rgb=DisplayColor(tex2D(_MainTex,float2(input.uv.x,0.175)).rgb);
                        rgb=lerp(rgb,float3(0.83,0.67,0.37),Embroidery(input.uv));
                    }
                    if(_Decoration>1.5)
                    {
                        // Replace two hairline cuff rules with one broad gold band
                        // and a larger stud; preserve the source sleeve shading.
                        float zone=smoothstep(0.53,0.55,input.uv.y)*(1-smoothstep(0.60,0.62,input.uv.y));
                        float3 plain=DisplayColor(tex2D(_MainTex,float2(input.uv.x,0.54)).rgb);
                        rgb=lerp(rgb,plain,zone);
                        float band=Mark(abs(input.uv.y-0.589),0.006);
                        float stud=Mark(length((input.uv-float2(input.uv.x<0.5?0.172:0.828,0.560))/float2(0.014,0.011)),1);
                        rgb=lerp(rgb,float3(0.83,0.67,0.37),max(band,stud)*zone);
                    }
                }
                else if(_Kind<2.5)
                {
                    float white = smoothstep(0.55,0.78,min(rgb.r,min(rgb.g,rgb.b)));
                    float pleat=smoothstep(0.45,1,cos(input.uv.y*12*UNITY_PI));
                    float3 ivory=float3(0.95,0.94,0.92)*(1-pleat*0.075);
                    rgb=lerp(rgb,ivory,white*0.55);
                }
                else
                {
                    float light=dot(rgb,float3(0.2126,0.7152,0.0722));
                    rgb=lerp(float3(0.25,0.39,0.60),float3(0.49,0.67,0.78),smoothstep(0.25,0.85,light));
                }
                // Broad, low-contrast form shading; avoid tiny normal-map glints.
                float form = smoothstep(-0.25,0.75,dot(normalize(input.normal),normalize(float3(-0.25,0.6,0.75))));
                rgb *= lerp(0.93,1.0,form);
                #ifndef UNITY_COLORSPACE_GAMMA
                rgb = GammaToLinearSpace(rgb);
                #endif
                return float4(rgb * _Color.rgb, source.a * _Color.a);
            }
            ENDCG
        }
    }
}
