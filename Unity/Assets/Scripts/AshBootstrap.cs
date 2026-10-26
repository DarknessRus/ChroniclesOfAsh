using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>Bootstraps the first playable Unity prototype without requiring a hand-authored scene.</summary>
public class AshBootstrap : MonoBehaviour
{
    static AshBootstrap instance;
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
        if (FindObjectOfType<EventSystem>() == null)
        {
            var e = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(e);
        }
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
        menuRoot = new GameObject("Main Menu");
        menuRoot.transform.SetParent(canvas.transform, false);
        RectTransform full = menuRoot.AddComponent<RectTransform>();
        full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one; full.offsetMin = Vector2.zero; full.offsetMax = Vector2.zero;
        AddPanel(menuRoot.transform, new Vector2(0,0), new Vector2(1,1), new Color(0.035f,0.045f,0.052f,1));
        AddPanel(menuRoot.transform, new Vector2(0.02f,0.05f), new Vector2(0.98f,0.95f), new Color(0.07f,0.085f,0.09f,0.96f));
        AddText(menuRoot.transform, "ХРОНИКИ ПЕПЛА", 66, new Vector2(0.04f,0.78f), new Vector2(0.66f,0.94f), new Color(0.91f,0.67f,0.30f), TextAnchor.MiddleLeft, FontStyle.Bold);
        AddText(menuRoot.transform, "ПЕПЕЛ ПОМНИТ ИМЕНА", 22, new Vector2(0.045f,0.72f), new Vector2(0.65f,0.80f), new Color(0.68f,0.72f,0.70f), TextAnchor.MiddleLeft, FontStyle.Italic);

        for (int i=0;i<4;i++)
        {
            int index=i;
            float x0=0.045f+i*0.155f;
            var card=AddPanel(menuRoot.transform,new Vector2(x0,0.25f),new Vector2(x0+0.145f,0.69f),new Color(0.10f,0.12f,0.13f,1));
            AddPanel(card.transform,new Vector2(0.035f,0.30f),new Vector2(0.965f,0.96f),HeroColor(i));
            AddHeroSigil(card.transform,i);
            AddText(card.transform,heroNames[i],23,new Vector2(0.04f,0.12f),new Vector2(0.96f,0.29f),new Color(0.96f,0.91f,0.78f),TextAnchor.MiddleCenter,FontStyle.Bold);
            AddText(card.transform,heroRoles[i],15,new Vector2(0.02f,0.025f),new Vector2(0.98f,0.13f),new Color(0.75f,0.75f,0.71f),TextAnchor.MiddleCenter,FontStyle.Normal);
            var b=card.AddComponent<Button>();
            b.targetGraphic=card.GetComponent<Image>();
            b.onClick.AddListener(()=>SelectHero(index));
            var outline=card.AddComponent<Outline>();
            outline.effectColor=index==selectedHero?new Color(1f,.65f,.18f):new Color(.3f,.33f,.32f);
            outline.effectDistance=new Vector2(2,-2);
        }
        AddText(menuRoot.transform,"На краю погибшего королевства пробуждается древняя сила. Выбери путь — и измени судьбу пепельных земель.",20,new Vector2(0.69f,0.48f),new Vector2(0.96f,0.75f),new Color(0.78f,0.80f,0.77f),TextAnchor.MiddleLeft,FontStyle.Normal);
        AddText(menuRoot.transform,"ТВОЯ ЛЕГЕНДА НАЧИНАЕТСЯ ЗДЕСЬ",17,new Vector2(0.69f,0.39f),new Vector2(0.96f,0.48f),new Color(0.79f,0.56f,0.25f),TextAnchor.MiddleLeft,FontStyle.Bold);
        var start=AddPanel(menuRoot.transform,new Vector2(0.70f,0.16f),new Vector2(0.95f,0.29f),new Color(0.67f,0.32f,0.09f,1));
        AddText(start.transform,"НАЧАТЬ ПУТЬ  ▶",27,new Vector2(0,0),new Vector2(1,1),new Color(1f,.91f,.70f),TextAnchor.MiddleCenter,FontStyle.Bold);
        var startButton=start.AddComponent<Button>(); startButton.targetGraphic=start.GetComponent<Image>(); startButton.onClick.AddListener(StartGame);
        AddText(menuRoot.transform,"ОДИНОЧНОЕ ПРИКЛЮЧЕНИЕ  •  ТЁМНОЕ ФЭНТЕЗИ",14,new Vector2(0.04f,0.07f),new Vector2(0.62f,0.13f),new Color(.43f,.48f,.47f),TextAnchor.MiddleLeft,FontStyle.Normal);
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
        var t=g.GetComponent<Text>();t.text=value;t.font=font;t.fontSize=size;t.color=color;t.alignment=anchor;t.fontStyle=style;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
    }

    void StartGame()
    {
        if(canvas!=null)Destroy(canvas.gameObject);
        if(worldRoot!=null)Destroy(worldRoot);
        worldRoot=new GameObject("Ash Valley World");
        BuildWorld(worldRoot.transform);
        var player=GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name=heroNames[selectedHero]+" Player"; player.transform.position=new Vector3(0,1,0);
        player.GetComponent<Renderer>().material=Mat(new Color(.22f,.24f,.25f),.65f);
        var controller=player.AddComponent<CharacterController>(); controller.height=1.8f; controller.radius=.35f;
        player.AddComponent<AshPlayerController>();
        var cam=new GameObject("Follow Camera").AddComponent<Camera>();
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
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Mossy valley";ground.transform.SetParent(root);ground.transform.localScale=new Vector3(90,1,90);ground.GetComponent<Renderer>().material=Mat(new Color(.16f,.23f,.18f),.9f);
        // Layered mountain silhouettes form the first playable skyline.
        for(int i=0;i<14;i++){float x=(i-7)*7.2f;var m=GameObject.CreatePrimitive(PrimitiveType.Cube);m.name="Distant mountain";m.transform.SetParent(root);m.transform.position=new Vector3(x,5+Mathf.Sin(i*1.7f)*2,32+Mathf.Cos(i)*3);m.transform.rotation=Quaternion.Euler(0,i*19,0);m.transform.localScale=new Vector3(12,18+Mathf.Abs(Mathf.Sin(i*2))*14,10);m.GetComponent<Renderer>().material=Mat(new Color(.16f,.22f,.24f),1);}
        for(int i=0;i<70;i++){float x=Mathf.Sin(i*12.37f)*36;float z=Mathf.Cos(i*5.17f)*34+8;CreateTree(root,new Vector3(x,0,z),1+Mathf.Abs(Mathf.Sin(i))*1.8f);}
        // River and stone crossing.
        var river=GameObject.CreatePrimitive(PrimitiveType.Cube);river.name="Moon river";river.transform.SetParent(root);river.transform.position=new Vector3(0,.04f,7);river.transform.localScale=new Vector3(4,.08f,70);river.GetComponent<Renderer>().material=Mat(new Color(.08f,.28f,.35f),.25f);
        for(int i=0;i<7;i++){var stone=GameObject.CreatePrimitive(PrimitiveType.Cube);stone.name="Old stone bridge";stone.transform.SetParent(root);stone.transform.position=new Vector3(0,.18f,2+i*.85f);stone.transform.localScale=new Vector3(4.8f,.35f,.75f);stone.GetComponent<Renderer>().material=Mat(new Color(.34f,.34f,.31f),.95f);}
        // Distant gothic keep.
        var keep=GameObject.CreatePrimitive(PrimitiveType.Cube);keep.name="Ashen castle";keep.transform.SetParent(root);keep.transform.position=new Vector3(0,7,48);keep.transform.localScale=new Vector3(13,14,7);keep.GetComponent<Renderer>().material=Mat(new Color(.17f,.18f,.19f),.95f);
        for(int i=0;i<4;i++){var tower=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tower.transform.SetParent(root);tower.transform.position=new Vector3(-5+i*3.3f,12,48);tower.transform.localScale=new Vector3(1.4f,4,1.4f);tower.GetComponent<Renderer>().material=Mat(new Color(.13f,.14f,.16f),.95f);var roof=CreateCone(root,new Vector3(-5+i*3.3f,17,48),new Vector3(2,4,2));roof.GetComponent<Renderer>().material=Mat(new Color(.10f,.11f,.13f),.95f);}
        // Campfire and canvas tent.
        var fire=GameObject.CreatePrimitive(PrimitiveType.Sphere);fire.name="Campfire ember";fire.transform.SetParent(root);fire.transform.position=new Vector3(-5,.6f,-2);fire.transform.localScale=new Vector3(.5f,.9f,.5f);fire.GetComponent<Renderer>().material=Mat(new Color(1f,.28f,.035f),.35f);
        var flame=fire.AddComponent<Light>();flame.type=LightType.Point;flame.range=12;flame.intensity=3;flame.color=new Color(1f,.28f,.07f);
        var tent=GameObject.CreatePrimitive(PrimitiveType.Cube);tent.name="Traveller tent";tent.transform.SetParent(root);tent.transform.position=new Vector3(-8,1,-1);tent.transform.rotation=Quaternion.Euler(0,35,0);tent.transform.localScale=new Vector3(3,2,3);tent.GetComponent<Renderer>().material=Mat(new Color(.31f,.22f,.13f),1);
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
        var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",1f-smooth);return m;
    }
    void AddWorldHUD()
    {
        CreateCanvas();
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
