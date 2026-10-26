Shader "DesktopFish/WorkProps"
{
    Properties {
        _Unlit ("Unlit", Float) = 0
        [HideInInspector] _SrcBlend ("Source blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth write", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            Cull Off
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Unlit;
            struct app { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float4 color:COLOR; };
            v2f vert(app v) {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb=GammaToLinearSpace(o.color.rgb);
                #endif
                return o;
            }
            fixed4 frag(v2f i):SV_Target {
                float shade=lerp(.70,1.02,smoothstep(-.5,.9,dot(normalize(i.normal),normalize(float3(-.35,.65,.8)))));
                return float4(i.color.rgb*lerp(shade,1,_Unlit),i.color.a);
            }
            ENDCG
        }
    }
}
