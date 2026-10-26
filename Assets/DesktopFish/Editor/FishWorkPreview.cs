using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DesktopFish.Editor
{
    public static class FishWorkPreview
    {
        [Serializable] sealed class Report
        {
            public int sampledFrames, countTransitions, maxCookers, avatarTriangles, maxPropTriangles;
            public float maxRightWristError, maxLeftWristError, maxArmLengthChange, maxCrankPhaseError, maxCableEndpointError;
            public float minimumViewportMargin=1;
            public bool passed, allJobsRepresented;
        }
        public static void Render() => Render("work/hand-generator-156");
        public static void RenderLargeCookers() => Render("work/rice-cooker-size-157");

        static void Render(string output)
        {
            Directory.CreateDirectory(output+"/frames");
            FishBuild.UseRebuilt=true;FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var model=companion.model;var motion=new FishMotion(model){randomActions=false};
            var camera=companion.view;camera.backgroundColor=Color.white;
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots=new MeshFilter[skins.Length];
            var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
            float[] lengths=new float[4];
            string[] sides={"L","R"};
            for(int side=0;side<2;side++)
            {
                string prefix="J_Bip_"+sides[side]+"_";
                lengths[side*2]=Vector3.Distance(bones[prefix+"UpperArm"].position,bones[prefix+"LowerArm"].position);
                lengths[side*2+1]=Vector3.Distance(bones[prefix+"LowerArm"].position,bones[prefix+"Hand"].position);
            }
            var report=new Report{allJobsRepresented=true,avatarTriangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3)};
            var previous=RenderTexture.active;
            var target=new RenderTexture(480,640,24,RenderTextureFormat.ARGB32){antiAliasing=4};
            var image=new Texture2D(480,640,TextureFormat.RGBA32,false);
            for(int s=0;s<skins.Length;s++)
            {
                var go=new GameObject("Generator preview snapshot");go.transform.SetParent(skins[s].transform,false);
                snapshots[s]=go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>().sharedMaterials=skins[s].sharedMaterials;skins[s].enabled=false;
            }
            using(var station=new FishWorkStation(model,companion.workPropShader))
            try
            {
                camera.targetTexture=target;
                Action<string> capture=path=>{
                    FishBuild.ApplyMotionExpressions(companion,motion);
                    foreach(var skin in skins) {
                        int fun=skin.sharedMesh.GetBlendShapeIndex("Fcl_ALL_Fun");
                        if(fun>=0)skin.SetBlendShapeWeight(fun,25*motion.WorkWeight);
                    }
                    for(int s=0;s<skins.Length;s++)
                    {
                        if(snapshots[s].sharedMesh)UnityEngine.Object.DestroyImmediate(snapshots[s].sharedMesh);
                        var mesh=new Mesh();skins[s].BakeMesh(mesh);snapshots[s].sharedMesh=mesh;
                        foreach(var p in mesh.vertices) {
                            if(!Finite(p))throw new InvalidOperationException("Non-finite skinned generator pose.");
                            Vector3 v=camera.WorldToViewportPoint(skins[s].transform.TransformPoint(p));
                            report.minimumViewportMargin=Mathf.Min(report.minimumViewportMargin,v.x,1-v.x,v.y,1-v.y);
                        }
                    }
                    foreach(var filter in station.Root.GetComponentsInChildren<MeshFilter>())foreach(var p in filter.sharedMesh.vertices) {
                        Vector3 v=camera.WorldToViewportPoint(filter.transform.TransformPoint(p));
                        report.minimumViewportMargin=Mathf.Min(report.minimumViewportMargin,v.x,1-v.x,v.y,1-v.y);
                    }
                    camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,480,640),0,0);image.Apply();
                    File.WriteAllBytes(output+"/"+path+".png",image.EncodeToPNG());
                };
                motion.Tick(0,1,0,0,"idle",false);station.Tick(0,motion.WorkWeight,0);capture("idle");
                if(station.Visible)throw new InvalidOperationException("Work props visible while idle.");
                // Settle the same exponential blend used by the actual player.
                for(int i=0;i<150;i++)motion.Tick(i/30f,1/30f,0,0,"working",false);
                for(int f=0;f<48;f++)
                {
                    float time=f/24f;motion.Tick(time,1/24f,0,0,"working",false);station.Tick(time,motion.WorkWeight,3);
                    if(station.CookerCount!=3||!station.Visible)throw new InvalidOperationException("Missing job cooker.");
                    Vector3 right=model.InverseTransformPoint(bones["J_Bip_R_Hand"].position),left=model.InverseTransformPoint(bones["J_Bip_L_Hand"].position);
                    report.maxRightWristError=Mathf.Max(report.maxRightWristError,Vector3.Distance(right,FishWorkStation.CrankGrip(time)+Vector3.up*.025f));
                    report.maxLeftWristError=Mathf.Max(report.maxLeftWristError,Vector3.Distance(left,FishWorkStation.BraceWrist));
                    var actualGrip=station.Root.Find("Hand crank/Crank grip");
                    report.maxCrankPhaseError=Mathf.Max(report.maxCrankPhaseError,Vector3.Distance(model.InverseTransformPoint(actualGrip.position),FishWorkStation.CrankGrip(time)));
                    for(int side=0;side<2;side++)
                    {
                        string prefix="J_Bip_"+sides[side]+"_";
                        report.maxArmLengthChange=Mathf.Max(report.maxArmLengthChange,Mathf.Abs(Vector3.Distance(bones[prefix+"UpperArm"].position,bones[prefix+"LowerArm"].position)-lengths[side*2]),Mathf.Abs(Vector3.Distance(bones[prefix+"LowerArm"].position,bones[prefix+"Hand"].position)-lengths[side*2+1]));
                    }
                    capture("frames/frame"+f.ToString("D3"));report.sampledFrames++;
                }
                foreach(int count in new[]{1,3,5,2,0,8,1,0})
                {
                    motion.Tick(.5f,1,0,0,"working",false);
                    station.Tick(.5f,1,count);
                    if(station.CookerCount!=count)throw new InvalidOperationException("Cooker count differs from active jobs.");
                    report.countTransitions++;report.maxCookers=Mathf.Max(report.maxCookers,count);
                    report.maxPropTriangles=Mathf.Max(report.maxPropTriangles,station.Root.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3));
                    foreach(var filter in station.Root.GetComponentsInChildren<MeshFilter>())
                    {
                        var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
                        foreach(var p in vertices)if(!Finite(p))throw new InvalidOperationException("Non-finite work prop.");
                        for(int i=0;i<triangles.Length;i+=3)
                            if(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).sqrMagnitude<1e-18f)
                                throw new InvalidOperationException("Degenerate work prop in "+filter.name);
                        if(filter.name=="Power cable") {
                            Vector3 end=(vertices[vertices.Length-1]+vertices[vertices.Length-2])*.5f;
                            report.maxCableEndpointError=Mathf.Max(report.maxCableEndpointError,Vector3.Distance(end,new Vector3(.046f,.038f,-.012f)));
                        }
                    }
                    capture("jobs-"+count);
                }
                foreach(string state in new[]{"waiting","complete","idle","error"})
                {
                    for(int i=0;i<90;i++)motion.Tick(3+i/30f,1/30f,0,0,state,false);
                    station.Tick(6,motion.WorkWeight,0);if(station.Visible)throw new InvalidOperationException("Work props persist after work.");
                }
                for(int i=0;i<90;i++)motion.Tick(6+i/30f,1/30f,0,0,"working",true);
                station.Tick(9,motion.WorkWeight,3);if(station.Visible)throw new InvalidOperationException("Crank props persist during drag.");
                if(report.maxRightWristError>.0002f||report.maxLeftWristError>.0002f||report.maxArmLengthChange>.000001f||
                    report.maxCrankPhaseError>.000001f||report.maxCableEndpointError>.000001f||report.minimumViewportMargin<.02f)
                    throw new InvalidOperationException("Crank contact or arm length validation failed: "+JsonUtility.ToJson(report));
                report.passed=true;File.WriteAllText(output+"/validation.json",JsonUtility.ToJson(report,true));
                Debug.Log("Hand generator preview passed: "+JsonUtility.ToJson(report));
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
                for(int s=0;s<skins.Length;s++){if(snapshots[s].sharedMesh)UnityEngine.Object.DestroyImmediate(snapshots[s].sharedMesh);UnityEngine.Object.DestroyImmediate(snapshots[s].gameObject);skins[s].enabled=true;}
            }
        }
        static bool Finite(Vector3 p)=>!(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z));
    }
}
