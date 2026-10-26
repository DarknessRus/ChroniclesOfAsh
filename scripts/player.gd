extends CharacterBody3D

@export var move_speed: float = 5.0
var health: int = 100
var experience: int = 0
var level: int = 1
var attack_cooldown: float = 0.0

@onready var body_mesh: MeshInstance3D = $Body

func _ready() -> void:
    var sphere := SphereMesh.new()
    sphere.radius = 0.55
    sphere.height = 1.4
    body_mesh.mesh = sphere
    var collision := CollisionShape3D.new()
    var capsule := CapsuleShape3D.new()
    capsule.radius = 0.45
    capsule.height = 1.5
    collision.shape = capsule
    add_child(collision)
    var material := StandardMaterial3D.new()
    material.albedo_color = Color(0.95, 0.56, 0.20)
    material.roughness = 0.9
    body_mesh.material_override = material
    var world = get_parent()
    if world.has_node("HUD/AttackButton"):
        world.get_node("HUD/AttackButton").pressed.connect(attack)

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
    var enemy = get_parent().get_node_or_null("Enemy")
    if enemy and global_position.distance_to(enemy.global_position) < 2.5:
        enemy.take_damage(25)
        if enemy.health <= 0:
            experience += 25
            if experience >= level * 50:
                experience = 0
                level += 1
            get_parent().update_status(level, health, experience)
