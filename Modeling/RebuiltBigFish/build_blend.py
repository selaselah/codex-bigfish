"""Build the editable Blender source from our authored mesh/rig export.

Run: Blender --background --factory-startup --python Modeling/RebuiltBigFish/build_blend.py
Reads the generated geometry, idle-pose JSON and authored HairPaint.png atlas.
"""
import bpy
import json
import struct
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

ROOT = Path(__file__).resolve().parents[2]
data = json.loads((ROOT / 'work/rebuilt-geometry.json').read_text())
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def xyz(value):
    return Vector((value['x'], -value['z'], value['y']))


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
    if item['parent'] >= 0:
        bone.parent = rig_data.edit_bones[data['bones'][item['parent']]['name']]
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front = True
rig.display_type = 'WIRE'

for index, item in enumerate(data['meshes']):
    mesh = bpy.data.meshes.new(item['name'])
    triangles = item['triangles']
    mesh.from_pydata([xyz(v) for v in item['vertices']], [],
                    [tuple(triangles[i:i+3]) for i in range(0, len(triangles), 3)])
    mesh.update()
    uv_layer = mesh.uv_layers.new(name='SurfaceUV')
    for loop in mesh.loops:
        value = item['uv'][loop.vertex_index]
        uv_layer.data[loop.index].uv = (value['x'], value['y'])
    obj = bpy.data.objects.new(item['name'], mesh)
    bpy.context.collection.objects.link(obj)
    for poly in mesh.polygons:
        poly.use_smooth = True
    colors = mesh.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='POINT')
    for vertex, color in zip(colors.data, item['colors']):
        vertex.color_srgb = (color['r'], color['g'], color['b'], color['a'])
    mat = bpy.data.materials.new('Illustration colors' if index else 'Toon body colors')
    mat.use_nodes = True
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
    if index:
        # A color-to-surface connection is Blender's unlit material convention.
        # glTF exports it as KHR_materials_unlit, preserving vertex-color eyes
        # without a separate white emission texture washing them out.
        mat.node_tree.links.new(multiply.outputs[0], nodes.get('Material Output').inputs['Surface'])
    mesh.materials.append(mat)
    obj.parent = rig
    groups = [obj.vertex_groups.new(name=b['name']) for b in data['bones']]
    for v, bone in enumerate(item['bone_indices']):
        groups[bone].add([v], 1.0, 'REPLACE')
    modifier = obj.modifiers.new('Armature', 'ARMATURE')
    modifier.object = rig
    if item['shapes']:
        obj.shape_key_add(name='Basis')
        for shape in item['shapes']:
            key = obj.shape_key_add(name=shape['name'])
            key.value = 0.0
            for vertex, base, delta in zip(key.data, item['vertices'], shape['deltas']):
                vertex.co = xyz(base) + xyz(delta)

pose_path = ROOT / 'work/rebuilt-idle-pose.json'
if pose_path.exists():
    conversion = Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
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
folder = ROOT / 'Modeling/RebuiltBigFish'
bpy.ops.wm.save_as_mainfile(filepath=str(folder / 'BigFish.blend'))
for obj in bpy.context.selected_objects:
    obj.select_set(False)
rig.select_set(True)
for child in rig.children:
    child.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.gltf(filepath=str(folder / 'BigFish.glb'), export_format='GLB',
                          use_selection=True, export_animations=False, export_skins=True,
                          export_morph=True, export_vertex_color='NAME',
                          export_vertex_color_name='Color', export_all_vertex_colors=False)

# Check the exported interchange data, not just Blender's successful-save message.
# A previous exporter fallback substituted a white COLOR_0 for the authored colors.
blob = (folder / 'BigFish.glb').read_bytes()
json_length = struct.unpack_from('<I', blob, 12)[0]
document = json.loads(blob[20:20 + json_length])
binary = blob[28 + json_length:]
assert len(document['meshes']) == len(data['meshes'])
assert len(document['images']) == 1
assert len(document['skins'][0]['joints']) == len(data['bones'])
for mesh in document['meshes']:
    for primitive in mesh['primitives']:
        attributes = primitive['attributes']
        assert {'COLOR_0', 'TEXCOORD_0', 'JOINTS_0', 'WEIGHTS_0'} <= attributes.keys()
        assert 'COLOR_1' not in attributes
        accessor = document['accessors'][attributes['COLOR_0']]
        view = document['bufferViews'][accessor['bufferView']]
        offset = view.get('byteOffset', 0) + accessor.get('byteOffset', 0)
        kind = {5121: 'B', 5123: 'H', 5126: 'f'}[accessor['componentType']]
        rgba = struct.unpack_from('<' + kind * 4, binary, offset)
        assert len(set(rgba[:3])) > 1, f"Unexpected neutral vertex color in {mesh['name']}"
    assert not any(mesh.get('weights', [])), "Facial expressions must start neutral"
assert 'KHR_materials_unlit' in document['materials'][1].get('extensions', {})
print('Saved and validated Blender/GLB: packed paint, UVs, vertex colors, rig and neutral expressions.')
