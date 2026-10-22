using UnityEngine;
namespace DesktopFish.Editor {
    // Sparse alpha-visible rays through the combined local rear-hair surface.
    // The mesh, chart and paint remain authored rather than copied.
    public static class FishRearEnvelopeGuides {
        static readonly Vector3[][] Rows={
            new[]{
                new Vector3(-0.2633112f,0.7325000f,-0.0770599f),new Vector3(-0.2372429f,0.7325000f,-0.1890302f),new Vector3(-0.2111746f,0.7325000f,-0.2228275f),
                new Vector3(-0.1851063f,0.7325000f,-0.2539718f),new Vector3(-0.1590381f,0.7325000f,-0.2735464f),new Vector3(-0.1329698f,0.7325000f,-0.2773916f),
                new Vector3(-0.1069015f,0.7325000f,-0.3020200f),new Vector3(-0.0808332f,0.7325000f,-0.3119208f),new Vector3(-0.0547649f,0.7325000f,-0.3130530f),
                new Vector3(-0.0286966f,0.7325000f,-0.3258932f),new Vector3(-0.0026283f,0.7325000f,-0.3208011f),new Vector3(0.0234400f,0.7325000f,-0.3202918f),
                new Vector3(0.0495082f,0.7325000f,-0.3172181f),new Vector3(0.0755765f,0.7325000f,-0.3143304f),new Vector3(0.1016448f,0.7325000f,-0.3041552f),
                new Vector3(0.1277131f,0.7325000f,-0.2829831f),new Vector3(0.1537814f,0.7325000f,-0.2619561f),new Vector3(0.1798497f,0.7325000f,-0.2594008f),
                new Vector3(0.2059179f,0.7325000f,-0.2305127f),new Vector3(0.2319862f,0.7325000f,-0.2021444f),new Vector3(0.2580545f,0.7325000f,-0.0827929f),
            },
            new[]{
                new Vector3(-0.2666198f,0.6825000f,-0.0828238f),new Vector3(-0.2400453f,0.6825000f,-0.1428061f),new Vector3(-0.2134709f,0.6825000f,-0.2243685f),
                new Vector3(-0.1868965f,0.6825000f,-0.2545737f),new Vector3(-0.1603221f,0.6825000f,-0.2736118f),new Vector3(-0.1337476f,0.6825000f,-0.2801079f),
                new Vector3(-0.1071732f,0.6825000f,-0.3051617f),new Vector3(-0.0805988f,0.6825000f,-0.3164758f),new Vector3(-0.0540244f,0.6825000f,-0.3176484f),
                new Vector3(-0.0274499f,0.6825000f,-0.3284276f),new Vector3(-0.0008755f,0.6825000f,-0.3281891f),new Vector3(0.0256989f,0.6825000f,-0.3253689f),
                new Vector3(0.0522734f,0.6825000f,-0.3225487f),new Vector3(0.0788478f,0.6825000f,-0.3197300f),new Vector3(0.1054222f,0.6825000f,-0.3071739f),
                new Vector3(0.1319966f,0.6825000f,-0.2860627f),new Vector3(0.1585710f,0.6825000f,-0.2785506f),new Vector3(0.1851455f,0.6825000f,-0.2564297f),
                new Vector3(0.2117199f,0.6825000f,-0.2261310f),new Vector3(0.2382943f,0.6825000f,-0.1526518f),new Vector3(0.2648687f,0.6825000f,-0.0862055f),
            },
            new[]{
                new Vector3(-0.2652049f,0.6325000f,-0.0902694f),new Vector3(-0.2386131f,0.6325000f,-0.1514163f),new Vector3(-0.2120213f,0.6325000f,-0.2271076f),
                new Vector3(-0.1854295f,0.6325000f,-0.2561445f),new Vector3(-0.1588378f,0.6325000f,-0.2752949f),new Vector3(-0.1322460f,0.6325000f,-0.2886427f),
                new Vector3(-0.1056542f,0.6325000f,-0.3144541f),new Vector3(-0.0790624f,0.6325000f,-0.3277167f),new Vector3(-0.0524707f,0.6325000f,-0.3286096f),
                new Vector3(-0.0258789f,0.6325000f,-0.3364943f),new Vector3(0.0007129f,0.6325000f,-0.3368662f),new Vector3(0.0273047f,0.6325000f,-0.3319036f),
                new Vector3(0.0538964f,0.6325000f,-0.3291465f),new Vector3(0.0804882f,0.6325000f,-0.3263951f),new Vector3(0.1070800f,0.6325000f,-0.3110223f),
                new Vector3(0.1336717f,0.6325000f,-0.2897962f),new Vector3(0.1602635f,0.6325000f,-0.2764846f),new Vector3(0.1868553f,0.6325000f,-0.2550573f),
                new Vector3(0.2134470f,0.6325000f,-0.2260248f),new Vector3(0.2400388f,0.6325000f,-0.1525712f),new Vector3(0.2666306f,0.6325000f,-0.0910259f),
            },
            new[]{
                new Vector3(-0.2593987f,0.5825000f,-0.1007037f),new Vector3(-0.2333255f,0.5825000f,-0.1651796f),new Vector3(-0.2072523f,0.5825000f,-0.2349832f),
                new Vector3(-0.1811792f,0.5825000f,-0.2614907f),new Vector3(-0.1551060f,0.5825000f,-0.2783420f),new Vector3(-0.1290328f,0.5825000f,-0.3004996f),
                new Vector3(-0.1029596f,0.5825000f,-0.3274427f),new Vector3(-0.0768865f,0.5825000f,-0.3432082f),new Vector3(-0.0508133f,0.5825000f,-0.3443829f),
                new Vector3(-0.0247401f,0.5825000f,-0.3474855f),new Vector3(0.0013330f,0.5825000f,-0.3537961f),new Vector3(0.0274062f,0.5825000f,-0.3476474f),
                new Vector3(0.0534794f,0.5825000f,-0.3443708f),new Vector3(0.0795525f,0.5825000f,-0.3410942f),new Vector3(0.1056257f,0.5825000f,-0.3239472f),
                new Vector3(0.1316989f,0.5825000f,-0.2999020f),new Vector3(0.1577721f,0.5825000f,-0.2782486f),new Vector3(0.1838453f,0.5825000f,-0.2592627f),
                new Vector3(0.2099184f,0.5825000f,-0.2324517f),new Vector3(0.2359916f,0.5825000f,-0.1645024f),new Vector3(0.2620648f,0.5825000f,-0.0991172f),
            },
            new[]{
                new Vector3(-0.2531270f,0.5325000f,-0.1123353f),new Vector3(-0.2277347f,0.5325000f,-0.1797148f),new Vector3(-0.2023424f,0.5325000f,-0.2427405f),
                new Vector3(-0.1769501f,0.5325000f,-0.2616661f),new Vector3(-0.1515578f,0.5325000f,-0.2794431f),new Vector3(-0.1261655f,0.5325000f,-0.3102718f),
                new Vector3(-0.1007731f,0.5325000f,-0.3395438f),new Vector3(-0.0753808f,0.5325000f,-0.3583567f),new Vector3(-0.0499885f,0.5325000f,-0.3601211f),
                new Vector3(-0.0245962f,0.5325000f,-0.3624017f),new Vector3(0.0007961f,0.5325000f,-0.3715374f),new Vector3(0.0261884f,0.5325000f,-0.3662406f),
                new Vector3(0.0515808f,0.5325000f,-0.3625955f),new Vector3(0.0769730f,0.5325000f,-0.3589503f),new Vector3(0.1023654f,0.5325000f,-0.3429281f),
                new Vector3(0.1277577f,0.5325000f,-0.3174730f),new Vector3(0.1531500f,0.5325000f,-0.2927382f),new Vector3(0.1785423f,0.5325000f,-0.2667112f),
                new Vector3(0.2039346f,0.5325000f,-0.2406295f),new Vector3(0.2293269f,0.5325000f,-0.1786550f),new Vector3(0.2547193f,0.5325000f,-0.1090598f),
            },
            new[]{
                new Vector3(-0.2671357f,0.4825000f,-0.1373837f),new Vector3(-0.2404257f,0.4825000f,-0.1569735f),new Vector3(-0.2137158f,0.4825000f,-0.2525702f),
                new Vector3(-0.1870058f,0.4825000f,-0.2717173f),new Vector3(-0.1602958f,0.4825000f,-0.2899318f),new Vector3(-0.1335859f,0.4825000f,-0.3246078f),
                new Vector3(-0.1068759f,0.4825000f,-0.3495318f),new Vector3(-0.0801659f,0.4825000f,-0.3841163f),new Vector3(-0.0534560f,0.4825000f,-0.3882010f),
                new Vector3(-0.0267460f,0.4825000f,-0.3923356f),new Vector3(-0.0000360f,0.4825000f,-0.4001897f),new Vector3(0.0266739f,0.4825000f,-0.3977839f),
                new Vector3(0.0533839f,0.4825000f,-0.3901532f),new Vector3(0.0800938f,0.4825000f,-0.3825225f),new Vector3(0.1068038f,0.4825000f,-0.3591119f),
                new Vector3(0.1335138f,0.4825000f,-0.3333882f),new Vector3(0.1602237f,0.4825000f,-0.3031632f),new Vector3(0.1869337f,0.4825000f,-0.2714535f),
                new Vector3(0.2136437f,0.4825000f,-0.2531414f),new Vector3(0.2403536f,0.4825000f,-0.2003803f),new Vector3(0.2670636f,0.4825000f,-0.1334708f),
            },
        };
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;float wa=2*hb+ha,wb=hb+2*ha;return (wa+wb)/(wa/a+wb/b);
        }
        static float Cubic(float a,float b,float ma,float mb,float h,float t) {
            float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        static float RowDepth(int row,float x) {
            var p=Rows[row];int col=0;while(col<p.Length-2&&p[col+1].x<x)col++;
            float h=p[col+1].x-p[col].x,t=Mathf.Clamp01((x-p[col].x)/h),d=(p[col+1].z-p[col].z)/h,ma=d,mb=d;
            if(col>0){float hp=p[col].x-p[col-1].x;ma=Slope((p[col].z-p[col-1].z)/hp,d,hp,h);}
            if(col<p.Length-2){float hn=p[col+2].x-p[col+1].x;mb=Slope(d,(p[col+2].z-p[col+1].z)/hn,h,hn);}
            return Cubic(p[col].z,p[col+1].z,ma,mb,h,t);
        }
        public static float Depth(float x,float y) {
            int row=0;while(row<Rows.Length-2&&Rows[row+1][0].y>y)row++;
            float h=Rows[row][0].y-Rows[row+1][0].y,t=Mathf.Clamp01((Rows[row][0].y-y)/h);
            float a=RowDepth(row,x),b=RowDepth(row+1,x),d=(b-a)/h,ma=d,mb=d;
            if(row>0){float hp=Rows[row-1][0].y-Rows[row][0].y;ma=Slope((a-RowDepth(row-1,x))/hp,d,hp,h);}
            if(row<Rows.Length-2){float hn=Rows[row+1][0].y-Rows[row+2][0].y;mb=Slope(d,(RowDepth(row+2,x)-b)/hn,h,hn);}
            return Cubic(a,b,ma,mb,h,t);
        }
    }
}
