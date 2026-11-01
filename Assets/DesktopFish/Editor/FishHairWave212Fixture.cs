using System;
using System.Linq;
using UnityEngine;

namespace DesktopFish.Editor {
    // Continuous root-anchored travelling shear on the low-poly hair. The y coordinate is
    // unchanged, so the spatial map is injective (Jacobian determinant = 1).
    // Transport normals by its inverse transpose, rather than faking softness.
    public sealed class LegacyHairWave212 : IDisposable {
        sealed class Part {
            public SkinnedMeshRenderer renderer;
            public Mesh original,mesh;
            public Vector3[] positions,normals,animatedPositions,animatedNormals;
            public bool[] hair;
        }
        readonly Part[] parts;
        public LegacyHairWave212(Transform model) {
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(!skins.Any(s=>s.sharedMesh.name=="RearHairCards")){parts=new Part[0];return;}
            parts=skins.Where(s=>s.sharedMesh.name=="Body"||s.sharedMesh.name=="RearHairCards").Select(s=>{
                var source=s.sharedMesh;bool card=source.name=="RearHairCards";var weights=source.boneWeights;
                var part=new Part{renderer=s,original=source,mesh=UnityEngine.Object.Instantiate(source),positions=source.vertices,normals=source.normals};
                part.mesh.name=source.name;part.mesh.MarkDynamic();part.animatedPositions=(Vector3[])part.positions.Clone();part.animatedNormals=(Vector3[])part.normals.Clone();
                part.hair=weights.Select(w=>card||new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}.Any(i=>i>=5&&i<=9)).ToArray();
                s.sharedMesh=part.mesh;return part;
            }).ToArray();
        }
        public void Tick(float time,float weight) {
            if(Mathf.Abs(weight)<.0001f)weight=0;
            float crankPhase=FishWorkStation.CrankAngle(time);
            foreach(var part in parts) {
                for(int i=0;i<part.positions.Length;i++) {
                    Vector3 p=part.positions[i],n=part.normals[i];
                    if(weight>0&&part.hair[i]&&p.y<.80f) {
                        float length=.80f-p.y,phase=crankPhase-.45f-1.8f*length;
                        float x=(.10f*Mathf.Sin(phase)+.016f*Mathf.Sin(2*phase-.3f))*weight;
                        float z=.045f*Mathf.Sin(phase-.55f)*weight;
                        float dxdy=-x+length*1.8f*(.10f*Mathf.Cos(phase)+.032f*Mathf.Cos(2*phase-.3f))*weight;
                        float dzdy=-z+length*1.8f*.045f*Mathf.Cos(phase-.55f)*weight;
                        p.x+=x*length;p.z+=z*length;
                        n.y-=dxdy*n.x+dzdy*n.z;n.Normalize();
                    }
                    part.animatedPositions[i]=p;part.animatedNormals[i]=n;
                }
                part.mesh.vertices=part.animatedPositions;part.mesh.normals=part.animatedNormals;part.mesh.RecalculateBounds();
            }
        }
        public void Dispose() {
            foreach(var part in parts) {
                if(part.renderer&&part.renderer.sharedMesh==part.mesh)part.renderer.sharedMesh=part.original;
                if(Application.isPlaying)UnityEngine.Object.Destroy(part.mesh);else UnityEngine.Object.DestroyImmediate(part.mesh);
            }
        }
    }
}
