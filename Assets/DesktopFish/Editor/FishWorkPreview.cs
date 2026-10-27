using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace DesktopFish.Editor
{
    public static class FishWorkPreview
    {
        [Serializable] sealed class Report
        {
            public int transitionFrames;
            public int sampledFrames, countTransitions, maxCookers, avatarTriangles, maxPropTriangles;
            public float maxRightWristError, maxLeftWristError, maxArmLengthChange, maxCrankPhaseError, maxCableEndpointError;
            public float maxCookerRockDegrees, maxLidLift, maxSteamAlpha, maxSteamTravel, maxCookerPhaseDifference;
            public float maxGeneratorFloorDrift, maxWorkingYawError, maxNonWorkYaw, maxNonWorkCameraDifference;
            public float maxFootDrift,maxLegLengthChange,minKneeFlex=180,maxKneeFlex;
            public float minChestPitch=180,maxChestPitch,minRootY=100,maxRootY=-100,minRightFacing=1;
            public float minimumOpaqueAlpha=1;
            public float minimumViewportMargin=1;
            public bool passed, allJobsRepresented;
            public int stableStyleChecks, distinctRandomOrders, distinctShellGeometries;
            public string[] firstFiveStyles;
        }
        public static void Render() => Render("work/hand-generator-156");
        public static void RenderLargeCookers() => Render("work/rice-cooker-size-157");
        public static void RenderCartoonCookers() => Render("work/cartoon-cookers-158");
        public static void RenderCookerVariants() => Render("work/cooker-variants-159");
        public static void RenderCookerHandles() => Render("work/cooker-handles-160");
        public static void RenderFloorGenerator() => Render("work/floor-generator-162",true);
        public static void RenderFullBodyCrank() => Render("work/full-body-crank-163",true);

        static void Render(string output,bool savedScene=false)
        {
            Directory.CreateDirectory(output+"/frames");
            FishBuild.UseRebuilt=true;
            if(savedScene)EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
            else FishBuild.CreateScene();
            var companion=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var model=companion.model;var motion=new FishMotion(model){randomActions=false};
            var camera=companion.view;camera.backgroundColor=Color.white;
            float restingViewSize=camera.orthographicSize;Vector3 restingViewPosition=camera.transform.position;
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots=new MeshFilter[skins.Length];
            var bones=model.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
            float[] lengths=new float[4],legLengths=new float[4];
            string[] sides={"L","R"};
            for(int side=0;side<2;side++)
            {
                string prefix="J_Bip_"+sides[side]+"_";
                lengths[side*2]=Vector3.Distance(bones[prefix+"UpperArm"].position,bones[prefix+"LowerArm"].position);
                lengths[side*2+1]=Vector3.Distance(bones[prefix+"LowerArm"].position,bones[prefix+"Hand"].position);
                legLengths[side*2]=Vector3.Distance(bones[prefix+"UpperLeg"].position,bones[prefix+"LowerLeg"].position);
                legLengths[side*2+1]=Vector3.Distance(bones[prefix+"LowerLeg"].position,bones[prefix+"Foot"].position);
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
            using(var station=new FishWorkStation(model,companion.workPropShader,159))
            try
            {
                camera.targetTexture=target;
                Action<string> capture=path=>{
                    FishWorkStation.FrameWorkingCamera(camera,restingViewSize,restingViewPosition,motion.WorkWeight);
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
                    foreach(var pixel in image.GetPixels32())report.minimumOpaqueAlpha=Mathf.Min(report.minimumOpaqueAlpha,pixel.a/255f);
                    File.WriteAllBytes(output+"/"+path+".png",image.EncodeToPNG());
                };
                motion.Tick(0,1,0,0,"idle",false);station.Tick(0,motion.WorkWeight,0);capture("idle");
                if(station.Visible)throw new InvalidOperationException("Work props visible while idle.");
                // Settle the same exponential blend used by the actual player.
                for(int i=0;i<150;i++)motion.Tick(i/30f,1/30f,0,0,"working",false);
                motion.Tick(.5f,1,0,0,"working",false);station.Tick(.5f,1,5);
                report.firstFiveStyles=Enumerable.Range(0,5).Select(i=>station.CookerStyleAt(i).ToString()).ToArray();
                if(report.firstFiveStyles.Distinct().Count()!=FishWorkStation.CookerStyleCount)
                    throw new InvalidOperationException("Shuffled cooker set did not include every design.");
                var shells=station.Root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Cooker shell ")).ToArray();
                report.distinctShellGeometries=shells.Select(f=>string.Join(";",f.sharedMesh.vertices.Select(p=>p.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))).Distinct().Count();
                if(report.distinctShellGeometries!=FishWorkStation.CookerStyleCount)
                    throw new InvalidOperationException("Cooker variants differ only by their paint.");
                capture("all-five-styles");RenderCatalog(camera,station,snapshots,output);
                var initialStyles=Enumerable.Range(0,3).Select(i=>station.CookerStyleAt(i)).ToArray();
                Vector3 firstSteam=Vector3.zero;
                for(int f=0;f<48;f++)
                {
                    float time=f/24f;motion.Tick(time,1/24f,0,0,"working",false);station.Tick(time,motion.WorkWeight,3);
                    if(station.CookerCount!=3||!station.Visible)throw new InvalidOperationException("Missing job cooker.");
                    for(int i=0;i<3;i++){if(station.CookerStyleAt(i)!=initialStyles[i])throw new InvalidOperationException("Cooker appearance changed while working.");report.stableStyleChecks++;}
                    Vector3 right=model.InverseTransformPoint(bones["J_Bip_R_Hand"].position),left=station.Root.InverseTransformPoint(bones["J_Bip_L_Hand"].position);
                    report.maxGeneratorFloorDrift=Mathf.Max(report.maxGeneratorFloorDrift,station.Root.localPosition.magnitude,Quaternion.Angle(station.Root.localRotation,Quaternion.identity));
                    report.maxWorkingYawError=Mathf.Max(report.maxWorkingYawError,Mathf.Abs(Mathf.DeltaAngle(model.localEulerAngles.y,FishMotion.WorkingYawAt(time))));
                    report.maxRightWristError=Mathf.Max(report.maxRightWristError,Vector3.Distance(right,FishMotion.BalanceWristAt(time)));
                    report.maxLeftWristError=Mathf.Max(report.maxLeftWristError,Vector3.Distance(left,FishWorkStation.CrankGrip(time)+Vector3.up*.025f));
                    var actualGrip=station.Root.Find("Hand crank/Crank grip");
                    report.maxCrankPhaseError=Mathf.Max(report.maxCrankPhaseError,Vector3.Distance(station.Root.InverseTransformPoint(actualGrip.position),FishWorkStation.CrankGrip(time)));
                    var wobble=station.Root.Find("Rice cooker 1/Cooker wobble");
                    report.maxCookerRockDegrees=Mathf.Max(report.maxCookerRockDegrees,Quaternion.Angle(wobble.localRotation,Quaternion.identity));
                    report.maxLidLift=Mathf.Max(report.maxLidLift,wobble.Find("Bouncing lid").localPosition.y);
                    var steam=wobble.Find("Steam").GetComponent<MeshFilter>().sharedMesh;
                    report.maxSteamAlpha=Mathf.Max(report.maxSteamAlpha,steam.colors.Max(c=>c.a));
                    if(f==0)firstSteam=steam.vertices[0];
                    report.maxSteamTravel=Mathf.Max(report.maxSteamTravel,Vector3.Distance(firstSteam,steam.vertices[0]));
                    report.maxCookerPhaseDifference=Mathf.Max(report.maxCookerPhaseDifference,Quaternion.Angle(wobble.localRotation,station.Root.Find("Rice cooker 2/Cooker wobble").localRotation));
                    foreach(var wire in station.Root.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="Power cable"))
                    {
                        var vertices=wire.sharedMesh.vertices;
                        Vector3 end=wire.transform.TransformPoint((vertices[vertices.Length-1]+vertices[vertices.Length-2])*.5f);
                        Vector3 plug=wire.transform.parent.Find("Cooker wobble").TransformPoint(FishWorkStation.CookerPort);
                        report.maxCableEndpointError=Mathf.Max(report.maxCableEndpointError,Vector3.Distance(end,plug));
                    }
                    for(int side=0;side<2;side++)
                    {
                        string prefix="J_Bip_"+sides[side]+"_";
                        report.maxArmLengthChange=Mathf.Max(report.maxArmLengthChange,Mathf.Abs(Vector3.Distance(bones[prefix+"UpperArm"].position,bones[prefix+"LowerArm"].position)-lengths[side*2]),Mathf.Abs(Vector3.Distance(bones[prefix+"LowerArm"].position,bones[prefix+"Hand"].position)-lengths[side*2+1]));
                    }
                    report.minRightFacing=Mathf.Min(report.minRightFacing,Vector3.Dot(camera.transform.right,model.forward));
                    float pitch=Mathf.DeltaAngle(0,bones["J_Bip_C_Chest"].localEulerAngles.x);
                    report.minChestPitch=Mathf.Min(report.minChestPitch,pitch);report.maxChestPitch=Mathf.Max(report.maxChestPitch,pitch);
                    report.minRootY=Mathf.Min(report.minRootY,model.localPosition.y);report.maxRootY=Mathf.Max(report.maxRootY,model.localPosition.y);
                    for(int side=0;side<2;side++) {
                        string prefix="J_Bip_"+sides[side]+"_";var hip=bones[prefix+"UpperLeg"];var knee=bones[prefix+"LowerLeg"];var foot=bones[prefix+"Foot"];
                        report.maxFootDrift=Mathf.Max(report.maxFootDrift,Vector3.Distance(motion.SoleContactPosition(sides[side]),motion.PlantedFootPosition(sides[side])));
                        report.maxLegLengthChange=Mathf.Max(report.maxLegLengthChange,Mathf.Abs(Vector3.Distance(hip.position,knee.position)-legLengths[side*2]),Mathf.Abs(Vector3.Distance(knee.position,foot.position)-legLengths[side*2+1]));
                        float flex=Vector3.Angle(knee.position-hip.position,foot.position-knee.position);report.minKneeFlex=Mathf.Min(report.minKneeFlex,flex);report.maxKneeFlex=Mathf.Max(report.maxKneeFlex,flex);
                    }
                    capture("frames/frame"+f.ToString("D3"));report.sampledFrames++;
                    if(output.EndsWith("163")&&new[]{0,9,17,26}.Contains(f)) {station.Root.gameObject.SetActive(false);capture("body-only-"+f);station.Root.gameObject.SetActive(true);}
                }
                foreach(int count in new[]{1,3,5,2,0,8,1,0})
                {
                    var previousStyles=Enumerable.Range(0,station.CookerCount).Select(i=>station.CookerStyleAt(i)).ToArray();
                    motion.Tick(.5f,1,0,0,"working",false);
                    station.Tick(.5f,1,count);
                    if(station.CookerCount!=count)throw new InvalidOperationException("Cooker count differs from active jobs.");
                    for(int i=0;i<Mathf.Min(previousStyles.Length,count);i++){if(previousStyles[i]!=station.CookerStyleAt(i))throw new InvalidOperationException("Existing cookers rerolled when count changed.");report.stableStyleChecks++;}
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
                            Vector3 end=filter.transform.TransformPoint((vertices[vertices.Length-1]+vertices[vertices.Length-2])*.5f);
                            Vector3 plug=filter.transform.parent.Find("Cooker wobble").TransformPoint(FishWorkStation.CookerPort);
                            report.maxCableEndpointError=Mathf.Max(report.maxCableEndpointError,Vector3.Distance(end,plug));
                        }
                    }
                    capture("jobs-"+count);
                }
                foreach(string state in new[]{"waiting","complete","idle","error"})
                {
                    for(int i=0;i<90;i++)motion.Tick(3+i/30f,1/30f,0,0,state,false);
                    report.maxNonWorkYaw=Mathf.Max(report.maxNonWorkYaw,Quaternion.Angle(model.localRotation,Quaternion.identity));
                    FishWorkStation.FrameWorkingCamera(camera,restingViewSize,restingViewPosition,motion.WorkWeight);
                    report.maxNonWorkCameraDifference=Mathf.Max(report.maxNonWorkCameraDifference,Mathf.Abs(camera.orthographicSize-restingViewSize),Vector3.Distance(camera.transform.position,restingViewPosition));
                    station.Tick(6,motion.WorkWeight,0);if(station.Visible)throw new InvalidOperationException("Work props persist after work.");
                }
                for(int i=0;i<90;i++)motion.Tick(6+i/30f,1/30f,0,0,"working",true);
                station.Tick(9,motion.WorkWeight,3);if(station.Visible)throw new InvalidOperationException("Crank props persist during drag.");
                var orders=new System.Collections.Generic.HashSet<string>();
                for(int seed=159;seed<167;seed++)using(var probe=new FishWorkStation(model,companion.workPropShader,seed))
                {probe.Tick(.5f,1,5);orders.Add(string.Join(",",Enumerable.Range(0,5).Select(i=>probe.CookerStyleAt(i).ToString())));}
                report.distinctRandomOrders=orders.Count;
                if(report.distinctRandomOrders<2)throw new InvalidOperationException("Cooker order is fixed instead of randomized.");
                if(savedScene) {
                    Directory.CreateDirectory(output+"/transition-in");Directory.CreateDirectory(output+"/transition-out");
                    foreach(bool entering in new[]{true,false})for(int i=0;i<30;i++) {
                        float time=10+i*.04f;motion.Tick(time,.04f,0,0,entering?"working":"idle",false);
                        station.Tick(time,motion.WorkWeight,entering?3:0);
                        capture((entering?"transition-in/":"transition-out/")+"frame"+i.ToString("D3"));report.transitionFrames++;
                    }
                }
                if(output.EndsWith("163")&&(report.maxFootDrift>.00001f||report.maxLegLengthChange>.000001f||report.maxChestPitch-report.minChestPitch<40||report.maxRootY-report.minRootY<.025f||report.maxKneeFlex-report.minKneeFlex<15||report.minRightFacing<.3f))
                    throw new InvalidOperationException("Whole-body effort or planted stance validation failed: "+JsonUtility.ToJson(report));
                if(report.maxGeneratorFloorDrift>.000001f||report.maxWorkingYawError>.001f||report.maxNonWorkYaw>.001f||report.maxNonWorkCameraDifference>.00001f)
                    throw new InvalidOperationException("Generator floor frame or actor turn validation failed: "+JsonUtility.ToJson(report));
                if(report.maxRightWristError>.0002f||report.maxLeftWristError>.0002f||report.maxArmLengthChange>.000001f||
                    report.maxCrankPhaseError>.000001f||report.maxCableEndpointError>.000001f||report.minimumViewportMargin<.02f)
                    throw new InvalidOperationException("Crank contact or arm length validation failed: "+JsonUtility.ToJson(report));
                if(report.maxCookerRockDegrees<1||report.maxLidLift<.003f||report.maxSteamAlpha<.6f||report.maxSteamTravel<.02f||report.maxCookerPhaseDifference<1)
                    throw new InvalidOperationException("Cookers are not visibly boiling: "+JsonUtility.ToJson(report));
                if(report.minimumOpaqueAlpha<.999f)
                    throw new InvalidOperationException("Steam reduced the opaque background alpha: "+JsonUtility.ToJson(report));
                report.passed=true;File.WriteAllText(output+"/validation.json",JsonUtility.ToJson(report,true));
                Debug.Log("Hand generator preview passed: "+JsonUtility.ToJson(report));
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
                for(int s=0;s<skins.Length;s++){if(snapshots[s].sharedMesh)UnityEngine.Object.DestroyImmediate(snapshots[s].sharedMesh);UnityEngine.Object.DestroyImmediate(snapshots[s].gameObject);skins[s].enabled=true;}
            }
        }

        static void RenderCatalog(Camera camera,FishWorkStation station,MeshFilter[] snapshots,string output)
        {
            var transforms=station.Root.GetComponentsInChildren<Transform>(true);
            var active=transforms.Select(t=>t.gameObject.activeSelf).ToArray();
            var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();
            var snapshotActive=snapshots.Select(s=>s.gameObject.activeSelf).ToArray();
            Vector3 cameraPosition=camera.transform.position;Quaternion cameraRotation=camera.transform.rotation;
            float size=camera.orthographicSize,aspect=camera.aspect;var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32){antiAliasing=4};var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
            try
            {
                foreach(var s in snapshots)s.gameObject.SetActive(false);
                foreach(Transform t in station.Root)t.gameObject.SetActive(false);
                camera.targetTexture=target;camera.aspect=1;
                for(int i=0;i<5;i++)
                {
                    var pot=station.Root.Find("Rice cooker "+(i+1));pot.gameObject.SetActive(true);
                    pot.Find("Power cable").gameObject.SetActive(false);pot.Find("Electric pulse").gameObject.SetActive(false);
                    var wobble=pot.Find("Cooker wobble");wobble.localPosition=Vector3.zero;wobble.localRotation=Quaternion.identity;
                    wobble.Find("Steam").gameObject.SetActive(false);var lid=wobble.Find("Bouncing lid");lid.localPosition=Vector3.zero;lid.localRotation=Quaternion.identity;
                    float scale=pot.lossyScale.x;Vector3 center=pot.TransformPoint(Vector3.up*.050f);
                    camera.orthographicSize=.0925f*scale;camera.transform.position=center+new Vector3(0,.050f*scale,.5f);camera.transform.LookAt(center);
                    camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();
                    File.WriteAllBytes(output+"/style-"+station.CookerStyleAt(i)+".png",image.EncodeToPNG());pot.gameObject.SetActive(false);
                }
            }
            finally
            {
                for(int i=0;i<transforms.Length;i++){transforms[i].gameObject.SetActive(active[i]);transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];}
                for(int i=0;i<snapshots.Length;i++)snapshots[i].gameObject.SetActive(snapshotActive[i]);
                camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.orthographicSize=size;camera.aspect=aspect;camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
                UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
            }
        }
        static bool Finite(Vector3 p)=>!(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z));
    }
}
