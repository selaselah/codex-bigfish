using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace DesktopFish.Editor {
    // Offline demonstration only: seeded props, in-memory fake sessions, no
    // bridge writes or saved model/scene changes. Uses current runtime motion.
    public static class FishCoFishMovie {
        const int Width=936,Height=1248,Fps=30,Frames=1800;
        const string Output="work/demo-cofish-276";
        [Serializable] sealed class Input {public FishTaskItem[] tasks;}
        [Serializable] sealed class Frame {public int frame,jobs,pending;public float time,weight,phase,potX,potY;public string taskJson;}
        [Serializable] sealed class Report {public int frames=Frames,fps=Fps,triangles,bones;public bool passed;public float minMargin=1,cameraSize;public Vector3 cameraPosition;}
        static string ID(int i)=>"00000000-0000-4000-8000-"+i.ToString("D12");
        static string Title(int i)=>i==1?"设计 CoFish 图标":i==2?"打磨玻璃卡片":i==3?"制作演示视频":"新任务 "+i;
        static FishTaskItem Item(int i,float t) {
            bool done=(i==1&&t>=35&&t<39)||(i==2&&t>=43)||(i>=4&&i<=8&&t>=51)||t>=55;
            float stamp=i==1?(t>=55?55:t>=39?39:t>=35?35:3):i==2?(t>=43?43:6):i==3?(t>=55?55:9):t>=55?55:t>=51&&i<=8?51:49;
            return new FishTaskItem {session_id=ID(i),turn_id=i==1&&t>=39?"second-turn":"first-turn",title=Title(i),summary=done?"结果已准备好，等你查看":i==1&&t>=39?"继续对话，更新原来的卡片":"正在处理任务",message="演示任务，非真实聊天",state=done?"complete":"working",updated_at=1000+stamp,expires_at=10000};
        }
        static FishTaskItem[] Inputs(float t) {
            var ids=t<3?Array.Empty<int>():t<6?new[]{1}:t<9?new[]{1,2}:t<49?new[]{1,2,3}:Enumerable.Range(1,11).ToArray();
            return ids.Where(i=>!(i==2&&t>=45.5f)).Select(i=>Item(i,t)).ToArray();
        }
        public static void Render() {
            bool preview=Environment.GetEnvironmentVariable("COFISH_MOVIE_PREVIEW")=="1";
            var probes=new HashSet<int>(new[]{0,60,120,240,330,375,450,495,540,615,690,750,795,840,930,1020,1080,1169,1200,1320,1365,1380,1425,1500,1560,1680,1755,1799});
            string folder=Output+(preview?"/preview-model":"/model-frames");Directory.CreateDirectory(folder);
            var report=new Report();Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;Vector3 right=Vector3.right,up=Vector3.up,origin=Vector3.zero;
            var metadata=new System.Text.StringBuilder();
            for(int pass=0;pass<2;pass++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();
                report.triangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3);report.bones=skins[0].bones.Length;
                if(report.triangles!=4999||report.bones!=58)throw new InvalidOperationException("Use selected avatar, no model rebuild");
                var camera=c.view;camera.aspect=Width/(float)Height;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                if(pass==0){right=camera.transform.right;up=camera.transform.up;origin=camera.transform.position;}else{camera.orthographicSize=report.cameraSize;camera.transform.position=report.cameraPosition;}
                using(var motion=new FishMotion(c.model){randomActions=false})using(var station=new FishWorkStation(c.model,c.workPropShader,276,0)) {
                    var ledger=new FishTaskLedger();var meshes=skins.Select((s,i)=>new Mesh{name="Movie snapshot "+i}).ToArray();
                    if(pass==1)for(int i=0;i<skins.Length;i++){var go=new GameObject("Offline snapshot");go.transform.SetParent(skins[i].transform,false);go.AddComponent<MeshFilter>().sharedMesh=meshes[i];go.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;}
                    var old=RenderTexture.active;RenderTexture target=null;Texture2D image=null;
                    if(pass==1){target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32){antiAliasing=4};image=new Texture2D(Width,Height,TextureFormat.RGBA32,false);camera.targetTexture=target;}
                    try {for(int frame=0;frame<Frames;frame++) {
                        float t=frame/(float)Fps,now=100+t;
                        if(frame==1365){var finished=ledger.Completed.FirstOrDefault(item=>item.chat_id==ID(2));if(finished==null)throw new InvalidOperationException("Missing completion to acknowledge");ledger.Acknowledge(finished.session_id);}
                        ledger.Update(JsonUtility.ToJson(new Input{tasks=Inputs(t)}),1000+t);
                        if(frame==1050||frame==1290||frame==1650)motion.Play(FishMotion.Gesture.Cheer,now);
                        int jobs=ledger.Active.Count;station.SyncCompletedTasks(ledger.Completed,now);
                        motion.Tick(now,1f/Fps,0,0,jobs>0?"working":"idle",false);station.Tick(now,motion.WorkWeight,jobs,jobs>0);station.BindTasks(ledger.Active);
                        FishBuild.ApplyMotionExpressions(c,motion);float phase=Mathf.Repeat(t+1.1f,3.7f),blink=phase<.18f?Mathf.Sin(phase/.18f*Mathf.PI)*100:0;
                        foreach(var skin in skins){int closed=skin.sharedMesh.GetBlendShapeIndex("Fcl_EYE_Close"),fun=skin.sharedMesh.GetBlendShapeIndex("Fcl_ALL_Fun");if(closed>=0)skin.SetBlendShapeWeight(closed,motion.BlinkWeight(blink));if(fun>=0)skin.SetBlendShapeWeight(fun,25*motion.WorkWeight);}
                        if(pass==1){Vector2 pot=Vector2.zero;for(int i=0;i<station.CookerCount;i++)if(station.CookerTaskIdAt(i)==ID(2)){var hit=(BoxCollider)station.CookerHitAreaAt(i);pot=camera.WorldToViewportPoint(hit.transform.TransformPoint(hit.center));}
                            metadata.AppendLine(JsonUtility.ToJson(new Frame{frame=frame,time=t,jobs=jobs,pending=ledger.Completed.Count,taskJson=ledger.Json,weight=motion.WorkWeight,phase=FishWorkStation.CrankAngle(now),potX=pot.x,potY=pot.y}));}
                        if((pass==0&&frame%6!=0)||(pass==1&&preview&&!probes.Contains(frame)))continue;
                        for(int i=0;i<skins.Length;i++)skins[i].BakeMesh(meshes[i]);
                        Action<Vector3> measure=p=>{if(float.IsNaN(p.x)||float.IsInfinity(p.x))throw new InvalidOperationException("Nonfinite movie mesh");if(pass==0){var d=p-origin;min=Vector2.Min(min,new Vector2(Vector3.Dot(d,right),Vector3.Dot(d,up)));max=Vector2.Max(max,new Vector2(Vector3.Dot(d,right),Vector3.Dot(d,up)));}else{var v=camera.WorldToViewportPoint(p);report.minMargin=Mathf.Min(report.minMargin,v.x,1-v.x,v.y,1-v.y);}};
                        for(int i=0;i<skins.Length;i++)foreach(var p in meshes[i].vertices)measure(skins[i].transform.TransformPoint(p));
                        foreach(var f in station.Root.GetComponentsInChildren<MeshFilter>())if(f.gameObject.activeInHierarchy)foreach(var p in f.sharedMesh.vertices)measure(f.transform.TransformPoint(p));
                        if(pass==1){camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,Width,Height),0,0);image.Apply();var pixels=image.GetPixels32();for(int i=0;i<pixels.Length;i++){var q=pixels[i];int a=q.a;pixels[i]=a==0?new Color32(0,0,0,0):new Color32((byte)Math.Min(255,(q.r*255+a/2)/a),(byte)Math.Min(255,(q.g*255+a/2)/a),(byte)Math.Min(255,(q.b*255+a/2)/a),q.a);}image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(folder+"/frame"+frame.ToString("D4")+".png",image.EncodeToPNG());if(frame%150==0||preview)Debug.Log("CoFish movie frame "+frame+"/1800");}
                    }
                    if(pass==0){var ext=(max-min)*.5f;var center=(max+min)*.5f;report.cameraSize=Mathf.Max(ext.x/(Width/(float)Height*.90f),ext.y/.76f);report.cameraPosition=origin+right*center.x+up*(center.y-.13f*report.cameraSize);}
                    }finally{camera.targetTexture=null;RenderTexture.active=old;if(target)UnityEngine.Object.DestroyImmediate(target);if(image)UnityEngine.Object.DestroyImmediate(image);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);foreach(var skin in skins)skin.enabled=true;}
                }
            }
            if(report.minMargin<.02f)throw new InvalidOperationException("Movie framing clips props: "+report.minMargin);
            report.passed=true;File.WriteAllText(Output+"/"+(preview?"preview-model-validation.json":"model-validation.json"),JsonUtility.ToJson(report,true));File.WriteAllText(Output+"/metadata.jsonl",metadata.ToString());Debug.Log("PASS: CoFish movie current avatar/hair/full-size props/ledger rendered, protected assets not saved");
        }
    }
}
