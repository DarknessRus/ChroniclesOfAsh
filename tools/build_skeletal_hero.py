#!/usr/bin/env python3
"""Download CC0 skinned character parts and external clips, then assemble a Unity FBX."""
from pathlib import Path
from urllib.request import Request, urlopen
import subprocess, sys

ROOT = Path(__file__).resolve().parents[1]
BASE = "https://raw.githubusercontent.com/NoLimitNexus/game-models/main/"
SRC = ROOT / "build/skeletal-hero-source"
OUT = ROOT / "Unity/Assets/Resources/Characters"
OUT.mkdir(parents=True, exist_ok=True)
SRC.mkdir(parents=True, exist_ok=True)
ASSETS = [
 "library/ours/body/Body.glb",
 "library/ours/parts/top_tunic.glb",
 "library/ours/parts/bottom_trousers.glb",
 "library/ours/parts/boots_tall.glb",
 "library/ours/parts/hair_tousled.glb",
 "library/animations/Rig_Medium_General.glb",
 "library/animations/Rig_Medium_MovementBasic.glb",
 "library/animations/Rig_Medium_MovementAdvanced.glb",
]
for relative in ASSETS:
    dest = SRC / relative
    dest.parent.mkdir(parents=True, exist_ok=True)
    request = Request(BASE + relative, headers={"User-Agent":"ChroniclesOfAsh-AssetBuilder/1.0"})
    with urlopen(request, timeout=90) as response: data = response.read()
    if len(data) < 1024 or not data.startswith(b"glTF"):
        raise RuntimeError(f"Invalid GLB download: {relative} ({len(data)} bytes)")
    dest.write_bytes(data)
    print(f"Downloaded {relative}: {len(data):,} bytes", flush=True)

blend = ROOT / "build/assemble_skeletal_hero.py"
blend.write_text(r'''
import bpy, sys
from pathlib import Path
args=sys.argv[sys.argv.index("--")+1:]
source=Path(args[0]); output=Path(args[1])
bpy.ops.object.select_all(action="SELECT"); bpy.ops.object.delete(use_global=False)
def import_glb(rel):
    before=set(bpy.data.objects); actions=set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=str(source/rel))
    return [o for o in bpy.data.objects if o not in before], [a for a in bpy.data.actions if a not in actions]
base,_=import_glb("library/ours/body/Body.glb")
rigs=[o for o in base if o.type=="ARMATURE"]
if not rigs: raise RuntimeError("Body GLB has no armature; refusing unskinned output")
rig=rigs[0]; rig.name="AshHero_Rig"; rig.data.name="AshHero_Skeleton"
meshes=[o for o in base if o.type=="MESH"]
for rel in ["library/ours/parts/top_tunic.glb","library/ours/parts/bottom_trousers.glb",
            "library/ours/parts/boots_tall.glb","library/ours/parts/hair_tousled.glb"]:
    objects,_=import_glb(rel)
    for mesh in [o for o in objects if o.type=="MESH"]:
        # All published kit pieces use the same 23-bone rig and vertex-group names.
        for mod in list(mesh.modifiers):
            if mod.type=="ARMATURE": mesh.modifiers.remove(mod)
        mod=mesh.modifiers.new("AshHero_Skin","ARMATURE"); mod.object=rig
        mod.use_vertex_groups=True; mod.use_bone_envelopes=False
        mesh.parent=rig
    for old in [o for o in objects if o.type=="ARMATURE"]:
        bpy.data.objects.remove(old,do_unlink=True)
wanted=("Idle_A","Walking_A","Running_A"); found={}
for rel in ["library/animations/Rig_Medium_General.glb",
            "library/animations/Rig_Medium_MovementBasic.glb",
            "library/animations/Rig_Medium_MovementAdvanced.glb"]:
    objects,actions=import_glb(rel)
    for action in actions:
        for key in wanted:
            if key.lower() in action.name.lower() and key not in found: found[key]=action
    for obj in objects:
        if obj.type=="ARMATURE": bpy.data.objects.remove(obj,do_unlink=True)
missing=[x for x in wanted if x not in found]
if missing: raise RuntimeError("Missing external animation clips: "+", ".join(missing))
rig.animation_data_create(); rig.animation_data.action=None
for name in wanted:
    action=found[name]; track=rig.animation_data.nla_tracks.new(); track.name=name
    strip=track.strips.new(name,int(action.frame_range[0]),action); strip.name=name
    strip.action_frame_start=action.frame_range[0]; strip.action_frame_end=action.frame_range[1]
    strip.blend_type="REPLACE"; strip.extrapolation="NOTHING"
bpy.context.scene.unit_settings.system="METRIC"
bpy.ops.object.select_all(action="DESELECT"); rig.select_set(True)
for obj in meshes: obj.select_set(True)
bpy.context.view_layer.objects.active=rig
output.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={"ARMATURE","MESH"},
    apply_scale_options="FBX_SCALE_ALL",add_leaf_bones=False,bake_anim=True,
    bake_anim_use_nla_strips=True,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0,path_mode="COPY",embed_textures=True,axis_forward="-Z",axis_up="Y")
if not output.exists() or output.stat().st_size<100000: raise RuntimeError("FBX output missing or too small")
print("Skinned FBX exported:",output,output.stat().st_size,"bytes")
''',encoding="utf-8")
result=subprocess.run(["blender","-b","--factory-startup","--python",str(blend),"--",str(SRC),str(OUT/"AshHero.fbx")],
                      cwd=str(ROOT),text=True,stdout=sys.stdout,stderr=sys.stderr)
if result.returncode: raise SystemExit(result.returncode)
(OUT/"ASSET_SOURCES.txt").write_text(
 "Chronicles of Ash external skinned hero\n"
 "Character body and outfit parts: NoLimitNexus/game-models, library/ours; source repository documents these meshes as original.\n"
 "External animation clips and skeleton: Kay Lousberg / KayKit, CC0, as documented in the source repository.\n"
 "Source repository: https://github.com/NoLimitNexus/game-models\n"
 "Animation source and license: https://quaternius.itch.io/universal-animation-library\n"
 "FBX is assembled during CI; expected clips: Idle_A, Walking_A, Running_A.\n",encoding="utf-8")
print("External skinned character and clips prepared for Unity.")
