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
    var mat := StandardMaterial3D.new()
    mat.roughness = 0.78
    match class_id:
        "mage": mat.albedo_color = Color(0.20, 0.42, 0.95)
        "ranger": mat.albedo_color = Color(0.20, 0.62, 0.30)
        "necromancer": mat.albedo_color = Color(0.46, 0.24, 0.62)
        _: mat.albedo_color = Color(0.82, 0.28, 0.12)
    var capsule_mesh := CapsuleMesh.new()
    capsule_mesh.radius = 0.48
    capsule_mesh.height = 1.55
    body_mesh.mesh = capsule_mesh
    body_mesh.material_override = mat
    # Class-specific silhouette: weapon/tool meshes are simple readable placeholders.
    var old_weapon := get_node_or_null("ClassWeapon")
    if old_weapon:
        old_weapon.queue_free()
    var weapon := MeshInstance3D.new()
    weapon.name = "ClassWeapon"
    var weapon_mat := StandardMaterial3D.new()
    weapon_mat.albedo_color = Color(0.66, 0.67, 0.72)
    if class_id == "necromancer":
        weapon_mat.albedo_color = Color(0.32, 0.95, 0.54)
        weapon_mat.emission_enabled = true
        weapon_mat.emission = Color(0.1, 0.65, 0.25)
    if class_id == "mage":
        weapon_mat.albedo_color = Color(0.35, 0.8, 1.0)
        weapon_mat.emission_enabled = true
        weapon_mat.emission = Color(0.1, 0.4, 1.0)
    if class_id == "ranger":
        var bow := TorusMesh.new()
        bow.inner_radius = 0.12
        bow.outer_radius = 0.18
        weapon.mesh = bow
    else:
        var weapon_mesh := BoxMesh.new()
        weapon_mesh.size = Vector3(0.18, 0.85, 0.18)
        weapon.mesh = weapon_mesh
    weapon.position = Vector3(0.55, 0.25, -0.1)
    weapon.rotation.z = 0.35
    weapon.material_override = weapon_mat
    add_child(weapon)

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
