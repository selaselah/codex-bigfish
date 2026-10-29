using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesktopFish.Editor {
    public static class FishLowPoly5K {
        public const string Folder="Assets/DesktopFish/Rebuilt5K";
        [Serializable] sealed class Recipe {
            public int sourceVertices,sourceTriangles,targetTotalTriangles,preservedFaceTriangles;
            public string sourcePositionIndexSHA256,sourceAttributeSHA256,faceAttributeSHA256;
            public int[] sourceIndices,triangles;
            public int faceSourceVertices,faceSourceTriangles;
            public string faceSourcePositionIndexSHA256;
            public int[] faceSourceIndices,faceTriangles;
        }
        static string Signature(Mesh mesh) {
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)) {
                foreach(var p in mesh.vertices){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}foreach(int i in mesh.triangles)writer.Write(i);writer.Flush();
                using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2")));
            }
        }
        static string AttributeSignature(Mesh mesh){
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)){
                w.Write(mesh.vertexCount);foreach(var a in mesh.vertices){w.Write(a.x);w.Write(a.y);w.Write(a.z);}foreach(var a in mesh.normals){w.Write(a.x);w.Write(a.y);w.Write(a.z);}foreach(var a in mesh.colors){w.Write(a.r);w.Write(a.g);w.Write(a.b);w.Write(a.a);}
                for(int c=0;c<8;c++){var list=new List<Vector4>();mesh.GetUVs(c,list);w.Write(list.Count);foreach(var a in list){w.Write(a.x);w.Write(a.y);w.Write(a.z);w.Write(a.w);}}
                foreach(var a in mesh.boneWeights){w.Write(a.boneIndex0);w.Write(a.boneIndex1);w.Write(a.boneIndex2);w.Write(a.boneIndex3);w.Write(a.weight0);w.Write(a.weight1);w.Write(a.weight2);w.Write(a.weight3);}foreach(var m in mesh.bindposes)for(int i=0;i<16;i++)w.Write(m[i]);foreach(int i in mesh.triangles)w.Write(i);
                var p=new Vector3[mesh.vertexCount];var n=new Vector3[p.Length];var t=new Vector3[p.Length];for(int i=0;i<mesh.blendShapeCount;i++){w.Write(mesh.GetBlendShapeName(i));for(int f=0;f<mesh.GetBlendShapeFrameCount(i);f++){w.Write(mesh.GetBlendShapeFrameWeight(i,f));mesh.GetBlendShapeFrameVertices(i,f,p,n,t);foreach(var list in new[]{p,n,t})foreach(var a in list){w.Write(a.x);w.Write(a.y);w.Write(a.z);}}}w.Flush();using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2")));
            }
        }
        public static void FreezeSource(GameObject model){
            string path="Assets/DesktopFish/Editor/FishLowPoly5KRecipe.json";var r=JsonUtility.FromJson<Recipe>(File.ReadAllText(path));var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();string body=AttributeSignature(skins.Single(s=>s.name!="Face").sharedMesh),face=AttributeSignature(skins.Single(s=>s.name=="Face").sharedMesh);
            if(!string.IsNullOrEmpty(r.sourceAttributeSHA256)&&(r.sourceAttributeSHA256!=body||r.faceAttributeSHA256!=face))throw new InvalidOperationException("5K source attributes changed; regenerate the LOD recipe.");r.sourceAttributeSHA256=body;r.faceAttributeSHA256=face;File.WriteAllText(path,JsonUtility.ToJson(r));
        }
        public static void RestoreFullModel(GameObject model){
            foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/DesktopFish/Rebuilt/"+(skin.name=="Face"?"Face":"Body")+".asset");
        }
        public static Mesh Reduce(Mesh source,bool face=false) {
            var recipe=JsonUtility.FromJson<Recipe>(File.ReadAllText("Assets/DesktopFish/Editor/FishLowPoly5KRecipe.json"));
            if(source.vertexCount!=(face?recipe.faceSourceVertices:recipe.sourceVertices)||source.triangles.Length/3!=(face?recipe.faceSourceTriangles:recipe.sourceTriangles)||Signature(source)!=(face?recipe.faceSourcePositionIndexSHA256:recipe.sourcePositionIndexSHA256))throw new InvalidOperationException("5K recipe does not match the selected full-detail model.");
            string attributes=face?recipe.faceAttributeSHA256:recipe.sourceAttributeSHA256;if(!string.IsNullOrEmpty(attributes)&&AttributeSignature(source)!=attributes)throw new InvalidOperationException("5K source UV/color/skin/expression attributes no longer match the recipe.");
            int[] used=face?recipe.faceSourceIndices:recipe.sourceIndices;var mesh=new Mesh{name=source.name,indexFormat=IndexFormat.UInt16};
            mesh.vertices=Pick(source.vertices,used);mesh.normals=Pick(source.normals,used);mesh.colors=Pick(source.colors,used);mesh.tangents=Pick(source.tangents,used);mesh.boneWeights=Pick(source.boneWeights,used);mesh.bindposes=source.bindposes;
            for(int channel=0;channel<8;channel++){var uv=new List<Vector4>();source.GetUVs(channel,uv);if(uv.Count>0)mesh.SetUVs(channel,used.Select(i=>uv[i]).ToList());}
            mesh.triangles=face?recipe.faceTriangles:recipe.triangles;
            var p=new Vector3[source.vertexCount];var n=new Vector3[source.vertexCount];var t=new Vector3[source.vertexCount];
            for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++){
                source.GetBlendShapeFrameVertices(shape,frame,p,n,t);mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),Pick(p,used),Pick(n,used),Pick(t,used));
            }
            if(!face&&Environment.GetEnvironmentVariable("FISH_LOD_SCALP_COVERAGE")!="0")CoverRearScalp(mesh);
            mesh.RecalculateBounds();return mesh;
        }
        [Serializable] sealed class ScalpAudit {public int triangles,capTriangles,capVertices;public int[] originalLowIndices,capIndices;public float maximumOutset;}
        static void CoverRearScalp(Mesh mesh){
            // The source skull and tall rear-neck support show through behind the ears.
            // Reuse those existing faces as a scalp underlayer instead of spending more
            // triangles or changing the visible face. Boundary positions stay welded;
            // interior vertices have a 2 mm outset and retain their authored normals.
            var original=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var triangles=mesh.triangles;var selected=new bool[triangles.Length/3];var cap=new HashSet<int>();var outside=new HashSet<int>();var edges=new Dictionary<long,int>();
            var skin=mesh.colors;var originalWeights=mesh.boneWeights;
            Func<int,bool> head=i=>uv[i].x*1024>=647.999f&&uv[i].x*1024<=752.001f&&uv[i].y*2048>=783.999f&&uv[i].y*2048<=1080.001f;
            Func<int,bool> rearNeck=i=>skin[i].r==1&&Mathf.Abs(skin[i].g-240/255f)<1e-6f&&originalWeights[i].boneIndex0==3&&originalWeights[i].boneIndex1==4;
            for(int f=0;f<selected.Length;f++){
                int a=triangles[3*f],b=triangles[3*f+1],c=triangles[3*f+2];var center=(original[a]+original[b]+original[c])/3;
                float angle=Mathf.Abs(Mathf.Atan2(center.x,center.z-.005f)*Mathf.Rad2Deg);
                bool upperNeck=rearNeck(a)&&rearNeck(b)&&rearNeck(c)&&center.y>.60f;
                bool rearSkull=head(a)&&head(b)&&head(c)&&center.y>.64f&&center.y<.90f&&Mathf.Min(original[a].y,Mathf.Min(original[b].y,original[c].y))>.60f&&angle>55;
                selected[f]=upperNeck||rearSkull;
                foreach(int i in new[]{a,b,c})(selected[f]?cap:outside).Add(i);
                if(selected[f])foreach(var pair in new[]{new[]{a,b},new[]{b,c},new[]{c,a}}){long key=((long)Math.Min(pair[0],pair[1])<<32)|(uint)Math.Max(pair[0],pair[1]);edges[key]=edges.ContainsKey(key)?edges[key]+1:1;}
            }
            if(cap.Count!=80||selected.Count(s=>s)!=85)throw new InvalidOperationException("Rear-scalp coverage no longer matches the frozen skull/neck surfaces.");
            var boundary=new HashSet<int>();foreach(var pair in edges)if(pair.Value==1){boundary.Add((int)(pair.Key>>32));boundary.Add((int)pair.Key);}
            var mapping=Enumerable.Range(0,mesh.vertexCount).ToList();var target=new Dictionary<int,int>();
            foreach(int i in cap.OrderBy(i=>i)){if(outside.Contains(i)){target[i]=mapping.Count;mapping.Add(i);}else target[i]=i;}
            var positions=mapping.Select(i=>original[i]).ToArray();var ns=mapping.Select(i=>normals[i]).ToArray();var colors=mapping.Select(i=>mesh.colors[i]).ToArray();var weights=mapping.Select(i=>mesh.boneWeights[i]).ToArray();var tangents=Pick(mesh.tangents,mapping.ToArray());
            foreach(int i in cap){if(!boundary.Contains(i))positions[target[i]]+=normals[i]*.002f;colors[target[i]]=new Color(48/255f,76/255f,145/255f,1);}
            var channels=new List<Vector4>[8];for(int ch=0;ch<8;ch++){var list=new List<Vector4>();mesh.GetUVs(ch,list);if(list.Count>0){channels[ch]=mapping.Select(i=>list[i]).ToList();if(ch==0)foreach(int i in cap)channels[ch][target[i]]=new Vector4(.995f,.56f,0,0);}}
            var frames=new List<Tuple<string,float,Vector3[],Vector3[],Vector3[]>>();
            for(int shape=0;shape<mesh.blendShapeCount;shape++)for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++){
                var dp=new Vector3[original.Length];var dn=new Vector3[original.Length];var dt=new Vector3[original.Length];mesh.GetBlendShapeFrameVertices(shape,frame,dp,dn,dt);dp=Pick(dp,mapping.ToArray());dn=Pick(dn,mapping.ToArray());dt=Pick(dt,mapping.ToArray());
                // Copy every facial delta and the continuous Head/Neck attachment exactly.
                frames.Add(Tuple.Create(mesh.GetBlendShapeName(shape),mesh.GetBlendShapeFrameWeight(shape,frame),dp,dn,dt));
            }
            for(int f=0;f<selected.Length;f++)if(selected[f])for(int j=0;j<3;j++)triangles[3*f+j]=target[triangles[3*f+j]];
            var binds=mesh.bindposes;mesh.Clear(false);mesh.ClearBlendShapes();mesh.vertices=positions;mesh.normals=ns;mesh.colors=colors;mesh.tangents=tangents;mesh.boneWeights=weights;mesh.bindposes=binds;
            for(int ch=0;ch<8;ch++)if(channels[ch]!=null)mesh.SetUVs(ch,channels[ch]);mesh.triangles=triangles;foreach(var f in frames)mesh.AddBlendShapeFrame(f.Item1,f.Item2,f.Item3,f.Item4,f.Item5);
            string folder="work/scalp-cover-193";Directory.CreateDirectory(folder);File.WriteAllText(folder+"/Scalp-Coverage.json",JsonUtility.ToJson(new ScalpAudit{triangles=triangles.Length/3,capTriangles=selected.Count(s=>s),capVertices=cap.Count,originalLowIndices=mapping.ToArray(),capIndices=target.Values.OrderBy(i=>i).ToArray(),maximumOutset=.002f},true));
        }
        static T[] Pick<T>(T[] data,int[] used)=>data.Length==0?new T[0]:used.Select(i=>data[i]).ToArray();
        public static void Apply(GameObject model,bool saveAssets) {
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var reductions=skins.Select(s=>Reduce(s.sharedMesh,s.name=="Face")).ToArray();int triangles=reductions.Sum(m=>m.triangles.Length/3);
            if(triangles>5000||skins.Any(s=>s.bones.Length!=58)||reductions.Any(m=>m.blendShapeCount!=6))throw new InvalidOperationException("5K triangle/rig/expression contract failed.");
            if(saveAssets){Directory.CreateDirectory(Folder);AssetDatabase.Refresh();}
            for(int i=0;i<skins.Length;i++){
                var skin=skins[i];var reduced=reductions[i];
                if(saveAssets){string path=Folder+(skin.name=="Face"?"/Face.asset":"/Body.asset");var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){existing.Clear(false);existing.ClearBlendShapes();EditorUtility.CopySerialized(reduced,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(reduced);reduced=existing;}else AssetDatabase.CreateAsset(reduced,path);}
                skin.sharedMesh=reduced;skin.quality=SkinQuality.Bone4;
            }
            if(saveAssets){PrefabUtility.SaveAsPrefabAsset(model,Folder+"/BigFish.prefab");AssetDatabase.SaveAssets();}
            Debug.Log("5K model selected: "+triangles+" triangles; "+skins.Sum(s=>s.sharedMesh.vertexCount)+" vertices;58 joints/six expressions; eyelid/brow/mouth topology and original atlas retained.");
        }
    }
}
