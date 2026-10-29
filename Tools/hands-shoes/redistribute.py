"""Reallocate the frozen 194 LOD to shoes and hands without exceeding 5000.

Offline authoring requires local own-source exports, the 194 snapshot and the
216-view visibility/actual-pose measurements in work/hands-shoes-195. Native
rebuilds use the checked-in recipe directly and verify its exact baseline hash.
No reference asset is read. Every source palette value is retained; numerical
attribute round-trip noise is snapped before checking unchanged interior faces.
"""
from pathlib import Path
import runpy
runpy.run_path(str(Path(__file__).with_name('templates.py')),run_name='__main__')
# fund.py
from pathlib import Path
import json,numpy as np,collections,struct,subprocess,hashlib
R=Path(__file__).resolve().parents[2];W=R/'work/hands-shoes-195';D=json.loads((R/'work/rebuilt-geometry.json').read_text());H=D['meshes'][0];L=json.loads((W/'before/work/rebuilt-geometry-5k.json').read_text())['meshes'][0]
v=lambda x,k:np.array([[q[c]for c in k]for q in x],np.float32)
p=v(H['vertices'],'xyz');t=np.array(H['triangles']).reshape(-1,3);pa=list(range(len(p)))
def root(i):
 while pa[i]!=i:pa[i]=pa[pa[i]];i=pa[i]
 return i
for a,b,c in t:pa[root(b)]=root(a);pa[root(c)]=root(a)
g=collections.defaultdict(list)
for f,q in enumerate(t):g[root(q[0])].append(f)
src=np.array(json.loads((R/'Assets/DesktopFish/Editor/FishLowPoly5KRecipe.json').read_text())['sourceIndices'])[np.array(json.loads((R/'work/scalp-cover-193/Scalp-Coverage.json').read_text())['originalLowIndices'])[json.loads((R/'work/hair-cards-194/Retained-Body-Indices.json').read_text())]]
lp=v(L['vertices'],'xyz');ln=v(L['normals'],'xyz');uv=v(L['uv'],'xy');col=v(L['colors'],'rgba');lt=np.array(L['triangles'],np.uint32).reshape(-1,3);owner=np.array([root(i)for i in src]);fo=owner[lt[:,0]]
shoes=[9912,10004,10005,10014,10093,10143,10169,10185,10551,10646,10647,10656,10735,10785,10811,10827];bands=[9743,9769,10382,10408];remove=set([11957,12339]+shoes+bands+[12968]);scope=np.isin(fo,list(remove));rest=lt[~scope]
print('SHOES',sum(len(g[i])for i in shoes),np.isin(fo,shoes).sum(),'bands',np.isin(fo,bands).sum(),'remove',scope.sum(),'rest',len(rest),flush=True)
locked=np.zeros(len(lp),bool)
for s in L['shapes']:
 for field in ['deltas','normal_deltas']:
  if field in s:locked|=np.any(v(s[field],'xyz')!=0,1)
locked|=np.isin(owner,[31,8054,8101,8493,11775,9559,10195])
scalp=set(json.loads((R/'work/scalp-cover-193/Scalp-Coverage.json').read_text())['capIndices']);m194=json.loads((R/'work/hair-cards-194/Retained-Body-Indices.json').read_text());locked[[i for i,j in enumerate(m194)if j in scalp]]=True
rigid=np.array(L['primary_bone_weights'])>.999999
poseD=json.loads((W/'deformation-full.json').read_text())['poses'];mat=np.array([[[[x['e'+str(a)+str(b)]for b in range(4)]for a in range(4)]for x in q['skinMatrices']]for q in poseD],np.float64);sw=np.zeros((len(lp),58))
for i,sk in enumerate(L['skin4']):
 for bi,bw in zip(sk['indices'],sk['weights']):sw[i,bi]+=bw
blend=np.einsum('vb,pbij->pvij',sw,mat);ph=np.concatenate([lp,np.ones((len(lp),1))],1);poseP=np.einsum('pvij,vj->pvi',blend,ph)[:,:,:3];poseN=np.einsum('pvij,vj->pvi',blend[:,:,:3,:3],ln)
allnew=[];pp=lp.copy();nn=ln.copy();uu=uv.copy();cc=col.copy();report=[]
for key in sorted(set(fo[~scope])):
 faces=rest[owner[rest[:,0]]==key];ids=np.unique(faces)
 # Protect facial/scalp records; actual seven-pose LBS guards also cover blended parts.
 if locked[ids].any() or len(faces)<20:
  allnew.append(faces);continue
 ix=np.full(len(lp),-1);ix[ids]=np.arange(len(ids));idx=ix[faces].astype('<u4');attrs=np.concatenate([ln[ids],uv[ids],col[ids]]+[poseP[j,ids]-lp[ids]for j in range(7)],1).astype('<f4');locks=np.zeros(len(ids),np.uint8)
 # Keep each component's actual silhouette support and surface borders.
 for axis in range(3):locks[np.argmin(lp[ids,axis])]=1;locks[np.argmax(lp[ids,axis])]=1
 inp=W/'fund-input.bin';out=W/'fund-output.bin';target=max(12,int(len(faces)*.65))*3
 for attempt in range(33):
  inp.write_bytes(struct.pack('<5IfI',0x35444f4c,len(ids),idx.size,30,target,.003,4|32|64)+lp[ids].astype('<f4').tobytes()+attrs.tobytes()+np.array([.01]*3+[.15]*2+[.03]*4+[1]*21,'<f4').tobytes()+locks.tobytes()+idx.tobytes())
  cp=subprocess.run([str(R/'Tools/bin/meshopt-update'),str(inp),str(out)],capture_output=True,text=True,check=True);blob=out.read_bytes();magic,count,err=struct.unpack_from('<IIf',blob);assert magic==0x35444f4c and err<=.00300001
  newidx=np.frombuffer(blob,'<u4',count=count,offset=12).reshape(-1,3);newp=np.frombuffer(blob,'<f4',count=len(ids)*3,offset=12+count*4).reshape(-1,3);newa=np.frombuffer(blob,'<f4',offset=12+count*4+len(ids)*12).reshape(-1,30)
  xp=np.concatenate([newp,np.ones((len(ids),1))],1);qp=np.einsum('pvij,vj->pvi',blend[:,ids],xp)[:,:,:3];qn=np.einsum('pvij,vj->pvi',blend[:,ids,:3,:3],newa[:,:3]);cross=np.cross(qp[:,newidx[:,1]]-qp[:,newidx[:,0]],qp[:,newidx[:,2]]-qp[:,newidx[:,0]]);bad=np.any((np.sum(cross*cross,2)<1e-18)|(np.sum(cross*qn[:,newidx].sum(2),2)<-1e-16),0)
  oldset={tuple(int(i)for i in q)for q in idx};unchanged=np.array([tuple(int(i)for i in q)in oldset and np.array_equal(newp[q],lp[ids[q]]) and np.array_equal(newa[q,:3],ln[ids[q]])for q in newidx]);bad&=~unchanged
  if not bad.any():break
  affected=set(newidx[bad].ravel());mask=np.array([any(i in affected for i in q)for q in idx]);lockids=np.unique(idx[mask]);previous=locks.copy();locks[lockids]=1
  assert not np.array_equal(locks,previous),'pose protection stalled'
 else:raise AssertionError('pose protection retries')
 changed=np.any(newp!=lp[ids],1)|np.any(newa!=attrs,1);newn=newa[:,:3].copy();newn[changed]/=np.linalg.norm(newn[changed],axis=1)[:,None]
 pp[ids]=newp;nn[ids]=newn;uu[ids]=newa[:,3:5];cc[ids]=newa[:,5:9];allnew.append(ids[newidx]);report.append(dict(component=int(key),before=len(faces),after=len(newidx),changed=int(changed.sum()),error=float(err),bbox=np.round(np.ptp(lp[ids],axis=0),3).tolist()))
newt=np.concatenate(allnew);print('FUND',len(rest),'->',len(newt),'saving',len(rest)-len(newt),flush=True)
print(json.dumps(report,indent=2));np.savez(W/'Funding.npz',positions=pp,normals=nn,uv=uu,colors=cc,triangles=newt,source=src,owner=owner);(W/'Funding.json').write_text(json.dumps(dict(before=len(rest),after=len(newt),components=report),indent=2))


# snap.py
from pathlib import Path
import json,numpy as np
R=Path(__file__).resolve().parents[2];W=R/'work/hands-shoes-195';F=dict(np.load(W/'Funding.npz'));B=json.load(open(W/'before/work/rebuilt-geometry-5k.json'))['meshes'][0]
for k,f,axes,eps in [('positions','vertices','xyz',1e-7),('normals','normals','xyz',1e-6),('uv','uv','xy',1e-7),('colors','colors','rgba',1e-6)]:
 old=np.array([[x[c]for c in axes]for x in B[f]],np.float32);same=np.linalg.norm(F[k]-old,axis=1)<=eps;F[k][same]=old[same]
# Restore the crown before its separate 18-face reduction; avoid applying the
# broader funding target to this already repaired silhouette.
lt=np.array(B['triangles']).reshape(-1,3);owner=F['owner'];new=F['triangles'];origcrown=lt[owner[lt[:,0]]==2428];newcrown=new[owner[new[:,0]]==2428];print('crownrestore',len(origcrown)-len(newcrown));F['triangles']=np.concatenate([new[owner[new[:,0]]!=2428],origcrown]);ids=np.unique(origcrown)
for k,f,axes in [('positions','vertices','xyz'),('normals','normals','xyz'),('uv','uv','xy'),('colors','colors','rgba')]:F[k][ids]=np.array([[B[f][i][c]for c in axes]for i in ids],np.float32)
np.savez(W/'Funding.npz',**F)


# crown-budget.py
F=dict(np.load(W/'Funding.npz'));faces=lt[fo==2428];ids=np.unique(faces);ix=np.full(len(lp),-1);ix[ids]=np.arange(len(ids));idx=ix[faces].astype('<u4');attrs=np.concatenate([ln[ids],uv[ids],col[ids]],1).astype('<f4');locks=np.zeros(len(ids),np.uint8)
for ax in range(3):locks[np.argmin(lp[ids,ax])]=1;locks[np.argmax(lp[ids,ax])]=1
inp=W/'crown-fund-input.bin';out=W/'crown-fund-out.bin';inp.write_bytes(struct.pack('<5IfI',0x35444f4c,len(ids),idx.size,9,272*3,.003,4|32|64)+lp[ids].astype('<f4').tobytes()+attrs.tobytes()+np.array([.01]*3+[.15]*2+[.03]*4,'<f4').tobytes()+locks.tobytes()+idx.tobytes());cp=subprocess.run([str(R/'Tools/bin/meshopt-update'),str(inp),str(out)],capture_output=True,text=True,check=True);print(cp.stdout);blob=out.read_bytes();_,count,err=struct.unpack_from('<IIf',blob);assert err<=.00300001;new=np.frombuffer(blob,'<u4',count=count,offset=12).reshape(-1,3);newp=np.frombuffer(blob,'<f4',count=len(ids)*3,offset=12+count*4).reshape(-1,3);newa=np.frombuffer(blob,'<f4',offset=12+count*4+len(ids)*12).reshape(-1,9);nn=newa[:,:3].copy();nn/=np.linalg.norm(nn,axis=1)[:,None]
for key,newvalues,original,eps in [('positions',newp,lp,1e-7),('normals',nn,ln,1e-6),('uv',newa[:,3:5],uv,1e-7),('colors',newa[:,5:9],col,1e-6)]:
 values=newvalues.copy();same=np.linalg.norm(values-original[ids],axis=1)<=eps;values[same]=original[ids][same];F[key][ids]=values
facesold=F['triangles'];F['triangles']=np.concatenate([facesold[owner[facesold[:,0]]!=2428],ids[new]]);np.savez(W/'Funding.npz',**F)


# feet.py
N=v(H['normals'],'xyz');U=v(H['uv'],'xy');C=v(H['colors'],'rgba');deform=json.loads((W/'deformation-full.json').read_text())['poses'];gp=[v(x['positions'],'xyz')for x in deform];gn=[v(x['normals'],'xyz')for x in deform];result=[];report=[]
for key in shoes:
 faces=t[g[key]];ids=np.unique(faces);idx=np.full(len(p),-1);idx[ids]=np.arange(len(ids));ix=idx[faces].astype('<u4');attrs=np.concatenate([N[ids],U[ids],C[ids]],1).astype('<f4');flags=np.zeros(len(ids),np.uint8)
 # Retain source maximum/minimum silhouette supports, strap and ring detail intact.
 for axis in range(3):flags[np.argmin(p[ids,axis])]=1;flags[np.argmax(p[ids,axis])]=1
 target=len(faces) if len(faces)<=24 or key in [10093,10735] else max(20,int(len(faces)*.52));inp=W/'foot-input.bin';out=W/'foot-out.bin'
 inp.write_bytes(struct.pack('<5IfI',0x35444f4c,len(ids),ix.size,9,target*3,.003,4|32|64|128)+p[ids].astype('<f4').tobytes()+attrs.tobytes()+np.array([.01]*3+[.15]*2+[.03]*4,'<f4').tobytes()+flags.tobytes()+ix.tobytes())
 subprocess.run([str(R/'Tools/bin/meshopt-update'),str(inp),str(out)],capture_output=True,text=True,check=True);blob=out.read_bytes();magic,count,err=struct.unpack_from('<IIf',blob);reduced=np.frombuffer(blob,'<u4',count=count,offset=12).reshape(-1,3);np.savez(W/('Foot-'+str(key)+'.npz'),ids=ids,triangles=ids[reduced],positions=np.frombuffer(blob,'<f4',count=len(ids)*3,offset=12+count*4).reshape(-1,3),attrs=np.frombuffer(blob,'<f4',offset=12+count*4+len(ids)*12).reshape(-1,9));result.append(ids[reduced]);report.append(dict(component=key,before=len(faces),after=len(reduced),error=float(err)))
print(json.dumps(report,indent=2));print('feet total',sum(x['after']for x in report));np.save(W/'Feet.npy',np.concatenate(result));(W/'Feet.json').write_text(json.dumps(report,indent=2))


# assemble.py
F=np.load(W/'Funding.npz');fundt=F['triangles'];keys=lambda x:tuple(x[np.argmin(x):].tolist()+x[:np.argmin(x)].tolist());oldlookup={keys(q):i for i,q in enumerate(lt)};lowvis=json.load(open(W/'visibility194.json'))['visible'];capold=set(json.load(open(R/'work/scalp-cover-193/Scalp-Coverage.json'))['capIndices']);m194=json.load(open(R/'work/hair-cards-194/Retained-Body-Indices.json'));cap=set(i for i,j in enumerate(m194)if j in capold);delete=[]
for f,q in enumerate(fundt):
 old=oldlookup.get(keys(q));key=owner[q[0]]
 if old is not None and not lowvis[old] and key not in [31,8054,8101,8493,11775] and not any(i in cap for i in q) and np.array_equal(F['positions'][q],lp[q]) and np.array_equal(F['uv'][q],uv[q]):delete.append(f)
fundt=np.delete(fundt,delete,axis=0);fundt=np.concatenate([fundt,lt[fo==12968]]);used=np.unique(fundt);mapping={int(x):i for i,x in enumerate(used)};out={k:[L[k][i]for i in used]for k in ['vertices','normals','uv','colors','skin4']};out['triangles']=[mapping[int(x)]for x in fundt.ravel()];out['shapes']=[dict(name=s['name'],deltas=[s['deltas'][i]for i in used],normal_deltas=[s['normal_deltas'][i]for i in used])for s in L['shapes']]
fields=[('vertices','positions','xyz'),('normals','normals','xyz'),('uv','uv','xy'),('colors','colors','rgba')]
for name,f,k in fields:out[name]=[dict(zip(k,map(float,q)))for q in F[f][used]]
def append_record(data,triangles):
 start=len(out['vertices']);n=len(data['vertices']);
 for key in ['vertices','normals','uv','colors','skin4']:out[key]+=data[key]
 out['triangles']+=[int(i)+start for i in triangles]
 for i,s in enumerate(out['shapes']):
  for key in ['deltas','normal_deltas']:s[key]+=data['shapes'][i][key]
sourcevis=np.array(json.load(open(W/'visibilityFull.json'))['visible']);footreport=[]
for key in shoes+bands:
 faces=t[g[key]].copy();ids=np.unique(faces);positions=p.copy();normals=v(H['normals'],'xyz');uvs=v(H['uv'],'xy');colors=v(H['colors'],'rgba')
 if key in [10014,10656]:faces=faces[sourcevis[g[key]]]
 # Restore full fine-detail parts; only smooth rigid uppers/soles use 3mm update.
 if key in [9912,10004,10551,10646]:
  ff=np.load(W/('Foot-'+str(key)+'.npz'));ids=ff['ids'];faces=ff['triangles'];positions[ids]=ff['positions'];normals[ids]=ff['attrs'][:,:3];normals[ids]/=np.linalg.norm(normals[ids],axis=1)[:,None];uvs[ids]=ff['attrs'][:,3:5];colors[ids]=ff['attrs'][:,5:9]
 if key in bands:positions[ids]+=normals[ids]*.0025
 ids=np.unique(faces);mm={int(x):i for i,x in enumerate(ids)};data={name:[dict(zip(k,map(float,q)))for q in a[ids]]for name,a,k in [('vertices',positions,'xyz'),('normals',normals,'xyz'),('uv',uvs,'xy'),('colors',colors,'rgba')]};data['skin4']=[]
 for i in ids:data['skin4'].append(dict(indices=[H['bone_indices'][i],H['secondary_bone_indices'][i],0,0],weights=[H['primary_bone_weights'][i],1-H['primary_bone_weights'][i],0,0]))
 data['shapes']=[dict(deltas=[s['deltas'][i]for i in ids],normal_deltas=[s['normal_deltas'][i]for i in ids])for s in H['shapes']];append_record(data,[mm[int(i)]for i in faces.ravel()]);footreport.append(dict(component=key,triangles=len(faces)))
for part in json.load(open(W/'Templates.json')):
 count=len(part['vertices']);data={k:[dict(zip('xyz',map(float,q)))for q in part[k]]for k in ['vertices','normals']};data['uv']=[dict(x=.995,y=.56)for _ in range(count)];data['colors']=[dict(zip('rgba',part['color']))for _ in range(count)];data['skin4']=[]
 for w in part['weights']:
  ix=sorted(range(58),key=lambda i:-w[i])[:4];data['skin4'].append(dict(indices=ix,weights=[w[i]for i in ix]))
 data['shapes']=[dict(deltas=[dict(x=0,y=0,z=0)for _ in range(count)],normal_deltas=[dict(x=0,y=0,z=0)for _ in range(count)])for _ in range(6)];append_record(data,part['triangles'])
actual=len(out['triangles'])//3+564+48;print('BUDGET',actual,'body',len(out['triangles'])//3,'verts',len(out['vertices']),'pruned',len(delete),'feet',sum(x['triangles']for x in footreport if x['component']in shoes));assert actual<=5000
out['bodyBeforePositionIndexSHA256']=hashlib.sha256(lp.astype('<f4').tobytes()+lt.astype('<i4').tobytes()).hexdigest();(R/'Assets/DesktopFish/Editor/FishHandsShoes5K.json').write_text(json.dumps(out,separators=(',',':')))
(W/'Candidate.json').write_text(json.dumps(dict(triangles=actual,body_vertices=len(out['vertices']),funded_faces=len(fundt),funding_original=3465,hidden_194_faces=[int(oldlookup[keys(F['triangles'][f])])for f in delete],shoes_and_gold=footreport,current_original_indices=used.tolist()),indent=2))
