Shader "Hidden/DesktopFish/VisibilityID" {
 Properties {_MainTex("Texture",2D)="white"{}}
 SubShader {Pass {Cull Off ZWrite On ZTest LEqual Blend Off
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 alpha:TEXCOORD1;float4 color:COLOR;};
 struct val {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float alpha:TEXCOORD1;float4 color:COLOR;};
 val vert(app v){val o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.alpha=v.alpha.x;o.color=v.color;return o;}
 float4 frag(val i):SV_Target{clip(tex2D(_MainTex,i.uv).a*i.alpha-.5);return i.color;}
 ENDCG
 }}
}
