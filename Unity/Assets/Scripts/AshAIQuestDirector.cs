using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight in-world quest director for mobile. It supplies contextual guidance locally,
/// so the adventure works offline; a remote generative-AI endpoint can be connected later.
/// </summary>
public sealed class AshAIQuestDirector : MonoBehaviour
{
    Canvas canvas;
    Text title;
    Text body;
    Transform player;
    Transform enemy;
    float nextRefresh;
    int lastHint = -1;
    readonly string[] explorationHints =
    {
        "Следуй по дороге к разрушенным воротам. Руны указывают путь к долине.",
        "Осмотри светящиеся кристаллы: древняя магия всё ещё удерживает эти земли.",
        "Не спеши вступать в бой. Подойди к руинам и изучи проход между башнями.",
        "Пепел скрывает старые тропы. Держись дороги и следи за движением теней."
    };

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            var candidates = FindObjectsOfType<AshPlayerController>();
            if (candidates.Length > 0) player = candidates[0].transform;
        }
        var e = GameObject.Find("Ash Wraith");
        if (e != null) enemy = e.transform;
        CreateGuidePanel();
        RefreshHint(true);
    }

    void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 4f;
            RefreshHint(false);
        }
    }

    void CreateGuidePanel()
    {
        var root = new GameObject("AI Guide HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        var panel = new GameObject("AI Guide Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.69f, .76f);
        rect.anchorMax = new Vector2(.985f, .965f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.025f, .035f, .04f, .84f);

        title = MakeText(panel.transform, "ПРОВОДНИК ПЕПЛА", 22, new Vector2(.04f, .68f), new Vector2(.96f, .98f));
        title.color = new Color(1f, .58f, .19f);
        title.fontStyle = FontStyle.Bold;
        body = MakeText(panel.transform, "Путь ещё не определён...", 18, new Vector2(.04f, .06f), new Vector2(.96f, .69f));
        body.color = new Color(.88f, .86f, .78f);
    }

    Text MakeText(Transform parent, string value, int size, Vector2 min, Vector2 max)
    {
        var go = new GameObject("Guide Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = new Vector2(8, 4); r.offsetMax = new Vector2(-8, -4);
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.text = value;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        return t;
    }

    void RefreshHint(bool force)
    {
        if (body == null) return;
        if (player == null)
        {
            body.text = "Проводник пробуждается. Исследуй долину и найди разрушенные ворота.";
            return;
        }

        if (enemy != null && Vector3.Distance(player.position, enemy.position) < 7f)
        {
            title.text = "УГРОЗА ОБНАРУЖЕНА";
            body.text = "Тень рядом! Держи врага перед собой, уклоняйся и используй боевые умения.";
            return;
        }

        title.text = "ПРОВОДНИК ПЕПЛА";
        float distance = Vector3.Distance(player.position, new Vector3(0f, 0f, 18f));
        if (distance < 5f)
        {
            body.text = "Ты достиг древних ворот. Исследуй проход: за ним начинается путь в долину теней.";
            return;
        }

        if (force || lastHint < 0)
            lastHint = Random.Range(0, explorationHints.Length);
        else if (Random.value < .35f)
            lastHint = (lastHint + 1 + Random.Range(0, explorationHints.Length - 1)) % explorationHints.Length;
        body.text = explorationHints[lastHint];
    }
}
