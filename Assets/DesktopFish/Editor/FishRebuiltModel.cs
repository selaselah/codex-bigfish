using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesktopFish.Editor
{
    // All vertices, colors, binding positions and expression deltas originate here.
    // Sparse measured control guides constrain the surfaces; no source mesh topology is loaded.
    public static class FishRebuiltModel
    {
        const string Folder = "Assets/DesktopFish/Rebuilt";
        const int RearDomeColumns=40;
        internal const int PaintWidth=1024, LegacyPaintHeight=1152, RearPaintHeight=512, PaintHeight=2048;
        static readonly Color Skin = Hex("FFF0E9"), Hair = Hex("304C91"), Tip = Hex("58B2D5"),
            Navy = Hex("252E5D"), White = Hex("FFFCF3"), Gold = Hex("E2C47D"), Ink = Hex("233150");
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color c); return c; }
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        [Serializable] sealed class PartBudget {public string name;public int triangles;}
        [Serializable] sealed class BudgetDump {public string partCountStage="authored, before compaction and reduction";public int limit=20000,total;public PartBudget[] parts;}
        [Serializable] sealed class BoneDump { public string name; public int parent; public Vector3 position; }
        [Serializable] sealed class ShapeDump { public string name; public Vector3[] deltas; }
        [Serializable] sealed class MeshDump {
            public string name; public Vector3[] vertices, normals; public Color[] colors; public Vector2[] uv;
            public int[] triangles, bone_indices, secondary_bone_indices;
            public float[] primary_bone_weights; public ShapeDump[] shapes;
        }
        [Serializable] sealed class ModelDump { public BoneDump[] bones; public MeshDump[] meshes; }
        [Serializable] sealed class PoseBone { public string name; public Vector3 position; public Quaternion rotation; }
        [Serializable] sealed class PoseDump { public PoseBone[] bones; }

        public static void ExportIdlePose(GameObject model) {
            var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
            File.WriteAllText("work/rebuilt-idle-pose.json",JsonUtility.ToJson(new PoseDump {
                bones=skin.bones.Select(b=>new PoseBone {name=b.name,position=b.position,rotation=b.rotation}).ToArray()
            }));
        }

        sealed class Geo
        {
            public readonly List<Vector3> p = new List<Vector3>();
            public int region;
            public readonly List<int> regions=new List<int>();
            public readonly List<Color> colors = new List<Color>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<BoneWeight> weights = new List<BoneWeight>();
            public readonly List<int> tri = new List<int>();
            public readonly Dictionary<int,Vector4> normalBlends = new Dictionary<int,Vector4>();
            public readonly List<Vector2Int> smoothNormalPairs = new List<Vector2Int>();
            public readonly Dictionary<int,List<int>> splitNormalCorners = new Dictionary<int,List<int>>();
            public int Add(Vector3 v, Color color, int bone, Vector2? texture = null, int secondaryBone = -1, float secondaryWeight = 0) {
                // Cloth shading scales RGB only. Cutout coverage belongs to the
                // atlas, so glTF vertex alpha must not shrink the lace edge.
                color.a=1;
                p.Add(v);regions.Add(region); colors.Add(color); uv.Add(texture ?? new Vector2(.995f,.995f));
                weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1-secondaryWeight,
                    boneIndex1 = secondaryBone < 0 ? bone : secondaryBone, weight1 = secondaryWeight });
                return p.Count - 1;
            }
            public void Face(int a, int b, int c) { tri.Add(a); tri.Add(b); tri.Add(c); }
            public Mesh Build(string name, Transform root, Transform[] bones) {
                // Duplicate only a shared normal boundary, after all parts have
                // been authored, so existing vertex indices and skinning stay stable.
                foreach(var edge in splitNormalCorners.OrderBy(e=>e.Key)) {
                    int source=edge.Key,target=p.Count;
                    p.Add(p[source]);regions.Add(regions[source]);colors.Add(colors[source]);uv.Add(uv[source]);weights.Add(weights[source]);
                    foreach(int corner in edge.Value)tri[corner]=target;
                }
                if (p.Any(v => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                    float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)))
                    throw new InvalidOperationException("Non-finite authored mesh vertex: " + name);
                var mesh = new Mesh { name = name, indexFormat = p.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(p); mesh.SetColors(colors); mesh.SetUVs(0,uv.Select(v=>new Vector2(v.x,(v.y*1024+128)/PaintHeight)).ToList()); mesh.SetTriangles(tri, 0);
                mesh.boneWeights = weights.ToArray();
                mesh.bindposes = bones.Select(b => b.worldToLocalMatrix * root.localToWorldMatrix).ToArray();
                mesh.RecalculateNormals();
                var normals=mesh.normals;
                foreach(var entry in normalBlends) {
                    Vector4 target=entry.Value;
                    normals[entry.Key]=Vector3.Lerp(normals[entry.Key],V(target.x,target.y,target.z),target.w).normalized;
                }
                foreach(var pair in smoothNormalPairs) {
                    Vector3 joined=(normals[pair.x]+normals[pair.y]).normalized;
                    normals[pair.x]=joined;normals[pair.y]=joined;
                }
                mesh.normals=normals;
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // Remove collapsed edges only when the coincident vertices also deform
        // together. Rest-pose collinearity alone is unsafe for a skinned mesh.
        static Mesh CompactMesh(Mesh source) {
            var vertices=source.vertices;var weights=source.boneWeights;
            var frames=new List<Tuple<string,float,Vector3[],Vector3[],Vector3[]>>();
            for(int shape=0;shape<source.blendShapeCount;shape++)
                for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++) {
                    var dp=new Vector3[vertices.Length];var dn=new Vector3[vertices.Length];var dt=new Vector3[vertices.Length];
                    source.GetBlendShapeFrameVertices(shape,frame,dp,dn,dt);
                    frames.Add(Tuple.Create(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),dp,dn,dt));
                }
            Func<int,int,bool> sameMotion=(a,b)=>(vertices[a]-vertices[b]).sqrMagnitude<1e-14f &&
                weights[a].Equals(weights[b]) && frames.All(f=>(f.Item3[a]-f.Item3[b]).sqrMagnitude<1e-14f);
            var triangles=new List<int>();var original=source.triangles;
            for(int i=0;i<original.Length;i+=3) {
                int a=original[i],b=original[i+1],c=original[i+2];
                // Double precision avoids float cancellation at collapsed poles.
                double ux=(double)vertices[b].x-vertices[a].x,uy=(double)vertices[b].y-vertices[a].y,uz=(double)vertices[b].z-vertices[a].z;
                double vx=(double)vertices[c].x-vertices[a].x,vy=(double)vertices[c].y-vertices[a].y,vz=(double)vertices[c].z-vertices[a].z;
                double nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx;
                bool tiny=nx*nx+ny*ny+nz*nz<4e-24;
                if(tiny&&(sameMotion(a,b)||sameMotion(b,c)||sameMotion(c,a)))continue;
                triangles.Add(a);triangles.Add(b);triangles.Add(c);
            }
            var used=new HashSet<int>(triangles);
            int[] retained=Enumerable.Range(0,vertices.Length).Where(used.Contains).ToArray();
            var remap=new int[vertices.Length];
            for(int i=0;i<retained.Length;i++)remap[retained[i]]=i;
            var mesh=new Mesh {name=source.name,indexFormat=retained.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            var normals=source.normals;var colors=source.colors;var uv=source.uv;
            mesh.vertices=retained.Select(i=>vertices[i]).ToArray();
            mesh.normals=retained.Select(i=>normals[i]).ToArray();
            mesh.colors=retained.Select(i=>colors[i]).ToArray();
            mesh.uv=retained.Select(i=>uv[i]).ToArray();
            mesh.boneWeights=retained.Select(i=>weights[i]).ToArray();mesh.bindposes=source.bindposes;
            mesh.triangles=triangles.Select(i=>remap[i]).ToArray();
            foreach(var frame in frames)mesh.AddBlendShapeFrame(frame.Item1,frame.Item2,
                retained.Select(i=>frame.Item3[i]).ToArray(),retained.Select(i=>frame.Item4[i]).ToArray(),
                retained.Select(i=>frame.Item5[i]).ToArray());
            mesh.RecalculateBounds();
            Debug.Log($"Compacted {source.name}: {vertices.Length} -> {retained.Length} vertices; {original.Length/3} -> {triangles.Count/3} triangles");
            UnityEngine.Object.DestroyImmediate(source);
            return mesh;
        }

        sealed class Author
        {
            public readonly GameObject root = new GameObject("Rebuilt BigFish");
            public readonly List<Transform> bones = new List<Transform>();
            public readonly Dictionary<string, int> ids = new Dictionary<string, int>();
            public readonly Geo body = new Geo(), face = new Geo();
            public readonly List<PartBudget> budgets=new List<PartBudget>();
            public readonly List<string> rearAttachmentReport=new List<string>();
            int lastTriangle;
            public void Mark(string name){budgets.Add(new PartBudget{name=name,triangles=(body.tri.Count-lastTriangle)/3});lastTriangle=body.tri.Count;}
            readonly List<Tuple<int, Vector3, string>> features = new List<Tuple<int, Vector3, string>>();
            readonly Dictionary<int,Vector3> closedLashes = new Dictionary<int,Vector3>();
            readonly Dictionary<int,Vector3> smilingLashes = new Dictionary<int,Vector3>();
            public int Bone(string name, int parent, Vector3 position) {
                var bone = new GameObject(name).transform;
                bone.SetParent(parent < 0 ? root.transform : bones[parent], false); bone.position = position;
                ids[name] = bones.Count; bones.Add(bone); return bones.Count - 1;
            }
            public void Sphere(Geo g, Vector3 c, Vector3 r, Color color, int bone, int n = 12, int rows = 8) {
                if(r.y<.10f){ n=Mathf.Min(n,12);rows=Mathf.Min(rows,8); }
                if(Mathf.Max(r.x,Mathf.Max(r.y,r.z))<.012f){n=Mathf.Min(n,8);rows=Mathf.Min(rows,4);}
                int start = g.p.Count;
                for (int y = 0; y <= rows; y++) for (int x = 0; x <= n; x++) {
                    float a = x * Mathf.PI * 2 / n, b = y * Mathf.PI / rows;
                    g.Add(c + Vector3.Scale(r, V(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Cos(b), Mathf.Sin(b) * Mathf.Sin(a))), color, bone);
                }
                Grid(g, start, rows, n);
            }
            public void GuidedNeck(int neck,int head) {
                int start=body.p.Count,oldRegion=body.region;body.region=0;const int columns=12;var ys=FishNeckGuides.Stations;
                for(int row=0;row<ys.Length;row++)for(int col=0;col<=columns;col++) {
                    float y=ys[row],angle=col*Mathf.PI*2/columns;Vector4 q=FishNeckGuides.Profile(y);
                    // A convex section overlaps the independent head shell
                    // inside its hidden anterior face. Its measured posterior
                    // bound and width remain fixed; avoid concave clipped rings.
                    if(row>0&&row<ys.Length-1&&y>.548f) {
                        Vector3 old=AuthorPoint(V(0,y,0));float width=FaceWidthScale(old.y)*FishModelGuides.Point(V(1,y,0),1).x;
                        float x=q.y*.95f/width,disc=1-x*x/(.214f*.214f)-Mathf.Pow((old.y-.773f)/.187f,2);
                        if(disc>0)q.w=Mathf.Max(q.w,ShapeHeadPoint(V(x,old.y,-.166f*Mathf.Sqrt(disc))).z+.004f);
                    }
                    float center=(q.z+q.w)*.5f,rx=q.y,rz=(q.w-q.z)*.5f;
                    if(row==0||row==ys.Length-1){rx=0;rz=0;}
                    Vector3 final=V(rx*Mathf.Cos(angle),y,center+rz*Mathf.Sin(angle));
                    Vector3 p=AuthorPoint(final);if(p.y<.585f)p.y=.585f-(.585f-p.y)*.88f;
                    float share=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.52f,.58f,y));
                    body.Add(p,Skin,neck,null,head,share);
                }
                Grid(body,start,ys.Length-1,columns);
                for(int row=0;row<ys.Length;row++)body.smoothNormalPairs.Add(new Vector2Int(start+row*(columns+1),start+row*(columns+1)+columns));
                body.region=oldRegion;
            }
            public void Tube(Geo g, Vector3[] path, float radius, Color color, int bone, int sides = 6) {
                Sweep(g, path, t => radius, t => radius, t => color, bone, sides);
            }
            public void Sweep(Geo g, Vector3[] path, Func<float,float> width, Func<float,float> depth,
                Func<float,Color> color, int bone, int sides = 10) {
                int start = g.p.Count;
                for (int i = 0; i < path.Length; i++) {
                    float t = i / (float)(path.Length - 1);
                    Vector3 tangent = (path[Mathf.Min(i + 1, path.Length - 1)] - path[Mathf.Max(0, i - 1)]).normalized;
                    Vector3 axis = Vector3.Cross(tangent, Vector3.forward).normalized;
                    if (axis.sqrMagnitude < .1f) axis = Vector3.right;
                    Vector3 other = Vector3.Cross(tangent, axis).normalized;
                    for (int j = 0; j <= sides; j++) {
                        float a = j * Mathf.PI * 2 / sides;
                        g.Add(path[i] + axis * Mathf.Cos(a) * width(t) + other * Mathf.Sin(a) * depth(t), color(t), bone);
                    }
                }
                Grid(g, start, path.Length - 1, sides);
            }
            // Flattened, ridged locks with separately authored silhouette widths.
            // The cross-section closes at the edges; neighboring locks overlap at
            // the roots and terminate in sharp, offset tips rather than tubes.
            public void HairPanel(Vector3[] controls, float width, int bone, bool cyan, int phase = 0,
                Vector3? outward = null, int sides = 12, int samples = 0, float tipStart = .58f,
                float rootScale = 1, float backSweep = 0, float sideTurn = 0, float fullness = 1,
                float lowerWidth = 1, float tipBias = 0, float tipPower = 0, float rootTaper = 0, float paintStart = 0,
                float tipPaintEase = 1, Vector3[] edgeA = null, Vector3[] edgeB = null, float paintOffset = 0,
                float cheekInset = -1, float frontLift = 0, float wrapDepth = 0, Vector3[] paintGuide = null,
                bool surfaceThickness = false, float rootTurn = 0) {
                int start=body.p.Count; int rows=HairRows(samples>0?samples:controls.Length>4?36:24);
                sides=Mathf.Min(sides,8);
                var path=Curve(controls,rows);
                if(outward.HasValue&&controls.Length==13) {
                    // Spend one third of the longitudinal stations on the high
                    // curvature crown; keep the lower Bezier guides unchanged.
                    for(int i=0;i<=rows;i++) {
                        float t=i/(float)rows;
                        float parameter=RearHairParameter(t);
                        float local=parameter*4;int segment=Mathf.Min(3,(int)local);
                        path[i]=Bezier(controls.Skip(segment*3).Take(4).ToArray(),local-segment);
                    }
                }
                var boundaryA=edgeA==null?null:Curve(edgeA,rows);
                var boundaryB=edgeB==null?null:Curve(edgeB,rows);
                Vector3 facing=outward??Vector3.forward;
                bool frontalSection=bone==ids["J_Bip_C_Head"]&&!outward.HasValue&&boundaryA!=null;
                bool normalSection=frontalSection||surfaceThickness;
                var frontalDepth=normalSection?new float[(rows+1)*(sides+1)]:null;
                int mid=rows/2;
                float boundarySign=boundaryA==null?1:Mathf.Sign(Vector3.Dot(boundaryB[mid]-boundaryA[mid],
                    Vector3.Cross(path[mid+1]-path[mid-1],facing)));
                if(outward.HasValue)for(int i=0;i<path.Length;i++) {
                    Vector3 p=path[i];
                    if(p.y>.675f&&p.y<.975f) {
                        float scalp=Mathf.Sqrt(Mathf.Max(.01f,1-Mathf.Pow((p.y-.765f)/.215f,2)));
                        float ellipse=Mathf.Sqrt(Mathf.Pow(p.x/(.232f*scalp),2)+Mathf.Pow(p.z/(.182f*scalp),2));
                        // Follow our analytic scalp instead of letting the cap cut
                        // across a recessed lock with an exposed polygonal edge.
                        float clearance=Mathf.Max(1,1.025f/Mathf.Max(.001f,ellipse));
                        path[i]=V(p.x*clearance,p.y,p.z*clearance);
                    }
                }
                Vector3 previousSide=Vector3.zero;
                for(int row=0;row<=rows;row++) {
                    float t=row/(float)rows;
                    if(outward.HasValue&&controls.Length==13)t=RearHairParameter(t);
                    Vector3 tangent=(path[Mathf.Min(row+1,rows)]-path[Mathf.Max(row-1,0)]).normalized;
                    // Independent locks can turn their broad surface toward the
                    // side without invoking the radial scalp-root construction.
                    float turn=Mathf.Lerp(rootTurn,sideTurn,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.38f,t)));
                    Vector3 sectionFacing=Quaternion.Euler(0,turn,0)*facing;
                    Vector3 side=Vector3.Cross(tangent,sectionFacing);
                    float facingCross=side.magnitude;
                    if(outward.HasValue&&previousSide.sqrMagnitude>.1f) {
                        Vector3 transported=Vector3.ProjectOnPlane(previousSide,tangent).normalized;
                        Vector3 desired=side.normalized;
                        if(Vector3.Dot(desired,transported)<0)desired=-desired;
                        float aligned=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.70f,facingCross));
                        side=Vector3.Lerp(transported,desired,aligned);
                    }
                    if(side.sqrMagnitude<.001f)side=Vector3.ProjectOnPlane(previousSide,tangent);
                    if(side.sqrMagnitude<.001f)side=Vector3.right;
                    side.Normalize();
                    if(previousSide.sqrMagnitude>.1f&&Vector3.Dot(side,previousSide)<0)side=-side;
                    previousSide=side;
                    Vector3 thicknessAxis=Vector3.Cross(side,tangent).normalized;
                    float taperPower=tipPower>0?tipPower:outward.HasValue?1.05f:.83f;
                    float taper=Mathf.Pow(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(tipStart,1,t)),taperPower);
                    float rootWidth=outward.HasValue?
                        Mathf.Clamp01(Mathf.Sqrt(Mathf.Pow(path[row].x/.244f,2)+Mathf.Pow(path[row].z/.192f,2))):
                        .66f+.34f*Mathf.Sin(t*Mathf.PI*.85f);
                    float rootBlend=Mathf.Lerp(rootScale,1,Mathf.SmoothStep(0,1,t/.35f));
                    float lowerScale=Mathf.Lerp(1,lowerWidth,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.42f,t)));
                    float w=width*rootWidth*rootBlend*taper*lowerScale+.00015f;
                    float volume=Mathf.Lerp(1,fullness,Mathf.Sin(t*Mathf.PI));
                    float thickness=(cyan?.005f:.006f)*(.6f+.4f*Mathf.Sin(t*Mathf.PI))*taper*volume+.0001f;
                    // Secondary curls grow out of a narrow embedded root; both
                    // width and depth open gradually into the existing lock.
                    float rootNeck=rootTaper>0?Mathf.Lerp(.06f,1,Mathf.SmoothStep(0,1,t/rootTaper)):1;
                    for(int j=0;j<=sides;j++) {
                        float angle=j/(float)sides*Mathf.PI*2;
                        float u=Mathf.Cos(angle),front=Mathf.Sin(angle);
                        float facet=Mathf.Pow(Mathf.Abs(front),.7f)*Mathf.Sign(front);
                        // Different taper starts on the two edges let a fringe
                        // sweep into an off-center point without changing roots.
                        float edgeTaper=tipBias==0?taper:Mathf.Pow(1-Mathf.SmoothStep(0,1,
                            Mathf.InverseLerp(tipStart+tipBias*u,1,t)),taperPower);
                        float edgeWidth=tipBias==0?w:width*rootWidth*rootBlend*lowerScale*edgeTaper+.00015f;
                        // Frontal locks can use independently shaped silhouette edges.
                        // This lets the forehead window rise on one side while
                        // the other edge continues into a long swept point.
                        Vector3 p=boundaryA==null?path[row]+side*(u*edgeWidth*rootNeck):
                            Vector3.Lerp(boundaryA[row],boundaryB[row],(u*boundarySign+1)*.5f)+side*(u*.00015f);
                        if(!normalSection)p+=thicknessAxis*facet*thickness*rootNeck;
                        // Keep attachment weights and crown paint on their
                        // original guide while reshaping the visible cross-section.
                        float attachmentHeight=p.y;
                        if(outward.HasValue&&attachmentHeight>.72f) {
                            float height=Mathf.Clamp((path[row].y-.765f)/.215f,-1,1);
                            float lift=.0045f+front*.002f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.18f,t));
                            attachmentHeight=Mathf.Lerp(attachmentHeight,.765f+(.215f+lift)*height,
                                Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.86f,attachmentHeight)));
                        }
                        if(outward.HasValue&&path[row].y>.480f) {
                            // Neighboring upper locks share angular section edges.
                            // Curving each complete section avoids nine flat plates
                            // while retaining the existing longitudinal guides.
                            float centerAngle=Mathf.Atan2(facing.x,facing.z);
                            float rx=Mathf.Abs(Mathf.Sin(centerAngle))>.1f?path[row].x/Mathf.Sin(centerAngle):.244f;
                            float rz=Mathf.Abs(Mathf.Cos(centerAngle))>.1f?path[row].z/Mathf.Cos(centerAngle):.192f;
                            float theta=centerAngle-u*(Mathf.PI*2-2*1.12f)/16;
                            Vector3 rounded=V(Mathf.Sin(theta)*(rx+front*.0025f),path[row].y,
                                Mathf.Cos(theta)*(rz+front*.0025f));
                            p=Vector3.Lerp(p,rounded,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.480f,.610f,path[row].y)));
                        }
                        if(outward.HasValue&&p.y>.72f) {
                            // Wrap the whole width around the scalp, not just the
                            // centerline. Flat roots otherwise read as raised plates.
                            float height=Mathf.Clamp((path[row].y-.765f)/.215f,-1,1);
                            float scalp=Mathf.Sqrt(Mathf.Max(.0001f,1-height*height));
                            float centerAngle=Mathf.Atan2(facing.x,facing.z);
                            float rootJoin=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.79f,.90f,path[row].y));
                            float scalpAngle=centerAngle-u*(Mathf.PI*2-2*1.12f)/16*(1+.025f*rootJoin);
                            float lift=.0045f+front*.002f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.18f,t));
                            Vector3 onScalp=V(Mathf.Sin(scalpAngle)*(.232f+lift)*scalp,
                                .765f+(.215f+lift)*height,Mathf.Cos(scalpAngle)*(.182f+lift)*scalp);
                            p=Vector3.Lerp(p,onScalp,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.86f,p.y)));
                        }
                        int head=ids["J_Bip_C_Head"];
                        if(bone==head&&!outward.HasValue&&p.y>.70f) {
                            // The root cross-section shares the headband's arc.
                            // Radial fitting preserves the crown volume instead
                            // of cutting a horizontal step into the side locks.
                            float radius=Mathf.Sqrt(p.x*p.x+Mathf.Pow(p.y-.765f,2));
                            float tuck=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,.94f,p.y))*
                                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.13f,.18f,p.z)));
                            float fit=Mathf.Lerp(1,Mathf.Min(1,.218f/Mathf.Max(.001f,radius)),tuck);
                            p.x*=fit;p.y=.765f+(p.y-.765f)*fit;
                            // The inner edge of a broad cheek lock can cross the
                            // forehead even while its centerline remains outside.
                            float skin=1-Mathf.Pow(p.x/.214f,2)-Mathf.Pow((p.y-.773f)/.187f,2);
                            if(skin>0)p.z=Mathf.Max(p.z,.166f*Mathf.Sqrt(skin)+.0045f+(frontalSection?0:front*.002f));
                        }
                        // A cheek lock wraps from its forward face-framing edge
                        // into the temple. The width/height silhouette stays authored;
                        // its rear edge opens in depth instead of remaining a flat card.
                        if(cheekInset>=0) {
                            float y=p.y;
                            float center=y<.70f?Mathf.Lerp(.202f,.223f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.625f,.70f,y))):
                                y<.78f?Mathf.Lerp(.223f,.222f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,.78f,y))):
                                Mathf.Lerp(.222f,.175f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,.94f,y)));
                            float depth=center-2.2f*p.x*p.x-cheekInset+
                                (frontalSection?0:thicknessAxis.z*facet*thickness*rootNeck);
                            float wrap=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,.92f,y));
                            p.z=Mathf.Lerp(p.z,depth,wrap);
                        }
                        // Lift the overlying fringe as a complete thin section so
                        // its junction cannot cut through the wrapped cheek surface.
                        p.z+=frontLift*Mathf.Max(0,Mathf.Sin(t*Mathf.PI));
                        if(wrapDepth>0)p.z+=wrapDepth*(1-u*u)*
                            Mathf.SmoothStep(0,1,Mathf.InverseLerp(.405f,.530f,p.y))*
                            (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.650f,.740f,p.y)));
                        // Sweep the complete section rearward below the crown.
                        // Deforming the frame with the vertices retains its width
                        // and curled tips while leaving the shared roots in place.
                        p=SweepHairSection(p,backSweep);
                        // Deform the shared front/back mid-surface first. Applying
                        // thickness before the scalp clamp can fold the inner face
                        // back through the outer face around the narrow crown root.
                        if(normalSection)frontalDepth[row*(sides+1)+j]=facet*thickness*rootNeck;
                        float ridge=.94f+.06f*Mathf.Pow(Mathf.Max(0,front),.6f)-.04f*Mathf.Pow(Mathf.Abs(u),6);
                        float crown=outward.HasValue?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.79f,.90f,attachmentHeight)):0;
                        ridge=Mathf.Lerp(ridge,.97f,crown*.85f);
                        // Joined rear locks describe one upper mass. Keep broad
                        // painted reflection continuous instead of baking a dark
                        // stripe into both edges of every longitudinal patch.
                        if(outward.HasValue)ridge=Mathf.Lerp(ridge,.97f,
                            Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.64f,attachmentHeight))*.94f);
                        Color shade=Color.white*ridge; shade.a=1;
                        float anchored=bone==head?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.88f,outward.HasValue?attachmentHeight:p.y));
                        int paintStrip=cyan?3:((phase%3)+3)%3;
                        int longPaint=((phase%3)+3)%3;
                        float paintU=cyan?.75f+longPaint/12f+.004f+(u+1)*.0375f:
                            paintStrip*.25f+.012f+(u+1)*.113f;
                        float paintT=t;
                        if(paintGuide!=null) {
                            // Preserve the painted highlight height when new
                            // silhouette edges redistribute longitudinal samples.
                            float lo=0,hi=1;
                            for(int k=0;k<14;k++) {
                                float candidate=(lo+hi)*.5f;
                                if(Bezier(paintGuide,candidate).y>path[row].y)lo=candidate;else hi=candidate;
                            }
                            paintT=(lo+hi)*.5f;
                        }
                        paintT=paintStart>0?Mathf.InverseLerp(paintStart,1,paintT):paintT;
                        paintT=Mathf.Lerp(paintOffset,1,paintT);
                        if(tipPaintEase!=1&&paintT>.46f) {
                            float eased=.46f+.54f*Mathf.Pow((paintT-.46f)/.54f,tipPaintEase);
                            paintT=Mathf.Lerp(paintT,eased,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.46f,.62f,paintT)));
                        }
                        int vertex=body.Add(p,shade,bone,new Vector2(paintU,
                            .02f+.94f*Mathf.Clamp01(paintT+.021f*Mathf.Sin(phase))),head,anchored);
                        if(crown>0&&front>=-.001f&&j<sides) {
                            // A shared scalp normal joins the exposed root faces;
                            // the closed inner surface and lower locks retain their own normals.
                            Vector3 normal=V(p.x/(.2365f*.2365f),(p.y-.765f)/(.2195f*.2195f),
                                p.z/(.1865f*.1865f)).normalized;
                            body.normalBlends[vertex]=new Vector4(normal.x,normal.y,normal.z,crown);
                        }
                    }
                }
                if(normalSection) {
                    var surface=body.p.Skip(start).ToArray();int half=sides/2;
                    for(int row=0;row<=rows;row++)for(int j=0;j<=sides;j++) {
                        int column=Mathf.Min(j,sides-j),index=row*(sides+1)+j;
                        Vector3 along=surface[Mathf.Min(rows,row+1)*(sides+1)+column]-
                            surface[Mathf.Max(0,row-1)*(sides+1)+column];
                        Vector3 across=surface[row*(sides+1)+Mathf.Min(half,column+1)]-
                            surface[row*(sides+1)+Mathf.Max(0,column-1)];
                        Vector3 normal=Vector3.Cross(along.normalized,across.normalized).normalized;
                        int center=half/2;
                        Vector3 tipAlong=surface[Mathf.Min(rows,row+1)*(sides+1)+center]-
                            surface[Mathf.Max(0,row-1)*(sides+1)+center];
                        Vector3 tipAcross=surface[row*(sides+1)+center+1]-surface[row*(sides+1)+center-1];
                        Vector3 tipNormal=Vector3.Cross(tipAlong.normalized,tipAcross.normalized).normalized;
                        normal=Vector3.Slerp(normal,tipNormal,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.84f,1,row/(float)rows)));
                        body.p[start+index]+=normal*frontalDepth[index];
                    }
                    for(int row=0;row<=rows;row++)for(int j=0;j<=sides;j++) {
                        float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.88f,row/(float)rows));
                        if(blend<=0)continue;
                        Vector3 along=body.p[start+Mathf.Min(rows,row+1)*(sides+1)+j]-
                            body.p[start+Mathf.Max(0,row-1)*(sides+1)+j];
                        Vector3 across=body.p[start+row*(sides+1)+(j+1)%sides]-
                            body.p[start+row*(sides+1)+(j+sides-1)%sides];
                        Vector3 normal=Vector3.Cross(along.normalized,across.normalized).normalized;
                        body.normalBlends[start+row*(sides+1)+j]=new Vector4(normal.x,normal.y,normal.z,blend);
                    }
                    for(int row=0;row<=rows;row++)body.smoothNormalPairs.Add(new Vector2Int(
                        start+row*(sides+1),start+row*(sides+1)+sides));
                }
                int firstTriangle=body.tri.Count;
                Grid(body,start,rows,sides);
                ReverseFaces(body,firstTriangle);
                if(outward.HasValue) {
                    // Column zero and the closing column already have distinct
                    // vertices. Split the other shared edge for the inward half,
                    // keeping joined exterior normals off the hidden inner faces.
                    for(int k=firstTriangle;k<body.tri.Count;k+=3) {
                        if(Enumerable.Range(k,3).Any(c=>(body.tri[c]-start)%(sides+1)<sides/2))continue;
                        for(int c=k;c<k+3;c++) {
                            int vertex=body.tri[c];
                            if((vertex-start)%(sides+1)!=sides/2)continue;
                            if(!body.splitNormalCorners.ContainsKey(vertex))body.splitNormalCorners[vertex]=new List<int>();
                            body.splitNormalCorners[vertex].Add(c);
                        }
                    }
                }
            }
            public void GuidedCrownTuft(int head) {
                int oldRegion=body.region;body.region=0;
                int rows=FishCrownTuftGuides.Rows,cols=FishCrownTuftGuides.Columns;
                var indices=new int[rows,cols];var centers=new Vector3[rows];
                for(int row=0;row<rows;row++) {
                    for(int col=0;col<cols;col++)centers[row]+=AuthorPoint(FishCrownTuftGuides.Point(row,col))/cols;
                    for(int col=0;col<cols;col++)indices[row,col]=(row==0||row==rows-1)&&col>0?indices[row,0]:
                        body.Add(AuthorPoint(FishCrownTuftGuides.Point(row,col)),Hair,head);
                }
                Action<int,int,int,Vector3> face=(a,b,c,outward)=>{
                    if(a==b||b==c||c==a)return;
                    if(Vector3.Dot(Vector3.Cross(body.p[b]-body.p[a],body.p[c]-body.p[a]),outward)<0){int swap=b;b=c;c=swap;}
                    body.tri.AddRange(new[]{a,b,c});
                };
                for(int row=0;row<rows-1;row++)for(int col=0;col<cols;col++) {
                    int next=(col+1)%cols,a=indices[row,col],b=indices[row+1,col],c=indices[row,next],d=indices[row+1,next];
                    var middle=(centers[row]+centers[row+1])*.5f;
                    face(a,b,c,(body.p[a]+body.p[b]+body.p[c])/3-middle);
                    face(c,b,d,(body.p[c]+body.p[b]+body.p[d])/3-middle);
                }
                body.region=oldRegion;
            }
            public void GuidedFrontPanel(int panel,int sign,int head,int phase) {
                bool cheekReturn=panel==4;
                int rows=cheekReturn?10:panel==0?18:panel==7?18:panel>=8?14:12,cols=cheekReturn?8:4;
                var stations=Enumerable.Range(0,rows+1).Select(row=>panel==0?(row==0?0f:row==1?.010f:row==2?.030f:row==3?.055f:(row-3)/15f):row/(float)rows)
                    .Where(t=>t>=.15f).Concat(new[]{0f,.004f,.016f,.045f,.100f}).Distinct().OrderBy(t=>t).ToArray();
                rows=stations.Length-1;
                int start=body.p.Count,oldRegion=body.region;body.region=0;
                for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++) {
                    float t=stations[row];
                    float across=col<=4?col/4f:(8-col)/4f;
                    Vector3 p=cheekReturn?(col>4&&col<8?FishModelGuides.CheekReturn(t,8-col):FishFrontRootGuides.Surface(panel,1,t,across)):
                        FishFrontRootGuides.Surface(panel,sign,t,across);
                    // A closed cross-section ends in one pole, not a tiny
                    // coplanar line that turns its last return facets inside out.
                    if(cheekReturn&&(row==0||row==rows))p=FishFrontRootGuides.Surface(panel,1,t,.5f);
                    if(cheekReturn)p.x*=sign;
                    // A common crown pole prevents a tiny reversed root fan.
                    if(!cheekReturn&&row==0)p=FishFrontRootGuides.Surface(panel,sign,t,.5f);
                    float paintAcross=cheekReturn?across:sign<0?1-across:across;
                    body.Add(AuthorPoint(p),Color.white*.98f,head,FrontPaintUV(panel,paintAcross,t));
                }
                // Most front locks remain single two-sided sheets. The measured
                // cheek return has real transverse depth, not a coplanar back copy.
                int first=body.tri.Count;Grid(body,start,rows,cols);
                if(!cheekReturn||sign>0)ReverseFaces(body,first);
                body.region=oldRegion;
            }
            public void GuidedChestPanel(int panel,int bone) {
                int head=ids["J_Bip_C_Head"],start=body.p.Count,oldRegion=body.region;body.region=0;
                const int sides=16;var ys=FishChestHairGuides.Stations(panel);
                Func<float,float,Vector3> surface=(height,around)=>FishChestHairGuides.Surface(panel,height,around);
                for(int row=0;row<ys.Length;row++)for(int col=0;col<=sides;col++) {
                    float u=col/(float)sides,y=ys[row];Vector3 final=surface(y,u),author=AuthorPoint(final);
                    float anchored=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.88f,author.y));
                    float paintT=Mathf.InverseLerp(.82f,.405f,final.y),paintU=(1+Mathf.Cos(2*Mathf.PI*u))*.5f;
                    int vertex=body.Add(author,Color.white*.98f,bone,new Vector2(.75f+(panel>=2?1:0)/12f+.004f+paintU*.075f,.02f+.94f*paintT),head,anchored);
                    if(row==0) {
                        float neighbor=ys[1];Vector3 center=Vector3.zero;
                        for(int j=0;j<sides;j++)center+=AuthorPoint(surface(neighbor,j/(float)sides))/sides;
                        Vector3 n=(author-center).normalized;
                        if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,1);
                    // The final thin ring and pole use their actual mesh normals;
                    // derivatives are unstable where a flat section closes.
                    } else if(row<ys.Length-2) {
                        Vector3 along=AuthorPoint(surface(y-.0002f,u))-AuthorPoint(surface(y+.0002f,u));
                        Vector3 across=AuthorPoint(surface(y,u+.001f))-AuthorPoint(surface(y,u-.001f));
                        Vector3 n=Vector3.Cross(across.normalized,along.normalized).normalized;
                        if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,.88f);
                    }
                }
                Grid(body,start,ys.Length-1,sides);
                for(int row=0;row<ys.Length;row++)body.smoothNormalPairs.Add(new Vector2Int(start+row*(sides+1),start+row*(sides+1)+sides));
                body.region=oldRegion;
            }
            public void GuidedChestBacking(int panel,int bone) {
                int head=ids["J_Bip_C_Head"],oldRegion=body.region;body.region=0;
                const int columns=4;var ys=FishChestBackingGuides.Stations(panel);
                Func<float,float,Vector3> surface=(height,around)=>FishChestBackingGuides.Surface(panel,height,around);
                // Front and return share edge positions, UV and weights. They
                // keep distinct normals at the thin folded lip, avoiding a
                // front/back normal cancellation on the broad sheet.
                for(int side=0;side<2;side++) {
                    int start=body.p.Count;
                    for(int row=0;row<ys.Length;row++)for(int col=0;col<=columns;col++) {
                        float u=side==0?col/8f:1-col/8f,y=ys[row];Vector3 final=surface(y,u),author=AuthorPoint(final);
                        float anchored=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.88f,author.y));
                        float paintT=Mathf.InverseLerp(.82f,.405f,final.y),paintU=col/4f;
                        int vertex=body.Add(author,Color.white*.98f,bone,new Vector2(.75f+2f/12f+.004f+paintU*.075f,.02f+.94f*paintT),head,anchored);
                        if(row>0&&row<ys.Length-2) {
                            Vector3 along=AuthorPoint(surface(y-.0002f,u))-AuthorPoint(surface(y+.0002f,u));
                            float lo=side==0?0:.5f,hi=side==0?.5f:1;
                            Vector3 across=AuthorPoint(surface(y,Mathf.Clamp(u+.001f,lo,hi)))-AuthorPoint(surface(y,Mathf.Clamp(u-.001f,lo,hi)));
                            Vector3 n=Vector3.Cross(along.normalized,across.normalized).normalized;
                            if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,.88f);
                        }
                    }
                    int first=body.tri.Count;Grid(body,start,ys.Length-1,columns);if(side==0)ReverseFaces(body,first);
                }
                body.region=oldRegion;
            }
            public void GuidedSideLock(int panel,int bone,bool fork=false) {
                int head=ids["J_Bip_C_Head"],start=body.p.Count,firstFace=body.tri.Count,oldRegion=body.region;body.region=0;
                int columns=fork?4:8,sign=fork?(panel==0?1:-1):(panel==0?-1:1);
                Func<float,float,Vector3> raw=(height,u)=>{
                    if(!fork)return FishSideLockGuides.Outer(panel,height,u);
                    float f=Mathf.Clamp01(u)*4;int col=Mathf.Min(3,(int)f);
                    return Vector3.Lerp(FishRearForkGuides.Point(panel,height,col),FishRearForkGuides.Point(panel,height,col+1),f-col);
                };
                int orientation=sign*(raw(.66f,1).z>raw(.66f,0).z?1:-1);
                Func<int,Vector3> capCorner=edge=>{
                    float angle=edge*360f/RearDomeColumns,front=Mathf.SmoothStep(0,1,Mathf.Cos(angle*Mathf.Deg2Rad));
                    Vector3 p=FishRearCrownGuides.Surface(-5+35*front,angle);
                    return p-(p-V(0,.77f,-.05f)).normalized*(.003f*front);
                };
                Func<float,Vector3> capAtZ=z=>{
                    Vector3 nearest=Vector3.zero;float best=float.PositiveInfinity;int first=sign<0?RearDomeColumns/2:0;
                    for(int edge=first;edge<first+RearDomeColumns/2;edge++) {
                        Vector3 a=capCorner(edge),b=capCorner(edge+1),q=Vector3.Lerp(a,b,Mathf.Clamp01((z-a.z)/(b.z-a.z)));
                        float error=Mathf.Abs(q.z-z);if(error<best){best=error;nearest=q;}
                    }
                    return nearest;
                };
                Func<float,Vector3> root=u=>capAtZ(raw(.745f,u).z);
                Func<float,float,Vector3> surface=(height,u)=>{
                    if(height<=.66f)return raw(height,u);
                    Vector3 top=root(u),lower=raw(.66f,u);if(height>=top.y)return top;
                    float h=top.y-.66f,t=Mathf.Clamp01((height-.66f)/h),t2=t*t,t3=t2*t;
                    Vector3 slope=(raw(.6602f,u)-raw(.6598f,u))/.0004f;
                    Vector3 n=FishRearCrownGuides.Normal(top),topSlope=V(-n.y/n.x,1,0);
                    Vector3 p=(2*t3-3*t2+1)*lower+(t3-2*t2+t)*h*slope+(-2*t3+3*t2)*top+(t3-t2)*h*topSlope;
                    p.y=height;return p;
                };
                var ys=new[]{.745f,.7075f,.68f,.66f}.Concat((fork?FishRearForkGuides.Stations(panel):FishSideLockGuides.Stations(panel)).Where(y=>y<.66f)).ToArray();
                Action<int,Vector3> rootNormal=(vertex,p)=>{Vector3 n=FishRearCrownGuides.Normal(p);float derivative;FitHeight(body.p[vertex].y,out derivative);n.y*=derivative;body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,1);};
                for(int row=0;row<ys.Length;row++)for(int col=0;col<=columns;col++) {
                    float u=col/(float)columns,y=ys[row];Vector3 final=row==0?root(u):surface(y,u);
                    float headWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.74f,final.y));
                    int vertex=body.Add(AuthorPoint(final),Color.white*.98f,bone,RearPaintFinalUV(final),head,headWeight);
                    if(row==0)rootNormal(vertex,final);
                    else if(row<ys.Length-2) {
                        Vector3 along=AuthorPoint(surface(y-.0002f,u))-AuthorPoint(surface(y+.0002f,u));
                        Vector3 across=AuthorPoint(surface(y,Mathf.Min(1,u+.001f)))-AuthorPoint(surface(y,Mathf.Max(0,u-.001f)));
                        Vector3 n=Vector3.Cross(across.normalized,along.normalized).normalized*orientation;
                        if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,.88f);
                    }
                }
                // Add the real cap corners to the attachment border, then
                // zipper to the side ribbon's regular second row.
                var roots=Enumerable.Range(0,columns+1).Select(c=>new KeyValuePair<float,int>(c/(float)columns,start+c)).ToList();
                float left=root(0).z,right=root(1).z;int firstEdge=sign<0?RearDomeColumns/2:0;
                for(int edge=firstEdge+1;edge<firstEdge+RearDomeColumns/2;edge++) {
                    Vector3 p=capCorner(edge);if(p.z<=Mathf.Min(left,right)||p.z>=Mathf.Max(left,right))continue;
                    // Fork columns are not evenly spaced in depth. Invert
                    // the actual boundary parameter before sorting the zipper.
                    float lo=0,hi=1;
                    for(int step=0;step<24;step++){float mid=(lo+hi)*.5f;if((root(mid).z<p.z)==(right>left))lo=mid;else hi=mid;}
                    float u=(lo+hi)*.5f;var near=roots.OrderBy(pair=>Mathf.Abs(pair.Key-u)).First();
                    if(Mathf.Abs(near.Key-u)<.0001f){body.p[near.Value]=AuthorPoint(p);body.uv[near.Value]=RearPaintFinalUV(p);rootNormal(near.Value,p);}
                    else {int vertex=body.Add(AuthorPoint(p),Color.white*.98f,bone,RearPaintFinalUV(p),head,1);rootNormal(vertex,p);roots.Add(new KeyValuePair<float,int>(u,vertex));}
                }
                roots=roots.OrderBy(pair=>pair.Key).ToList();foreach(var pair in roots)rearAttachmentReport.Add(RearAttachmentLine(fork?"fork-root":"side-root",panel,FitPoint(body.p[pair.Value])));
                int upper=0,lowerIndex=0;
                while(upper<roots.Count-1||lowerIndex<columns) {
                    if(upper<roots.Count-1&&(lowerIndex==columns||roots[upper+1].Key<=(lowerIndex+1)/(float)columns)) {body.Face(roots[upper].Value,roots[upper+1].Value,start+columns+1+lowerIndex);upper++;}
                    else {body.Face(roots[upper].Value,start+columns+2+lowerIndex,start+columns+1+lowerIndex);lowerIndex++;}
                }
                Grid(body,start+columns+1,ys.Length-2,columns);if(orientation<0)ReverseFaces(body,firstFace);body.region=oldRegion;
            }
            public void GuidedRearCurtain(int head) {
                int oldRegion=body.region;body.region=0;
                const int columns=40,perLock=8;const float join=.480f,release=.405f;
                int middle=ids["J_Sec_Hair3_01"];
                // One connected upper surface replaces five nearly coincident
                // sheets. Preserve the lower measured locks as separate lobes.
                float left=FishLowerRearGuides.Surface(0,join,0).x,right=FishLowerRearGuides.Surface(4,join,1).x;
                var cuts=new float[6];cuts[0]=0;cuts[5]=1;
                for(int panel=1;panel<5;panel++) {
                    float center=(FishLowerRearGuides.Surface(panel-1,join,.5f).x+FishLowerRearGuides.Surface(panel,join,.5f).x)*.5f;
                    cuts[panel]=Mathf.InverseLerp(left,right,center);
                }
                Func<float,Vector3> capAtX=x=>{
                    Vector3 nearest=Vector3.zero;float best=float.PositiveInfinity;
                    for(int edge=RearDomeColumns/4;edge<RearDomeColumns*3/4;edge++) {
                        Vector3 a=FishRearCrownGuides.Surface(-5,edge*360f/RearDomeColumns),b=FishRearCrownGuides.Surface(-5,(edge+1)*360f/RearDomeColumns);
                        Vector3 q=Vector3.Lerp(a,b,Mathf.Clamp01((x-a.x)/(b.x-a.x)));
                        float error=Mathf.Abs(q.x-x);if(error<best){best=error;nearest=q;}
                    }
                    return nearest;
                };
                Func<float,float,float> xAt=(height,u)=>{
                    float lo=FishRearHairGuides.Surface(0,height,0).x,hi=FishRearHairGuides.Surface(4,height,1).x;
                    if(height<.51f) {
                        float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.51f,join,height));
                        lo=Mathf.Lerp(lo,FishLowerRearGuides.Surface(0,height,0).x,blend);
                        hi=Mathf.Lerp(hi,FishLowerRearGuides.Surface(4,height,1).x,blend);
                    }
                    return Mathf.Lerp(lo,hi,u);
                };
                Func<float,float,Vector3> surface=(height,u)=>{
                    float x=xAt(height,u);Vector3 border=capAtX(x);
                    if(height>=border.y)return border;
                    float z=FishRearEnvelopeGuides.Depth(x,Mathf.Max(join,height));
                    // Stop the common sheet before the first open lobe gaps.
                    // Its attachment tangent continues briefly into each lock.
                    if(height<join)z+=(height-join)*(FishRearEnvelopeGuides.Depth(x,join+.0002f)-FishRearEnvelopeGuides.Depth(x,join))/.0002f;
                    if(height>.66f) {
                        Vector3 n=FishRearCrownGuides.Normal(border);
                        float h=border.y-.66f,t=Mathf.Clamp01((height-.66f)/h),t2=t*t,t3=t2*t;
                        float a=FishRearEnvelopeGuides.Depth(x,.66f),slope=(FishRearEnvelopeGuides.Depth(x,.6602f)-FishRearEnvelopeGuides.Depth(x,.6598f))/.0004f;
                        z=(2*t3-3*t2+1)*a+(t3-2*t2+t)*h*slope+(-2*t3+3*t2)*border.z+(t3-t2)*h*(-n.y/n.z);
                    }
                    return V(x,height,z);
                };
                var parameters=new float[columns+1];
                for(int col=0;col<=columns;col++) {int panel=Mathf.Min(4,col/perLock);parameters[col]=Mathf.Lerp(cuts[panel],cuts[panel+1],(col-panel*perLock)/(float)perLock);}
                float[] ys={.745f,.7075f,.66f,.6325f,.6075f,.5825f,.5575f,.5325f,.5075f,join};
                int start=body.p.Count;
                Action<int,Vector3> rootNormal=(vertex,final)=>{
                    Vector3 n=FishRearCrownGuides.Normal(final);float derivative;FitHeight(body.p[vertex].y,out derivative);n.y*=derivative;
                    body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,1);
                };
                for(int row=0;row<ys.Length;row++)for(int col=0;col<=columns;col++) {
                    float u=parameters[col],y=ys[row];Vector3 final=row==0?capAtX(xAt(.745f,u)):surface(y,u);
                    float headWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.74f,final.y));
                    int vertex=body.Add(AuthorPoint(final),Color.white*.97f,middle,RearPaintFinalUV(final),head,headWeight);
                    if(row==0)rootNormal(vertex,final);
                    else {
                        Vector3 along=AuthorPoint(surface(y-.0003f,u))-AuthorPoint(surface(y+.0003f,u));
                        Vector3 across=AuthorPoint(surface(y,Mathf.Min(1,u+.001f)))-AuthorPoint(surface(y,Mathf.Max(0,u-.001f)));
                        Vector3 n=Vector3.Cross(across.normalized,along.normalized).normalized;
                        if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,.88f);
                    }
                }
                // Keep every actual dome corner along the top attachment.
                var roots=Enumerable.Range(0,columns+1).Select(c=>new KeyValuePair<float,int>(parameters[c],start+c)).ToList();
                for(int edge=RearDomeColumns/4+1;edge<RearDomeColumns*3/4;edge++) {
                    Vector3 q=FishRearCrownGuides.Surface(-5,edge*360f/RearDomeColumns);
                    float a=xAt(.745f,0),b=xAt(.745f,1);if(q.x<=a||q.x>=b)continue;
                    float u=Mathf.InverseLerp(a,b,q.x);var near=roots.OrderBy(pair=>Mathf.Abs(pair.Key-u)).First();
                    if(Mathf.Abs(near.Key-u)<.0001f) {body.p[near.Value]=AuthorPoint(q);body.uv[near.Value]=RearPaintFinalUV(q);rootNormal(near.Value,q);}
                    else {
                        int vertex=body.Add(AuthorPoint(q),Color.white*.97f,middle,RearPaintFinalUV(q),head,1);rootNormal(vertex,q);
                        roots.Add(new KeyValuePair<float,int>(u,vertex));
                    }
                }
                roots=roots.OrderBy(pair=>pair.Key).ToList();
                foreach(var root in roots)rearAttachmentReport.Add(RearAttachmentLine("root",-1,FitPoint(body.p[root.Value])));
                int upper=0,lower=0;
                while(upper<roots.Count-1||lower<columns) {
                    if(upper<roots.Count-1&&(lower==columns||roots[upper+1].Key<=parameters[lower+1])) {
                        body.Face(roots[upper].Value,roots[upper+1].Value,start+columns+1+lower);upper++;
                    } else {body.Face(roots[upper].Value,start+columns+2+lower,start+columns+1+lower);lower++;}
                }
                Grid(body,start+columns+1,ys.Length-2,columns);
                int shared=start+(ys.Length-1)*(columns+1);
                for(int panel=0;panel<5;panel++) {
                    int bone=ids["J_Sec_Hair"+new[]{2,3,3,3,4}[panel]+"_01"];
                    Func<float,float,Vector3> lowerSurface=(height,u)=>{
                        Vector3 measured=FishLowerRearGuides.Surface(panel,height,u);
                        float merge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(release,join,height));
                        return Vector3.Lerp(measured,surface(height,Mathf.Lerp(cuts[panel],cuts[panel+1],u)),merge);
                    };
                    var rows=new[]{join,.46f,.445f,.435f,.425f,.415f,release,.400f}.Concat(FishLowerRearGuides.Stations(panel).Where(y=>y<=.38251f)).ToArray();
                    int[] previous=Enumerable.Range(0,perLock+1).Select(c=>shared+panel*perLock+c).ToArray();
                    for(int row=1;row<rows.Length;row++) {
                        const int lowerColumns=4;var current=new int[lowerColumns+1];float y=rows[row];
                        for(int col=0;col<=lowerColumns;col++) {
                            float u=col/(float)lowerColumns;Vector3 final=lowerSurface(y,u);
                            float rootShare=Mathf.SmoothStep(0,1,Mathf.InverseLerp(release,join,y));
                            int vertex=body.Add(AuthorPoint(final),Color.white*.97f,bone,RearPaintFinalUV(final),middle,bone==middle?0:rootShare);current[col]=vertex;
                            if(y>FishLowerRearGuides.Bottom(panel)+.002f) {
                                Vector3 along=AuthorPoint(lowerSurface(y-.0003f,u))-AuthorPoint(lowerSurface(y+.0003f,u));
                                Vector3 across=AuthorPoint(lowerSurface(y,Mathf.Min(1,u+.001f)))-AuthorPoint(lowerSurface(y,Mathf.Max(0,u-.001f)));
                                Vector3 n=Vector3.Cross(across.normalized,along.normalized).normalized;
                                if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,.88f);
                            }
                        }
                        // Keep the actual eight-segment common root, then use the
                        // five measured transverse controls for each lower lobe.
                        int rootSegment=0,lobeSegment=0,segments=previous.Length-1;
                        while(rootSegment<segments||lobeSegment<lowerColumns) {
                            if(rootSegment<segments&&(lobeSegment==lowerColumns||(rootSegment+1)/(float)segments<=(lobeSegment+1)/(float)lowerColumns)) {
                                body.Face(previous[rootSegment],previous[rootSegment+1],current[lobeSegment]);rootSegment++;
                            } else {body.Face(previous[rootSegment],current[lobeSegment+1],current[lobeSegment]);lobeSegment++;}
                        }
                        previous=current;
                    }
                }
                body.region=oldRegion;
            }
            public void GuidedSplitInner(int head) {
                int oldRegion=body.region;body.region=0;
                float cut=FishInnerRearForkGuides.Cut;
                Func<float,float,Vector3> surface=(y,u)=>{
                    Vector3 old=FishRearHairGuides.Surface(5,y,u);
                    if(y<.51f)old=Vector3.Lerp(old,FishInnerRearForkGuides.Surface(0,y,u),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.51f,.49f,y)));
                    return FishRearCrownGuides.Point(old);
                };
                Func<Vector3,int> add=p=>body.Add(AuthorPoint(p),Color.white*.94f,head,RearPaintFinalUV(p));
                const int columns=4;
                float[] us={0,cut*.5f,cut,(1+cut)*.5f,1};
                float[] ys=new[]{FishRearHairGuides.Top(5),.78f,.75f,.72f,.69f,.66f,.63f,.6075f,.5625f}.Concat(FishInnerRearForkGuides.Stations(0)).ToArray();
                int start=body.p.Count;
                for(int row=0;row<ys.Length;row++)for(int col=0;col<=columns;col++)add(surface(ys[row],us[col]));
                Grid(body,start,ys.Length-1,columns);
                int shared=start+(ys.Length-1)*(columns+1);
                for(int branch=0;branch<2;branch++) {
                    var rows=FishInnerRearForkGuides.Stations(branch+1);
                    int[] previous=Enumerable.Range(0,3).Select(c=>shared+branch*2+c).ToArray();
                    for(int row=0;row<rows.Length;row++) {
                        var current=new int[5];float y=rows[row];
                        for(int col=0;col<5;col++) {
                            float u=col/4f;Vector3 final=FishInnerRearForkGuides.Surface(branch+1,y,u);int vertex=add(final);current[col]=vertex;
                            if(row<rows.Length-1) {
                                Vector3 along=AuthorPoint(FishInnerRearForkGuides.Surface(branch+1,y-.0002f,u))-AuthorPoint(FishInnerRearForkGuides.Surface(branch+1,y+.0002f,u));
                                Vector3 across=AuthorPoint(FishInnerRearForkGuides.Surface(branch+1,y,Mathf.Min(1,u+.001f)))-AuthorPoint(FishInnerRearForkGuides.Surface(branch+1,y,Mathf.Max(0,u-.001f)));
                                Vector3 n=Vector3.Cross(across.normalized,along.normalized).normalized;
                                if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,.88f);
                            }
                        }
                        // Zip the actual two-segment common edge to four lower
                        // segments. Both branches share the same trunk vertices.
                        int upper=0,lower=0,segments=previous.Length-1;
                        while(upper<segments||lower<4) {
                            if(upper<segments&&(lower==4||(upper+1)/(float)segments<=(lower+1)/4f)) {
                                body.Face(previous[upper],previous[upper+1],current[lower]);upper++;
                            } else {body.Face(previous[upper],current[lower+1],current[lower]);lower++;}
                        }
                        previous=current;
                    }
                }
                body.region=oldRegion;
            }
            public void GuidedRearPanel(int panel,int head,bool fork=false) {
                if(!fork&&panel==5){GuidedSplitInner(head);return;}
                int start=body.p.Count,region=body.region;body.region=0;
                float[] stations=fork?FishRearForkGuides.Stations(panel):FishRearHairGuides.Stations(panel);
                bool sideUnderlay=!fork&&(panel==5||panel==10),inner=!fork&&panel>=6&&panel<=10;
                Func<float,float,Vector3> surface=(y,u)=>{
                    Vector3 old=FishRearHairGuides.Surface(panel,y,u);
                    if(!inner||y>=.42f)return old;
                    return Vector3.Lerp(old,FishInnerRearGuides.Surface(panel-6,y,u),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.395f,y)));
                };

                if(sideUnderlay)stations=new[]{FishRearHairGuides.Top(panel),.78f,.75f,.72f,.69f,.66f,.63f}.Concat(stations.Where(y=>y<.63f)).ToArray();
                if(inner)stations=stations.Where(y=>y>.42001f).Concat(FishInnerRearGuides.Stations(panel-6)).ToArray();
                int cols=fork?FishRearForkGuides.Columns(panel):sideUnderlay||inner?4:2;
                int bone=fork?ids["J_Sec_Hair"+FishRearForkGuides.BoneGroup(panel)+"_01"]:head;
                for(int row=0;row<stations.Length;row++)for(int col=0;col<=cols;col++) {
                    Vector3 final=fork?FishRearForkGuides.Point(panel,stations[row],col*4/cols):surface(stations[row],col/(float)cols);
                    if(sideUnderlay)final=FishRearCrownGuides.Point(final);
                    Vector3 p=AuthorPoint(final);float anchored=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.88f,p.y));
                    int vertex=body.Add(p,Color.white*(!fork?.94f:.97f),bone,RearPaintFinalUV(final),head,bone==head?0:anchored);
                    if(inner&&stations[row]<.42f&&row>0&&row<stations.Length-1) {
                        float y=stations[row],u=col/(float)cols;
                        Vector3 along=AuthorPoint(surface(y-.0002f,u))-AuthorPoint(surface(y+.0002f,u));
                        Vector3 across=AuthorPoint(surface(y,Mathf.Min(1,u+.001f)))-AuthorPoint(surface(y,Mathf.Max(0,u-.001f)));
                        Vector3 normal=Vector3.Cross(across.normalized,along.normalized).normalized;
                        if(normal.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(normal.x,normal.y,normal.z,.88f);
                    }

                }
                Grid(body,start,stations.Length-1,cols);body.region=region;
            }
            public void GuidedSideCurl(int panel,int bone,int head) {
                int start=body.p.Count,region=body.region;body.region=0;int sides=FishSideCurlGuides.Sides(panel);
                var ys=FishSideCurlGuides.Stations(panel);
                // The measured upper tip belongs under a different scalp mass.
                // Embed only the hidden attachment into the rebuilt crown.
                Func<float,float,Vector3> surface=(height,around)=>{
                    Vector3 p=FishSideCurlGuides.Surface(panel,height,around);
                    float embed=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.84f,.94f,p.y));
                    p.x*=1-.10f*embed;p.y-=.014f*embed;p.z-=.008f*embed;return p;
                };
                for(int row=0;row<ys.Length;row++)for(int col=0;col<=sides;col++) {
                    float u=col/(float)sides,y=ys[row];Vector3 final=surface(y,u),author=AuthorPoint(final);
                    float headWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.88f,author.y));
                    int vertex=body.Add(author,Color.white*.97f,bone,RearPaintFinalUV(final),head,headWeight);
                    if(row==0||row==ys.Length-1) {
                        float neighbor=ys[row==0?1:ys.Length-2];Vector3 center=Vector3.zero;
                        for(int j=0;j<sides;j++)center+=AuthorPoint(surface(neighbor,j/(float)sides))/sides;
                        Vector3 n=(author-center).normalized;
                        if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,1);
                    } else if(panel>=2||y>FishSideCurlGuides.Bottom(panel)+.004f) {
                        // Closing micro-rings use actual mesh normals. A guide
                        // derivative is unstable next to the collapsed tip.
                        Vector3 along=AuthorPoint(surface(y-.0002f,u))-AuthorPoint(surface(y+.0002f,u));
                        Vector3 across=AuthorPoint(surface(y,u+.001f))-AuthorPoint(surface(y,u-.001f));
                        Vector3 n=Vector3.Cross(across.normalized,along.normalized).normalized;
                        if(n.sqrMagnitude>.5f)body.normalBlends[vertex]=new Vector4(n.x,n.y,n.z,.88f);
                    }
                }
                Grid(body,start,ys.Length-1,sides);
                for(int row=0;row<ys.Length;row++)body.smoothNormalPairs.Add(new Vector2Int(start+row*(sides+1),start+row*(sides+1)+sides));
                body.region=region;
            }
            public void HairMantle(int head) {
                const int rows=14,cols=40;
                // Concentrate the unchanged station budget near the crown arc.
                float[] stations={0f,.025f,.060f,.105f,.160f,.240f,.320f,.400f,.500f,.600f,.700f,.790f,.870f,.940f,1f};
                int[] breaks={0,8,15,20,25,32,40};
                float[] tips={.125f,.098f,.090f,.088f,.100f,.130f};
                for(int layer=0;layer<1;layer++) {
                    int layerStart=body.p.Count;
                    for(int row=0;row<=rows;row++)for(int i=0;i<=cols;i++) {
                        float t=stations[row],theta=Mathf.Lerp(1.10f,Mathf.PI*2-1.10f,i/(float)cols);
                        float side=Mathf.Abs(theta-Mathf.PI)/(Mathf.PI-1.10f);
                        float y=.978f-.490f*t-.130f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,1,t))
                            -Mathf.Lerp(.205f,.240f,side*side)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.80f,1,t));
                        float scalp=Mathf.Sqrt(Mathf.Max(.01f,1-Mathf.Pow((y-.765f)/.215f,2)));
                        float lower=Mathf.InverseLerp(.765f,.488f,y);
                        float rx=y>.765f?.232f*scalp+.002f:.234f-.030f*Mathf.SmoothStep(0,1,lower);
                        float rz=y>.765f?.182f*scalp+.002f:.184f+.024f*lower;
                        float innerHem=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.488f,.358f,y));
                        rx-=.020f*innerHem;rz-=.010f*innerHem;
                        float undercut=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,.75f,y))*.08f;
                        rx*=1-undercut;rz*=1-undercut;
                        float fold=Mathf.Cos(theta*12+t*.6f),thickness=layer*.005f;
                        rx-=thickness;rz-=thickness;
                        float tip=Mathf.Pow(t,12)*(.018f+Mathf.Lerp(.014f,.040f,side*side)*(.5f+.5f*Mathf.Cos(theta*11)));
                        Vector3 p=V(Mathf.Sin(theta)*(rx+.007f*fold*t),y+tip,
                            Mathf.Cos(theta)*(rz+.006f*fold*t));
                        // The backing curtain has broad, longer lobes below the
                        // shorter outer locks. Reuse its grid rather than stack
                        // more narrow strips on top of the existing hair.
                        int lobe=0;while(lobe<5&&i>breaks[lobe+1])lobe++;
                        float across=Mathf.InverseLerp(breaks[lobe],breaks[lobe+1],i);
                        float peak=lobe==2?.60f:lobe==3?.40f:.50f;
                        float tipDistance=across<peak?across/peak:(1-across)/(1-peak);
                        float rounded=Mathf.Pow(Mathf.Max(0,Mathf.Sin(tipDistance*Mathf.PI*.5f)),1.10f);
                        float hem=Mathf.Lerp(.195f-.045f*side,tips[lobe],rounded);
                        float extension=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,t));
                        p=SweepHairSection(p,HairSweepAtAngle(theta)*1.25f);
                        p.y+=extension*(hem-(.978f-.490f-.130f-Mathf.Lerp(.205f,.240f,side*side)+
                            .018f+Mathf.Lerp(.014f,.040f,side*side)*(.5f+.5f*Mathf.Cos(theta*11))));
                        float fullness=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.67f,.88f,t));
                        // A fixed lower angular frame keeps the side curtain
                        // behind the shoulder without twisting across uneven tips.
                        float lowerAngle=theta+Mathf.Sign(Mathf.Sin(theta))*.85f*
                            Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,1,side));
                        Vector3 lowerSection=V(Mathf.Sin(lowerAngle)*(.265f+.025f*side-.014f*extension),
                            .40f,Mathf.Cos(lowerAngle)*(.250f-.014f*extension));
                        p.x=Mathf.Lerp(p.x,lowerSection.x,fullness);
                        p.z=Mathf.Lerp(p.z,lowerSection.z,fullness);
                        float paintT=Mathf.InverseLerp(.98f,.09f,p.y);
                        Color color=Color.white*(.96f-.025f*(1-rounded));color.a=1;
                        body.Add(p,color,head,
                            new Vector2(.75f+1/12f+.004f+across*.075f,.02f+.94f*paintT));
                    }
                    Grid(body,layerStart,rows,cols);
                }
            }
            public void Disk(Geo g, Vector2 center, Vector2 size, Color color, int bone,
                float offset, string feature = "", int steps = 28) {
                int start = g.p.Count;
                // Concentric rings follow the cheek surface. A single triangle fan
                // cuts through the curved face and needs an excessive depth offset.
                int rings = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(size.x,size.y)/.010f));
                for (int row = 0; row <= rings; row++) {
                    float r = row / (float)rings;
                    for (int i = 0; i <= steps; i++) {
                        float angle = i * Mathf.PI * 2 / steps;
                        float sine=Mathf.Sin(angle);
                        // A flatter upper contour keeps the white inside the
                        // upper eyelid while the lower eye stays rounded.
                        if(feature=="eye" && sine>0) sine=.85f*Mathf.Pow(sine,.55f);
                        Vector2 xy = center + Vector2.Scale(size, new Vector2(Mathf.Cos(angle), sine)) * r;
                        Color surfaceColor=color;
                        if(feature=="eye") {
                            float lidShadow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.90f,(xy.y-center.y)/size.y));
                            surfaceColor=Color.Lerp(color,Hex("D8C9D3"),lidShadow*.30f);
                        }
                        int index = g.Add(FacePoint(xy.x, xy.y, offset), surfaceColor, bone);
                        if (feature != "") features.Add(Tuple.Create(index, V(center.x, center.y, offset), feature));
                    }
                }
                Grid(g,start,rings,steps);
            }
            public void FaceLine(Vector2[] xy, float radius, Color color, int bone, string feature = "", float? eyeCenter = null,
                float startScale = 1, float endScale = 1) {
                int start = face.p.Count;
                Func<float,float> thickness=t=>radius*Mathf.Lerp(startScale,endScale,t);
                Sweep(face, xy.Select(p => FacePoint(p.x, p.y, .0035f)).ToArray(), thickness, thickness, t=>color, bone, 6);
                if(feature=="lash") {
                    float center=eyeCenter??xy.Average(p=>p.x);
                    var closed=new Geo();
                    var path=xy.Select(p=>{
                        float y=.698f-.008f*(1-Mathf.Clamp01(Mathf.Pow((p.x-center)/.050f,2)));
                        return FacePoint(p.x,y,.0035f);
                    }).ToArray();
                    // Rebuild the closed cross-section instead of flattening every
                    // vertex onto one y value, which creates a degenerate eyelid.
                    Func<float,float> closedThickness=t=>Mathf.Min(radius,.0021f)*Mathf.Lerp(startScale,endScale,t);
                    Sweep(closed,path,closedThickness,closedThickness,t=>color,bone,6);
                    for(int i=0;i<closed.p.Count;i++)closedLashes[start+i]=closed.p[i];
                    var smiling=new Geo();
                    var smilePath=xy.Select(p=>{
                        float y=.694f+.016f*(1-Mathf.Clamp01(Mathf.Pow((p.x-center)/.050f,2)));
                        return FacePoint(p.x,y,.0035f);
                    }).ToArray();
                    Sweep(smiling,smilePath,closedThickness,closedThickness,t=>color,bone,6);
                    for(int i=0;i<smiling.p.Count;i++)smilingLashes[start+i]=smiling.p[i];
                }
                if (feature != "") for (int i = start; i < face.p.Count; i++)
                    features.Add(Tuple.Create(i, V(xy.Average(p => p.x), .700f, 0), feature));
            }
            Vector2 EyeInputPoint(Vector3 registered) {
                float width=FishModelGuides.Point(V(1,registered.y,0),1).x,center=Mathf.Sign(registered.x)*.085f;
                float x=center+(registered.x/width-center)/1.08f;
                float y=.700f+(AuthorPoint(registered).y-.700f)/1.10f;return new Vector2(x,y);
            }
            public void GuidedUpperEyeLine(int sign,int bone) {
                int start=face.p.Count,columns=FishEyeContourGuides.Columns(sign);const int rows=2;
                for(int row=0;row<=rows;row++)for(int col=0;col<columns;col++) {
                    Vector3 lower=FishEyeContourGuides.Point(sign,col,false),upper=FishEyeContourGuides.Point(sign,col,true);
                    float center=(lower.y+upper.y)*.5f,half=Mathf.Max((upper.y-lower.y)*.5f,.000075f);
                    Vector3 final=Vector3.Lerp(lower,upper,row/(float)rows);final.y=center+(row-rows*.5f)*half;
                    Vector2 p=EyeInputPoint(final);int vertex=face.Add(FacePoint(p.x,p.y,.0035f),Hex("211D27"),bone);
                    features.Add(Tuple.Create(vertex,V(sign*.085f,.700f,0),"lash"));
                    float arc=1-Mathf.Clamp01(Mathf.Pow((p.x-sign*.085f)/.050f,2));
                    float thickness=.0014f*(row-rows*.5f);
                    closedLashes[vertex]=FacePoint(p.x,.698f-.008f*arc+thickness,.0035f);
                    smilingLashes[vertex]=FacePoint(p.x,.694f+.016f*arc+thickness,.0035f);
                }
                int first=face.tri.Count;Grid(face,start,rows,columns-1);
                if(sign<0)ReverseFaces(face,first);
            }
            public void GuidedEyeWhite(int sign,int bone) {
                const int rows=4;int columns=FishEyeWhiteGuides.Columns(sign);int[] previous=null;
                for(int col=0;col<columns;col++) {
                    Vector3 lower=FishEyeWhiteGuides.Point(sign,col,false),upper=FishEyeWhiteGuides.Point(sign,col,true);
                    bool pole=col==0||col==columns-1;var current=new int[pole?1:rows+1];
                    for(int row=0;row<current.Length;row++) {
                        float v=pole?.5f:row/(float)rows;Vector2 xy=EyeInputPoint(Vector3.Lerp(lower,upper,v));
                        float shade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,v));
                        Color color=Color.Lerp(Hex("FFFCFA"),Hex("D8C9D3"),shade*.30f);
                        int vertex=face.Add(FacePoint(xy.x,xy.y,.0015f),color,bone);current[row]=vertex;
                        features.Add(Tuple.Create(vertex,V(sign*.085f,.696f,.0015f),"eye"));
                    }
                    if(previous!=null) {
                        if(previous.Length==1)for(int row=0;row<rows;row++)face.Face(previous[0],current[row],current[row+1]);
                        else if(pole)for(int row=0;row<rows;row++)face.Face(previous[row],current[0],previous[row+1]);
                        else for(int row=0;row<rows;row++){face.Face(previous[row],current[row],current[row+1]);face.Face(previous[row],current[row+1],previous[row+1]);}
                    }
                    previous=current;
                }
            }
            // Opaque skin behind the opening fills the procedural closed eye.
            // Neutral eye surfaces cover it; the existing head bone drives it.
            public void GuidedEyeSocket(int sign,int bone) {
                int oldRegion=body.region;body.region=1;const int rows=2;
                int columns=FishEyeWhiteGuides.Columns(sign);int[] previous=null;
                for(int col=0;col<columns;col++) {
                    Vector3 lower=FishEyeWhiteGuides.Point(sign,col,false),upper=FishEyeWhiteGuides.Point(sign,col,true);
                    bool pole=col==0||col==columns-1;var current=new int[pole?1:rows+1];
                    for(int row=0;row<current.Length;row++) {
                        float v=pole?.5f:row/(float)rows;Vector2 xy=EyeInputPoint(Vector3.Lerp(lower,upper,v));
                        Vector3 surface=EyeProportionPoint(FacePoint(xy.x,xy.y,0)),point=surface;point.z-=.008f;
                        float blush=Mathf.Exp(-Mathf.Pow((Mathf.Abs(surface.x)-.126f)/.048f,2)
                            -Mathf.Pow((surface.y-.655f)/.029f,2))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.12f,surface.z));
                        current[row]=body.Add(point,Color.Lerp(Hex("FFF2E8"),Hex("F2B8AB"),blush*.88f),bone);
                    }
                    if(previous!=null) {
                        if(previous.Length==1)for(int row=0;row<rows;row++)body.Face(previous[0],current[row],current[row+1]);
                        else if(pole)for(int row=0;row<rows;row++)body.Face(previous[row],current[0],previous[row+1]);
                        else for(int row=0;row<rows;row++){body.Face(previous[row],current[row],current[row+1]);body.Face(previous[row],current[row+1],previous[row+1]);}
                    }
                    previous=current;
                }
                body.region=oldRegion;
            }
            public void PaintedIris(int sign,int bone) {
                int start=face.p.Count;const int columns=32,rows=2;
                Vector3 center=FishIrisGuides.Center(sign);Vector2 inputCenter=EyeInputPoint(center);
                var clip=Enumerable.Range(0,columns).Select(col=>FishEyeWhiteGuides.Clip(sign,center,FishIrisGuides.Rim(sign,col))).ToArray();
                Debug.Log("Iris opening clip "+sign+": "+clip.Count(t=>t<.99999f)+"/"+columns+", min radius "+clip.Min());
                for(int row=0;row<=rows;row++)for(int col=0;col<(row==0?1:columns);col++) {
                    float radius=row/(float)rows*clip[col],angle=col*Mathf.PI*2/columns;
                    Vector2 xy=EyeInputPoint(Vector3.Lerp(center,FishIrisGuides.Rim(sign,col),radius));
                    int vertex=face.Add(FacePoint(xy.x,xy.y,.0052f),Color.white,bone,
                        IrisPaintUV(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius));
                    features.Add(Tuple.Create(vertex,V(inputCenter.x,inputCenter.y,.0052f),"iris"));
                }
                for(int col=0;col<columns;col++) {
                    int next=(col+1)%columns;face.Face(start,start+1+col,start+1+next);
                    int inner=start+1,outer=inner+columns;
                    face.Face(inner+col,outer+col,outer+next);face.Face(inner+col,outer+next,inner+next);
                }
            }
            public void Mouth(int bone) {
                int start=face.p.Count;const int rows=2,cols=32;
                for(int row=0;row<=rows;row++)for(int j=0;j<(row==0?1:cols);j++) {
                    float radius=row/(float)rows,angle=j/(float)cols*Mathf.PI*2;
                    float u=Mathf.Cos(angle)*radius,v=Mathf.Sin(angle)*radius;
                    int index=face.Add(FacePoint(.020f*u,.6195f+.0008f*u*u+.0009f*v,.0035f),Hex("AE7C7B"),bone);
                    features.Add(Tuple.Create(index,V(u,v,0),"mouth"));
                }
                for(int j=0;j<cols;j++)face.Face(start,start+1+j,start+1+(j+1)%cols);
                for(int row=1;row<rows;row++)for(int j=0;j<cols;j++) {
                    int a=start+1+(row-1)*cols+j,b=start+1+row*cols+j;
                    int an=start+1+(row-1)*cols+(j+1)%cols,bn=start+1+row*cols+(j+1)%cols;
                    face.Face(a,b,bn);face.Face(a,bn,an);
                }
            }
            public void Expressions(Mesh mesh) {
                foreach (string name in new[] { "Fcl_EYE_Close", "Fcl_ALL_Joy", "Fcl_ALL_Fun", "Fcl_ALL_Sorrow", "Fcl_ALL_Surprised", "Fcl_EYE_Joy" }) {
                    var delta = new Vector3[face.p.Count];
                    foreach (var feature in features) {
                        int i = feature.Item1; Vector3 p = face.p[i], c = feature.Item2;
                        string kind = feature.Item3;
                        Vector3 target = p;
                        if (name == "Fcl_EYE_Close" || name == "Fcl_EYE_Joy") {
                            if (kind == "eye" || kind == "iris" || kind == "pupil" || kind == "shine") {
                                if(name=="Fcl_EYE_Joy") {
                                    float lid=.694f+.016f*(1-Mathf.Clamp01(Mathf.Pow((p.x-Mathf.Sign(p.x)*.085f)/.050f,2)));
                                    // Keep eye width during the slower smile transition;
                                    // collapse vertically into the curved lid, just under skin.
                                    target=FacePoint(p.x,lid,-.001f);
                                } else {
                                    // Retain width and follow the closing lid.
                                    // Collapsing into a deep central point hid
                                    // all eye paint before the lid was closed.
                                    float lid=.698f-.008f*(1-Mathf.Clamp01(Mathf.Pow((p.x-Mathf.Sign(p.x)*.085f)/.050f,2)));
                                    target=FacePoint(p.x,lid,-.001f);
                                }
                            }
                            if (kind == "lash") target = name=="Fcl_EYE_Joy" ? smilingLashes[i] : closedLashes[i];
                        }
                        if(kind=="mouth"&&name.StartsWith("Fcl_ALL_")) {
                            float u=c.x,v=c.y,x=.020f*u,y=.6195f+.0008f*u*u+.0009f*v;
                            if(name=="Fcl_ALL_Joy") {x=.027f*u;y=.625f+.010f*u*u+.0075f*v;}
                            if(name=="Fcl_ALL_Fun") {x=.021f*u;y=.625f+.004f*u*u+.0012f*v+.002f*u;}
                            if(name=="Fcl_ALL_Sorrow") {x=.018f*u;y=.630f-.006f*u*u+.0012f*v;}
                            if(name=="Fcl_ALL_Surprised") {x=.010f*u;y=.628f+.011f*v;}
                            target=FacePoint(x,y,.0035f);
                        }
                        if(kind=="brow") {
                            float inward=Mathf.Clamp(-Mathf.Sign(c.x)*(p.x-c.x)/.031f,-1,1);
                            if(name=="Fcl_ALL_Joy")target.y+=.002f+.002f*(1-inward*inward);
                            if(name=="Fcl_ALL_Fun")target.y+=(c.x>0?.001f:-.001f)+.0015f*inward;
                            if(name=="Fcl_ALL_Sorrow")target.y+=.004f+.009f*inward;
                            if(name=="Fcl_ALL_Surprised")target.y+=.008f+.003f*(1-inward*inward);
                            target.z+=FacePoint(target.x,target.y,0).z-FacePoint(p.x,p.y,0).z;
                        }
                        delta[i] = target - p;
                    }
                    mesh.AddBlendShapeFrame(name, 100, delta, new Vector3[delta.Length], new Vector3[delta.Length]);
                }
            }

            Vector3 EyeProportionPoint(Vector3 p) {
                float center=Mathf.Sign(p.x)*.085f;
                float x=center+(p.x-center)*1.08f,y=.700f+(p.y-.700f)*1.10f;
                return V(x,y,p.z+FacePoint(x,y,0).z-FacePoint(p.x,p.y,0).z);
            }
            void ShapeEyes(Mesh mesh) {
                var indices=new HashSet<int>(features.Where(f=>f.Item3=="eye"||f.Item3=="iris"||
                    f.Item3=="pupil"||f.Item3=="shine"||f.Item3=="lash").Select(f=>f.Item1));
                var source=mesh.vertices;var mapped=(Vector3[])source.Clone();
                foreach(int i in indices)mapped[i]=EyeProportionPoint(source[i]);
                var frames=new List<Tuple<string,float,Vector3[]>>();
                for(int shape=0;shape<mesh.blendShapeCount;shape++) {
                    var delta=new Vector3[source.Length];mesh.GetBlendShapeFrameVertices(shape,0,delta,null,null);
                    foreach(int i in indices)delta[i]=EyeProportionPoint(source[i]+delta[i])-mapped[i];
                    frames.Add(Tuple.Create(mesh.GetBlendShapeName(shape),mesh.GetBlendShapeFrameWeight(shape,0),delta));
                }
                mesh.ClearBlendShapes();mesh.vertices=mapped;mesh.RecalculateNormals();
                foreach(var frame in frames)mesh.AddBlendShapeFrame(frame.Item1,frame.Item2,frame.Item3,
                    new Vector3[source.Length],new Vector3[source.Length]);
                mesh.RecalculateBounds();
            }

            public GameObject Finish() {
                Directory.CreateDirectory(Folder);
                WriteHairPaint();
                AssetDatabase.Refresh();
                var textureImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"/HairPaint.png");
                textureImporter.sRGBTexture=true;textureImporter.mipmapEnabled=true;
                textureImporter.npotScale=TextureImporterNPOTScale.None;textureImporter.maxTextureSize=2048;
                textureImporter.alphaSource=TextureImporterAlphaSource.FromInput;textureImporter.alphaIsTransparency=true;
                textureImporter.mipMapsPreserveCoverage=true;textureImporter.alphaTestReferenceValue=.5f;
                textureImporter.textureCompression=TextureImporterCompression.Uncompressed;
                textureImporter.wrapMode=TextureWrapMode.Clamp;textureImporter.SaveAndReimport();
                var paint=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/HairPaint.png");
                var shader = Shader.Find("DesktopFish/Rebuilt Toon");
                var exports = new List<MeshDump>();
                for (int part = 0; part < 2; part++) {
                    var geo = part == 0 ? body : face;
                    var mesh = geo.Build(part == 0 ? "Authored body" : "Authored expressions", root.transform, bones.ToArray());
                    if (part == 1) { Expressions(mesh); ShapeEyes(mesh); }
                    FitProportions(mesh);
                    FishModelGuides.Apply(mesh,geo.regions.ToArray());
                    mesh=CompactMesh(mesh);
                    if(part==0)mesh=FishMeshOptimizer.ReduceRebuilt(mesh,17500,ids["J_Bip_C_Head"],
                        ids.Where(pair=>new[]{"_Hand","_Thumb","_Index","_Middle","_Ring","_Little"}.Any(partName=>pair.Key.Contains(partName))).Select(pair=>pair.Value).ToArray());
                    var shapes = new List<ShapeDump>();
                    for(int i=0;i<mesh.blendShapeCount;i++) {
                        var deltas = new Vector3[mesh.vertexCount];
                        mesh.GetBlendShapeFrameVertices(i,0,deltas,null,null);
                        shapes.Add(new ShapeDump { name=mesh.GetBlendShapeName(i), deltas=deltas });
                    }
                    exports.Add(new MeshDump {name=mesh.name, vertices=mesh.vertices, normals=mesh.normals, colors=mesh.colors, uv=mesh.uv,
                        triangles=mesh.triangles,bone_indices=mesh.boneWeights.Select(w=>w.boneIndex0).ToArray(),
                        secondary_bone_indices=mesh.boneWeights.Select(w=>w.boneIndex1).ToArray(),
                        primary_bone_weights=mesh.boneWeights.Select(w=>w.weight0).ToArray(),shapes=shapes.ToArray()});
                    string path = Folder + (part == 0 ? "/Body.asset" : "/Face.asset");
                    // Replace the generated mesh natively; CopySerialized does not reliably
                    // invalidate skinning/GPU buffers after a topology-size change.
                    if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(mesh, path);
                    string matPath = Folder + (part == 0 ? "/Toon.mat" : "/Face.mat");
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, matPath); }
                    mat.shader = shader; mat.SetFloat("_Flat", part);
                    mat.SetTexture("_MainTex",paint);
                    mat.SetFloat("_OutlineWidth", part == 0 ? .00065f : 0);
                    var child = new GameObject(part == 0 ? "Body and hair" : "Face");
                    child.transform.SetParent(root.transform, false);
                    var skin = child.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMesh = mesh; skin.bones = bones.ToArray(); skin.rootBone = bones[0];
                    skin.sharedMaterial = mat; skin.updateWhenOffscreen = true;
                    EditorUtility.SetDirty(mesh); EditorUtility.SetDirty(mat);
                }
                int totalTriangles=exports.Sum(m=>m.triangles.Length)/3;
                if(totalTriangles>20000)throw new InvalidOperationException("Rebuilt model exceeds 20,000-triangle budget: "+totalTriangles);
                root.AddComponent<FishRigProfile>();
                Directory.CreateDirectory("work");
                File.WriteAllText("work/rebuilt-geometry.json",JsonUtility.ToJson(new ModelDump {
                    bones=bones.Select(b=>new BoneDump {name=b.name,parent=bones.IndexOf(b.parent),position=b.position}).ToArray(),
                    meshes=exports.ToArray()
                }));
                File.WriteAllText("work/rebuilt-budget.json",JsonUtility.ToJson(new BudgetDump { total=exports.Sum(m=>m.triangles.Length)/3,
                    parts=budgets.Concat(new[]{new PartBudget{name="face-expressions",triangles=exports[1].triangles.Length/3}}).ToArray()},true));
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/BigFish.prefab");
                File.WriteAllText(Folder + "/provenance.json", "{\n  \"generator\": \"FishRebuiltModel.cs\",\n  \"source_3d_assets_loaded\": false,\n  \"sparse_reference_controls\": true,\n  \"triangle_budget\": 20000,\n  \"paint_size\": [1024, 2048],\n  \"body_vertices\": " + exports[0].vertices.Length + ",\n  \"face_vertices\": " + exports[1].vertices.Length + ",\n  \"triangles\": " + exports.Sum(m=>m.triangles.Length) / 3 + ",\n  \"bones\": " + bones.Count + "\n}\n");
                return root;
            }
        }

        // Rounded visual landmarks from the local reference review: align the
        // main crown and soles, then lower the chin/eyes and shorten the torso.
        // The generated mesh, rig and morph targets share this monotone height map.
        // No source mesh data is needed to build the model.
        static readonly float[] GuideSourceY={.0702f,.190f,.401f,.556f,.586f,.650f,.760f,.980f};
        static readonly float[] GuideTargetY={.0702f,.180f,.414f,.522f,.547f,.628f,.738f,.980f};
        static float GuideSlope(int i) {
            // Keep the eye/lash band rigid in height while lowering its position.
            if(i==0||i==5||i==6||i==GuideSourceY.Length-1)return 1;
            float a=GuideSourceY[i]-GuideSourceY[i-1],b=GuideSourceY[i+1]-GuideSourceY[i];
            float left=(GuideTargetY[i]-GuideTargetY[i-1])/a;
            float right=(GuideTargetY[i+1]-GuideTargetY[i])/b;
            float w1=2*b+a,w2=b+2*a;
            return (w1+w2)/(w1/left+w2/right);
        }
        static float FitHeight(float y,out float derivative) {
            derivative=1;
            if(y<=GuideSourceY[0]||y>=GuideSourceY[GuideSourceY.Length-1])return y;
            int i=0;while(i+1<GuideSourceY.Length-1&&y>GuideSourceY[i+1])i++;
            float h=GuideSourceY[i+1]-GuideSourceY[i],t=(y-GuideSourceY[i])/h,t2=t*t,t3=t2*t;
            float a=GuideTargetY[i],b=GuideTargetY[i+1],ma=GuideSlope(i),mb=GuideSlope(i+1);
            derivative=((6*t2-6*t)*a+(-6*t2+6*t)*b)/h+(3*t2-4*t+1)*ma+(3*t2-2*t)*mb;
            return (2*t3-3*t2+1)*a+(t3-2*t2+t)*h*ma+(-2*t3+3*t2)*b+(t3-t2)*h*mb;
        }
        static Vector3 FitPoint(Vector3 p) {float d;p.y=FitHeight(p.y,out d);return p;}
        static void FitProportions(Mesh mesh) {
            var source=mesh.vertices;var normals=mesh.normals;
            var mapped=new Vector3[source.Length];
            for(int i=0;i<source.Length;i++) {
                float derivative;mapped[i]=source[i];mapped[i].y=FitHeight(source[i].y,out derivative);
                normals[i]=V(normals[i].x,normals[i].y/derivative,normals[i].z).normalized;
            }
            var frames=new List<Tuple<string,float,Vector3[]>>();
            for(int shape=0;shape<mesh.blendShapeCount;shape++)for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++) {
                var delta=new Vector3[source.Length];mesh.GetBlendShapeFrameVertices(shape,frame,delta,null,null);
                for(int i=0;i<delta.Length;i++)delta[i]=FitPoint(source[i]+delta[i])-mapped[i];
                frames.Add(Tuple.Create(mesh.GetBlendShapeName(shape),mesh.GetBlendShapeFrameWeight(shape,frame),delta));
            }
            mesh.ClearBlendShapes();mesh.vertices=mapped;mesh.normals=normals;
            foreach(var frame in frames)mesh.AddBlendShapeFrame(frame.Item1,frame.Item2,frame.Item3,
                new Vector3[source.Length],new Vector3[source.Length]);
            mesh.RecalculateBounds();
        }

        static void Grid(Geo g, int start, int rows, int cols) {
            for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++) {
                int a = start + y * (cols + 1) + x, b = a + cols + 1;
                g.Face(a, a + 1, b); g.Face(a + 1, b + 1, b);
            }
        }
        static readonly float[] JawLevels={.586f,.595f,.605f,.625f,.650f,.675f,.700f};
        static readonly float[] JawDepths={.130f,.142f,.150f,.158f,.160f,.153f,
            .166f*Mathf.Sqrt(1-Mathf.Pow((.700f-.773f)/.187f,2))};
        static readonly float[] JawSlopes={1.4f,.95f,.55f,.25f,0,-.10f,
            .166f*(.773f-.700f)/(.187f*.187f*Mathf.Sqrt(1-Mathf.Pow((.700f-.773f)/.187f,2)))};
        // Width changes are concentrated in the exposed face. Retain a fuller
        // lower cheek while narrowing the eye-side contour and small chin.
        static readonly float[] FaceWidthY={.547f,.5575f,.570f,.595f,.620f,.645f,.670f,.695f,.740f,.820f};
        static readonly float[] FaceWidth={.88f,.89f,1.01f,1.04f,.98f,.92f,.89f,.89f,.91f,1};
        static float FaceWidthSlope(int i) {
            if(i==0||i==FaceWidth.Length-1)return 0;
            float a=FaceWidthY[i]-FaceWidthY[i-1],b=FaceWidthY[i+1]-FaceWidthY[i];
            float l=(FaceWidth[i]-FaceWidth[i-1])/a,r=(FaceWidth[i+1]-FaceWidth[i])/b;
            if(l*r<=0)return 0;
            return 3*(a+b)/((2*b+a)/l+(b+2*a)/r);
        }
        static float FaceWidthScale(float authorY) {
            float derivative,y=FitHeight(authorY,out derivative);
            if(y<=FaceWidthY[0])return FaceWidth[0];
            if(y>=FaceWidthY[FaceWidthY.Length-1])return 1;
            int i=0;while(y>FaceWidthY[i+1])i++;
            float h=FaceWidthY[i+1]-FaceWidthY[i],t=(y-FaceWidthY[i])/h,t2=t*t,t3=t2*t;
            return (2*t3-3*t2+1)*FaceWidth[i]+(t3-2*t2+t)*h*FaceWidthSlope(i)+
                (-2*t3+3*t2)*FaceWidth[i+1]+(t3-t2)*h*FaceWidthSlope(i+1);
        }
        static float JawForward(float y) {
            // The lower face turns forward into the chin rather than rolling
            // underneath an ellipsoid. Keep the eye band and crown unchanged.
            if(y>=.700f)return 0;
            int i=0;while(i<JawLevels.Length-2&&y>JawLevels[i+1])i++;
            float h=JawLevels[i+1]-JawLevels[i],t=Mathf.Clamp01((y-JawLevels[i])/h),t2=t*t,t3=t2*t;
            float front=(2*t3-3*t2+1)*JawDepths[i]+(t3-2*t2+t)*h*JawSlopes[i]
                +(-2*t3+3*t2)*JawDepths[i+1]+(t3-t2)*h*JawSlopes[i+1];
            float radius=.166f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((y-.773f)/.187f,2)));
            return front-radius;
        }
        static Vector3 ShapeHeadPoint(Vector3 p) {
            float radius=.166f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((p.y-.773f)/.187f,2)));
            // Keep XY anchors while measured front/rear fields reshape depth.
            // A separate sparse-section neck loft supplies hidden closure.
            float forward=radius<.001f?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.5f,.5f,p.z/radius));
            float raw=p.z,jaw=JawForward(p.y),derivative;float finalY=FitHeight(p.y,out derivative);
            p.z+=jaw*forward;
            p.z=FishHeadSideGuides.Depth(p.z,raw,radius,jaw,finalY);
            p.x*=FaceWidthScale(p.y);
            float registeredX=FishModelGuides.Point(V(p.x,finalY,p.z),1).x;
            p.z=FishFrontSkinGuides.Depth(p.z,registeredX,finalY,radius<.001f?0:raw/radius);
            return p;
        }
        static Vector3 FacePoint(float x, float y, float offset) {
            // Facial marks keep their authored XY placement while following
            // the narrowed skin surface, including all expression targets.
            float sourceX=x/FaceWidthScale(y);
            float depth = Mathf.Sqrt(Mathf.Max(.05f, 1 - sourceX * sourceX / (.214f * .214f) - (y - .773f) * (y - .773f) / (.187f * .187f)));
            var p=ShapeHeadPoint(V(sourceX,y,.166f*depth));p.x=x;p.z+=offset;return p;
        }
        static float RearHairParameter(float t) => t<=1f/3 ? t*.75f : .25f+(t-1f/3)*1.125f;
        static int HairRows(int authored) => Mathf.Max(8,Mathf.CeilToInt(authored*.68f));

        static Vector3 Bezier(Vector3[] p, float t) => Mathf.Pow(1 - t, 3) * p[0] +
            3 * (1 - t) * (1 - t) * t * p[1] + 3 * (1 - t) * t * t * p[2] + t * t * t * p[3];
        static Vector3[] Curve(Vector3[] points, int steps) {
            int segments=(points.Length-1)/3;
            return Enumerable.Range(0,steps+1).Select(i=>{
                float t=i/(float)steps*segments;int segment=Mathf.Min(segments-1,(int)t);
                return Bezier(points.Skip(segment*3).Take(4).ToArray(),t-segment);
            }).ToArray();
        }

        // A small authored UV atlas supplies paint detail without adding hair geometry.
        // Three fringe strips and three narrower cyan-ended layouts prevent locks
        // repeating the same highlights. The upper-right swatch stays neutral white.
        static void WriteHairPaint() {
            const int size=1024;
            const int laceHeight=128;
            var texture=new Texture2D(size,PaintHeight,TextureFormat.RGBA32,false);
            var pixels=new Color[size*PaintHeight];
            float[] brushCenters={.08f,.48f,.88f};
            float[] brushStarts={.56f,.68f,.61f};
            float[] brushWidths={.43f,.35f,.40f};
            float[] reflectionCenters={.48f,.52f,.46f},reflectionHeights={.33f,.35f,.34f},reflectionBends={.08f,-.06f,.05f};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float ax=x/(float)(size-1),ay=y/(float)(size-1);
                int strip=Mathf.Min(3,(int)(ax*4));bool cyan=strip==3;
                float u=Mathf.Clamp01((ax-strip*.25f-.012f)/.226f);
                int longPaint=cyan?Mathf.Min(2,(int)((ax-.75f)*12)):0;
                if(cyan)u=Mathf.Clamp01((ax-.75f-longPaint/12f-.004f)/.075f);
                float t=Mathf.Clamp01((ay-.02f)/.94f);
                float edge=Mathf.Pow(Mathf.Abs(u*2-1),8);
                Color baseColor=Color.Lerp(Hex("2B4B9E"),Hex("365DB6"),Mathf.Sin(t*Mathf.PI)*.50f);
                if(!cyan)baseColor=Color.Lerp(Hex("1E3888"),Hex("486FB3"),Mathf.Sin(t*Mathf.PI)*.65f);
                float ends=cyan?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.56f,.85f,t))*.96f:
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,1,t));
                if(cyan) {
                    // Broad, tapered brush shapes grow upward from the tips.
                    // Changing their height and bend per layout keeps the lower
                    // silhouette readable without a comb of evenly spaced lines.
                    float brush=0;
                    for(int k=0;k<brushCenters.Length;k++) {
                        float shift=.035f*Mathf.Sin(k*2.3f+longPaint*2.1f);
                        float top=brushStarts[(k+longPaint)%brushStarts.Length]+.025f*Mathf.Sin(k+longPaint);
                        brush=Mathf.Max(brush,PaintBrush(u,t,brushCenters[k]+shift,top,
                            brushWidths[k],.045f*Mathf.Sin(k*1.7f+longPaint)));
                    }
                    ends=Mathf.Max(ends,brush*.94f);
                }
                baseColor=Color.Lerp(baseColor,cyan?Hex("67C0F0"):Hex("8C8CB7"),ends);
                if(cyan) {
                    // A few blue tongues extend into the cyan, separating broad
                    // painted masses without outlining every individual strand.
                    float blue=PaintBrush(u,1-t,.24f+longPaint*.065f,.045f,.072f,-.035f);
                    blue=Mathf.Max(blue,PaintBrush(u,1-t,.72f-longPaint*.055f,.10f,.047f,.025f));
                    blue*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.59f,.75f,t));
                    baseColor=Color.Lerp(baseColor,Hex("345391"),blue*.38f);
                }
                if(cyan) {
                    // Wide curved reflection bands describe the joined upper
                    // mass; the lower brush shapes still separate individual tips.
                    float center=reflectionCenters[longPaint];
                    float height=reflectionHeights[longPaint];
                    float bend=reflectionBends[longPaint];
                    float du=(u-center-bend*Mathf.Sin((t-.14f)*Mathf.PI*2))/1.7f;
                    float dt=(t-height-.035f*Mathf.Cos((u-.5f)*Mathf.PI))/.16f;
                    float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,.18f,t))*
                        (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.58f,t)));
                    float reflection=Mathf.Exp(-du*du*1.4f-dt*dt)*envelope;
                    baseColor=Color.Lerp(baseColor,Hex("5475BC"),reflection*.40f);
                }
                // Slightly wandering broad streaks and finer tip strokes retain
                // strand detail without a uniform comb of vertical stripes.
                float strand=Mathf.Sin(u*43+t*3+Mathf.Sin(t*7+u*5))*.006f+
                    Mathf.Sin(u*19-t*2)*.012f;
                float edgeContrast=cyan?Mathf.Lerp(.008f,.055f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.58f,t))):.06f;
                baseColor*=1-edge*edgeContrast+strand*(cyan?.40f:1);
                if(!cyan)baseColor*=1-.15f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.93f,1,Mathf.Abs(u*2-1)));
                // Three spaced fine parting strokes replace the repeated
                // broad vertex-darkened channels on the joined rear surface.
                if(cyan&&longPaint==1) {
                    float part=Mathf.InverseLerp(.09f,.49f,t);
                    float center=.10f+.08f*Mathf.Sin(part*Mathf.PI);
                    float width=.010f+.005f*Mathf.Sin(part*Mathf.PI);
                    float line=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,Mathf.Abs(u-center)/width));
                    float endsFade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.07f,part))*
                        (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,1,part)));
                    baseColor=Color.Lerp(baseColor,Hex("182C63"),line*endsFade*.68f);
                }
                float glints=0;
                if(!cyan) {
                    // Unequal slanted brushes and short joined pools describe
                    // the curved fringe without repeating round dot clusters.
                    if(strip==0) {
                        glints=Mathf.Max(PaintSpot(u,t,.29f,.44f,.26f,.035f,-.2f),
                            PaintSpot(u,t,.52f,.455f,.15f,.042f,-.15f));
                        glints=Mathf.Max(glints,PaintSpot(u,t,.76f,.44f,.025f,.03f,.2f));
                    } else if(strip==1) {
                        glints=Mathf.Max(PaintSpot(u,t,.26f,.41f,.045f,.10f,.8f),
                            PaintSpot(u,t,.42f,.44f,.07f,.05f,.50f));
                        glints=Mathf.Max(glints,PaintSpot(u,t,.56f,.43f,.033f,.03f,.3f));
                    } else {
                        glints=Mathf.Max(PaintSpot(u,t,.29f,.43f,.055f,.065f,.8f),
                            PaintSpot(u,t,.43f,.46f,.06f,.045f,.6f));
                        glints=Mathf.Max(glints,PaintSpot(u,t,.65f,.44f,.026f,.034f,.3f));
                    }
                }
                float fineTip=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*64+t*4)),14)*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.94f,t))*(1-ends*.5f);
                if(!cyan) {
                    // Short blue-violet strokes retain painted tip separation
                    // without silver threads running far up every lock.
                    float stroke=PaintBrush(u,t,.17f+.025f*strip,.77f,.043f,.035f);
                    stroke=Mathf.Max(stroke,.72f*PaintBrush(u,t,.43f-.020f*strip,.84f,.028f,-.025f));
                    stroke=Mathf.Max(stroke,.84f*PaintBrush(u,t,.78f-.015f*strip,.80f,.039f,-.020f));
                    fineTip=stroke*(1-ends*.5f);
                }
                baseColor=Color.Lerp(baseColor,Hex("8BA6D5"),glints*.60f);
                baseColor=Color.Lerp(baseColor,cyan?Hex("94DBE7"):Hex("7989BB"),fineTip*(cyan?.035f:.10f));
                baseColor.a=1;
                pixels[(y+laceHeight)*size+x]=(ax>.98f&&ay>.98f)?Color.white:baseColor;
            }
            // Authored cutout tiles: continuous curved sheets retain volume,
            // while rounded lace scallops are resolved in the alpha channel.
            int[] scallops={16,20,8,12};
            for(int y=0;y<laceHeight;y++)for(int x=0;x<size;x++) {
                int tile=x/256;float u=(x%256-8)/239f,r=(y-16)/96f;
                float wave=Mathf.Repeat(u*scallops[tile],1)*2-1;
                float edge=(tile==1?.66f:.78f)+(tile==1?.32f:.20f)*Mathf.Sqrt(Mathf.Max(0,1-wave*wave));
                float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(edge-.012f,edge+.012f,r));
                float fold=.94f+.06f*Mathf.Cos(u*scallops[tile]*Mathf.PI*2);
                if(tile==0) pixels[y*size+x]=FishHeadbandGuides.Paint(u,r);
                else if(tile==1) {
                    float phase=Mathf.Repeat(u*scallops[tile],1);
                    float crease=Mathf.Exp(-Mathf.Pow(Mathf.Min(phase,1-phase)/.10f,2));
                    float shade=1-.10f*crease*Mathf.Sin(Mathf.Clamp01(r)*Mathf.PI*.8f);
                    Color cloth=new Color(shade,shade,1,alpha);
                    float border=1-Mathf.SmoothStep(0,1,Mathf.Abs(r-edge+.012f)/.018f);
                    Color tint=Color.Lerp(cloth,new Color(.70f,.76f,.88f,alpha),border*.45f);
                    tint.a=alpha;pixels[y*size+x]=tint;
                } else if(tile==2) {
                    // Broad fabric lobes, a narrow lavender stitched edge and
                    // shallow pleat creases are authored inside the alpha strip.
                    float phase=Mathf.Repeat(u*scallops[tile],1),crease=Mathf.Exp(-Mathf.Pow(Mathf.Min(phase,1-phase)/.10f,2));
                    float shade=.99f-.11f*crease*Mathf.Sin(r*Mathf.PI*.8f);
                    Color cloth=new Color(shade,shade,1,alpha);
                    float border=1-Mathf.SmoothStep(0,1,Mathf.Abs(r-edge+.013f)/.021f);
                    Color tint=Color.Lerp(cloth,new Color(.65f,.71f,.86f,alpha),border*.72f);
                    tint.a=alpha;pixels[y*size+x]=tint;
                } else pixels[y*size+x]=new Color(fold,fold,fold,alpha);
            }
            // A continuous rear field decouples the broad cyan masses from the
            // nine geometric lock patches. Legacy hair/lace pixels stay intact.
            for(int y=0;y<RearPaintHeight;y++) {
                float t=y/(float)(RearPaintHeight-1),height=1.03f-t*.96f;
                var spans=new Vector2[5];
                for(int panel=0;panel<5;panel++) {
                    float sample=Mathf.Min(.807f,height),blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.53f,height));
                    float cap=Mathf.Sqrt(Mathf.Max(.003f,1-Mathf.Pow(Mathf.Max(0,height-.807f)/.185f,2)));
                    float left=Mathf.Lerp(FishLowerRearGuides.Surface(panel,Mathf.Min(.50f,sample),0).x,FishRearHairGuides.Surface(panel,sample,0).x,blend);
                    float right=Mathf.Lerp(FishLowerRearGuides.Surface(panel,Mathf.Min(.50f,sample),1).x,FishRearHairGuides.Surface(panel,sample,1).x,blend);
                    spans[panel]=new Vector2(left,right)*(height>.807f?cap:1);
                }
                for(int x=0;x<size;x++) {
                    float u=x/(float)(size-1);
                    // Preserve the neutral swatch and every other atlas chart.
                    pixels[(LegacyPaintHeight+y)*size+x]=u>.98f?Color.white:RearPaintColor((u-.5f)*.82f,height,spans);
                }
            }
            PaintApron(pixels);
            PaintIris(pixels);
            PaintBib(pixels);
            PaintSideLace(pixels);
            PaintFrontPanels(pixels);
            PaintFrontSkin(pixels);
            texture.SetPixels(pixels);texture.Apply();
            File.WriteAllBytes(Folder+"/HairPaint.png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        static Vector2 RearPaintUV(Vector3 p) {
            if(p.y<.585f)p.y=.585f-(.585f-p.y)*.88f;
            p=FishModelGuides.Point(FitPoint(p),2);
            return RearPaintFinalUV(p);
        }
        static Vector2 RearPaintFinalUV(Vector3 p) {
            float u=Mathf.Clamp01(.5f+p.x/.82f),t=Mathf.Clamp01((1.03f-p.y)/.96f);
            return new Vector2(u,(LegacyPaintHeight+t*(RearPaintHeight-1)-128)/1024f);
        }
        // Unequal tapered brush returns follow the overall fall of the hair.
        // Broad cyan shading and narrow accents have separate scales, so the
        // small desktop view does not become a row of solid blue fence posts.
        static readonly float[] RearBlueCenters={-.291f,-.254f,-.222f,-.197f,-.165f,-.128f,-.101f,-.060f,-.026f,.009f,.043f,.079f,.106f,.144f,.178f,.215f,.253f,.288f};
        static readonly float[] RearBlueWidths={.0035f,.0028f,.0045f,.0025f,.0040f,.0030f,.0050f,.0028f,.0032f,.0025f,.0044f,.0032f,.0046f,.0027f,.0038f,.0030f,.0040f,.0025f};
        static Color RearPaintColor(float x,float y,Vector2[] spans) {
            float crest=FishRearPaintGuides.Height(x);
            float mass=(FishRearPaintGuides.Height(x,true)*4+
                2*FishRearPaintGuides.Height(x-.012f,true)+2*FishRearPaintGuides.Height(x+.012f,true)+
                FishRearPaintGuides.Height(x-.026f,true)+FishRearPaintGuides.Height(x+.026f,true))/10;
            float cyan=Mathf.SmoothStep(0,1,Mathf.InverseLerp(mass+.014f,mass-.083f,y));
            // Sparse higher tongues are painted on top of the broad gradient;
            // their contrast fades into it, rather than cutting a hard band.
            float tongue=Mathf.SmoothStep(0,1,Mathf.InverseLerp(crest+.002f,crest-.015f,y));
            cyan=Mathf.Max(cyan,tongue*.72f);
            // Independent broad painted volume follows the measured lock
            // widths. This reads at pet size without extra overlay geometry.
            float crown=Mathf.Exp(-Mathf.Pow((y-.755f)/.185f,2));
            Color blue=Color.Lerp(Hex("24438C"),Hex("4A78C6"),crown*.85f);
            float volume=0,seam=0;
            for(int panel=0;panel<spans.Length;panel++) {
                float width=spans[panel].y-spans[panel].x;
                if(width<.002f)continue;
                float u=(x-spans[panel].x)/width;
                if(u>=0&&u<=1)volume=Mathf.Max(volume,Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.65f));
                if(panel<spans.Length-1&&panel!=2) {
                    float edge=(spans[panel].y+spans[panel+1].x)*.5f;
                    // The narrow middle pair forms a shared parting, rather
                    // than two long parallel ink lines down the center.
                    if(panel==1)edge=(edge+(spans[2].y+spans[3].x)*.5f)*.5f;
                    seam=Mathf.Max(seam,Mathf.Exp(-Mathf.Pow((x-edge)/.00075f,2)));
                }
            }
            float volumeGate=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.23f,.48f,y));
            blue*=1-(1-volume)*(.085f+.055f*crown)*volumeGate;
            Color color=Color.Lerp(blue,Hex("64B6F4"),cyan*.96f);
            color*=1-(1-volume)*cyan*.085f;
            for(int i=0;i<RearBlueCenters.Length;i++) {
                float foot=.193f+.023f*Mathf.Sin(i*2.17f),top=FishRearPaintGuides.Height(RearBlueCenters[i])+.020f;
                float along=Mathf.Clamp01((y-foot)/(top-foot));
                float bend=.014f*Mathf.Sin(i*1.73f)*(1-along)+.009f*Mathf.Sin(i*2.31f)*Mathf.Sin(along*Mathf.PI);
                float width=RearBlueWidths[i]*Mathf.Pow(along,.72f)+.0002f;
                float brush=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,Mathf.Abs(x-RearBlueCenters[i]-bend)/width));
                float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.16f,along))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,1,along)));
                color=Color.Lerp(color,Hex("345391"),brush*fade*cyan*.64f);
            }
            float upper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.62f,y))*
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.90f,.965f,y)));
            color=Color.Lerp(color,Hex("172B60"),seam*upper*.40f);color.a=1;
            return color;
        }

        // Independent 104x296 alpha tile in an audited unused chart region.
        // Planar front XY stays exact; the rear uses an opaque tile edge.
        static Vector2 FrontSkinUV(Vector3 registered,bool frontal) {
            float u=frontal?Mathf.Clamp01((registered.x+.24f)/.48f):0;
            float v=Mathf.Clamp01((registered.y-.54f)/.40f);
            return new Vector2((652+96*u)/PaintWidth,(792+280*v-128)/1024f);
        }
        static bool IsFrontSkinUV(Vector2 uv)=>uv.x>=648f/PaintWidth&&uv.x<=752f/PaintWidth&&
            uv.y>=784f/PaintHeight&&uv.y<=1080f/PaintHeight;
        static void PaintFrontSkin(Color[] pixels) {
            // Fresh planar alpha apertures, generated from sparse opening
            // controls. RGB stays white so existing skin/blush colors survive.
            for(int y=784;y<1080;y++)for(int x=648;x<752;x++) {
                Vector3 p=V(-.24f+.48f*(x-652)/96f,.54f+.40f*(y-792)/280f,0);
                int sign=p.x<0?-1:1;float alpha=1;
                if(FishEyeWhiteGuides.Contains(sign,p)) {
                    Vector2 bounds=FishEyeWhiteGuides.Bounds(sign,p.x);
                    float d=Mathf.Min(p.y-bounds.x,bounds.y-p.y,
                        p.x-FishEyeWhiteGuides.Point(sign,0,false).x,
                        FishEyeWhiteGuides.Point(sign,FishEyeWhiteGuides.Columns(sign)-1,false).x-p.x);
                    alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.0024f,d));
                }
                pixels[y*PaintWidth+x]=new Color(1,1,1,alpha);
            }
        }
        // A 320px authored tile reuses a verified empty part of the apron
        // field, with a wide gutter from all existing UV charts/white swatches.
        // Fine iris bands and glints live in paint instead of overlay polygons.
        static Vector2 IrisPaintUV(float u,float v)=>new Vector2((736+145*u)/PaintWidth,(1856+145*v-128)/1024f);
        static float PaintEllipse(float x,float y,float cx,float cy,float rx,float ry) {
            float distance=Mathf.Sqrt(Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2));
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.97f,1.03f,distance));
        }
        static Vector2 FrontPaintUV(int panel,float u,float t) {
            return new Vector2((panel%6*128+16+96*u)/1024f,(panel/6*320+16+288*t)/1024f);
        }
        static void PaintFrontPanels(Color[] pixels) {
            for(int y=128;y<LegacyPaintHeight;y++)for(int x=0;x<768;x++)pixels[y*PaintWidth+x]=Hex("2C457F");
            for(int panel=0;panel<17;panel++) {
                int ox=panel%6*128,oy=128+panel/6*320;
                for(int py=0;py<320;py++) {
                    float t=Mathf.Clamp01((py+.5f-16)/288);var row=new Vector3[5];
                    for(int col=0;col<5;col++)row[col]=FishModelGuides.FrontPanel(panel,1,t,col/4f);
                    for(int px=0;px<128;px++) {
                        float u=Mathf.Clamp01((px+.5f-16)/96),f=u*4;int j=Mathf.Min(3,(int)f);
                        Vector3 p=Vector3.Lerp(row[j],row[j+1],f-j);
                        float mass=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.8f);
                        Color color=Color.Lerp(Hex("1A388B"),Hex("365FAC"),mass*.70f);
                        bool fringe=panel>=8&&panel<=14;
                        float ends=Mathf.SmoothStep(0,1,Mathf.InverseLerp(fringe?.57f:.70f,fringe?.98f:1,t));
                        color=Color.Lerp(color,fringe?Hex("ACA4D0"):Hex("8A8ABB"),ends*(fringe?.86f:.78f));
                        float filament=Mathf.Sin(u*21+t*2.3f+panel*.41f)*.0035f;
                        color*=1-Mathf.Pow(Mathf.Abs(u*2-1),3)*.075f+filament;
                        float glint=FishFrontPaintGuides.Coverage(panel,new Vector2(p.x,p.y));
                        color=Color.Lerp(color,Hex("7FA8E8"),glint*.72f);
                        float stroke=PaintBrush(u,t,.20f+(panel%3)*.10f,.79f,.021f,.025f);
                        stroke=Mathf.Max(stroke,PaintBrush(u,t,.72f-(panel%2)*.08f,.84f,.016f,-.02f));
                        color=Color.Lerp(color,Hex("526DA4"),stroke*.24f);
                        if(panel==0||panel==4||(panel>=7&&panel<=14)) {
                            float ink=Mathf.Exp(-Mathf.Pow(u/.014f,2))+Mathf.Exp(-Mathf.Pow((u-1)/.014f,2));
                            float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.16f,.30f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,1,t)));
                            color=Color.Lerp(color,Hex("15285B"),ink*fade*.38f);
                        }
                        color.a=1;
                        pixels[(oy+py)*PaintWidth+ox+px]=color;
                    }
                }
            }
        }

        static Vector2 SideLaceUV(float t,float r) {
            return new Vector2((936+80*r)/1024f,(1864+144*t-128)/1024f);
        }
        // A dedicated chart preserves the other lace tiles and adds broad,
        // irregular gathered folds plus an independently authored cutout edge.
        static void PaintSideLace(Color[] pixels) {
            float[] folds={.418f,.435f,.451f,.467f,.482f,.496f,.513f};
            for(int py=1856;py<2016;py++)for(int px=928;px<1024;px++) {
                float t=Mathf.Clamp01((py+.5f-1864)/144),r=Mathf.Clamp01((px+.5f-936)/80);
                float y=Mathf.Lerp(.4102f,.522f,t),shade=0,light=0;
                for(int k=0;k<folds.Length;k++) {
                    float d=y-folds[k]+.007f*(r-.35f)*(r-.35f)-.002f*(r-.35f)*Mathf.Sin(k*2.1f);
                    float width=.0031f+.00050f*Mathf.Sin(k*1.7f);
                    shade+=Mathf.Exp(-d*d/(width*width))*(.20f+.42f*r);
                    light+=Mathf.Exp(-(d-.003f)*(d-.003f)/.000010f)*.022f;
                }
                float seam=Mathf.Exp(-Mathf.Pow((r-.065f)/.045f,2))*.24f;
                Color color=Color.Lerp(Hex("F9FAFF"),Hex("BFC4DA"),Mathf.Clamp01(shade+seam-light));
                float edge=.965f+.035f*Mathf.Sin(t*Mathf.PI*13+.50f*Mathf.Sin(t*9));
                color.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(edge-.010f,edge+.010f,r));
                pixels[py*PaintWidth+px]=color;
            }
        }

        static Vector2 BibPaintUV(float u,float v) {
            return new Vector2((936+80*u)/1024f,(1704+112*v-128)/1024f);
        }
        // Independent cloth painting: broad pintuck shading and readable oval
        // buttons replace small faceted spheres, without copying source pixels.
        static void PaintBib(Color[] pixels) {
            for(int py=1696;py<1824;py++) {
                float v=Mathf.Clamp01((py+.5f-1704)/112),y=Mathf.Lerp(.416f,.514f,v);
                float width=FishBibSurfaceGuides.Point(y,1).x-FishBibSurfaceGuides.Point(y,-1).x;
                var creases=new float[6];for(int i=0;i<6;i++)creases[i]=FishBibSurfaceGuides.CreaseU(i,y);
                for(int px=928;px<1024;px++) {
                    float u=Mathf.Clamp01((px+.5f-936)/80)*2-1;
                    float shade=0,light=0;
                    for(int i=0;i<6;i++) {
                        float d=(u-creases[i])*width*.5f;
                        float falloff=.60f+.40f*Mathf.Sin(Mathf.PI*v);
                        shade+=Mathf.Exp(-d*d/.000022f)*.025f*falloff;
                        shade+=Mathf.Exp(-d*d/.0000018f)*.010f;
                        light+=Mathf.Exp(-(d-.003f)*(d-.003f)/.000012f)*.016f*falloff;
                    }
                    float hem=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.09f,v));
                    float gathers=0;
                    foreach(float center in new[]{-.72f,-.36f,.37f,.74f}) {
                        float d=u-center-.06f*v;
                        gathers+=Mathf.Exp(-d*d/.00065f)*.065f*(1-Mathf.SmoothStep(0,1,v/.16f));
                    }
                    Color color=White*(.985f-shade+light-hem*.035f-gathers);color.a=1;
                    // Preserve the measured centers; broaden the painted ovals
                    // so their dark cores survive the actual small pet view.
                    foreach(var oval in FishBibSurfaceGuides.Buttons) {
                        Vector3 c=oval[0];float cx=FishBibSurfaceGuides.Across(c.x,y);
                        float dx=(u-cx)*width*.5f/(1.25f*(oval[2].x-oval[1].x))*2;
                        float dy=(y-c.y)/(1.55f*(oval[4].y-oval[3].y))*2;
                        float r=Mathf.Sqrt(dx*dx+dy*dy);
                        float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.80f,1.10f,r));
                        Color button=Color.Lerp(Hex("1F2949"),Hex("354367"),Mathf.Clamp01(dy*.5f+.5f)*.48f);
                        float glint=Mathf.Exp(-((dx+.28f)*(dx+.28f)/.12f+(dy-.35f)*(dy-.35f)/.14f));
                        button=Color.Lerp(button,Hex("8EABC7"),glint*.15f);
                        color=Color.Lerp(color,button,edge);
                    }
                    color.a=1;pixels[py*PaintWidth+px]=color;
                }
            }
        }

        static void PaintIris(Color[] pixels) {
            for(int y=1696;y<2016;y++)for(int x=576;x<896;x++) {
                float u=(x+.5f-736)/145f,v=(y+.5f-1856)/145f,r=Mathf.Sqrt(u*u+v*v);
                float arc=v+.34f*u*u;
                Color color=Color.Lerp(Hex("263762"),Hex("344D85"),Mathf.Clamp01((1-v)*.55f));
                float blue=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.04f,.02f,arc));
                color=Color.Lerp(color,Hex("4B9ED7"),blue);
                float cyan=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.56f,-.50f,arc));
                color=Color.Lerp(color,Hex("8FE0F3"),cyan);
                float pool=PaintEllipse(u,v,0,-1.02f,.70f,.37f);
                color=Color.Lerp(color,Hex("EEFFFF"),pool);
                color=Color.Lerp(color,Hex("24345F"),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,.995f,r))*.75f);
                float reflected=PaintEllipse(u,v,-.48f,.62f,.28f,.19f);
                color=Color.Lerp(color,Hex("7189B6"),reflected*.30f);
                float pupil=PaintEllipse(u,v,0,-.02f,.245f,.33f);
                color=Color.Lerp(color,Hex("25365F"),pupil);
                float main=PaintEllipse(u,v,.30f,.57f,.225f,.225f);
                float small=PaintEllipse(u,v,-.14f,.63f,.080f,.080f);
                float lower=PaintEllipse(u,v,-.69f,-.68f,.055f,.055f);
                color=Color.Lerp(color,Color.white,Mathf.Max(main,Mathf.Max(small,lower)));
                color.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.997f,1.005f,r));
                pixels[y*PaintWidth+x]=color;
            }
        }

        static Vector2 LaceUV(int tile,float u,float r) {
            if(tile==0)u=FishHeadbandGuides.TextureU(u);
            float center=tile==0?.5f:0;
            return new Vector2((tile*256+8+center+239*u)/1024f,(16+center+96*r-128)/1024f);
        }
        static Vector3 AuthorPoint(Vector3 final) {
            float lo=-.5f,hi=1.5f;
            for(int i=0;i<28;i++) {float mid=(lo+hi)*.5f,d;if(FitHeight(mid,out d)<final.y)lo=mid;else hi=mid;}
            final.y=(lo+hi)*.5f;if(final.y<.585f)final.y=.585f-(.585f-final.y)/.88f;
            return final;
        }
        static float PaintSpot(float u,float t,float x,float y,float rx,float ry,float tilt) {
            float dx=(u-x-tilt*(t-y))/rx,dy=(t-y)/ry;
            float distance=dx*dx+dy*dy;
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1,distance));
        }
        static float PaintBrush(float u,float t,float center,float start,float width,float bend) {
            float p=Mathf.Clamp01((t-start)/(1-start));
            float radius=width*Mathf.Pow(p,.60f)+.00001f;
            float x=center+bend*Mathf.Sin(p*Mathf.PI*.8f);
            float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,1,Mathf.Abs(u-x)/radius));
            return edge*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.06f,p));
        }

        public static GameObject Create()
        {
            var a = new Author();
            int hips = a.Bone("J_Bip_C_Hips", -1, V(0, .30f, 0));
            int spine = a.Bone("J_Bip_C_Spine", hips, V(0, .39f, 0));
            int chest = a.Bone("J_Bip_C_Chest", spine, V(0, .48f, 0));
            int neck = a.Bone("J_Bip_C_Neck", chest, V(0, .55f, 0));
            int head = a.Bone("J_Bip_C_Head", neck, V(0, .773f, 0));
            a.body.region=1;
            a.Sphere(a.body, V(0, .773f, 0), V(.214f, .187f, .166f), Skin, head, 32, 20);
            for(int row=0;row<=20;row++)a.body.smoothNormalPairs.Add(new Vector2Int(row*33,row*33+32));
            for(int i=0;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];
                // Exact coincident pole positions let cleanup remove the
                // collapsed fan faces after the jaw stretches forward.
                if(i>=20*33)p=V(0,.773f-.187f,0);
                p=ShapeHeadPoint(p);
                float blush=Mathf.Exp(-Mathf.Pow((Mathf.Abs(p.x)-.126f)/.048f,2)
                    -Mathf.Pow((p.y-.655f)/.029f,2))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.12f,p.z));
                a.body.colors[i]=Color.Lerp(Hex("FFF2E8"),Hex("F2B8AB"),blush*.88f);
                a.body.p[i]=p;
                // Front uses planar XY; the existing rear seam collapses to
                // the same tile's opaque edge. Guarded charts retain this UV map.
                a.body.uv[i]=FrontSkinUV(FishModelGuides.Point(FitPoint(p),1),i%33<=16);
            }
            a.body.region=0;
            a.GuidedNeck(neck,head);
            int torsoStart=a.body.p.Count;
            a.Sphere(a.body, V(0, .460f, 0), V(.120f, .108f, .080f), Navy, chest);
            for(int i=torsoStart;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];
                if(Mathf.Abs(Mathf.Abs(p.y-.460f)-.108f)<.000001f){p.x=0;p.z=0;}
                Vector3 mapped=ChestVolumePoint(p);
                mapped.y=mapped.y<.585f?.585f-(.585f-mapped.y)*.88f:mapped.y;mapped=FitPoint(mapped);
                Vector3 edge=FishModelGuides.Bib(mapped.y,1),lace=FishModelGuides.BibLace(mapped.y,0);
                float x=Mathf.Abs(mapped.x),target;
                if(x<=edge.x)target=FishModelGuides.Bib(mapped.y,x/edge.x).z-.003f;
                else target=Mathf.Lerp(edge.z-.003f,lace.z-.003f,Mathf.InverseLerp(edge.x,lace.x,x));
                float coverage=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.045f,p.z));
                mapped.z=Mathf.Lerp(mapped.z,target-.018f,coverage);
                a.body.p[i]=AuthorGarmentPoint(mapped);
            }
            a.Mark("head-and-torso");
            a.body.region=2;BuildHair(a, head);a.body.region=0;a.Mark("hair");
            a.face.region=1;BuildFace(a, head);a.Mark("eye-socket-skin-backing");
            BuildDress(a, hips, chest);a.Mark("dress-apron-lace-gold");
            BuildLimbs(a, hips, chest);a.Mark("limbs-hands-shoes");
            BuildAccessories(a, head, chest, hips);a.Mark("headband-bows-fins-ribbon");

            // Shape the default upper garment as one volume. The chest share
            // of the sleeve roots follows it; bone/skin attachment data stays fixed.
            for(int i=0;i<a.body.p.Count;i++) {
                var w=a.body.weights[i];var p=a.body.p[i];
                float chestShare=(w.boneIndex0==chest?w.weight0:0)+(w.boneIndex1==chest?w.weight1:0);
                if(chestShare>0)p=Vector3.Lerp(p,ChestVolumePoint(p),chestShare);
                if(w.boneIndex0==neck) {
                    float t=Mathf.InverseLerp(.520f,.590f,p.y);
                    p.z-=.025f*Mathf.Sin(t*Mathf.PI);
                }
                a.body.p[i]=p;
            }
            // Compress the body continuously below the neck. Head and facial morph
            // coordinates stay intact; garment, hair ends and joints share the map.
            Func<Vector3,Vector3> compact=p=>V(p.x,p.y<.585f?.585f-(.585f-p.y)*.88f:p.y,p.z);
            for(int i=0;i<a.body.p.Count;i++)a.body.p[i]=compact(a.body.p[i]);
            var positions=a.bones.Select(b=>FitPoint(compact(b.position))).ToArray();
            for(int i=0;i<a.bones.Count;i++)a.bones[i].position=positions[i];
            return a.Finish();
        }

        [MenuItem("Desktop Fish/Rebuilt Model/Render Preview")]
        public static void Preview() {
            FishBuild.UseRebuilt = true;
            FishBuild.RenderMotionPreview();
            File.Copy("Preview.png", "work/rebuilt-idle.png", true);
            File.Copy("work/motion-preview.png", "work/rebuilt-gestures.png", true);
        }

        [MenuItem("Desktop Fish/Rebuilt Model/Build macOS App")]
        public static void Build() {
            FishBuild.UseRebuilt = true;
            FishBuild.BuildMac();
        }

        [MenuItem("Desktop Fish/Rebuilt Model/Review Geometry")]
        public static void Review() { Preview(); RenderAngles(); }

        public static void ReviewAndBuild() { Review(); Build(); }

        [MenuItem("Desktop Fish/Rebuilt Model/Review Default Pose")]
        public static void RenderIdle() { RenderAngles(false); }
        public static void RenderIdleLaceStudy() { RenderAngles(false,false,false,false,true); }
        public static void RenderIdleFinStudy() { RenderAngles(false,false,false,false,false,false,false,true); }
        public static void RenderIdleFrontStudy() { RenderAngles(false,false,false,false,false,true); }
        public static void RenderIdleSurfaceStudy() { RenderAngles(false,false,true); }
        public static void RenderIdleCurlStudy() { RenderAngles(false,includeSurfaceStudy:true,includeCurls:true); }
        public static void RenderIdleRearLayerStudy() { RenderAngles(false,false,false,false,false,false,true); }
        public static void RenderIdleAndBuild() { RenderIdle(); Build(); }
        public static void RenderIdleHandsAndBuild() { RenderAngles(false,false,false,true); Build(); }
        public static void RenderIdleFace() { RenderAngles(false,true); }
        public static void RenderIdleFaceAndBuild() { RenderIdleFace(); Build(); }
        public static void RenderIdleSkinningAndBuild() { BuildWithSkinningReview(false); }
        public static void RenderIdleHandSkinningAndBuild() { BuildWithSkinningReview(true); }
        // Targeted regression for upper garment geometry and its attachments.
        public static void RenderIdleGarmentCheckAndBuild() {
            FishBuild.UseRebuilt=true;FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion=new FishMotion(companion.model){randomActions=false};
            var skins=companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots=new List<MeshFilter>();
            foreach(var skin in skins) {
                var obj=new GameObject("Garment regression snapshot");obj.transform.SetParent(skin.transform,false);
                var filter=obj.AddComponent<MeshFilter>();filter.sharedMesh=new Mesh();snapshots.Add(filter);
                obj.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            }
            var camera=companion.view;var target=new RenderTexture(360,480,24){antiAliasing=4};camera.targetTexture=target;
            var frame=new Texture2D(360,480,TextureFormat.RGBA32,false);
            var sheet=new Texture2D(1440,960,TextureFormat.RGBA32,false);
            var gestures=new[]{FishMotion.Gesture.None,FishMotion.Gesture.Bow,FishMotion.Gesture.Wave,FishMotion.Gesture.Cheer};
            Vector2 min=Vector2.one,max=Vector2.zero;
            for(int pose=0;pose<8;pose++) {
                motion.Play(gestures[pose/2],0);
                motion.Tick(pose%2==0?.25f:.7f,1,0,0,pose<2?"working":"idle",false);
                for(int i=0;i<skins.Length;i++) {
                    var mesh=snapshots[i].sharedMesh;skins[i].BakeMesh(mesh);
                    foreach(var point in mesh.vertices) {
                        Vector3 p=camera.WorldToViewportPoint(skins[i].transform.TransformPoint(point));
                        if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y))
                            throw new InvalidOperationException("Non-finite garment regression pose "+pose);
                        min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                    }
                }
                camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,360,480),0,0);frame.Apply();
                sheet.SetPixels((pose%4)*360,(1-pose/4)*480,360,480,frame.GetPixels());
            }
            if(min.x<.02f||min.y<.02f||max.x>.98f||max.y>.98f)
                throw new InvalidOperationException("Garment regression outside viewport: "+min+" / "+max);
            sheet.Apply();File.WriteAllBytes("work/rebuilt-garment-regression.png",sheet.EncodeToPNG());
            File.WriteAllText("work/garment-motion-validation.json","{\n  \"sampled_poses\": 8,\n  \"scope\": \"working, bow, wave and cheer upper garment samples\",\n  \"finite_and_viewport_passed\": true,\n  \"intersection_test\": false\n}\n");
            Debug.Log("Targeted garment validation passed: 8 poses; viewport "+min+" / "+max);
            camera.targetTexture=null;RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(sheet);
            foreach(var snapshot in snapshots){UnityEngine.Object.DestroyImmediate(snapshot.sharedMesh);UnityEngine.Object.DestroyImmediate(snapshot.gameObject);}
            RenderIdle();Build();
        }
        // Localized head-look, secondary-sway and working hair checks.
        public static void RenderIdleHairCheckAndBuild() { RenderHairCheckAndBuild(false); }
        public static void RenderIdleHeadSkinAndBuild() { RenderHairCheckAndBuild(true); }
        static void RenderHairCheckAndBuild(bool includeFace) {
            FishBuild.UseRebuilt=true;FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion=new FishMotion(companion.model){randomActions=false};
            var skins=companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots=new List<MeshFilter>();
            foreach(var skin in skins) {
                var obj=new GameObject("Hair regression snapshot");obj.transform.SetParent(skin.transform,false);
                var filter=obj.AddComponent<MeshFilter>();filter.sharedMesh=new Mesh();snapshots.Add(filter);
                obj.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            }
            var camera=companion.view;
            var target=new RenderTexture(360,480,24){antiAliasing=4};camera.targetTexture=target;
            var frame=new Texture2D(360,480,TextureFormat.RGBA32,false);
            var sheet=new Texture2D(1440,960,TextureFormat.RGBA32,false);
            Vector2 min=Vector2.one,max=Vector2.zero;
            for(int pose=0;pose<8;pose++) {
                motion.Play(FishMotion.Gesture.None,0);
                motion.Tick(pose*.73f,1,pose%2==0?-1:1,pose%3==0?-1:1,pose>=6?"working":"idle",false);
                for(int i=0;i<skins.Length;i++) {
                    var mesh=snapshots[i].sharedMesh;skins[i].BakeMesh(mesh);
                    foreach(var point in mesh.vertices) {
                        Vector3 p=camera.WorldToViewportPoint(skins[i].transform.TransformPoint(point));
                        if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y))
                            throw new InvalidOperationException("Non-finite hair regression pose "+pose);
                        min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                    }
                }
                camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,360,480),0,0);frame.Apply();
                sheet.SetPixels((pose%4)*360,(1-pose/4)*480,360,480,frame.GetPixels());
            }
            if(min.x<.02f||min.y<.02f||max.x>.98f||max.y>.98f)
                throw new InvalidOperationException("Hair regression outside viewport: "+min+" / "+max);
            sheet.Apply();File.WriteAllBytes("work/rebuilt-hair-regression.png",sheet.EncodeToPNG());
            File.WriteAllText("work/hair-motion-validation.json","{\n  \"sampled_poses\": 8,\n  \"scope\": \"head look, secondary hair sway, working posture\",\n  \"finite_and_viewport_passed\": true,\n  \"intersection_test\": false\n}\n");
            Debug.Log("Targeted hair validation passed: 8 poses; viewport "+min+" / "+max);
            camera.targetTexture=null;RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(sheet);
            foreach(var snapshot in snapshots){UnityEngine.Object.DestroyImmediate(snapshot.sharedMesh);UnityEngine.Object.DestroyImmediate(snapshot.gameObject);}
            if(includeFace)RenderIdleFace();else RenderIdle();Build();
        }
        public static void RenderIdleFootCheckAndBuild() {
            FishBuild.UseRebuilt=true;FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion=new FishMotion(companion.model){randomActions=false};
            var skins=companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots=new List<MeshFilter>();
            foreach(var skin in skins) {
                var obj=new GameObject("Foot regression snapshot");obj.transform.SetParent(skin.transform,false);
                var filter=obj.AddComponent<MeshFilter>();filter.sharedMesh=new Mesh();snapshots.Add(filter);
                obj.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            }
            var camera=companion.view;
            var target=new RenderTexture(360,480,24){antiAliasing=4};camera.targetTexture=target;
            var frame=new Texture2D(360,480,TextureFormat.RGBA32,false);
            var sheet=new Texture2D(1440,960,TextureFormat.RGBA32,false);
            Vector2 min=Vector2.one,max=Vector2.zero;
            var footSheet=new Texture2D(1440,960,TextureFormat.RGBA32,false);
            for(int pose=0;pose<8;pose++) {
                motion.Play(pose<6?FishMotion.Gesture.Step:FishMotion.Gesture.Bow,0);
                motion.Tick(pose<6?.60f+pose*.30f:.75f+(pose-6)*.5f,1,0,0,"idle",false);
                for(int i=0;i<skins.Length;i++) {
                    var mesh=snapshots[i].sharedMesh;skins[i].BakeMesh(mesh);
                    foreach(var point in mesh.vertices) {
                        Vector3 p=camera.WorldToViewportPoint(skins[i].transform.TransformPoint(point));
                        if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y))
                            throw new InvalidOperationException("Non-finite foot regression pose "+pose);
                        min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                    }
                }
                camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,360,480),0,0);frame.Apply();
                sheet.SetPixels((pose%4)*360,(1-pose/4)*480,360,480,frame.GetPixels());
                var savedPosition=camera.transform.position;var savedRotation=camera.transform.rotation;float savedSize=camera.orthographicSize;
                Vector3 focus=V(0,.135f,.025f);camera.orthographicSize=.18f;camera.transform.position=focus+Quaternion.Euler(0,35,0)*V(0,1f,3);
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,360,480),0,0);frame.Apply();
                footSheet.SetPixels((pose%4)*360,(1-pose/4)*480,360,480,frame.GetPixels());
                camera.transform.SetPositionAndRotation(savedPosition,savedRotation);camera.orthographicSize=savedSize;

            }
            if(min.x<.02f||min.y<.02f||max.x>.98f||max.y>.98f)
                throw new InvalidOperationException("Foot regression outside viewport: "+min+" / "+max);
            footSheet.Apply();File.WriteAllBytes("work/rebuilt-foot-regression-close.png",footSheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(footSheet);sheet.Apply();File.WriteAllBytes("work/rebuilt-foot-regression.png",sheet.EncodeToPNG());
            File.WriteAllText("work/foot-motion-validation.json","{\n  \"sampled_poses\": 8,\n  \"scope\": \"six stepping phases and two bow phases\",\n  \"finite_and_viewport_passed\": true,\n  \"intersection_test\": false\n}\n");
            Debug.Log("Targeted foot validation passed: 8 poses; viewport "+min+" / "+max);
            camera.targetTexture=null;RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(sheet);
            foreach(var snapshot in snapshots){UnityEngine.Object.DestroyImmediate(snapshot.sharedMesh);UnityEngine.Object.DestroyImmediate(snapshot.gameObject);}
            RenderIdle();Build();
        }
        public static void RenderIdleLowPolyValidationAndBuild() {
            FishBuild.UseRebuilt=true;FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion=new FishMotion(companion.model){randomActions=false};
            FishBuild.ValidateMotion(companion,motion,companion.model.GetComponentsInChildren<SkinnedMeshRenderer>());
            RenderAngles(false,true,false,true);Build();
        }
        static void BuildWithSkinningReview(bool includeHands) {
            FishBuild.UseRebuilt=true;FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion=new FishMotion(companion.model){randomActions=false};
            FishBuild.ValidateMotion(companion,motion,companion.model.GetComponentsInChildren<SkinnedMeshRenderer>());
            RenderAngles(false,false,false,includeHands);Build();
        }
        public static void RenderAngles() { RenderAngles(true); }
        static void RenderAngles(bool includeMotion,bool includeExpressions=false,bool includeSurfaceStudy=false,bool includeHandReview=false,bool includeLaceReview=false,bool includeFrontReview=false,bool includeRearLayers=false,bool includeFins=false,bool includeCurls=false) {
            FishBuild.UseRebuilt=true; FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion=new FishMotion(companion.model){randomActions=false};
            motion.Tick(0,1,0,0,"idle",false);
            var skins=companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots=new List<MeshFilter>();
            foreach(var skin in skins) {
                var mesh=new Mesh();skin.BakeMesh(mesh);
                var snapshot=new GameObject("Rebuilt review snapshot");
                snapshot.transform.SetParent(skin.transform,false);
                var filter=snapshot.AddComponent<MeshFilter>();
                filter.sharedMesh=mesh;snapshots.Add(filter);
                snapshot.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                skin.enabled=false;
            }
            var camera=companion.view;
            Vector3 front=camera.transform.position;
            Vector3 center=front+camera.transform.forward*(3f/Mathf.Cos(Mathf.Atan(.24f/3f)));
            var target=new RenderTexture(720,960,24){antiAliasing=4};
            var frame=new Texture2D(720,960,TextureFormat.RGBA32,false);
            camera.targetTexture=target;
            foreach(int angle in new[]{0,35,90,180}) {
                camera.transform.position=center+Quaternion.Euler(0,angle,0)*(front-center);
                camera.transform.LookAt(center);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                File.WriteAllBytes("work/rebuilt-angle-"+angle+".png",frame.EncodeToPNG());
            }
            camera.transform.position=center+new Vector3(0,.64f,3);
            camera.transform.LookAt(center);camera.Render();RenderTexture.active=target;
            frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
            File.WriteAllBytes("work/rebuilt-angle-top.png",frame.EncodeToPNG());
            File.Copy("work/rebuilt-angle-0.png","work/rebuilt-idle.png",true);
            RenderHeadStudy(camera,"work/rebuilt-head",1,Vector3.zero);
            RenderBibStudy(camera,"work/rebuilt-bib",1,Vector3.zero);
            RenderApronStudy(camera,"work/rebuilt-apron",1,Vector3.zero);
            RenderFootStudy(camera,"work/rebuilt-foot",1,Vector3.zero);
            RenderBodyStudy(camera,"work/rebuilt-aligned",1,Vector3.zero);
            RenderBodyStudy(camera,"work/rebuilt-small",1,Vector3.zero,180,240);
            var defaultHand=companion.model.GetComponentsInChildren<Transform>().First(t=>t.name=="J_Bip_R_Hand");
            RenderDefaultHandStudy(camera,"work/rebuilt-hand-default",defaultHand.position+V(0,-.014f,.015f),1);
            // Inspect the skin separately from fringe occlusion. Head skin is
            // the first contiguous bone group in the authored body mesh.
            var bodySnapshot=snapshots[0];var completeBody=bodySnapshot.sharedMesh;
            var skinWeights=skins[0].sharedMesh.boneWeights;
            int headCount=skinWeights.TakeWhile(w=>w.boneIndex0==skinWeights[0].boneIndex0).Count();
            var headOnly=UnityEngine.Object.Instantiate(completeBody);
            var bodyTriangles=completeBody.triangles;var headTriangles=new List<int>();
            for(int i=0;i<bodyTriangles.Length;i+=3)
                if(bodyTriangles[i]<headCount&&bodyTriangles[i+1]<headCount&&bodyTriangles[i+2]<headCount)
                    headTriangles.AddRange(new[]{bodyTriangles[i],bodyTriangles[i+1],bodyTriangles[i+2]});
            headOnly.triangles=headTriangles.ToArray();bodySnapshot.sharedMesh=headOnly;
            foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
            RenderHeadStudy(camera,"work/rebuilt-head-skin",1,Vector3.zero);
            foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
            bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(headOnly);
            var sleevesOnly=UnityEngine.Object.Instantiate(completeBody);var sleeveFacets=new List<int>();
            var armBones=skins[0].bones.Select((b,i)=>new{b,i}).Where(v=>v.b.name.Contains("UpperArm")||v.b.name.Contains("LowerArm")||v.b.name.Contains("Shoulder")).Select(v=>v.i).ToArray();
            var bodyColors=skins[0].sharedMesh.colors;
            for(int i=0;i<bodyTriangles.Length;i+=3)if(Enumerable.Range(0,3).All(k=>{
                int v=bodyTriangles[i+k];var w=skinWeights[v];var color=bodyColors[v];
                return (armBones.Contains(w.boneIndex0)||armBones.Contains(w.boneIndex1))&&color.r<.5f&&color.b<.65f;
            }))sleeveFacets.AddRange(new[]{bodyTriangles[i],bodyTriangles[i+1],bodyTriangles[i+2]});
            sleevesOnly.triangles=sleeveFacets.ToArray();bodySnapshot.sharedMesh=sleevesOnly;
            foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
            RenderBodyStudy(camera,"work/rebuilt-sleeves",1,Vector3.zero);
            foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
            bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(sleevesOnly);
            var bandOnly=UnityEngine.Object.Instantiate(completeBody);var bandFacets=new List<int>();var bandUV=completeBody.uv;
            for(int i=0;i<bodyTriangles.Length;i+=3)if(Enumerable.Range(0,3).All(k=>{
                var q=bandUV[bodyTriangles[i+k]];return q.x>=0&&q.x<.25f&&q.y>=0&&q.y<128f/PaintHeight;
            }))bandFacets.AddRange(new[]{bodyTriangles[i],bodyTriangles[i+1],bodyTriangles[i+2]});
            bandOnly.triangles=bandFacets.ToArray();bodySnapshot.sharedMesh=bandOnly;
            foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
            RenderHeadStudy(camera,"work/rebuilt-headband",1,Vector3.zero);
            foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
            bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(bandOnly);
            var feetOnly=UnityEngine.Object.Instantiate(completeBody);var footTriangles=new List<int>();
            var footBones=skins[0].bones.Select((b,i)=>new{b,i}).Where(v=>v.b.name.Contains("Foot")||v.b.name.Contains("LowerLeg")).Select(v=>v.i).ToArray();
            for(int i=0;i<bodyTriangles.Length;i+=3)if(Enumerable.Range(0,3).All(k=>footBones.Contains(skinWeights[bodyTriangles[i+k]].boneIndex0)))
                footTriangles.AddRange(new[]{bodyTriangles[i],bodyTriangles[i+1],bodyTriangles[i+2]});
            feetOnly.triangles=footTriangles.ToArray();bodySnapshot.sharedMesh=feetOnly;
            RenderFootStudy(camera,"work/rebuilt-foot-isolated",1,Vector3.zero);
            bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(feetOnly);
            if(includeFrontReview) {
                var only=UnityEngine.Object.Instantiate(completeBody);var chosen=new List<int>();var st=completeBody.uv;
                for(int k=0;k<bodyTriangles.Length;k+=3)if(Enumerable.Range(0,3).All(j=>{
                    Vector2 q=st[bodyTriangles[k+j]];return q.x<.75f&&q.y>=128f/PaintHeight&&q.y<LegacyPaintHeight/(float)PaintHeight&&!IsFrontSkinUV(q);
                }))chosen.AddRange(new[]{bodyTriangles[k],bodyTriangles[k+1],bodyTriangles[k+2]});
                only.triangles=chosen.ToArray();bodySnapshot.sharedMesh=only;var renderer=bodySnapshot.GetComponent<Renderer>();var original=renderer.sharedMaterials;
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
                for(int mode=0;mode<2;mode++) {
                    only.colors=Enumerable.Repeat(mode==0?new Color(.65f,.65f,.65f,1):Color.white,only.vertexCount).ToArray();
                    var mat=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));mat.mainTexture=mode==0?Texture2D.whiteTexture:original[0].mainTexture;
                    mat.SetFloat("_Flat",mode);mat.SetFloat("_OutlineWidth",0);renderer.sharedMaterial=mat;
                    RenderHeadStudy(camera,"work/rebuilt-front-"+(mode==0?"clay":"paint-flat"),1,Vector3.zero);UnityEngine.Object.DestroyImmediate(mat);
                }
                renderer.sharedMaterials=original;bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(only);
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
            }
            if(includeRearLayers) {
                var bind=skins[0].sharedMesh.vertices;var uv=completeBody.uv;int[] parents=Enumerable.Range(0,bind.Length).ToArray();
                Func<int,int> root=null;root=i=>parents[i]==i?i:parents[i]=root(parents[i]);
                var rearFaces=new List<int>();
                for(int k=0;k<bodyTriangles.Length;k+=3) {
                    bool rear=true;
                    for(int c=0;c<3;c++) {
                        int i=bodyTriangles[k+c];Vector3 p=bind[i];float t=Mathf.Clamp01((1.03f-p.y)/.96f);
                        if(Mathf.Abs(uv[i].x-Mathf.Clamp01(.5f+p.x/.82f))>.00001f||Mathf.Abs(uv[i].y-(LegacyPaintHeight+t*(RearPaintHeight-1))/PaintHeight)>.00001f)rear=false;
                    }
                    if(!rear)continue;rearFaces.Add(k);parents[root(bodyTriangles[k+1])]=root(bodyTriangles[k]);parents[root(bodyTriangles[k+2])]=root(bodyTriangles[k]);
                }
                foreach(var group in rearFaces.GroupBy(k=>root(bodyTriangles[k]))) {
                    var indices=group.SelectMany(k=>new[]{bodyTriangles[k],bodyTriangles[k+1],bodyTriangles[k+2]}).ToArray();
                    float top=indices.Max(i=>bind[i].y),bottom=indices.Min(i=>bind[i].y);
                    bool curtain=top>.740f&&top<.751f&&bottom<.21f&&indices.Max(i=>bind[i].x)-indices.Min(i=>bind[i].x)>.30f;
                    if((top<.80f||top>.84f||bottom>.20f)&&!curtain)continue;
                    int component=indices.Average(i=>bind[i].x)<0?0:5;
                    var only=UnityEngine.Object.Instantiate(completeBody);only.triangles=indices;
                    only.colors=Enumerable.Repeat(new Color(.65f,.65f,.65f,1),only.vertexCount).ToArray();bodySnapshot.sharedMesh=only;
                    foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
                    var renderer=bodySnapshot.GetComponent<Renderer>();var original=renderer.sharedMaterials;
                    var material=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));material.mainTexture=Texture2D.whiteTexture;material.SetFloat("_OutlineWidth",0);renderer.sharedMaterial=material;
                    RenderBodyStudy(camera,curtain?"work/rebuilt-outer-clay-total":"work/rebuilt-inner-clay-"+component,1,Vector3.zero);
                    renderer.sharedMaterials=original;bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(only);
                }
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
            }
            if(includeLaceReview) {
                var laceOnly=UnityEngine.Object.Instantiate(completeBody);var laceTriangles=new List<int>();var st=completeBody.uv;
                for(int k=0;k<bodyTriangles.Length;k+=3)if(Enumerable.Range(0,3).All(j=>{
                    Vector2 q=st[bodyTriangles[k+j]];return q.x>=928f/1024&&q.y>=1856f/PaintHeight&&q.y<=2016f/PaintHeight;
                }))laceTriangles.AddRange(new[]{bodyTriangles[k],bodyTriangles[k+1],bodyTriangles[k+2]});
                laceOnly.triangles=laceTriangles.ToArray();bodySnapshot.sharedMesh=laceOnly;
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
                RenderBibStudy(camera,"work/rebuilt-lace-isolated",1,Vector3.zero);
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
                bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(laceOnly);
            }
            if(includeSurfaceStudy) {
                RenderSurfaceStudy(snapshots,camera,center,front,target,frame);
                var chestOnly=UnityEngine.Object.Instantiate(completeBody);var chestTriangles=new List<int>();var st=completeBody.uv;
                for(int k=0;k<bodyTriangles.Length;k+=3)if(Enumerable.Range(0,3).All(j=>{
                    Vector2 q=st[bodyTriangles[k+j]];return q.x>.751f&&q.x<.996f&&q.y>128f/PaintHeight&&q.y<.55f;
                }))chestTriangles.AddRange(new[]{bodyTriangles[k],bodyTriangles[k+1],bodyTriangles[k+2]});
                chestOnly.triangles=chestTriangles.ToArray();bodySnapshot.sharedMesh=chestOnly;
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
                RenderBodyStudy(camera,"work/rebuilt-chest-isolated",1,Vector3.zero);
                RenderSurfaceStudy(snapshots,camera,center,front,target,frame);
                foreach(string mode in new[]{"clay-lit","paint-flat","albedo-flat","toon-no-outline"})foreach(int angle in new[]{0,35,90,180})
                    File.Copy("work/rebuilt-surface-"+mode+"-"+angle+".png","work/rebuilt-chest-"+mode+"-"+angle+".png",true);
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
                bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(chestOnly);
                RenderSurfaceStudy(snapshots,camera,center,front,target,frame);
            }
            if(includeCurls) {
                var bind=skins[0].sharedMesh.vertices;var st=completeBody.uv;int[] parents=Enumerable.Range(0,bind.Length).ToArray();
                Func<int,int> root=null;root=i=>parents[i]==i?i:parents[i]=root(parents[i]);var faces=new List<int>();
                for(int k=0;k<bodyTriangles.Length;k+=3) {
                    if(!Enumerable.Range(0,3).All(c=>{int i=bodyTriangles[k+c];Vector3 p=bind[i];float t=Mathf.Clamp01((1.03f-p.y)/.96f);return Mathf.Abs(st[i].x-Mathf.Clamp01(.5f+p.x/.82f))<.00001f&&Mathf.Abs(st[i].y-(LegacyPaintHeight+t*(RearPaintHeight-1))/PaintHeight)<.00001f;}))continue;
                    faces.Add(k);parents[root(bodyTriangles[k+1])]=root(bodyTriangles[k]);parents[root(bodyTriangles[k+2])]=root(bodyTriangles[k]);
                }
                var renderer=bodySnapshot.GetComponent<Renderer>();var original=renderer.sharedMaterials;
                foreach(var group in faces.GroupBy(k=>root(bodyTriangles[k]))) {
                    var indices=group.SelectMany(k=>new[]{bodyTriangles[k],bodyTriangles[k+1],bodyTriangles[k+2]}).ToArray();
                    float top=indices.Max(i=>bind[i].y),bottom=indices.Min(i=>bind[i].y);
                    if(top<.94f||bottom<.20f||bottom>.235f)continue;
                    int component=indices.Average(i=>bind[i].x)<0?8:11;
                    var isolated=UnityEngine.Object.Instantiate(completeBody);isolated.triangles=indices;bodySnapshot.sharedMesh=isolated;
                    foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
                    RenderBodyStudy(camera,"work/rebuilt-outer-hook-"+component,1,Vector3.zero);
                    isolated.colors=Enumerable.Repeat(new Color(.65f,.65f,.65f,1),isolated.vertexCount).ToArray();
                    var material=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));material.mainTexture=DiagnosticAlpha(original[0].mainTexture);material.SetFloat("_OutlineWidth",0);renderer.sharedMaterial=material;
                    RenderBodyStudy(camera,"work/rebuilt-outer-hook-clay-"+component,1,Vector3.zero,reviewMaterials:new[]{material});
                    renderer.sharedMaterials=original;bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(material.mainTexture);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(isolated);
                }
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
            }
            if(includeFins) {
                var isolated=UnityEngine.Object.Instantiate(completeBody);var selected=new List<int>();var st=completeBody.uv;
                for(int k=0;k<bodyTriangles.Length;k+=3)if(Enumerable.Range(0,3).All(j=>{
                    int v=bodyTriangles[k+j];return st[v].x>=.75f&&st[v].y<.058f&&Mathf.Abs(completeBody.vertices[v].x)>.13f&&completeBody.vertices[v].y>.60f;
                }))selected.AddRange(new[]{bodyTriangles[k],bodyTriangles[k+1],bodyTriangles[k+2]});
                isolated.triangles=selected.ToArray();bodySnapshot.sharedMesh=isolated;
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=false;
                RenderHeadStudy(camera,"work/rebuilt-fin-isolated",1,Vector3.zero);
                foreach(var snapshot in snapshots.Skip(1))snapshot.GetComponent<Renderer>().enabled=true;
                bodySnapshot.sharedMesh=completeBody;UnityEngine.Object.DestroyImmediate(isolated);
            }
            if(!includeMotion) {
                if(includeHandReview) {
                    RenderHandDetails(companion,motion,skins,snapshots,camera,"R");
                    RenderHandDetails(companion,motion,skins,snapshots,camera,"L");
                }
                if(includeExpressions)RenderExpressionDetails(companion,motion,skins,snapshots,camera);
                camera.targetTexture=null;RenderTexture.active=null;
                UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
                foreach(var snapshot in snapshots) {
                    UnityEngine.Object.DestroyImmediate(snapshot.sharedMesh);
                    UnityEngine.Object.DestroyImmediate(snapshot.gameObject);
                }
                foreach(var skin in skins)skin.enabled=true;
                Debug.Log("Default-pose review rendered: front, oblique, side, rear and elevated front.");
                return;
            }
            // Inspect the wrist at both wave extremes and the eyelid surface from
            // an oblique view. The usual front contact sheet hides depth defects.
            var details=new Texture2D(1440,1920,TextureFormat.RGBA32,false);
            camera.transform.position=center+Quaternion.Euler(0,35,0)*(front-center);
            camera.transform.LookAt(center);
            for(int pose=0;pose<4;pose++) {
                motion.Play(pose==1||pose==2?FishMotion.Gesture.Wave:FishMotion.Gesture.None,0);
                motion.Evaluate(pose==1?1.3625f:pose==2?1.7875f:1,0,0,"idle",false);
                FishBuild.ApplyMotionExpressions(companion,motion);
                if(pose==3)companion.face.SetBlendShapeWeight(companion.face.sharedMesh.GetBlendShapeIndex("Fcl_EYE_Close"),100);
                for(int i=0;i<skins.Length;i++) {
                    UnityEngine.Object.DestroyImmediate(snapshots[i].sharedMesh);
                    var mesh=new Mesh();skins[i].BakeMesh(mesh);snapshots[i].sharedMesh=mesh;
                }
                camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                details.SetPixels((pose%2)*720,(1-pose/2)*960,720,960,frame.GetPixels());
            }
            details.Apply();
            File.WriteAllBytes("work/rebuilt-joints-and-face.png",details.EncodeToPNG());
            // Tick the working state to include its blend, then inspect bent
            // sleeves and curled fingers from oblique and side views.
            var working=new Texture2D(1440,960,TextureFormat.RGBA32,false);
            motion.Play(FishMotion.Gesture.None,0);
            for(int pose=0;pose<2;pose++) {
                motion.Tick(pose==0?2.2f:3f,1,0,0,"working",false);
                for(int shape=0;shape<companion.face.sharedMesh.blendShapeCount;shape++)
                    companion.face.SetBlendShapeWeight(shape,0);
                for(int i=0;i<skins.Length;i++) {
                    UnityEngine.Object.DestroyImmediate(snapshots[i].sharedMesh);
                    var mesh=new Mesh();skins[i].BakeMesh(mesh);snapshots[i].sharedMesh=mesh;
                }
                camera.transform.position=center+Quaternion.Euler(0,pose==0?35:90,0)*(front-center);
                camera.transform.LookAt(center);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                working.SetPixels(pose*720,0,720,960,frame.GetPixels());
            }
            working.Apply();
            File.WriteAllBytes("work/rebuilt-working.png",working.EncodeToPNG());
            RenderHandDetails(companion,motion,skins,snapshots,camera,"R");
            RenderHandDetails(companion,motion,skins,snapshots,camera,"L");
            RenderExpressionDetails(companion,motion,skins,snapshots,camera);
            camera.targetTexture=null;RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
            UnityEngine.Object.DestroyImmediate(details);
            UnityEngine.Object.DestroyImmediate(working);
            foreach(var snapshot in snapshots) {
                UnityEngine.Object.DestroyImmediate(snapshot.sharedMesh);
                UnityEngine.Object.DestroyImmediate(snapshot.gameObject);
            }
            foreach(var skin in skins)skin.enabled=true;
        }

        // Temporary diagnostic texture: preserve cutout evidence while
        // removing RGB. Never save or reuse reference pixels in model paint.
        public static Texture2D DiagnosticAlpha(Texture source) {
            if(!source)return null;
            var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Graphics.Blit(source,rt);RenderTexture.active=rt;
            var result=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
            result.ReadPixels(new Rect(0,0,source.width,source.height),0,0);var pixels=result.GetPixels32();
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(255,255,255,pixels[i].a);
            result.SetPixels32(pixels);result.Apply();result.wrapMode=source.wrapMode;result.filterMode=source.filterMode;
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);return result;
        }

        // Separate lighting, vertex modulation and paint when diagnosing a
        // visible groove. Only temporary baked snapshots are modified.
        static void RenderSurfaceStudy(List<MeshFilter> snapshots,Camera camera,Vector3 center,
            Vector3 front,RenderTexture target,Texture2D frame) {
            var originalMeshes=snapshots.Select(s=>s.sharedMesh).ToArray();
            var originalMaterials=snapshots.Select(s=>s.GetComponent<Renderer>().sharedMaterial).ToArray();
            for(int mode=0;mode<4;mode++) {
                string name=mode==0?"clay-lit":mode==1?"paint-flat":mode==2?"albedo-flat":"toon-no-outline";
                for(int i=0;i<snapshots.Count;i++) {
                    var mesh=UnityEngine.Object.Instantiate(originalMeshes[i]);
                    var material=UnityEngine.Object.Instantiate(originalMaterials[i]);
                    if(mode!=1&&mode!=3)mesh.colors=Enumerable.Repeat(mode==0?new Color(.65f,.65f,.65f,1):Color.white,mesh.vertexCount).ToArray();
                    if(mode==0)material.mainTexture=DiagnosticAlpha(originalMaterials[i].mainTexture);
                    material.SetFloat("_Flat",mode==0||mode==3?0:1);material.SetFloat("_OutlineWidth",0);
                    snapshots[i].sharedMesh=mesh;snapshots[i].GetComponent<Renderer>().sharedMaterial=material;
                }
                RenderBodyStudy(camera,"work/rebuilt-surface-"+name,1,Vector3.zero,reviewMaterials:mode==0?snapshots.Select(m=>m.GetComponent<Renderer>().sharedMaterial).ToArray():null);
                for(int i=0;i<snapshots.Count;i++) {
                    UnityEngine.Object.DestroyImmediate(snapshots[i].sharedMesh);
                    if(mode==0&&snapshots[i].GetComponent<Renderer>().sharedMaterial.mainTexture)
                        UnityEngine.Object.DestroyImmediate(snapshots[i].GetComponent<Renderer>().sharedMaterial.mainTexture);
                    UnityEngine.Object.DestroyImmediate(snapshots[i].GetComponent<Renderer>().sharedMaterial);
                    snapshots[i].sharedMesh=originalMeshes[i];snapshots[i].GetComponent<Renderer>().sharedMaterial=originalMaterials[i];
                }
            }
        }

        public static void RenderDefaultHandStudy(Camera camera,string prefix,Vector3 focus,float scale) {
            var position=camera.transform.position;var rotation=camera.transform.rotation;
            float size=camera.orthographicSize,aspect=camera.aspect;var oldTarget=camera.targetTexture;
            var target=new RenderTexture(512,512,24){antiAliasing=4};
            var frame=new Texture2D(512,512,TextureFormat.RGBA32,false);
            camera.targetTexture=target;camera.aspect=1;camera.orthographicSize=.064f*scale;
            foreach(int angle in new[]{0,35,90}) {
                camera.transform.position=focus+Quaternion.Euler(0,angle,0)*V(0,.045f,.40f)*scale;
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,512,512),0,0);frame.Apply();
                File.WriteAllBytes(prefix+"-"+angle+".png",frame.EncodeToPNG());
            }
            camera.targetTexture=oldTarget;RenderTexture.active=null;camera.aspect=aspect;camera.orthographicSize=size;
            camera.transform.SetPositionAndRotation(position,rotation);
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }

        static void RenderHandDetails(FishCompanion companion,FishMotion motion,
            SkinnedMeshRenderer[] skins,List<MeshFilter> snapshots,Camera camera,string side) {
            float sign=side=="R"?1:-1;
            var hand=companion.model.GetComponentsInChildren<Transform>().First(t=>t.name=="J_Bip_"+side+"_Hand");
            Vector3 oldPosition=camera.transform.position;
            Quaternion oldRotation=camera.transform.rotation;
            float oldSize=camera.orthographicSize;
            var oldTarget=camera.targetTexture;
            var target=new RenderTexture(512,512,24){antiAliasing=4};
            var frame=new Texture2D(512,512,TextureFormat.RGBA32,false);
            var sheet=new Texture2D(1024,1024,TextureFormat.RGBA32,false);
            camera.targetTexture=target;camera.orthographicSize=.063f;
            for(int pose=0;pose<4;pose++) {
                motion.Play(pose==1||pose==2?FishMotion.Gesture.Wave:FishMotion.Gesture.None,0);
                motion.Tick(pose==1?1.3625f:pose==2?1.7875f:pose==3?3:1,1,0,0,
                    pose==3?"working":"idle",false);
                for(int shape=0;shape<companion.face.sharedMesh.blendShapeCount;shape++)
                    companion.face.SetBlendShapeWeight(shape,0);
                for(int i=0;i<skins.Length;i++) {
                    UnityEngine.Object.DestroyImmediate(snapshots[i].sharedMesh);
                    var mesh=new Mesh();skins[i].BakeMesh(mesh);snapshots[i].sharedMesh=mesh;
                }
                Vector3 focus=hand.TransformPoint(V(sign*.031f,0,.004f));
                camera.transform.position=focus+Quaternion.Euler(0,sign*35,0)*V(0,.018f,.40f);
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,512,512),0,0);frame.Apply();
                sheet.SetPixels((pose%2)*512,(1-pose/2)*512,512,512,frame.GetPixels());
            }
            sheet.Apply();File.WriteAllBytes("work/rebuilt-hands"+(side=="R"?"":"-left")+".png",sheet.EncodeToPNG());
            camera.targetTexture=oldTarget;RenderTexture.active=null;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.orthographicSize=oldSize;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        static void RenderExpressionDetails(FishCompanion companion,FishMotion motion,
            SkinnedMeshRenderer[] skins,List<MeshFilter> snapshots,Camera camera) {
            Vector3 oldPosition=camera.transform.position;
            Quaternion oldRotation=camera.transform.rotation;
            float oldSize=camera.orthographicSize,oldAspect=camera.aspect;
            var oldTarget=camera.targetTexture;
            const int size=384,columns=8;
            var target=new RenderTexture(size,size,24){antiAliasing=4};
            var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);
            camera.targetTexture=target;camera.orthographicSize=.27f;camera.aspect=1;
            motion.Play(FishMotion.Gesture.None,0);
            motion.Tick(0,10,0,0,"idle",false);
            var head=companion.model.GetComponentsInChildren<Transform>().First(t=>t.name=="J_Bip_C_Head");
            Vector3 focus=head.position+companion.model.TransformVector(V(0,-.015f,.06f));
            foreach(bool extreme in new[]{false,true}) {
                string[] names=extreme?
                    new[]{"","Fcl_ALL_Joy","Fcl_ALL_Fun","Fcl_ALL_Sorrow","Fcl_ALL_Surprised","Fcl_ALL_Surprised","Fcl_ALL_Joy","Fcl_EYE_Joy"}:
                    new[]{"","Fcl_ALL_Joy","Fcl_ALL_Fun","Fcl_ALL_Sorrow","Fcl_ALL_Surprised","Fcl_ALL_Surprised","Fcl_EYE_Close","Fcl_EYE_Joy"};
                float[] amounts=extreme?new[]{0f,100,100,100,100,45,70,100}:new[]{0f,70,25,70,35,45,100,100};
                var sheet=new Texture2D(size*columns,size*2,TextureFormat.RGBA32,false);
                for(int angle=0;angle<2;angle++)for(int pose=0;pose<columns;pose++) {
                    for(int shape=0;shape<companion.face.sharedMesh.blendShapeCount;shape++) {
                        string name=companion.face.sharedMesh.GetBlendShapeName(shape);
                        float amount=name==names[pose]?amounts[pose]:0;
                        if(extreme&&(pose==5||pose==6)&&name=="Fcl_EYE_Close")amount=100;
                        if(pose==7&&name=="Fcl_ALL_Joy")amount=70;
                        companion.face.SetBlendShapeWeight(shape,amount);
                    }
                    for(int i=0;i<skins.Length;i++) {
                        UnityEngine.Object.DestroyImmediate(snapshots[i].sharedMesh);
                        var mesh=new Mesh();skins[i].BakeMesh(mesh);snapshots[i].sharedMesh=mesh;
                    }
                    camera.transform.position=focus+Quaternion.Euler(0,angle*35,0)*V(0,.20f,3);
                    camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                    frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply();
                    sheet.SetPixels(pose*size,(1-angle)*size,size,size,frame.GetPixels());
                }
                sheet.Apply();
                File.WriteAllBytes("work/rebuilt-expression"+(extreme?"-extremes":"s")+".png",sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            var fractions=new Texture2D(size*5,size*4,TextureFormat.RGBA32,false);
            for(int kind=0;kind<2;kind++)for(int angle=0;angle<2;angle++)for(int step=0;step<5;step++) {
                string active=kind==0?"Fcl_EYE_Close":"Fcl_EYE_Joy";
                for(int shape=0;shape<companion.face.sharedMesh.blendShapeCount;shape++)
                    companion.face.SetBlendShapeWeight(shape,companion.face.sharedMesh.GetBlendShapeName(shape)==active?step*25f:0);
                for(int i=0;i<skins.Length;i++) {
                    UnityEngine.Object.DestroyImmediate(snapshots[i].sharedMesh);
                    var mesh=new Mesh();skins[i].BakeMesh(mesh);snapshots[i].sharedMesh=mesh;
                }
                camera.transform.position=focus+Quaternion.Euler(0,angle*35,0)*V(0,.20f,3);
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply();
                fractions.SetPixels(step*size,(3-kind*2-angle)*size,size,size,frame.GetPixels());
            }
            fractions.Apply();File.WriteAllBytes("work/rebuilt-eye-fractions.png",fractions.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(fractions);
            camera.targetTexture=oldTarget;RenderTexture.active=null;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.orthographicSize=oldSize;camera.aspect=oldAspect;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }

        public static void RenderBodyStudy(Camera camera,string prefix,float scale,Vector3 offset,int width=720,int height=960,Material[] reviewMaterials=null) {
            // Use the same approximate crown/sole calibration as head studies.
            // Fixed framing prevents different auto-fit margins from hiding
            // overall proportion differences between reference and rebuild.
            var position=camera.transform.position;var rotation=camera.transform.rotation;
            var oldTarget=camera.targetTexture;float size=camera.orthographicSize,aspect=camera.aspect;
            var target=new RenderTexture(width,height,24){antiAliasing=4};
            var frame=new Texture2D(width,height,TextureFormat.RGBA32,false);
            Vector3 focus=V(0,.56f,-.05f)*scale+offset;
            camera.targetTexture=target;camera.aspect=.75f;camera.orthographicSize=.64f*scale;
            foreach(int angle in new[]{0,35,90,180,360}) {
                Vector3 direction=angle==360?V(0,.64f,3):Quaternion.Euler(0,angle,0)*V(0,.24f,3);
                camera.transform.position=focus+direction*scale;camera.transform.LookAt(focus);
                if(reviewMaterials!=null) {
                    Vector3 light=Quaternion.Euler(0,angle==360?0:angle,0)*V(-.35f,.65f,.80f);
                    foreach(var material in reviewMaterials)material.SetVector("_LightDirection",new Vector4(light.x,light.y,light.z,0));
                }
                camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,width,height),0,0);frame.Apply();
                File.WriteAllBytes(prefix+"-"+(angle==360?"top":angle.ToString())+".png",frame.EncodeToPNG());
            }
            camera.targetTexture=oldTarget;camera.orthographicSize=size;camera.aspect=aspect;
            camera.transform.SetPositionAndRotation(position,rotation);RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }

        public static void RenderHeadStudy(Camera camera,string prefix,float scale,Vector3 offset) {
            // Identical pitch and world-space framing expose modelling differences
            // independently of the full-body camera's gesture clearance.
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var oldTarget=camera.targetTexture;float oldSize=camera.orthographicSize,oldAspect=camera.aspect;
            const int size=720;
            var target=new RenderTexture(size,size,24){antiAliasing=4};
            var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);
            Vector3 focus=V(0,.77f,.06f)*scale+offset;
            camera.targetTexture=target;camera.aspect=1;camera.orthographicSize=.33f*scale;
            foreach(int angle in new[]{0,35,90}) {
                camera.transform.position=focus+Quaternion.Euler(0,angle,0)*V(0,.24f,3)*scale;
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply();
                File.WriteAllBytes(prefix+"-"+angle+".png",frame.EncodeToPNG());
            }
            camera.targetTexture=oldTarget;camera.orthographicSize=oldSize;camera.aspect=oldAspect;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }

        public static void RenderBibStudy(Camera camera,string prefix,float scale,Vector3 offset) {
            // Identical pitch and world-space framing expose modelling differences
            // independently of the full-body camera's gesture clearance.
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var oldTarget=camera.targetTexture;float oldSize=camera.orthographicSize,oldAspect=camera.aspect;
            const int size=720;
            var target=new RenderTexture(size,size,24){antiAliasing=4};
            var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);
            Vector3 focus=V(0,.465f,.09f)*scale+offset;
            camera.targetTexture=target;camera.aspect=1;camera.orthographicSize= .135f*scale;
            foreach(int angle in new[]{0,35,90}) {
                camera.transform.position=focus+Quaternion.Euler(0,angle,0)*V(0,.24f,3)*scale;
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply();
                File.WriteAllBytes(prefix+"-"+angle+".png",frame.EncodeToPNG());
            }
            camera.targetTexture=oldTarget;camera.orthographicSize=oldSize;camera.aspect=oldAspect;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }

        public static void RenderApronStudy(Camera camera,string prefix,float scale,Vector3 offset) {
            // Identical pitch and world-space framing expose modelling differences
            // independently of the full-body camera's gesture clearance.
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var oldTarget=camera.targetTexture;float oldSize=camera.orthographicSize,oldAspect=camera.aspect;
            const int size=720;
            var target=new RenderTexture(size,size,24){antiAliasing=4};
            var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);
            Vector3 focus=V(0,.315f,.22f)*scale+offset;
            camera.targetTexture=target;camera.aspect=1;camera.orthographicSize= .14f*scale;
            foreach(int angle in new[]{0,35,90}) {
                camera.transform.position=focus+Quaternion.Euler(0,angle,0)*V(0,.24f,3)*scale;
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply();
                File.WriteAllBytes(prefix+"-"+angle+".png",frame.EncodeToPNG());
            }
            camera.targetTexture=oldTarget;camera.orthographicSize=oldSize;camera.aspect=oldAspect;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }

        public static void RenderFootStudy(Camera camera,string prefix,float scale,Vector3 offset) {
            // Identical pitch and world-space framing expose modelling differences
            // independently of the full-body camera's gesture clearance.
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var oldTarget=camera.targetTexture;float oldSize=camera.orthographicSize,oldAspect=camera.aspect;
            const int size=720;
            var target=new RenderTexture(size,size,24){antiAliasing=4};
            var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);
            Vector3 focus=V(0,.135f,.025f)*scale+offset;
            camera.targetTexture=target;camera.aspect=1;camera.orthographicSize= .165f*scale;
            foreach(int angle in new[]{0,35,90,180}) {
                camera.transform.position=focus+Quaternion.Euler(0,angle,0)*V(0,1.0f,3)*scale;
                camera.transform.LookAt(focus);camera.Render();RenderTexture.active=target;
                frame.ReadPixels(new Rect(0,0,size,size),0,0);frame.Apply();
                File.WriteAllBytes(prefix+"-"+angle+".png",frame.EncodeToPNG());
            }
            camera.targetTexture=oldTarget;camera.orthographicSize=oldSize;camera.aspect=oldAspect;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
        }

        static void BuildFace(Author a, int head)
        {
            foreach (int sign in new[] { -1, 1 }) {
                float x = sign * .085f;
                a.GuidedEyeSocket(sign,head);
                a.GuidedEyeWhite(sign,head);
                a.PaintedIris(sign,head);
                a.GuidedUpperEyeLine(sign,head);
                a.FaceLine(new[] { new Vector2(x - .031f, .742f), new Vector2(x, .748f), new Vector2(x + .029f, .742f) }, .0016f, Hex("83666B"), head,"brow");
            }
            a.Mouth(head);
            a.Disk(a.face,new Vector2(0,.652f),new Vector2(.0015f,.0018f),Hex("E5B5A6"),head,.0018f,"",16);
        }

        static void BuildHair(Author a, int head)
        {
            // All rear surfaces now come from authored sparse-section
            // reconstructions; keep the original five secondary bone frames.
            for(int i=0;i<5;i++) {
                float theta=Mathf.Lerp(-1.12f,-Mathf.PI*2+1.12f,i/4f);
                a.Bone("J_Sec_Hair"+(i+1)+"_01",head,V(Mathf.Sin(theta)*.16f,.90f,Mathf.Cos(theta)*.13f));
            }
            // One connected measured envelope reaches the actual cap border.
            // Shared lower-row indices branch into independently shaped tips;
            // sparse side underlays and fork contours retain separate coverage.
            // Two adjoining locks are split into a trunk and separate fork tips.
            a.GuidedRearCurtain(head);
            for(int panel=5;panel<FishRearHairGuides.Count;panel++)a.GuidedRearPanel(panel,head);
            for(int panel=0;panel<FishRearForkGuides.Count;panel++) {
                if(panel==0||panel==3)a.GuidedSideLock(panel,a.ids["J_Sec_Hair"+FishRearForkGuides.BoneGroup(panel)+"_01"],true);
                else a.GuidedRearPanel(panel,head,true);
            }
            // One low-density upper dome replaces the overlapping crown pipes.
            int domeStart=a.body.p.Count,oldRegion=a.body.region;a.body.region=0;
            const int domeRows=12,domeCols=RearDomeColumns;
            for(int row=0;row<=domeRows;row++)for(int col=0;col<=domeCols;col++) {
                float angle=col*360f/domeCols;
                float front=Mathf.SmoothStep(0,1,Mathf.Cos(angle*Mathf.Deg2Rad));
                Vector3 final=FishRearCrownGuides.Surface(Mathf.Lerp(-5+35*front,90,row/(float)domeRows),angle);
                final-=(final-V(0,.77f,-.05f)).normalized*(.003f*front*(1-row/(float)domeRows));
                // Measured side locks conform beneath the analytic dome; do
                // not project its edge back onto the replaced hand-drawn pipes.
                Vector3 author=AuthorPoint(final);
                if(row==0&&col>=domeCols/4&&col<=domeCols*3/4)a.rearAttachmentReport.Add(RearAttachmentLine("cap",col,final));
                if(row==0)a.rearAttachmentReport.Add(RearAttachmentLine("side-cap",col,final));
                int vertex=a.body.Add(author,Color.white*.97f,head,RearPaintFinalUV(final));
                Vector3 normal=FishRearCrownGuides.Normal(final);
                float derivative;FitHeight(author.y,out derivative);normal.y*=derivative;
                a.body.normalBlends[vertex]=new Vector4(normal.x,normal.y,normal.z,1);
            }
            Grid(a.body,domeStart,domeRows,domeCols);
            for(int row=0;row<=domeRows;row++)a.body.smoothNormalPairs.Add(new Vector2Int(domeStart+row*(domeCols+1),domeStart+row*(domeCols+1)+domeCols));
            for(int col=1;col<=domeCols;col++)a.body.smoothNormalPairs.Add(new Vector2Int(domeStart+domeRows*(domeCols+1),domeStart+domeRows*(domeCols+1)+col));
            a.body.region=oldRegion;
            foreach(int sign in new[]{-1,1}) {
                int bone=a.ids[sign<0?"J_Sec_Hair1_01":"J_Sec_Hair5_01"];
                // An outer lower curl and a higher folded ribbon describe
                // different silhouettes. Their depth follows the back curtain,
                // instead of putting three parallel wisps in front of the shoulder.
                a.GuidedSideLock(sign<0?0:1,bone);
                a.GuidedSideCurl(sign<0?0:1,bone,head);
                // Each higher Ref curl has a front and back surface at almost
                // the same contour. Their combined visible sections form one
                // closed ribbon, preserving both edges without a duplicate wisp.
                a.GuidedSideCurl(sign<0?2:3,bone,head);
                // Independently measured left/right S locks and outward forks
                // use closed sparse section lofts, retaining transverse volume
                // without mirrored sections or copied source topology.
                a.GuidedChestPanel(sign<0?0:1,bone);
                a.GuidedChestPanel(sign<0?3:2,bone);
                a.GuidedChestBacking(sign<0?0:1,bone);
                for(int panel=0;panel<7;panel++)a.GuidedFrontPanel(panel,sign,head,panel%3);
                a.GuidedFrontPanel(7,sign,head,sign);
                // Corresponding Ref rays locate these two underlying locks.
                // Their sparse sections close forehead gaps without enlarging
                // the outer fringe or shrinking the skin shell.
                a.GuidedFrontPanel(15,sign,head,0);
                a.GuidedFrontPanel(16,sign,head,1);
            }
            for(int panel=8;panel<15;panel++)a.GuidedFrontPanel(panel,1,head,panel%3);
            // Source-measured sparse support sections reconstruct the whole
            // hooked silhouette with independent topology and shared end poles.
            a.GuidedCrownTuft(head);
            File.WriteAllLines("work/rear-attachment.csv",new[]{"kind,panel,x,y,z"}.Concat(a.rearAttachmentReport));
        }

        static string RearAttachmentLine(string kind,int panel,Vector3 p) {
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            return kind+","+panel+","+p.x.ToString("R",culture)+","+p.y.ToString("R",culture)+","+p.z.ToString("R",culture);
        }

        // Height-based shaping keeps the uneven inner hem in one consistent
        // frame: the side edge opens at the waist and returns at the tip.
        static Vector3 InnerEdgePoint(Vector3 p) {
            float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.11f,.24f,Mathf.Abs(p.x)));
            float tip=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.26f,.145f,p.y));
            float waist=.12f*Mathf.Exp(-Mathf.Pow((p.y-.35f)/.065f,2));
            p.x*=1-edge*(waist+.14f*tip);
            float shoulder=.085f*Mathf.Exp(-Mathf.Pow((p.y-.60f)/.13f,2));
            p.z-=edge*(.085f*tip+shoulder);
            return p;
        }

        static float HairSweepAtAngle(float angle) {
            float signed=Mathf.DeltaAngle(0,angle*Mathf.Rad2Deg)*Mathf.Deg2Rad;
            float side=Mathf.Clamp01((Mathf.PI-Mathf.Abs(signed))/(Mathf.PI-1.12f));
            return Mathf.Sign(signed)*.75f*side;
        }
        // Rounded section guides for the front edge of the rear curtain.
        // Side locks move back above the shoulder and forward at the waist,
        // so their profile falls continuously instead of opening a large arch.
        static readonly float[] SideCoverageY={.13f,.215f,.274f,.338f,.422f,.530f,.605f,.655f,.717f,.779f,.836f,.906f};
        static readonly float[] SideCoverageX={0,.030f,.025f,0,-.010f,.005f,.030f,.060f,.065f,.050f,.025f,0};
        static readonly float[] SideCoverageZ={0,.040f,.050f,.065f,.045f,-.060f,-.130f,-.160f,-.170f,-.150f,-.080f,0};
        // Keep the rear center depth while rounding the shoulder of each
        // horizontal section. The old broad, almost flat back over-inflated
        // the sides; all rear layers share the same continuous correction.
        static float RearMassOffset(Vector3 p) {
            float band=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.340f,.530f,p.y))*
                (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.800f,.925f,p.y)));
            float rear=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.180f,.025f,p.z));
            return Mathf.Min(.115f,1.5f*p.x*p.x)*band*rear;
        }
        static Vector3 RoundRearMass(Vector3 p) => p+V(0,0,RearMassOffset(p));
        static Vector3 RearMassSlope(Vector3 p) {
            const float e=.0001f;
            return V((RearMassOffset(p+V(e,0,0))-RearMassOffset(p-V(e,0,0)))/(2*e),
                (RearMassOffset(p+V(0,e,0))-RearMassOffset(p-V(0,e,0)))/(2*e),
                (RearMassOffset(p+V(0,0,e))-RearMassOffset(p-V(0,0,e)))/(2*e));
        }

        static Vector3 SideHairCoverage(float y) {
            if(y<=SideCoverageY[0]||y>=SideCoverageY[SideCoverageY.Length-1])return Vector3.zero;
            int i=0;while(y>SideCoverageY[i+1])i++;
            float h=SideCoverageY[i+1]-SideCoverageY[i],t=(y-SideCoverageY[i])/h,t2=t*t,t3=t2*t;
            Func<int,Vector3> value=k=>V(SideCoverageX[k],0,SideCoverageZ[k]);
            Func<int,Vector3> slope=k=>k==0||k==SideCoverageY.Length-1?Vector3.zero:
                (value(k+1)-value(k-1))/(SideCoverageY[k+1]-SideCoverageY[k-1]);
            return (2*t3-3*t2+1)*value(i)+(t3-2*t2+t)*h*slope(i)+
                (-2*t3+3*t2)*value(i+1)+(t3-t2)*h*slope(i+1);
        }

        static readonly Vector3[][] RearHemControls={
            new[]{V(-0.3456f,0.2887f,0.1205f),V(-0.3375f,0.2363f,0.1263f),V(-0.3360f,0.1836f,0.1249f),V(-0.3204f,0.1559f,0.1299f),V(-0.3045f,0.1361f,0.1353f)},
            new[]{V(-0.3184f,0.2665f,-0.0250f),V(-0.3135f,0.2146f,-0.0210f),V(-0.3135f,0.1602f,-0.0248f),V(-0.2942f,0.1304f,-0.0092f),V(-0.2776f,0.1087f,0.0022f)},
            new[]{V(-0.2859f,0.2646f,-0.1831f),V(-0.2817f,0.2127f,-0.1779f),V(-0.2813f,0.1581f,-0.1822f),V(-0.2619f,0.1280f,-0.1548f),V(-0.2435f,0.1062f,-0.1298f)},
            new[]{V(-0.1778f,0.3035f,-0.2667f),V(-0.1713f,0.2504f,-0.2430f),V(-0.1694f,0.1983f,-0.2436f),V(-0.1573f,0.1715f,-0.2227f),V(-0.1449f,0.1527f,-0.2003f)},
            new[]{V(0.0110f,0.3106f,-0.3119f),V(0.0090f,0.2570f,-0.2851f),V(0.0070f,0.2050f,-0.2825f),V(-0.0010f,0.1786f,-0.2602f),V(-0.0090f,0.1602f,-0.2370f)},
        };
        static readonly float[] RearHairHeights={.14f,.26f,.50f,.65f,.78f};
        static readonly float[] RearHairOffsets={.165f,.205f,.145f,.120f,.120f};
        static Vector3 RearHairPoint(Vector3 p) {
            int k=0;while(k<RearHairHeights.Length-2&&p.y>RearHairHeights[k+1])k++;
            float h=RearHairHeights[k+1]-RearHairHeights[k];
            float t=Mathf.Clamp01((p.y-RearHairHeights[k])/h);
            float offset=Mathf.Lerp(RearHairOffsets[k],RearHairOffsets[k+1],t*t*(3-2*t));
            // Scaling the rounded upper shell in depth retains its crown arc;
            // a height-only displacement would flatten the upper silhouette.
            float crown=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,.78f,p.y));
            float upper=Mathf.Clamp(.55f+1.6f*(p.y-.68f),.55f,1.05f)*Mathf.Max(0,-p.z);
            offset=Mathf.Lerp(offset,upper,crown);
            float rear=Mathf.Clamp01((.04f-p.z)/Mathf.Lerp(.24f,.16f,crown));
            float gate=rear*rear*(3-2*rear);
            return V(p.x,p.y,p.z-offset*gate);
        }
        static Vector3 ShapeRearHair(Vector3 p,out float dy,out float dz) {
            const float e=.0001f;
            dy=(RearHairPoint(p+V(0,e,0)).z-RearHairPoint(p-V(0,e,0)).z)/(2*e);
            dz=(RearHairPoint(p+V(0,0,e)).z-RearHairPoint(p-V(0,0,e)).z)/(2*e);
            return RearHairPoint(p);
        }

        static Vector3 SweepHairSection(Vector3 p,float amount) {
            float sweep=amount*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.74f,.42f,p.y));
            float c=Mathf.Cos(sweep),s=Mathf.Sin(sweep);
            return V(p.x*c+p.z*s,p.y,-p.x*s+p.z*c);
        }

        static void BuildDress(Author a, int hips, int chest)
        {
            int dressStart=a.body.p.Count;
            // Four height intervals and 48 angular samples retain the broad drape.
            Lathe(a,hips,.173f,.375f,t=>Mathf.Lerp(.223f,.113f,t),t=>Mathf.Lerp(.137f,.077f,t),Navy,4,48);
            Lathe(a,hips,.365f,.384f,t=>.116f,t=>.079f,Navy*.82f,1,24);
            GuidedBelt(a,hips);
            GuidedBib(a,chest);
            Collar(a,chest);
            foreach(int sign in new[]{-1,1}) {
                GoldBow(a,V(sign*.142f,.220f,0),hips,.022f);
                Star(a,V(sign*.094f,.217f,0),.0065f,hips);
                Star(a,V(sign*.184f,.223f,0),.0065f,hips);
                // A pair of broad tapered leaves keeps the ornament readable at
                // desktop size; the stem remains separated from the hem bands.
                var stem=Curve(new[]{V(sign*.140f,.242f,0),V(sign*.144f,.249f,0),
                    V(sign*.144f,.258f,0),V(sign*.141f,.265f,0)},12);
                SkirtStroke(a,stem,.0016f,hips);
                SkirtLeaf(a,V(sign*.144f,.251f,0),V(sign*.154f,.258f,0),.0034f,hips);
                SkirtLeaf(a,V(sign*.144f,.256f,0),V(sign*.133f,.263f,0),.0038f,hips);
            }

            foreach(float y in new[]{.184f,.195f}) {
                int start=a.body.p.Count;const int sides=72,rows=1;
                float width=y<.19f?.0027f:.0021f;
                for(int row=0;row<=rows;row++)for(int i=0;i<=sides;i++) {
                    float u=row/(float)rows,theta=i/(float)sides*Mathf.PI*2;
                    Vector3 p=SkirtSurface(theta,y+(u*2-1)*width);
                    p+=V(Mathf.Sin(theta),0,Mathf.Cos(theta))*(.0025f+.0005f*Mathf.Sin(u*Mathf.PI));
                    Color color=Gold*(row==1?1.06f:.93f);color.a=1;
                    a.body.Add(p,color,hips);
                }
                Grid(a.body,start,rows,sides);
            }
            // A flat waist edge opens into a fuller oval cloth panel, with a
            // curved lower outline. The gathered lace follows that same edge.
            a.body.region=5;
            var apronControls=new[]{V(-.078f,.374f,0),V(-.030f,.374f,0),V(.030f,.374f,0),V(.078f,.374f,0),
                V(.108f,.350f,0),V(.120f,.296f,0),V(.106f,.262f,0),
                V(.092f,.226f,0),V(.052f,.207f,0),V(0,.207f,0),
                V(-.052f,.207f,0),V(-.092f,.226f,0),V(-.106f,.262f,0),
                V(-.120f,.296f,0),V(-.108f,.350f,0),V(-.078f,.374f,0)};
            // Keep the outer silhouette while widening the lower lace toward
            // the measured inner hem; a narrow blue seam separates the layers.
            Func<Vector3,Vector3> innerEdge=p=>{
                float lower=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.22f,p.y));
                p.x*=Mathf.Lerp(1,.94f,lower);p.y+=.016f*lower;return p;
            };
            var outline=Curve(apronControls,48).Select(innerEdge).ToArray();
            int apronStart=a.body.p.Count;const int apronRings=4;
            for(int row=0;row<=apronRings;row++)for(int i=0;i<outline.Length;i++) {
                float r=row/(float)apronRings;
                Vector3 p=Vector3.Lerp(V(0,.293f,0),outline[i],r);
                float gather=ApronGather(p.x,p.y);
                Color cloth=White*Mathf.Lerp(.91f,1,(gather+1)*.5f);cloth.a=1;
                a.body.Add(ApronSurface(p.x,p.y),cloth,hips,new Vector2(.5f,3));
            }
            Grid(a.body,apronStart,apronRings,outline.Length-1);
            var lacePath=Curve(apronControls.Skip(3).ToArray(),80);
            int laceStart=a.body.p.Count;const int laceRows=2;
            for(int row=0;row<=laceRows;row++)for(int i=0;i<lacePath.Length;i++) {
                float t=i/(float)(lacePath.Length-1),r=row/(float)laceRows;
                Vector3 tangent=(lacePath[Mathf.Min(i+1,lacePath.Length-1)]-
                    lacePath[Mathf.Max(0,i-1)]).normalized;
                Vector3 outward=V(-tangent.y,tangent.x,0);
                float fold=Mathf.Cos(t*Mathf.PI*40);
                float end=Mathf.SmoothStep(0,1,Mathf.Min(t,1-t)/.045f);
                Vector3 p=Vector3.Lerp(innerEdge(lacePath[i]),lacePath[i]+outward*.019f*end,r);
                p=ApronSurface(p.x,p.y)+Vector3.forward*(Mathf.Sin(r*Mathf.PI)*.004f+fold*r*.002f)*end;
                a.body.Add(p,White,hips,LaceUV(1,t,r));
            }
            Grid(a.body,laceStart,laceRows,lacePath.Length-1);
            var inset=outline.Select(p=>Vector3.Lerp(V(0,.293f,0),p,.975f)).ToArray();
            ApronStroke(a,inset,.0009f,Hex("9FAAC7"),hips);
            a.body.region=0;
            // Shape the whole hip-bound garment together, including embroidery,
            // apron artwork and both lace layers. Keep the waist attachment fixed
            // while giving the hem a deeper, longer bell silhouette.
            for(int i=dressStart;i<a.body.p.Count;i++)
                if(a.body.weights[i].boneIndex0==hips) {
                    Vector3 p=DressVolume(a.body.p[i]);
                    a.body.p[i]=a.body.regions[i]==5?p:CalibrateLowerHem(p);
                }
            GuidedHem(a,hips);
        }

        static Vector3 FinalBodyPoint(Vector3 p) {
            if(p.y<.585f)p.y=.585f-(.585f-p.y)*.88f;
            return FitPoint(p);
        }
        static Vector3 CalibrateLowerHem(Vector3 p) {
            Vector3 q=FinalBodyPoint(p);if(q.y>=.31f)return p;
            float angle=Mathf.Atan2(q.x,q.z),t=Mathf.Repeat(angle/(Mathf.PI*2),1);
            Vector3 ray=V(Mathf.Sin(angle),0,Mathf.Cos(angle)),previous=FinalBodyPoint(DressVolume(SkirtSurface(0,.173f)));
            Vector3 oldEdge=previous;
            // Intersect the former authored ring with this ray. Decorations
            // follow the same smooth correction, preserving thick gold borders.
            for(int i=1;i<=96;i++) {
                Vector3 next=FinalBodyPoint(DressVolume(SkirtSurface(i*Mathf.PI*2/96,.173f)));
                float a=previous.x*ray.z-previous.z*ray.x,b=next.x*ray.z-next.z*ray.x;
                if((a<=0&&b>0)||(b<=0&&a>0)) {
                    Vector3 hit=Vector3.Lerp(previous,next,a/(a-b));
                    if(Vector3.Dot(hit,ray)>0){oldEdge=hit;break;}
                }
                previous=next;
            }
            float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(oldEdge.y,.31f,q.y));
            return AuthorPoint(q+(FishModelGuides.HemEdge(t,false)-oldEdge)*weight);
        }
        static void GuidedHem(Author a,int bone) {
            // Five strips repeat only this atlas tile. Eight scallops per tile
            // leave enough texels for a rounded alpha silhouette and stitched rim.
            const int panels=5,sides=32,rows=3;
            var attachment=Enumerable.Range(0,49).Select(i=>FinalBodyPoint(CalibrateLowerHem(
                DressVolume(SkirtSurface(i*Mathf.PI*2/48,.173f))))).ToArray();
            Func<float,Vector3> join=t=> {
                Vector3 ray=V(Mathf.Sin(t*Mathf.PI*2),0,Mathf.Cos(t*Mathf.PI*2));
                for(int i=0;i<48;i++) {
                    Vector3 p=attachment[i],q=attachment[i+1];
                    float a0=p.x*ray.z-p.z*ray.x,b0=q.x*ray.z-q.z*ray.x;
                    if((a0<=0&&b0>0)||(b0<=0&&a0>0)) {
                        Vector3 hit=Vector3.Lerp(p,q,a0/(a0-b0));if(Vector3.Dot(hit,ray)>0)return hit;
                    }
                }
                return FishModelGuides.HemEdge(t,false);
            };
            var starts=new List<int>();
            for(int panel=0;panel<panels;panel++) {
                int start=a.body.p.Count,first=a.body.tri.Count;starts.Add(start);
                for(int j=0;j<=rows;j++)for(int i=0;i<=sides;i++) {
                    float u=i/(float)sides,t=(panel+u)/panels,r=j/(float)rows,angle=t*Mathf.PI*2;
                    Vector3 inner=join(t),outer=FishModelGuides.HemEdge(t,true);
                    Vector3 radial=V(Mathf.Sin(angle),0,Mathf.Cos(angle));
                    inner-=radial*.005f;inner.y+=.003f;
                    float fold=-Mathf.Cos(t*Mathf.PI*80);
                    Vector3 p=Vector3.Lerp(inner,outer,r);
                    p+=radial*((.008f+.006f*fold)*Mathf.Sin(r*Mathf.PI)*r-.003f*fold*r*r);
                    p.y-=.0020f*fold*r;
                    Color cloth=Color.Lerp(Hex("D3D9E9"),White,.60f+.40f*(fold+1)*.5f);cloth.a=1;
                    a.body.Add(AuthorPoint(p),cloth,bone,LaceUV(2,u,r));
                }
                Grid(a.body,start,rows,sides);ReverseFaces(a.body,first);
            }
            for(int panel=0;panel<panels;panel++)for(int row=0;row<=rows;row++)
                a.body.smoothNormalPairs.Add(new Vector2Int(starts[panel]+row*(sides+1)+sides,starts[(panel+1)%panels]+row*(sides+1)));
        }

        static Vector3 ChestVolumePoint(Vector3 p) {
            float t=Mathf.InverseLerp(.375f,.576f,p.y);
            float front=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.070f,.070f,p.z));
            float offset=.022f*Mathf.Sin(t*Mathf.PI)-.052f*
                Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.90f,t));
            p.z+=offset*front;
            return p;
        }

        static Vector3 DressVolume(Vector3 p) {
            float lower=Mathf.InverseLerp(.375f,.173f,p.y);
            float back=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,-.060f,p.z));
            float depth=Mathf.Lerp(.9f*lower,1.25f*Mathf.Pow(lower,1.25f),back);
            p.z*=1+depth;
            // A full middle section softens the straight cone into a bell.
            // The waist and hem stay fixed, and all surface decoration travels
            // with the same continuous map, including the large whale.
            float fullness=Mathf.Sin(lower*Mathf.PI);
            p.x*=1+.055f*fullness;
            p.z*=1+Mathf.Lerp(.19f,.13f,back)*fullness;
            p.y=p.y>=.173f?p.y-.025f*lower:.148f+(p.y-.173f)*.85f;
            return p;
        }

        static Vector3[] ClosedArtworkCurve(Vector3[] points,int count) {
            var path=new Vector3[count+1];
            for(int i=0;i<=count;i++) {
                float f=(i/(float)count)*points.Length;int j=(int)f%points.Length;float t=f-Mathf.Floor(f);
                Vector3 p0=points[(j+points.Length-1)%points.Length],p1=points[j],p2=points[(j+1)%points.Length],p3=points[(j+2)%points.Length];
                path[i]=.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t);
            }
            float area=0;for(int i=0;i<count;i++)area+=path[i].x*path[i+1].y-path[i+1].x*path[i].y;
            if(area>0)Array.Reverse(path);return path;
        }
        // Surface artwork is painted into the apron UV field, avoiding floating
        // overlay polygons and allocating those triangles to the curved model.
        static System.Collections.Generic.List<Tuple<Vector3[],Color>> ApronPaintShapes() {
            var shapes=new System.Collections.Generic.List<Tuple<Vector3[],Color>>();
            Action<Vector3[],Color> patch=(path,color)=>shapes.Add(Tuple.Create(path,color));
            Action<Vector3,Vector2,Color,int> oval=(center,size,color,sides)=>
                patch(Enumerable.Range(0,sides+1).Select(i=>center+V(Mathf.Cos(-i/(float)sides*Mathf.PI*2)*size.x,Mathf.Sin(-i/(float)sides*Mathf.PI*2)*size.y,0)).ToArray(),color);
            var g=FishModelGuides.ApronDetailAnchors;Color blue=Hex("577FAE");
            patch(ClosedArtworkCurve(new[]{g[0],g[25],g[1],g[26],g[2],g[3],g[4],g[28],g[27]},64),blue);
            var tail=Curve(new[]{g[3]+V(.008f,.006f,0),g[11]+V(.009f,-.008f,0),g[11],(g[9]+g[10])*.5f},28);
            var left=new Vector3[tail.Length];var right=new Vector3[tail.Length];
            for(int i=0;i<tail.Length;i++) {
                Vector3 tangent=(tail[Mathf.Min(i+1,tail.Length-1)]-tail[Mathf.Max(0,i-1)]).normalized;
                Vector3 side=V(tangent.y,-tangent.x,0)*Mathf.Lerp(.009f,.0048f,i/(float)(tail.Length-1));
                left[i]=tail[i]+side;right[i]=tail[i]-side;
            }
            patch(left.Concat(right.Reverse()).ToArray(),blue);
            oval(g[9]+V(.005f,-.001f,0),new Vector2(.009f,.006f),blue,32);
            oval(g[10]+V(-.002f,-.001f,0),new Vector2(.006f,.009f),blue,32);
            patch(ClosedArtworkCurve(new[]{g[30],g[6],g[7],g[8],g[31]},48),Color.white);
            oval(g[29],new Vector2(.0025f,.0028f),Color.white,24);
            oval(g[18],new Vector2(.0038f,.005f),blue,24);
            oval(g[19],new Vector2(.0048f,.0026f),blue,24);
            oval((g[18]+g[19])*.5f+V(-.004f,.010f,0),new Vector2(.0018f,.0024f),blue,20);
            patch(new[]{g[12],g[13],g[14],g[15]},Hex("819BBE"));
            patch(new[]{g[15],g[15]+V(.001f,.009f,0),g[14]+V(.001f,-.001f,0)},Color.white);
            oval(g[16],new Vector2(.0025f,.0017f),Hex("78ADCE"),20);
            oval(g[17],new Vector2(.0020f,.0010f),Hex("9BC6DC"),20);
            return shapes;
        }
        internal static Vector2 ApronPaintUV(Vector3 p) => new Vector2(
            (16+(p.x+.106f)/.212f*480)/PaintWidth,(1680+(p.y-.235f)/.175f*352)/PaintHeight);
        static bool InsideArtwork(Vector3[] polygon,float x,float y) {
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++) {
                Vector3 a=polygon[i],b=polygon[j];
                if((a.y>y)!=(b.y>y)&&x<(b.x-a.x)*(y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
        static void PaintApron(Color[] pixels) {
            for(int y=1664;y<PaintHeight;y++)for(int x=0;x<PaintWidth;x++)pixels[y*PaintWidth+x]=Color.white;
            foreach(var shape in ApronPaintShapes()) {
                var path=shape.Item1;float minX=path.Min(p=>p.x),maxX=path.Max(p=>p.x),minY=path.Min(p=>p.y),maxY=path.Max(p=>p.y);
                int x0=Mathf.FloorToInt(16+(minX+.106f)/.212f*480)-1,x1=Mathf.CeilToInt(16+(maxX+.106f)/.212f*480)+1;
                int y0=Mathf.FloorToInt(1680+(minY-.235f)/.175f*352)-1,y1=Mathf.CeilToInt(1680+(maxY-.235f)/.175f*352)+1;
                for(int y=Mathf.Max(1664,y0);y<=Mathf.Min(PaintHeight-1,y1);y++)for(int x=Mathf.Max(0,x0);x<=Mathf.Min(PaintWidth-1,x1);x++) {
                    int covered=0;
                    for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++) {
                        float px=(x+(sx+.5f)*.5f-16)/480*.212f-.106f;
                        float py=(y+(sy+.5f)*.5f-1680)/352*.175f+.235f;
                        if(InsideArtwork(path,px,py))covered++;
                    }
                    int index=y*PaintWidth+x;pixels[index]=Color.Lerp(pixels[index],shape.Item2,covered*.25f);
                }
            }
        }

        static float ApronGather(float x,float y) {
            float t=Mathf.InverseLerp(.374f,.207f,y);
            return Mathf.Cos(x/(.078f+.025f*t)*Mathf.PI*5+.4f*Mathf.Sin(t*5))*
                Mathf.Pow(1-t,1.5f);
        }
        static Vector3 UndressPoint(Vector3 p) {
            float lo=.10f,hi=.45f;
            for(int i=0;i<26;i++) {float y=(lo+hi)*.5f;if(DressVolume(V(0,y,0)).y<p.y)lo=y;else hi=y;}
            float sourceY=(lo+hi)*.5f,lower=Mathf.InverseLerp(.375f,.173f,sourceY);
            float fullness=Mathf.Sin(lower*Mathf.PI);
            return V(p.x/(1+.055f*fullness),sourceY,p.z/((1+.9f*lower)*(1+.19f*fullness)));
        }
        public static Vector3 FitApronToGuides(Vector3 p,Bounds source) {
            Vector3 old=UndressPoint(AuthorPoint(p));
            float lift=old.z-ApronSurface(old.x,old.y).z;
            float t=(p.y-source.min.y)/source.size.y;
            Vector3 target=p;target.x*=.096f/source.extents.x;
            target.y=.2353f+(.40034f-.2353f)*t;
            float finalY=target.y;
            target=UndressPoint(AuthorPoint(target));
            float centerDepth=DressVolume(ApronSurface(0,target.y)).z;
            float sheetLift=DressVolume(ApronSurface(target.x,target.y)+Vector3.forward*lift).z-DressVolume(ApronSurface(target.x,target.y)).z;
            target=DressVolume(ApronSurface(target.x,target.y)+Vector3.forward*lift);
            float centerWeight=Mathf.Pow(Mathf.Clamp01(1-Mathf.Pow(target.x/.096f,2)),2);
            float upper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.37f,finalY));
            target.z+=(FishModelGuides.ApronDepth(finalY)-centerDepth)*centerWeight*upper;
            if(target.y<.585f)target.y=.585f-(.585f-target.y)*.88f;
            target=FitPoint(target);
            float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.37f,.398f,target.y));
            float folds=.0015f*ApronGather(old.x,old.y);
            target.z=Mathf.Lerp(target.z,FishModelGuides.ApronSheetDepth(target.x,target.y)+sheetLift+folds,blend);
            return target;
        }
        static Vector3 ApronSurface(float x,float y) {
            float t=Mathf.InverseLerp(.173f,.375f,y);
            float rx=Mathf.Lerp(.223f,.113f,t),rz=Mathf.Lerp(.137f,.077f,t);
            float curve=Mathf.Sqrt(Mathf.Max(.08f,1-x*x/(rx*rx)));
            float drape=Mathf.Sin(Mathf.InverseLerp(.207f,.374f,y)*Mathf.PI);
            float z=rz*curve+.009f+.005f*drape+.002f*ApronGather(x,y);
            return V(x,y,z);
        }
        static void ApronStroke(Author a,Vector3[] path,float radius,Color color,int bone) {
            int start=a.body.p.Count;const int cols=2;
            for(int i=0;i<path.Length;i++)for(int j=0;j<=cols;j++) {
                Vector3 tangent=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;
                Vector3 p=path[i]+V(tangent.y,-tangent.x,0)*radius*(j/(float)cols*2-1);
                a.body.Add(ApronSurface(p.x,p.y)+Vector3.forward*.0015f,color,bone);
            }
            Grid(a.body,start,path.Length-1,cols);
        }

        // Undo the common chest volume before adding already registered guides.
        static Vector3 AuthorGarmentPoint(Vector3 final) {
            Vector3 p=AuthorPoint(final);float lo=-.2f,hi=.3f;
            for(int i=0;i<28;i++) {float mid=(lo+hi)*.5f;if(ChestVolumePoint(V(p.x,p.y,mid)).z<final.z)lo=mid;else hi=mid;}
            p.z=(lo+hi)*.5f;return p;
        }
        static void GuidedBelt(Author a,int bone) {
            int start=a.body.p.Count;const int cols=12;
            for(int row=0;row<=1;row++)for(int col=0;col<=cols;col++) {
                float u=col/(float)cols*2-1;Vector3 p=V(u*.102f,Mathf.Lerp(.394f,.416f,row),Mathf.Lerp(.110f,.1155f,row)-.033f*u*u);
                a.body.Add(UndressPoint(AuthorPoint(p)),Navy*.90f,bone);
            }
            Grid(a.body,start,1,cols);
            foreach(Vector3 center in FishModelGuides.BeltStuds) {
                int first=a.body.p.Count;Vector3 c=center+Vector3.forward*.0008f;
                a.body.Add(UndressPoint(AuthorPoint(c)),Hex("FFF0BB"),bone);
                for(int i=0;i<8;i++) {
                    float angle=i*Mathf.PI/4;Vector3 p=c+V(Mathf.Cos(angle)*.0041f,Mathf.Sin(angle)*.0028f,-.0003f);
                    a.body.Add(UndressPoint(AuthorPoint(p)),Gold,bone);
                }
                for(int i=0;i<8;i++)a.body.Face(first,first+1+i,first+1+(i+1)%8);
            }
        }

        static void GuidedBib(Author a,int bone) {
            int start=a.body.p.Count;const int rows=12,cols=14;
            for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++) {
                float t=row/(float)rows,u=col/(float)cols*2-1;
                Vector3 p=FishBibSurfaceGuides.Point(Mathf.Lerp(.416f,.514f,t),u);
                a.body.Add(AuthorGarmentPoint(p),Color.white,bone,BibPaintUV((u+1)*.5f,t));
            }
            Grid(a.body,start,rows,cols);
            foreach(int sign in new[]{-1,1}) {
                // The measured front bands recess behind the folded frill;
                // a straight bridge to its inner rim would hide the outer edge.
                int band=a.body.p.Count,first=a.body.tri.Count;const int bandRows=18,bandCols=4;
                for(int row=0;row<=bandRows;row++)for(int col=0;col<=bandCols;col++) {
                    float y=Mathf.Lerp(.414f,.514f,row/(float)bandRows),r=col/(float)bandCols;
                    Vector3 p=FishSideLaceGuides.Strap(sign,y,r);p.z-=.0008f;
                    p.x+=sign*Mathf.Lerp(-.0005f,.0005f,r);
                    a.body.Add(AuthorGarmentPoint(p),Navy*.95f,bone);
                }
                Grid(a.body,band,bandRows,bandCols);if(sign<0)ReverseFaces(a.body,first);
                int lace=a.body.p.Count;first=a.body.tri.Count;const int laceRows=36,laceCols=4;
                for(int row=0;row<=laceRows;row++)for(int col=0;col<=laceCols;col++) {
                    float t=row/(float)laceRows,r=col/(float)laceCols;
                    float y=Mathf.Lerp(FishSideLaceGuides.Low(sign),FishSideLaceGuides.High(sign),t);
                    Vector3 p=FishSideLaceGuides.Point(sign,y,r);
                    a.body.Add(AuthorGarmentPoint(p),Color.white,bone,SideLaceUV(t,r));
                }
                Grid(a.body,lace,laceRows,laceCols);if(sign<0)ReverseFaces(a.body,first);
            }
        }

        static void Collar(Author a,int bone) {
            int start=a.body.p.Count;const int sides=24;
            for(int row=0;row<=1;row++)for(int i=0;i<=sides;i++) {
                float t=(float)row,theta=i/(float)sides*Mathf.PI*2;
                float radius=Mathf.Lerp(.038f,.031f,t);
                a.body.Add(V(Mathf.Sin(theta)*radius,Mathf.Lerp(.550f,.576f,t),
                    Mathf.Cos(theta)*radius),White*.94f,bone);
            }
            Grid(a.body,start,1,sides);
            foreach(int sign in new[]{-1,1}) {
                int first=a.body.p.Count,triangles=a.body.tri.Count;const int rows=12,cols=4;
                for(int i=0;i<=rows;i++)for(int j=0;j<=cols;j++) {
                    Vector3 p=FishModelGuides.CollarFold(i/(float)rows,j/(float)cols);p.x*=sign;
                    Color color=White*Mathf.Lerp(1,.92f,j/(float)cols);color.a=1;
                    a.body.Add(AuthorGarmentPoint(p),color,bone);
                }
                Grid(a.body,first,rows,cols);if(sign<0)ReverseFaces(a.body,triangles);
            }
            int underlap=a.body.p.Count;
            foreach(Vector3 p in new[]{V(-.004f,.537f,.009f),V(.004f,.537f,.009f),V(-.004f,.513f,.046f),V(.004f,.513f,.046f)})
                a.body.Add(AuthorGarmentPoint(p),White,bone);
            a.body.Face(underlap,underlap+2,underlap+1);a.body.Face(underlap+1,underlap+2,underlap+3);
        }

        static void ChestRibbon(Author a,int bone) {
            foreach(int sign in new[]{-1,1}) {
                foreach(int piece in new[]{0,1,2}) {
                    int rows=FishRibbonGuides.Count(sign,piece)-1,cols=piece==0?4:1;
                    int start=a.body.p.Count,triangles=a.body.tri.Count;
                    for(int i=0;i<=rows;i++)for(int j=0;j<=cols;j++) {
                        float x=FishRibbonGuides.Station(sign,piece,i),v=j/(float)cols;
                        Vector3 p=FishRibbonGuides.Point(sign,piece,x,v);
                        float t=Mathf.InverseLerp(FishRibbonGuides.Station(sign,0,0),FishRibbonGuides.Station(sign,0,FishRibbonGuides.Count(sign,0)-1),x);
                        float crease=Mathf.Exp(-Mathf.Pow((v-.35f)/.15f,2))*Mathf.Sin(t*Mathf.PI);
                        Color color=Color.Lerp(Hex("2C3F80"),Hex("142650"),crease*.9f)*(piece==0?1:.81f);color.a=1;
                        a.body.Add(AuthorGarmentPoint(p),color,bone);
                    }
                    Grid(a.body,start,rows,cols);
                    if((sign>0)==(piece!=2))ReverseFaces(a.body,triangles);
                }
                // A broad painted fold is represented by a shallow strip so
                // the index-only simplifier cannot erase its color boundary.
                int foldStart=a.body.p.Count,foldTriangles=a.body.tri.Count;
                for(int i=0;i<=5;i++)for(int j=0;j<=2;j++) {
                    float t=i/5f,u=Mathf.Lerp(.08f,.64f,t),width=.115f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.7f);
                    Vector3 p=FishRibbonGuides.Fold(sign,u,.43f+(j-1)*width);p.z+=.0012f;
                    a.body.Add(AuthorGarmentPoint(p),Hex("172650"),bone);
                }
                Grid(a.body,foldStart,5,2);if(sign>0)ReverseFaces(a.body,foldTriangles);
                int tail=a.body.p.Count,first=a.body.tri.Count;const int tailRows=7;
                for(int i=0;i<=tailRows;i++)for(int j=0;j<=4;j++) {
                    float t=i/(float)tailRows,v=j/4f;Vector3 p=FishModelGuides.RibbonTail(t,v);p.x*=sign;
                    Color color=Color.Lerp(Hex("2C3F80"),Navy,.18f*t+.12f*v);color.a=1;
                    a.body.Add(AuthorGarmentPoint(p),color,bone);
                }
                Grid(a.body,tail,tailRows,4);if(sign<0)ReverseFaces(a.body,first);
            }
            // Registered bounds of the separate central clasp. Broad facets
            // and a raised gold rim stay readable at desktop scale.
            var outline=new[]{V(0,1,0),V(.72f,.78f,0),V(1,.45f,0),V(.85f,-.25f,0),V(.52f,-.82f,0),V(0,-1,0),V(-.52f,-.82f,0),V(-.85f,-.25f,0),V(-1,.45f,0),V(-.72f,.78f,0)};
            int gem=a.body.p.Count;Vector3 center=V(0,.51519f,.06062f);
            for(int ring=0;ring<3;ring++)foreach(Vector3 q in outline) {
                float r=new[]{1f,.78f,.65f}[ring];Vector3 p=center+V(q.x*.01179f*r,q.y*.01031f*r,new[]{-.0008f,.0003f,.0012f}[ring]);
                Color color=ring<2?Gold:Hex("537BA9");
                if(ring==2)color=Color.Lerp(color,Hex("A7D9EE"),Mathf.Clamp01((q.y-q.x)*.4f+.3f));
                a.body.Add(AuthorGarmentPoint(p),color,bone);
            }
            for(int ring=0;ring<2;ring++)for(int j=0;j<10;j++) {
                int a0=gem+ring*10+j,b0=gem+ring*10+(j+1)%10,c0=a0+10,d0=b0+10;
                a.body.Face(a0,c0,b0);a.body.Face(b0,c0,d0);
            }
            int middle=a.body.Add(AuthorGarmentPoint(center+Vector3.forward*.002f),Hex("7FBDD8"),bone);
            for(int j=0;j<10;j++)a.body.Face(middle,gem+20+(j+1)%10,gem+20+j);
        }

        static void Lathe(Author a,int bone,float y0,float y1,Func<float,float> rx,Func<float,float> rz,Color color,int rows,int sides) {
            int start=a.body.p.Count;
            for(int y=0;y<=rows;y++)for(int x=0;x<=sides;x++) {
                float t=y/(float)rows,theta=x/(float)sides*Mathf.PI*2;
                float pleat=1+.045f*Mathf.Cos(theta*12)*(1-t);
                Color foldColor = Color.Lerp(color * .90f, color * 1.06f, (Mathf.Cos(theta*12)+1)*.5f);
                foldColor.a=1;
                a.body.Add(V(Mathf.Sin(theta)*rx(t)*pleat,Mathf.Lerp(y0,y1,t),Mathf.Cos(theta)*rz(t)*pleat),foldColor,bone);
            }
            Grid(a.body,start,rows,sides);
        }
        static Vector3 SleeveSection(float x) {
            // The compact body map compresses sleeve height; a fuller cuff
            // opens around the wrist while its depth stays restrained.
            float opening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.278f,.290f,x));
            return V(Mathf.Lerp(1,1.65f,opening),Mathf.Lerp(.95f,1.10f,opening),-.005f*opening);
        }

        static Vector3 SleevePoint(int sign,float x,float theta,float relief=0) {
            float[] sx={.092f,.110f,.130f,.153f,.180f,.205f,.222f,.235f,.248f,.268f,.290f,.309f,.326f,.342f};
            float[] sr={.025f,.040f,.045f,.046f,.041f,.035f,.033f,.032f,.032f,.030f,.028f,.0275f,.0275f,.027f};
            int row=1;while(row<sx.Length-1&&sx[row]<x)row++;
            float radius=Mathf.Lerp(sr[row-1],sr[row],Mathf.InverseLerp(sx[row-1],sx[row],x));
            float gathering=Mathf.Sin(Mathf.InverseLerp(.11f,.205f,x)*Mathf.PI);
            radius*=1+.035f*Mathf.Cos(theta*4+.6f)*gathering;
            var section=SleeveSection(x);
            Vector3 original=V(sign*x,.49f+Mathf.Cos(theta)*radius*section.x+section.z,Mathf.Sin(theta)*radius*section.y);
            var measured=AuthorPoint(FishSleeveGuides.Point(sign,x,theta));
            var point=Vector3.Lerp(original,measured,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.130f,FishSleeveGuides.FirstX,x)));
            if(relief>0) {
                // Share the same shaped sleeve surface with both embroidery bands.
                Vector3 n=V(0,Mathf.Cos(theta),Mathf.Sin(theta));point+=n*relief;
            }
            return point;
        }

        static void BuildLimbs(Author a,int hips,int chest)
        {
            foreach(int s in new[]{-1,1}) {
                string side=s==-1?"L":"R", prefix="J_Bip_"+side+"_";
                int shoulder=a.Bone(prefix+"Shoulder",chest,V(s*.10f,.49f,0));
                int upper=a.Bone(prefix+"UpperArm",shoulder,V(s*.13f,.49f,0));
                int elbow=a.Bone(prefix+"LowerArm",upper,V(s*.235f,.49f,0));
                int hand=a.Bone(prefix+"Hand",elbow,V(s*.34f,.49f,0));
                // One rounded sleeve bridges shoulder and elbow with continuous
                // weights; the cuff remains rigid with the forearm.
                float[] sleeveX=new[]{.092f,.110f,.130f}.Concat(FishSleeveGuides.Stations(s)).Concat(new[]{.342f}).ToArray();
                int sleeveStart=a.body.p.Count,sleeveTriangles=a.body.tri.Count;const int sleeveSides=12;
                for(int row=0;row<sleeveX.Length;row++)for(int j=0;j<=sleeveSides;j++) {
                    float x=sleeveX[row],theta=j/(float)sleeveSides*Mathf.PI*2;
                    float gathering=Mathf.Sin(Mathf.InverseLerp(.11f,.205f,x)*Mathf.PI);
                    float fold=Mathf.Cos(theta*4+.6f)*gathering;
                    int primary=x<.18f?chest:upper,secondary=x<.18f?upper:elbow;
                    float blend=x<.18f?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.070f,.180f,x)):
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.192f,.278f,x));
                    Color cloth=Navy*(.98f+.025f*fold);cloth.a=1;
                    a.body.Add(SleevePoint(s,x,theta),
                        cloth,primary,null,secondary,blend);
                }
                Grid(a.body,sleeveStart,sleeveX.Length-1,sleeveSides);
                if(s<0)ReverseFaces(a.body,sleeveTriangles);
                // Keep the duplicated closure vertices for the grid, but shade
                // the closed sleeve as one surface on both sides of its seam.
                for(int row=0;row<sleeveX.Length;row++) {
                    int first=sleeveStart+row*(sleeveSides+1);
                    a.body.smoothNormalPairs.Add(new Vector2Int(first,first+sleeveSides));
                }
                foreach(float centerX in new[]{.321f,.333f}) {
                    // Shallow embroidered bands follow the same measured
                    // sleeve profile instead of intersecting it as round tubes.
                    int bandStart=a.body.p.Count,bandTriangles=a.body.tri.Count;
                    const int bandSegments=12;
                    for(int row=0;row<2;row++) {
                        float x=centerX+(row*2-1)*.0018f;
                        for(int j=0;j<=bandSegments;j++) {
                            float theta=j/(float)bandSegments*Mathf.PI*2;
                            a.body.Add(SleevePoint(s,x,theta,.0009f),Gold,elbow);
                        }
                    }
                    Grid(a.body,bandStart,1,bandSegments);
                    if(s<0)ReverseFaces(a.body,bandTriangles);
                }
                // One soft volume joins wrist and palm. Gradual skin weights
                // avoid the seam between two independently turning spheres.
                int palmStart=a.body.p.Count,palmTriangles=a.body.tri.Count;
                const int palmRings=8,palmSides=10;
                for(int row=0;row<=palmRings;row++)for(int j=0;j<=palmSides;j++) {
                    float t=row/(float)palmRings,theta=j/(float)palmSides*Mathf.PI*2;
                    // Keep the wrist and its skinning transition fixed while
                    // shortening only the distal palm under the finger roots.
                    float x=.319f+.062f*t-.006f*
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,t));
                    float cap=row==palmRings?0:
                        Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(Mathf.Max(0,(t-.72f)/.28f),2)));
                    // A broad, soft metacarpal volume tapers into the knuckle
                    // roots instead of ending as a thick oval over the fingers.
                    float distal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.36f,.82f,t));
                    float ry=.018f*(1+.08f*Mathf.Sin(t*Mathf.PI))*cap*(1-.32f*distal);
                    float rz=(.019f+.007f*Mathf.Sin(t*Mathf.PI))*cap;
                    float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.326f,.350f,x));
                    a.body.Add(V(s*x,.49f+Mathf.Cos(theta)*ry,Mathf.Sin(theta)*rz),
                        Skin,elbow,null,hand,blend);
                }
                Grid(a.body,palmStart,palmRings,palmSides);
                if(s<0)ReverseFaces(a.body,palmTriangles);
                for(int row=0;row<palmRings;row++) {
                    int first=palmStart+row*(palmSides+1);
                    a.body.smoothNormalPairs.Add(new Vector2Int(first,first+palmSides));
                }
                string[] names={"Index","Middle","Ring","Little"};
                for(int f=0;f<4;f++) {
                    // The index finger sits next to the thumb; the knuckle line
                    // forms a shallow arc instead of three equal-length tips.
                    float z=(1.5f-f)*.010f;
                    float length=new[]{.027f,.030f,.028f,.022f}[f];
                    float fingerRadius=new[]{.0073f,.0076f,.0072f,.0067f}[f];
                    int previous=hand;
                    var joints=new int[3];
                    for(int joint=1;joint<=3;joint++) {
                        int bone=a.Bone(prefix+names[f]+joint,previous,V(s*(.369f+(joint-1)*.007f),.49f,z));
                        joints[joint-1]=bone;
                        previous=bone;
                    }
                    // One continuous rounded finger replaces three overlapping balls.
                    int start=a.body.p.Count;const int rings=7,sides=8;
                    for(int row=0;row<=rings;row++)for(int j=0;j<=sides;j++) {
                        float theta=j/(float)sides*Mathf.PI*2;
                        // Five stem intervals and four angular cap intervals
                        // produce a round fingertip without an elongated point.
                        float capAngle=Mathf.Max(0,(row-4)/3f)*Mathf.PI*.5f;
                        float x=row<=4?.366f+(length-fingerRadius)*(row/4f):
                            .366f+length-fingerRadius+Mathf.Sin(capAngle)*fingerRadius;
                        float radius=row<=4?fingerRadius*Mathf.Lerp(1.10f,1,row/4f):
                            fingerRadius*Mathf.Max(0,Mathf.Cos(capAngle));
                        int primary,secondary;float blend;
                        if(x<.373f) {
                            primary=hand;secondary=joints[0];
                            blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.365f,.372f,x));
                        } else if(x<.380f) {
                            primary=joints[0];secondary=joints[1];
                            blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.373f,.379f,x));
                        } else {
                            primary=joints[1];secondary=joints[2];
                            blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.380f,.386f,x));
                        }
                        a.body.Add(V(s*x,.49f+Mathf.Cos(theta)*radius,z+Mathf.Sin(theta)*radius),
                            Skin,primary,null,secondary,blend);
                    }
                    int fingerTriangles=a.body.tri.Count;
                    Grid(a.body,start,rings,sides);
                    if(s<0)ReverseFaces(a.body,fingerTriangles);
                    for(int row=0;row<rings;row++) {
                        int first=start+row*(sides+1);
                        a.body.smoothNormalPairs.Add(new Vector2Int(first,first+sides));
                    }
                }
                int thumb=a.Bone(prefix+"Thumb1",hand,V(s*.353f,.487f,.023f));
                int thumbStart=a.body.p.Count;
                var thumbPath=Curve(new[]{V(s*.348f,.490f,.014f),V(s*.353f,.486f,.024f),
                    V(s*.365f,.484f,.031f),V(s*.374f,.484f,.031f)},12);
                a.Sweep(a.body,thumbPath,
                    t=>(.0118f-.002f*t)*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(Mathf.Max(0,(t-.65f)/.35f),2))),
                    t=>(.0108f-.001f*t)*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(Mathf.Max(0,(t-.65f)/.35f),2))),
                    t=>Skin,thumb,10);
                for(int row=0;row<thumbPath.Length-1;row++)
                    a.body.smoothNormalPairs.Add(new Vector2Int(thumbStart+row*11,thumbStart+row*11+10));
                int thigh=a.Bone(prefix+"UpperLeg",hips,V(s*.065f,.27f,0));
                int knee=a.Bone(prefix+"LowerLeg",thigh,V(s*.065f,.16f,0));
                int foot=a.Bone(prefix+"Foot",knee,V(s*.065f,.062f,0));
                a.Sphere(a.body,V(s*.065f,.208f,0),V(.036f,.062f,.038f),Skin,thigh);
                RebuiltFoot(a,s,knee,foot);

            }
        }

        static void RebuiltFoot(Author a,int sign,int knee,int foot) {
            const int sides=24;
            // Loft the visible opening into the wide toe and shallow outsole.
            int start=a.body.p.Count,triangles=a.body.tri.Count;
            for(int row=0;row<=4;row++)for(int i=0;i<=sides;i++) {
                float t=i/(float)sides;Vector3 p=FishModelGuides.FootRing(t,row);p.x*=sign;
                float toe=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI*2)),2);
                Color color=Color.Lerp(Hex("293E71"),Hex("3F5A99"),toe*(row==1?.8f:.25f));
                a.body.Add(AuthorPoint(p),color,foot);
            }
            Grid(a.body,start,4,sides);if(sign<0)ReverseFaces(a.body,triangles);
            for(int row=0;row<=4;row++)a.body.smoothNormalPairs.Add(new Vector2Int(start+row*(sides+1),start+row*(sides+1)+sides));
            start=a.body.p.Count;triangles=a.body.tri.Count;
            for(int row=4;row<=5;row++)for(int i=0;i<=sides;i++) {
                Vector3 p=FishModelGuides.FootRing(i/(float)sides,row);p.x*=sign;
                a.body.Add(AuthorPoint(p),Hex("202B4D"),foot);
            }
            Grid(a.body,start,1,sides);if(sign<0)ReverseFaces(a.body,triangles);
            for(int row=0;row<2;row++)a.body.smoothNormalPairs.Add(new Vector2Int(start+row*(sides+1),start+row*(sides+1)+sides));
            int soleRing=start+sides+1,soleCenter=a.body.p.Count;
            Vector3 cap=V(sign*.069f,.071f,.025f);a.body.Add(AuthorPoint(cap),Hex("202B4D"),foot);
            int capTriangles=a.body.tri.Count;
            for(int i=0;i<sides;i++)a.body.Face(soleCenter,soleRing+i,soleRing+i+1);
            if(sign<0)ReverseFaces(a.body,capTriangles);
            // One broad painted-style highlight reads at desktop scale.
            int shine=a.body.p.Count,shineTriangles=a.body.tri.Count;
            for(int i=-1;i<8;i++) {
                float angle=i<0?0:i*Mathf.PI*2/8;
                float t=.18f+(i<0?0:Mathf.Cos(angle)*.018f),r=.28f+(i<0?0:Mathf.Sin(angle)*.25f);
                Vector3 p=Vector3.Lerp(FishModelGuides.FootRing(t,1),FishModelGuides.FootRing(t,2),r);p.y+=.0009f;p.z+=.0003f;p.x*=sign;
                a.body.Add(AuthorPoint(p),Hex("DBE6F5"),foot);
            }
            for(int i=0;i<8;i++)a.body.Face(shine,shine+1+i,shine+1+(i+1)%8);
            if(sign<0)ReverseFaces(a.body,shineTriangles);
            start=a.body.p.Count;triangles=a.body.tri.Count;
            for(int row=0;row<=4;row++)for(int i=0;i<=16;i++) {
                Vector3 p=FishModelGuides.SockRing(i/16f,row);p.x*=sign;
                a.body.Add(AuthorPoint(p),White,knee);
            }
            Grid(a.body,start,4,16);if(sign<0)ReverseFaces(a.body,triangles);
            for(int row=0;row<=4;row++)a.body.smoothNormalPairs.Add(new Vector2Int(start+row*17,start+row*17+16));
            // A small flared cuff uses the independently painted scallop tile.
            start=a.body.p.Count;triangles=a.body.tri.Count;
            for(int row=0;row<=2;row++)for(int i=0;i<=24;i++) {
                float t=i/24f,r=row/2f,angle=t*Mathf.PI*2;
                Vector3 p=FishModelGuides.SockRing(t,1),center=V(.069f,p.y,-.019f);
                p=center+(p-center)*(1+.18f*r);p.y=.164f-.019f*r+.0015f*Mathf.Cos(angle*6)*r;p.x*=sign;
                a.body.Add(AuthorPoint(p),White,knee,LaceUV(3,t,r));
            }
            Grid(a.body,start,2,24);if(sign<0)ReverseFaces(a.body,triangles);
            start=a.body.p.Count;triangles=a.body.tri.Count;
            for(int row=0;row<=1;row++)for(int i=0;i<=12;i++) {
                Vector3 p=FishModelGuides.FootStrap(i/12f);p.z+=(row-.5f)*.011f;p.y-=(row-.5f)*.006f;p.x*=sign;
                a.body.Add(AuthorPoint(p),Hex("354C82"),foot);
            }
            Grid(a.body,start,1,12);if(sign<0)ReverseFaces(a.body,triangles);
            Vector3 c=FishModelGuides.FootStrap(.96f)+V(0,.002f,0);
            Vector3 slope=FishModelGuides.FootStrap(1)-FishModelGuides.FootStrap(.84f);float rise=slope.y/slope.x;
            // Wide gold frame, four sides and a single tongue remain legible
            // at the pet's desktop size without a high-sided tube.
            Vector3[] corners={V(-.008f,0,-.006f),V(.008f,0,-.006f),V(.008f,0,.006f),V(-.008f,0,.006f)};
            start=a.body.p.Count;triangles=a.body.tri.Count;
            foreach(float scale in new[]{1f,.69f})foreach(var q in corners) {
                Vector3 p=c+q*scale;p.y+=(q.x*rise-q.z*.55f)*scale;p.x*=sign;a.body.Add(AuthorPoint(p),Gold,foot);
            }
            for(int i=0;i<4;i++){int next=(i+1)%4;a.body.Face(start+i,start+next,start+4+i);a.body.Face(start+next,start+4+next,start+4+i);}
            if(sign>0)ReverseFaces(a.body,triangles);
            Vector3 tongue=c+V(-.003f,.0006f,0);tongue.x*=sign;
            a.Tube(a.body,new[]{AuthorPoint(tongue),AuthorPoint(tongue+V(sign*.008f,0,0))},.0008f,Gold,foot,4);
        }

        static void BuildAccessories(Author a,int head,int chest,int hips)
        {
            // A single cloth sheet uses a sparse measured radial field. The
            // binding and irregular pleats are painted into its own cutout tile.
            int headbandStart=a.body.p.Count;
            float[] radialRows={0,.14f,.50f,1};
            for(int row=0;row<radialRows.Length;row++)for(int i=0;i<=64;i++) {
                float u=i/64f,t=radialRows[row];
                a.body.Add(AuthorPoint(FishHeadbandGuides.Point(u,t)),White,head,LaceUV(0,u,t));
            }
            Grid(a.body,headbandStart,radialRows.Length-1,64);
            foreach(int s in new[]{-1,1}) {
                HeadFin(a,s,head);
                a.body.region=s>0?3:4;
                Bow(a,V(s*.252f,.835f,.045f),head,.050f,Hex("47A7D3"),Quaternion.Euler(0,s*48,s*-8));a.body.region=0;
            }
            ChestRibbon(a,chest);
            int tail=a.Bone("J_Opt_C_FoxTail1_01",hips,V(0,.255f,-.083f));
            int tailStart=a.body.p.Count,tailTriangles=a.body.tri.Count;
            a.Sweep(a.body,Curve(new[]{V(.02f,.26f,-.095f),V(.19f,.18f,-.12f),V(.22f,.10f,-.12f),V(.31f,.13f,-.065f)},20),
                t=>Mathf.Lerp(.030f,.012f,t),t=>Mathf.Lerp(.020f,.009f,t),t=>Color.Lerp(Hair,Hex("689AAF"),t),tail,10);
            // Two curved lobes leave an open notch instead of a flat arrowhead.
            FinBlade(a,
                new[]{V(.302f,.132f,-.064f),V(.303f,.179f,-.060f),V(.347f,.217f,-.025f),V(.369f,.222f,-.018f)},
                new[]{V(.302f,.132f,-.064f),V(.355f,.138f,-.035f),V(.341f,.190f,-.020f),V(.369f,.222f,-.018f)},
                tail,Hex("4A6E9C"),Hair,false,.005f);
            FinBlade(a,
                new[]{V(.302f,.132f,-.064f),V(.351f,.144f,-.040f),V(.378f,.169f,-.015f),V(.408f,.163f,-.006f)},
                new[]{V(.302f,.132f,-.064f),V(.331f,.103f,-.060f),V(.386f,.118f,-.028f),V(.408f,.163f,-.006f)},
                tail,Hex("4A6E9C"),Hair,false,.005f);
            for(int i=tailStart;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];p.x=-p.x;a.body.p[i]=p;
            }
            ReverseFaces(a.body,tailTriangles);
        }
        static void HeadFin(Author a,int sign,int bone) {
            // Separate open outer and inner shells retain the pale lining seen
            // through the lower front opening, instead of painting a solid oval.
            foreach(bool inner in new[]{false,true}) {
                // Spend transverse samples on the visible rolled lip. The final
                // index-only reduction still enforces the complete 20k budget.
                int rows=FishModelGuides.FinRows(sign,inner),cols=inner?8:12;
                int start=a.body.p.Count,triangles=a.body.tri.Count;
                for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++) {
                    float t=row/(float)rows,v=col/(float)cols;
                    Vector3 p=FishModelGuides.FinShell(sign,inner,t,row==rows?0:v);
                    // A narrow painted fold follows the front lip, not the whole oval.
                    float sheen=Mathf.Exp(-Mathf.Pow((t-.53f)/.36f,2)-Mathf.Pow((v-.20f)/.13f,2));
                    // The lining is lavender cloth with a pale lower return,
                    // not a white center band across the entire open shell.
                    Color color=inner?Color.Lerp(Hex("6976AC"),Hex("CED6EA"),.80f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,.95f,v))):
                        Color.Lerp(Hex("333F88"),Hex("B7BFEB"),sheen*.90f);
                    Vector3 author=AuthorPoint(p);
                    int vertex=a.body.Add(author,color,bone,LaceUV(3,t/3,Mathf.Max(0,(v-.95f)/.05f)));
                    // Evaluate the smooth guide tangent before polygon reduction.
                    // The collapsed tip keeps its mesh-derived normal.
                    if(row<rows) {
                        Vector3 along=FishModelGuides.FinShell(sign,inner,Mathf.Min(1,t+.001f),v)-FishModelGuides.FinShell(sign,inner,Mathf.Max(0,t-.001f),v);
                        Vector3 across=FishModelGuides.FinShell(sign,inner,t,Mathf.Min(1,v+.001f))-FishModelGuides.FinShell(sign,inner,t,Mathf.Max(0,v-.001f));
                        Vector3 normal=Vector3.Cross(across.normalized,along.normalized).normalized;
                        if((sign>0)!=inner)normal=-normal;
                        if(normal.sqrMagnitude>.5f) {
                            float derivative;FitHeight(author.y,out derivative);normal.y*=derivative;
                            a.body.normalBlends[vertex]=new Vector4(normal.x,normal.y,normal.z,.90f);
                        }
                    }
                }
                Grid(a.body,start,rows,cols);
                if((sign>0)!=inner)ReverseFaces(a.body,triangles);
            }
        }
        static void FinBlade(Author a,Vector3[] leading,Vector3[] trailing,int bone,
            Color rootColor,Color tipColor,bool paleInset,float thickness,Vector3? surfaceNormal=null)
        {
            Vector3 normal=(surfaceNormal??Vector3.forward).normalized;
            int rows=paleInset?9:8,cols=paleInset?4:3,mid=rows/2;
            var edgeA=Curve(leading,rows);var edgeB=Curve(trailing,rows);
            if(paleInset)for(int i=0;i<=rows;i++) {
                float t=i/(float)rows;
                edgeB[i].y-=.003f*Mathf.Sin(t*Mathf.PI)*(.5f+.5f*Mathf.Cos(t*Mathf.PI*6));
            }
            bool forward=Vector3.Dot(Vector3.Cross(edgeB[mid]-edgeA[mid],edgeA[mid+1]-edgeA[mid-1]),normal)>0;
            foreach(int side in new[]{1,-1}) {
                int start=a.body.p.Count,triangles=a.body.tri.Count;
                for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++) {
                    float t=row/(float)rows,v=col/(float)cols;
                    float lens=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)*Mathf.Sin(v*Mathf.PI)),.7f);
                    Vector3 p=Vector3.Lerp(edgeA[row],edgeB[row],v)+normal*(side*thickness*lens);
                    Color color=Color.Lerp(rootColor,tipColor,Mathf.SmoothStep(0,1,t));
                    if(paleInset&&side>0) {
                        float inset=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.42f,t))*
                            (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.73f,.94f,t)))*
                            Mathf.SmoothStep(0,1,Mathf.InverseLerp(.52f,.70f,v))*
                            (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,1,v)));
                        color=Color.Lerp(color,White,inset);
                    }
                    a.body.Add(p,color,bone);
                }
                Grid(a.body,start,rows,cols);
                if((side>0)!=forward)ReverseFaces(a.body,triangles);
            }
        }
        static void ReverseFaces(Geo geo,int start) {
            for(int i=start;i<geo.tri.Count;i+=3) {
                int temp=geo.tri[i+1];geo.tri[i+1]=geo.tri[i+2];geo.tri[i+2]=temp;
            }
        }
        static void Fan(Author a,Vector3[] points,Color color,int bone) {
            int start=a.body.p.Count;
            foreach(var p in points)a.body.Add(p,color,bone);
            for(int i=1;i<points.Length-1;i++)a.body.Face(start,start+i,start+i+1);
        }
        static void Bow(Author a,Vector3 center,int bone,float size,Color color,Quaternion? orientation=null) {
            int first=a.body.p.Count;
            foreach(int s in new[]{-1,1}) {
                bool gathered=orientation.HasValue;
                int rows=gathered?6:8,cols=4;
                foreach(int surface in gathered?new[]{1,-1}:new[]{1}) {
                    int start=a.body.p.Count,triangles=a.body.tri.Count;
                    for(int i=0;i<=rows;i++)for(int j=0;j<=cols;j++) {
                        float u=i/(float)rows,v=j/(float)cols*2-1;
                        float height=size*(.08f+.38f*Mathf.Sin(u*Mathf.PI*.78f));
                        float z=size*(.12f*Mathf.Sin(u*Mathf.PI)+.08f*(1-v*v));
                        float x=s*size*u;
                        Color shaded=Color.Lerp(color*.72f,color*1.17f,(v+1)*.5f);
                        if(gathered) {
                            float swell=Mathf.Sin(u*Mathf.PI)*(1-v*v);
                            float fold=Mathf.Exp(-Mathf.Pow((Mathf.Abs(v)-.38f)/.18f,2));
                            x*=1-.14f*Mathf.Pow(Mathf.Abs(v),3);
                            z=size*swell*(surface>0?.25f:-.10f);
                            z-=surface*size*.045f*fold*Mathf.Sin(u*Mathf.PI);
                            shaded*=1-.15f*fold*Mathf.Sin(u*Mathf.PI);
                        }
                        shaded.a=1;
                        a.body.Add(center+V(x,v*height,z),shaded,bone);
                    }
                    Grid(a.body,start,rows,cols);
                    if(gathered&&((surface>0)==(s>0)))ReverseFaces(a.body,triangles);
                }
                Fan(a,new[]{center+V(s*size*.05f,-size*.08f,0),center+V(s*size*.62f,-size*.83f,.001f),
                    center+V(s*size*.32f,-size*.72f,.003f),center+V(s*size*.12f,-size*.88f,.004f)},color,bone);
            }
            a.Sphere(a.body,center+V(0,0,.002f),V(size*.17f,size*.20f,size*.13f),color,bone,12,8);
            if(orientation.HasValue)for(int i=first;i<a.body.p.Count;i++)
                a.body.p[i]=center+orientation.Value*(a.body.p[i]-center);
        }
        static void GoldBow(Author a,Vector3 c,int bone,float size) {
            foreach(int s in new[]{-1,1}) {
                SkirtStroke(a,Curve(new[]{c,c+V(s*size*.8f,size*.02f,0),
                    c+V(s*size*1.5f,size*.65f,0),c+V(s*size*1.2f,size*.88f,0),
                    c+V(s*size*.85f,size*1.12f,0),c+V(s*size*.28f,size*.38f,0),c},24),.0026f,bone);
                SkirtStroke(a,Curve(new[]{c,c+V(s*size*.12f,-size*.25f,0),
                    c+V(s*size*.22f,-size*.50f,0),c+V(s*size*.60f,-size*.72f,0)},10),.0024f,bone);
            }
            var knot=Enumerable.Range(0,12).Select(i=>{
                float angle=-i/12f*Mathf.PI*2;
                return c+V(Mathf.Cos(angle)*.0038f,Mathf.Sin(angle)*.0043f,0);
            }).ToArray();
            SkirtPatch(a,c,knot,bone);
        }
        static Vector3 SkirtSurface(float theta,float y) {
            float t=Mathf.InverseLerp(.173f,.375f,y),rx=Mathf.Lerp(.223f,.113f,t),rz=Mathf.Lerp(.137f,.077f,t);
            float pleat=1+.045f*Mathf.Cos(theta*12)*(1-t);
            return V(Mathf.Sin(theta)*rx*pleat,y,Mathf.Cos(theta)*rz*pleat);
        }
        static Vector3 OnSkirt(Vector3 p) {
            // Project onto the same angular segments as the cloth. Folds near
            // the sides can overlap in x, so use the frontmost intersection.
            float z=-1,nearest=float.MaxValue;Vector3 fallback=Vector3.zero;
            Vector3 previous=SkirtSurface(-Mathf.PI*.5f,p.y);
            for(int i=1;i<=36;i++) {
                Vector3 next=SkirtSurface(-Mathf.PI*.5f+i*Mathf.PI/36,p.y);
                float delta=next.x-previous.x;
                if(Mathf.Abs(delta)>.000001f) {
                    float u=(p.x-previous.x)/delta;
                    if(u>=0&&u<=1)z=Mathf.Max(z,Mathf.Lerp(previous.z,next.z,u));
                }
                foreach(var point in new[]{previous,next})
                    if(Mathf.Abs(point.x-p.x)<nearest) { nearest=Mathf.Abs(point.x-p.x);fallback=point; }
                previous=next;
            }
            if(z<0) {p.x=fallback.x;z=fallback.z;}
            p.z=z+.003f;
            return p;
        }
        static void SkirtStroke(Author a,Vector3[] path,float radius,int bone) {
            int start=a.body.p.Count;const int cols=2;
            for(int i=0;i<path.Length;i++)for(int j=0;j<=cols;j++) {
                Vector3 tangent=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(i-1,0)]).normalized;
                float u=j/(float)cols;
                Vector3 p=OnSkirt(path[i]+V(tangent.y,-tangent.x,0)*radius*(u*2-1));
                p.z+=Mathf.Sin(u*Mathf.PI)*.0005f;
                Color color=Gold*(j==1?1.06f:.94f);color.a=1;
                a.body.Add(p,color,bone);
            }
            Grid(a.body,start,path.Length-1,cols);
        }
        static void SkirtPatch(Author a,Vector3 center,Vector3[] edge,int bone) {
            int start=a.body.p.Count;
            a.body.Add(OnSkirt(center)+Vector3.forward*.0005f,Gold,bone);
            foreach(var p in edge)a.body.Add(OnSkirt(p),Gold,bone);
            for(int i=0;i<edge.Length;i++) {
                int next=(i+1)%edge.Length;
                if(Vector3.Cross(edge[i]-center,edge[next]-center).z>0)
                    a.body.Face(start,start+1+i,start+1+next);
                else a.body.Face(start,start+1+next,start+1+i);
            }
        }
        static void SkirtLeaf(Author a,Vector3 root,Vector3 tip,float width,int bone) {
            Vector3 center=(root+tip)*.5f,axis=(tip-root).normalized,side=V(axis.y,-axis.x,0);
            var edge=new List<Vector3>();
            for(int i=0;i<=6;i++) {
                float t=i/6f;
                edge.Add(Vector3.Lerp(root,tip,t)+side*(Mathf.Sin(t*Mathf.PI)*width));
            }
            for(int i=5;i>0;i--) {
                float t=i/6f;
                edge.Add(Vector3.Lerp(root,tip,t)-side*(Mathf.Sin(t*Mathf.PI)*width*.75f));
            }
            SkirtPatch(a,center,edge.ToArray(),bone);
        }
        static void Star(Author a,Vector3 c,float size,int bone) {
            SkirtPatch(a,c,new[]{c+V(0,size,0),c+V(size*.32f,size*.30f,0),
                c+V(size*.83f,0,0),c+V(size*.30f,-size*.28f,0),c+V(0,-size,0),
                c+V(-size*.30f,-size*.28f,0),c+V(-size*.83f,0,0),c+V(-size*.32f,size*.30f,0)},bone);
        }
    }
}
