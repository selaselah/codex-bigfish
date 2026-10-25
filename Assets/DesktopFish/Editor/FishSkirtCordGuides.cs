using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;
namespace DesktopFish.Editor {
// Original printed artwork, explicitly requested by the user. The unchanged
// base QEM runs first; all new actual UV/skin surfaces are kept afterward.
public static class FishSkirtCordGuides {
static Vector3 V(float x,float y,float z=0)=>new Vector3(x,y,z);
static Vector2 Q(float x,float y)=>new Vector2(x,y);
public static Mesh Apply(Mesh old,int hips) {
 var p=old.vertices;var n=old.normals;var uv=old.uv;var c=old.colors;var w=old.boneWeights;var tri=old.triangles;
 var selected=Enumerable.Range(0,p.Length).Select(i=>w[i].boneIndex0==hips&&w[i].weight0>.999f&&p[i].y<.30f&&Mathf.Abs(uv[i].x-.995f)<1e-6f&&c[i].r>c[i].b*1.15f&&c[i].g>c[i].b*1.15f).ToArray();
 var keep=new List<int>();int removed=0;var adjacent=new Dictionary<int,List<int>>();
 for(int k=0;k<tri.Length;k+=3) {
  int a=tri[k],b=tri[k+1],d=tri[k+2];bool on=selected[a]||selected[b]||selected[d];
  if(on) {if(!(selected[a]&&selected[b]&&selected[d]))throw new InvalidOperationException("146 mixed old gold boundary");removed++;foreach(int x in new[]{a,b,d}){if(!adjacent.ContainsKey(x))adjacent[x]=new List<int>();adjacent[x].AddRange(new[]{a,b,d});}}
  else keep.AddRange(new[]{a,b,d});
 }
 if(removed!=728||selected.Count(b=>b)!=764)throw new InvalidOperationException("146 actual old gold scope changed");
 var visited=new HashSet<int>();var components=new List<int[]>();foreach(int i in adjacent.Keys){if(visited.Contains(i))continue;var queue=new Queue<int>();var group=new List<int>();queue.Enqueue(i);visited.Add(i);while(queue.Count>0){int j=queue.Dequeue();group.Add(j);foreach(int v in adjacent[j])if(visited.Add(v))queue.Enqueue(v);}components.Add(group.ToArray());}
 var border=components.Where(g=>g.Length==146).OrderBy(g=>g.Average(i=>p[i].y)).First();
 const int cols=72;var ys=new float[cols];var borderSet=new HashSet<int>(border);
 for(int j=0;j<cols;j++) {
  float theta=j*Mathf.PI*2/cols;var ray=V(Mathf.Sin(theta),0,Mathf.Cos(theta));var side=V(ray.z,0,-ray.x);var hits=new List<float>();
  for(int k=0;k<tri.Length;k+=3)if(borderSet.Contains(tri[k])&&borderSet.Contains(tri[k+1])&&borderSet.Contains(tri[k+2]))for(int e=0;e<3;e++) {
   var a=p[tri[k+e]];var b=p[tri[k+(e+1)%3]];float da=Vector3.Dot(a,side),db=Vector3.Dot(b,side);
   if(Mathf.Abs(da)<1e-8f&&Vector3.Dot(a,ray)>0)hits.Add(a.y);
   if((da<=0&&db>0)||(db<=0&&da>0)){var q=Vector3.Lerp(a,b,da/(da-db));if(Vector3.Dot(q,ray)>0)hits.Add(q.y);}
  }
  if(hits.Count<2)throw new InvalidOperationException("146 own border section missing "+j);ys[j]=(hits.Min()+hits.Max())*.5f;
 }
 var cloth=new List<int>();for(int k=0;k<tri.Length;k+=3){var ids=new[]{tri[k],tri[k+1],tri[k+2]};if(ids.All(i=>w[i].boneIndex0==hips&&w[i].weight0>.999f&&Mathf.Abs(uv[i].x-.995f)<1e-6f&&c[i].r<.22f&&c[i].g<.28f&&c[i].b<.46f&&p[i].y>.16f&&p[i].y<.39f))cloth.Add(k);}
 if(cloth.Count<100)throw new InvalidOperationException("146 actual own skirt projection scope");
 Func<float,float,Vector3> radial=(theta,y)=>{
  var ray=V(Mathf.Sin(theta),0,Mathf.Cos(theta));var side=V(ray.z,0,-ray.x);float radius=float.NegativeInfinity;
  foreach(int k in cloth){var cut=new List<Vector3>();for(int e=0;e<3;e++){var a=p[tri[k+e]];var b=p[tri[k+(e+1)%3]];if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))cut.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));}if(cut.Count!=2)continue;float da=Vector3.Dot(cut[0],side),db=Vector3.Dot(cut[1],side);if(Mathf.Abs(da-db)<1e-10f)continue;float t=da/(da-db);if(t>=0&&t<=1)radius=Mathf.Max(radius,Vector3.Dot(Vector3.Lerp(cut[0],cut[1],t),ray));}
  if(float.IsNegativeInfinity(radius))throw new InvalidOperationException("146 border outside own cloth");return ray*(radius+.00055f)+Vector3.up*y;
 };
 var np=new List<Vector3>();var nt=new List<int>();
 Action<int,int,int> add=(a,b,d)=>{var cross=Vector3.Cross(np[b]-np[a],np[d]-np[a]);if(cross.sqrMagnitude<=1e-18f)throw new InvalidOperationException("146 degenerate cord");var mid=(np[a]+np[b]+np[d])/3;if(Vector3.Dot(cross,V(mid.x,0,mid.z))<0)nt.AddRange(new[]{a,d,b});else nt.AddRange(new[]{a,b,d});};
 // One 1.3mm border, centered on our original lower hem; no parallel band.
 for(int row=0;row<2;row++)for(int j=0;j<=cols;j++)np.Add(radial(j*Mathf.PI*2/cols,ys[j%cols]+(row==0?-.00065f:.00065f)));
 // A thin ribbon must clear the *actual piecewise-planar* cloth between
 // stations as well as at its vertices. Lift only the necessary adjacent
 // columns; changing shading normals cannot repair an embedded line.
 for(int pass=0;pass<4;pass++)for(int j=0;j<cols;j++) {
  float lift=0;for(int k=1;k<8;k++)for(int row=0;row<3;row++) {
   float u=k/8f,v=row*.5f;var a=Vector3.Lerp(np[j],np[j+cols+1],v);var b=Vector3.Lerp(np[j+1],np[j+cols+2],v);var q=Vector3.Lerp(a,b,u);
   float theta=Mathf.Atan2(q.x,q.z);var actual=radial(theta,q.y);lift=Mathf.Max(lift,new Vector2(actual.x,actual.z).magnitude-new Vector2(q.x,q.z).magnitude);
  }
  if(lift>0)foreach(int id in new[]{j,j+1,j+cols+1,j+cols+2}) {var q=np[id];q+=V(q.x,0,q.z).normalized*(lift+.00004f);np[id]=q;}
  // The periodic seam shares the same physical positions.
  np[cols]=np[0];np[2*cols+1]=np[cols+1];
 }
 for(int j=0;j<cols;j++){int a=j,b=j+cols+1;add(a,a+1,b);add(a+1,b+1,b);}
 // Original artwork is mapped onto exact clipped triangles of our own
 // current skirt, so the complete printed surface clears the cloth rather
 // than only its sample vertices. No reference mesh/UV/topology is imported.
 var newUV=new List<Vector2>();for(int row=0;row<2;row++)for(int j=0;j<=cols;j++)newUV.Add(FishOriginalSkirtPattern.UV(9,4.5f,row==0?5.5f:4.5f));
 Action<int,float,float,float,float,bool> patch=(cell,x0,yTop,width,height,back)=>{
  float pixel=FishOriginalSkirtPattern.PixelSize,x1=x0+width*pixel,yBottom=yTop-height*pixel;
  Func<List<Vector3>,int,float,bool,List<Vector3>> clip=(input,axis,bound,greater)=>{
   var output=new List<Vector3>();if(input.Count==0)return output;var a=input[input.Count-1];bool ai=greater?a[axis]>=bound:a[axis]<=bound;
   foreach(var b in input){bool bi=greater?b[axis]>=bound:b[axis]<=bound;if(ai!=bi){float u=(bound-a[axis])/(b[axis]-a[axis]);var v=Vector3.LerpUnclamped(a,b,u);v[axis]=bound;output.Add(v);}if(bi)output.Add(b);a=b;ai=bi;}return output;
  };
  int hits=0;foreach(int k in cloth) {
   if(((p[tri[k]].z+p[tri[k+1]].z+p[tri[k+2]].z)>0)==back)continue;
   var poly=new List<Vector3>{p[tri[k]],p[tri[k+1]],p[tri[k+2]]};
   poly=clip(poly,0,x0,true);poly=clip(poly,0,x1,false);poly=clip(poly,1,yBottom,true);poly=clip(poly,1,yTop,false);
   var clean=new List<Vector3>();foreach(var v in poly)if(clean.Count==0||(v-clean[clean.Count-1]).sqrMagnitude>1e-20f)clean.Add(v);
   if(clean.Count>1&&(clean[0]-clean[clean.Count-1]).sqrMagnitude<=1e-20f)clean.RemoveAt(clean.Count-1);if(clean.Count<3)continue;
   int start=np.Count;foreach(var v in clean){np.Add(v+Vector3.forward*(back?-.0006f:.0006f));newUV.Add(FishOriginalSkirtPattern.UV(cell,(v.x-x0)/pixel,(yTop-v.y)/pixel));}
   for(int i=1;i<clean.Count-1;i++)add(start,start+i,start+i+1);hits++;
  }
  if(hits==0)throw new InvalidOperationException("147 original artwork misses actual own cloth "+cell);
 };
 float px=FishOriginalSkirtPattern.PixelSize;
 // The native 96x72 area includes clear pixels around the original89x68 bow.
 // The upper/lower atlas charts overlap in their8px *native* padding.
 foreach(bool back in new[]{false,true})foreach(int sign in new[]{-1,1}) {
  float center=sign*.145f,anchor=sign>0?253.5f:1797.5f;
  float sourceX=sign>0?205:1750;int top=sign>0?0:2,bottom=top+1;
  patch(top,center+(sourceX-anchor)*px,.224f+(1780-1757)*px,96,36,back);
  patch(bottom,center+(sourceX-anchor)*px,.224f+(1780-1793)*px,96,36,back);
  if(sign>0) {
   patch(4,center+(159-anchor)*px,.224f+(1780-1794)*px,25,20,back);
   patch(5,center+(315-anchor)*px,.224f+(1780-1776)*px,25,20,back);
   patch(8,center+(131-anchor)*px,.224f+(1780-1800)*px,10,10,back);
   patch(8,center+(359-anchor)*px,.224f+(1780-1774)*px,10,10,back);
  }else {
   patch(6,center+(1867-anchor)*px,.224f+(1780-1794)*px,25,20,back);
   patch(7,center+(1711-anchor)*px,.224f+(1780-1776)*px,25,20,back);
   patch(8,center+(1910-anchor)*px,.224f+(1780-1800)*px,10,10,back);
   patch(8,center+(1682-anchor)*px,.224f+(1780-1774)*px,10,10,back);
  }
 }
 if(np.Count!=newUV.Count)throw new InvalidOperationException("147 original chart mapping count");
 var part=new Mesh();part.SetVertices(np);part.SetTriangles(nt,0);part.RecalculateNormals();
 var used=keep.Distinct().OrderBy(i=>i).ToArray();var map=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)map[used[i]]=i;
 var pp=used.Select(i=>p[i]).ToList();var nn=used.Select(i=>n[i]).ToList();var st=used.Select(i=>uv[i]).ToList();var cc=used.Select(i=>c[i]).ToList();var ww=used.Select(i=>w[i]).ToList();var tt=keep.Select(i=>map[i]).ToList();int offset=pp.Count;
 pp.AddRange(np);nn.AddRange(part.normals);st.AddRange(newUV);cc.AddRange(Enumerable.Repeat(Color.white,np.Count));ww.AddRange(Enumerable.Repeat(new BoneWeight{boneIndex0=hips,boneIndex1=hips,weight0=1},np.Count));tt.AddRange(nt.Select(i=>i+offset));
 var mesh=new Mesh{name=old.name};mesh.SetVertices(pp);mesh.SetNormals(nn);mesh.SetUVs(0,st);mesh.SetColors(cc);mesh.boneWeights=ww.ToArray();mesh.bindposes=old.bindposes;mesh.SetTriangles(tt,0);mesh.RecalculateBounds();
 File.WriteAllText("work/rebuilt-skirt-cord.json","{\"method\":\"explicit user-authorized original skirt motif RGB copied without repaint or resampling; alpha-only foreground separation, original highlights and silhouettes at1.25scale, exact own-cloth clipped surface\",\"outside_actual_vertices\":"+used.Length+",\"removed_actual_vertices\":764,\"removed_actual_triangles\":728,\"new_vertices\":"+np.Count+",\"new_triangles\":"+nt.Count/3+",\"original_art_scale\":1.25,\"bow_instances\":4,\"front_and_back_printed\":true,\"border_full_width_m\":0.0013,\"source_art_RGB_copied\":true,\"source_mesh_UV_topology_weights_copied\":false,\"new_surfaces_retained_complete\":true}");
 UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(old);return mesh;
}
}}
