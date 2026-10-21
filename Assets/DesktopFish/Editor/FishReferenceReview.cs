using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesktopFish.Editor
{
    // Local visual review only. Reference geometry/materials are never exported
    // into the rebuilt model or its portable deliverables.
    public static class FishReferenceReview
    {
        public static void Render()
        {
            AssetDatabase.Refresh();
            var reference = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/DesktopFish/Generated/Ref.asset");
            if (!reference) throw new InvalidOperationException("Local Ref.asset is missing.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string sourcePath = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Mzkagami" })
                .Select(AssetDatabase.GUIDToAssetPath).Single();
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
            var animator = model.GetComponent<Animator>();
            if (animator) animator.enabled = false;
            FishChibiStyle.Apply(model);
            var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().Single();
            if (reference.subMeshCount != skin.sharedMaterials.Length || reference.bindposes.Length != skin.bones.Length)
                throw new InvalidOperationException("Reference does not match the local review rig/material slots.");
            skin.sharedMesh = reference;
            skin.localBounds = reference.bounds;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "Basin01" || bone.name == "Basin02") bone.gameObject.SetActive(false);
                if (bone.name == "J_Bip_L_UpperArm") bone.localRotation *= Quaternion.Euler(0, 0, 62);
                if (bone.name == "J_Bip_R_UpperArm") bone.localRotation *= Quaternion.Euler(0, 0, -62);
            }
            var motion = new FishMotion(model.transform) { randomActions = false };
            motion.Tick(0, 1, 0, 0, "idle", false);
            var baked = new Mesh(); skin.BakeMesh(baked);
            var snapshot = new GameObject("Local reference snapshot");
            snapshot.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
            snapshot.transform.localScale = skin.transform.lossyScale;
            snapshot.AddComponent<MeshFilter>().sharedMesh = baked;
            snapshot.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
            model.SetActive(false);
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.orthographic = true; camera.aspect = .75f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.nearClipPlane = .01f; camera.farClipPlane = 20;
            camera.gameObject.AddComponent<FishPalette>().paletteShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/DesktopFish/Shaders/IllustratedPalette.shader");
            var light = new GameObject("Review light").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = .9f; light.color = new Color(1,.97f,.94f);
            light.transform.rotation = Quaternion.Euler(35,-25,0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.78f,.81f,.9f);
            var bounds = snapshot.GetComponent<Renderer>().bounds;
            WriteLandmarks(reference, baked, skin, snapshot.transform, bounds);
            camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x/.75f)*1.17f;
            var target = new RenderTexture(720,960,24) { antiAliasing = 4 };
            var frame = new Texture2D(720,960,TextureFormat.RGBA32,false);
            camera.targetTexture = target; Directory.CreateDirectory("work/reference-review");
            foreach (int angle in new[] {0,35,90})
            {
                camera.transform.position = bounds.center + Quaternion.Euler(0,angle,0)*new Vector3(0,.24f,3);
                camera.transform.LookAt(bounds.center); camera.Render(); RenderTexture.active = target;
                frame.ReadPixels(new Rect(0,0,720,960),0,0); frame.Apply();
                File.WriteAllBytes("work/reference-review/ref-"+angle+".png",frame.EncodeToPNG());
            }
            ReviewSectionParts(baked, snapshot, camera, bounds, frame, target);
            ReviewFaceSections(baked,snapshot.transform);
            File.WriteAllText("work/reference-review/stats.json", JsonUtility.ToJson(new Stats {
                vertices=reference.vertexCount,
                totalTriangles=Enumerable.Range(0,reference.subMeshCount).Sum(s=>(int)reference.GetIndexCount(s)/3),
                materialSlots=reference.subMeshCount,bones=reference.bindposes.Length,expressions=reference.blendShapeCount
            },true));
            RenderTexture.active = null; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(frame); UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("Local Ref.asset review rendered; reference left unchanged.");
        }
        // Sparse cross-section bounds are local modelling guides. The isolated
        // previews expose which lock owns a silhouette without exporting a mesh.
        static void ReviewSectionParts(Mesh baked, GameObject snapshot, Camera camera,
            Bounds total, Texture2D frame, RenderTexture target) {
            var filter=snapshot.GetComponent<MeshFilter>();
            var points=baked.vertices.Select(snapshot.transform.TransformPoint).ToArray();
            var sections=new System.Collections.Generic.List<HairSection>();
            foreach(int slot in new[]{3,10,17,21}) {
                int[] triangles=baked.GetTriangles(slot);
                foreach(float y in new[]{.30f,.35f,.40f,.45f,.50f,.55f,.60f,.65f}) {
                    var cuts=new System.Collections.Generic.List<Vector3>();
                    for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++) {
                        Vector3 a=points[triangles[i+j]],b=points[triangles[i+(j+1)%3]];
                        if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y)) {
                            Vector3 p=Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y));
                            if(p.x>0)cuts.Add(p);
                        }
                    }
                    if(cuts.Count==0)continue;
                    var bounds=new Bounds(cuts[0],Vector3.zero);
                    foreach(var point in cuts)bounds.Encapsulate(point);
                    sections.Add(new HairSection{slot=slot,height=y,minimum=bounds.min,maximum=bounds.max,
                        inner=cuts.OrderBy(p=>p.x).First(),outer=cuts.OrderByDescending(p=>p.x).First()});
                }
                var isolated=UnityEngine.Object.Instantiate(baked);
                for(int sub=0;sub<isolated.subMeshCount;sub++)
                    if(sub!=slot)isolated.SetTriangles(Array.Empty<int>(),sub,false);
                filter.sharedMesh=isolated;
                foreach(int angle in new[]{0,35,90}) {
                    camera.transform.position=total.center+Quaternion.Euler(0,angle,0)*new Vector3(0,.24f,3);
                    camera.transform.LookAt(total.center);camera.Render();RenderTexture.active=target;
                    frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                    string label=slot==3?"face":"hair-"+slot;
                    File.WriteAllBytes("work/reference-review/"+label+"-"+angle+".png",frame.EncodeToPNG());
                }
                filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
            }
            File.WriteAllText("work/reference-review/hair-sections.json",JsonUtility.ToJson(
                new HairSectionReport{sections=sections.ToArray()},true));
        }
        static void ReviewFaceSections(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var triangles=baked.GetTriangles(3);
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(float y in new[]{.385f,.39f,.40f,.42f,.44f,.46f,.48f,.50f,.55f,.60f,.65f})
                foreach(float x in new[]{0f,.025f,.050f,.075f,.10f,.125f,.15f}) {
                    float front=float.NegativeInfinity;
                    for(int i=0;i<triangles.Length;i+=3) {
                        var cuts=new System.Collections.Generic.List<Vector3>();
                        for(int j=0;j<3;j++) {
                            var a=points[triangles[i+j]];var b=points[triangles[i+(j+1)%3]];
                            if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cuts.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                        }
                        if(cuts.Count!=2)continue;
                        var c=cuts[0];var d=cuts[1];
                        if(Mathf.Abs(d.x-c.x)<1e-7f||x<Mathf.Min(c.x,d.x)||x>Mathf.Max(c.x,d.x))continue;
                        front=Mathf.Max(front,Mathf.Lerp(c.z,d.z,(x-c.x)/(d.x-c.x)));
                    }
                    if(!float.IsNegativeInfinity(front))samples.Add(new Landmark{name="front",position=new Vector3(x,y,front)});
                }
            File.WriteAllText("work/reference-review/face-sections.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }
        // Aggregate measurements only: no reference vertices, triangles, UVs,
        // weights or expression data leave this local review routine.
        static void WriteLandmarks(Mesh source, Mesh baked, SkinnedMeshRenderer skin, Transform snapshot, Bounds total) {
            Directory.CreateDirectory("work/reference-review");
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var parts=Enumerable.Range(0,source.subMeshCount).Select(index=>{
                int[] used=source.GetTriangles(index).Distinct().ToArray();
                var bounds=new Bounds(points[used[0]],Vector3.zero);
                foreach(int vertex in used)bounds.Encapsulate(points[vertex]);
                return new PartBounds {slot=index,name=skin.sharedMaterials[index].name,
                    minimum=bounds.min,maximum=bounds.max};
            }).ToArray();
            string[] names={"J_Bip_C_Head","J_Bip_C_Neck","J_Bip_C_Hips","J_Adj_L_FaceEye","J_Adj_R_FaceEye",
                "J_Bip_L_Eye","J_Bip_R_Eye","J_Bip_L_Hand","J_Bip_R_Hand","J_Bip_L_Foot","J_Bip_R_Foot"};
            var joints=skin.bones.Where(b=>names.Contains(b.name)).Select(b=>new Landmark {name=b.name,position=b.position}).ToArray();
            File.WriteAllText("work/reference-review/landmarks.json",JsonUtility.ToJson(new LandmarkReport {
                minimum=total.min,maximum=total.max,parts=parts,joints=joints
            },true));
        }
        [Serializable] sealed class PartBounds {public int slot;public string name;public Vector3 minimum,maximum;}
        [Serializable] sealed class HairSection {public int slot;public float height;public Vector3 minimum,maximum,inner,outer;}
        [Serializable] sealed class HairSectionReport {public HairSection[] sections;}
        [Serializable] sealed class FaceSectionReport {public Landmark[] samples;}
        [Serializable] sealed class Landmark {public string name;public Vector3 position;}
        [Serializable] sealed class LandmarkReport {public Vector3 minimum,maximum;public PartBounds[] parts;public Landmark[] joints;}
        [Serializable] sealed class Stats {public int vertices,totalTriangles,materialSlots,bones,expressions;}
    }
}
