extends CharacterBody3D

@export var move_speed: float = 5.0
var health: int = 100
var experience: int = 0
var level: int = 1
var attack_cooldown: float = 0.0
var touch_up := false
var touch_down := false
var touch_left := false
var touch_right := false

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
    var hud = get_parent().get_node_or_null("HUD")
    if hud:
        var attack_button = hud.get_node_or_null("AttackButton")
        if attack_button:
            attack_button.pressed.connect(attack)
        _connect_direction_button(hud, "UpButton", "up")
        _connect_direction_button(hud, "DownButton", "down")
        _connect_direction_button(hud, "LeftButton", "left")
        _connect_direction_button(hud, "RightButton", "right")

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
    var enemy = get_parent().get_node_or_null("Enemy")
    if enemy and not enemy.is_queued_for_deletion() and global_position.distance_to(enemy.global_position) < 2.5:
        var was_alive: bool = enemy.health > 0
        enemy.take_damage(25)
        if was_alive and enemy.health <= 0:
            experience += 25
            if experience >= level * 50:
                experience -= level * 50
                level += 1
            get_parent().update_status(level, health, experience)
