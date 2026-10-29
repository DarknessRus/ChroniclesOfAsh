"""
Chronicles of Ash — free procedural Warrior asset generator.
Run with Blender 3.6+ in background mode. Exports a real FBX with an articulated
armature and looping Idle / Walk / Run actions. All geometry/materials are created
locally; no paid API or external asset is required.

Usage:
  blender --background --python Tools/Blender/create_ash_warrior.py
Output:
  Unity/Assets/Resources/Characters/AshHero.fbx
"""
import bpy
import math
import os
from mathutils import Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUTPUT_DIR = os.path.join(ROOT, "Unity", "Assets", "Resources", "Characters")
OUTPUT_FBX = os.path.join(OUTPUT_DIR, "AshHero.fbx")

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.armatures, bpy.data.actions):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)

def make_material(name, color, metallic=0.0, roughness=0.5, emission=None):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        socket = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if socket:
            socket.default_value = (*emission, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 1.6
    return mat

def finish_mesh(obj, name, material, bevel=0.0):
    obj.name = name
    if obj.type == "MESH":
        obj.data.materials.append(material)
        for poly in obj.data.polygons:
            poly.use_smooth = True
        if bevel > 0:
            mod = obj.modifiers.new("Forged edge highlights", "BEVEL")
            mod.width = bevel
            mod.segments = 2
            mod = obj.modifiers.new("Weighted armor normals", "WEIGHTED_NORMAL")
    return obj

def cube(name, loc, scale, mat, bevel=0.06):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_mesh(obj, name, mat, bevel)

def sphere(name, loc, scale, mat, segments=20, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_mesh(obj, name, mat)

def cylinder(name, loc, radius, depth, mat, vertices=12, bevel=0.025):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    return finish_mesh(bpy.context.object, name, mat, bevel)

def cone(name, loc, radius1, radius2, depth, mat, vertices=12):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=loc)
    return finish_mesh(bpy.context.object, name, mat, 0.02)

def create_armature():
    arm_data = bpy.data.armatures.new("AshWarrior_Skeleton")
    arm = bpy.data.objects.new("AshWarrior_Rig", arm_data)
    bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = {
        "root": ((0,0,0.08),(0,0,0.95),None),
        "spine": ((0,0,0.95),(0,0,1.55),"root"),
        "head": ((0,0,1.55),(0,0,2.02),"spine"),
        "upper_arm.L": ((-0.28,0,1.48),(-0.56,0,1.16),"spine"),
        "forearm.L": ((-0.56,0,1.16),(-0.64,0,0.89),"upper_arm.L"),
        "upper_arm.R": ((0.28,0,1.48),(0.56,0,1.16),"spine"),
        "forearm.R": ((0.56,0,1.16),(0.64,0,0.89),"upper_arm.R"),
        "thigh.L": ((-0.16,0,0.93),(-0.19,0,0.52),"root"),
        "shin.L": ((-0.19,0,0.52),(-0.19,0,0.14),"thigh.L"),
        "thigh.R": ((0.16,0,0.93),(0.19,0,0.52),"root"),
        "shin.R": ((0.19,0,0.52),(0.19,0,0.14),"thigh.R"),
    }
    for name, (head, tail, parent) in bones.items():
        b = arm_data.edit_bones.new(name)
        b.head, b.tail = head, tail
        if parent:
            b.parent = arm_data.edit_bones[parent]
            b.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.show_in_front = True
    arm.display_type = "WIRE"
    return arm

def attach_to_bone(obj, arm, bone_name):
    bpy.context.view_layer.update()
    bone = arm.pose.bones[bone_name]
    bone_matrix = arm.matrix_world @ bone.matrix
    world = obj.matrix_world.copy()
    obj.parent = arm
    obj.parent_type = "BONE"
    obj.parent_bone = bone_name
    obj.matrix_parent_inverse = bone_matrix.inverted()
    obj.matrix_world = world
    obj["attached_bone"] = bone_name

def animate(arm, name, frames, keys):
    action = bpy.data.actions.new(name)
    arm.animation_data_create()
    arm.animation_data.action = action
    for frame, pose in keys:
        for bone_name, rot in pose.items():
            bone = arm.pose.bones.get(bone_name)
            if not bone:
                continue
            bone.rotation_mode = "XYZ"
            bone.rotation_euler = rot
            bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone_name)
    for fcurve in action.fcurves:
        for key in fcurve.keyframe_points:
            key.interpolation = "BEZIER"
    action.use_fake_user = True
    action["looping"] = True
    action["intended_frames"] = frames
    return action

def build():
    clear_scene()
    steel = make_material("PBR_BlackenedSteel", (0.075,0.095,0.12), 0.82, 0.28)
    edge = make_material("PBR_EdgeSilver", (0.34,0.39,0.43), 0.78, 0.24)
    leather = make_material("PBR_OxbloodLeather", (0.16,0.035,0.025), 0.12, 0.62)
    cloth = make_material("PBR_CharcoalCloth", (0.035,0.025,0.03), 0.0, 0.92)
    gold = make_material("PBR_AgedBrass", (0.42,0.22,0.055), 0.72, 0.32)
    ember = make_material("PBR_EmberRune", (0.8,0.08,0.012), 0.25, 0.3, (1.0,0.045,0.005))
    skin = make_material("PBR_WeatheredSkin", (0.31,0.19,0.13), 0.0, 0.82)

    arm = create_armature()
    parts = []
    def add(obj, bone):
        parts.append((obj,bone))
        return obj

    # Boots, greaves and layered leg armor
    for side, x in (("L",-0.19),("R",0.19)):
        add(cube("Boot_"+side,(x,-0.055,0.105),(0.23,0.36,0.18),leather,0.045),"shin."+side)
        add(cube("Greave_"+side,(x,-0.105,0.34),(0.22,0.20,0.32),steel,0.045),"shin."+side)
        add(cube("KneeGuard_"+side,(x,-0.145,0.54),(0.24,0.13,0.17),edge,0.045),"thigh."+side)
        add(cube("Tasset_"+side,(x,-0.02,0.79),(0.28,0.20,0.30),steel,0.045),"thigh."+side)
        add(cube("ThighStrap_"+side,(x,-0.13,0.77),(0.29,0.045,0.045),leather,0.012),"thigh."+side)

    # Torso and overlapping breastplate
    add(cube("ArmoredTorso",(0,0,1.25),(0.55,0.34,0.58),leather,0.10),"spine")
    add(cube("Breastplate_Main",(0,-0.19,1.36),(0.49,0.13,0.36),steel,0.075),"spine")
    add(cube("Breastplate_Ridge",(0,-0.265,1.39),(0.055,0.035,0.31),edge,0.012),"spine")
    add(cube("Belt",(0,-0.005,1.00),(0.58,0.39,0.105),leather,0.025),"spine")
    add(cube("Buckle",(0,-0.213,1.00),(0.12,0.035,0.09),gold,0.018),"spine")
    for z in (1.17,1.24,1.31):
        add(cube("RibPlate",(0,-0.263,z),(0.37,0.025,0.025),edge,0.008),"spine")

    # Shoulders, arms and gauntlets
    for side, sign in (("L",-1),("R",1)):
        add(sphere("Pauldron_"+side,(sign*0.36,0,1.49),(0.23,0.25,0.19),steel),"spine")
        add(cone("PauldronSpike_"+side,(sign*0.40,0,1.66),0.085,0.012,0.23,edge),"spine")
        add(cube("UpperArm_"+side,(sign*0.47,0,1.30),(0.19,0.22,0.32),leather,0.05),"upper_arm."+side)
        add(cube("Bracer_"+side,(sign*0.59,-0.01,1.04),(0.19,0.24,0.26),steel,0.045),"forearm."+side)
        add(cube("Gauntlet_"+side,(sign*0.64,-0.02,0.88),(0.17,0.21,0.13),edge,0.035),"forearm."+side)
        for i in range(3):
            add(cube("BracerRivet_"+side,(sign*(0.53+i*0.045),-0.14,1.08),(0.025,0.018,0.025),gold,0.006),"forearm."+side)

    # Hooded, closed helmet and face slit
    add(sphere("Helmet_Crown",(0,0,1.78),(0.205,0.19,0.24),steel),"head")
    add(cube("Helmet_Faceplate",(0,-0.16,1.70),(0.27,0.075,0.22),edge,0.04),"head")
    add(cube("Visor_Shadow",(0,-0.202,1.75),(0.19,0.018,0.035),cloth,0.008),"head")
    add(cube("Visor_EmberGlow",(0,-0.215,1.75),(0.14,0.012,0.012),ember,0.004),"head")
    add(cone("Helmet_Crest",(0,0,2.00),0.10,0.012,0.28,gold,10),"head")
    for x in (-0.11,0.11):
        add(cube("Helmet_Ridge",(x,-0.12,1.80),(0.035,0.035,0.18),edge,0.01),"head")

    # Ragged segmented cloak panels down the back
    for i in range(5):
        x = (i-2)*0.105
        panel = cube("CloakPanel_%02d"%i,(x,0.205,0.83),(0.115,0.055,0.83-(abs(i-2)*0.08)),cloth,0.025)
        panel.rotation_euler[1] = (i-2)*0.045
        add(panel,"spine")
    add(cube("CloakShoulder",(0,0.17,1.42),(0.48,0.09,0.22),cloth,0.035),"spine")
    add(cube("RuneChest",(0,-0.285,1.48),(0.09,0.018,0.10),ember,0.012),"spine")

    # Long sword held in the right hand; blade points down for a readable menu silhouette.
    add(cylinder("Sword_Grip",(0.65,-0.04,0.77),0.035,0.23,leather,10),"forearm.R")
    add(cube("Sword_Crossguard",(0.65,-0.04,0.90),(0.28,0.055,0.045),gold,0.012),"forearm.R")
    add(cube("Sword_Blade",(0.65,-0.04,0.54),(0.10,0.035,0.63),edge,0.012),"forearm.R")
    add(cone("Sword_Point",(0.65,-0.04,0.20),0.05,0.0,0.12,edge,4),"forearm.R")
    add(sphere("Sword_Pommel",(0.65,-0.04,0.91),(0.055,0.055,0.055),gold),"forearm.R")

    # Small belt pouches and back scabbard detail
    add(cube("Pouch_L",(-0.27,-0.01,0.94),(0.13,0.14,0.16),leather,0.025),"spine")
    add(cube("Pouch_R",(0.27,-0.01,0.94),(0.13,0.14,0.16),leather,0.025),"spine")
    add(cube("Scabbard",(0.22,0.25,1.05),(0.10,0.08,0.58),steel,0.025),"spine")

    for obj,bone in parts:
        attach_to_bone(obj,arm,bone)

    # Animation clips are deliberately conservative for a heavy armored warrior.
    def pose(root=(0,0,0), spine=(0,0,0), head=(0,0,0), arm_l=(0,0,0), fore_l=(0,0,0),
             arm_r=(0,0,0), fore_r=(0,0,0), thigh_l=(0,0,0), shin_l=(0,0,0),
             thigh_r=(0,0,0), shin_r=(0,0,0)):
        return {"root":root,"spine":spine,"head":head,"upper_arm.L":arm_l,"forearm.L":fore_l,
                "upper_arm.R":arm_r,"forearm.R":fore_r,"thigh.L":thigh_l,"shin.L":shin_l,
                "thigh.R":thigh_r,"shin.R":shin_r}
    animate(arm,"Idle",48,[
        (1,pose(spine=(0,0,0.01),head=(0,0.015,0))),
        (24,pose(spine=(0.012,0,0),head=(0,-0.012,0))),
        (48,pose(spine=(0,0,0.01),head=(0,0.015,0)))])
    animate(arm,"Walk",24,[
        (1,pose(arm_l=(0.35,0,0),arm_r=(-0.35,0,0),thigh_l=(-0.42,0,0),thigh_r=(0.42,0,0),shin_l=(0.25,0,0),shin_r=(0,0,0))),
        (7,pose(arm_l=(0,0,0),arm_r=(0,0,0),thigh_l=(0,0,0),thigh_r=(0,0,0),shin_l=(0,0,0),shin_r=(0,0,0))),
        (13,pose(arm_l=(-0.35,0,0),arm_r=(0.35,0,0),thigh_l=(0.42,0,0),thigh_r=(-0.42,0,0),shin_l=(0,0,0),shin_r=(0.25,0,0))),
        (19,pose(arm_l=(0,0,0),arm_r=(0,0,0),thigh_l=(0,0,0),thigh_r=(0,0,0),shin_l=(0,0,0),shin_r=(0,0,0))),
        (25,pose(arm_l=(0.35,0,0),arm_r=(-0.35,0,0),thigh_l=(-0.42,0,0),thigh_r=(0.42,0,0),shin_l=(0.25,0,0),shin_r=(0,0,0)))])
    animate(arm,"Run",16,[
        (1,pose(spine=(-0.10,0,0),arm_l=(0.70,0,0),arm_r=(-0.70,0,0),fore_l=(-0.55,0,0),fore_r=(-0.55,0,0),thigh_l=(-0.72,0,0),thigh_r=(0.72,0,0),shin_l=(0.70,0,0),shin_r=(0.05,0,0))),
        (5,pose(spine=(-0.06,0,0),arm_l=(0.1,0,0),arm_r=(-0.1,0,0),fore_l=(-0.35,0,0),fore_r=(-0.35,0,0),thigh_l=(0,0,0),thigh_r=(0,0,0),shin_l=(0.05,0,0),shin_r=(0.05,0,0))),
        (9,pose(spine=(-0.10,0,0),arm_l=(-0.70,0,0),arm_r=(0.70,0,0),fore_l=(-0.55,0,0),fore_r=(-0.55,0,0),thigh_l=(0.72,0,0),thigh_r=(-0.72,0,0),shin_l=(0.05,0,0),shin_r=(0.70,0,0))),
        (13,pose(spine=(-0.06,0,0),arm_l=(-0.1,0,0),arm_r=(0.1,0,0),fore_l=(-0.35,0,0),fore_r=(-0.35,0,0),thigh_l=(0,0,0),thigh_r=(0,0,0),shin_l=(0.05,0,0),shin_r=(0.05,0,0))),
        (17,pose(spine=(-0.10,0,0),arm_l=(0.70,0,0),arm_r=(-0.70,0,0),fore_l=(-0.55,0,0),fore_r=(-0.55,0,0),thigh_l=(-0.72,0,0),thigh_r=(0.72,0,0),shin_l=(0.70,0,0),shin_r=(0.05,0,0)))])
    # Export only the armature and its bound meshes. Blender's FBX exporter embeds actions.
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for obj,bone in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=OUTPUT_FBX, use_selection=True, object_types={"ARMATURE","MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        use_mesh_modifiers=True, mesh_smooth_type="FACE", path_mode="AUTO")
    print("ASH_WARRIOR_EXPORTED=" + OUTPUT_FBX)
    print("ASH_WARRIOR_ACTIONS=Idle,Walk,Run")
    print("ASH_WARRIOR_MESH_PARTS=" + str(len(parts)))

if __name__ == "__main__":
    build()
