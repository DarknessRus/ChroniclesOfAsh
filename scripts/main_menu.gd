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
    bg.color = Color(0.012,0.012,0.018)
    add_child(bg)

    var scroll := ScrollContainer.new()
    scroll.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    scroll.offset_left = 14
    scroll.offset_top = 8
    scroll.offset_right = -14
    scroll.offset_bottom = -8
    add_child(scroll)

    var col := VBoxContainer.new()
    col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    col.add_theme_constant_override("separation", 8)
    scroll.add_child(col)

    col.add_child(_label("DARK FANTASY  ·  ACTION RPG", 11, Color(0.92,0.42,0.12)))
    col.add_child(_label("ХРОНИКИ", 22, Color(0.87,0.78,0.61)))
    var title := _label("ПЕПЛА", 42, Color(1.0,0.30,0.075))
    col.add_child(title)
    col.add_child(_label("КОРОЛЕВСТВА ПАЛИ. ПРАХ ПОМНИТ ИМЕНА.", 10, Color(0.63,0.56,0.49)))
    col.add_child(_ornament())

    var heading := _label("—  ВЫБЕРИ СВОЙ ПУТЬ  —", 17, Color(0.91,0.75,0.51))
    col.add_child(heading)

    var grid := GridContainer.new()
    grid.columns = 2
    grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    grid.add_theme_constant_override("h_separation", 9)
    grid.add_theme_constant_override("v_separation", 9)
    col.add_child(grid)

    for data in CLASSES:
        var card := PanelContainer.new()
        card.set_meta("class_id", data.id)
        card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
        card.custom_minimum_size = Vector2(0, 300)
        var style := StyleBoxFlat.new()
        style.bg_color = Color(0.035,0.032,0.038)
        style.border_color = data.color.darkened(0.58)
        style.set_border_width_all(2)
        style.set_corner_radius_all(1)
        style.content_margin_left = 7
        style.content_margin_right = 7
        style.content_margin_top = 7
        style.content_margin_bottom = 8
        card.add_theme_stylebox_override("panel", style)

        var inner := VBoxContainer.new()
        inner.add_theme_constant_override("separation", 5)
        card.add_child(inner)

        var portrait := PortraitArt.new()
        portrait.setup(data.id, data.color)
        inner.add_child(portrait)

        var name := _label(data.name, 16, Color(0.92,0.80,0.61))
        inner.add_child(name)
        var role := _label(data.role, 10, data.color.lightened(0.18))
        role.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
        inner.add_child(role)
        var desc := _label(data.desc, 10, Color(0.67,0.62,0.57))
        desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
        desc.custom_minimum_size = Vector2(0, 30)
        inner.add_child(desc)

        var choose := Button.new()
        choose.text = "ВЫБРАТЬ ГЕРОЯ"
        choose.custom_minimum_size = Vector2(0, 34)
        choose.pressed.connect(_select.bind(data.id))
        inner.add_child(choose)
        grid.add_child(card)
        cards.append(card)

    col.add_child(_ornament())
    var lore := _label("МЁРТВЫЕ ЛЕСА  ·  ЗАБЫТЫЕ КАТАКОМБЫ  ·  ДРЕВНИЕ РЕЛИКВИИ", 10, Color(0.63,0.54,0.45))
    lore.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    col.add_child(lore)

    start_button = Button.new()
    start_button.custom_minimum_size = Vector2(0, 52)
    start_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    start_button.add_theme_font_size_override("font_size", 18)
    start_button.pressed.connect(_start_game)
    col.add_child(start_button)
    col.add_child(_label("МИР СОЗДАЁТСЯ ЗАНОВО. КАЖДАЯ ЭКСПЕДИЦИЯ УНИКАЛЬНА.", 9, Color(0.45,0.39,0.34)))
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
