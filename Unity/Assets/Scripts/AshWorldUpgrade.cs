using UnityEngine;

/// <summary>Second environment pass: layered gothic keep, cliffs, battlements and emissive windows.</summary>
public static class AshWorldUpgrade
{
    static readonly System.Collections.Generic.Dictionary<string, Material> materialCache = new System.Collections.Generic.Dictionary<string, Material>();

    static Material Mat(Color color, float smooth=0.75f, bool glow=false)
    {
        string key = color.ToString() + "|" + smooth + "|" + glow;
        Material cached;
        if (materialCache.TryGetValue(key, out cached) && cached != null) return cached;
        var m = AshMaterials.Create(color, smooth, glow);
        materialCache[key] = m;
        return m;
    }
    static GameObject Part(Transform root,PrimitiveType type,string name,Vector3 p,Vector3 s,Color c,Vector3? rotation=null,bool glow=false,bool solid=false)
    {
        var g=GameObject.CreatePrimitive(type);
        g.name=name;g.transform.SetParent(root,false);g.transform.position=p;g.transform.localScale=s;
        if(rotation.HasValue)g.transform.rotation=Quaternion.Euler(rotation.Value);
        var r=g.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=Mat(c,.8f,glow);
        if(!solid){var collider=g.GetComponent<Collider>();if(collider!=null)Object.Destroy(collider);}
        return g;
    }

    static GameObject Spire(Transform root,Vector3 p,float height,Color color)
    {
        var g=new GameObject("Gothic spire");
        g.transform.SetParent(root,false);g.transform.position=p;g.transform.localScale=new Vector3(height*.46f,height,height*.46f);
        var mesh=new Mesh();const int n=12;
        var v=new Vector3[n+1];var tri=new int[n*3];
        v[0]=new Vector3(0,.5f,0);
        for(int i=0;i<n;i++)
        {
            float a=i*Mathf.PI*2/n;v[i+1]=new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f);
            tri[i*3]=0;tri[i*3+1]=i+1;tri[i*3+2]=(i+1)%n+1;
        }
        mesh.vertices=v;mesh.triangles=tri;mesh.RecalculateNormals();
        g.AddComponent<MeshFilter>().sharedMesh=mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial=Mat(color,.9f);
        return g;
    }

    public static void BuildCastleAndValley(Transform root)
    {
        Color stone=new Color(.12f,.14f,.16f);
        Color edge=new Color(.25f,.25f,.24f);
        Color ember=new Color(1f,.20f,.035f);
        Color cold=new Color(.08f,.48f,.8f);

        // Reinforced keep corners, layered buttresses and battlement teeth.
        for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
        {
            float x=sx*6.15f,z=48f+sz*3.35f;
            Part(root,PrimitiveType.Cube,"Castle corner buttress",new Vector3(x,6.5f,z),
                new Vector3(1.25f,13f,1.25f),edge,null,false,true);
            Part(root,PrimitiveType.Cube,"Buttress cap",new Vector3(x,13.1f,z),
                new Vector3(1.7f,.55f,1.7f),stone);
            Part(root,PrimitiveType.Cylinder,"High watchtower",new Vector3(x,15.3f,z),
                new Vector3(2.2f,4.5f,2.2f),stone,null,false,true);
            Spire(root,new Vector3(x,19.4f,z),5.5f,new Color(.055f,.065f,.08f));
            Part(root,PrimitiveType.Cylinder,"Spire ring",new Vector3(x,17.4f,z),
                new Vector3(2.35f,.22f,2.35f),edge);
            for(int k=0;k<4;k++)
                Part(root,PrimitiveType.Cube,"Tower crenellation",new Vector3(x+Mathf.Cos(k*Mathf.PI/2)*.8f,18.2f,z+Mathf.Sin(k*Mathf.PI/2)*.8f),
                    new Vector3(.38f,.85f,.38f),edge);
        }

        // Repeated narrow windows create a readable vertical rhythm on the fortress face.
        for(int floor=0;floor<4;floor++)for(int col=-3;col<=3;col++)
        {
            float x=col*1.65f;
            float y=2.2f+floor*3.0f;
            Part(root,PrimitiveType.Cube,"Castle lancet window",new Vector3(x,y,44.42f),
                new Vector3(.36f,1.15f,.10f),floor%2==0?ember:cold,null,true);
            Part(root,PrimitiveType.Cube,"Window stone surround",new Vector3(x,y,44.34f),
                new Vector3(.56f,1.38f,.13f),edge);
            Part(root,PrimitiveType.Cube,"Window inset",new Vector3(x,y,44.24f),
                new Vector3(.30f,1.02f,.035f),floor%2==0?ember:cold,null,true);
        }

        // Great entrance with layered arch, gate bars and a burning sigil.
        Part(root,PrimitiveType.Cube,"Keep gate left pier",new Vector3(-2.1f,2.5f,43.85f),
            new Vector3(.8f,5f,.75f),edge);
        Part(root,PrimitiveType.Cube,"Keep gate right pier",new Vector3(2.1f,2.5f,43.85f),
            new Vector3(.8f,5f,.75f),edge);
        Part(root,PrimitiveType.Cube,"Keep gate lintel",new Vector3(0,5.05f,43.85f),
            new Vector3(4.8f,.65f,.9f),edge);
        Part(root,PrimitiveType.Cube,"Gate shadow",new Vector3(0,2.15f,44.15f),
            new Vector3(3.35f,4.25f,.16f),new Color(.025f,.025f,.03f));
        for(int i=0;i<7;i++)
            Part(root,PrimitiveType.Cube,"Gate iron bar",new Vector3(-1.35f+i*.45f,2.1f,44.02f),
                new Vector3(.07f,4.1f,.09f),new Color(.28f,.25f,.22f));
        Part(root,PrimitiveType.Cube,"Ancient gate rune",new Vector3(0,4.35f,43.94f),
            new Vector3(.38f,.58f,.08f),ember,null,true);

        // Cliffs form a valley rim; their staggered rock faces frame the playable route.
        for(int side=-1;side<=1;side+=2)
        for(int i=0;i<12;i++)
        {
            float z=-5+i*4.9f;
            float x=side*(24f+(i%3)*2.1f);
            float h=5f+(i%4)*2.8f;
            Part(root,PrimitiveType.Cube,"Layered valley cliff",new Vector3(x,h*.48f,z),
                new Vector3(5.5f,h,6.5f),i%3==0?new Color(.15f,.18f,.18f):stone,
                new Vector3((i%2)*5f,side*(i*11f),side*(i%3==0?7f:-4f)));
            if(i%2==0)
                Part(root,PrimitiveType.Cube,"Cliff ledge",new Vector3(x-side*1.2f,h*.83f,z),
                    new Vector3(6.8f,.55f,7.1f),edge,new Vector3(0,side*i*9f,side*3f));
        }

        // Stone parapets and orange lanterns along the approach.
        for(int side=-1;side<=1;side+=2)for(int i=0;i<8;i++)
        {
            float z=4+i*2.4f;
            Part(root,PrimitiveType.Cube,"Bridge parapet",new Vector3(side*2.75f,.62f,z),
                new Vector3(.35f,.95f,2.25f),edge);
            if(i%2==0)
            {
                Part(root,PrimitiveType.Cylinder,"Roadside brazier stem",new Vector3(side*3.2f,1.05f,z),
                    new Vector3(.13f,.9f,.13f),new Color(.22f,.16f,.11f));
                Part(root,PrimitiveType.Sphere,"Roadside ember",new Vector3(side*3.2f,1.58f,z),
                    new Vector3(.28f,.38f,.28f),ember,null,true);
            }
        }

        // A few distant ruined towers increase skyline depth without excessive draw calls.
        for(int i=0;i<7;i++)
        {
            float x=(i%2==0?-1:1)*(17f+(i%3)*5f);
            float z=27f+i*4.8f;
            float h=5f+(i%3)*2.5f;
            Part(root,PrimitiveType.Cube,"Valley watchtower ruin",new Vector3(x,h*.5f,z),
                new Vector3(2.4f,h,2.4f),stone,null,false,true);
            Spire(root,new Vector3(x,h+1.3f,z),2.6f,new Color(.06f,.07f,.085f));
            Part(root,PrimitiveType.Cube,"Tower fire slit",new Vector3(x,h*.55f,z-1.23f),
                new Vector3(.22f,.75f,.04f),i%2==0?ember:cold,null,true);
        }
    }

    /// <summary>Expands the valley into a genuinely explorable landscape with ridges, forest, and a winding river.</summary>
    public static void BuildOpenWorld(Transform root)
    {
        Color darkRock = new Color(.105f,.14f,.16f);
        Color ground = new Color(.13f,.18f,.15f);
        Color water = new Color(.035f,.19f,.25f);
        Color pine = new Color(.055f,.13f,.105f);
        Color pineLight = new Color(.09f,.19f,.145f);

        // Rolling foothills in several layers, extending well beyond the starting clearing.
        for (int i=0;i<44;i++)
        {
            float a=i*2.39996f, radius=48f+(i%5)*8f;
            float x=Mathf.Cos(a)*radius, z=Mathf.Sin(a)*radius+18f;
            float h=10f+(i%7)*3.2f;
            var hill=Part(root,PrimitiveType.Sphere,"Open-world rolling foothill",new Vector3(x,h*.12f,z),
                new Vector3(19f+(i%3)*5f,h,23f+(i%4)*4f),i%4==0?darkRock:ground,new Vector3((i%3)*4f,i*17f,(i%5)-2f));
        }

        // A broken mountain range with overlapping peaks at different depths.
        for (int i=0;i<25;i++)
        {
            float x=-120f+i*10f, z=88f+Mathf.Sin(i*.73f)*14f;
            float h=28f+Mathf.Abs(Mathf.Sin(i*1.91f))*34f;
            Part(root,PrimitiveType.Cube,"Distant jagged mountain",new Vector3(x,h*.43f,z),
                new Vector3(17f+(i%3)*4f,h,20f+(i%4)*3f),darkRock,new Vector3(0,i*23f,(i%2==0?-1f:1f)*8f));
            Part(root,PrimitiveType.Cube,"Mountain broken crown",new Vector3(x,h+5f,z),
                new Vector3(10f+(i%3)*3f,12f+(i%4)*4f,12f),new Color(.16f,.18f,.19f),new Vector3(0,i*31f,12f));
        }

        // Curving river segments; the crossing stones make a visible route through the forest.
        for (int i=0;i<46;i++)
        {
            float z=-30f+i*3.2f;
            float x=15f+Mathf.Sin(i*.18f)*9f;
            float nextX=15f+Mathf.Sin((i+1)*.18f)*9f;
            var segment=Part(root,PrimitiveType.Cube,"Winding moon river",new Vector3(x,.10f,z),
                new Vector3(5f,.12f,4.1f),water,new Vector3(0,Mathf.Atan2(nextX-x,3.2f)*Mathf.Rad2Deg,0));
        }

        // A broad forest beyond the starting camp, with a clear corridor toward the castle.
        Random.State previous=Random.state;
        Random.InitState(617204);
        int planted=0, attempts=0;
        while(planted<180 && attempts<1200)
        {
            attempts++;
            float x=Random.Range(-88f,88f), z=Random.Range(-38f,104f);
            if(Mathf.Abs(x)<8f && z<28f) continue;
            float riverX=15f+Mathf.Sin(((z+30f)/3.2f)*.18f)*9f;
            if(Mathf.Abs(x-riverX)<6f) continue;
            float s=Random.Range(1.6f,3.6f);
            Part(root,PrimitiveType.Cylinder,"Forest pine trunk",new Vector3(x,s*.58f,z),
                new Vector3(.20f,s*.7f,.20f),new Color(.20f,.13f,.09f));
            for(int tier=0;tier<3;tier++)
                Part(root,PrimitiveType.Sphere,"Pine canopy",new Vector3(x,s*(.8f+tier*.40f),z),
                    new Vector3(s*(1.1f-tier*.18f),s*.75f,s*(1.1f-tier*.18f)),
                    tier==0?pine:pineLight);
            planted++;
        }
        Random.state=previous;

        // Landmark boulders and stepping stones deepen the landscape and mark a river crossing.
        for(int i=0;i<58;i++)
        {
            float a=i*2.39996f, r=12f+(i%9)*7.3f;
            float s=.7f+(i%4)*.28f;
            Part(root,PrimitiveType.Sphere,"Weathered valley boulder",
                new Vector3(Mathf.Cos(a)*r,.35f,Mathf.Sin(a)*r+12f),
                new Vector3(s*1.5f,s,s*.9f),new Color(.19f,.21f,.22f),
                new Vector3(i*11f,i*37f,i*17f));
        }
        for(int i=0;i<11;i++)
        {
            float z=24f+i*1.1f, x=15f+Mathf.Sin(((z+30f)/3.2f)*.18f)*9f;
            Part(root,PrimitiveType.Cube,"River crossing stone",new Vector3(x,.24f,z),
                new Vector3(5.5f,.38f,.9f),new Color(.31f,.31f,.29f),null,false,true);
        }
    }

}
