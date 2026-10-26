extends Node3D
## Drop this node under the main 3D world root to add a dark-fantasy atmosphere.
## Godot 4.3 compatible; uses procedural settings and does not require external assets.

@export var fog_density: float = 0.018
@export var fog_color: Color = Color(0.16, 0.19, 0.22, 1.0)
@export var sunlight_energy: float = 0.55
@export var sunlight_color: Color = Color(0.78, 0.72, 0.61, 1.0)
@export var ambient_color: Color = Color(0.29, 0.34, 0.42, 1.0)
@export var enable_shadows: bool = true

func _ready() -> void:
	_apply_world_environment()
	_create_sunlight()


func _apply_world_environment() -> void:
	var world_environment := WorldEnvironment.new()
	world_environment.name = "AshWorldEnvironment"

	var environment := Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color(0.055, 0.075, 0.095, 1.0)
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = ambient_color
	environment.ambient_light_energy = 0.65
	environment.reflected_light_source = Environment.REFLECTION_SOURCE_BG
	environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	environment.tonemap_exposure = 0.9
	environment.tonemap_white = 1.15

	environment.fog_enabled = true
	environment.fog_light_color = fog_color
	environment.fog_light_energy = 0.8
	environment.fog_density = maxf(fog_density, 0.0)
	environment.fog_sky_affect = 0.35

	environment.adjustment_enabled = true
	environment.adjustment_brightness = 0.96
	environment.adjustment_contrast = 1.12
	environment.adjustment_saturation = 0.78

	world_environment.environment = environment
	add_child(world_environment)


func _create_sunlight() -> void:
	var sunlight := DirectionalLight3D.new()
	sunlight.name = "AshSunlight"
	sunlight.rotation_degrees = Vector3(-38.0, -28.0, 0.0)
	sunlight.light_color = sunlight_color
	sunlight.light_energy = sunlight_energy
	sunlight.shadow_enabled = enable_shadows
	sunlight.directional_shadow_max_distance = 55.0
	sunlight.sky_mode = DirectionalLight3D.SKY_MODE_LIGHT_AND_SKY
	add_child(sunlight)
