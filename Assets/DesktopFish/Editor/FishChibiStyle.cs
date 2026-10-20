using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DesktopFish.Editor
{
    // Generate a separate rest mesh and matching skeleton; never edit the source avatar.
    public static class FishChibiStyle
    {
        const string Folder = "Assets/DesktopFish/Generated";
        const float Neck = 0.6854f;

        // Integral of a smooth slope transition. Outside the blend interval this
        // matches max(0, y-center), with no abrupt change at ankle/knee/hip seams.
        static float SmoothRamp(float y, float center, float halfWidth)
        {
            float t = (y - center + halfWidth) / (2 * halfWidth);
            if (t <= 0) return 0;
            if (t >= 1) return y - center;
            return 2 * halfWidth * (t * t * t - 0.5f * t * t * t * t);
        }

        static Vector3 WarpBody(Vector3 point)
        {
            float head = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.59f, 0.75f, point.y));
            float bodyX = Mathf.Sign(point.x) * (Mathf.Min(Mathf.Abs(point.x), 0.10f) +
                Mathf.Max(0, Mathf.Abs(point.x) - 0.10f) * 0.60f);
            float x = Mathf.Lerp(bodyX, point.x * 1.10f, head);
            float y = point.y * 0.72f + (0.33f - 0.72f) * SmoothRamp(point.y, 0.10f, 0.055f) +
                (0.70f - 0.33f) * SmoothRamp(point.y, 0.42f, 0.08f) +
                (1.00f - 0.70f) * SmoothRamp(point.y, Neck, 0.03f);
            return new Vector3(x, y, point.z * Mathf.Lerp(1, 1.08f, head));
        }

        struct SkinRegion
        {
            public Vector4 arms;
            public Vector4 fingers; // index, middle, ring, little (within hand weights)
            public float thumb;
            public Vector2 legs;
            public Vector4 details; // hair, tail, ear fins, dress
            public float apron;
            public static SkinRegion operator +(SkinRegion a, SkinRegion b) =>
                new SkinRegion { arms = a.arms + b.arms, fingers = a.fingers + b.fingers,
                    thumb = a.thumb + b.thumb, legs = a.legs + b.legs,
                    details = a.details + b.details, apron = a.apron + b.apron };
            public static SkinRegion operator *(SkinRegion a, float weight) =>
                new SkinRegion { arms = a.arms * weight, fingers = a.fingers * weight,
                    thumb = a.thumb * weight, legs = a.legs * weight,
                    details = a.details * weight, apron = a.apron * weight };
        }

        sealed class RigWarp
        {
            // Limb regions follow skin weights, not height. In the source T-pose,
            // hands share the head transition's height and must not be sheared by it.
            const float ArmScale = 0.64f;
            readonly Vector3 leftShoulder, rightShoulder, leftWrist, rightWrist;
            readonly Vector3[] leftLeg, rightLeg;
            readonly HandShape leftHand, rightHand;

            sealed class HandShape
            {
                readonly Matrix4x4 toHand, fromHand;
                readonly Vector3[] origins = new Vector3[5], axes = new Vector3[5];

                public HandShape(Transform root, Transform[] bones, string side)
                {
                    var hand = bones.Single(t => t.name == "J_Bip_" + side + "_Hand");
                    toHand = hand.worldToLocalMatrix * root.localToWorldMatrix;
                    fromHand = toHand.inverse;
                    string[] names = { "Index", "Middle", "Ring", "Little", "Thumb" };
                    for (int i = 0; i < names.Length; i++)
                    {
                        string prefix = "J_Bip_" + side + "_" + names[i];
                        origins[i] = hand.InverseTransformPoint(bones.Single(t => t.name == prefix + "1").position);
                        axes[i] = (hand.InverseTransformPoint(bones.Single(t => t.name == prefix + "3").position) - origins[i]).normalized;
                    }
                }

                public Vector3 Map(Vector3 point, SkinRegion region, float handWeight)
                {
                    Vector3 local = toHand.MultiplyPoint3x4(point);
                    Vector3 shaped = local;
                    for (int i = 0; i < 5; i++)
                    {
                        float weight = (i == 4 ? region.thumb : region.fingers[i]) / handWeight;
                        if (weight <= 0) continue;
                        Vector3 offset = local - origins[i];
                        Vector3 along = axes[i] * Vector3.Dot(offset, axes[i]);
                        // Shorter phalanges and fuller finger pads, around each digit's
                        // own axis. Joint positions receive the same map as the skin.
                        Vector3 radial = offset - along;
                        Vector3 depthAxis = (Vector3.up - axes[i] * Vector3.Dot(Vector3.up, axes[i])).normalized;
                        Vector3 depth = depthAxis * Vector3.Dot(radial, depthAxis);
                        shaped += (along * (i == 4 ? -0.18f : -0.28f) +
                            depth * 0.22f + (radial - depth) * (i == 4 ? 0.12f : 0.06f)) * weight;
                    }
                    float palm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 0.025f, Mathf.Abs(local.x)));
                    // Keep the wrist tucked into the cuff; swell the palm beyond it.
                    shaped = new Vector3(shaped.x * 0.70f,
                        shaped.y * Mathf.Lerp(0.86f, 1.05f, palm),
                        shaped.z * Mathf.Lerp(0.86f, 0.88f, palm));
                    return fromHand.MultiplyPoint3x4(shaped);
                }
            }

            public RigWarp(Transform root)
            {
                var bones = root.GetComponentsInChildren<Transform>(true);
                leftShoulder = root.InverseTransformPoint(bones.Single(t => t.name == "J_Bip_L_UpperArm").position);
                rightShoulder = root.InverseTransformPoint(bones.Single(t => t.name == "J_Bip_R_UpperArm").position);
                leftWrist = root.InverseTransformPoint(bones.Single(t => t.name == "J_Bip_L_Hand").position);
                rightWrist = root.InverseTransformPoint(bones.Single(t => t.name == "J_Bip_R_Hand").position);
                leftHand = new HandShape(root, bones, "L");
                rightHand = new HandShape(root, bones, "R");
                leftLeg = new[] { "UpperLeg", "LowerLeg", "Foot" }.Select(n =>
                    root.InverseTransformPoint(bones.Single(t => t.name == "J_Bip_L_" + n).position)).ToArray();
                rightLeg = new[] { "UpperLeg", "LowerLeg", "Foot" }.Select(n =>
                    root.InverseTransformPoint(bones.Single(t => t.name == "J_Bip_R_" + n).position)).ToArray();
            }

            public static SkinRegion Region(Transform bone)
            {
                bool hand = false;
                Vector4 fingers = Vector4.zero;
                float thumb = 0;
                for (var current = bone; current; current = current.parent)
                {
                    if (current.name.Contains("_Index")) fingers.x = 1;
                    if (current.name.Contains("_Middle")) fingers.y = 1;
                    if (current.name.Contains("_Ring")) fingers.z = 1;
                    if (current.name.Contains("_Little")) fingers.w = 1;
                    if (current.name.Contains("_Thumb")) thumb = 1;
                    if (current.name.StartsWith("J_Sec_Hair")) return new SkinRegion { details = new Vector4(1, 0, 0, 0) };
                    if (current.name.StartsWith("J_Opt_C_FoxTail")) return new SkinRegion { details = new Vector4(0, 1, 0, 0) };
                    if (current.name.Contains("CatEar")) return new SkinRegion { details = new Vector4(0, 0, 1, 0) };
                    if (current.name.Contains("_Skirt")) return new SkinRegion { details = new Vector4(0, 0, 0, 1) };
                    if (current.name == "J_Bip_L_Hand" || current.name == "J_Bip_R_Hand") hand = true;
                    if (current.name == "J_Bip_L_UpperArm") return new SkinRegion {
                        arms = new Vector4(1, 0, hand ? 1 : 0, 0), fingers = fingers, thumb = thumb };
                    if (current.name == "J_Bip_R_UpperArm") return new SkinRegion {
                        arms = new Vector4(0, 1, 0, hand ? 1 : 0), fingers = fingers, thumb = thumb };
                    if (current.name == "J_Bip_L_UpperLeg") return new SkinRegion { legs = Vector2.right };
                    if (current.name == "J_Bip_R_UpperLeg") return new SkinRegion { legs = Vector2.up };
                }
                return new SkinRegion();
            }

            static Vector3 HairPoint(Vector3 point)
            {
                Vector3 result = WarpBody(point);
                float upper = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.50f, 0.85f, point.y));
                result.x = point.x * Mathf.Lerp(0.95f, 1.10f, upper);
                result.y -= 0.095f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.26f, 0.74f, point.y)));
                return result;
            }

            static Vector3 TailPoint(Vector3 point)
            {
                Vector3 result = WarpBody(point);
                float tip = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.08f, -0.52f, point.z));
                result.y -= tip * 0.115f;
                result.x *= 1.04f;
                result.z *= 0.85f;
                return result;
            }

            static Vector3 EarPoint(Vector3 point)
            {
                point.x = Mathf.Sign(point.x) * (0.12f + Mathf.Max(0, Mathf.Abs(point.x) - 0.12f) * 0.75f);
                point.y = 0.83f + (point.y - 0.83f) * 0.85f;
                return WarpBody(point);
            }

            static Vector3 DressPoint(Vector3 point)
            {
                Vector3 result = WarpBody(point);
                float lower = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.60f, 0.73f, point.y));
                result.y = Mathf.Lerp(result.y, 0.040f + (point.y - 0.1834f) * 0.63f, lower);
                result.x *= Mathf.Lerp(0.90f, 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.40f, 0.68f, point.y)));
                return result;
            }

            static Vector3 ApronPoint(Vector3 point)
            {
                Vector3 result = WarpBody(point);
                result.x *= 0.62f;
                result.y = 0.185f + (point.y - 0.40f) * 0.65f;
                result.z += 0.012f;
                return result;
            }

            static Vector3 ArmPoint(Vector3 point, Vector3 shoulder, Vector3 wrist)
            {
                Vector3 axis = (wrist - shoulder).normalized;
                float along = Vector3.Dot(point - shoulder, axis);
                Vector3 radial = point - shoulder - axis * along;
                float taper = Mathf.SmoothStep(0, 1, Mathf.Clamp01(along / Vector3.Distance(shoulder, wrist)));
                // Shorten the limb while keeping a full, rounded sleeve cross-section.
                float radius = Mathf.Lerp(1.05f, 0.86f, taper);
                return WarpBody(shoulder) + axis * (along * ArmScale) + radial * radius;
            }

            static Vector3 HandPoint(Vector3 point, Vector3 shoulder, Vector3 wrist, HandShape shape,
                SkinRegion region, float weight) =>
                WarpBody(shoulder) + (wrist - shoulder) * ArmScale + shape.Map(point, region, weight) - wrist;

            static Vector3 LegPoint(Vector3 point, Vector3[] leg)
            {
                Vector3 upper = point.y >= leg[1].y ? leg[0] : leg[1];
                Vector3 lower = point.y >= leg[1].y ? leg[1] : leg[2];
                float t = Mathf.InverseLerp(lower.y, upper.y, point.y);
                Vector3 center = Vector3.Lerp(lower, upper, t);
                float radius = point.y < leg[2].y ? Mathf.Lerp(1.04f, 1.28f,
                    Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.02f, leg[2].y, point.y))) :
                    point.y < leg[1].y ? Mathf.Lerp(1.28f, 1.08f,
                    Mathf.SmoothStep(0, 1, Mathf.InverseLerp(leg[2].y, leg[1].y, point.y))) :
                    Mathf.Lerp(1.08f, 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(leg[1].y, leg[0].y, point.y)));
                return WarpBody(point) + new Vector3(point.x - center.x, 0, point.z - center.z) * (radius - 1);
            }

            public Vector3 Map(Vector3 point, SkinRegion region)
            {
                Vector4 arm = region.arms;
                float body = Mathf.Max(0, 1 - arm.x - arm.y - region.legs.x - region.legs.y -
                    region.details.x - region.details.y - region.details.z - region.details.w - region.apron);
                Vector3 result = body > 0 ? WarpBody(point) * body : Vector3.zero;
                result +=
                    ArmPoint(point, leftShoulder, leftWrist) * (arm.x - arm.z) +
                    ArmPoint(point, rightShoulder, rightWrist) * (arm.y - arm.w) +
                    LegPoint(point, leftLeg) * region.legs.x + LegPoint(point, rightLeg) * region.legs.y;
                if (arm.z > 0) result += HandPoint(point, leftShoulder, leftWrist, leftHand, region, arm.z) * arm.z;
                if (arm.w > 0) result += HandPoint(point, rightShoulder, rightWrist, rightHand, region, arm.w) * arm.w;
                if (region.details.x > 0) result += HairPoint(point) * region.details.x;
                if (region.details.y > 0) result += TailPoint(point) * region.details.y;
                if (region.details.z > 0) result += EarPoint(point) * region.details.z;
                if (region.details.w > 0) result += DressPoint(point) * region.details.w;
                if (region.apron > 0) result += ApronPoint(point) * region.apron;
                return result;
            }

            public Matrix4x4 Jacobian(Vector3 point, SkinRegion region)
            {
                const float e = 0.0001f;
                var matrix = Matrix4x4.identity;
                matrix.SetColumn(0, (Map(point + Vector3.right * e, region) - Map(point - Vector3.right * e, region)) / (2 * e));
                matrix.SetColumn(1, (Map(point + Vector3.up * e, region) - Map(point - Vector3.up * e, region)) / (2 * e));
                matrix.SetColumn(2, (Map(point + Vector3.forward * e, region) - Map(point - Vector3.forward * e, region)) / (2 * e));
                return matrix;
            }
        }

        public static void Apply(GameObject model)
        {
            System.IO.Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var root = model.transform;
            var warp = new RigWarp(root);
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var meshes = new Mesh[renderers.Length];
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                var source = renderer.sharedMesh;
                if (!source) continue;
                var mesh = UnityEngine.Object.Instantiate(source);
                mesh.name = source.name + " Chibi";
                var toRoot = root.worldToLocalMatrix * renderer.localToWorldMatrix;
                var toLocal = toRoot.inverse;
                Vector3[] vertices = source.vertices, normals = source.normals;
                Vector2[] uvs = source.uv;
                var boneRegions = renderer.bones.Select(RigWarp.Region).ToArray();
                var regions = source.boneWeights.Select(b => boneRegions[b.boneIndex0] * b.weight0 +
                    boneRegions[b.boneIndex1] * b.weight1 + boneRegions[b.boneIndex2] * b.weight2 +
                    boneRegions[b.boneIndex3] * b.weight3).ToArray();
                // The same torso bone carries shirt, skirt and apron. Classify the
                // disconnected garment panels by their source material and UV island.
                for (int s = 0; s < source.subMeshCount; s++)
                {
                    string material = renderer.sharedMaterials[s].name;
                    if (!material.StartsWith("N00_002_03_Tops")) continue;
                    foreach (int i in source.GetTriangles(s).Distinct())
                    {
                        var region = regions[i];
                        float available = Mathf.Clamp01(1 - region.arms.x - region.arms.y - region.legs.x - region.legs.y -
                            region.details.x - region.details.y - region.details.z);
                        if (material.EndsWith("CLOTH_01") && uvs[i].y < 0.50f && vertices[i].y < 0.56f)
                        {
                            region.details.w = 0;
                            region.apron = available;
                        }
                        else region.details.w = available;
                        regions[i] = region;
                    }
                }
                var deformed = new Vector3[vertices.Length];
                var normalMaps = new Matrix4x4[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 point = toRoot.MultiplyPoint3x4(vertices[i]);
                    deformed[i] = toLocal.MultiplyPoint3x4(warp.Map(point, regions[i]));
                    normalMaps[i] = toRoot.transpose * warp.Jacobian(point, regions[i]).inverse.transpose * toRoot.inverse.transpose;
                }
                mesh.vertices = deformed;
                if (normals.Length == vertices.Length)
                    mesh.normals = normals.Select((normal, i) => normalMaps[i].MultiplyVector(normal).normalized).ToArray();
                mesh.ClearBlendShapes();
                var dv = new Vector3[vertices.Length];
                var dn = new Vector3[vertices.Length];
                var dt = new Vector3[vertices.Length];
                for (int shape = 0; shape < source.blendShapeCount; shape++)
                    for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
                    {
                        source.GetBlendShapeFrameVertices(shape, frame, dv, dn, dt);
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            dv[i] = toLocal.MultiplyPoint3x4(warp.Map(toRoot.MultiplyPoint3x4(vertices[i] + dv[i]), regions[i])) - deformed[i];
                            if (normals.Length == vertices.Length)
                                dn[i] = normalMaps[i].MultiplyVector(normals[i] + dn[i]).normalized -
                                    normalMaps[i].MultiplyVector(normals[i]).normalized;
                            dt[i] = Vector3.zero;
                        }
                        mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),
                            source.GetBlendShapeFrameWeight(shape, frame), dv, dn, dt);
                    }
                mesh.RecalculateBounds();
                meshes[r] = mesh;
            }
            // Snapshot before changing parents; set positions in parent-first order.
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var points = transforms.Select(t => root.InverseTransformPoint(t.position)).ToArray();
            var transformRegions = transforms.Select(RigWarp.Region).ToArray();
            for (int i = 1; i < transforms.Length; i++)
                transforms[i].position = root.TransformPoint(warp.Map(points[i], transformRegions[i]));
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                var mesh = meshes[r];
                if (!mesh) continue;
                mesh.bindposes = renderer.bones.Select(b => b.worldToLocalMatrix * renderer.localToWorldMatrix).ToArray();
                if (FishMeshOptimizer.Enabled)
                {
                    var optimized = FishMeshOptimizer.Reduce(mesh, renderer.sharedMaterials, out Material[] keptMaterials);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    mesh = optimized;
                    renderer.sharedMaterials = keptMaterials;
                }
                string path = Folder + "/Chibi-" + r + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing)
                {
                    EditorUtility.CopySerialized(mesh, existing);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    mesh = existing;
                    EditorUtility.SetDirty(mesh);
                }
                else AssetDatabase.CreateAsset(mesh, path);
                renderer.sharedMesh = mesh;
                renderer.localBounds = mesh.bounds;
                var materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    var original = materials[m];
                    if (!original.name.StartsWith("N00_002_03_Tops") || !original.name.EndsWith("CLOTH_01")) continue;
                    var illustrated = new Material(original) {
                        shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DesktopFish/Shaders/ApronAtlas.shader"),
                        name = "Illustrated Apron", shaderKeywords = new string[0]
                    };
                    if (!illustrated.shader) throw new InvalidOperationException("Missing apron shader.");
                    string materialPath = Folder + "/IllustratedApron.mat";
                    var saved = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (saved)
                    {
                        EditorUtility.CopySerialized(illustrated, saved);
                        UnityEngine.Object.DestroyImmediate(illustrated);
                        EditorUtility.SetDirty(saved);
                    }
                    else { AssetDatabase.CreateAsset(illustrated, materialPath); saved = illustrated; }
                    materials[m] = saved;
                }
                for (int m = 0; m < materials.Length; m++) materials[m] = FishVisualStyle.Apply(materials[m]);
                renderer.sharedMaterials = materials;
            }
            Debug.Log("Desktop Fish chibi: separate garment/apron/hair/tail/fin regions; head 110% width / 100% height; source meshes preserved.");
        }

    }
}
