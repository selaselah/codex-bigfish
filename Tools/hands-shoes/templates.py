"""Independent own-source fingers/palms and rigid forearm cuffs; seven actual-pose guards."""
from pathlib import Path
import json,numpy as np,collections,math
R=Path(__file__).resolve().parents[2];W=R/'work/hands-shoes-195';D=json.loads((R/'work/rebuilt-geometry.json').read_text());M=D['meshes'][0];vec=lambda a,k:np.array([[v[c]for c in k]for v in a],float);P=vec(M['vertices'],'xyz');T=np.array(M['triangles']).reshape(-1,3);N=vec(M['normals'],'xyz');B={q['name']:i for i,q in enumerate(D['bones'])};BP=vec([q['position']for q in D['bones']],'xyz');PA=list(range(len(P)))
def root(i):
 while PA[i]!=i:PA[i]=PA[PA[i]];i=PA[i]
 return i
for a,b,c in T:PA[root(b)]=root(a);PA[root(c)]=root(a)
G=collections.defaultdict(list)
for f,q in enumerate(T):G[root(q[0])].append(f)
SW=np.zeros((len(P),58))
for i in range(len(P)):
 SW[i,M['bone_indices'][i]]+=M['primary_bone_weights'][i];SW[i,M['secondary_bone_indices'][i]]+=1-M['primary_bone_weights'][i]
unit=lambda v:v/max(np.linalg.norm(v),1e-20)
def section(faces,origin,axis,allowed=None,radius=.03,sides=6,up=None):
 up=np.array([0.,1,0])if up is None else up
 u=unit(up-axis*np.dot(up,axis));v=np.cross(axis,u);q=P[T[faces]];d=(q-origin)@axis;segments=[];weights=[]
 for f,(pp,dd)in enumerate(zip(q,d)):
  points=[];ww=[]
  for a,b in [(0,1),(1,2),(2,0)]:
   if (dd[a]<=0 and dd[b]>=0)or(dd[b]<=0 and dd[a]>=0):
    if abs(dd[a]-dd[b])<1e-12:continue
    t=dd[a]/(dd[a]-dd[b]);points.append(pp[a]*(1-t)+pp[b]*t);ww.append(SW[T[faces[f],a]]*(1-t)+SW[T[faces[f],b]]*t)
  if len(points)>=2:segments.append(points[:2]);weights.append(ww[:2])
 if not segments:raise ValueError('No own section')
 ss=np.array(segments);ws=np.array(weights);two=np.stack([(ss-origin)@u,(ss-origin)@v],-1)
 near=np.linalg.norm(two,axis=2).min(1)<radius;two=two[near];ss=ss[near];ws=ws[near]
 if not len(two):raise ValueError('Empty local section')
 close=two[np.linalg.norm(two,axis=2)<radius]
 center=(close.min(0)+close.max(0))*.5
 results=[];infl=[];cross=lambda a,b:a[...,0]*b[...,1]-a[...,1]*b[...,0]
 for j in range(sides):
  ray=np.array([math.cos(j*2*math.pi/sides),math.sin(j*2*math.pi/sides)]);a=two[:,0]-center;e=two[:,1]-two[:,0];den=cross(ray,e);dist=np.divide(cross(a,e),den,out=np.full(len(e),np.inf),where=abs(den)>1e-12);s=np.divide(cross(a,ray),den,out=np.full(len(e),np.inf),where=abs(den)>1e-12);valid=(dist>1e-7)&(dist<radius*2)&(s>=-1e-6)&(s<=1+1e-6)
  if not valid.any():
   radius2=np.max((two.reshape(-1,2)-center)@ray);point=origin+u*(center[0]+ray[0]*radius2)+v*(center[1]+ray[1]*radius2);results.append(point);infl.append(ws.mean((0,1)))
  else:
   dist[~valid]=np.inf;k=np.argmin(dist);results.append(ss[k,0]*(1-s[k])+ss[k,1]*s[k]);infl.append(ws[k,0]*(1-s[k])+ws[k,1]*s[k])
 return np.array(results),np.array(infl),origin+u*center[0]+v*center[1]
parts=[]
def meshpart(name,pp,tt,weights,color):
 pp=np.array(pp);tt=np.array(tt,int).reshape(-1,3);nn=np.zeros_like(pp)
 for q in tt:
  n=np.cross(pp[q[1]]-pp[q[0]],pp[q[2]]-pp[q[0]]);assert n@n>1e-18,(name,q)
  nn[q]+=n
 nn=np.array([unit(n)for n in nn]);parts.append(dict(name=name,vertices=pp.tolist(),triangles=tt.ravel().tolist(),normals=nn.tolist(),weights=np.array(weights).tolist(),color=color));print(name,len(pp),len(tt),flush=True)
for side,component,sc in [('L',11957,-1),('R',12339,1)]:
 prefix='J_Bip_'+side+'_';hand=B[prefix+'Hand'];arm=B[prefix+'LowerArm'];faces=np.array(G[component]);pts=[];facesout=[];weights=[];roots=[]
 def ringstrip(rings,ww,rootcap=True,tip=None):
  start=len(pts);count=len(rings[0]);
  for ring,w in zip(rings,ww):pts.extend(ring);weights.extend([w]*len(ring))
  for row in range(len(rings)-1):
   for j in range(count):
    a=start+row*count+j;b=start+row*count+(j+1)%count;c=a+count;d=b+count;facesout.extend([[a,b,c],[b,d,c]])
  if rootcap:
   i=len(pts);pts.append(rings[0].mean(0));weights.append(ww[0]);facesout.extend([[i,start+(j+1)%count,start+j]for j in range(count)])
  i=len(pts);pts.append(rings[-1].mean(0)if tip is None else tip);weights.append(ww[-1]if tip is None else tipweight);last=start+(len(rings)-1)*count;facesout.extend([[last+j,last+(j+1)%count,i]for j in range(count)])
 for finger in ['Index','Middle','Ring','Little','Thumb']:
  bones=[B[prefix+finger+str(i)]for i in [1,2,3]];axis=unit(BP[bones[2]]-BP[bones[0]]);owned=SW[:,bones].sum(1);ff=np.array([f for f in faces if owned[T[f]].max()>.15]);rings=[]
  for j in range(3):
   origin=BP[bones[j]]+axis*(.004 if j==0 else 0);ring,_,center=section(ff,origin,axis,radius=.023 if finger=='Thumb'else .013,sides=6)
   if j==0:
    ring+=BP[bones[j]]-axis*.002-np.array([0,.002,0])-ring.mean(0)
   else:ring+=BP[bones[j]]-ring.mean(0)
   rings.append(ring)
  ww=[]
  for items in [{hand:.8,bones[0]:.2},{bones[0]:.5,bones[1]:.5},{bones[1]:.5,bones[2]:.5}]:
   q=np.zeros(58)
   for b,w in items.items():q[b]=w
   ww.append(q)
  ids=np.array([i for i in np.unique(T[faces])if SW[i,bones[2]]>.3]);tipdistance=max((P[ids]-BP[bones[2]])@axis);tip=rings[-1].mean(0)+axis*tipdistance;lastcenter=rings[-1].mean(0);rings[-1]=lastcenter+(rings[-1]-lastcenter)*.8+axis*max(0,tipdistance-.0015);ww[-1]=np.eye(58)[bones[2]];tipweight=np.zeros(58);tipweight[bones[2]]=1;roots.append(rings[0]);ringstrip(rings,ww,rootcap=False,tip=tip)
 # Match actual own wrist/palm sections. The front web encloses finger roots.
 axis=np.array([sc,0.,0.]);rootx=abs(BP[hand,0])-.009;frontx=np.mean([abs(BP[B[prefix+f+'1'],0])for f in ['Index','Middle','Ring','Little']]);rings=[];ww=[]
 for k,x in enumerate([rootx,frontx]):
  ring,_,center=section(faces,np.array([sc*x,.496,-.028]),axis,radius=.05,sides=8)
  if k==1:
   rr=np.concatenate(roots[:4]);cy=(rr[:,1].min()+rr[:,1].max())*.5;cz=(rr[:,2].min()+rr[:,2].max())*.5;ry=(rr[:,1].max()-rr[:,1].min())*.5+.0015;rz=(rr[:,2].max()-rr[:,2].min())*.5+.0015
   angles=np.arange(8)*2*np.pi/8;yz=np.stack([np.sign(np.cos(angles))*np.sqrt(abs(np.cos(angles))),np.sign(np.sin(angles))*np.sqrt(abs(np.sin(angles)))],1)
   # Axis-ordered section: +Y then towards cross(axis,+Y).
   ring=np.column_stack([np.full(8,sc*(frontx+.001)),cy+yz[:,0]*ry,cz+sc*yz[:,1]*rz])
  rings.append(ring);q=np.zeros(58);q[hand]=1;ww.append(q)
 ringstrip(rings,ww,rootcap=False,tip=None);meshpart('hand-'+side,pts,facesout,weights,[1,240/255,233/255,1])
poses0=json.loads((W/'deformation-full.json').read_text())['poses'][0];mat0=np.array([[[q[f'e{j}{k}']for k in range(4)]for j in range(4)]for q in poses0['skinMatrices']]);bindP=P.copy();P=vec(poses0['positions'],'xyz')
for side,component,sc in [('L',9559,-1),('R',10195,1)]:
 arm=B['J_Bip_'+side+'_LowerArm'];hand=B['J_Bip_'+side+'_Hand'];elbow=(mat0[arm]@np.r_[BP[arm],1])[:3];wrist=(mat0[hand]@np.r_[BP[hand],1])[:3];axis=unit(wrist-elbow);up=mat0[arm,:3,:3]@np.array([0.,1,0]);inverse=np.linalg.inv(mat0[arm]);faces=np.array(G[component]);pp=[];tt=[];weights=[]
 for k,distance in enumerate([-.045,-.022,.0005]):
  ring,_,center=section(faces,wrist+axis*distance,axis,radius=.07,sides=10,up=up);ring+=np.array([unit(p-center)*.002 for p in ring]);ring=(inverse@np.column_stack([ring,np.ones(10)]).T).T[:,:3];pp.extend(ring);weight=np.zeros(58);weight[arm]=1;weights.extend([weight]*10)
 for row in range(2):
  for j in range(10):
   a=row*10+j;b=row*10+(j+1)%10;c=a+10;d=b+10;tt.extend([[a,b,c],[b,d,c]])
 meshpart('cuff-'+side,pp,tt,weights,[37/255*.98,46/255*.98,93/255*.98,1])
P=bindP
# All new templates use constant source-owned white texel; source-like skin/cloth.
(W/'Templates.json').write_text(json.dumps(parts,separators=(',',':')))
mat=np.array([[[[q[f'e{j}{k}']for k in range(4)]for j in range(4)]for q in s['skinMatrices']]for s in json.loads((W/'deformation-full.json').read_text())['poses']]);reports=[]
for part in parts:
 p=np.array(part['vertices']);n=np.array(part['normals']);t=np.array(part['triangles']).reshape(-1,3);w=np.array(part['weights']);report=[]
 for pi,m in enumerate(mat):
  q=(np.einsum('bij,vj->vbi',m,np.column_stack([p,np.ones(len(p))]))[:,:,:3]*w[:,:,None]).sum(1);nn=(np.einsum('bij,vj->vbi',m[:,:3,:3],n)*w[:,:,None]).sum(1);c=np.cross(q[t[:,1]]-q[t[:,0]],q[t[:,2]]-q[t[:,0]]);bad=(c*nn[t].sum(1)).sum(1)<-1e-16;deg=(c*c).sum(1)<1e-18;report.append([int(sum(bad)),int(sum(deg))])
 assert not any(a or b for a,b in report),(part['name'],report)
 print(part['name'],'posebad/degen',report,flush=True);reports.append({'part':part['name'],'bad':report})
(W/'Template-Guards.json').write_text(json.dumps(reports,indent=2))
