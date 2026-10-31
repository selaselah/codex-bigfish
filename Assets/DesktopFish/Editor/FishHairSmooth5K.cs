using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace DesktopFish.Editor {
    // Retopology of the existing own-source crown, with exact lower seam anchors. The
    // immutable input signature prevents application to a different LOD.
    public static class FishHairSmooth5K {
        [Serializable] sealed class Skin {public int[] indices;public float[] weights;}
        [Serializable] sealed class Recipe {
            public int sourceVertexCount;
            public string sourcePositionIndexSHA256;
            public int[] surfaceIndices,beforeSurfaceIndices,removeFaces,triangles,boundarySourceIndices,boundaryOutputIndices;
            public Vector3[] positions,normals;
            public Vector2[] uv;
            public Color[] colors;
            public Skin[] skin4;
        }
        static string RecipePath=>Environment.GetEnvironmentVariable("FISH_HAIR_SMOOTH_RECIPE")??"Assets/DesktopFish/Editor/FishHairSmooth5K.json";
        public static void Apply(Mesh mesh) {
            var data=JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath));
            var p=mesh.vertices;var n=mesh.normals;
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)) {
                foreach(var q in p){w.Write(q.x);w.Write(q.y);w.Write(q.z);}foreach(int i in mesh.triangles)w.Write(i);w.Flush();
                using(var sha=SHA256.Create())if(string.Concat(sha.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2")))!=data.sourcePositionIndexSHA256)
                    throw new InvalidOperationException("Hair fairing input no longer matches selected195 geometry.");
            }
            Replace(mesh,data);
        }
        static void Replace(Mesh mesh,Recipe data) {
            int oldCount=mesh.vertexCount,count=data.positions.Length;var oldTriangles=mesh.triangles;
            if(oldCount!=data.sourceVertexCount||data.normals.Length!=count||data.uv.Length!=count||data.colors.Length!=count||data.skin4.Length!=count||data.removeFaces.Distinct().Count()!=data.removeFaces.Length||data.triangles.Length%3!=0||oldTriangles.Length/3-data.removeFaces.Length+data.triangles.Length/3+612>5000)
                throw new InvalidOperationException("Hair replacement budget/streams changed.");
            var removed=data.removeFaces.ToHashSet();
            if(removed.Any(i=>i<0||3*i>=oldTriangles.Length)||data.triangles.Any(i=>i<0||i>=count))throw new InvalidOperationException("Hair replacement index invalid.");
            var triangles=Enumerable.Range(0,oldTriangles.Length/3).Where(i=>!removed.Contains(i)).SelectMany(i=>new[]{oldTriangles[3*i],oldTriangles[3*i+1],oldTriangles[3*i+2]}).Concat(data.triangles.Select(i=>i+oldCount)).ToArray();
            var used=triangles.Distinct().OrderBy(i=>i).ToArray();var map=new int[oldCount+count];for(int i=0;i<used.Length;i++)map[used[i]]=i;
            var p=mesh.vertices.Concat(data.positions).ToArray();var n=mesh.normals.Concat(data.normals).ToArray();var uv=mesh.uv.Concat(data.uv).ToArray();var colors=mesh.colors.Concat(data.colors).ToArray();
            var newWeights=data.skin4.Select(s=>{
                if(s.indices.Length!=4||s.weights.Length!=4||s.indices.Any(i=>i<0||i>=58)||s.weights.Any(a=>a<0||a>1)||Mathf.Abs(s.weights.Sum()-1)>1e-6f)throw new InvalidOperationException("Hair four-slot skin invalid.");
                return new BoneWeight{boneIndex0=s.indices[0],boneIndex1=s.indices[1],boneIndex2=s.indices[2],boneIndex3=s.indices[3],weight0=s.weights[0],weight1=s.weights[1],weight2=s.weights[2],weight3=s.weights[3]};
            });
            var weights=mesh.boneWeights.Concat(newWeights).ToArray();var bindposes=mesh.bindposes;
            if(data.boundarySourceIndices==null||data.boundarySourceIndices.Length!=44||data.boundaryOutputIndices.Length!=44)throw new InvalidOperationException("Crown seam contract changed.");
            for(int j=0;j<44;j++) {
                int a=data.boundarySourceIndices[j],b=oldCount+data.boundaryOutputIndices[j];
                if(!p[a].Equals(p[b])||!n[a].Equals(n[b])||!uv[a].Equals(uv[b])||!colors[a].Equals(colors[b])||!weights[a].Equals(weights[b]))throw new InvalidOperationException("Crown lower seam attributes changed.");
            }
            for(int f=0;f<data.triangles.Length;f+=3) {
                int a=data.triangles[f]+oldCount,b=data.triangles[f+1]+oldCount,c=data.triangles[f+2]+oldCount;Vector3 cross=Vector3.Cross(p[b]-p[a],p[c]-p[a]);
                if(cross.sqrMagnitude<=1e-18f||Vector3.Dot(cross,n[a]+n[b]+n[c]) < -1e-16f)throw new InvalidOperationException("New crown triangle degenerate/opposed.");
            }
            if(p.Any(a=>float.IsNaN(a.x)||float.IsInfinity(a.x)||float.IsNaN(a.y)||float.IsInfinity(a.y)||float.IsNaN(a.z)||float.IsInfinity(a.z))||n.Any(a=>Mathf.Abs(a.magnitude-1)>.001f))throw new InvalidOperationException("Hair geometry/normal invalid.");
            var morphs=new System.Collections.Generic.List<Tuple<string,float,Vector3[],Vector3[],Vector3[]>>();
            var oldScope=removed.SelectMany(f=>new[]{oldTriangles[3*f],oldTriangles[3*f+1],oldTriangles[3*f+2]}).Distinct().ToArray();
            for(int s=0;s<mesh.blendShapeCount;s++)for(int f=0;f<mesh.GetBlendShapeFrameCount(s);f++) {
                var dp=new Vector3[oldCount+count];var dn=new Vector3[oldCount+count];var dt=new Vector3[oldCount+count];var a=new Vector3[oldCount];var b=new Vector3[oldCount];var c=new Vector3[oldCount];mesh.GetBlendShapeFrameVertices(s,f,a,b,c);
                if(oldScope.Any(i=>a[i]!=Vector3.zero||b[i]!=Vector3.zero||c[i]!=Vector3.zero))throw new InvalidOperationException("Replaced hair contains protected expression attachment.");
                Array.Copy(a,dp,oldCount);Array.Copy(b,dn,oldCount);Array.Copy(c,dt,oldCount);
                morphs.Add(Tuple.Create(mesh.GetBlendShapeName(s),mesh.GetBlendShapeFrameWeight(s,f),used.Select(i=>dp[i]).ToArray(),used.Select(i=>dn[i]).ToArray(),used.Select(i=>dt[i]).ToArray()));
            }
            for(int channel=1;channel<8;channel++){var values=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(channel,values);if(values.Count>0)throw new InvalidOperationException("Hair replacement must explicitly preserve added UV streams.");}
            if(mesh.tangents.Length>0)throw new InvalidOperationException("Hair replacement requires explicit tangent transport.");
            mesh.Clear(false);mesh.ClearBlendShapes();mesh.vertices=used.Select(i=>p[i]).ToArray();mesh.normals=used.Select(i=>n[i]).ToArray();mesh.uv=used.Select(i=>uv[i]).ToArray();mesh.colors=used.Select(i=>colors[i]).ToArray();mesh.boneWeights=used.Select(i=>weights[i]).ToArray();mesh.bindposes=bindposes;mesh.triangles=triangles.Select(i=>map[i]).ToArray();
            foreach(var m in morphs)mesh.AddBlendShapeFrame(m.Item1,m.Item2,m.Item3,m.Item4,m.Item5);
            mesh.RecalculateBounds();
        }
        public static void RenderStudy() {
            string folder=Environment.GetEnvironmentVariable("FISH_HAIR_REVIEW")??"work/hair-smooth-212/study1";
            Directory.CreateDirectory(folder);
            var recipe=JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath));
            for(int version=0;version<2;version++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
                var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();
                FishLowPoly5K.RestoreFullModel(c.model.gameObject);FishLowPoly5K.Apply(c.model.gameObject,false,false);
                var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();var body=skins.Single(s=>s.sharedMesh.name=="Body");
                if(version==1)Apply(body.sharedMesh);
                int expected=4999+(version==1&&recipe.removeFaces!=null?recipe.triangles.Length/3-recipe.removeFaces.Length:0);
                if(expected>5000||skins.Sum(s=>s.sharedMesh.triangles.Length/3)!=expected)throw new InvalidOperationException("Hair study changed triangle budget.");
                var motion=new FishMotion(c.model){randomActions=false};motion.Tick(1,1,0,0,"idle",false);FishBuild.ApplyMotionExpressions(c,motion);
                var bakes=new Mesh[skins.Length];var renderers=new MeshRenderer[skins.Length];
                for(int i=0;i<skins.Length;i++) {
                    bakes[i]=new Mesh();skins[i].BakeMesh(bakes[i]);
                    var go=new GameObject("Hair curve study snapshot");go.transform.SetParent(skins[i].transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=bakes[i];renderers[i]=go.AddComponent<MeshRenderer>();renderers[i].sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;
                }
                var camera=c.view;camera.backgroundColor=Color.white;
                Vector3 front=camera.transform.position,center=front+camera.transform.forward*3;float size=camera.orthographicSize;
                string prefix=folder+"/"+(version==0?"before":"after");
                Action<string,int,int> capture=(name,width,height)=>{
                    var target=new RenderTexture(width,height,24){antiAliasing=4};var image=new Texture2D(width,height,TextureFormat.RGBA32,false);
                    camera.targetTexture=target;camera.aspect=width/(float)height;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                    File.WriteAllBytes(prefix+"-"+name+".png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
                };
                foreach(int angle in new[]{0,35,90,180,-35,-90,999}) {
                    camera.orthographicSize=size;camera.transform.position=center+(angle==999?Quaternion.Euler(-16,0,0):Quaternion.Euler(0,angle,0))*(front-center);camera.transform.LookAt(center);
                    string a=angle==999?"top":angle.ToString();capture("default-"+a,720,960);capture("small-"+a,180,240);
                }
                Vector3 headCenter=new Vector3(0,.68f,-.08f);camera.orthographicSize=.43f;
                foreach(int angle in new[]{0,35,90,180}) {
                    camera.transform.position=headCenter+Quaternion.Euler(0,angle,0)*c.model.forward*3;camera.transform.LookAt(headCenter);capture("hair-"+angle,720,720);
                }
                var isolated=UnityEngine.Object.Instantiate(bakes[Array.IndexOf(skins,body)]);var scope=(version==0&&recipe.beforeSurfaceIndices!=null?recipe.beforeSurfaceIndices:recipe.surfaceIndices).ToHashSet();var indices=isolated.triangles;
                isolated.triangles=Enumerable.Range(0,indices.Length/3).Where(f=>scope.Contains(indices[3*f])&&scope.Contains(indices[3*f+1])&&scope.Contains(indices[3*f+2])).SelectMany(f=>new[]{indices[3*f],indices[3*f+1],indices[3*f+2]}).ToArray();
                isolated.colors=Enumerable.Repeat(new Color(.58f,.60f,.64f,1),isolated.vertexCount).ToArray();
                foreach(var renderer in renderers)renderer.enabled=false;
                int bodyIndex=Array.IndexOf(skins,body);renderers[bodyIndex].GetComponent<MeshFilter>().sharedMesh=isolated;renderers[bodyIndex].enabled=true;
                var gray=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));gray.mainTexture=Texture2D.whiteTexture;gray.SetFloat("_OutlineWidth",0);renderers[bodyIndex].sharedMaterial=gray;
                foreach(int angle in new[]{0,35,90,180}) {
                    camera.transform.position=headCenter+Quaternion.Euler(0,angle,0)*c.model.forward*3;camera.transform.LookAt(headCenter);capture("gray-"+angle,720,720);
                }
                UnityEngine.Object.DestroyImmediate(isolated);UnityEngine.Object.DestroyImmediate(gray);foreach(var m in bakes)UnityEngine.Object.DestroyImmediate(m);
            }
            Debug.Log("Hair curve study passed: <=5000tri; paired default7/small7/hair4/gray4 per version; scene not saved.");
        }
    }
}
