extends Control

const CLASSES := [
    {"id":"warrior","name":"ВОИН","role":"Хранитель пепла","desc":"Тяжёлая броня • боевой топор • ближний бой","color":Color(0.92,0.30,0.10),"sigil":"⚔"},
    {"id":"mage","name":"МАГ","role":"Повелитель стихий","desc":"Рунический посох • магия • дальние атаки","color":Color(0.20,0.58,1.0),"sigil":"✦"},
    {"id":"ranger","name":"ЭЛЬФ-ЛУЧНИК","role":"Страж древнего леса","desc":"Лук • точность • быстрые выстрелы","color":Color(0.36,0.78,0.34),"sigil":"➶"},
    {"id":"necromancer","name":"НЕКРОМАНТ","role":"Повелитель мёртвых","desc":"Тёмная магия • призыв скелетов","color":Color(0.54,0.30,0.86),"sigil":"☠"}
]
var selected_index: int = 0
var cards: Array[PanelContainer] = []
var title_label: Label
var start_button: Button
var subtitle_label: Label

func _ready() -> void:
    set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    _build_ui()
    _select_class(0)
    queue_redraw()

func _draw() -> void:
    var size := get_rect().size
    draw_rect(Rect2(Vector2.ZERO, size), Color(0.018,0.022,0.032))
    for i in range(12):
        var x := float(i) / 12.0 * size.x
        var height := size.y * (0.26 + 0.16 * sin(float(i) * 1.4))
        draw_colored_polygon(PackedVector2Array([Vector2(x,0),Vector2(x+size.x/12.0+2,0),Vector2(x+size.x/12.0+2,height),Vector2(x,height)]), Color(0.08,0.09,0.12,0.34))
    draw_rect(Rect2(0,size.y*0.70,size.x,size.y*0.30),Color(0.025,0.018,0.018,0.75))
    draw_line(Vector2(0,size.y*0.70),Vector2(size.x,size.y*0.70),Color(0.72,0.27,0.09,0.7),2.0)
    for i in range(42):
        var x := fmod(float(i*137), maxf(1.0,size.x))
        var y := fmod(float(i*79), maxf(1.0,size.y))
        draw_circle(Vector2(x,y),1.0 + float(i%3)*0.6,Color(1.0,0.34,0.08,0.22))

func _build_ui() -> void:
    var margin := MarginContainer.new()
    margin.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    margin.add_theme_constant_override("margin_left",28)
    margin.add_theme_constant_override("margin_right",28)
    margin.add_theme_constant_override("margin_top",18)
    margin.add_theme_constant_override("margin_bottom",18)
    add_child(margin)
    var root := VBoxContainer.new()
    root.add_theme_constant_override("separation",8)
    margin.add_child(root)
    var top := HBoxContainer.new()
    top.alignment = BoxContainer.ALIGNMENT_CENTER
    root.add_child(top)
    var brand := VBoxContainer.new()
    brand.alignment = BoxContainer.ALIGNMENT_CENTER
    top.add_child(brand)
    title_label = Label.new()
    title_label.text = "ХРОНИКИ ПЕПЛА"
    title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    title_label.add_theme_font_size_override("font_size",36)
    title_label.add_theme_color_override("font_color",Color(0.95,0.78,0.52))
    brand.add_child(title_label)
    subtitle_label = Label.new()
    subtitle_label.text = "МИР НА КРАЮ ГИБЕЛИ • ВЫБЕРИ СВОЮ ЛЕГЕНДУ"
    subtitle_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    subtitle_label.add_theme_font_size_override("font_size",12)
    subtitle_label.add_theme_color_override("font_color",Color(0.69,0.68,0.66))
    brand.add_child(subtitle_label)
    var spacer := Control.new()
    spacer.custom_minimum_size.y = 10
    root.add_child(spacer)
    var row := HBoxContainer.new()
    row.size_flags_vertical = Control.SIZE_EXPAND_FILL
    row.add_theme_constant_override("separation",12)
    root.add_child(row)
    for i in range(CLASSES.size()):
        var data: Dictionary = CLASSES[i]
        var card := PanelContainer.new()
        card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
        card.size_flags_vertical = Control.SIZE_EXPAND_FILL
        card.custom_minimum_size = Vector2(140,210)
        var style := StyleBoxFlat.new()
        style.bg_color = Color(0.045,0.052,0.065,0.94)
        style.border_width_left = 1
        style.border_width_right = 1
        style.border_width_top = 1
        style.border_width_bottom = 1
        style.border_color = Color(0.28,0.27,0.25)
        style.corner_radius_top_left = 8
        style.corner_radius_top_right = 8
        style.corner_radius_bottom_left = 8
        style.corner_radius_bottom_right = 8
        card.add_theme_stylebox_override("panel",style)
        row.add_child(card)
        cards.append(card)
        var content := VBoxContainer.new()
        content.alignment = BoxContainer.ALIGNMENT_CENTER
        content.add_theme_constant_override("separation",7)
        card.add_child(content)
        var portrait := CharacterPortrait.new()
        portrait.custom_minimum_size = Vector2(130,150)
        portrait.size_flags_horizontal = Control.SIZE_EXPAND_FILL
        portrait.character_data = data
        content.add_child(portrait)
        var name := Label.new()
        name.text = data.name
        name.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
        name.add_theme_font_size_override("font_size",19)
        name.add_theme_color_override("font_color",data.color)
        content.add_child(name)
        var role := Label.new()
        role.text = data.role
        role.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
        role.add_theme_font_size_override("font_size",11)
        role.add_theme_color_override("font_color",Color(0.83,0.79,0.70))
        content.add_child(role)
        var desc := Label.new()
        desc.text = data.desc
        desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
        desc.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
        desc.add_theme_font_size_override("font_size",10)
        desc.add_theme_color_override("font_color",Color(0.65,0.67,0.69))
        content.add_child(desc)
        var pick := Button.new()
        pick.text = "ВЫБРАТЬ"
        pick.custom_minimum_size.y = 34
        pick.pressed.connect(_select_class.bind(i))
        content.add_child(pick)
    var bottom := HBoxContainer.new()
    bottom.alignment = BoxContainer.ALIGNMENT_CENTER
    bottom.add_theme_constant_override("separation",20)
    root.add_child(bottom)
    var lore := Label.new()
    lore.text = "Пепельные горы • Забытые храмы • Проклятые подземелья"
    lore.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    lore.size_flags_horizontal = Control.SIZE_EXPAND_FILL
    lore.add_theme_color_override("font_color",Color(0.76,0.69,0.57))
    lore.add_theme_font_size_override("font_size",13)
    bottom.add_child(lore)
    start_button = Button.new()
    start_button.text = "НАЧАТЬ ПУТЕШЕСТВИЕ"
    start_button.custom_minimum_size = Vector2(240,52)
    start_button.add_theme_font_size_override("font_size",17)
    start_button.pressed.connect(_start_game)
    bottom.add_child(start_button)

func _select_class(index: int) -> void:
    selected_index = index
    for i in range(cards.size()):
        var style := cards[i].get_theme_stylebox("panel").duplicate() as StyleBoxFlat
        style.border_color = CLASSES[i].color if i == index else Color(0.28,0.27,0.25)
        style.border_width_left = 3 if i == index else 1
        style.border_width_right = 3 if i == index else 1
        style.border_width_top = 3 if i == index else 1
        style.border_width_bottom = 3 if i == index else 1
        cards[i].add_theme_stylebox_override("panel",style)
    start_button.text = "НАЧАТЬ: " + CLASSES[index].name

func _start_game() -> void:
    get_tree().set_meta("selected_class", CLASSES[selected_index].id)
    get_tree().change_scene_to_file("res://scenes/world.tscn")

class CharacterPortrait extends Control:
    var character_data: Dictionary = {}
    func _draw() -> void:
        var w := size.x
        var h := size.y
        var accent: Color = character_data.get("color",Color(0.8,0.3,0.1))
        draw_rect(Rect2(Vector2.ZERO,size),Color(0.025,0.035,0.05))
        for i in range(7):
            var x := float(i)*w/7.0
            draw_line(Vector2(x,0),Vector2(w*0.5,h),Color(accent.r,accent.g,accent.b,0.12),1.0)
        draw_circle(Vector2(w*0.5,h*0.28),w*0.17,Color(0.16,0.18,0.21))
        draw_circle(Vector2(w*0.5,h*0.28),w*0.13,Color(0.57,0.43,0.32))
        draw_rect(Rect2(w*0.5-w*0.14,h*0.34,w*0.28,h*0.36),accent)
        draw_colored_polygon(PackedVector2Array([Vector2(w*0.5-w*0.23,h*0.43),Vector2(w*0.5+w*0.23,h*0.43),Vector2(w*0.5+w*0.32,h*0.83),Vector2(w*0.5-w*0.32,h*0.83)]),Color(accent.r*0.55,accent.g*0.55,accent.b*0.55))
        draw_circle(Vector2(w*0.5-w*0.045,h*0.28),2.0,Color(1.0,0.78,0.42))
        draw_circle(Vector2(w*0.5+w*0.045,h*0.28),2.0,Color(1.0,0.78,0.42))
        match str(character_data.get("id","warrior")):
            "warrior":
                draw_line(Vector2(w*0.68,h*0.43),Vector2(w*0.86,h*0.16),Color(0.78,0.78,0.73),5.0)
                draw_line(Vector2(w*0.79,h*0.13),Vector2(w*0.94,h*0.19),Color(0.78,0.78,0.73),8.0)
            "mage":
                draw_colored_polygon(PackedVector2Array([Vector2(w*0.5-w*0.18,h*0.16),Vector2(w*0.5,h*0.01),Vector2(w*0.5+w*0.18,h*0.16)]),Color(0.07,0.12,0.24))
                draw_circle(Vector2(w*0.79,h*0.12),7.0,Color(0.2,0.7,1.0))
            "ranger":
                draw_arc(Vector2(w*0.78,h*0.42),w*0.18,-1.2,1.2,24,Color(0.64,0.43,0.19),4.0)
                draw_line(Vector2(w*0.78,h*0.25),Vector2(w*0.78,h*0.59),Color(0.82,0.75,0.56),1.5)
                draw_colored_polygon(PackedVector2Array([Vector2(w*0.5-w*0.12,h*0.1),Vector2(w*0.5,h*0.01),Vector2(w*0.5+w*0.12,h*0.1)]),Color(0.16,0.3,0.19))
            "necromancer":
                draw_colored_polygon(PackedVector2Array([Vector2(w*0.5-w*0.20,h*0.16),Vector2(w*0.5,h*0.01),Vector2(w*0.5+w*0.20,h*0.16)]),Color(0.10,0.07,0.15))
                draw_circle(Vector2(w*0.5,h*0.28),w*0.14,Color(0.04,0.05,0.07))
                draw_circle(Vector2(w*0.5-w*0.04,h*0.28),2.5,Color(0.1,1.0,0.55))
                draw_circle(Vector2(w*0.5+w*0.04,h*0.28),2.5,Color(0.1,1.0,0.55))
        draw_line(Vector2(0,h-1),Vector2(w,h-1),accent,2.0)
