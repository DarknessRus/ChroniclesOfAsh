extends CharacterBody3D

@export var move_speed: float = 6.0
var health: int = 100
var experience: int = 0
var level: int = 1
var attack_cooldown: float = 0.0
var touch_up := false
var touch_down := false
var touch_left := false
var touch_right := false
var class_id := "warrior"
var class_title := "Воин с топором"
var skeleton_summons: Array[Node3D] = []
var attack_serial := 0
@onready var body_mesh: MeshInstance3D = $Body

func _ready() -> void:
    var chosen_class = get_tree().root.get_meta("selected_class", "warrior")
    set_class(str(chosen_class))
    _build_character()
    var collision := get_node_or_null("CollisionShape3D")
    if collision == null:
        collision = CollisionShape3D.new()
        collision.name = "CollisionShape3D"
        var capsule := CapsuleShape3D.new()
        capsule.radius = 0.45
        capsule.height = 1.5
        collision.shape = capsule
        add_child(collision)
    var hud = get_parent().get_node_or_null("HUD")
    if hud:
        var attack_button = hud.get_node_or_null("AttackButton")
        if attack_button:
            attack_button.pressed.connect(attack)
        _connect_direction_button(hud, "UpButton", "up")
        _connect_direction_button(hud, "DownButton", "down")
        _connect_direction_button(hud, "LeftButton", "left")
        _connect_direction_button(hud, "RightButton", "right")

func set_class(next_class: String) -> void:
    class_id = next_class
    match class_id:
        "mage": class_title = "Маг с посохом"
        "ranger": class_title = "Эльф-лучник"
        "necromancer": class_title = "Некромант"
        _: class_id = "warrior"; class_title = "Воин с топором"
    _build_character()

func _build_character() -> void:
    if body_mesh == null:
        return
    var old_art := get_node_or_null("CharacterArt")
    if old_art:
        old_art.queue_free()
    var art := Node3D.new()
    art.name = "CharacterArt"
    add_child(art)
    body_mesh.visible = false
    var armor := Color(0.34,0.30,0.28)
    var cloth := Color(0.14,0.12,0.13)
    var accent := Color(0.95,0.28,0.06)
    match class_id:
        "mage":
            armor = Color(0.13,0.22,0.46); cloth = Color(0.06,0.08,0.18); accent = Color(0.18,0.68,1.0)
        "ranger":
            armor = Color(0.20,0.34,0.22); cloth = Color(0.10,0.18,0.12); accent = Color(0.72,0.84,0.42)
        "necromancer":
            armor = Color(0.27,0.18,0.34); cloth = Color(0.10,0.07,0.15); accent = Color(0.15,1.0,0.40)
        _:
            armor = Color(0.38,0.29,0.23); cloth = Color(0.17,0.11,0.09); accent = Color(0.95,0.28,0.06)
    _part_box(art,"Torso",Vector3(0.58,0.68,0.34),Vector3(0,1.12,0),armor)
    _part_box(art,"Breastplate",Vector3(0.62,0.34,0.39),Vector3(0,1.31,-0.035),armor.lightened(0.12))
    _part_box(art,"Belt",Vector3(0.61,0.10,0.37),Vector3(0,0.88,0),Color(0.09,0.065,0.045))
    _part_box(art,"Buckle",Vector3(0.12,0.12,0.06),Vector3(0,0.88,-0.22),accent)
    _part_sphere(art,"Head",Vector3(0.34,0.38,0.32),Vector3(0,1.77,0),Color(0.62,0.46,0.34))
    _part_box(art,"Helmet",Vector3(0.42,0.20,0.38),Vector3(0,1.98,0),armor.darkened(0.2))
    _part_box(art,"LegL",Vector3(0.23,0.53,0.28),Vector3(-0.18,0.38,0),cloth)
    _part_box(art,"LegR",Vector3(0.23,0.53,0.28),Vector3(0.18,0.38,0),cloth)
    _part_box(art,"BootL",Vector3(0.28,0.16,0.38),Vector3(-0.18,0.08,-0.04),Color(0.08,0.065,0.06))
    _part_box(art,"BootR",Vector3(0.28,0.16,0.38),Vector3(0.18,0.08,-0.04),Color(0.08,0.065,0.06))
    _part_box(art,"ArmL",Vector3(0.20,0.54,0.22),Vector3(-0.41,1.17,0),armor)
    _part_box(art,"ArmR",Vector3(0.20,0.54,0.22),Vector3(0.41,1.17,0),armor)
    _part_box(art,"GloveL",Vector3(0.21,0.18,0.23),Vector3(-0.42,0.88,0),Color(0.11,0.09,0.08))
    _part_box(art,"GloveR",Vector3(0.21,0.18,0.23),Vector3(0.42,0.88,0),Color(0.11,0.09,0.08))
    if class_id == "warrior":
        _part_sphere(art,"ShoulderL",Vector3(0.34,0.30,0.36),Vector3(-0.42,1.48,0),armor.lightened(0.16))
        _part_sphere(art,"ShoulderR",Vector3(0.34,0.30,0.36),Vector3(0.42,1.48,0),armor.lightened(0.16))
        _part_box(art,"HelmetRidge",Vector3(0.08,0.28,0.44),Vector3(0,2.12,0),Color(0.68,0.56,0.38))
    elif class_id == "mage":
        _part_box(art,"Robe",Vector3(0.73,0.75,0.43),Vector3(0,0.98,0.04),cloth)
        _part_box(art,"RobeTrim",Vector3(0.76,0.07,0.45),Vector3(0,0.66,0.04),accent)
        _part_box(art,"HatBrim",Vector3(0.65,0.08,0.60),Vector3(0,2.08,0),cloth)
        _part_cone(art,"WizardHat",0.22,0.55,Vector3(0,2.36,0),cloth)
    elif class_id == "ranger":
        _part_box(art,"Hood",Vector3(0.47,0.27,0.42),Vector3(0,2.02,0.02),cloth)
        _part_box(art,"ElfEarL",Vector3(0.22,0.07,0.08),Vector3(-0.27,1.80,0),Color(0.65,0.49,0.36),Vector3(0,0,-0.30))
        _part_box(art,"ElfEarR",Vector3(0.22,0.07,0.08),Vector3(0.27,1.80,0),Color(0.65,0.49,0.36),Vector3(0,0,0.30))
        _part_box(art,"Quiver",Vector3(0.20,0.48,0.16),Vector3(-0.30,1.40,0.18),Color(0.35,0.20,0.10),Vector3(0,0,-0.18))
    else:
        _part_box(art,"Hood",Vector3(0.49,0.34,0.43),Vector3(0,2.02,0.02),cloth)
        _part_sphere(art,"SoulGem",Vector3(0.14,0.14,0.08),Vector3(0,1.39,-0.23),accent,true)
        _part_box(art,"BoneShoulderL",Vector3(0.31,0.28,0.34),Vector3(-0.42,1.48,0),Color(0.65,0.62,0.52))
    var weapon := Node3D.new()
    weapon.name = "ClassWeapon"
    weapon.position = Vector3(0.58,1.03,-0.12)
    weapon.rotation.z = -0.22
    art.add_child(weapon)
    if class_id == "warrior":
        _part_cylinder(weapon,"AxeHandle",0.045,0.045,1.0,Vector3(0,0.22,0),Color(0.25,0.13,0.07))
        _part_box(weapon,"AxeBlade",Vector3(0.42,0.36,0.12),Vector3(-0.18,0.65,0),Color(0.65,0.68,0.71),Vector3(0,0,-0.12))
        _part_box(weapon,"AxeEdge",Vector3(0.05,0.31,0.14),Vector3(-0.37,0.65,0),Color(0.88,0.88,0.84))
    elif class_id == "mage":
        _part_cylinder(weapon,"Staff",0.035,0.055,1.45,Vector3(0,0.20,0),Color(0.28,0.17,0.10))
        _part_sphere(weapon,"StaffCrystal",Vector3(0.22,0.24,0.22),Vector3(0,0.96,0),accent,true)
        _part_box(weapon,"CrystalBand",Vector3(0.30,0.06,0.30),Vector3(0,0.83,0),Color(0.72,0.65,0.42))
    elif class_id == "ranger":
        _part_cylinder(weapon,"BowGrip",0.035,0.035,0.72,Vector3(0,0.20,0),Color(0.40,0.25,0.12))
        _part_box(weapon,"BowUpper",Vector3(0.09,0.38,0.09),Vector3(0,0.60,0),Color(0.48,0.31,0.15),Vector3(0,0,-0.28))
        _part_box(weapon,"BowLower",Vector3(0.09,0.38,0.09),Vector3(0,-0.20,0),Color(0.48,0.31,0.15),Vector3(0,0,0.28))
        _part_cylinder(weapon,"Arrow",0.018,0.018,0.80,Vector3(0.08,0.20,-0.05),Color(0.82,0.78,0.66))
    else:
        _part_cylinder(weapon,"BoneStaff",0.04,0.06,1.4,Vector3(0,0.20,0),Color(0.72,0.69,0.59))
        _part_sphere(weapon,"SoulOrb",Vector3(0.25,0.25,0.25),Vector3(0,0.92,0),accent,true)
        _part_box(weapon,"SkullMark",Vector3(0.16,0.13,0.12),Vector3(0,0.66,-0.02),Color(0.85,0.83,0.73))

func _material(color: Color, emissive: bool = false) -> StandardMaterial3D:
    var mat := StandardMaterial3D.new()
    mat.albedo_color = color
    mat.roughness = 0.72
    if emissive:
        mat.emission_enabled = true
        mat.emission = color
        mat.emission_energy_multiplier = 1.5
    return mat

func _part_box(parent: Node3D, part_name: String, dimensions: Vector3, pos: Vector3, color: Color, rot: Vector3 = Vector3.ZERO) -> void:
    var part := MeshInstance3D.new()
    part.name = part_name
    var mesh := BoxMesh.new()
    mesh.size = dimensions
    part.mesh = mesh
    part.position = pos
    part.rotation = rot
    part.material_override = _material(color)
    parent.add_child(part)

func _part_sphere(parent: Node3D, part_name: String, dimensions: Vector3, pos: Vector3, color: Color, emissive: bool = false) -> void:
    var part := MeshInstance3D.new()
    part.name = part_name
    var mesh := SphereMesh.new()
    mesh.radius = 0.5
    mesh.height = 1.0
    part.mesh = mesh
    part.scale = dimensions
    part.position = pos
    part.material_override = _material(color, emissive)
    parent.add_child(part)

func _part_cylinder(parent: Node3D, part_name: String, top_radius: float, bottom_radius: float, height: float, pos: Vector3, color: Color) -> void:
    var part := MeshInstance3D.new()
    part.name = part_name
    var mesh := CylinderMesh.new()
    mesh.top_radius = top_radius
    mesh.bottom_radius = bottom_radius
    mesh.height = height
    part.mesh = mesh
    part.position = pos
    part.material_override = _material(color)
    parent.add_child(part)

func _part_cone(parent: Node3D, part_name: String, radius: float, height: float, pos: Vector3, color: Color) -> void:
    var part := MeshInstance3D.new()
    part.name = part_name
    var mesh := CylinderMesh.new()
    mesh.top_radius = 0.01
    mesh.bottom_radius = radius
    mesh.height = height
    part.mesh = mesh
    part.position = pos
    part.material_override = _material(color)
    parent.add_child(part)

func _connect_direction_button(hud: Node, button_name: String, direction: String) -> void:
    var button = hud.get_node_or_null(button_name)
    if button:
        button.button_down.connect(_set_touch_direction.bind(direction, true))
        button.button_up.connect(_set_touch_direction.bind(direction, false))

func _set_touch_direction(direction: String, pressed: bool) -> void:
    match direction:
        "up": touch_up = pressed
        "down": touch_down = pressed
        "left": touch_left = pressed
        "right": touch_right = pressed

func _physics_process(delta: float) -> void:
    attack_cooldown = maxf(0.0, attack_cooldown - delta)
    var input_vector := Vector2(
        Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left"),
        Input.get_action_strength("ui_down") - Input.get_action_strength("ui_up")
    )
    if Input.is_key_pressed(KEY_A): input_vector.x -= 1.0
    if Input.is_key_pressed(KEY_D): input_vector.x += 1.0
    if Input.is_key_pressed(KEY_W): input_vector.y -= 1.0
    if Input.is_key_pressed(KEY_S): input_vector.y += 1.0
    if touch_left: input_vector.x -= 1.0
    if touch_right: input_vector.x += 1.0
    if touch_up: input_vector.y -= 1.0
    if touch_down: input_vector.y += 1.0
    input_vector = input_vector.normalized()
    velocity.x = input_vector.x * move_speed
    velocity.z = input_vector.y * move_speed
    velocity.y = 0.0
    move_and_slide()
    if Input.is_action_just_pressed("ui_accept"):
        attack()

func attack() -> void:
    if attack_cooldown > 0.0:
        return
    attack_cooldown = 0.55
    var world := get_parent()
    var nearest: Node3D = null
    var nearest_distance := 9999.0
    for child in world.get_children():
        if child is Node3D and child.has_method("take_damage") and child != self and not child.is_queued_for_deletion():
            var distance := global_position.distance_to(child.global_position)
            if distance < nearest_distance:
                nearest = child
                nearest_distance = distance
    var range := 2.8
    var damage := 28
    if class_id == "mage": range = 13.0; damage = 24
    elif class_id == "ranger": range = 16.0; damage = 22
    elif class_id == "necromancer": range = 9.0; damage = 18
    if nearest != null and nearest_distance <= range:
        if class_id == "mage" or class_id == "ranger":
            _spawn_projectile(nearest.global_position, class_id)
        nearest.call("take_damage", damage)
        if int(nearest.get("health")) <= 0:
            if class_id == "necromancer":
                _summon_skeleton(nearest.global_position)
            experience += 25
            if experience >= level * 50:
                experience -= level * 50
                level += 1
            world.update_status(level, health, experience)
            world.on_enemy_defeated(str(nearest.get("enemy_type")))
    elif class_id == "necromancer" and skeleton_summons.is_empty():
        _summon_skeleton(global_position + Vector3(1.5, 0, 1.5))

func _spawn_projectile(target: Vector3, type: String) -> void:
    var orb := MeshInstance3D.new()
    var sphere := SphereMesh.new()
    sphere.radius = 0.25 if type == "mage" else 0.13
    sphere.height = sphere.radius * 2.0
    orb.mesh = sphere
    orb.position = global_position + Vector3(0, 0.8, 0)
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color(0.2, 0.65, 1.0) if type == "mage" else Color(0.65, 0.9, 0.3)
    mat.emission_enabled = true
    mat.emission = mat.albedo_color
    orb.material_override = mat
    get_parent().add_child(orb)
    var tween := create_tween()
    tween.tween_property(orb, "global_position", target + Vector3(0, 0.6, 0), 0.18)
    tween.tween_callback(orb.queue_free)

func _summon_skeleton(pos: Vector3) -> void:
    skeleton_summons = skeleton_summons.filter(func(s): return is_instance_valid(s))
    if skeleton_summons.size() >= 3:
        return
    var skeleton := CharacterBody3D.new()
    skeleton.name = "SummonedSkeleton"
    skeleton.position = pos + Vector3(randf_range(-1.5, 1.5), 0.7, randf_range(-1.5, 1.5))
    skeleton.set_script(load("res://scripts/skeleton.gd"))
    var skeleton_body := MeshInstance3D.new()
    skeleton_body.name = "Body"
    var mesh := CapsuleMesh.new()
    mesh.radius = 0.28
    mesh.height = 1.0
    skeleton_body.mesh = mesh
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color(0.82, 0.86, 0.78)
    mat.emission_enabled = true
    mat.emission = Color(0.08, 0.3, 0.12)
    skeleton_body.material_override = mat
    skeleton.add_child(skeleton_body)
    get_parent().add_child(skeleton)
    skeleton_summons.append(skeleton)
