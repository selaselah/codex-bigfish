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
        static Material[] guideMaterials;
        static readonly System.Collections.Generic.Dictionary<int,Texture2D> guideTextures=new System.Collections.Generic.Dictionary<int,Texture2D>();
        static Texture2D GuideTexture(int slot) {
            if(guideTextures.TryGetValue(slot,out var cached))return cached;
            var texture=guideMaterials[slot].mainTexture;if(!texture)return null;
            var rt=RenderTexture.GetTemporary(512,512,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Graphics.Blit(texture,rt);RenderTexture.active=rt;
            var readable=new Texture2D(512,512,TextureFormat.RGBA32,false);readable.ReadPixels(new Rect(0,0,512,512),0,0);readable.Apply();
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);guideTextures[slot]=readable;return readable;
        }
        static System.Collections.Generic.List<Vector3> VisibleSection(Vector3[] points,Vector2[] uv,int[] triangles,
            Vector3 normal,float plane,int slot,Func<Vector3,Vector2,bool> keep=null) {
            var result=new System.Collections.Generic.List<Vector3>();var texture=GuideTexture(slot);
            for(int i=0;i<triangles.Length;i+=3) {
                var cuts=new System.Collections.Generic.List<Vector3>();var tex=new System.Collections.Generic.List<Vector2>();
                for(int j=0;j<3;j++) {
                    int ai=triangles[i+j],bi=triangles[i+(j+1)%3];Vector3 a=points[ai],b=points[bi];
                    float da=Vector3.Dot(a,normal)-plane,db=Vector3.Dot(b,normal)-plane;
                    if(!((da<=0&&db>0)||(db<=0&&da>0)))continue;
                    float t=da/(da-db);cuts.Add(Vector3.Lerp(a,b,t));tex.Add(Vector2.Lerp(uv[ai],uv[bi],t));
                }
                if(cuts.Count!=2)continue;
                for(int k=0;k<=24;k++) {
                    float t=k/24f;Vector3 point=Vector3.Lerp(cuts[0],cuts[1],t);Vector2 st=Vector2.Lerp(tex[0],tex[1],t);
                    if(texture&&texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                    if(keep==null||keep(point,st))result.Add(point);
                }
            }
            return result;
        }
        public static void Render()
        {
            foreach(var texture in guideTextures.Values)UnityEngine.Object.DestroyImmediate(texture);
            guideTextures.Clear();
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
            guideMaterials=skin.sharedMaterials;guideTextures.Clear();
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
            foreach (int angle in new[] {0,35,90,180})
            {
                camera.transform.position = bounds.center + Quaternion.Euler(0,angle,0)*new Vector3(0,.24f,3);
                camera.transform.LookAt(bounds.center); camera.Render(); RenderTexture.active = target;
                frame.ReadPixels(new Rect(0,0,720,960),0,0); frame.Apply();
                File.WriteAllBytes("work/reference-review/ref-"+angle+".png",frame.EncodeToPNG());
            }
            camera.transform.position=bounds.center+new Vector3(0,.64f,3);
            camera.transform.LookAt(bounds.center);camera.Render();RenderTexture.active=target;
            frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
            File.WriteAllBytes("work/reference-review/ref-top.png",frame.EncodeToPNG());
            FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/head",.8f,new Vector3(0,-.056f,0));
            FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/aligned",.8f,new Vector3(0,-.056f,0));
            ReviewSectionParts(baked, snapshot, camera, bounds, frame, target);
            ReviewHairHem(baked,snapshot.transform);
            ReviewFaceSections(baked,snapshot.transform);
            ReviewHeadEnvelope(baked,snapshot.transform);
            ReviewFrontCrown(baked,snapshot.transform);
            ReviewFringeCoverage(baked,snapshot.transform);
            ReviewFeatureGuides(baked,snapshot.transform);
            ReviewFinSections(baked,snapshot.transform);
            ReviewTorsoSections(baked,snapshot.transform);
            var defaultHand=skin.bones.First(t=>t.name=="J_Bip_R_Hand");
            FishRebuiltModel.RenderDefaultHandStudy(camera,"work/reference-review/hand-default",defaultHand.position+new Vector3(0,-.0112f,.012f),.8f);
            ReviewHandBounds(reference,baked,skin,snapshot.transform);
            ReviewCuffSections(baked,skin,snapshot.transform);
            ReviewAccessoryComponents(baked,snapshot.transform,camera,bounds,frame,target);
            File.WriteAllText("work/reference-review/stats.json", JsonUtility.ToJson(new Stats {
                vertices=reference.vertexCount,
                totalTriangles=Enumerable.Range(0,reference.subMeshCount).Sum(s=>(int)reference.GetIndexCount(s)/3),
                materialSlots=reference.subMeshCount,bones=reference.bindposes.Length,expressions=reference.blendShapeCount
            },true));
            RenderTexture.active = null; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(frame); UnityEngine.Object.DestroyImmediate(target);
            foreach(var texture in guideTextures.Values)UnityEngine.Object.DestroyImmediate(texture);
            guideTextures.Clear();
            Debug.Log("Local Ref.asset review rendered; reference left unchanged.");
        }
        static void ReviewCuffSections(Mesh baked,SkinnedMeshRenderer skin,Transform snapshot) {
            var elbow=skin.bones.First(t=>t.name=="J_Bip_R_LowerArm").position;
            var hand=skin.bones.First(t=>t.name=="J_Bip_R_Hand").position;
            Vector3 axis=(hand-elbow).normalized,up=Vector3.ProjectOnPlane(Vector3.up,axis).normalized;
            Vector3 across=Vector3.Cross(axis,up).normalized;
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var triangles=baked.GetTriangles(32);
            var parts=new System.Collections.Generic.List<PartBounds>();float length=Vector3.Distance(hand,elbow);
            var weights=skin.sharedMesh.boneWeights;
            int lowerIndex=Array.FindIndex(skin.bones,t=>t.name=="J_Bip_R_LowerArm");
            Func<int,float> lowerWeight=i=> {
                var w=weights[i];return (w.boneIndex0==lowerIndex?w.weight0:0)+(w.boneIndex1==lowerIndex?w.weight1:0)+
                    (w.boneIndex2==lowerIndex?w.weight2:0)+(w.boneIndex3==lowerIndex?w.weight3:0);
            };
            foreach(float t in new[]{.50f,.65f,.80f,.95f,1.10f}) {
                float along=t*length;var cuts=new System.Collections.Generic.List<Vector3>();
                for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++) {
                    Vector3 a=points[triangles[i+j]],b=points[triangles[i+(j+1)%3]];
                    float qa=Vector3.Dot(a-elbow,axis),qb=Vector3.Dot(b-elbow,axis);
                    if((qa<=along&&qb>along)||(qb<=along&&qa>along)) {
                        float fraction=(along-qa)/(qb-qa);
                        if(Mathf.Lerp(lowerWeight(triangles[i+j]),lowerWeight(triangles[i+(j+1)%3]),fraction)<.5f)continue;
                        Vector3 p=Vector3.Lerp(a,b,fraction)-elbow;
                        cuts.Add(new Vector3(Vector3.Dot(p,across),Vector3.Dot(p,up),along));
                    }
                }
                if(cuts.Count==0)continue;var bounds=new Bounds(cuts[0],Vector3.zero);
                foreach(var p in cuts)bounds.Encapsulate(p);
                parts.Add(new PartBounds{slot=32,name="forearm-t="+t.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),minimum=bounds.min,maximum=bounds.max});
            }
            File.WriteAllText("work/reference-review/cuff-sections.json",JsonUtility.ToJson(new AccessoryReport{parts=parts.ToArray()},true));
        }

        static void ReviewHandBounds(Mesh source,Mesh baked,SkinnedMeshRenderer skin,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var weights=source.boneWeights;
            var parts=new System.Collections.Generic.List<PartBounds>();
            foreach(string name in new[]{"Hand","Thumb","Index","Middle","Ring","Little"}) {
                var selected=Enumerable.Range(0,skin.bones.Length).Where(i=>skin.bones[i].name.StartsWith("J_Bip_R_"+name)).ToArray();
                var ids=source.GetTriangles(18).Distinct().Where(i=> {
                    var w=weights[i];return (selected.Contains(w.boneIndex0)?w.weight0:0)+(selected.Contains(w.boneIndex1)?w.weight1:0)+
                        (selected.Contains(w.boneIndex2)?w.weight2:0)+(selected.Contains(w.boneIndex3)?w.weight3:0)>.5f;
                }).ToArray();
                if(ids.Length==0)continue;var bounds=new Bounds(points[ids[0]],Vector3.zero);
                foreach(int i in ids)bounds.Encapsulate(points[i]);
                parts.Add(new PartBounds{slot=18,name=name,minimum=bounds.min,maximum=bounds.max});
            }
            File.WriteAllText("work/reference-review/hand-bounds.json",JsonUtility.ToJson(new AccessoryReport{parts=parts.ToArray()},true));
        }

        static void ReviewTorsoSections(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var samples=new System.Collections.Generic.List<Landmark>();
            var triangles=baked.GetTriangles(37);
            foreach(float y in new[]{.26f,.28f,.30f,.32f,.34f,.36f,.38f}) {
                var cuts=new System.Collections.Generic.List<Vector3>();
                for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++) {
                    var a=points[triangles[i+j]];var b=points[triangles[i+(j+1)%3]];
                    if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y)) {
                        var p=Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y));
                        if(Mathf.Abs(p.x)<.055f)cuts.Add(p);
                    }
                }
                if(cuts.Count==0)continue;
                samples.Add(new Landmark{name="center-front",position=cuts.OrderByDescending(p=>p.z).First()});
                samples.Add(new Landmark{name="center-rear",position=cuts.OrderBy(p=>p.z).First()});
            }
            File.WriteAllText("work/reference-review/torso-sections.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }

        static void ReviewFinSections(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var sections=new System.Collections.Generic.List<PartBounds>();
            foreach(int slot in new[]{22,23,24,25}) {
                var triangles=baked.GetTriangles(slot);float sign=slot<24?1:-1;
                foreach(float distance in new[]{.14f,.16f,.18f,.20f,.22f,.24f,.26f,.275f}) {
                    float x=distance*sign;var cuts=new System.Collections.Generic.List<Vector3>();
                    for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++) {
                        Vector3 a=points[triangles[i+j]],b=points[triangles[i+(j+1)%3]];
                        if((a.x<=x&&b.x>x)||(b.x<=x&&a.x>x))
                            cuts.Add(Vector3.Lerp(a,b,(x-a.x)/(b.x-a.x)));
                    }
                    if(cuts.Count==0)continue;
                    var bounds=new Bounds(cuts[0],Vector3.zero);
                    foreach(var p in cuts)bounds.Encapsulate(p);
                    sections.Add(new PartBounds{slot=slot,name="x="+x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),minimum=bounds.min,maximum=bounds.max});
                }
            }
            File.WriteAllText("work/reference-review/fin-sections.json",JsonUtility.ToJson(new AccessoryReport{parts=sections.ToArray()},true));
        }

        // Sparse cross-section bounds are local modelling guides. The isolated
        // previews expose which lock owns a silhouette without exporting a mesh.
        static void ReviewSectionParts(Mesh baked, GameObject snapshot, Camera camera,
            Bounds total, Texture2D frame, RenderTexture target) {
            var filter=snapshot.GetComponent<MeshFilter>();
            var points=baked.vertices.Select(snapshot.transform.TransformPoint).ToArray();
            var sections=new System.Collections.Generic.List<HairSection>();
            foreach(int slot in new[]{1,3,4,6,10,11,12,15,16,17,21,22,23,24,25,26,27,28,29,30,32,34,35,37}) {
                int[] triangles=baked.GetTriangles(slot);
                float[] heights=slot>=26&&slot<=28?new[]{.01f,.02f,.03f,.04f,.05f,.06f,.07f,.08f,.09f,.10f}:
                    new[]{.04f,.06f,.08f,.10f,.15f,.20f,.25f,.30f,.35f,.40f,.45f,.50f,.55f,.60f,.65f,.70f};
                foreach(float y in heights) {
                    var cuts=new System.Collections.Generic.List<Vector3>();
                    for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++) {
                        Vector3 a=points[triangles[i+j]],b=points[triangles[i+(j+1)%3]];
                        if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y)) {
                            Vector3 p=Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y));
                            if(p.x>0)cuts.Add(p);
                        }
                    }
                    if(cuts.Count==0)continue;
                    var bounds=new Bounds(cuts[0],Vector3.zero);
                    foreach(var point in cuts)bounds.Encapsulate(point);
                    sections.Add(new HairSection{slot=slot,height=y,minimum=bounds.min,maximum=bounds.max,
                        inner=cuts.OrderBy(p=>p.x).First(),outer=cuts.OrderByDescending(p=>p.x).First(),
                        front=cuts.OrderByDescending(p=>p.z).First(),rear=cuts.OrderBy(p=>p.z).First()});
                }
                var isolated=UnityEngine.Object.Instantiate(baked);
                for(int sub=0;sub<isolated.subMeshCount;sub++)
                    if(sub!=slot)isolated.SetTriangles(Array.Empty<int>(),sub,false);
                filter.sharedMesh=isolated;
                foreach(int angle in new[]{0,35,90,180}) {
                    camera.transform.position=total.center+Quaternion.Euler(0,angle,0)*new Vector3(0,.24f,3);
                    camera.transform.LookAt(total.center);camera.Render();RenderTexture.active=target;
                    frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                    string label=slot==3?"face":slot<8?"eye-"+slot:slot>=26&&slot<=28?"shoe-"+slot:slot>=29?"cloth-"+slot:slot==12||slot>=22?"accessory-"+slot:"hair-"+slot;
                    File.WriteAllBytes("work/reference-review/"+label+"-"+angle+".png",frame.EncodeToPNG());
                }
                if(slot==10||slot==11)FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/head-hair-"+slot,.8f,new Vector3(0,-.056f,0));
                if(slot==3)FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/head-skin",.8f,new Vector3(0,-.056f,0));
                if(slot>=22&&slot<=25)FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/head-fin-"+slot,.8f,new Vector3(0,-.056f,0));
                filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
            }
            File.WriteAllText("work/reference-review/hair-sections.json",JsonUtility.ToJson(
                new HairSectionReport{sections=sections.ToArray()},true));
        }
        static void ReviewHairHem(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{15,16}) {
                var used=baked.GetTriangles(slot).Distinct().Select(i=>points[i]).ToArray();
                for(int band=-4;band<=4;band++) {
                    float x=band*.07f;
                    var lower=used.Where(p=>Mathf.Abs(p.x-x)<.035f&&p.y<.25f&&p.z<-.07f).ToArray();
                    if(lower.Length==0)continue;
                    samples.Add(new Landmark{name="slot-"+slot+"-band-"+band,position=lower.OrderBy(p=>p.y).First()});
                }
            }
            File.WriteAllText("work/reference-review/hair-hem.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }
        static void ReviewAccessoryComponents(Mesh baked,Transform snapshot,Camera camera,Bounds total,Texture2D frame,RenderTexture target) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var parts=new System.Collections.Generic.List<PartBounds>();
            var visibleParts=new System.Collections.Generic.List<PartBounds>();
            var coverage=new System.Collections.Generic.List<HairSection>();
            var profiles=new System.Collections.Generic.List<Landmark>();
            var chestSections=new System.Collections.Generic.List<HairSection>();
            var fringeProfiles=new System.Collections.Generic.List<Landmark>();
            var frontPanels=new System.Collections.Generic.List<Landmark>();
            var cheekWrap=new System.Collections.Generic.List<Landmark>();
            var chestTips=new System.Collections.Generic.List<Landmark>();
            var chestPanels=new System.Collections.Generic.List<Landmark>();
            var sideCurlSections=new System.Collections.Generic.List<Landmark>();
            var innerTips=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{1,4,6,10,11,12,15,16,22,23,24,25,26,27,28,29,30,32,37}) {
                var triangles=baked.GetTriangles(slot);var used=triangles.Distinct().ToArray();
                var parents=Enumerable.Range(0,points.Length).ToArray();
                Func<int,int> root=null;root=i=>parents[i]==i?i:parents[i]=root(parents[i]);
                for(int i=0;i<triangles.Length;i+=3) {
                    parents[root(triangles[i+1])]=root(triangles[i]);
                    parents[root(triangles[i+2])]=root(triangles[i]);
                }
                // Join duplicate seam positions for aggregate component bounds.
                foreach(var seam in used.GroupBy(i=>Vector3Int.RoundToInt(points[i]*100000))) {
                    int first=seam.First();foreach(int i in seam)parents[root(i)]=root(first);
                }
                int index=0;
                var rearVertices=new System.Collections.Generic.HashSet<int>();
                var chestVertices=new System.Collections.Generic.HashSet<int>();
                foreach(var group in used.GroupBy(i=>root(i)).OrderByDescending(g=>g.Count())) {
                    var bounds=new Bounds(points[group.First()],Vector3.zero);
                    foreach(int i in group)bounds.Encapsulate(points[i]);
                    parts.Add(new PartBounds{slot=slot,name="component-"+(index++),minimum=bounds.min,maximum=bounds.max});
                    if(slot==12||slot==37||slot==26||slot==30) {
                        var members=new System.Collections.Generic.HashSet<int>(group);var visible=new System.Collections.Generic.List<Vector3>();
                        var tex=GuideTexture(slot);var uv=baked.uv;
                        for(int ti=0;ti<triangles.Length;ti+=3) {
                            if(!members.Contains(triangles[ti]))continue;
                            int ai=triangles[ti],bi=triangles[ti+1],ci=triangles[ti+2];
                            for(int u=0;u<=6;u++)for(int v=0;v<=6-u;v++) {
                                float b=u/6f,c=v/6f,a=1-b-c;var st=uv[ai]*a+uv[bi]*b+uv[ci]*c;
                                Vector3 p=points[ai]*a+points[bi]*b+points[ci]*c;
                                if(tex&&tex.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                                if(slot==37&&(st.y>=.5f||p.y>.3f))continue;
                                visible.Add(p);
                            }
                        }
                        if(visible.Count>0) {
                            var box=new Bounds(visible[0],Vector3.zero);foreach(var p in visible)box.Encapsulate(p);
                            visibleParts.Add(new PartBounds{slot=slot,name="component-"+(index-1),minimum=box.min,maximum=box.max});
                        }
                    }
                    if(slot==12&&index==1) {
                        var members=new System.Collections.Generic.HashSet<int>(group);
                        for(int station=0;station<=24;station++) {
                            float angle=Mathf.Lerp(-1.60f,1.60f,station/24f);
                            Vector3 center=new Vector3(0,.54f,0),ray=new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),0);
                            Vector3 normal=new Vector3(ray.y,-ray.x,0);
                            var selected=new System.Collections.Generic.List<int>();
                            for(int j=0;j<triangles.Length;j+=3)if(members.Contains(triangles[j]))selected.AddRange(new[]{triangles[j],triangles[j+1],triangles[j+2]});
                            var cuts=VisibleSection(points,baked.uv,selected.ToArray(),normal,Vector3.Dot(center,normal),slot,
                                (p,st)=>Vector3.Dot(p-center,ray)>0);
                            if(cuts.Count==0)continue;
                            profiles.Add(new Landmark{name="headband-inner-"+station,position=cuts.OrderBy(p=>Vector3.Dot(p-center,ray)).First()});
                            profiles.Add(new Landmark{name="headband-outer-"+station,position=cuts.OrderByDescending(p=>Vector3.Dot(p-center,ray)).First()});
                        }
                    }
                    if(slot==10||slot==11) {
                        string name="slot-"+slot+"-component-"+(index-1);
                        var members=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int ti=0;ti<triangles.Length;ti+=3)if(members.Contains(triangles[ti]))
                            selected.AddRange(new[]{triangles[ti],triangles[ti+1],triangles[ti+2]});
                        frontPanels.Add(new Landmark{name=name+"-root",position=group.Select(i=>points[i]).OrderByDescending(p=>p.y).First()});
                        foreach(float y in new[]{.71f,.70f,.68f,.65f,.61f,.57f,.53f,.50f,.47f,.44f,.425f,.415f}) {
                            var cuts=VisibleSection(points,baked.uv,selected.ToArray(),Vector3.up,y,slot);
                            if(cuts.Count==0)continue;
                            float min=cuts.Min(p=>p.x),max=cuts.Max(p=>p.x);
                            if(slot==10&&index-1==8) {
                                var rear=cuts.Where(p=>p.x>=max-.0005f).OrderBy(p=>p.z).First();rear.x=max;
                                cheekWrap.Add(new Landmark{name="outer-rear",position=rear});
                                for(int quarter=1;quarter<=3;quarter++) {
                                    float x=Mathf.Lerp(min,max,quarter/4f),nearest=cuts.Min(p=>Mathf.Abs(p.x-x));
                                    var back=cuts.Where(p=>Mathf.Abs(p.x-x)<=nearest+.0005f).OrderBy(p=>p.z).First();back.x=x;
                                    cheekWrap.Add(new Landmark{name="back-quarter-"+quarter,position=back});
                                }
                            }
                            for(int col=0;col<5;col++) {
                                float x=Mathf.Lerp(min,max,col/4f),nearest=cuts.Min(p=>Mathf.Abs(p.x-x));
                                var point=cuts.Where(p=>Mathf.Abs(p.x-x)<=nearest+.0005f).OrderByDescending(p=>p.z).First();
                                point.x=x;
                                frontPanels.Add(new Landmark{name=name+"-cross-"+col,position=point});
                            }
                        }
                        frontPanels.Add(new Landmark{name=name+"-tip",position=group.Select(i=>points[i]).OrderBy(p=>p.y).First()});
                    }
                    if(slot==15&&(index-1==18||index-1==19)) {
                        string name="component-"+(index-1);
                        var members=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int ti=0;ti<triangles.Length;ti+=3)if(members.Contains(triangles[ti]))
                            selected.AddRange(new[]{triangles[ti],triangles[ti+1],triangles[ti+2]});
                        // Five sparse points across the principal section axis.
                        // A strip turns from almost side-facing at the temple to
                        // frontal at its tip; fixed X stations lose that turn.
                        Vector3 previous=Vector3.forward;
                        foreach(float y in new[]{.60f,.55f,.50f,.45f,.40f,.375f,.35f,.335f,.325f,.315f,.30f,.285f,.275f}) {
                            var cuts=VisibleSection(points,baked.uv,selected.ToArray(),Vector3.up,y,slot);
                            if(cuts.Count==0)continue;
                            Vector3 center=Vector3.zero;foreach(var q in cuts)center+=q/cuts.Count;
                            float xx=0,zz=0,xz=0;
                            foreach(var q in cuts) {var d=q-center;xx+=d.x*d.x;zz+=d.z*d.z;xz+=d.x*d.z;}
                            float angle=.5f*Mathf.Atan2(2*xz,xx-zz);
                            Vector3 axis=index-1==19?Vector3.right:new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                            if(Vector3.Dot(axis,previous)<0)axis=-axis;previous=axis;
                            Vector3 normal=new Vector3(-axis.z,0,axis.x);
                            float lo=cuts.Min(q=>Vector3.Dot(q-center,axis)),hi=cuts.Max(q=>Vector3.Dot(q-center,axis));
                            for(int col=0;col<5;col++) {
                                float d=Mathf.Lerp(lo,hi,col/4f),nearest=cuts.Min(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d));
                                var band=cuts.Where(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d)<=nearest+.0004f).ToArray();
                                float lower=band.Min(q=>Vector3.Dot(q-center,normal)),upper=band.Max(q=>Vector3.Dot(q-center,normal));
                                chestPanels.Add(new Landmark{name=name+"-cross-front-"+col,position=center+axis*d+normal*upper});
                                chestPanels.Add(new Landmark{name=name+"-cross-back-"+col,position=center+axis*d+normal*lower});
                            }
                        }
                        chestPanels.Add(new Landmark{name=name+"-tip",position=group.Select(i=>points[i]).OrderBy(q=>q.y).First()});
                    }
                    if(slot==11) {
                        int component=index-1;
                        string prefix="component-"+component;
                        fringeProfiles.Add(new Landmark{name=prefix+"-tip",position=group.Select(i=>points[i]).OrderBy(p=>p.y).First()});
                        var membership=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int i=0;i<triangles.Length;i+=3)if(membership.Contains(triangles[i]))
                            selected.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
                        foreach(float y in new[]{.49f,.50f,.52f,.55f,.58f,.60f,.63f,.66f,.69f}) {
                            var cuts=new System.Collections.Generic.List<Vector3>();
                            for(int i=0;i<selected.Count;i+=3)for(int j=0;j<3;j++) {
                                var p=points[selected[i+j]];var q=points[selected[i+(j+1)%3]];
                                if((p.y<=y&&q.y>y)||(q.y<=y&&p.y>y))cuts.Add(Vector3.Lerp(p,q,(y-p.y)/(q.y-p.y)));
                            }
                            if(cuts.Count==0)continue;
                            fringeProfiles.Add(new Landmark{name=prefix+"-inner",position=cuts.OrderBy(p=>Mathf.Abs(p.x)).First()});
                            fringeProfiles.Add(new Landmark{name=prefix+"-outer",position=cuts.OrderByDescending(p=>Mathf.Abs(p.x)).First()});
                            fringeProfiles.Add(new Landmark{name=prefix+"-front",position=cuts.OrderByDescending(p=>p.z).First()});
                        }
                        if(component==5||component==8||component==9) {
                            var isolated=UnityEngine.Object.Instantiate(baked);
                            for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==slot?selected.ToArray():Array.Empty<int>(),sub,false);
                            var filter=snapshot.GetComponent<MeshFilter>();filter.sharedMesh=isolated;
                            FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/fringe-component-"+component,.8f,new Vector3(0,-.056f,0));
                            filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
                        }
                    }
                    if(slot==15&&(index-1==11||index-1==14||index-1==16)) {
                        int component=index-1;
                        var membership=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int i=0;i<triangles.Length;i+=3)if(membership.Contains(triangles[i]))
                            selected.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
                        foreach(float y in new[]{.12f,.15f,.18f,.21f,.24f,.27f,.30f,.35f,.40f,.45f,.50f,.55f,.60f,.65f}) {
                            var cuts=new System.Collections.Generic.List<Vector3>();
                            for(int i=0;i<selected.Count;i+=3)for(int j=0;j<3;j++) {
                                Vector3 a=points[selected[i+j]],b=points[selected[i+(j+1)%3]];
                                if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cuts.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                            }
                            if(cuts.Count==0)continue;
                            string prefix="component-"+component;
                            sideCurlSections.Add(new Landmark{name=prefix+"-inner",position=cuts.OrderBy(p=>p.x).First()});
                            sideCurlSections.Add(new Landmark{name=prefix+"-outer",position=cuts.OrderByDescending(p=>p.x).First()});
                            sideCurlSections.Add(new Landmark{name=prefix+"-front",position=cuts.OrderByDescending(p=>p.z).First()});
                            sideCurlSections.Add(new Landmark{name=prefix+"-rear",position=cuts.OrderBy(p=>p.z).First()});
                        }
                        var isolated=UnityEngine.Object.Instantiate(baked);
                        for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==slot?selected.ToArray():Array.Empty<int>(),sub,false);
                        var filter=snapshot.GetComponent<MeshFilter>();filter.sharedMesh=isolated;
                        foreach(int angle in new[]{0,90,180}) {
                            camera.transform.position=total.center+Quaternion.Euler(0,angle,0)*new Vector3(0,.24f,3);
                            camera.transform.LookAt(total.center);camera.Render();RenderTexture.active=target;
                            frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                            File.WriteAllBytes("work/reference-review/side-curl-"+component+"-"+angle+".png",frame.EncodeToPNG());
                        }
                        filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
                    }
                    if(slot==16) {
                        int component=index-1;string prefix="component-"+component;
                        innerTips.Add(new Landmark{name=prefix+"-tip",position=group.Select(i=>points[i]).OrderBy(p=>p.y).First()});
                        var membership=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int i=0;i<triangles.Length;i+=3)if(membership.Contains(triangles[i]))
                            selected.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
                        foreach(float y in new[]{.075f,.10f,.13f,.18f,.24f,.30f,.40f}) {
                            var cuts=new System.Collections.Generic.List<Vector3>();
                            for(int i=0;i<selected.Count;i+=3)for(int j=0;j<3;j++) {
                                Vector3 a=points[selected[i+j]],b=points[selected[i+(j+1)%3]];
                                if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cuts.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                            }
                            if(cuts.Count==0)continue;
                            innerTips.Add(new Landmark{name=prefix+"-left",position=cuts.OrderBy(p=>p.x).First()});
                            innerTips.Add(new Landmark{name=prefix+"-right",position=cuts.OrderByDescending(p=>p.x).First()});
                            innerTips.Add(new Landmark{name=prefix+"-rear",position=cuts.OrderBy(p=>p.z).First()});
                        }
                        var isolated=UnityEngine.Object.Instantiate(baked);
                        for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==slot?selected.ToArray():Array.Empty<int>(),sub,false);
                        var filter=snapshot.GetComponent<MeshFilter>();filter.sharedMesh=isolated;
                        foreach(int angle in new[]{90,180}) {
                            camera.transform.position=total.center+Quaternion.Euler(0,angle,0)*new Vector3(0,.24f,3);
                            camera.transform.LookAt(total.center);camera.Render();RenderTexture.active=target;
                            frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                            File.WriteAllBytes("work/reference-review/inner-lock-"+component+"-"+angle+".png",frame.EncodeToPNG());
                        }
                        filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
                    }
                    if((slot==15||slot==16)&&bounds.min.z<-.1f&&bounds.min.y<.2f)
                        foreach(int i in group)rearVertices.Add(i);
                    if(slot==15&&bounds.min.z>0)foreach(int i in group)chestVertices.Add(i);
                }
                if(slot==15) {
                    var selected=new System.Collections.Generic.List<int>();
                    for(int i=0;i<triangles.Length;i+=3)if(chestVertices.Contains(triangles[i]))
                        selected.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
                    // Only a few extremal points guide the new branch; the
                    // reference topology remains confined to this local preview.
                    foreach(int side in new[]{-1,1}) {
                        var lower=chestVertices.Select(i=>points[i]).Where(p=>p.x*side>0&&p.y<.35f).ToArray();
                        if(lower.Length==0)continue;
                        chestTips.Add(new Landmark{name="side-"+side+"-lower-tip",position=lower.OrderBy(p=>p.y).First()});
                        chestTips.Add(new Landmark{name="side-"+side+"-outer-fork",position=lower.OrderByDescending(p=>p.x*side).First()});
                    }
                    foreach(float y in new[]{.25f,.275f,.30f,.325f,.35f,.375f,.40f,.45f,.50f,.55f}) {
                        var cuts=new System.Collections.Generic.List<Vector3>();
                        for(int i=0;i<selected.Count;i+=3)for(int j=0;j<3;j++) {
                            Vector3 a=points[selected[i+j]],b=points[selected[i+(j+1)%3]];
                            if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y)) {
                                Vector3 p=Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y));if(p.x>=0)cuts.Add(p);
                            }
                        }
                        if(cuts.Count==0)continue;
                        var bounds=new Bounds(cuts[0],Vector3.zero);foreach(var p in cuts)bounds.Encapsulate(p);
                        chestSections.Add(new HairSection{slot=slot,height=y,minimum=bounds.min,maximum=bounds.max,
                            inner=cuts.OrderBy(p=>p.x).First(),outer=cuts.OrderByDescending(p=>p.x).First(),
                            front=cuts.OrderByDescending(p=>p.z).First(),rear=cuts.OrderBy(p=>p.z).First()});
                    }
                    var isolated=UnityEngine.Object.Instantiate(baked);
                    for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==slot?selected.ToArray():Array.Empty<int>(),sub,false);
                    var filter=snapshot.GetComponent<MeshFilter>();filter.sharedMesh=isolated;
                    foreach(int angle in new[]{0,35,90}) {
                        camera.transform.position=total.center+Quaternion.Euler(0,angle,0)*new Vector3(0,.24f,3);
                        camera.transform.LookAt(total.center);camera.Render();RenderTexture.active=target;
                        frame.ReadPixels(new Rect(0,0,720,960),0,0);frame.Apply();
                        File.WriteAllBytes("work/reference-review/chest-15-"+angle+".png",frame.EncodeToPNG());
                    }
                    filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
                }
                // The outer-hair material also contains front chest locks.
                // Measure only connected rear components, so their positive-Z
                // chest tips cannot masquerade as the rear curtain's front edge.
                foreach(float y in new[]{.15f,.20f,.25f,.30f,.35f,.40f,.45f,.50f,.55f,.60f}) {
                    var cuts=new System.Collections.Generic.List<Vector3>();
                    for(int i=0;i<triangles.Length;i+=3) {
                        if(!rearVertices.Contains(triangles[i]))continue;
                        for(int j=0;j<3;j++) {
                            Vector3 a=points[triangles[i+j]],b=points[triangles[i+(j+1)%3]];
                            if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y)) {
                                Vector3 p=Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y));if(p.x>=0)cuts.Add(p);
                            }
                        }
                    }
                    if(cuts.Count==0)continue;
                    var bounds=new Bounds(cuts[0],Vector3.zero);foreach(var point in cuts)bounds.Encapsulate(point);
                    coverage.Add(new HairSection{slot=slot,height=y,minimum=bounds.min,maximum=bounds.max,
                        inner=cuts.OrderBy(p=>p.x).First(),outer=cuts.OrderByDescending(p=>p.x).First(),
                        front=cuts.OrderByDescending(p=>p.z).First(),rear=cuts.OrderBy(p=>p.z).First()});
                }
                if(slot==15)foreach(float y in new[]{.35f,.40f,.45f,.50f,.55f,.60f,.65f})
                    foreach(float x in new[]{0,.04f,.08f,.12f,.16f,.20f,.24f}) {
                        float rear=float.PositiveInfinity;
                        for(int i=0;i<triangles.Length;i+=3) {
                            if(!rearVertices.Contains(triangles[i]))continue;
                            var cuts=new System.Collections.Generic.List<Vector3>();
                            for(int j=0;j<3;j++) {
                                Vector3 a=points[triangles[i+j]],b=points[triangles[i+(j+1)%3]];
                                if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cuts.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                            }
                            if(cuts.Count!=2)continue;
                            Vector3 u=cuts[0],v=cuts[1];
                            if((u.x<=x&&x<v.x)||(v.x<=x&&x<u.x)) {
                                float z=Mathf.Lerp(u.z,v.z,(x-u.x)/(v.x-u.x));if(z<0)rear=Mathf.Min(rear,z);
                            }
                        }
                        if(!float.IsInfinity(rear))profiles.Add(new Landmark{name="rear-profile",position=new Vector3(x,y,rear)});
                    }
            }
            File.WriteAllText("work/reference-review/inner-tip-sections.json",JsonUtility.ToJson(new FaceSectionReport{samples=innerTips.ToArray()},true));
            File.WriteAllText("work/reference-review/visible-components.json",JsonUtility.ToJson(new AccessoryReport{parts=visibleParts.ToArray()},true));
            File.WriteAllText("work/reference-review/rear-profile.json",JsonUtility.ToJson(new FaceSectionReport{samples=profiles.ToArray()},true));
            File.WriteAllText("work/reference-review/front-panel-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=frontPanels.ToArray()},true));
            File.WriteAllText("work/reference-review/fringe-profile.json",JsonUtility.ToJson(new FaceSectionReport{samples=fringeProfiles.ToArray()},true));
            File.WriteAllText("work/reference-review/side-curl-sections.json",JsonUtility.ToJson(new FaceSectionReport{samples=sideCurlSections.ToArray()},true));
            File.WriteAllText("work/reference-review/cheek-wrap-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=cheekWrap.ToArray()},true));
            File.WriteAllText("work/reference-review/chest-panel-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=chestPanels.ToArray()},true));
            File.WriteAllText("work/reference-review/chest-tips.json",JsonUtility.ToJson(new FaceSectionReport{samples=chestTips.ToArray()},true));
            File.WriteAllText("work/reference-review/chest-sections.json",JsonUtility.ToJson(new HairSectionReport{sections=chestSections.ToArray()},true));
            File.WriteAllText("work/reference-review/rear-coverage-sections.json",JsonUtility.ToJson(new HairSectionReport{sections=coverage.ToArray()},true));
            File.WriteAllText("work/reference-review/accessory-components.json",
                JsonUtility.ToJson(new AccessoryReport{parts=parts.ToArray()},true));
        }
        static void ReviewFeatureGuides(Mesh baked,Transform snapshot) {
            var vertices=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var guides=new System.Collections.Generic.List<Landmark>();
            Action<string,int,float[],float,float,float> section=(name,slot,heights,minX,maxX,minZ)=> {
                var triangles=baked.GetTriangles(slot);
                foreach(float y in heights) {
                    var cuts=VisibleSection(vertices,baked.uv,triangles,Vector3.up,y,slot,(p,st)=>
                        p.x>=minX&&p.x<=maxX&&p.z>=minZ&&
                        (name!="apron"||st.y<.5f)&&(name!="bib"||(st.x>.445f&&st.x<.555f&&st.y>.543f&&st.y<.704f)));
                    if(cuts.Count==0)continue;
                    string key=name+"/"+y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
                    guides.Add(new Landmark{name=key+"/left",position=cuts.OrderBy(p=>p.x).First()});
                    guides.Add(new Landmark{name=key+"/right",position=cuts.OrderByDescending(p=>p.x).First()});
                    guides.Add(new Landmark{name=key+"/front",position=cuts.OrderByDescending(p=>p.z).First()});
                    guides.Add(new Landmark{name=key+"/rear",position=cuts.OrderBy(p=>p.z).First()});
                }
            };
            section("apron",37,new[]{.12f,.13f,.14f,.15f,.16f,.18f,.20f,.22f,.24f,.26f,.27f,.28f,.29f},-.2f,.2f,.01f);
            section("bib",37,new[]{.29f,.31f,.33f,.35f,.37f,.385f},-.075f,.075f,.04f);
            section("skirt",34,new[]{.05f,.08f,.12f,.16f,.20f,.24f},-.5f,.5f,-1);
            section("skirt-gold",35,new[]{.05f,.08f,.12f,.16f,.20f,.24f},-.5f,.5f,-1);
            section("collar-lace",30,new[]{.255f,.28f,.31f,.34f,.37f,.39f},-.2f,.2f,-1);
            section("headband",12,new[]{.56f,.60f,.65f,.70f,.75f,.78f,.80f},-.21f,.21f,-1);
            section("clip-right",12,new[]{.55f,.57f,.59f,.60f},.17f,.29f,-1);
            section("shoe-right",26,new[]{.01f,.02f,.03f,.04f,.05f,.06f},0,.2f,-1);
            // The reference uses a buckled strap, not crossing laces. Trace three
            // upper-front stations across the right shoe at its strap height.
            var shoe=baked.GetTriangles(26);
            foreach(float x in new[]{.035f,.055f,.075f}) {
                float y=.044f,front=float.NegativeInfinity;
                for(int i=0;i<shoe.Length;i+=3) {
                    var cuts=new System.Collections.Generic.List<Vector3>();
                    for(int j=0;j<3;j++) {
                        Vector3 a=vertices[shoe[i+j]],b=vertices[shoe[i+(j+1)%3]];
                        if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cuts.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                    }
                    if(cuts.Count!=2)continue;var c=cuts[0];var d=cuts[1];
                    if(Mathf.Abs(d.x-c.x)<1e-7f||x<Mathf.Min(c.x,d.x)||x>Mathf.Max(c.x,d.x))continue;
                    front=Mathf.Max(front,Mathf.Lerp(c.z,d.z,(x-c.x)/(d.x-c.x)));
                }
                if(!float.IsNegativeInfinity(front))guides.Add(new Landmark{name="shoe-strap/"+x.ToString("F3"),position=new Vector3(x,y,front)});
            }
            File.WriteAllText("work/reference-review/feature-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=guides.ToArray()},true));
        }

        // Sparse control sections only; the generated topology is independently authored.
        static void ReviewHeadEnvelope(Mesh baked,Transform snapshot) {
            var vertices=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var rows=new System.Collections.Generic.List<HairSection>();
            foreach(int group in new[]{0,1}) {
                int[] slots=group==0?new[]{3}:new[]{10,11,13,15,16,21};
                int[] triangles=slots.SelectMany(baked.GetTriangles).ToArray();
                foreach(float y in new[]{.385f,.40f,.42f,.45f,.48f,.52f,.56f,.60f,.64f,.67f,.69f,.71f,.72f,.725f,.73f}) {
                    var cuts=new System.Collections.Generic.List<Vector3>();
                    foreach(int slot in slots)cuts.AddRange(VisibleSection(vertices,baked.uv,baked.GetTriangles(slot),Vector3.up,y,slot));
                    if(cuts.Count==0)continue;
                    var bounds=new Bounds(cuts[0],Vector3.zero);foreach(var point in cuts)bounds.Encapsulate(point);
                    rows.Add(new HairSection {slot=group,height=y,minimum=bounds.min,maximum=bounds.max,
                        inner=cuts.OrderBy(p=>p.x).First(),outer=cuts.OrderByDescending(p=>p.x).First(),
                        front=cuts.OrderByDescending(p=>p.z).First(),rear=cuts.OrderBy(p=>p.z).First()});
                }
            }
            File.WriteAllText("work/reference-review/head-envelope.json",JsonUtility.ToJson(new HairSectionReport{sections=rows.ToArray()},true));
        }

        static void ReviewFringeCoverage(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;
            var rays=new System.Collections.Generic.List<Landmark>();
            foreach(Vector2 pixel in new[]{new Vector2(230,345),new Vector2(230,380),new Vector2(225,430),new Vector2(238,490)}) {
                var focus=new Vector3(0,.56f,.048f);
                Vector3 offset=Quaternion.Euler(0,35,0)*new Vector3(0,.24f,3)*.8f;
                Vector3 direction=-offset.normalized,right=Vector3.Cross(Vector3.up,direction).normalized,up=Vector3.Cross(direction,right);
                Vector3 origin=focus+offset+right*((pixel.x-360)*.528f/720)+up*((360-pixel.y)*.528f/720);
                float nearest=float.PositiveInfinity;int bestSlot=-1,bestTriangle=-1;Vector3 hit=Vector3.zero;
                for(int slot=0;slot<baked.subMeshCount;slot++) {
                    var triangles=baked.GetTriangles(slot);var texture=GuideTexture(slot);
                    for(int i=0;i<triangles.Length;i+=3) {
                        int ai=triangles[i],bi=triangles[i+1],ci=triangles[i+2];
                        Vector3 a=points[ai],ab=points[bi]-a,ac=points[ci]-a,h=Vector3.Cross(direction,ac);
                        float det=Vector3.Dot(ab,h);if(Mathf.Abs(det)<1e-9f)continue;
                        Vector3 delta=origin-a;float u=Vector3.Dot(delta,h)/det;if(u<0||u>1)continue;
                        Vector3 q=Vector3.Cross(delta,ab);float v=Vector3.Dot(direction,q)/det;if(v<0||u+v>1)continue;
                        float distance=Vector3.Dot(ac,q)/det;if(distance<=0||distance>=nearest)continue;
                        Vector2 st=uv[ai]*(1-u-v)+uv[bi]*u+uv[ci]*v;
                        if(texture&&texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                        nearest=distance;bestSlot=slot;bestTriangle=i;hit=origin+direction*distance;
                    }
                }
                int component=-1;
                if(bestSlot>=0) {
                    var indices=baked.GetTriangles(bestSlot);var parents=Enumerable.Range(0,points.Length).ToArray();
                    Func<int,int> root=null;root=i=>parents[i]==i?i:parents[i]=root(parents[i]);
                    for(int i=0;i<indices.Length;i+=3){parents[root(indices[i+1])]=root(indices[i]);parents[root(indices[i+2])]=root(indices[i]);}
                    foreach(var seam in indices.Distinct().GroupBy(i=>Vector3Int.RoundToInt(points[i]*100000))) {
                        int first=seam.First();foreach(int i in seam)parents[root(i)]=root(first);
                    }
                    int index=0;foreach(var group in indices.Distinct().GroupBy(i=>root(i)).OrderByDescending(g=>g.Count())) {
                        if(group.Contains(indices[bestTriangle]))component=index;index++;
                    }
                }
                rays.Add(new Landmark{name="pixel-"+pixel.x+"-"+pixel.y+"-slot-"+bestSlot+"-component-"+component,position=hit});
            }
            File.WriteAllText("work/reference-review/fringe-coverage-rays.json",JsonUtility.ToJson(new FaceSectionReport{samples=rays.ToArray()},true));
        }

        static void ReviewFrontCrown(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var rows=new System.Collections.Generic.List<Landmark>();
            var center=new Vector3(0,.52f,0);
            for(int angle=-80;angle<=80;angle+=10) {
                float theta=angle*Mathf.Deg2Rad;
                var ray=new Vector3(Mathf.Sin(theta),Mathf.Cos(theta),0);
                var normal=new Vector3(ray.y,-ray.x,0);
                var cuts=new System.Collections.Generic.List<Vector3>();
                foreach(int slot in new[]{10,11})cuts.AddRange(VisibleSection(points,baked.uv,baked.GetTriangles(slot),normal,
                    Vector3.Dot(center,normal),slot,(p,uv)=>p.z>.015f&&Vector3.Dot(p-center,ray)>.01f));
                if(cuts.Count>0)rows.Add(new Landmark{name="front-crown-"+angle,
                    position=cuts.OrderByDescending(p=>Vector3.Dot(p-center,ray)).First()});
            }
            File.WriteAllText("work/reference-review/front-crown.json",JsonUtility.ToJson(new FaceSectionReport{samples=rows.ToArray()},true));
        }

        static void ReviewFaceSections(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var triangles=baked.GetTriangles(3);
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(float y in new[]{.385f,.39f,.40f,.42f,.44f,.46f,.48f,.50f,.55f,.60f,.65f}) {
                var cuts=new System.Collections.Generic.List<Vector3>();
                for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++) {
                    var a=points[triangles[i+j]];var b=points[triangles[i+(j+1)%3]];
                    if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cuts.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                }
                if(cuts.Count>0)samples.Add(new Landmark{name="side",position=cuts.OrderByDescending(p=>p.x).First()});
            }
            foreach(float y in new[]{.385f,.39f,.40f,.42f,.44f,.46f,.48f,.50f,.55f,.60f,.65f})
                foreach(float x in new[]{0f,.025f,.050f,.075f,.10f,.125f,.15f}) {
                    float front=float.NegativeInfinity;
                    for(int i=0;i<triangles.Length;i+=3) {
                        var cuts=new System.Collections.Generic.List<Vector3>();
                        for(int j=0;j<3;j++) {
                            var a=points[triangles[i+j]];var b=points[triangles[i+(j+1)%3]];
                            if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cuts.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                        }
                        if(cuts.Count!=2)continue;
                        var c=cuts[0];var d=cuts[1];
                        if(Mathf.Abs(d.x-c.x)<1e-7f||x<Mathf.Min(c.x,d.x)||x>Mathf.Max(c.x,d.x))continue;
                        front=Mathf.Max(front,Mathf.Lerp(c.z,d.z,(x-c.x)/(d.x-c.x)));
                    }
                    if(!float.IsNegativeInfinity(front))samples.Add(new Landmark{name="front",position=new Vector3(x,y,front)});
                }
            File.WriteAllText("work/reference-review/face-sections.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
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
        [Serializable] sealed class AccessoryReport {public PartBounds[] parts;}
        [Serializable] sealed class PartBounds {public int slot;public string name;public Vector3 minimum,maximum;}
        [Serializable] sealed class HairSection {public int slot;public float height;public Vector3 minimum,maximum,inner,outer,front,rear;}
        [Serializable] sealed class HairSectionReport {public HairSection[] sections;}
        [Serializable] sealed class FaceSectionReport {public Landmark[] samples;}
        [Serializable] sealed class Landmark {public string name;public Vector3 position;}
        [Serializable] sealed class LandmarkReport {public Vector3 minimum,maximum;public PartBounds[] parts;public Landmark[] joints;}
        [Serializable] sealed class Stats {public int vertices,totalTriangles,materialSlots,bones,expressions;}
    }
}
