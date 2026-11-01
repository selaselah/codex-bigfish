using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DesktopFish {
    // Complete own sideburn domains and curved tuft in the fixed 5K body.
    // The enclosing side-strand recipe verifies the neutral position/index SHA.
    internal sealed class FishFrontHairFollow {
        public readonly int[] Groups;
        public readonly float[] TuftWeight;
        public readonly Vector3[] TuftGradient;
        readonly float[] top=new float[6],span=new float[6];
        readonly Vector3 restTip;
        readonly int[] tuft;
        Matrix4x4 previousHead;
        Vector3 tip,velocity;
        float previousTime;
        bool initialized;
        public Vector3 TuftDelta {get;private set;}
        public FishFrontHairFollow(Mesh mesh) {
            var p=mesh.vertices;var t=mesh.triangles;
            Groups=Enumerable.Repeat(-1,p.Length).ToArray();TuftWeight=new float[p.Length];TuftGradient=new Vector3[p.Length];
            int[] first={804,847,876,1531,1577,1622,2819},count={43,24,65,46,45,54,69};
            for(int g=0;g<7;g++)for(int i=first[g];i<first[g]+count[g];i++)Groups[i]=g;
            for(int g=0;g<6;g++){var indices=Enumerable.Range(first[g],count[g]).ToArray();top[g]=Mathf.Min(.80f,indices.Max(i=>p[i].y));span[g]=top[g]-indices.Min(i=>p[i].y);}
            tuft=Enumerable.Range(first[6],count[6]).ToArray();
            var adjacency=tuft.ToDictionary(i=>i,i=>new List<int>());int faces=0;
            for(int f=0;f<t.Length;f+=3)if(Groups[t[f]]==6&&Groups[t[f+1]]==6&&Groups[t[f+2]]==6) {
                faces++;for(int j=0;j<3;j++){int a=t[f+j],b=t[f+(j+1)%3];adjacency[a].Add(b);adjacency[b].Add(a);}
            }
            if(faces!=132)throw new InvalidOperationException("Curved tuft domain changed.");
            float rootY=tuft.Min(i=>p[i].y)+.008f;var distance=tuft.ToDictionary(i=>i,i=>p[i].y<=rootY?0:float.PositiveInfinity);var done=new HashSet<int>();
            while(done.Count<tuft.Length) {
                int i=tuft.Where(j=>!done.Contains(j)).OrderBy(j=>distance[j]).First();
                if(float.IsInfinity(distance[i]))throw new InvalidOperationException("Disconnected tuft parameterization.");done.Add(i);
                foreach(int j in adjacency[i])distance[j]=Mathf.Min(distance[j],distance[i]+Vector3.Distance(p[i],p[j]));
            }
            float length=distance.Values.Max();int end=tuft.OrderByDescending(i=>distance[i]).First();restTip=p[end];
            foreach(int i in tuft)TuftWeight[i]=Mathf.SmoothStep(0,1,distance[i]/length);
            var area=new float[p.Length];
            for(int f=0;f<t.Length;f+=3)if(Groups[t[f]]==6&&Groups[t[f+1]]==6&&Groups[t[f+2]]==6) {
                int a=t[f],b=t[f+1],c=t[f+2];Vector3 e1=p[b]-p[a],e2=p[c]-p[a],n=Vector3.Cross(e1,e2);
                if(n.sqrMagnitude<1e-18f)throw new InvalidOperationException("Degenerate tuft parameter cell.");
                Vector3 gradient=((TuftWeight[b]-TuftWeight[a])*Vector3.Cross(e2,n)+(TuftWeight[c]-TuftWeight[a])*Vector3.Cross(n,e1))/n.sqrMagnitude;
                float weight=n.magnitude;foreach(int i in new[]{a,b,c}){TuftGradient[i]+=gradient*weight;area[i]+=weight;}
            }
            foreach(int i in tuft)TuftGradient[i]=TuftWeight[i]==0?Vector3.zero:TuftGradient[i]/area[i];
        }
        public float Progress(int vertex,float y)=>Mathf.Clamp01((top[Groups[vertex]]-y)/span[Groups[vertex]]);
        public float Span(int vertex)=>span[Groups[vertex]];
        public float FreeWeight(int vertex,Vector3 p)=>Groups[vertex]<0?0:Groups[vertex]==6?TuftWeight[vertex]:Mathf.SmoothStep(0,1,Progress(vertex,p.y));
        public static Vector3 SoftLimit(Vector3 v,float limit)=>v*(limit/Mathf.Sqrt(limit*limit+v.sqrMagnitude));
        public static void SoftLimit(ref Vector3 value,ref Vector3 derivative,float limit) {
            float a=limit*limit+value.sqrMagnitude,s=limit/Mathf.Sqrt(a);
            derivative=s*(derivative-value*(Vector3.Dot(value,derivative)/a));value*=s;
        }
        public void Tick(float time,Matrix4x4 head,Vector3 common) {
            float elapsed=time-previousTime;Vector3 target=head.MultiplyPoint3x4(restTip);
            bool reset=!initialized||elapsed<0||elapsed>.15f||(target-previousHead.MultiplyPoint3x4(restTip)).magnitude>.30f;
            if(reset){tip=target;velocity=Vector3.zero;}
            else if(elapsed>0) {
                int steps=Mathf.CeilToInt(elapsed/(1f/120));float dt=elapsed/steps;
                for(int i=1;i<=steps;i++) {
                    Vector3 goal=Vector3.Lerp(previousHead.MultiplyPoint3x4(restTip),target,i/(float)steps);
                    velocity+=(goal-tip)*150*dt;velocity*=Mathf.Exp(-7.5f*dt);tip+=velocity*dt;
                }
            }
            Vector3 delta=SoftLimit(common*.8f+head.inverse.MultiplyVector(tip-target)*.2f,.025f);
            float scale=1;foreach(int i in tuft){float d=Vector3.Dot(delta,TuftGradient[i]);if(d<-.65f)scale=Mathf.Min(scale,-.65f/d);}
            TuftDelta=delta*scale;previousHead=head;previousTime=time;initialized=true;
        }
    }
}
