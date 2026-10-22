using UnityEngine;
namespace DesktopFish.Editor {
    // 135 independently sampled surface positions, 47 paint-crease locations,
    // and 15 oval-button anchors. No source topology, UV or texels are stored.
    public static class FishBibSurfaceGuides {
        static readonly Vector3[] Rows={
            new Vector3(-0.0572942f,0.4160000f,0.1083938f),
            new Vector3(-0.0489843f,0.4160000f,0.1108126f),
            new Vector3(-0.0406744f,0.4160000f,0.1138423f),
            new Vector3(-0.0323646f,0.4160000f,0.1160690f),
            new Vector3(-0.0240547f,0.4160000f,0.1168270f),
            new Vector3(-0.0157448f,0.4160000f,0.1170882f),
            new Vector3(-0.0074350f,0.4160000f,0.1171167f),
            new Vector3(0.0008749f,0.4160000f,0.1171400f),
            new Vector3(0.0091848f,0.4160000f,0.1171109f),
            new Vector3(0.0174947f,0.4160000f,0.1170824f),
            new Vector3(0.0258045f,0.4160000f,0.1166719f),
            new Vector3(0.0341144f,0.4160000f,0.1158967f),
            new Vector3(0.0424243f,0.4160000f,0.1132621f),
            new Vector3(0.0507342f,0.4160000f,0.1101680f),
            new Vector3(0.0590440f,0.4160000f,0.1078908f),
            new Vector3(-0.0627477f,0.4280000f,0.1050585f),
            new Vector3(-0.0536079f,0.4280000f,0.1095087f),
            new Vector3(-0.0444681f,0.4280000f,0.1134067f),
            new Vector3(-0.0353283f,0.4280000f,0.1159039f),
            new Vector3(-0.0261885f,0.4280000f,0.1167479f),
            new Vector3(-0.0170487f,0.4280000f,0.1175920f),
            new Vector3(-0.0079089f,0.4280000f,0.1184360f),
            new Vector3(0.0012309f,0.4280000f,0.1186088f),
            new Vector3(0.0103707f,0.4280000f,0.1182120f),
            new Vector3(0.0195105f,0.4280000f,0.1173680f),
            new Vector3(0.0286503f,0.4280000f,0.1165240f),
            new Vector3(0.0377901f,0.4280000f,0.1156346f),
            new Vector3(0.0469299f,0.4280000f,0.1128223f),
            new Vector3(0.0560698f,0.4280000f,0.1088281f),
            new Vector3(0.0652096f,0.4280000f,0.1041154f),
            new Vector3(-0.0667051f,0.4400000f,0.0987905f),
            new Vector3(-0.0568924f,0.4400000f,0.1035245f),
            new Vector3(-0.0470796f,0.4400000f,0.1081736f),
            new Vector3(-0.0372669f,0.4400000f,0.1124607f),
            new Vector3(-0.0274541f,0.4400000f,0.1133811f),
            new Vector3(-0.0176414f,0.4400000f,0.1144315f),
            new Vector3(-0.0078286f,0.4400000f,0.1154820f),
            new Vector3(0.0019841f,0.4400000f,0.1160793f),
            new Vector3(0.0117969f,0.4400000f,0.1150424f),
            new Vector3(0.0216096f,0.4400000f,0.1140055f),
            new Vector3(0.0314224f,0.4400000f,0.1129993f),
            new Vector3(0.0412351f,0.4400000f,0.1112312f),
            new Vector3(0.0510479f,0.4400000f,0.1070871f),
            new Vector3(0.0608606f,0.4400000f,0.1029430f),
            new Vector3(0.0706734f,0.4400000f,0.0986781f),
            new Vector3(-0.0697064f,0.4520000f,0.0900172f),
            new Vector3(-0.0595769f,0.4520000f,0.0953848f),
            new Vector3(-0.0494474f,0.4520000f,0.0994178f),
            new Vector3(-0.0393180f,0.4520000f,0.1008533f),
            new Vector3(-0.0291885f,0.4520000f,0.1020834f),
            new Vector3(-0.0190590f,0.4520000f,0.1033537f),
            new Vector3(-0.0089296f,0.4520000f,0.1045234f),
            new Vector3(0.0011999f,0.4520000f,0.1051813f),
            new Vector3(0.0113294f,0.4520000f,0.1042781f),
            new Vector3(0.0214589f,0.4520000f,0.1030410f),
            new Vector3(0.0315883f,0.4520000f,0.1017810f),
            new Vector3(0.0417178f,0.4520000f,0.1005706f),
            new Vector3(0.0518473f,0.4520000f,0.0983189f),
            new Vector3(0.0619767f,0.4520000f,0.0936368f),
            new Vector3(0.0721062f,0.4520000f,0.0883570f),
            new Vector3(-0.0702520f,0.4640000f,0.0791415f),
            new Vector3(-0.0601368f,0.4640000f,0.0839449f),
            new Vector3(-0.0500216f,0.4640000f,0.0854978f),
            new Vector3(-0.0399064f,0.4640000f,0.0873094f),
            new Vector3(-0.0297911f,0.4640000f,0.0890925f),
            new Vector3(-0.0196759f,0.4640000f,0.0900967f),
            new Vector3(-0.0095607f,0.4640000f,0.0909714f),
            new Vector3(0.0005545f,0.4640000f,0.0917489f),
            new Vector3(0.0106697f,0.4640000f,0.0908742f),
            new Vector3(0.0207849f,0.4640000f,0.0899995f),
            new Vector3(0.0309001f,0.4640000f,0.0889233f),
            new Vector3(0.0410154f,0.4640000f,0.0871118f),
            new Vector3(0.0511306f,0.4640000f,0.0853003f),
            new Vector3(0.0612458f,0.4640000f,0.0837872f),
            new Vector3(0.0713610f,0.4640000f,0.0788958f),
            new Vector3(-0.0705724f,0.4760000f,0.0666700f),
            new Vector3(-0.0604245f,0.4760000f,0.0681316f),
            new Vector3(-0.0502766f,0.4760000f,0.0695932f),
            new Vector3(-0.0401286f,0.4760000f,0.0712453f),
            new Vector3(-0.0299807f,0.4760000f,0.0724412f),
            new Vector3(-0.0198328f,0.4760000f,0.0729127f),
            new Vector3(-0.0096849f,0.4760000f,0.0736141f),
            new Vector3(0.0004630f,0.4760000f,0.0746352f),
            new Vector3(0.0106110f,0.4760000f,0.0742086f),
            new Vector3(0.0207589f,0.4760000f,0.0735432f),
            new Vector3(0.0309068f,0.4760000f,0.0725271f),
            new Vector3(0.0410547f,0.4760000f,0.0711046f),
            new Vector3(0.0512027f,0.4760000f,0.0694603f),
            new Vector3(0.0613506f,0.4760000f,0.0679988f),
            new Vector3(0.0714985f,0.4760000f,0.0665373f),
            new Vector3(-0.0719743f,0.4880000f,0.0516437f),
            new Vector3(-0.0616004f,0.4880000f,0.0533563f),
            new Vector3(-0.0512264f,0.4880000f,0.0541715f),
            new Vector3(-0.0408525f,0.4880000f,0.0547713f),
            new Vector3(-0.0304785f,0.4880000f,0.0553405f),
            new Vector3(-0.0201046f,0.4880000f,0.0558236f),
            new Vector3(-0.0097306f,0.4880000f,0.0562980f),
            new Vector3(0.0006433f,0.4880000f,0.0567660f),
            new Vector3(0.0110173f,0.4880000f,0.0563205f),
            new Vector3(0.0213912f,0.4880000f,0.0558876f),
            new Vector3(0.0317652f,0.4880000f,0.0554624f),
            new Vector3(0.0421391f,0.4880000f,0.0545644f),
            new Vector3(0.0525131f,0.4880000f,0.0541398f),
            new Vector3(0.0628870f,0.4880000f,0.0531460f),
            new Vector3(0.0732610f,0.4880000f,0.0514334f),
            new Vector3(-0.0711019f,0.5000000f,0.0363285f),
            new Vector3(-0.0607926f,0.5000000f,0.0373482f),
            new Vector3(-0.0504833f,0.5000000f,0.0398404f),
            new Vector3(-0.0401741f,0.5000000f,0.0418527f),
            new Vector3(-0.0298648f,0.5000000f,0.0427135f),
            new Vector3(-0.0195555f,0.5000000f,0.0433584f),
            new Vector3(-0.0092462f,0.5000000f,0.0439803f),
            new Vector3(0.0010630f,0.5000000f,0.0446321f),
            new Vector3(0.0113723f,0.5000000f,0.0438471f),
            new Vector3(0.0216816f,0.5000000f,0.0432363f),
            new Vector3(0.0319909f,0.5000000f,0.0418886f),
            new Vector3(0.0423001f,0.5000000f,0.0396627f),
            new Vector3(0.0526094f,0.5000000f,0.0392350f),
            new Vector3(0.0629187f,0.5000000f,0.0368141f),
            new Vector3(0.0732280f,0.5000000f,0.0354221f),
            new Vector3(-0.0665794f,0.5120000f,0.0162640f),
            new Vector3(-0.0571430f,0.5120000f,0.0176317f),
            new Vector3(-0.0477066f,0.5120000f,0.0199314f),
            new Vector3(-0.0382701f,0.5120000f,0.0238911f),
            new Vector3(-0.0288337f,0.5120000f,0.0287601f),
            new Vector3(-0.0193973f,0.5120000f,0.0336291f),
            new Vector3(-0.0099609f,0.5120000f,0.0145111f),
            new Vector3(-0.0005244f,0.5120000f,0.0173295f),
            new Vector3(0.0089120f,0.5120000f,0.0151618f),
            new Vector3(0.0183484f,0.5120000f,0.0338256f),
            new Vector3(0.0277848f,0.5120000f,0.0294386f),
            new Vector3(0.0372212f,0.5120000f,0.0255929f),
            new Vector3(0.0466577f,0.5120000f,0.0224868f),
            new Vector3(0.0560941f,0.5120000f,0.0183197f),
            new Vector3(0.0655305f,0.5120000f,0.0145666f),
        };
        static readonly Vector3[][] Creases={
            new[]{new Vector3(-0.0471047f,0.4160000f,0.1115002f),new Vector3(-0.0478985f,0.4280000f,0.1125200f),new Vector3(-0.0489370f,0.4400000f,0.1072489f),new Vector3(-0.0499388f,0.4520000f,0.0992304f),new Vector3(-0.0493094f,0.4640000f,0.0856186f),new Vector3(-0.0473664f,0.4760000f,0.0700663f),new Vector3(-0.0459192f,0.5000000f,0.0409697f),new Vector3(-0.0429777f,0.5120000f,0.0214152f)},
            new[]{new Vector3(-0.0242454f,0.4160000f,0.1168109f),new Vector3(-0.0266559f,0.4280000f,0.1167020f),new Vector3(-0.0253131f,0.4519999f,0.1025636f),new Vector3(-0.0253994f,0.4640000f,0.0895893f),new Vector3(-0.0233104f,0.4760001f,0.0727484f),new Vector3(-0.0285387f,0.4999999f,0.0427878f)},
            new[]{new Vector3(-0.0079739f,0.4160000f,0.1171149f),new Vector3(-0.0034934f,0.4280000f,0.1186005f),new Vector3(-0.0095523f,0.4400000f,0.1152904f),new Vector3(-0.0020921f,0.4520000f,0.1051214f),new Vector3(-0.0095285f,0.4640000f,0.0909694f),new Vector3(-0.0093560f,0.4760000f,0.0736509f),new Vector3(-0.0076235f,0.4880000f,0.0563957f),new Vector3(-0.0078044f,0.5000000f,0.0440781f),new Vector3(-0.0071085f,0.5120000f,0.0154321f)},
            new[]{new Vector3(0.0106184f,0.4160000f,0.1171058f),new Vector3(0.0058685f,0.4280000f,0.1185923f),new Vector3(0.0124648f,0.4400000f,0.1149650f),new Vector3(0.0044648f,0.4520000f,0.1048955f),new Vector3(0.0126282f,0.4640000f,0.0906988f),new Vector3(0.0105406f,0.4760000f,0.0742128f),new Vector3(0.0106779f,0.4880000f,0.0563326f),new Vector3(0.0103534f,0.5000001f,0.0439093f),new Vector3(0.0081281f,0.5120001f,0.0153383f)},
            new[]{new Vector3(0.0267177f,0.4280000f,0.1166962f),new Vector3(0.0282806f,0.4400000f,0.1132949f),new Vector3(0.0284165f,0.4520000f,0.1021704f),new Vector3(0.0285398f,0.4640000f,0.0892380f),new Vector3(0.0221473f,0.4760000f,0.0733995f),new Vector3(0.0309035f,0.5000000f,0.0420866f),new Vector3(0.0191953f,0.5120000f,0.0336917f)},
            new[]{new Vector3(0.0467526f,0.4160000f,0.1116291f),new Vector3(0.0506248f,0.4280000f,0.1113378f),new Vector3(0.0523688f,0.4400000f,0.1065298f),new Vector3(0.0529724f,0.4520000f,0.0978113f),new Vector3(0.0522560f,0.4640000f,0.0850917f),new Vector3(0.0501332f,0.4760000f,0.0696020f),new Vector3(0.0473951f,0.4880000f,0.0542618f),new Vector3(0.0516218f,0.4999999f,0.0392713f)},
        };
        public static readonly Vector3[][] Buttons={
            new[]{new Vector3(0.0014564f,0.4293755f,0.1190874f),new Vector3(-0.0030740f,0.4293755f,0.1190874f),new Vector3(0.0059868f,0.4293755f,0.1190874f),new Vector3(0.0014564f,0.4268754f,0.1190874f),new Vector3(0.0014564f,0.4318756f,0.1190874f)},
            new[]{new Vector3(0.0015765f,0.4485951f,0.1119122f),new Vector3(-0.0024816f,0.4485951f,0.1119122f),new Vector3(0.0056346f,0.4485951f,0.1119122f),new Vector3(0.0015765f,0.4459388f,0.1119122f),new Vector3(0.0015765f,0.4512515f,0.1119122f)},
            new[]{new Vector3(0.0010353f,0.4676586f,0.0897859f),new Vector3(-0.0036773f,0.4676586f,0.0897859f),new Vector3(0.0057479f,0.4676586f,0.0897859f),new Vector3(0.0010353f,0.4656272f,0.0897859f),new Vector3(0.0010353f,0.4696899f,0.0897859f)},
        };
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;float wa=2*hb+ha,wb=hb+2*ha;return (wa+wb)/(wa/a+wb/b);
        }
        static float Cubic(float a,float b,float ma,float mb,float h,float t) {
            float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        static Vector3 Column(float y,int col) {
            y=Mathf.Clamp(y,.416f,.512f);int row=Mathf.Min(7,Mathf.FloorToInt((y-.416f)/.012f));
            Vector3 a=Rows[row*15+col],b=Rows[(row+1)*15+col],p=a;
            float h=b.y-a.y,t=Mathf.Clamp01((y-a.y)/h);
            for(int k=0;k<3;k++) {
                if(k==1){p.y=y;continue;}
                float d=(b[k]-a[k])/h,ma=d,mb=d;
                if(row>0){var before=Rows[(row-1)*15+col];float hp=a.y-before.y;ma=Slope((a[k]-before[k])/hp,d,hp,h);}
                if(row<7){var after=Rows[(row+2)*15+col];float hn=after.y-b.y;mb=Slope(d,(after[k]-b[k])/hn,h,hn);}
                p[k]=Cubic(a[k],b[k],ma,mb,h,t);
            }
            return p;
        }
        public static Vector3 Point(float y,float across) {
            float f=(Mathf.Clamp(across,-1,1)+1)*7;int col=Mathf.Min(13,(int)f);float t=f-col;
            Vector3 a=Column(y,col),b=Column(y,col+1),p=a;
            for(int k=0;k<3;k++) {
                if(k==1){p.y=y;continue;}
                float d=b[k]-a[k],ma=d,mb=d;
                if(col>0)ma=Slope(a[k]-Column(y,col-1)[k],d,1,1);
                if(col<13)mb=Slope(d,Column(y,col+2)[k]-b[k],1,1);
                p[k]=Cubic(a[k],b[k],ma,mb,1,t);
            }
            return p;
        }
        public static float Across(float x,float y) {
            return Mathf.InverseLerp(Point(y,-1).x,Point(y,1).x,x)*2-1;
        }
        public static float CreaseU(int index,float y) {
            var list=Creases[index];float sum=0,weight=0;
            float expected=new[]{-.78f,-.39f,-.14f,.14f,.39f,.78f}[index];
            foreach(var p in list) {
                float u=Across(p.x,p.y);if(Mathf.Abs(u-expected)>.13f)continue;
                bool buttonEdge=false;
                if(index==2||index==3)foreach(var oval in Buttons)
                    if(Mathf.Abs(oval[0].y-p.y)<.006f)buttonEdge=true;
                if(buttonEdge)continue;
                float d=(p.y-y)/.016f,w=Mathf.Exp(-.5f*d*d);
                sum+=u*w;weight+=w;
            }
            return weight>1e-8f?sum/weight:expected;
        }
    }
}
