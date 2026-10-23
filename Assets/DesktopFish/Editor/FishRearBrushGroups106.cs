using UnityEngine;
namespace DesktopFish.Editor {
    // Independent tapered blue-return curves fitted to sparse alpha-visible
    // positional runs. Source UVs, texels and color values stay in the reader.
    public static class FishRearBrushGroups106 {
        static readonly Vector3[][] Curves={
            new[]{new Vector3(-0.2662500f,0.2200000f,0.0000000f),new Vector3(-0.2662500f,0.2420000f,0.0037500f),new Vector3(-0.2712500f,0.2600000f,0.0062500f),new Vector3(-0.2775000f,0.2780000f,0.0075000f),new Vector3(-0.2825000f,0.2960000f,0.0110000f),new Vector3(-0.2950000f,0.3140000f,0.0050000f),new Vector3(-0.2950000f,0.3320000f,0.0000000f)},
            new[]{new Vector3(0.2362500f,0.2200000f,0.0000000f),new Vector3(0.2362500f,0.2420000f,0.0012500f),new Vector3(0.2412500f,0.2600000f,0.0037500f),new Vector3(0.2412500f,0.2780000f,0.0037500f),new Vector3(0.2437500f,0.2960000f,0.0062500f),new Vector3(0.2437500f,0.3140000f,0.0000000f)},
            new[]{new Vector3(0.2487500f,0.2200000f,0.0000000f),new Vector3(0.2487500f,0.2420000f,0.0012500f),new Vector3(0.2550000f,0.2600000f,0.0025000f),new Vector3(0.2625000f,0.2780000f,0.0050000f),new Vector3(0.2662500f,0.2960000f,0.0062500f),new Vector3(0.2725000f,0.3140000f,0.0110000f),new Vector3(0.2725000f,0.3320000f,0.0000000f)},
            new[]{new Vector3(-0.2362500f,0.2380000f,0.0000000f),new Vector3(-0.2362500f,0.2600000f,0.0012500f),new Vector3(-0.2375000f,0.2780000f,0.0025000f),new Vector3(-0.2387500f,0.2960000f,0.0012500f),new Vector3(-0.2387500f,0.3140000f,0.0000000f)},
            new[]{new Vector3(-0.1887500f,0.2380000f,0.0000000f),new Vector3(-0.1887500f,0.2600000f,0.0012500f),new Vector3(-0.1912500f,0.2780000f,0.0037500f),new Vector3(-0.1937500f,0.2960000f,0.0037500f),new Vector3(-0.1962500f,0.3140000f,0.0062500f),new Vector3(-0.1962500f,0.3320000f,0.0000000f)},
            new[]{new Vector3(-0.1337500f,0.2380000f,0.0000000f),new Vector3(-0.1337500f,0.2600000f,0.0012500f),new Vector3(-0.1337500f,0.2780000f,0.0012500f),new Vector3(-0.1350000f,0.2960000f,0.0025000f),new Vector3(-0.1337500f,0.3140000f,0.0012500f),new Vector3(-0.1362500f,0.3320000f,0.0037500f),new Vector3(-0.1362500f,0.3500000f,0.0000000f)},
            new[]{new Vector3(0.0437500f,0.2380000f,0.0000000f),new Vector3(0.0437500f,0.2600000f,0.0012500f),new Vector3(0.0387500f,0.2780000f,0.0012500f),new Vector3(0.0362500f,0.2960000f,0.0012500f),new Vector3(0.0337500f,0.3140000f,0.0012500f),new Vector3(0.0287500f,0.3320000f,0.0062500f),new Vector3(0.0325000f,0.3500000f,0.0050000f),new Vector3(0.0400000f,0.3680000f,0.0100000f),new Vector3(0.0400000f,0.3860000f,0.0000000f)},
            new[]{new Vector3(0.1412500f,0.2380000f,0.0000000f),new Vector3(0.1412500f,0.2600000f,0.0012500f),new Vector3(0.1425000f,0.2780000f,0.0025000f),new Vector3(0.1425000f,0.2960000f,0.0050000f),new Vector3(0.1437500f,0.3140000f,0.0062500f),new Vector3(0.1437500f,0.3320000f,0.0000000f)},
            new[]{new Vector3(0.2125000f,0.2380000f,0.0000000f),new Vector3(0.2125000f,0.2600000f,0.0025000f),new Vector3(0.2162500f,0.2780000f,0.0062500f),new Vector3(0.2162500f,0.2960000f,0.0087500f),new Vector3(0.2162500f,0.3140000f,0.0000000f)},
            new[]{new Vector3(-0.2262500f,0.2560000f,0.0000000f),new Vector3(-0.2262500f,0.2780000f,0.0012500f),new Vector3(-0.2262500f,0.2960000f,0.0037500f),new Vector3(-0.2312500f,0.3140000f,0.0110000f),new Vector3(-0.2312500f,0.3320000f,0.0000000f)},
            new[]{new Vector3(-0.1725000f,0.2560000f,0.0000000f),new Vector3(-0.1725000f,0.2780000f,0.0025000f),new Vector3(-0.1737500f,0.2960000f,0.0062500f),new Vector3(-0.1737500f,0.3140000f,0.0087500f),new Vector3(-0.1737500f,0.3320000f,0.0000000f)},
            new[]{new Vector3(0.1737500f,0.2560000f,0.0000000f),new Vector3(0.1737500f,0.2780000f,0.0037500f),new Vector3(0.1762500f,0.2960000f,0.0062500f),new Vector3(0.1775000f,0.3140000f,0.0075000f),new Vector3(0.1775000f,0.3320000f,0.0000000f)},
            new[]{new Vector3(0.3512500f,0.2740000f,0.0000000f),new Vector3(0.3512500f,0.2960000f,0.0012500f),new Vector3(0.3525000f,0.3140000f,0.0025000f),new Vector3(0.3525000f,0.3320000f,0.0050000f),new Vector3(0.3500000f,0.3500000f,0.0075000f),new Vector3(0.3500000f,0.3680000f,0.0000000f)},
            new[]{new Vector3(-0.3512500f,0.2920000f,0.0000000f),new Vector3(-0.3512500f,0.3140000f,0.0012500f),new Vector3(-0.3475000f,0.3320000f,0.0050000f),new Vector3(-0.3425000f,0.3500000f,0.0075000f),new Vector3(-0.3425000f,0.3680000f,0.0000000f)},
            new[]{new Vector3(-0.1062500f,0.2920000f,0.0000000f),new Vector3(-0.1062500f,0.3140000f,0.0012500f),new Vector3(-0.1050000f,0.3320000f,0.0050000f),new Vector3(-0.1000000f,0.3500000f,0.0100000f),new Vector3(-0.0987500f,0.3680000f,0.0110000f),new Vector3(-0.0987500f,0.3860000f,0.0000000f)},
            new[]{new Vector3(0.0037500f,0.2920000f,0.0000000f),new Vector3(0.0037500f,0.3140000f,0.0012500f),new Vector3(0.0075000f,0.3320000f,0.0050000f),new Vector3(0.0100000f,0.3500000f,0.0075000f),new Vector3(0.0112500f,0.3680000f,0.0110000f),new Vector3(0.0112500f,0.3860000f,0.0000000f)},
            new[]{new Vector3(0.0762500f,0.3100000f,0.0000000f),new Vector3(0.0762500f,0.3320000f,0.0012500f),new Vector3(0.0712500f,0.3500000f,0.0037500f),new Vector3(0.0675000f,0.3680000f,0.0050000f),new Vector3(0.0675000f,0.3860000f,0.0000000f)},
            new[]{new Vector3(0.0962500f,0.3100000f,0.0000000f),new Vector3(0.0962500f,0.3320000f,0.0012500f),new Vector3(0.0975000f,0.3500000f,0.0050000f),new Vector3(0.0925000f,0.3680000f,0.0110000f),new Vector3(0.0925000f,0.3860000f,0.0000000f)},
            new[]{new Vector3(0.1137500f,0.3100000f,0.0000000f),new Vector3(0.1137500f,0.3320000f,0.0012500f),new Vector3(0.1187500f,0.3500000f,0.0037500f),new Vector3(0.1212500f,0.3680000f,0.0062500f),new Vector3(0.1212500f,0.3860000f,0.0000000f)},
        };
        static Vector2 Sample(Vector3[] curve,float y) {
            int i=0;while(i<curve.Length-2&&curve[i+1].y<y)i++;
            float t=Mathf.Clamp01((y-curve[i].y)/(curve[i+1].y-curve[i].y));
            Vector3 a=curve[Mathf.Max(0,i-1)],b=curve[i],c=curve[i+1],d=curve[Mathf.Min(curve.Length-1,i+2)];
            float x=.5f*((2*b.x)+(-a.x+c.x)*t+(2*a.x-5*b.x+4*c.x-d.x)*t*t+(-a.x+3*b.x-3*c.x+d.x)*t*t*t);
            float w=Mathf.Lerp(b.z,c.z,Mathf.SmoothStep(0,1,t));
            return new Vector2(x,Mathf.Max(0,w));
        }
        public static float Coverage(float x,float y) {
            float amount=0;
            foreach(var curve in Curves) {
                if(y<=curve[0].y||y>=curve[curve.Length-1].y)continue;
                var q=Sample(curve,y);if(q.y<.00005f)continue;
                amount=Mathf.Max(amount,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,Mathf.Abs(x-q.x)/q.y)));
            }
            return amount;
        }
        public static float ReturnOffset(float x,float y) {
            float total=0,offset=0;
            foreach(var curve in Curves) {
                if(y<=curve[0].y||y>=curve[curve.Length-1].y)continue;
                var q=Sample(curve,y);
                float weight=Mathf.Exp(-Mathf.Pow((x-q.x)/.023f,2))*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.004f,q.y));
                total+=weight;offset+=Mathf.Clamp(q.x-x,-.010f,.010f)*weight;
            }
            // Width-weighted influence fades continuously at every pointed
            // end; hard nearest-curve switches would notch the older returns.
            return offset/(1+total)*.65f;
        }
    }
}
