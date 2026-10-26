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
            public Transform root, spark;
            public Mesh steam;
            public Vector3[] steamVertices = new Vector3[28];
            public Color[] steamColors = new Color[28];
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
                var p=pots[i];float phase=Mathf.Repeat(time*.55f-i*.19f,1);
                p.spark.localPosition=p.root.InverseTransformPoint(root.TransformPoint(Cable(p.cableStart,p.cableEnd,phase)));
                for(int strand=0;strand<2;strand++) for(int k=0;k<7;k++)
                {
                    float t=k/6f, pulse=Mathf.Repeat(time*.65f+strand*.45f+i*.2f,1);
                    float rise=t*.065f+pulse*.018f;
                    float x=(strand==0?-.013f:.013f)+Mathf.Sin(t*6-time*2.4f+strand)*.006f;
                    float width=Mathf.Max(.00001f,.0035f*Mathf.Sin(Mathf.PI*t));
                    int n=(strand*7+k)*2;
                    p.steamVertices[n]=new Vector3(x-width,.097f+rise,.018f);
                    p.steamVertices[n+1]=new Vector3(x+width,.097f+rise,.018f);
                    float alpha=Mathf.Sin(Mathf.PI*t)*(.58f+.12f*Mathf.Sin(time*2+i));
                    p.steamColors[n]=p.steamColors[n+1]=new Color(.55f,.76f,.81f,alpha);
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
            int columns=Mathf.Min(4,Mathf.Max(1,count)),rows=Mathf.CeilToInt(count/(float)columns);
            float size=Mathf.Min(1,2f/Mathf.Max(1,rows));
            for(int i=0;i<count;i++)
            {
                int row=i/columns,column=i%columns,inRow=Mathf.Min(columns,count-row*columns);
                var p=pots[i];
                p.root.localPosition=new Vector3((column-(inRow-1)*.5f)*.128f*size,.067f+row*.115f*size,.265f+row*.012f);
                p.root.localScale=Vector3.one*size;
                p.cableStart=new Vector3(.063f,.365f,.186f);
                p.cableEnd=p.root.localPosition+new Vector3(.046f,.038f,-.012f)*size;
                // Cable belongs to this pot but is authored in station space.
                var wire=p.root.Find("Power cable");
                wire.localPosition=Vector3.zero;wire.localScale=Vector3.one;
                var shape=new Shape();const int steps=18;
                for(int j=0;j<steps;j++)
                {
                    Vector3 a=Cable(p.cableStart,p.cableEnd,j/(float)steps),b=Cable(p.cableStart,p.cableEnd,(j+1f)/steps);
                    Vector3 normal=Vector3.Cross(b-a,Vector3.forward).normalized*.0022f;
                    shape.Quad(p.root.InverseTransformPoint(root.TransformPoint(a-normal)),p.root.InverseTransformPoint(root.TransformPoint(a+normal)),
                        p.root.InverseTransformPoint(root.TransformPoint(b+normal)),p.root.InverseTransformPoint(root.TransformPoint(b-normal)),Navy);
                }
                var f=wire.GetComponent<MeshFilter>();if(f.sharedMesh){meshes.Remove(f.sharedMesh);Release(f.sharedMesh);}f.sharedMesh=Own(shape.Make("Connected power cable"));
                // Electric pulse travels in station coordinates, independent of pot scaling.
                p.spark.localScale=Vector3.one/size;
            }
        }

        static Vector3 Cable(Vector3 start, Vector3 end, float t)
        {
            Vector3 a=start+new Vector3(0,-.075f,.06f),b=end+new Vector3(.045f,-.040f,.045f);
            float u=1-t;return u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end;
        }

        Pot CreatePot(int index)
        {
            var p=new Pot {root=Child(root,"Rice cooker "+(index+1))};
            var body=new Shape();
            body.Lathe(new[] {new Vector2(.039f,0),new Vector2(.053f,.012f),new Vector2(.055f,.057f),new Vector2(.048f,.072f)},12,Ivory);
            body.Lathe(new[] {new Vector2(.049f,.070f),new Vector2(.054f,.075f),new Vector2(.046f,.083f),new Vector2(.018f,.088f)},12,Teal);
            body.Box(new Vector3(0,.093f,0),new Vector3(.026f,.011f,.017f),Navy);
            body.Box(new Vector3(0,.033f,.054f),new Vector3(.031f,.024f,.004f),Navy);
            body.Box(new Vector3(0,.036f,.057f),new Vector3(.018f,.009f,.003f),new Color(.62f,.88f,.80f));
            body.Box(new Vector3(.060f,.042f,0),new Vector3(.017f,.013f,.026f),Teal);
            body.Box(new Vector3(-.060f,.042f,0),new Vector3(.017f,.013f,.026f),Teal);
            Part(p.root,"Rounded rice cooker, lid and controls",body);
            var wire=Child(p.root,"Power cable");wire.gameObject.AddComponent<MeshFilter>();wire.gameObject.AddComponent<MeshRenderer>().sharedMaterial=solid;
            var spark=new Shape();spark.Box(Vector3.zero,new Vector3(.005f,.010f,.005f),Gold);
            p.spark=Part(p.root,"Electric pulse",spark);
            // Steam has its own transparent material and no opaque shadow quads.
            p.steam=Own(new Mesh {name="Rising steam"});p.steam.MarkDynamic();
            p.steam.vertices=p.steamVertices;p.steam.colors=p.steamColors;
            var triangles=new List<int>();
            for(int s=0;s<2;s++)for(int j=0;j<6;j++){int n=(s*7+j)*2;triangles.AddRange(new[]{n,n+1,n+3,n,n+3,n+2});}
            p.steam.triangles=triangles.ToArray();var normals=new Vector3[28];for(int j=0;j<28;j++)normals[j]=Vector3.forward;p.steam.normals=normals;
            var steam=Child(p.root,"Steam");steam.gameObject.AddComponent<MeshFilter>().sharedMesh=p.steam;steam.gameObject.AddComponent<MeshRenderer>().sharedMaterial=vapor;
            return p;
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
