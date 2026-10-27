using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>Bootstraps the first playable Unity prototype without requiring a hand-authored scene.</summary>
public class AshBootstrap : MonoBehaviour
{
    static AshBootstrap instance;
    static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var root = new GameObject("Ash Chronicles Bootstrap");
        instance = root.AddComponent<AshBootstrap>();
        DontDestroyOnLoad(root);
    }

    Canvas canvas;
    Font font;
    GameObject menuRoot, worldRoot;
    int selectedHero;
    readonly string[] heroNames = { "ВОИН", "МАГ", "СЛЕДОПЫТ", "НЕКРОМАНТ" };
    readonly string[] heroRoles = { "Сталь и ярость", "Пламя древних", "Лесная тень", "Власть праха" };

    void Start()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureEventSystem();
        ShowMenu();
    }

    void EnsureEventSystem()
    {
        // Keep one active EventSystem and make sure it can process legacy touch input on Android.
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var e = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = e.GetComponent<EventSystem>();
            DontDestroyOnLoad(e);
        }

        var module = eventSystem.GetComponent<StandaloneInputModule>();
        if (module == null)
            module = eventSystem.gameObject.AddComponent<StandaloneInputModule>();

        // Avoid two input modules competing for the same pointer events.
        var modules = eventSystem.GetComponents<BaseInputModule>();
        foreach (var inputModule in modules)
        {
            if (inputModule != module)
                inputModule.enabled = false;
        }

        module.enabled = true;
    }

    void CreateCanvas()
    {
        var go = new GameObject("Ash UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        DontDestroyOnLoad(go);
    }

    void ShowMenu()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        CreateCanvas();
        menuRoot = new GameObject("Hero Selection Menu");
        menuRoot.transform.SetParent(canvas.transform, false);
        RectTransform full = menuRoot.AddComponent<RectTransform>();
        full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
        full.offsetMin = Vector2.zero; full.offsetMax = Vector2.zero;

        // Use the exact uploaded reference artwork as the menu, edge-to-edge.
        // The image is in Resources so it is included in Android builds.
        Texture2D artwork = Resources.Load<Texture2D>("Menu/HeroSelectionBackground");
        if (artwork != null)
        {
            Sprite backgroundSprite = Sprite.Create(
                artwork, new Rect(0, 0, artwork.width, artwork.height),
                new Vector2(0.5f, 0.5f), 100f);
            var bg = new GameObject("Exact Hero Selection Artwork", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(menuRoot.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var bgImage = bg.GetComponent<Image>();
            bgImage.sprite = backgroundSprite;
            bgImage.preserveAspect = false;
            bgImage.raycastTarget = false;
        }
        else
        {
            Debug.LogError("[Ash Chronicles] HeroSelectionBackground.png missing at Resources/Menu/HeroSelectionBackground.png");
            AddPanel(menuRoot.transform, Vector2.zero, Vector2.one, new Color(.018f,.024f,.032f,1f));
            AddText(menuRoot.transform, "НЕ НАЙДЕН ФОН МЕНЮ", 28, new Vector2(.2f,.4f), new Vector2(.8f,.6f),
                Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        // Invisible touch zones preserve all four class selections without drawing
        // substitute cards or text over the supplied artwork.
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            float x0 = .025f + i * .24f;
            float x1 = x0 + .225f;
            var hit = AddPanel(menuRoot.transform, new Vector2(x0,.20f), new Vector2(x1,.79f), new Color(1,1,1,0));
            hit.name = "Select hero " + i;

            // Clearly mark the currently selected hero without changing the reference artwork.
            if (i == selectedHero)
            {
                var tint = hit.GetComponent<Image>();
                tint.color = new Color(1f, .20f, .015f, .035f);
                tint.raycastTarget = false;
                AddHeroContourOverlay(menuRoot.transform, i);
            }

            var button = hit.AddComponent<Button>();
            button.targetGraphic = hit.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = new Color(1,1,1,0);
            colors.highlightedColor = new Color(1f,.65f,.25f,.08f);
            colors.pressedColor = new Color(1f,.40f,.10f,.12f);
            button.colors = colors;
            button.onClick.AddListener(() => SelectHero(index));
        }

        // Transparent hit area over the central bottom CTA in the reference image.
        var start = AddPanel(menuRoot.transform, new Vector2(.36f,.025f), new Vector2(.64f,.16f), new Color(1,1,1,0));
        start.name = "Start Game - artwork CTA";
        var startButton = start.AddComponent<Button>();
        startButton.targetGraphic = start.GetComponent<Image>();
        var startColors = startButton.colors;
        startColors.normalColor = new Color(1,1,1,0);
        startColors.highlightedColor = new Color(1f,.65f,.25f,.07f);
        startColors.pressedColor = new Color(1f,.40f,.10f,.12f);
        startButton.colors = startColors;
        startButton.onClick.AddListener(StartGame);
    }

    void AddHeroContourOverlay(Transform parent, int heroIndex)
    {
        // Generated from the source artwork by foreground segmentation in the build workflow.
        // Each texture is full-canvas, so its outline aligns with the original hero pixels.
        Texture2D contourTexture = Resources.Load<Texture2D>("Menu/HeroContour" + heroIndex);
        if (contourTexture == null)
        {
            Debug.LogWarning("[Ash Chronicles] Precise hero contour texture missing for hero " + heroIndex +
                ". Run the build workflow to generate it.");
            return;
        }

        Sprite contourSprite = Sprite.Create(
            contourTexture, new Rect(0, 0, contourTexture.width, contourTexture.height),
            new Vector2(.5f, .5f), 100f);
        var overlay = new GameObject("Pixel-accurate fire contour " + heroIndex,
            typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = overlay.GetComponent<Image>();
        image.sprite = contourSprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    Color HeroColor(int i) { return i==0?new Color(.28f,.22f,.17f):i==1?new Color(.10f,.25f,.34f):i==2?new Color(.15f,.29f,.24f):new Color(.25f,.18f,.31f); }
    void SelectHero(int i) { selectedHero=i; ShowMenu(); }

    void AddHeroSigil(Transform parent,int i)
    {
        string[] marks={"⚔","✦","➶","☠"};
        AddText(parent,marks[i],90,new Vector2(0,0.32f),new Vector2(1,0.95f),new Color(.88f,.78f,.58f,.95f),TextAnchor.MiddleCenter,FontStyle.Bold);
        AddPanel(parent,new Vector2(.12f,.31f),new Vector2(.88f,.325f),new Color(.86f,.58f,.22f,.8f));
    }

    GameObject AddPanel(Transform parent,Vector2 min,Vector2 max,Color color)
    {
        var g=new GameObject("Panel",typeof(RectTransform),typeof(Image));
        g.transform.SetParent(parent,false);
        var r=g.GetComponent<RectTransform>(); r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;
        g.GetComponent<Image>().color=color; return g;
    }
    Text AddText(Transform parent,string value,int size,Vector2 min,Vector2 max,Color color,TextAnchor anchor,FontStyle style)
    {
        var g=new GameObject("Text",typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);
        var r=g.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=new Vector2(8,4);r.offsetMax=new Vector2(-8,-4);
        var t=g.GetComponent<Text>();t.text=value;t.font=font;t.fontSize=size;t.color=color;t.alignment=anchor;t.fontStyle=style;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
    }

    bool gameStarting;
    GameObject loadingRoot;
    Text loadingLabel;

    void StartGame()
    {
        if (gameStarting) return;
        gameStarting = true;
        Debug.Log("[Ash Chronicles] Start button received; starting staged world creation.");
        StartCoroutine(StartGameRoutine());
    }

    System.Collections.IEnumerator StartGameRoutine()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        if (canvas != null) canvas.gameObject.SetActive(true);
        loadingRoot = AddPanel(canvas.transform, Vector2.zero, Vector2.one, new Color(.025f,.032f,.038f,.98f));
        loadingRoot.name = "World Loading Overlay";
        loadingLabel = AddText(loadingRoot.transform, "ПРОБУЖДЕНИЕ ПЕПЕЛЬНОЙ ДОЛИНЫ...\nПодготовка мира", 30,
            new Vector2(.12f,.38f), new Vector2(.88f,.62f), new Color(.94f,.72f,.38f),
            TextAnchor.MiddleCenter, FontStyle.Bold);

        // Let Unity render the loading overlay before creating any scene objects.
        yield return null;
        worldRoot = new GameObject("Ash Valley World");

        if (!RunBuildStep("Создание долины...", delegate { BuildWorld(worldRoot.transform); })) yield break;
        yield return null;

        // AshOpenWorld is the single full-scale terrain/river/forest generator.
        // The older BuildOpenWorld generator created a second, overlapping forest and river.
        if (!RunBuildStep("Создание открытого мира: горы, реки и леса...", delegate { AshOpenWorld.Build(worldRoot.transform); })) yield break;
        yield return null;

        if (!RunBuildStep("Добавление руин и деталей...", delegate { AshArtExpansion.Build(worldRoot.transform); })) yield break;
        yield return null;

        if (!RunBuildStep("Создание замка и долины...", delegate { AshWorldUpgrade.BuildCastleAndValley(worldRoot.transform); })) yield break;
        yield return null;

        if (!RunBuildStep("Подготовка заданий...", delegate { worldRoot.AddComponent<AshAIQuestDirector>(); })) yield break;
        yield return null;

        if (!RunBuildStep("Создание героя и управления...", BuildPlayerAndHud)) yield break;

        // Remove the loading overlay only after the player, camera and HUD exist.
        // Otherwise it remains above the game and makes Start look unresponsive.
        if (loadingRoot != null)
        {
            loadingRoot.SetActive(false);
            Destroy(loadingRoot);
            loadingRoot = null;
            loadingLabel = null;
        }

        gameStarting = false;
        Debug.Log("[Ash Chronicles] Playable world is ready and loading overlay was removed.");
    }

    bool RunBuildStep(string status, System.Action action)
    {
        try
        {
            if (loadingLabel != null) loadingLabel.text = status;
            Debug.Log("[Ash Chronicles] " + status);
            action();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Ash Chronicles] " + status + " FAILED: " + ex);
            if (loadingLabel != null) loadingLabel.text = "НЕ УДАЛОСЬ ЗАПУСТИТЬ ИГРУ\n" + ex.GetType().Name + ": " + ex.Message;
            var back = AddPanel(loadingRoot.transform, new Vector2(.34f,.18f), new Vector2(.66f,.29f), new Color(.55f,.22f,.07f,1));
            back.name = "Return to character selection";
            AddText(back.transform, "НАЗАД К ВЫБОРУ ГЕРОЯ", 20, Vector2.zero, Vector2.one,
                new Color(1f,.91f,.72f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var button = back.AddComponent<Button>();
            button.targetGraphic = back.GetComponent<Image>();
            button.onClick.AddListener(ReturnToMenuAfterFailure);
            gameStarting = false;
            return false;
        }
    }

    void ReturnToMenuAfterFailure()
    {
        if (worldRoot != null) Destroy(worldRoot);
        worldRoot = null;
        if (loadingRoot != null) Destroy(loadingRoot);
        loadingRoot = null;
        loadingLabel = null;
        if (canvas != null) canvas.gameObject.SetActive(true);
        if (menuRoot != null) menuRoot.SetActive(true);
        gameStarting = false;
    }

    void BuildPlayerAndHud()
    {
        var player=GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name=heroNames[selectedHero]+" Player"; player.transform.position=new Vector3(0,1,0);
        player.GetComponent<Renderer>().material=Mat(new Color(.22f,.24f,.25f),.65f);
        var controller=player.AddComponent<CharacterController>(); controller.height=1.8f; controller.radius=.35f;
        player.AddComponent<AshPlayerController>();
        // Keep the CharacterController capsule for collision, but show only the assembled armor model.
        player.GetComponent<Renderer>().enabled = false;
        AshArtExpansion.DetailHero(player.transform, selectedHero);
        var enemy=GameObject.CreatePrimitive(PrimitiveType.Capsule);enemy.name="Ash Wraith";enemy.transform.position=new Vector3(2,1,5);enemy.GetComponent<Renderer>().material=Mat(new Color(.35f,.12f,.16f),.8f);enemy.AddComponent<AshEnemy>();
        AshArtExpansion.DetailWraith(enemy.transform);
        var cam=new GameObject("Follow Camera").AddComponent<Camera>(); cam.tag="MainCamera";
        cam.transform.position=new Vector3(0,5,-8);cam.transform.LookAt(player.transform.position+Vector3.up*1.2f);
        var follow=cam.gameObject.AddComponent<AshFollowCamera>();follow.target=player.transform;
        cam.fieldOfView=58;cam.farClipPlane=350;
        RenderSettings.fog=true;RenderSettings.fogColor=new Color(.35f,.42f,.42f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.006f;
        var light=new GameObject("Late dusk sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.color=new Color(1f,.78f,.56f);light.transform.rotation=Quaternion.Euler(38,-32,0);
        AddWorldHUD();
    }

    void BuildWorld(Transform root)
    {
        RenderSettings.ambientLight=new Color(.34f,.37f,.37f);
        // The open-world terrain mesh supplies the ground; do not place a giant flat plane over it.
        // Mountains and forests are supplied by AshOpenWorld; avoid building a second overlapping set.
        // River and stone crossing.
        // The winding river is generated by AshOpenWorld; keep the stone crossing below.
        for(int i=0;i<7;i++){var stone=GameObject.CreatePrimitive(PrimitiveType.Cube);stone.name="Old stone bridge";stone.transform.SetParent(root);stone.transform.position=new Vector3(0,.18f,2+i*.85f);stone.transform.localScale=new Vector3(4.8f,.35f,.75f);stone.GetComponent<Renderer>().material=Mat(new Color(.34f,.34f,.31f),.95f);}
        // Distant gothic keep.
        var keep=GameObject.CreatePrimitive(PrimitiveType.Cube);keep.name="Ashen castle";keep.transform.SetParent(root);keep.transform.position=new Vector3(0,7,48);keep.transform.localScale=new Vector3(13,14,7);keep.GetComponent<Renderer>().material=Mat(new Color(.17f,.18f,.19f),.95f);
        for(int i=0;i<4;i++){var tower=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tower.transform.SetParent(root);tower.transform.position=new Vector3(-5+i*3.3f,12,48);tower.transform.localScale=new Vector3(1.4f,4,1.4f);tower.GetComponent<Renderer>().material=Mat(new Color(.13f,.14f,.16f),.95f);var roof=CreateCone(root,new Vector3(-5+i*3.3f,17,48),new Vector3(2,4,2));roof.GetComponent<Renderer>().material=Mat(new Color(.10f,.11f,.13f),.95f);}
        // Campfire and canvas tent.
        var fire=GameObject.CreatePrimitive(PrimitiveType.Sphere);fire.name="Campfire ember";fire.transform.SetParent(root);fire.transform.position=new Vector3(-5,.6f,-2);fire.transform.localScale=new Vector3(.5f,.9f,.5f);fire.GetComponent<Renderer>().material=Mat(new Color(1f,.28f,.035f),.35f);
        var flame=fire.AddComponent<Light>();flame.type=LightType.Point;flame.range=12;flame.intensity=3;flame.color=new Color(1f,.28f,.07f);
        var tent=GameObject.CreatePrimitive(PrimitiveType.Cube);tent.name="Traveller tent";tent.transform.SetParent(root);tent.transform.position=new Vector3(-8,1,-1);tent.transform.rotation=Quaternion.Euler(0,35,0);tent.transform.localScale=new Vector3(3,2,3);tent.GetComponent<Renderer>().material=Mat(new Color(.31f,.22f,.13f),1);
        BuildWorldDetails(root);
    }
    void BuildWorldDetails(Transform root)
    {
        // Broken basalt slabs and boulders break up the flat prototype ground.
        for(int i=0;i<34;i++)
        {
            float a=i*2.399f;
            float radius=7f+(i%6)*4.2f;
            Vector3 p=new Vector3(Mathf.Cos(a)*radius,.25f,Mathf.Sin(a)*radius+5f);
            var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name="Weathered basalt"; rock.transform.SetParent(root);
            rock.transform.position=p;
            rock.transform.rotation=Quaternion.Euler(i*13f,i*37f,i*19f);
            float s=.55f+(i%4)*.22f;
            rock.transform.localScale=new Vector3(s*1.5f,s,s*.9f);
            rock.GetComponent<Renderer>().material=Mat(new Color(.19f,.21f,.22f),.98f);
        }
        // A winding ash path leads from the camp toward the bridge.
        for(int i=0;i<18;i++)
        {
            float z=-7f+i*1.05f;
            float x=Mathf.Sin(i*.22f)*1.7f;
            var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name="Ashen footpath"; tile.transform.SetParent(root);
            tile.transform.position=new Vector3(x,.055f,z);
            tile.transform.rotation=Quaternion.Euler(0,Mathf.Sin(i*.4f)*12f,0);
            tile.transform.localScale=new Vector3(1.45f,.07f,1.0f);
            tile.GetComponent<Renderer>().material=Mat(i%3==0?new Color(.27f,.25f,.23f):new Color(.21f,.22f,.22f),1f);
        }
        // Sparse drifting embers add movement without heavy particle counts.
        var ash=new GameObject("Drifting ash and embers"); ash.transform.SetParent(root);
        var ps=ash.AddComponent<ParticleSystem>();
        var main=ps.main; main.loop=true; main.startLifetime=7f; main.startSpeed=.35f;
        main.startSize=new ParticleSystem.MinMaxCurve(.025f,.075f);
        main.startColor=new Color(1f,.42f,.13f,.72f); main.maxParticles=180;
        var emission=ps.emission; emission.rateOverTime=18f;
        var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(55f,8f,55f);
        var velocity=ps.velocityOverLifetime; velocity.enabled=true;
        velocity.x=new ParticleSystem.MinMaxCurve(-.18f,.18f);
        velocity.y=new ParticleSystem.MinMaxCurve(.08f,.35f);
        velocity.z=new ParticleSystem.MinMaxCurve(-.12f,.12f);
        ash.transform.position=new Vector3(0,2f,10f);
        var renderer=ash.GetComponent<ParticleSystemRenderer>();
        Material emberMaterial = AshMaterials.CreateSpriteMaterial();
        if (emberMaterial != null) renderer.sharedMaterial = emberMaterial;
    }
    void BuildHeroSilhouette(Transform hero,int kind)
    {
        Color metal=new Color(.19f,.21f,.23f);
        Color accent=kind==0?new Color(.55f,.12f,.08f):kind==1?new Color(.10f,.32f,.52f):kind==2?new Color(.12f,.34f,.22f):new Color(.32f,.19f,.40f);
        CreateHeroPart(hero,PrimitiveType.Cube,"Chest armor",new Vector3(0,.08f,0),new Vector3(.72f,.68f,.42f),metal);
        CreateHeroPart(hero,PrimitiveType.Sphere,"Shoulder left",new Vector3(-.39f,.30f,0),new Vector3(.28f,.28f,.30f),accent);
        CreateHeroPart(hero,PrimitiveType.Sphere,"Shoulder right",new Vector3(.39f,.30f,0),new Vector3(.28f,.28f,.30f),accent);
        CreateHeroPart(hero,PrimitiveType.Sphere,"Helmet",new Vector3(0,.67f,0),new Vector3(.43f,.43f,.40f),kind==1?accent:metal);
        CreateHeroPart(hero,PrimitiveType.Cube,"Waist guard",new Vector3(0,-.38f,0),new Vector3(.66f,.22f,.43f),accent);
        if(kind==0 || kind==2)
        {
            CreateHeroPart(hero,PrimitiveType.Cube,kind==0?"Battle axe haft":"Longbow",new Vector3(.57f,.05f,.05f),new Vector3(.10f,1.05f,.10f),new Color(.34f,.22f,.12f));
            if(kind==0) CreateHeroPart(hero,PrimitiveType.Cube,"Axe blade",new Vector3(.58f,.48f,.05f),new Vector3(.42f,.28f,.12f),new Color(.48f,.50f,.50f));
        }
        else
        {
            CreateHeroPart(hero,PrimitiveType.Cylinder,"Arcane staff",new Vector3(.56f,.02f,.05f),new Vector3(.09f,.72f,.09f),new Color(.30f,.20f,.13f));
            var orb=CreateHeroPart(hero,PrimitiveType.Sphere,"Glowing staff crystal",new Vector3(.56f,.48f,.05f),new Vector3(.22f,.22f,.22f),kind==1?new Color(.18f,.55f,1f):new Color(.58f,.25f,.82f));
            var glow=orb.AddComponent<Light>(); glow.type=LightType.Point; glow.range=2.5f; glow.intensity=.7f; glow.color=kind==1?new Color(.2f,.5f,1f):new Color(.55f,.2f,.8f);
        }
    }
    GameObject CreateHeroPart(Transform parent,PrimitiveType type,string label,Vector3 localPosition,Vector3 localScale,Color color)
    {
        var part=GameObject.CreatePrimitive(type); part.name=label; part.transform.SetParent(parent,false);
        part.transform.localPosition=localPosition; part.transform.localScale=localScale;
        var col=part.GetComponent<Collider>(); if(col!=null) Destroy(col);
        part.GetComponent<Renderer>().material=Mat(color,.72f);
        return part;
    }
    void CreateTree(Transform root,Vector3 pos,float scale)
    {
        var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="Pine trunk";trunk.transform.SetParent(root);trunk.transform.position=pos+Vector3.up*scale*.65f;trunk.transform.localScale=new Vector3(.22f,scale*.75f,.22f);trunk.GetComponent<Renderer>().material=Mat(new Color(.22f,.16f,.12f),1);
        for(int k=0;k<3;k++){var crown=CreateCone(root,pos+Vector3.up*(scale*(1.0f+k*.48f)),new Vector3(scale*(1.1f-k*.15f),scale*.8f,scale*(1.1f-k*.15f)));crown.GetComponent<Renderer>().material=Mat(new Color(.09f,.20f,.16f),.95f);}
    }
    GameObject CreateCone(Transform parent,Vector3 position,Vector3 scale)
    {
        var go=new GameObject("Conical mesh"); go.transform.SetParent(parent); go.transform.position=position; go.transform.localScale=scale;
        const int segments=16; var mesh=new Mesh(); var vertices=new Vector3[segments+2]; var triangles=new int[segments*6];
        vertices[0]=new Vector3(0,.5f,0); vertices[1]=new Vector3(0,-.5f,0);
        for(int i=0;i<segments;i++){float a=2*Mathf.PI*i/segments;vertices[i+2]=new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f);}
        for(int i=0;i<segments;i++){int n=(i+1)%segments;int t=i*6;triangles[t]=0;triangles[t+1]=i+2;triangles[t+2]=n+2;triangles[t+3]=1;triangles[t+4]=n+2;triangles[t+5]=i+2;}
        mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>(); return go;
    }
    Material Mat(Color c,float smooth)
    {
        string key = c.ToString() + "|" + smooth;
        Material cached;
        if (materialCache.TryGetValue(key, out cached) && cached != null) return cached;
        var m = AshMaterials.Create(c, 1f - smooth);
        materialCache[key] = m;
        return m;
    }
    void AddWorldHUD()
    {
        // Reuse the existing canvas. A second canvas left the loading overlay above the world.
        var hud=new GameObject("World HUD");hud.transform.SetParent(canvas.transform,false);
        var r=hud.AddComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;
        AddText(hud.transform,heroNames[selectedHero]+"  •  УРОВЕНЬ 1",23,new Vector2(.025f,.87f),new Vector2(.40f,.98f),new Color(.95f,.84f,.62f),TextAnchor.MiddleLeft,FontStyle.Bold);
        AddBar(hud.transform,"ЗДОРОВЬЕ",new Vector2(.025f,.83f),new Color(.65f,.12f,.09f));
        AddBar(hud.transform,"МАНА",new Vector2(.025f,.79f),new Color(.10f,.36f,.69f));
        var stick=AddPanel(hud.transform,new Vector2(.025f,.035f),new Vector2(.19f,.29f),new Color(.07f,.09f,.10f,.62f)); stick.name="Virtual Joystick"; stick.AddComponent<AshVirtualJoystick>(); var knob=AddPanel(stick.transform,new Vector2(.30f,.30f),new Vector2(.70f,.70f),new Color(.78f,.57f,.28f,.82f)); knob.name="Knob";
        AddText(hud.transform,"МИНИ-КАРТА",15,new Vector2(.82f,.80f),new Vector2(.97f,.96f),new Color(.92f,.80f,.57f),TextAnchor.MiddleCenter,FontStyle.Bold);
        AddCombatButton(hud.transform,"⚔",new Vector2(.86f,.04f),new Vector2(.97f,.20f)); AddCombatButton(hud.transform,"✦",new Vector2(.75f,.14f),new Vector2(.85f,.27f));
        AddText(hud.transform,"КОСТЁР  •  ПЕПЕЛЬНАЯ ДОЛИНА",15,new Vector2(.28f,.04f),new Vector2(.68f,.10f),new Color(.85f,.83f,.75f),TextAnchor.MiddleCenter,FontStyle.Normal);
    }
    void AddCombatButton(Transform parent,string label,Vector2 min,Vector2 max){var b=AddPanel(parent,min,max,new Color(.24f,.10f,.055f,.92f));b.AddComponent<Outline>().effectColor=new Color(.91f,.55f,.16f);AddText(b.transform,label,38,new Vector2(0,0),new Vector2(1,1),new Color(1f,.78f,.36f),TextAnchor.MiddleCenter,FontStyle.Bold);var button=b.AddComponent<Button>();button.targetGraphic=b.GetComponent<Image>();button.onClick.AddListener(()=>{AshCombatButton.AttackPressed=true;});}
    void AddBar(Transform parent,string label,Vector2 pos,Color color)
    {
        var b=AddPanel(parent,pos,new Vector2(pos.x+.25f,pos.y+.025f),new Color(.03f,.04f,.05f,.9f));
        AddPanel(b.transform,new Vector2(0,0),new Vector2(.72f,1),color);
        AddText(b.transform,label,13,new Vector2(0,0),new Vector2(1,1),Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
    }
}
