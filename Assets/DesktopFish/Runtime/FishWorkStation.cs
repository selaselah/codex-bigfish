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
        const int CableSteps = 32, SteamPuffs = 9, PuffSides = 12;
        const float StageRevealWeight=.20f;
        public static readonly Vector3 LocalCrankCenter=new Vector3(.135f,.127f,-.030f);
        static readonly Vector3 LocalPowerOutlet=new Vector3(-.040f,-.145f,.146f);
        static readonly Vector3[] Feet={new Vector3(-.075f,-.228f,-.055f),new Vector3(-.075f,-.228f,.055f),new Vector3(.075f,-.228f,-.055f),new Vector3(.075f,-.228f,.055f)};
        static readonly Vector3 FootSize=new Vector3(.033f,.024f,.036f);
        public static readonly Vector3 MachinePivot=new Vector3(0,-.216f,0);
        public const float GeneratorFloorHeight=.055f;
        public static Vector3 GeneratorPoint(Vector3 local) => GeneratorPosition + GeneratorRotation * local;
        public static Vector3 CrankCenter => GeneratorPoint(LocalCrankCenter);
        public static Vector3 PowerOutlet => GeneratorPoint(LocalPowerOutlet);
        public static float CrankAngle(float time) => Mathf.Repeat(time*4.4f,Mathf.PI*2);
        public const float GripCenter = .135f;
        public const float HandSpacing = .190f;
        static Vector3 LocalGrip(float time) => LocalCrankCenter+new Vector3(GripCenter,Mathf.Cos(CrankAngle(time))*CrankRadius,Mathf.Sin(CrankAngle(time))*CrankRadius);
        static void AddFeet(Shape shape,Color color){foreach(var foot in Feet)shape.RoundedBlock(foot,FootSize,.006f,color);}
        public static Pose GeneratorPoseAt(float time,float weight=1) {
            float phase=CrankAngle(time);
            Quaternion rotation=GeneratorRotation*Quaternion.Euler(new Vector3(3*Mathf.Sin(phase)+.35f*Mathf.Sin(phase*2),0,1.6f*Mathf.Cos(phase))*Mathf.Clamp01(weight));
            // The upper housing pivots above a separate stationary footing.
            // Short mounts bridge the small gap while all four pads stay fixed.
            return new Pose(GeneratorPoint(MachinePivot)-rotation*MachinePivot,rotation);
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
        static readonly Color SupplyWhite=new Color(.94f,.95f,.93f);
        const int SupplySides=8;
        static readonly Color Teal = new Color(.27f, .65f, .69f);
        static readonly Color Ivory = new Color(.95f, .96f, .92f);
        static readonly Color Gold = new Color(1f, .76f, .27f);
        readonly Transform root, machine, footing, crank, gripHandle;
        readonly Transform[] footMounts=new Transform[4],lowerFootJoints=new Transform[4],upperFootJoints=new Transform[4];
        readonly Material solid, vapor, electricity;
        readonly Transform[] chargeCells=new Transform[5];
        const int ElectricArcs=8,ElectricPaths=ElectricArcs*3,ElectricNodes=7,ElectricBands=6,SparksPerArc=3;
        readonly Mesh electricMesh,sparkMesh;
        readonly Vector3[] sparkVertices=new Vector3[ElectricArcs*SparksPerArc*4];
        readonly Color[] sparkColors=new Color[ElectricArcs*SparksPerArc*4];
        readonly Vector3[] electricPath=new Vector3[ElectricNodes],branchPath=new Vector3[ElectricNodes];
        readonly Vector3[] electricVertices=new Vector3[ElectricPaths*ElectricNodes*ElectricBands];
        readonly Color[] electricColors=new Color[ElectricPaths*ElectricNodes*ElectricBands];
        static readonly Vector3[] ArcStart={new Vector3(-.126f,.210f,.123f),new Vector3(.126f,.210f,.123f),new Vector3(-.126f,-.025f,.123f),new Vector3(.126f,-.025f,.123f),new Vector3(-.09f,.236f,.105f),new Vector3(-.09f,-.244f,.105f),new Vector3(-.079f,.264f,.020f),new Vector3(.040f,.316f,.020f)};
        static readonly Vector3[] ArcEnd={new Vector3(-.126f,.065f,.123f),new Vector3(.126f,.065f,.123f),new Vector3(-.126f,-.190f,.123f),new Vector3(.126f,-.190f,.123f),new Vector3(.09f,.236f,.105f),new Vector3(.09f,-.244f,.105f),new Vector3(-.040f,.316f,.020f),new Vector3(.079f,.264f,.020f)};
        static readonly float[] ElectricWidth={-1,-.65f,-.17f,.17f,.65f,1};
        static readonly float[] ElectricAlpha={0,.72f,1,1,.72f,0};
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Pot> pots = new List<Pot>();
        readonly List<Supply> supplies=new List<Supply>();
        readonly Transform[] generatorPlugs=new Transform[2],generatorOutlets=new Transform[2];
        readonly bool persistPowerUpgrades;
        const string PowerUpgradeKey="RebuiltBigFish.PowerStripUpgradeMask.v1";
        int powerUpgradeMask;
        readonly Transform current;
        readonly System.Random styleRandom;
        readonly int[] styleOrder = new int[CookerStyleCount];
        int nextStyle = CookerStyleCount, lastStyle = -1;
        bool disposed,workingShown;
        public int CookerCount => pots.Count;
        public Transform CookerSourceAt(int index) => pots[index].source;
        public int PowerUpgradeMask => powerUpgradeMask;
        public int SupplyCount => supplies.Count;
        public int SupplyCapacityAt(int index) => supplies[index].capacity;
        public Transform SupplyAt(int index) => supplies[index].root;
        public Transform SupplyFeedSourceAt(int index) => supplies[index].feedSource;
        public bool Visible => root && root.gameObject.activeSelf;
        public bool WorkingVisible => machine&&machine.gameObject.activeInHierarchy;
        public Transform Root => root;
        public CookerStyle CookerStyleAt(int index) => pots[index].style;

        sealed class Pot
        {
            public Transform root, wobble, lid, spark, source;
            public Mesh steam, cable;
            public Vector3[] steamVertices = new Vector3[SteamPuffs * (1 + 2 * PuffSides)];
            public Color[] steamColors = new Color[SteamPuffs * (1 + 2 * PuffSides)];
            public Vector3[] cableVertices = new Vector3[CableSteps * 4];
            public Vector3 cableStart, cableEnd;
            public CookerStyle style;
            public float cableSeed;
        }

        sealed class Supply {
            public Transform root,geometry,inlet,wire,feedSource;
            public Transform[] plugs,outlets;
            public Mesh cable;
            public Vector3[] vertices=new Vector3[(CableSteps+1)*SupplySides],normals=new Vector3[(CableSteps+1)*SupplySides],points=new Vector3[CableSteps+1];
            public int capacity;
            public float cableSeed;
            public bool present;
        }

        public FishWorkStation(Transform model, Shader shader, int? cookerSeed = null,int initialPowerUpgradeMask=-1)
        {
            // Keep appearance randomness separate from the mascot's blink and
            // gesture RNG. A seed is optional for repeatable editor previews.
            styleRandom = cookerSeed.HasValue ? new System.Random(cookerSeed.Value) : new System.Random();
            persistPowerUpgrades=!Application.isEditor&&!cookerSeed.HasValue&&initialPowerUpgradeMask<0;
            powerUpgradeMask=initialPowerUpgradeMask>=0?initialPowerUpgradeMask:(persistPowerUpgrades?PlayerPrefs.GetInt(PowerUpgradeKey,0):0);
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
            footing=Child(root,"Generator base");footing.localPosition=GeneratorPosition;footing.localRotation=GeneratorRotation;
            var pads=new Shape();AddFeet(pads,dark);Part(footing,"Fixed generator feet",pads);
            for(int i=0;i<Feet.Length;i++) {
                lowerFootJoints[i]=Child(footing,"Fixed foot joint "+i);lowerFootJoints[i].localPosition=Feet[i]+Vector3.up*.005f;
                upperFootJoints[i]=Child(machine,"Body foot joint "+i);upperFootJoints[i].localPosition=new Vector3(Feet[i].x,-.214f,Feet[i].z);
                var neck=new Shape();neck.Lathe(new[]{new Vector2(.0105f,-.5f),new Vector2(.0105f,.5f)},12,dark);
                footMounts[i]=Part(footing,"Flexible foot mount "+i,neck);
            }
            var housing=new Shape();
            housing.RoundedBlock(Vector3.zero,new Vector3(.220f,.440f,.180f),.012f,Ivory);
            housing.RoundedBlock(new Vector3(0,0,.094f),new Vector3(.198f,.416f,.014f),.009f,new Color(.97f,.97f,.93f));
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
            for(int i=0;i<2;i++) {
                var plug=new Shape();plug.RoundedBlock(Vector3.zero,new Vector3(.035f,.033f,.018f),.006f,Navy);
                generatorPlugs[i]=Part(machine,"Generator plug "+i,plug);generatorPlugs[i].localPosition=new Vector3(i==0?-.040f:.040f,-.145f,.132f);
                var strain=new Shape();strain.Lathe(new[]{new Vector2(.006f,-.005f),new Vector2(.005f,.005f)},10,Navy);
                generatorOutlets[i]=Part(generatorPlugs[i],"Cable outlet",strain);generatorOutlets[i].localPosition=new Vector3(0,0,.014f);generatorOutlets[i].localRotation=Quaternion.Euler(90,0,0);
            }
            electricMesh=Own(new Mesh{name="Crackling branched lightning"});electricMesh.MarkDynamic();electricMesh.vertices=electricVertices;electricMesh.colors=electricColors;
            var electricNormals=new Vector3[electricVertices.Length];for(int i=0;i<electricNormals.Length;i++)electricNormals[i]=Vector3.forward;electricMesh.normals=electricNormals;
            var lightningTriangles=new List<int>();
            for(int arc=0;arc<ElectricPaths;arc++)for(int node=0;node<ElectricNodes-1;node++)for(int band=0;band<ElectricBands-1;band++) {
                int a=(arc*ElectricNodes+node)*ElectricBands+band,b=a+1,c=b+ElectricBands,d=a+ElectricBands;lightningTriangles.AddRange(new[]{a,b,c,a,c,d});
            }
            electricMesh.triangles=lightningTriangles.ToArray();
            var electricPart=Child(machine,"Generator electricity");electricPart.gameObject.AddComponent<MeshFilter>().sharedMesh=electricMesh;electricPart.gameObject.AddComponent<MeshRenderer>().sharedMaterial=electricity;
            sparkMesh=Own(new Mesh{name="Flying electric sparks"});sparkMesh.MarkDynamic();sparkMesh.vertices=sparkVertices;sparkMesh.colors=sparkColors;
            var sparkNormals=new Vector3[sparkVertices.Length];for(int i=0;i<sparkNormals.Length;i++)sparkNormals[i]=Vector3.forward;sparkMesh.normals=sparkNormals;
            var sparkTriangles=new List<int>();for(int i=0;i<ElectricArcs*SparksPerArc;i++){int n=i*4;sparkTriangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}sparkMesh.triangles=sparkTriangles.ToArray();
            var sparkPart=Child(machine,"Generator crackle sparks");sparkPart.gameObject.AddComponent<MeshFilter>().sharedMesh=sparkMesh;sparkPart.gameObject.AddComponent<MeshRenderer>().sharedMaterial=electricity;
            var axle=new Shape();axle.Lathe(new[]{new Vector2(.012f,-.018f),new Vector2(.012f,.018f)},12,metal);
            var shaft=Part(machine,"Side shaft",axle);shaft.localPosition=new Vector3(.122f,.127f,-.030f);shaft.localRotation=Quaternion.Euler(0,0,90);
            var lamp=new Shape();lamp.Lathe(new[]{new Vector2(.005f,-.002f),new Vector2(.005f,.002f)},10,Gold);
            current=Part(machine,"Charge light",lamp);current.localPosition=new Vector3(.078f,.122f,.105f);current.localRotation=Quaternion.Euler(90,0,0);
            crank=Child(root,"Hand crank");crank.localPosition=CrankCenter;
            var arm=new Shape();arm.RoundedBlock(new Vector3(0,CrankRadius*.5f,0),new Vector3(.014f,CrankRadius+.019f,.017f),.003f,metal);
            Part(crank,"Crank arm",arm);
            var grip=new Shape();grip.Lathe(new[]{new Vector2(.009f,-.135f),new Vector2(.0105f,-.123f),new Vector2(.0105f,.123f),new Vector2(.009f,.135f)},12,dark);
            var knob=Part(crank,"Crank grip",grip);knob.localPosition=new Vector3(GripCenter,CrankRadius,0);knob.localRotation=Quaternion.Euler(0,0,90);gripHandle=knob;
            for(int i=0;i<2;i++)supplies.Add(CreateSupply(i,(powerUpgradeMask&(1<<i))!=0?(i==0?8:10):(i==0?4:5)));
            root.gameObject.SetActive(false);
        }

        public void Tick(float time, float weight, int activeCount)
        {
            if (disposed) return;
            bool working=weight>StageRevealWeight,wasWorking=workingShown;workingShown=working;
            root.gameObject.SetActive(working||powerUpgradeMask!=0);
            machine.gameObject.SetActive(working);footing.gameObject.SetActive(working);crank.gameObject.SetActive(working);
            foreach(var p in pots)p.root.gameObject.SetActive(working);
            if(!working) {
                if(activeCount==0&&pots.Count>0)Arrange(0);
                for(int i=0;i<supplies.Count;i++) {
                    var supply=supplies[i];supply.present=false;
                    supply.root.gameObject.SetActive(i<2&&(powerUpgradeMask&(1<<i))!=0);supply.wire.gameObject.SetActive(false);
                    foreach(var plug in supply.plugs)plug.gameObject.SetActive(false);
                }
                return;
            }
            int count=Mathf.Max(0,activeCount);
            if(count!=pots.Count||!wasWorking)Arrange(count);
            var pose=GeneratorPoseAt(time,weight);
            machine.localPosition=pose.position;machine.localRotation=pose.rotation;
            for(int i=0;i<footMounts.Length;i++) {
                Vector3 lower=lowerFootJoints[i].localPosition,upper=footing.InverseTransformPoint(upperFootJoints[i].position),delta=upper-lower;
                footMounts[i].localPosition=(lower+upper)*.5f;
                footMounts[i].localRotation=Quaternion.FromToRotation(Vector3.up,delta);
                footMounts[i].localScale=new Vector3(1,delta.magnitude,1);
            }
            crank.localPosition=pose.position+pose.rotation*LocalCrankCenter;
            crank.localRotation = pose.rotation * Quaternion.Euler(CrankAngle(time)*Mathf.Rad2Deg,0,0);
            // The grip has a free bearing; it does not drag the fist around the shaft.
            gripHandle.localRotation=Quaternion.Euler(-CrankAngle(time)*Mathf.Rad2Deg,0,0)*Quaternion.Euler(0,0,90);
            current.localScale = Vector3.one * (1 + Mathf.Sin(CrankAngle(time))*.10f);
            // Fill0..5, then reset directly to0 each revolution.
            float charge=Mathf.Repeat(time*4.4f/(2*Mathf.PI),1);
            int filled=Mathf.Clamp(Mathf.FloorToInt(charge*6),0,5);
            for(int i=0;i<chargeCells.Length;i++)chargeCells[i].gameObject.SetActive(i>=chargeCells.Length-filled);
            UpdateElectricity(time,weight);
            foreach(var supply in supplies)if(supply.present)
                UpdateSupplyCord(supply,root.InverseTransformPoint(supply.feedSource.position),root.InverseTransformPoint(supply.inlet.position));
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
                p.cableStart=root.InverseTransformPoint(p.source.position);
                UpdateCable(p);
                float phase=Mathf.Repeat(time*.55f-i*.19f,1);
                p.spark.localPosition=p.root.InverseTransformPoint(root.TransformPoint(Cable(p.cableStart,p.cableEnd,phase,p.cableSeed)));
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

        static float ElectricHash(int seed) {
            unchecked {uint x=(uint)seed;x^=x>>16;x*=0x7feb352d;x^=x>>15;x*=0x846ca68b;x^=x>>16;return (x&0xffffff)/16777215f;}
        }
        static float StrikePulse(float age,float duration) {
            if(age<0||age>=duration)return 0;
            return Mathf.SmoothStep(0,1,age/.012f)*Mathf.Pow(1-age/duration,.55f);
        }
        void WriteElectricPath(int path,Vector3[] points,float width,float alpha,Color core) {
            for(int node=0;node<ElectricNodes;node++) {
                Vector3 tangent=points[Mathf.Min(ElectricNodes-1,node+1)]-points[Mathf.Max(0,node-1)];
                Vector3 normal=Vector3.Cross(tangent,Vector3.forward).normalized;
                float taper=.45f+.55f*Mathf.Sin(node*Mathf.PI/(ElectricNodes-1));
                for(int band=0;band<ElectricBands;band++) {
                    int index=(path*ElectricNodes+node)*ElectricBands+band;
                    electricVertices[index]=points[node]+normal*(ElectricWidth[band]*width);
                    Color color=band==2||band==3?core:new Color(.015f,.20f,.90f);
                    color.a=ElectricAlpha[band]*alpha*taper;electricColors[index]=color;
                }
            }
        }
        void UpdateElectricity(float time,float weight) {
            // Each local strike has a quiet gap, a fresh jagged shape, forks
            // and a short re-strike. Events are staggered, not whole-box flashes.
            for(int arc=0;arc<ElectricArcs;arc++) {
                float period=.36f+.17f*ElectricHash(arc+173);
                float clock=time+arc*.137f;int strike=Mathf.FloorToInt(clock/period);
                float age=clock-strike*period;int seed=unchecked(strike*9176+arc*619+171);
                float duration=.065f+.045f*ElectricHash(seed+1);
                float envelope=StrikePulse(age,duration)+.58f*StrikePulse(age-duration-.035f,.04f);
                envelope=Mathf.Clamp01(envelope)*weight;
                Vector3 line=ArcEnd[arc]-ArcStart[arc],tangent=line.normalized,normal=Vector3.Cross(line,Vector3.forward).normalized;
                Vector3 outward=arc<4?new Vector3(Mathf.Sign(ArcStart[arc].x),0,0):arc==5?Vector3.down:Vector3.up;
                float start=.10f*ElectricHash(seed+2),span=.65f+.25f*ElectricHash(seed+3);
                for(int node=0;node<ElectricNodes;node++) {
                    float t=node/(float)(ElectricNodes-1);
                    float zigzag=(ElectricHash(seed+node*31+4)*2-1)*.010f*Mathf.Sin(t*Mathf.PI);
                    electricPath[node]=ArcStart[arc]+line*(start+span*t)+normal*zigzag;
                }
                Color core=arc>=6?new Color(1,.82f,.18f):new Color(.20f,.72f,1);
                WriteElectricPath(arc,electricPath,.0065f+.0015f*ElectricHash(seed+5),envelope*.98f,core);
                for(int fork=0;fork<2;fork++) {
                    Vector3 origin=electricPath[fork==0?2:4];
                    float length=(arc<4?.010f:.016f)+ElectricHash(seed+fork*43+7)*.006f;
                    Vector3 end=origin+outward*length+tangent*(fork==0?-.022f:.025f);
                    for(int node=0;node<ElectricNodes;node++) {
                        float t=node/(float)(ElectricNodes-1);
                        float zigzag=(ElectricHash(seed+fork*107+node*17+11)*2-1)*.0035f*Mathf.Sin(t*Mathf.PI);
                        branchPath[node]=Vector3.Lerp(origin,end,t)+normal*zigzag;
                    }
                    bool visible=ElectricHash(seed+fork*53+13)<(fork==0?.82f:.52f);
                    WriteElectricPath(ElectricArcs+arc*2+fork,branchPath,.0038f,visible?envelope*.80f:0,core);
                }
                for(int spark=0;spark<SparksPerArc;spark++) {
                    float life=Mathf.Clamp01((age-spark*.012f)/.18f);
                    float turn=(ElectricHash(seed+spark*73+19)-.5f)*1.8f;
                    Vector3 direction=(outward+tangent*turn).normalized;
                    Vector3 center=electricPath[2+spark]+direction*(.003f+.016f*life);
                    float length=.006f+.006f*ElectricHash(seed+spark*79+23);
                    Vector3 cross=Vector3.Cross(direction,Vector3.forward).normalized*.0020f;
                    int n=(arc*SparksPerArc+spark)*4;
                    sparkVertices[n]=center-direction*length*.5f-cross;sparkVertices[n+1]=center-direction*length*.5f+cross;
                    sparkVertices[n+2]=center+direction*length*.5f+cross;sparkVertices[n+3]=center+direction*length*.5f-cross;
                    float alpha=age>=spark*.012f&&age<.18f+spark*.012f?Mathf.Pow(1-life,1.4f)*weight:0;
                    Color tail=new Color(1,.47f,.035f,alpha*.85f),tip=new Color(1,.96f,.67f,alpha);
                    sparkColors[n]=tail;sparkColors[n+1]=tail;sparkColors[n+2]=tip;sparkColors[n+3]=tip;
                }
            }
            electricMesh.vertices=electricVertices;electricMesh.colors=electricColors;electricMesh.RecalculateBounds();
            sparkMesh.vertices=sparkVertices;sparkMesh.colors=sparkColors;sparkMesh.RecalculateBounds();
        }

        void Arrange(int count)
        {
            // Two generator outlets feed direct cookers first, then power strips.
            // Upgraded eight/ten-way strips remain even after job counts fall.
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
                p.root.localPosition=new Vector3((column-(inRow-1)*.5f)*.155f*size+((count>=3||powerUpgradeMask!=0)?.20f:0),.085f+row*.085f*size,.430f-row*.025f);
                p.root.localScale=Vector3.one*size;
                // Electric pulse travels in station coordinates, independent of pot scaling.
                p.spark.localScale=Vector3.one/size;
            }
            PlanPower(count);
            foreach(var p in pots){p.cableStart=root.InverseTransformPoint(p.source.position);UpdateCable(p);}
        }

        void PlanPower(int count) {
            int extra=Mathf.Max(0,Mathf.CeilToInt((count-18)/9f));
            int a=Mathf.Min(8,(count+1)/2),b=Mathf.Min(extra>0?9:10,count-a),before=powerUpgradeMask;
            if(a>=4)powerUpgradeMask|=1;if(b>=5)powerUpgradeMask|=2;
            if(before!=powerUpgradeMask&&persistPowerUpgrades){PlayerPrefs.SetInt(PowerUpgradeKey,powerUpgradeMask);PlayerPrefs.Save();}
            while(supplies.Count<2+extra)supplies.Add(CreateSupply(supplies.Count,10));
            for(int i=0;i<supplies.Count;i++) {
                var supply=supplies[i];int capacity=i==0?((powerUpgradeMask&1)!=0?8:4):i==1?((powerUpgradeMask&2)!=0?10:5):10;
                if(supply.capacity!=capacity)BuildSupply(supply,i,capacity);
                supply.present=i<2?((i==0?a:b)>1||(powerUpgradeMask&(1<<i))!=0):i<2+extra;
                supply.root.gameObject.SetActive(supply.present);supply.wire.gameObject.SetActive(supply.present);
                foreach(var plug in supply.plugs){plug.gameObject.SetActive(false);TintPlug(plug,Navy);}
                supply.feedSource=i<2?generatorOutlets[i]:supplies[i-1].outlets[9];
                if(i>1&&supply.present){supplies[i-1].plugs[9].gameObject.SetActive(true);TintPlug(supplies[i-1].plugs[9],SupplyWhite);}
            }
            for(int i=0;i<2;i++){generatorPlugs[i].gameObject.SetActive((i==0?a:b)>0||supplies[i].present);TintPlug(generatorPlugs[i],supplies[i].present?SupplyWhite:Navy);}
            // Keep each strip's cookers on its side of the stage, instead of
            // alternating far-left/far-right cords across the whole scene.
            var ordered=new List<Pot>(pots);
            ordered.Sort((left,right)=>{int x=right.root.localPosition.x.CompareTo(left.root.localPosition.x);return x!=0?x:left.root.GetSiblingIndex().CompareTo(right.root.GetSiblingIndex());});
            for(int i=0;i<count;i++) {
                int lane,slot;
                if(i<a){lane=0;slot=a-1-i;}
                else if(i<a+b){lane=1;slot=a+b-1-i;}
                else {
                    lane=2;slot=i-a-b;
                    while(lane<1+extra&&slot>=9){slot-=9;lane++;}
                }
                var supply=supplies[lane];
                if(supply.present){supply.plugs[slot].gameObject.SetActive(true);ordered[i].source=supply.outlets[slot];}
                else ordered[i].source=generatorOutlets[lane];
            }
        }

        Supply CreateSupply(int index,int capacity) {
            var supply=new Supply{root=Child(root,"Power strip "+index),cableSeed=13+index*1.37f};
            supply.wire=Child(root,"Supply cable "+index);supply.cable=CreateSupplyCord(supply);
            BuildSupply(supply,index,capacity);supply.root.gameObject.SetActive(false);supply.wire.gameObject.SetActive(false);return supply;
        }
        void BuildSupply(Supply supply,int index,int capacity) {
            if(supply.geometry) {
                supply.geometry.gameObject.SetActive(false);
                foreach(var f in supply.geometry.GetComponentsInChildren<MeshFilter>(true)){meshes.Remove(f.sharedMesh);Release(f.sharedMesh);}
                Release(supply.geometry.gameObject);
            }
            supply.capacity=capacity;supply.geometry=Child(supply.root,"Geometry");supply.plugs=new Transform[capacity];supply.outlets=new Transform[capacity];
            int columns=capacity==4||capacity==8?4:5,rows=capacity/columns;
            float length=.055f+columns*.040f,width=rows==1?.048f:.086f;
            Color paint=index%2==0?Ivory:new Color(.75f,.86f,.83f),dark=new Color(.15f,.21f,.27f);
            Color side=index%2==0?new Color(.63f,.69f,.70f):new Color(.39f,.56f,.54f);
            // Real stepped upper/lower shells, with a28mm casing instead of
            // a thin single plate. The darker lower moulding remains visible.
            var body=new Shape();body.RoundedBlock(new Vector3(0,0,-.003f),new Vector3(length,width,.020f),.005f,side);
            body.RoundedBlock(new Vector3(0,0,.009f),new Vector3(length-.003f,width-.003f,.012f),.004f,paint);
            var casing=Part(supply.geometry,"Power strip body "+capacity,body);
            var rocker=new Shape();rocker.RoundedBlock(Vector3.zero,new Vector3(.021f,.027f,.008f),.002f,index%2==0?Teal:Gold);
            var switchPart=Part(supply.geometry,"Raised rocker switch",rocker);switchPart.localPosition=new Vector3(-length*.5f+.020f,0,.021f);
            for(int slot=0;slot<capacity;slot++) {
                int row=slot/columns,column=slot%columns;
                var socket=Child(supply.geometry,"Socket "+slot);socket.localPosition=new Vector3(-length*.5f+.053f+column*.040f,(row-(rows-1)*.5f)*.038f,.016f);
                var plate=new Shape();plate.RoundedBlock(Vector3.zero,new Vector3(.034f,.032f,.003f),.003f,new Color(.46f,.59f,.58f));
                plate.RoundedBlock(new Vector3(0,0,.0018f),new Vector3(.030f,.028f,.002f),.002f,new Color(.91f,.94f,.91f));Part(socket,"Socket plate",plate);
                for(int hole=0;hole<3;hole++) {
                    var shape=new Shape();shape.RoundedBlock(Vector3.zero,new Vector3(.0028f,hole==0?.008f:.010f,.001f),.0004f,dark);
                    var h=Part(socket,"Slot "+hole,shape);h.localPosition=new Vector3(hole==0?0:hole==1?-.008f:.008f,hole==0?.008f:-.004f,.0033f);h.localRotation=Quaternion.Euler(0,0,hole==0?0:hole==1?-28:28);
                }
                var plug=new Shape();plug.RoundedBlock(Vector3.zero,new Vector3(.023f,.024f,.014f),.003f,Navy);
                supply.plugs[slot]=Part(socket,"Plug",plug);supply.plugs[slot].localPosition=Vector3.forward*.010f;
                var strain=new Shape();strain.Lathe(new[]{new Vector2(.0045f,-.004f),new Vector2(.004f,.004f)},10,Navy);
                supply.outlets[slot]=Part(supply.plugs[slot],"Cable outlet",strain);supply.outlets[slot].localPosition=Vector3.forward*.011f;supply.outlets[slot].localRotation=Quaternion.Euler(90,0,0);
            }
            var inlet=new Shape();inlet.RoundedBlock(Vector3.zero,new Vector3(.014f,.018f,.012f),.003f,SupplyWhite);
            supply.inlet=Part(supply.geometry,"Feed inlet",inlet);supply.inlet.localPosition=new Vector3(-length*.5f-.006f,0,0);
            // Casually set down beside the generator, rather than lined up
            // across the front of the whole cooker row.
            supply.root.localRotation=Quaternion.Euler(index%2==0?-68:-64,index%2==0?18:-24,index%2==0?6:-4);
            float lowest=float.PositiveInfinity;
            foreach(var point in casing.GetComponent<MeshFilter>().sharedMesh.vertices)lowest=Mathf.Min(lowest,(supply.root.localRotation*point).y);
            float x=index==0?-.220f:index==1?-.450f:(index%2==0?-.250f:-.390f);
            float z=index==0?.430f:index==1?.480f:.610f+(index-2)*.12f;
            supply.root.localPosition=new Vector3(x,GeneratorFloorHeight-lowest,z);
        }
        static void TintPlug(Transform plug,Color color) {
            foreach(var filter in plug.GetComponentsInChildren<MeshFilter>(true)) {
                var colors=filter.sharedMesh.colors;
                if(colors.Length>0&&colors[0]==color)continue;
                for(int i=0;i<colors.Length;i++)colors[i]=color;filter.sharedMesh.colors=colors;
            }
        }
        Mesh CreateSupplyCord(Supply supply) {
            var mesh=Own(new Mesh{name="White round power strip cord"});mesh.MarkDynamic();mesh.vertices=supply.vertices;
            var colors=new Color[supply.vertices.Length];for(int i=0;i<colors.Length;i++)colors[i]=SupplyWhite;
            var triangles=new List<int>();
            for(int ring=0;ring<CableSteps;ring++)for(int j=0;j<SupplySides;j++) {
                int a=ring*SupplySides+j,b=ring*SupplySides+(j+1)%SupplySides,c=b+SupplySides,d=a+SupplySides;triangles.AddRange(new[]{a,c,b,a,d,c});
            }
            mesh.colors=colors;mesh.normals=supply.normals;mesh.triangles=triangles.ToArray();
            supply.wire.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;supply.wire.gameObject.AddComponent<MeshRenderer>().sharedMaterial=solid;return mesh;
        }
        void UpdateSupplyCord(Supply supply,Vector3 start,Vector3 end) {
            for(int i=0;i<=CableSteps;i++)supply.points[i]=Cable(start,end,i/(float)CableSteps,supply.cableSeed);
            Vector3 previousSide=Vector3.zero;
            for(int i=0;i<=CableSteps;i++) {
                Vector3 tangent=(supply.points[Mathf.Min(CableSteps,i+1)]-supply.points[Mathf.Max(0,i-1)]).normalized;
                Vector3 side=i==0?Vector3.Cross(tangent,new Vector3(0,.65f,1)):Vector3.ProjectOnPlane(previousSide,tangent);
                side.Normalize();Vector3 up=Vector3.Cross(side,tangent).normalized;previousSide=side;
                for(int j=0;j<SupplySides;j++) {
                    float angle=j*Mathf.PI*2/SupplySides;Vector3 radial=side*Mathf.Cos(angle)+up*Mathf.Sin(angle);int n=i*SupplySides+j;
                    supply.vertices[n]=supply.wire.InverseTransformPoint(root.TransformPoint(supply.points[i]+radial*.003f));
                    supply.normals[n]=supply.wire.InverseTransformDirection(root.TransformDirection(radial));
                }
            }
            supply.cable.vertices=supply.vertices;supply.cable.normals=supply.normals;supply.cable.RecalculateBounds();
        }
        Mesh CreateCord(Transform transform,Vector3[] vertices,string name) {
            var mesh=Own(new Mesh{name=name});mesh.MarkDynamic();mesh.vertices=vertices;
            var colors=new Color[vertices.Length];var normals=new Vector3[vertices.Length];var triangles=new List<int>();
            for(int i=0;i<vertices.Length;i++){colors[i]=Navy;normals[i]=Vector3.forward;}
            for(int j=0;j<CableSteps;j++){int n=j*4;triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            mesh.colors=colors;mesh.normals=normals;mesh.triangles=triangles.ToArray();transform.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;transform.gameObject.AddComponent<MeshRenderer>().sharedMaterial=solid;return mesh;
        }
        void UpdateCord(Mesh mesh,Vector3[] vertices,Transform transform,Vector3 start,Vector3 end,float seed) {
            for(int j=0;j<CableSteps;j++) {
                Vector3 a=Cable(start,end,j/(float)CableSteps,seed),b=Cable(start,end,(j+1f)/CableSteps,seed),normal=Vector3.Cross(b-a,new Vector3(0,.65f,1)).normalized*.0022f;int n=j*4;
                vertices[n]=transform.InverseTransformPoint(root.TransformPoint(a-normal));vertices[n+1]=transform.InverseTransformPoint(root.TransformPoint(a+normal));
                vertices[n+2]=transform.InverseTransformPoint(root.TransformPoint(b+normal));vertices[n+3]=transform.InverseTransformPoint(root.TransformPoint(b-normal));
            }
            mesh.vertices=vertices;mesh.RecalculateBounds();
        }

        void UpdateCable(Pot p)
        {
            // Keep the actual plug connected while the cooker rocks. The cable
            // mesh and its buffers are reused, rather than rebuilt each frame.
            p.cableEnd=root.InverseTransformPoint(p.wobble.TransformPoint(CookerPort));
            UpdateCord(p.cable,p.cableVertices,p.root,p.cableStart,p.cableEnd,p.cableSeed);
        }

        static Vector3 Cable(Vector3 start,Vector3 end,float t,float seed)
        {
            Vector3 groundDirection=Vector3.ProjectOnPlane(end-start,Vector3.up);
            float distance=groundDirection.magnitude;
            groundDirection=distance>.00001f?groundDirection/distance:Vector3.right;
            Vector3 sideways=Vector3.Cross(Vector3.up,groundDirection);
            float envelope=Mathf.Pow(Mathf.Sin(t*Mathf.PI),2);
            float extra=Mathf.Min(.090f,.030f+distance*.15f)*(.90f+.10f*Mathf.Sin(seed*1.2f));
            Vector3 point=Vector3.Lerp(start,end,t);
            // Extra cable lies in a lazy S on the floor, with a soft descent
            // from each real plug. Seed belongs to the cooker, so it does not
            // reroll or slide when another job appears.
            point+=sideways*(Mathf.Sin(t*Mathf.PI*2+seed*.7f)*extra*envelope);
            point+=groundDirection*(Mathf.Sin(t*Mathf.PI*2)*Mathf.Min(.035f,distance*.06f)*envelope);
            float floor=GeneratorFloorHeight+.006f;
            point.y=floor+(start.y-floor)*(1-Mathf.SmoothStep(0,1,t/.26f))+(end.y-floor)*Mathf.SmoothStep(0,1,(t-.74f)/.26f);
            return point;
        }

        Pot CreatePot(int index)
        {
            var p=new Pot {root=Child(root,"Rice cooker "+(index+1)),style=PickStyle(),cableSeed=index*1.73f};
            p.wobble=Child(p.root,"Cooker wobble");
            BuildCooker(p);
            var wire=Child(p.root,"Power cable");p.cable=CreateCord(wire,p.cableVertices,"Connected power cable");
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
