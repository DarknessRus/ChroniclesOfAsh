using UnityEngine;

/// <summary>
/// Adds the major visual landmarks visible in the world reference: distant snow-capped
/// peaks, a stepped waterfall, and a blood-red moon. Water and snow are visual surfaces;
/// solid mountain geometry retains mesh colliders and does not replace the walkable ground.
/// </summary>
public static class AshReferenceLandscapeDetails
{
    static Material MakeMaterial(string name, Color color, float metallic, float smoothness, bool emissive = false)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Diffuse");
        var material = new Material(shader);
        material.name = name;
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        if (emissive)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        return material;
    }

    public static void Build(Transform root)
    {
        BuildSnowyMountainHorizon(root);
        BuildWaterfall(root);
        BuildBloodMoon(root);
    }

    static void BuildSnowyMountainHorizon(Transform root)
    {
        var rock = MakeMaterial("Reference Peaks - blue charcoal stone", new Color(.16f, .19f, .22f), .12f, .22f);
        var snow = MakeMaterial("Reference Peaks - cold snow", new Color(.72f, .78f, .81f), .02f, .18f);
        // The far horizon is deliberately layered behind the playable valley, not placed
        // over the ground mesh. Each mountain is a closed, collidable faceted mesh.
        BuildPeak(root, "Distant snow mountain - west", new Vector3(-37f, 0f, 174f), 26f, 53f, 24f, rock, snow, 11);
        BuildPeak(root, "Distant snow mountain - central", new Vector3(-5f, 0f, 190f), 33f, 67f, 28f, rock, snow, 17);
        BuildPeak(root, "Distant snow mountain - east", new Vector3(30f, 0f, 178f), 28f, 57f, 25f, rock, snow, 23);
        BuildPeak(root, "Distant snow mountain - far east", new Vector3(53f, 0f, 194f), 22f, 46f, 23f, rock, snow, 31);
        // Layered secondary ridges give the valley the deep, overlapping mountain horizon
        // visible in the saved reference image, rather than four isolated cone silhouettes.
        var farRock = MakeMaterial("Reference far ridges - atmospheric blue slate", new Color(.105f, .135f, .17f), .04f, .16f);
        var farSnow = MakeMaterial("Reference far ridges - misted snow", new Color(.43f, .49f, .53f), .01f, .12f);
        BuildPeak(root, "Far ridge - western shoulder", new Vector3(-69f, -3f, 213f), 31f, 38f, 20f, farRock, farSnow, 43);
        BuildPeak(root, "Far ridge - central saddle", new Vector3(-28f, -5f, 224f), 39f, 44f, 22f, farRock, farSnow, 47);
        BuildPeak(root, "Far ridge - eastern shoulder", new Vector3(70f, -4f, 218f), 34f, 40f, 21f, farRock, farSnow, 53);
    }

    static void BuildPeak(Transform root, string name, Vector3 basePosition, float radius, float height,
        float depth, Material rock, Material snow, int seed)
    {
        int sides = 11;
        var vertices = new Vector3[sides * 2 + 1];
        var triangles = new int[sides * 6];
        var random = new System.Random(seed);
        vertices[0] = new Vector3(0f, height, 0f);
        for (int i = 0; i < sides; i++)
        {
            float a = Mathf.PI * 2f * i / sides;
            float r = radius * (.78f + (float)random.NextDouble() * .34f);
            vertices[1 + i] = new Vector3(Mathf.Cos(a) * r, height * (.20f + (float)random.NextDouble() * .13f), Mathf.Sin(a) * depth);
            vertices[1 + sides + i] = new Vector3(Mathf.Cos(a + .17f) * r * .45f, height * (.52f + (float)random.NextDouble() * .16f), Mathf.Sin(a + .17f) * depth * .48f);
        }
        // Alternating upper and lower rings create angular ridges instead of smooth cones.
        var all = new Vector3[1 + sides * 2];
        all[0] = vertices[0];
        for (int i = 0; i < sides; i++) { all[1 + i] = vertices[1 + i]; all[1 + sides + i] = vertices[1 + sides + i]; }
        var tris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            tris.Add(0); tris.Add(1 + sides + i); tris.Add(1 + sides + next);
            tris.Add(1 + sides + i); tris.Add(1 + i); tris.Add(1 + next);
            tris.Add(1 + sides + i); tris.Add(1 + next); tris.Add(1 + sides + next);
        }
        var mesh = new Mesh { name = name + " collidable mesh" };
        mesh.vertices = all; mesh.triangles = tris.ToArray(); mesh.RecalculateNormals();
        var go = new GameObject(name);
        go.transform.SetParent(root, false); go.transform.position = basePosition;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = rock;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;

        // Small faceted snow patches hug the upper slopes; these are decoration only.
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 2.39996f + seed;
            float y = height * (.62f + (i % 3) * .055f);
            float x = Mathf.Cos(angle) * radius * (.12f + (i % 2) * .13f);
            float z = Mathf.Sin(angle) * depth * (.12f + (i % 3) * .07f);
            var patch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            patch.name = "Snow patch on distant mountain";
            patch.transform.SetParent(root, false);
            patch.transform.position = basePosition + new Vector3(x, y, z);
            patch.transform.rotation = Quaternion.Euler(18f + i * 7f, i * 31f, 12f - i * 5f);
            patch.transform.localScale = new Vector3(radius * (.22f + (i % 3) * .055f), height * (.032f + (i % 2) * .012f), depth * (.11f + (i % 2) * .035f));
            patch.GetComponent<Renderer>().sharedMaterial = snow;
            var collider = patch.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
        }
    }

    static float RiverX(float z) { return Mathf.Sin(z * .027f) * 15f + Mathf.Sin(z * .071f) * 4f; }

    static void BuildWaterfall(Transform root)
    {
        var cliffMaterial = MakeMaterial("Waterfall ledge - wet dark stone", new Color(.105f, .13f, .15f), .08f, .28f);
        var waterMaterial = MakeMaterial("Waterfall - cold silver blue", new Color(.34f, .55f, .64f, .92f), .18f, .72f);
        waterMaterial.EnableKeyword("_EMISSION");
        waterMaterial.SetColor("_EmissionColor", new Color(.12f, .22f, .27f));
        float z = 104f;
        float centerX = RiverX(z - 45f);
        // Two broken rocky banks frame a narrow gorge; the existing river mesh continues below.
        for (int side = -1; side <= 1; side += 2)
        {
            var bank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bank.name = "Waterfall gorge rock bank";
            bank.transform.SetParent(root, false);
            bank.transform.position = new Vector3(centerX + side * 5.4f, 4.1f, z);
            bank.transform.rotation = Quaternion.Euler(0f, side * 7f, side * 3f);
            bank.transform.localScale = new Vector3(5.5f, 8.5f, 8f);
            bank.GetComponent<Renderer>().sharedMaterial = cliffMaterial;
        }
        // A vertical, slightly angled water sheet and a broad white splash at its foot.
        var waterfall = new GameObject("Reference waterfall - vertical cascade");
        waterfall.transform.SetParent(root, false);
        var mesh = new Mesh { name = "Waterfall curtain mesh" };
        mesh.vertices = new Vector3[] {
            new Vector3(centerX - 3.3f, .25f, z - .5f),
            new Vector3(centerX + 3.3f, .25f, z - .5f),
            new Vector3(centerX - 2.8f, 8.3f, z + 1.4f),
            new Vector3(centerX + 2.8f, 8.3f, z + 1.4f)
        };
        mesh.uv = new Vector2[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();
        waterfall.AddComponent<MeshFilter>().sharedMesh = mesh;
        waterfall.AddComponent<MeshRenderer>().sharedMaterial = waterMaterial;

        var foam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foam.name = "Waterfall foot - whitewater";
        foam.transform.SetParent(root, false);
        foam.transform.position = new Vector3(centerX, .18f, z - 1.2f);
        foam.transform.localScale = new Vector3(8.2f, .24f, 2.5f);
        foam.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Waterfall foam", new Color(.57f, .67f, .69f), .04f, .6f);
        Object.Destroy(foam.GetComponent<Collider>());
    }

    static void BuildBloodMoon(Transform root)
    {
        var moonMaterial = MakeMaterial("Blood moon - emissive crimson", new Color(.88f, .13f, .10f), .02f, .25f, true);
        var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Reference sky - blood red moon";
        moon.transform.SetParent(root, false);
        moon.transform.position = new Vector3(17f, 67f, 205f);
        moon.transform.localScale = new Vector3(13f, 13f, 2.2f);
        moon.GetComponent<Renderer>().sharedMaterial = moonMaterial;
        Object.Destroy(moon.GetComponent<Collider>());

        var haloMaterial = MakeMaterial("Blood moon - soft ember halo", new Color(.43f, .055f, .045f, .28f), 0f, .05f, true);
        var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        halo.name = "Reference sky - crimson moon halo";
        halo.transform.SetParent(root, false);
        halo.transform.position = moon.transform.position + new Vector3(0f, 0f, 1.2f);
        halo.transform.localScale = new Vector3(18f, 18f, 1.2f);
        halo.GetComponent<Renderer>().sharedMaterial = haloMaterial;
        Object.Destroy(halo.GetComponent<Collider>());
    }
}
