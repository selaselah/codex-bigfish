using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace DesktopFish.Editor {
    public static class FishRearAlphaCards {
        [Serializable] sealed class Cards {public string bodyBeforePositionIndexSHA256;public int[] removeTriangles,triangles;public Vector3[] vertices,normals;public Vector2[] uv;}
        static Cards Read()=>JsonUtility.FromJson<Cards>(File.ReadAllText("Assets/DesktopFish/Editor/FishRearAlphaCards.json"));
        public static void RemoveOldUnderlayers(Mesh mesh){
            var data=Read();using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)){
                foreach(var p in mesh.vertices){w.Write(p.x);w.Write(p.y);w.Write(p.z);}foreach(int i in mesh.triangles)w.Write(i);w.Flush();using(var sha=SHA256.Create())if(string.Concat(sha.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2")))!=data.bodyBeforePositionIndexSHA256)throw new InvalidOperationException("Rear cards no longer match the selected scalp/LOD surface.");
            }
            var remove=new HashSet<int>(data.removeTriangles);if(remove.Count!=61)throw new InvalidOperationException("Rear card replacement scope changed.");
            var triangles=mesh.triangles.Where((i,index)=>!remove.Contains(index/3)).ToArray();var used=triangles.Distinct().OrderBy(i=>i).ToArray();var map=new int[mesh.vertexCount];for(int i=0;i<used.Length;i++)map[used[i]]=i;
            var copy=UnityEngine.Object.Instantiate(mesh);var positions=copy.vertices;var normals=copy.normals;var colors=copy.colors;var weights=copy.boneWeights;var tangents=copy.tangents;mesh.Clear(false);mesh.ClearBlendShapes();mesh.vertices=used.Select(i=>positions[i]).ToArray();mesh.normals=used.Select(i=>normals[i]).ToArray();mesh.colors=used.Select(i=>colors[i]).ToArray();mesh.boneWeights=used.Select(i=>weights[i]).ToArray();mesh.bindposes=copy.bindposes;if(tangents.Length>0)mesh.tangents=used.Select(i=>tangents[i]).ToArray();
            for(int ch=0;ch<8;ch++){var uv=new List<Vector4>();copy.GetUVs(ch,uv);if(uv.Count>0)mesh.SetUVs(ch,used.Select(i=>uv[i]).ToList());}
            for(int shape=0;shape<copy.blendShapeCount;shape++)for(int frame=0;frame<copy.GetBlendShapeFrameCount(shape);frame++){
                var p=new Vector3[copy.vertexCount];var n=new Vector3[p.Length];var t=new Vector3[p.Length];copy.GetBlendShapeFrameVertices(shape,frame,p,n,t);mesh.AddBlendShapeFrame(copy.GetBlendShapeName(shape),copy.GetBlendShapeFrameWeight(shape,frame),used.Select(i=>p[i]).ToArray(),used.Select(i=>n[i]).ToArray(),used.Select(i=>t[i]).ToArray());
            }
            mesh.triangles=triangles.Select(i=>map[i]).ToArray();mesh.RecalculateBounds();UnityEngine.Object.DestroyImmediate(copy);
            Directory.CreateDirectory("work/hair-cards-194");File.WriteAllText("work/hair-cards-194/Retained-Body-Indices.json","["+string.Join(",",used)+"]");
        }
        public static Mesh Create(Mesh original){
            var data=Read();var mesh=new Mesh{name="RearHairCards"};mesh.vertices=data.vertices;mesh.normals=data.normals;mesh.uv=data.uv;mesh.colors=Enumerable.Repeat(Color.white,data.vertices.Length).ToArray();mesh.boneWeights=Enumerable.Repeat(new BoneWeight{boneIndex0=4,weight0=1},data.vertices.Length).ToArray();mesh.bindposes=original.bindposes;mesh.triangles=data.triangles;
            for(int i=0;i<original.blendShapeCount;i++)mesh.AddBlendShapeFrame(original.GetBlendShapeName(i),100,new Vector3[mesh.vertexCount],new Vector3[mesh.vertexCount],new Vector3[mesh.vertexCount]);
            if(mesh.triangles.Length/3!=48||mesh.vertexCount!=45)throw new InvalidOperationException("Unexpected curved card budget.");mesh.RecalculateBounds();return mesh;
        }
        public static void Attach(SkinnedMeshRenderer body,Mesh cards,bool save){
            const string folder=FishLowPoly5K.Folder;string texturePath=folder+"/HairCardsPaint.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);if(importer==null)throw new InvalidOperationException("Rear card atlas was not imported.");
            if(importer.npotScale!=TextureImporterNPOTScale.None||importer.maxTextureSize<2048||importer.textureCompression!=TextureImporterCompression.Uncompressed||!importer.alphaIsTransparency){importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.5f;importer.SaveAndReimport();}
            var shader=Shader.Find("DesktopFish/Rear Hair Alpha Cards");if(!shader)throw new InvalidOperationException("Rear alpha-card shader missing.");var material=new Material(shader){name="Rear hair alpha cards"};material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(save){string mp=folder+"/RearHairCards.asset",matp=folder+"/RearHairCards.mat";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(mp);if(existing){existing.Clear(false);existing.ClearBlendShapes();EditorUtility.CopySerialized(cards,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(cards);cards=existing;}else AssetDatabase.CreateAsset(cards,mp);var mat=AssetDatabase.LoadAssetAtPath<Material>(matp);if(mat){EditorUtility.CopySerialized(material,mat);EditorUtility.SetDirty(mat);UnityEngine.Object.DestroyImmediate(material);material=mat;}else AssetDatabase.CreateAsset(material,matp);}
            var go=new GameObject("RearHairCards");go.transform.SetParent(body.transform.parent,false);go.transform.localPosition=body.transform.localPosition;go.transform.localRotation=body.transform.localRotation;go.transform.localScale=body.transform.localScale;go.layer=body.gameObject.layer;var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=cards;skin.sharedMaterial=material;skin.bones=body.bones;skin.rootBone=body.rootBone;skin.localBounds=body.localBounds;skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=body.updateWhenOffscreen;
        }
    }
}
