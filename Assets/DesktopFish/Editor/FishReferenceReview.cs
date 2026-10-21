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
            File.WriteAllText("work/reference-review/stats.json", JsonUtility.ToJson(new Stats {
                vertices=reference.vertexCount,
                totalTriangles=Enumerable.Range(0,reference.subMeshCount).Sum(s=>(int)reference.GetIndexCount(s)/3),
                materialSlots=reference.subMeshCount,bones=reference.bindposes.Length,expressions=reference.blendShapeCount
            },true));
            RenderTexture.active = null; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(frame); UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("Local Ref.asset review rendered; reference left unchanged.");
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
        [Serializable] sealed class Landmark {public string name;public Vector3 position;}
        [Serializable] sealed class LandmarkReport {public Vector3 minimum,maximum;public PartBounds[] parts;public Landmark[] joints;}
        [Serializable] sealed class Stats {public int vertices,totalTriangles,materialSlots,bones,expressions;}
    }
}
