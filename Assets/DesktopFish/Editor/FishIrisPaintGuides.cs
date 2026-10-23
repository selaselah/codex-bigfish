using UnityEngine;
namespace DesktopFish.Editor {
    // Sparse visible position landmarks mapped through our independently
    // authored iris fan. These are our paint coordinates, not source UVs.
    public static class FishIrisPaintGuides {
        static readonly Vector2[] Bands={
            new Vector2(0.0672932f,-0.5532207f),
            new Vector2(-0.3598684f,-0.6187090f),
            new Vector2(-0.6028065f,-0.9488411f),
        };
        public static float Boundary(int band,float u)=>Bands[band].x+Bands[band].y*u*u;
        public static readonly Vector4[] Glints={
            new Vector4(0.2778001f,0.5846770f,0.2210736f,0.2030345f),
            new Vector4(-0.1391833f,0.6627845f,0.0741287f,0.0639782f),
            new Vector4(-0.6644948f,-0.6430024f,0.0780833f,0.0632033f),
        };
    }
}
