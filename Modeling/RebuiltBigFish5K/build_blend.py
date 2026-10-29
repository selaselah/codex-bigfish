"""Build the editable Blender source from our authored mesh/rig export.

Run: Blender --background --factory-startup --python-exit-code 1 --python Modeling/RebuiltBigFish5K/build_blend.py
Reads the generated geometry, idle-pose JSON and authored HairPaint.png atlas.
"""
import bpy
import json
import math
import struct
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

ROOT = Path(__file__).resolve().parents[2]
data = json.loads((ROOT / 'work/rebuilt-geometry-5k.json').read_text())
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def xyz(value):
    # Unity's camera/mesh convention is left-handed. Reflect X as well
    # as changing the up axis so a front view retains left/right placement.
    return Vector((-value['x'], -value['z'], value['y']))


def gltf_xyz(value):
    return Vector((-value['x'], value['y'], value['z']))


rig_data = bpy.data.armatures.new('Authored BigFish rig')
rig = bpy.data.objects.new('BigFish rig', rig_data)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for index, item in enumerate(data['bones']):
    bone = rig_data.edit_bones.new(item['name'])
    bone.head = xyz(item['position'])
    children = [b for b in data['bones'] if b['parent'] == index]
    direction = xyz(children[0]['position']) - bone.head if children else Vector((0, 0, .018))
    if direction.length < .005:
        direction = Vector((0, 0, .018))
    bone.tail = bone.head + direction
# Appended helper roles can have a higher index than an existing child.
# Create every bone before resolving hierarchy; preserve the authored indices.
for item in data['bones']:
    if item['parent'] >= 0:
        rig_data.edit_bones[item['name']].parent = rig_data.edit_bones[data['bones'][item['parent']]['name']]
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front = True
rig.display_type = 'WIRE'
degenerate_normal_positions = {}

for index, item in enumerate(data['meshes']):
    mesh = bpy.data.meshes.new(item['name'])
    triangles = item['triangles']
    # A reflection reverses orientation; reverse each face to retain outward normals.
    mesh.from_pydata([xyz(v) for v in item['vertices']], [],
                    [(triangles[i], triangles[i+2], triangles[i+1]) for i in range(0, len(triangles), 3)])
    mesh.update()
    uv_layer = mesh.uv_layers.new(name='SurfaceUV')
    for loop in mesh.loops:
        value = item['uv'][loop.vertex_index]
        uv_layer.data[loop.index].uv = (value['x'], value['y'])
    source_id=mesh.attributes.new('_SOURCE_ID','FLOAT','POINT')
    for i,value in enumerate(source_id.data):value.value=i
    obj = bpy.data.objects.new(item['name'], mesh)
    bpy.context.collection.objects.link(obj)
    for poly in mesh.polygons:
        poly.use_smooth = True
    assert len(item['normals']) == len(item['vertices'])
    authored_normals = [xyz(n) for n in item['normals']]
    assert all(math.isfinite(c) for n in authored_normals for c in n)
    # Independent loop normal spaces avoid singular fans on aggressively reduced thin parts.
    # The authored normals themselves remain identical across shared edges.
    for edge in mesh.edges:edge.use_edge_sharp=True
    mesh.update()
    mesh.normals_split_custom_set_from_vertices(authored_normals)
    mesh.update()
    collapsed_vertices = {v for poly in mesh.polygons if poly.area < 1e-12 for v in poly.vertices}
    degenerate_normal_positions[item['name']] = {
        tuple(round(c, 6) for c in gltf_xyz(item['vertices'][v])) for v in collapsed_vertices}
    for loop, normal in zip(mesh.loops, mesh.corner_normals):
        expected = authored_normals[loop.vertex_index]
        if expected.length_squared > .5 and loop.vertex_index not in collapsed_vertices:
            assert normal.vector.dot(expected) > .9999, (item['name'], loop.vertex_index, tuple(normal.vector), tuple(expected))
    colors = mesh.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='POINT')
    for vertex, color in zip(colors.data, item['colors']):
        vertex.color_srgb = (color['r'], color['g'], color['b'], color['a'])
    mat = bpy.data.materials.new('Illustration colors' if index else 'Toon body colors')
    mat.use_nodes = True
    mat.use_backface_culling = False
    nodes = mat.node_tree.nodes
    shader = nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .75
    color_node = nodes.new('ShaderNodeVertexColor')
    color_node.layer_name = 'Color'
    paint = nodes.new('ShaderNodeTexImage')
    paint.image = bpy.data.images.load(str(ROOT / 'Assets/DesktopFish/Rebuilt/HairPaint.png'), check_existing=True)
    paint.image.pack()
    multiply = nodes.new('ShaderNodeMixRGB')
    multiply.blend_type = 'MULTIPLY'
    multiply.inputs[0].default_value = 1
    mat.node_tree.links.new(color_node.outputs['Color'], multiply.inputs[1])
    mat.node_tree.links.new(paint.outputs['Color'], multiply.inputs[2])
    mat.node_tree.links.new(multiply.outputs[0], shader.inputs['Base Color'])
    if not index:
        # A binary alpha node is recognized by glTF as MASK, and matches
        # Unity's clip in both the color and outline passes.
        cutoff = nodes.new('ShaderNodeMath')
        cutoff.operation = 'GREATER_THAN'
        cutoff.inputs[1].default_value = .5
        mat.node_tree.links.new(paint.outputs['Alpha'], cutoff.inputs[0])
        mat.node_tree.links.new(cutoff.outputs[0], shader.inputs['Alpha'])
        mat.surface_render_method = 'DITHERED'
    if index:
        # A color-to-surface connection is Blender's unlit material convention.
        # glTF exports it as KHR_materials_unlit, preserving vertex-color eyes
        # without a separate white emission texture washing them out.
        mat.node_tree.links.new(multiply.outputs[0], nodes.get('Material Output').inputs['Surface'])
    mesh.materials.append(mat)
    obj.parent = rig
    groups = [obj.vertex_groups.new(name=b['name']) for b in data['bones']]
    for v, value in enumerate(item['skin4']):
        assert len(value['indices'])==4 and len(value['weights'])==4
        assert abs(sum(value['weights'])-1)<.000001
        for bone,weight in zip(value['indices'],value['weights']):
            assert 0<=bone<len(groups) and 0<=weight<=1
            if weight>0:groups[bone].add([v],weight,'ADD')
    modifier = obj.modifiers.new('Armature', 'ARMATURE')
    modifier.object = rig
    if item['shapes']:
        obj.shape_key_add(name='Basis')
        for shape in item['shapes']:
            key = obj.shape_key_add(name=shape['name'])
            key.value = 0.0
            # Shared expression controls drive the skin and feature meshes together.
            if shape['name'] not in rig:
                rig[shape['name']] = 0.0
                rig.id_properties_ui(shape['name']).update(min=0.0, max=1.0)
            driver = key.driver_add('value').driver
            variable = driver.variables.new()
            variable.name = 'amount'
            variable.targets[0].id = rig
            variable.targets[0].data_path = '["' + shape['name'] + '"]'
            driver.expression = 'amount'
            if shape.get('normal_deltas'):
                attribute = mesh.attributes.new('NormalDelta:' + shape['name'], 'FLOAT_VECTOR', 'POINT')
                for value, delta in zip(attribute.data, shape['normal_deltas']):
                    value.vector = xyz(delta)
            for vertex, base, delta in zip(key.data, item['vertices'], shape['deltas']):
                vertex.co = xyz(base) + xyz(delta)

pose_path = ROOT / 'work/rebuilt-idle-pose.json'
if pose_path.exists():
    conversion = Matrix(((-1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
    posed = {bone['name']: bone for bone in json.loads(pose_path.read_text())['bones']}
    for bone in data['bones']:
        value = posed[bone['name']]
        q = value['rotation']
        rotation = Quaternion((q['w'], q['x'], q['y'], q['z'])).to_matrix().to_4x4()
        p = value['position']
        matrix = Matrix.Translation((p['x'], p['y'], p['z'])) @ rotation
        rest = bone['position']
        rest_matrix = Matrix.Translation((rest['x'], rest['y'], rest['z']))
        delta = conversion @ matrix @ rest_matrix.inverted() @ conversion.inverted()
        rig.pose.bones[bone['name']].matrix = delta @ rig.data.bones[bone['name']].matrix_local
        bpy.context.view_layer.update()

# Check the opened idle pose against source-space linear skinning. This catches
# inconsistent handedness between the mesh, rig and pose conversion.
if pose_path.exists():
    idle_error = 0.0
    for item in data['meshes']:
        evaluated = bpy.data.objects[item['name']].evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        for i, vertex in enumerate(item['vertices']):
            point = Vector(tuple(vertex[c] for c in 'xyz'))
            expected = Vector((0, 0, 0))
            for bone_index,weight in zip(item['skin4'][i]['indices'],item['skin4'][i]['weights']):
                bone = data['bones'][bone_index]
                pose = posed[bone['name']]
                q = pose['rotation']
                rotation = Quaternion((q['w'], q['x'], q['y'], q['z']))
                rest = Vector(tuple(bone['position'][c] for c in 'xyz'))
                position = Vector(tuple(pose['position'][c] for c in 'xyz'))
                expected += (rotation @ (point - rest) + position) * weight
            expected = Vector((-expected.x, -expected.z, expected.y))
            idle_error = max(idle_error, (evaluated.matrix_world @ mesh.vertices[i].co - expected).length)
        evaluated.to_mesh_clear()
    assert idle_error < .00002, f"Exported idle pose differs from Unity skinning: {idle_error}"
    print(f"Idle-pose handedness/skinning validated: maximum vertex error {idle_error:.9g}")

bpy.ops.object.camera_add(location=(0, -3, .85))
camera = bpy.context.object
camera.name = 'Preview camera'
camera.rotation_euler = (Vector((0, 0, .54)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.28
bpy.context.scene.camera = camera
for location, power, size in [((-2, -3, 4), 450, 4), ((2, -1, 2), 150, 3)]:
    bpy.ops.object.light_add(type='AREA', location=location)
    lamp = bpy.context.object
    lamp.data.energy = power
    lamp.data.shape = 'DISK'
    lamp.data.size = size
    lamp.rotation_euler = (Vector((0, 0, .5)) - lamp.location).to_track_quat('-Z', 'Y').to_euler()
scene = bpy.context.scene
try:
    scene.render.engine = 'BLENDER_EEVEE'
except TypeError:
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
scene.render.resolution_x, scene.render.resolution_y = 720, 960
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.view_settings.view_transform = 'Standard'
scene.world.color = (.6, .6, .6)
folder = ROOT / 'Modeling/RebuiltBigFish5K'
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'BigFish.blend'))
for obj in bpy.context.selected_objects:
    obj.select_set(False)
rig.select_set(True)
for child in rig.children:
    child.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.gltf(filepath=str(folder / 'BigFish.glb'), export_format='GLB',
                          use_selection=True, export_animations=False, export_skins=True,
                          export_morph=True, export_attributes=True, export_vertex_color='NAME',
                          export_vertex_color_name='Color', export_all_vertex_colors=False)

# Check the exported interchange data, not just Blender's successful-save message.
# A previous exporter fallback substituted a white COLOR_0 for the authored colors.
blob = (folder / 'BigFish.glb').read_bytes()
json_length = struct.unpack_from('<I', blob, 12)[0]
document = json.loads(blob[20:20 + json_length])
binary = blob[28 + json_length:]


def float_vectors(accessor_id):
    accessor = document['accessors'][accessor_id]
    assert accessor['componentType'] == 5126
    dimensions = {'SCALAR':1,'VEC2': 2, 'VEC3': 3}[accessor['type']]
    values = [(0.0,) * dimensions for _ in range(accessor['count'])]
    if 'bufferView' in accessor:
        view = document['bufferViews'][accessor['bufferView']]
        offset = view.get('byteOffset', 0) + accessor.get('byteOffset', 0)
        stride = view.get('byteStride', dimensions * 4)
        values = [struct.unpack_from('<' + 'f' * dimensions, binary, offset + i * stride)
                  for i in range(accessor['count'])]
    if 'sparse' in accessor:
        sparse = accessor['sparse']
        indices = sparse['indices']
        index_view = document['bufferViews'][indices['bufferView']]
        index_offset = index_view.get('byteOffset', 0) + indices.get('byteOffset', 0)
        index_kind = {5121: 'B', 5123: 'H', 5125: 'I'}[indices['componentType']]
        index_size = struct.calcsize('<' + index_kind)
        replacements = sparse['values']
        value_view = document['bufferViews'][replacements['bufferView']]
        value_offset = value_view.get('byteOffset', 0) + replacements.get('byteOffset', 0)
        for row in range(sparse['count']):
            index = struct.unpack_from('<' + index_kind, binary, index_offset + row * index_size)[0]
            assert 0 <= index < len(values)
            values[index] = struct.unpack_from('<' + 'f' * dimensions, binary, value_offset + row * dimensions * 4)
    return values


# Blender regenerates morph normals from geometry, which discards the authored
# transport of custom skin normals. Preserve the explicit deformation normals
# stored as POINT vectors in the saved project, using the actual exported seams.
normal_checks = []
rebuilt_binary = bytearray(binary)
for mesh in document['meshes']:
    source = next(item for item in data['meshes'] if item['name'] == mesh['name'])
    if not any(shape.get('normal_deltas') for shape in source['shapes']):
        continue
    by_position = {}
    for i, position in enumerate(source['vertices']):
        by_position.setdefault(tuple(round(c, 6) for c in gltf_xyz(position)), []).append(i)
    for primitive in mesh['primitives']:
        positions = float_vectors(primitive['attributes']['POSITION'])
        uvs = float_vectors(primitive['attributes']['TEXCOORD_0'])
        normals = float_vectors(primitive['attributes']['NORMAL'])
        ids=float_vectors(primitive['attributes']['_SOURCE_ID']);mapping=[int(value[0]) for value in ids]
        assert all(0<=i<len(source['vertices']) for i in mapping)
        assert all((Vector(point)-gltf_xyz(source['vertices'][i])).length<2e-6 and abs(uv[0]-source['uv'][i]['x'])<2e-6 and abs(uv[1]-(1-source['uv'][i]['y']))<2e-6 for i,point,uv in zip(mapping,positions,uvs)), 'LOD source ID correspondence changed'
        joint_names=[document['nodes'][i]['name'] for i in document['skins'][0]['joints']]
        for semantic,component,kind,values in [
            ('JOINTS_0',5123,'H',[tuple(joint_names.index(data['bones'][bi]['name']) for bi in source['skin4'][i]['indices']) for i in mapping]),
            ('WEIGHTS_0',5126,'f',[tuple(source['skin4'][i]['weights']) for i in mapping])]:
            while len(rebuilt_binary)%4:rebuilt_binary.append(0)
            offset=len(rebuilt_binary)
            for value in values:rebuilt_binary.extend(struct.pack('<'+kind*4,*value))
            view=len(document['bufferViews']);document['bufferViews'].append(dict(buffer=0,byteOffset=offset,byteLength=len(values)*struct.calcsize('<'+kind*4)))
            accessor=len(document['accessors']);document['accessors'].append(dict(bufferView=view,componentType=component,count=len(values),type='VEC4'));primitive['attributes'][semantic]=accessor
        # Provenance is used only for exact reconstruction, not shipped as GPU data.
        for key in list(primitive['attributes']):
            if key.startswith('_'):del primitive['attributes'][key]
        # Restore base normals as the exact Unity float32 values before welding.
        expected = [tuple(gltf_xyz(source['normals'][i])) for i in mapping]
        while len(rebuilt_binary)%4:rebuilt_binary.append(0)
        offset=len(rebuilt_binary)
        for value in expected:rebuilt_binary.extend(struct.pack('<3f',*value))
        view=len(document['bufferViews']);document['bufferViews'].append(dict(buffer=0,byteOffset=offset,byteLength=len(expected)*12))
        accessor=len(document['accessors']);document['accessors'].append(dict(bufferView=view,componentType=5126,count=len(expected),type='VEC3'))
        primitive['attributes']['NORMAL']=accessor;normal_checks.append((accessor,expected))
        for target, shape in zip(primitive['targets'], source['shapes']):
            expected = [tuple(gltf_xyz(shape['normal_deltas'][i])) for i in mapping]
            while len(rebuilt_binary) % 4:
                rebuilt_binary.append(0)
            offset = len(rebuilt_binary)
            for normal in expected:
                rebuilt_binary.extend(struct.pack('<3f', *normal))
            view = len(document['bufferViews'])
            document['bufferViews'].append(dict(buffer=0, byteOffset=offset, byteLength=len(expected)*12))
            accessor = len(document['accessors'])
            document['accessors'].append(dict(bufferView=view, componentType=5126, count=len(expected), type='VEC3'))
            target['NORMAL'] = accessor
            normal_checks.append((accessor, expected))
binary = bytes(rebuilt_binary)
if normal_checks:
    document['buffers'][0]['byteLength'] = len(binary)
    encoded_json = json.dumps(document, separators=(',', ':')).encode()
    encoded_json += b' ' * ((-len(encoded_json)) % 4)
    payload = binary + b'\0' * ((-len(binary)) % 4)
    encoded_glb = struct.pack('<III', 0x46546C67, 2, 28+len(encoded_json)+len(payload)) + struct.pack('<II', len(encoded_json), 0x4E4F534A) + encoded_json + struct.pack('<II', len(payload), 0x004E4942) + payload
    (folder / 'BigFish.glb').write_bytes(encoded_glb)
    for accessor, expected in normal_checks:
        assert float_vectors(accessor) == [struct.unpack('<3f', struct.pack('<3f', *value)) for value in expected], 'Authored GLB morph normal changed'
    print(f'Explicit skin morph normals validated: {sum(len(values) for _, values in normal_checks)} float32 vectors')

# Exact custom normals can be encoded slightly differently by separate Blender
# normal fans. Weld only fully identical complete attribute/morph records after
# restoring authored normals; this changes no rendered value or triangle.
def raw_rows(ai):
    ac=document['accessors'][ai];dims={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[ac['type']];kind={5120:'b',5121:'B',5122:'h',5123:'H',5125:'I',5126:'f'}[ac['componentType']];size=struct.calcsize('<'+kind*dims);rows=[bytes(size)]*ac['count']
    if 'bufferView' in ac:
        v=document['bufferViews'][ac['bufferView']];start=v.get('byteOffset',0)+ac.get('byteOffset',0);stride=v.get('byteStride',size);rows=[binary[start+i*stride:start+i*stride+size] for i in range(ac['count'])]
    if 'sparse' in ac:
        sp=ac['sparse'];ind=sp['indices'];iv=document['bufferViews'][ind['bufferView']];io=iv.get('byteOffset',0)+ind.get('byteOffset',0);ik={5121:'B',5123:'H',5125:'I'}[ind['componentType']];isz=struct.calcsize('<'+ik);vv=document['bufferViews'][sp['values']['bufferView']];vo=vv.get('byteOffset',0)+sp['values'].get('byteOffset',0)
        for j in range(sp['count']):rows[struct.unpack_from('<'+ik,binary,io+j*isz)[0]]=binary[vo+j*size:vo+(j+1)*size]
    assert all(len(row)==size for row in rows);return rows
extra=bytearray(binary)
def append_rows(original,rows):
    while len(extra)%4:extra.append(0)
    offset=len(extra);payload=b''.join(rows);extra.extend(payload);view=len(document['bufferViews']);document['bufferViews'].append(dict(buffer=0,byteOffset=offset,byteLength=len(payload)));ac={k:v for k,v in original.items() if k not in ['bufferView','byteOffset','sparse']};ac.update(bufferView=view,count=len(rows));ai=len(document['accessors']);document['accessors'].append(ac);return ai
for mesh in document['meshes']:
    for pr in mesh['primitives']:
        streams=[(pr['attributes'],k) for k in sorted(pr['attributes'])]+[(tar,k) for tar in pr.get('targets',[]) for k in sorted(tar)]
        rows=[raw_rows(container[key]) for container,key in streams];lookup={};unique=[];remap=[]
        for v in range(len(rows[0])):
            key=b''.join(row[v] for row in rows)
            if key not in lookup:lookup[key]=len(unique);unique.append(v)
            remap.append(lookup[key])
        oldindices=raw_rows(pr['indices']);kind={5121:'B',5123:'H',5125:'I'}[document['accessors'][pr['indices']]['componentType']];indices=[remap[struct.unpack('<'+kind,row)[0]] for row in oldindices]
        for (container,key),row in zip(streams,rows):container[key]=append_rows(document['accessors'][container[key]],[row[v] for v in unique])
        pr['indices']=append_rows(dict(componentType=5123,type='SCALAR'),[struct.pack('<H',v) for v in indices])
        assert len(indices)//3==document['accessors'][pr['indices']]['count']//3
binary=bytes(extra)
# Retain just referenced accessor/image data in the final interchange file.
oldaccess=document['accessors'];newaccess=[];newviews=[];packed=bytearray();accessmap={}
def add_payload(payload):
    while len(packed)%4:packed.append(0)
    view=len(newviews);newviews.append(dict(buffer=0,byteOffset=len(packed),byteLength=len(payload)));packed.extend(payload);return view
refs=[]
for mesh in document['meshes']:
    for pr in mesh['primitives']:
        refs.extend([(pr,'indices')]);refs.extend((pr['attributes'],k) for k in pr['attributes'])
        for tar in pr.get('targets',[]):refs.extend((tar,k) for k in tar)
for skin in document['skins']:refs.append((skin,'inverseBindMatrices'))
for container,key in refs:
    old=container[key]
    if old not in accessmap:
        ac={k:v for k,v in oldaccess[old].items() if k not in ['bufferView','byteOffset','sparse']};ac['bufferView']=add_payload(b''.join(raw_rows(old)));accessmap[old]=len(newaccess);newaccess.append(ac)
    container[key]=accessmap[old]
for im in document['images']:
    v=document['bufferViews'][im['bufferView']];start=v.get('byteOffset',0);im['bufferView']=add_payload(binary[start:start+v['byteLength']])
document['accessors']=newaccess;document['bufferViews']=newviews;binary=bytes(packed);document['buffers'][0]['byteLength']=len(binary)
encoded_json=json.dumps(document,separators=(',',':')).encode();encoded_json+=b' '*((-len(encoded_json))%4);payload=binary+b'\0'*((-len(binary))%4)
(folder/'BigFish.glb').write_bytes(struct.pack('<III',0x46546C67,2,28+len(encoded_json)+len(payload))+struct.pack('<II',len(encoded_json),0x4E4F534A)+encoded_json+struct.pack('<II',len(payload),0x004E4942)+payload)
print('Exact complete-record weld:',sum(document['accessors'][pr['attributes']['POSITION']]['count'] for mesh in document['meshes'] for pr in mesh['primitives']),'encoded vertices')

assert len(document['meshes']) == len(data['meshes'])
assert len(document['images']) == 1
assert document['materials'][0]['alphaMode'] == 'MASK'
assert document['materials'][0].get('doubleSided', False), 'Thin hair sheets require a two-sided material'
assert abs(document['materials'][0].get('alphaCutoff', .5) - .5) < 1e-6
assert sum(len(item['triangles']) // 3 for item in data['meshes']) <= 5000
exported_triangles = sum(document['accessors'][primitive['indices']]['count'] // 3
                         for mesh in document['meshes'] for primitive in mesh['primitives'])
assert exported_triangles == sum(len(item['triangles']) // 3 for item in data['meshes'])
assert len(document['skins'][0]['joints']) == len(data['bones'])
blended_vertices = 0
checked_normals = 0
exported_normals = 0
checked_morph_deltas = 0
checked_vertex_colors = 0
for mesh in document['meshes']:
    source = next(item for item in data['meshes'] if item['name'] == mesh['name'])
    normals_by_position = {}
    indices_by_position = {}
    for index, (position, normal) in enumerate(zip(source['vertices'], source['normals'])):
        key = tuple(round(c, 6) for c in gltf_xyz(position))
        normals_by_position.setdefault(key, []).append(gltf_xyz(normal))
        indices_by_position.setdefault(key, []).append(index)
    assert mesh.get('extras', {}).get('targetNames', []) == [shape['name'] for shape in source['shapes']]
    for primitive in mesh['primitives']:
        attributes = primitive['attributes']
        assert {'COLOR_0', 'TEXCOORD_0', 'JOINTS_0', 'WEIGHTS_0', 'NORMAL'} <= attributes.keys()
        positions = float_vectors(attributes['POSITION'])
        for position, uv in zip(positions, float_vectors(attributes['TEXCOORD_0'])):
            candidates = indices_by_position[tuple(round(c, 6) for c in position)]
            assert any(abs(uv[0] - source['uv'][i]['x']) < 2e-6 and
                       abs(uv[1] - (1 - source['uv'][i]['y'])) < 2e-6
                       for i in candidates), 'Authored GLB texture coordinates changed'
        for position, normal in zip(positions, float_vectors(attributes['NORMAL'])):
            exported_normals += 1
            assert all(math.isfinite(c) for c in normal)
            key = tuple(round(c, 6) for c in position)
            candidates = normals_by_position[key]
            # Collapsed poles can have zero or ambiguous fan normals; Blender
            # calculates fallback corners there. Check every other source normal.
            if key not in degenerate_normal_positions[mesh['name']] and not any(n.length_squared < .5 for n in candidates):
                assert abs(Vector(normal).length - 1) < .001
                assert max(Vector(normal).dot(n) for n in candidates) > .9999, 'Authored GLB normal changed'
                checked_normals += 1
        targets = primitive.get('targets', [])
        assert len(targets) == len(source['shapes'])
        if targets:
            deltas = [float_vectors(target['POSITION']) for target in targets]
            assert all(len(delta) == len(positions) for delta in deltas)
            for vertex, position in enumerate(positions):
                candidates = indices_by_position[tuple(round(c, 6) for c in position)]
                assert any(all(
                    (Vector(deltas[shape][vertex]) - gltf_xyz(source['shapes'][shape]['deltas'][index])).length < .000002
                    for shape in range(len(targets))) for index in candidates), 'Authored GLB expression changed'
            checked_morph_deltas += len(positions) * len(targets)
        assert 'COLOR_1' not in attributes
        accessor = document['accessors'][attributes['COLOR_0']]
        view = document['bufferViews'][accessor['bufferView']]
        offset = view.get('byteOffset', 0) + accessor.get('byteOffset', 0)
        kind = {5121: 'B', 5123: 'H', 5126: 'f'}[accessor['componentType']]
        # White is valid for skin whose color now comes from paint. Verify
        # every exported RGBA value against the source, including UV seams.
        color_size = struct.calcsize('<' + kind * 4)
        color_stride = view.get('byteStride', color_size)
        divisor = {5121: 255, 5123: 65535, 5126: 1}[accessor['componentType']]
        if divisor != 1:
            assert accessor.get('normalized', False), 'Integer GLB colors must be normalized'
        tolerance = 1.5 / divisor if divisor != 1 else .00003
        uvs = float_vectors(attributes['TEXCOORD_0'])
        def linear_color(color):
            rgb = [color[k] for k in ('r', 'g', 'b')]
            return tuple(c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4 for c in rgb) + (color['a'],)
        for vertex, (position, uv) in enumerate(zip(positions, uvs)):
            rgba = tuple(c / divisor for c in struct.unpack_from('<' + kind * 4, binary,
                         offset + vertex * color_stride))
            candidates = indices_by_position[tuple(round(c, 6) for c in position)]
            assert any(abs(uv[0] - source['uv'][i]['x']) < 2e-6 and
                       abs(uv[1] - (1 - source['uv'][i]['y'])) < 2e-6 and
                       max(abs(a - b) for a, b in zip(rgba, linear_color(source['colors'][i]))) < tolerance
                       for i in candidates), 'Authored GLB vertex color changed'
            checked_vertex_colors += 1
        weights = document['accessors'][attributes['WEIGHTS_0']]
        weight_view = document['bufferViews'][weights['bufferView']]
        weight_offset = weight_view.get('byteOffset', 0) + weights.get('byteOffset', 0)
        weight_kind = {5121: 'B', 5123: 'H', 5126: 'f'}[weights['componentType']]
        size = struct.calcsize('<' + weight_kind * 4)
        stride = weight_view.get('byteStride', size)
        divisor = {5121: 255, 5123: 65535, 5126: 1}[weights['componentType']]
        for vertex in range(weights['count']):
            values = [w / divisor for w in struct.unpack_from(
                '<' + weight_kind * 4, binary, weight_offset + vertex * stride)]
            assert all(0 <= w <= 1 for w in values) and abs(sum(values) - 1) < .0001
            blended_vertices += sum(w > .0001 for w in values) > 1
    assert not any(mesh.get('weights', [])), "Facial expressions must start neutral"
assert 'KHR_materials_unlit' in document['materials'][1].get('extensions', {})
assert blended_vertices > 0, "Smooth hair-root weights must survive the GLB export"
assert checked_normals > .99 * exported_normals
assert checked_morph_deltas > 0, "Facial morph targets must survive the GLB export"
print(f'Saved and validated Blender/GLB: {exported_triangles} triangles, MASK alpha, packed paint, UVs, vertex colors, rig, '
      f'{blended_vertices} blended vertices, {checked_normals}/{exported_normals} shading normals, '
      f'{checked_morph_deltas} expression deltas, {checked_vertex_colors} verified RGBA vertex colors and neutral expressions.')
