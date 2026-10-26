extends CharacterBody3D

var health: int = 50
var speed: float = 1.5
var enemy_type := "slime"
var home_position: Vector3
@onready var body_mesh: MeshInstance3D = $Body

func configure_type(kind: String) -> void:
    enemy_type = kind
    match enemy_type:
        "ash_wolf": health = 65; speed = 2.4
        "bone_wretch": health = 85; speed = 1.25
        _: enemy_type = "slime"; health = 50; speed = 1.5
    if is_inside_tree():
        _style_body()

func _ready() -> void:
    home_position = global_position
    _style_body()
    var collision := get_node_or_null("CollisionShape3D")
    if collision == null:
        collision = CollisionShape3D.new()
        collision.name = "CollisionShape3D"
        var shape := SphereShape3D.new()
        shape.radius = 0.65
        collision.shape = shape
        add_child(collision)

func _style_body() -> void:
    var sphere := SphereMesh.new()
    sphere.radius = 0.65
    sphere.height = 1.3
    body_mesh.mesh = sphere
    var material := StandardMaterial3D.new()
    match enemy_type:
        "ash_wolf": material.albedo_color = Color(0.32, 0.25, 0.20)
        "bone_wretch": material.albedo_color = Color(0.74, 0.72, 0.62)
        _: material.albedo_color = Color(0.42, 0.78, 0.38)
    material.roughness = 1.0
    body_mesh.material_override = material

func _physics_process(delta: float) -> void:
    if health <= 0:
        return
    var player = get_parent().get_node_or_null("Player")
    if player and global_position.distance_to(player.global_position) < 9.0:
        var direction = player.global_position - global_position
        direction.y = 0.0
        if direction.length() > 1.3:
            velocity.x = direction.normalized().x * speed
            velocity.z = direction.normalized().z * speed
            move_and_slide()
    elif global_position.distance_to(home_position) > 0.3:
        var direction_home = home_position - global_position
        velocity.x = direction_home.normalized().x * speed * 0.6
        velocity.z = direction_home.normalized().z * speed * 0.6
        move_and_slide()

func take_damage(amount: int) -> void:
    health -= amount
    if health <= 0:
        queue_free()
