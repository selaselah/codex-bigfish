"""Bake our own full-detail rear underlayers onto three curved alpha cards.

No reference asset is read. Run after exporting the selected own high/low models.
"""
from pathlib import Path
import json,struct,hashlib,collections
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
full=json.loads((ROOT/'work/rebuilt-geometry.json').read_text())['meshes'][0]
vec=lambda a,k:np.array([[v[c]for c in k]for v in a],float)
p=vec(full['vertices'],'xyz');uv=vec(full['uv'],'xy');color=vec(full['colors'],'rgba');tri=np.array(full['triangles']).reshape(-1,3)
parent=list(range(len(p)))
def find(i):
 while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
 return i
for a,b,c in tri:parent[find(b)]=find(a);parent[find(c)]=find(a)
groups=collections.defaultdict(list)
for f,t in enumerate(tri):groups[find(t[0])].append(f)
parts=[[1540],[1694,1772,1850,1927],[1994]]
recipe=json.loads((ROOT/'Assets/DesktopFish/Editor/FishLowPoly5KRecipe.json').read_text())
# Reconstruct the frozen193 scalp overlay from own source + source-index recipe.
# This avoids depending on a cached native asset from a previous trial.
used=np.array(recipe['sourceIndices']);oldp=p[used].astype(np.float32);oldn=vec(full['normals'],'xyz')[used].astype(np.float32);oldtri=np.array(recipe['triangles']).reshape(-1,3);oldst=uv[used]*[1024,2048];cent=oldp[oldtri].mean(1)
head=(oldst[:,0]>=647.999)&(oldst[:,0]<=752.001)&(oldst[:,1]>=783.999)&(oldst[:,1]<=1080.001)
neck=np.array([full['colors'][i]['r']==1 and abs(full['colors'][i]['g']-240/255)<1e-6 and full['bone_indices'][i]==3 and full['secondary_bone_indices'][i]==4 for i in used])
angle=abs(np.arctan2(cent[:,0],cent[:,2]-.005))*180/np.pi
selected=(neck[oldtri].all(1)&(cent[:,1]>.60))|(head[oldtri].all(1)&(cent[:,1]>.64)&(cent[:,1]<.9)&(oldp[oldtri,1].min(1)>.60)&(angle>55))
cap=set(np.unique(oldtri[selected]));outside=set(np.unique(oldtri[~selected]));edges=collections.Counter(tuple(sorted((int(q[j]),int(q[(j+1)%3]))))for q in oldtri[selected]for j in range(3));boundary={i for e,n in edges.items()if n==1 for i in e};mapping=list(range(len(oldp)));target={}
assert len(cap)==80 and sum(selected)==85
for i in sorted(cap):
 if i in outside:target[i]=len(mapping);mapping.append(i)
 else:target[i]=i
lowp=oldp[mapping].copy();lowtri=oldtri.copy()
for i in cap:
 if i not in boundary:lowp[target[i]]+=oldn[i]*np.float32(.002)
for f in np.flatnonzero(selected):lowtri[f]=[target[i]for i in lowtri[f]]
source_ids=used[mapping]
remove=[f for f,t in enumerate(lowtri)if find(source_ids[t[0]])in sum(parts,[])]
assert len(remove)==61
paint=np.array(Image.open(ROOT/'Assets/DesktopFish/Rebuilt/HairPaint.png').convert('RGBA'))[::-1]/255.
def linear(a):return np.where(a<=.04045,a/12.92,((a+.055)/1.055)**2.4)
def srgb(a):return np.where(a<=.0031308,a*12.92,1.055*np.maximum(a,0)**(1/2.4)-.055)
def sample(st):
 x=np.clip(st[...,0]*paint.shape[1]-.5,0,paint.shape[1]-1);y=np.clip(st[...,1]*paint.shape[0]-.5,0,paint.shape[0]-1)
 a=x.astype(int);b=y.astype(int);c=np.minimum(a+1,paint.shape[1]-1);d=np.minimum(b+1,paint.shape[0]-1);u=x-a;v=y-b
 return paint[b,a]*(1-u[...,None])*(1-v[...,None])+paint[b,c]*u[...,None]*(1-v[...,None])+paint[d,a]*(1-u[...,None])*v[...,None]+paint[d,c]*u[...,None]*v[...,None]
atlas=np.zeros((1024,1536,4),np.uint8);vertices=[];normals=[];st=[];indices=[];reports=[]
cross2=lambda a,b:a[...,0]*b[...,1]-a[...,1]*b[...,0]
ROWS=4
for card,components in enumerate(parts):
 faces=np.array(sum([groups[k]for k in components],[]));t=tri[faces];v=p[np.unique(t)]
 for i in np.unique(t):
  assert full['bone_indices'][i]==4 and full['primary_bone_weights'][i]==1
  assert all(np.linalg.norm(list(s['deltas'][i].values()))==0 for s in full['shapes'])
 lo=v[:,:2].min(0)-.004;hi=v[:,:2].max(0)+.004
 W,H=496,1008;scale=2;ww,hh=W*scale,H*scale
 zbuf=np.full((hh,ww),np.inf);rgba=np.zeros((hh,ww,4))
 for q in t:
  pts=p[q,:2];grid=(pts-lo)/(hi-lo)*[ww,hh]-.5;mn=np.maximum(0,np.ceil(grid.min(0)).astype(int));mx=np.minimum([ww-1,hh-1],np.floor(grid.max(0)).astype(int))
  if np.any(mx<mn):continue
  yy,xx=np.mgrid[mn[1]:mx[1]+1,mn[0]:mx[0]+1];point=np.stack([xx,yy],-1);a,b,c=grid;den=cross2(b-a,c-a)
  if abs(den)<1e-12:continue
  vb=cross2(point-a,c-a)/den;vc=cross2(b-a,point-a)/den;va=1-vb-vc;inside=(va>=-1e-8)&(vb>=-1e-8)&(vc>=-1e-8);weight=np.stack([va,vb,vc],-1);depth=weight@p[q,2];tex=sample(weight@uv[q]);ink=weight@color[q];inside&=(tex[...,3]>.5)
  view=zbuf[mn[1]:mx[1]+1,mn[0]:mx[0]+1];take=inside&(depth<view);view[take]=depth[take]
  value=np.concatenate([srgb(linear(tex[...,:3])*linear(ink[...,:3])),np.ones((*depth.shape,1))],-1);dest=rgba[mn[1]:mx[1]+1,mn[0]:mx[0]+1];dest[take]=value[take]
 # Average coverage and premultiplied RGB independently; then bleed RGB beneath
 # transparent pixels, keeping the alpha untouched for filtering at mask edges.
 small=rgba.reshape(H,scale,W,scale,4).mean((1,3));alpha=small[...,3].copy();rgb=np.divide(small[...,:3],alpha[...,None],out=np.zeros_like(small[...,:3]),where=alpha[...,None]>0)
 known=alpha>0
 for _ in range(8):
  total=np.zeros_like(rgb);count=np.zeros_like(alpha)
  for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
   shifted=np.roll(np.roll(known,dy,0),dx,1);values=np.roll(np.roll(rgb,dy,0),dx,1);total+=values*shifted[...,None];count+=shifted
  fill=(~known)&(count>0);rgb[fill]=total[fill]/count[fill,None];known|=fill
 image=np.clip(np.concatenate([rgb,alpha[...,None]],-1)*255+.5,0,255).astype(np.uint8);px=card*512+8;py=8;atlas[py:py+H,px:px+W]=image
 # Eight cells per card: four longitudinal spans and two transverse spans.
 # Sample the actual own surface depth; nearest projected edge extends the
 # transparent card border without changing the baked visible outline.
 def depth_at(xy):
  bestdist=np.inf;bestz=0.;inside=[]
  for q in t:
   a,b,c=p[q,:2];den=cross2(b-a,c-a)
   if abs(den)>1e-14:
    vb=cross2(xy-a,c-a)/den;vc=cross2(b-a,xy-a)/den;va=1-vb-vc
    if min(va,vb,vc)>=-1e-8:inside.append(float(np.array([va,vb,vc])@p[q,2]))
   for ia,ib in [(q[0],q[1]),(q[1],q[2]),(q[2],q[0])]:
    e=p[ib,:2]-p[ia,:2];u=np.clip(np.dot(xy-p[ia,:2],e)/max(np.dot(e,e),1e-20),0,1);d=np.sum((xy-p[ia,:2]-u*e)**2)
    if d<bestdist:bestdist=d;bestz=p[ia,2]*(1-u)+p[ib,2]*u
  return min(inside)if inside else bestz
 start=len(vertices)
 for row in range(ROWS+1):
  for col in range(3):
   u=col/2;v0=row/ROWS;xy=lo+(hi-lo)*[u,v0];vertices.append([*xy,depth_at(xy)]);st.append([(px+u*W)/1536,(py+v0*H)/1024])
 for row in range(ROWS):
  for col in range(2):
   a=start+row*3+col;indices.extend([a,a+1,a+3,a+1,a+4,a+3])
 reports.append(dict(components=components,full_triangles=len(t),card_triangles=ROWS*4,xy_min=lo.tolist(),xy_max=hi.tolist(),alpha_visible_pixels=int(np.sum(alpha>=.5))))
pp=np.array(vertices);tt=np.array(indices).reshape(-1,3);nn=np.zeros_like(pp)
for q in tt:
 n=np.cross(pp[q[1]]-pp[q[0]],pp[q[2]]-pp[q[0]]);assert np.linalg.norm(n)>1e-9 and n[2]>0
 nn[q]+=n
nn/=np.linalg.norm(nn,axis=1)[:,None]
out=ROOT/'Assets/DesktopFish/Editor/FishRearAlphaCards.json'
def objects(values,keys):return[dict(zip(keys,map(float,v)))for v in values]
sig=hashlib.sha256(lowp.astype('<f4').tobytes()+lowtri.astype('<i4').tobytes()).hexdigest()
if out.exists():assert json.loads(out.read_text())['bodyBeforePositionIndexSHA256']==sig,'Frozen scalp reconstruction differs from native'
out.write_text(json.dumps(dict(bodyBeforePositionIndexSHA256=sig,removeTriangles=remove,vertices=objects(vertices,'xyz'),normals=objects(nn,'xyz'),uv=objects(st,'xy'),triangles=indices),separators=(',',':')))
Image.fromarray(atlas[::-1]).save(ROOT/'Assets/DesktopFish/Rebuilt5K/HairCardsPaint.png')
report=dict(source='Own151 full-detail mesh and HairPaint only',removed_actual_triangles=61,new_card_triangles=ROWS*12,total_triangles=4993-61+ROWS*12,cards=reports,texture_size=[1536,1024],alpha_filter='2x2 area coverage,8px RGB-only dilation,mask .5')
(ROOT/'work/hair-cards-194/Bake.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
