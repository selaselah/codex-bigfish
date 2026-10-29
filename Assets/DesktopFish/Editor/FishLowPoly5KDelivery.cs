using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DesktopFish.Editor {
 public static class FishLowPoly5KDelivery {
  [Serializable] class Bone {public string name;public int parent;public Vector3 position;}
  [Serializable] class Shape {public string name;public Vector3[] deltas,normal_deltas;}
  [Serializable] class Skin {public int[] indices;public float[] weights;}
  [Serializable] class Part {public string name;public Vector3[] vertices,normals;public Vector2[] uv;public Color[] colors;public int[] triangles,bone_indices,secondary_bone_indices;public float[] primary_bone_weights;public Skin[] skin4;public Shape[] shapes;}
  [Serializable] class Model {public Bone[] bones;public Part[] meshes;}
  [MenuItem("Desktop Fish/5K/Save Model Variant")]
  public static void SaveVariant(){
   EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();FishLowPoly5K.RestoreFullModel(c.model.gameObject);FishLowPoly5K.FreezeSource(c.model.gameObject);FishLowPoly5K.Apply(c.model.gameObject,true);
   var original=JsonUtility.FromJson<Model>(File.ReadAllText("work/rebuilt-geometry.json"));var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();
   var parts=skins.Select(s=>{
    var m=s.sharedMesh;var p=m.vertices;var n=m.normals;var colors=m.colors;var uv=m.uv;var weights=m.boneWeights;if(n.Length!=p.Length||colors.Length!=p.Length||uv.Length!=p.Length||weights.Length!=p.Length)throw new InvalidOperationException("5K attribute stream missing.");
    for(int i=0;i<p.Length;i++){if(!Finite(p[i])||!Finite(n[i])||Mathf.Abs(n[i].magnitude-1)>.001f||float.IsNaN(uv[i].x)||float.IsNaN(uv[i].y))throw new InvalidOperationException("5K position/normal/UV validation failed.");var w=weights[i];if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.000001f||new[]{w.weight0,w.weight1,w.weight2,w.weight3}.Any(a=>a<0||a>1)||new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(a=>a<0||a>=58))throw new InvalidOperationException("5K skin support failed.");}
    var shapes=Enumerable.Range(0,m.blendShapeCount).Select(i=>{var dp=new Vector3[p.Length];var dn=new Vector3[p.Length];var dt=new Vector3[p.Length];m.GetBlendShapeFrameVertices(i,0,dp,dn,dt);if(dp.Any(v=>!Finite(v))||dn.Any(v=>!Finite(v)))throw new InvalidOperationException("5K expression finite check");return new Shape{name=m.GetBlendShapeName(i),deltas=dp,normal_deltas=dn};}).ToArray();
    return new Part{name=m.name,vertices=p,normals=n,uv=uv,colors=colors,triangles=m.triangles,bone_indices=weights.Select(w=>w.boneIndex0).ToArray(),secondary_bone_indices=weights.Select(w=>w.boneIndex1).ToArray(),primary_bone_weights=weights.Select(w=>w.weight0).ToArray(),skin4=weights.Select(w=>new Skin{indices=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3},weights=new[]{w.weight0,w.weight1,w.weight2,w.weight3}}).ToArray(),shapes=shapes};
   }).ToArray();
   if(parts.Sum(p=>p.triangles.Length/3)>5000||original.bones.Length!=58)throw new InvalidOperationException("5K exported triangle/rig count");File.WriteAllText("work/rebuilt-geometry-5k.json",JsonUtility.ToJson(new Model{bones=original.bones,meshes=parts}));Debug.Log("5K saved and validated:"+parts.Sum(p=>p.triangles.Length/3)+" triangles/58 bones/six shared expressions/full four-slot skin; source assets retained.");
  }
  static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
  [MenuItem("Desktop Fish/5K/Build macOS App")]
  public static void Build(){FishBuild.UseRebuilt=true;FishBuild.UseLowPoly5K=true;FishBuild.BuildMac();Debug.Log("5K application build completed.");}
 }
}
