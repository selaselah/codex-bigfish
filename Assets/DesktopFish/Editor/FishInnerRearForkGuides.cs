using UnityEngine;
namespace DesktopFish.Editor {
    // Sparse alpha-visible positions only. Stable authored section lofts;
    // no source topology, UV, texture pixels or weights.
    public static class FishInnerRearForkGuides {
        public const float Join=0.4450000f, Cut=0.5791937f;
        static readonly Vector3[][] Rows = {
            new[] { // section family 0
                new Vector3(-0.2175331f,0.5100000f,-0.1071034f),
                new Vector3(-0.1860243f,0.5100000f,-0.1870123f),
                new Vector3(-0.1545155f,0.5100000f,-0.2110616f),
                new Vector3(-0.1230067f,0.5100000f,-0.2423867f),
                new Vector3(-0.0914979f,0.5100000f,-0.2501344f),
                new Vector3(-0.2183222f,0.4900000f,-0.1103329f),
                new Vector3(-0.1863545f,0.4900000f,-0.1903122f),
                new Vector3(-0.1543869f,0.4900000f,-0.2144399f),
                new Vector3(-0.1224192f,0.4900000f,-0.2461324f),
                new Vector3(-0.0904515f,0.4900000f,-0.2543188f),
                new Vector3(-0.2200031f,0.4700000f,-0.1146969f),
                new Vector3(-0.1876724f,0.4700000f,-0.1958540f),
                new Vector3(-0.1553416f,0.4700000f,-0.2200199f),
                new Vector3(-0.1230108f,0.4700000f,-0.2515221f),
                new Vector3(-0.0906801f,0.4700000f,-0.2600107f),
                new Vector3(-0.2217681f,0.4550000f,-0.1186176f),
                new Vector3(-0.1892678f,0.4550000f,-0.1994282f),
                new Vector3(-0.1567675f,0.4550000f,-0.2235032f),
                new Vector3(-0.1242673f,0.4550000f,-0.2567281f),
                new Vector3(-0.0917670f,0.4550000f,-0.2655135f),
                new Vector3(-0.2232228f,0.4450000f,-0.1216894f),
                new Vector3(-0.1907613f,0.4450000f,-0.2019062f),
                new Vector3(-0.1582999f,0.4450000f,-0.2259461f),
                new Vector3(-0.1258384f,0.4450000f,-0.2616255f),
                new Vector3(-0.0933770f,0.4450000f,-0.2704533f),
            },
            new[] { // section family 1
                new Vector3(-0.2240177f,0.4400000f,-0.1233354f),
                new Vector3(-0.2051788f,0.4400000f,-0.1938140f),
                new Vector3(-0.1863400f,0.4400000f,-0.2077811f),
                new Vector3(-0.1675011f,0.4400000f,-0.2217506f),
                new Vector3(-0.1486622f,0.4400000f,-0.2357436f),
                new Vector3(-0.2276842f,0.4200000f,-0.1306640f),
                new Vector3(-0.2097399f,0.4200000f,-0.2022862f),
                new Vector3(-0.1917956f,0.4200000f,-0.2155497f),
                new Vector3(-0.1738513f,0.4200000f,-0.2288727f),
                new Vector3(-0.1559070f,0.4200000f,-0.2421597f),
                new Vector3(-0.2340572f,0.3950000f,-0.1423891f),
                new Vector3(-0.2173803f,0.3950000f,-0.2149842f),
                new Vector3(-0.2007034f,0.3950000f,-0.2273809f),
                new Vector3(-0.1840265f,0.3950000f,-0.2398172f),
                new Vector3(-0.1673496f,0.3950000f,-0.2522535f),
                new Vector3(-0.2422041f,0.3700000f,-0.1565492f),
                new Vector3(-0.2263511f,0.3700000f,-0.2266598f),
                new Vector3(-0.2104981f,0.3700000f,-0.2386163f),
                new Vector3(-0.1946452f,0.3700000f,-0.2505026f),
                new Vector3(-0.1787922f,0.3700000f,-0.2623474f),
                new Vector3(-0.2507951f,0.3450000f,-0.1717971f),
                new Vector3(-0.2353731f,0.3450000f,-0.2357902f),
                new Vector3(-0.2199510f,0.3450000f,-0.2474597f),
                new Vector3(-0.2045290f,0.3450000f,-0.2591841f),
                new Vector3(-0.1891070f,0.3450000f,-0.2708525f),
                new Vector3(-0.2572813f,0.3200000f,-0.1859428f),
                new Vector3(-0.2426179f,0.3200000f,-0.2409473f),
                new Vector3(-0.2279546f,0.3200000f,-0.2538195f),
                new Vector3(-0.2132913f,0.3200000f,-0.2655153f),
                new Vector3(-0.1986280f,0.3200000f,-0.2774415f),
                new Vector3(-0.2600829f,0.2950000f,-0.1973781f),
                new Vector3(-0.2470344f,0.2950000f,-0.2470482f),
                new Vector3(-0.2339858f,0.2950000f,-0.2598029f),
                new Vector3(-0.2209372f,0.2950000f,-0.2708265f),
                new Vector3(-0.2078887f,0.2950000f,-0.2824523f),
                new Vector3(-0.2610359f,0.2700000f,-0.2076540f),
                new Vector3(-0.2500883f,0.2700000f,-0.2520998f),
                new Vector3(-0.2391407f,0.2700000f,-0.2649303f),
                new Vector3(-0.2281931f,0.2700000f,-0.2748241f),
                new Vector3(-0.2172455f,0.2700000f,-0.2857350f),
                new Vector3(-0.2599562f,0.2450000f,-0.2197048f),
                new Vector3(-0.2506544f,0.2450000f,-0.2581544f),
                new Vector3(-0.2413527f,0.2450000f,-0.2710954f),
                new Vector3(-0.2320509f,0.2450000f,-0.2795340f),
                new Vector3(-0.2227492f,0.2450000f,-0.2878132f),
                new Vector3(-0.2565360f,0.2250000f,-0.2342599f),
                new Vector3(-0.2485977f,0.2250000f,-0.2634009f),
                new Vector3(-0.2406595f,0.2250000f,-0.2765719f),
                new Vector3(-0.2327212f,0.2250000f,-0.2835046f),
                new Vector3(-0.2247830f,0.2250000f,-0.2896023f),
                new Vector3(-0.2507900f,0.2100000f,-0.2496901f),
                new Vector3(-0.2440521f,0.2100000f,-0.2695164f),
                new Vector3(-0.2373141f,0.2100000f,-0.2826199f),
                new Vector3(-0.2305762f,0.2100000f,-0.2882534f),
                new Vector3(-0.2238382f,0.2100000f,-0.2936434f),
                new Vector3(-0.2392920f,0.1950000f,-0.2719314f),
                new Vector3(-0.2346064f,0.1950000f,-0.2822721f),
                new Vector3(-0.2299207f,0.1950000f,-0.2925900f),
                new Vector3(-0.2252351f,0.1950000f,-0.2964205f),
                new Vector3(-0.2205494f,0.1950000f,-0.3001881f),
                new Vector3(-0.2352529f,0.1917500f,-0.2786088f),
                new Vector3(-0.2312891f,0.1917500f,-0.2873391f),
                new Vector3(-0.2273253f,0.1917500f,-0.2947897f),
                new Vector3(-0.2233615f,0.1917500f,-0.2988433f),
                new Vector3(-0.2193977f,0.1917500f,-0.3020565f),
                new Vector3(-0.2271157f,0.1867500f,-0.2912983f),
                new Vector3(-0.2247432f,0.1867500f,-0.2955439f),
                new Vector3(-0.2223708f,0.1867500f,-0.2998761f),
                new Vector3(-0.2199983f,0.1867500f,-0.3030537f),
                new Vector3(-0.2176258f,0.1867500f,-0.3049310f),
                new Vector3(-0.2200739f,0.1837500f,-0.3016335f),
                new Vector3(-0.2191961f,0.1837500f,-0.3031993f),
                new Vector3(-0.2183183f,0.1837500f,-0.3046432f),
                new Vector3(-0.2174405f,0.1837500f,-0.3056984f),
                new Vector3(-0.2165627f,0.1837500f,-0.3066557f),
                new Vector3(-0.2177266f,0.1827500f,-0.3050909f),
                new Vector3(-0.2175774f,0.1827500f,-0.3053692f),
                new Vector3(-0.2174282f,0.1827500f,-0.3055666f),
                new Vector3(-0.2172790f,0.1827500f,-0.3057461f),
                new Vector3(-0.2171298f,0.1827500f,-0.3058603f),
            },
            new[] { // section family 2
                new Vector3(-0.1473716f,0.4400000f,-0.2189522f),
                new Vector3(-0.1340742f,0.4400000f,-0.2620457f),
                new Vector3(-0.1207768f,0.4400000f,-0.2656625f),
                new Vector3(-0.1074794f,0.4400000f,-0.2692929f),
                new Vector3(-0.0941820f,0.4400000f,-0.2729233f),
                new Vector3(-0.1491406f,0.4200000f,-0.2299131f),
                new Vector3(-0.1365742f,0.4200000f,-0.2749209f),
                new Vector3(-0.1240077f,0.4200000f,-0.2783375f),
                new Vector3(-0.1114413f,0.4200000f,-0.2817028f),
                new Vector3(-0.0988748f,0.4200000f,-0.2850020f),
                new Vector3(-0.1531165f,0.3950000f,-0.2465958f),
                new Vector3(-0.1411096f,0.3950000f,-0.2911445f),
                new Vector3(-0.1291026f,0.3950000f,-0.2943183f),
                new Vector3(-0.1170957f,0.3950000f,-0.2974690f),
                new Vector3(-0.1050887f,0.3950000f,-0.3006197f),
                new Vector3(-0.1579304f,0.3700000f,-0.2642429f),
                new Vector3(-0.1462517f,0.3700000f,-0.3064308f),
                new Vector3(-0.1345730f,0.3700000f,-0.3096009f),
                new Vector3(-0.1228943f,0.3700000f,-0.3127711f),
                new Vector3(-0.1112156f,0.3700000f,-0.3159359f),
                new Vector3(-0.1620676f,0.3450000f,-0.2811537f),
                new Vector3(-0.1507353f,0.3450000f,-0.3199572f),
                new Vector3(-0.1394031f,0.3450000f,-0.3230346f),
                new Vector3(-0.1280708f,0.3450000f,-0.3261099f),
                new Vector3(-0.1167385f,0.3450000f,-0.3291592f),
                new Vector3(-0.1651293f,0.3200000f,-0.2968985f),
                new Vector3(-0.1543764f,0.3200000f,-0.3333680f),
                new Vector3(-0.1436235f,0.3200000f,-0.3363777f),
                new Vector3(-0.1328705f,0.3200000f,-0.3393197f),
                new Vector3(-0.1221176f,0.3200000f,-0.3420270f),
                new Vector3(-0.1682081f,0.2950000f,-0.3129228f),
                new Vector3(-0.1579553f,0.2950000f,-0.3446825f),
                new Vector3(-0.1477024f,0.2950000f,-0.3486703f),
                new Vector3(-0.1374496f,0.2950000f,-0.3512591f),
                new Vector3(-0.1271967f,0.2950000f,-0.3533806f),
                new Vector3(-0.1712992f,0.2700000f,-0.3291482f),
                new Vector3(-0.1616084f,0.2700000f,-0.3544176f),
                new Vector3(-0.1519175f,0.2700000f,-0.3584686f),
                new Vector3(-0.1422266f,0.2700000f,-0.3605550f),
                new Vector3(-0.1325357f,0.2700000f,-0.3624308f),
                new Vector3(-0.1747455f,0.2450000f,-0.3402050f),
                new Vector3(-0.1657921f,0.2450000f,-0.3605084f),
                new Vector3(-0.1568387f,0.2450000f,-0.3644305f),
                new Vector3(-0.1478852f,0.2450000f,-0.3663472f),
                new Vector3(-0.1389318f,0.2450000f,-0.3682766f),
                new Vector3(-0.1775442f,0.2250000f,-0.3438257f),
                new Vector3(-0.1695343f,0.2250000f,-0.3625082f),
                new Vector3(-0.1615244f,0.2250000f,-0.3659580f),
                new Vector3(-0.1535145f,0.2250000f,-0.3676655f),
                new Vector3(-0.1455046f,0.2250000f,-0.3693730f),
                new Vector3(-0.1799287f,0.2100000f,-0.3435374f),
                new Vector3(-0.1728612f,0.2100000f,-0.3610243f),
                new Vector3(-0.1657938f,0.2100000f,-0.3638072f),
                new Vector3(-0.1587263f,0.2100000f,-0.3656852f),
                new Vector3(-0.1516589f,0.2100000f,-0.3675384f),
                new Vector3(-0.1823132f,0.1950000f,-0.3432477f),
                new Vector3(-0.1764322f,0.1950000f,-0.3581524f),
                new Vector3(-0.1705513f,0.1950000f,-0.3600375f),
                new Vector3(-0.1646703f,0.1950000f,-0.3618599f),
                new Vector3(-0.1587894f,0.1950000f,-0.3636403f),
                new Vector3(-0.1847050f,0.1800000f,-0.3408221f),
                new Vector3(-0.1803958f,0.1800000f,-0.3517245f),
                new Vector3(-0.1760866f,0.1800000f,-0.3533453f),
                new Vector3(-0.1717774f,0.1800000f,-0.3550015f),
                new Vector3(-0.1674682f,0.1800000f,-0.3565977f),
                new Vector3(-0.1863856f,0.1700000f,-0.3374244f),
                new Vector3(-0.1835944f,0.1700000f,-0.3450816f),
                new Vector3(-0.1808031f,0.1700000f,-0.3461002f),
                new Vector3(-0.1780119f,0.1700000f,-0.3471362f),
                new Vector3(-0.1752206f,0.1700000f,-0.3481722f),
                new Vector3(-0.1870993f,0.1665000f,-0.3350211f),
                new Vector3(-0.1848651f,0.1665000f,-0.3410960f),
                new Vector3(-0.1826309f,0.1665000f,-0.3423023f),
                new Vector3(-0.1803966f,0.1665000f,-0.3434622f),
                new Vector3(-0.1781624f,0.1665000f,-0.3446221f),
                new Vector3(-0.1881189f,0.1615000f,-0.3315856f),
                new Vector3(-0.1869801f,0.1615000f,-0.3344716f),
                new Vector3(-0.1858414f,0.1615000f,-0.3351965f),
                new Vector3(-0.1847026f,0.1615000f,-0.3357958f),
                new Vector3(-0.1835638f,0.1615000f,-0.3363952f),
                new Vector3(-0.1887306f,0.1585000f,-0.3295165f),
                new Vector3(-0.1882491f,0.1585000f,-0.3303340f),
                new Vector3(-0.1877677f,0.1585000f,-0.3309749f),
                new Vector3(-0.1872862f,0.1585000f,-0.3312371f),
                new Vector3(-0.1868047f,0.1585000f,-0.3314590f),
                new Vector3(-0.1885542f,0.1575000f,-0.3291539f),
                new Vector3(-0.1883869f,0.1575000f,-0.3293701f),
                new Vector3(-0.1882196f,0.1575000f,-0.3295863f),
                new Vector3(-0.1880523f,0.1575000f,-0.3297560f),
                new Vector3(-0.1878849f,0.1575000f,-0.3298136f),
            },
        };
        public static float Bottom(int panel) => Rows[panel][Rows[panel].Length-1].y;
        public static float[] Stations(int panel) {
            var rows=Rows[panel];var result=new float[rows.Length/5];
            for(int i=0;i<result.Length;i++)result[i]=rows[i*5].y;return result;
        }
        static float Slope(float a,float b,float ha,float hb) {
            if(a*b<=0)return 0;
            float wa=2*hb+ha,wb=hb+2*ha;return (wa+wb)/(wa/a+wb/b);
        }
        static float Interpolate(float a,float b,float ma,float mb,float h,float t) {
            float t2=t*t,t3=t2*t;
            return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        public static Vector3 Point(int panel,float y,int col) {
            var rows=Rows[panel];int count=rows.Length/5;
            int row=0;while(row<count-2&&rows[(row+1)*5].y>y)row++;
            float h=rows[row*5].y-rows[(row+1)*5].y;
            float t=Mathf.Clamp01((rows[row*5].y-y)/h);
            Vector3 a=rows[row*5+col],b=rows[(row+1)*5+col],p=a;
            for(int k=0;k<3;k++) {
                if(k==1){p.y=y;continue;}
                float d=(b[k]-a[k])/h,ma=d,mb=d;
                if(row>0) {
                    float hp=rows[(row-1)*5].y-rows[row*5].y;
                    ma=Slope((a[k]-rows[(row-1)*5+col][k])/hp,d,hp,h);
                }
                if(row<count-2) {
                    float hn=rows[(row+1)*5].y-rows[(row+2)*5].y;
                    mb=Slope(d,(rows[(row+2)*5+col][k]-b[k])/hn,h,hn);
                }
                p[k]=Interpolate(a[k],b[k],ma,mb,h,t);
            }
            return p;
        }
        public static Vector3 Surface(int panel,float y,float across) {
            if(panel>0&&y<=Bottom(panel)+.000001f) {
                var rows=Rows[panel];Vector3 pole=Vector3.zero;
                for(int corner=0;corner<5;corner++)pole+=rows[rows.Length-5+corner]/5;return pole;
            }
            float f=Mathf.Clamp01(across)*4;int col=Mathf.Min(3,(int)f);float t=f-col;
            Vector3 a=Point(panel,y,col),b=Point(panel,y,col+1),p=a;
            for(int k=0;k<3;k++) {
                if(k==1){p.y=y;continue;}
                float d=b[k]-a[k],ma=d,mb=d;
                if(col>0)ma=Slope(a[k]-Point(panel,y,col-1)[k],d,1,1);
                if(col<3)mb=Slope(d,Point(panel,y,col+2)[k]-b[k],1,1);
                p[k]=Interpolate(a[k],b[k],ma,mb,1,t);
            }
            return p;
        }

    }
}
