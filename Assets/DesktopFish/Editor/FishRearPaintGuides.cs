using UnityEngine;

namespace DesktopFish.Editor {
    // Sparse alpha-visible color-transition positions registered from the local
    // reference. These are curve guides, never source pixels or source UVs.
    public static class FishRearPaintGuides {
        // x, highest visible cyan, highest point of the longest lower cyan interval.
        static readonly Vector3[] Rows = {
            new Vector3(-0.3125000f,0.2481250f,0.2481250f),
            new Vector3(-0.2968750f,0.2931250f,0.2931250f),
            new Vector3(-0.2812500f,0.4131250f,0.2668750f),
            new Vector3(-0.2656250f,0.2968750f,0.2368750f),
            new Vector3(-0.2500000f,0.3081250f,0.2593750f),
            new Vector3(-0.2343750f,0.3475000f,0.2443750f),
            new Vector3(-0.2187500f,0.3175000f,0.3175000f),
            new Vector3(-0.2031250f,0.3287500f,0.3287500f),
            new Vector3(-0.1875000f,0.3287500f,0.2556250f),
            new Vector3(-0.1718750f,0.2593750f,0.2593750f),
            new Vector3(-0.1562500f,0.3475000f,0.3475000f),
            new Vector3(-0.1406250f,0.3456250f,0.3456250f),
            new Vector3(-0.1250000f,0.3550000f,0.3550000f),
            new Vector3(-0.1093750f,0.3306250f,0.3306250f),
            new Vector3(-0.0937500f,0.3475000f,0.3475000f),
            new Vector3(-0.0781250f,0.3250000f,0.3250000f),
            new Vector3(-0.0625000f,0.3006250f,0.2593750f),
            new Vector3(-0.0468750f,0.3137500f,0.3137500f),
            new Vector3(-0.0312500f,0.3212500f,0.3212500f),
            new Vector3(-0.0156250f,0.3343750f,0.3343750f),
            new Vector3(0.0000000f,0.3643750f,0.3643750f),
            new Vector3(0.0156250f,0.3418750f,0.2912500f),
            new Vector3(0.0312500f,0.3006250f,0.3006250f),
            new Vector3(0.0468750f,0.3962500f,0.3906250f),
            new Vector3(0.0625000f,0.3606250f,0.3606250f),
            new Vector3(0.0781250f,0.3756250f,0.3756250f),
            new Vector3(0.0937500f,0.3287500f,0.3287500f),
            new Vector3(0.1093750f,0.3700000f,0.3700000f),
            new Vector3(0.1250000f,0.3587500f,0.3587500f),
            new Vector3(0.1406250f,0.2500000f,0.2500000f),
            new Vector3(0.1562500f,0.3306250f,0.3306250f),
            new Vector3(0.1718750f,0.2575000f,0.2575000f),
            new Vector3(0.1875000f,0.3156250f,0.3156250f),
            new Vector3(0.2031250f,0.3268750f,0.3268750f),
            new Vector3(0.2187500f,0.2650000f,0.2650000f),
            new Vector3(0.2343750f,0.3587500f,0.3043750f),
            new Vector3(0.2500000f,0.3325000f,0.2931250f),
            new Vector3(0.2656250f,0.2725000f,0.2725000f),
            new Vector3(0.2812500f,0.4075000f,0.2387500f),
            new Vector3(0.2968750f,0.3362500f,0.3362500f),
            new Vector3(0.3125000f,0.2593750f,0.2593750f),
        };
        static float Slope(float a,float b) => a*b<=0?0:2*a*b/(a+b);
        public static float Height(float x,bool broad=false) {
            int i=0;while(i<Rows.Length-2&&Rows[i+1].x<x)i++;
            int channel=broad?2:1;float h=Rows[i+1].x-Rows[i].x,t=Mathf.Clamp01((x-Rows[i].x)/h);
            float a=Rows[i][channel],b=Rows[i+1][channel],d=(b-a)/h,ma=d,mb=d;
            if(i>0)ma=Slope((a-Rows[i-1][channel])/(Rows[i].x-Rows[i-1].x),d);
            if(i<Rows.Length-2)mb=Slope(d,(Rows[i+2][channel]-b)/(Rows[i+2].x-Rows[i+1].x));
            float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
    }
}
