using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesktopFish.Editor {
    // Offline frames for the native-glass movie host. No bridge or asset writes.
    public static class FishTaskDemoVideo {
        const int Width=936,Height=1248,Fps=30,Frames=1500;
        const string Output="work/demo-native-210";
        static int Count(float t)=>t<3?0:t<11?1:t<19?2:t<37?3:2;
        [Serializable] sealed class Report {
            public int width=Width,height=Height,fps=Fps,frames=Frames,triangles,bones,completedCookers;
            public bool transparent=true,constantCamera=true,passed;
            public float cameraSize,minMargin=1;
            public Vector3 cameraPosition;
        }
        public static void Render() {
            bool preview=Environment.GetEnvironmentVariable("FISH_TASK_VIDEO_PREVIEW")=="1";
            var probes=new HashSet<int>(new[]{0,240,510,720,825,900,960,1050,1122,1260,1499});
            string folder=Output+(preview?"/preview-model":"/model-frames");Directory.CreateDirectory(folder);
            var report=new Report();Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=-min;
            Vector3 right=Vector3.right,up=Vector3.up,origin=Vector3.zero;
            var csv=new System.Text.StringBuilder("frame,time,workWeight,crankPhase,jobs\n");
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            for(int pass=0;pass<2;pass++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
                var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();var model=c.model;
                var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
                report.triangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3);report.bones=skins[0].bones.Length;
                if(report.triangles!=4999||report.bones!=58||skins.Length!=3||skins.Any(s=>s.sharedMesh.blendShapeCount!=6))
                    throw new InvalidOperationException("Movie must use selected4999tri/58joint/three-mesh/six-expression model.");
                var camera=c.view;camera.aspect=Width/(float)Height;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                if(pass==0){right=camera.transform.right;up=camera.transform.up;origin=camera.transform.position;}
                else{camera.orthographicSize=report.cameraSize;camera.transform.position=report.cameraPosition;}
                var motion=new FishMotion(model){randomActions=false};
                var meshes=skins.Select((s,i)=>new Mesh{name="Offline task movie pose "+i}).ToArray();
                if(pass==1)for(int i=0;i<skins.Length;i++){
                    var go=new GameObject("Offline task movie snapshot");go.transform.SetParent(skins[i].transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=meshes[i];go.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;
                }
                RenderTexture previous=RenderTexture.active,target=null;Texture2D image=null;
                if(pass==1){target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32){antiAliasing=4};image=new Texture2D(Width,Height,TextureFormat.RGBA32,false);camera.targetTexture=target;}
                using(var station=new FishWorkStation(model,c.workPropShader,159,0))
                try {
                    for(int frame=0;frame<Frames;frame++) {
                        float t=frame/(float)Fps,now=100+t;int count=Count(t);
                        if(frame==37*Fps){motion.Play(FishMotion.Gesture.Cheer,now);int completed=station.CompleteCookers(1,now);if(pass==1)report.completedCookers=completed;}
                        motion.Tick(now,1f/Fps,0,0,count==0?"idle":"working",false);station.Tick(now,motion.WorkWeight,count,count>0);
                        FishBuild.ApplyMotionExpressions(c,motion);
                        float phase=Mathf.Repeat(t+1.1f,3.7f),blink=phase<.18f?Mathf.Sin(phase/.18f*Mathf.PI)*100:0;
                        foreach(var skin in skins){int closed=skin.sharedMesh.GetBlendShapeIndex("Fcl_EYE_Close"),fun=skin.sharedMesh.GetBlendShapeIndex("Fcl_ALL_Fun");if(closed>=0)skin.SetBlendShapeWeight(closed,motion.BlinkWeight(blink));if(fun>=0)skin.SetBlendShapeWeight(fun,25*motion.WorkWeight);}
                        if(pass==1){csv.Append(frame).Append(',').Append(t.ToString("R",culture)).Append(',').Append(motion.WorkWeight.ToString("R",culture)).Append(',').Append(FishWorkStation.CrankAngle(now).ToString("R",culture)).Append(',').Append(count).Append('\n');if(t>=37&&(!station.WorkingVisible||station.CookerCount!=2))throw new InvalidOperationException("Movie lost remaining generator/two cookers.");}
                        if((pass==0&&frame%6!=0)||(pass==1&&preview&&!probes.Contains(frame)))continue;
                        for(int i=0;i<skins.Length;i++)skins[i].BakeMesh(meshes[i]);
                        Action<Vector3> measure=p=>{
                            if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))throw new InvalidOperationException("Nonfinite movie geometry.");
                            if(pass==0){Vector3 d=p-origin;min=Vector2.Min(min,new Vector2(Vector3.Dot(d,right),Vector3.Dot(d,up)));max=Vector2.Max(max,new Vector2(Vector3.Dot(d,right),Vector3.Dot(d,up)));}
                            else{var v=camera.WorldToViewportPoint(p);report.minMargin=Mathf.Min(report.minMargin,v.x,1-v.x,v.y,1-v.y);}
                        };
                        for(int i=0;i<skins.Length;i++)foreach(var p in meshes[i].vertices)measure(skins[i].transform.TransformPoint(p));
                        foreach(var f in station.Root.GetComponentsInChildren<MeshFilter>())if(f.gameObject.activeInHierarchy)foreach(var p in f.sharedMesh.vertices)measure(f.transform.TransformPoint(p));
                        if(pass==1){
                            camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,Width,Height),0,0);image.Apply();
                            // Render target edges are premultiplied; PNG/NSImage expects straight alpha.
                            var pixels=image.GetPixels32();for(int i=0;i<pixels.Length;i++){var q=pixels[i];int a=q.a;pixels[i]=a==0?new Color32(0,0,0,0):new Color32((byte)Math.Min(255,(q.r*255+a/2)/a),(byte)Math.Min(255,(q.g*255+a/2)/a),(byte)Math.Min(255,(q.b*255+a/2)/a),q.a);}
                            image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(folder+"/frame"+frame.ToString("D4")+".png",image.EncodeToPNG());
                            if(frame%150==0||preview)Debug.Log("Task demo model frame "+frame+"/"+Frames+" jobs "+count);
                        }
                    }
                    if(pass==0){var extent=(max-min)*.5f;var center=(max+min)*.5f;report.cameraSize=Mathf.Max(extent.x/(Width/(float)Height*.88f),extent.y/.74f);report.cameraPosition=origin+right*center.x+up*(center.y-.14f*report.cameraSize);}
                } finally {
                    camera.targetTexture=null;RenderTexture.active=previous;if(target)UnityEngine.Object.DestroyImmediate(target);if(image)UnityEngine.Object.DestroyImmediate(image);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);foreach(var skin in skins)skin.enabled=true;
                }
            }
            if(report.minMargin<.03f||report.completedCookers!=1)throw new InvalidOperationException("Task demo framing/completion failed: "+JsonUtility.ToJson(report));
            report.passed=true;File.WriteAllText(Output+(preview?"/preview-model-validation.json":"/model-validation.json"),JsonUtility.ToJson(report,true));File.WriteAllText(Output+"/motion-timing.csv",csv.ToString());
            Debug.Log("Task demo model capture passed: "+JsonUtility.ToJson(report));
        }
    }
}
