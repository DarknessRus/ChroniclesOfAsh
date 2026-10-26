extends Node3D

@onready var status_label: Label = $HUD/Status
var ai_story: Node
var world_seed: int = 0
var world_size: float = 180.0
var dungeon_count: int = 0

func _ready() -> void:
    randomize()
    world_seed = randi()
    var env := $WorldEnvironment.environment
    env.background_color = Color(0.035, 0.045, 0.065)
    env.ambient_light_color = Color(0.38, 0.42, 0.52)
    env.ambient_light_energy = 0.42
    env.fog_enabled = true
    env.fog_light_color = Color(0.12, 0.14, 0.19)
    env.fog_density = 0.008
    env.glow_enabled = true
    env.glow_intensity = 0.55
    env.glow_strength = 0.7
    env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
    $Sun.light_color = Color(0.72, 0.70, 0.78)
    $Sun.light_energy = 0.82
    var ground_material := StandardMaterial3D.new()
    ground_material.albedo_color = Color(0.105, 0.115, 0.105)
    ground_material.roughness = 1.0
    $Ground/GroundMesh.material_override = ground_material
    var ground_shape := BoxShape3D.new()
    ground_shape.size = Vector3(world_size, 1.0, world_size)
    $Ground/GroundCollision.shape = ground_shape
    $Ground/GroundMesh.mesh = _box_mesh(Vector3(world_size, 1.0, world_size))
    $Ground.position.y = -0.5
    _build_world()
    _build_class_picker()
    _spawn_enemy(Vector3(5, 0.8, -4), "slime")
    _spawn_enemy(Vector3(-13, 0.8, -9), "ash_wolf")
    _spawn_enemy(Vector3(15, 0.8, 13), "bone_wretch")
    ai_story = preload("res://scripts/ai_story.gd").new()
    ai_story.name = "AIStory"
    add_child(ai_story)
    ai_story.setup(self)

func _box_mesh(size: Vector3) -> BoxMesh:
    var mesh := BoxMesh.new()
    mesh.size = size
    return mesh

func _build_world() -> void:
    # A branching river and its stony banks.
    _add_box_visual("RiverNorth", Vector3(10, 0.12, 66), Vector3(-34, -0.12, -50), Color(0.06, 0.20, 0.30), 0.0)
    _add_box_visual("RiverSouth", Vector3(10, 0.12, 62), Vector3(-34, -0.12, 49), Color(0.06, 0.20, 0.30), 0.0)
    _add_box_visual("RiverBend", Vector3(58, 0.12, 10), Vector3(-5, -0.12, -8), Color(0.06, 0.20, 0.30), -0.24)
    var rng := RandomNumberGenerator.new()
    rng.seed = world_seed
    for i in range(230):
        var x := rng.randf_range(-world_size * 0.47, world_size * 0.47)
        var z := rng.randf_range(-world_size * 0.47, world_size * 0.47)
        if Vector2(x, z).length() < 9.0 or (x < -27.0 and x > -42.0):
            continue
        _make_tree(Vector3(x, 0, z), rng.randf_range(0.75, 1.55), rng)
    for i in range(110):
        var x := rng.randf_range(-82, 82)
        var z := rng.randf_range(-82, 82)
        if Vector2(x, z).length() < 13.0:
            continue
        _make_rock(Vector3(x, 0, z), rng.randf_range(0.35, 1.8), rng.randf_range(0.45, 2.2))
    # Sparse grass, fallen shale and ash clumps keep the ground from looking flat.
    for i in range(260):
        var detail_pos := Vector3(rng.randf_range(-world_size * 0.46, world_size * 0.46), 0.0, rng.randf_range(-world_size * 0.46, world_size * 0.46))
        if Vector2(detail_pos.x, detail_pos.z).length() < 8.0 or (detail_pos.x < -28.0 and detail_pos.x > -41.0):
            continue
        _make_ground_detail(detail_pos, rng)
    # Jagged mountain ring on the far horizon.
    for i in range(34):
        var angle := TAU * float(i) / 34.0
        var distance := rng.randf_range(74.0, 88.0)
        var height := rng.randf_range(10.0, 22.0)
        _make_mountain(Vector3(cos(angle) * distance, height * 0.35, sin(angle) * distance), height, rng.randf_range(7.0, 13.0))
    _make_ruin(Vector3(25, 0, -35))
    _make_ruin(Vector3(-57, 0, -28))
    _make_cave_entrance(Vector3(-45, 0, 38), "Пещера Чёрного Эха")
    _make_cave_entrance(Vector3(56, 0, 24), "Склеп Пепельного Короля")
    _make_bonfire(Vector3(2, 0, 2))



func _make_ground_detail(pos: Vector3, rng: RandomNumberGenerator) -> void:
    if rng.randf() < 0.58:
        var tuft := MeshInstance3D.new()
        var mesh := CylinderMesh.new()
        mesh.top_radius = 0.015
        mesh.bottom_radius = rng.randf_range(0.07, 0.14)
        mesh.height = rng.randf_range(0.14, 0.34)
        mesh.radial_segments = 4
        tuft.mesh = mesh
        tuft.position = pos + Vector3(0, mesh.height * 0.5, 0)
        tuft.rotation = Vector3(rng.randf_range(-0.18, 0.18), rng.randf_range(0, TAU), rng.randf_range(-0.18, 0.18))
        tuft.material_override = _simple_material([Color(0.12, 0.16, 0.11), Color(0.20, 0.18, 0.12), Color(0.08, 0.12, 0.11)][rng.randi_range(0, 2)])
        add_child(tuft)
    else:
        var shard := MeshInstance3D.new()
        var mesh := BoxMesh.new()
        mesh.size = Vector3(rng.randf_range(0.12, 0.42), rng.randf_range(0.06, 0.18), rng.randf_range(0.12, 0.38))
        shard.mesh = mesh
        shard.position = pos + Vector3(0, mesh.size.y * 0.5, 0)
        shard.rotation.y = rng.randf_range(0, TAU)
        shard.rotation.z = rng.randf_range(-0.12, 0.12)
        shard.material_override = _simple_material(Color(0.16, 0.17, 0.18))
        add_child(shard)

func _make_bonfire(pos: Vector3) -> void:
    var base := MeshInstance3D.new()
    var base_mesh := CylinderMesh.new()
    base_mesh.top_radius = 0.55
    base_mesh.bottom_radius = 0.72
    base_mesh.height = 0.25
    base.mesh = base_mesh
    base.position = pos + Vector3(0, 0.12, 0)
    base.material_override = _simple_material(Color(0.12, 0.10, 0.09))
    add_child(base)
    for i in range(3):
        var log := MeshInstance3D.new()
        var log_mesh := CylinderMesh.new()
        log_mesh.top_radius = 0.10
        log_mesh.bottom_radius = 0.13
        log_mesh.height = 1.0
        log.mesh = log_mesh
        log.position = pos + Vector3(0, 0.32, 0)
        log.rotation.z = PI * 0.5
        log.rotation.y = float(i) * PI / 3.0
        log.material_override = _simple_material(Color(0.20, 0.095, 0.045))
        add_child(log)
    var flame := MeshInstance3D.new()
    var flame_mesh := SphereMesh.new()
    flame_mesh.radius = 0.42
    flame_mesh.height = 1.25
    flame.mesh = flame_mesh
    flame.position = pos + Vector3(0, 0.88, 0)
    flame.scale = Vector3(0.65, 1.0, 0.65)
    flame.material_override = _simple_material(Color(1.0, 0.22, 0.025), true)
    add_child(flame)
    var light := OmniLight3D.new()
    light.position = pos + Vector3(0, 1.3, 0)
    light.light_color = Color(1.0, 0.28, 0.07)
    light.light_energy = 2.2
    light.omni_range = 11.0
    add_child(light)

func _simple_material(color: Color, emissive: bool = false) -> StandardMaterial3D:
    var mat := StandardMaterial3D.new()
    mat.albedo_color = color
    mat.roughness = 0.9
    if emissive:
        mat.emission_enabled = true
        mat.emission = color
        mat.emission_energy_multiplier = 2.0
    return mat

func _add_box_visual(node_name: String, size: Vector3, pos: Vector3, color: Color, rotation_y: float) -> void:
    var visual := MeshInstance3D.new()
    visual.name = node_name
    visual.mesh = _box_mesh(size)
    visual.position = pos
    visual.rotation.y = rotation_y
    var mat := StandardMaterial3D.new()
    mat.albedo_color = color
    mat.roughness = 0.25 if color.b > color.r else 1.0
    visual.material_override = mat
    add_child(visual)

func _make_tree(pos: Vector3, scale_factor: float, rng: RandomNumberGenerator) -> void:
    var trunk := MeshInstance3D.new()
    var trunk_mesh := CylinderMesh.new()
    trunk_mesh.top_radius = 0.16 * scale_factor
    trunk_mesh.bottom_radius = 0.32 * scale_factor
    trunk_mesh.height = 2.1 * scale_factor
    trunk.mesh = trunk_mesh
    trunk.position = pos + Vector3(0, trunk_mesh.height * 0.5, 0)
    var bark := StandardMaterial3D.new()
    bark.albedo_color = Color(0.18, 0.12, 0.10)
    trunk.material_override = bark
    add_child(trunk)
    for branch in range(3):
        var crown := MeshInstance3D.new()
        var crown_mesh := SphereMesh.new()
        crown_mesh.radius = rng.randf_range(0.8, 1.15) * scale_factor
        crown_mesh.height = crown_mesh.radius * 1.9
        crown.mesh = crown_mesh
        crown.position = pos + Vector3(rng.randf_range(-0.25, 0.25), (1.7 + branch * 0.55) * scale_factor, rng.randf_range(-0.25, 0.25))
        var leaf := StandardMaterial3D.new()
        leaf.albedo_color = [Color(0.06, 0.16, 0.12), Color(0.10, 0.20, 0.14), Color(0.18, 0.19, 0.13)][rng.randi_range(0, 2)]
        leaf.roughness = 1.0
        crown.material_override = leaf
        add_child(crown)

func _make_rock(pos: Vector3, size: float, height: float) -> void:
    var rock := MeshInstance3D.new()
    var mesh := SphereMesh.new()
    mesh.radius = size
    mesh.height = height
    rock.mesh = mesh
    rock.position = pos + Vector3(0, height * 0.35, 0)
    rock.scale = Vector3(1.0, 0.8, rng_scale())
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color(0.22, 0.23, 0.25)
    mat.roughness = 1.0
    rock.material_override = mat
    add_child(rock)

func rng_scale() -> float:
    return randf_range(0.7, 1.3)

func _make_mountain(pos: Vector3, height: float, radius: float) -> void:
    var mountain := MeshInstance3D.new()
    var mesh := CylinderMesh.new()
    mesh.top_radius = 0.4
    mesh.bottom_radius = radius
    mesh.height = height
    mesh.radial_segments = 5
    mountain.mesh = mesh
    mountain.position = pos
    mountain.rotation = Vector3(randf_range(-0.12, 0.12), randf_range(0, TAU), randf_range(-0.12, 0.12))
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color(0.13, 0.14, 0.17)
    mat.roughness = 1.0
    mountain.material_override = mat
    add_child(mountain)

func _make_ruin(pos: Vector3) -> void:
    for i in range(7):
        var pillar := MeshInstance3D.new()
        var mesh := BoxMesh.new()
        mesh.size = Vector3(0.8, randf_range(2.5, 4.5), 0.8)
        pillar.mesh = mesh
        pillar.position = pos + Vector3(cos(i * TAU / 7.0) * 5.0, mesh.size.y * 0.5, sin(i * TAU / 7.0) * 5.0)
        var mat := StandardMaterial3D.new()
        mat.albedo_color = Color(0.27, 0.25, 0.23)
        pillar.material_override = mat
        add_child(pillar)

func _make_cave_entrance(pos: Vector3, cave_name: String) -> void:
    var arch := MeshInstance3D.new()
    var mesh := TorusMesh.new()
    mesh.inner_radius = 1.6
    mesh.outer_radius = 2.4
    arch.mesh = mesh
    arch.position = pos + Vector3(0, 2.0, 0)
    arch.rotation.x = PI * 0.5
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color(0.16, 0.12, 0.22)
    mat.emission_enabled = true
    mat.emission = Color(0.24, 0.08, 0.42)
    mat.emission_energy_multiplier = 1.2
    arch.material_override = mat
    add_child(arch)
    var marker := Label3D.new()
    marker.text = cave_name
    marker.position = pos + Vector3(0, 4.2, 0)
    marker.font_size = 42
    marker.modulate = Color(0.86, 0.74, 1.0)
    add_child(marker)
    var trigger := Area3D.new()
    trigger.name = "CaveTrigger" + str(dungeon_count)
    trigger.position = pos
    var shape_node := CollisionShape3D.new()
    var shape := SphereShape3D.new()
    shape.radius = 3.0
    shape_node.shape = shape
    trigger.add_child(shape_node)
    trigger.body_entered.connect(_on_cave_entered.bind(cave_name, pos))
    add_child(trigger)
    dungeon_count += 1

func _on_cave_entered(body: Node3D, cave_name: String, pos: Vector3) -> void:
    if body.name != "Player":
        return
    _generate_local_dungeon(cave_name, pos)

func _generate_local_dungeon(cave_name: String, pos: Vector3) -> void:
    var dungeon: Node3D = get_node_or_null("ActiveDungeon") as Node3D
    if dungeon:
        dungeon.queue_free()
    dungeon = Node3D.new()
    dungeon.name = "ActiveDungeon"
    dungeon.position = pos + Vector3(0, 0, -12)
    add_child(dungeon)
    var floor := MeshInstance3D.new()
    var floor_mesh := BoxMesh.new()
    floor_mesh.size = Vector3(28, 0.5, 28)
    floor.mesh = floor_mesh
    var floor_mat := StandardMaterial3D.new()
    floor_mat.albedo_color = Color(0.10, 0.09, 0.12)
    floor.material_override = floor_mat
    dungeon.add_child(floor)
    for i in range(12):
        var wall := MeshInstance3D.new()
        var wall_mesh := BoxMesh.new()
        wall_mesh.size = Vector3(28 if i < 4 else 0.7, 3.5, 0.7 if i < 4 else 28)
        wall.mesh = wall_mesh
        var sides := [Vector3(0, 1.5, -14), Vector3(0, 1.5, 14), Vector3(-14, 1.5, 0), Vector3(14, 1.5, 0)]
        wall.position = sides[i % 4]
        var wall_mat := StandardMaterial3D.new()
        wall_mat.albedo_color = Color(0.17, 0.15, 0.18)
        wall.material_override = wall_mat
        dungeon.add_child(wall)
    for i in range(4):
        var chest := MeshInstance3D.new()
        var chest_mesh := BoxMesh.new()
        chest_mesh.size = Vector3(1.2, 0.8, 0.8)
        chest.mesh = chest_mesh
        chest.position = Vector3(randf_range(-9, 9), 0.65, randf_range(-9, 9))
        var chest_mat := StandardMaterial3D.new()
        chest_mat.albedo_color = Color(0.55, 0.28, 0.07) if i < 3 else Color(0.55, 0.42, 0.12)
        chest.material_override = chest_mat
        dungeon.add_child(chest)
        var label := Label3D.new()
        label.text = "СУНДУК · " + ["Зелье", "Золото", "Реликвия", "Редкий лут"][i]
        label.position = chest.position + Vector3(0, 1.0, 0)
        label.font_size = 30
        dungeon.add_child(label)
    for i in range(5):
        _spawn_enemy(dungeon.position + Vector3(randf_range(-8, 8), 0.8, randf_range(-8, 8)), ["ash_wolf", "bone_wretch", "slime"][randi_range(0, 2)])
    var announcement := Label3D.new()
    announcement.text = "ПОДЗЕМЕЛЬЕ: " + cave_name
    announcement.position = dungeon.position + Vector3(0, 4, 0)
    announcement.font_size = 48
    announcement.modulate = Color(1.0, 0.55, 0.18)
    add_child(announcement)
    if ai_story != null:
        ai_story.note_world_event("Игрок открыл подземелье: " + cave_name)

func spawn_ai_dungeon(event_data: Dictionary) -> void:
    var title := str(event_data.get("title", "Разлом под пепельным холмом")).strip_edges()
    if title.is_empty():
        title = "Разлом под пепельным холмом"
    var pos := Vector3(randf_range(-65, 65), 0, randf_range(-65, 65))
    _make_cave_entrance(pos, title)
    var kind := str(event_data.get("kind", "haunted_cave"))
    var enemies := int(event_data.get("enemy_count", 5))
    var chests := int(event_data.get("chest_count", 3))
    _generate_local_dungeon(title, pos)
    var notice := Label3D.new()
    notice.text = "НОВАЯ ЛОКАЦИЯ ИИ: " + title + " · " + kind
    notice.position = pos + Vector3(0, 6, 0)
    notice.font_size = 38
    notice.modulate = Color(1.0, 0.46, 0.16)
    add_child(notice)

func _build_class_picker() -> void:
    var hud := $HUD
    var picker := HBoxContainer.new()
    picker.name = "ClassPicker"
    picker.position = Vector2(20, 92)
    picker.add_theme_constant_override("separation", 6)
    hud.add_child(picker)
    var classes := [
        ["ВОИН · ТОПОР", "warrior"],
        ["МАГ · ПОСОХ", "mage"],
        ["ЭЛЬФ · ЛУК", "ranger"],
        ["НЕКРОМАНТ", "necromancer"]
    ]
    for entry in classes:
        var button := Button.new()
        button.text = entry[0]
        button.custom_minimum_size = Vector2(125, 42)
        button.pressed.connect(_select_class.bind(entry[1]))
        picker.add_child(button)

func _select_class(class_id: String) -> void:
    var player := get_node_or_null("Player")
    if player:
        player.set_class(class_id)
        status_label.text = "Класс: %s   Уровень %d   Здоровье: %d   Опыт: %d" % [player.class_title, player.level, player.health, player.experience]

func _spawn_enemy(pos: Vector3, enemy_type: String) -> void:
    var enemy := CharacterBody3D.new()
    enemy.name = "Enemy_" + enemy_type + "_" + str(randi())
    enemy.position = pos
    enemy.set_script(load("res://scripts/enemy.gd"))
    var body := MeshInstance3D.new()
    body.name = "Body"
    enemy.add_child(body)
    add_child(enemy)
    enemy.configure_type(enemy_type)

func update_status(level: int, health: int, experience: int) -> void:
    status_label.text = "Уровень %d   Здоровье: %d   Опыт: %d" % [level, health, experience]

func on_enemy_defeated(enemy_type: String) -> void:
    if ai_story != null:
        ai_story.on_enemy_defeated(enemy_type)
        ai_story.note_world_event("Побеждён противник: " + enemy_type)
    if get_tree() != null:
        await get_tree().create_timer(4.0).timeout
        if is_inside_tree():
            _spawn_enemy(Vector3(randf_range(-65, 65), 0.8, randf_range(-65, 65)), ["slime", "ash_wolf", "bone_wretch"][randi_range(0, 2)])
