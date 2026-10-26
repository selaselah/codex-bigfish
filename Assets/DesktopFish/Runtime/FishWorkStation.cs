using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesktopFish
{
    // Transient work props are separate from the saved avatar meshes/rig/atlas.
    // Crank and hand use the same phase and contact point, so they cannot drift.
    public sealed class FishWorkStation : IDisposable
    {
        public static readonly Vector3 CrankCenter = new Vector3(.120f, .435f, .176f);
        public static readonly Vector3 BraceWrist = new Vector3(-.045f, .478f, .176f);
        public const float CrankRadius = .024f;
        public static readonly Vector3 CookerPort = new Vector3(.046f, .038f, -.012f);
        const int CableSteps = 18, SteamPuffs = 9, PuffSides = 12;
        public static float CrankAngle(float time) => time * 4.4f;
        public static Vector3 CrankGrip(float time) => CrankCenter +
            new Vector3(Mathf.Cos(CrankAngle(time)) * CrankRadius, Mathf.Sin(CrankAngle(time)) * CrankRadius, .009f);

        static readonly Color Navy = new Color(.15f, .23f, .36f);
        static readonly Color Teal = new Color(.27f, .65f, .69f);
        static readonly Color Ivory = new Color(.95f, .96f, .92f);
        static readonly Color Gold = new Color(1f, .76f, .27f);
        readonly Transform root, crank;
        readonly Material solid, vapor;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Pot> pots = new List<Pot>();
        readonly Transform current;
        bool disposed;
        public int CookerCount => pots.Count;
        public bool Visible => root && root.gameObject.activeSelf;
        public Transform Root => root;

        sealed class Pot
        {
            public Transform root, wobble, lid, spark;
            public Mesh steam, cable;
            public Vector3[] steamVertices = new Vector3[SteamPuffs * (1 + 2 * PuffSides)];
            public Color[] steamColors = new Color[SteamPuffs * (1 + 2 * PuffSides)];
            public Vector3[] cableVertices = new Vector3[CableSteps * 4];
            public Vector3 cableStart, cableEnd;
        }

        public FishWorkStation(Transform model, Shader shader)
        {
            if (!shader) shader = Shader.Find("DesktopFish/WorkProps");
            if (!shader) throw new InvalidOperationException("Work-prop shader is missing from the player.");
            solid = new Material(shader) { name = "Generator and rice cookers" };
            vapor = new Material(shader) { name = "Rice-cooker steam", renderQueue = 3000 };
            vapor.SetFloat("_Unlit", 1);
            vapor.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            vapor.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            vapor.SetFloat("_ZWrite", 0);
            root = Child(model, "Work station (runtime only)");
            var housing = new Shape();
            housing.Box(new Vector3(.030f, .420f, .125f), new Vector3(.170f, .105f, .100f), Teal);
            housing.Box(new Vector3(.030f, .418f, .178f), new Vector3(.142f, .077f, .006f), Ivory);
            housing.Box(new Vector3(.030f, .365f, .125f), new Vector3(.190f, .014f, .115f), Navy);
            housing.Box(new Vector3(.090f, .435f, .176f), new Vector3(.060f, .022f, .022f), Navy);
            // Folded lightning emblem and a small charge lamp make the source
            // recognizable without text labels at desktop-pet size.
            housing.Polygon(new[] { new Vector3(.009f,.453f,.183f), new Vector3(-.013f,.424f,.183f),
                new Vector3(.002f,.424f,.183f), new Vector3(-.007f,.395f,.183f),
                new Vector3(.022f,.430f,.183f), new Vector3(.008f,.430f,.183f) }, Gold);
            Part(root, "Generator housing", housing);
            var lamp = new Shape(); lamp.Lathe(new[] { new Vector2(.006f,-.003f), new Vector2(.006f,.003f) }, 8, Gold);
            current = Part(root, "Charge light", lamp); current.localPosition = new Vector3(.058f,.439f,.186f);
            current.localRotation = Quaternion.Euler(90,0,0);
            crank = Child(root, "Hand crank"); crank.localPosition = CrankCenter;
            var arm = new Shape();
            arm.Box(new Vector3(CrankRadius*.5f,0,0),new Vector3(CrankRadius+.009f,.009f,.010f),Navy);
            Part(crank,"Crank arm",arm);
            var grip = new Shape(); grip.Lathe(new[] { new Vector2(.011f,-.013f), new Vector2(.011f,.013f) },8,Gold);
            var knob=Part(crank,"Crank grip",grip);knob.localPosition=new Vector3(CrankRadius,0,.009f);knob.localRotation=Quaternion.Euler(90,0,0);
            root.gameObject.SetActive(false);
        }

        public void Tick(float time, float weight, int activeCount)
        {
            if (disposed) return;
            bool visible = weight > .015f;
            root.gameObject.SetActive(visible);
            if (!visible) return;
            int count = Mathf.Max(0, activeCount);
            if (count != pots.Count) Arrange(count);
            crank.localRotation = Quaternion.Euler(0,0,CrankAngle(time)*Mathf.Rad2Deg);
            current.localScale = Vector3.one * (1 + Mathf.Sin(time*4.4f)*.10f);
            for(int i=0;i<pots.Count;i++)
            {
                var p=pots[i];float seed=i*1.73f;
                // Each cooker has its own boiling rhythm. Rock the cooker above
                // its fixed floor and let the lid briefly release the pressure.
                p.wobble.localPosition=new Vector3(Mathf.Sin(time*26+seed)*.0015f,
                    Mathf.Abs(Mathf.Sin(time*17+seed))*.0014f,0)*weight;
                p.wobble.localRotation=Quaternion.Euler(0,0,(Mathf.Sin(time*24+seed)*1.8f+Mathf.Sin(time*39+seed)*.6f)*weight);
                float pressure=Mathf.Pow(Mathf.Max(0,Mathf.Sin(time*9+seed)),4)*weight;
                p.lid.localPosition=Vector3.up*(pressure*.0045f);
                p.lid.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*21+seed)*pressure*1.7f);
                UpdateCable(p);
                float phase=Mathf.Repeat(time*.55f-i*.19f,1);
                p.spark.localPosition=p.root.InverseTransformPoint(root.TransformPoint(Cable(p.cableStart,p.cableEnd,phase)));
                for(int puff=0;puff<SteamPuffs;puff++)
                {
                    float life=Mathf.Repeat(time*.52f+puff/9f+seed*.13f,1);
                    float x=(puff%3-1)*.014f+Mathf.Sin(life*5+seed+puff)*(.003f+.005f*life);
                    Vector3 center=new Vector3(x,.098f+life*.080f+pressure*.003f,.032f);
                    float radius=.0045f+.0085f*life;
                    float alpha=Mathf.Sin(life*Mathf.PI)*.67f*weight;
                    int n=puff*(1+2*PuffSides);p.steamVertices[n]=center;
                    p.steamColors[n]=new Color(.92f,.97f,1,alpha);
                    for(int ring=0;ring<2;ring++)for(int j=0;j<PuffSides;j++)
                    {
                        float a=j*Mathf.PI*2/PuffSides;
                        float r=radius*(ring==0?1:1.5f)*(1+.11f*Mathf.Sin(a*3+life*3));
                        int v=n+1+ring*PuffSides+j;
                        p.steamVertices[v]=center+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);
                        p.steamColors[v]=ring==0?new Color(.64f,.83f,.90f,alpha*.60f):new Color(.64f,.83f,.90f,0);
                    }
                }
                p.steam.vertices=p.steamVertices;p.steam.colors=p.steamColors;p.steam.RecalculateBounds();
            }
        }

        void Arrange(int count)
        {
            // One physical pot and one connected cable for every active job.
            // Additional rows scale together; no jobs are silently omitted.
            while(pots.Count>count)
            {
                var p=pots[pots.Count-1];
                p.root.gameObject.SetActive(false);
                foreach(var f in p.root.GetComponentsInChildren<MeshFilter>(true)) { meshes.Remove(f.sharedMesh); Release(f.sharedMesh); }
                Release(p.root.gameObject);pots.RemoveAt(pots.Count-1);
            }
            while(pots.Count<count) pots.Add(CreatePot(pots.Count));
            int columns=Mathf.Min(count<=6?3:4,Mathf.Max(1,count)),rows=Mathf.CeilToInt(count/(float)columns);
            // Make a few cookers substantially larger; only shrink when their
            // combined footprint needs to fit the available width or height.
            float size=Mathf.Min(1.75f,.90f/(.137f+(columns-1)*.155f),.26f/(.10f+Mathf.Max(0,rows-1)*.085f));
            for(int i=0;i<count;i++)
            {
                int row=i/columns,column=i%columns,inRow=Mathf.Min(columns,count-row*columns);
                var p=pots[i];
                p.root.localPosition=new Vector3((column-(inRow-1)*.5f)*.155f*size,.085f+row*.085f*size,.280f-row*.025f);
                p.root.localScale=Vector3.one*size;
                p.cableStart=new Vector3(.063f,.365f,.186f);
                UpdateCable(p);
                // Electric pulse travels in station coordinates, independent of pot scaling.
                p.spark.localScale=Vector3.one/size;
            }
        }

        void UpdateCable(Pot p)
        {
            // Keep the actual plug connected while the cooker rocks. The cable
            // mesh and its buffers are reused, rather than rebuilt each frame.
            p.cableEnd=root.InverseTransformPoint(p.wobble.TransformPoint(CookerPort));
            for(int j=0;j<CableSteps;j++)
            {
                Vector3 a=Cable(p.cableStart,p.cableEnd,j/(float)CableSteps),b=Cable(p.cableStart,p.cableEnd,(j+1f)/CableSteps);
                Vector3 normal=Vector3.Cross(b-a,Vector3.forward).normalized*.0022f;int n=j*4;
                p.cableVertices[n]=p.root.InverseTransformPoint(root.TransformPoint(a-normal));
                p.cableVertices[n+1]=p.root.InverseTransformPoint(root.TransformPoint(a+normal));
                p.cableVertices[n+2]=p.root.InverseTransformPoint(root.TransformPoint(b+normal));
                p.cableVertices[n+3]=p.root.InverseTransformPoint(root.TransformPoint(b-normal));
            }
            p.cable.vertices=p.cableVertices;p.cable.RecalculateBounds();
        }

        static Vector3 Cable(Vector3 start, Vector3 end, float t)
        {
            Vector3 a=start+new Vector3(0,-.075f,.06f),b=end+new Vector3(.045f,-.040f,.045f);
            float u=1-t;return u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end;
        }

        Pot CreatePot(int index)
        {
            var p=new Pot {root=Child(root,"Rice cooker "+(index+1))};
            p.wobble=Child(p.root,"Cooker wobble");
            var body=new Shape();
            body.Lathe(new[] {new Vector2(.035f,0),new Vector2(.046f,.006f),new Vector2(.053f,.019f),
                new Vector2(.055f,.041f),new Vector2(.053f,.057f),new Vector2(.048f,.068f)},20,Ivory);
            foreach(float sign in new[]{-1f,1f})
            {
                var loop=new Vector3[16];for(int j=0;j<loop.Length;j++){float a=j*Mathf.PI*2/loop.Length;loop[j]=new Vector3(sign*.057f+Mathf.Cos(a)*.011f,.038f+Mathf.Sin(a)*.009f,0);}
                body.Tube(loop,.0032f,6,Teal,true);
            }
            Part(p.wobble,"Round cartoon cooker",body);
            var lid=new Shape();lid.Lathe(new[] {new Vector2(.047f,.068f),new Vector2(.054f,.073f),new Vector2(.052f,.079f),new Vector2(.040f,.085f),new Vector2(.020f,.089f)},20,Teal);
            lid.Lathe(new[] {new Vector2(.006f,.090f),new Vector2(.012f,.093f),new Vector2(.011f,.097f),new Vector2(.004f,.100f)},12,Navy);
            p.lid=Part(p.wobble,"Bouncing lid",lid);
            var face=new Shape();
            foreach(float sign in new[]{-1f,1f})
            {
                face.FaceOval(new Vector2(sign*.015f,.044f),new Vector2(.0033f,.0058f),Navy);
                face.FaceOval(new Vector2(sign*.015f-.0009f,.046f),new Vector2(.0011f,.0016f),Ivory,.0015f);
                face.FaceOval(new Vector2(sign*.026f,.033f),new Vector2(.0055f,.0028f),new Color(1,.68f,.66f));
            }
            for(int j=0;j<12;j++)
            {
                float x0=Mathf.Lerp(-.009f,.009f,j/12f),x1=Mathf.Lerp(-.009f,.009f,(j+1)/12f);
                float y0=.026f+.005f*Mathf.Pow(x0/.009f,2),y1=.026f+.005f*Mathf.Pow(x1/.009f,2);
                face.Quad(FacePoint(x0,y0-.0008f),FacePoint(x0,y0+.0008f),FacePoint(x1,y1+.0008f),FacePoint(x1,y1-.0008f),Navy);
            }
            face.FaceOval(new Vector2(0,.013f),new Vector2(.012f,.005f),Teal);
            face.FaceOval(new Vector2(0,.013f),new Vector2(.006f,.0028f),Gold,.0017f);
            Part(p.wobble,"Smiling face and heating light",face);
            var wire=Child(p.root,"Power cable");p.cable=Own(new Mesh{name="Connected power cable"});p.cable.MarkDynamic();
            p.cable.vertices=p.cableVertices;var cableColors=new Color[p.cableVertices.Length];var cableNormals=new Vector3[cableColors.Length];var cableTriangles=new List<int>();
            for(int j=0;j<cableColors.Length;j++){cableColors[j]=Navy;cableNormals[j]=Vector3.forward;}
            for(int j=0;j<CableSteps;j++){int n=j*4;cableTriangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            p.cable.colors=cableColors;p.cable.normals=cableNormals;p.cable.triangles=cableTriangles.ToArray();
            wire.gameObject.AddComponent<MeshFilter>().sharedMesh=p.cable;wire.gameObject.AddComponent<MeshRenderer>().sharedMaterial=solid;
            var spark=new Shape();spark.Box(Vector3.zero,new Vector3(.005f,.010f,.005f),Gold);
            p.spark=Part(p.root,"Electric pulse",spark);
            // Steam has its own transparent material and no opaque shadow quads.
            p.steam=Own(new Mesh {name="Rising steam"});p.steam.MarkDynamic();
            p.steam.vertices=p.steamVertices;p.steam.colors=p.steamColors;
            var triangles=new List<int>();
            for(int s=0;s<SteamPuffs;s++)for(int j=0;j<PuffSides;j++)
            {
                int n=s*(1+2*PuffSides),next=(j+1)%PuffSides;
                triangles.AddRange(new[]{n,n+1+j,n+1+next,n+1+j,n+1+PuffSides+j,n+1+PuffSides+next,n+1+j,n+1+PuffSides+next,n+1+next});
            }
            p.steam.triangles=triangles.ToArray();var normals=new Vector3[p.steamVertices.Length];for(int j=0;j<normals.Length;j++)normals[j]=Vector3.forward;p.steam.normals=normals;
            var steam=Child(p.wobble,"Steam");steam.gameObject.AddComponent<MeshFilter>().sharedMesh=p.steam;steam.gameObject.AddComponent<MeshRenderer>().sharedMaterial=vapor;
            return p;
        }

        static Vector3 FacePoint(float x,float y,float offset=.001f)
        {
            // The expression follows the rounded front rather than floating on
            // a flat sign. Keep it just above the faceted 20-column body surface.
            float r=y<.019f?Mathf.Lerp(.046f,.053f,Mathf.InverseLerp(.006f,.019f,y)):
                y<.041f?Mathf.Lerp(.053f,.055f,Mathf.InverseLerp(.019f,.041f,y)):
                Mathf.Lerp(.055f,.053f,Mathf.InverseLerp(.041f,.057f,y));
            return new Vector3(x,y,Mathf.Sqrt(Mathf.Max(0,r*r-x*x))+offset);
        }

        Transform Part(Transform parent,string name,Shape shape)
        {
            var t=Child(parent,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Own(shape.Make(name));
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=solid;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return t;
        }
        Mesh Own(Mesh mesh){meshes.Add(mesh);return mesh;}
        static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static void Release(UnityEngine.Object value){if(!value)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void Dispose(){if(disposed)return;disposed=true;if(root)Release(root.gameObject);foreach(var mesh in meshes)Release(mesh);meshes.Clear();Release(solid);Release(vapor);}

        sealed class Shape
        {
            readonly List<Vector3> points=new List<Vector3>();readonly List<int> triangles=new List<int>();readonly List<Color> colors=new List<Color>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){int n=points.Count;points.AddRange(new[]{a,b,c,d});for(int i=0;i<4;i++)colors.Add(color);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Box(Vector3 center,Vector3 size,Color color)
            {
                Vector3 a=size*.5f;Vector3[] v={new Vector3(-a.x,-a.y,-a.z),new Vector3(a.x,-a.y,-a.z),new Vector3(a.x,a.y,-a.z),new Vector3(-a.x,a.y,-a.z),new Vector3(-a.x,-a.y,a.z),new Vector3(a.x,-a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(-a.x,a.y,a.z)};
                int[] ids={0,3,2,1,4,5,6,7,0,4,7,3,1,2,6,5,3,7,6,2,0,1,5,4};
                for(int i=0;i<24;i+=4)Quad(center+v[ids[i]],center+v[ids[i+1]],center+v[ids[i+2]],center+v[ids[i+3]],color);
            }
            public void Polygon(Vector3[] p,Color color){int n=points.Count;points.AddRange(p);foreach(var unused in p)colors.Add(color);for(int i=1;i<p.Length-1;i++)triangles.AddRange(new[]{n,n+i,n+i+1});}
            public void FaceOval(Vector2 center,Vector2 radius,Color color,float offset=.001f)
            {
                const int steps=16;int n=points.Count;points.Add(FacePoint(center.x,center.y,offset));colors.Add(color);
                for(int j=0;j<steps;j++){float a=j*Mathf.PI*2/steps;points.Add(FacePoint(center.x+Mathf.Cos(a)*radius.x,center.y+Mathf.Sin(a)*radius.y,offset));colors.Add(color);}
                for(int j=0;j<steps;j++)triangles.AddRange(new[]{n,n+1+j,n+1+(j+1)%steps});
            }
            public void Tube(Vector3[] path,float radius,int sides,Color color,bool closed)
            {
                int n=points.Count;
                for(int i=0;i<path.Length;i++)
                {
                    Vector3 tangent=(path[(i+1)%path.Length]-path[(i+path.Length-1)%path.Length]).normalized;
                    Vector3 normal=Vector3.Cross(tangent,Vector3.forward).normalized;
                    for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;points.Add(path[i]+radius*(normal*Mathf.Cos(a)+Vector3.forward*Mathf.Sin(a)));colors.Add(color);}
                }
                for(int i=0;i<(closed?path.Length:path.Length-1);i++)for(int j=0;j<sides;j++)
                {int a=n+i*sides+j,b=n+i*sides+(j+1)%sides,c=n+(i+1)%path.Length*sides+(j+1)%sides,d=n+(i+1)%path.Length*sides+j;triangles.AddRange(new[]{a,b,c,a,c,d});}
            }
            public void Lathe(Vector2[] profile,int sides,Color color)
            {
                int n=points.Count;
                foreach(var p in profile)for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;points.Add(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x));colors.Add(color);}
                for(int row=0;row<profile.Length-1;row++)for(int j=0;j<sides;j++){int a=n+row*sides+j,b=n+row*sides+(j+1)%sides,c=b+sides,d=a+sides;triangles.AddRange(new[]{a,d,c,a,c,b});}
                for(int end=0;end<2;end++)
                {
                    int row=end==0?0:profile.Length-1;int mid=points.Count;points.Add(new Vector3(0,profile[row].y,0));colors.Add(color);
                    for(int j=0;j<sides;j++){int a=n+row*sides+j,b=n+row*sides+(j+1)%sides;if(profile[row].x<=0)continue;triangles.AddRange(end==0?new[]{mid,a,b}:new[]{mid,b,a});}
                }
            }
            public Mesh Make(string name){var mesh=new Mesh {name=name};mesh.SetVertices(points);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        }
    }
}
