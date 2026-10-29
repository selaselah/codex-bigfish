using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace DesktopFish.Editor {
 public static class FishVisibilityAudit {
  [Serializable] sealed class Report {public bool[] visible,outline;public int[] maximumPixels;public int views,seen,total;public string method="actual authored alpha/depth triangle ID,default8angles plus high/low views;all expressions and runtime gesture/work poses;sampled visibility only";}
  public static void Run(){
   EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();var camera=c.view;var palette=camera.GetComponent<FishPalette>();palette.enabled=false;
   FishLowPoly5K.RestoreFullModel(c.model.gameObject);if(Environment.GetEnvironmentVariable("FISH_VISIBILITY_LOW")=="1")FishLowPoly5K.Apply(c.model.gameObject,false);var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();var body=skins.Single(s=>s.name=="Body and hair");int total=skins.Sum(s=>s.sharedMesh.triangles.Length/3);var report=new Report{visible=new bool[total],outline=new bool[total],maximumPixels=new int[total],total=total};
   var diagnostic=new Mesh[skins.Length];var baked=new Mesh[skins.Length];var filters=new MeshFilter[skins.Length];var materials=new Material[skins.Length];var originalTriangles=skins.Select(s=>s.sharedMesh.triangles).ToArray();int offset=0;
   for(int s=0;s<skins.Length;s++){
    int[] indices=originalTriangles[s];var mesh=new Mesh{indexFormat=IndexFormat.UInt32};var points=new Vector3[indices.Length];var uv=skins[s].sharedMesh.uv;var colors=skins[s].sharedMesh.colors;mesh.vertices=points;mesh.triangles=Enumerable.Range(0,indices.Length).ToArray();mesh.uv=indices.Select(i=>uv[i]).ToArray();mesh.SetUVs(1,indices.Select(i=>new Vector2(colors[i].a,0)).ToList());
    var ids=new Color32[indices.Length];for(int i=0;i<indices.Length;i++){int id=offset+i/3+1;ids[i]=new Color32((byte)id,(byte)(id>>8),(byte)(id>>16),255);}mesh.colors32=ids;diagnostic[s]=mesh;baked[s]=new Mesh();
    var go=new GameObject("Triangle visibility IDs");go.transform.SetParent(skins[s].transform,false);filters[s]=go.AddComponent<MeshFilter>();filters[s].sharedMesh=mesh;materials[s]=new Material(Shader.Find("Hidden/DesktopFish/VisibilityID"));materials[s].mainTexture=skins[s].sharedMaterial.mainTexture;go.AddComponent<MeshRenderer>().sharedMaterial=materials[s];skins[s].enabled=false;offset+=indices.Length/3;
   }
   var rt=new RenderTexture(1080,1440,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){antiAliasing=1};var image=new Texture2D(1080,1440,TextureFormat.RGBA32,false,true);camera.targetTexture=rt;camera.backgroundColor=Color.clear;camera.clearFlags=CameraClearFlags.SolidColor;camera.aspect=.75f;camera.orthographicSize*=1.12f;
   Vector3 front=camera.transform.position,center=front+camera.transform.forward*3;var motion=new FishMotion(c.model){randomActions=false};
   Action bake=()=>{for(int s=0;s<skins.Length;s++){skins[s].BakeMesh(baked[s]);var p=baked[s].vertices;diagnostic[s].vertices=originalTriangles[s].Select(i=>p[i]).ToArray();diagnostic[s].RecalculateBounds();}};
   Action<float,float> view=(yaw,pitch)=>{camera.transform.position=center+Quaternion.Euler(-pitch,yaw,0)*(front-center);camera.transform.LookAt(center);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1080,1440),0,0);image.Apply();var pixels=image.GetPixels32();var ids=new int[pixels.Length];var counts=new int[total];for(int i=0;i<pixels.Length;i++){var pixel=pixels[i];int id=pixel.r+(pixel.g<<8)+(pixel.b<<16);ids[i]=id;if(id>0&&id<=total){report.visible[id-1]=true;counts[id-1]++;}}for(int i=1080;i<ids.Length-1080;i++){int id=ids[i];if(id>0&&id<=total&&(ids[i-1]==0||ids[i+1]==0||ids[i-1080]==0||ids[i+1080]==0))report.outline[id-1]=true;}for(int i=0;i<total;i++)report.maximumPixels[i]=Math.Max(report.maximumPixels[i],counts[i]);report.views++;};
   try {
    for(int pose=0;pose<25;pose++) {
     motion.Play(FishMotion.Gesture.None,100);string state="idle";float now=101;
     if(pose>=1&&pose<=10){int action=(pose-1)/2;motion.Play(new[]{FishMotion.Gesture.Wave,FishMotion.Gesture.Bow,FishMotion.Gesture.Doze,FishMotion.Gesture.Step,FishMotion.Gesture.Cheer}[action],100);now=100+new[]{1.36f,1.79f,1.4f,2.4f,2f,4f,.65f,1.95f,.8f,1.6f}[pose-1];}
     if(pose>=11&&pose<=14){state="working";for(int i=0;i<120;i++)motion.Tick(100+i/30f,1/30f,0,0,state,false);now=104+(pose-11)*Mathf.PI/(2*4.4f);}
     motion.Tick(now,1,pose==15?-1:pose==16?1:0,pose==17?1:pose==18?-1:0,state,false);FishBuild.ApplyMotionExpressions(c,motion);
     if(pose>=19){foreach(var skin in skins){for(int shape=0;shape<skin.sharedMesh.blendShapeCount;shape++)skin.SetBlendShapeWeight(shape,shape==pose-19?100:0);}}
     bake();for(int a=0;a<8;a++)view(a*45,pose==23?28:pose==24?-12:0);
     if(pose==0){for(int a=0;a<8;a++){view(a*45,28);view(a*45,-12);}}
     Debug.Log("Visibility pose "+pose+"/25");
    }
    report.seen=report.visible.Count(x=>x);string output=Environment.GetEnvironmentVariable("FISH_VISIBILITY_OUTPUT")??"work/poly5k-192/visibility.json";Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report));Debug.Log("Visibility audit passed: "+report.seen+"/"+total+" triangles seen across "+report.views+" alpha/depth views.");
   }finally{camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);for(int s=0;s<skins.Length;s++){UnityEngine.Object.DestroyImmediate(diagnostic[s]);UnityEngine.Object.DestroyImmediate(baked[s]);UnityEngine.Object.DestroyImmediate(filters[s].gameObject);UnityEngine.Object.DestroyImmediate(materials[s]);skins[s].enabled=true;}}
  }
 }
}
