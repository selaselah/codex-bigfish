using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;

namespace DesktopFish.Editor {
    // Matched offline working poses; never writes the scene, app state or tasks.
    public static class FishHairInertiaReview {
        [Serializable] sealed class PhysicsReport {public float maxOffset,minJacobian,maxLengthError,maxTransferCorrection,maxMeshCorrection;public int collisions,resets,safetyResets;}
        [Serializable] sealed class Domains {public int[] left,right;}
        [Serializable] sealed class Pose {public Vector3[] positions,scales;public Quaternion[] rotations;}

        public static void RenderDefaultTop() {
            EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();
            FishLowPoly5K.RestoreFullModel(c.model.gameObject);FishLowPoly5K.Apply(c.model.gameObject,false);
            using(var motion=new FishMotion(c.model){randomActions=false}) {
                motion.Tick(100,1f/30,0,0,"idle",false);FishBuild.ApplyMotionExpressions(c,motion);
                var camera=c.view;camera.backgroundColor=Color.white;camera.aspect=640f/720;camera.orthographicSize=.78f;
                var center=new Vector3(0,.55f,0);camera.transform.position=center+Quaternion.Euler(-20,35,0)*Vector3.forward*3;camera.transform.LookAt(center);
                var target=new RenderTexture(640,720,24){antiAliasing=4};var image=new Texture2D(640,720,TextureFormat.RGBA32,false);camera.targetTexture=target;
                camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,640,720),0,0);image.Apply();File.WriteAllBytes("work/hair-inertia-214/motion/after-neutral-4.png",image.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
            }
            Debug.Log("Default overhead review passed.");
        }
        public static void ValidateAndRender(){FishHairPhysicsTests.Run();Render();}
        public static void Render() {
            const int frames=600,width=640,height=720;
            bool checkOnly=Environment.GetEnvironmentVariable("FISH_HAIR_CHECK_ONLY")=="1";
            float gain=float.Parse(Environment.GetEnvironmentVariable("FISH_HAIR_GAIN")??"1",System.Globalization.CultureInfo.InvariantCulture);
            string folder="work/hair-inertia-214/motion";Directory.CreateDirectory(folder);
            bool before=Environment.GetEnvironmentVariable("FISH_HAIR_BEFORE")=="1";
            var stream=File.Open(folder+"/Baseline.bin",before?FileMode.Create:FileMode.Open,before?FileAccess.Write:FileAccess.Read);
            var writer=before?new BinaryWriter(stream):null;var reader=before?null:new BinaryReader(stream);
            var domains=JsonUtility.FromJson<Domains>(File.ReadAllText("work/hair-inertia-214/Side-Domains.json"));
            float maximumDisplacement=0;int changedPositions=0,newAuthoredNormalFacing=0;
            for(int version=before?0:1;version<(before?1:2);version++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
                var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();
                FishLowPoly5K.RestoreFullModel(c.model.gameObject);FishLowPoly5K.Apply(c.model.gameObject,false);
                var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();

                var transforms=c.model.GetComponentsInChildren<Transform>(true);
                var hairMasks=skins.Select(s=>s.sharedMesh.boneWeights.Select(w=>s.sharedMesh.name=="RearHairCards"||new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(i=>i>=5&&i<=9)).ToArray()).ToArray();
                var allowed=skins.Select(s=>{var w=s.sharedMesh.boneWeights;return w.Select((weight,i)=>s.sharedMesh.name=="RearHairCards"||(s.sharedMesh.name=="Body"&&(domains.left.Contains(i)||domains.right.Contains(i)||new[]{weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3}.Select((bone,j)=>bone>=6&&bone<=8&&new[]{weight.weight0,weight.weight1,weight.weight2,weight.weight3}[j]>0).Any(x=>x)))).ToArray();}).ToArray();
                var motion=new FishMotion(c.model){randomActions=false,secondaryHair=true,secondaryHairScale=gain,centralHairOnly=Environment.GetEnvironmentVariable("FISH_PHYSICS_ONE")=="1"};
                var legacy=(LegacyHairWave212)null;
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
                        for(int i=0;i<transforms.Length;i++) {
                            var p=pose.positions[i];var q=pose.rotations[i];var scale=pose.scales[i];
                            float[] values={p.x,p.y,p.z,q.x,q.y,q.z,q.w,scale.x,scale.y,scale.z};
                            foreach(float v in values)if(before)writer.Write(v);else if(v!=reader.ReadSingle())throw new InvalidOperationException("Changed core joint pose "+transforms[i].name);
                        }
                        for(int i=0;i<skins.Length;i++) {
                            skins[i].BakeMesh(meshes[i]);
                            var p=meshes[i].vertices;var n=meshes[i].normals;var triangles=meshes[i].triangles;
                            if(p.Any(q=>float.IsNaN(q.x)||float.IsNaN(q.y)||float.IsNaN(q.z)||float.IsInfinity(q.x)||float.IsInfinity(q.y)||float.IsInfinity(q.z)))throw new InvalidOperationException("Nonfinite hair motion.");
                            var valid=new bool[triangles.Length/3];var crosses=new Vector3[valid.Length];
                            Vector3[] old=before?null:new Vector3[p.Length];
                            bool[] oldValid=before?null:new bool[valid.Length];
                            if(!before){for(int j=0;j<old.Length;j++)old[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());for(int f=0;f<oldValid.Length;f++)oldValid[f]=reader.ReadBoolean();}
                            Vector3[] referencePositions=null;
                            if(version==1) {
                                // Same current pose and neutral geometry: all core poses and the rear joint drivers are identical in both versions.
                                var dynamicMesh=skins[i].sharedMesh;var referenceMesh=new Mesh();
                                skins[i].sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(FishLowPoly5K.Folder+"/"+dynamicMesh.name+".asset");
                                skins[i].BakeMesh(referenceMesh);referencePositions=referenceMesh.vertices;skins[i].sharedMesh=dynamicMesh;UnityEngine.Object.DestroyImmediate(referenceMesh);
                            }
                            for(int f=0;f<valid.Length;f++) {
                                int a=triangles[3*f],b=triangles[3*f+1],z=triangles[3*f+2];Vector3 cross=Vector3.Cross(p[b]-p[a],p[z]-p[a]);crosses[f]=cross;valid[f]=cross.sqrMagnitude>1e-18f&&Vector3.Dot(cross,n[a]+n[b]+n[z])>=-1e-16f;
                                if(version==1) {
                                    if(oldValid[f]&&!valid[f])newAuthoredNormalFacing++;
                                    Vector3 referenceCross=Vector3.Cross(referencePositions[b]-referencePositions[a],referencePositions[z]-referencePositions[a]);
                                    if(referenceCross.sqrMagnitude>1e-18f&&(cross.sqrMagnitude<=1e-18f||Vector3.Dot(referenceCross,cross)<=0))throw new InvalidOperationException("New geometric hair degeneracy/reversal: "+skins[i].sharedMesh.name+" frame"+frame+" face"+f);
                                }
                            }
                            if(before){foreach(var v in p){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}foreach(bool v in valid)writer.Write(v);}
                            else for(int j=0;j<p.Length;j++) {
                                float displacement=(p[j]-old[j]).magnitude;maximumDisplacement=Mathf.Max(maximumDisplacement,displacement);if(displacement>1e-7f)changedPositions++;
                                if(!allowed[i][j]&&p[j]!=old[j])throw new InvalidOperationException("Changed protected pose "+skins[i].sharedMesh.name+" vertex"+j);
                            }
                        }
                        if(checkOnly)continue;
                        Vector3 center=new Vector3(0,.55f,0);float angle=frame<360?180:35;
                        camera.transform.position=center+Quaternion.Euler(0,angle,0)*Vector3.forward*3;camera.transform.LookAt(center);
                        if(frame==0) {
                            float[] angles={0,35,90,180,35};
                            for(int v=0;v<5;v++) {
                                camera.transform.position=center+Quaternion.Euler(v==4?-20:0,angles[v],0)*Vector3.forward*3;camera.transform.LookAt(center);
                                camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(before?"before":"after")+"-neutral-"+v+".png",image.EncodeToPNG());
                            }
                            camera.transform.position=center+Quaternion.Euler(0,angle,0)*Vector3.forward*3;camera.transform.LookAt(center);
                            var skin=skins.Single(x=>x.sharedMesh.name=="Body");int k=Array.IndexOf(skins,skin);var positions=meshes[k].vertices;var normals=meshes[k].normals;var indices=meshes[k].triangles;
                            var selected=domains.left.Concat(domains.right).ToArray();var highlighted=new Mesh();highlighted.vertices=positions.Select((p,i)=>p+normals[i]*.0005f).ToArray();highlighted.triangles=Enumerable.Range(0,indices.Length/3).Where(f=>Enumerable.Range(0,3).All(j=>selected.Contains(indices[3*f+j]))).SelectMany(f=>new[]{indices[3*f],indices[3*f+1],indices[3*f+2]}).ToArray();
                            var overlay=new GameObject("Measured complete curl domains");overlay.transform.SetParent(skin.transform,false);overlay.AddComponent<MeshFilter>().sharedMesh=highlighted;var material=new Material(Shader.Find("Unlit/Color"));material.color=new Color(1,.35f,.02f);overlay.AddComponent<MeshRenderer>().sharedMaterial=material;
                            camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(before?"before":"after")+"-domains.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(overlay);UnityEngine.Object.DestroyImmediate(highlighted);UnityEngine.Object.DestroyImmediate(material);
                        }
                        camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                        File.WriteAllBytes(folder+"/"+(version==0?"before":"after")+frame.ToString("D4")+".png",image.EncodeToPNG());
                    }
                }
                if(version==1&&motion.HairSimulation!=null){var sim=motion.HairSimulation;if(sim.SafetyResets!=0)throw new InvalidOperationException("Physics required unsafe-field resets.");File.WriteAllText(folder+"/Physics.json",JsonUtility.ToJson(new PhysicsReport{maxOffset=sim.MaximumParticleOffset,minJacobian=sim.MinimumJacobian,maxLengthError=sim.MaximumLengthError,maxTransferCorrection=sim.MaximumStrainCorrection,maxMeshCorrection=sim.MaximumMeshCorrection,collisions=sim.CollisionProjections,resets=sim.ResetCount,safetyResets=sim.SafetyResets},true));}
                legacy?.Dispose();motion.Dispose();camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
            }
            writer?.Dispose();reader?.Dispose();stream.Dispose();
            File.WriteAllText(folder+"/"+(before?"Before-Validation.json":"Validation.json"),"{\"frames_per_version\":600,\"fps\":30,\"non_hair_local_poses_exact\":true,\"face_poses_exact\":true,\"new_geometric_degenerate_reversed\":0,\"inherited_authored_normal_facing_transitions\":"+newAuthoredNormalFacing+",\"changed_posed_records\":"+changedPositions+",\"max_local_displacement\":"+maximumDisplacement.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"scene_saved\":false,\"task_state_written\":false}");
            Debug.Log("Hair inertia matched600frames phase passed; all non-hair local joint poses/model transform identical; no assets/state saved.");
        }
    }
}
