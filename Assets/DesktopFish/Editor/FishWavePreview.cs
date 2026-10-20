using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DesktopFish.Editor
{
    public static class FishWavePreview
    {
        [Serializable] sealed class Report
        {
            public int frames;
            public float minimumElbowFlex = 180, maximumElbowFlex;
            public float minimumPalmForward = 1, maximumWristDrift;
            public float endpointRotationDifference;
            public bool passed;
        }

        public static void Render()
        {
            FishBuild.CreateScene();
            var companion = UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion = new FishMotion(companion.model);
            var camera = companion.view;
            camera.backgroundColor = Color.white;
            var skins = companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var filters = new MeshFilter[skins.Length];
            for (int s = 0; s < skins.Length; s++)
            {
                var snapshot = new GameObject("Wave Snapshot");
                snapshot.transform.SetParent(skins[s].transform, false);
                filters[s] = snapshot.AddComponent<MeshFilter>();
                snapshot.AddComponent<MeshRenderer>().sharedMaterials = skins[s].sharedMaterials;
                skins[s].enabled = false;
            }
            var target = new RenderTexture(360, 480, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var frame = new Texture2D(360, 480, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(2160, 960, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            var report = new Report();
            var all = companion.model.GetComponentsInChildren<Transform>(true).ToDictionary(t => t.name);
            var referenceWrists = new Vector3[2];
            bool haveReference = false;
            int[] keyframes = { 0, 10, 20, 30, 40, 50, 60, 70, 84, 100, 112, 125 };
            Directory.CreateDirectory("work/wave-frames");
            motion.Play(FishMotion.Gesture.Wave, 0);
            try
            {
                camera.targetTexture = target;
                int count = Mathf.CeilToInt(FishMotion.WaveDuration * 24) + 1;
                for (int f = 0; f < count; f++)
                {
                    float time = f / 24f;
                    motion.Evaluate(time, 0, 0, "idle", false);
                    if (time >= 1.4f && time <= 3.4f)
                    {
                        for (int side = 0; side < 2; side++)
                        {
                            string prefix = "J_Bip_" + (side == 0 ? "L" : "R") + "_";
                            Transform upper = all[prefix + "UpperArm"], elbow = all[prefix + "LowerArm"], hand = all[prefix + "Hand"];
                            float flex = Vector3.Angle(elbow.position - upper.position, hand.position - elbow.position);
                            float forward = Vector3.Dot(hand.TransformDirection(Vector3.down), companion.model.forward);
                            Vector3 wrist = companion.model.InverseTransformPoint(hand.position);
                            if (!haveReference) referenceWrists[side] = wrist;
                            report.maximumWristDrift = Mathf.Max(report.maximumWristDrift, Vector3.Distance(wrist, referenceWrists[side]));
                            report.minimumElbowFlex = Mathf.Min(report.minimumElbowFlex, flex);
                            report.maximumElbowFlex = Mathf.Max(report.maximumElbowFlex, flex);
                            report.minimumPalmForward = Mathf.Min(report.minimumPalmForward, forward);
                        }
                        haveReference = true;
                    }
                    for (int shape = 0; shape < companion.face.sharedMesh.blendShapeCount; shape++)
                    {
                        string name = companion.face.sharedMesh.GetBlendShapeName(shape);
                        companion.face.SetBlendShapeWeight(shape, name == "Fcl_ALL_Joy" ? motion.Joy :
                            name == "Fcl_EYE_Close" ? motion.ClosedEyes : 0);
                    }
                    for (int s = 0; s < skins.Length; s++)
                    {
                        if (filters[s].sharedMesh) UnityEngine.Object.DestroyImmediate(filters[s].sharedMesh);
                        var baked = new Mesh();
                        skins[s].BakeMesh(baked);
                        filters[s].sharedMesh = baked;
                    }
                    camera.Render();
                    RenderTexture.active = target;
                    frame.ReadPixels(new Rect(0, 0, 360, 480), 0, 0);
                    frame.Apply();
                    File.WriteAllBytes("work/wave-frames/frame" + f.ToString("D3") + ".png", frame.EncodeToPNG());
                    int slot = Array.IndexOf(keyframes, f);
                    if (slot >= 0) sheet.SetPixels((slot % 6) * 360, (1 - slot / 6) * 480, 360, 480, frame.GetPixels());
                    report.frames++;
                }
                sheet.Apply();
                File.WriteAllBytes("work/wave-preview.png", sheet.EncodeToPNG());
                foreach (float time in new[] { 0f, FishMotion.WaveDuration })
                {
                    motion.Play(FishMotion.Gesture.Wave, 0);
                    motion.Evaluate(time, 0, 0, "idle", false);
                    var rotations = all.ToDictionary(p => p.Key, p => p.Value.localRotation);
                    motion.Play(FishMotion.Gesture.None, 0);
                    motion.Evaluate(time, 0, 0, "idle", false);
                    foreach (var pair in all)
                        report.endpointRotationDifference = Mathf.Max(report.endpointRotationDifference,
                            Quaternion.Angle(rotations[pair.Key], pair.Value.localRotation));
                }
                if (report.minimumElbowFlex < 25 || report.maximumElbowFlex > 145 ||
                    report.minimumPalmForward < 0.95f || report.maximumWristDrift > 0.006f ||
                    report.endpointRotationDifference > 0.1f)
                    throw new InvalidOperationException("Wave pose validation failed: " + JsonUtility.ToJson(report));
                FishBuild.ValidateMotion(companion, motion, skins);
                report.passed = true;
                File.WriteAllText("work/wave-validation.json", JsonUtility.ToJson(report, true));
                Debug.Log("Wave validation passed: " + JsonUtility.ToJson(report));
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(frame);
                UnityEngine.Object.DestroyImmediate(sheet);
                for (int s = 0; s < skins.Length; s++)
                {
                    if (filters[s].sharedMesh) UnityEngine.Object.DestroyImmediate(filters[s].sharedMesh);
                    UnityEngine.Object.DestroyImmediate(filters[s].gameObject);
                    skins[s].enabled = true;
                }
            }
        }
    }
}
