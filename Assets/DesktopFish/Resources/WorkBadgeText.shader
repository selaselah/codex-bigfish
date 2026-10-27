Shader "DesktopFish/WorkBadgeText"
{
    Properties { _MainTex ("Font atlas", 2D) = "white" {} }
    SubShader {
        Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }
        Pass {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct app { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(app v) {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb=GammaToLinearSpace(o.color.rgb);
                #endif
                return o;
            }
            fixed4 frag(v2f i):SV_Target { return float4(i.color.rgb,i.color.a*tex2D(_MainTex,i.uv).a); }
            ENDCG
        }
    }
}
