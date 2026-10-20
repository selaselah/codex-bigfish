using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DesktopFish.Editor
{
    public static class FishOptimizationReview
    {
        [Serializable] sealed class Comparison
        {
            public FishMeshOptimizer.Report geometry;
            public float meanVisibleColorDifference;
            public float silhouetteMismatchFraction;
            public int poses, individuallyBakedBlendshapes;
            public bool passed;
        }
        public static void Run()
        {
            Directory.CreateDirectory("work");
            var images = new Texture2D[2, 6];
            var report = new Comparison();
            try
            {
                for (int version = 0; version < 2; version++)
                {
                    FishMeshOptimizer.Enabled = version == 1;
                    FishBuild.CreateScene();
                    var companion = UnityEngine.Object.FindObjectOfType<FishCompanion>();
                    var motion = new FishMotion(companion.model);
                    var skins = companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var filters = new MeshFilter[skins.Length];
                    for (int i = 0; i < skins.Length; i++)
                    {
                        var snapshot = new GameObject("Optimization Snapshot");
                        snapshot.transform.SetParent(skins[i].transform, false);
                        filters[i] = snapshot.AddComponent<MeshFilter>();
                        snapshot.AddComponent<MeshRenderer>().sharedMaterials = skins[i].sharedMaterials;
                        skins[i].enabled = false;
                    }
                    // Retina 2x pixels for the new 240x320-point desktop window.
                    var target = new RenderTexture(480, 640, 24) { antiAliasing = 4 };
                    var previous = RenderTexture.active;
                    companion.view.targetTexture = target;
                    for (int pose = 0; pose < 6; pose++)
                    {
                        motion.Play((FishMotion.Gesture)pose, 0);
                        motion.Evaluate(pose == 5 ? 0.65f : pose == 3 ? 3 : 1.5f, 0, 0, "idle", false);
                        for (int shape = 0; shape < companion.face.sharedMesh.blendShapeCount; shape++)
                        {
                            string name = companion.face.sharedMesh.GetBlendShapeName(shape);
                            companion.face.SetBlendShapeWeight(shape, name == "Fcl_ALL_Joy" ? motion.Joy :
                                name == "Fcl_EYE_Close" ? motion.ClosedEyes : 0);
                        }
                        for (int i = 0; i < skins.Length; i++)
                        {
                            if (filters[i].sharedMesh) UnityEngine.Object.DestroyImmediate(filters[i].sharedMesh);
                            var baked = new Mesh(); skins[i].BakeMesh(baked); filters[i].sharedMesh = baked;
                        }
                        companion.view.Render();
                        RenderTexture.active = target;
                        var frame = new Texture2D(480, 640, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                        frame.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); frame.Apply();
                        images[version, pose] = frame;
                        if (pose == 1) File.WriteAllBytes("work/optimized-wave-" + version + ".png", frame.EncodeToPNG());
                    }
                    companion.view.targetTexture = null;
                    RenderTexture.active = previous;
                    UnityEngine.Object.DestroyImmediate(target);
                    for (int i = 0; i < skins.Length; i++)
                    {
                        UnityEngine.Object.DestroyImmediate(filters[i].sharedMesh);
                        UnityEngine.Object.DestroyImmediate(filters[i].gameObject);
                        skins[i].enabled = true;
                    }
                    if (version == 1)
                    {
                        report.geometry = FishMeshOptimizer.LastReport;
                        FishBuild.ValidateMotion(companion, motion, skins);
                        var baked = new Mesh();
                        for (int shape = 0; shape < companion.face.sharedMesh.blendShapeCount; shape++)
                        {
                            for (int j = 0; j < companion.face.sharedMesh.blendShapeCount; j++)
                                companion.face.SetBlendShapeWeight(j, j == shape ? 100 : 0);
                            companion.face.BakeMesh(baked);
                            if (baked.vertices.Any(p => !Finite(p.x) || !Finite(p.y) || !Finite(p.z)))
                                throw new InvalidOperationException("Invalid blendshape " + shape);
                            report.individuallyBakedBlendshapes++;
                        }
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }
                var sheet = new Texture2D(480 * 6, 640 * 2, TextureFormat.RGBA32, false);
                long visible = 0, silhouette = 0, union = 0;
                double error = 0;
                for (int pose = 0; pose < 6; pose++)
                {
                    var a = images[0, pose].GetPixels32(); var b = images[1, pose].GetPixels32();
                    for (int i = 0; i < a.Length; i++)
                    {
                        bool aa = a[i].a > 127, bb = b[i].a > 127;
                        if (aa || bb) { union++; if (aa != bb) silhouette++; }
                        if (aa && bb)
                        {
                            visible++;
                            error += (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b)) / (3.0 * 255);
                        }
                    }
                    sheet.SetPixels(pose * 480, 640, 480, 640, images[0, pose].GetPixels());
                    sheet.SetPixels(pose * 480, 0, 480, 640, images[1, pose].GetPixels());
                    report.poses++;
                }
                sheet.Apply();
                File.WriteAllBytes("work/optimization-comparison.png", sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                report.meanVisibleColorDifference = (float)(error / visible);
                report.silhouetteMismatchFraction = (float)silhouette / union;
                report.passed = report.meanVisibleColorDifference < 0.025f && report.silhouetteMismatchFraction < 0.01f &&
                    report.geometry.optimizedTriangles < report.geometry.originalTriangles && report.individuallyBakedBlendshapes == 137;
                File.WriteAllText("work/optimization-review.json", JsonUtility.ToJson(report, true));
                if (!report.passed) throw new InvalidOperationException("Optimization image comparison exceeded limits.");
                Debug.Log("Optimization review passed: color " + report.meanVisibleColorDifference + "; silhouette " + report.silhouetteMismatchFraction);
            }
            finally
            {
                FishMeshOptimizer.Enabled = true;
                foreach (var frame in images) if (frame) UnityEngine.Object.DestroyImmediate(frame);
            }
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
