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
            public int faceLayoutChecks;
            public int partialCompletionFrames;
            public float minCompletionWorkWeight=1,completionSteamAtPause;
            public bool completionOldHideReproduced,completionResumed,lastJobHidesEquipment;
            public float batteryWidth,boltWidth,boltHeight,boltTopArea,boltBottomArea,boltAreaMismatch;
            public bool batteryFaceFilled;
            public float badgeTopGap,badgeBottomGap,badgeCenteringError;
            public float batteryOuterCornerSetback,batteryShoulderSetback;
            public int shellChecks,dualRowChecks,whiteFeedSamples;
            public float minShellThickness=1,minStripCaseGap=1,minWhiteFeedRGB=1,minTubeNormalDot=1;
            public int slackCordChecks,groundedCordSamples;
            public float minCordSlackRatio=10,meanCordSlackRatio,minCordFloor=1,maxPrimaryStripDistance;
            public int supplyGraphChecks,lifecycleChecks,maxSupplyCapacityA,maxSupplyCapacityB,maxVisibleSupplies,persistentIdleChecks;
            public float maxSupplyFeedError,maxSupplyPlacementDrift;
            [NonSerialized] public System.Collections.Generic.Dictionary<int,Vector3> supplyPositions=new System.Collections.Generic.Dictionary<int,Vector3>();
            public bool directOne,directTwo,threeUsesOneFourWay,fourUsesFourAndFive,upgradedBoardsRetained,reloadedUpgradesRetained;
            public int crackleActiveCountStates,minActiveArcs=8,maxActiveArcs,arcZeroBursts,arcZeroSilentFrames,arcZeroActiveFrames,maxArcZeroRun,maxActiveBranches;
            public float maxSparkAlpha;
            public float maxBaseVertexDrift,maxBaseRotationDegrees,maxFootMountError,minFootMountLength=1,maxFootMountLength,maxMachinePivotError;
            public float maxMachineRockDegrees,maxMachineFloorError,maxCrankMountError,maxOrbitRadiusError,maxCookerPlacementDrift;
            public int machineLabelCount;
            public int chargeStateCount,chargeRises,chargeFalls,chargeResets,unexpectedChargeDecreases,unexpectedChargeIncreases,minChargeCells=5,maxChargeCells,socketCount;
            public int[] chargeSequence;
            public float maxElectricAlpha,minElectricSampleAlpha=1,maxElectricSampleAlpha,maxElectricTravel,maxCableSourceError;
            public float maxCharacterPixelScaleError,scenePixelScaleVsPreviousWork;
            public int canvasWidth,canvasHeight;
            public int longTimeSamples;
            public int transitionFrames,activeBoundsMeshesChecked,hiddenBoundsMeshesSkipped;
            public int sampledFrames, countTransitions, maxCookers, avatarTriangles, maxPropTriangles;
            public float maxRightWristError, maxLeftWristError, maxArmLengthChange, maxCrankPhaseError, maxCableEndpointError;
            public float maxCookerRockDegrees, maxLidLift, maxSteamAlpha, maxSteamTravel, maxCookerPhaseDifference;
            public float maxGeneratorFloorDrift, maxWorkingYawError, maxNonWorkYaw, maxNonWorkCameraDifference;
            public float maxFootDrift,maxLegLengthChange,minKneeFlex=180,maxKneeFlex;
            public float minChestPitch=180,maxChestPitch,minRootY=100,maxRootY=-100,minRightFacing=1;
            public float minForearmAxisDistance=100,crankCenterHeight,measuredCrankRadius;
            public float maxForearmTwist,maxRightGraspError,maxLeftGraspError,minGraspSeparation=100;
            public float maxGraspCenterError,maxGraspAxisError,minShoulderPlaneDistance=100,maxHandAngularStep;
            public string minimumMarginContext;
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
        public static void DiagnoseCrankContact() => Render("work/crank-sync-164/baseline",true);
        public static void RenderCrankSynchronization() => Render("work/crank-sync-164",true);

        public static void RenderTwoHandCrank() => Render("work/two-hand-crank-165",true);

        public static void RenderLowerWideCrank() => Render("work/lower-wide-crank-166",true);

        public static void RenderFullSizeWorkStage() => Render("work/full-size-work-stage-167",true);

        public static void RenderElectricGenerator() => Render("work/electric-generator-168",true);

        public static void RenderRockingGenerator() => Render("work/rocking-generator-169",true);

        public static void RenderFixedGeneratorFeet() => Render("work/fixed-generator-feet-170",true);

        public static void RenderCracklingElectricity() => Render("work/crackling-electricity-171",true);

        public static void RenderSplitterChain() => Render("work/splitter-chain-172",true);

        public static void RenderUpgradeablePowerStrips() => Render("work/upgradeable-power-strips-174",true);

        public static void RenderLooseCookerCords() => Render("work/loose-cooker-cords-175",true);

        public static void RenderSolidWhiteCordStrips() => Render("work/solid-white-cord-strips-176",true);

        public static void RenderFillResetMeter() => Render("work/fill-reset-meter-177",true);

        public static void RenderBatteryRoundedBolt() => Render("work/battery-rounded-bolt-178",true);

        public static void RenderDarkBatteryBody() => Render("work/dark-battery-body-179",true);

        public static void RenderRoundedBatteryJunction() => Render("work/rounded-battery-junction-180",true);

        public static void RenderStraightBatteryJunction() => Render("work/straight-battery-junction-181",true);

        public static void RenderDarkGreenBattery() => Render("work/dark-green-battery-182",true);

        public static void RenderBlackBatteryCells() => Render("work/black-battery-cells-183",true);

        public static void RenderLightningCircle() => Render("work/lightning-circle-184",true);

        public static void RenderCenteredPowerBadge() => Render("work/centered-lightning-185",true);

        public static void RenderRemainingJobEquipment() => Render("work/remaining-job-equipment-186",true);

        static void Render(string output,bool savedScene=false)
        {
            bool faceLayout=output.EndsWith("178")||(output.EndsWith("179")||(output.EndsWith("180")||(output.EndsWith("181")||(output.EndsWith("182")||(output.EndsWith("183")||(output.EndsWith("184")||(output.EndsWith("185")||output.EndsWith("186"))))))));
            bool solidStrips=output.EndsWith("176")||output.EndsWith("177")||faceLayout;
            bool looseCords=output.EndsWith("175")||solidStrips;
            bool supplyReview=output.EndsWith("174")||looseCords;
            bool splitterReview=output.EndsWith("172");
            bool crackleReview=output.EndsWith("171")||splitterReview||supplyReview;
            bool fixedFeetReview=output.EndsWith("170")||crackleReview;
            bool rockReview=output.EndsWith("169")||fixedFeetReview;
            bool electricReview=output.EndsWith("168")||rockReview;
            bool fullSize=output.EndsWith("167")||electricReview;
            bool movingOrbit=output.EndsWith("166")||fullSize;
            bool twoHand=output.EndsWith("165")||movingOrbit;
            bool refined=twoHand||output.EndsWith("164");
            bool fullBody=refined||output.EndsWith("163");
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
            int previewWidth=fullSize?FishCompanion.NativeWidth*2:480,previewHeight=fullSize?FishCompanion.NativeHeight*2:640;
            report.canvasWidth=previewWidth;report.canvasHeight=previewHeight;
            var target=new RenderTexture(previewWidth,previewHeight,24,RenderTextureFormat.ARGB32){antiAliasing=4};
            var image=new Texture2D(previewWidth,previewHeight,TextureFormat.RGBA32,false);
            for(int s=0;s<skins.Length;s++)
            {
                var go=new GameObject("Generator preview snapshot");go.transform.SetParent(skins[s].transform,false);
                snapshots[s]=go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>().sharedMaterials=skins[s].sharedMaterials;skins[s].enabled=false;
            }
            using(var station=new FishWorkStation(model,companion.workPropShader,159))
            try
            {
                camera.targetTexture=target;
                Vector3[] fixedPadVertices=null;
                Action<string> capture=path=>{
                    if(fixedFeetReview) {
                        var footing=station.Root.Find("Generator base");var pads=footing.Find("Fixed generator feet").GetComponent<MeshFilter>();var points=pads.sharedMesh.vertices;
                        if(fixedPadVertices==null)fixedPadVertices=points.Select(p=>pads.transform.TransformPoint(p)).ToArray();
                        for(int i=0;i<points.Length;i++)report.maxBaseVertexDrift=Mathf.Max(report.maxBaseVertexDrift,Vector3.Distance(pads.transform.TransformPoint(points[i]),fixedPadVertices[i]));
                        report.maxBaseRotationDegrees=Mathf.Max(report.maxBaseRotationDegrees,Quaternion.Angle(footing.localRotation,FishWorkStation.GeneratorRotation));
                    }
                    if(supplyReview)CheckSupply(station,report);
                    if(looseCords)CheckLooseCords(station,report);
                    if(solidStrips)CheckSolidStrips(station,report);
                    if(faceLayout)CheckFaceLayout(station,report);
                    FishWorkStation.FrameWorkingCamera(camera,restingViewSize,restingViewPosition,motion.WorkWeight);
                    if(fullSize) {
                        Vector3 p=camera.transform.position+camera.transform.forward;
                        float pixels=Mathf.Abs(camera.WorldToViewportPoint(p+camera.transform.up*.1f).y-camera.WorldToViewportPoint(p).y)*previewHeight;
                        float originalIdlePixels=.1f*640/(2*restingViewSize);
                        report.maxCharacterPixelScaleError=Mathf.Max(report.maxCharacterPixelScaleError,Mathf.Abs(pixels/originalIdlePixels-1));
                        report.scenePixelScaleVsPreviousWork=pixels/(originalIdlePixels/1.30f);
                    }
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
                            float margin=Mathf.Min(v.x,1-v.x,v.y,1-v.y);
                            if(margin<report.minimumViewportMargin){report.minimumViewportMargin=margin;report.minimumMarginContext=path+" avatar "+skins[s].name+" camera "+camera.orthographicSize+" viewport "+v;}
                        }
                    }
                    foreach(var filter in station.Root.GetComponentsInChildren<MeshFilter>()) {
                        // Unity can enumerate this explicitly queried inactive
                        // subtree. Hidden equipment is not a viewport sample.
                        if(!filter.gameObject.activeInHierarchy){report.hiddenBoundsMeshesSkipped++;continue;}
                        report.activeBoundsMeshesChecked++;
                        foreach(var p in filter.sharedMesh.vertices) {
                            Vector3 v=camera.WorldToViewportPoint(filter.transform.TransformPoint(p));
                            float margin=Mathf.Min(v.x,1-v.x,v.y,1-v.y);
                            if(margin<report.minimumViewportMargin){report.minimumViewportMargin=margin;report.minimumMarginContext=path+" prop "+filter.name+" rootActive "+station.Root.gameObject.activeInHierarchy+" camera "+camera.orthographicSize+" viewport "+v;}
                        }
                    }
                    camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,previewWidth,previewHeight),0,0);image.Apply();
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
                Vector3 firstSteam=Vector3.zero;Quaternion[] previousPalms={Quaternion.identity,Quaternion.identity};
                var crackleCounts=new System.Collections.Generic.HashSet<int>();bool arcZeroWasActive=false;int arcZeroRun=0;
                Vector3[] cookerPositions=new Vector3[3];
                var chargeStates=new System.Collections.Generic.HashSet<int>();var chargeSequence=new System.Collections.Generic.List<int>();int lastCharge=-1;Vector3 firstElectric=Vector3.zero;
                for(int f=0;f<(electricReview?72:48);f++)
                {
                    float time=f/24f;motion.Tick(time,1/24f,0,0,"working",false);station.Tick(time,motion.WorkWeight,3);
                    if(rockReview) {
                        var machine=station.Root.Find("Floor generator");
                        report.maxMachineRockDegrees=Mathf.Max(report.maxMachineRockDegrees,Quaternion.Angle(machine.localRotation,FishWorkStation.GeneratorRotation));
                        float floor=float.PositiveInfinity;
                        var support=fixedFeetReview?station.Root.Find("Generator base/Fixed generator feet"):machine.Find("Generator housing");
                        foreach(var point in support.GetComponent<MeshFilter>().sharedMesh.vertices)if(fixedFeetReview||point.y<-.220f)
                            floor=Mathf.Min(floor,station.Root.InverseTransformPoint(support.TransformPoint(point)).y);
                        if(fixedFeetReview) {
                            var footing=station.Root.Find("Generator base");
                            report.maxMachinePivotError=Mathf.Max(report.maxMachinePivotError,Vector3.Distance(machine.TransformPoint(FishWorkStation.MachinePivot),footing.TransformPoint(FishWorkStation.MachinePivot)));
                            for(int i=0;i<4;i++) {
                                var mount=footing.Find("Flexible foot mount "+i);Vector3 lower=footing.Find("Fixed foot joint "+i).position,upper=machine.Find("Body foot joint "+i).position;
                                report.maxFootMountError=Mathf.Max(report.maxFootMountError,Vector3.Distance(mount.TransformPoint(Vector3.down*.5f),lower),Vector3.Distance(mount.TransformPoint(Vector3.up*.5f),upper));
                                report.minFootMountLength=Mathf.Min(report.minFootMountLength,Vector3.Distance(lower,upper));report.maxFootMountLength=Mathf.Max(report.maxFootMountLength,Vector3.Distance(lower,upper));
                            }
                        }
                        report.maxMachineFloorError=Mathf.Max(report.maxMachineFloorError,Mathf.Abs(floor-FishWorkStation.GeneratorFloorHeight));
                        report.maxCrankMountError=Mathf.Max(report.maxCrankMountError,Vector3.Distance(station.Root.Find("Hand crank").position,machine.TransformPoint(FishWorkStation.LocalCrankCenter)));
                        report.machineLabelCount=machine.GetComponentsInChildren<TextMesh>(true).Length;
                        for(int i=0;i<3;i++) {
                            Vector3 point=station.Root.Find("Rice cooker "+(i+1)).localPosition;
                            if(f==0)cookerPositions[i]=point;report.maxCookerPlacementDrift=Mathf.Max(report.maxCookerPlacementDrift,Vector3.Distance(point,cookerPositions[i]));
                        }
                    }
                    if(electricReview) {
                        var generator=station.Root.Find("Floor generator");int charge=0;
                        for(int cell=0;cell<5;cell++)if(generator.Find("Charge cell "+cell).gameObject.activeSelf)charge++;
                        chargeStates.Add(charge);report.minChargeCells=Mathf.Min(report.minChargeCells,charge);report.maxChargeCells=Mathf.Max(report.maxChargeCells,charge);
                        if(lastCharge!=charge)chargeSequence.Add(charge);
                        if(lastCharge>=0) {
                            if(charge>lastCharge){report.chargeRises++;if(charge!=lastCharge+1)report.unexpectedChargeIncreases++;}
                            if(charge<lastCharge){report.chargeFalls++;if(lastCharge==5&&charge==0)report.chargeResets++;else report.unexpectedChargeDecreases++;}
                        }
                        for(int cell=0;cell<5;cell++)if(generator.Find("Charge cell "+cell).gameObject.activeSelf!=(cell>=5-charge))throw new InvalidOperationException("Charge cells are not contiguous left-to-right.");
                        lastCharge=charge;
                        var lightning=generator.Find("Generator electricity").GetComponent<MeshFilter>().sharedMesh;
                        if(crackleReview) {
                            var colors=lightning.colors;const int pathSize=42;int active=0,branches=0;
                            for(int path=0;path<24;path++) {
                                float alpha=colors.Skip(path*pathSize).Take(pathSize).Max(c=>c.a);bool on=alpha>.05f;
                                if(path<8&&on)active++;if(path>=8&&on)branches++;
                                if(path==0) {
                                    if(on){report.arcZeroActiveFrames++;arcZeroRun++;if(!arcZeroWasActive)report.arcZeroBursts++;}
                                    else {report.arcZeroSilentFrames++;arcZeroRun=0;}
                                    report.maxArcZeroRun=Mathf.Max(report.maxArcZeroRun,arcZeroRun);arcZeroWasActive=on;
                                }
                            }
                            crackleCounts.Add(active);report.minActiveArcs=Mathf.Min(report.minActiveArcs,active);report.maxActiveArcs=Mathf.Max(report.maxActiveArcs,active);report.maxActiveBranches=Mathf.Max(report.maxActiveBranches,branches);
                            report.maxSparkAlpha=Mathf.Max(report.maxSparkAlpha,generator.Find("Generator crackle sparks").GetComponent<MeshFilter>().sharedMesh.colors.Max(c=>c.a));
                        }
                        report.maxElectricAlpha=Mathf.Max(report.maxElectricAlpha,lightning.colors.Max(c=>c.a));
                        report.minElectricSampleAlpha=Mathf.Min(report.minElectricSampleAlpha,lightning.colors[20].a);report.maxElectricSampleAlpha=Mathf.Max(report.maxElectricSampleAlpha,lightning.colors[20].a);
                        if(f==0)firstElectric=lightning.vertices[20];report.maxElectricTravel=Mathf.Max(report.maxElectricTravel,Vector3.Distance(firstElectric,lightning.vertices[20]));
                        report.socketCount=generator.GetComponentsInChildren<Transform>().Count(t=>t.name.StartsWith("AC socket "));
                        if(generator.Find("Output terminal"))throw new InvalidOperationException("Old output electrodes are still present");
                        foreach(var wire in station.Root.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="Power cable")) {
                            var points=wire.sharedMesh.vertices;Vector3 cableStart=wire.transform.TransformPoint((points[0]+points[1])*.5f);
                            report.maxCableSourceError=Mathf.Max(report.maxCableSourceError,Vector3.Distance(cableStart,station.CookerSourceAt(int.Parse(wire.transform.parent.name.Substring("Rice cooker ".Length))-1).position));
                        }
                    }
                    if(station.CookerCount!=3||!station.Visible)throw new InvalidOperationException("Missing job cooker.");
                    for(int i=0;i<3;i++){if(station.CookerStyleAt(i)!=initialStyles[i])throw new InvalidOperationException("Cooker appearance changed while working.");report.stableStyleChecks++;}
                    Vector3 right=station.Root.InverseTransformPoint(bones["J_Bip_R_Hand"].position),left=station.Root.InverseTransformPoint(bones["J_Bip_L_Hand"].position);
                    report.maxGeneratorFloorDrift=Mathf.Max(report.maxGeneratorFloorDrift,station.Root.localPosition.magnitude,Quaternion.Angle(station.Root.localRotation,Quaternion.identity));
                    report.maxWorkingYawError=Mathf.Max(report.maxWorkingYawError,Mathf.Abs(Mathf.DeltaAngle(model.localEulerAngles.y,FishMotion.WorkingYawAt(time))));
                    report.maxRightWristError=Mathf.Max(report.maxRightWristError,Vector3.Distance(right,station.Root.InverseTransformPoint(motion.CrankWrist("R"))));
                    report.maxLeftWristError=Mathf.Max(report.maxLeftWristError,Vector3.Distance(left,station.Root.InverseTransformPoint(motion.CrankWrist("L"))));
                    var actualGrip=station.Root.Find("Hand crank/Crank grip");
                    report.maxCrankPhaseError=Mathf.Max(report.maxCrankPhaseError,Vector3.Distance(station.Root.InverseTransformPoint(actualGrip.position),FishWorkStation.CrankGrip(time)));
                    Vector3[] graspCenters=new Vector3[2];
                    for(int handIndex=0;handIndex<2;handIndex++) {
                        string side=sides[handIndex];string prefix="J_Bip_"+side+"_";
                        Vector3 ka=bones[prefix+"Middle1"].position,kb=bones[prefix+"Middle2"].position,kc=bones[prefix+"Middle3"].position;
                        Vector3 ab=kb-ka,ac=kc-ka,normal=Vector3.Cross(ab,ac);
                        Vector3 grasp=ka+(Vector3.Cross(normal,ab)*ac.sqrMagnitude+Vector3.Cross(ac,normal)*ab.sqrMagnitude)/(2*normal.sqrMagnitude);
                        graspCenters[handIndex]=grasp;
                        Vector3 targetGrip=actualGrip.position+actualGrip.up*(side=="L"?1:-1)*FishWorkStation.HandSpacing*.5f;
                        report.minForearmAxisDistance=Mathf.Min(report.minForearmAxisDistance,Vector3.ProjectOnPlane(targetGrip-bones[prefix+"LowerArm"].position,actualGrip.up).magnitude);
                        float error=Vector3.Distance(grasp,targetGrip);
                        if(side=="L")report.maxLeftGraspError=Mathf.Max(report.maxLeftGraspError,error);else report.maxRightGraspError=Mathf.Max(report.maxRightGraspError,error);
                        report.maxGraspCenterError=Mathf.Max(report.maxGraspCenterError,error);
                        report.maxGraspAxisError=Mathf.Max(report.maxGraspAxisError,Mathf.Acos(Mathf.Clamp01(Mathf.Abs(Vector3.Dot(normal.normalized,actualGrip.up))))*Mathf.Rad2Deg);
                        report.maxForearmTwist=Mathf.Max(report.maxForearmTwist,Mathf.Abs(motion.CrankTwist(side)));
                        report.minShoulderPlaneDistance=Mathf.Min(report.minShoulderPlaneDistance,Vector3.ProjectOnPlane(bones[prefix+"UpperArm"].position-station.Root.Find("Hand crank").position,actualGrip.up).magnitude);
                        if(f>0)report.maxHandAngularStep=Mathf.Max(report.maxHandAngularStep,Quaternion.Angle(previousPalms[handIndex],bones[prefix+"Hand"].rotation));previousPalms[handIndex]=bones[prefix+"Hand"].rotation;
                    }
                    report.crankCenterHeight=station.Root.Find("Hand crank").localPosition.y;
                    report.measuredCrankRadius=Vector3.ProjectOnPlane(actualGrip.position-station.Root.Find("Hand crank").position,actualGrip.up).magnitude;
                    report.maxOrbitRadiusError=Mathf.Max(report.maxOrbitRadiusError,Mathf.Abs(report.measuredCrankRadius-FishWorkStation.CrankRadius));
                    report.minGraspSeparation=Mathf.Min(report.minGraspSeparation,Vector3.Distance(graspCenters[0],graspCenters[1]));
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
                    if(electricReview)RenderGeneratorDetail(camera,station,snapshots,output,f);
                    if(fullBody&&new[]{0,9,17,26}.Contains(f)) {station.Root.gameObject.SetActive(false);capture("body-only-"+f);station.Root.gameObject.SetActive(true);}
                    if(refined&&new[]{0,9,17,26}.Contains(f))RenderCrankDetail(camera,station,bones["J_Bip_L_LowerArm"],output,f);
                }
                if(refined)foreach(float time in new[]{60f,3600f,86400f,172800f}) {
                    motion.Tick(time,1,0,0,"working",false);station.Tick(time,1,3);
                    var knob=station.Root.Find("Hand crank/Crank grip");
                    report.maxCrankPhaseError=Mathf.Max(report.maxCrankPhaseError,Vector3.Distance(station.Root.InverseTransformPoint(knob.position),FishWorkStation.CrankGrip(time)));
                    foreach(string side in sides) {
                        string prefix="J_Bip_"+side+"_";
                        Vector3 a=bones[prefix+"Middle1"].position,ab=bones[prefix+"Middle2"].position-a,ac=bones[prefix+"Middle3"].position-a,n=Vector3.Cross(ab,ac);
                        Vector3 center=a+(Vector3.Cross(n,ab)*ac.sqrMagnitude+Vector3.Cross(ac,n)*ab.sqrMagnitude)/(2*n.sqrMagnitude);
                        Vector3 targetGrip=knob.position+knob.up*(side=="L"?1:-1)*FishWorkStation.HandSpacing*.5f;
                        report.maxGraspCenterError=Mathf.Max(report.maxGraspCenterError,Vector3.Distance(center,targetGrip));
                    }
                    capture("long-run-"+time.ToString("F0"));report.longTimeSamples++;
                }
                int powerStep=0;
                foreach(int count in supplyReview?new[]{1,2,3,4,5,7,8,9,10,2,1,0,12,18,24,4,0}:new[]{1,3,5,2,0,8,1,0})
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
                    if(supplyReview){capture("step-"+powerStep.ToString("D2")+"-jobs-"+count);RenderSupplyDetail(camera,station,snapshots,output,powerStep++,count);}
                }
                foreach(string state in new[]{"waiting","complete","idle","error"})
                {
                    for(int i=0;i<90;i++)motion.Tick(3+i/30f,1/30f,0,0,state,false);
                    report.maxNonWorkYaw=Mathf.Max(report.maxNonWorkYaw,Quaternion.Angle(model.localRotation,Quaternion.identity));
                    FishWorkStation.FrameWorkingCamera(camera,restingViewSize,restingViewPosition,motion.WorkWeight);
                    report.maxNonWorkCameraDifference=Mathf.Max(report.maxNonWorkCameraDifference,Mathf.Abs(camera.orthographicSize-restingViewSize*(fullSize?1.30f:1)),Vector3.Distance(camera.transform.position,restingViewPosition));
                    station.Tick(6,motion.WorkWeight,0);if(station.WorkingVisible)throw new InvalidOperationException("Work equipment persists after work.");
                    if(supplyReview){CheckSupply(station,report);capture("rest-"+state);}
                }
                for(int i=0;i<90;i++)motion.Tick(6+i/30f,1/30f,0,0,"working",true);
                station.Tick(9,motion.WorkWeight,3);if(station.WorkingVisible)throw new InvalidOperationException("Crank props persist during drag.");
                if(supplyReview)CheckSupply(station,report);
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
                if(output.EndsWith("186")) {
                    Directory.CreateDirectory(output+"/completion");
                    motion.Play(FishMotion.Gesture.None,99);motion.Tick(100,1,0,0,"working",false);
                    station.Tick(100,motion.WorkWeight,3,true);capture("completion-working-3");
                    var remainingStyles=Enumerable.Range(0,2).Select(n=>station.CookerStyleAt(n)).ToArray();
                    motion.Play(FishMotion.Gesture.Cheer,101);
                    for(int frame=0;frame<90;frame++) {
                        float time=101+frame/30f;motion.Tick(time,1/30f,0,0,"working",false);
                        station.Tick(time,motion.WorkWeight,2,true);
                        if(!station.WorkingVisible||station.CookerCount!=2||Enumerable.Range(0,2).Any(n=>station.CookerStyleAt(n)!=remainingStyles[n]||!station.Root.Find("Rice cooker "+(n+1)).gameObject.activeInHierarchy))
                            throw new InvalidOperationException("Remaining equipment disappeared or cooker styles changed during completion.");
                        report.minCompletionWorkWeight=Mathf.Min(report.minCompletionWorkWeight,motion.WorkWeight);
                        if(motion.WorkWeight<.01f) {
                            var steam=station.Root.Find("Rice cooker 1/Cooker wobble/Steam").GetComponent<MeshFilter>().sharedMesh;
                            report.completionSteamAtPause=Mathf.Max(report.completionSteamAtPause,steam.colors.Max(c=>c.a));
                        }
                        if(frame==30) {
                            station.Root.gameObject.SetActive(false);
                            using(var oldBehavior=new FishWorkStation(model,companion.workPropShader,159,station.PowerUpgradeMask)) {
                                oldBehavior.Root.localPosition=station.Root.localPosition;oldBehavior.Root.localRotation=station.Root.localRotation;oldBehavior.Root.localScale=station.Root.localScale;
                                oldBehavior.Tick(time,1,2);oldBehavior.Tick(time,motion.WorkWeight,2);
                                report.completionOldHideReproduced=!oldBehavior.WorkingVisible;
                                capture("completion-before");
                            }
                            station.Root.gameObject.SetActive(true);
                            capture("completion-after");
                        }
                        capture("completion/frame"+frame.ToString("D3"));report.partialCompletionFrames++;
                    }
                    report.completionResumed=station.WorkingVisible&&motion.WorkWeight>.8f;
                    motion.Play(FishMotion.Gesture.None,109);motion.Tick(109,1,0,0,"working",false);
                    station.Tick(109,motion.WorkWeight,1,true);capture("last-job-working");
                    motion.Play(FishMotion.Gesture.Cheer,110);motion.Tick(110,1/30f,0,0,"complete",false);
                    station.Tick(110,motion.WorkWeight,0,false);
                    report.lastJobHidesEquipment=!station.WorkingVisible&&station.CookerCount==0;
                    capture("last-job-complete");
                    if(report.partialCompletionFrames!=90||report.minCompletionWorkWeight>.01f||report.completionSteamAtPause<.6f||!report.completionOldHideReproduced||!report.completionResumed||!report.lastJobHidesEquipment)
                        throw new InvalidOperationException("Partial/final job completion equipment lifecycle failed: "+JsonUtility.ToJson(report));
                }
                if(output.EndsWith("180")&&(report.batteryOuterCornerSetback<.008f||report.batteryShoulderSetback<.002f))
                    throw new InvalidOperationException("Battery outer corners or terminal shoulders are not rounded: "+JsonUtility.ToJson(report));
                if((output.EndsWith("181")||(output.EndsWith("182")||(output.EndsWith("183")||(output.EndsWith("184")||(output.EndsWith("185")||output.EndsWith("186"))))))&&(report.batteryOuterCornerSetback<.008f||report.batteryShoulderSetback>1e-7f))
                    throw new InvalidOperationException("Rounded battery body or direct terminal join failed: "+JsonUtility.ToJson(report));
                if((output.EndsWith("185")||output.EndsWith("186"))&&(report.badgeTopGap<.015f||report.badgeBottomGap<.015f||report.badgeCenteringError>1e-7f))
                    throw new InvalidOperationException("Power badge is not centered between battery and sockets: "+JsonUtility.ToJson(report));
                if(faceLayout&&(report.faceLayoutChecks<100||report.batteryWidth<.16f||report.boltWidth<.064f||report.boltHeight<.10f||report.boltTopArea<.0008f||report.boltAreaMismatch>.001f||!report.batteryFaceFilled))
                    throw new InvalidOperationException("Solid battery body or balanced full rounded lightning failed: "+JsonUtility.ToJson(report));
                if(solidStrips&&(report.shellChecks<100||report.dualRowChecks<8||report.whiteFeedSamples<100||report.minShellThickness<.026f||report.minStripCaseGap<.025f||report.minWhiteFeedRGB<.90f||report.minTubeNormalDot<=0))
                    throw new InvalidOperationException("Solid dual-row strips, white feed or generator clearance failed: "+JsonUtility.ToJson(report));
                if(looseCords) {
                    report.meanCordSlackRatio/=Mathf.Max(1,report.slackCordChecks);
                    if(report.slackCordChecks<200||report.minCordSlackRatio<1.05f||report.meanCordSlackRatio<1.12f||report.groundedCordSamples<report.slackCordChecks*8||report.minCordFloor<FishWorkStation.GeneratorFloorHeight+.002f||report.maxPrimaryStripDistance>.46f)
                        throw new InvalidOperationException("Cooker cords are taut, leave the floor or strips are too far from generator: "+JsonUtility.ToJson(report));
                }
                if(supplyReview) {
                    TestSupplyLifecycle(model,companion.workPropShader,report);
                    if(report.supplyGraphChecks<100||report.maxSupplyFeedError>.000001f||report.maxSupplyPlacementDrift>.000001f||report.persistentIdleChecks<6||!report.directOne||!report.directTwo||!report.threeUsesOneFourWay||!report.fourUsesFourAndFive||!report.upgradedBoardsRetained||!report.reloadedUpgradesRetained)
                        throw new InvalidOperationException("Two outlet power-strip routing or retained upgrades failed: "+JsonUtility.ToJson(report));
                }
                if(crackleReview) {
                    report.crackleActiveCountStates=crackleCounts.Count;
                    if(report.crackleActiveCountStates<3||report.arcZeroBursts<4||report.arcZeroSilentFrames<12||report.arcZeroActiveFrames<3||report.maxArcZeroRun>6||report.maxActiveBranches<2||report.maxSparkAlpha<.5f)
                        throw new InvalidOperationException("Electric arcs do not have short staggered strikes, forks and sparks: "+JsonUtility.ToJson(report));
                }
                if(fixedFeetReview&&(report.maxBaseVertexDrift>.000001f||report.maxBaseRotationDegrees>.001f||report.maxFootMountError>.000001f||report.maxMachinePivotError>.000001f||report.minFootMountLength<.002f))
                    throw new InvalidOperationException("Fixed foot pads or casing support joints moved: "+JsonUtility.ToJson(report));
                if(rockReview&&(report.machineLabelCount!=0||report.maxMachineRockDegrees<2||report.maxMachineFloorError>.00001f||report.maxCrankMountError>.000001f||report.maxOrbitRadiusError>.000001f||report.maxCookerPlacementDrift>.000001f))
                    throw new InvalidOperationException("Whole generator rocking, floor support or no-label validation failed: "+JsonUtility.ToJson(report));
                if(electricReview) {
                    report.chargeStateCount=chargeStates.Count;report.chargeSequence=chargeSequence.ToArray();
                    if(report.chargeStateCount!=6||report.minChargeCells!=0||report.maxChargeCells!=5||report.chargeRises<10||report.chargeResets<2||report.chargeFalls!=report.chargeResets||report.unexpectedChargeDecreases!=0||report.unexpectedChargeIncreases!=0||report.socketCount!=2||report.maxElectricAlpha<.5f||report.maxElectricSampleAlpha-report.minElectricSampleAlpha<.1f||report.maxElectricTravel<.002f||report.maxCableSourceError>.000001f)
                        throw new InvalidOperationException("Charge loop, electrical flow or actual AC connection failed: "+JsonUtility.ToJson(report));
                }
                if(fullBody&&(report.maxFootDrift>.00001f||report.maxLegLengthChange>.000001f||report.maxChestPitch-report.minChestPitch<40||report.maxRootY-report.minRootY<.025f||report.maxKneeFlex-report.minKneeFlex<15||report.minRightFacing<.3f))
                    throw new InvalidOperationException("Whole-body effort or planted stance validation failed: "+JsonUtility.ToJson(report));
                if(refined&&report.activeBoundsMeshesChecked<500)
                    throw new InvalidOperationException("Visible work equipment was not checked for frame bounds");
                if(refined&&(report.maxGraspCenterError>.0002f||report.maxGraspAxisError>.1f||report.maxForearmTwist>85||report.maxHandAngularStep>15))
                    throw new InvalidOperationException("Actual finger grip is not on the visible shaft: "+JsonUtility.ToJson(report));
                // The larger orbit moves the torso with the crank. A shoulder
                // can enter that orbit without an IK singularity. Keep the old
                // static-envelope gate for its original previews; the moving
                // case checks the actual elbow-to-grip radial vector instead.
                if(refined&&!movingOrbit&&report.minShoulderPlaneDistance<FishWorkStation.CrankRadius+.015f)
                    throw new InvalidOperationException("Shoulder entered the static crank envelope");
                if(movingOrbit&&report.minForearmAxisDistance<.04f)
                    throw new InvalidOperationException("Forearm approached a singular shaft alignment");
                if(fullSize&&(report.maxCharacterPixelScaleError>.00001f||Mathf.Abs(report.scenePixelScaleVsPreviousWork-1.30f)>.00001f))
                    throw new InvalidOperationException("Working stage shrank the original character pixel scale");
                if(twoHand&&report.minGraspSeparation<.055f)throw new InvalidOperationException("Two hands overlap on the crank grip");
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

        static float CrossFace(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static float HalfArea(Vector3 a,Vector3 b,Vector3 c,bool upper) {
            var input=new System.Collections.Generic.List<Vector2>{new Vector2(a.x,a.y),new Vector2(b.x,b.y),new Vector2(c.x,c.y)};
            var output=new System.Collections.Generic.List<Vector2>();
            for(int i=0;i<input.Count;i++) {
                Vector2 p=input[i],q=input[(i+1)%input.Count];bool inside=upper?p.y>=0:p.y<=0,next=upper?q.y>=0:q.y<=0;
                if(inside)output.Add(p);if(inside!=next)output.Add(Vector2.Lerp(p,q,-p.y/(q.y-p.y)));
            }
            float area=0;for(int i=1;i+1<output.Count;i++)area+=Mathf.Abs(CrossFace(output[i]-output[0],output[i+1]-output[0]))*.5f;return area;
        }
        static void CheckFaceLayout(FishWorkStation station,Report report) {
            if(!station.WorkingVisible)return;report.faceLayoutChecks++;
            var machine=station.Root.Find("Floor generator");var outline=machine.Find("Battery body").GetComponent<MeshFilter>().sharedMesh;report.batteryWidth=outline.bounds.size.x;
            Vector2 gap=new Vector2(.006f,.160f);bool blocked=false;var points=outline.vertices;var triangles=outline.triangles;
            for(int i=0;i<triangles.Length;i+=3) {
                Vector3 p=points[triangles[i]],q=points[triangles[i+1]],r=points[triangles[i+2]];if(Mathf.Abs(p.z-outline.bounds.max.z)>1e-7f||Mathf.Abs(q.z-p.z)>1e-7f||Mathf.Abs(r.z-p.z)>1e-7f)continue;
                Vector2 a=new Vector2(p.x,p.y),b=new Vector2(q.x,q.y),c=new Vector2(r.x,r.y);float x=CrossFace(b-a,gap-a),y=CrossFace(c-b,gap-b),z=CrossFace(a-c,gap-c);
                if((x>=0&&y>=0&&z>=0)||(x<=0&&y<=0&&z<=0))blocked=true;
            }
            report.batteryFaceFilled=blocked;
            var rim=machine.Find("Round power badge rim");var rimMesh=rim.GetComponent<MeshFilter>().sharedMesh;var socket=machine.Find("AC socket 1");
            report.badgeTopGap=outline.bounds.min.y-(rim.localPosition.y+rimMesh.bounds.max.y);
            report.badgeBottomGap=rim.localPosition.y+rimMesh.bounds.min.y-(socket.localPosition.y+socket.GetComponent<MeshFilter>().sharedMesh.bounds.max.y);
            report.badgeCenteringError=Mathf.Max(Mathf.Abs(report.badgeTopGap-report.badgeBottomGap),Mathf.Abs(rim.localPosition.y-machine.Find("Black round power badge").localPosition.y),Mathf.Abs(rim.localPosition.y-machine.Find("Large yellow power bolt").localPosition.y));
            report.batteryOuterCornerSetback=points.Min(p=>Vector2.Distance(new Vector2(p.x,p.y),new Vector2(.083f,.173f)));
            report.batteryShoulderSetback=Mathf.Max(points.Min(p=>Vector2.Distance(new Vector2(p.x,p.y),new Vector2(-.071f,.143f))),points.Min(p=>Vector2.Distance(new Vector2(p.x,p.y),new Vector2(-.071f,.117f))));
            var bolt=machine.Find("Large yellow power bolt").GetComponent<MeshFilter>().sharedMesh;report.boltWidth=bolt.bounds.size.x;report.boltHeight=bolt.bounds.size.y;
            points=bolt.vertices;triangles=bolt.triangles;float top=0,bottom=0;
            for(int i=0;i<triangles.Length;i+=3) {
                Vector3 a=points[triangles[i]],b=points[triangles[i+1]],c=points[triangles[i+2]];
                if(Mathf.Abs(a.z-bolt.bounds.max.z)>1e-7f||Mathf.Abs(a.z-b.z)>1e-7f||Mathf.Abs(a.z-c.z)>1e-7f)continue;
                top+=HalfArea(a,b,c,true);bottom+=HalfArea(a,b,c,false);
            }
            report.boltTopArea=top;report.boltBottomArea=bottom;report.boltAreaMismatch=Mathf.Abs(top-bottom)/Mathf.Max(top,bottom);
        }

        static void CheckSolidStrips(FishWorkStation station,Report report) {
            if(!station.Visible)return;
            Bounds generator=new Bounds();bool first=true;
            var housing=station.Root.Find("Floor generator/Generator housing").GetComponent<MeshFilter>();
            foreach(var p in housing.sharedMesh.vertices){Vector3 v=station.Root.InverseTransformPoint(housing.transform.TransformPoint(p));if(first){generator=new Bounds(v,Vector3.zero);first=false;}else generator.Encapsulate(v);}
            for(int i=0;i<station.SupplyCount;i++) {
                var strip=station.SupplyAt(i);if(!strip.gameObject.activeInHierarchy)continue;
                var shell=strip.Find("Geometry/Power strip body "+station.SupplyCapacityAt(i)).GetComponent<MeshFilter>();var points=shell.sharedMesh.vertices;
                report.minShellThickness=Mathf.Min(report.minShellThickness,points.Max(p=>p.z)-points.Min(p=>p.z));report.shellChecks++;
                var rows=strip.Find("Geometry").Cast<Transform>().Where(t=>t.name.StartsWith("Socket ")).GroupBy(t=>Mathf.RoundToInt(t.localPosition.y*100000)).Select(g=>g.Count()).OrderBy(x=>x).ToArray();
                int capacity=station.SupplyCapacityAt(i);
                if(capacity==8||capacity==10){if(rows.Length!=2||rows.Any(n=>n!=capacity/2))throw new InvalidOperationException("Upgraded strip is not2x4/2x5.");report.dualRowChecks++;}
                Bounds bounds=new Bounds(station.Root.InverseTransformPoint(shell.transform.TransformPoint(points[0])),Vector3.zero);
                foreach(var p in points)bounds.Encapsulate(station.Root.InverseTransformPoint(shell.transform.TransformPoint(p)));
                float dx=Mathf.Max(0,generator.min.x-bounds.max.x,bounds.min.x-generator.max.x),dz=Mathf.Max(0,generator.min.z-bounds.max.z,bounds.min.z-generator.max.z);
                report.minStripCaseGap=Mathf.Min(report.minStripCaseGap,Mathf.Sqrt(dx*dx+dz*dz));
                if(station.WorkingVisible) {
                    var cord=station.Root.Find("Supply cable "+i).GetComponent<MeshFilter>().sharedMesh;
                    if(cord.name!="White round power strip cord"||cord.vertexCount%8!=0)throw new InvalidOperationException("Strip feed is not a round white cable.");
                    var v=cord.vertices;var normals=cord.normals;var triangles=cord.triangles;
                    for(int j=0;j<triangles.Length;j+=3){int a=triangles[j],b=triangles[j+1],c=triangles[j+2];Vector3 face=Vector3.Cross(v[b]-v[a],v[c]-v[a]);if(face.sqrMagnitude<1e-18f)throw new InvalidOperationException("Degenerate white cable face.");face/=Mathf.Sqrt(face.sqrMagnitude);report.minTubeNormalDot=Mathf.Min(report.minTubeNormalDot,Vector3.Dot(face,(normals[a]+normals[b]+normals[c]).normalized));}
                    foreach(var c in cord.colors){report.minWhiteFeedRGB=Mathf.Min(report.minWhiteFeedRGB,c.r,c.g,c.b);report.whiteFeedSamples++;}
                }
            }
        }

        static void CheckLooseCords(FishWorkStation station,Report report) {
            if(!station.WorkingVisible)return;
            for(int i=0;i<2;i++) {
                var strip=station.SupplyAt(i);if(!strip.gameObject.activeInHierarchy)continue;
                Vector3 offset=strip.localPosition-FishWorkStation.GeneratorPosition;offset.y=0;report.maxPrimaryStripDistance=Mathf.Max(report.maxPrimaryStripDistance,offset.magnitude);
            }
            foreach(var cord in station.Root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Power cable"&&f.gameObject.activeInHierarchy)) {
                var v=cord.sharedMesh.vertices;Vector3 first=cord.transform.TransformPoint((v[0]+v[1])*.5f),previous=first;float length=0;
                for(int j=0;j<v.Length/4;j++) {
                    Vector3 next=cord.transform.TransformPoint((v[j*4+2]+v[j*4+3])*.5f);length+=Vector3.Distance(previous,next);previous=next;
                    float y=station.Root.InverseTransformPoint(next).y;
                    if(y<=FishWorkStation.GeneratorFloorHeight+.010f)report.groundedCordSamples++;
                }
                float ratio=length/Vector3.Distance(first,previous);report.minCordSlackRatio=Mathf.Min(report.minCordSlackRatio,ratio);report.meanCordSlackRatio+=ratio;report.slackCordChecks++;
                foreach(var p in v)report.minCordFloor=Mathf.Min(report.minCordFloor,station.Root.InverseTransformPoint(cord.transform.TransformPoint(p)).y);
            }
        }

        static void CheckSupply(FishWorkStation station,Report report) {
            if(!station.Visible)return;
            report.supplyGraphChecks++;
            if(!station.WorkingVisible) {
                for(int i=0;i<2;i++)if(station.SupplyAt(i).gameObject.activeInHierarchy!=((station.PowerUpgradeMask&(1<<i))!=0))throw new InvalidOperationException("Upgraded strip disappeared while idle.");
                foreach(var t in station.Root.GetComponentsInChildren<Transform>())if(t.gameObject.activeInHierarchy&&(t.name=="Floor generator"||t.name=="Generator base"||t.name=="Hand crank"||t.name.StartsWith("Rice cooker ")||t.name.StartsWith("Supply cable ")))throw new InvalidOperationException("Working equipment did not hide beside persistent empty strips.");
                report.persistentIdleChecks++;return;
            }
            var seen=new System.Collections.Generic.HashSet<Transform>();
            for(int i=0;i<station.CookerCount;i++) {
                var source=station.CookerSourceAt(i);
                if(!source.gameObject.activeInHierarchy||!seen.Add(source))throw new InvalidOperationException("A cooker is disconnected or shares an occupied outlet.");
                var cable=station.Root.Find("Rice cooker "+(i+1)+"/Power cable").GetComponent<MeshFilter>();var p=cable.sharedMesh.vertices;
                report.maxCableSourceError=Mathf.Max(report.maxCableSourceError,Vector3.Distance(cable.transform.TransformPoint((p[0]+p[1])*.5f),source.position));
            }
            int visible=0;
            for(int i=0;i<station.SupplyCount;i++) {
                var strip=station.SupplyAt(i);if(!strip.gameObject.activeSelf)continue;visible++;
                int geometryId=strip.Find("Geometry").GetInstanceID();Vector3 rest;
                if(report.supplyPositions.TryGetValue(geometryId,out rest))report.maxSupplyPlacementDrift=Mathf.Max(report.maxSupplyPlacementDrift,Vector3.Distance(strip.position,rest));
                else report.supplyPositions[geometryId]=strip.position;
                var source=station.SupplyFeedSourceAt(i);
                if(!source.gameObject.activeInHierarchy||!seen.Add(source))throw new InvalidOperationException("Power-strip feed is disconnected or double-booked.");
                int capacity=strip.Find("Geometry").Cast<Transform>().Count(t=>t.name.StartsWith("Socket "));
                if(capacity!=station.SupplyCapacityAt(i))throw new InvalidOperationException("Power strip slot geometry does not match capacity.");
                var wire=station.Root.Find("Supply cable "+i).GetComponent<MeshFilter>();var p=wire.sharedMesh.vertices;
                Vector3 first=Vector3.zero,last=Vector3.zero;
                if(wire.sharedMesh.name=="White round power strip cord"){for(int j=0;j<8;j++){first+=p[j]/8;last+=p[p.Length-8+j]/8;}}
                else {first=(p[0]+p[1])*.5f;last=(p[p.Length-1]+p[p.Length-2])*.5f;}
                report.maxSupplyFeedError=Mathf.Max(report.maxSupplyFeedError,Vector3.Distance(wire.transform.TransformPoint(first),source.position),Vector3.Distance(wire.transform.TransformPoint(last),strip.Find("Geometry/Feed inlet").position));
            }
            report.maxVisibleSupplies=Mathf.Max(report.maxVisibleSupplies,visible);
            report.maxSupplyCapacityA=Mathf.Max(report.maxSupplyCapacityA,station.SupplyCapacityAt(0));report.maxSupplyCapacityB=Mathf.Max(report.maxSupplyCapacityB,station.SupplyCapacityAt(1));
            var activePlugs=station.Root.GetComponentsInChildren<Transform>().Where(t=>t.gameObject.activeInHierarchy&&(t.name=="Plug"||t.name.StartsWith("Generator plug "))).ToArray();
            if(activePlugs.Length!=station.CookerCount+visible)throw new InvalidOperationException("Active mains plugs do not match cookers plus strip feeds.");
        }
        static void TestSupplyLifecycle(Transform model,Shader shader,Report report) {
            using(var probe=new FishWorkStation(model,shader,174)) {
                foreach(int count in new[]{1,2,3,4,7,10,2,1,0,24,1}) {
                    probe.Tick(.5f,1,count);CheckSupply(probe,report);report.lifecycleChecks++;
                    bool a=probe.SupplyAt(0).gameObject.activeSelf,b=probe.SupplyAt(1).gameObject.activeSelf;
                    if(report.lifecycleChecks==1)report.directOne=!a&&!b&&probe.CookerSourceAt(0).parent.name=="Generator plug 0";
                    if(report.lifecycleChecks==2)report.directTwo=!a&&!b&&probe.CookerSourceAt(0)!=probe.CookerSourceAt(1);
                    if(report.lifecycleChecks==3)report.threeUsesOneFourWay=a&&!b&&probe.SupplyCapacityAt(0)==4&&probe.SupplyAt(0).GetComponentsInChildren<Transform>().Count(t=>t.name=="Plug"&&t.gameObject.activeInHierarchy)==2;
                    if(report.lifecycleChecks==4)report.fourUsesFourAndFive=a&&b&&probe.SupplyCapacityAt(0)==4&&probe.SupplyCapacityAt(1)==5;
                    if(report.lifecycleChecks==5&&probe.SupplyCapacityAt(0)!=8)throw new InvalidOperationException("Four-way strip did not upgrade when filled.");
                    if(report.lifecycleChecks==6&&(probe.SupplyCapacityAt(0)!=8||probe.SupplyCapacityAt(1)!=10))throw new InvalidOperationException("Five-way strip did not upgrade when filled.");
                    if(report.lifecycleChecks>=7&&(!a||!b||probe.SupplyCapacityAt(0)!=8||probe.SupplyCapacityAt(1)!=10))throw new InvalidOperationException("Upgraded strip disappeared after count fell.");
                }
                probe.Tick(.5f,0,0);CheckSupply(probe,report);
                report.upgradedBoardsRetained=true;
                using(var reloaded=new FishWorkStation(model,shader,175,probe.PowerUpgradeMask)) {
                    reloaded.Tick(.5f,0,0);CheckSupply(reloaded,report);
                    reloaded.Tick(.5f,1,1);CheckSupply(reloaded,report);
                    report.reloadedUpgradesRetained=reloaded.SupplyAt(0).gameObject.activeSelf&&reloaded.SupplyAt(1).gameObject.activeSelf&&reloaded.SupplyCapacityAt(0)==8&&reloaded.SupplyCapacityAt(1)==10;
                }
            }
        }
        static void RenderSupplyDetail(Camera camera,FishWorkStation station,MeshFilter[] snapshots,string output,int step,int count) {
            var children=station.Root.GetComponentsInChildren<Transform>(true);var active=children.Select(t=>t.gameObject.activeSelf).ToArray();var visible=snapshots.Select(t=>t.gameObject.activeSelf).ToArray();
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;float size=camera.orthographicSize,aspect=camera.aspect;
            var target=camera.targetTexture;var previous=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(640,400,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);var image=new Texture2D(640,400,TextureFormat.RGBA32,false);
            try {
                foreach(var mesh in snapshots)mesh.gameObject.SetActive(false);
                station.Root.Find("Hand crank").gameObject.SetActive(false);
                foreach(var child in children)if(child.name=="Cooker wobble"||child.name=="Electric pulse")child.gameObject.SetActive(false);
                camera.targetTexture=rt;camera.aspect=1.6f;camera.orthographicSize=(output.EndsWith("175")||output.EndsWith("176")||output.EndsWith("177")||output.EndsWith("178")||(output.EndsWith("179")||(output.EndsWith("180")||(output.EndsWith("181")||(output.EndsWith("182")||(output.EndsWith("183")||(output.EndsWith("184")||(output.EndsWith("185")||output.EndsWith("186")))))))))?.25f:.22f;
                Vector3 focus=station.Root.TransformPoint((output.EndsWith("175")||output.EndsWith("176")||output.EndsWith("177")||output.EndsWith("178")||(output.EndsWith("179")||(output.EndsWith("180")||(output.EndsWith("181")||(output.EndsWith("182")||(output.EndsWith("183")||(output.EndsWith("184")||(output.EndsWith("185")||output.EndsWith("186")))))))))?new Vector3(-.20f,.13f,.34f):new Vector3(-.015f,.13f,.39f));
                camera.transform.position=focus+new Vector3(0,(output.EndsWith("175")||output.EndsWith("176")||output.EndsWith("177")||output.EndsWith("178")||(output.EndsWith("179")||(output.EndsWith("180")||(output.EndsWith("181")||(output.EndsWith("182")||(output.EndsWith("183")||(output.EndsWith("184")||(output.EndsWith("185")||output.EndsWith("186")))))))))?.45f:.34f,.8f);camera.transform.LookAt(focus);
                camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,640,400),0,0);image.Apply();File.WriteAllBytes(output+"/supply-step-"+step.ToString("D2")+"-jobs-"+count+".png",image.EncodeToPNG());
            } finally {
                for(int i=0;i<children.Length;i++)children[i].gameObject.SetActive(active[i]);for(int i=0;i<snapshots.Length;i++)snapshots[i].gameObject.SetActive(visible[i]);
                camera.targetTexture=target;camera.aspect=aspect;camera.orthographicSize=size;camera.transform.SetPositionAndRotation(position,rotation);RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static void RenderCrankDetail(Camera camera,FishWorkStation station,Transform elbow,string output,int frame) {
            Directory.CreateDirectory(output+"/arms");
            var steam=station.Root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Steam").ToArray();var active=steam.Select(t=>t.gameObject.activeSelf).ToArray();
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;float size=camera.orthographicSize,aspect=camera.aspect;
            var target=camera.targetTexture;var previous=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(720,480,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);var image=new Texture2D(720,480,TextureFormat.RGBA32,false);
            try {
                foreach(var t in steam)t.gameObject.SetActive(false);
                Vector3 grip=station.Root.Find("Hand crank/Crank grip").position,focus=Vector3.Lerp(grip,elbow.position,.3f);
                Vector3 shaft=station.Root.TransformDirection(FishWorkStation.GeneratorRotation*Vector3.right);
                camera.targetTexture=rt;camera.aspect=1.5f;camera.orthographicSize=(output.EndsWith("165")||output.EndsWith("166")||output.EndsWith("167")||output.EndsWith("168")||output.EndsWith("169")||output.EndsWith("170")||output.EndsWith("171")||output.EndsWith("172")||output.EndsWith("174")||output.EndsWith("175")||output.EndsWith("176")||output.EndsWith("177")||output.EndsWith("178")||(output.EndsWith("179")||(output.EndsWith("180")||(output.EndsWith("181")||(output.EndsWith("182")||(output.EndsWith("183")||(output.EndsWith("184")||(output.EndsWith("185")||output.EndsWith("186")))))))))?.15f:.115f;
                foreach(string angle in new[]{"front","axial"}) {
                    camera.transform.position=focus+(angle=="front"?Vector3.forward:shaft)*.7f+Vector3.up*.10f;camera.transform.LookAt(focus);
                    camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,720,480),0,0);image.Apply();File.WriteAllBytes(output+"/arms/frame"+frame.ToString("D3")+"-"+angle+".png",image.EncodeToPNG());
                }
            } finally {
                for(int i=0;i<steam.Length;i++)steam[i].gameObject.SetActive(active[i]);camera.targetTexture=target;camera.aspect=aspect;camera.orthographicSize=size;camera.transform.SetPositionAndRotation(position,rotation);RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static void RenderGeneratorDetail(Camera camera,FishWorkStation station,MeshFilter[] snapshots,string output,int frame) {
            Directory.CreateDirectory(output+"/generator");
            var children=station.Root.Cast<Transform>().ToArray();var active=children.Select(t=>t.gameObject.activeSelf).ToArray();var visible=snapshots.Select(t=>t.gameObject.activeSelf).ToArray();
            Vector3 position=camera.transform.position;Quaternion rotation=camera.transform.rotation;float size=camera.orthographicSize,aspect=camera.aspect;
            var target=camera.targetTexture;var previous=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(480,640,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);var image=new Texture2D(480,640,TextureFormat.RGBA32,false);
            try {
                foreach(var mesh in snapshots)mesh.gameObject.SetActive(false);
                foreach(var child in children)child.gameObject.SetActive(child.name=="Floor generator"||child.name=="Hand crank"||child.name=="Generator base");
                camera.targetTexture=rt;camera.aspect=.75f;camera.orthographicSize=.39f;
                Vector3 focus=station.Root.TransformPoint(FishWorkStation.GeneratorPosition+FishWorkStation.GeneratorRotation*Vector3.right*.14f+Vector3.up*.04f);
                Vector3 forward=station.Root.TransformDirection(FishWorkStation.GeneratorRotation*Vector3.forward);
                camera.transform.position=focus+forward*.8f;camera.transform.LookAt(focus);
                camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,480,640),0,0);image.Apply();File.WriteAllBytes(output+"/generator/frame"+frame.ToString("D3")+".png",image.EncodeToPNG());
            } finally {
                for(int i=0;i<children.Length;i++)children[i].gameObject.SetActive(active[i]);for(int i=0;i<snapshots.Length;i++)snapshots[i].gameObject.SetActive(visible[i]);
                camera.targetTexture=target;camera.aspect=aspect;camera.orthographicSize=size;camera.transform.SetPositionAndRotation(position,rotation);RenderTexture.active=previous;
                RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
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
