using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesktopFish.Editor
{
    // Offline reduction only. The source model and protected facial/hand topology
    // stay intact; the player loads the generated mesh without a runtime reducer.
    public static class FishMeshOptimizer
    {
        public static bool Enabled = true;
        [Serializable] public sealed class Part
        {
            public string material;
            public int originalTriangles, optimizedTriangles;
            public string treatment;
            public float maximumError;
        }
        [Serializable] public sealed class Report
        {
            public int originalVertices, optimizedVertices, originalTriangles, optimizedTriangles;
            public int originalMaterialSlots, optimizedMaterialSlots, blendshapes;
            public int protectedTriangles;
            public int originalIndexBytes, optimizedIndexBytes;
            public Part[] parts;
        }
        public static Report LastReport;

        // Rebuilt meshes already have sparse authored surface grids. This final
        // index-only pass keeps all retained UV/color/normal/skin data exact.
        public static Mesh ReduceRebuilt(Mesh source,int targetTriangles,int headBone) {
            Directory.CreateDirectory("work");
            var p=source.vertices;var n=source.normals;var uv=source.uv;var w=source.boneWeights;
            var indices=source.triangles;if(indices.Length/3<=targetTriangles)return source;
            var locked=new byte[p.Length];
            // The absolute body tolerance is larger than tiny button thickness.
            // Keep small connected ornaments, authored with sparse grids, intact
            // so a closed bead cannot collapse into opposing coincident sheets.
            var parent=Enumerable.Range(0,p.Length).ToArray();
            Func<int,int> component=i=>{while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;};
            for(int i=0;i<indices.Length;i+=3) {
                parent[component(indices[i+1])]=component(indices[i]);
                parent[component(indices[i+2])]=component(indices[i]);
            }
            var bounds=new Dictionary<int,Bounds>();
            foreach(int i in indices.Distinct()) {
                int root=component(i);Bounds b;if(!bounds.TryGetValue(root,out b))b=new Bounds(p[i],Vector3.zero);
                b.Encapsulate(p[i]);bounds[root]=b;
            }
            var colors=source.colors;var contrast=new Dictionary<int,Bounds>();
            foreach(int i in indices.Distinct()) {
                int root=component(i);var c=colors[i];Vector3 rgb=new Vector3(c.r,c.g,c.b);Bounds b;
                if(!contrast.TryGetValue(root,out b))b=new Bounds(rgb,Vector3.zero);b.Encapsulate(rgb);contrast[root]=b;
            }
            foreach(int i in indices.Distinct()) {
                int root=component(i);float extent=bounds[root].size.sqrMagnitude;
                // Preserve small multicolor ornaments as well as tiny beads:
                // geometry-only error cannot represent a gold-to-blue rim.
                if(extent<.025f*.025f||(extent<.04f*.04f&&contrast[root].size.sqrMagnitude>.3f*.3f))locked[i]=1;
            }
            // Front strips carry deliberately sparse measured cross-sections.
            // Keep their thin layered surfaces; body-scale collapse can alter
            // their carefully allocated silhouette and overlap.
            for(int i=0;i<p.Length;i++)if(w[i].boneIndex0==headBone&&w[i].weight0>.999f&&uv[i].x<.75f&&uv[i].y>.12f*FishRebuiltModel.LegacyPaintHeight/FishRebuiltModel.PaintHeight&&
                uv[i].y<(float)FishRebuiltModel.LegacyPaintHeight/FishRebuiltModel.PaintHeight)locked[i]=1;
            for(int i=0;i<p.Length;i++)if(w[i].weight1>.00001f&&w[i].weight1<.99999f)locked[i]=1;
            for(int i=0;i<indices.Length;i+=3) {
                int a=indices[i],b=indices[i+1],c=indices[i+2];
                if(w[a].boneIndex0!=w[b].boneIndex0||w[b].boneIndex0!=w[c].boneIndex0)
                    locked[a]=locked[b]=locked[c]=1;
            }
            string input=Path.GetFullPath("work/rebuilt-reduce-input.bin"),output=Path.GetFullPath("work/rebuilt-reduce-output.bin");
            using(var writer=new BinaryWriter(File.Create(input))) {
                writer.Write(0x46495348);writer.Write(p.Length);writer.Write(1);
                for(int i=0;i<p.Length;i++) {
                    writer.Write(p[i].x);writer.Write(p[i].y);writer.Write(p[i].z);
                    writer.Write(n[i].x);writer.Write(n[i].y);writer.Write(n[i].z);
                    writer.Write(uv[i].x);writer.Write(uv[i].y);
                }
                writer.Write(locked);writer.Write(1);writer.Write(indices.Length);foreach(int i in indices)writer.Write(i);
            }
            string binary=Path.GetFullPath("Tools/bin/meshopt-reduce");
            string[] sources={"Tools/meshopt-reduce.cpp","Tools/meshoptimizer/simplifier.cpp","Tools/meshoptimizer/allocator.cpp","Tools/meshoptimizer/meshoptimizer.h"};
            if(!File.Exists(binary)||sources.Any(f=>File.GetLastWriteTimeUtc(f)>File.GetLastWriteTimeUtc(binary)))
                RunProcess("/bin/zsh","\""+Path.GetFullPath("Tools/BuildOptimizer.command")+"\"");
            float ratio=targetTriangles/(indices.Length/3f);
            RunProcess(binary,"\""+input+"\" \""+output+"\" "+ratio.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" 0.0030");
            int[] result;float error;
            using(var reader=new BinaryReader(File.OpenRead(output))) {
                if(reader.ReadInt32()!=0x46495348||reader.ReadInt32()!=1)throw new InvalidOperationException("Invalid rebuilt reducer output");
                error=reader.ReadSingle();int count=reader.ReadInt32();
                if(count<0||count%3!=0||count>indices.Length)throw new InvalidOperationException("Invalid rebuilt index count");
                result=Enumerable.Range(0,count).Select(i=>reader.ReadInt32()).ToArray();
                if(result.Any(i=>i<0||i>=p.Length))throw new InvalidOperationException("Invalid rebuilt vertex index");
            }
            var mesh=Extract(source,new List<int>{0},new[]{result});mesh.name=source.name;
            Debug.Log($"Rebuilt reduction: {indices.Length/3} -> {result.Length/3}; error={error}; locked={locked.Count(x=>x!=0)}");
            File.WriteAllText("work/rebuilt-reduction.json",JsonUtility.ToJson(new Part {material="Body",originalTriangles=indices.Length/3,optimizedTriangles=result.Length/3,maximumError=error,treatment="index-only; locked seams, mixed weights and bone boundaries"},true));
            UnityEngine.Object.DestroyImmediate(source);return mesh;
        }

        static bool Protected(string name) => name.Contains("Face") || name.Contains("Eye") ||
            name.Contains("Body_00_SKIN") || new[] { "HAIR_10", "HAIR_11", "HAIR_12" }.Any(name.EndsWith);

        public static Mesh Reduce(Mesh source, Material[] materials, out Material[] outputMaterials)
        {
            var kept = new List<int>();
            var protectedSlots = new List<int>();
            var reducibleSlots = new List<int>();
            var rows = new List<Part>();
            for (int s = 0; s < source.subMeshCount; s++)
            {
                var material = materials[s];
                int triangles = source.GetTriangles(s).Length / 3;
                bool hidden = material.HasProperty("_Color") && material.GetColor("_Color").a <= 0.001f;
                bool preserve = Protected(material.name) || triangles < 800;
                rows.Add(new Part { material = material.name, originalTriangles = triangles,
                    treatment = hidden ? "omit fully transparent generated geometry" : preserve ? "preserve exact topology" : "attribute-aware index reduction; lock borders; retain original vertex data" });
                if (hidden || triangles == 0) continue;
                kept.Add(s);
                (preserve ? protectedSlots : reducibleSlots).Add(s);
            }
            var input = Path.GetFullPath("work/meshopt-input.bin");
            var output = Path.GetFullPath("work/meshopt-output.bin");
            Directory.CreateDirectory("work");
            var vertices = source.vertices; var normals = source.normals; var uv = source.uv;
            var locked = new byte[source.vertexCount];
            foreach (int slot in protectedSlots)
                foreach (int i in source.GetTriangles(slot)) locked[i] = 1;
            using (var writer = new BinaryWriter(File.Create(input)))
            {
                writer.Write(0x46495348); writer.Write(source.vertexCount); writer.Write(kept.Count);
                for (int i = 0; i < source.vertexCount; i++)
                {
                    writer.Write(vertices[i].x); writer.Write(vertices[i].y); writer.Write(vertices[i].z);
                    writer.Write(normals[i].x); writer.Write(normals[i].y); writer.Write(normals[i].z);
                    writer.Write(uv[i].x); writer.Write(uv[i].y);
                }
                writer.Write(locked);
                foreach (int slot in kept)
                {
                    int[] triangles = source.GetTriangles(slot);
                    writer.Write(reducibleSlots.Contains(slot) ? 1 : 0); writer.Write(triangles.Length);
                    foreach (int i in triangles) writer.Write(i);
                }
            }
            string binary = Path.GetFullPath("Tools/bin/meshopt-reduce");
            string[] toolSources = { "Tools/meshopt-reduce.cpp", "Tools/meshoptimizer/simplifier.cpp", "Tools/meshoptimizer/allocator.cpp", "Tools/meshoptimizer/meshoptimizer.h" };
            if (!File.Exists(binary) || toolSources.Any(path => File.GetLastWriteTimeUtc(path) > File.GetLastWriteTimeUtc(binary)))
                RunProcess("/bin/zsh", "\"" + Path.GetFullPath("Tools/BuildOptimizer.command") + "\"");
            RunProcess(binary, "\"" + input + "\" \"" + output + "\"");
            var optimizedIndices = new int[kept.Count][];
            using (var reader = new BinaryReader(File.OpenRead(output)))
            {
                if (reader.ReadInt32() != 0x46495348 || reader.ReadInt32() != kept.Count)
                    throw new InvalidOperationException("Invalid optimizer output header.");
                for (int slot = 0; slot < kept.Count; slot++)
                {
                    rows[kept[slot]].maximumError = reader.ReadSingle();
                    int count = reader.ReadInt32();
                    if (count < 0 || count % 3 != 0 || count > source.GetTriangles(kept[slot]).Length)
                        throw new InvalidOperationException("Invalid optimizer output count.");
                    optimizedIndices[slot] = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        int index = reader.ReadInt32();
                        if (index < 0 || index >= source.vertexCount) throw new InvalidOperationException("Invalid optimizer index.");
                        optimizedIndices[slot][i] = index;
                    }
                    if (protectedSlots.Contains(kept[slot]) && !source.GetTriangles(kept[slot]).SequenceEqual(optimizedIndices[slot]))
                        throw new InvalidOperationException("Optimizer changed protected topology.");
                }
            }
            var result = Extract(source, kept, optimizedIndices);
            outputMaterials = kept.Select(slot => materials[slot]).ToArray();
            for (int slot = 0; slot < kept.Count; slot++) rows[kept[slot]].optimizedTriangles = optimizedIndices[slot].Length / 3;
            LastReport = new Report {
                originalVertices = source.vertexCount, optimizedVertices = result.vertexCount,
                originalTriangles = source.triangles.Length / 3, optimizedTriangles = result.triangles.Length / 3,
                originalMaterialSlots = source.subMeshCount, optimizedMaterialSlots = result.subMeshCount,
                blendshapes = result.blendShapeCount, protectedTriangles = protectedSlots.Sum(slot => source.GetTriangles(slot).Length / 3),
                originalIndexBytes = source.triangles.Length * (source.indexFormat == IndexFormat.UInt16 ? 2 : 4),
                optimizedIndexBytes = result.triangles.Length * (result.indexFormat == IndexFormat.UInt16 ? 2 : 4),
                parts = rows.ToArray()
            };
            Directory.CreateDirectory("work");
            File.WriteAllText("work/mesh-optimization.json", JsonUtility.ToJson(LastReport, true));
            Debug.Log("Desktop Fish mesh reduced: " + LastReport.originalTriangles + " -> " + LastReport.optimizedTriangles +
                " triangles; " + LastReport.originalVertices + " -> " + LastReport.optimizedVertices + " vertices; materials " +
                LastReport.originalMaterialSlots + " -> " + LastReport.optimizedMaterialSlots);
            return result;
        }

        static Mesh Extract(Mesh source, List<int> slots, int[][] indices)
        {
            int[] used = indices.SelectMany(x => x).Distinct().OrderBy(i => i).ToArray();
            var remap = Enumerable.Repeat(-1, source.vertexCount).ToArray();
            for (int i = 0; i < used.Length; i++) remap[used[i]] = i;
            var result = new Mesh { name = source.name + " Desktop", indexFormat =
                used.Length <= ushort.MaxValue ? IndexFormat.UInt16 : IndexFormat.UInt32 };
            result.vertices = Pick(source.vertices, used);
            result.normals = Pick(source.normals, used);
            result.tangents = Pick(source.tangents, used);
            result.colors = Pick(source.colors, used);
            result.boneWeights = Pick(source.boneWeights, used);
            result.bindposes = source.bindposes;
            for (int channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>(); source.GetUVs(channel, uv);
                if (uv.Count > 0) result.SetUVs(channel, used.Select(i => uv[i]).ToList());
            }
            result.subMeshCount = slots.Count;
            for (int s = 0; s < indices.Length; s++) result.SetTriangles(indices[s].Select(i => remap[i]).ToArray(), s);
            var dv = new Vector3[source.vertexCount]; var dn = new Vector3[source.vertexCount]; var dt = new Vector3[source.vertexCount];
            for (int shape = 0; shape < source.blendShapeCount; shape++)
                for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
                {
                    source.GetBlendShapeFrameVertices(shape, frame, dv, dn, dt);
                    result.AddBlendShapeFrame(source.GetBlendShapeName(shape), source.GetBlendShapeFrameWeight(shape, frame),
                        Pick(dv, used), Pick(dn, used), Pick(dt, used));
                }
            result.RecalculateBounds();
            return result;
        }

        static T[] Pick<T>(T[] source, int[] used) => source.Length == 0 ? new T[0] : used.Select(i => source[i]).ToArray();
        static void RunProcess(string program, string arguments)
        {
            using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = program, Arguments = arguments, UseShellExecute = false, RedirectStandardError = true,
                CreateNoWindow = true, WorkingDirectory = Directory.GetCurrentDirectory()
            }))
            {
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("Mesh optimizer failed: " + error);
            }
        }
    }
}
