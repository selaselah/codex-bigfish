using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace DesktopFish.Editor {
    // Own-source rounded shoes and articulated hands replace their collapsed LOD
    // counterparts. The frozen base recipe and the scalp/card stages run first.
    public static class FishHandsShoes5K {
        [Serializable] sealed class Skin { public int[] indices; public float[] weights; }
        [Serializable] sealed class Shape { public string name; public Vector3[] deltas,normal_deltas; }
        [Serializable] sealed class Part {
            public string bodyBeforePositionIndexSHA256;
            public Vector3[] vertices,normals;
            public Vector2[] uv;
            public Color[] colors;
            public int[] triangles;
            public Skin[] skin4;
            public Shape[] shapes;
        }
        public static void Apply(Mesh mesh) {
            var data=JsonUtility.FromJson<Part>(File.ReadAllText("Assets/DesktopFish/Editor/FishHandsShoes5K.json"));
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)) {
                foreach(var p in mesh.vertices){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
                foreach(int i in mesh.triangles)w.Write(i);
                w.Flush();using(var sha=SHA256.Create())if(string.Concat(sha.ComputeHash(stream.ToArray()).Select(b=>b.ToString("x2")))!=data.bodyBeforePositionIndexSHA256)
                    throw new InvalidOperationException("Hand/shoe redistribution no longer matches the covered scalp and rear cards.");
            }
            int count=data.vertices.Length;
            if(data.normals.Length!=count||data.uv.Length!=count||data.colors.Length!=count||data.skin4.Length!=count||data.shapes.Length!=6||data.triangles.Length/3!=4387)
                throw new InvalidOperationException("Hand/shoe streams or triangle budget changed.");
            var weights=data.skin4.Select(s=>{
                if(s.indices.Length!=4||s.weights.Length!=4||Mathf.Abs(s.weights.Sum()-1)>1e-6f||s.weights.Any(v=>v<0||v>1)||s.indices.Any(i=>i<0||i>=58))throw new InvalidOperationException("Hand/shoe skin support invalid.");
                return new BoneWeight{boneIndex0=s.indices[0],boneIndex1=s.indices[1],boneIndex2=s.indices[2],boneIndex3=s.indices[3],weight0=s.weights[0],weight1=s.weights[1],weight2=s.weights[2],weight3=s.weights[3]};
            }).ToArray();
            if(data.normals.Any(n=>float.IsNaN(n.x)||Mathf.Abs(n.magnitude-1)>.001f)||data.triangles.Any(i=>i<0||i>=count))throw new InvalidOperationException("Hand/shoe normals or indices invalid.");
            var binds=mesh.bindposes;mesh.Clear(false);mesh.ClearBlendShapes();mesh.vertices=data.vertices;mesh.normals=data.normals;mesh.uv=data.uv;mesh.colors=data.colors;mesh.boneWeights=weights;mesh.bindposes=binds;mesh.triangles=data.triangles;
            foreach(var shape in data.shapes){if(shape.deltas.Length!=count||shape.normal_deltas.Length!=count)throw new InvalidOperationException("Hand/shoe expression support lost.");mesh.AddBlendShapeFrame(shape.name,100,shape.deltas,shape.normal_deltas,new Vector3[count]);}
            mesh.RecalculateBounds();
        }
    }
}
