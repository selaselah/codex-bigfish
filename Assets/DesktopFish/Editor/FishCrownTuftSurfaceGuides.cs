using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;
namespace DesktopFish.Editor {
// Complete independent blade, retained after the unchanged base reduction and
// neck assembly. Longitudinal corners replace the old coarse polar envelope.
public static class FishCrownTuftSurfaceGuides {
static readonly Vector3[] Positions={new Vector3(0.102165706f,1.050935626f,-0.039195348f),
new Vector3(0.092063009f,1.063627612f,-0.036552983f),
new Vector3(0.095289180f,1.066650175f,-0.034206333f),
new Vector3(0.096139064f,1.067454838f,-0.038963047f),
new Vector3(0.092949712f,1.064461193f,-0.040703472f),
new Vector3(0.081098022f,1.074830136f,-0.033024894f),
new Vector3(0.085685983f,1.080833206f,-0.028274383f),
new Vector3(0.086626509f,1.082065838f,-0.037068152f),
new Vector3(0.082124469f,1.076176287f,-0.040768811f),
new Vector3(0.068795060f,1.083806800f,-0.028458324f),
new Vector3(0.073304723f,1.092324213f,-0.021343080f),
new Vector3(0.074227525f,1.094051627f,-0.033252709f),
new Vector3(0.069785368f,1.085670788f,-0.039094282f),
new Vector3(0.055882381f,1.091308096f,-0.023329566f),
new Vector3(0.059245145f,1.101150621f,-0.014235842f),
new Vector3(0.060151466f,1.103740462f,-0.028162015f),
new Vector3(0.056714559f,1.093656962f,-0.036332234f),
new Vector3(0.042108016f,1.095645067f,-0.017389617f),
new Vector3(0.043854877f,1.105238824f,-0.006261890f),
new Vector3(0.044630079f,1.109458913f,-0.021422236f),
new Vector3(0.042835113f,1.099606114f,-0.031722577f),
new Vector3(0.028106398f,1.094346626f,-0.010157950f),
new Vector3(0.028436354f,1.104273197f,0.002504068f),
new Vector3(0.028674720f,1.111337516f,-0.013745581f),
new Vector3(0.028348323f,1.101519547f,-0.025396901f),
new Vector3(0.015088971f,1.089723608f,-0.002570838f),
new Vector3(0.013920292f,1.100117177f,0.011471185f),
new Vector3(0.012868094f,1.109488721f,-0.005576237f),
new Vector3(0.013995613f,1.099463885f,-0.018532674f),
new Vector3(0.003378772f,1.082472137f,0.004566488f),
new Vector3(0.000546493f,1.093124899f,0.020061800f),
new Vector3(-0.002609065f,1.104947462f,0.002801268f),
new Vector3(0.000121375f,1.094691946f,-0.011506455f),
new Vector3(-0.006704325f,1.073137125f,0.010796438f),
new Vector3(-0.011180271f,1.083287455f,0.027806984f),
new Vector3(-0.017382726f,1.097346829f,0.011268598f),
new Vector3(-0.012990900f,1.087387870f,-0.004686784f),
new Vector3(-0.014898485f,1.062230518f,0.016354882f),
new Vector3(-0.020865075f,1.071349542f,0.034301317f),
new Vector3(-0.030796194f,1.086528940f,0.019489577f),
new Vector3(-0.024861305f,1.077461123f,0.001910699f),
new Vector3(-0.020666335f,1.049996681f,0.021360600f),
new Vector3(-0.027806138f,1.057619764f,0.039376533f),
new Vector3(-0.041585794f,1.072342100f,0.027009655f),
new Vector3(-0.034476464f,1.064747317f,0.008540338f),
new Vector3(-0.024219127f,1.037353778f,0.025973184f),
new Vector3(-0.031890296f,1.042968304f,0.043634775f),
new Vector3(-0.048252621f,1.054978066f,0.033501048f),
new Vector3(-0.040757083f,1.049483097f,0.014812614f),
new Vector3(-0.025762476f,1.024486339f,0.029586915f),
new Vector3(-0.033056809f,1.027794428f,0.046423550f),
new Vector3(-0.050542981f,1.035826396f,0.038680293f),
new Vector3(-0.043479099f,1.032602518f,0.020767951f),
new Vector3(-0.025039543f,1.011413321f,0.031211645f),
new Vector3(-0.030693855f,1.012563451f,0.046526949f),
new Vector3(-0.047773457f,1.016032867f,0.041221738f),
new Vector3(-0.042141396f,1.014888002f,0.024980343f),
new Vector3(-0.020550729f,0.998993787f,0.029952758f),
new Vector3(-0.024277158f,0.998809246f,0.043359641f),
new Vector3(-0.038594935f,0.998099945f,0.039225613f),
new Vector3(-0.034880870f,0.998283894f,0.025517598f),
new Vector3(-0.014484522f,0.987199264f,0.027227019f),
new Vector3(-0.016352872f,0.986532867f,0.038309768f),
new Vector3(-0.026393911f,0.982959085f,0.034020690f),
new Vector3(-0.024354482f,0.983686044f,0.023209366f),
new Vector3(-0.005754610f,0.976875014f,0.022764686f),
new Vector3(-0.006781207f,0.976062162f,0.031060461f),
new Vector3(-0.012256543f,0.971718237f,0.026834558f),
new Vector3(-0.010992050f,0.972722634f,0.019146008f),
new Vector3(0.005049229f,0.968056246f,0.017050745f),
new Vector3(0.004613076f,0.967326074f,0.021856591f),
new Vector3(0.002880813f,0.964429483f,0.018604728f),
new Vector3(0.003398269f,0.965297592f,0.014581776f),
new Vector3(0.017526144f,0.959950745f,0.010430281f)};
static readonly int[] Triangles={0,2,1,0,3,2,0,4,3,0,1,4,1,2,5,2,6,5,2,3,6,3,7,6,3,4,7,4,8,7,4,1,8,1,5,8,5,6,9,6,10,9,6,7,10,7,11,10,7,8,11,8,12,11,8,5,12,5,9,12,9,10,13,10,14,13,10,11,14,11,15,14,11,12,15,12,16,15,12,9,16,9,13,16,13,14,17,14,18,17,14,15,18,15,19,18,15,16,19,16,20,19,16,13,20,13,17,20,17,18,21,18,22,21,18,19,22,19,23,22,19,20,23,20,24,23,20,17,24,17,21,24,21,22,25,22,26,25,22,23,26,23,27,26,23,24,27,24,28,27,24,21,28,21,25,28,25,26,29,26,30,29,26,27,30,27,31,30,27,28,31,28,32,31,28,25,32,25,29,32,29,30,33,30,34,33,30,31,34,31,35,34,31,32,35,32,36,35,32,29,36,29,33,36,33,34,37,34,38,37,34,35,38,35,39,38,35,36,39,36,40,39,36,33,40,33,37,40,37,38,41,38,42,41,38,39,42,39,43,42,39,40,43,40,44,43,40,37,44,37,41,44,41,42,45,42,46,45,42,43,46,43,47,46,43,44,47,44,48,47,44,41,48,41,45,48,45,46,49,46,50,49,46,47,50,47,51,50,47,48,51,48,52,51,48,45,52,45,49,52,49,50,53,50,54,53,50,51,54,51,55,54,51,52,55,52,56,55,52,49,56,49,53,56,53,54,57,54,58,57,54,55,58,55,59,58,55,56,59,56,60,59,56,53,60,53,57,60,57,58,61,58,62,61,58,59,62,59,63,62,59,60,63,60,64,63,60,57,64,57,61,64,61,62,65,62,66,65,62,63,66,63,67,66,63,64,67,64,68,67,64,61,68,61,65,68,65,66,69,66,70,69,66,67,70,67,71,70,67,68,71,68,72,71,68,65,72,65,69,72,69,70,73,70,71,73,71,72,73,72,69,73};
static Matrix4x4 DefaultHead(Transform root,Transform[] bones,Matrix4x4[] bind,int head){
 var all=root.GetComponentsInChildren<Transform>(true);var p=all.Select(t=>t.localPosition).ToArray();var q=all.Select(t=>t.localRotation).ToArray();var s=all.Select(t=>t.localScale).ToArray();if(root.GetComponent<FishRigProfile>())throw new InvalidOperationException("141 expects original bind frame");var profile=root.gameObject.AddComponent<FishRigProfile>();profile.articulatedHandRig=true;Matrix4x4 matrix;
 try{new FishMotion(root){randomActions=false}.Tick(0,1,0,0,"idle",false);matrix=root.worldToLocalMatrix*bones[head].localToWorldMatrix*bind[head];}
 finally{for(int i=0;i<all.Length;i++){all[i].localPosition=p[i];all[i].localRotation=q[i];all[i].localScale=s[i];}UnityEngine.Object.DestroyImmediate(profile);}return matrix;
}
public static Mesh Apply(Mesh old,Transform root,Transform[] bones,int head){
 var p=old.vertices;var n=old.normals;var uv=old.uv;var c=old.colors;var w=old.boneWeights;var t=old.triangles;
 var controls=Enumerable.Range(0,FishCrownTuftGuides.Rows).SelectMany(row=>Enumerable.Range(0,FishCrownTuftGuides.Columns).Select(col=>FishCrownTuftGuides.Point(row,col))).ToArray();
 var selected=Enumerable.Range(0,p.Length).Select(i=>w[i].boneIndex0==head&&w[i].weight0>.999f&&controls.Min(q=>(p[i]-q).sqrMagnitude)<1e-10f).ToArray();var keep=new List<int>();int removed=0;
 for(int k=0;k<t.Length;k+=3){bool on=selected[t[k]]||selected[t[k+1]]||selected[t[k+2]];if(on){if(!(selected[t[k]]&&selected[t[k+1]]&&selected[t[k+2]]))throw new InvalidOperationException("141 mixed old tuft boundary");removed++;}else keep.AddRange(new[]{t[k],t[k+1],t[k+2]});}
 if(removed!=114||selected.Count(q=>q)!=59)throw new InvalidOperationException("141 actual old tuft scope changed");
 var used=keep.Distinct().OrderBy(i=>i).ToArray();var remap=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)remap[used[i]]=i;
 var pp=used.Select(i=>p[i]).ToList();var nn=used.Select(i=>n[i]).ToList();var st=used.Select(i=>uv[i]).ToList();var colors=used.Select(i=>c[i]).ToList();var weights=used.Select(i=>w[i]).ToList();var faces=keep.Select(i=>remap[i]).ToList();
 var matrix=DefaultHead(root,bones,old.bindposes,head);if(Mathf.Abs(matrix.determinant)<.000001f)throw new InvalidOperationException("141 singular head response");var inverse=matrix.inverse;var own=Positions.Select(inverse.MultiplyPoint3x4).ToArray();var part=new Mesh();part.vertices=own;part.triangles=Triangles;part.RecalculateNormals();float residual=Enumerable.Range(0,own.Length).Max(i=>Vector3.Distance(matrix.MultiplyPoint3x4(own[i]),Positions[i]));if(residual>2e-7f)throw new InvalidOperationException("141 default rigid head attachment");int start=pp.Count;pp.AddRange(own);nn.AddRange(part.normals);
 st.AddRange(Enumerable.Repeat(new Vector2(0.995000005f,0.560000002f),own.Length));colors.AddRange(Enumerable.Repeat(new Color(0.188235298f,0.298039228f,0.568627477f,1.000000000f),own.Length));
 weights.AddRange(Enumerable.Repeat(new BoneWeight{boneIndex0=head,weight0=1},own.Length));faces.AddRange(Triangles.Select(i=>i+start));
 var mesh=new Mesh{name=old.name};mesh.SetVertices(pp);mesh.SetNormals(nn);mesh.SetUVs(0,st);mesh.SetColors(colors);mesh.boneWeights=weights.ToArray();mesh.bindposes=old.bindposes;mesh.SetTriangles(faces,0);mesh.RecalculateBounds();if(mesh.triangles.Length/3+800>20000)throw new InvalidOperationException("141 actual full budget");
 File.WriteAllText("work/rebuilt-tuft-surface.json","{\"method\":\"unchanged base QEM and135assembly, complete independent crown blade replacement, no further reduction\",\"outside_actual_vertices\":"+used.Length+",\"removed_actual_triangles\":114,\"new_complete_triangles\":144,\"new_complete_vertices\":74,\"new_part_reduction_error\":0,\"actual_UV_preserved\":true,\"own_rigid_default_residual\":"+residual.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(old);return mesh;
}
}}
