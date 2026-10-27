using UnityEngine;

/// <summary>
/// High-resolution, multi-scale terrain surfaces for the mobile Ash valley.
/// Generates tiled 2K soil/stone albedo and a matching detail normal map locally,
/// avoiding external downloads while adding fine-scale surface relief.
/// </summary>
public static class AshRealisticWorldVisuals
{
    const int GroundSize = 2048;
    const int NormalSize = 1024;
    static Texture2D groundTexture;
    static Texture2D groundNormal;
    static Texture2D groundRoughness;
    static Texture2D groundSmoothness;
    static Texture2D rockTexture;
    static Texture2D rockNormal;
    static Texture2D rockRoughness;
    static Texture2D rockSmoothness;
    static Texture2D rockHeight;
    static Texture2D barkTexture;
    static Texture2D barkNormal;
    static Texture2D barkRoughness;
    static Texture2D barkSmoothness;
    static Texture2D waterTexture;
    static Mesh bladeMeshCache;

    public static void Apply(Transform root)
    {
        ApplyTerrainSurface(root);
        ApplyRockSurfaces(root);
        ApplyBarkSurfaces(root);
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
        groundTexture = Resources.Load<Texture2D>("PBR/GrassGround/grass_ground_diff_1k");
        groundNormal = Resources.Load<Texture2D>("PBR/GrassGround/grass_ground_nor_dx_1k");
        groundRoughness = Resources.Load<Texture2D>("PBR/GrassGround/grass_ground_rough_1k");
        if (groundTexture == null) groundTexture = MakeGroundTexture();
        if (groundNormal == null) groundNormal = MakeGroundNormal();
        if (groundRoughness != null && groundSmoothness == null)
            groundSmoothness = MakeSmoothnessMap(groundRoughness, "Ash Ground - smoothness from CC0 roughness");
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = renderer.sharedMaterial.shader;
        renderer.sharedMaterial = CreatePbrMaterial("Ash Valley - scanned ground PBR", shader,
            groundTexture, groundNormal, groundSmoothness, new Color(.72f,.73f,.69f), new Vector2(4.8f,4.8f));
    }

    // 2K seamless albedo: layered damp earth, shale chips, grit, tiny roots and moss.
    static Texture2D MakeGroundTexture()
    {
        int size = GroundSize;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true, false);
        tex.name = "Ash Valley 2048 - soil shale moss and mineral grit";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 16;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float v = y / (float)size;
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float macro = Mathf.PerlinNoise(u * 5.2f + 3.1f, v * 5.2f + 8.7f);
                float soil = Mathf.PerlinNoise(u * 21f + 13f, v * 21f + 5f);
                float stone = Mathf.PerlinNoise(u * 59f + 41f, v * 59f + 17f);
                float grain = Mathf.PerlinNoise(u * 137f + 19f, v * 137f + 2f);
                // Deterministic fine mineral grains avoid another expensive noise lookup.
                float hash = Mathf.Abs(Mathf.Sin((x * 127.1f + y * 311.7f) * .0174533f) * 43758.5453f);
                hash = hash - Mathf.Floor(hash);
                float hash2 = Mathf.Abs(Mathf.Sin((x * 269.5f + y * 183.3f) * .01357f) * 24634.6345f);
                hash2 = hash2 - Mathf.Floor(hash2);
                float crackField = Mathf.PerlinNoise(u * 34f + 9f, v * 34f + 33f);
                float cracks = Mathf.Pow(1f - Mathf.Abs(crackField - .5f) * 2f, 17f);
                float value = .29f + macro * .20f + soil * .17f + stone * .13f +
                              grain * .075f + hash * .075f + hash2 * .035f - cracks * .095f;
                value = Mathf.Clamp(value, .12f, .78f);
                // Cool charcoal shale mixed with muted brown soil.
                Color earth = new Color(value * .78f, value * .79f, value * .75f);
                float mossNoise = Mathf.PerlinNoise(u * 12f + 41f, v * 12f + 29f);
                float mossMask = Mathf.SmoothStep(.59f, .76f, mossNoise) * Mathf.Clamp01((soil + macro) * .8f);
                Color moss = new Color(value * .48f, value * .61f, value * .43f);
                Color result = Color.Lerp(earth, moss, mossMask * .62f);
                // Scattered pale mineral chips break up broad smooth patches.
                float fleck = Mathf.Pow(hash2, 22f) * Mathf.Clamp01(stone * 1.6f);
                result += new Color(fleck * .19f, fleck * .18f, fleck * .15f);
                pixels[y * size + x] = result;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(true, true);
        return tex;
    }

    // Tangent-space normal map generated from the same layered micro-relief.
    static Texture2D MakeGroundNormal()
    {
        int size = NormalSize;
        var heights = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            float v = y / (float)size;
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float broad = Mathf.PerlinNoise(u * 17f + 4f, v * 17f + 8f);
                float grit = Mathf.PerlinNoise(u * 73f + 29f, v * 73f + 13f);
                float fine = Mathf.PerlinNoise(u * 191f + 11f, v * 191f + 51f);
                heights[y * size + x] = broad * .48f + grit * .34f + fine * .18f;
            }
        }
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true, true);
        tex.name = "Ash Valley 1024 - soil and shale normal detail";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int left = y * size + (x == 0 ? size - 1 : x - 1);
            int right = y * size + (x + 1 == size ? 0 : x + 1);
            int down = (y == 0 ? size - 1 : y - 1) * size + x;
            int up = (y + 1 == size ? 0 : y + 1) * size + x;
            float dx = (heights[left] - heights[right]) * 2.1f;
            float dy = (heights[down] - heights[up]) * 2.1f;
            Vector3 n = new Vector3(dx, dy, 1f).normalized;
            pixels[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f);
        }
        tex.SetPixels(pixels);
        tex.Apply(true, true);
        return tex;
    }

    static void ApplyRockSurfaces(Transform root)
    {
        rockTexture = Resources.Load<Texture2D>("PBR/RockGround/rocks_ground_02_diff_1k");
        rockNormal = Resources.Load<Texture2D>("PBR/RockGround/rocks_ground_02_nor_dx_1k");
        rockRoughness = Resources.Load<Texture2D>("PBR/RockGround/rocks_ground_02_rough_1k");
        if (rockRoughness != null && rockSmoothness == null)
            rockSmoothness = MakeSmoothnessMap(rockRoughness, "Ash Rock - smoothness from CC0 roughness");
        if (rockHeight == null) rockHeight = Resources.Load<Texture2D>("PBR/RockGround/rocks_ground_02_disp_1k");
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.name != "Jagged mountain cliff" && item.name != "Weathered boulder" &&
                item.name != "Layered valley cliff" && item.name != "Distant jagged mountain" &&
                item.name != "Mountain broken crown") continue;
            Renderer r = item.GetComponent<Renderer>();
            if (r == null || r.sharedMaterial == null) continue;
            if (rockTexture != null && rockNormal != null)
            {
                float scale = Mathf.Max(item.lossyScale.x, item.lossyScale.z);
                float tiling = Mathf.Clamp(scale * .42f, 1.2f, 8f);
                Color tint = item.name == "Weathered boulder" ? new Color(.76f,.77f,.75f) : new Color(.66f,.68f,.70f);
                r.sharedMaterial = CreatePbrMaterial("Ash Cliffs - scanned rock PBR", r.sharedMaterial.shader,
                    rockTexture, rockNormal, rockSmoothness, tint, new Vector2(tiling,tiling));
            }
            else
            {
                Material m = r.sharedMaterial;
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .08f);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .08f);
            }
        }
    }

    static void ApplyBarkSurfaces(Transform root)
    {
        barkTexture = Resources.Load<Texture2D>("PBR/Bark/bark_brown_02_diff_2k");
        barkNormal = Resources.Load<Texture2D>("PBR/Bark/bark_brown_02_nor_dx_2k");
        barkRoughness = Resources.Load<Texture2D>("PBR/Bark/bark_brown_02_rough_2k");
        if (barkRoughness != null && barkSmoothness == null)
            barkSmoothness = MakeSmoothnessMap(barkRoughness, "Ash Pine Bark - smoothness from CC0 roughness");

        if (barkTexture == null || barkNormal == null)
        {
            Debug.LogError("[Ash Chronicles] Required 2K pine bark texture maps are missing from Resources/PBR/Bark.");
            return;
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name != "Pine trunk") continue;
                Renderer existing = item.GetComponent<Renderer>();
                if (existing != null && existing.sharedMaterial != null) { shader = existing.sharedMaterial.shader; break; }
            }
        }
        if (shader == null)
        {
            Debug.LogError("[Ash Chronicles] No compatible shader found for scanned pine bark.");
            return;
        }

        Material barkMaterial = CreatePbrMaterial("Ash Pines - 2K scanned bark PBR", shader,
            barkTexture, barkNormal, barkSmoothness, new Color(.78f,.72f,.64f), new Vector2(1.8f, 2.2f));
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.name != "Pine trunk") continue;
            Renderer renderer = item.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = barkMaterial;
        }
    }

    static Material CreatePbrMaterial(string materialName, Shader shader, Texture2D albedo,
        Texture2D normal, Texture2D smoothness, Color tint, Vector2 tiling)
    {
        Material mat = new Material(shader);
        mat.name = materialName;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_MainTex") && albedo != null)
        {
            mat.SetTexture("_MainTex", albedo);
            mat.SetTextureScale("_MainTex", tiling);
        }
        if (mat.HasProperty("_BaseMap") && albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", tiling);
        }
        if (normal != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", .78f);
        }
        if (smoothness != null && mat.HasProperty("_MetallicGlossMap"))
        {
            mat.SetTexture("_MetallicGlossMap", smoothness);
            mat.EnableKeyword("_METALLICGLOSSMAP");
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 1f);
            if (mat.HasProperty("_SmoothnessTextureChannel")) mat.SetFloat("_SmoothnessTextureChannel", 0f);
        }
        else
        {
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .16f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .16f);
        }
        return mat;
    }

    static Texture2D MakeSmoothnessMap(Texture2D roughness, string textureName)
    {
        if (roughness == null) return null;
        try
        {
            Color32[] pixels = roughness.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(0, 0, 0, (byte)(255 - pixels[i].r));
            Texture2D result = new Texture2D(roughness.width, roughness.height, TextureFormat.RGBA32, true, true);
            result.name = textureName;
            result.wrapMode = TextureWrapMode.Repeat;
            result.filterMode = FilterMode.Trilinear;
            result.anisoLevel = 8;
            result.SetPixels32(pixels);
            result.Apply(true, true);
            return result;
        }
        catch { return null; }
    }

    public static float SampleRockHeight(float u, float v)
    {
        if (rockHeight == null) rockHeight = Resources.Load<Texture2D>("PBR/RockGround/rocks_ground_02_disp_1k");
        if (rockHeight != null)
        {
            try { return rockHeight.GetPixelBilinear(Mathf.Repeat(u, 1f), Mathf.Clamp01(v)).grayscale; }
            catch { }
        }
        return Mathf.PerlinNoise(u * 11.7f + 2.3f, v * 13.1f + 7.9f);
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
            Vector2[] uv = new Vector2[waterMesh.vertexCount];
            Vector3[] vertices = waterMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                uv[i] = new Vector2(vertices[i].x * .12f, vertices[i].z * .06f);
            waterMesh.uv = uv;
        }
        Material mat = new Material(AshMaterials.Create(new Color(.48f,.70f,.73f), .82f));
        if (mat.HasProperty("_MainTex")) mat.mainTexture = waterTexture;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .82f);
        renderer.sharedMaterial = mat;
    }

    static Texture2D MakeWaterTexture()
    {
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true, false);
        tex.name = "Ash Valley - cold water micro-ripples";
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = x / (float)size, v = y / (float)size;
            float broad = Mathf.PerlinNoise(u * 7f + 2f, v * 11f + 17f);
            float ripple = Mathf.PerlinNoise(u * 47f + 8f, v * 89f + 3f);
            float fine = Mathf.PerlinNoise(u * 131f + 14f, v * 173f + 29f);
            float streak = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(ripple - .5f) * 2f), 6f);
            float c = .25f + broad * .16f + streak * .24f + fine * .12f;
            pixels[y * size + x] = new Color(c * .36f, c * .65f, c * .76f);
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
        var rng = new System.Random(78431);
        Material grassA = AshMaterials.Create(new Color(.16f,.20f,.13f), .05f);
        Material grassB = AshMaterials.Create(new Color(.23f,.24f,.15f), .05f);
        Mesh bladeMesh = BladeMesh();
        for (int i = 0; i < 700; i++)
        {
            float x = (float)(rng.NextDouble() * 210.0 - 105.0);
            float z = (float)(rng.NextDouble() * 210.0 - 60.0);
            if (Mathf.Abs(x) < 8f && z > 0f && z < 82f) continue;
            float rx = Mathf.Sin((z - 45f) * .027f) * 15f + Mathf.Sin((z - 45f) * .071f) * 4f;
            if (Mathf.Abs(x - rx) < 6.5f) continue;
            float y = TerrainHeight(x, z);
            var clump = new GameObject("Wild grass and scrub");
            clump.transform.SetParent(root, false);
            clump.transform.position = new Vector3(x, y, z);
            clump.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            float s = .35f + (float)rng.NextDouble() * .65f;
            clump.transform.localScale = new Vector3(s, s, s);
            for (int blade = 0; blade < 3; blade++)
            {
                var go = new GameObject("Grass blade", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(clump.transform, false);
                go.transform.localPosition = new Vector3((blade - 1) * .16f, .28f, (blade % 2) * .12f);
                go.transform.localRotation = Quaternion.Euler(0f, blade * 61f, (blade - 1) * 17f);
                go.transform.localScale = new Vector3(.12f, .58f + blade * .08f, .08f);
                go.GetComponent<MeshFilter>().sharedMesh = bladeMesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = blade == 1 ? grassB : grassA;
            }
        }
    }

    static float TerrainHeight(float x, float worldZ)
    {
        // Keep decorative grass anchored to the exact same height field as the
        // collidable terrain mesh in AshOpenWorld.BuildTerrain.
        float z = worldZ - 45f;
        float macro = Mathf.Sin(x * .021f + Mathf.Sin(z * .013f) * .8f) * 5.2f
                    + Mathf.Cos(z * .024f + x * .009f) * 4.4f;
        float rolling = (Mathf.PerlinNoise((x + 450f) * .018f, (z + 450f) * .018f) - .5f) * 9.5f;
        float erosion = (Mathf.PerlinNoise((x + 90f) * .062f, (z + 90f) * .062f) - .5f) * 2.4f
                      + (Mathf.PerlinNoise((x - 170f) * .15f, (z + 270f) * .15f) - .5f) * .75f;
        float n = Mathf.PerlinNoise((x + 350f) * .010f, (z + 350f) * .010f);
        float ridgeNoise = 1f - Mathf.Abs(n * 2f - 1f);
        float ridge = Mathf.Pow(Mathf.Clamp01((ridgeNoise - .48f) / .52f), 2.0f) * 24f;
        float riverX = Mathf.Sin(z * .027f) * 15f + Mathf.Sin(z * .071f) * 4f;
        float distance = Mathf.Abs(x - riverX);
        float valleyShoulder = Mathf.Clamp01((distance - 10f) / 43f);
        float riverBank = Mathf.Clamp01((distance - 3.6f) / 8.5f);
        float upland = macro + rolling + erosion + ridge * valleyShoulder;
        float localY = Mathf.Lerp(-.08f, upland, riverBank);
        localY -= Mathf.Exp(-Mathf.Pow(distance / 31f, 2f)) * 2.2f;
        return -.15f + localY;
    }

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