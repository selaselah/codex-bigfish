using UnityEngine;
namespace DesktopFish.Editor {
    // Sparse alpha-visible main-cloth and folded-return positions only.
    // Source topology, UVs, normals, texels and skin weights are not stored.
    public static class FishRibbonGuides {
        static readonly Vector3[][] Pieces={
            new[]{ // -1 main
                new Vector3(-0.0037500f,0.5094661f,0.0558193f),
                new Vector3(-0.0037500f,0.5122964f,0.0538808f),
                new Vector3(-0.0037500f,0.5151267f,0.0503018f),
                new Vector3(-0.0037500f,0.5179571f,0.0472492f),
                new Vector3(-0.0037500f,0.5207874f,0.0416835f),
                new Vector3(-0.0100000f,0.5062411f,0.0574211f),
                new Vector3(-0.0100000f,0.5103286f,0.0582845f),
                new Vector3(-0.0100000f,0.5144161f,0.0527116f),
                new Vector3(-0.0100000f,0.5185036f,0.0496689f),
                new Vector3(-0.0100000f,0.5225911f,0.0414034f),
                new Vector3(-0.0187500f,0.5018351f,0.0595975f),
                new Vector3(-0.0187500f,0.5076554f,0.0631652f),
                new Vector3(-0.0187500f,0.5134757f,0.0564071f),
                new Vector3(-0.0187500f,0.5192960f,0.0515954f),
                new Vector3(-0.0187500f,0.5251163f,0.0410089f),
                new Vector3(-0.0312500f,0.4974064f,0.0615710f),
                new Vector3(-0.0312500f,0.5050819f,0.0679594f),
                new Vector3(-0.0312500f,0.5127575f,0.0607653f),
                new Vector3(-0.0312500f,0.5204330f,0.0542420f),
                new Vector3(-0.0312500f,0.5281085f,0.0407834f),
                new Vector3(-0.0437500f,0.4943201f,0.0645145f),
                new Vector3(-0.0437500f,0.5032789f,0.0716729f),
                new Vector3(-0.0437500f,0.5122376f,0.0624751f),
                new Vector3(-0.0437500f,0.5211964f,0.0558347f),
                new Vector3(-0.0437500f,0.5301552f,0.0410790f),
                new Vector3(-0.0540989f,0.4932906f,0.0680536f),
                new Vector3(-0.0540989f,0.5024742f,0.0718624f),
                new Vector3(-0.0540989f,0.5116578f,0.0624662f),
                new Vector3(-0.0540989f,0.5208413f,0.0548835f),
                new Vector3(-0.0540989f,0.5300249f,0.0410099f),
                new Vector3(-0.0574816f,0.4963350f,0.0725954f),
                new Vector3(-0.0574816f,0.5047346f,0.0676351f),
                new Vector3(-0.0574816f,0.5131343f,0.0586369f),
                new Vector3(-0.0574816f,0.5215340f,0.0534174f),
                new Vector3(-0.0574816f,0.5299337f,0.0410064f),
                new Vector3(-0.0625000f,0.5144462f,0.0513443f),
                new Vector3(-0.0625000f,0.5181802f,0.0503064f),
                new Vector3(-0.0625000f,0.5219141f,0.0501018f),
                new Vector3(-0.0625000f,0.5256481f,0.0467389f),
                new Vector3(-0.0625000f,0.5293821f,0.0406339f),
                new Vector3(-0.0661641f,0.5270633f,0.0386260f),
                new Vector3(-0.0661641f,0.5270663f,0.0386260f),
                new Vector3(-0.0661641f,0.5270692f,0.0386260f),
                new Vector3(-0.0661641f,0.5270722f,0.0386260f),
                new Vector3(-0.0661641f,0.5270752f,0.0386260f),
            },
            new[]{ // -1 upper
                new Vector3(-0.0037500f,0.5201607f,0.0407173f),
                new Vector3(-0.0100000f,0.5209199f,0.0390547f),
                new Vector3(-0.0187500f,0.5219828f,0.0367271f),
                new Vector3(-0.0312500f,0.5232643f,0.0342026f),
                new Vector3(-0.0437500f,0.5239509f,0.0336895f),
                new Vector3(-0.0540989f,0.5246486f,0.0337608f),
                new Vector3(-0.0574816f,0.5248990f,0.0338699f),
                new Vector3(-0.0625000f,0.5258679f,0.0357721f),
                new Vector3(-0.0661641f,0.5270740f,0.0386133f),
            },
            new[]{ // -1 lower
                new Vector3(-0.0037500f,0.5096909f,0.0567585f),
                new Vector3(-0.0100000f,0.5070550f,0.0608201f),
                new Vector3(-0.0187500f,0.5033647f,0.0665063f),
                new Vector3(-0.0312500f,0.4999090f,0.0732869f),
                new Vector3(-0.0437500f,0.4981570f,0.0762322f),
                new Vector3(-0.0540989f,0.4970843f,0.0755263f),
                new Vector3(-0.0574816f,0.4963363f,0.0725954f),
            },
            new[]{ // 1 main
                new Vector3(0.0037500f,0.5094660f,0.0558193f),
                new Vector3(0.0037500f,0.5122964f,0.0538808f),
                new Vector3(0.0037500f,0.5151267f,0.0503018f),
                new Vector3(0.0037500f,0.5179571f,0.0472492f),
                new Vector3(0.0037500f,0.5207875f,0.0416835f),
                new Vector3(0.0100000f,0.5062411f,0.0574211f),
                new Vector3(0.0100000f,0.5103286f,0.0582845f),
                new Vector3(0.0100000f,0.5144161f,0.0527116f),
                new Vector3(0.0100000f,0.5185036f,0.0496689f),
                new Vector3(0.0100000f,0.5225911f,0.0414034f),
                new Vector3(0.0187500f,0.5018350f,0.0595975f),
                new Vector3(0.0187500f,0.5076553f,0.0631652f),
                new Vector3(0.0187500f,0.5134757f,0.0564071f),
                new Vector3(0.0187500f,0.5192960f,0.0515954f),
                new Vector3(0.0187500f,0.5251163f,0.0410090f),
                new Vector3(0.0312500f,0.4974064f,0.0615711f),
                new Vector3(0.0312500f,0.5050819f,0.0679594f),
                new Vector3(0.0312500f,0.5127575f,0.0607653f),
                new Vector3(0.0312500f,0.5204330f,0.0542420f),
                new Vector3(0.0312500f,0.5281085f,0.0407834f),
                new Vector3(0.0437500f,0.4943201f,0.0645145f),
                new Vector3(0.0437500f,0.5032789f,0.0714925f),
                new Vector3(0.0437500f,0.5122376f,0.0624752f),
                new Vector3(0.0437500f,0.5211964f,0.0558347f),
                new Vector3(0.0437500f,0.5301552f,0.0410790f),
                new Vector3(0.0540989f,0.4932906f,0.0680536f),
                new Vector3(0.0540989f,0.5024742f,0.0718411f),
                new Vector3(0.0540989f,0.5116578f,0.0624662f),
                new Vector3(0.0540989f,0.5208413f,0.0548836f),
                new Vector3(0.0540989f,0.5300249f,0.0410099f),
                new Vector3(0.0574816f,0.4963350f,0.0725954f),
                new Vector3(0.0574816f,0.5047346f,0.0676352f),
                new Vector3(0.0574816f,0.5131343f,0.0586369f),
                new Vector3(0.0574816f,0.5215340f,0.0534174f),
                new Vector3(0.0574816f,0.5299337f,0.0410065f),
                new Vector3(0.0625000f,0.5144463f,0.0513443f),
                new Vector3(0.0625000f,0.5181802f,0.0503064f),
                new Vector3(0.0625000f,0.5219142f,0.0501018f),
                new Vector3(0.0625000f,0.5256482f,0.0467389f),
                new Vector3(0.0625000f,0.5293821f,0.0406339f),
                new Vector3(0.0661641f,0.5270633f,0.0386261f),
                new Vector3(0.0661641f,0.5270663f,0.0386261f),
                new Vector3(0.0661641f,0.5270692f,0.0386261f),
                new Vector3(0.0661641f,0.5270722f,0.0386261f),
                new Vector3(0.0661641f,0.5270752f,0.0386261f),
            },
            new[]{ // 1 upper
                new Vector3(0.0037500f,0.5201607f,0.0407173f),
                new Vector3(0.0100000f,0.5209199f,0.0390547f),
                new Vector3(0.0187500f,0.5219828f,0.0367271f),
                new Vector3(0.0312500f,0.5232643f,0.0342026f),
                new Vector3(0.0437500f,0.5239509f,0.0336896f),
                new Vector3(0.0540989f,0.5246486f,0.0337608f),
                new Vector3(0.0574816f,0.5248990f,0.0338700f),
                new Vector3(0.0625000f,0.5258680f,0.0357721f),
                new Vector3(0.0661641f,0.5270740f,0.0386134f),
            },
            new[]{ // 1 lower
                new Vector3(0.0037500f,0.5096909f,0.0567585f),
                new Vector3(0.0100000f,0.5070550f,0.0608201f),
                new Vector3(0.0187500f,0.5033647f,0.0665063f),
                new Vector3(0.0312500f,0.4999090f,0.0732870f),
                new Vector3(0.0437500f,0.4981570f,0.0762323f),
                new Vector3(0.0540989f,0.4970843f,0.0755264f),
                new Vector3(0.0574816f,0.4963363f,0.0725954f),
            },
        };
        static Vector3[] Rows(int sign,int piece)=>Pieces[(sign<0?0:3)+piece];
        public static int Count(int sign,int piece)=>Rows(sign,piece).Length/(piece==0?5:1);
        public static float Station(int sign,int piece,int row)=>Mathf.Abs(Rows(sign,piece)[row*(piece==0?5:1)].x);
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;float wa=2*hb+ha,wb=hb+2*ha;
            return (wa+wb)/(wa/a+wb/b);
        }
        static Vector3 Interpolate(Vector3 p0,Vector3 a,Vector3 b,Vector3 p3,float x0,float x1,float x2,float x3,float x) {
            float h=x2-x1,t=Mathf.Clamp01((x-x1)/h),t2=t*t,t3=t2*t;Vector3 result=Vector3.zero;
            for(int k=0;k<3;k++) {
                float d=(b[k]-a[k])/h;
                float m0=x0==x1?d:Slope((a[k]-p0[k])/(x1-x0),d,x1-x0,h);
                float m1=x2==x3?d:Slope(d,(p3[k]-b[k])/(x3-x2),h,x3-x2);
                result[k]=(2*t3-3*t2+1)*a[k]+(t3-2*t2+t)*h*m0+(-2*t3+3*t2)*b[k]+(t3-t2)*h*m1;
            }
            return result;
        }
        static Vector3 Along(int sign,int piece,float x,int col) {
            var rows=Rows(sign,piece);int count=Count(sign,piece),width=piece==0?5:1;
            if(x<=Station(sign,piece,0))return rows[col];if(x>=Station(sign,piece,count-1))return rows[(count-1)*width+col];
            int i=0;while(x>Station(sign,piece,i+1))i++;
            int before=Mathf.Max(0,i-1),after=Mathf.Min(count-1,i+2);
            return Interpolate(rows[before*width+col],rows[i*width+col],rows[(i+1)*width+col],rows[after*width+col],
                Station(sign,piece,before),Station(sign,piece,i),Station(sign,piece,i+1),Station(sign,piece,after),x);
        }
        public static Vector3 Main(int sign,float x,float across) {
            float f=Mathf.Clamp01(across)*4;int i=Mathf.Min(3,(int)f),before=Mathf.Max(0,i-1),after=Mathf.Min(4,i+2);
            return Interpolate(Along(sign,0,x,before),Along(sign,0,x,i),Along(sign,0,x,i+1),Along(sign,0,x,after),before*.25f,i*.25f,(i+1)*.25f,after*.25f,across);
        }
        public static Vector3 Point(int sign,int piece,float x,float across) {
            if(piece==0)return Main(sign,x,across);
            // Both returns share the independently reconstructed main edge;
            // no second full rear shell fills the folded cloth volume.
            return Vector3.Lerp(Main(sign,x,piece==1?1:0),Along(sign,piece,x,0),across);
        }
        public static Vector3 Fold(int sign,float t,float across)=>Main(sign,Mathf.Lerp(Station(sign,0,0),Station(sign,0,Count(sign,0)-1),t),across);
    }
}
