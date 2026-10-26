extends Control

const CLASSES = [
    {"id":"warrior","name":"ВОИН","role":"Сила · Выносливость · Ближний бой","desc":"Тяжёлая броня и сокрушительные удары.","color":Color(1.0,0.28,0.055),"glyph":"⚔"},
    {"id":"mage","name":"МАГ","role":"Мана · Контроль · Дальний бой","desc":"Древние заклинания и магия стихий.","color":Color(0.15,0.55,1.0),"glyph":"✦"},
    {"id":"ranger","name":"ЭЛЬФ-ЛУЧНИК","role":"Ловкость · Точность · Дальний бой","desc":"Быстрые атаки и смертоносные выстрелы.","color":Color(0.35,0.78,0.38),"glyph":"➶"},
    {"id":"necromancer","name":"НЕКРОМАНТ","role":"Призыв · Сила · Контроль","desc":"Поднимает павших и повелевает мёртвыми.","color":Color(0.15,0.95,0.72),"glyph":"☠"}
]

var selected_id := "warrior"
var cards: Array[PanelContainer] = []
var start_button: Button

class HeroModelPortrait extends SubViewportContainer:
    var model_class_id := "warrior"

    func setup(id: String, tint: Color) -> bool:
        model_class_id = id
        custom_minimum_size = Vector2(0, 210)
        size_flags_horizontal = Control.SIZE_EXPAND_FILL
        stretch = true
        var files: Array[String] = []
        _collect_model_files("res://assets/free/rpg_characters", files)
        files.sort()
        if files.is_empty():
            return false
        var terms: Array[String] = []
        match id:
            "warrior": terms = ["warrior", "knight", "barbarian", "fighter"]
            "mage": terms = ["mage", "wizard", "sorcerer"]
            "ranger": terms = ["ranger", "archer", "elf", "hunter"]
            "necromancer": terms = ["necromancer", "warlock", "dark", "skeleton"]
        var chosen := ""
        for term in terms:
            for path in files:
                if path.get_file().to_lower().contains(term):
                    chosen = path
                    break
            if not chosen.is_empty():
                break
        if chosen.is_empty():
            var index := 0
            match id:
                "mage": index = 1
                "ranger": index = 2
                "necromancer": index = 3
            chosen = files[mini(index, files.size() - 1)]
        var resource = load(chosen)
        if not resource is PackedScene:
            return false
        var viewport := SubViewport.new()
        viewport.size = Vector2i(300, 420)
        viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
        viewport.own_world_3d = true
        viewport.transparent_bg = false
        add_child(viewport)
        var world := Node3D.new()
        viewport.add_child(world)
        var environment_node := WorldEnvironment.new()
        var environment := Environment.new()
        environment.background_mode = Environment.BG_COLOR
        environment.background_color = Color(0.025, 0.022, 0.030)
        environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
        environment.ambient_light_color = Color(0.50, 0.48, 0.46)
        environment.ambient_light_energy = 0.75
        environment.glow_enabled = true
        environment_node.environment = environment
        world.add_child(environment_node)
        var key_light := DirectionalLight3D.new()
        key_light.rotation_degrees = Vector3(-28, -25, 0)
        key_light.light_color = tint.lightened(0.38)
        key_light.light_energy = 1.6
        world.add_child(key_light)
        var rim_light := OmniLight3D.new()
        rim_light.position = Vector3(-1.4, 2.1, -1.5)
        rim_light.light_color = tint
        rim_light.light_energy = 2.2
        rim_light.omni_range = 6.0
        world.add_child(rim_light)
        var camera := Camera3D.new()
        camera.position = Vector3(0, 1.1, 4.0)
        camera.fov = 38.0
        camera.current = true
        camera.look_at(Vector3(0, 1.0, 0), Vector3.UP)
        world.add_child(camera)
        var model = resource.instantiate()
        if not model is Node3D:
            model.queue_free()
            return false
        model.rotation.y = PI
        world.add_child(model)
        call_deferred("_fit_model", model)
        return true

    func _collect_model_files(folder: String, output: Array[String]) -> void:
        var dir := DirAccess.open(folder)
        if dir == null:
            return
        dir.list_dir_begin()
        var entry := dir.get_next()
        while not entry.is_empty():
            if entry.begins_with("."):
                entry = dir.get_next()
                continue
            var path := folder.path_join(entry)
            if dir.current_is_dir():
                _collect_model_files(path, output)
            elif entry.get_extension().to_lower() in ["fbx", "glb", "gltf", "obj"]:
                output.append(path)
            entry = dir.get_next()
        dir.list_dir_end()

    func _fit_model(model: Node3D) -> void:
        if not is_instance_valid(model):
            return
        var meshes: Array[MeshInstance3D] = []
        _collect_meshes(model, meshes)
        var bounds := AABB()
        var first := true
        for mesh in meshes:
            if mesh.mesh == null:
                continue
            var current: AABB = mesh.global_transform * mesh.get_aabb()
            if first:
                bounds = current
                first = false
            else:
                bounds = bounds.merge(current)
        if first or bounds.size.y <= 0.01:
            return
        var factor := 2.1 / bounds.size.y
        model.scale *= factor
        model.position.y -= bounds.position.y * factor

    func _collect_meshes(node: Node, output: Array[MeshInstance3D]) -> void:
        if node is MeshInstance3D:
            output.append(node)
        for child in node.get_children():
            _collect_meshes(child, output)


class PortraitArt extends Control:
    var class_id := "warrior"
    var accent := Color(1.0, 0.28, 0.055)

    func setup(id: String, tint: Color) -> void:
        class_id = id
        accent = tint
        custom_minimum_size = Vector2(0, 210)
        size_flags_horizontal = Control.SIZE_EXPAND_FILL
        queue_redraw()

    func _draw() -> void:
        var w := size.x
        var h := size.y
        if w < 1.0 or h < 1.0:
            return
        draw_rect(Rect2(Vector2.ZERO, size), Color(0.025,0.025,0.035))
        # Glowing ruined sky and distant silhouettes.
        for i in range(7):
            var x := w * (0.08 + float(i) * 0.15)
            var peak := h * (0.47 + float((i * 7) % 4) * 0.045)
            draw_colored_polygon(PackedVector2Array([Vector2(x-22,h),Vector2(x,peak),Vector2(x+27,h)]), Color(0.10,0.095,0.105))
        draw_circle(Vector2(w*0.72,h*0.23), h*0.115, accent.darkened(0.65))
        draw_circle(Vector2(w*0.72,h*0.23), h*0.075, accent.darkened(0.35))
        # Backlit full-body hero silhouette, layered shapes instead of a placeholder glyph.
        var cx := w * 0.50
        var head := Vector2(cx, h*0.28)
        var dark := Color(0.035,0.032,0.038)
        var armor := Color(0.22,0.19,0.18)
        draw_colored_polygon(PackedVector2Array([
            Vector2(cx-w*0.14,h*0.49),Vector2(cx-w*0.21,h*0.78),
            Vector2(cx-w*0.14,h*0.94),Vector2(cx+w*0.15,h*0.94),
            Vector2(cx+w*0.21,h*0.78),Vector2(cx+w*0.13,h*0.49)
        ]), dark)
        draw_colored_polygon(PackedVector2Array([
            Vector2(cx-w*0.13,h*0.47),Vector2(cx-w*0.19,h*0.68),
            Vector2(cx-w*0.12,h*0.73),Vector2(cx+w*0.12,h*0.73),
            Vector2(cx+w*0.19,h*0.68),Vector2(cx+w*0.13,h*0.47)
        ]), armor)
        draw_circle(head, h*0.09, Color(0.39,0.29,0.23))
        draw_rect(Rect2(cx-h*0.09, h*0.22, h*0.18, h*0.06), dark)
        draw_colored_polygon(PackedVector2Array([
            Vector2(cx-h*0.11,h*0.24),Vector2(cx-h*0.13,h*0.17),
            Vector2(cx,h*0.12),Vector2(cx+h*0.13,h*0.17),Vector2(cx+h*0.11,h*0.24)
        ]), armor)
        # Shoulder plates and engraved chest accents.
        draw_colored_polygon(PackedVector2Array([
            Vector2(cx-w*0.19,h*0.49),Vector2(cx-w*0.28,h*0.43),
            Vector2(cx-w*0.32,h*0.51),Vector2(cx-w*0.22,h*0.59)
        ]), armor.lightened(0.12))
        draw_colored_polygon(PackedVector2Array([
            Vector2(cx+w*0.19,h*0.49),Vector2(cx+w*0.28,h*0.43),
            Vector2(cx+w*0.32,h*0.51),Vector2(cx+w*0.22,h*0.59)
        ]), armor.lightened(0.12))
        draw_line(Vector2(cx,h*0.49),Vector2(cx,h*0.69),accent,2.0)
        draw_line(Vector2(cx-w*0.09,h*0.56),Vector2(cx+w*0.09,h*0.56),accent.darkened(0.15),2.0)
        # Class-specific signature weapon / magic.
        match class_id:
            "warrior":
                draw_line(Vector2(cx+w*0.18,h*0.83),Vector2(cx+w*0.34,h*0.34),Color(0.42,0.30,0.19),5.0)
                draw_colored_polygon(PackedVector2Array([Vector2(cx+w*0.25,h*0.48),Vector2(cx+w*0.39,h*0.37),Vector2(cx+w*0.45,h*0.47),Vector2(cx+w*0.33,h*0.56)]),Color(0.67,0.69,0.70))
                draw_line(Vector2(cx+w*0.28,h*0.49),Vector2(cx+w*0.42,h*0.43),Color(0.92,0.84,0.65),2.0)
            "mage":
                draw_line(Vector2(cx+w*0.23,h*0.90),Vector2(cx+w*0.28,h*0.29),Color(0.36,0.22,0.12),4.0)
                draw_circle(Vector2(cx+w*0.28,h*0.26),h*0.075,accent.darkened(0.4))
                draw_circle(Vector2(cx+w*0.28,h*0.26),h*0.045,accent)
                for i in range(5):
                    var p := Vector2(cx+w*0.28+cos(float(i)*TAU/5.0)*h*0.10,h*0.26+sin(float(i)*TAU/5.0)*h*0.10)
                    draw_circle(p,2.0,Color(0.6,0.82,1.0))
            "ranger":
                draw_arc(Vector2(cx+w*0.24,h*0.57),h*0.23,-1.15,1.15,24,Color(0.52,0.34,0.16),4.0)
                draw_line(Vector2(cx+w*0.14,h*0.37),Vector2(cx+w*0.34,h*0.77),Color(0.72,0.70,0.56),1.5)
                draw_colored_polygon(PackedVector2Array([Vector2(cx-w*0.13,h*0.23),Vector2(cx-w*0.25,h*0.31),Vector2(cx-w*0.14,h*0.36)]),Color(0.16,0.28,0.18))
            "necromancer":
                draw_circle(Vector2(cx+w*0.25,h*0.46),h*0.13,accent.darkened(0.65))
                draw_circle(Vector2(cx+w*0.25,h*0.46),h*0.065,accent)
                draw_line(Vector2(cx+w*0.23,h*0.86),Vector2(cx+w*0.27,h*0.29),Color(0.66,0.65,0.57),4.0)
                for i in range(3):
                    draw_circle(Vector2(cx+w*(0.13+0.12*i),h*(0.68+0.045*(i%2))),h*0.025,Color(0.72,0.72,0.64))
        # Vignette and lower shadow strip.
        draw_rect(Rect2(0,0,w,h),Color(0.015,0.012,0.018,0.16),false,1.0)
        draw_rect(Rect2(0,h-22,w,22),Color(0.012,0.01,0.015,0.82))

func _ready() -> void:
    set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    _build()

func _build() -> void:
    var bg := ColorRect.new()
    bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    bg.color = Color(0.012, 0.010, 0.016)
    add_child(bg)

    # Landscape composition matching the reference: title rail, four hero cards, lore/start rail.
    var root := HBoxContainer.new()
    root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    root.offset_left = 12
    root.offset_top = 12
    root.offset_right = -12
    root.offset_bottom = -12
    root.add_theme_constant_override("separation", 10)
    add_child(root)

    var left := PanelContainer.new()
    left.custom_minimum_size = Vector2(155, 0)
    var left_style := StyleBoxFlat.new()
    left_style.bg_color = Color(0.025, 0.018, 0.022)
    left_style.border_color = Color(0.42, 0.19, 0.075)
    left_style.set_border_width_all(1)
    left.add_theme_stylebox_override("panel", left_style)
    root.add_child(left)

    var left_col := VBoxContainer.new()
    left_col.alignment = BoxContainer.ALIGNMENT_CENTER
    left_col.add_theme_constant_override("separation", 12)
    left.add_child(left_col)
    left_col.add_child(_label("✥", 28, Color(1.0, 0.43, 0.10)))
    left_col.add_child(_label("ХРОНИКИ", 23, Color(0.88, 0.77, 0.59)))
    var title := _label("ПЕПЛА", 34, Color(1.0, 0.29, 0.06))
    left_col.add_child(title)
    left_col.add_child(_ornament())
    left_col.add_child(_label("КОРОЛЕВСТВА\nПАЛИ.", 13, Color(0.78, 0.67, 0.54)))
    left_col.add_child(_label("ПРАХ ПОМНИТ\nИМЕНА.", 12, Color(0.57, 0.49, 0.42)))
    var left_spacer := Control.new()
    left_spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
    left_col.add_child(left_spacer)
    left_col.add_child(_label("DARK FANTASY\nACTION RPG", 10, Color(0.82, 0.34, 0.10)))

    var center := VBoxContainer.new()
    center.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    center.add_theme_constant_override("separation", 8)
    root.add_child(center)
    center.add_child(_label("ВЫБЕРИ СВОЙ ПУТЬ", 19, Color(0.93, 0.78, 0.54)))
    var subtitle := _label("ЧЕТЫРЕ СУДЬБЫ · ОДИН МИР, ПОГРУЖЁННЫЙ В ПЕПЕЛ", 9, Color(0.61, 0.52, 0.43))
    center.add_child(subtitle)

    var grid := GridContainer.new()
    grid.columns = 4
    grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    grid.size_flags_vertical = Control.SIZE_EXPAND_FILL
    grid.add_theme_constant_override("h_separation", 7)
    center.add_child(grid)

    for data in CLASSES:
        var card := PanelContainer.new()
        card.set_meta("class_id", data.id)
        card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
        card.size_flags_vertical = Control.SIZE_EXPAND_FILL
        card.custom_minimum_size = Vector2(0, 0)
        var style := StyleBoxFlat.new()
        style.bg_color = Color(0.025, 0.024, 0.030)
        style.border_color = data.color.darkened(0.62)
        style.set_border_width_all(1)
        style.content_margin_left = 5
        style.content_margin_right = 5
        style.content_margin_top = 6
        style.content_margin_bottom = 6
        card.add_theme_stylebox_override("panel", style)

        var inner := VBoxContainer.new()
        inner.size_flags_vertical = Control.SIZE_EXPAND_FILL
        inner.add_theme_constant_override("separation", 5)
        card.add_child(inner)

        var model_portrait := HeroModelPortrait.new()
        var portrait: Control
        if model_portrait.setup(data.id, data.color):
            portrait = model_portrait
        else:
            var drawn_portrait := PortraitArt.new()
            drawn_portrait.setup(data.id, data.color)
            portrait = drawn_portrait
        portrait.size_flags_vertical = Control.SIZE_EXPAND_FILL
        portrait.custom_minimum_size = Vector2(0, 180)
        inner.add_child(portrait)

        inner.add_child(_ornament())
        inner.add_child(_label(data.name, 15, Color(0.93, 0.81, 0.63)))
        var role := _label(data.role, 9, data.color.lightened(0.18))
        role.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
        inner.add_child(role)
        var desc := _label(data.desc, 9, Color(0.67, 0.61, 0.54))
        desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
        desc.size_flags_vertical = Control.SIZE_EXPAND_FILL
        inner.add_child(desc)

        var choose := Button.new()
        choose.text = "ВЫБРАТЬ"
        choose.custom_minimum_size = Vector2(0, 34)
        choose.pressed.connect(_select.bind(data.id))
        inner.add_child(choose)
        grid.add_child(card)
        cards.append(card)

    var right := PanelContainer.new()
    right.custom_minimum_size = Vector2(158, 0)
    var right_style := StyleBoxFlat.new()
    right_style.bg_color = Color(0.025, 0.018, 0.022)
    right_style.border_color = Color(0.42, 0.19, 0.075)
    right_style.set_border_width_all(1)
    right_style.content_margin_left = 8
    right_style.content_margin_right = 8
    right_style.content_margin_top = 10
    right_style.content_margin_bottom = 10
    right.add_theme_stylebox_override("panel", right_style)
    root.add_child(right)

    var right_col := VBoxContainer.new()
    right_col.alignment = BoxContainer.ALIGNMENT_CENTER
    right_col.add_theme_constant_override("separation", 10)
    right.add_child(right_col)
    right_col.add_child(_label("ВЫБЕРИ\nСВОЙ ПУТЬ", 17, Color(0.94, 0.78, 0.55)))
    right_col.add_child(_ornament())
    var lore := _label("Мир Хроник Пепла хранит множество тайн, опасностей и наград.", 10, Color(0.73, 0.66, 0.58))
    lore.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    right_col.add_child(lore)
    var right_spacer := Control.new()
    right_spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
    right_col.add_child(right_spacer)
    start_button = Button.new()
    start_button.custom_minimum_size = Vector2(0, 54)
    start_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    start_button.add_theme_font_size_override("font_size", 14)
    start_button.pressed.connect(_start_game)
    right_col.add_child(start_button)
    right_col.add_child(_label("ГОРЫ · ЛЕСА\nРЕКИ · ПОДЗЕМЕЛЬЯ", 9, Color(0.77, 0.46, 0.25)))
    _refresh()

func _ornament() -> Control:
    var line := ColorRect.new()
    line.custom_minimum_size = Vector2(0, 2)
    line.color = Color(0.48,0.22,0.09)
    return line

func _label(value: String, font_size: int, color: Color) -> Label:
    var l := Label.new()
    l.text = value
    l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    l.add_theme_font_size_override("font_size", font_size)
    l.add_theme_color_override("font_color", color)
    return l

func _select(class_id: String) -> void:
    selected_id = class_id
    _refresh()

func _refresh() -> void:
    for card in cards:
        var id := str(card.get_meta("class_id"))
        var data: Dictionary = CLASSES.filter(func(item): return item.id == id)[0]
        var style := card.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
        style.border_color = data.color if id == selected_id else data.color.darkened(0.58)
        style.set_border_width_all(3 if id == selected_id else 1)
        style.bg_color = Color(0.105,0.055,0.035) if id == selected_id else Color(0.035,0.032,0.038)
        card.add_theme_stylebox_override("panel", style)
    var chosen: Dictionary = CLASSES.filter(func(item): return item.id == selected_id)[0]
    start_button.text = "НАЧАТЬ ИГРУ  ·  " + str(chosen.name)

func _start_game() -> void:
    get_tree().root.set_meta("selected_class", selected_id)
    get_tree().change_scene_to_file("res://scenes/world.tscn")
