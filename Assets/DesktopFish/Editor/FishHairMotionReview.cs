using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace DesktopFish.Editor {
    // Matched offline working poses; never writes the scene, app state or tasks.
    public static class FishHairMotionReview {
        [Serializable] sealed class Pose {public Vector3[] positions,scales;public Quaternion[] rotations;}
        public static void VerifyFinal(){FishHairSmooth5K.RenderStudy();Render();}
        public static void Render() {
            const int frames=240,width=640,height=720;
            bool checkOnly=Environment.GetEnvironmentVariable("FISH_HAIR_CHECK_ONLY")=="1";
            float gain=float.Parse(Environment.GetEnvironmentVariable("FISH_HAIR_GAIN")??"1",System.Globalization.CultureInfo.InvariantCulture);
            string folder="work/hair-smooth-212/motion";Directory.CreateDirectory(folder);
            Pose[] baseline=new Pose[frames];var baselineVertices=new Vector3[frames][][];var baselineValid=new bool[frames][][];var baselineCross=new Vector3[frames][][];float maximumDisplacement=0;int changedPositions=0,newAuthoredNormalFacing=0;
            for(int version=0;version<2;version++) {
                EditorSceneManager.OpenScene("Assets/Scenes/DesktopFish.unity");
                var c=UnityEngine.Object.FindObjectOfType<FishCompanion>();
                FishLowPoly5K.RestoreFullModel(c.model.gameObject);FishLowPoly5K.Apply(c.model.gameObject,false);
                var skins=c.model.GetComponentsInChildren<SkinnedMeshRenderer>();
                
                var transforms=c.model.GetComponentsInChildren<Transform>(true);
                var hairMasks=skins.Select(s=>s.sharedMesh.boneWeights.Select(w=>s.sharedMesh.name=="RearHairCards"||new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(i=>i>=5&&i<=9)).ToArray()).ToArray();
                var motion=new FishMotion(c.model){randomActions=false,secondaryHair=version==1,secondaryHairScale=gain};
                var meshes=skins.Select(s=>new Mesh()).ToArray();
                for(int i=0;i<skins.Length;i++) {
                    var go=new GameObject("Hair motion snapshot");go.transform.SetParent(skins[i].transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=meshes[i];go.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;
                }
                var camera=c.view;camera.backgroundColor=Color.white;camera.aspect=width/(float)height;camera.orthographicSize=.78f;
                var target=new RenderTexture(width,height,24){antiAliasing=4};var image=new Texture2D(width,height,TextureFormat.RGBA32,false);camera.targetTexture=target;
                using(var station=new FishWorkStation(c.model,c.workPropShader,159,0)) {
                    // Settle the working blend before the paired recorded interval.
                    for(int frame=-90;frame<frames;frame++) {
                        float now=100+frame/30f;
                        motion.Tick(now,1f/30,0,0,"working",false);station.Tick(now,motion.WorkWeight,2,true);FishBuild.ApplyMotionExpressions(c,motion);
                        if(frame<0)continue;
                        var pose=new Pose{positions=transforms.Select(t=>t.localPosition).ToArray(),scales=transforms.Select(t=>t.localScale).ToArray(),rotations=transforms.Select(t=>t.localRotation).ToArray()};
                        if(version==0)baseline[frame]=pose;
                        else for(int i=0;i<transforms.Length;i++) {
                            if(pose.positions[i]!=baseline[frame].positions[i]||pose.scales[i]!=baseline[frame].scales[i]||pose.rotations[i]!=baseline[frame].rotations[i])
                                throw new InvalidOperationException("Hair follow-through changed body, grip or other joint pose: "+transforms[i].name);
                        }
                        if(version==0){baselineVertices[frame]=new Vector3[skins.Length][];baselineValid[frame]=new bool[skins.Length][];baselineCross[frame]=new Vector3[skins.Length][];}
                        for(int i=0;i<skins.Length;i++) {
                            skins[i].BakeMesh(meshes[i]);
                            var p=meshes[i].vertices;var n=meshes[i].normals;var triangles=meshes[i].triangles;
                            if(p.Any(q=>float.IsNaN(q.x)||float.IsNaN(q.y)||float.IsNaN(q.z)||float.IsInfinity(q.x)||float.IsInfinity(q.y)||float.IsInfinity(q.z)))throw new InvalidOperationException("Nonfinite hair motion.");
                            var valid=new bool[triangles.Length/3];var crosses=new Vector3[valid.Length];
                            for(int f=0;f<valid.Length;f++) {
                                int a=triangles[3*f],b=triangles[3*f+1],z=triangles[3*f+2];Vector3 cross=Vector3.Cross(p[b]-p[a],p[z]-p[a]);crosses[f]=cross;valid[f]=cross.sqrMagnitude>1e-18f&&Vector3.Dot(cross,n[a]+n[b]+n[z])>=-1e-16f;
                                if(version==1) {
                                    if(baselineValid[frame][i][f]&&!valid[f])newAuthoredNormalFacing++;
                                    Vector3 before=baselineCross[frame][i][f];
                                    if(before.sqrMagnitude>1e-18f&&(cross.sqrMagnitude<=1e-18f||Vector3.Dot(before,cross)<=0))throw new InvalidOperationException("New geometric hair degeneracy/reversal: "+skins[i].sharedMesh.name+" frame"+frame+" face"+f);
                                }
                            }
                            if(version==0){baselineVertices[frame][i]=p;baselineValid[frame][i]=valid;baselineCross[frame][i]=crosses;}
                            else for(int j=0;j<p.Length;j++) {
                                float displacement=(p[j]-baselineVertices[frame][i][j]).magnitude;maximumDisplacement=Mathf.Max(maximumDisplacement,displacement);if(displacement>1e-7f)changedPositions++;
                                if(!hairMasks[i][j]&&p[j]!=baselineVertices[frame][i][j])throw new InvalidOperationException("Hair changed protected non-hair pose.");
                            }
                        }
                        if(checkOnly)continue;
                        Vector3 center=new Vector3(0,.55f,0);float angle=frame<120?0:165;
                        camera.transform.position=center+Quaternion.Euler(0,angle,0)*Vector3.forward*3;camera.transform.LookAt(center);
                        camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                        File.WriteAllBytes(folder+"/"+(version==0?"before":"after")+frame.ToString("D4")+".png",image.EncodeToPNG());
                    }
                }
                motion.Dispose();camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
            }
            File.WriteAllText(folder+"/Validation.json","{\"frames_per_version\":240,\"fps\":30,\"non_hair_local_poses_exact\":true,\"face_poses_exact\":true,\"new_geometric_degenerate_reversed\":0,\"inherited_authored_normal_facing_transitions\":"+newAuthoredNormalFacing+",\"changed_posed_records\":"+changedPositions+",\"max_local_displacement\":"+maximumDisplacement.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"scene_saved\":false,\"task_state_written\":false}");
            Debug.Log("Hair motion matched480frames passed; all non-hair local joint poses/model transform identical; no assets/state saved.");
        }
    }
}
