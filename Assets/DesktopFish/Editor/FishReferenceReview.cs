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
            Vector3 normal,float plane,int slot,Func<Vector3,Vector2,bool> keep=null,float maxStep=0) {
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
                int steps=maxStep>0?Mathf.Max(24,Mathf.CeilToInt(Vector3.Distance(cuts[0],cuts[1])/maxStep)):24;
                for(int k=0;k<=steps;k++) {
                    float t=k/(float)steps;Vector3 point=Vector3.Lerp(cuts[0],cuts[1],t);Vector2 st=Vector2.Lerp(tex[0],tex[1],t);
                    if(texture&&texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                    if(keep==null||keep(point,st))result.Add(point);
                }
            }
            return result;
        }
        // A near-touching fork can merge under Euclidean clustering. Use the
        // two local branches below the junction to measure each contour on its
        // own, then sample alpha along its section; only sparse controls leave
        // this local review, never source triangles or source image pixels.
        static void ReviewRearBranches(Vector3[] points,Vector2[] uv,int[] triangles,int component) {
            foreach(float split in new[]{.26f}) {
                var used=triangles.Distinct().Where(i=>points[i].y<split).ToArray();
                var ids=new System.Collections.Generic.HashSet<int>(used);
                var parents=Enumerable.Range(0,points.Length).ToArray();
                Func<int,int> root=null;root=i=>parents[i]==i?i:parents[i]=root(parents[i]);
                for(int k=0;k<triangles.Length;k+=3) {
                    var lower=new[]{triangles[k],triangles[k+1],triangles[k+2]}.Where(ids.Contains).ToArray();
                    if(lower.Length>1)for(int j=1;j<lower.Length;j++)parents[root(lower[j])]=root(lower[0]);
                }
                foreach(var seam in used.GroupBy(i=>Vector3Int.RoundToInt(points[i]*100000))) {
                    int first=seam.First();foreach(int i in seam)parents[root(i)]=root(first);
                }
                var groups=used.GroupBy(root).OrderBy(g=>g.Average(i=>points[i].x)).ToArray();
                if(groups.Length!=2)throw new InvalidOperationException("Expected two lower rear branches: "+component);
                Debug.Log("Fork topology "+component+" below "+split+": "+string.Join("; ",groups.Select(g=>g.Count()+" vertices min="+g.Min(i=>points[i].y)+" x="+g.Average(i=>points[i].x))));
                var measured=new System.Collections.Generic.List<Landmark>();
                int branch=0;
                foreach(var group in groups) {
                    var members=new System.Collections.Generic.HashSet<int>(group);var selected=new System.Collections.Generic.List<int>();
                    for(int k=0;k<triangles.Length;k+=3) {
                        var face=new[]{triangles[k],triangles[k+1],triangles[k+2]};
                        if(face.Any(members.Contains))selected.AddRange(face);
                    }
                    var texture=GuideTexture(15);
                    var tip=group.Where(i=>!texture||texture.GetPixelBilinear(uv[i].x,uv[i].y).a>=.5f)
                        .Select(i=>points[i]).OrderBy(q=>q.y).First();
                    foreach(float y in new[]{.26f,.24f,.22f,.20f,.18f,.16f,.14f,.12f,.10f,.085f,.075f,.068f}.Where(y=>y>tip.y+.001f)) {
                        var section=VisibleSection(points,uv,selected.ToArray(),Vector3.up,y,15,maxStep:.0005f);
                        if(section.Count==0)continue;
                        Vector3 center=Vector3.zero;foreach(var q in section)center+=q/section.Count;
                        float xx=0,zz=0,xz=0;foreach(var q in section){var d=q-center;xx+=d.x*d.x;zz+=d.z*d.z;xz+=d.x*d.z;}
                        float angle=.5f*Mathf.Atan2(2*xz,xx-zz);var axis=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        var normal=new Vector3(axis.z,0,-axis.x);if(Vector3.Dot(normal,new Vector3(center.x,0,center.z))<0){axis=-axis;normal=-normal;}
                        float lo=section.Min(q=>Vector3.Dot(q-center,axis)),hi=section.Max(q=>Vector3.Dot(q-center,axis));
                        for(int col=0;col<5;col++) {
                            float d=Mathf.Lerp(lo,hi,col/4f),nearest=section.Min(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d));
                            float depth=section.Where(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d)<=nearest+.00025f).Max(q=>Vector3.Dot(q-center,normal));
                            measured.Add(new Landmark{name="branch-"+branch+"-col-"+col,position=center+axis*d+normal*depth});
                        }
                    }
                    measured.Add(new Landmark{name="branch-"+branch+"-tip",position=tip});branch++;
                }
                File.WriteAllText("work/reference-review/rear-branch-guides-"+component+".json",JsonUtility.ToJson(new FaceSectionReport{samples=measured.ToArray()},true));
            }
        }
        // Local rest-space calibration; only sparse sleeve positions leave this review.
        public static void MeasureSleeves() {
            AssetDatabase.Refresh();
            var reference=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/DesktopFish/Generated/Ref.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            string sourcePath=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Mzkagami"}).Select(AssetDatabase.GUIDToAssetPath).Single();
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));
            var animator=model.GetComponent<Animator>();if(animator)animator.enabled=false;
            FishChibiStyle.Apply(model);
            var skin=model.GetComponentsInChildren<SkinnedMeshRenderer>().Single();skin.sharedMesh=reference;
            guideMaterials=skin.sharedMaterials;guideTextures.Clear();
            var rest=new Mesh();skin.BakeMesh(rest);
            var points=rest.vertices.Select(skin.transform.TransformPoint).ToArray();
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(string side in new[]{"R","L"}) {
                var upper=skin.bones.First(t=>t.name=="J_Bip_"+side+"_UpperArm").position;
                var elbow=skin.bones.First(t=>t.name=="J_Bip_"+side+"_LowerArm").position;
                var hand=skin.bones.First(t=>t.name=="J_Bip_"+side+"_Hand").position;
                samples.Add(new Landmark{name=side+"-upper",position=upper});
                samples.Add(new Landmark{name=side+"-elbow",position=elbow});
                samples.Add(new Landmark{name=side+"-hand",position=hand});
                var axis=(hand-upper).normalized;var up=Vector3.ProjectOnPlane(Vector3.up,axis).normalized;
                var across=Vector3.Cross(axis,up).normalized;
                float length=Vector3.Distance(hand,upper);
                foreach(float station in new[]{.18f,.22f,.28f,.36f,.44f,.52f,.58f,.64f,.70f,.76f,.84f,.92f,.98f,1.01f}) {
                    var center=upper+axis*(length*station);
                    var cuts=VisibleSection(points,rest.uv,rest.GetTriangles(32),axis,Vector3.Dot(center,axis),32,maxStep:.00008f);
                    if(cuts.Count<3)continue;
                    // Fixed ray angles establish corresponding folds, rather than a rotating PCA hull.
                    for(int j=0;j<12;j++) {
                        float angle=j*Mathf.PI/6;Vector3 ray=up*Mathf.Cos(angle)+across*Mathf.Sin(angle);
                        var perpendicular=Vector3.Cross(axis,ray);
                        var candidates=cuts.Where(q=>Vector3.Dot(q-center,ray)>0).ToArray();
                        if(candidates.Length==0)throw new InvalidOperationException("Empty sleeve support ray");
                        float distance=candidates.Min(q=>Mathf.Abs(Vector3.Dot(q-center,perpendicular)));
                        var point=candidates.Where(q=>Mathf.Abs(Vector3.Dot(q-center,perpendicular))<=distance+.00008f)
                            .OrderByDescending(q=>Vector3.Dot(q-center,ray)).First();
                        samples.Add(new Landmark{name=side+"-"+station.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-"+j,position=point});
                    }
                }
            }
            Directory.CreateDirectory("work/ref-sleeves-80");
            File.WriteAllText("work/ref-sleeves-80/source-rest-sections.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            UnityEngine.Object.DestroyImmediate(rest);
            foreach(var texture in guideTextures.Values)UnityEngine.Object.DestroyImmediate(texture);guideTextures.Clear();
            Debug.Log("Sparse rest sleeves measured; reference unchanged.");
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
            FishRebuiltModel.RenderBibStudy(camera,"work/reference-review/bib",.8f,new Vector3(0,-.056f,0));
            FishRebuiltModel.RenderApronStudy(camera,"work/reference-review/apron",.8f,new Vector3(0,-.056f,0));
            FishRebuiltModel.RenderFootStudy(camera,"work/reference-review/foot",.8f,new Vector3(0,-.056f,0));
            FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/aligned",.8f,new Vector3(0,-.056f,0));
            FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/small",.8f,new Vector3(0,-.056f,0),180,240);
            // Diagnose the assembled front chest silhouette without guessing
            // whether its visible width comes from paint or a backing layer.
            foreach(int slot in new[]{10,11,15,17}) {
                var isolated=UnityEngine.Object.Instantiate(baked);
                for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==slot?baked.GetTriangles(sub):Array.Empty<int>(),sub,false);
                snapshot.GetComponent<MeshFilter>().sharedMesh=isolated;
                FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/front-layer-"+slot,.8f,new Vector3(0,-.056f,0));
                snapshot.GetComponent<MeshFilter>().sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
            }
            var finOnly=UnityEngine.Object.Instantiate(baked);
            for(int sub=0;sub<finOnly.subMeshCount;sub++)finOnly.SetTriangles(sub>=22&&sub<=25?baked.GetTriangles(sub):Array.Empty<int>(),sub,false);
            snapshot.GetComponent<MeshFilter>().sharedMesh=finOnly;
            FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/head-fins",.8f,new Vector3(0,-.056f,0));
            snapshot.GetComponent<MeshFilter>().sharedMesh=baked;UnityEngine.Object.DestroyImmediate(finOnly);
            ReviewSectionParts(baked, snapshot, camera, bounds, frame, target);
            ReviewHairHem(baked,snapshot.transform);
            ReviewRearPaint(baked,snapshot.transform);
            ReviewRearPaintEdges(baked,snapshot.transform);
            ReviewFaceSections(baked,snapshot.transform);
            ReviewHeadSideGuides(baked,snapshot.transform,skin);
            ReviewEyeProfiles(baked,snapshot.transform);
            ReviewEyeApertures(baked,snapshot.transform);
            ReviewFrontDepth(baked,snapshot.transform);
            ReviewHeadEnvelope(baked,snapshot.transform);
            ReviewFrontCrown(baked,snapshot.transform);
            ReviewFrontPaint(baked,snapshot,camera);
            ReviewOuterCurtainClay(baked,snapshot,camera);
            ReviewCommonSurfaces(baked,snapshot,camera);
            ReviewRearCrown(baked,snapshot.transform);
            ReviewFringeCoverage(baked,snapshot.transform);
            ReviewFeatureGuides(baked,snapshot.transform);
            ReviewApronDetails(baked,snapshot.transform);
            ReviewFinSections(baked,snapshot.transform);
            ReviewFinShells(baked,snapshot.transform);
            ReviewTorsoSections(baked,snapshot.transform);
            ReviewBibGuides(baked,snapshot.transform);
            ReviewBibSurface(baked,snapshot,camera);
            ReviewSideLaceSurface(baked,snapshot,camera);
            ReviewCollarRibbonGuides(baked,snapshot.transform);
            ReviewRibbonVolume(baked,snapshot.transform);
            ReviewFootGuides(baked,snapshot.transform);
            ReviewHemGuides(baked,snapshot.transform);
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

        // Sparse alpha-visible open-shell sections, with a stable angular order.
        static void ReviewFinShells(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var guides=new System.Collections.Generic.List<Landmark>();
            var diagnostics=new System.Collections.Generic.List<string>();
            foreach(int slot in new[]{22,23,24,25}) {
                int sign=slot<24?1:-1;var triangles=baked.GetTriangles(slot);
                var rawTip=triangles.Distinct().Select(i=>points[i]).OrderByDescending(p=>p.x*sign).First();
                float last=.25f,firstMissing=rawTip.x*sign+.0001f;
                for(float x=.25f;x<=rawTip.x*sign+.0001f;x+=.001f) {
                    if(VisibleSection(points,baked.uv,triangles,Vector3.right,x*sign,slot,maxStep:.00004f).Count>0)last=x;
                    else if(x>last) {firstMissing=x;break;}
                }
                for(int j=0;j<18;j++) {
                    float mid=(last+firstMissing)*.5f;
                    if(VisibleSection(points,baked.uv,triangles,Vector3.right,mid*sign,slot,maxStep:.00004f).Count>0)last=mid;else firstMissing=mid;
                }
                var tipCut=VisibleSection(points,baked.uv,triangles,Vector3.right,(last-.000001f)*sign,slot,maxStep:.00002f);
                Vector3 tip=Vector3.zero;foreach(var p in tipCut)tip+=p/tipCut.Count;
                foreach(float distance in new[]{.140f,.150f,.160f,.180f,.200f,.220f,.240f,.260f,.275f}.Where(x=>x<tip.x*sign-.001f)) {
                    var cuts=VisibleSection(points,baked.uv,triangles,Vector3.right,distance*sign,slot,maxStep:.00008f);
                    float cy=(cuts.Min(p=>p.y)+cuts.Max(p=>p.y))*.5f,cz=(cuts.Min(p=>p.z)+cuts.Max(p=>p.z))*.5f;
                    float ry=(cuts.Max(p=>p.y)-cuts.Min(p=>p.y))*.5f,rz=(cuts.Max(p=>p.z)-cuts.Min(p=>p.z))*.5f;
                    Func<Vector3,float> angle=p=>Mathf.Repeat(Mathf.Atan2((p.y-cy)/ry,(p.z-cz)/rz),Mathf.PI*2);
                    var ordered=cuts.OrderBy(angle).ToArray();float gap=-1;int after=0;
                    for(int i=0;i<ordered.Length;i++) {
                        float delta=Mathf.Repeat(angle(ordered[(i+1)%ordered.Length])-angle(ordered[i]),Mathf.PI*2);
                        if(delta>gap){gap=delta;after=(i+1)%ordered.Length;}
                    }
                    float start=angle(ordered[after]),length=Mathf.PI*2-gap;
                    if(start>Mathf.PI)start-=Mathf.PI*2;
                    float end=Mathf.Min(start+length,4.5f);
                    // Fixed angular landmarks keep root partial sections from
                    // twisting their top/back quadrants into the frontal lip.
                    for(int col=0;col<=8;col++) {
                        float theta=col==0?start:col==8?end:Mathf.Clamp(.25f+(col-1)*.55f,start,end);
                        var q=ordered.OrderBy(p=>Mathf.Abs(Mathf.DeltaAngle(theta*Mathf.Rad2Deg,angle(p)*Mathf.Rad2Deg))).First();
                        guides.Add(new Landmark{name="shell-"+slot+"-x-"+distance.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"-col-"+col,position=q});
                    }
                    diagnostics.Add(slot+" x="+distance+" start="+start+" end="+(start+length)+" gap="+gap);
                }
                guides.Add(new Landmark{name="shell-"+slot+"-tip",position=tip});
            }
            File.WriteAllText("work/reference-review/fin-shell-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=guides.ToArray()},true));
            File.WriteAllLines("work/reference-review/fin-shell-diagnostics.txt",diagnostics);
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
            var guides=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{22,23,24,25}) {
                float sign=slot<24?1:-1;var triangles=baked.GetTriangles(slot);
                foreach(float distance in new[]{.14f,.16f,.18f,.20f,.22f,.24f,.26f,.275f}) {
                    var cuts=VisibleSection(points,baked.uv,triangles,Vector3.right,distance*sign,slot);
                    if(cuts.Count<3)continue;
                    float cy=(cuts.Min(p=>p.y)+cuts.Max(p=>p.y))*.5f,cz=(cuts.Min(p=>p.z)+cuts.Max(p=>p.z))*.5f;
                    float ry=(cuts.Max(p=>p.y)-cuts.Min(p=>p.y))*.5f,rz=(cuts.Max(p=>p.z)-cuts.Min(p=>p.z))*.5f;
                    for(int i=0;i<8;i++) {
                        float theta=i*Mathf.PI/4;
                        var near=cuts.OrderBy(p=>Mathf.Abs(Mathf.DeltaAngle(theta*Mathf.Rad2Deg,Mathf.Atan2((p.y-cy)/ry,(p.z-cz)/rz)*Mathf.Rad2Deg))).First();
                        guides.Add(new Landmark{name="fin-"+slot+"-x-"+distance.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"-ring-"+i,position=near});
                    }
                }
                var used=triangles.Distinct().Select(i=>points[i]).OrderByDescending(p=>p.x*sign).First();
                guides.Add(new Landmark{name="fin-"+slot+"-tip",position=used});
            }
            File.WriteAllText("work/reference-review/fin-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=guides.ToArray()},true));
        }

        // Sparse cross-section bounds are local modelling guides. The isolated
        // previews expose which lock owns a silhouette without exporting a mesh.
        static void ReviewSectionParts(Mesh baked, GameObject snapshot, Camera camera,
            Bounds total, Texture2D frame, RenderTexture target) {
            var filter=snapshot.GetComponent<MeshFilter>();
            var points=baked.vertices.Select(snapshot.transform.TransformPoint).ToArray();
            var sections=new System.Collections.Generic.List<HairSection>();
            foreach(int slot in new[]{1,3,4,5,6,7,10,11,12,15,16,17,18,21,22,23,24,25,26,27,28,29,30,31,32,34,35,36,37}) {
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
                if(slot==29||slot==30)FishRebuiltModel.RenderBibStudy(camera,"work/reference-review/bib-cloth-"+slot,.8f,new Vector3(0,-.056f,0));
                if(slot>=26&&slot<=28)FishRebuiltModel.RenderFootStudy(camera,"work/reference-review/foot-part-"+slot,.8f,new Vector3(0,-.056f,0));
                if(slot==34||slot==35||slot==36)FishRebuiltModel.RenderFootStudy(camera,"work/reference-review/hem-part-"+slot,.8f,new Vector3(0,-.056f,0));
                if(slot==3||slot==18)FishRebuiltModel.RenderHeadStudy(camera,slot==3?"work/reference-review/head-skin":"work/reference-review/head-body-skin",.8f,new Vector3(0,-.056f,0));
                if(slot>=22&&slot<=25)FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/head-fin-"+slot,.8f,new Vector3(0,-.056f,0));
                filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
            }
            var footwear=UnityEngine.Object.Instantiate(baked);
            for(int sub=0;sub<footwear.subMeshCount;sub++)if(sub<26||sub>28)footwear.SetTriangles(Array.Empty<int>(),sub,false);
            filter.sharedMesh=footwear;
            FishRebuiltModel.RenderFootStudy(camera,"work/reference-review/foot-isolated",.8f,new Vector3(0,-.056f,0));
            filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(footwear);
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
            var curlFaces=new System.Collections.Generic.Dictionary<int,int[]>();
            var chestFaces=new System.Collections.Generic.Dictionary<int,int[]>();
            var envelopeFaces=new System.Collections.Generic.Dictionary<int,System.Collections.Generic.List<int>>{
                {15,new System.Collections.Generic.List<int>()},{16,new System.Collections.Generic.List<int>()}};
            var innerTips=new System.Collections.Generic.List<Landmark>();
            var rearPanels=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{1,4,6,10,11,12,15,16,22,23,24,25,26,27,28,29,30,31,32,37}) {
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
                    if(slot==16||slot==15&&new[]{0,1,2,3,4,5,6,7,9,10,13}.Contains(index-1)) {
                        var membership=new System.Collections.Generic.HashSet<int>(group);
                        for(int face=0;face<triangles.Length;face+=3)if(membership.Contains(triangles[face]))
                            envelopeFaces[slot].AddRange(new[]{triangles[face],triangles[face+1],triangles[face+2]});
                    }
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
                        var heights=new[]{.71f,.70f,.68f,.65f,.61f,.57f,.53f,.50f,.47f,.44f,.425f,.415f};
                        if(slot==10&&index-1==0)heights=new[]{.723f,.720f,.717f,.715f}.Concat(heights).ToArray();
                        foreach(float y in heights) {
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
                    if(slot==15&&new[]{4,5,8,9,11,12,13,14,15,16}.Contains(index-1)) {
                        var members=new System.Collections.Generic.HashSet<int>(group);
                        curlFaces[index-1]=Enumerable.Range(0,triangles.Length/3).Where(i=>members.Contains(triangles[i*3]))
                            .SelectMany(i=>new[]{triangles[i*3],triangles[i*3+1],triangles[i*3+2]}).ToArray();
                    }
                    if(slot==15&&(index==1||index==2)) {
                        var members=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int ti=0;ti<triangles.Length;ti+=3)if(members.Contains(triangles[ti]))
                            selected.AddRange(new[]{triangles[ti],triangles[ti+1],triangles[ti+2]});
                        ReviewRearBranches(points,baked.uv,selected.ToArray(),index-1);
                    }
                    if((slot==15&&new[]{0,1,2,3,4,5,6,7,8,9,10,11,13}.Contains(index-1))||slot==16) {
                        int component=index-1;string prefix=(slot==16?"inner-":"component-")+component;
                        var members=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int ti=0;ti<triangles.Length;ti+=3)if(members.Contains(triangles[ti]))
                            selected.AddRange(new[]{triangles[ti],triangles[ti+1],triangles[ti+2]});
                        if(slot==15&&new[]{3,7,10,6,2}.Contains(component))ReviewLowerRearGuides(points,baked.uv,selected.ToArray(),component);
                        rearPanels.Add(new Landmark{name=prefix+"-root",position=group.Select(i=>points[i]).OrderByDescending(p=>p.y).First()});
                        Vector3 tip=group.Select(i=>points[i]).OrderBy(p=>p.y).First();
                        Vector3 previous=Vector3.right;
                        foreach(float y in new[]{.725f,.715f,.69f,.65f,.59f,.51f,.43f,.35f,.28f,.22f,.17f,.135f,.11f,tip.y+.006f}.Where(y=>y>tip.y+.002f).Distinct().OrderByDescending(y=>y)) {
                            var cuts=VisibleSection(points,baked.uv,selected.ToArray(),Vector3.up,y,slot);
                            if(cuts.Count==0)continue;
                            Vector3 center=Vector3.zero;foreach(var q in cuts)center+=q/cuts.Count;
                            float xx=0,zz=0,xz=0;
                            foreach(var q in cuts){var d=q-center;xx+=d.x*d.x;zz+=d.z*d.z;xz+=d.x*d.z;}
                            float angle=.5f*Mathf.Atan2(2*xz,xx-zz);
                            Vector3 axis=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                            // Each ring travels left-to-right along the outward
                            // face; its normal points away from the scalp axis.
                            Vector3 normal=new Vector3(axis.z,0,-axis.x);
                            if(Vector3.Dot(normal,new Vector3(center.x,0,center.z))<0){axis=-axis;normal=-normal;}
                            if(Vector3.Dot(axis,previous)<-.5f)Debug.LogWarning("rear section axis turn "+prefix+" y="+y);
                            previous=axis;
                            float lo=cuts.Min(q=>Vector3.Dot(q-center,axis)),hi=cuts.Max(q=>Vector3.Dot(q-center,axis));
                            for(int col=0;col<5;col++) {
                                float d=Mathf.Lerp(lo,hi,col/4f),nearest=cuts.Min(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d));
                                var band=cuts.Where(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d)<=nearest+.00025f).ToArray();
                                float near=band.Max(q=>Vector3.Dot(q-center,normal));
                                float far=band.Min(q=>Vector3.Dot(q-center,normal));
                                rearPanels.Add(new Landmark{name=prefix+"-outer-"+col,position=center+axis*d+normal*near});
                                if(col>0&&col<4)rearPanels.Add(new Landmark{name=prefix+"-inner-"+col,position=center+axis*d+normal*far});
                            }
                        }
                        rearPanels.Add(new Landmark{name=prefix+"-tip",position=tip});
                        var isolated=UnityEngine.Object.Instantiate(baked);
                        for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==slot?selected.ToArray():Array.Empty<int>(),sub,false);
                        var filter=snapshot.GetComponent<MeshFilter>();filter.sharedMesh=isolated;
                        FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/rear-lock-"+slot+"-"+component,.8f,new Vector3(0,-.056f,0));
                        if(slot==15&&new[]{3,7,10,6,2}.Contains(component)) {
                            var grey=UnityEngine.Object.Instantiate(isolated);grey.colors=Enumerable.Repeat(new Color(.65f,.65f,.65f,1),grey.vertexCount).ToArray();
                            var renderer=snapshot.GetComponent<MeshRenderer>();var mats=renderer.sharedMaterials;
                            var material=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));material.mainTexture=Texture2D.whiteTexture;material.SetFloat("_OutlineWidth",0);
                            renderer.sharedMaterials=Enumerable.Repeat(material,mats.Length).ToArray();filter.sharedMesh=grey;
                            FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/outer-clay-"+component,.8f,new Vector3(0,-.056f,0));
                            renderer.sharedMaterials=mats;filter.sharedMesh=isolated;UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(grey);
                        }
                        filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
                    }
                    if(slot==15&&new[]{17,18,19,20}.Contains(index-1)) {
                        var members=new System.Collections.Generic.HashSet<int>(group);var selected=new System.Collections.Generic.List<int>();
                        for(int k=0;k<triangles.Length;k+=3)if(members.Contains(triangles[k]))selected.AddRange(new[]{triangles[k],triangles[k+1],triangles[k+2]});
                        chestFaces[index-1]=selected.ToArray();
                        var isolated=UnityEngine.Object.Instantiate(baked);
                        for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==slot?selected.ToArray():Array.Empty<int>(),sub,false);
                        var filter=snapshot.GetComponent<MeshFilter>();filter.sharedMesh=isolated;
                        FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/chest-lock-"+(index-1),.8f,new Vector3(0,-.056f,0));
                        filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
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
                    if(slot==15&&new[]{11,12,14,15,16}.Contains(index-1)) {
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
                        int component=index-1;
                        string prefix="component-"+component;
                        innerTips.Add(new Landmark{name=prefix+"-tip",position=group.Select(i=>points[i]).OrderBy(p=>p.y).First()});
                        var membership=new System.Collections.Generic.HashSet<int>(group);
                        var selected=new System.Collections.Generic.List<int>();
                        for(int i=0;i<triangles.Length;i+=3)if(membership.Contains(triangles[i]))
                            selected.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
                        if(component>=0&&component<=5) {
                            ReviewInnerRearGuides(points,baked.uv,selected.ToArray(),component);
                            var clayFilter=snapshot.GetComponent<MeshFilter>();var renderer=snapshot.GetComponent<MeshRenderer>();var mats=renderer.sharedMaterials;
                            var clay=UnityEngine.Object.Instantiate(baked);
                            for(int sub=0;sub<clay.subMeshCount;sub++)clay.SetTriangles(sub==16?selected.ToArray():Array.Empty<int>(),sub,false);
                            clay.colors=Enumerable.Repeat(new Color(.65f,.65f,.65f,1),clay.vertexCount).ToArray();
                            var material=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));material.mainTexture=Texture2D.whiteTexture;material.SetFloat("_OutlineWidth",0);
                            clayFilter.sharedMesh=clay;renderer.sharedMaterials=Enumerable.Repeat(material,mats.Length).ToArray();
                            FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/inner-clay-"+component,.8f,new Vector3(0,-.056f,0));
                            clayFilter.sharedMesh=baked;renderer.sharedMaterials=mats;UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(clay);
                        }
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
            ReviewChestLoftGuides(points,baked.uv,chestFaces);
            ReviewChestBackingGuides(points,baked.uv,baked.GetTriangles(17),camera,baked,snapshot);
            ReviewRearEnvelope(points,baked.uv,envelopeFaces);
            ReviewSideCurlGuides(points,baked.uv,curlFaces);
            ReviewOuterCurlRefinement(baked,snapshot.gameObject,camera,points,baked.uv,curlFaces);
            ReviewSideLockGuides(points,baked.uv,curlFaces);
            File.WriteAllText("work/reference-review/rear-panel-sections.json",JsonUtility.ToJson(new FaceSectionReport{samples=rearPanels.ToArray()},true));
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
        static void ReviewChestBackingGuides(Vector3[] points,Vector2[] uv,int[] triangles,Camera camera,Mesh baked,Transform snapshot) {
            var samples=new System.Collections.Generic.List<Landmark>();var bounds=new System.Collections.Generic.List<Landmark>();
            foreach(int sign in new[]{-1,1}) {
                var selected=Enumerable.Range(0,triangles.Length/3).Where(i=>
                    Enumerable.Range(0,3).Average(j=>points[triangles[i*3+j]].x)*sign>0)
                    .SelectMany(i=>new[]{triangles[i*3],triangles[i*3+1],triangles[i*3+2]}).ToArray();
                var visible=selected.Distinct().Where(i=>GuideTexture(17).GetPixelBilinear(uv[i].x,uv[i].y).a>=.5f).Select(i=>points[i]).ToArray();
                float tipY=visible.Min(p=>p.y);
                // Eight points describe a broad curved sheet with a measured
                // return. World-X order stays fixed through the lower S bend.
                foreach(float y in new[]{.70f,.66f,.62f,.58f,.54f,.50f,.46f,.43f,.41f,.39f,.37f,.35f,.33f,.315f,.30f,.29f,.285f,tipY+.0015f}.Where(y=>y>tipY+.0003f).Distinct().OrderByDescending(y=>y)) {
                    var cuts=VisibleSection(points,uv,selected,Vector3.up,y,17,maxStep:.00015f);
                    if(cuts.Count<2)continue;float lo=cuts.Min(p=>p.x),hi=cuts.Max(p=>p.x);
                    for(int column=0;column<8;column++) {
                        int q=column<=4?column:8-column;float x=Mathf.Lerp(lo,hi,q/4f),nearest=cuts.Min(p=>Mathf.Abs(p.x-x));
                        var band=cuts.Where(p=>Mathf.Abs(p.x-x)<=nearest+.0001f).ToArray();
                        float front=band.Max(p=>p.z),back=band.Min(p=>p.z);
                        float z=q==0||q==4?(front+back)*.5f:column<4?front:back;
                        samples.Add(new Landmark{name="backing-"+sign+"-ring-"+column,position=new Vector3(x,y,z)});
                    }
                    bounds.Add(new Landmark{name="backing-"+sign+"-minimum",position=new Vector3(lo,y,cuts.Min(p=>p.z))});
                    bounds.Add(new Landmark{name="backing-"+sign+"-maximum",position=new Vector3(hi,y,cuts.Max(p=>p.z))});
                }
                Vector3 tip=visible.OrderBy(p=>p.y).First();samples.Add(new Landmark{name="backing-"+sign+"-tip",position=tip});
                var isolated=UnityEngine.Object.Instantiate(baked);
                for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==17?selected:Array.Empty<int>(),sub,false);
                snapshot.GetComponent<MeshFilter>().sharedMesh=isolated;
                FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/chest-backing-"+sign,.8f,new Vector3(0,-.056f,0));
                snapshot.GetComponent<MeshFilter>().sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
            }
            File.WriteAllText("work/reference-review/chest-backing-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            File.WriteAllText("work/reference-review/chest-backing-envelope.json",JsonUtility.ToJson(new FaceSectionReport{samples=bounds.ToArray()},true));
        }
        static void ReviewChestLoftGuides(Vector3[] points,Vector2[] uv,System.Collections.Generic.Dictionary<int,int[]> faces) {
            var samples=new System.Collections.Generic.List<Landmark>();var envelope=new System.Collections.Generic.List<Landmark>();var texture=GuideTexture(15);
            foreach(var entry in faces.OrderBy(p=>p.Key)) {
                int component=entry.Key;var triangles=entry.Value;
                var visible=triangles.Distinct().Where(i=>texture.GetPixelBilinear(uv[i].x,uv[i].y).a>=.5f).Select(i=>points[i]).ToArray();
                Vector3 root=visible.OrderByDescending(p=>p.y).First(),tip=visible.OrderBy(p=>p.y).First();
                samples.Add(new Landmark{name="chest-"+component+"-root",position=root});float phase=float.NaN;
                var heights=new[]{.70f,.66f,.62f,.58f,.54f,.50f,.46f,.42f,.40f,.38f,.36f,.35f,.34f,.33f,.32f,.31f,.30f,.29f,.28f,.275f,tip.y+.003f};
                foreach(float y in heights.Where(y=>y>tip.y+.001f&&y<root.y-.001f).Distinct().OrderByDescending(y=>y)) {
                    var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,15,maxStep:.0004f);
                    if(cuts.Count<2)continue;var ring=PhaseAlignedCurlRing(cuts,ref phase);
                    string key="chest-"+component+"-"+y.ToString("F5",System.Globalization.CultureInfo.InvariantCulture);
                    envelope.Add(new Landmark{name=key+"-minimum",position=new Vector3(cuts.Min(p=>p.x),y,cuts.Min(p=>p.z))});
                    envelope.Add(new Landmark{name=key+"-maximum",position=new Vector3(cuts.Max(p=>p.x),y,cuts.Max(p=>p.z))});
                    for(int col=0;col<16;col++)samples.Add(new Landmark{name="chest-"+component+"-ring-"+col,position=ring[col]});
                }
                samples.Add(new Landmark{name="chest-"+component+"-tip",position=tip});
            }
            File.WriteAllText("work/reference-review/chest-loft-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            File.WriteAllText("work/reference-review/chest-loft-envelope.json",JsonUtility.ToJson(new FaceSectionReport{samples=envelope.ToArray()},true));
        }

        static void ReviewRearEnvelope(Vector3[] points,Vector2[] uv,System.Collections.Generic.Dictionary<int,System.Collections.Generic.List<int>> faces) {
            var samples=new System.Collections.Generic.List<Landmark>();int row=0;
            foreach(float y in new[]{.53f,.49f,.45f,.41f,.37f,.33f,.31f,.29f,.25f,.21f}) {
                var visible=new System.Collections.Generic.List<Vector3>();
                foreach(var entry in faces)visible.AddRange(VisibleSection(points,uv,entry.Value.ToArray(),Vector3.up,y,entry.Key,maxStep:.0004f));
                float lo=visible.Min(p=>p.x)+.000001f,hi=visible.Max(p=>p.x)-.000001f;
                for(int col=0;col<=20;col++) {
                    float x=Mathf.Lerp(lo,hi,col/20f),z=float.PositiveInfinity;
                    foreach(var entry in faces) {
                        int[] triangles=entry.Value.ToArray();var texture=GuideTexture(entry.Key);
                        for(int i=0;i<triangles.Length;i+=3) {
                            var cuts=new System.Collections.Generic.List<Vector3>();var tex=new System.Collections.Generic.List<Vector2>();
                            for(int j=0;j<3;j++) {
                                int ai=triangles[i+j],bi=triangles[i+(j+1)%3];Vector3 a=points[ai],b=points[bi];
                                if(!((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y)))continue;
                                float t=(y-a.y)/(b.y-a.y);cuts.Add(Vector3.Lerp(a,b,t));tex.Add(Vector2.Lerp(uv[ai],uv[bi],t));
                            }
                            if(cuts.Count!=2||Mathf.Abs(cuts[1].x-cuts[0].x)<1e-9f)continue;
                            float u=(x-cuts[0].x)/(cuts[1].x-cuts[0].x);if(u<-.00001f||u>1.00001f)continue;
                            Vector2 st=Vector2.Lerp(tex[0],tex[1],u);if(texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                            z=Mathf.Min(z,Mathf.Lerp(cuts[0].z,cuts[1].z,u));
                        }
                    }
                    if(float.IsInfinity(z))throw new InvalidOperationException("No visible rear envelope hit: "+x+" / "+y);
                    samples.Add(new Landmark{name="rear-envelope-"+row+"-"+col,position=new Vector3(x,y,z)});
                }
                row++;
            }
            File.WriteAllText("work/reference-review/rear-envelope-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }

        static void ReviewSideLockGuides(Vector3[] points,Vector2[] uv,System.Collections.Generic.Dictionary<int,int[]> faces) {
            var samples=new System.Collections.Generic.List<Landmark>();var bounds=new System.Collections.Generic.List<Landmark>();var texture=GuideTexture(15);
            for(int side=0;side<2;side++) {
                int first=side==0?4:5,second=side==0?9:13;var triangles=faces[first].Concat(faces[second]).ToArray();
                var visible=triangles.Distinct().Where(i=>texture.GetPixelBilinear(uv[i].x,uv[i].y).a>=.5f).ToArray();
                Vector3 root=visible.Select(i=>points[i]).OrderByDescending(p=>p.y).First(),tip=visible.Select(i=>points[i]).OrderBy(p=>p.y).First();
                samples.Add(new Landmark{name="side-lock-"+side+"-root",position=root});float phase=float.NaN;
                foreach(float y in new[]{.725f,.72f,.71f,.69f,.66f,.63f,.60f,.57f,.54f,.51f,.48f,.45f,.42f,.39f,.36f,.33f,.30f,.27f,.24f,.21f,.18f,.15f,.13f,.11f,tip.y+.006f}.Where(y=>y>tip.y+.002f&&y<root.y-.001f).Distinct().OrderByDescending(y=>y)) {
                    var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,15,maxStep:.0004f);if(cuts.Count<2)continue;
                    var ring=PhaseAlignedCurlRing(cuts,ref phase);string name="side-lock-"+side+"-"+y.ToString("F5",System.Globalization.CultureInfo.InvariantCulture);
                    bounds.Add(new Landmark{name=name+"-minimum",position=new Vector3(cuts.Min(p=>p.x),y,cuts.Min(p=>p.z))});
                    bounds.Add(new Landmark{name=name+"-maximum",position=new Vector3(cuts.Max(p=>p.x),y,cuts.Max(p=>p.z))});
                    for(int col=0;col<16;col++)samples.Add(new Landmark{name="side-lock-"+side+"-ring-"+col,position=ring[col]});
                }
                samples.Add(new Landmark{name="side-lock-"+side+"-tip",position=tip});
            }
            File.WriteAllText("work/reference-review/side-lock-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            File.WriteAllText("work/reference-review/side-lock-envelope.json",JsonUtility.ToJson(new FaceSectionReport{samples=bounds.ToArray()},true));
        }

        // Sparse closed sections of the two visible lower side hooks. Local
        // support hulls select positions only; source indices/UVs are not reused.
        static void ReviewOuterCurlRefinement(Mesh baked,GameObject snapshot,Camera camera,Vector3[] points,Vector2[] uv,
            System.Collections.Generic.Dictionary<int,int[]> faces) {
            var samples=new System.Collections.Generic.List<Landmark>();
            var rows=new System.Collections.Generic.List<string>();
            foreach(int component in new[]{8,11}) {
                int[] triangles=faces[component];var texture=GuideTexture(15);var all=triangles.Distinct().ToArray();
                float low=all.Min(i=>points[i].y),high=all.Max(i=>points[i].y);
                Func<float,System.Collections.Generic.List<Vector3>> section=y=>VisibleSection(points,uv,triangles,Vector3.up,y,15,maxStep:.00008f);
                float first=low;
                while(first<high&&section(first+.0000001f).Count==0)first+=.0005f;
                float a=first-.0005f,b=first;for(int step=0;step<18;step++){float m=(a+b)*.5f;if(section(m).Count>0)b=m;else a=m;}
                float tip=b;Vector3 previous=Vector3.zero;
                var heights=new[]{.57f,.55f,.53f,.51f,.49f,.47f,.45f,.43f,.41f,.39f,.37f,.35f,.33f,.31f,.29f,.27f,.255f,.245f,.235f,.2275f,.2225f,.218f,.214f}
                    .Select(y=>(y-.07f)/1.25f).Where(y=>y>tip+.00005f&&y<high).Concat(new[]{tip+.0002f,tip+.00004f}).Distinct().OrderByDescending(y=>y).ToArray();
                int row=0;
                foreach(float y in heights) {
                    var cuts=section(y);if(cuts.Count<3)continue;
                    var unique=cuts.GroupBy(q=>Vector3Int.RoundToInt(q*100000)).Select(g=>g.First()).ToArray();
                    Vector3 center=Vector3.zero;foreach(var q in unique)center+=q/unique.Length;
                    float xx=0,zz=0,xz=0;foreach(var q in unique){var d=q-center;xx+=d.x*d.x;zz+=d.z*d.z;xz+=d.x*d.z;}
                    float angle=.5f*Mathf.Atan2(2*xz,xx-zz);Vector3 axis=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    Vector3 normal=new Vector3(axis.z,0,-axis.x);
                    if(previous==Vector3.zero?Vector3.Dot(normal,new Vector3(center.x,0,center.z))<0:Vector3.Dot(axis,previous)<0){axis=-axis;normal=-normal;}
                    previous=axis;
                    var projected=unique.Select(q=>new Vector2(Vector3.Dot(q-center,axis),Vector3.Dot(q-center,normal))).OrderBy(q=>q.x).ThenBy(q=>q.y).ToArray();
                    Func<Vector2,Vector2,float> cross=(u,v)=>u.x*v.y-u.y*v.x;
                    var hull=new System.Collections.Generic.List<Vector2>();
                    foreach(var q in projected){while(hull.Count>=2&&cross(hull[hull.Count-1]-hull[hull.Count-2],q-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(q);}
                    int lower=hull.Count;for(int k=projected.Length-2;k>=0;k--){var q=projected[k];while(hull.Count>lower&&cross(hull[hull.Count-1]-hull[hull.Count-2],q-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(q);}hull.RemoveAt(hull.Count-1);
                    float lo=projected.Min(q=>q.x),hi=projected.Max(q=>q.x);
                    for(int col=0;col<8;col++) {
                        int quarter=col<=4?col:8-col;float d=Mathf.Lerp(lo+.00000001f,hi-.00000001f,quarter/4f);
                        var hits=new System.Collections.Generic.List<float>();
                        for(int k=0;k<hull.Count;k++) {
                            Vector2 p=hull[k],q=hull[(k+1)%hull.Count];float width=q.x-p.x;
                            if(Mathf.Abs(width)<1e-12f)continue;float u=(d-p.x)/width;
                            if(u>=-.00001f&&u<=1.00001f)hits.Add(Mathf.Lerp(p.y,q.y,u));
                        }
                        if(hits.Count==0)throw new InvalidOperationException("No curl support hit "+component+"/"+row+"/"+col);
                        float depth=col<=4?hits.Max():hits.Min();
                        samples.Add(new Landmark{name="outer-hook-"+component+"-"+row+"-"+col,position=center+axis*d+normal*depth});
                    }
                    rows.Add(component+","+y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+lo+","+hi+","+hull.Count);row++;
                }
                var tipSection=section(tip+.000001f);Vector3 endpoint=Vector3.zero;foreach(var q in tipSection)endpoint+=q/tipSection.Count;
                samples.Add(new Landmark{name="outer-hook-"+component+"-tip",position=endpoint});
                var filter=snapshot.GetComponent<MeshFilter>();var renderer=snapshot.GetComponent<MeshRenderer>();var original=renderer.sharedMaterials;
                var isolated=UnityEngine.Object.Instantiate(baked);for(int slot=0;slot<isolated.subMeshCount;slot++)isolated.SetTriangles(slot==15?triangles:Array.Empty<int>(),slot,false);filter.sharedMesh=isolated;
                FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/outer-hook-"+component,.8f,new Vector3(0,-.056f,0));
                isolated.colors=Enumerable.Repeat(new Color(.65f,.65f,.65f,1),isolated.vertexCount).ToArray();var material=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));
                material.mainTexture=FishRebuiltModel.DiagnosticAlpha(texture);material.SetFloat("_OutlineWidth",0);renderer.sharedMaterials=Enumerable.Repeat(material,original.Length).ToArray();
                FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/outer-hook-clay-"+component,.8f,new Vector3(0,-.056f,0),reviewMaterials:new[]{material});
                filter.sharedMesh=baked;renderer.sharedMaterials=original;UnityEngine.Object.DestroyImmediate(material.mainTexture);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(isolated);
            }
            File.WriteAllText("work/reference-review/outer-hook-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            File.WriteAllLines("work/reference-review/outer-hook-sections.csv",rows);
        }

        static void ReviewSideCurlGuides(Vector3[] points,Vector2[] uv,System.Collections.Generic.Dictionary<int,int[]> faces) {
            var samples=new System.Collections.Generic.List<Landmark>();var outer=new System.Collections.Generic.List<Landmark>();var envelope=new System.Collections.Generic.List<Landmark>();var texture=GuideTexture(15);
            foreach(int component in new[]{8,11,12,14,15,16,120,140}) {
                var triangles=component==120?faces[12].Concat(faces[15]).ToArray():component==140?faces[14].Concat(faces[16]).ToArray():faces[component];
                var visible=triangles.Distinct().Where(i=>texture.GetPixelBilinear(uv[i].x,uv[i].y).a>=.5f).ToArray();
                Vector3 tip=visible.Select(i=>points[i]).OrderBy(p=>p.y).First();
                Vector3 root=visible.Select(i=>points[i]).OrderByDescending(p=>p.y).First();
                float previousPhase=float.NaN;
                int first=samples.Count;
                samples.Add(new Landmark{name="curl-"+component+"-root",position=root});
                float[] stations=component==8||component==11?new[]{.725f,.715f,.69f,.65f,.60f,.55f,.50f,.45f,.40f,.35f,.30f,.27f,.24f,.21f,.18f,.15f,.12f,tip.y+.003f}:new[]{.725f,.715f,.69f,.65f,.60f,.55f,.50f,.45f,.40f,.375f,.35f,.34f,.33f,.32f,.31f,.30f,.29f,.28f,.27f,.26f,.25f,.24f,.23f,.22f,.21f,.20f,.19f,.18f,.165f,.15f,.12f,tip.y+.003f};
                foreach(float y in stations.Where(y=>y>tip.y+.001f&&y<root.y-.001f).Distinct().OrderByDescending(y=>y)) {
                    var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,15,maxStep:.0004f);
                    if(cuts.Count<2)continue;
                    if(component>=12) {
                        var ring=PhaseAlignedCurlRing(cuts,ref previousPhase);
                        string key="curl-"+component+"-"+y.ToString("F5",System.Globalization.CultureInfo.InvariantCulture);
                        envelope.Add(new Landmark{name=key+"-minimum",position=new Vector3(cuts.Min(p=>p.x),y,cuts.Min(p=>p.z))});
                        envelope.Add(new Landmark{name=key+"-maximum",position=new Vector3(cuts.Max(p=>p.x),y,cuts.Max(p=>p.z))});
                        for(int col=0;col<16;col++)samples.Add(new Landmark{name="curl-"+component+"-ring-"+col,position=ring[col]});
                        continue;
                    }
                    Vector3 center=Vector3.zero;foreach(var q in cuts)center+=q/cuts.Count;
                    float xx=0,zz=0,xz=0;foreach(var q in cuts){var d=q-center;xx+=d.x*d.x;zz+=d.z*d.z;xz+=d.x*d.z;}
                    float angle=.5f*Mathf.Atan2(2*xz,xx-zz);Vector3 axis=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    Vector3 normal=new Vector3(axis.z,0,-axis.x);
                    if(Vector3.Dot(normal,new Vector3(center.x,0,center.z))<0){axis=-axis;normal=-normal;}
                    float lo=cuts.Min(q=>Vector3.Dot(q-center,axis)),hi=cuts.Max(q=>Vector3.Dot(q-center,axis));
                    for(int col=0;col<8;col++) {
                        int quarter=col<=4?col:8-col;float d=Mathf.Lerp(lo,hi,quarter/4f);
                        float nearest=cuts.Min(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d));
                        var band=cuts.Where(q=>Mathf.Abs(Vector3.Dot(q-center,axis)-d)<=nearest+.0002f);
                        float depth=col<=4?band.Max(q=>Vector3.Dot(q-center,normal)):band.Min(q=>Vector3.Dot(q-center,normal));
                        samples.Add(new Landmark{name="curl-"+component+"-ring-"+col,position=center+axis*d+normal*depth});
                    }
                }
                samples.Add(new Landmark{name="curl-"+component+"-tip",position=tip});
                if(component==8||component==11)outer.AddRange(samples.Skip(first));
            }
            File.WriteAllText("work/reference-review/outer-curl-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=outer.ToArray()},true));
            File.WriteAllText("work/reference-review/folded-curl-envelope-bounds.json",JsonUtility.ToJson(new FaceSectionReport{samples=envelope.ToArray()},true));
            File.WriteAllText("work/reference-review/folded-curl-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.Where(p=>!p.name.StartsWith("curl-8-")&&!p.name.StartsWith("curl-11-")).ToArray()},true));
        }

        static Vector3[] PhaseAlignedCurlRing(System.Collections.Generic.List<Vector3> cuts,ref float previousPhase) {
            // A nearly round section can swap PCA eigenvectors by 90 degrees.
            // Uniform angular rays with a continuous phase preserve a
            // consistent longitudinal correspondence across those stations.
            Func<Vector2,Vector2,float> cross=(a,b)=>a.x*b.y-a.y*b.x;
            var sorted=cuts.Select(p=>new Vector2(p.x,p.z)).Distinct().OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();
            var hull=new System.Collections.Generic.List<Vector2>();
            foreach(var p in sorted) {
                while(hull.Count>=2&&cross(hull[hull.Count-1]-hull[hull.Count-2],p-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);
                hull.Add(p);
            }
            int lower=hull.Count;
            for(int i=sorted.Length-2;i>=0;i--) {
                var p=sorted[i];while(hull.Count>lower&&cross(hull[hull.Count-1]-hull[hull.Count-2],p-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);
                hull.Add(p);
            }
            hull.RemoveAt(hull.Count-1);
            Vector2 center=new Vector2((sorted.Min(p=>p.x)+sorted.Max(p=>p.x))*.5f,(sorted.Min(p=>p.y)+sorted.Max(p=>p.y))*.5f);
            Vector3 mean=Vector3.zero;foreach(var q in cuts)mean+=q/cuts.Count;
            float xx=0,zz=0,xz=0;foreach(var q in cuts){var d=q-mean;xx+=d.x*d.x;zz+=d.z*d.z;xz+=d.x*d.z;}
            float phase=.5f*Mathf.Atan2(2*xz,xx-zz),step=Mathf.PI/8;
            if(!float.IsNaN(previousPhase))phase+=Mathf.Round((previousPhase-phase)/step)*step;
            previousPhase=phase;
            var ring=new Vector3[16];float y=cuts.Average(p=>p.y);
            for(int col=0;col<16;col++) {
                float angle=phase+col*Mathf.PI/8;Vector2 ray=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));float nearest=float.PositiveInfinity;
                for(int i=0;i<hull.Count;i++) {
                    Vector2 a=hull[i],edge=hull[(i+1)%hull.Count]-a;float det=cross(ray,edge);
                    if(Mathf.Abs(det)<1e-12f)continue;
                    float t=cross(a-center,edge)/det,u=cross(a-center,ray)/det;
                    if(t>=0&&u>=-.00001f&&u<=1.00001f)nearest=Mathf.Min(nearest,t);
                }
                if(float.IsInfinity(nearest))throw new InvalidOperationException("No visible curl envelope intersection");
                Vector2 p=center+ray*nearest;ring[col]=new Vector3(p.x,y,p.y);
            }
            return ring;
        }

        // Rear-facing lower ribbons use a stable left-to-right projection.
        // A changing PCA section axis can rotate/reverse at the curled tip.
        static void ReviewLowerRearGuides(Vector3[] points,Vector2[] uv,int[] triangles,int component) {
            var used=triangles.Distinct().ToArray();float bottom=used.Min(i=>points[i].y);
            var samples=new System.Collections.Generic.List<Landmark>();
            System.Collections.Generic.List<Vector3> first=null;float tipY=bottom;
            for(int step=1;step<=200;step++) {
                tipY=bottom+step*.00005f;first=VisibleSection(points,uv,triangles,Vector3.up,tipY,15,maxStep:.0002f);
                if(first.Count>0)break;
            }
            if(first==null||first.Count==0)throw new InvalidOperationException("No alpha-visible lower tip "+component);
            var texture=GuideTexture(15);
            foreach(float y in new[]{.25f,.23f,.21f,.19f,.17f,.155f,.14f,.125f,.11f,.095f,.085f,tipY+.004f,tipY+.001f,tipY}.Where(y=>y>=tipY).Distinct().OrderByDescending(y=>y)) {
                var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,15,maxStep:.0002f);if(cuts.Count==0)continue;
                float lo=cuts.Min(q=>q.x),hi=cuts.Max(q=>q.x);
                for(int col=0;col<5;col++) {
                    float x=Mathf.Lerp(lo+.0000001f,hi-.0000001f,col/4f),z=float.PositiveInfinity;
                    for(int i=0;i<triangles.Length;i+=3) {
                        int ai=triangles[i],bi=triangles[i+1],ci=triangles[i+2];Vector3 a=points[ai],ab=points[bi]-a,ac=points[ci]-a;
                        float det=ab.x*ac.y-ab.y*ac.x;if(Mathf.Abs(det)<1e-12f)continue;
                        float dx=x-a.x,dy=y-a.y,u=(dx*ac.y-dy*ac.x)/det,v=(ab.x*dy-ab.y*dx)/det;
                        if(u<-.000001f||v<-.000001f||u+v>1.000001f)continue;
                        Vector2 st=uv[ai]+(uv[bi]-uv[ai])*u+(uv[ci]-uv[ai])*v;if(texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                        z=Mathf.Min(z,(a+ab*u+ac*v).z);
                    }
                    if(float.IsInfinity(z)) {float nearest=cuts.Min(q=>Mathf.Abs(q.x-x));z=cuts.Where(q=>Mathf.Abs(q.x-x)<=nearest+.0001f).Min(q=>q.z);}
                    samples.Add(new Landmark{name="lower-"+component+"-col-"+col,position=new Vector3(x,y,z)});
                }
            }
            File.WriteAllText("work/reference-review/lower-rear-"+component+".json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            var intervals=new System.Collections.Generic.List<Landmark>();
            for(int row=0;row<=246;row++) {
                float registered=row<=60?.50f-row*.005f:.195f-(row-61)*.00025f;
                float y=(registered-.07f)/1.25f;if(y<tipY)break;
                var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,15,maxStep:.00008f).OrderBy(q=>q.x).ToArray();
                var bands=new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
                foreach(var q in cuts){if(bands.Count==0||q.x-bands.Last().Last().x>.0002f)bands.Add(new System.Collections.Generic.List<Vector3>());bands.Last().Add(q);}
                int band=0;
                foreach(var region in bands.Where(g=>g.Last().x-g.First().x>.00008f)) {
                    float lo=region.First().x,hi=region.Last().x;
                    for(int col=0;col<5;col++) {
                        float x=Mathf.Lerp(lo,hi,col/4f),near=region.Min(q=>Mathf.Abs(q.x-x));
                        var point=region.Where(q=>Mathf.Abs(q.x-x)<=near+.00005f).OrderBy(q=>q.z).First();point.x=x;point.y=y;
                        intervals.Add(new Landmark{name="outer-"+row+"-"+band+"-"+col,position=point});
                    }
                    band++;
                }
            }
            File.WriteAllText("work/reference-review/outer-intervals-"+component+".json",JsonUtility.ToJson(new FaceSectionReport{samples=intervals.ToArray()},true));
        }

        static void ReviewInnerRearGuides(Vector3[] points,Vector2[] uv,int[] triangles,int component) {
            const int slot=16;var used=triangles.Distinct().ToArray();float bottom=used.Min(i=>points[i].y);
            System.Collections.Generic.List<Vector3> first=null;float tipY=bottom;
            for(int step=1;step<=200;step++) {
                tipY=bottom+step*.00005f;first=VisibleSection(points,uv,triangles,Vector3.up,tipY,slot,maxStep:.0001f);
                if(first.Count>0)break;
            }
            if(first==null||first.Count==0)throw new InvalidOperationException("No alpha-visible inner rear tip "+component);
            var samples=new System.Collections.Generic.List<Landmark>();var texture=GuideTexture(slot);
            var heights=new[]{.420f,.395f,.370f,.345f,.320f,.295f,.270f,.245f,.225f,.210f,.195f,.180f,.170f,.160f}
                .Select(y=>(y-.07f)/1.25f).Concat(new[]{tipY+.004f,tipY+.001f,tipY})
                .Where(y=>y>=tipY).Distinct().OrderByDescending(y=>y);
            foreach(float y in heights) {
                var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,slot,maxStep:.0001f);if(cuts.Count==0)continue;
                float lo=cuts.Min(q=>q.x),hi=cuts.Max(q=>q.x);
                for(int col=0;col<5;col++) {
                    float x=Mathf.Lerp(lo+.0000001f,hi-.0000001f,col/4f),z=float.PositiveInfinity;
                    for(int k=0;k<triangles.Length;k+=3) {
                        int ai=triangles[k],bi=triangles[k+1],ci=triangles[k+2];Vector3 a=points[ai],ab=points[bi]-a,ac=points[ci]-a;
                        float det=ab.x*ac.y-ab.y*ac.x;if(Mathf.Abs(det)<1e-12f)continue;
                        float dx=x-a.x,dy=y-a.y,u=(dx*ac.y-dy*ac.x)/det,v=(ab.x*dy-ab.y*dx)/det;
                        if(u<-.000001f||v<-.000001f||u+v>1.000001f)continue;
                        Vector2 st=uv[ai]+(uv[bi]-uv[ai])*u+(uv[ci]-uv[ai])*v;if(texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                        z=Mathf.Min(z,(a+ab*u+ac*v).z);
                    }
                    if(float.IsInfinity(z)){float nearest=cuts.Min(q=>Mathf.Abs(q.x-x));z=cuts.Where(q=>Mathf.Abs(q.x-x)<=nearest+.0001f).Min(q=>q.z);}
                    samples.Add(new Landmark{name="inner-"+component+"-col-"+col,position=new Vector3(x,y,z)});
                }
            }
            File.WriteAllText("work/reference-review/inner-rear-"+component+".json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            if(component==0) {
                var fork=new System.Collections.Generic.List<Landmark>();
                for(int row=0;row<=330;row++) {
                    float registered=row<=124?.82f-row*.005f:.195f-(row-125)*.00025f;
                    float y=(registered-.07f)/1.25f;if(y<tipY)break;
                    var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,slot,maxStep:.00008f).OrderBy(q=>q.x).ToArray();
                    var bands=new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
                    foreach(var q in cuts){if(bands.Count==0||q.x-bands.Last().Last().x>.0002f)bands.Add(new System.Collections.Generic.List<Vector3>());bands.Last().Add(q);}
                    int band=0;
                    foreach(var region in bands.Where(g=>g.Last().x-g.First().x>.00008f)) {
                        float lo=region.First().x,hi=region.Last().x;
                        for(int col=0;col<5;col++) {
                            float x=Mathf.Lerp(lo,hi,col/4f),near=region.Min(q=>Mathf.Abs(q.x-x));
                            var point=region.Where(q=>Mathf.Abs(q.x-x)<=near+.00005f).OrderBy(q=>q.z).First();point.x=x;point.y=y;
                            fork.Add(new Landmark{name="fork-"+row+"-"+band+"-"+col,position=point});
                        }
                        band++;
                    }
                }
                File.WriteAllText("work/reference-review/inner-rear-0-fork-scan.json",JsonUtility.ToJson(new FaceSectionReport{samples=fork.ToArray()},true));
            }
        }

        // Sparse color-boundary heights, not copied reference pixels or UVs.
        static void ReviewRearPaint(Mesh baked,Transform snapshot) {
            var p=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;
            var rows=new System.Collections.Generic.List<Landmark>();
            for(int column=0;column<=12;column++) {
                float x=Mathf.Lerp(-.24f,.24f,column/12f),top=float.NegativeInfinity;
                Vector3 hit=Vector3.zero;
                for(int row=0;row<=80;row++) {
                    float y=.065f+row*.004f,nearest=float.PositiveInfinity;Color paint=Color.clear;
                    Vector3 point=Vector3.zero;
                    foreach(int slot in new[]{15,16}) {
                        var triangles=baked.GetTriangles(slot);var texture=GuideTexture(slot);
                        for(int i=0;i<triangles.Length;i+=3) {
                            int ai=triangles[i],bi=triangles[i+1],ci=triangles[i+2];
                            Vector3 a=p[ai],ab=p[bi]-a,ac=p[ci]-a;
                            float det=ab.x*ac.y-ab.y*ac.x;if(Mathf.Abs(det)<1e-10f)continue;
                            float dx=x-a.x,dy=y-a.y;
                            float u=(dx*ac.y-dy*ac.x)/det,v=(ab.x*dy-ab.y*dx)/det;
                            if(u<0||v<0||u+v>1)continue;
                            Vector3 q=a+ab*u+ac*v;if(q.z>=nearest)continue;
                            Vector2 st=uv[ai]+(uv[bi]-uv[ai])*u+(uv[ci]-uv[ai])*v;
                            Color c=texture.GetPixelBilinear(st.x,st.y);if(c.a<.5f)continue;
                            nearest=q.z;paint=c;point=q;
                        }
                    }
                    if(paint.a>.5f&&paint.g>paint.b*.62f&&paint.g>.25f) {top=y;hit=point;}
                }
                if(!float.IsNegativeInfinity(top))rows.Add(new Landmark{name="cyan-upper-"+column,position=hit});
            }
            File.WriteAllText("work/reference-review/rear-paint-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=rows.ToArray()},true));
        }

        // Sparse visible color-transition positions only; no source texels or UVs leave the local review.
        static void ReviewRearPaintEdges(Mesh baked,Transform snapshot) {
            var p=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;
            var rows=new System.Collections.Generic.List<Landmark>();
            var triangles=new[]{15,16}.Select(slot=>baked.GetTriangles(slot)).ToArray();
            for(int column=-20;column<=180;column++) {
                float x=Mathf.LerpUnclamped(-.25f,.25f,column/160f);
                var cyan=new System.Collections.Generic.List<Vector3>();
                for(int row=0;row<=180;row++) {
                    float y=.075f+row*.0015f,nearest=float.PositiveInfinity;Color paint=Color.clear;Vector3 hit=Vector3.zero;
                    for(int part=0;part<2;part++) {
                        var texture=GuideTexture(part+15);var faces=triangles[part];
                        for(int i=0;i<faces.Length;i+=3) {
                            int ai=faces[i],bi=faces[i+1],ci=faces[i+2];Vector3 a=p[ai],ab=p[bi]-a,ac=p[ci]-a;
                            float det=ab.x*ac.y-ab.y*ac.x;if(Mathf.Abs(det)<1e-10f)continue;
                            float dx=x-a.x,dy=y-a.y,u=(dx*ac.y-dy*ac.x)/det,v=(ab.x*dy-ab.y*dx)/det;
                            if(u<0||v<0||u+v>1)continue;
                            Vector3 q=a+ab*u+ac*v;if(q.z>=nearest)continue;
                            Vector2 st=uv[ai]+(uv[bi]-uv[ai])*u+(uv[ci]-uv[ai])*v;Color c=texture.GetPixelBilinear(st.x,st.y);
                            if(c.a<.5f)continue;nearest=q.z;paint=c;hit=q;
                        }
                    }
                    if(paint.a>=.5f&&paint.g>paint.b*.62f&&paint.g>.25f)cyan.Add(hit);
                }
                if(cyan.Count==0)continue;
                rows.Add(new Landmark{name="edge-"+column,position=cyan.OrderByDescending(q=>q.y).First()});
                // Longest contiguous cyan interval identifies the broad lower mass, ignoring isolated thin upper brushes.
                var runs=new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
                foreach(var q in cyan.OrderBy(q=>q.y)) {if(runs.Count==0||q.y-runs.Last().Last().y>.002f)runs.Add(new System.Collections.Generic.List<Vector3>());runs.Last().Add(q);}
                rows.Add(new Landmark{name="mass-"+column,position=runs.OrderByDescending(g=>g.Count).First().OrderByDescending(q=>q.y).First()});
            }
            File.WriteAllText("work/reference-review/rear-paint-edges.json",JsonUtility.ToJson(new FaceSectionReport{samples=rows.ToArray()},true));
        }

        static void ReviewApronDetails(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var texture=GuideTexture(37);var samples=new System.Collections.Generic.List<Landmark>();
            foreach(float y in new[]{.135f,.140f,.150f,.165f,.180f,.200f,.220f,.240f,.250f,.260f,.264f}) {
                var cuts=VisibleSection(points,baked.uv,baked.GetTriangles(37),Vector3.up,y,37,
                    (p,st)=>st.y<.5f&&p.z>=.04f,.00025f);
                if(cuts.Count==0)continue;
                string label="visible/"+y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
                samples.Add(new Landmark{name=label+"/left",position=cuts.OrderBy(p=>p.x).First()});
                samples.Add(new Landmark{name=label+"/right",position=cuts.OrderByDescending(p=>p.x).First()});
            }
            // Sparse visible motif/edge probes picked in the fixed frontal study.
            // Intersect the actual surface and reject transparent texels; keep only
            // the landmark coordinate, never the source triangles or image pixels.
            var probes=new[]{
                new Vector2(219,320),new Vector2(312,242),new Vector2(380,290),new Vector2(374,363),new Vector2(300,389),
                new Vector2(240,352),new Vector2(288,344),new Vector2(340,380),new Vector2(279,385),
                new Vector2(470,266),new Vector2(418,256),new Vector2(435,311),
                new Vector2(465,402),new Vector2(521,331),new Vector2(531,374),new Vector2(496,389),
                new Vector2(474,354),new Vector2(451,362),new Vector2(331,208),new Vector2(358,219),
                new Vector2(170,236),new Vector2(179,424),new Vector2(360,490),new Vector2(550,417),new Vector2(550,245),
                new Vector2(240,270),new Vector2(361,261),new Vector2(224,355),new Vector2(253,379),
                new Vector2(274,318),new Vector2(231,353),new Vector2(272,383)
            };
            var names=new[]{"whale-head","whale-crown","whale-shoulder","whale-flank","whale-bottom",
                "belly-start","belly-upper","belly-end","belly-bottom","tail-tip","tail-root","tail-turn",
                "boat-left","boat-top","boat-right","boat-notch","spray-0","spray-1","spout-0","spout-1",
                "edge-left-upper","edge-left-lower","edge-bottom","edge-right-lower","edge-right-upper","whale-upper-left","whale-upper-right","whale-lower-left","whale-lower-arc","eye","belly-left","belly-lower"};
            Vector3 focus=new Vector3(0,.315f,.22f)*.8f+new Vector3(0,-.056f,0);
            Vector3 view=new Vector3(0,.24f,3).normalized,up=new Vector3(0,view.z,-view.y);
            var triangles=baked.GetTriangles(37);var uv=baked.uv;
            for(int probe=0;probe<probes.Length;probe++) {
                Vector2 pixel=probes[probe];Vector3 origin=focus+Vector3.left*((pixel.x/720-.5f)*.224f)+up*((.5f-pixel.y/720)*.224f)+view*2;
                Vector3 direction=-view,hit=Vector3.zero;float nearest=float.PositiveInfinity;
                for(int k=0;k<triangles.Length;k+=3) {
                    int ai=triangles[k],bi=triangles[k+1],ci=triangles[k+2];Vector3 a=points[ai],ab=points[bi]-a,ac=points[ci]-a;
                    Vector3 h=Vector3.Cross(direction,ac);float det=Vector3.Dot(ab,h);if(Mathf.Abs(det)<1e-9f)continue;
                    Vector3 rel=origin-a;float u=Vector3.Dot(rel,h)/det;if(u<0||u>1)continue;
                    Vector3 q=Vector3.Cross(rel,ab);float v=Vector3.Dot(direction,q)/det;if(v<0||u+v>1)continue;
                    float distance=Vector3.Dot(ac,q)/det;if(distance<0||distance>=nearest)continue;
                    Vector2 st=uv[ai]+(uv[bi]-uv[ai])*u+(uv[ci]-uv[ai])*v;
                    if(st.y>=.5f||texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                    nearest=distance;hit=origin+direction*distance;
                }
                if(float.IsPositiveInfinity(nearest))throw new InvalidOperationException("Apron probe missed "+names[probe]);
                samples.Add(new Landmark{name="motif/"+names[probe],position=hit});
            }
            File.WriteAllText("work/reference-review/apron-edge-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
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
            section("apron",37,new[]{.12f,.13f,.14f,.15f,.16f,.18f,.20f,.22f,.24f,.26f,.264f,.27f,.28f,.29f},-.2f,.2f,.01f);
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
            var probes=new[]{new Vector3(230,345,35),new Vector3(230,380,35),new Vector3(225,430,35),new Vector3(238,490,35),
                new Vector3(228,315,0),new Vector3(224,335,0),new Vector3(492,315,0),new Vector3(498,340,0),
                new Vector3(360,150,0),new Vector3(330,165,0),new Vector3(390,165,0)};
            foreach(Vector3 probe in probes) {
                Vector2 pixel=new Vector2(probe.x,probe.y);
                var focus=new Vector3(0,.56f,.048f);
                Vector3 offset=Quaternion.Euler(0,probe.z,0)*new Vector3(0,.24f,3)*.8f;
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
                rays.Add(new Landmark{name="view-"+probe.z+"-pixel-"+pixel.x+"-"+pixel.y+"-slot-"+bestSlot+"-component-"+component,position=hit});
            }
            File.WriteAllText("work/reference-review/fringe-coverage-rays.json",JsonUtility.ToJson(new FaceSectionReport{samples=rays.ToArray()},true));
        }

        // Sample the visible outer dome with sparse radial probes. This keeps
        // rear/top curvature distinct from the height-only envelope bounds.
        static void ReviewRearCrown(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;
            var samples=new System.Collections.Generic.List<Landmark>();
            Vector3 center=new Vector3(0,.56f,-.04f);
            foreach(int elevation in new[]{0,15,30,45,60,75,90})foreach(int angle in Enumerable.Range(0,18).Select(i=>i*20)) {
                float phi=elevation*Mathf.Deg2Rad,theta=angle*Mathf.Deg2Rad;
                Vector3 direction=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Sin(phi),Mathf.Cos(theta)*Mathf.Cos(phi));
                float farthest=-1;
                foreach(int slot in new[]{10,11,15,16}) {
                    var triangles=baked.GetTriangles(slot);var texture=GuideTexture(slot);
                    for(int i=0;i<triangles.Length;i+=3) {
                        int ai=triangles[i],bi=triangles[i+1],ci=triangles[i+2];
                        Vector3 a=points[ai],ab=points[bi]-a,ac=points[ci]-a,h=Vector3.Cross(direction,ac);
                        float det=Vector3.Dot(ab,h);if(Mathf.Abs(det)<1e-9f)continue;
                        Vector3 delta=center-a;float u=Vector3.Dot(delta,h)/det;if(u<0||u>1)continue;
                        Vector3 q=Vector3.Cross(delta,ab);float v=Vector3.Dot(direction,q)/det;if(v<0||u+v>1)continue;
                        float distance=Vector3.Dot(ac,q)/det;if(distance<=farthest||distance<=0)continue;
                        Vector2 st=uv[ai]*(1-u-v)+uv[bi]*u+uv[ci]*v;
                        if(texture&&texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                        farthest=distance;
                    }
                }
                if(farthest<0){Debug.Log("Missing rear dome probe "+elevation+"/"+angle);continue;}
                samples.Add(new Landmark{name=elevation+"/"+angle,position=center+direction*farthest});
            }
            File.WriteAllText("work/reference-review/rear-crown-rays.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }

        static void ReviewCommonSurfaces(Mesh baked,GameObject snapshot,Camera camera) {
            var filter=snapshot.GetComponent<MeshFilter>();var renderer=snapshot.GetComponent<MeshRenderer>();
            var original=renderer.sharedMaterials;
            for(int mode=0;mode<4;mode++) {
                string name=mode==0?"clay-lit":mode==1?"paint-flat":mode==2?"albedo-flat":"toon-no-outline";
                var mesh=UnityEngine.Object.Instantiate(baked);
                // Ref's visual color lives in material textures, not vertex tint.
                mesh.colors=Enumerable.Repeat(mode==0?new Color(.65f,.65f,.65f,1):Color.white,mesh.vertexCount).ToArray();
                var materials=new Material[original.Length];
                for(int slot=0;slot<materials.Length;slot++) {
                    var material=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));
                    material.mainTexture=mode==0?FishRebuiltModel.DiagnosticAlpha(original[slot].mainTexture):original[slot].mainTexture;
                    material.SetFloat("_Flat",mode==0||mode==3?0:1);material.SetFloat("_OutlineWidth",0);materials[slot]=material;
                }
                filter.sharedMesh=mesh;renderer.sharedMaterials=materials;
                FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/surface-"+name,.8f,new Vector3(0,-.056f,0),reviewMaterials:mode==0?materials:null);
                filter.sharedMesh=baked;renderer.sharedMaterials=original;
                foreach(var material in materials){if(mode==0&&material.mainTexture)UnityEngine.Object.DestroyImmediate(material.mainTexture);UnityEngine.Object.DestroyImmediate(material);}
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        static void ReviewOuterCurtainClay(Mesh baked,GameObject snapshot,Camera camera) {
            var points=baked.vertices.Select(snapshot.transform.TransformPoint).ToArray();var tri=baked.GetTriangles(15);var used=tri.Distinct().ToArray();
            int[] parents=Enumerable.Range(0,points.Length).ToArray();Func<int,int> root=null;root=i=>parents[i]==i?i:parents[i]=root(parents[i]);
            for(int k=0;k<tri.Length;k+=3){parents[root(tri[k+1])]=root(tri[k]);parents[root(tri[k+2])]=root(tri[k]);}
            foreach(var seam in used.GroupBy(i=>Vector3Int.RoundToInt(points[i]*100000))){int first=seam.First();foreach(int i in seam)parents[root(i)]=root(first);}
            var groups=used.GroupBy(root).OrderByDescending(g=>g.Count()).ToArray();var members=new System.Collections.Generic.HashSet<int>(new[]{3,7,10,6,2}.SelectMany(c=>groups[c]));
            var selected=new System.Collections.Generic.List<int>();for(int k=0;k<tri.Length;k+=3)if(members.Contains(tri[k]))selected.AddRange(new[]{tri[k],tri[k+1],tri[k+2]});
            var filter=snapshot.GetComponent<MeshFilter>();var renderer=snapshot.GetComponent<MeshRenderer>();var original=renderer.sharedMaterials;
            var clay=UnityEngine.Object.Instantiate(baked);for(int slot=0;slot<clay.subMeshCount;slot++)clay.SetTriangles(slot==15?selected.ToArray():Array.Empty<int>(),slot,false);
            clay.colors=Enumerable.Repeat(new Color(.65f,.65f,.65f,1),clay.vertexCount).ToArray();
            var material=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));material.mainTexture=FishRebuiltModel.DiagnosticAlpha(GuideTexture(15));material.SetFloat("_OutlineWidth",0);
            filter.sharedMesh=clay;renderer.sharedMaterials=Enumerable.Repeat(material,original.Length).ToArray();
            FishRebuiltModel.RenderBodyStudy(camera,"work/reference-review/outer-clay-total",.8f,new Vector3(0,-.056f,0));
            filter.sharedMesh=baked;renderer.sharedMaterials=original;UnityEngine.Object.DestroyImmediate(clay);UnityEngine.Object.DestroyImmediate(material.mainTexture);UnityEngine.Object.DestroyImmediate(material);
        }

        static void ReviewFrontPaint(Mesh baked,GameObject snapshot,Camera camera) {
            var points=baked.vertices.Select(snapshot.transform.TransformPoint).ToArray();var uv=baked.uv;
            int[] slots={10,10,10,10,10,10,10,11,11,11,11,11,11,11,11,11,11};
            int[] components={0,3,4,6,8,10,13,8,4,6,7,9,10,11,12,0,2};
            var samples=new System.Collections.Generic.List<Landmark>();
            var rootSamples=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{10,11}) {
                var triangles=baked.GetTriangles(slot);var used=triangles.Distinct().ToArray();
                var parents=Enumerable.Range(0,points.Length).ToArray();Func<int,int> root=null;
                root=i=>parents[i]==i?i:parents[i]=root(parents[i]);
                for(int k=0;k<triangles.Length;k+=3){parents[root(triangles[k+1])]=root(triangles[k]);parents[root(triangles[k+2])]=root(triangles[k]);}
                foreach(var seam in used.GroupBy(i=>Vector3Int.RoundToInt(points[i]*100000))) {
                    int first=seam.First();foreach(int i in seam)parents[root(i)]=root(first);
                }
                var groups=used.GroupBy(root).OrderByDescending(g=>g.Count()).ToArray();var texture=GuideTexture(slot);
                foreach(int panel in Enumerable.Range(0,17).Where(i=>slots[i]==slot)) {
                    var members=new System.Collections.Generic.HashSet<int>(groups[components[panel]]);var selected=new System.Collections.Generic.List<int>();
                    for(int k=0;k<triangles.Length;k+=3)if(members.Contains(triangles[k]))selected.AddRange(new[]{triangles[k],triangles[k+1],triangles[k+2]});
                    // Near the root, height sections open rapidly around the crown.
                    // Read only sparse section positions, never source indices/UVs.
                    float top=groups[components[panel]].Max(i=>points[i].y)*1.25f+.07f;
                    var rootHeights=new[]{top-.0005f,top-.0015f,top-.003f,top-.006f,top-.010f,top-.016f,top-.024f,top-.036f,top-.050f,.920f}
                        .Where(y=>y>.9199f&&y<top).Distinct().OrderByDescending(y=>y).ToArray();
                    foreach(float height in rootHeights) {
                        float rawY=(height-.07f)/1.25f;
                        var cuts=VisibleSection(points,uv,selected.ToArray(),Vector3.up,rawY,slot,maxStep:.0001f);
                        if(cuts.Count<2)continue;
                        float lo=cuts.Min(q=>q.x),hi=cuts.Max(q=>q.x);
                        for(int col=0;col<5;col++) {
                            float x=Mathf.Lerp(lo,hi,col/4f),nearest=cuts.Min(q=>Mathf.Abs(q.x-x));
                            Vector3 q=cuts.Where(q=>Mathf.Abs(q.x-x)<=nearest+.00008f).OrderByDescending(q=>q.z).First();q.x=x;q.y=rawY;
                            rootSamples.Add(new Landmark{name="front-root-"+panel+"-"+col,position=q});
                        }
                    }
                    var bright=new System.Collections.Generic.List<Vector3>();
                    for(float registeredY=.80f;registeredY<=.925f;registeredY+=.0015f) {
                        float y=(registeredY-.07f)/1.25f;
                        bright.AddRange(VisibleSection(points,uv,selected.ToArray(),Vector3.up,y,slot,(p,st)=>{
                            Color c=texture.GetPixelBilinear(st.x,st.y);
                            return p.z>0&&c.r>.34f&&c.g>.44f&&c.b>.56f&&c.g/c.b>.60f;
                        },maxStep:.0002f));
                    }
                    var cells=bright.GroupBy(p=>new Vector2Int(Mathf.FloorToInt(p.x/.0006f),Mathf.FloorToInt(p.y/.0013f))).ToDictionary(g=>g.Key,g=>g.ToArray());
                    var remaining=new System.Collections.Generic.HashSet<Vector2Int>(cells.Keys);int island=0;
                    while(remaining.Count>0) {
                        var queue=new System.Collections.Generic.Queue<Vector2Int>();var region=new System.Collections.Generic.List<Vector3>();
                        var first=remaining.OrderBy(q=>q.x).ThenBy(q=>q.y).First();queue.Enqueue(first);remaining.Remove(first);
                        while(queue.Count>0) {
                            var cell=queue.Dequeue();region.AddRange(cells[cell]);
                            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++) {
                                var next=cell+new Vector2Int(dx,dy);if(remaining.Remove(next))queue.Enqueue(next);
                            }
                        }
                        float x0=region.Min(p=>p.x),x1=region.Max(p=>p.x),y0=region.Min(p=>p.y),y1=region.Max(p=>p.y);
                        if(region.Count<5||x1-x0<.0008f||y1-y0<.0008f)continue;
                        Vector3 center=new Vector3((x0+x1)*.5f,(y0+y1)*.5f,region.Max(p=>p.z));
                        float xx=0,yy=0,xy=0;foreach(var p in region){Vector3 d=p-center;xx+=d.x*d.x;yy+=d.y*d.y;xy+=d.x*d.y;}
                        float angle=.5f*Mathf.Atan2(2*xy,xx-yy);Vector3 axis=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0),cross=new Vector3(-axis.y,axis.x,0);
                        float a0=region.Min(p=>Vector3.Dot(p-center,axis)),a1=region.Max(p=>Vector3.Dot(p-center,axis));
                        float b0=region.Min(p=>Vector3.Dot(p-center,cross)),b1=region.Max(p=>Vector3.Dot(p-center,cross));
                        center+=axis*((a0+a1)*.5f)+cross*((b0+b1)*.5f);float ra=(a1-a0)*.5f,rb=(b1-b0)*.5f;
                        // Box extrema plus projected PCA extent endpoints, only positions.
                        foreach(var q in new[]{center,new Vector3(x0,center.y,center.z),new Vector3(x1,center.y,center.z),new Vector3(center.x,y0,center.z),new Vector3(center.x,y1,center.z),center-axis*ra,center+axis*ra,center-cross*rb,center+cross*rb})
                            samples.Add(new Landmark{name="front-glint-"+panel+"-"+island,position=q});island++;
                    }
                }
            }
            File.WriteAllText("work/reference-review/front-root-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=rootSamples.ToArray()},true));
            File.WriteAllText("work/reference-review/front-glint-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            var filter=snapshot.GetComponent<MeshFilter>();var renderer=snapshot.GetComponent<MeshRenderer>();var original=renderer.sharedMaterials;
            for(int mode=0;mode<2;mode++) {
                var mesh=UnityEngine.Object.Instantiate(baked);
                for(int slot=0;slot<mesh.subMeshCount;slot++)if(slot!=10&&slot!=11)mesh.SetTriangles(Array.Empty<int>(),slot,false);
                mesh.colors=Enumerable.Repeat(mode==0?new Color(.65f,.65f,.65f,1):Color.white,mesh.vertexCount).ToArray();
                var materials=original.Select(m=>{var n=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));n.mainTexture=mode==0?Texture2D.whiteTexture:m.mainTexture;n.SetFloat("_Flat",mode);n.SetFloat("_OutlineWidth",0);return n;}).ToArray();
                filter.sharedMesh=mesh;renderer.sharedMaterials=materials;
                FishRebuiltModel.RenderHeadStudy(camera,"work/reference-review/front-"+(mode==0?"clay":"paint-flat"),.8f,new Vector3(0,-.056f,0));
                filter.sharedMesh=baked;renderer.sharedMaterials=original;UnityEngine.Object.DestroyImmediate(mesh);foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);
            }
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

        static void ReviewHemGuides(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{34,35})for(int i=0;i<24;i++) {
                float theta=i*Mathf.PI*2/24;var radial=new Vector3(Mathf.Sin(theta),0,Mathf.Cos(theta));
                var normal=new Vector3(Mathf.Cos(theta),0,-Mathf.Sin(theta));
                var cuts=VisibleSection(points,baked.uv,baked.GetTriangles(slot),normal,0,slot,
                    (p,st)=>Vector3.Dot(p,radial)>.04f&&p.y<.21f);
                if(cuts.Count==0)continue;
                samples.Add(new Landmark{name=slot+"/edge/"+i,position=cuts.OrderBy(p=>p.y).First()});
                foreach(float y in new[]{.045f,.055f,.065f,.075f,.085f,.095f,.11f,.14f,.18f}) {
                    var near=cuts.Where(p=>Mathf.Abs(p.y-y)<.0015f).ToArray();
                    if(near.Length==0)continue;
                    var q=near.OrderByDescending(p=>Vector3.Dot(p,radial)).First();q.y=y;
                    samples.Add(new Landmark{name=slot+"/y/"+y.ToString("F3")+"/"+i,position=q});
                }
            }
            File.WriteAllText("work/reference-review/hem-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }
        static void ReviewFootGuides(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{26,27,28}) {
                foreach(float z in new[]{-.064f,-.058f,-.045f,-.025f,0,.025f,.045f,.065f,.080f,.092f,.101f}) {
                    var cuts=VisibleSection(points,baked.uv,baked.GetTriangles(slot),Vector3.forward,z,slot,(p,st)=>p.x>0);
                    if(cuts.Count<3)continue;
                    float lo=cuts.Min(p=>p.x),hi=cuts.Max(p=>p.x);
                    for(int row=0;row<=8;row++) {
                        float x=Mathf.Lerp(lo,hi,row/8f);var near=cuts.Where(p=>Mathf.Abs(p.x-x)<.0008f).ToArray();
                        if(near.Length==0)near=new[]{cuts.OrderBy(p=>Mathf.Abs(p.x-x)).First()};
                        samples.Add(new Landmark{name=slot+"/z/"+z.ToString("F3")+"/top/"+row,position=new Vector3(x,near.Max(p=>p.y),z)});
                        samples.Add(new Landmark{name=slot+"/z/"+z.ToString("F3")+"/bottom/"+row,position=new Vector3(x,near.Min(p=>p.y),z)});
                    }
                }
                foreach(float y in new[]{.01f,.02f,.03f,.04f,.05f,.06f,.07f,.08f}) {
                    var cuts=VisibleSection(points,baked.uv,baked.GetTriangles(slot),Vector3.up,y,slot,(p,st)=>p.x>0);
                    if(cuts.Count<3)continue;
                    float cx=(cuts.Min(p=>p.x)+cuts.Max(p=>p.x))*.5f,cz=(cuts.Min(p=>p.z)+cuts.Max(p=>p.z))*.5f;
                    for(int i=0;i<16;i++) {
                        float angle=i*Mathf.PI*2/16;
                        var p=cuts.OrderBy(q=>Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(q.z-cz,q.x-cx)*Mathf.Rad2Deg,angle*Mathf.Rad2Deg))).First();
                        samples.Add(new Landmark{name=slot+"/y/"+y.ToString("F3")+"/"+i,position=p});
                    }
                }
            }
            // Weld positional seams only for local boundary analysis. Keep a
            // sparse angular sample; never reuse reference indices in output.
            foreach(int slot in new[]{26,27,28}) {
                var faces=baked.GetTriangles(slot);var lookup=new System.Collections.Generic.Dictionary<Vector3Int,int>();
                var welded=new System.Collections.Generic.Dictionary<int,int>();
                foreach(int id in faces.Distinct()) {
                    Vector3 p=points[id];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                    if(!lookup.TryGetValue(key,out int index)){index=id;lookup[key]=index;}welded[id]=index;
                }
                var edges=new System.Collections.Generic.Dictionary<(int,int),int>();
                for(int j=0;j<faces.Length;j+=3)for(int k=0;k<3;k++) {
                    int a=welded[faces[j+k]],b=welded[faces[j+(k+1)%3]];var key=(Mathf.Min(a,b),Mathf.Max(a,b));
                    edges.TryGetValue(key,out int count);edges[key]=count+1;
                }
                var boundary=edges.Where(e=>e.Value==1).SelectMany(e=>new[]{points[e.Key.Item1],points[e.Key.Item2]}).Where(p=>p.x>0&&p.y>.025f).Distinct().ToArray();
                Debug.Log("Foot boundary "+slot+": "+boundary.Length);
                var center=new Vector3(.055f,0,-.012f);
                for(int k=0;k<16&&boundary.Length>0;k++) {
                    float angle=k*Mathf.PI*2/16;Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)/.6f).normalized;
                    Vector3 normal=Vector3.Cross(direction,Vector3.up);var hits=new System.Collections.Generic.List<Vector3>();
                    foreach(var edge in edges.Where(e=>e.Value==1)) {
                        Vector3 a=points[edge.Key.Item1],b=points[edge.Key.Item2];
                        if(a.x<=0||b.x<=0||a.y<.025f||b.y<.025f)continue;
                        float da=Vector3.Dot(a-center,normal),db=Vector3.Dot(b-center,normal);
                        if((da<=0&&db>0)||(db<=0&&da>0)) {
                            Vector3 p=Vector3.Lerp(a,b,da/(da-db));
                            if(Vector3.Dot(p-center,direction)>0)hits.Add(p);
                        }
                    }
                    if(hits.Count>0)samples.Add(new Landmark{name=slot+"/opening/"+k,position=hits.OrderByDescending(q=>(q-new Vector3(center.x,q.y,center.z)).sqrMagnitude).First()});
                }
            }
            for(int k=0;k<16;k++) {
                float angle=k*Mathf.PI*2/16;Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)/.6f).normalized;
                Vector3 center=new Vector3(.055f,0,-.012f),normal=Vector3.Cross(direction,Vector3.up);
                var cut=VisibleSection(points,baked.uv,baked.GetTriangles(26),normal,Vector3.Dot(center,normal),26,
                    (p,st)=>p.x>0&&Vector3.Dot(p-center,direction)>0&&!(p.z>.03f&&p.z<.058f&&Mathf.Abs(p.x-.055f)<.025f));
                if(cut.Count>0)samples.Add(new Landmark{name="26/visible-rim/"+k,position=cut.OrderByDescending(p=>p.y).First()});
            }
            // Locate the visible vamp edge after transparent instep pixels.
            foreach(float x in new[]{.025f,.035f,.045f,.055f,.065f,.075f,.085f}) {
                var cuts=VisibleSection(points,baked.uv,baked.GetTriangles(26),Vector3.right,x,26);
                for(int i=0;i<=30;i++) {
                    float z=.020f+i*.002f;var near=cuts.Where(p=>Mathf.Abs(p.z-z)<.0005f).ToArray();
                    if(near.Length>0)samples.Add(new Landmark{name="vamp/"+x.ToString("F3")+"/"+i,position=new Vector3(x,near.Max(p=>p.y),z)});
                }
            }
            File.WriteAllText("work/reference-review/foot-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }

        // UV-seamed physical pieces distinguish the main folded cloth from
        // its upper/lower returns and the overlapping hanging tails.
        static void ReviewRibbonVolume(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var all=baked.GetTriangles(29);
            var parents=Enumerable.Range(0,points.Length).ToArray();Func<int,int> find=null;find=i=>parents[i]==i?i:parents[i]=find(parents[i]);
            for(int i=0;i<all.Length;i+=3){parents[find(all[i+1])]=find(all[i]);parents[find(all[i+2])]=find(all[i]);}
            var islands=all.Distinct().GroupBy(find).Select(g=>g.ToArray()).ToArray();
            var samples=new System.Collections.Generic.List<Landmark>();var log=new System.Collections.Generic.List<string>();
            foreach(int sign in new[]{-1,1}) {
                Func<int[],bool> side=g=>g.Average(i=>points[i].x)*sign>0;
                var main=islands.Where(side).Where(g=>g.Length>20&&g.Max(i=>points[i].x)-g.Min(i=>points[i].x)>.04f).Single();
                var upper=islands.Where(side).Where(g=>g.Max(i=>points[i].x)-g.Min(i=>points[i].x)>.04f&&g.Min(i=>points[i].y)>.359f).Single();
                var lower=islands.Where(side).Where(g=>g.Length<=12&&g.Max(i=>points[i].x)-g.Min(i=>points[i].x)>.04f&&g.Max(i=>points[i].y)<.354f).Single();
                Func<int[],int[]> faces=g=>{var ids=new System.Collections.Generic.HashSet<int>(g);return Enumerable.Range(0,all.Length/3).Where(k=>ids.Contains(all[k*3])).SelectMany(k=>new[]{all[k*3],all[k*3+1],all[k*3+2]}).ToArray();};
                var mainFaces=faces(main);
                foreach(string piece in new[]{"main","upper","lower"}) {
                    var group=piece=="main"?main:piece=="upper"?upper:lower;var selected=faces(group);
                    float tipX=group.Max(i=>points[i].x*sign),mainTip=main.Max(i=>points[i].x*sign),lowerTip=lower.Max(i=>points[i].x*sign);
                    var visibleMain=main.Where(i=>GuideTexture(29).GetPixelBilinear(baked.uv[i].x,baked.uv[i].y).a>=.5f).ToArray();
                    var corner=visibleMain.OrderBy(i=>points[i].y).First();float cornerX=points[corner].x*sign;
                    var stations=new[]{.003f,.008f,.015f,.025f,.035f,cornerX-.0000002f,lowerTip-.000001f,.050f,mainTip-.000001f}.Where(x=>x<tipX+.0000001f).Distinct().OrderBy(x=>x).ToArray();
                    log.Add(sign+"/"+piece+" lower-main-corner="+points[corner].ToString("F7"));
                    foreach(float x in stations) {
                        var cuts=VisibleSection(points,baked.uv,selected,Vector3.right,x*sign,29,maxStep:.00006f);
                        if(cuts.Count==0)throw new InvalidOperationException("Missing ribbon section "+piece+" x="+x);
                        if(piece=="main") {
                            float low=cuts.Min(p=>p.y),high=cuts.Max(p=>p.y);
                            for(int col=0;col<=4;col++) {
                                float y=Mathf.Lerp(low,high,col/4f),nearest=cuts.Min(p=>Mathf.Abs(p.y-y));
                                var p=cuts.Where(p=>Mathf.Abs(p.y-y)<=nearest+.00006f).OrderByDescending(p=>p.z).First();p.y=y;
                                samples.Add(new Landmark{name=sign+"/main/"+x.ToString("F7",System.Globalization.CultureInfo.InvariantCulture)+"/"+col,position=p});
                            }
                        } else {
                            var seam=VisibleSection(points,baked.uv,mainFaces,Vector3.right,x*sign,29,maxStep:.00006f);
                            var edge=piece=="upper"?seam.OrderByDescending(p=>p.y).First():seam.OrderBy(p=>p.y).First();
                            var far=cuts.OrderByDescending(p=>(p-edge).sqrMagnitude).First();
                            samples.Add(new Landmark{name=sign+"/"+piece+"/"+x.ToString("F7",System.Globalization.CultureInfo.InvariantCulture)+"/far",position=far});
                        }
                    }
                    log.Add(sign+"/"+piece+" source-piece-points="+group.Length+" measured-rows="+stations.Length+" last-X="+stations.Last().ToString("F7"));
                }
            }
            File.WriteAllText("work/reference-review/ribbon-volume-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            File.WriteAllLines("work/reference-review/ribbon-volume-sections.txt",log);
        }

        static void ReviewCollarRibbonGuides(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{29,30})foreach(float x in new[]{.003f,.008f,.015f,.025f,.035f,.045f,.051f,.0525f,.065f,.08f,.10f}) {
                var cuts=VisibleSection(points,baked.uv,baked.GetTriangles(slot),Vector3.right,x,slot,(p,st)=>p.z>-.01f&&(slot==29||p.y>.34f));
                if(cuts.Count==0)continue;
                // Record separated vertical intervals rather than joining a
                // tail, loop and collar simply because they share a material.
                var ordered=cuts.OrderBy(p=>p.y).ToArray();int start=0,band=0;
                for(int end=1;end<=ordered.Length;end++) {
                    if(end<ordered.Length&&ordered[end].y-ordered[end-1].y<.001f)continue;
                    var part=ordered.Skip(start).Take(end-start).ToArray();float low=part.First().y,high=part.Last().y;
                    for(int row=0;row<25;row++) {
                        float y=Mathf.Lerp(low,high,row/24f),nearest=part.Min(p=>Mathf.Abs(p.y-y));
                        var near=part.Where(p=>Mathf.Abs(p.y-y)<=nearest+.0003f).ToArray();
                        Vector3 front=near.OrderByDescending(p=>p.z).First(),back=near.OrderBy(p=>p.z).First();front.y=back.y=y;
                        samples.Add(new Landmark{name=slot+"/"+x.ToString("F4")+"/"+band+"/front/"+row,position=front});
                        samples.Add(new Landmark{name=slot+"/"+x.ToString("F4")+"/"+band+"/back/"+row,position=back});
                    }
                    start=end;band++;
                }
            }
            foreach(float y in new[]{.324f,.328f,.332f,.336f,.340f,.344f,.348f}) {
                var cuts=VisibleSection(points,baked.uv,baked.GetTriangles(29),Vector3.up,y,29,(p,st)=>p.x>.004f&&p.x<.034f);
                if(cuts.Count==0)continue;
                float low=cuts.Min(p=>p.x),high=cuts.Max(p=>p.x);
                for(int col=0;col<5;col++) {
                    float x=Mathf.Lerp(low,high,col/4f),nearest=cuts.Min(p=>Mathf.Abs(p.x-x));
                    var p=cuts.Where(p=>Mathf.Abs(p.x-x)<=nearest+.0002f).OrderBy(p=>p.z).First();p.x=x;
                    samples.Add(new Landmark{name="tail/"+y.ToString("F4")+"/"+col,position=p});
                }
            }
            File.WriteAllText("work/reference-review/collar-ribbon-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }

        static void ReviewBibGuides(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(float y in new[]{.27f,.28f,.29f,.30f,.31f,.32f,.33f,.34f,.35f,.36f}) {
                foreach(int slot in new[]{30,37}) {
                    var texture=GuideTexture(slot);
                    var cuts=VisibleSection(points,uv,baked.GetTriangles(slot),Vector3.up,y,slot,(p,st)=>{
                        if(p.z<0||Mathf.Abs(p.x)>.12f)return false;
                        if(slot==30)return p.x>.035f;
                        Color c=texture.GetPixelBilinear(st.x,st.y);
                        return Mathf.Min(c.r,Mathf.Min(c.g,c.b))>.60f&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<.25f;
                    });
                    if(cuts.Count==0)continue;
                    samples.Add(new Landmark{name=slot+"-left",position=cuts.OrderBy(p=>p.x).First()});
                    samples.Add(new Landmark{name=slot+"-right",position=cuts.OrderByDescending(p=>p.x).First()});
                    float mid=slot==37?0:(cuts.Min(p=>p.x)+cuts.Max(p=>p.x))*.5f;
                    samples.Add(new Landmark{name=slot+"-middle",position=cuts.OrderBy(p=>Mathf.Abs(p.x-mid)).First()});
                }
            }
            var goldPoints=new System.Collections.Generic.List<Vector3>();var cloth=GuideTexture(37);var clothTriangles=baked.GetTriangles(37);
            for(int i=0;i<clothTriangles.Length;i+=3) {
                int ai=clothTriangles[i],bi=clothTriangles[i+1],ci=clothTriangles[i+2];
                if(new[]{points[ai],points[bi],points[ci]}.All(p=>p.y<.25f||p.y>.28f||p.z<0))continue;
                for(int a=0;a<=16;a++)for(int b=0;b<=16-a;b++) {
                    float u=a/16f,v=b/16f;Vector3 p=points[ai]*(1-u-v)+points[bi]*u+points[ci]*v;
                    if(p.y<.25f||p.y>.28f||Mathf.Abs(p.x)<.015f||Mathf.Abs(p.x)>.05f||p.z<0)continue;
                    Vector2 st=uv[ai]*(1-u-v)+uv[bi]*u+uv[ci]*v;Color c=cloth.GetPixelBilinear(st.x,st.y);
                    if(c.a>.5f&&c.r>.5f&&c.g>.35f&&c.r-c.b>.20f&&c.g-c.b>.05f)goldPoints.Add(p);
                }
            }
            foreach(int sign in new[]{-1,1})foreach(int row in new[]{0,1}) {
                var selected=goldPoints.Where(p=>p.x*sign>0&&(p.y>.265f)==(row==1)).ToArray();
                if(selected.Length==0)continue;var bounds=new Bounds(selected[0],Vector3.zero);foreach(var p in selected)bounds.Encapsulate(p);
                samples.Add(new Landmark{name="gold-stud-"+sign+"-"+row,position=bounds.center});
            }
            File.WriteAllText("work/reference-review/bib-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }

        static void ReviewSideLaceSurface(Mesh baked,GameObject snapshot,Camera camera) {
            var points=baked.vertices.Select(snapshot.transform.TransformPoint).ToArray();var uv=baked.uv;
            var triangles=baked.GetTriangles(30);var samples=new System.Collections.Generic.List<Landmark>();
            foreach(int sign in new[]{-1,1})for(int row=0;row<=20;row++) {
                float registeredY=.414f+row*.006f,y=(registeredY-.07f)/1.25f;
                var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,30,(p,st)=>p.x*sign>.065f&&p.z>0,maxStep:.0001f);
                if(cuts.Count==0)continue;
                float lo=cuts.Min(p=>p.x),hi=cuts.Max(p=>p.x);
                for(int col=0;col<5;col++) {
                    float x=Mathf.Lerp(lo,hi,col/4f),near=cuts.Min(p=>Mathf.Abs(p.x-x));
                    Vector3 p=cuts.Where(q=>Mathf.Abs(q.x-x)<=near+.00005f).OrderByDescending(q=>q.z).First();p.x=x;
                    samples.Add(new Landmark{name="side-lace-"+sign+"-"+col,position=p});
                }
            }
            var strap=new System.Collections.Generic.List<Landmark>();var cloth=GuideTexture(37);var clothTriangles=baked.GetTriangles(37);
            foreach(int sign in new[]{-1,1})foreach(float registeredY in new[]{.416f,.428f,.440f,.452f,.464f,.476f,.488f,.500f,.512f}) {
                float y=(registeredY-.07f)/1.25f;
                var white=VisibleSection(points,uv,clothTriangles,Vector3.up,y,37,(p,st)=>{
                    Color c=cloth.GetPixelBilinear(st.x,st.y);return p.x*sign>0&&p.z>0&&Mathf.Min(c.r,Mathf.Min(c.g,c.b))>.60f;
                },maxStep:.0001f);
                var lace=VisibleSection(points,uv,triangles,Vector3.up,y,30,(p,st)=>p.x*sign>.065f&&p.z>0,maxStep:.0001f);
                var cuts=VisibleSection(points,uv,clothTriangles,Vector3.up,y,37,(p,st)=>p.x*sign>0&&p.z>0,maxStep:.0001f);
                if(white.Count==0||lace.Count==0||cuts.Count==0)throw new InvalidOperationException("Missing strap section: "+registeredY);
                float inner=white.Max(p=>p.x*sign),outer=lace.Min(p=>p.x*sign);
                for(int col=0;col<5;col++) {
                    float x=Mathf.Lerp(inner,outer,col/4f)*sign,near=cuts.Min(p=>Mathf.Abs(p.x-x));
                    Vector3 p=cuts.Where(q=>Mathf.Abs(q.x-x)<=near+.00005f).OrderByDescending(q=>q.z).First();p.x=x;
                    strap.Add(new Landmark{name="side-strap-"+sign+"-"+col,position=p});
                }
            }
            File.WriteAllText("work/reference-review/side-strap-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=strap.ToArray()},true));
            var tips=new System.Collections.Generic.List<Landmark>();
            foreach(int sign in new[]{-1,1})for(float registeredY=.408f;registeredY<=.414f;registeredY+=.00005f) {
                float y=(registeredY-.07f)/1.25f;
                var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,30,(p,st)=>p.x*sign>.065f&&p.z>0,maxStep:.00005f);
                if(cuts.Count==0)continue;Vector3 tip=Vector3.zero;foreach(var p in cuts)tip+=p/cuts.Count;
                tips.Add(new Landmark{name="side-lace-tip-"+sign,position=tip});break;
            }
            File.WriteAllText("work/reference-review/side-lace-tips.json",JsonUtility.ToJson(new FaceSectionReport{samples=tips.ToArray()},true));
            File.WriteAllText("work/reference-review/side-lace-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            var selected=new System.Collections.Generic.List<int>();
            for(int k=0;k<triangles.Length;k+=3) {
                var ids=new[]{triangles[k],triangles[k+1],triangles[k+2]};
                if(ids.All(i=>Mathf.Abs(points[i].x)>.065f&&points[i].z>0))selected.AddRange(ids);
            }
            var isolated=UnityEngine.Object.Instantiate(baked);
            for(int sub=0;sub<isolated.subMeshCount;sub++)isolated.SetTriangles(sub==30?selected.ToArray():Array.Empty<int>(),sub,false);
            var filter=snapshot.GetComponent<MeshFilter>();filter.sharedMesh=isolated;
            FishRebuiltModel.RenderBibStudy(camera,"work/reference-review/side-lace-selected",.8f,new Vector3(0,-.056f,0));
            filter.sharedMesh=baked;UnityEngine.Object.DestroyImmediate(isolated);
        }

        static void ReviewBibSurface(Mesh baked,GameObject snapshot,Camera camera) {
            var filter=snapshot.GetComponent<MeshFilter>();var renderer=snapshot.GetComponent<MeshRenderer>();
            var points=baked.vertices.Select(snapshot.transform.TransformPoint).ToArray();var uv=baked.uv;
            var triangles=baked.GetTriangles(37);var texture=GuideTexture(37);var samples=new System.Collections.Generic.List<Landmark>();
            var creases=new System.Collections.Generic.List<Landmark>();
            foreach(float registeredY in new[]{.416f,.428f,.440f,.452f,.464f,.476f,.488f,.500f,.512f}) {
                float y=(registeredY-.07f)/1.25f;
                var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,37,(p,st)=>p.z>0&&Mathf.Abs(p.x)<.12f,maxStep:.00015f);
                var white=VisibleSection(points,uv,triangles,Vector3.up,y,37,(p,st)=>{
                    Color c=texture.GetPixelBilinear(st.x,st.y);
                    return p.z>0&&Mathf.Abs(p.x)<.12f&&Mathf.Min(c.r,Mathf.Min(c.g,c.b))>.60f&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<.25f;
                },maxStep:.00015f);
                if(white.Count==0)throw new InvalidOperationException("Missing white bib section: "+registeredY);
                float lo=white.Min(p=>p.x),hi=white.Max(p=>p.x);
                var tonal=new System.Collections.Generic.List<Tuple<Vector3,float>>();
                VisibleSection(points,uv,triangles,Vector3.up,y,37,(p,st)=>{
                    Color c=texture.GetPixelBilinear(st.x,st.y);
                    if(p.z>0&&p.x>=lo&&p.x<=hi&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<.15f)
                        tonal.Add(Tuple.Create(p,c.grayscale));return false;
                },maxStep:.0001f);
                float[] centers={-.78f,-.39f,-.14f,.14f,.39f,.78f};
                for(int crease=0;crease<centers.Length;crease++) {
                    float target=centers[crease],width=Mathf.Abs(target)>.5f?.14f:.095f;
                    var band=tonal.Where(q=>Mathf.Abs(Mathf.InverseLerp(lo,hi,q.Item1.x)*2-1-target)<width&&q.Item2>.45f).ToArray();
                    if(band.Length==0||band.Max(q=>q.Item2)-band.Min(q=>q.Item2)<.025f)continue;
                    float minimum=band.Min(q=>q.Item2);var dark=band.Where(q=>q.Item2<minimum+.010f).ToArray();
                    Vector3 center=Vector3.zero;foreach(var q in dark)center+=q.Item1/dark.Length;
                    creases.Add(new Landmark{name="bib-crease-"+crease,position=center});
                }
                for(int col=0;col<15;col++) {
                    float x=Mathf.Lerp(lo,hi,col/14f),near=cuts.Min(p=>Mathf.Abs(p.x-x));
                    var p=cuts.Where(q=>Mathf.Abs(q.x-x)<=near+.000075f).OrderByDescending(q=>q.z).First();p.x=x;
                    samples.Add(new Landmark{name="bib-surface-"+col,position=p});
                }
            }
            File.WriteAllText("work/reference-review/bib-surface-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            File.WriteAllText("work/reference-review/bib-crease-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=creases.ToArray()},true));
            var buttons=new System.Collections.Generic.List<Vector3>();
            for(float y=.279f;y<=.340f;y+=.00025f)VisibleSection(points,uv,triangles,Vector3.up,y,37,(p,st)=>{
                Color c=texture.GetPixelBilinear(st.x,st.y);
                if(p.z>0&&Mathf.Abs(p.x)<.014f&&c.grayscale<.35f)buttons.Add(p);return false;
            },maxStep:.0001f);
            var groups=new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
            foreach(var p in buttons.OrderBy(p=>p.y)) {
                if(groups.Count==0||p.y-groups.Last().Last().y>.001f)groups.Add(new System.Collections.Generic.List<Vector3>());
                groups.Last().Add(p);
            }
            if(groups.Count!=3)throw new InvalidOperationException("Expected three bib button islands, found "+groups.Count);
            var anchors=new System.Collections.Generic.List<Landmark>();int index=0;
            foreach(var group in groups) {
                float x0=group.Min(p=>p.x),x1=group.Max(p=>p.x),y0=group.Min(p=>p.y),y1=group.Max(p=>p.y);
                Vector3 c=new Vector3((x0+x1)*.5f,(y0+y1)*.5f,group.Max(p=>p.z));
                anchors.Add(new Landmark{name="bib-button-"+index+"-center",position=c});
                anchors.Add(new Landmark{name="bib-button-"+index+"-left",position=new Vector3(x0,c.y,c.z)});
                anchors.Add(new Landmark{name="bib-button-"+index+"-right",position=new Vector3(x1,c.y,c.z)});
                anchors.Add(new Landmark{name="bib-button-"+index+"-bottom",position=new Vector3(c.x,y0,c.z)});
                anchors.Add(new Landmark{name="bib-button-"+index+"-top",position=new Vector3(c.x,y1,c.z)});index++;
            }
            File.WriteAllText("work/reference-review/bib-button-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=anchors.ToArray()},true));

            // Diagnose shape/normal shading separately from source paint.
            // Temporary snapshots/materials only; original assets are untouched.
            var original=renderer.sharedMaterials;
            for(int mode=0;mode<2;mode++) {
                var mesh=UnityEngine.Object.Instantiate(baked);mesh.colors=Enumerable.Repeat(mode==0?new Color(.65f,.65f,.65f,1):Color.white,mesh.vertexCount).ToArray();
                var materials=original.Select(m=>{
                    var n=new Material(Shader.Find("DesktopFish/Rebuilt Toon"));n.mainTexture=mode==0?Texture2D.whiteTexture:m.mainTexture;
                    n.SetFloat("_Flat",mode);n.SetFloat("_OutlineWidth",0);return n;
                }).ToArray();filter.sharedMesh=mesh;renderer.sharedMaterials=materials;
                FishRebuiltModel.RenderBibStudy(camera,"work/reference-review/bib-"+(mode==0?"clay":"paint-flat"),.8f,new Vector3(0,-.056f,0));
                filter.sharedMesh=baked;renderer.sharedMaterials=original;UnityEngine.Object.DestroyImmediate(mesh);
                foreach(var material in materials)UnityEngine.Object.DestroyImmediate(material);
            }
        }

        static Func<float,float,float> ProjectedDepth(Mesh baked,Transform snapshot,int slot,float lowY,float highY,float minZ) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;
            var source=baked.GetTriangles(slot);var texture=GuideTexture(slot);
            var faces=Enumerable.Range(0,source.Length/3).Select(k=>new[]{source[k*3],source[k*3+1],source[k*3+2]})
                .Where(t=>t.Max(i=>points[i].y)>lowY&&t.Min(i=>points[i].y)<highY&&t.Max(i=>points[i].z)>minZ).ToArray();
            return (x,y)=>{
                float front=float.NegativeInfinity;
                foreach(var t in faces) {
                    Vector3 a=points[t[0]],b=points[t[1]],c=points[t[2]];
                    if(x<Mathf.Min(a.x,Mathf.Min(b.x,c.x))||x>Mathf.Max(a.x,Mathf.Max(b.x,c.x))||
                        y<Mathf.Min(a.y,Mathf.Min(b.y,c.y))||y>Mathf.Max(a.y,Mathf.Max(b.y,c.y)))continue;
                    float den=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(den)<1e-12f)continue;
                    float wa=((b.y-c.y)*(x-c.x)+(c.x-b.x)*(y-c.y))/den;
                    float wb=((c.y-a.y)*(x-c.x)+(a.x-c.x)*(y-c.y))/den,wc=1-wa-wb;
                    if(wa<-.000001f||wb<-.000001f||wc<-.000001f)continue;
                    Vector2 st=uv[t[0]]*wa+uv[t[1]]*wb+uv[t[2]]*wc;
                    if(texture&&texture.GetPixelBilinear(st.x,st.y).a<.5f)continue;
                    front=Mathf.Max(front,a.z*wa+b.z*wb+c.z*wc);
                }
                return front;
            };
        }

        static void ReviewFrontDepth(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;
            var skin=ProjectedDepth(baked,snapshot,3,.38f,.67f,-1);var white=ProjectedDepth(baked,snapshot,4,.38f,.67f,-1);
            var samples=new System.Collections.Generic.List<Landmark>();
            float[] ys={.55125f,.5575f,.570f,.5825f,.595f,.6075f,.620f,.6325f,.645f,.6575f,.670f,.6825f,.695f,.7075f,.720f,.745f,.770f,.795f,.820f,.845f,.870f,.895f};
            float[] columns={0,.04f,.12f,.24f,.36f,.44f,.5f,.56f,.64f,.76f,.88f,.96f,1};
            for(int row=0;row<ys.Length;row++) {
                float y=(ys[row]-.07f)/1.25f;var cuts=VisibleSection(points,uv,baked.GetTriangles(3),Vector3.up,y,3,maxStep:.0001f);
                float lo=cuts.Min(q=>q.x),hi=cuts.Max(q=>q.x);
                for(int col=0;col<columns.Length;col++) {
                    float u=Mathf.Clamp(columns[col],.00001f,.99999f),x=Mathf.Lerp(lo,hi,u),z=Mathf.Max(skin(x,y),white(x,y));
                    if(float.IsNegativeInfinity(z))throw new InvalidOperationException("Missing front-depth sample "+row+"/"+col);
                    samples.Add(new Landmark{name="front-depth-"+row+"-"+col,position=new Vector3(x,y,z)});
                }
            }
            File.WriteAllText("work/reference-review/front-depth-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
        }

        // Visible white backing is clipped by the facial opening. Measuring
        // the isolated eyeball extrema alone would reproduce the hidden sphere.
        static void ReviewEyeApertures(Mesh baked,Transform snapshot) {
            var projected=new System.Collections.Generic.Dictionary<int,Func<float,float,float>>();
            foreach(int slot in new[]{3,4})projected[slot]=ProjectedDepth(baked,snapshot,slot,.43f,.53f,.065f);
            Func<float,float,bool> visible=(x,y)=>{
                float white=projected[4](x,y);return !float.IsNegativeInfinity(white)&&white>projected[3](x,y)+.00001f;
            };
            Func<float,Vector2?> section=x=>{
                float lo=float.PositiveInfinity,hi=float.NegativeInfinity;
                for(int row=0;row<=634;row++){float y=.43f+row*.00015f;if(visible(x,y)){lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}}
                if(float.IsPositiveInfinity(lo))return null;
                float a=lo-.00015f,b=lo;for(int i=0;i<20;i++){float y=(a+b)*.5f;if(visible(x,y))b=y;else a=y;}lo=b;
                a=hi;b=hi+.00015f;for(int i=0;i<20;i++){float y=(a+b)*.5f;if(visible(x,y))a=y;else b=y;}hi=a;
                return new Vector2(lo,hi);
            };
            var measured=new System.Collections.Generic.List<Landmark>();
            foreach(int sign in new[]{-1,1}) {
                var occupied=Enumerable.Range(0,106).Select(i=>.025f+i*.001f).Where(x=>section(sign*x).HasValue).ToArray();
                if(occupied.Length==0)throw new InvalidOperationException("No visible white aperture: "+sign);
                float inner=occupied.First(),outer=occupied.Last();
                foreach(bool far in new[]{false,true}) {
                    float inside=far?outer:inner,outside=inside+(far?.001f:-.001f);
                    for(int i=0;i<20;i++){float x=(inside+outside)*.5f;if(section(sign*x).HasValue)inside=x;else outside=x;}
                    if(far)outer=inside;else inner=inside;
                }
                for(int col=0;col<=16;col++) {
                    float u=.5f-.5f*Mathf.Cos(col*Mathf.PI/16),x=sign*Mathf.Lerp(inner,outer,u);
                    Vector2 bounds=section(x).Value;
                    foreach(bool upper in new[]{false,true}) {
                        float y=upper?bounds.y:bounds.x;
                        measured.Add(new Landmark{name="white-"+sign+"-"+col+"-"+(upper?"upper":"lower"),position=new Vector3(x,y,projected[4](x,y))});
                    }
                }
            }
            File.WriteAllText("work/reference-review/eye-aperture-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=measured.ToArray()},true));
        }

        static void ReviewEyeProfiles(Mesh baked,Transform snapshot) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var samples=new System.Collections.Generic.List<Landmark>();
            var irisSamples=new System.Collections.Generic.List<Landmark>();
            foreach(int slot in new[]{1,4,5,7})foreach(int sign in new[]{-1,1}) {
                var triangles=baked.GetTriangles(slot);var texture=GuideTexture(slot);
                Func<float,System.Collections.Generic.List<Vector3>> section=distance=>{
                    var cuts=VisibleSection(points,baked.uv,triangles,Vector3.right,sign*distance,slot,(p,st)=>
                        p.z>.065f&&(slot!=7||p.y>.46f&&texture.GetPixelBilinear(st.x,st.y).grayscale<.35f),maxStep:.00015f);
                    if(slot==7&&cuts.Count>0) {
                        // A separated lower-lid trace must not fill the eye.
                        var sorted=cuts.OrderBy(p=>p.y).ToArray();int first=sorted.Length-1;
                        while(first>0&&sorted[first].y-sorted[first-1].y<.001f)first--;
                        cuts=sorted.Skip(first).ToList();
                    }
                    return cuts;
                };
                var rows=new System.Collections.Generic.List<Tuple<float,System.Collections.Generic.List<Vector3>>>();
                for(int col=0;col<=40;col++) {
                    float distance=.015f+.0035f*col;var cuts=section(distance);
                    if(cuts.Count>0)rows.Add(Tuple.Create(distance,cuts));
                }
                if((slot==7||slot==1)&&rows.Count>0)foreach(bool outer in new[]{false,true}) {
                    float inside=outer?rows.Last().Item1:rows.First().Item1,outside=inside+(outer?.0035f:-.0035f);
                    for(int i=0;i<24;i++) {
                        float middle=(inside+outside)*.5f;
                        if(section(middle).Count>0)inside=middle;else outside=middle;
                    }
                    var boundary=Tuple.Create(inside,section(inside));
                    if(outer)rows.Add(boundary);else rows.Insert(0,boundary);
                }
                if(slot==1&&rows.Count>0) {
                    var all=rows.SelectMany(r=>r.Item2).ToArray();Vector3 center=new Vector3(
                        (all.Min(p=>p.x)+all.Max(p=>p.x))*.5f,(all.Min(p=>p.y)+all.Max(p=>p.y))*.5f,0);
                    var middle=section(Mathf.Abs(center.x));float nearest=middle.Min(p=>Mathf.Abs(p.y-center.y));
                    center.z=middle.Where(p=>Mathf.Abs(p.y-center.y)<=nearest+.0001f).Max(p=>p.z);
                    irisSamples.Add(new Landmark{name="iris-"+sign+"-center",position=center});
                    for(int ray=0;ray<32;ray++) {
                        float angle=ray*Mathf.PI*2/32;Vector3 direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                        Vector3 normal=new Vector3(-direction.y,direction.x,0);
                        var cuts=VisibleSection(points,baked.uv,triangles,normal,Vector3.Dot(center,normal),slot,
                            (p,st)=>p.x*sign>0&&p.z>.065f&&Vector3.Dot(p-center,direction)>0,maxStep:.0001f);
                        if(cuts.Count==0)throw new InvalidOperationException("Missing alpha-visible iris ray: "+sign+"/"+ray);
                        irisSamples.Add(new Landmark{name="iris-"+sign+"-outer-"+ray,position=cuts.OrderByDescending(p=>Vector3.Dot(p-center,direction)).First()});
                    }
                }
                for(int col=0;col<rows.Count;col++) {
                    var cuts=rows[col].Item2;string key="eye-"+slot+"-"+sign+"-"+col;
                    samples.Add(new Landmark{name=key+"-lower",position=cuts.OrderBy(p=>p.y).First()});
                    samples.Add(new Landmark{name=key+"-upper",position=cuts.OrderByDescending(p=>p.y).First()});
                }
            }
            File.WriteAllText("work/reference-review/eye-profile-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            File.WriteAllText("work/reference-review/iris-outline-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=irisSamples.ToArray()},true));
        }

        // Three sparse side/rear/front extrema per alpha-visible skin section.
        // Only position guides leave this local review, never source mesh or UVs.
        static void ReviewHeadSideGuides(Mesh baked,Transform snapshot,SkinnedMeshRenderer skin) {
            var points=baked.vertices.Select(snapshot.TransformPoint).ToArray();var uv=baked.uv;var triangles=baked.GetTriangles(3);
            var samples=new System.Collections.Generic.List<Landmark>();
            foreach(float y in new[]{.3825f,.385f,.39f,.40f,.41f,.42f,.43f,.44f,.45f,.46f,.47f,.48f,.50f,.52f,.54f,.56f,.58f,.60f}) {
                var cuts=VisibleSection(points,uv,triangles,Vector3.up,y,3,maxStep:.00015f);if(cuts.Count==0)continue;
                var left=cuts.OrderBy(q=>q.x).First();var right=cuts.OrderByDescending(q=>q.x).First();
                samples.Add(new Landmark{name="head-left",position=left});samples.Add(new Landmark{name="head-right",position=right});
                samples.Add(new Landmark{name="head-rear",position=cuts.OrderBy(q=>q.z).First()});
                samples.Add(new Landmark{name="head-front",position=cuts.OrderByDescending(q=>q.z).First()});
            }
            File.WriteAllText("work/reference-review/head-side-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=samples.ToArray()},true));
            var neck=new System.Collections.Generic.List<Landmark>();var weights=skin.sharedMesh.boneWeights;
            var headNeck=new System.Collections.Generic.HashSet<int>(skin.bones.Select((bone,index)=>new{bone,index}).Where(v=>v.bone.name=="J_Bip_C_Head"||v.bone.name=="J_Bip_C_Neck").Select(v=>v.index));
            Func<int,float> influence=i=>{var w=weights[i];return (headNeck.Contains(w.boneIndex0)?w.weight0:0)+(headNeck.Contains(w.boneIndex1)?w.weight1:0)+(headNeck.Contains(w.boneIndex2)?w.weight2:0)+(headNeck.Contains(w.boneIndex3)?w.weight3:0);};
            var all=baked.GetTriangles(18);var selected=new System.Collections.Generic.List<int>();
            for(int i=0;i<all.Length;i+=3)if(influence(all[i])>.5f||influence(all[i+1])>.5f||influence(all[i+2])>.5f)selected.AddRange(new[]{all[i],all[i+1],all[i+2]});
            foreach(float y in new[]{.34f,.35f,.36f,.37f,.38f,.39f,.40f,.41f,.42f,.44f,.46f,.48f,.50f,.52f}) {
                var cuts=VisibleSection(points,uv,selected.ToArray(),Vector3.up,y,18,maxStep:.00015f);if(cuts.Count==0)continue;
                neck.Add(new Landmark{name="neck-left",position=cuts.OrderBy(q=>q.x).First()});neck.Add(new Landmark{name="neck-right",position=cuts.OrderByDescending(q=>q.x).First()});
                neck.Add(new Landmark{name="neck-rear",position=cuts.OrderBy(q=>q.z).First()});neck.Add(new Landmark{name="neck-front",position=cuts.OrderByDescending(q=>q.z).First()});
            }
            File.WriteAllText("work/reference-review/neck-guides.json",JsonUtility.ToJson(new FaceSectionReport{samples=neck.ToArray()},true));
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
