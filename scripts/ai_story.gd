extends Node

const QUEST_ENDPOINT_SETTING := "ai/story_endpoint"
var world: Node3D
var request_node: HTTPRequest
var panel: PanelContainer
var quest_label: Label
var result_label: Label
var active_quest: Dictionary = {}
var recent_events: Array[String] = []
var completed_quests: Array[String] = []
var fallback_index := 0
var waiting_for_ai := false
var auto_generation_enabled := true
var generation_timer: Timer

var fallback_quests: Array[Dictionary] = [
    {"title":"Шёпот у старого тракта","story":"Среди пепла видны свежие следы. Слизень охраняет обломок печати погибшего каравана.","objective":"Победи тварей у старого тракта.","objective_type":"kill_slime","reward_xp":25,"next_hook":"На печати выцарапан знак неизвестного ордена."},
    {"title":"Свет в пепельной чаще","story":"Между деревьями мерцает огонь, зовущий глубже в лес.","objective":"Исследуй окрестности и переживи встречу с тварью чащи.","objective_type":"explore_ruins","reward_xp":30,"next_hook":"На земле лежит обгоревшая карта."},
    {"title":"Долг мёртвого путника","story":"Записка предупреждает о воротах под пепельным холмом.","objective":"Найди следы у холма и одолей охрану.","objective_type":"kill_slime","reward_xp":35,"next_hook":"Из-под холма доносится металлический удар."}
]

func setup(owner_world: Node3D) -> void:
    world = owner_world
    request_node = HTTPRequest.new()
    request_node.timeout = 30.0
    add_child(request_node)
    request_node.request_completed.connect(_on_request_completed)
    _build_ui()
    generation_timer = Timer.new()
    generation_timer.wait_time = 95.0
    generation_timer.one_shot = false
    add_child(generation_timer)
    generation_timer.timeout.connect(generate_quest)
    generation_timer.start()
    call_deferred("generate_quest")

func _build_ui() -> void:
    var hud := world.get_node_or_null("HUD") as CanvasLayer
    if hud == null:
        return
    panel = PanelContainer.new()
    panel.name = "StoryPanel"
    panel.anchor_left = 1.0
    panel.anchor_right = 1.0
    panel.anchor_top = 0.0
    panel.anchor_bottom = 0.0
    panel.offset_left = -330.0
    panel.offset_right = -12.0
    panel.offset_top = 18.0
    panel.offset_bottom = 230.0
    hud.add_child(panel)
    var column := VBoxContainer.new()
    column.add_theme_constant_override("separation", 6)
    panel.add_child(column)
    var heading := Label.new()
    heading.text = "ХРОНИКИ МИРА · ИИ"
    heading.add_theme_font_size_override("font_size", 18)
    column.add_child(heading)
    quest_label = Label.new()
    quest_label.text = "Летописец пробуждается…"
    quest_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    quest_label.custom_minimum_size = Vector2(280, 120)
    column.add_child(quest_label)
    result_label = Label.new()
    result_label.text = "ИИ: автоматическое создание событий включено"
    result_label.add_theme_font_size_override("font_size", 12)
    result_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    column.add_child(result_label)

func generate_quest() -> void:
    if waiting_for_ai:
        return
    var endpoint := str(ProjectSettings.get_setting(QUEST_ENDPOINT_SETTING, "")).strip_edges()
    if endpoint.is_empty():
        _show_fallback("Локальный режим · сервер не настроен")
        return
    waiting_for_ai = true
    result_label.text = "ИИ создаёт событие мира без подтверждения…"
    var player = world.get_node_or_null("Player")
    var level := int(player.level) if player != null else 1
    var class_id := str(player.class_id) if player != null else "warrior"
    var payload := {
        "level": level,
        "class": class_id,
        "location": "Мир Пепельных Королевств",
        "style": "оригинальное мрачное тёмное фэнтези, суровая готическая атмосфера, руины и древняя магия",
        "recent_events": recent_events,
        "completed_quests": completed_quests,
        "request_world_event": true
    }
    var headers := PackedStringArray(["Content-Type: application/json", "Accept: application/json"])
    var error := request_node.request(endpoint, headers, HTTPClient.METHOD_POST, JSON.stringify(payload))
    if error != OK:
        waiting_for_ai = false
        _show_fallback("Нет сети · создано локальное событие")
        return

func _on_request_completed(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
    waiting_for_ai = false
    if result == HTTPRequest.RESULT_SUCCESS and response_code >= 200 and response_code < 300:
        var parsed = JSON.parse_string(body.get_string_from_utf8())
        if parsed is Dictionary and parsed.get("quest") is Dictionary:
            var quest: Dictionary = parsed.quest
            if _is_valid_quest(quest):
                _set_quest(quest)
                var world_event: Dictionary = parsed.get("world_event", {})
                if not world_event.is_empty() and world.has_method("spawn_ai_dungeon"):
                    world.spawn_ai_dungeon(world_event)
                result_label.text = "ИИ создал событие и локацию · без подтверждения"
                return
    _show_fallback("ИИ временно недоступен · мир продолжает жить")

func _is_valid_quest(quest: Dictionary) -> bool:
    return not str(quest.get("title", "")).strip_edges().is_empty() and not str(quest.get("story", "")).strip_edges().is_empty() and not str(quest.get("objective", "")).strip_edges().is_empty()

func _show_fallback(message: String) -> void:
    var quest: Dictionary = fallback_quests[fallback_index % fallback_quests.size()].duplicate(true)
    fallback_index += 1
    _set_quest(quest)
    # Even offline, the world gets an original procedural dungeon/event.
    if world.has_method("spawn_ai_dungeon"):
        world.spawn_ai_dungeon({"title":"Разлом " + str(fallback_index) + " · " + ["Чёрного Эха","Сломанной Короны","Забытых Костей"][randi_range(0,2)],"kind":"haunted_cave","enemy_count":randi_range(4,7),"chest_count":randi_range(2,4)})
    result_label.text = message

func _set_quest(quest: Dictionary) -> void:
    active_quest = quest.duplicate(true)
    quest_label.text = "%s\n\n%s\n\nЦель: %s\nНаграда: %d опыта" % [
        str(active_quest.get("title", "Неизвестное событие")),
        str(active_quest.get("story", "")),
        str(active_quest.get("objective", "")),
        int(active_quest.get("reward_xp", 25))
    ]
    recent_events.append("Событие: " + str(active_quest.get("title", "")))
    while recent_events.size() > 8:
        recent_events.pop_front()

func note_world_event(description: String) -> void:
    recent_events.append(description)
    while recent_events.size() > 8:
        recent_events.pop_front()

func on_enemy_defeated(enemy_type: String) -> void:
    if active_quest.is_empty():
        return
    if str(active_quest.get("objective_type", "")) == "kill_slime" and enemy_type == "slime":
        var title := str(active_quest.get("title", "Задание"))
        var reward := int(active_quest.get("reward_xp", 25))
        completed_quests.append(title)
        note_world_event("Задание завершено: " + title)
        active_quest.clear()
        quest_label.text = "Задание выполнено: %s\n\nНаграда: %d опыта. Следующее событие появится автоматически." % [title, reward]
        result_label.text = "Событие завершено"
        var player = world.get_node_or_null("Player")
        if player != null:
            player.experience += reward
            if player.experience >= player.level * 50:
                player.experience -= player.level * 50
                player.level += 1
            world.update_status(player.level, player.health, player.experience)
        if generation_timer:
            generation_timer.start(5.0)
