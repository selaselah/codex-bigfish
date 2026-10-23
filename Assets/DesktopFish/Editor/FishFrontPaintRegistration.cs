using System;
using System.IO;
using UnityEngine;
namespace DesktopFish.Editor {
    // The current independently authored surface supplies its own paint chart.
    // Source reflection contours remain sparse world-space guides; no reference
    // topology, texels, UVs, normals or skin weights enter this reconstruction.
    public static class FishFrontPaintRegistration {
        public sealed class Field {
            internal readonly Vector3[][] positions=new Vector3[17][];
            internal readonly bool[][] occupied=new bool[17][];
            public bool TryPoint(int panel,int x,int y,out Vector3 point) {
                int index=y*128+x;point=positions[panel][index];return occupied[panel][index];
            }
        }
        [Serializable] sealed class PanelReport {public int panel,triangles,coveredPixels;public float meanXYDifference,maxXYDifference;}
        [Serializable] sealed class Report {public string scope;public PanelReport[] panels;}
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        public static Field Raster(Vector3[] points,Vector2[] pixelUV,int[] triangles) {
            var field=new Field();var reports=new PanelReport[17];
            for(int panel=0;panel<17;panel++) {
                field.positions[panel]=new Vector3[128*320];field.occupied[panel]=new bool[128*320];reports[panel]=new PanelReport{panel=panel};
            }
            for(int k=0;k<triangles.Length;k+=3) {
                int a=triangles[k],b=triangles[k+1],c=triangles[k+2];Vector2 first=pixelUV[a];
                int column=Mathf.FloorToInt(first.x/128),row=Mathf.FloorToInt((first.y-128)/320),panel=row*6+column;
                if(column<0||column>=6||row<0||panel<0||panel>=17)continue;
                float ox=column*128,oy=128+row*320;
                Func<Vector2,bool> inside=q=>q.x>=ox+15.99f&&q.x<=ox+112.01f&&q.y>=oy+15.99f&&q.y<=oy+304.01f;
                if(!inside(first)||!inside(pixelUV[b])||!inside(pixelUV[c]))continue;
                Vector2 u=first-new Vector2(ox,oy),v=pixelUV[b]-new Vector2(ox,oy),w=pixelUV[c]-new Vector2(ox,oy);
                float denominator=Cross(v-u,w-u);if(Mathf.Abs(denominator)<.000001f)continue;
                int x0=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.x,Mathf.Min(v.x,w.x))-.5f)),x1=Mathf.Min(127,Mathf.FloorToInt(Mathf.Max(u.x,Mathf.Max(v.x,w.x))-.5f));
                int y0=Mathf.Max(0,Mathf.CeilToInt(Mathf.Min(u.y,Mathf.Min(v.y,w.y))-.5f)),y1=Mathf.Min(319,Mathf.FloorToInt(Mathf.Max(u.y,Mathf.Max(v.y,w.y))-.5f));
                reports[panel].triangles++;
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) {
                    Vector2 delta=new Vector2(x+.5f,y+.5f)-u;
                    float wb=Cross(delta,w-u)/denominator,wc=Cross(v-u,delta)/denominator,wa=1-wb-wc;
                    if(wa<-.00001f||wb<-.00001f||wc<-.00001f)continue;
                    Vector3 p=points[a]*wa+points[b]*wb+points[c]*wc;
                    if(panel<=7||panel>=15)p.x=Mathf.Abs(p.x);
                    int index=y*128+x;
                    // Shared front/return charts use the physical front face.
                    // This resolves the UV overlap without moving any geometry.
                    if(field.occupied[panel][index]&&field.positions[panel][index].z>=p.z)continue;
                    field.positions[panel][index]=p;field.occupied[panel][index]=true;
                }
            }
            for(int panel=0;panel<17;panel++) {
                double total=0;
                for(int y=0;y<320;y++)for(int x=0;x<128;x++) {
                    int index=y*128+x;if(!field.occupied[panel][index])continue;
                    float u=Mathf.Clamp01((x+.5f-16)/96),t=Mathf.Clamp01((y+.5f-16)/288);
                    Vector3 previous=FishModelGuides.FrontPanel(panel,1,t,u),current=field.positions[panel][index];
                    float error=Vector2.Distance(new Vector2(previous.x,previous.y),new Vector2(current.x,current.y));
                    reports[panel].coveredPixels++;total+=error;reports[panel].maxXYDifference=Mathf.Max(reports[panel].maxXYDifference,error);
                }
                if(reports[panel].coveredPixels==0)throw new InvalidOperationException("Missing authored front-paint chart "+panel);
                reports[panel].meanXYDifference=(float)(total/reports[panel].coveredPixels);
            }
            Directory.CreateDirectory("work");File.WriteAllText("work/front-paint-registration.json",JsonUtility.ToJson(new Report{scope="UV-to-current-author-surface versus legacy interpolated field; construction diagnostics, not likeness scores",panels=reports},true));
            return field;
        }
    }
}
