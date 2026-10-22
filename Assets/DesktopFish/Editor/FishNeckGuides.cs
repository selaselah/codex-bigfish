using UnityEngine;
using System.Linq;

namespace DesktopFish.Editor {
    // Sparse head/neck-influenced body-skin bounds, not the front facial shell.
    // Our closed low-density loft fills the hidden interface independently.
    public static class FishNeckGuides {
        // finalY, half width, posteriorZ, anterior extentZ.
        static readonly Vector4[] Rows = {
            new Vector4(0.5325000f,0.0421489f,-0.0657069f,0.0042306f),
            new Vector4(0.5450000f,0.0309892f,-0.0676440f,0.0215496f),
            new Vector4(0.5575000f,0.0377154f,-0.0738064f,0.0445504f),
            new Vector4(0.5700000f,0.0624321f,-0.0843653f,0.0271896f),
            new Vector4(0.5825000f,0.0821148f,-0.0982763f,0.0092482f),
            new Vector4(0.5950000f,0.0989371f,-0.1142245f,-0.0079044f),
            new Vector4(0.6200000f,0.1271576f,-0.1496771f,-0.0317272f),
            new Vector4(0.6450000f,0.1483592f,-0.1794377f,-0.0460374f),
            new Vector4(0.6700000f,0.1643210f,-0.2024797f,-0.0587642f),
            new Vector4(0.6950000f,0.1770163f,-0.2171620f,-0.0585142f),
            new Vector4(0.7200000f,0.1874860f,-0.2274583f,-0.0137723f),
        };
        public static float[] Stations => new[]{.510f}.Concat(Rows.Select(p=>p.x)).Concat(new[]{.75f}).Reverse().ToArray();
        public static Vector4 Profile(float y) {
            int i=0;while(i<Rows.Length-2&&Rows[i+1].x<y)i++;
            return Vector4.Lerp(Rows[i],Rows[i+1],Mathf.Clamp01((y-Rows[i].x)/(Rows[i+1].x-Rows[i].x)));
        }
    }
}
