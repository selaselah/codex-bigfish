using UnityEngine;

namespace DesktopFish.Editor {
    // Sparse alpha-visible skin side/rear positions, locally registered.
    // A fresh monotone depth map reshapes our own closed shell; source topology,
    // eye-hole topology and UVs are not copied. Frontal depth anchors stay fixed.
    public static class FishHeadSideGuides {
        // final Y, rear edge Z, mean left/right outer-side Z, front extremum Z.
        static readonly Vector4[] Rows = {
            new Vector4(0.5481250f,0.0568485f,0.1037555f,0.1325380f),
            new Vector4(0.5512500f,0.0528674f,0.0843297f,0.1391729f),
            new Vector4(0.5575000f,0.0445368f,0.1052127f,0.1436533f),
            new Vector4(0.5700000f,0.0271807f,0.0928513f,0.1496229f),
            new Vector4(0.5825000f,0.0092307f,0.0783848f,0.1564125f),
            new Vector4(0.5950000f,-0.0079180f,0.0567309f,0.1623304f),
            new Vector4(0.6075000f,-0.0222680f,0.0463272f,0.1693820f),
            new Vector4(0.6200000f,-0.0317353f,0.0190533f,0.1733995f),
            new Vector4(0.6325000f,-0.0398147f,0.0141807f,0.1721651f),
            new Vector4(0.6450000f,-0.0460440f,0.0062236f,0.1577257f),
            new Vector4(0.6575000f,-0.0525385f,0.0092040f,0.1510387f),
            new Vector4(0.6700000f,-0.0587696f,0.0089204f,0.1494433f),
            new Vector4(0.6950000f,-0.0585174f,-0.0143813f,0.1507123f),
            new Vector4(0.7200000f,-0.0462476f,-0.0137759f,0.1549397f),
            new Vector4(0.7450000f,-0.0121531f,-0.0121530f,0.1609452f),
            new Vector4(0.7700000f,-0.0078186f,-0.0078186f,0.1670806f),
            new Vector4(0.7950000f,-0.0021039f,-0.0021038f,0.1682328f),
            new Vector4(0.8200000f,0.0044405f,0.0044406f,0.1646719f),
        };
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;float wa=2*hb+ha,wb=hb+2*ha;return (wa+wb)/(wa/a+wb/b);
        }
        static float Cubic(float a,float b,float ma,float mb,float h,float t) {
            float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        public static Vector4 Profile(float y) {
            int i=0;while(i<Rows.Length-2&&Rows[i+1].x<y)i++;
            float h=Rows[i+1].x-Rows[i].x,t=Mathf.Clamp01((y-Rows[i].x)/h);Vector4 p=Rows[i];p.x=y;
            for(int k=1;k<4;k++) {
                float a=Rows[i][k],b=Rows[i+1][k],d=(b-a)/h,ma=d,mb=d;
                if(i>0){float hp=Rows[i].x-Rows[i-1].x;ma=Slope((a-Rows[i-1][k])/hp,d,hp,h);}
                if(i<Rows.Length-2){float hn=Rows[i+2].x-Rows[i+1].x;mb=Slope(d,(Rows[i+2][k]-b)/hn,h,hn);}
                p[k]=Cubic(a,b,ma,mb,h,t);
            }
            return p;
        }
        public static float Depth(float existing,float raw,float radius,float jaw,float y) {
            if(radius<.001f||y>=.80f)return existing;
            float q=Mathf.Min(.10f,radius*.85f);if(raw>=q)return existing;
            Vector4 profile=Profile(y);float anchor=q+jaw;
            // The reference is an open facial shell. Keep a thin, independently
            // closed posterior surface inside the hair without collapsing faces.
            float rear=Mathf.Min(profile.y,profile.z-.001f),side=Mathf.Clamp(profile.z,rear+.0001f,anchor-.001f);
            float a=(side-rear)/radius,b=(anchor-side)/q,mid=Slope(a,b,radius,q),mapped;
            if(raw<=0)mapped=Cubic(rear,side,a,mid,radius,Mathf.Clamp01((raw+radius)/radius));
            else mapped=Cubic(side,anchor,mid,Mathf.Min(1,3*b),q,Mathf.Clamp01(raw/q));
            float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.80f,y));
            return Mathf.Lerp(existing,mapped,weight);
        }
    }
}
