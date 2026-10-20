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
            public string name; public Vector3[] vertices; public Color[] colors; public Vector2[] uv;
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
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
        }

        sealed class Author
        {
            public readonly GameObject root = new GameObject("Rebuilt BigFish");
            public readonly List<Transform> bones = new List<Transform>();
            public readonly Dictionary<string, int> ids = new Dictionary<string, int>();
            public readonly Geo body = new Geo(), face = new Geo();
            readonly List<Tuple<int, Vector3, string>> features = new List<Tuple<int, Vector3, string>>();
            readonly Dictionary<int,Vector3> closedLashes = new Dictionary<int,Vector3>();
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
                float lowerWidth = 1) {
                int start=body.p.Count; int rows=samples>0?samples:controls.Length>4?36:24;
                var path=Curve(controls,rows);
                Vector3 facing=outward??Vector3.forward;
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
                    float taper=Mathf.Pow(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(tipStart,1,t)),outward.HasValue?1.05f:.83f);
                    float rootWidth=outward.HasValue?
                        Mathf.Clamp01(Mathf.Sqrt(Mathf.Pow(path[row].x/.244f,2)+Mathf.Pow(path[row].z/.192f,2))):
                        .66f+.34f*Mathf.Sin(t*Mathf.PI*.85f);
                    float rootBlend=Mathf.Lerp(rootScale,1,Mathf.SmoothStep(0,1,t/.35f));
                    float lowerScale=Mathf.Lerp(1,lowerWidth,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.42f,t)));
                    float w=width*rootWidth*rootBlend*taper*lowerScale+.00015f;
                    float volume=Mathf.Lerp(1,fullness,Mathf.Sin(t*Mathf.PI));
                    float thickness=(cyan?.005f:.006f)*(.6f+.4f*Mathf.Sin(t*Mathf.PI))*taper*volume+.0001f;
                    for(int j=0;j<=sides;j++) {
                        float angle=j/(float)sides*Mathf.PI*2;
                        float u=Mathf.Cos(angle),front=Mathf.Sin(angle);
                        float facet=Mathf.Pow(Mathf.Abs(front),.7f)*Mathf.Sign(front);
                        Vector3 p=path[row]+side*(u*w)+thicknessAxis*facet*thickness;
                        if(outward.HasValue&&p.y>.72f) {
                            // Wrap the whole width around the scalp, not just the
                            // centerline. Flat roots otherwise read as raised plates.
                            float height=Mathf.Clamp((path[row].y-.765f)/.215f,-1,1);
                            float scalp=Mathf.Sqrt(Mathf.Max(.0001f,1-height*height));
                            float centerAngle=Mathf.Atan2(facing.x,facing.z);
                            float scalpAngle=centerAngle-u*(Mathf.PI*2-2*1.12f)/16;
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
                            if(skin>0)p.z=Mathf.Max(p.z,.166f*Mathf.Sqrt(skin)+.0045f+front*.002f);
                        }
                        // Sweep the complete section rearward below the crown.
                        // Deforming the frame with the vertices retains its width
                        // and curled tips while leaving the shared roots in place.
                        p=SweepHairSection(p,backSweep);
                        float ridge=.94f+.06f*Mathf.Pow(Mathf.Max(0,front),.6f)-.04f*Mathf.Pow(Mathf.Abs(u),6);
                        Color shade=Color.white*ridge; shade.a=1;
                        float anchored=bone==head?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.88f,p.y));
                        int paintStrip=cyan?3:((phase%3)+3)%3;
                        body.Add(p,shade,bone,new Vector2(paintStrip*.25f+.012f+(u+1)*.113f,
                            .02f+.94f*Mathf.Clamp01(t+.021f*Mathf.Sin(phase))),head,anchored);
                    }
                }
                Grid(body,start,rows,sides);
            }
            public void HairMantle(int head) {
                const int rows=16,cols=40;
                for(int layer=0;layer<1;layer++) {
                    int layerStart=body.p.Count;
                    for(int row=0;row<=rows;row++)for(int i=0;i<=cols;i++) {
                        float t=row/(float)rows,theta=Mathf.Lerp(1.10f,Mathf.PI*2-1.10f,i/(float)cols);
                        float y=.978f-.490f*t-.130f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,1,t));
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
                        float tip=Mathf.Pow(t,12)*(.018f+.040f*(.5f+.5f*Mathf.Cos(theta*11)));
                        Vector3 p=V(Mathf.Sin(theta)*(rx+.007f*fold*t),y+tip,
                            Mathf.Cos(theta)*(rz+.006f*fold*t));
                        Color color=Color.Lerp(Hair,Hex("3D619C"),t*.35f);
                        color*=1-.18f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.4f,t))*(.5f-.5f*fold);color.a=1;
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
                        Vector2 xy = center + Vector2.Scale(size, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) * r;
                        int index = g.Add(FacePoint(xy.x, xy.y, offset), color, bone);
                        if (feature != "") features.Add(Tuple.Create(index, V(center.x, center.y, offset), feature));
                    }
                }
                Grid(g,start,rings,steps);
            }
            public void FaceLine(Vector2[] xy, float radius, Color color, int bone, string feature = "", float? eyeCenter = null) {
                int start = face.p.Count;
                Tube(face, xy.Select(p => FacePoint(p.x, p.y, .0035f)).ToArray(), radius, color, bone, 6);
                if(feature=="lash") {
                    float center=eyeCenter??xy.Average(p=>p.x);
                    var closed=new Geo();
                    var path=xy.Select(p=>{
                        float y=.698f-.008f*(1-Mathf.Clamp01(Mathf.Pow((p.x-center)/.054f,2)));
                        return FacePoint(p.x,y,.0035f);
                    }).ToArray();
                    // Rebuild the closed cross-section instead of flattening every
                    // vertex onto one y value, which creates a degenerate eyelid.
                    Tube(closed,path,Mathf.Min(radius,.0021f),color,bone,6);
                    for(int i=0;i<closed.p.Count;i++)closedLashes[start+i]=closed.p[i];
                }
                if (feature != "") for (int i = start; i < face.p.Count; i++)
                    features.Add(Tuple.Create(i, V(xy.Average(p => p.x), .700f, 0), feature));
            }
            public void Iris(float x,int bone) {
                int start=face.p.Count; const int rows=8,cols=48;
                for(int row=0;row<=rows;row++)for(int j=0;j<=cols;j++) {
                    float r=row/(float)rows,t=j/(float)cols*Mathf.PI*2;
                    float px=x+Mathf.Cos(t)*.034f*r,py=.695f+Mathf.Sin(t)*.037f*r;
                    float h=Mathf.InverseLerp(.658f,.732f,py);
                    Color color=Color.Lerp(Hex("8BE6F2"),Hex("26376C"),
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.72f,h)));
                    color=Color.Lerp(color,Hex("24325D"),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.90f,1,r))*.75f);
                    int index=face.Add(FacePoint(px,py,.0027f),color,bone);
                    features.Add(Tuple.Create(index,V(x,.695f,.0027f),"iris"));
                }
                Grid(face,start,rows,cols);
            }
            public void Expressions(Mesh mesh) {
                foreach (string name in new[] { "Fcl_EYE_Close", "Fcl_ALL_Joy", "Fcl_ALL_Fun", "Fcl_ALL_Sorrow", "Fcl_ALL_Surprised" }) {
                    var delta = new Vector3[face.p.Count];
                    foreach (var feature in features) {
                        int i = feature.Item1; Vector3 p = face.p[i], c = feature.Item2;
                        string kind = feature.Item3;
                        Vector3 target = p;
                        if (name == "Fcl_EYE_Close") {
                            if (kind == "eye" || kind == "iris" || kind == "pupil" || kind == "shine")
                                target = V(c.x, .700f, FacePoint(c.x, .700f, -.025f).z);
                            if (kind == "lash") target = closedLashes[i];
                        }
                        if ((name == "Fcl_ALL_Joy" || name == "Fcl_ALL_Fun") && kind == "mouth") {
                            target.x *= 1.5f; target.y += .007f * Mathf.Pow(p.x / .018f, 2);
                        }
                        if (name == "Fcl_ALL_Sorrow" && kind == "mouth") target.y -= .008f * Mathf.Pow(p.x / .018f, 2);
                        if (name == "Fcl_ALL_Surprised" && kind == "mouth") {
                            target.x *= .5f; target.y = .625f + (p.y - .625f) * 4;
                        }
                        if (kind == "mouth")
                            target.z += FacePoint(target.x,target.y,0).z - FacePoint(p.x,p.y,0).z;
                        delta[i] = target - p;
                    }
                    mesh.AddBlendShapeFrame(name, 100, delta, new Vector3[delta.Length], new Vector3[delta.Length]);
                }
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
                    if (part == 1) Expressions(mesh);
                    var shapes = new List<ShapeDump>();
                    for(int i=0;i<mesh.blendShapeCount;i++) {
                        var deltas = new Vector3[mesh.vertexCount];
                        mesh.GetBlendShapeFrameVertices(i,0,deltas,null,null);
                        shapes.Add(new ShapeDump { name=mesh.GetBlendShapeName(i), deltas=deltas });
                    }
                    exports.Add(new MeshDump {name=mesh.name, vertices=mesh.vertices, colors=mesh.colors, uv=mesh.uv,
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
                File.WriteAllText(Folder + "/provenance.json", "{\n  \"generator\": \"FishRebuiltModel.cs\",\n  \"source_3d_assets_loaded\": false,\n  \"body_vertices\": " + body.p.Count + ",\n  \"face_vertices\": " + face.p.Count + ",\n  \"triangles\": " + (body.tri.Count + face.tri.Count) / 3 + ",\n  \"bones\": " + bones.Count + "\n}\n");
                return root;
            }
        }

        static void Grid(Geo g, int start, int rows, int cols) {
            for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++) {
                int a = start + y * (cols + 1) + x, b = a + cols + 1;
                g.Face(a, a + 1, b); g.Face(a + 1, b + 1, b);
            }
        }
        static Vector3 FacePoint(float x, float y, float offset) {
            float depth = Mathf.Sqrt(Mathf.Max(.05f, 1 - x * x / (.214f * .214f) - (y - .773f) * (y - .773f) / (.187f * .187f)));
            return V(x, y, .166f * depth + offset);
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
        // Three fringe variations and one cyan-ended strip prevent every lock
        // repeating the same highlights. The upper-right swatch stays neutral white.
        static void WriteHairPaint() {
            const int size=1024;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float ax=x/(float)(size-1),ay=y/(float)(size-1);
                int strip=Mathf.Min(3,(int)(ax*4));bool cyan=strip==3;
                float u=Mathf.Clamp01((ax-strip*.25f-.012f)/.226f);
                float t=Mathf.Clamp01((ay-.02f)/.94f);
                float edge=Mathf.Pow(Mathf.Abs(u*2-1),8);
                Color baseColor=Color.Lerp(Hex("2B4588"),Hex("4165A1"),Mathf.Sin(t*Mathf.PI)*.50f);
                float ends=Mathf.SmoothStep(0,1,Mathf.InverseLerp(cyan?.47f:.76f,1,t));
                baseColor=Color.Lerp(baseColor,cyan?Hex("59BED8"):Hex("94A8CF"),ends);
                // Slightly wandering broad streaks and finer tip strokes retain
                // strand detail without a uniform comb of vertical stripes.
                float strand=Mathf.Sin(u*43+t*3+Mathf.Sin(t*7+u*5))*.006f+
                    Mathf.Sin(u*19-t*2)*.012f;
                baseColor*=1-edge*.18f+strand;
                float glints=0;
                if(!cyan) {
                    if(strip==0) {
                        glints=Mathf.Max(PaintSpot(u,t,.29f,.34f,.050f,.051f,.25f),
                            PaintSpot(u,t,.43f,.36f,.100f,.029f,-.16f));
                        glints=Mathf.Max(glints,PaintSpot(u,t,.65f,.345f,.029f,.025f,.1f));
                    } else if(strip==1) {
                        glints=Mathf.Max(PaintSpot(u,t,.31f,.35f,.052f,.073f,.30f),
                            PaintSpot(u,t,.47f,.39f,.043f,.027f,-.2f));
                        glints=Mathf.Max(glints,PaintSpot(u,t,.73f,.36f,.022f,.018f,.1f));
                    } else {
                        glints=Mathf.Max(PaintSpot(u,t,.38f,.365f,.099f,.036f,.05f),
                            PaintSpot(u,t,.56f,.35f,.061f,.030f,.1f));
                        glints=Mathf.Max(glints,PaintSpot(u,t,.75f,.37f,.021f,.026f,-.1f));
                    }
                }
                float fineTip=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*64+t*4)),14)*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.94f,t))*(1-ends*.5f);
                baseColor=Color.Lerp(baseColor,Hex("8BA6D5"),glints*.60f);
                baseColor=Color.Lerp(baseColor,cyan?Hex("94DBE7"):Hex("CBD5E8"),fineTip*.12f);
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

        public static GameObject Create()
        {
            var a = new Author();
            int hips = a.Bone("J_Bip_C_Hips", -1, V(0, .30f, 0));
            int spine = a.Bone("J_Bip_C_Spine", hips, V(0, .39f, 0));
            int chest = a.Bone("J_Bip_C_Chest", spine, V(0, .48f, 0));
            int neck = a.Bone("J_Bip_C_Neck", chest, V(0, .55f, 0));
            int head = a.Bone("J_Bip_C_Head", neck, V(0, .773f, 0));
            a.Sphere(a.body, V(0, .773f, 0), V(.214f, .187f, .166f), Skin, head, 48, 28);
            for(int i=0;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];
                float blush=Mathf.Exp(-Mathf.Pow((Mathf.Abs(p.x)-.138f)/.041f,2)
                    -Mathf.Pow((p.y-.660f)/.025f,2))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.12f,p.z));
                a.body.colors[i]=Color.Lerp(Skin,Hex("F1BFC3"),blush*.78f);
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
            var positions=a.bones.Select(b=>compact(b.position)).ToArray();
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

        public static void RenderAngles() {
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
            // Inspect the wrist at both wave extremes and the eyelid surface from
            // an oblique view. The usual front contact sheet hides depth defects.
            var details=new Texture2D(1440,1920,TextureFormat.RGBA32,false);
            camera.transform.position=center+Quaternion.Euler(0,35,0)*(front-center);
            camera.transform.LookAt(center);
            for(int pose=0;pose<4;pose++) {
                motion.Play(pose==1||pose==2?FishMotion.Gesture.Wave:FishMotion.Gesture.None,0);
                motion.Evaluate(pose==1?1.3625f:pose==2?1.7875f:1,0,0,"idle",false);
                for(int shape=0;shape<companion.face.sharedMesh.blendShapeCount;shape++)
                    companion.face.SetBlendShapeWeight(shape,
                        companion.face.sharedMesh.GetBlendShapeName(shape)=="Fcl_EYE_Close"&&pose==3?100:0);
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

        static void BuildFace(Author a, int head)
        {
            foreach (int sign in new[] { -1, 1 }) {
                float x = sign * .085f;
                a.Disk(a.face, new Vector2(x, .700f), new Vector2(.050f, .041f), Hex("FFFCFA"), head, .0015f, "eye");
                a.Iris(x,head);
                a.Disk(a.face, new Vector2(x, .703f), new Vector2(.014f, .022f), Hex("253965"), head, .0036f, "pupil",32);
                a.Disk(a.face, new Vector2(x - .011f, .719f), new Vector2(.008f, .008f), Color.white, head, .0045f, "shine",24);
                a.Disk(a.face, new Vector2(x + .009f, .725f), new Vector2(.003f, .003f), Color.white, head, .0045f, "shine",16);
                a.Disk(a.face, new Vector2(x, .666f), new Vector2(.018f, .005f), Hex("C4F6FF"), head, .0042f, "shine",24);
                var lash = new Vector2[25];
                for (int i = 0; i < lash.Length; i++) {
                    float t = i / 24f * Mathf.PI;
                    lash[i] = new Vector2(x + Mathf.Cos(t) * .052f, .704f + Mathf.Sin(t) * .034f);
                }
                a.FaceLine(lash, .0048f, Ink, head, "lash",x);
                a.FaceLine(new[] { new Vector2(x + sign * .041f, .728f), new Vector2(x + sign * .065f, .735f) }, .0035f, Ink, head,"lash",x);
                a.FaceLine(new[] { new Vector2(x - .031f, .751f), new Vector2(x, .760f), new Vector2(x + .029f, .754f) }, .0016f, Hex("83666B"), head);
            }
            a.FaceLine(new[]{new Vector2(-.020f,.628f),new Vector2(-.009f,.625f),new Vector2(.009f,.625f),new Vector2(.020f,.628f)},.0014f,Hex("AE7C7B"),head,"mouth");
            a.Disk(a.face,new Vector2(0,.652f),new Vector2(.0015f,.0018f),Hex("E5B5A6"),head,.0018f,"",16);
        }

        static void BuildHair(Author a, int head)
        {
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
            for(int i=0;i<9;i++) {
                float theta=Mathf.Lerp(-1.12f,-Mathf.PI*2+1.12f,i/8f);
                Func<float,float,float,Vector3> radial=(rx,y,rz)=>V(Mathf.Sin(theta)*rx,y,Mathf.Cos(theta)*rz);
                int bone=a.ids["J_Sec_Hair"+(1+Mathf.RoundToInt(i*.5f))+"_01"];
                int group=Mathf.Min(i,8-i);
                float lift=new[]{.008f,.055f,.022f,.085f,.035f}[group];
                float layer=new[]{.008f,-.006f,.013f,-.010f,.006f}[group];
                float tipRadius=new[]{.300f,.275f,.320f,.265f,.290f}[group];
                float lowerWidth=new[]{.77f,.66f,.80f,.70f,.78f}[group];
                float taperAt=new[]{.64f,.59f,.67f,.58f,.65f}[group];
                var controls=new[]{radial(.008f,.979f,.006f),radial(.150f,.979f,.118f),
                    radial(.244f,.88f,.192f),radial(.244f,.735f,.192f),
                    radial(.244f,.65f,.205f),radial(.243f+layer*.25f,.56f,.225f+layer*.25f),
                    radial(.265f+layer,.48f+lift*.18f,.248f+layer),
                    radial(.325f+layer,.33f+lift*.60f,.310f+layer),
                    radial(.220f+layer,.27f+lift,.220f+layer),
                    radial(.230f+layer,.215f+lift,.215f+layer),radial(.240f+layer,.145f+lift,.225f+layer),
                    radial(tipRadius-.020f,.145f+lift,.275f+layer),
                    radial(tipRadius,.172f+lift+(i%3)*.006f,tipRadius-.010f)};
                // A sideways finish puts each tapered tip on the silhouette
                // instead of pointing every curl straight into the rear view.
                Vector3 lateral=V(Mathf.Cos(theta),0,-Mathf.Sin(theta));
                float curl=i==4?-.020f:Mathf.Sign(Mathf.Sin(theta)*Mathf.Cos(theta))*
                    new[]{0f,.012f,.025f,.020f,.020f}[group];
                controls[9]+=lateral*curl*.10f;
                controls[10]+=lateral*curl*.25f;
                controls[11]+=lateral*curl*.65f;
                controls[12]+=lateral*curl;
                a.HairPanel(controls,.067f,bone,true,i,V(Mathf.Sin(theta),0,Mathf.Cos(theta)),8,44,
                    tipStart:taperAt,backSweep:HairSweepAtAngle(theta),lowerWidth:lowerWidth);
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
                a.HairPanel(new[]{V(sign*.225f,.68f,-.04f),V(sign*.35f,.51f,.01f),
                    V(sign*.29f,.48f,.01f),V(sign*.27f,.47f,.025f),
                    V(sign*.24f,.46f,.025f),V(sign*.27f,.44f,.03f),V(sign*.30f,.43f,.03f)},.014f,bone,false,4,
                    backSweep:sign*.12f,sideTurn:sign*42,fullness:1.5f);
                a.HairPanel(new[]{V(sign*.177f,.67f,.118f),V(sign*.105f,.55f,.15f),
                    V(sign*.233f,.52f,.16f),V(sign*.155f,.444f,.16f)},.030f,bone,true,5,
                    sideTurn:sign*18,fullness:1.6f);
                a.HairPanel(new[]{V(sign*.177f,.51f,.15f),V(sign*.13f,.445f,.16f),
                    V(sign*.135f,.414f,.16f),V(sign*.162f,.390f,.15f)},.025f,bone,true,6,
                    sideTurn:sign*18,fullness:1.6f);
                // Broad cheek-framing sheets with narrow, swept tips.
                a.HairPanel(new[]{V(sign*.075f,.96f,.045f),V(sign*.235f,.94f,.12f),
                    V(sign*.225f,.67f,.16f),V(sign*.135f,.607f,.14f)},.072f,head,false,1,tipStart:.64f,rootScale:.79f);
                a.HairPanel(new[]{V(sign*.163f,.83f,.137f),V(sign*.25f,.72f,.13f),
                    V(sign*.19f,.64f,.16f),V(sign*.142f,.625f,.16f)},.022f,head,false,4);
                a.HairPanel(new[]{V(sign*.030f,.968f,.060f),V(sign*.115f,.95f,.16f),
                    V(sign*.115f,.80f,.20f),V(sign*.102f,.747f,.184f)},.056f,head,false,sign,tipStart:.74f);
            }
            a.HairPanel(new[]{V(-.012f,.978f,.055f),V(-.046f,.91f,.185f),
                V(.033f,.79f,.205f),V(-.007f,.714f,.191f)},.056f,head,false,0,tipStart:.73f,rootScale:.93f);
            a.HairPanel(new[]{V(.019f,.966f,.06f),V(.064f,.90f,.19f),
                V(.040f,.79f,.208f),V(.047f,.719f,.194f)},.029f,head,false,2,tipStart:.76f);
            a.Sweep(a.body,Curve(new[]{V(-.025f,.974f,-.02f),V(-.025f,1.11f,0),
                V(.10f,1.10f,.005f),V(.12f,1.03f,.015f)},24),
                t=>.010f*Mathf.Sin(Mathf.PI*Mathf.Lerp(.25f,1,t)),t=>.0035f,t=>Hair,head,8);
        }

        static float HairSweepAtAngle(float angle) {
            float signed=Mathf.DeltaAngle(0,angle*Mathf.Rad2Deg)*Mathf.Deg2Rad;
            float side=Mathf.Clamp01((Mathf.PI-Mathf.Abs(signed))/(Mathf.PI-1.12f));
            return Mathf.Sign(signed)*.75f*side;
        }
        static Vector3 SweepHairSection(Vector3 p,float amount) {
            float sweep=amount*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.74f,.42f,p.y));
            float c=Mathf.Cos(sweep),s=Mathf.Sin(sweep);
            return V(p.x*c+p.z*s,p.y,-p.x*s+p.z*c);
        }

        static void BuildDress(Author a, int hips, int chest)
        {
            Lathe(a,hips,.173f,.375f,t=>Mathf.Lerp(.223f,.113f,t),t=>Mathf.Lerp(.137f,.077f,t),Navy,14,72);
            Lathe(a,hips,.133f,.170f,t=>Mathf.Lerp(.214f,.222f,t),
                t=>Mathf.Lerp(.134f,.138f,t),White*.96f,2,72);
            Ruffle(a,hips,.164f,.223f,.138f,.022f,24);
            Ruffle(a,hips,.145f,.213f,.130f,.022f,24);
            Lathe(a,hips,.365f,.384f,t=>.116f,t=>.079f,Navy*.82f,2,48);
            ClothPanel(a,V(0,.463f,.080f),.071f,.063f,.009f,chest);
            foreach(int sign in new[]{-1,1}) {
                // Pleated lace straps, continuous cloth rather than bead chains.
                int first=a.body.p.Count; const int rows=40,cols=5;
                for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++) {
                    float t=y/(float)rows,r=x/(float)cols,fold=Mathf.Cos(t*Mathf.PI*14);
                    float px=sign*(.074f+.020f*r+.003f*r*fold);
                    a.body.Add(V(px,.523f-t*.121f,.081f+.006f*Mathf.Sin(r*Mathf.PI)+.004f*r*fold),
                        Color.Lerp(White*.87f,White,(fold+1)*.5f),chest);
                }
                Grid(a.body,first,rows,cols);
                Fan(a,new[]{V(sign*.004f,.578f,.074f),V(sign*.085f,.565f,.057f),
                    V(sign*.066f,.532f,.091f),V(sign*.035f,.549f,.098f)},White,chest);
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
            for(int i=0;i<3;i++)a.Sphere(a.body,V(0,.425f+i*.022f,.092f),V(.004f,.004f,.002f),Ink,chest,12,6);
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

        static void ClothPanel(Author a,Vector3 center,float rx,float ry,float bulge,int bone) {
            int start=a.body.p.Count; const int rings=6,sides=64;
            for(int row=0;row<=rings;row++)for(int i=0;i<=sides;i++) {
                float r=row/(float)rings,t=i/(float)sides*Mathf.PI*2;
                float x=Mathf.Sign(Mathf.Cos(t))*Mathf.Pow(Mathf.Abs(Mathf.Cos(t)),.55f)*rx*r;
                float y=Mathf.Sign(Mathf.Sin(t))*Mathf.Pow(Mathf.Abs(Mathf.Sin(t)),.55f)*ry*r;
                float fold=Mathf.Cos(x/rx*Mathf.PI*4);
                a.body.Add(center+V(x,y,bulge*(1-r*r)+.0015f*fold),
                    Color.Lerp(White*.89f,White,.55f+.45f*fold),bone);
            }
            Grid(a.body,start,rings,sides);
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
        static void Ruffle(Author a,int bone,float y,float rx,float rz,float width,int lobes) {
            int start=a.body.p.Count;const int sides=144,rows=5;
            for(int j=0;j<=rows;j++)for(int i=0;i<=sides;i++) {
                float theta=i/(float)sides*Mathf.PI*2,t=j/(float)rows;
                float fold=Mathf.Cos(theta*lobes),radius=1+width*t/rx*(1+.32f*fold);
                a.body.Add(V(Mathf.Sin(theta)*rx*radius,y-t*.014f+fold*.0035f*t,Mathf.Cos(theta)*rz*radius),
                    Color.Lerp(White*.87f,White,(fold+1)*.5f),bone);
            }
            Grid(a.body,start,rows,sides);
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
                    float blend=x<.18f?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.097f,.155f,x)):
                        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.212f,.258f,x));
                    Color cloth=Navy*(.98f+.025f*fold);cloth.a=1;
                    a.body.Add(V(s*x,.49f+Mathf.Cos(theta)*radius,Mathf.Sin(theta)*radius*.95f),
                        cloth,primary,null,secondary,blend);
                }
                Grid(a.body,sleeveStart,sleeveX.Length-1,sleeveSides);
                if(s<0)ReverseFaces(a.body,sleeveTriangles);
                foreach(float x in new[]{.309f,.317f}) {
                    var ring=Enumerable.Range(0,25).Select(i=>V(s*x,.49f+Mathf.Sin(i/24f*Mathf.PI*2)*.0275f,Mathf.Cos(i/24f*Mathf.PI*2)*.0275f)).ToArray();
                    a.Tube(a.body,ring,.0018f,Gold,elbow,4);
                }
                // A round wrist overlaps both the sleeve opening and the palm.
                // Keeping it on the forearm fills the joint while the hand waves.
                a.Sphere(a.body,V(s*.335f,.49f,0),V(.022f,.018f,.021f),Skin,elbow,16,10);
                a.Sphere(a.body,V(s*.354f,.49f,0),V(.024f,.015f,.024f),Skin,hand,16,10);
                string[] names={"Index","Middle","Ring","Little"};
                for(int f=0;f<4;f++) {
                    float z=(f-1.5f)*.010f;
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
                        float t=row/(float)rings,theta=j/(float)sides*Mathf.PI*2;
                        float radius=.0068f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(Mathf.Max(0,(t-.4f)/.6f),2)));
                        float x=.367f+t*(f==3?.025f:.030f);
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
                    Grid(a.body,start,rings,sides);
                }
                int thumb=a.Bone(prefix+"Thumb1",hand,V(s*.353f,.487f,.023f));
                a.Sphere(a.body,V(s*.360f,.483f,.029f),V(.014f,.010f,.010f),Skin,thumb,14,10);
                int thigh=a.Bone(prefix+"UpperLeg",hips,V(s*.065f,.27f,0));
                int knee=a.Bone(prefix+"LowerLeg",thigh,V(s*.065f,.16f,0));
                int foot=a.Bone(prefix+"Foot",knee,V(s*.065f,.062f,0));
                a.Sphere(a.body,V(s*.065f,.208f,0),V(.036f,.062f,.038f),Skin,thigh);
                a.Sphere(a.body,V(s*.065f,.112f,.003f),V(.032f,.055f,.033f),White,knee);
                a.Sphere(a.body,V(s*.065f,.045f,.022f),V(.048f,.031f,.068f),Navy,foot,20,10);
                a.Sphere(a.body,V(s*.065f,.020f,.023f),V(.049f,.009f,.069f),Ink,foot,16,6);
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
            }
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
                FinBlade(a,
                    new[]{V(s*.216f,.785f,.035f),V(s*.236f,.760f,.025f),V(s*.277f,.738f,.025f),V(s*.331f,.720f,.035f)},
                    new[]{V(s*.216f,.785f,.035f),V(s*.203f,.704f,.075f),V(s*.274f,.687f,.080f),V(s*.331f,.720f,.035f)},
                    head,Hair,Hex("3E588D"),true,.006f);
                Bow(a,V(s*.257f,.835f,.065f),head,.025f,Hex("65BDD4"));
            }
            Bow(a,V(0,.552f,.101f),chest,.060f,Navy);
            a.Sphere(a.body,V(0,.552f,.109f),V(.011f,.009f,.004f),Gold,chest,16,10);
            a.Sphere(a.body,V(0,.553f,.113f),V(.006f,.005f,.002f),Hex("67AACA"),chest,14,8);
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
        static void FinBlade(Author a,Vector3[] leading,Vector3[] trailing,int bone,
            Color rootColor,Color tipColor,bool paleInset,float thickness)
        {
            int rows=paleInset?14:12,cols=paleInset?8:4,mid=rows/2;
            var edgeA=Curve(leading,rows);var edgeB=Curve(trailing,rows);
            bool forward=Vector3.Dot(Vector3.Cross(edgeB[mid]-edgeA[mid],edgeA[mid+1]-edgeA[mid-1]),Vector3.forward)>0;
            foreach(int side in new[]{1,-1}) {
                int start=a.body.p.Count,triangles=a.body.tri.Count;
                for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++) {
                    float t=row/(float)rows,v=col/(float)cols;
                    float lens=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)*Mathf.Sin(v*Mathf.PI)),.7f);
                    Vector3 p=Vector3.Lerp(edgeA[row],edgeB[row],v)+Vector3.forward*(side*thickness*lens);
                    Color color=Color.Lerp(rootColor,tipColor,Mathf.SmoothStep(0,1,t));
                    if(paleInset&&side>0) {
                        float inset=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.36f,t))*
                            (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.73f,.94f,t)))*
                            Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.58f,v))*
                            (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1,v)));
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
        static void Bow(Author a,Vector3 center,int bone,float size,Color color) {
            foreach(int s in new[]{-1,1}) {
                int start=a.body.p.Count;const int rows=12,cols=8;
                for(int i=0;i<=rows;i++)for(int j=0;j<=cols;j++) {
                    float u=i/(float)rows,v=j/(float)cols*2-1;
                    float height=size*(.08f+.38f*Mathf.Sin(u*Mathf.PI*.78f));
                    float z=size*(.12f*Mathf.Sin(u*Mathf.PI)+.08f*(1-v*v));
                    Color shaded=Color.Lerp(color*.72f,color*1.17f,(v+1)*.5f);shaded.a=1;
                    a.body.Add(center+V(s*size*u,v*height,z),shaded,bone);
                }
                Grid(a.body,start,rows,cols);
                Fan(a,new[]{center+V(s*size*.05f,-size*.08f,0),center+V(s*size*.62f,-size*.83f,.001f),
                    center+V(s*size*.32f,-size*.72f,.003f),center+V(s*size*.12f,-size*.88f,.004f)},color,bone);
            }
            a.Sphere(a.body,center+V(0,0,.002f),V(size*.17f,size*.20f,size*.13f),color,bone,12,8);
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
