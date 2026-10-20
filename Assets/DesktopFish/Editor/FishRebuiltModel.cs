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
                Vector3? outward = null, int sides = 12, int samples = 0) {
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
                    Vector3 side=Vector3.Cross(tangent,facing);
                    if(side.sqrMagnitude<.001f)side=Vector3.ProjectOnPlane(previousSide,tangent);
                    if(side.sqrMagnitude<.001f)side=Vector3.right;
                    side.Normalize();
                    if(previousSide.sqrMagnitude>.1f&&Vector3.Dot(side,previousSide)<0)side=-side;
                    previousSide=side;
                    Vector3 thicknessAxis=Vector3.Cross(side,tangent).normalized;
                    float taper=Mathf.Pow(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,1,t)),.83f);
                    float rootWidth=outward.HasValue?
                        Mathf.Clamp01(Mathf.Sqrt(Mathf.Pow(path[row].x/.244f,2)+Mathf.Pow(path[row].z/.192f,2))):
                        .66f+.34f*Mathf.Sin(t*Mathf.PI*.85f);
                    float w=width*rootWidth*taper+.00015f;
                    float thickness=(cyan?.005f:.006f)*(.6f+.4f*Mathf.Sin(t*Mathf.PI))*taper+.0001f;
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
                        float ridge=.94f+.06f*Mathf.Pow(Mathf.Max(0,front),.6f)-.04f*Mathf.Pow(Mathf.Abs(u),6);
                        Color shade=Color.white*ridge; shade.a=1;
                        int head=ids["J_Bip_C_Head"];
                        float anchored=bone==head?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.88f,p.y));
                        body.Add(p,shade,bone,new Vector2((cyan?.51f:.01f)+(u+1)*.235f,
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
                        float y=.978f-.490f*t;
                        float scalp=Mathf.Sqrt(Mathf.Max(.01f,1-Mathf.Pow((y-.765f)/.215f,2)));
                        float lower=Mathf.InverseLerp(.765f,.488f,y);
                        float rx=y>.765f?.232f*scalp+.002f:.234f-.030f*Mathf.SmoothStep(0,1,lower);
                        float rz=y>.765f?.182f*scalp+.002f:.184f+.024f*lower;
                        float undercut=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,.75f,y))*.08f;
                        rx*=1-undercut;rz*=1-undercut;
                        float fold=Mathf.Cos(theta*12+t*.6f),thickness=layer*.005f;
                        rx-=thickness;rz-=thickness;
                        float tip=Mathf.Pow(t,12)*(.018f+.040f*(.5f+.5f*Mathf.Cos(theta*11)));
                        Vector3 p=V(Mathf.Sin(theta)*(rx+.007f*fold*t),y+tip,
                            Mathf.Cos(theta)*(rz+.006f*fold*t));
                        Color color=Color.Lerp(Hair,Hex("3D619C"),t*.35f);
                        color*=1-.18f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.4f,t))*(.5f-.5f*fold);color.a=1;
                        body.Add(p,color,head);
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
        // The two strips are fringe / cyan ends. The upper-right swatch is neutral
        // white for all untextured cloth, skin, eyes and accessories.
        static void WriteHairPaint() {
            const int size=1024;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float ax=x/(float)(size-1),ay=y/(float)(size-1);
                bool cyan=ax>.5f;
                float u=Mathf.Clamp01((ax-(cyan?.51f:.01f))/.47f);
                float t=Mathf.Clamp01((ay-.02f)/.94f);
                float edge=Mathf.Pow(Mathf.Abs(u*2-1),8);
                Color baseColor=Color.Lerp(Hex("2B4588"),Hex("4165A1"),Mathf.Sin(t*Mathf.PI)*.50f);
                float ends=Mathf.SmoothStep(0,1,Mathf.InverseLerp(cyan?.47f:.76f,1,t));
                baseColor=Color.Lerp(baseColor,cyan?Hex("59BED8"):Hex("94A8CF"),ends);
                // Long narrow strokes merge into broad values when viewed at pet size.
                float strand=Mathf.Sin(u*83+t*2)*.008f+Mathf.Sin(u*39-t*3)*.014f;
                baseColor*=1-edge*.18f+strand;
                float glints=0;
                if(!cyan)for(int j=0;j<3;j++) {
                    float center=.20f+j*.25f;
                    float along=.36f+.032f*Mathf.Sin(j*2.7f)+.035f*(u-center);
                    glints+=Mathf.Exp(-Mathf.Pow((u-center)/(.048f+j*.006f),4)
                        -Mathf.Pow((t-along)/(.026f+j*.006f),4));
                }
                float fineTip=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*64+t*4)),14)*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.94f,t))*(1-ends*.5f);
                baseColor=Color.Lerp(baseColor,Hex("A0BBE4"),Mathf.Clamp01(glints)*.47f);
                baseColor=Color.Lerp(baseColor,cyan?Hex("94DBE7"):Hex("CBD5E8"),fineTip*.12f);
                baseColor.a=1;
                pixels[y*size+x]=(ax>.98f&&ay>.98f)?Color.white:baseColor;
            }
            texture.SetPixels(pixels);texture.Apply();
            File.WriteAllBytes(Folder+"/HairPaint.png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
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
            camera.targetTexture=null;RenderTexture.active=null;
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);
            UnityEngine.Object.DestroyImmediate(details);
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
                var controls=new[]{radial(.008f,.979f,.006f),radial(.150f,.979f,.118f),
                    radial(.244f,.88f,.192f),radial(.244f,.735f,.192f),
                    radial(.244f,.65f,.205f),radial(.243f,.56f,.225f),radial(.265f,.48f,.248f),
                    radial(.325f,.33f,.310f),radial(.220f,.27f,.220f),
                    radial(.230f,.215f,.215f),radial(.240f,.145f,.225f),
                    radial(.280f,.145f,.275f),radial(.300f,.172f+(i%3)*.009f,.290f)};
                a.HairPanel(controls,.067f,bone,true,i,V(Mathf.Sin(theta),0,Mathf.Cos(theta)),8,44);
            }
            foreach(int sign in new[]{-1,1}) {
                int bone=a.ids[sign<0?"J_Sec_Hair1_01":"J_Sec_Hair5_01"];
                // Separate long S-shaped locks leave negative space at the hem.
                a.HairPanel(new[]{V(sign*.17f,.90f,-.045f),V(sign*.27f,.73f,-.025f),
                    V(sign*.22f,.59f,.005f),V(sign*.245f,.45f,.02f),
                    V(sign*.28f,.31f,.035f),V(sign*.17f,.21f,.055f),V(sign*.29f,.17f,.06f)},.057f,bone,true,2);
                a.HairPanel(new[]{V(sign*.21f,.79f,-.04f),V(sign*.28f,.64f,-.005f),
                    V(sign*.28f,.47f,.025f),V(sign*.31f,.36f,.025f),
                    V(sign*.39f,.22f,.035f),V(sign*.22f,.28f,.055f),V(sign*.22f,.16f,.06f)},.031f,bone,true,3);
                a.HairPanel(new[]{V(sign*.225f,.68f,-.04f),V(sign*.35f,.51f,.01f),
                    V(sign*.29f,.48f,.01f),V(sign*.27f,.47f,.025f),
                    V(sign*.24f,.46f,.025f),V(sign*.27f,.44f,.03f),V(sign*.30f,.43f,.03f)},.014f,bone,false,4);
                a.HairPanel(new[]{V(sign*.177f,.67f,.118f),V(sign*.105f,.55f,.15f),
                    V(sign*.233f,.52f,.16f),V(sign*.155f,.444f,.16f)},.030f,bone,true,5);
                a.HairPanel(new[]{V(sign*.177f,.51f,.15f),V(sign*.13f,.445f,.16f),
                    V(sign*.135f,.414f,.16f),V(sign*.162f,.390f,.15f)},.025f,bone,true,6);
                // Broad cheek-framing sheets with narrow, swept tips.
                a.HairPanel(new[]{V(sign*.075f,.96f,.045f),V(sign*.235f,.94f,.12f),
                    V(sign*.225f,.67f,.16f),V(sign*.135f,.607f,.14f)},.057f,head,false,1);
                a.HairPanel(new[]{V(sign*.163f,.83f,.137f),V(sign*.25f,.72f,.13f),
                    V(sign*.19f,.64f,.16f),V(sign*.142f,.625f,.16f)},.022f,head,false,4);
                a.HairPanel(new[]{V(sign*.030f,.968f,.060f),V(sign*.115f,.95f,.16f),
                    V(sign*.136f,.80f,.20f),V(sign*.132f,.736f,.18f)},.060f,head,false,sign);
            }
            a.HairPanel(new[]{V(-.012f,.978f,.055f),V(-.046f,.91f,.185f),
                V(.033f,.79f,.205f),V(.009f,.715f,.184f)},.052f,head,false,0);
            a.HairPanel(new[]{V(.019f,.966f,.06f),V(.064f,.90f,.19f),
                V(.040f,.79f,.208f),V(.045f,.712f,.19f)},.029f,head,false,2);
            a.Sweep(a.body,Curve(new[]{V(-.025f,.974f,-.02f),V(-.025f,1.11f,0),
                V(.10f,1.10f,.005f),V(.12f,1.03f,.015f)},24),
                t=>.010f*Mathf.Sin(Mathf.PI*Mathf.Lerp(.25f,1,t)),t=>.0035f,t=>Hair,head,8);
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
                GoldBow(a,V(sign*.142f,.211f,.102f),hips,.022f);
                foreach(float x in new[]{sign*.089f,sign*.191f})
                    Star(a,V(x,.207f,.13f*Mathf.Sqrt(1-x*x/(.223f*.223f))+.003f),.0055f,hips);
            }
            for(int i=0;i<3;i++)a.Sphere(a.body,V(0,.425f+i*.022f,.092f),V(.004f,.004f,.002f),Ink,chest,12,6);
            a.Sphere(a.body,V(0,.376f,.083f),V(.006f,.003f,.002f),Gold,hips,12,6);
            foreach(float y in new[]{.185f,.194f}) {
                float t=(y-.173f)/(.375f-.173f),rx=Mathf.Lerp(.223f,.113f,t),rz=Mathf.Lerp(.137f,.077f,t);
                var ring=Enumerable.Range(0,145).Select(i=>{
                    float theta=i/144f*Mathf.PI*2,pleat=1+.045f*Mathf.Cos(theta*12)*(1-t);
                    return V(Mathf.Sin(theta)*(rx*pleat+.002f),y,Mathf.Cos(theta)*(rz*pleat+.002f));
                }).ToArray();
                a.Tube(a.body,ring,.0018f,Gold,hips,4);
            }
            // The apron is a shallow fabric panel, surrounded by folded lace.
            int apronStart=a.body.p.Count;
            a.Sphere(a.body,V(0,.286f,.124f),V(.092f,.088f,.012f),White,hips,32,16);
            int laceStart=a.body.p.Count; const int laceSides=144,laceRows=4;
            for(int row=0;row<=laceRows;row++)for(int i=0;i<=laceSides;i++) {
                float t=i/(float)laceSides*Mathf.PI*2,r=row/(float)laceRows;
                float fold=Mathf.Cos(t*24),scale=1+r*(.17f+.035f*fold);
                a.body.Add(V(Mathf.Sin(t)*.091f*scale,.286f+Mathf.Cos(t)*.087f*scale,
                    .126f+Mathf.Sin(r*Mathf.PI)*.009f+fold*r*.005f),
                    Color.Lerp(White*.87f,White,(fold+1)*.5f),hips);
            }
            Grid(a.body,laceStart,laceRows,laceSides);
            // Large, simple whale silhouette and belly crescent.
            int whaleStart=a.body.p.Count,whaleTriangles=a.body.tri.Count;
            Color blue=Hex("577FAE");
            a.Sphere(a.body,V(-.014f,.281f,.139f),V(.046f,.030f,.0025f),blue,hips,24,12);
            a.Sphere(a.body,V(-.027f,.268f,.142f),V(.030f,.012f,.001f),White,hips,20,8);
            a.Tube(a.body,Curve(new[]{V(.015f,.276f,.140f),V(.040f,.278f,.141f),
                V(.038f,.309f,.143f),V(.050f,.317f,.142f)},20),.0085f,blue,hips);
            foreach(int sign in new[]{-1,1})a.Sphere(a.body,V(.050f+sign*.010f,.319f,.142f),V(.013f,.008f,.002f),blue,hips,16,8);
            a.Sphere(a.body,V(-.040f,.286f,.143f),V(.0022f,.0022f,.001f),White,hips,10,6);
            foreach(int sign in new[]{-1,1})a.Tube(a.body,Curve(new[]{V(-.018f,.315f,.140f),
                V(-.020f,.327f,.140f),V(-.018f+sign*.009f,.334f,.140f),V(-.018f+sign*.012f,.326f,.140f)},12),.002f,blue,hips);
            for(int i=whaleStart;i<a.body.p.Count;i++) {
                Vector3 p=a.body.p[i];p.x=-p.x;a.body.p[i]=p;
            }
            ReverseFaces(a.body,whaleTriangles);
            for(int i=apronStart;i<a.body.p.Count;i++)a.body.p[i]+=Vector3.forward*.012f;
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
                a.Sweep(a.body,new[]{V(s*.13f,.49f,0),V(s*.18f,.49f,0),V(s*.235f,.49f,0)},
                    t=>Mathf.Lerp(.042f,.032f,t),t=>Mathf.Lerp(.039f,.031f,t),t=>Navy,upper,16);
                a.Sweep(a.body,new[]{V(s*.232f,.49f,0),V(s*.28f,.49f,0),V(s*.325f,.49f,0)},
                    t=>Mathf.Lerp(.034f,.027f,t),t=>Mathf.Lerp(.033f,.027f,t),t=>Navy,elbow,16);
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
                        int joint=Mathf.Min(2,Mathf.FloorToInt(t*3));
                        a.body.Add(V(s*(.367f+t*(f==3?.025f:.030f)),.49f+Mathf.Cos(theta)*radius,z+Mathf.Sin(theta)*radius),
                            Skin,joints[joint]);
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
                float radius=.218f+t*(.060f+.006f*fold);
                a.body.Add(V(Mathf.Sin(angle)*radius,.765f+Mathf.Cos(angle)*radius,
                    .045f+Mathf.Sin(t*Mathf.PI)*.015f+t*fold*.006f),
                    Color.Lerp(White*.83f,White,(fold+1)*.5f),head);
            }
            Grid(a.body,headbandStart,4,100);
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
            int start=a.body.p.Count;
            foreach(int s in new[]{-1,1}) {
                a.Tube(a.body,Curve(new[]{c,c+V(s*size*1.3f,size*1.7f,0),c+V(s*size*1.7f,-size*.1f,0),c},12),.0018f,Gold,bone,4);
                a.Tube(a.body,new[]{c,c+V(s*size*.5f,-size*.7f,.001f),c+V(s*size*.8f,-size*.8f,.001f)},.0017f,Gold,bone,6);
            }
            for(int i=start;i<a.body.p.Count;i++)a.body.p[i]=OnSkirt(a.body.p[i]);
        }
        static Vector3 OnSkirt(Vector3 p) {
            float t=Mathf.InverseLerp(.173f,.375f,p.y),rx=Mathf.Lerp(.223f,.113f,t),rz=Mathf.Lerp(.137f,.077f,t);
            float theta=Mathf.Asin(Mathf.Clamp(p.x/rx,-.99f,.99f));
            float pleat=1+.045f*Mathf.Cos(theta*12)*(1-t);
            p.z=rz*pleat*Mathf.Cos(theta)+.004f;
            return p;
        }
        static void Star(Author a,Vector3 c,float size,int bone) {
            int start=a.body.p.Count;
            Fan(a,new[]{c+V(0,size,0),c+V(size*.35f,size*.25f,0),c+V(size,0,0),c+V(size*.25f,-size*.3f,0),c+V(0,-size,0),c+V(-size*.25f,-size*.3f,0),c+V(-size,0,0),c+V(-size*.35f,size*.25f,0)},Gold,bone);
            for(int i=start;i<a.body.p.Count;i++)a.body.p[i]=OnSkirt(a.body.p[i]);
        }
    }
}
