using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;
namespace DesktopFish.Editor {
// Final actual-mesh embroidery replacement. The original base QEM and its
// metric remain untouched. All new cord surfaces retain their real UV/skin.
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
 Func<Vector2,float,Vector3> front=(q,lift)=>{
  float depth=float.NegativeInfinity;foreach(int k in cloth){var a=p[tri[k]];var b=p[tri[k+1]];var d=p[tri[k+2]];float det=(b.x-a.x)*(d.y-a.y)-(b.y-a.y)*(d.x-a.x);if(Mathf.Abs(det)<1e-12f)continue;float u=((q.x-a.x)*(d.y-a.y)-(q.y-a.y)*(d.x-a.x))/det;float v=((b.x-a.x)*(q.y-a.y)-(b.y-a.y)*(q.x-a.x))/det;if(u>=-1e-6f&&v>=-1e-6f&&u+v<=1.000001f)depth=Mathf.Max(depth,a.z+u*(b.z-a.z)+v*(d.z-a.z));}
  if(float.IsNegativeInfinity(depth)||depth<=0)throw new InvalidOperationException("146 cord outside own cloth "+q);return V(q.x,q.y,depth+lift);
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
 Action<Vector2[],float,float> stroke=(path,width,lift)=>{
  int start=np.Count;for(int j=0;j<path.Length;j++){Vector2 direction=path[Mathf.Min(path.Length-1,j+1)]-path[Mathf.Max(0,j-1)];if(direction.sqrMagnitude<1e-14f)throw new InvalidOperationException("146 zero cord tangent");direction.Normalize();Vector2 across=new Vector2(-direction.y,direction.x)*width;np.Add(front(path[j]-across,lift));np.Add(front(path[j]+across,lift));}
  for(int j=0;j<path.Length-1;j++){int a=start+j*2;add(a,a+2,a+1);add(a+1,a+2,a+3);}
 };
 Func<Vector2[],int,Vector2[]> curve=(controls,steps)=>Enumerable.Range(0,steps+1).Select(j=>{float t=j/(float)steps*((controls.Length-1)/3);int s=Mathf.Min((controls.Length-1)/3-1,(int)t);t-=s;float u=1-t;return u*u*u*controls[s*3]+3*u*u*t*controls[s*3+1]+3*u*t*t*controls[s*3+2]+t*t*t*controls[s*3+3];}).ToArray();
 foreach(int sign in new[]{-1,1}){
  Func<Vector2[],Vector2[]> mirror=points=>points.Select(q=>Q(sign*q.x,q.y)).ToArray();
  // Two open cord loops meet at a crossing, with separate free ends.
  stroke(curve(mirror(new[]{Q(.145f,.222f),Q(.135f,.222f),Q(.128f,.239f),Q(.135f,.235f),Q(.142f,.232f),Q(.143f,.226f),Q(.147f,.2225f)}),16),.00070f,.00060f);
  stroke(curve(mirror(new[]{Q(.145f,.222f),Q(.149f,.229f),Q(.160f,.241f),Q(.161f,.233f),Q(.163f,.225f),Q(.154f,.222f),Q(.145f,.2228f)}),16),.00070f,.00065f);
  stroke(curve(mirror(new[]{Q(.144f,.222f),Q(.143f,.215f),Q(.135f,.211f),Q(.133f,.206f)}),10),.00080f,.00065f);
  stroke(curve(mirror(new[]{Q(.147f,.222f),Q(.149f,.216f),Q(.153f,.212f),Q(.151f,.2038f)}),10),.00072f,.00075f);
  stroke(curve(mirror(new[]{Q(.1438f,.2228f),Q(.1445f,.224f),Q(.147f,.223f),Q(.1477f,.2215f)}),5),.00085f,.00105f);
  // Hollow four-point sparks and small stitches, rather than solid stars.
  foreach(var spark in new[]{V(.096f,.212f,.0046f),V(.177f,.224f,.0040f)}) {
   float x=spark.x,y=spark.y,r=spark.z;var path=new[]{Q(x,y+r*.55f),Q(x+r*.25f,y+r*.14f),Q(x+r,y),Q(x+r*.25f,y-r*.14f),Q(x,y-r*.55f),Q(x-r*.25f,y-r*.14f),Q(x-r,y),Q(x-r*.25f,y+r*.14f),Q(x,y+r*.55f)};
   stroke(mirror(path),.00028f,.0006f);
  }
  foreach(var dot in new[]{Q(.073f,.212f),Q(.189f,.229f)})stroke(mirror(new[]{dot-Q(.0005f,0),dot+Q(.0005f,0)}),.00040f,.0006f);
 }
 var part=new Mesh();part.SetVertices(np);part.SetTriangles(nt,0);part.RecalculateNormals();
 var used=keep.Distinct().OrderBy(i=>i).ToArray();var map=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)map[used[i]]=i;
 var pp=used.Select(i=>p[i]).ToList();var nn=used.Select(i=>n[i]).ToList();var st=used.Select(i=>uv[i]).ToList();var cc=used.Select(i=>c[i]).ToList();var ww=used.Select(i=>w[i]).ToList();var tt=keep.Select(i=>map[i]).ToList();int offset=pp.Count;
 pp.AddRange(np);nn.AddRange(part.normals);st.AddRange(Enumerable.Repeat(uv[border[0]],np.Count));cc.AddRange(Enumerable.Repeat(new Color(226/255f,196/255f,125/255f,1),np.Count));ww.AddRange(Enumerable.Repeat(new BoneWeight{boneIndex0=hips,boneIndex1=hips,weight0=1},np.Count));tt.AddRange(nt.Select(i=>i+offset));
 var mesh=new Mesh{name=old.name};mesh.SetVertices(pp);mesh.SetNormals(nn);mesh.SetUVs(0,st);mesh.SetColors(cc);mesh.boneWeights=ww.ToArray();mesh.bindposes=old.bindposes;mesh.SetTriangles(tt,0);mesh.RecalculateBounds();
 File.WriteAllText("work/rebuilt-skirt-cord.json","{\"method\":\"actual lower gold replacement after unchanged base QEM/135/141/138; one fine border, small independent looped cord knots, no stems/leaves\",\"outside_actual_vertices\":"+used.Length+",\"removed_actual_vertices\":764,\"removed_actual_triangles\":728,\"new_vertices\":"+np.Count+",\"new_triangles\":"+nt.Count/3+",\"border_full_width_m\":0.0013,\"new_surfaces_retained_complete\":true,\"actual_own_cloth_projected\":true,\"source_pixels_UV_topology_weights_copied\":false}");
 UnityEngine.Object.DestroyImmediate(part);UnityEngine.Object.DestroyImmediate(old);return mesh;
}
}}
