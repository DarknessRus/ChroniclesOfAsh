extends Node3D

@onready var status_label: Label = $HUD/Status

func _ready() -> void:
    randomize()
    var ground_material := StandardMaterial3D.new()
    ground_material.albedo_color = Color(0.25, 0.43, 0.25)
    ground_material.roughness = 1.0
    $Ground/GroundMesh.material_override = ground_material
    var ground_shape := BoxShape3D.new()
    ground_shape.size = Vector3(34, 0.4, 34)
    $Ground/GroundCollision.shape = ground_shape
    _add_decorations()

func update_status(level: int, health: int, experience: int) -> void:
    status_label.text = "Уровень %d   Здоровье: %d   Опыт: %d" % [level, health, experience]

func _add_decorations() -> void:
    for i in range(28):
        var tree := MeshInstance3D.new()
        var trunk := CylinderMesh.new()
        trunk.top_radius = 0.18
        trunk.bottom_radius = 0.28
        trunk.height = 1.5
        tree.mesh = trunk
        tree.position = Vector3(randf_range(-15, 15), 0.55, randf_range(-15, 15))
        if tree.position.length() < 4.0:
            tree.position.x += 6.0
        var material := StandardMaterial3D.new()
        material.albedo_color = Color(0.34, 0.22, 0.12)
        tree.material_override = material
        add_child(tree)
        var crown := MeshInstance3D.new()
        var crown_mesh := SphereMesh.new()
        crown_mesh.radius = 0.85
        crown_mesh.height = 1.7
        crown.mesh = crown_mesh
        crown.position = tree.position + Vector3(0, 1.15, 0)
        var crown_mat := StandardMaterial3D.new()
        crown_mat.albedo_color = Color(0.12, randf_range(0.28, 0.42), 0.16)
        crown.material_override = crown_mat
        add_child(crown)
