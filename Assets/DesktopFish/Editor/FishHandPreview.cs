using System.IO;
using System.Linq;
using UnityEngine;

namespace DesktopFish.Editor
{
    public static class FishHandPreview
    {
        public static void Before() => Render("before");
        public static void After() => Render("after");

        static void Render(string suffix)
        {
            FishBuild.CreateScene();
            var companion = Object.FindObjectOfType<FishCompanion>();
            var motion = new FishMotion(companion.model);
            var camera = companion.view;
            camera.backgroundColor = Color.white;
            camera.aspect = 1;
            var wrist = companion.model.GetComponentsInChildren<Transform>(true)
                .Single(t => t.name == "J_Bip_L_Hand");
            var rest = companion.model.GetComponentsInChildren<Transform>(true).ToDictionary(t => t, t => t.localRotation);
            var skins = companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots = new MeshFilter[skins.Length];
            for (int i = 0; i < skins.Length; i++)
            {
                var go = new GameObject("Hand Preview Snapshot");
                go.transform.SetParent(skins[i].transform, false);
                snapshots[i] = go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>().sharedMaterials = skins[i].sharedMaterials;
                skins[i].enabled = false;
            }
            var rt = new RenderTexture(512, 512, 24) { antiAliasing = 4 };
            var frame = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(1536, 1024, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                for (int pose = 0; pose < 3; pose++)
                {
                    motion.Play(pose == 0 ? FishMotion.Gesture.None : FishMotion.Gesture.Wave, 0);
                    motion.Evaluate(1.8f, 0, 0, "idle", false);
                    // Also inspect the rounded fingers folded into the palm.
                    if (pose == 2)
                        foreach (var bone in companion.model.GetComponentsInChildren<Transform>(true))
                            if (bone.name.StartsWith("J_Bip_L_") &&
                                new[] { "Index", "Middle", "Ring", "Little" }.Any(f => bone.name.Contains(f)))
                                bone.localRotation = rest[bone] * Quaternion.Euler(0, 0,
                                    bone.name.EndsWith("1") ? 24 : bone.name.EndsWith("2") ? 34 : 18);
                    for (int i = 0; i < skins.Length; i++)
                    {
                        if (snapshots[i].sharedMesh) Object.DestroyImmediate(snapshots[i].sharedMesh);
                        var baked = new Mesh();
                        skins[i].BakeMesh(baked);
                        snapshots[i].sharedMesh = baked;
                    }
                    Vector3 center = wrist.position + companion.model.up * (pose == 0 ? -0.008f : 0.025f);
                    camera.orthographicSize = 0.068f;
                    for (int angle = 0; angle < 2; angle++)
                    {
                        camera.transform.position = center + Quaternion.AngleAxis(angle * -55, companion.model.up) * companion.model.forward * 3;
                        camera.transform.LookAt(center);
                        camera.Render();
                        RenderTexture.active = rt;
                        frame.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                        frame.Apply();
                        if (suffix == "after" && pose == 1 && angle == 0)
                            File.WriteAllBytes("work/hand-open.png", frame.EncodeToPNG());
                        sheet.SetPixels(pose * 512, (1 - angle) * 512, 512, 512, frame.GetPixels());
                    }
                }
                sheet.Apply();
                Directory.CreateDirectory("work");
                File.WriteAllBytes("work/hand-" + suffix + ".png", sheet.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(frame);
                Object.DestroyImmediate(sheet);
                for (int i = 0; i < skins.Length; i++)
                {
                    Object.DestroyImmediate(snapshots[i].sharedMesh);
                    Object.DestroyImmediate(snapshots[i].gameObject);
                    skins[i].enabled = true;
                }
            }
        }
    }
}
