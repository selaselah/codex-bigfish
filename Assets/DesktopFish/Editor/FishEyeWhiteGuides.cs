using UnityEngine;
namespace DesktopFish.Editor {
    // Sparse alpha/depth-visible opening controls, not isolated eyeball bounds.
    // New topology follows our skin; no source UVs, texels or weights are used.
    public static class FishEyeWhiteGuides {
        static readonly Vector3[][] Rows={
            new[]{
                new Vector3(-0.1402120f,0.6705000f,0.0939760f),
                new Vector3(-0.1402120f,0.6706870f,0.0939957f),
                new Vector3(-0.1393594f,0.6635833f,0.0933226f),
                new Vector3(-0.1393594f,0.6727142f,0.0942708f),
                new Vector3(-0.1368347f,0.6541172f,0.0933814f),
                new Vector3(-0.1368347f,0.6787180f,0.0961193f),
                new Vector3(-0.1327347f,0.6452092f,0.0943700f),
                new Vector3(-0.1327347f,0.6825915f,0.0978555f),
                new Vector3(-0.1272170f,0.6384075f,0.0961609f),
                new Vector3(-0.1272170f,0.6870742f,0.1129436f),
                new Vector3(-0.1204937f,0.6336809f,0.0990859f),
                new Vector3(-0.1204937f,0.6918521f,0.1033392f),
                new Vector3(-0.1128232f,0.6309825f,0.1023852f),
                new Vector3(-0.1128232f,0.6956939f,0.1213998f),
                new Vector3(-0.1045001f,0.6299381f,0.1061169f),
                new Vector3(-0.1045001f,0.6981282f,0.1104496f),
                new Vector3(-0.0958444f,0.6303514f,0.1098173f),
                new Vector3(-0.0958444f,0.6993070f,0.1296382f),
                new Vector3(-0.0871888f,0.6319526f,0.1129141f),
                new Vector3(-0.0871888f,0.6989344f,0.1330536f),
                new Vector3(-0.0788657f,0.6334199f,0.1160346f),
                new Vector3(-0.0788657f,0.6978210f,0.1196739f),
                new Vector3(-0.0711952f,0.6344641f,0.1306073f),
                new Vector3(-0.0711952f,0.6961888f,0.1222910f),
                new Vector3(-0.0644719f,0.6364559f,0.1317829f),
                new Vector3(-0.0644719f,0.6941328f,0.1388825f),
                new Vector3(-0.0589542f,0.6389410f,0.1245446f),
                new Vector3(-0.0589542f,0.6916631f,0.1396421f),
                new Vector3(-0.0548542f,0.6466123f,0.1360688f),
                new Vector3(-0.0548542f,0.6880650f,0.1293577f),
                new Vector3(-0.0523294f,0.6567306f,0.1371857f),
                new Vector3(-0.0523294f,0.6779005f,0.1390107f),
                new Vector3(-0.0514769f,0.6727500f,0.1385447f),
                new Vector3(-0.0514769f,0.6728041f,0.1385492f),
            },
            new[]{
                new Vector3(0.0514769f,0.6727500f,0.1385447f),
                new Vector3(0.0514769f,0.6728041f,0.1385493f),
                new Vector3(0.0523294f,0.6567305f,0.1371858f),
                new Vector3(0.0523294f,0.6779007f,0.1390107f),
                new Vector3(0.0548542f,0.6466122f,0.1360688f),
                new Vector3(0.0548542f,0.6880655f,0.1293579f),
                new Vector3(0.0589542f,0.6389410f,0.1334712f),
                new Vector3(0.0589542f,0.6916636f,0.1277432f),
                new Vector3(0.0644719f,0.6364559f,0.1220452f),
                new Vector3(0.0644719f,0.6941331f,0.1249349f),
                new Vector3(0.0711952f,0.6344640f,0.1191195f),
                new Vector3(0.0711952f,0.6961890f,0.1222911f),
                new Vector3(0.0788657f,0.6334198f,0.1290599f),
                new Vector3(0.0788657f,0.6978211f,0.1196739f),
                new Vector3(0.0871888f,0.6319524f,0.1129142f),
                new Vector3(0.0871888f,0.6989345f,0.1169256f),
                new Vector3(0.0958444f,0.6303515f,0.1098174f),
                new Vector3(0.0958444f,0.6993076f,0.1138061f),
                new Vector3(0.1045001f,0.6299384f,0.1061169f),
                new Vector3(0.1045001f,0.6981289f,0.1104499f),
                new Vector3(0.1128232f,0.6309825f,0.1023853f),
                new Vector3(0.1128232f,0.6956944f,0.1213999f),
                new Vector3(0.1204937f,0.6336808f,0.0990860f),
                new Vector3(0.1204937f,0.6918523f,0.1033393f),
                new Vector3(0.1272170f,0.6384073f,0.0961610f),
                new Vector3(0.1272170f,0.6870742f,0.1002876f),
                new Vector3(0.1327347f,0.6452092f,0.0943701f),
                new Vector3(0.1327347f,0.6825915f,0.0978556f),
                new Vector3(0.1368347f,0.6541173f,0.0933814f),
                new Vector3(0.1368347f,0.6787179f,0.0961193f),
                new Vector3(0.1393595f,0.6635835f,0.0933227f),
                new Vector3(0.1393595f,0.6727142f,0.0942709f),
                new Vector3(0.1402120f,0.6705000f,0.0939761f),
                new Vector3(0.1402120f,0.6706869f,0.0939958f),
            },
        };
        public static int Columns(int sign)=>Rows[sign<0?0:1].Length/2;
        public static Vector3 Point(int sign,int col,bool upper)=>Rows[sign<0?0:1][col*2+(upper?1:0)];
        static float Slope(Vector3[] q,int i,int side) {
            int count=q.Length/2;
            if(i==0)return (q[2+side].y-q[side].y)/(q[2+side].x-q[side].x);
            if(i==count-1)return (q[i*2+side].y-q[(i-1)*2+side].y)/(q[i*2+side].x-q[(i-1)*2+side].x);
            Vector3 a=q[(i-1)*2+side],b=q[i*2+side],c=q[(i+1)*2+side];
            float h=b.x-a.x,k=c.x-b.x,l=(b.y-a.y)/h,r=(c.y-b.y)/k;
            if(l*r<=0)return 0;return 3*(h+k)/((2*k+h)/l+(k+2*h)/r);
        }
        public static Vector2 Bounds(int sign,float x) {
            var q=Rows[sign<0?0:1];int last=q.Length/2-1,i=0;
            x=Mathf.Clamp(x,q[0].x,q[last*2].x);while(i<last-1&&x>q[(i+1)*2].x)i++;
            float h=q[(i+1)*2].x-q[i*2].x,t=(x-q[i*2].x)/h,t2=t*t,t3=t2*t;Vector2 result=Vector2.zero;
            for(int side=0;side<2;side++)result[side]=(2*t3-3*t2+1)*q[i*2+side].y+(t3-2*t2+t)*h*Slope(q,i,side)
                +(-2*t3+3*t2)*q[(i+1)*2+side].y+(t3-t2)*h*Slope(q,i+1,side);
            return result;
        }
        public static bool Contains(int sign,Vector3 p) {
            var q=Rows[sign<0?0:1];if(p.x<q[0].x||p.x>q[q.Length-2].x)return false;
            Vector2 bounds=Bounds(sign,p.x);return p.y>=bounds.x&&p.y<=bounds.y;
        }
        public static float Clip(int sign,Vector3 center,Vector3 rim) {
            if(!Contains(sign,center))throw new System.InvalidOperationException("Iris center outside measured opening");
            if(Contains(sign,rim))return 1;
            float lo=0,hi=1;for(int i=0;i<24;i++){float t=(lo+hi)*.5f;if(Contains(sign,Vector3.Lerp(center,rim,t)))lo=t;else hi=t;}
            return lo;
        }
    }
}
