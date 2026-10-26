extends Control
const CLASSES = [
 {"id":"warrior","name":"ВОИН С ТОПОРОМ","role":"Тяжёлая броня · ближний бой","desc":"Разрывает ряды врагов сокрушительными ударами.","color":Color(0.72,0.24,0.12),"glyph":"⚔"},
 {"id":"mage","name":"МАГ С ПОСОХОМ","role":"Магия стихий · дальний бой","desc":"Обрушивает на чудовищ потоки древней энергии.","color":Color(0.22,0.53,0.95),"glyph":"✦"},
 {"id":"ranger","name":"ЭЛЬФ-ЛУЧНИК","role":"Точность · скорость","desc":"Выслеживает добычу и поражает её из тени леса.","color":Color(0.24,0.65,0.34),"glyph":"➶"},
 {"id":"necromancer","name":"НЕКРОМАНТ","role":"Тёмная магия · призыв","desc":"Поднимает скелетов из павших врагов.","color":Color(0.55,0.30,0.76),"glyph":"☠"}
]
var selected_id = "warrior"
var cards = []
var start_button: Button
func _ready():
 set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
 _build()
func _build():
 var bg=ColorRect.new()
 bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
 bg.color=Color(0.025,0.022,0.035)
 add_child(bg)
 var scroll=ScrollContainer.new()
 scroll.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
 scroll.offset_left=22
 scroll.offset_top=12
 scroll.offset_right=-22
 scroll.offset_bottom=-12
 add_child(scroll)
 var col=VBoxContainer.new()
 col.size_flags_horizontal=Control.SIZE_EXPAND_FILL
 col.add_theme_constant_override("separation",12)
 scroll.add_child(col)
 var eyebrow=_label("DARK FANTASY · ACTION RPG",13,Color(0.82,0.43,0.20))
 col.add_child(eyebrow)
 var title=_label("ХРОНИКИ ПЕПЛА",38,Color(0.91,0.84,0.72))
 col.add_child(title)
 col.add_child(_label("КОРОЛЕВСТВА ПАЛИ. ПРАХ ПОМНИТ ИМЕНА.",14,Color(0.57,0.52,0.49)))
 col.add_child(HSeparator.new())
 col.add_child(_label("ВЫБЕРИ СВОЮ СУДЬБУ",20,Color(0.85,0.72,0.53)))
 var row=HBoxContainer.new()
 row.size_flags_horizontal=Control.SIZE_EXPAND_FILL
 row.add_theme_constant_override("separation",10)
 col.add_child(row)
 for data in CLASSES:
  var card=PanelContainer.new()
  card.set_meta("class_id",data.id)
  card.size_flags_horizontal=Control.SIZE_EXPAND_FILL
  card.custom_minimum_size=Vector2(170,275)
  var style=StyleBoxFlat.new()
  style.bg_color=Color(0.075,0.067,0.075)
  style.border_color=data.color.darkened(0.55)
  style.set_border_width_all(2)
  style.set_corner_radius_all(5)
  style.content_margin_left=10
  style.content_margin_right=10
  style.content_margin_top=12
  style.content_margin_bottom=12
  card.add_theme_stylebox_override("panel",style)
  var inner=VBoxContainer.new()
  inner.add_theme_constant_override("separation",8)
  card.add_child(inner)
  var portrait=ColorRect.new()
  portrait.custom_minimum_size=Vector2(0,112)
  portrait.color=Color(0.035,0.03,0.04)
  inner.add_child(portrait)
  var symbol=_label(data.glyph,52,data.color)
  portrait.add_child(symbol)
  symbol.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
  symbol.vertical_alignment=VERTICAL_ALIGNMENT_CENTER
  var name=_label(data.name,17,data.color.lightened(0.25))
  inner.add_child(name)
  inner.add_child(_label(data.role,12,Color(0.82,0.77,0.70)))
  var desc=_label(data.desc,12,Color(0.63,0.60,0.59))
  desc.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART
  desc.custom_minimum_size=Vector2(0,58)
  inner.add_child(desc)
  var choose=Button.new()
  choose.text="ВЫБРАТЬ"
  choose.pressed.connect(_select.bind(data.id))
  inner.add_child(choose)
  row.add_child(card)
  cards.append(card)
 var lore=_label("Исследуй мёртвые леса • Спускайся в подземелья • Сражайся за древние реликвии",13,Color(0.63,0.59,0.55))
 lore.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART
 col.add_child(lore)
 start_button=Button.new()
 start_button.custom_minimum_size=Vector2(0,56)
 start_button.size_flags_horizontal=Control.SIZE_SHRINK_CENTER
 start_button.add_theme_font_size_override("font_size",19)
 start_button.pressed.connect(_start_game)
 col.add_child(start_button)
 col.add_child(_label("Мир создаётся заново. Ни одна экспедиция не будет прежней.",12,Color(0.43,0.40,0.40)))
 _refresh()
func _label(value:String, font_size:int, color:Color)->Label:
 var l=Label.new()
 l.text=value
 l.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
 l.add_theme_font_size_override("font_size",font_size)
 l.add_theme_color_override("font_color",color)
 return l
func _select(class_id:String):
 selected_id=class_id
 _refresh()
func _refresh():
 for card in cards:
  var id=str(card.get_meta("class_id"))
  var data=CLASSES.filter(func(item): return item.id==id)[0]
  var style=card.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
  style.border_color=data.color.lightened(0.25) if id==selected_id else data.color.darkened(0.55)
  style.set_border_width_all(4 if id==selected_id else 2)
  style.bg_color=Color(0.13,0.095,0.085) if id==selected_id else Color(0.075,0.067,0.075)
  card.add_theme_stylebox_override("panel",style)
 var chosen=CLASSES.filter(func(item): return item.id==selected_id)[0]
 start_button.text="НАЧАТЬ ПУТЬ · "+str(chosen.name)
func _start_game():
 get_tree().root.set_meta("selected_class",selected_id)
 get_tree().change_scene_to_file("res://scenes/world.tscn")
