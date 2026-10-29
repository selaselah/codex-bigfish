"""Reopen the 5K project and verify every expression, skin stream and GLB normal."""
import bpy,json,struct,math
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[2];out=ROOT/'work/hair-cards-194/blender-review';out.mkdir(parents=True,exist_ok=True)
data=json.loads((ROOT/'work/rebuilt-geometry-5k.json').read_text());poses={b['name']:b for b in json.loads((ROOT/'work/rebuilt-idle-pose.json').read_text())['bones']}
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Modeling/RebuiltBigFish5K/BigFish.blend'));rig=bpy.data.objects['BigFish rig']
def v(x):return Vector(tuple(x[a] for a in 'xyz'))
def conv(x):return Vector((-x.x,-x.z,x.y))
def gconv(x):return (-x['x'],x['y'],x['z'])
def f32(q):return struct.unpack('<'+'f'*len(q),struct.pack('<'+'f'*len(q),*q))
assert len(rig.data.bones)==len(data['bones'])==58
report={'triangles':sum(len(m['triangles'])//3 for m in data['meshes']),'bones':58,'cases':[],'saved_loop_normals':0,'saved_normal_deltas':0}
assert report['triangles']<=5000
trans=[]
for b in data['bones']:
 p=poses[b['name']];q=p['rotation'];trans.append((v(b['position']),Quaternion((q['w'],q['x'],q['y'],q['z'])),v(p['position'])))
for m in data['meshes']:
 obj=bpy.data.objects[m['name']];mesh=obj.data;assert len(mesh.vertices)==len(m['vertices']);assert len(mesh.polygons)*3==len(m['triangles'])
 # Preserve the full-detail exporter's existing collapsed-pole exception,
 # and prove that every such triangle was inherited unchanged from the source.
 recipe=json.loads((ROOT/'Assets/DesktopFish/Editor/FishLowPoly5KRecipe.json').read_text());original=json.loads((ROOT/'work/rebuilt-geometry.json').read_text());mi=0 if m['name']=='Body' else 1;src=original['meshes'][mi];mapping=recipe['sourceIndices' if mi==0 else 'faceSourceIndices'];original_faces={tuple(sorted(src['triangles'][j:j+3])) for j in range(0,len(src['triangles']),3)};collapsed=set()
 for poly in mesh.polygons:
  if poly.area<1e-12:
   assert m['name']!='RearHairCards','New degenerate card triangle'
   assert tuple(sorted(mapping[i] for i in poly.vertices)) in original_faces,'New degenerate triangle'
   collapsed.update(poly.vertices)
 for loop,n in zip(mesh.loops,mesh.corner_normals):
  if loop.vertex_index not in collapsed:assert n.vector.dot(conv(v(m['normals'][loop.vertex_index])))>.9999
 report.setdefault('inherited_collapsed_pole_vertices',{})[m['name']]=len(collapsed)
 report['saved_loop_normals']+=len(mesh.loops)
 for s in m['shapes']:
  attr=mesh.attributes['NormalDelta:'+s['name']]
  for actual,expected in zip(attr.data,s['normal_deltas']):assert tuple(actual.vector)==f32(conv(v(expected)))
  report['saved_normal_deltas']+=len(attr.data)
for shape,amount in [(None,0)]+[(s['name'],a) for s in data['meshes'][0]['shapes'] for a in [.5,1.]]:
 for s in data['meshes'][0]['shapes']:rig[s['name']]=amount if s['name']==shape else 0
 rig.update_tag();bpy.context.view_layer.update();case={'shape':shape,'amount':amount,'maximum_lbs_error':0}
 for m in data['meshes']:
  obj=bpy.data.objects[m['name']];ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh()
  for i,p in enumerate(m['vertices']):
   point=v(p)+sum((v(s['deltas'][i])*amount for s in m['shapes'] if s['name']==shape),Vector());expected=Vector()
   for bi,weight in zip(m['skin4'][i]['indices'],m['skin4'][i]['weights']):
    rest,q,pos=trans[bi];expected+=(q@(point-rest)+pos)*weight
   case['maximum_lbs_error']=max(case['maximum_lbs_error'],(ev.matrix_world@mesh.vertices[i].co-conv(expected)).length)
  ev.to_mesh_clear()
 assert case['maximum_lbs_error']<2e-5,case;report['cases'].append(case)
for s in data['meshes'][0]['shapes']:rig[s['name']]=0
rig.update_tag();bpy.context.view_layer.update()
blob=(ROOT/'Modeling/RebuiltBigFish5K/BigFish.glb').read_bytes();jl=struct.unpack_from('<I',blob,12)[0];doc=json.loads(blob[20:20+jl]);binary=blob[28+jl:]
def rows(ai):
 ac=doc['accessors'][ai];n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[ac['type']];k={5121:'B',5123:'H',5125:'I',5126:'f'}[ac['componentType']];size=struct.calcsize('<'+k*n);view=doc['bufferViews'][ac['bufferView']];start=view.get('byteOffset',0)+ac.get('byteOffset',0);stride=view.get('byteStride',size);values=[struct.unpack_from('<'+k*n,binary,start+i*stride) for i in range(ac['count'])]
 if ac.get('normalized',False):values=[tuple(x/({5121:255,5123:65535}[ac['componentType']]) for x in q) for q in values]
 return values
jointnames=[doc['nodes'][i]['name'] for i in doc['skins'][0]['joints']];assert len(jointnames)==58
for skin in doc['skins']:assert skin['joints']==doc['skins'][0]['joints'] and rows(skin['inverseBindMatrices'])==rows(doc['skins'][0]['inverseBindMatrices'])
report['glb_vertices']=0;report['glb_exact_base_and_morph_normals']=0;report['glb_skin_records']=0
for mesh in doc['meshes']:
 m=next(m for m in data['meshes'] if m['name']==mesh['name']);lookup={}
 for i,p in enumerate(m['vertices']):lookup.setdefault(tuple(round(x,6) for x in gconv(p)),[]).append(i)
 for pr in mesh['primitives']:
  attrs=pr['attributes'];pos=rows(attrs['POSITION']);uv=rows(attrs['TEXCOORD_0']);normal=rows(attrs['NORMAL']);weights=rows(attrs['WEIGHTS_0']);joints=rows(attrs['JOINTS_0']);targets=[{k:rows(ai) for k,ai in tar.items()} for tar in pr['targets']];report['glb_vertices']+=len(pos)
  for j,point in enumerate(pos):
   choices=[i for i in lookup[tuple(round(x,6) for x in point)] if abs(uv[j][0]-m['uv'][i]['x'])<2e-6 and abs(uv[j][1]-(1-m['uv'][i]['y']))<2e-6 and normal[j]==f32(gconv(m['normals'][i]))];assert choices
   def match(i):
    expected={};actual={}
    for bi,w in zip(m['skin4'][i]['indices'],m['skin4'][i]['weights']):expected[data['bones'][bi]['name']]=expected.get(data['bones'][bi]['name'],0)+w
    for bi,w in zip(joints[j],weights[j]):actual[jointnames[bi]]=actual.get(jointnames[bi],0)+w
    return all(abs(expected.get(k,0)-actual.get(k,0))<3e-5 for k in set(expected)|set(actual)) and all(t['NORMAL'][j]==f32(gconv(s['normal_deltas'][i])) and (Vector(t['POSITION'][j])-Vector(gconv(s['deltas'][i]))).length<2e-6 for t,s in zip(targets,m['shapes']))
   assert any(match(i) for i in choices),('GLB skin or expression mismatch',mesh['name'],j);report['glb_skin_records']+=1;report['glb_exact_base_and_morph_normals']+=7
assert sum(doc['accessors'][pr['indices']]['count']//3 for m in doc['meshes'] for pr in m['primitives'])<=5000
(out/'Validation.json').write_text(json.dumps(report,indent=2));print('5K saved-project independent verification passed',json.dumps(report))
scene=bpy.context.scene;camera=scene.camera;center=Vector((0,0,.54));front=camera.location.copy();scene.render.resolution_x=360;scene.render.resolution_y=480;scene.render.resolution_percentage=100
for label,angle in [('0',0),('35',35),('90',90),('180',180),('top',0)]:
 rotation=Quaternion((0,0,1),math.radians(angle));camera.location=center+rotation@(front-center)
 if label=='top':camera.location.z+=.7
 camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/('Saved-'+label+'.png'));bpy.ops.render.render(write_still=True)
print('5K saved-project five views rendered')
