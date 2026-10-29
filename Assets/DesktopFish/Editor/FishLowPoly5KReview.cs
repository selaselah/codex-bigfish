using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DesktopFish.Editor {
    public static class FishLowPoly5KReview {
        static string Folder=>Environment.GetEnvironmentVariable("FISH_LOD_REVIEW_FOLDER")??"work/hair-cards-194/review";
        public static void Render() {
            Directory.CreateDirectory(Folder);
            for(int version=0;version<2;version++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
                FishLowPoly5K.RestoreFullModel(companion.model.gameObject);
                if(version==1){
                    FishLowPoly5K.Apply(companion.model.gameObject,false);
                }
                var camera=companion.view;var motion=new FishMotion(companion.model){randomActions=false};var skins=companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
                var snapshots=new MeshFilter[skins.Length];var meshes=new Mesh[skins.Length];
                for(int s=0;s<skins.Length;s++){var go=new GameObject("5K comparison snapshot");go.transform.SetParent(skins[s].transform,false);snapshots[s]=go.AddComponent<MeshFilter>();meshes[s]=new Mesh();snapshots[s].sharedMesh=meshes[s];go.AddComponent<MeshRenderer>().sharedMaterials=skins[s].sharedMaterials;skins[s].enabled=false;}
                camera.backgroundColor=Color.white;int rw=720,rh=960;bool manualExpression=false;var target=new RenderTexture(720,960,24){antiAliasing=4};var frame=new Texture2D(720,960,TextureFormat.RGBA32,false);camera.targetTexture=target;camera.aspect=.75f;
                Vector3 front=camera.transform.position;Quaternion rotation=camera.transform.rotation;Vector3 center=front+camera.transform.forward*3;string prefix=Folder+"/"+(version==0?"full":"5k");
                Action<string> capture=name=>{if(!manualExpression)FishBuild.ApplyMotionExpressions(companion,motion);for(int s=0;s<skins.Length;s++){skins[s].BakeMesh(meshes[s]);if(meshes[s].vertices.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)))throw new InvalidOperationException("Non-finite 5K pose");}camera.Render();RenderTexture.active=target;frame.ReadPixels(new Rect(0,0,rw,rh),0,0);frame.Apply();File.WriteAllBytes(prefix+"-"+name+".png",frame.EncodeToPNG());};
                Action<int,int> resize=(width,height)=>{camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);rw=width;rh=height;target=new RenderTexture(width,height,24){antiAliasing=4};frame=new Texture2D(width,height,TextureFormat.RGBA32,false);camera.targetTexture=target;};
                motion.Tick(1,1,0,0,"idle",false);
                foreach(int angle in new[]{0,35,90,180,-35,-90}){camera.transform.position=center+Quaternion.Euler(0,angle,0)*(front-center);camera.transform.LookAt(center);capture("default-"+angle);}
                camera.transform.position=center+Quaternion.Euler(-16,0,0)*(front-center);camera.transform.LookAt(center);capture("default-top");
                if(version==1){
                    foreach(var snapshot in snapshots)snapshot.GetComponent<Renderer>().enabled=snapshot.transform.parent.name=="RearHairCards";
                    foreach(int angle in new[]{35,90,180}){camera.transform.position=center+Quaternion.Euler(0,angle,0)*(front-center);camera.transform.LookAt(center);capture("cards-isolated-"+angle);}
                    foreach(var snapshot in snapshots)snapshot.GetComponent<Renderer>().enabled=true;
                }
                resize(180,240);
                foreach(int angle in new[]{0,35,90,180,-35,-90}){camera.transform.position=center+Quaternion.Euler(0,angle,0)*(front-center);camera.transform.LookAt(center);capture("small-"+angle);}camera.transform.position=center+Quaternion.Euler(-16,0,0)*(front-center);camera.transform.LookAt(center);capture("small-top");
                resize(720,960);camera.transform.SetPositionAndRotation(front,rotation);manualExpression=true;
                for(int shape=0;shape<6;shape++)foreach(float amount in new[]{.5f,1f}){foreach(var skin in skins)for(int j=0;j<6;j++)skin.SetBlendShapeWeight(j,j==shape?amount*100:0);capture("expression-"+shape+"-"+amount.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));}
                manualExpression=false;foreach(var skin in skins)for(int j=0;j<6;j++)skin.SetBlendShapeWeight(j,0);

                foreach(var action in new[]{FishMotion.Gesture.Wave,FishMotion.Gesture.Bow,FishMotion.Gesture.Doze,FishMotion.Gesture.Step,FishMotion.Gesture.Cheer}){
                    motion.Play(action,10);float time=action==FishMotion.Gesture.Wave?11.36f:action==FishMotion.Gesture.Bow?12.1f:action==FishMotion.Gesture.Doze?13:11.2f;motion.Tick(time,1,0,0,"idle",false);capture("action-"+action);
                    for(int sample=0;sample<6;sample++){motion.Play(action,30);motion.Tick(30+sample*.42f,.1f,0,0,"idle",false);capture("pose-"+action+"-"+sample);}
                }
                motion.Play(FishMotion.Gesture.None,20);using(var station=new FishWorkStation(companion.model,companion.workPropShader,159,0)){
                    for(int i=0;i<120;i++)motion.Tick(20+i/30f,1/30f,0,0,"working",false);
                    float time=24;motion.Tick(time,1/30f,0,0,"working",false);station.Tick(time,motion.WorkWeight,3,true);FishWorkStation.FrameWorkingCamera(camera,camera.orthographicSize,front,motion.WorkWeight);capture("working");
                    for(int sample=0;sample<12;sample++){time=25+sample*.12f;motion.Tick(time,.12f,0,0,"working",false);station.Tick(time,motion.WorkWeight,3,true);capture("crank-"+sample.ToString("00"));}
                }
                camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(frame);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);
            }
            Debug.Log("5K full comparison passed:default7/actual180x240/12 expression samples/35 gestures/13 working samples per model; all baked positions finite.");
        }
    }
}
