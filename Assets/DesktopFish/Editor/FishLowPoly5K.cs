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
            mesh.RecalculateBounds();return mesh;
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
