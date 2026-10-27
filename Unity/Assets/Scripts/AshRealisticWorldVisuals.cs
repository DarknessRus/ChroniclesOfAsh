using UnityEngine;

/// <summary>
/// Adds a cohesive, moody, higher-detail surface pass to the runtime-generated Ash valley.
/// Uses generated textures only, so Android builds do not depend on external downloads.
/// </summary>
public static class AshRealisticWorldVisuals
{
    static Texture2D groundTexture;
    static Texture2D waterTexture;

    public static void Apply(Transform root)
    {
        ApplyTerrainSurface(root);
        ApplyWaterSurface(root);
        ApplyAtmosphere();
        AddGroundDetail(root);
    }

    static void ApplyTerrainSurface(Transform root)
    {
        Transform terrain = root.Find("Open World - sculpted ash valley");
        if (terrain == null) return;
        Renderer renderer = terrain.GetComponent<Renderer>();
        if (renderer == null) return;

        if (groundTexture == null) groundTexture = MakeGroundTexture();
        Material mat = new Material(AshMaterials.Create(Color.white, .12f));
        if (mat.HasProperty("_MainTex"))
        {
            mat.mainTexture = groundTexture;
            mat.mainTextureScale = new Vector2(1.7f, 1.7f);
        }
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .08f);
        renderer.sharedMaterial = mat;
    }

    static Texture2D MakeGroundTexture()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true, true);
        tex.name = "Ash Valley - layered soil, moss and stone";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size, v = y / (float)size;
            float macro = Mathf.PerlinNoise(u * 4.5f + 3.1f, v * 4.5f + 8.7f);
            float soil = Mathf.PerlinNoise(u * 19f + 13f, v * 19f + 5f);
            float grit = Mathf.PerlinNoise(u * 67f + 4f, v * 67f + 17f);
            float grain = Mathf.PerlinNoise(u * 143f + 19f, v * 143f + 2f);
            float moss = Mathf.PerlinNoise(u * 11f + 41f, v * 11f + 29f);
            float crackNoise = Mathf.PerlinNoise(u * 31f + 9f, v * 31f + 33f);
            float cracks = Mathf.Pow(1f - Mathf.Abs(crackNoise - .5f) * 2f, 13f);
            float value = .50f + macro * .22f + soil * .14f + grit * .085f + grain * .045f - cracks * .12f;
            Color earth = new Color(value * .78f, value * .79f, value * .70f);
            Color mossColor = new Color(value * .54f, value * .70f, value * .53f);
            float blend = Mathf.Clamp01((moss - .57f) * 3.1f);
            pixels[y * size + x] = Color.Lerp(earth, mossColor, blend * .48f);
        }
        tex.SetPixels(pixels);
        tex.Apply(true, true);
        return tex;
    }

    static void ApplyWaterSurface(Transform root)
    {
        Transform river = root.Find("Winding river - cold reflective water");
        if (river == null) return;
        Renderer renderer = river.GetComponent<Renderer>();
        if (renderer == null) return;
        if (waterTexture == null) waterTexture = MakeWaterTexture();
        MeshFilter waterFilter = river.GetComponent<MeshFilter>();
        if (waterFilter != null && waterFilter.sharedMesh != null)
        {
            Mesh waterMesh = waterFilter.sharedMesh;
            Vector3[] vertices = waterMesh.vertices;
            Vector2[] uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                uv[i] = new Vector2(vertices[i].x * .12f, vertices[i].z * .06f);
            waterMesh.uv = uv;
        }
        Material mat = new Material(AshMaterials.Create(new Color(.48f,.70f,.73f), .82f));
        if (mat.HasProperty("_MainTex"))
        {
            mat.mainTexture = waterTexture;
            mat.mainTextureScale = new Vector2(3f, 28f);
        }
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .82f);
        renderer.sharedMaterial = mat;
    }

    static Texture2D MakeWaterTexture()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true, true);
        tex.name = "Ash Valley - cloudy water ripples";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size, v = y / (float)size;
            float broad = Mathf.PerlinNoise(u * 5f + 2f, v * 9f + 17f);
            float ripple = Mathf.PerlinNoise(u * 31f + 8f, v * 53f + 3f);
            float streak = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(ripple - .5f) * 2f), 5f);
            float c = .45f + broad * .23f + streak * .22f;
            pixels[y * size + x] = new Color(c * .55f, c * .78f, c * .83f);
        }
        tex.SetPixels(pixels);
        tex.Apply(true, true);
        return tex;
    }

    static void ApplyAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.105f, .125f, .135f, 1f);
        RenderSettings.fogDensity = .0065f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.31f, .34f, .36f);
        if (RenderSettings.sun != null)
        {
            RenderSettings.sun.color = new Color(.72f, .75f, .78f);
            RenderSettings.sun.intensity = .72f;
            RenderSettings.sun.transform.rotation = Quaternion.Euler(31f, -32f, 0f);
        }
    }

    static void AddGroundDetail(Transform root)
    {
        // Small clumps break up empty ground without blocking the player's route.
        var rng = new System.Random(78431);
        Material grassA = AshMaterials.Create(new Color(.16f,.20f,.13f), .05f);
        Material grassB = AshMaterials.Create(new Color(.23f,.24f,.15f), .05f);
        for (int i = 0; i < 520; i++)
        {
            float x = (float)(rng.NextDouble() * 210.0 - 105.0);
            float z = (float)(rng.NextDouble() * 210.0 - 60.0);
            if (Mathf.Abs(x) < 8f && z > 0f && z < 82f) continue;
            float riverX = Mathf.Sin((z - 45f) * .027f) * 15f + Mathf.Sin((z - 45f) * .071f) * 4f;
            if (Mathf.Abs(x - riverX) < 6.5f) continue;
            float n = Mathf.PerlinNoise((x + 400f) * .035f, (z + 400f) * .035f);
            float y = TerrainHeight(x, z);
            var clump = new GameObject("Wild grass and scrub");
            clump.transform.SetParent(root, false);
            clump.transform.position = new Vector3(x, y, z);
            clump.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            float s = .35f + (float)rng.NextDouble() * .65f;
            clump.transform.localScale = new Vector3(s, s, s);
            for (int blade = 0; blade < 3; blade++)
            {
                var mesh = new GameObject("Grass blade", typeof(MeshFilter), typeof(MeshRenderer));
                mesh.transform.SetParent(clump.transform, false);
                mesh.transform.localPosition = new Vector3((blade - 1) * .16f, .28f, (blade % 2) * .12f);
                mesh.transform.localRotation = Quaternion.Euler(0f, blade * 61f, (blade - 1) * 17f);
                mesh.transform.localScale = new Vector3(.12f, .58f + blade * .08f, .08f);
                mesh.GetComponent<MeshFilter>().sharedMesh = BladeMesh();
                mesh.GetComponent<MeshRenderer>().sharedMaterial = blade == 1 ? grassB : grassA;
            }
        }
    }

    static float TerrainHeight(float x, float worldZ)
    {
        float z = worldZ - 45f;
        float broad = Mathf.Sin(x * .034f) * 2.4f + Mathf.Cos(z * .031f) * 2.2f;
        float detail = (Mathf.PerlinNoise((x + 450f) * .035f, (z + 450f) * .035f) - .5f) * 2.6f
                     + (Mathf.PerlinNoise((x + 90f) * .095f, (z + 90f) * .095f) - .5f) * .75f;
        float ridgeNoise = Mathf.PerlinNoise((x + 350f) * .012f, (z + 350f) * .012f);
        float ridge = Mathf.Pow(Mathf.Clamp01((ridgeNoise - .43f) / .57f), 1.4f) * 13f;
        float riverX = Mathf.Sin(z * .027f) * 15f + Mathf.Sin(z * .071f) * 4f;
        float bank = Mathf.Clamp01((Mathf.Abs(x - riverX) - 4.2f) / 10f);
        return -.15f + Mathf.Lerp(-.08f, broad + detail + ridge, bank) + .035f;
    }

    static Mesh bladeMeshCache;
    static Mesh BladeMesh()
    {
        if (bladeMeshCache != null) return bladeMeshCache;
        var mesh = new Mesh { name = "Tapered wild grass blade" };
        mesh.vertices = new Vector3[] {
            new Vector3(-.5f,0,0), new Vector3(.5f,0,0), new Vector3(-.23f,.72f,0),
            new Vector3(.23f,.72f,0), new Vector3(0,1,0)
        };
        mesh.triangles = new int[] { 0,2,1, 1,2,3, 2,4,3 };
        mesh.RecalculateNormals();
        bladeMeshCache = mesh;
        return mesh;
    }
}