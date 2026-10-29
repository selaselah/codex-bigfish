using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace DesktopFish.Editor {
 public static class FishLowPolyDeformation {
  [Serializable] class Pose {public string name;public Vector3[] positions,normals;public Matrix4x4[] skinMatrices;}
  [Serializable] class Poses {public Pose[] poses;}
  public static void Export(){
   EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();FishLowPoly5K.RestoreFullModel(c.model.gameObject);if(Environment.GetEnvironmentVariable("FISH_DEFORMATION_LOW")=="1")FishLowPoly5K.Apply(c.model.gameObject,false);var m=new FishMotion(c.model){randomActions=false};var skin=c.model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.sharedMesh.name=="Body");var mesh=new Mesh();var poses=new List<Pose>();int expected=skin.sharedMesh.vertexCount;
   Action<string> save=name=>{skin.BakeMesh(mesh);if(mesh.vertexCount!=expected)throw new InvalidOperationException("source vertex count");var bind=skin.sharedMesh.bindposes;poses.Add(new Pose{name=name,positions=mesh.vertices,normals=mesh.normals,skinMatrices=skin.bones.Select((b,i)=>skin.transform.worldToLocalMatrix*b.localToWorldMatrix*bind[i]).ToArray()});};
   m.Tick(1,1,0,0,"idle",false);save("idle");m.Play(FishMotion.Gesture.Wave,10);m.Tick(11.36f,1,0,0,"idle",false);save("wave");m.Play(FishMotion.Gesture.Bow,20);m.Tick(22.1f,1,0,0,"idle",false);save("bow");m.Play(FishMotion.Gesture.None,30);
   for(int i=0;i<120;i++)m.Tick(30+i/30f,1/30f,0,0,"working",false);
   for(int i=0;i<4;i++){m.Tick(34+i*.357f,1/30f,0,0,"working",false);save("crank"+i);}
   File.WriteAllText(Environment.GetEnvironmentVariable("FISH_DEFORMATION_OUTPUT")??"work/poly5k-192/actual-deformation.json",JsonUtility.ToJson(new Poses{poses=poses.ToArray()}));Debug.Log("5K actual deformation export passed:7 own runtime poses.");
  }
 }
}
