using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesktopFish.Editor
{
    // Offline movie capture only. Does not touch the app's bridge or saved assets.
    public static class FishDemoVideo
    {
        const int Size=1080,Fps=30,Frames=1500;
        const string Output="work/demo-video-190";
        [Serializable] sealed class Report {
            public int width=Size,height=Size,fps=Fps,frames=Frames,durationSeconds=50,completedCookers;
            public float cameraSize,minMargin=1;
            public Vector3 cameraPosition;
            public bool pureWhiteBackground,constantCamera=true,remainingJobsRetained=true,passed;
            public string[] stages={"0..7 idle","7..17 one job","17..27 two jobs","27..37 three jobs","37..50 complete one, two remain"};
        }
        static int Count(float t)=>t<7?0:t<17?1:t<27?2:t<37?3:2;

        public static void ExportAudioTiming() {
            EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();var motion=new FishMotion(companion.model){randomActions=false};
            var csv=new System.Text.StringBuilder("frame,time,workWeight,crankPhase,jobs\n");
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            for(int frame=0;frame<Frames;frame++) {
                float t=frame/(float)Fps,now=100+t;int count=Count(t);
                if(frame==37*Fps)motion.Play(FishMotion.Gesture.Cheer,now);
                motion.Tick(now,1f/Fps,0,0,count==0?"idle":"working",false);
                csv.Append(frame).Append(',').Append(t.ToString("R",culture)).Append(',').Append(motion.WorkWeight.ToString("R",culture)).Append(',').Append(FishWorkStation.CrankAngle(now).ToString("R",culture)).Append(',').Append(count).Append('\n');
            }
            Directory.CreateDirectory("work/demo-voice-191");File.WriteAllText("work/demo-voice-191/motion-timing.csv",csv.ToString());
            Debug.Log("Demo audio timing exported:1500frames, original50second motion sequence.");
        }

        public static void Render() {
            Directory.CreateDirectory(Output+"/frames");
            Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            Vector3 right=Vector3.right,up=Vector3.up,origin=Vector3.zero;
            var report=new Report();
            // Measure the whole actual sequence, then replay with one fixed square camera.
            for(int pass=0;pass<2;pass++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
                var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();var model=companion.model;
                var camera=companion.view;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                if(pass==0){right=camera.transform.right;up=camera.transform.up;origin=camera.transform.position;}
                else {camera.orthographicSize=report.cameraSize;camera.transform.position=report.cameraPosition;}
                var motion=new FishMotion(model){randomActions=false};
                var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();var snapshots=new MeshFilter[skins.Length];var meshes=new Mesh[skins.Length];
                for(int s=0;s<skins.Length;s++) {
                    meshes[s]=new Mesh{name="Movie pose "+s};meshes[s].MarkDynamic();
                    if(pass==1){var go=new GameObject("Movie snapshot");go.transform.SetParent(skins[s].transform,false);snapshots[s]=go.AddComponent<MeshFilter>();snapshots[s].sharedMesh=meshes[s];go.AddComponent<MeshRenderer>().sharedMaterials=skins[s].sharedMaterials;skins[s].enabled=false;}
                }
                RenderTexture target=null;Texture2D image=null;RenderTexture previous=RenderTexture.active;
                if(pass==1){target=new RenderTexture(Size,Size,24,RenderTextureFormat.ARGB32){antiAliasing=4};image=new Texture2D(Size,Size,TextureFormat.RGBA32,false);camera.targetTexture=target;}
                using(var station=new FishWorkStation(model,companion.workPropShader,159,0))
                try {
                    for(int frame=0;frame<Frames;frame++) {
                        float t=frame/(float)Fps,now=100+t;int count=Count(t);string state=count==0?"idle":"working";
                        if(frame==37*Fps){motion.Play(FishMotion.Gesture.Cheer,now);int finished=station.CompleteCookers(1,now);if(pass==1)report.completedCookers=finished;}
                        motion.Tick(now,1f/Fps,0,0,state,false);station.Tick(now,motion.WorkWeight,count,count>0);
                        FishBuild.ApplyMotionExpressions(companion,motion);
                        float blinkPhase=Mathf.Repeat(t+1.1f,3.7f),blink=blinkPhase<.18f?Mathf.Sin(blinkPhase/.18f*Mathf.PI)*100:0;
                        foreach(var skin in skins) {
                            int closed=skin.sharedMesh.GetBlendShapeIndex("Fcl_EYE_Close"),fun=skin.sharedMesh.GetBlendShapeIndex("Fcl_ALL_Fun");
                            if(closed>=0)skin.SetBlendShapeWeight(closed,motion.BlinkWeight(blink));
                            if(fun>=0)skin.SetBlendShapeWeight(fun,25*motion.WorkWeight);
                        }
                        if(pass==0&&frame%6!=0)continue;
                        for(int s=0;s<skins.Length;s++)skins[s].BakeMesh(meshes[s]);
                        Action<Vector3> measure=p=>{
                            if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))throw new InvalidOperationException("Non-finite demo geometry.");
                            if(pass==0){Vector3 d=p-origin;float x=Vector3.Dot(d,right),y=Vector3.Dot(d,up);min=Vector2.Min(min,new Vector2(x,y));max=Vector2.Max(max,new Vector2(x,y));}
                            else {Vector3 v=camera.WorldToViewportPoint(p);report.minMargin=Mathf.Min(report.minMargin,v.x,1-v.x,v.y,1-v.y);}
                        };
                        for(int s=0;s<skins.Length;s++)foreach(var p in meshes[s].vertices)measure(skins[s].transform.TransformPoint(p));
                        foreach(var f in station.Root.GetComponentsInChildren<MeshFilter>())if(f.gameObject.activeInHierarchy)foreach(var p in f.sharedMesh.vertices)measure(f.transform.TransformPoint(p));
                        if(pass==1) {
                            if(t>=37&&(!station.WorkingVisible||station.CookerCount!=2))throw new InvalidOperationException("Demo lost remaining two cookers.");
                            camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,Size,Size),0,0);image.Apply();
                            // Palette keeps alpha. Composite its premultiplied edges over exact white.
                            Color32[] pixels=image.GetPixels32();
                            for(int i=0;i<pixels.Length;i++){var c=pixels[i];int white=255-c.a;pixels[i]=new Color32((byte)Math.Min(255,c.r+white),(byte)Math.Min(255,c.g+white),(byte)Math.Min(255,c.b+white),255);}
                            if(pixels[0].r!=255||pixels[0].g!=255||pixels[0].b!=255)throw new InvalidOperationException("Video matte is not pure white.");
                            image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(Output+"/frames/frame"+frame.ToString("D4")+".png",image.EncodeToPNG());
                            if(frame%150==0)Debug.Log("Demo video frame "+frame+"/"+Frames+" jobs "+count);
                        }
                    }
                    if(pass==0){Vector2 extent=(max-min)*.5f,center=(max+min)*.5f;report.cameraSize=Mathf.Max(extent.x/.88f,extent.y/.78f);report.cameraPosition=origin+right*center.x+up*(center.y+.025f*report.cameraSize);}
                } finally {
                    camera.targetTexture=null;RenderTexture.active=previous;
                    if(target)UnityEngine.Object.DestroyImmediate(target);if(image)UnityEngine.Object.DestroyImmediate(image);
                    foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);
                    foreach(var skin in skins)skin.enabled=true;
                }
            }
            if(report.minMargin<.04f||report.completedCookers!=1)throw new InvalidOperationException("Demo framing or completion failed: "+JsonUtility.ToJson(report));
            report.pureWhiteBackground=true;report.passed=true;File.WriteAllText(Output+"/render-validation.json",JsonUtility.ToJson(report,true));
            Debug.Log("Square demo video capture passed: "+JsonUtility.ToJson(report));
        }
    }
}
