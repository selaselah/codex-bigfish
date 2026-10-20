using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DesktopFish.Editor
{
    public static class FishReadabilityReview
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
                    FishMeshOptimizer.Enabled = true;
                    FishVisualStyle.Enabled = version == 1;
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
                    // Judge the design at its actual 240x320-point display size.
                    var target = new RenderTexture(240, 320, 24) { antiAliasing = 4 };
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
                        var frame = new Texture2D(240, 320, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                        frame.ReadPixels(new Rect(0, 0, 240, 320), 0, 0); frame.Apply();
                        images[version, pose] = frame;
                        if (pose == 0 && version == 1) File.WriteAllBytes("work/readability-idle.png", frame.EncodeToPNG());
                        if (pose == 1) File.WriteAllBytes("work/readability-wave-" + version + ".png", frame.EncodeToPNG());
                        // Also check the actual Retina framebuffer, keeping the
                        // native-point comparison independent of image viewer scale.
                        if (pose <= 1 && version == 1)
                        {
                            var retina = new RenderTexture(480, 640, 24) { antiAliasing = 4 };
                            companion.view.targetTexture = retina;
                            companion.view.Render(); RenderTexture.active = retina;
                            var large = new Texture2D(480, 640, TextureFormat.RGBA32, false);
                            large.ReadPixels(new Rect(0, 0, 480, 640), 0, 0); large.Apply();
                            File.WriteAllBytes("work/readability-retina-" + pose + ".png", large.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(large); UnityEngine.Object.DestroyImmediate(retina);
                            companion.view.targetTexture = target;
                        }
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
                var sheet = new Texture2D(240 * 6, 320 * 2, TextureFormat.RGBA32, false);
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
                    sheet.SetPixels(pose * 240, 320, 240, 320, images[0, pose].GetPixels());
                    sheet.SetPixels(pose * 240, 0, 240, 320, images[1, pose].GetPixels());
                    report.poses++;
                }
                sheet.Apply();
                File.WriteAllBytes("work/readability-comparison.png", sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                var focused = new Texture2D(480, 640, TextureFormat.RGBA32, false);
                var backgrounds = new Texture2D(480, 320, TextureFormat.RGBA32, false);
                for (int version = 0; version < 2; version++)
                    for (int pose = 0; pose < 2; pose++)
                        focused.SetPixels(pose * 240, (1-version) * 320, 240, 320,
                            Composite(images[version, pose].GetPixels(), Color.white));
                backgrounds.SetPixels(0, 0, 240, 320, Composite(images[1, 0].GetPixels(), Color.white));
                backgrounds.SetPixels(240, 0, 240, 320, Composite(images[1, 0].GetPixels(), new Color(0.12f,0.14f,0.20f)));
                focused.Apply(); backgrounds.Apply();
                File.WriteAllBytes("work/readability-focused.png", focused.EncodeToPNG());
                File.WriteAllBytes("work/readability-backgrounds.png", backgrounds.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(focused); UnityEngine.Object.DestroyImmediate(backgrounds);
                var reference = new Texture2D(2,2);
                reference.LoadImage(File.ReadAllBytes("work/reference-dsh/front-reference.png"));
                var referencePixels = new Color[240*320];
                for(int y=0;y<320;y++) for(int x=0;x<240;x++)
                {
                    // Normalize the existing 2D video frame to the same character
                    // height for a visual comparison only; never use it at runtime.
                    Color color=reference.GetPixelBilinear((194+(x+0.5f)*252/240)/reference.width,
                        (5+(y+0.5f)*336/320)/reference.height);
                    float key=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.035f,0.10f,Mathf.Max(color.r,Mathf.Max(color.g,color.b))));
                    referencePixels[y*240+x]=Color.Lerp(Color.white,color,key);
                }
                var referenceSheet=new Texture2D(720,320,TextureFormat.RGBA32,false);
                referenceSheet.SetPixels(0,0,240,320,referencePixels);
                referenceSheet.SetPixels(240,0,240,320,Composite(images[0,0].GetPixels(),Color.white));
                referenceSheet.SetPixels(480,0,240,320,Composite(images[1,0].GetPixels(),Color.white));
                referenceSheet.Apply();
                File.WriteAllBytes("work/reference-2d-design.png",referenceSheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(referenceSheet); UnityEngine.Object.DestroyImmediate(reference);
                report.meanVisibleColorDifference = (float)(error / visible);
                report.silhouetteMismatchFraction = (float)silhouette / union;
                report.passed = report.silhouetteMismatchFraction < 0.04f &&
                    report.geometry.optimizedTriangles < report.geometry.originalTriangles && report.individuallyBakedBlendshapes == 137;
                File.WriteAllText("work/readability-review.json", JsonUtility.ToJson(report, true));
                if (!report.passed) throw new InvalidOperationException("Readability preview lost too much silhouette.");
                Debug.Log("Readability review passed: color " + report.meanVisibleColorDifference + "; silhouette " + report.silhouetteMismatchFraction);
            }
            finally
            {
                FishMeshOptimizer.Enabled = true;
                FishVisualStyle.Enabled = true;
                foreach (var frame in images) if (frame) UnityEngine.Object.DestroyImmediate(frame);
            }
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static Color[] Composite(Color[] pixels, Color background) => pixels.Select(p => {
            var color = Color.Lerp(background, p, p.a); color.a = 1; return color;
        }).ToArray();
    }
}
