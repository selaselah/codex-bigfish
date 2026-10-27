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
        // Floor equipment has its own frame. The machine rocks within it;
        // hands, crank and plug use exactly the same moving machine pose.
        public static readonly Vector3 GeneratorPosition = new Vector3(-.401f, .295f, .030f);
        public static readonly Quaternion GeneratorRotation = Quaternion.Euler(0,-36,0);
        public static readonly Vector3 BraceWrist = new Vector3(.125f,.425f,.105f);
        public const float CrankRadius = .110f;
        public static readonly Vector3 CookerPort = new Vector3(.046f,.038f,-.012f);
        public enum CookerStyle { ClassicWhite, RetroGold, CoralMetal, ModernCream, SlateBlue }
        public const int CookerStyleCount = 5;
        const int CableSteps = 18, SteamPuffs = 9, PuffSides = 12;
        const float StageRevealWeight=.20f;
        public static readonly Vector3 LocalCrankCenter=new Vector3(.135f,.127f,-.030f);
        static readonly Vector3 LocalPowerOutlet=new Vector3(-.040f,-.145f,.146f);
        static readonly Vector3[] Feet={new Vector3(-.075f,-.228f,-.055f),new Vector3(-.075f,-.228f,.055f),new Vector3(.075f,-.228f,-.055f),new Vector3(.075f,-.228f,.055f)};
        static readonly Vector3 FootSize=new Vector3(.033f,.024f,.036f);
        static readonly Vector3[] FootSupports=MakeFootSupports();
        public const float GeneratorFloorHeight=.055f;
        public static Vector3 GeneratorPoint(Vector3 local) => GeneratorPosition + GeneratorRotation * local;
        public static Vector3 CrankCenter => GeneratorPoint(LocalCrankCenter);
        public static Vector3 PowerOutlet => GeneratorPoint(LocalPowerOutlet);
        public static float CrankAngle(float time) => Mathf.Repeat(time*4.4f,Mathf.PI*2);
        public const float GripCenter = .135f;
        public const float HandSpacing = .190f;
        static Vector3 LocalGrip(float time) => LocalCrankCenter+new Vector3(GripCenter,Mathf.Cos(CrankAngle(time))*CrankRadius,Mathf.Sin(CrankAngle(time))*CrankRadius);
        static void AddFeet(Shape shape,Color color){foreach(var foot in Feet)shape.RoundedBlock(foot,FootSize,.006f,color);}
        static Vector3[] MakeFootSupports(){var shape=new Shape();AddFeet(shape,Color.white);return shape.Positions();}
        public static Pose GeneratorPoseAt(float time,float weight=1) {
            float phase=CrankAngle(time);
            Quaternion rotation=GeneratorRotation*Quaternion.Euler(new Vector3(3*Mathf.Sin(phase)+.35f*Mathf.Sin(phase*2),0,1.6f*Mathf.Cos(phase))*Mathf.Clamp01(weight));
            float lowest=float.PositiveInfinity;
            foreach(var support in FootSupports)lowest=Mathf.Min(lowest,(rotation*support).y);
            return new Pose(GeneratorPosition+Vector3.up*(GeneratorFloorHeight-GeneratorPosition.y-lowest),rotation);
        }
        public static Vector3 HandGrip(float time,string side,float weight=1) {
            var pose=GeneratorPoseAt(time,weight);
            return pose.position+pose.rotation*(LocalGrip(time)+Vector3.right*(side=="L"?-HandSpacing*.5f:HandSpacing*.5f));
        }
        public static Vector3 CrankGrip(float time,float weight=1) {
            var pose=GeneratorPoseAt(time,weight);return pose.position+pose.rotation*LocalGrip(time);
        }
        public static Vector3 GripRockOffset(float time,float weight) => CrankGrip(time,weight)-GeneratorPoint(LocalGrip(time));

        // The canvas is1.30x larger than the original240x320 window. Matching
        // that padding in all states keeps the original idle pixels per world
        // unit, without zooming the character out when work begins.
        public static void FrameWorkingCamera(Camera camera,float size,Vector3 position,float weight)
        {
            float framing=Mathf.SmoothStep(0,1,Mathf.Clamp01(weight/StageRevealWeight));
            camera.orthographicSize=size*1.30f;
            camera.transform.position=position+new Vector3(.015f,0,0)*framing;
        }

        static readonly Color Navy = new Color(.15f, .23f, .36f);
        static readonly Color Teal = new Color(.27f, .65f, .69f);
        static readonly Color Ivory = new Color(.95f, .96f, .92f);
        static readonly Color Gold = new Color(1f, .76f, .27f);
        readonly Transform root, machine, crank, gripHandle;
        readonly Material solid, vapor, electricity;
        readonly Transform[] chargeCells=new Transform[5];
        const int ElectricArcs=8,ElectricNodes=7,ElectricBands=6;
        readonly Mesh electricMesh;
        readonly Vector3[] electricVertices=new Vector3[ElectricArcs*ElectricNodes*ElectricBands];
        readonly Color[] electricColors=new Color[ElectricArcs*ElectricNodes*ElectricBands];
        static readonly Vector3[] ArcStart={new Vector3(-.126f,.210f,.123f),new Vector3(.126f,.210f,.123f),new Vector3(-.126f,-.025f,.123f),new Vector3(.126f,-.025f,.123f),new Vector3(-.09f,.236f,.105f),new Vector3(-.09f,-.244f,.105f),new Vector3(-.079f,.264f,.020f),new Vector3(.040f,.316f,.020f)};
        static readonly Vector3[] ArcEnd={new Vector3(-.126f,.065f,.123f),new Vector3(.126f,.065f,.123f),new Vector3(-.126f,-.190f,.123f),new Vector3(.126f,-.190f,.123f),new Vector3(.09f,.236f,.105f),new Vector3(.09f,-.244f,.105f),new Vector3(-.040f,.316f,.020f),new Vector3(.079f,.264f,.020f)};
        static readonly float[] ElectricWidth={-1,-.50f,-.22f,.22f,.50f,1};
        static readonly float[] ElectricAlpha={0,.32f,1,1,.32f,0};
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Pot> pots = new List<Pot>();
        readonly Transform current;
        readonly System.Random styleRandom;
        readonly int[] styleOrder = new int[CookerStyleCount];
        int nextStyle = CookerStyleCount, lastStyle = -1;
        bool disposed;
        public int CookerCount => pots.Count;
        public bool Visible => root && root.gameObject.activeSelf;
        public Transform Root => root;
        public CookerStyle CookerStyleAt(int index) => pots[index].style;

        sealed class Pot
        {
            public Transform root, wobble, lid, spark;
            public Mesh steam, cable;
            public Vector3[] steamVertices = new Vector3[SteamPuffs * (1 + 2 * PuffSides)];
            public Color[] steamColors = new Color[SteamPuffs * (1 + 2 * PuffSides)];
            public Vector3[] cableVertices = new Vector3[CableSteps * 4];
            public Vector3 cableStart, cableEnd;
            public CookerStyle style;
        }

        public FishWorkStation(Transform model, Shader shader, int? cookerSeed = null)
        {
            // Keep appearance randomness separate from the mascot's blink and
            // gesture RNG. A seed is optional for repeatable editor previews.
            styleRandom = cookerSeed.HasValue ? new System.Random(cookerSeed.Value) : new System.Random();
            if (!shader) shader = Shader.Find("DesktopFish/WorkProps");
            if (!shader) throw new InvalidOperationException("Work-prop shader is missing from the player.");
            solid = new Material(shader) { name = "Generator and rice cookers" };
            vapor = new Material(shader) { name = "Rice-cooker steam", renderQueue = 3000 };
            vapor.SetFloat("_Unlit", 1);
            vapor.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            vapor.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            vapor.SetFloat("_ZWrite", 0);
            electricity=new Material(vapor){name="Flowing generator electricity",renderQueue=3001};
            // A sibling keeps the generator and pots on the floor while the
            // avatar turns toward the side-mounted crank.
            root = Child(model.parent, "Work station (runtime only)");
            root.localPosition=model.localPosition;root.localRotation=model.localRotation;root.localScale=model.localScale;
            machine=Child(root,"Floor generator");
            machine.localPosition=GeneratorPosition;machine.localRotation=GeneratorRotation;
            Color dark=new Color(.16f,.19f,.21f),metal=new Color(.62f,.67f,.69f);
            var housing=new Shape();
            housing.RoundedBlock(Vector3.zero,new Vector3(.220f,.440f,.180f),.012f,Ivory);
            housing.RoundedBlock(new Vector3(0,0,.094f),new Vector3(.198f,.416f,.014f),.009f,new Color(.97f,.97f,.93f));
            AddFeet(housing,dark);
            // Rigid bridge carry handle, with an actual open center.
            housing.RoundedBlock(new Vector3(-.064f,.248f,0),new Vector3(.025f,.078f,.030f),.006f,dark);
            housing.RoundedBlock(new Vector3(.064f,.248f,0),new Vector3(.025f,.078f,.030f),.006f,dark);
            housing.RoundedBlock(new Vector3(0,.287f,0),new Vector3(.148f,.026f,.030f),.006f,dark);
            // Raised charge meter, round power badge, screws and AC outlets.
            housing.RoundedBlock(new Vector3(0,.120f,.107f),new Vector3(.116f,.071f,.013f),.006f,dark);
            housing.Box(new Vector3(0,.120f,.115f),new Vector3(.091f,.045f,.004f),new Color(.25f,.39f,.38f));
            for(int i=0;i<5;i++) {
                Vector3 position=new Vector3(-.034f+i*.0165f,.119f,.119f);
                housing.Box(position,new Vector3(.010f,.024f,.002f),new Color(.12f,.20f,.18f));
                var cell=new Shape();cell.Box(Vector3.zero,new Vector3(.009f,.023f,.002f),new Color(.62f,.96f,.38f));
                chargeCells[i]=Part(machine,"Charge cell "+i,cell);chargeCells[i].localPosition=position+Vector3.forward*.0015f;
            }
            housing.RoundedBlock(new Vector3(0,.177f,.104f),new Vector3(.092f,.021f,.004f),.003f,Gold);
            var rim=new Shape();rim.Lathe(new[]{new Vector2(.078f,-.002f),new Vector2(.078f,.002f)},40,Gold);
            var rimPart=Part(machine,"Round power badge rim",rim);rimPart.localPosition=new Vector3(0,-.030f,.111f);rimPart.localRotation=Quaternion.Euler(90,0,0);
            var badge=new Shape();badge.Lathe(new[]{new Vector2(.074f,-.002f),new Vector2(.074f,.002f)},40,new Color(.065f,.078f,.09f));
            var badgePart=Part(machine,"Black round power badge",badge);badgePart.localPosition=new Vector3(0,-.030f,.115f);badgePart.localRotation=Quaternion.Euler(90,0,0);
            var mark=new Shape();
            mark.Polygon(new[]{new Vector3(.013f,.027f,.120f),new Vector3(-.024f,-.012f,.120f),new Vector3(.003f,-.012f,.120f)},Gold);
            mark.Polygon(new[]{new Vector3(-.013f,-.055f,.120f),new Vector3(.025f,-.002f,.120f),new Vector3(-.004f,-.002f,.120f)},Gold);
            mark.Box(new Vector3(-.0005f,-.007f,.120f),new Vector3(.007f,.010f,.001f),Gold);
            var bolt=Part(machine,"Large yellow power bolt",mark);bolt.localPosition=Vector3.down*.016f;
            for(int i=0;i<4;i++)housing.Box(new Vector3(-.112f,.060f-i*.022f,0),new Vector3(.004f,.008f,.060f),dark);
            Part(machine,"Generator housing",housing);
            foreach(float x in new[]{-.086f,.086f})foreach(float y in new[]{-.190f,.190f}) {
                var screw=new Shape();screw.Lathe(new[]{new Vector2(.006f,-.002f),new Vector2(.006f,.002f)},10,metal);
                var o=Part(machine,"Case screw",screw);o.localPosition=new Vector3(x,y,.104f);o.localRotation=Quaternion.Euler(90,0,0);
            }
            for(int socketIndex=0;socketIndex<2;socketIndex++) {
                float x=socketIndex==0?-.040f:.040f;var socket=new Shape();
                socket.RoundedBlock(Vector3.zero,new Vector3(.066f,.060f,.010f),.006f,dark);
                socket.RoundedBlock(new Vector3(0,0,.007f),new Vector3(.055f,.049f,.006f),.004f,new Color(.85f,.87f,.84f));
                var socketPart=Part(machine,"AC socket "+(socketIndex+1),socket);socketPart.localPosition=new Vector3(x,-.145f,.109f);
                for(int slot=0;slot<3;slot++) {
                    var hole=new Shape();hole.RoundedBlock(Vector3.zero,new Vector3(.0055f,slot==0?.013f:.019f,.002f),.001f,new Color(.06f,.08f,.10f));
                    var holePart=Part(socketPart,"Socket slot "+slot,hole);holePart.localPosition=new Vector3(slot==0?0:slot==1?-.012f:.012f,slot==0?.013f:-.006f,.011f);holePart.localRotation=Quaternion.Euler(0,0,slot==0?0:slot==1?-28:28);
                }
            }
            var plug=new Shape();plug.RoundedBlock(Vector3.zero,new Vector3(.035f,.033f,.018f),.006f,Navy);
            var plugPart=Part(machine,"Mains plug",plug);plugPart.localPosition=new Vector3(-.040f,-.145f,.132f);
            var strain=new Shape();strain.Lathe(new[]{new Vector2(.006f,-.005f),new Vector2(.005f,.005f)},10,Navy);
            var outlet=Part(plugPart,"Cable outlet",strain);outlet.localPosition=new Vector3(0,0,.014f);outlet.localRotation=Quaternion.Euler(90,0,0);
            electricMesh=Own(new Mesh{name="Soft generator lightning ribbons"});electricMesh.MarkDynamic();electricMesh.vertices=electricVertices;electricMesh.colors=electricColors;
            var electricNormals=new Vector3[electricVertices.Length];for(int i=0;i<electricNormals.Length;i++)electricNormals[i]=Vector3.forward;electricMesh.normals=electricNormals;
            var lightningTriangles=new List<int>();
            for(int arc=0;arc<ElectricArcs;arc++)for(int node=0;node<ElectricNodes-1;node++)for(int band=0;band<ElectricBands-1;band++) {
                int a=(arc*ElectricNodes+node)*ElectricBands+band,b=a+1,c=b+ElectricBands,d=a+ElectricBands;lightningTriangles.AddRange(new[]{a,b,c,a,c,d});
            }
            electricMesh.triangles=lightningTriangles.ToArray();
            var electricPart=Child(machine,"Generator electricity");electricPart.gameObject.AddComponent<MeshFilter>().sharedMesh=electricMesh;electricPart.gameObject.AddComponent<MeshRenderer>().sharedMaterial=electricity;
            var axle=new Shape();axle.Lathe(new[]{new Vector2(.012f,-.018f),new Vector2(.012f,.018f)},12,metal);
            var shaft=Part(machine,"Side shaft",axle);shaft.localPosition=new Vector3(.122f,.127f,-.030f);shaft.localRotation=Quaternion.Euler(0,0,90);
            var lamp=new Shape();lamp.Lathe(new[]{new Vector2(.005f,-.002f),new Vector2(.005f,.002f)},10,Gold);
            current=Part(machine,"Charge light",lamp);current.localPosition=new Vector3(.078f,.122f,.105f);current.localRotation=Quaternion.Euler(90,0,0);
            crank=Child(root,"Hand crank");crank.localPosition=CrankCenter;
            var arm=new Shape();arm.RoundedBlock(new Vector3(0,CrankRadius*.5f,0),new Vector3(.014f,CrankRadius+.019f,.017f),.003f,metal);
            Part(crank,"Crank arm",arm);
            var grip=new Shape();grip.Lathe(new[]{new Vector2(.009f,-.135f),new Vector2(.0105f,-.123f),new Vector2(.0105f,.123f),new Vector2(.009f,.135f)},12,dark);
            var knob=Part(crank,"Crank grip",grip);knob.localPosition=new Vector3(GripCenter,CrankRadius,0);knob.localRotation=Quaternion.Euler(0,0,90);gripHandle=knob;
            root.gameObject.SetActive(false);
        }

        public void Tick(float time, float weight, int activeCount)
        {
            if (disposed) return;
            bool visible = weight > StageRevealWeight;
            root.gameObject.SetActive(visible);
            if (!visible) return;
            int count = Mathf.Max(0, activeCount);
            if (count != pots.Count) Arrange(count);
            var pose=GeneratorPoseAt(time,weight);
            machine.localPosition=pose.position;machine.localRotation=pose.rotation;
            crank.localPosition=pose.position+pose.rotation*LocalCrankCenter;
            crank.localRotation = pose.rotation * Quaternion.Euler(CrankAngle(time)*Mathf.Rad2Deg,0,0);
            Vector3 liveOutlet=pose.position+pose.rotation*LocalPowerOutlet;
            // The grip has a free bearing; it does not drag the fist around the shaft.
            gripHandle.localRotation=Quaternion.Euler(-CrankAngle(time)*Mathf.Rad2Deg,0,0)*Quaternion.Euler(0,0,90);
            current.localScale = Vector3.one * (1 + Mathf.Sin(CrankAngle(time))*.10f);
            // One charge/discharge cycle spans two crank revolutions.
            float charge=Mathf.PingPong(time*4.4f/(2*Mathf.PI),1);
            int filled=Mathf.Clamp(Mathf.FloorToInt(charge*6),0,5);
            for(int i=0;i<chargeCells.Length;i++)chargeCells[i].gameObject.SetActive(i>=chargeCells.Length-filled);
            UpdateElectricity(time,weight);
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
                p.cableStart=liveOutlet;
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

        void UpdateElectricity(float time,float weight) {
            // Reuse one mesh: soft blue halos, narrow luminous cores and a
            // travelling highlight. No full-frame flashes or post-process.
            for(int arc=0;arc<ElectricArcs;arc++) {
                Vector3 along=ArcEnd[arc]-ArcStart[arc],normal=Vector3.Cross(along,Vector3.forward).normalized;
                float pulse=.45f+.55f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(time*3+arc*1.9f)),2);
                float travel=Mathf.Repeat(time*.60f+arc*.17f,1);
                for(int node=0;node<ElectricNodes;node++) {
                    float t=node/(float)(ElectricNodes-1);
                    float zigzag=Mathf.Sin(t*Mathf.PI)*(.005f*(node%2==0?1:-1)+.003f*Mathf.Sin(time*7+arc*1.7f+node*2.2f));
                    Vector3 center=ArcStart[arc]+along*t+normal*zigzag;
                    float glow=.45f+.55f*Mathf.Exp(-Mathf.Pow((t-travel)*4,2));
                    for(int band=0;band<ElectricBands;band++) {
                        int index=(arc*ElectricNodes+node)*ElectricBands+band;
                        electricVertices[index]=center+normal*(ElectricWidth[band]*.010f);
                        Color core=arc>=6?new Color(1,.91f,.47f):new Color(.18f,.67f,1);
                        Color color=Color.Lerp(new Color(.08f,.43f,1),core,1-Mathf.Abs(ElectricWidth[band]));
                        color.a=ElectricAlpha[band]*pulse*glow*.88f*weight;electricColors[index]=color;
                    }
                }
            }
            electricMesh.vertices=electricVertices;electricMesh.colors=electricColors;electricMesh.RecalculateBounds();
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
                p.root.localPosition=new Vector3((column-(inRow-1)*.5f)*.155f*size,.085f+row*.085f*size,.430f-row*.025f);
                p.root.localScale=Vector3.one*size;
                p.cableStart=PowerOutlet;
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
            Vector3 a=start+new Vector3(-.045f,-.065f,.080f),b=end+new Vector3(.045f,-.025f,.045f);
            float u=1-t;return u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end;
        }

        Pot CreatePot(int index)
        {
            var p=new Pot {root=Child(root,"Rice cooker "+(index+1)),style=PickStyle()};
            p.wobble=Child(p.root,"Cooker wobble");
            BuildCooker(p);
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

        CookerStyle PickStyle()
        {
            if(nextStyle>=CookerStyleCount)
            {
                for(int i=0;i<CookerStyleCount;i++)styleOrder[i]=i;
                for(int i=CookerStyleCount-1;i>0;i--){int j=styleRandom.Next(i+1),v=styleOrder[i];styleOrder[i]=styleOrder[j];styleOrder[j]=v;}
                if(styleOrder[0]==lastStyle){int v=styleOrder[0];styleOrder[0]=styleOrder[1];styleOrder[1]=v;}
                nextStyle=0;
            }
            lastStyle=styleOrder[nextStyle++];return (CookerStyle)lastStyle;
        }

        void BuildCooker(Pot p)
        {
            Color charcoal=new Color(.20f,.23f,.26f),silver=new Color(.72f,.77f,.79f),red=new Color(.91f,.25f,.23f),green=new Color(.35f,.68f,.39f);
            Vector2[] profile;Color paint,trim;float exponent=2,depth=1;
            switch(p.style)
            {
                case CookerStyle.RetroGold:
                    profile=new[]{new Vector2(.035f,.004f),new Vector2(.050f,.010f),new Vector2(.055f,.035f),new Vector2(.054f,.060f),new Vector2(.047f,.073f)};
                    paint=new Color(.89f,.67f,.32f);trim=new Color(.52f,.35f,.19f);break;
                case CookerStyle.CoralMetal:
                    profile=new[]{new Vector2(.033f,.004f),new Vector2(.042f,.010f),new Vector2(.048f,.045f),new Vector2(.052f,.071f)};
                    paint=new Color(.92f,.31f,.41f);trim=charcoal;break;
                case CookerStyle.ModernCream:
                    profile=new[]{new Vector2(.038f,.005f),new Vector2(.052f,.013f),new Vector2(.055f,.020f),new Vector2(.055f,.062f),new Vector2(.051f,.072f)};
                    paint=new Color(.95f,.94f,.90f);trim=new Color(.27f,.29f,.33f);exponent=3.4f;depth=.87f;break;
                case CookerStyle.SlateBlue:
                    profile=new[]{new Vector2(.039f,.004f),new Vector2(.052f,.015f),new Vector2(.056f,.040f),new Vector2(.052f,.067f),new Vector2(.048f,.073f)};
                    paint=new Color(.32f,.41f,.48f);trim=charcoal;exponent=2.5f;depth=.95f;break;
                default:
                    profile=new[]{new Vector2(.034f,.005f),new Vector2(.046f,.010f),new Vector2(.054f,.022f),new Vector2(.054f,.060f),new Vector2(.049f,.071f)};
                    paint=new Color(.96f,.97f,.96f);trim=charcoal;break;
            }
            var body=new Shape();var lid=new Shape();var panel=new Shape();
            body.Lathe(profile,20,paint,exponent,depth);
            foreach(float x in new[]{-.033f,.033f})foreach(float z in new[]{-.021f,.021f})
                body.Box(new Vector3(x,.003f,z),new Vector3(.013f,.006f,.013f),trim);
            foreach(float sign in new[]{-1f,1f})
            {
                if(p.style==CookerStyle.ModernCream)
                    body.RoundedBlock(new Vector3(sign*.060f,.068f,0),new Vector3(.019f,.012f,.026f),.002f,trim);
                else if(p.style==CookerStyle.RetroGold)
                {
                    body.RoundedBlock(new Vector3(sign*.055f,.066f,0),new Vector3(.012f,.022f,.024f),.003f,new Color(.97f,.95f,.87f));
                    body.RoundedBlock(new Vector3(sign*.058f,.074f,0),new Vector3(.013f,.006f,.025f),.0015f,trim);
                }
                else if(p.style==CookerStyle.CoralMetal)
                {
                    body.RoundedBlock(new Vector3(sign*.057f,.068f,0),new Vector3(.017f,.005f,.016f),.0015f,trim);
                    body.RoundedBlock(new Vector3(sign*.065f,.073f,0),new Vector3(.006f,.015f,.017f),.0015f,trim);
                }
                else
                {
                    float y=p.style==CookerStyle.SlateBlue?.064f:.058f;
                    body.RoundedBlock(new Vector3(sign*.060f,y,0),new Vector3(.024f,.013f,.028f),.003f,trim);
                    body.RoundedBlock(new Vector3(sign*.062f,y+.005f,0),new Vector3(.016f,.003f,.020f),.001f,new Color(.38f,.45f,.48f));
                }
            }
            Func<float,float,float,Vector3> surface=(x,y,offset)=>SurfacePoint(profile,exponent,depth,x,y,offset);
            float controlDepth=0;foreach(var row in profile)controlDepth=Mathf.Max(controlDepth,row.x*depth);
            Func<float,float,float,Vector3> front=(x,y,offset)=>new Vector3(x,y,controlDepth+offset);
            if(p.style==CookerStyle.ModernCream)
            {
                body.Lathe(new[]{new Vector2(.040f,.002f),new Vector2(.055f,.012f),new Vector2(.056f,.020f)},20,trim,exponent,depth);
                lid.Lathe(new[]{new Vector2(.053f,.068f),new Vector2(.057f,.072f),new Vector2(.057f,.083f),new Vector2(.049f,.091f)},20,trim,exponent,depth);
                lid.Lathe(new[]{new Vector2(.047f,.090f),new Vector2(.045f,.094f)},20,new Color(.76f,.87f,.86f),exponent,depth);
                CarryHandle(lid,.094f,.020f,.003f,trim);
                panel.ControlHousingRect(surface,front,new Vector2(0,.036f),new Vector2(.031f,.041f),.006f,new Color(.65f,.67f,.68f));
                panel.SurfaceRect(front,new Vector2(0,.047f),new Vector2(.018f,.008f),.002f,charcoal,.0017f);
                panel.SurfaceRect(front,new Vector2(0,.047f),new Vector2(.010f,.002f),.0007f,new Color(.66f,.87f,.79f),.0023f);
                for(int j=-1;j<=1;j++)panel.SurfaceOval(front,new Vector2(j*.008f,.030f),new Vector2(.0025f,.0025f),Ivory,.0016f);
                panel.SurfaceOval(front,new Vector2(0,.020f),new Vector2(.0034f,.0028f),Gold,.0016f);
            }
            else if(p.style==CookerStyle.RetroGold)
            {
                lid.Lathe(new[]{new Vector2(.047f,.069f),new Vector2(.054f,.074f),new Vector2(.050f,.084f),new Vector2(.028f,.090f)},20,new Color(.97f,.95f,.87f));
                LidKnob(lid,trim,.090f);
                panel.ControlHousingOval(surface,front,new Vector2(0,.036f),new Vector2(.019f,.025f),trim);
                panel.SurfaceOval(front,new Vector2(0,.036f),new Vector2(.016f,.022f),Ivory,.0017f);
                panel.SurfaceOval(front,new Vector2(0,.039f),new Vector2(.011f,.014f),charcoal,.0023f);
                panel.SurfaceOval(front,new Vector2(0,.044f),new Vector2(.006f,.0024f),red,.0029f);
                panel.SurfaceOval(front,new Vector2(0,.036f),new Vector2(.006f,.0024f),green,.0029f);
                panel.SurfaceRect(front,new Vector2(0,.022f),new Vector2(.011f,.004f),.0015f,trim,.0023f);
                foreach(float sign in new[]{-1f,1f})for(int j=0;j<5;j++)
                {float a=j*Mathf.PI*2/5;panel.SurfaceOval(surface,new Vector2(sign*.034f+Mathf.Cos(a)*.0055f,.048f+Mathf.Sin(a)*.0065f),new Vector2(.0025f,.0031f),new Color(.68f,.43f,.17f));}
            }
            else if(p.style==CookerStyle.CoralMetal)
            {
                lid.Lathe(new[]{new Vector2(.052f,.070f),new Vector2(.055f,.075f),new Vector2(.049f,.085f),new Vector2(.022f,.090f)},20,silver);
                LidKnob(lid,charcoal,.090f);
                panel.ControlHousingRect(surface,front,new Vector2(0,.022f),new Vector2(.025f,.032f),.003f,charcoal);
                panel.SurfaceRect(front,new Vector2(0,.022f),new Vector2(.019f,.027f),.002f,silver,.0017f);
                panel.SurfaceRect(front,new Vector2(0,.022f),new Vector2(.0035f,.019f),.0015f,charcoal,.0023f);
                panel.SurfaceOval(front,new Vector2(0,.030f),new Vector2(.0042f,.0042f),charcoal,.0029f);
            }
            else if(p.style==CookerStyle.SlateBlue)
            {
                lid.Lathe(new[]{new Vector2(.048f,.071f),new Vector2(.054f,.076f),new Vector2(.049f,.084f),new Vector2(.025f,.090f)},20,silver,exponent,depth);
                lid.RoundedBlock(new Vector3(0,.092f,0),new Vector3(.029f,.004f,.021f),.0015f,trim);
                lid.RoundedBlock(new Vector3(0,.096f,0),new Vector3(.020f,.008f,.013f),.0025f,trim);
                panel.ControlHousingRect(surface,front,new Vector2(.010f,.032f),new Vector2(.027f,.043f),.003f,charcoal);
                panel.SurfaceRect(front,new Vector2(.010f,.043f),new Vector2(.018f,.004f),.0015f,Gold,.0017f);
                panel.SurfaceRect(front,new Vector2(.010f,.034f),new Vector2(.018f,.004f),.0015f,new Color(.97f,.49f,.18f),.0017f);
                panel.SurfaceRect(front,new Vector2(.010f,.019f),new Vector2(.015f,.010f),.0015f,silver,.0017f);
            }
            else
            {
                lid.Lathe(new[]{new Vector2(.049f,.069f),new Vector2(.055f,.073f),new Vector2(.055f,.078f)},20,trim);
                lid.Lathe(new[]{new Vector2(.050f,.077f),new Vector2(.046f,.085f),new Vector2(.023f,.091f)},20,new Color(.74f,.84f,.88f));
                LidKnob(lid,trim,.090f);
                panel.ControlHousingRect(surface,front,new Vector2(0,.032f),new Vector2(.033f,.045f),.007f,trim);
                panel.SurfaceRect(front,new Vector2(0,.032f),new Vector2(.025f,.037f),.005f,new Color(.80f,.87f,.89f),.0017f);
                panel.SurfaceOval(front,new Vector2(0,.043f),new Vector2(.006f,.0026f),red,.0023f);
                panel.SurfaceOval(front,new Vector2(0,.034f),new Vector2(.006f,.0026f),green,.0023f);
                panel.SurfaceRect(front,new Vector2(0,.021f),new Vector2(.014f,.008f),.002f,trim,.0023f);
            }
            Part(p.wobble,"Cooker shell "+p.style,body);
            p.lid=Part(p.wobble,"Bouncing lid",lid);
            Part(p.wobble,"Controls and indicator lamps",panel);
        }

        static void LidKnob(Shape shape,Color color,float bottom)
        {
            shape.Lathe(new[]{new Vector2(.007f,bottom),new Vector2(.011f,bottom+.003f),new Vector2(.010f,.098f),new Vector2(.005f,.100f)},12,color);
        }

        static void CarryHandle(Shape shape,float y,float width,float height,Color color)
        {
            // A rigid bridge and two supports, rather than an oval wire ring.
            float top=y+height+.005f;
            shape.RoundedBlock(new Vector3(0,top,0),new Vector3(width*2+.006f,.005f,.014f),.002f,color);
            foreach(float sign in new[]{-1f,1f})
                shape.RoundedBlock(new Vector3(sign*width,top-.005f,0),new Vector3(.006f,.014f,.014f),.0015f,color);
        }

        static Vector3 SurfacePoint(Vector2[] profile,float exponent,float depth,float x,float y,float offset)
        {
            float r=profile[profile.Length-1].x;
            for(int i=1;i<profile.Length;i++)if(y<=profile[i].y){r=Mathf.Lerp(profile[i-1].x,profile[i].x,Mathf.InverseLerp(profile[i-1].y,profile[i].y,y));break;}
            float z=Mathf.Pow(Mathf.Max(0,1-Mathf.Pow(Mathf.Abs(x/r),exponent)),1/exponent)*r*depth;
            return new Vector3(x,y,z+offset);
        }

        Transform Part(Transform parent,string name,Shape shape)
        {
            var t=Child(parent,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Own(shape.Make(name));
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=solid;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return t;
        }
        Mesh Own(Mesh mesh){meshes.Add(mesh);return mesh;}
        static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static void Release(UnityEngine.Object value){if(!value)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void Dispose(){if(disposed)return;disposed=true;if(root)Release(root.gameObject);foreach(var mesh in meshes)Release(mesh);meshes.Clear();Release(solid);Release(vapor);Release(electricity);}

        sealed class Shape
        {
            readonly List<Vector3> points=new List<Vector3>();readonly List<int> triangles=new List<int>();readonly List<Color> colors=new List<Color>();
            public Vector3[] Positions()=>points.ToArray();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){int n=points.Count;points.AddRange(new[]{a,b,c,d});for(int i=0;i<4;i++)colors.Add(color);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Box(Vector3 center,Vector3 size,Color color)
            {
                Vector3 a=size*.5f;Vector3[] v={new Vector3(-a.x,-a.y,-a.z),new Vector3(a.x,-a.y,-a.z),new Vector3(a.x,a.y,-a.z),new Vector3(-a.x,a.y,-a.z),new Vector3(-a.x,-a.y,a.z),new Vector3(a.x,-a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(-a.x,a.y,a.z)};
                int[] ids={0,3,2,1,4,5,6,7,0,4,7,3,1,2,6,5,3,7,6,2,0,1,5,4};
                for(int i=0;i<24;i+=4)Quad(center+v[ids[i]],center+v[ids[i+1]],center+v[ids[i+2]],center+v[ids[i+3]],color);
            }
            public void RoundedBlock(Vector3 center,Vector3 size,float bevel,Color color)
            {
                const int sides=8;Vector3 half=size*.5f;
                float r=Mathf.Min(bevel,half.x*.45f,half.y*.45f,half.z*.45f);int n=points.Count;
                float[] x={-half.x,-half.x+r,half.x-r,half.x};
                for(int ring=0;ring<4;ring++)for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides,c=Mathf.Cos(a),s=Mathf.Sin(a),inset=ring==0||ring==3?r:0;
                    points.Add(center+new Vector3(x[ring],Mathf.Sign(c)*Mathf.Sqrt(Mathf.Abs(c))*(half.y-inset),Mathf.Sign(s)*Mathf.Sqrt(Mathf.Abs(s))*(half.z-inset)));colors.Add(color);
                }
                for(int ring=0;ring<3;ring++)for(int j=0;j<sides;j++)
                {int a=n+ring*sides+j,b=n+ring*sides+(j+1)%sides,c=b+sides,d=a+sides;triangles.AddRange(new[]{a,b,c,a,c,d});}
                for(int end=0;end<2;end++)
                {
                    int mid=points.Count;points.Add(center+new Vector3(x[end==0?0:3],0,0));colors.Add(color);
                    for(int j=0;j<sides;j++){int a=n+(end==0?0:3*sides)+j,b=n+(end==0?0:3*sides)+(j+1)%sides;triangles.AddRange(end==0?new[]{mid,b,a}:new[]{mid,a,b});}
                }
            }
            public void Polygon(Vector3[] p,Color color){int n=points.Count;points.AddRange(p);foreach(var unused in p)colors.Add(color);for(int i=1;i<p.Length-1;i++)triangles.AddRange(new[]{n,n+i,n+i+1});}
            public void SurfaceOval(Func<float,float,float,Vector3> point,Vector2 center,Vector2 radius,Color color,float offset=.0012f)
            {
                const int steps=16;int n=points.Count;points.Add(point(center.x,center.y,offset));colors.Add(color);
                for(int j=0;j<steps;j++){float a=j*Mathf.PI*2/steps;points.Add(point(center.x+Mathf.Cos(a)*radius.x,center.y+Mathf.Sin(a)*radius.y,offset));colors.Add(color);}
                for(int j=0;j<steps;j++)triangles.AddRange(new[]{n,n+1+j,n+1+(j+1)%steps});
            }
            public void SurfaceRect(Func<float,float,float,Vector3> point,Vector2 center,Vector2 size,float radius,Color color,float offset=.0012f)
            {
                var boundary=new Vector2[16];
                for(int corner=0;corner<4;corner++)for(int j=0;j<4;j++)
                {float a=(corner*90+j*30)*Mathf.Deg2Rad;float x=center.x+(corner==0||corner==3?1:-1)*(size.x*.5f-radius)+Mathf.Cos(a)*radius;
                    float y=center.y+(corner<2?1:-1)*(size.y*.5f-radius)+Mathf.Sin(a)*radius;boundary[corner*4+j]=new Vector2(x,y);}
                // Long corner-to-corner chords can cut into a rounded shell.
                // Sample the actual surface between the center and perimeter.
                int n=points.Count;points.Add(point(center.x,center.y,offset));colors.Add(color);
                for(int ring=0;ring<2;ring++)for(int j=0;j<16;j++)
                {Vector2 p=Vector2.Lerp(center,boundary[j],ring==0?.5f:1);points.Add(point(p.x,p.y,offset));colors.Add(color);}
                for(int j=0;j<16;j++)
                {int a=n+1+j,b=n+1+(j+1)%16,c=n+17+j,d=n+17+(j+1)%16;triangles.AddRange(new[]{n,a,b,a,c,d,a,d,b});}
            }
            public void ControlHousingRect(Func<float,float,float,Vector3> back,Func<float,float,float,Vector3> front,Vector2 center,Vector2 size,float radius,Color color)
            {
                SurfaceRect(front,center,size,radius,color);var boundary=new Vector2[16];
                for(int corner=0;corner<4;corner++)for(int j=0;j<4;j++)
                {float a=(corner*90+j*30)*Mathf.Deg2Rad;boundary[corner*4+j]=center+new Vector2((corner==0||corner==3?1:-1)*(size.x*.5f-radius)+Mathf.Cos(a)*radius,(corner<2?1:-1)*(size.y*.5f-radius)+Mathf.Sin(a)*radius);}
                HousingWalls(back,front,boundary,color);
            }
            public void ControlHousingOval(Func<float,float,float,Vector3> back,Func<float,float,float,Vector3> front,Vector2 center,Vector2 radius,Color color)
            {
                SurfaceOval(front,center,radius,color);var boundary=new Vector2[16];
                for(int j=0;j<16;j++){float a=j*Mathf.PI*2/16;boundary[j]=center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y);}
                HousingWalls(back,front,boundary,color);
            }
            void HousingWalls(Func<float,float,float,Vector3> back,Func<float,float,float,Vector3> front,Vector2[] boundary,Color color)
            {
                for(int j=0;j<boundary.Length;j++){var a=boundary[j];var b=boundary[(j+1)%boundary.Length];Quad(back(a.x,a.y,.0005f),back(b.x,b.y,.0005f),front(b.x,b.y,.0012f),front(a.x,a.y,.0012f),color);}
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
            public void Lathe(Vector2[] profile,int sides,Color color,float exponent=2,float depth=1)
            {
                int n=points.Count;
                foreach(var p in profile)for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;float c=Mathf.Cos(a),s=Mathf.Sin(a);points.Add(new Vector3(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),2/exponent)*p.x,p.y,Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),2/exponent)*p.x*depth));colors.Add(color);}
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
