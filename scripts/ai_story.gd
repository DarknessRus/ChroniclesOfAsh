extends Node

const QUEST_ENDPOINT_SETTING := "ai/story_endpoint"

var world: Node3D
var request_node: HTTPRequest
var panel: PanelContainer
var quest_label: Label
var result_label: Label
var generate_button: Button
var active_quest: Dictionary = {}
var recent_events: Array[String] = []
var completed_quests: Array[String] = []
var fallback_index := 0
var waiting_for_ai := false

var fallback_quests: Array[Dictionary] = [
    {
        "title": "Шёпот у старого тракта",
        "story": "Среди пепла ты замечаешь свежие следы. Слизень охраняет обломок печати, оставленный кем-то из погибшего каравана.",
        "objective": "Победи слизня и проверь, что осталось на тракте.",
        "objective_type": "kill_slime",
        "reward_xp": 25,
        "next_hook": "На печати выцарапан знак неизвестного ордена."
    },
    {
        "title": "Свет в пепельной чаще",
        "story": "Между деревьями мерцает тусклый огонь. Он появляется лишь на мгновение, будто кто-то зовёт тебя глубже в лес.",
        "objective": "Исследуй окрестности и переживи встречу с тварью чащи.",
        "objective_type": "explore_ruins",
        "reward_xp": 30,
        "next_hook": "Кто-то оставил на земле обгоревшую карту."
    },
    {
        "title": "Долг мёртвого путника",
        "story": "Ветер приносит обрывок записки: «Если кто-то найдёт это, не позволяйте им открыть ворота под пепельным холмом». Подпись стёрта.",
        "objective": "Победи слизня у старого тракта и найди следы ведущие к холму.",
        "objective_type": "kill_slime",
        "reward_xp": 35,
        "next_hook": "Из-под холма доносится глухой металлический удар."
    },
    {
        "title": "Следы под корнями",
        "story": "Земля вокруг древнего дерева потрескалась. Кто-то недавно вытащил из неё небольшой предмет, оставив после себя странный серый след.",
        "objective": "Исследуй место и одолей охраняющую его тварь.",
        "objective_type": "explore_ruins",
        "reward_xp": 30,
        "next_hook": "След ведёт к развалинам, которых нет ни на одной карте."
    }
]

func setup(owner_world: Node3D) -> void:
    world = owner_world
    request_node = HTTPRequest.new()
    request_node.timeout = 30.0
    add_child(request_node)
    request_node.request_completed.connect(_on_request_completed)
    _build_ui()

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
    panel.offset_left = -340.0
    panel.offset_right = -18.0
    panel.offset_top = 18.0
    panel.offset_bottom = 252.0
    hud.add_child(panel)

    var column := VBoxContainer.new()
    column.add_theme_constant_override("separation", 8)
    panel.add_child(column)

    var heading := Label.new()
    heading.text = "ХРОНИКИ МИРА"
    heading.add_theme_font_size_override("font_size", 18)
    column.add_child(heading)

    quest_label = Label.new()
    quest_label.text = "Мир хранит тайны. Создай новое задание."
    quest_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    quest_label.custom_minimum_size = Vector2(285, 125)
    column.add_child(quest_label)

    result_label = Label.new()
    result_label.text = "ИИ: не подключён"
    result_label.add_theme_font_size_override("font_size", 12)
    result_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    column.add_child(result_label)

    generate_button = Button.new()
    generate_button.text = "НОВОЕ СОБЫТИЕ"
    generate_button.custom_minimum_size = Vector2(0, 42)
    generate_button.pressed.connect(generate_quest)
    column.add_child(generate_button)

func generate_quest() -> void:
    if waiting_for_ai:
        return
    var endpoint := str(ProjectSettings.get_setting(QUEST_ENDPOINT_SETTING, "")).strip_edges()
    if endpoint.is_empty():
        _show_fallback("Локальное событие · ИИ-сервер ещё не подключён")
        return

    waiting_for_ai = true
    generate_button.disabled = true
    generate_button.text = "СОЗДАЁМ ИСТОРИЮ..."
    result_label.text = "Связываемся с летописцем мира…"

    var player = world.get_node_or_null("Player")
    var level := int(player.level) if player != null and "level" in player else 1
    var payload := {
        "level": level,
        "location": "Пепельная чаща",
        "style": "мрачное тёмное фэнтези, тайны древнего пепла",
        "recent_events": recent_events,
        "completed_quests": completed_quests
    }
    var headers := PackedStringArray([
        "Content-Type: application/json",
        "Accept: application/json"
    ])
    var error := request_node.request(endpoint, headers, HTTPClient.METHOD_POST, JSON.stringify(payload))
    if error != OK:
        waiting_for_ai = false
        generate_button.disabled = false
        generate_button.text = "НОВОЕ СОБЫТИЕ"
        _show_fallback("Сеть недоступна · локальное событие")
        return
    result_label.text = "ИИ придумывает событие…"

func _on_request_completed(result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
    waiting_for_ai = false
    generate_button.disabled = false
    generate_button.text = "НОВОЕ СОБЫТИЕ"

    if result == HTTPRequest.RESULT_SUCCESS and response_code >= 200 and response_code < 300:
        var parsed = JSON.parse_string(body.get_string_from_utf8())
        if parsed is Dictionary and parsed.get("quest") is Dictionary:
            var quest: Dictionary = parsed.quest
            if _is_valid_quest(quest):
                _set_quest(quest)
                result_label.text = "Создано ИИ · событие сохранено в истории"
                return

    _show_fallback("ИИ временно недоступен · локальное событие")

func _is_valid_quest(quest: Dictionary) -> bool:
    return not str(quest.get("title", "")).strip_edges().is_empty() and not str(quest.get("story", "")).strip_edges().is_empty() and not str(quest.get("objective", "")).strip_edges().is_empty()

func _show_fallback(message: String) -> void:
    var quest: Dictionary = fallback_quests[fallback_index % fallback_quests.size()].duplicate(true)
    fallback_index += 1
    _set_quest(quest)
    result_label.text = message

func _set_quest(quest: Dictionary) -> void:
    active_quest = quest.duplicate(true)
    var reward := int(active_quest.get("reward_xp", 25))
    quest_label.text = "%s\n\n%s\n\nЦель: %s\nНаграда: %d опыта" % [
        str(active_quest.get("title", "Неизвестное событие")),
        str(active_quest.get("story", "")),
        str(active_quest.get("objective", "")),
        reward
    ]
    recent_events.append("Начато задание: " + str(active_quest.get("title", "")))
    while recent_events.size() > 8:
        recent_events.pop_front()

func on_enemy_defeated(enemy_type: String) -> void:
    if active_quest.is_empty():
        return
    if str(active_quest.get("objective_type", "")) == "kill_slime" and enemy_type == "slime":
        var title := str(active_quest.get("title", "Задание"))
        var reward := int(active_quest.get("reward_xp", 25))
        completed_quests.append(title)
        recent_events.append("Задание завершено: " + title)
        active_quest.clear()
        quest_label.text = "Задание выполнено: %s\n\nНаграда: %d опыта. Создай новое событие, чтобы узнать, что произойдёт дальше." % [title, reward]
        result_label.text = "Событие завершено"
        var player = world.get_node_or_null("Player")
        if player != null and "experience" in player:
            player.experience += reward
            if player.experience >= player.level * 50:
                player.experience -= player.level * 50
                player.level += 1
            world.update_status(player.level, player.health, player.experience)
