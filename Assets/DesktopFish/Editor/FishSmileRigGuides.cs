using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;
namespace DesktopFish.Editor {public static class FishSmileRigGuides {
const string Folder="work/ref-smile-rig-151";
[Serializable]public class Joint {public string name,parent;public Vector3 position;}
[Serializable]public class Band {public float lower,upper,zLower,zUpper;}
[Serializable]public class Profile {public int side;public Band[] neutral,eye,all;}
[Serializable]public class BrowPoint {public float x,lower,upper,zLower,zUpper;}
[Serializable]public class Brows {public int side;public BrowPoint[] lowerNeutral,lowerClosed,upperNeutral,upperClosed;}
[Serializable]public class Config {public Brows[] brows;public Joint[] joints;public int width,height;public float minX,minY,stepX,stepY;public Vector3[] eye,residual;public Profile[] profiles;public int[] mouthIndices;public Vector2[] mouthUV;public Vector3[] mouthJoy;}
static Config config;
static Vector3 Sample(Vector3 p,bool residual){
 float x=(p.x-config.minX)/config.stepX,y=(p.y-config.minY)/config.stepY;float edge=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.20f,Mathf.Abs(p.x))))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.53f,.55f,p.y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.79f,.82f,p.y)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.11f,p.z));
 x=Mathf.Clamp(x,0,config.width-1);y=Mathf.Clamp(y,0,config.height-1);int ix=Mathf.Min(config.width-2,Mathf.FloorToInt(x)),iy=Mathf.Min(config.height-2,Mathf.FloorToInt(y));var f=residual?config.residual:config.eye;return Vector3.Lerp(Vector3.Lerp(f[iy*config.width+ix],f[iy*config.width+ix+1],x-ix),Vector3.Lerp(f[(iy+1)*config.width+ix],f[(iy+1)*config.width+ix+1],x-ix),y-iy)*edge;
}
static Band At(float x,bool closed){var f=config.profiles[x<0?0:1];var a=closed?f.eye:f.neutral;float q=Mathf.Clamp((Mathf.Abs(x)-.04f)/.003f,0,a.Length-1);int i=Mathf.Min(a.Length-2,Mathf.FloorToInt(q));float t=q-i;return new Band{lower=Mathf.Lerp(a[i].lower,a[i+1].lower,t),upper=Mathf.Lerp(a[i].upper,a[i+1].upper,t),zLower=Mathf.Lerp(a[i].zLower,a[i+1].zLower,t),zUpper=Mathf.Lerp(a[i].zUpper,a[i+1].zUpper,t)};}
static bool Skin(Vector2 st){float x=st.x*1024,y=st.y*2048;return x>=648&&x<=752&&y>=784&&y<=1080||x>=512&&x<=568&&(y>=1696&&y<=1824||y>=1856&&y<=1984);}
static bool Colour(Color c,int r,int g,int b)=>Mathf.Abs(c.r-r/255f)<1e-6f&&Mathf.Abs(c.g-g/255f)<1e-6f&&Mathf.Abs(c.b-b/255f)<1e-6f;
static void Calibrate(GameObject root,SkinnedMeshRenderer[] skins){
 var old=skins[0].bones;var bones=old.ToList();var map=old.ToDictionary(b=>b.name);var points=old.ToDictionary(b=>b,b=>b.position);
 foreach(var j in config.joints){if(!map.TryGetValue(j.name,out var b)){b=new GameObject(j.name).transform;b.SetParent(map[j.parent],false);bones.Add(b);map[j.name]=b;}points[b]=j.position;}
 foreach(string n in new[]{"J_Bip_C_Neck","J_Bip_L_Shoulder","J_Bip_R_Shoulder"})map[n].SetParent(map["J_Bip_C_UpperChest"],true);
 foreach(var b in root.GetComponentsInChildren<Transform>(true))if(points.TryGetValue(b,out var p))b.position=p;
 if(bones.Count!=58||old.Where((b,i)=>bones[i]!=b).Any())throw new InvalidOperationException("151 preserved bone indices changed");
 foreach(var skin in skins){var mesh=skin.sharedMesh;skin.bones=bones.ToArray();mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*skin.localToWorldMatrix).ToArray();if(mesh.blendShapeCount==6){var weights=mesh.boneWeights;var c=mesh.colors;var p=mesh.vertices;int iris=0;for(int i=0;i<c.Length;i++)if(Colour(c[i],255,255,255)){int id=bones.IndexOf(map[p[i].x<0?"J_Adj_L_FaceEye":"J_Adj_R_FaceEye"]);weights[i]=new BoneWeight{boneIndex0=id,boneIndex1=id,weight0=1};iris++;}if(iris!=130)throw new InvalidOperationException("151 iris binding scope "+iris);mesh.boneWeights=weights;}}
 Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/trial-rig.json","{\"joints\":58,\"existing55_indices_preserved\":true,\"new_roles\":[\"UpperChest\",\"LeftFaceEye\",\"RightFaceEye\"],\"own_iris_records_rebound\":130,\"source_numeric_weights_copied\":false}");
}
// Project only the independently reconstructed morph onto the existing own
// surface orientation constraints. Neutral geometry and topology remain exact.
static Vector3[] PreserveSurface(Mesh mesh,Vector3[] d,HashSet<int> allowed,string label) {
 var p=mesh.vertices;var q=p.Select((v,i)=>v+d[i]).ToArray();var tri=mesh.triangles;int corrected=0;
 for(int pass=0;pass<200;pass++){bool changed=false;for(int k=0;k<tri.Length;k+=3){int a=tri[k],b=tri[k+1],c=tri[k+2];if(!allowed.Contains(a)||!allowed.Contains(b)||!allowed.Contains(c))continue;
  Vector3 n=Vector3.Cross(p[b]-p[a],p[c]-p[a]);float area=n.magnitude;if(area<1e-9f)continue;n/=area;
  float required=Mathf.Max(1e-9f,area*.02f),actual=Vector3.Dot(Vector3.Cross(q[b]-q[a],q[c]-q[a]),n);if(actual>=required)continue;
  Vector3 gb=Vector3.Cross(q[c]-q[a],n),gc=Vector3.Cross(n,q[b]-q[a]),ga=-gb-gc;float denominator=ga.sqrMagnitude+gb.sqrMagnitude+gc.sqrMagnitude;if(denominator<1e-16f)throw new InvalidOperationException("151 singular surface constraint");
  float t=(required-actual)/denominator*1.001f;q[a]+=ga*t;q[b]+=gb*t;q[c]+=gc*t;changed=true;corrected++;
 }if(!changed)break;}
 for(int k=0;k<tri.Length;k+=3){int a=tri[k],b=tri[k+1],c=tri[k+2];if(allowed.Contains(a)&&allowed.Contains(b)&&allowed.Contains(c)){var n=Vector3.Cross(p[b]-p[a],p[c]-p[a]);if(n.sqrMagnitude>1e-18f&&Vector3.Dot(Vector3.Cross(q[b]-q[a],q[c]-q[a]),n)<=0)throw new InvalidOperationException("151 opposed reconstructed surface "+label+" face "+k/3);}}
 float max=0;for(int i=0;i<d.Length;i++){max=Mathf.Max(max,(q[i]-p[i]-d[i]).magnitude);d[i]=q[i]-p[i];}Debug.Log("151 surface constraint "+label+" corrections "+corrected+" max physical displacement correction "+max);return d;
}
static Vector3[] TransportNormals(Mesh mesh,Vector3[] delta) {
 var copy=UnityEngine.Object.Instantiate(mesh);copy.ClearBlendShapes();copy.RecalculateNormals();var before=copy.normals;copy.vertices=copy.vertices.Select((v,i)=>v+delta[i]).ToArray();copy.RecalculateNormals();var after=copy.normals;var authored=mesh.normals;var result=new Vector3[authored.Length];var st=mesh.uv;
 for(int i=0;i<result.Length;i++)if(Skin(st[i]))result[i]=Quaternion.FromToRotation(before[i],after[i])*authored[i]-authored[i];UnityEngine.Object.DestroyImmediate(copy);return result;
}
static bool Socket(Vector2 uv)=>uv.x*1024>=512&&uv.x*1024<=568&&uv.y*2048>=1696;
static Vector3 SocketDelta(int index,Vector3[] p,Vector2[] st,int[] tri,Vector3[] d){float bestZ=float.NegativeInfinity;Vector3 found=Vector3.zero;bool hit=false;Vector2 q=p[index];for(int k=0;k<tri.Length;k+=3){int a=tri[k],b=tri[k+1],c=tri[k+2];if(!Skin(st[a])||!Skin(st[b])||!Skin(st[c])||Socket(st[a])||Socket(st[b])||Socket(st[c]))continue;Vector2 A=p[a],B=p[b],C=p[c];float det=(B.x-A.x)*(C.y-A.y)-(B.y-A.y)*(C.x-A.x);if(Mathf.Abs(det)<1e-12f)continue;Vector2 v=q-A;float u=(v.x*(C.y-A.y)-v.y*(C.x-A.x))/det,w=((B.x-A.x)*v.y-(B.y-A.y)*v.x)/det;if(u<-.00001f||w<-.00001f||u+w>1.00001f)continue;float z=(1-u-w)*p[a].z+u*p[b].z+w*p[c].z;if(z>bestZ){bestZ=z;found=(1-u-w)*d[a]+u*d[b]+w*d[c];hit=true;}}if(!hit)throw new InvalidOperationException("151 socket has no corresponding head triangle "+index);return found;}
static void DeformBody(Mesh mesh,string[] names){var p=mesh.vertices;var n=mesh.normals;var st=mesh.uv;int selected=st.Count(Skin);if(selected!=785)throw new InvalidOperationException("151 head scope "+selected);mesh.ClearBlendShapes();foreach(string name in names){var d=new Vector3[p.Length];var dn=new Vector3[p.Length];bool active=name=="Fcl_EYE_Joy"||name=="Fcl_ALL_Joy",residual=name=="Fcl_ALL_Joy";if(active)for(int i=0;i<p.Length;i++)if(Skin(st[i])){d[i]=Sample(p[i],residual);const float e=.0001f;Vector3 x=Vector3.right+(Sample(p[i]+Vector3.right*e,residual)-Sample(p[i]-Vector3.right*e,residual))/(2*e),y=Vector3.up+(Sample(p[i]+Vector3.up*e,residual)-Sample(p[i]-Vector3.up*e,residual))/(2*e),z=Vector3.forward+(Sample(p[i]+Vector3.forward*e,residual)-Sample(p[i]-Vector3.forward*e,residual))/(2*e);Vector3 target=Vector3.Cross(y,z)*n[i].x+Vector3.Cross(z,x)*n[i].y+Vector3.Cross(x,y)*n[i].z;if(target.sqrMagnitude<1e-12f)throw new InvalidOperationException("151 singular skin normal");dn[i]=target.normalized-n[i];}if(active)for(int i=0;i<p.Length;i++)if(Skin(st[i])&&Socket(st[i]))d[i]=SocketDelta(i,p,st,mesh.triangles,d);mesh.AddBlendShapeFrame(name,100,d,dn,new Vector3[p.Length]);}
var eye=new Vector3[p.Length];var joy=new Vector3[p.Length];mesh.GetBlendShapeFrameVertices(Array.IndexOf(names,"Fcl_EYE_Joy"),0,eye,null,null);mesh.GetBlendShapeFrameVertices(Array.IndexOf(names,"Fcl_ALL_Joy"),0,joy,null,null);
var allowed=new HashSet<int>(Enumerable.Range(0,p.Length).Where(i=>Skin(st[i])&&!Socket(st[i])));
var total=eye.Select((v,i)=>v+joy[i]).ToArray();eye=PreserveSurface(mesh,eye,allowed,"skin eye");total=PreserveSurface(mesh,total,allowed,"skin combined");
for(int i=0;i<p.Length;i++)if(Skin(st[i])&&Socket(st[i])) {
 var column=Enumerable.Range(0,p.Length).Where(k=>Skin(st[k])&&Socket(st[k])&&Mathf.Abs(p[k].x-p[i].x)<.000001f).ToArray();
 int lower=column.OrderBy(k=>p[k].y).First();float high=column.Max(k=>p[k].y),low=p[lower].y;
 float t=high-low<.000001f?.5f:Mathf.InverseLerp(low,high,p[i].y);var band=At(p[i].x,true);
 eye[i]=SocketDelta(i,p,st,mesh.triangles,eye);total[i]=SocketDelta(i,p,st,mesh.triangles,total);
 // Keep the original cheek UV and alpha. Close its own upper and middle rows
 // underneath the measured upper band; only the lower cheek rim stays exposed.
 float lowerEye=low+SocketDelta(lower,p,st,mesh.triangles,eye).y;
 float lowerTotal=low+SocketDelta(lower,p,st,mesh.triangles,total).y;
 eye[i].y=Mathf.Lerp(lowerEye,Mathf.Max(lowerEye+.0001f,band.upper-.001f),t)-p[i].y;
 total[i].y=Mathf.Lerp(lowerTotal,Mathf.Max(lowerTotal+.0001f,band.upper-.001f),t)-p[i].y;
}
joy=total.Select((v,i)=>v-eye[i]).ToArray();var ne=TransportNormals(mesh,eye);var nt=TransportNormals(mesh,total);var nj=nt.Select((v,i)=>v-ne[i]).ToArray();mesh.ClearBlendShapes();foreach(string name in names)mesh.AddBlendShapeFrame(name,100,name=="Fcl_EYE_Joy"?eye:name=="Fcl_ALL_Joy"?total:new Vector3[p.Length],name=="Fcl_EYE_Joy"?ne:name=="Fcl_ALL_Joy"?nt:new Vector3[p.Length],new Vector3[p.Length]);

}

static BrowPoint Curve(BrowPoint[] a,float x){x=Mathf.Clamp(x,a[0].x,a[a.Length-1].x);int k=0;while(k<a.Length-2&&x>a[k+1].x)k++;float f=Mathf.InverseLerp(a[k].x,a[k+1].x,x);return new BrowPoint{x=x,lower=Mathf.Lerp(a[k].lower,a[k+1].lower,f),upper=Mathf.Lerp(a[k].upper,a[k+1].upper,f),zLower=Mathf.Lerp(a[k].zLower,a[k+1].zLower,f),zUpper=Mathf.Lerp(a[k].zUpper,a[k+1].zUpper,f)};}
static void DeformFace(Mesh mesh){var p=mesh.vertices;var colours=mesh.colors;var uv=mesh.uv;var names=Enumerable.Range(0,mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray();var frames=new List<Vector3[]>();for(int shape=0;shape<names.Length;shape++){var d=new Vector3[p.Length];mesh.GetBlendShapeFrameVertices(shape,0,d,null,null);frames.Add(d);}int eye=Array.IndexOf(names,"Fcl_EYE_Joy"),all=Array.IndexOf(names,"Fcl_ALL_Joy"),close=Array.IndexOf(names,"Fcl_EYE_Close");var eyeParts=new HashSet<int>(Enumerable.Range(0,p.Length).Where(i=>frames[close][i].sqrMagnitude>1e-16f&&!Colour(colours[i],33,29,39)));
 foreach(int sign in new[]{-1,1}){var ids=Enumerable.Range(0,p.Length).Where(i=>Mathf.Sign(p[i].x)==sign&&Colour(colours[i],33,29,39)).ToArray();if(ids.Length!=81)throw new InvalidOperationException("151 lash scope "+ids.Length);for(int k=0;k<ids.Length;k++){int i=ids[k];float t=k/27f/2f;t=Mathf.Floor(k/27f)/2f;var a=At(p[i].x,false);var b=At(p[i].x,true);frames[eye][i]=new Vector3(0,Mathf.Lerp(b.lower-a.lower,b.upper-a.upper,t),Mathf.Lerp(b.zLower-a.zLower,b.zUpper-a.zUpper,t));frames[all][i]=Vector3.zero;}}
 foreach(int i in eyeParts){var b=At(p[i].x,true);frames[eye][i]=new Vector3(0,Mathf.Lerp(b.lower,b.upper,.35f)-p[i].y,Sample(p[i],false).z-.012f);frames[all][i]=Vector3.zero;}
 for(int i=0;i<p.Length;i++)if(!eyeParts.Contains(i)&&!Colour(colours[i],33,29,39)){frames[eye][i]=Sample(p[i],false);frames[all][i]=Sample(p[i],true);}
 foreach(int side in new[]{-1,1})foreach(bool upper in new[]{false,true}){var ids=Enumerable.Range(0,p.Length).Where(i=>Mathf.Sign(p[i].x)==side&&(upper?Colour(colours[i],48,45,53):Colour(colours[i],131,102,107))).ToArray();if(ids.Length!=21)throw new InvalidOperationException("151 brow scope");float lo=ids.Min(i=>Mathf.Abs(p[i].x)),hi=ids.Max(i=>Mathf.Abs(p[i].x));var b=config.brows[side<0?0:1];for(int k=0;k<ids.Length;k++){int i=ids[k];float t=Mathf.InverseLerp(lo,hi,Mathf.Abs(p[i].x));var n=Curve(upper?b.upperNeutral:b.lowerNeutral,Mathf.Abs(p[i].x));float sampleX=upper?Mathf.Abs(p[i].x):Mathf.Lerp(b.lowerClosed[0].x,b.lowerClosed[b.lowerClosed.Length-1].x,t);var c=Curve(upper?b.upperClosed:b.lowerClosed,sampleX);float cy=(c.lower+c.upper)*.5f;frames[eye][i]=upper?new Vector3(0,(c.lower+c.upper-n.lower-n.upper)*.5f,(c.zLower+c.zUpper-n.zLower-n.zUpper)*.5f):new Vector3(side*c.x-p[i].x,cy-p[i].y+(k==0?0:(k%2==1?-1:1)*(c.upper-c.lower)*.5f),(c.zLower+c.zUpper-n.zLower-n.zUpper)*.5f);frames[all][i]=Vector3.zero;}}
 for(int k=0;k<config.mouthIndices.Length;k++){int i=config.mouthIndices[k];uv[i]=config.mouthUV[k];colours[i]=Color.white;Vector3 target=p[i]+config.mouthJoy[k];target.x*=1.1f;target.y=.5807f+(target.y-.5817427f)*(.6034f-.5807f)/(.59856951f-.5817427f);frames[all][i]=target-p[i];frames[eye][i]=Vector3.zero;}
 var lashes=new HashSet<int>(Enumerable.Range(0,p.Length).Where(i=>Colour(colours[i],33,29,39)));frames[eye]=PreserveSurface(mesh,frames[eye],lashes,"upper band");
 // ALL_Joy is a complete native smile, as in Ref. EYE_Joy remains independently usable.
 for(int i=0;i<p.Length;i++)frames[all][i]+=frames[eye][i];
 mesh.uv=uv;mesh.colors=colours;mesh.ClearBlendShapes();for(int shape=0;shape<names.Length;shape++)mesh.AddBlendShapeFrame(names[shape],100,frames[shape],new Vector3[p.Length],new Vector3[p.Length]);
}
public static Transform[] Apply(GameObject root) {
 config=JsonUtility.FromJson<Config>(File.ReadAllText("Modeling/RebuiltBigFish/SmileRigControls.json"));
 var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();var face=skins.Single(s=>s.name=="Face");var body=skins.Single(s=>s!=face);
 Calibrate(root,skins);var names=Enumerable.Range(0,face.sharedMesh.blendShapeCount).Select(face.sharedMesh.GetBlendShapeName).ToArray();
 DeformBody(body.sharedMesh,names);DeformFace(face.sharedMesh);
 foreach(var skin in skins){skin.sharedMesh.RecalculateBounds();EditorUtility.SetDirty(skin.sharedMesh);}

 return skins[0].bones;
}
}}
