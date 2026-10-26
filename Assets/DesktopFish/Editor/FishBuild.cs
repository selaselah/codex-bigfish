using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using Kirurobo;

namespace DesktopFish.Editor
{
    public static class FishBuild
    {
        const string ScenePath = "Assets/Scenes/DesktopFish.unity";
        public static bool UseRebuilt;

        [MenuItem("Desktop Fish/Create Scene")]
        public static void CreateScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var pivot = new GameObject("Fish Companion");
            GameObject model;
            if (UseRebuilt) model = FishRebuiltModel.Create();
            else
            {
                string prefabPath = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Mzkagami" })
                    .Select(AssetDatabase.GUIDToAssetPath).Single();
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (!source) throw new InvalidOperationException("Model prefab could not be imported.");
                model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            }
            model.transform.SetParent(pivot.transform, false);
            var animator = model.GetComponent<Animator>();
            if (animator) animator.enabled = false;
            if (!UseRebuilt) FishChibiStyle.Apply(model);
            foreach (var bone in model.GetComponentsInChildren<Transform>(true).Where(_ => !UseRebuilt))
            {
                if (bone.name == "Basin01" || bone.name == "Basin02")
                    bone.gameObject.SetActive(false);
                if (bone.name == "J_Bip_L_UpperArm")
                    bone.localRotation *= Quaternion.Euler(0, 0, 62);
                if (bone.name == "J_Bip_R_UpperArm")
                    bone.localRotation *= Quaternion.Euler(0, 0, -62);
            }
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible model renderers.");
            Bounds bounds = PosedBounds(renderers);
            if (UseRebuilt)
            {
                var transforms = model.GetComponentsInChildren<Transform>(true);
                var positions = transforms.Select(t => t.localPosition).ToArray();
                var rotations = transforms.Select(t => t.localRotation).ToArray();
                var scales = transforms.Select(t => t.localScale).ToArray();
                var pose = new FishMotion(model.transform) { randomActions = false };
                pose.Tick(0, 1, 0, 0, "idle", false);
                bounds = PosedBounds(renderers);
                FishRebuiltModel.ExportIdlePose(model);
                for (int i = 0; i < transforms.Length; i++) {
                    transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i];
                    transforms[i].localScale = scales[i];
                }
            }
            var hitArea = model.AddComponent<CapsuleCollider>();
            hitArea.center = model.transform.InverseTransformPoint(bounds.center);
            hitArea.height = bounds.size.y;
            hitArea.radius = Mathf.Min(bounds.size.x * 0.32f, bounds.size.y * 0.22f);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.aspect = (float)FishCompanion.NativeWidth / FishCompanion.NativeHeight;
            // The rebuilt long side locks swing farther during Cheer. Preserve
            // the same safe viewport margin with a little more framing space.
            float framing=UseRebuilt?1.30f:1.26f;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * framing,
                bounds.extents.x / (360f / 480f) * framing);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            cameraObject.AddComponent<FishPalette>().paletteShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/DesktopFish/Shaders/IllustratedPalette.shader");
            Vector3 center = bounds.center;
            camera.transform.position = center + new Vector3(0, UseRebuilt ? .24f : 0, 3);
            camera.transform.LookAt(center);
            var lightObject = new GameObject("Soft Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.9f;
            light.color = new Color(1, 0.97f, 0.94f);
            lightObject.transform.rotation = Quaternion.Euler(35, -25, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.81f, 0.9f);
            var companion = pivot.AddComponent<FishCompanion>();
            companion.model = model.transform;
            companion.view = camera;
            companion.workPropShader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/DesktopFish/Shaders/WorkProps.shader");
            companion.face = model.GetComponentsInChildren<SkinnedMeshRenderer>()
                .OrderByDescending(r => r.name=="Face").ThenByDescending(r => r.sharedMesh ? r.sharedMesh.blendShapeCount : 0).FirstOrDefault();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var desktopWindow = new GameObject("Desktop Window").AddComponent<UniWindowController>();
            desktopWindow.forceWindowed = true;
            if (!companion.face || !companion.face.sharedMesh)
                throw new InvalidOperationException("The skinned expression mesh is missing.");
            Debug.Log("Desktop model: " + companion.face.sharedMesh.vertexCount + " vertices, " +
                companion.face.sharedMesh.blendShapeCount + " blendshapes.");
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        static Bounds PosedBounds(Renderer[] renderers)
        {
            var result = new Bounds();
            bool initialized = false;
            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skin)
                {
                    var baked = new Mesh();
                    skin.BakeMesh(baked);
                    var vertices = baked.vertices;
                    foreach (int index in VisibleIndices(skin))
                    {
                        Vector3 point = skin.transform.TransformPoint(vertices[index]);
                        if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                        else result.Encapsulate(point);
                    }
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                else if (!initialized) { result = renderer.bounds; initialized = true; }
                else result.Encapsulate(renderer.bounds);
            }
            return result;
        }

        static int[] VisibleIndices(SkinnedMeshRenderer skin)
        {
            var mesh = skin.sharedMesh;
            var indices = new System.Collections.Generic.HashSet<int>();
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var material = skin.sharedMaterials[s];
                // Hidden source panels (alpha=0) must not enlarge the camera frame.
                if (material.HasProperty("_Color") && material.GetColor("_Color").a <= 0.001f) continue;
                foreach (int index in mesh.GetTriangles(s)) indices.Add(index);
            }
            return indices.ToArray();
        }

        [MenuItem("Desktop Fish/Build macOS App")]
        public static void BuildMac()
        {
            FishNativeStatusBuild.Ensure();
            CreateScene();
            PlayerSettings.companyName = "LocalCompanions";
            PlayerSettings.productName = "BigFatBlueFish";
            PlayerSettings.bundleVersion = "0.1.0";
            // Distinct macOS identities let both builds run and be selected side by side.
            // Keep company/product names so the existing state bridge still reaches both.
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, UseRebuilt ?
                "local.desktop.rebuiltbigfish" : "local.desktop.bigfatbluefish");
            PlayerSettings.defaultScreenWidth = FishCompanion.NativeWidth;
            PlayerSettings.defaultScreenHeight = FishCompanion.NativeHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.macRetinaSupport = true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 1); // Apple Silicon.
            QualitySettings.antiAliasing = 4;
            QualitySettings.vSyncCount = 0;
            string target = Path.GetFullPath(UseRebuilt ? "Builds/RebuiltBigFish.app" : "Builds/BigFatBlueFish.app");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = target,
                target = BuildTarget.StandaloneOSX, options = BuildOptions.None
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: " + result.summary.result);
            Debug.Log("Desktop Fish built successfully: " + target);
        }

        [MenuItem("Desktop Fish/Render Preview")]
        public static void RenderPreview()
        {
            CreateScene();
            var camera = Camera.main;
            var target = new RenderTexture(720, 960, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(720, 960, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 720, 960), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.GetFullPath("Preview.png"), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        public static void RenderMotionPreview()
        {
            CreateScene();
            var companion = UnityEngine.Object.FindObjectOfType<FishCompanion>();
            var motion = new FishMotion(companion.model);
            var camera = companion.view;
            int width = UseRebuilt ? 480 : 360, height = UseRebuilt ? 640 : 480;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing = 4;
            var frame = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var sheet = new Texture2D(width * 6, height, TextureFormat.RGBA32, false);
            var skins = companion.model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var snapshots = new MeshFilter[skins.Length];
            for (int s = 0; s < skins.Length; s++)
            {
                var snapshot = new GameObject("Pose Snapshot");
                snapshot.transform.SetParent(skins[s].transform, false);
                snapshots[s] = snapshot.AddComponent<MeshFilter>();
                snapshot.AddComponent<MeshRenderer>().sharedMaterials = skins[s].sharedMaterials;
                skins[s].enabled = false;
            }
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                for (int i = 0; i < 6; i++)
                {
                    motion.Play((FishMotion.Gesture)i, 0);
                    motion.Evaluate(i == 5 ? 0.65f : i == 3 ? 3 : 1.5f, 0, 0, "idle", false);
                    ApplyMotionExpressions(companion, motion);
                    for (int s = 0; s < skins.Length; s++)
                    {
                        if (snapshots[s].sharedMesh) UnityEngine.Object.DestroyImmediate(snapshots[s].sharedMesh);
                        var baked = new Mesh();
                        skins[s].BakeMesh(baked);
                        snapshots[s].sharedMesh = baked;
                    }
                    camera.Render();
                    RenderTexture.active = target;
                    frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    frame.Apply();
                    if (frame.GetPixel(0, 0).a > 0.001f || frame.GetPixel(width - 1, height - 1).a > 0.001f)
                        throw new InvalidOperationException("Palette must preserve transparent corners.");
                    sheet.SetPixels(i * width, 0, width, height, frame.GetPixels());
                    if (i == 0) File.WriteAllBytes(Path.GetFullPath("Preview.png"), frame.EncodeToPNG());
                }
                sheet.Apply();
                Directory.CreateDirectory("work");
                File.WriteAllBytes(Path.GetFullPath("work/motion-preview.png"), sheet.EncodeToPNG());
                ValidateMotion(companion, motion, skins);
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
                    if (snapshots[s].sharedMesh) UnityEngine.Object.DestroyImmediate(snapshots[s].sharedMesh);
                    UnityEngine.Object.DestroyImmediate(snapshots[s].gameObject);
                    skins[s].enabled = true;
                }
            }
        }

        internal static void ApplyMotionExpressions(FishCompanion companion, FishMotion motion)
        {
            foreach(var renderer in companion.model.GetComponentsInChildren<SkinnedMeshRenderer>())
            for (int shape = 0; shape < renderer.sharedMesh.blendShapeCount; shape++)
            {
                string name = renderer.sharedMesh.GetBlendShapeName(shape);
                renderer.SetBlendShapeWeight(shape, name == "Fcl_EYE_Close" ? motion.BlinkWeight() :
                    name == "Fcl_EYE_Joy" ? Mathf.Max(0,motion.SmileEyes-motion.Joy) : name == "Fcl_ALL_Joy" ? motion.Joy : 0);
            }
        }

        internal static void ValidateMotion(FishCompanion companion, FishMotion motion, SkinnedMeshRenderer[] skins)
        {
            Vector2 min = Vector2.one, max = Vector2.zero;
            int poses = 0;
            float closestMargin=float.PositiveInfinity;string boundaryContext="";
            var baked = new Mesh();
            var visible = skins.ToDictionary(s => s, VisibleIndices);
            try
            {
                for (int action = 0; action < 6; action++)
                    for (int sample = 0; sample <= 24; sample++)
                    {
                        motion.Play((FishMotion.Gesture)action, 0);
                        float time = sample / 24f * (action == 1 ? FishMotion.WaveDuration : action == 3 ? 7 : action == 5 ? 2.4f : 4.2f);
                        motion.Evaluate(time, sample % 2 == 0 ? -1 : 1, sample % 3 == 0 ? -1 : 1, "idle", false);
                        if(action!=1&&motion.SmileEyes!=0)
                            throw new InvalidOperationException("Smile eyes leaked outside the wave gesture.");
                        ApplyMotionExpressions(companion, motion);
                        foreach (var skin in skins)
                        {
                            skin.BakeMesh(baked);
                            var vertices = baked.vertices;
                            foreach (int index in visible[skin])
                            {
                                Vector3 p = companion.view.WorldToViewportPoint(skin.transform.TransformPoint(vertices[index]));
                                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y))
                                    throw new InvalidOperationException("Invalid deformed vertex in " + (FishMotion.Gesture)action);
                                float margin=Mathf.Min(p.x,p.y,1-p.x,1-p.y);
                                if(margin<closestMargin) {
                                    closestMargin=margin;
                                    boundaryContext=(FishMotion.Gesture)action+" sample "+sample+" / "+skin.name+" vertex "+index;
                                }
                                min = Vector2.Min(min, p);
                                max = Vector2.Max(max, p);
                            }
                        }
                        poses++;
                    }
                if (min.x < 0.02f || min.y < 0.02f || max.x > 0.98f || max.y > 0.98f)
                    throw new InvalidOperationException("Gesture exceeds safe camera bounds: " + min + " / " + max + "; nearest edge: " + boundaryContext);
                int expectedShapes = companion.model.GetComponent<FishRigProfile>() ? 6 : 137;
                if (companion.face.sharedMesh.blendShapeCount != expectedShapes)
                    throw new InvalidOperationException("Unexpected expression channel count.");
                File.WriteAllText("work/motion-validation.json", "{\n  \"sampled_poses\": " + poses +
                    ",\n  \"blendshapes\": " + expectedShapes + ",\n  \"viewport_min\": [" + min.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                    ", " + min.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "],\n  \"viewport_max\": [" +
                    max.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + ", " +
                    max.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "],\n  \"passed\": true\n}\n");
                Debug.Log("Desktop Fish motion validation passed: " + poses + " poses; viewport " + min + " / " + max);
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }
    }
}
