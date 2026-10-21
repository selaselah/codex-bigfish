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
    // This generator does not load, sample, retopologize or trace a source 3D asset.
    public static class FishRebuiltModel
    {
        const string Folder = "Assets/DesktopFish/Rebuilt";
        static readonly Color Skin = Hex("FFF0E9"), Hair = Hex("304C91"), Tip = Hex("58B2D5"),
            Navy = Hex("252E5D"), White = Hex("FFFCF3"), Gold = Hex("E2C47D"), Ink = Hex("233150");
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color c); return c; }
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
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
            public readonly List<Color> colors = new List<Color>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<BoneWeight> weights = new List<BoneWeight>();
            public readonly List<int> tri = new List<int>();
            public readonly Dictionary<int,Vector4> normalBlends = new Dictionary<int,Vector4>();
            public readonly List<Vector2Int> smoothNormalPairs = new List<Vector2Int>();
            public int Add(Vector3 v, Color color, int bone, Vector2? texture = null, int secondaryBone = -1, float secondaryWeight = 0) {
                p.Add(v); colors.Add(color); uv.Add(texture ?? new Vector2(.995f,.995f));
                weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1-secondaryWeight,
                    boneIndex1 = secondaryBone < 0 ? bone : secondaryBone, weight1 = secondaryWeight });
                return p.Count - 1;
            }
            public void Face(int a, int b, int c) { tri.Add(a); tri.Add(b); tri.Add(c); }
            public Mesh Build(string name, Transform root, Transform[] bones) {
                if (p.Any(v => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                    float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)))
                    throw new InvalidOperationException("Non-finite authored mesh vertex: " + name);
                var mesh = new Mesh { name = name, indexFormat = p.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(p); mesh.SetColors(colors); mesh.SetUVs(0,uv); mesh.SetTriangles(tri, 0);
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
            readonly List<Tuple<int, Vector3, string>> features = new List<Tuple<int, Vector3, string>>();
            readonly Dictionary<int,Vector3> closedLashes = new Dictionary<int,Vector3>();
            readonly Dictionary<int,Vector3> smilingLashes = new Dictionary<int,Vector3>();
            public int Bone(string name, int parent, Vector3 position) {
                var bone = new GameObject(name).transform;
                bone.SetParent(parent < 0 ? root.transform : bones[parent], false); bone.position = position;
                ids[name] = bones.Count; bones.Add(bone); return bones.Count - 1;
            }
            public void Sphere(Geo g, Vector3 c, Vector3 r, Color color, int bone, int n = 16, int rows = 12) {
                int start = g.p.Count;
                for (int y = 0; y <= rows; y++) for (int x = 0; x <= n; x++) {
                    float a = x * Mathf.PI * 2 / n, b = y * Mathf.PI / rows;
                    g.Add(c + Vector3.Scale(r, V(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Cos(b), Mathf.Sin(b) * Mathf.Sin(a))), color, bone);
                }
                Grid(g, start, rows, n);
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
                float cheekInset = -1, float frontLift = 0, float wrapDepth = 0, Vector3[] paintGuide = null) {
                int start=body.p.Count; int rows=samples>0?samples:controls.Length>4?36:24;
                var path=Curve(controls,rows);
                var boundaryA=edgeA==null?null:Curve(edgeA,rows);
                var boundaryB=edgeB==null?null:Curve(edgeB,rows);
                Vector3 facing=outward??Vector3.forward;
                bool frontalSection=bone==ids["J_Bip_C_Head"]&&!outward.HasValue&&boundaryA!=null;
                var frontalDepth=frontalSection?new float[(rows+1)*(sides+1)]:null;
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
                    Vector3 tangent=(path[Mathf.Min(row+1,rows)]-path[Mathf.Max(row-1,0)]).normalized;
                    // Independent locks can turn their broad surface toward the
                    // side without invoking the radial scalp-root construction.
                    float turn=sideTurn*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.38f,t));
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
                        if(!frontalSection)p+=thicknessAxis*facet*thickness*rootNeck;
                        // Keep attachment weights and crown paint on their
                        // original guide while reshaping the visible cross-section.
                        float attachmentHeight=p.y;
                        if(outward.HasValue&&attachmentHeight>.72f) {
                            float height=Mathf.Clamp((path[row].y-.765f)/.215f,-1,1);
                            float lift=.0045f+front*.002f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.18f,t));
                            attachmentHeight=Mathf.Lerp(attachmentHeight,.765f+(.215f+lift)*height,
                                Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.86f,attachmentHeight)));
                        }
                        if(outward.HasValue&&path[row].y>.550f) {
                            // Neighboring upper locks share angular section edges.
                            // Curving each complete section avoids nine flat plates
                            // while retaining the existing longitudinal guides.
                            float centerAngle=Mathf.Atan2(facing.x,facing.z);
                            float rx=Mathf.Abs(Mathf.Sin(centerAngle))>.1f?path[row].x/Mathf.Sin(centerAngle):.244f;
                            float rz=Mathf.Abs(Mathf.Cos(centerAngle))>.1f?path[row].z/Mathf.Cos(centerAngle):.192f;
                            float theta=centerAngle-u*(Mathf.PI*2-2*1.12f)/16;
                            Vector3 rounded=V(Mathf.Sin(theta)*(rx+front*.0025f),path[row].y,
                                Mathf.Cos(theta)*(rz+front*.0025f));
                            p=Vector3.Lerp(p,rounded,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.550f,.680f,path[row].y)));
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
                        if(frontalSection)frontalDepth[row*(sides+1)+j]=facet*thickness*rootNeck;
                        float ridge=.94f+.06f*Mathf.Pow(Mathf.Max(0,front),.6f)-.04f*Mathf.Pow(Mathf.Abs(u),6);
                        float crown=outward.HasValue?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.79f,.90f,attachmentHeight)):0;
                        ridge=Mathf.Lerp(ridge,.97f,crown*.85f);
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
                        if(crown>0&&front>=-.001f) {
                            // A shared scalp normal joins the exposed root faces;
                            // the closed inner surface and lower locks retain their own normals.
                            Vector3 normal=V(p.x/(.2365f*.2365f),(p.y-.765f)/(.2195f*.2195f),
                                p.z/(.1865f*.1865f)).normalized;
                            body.normalBlends[vertex]=new Vector4(normal.x,normal.y,normal.z,crown);
                        }
                    }
                }
                if(frontalSection) {
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
            }
            public void HairMantle(int head) {
                const int rows=16,cols=40;
                for(int layer=0;layer<1;layer++) {
                    int layerStart=body.p.Count;
                    for(int row=0;row<=rows;row++)for(int i=0;i<=cols;i++) {
                        float t=row/(float)rows,theta=Mathf.Lerp(1.10f,Mathf.PI*2-1.10f,i/(float)cols);
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
                        Color color=Color.Lerp(Hair,Hex("3D619C"),t*.35f);
                        color*=1-.18f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.4f,t))*(.5f-.5f*fold);
                        color=Color.Lerp(color,Hex("4D94B3"),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,.99f,t)));color.a=1;
                        body.Add(SweepHairSection(p,HairSweepAtAngle(theta)*1.25f),color,head);
                    }
                    Grid(body,layerStart,rows,cols);
                }
            }
            public void Disk(Geo g, Vector2 center, Vector2 size, Color color, int bone,
                float offset, string feature = "", int steps = 40) {
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
            public void Iris(float x,int bone) {
                int start=face.p.Count; const int rows=8,cols=32;
                // One shared center and closed rings avoid repeated center/seam
                // vertices. Thirty-two sides retain the desktop eye contour.
                for(int row=0;row<=rows;row++)for(int j=0;j<(row==0?1:cols);j++) {
                    float r=row/(float)rows,t=j/(float)cols*Mathf.PI*2;
                    float px=x+Mathf.Cos(t)*.0313f*r,py=.6905f+Mathf.Sin(t)*.0325f*r;
                    // Broad curved color fields read at pet size. Keep a dark
                    // upper iris, blue middle and pale cyan lower crescent.
                    float u=Mathf.Cos(t)*r,v=Mathf.Sin(t)*r,arc=v+.34f*u*u;
                    Color color=Hex("2B396C");
                    color=Color.Lerp(color,Hex("3A5393"),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.55f,arc)));
                    color=Color.Lerp(color,Hex("659AF4"),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.02f,.18f,arc)));
                    color=Color.Lerp(color,Hex("7BD9FF"),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.47f,-.27f,arc)));
                    color=Color.Lerp(color,Hex("243162"),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,1,r))*.85f);
                    int index=face.Add(FacePoint(px,py,.0027f),color,bone);
                    features.Add(Tuple.Create(index,V(x,.6905f,.0027f),"iris"));
                }
                for(int j=0;j<cols;j++) {
                    int next=(j+1)%cols;
                    face.Face(start,start+1+j,start+1+next);
                    for(int row=1;row<rows;row++) {
                        int inner=start+1+(row-1)*cols,outer=inner+cols;
                        face.Face(inner+j,outer+j,outer+next);
                        face.Face(inner+j,outer+next,inner+next);
                    }
                }
            }
            public void IrisReflection(float x,int bone) {
                int start=face.p.Count;const int rows=2,cols=24;
                for(int row=0;row<=rows;row++)for(int i=0;i<=cols;i++) {
                    float u=i/(float)cols,r=row/(float)rows;
                    float width=Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(2*u-1,2)));
                    // A rounded lower light pool keeps the small eye readable;
                    // the former bent ribbon produced two pointed bright horns.
                    float px=x+(2*u-1)*.0165f;
                    float py=.6905f-.026f+Mathf.Lerp(-.004f,.007f,r)*width;
                    Color color=Color.Lerp(Hex("83DCFA"),Hex("F2FFFF"),width*(1-.65f*r));
                    int index=face.Add(FacePoint(px,py,.0042f),color,bone);
                    features.Add(Tuple.Create(index,V(x,.6905f,.0042f),"shine"));
                }
                Grid(face,start,rows,cols);
            }
            public void LowerEyeGlint(float x,int bone) {
                const int sides=16;
                int start=face.p.Count;
                Vector3 center=V(x-.022f,.6695f,.0045f);
                // One center plus a ring is enough for this tiny catchlight.
                // Register every vertex so both eye-closing morphs include it.
                for(int i=0;i<=sides;i++) {
                    float angle=(i-1)*Mathf.PI*2/sides,r=i==0?0:.0024f;
                    int index=face.Add(FacePoint(center.x+Mathf.Cos(angle)*r,
                        center.y+Mathf.Sin(angle)*r,center.z),Color.white,bone);
                    features.Add(Tuple.Create(index,center,"shine"));
                }
                for(int i=0;i<sides;i++)face.Face(start,start+1+i,start+1+(i+1)%sides);
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
                                } else target = V(c.x, .700f, FacePoint(c.x, .700f, -.025f).z);
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
                    mesh=CompactMesh(mesh);
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
                root.AddComponent<FishRigProfile>();
                Directory.CreateDirectory("work");
                File.WriteAllText("work/rebuilt-geometry.json",JsonUtility.ToJson(new ModelDump {
                    bones=bones.Select(b=>new BoneDump {name=b.name,parent=bones.IndexOf(b.parent),position=b.position}).ToArray(),
                    meshes=exports.ToArray()
                }));
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/BigFish.prefab");
                File.WriteAllText(Folder + "/provenance.json", "{\n  \"generator\": \"FishRebuiltModel.cs\",\n  \"source_3d_assets_loaded\": false,\n  \"body_vertices\": " + exports[0].vertices.Length + ",\n  \"face_vertices\": " + exports[1].vertices.Length + ",\n  \"triangles\": " + exports.Sum(m=>m.triangles.Length) / 3 + ",\n  \"bones\": " + bones.Count + "\n}\n");
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
            // Retain the rear jaw/neck attachment while moving the chin. Moving
            // the whole lower ring forward would leave the neck outside it.
            float forward=radius<.001f?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.5f,.5f,p.z/radius));
            p.z+=JawForward(p.y)*forward;p.x*=FaceWidthScale(p.y);return p;
        }
        static Vector3 FacePoint(float x, float y, float offset) {
            // Facial marks keep their authored XY placement while following
            // the narrowed skin surface, including all expression targets.
            float sourceX=x/FaceWidthScale(y);
            float depth = Mathf.Sqrt(Mathf.Max(.05f, 1 - sourceX * sourceX / (.214f * .214f) - (y - .773f) * (y - .773f) / (.187f * .187f)));
            var p=ShapeHeadPoint(V(sourceX,y,.166f*depth));p.x=x;p.z+=offset;return p;
        }
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
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color[size*size];
            float[] brushCenters={.10f,.46f,.85f};
            float[] brushStarts={.59f,.71f,.64f};
            float[] brushWidths={.29f,.23f,.27f};
            float[] reflectionCenters={.37f,.61f,.44f},reflectionHeights={.32f,.39f,.35f},reflectionBends={.18f,-.15f,.10f};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float ax=x/(float)(size-1),ay=y/(float)(size-1);
                int strip=Mathf.Min(3,(int)(ax*4));bool cyan=strip==3;
                float u=Mathf.Clamp01((ax-strip*.25f-.012f)/.226f);
                int longPaint=cyan?Mathf.Min(2,(int)((ax-.75f)*12)):0;
                if(cyan)u=Mathf.Clamp01((ax-.75f-longPaint/12f-.004f)/.075f);
                float t=Mathf.Clamp01((ay-.02f)/.94f);
                float edge=Mathf.Pow(Mathf.Abs(u*2-1),8);
                Color baseColor=Color.Lerp(Hex("2B4588"),Hex("4165A1"),Mathf.Sin(t*Mathf.PI)*.50f);
                if(!cyan)baseColor=Color.Lerp(Hex("1E3888"),Hex("486FB3"),Mathf.Sin(t*Mathf.PI)*.65f);
                float ends=cyan?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.52f,.95f,t))*.90f:
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
                    ends=Mathf.Max(ends,brush*.90f);
                }
                baseColor=Color.Lerp(baseColor,cyan?Hex("59BED8"):Hex("8C8CB7"),ends);
                if(cyan) {
                    // A few blue tongues extend into the cyan, separating broad
                    // painted masses without outlining every individual strand.
                    float blue=PaintBrush(u,1-t,.24f+longPaint*.065f,.045f,.072f,-.035f);
                    blue=Mathf.Max(blue,PaintBrush(u,1-t,.72f-longPaint*.055f,.10f,.047f,.025f));
                    blue*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.59f,.75f,t));
                    baseColor=Color.Lerp(baseColor,Hex("385B99"),blue*.70f);
                }
                if(cyan) {
                    // Wide curved reflection bands describe the joined upper
                    // mass; the lower brush shapes still separate individual tips.
                    float center=reflectionCenters[longPaint];
                    float height=reflectionHeights[longPaint];
                    float bend=reflectionBends[longPaint];
                    float du=(u-center-bend*Mathf.Sin((t-.14f)*Mathf.PI*2))/.85f;
                    float dt=(t-height-.035f*Mathf.Cos((u-.5f)*Mathf.PI))/.16f;
                    float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,.18f,t))*
                        (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.58f,t)));
                    float reflection=Mathf.Exp(-du*du*1.4f-dt*dt)*envelope;
                    baseColor=Color.Lerp(baseColor,Hex("668CC5"),reflection*.24f);
                }
                // Slightly wandering broad streaks and finer tip strokes retain
                // strand detail without a uniform comb of vertical stripes.
                float strand=Mathf.Sin(u*43+t*3+Mathf.Sin(t*7+u*5))*.006f+
                    Mathf.Sin(u*19-t*2)*.012f;
                float edgeContrast=cyan?Mathf.Lerp(.02f,.18f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.58f,t))):.06f;
                baseColor*=1-edge*edgeContrast+strand;
                if(!cyan)baseColor*=1-.15f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.93f,1,Mathf.Abs(u*2-1)));
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
                pixels[y*size+x]=(ax>.98f&&ay>.98f)?Color.white:baseColor;
            }
            texture.SetPixels(pixels);texture.Apply();
            File.WriteAllBytes(Folder+"/HairPaint.png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
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
            a.Sphere(a.body, V(0, .773f, 0), V(.214f, .187f, .166f), Skin, head, 48, 28);
            for(int row=0;row<=28;row++)a.body.smoothNormalPairs.Add(new Vector2Int(row*49,row*49+48));
            for(int i=0;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];
                // Exact coincident pole positions let cleanup remove the
                // collapsed fan faces after the jaw stretches forward.
                if(i>=28*49)p=V(0,.773f-.187f,0);
                p=ShapeHeadPoint(p);
                float blush=Mathf.Exp(-Mathf.Pow((Mathf.Abs(p.x)-.126f)/.048f,2)
                    -Mathf.Pow((p.y-.655f)/.029f,2))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.12f,p.z));
                a.body.colors[i]=Color.Lerp(Hex("FFF2E8"),Hex("F2B8AB"),blush*.88f);
                a.body.p[i]=p;
            }
            a.Sphere(a.body, V(0, .555f, 0), V(.028f, .035f, .029f), Skin, neck);
            a.Sphere(a.body, V(0, .460f, 0), V(.120f, .108f, .080f), Navy, chest);
            BuildHair(a, head);
            BuildFace(a, head);
            BuildDress(a, hips, chest);
            BuildLimbs(a, hips, chest);
            BuildAccessories(a, head, chest, hips);
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
        public static void RenderIdleAndBuild() { RenderIdle(); Build(); }
        public static void RenderIdleFace() { RenderAngles(false,true); }
        public static void RenderIdleFaceAndBuild() { RenderIdleFace(); Build(); }
        public static void RenderIdleSkinningAndBuild() {
            FishBuild.UseRebuilt=true;FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion=new FishMotion(companion.model){randomActions=false};
            FishBuild.ValidateMotion(companion,motion,companion.model.GetComponentsInChildren<SkinnedMeshRenderer>());
            RenderIdle();Build();
        }
        public static void RenderAngles() { RenderAngles(true); }
        static void RenderAngles(bool includeMotion,bool includeExpressions=false) {
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
            if(!includeMotion) {
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
            float oldSize=camera.orthographicSize;
            var oldTarget=camera.targetTexture;
            const int size=384,columns=8;
            var target=new RenderTexture(size,size,24){antiAliasing=4};
            var frame=new Texture2D(size,size,TextureFormat.RGBA32,false);
            camera.targetTexture=target;camera.orthographicSize=.27f;
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
            camera.targetTexture=oldTarget;RenderTexture.active=null;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.orthographicSize=oldSize;
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

        static void BuildFace(Author a, int head)
        {
            foreach (int sign in new[] { -1, 1 }) {
                float x = sign * .085f;
                a.Disk(a.face, new Vector2(x, .696f), new Vector2(.046f, .0347f), Hex("FFFCFA"), head, .0015f, "eye");
                a.Iris(x,head);
                a.Disk(a.face, new Vector2(x, .6905f), new Vector2(.0095f, .0125f), Hex("283464"), head, .0036f, "pupil",32);
                a.Disk(a.face, new Vector2(x + .0101f, .7097f), new Vector2(.0074f, .0074f), Color.white, head, .0045f, "shine",24);
                a.Disk(a.face, new Vector2(x - .0048f, .7123f), new Vector2(.0028f, .0028f), Color.white, head, .0045f, "shine",16);
                a.IrisReflection(x,head);
                a.LowerEyeGlint(x,head);
                var lash = new Vector2[25];
                for (int i = 0; i < lash.Length; i++) {
                    float t = Mathf.Lerp(.90f,.13f,i/24f)*Mathf.PI;
                    lash[i] = new Vector2(x + sign*Mathf.Cos(t)*.047f,
                        .695f+.0315f*Mathf.Pow(Mathf.Sin(t),.50f));
                }
                a.FaceLine(lash, .0080f, Hex("211D27"), head, "lash",x,.45f,1);
                var wing=Enumerable.Range(0,7).Select(i=>{
                    float t=i/6f;
                    return Vector2.Lerp(lash[24],new Vector2(x+sign*.060f,.721f),t)
                        +new Vector2(0,-.003f*Mathf.Sin(t*Mathf.PI));
                }).ToArray();
                a.FaceLine(wing,.0080f,Hex("211D27"),head,"lash",x,1,.12f);
                a.FaceLine(new[] { new Vector2(x - .031f, .742f), new Vector2(x, .748f), new Vector2(x + .029f, .742f) }, .0016f, Hex("83666B"), head,"brow");
            }
            a.Mouth(head);
            a.Disk(a.face,new Vector2(0,.652f),new Vector2(.0015f,.0018f),Hex("E5B5A6"),head,.0018f,"",16);
        }

        static void BuildHair(Author a, int head)
        {
            int rearStart=a.body.p.Count;
            a.HairMantle(head);
            int start=a.body.p.Count; const int rings=14,sides=40;
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++) {
                float theta=x*Mathf.PI*2/sides,front=(Mathf.Cos(theta)+1)*.5f;
                float phi=y/(float)rings*Mathf.Lerp(2.05f,1.10f,front);
                a.body.Add(V(Mathf.Sin(theta)*Mathf.Sin(phi)*.232f,
                    .765f+Mathf.Cos(phi)*.215f,Mathf.Cos(theta)*Mathf.Sin(phi)*.182f),Hair,head);
            }
            Grid(a.body,start,rings,sides);
            for(int i=0;i<5;i++) {
                float theta=Mathf.Lerp(-1.12f,-Mathf.PI*2+1.12f,i/4f);
                a.Bone("J_Sec_Hair"+(i+1)+"_01",head,V(Mathf.Sin(theta)*.16f,.90f,Mathf.Cos(theta)*.13f));
            }
            // Radial locks wrap the back of the head. Each has its own transverse
            // direction, so it retains width in a side view and curls at the hem.
            int radialStart=a.body.p.Count;
            for(int i=0;i<9;i++) {
                float theta=Mathf.Lerp(-1.12f,-Mathf.PI*2+1.12f,i/8f);
                Func<float,float,float,Vector3> radial=(rx,y,rz)=>V(Mathf.Sin(theta)*rx,y,Mathf.Cos(theta)*rz);
                int bone=a.ids["J_Sec_Hair"+(1+Mathf.RoundToInt(i*.5f))+"_01"];
                int group=Mathf.Min(i,8-i);
                float lift=new[]{.008f,.055f,.022f,.085f,.035f}[group];
                float layer=new[]{.008f,-.006f,.013f,-.010f,.006f}[group];
                float lowerWidth=new[]{.77f,.66f,.80f,.70f,.78f}[group];
                float taperAt=new[]{.68f,.66f,.67f,.65f,.66f}[group];
                var controls=new[]{radial(.008f,.979f,.006f),radial(.150f,.979f,.118f),
                    radial(.244f,.88f,.192f),radial(.244f,.735f,.192f),
                    radial(.244f,.65f,.205f),radial(.243f+layer*.25f,.56f,.225f+layer*.25f),
                    radial(.265f+layer,.48f+lift*.18f,.248f+layer),
                    radial(.325f+layer,.33f+lift*.60f,.310f+layer),
                    Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero};
                // Rounded lower guides set long side tips and shorter inner
                // layers. Their last span travels down/forward instead of
                // curling every tip back upward into a repeating ringlet.
                if(group==1)controls[7]=radial(.295f,.405f,.280f);
                if(group==3)controls[7]=radial(.295f,.405f,.275f);
                float edgeBias=group==2?(i<4?.035f:-.035f):i==4?-.035f:0;
                for(int k=8;k<=12;k++) {
                    Vector3 guide=RearHemControls[group][k-8];
                    if(i>4)guide.x=-guide.x;
                    controls[k]=guide;
                }
                a.HairPanel(controls,.067f,bone,true,i,V(Mathf.Sin(theta),0,Mathf.Cos(theta)),8,44,
                    tipStart:taperAt,backSweep:HairSweepAtAngle(theta),lowerWidth:lowerWidth,tipBias:edgeBias,
                    tipPower:new[]{.75f,.46f,.60f,.48f,.50f}[group]);
            }
            // The rear volume shares one depth profile across the cap, inner
            // mantle and long locks. This keeps their overlap intact as the
            // shoulder-to-hem silhouette opens behind the head.
            Vector3 sectionCenter=Vector3.zero;
            for(int i=rearStart;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];float dy,dz;
                Vector3 coverageSlope=Vector3.zero;
                Vector3 shaped=ShapeRearHair(p,out dy,out dz);
                if(i>=radialStart) {
                    if((i-radialStart)%9==0) {
                        sectionCenter=Vector3.zero;
                        for(int j=0;j<8;j++)sectionCenter+=a.body.p[i+j]/8;
                    }
                    // Translate each lower section as a whole; independently
                    // stretching its front and rear edges produces a broad paddle.
                    Vector3 section=V(p.x,p.y,p.z+RearHairPoint(sectionCenter).z-sectionCenter.z);
                    float crown=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.550f,.680f,sectionCenter.y));
                    shaped=Vector3.Lerp(section,shaped,.30f+.70f*crown);
                    int lockIndex=(i-radialStart)/405,group=Mathf.Min(lockIndex,8-lockIndex);
                    float side=1-group/4f;
                    float theta=Mathf.Atan2(p.x,p.z);
                    float angularSide=Mathf.Clamp01((Mathf.PI-Mathf.Abs(theta))/(Mathf.PI-1.12f));
                    side=Mathf.Lerp(side,angularSide,crown);
                    Vector3 coverage=SideHairCoverage(sectionCenter.y);
                    Vector3 scale=V(Mathf.Sign(p.x)*Mathf.Pow(side,1.2f),0,Mathf.Pow(side,1.6f));
                    shaped+=Vector3.Scale(coverage,scale);
                    coverageSlope=Vector3.Scale((SideHairCoverage(sectionCenter.y+.0001f)-
                        SideHairCoverage(sectionCenter.y-.0001f))/.0002f,scale);
                }
                a.body.p[i]=shaped;
                if(a.body.normalBlends.TryGetValue(i,out var blend)) {
                    Vector3 n=V(blend.x,blend.y-dy*blend.z/dz,blend.z/dz);
                    n.y-=coverageSlope.x*n.x+coverageSlope.z*n.z;n.Normalize();
                    a.body.normalBlends[i]=new Vector4(n.x,n.y,n.z,blend.w);
                }
            }
            // Join the visible upper surfaces across lock boundaries. Closed
            // panel edges include inner faces, so their ordinary averaged mesh
            // normals make dark channels even when neighboring edges coincide.
            for(int lockIndex=0;lockIndex<9;lockIndex++)for(int row=1;row<44;row++)
                for(int j=0;j<=4;j++) {
                    int index=radialStart+lockIndex*405+row*9+j;
                    Vector3 p=a.body.p[index];
                    float join=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.550f,.680f,p.y))*
                        (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.820f,.900f,p.y)));
                    if(join<=0)continue;
                    Vector3 along=a.body.p[index+9]-a.body.p[index-9];
                    Vector3 minus=j>0?a.body.p[index-1]:lockIndex<8?a.body.p[index+405+3]:p;
                    Vector3 plus=j<4?a.body.p[index+1]:lockIndex>0?a.body.p[index-405-3]:p;
                    Vector3 normal=Vector3.Cross(along,plus-minus).normalized;
                    if(normal.sqrMagnitude<.1f)continue;
                    float weight=join;
                    if(a.body.normalBlends.TryGetValue(index,out var old)) {
                        weight=old.w*(1-join)+join;
                        normal=(V(old.x,old.y,old.z)*old.w*(1-join)+normal*join).normalized;
                    }
                    var target=new Vector4(normal.x,normal.y,normal.z,weight);
                    a.body.normalBlends[index]=target;
                    if(j==0)a.body.normalBlends[index+8]=target;
                }
            foreach(int sign in new[]{-1,1}) {
                int bone=a.ids[sign<0?"J_Sec_Hair1_01":"J_Sec_Hair5_01"];
                // Separate long S-shaped locks leave negative space at the hem.
                a.HairPanel(new[]{V(sign*.17f,.90f,-.045f),V(sign*.27f,.73f,-.025f),
                    V(sign*.22f,.59f,.005f),V(sign*.245f,.45f,.02f),
                    V(sign*.28f,.31f,.035f),V(sign*.17f,.21f,.055f),V(sign*.29f,.17f,.06f)},.063f,bone,true,2,
                    rootScale:.50f,backSweep:sign*.20f,sideTurn:sign*32,fullness:1.8f);
                a.HairPanel(new[]{V(sign*.21f,.79f,-.04f),V(sign*.28f,.64f,-.005f),
                    V(sign*.28f,.47f,.025f),V(sign*.31f,.36f,.025f),
                    V(sign*.39f,.22f,.035f),V(sign*.22f,.28f,.055f),V(sign*.22f,.16f,.06f)},.035f,bone,true,3,
                    rootScale:.50f,backSweep:sign*.12f,sideTurn:sign*46,fullness:2.2f);
                a.HairPanel(new[]{V(sign*.218f,.71f,.006f),V(sign*.255f,.62f,.014f),
                    V(sign*.29f,.48f,.01f),V(sign*.27f,.47f,.025f),
                    V(sign*.24f,.46f,.025f),V(sign*.27f,.44f,.03f),V(sign*.30f,.43f,.03f)},.014f,bone,false,4,
                    backSweep:sign*.12f,sideTurn:sign*42,fullness:1.5f,rootTaper:.18f);
                // The front drape starts behind the temple, turns forward at
                // the collar, and opens into a flat S-shaped tip. Independent
                // edges keep that turn from twisting the lock into a tube.
                var drapeInner=new[]{V(sign*.18f,.90f,.035f),V(sign*.19f,.84f,.052f),
                    V(sign*.18f,.74f,.050f),V(sign*.155f,.655f,.055f),
                    V(sign*.140f,.615f,.085f),V(sign*.130f,.57f,.100f),V(sign*.110f,.53f,.115f),
                    V(sign*.105f,.51f,.130f),V(sign*.125f,.485f,.145f),V(sign*.140f,.46f,.145f),
                    V(sign*.150f,.435f,.140f),V(sign*.148f,.400f,.138f),V(sign*.135f,.385f,.135f)};
                var drapeOuter=new[]{V(sign*.215f,.90f,.020f),V(sign*.245f,.84f,.015f),
                    V(sign*.242f,.74f,.012f),V(sign*.212f,.655f,-.015f),
                    V(sign*.195f,.615f,.000f),V(sign*.170f,.57f,.015f),V(sign*.175f,.53f,.045f),
                    V(sign*.190f,.51f,.070f),V(sign*.202f,.485f,.110f),V(sign*.190f,.46f,.130f),
                    V(sign*.175f,.435f,.130f),V(sign*.148f,.400f,.133f),V(sign*.135f,.385f,.135f)};
                a.HairPanel(drapeInner.Zip(drapeOuter,(l,r)=>(l+r)*.5f).ToArray(),.030f,bone,true,5,
                    fullness:.65f,tipPaintEase:.32f,edgeA:drapeInner,edgeB:drapeOuter,paintOffset:.25f,wrapDepth:.040f);
                var layerInner=new[]{V(sign*.180f,.90f,.035f),V(sign*.180f,.84f,.052f),
                    V(sign*.170f,.74f,.059f),V(sign*.166f,.655f,.085f),
                    V(sign*.150f,.615f,.125f),V(sign*.147f,.57f,.142f),V(sign*.155f,.53f,.154f),
                    V(sign*.165f,.51f,.165f),V(sign*.175f,.485f,.172f),V(sign*.172f,.46f,.171f),
                    V(sign*.163f,.435f,.169f),V(sign*.150f,.40f,.168f),V(sign*.140f,.375f,.168f)};
                var layerOuter=new[]{V(sign*.215f,.90f,.020f),V(sign*.225f,.84f,.015f),
                    V(sign*.215f,.74f,.012f),V(sign*.201f,.655f,.024f),
                    V(sign*.187f,.615f,.043f),V(sign*.185f,.57f,.057f),V(sign*.191f,.53f,.086f),
                    V(sign*.210f,.51f,.119f),V(sign*.210f,.485f,.140f),V(sign*.200f,.46f,.155f),
                    V(sign*.177f,.435f,.162f),V(sign*.158f,.40f,.167f),V(sign*.140f,.375f,.168f)};
                a.HairPanel(layerInner.Zip(layerOuter,(l,r)=>(l+r)*.5f).ToArray(),.024f,bone,true,6,
                    samples:24,fullness:.55f,edgeA:layerInner,edgeB:layerOuter,tipPaintEase:.45f,paintOffset:.25f,wrapDepth:.010f);
                // Independently drawn edges keep the temple broad while the
                // inner edge curls around the cheek into staggered lower tips.
                var cheekInner=new[]{V(sign*.050f,.970f,.045f),V(sign*.140f,.94f,.14f),
                    V(sign*.158f,.83f,.185f),V(sign*.149f,.738f,.177f),
                    V(sign*.145f,.666f,.173f),V(sign*.125f,.640f,.164f),V(sign*.107f,.633f,.162f)};
                var cheekOuter=new[]{V(sign*.082f,.970f,.045f),V(sign*.250f,.94f,.105f),
                    V(sign*.280f,.83f,.090f),V(sign*.236f,.738f,.103f),
                    V(sign*.216f,.654f,.119f),V(sign*.158f,.626f,.150f),V(sign*.107f,.633f,.162f)};
                a.HairPanel(cheekInner.Zip(cheekOuter,(l,r)=>(l+r)*.5f).ToArray(),.060f,head,false,1,
                    samples:24,tipStart:.61f,rootScale:.79f,cheekInset:0,edgeA:cheekInner,edgeB:cheekOuter,
                    paintGuide:new[]{V(0,.96f,0),V(0,.92f,0),V(0,.716f,0),V(0,.640f,0)});
                a.HairPanel(new[]{V(sign*.155f,.90f,.070f),V(sign*.285f,.74f,.112f),
                    V(sign*.210f,.630f,.160f),V(sign*.114f,.618f,.160f)},.022f,head,false,4,
                    rootTaper:.22f,paintStart:.50f,cheekInset:.003f);
                var inner=new[]{V(sign*.010f,.974f,.060f),V(sign*.030f,.93f,.17f),
                    V(sign*.004f,.770f,.225f),V(sign*.165f,.700f,.158f)};
                var outer=new[]{V(sign*.065f,.955f,.060f),V(sign*.170f,.93f,.14f),
                    V(sign*.205f,.79f,.145f),V(sign*.165f,.700f,.158f)};
                a.HairPanel(inner.Zip(outer,(l,r)=>(l+r)*.5f).ToArray(),.056f,head,false,sign,
                    tipStart:.72f,tipBias:-sign*.045f,tipPower:.90f,tipPaintEase:.54f,edgeA:inner,edgeB:outer,frontLift:.015f);
            }
            // Three staggered central tips share a broad crown, rather than
            // tapering the entire fringe into one symmetrical wedge. The three
            // sections reuse the previous two sections' total 650 vertices.
            var mainLeft=new[]{V(.026f,.978f,.055f),V(.052f,.94f,.18f),V(.057f,.845f,.236f),V(.048f,.770f,.232f),
                V(.045f,.734f,.234f),V(.030f,.724f,.232f),V(.018f,.708f,.229f)};
            var mainRight=new[]{V(-.018f,.978f,.055f),V(-.051f,.94f,.18f),V(-.078f,.845f,.230f),V(-.060f,.770f,.225f),
                V(-.060f,.742f,.223f),V(-.055f,.729f,.224f),V(-.045f,.709f,.225f)};
            var splitA=mainLeft.Zip(mainRight,(l,r)=>Vector3.Lerp(l,r,1f/6)).ToArray();
            var splitB=mainLeft.Zip(mainRight,(l,r)=>Vector3.Lerp(l,r,5f/6)).ToArray();
            var leftInner=(Vector3[])splitA.Clone();
            leftInner[4]=V(.031f,.746f,.237f);leftInner[5]=V(.021f,.728f,.233f);leftInner[6]=mainLeft[6];
            var rightInner=(Vector3[])splitB.Clone();
            rightInner[4]=V(-.040f,.746f,.235f);rightInner[5]=V(-.044f,.730f,.230f);rightInner[6]=mainRight[6];
            splitA[4]=V(.030f,.740f,.240f);splitA[5]=V(.007f,.711f,.234f);splitA[6]=V(-.025f,.698f,.229f);
            splitB[4]=V(-.047f,.744f,.236f);splitB[5]=V(-.042f,.724f,.232f);splitB[6]=splitA[6];
            var paintGuide=new[]{V(0,.978f,0),V(0,.91f,0),V(0,.768f,0),V(0,.713f,0)};
            a.HairPanel(mainLeft.Zip(leftInner,(l,r)=>(l+r)*.5f).ToArray(),.023f,head,false,2,
                samples:16,fullness:.65f,tipStart:.71f,tipPaintEase:.50f,edgeA:mainLeft,edgeB:leftInner,paintGuide:paintGuide);
            a.HairPanel(splitA.Zip(splitB,(l,r)=>(l+r)*.5f).ToArray(),.023f,head,false,0,
                samples:16,fullness:.65f,tipStart:.71f,tipPaintEase:.50f,edgeA:splitA,edgeB:splitB,paintGuide:paintGuide);
            a.HairPanel(rightInner.Zip(mainRight,(l,r)=>(l+r)*.5f).ToArray(),.023f,head,false,1,
                samples:15,fullness:.65f,tipStart:.71f,tipPaintEase:.50f,edgeA:rightInner,edgeB:mainRight,paintGuide:paintGuide);
            // A gently rearward arch keeps the crown tuft curved in side view.
            // Its rounded section narrows in both axes into one closed tip.
            int tuftStart=a.body.p.Count;
            Func<float,float> tuftProfile=t=>t>=1?0:Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Lerp(.21f,1,t)),.75f);
            a.Sweep(a.body,Curve(new[]{V(-.025f,.974f,-.02f),V(-.025f,1.11f,-.025f),
                V(.10f,1.10f,-.090f),V(.12f,1.03f,-.095f)},24),
                t=>.013f*tuftProfile(t),t=>.0075f*tuftProfile(t),t=>Hair,head,8);
            for(int row=0;row<24;row++)a.body.smoothNormalPairs.Add(new Vector2Int(tuftStart+row*9,tuftStart+row*9+8));
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
            // Six height intervals retain the gently quadratic skirt drape;
            // the 72 angular samples still describe all twelve broad pleats.
            Lathe(a,hips,.173f,.375f,t=>Mathf.Lerp(.223f,.113f,t),t=>Mathf.Lerp(.137f,.077f,t),Navy,6,72);
            Lathe(a,hips,.133f,.170f,t=>Mathf.Lerp(.214f,.222f,t),
                t=>Mathf.Lerp(.134f,.138f,t),White*.96f,2,72);
            Ruffle(a,hips,.164f,.223f,.138f,.022f,24);
            Ruffle(a,hips,.145f,.213f,.130f,.022f,24,Mathf.PI*.65f);
            Lathe(a,hips,.365f,.384f,t=>.116f,t=>.079f,Navy*.82f,2,48);
            ClothPanel(a,V(0,.458f,.080f),.071f,.096f,.009f,chest);
            Collar(a,chest);
            foreach(int sign in new[]{-1,1}) {
                // Gathered side lace starts on the bib surface and opens into
                // broad rounded folds, with enough samples to avoid saw teeth.
                int first=a.body.p.Count,laceTriangles=a.body.tri.Count; const int rows=48,cols=6;
                for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++) {
                    float t=y/(float)rows,r=x/(float)cols;
                    float phase=t*Mathf.PI*12+.25f*Mathf.Sin(t*Mathf.PI*2),fold=Mathf.Cos(phase);
                    float inner=.069f-.008f*Mathf.Pow(Mathf.Abs(t*2-1),6);
                    float endTaper=.70f+.30f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.40f);
                    float py=.546f-t*.176f,px=sign*(inner+.012f*r*(1+.10f*fold)*endTaper);
                    float baseDepth=.082f+BibDepth(inner,py-.458f,.071f,.096f,.009f);
                    a.body.Add(V(px,py+.002f*r*Mathf.Sin(phase),
                        baseDepth+.006f*Mathf.Sin(r*Mathf.PI)+.0035f*r*fold),
                        Color.Lerp(White*.91f,White,(fold+1)*.5f),chest);
                }
                Grid(a.body,first,rows,cols);
                if(sign>0)ReverseFaces(a.body,laceTriangles);
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
            for(int i=0;i<3;i++) {
                float y=.390f+i*.035f;
                a.Sphere(a.body,V(0,y,.0818f+BibDepth(0,y-.458f,.071f,.096f,.009f)),
                    V(.004f,.004f,.002f),Ink,chest,12,6);
            }
            a.Sphere(a.body,V(0,.376f,.083f),V(.006f,.003f,.002f),Gold,hips,12,6);
            foreach(float y in new[]{.184f,.195f}) {
                int start=a.body.p.Count;const int sides=144,rows=2;
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
            // A flat waist edge opens into a rounded rectangular cloth panel.
            // Its surface drapes along the skirt; the lace follows only the U edge.
            var apronControls=new[]{V(-.078f,.374f,0),V(-.030f,.374f,0),V(.030f,.374f,0),V(.078f,.374f,0),
                V(.090f,.350f,0),V(.104f,.296f,0),V(.098f,.262f,0),
                V(.092f,.220f,0),V(.070f,.207f,0),V(0,.207f,0),
                V(-.070f,.207f,0),V(-.092f,.220f,0),V(-.098f,.262f,0),
                V(-.104f,.296f,0),V(-.090f,.350f,0),V(-.078f,.374f,0)};
            var outline=Curve(apronControls,80);
            int apronStart=a.body.p.Count;const int apronRings=8;
            for(int row=0;row<=apronRings;row++)for(int i=0;i<outline.Length;i++) {
                float r=row/(float)apronRings;
                Vector3 p=Vector3.Lerp(V(0,.293f,0),outline[i],r);
                float gather=ApronGather(p.x,p.y);
                Color cloth=White*Mathf.Lerp(.91f,1,(gather+1)*.5f);cloth.a=1;
                a.body.Add(ApronSurface(p.x,p.y),cloth,hips);
            }
            Grid(a.body,apronStart,apronRings,outline.Length-1);
            var lacePath=Curve(apronControls.Skip(3).ToArray(),128);
            int laceStart=a.body.p.Count;const int laceRows=4;
            for(int row=0;row<=laceRows;row++)for(int i=0;i<lacePath.Length;i++) {
                float t=i/(float)(lacePath.Length-1),r=row/(float)laceRows;
                Vector3 tangent=(lacePath[Mathf.Min(i+1,lacePath.Length-1)]-
                    lacePath[Mathf.Max(0,i-1)]).normalized;
                Vector3 outward=V(-tangent.y,tangent.x,0);
                float fold=Mathf.Cos(t*Mathf.PI*40);
                float end=Mathf.SmoothStep(0,1,Mathf.Min(t,1-t)/.045f);
                Vector3 p=lacePath[i]+outward*(.014f+.003f*fold)*r*end;
                p=ApronSurface(p.x,p.y)+Vector3.forward*(Mathf.Sin(r*Mathf.PI)*.004f+fold*r*.002f)*end;
                a.body.Add(p,Color.Lerp(White*.88f,White,(fold+1)*.5f),hips);
            }
            Grid(a.body,laceStart,laceRows,lacePath.Length-1);
            ApronStroke(a,outline,.0011f,White*.85f,hips);
            // Surface artwork keeps the large whale, without hidden sphere backs.
            int whaleStart=a.body.p.Count,whaleTriangles=a.body.tri.Count;
            Color blue=Hex("577FAE");
            ApronOval(a,V(-.014f,.281f,0),new Vector2(.046f,.030f),blue,hips,.0015f,24,5);
            ApronOval(a,V(-.027f,.268f,0),new Vector2(.030f,.012f),White,hips,.0024f,24,3);
            ApronStroke(a,Curve(new[]{V(.015f,.276f,0),V(.040f,.278f,0),
                V(.038f,.309f,0),V(.050f,.317f,0)},20),.0085f,blue,hips);
            foreach(int sign in new[]{-1,1})ApronOval(a,V(.050f+sign*.010f,.319f,0),
                new Vector2(.013f,.008f),blue,hips,.0015f,16,2);
            ApronOval(a,V(-.040f,.286f,0),new Vector2(.0022f,.0022f),White,hips,.0024f,12,1);
            foreach(int sign in new[]{-1,1})ApronStroke(a,Curve(new[]{V(-.018f,.315f,0),
                V(-.020f,.327f,0),V(-.018f+sign*.009f,.334f,0),V(-.018f+sign*.012f,.326f,0)},12),.002f,blue,hips);
            for(int i=whaleStart;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];
                float lift=p.z-ApronSurface(p.x,p.y).z;
                p=ApronSurface(-p.x,p.y)+Vector3.forward*lift;
                a.body.p[i]=p;
            }
            ReverseFaces(a.body,whaleTriangles);
            // Shape the whole hip-bound garment together, including embroidery,
            // apron artwork and both lace layers. Keep the waist attachment fixed
            // while giving the hem a deeper, longer bell silhouette.
            for(int i=dressStart;i<a.body.p.Count;i++)
                if(a.body.weights[i].boneIndex0==hips)a.body.p[i]=DressVolume(a.body.p[i]);
        }

        static Vector3 DressVolume(Vector3 p) {
            float lower=Mathf.InverseLerp(.375f,.173f,p.y);
            float back=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,-.060f,p.z));
            float depth=Mathf.Lerp(.9f*lower,1.25f*Mathf.Pow(lower,1.25f),back);
            p.z*=1+depth;
            p.y=p.y>=.173f?p.y-.025f*lower:.148f+(p.y-.173f)*.85f;
            return p;
        }

        static float ApronGather(float x,float y) {
            float t=Mathf.InverseLerp(.374f,.207f,y);
            return Mathf.Cos(x/(.078f+.025f*t)*Mathf.PI*5+.4f*Mathf.Sin(t*5))*
                Mathf.Pow(1-t,1.5f);
        }
        static Vector3 ApronSurface(float x,float y) {
            float t=Mathf.InverseLerp(.173f,.375f,y);
            float rx=Mathf.Lerp(.223f,.113f,t),rz=Mathf.Lerp(.137f,.077f,t);
            float curve=Mathf.Sqrt(Mathf.Max(.08f,1-x*x/(rx*rx)));
            float drape=Mathf.Sin(Mathf.InverseLerp(.207f,.374f,y)*Mathf.PI);
            float z=rz*curve+.009f+.005f*drape+.002f*ApronGather(x,y);
            return V(x,y,z);
        }
        static void ApronOval(Author a,Vector3 c,Vector2 size,Color color,int bone,float lift,int sides,int rings) {
            int start=a.body.p.Count;
            for(int row=0;row<=rings;row++)for(int i=0;i<=sides;i++) {
                float r=row/(float)rings,theta=-i/(float)sides*Mathf.PI*2;
                Vector3 p=ApronSurface(c.x+Mathf.Cos(theta)*size.x*r,c.y+Mathf.Sin(theta)*size.y*r);
                a.body.Add(p+Vector3.forward*lift,color,bone);
            }
            Grid(a.body,start,rings,sides);
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

        static void Collar(Author a,int bone) {
            int start=a.body.p.Count;const int sides=32;
            for(int row=0;row<=2;row++)for(int i=0;i<=sides;i++) {
                float t=row/2f,theta=i/(float)sides*Mathf.PI*2;
                float radius=Mathf.Lerp(.038f,.031f,t);
                a.body.Add(V(Mathf.Sin(theta)*radius,Mathf.Lerp(.550f,.576f,t),
                    Mathf.Cos(theta)*radius),White*.94f,bone);
            }
            Grid(a.body,start,2,sides);
            foreach(int sign in new[]{-1,1}) {
                var neck=Curve(new[]{V(sign*.004f,.575f,.028f),V(sign*.019f,.576f,.029f),
                    V(sign*.035f,.568f,.012f),V(sign*.032f,.559f,.004f)},12);
                var edge=Curve(new[]{V(sign*.012f,.542f,.098f),V(sign*.034f,.528f,.090f),
                    V(sign*.080f,.539f,.061f),V(sign*.083f,.548f,.030f)},12);
                int first=a.body.p.Count,triangles=a.body.tri.Count;const int cols=4;
                for(int i=0;i<=12;i++)for(int j=0;j<=cols;j++) {
                    float t=i/12f,r=j/(float)cols;
                    Vector3 p=Vector3.Lerp(neck[i],edge[i],r);
                    p.y+=.002f*Mathf.Sin(r*Mathf.PI);
                    p.z+=.003f*Mathf.Sin(r*Mathf.PI)*Mathf.Sin(t*Mathf.PI);
                    Color color=White*Mathf.Lerp(.90f,1,Mathf.Sin(r*Mathf.PI*.70f));color.a=1;
                    a.body.Add(p,color,bone);
                }
                Grid(a.body,first,12,cols);
                if(sign<0)ReverseFaces(a.body,triangles);
            }
        }

        static void ChestRibbon(Author a,int bone) {
            foreach(int sign in new[]{-1,1}) {
                const int rows=10,cols=6;
                foreach(int surface in new[]{1,-1}) {
                    int start=a.body.p.Count,triangles=a.body.tri.Count;
                    for(int i=0;i<=rows;i++)for(int j=0;j<=cols;j++) {
                        float u=i/(float)rows,v=j/(float)cols*2-1;
                        float swell=Mathf.Sin(u*Mathf.PI)*(1-v*v);
                        float fold=Mathf.Exp(-Mathf.Pow((Mathf.Abs(v)-.34f)/.18f,2))*Mathf.Sin(u*Mathf.PI);
                        float x=sign*(.005f+.057f*u)*(1-.08f*Mathf.Pow(Mathf.Abs(v),3));
                        float y=.536f+.004f*u+v*(.006f+.021f*Mathf.Sin(u*Mathf.PI*.55f));
                        float z=.103f-.012f*u+(surface>0?.006f:-.002f)*swell-surface*.0025f*fold;
                        Color color=Color.Lerp(Navy,Hex("4B5B8B"),swell*.24f)*(.93f+.16f*(v+1)*.5f-.18f*fold);color.a=1;
                        a.body.Add(V(x,y,z),color,bone);
                    }
                    Grid(a.body,start,rows,cols);
                    if((surface>0)==(sign>0))ReverseFaces(a.body,triangles);
                }
                // Two broad draped tails keep the notch and center gap readable.
                int tailStart=a.body.p.Count,tailTriangles=a.body.tri.Count;const int tailCols=4;
                for(int i=0;i<=10;i++)for(int j=0;j<=tailCols;j++) {
                    float t=i/10f,v=j/(float)tailCols*2-1;
                    float x=sign*(.010f+.010f*t+.007f*t*t+v*(.004f+.012f*t));
                    float y=.534f-.060f*t+.006f*(1-Mathf.Abs(v))*Mathf.Pow(t,10);
                    float z=.100f+.005f*Mathf.Sin(t*Mathf.PI)-.004f*v*v;
                    Color color=Navy*(.90f+.16f*(1-v*v));color.a=1;
                    a.body.Add(V(x,y,z),color,bone);
                }
                Grid(a.body,tailStart,10,tailCols);
                if(sign>0)ReverseFaces(a.body,tailTriangles);
            }
            a.Sphere(a.body,V(0,.538f,.104f),V(.010f,.008f,.006f),Navy,bone,12,8);
        }

        static void ClothPanel(Author a,Vector3 center,float rx,float ry,float bulge,int bone) {
            // A regular grid spends samples across folds and avoids the repeated
            // center vertices and thin triangles of concentric rings.
            int start=a.body.p.Count; const int rows=16,cols=24;
            for(int row=0;row<=rows;row++)for(int i=0;i<=cols;i++) {
                float u=i/(float)cols*2-1,v=row/(float)rows*2-1;
                const float power=2/.55f;
                float radius=Mathf.Pow(Mathf.Pow(Mathf.Abs(u),power)+Mathf.Pow(Mathf.Abs(v),power),1/power);
                float r=Mathf.Max(Mathf.Abs(u),Mathf.Abs(v)),fit=radius>.00001f?r/radius:1;
                float x=u*fit*rx,y=v*fit*ry;
                float depth=BibDepth(x,y,rx,ry,bulge);
                float fold=depth-bulge*(1-r*r);
                a.body.Add(center+V(x,y,depth),White*(.96f+.065f*Mathf.Clamp(fold/.003f,-1,1)),bone);
            }
            Grid(a.body,start,rows,cols);
        }
        static float BibDepth(float x,float y,float rx,float ry,float bulge) {
            float u=x/rx,v=y/ry,h=Mathf.Clamp01((v+1)*.5f);
            const float power=2/.55f;
            float radius=Mathf.Pow(Mathf.Pow(Mathf.Abs(u),power)+Mathf.Pow(Mathf.Abs(v),power),1/power);
            float gather=Mathf.Exp(-Mathf.Pow((h-.08f)/.22f,2))+
                .80f*Mathf.Exp(-Mathf.Pow((h-.94f)/.25f,2));
            float arc=Mathf.Sin(h*Mathf.PI),guide=.46f+.16f*arc+.035f*(h-.5f);
            float width=.075f+.065f*arc;
            float ridge=Mathf.Exp(-Mathf.Pow((Mathf.Abs(u)-guide)/width,2));
            float valley=Mathf.Exp(-Mathf.Pow((Mathf.Abs(u)-guide-.15f)/(width*.80f),2));
            float centerClear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.13f,.30f,Mathf.Abs(u)));
            float fold=(.004f*ridge-.0035f*valley)*(.32f+.68f*gather)*centerClear;
            // Tuck the lower cloth and its lace behind the existing waist band.
            // The longer panel must not cover the dark belt or merge into the apron.
            float waistTuck=.014f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.28f,h)));
            return bulge*Mathf.Max(0,1-radius*radius)+fold-waistTuck;
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
        static void Ruffle(Author a,int bone,float y,float rx,float rz,float width,int lobes,float phase=0) {
            int start=a.body.p.Count;const int sides=144,rows=5;
            for(int j=0;j<=rows;j++)for(int i=0;i<=sides;i++) {
                float theta=i/(float)sides*Mathf.PI*2,t=j/(float)rows;
                float fold=Mathf.Cos(theta*lobes+phase),opening=Mathf.Sin(t*Mathf.PI*.5f);
                float gather=Mathf.SmoothStep(0,1,t);
                float radius=1+width*opening/rx*(1+.20f*fold*gather);
                float drop=.019f*(1-Mathf.Cos(t*Mathf.PI*.5f))+.0032f*fold*t*t;
                a.body.Add(V(Mathf.Sin(theta)*rx*radius,y-drop,Mathf.Cos(theta)*rz*radius),
                    Color.Lerp(White*(.95f-.06f*t),White,(fold+1)*.5f),bone);
            }
            int firstTriangle=a.body.tri.Count;
            Grid(a.body,start,rows,sides);
            ReverseFaces(a.body,firstTriangle);
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
                float[] sleeveX={.092f,.110f,.130f,.153f,.180f,.205f,.222f,.235f,.248f,.268f,.290f,.309f,.317f,.325f};
                float[] sleeveRadius={.025f,.040f,.045f,.046f,.041f,.035f,.033f,.032f,.032f,.030f,.028f,.0275f,.0275f,.027f};
                int sleeveStart=a.body.p.Count,sleeveTriangles=a.body.tri.Count;const int sleeveSides=16;
                for(int row=0;row<sleeveX.Length;row++)for(int j=0;j<=sleeveSides;j++) {
                    float x=sleeveX[row],theta=j/(float)sleeveSides*Mathf.PI*2;
                    float gathering=Mathf.Sin(Mathf.InverseLerp(.11f,.205f,x)*Mathf.PI);
                    float fold=Mathf.Cos(theta*4+.6f)*gathering;
                    float radius=sleeveRadius[row]*(1+.035f*fold);
                    int primary=x<.18f?chest:upper,secondary=x<.18f?upper:elbow;
                    float blend=x<.18f?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.070f,.180f,x)):
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.192f,.278f,x));
                    Color cloth=Navy*(.98f+.025f*fold);cloth.a=1;
                    a.body.Add(V(s*x,.49f+Mathf.Cos(theta)*radius,Mathf.Sin(theta)*radius*.95f),
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
                foreach(float centerX in new[]{.309f,.317f}) {
                    // Shallow embroidered bands follow the same elliptical
                    // sleeve profile instead of intersecting it as round tubes.
                    int bandStart=a.body.p.Count,bandTriangles=a.body.tri.Count;
                    const int bandSegments=32;
                    for(int row=0;row<3;row++) {
                        float x=centerX+(row-1)*.0018f;
                        int section=1;
                        while(section<sleeveX.Length-1&&sleeveX[section]<x)section++;
                        float radius=Mathf.Lerp(sleeveRadius[section-1],sleeveRadius[section],
                            Mathf.InverseLerp(sleeveX[section-1],sleeveX[section],x));
                        float relief=row==1?.0007f:.00035f;
                        for(int j=0;j<=bandSegments;j++) {
                            float theta=j/(float)bandSegments*Mathf.PI*2;
                            a.body.Add(V(s*x,.49f+Mathf.Cos(theta)*(radius+relief),
                                Mathf.Sin(theta)*(radius*.95f+relief)),Gold,elbow);
                        }
                    }
                    Grid(a.body,bandStart,2,bandSegments);
                    if(s<0)ReverseFaces(a.body,bandTriangles);
                }
                // One soft volume joins wrist and palm. Gradual skin weights
                // avoid the seam between two independently turning spheres.
                int palmStart=a.body.p.Count,palmTriangles=a.body.tri.Count;
                const int palmRings=16,palmSides=16;
                for(int row=0;row<=palmRings;row++)for(int j=0;j<=palmSides;j++) {
                    float t=row/(float)palmRings,theta=j/(float)palmSides*Mathf.PI*2;
                    // Keep the wrist and its skinning transition fixed while
                    // shortening only the distal palm under the finger roots.
                    float x=.319f+.062f*t-.003f*
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,t));
                    float cap=row==palmRings?0:
                        Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(Mathf.Max(0,(t-.72f)/.28f),2)));
                    float ry=.018f*(1+.08f*Mathf.Sin(t*Mathf.PI))*cap;
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
                    int start=a.body.p.Count;const int rings=9,sides=10;
                    for(int row=0;row<=rings;row++)for(int j=0;j<=sides;j++) {
                        float theta=j/(float)sides*Mathf.PI*2;
                        // Five stem intervals and four angular cap intervals
                        // produce a round fingertip without an elongated point.
                        float capAngle=Mathf.Max(0,(row-5)/4f)*Mathf.PI*.5f;
                        float x=row<=5?.366f+(length-fingerRadius)*(row/5f):
                            .366f+length-fingerRadius+Mathf.Sin(capAngle)*fingerRadius;
                        float radius=row<=5?fingerRadius*Mathf.Lerp(1.10f,1,row/5f):
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
                int sockStart=a.body.p.Count;
                a.Sphere(a.body,V(s*.065f,.112f,.003f),V(.032f,.055f,.033f),White,knee);
                for(int i=sockStart;i<a.body.p.Count;i++) {
                    Vector3 p=a.body.p[i];
                    float lower=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.140f,.167f,p.y));
                    float angle=Mathf.Atan2((p.z-.003f)/.033f,(p.x-s*.065f)/.032f);
                    float cuff=Mathf.Exp(-Mathf.Pow((p.y-.120f)/.016f,2));
                    float fold=.002f*Mathf.Cos(angle*8)*cuff;
                    p.x+=s*.002f*lower+(p.x-s*.065f)*.25f*lower+Mathf.Cos(angle)*fold;
                    p.z+=(p.z-.003f)*.35f*lower-.012f*lower+Mathf.Sin(angle)*fold;
                    p.y+=fold*.45f;
                    a.body.p[i]=p;
                }
                ShoeUpper(a,s,foot);
                int soleStart=a.body.p.Count;
                a.Sphere(a.body,V(s*.065f,.020f,.023f),V(.047f,.009f,.066f),Hex("19213C"),foot,16,6);
                int soleEnd=a.body.p.Count;
                a.Tube(a.body,new[]{V(s*.065f-.027f,.066f,.059f),V(s*.065f,.073f,.067f),V(s*.065f+.027f,.066f,.059f)},.006f,White,foot);
                Func<float,float,Vector3> shoeSurface=(x,y)=>V(x,y,.022f+.068f*Mathf.Sqrt(Mathf.Max(.01f,
                    1-Mathf.Pow((x-s*.065f)/.048f,2)-Mathf.Pow((y-.045f)/.031f,2)))+.0018f);
                float buckleX=s*.089f;
                var buckle=new[]{new Vector2(-.008f,-.0025f),new Vector2(-.006f,-.0045f),
                    new Vector2(.006f,-.0045f),new Vector2(.008f,-.0025f),new Vector2(.008f,.0025f),
                    new Vector2(.006f,.0045f),new Vector2(-.006f,.0045f),new Vector2(-.008f,.0025f),
                    new Vector2(-.008f,-.0025f)};
                a.Tube(a.body,buckle.Select(p=>shoeSurface(buckleX+p.x,.059f+p.y)).ToArray(),.0014f,Gold,foot,6);
                a.Tube(a.body,new[]{shoeSurface(buckleX,.055f),shoeSurface(buckleX,.063f)},.0011f,Gold,foot,4);
                // The same authored volume map carries the strap and buckle.
                // Keep the sole shallow while giving the upper a taller heel and toe.
                for(int i=soleStart;i<a.body.p.Count;i++) {
                    Vector3 p=a.body.p[i];
                    float heel=.018f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.040f,-.025f,p.z))*
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.060f,p.y));
                    float toe=.026f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.065f,p.z))*
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.035f,.065f,p.y));
                    float y=i>=soleStart&&i<soleEnd?.002f+(p.y-.011f)*.78f:
                        .002f+(p.y-.011f)*1.45f+heel-toe;
                    a.body.p[i]=V(s*.067f+(p.x-s*.065f)*1.04f,y,.0235f+(p.z-.023f)*1.55f);
                }
            }
        }

        static void ShoeUpper(Author a,int sign,int bone) {
            // An open ankle ring flows into a broad low toe and a shallow sole.
            // Eleven authored sections reuse the old ellipsoid's sampling budget.
            // The white sock remains a separate surface inside the opening.
            float[] height={.085f,.074f,.056f,.040f,.027f,.015f,.010f,.008f,.007f,.007f,.007f};
            float[] width={.037f,.043f,.049f,.049f,.047f,.046f,.045f,.038f,.028f,.015f,0};
            float[] depth={.047f,.059f,.090f,.106f,.104f,.101f,.099f,.085f,.063f,.034f,0};
            float[] center={-.015f,-.009f,.010f,.019f,.020f,.020f,.020f,.020f,.020f,.020f,.020f};
            int start=a.body.p.Count;const int sides=20;
            for(int row=0;row<height.Length;row++) {
                for(int i=0;i<=sides;i++) {
                    float angle=(i%sides)*Mathf.PI*2/sides,front=Mathf.Sin(angle);
                    float slope=row<=2?Mathf.Lerp(.008f,.006f,row/2f):0;
                    float y=height[row]-front*slope;
                    float broad=Mathf.Pow(Mathf.Max(0,front),3)*Mathf.Exp(-Mathf.Pow((row-2.5f)/1.5f,2));
                    Color color=Navy*Mathf.Lerp(.85f,1.23f,broad);color.a=1;
                    float glint=Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(angle*Mathf.Rad2Deg,sign>0?108:72)/10,2))*
                        Mathf.Exp(-Mathf.Pow((row-3)/.55f,2));
                    color=Color.Lerp(color,Hex("C8D5EC"),glint*.32f);
                    a.body.Add(V(sign*.067f+Mathf.Cos(angle)*width[row],y,center[row]+front*depth[row]),color,bone);
                }
                a.body.smoothNormalPairs.Add(new Vector2Int(start+row*(sides+1),start+row*(sides+1)+sides));
            }
            Grid(a.body,start,height.Length-1,sides);
        }

        static void BuildAccessories(Author a,int head,int chest,int hips)
        {
            // White frilled headband wraps over the crown rather than floating above it.
            int headbandStart=a.body.p.Count;
            for(int row=0;row<=4;row++)for(int i=0;i<=100;i++) {
                float angle=Mathf.Lerp(-1.34f,1.34f,i/100f),t=row/4f;
                float fold=Mathf.Cos(angle*40);
                float radius=.218f+t*(.047f+.005f*fold);
                a.body.Add(V(Mathf.Sin(angle)*radius,.765f+Mathf.Cos(angle)*radius,
                    .045f+Mathf.Sin(t*Mathf.PI)*.015f+t*fold*.006f),
                    Color.Lerp(White*.83f,White,(fold+1)*.5f),head);
            }
            Grid(a.body,headbandStart,4,100);
            // A narrow continuous binding joins the pleats to the hairline.
            // It slopes back into the existing ruffle instead of enlarging it.
            int bindingStart=a.body.p.Count;
            for(int row=0;row<=2;row++)for(int i=0;i<=100;i++) {
                float angle=Mathf.Lerp(-1.34f,1.34f,i/100f),t=row/2f;
                float radius=Mathf.Lerp(.219f,.229f,t);
                float fold=Mathf.Cos(angle*40),ruffleT=.011f/(.047f+.005f*fold);
                float joinZ=.045f+Mathf.Sin(ruffleT*Mathf.PI)*.015f+ruffleT*fold*.006f;
                float z=Mathf.Lerp(.065f,joinZ,t)+.002f*Mathf.Sin(t*Mathf.PI);
                a.body.Add(V(Mathf.Sin(angle)*radius,.765f+Mathf.Cos(angle)*radius,z),
                    Color.Lerp(White*.92f,White,t),head);
            }
            Grid(a.body,bindingStart,2,100);
            foreach(int s in new[]{-1,1}) {
                HeadFin(a,s,head);
                Bow(a,V(s*.252f,.835f,.045f),head,.050f,Hex("47A7D3"),Quaternion.Euler(0,s*48,s*-8));
            }
            ChestRibbon(a,chest);
            a.Sphere(a.body,V(0,.538f,.109f),V(.011f,.009f,.004f),Gold,chest,16,10);
            a.Sphere(a.body,V(0,.539f,.113f),V(.006f,.005f,.002f),Hex("67AACA"),chest,14,8);
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
            // Rounded authored sections retain depth from the side. The root
            // enters the hair; the tapered tip turns down and slightly forward.
            float[] xs={.168f,.200f,.230f,.267f,.304f,.330f,.350f};
            float[] ys={.735f,.748f,.734f,.713f,.691f,.679f,.677f};
            float[] zs={.028f,.028f,.029f,.033f,.039f,.043f,.044f};
            float[] ry={.042f,.052f,.053f,.044f,.023f,.009f,0};
            float[] rz={.057f,.062f,.056f,.044f,.025f,.009f,0};
            const int rows=14,cols=8;
            Func<float[],float,float> sample=(values,t)=>{
                float f=t*(values.Length-1);int k=Mathf.Min(values.Length-2,Mathf.FloorToInt(f));
                float u=f-k,d=values[k+1]-values[k];
                float before=k==0?d:values[k]-values[k-1],after=k+2==values.Length?d:values[k+2]-values[k+1];
                float m0=before*d>0?2*before*d/(before+d):0,m1=after*d>0?2*after*d/(after+d):0;
                return (2*u*u*u-3*u*u+1)*values[k]+(u*u*u-2*u*u+u)*m0+
                    (-2*u*u*u+3*u*u)*values[k+1]+(u*u*u-u*u)*m1;
            };
            int first=a.body.p.Count;
            foreach(int half in new[]{1,-1}) {
                int start=a.body.p.Count,triangles=a.body.tri.Count;
                for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++) {
                    float t=row/(float)rows,theta=(half>0?col:16-col)*Mathf.PI/cols;
                    float sy=Mathf.Sin(theta),sz=Mathf.Cos(theta);
                    float scallop=.0045f*(.5f+.5f*Mathf.Cos(theta*6))*
                        Mathf.Pow(Mathf.Max(0,-sy),2)*Mathf.Sin(t*Mathf.PI);
                    Vector3 p=V(sign*sample(xs,t),sample(ys,t)+sy*sample(ry,t)-scallop,sample(zs,t)+sz*sample(rz,t));
                    Color color=Color.Lerp(Hex("2B3D73"),Hex("3D528C"),t);
                    float gleam=Mathf.Exp(-Mathf.Pow((t-.70f)/.24f,2)-Mathf.Pow((sy-.25f)/.65f,2))*(.25f+.75f*Mathf.Max(0,-sz));
                    color=Color.Lerp(color,Hex("8193C6"),gleam*.70f);
                    float inset=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.16f,.30f,t))*
                        (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,.95f,t)))*
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,.64f,-sy))*
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.48f,sz));
                    color=Color.Lerp(color,Hex("CDD6F1"),inset);
                    a.body.Add(p,color,bone);
                }
                Grid(a.body,start,rows,cols);
                if((half>0)==(sign>0))ReverseFaces(a.body,triangles);
            }
            for(int row=0;row<=rows;row++)foreach(int col in new[]{0,cols})
                a.body.smoothNormalPairs.Add(new Vector2Int(first+row*(cols+1)+col,first+(rows+1)*(cols+1)+row*(cols+1)+col));
        }
        static void FinBlade(Author a,Vector3[] leading,Vector3[] trailing,int bone,
            Color rootColor,Color tipColor,bool paleInset,float thickness,Vector3? surfaceNormal=null)
        {
            Vector3 normal=(surfaceNormal??Vector3.forward).normalized;
            int rows=paleInset?14:12,cols=paleInset?8:4,mid=rows/2;
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
                int rows=gathered?10:12,cols=gathered?6:8;
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
