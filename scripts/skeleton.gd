extends CharacterBody3D

var health: int = 35
var move_speed: float = 3.0
var attack_cooldown: float = 0.0

func _ready() -> void:
    var collision := CollisionShape3D.new()
    collision.name = "CollisionShape3D"
    var shape := CapsuleShape3D.new()
    shape.radius = 0.3
    shape.height = 1.0
    collision.shape = shape
    add_child(collision)

func _physics_process(delta: float) -> void:
    attack_cooldown = maxf(0.0, attack_cooldown - delta)
    var target: Node3D = null
    var best_distance := 9999.0
    for child in get_parent().get_children():
        if child is Node3D and child != self and child.has_method("take_damage") and child is CharacterBody3D:
            var distance := global_position.distance_to(child.global_position)
            if distance < best_distance and distance < 16.0:
                target = child
                best_distance = distance
    if target == null:
        return
    var direction := target.global_position - global_position
    direction.y = 0.0
    if best_distance > 1.4:
        velocity.x = direction.normalized().x * move_speed
        velocity.z = direction.normalized().z * move_speed
        move_and_slide()
    elif attack_cooldown <= 0.0:
        target.call("take_damage", 12)
        attack_cooldown = 1.0
        if int(target.get("health")) <= 0 and get_parent().has_method("on_enemy_defeated"):
            get_parent().on_enemy_defeated(str(target.get("enemy_type")))
