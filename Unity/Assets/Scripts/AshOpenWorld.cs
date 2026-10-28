using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime-built dark fantasy valley. Landscape geometry has colliders so the player
/// cannot walk through terrain, cliffs, boulders, trunks or the solid parts of trees.
/// All geometry is generated locally; no external assets or editor setup are required.
/// </summary>
public static class AshOpenWorld
{
    static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    static Mesh pineCrownMesh;
    const int Grid = 320;
    const float WorldSize = 280f;

    public static void Build(Transform root)
    {
        BuildTerrain(root);
        BuildRiver(root);
        BuildMountainRanges(root);
        BuildRockFields(root);
        BuildForests(root);
        AshEnvironmentModelScatter.Build(root);
        BuildTrail(root);
        AshReferenceLandscapeDetails.Build(root);
        // Apply the presentation layer first, then assign scanned PBR surfaces last so
        // later world styling cannot replace the ground/rock/bark materials.
        AshDarkFantasyPresentation.Apply(root);
        AshRealisticWorldVisuals.Apply(root);
    }

    static void BuildTerrain(Transform root)
    {
        var terrain = new GameObject("Open World - sculpted ash valley");
        terrain.transform.SetParent(root, false);
        terrain.transform.position = new Vector3(0, -0.15f, 45f);
        var mesh = new Mesh { name = "Sculpted terrain mesh" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        var vertices = new Vector3[(Grid + 1) * (Grid + 1)];
        var uv = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var triangles = new int[Grid * Grid * 6];
        for (int z = 0; z <= Grid; z++)
        for (int x = 0; x <= Grid; x++)
        {
            float wx = ((float)x / Grid - .5f) * WorldSize;
            float wz = ((float)z / Grid - .5f) * WorldSize;
            // Layered macro terrain: broad valley shoulders, broken ridgelines and
            // smaller erosion detail. Keep the river corridor low and gently sloped.
            float macro = Mathf.Sin(wx * .021f + Mathf.Sin(wz * .013f) * .8f) * 5.2f
                        + Mathf.Cos(wz * .024f + wx * .009f) * 4.4f;
            float rolling = (Mathf.PerlinNoise((wx + 450f) * .018f, (wz + 450f) * .018f) - .5f) * 9.5f;
            float erosion = (Mathf.PerlinNoise((wx + 90f) * .062f, (wz + 90f) * .062f) - .5f) * 2.4f
                          + (Mathf.PerlinNoise((wx - 170f) * .15f, (wz + 270f) * .15f) - .5f) * .75f;
            // Ridged noise produces broken angular uplands rather than a smooth noise dome.
            float n = Mathf.PerlinNoise((wx + 350f) * .010f, (wz + 350f) * .010f);
            float ridgeNoise = 1f - Mathf.Abs(n * 2f - 1f);
            float ridge = Mathf.Pow(Mathf.Clamp01((ridgeNoise - .48f) / .52f), 2.0f) * 24f;
            // A shallow central valley with steeper shoulders; the river follows its floor.
            float valleyAxis = RiverX(wz);
            float valleyDistance = Mathf.Abs(wx - valleyAxis);
            float valleyShoulder = Mathf.Clamp01((valleyDistance - 10f) / 43f);
            float riverDistance = Mathf.Abs(wx - valleyAxis);
            float riverBank = Mathf.Clamp01((riverDistance - 3.6f) / 8.5f);
            float upland = macro + rolling + erosion + ridge * valleyShoulder;
            float y = Mathf.Lerp(-.08f, upland, riverBank);
            // Pull the broad valley floor down smoothly beyond the river banks.
            y -= Mathf.Exp(-Mathf.Pow(valleyDistance / 31f, 2f)) * 2.2f;
            int index = z * (Grid + 1) + x;
            vertices[index] = new Vector3(wx, y, wz);
            // Repeat the scanned ground texture at a realistic scale. 18 repeats across the whole valley made 2K PBR detail look like broad, nearly flat color patches on mobile; 96 repeats bring soil grain and small stones into view at player distance.
            uv[index] = new Vector2((float)x / Grid * 96f, (float)z / Grid * 96f);
            float tint = Mathf.PerlinNoise(wx * .075f + 12f, wz * .075f + 8f);
            float damp = Mathf.PerlinNoise(wx * .12f + 70f, wz * .12f + 20f);
            Color ground = Color.Lerp(new Color(.075f,.095f,.085f), new Color(.25f,.24f,.19f), tint);
            colors[index] = Color.white;
            if (x < Grid && z < Grid)
            {
                int t = (z * Grid + x) * 6, a = index, b = index + Grid + 1;
                triangles[t] = a; triangles[t+1] = b; triangles[t+2] = a+1;
                triangles[t+3] = a+1; triangles[t+4] = b; triangles[t+5] = b+1;
            }
        }
        mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles;
        mesh.RecalculateNormals();
        // Explicitly calculate bounds after assigning the generated 103k-vertex mesh.
        // This prevents renderer/frustum culling from treating the terrain as a zero-sized mesh
        // on devices/build configurations where bounds are not refreshed by the vertex setter.
        mesh.RecalculateBounds();
        var filter = terrain.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var terrainRenderer = terrain.AddComponent<MeshRenderer>();
        terrainRenderer.enabled = true;
        terrainRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        terrainRenderer.receiveShadows = true;

        // Use an explicit URP Lit material and disable back-face culling on this generated
        // landscape. This makes the ground visible even if a mobile graphics backend treats
        // procedural triangle winding differently, while the collider still uses the same mesh.
        Shader terrainShader = Shader.Find("Universal Render Pipeline/Lit");
        if (terrainShader == null) terrainShader = Shader.Find("Standard");
        if (terrainShader == null)
            throw new System.InvalidOperationException("No compatible shader found for the open-world terrain.");
        var terrainMaterial = new Material(terrainShader);
        terrainMaterial.name = "Ash Valley - visible double-sided terrain";
        Color terrainTint = new Color(.72f, .73f, .69f, 1f);
        if (terrainMaterial.HasProperty("_BaseColor")) terrainMaterial.SetColor("_BaseColor", terrainTint);
        if (terrainMaterial.HasProperty("_Color")) terrainMaterial.SetColor("_Color", terrainTint);
        if (terrainMaterial.HasProperty("_Cull")) terrainMaterial.SetFloat("_Cull", 0f);
        if (terrainMaterial.HasProperty("_Surface")) terrainMaterial.SetFloat("_Surface", 0f);
        terrainRenderer.sharedMaterial = terrainMaterial;

        var terrainCollider = terrain.AddComponent<MeshCollider>();
        terrainCollider.sharedMesh = mesh;
        terrainCollider.enabled = true;
        Debug.Log("[Ash Chronicles] Terrain mesh ready: vertices=" + vertices.Length +
            ", triangles=" + (triangles.Length / 3) + ", bounds=" + mesh.bounds +
            ", renderer=" + terrainRenderer.enabled + ", shader=" + terrainShader.name +
            ", doubleSided=" + (terrainMaterial.HasProperty("_Cull") && terrainMaterial.GetFloat("_Cull") == 0f) +
            ", collider=" + (terrainCollider.sharedMesh != null && terrainCollider.enabled));
    }

    static float RiverX(float z) { return Mathf.Sin(z * .027f) * 15f + Mathf.Sin(z * .071f) * 4f; }

    static void BuildRiver(Transform root)
    {
        const int steps = 180;
        var vertices = new Vector3[(steps + 1) * 2];
        var triangles = new int[steps * 6];
        for (int i = 0; i <= steps; i++)
        {
            float z = -85f + i;
            float x = RiverX(z);
            float worldZ = z + 45f;
            // Follow the actual generated valley floor at both banks. A fixed Y value
            // placed the entire river several metres above the terrain mesh.
            float leftY = AshGroundSafety.SampleGroundHeight(new Vector3(x - 3.7f, 0f, worldZ)) + 0.10f;
            float rightY = AshGroundSafety.SampleGroundHeight(new Vector3(x + 3.7f, 0f, worldZ)) + 0.10f;
            vertices[i*2] = new Vector3(x - 3.7f, leftY, worldZ);
            vertices[i*2+1] = new Vector3(x + 3.7f, rightY, worldZ);
            if (i < steps)
            {
                int t = i * 6, a = i * 2;
                triangles[t] = a; triangles[t+1] = a+2; triangles[t+2] = a+1;
                triangles[t+3] = a+1; triangles[t+4] = a+2; triangles[t+5] = a+3;
            }
        }
        var go = new GameObject("Winding river - cold reflective water");
        go.transform.SetParent(root, false);
        var m = new Mesh { name = "Winding river surface" };
        m.vertices = vertices; m.triangles = triangles; m.RecalculateNormals();
        go.AddComponent<MeshFilter>().sharedMesh = m;
        go.AddComponent<MeshRenderer>().sharedMaterial = AshMaterials.Create(new Color(.035f,.15f,.20f,.92f), .82f);
        // Water remains a visual surface; the terrain collider beneath it stays walkable.
    }

    static void BuildMountainRanges(Transform root)
    {
        for (int i = 0; i < 44; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = -55f + i * 5.15f;
            float x = side * (67f + Mathf.Sin(i * 1.7f) * 18f);
            float h = 27f + Mathf.Abs(Mathf.Sin(i * 2.13f)) * 39f;
            float w = 21f + (i % 4) * 5.5f;
            float d = 19f + (i % 3) * 6f;
            float worldZ = z + 45f;
            float groundY = AshGroundSafety.SampleGroundHeight(new Vector3(x, 0f, worldZ));
            // The terrain is procedurally uneven; anchor each cliff to its actual local ground
            // height instead of assuming the valley floor is at Y=0.
            var go = CreateJaggedRock(root, "Jagged mountain cliff",
                new Vector3(x, groundY + h * .46f, worldZ),
                new Vector3(w, h, d), new Color(.095f,.115f,.13f), 9, 2.8f);
            go.transform.rotation = Quaternion.Euler(0, i * 29f, Mathf.Sin(i) * 5f);
        }
    }

    static void BuildRockFields(Transform root)
    {
        // Clusters of boulders beside trails and along the riverbank, never forming a wall across the path.
        for (int i = 0; i < 145; i++)
        {
            float a = i * 2.39996f;
            float radius = 11f + (i % 19) * 5.2f;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius + 45f;
            if (Mathf.Abs(x - RiverX(z - 45f)) < 6.5f) x += x < 0 ? -7f : 7f;
            // Keep the central travel corridor open.
            if (Mathf.Abs(x) < 5.5f && z > 4f && z < 86f) continue;
            float size = 0.8f + Mathf.Abs(Mathf.Sin(i * 3.7f)) * 2.4f;
            Vector3 rockScale = new Vector3(size * (1.1f + (i%3)*.22f), size * (.65f + (i%4)*.1f), size);
            float groundY = AshGroundSafety.SampleGroundHeight(new Vector3(x, 0f, z));
            // Mesh origin is at its centre, so raise it by half its scaled height and bury
            // it slightly into the terrain to avoid floating boulders.
            float y = groundY + rockScale.y * .46f;
            CreateJaggedRock(root, "Weathered boulder", new Vector3(x, y, z),
                rockScale,
                i%4==0 ? new Color(.23f,.22f,.20f) : new Color(.13f,.15f,.16f), 4, .75f);
        }
    }

    static GameObject CreateJaggedRock(Transform root, string name, Vector3 position, Vector3 scale, Color color, int rings, float jaggedness)
    {
        int sides = Mathf.Clamp(rings * 2 + 8, 16, 24);
        var verts = new Vector3[sides * rings + 2];
        var uv = new Vector2[verts.Length];
        var tris = new List<int>();
        verts[0] = new Vector3(0f, -.5f, 0f);
        verts[1] = new Vector3(0f, .5f, 0f);
        uv[0] = new Vector2(.5f, 0f);
        uv[1] = new Vector2(.5f, 1f);
        for (int ring = 0; ring < rings; ring++)
        {
            float t = (float)ring / Mathf.Max(1, rings - 1);
            float y = Mathf.Lerp(-.44f, .42f, t);
            float rad = Mathf.Sin((t * .88f + .12f) * Mathf.PI * .5f) * .5f;
            for (int s = 0; s < sides; s++)
            {
                float u = s / (float)sides;
                float angle = u * Mathf.PI * 2f;
                float noise = Mathf.PerlinNoise((s + 13) * .71f, (ring + 7) * .53f) - .5f;
                float height = AshRealisticWorldVisuals.SampleRockHeight(u, t);
                float radial = 1f + noise * .12f * jaggedness + (height - .5f) * .075f * jaggedness;
                float vertical = (Mathf.PerlinNoise(s * 1.3f, ring * 1.7f) - .5f) * .07f * jaggedness
                               + (height - .5f) * .055f * jaggedness;
                verts[2 + ring * sides + s] = new Vector3(
                    Mathf.Cos(angle) * rad * radial, y + vertical, Mathf.Sin(angle) * rad * radial);
                uv[2 + ring * sides + s] = new Vector2(u, t);
            }
        }
        for (int s = 0; s < sides; s++)
        {
            int next = (s + 1) % sides;
            int bottomA = 2 + s;
            int bottomB = 2 + next;
            int topA = 2 + (rings - 1) * sides + s;
            int topB = 2 + (rings - 1) * sides + next;
            tris.Add(0); tris.Add(bottomA); tris.Add(bottomB);
            tris.Add(1); tris.Add(topB); tris.Add(topA);
        }
        for (int ring = 0; ring < rings - 1; ring++)
        for (int s = 0; s < sides; s++)
        {
            int n = (s + 1) % sides;
            int a = 2 + ring * sides + s;
            int b = 2 + ring * sides + n;
            int c = 2 + (ring + 1) * sides + s;
            int d = 2 + (ring + 1) * sides + n;
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(b); tris.Add(c); tris.Add(d);
        }
        var mesh = new Mesh { name = name + " - detailed displaced mesh" };
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(color);
        var collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        return go;
    }

        static void BuildForests(Transform root)
    {
        if (pineCrownMesh == null) pineCrownMesh = MakePineMesh();
        for (int i = 0; i < 340; i++)
        {
            float angle = i * 2.39996f;
            float radius = 17f + (i % 21) * 5.0f;
            float z = Mathf.Sin(angle) * radius + 45f;
            float x = Mathf.Cos(angle) * radius;
            if (Mathf.Abs(x - RiverX(z - 45f)) < 9f) continue;
            float scale = 2.7f + Mathf.Abs(Mathf.Sin(i * 7.1f)) * 4.0f;
            // Keep a navigable clearing around the starting approach.
            if (Mathf.Abs(x) < 7f && z > 5f && z < 36f) continue;
            var tree = new GameObject("Dark pine");
            tree.transform.SetParent(root, false);
            float groundY = AshGroundSafety.SampleGroundHeight(new Vector3(x, 0f, z));
            tree.transform.position = new Vector3(x, groundY, z);
            tree.transform.rotation = Quaternion.Euler(0, i * 37f, 0);
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Pine trunk"; trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = Vector3.up * scale * .39f;
            trunk.transform.localScale = new Vector3(.19f + scale*.025f, scale*.46f, .19f + scale*.025f);
            trunk.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.16f,.105f,.075f));
            var crown = new GameObject("Layered pine boughs", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            crown.transform.SetParent(tree.transform, false);
            crown.transform.localPosition = Vector3.up * (scale * .56f);
            crown.transform.localScale = new Vector3(scale * 1.22f, scale * 1.02f, scale * 1.22f);
            crown.GetComponent<MeshFilter>().sharedMesh = pineCrownMesh;
            crown.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(new Color(.035f, .105f, .078f));
            crown.GetComponent<MeshCollider>().sharedMesh = pineCrownMesh;
        }
    }

    static Mesh MakePineMesh()
    {
        // One continuous, layered conifer silhouette with irregular branch skirts.
        // It replaces the three identical cone primitives with a fuller, less toy-like pine.
        const int sides = 14;
        float[] heights = { -.50f, -.34f, -.20f, -.08f, .06f, .19f, .31f, .42f, .50f };
        float[] radii   = { .10f,  .43f,  .31f,  .49f, .34f, .39f, .25f, .27f, .015f };
        int rings = heights.Length;
        var vertices = new Vector3[rings * sides];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(rings - 1) * sides * 6 + sides * 3];
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                float angle = (s / (float)sides) * Mathf.PI * 2f + (r % 2) * .11f;
                float wobble = 1f + Mathf.Sin(s * 7.31f + r * 2.17f) * .10f
                                  + Mathf.Sin(s * 2.3f + r * 5.7f) * .055f;
                float radius = radii[r] * wobble;
                vertices[r * sides + s] = new Vector3(Mathf.Cos(angle) * radius,
                    heights[r] + Mathf.Sin(s * 4.7f + r) * .012f,
                    Mathf.Sin(angle) * radius);
                uv[r * sides + s] = new Vector2(s / (float)sides, (heights[r] + .5f));
            }
        }
        var tris = new List<int>(triangles.Length);
        for (int r = 0; r < rings - 1; r++)
        for (int s = 0; s < sides; s++)
        {
            int n = (s + 1) % sides;
            int a = r * sides + s;
            int b = r * sides + n;
            int d = (r + 1) * sides + n;
            int e = (r + 1) * sides + s;
            tris.Add(a); tris.Add(e); tris.Add(b);
            tris.Add(b); tris.Add(e); tris.Add(d);
        }
        int tip = tris.Count;
        for (int s = 0; s < sides; s++)
        {
            int n = (s + 1) % sides;
            tris.Add((rings - 1) * sides + s);
            tris.Add((rings - 1) * sides + n);
            tris.Add(tip); // top cap index is appended below by a dedicated tip vertex
        }
        // Use a dedicated, slightly off-centre crown tip for a natural silhouette.
        var finalVertices = new Vector3[vertices.Length + 1];
        var finalUv = new Vector2[uv.Length + 1];
        System.Array.Copy(vertices, finalVertices, vertices.Length);
        System.Array.Copy(uv, finalUv, uv.Length);
        finalVertices[vertices.Length] = new Vector3(.008f, .505f, -.006f);
        finalUv[uv.Length] = new Vector2(.5f, 1f);
        for (int i = tip + 2; i < tris.Count; i += 3) tris[i] = vertices.Length;
        var mesh = new Mesh { name = "Ash realistic layered pine bough mesh" };
        mesh.vertices = finalVertices;
        mesh.uv = finalUv;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void BuildTrail(Transform root)
    {
        for (int i = 0; i < 75; i++)
        {
            float z = -28f + i * 1.7f;
            float x = Mathf.Sin(i * .075f) * 4f - 5f;
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Worn trail stone"; stone.transform.SetParent(root, false);
            float worldZ = z + 45f;
            float groundY = AshGroundSafety.SampleGroundHeight(new Vector3(x, 0f, worldZ));
            // Trail stones are centred meshes; place their underside just below the ground.
            stone.transform.position = new Vector3(x, groundY + .075f, worldZ);
            stone.transform.rotation = Quaternion.Euler(0,Mathf.Sin(i*.2f)*10f,0);
            stone.transform.localScale = new Vector3(2.3f,.18f,1.5f);
            stone.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.20f,.20f,.18f));
            // Primitive collider is retained so path stones are solid, not walk-through decals.
        }
    }

    static Material MaterialFor(Color color)
    {
        Material material;
        if (materialCache.TryGetValue(color, out material) && material != null) return material;
        material = AshMaterials.Create(color, .12f);
        materialCache[color] = material;
        return material;
    }
}
