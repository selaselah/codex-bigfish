using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;

namespace DesktopFish.Editor {
    // Matched offline working poses; never writes the scene, app state or tasks.
    public static class FishHairSpringReview {
        [Serializable] sealed class PhysicsReport {public float maxOffset,minJacobian,maxLengthError,maxTransferCorrection,maxMeshCorrection;public int collisions,resets,safetyResets;}
        [Serializable] sealed class Pose {public Vector3[] positions,scales;public Quaternion[] rotations;}

        public static void ValidateAndRender(){FishHairPhysicsTests.Run();Render();}
        public static void Render() {
            const int frames=600,width=640,height=720;
            bool checkOnly=Environment.GetEnvironmentVariable("FISH_HAIR_CHECK_ONLY")=="1";
            float gain=float.Parse(Environment.GetEnvironmentVariable("FISH_HAIR_GAIN")??"1",System.Globalization.CultureInfo.InvariantCulture);
            string folder="work/hair-physics-213/motion";Directory.CreateDirectory(folder);
            Pose[] baseline=new Pose[frames];var baselineVertices=new Vector3[frames][][];var baselineValid=new bool[frames][][];var baselineCross=new Vector3[frames][][];float maximumDisplacement=0;int changedPositions=0,newAuthoredNormalFacing=0;
            for(int version=0;version<2;version++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
                var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();
                FishLowPoly5K.RestoreFullModel(c.model.gameObject);FishLowPoly5K.Apply(c.model.gameObject,false);
                var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();

                var transforms=c.model.GetComponentsInChildren<Transform>(true);
                var hairMasks=skins.Select(s=>s.sharedMesh.boneWeights.Select(w=>s.sharedMesh.name=="RearHairCards"||new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(i=>i>=5&&i<=9)).ToArray()).ToArray();
                var sideMasks=skins.Select(s=>s.sharedMesh.boneWeights.Select(w=>s.sharedMesh.name=="Body"&&new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(b=>b==5||b==9)).ToArray()).ToArray();
                var motion=new FishMotion(c.model){randomActions=false,secondaryHair=version==1,secondaryHairScale=gain,centralHairOnly=Environment.GetEnvironmentVariable("FISH_PHYSICS_ONE")=="1"};
                var legacy=version==0?new LegacyHairWave212(c.model):null;
                var meshes=skins.Select(s=>new Mesh()).ToArray();
                for(int i=0;i<skins.Length;i++) {
                    var go=new GameObject("Hair motion snapshot");go.transform.SetParent(skins[i].transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=meshes[i];go.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;
                }
                var camera=c.view;camera.backgroundColor=Color.white;camera.aspect=width/(float)height;camera.orthographicSize=.78f;
                var target=new RenderTexture(width,height,24){antiAliasing=4};var image=new Texture2D(width,height,TextureFormat.RGBA32,false);camera.targetTexture=target;
                using(var station=new FishWorkStation(c.model,c.workPropShader,159,0)) {
                    // Settle the working blend before the paired recorded interval.
                    for(int frame=0;frame<frames;frame++) {
                        float now=100+frame/30f;
                        if(frame==420)motion.Play(FishMotion.Gesture.Bow,now);
                        motion.Tick(now,1f/30,0,0,((frame>=30&&frame<150)||(frame>=240&&frame<360))?"working":"idle",false);legacy?.Tick(now,motion.WorkWeight);station.Tick(now,motion.WorkWeight,frame>=30?2:0,frame>=30);FishBuild.ApplyMotionExpressions(c,motion);

                        var pose=new Pose{positions=transforms.Select(t=>t.localPosition).ToArray(),scales=transforms.Select(t=>t.localScale).ToArray(),rotations=transforms.Select(t=>t.localRotation).ToArray()};
                        if(version==0)baseline[frame]=pose;
                        else for(int i=0;i<transforms.Length;i++) {
                            if(pose.positions[i]!=baseline[frame].positions[i]||pose.scales[i]!=baseline[frame].scales[i]||(transforms[i].name!="J_Sec_Hair2_01"&&transforms[i].name!="J_Sec_Hair3_01"&&transforms[i].name!="J_Sec_Hair4_01"&&pose.rotations[i]!=baseline[frame].rotations[i]))
                                throw new InvalidOperationException("Hair follow-through changed body, grip or other joint pose: "+transforms[i].name);
                        }
                        if(version==0){baselineVertices[frame]=new Vector3[skins.Length][];baselineValid[frame]=new bool[skins.Length][];baselineCross[frame]=new Vector3[skins.Length][];}
                        for(int i=0;i<skins.Length;i++) {
                            skins[i].BakeMesh(meshes[i]);
                            var p=meshes[i].vertices;var n=meshes[i].normals;var triangles=meshes[i].triangles;
                            if(p.Any(q=>float.IsNaN(q.x)||float.IsNaN(q.y)||float.IsNaN(q.z)||float.IsInfinity(q.x)||float.IsInfinity(q.y)||float.IsInfinity(q.z)))throw new InvalidOperationException("Nonfinite hair motion.");
                            var valid=new bool[triangles.Length/3];var crosses=new Vector3[valid.Length];
                            Vector3[] referencePositions=null;
                            if(version==1) {
                                // Same current pose and neutral geometry: the five
                                // old clock-driven hair joint offsets are intentionally removed.
                                var dynamicMesh=skins[i].sharedMesh;var referenceMesh=new Mesh();
                                skins[i].sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(FishLowPoly5K.Folder+"/"+dynamicMesh.name+".asset");
                                skins[i].BakeMesh(referenceMesh);referencePositions=referenceMesh.vertices;skins[i].sharedMesh=dynamicMesh;UnityEngine.Object.DestroyImmediate(referenceMesh);
                            }
                            for(int f=0;f<valid.Length;f++) {
                                int a=triangles[3*f],b=triangles[3*f+1],z=triangles[3*f+2];Vector3 cross=Vector3.Cross(p[b]-p[a],p[z]-p[a]);crosses[f]=cross;valid[f]=cross.sqrMagnitude>1e-18f&&Vector3.Dot(cross,n[a]+n[b]+n[z])>=-1e-16f;
                                if(version==1) {
                                    if(baselineValid[frame][i][f]&&!valid[f])newAuthoredNormalFacing++;
                                    Vector3 before=Vector3.Cross(referencePositions[b]-referencePositions[a],referencePositions[z]-referencePositions[a]);
                                    if(before.sqrMagnitude>1e-18f&&(cross.sqrMagnitude<=1e-18f||Vector3.Dot(before,cross)<=0))throw new InvalidOperationException("New geometric hair degeneracy/reversal: "+skins[i].sharedMesh.name+" frame"+frame+" face"+f);
                                }
                            }
                            if(version==0){baselineVertices[frame][i]=p;baselineValid[frame][i]=valid;baselineCross[frame][i]=crosses;}
                            else for(int j=0;j<p.Length;j++) {
                                float displacement=(p[j]-baselineVertices[frame][i][j]).magnitude;maximumDisplacement=Mathf.Max(maximumDisplacement,displacement);if(displacement>1e-7f)changedPositions++;
                                if((!hairMasks[i][j]||sideMasks[i][j])&&p[j]!=baselineVertices[frame][i][j])throw new InvalidOperationException("Hair changed protected non-hair pose.");
                            }
                        }
                        if(checkOnly)continue;
                        Vector3 center=new Vector3(0,.55f,0);float angle=frame<180?0:165;
                        camera.transform.position=center+Quaternion.Euler(0,angle,0)*Vector3.forward*3;camera.transform.LookAt(center);
                        camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                        File.WriteAllBytes(folder+"/"+(version==0?"before":"after")+frame.ToString("D4")+".png",image.EncodeToPNG());
                    }
                }
                if(version==1&&motion.HairSimulation!=null){var sim=motion.HairSimulation;if(sim.SafetyResets!=0)throw new InvalidOperationException("Physics required unsafe-field resets.");File.WriteAllText(folder+"/Physics.json",JsonUtility.ToJson(new PhysicsReport{maxOffset=sim.MaximumParticleOffset,minJacobian=sim.MinimumJacobian,maxLengthError=sim.MaximumLengthError,maxTransferCorrection=sim.MaximumStrainCorrection,maxMeshCorrection=sim.MaximumMeshCorrection,collisions=sim.CollisionProjections,resets=sim.ResetCount,safetyResets=sim.SafetyResets},true));}
                legacy?.Dispose();motion.Dispose();camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
            }
            File.WriteAllText(folder+"/Validation.json","{\"frames_per_version\":600,\"fps\":30,\"non_hair_local_poses_exact\":true,\"face_poses_exact\":true,\"new_geometric_degenerate_reversed\":0,\"inherited_authored_normal_facing_transitions\":"+newAuthoredNormalFacing+",\"changed_posed_records\":"+changedPositions+",\"max_local_displacement\":"+maximumDisplacement.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"scene_saved\":false,\"task_state_written\":false}");
            Debug.Log("Spring hair matched1200frames passed; all non-hair local joint poses/model transform identical; no assets/state saved.");
        }
    }
}
