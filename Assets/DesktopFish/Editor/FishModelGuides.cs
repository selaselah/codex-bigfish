using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesktopFish.Editor {
    // Sparse section guides measured from the local review, registered to the
    // rebuilt coordinate system. Curves between them generate new topology.
    public static class FishModelGuides {
        static readonly Vector4[] HairEnvelope = {
            new Vector4(0.530000f,1.000000f,1.000000f,0.000000f),
            new Vector4(0.632500f,0.976623f,1.032812f,-0.006522f),
            new Vector4(0.670000f,0.924562f,1.035522f,-0.005851f),
            new Vector4(0.720000f,0.912071f,1.027201f,-0.004146f),
            new Vector4(0.770000f,0.953721f,1.013866f,0.002719f),
            new Vector4(0.820000f,0.970487f,1.007982f,0.007829f),
            new Vector4(0.870000f,0.991852f,1.005432f,0.009197f),
            new Vector4(0.907500f,1.046844f,1.024193f,0.005488f),
            new Vector4(0.932500f,1.188661f,1.066403f,-0.000005f),
            new Vector4(0.957500f,1.326461f,1.197358f,-0.011695f),
            new Vector4(0.980000f,1.330000f,1.170000f,-0.045000f),
            new Vector4(1.100000f,1.000000f,1.000000f,-0.045000f)
        };
        static readonly Vector4[] SkinWidth = {
            new Vector4(0.540000f,1.000000f,0.000000f,0.000000f),
            new Vector4(0.551250f,1.002454f,0.000000f,0.000000f),
            new Vector4(0.570000f,1.004872f,0.000000f,0.000000f),
            new Vector4(0.595000f,1.001500f,0.000000f,0.000000f),
            new Vector4(0.632500f,0.992715f,0.000000f,0.000000f),
            new Vector4(0.670000f,0.995824f,0.000000f,0.000000f),
            new Vector4(0.720000f,0.990292f,0.000000f,0.000000f),
            new Vector4(0.770000f,0.961596f,0.000000f,0.000000f),
            new Vector4(0.820000f,0.937400f,0.000000f,0.000000f),
            new Vector4(0.870000f,0.918147f,0.000000f,0.000000f),
            new Vector4(0.907500f,0.804229f,0.000000f,0.000000f),
            new Vector4(0.960000f,0.800000f,0.000000f,0.000000f)
        };

        static readonly Vector3[] BandInner = {
            new Vector3(-0.199006f,0.793165f,0.038880f),
            new Vector3(-0.189586f,0.849590f,0.042368f),
            new Vector3(-0.163080f,0.903386f,0.048510f),
            new Vector3(-0.118371f,0.945492f,0.056720f),
            new Vector3(-0.060879f,0.967859f,0.062085f),
            new Vector3(0.000000f,0.974840f,0.064375f),
            new Vector3(0.060937f,0.968073f,0.062095f),
            new Vector3(0.118926f,0.946431f,0.056865f),
            new Vector3(0.161639f,0.901986f,0.049865f),
            new Vector3(0.187788f,0.848598f,0.044798f),
            new Vector3(0.202319f,0.793967f,0.041036f)
        };
        static readonly Vector3[] BandOuter = {
            new Vector3(-0.242682f,0.803736f,0.035654f),
            new Vector3(-0.233090f,0.873590f,0.040355f),
            new Vector3(-0.198082f,0.937380f,0.048422f),
            new Vector3(-0.146604f,0.993312f,0.057716f),
            new Vector3(-0.073963f,1.015755f,0.063818f),
            new Vector3(0.000000f,1.019724f,0.066419f),
            new Vector3(0.072304f,1.009683f,0.063763f),
            new Vector3(0.146014f,0.992312f,0.058009f),
            new Vector3(0.200151f,0.939389f,0.049576f),
            new Vector3(0.236214f,0.875314f,0.042072f),
            new Vector3(0.242043f,0.803582f,0.037793f)
        };
        public static Vector3 Headband(float t,bool outer) {
            var points=outer?BandOuter:BandInner;float f=t*(points.Length-1);int i=Mathf.Min(points.Length-2,(int)f);float u=f-i;
            Vector3 a=points[i],b=points[i+1],m0=(b-points[Mathf.Max(0,i-1)])*.5f,m1=(points[Mathf.Min(points.Length-1,i+2)]-a)*.5f;
            return (2*u*u*u-3*u*u+1)*a+(u*u*u-2*u*u+u)*m0+(-2*u*u*u+3*u*u)*b+(u*u*u-u*u)*m1;
        }
        static readonly Dictionary<int,Bounds> SourceBounds=new Dictionary<int,Bounds>();
        static Vector3 AccessoryPoint(Vector3 p,int region) {
            Bounds source=SourceBounds[region];
            if(region==5) {
                // Measured front apron outline; keep the separate raised whale
                // and cloth depth together, excluding rear UV islands.
                return FishRebuiltModel.FitApronToGuides(p,source);
            }
            if(region==6||region==7) {
                float side=region==6?1:-1;
                Vector3 shoeDelta=p-source.center;
                return new Vector3(side*.07087f,.09959f,.02357f)+
                    Vector3.Scale(shoeDelta,new Vector3(.09140f/source.size.x,.05842f/source.size.y,.21196f/source.size.z));
            }
            float sign=region==3?1:-1;
            Vector3 center=new Vector3(sign*.2572f,.7913f,.0297f),size=new Vector3(.062f,.048f,.077f);
            Vector3 q=p-source.center;
            return center+Vector3.Scale(q,new Vector3(size.x/source.size.x,size.y/source.size.y,size.z/source.size.z));
        }
        static Vector4 Sample(Vector4[] rows,float y) {
            if(y<=rows[0].x)return rows[0];if(y>=rows[rows.Length-1].x)return rows[rows.Length-1];
            int i=0;while(y>rows[i+1].x)i++;
            float t=(y-rows[i].x)/(rows[i+1].x-rows[i].x);
            Vector4 a=rows[i],b=rows[i+1];float h=b.x-a.x;
            Vector4 m0=i==0?(b-a)/h:(b-rows[i-1])/(b.x-rows[i-1].x);
            Vector4 m1=i+2==rows.Length?(b-a)/h:(rows[i+2]-a)/(rows[i+2].x-a.x);
            Vector4 value=(2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*h*m0+(-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*h*m1;
            for(int k=1;k<4;k++)value[k]=Mathf.Clamp(value[k],Mathf.Min(a[k],b[k]),Mathf.Max(a[k],b[k]));
            return value;
        }
        public static Vector3 Point(Vector3 p,int region) {
            if(region>=3)return AccessoryPoint(p,region);
            if(region==1) {p.x*=Sample(SkinWidth,p.y).y;return p;}
            if(region==2) {
                Vector4 g=Sample(HairEnvelope,p.y);float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.53f,.595f,p.y));
                p.x*=Mathf.Lerp(1,g.y,blend);p.z=Mathf.Lerp(p.z,p.z*g.z+g.w,blend);
                p.y+=.008f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,.98f,p.y));return p;
            }
            return p;
        }
        public static void Apply(Mesh mesh,int[] regions) {
            var old=mesh.vertices;var mapped=new Vector3[old.Length];var normals=mesh.normals;
            SourceBounds.Clear();
            for(int i=0;i<old.Length;i++)if(regions[i]>=3) {
                Bounds b;if(!SourceBounds.TryGetValue(regions[i],out b))b=new Bounds(old[i],Vector3.zero);
                b.Encapsulate(old[i]);SourceBounds[regions[i]]=b;
            }
            const float e=.00005f;
            for(int i=0;i<old.Length;i++) {
                int region=regions[i];Vector3 p=old[i];mapped[i]=Point(p,region);if(region==0)continue;
                Vector3 x=(Point(p+Vector3.right*e,region)-Point(p-Vector3.right*e,region))/(2*e);
                Vector3 y=(Point(p+Vector3.up*e,region)-Point(p-Vector3.up*e,region))/(2*e);
                Vector3 z=(Point(p+Vector3.forward*e,region)-Point(p-Vector3.forward*e,region))/(2*e);
                Vector3 n=normals[i];normals[i]=(Vector3.Cross(y,z)*n.x+Vector3.Cross(z,x)*n.y+Vector3.Cross(x,y)*n.z).normalized;
            }
            var shapes=new List<Tuple<string,float,Vector3[]>>();
            for(int s=0;s<mesh.blendShapeCount;s++) {
                var delta=new Vector3[old.Length];mesh.GetBlendShapeFrameVertices(s,0,delta,null,null);
                for(int i=0;i<old.Length;i++)delta[i]=Point(old[i]+delta[i],regions[i])-mapped[i];
                shapes.Add(Tuple.Create(mesh.GetBlendShapeName(s),mesh.GetBlendShapeFrameWeight(s,0),delta));
            }
            mesh.ClearBlendShapes();mesh.vertices=mapped;mesh.normals=normals;
            foreach(var s in shapes)mesh.AddBlendShapeFrame(s.Item1,s.Item2,s.Item3,new Vector3[old.Length],new Vector3[old.Length]);
            mesh.RecalculateBounds();
        }
    }
}
