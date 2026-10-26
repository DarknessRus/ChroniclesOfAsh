using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Clean entry point for the first playable slice of Chronicles of Ash.
/// Menu and world creation are separated so failures never leave the player on a dead screen.
/// </summary>
public sealed class AshBootstrap : MonoBehaviour
{
    private static AshBootstrap instance;
    private Canvas canvas;
    private GameObject menuRoot;
    private GameObject worldRoot;
    private Font uiFont;
    private int selectedHero;
    private bool starting;
    private Text loadingLabel;

    private readonly string[] heroes = { "ВОИН", "МАГ", "СЛЕДОПЫТ", "НЕКРОМАНТ" };
    private readonly string[] descriptions = {
        "Сталь и ярость", "Пламя древних", "Лесная тень", "Власть праха"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var root = new GameObject("Chronicles of Ash - Game Root");
        instance = root.AddComponent<AshBootstrap>();
        DontDestroyOnLoad(root);
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureEventSystem();
        ShowMainMenu();
    }

    private void EnsureEventSystem()
    {
        var es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es = go.GetComponent<EventSystem>();
            DontDestroyOnLoad(go);
        }
        if (es.GetComponent<StandaloneInputModule>() == null)
            es.gameObject.AddComponent<StandaloneInputModule>();
        foreach (var module in es.GetComponents<BaseInputModule>())
            module.enabled = module is StandaloneInputModule;
    }

    private void ShowMainMenu()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        var canvasObject = new GameObject("Ash UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        DontDestroyOnLoad(canvasObject);

        menuRoot = new GameObject("Main Menu", typeof(RectTransform));
        menuRoot.transform.SetParent(canvas.transform, false);
        Stretch(menuRoot.GetComponent<RectTransform>());

        Panel(menuRoot.transform, new Vector2(0, 0), new Vector2(1, 1), new Color(.025f, .03f, .035f));
        Panel(menuRoot.transform, new Vector2(.02f, .05f), new Vector2(.98f, .95f), new Color(.065f, .07f, .075f));
        Label(menuRoot.transform, "ХРОНИКИ ПЕПЛА", 66, new Vector2(.05f, .77f), new Vector2(.68f, .93f), new Color(.93f, .61f, .24f), FontStyle.Bold);
        Label(menuRoot.transform, "МРАЧНАЯ ЭКШЕН-RPG", 24, new Vector2(.055f, .71f), new Vector2(.65f, .79f), new Color(.65f, .67f, .66f), FontStyle.Italic);

        for (int i = 0; i < heroes.Length; i++)
        {
            int index = i;
            float left = .05f + i * .155f;
            var card = Panel(menuRoot.transform, new Vector2(left, .25f), new Vector2(left + .145f, .66f),
                i == selectedHero ? new Color(.25f, .14f, .075f) : new Color(.105f, .115f, .12f));
            Label(card.transform, HeroMark(i), 72, new Vector2(.05f, .32f), new Vector2(.95f, .9f),
                HeroAccent(i), FontStyle.Bold);
            Label(card.transform, heroes[i], 23, new Vector2(.02f, .12f), new Vector2(.98f, .31f), new Color(.93f, .9f, .83f), FontStyle.Bold);
            Label(card.transform, descriptions[i], 15, new Vector2(.02f, .025f), new Vector2(.98f, .13f), new Color(.7f, .7f, .68f), FontStyle.Normal);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => { selectedHero = index; ShowMainMenu(); });
        }

        Label(menuRoot.transform,
            "Королевство поглотил пепел. Из руин поднимаются древние силы. Выбери героя и отправляйся за пределы последнего убежища.",
            23, new Vector2(.69f, .47f), new Vector2(.95f, .73f), new Color(.78f, .78f, .74f), FontStyle.Normal);
        Label(menuRoot.transform, "ОДИНОЧНОЕ ПРИКЛЮЧЕНИЕ", 18, new Vector2(.69f, .38f), new Vector2(.95f, .46f), HeroAccent(0), FontStyle.Bold);

        var startPanel = Panel(menuRoot.transform, new Vector2(.69f, .16f), new Vector2(.95f, .29f), new Color(.58f, .24f, .07f));
        var startButton = startPanel.gameObject.AddComponent<Button>();
        startButton.targetGraphic = startPanel.GetComponent<Image>();
        Label(startPanel.transform, "НАЧАТЬ ИГРУ  ▶", 28, Vector2.zero, Vector2.one, new Color(1f, .91f, .72f), FontStyle.Bold);
        startButton.onClick.AddListener(() => { Debug.Log("[Chronicles of Ash] Start button clicked."); StartGame(); });

        Label(menuRoot.transform, "ANDROID  •  ТЁМНОЕ ФЭНТЕЗИ  •  ОДИНОЧНАЯ ИГРА", 15,
            new Vector2(.05f, .065f), new Vector2(.65f, .12f), new Color(.43f, .46f, .46f), FontStyle.Normal);
    }

    private void StartGame()
    {
        if (starting) return;
        starting = true;
        Debug.Log("[Chronicles of Ash] Start requested.");
        if (menuRoot != null) menuRoot.SetActive(false);
        var loadingPanel = Panel(canvas.transform, Vector2.zero, Vector2.one, new Color(.025f, .03f, .035f, .96f));
        loadingPanel.name = "World Loading Screen";
        Label(loadingPanel.transform, "ПРОБУЖДЕНИЕ ДОЛИНЫ ПЕПЛА…", 34,
            new Vector2(.12f, .38f), new Vector2(.88f, .62f), new Color(.93f, .61f, .24f), FontStyle.Bold);
        StartCoroutine(BuildWorldRoutine(loadingPanel));
    }

    private IEnumerator BuildWorldRoutine(GameObject loadingPanel)
    {
        // Render loading feedback before generating the procedural environment.
        yield return null;
        yield return new WaitForEndOfFrame();
        GameObject candidate = null;
        try
        {
            // Avoid two active gameplay cameras if a scene already contains one.
            foreach (var oldCamera in FindObjectsOfType<Camera>())
                if (oldCamera != null && oldCamera.gameObject.name != "UI Camera")
                    Destroy(oldCamera.gameObject);

            candidate = new GameObject("Ash Valley World");
            AshDiabloWorldBuilder.Build(candidate.transform, selectedHero);
            worldRoot = candidate;
            if (loadingPanel != null) Destroy(loadingPanel);
            Debug.Log("[Chronicles of Ash] Playable world created.");
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[Chronicles of Ash] World creation failed: " + exception);
            if (candidate != null) Destroy(candidate);
            worldRoot = null;
            if (loadingPanel != null) Destroy(loadingPanel);
            starting = false;
            ShowMainMenu();
            yield break;
        }
        starting = false;
    }

    private GameObject Panel(Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = color;
        return go;
    }

    private Text Label(Transform parent, string value, int size, Vector2 min, Vector2 max, Color color, FontStyle style)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = new Vector2(8, 4); rect.offsetMax = new Vector2(-8, -4);
        var text = go.GetComponent<Text>();
        text.font = uiFont; text.text = value; text.fontSize = size; text.color = color;
        text.fontStyle = style; text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private string HeroMark(int i) { return new[] { "⚔", "✦", "➶", "☠" }[i]; }
    private Color HeroAccent(int i)
    {
        return i == 0 ? new Color(.9f, .38f, .12f) :
               i == 1 ? new Color(.2f, .55f, .9f) :
               i == 2 ? new Color(.25f, .65f, .38f) : new Color(.62f, .35f, .78f);
    }
}
