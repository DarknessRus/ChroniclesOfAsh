using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds the actual traversable Ash Valley landscape at runtime: rolling terrain,
/// mountain ridges, a winding river, conifer forests and rocky paths.
/// Uses procedural meshes and primitives so no external assets are required.
/// </summary>
public static class AshOpenWorld
{
    static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();
    const int Grid = 100;
    const float WorldSize = 260f;

    public static void Build(Transform root)
    {
        var terrain = new GameObject("Open World - rolling ashlands");
        terrain.transform.SetParent(root, false);
        terrain.transform.position = new Vector3(0, -0.15f, 45f);
        var mesh = new Mesh { name = "Procedural rolling terrain" };
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
            float hills = Mathf.Sin(wx * .047f) * 2.8f + Mathf.Cos(wz * .038f) * 2.4f
                + Mathf.Sin((wx + wz) * .027f) * 3.2f;
            float ridge = Mathf.Max(0, Mathf.PerlinNoise((wx + 350) * .012f, (wz + 350) * .012f) - .48f) * 11f;
            float riverDistance = Mathf.Abs(wx - RiverX(wz));
            float bank = Mathf.Clamp01((riverDistance - 3.5f) / 8f);
            float y = Mathf.Lerp(-.05f, hills + ridge, bank);
            int index = z * (Grid + 1) + x;
            vertices[index] = new Vector3(wx, y, wz);
            uv[index] = new Vector2((float)x / Grid * 12, (float)z / Grid * 12);
            float tint = Mathf.PerlinNoise(wx * .09f + 12, wz * .09f + 8);
            colors[index] = Color.Lerp(new Color(.12f,.18f,.14f), new Color(.28f,.29f,.22f), tint);
            if (x < Grid && z < Grid)
            {
                int t = (z * Grid + x) * 6, a = index, b = index + Grid + 1;
                triangles[t] = a; triangles[t+1] = b; triangles[t+2] = a+1;
                triangles[t+3] = a+1; triangles[t+4] = b; triangles[t+5] = b+1;
            }
        }
        mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles;
        mesh.RecalculateNormals();
        terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
        var terrainMat = new Material(Shader.Find("Standard"));
        terrainMat.color = Color.white;
        terrainMat.SetFloat("_Glossiness", .08f);
        terrain.AddComponent<MeshRenderer>().sharedMaterial = terrainMat;
        terrain.AddComponent<MeshCollider>().sharedMesh = mesh;

        BuildRiver(root);
        BuildMountainRanges(root);
        BuildForests(root);
        BuildTrail(root);
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
            vertices[i*2] = new Vector3(x - 3.5f, .12f, z + 45f);
            vertices[i*2+1] = new Vector3(x + 3.5f, .12f, z + 45f);
            if (i < steps)
            {
                int t = i * 6, a = i * 2;
                triangles[t] = a; triangles[t+1] = a+2; triangles[t+2] = a+1;
                triangles[t+3] = a+1; triangles[t+4] = a+2; triangles[t+5] = a+3;
            }
        }
        var go = new GameObject("Winding river - deep blue water");
        go.transform.SetParent(root, false);
        var m = new Mesh { name = "Winding river mesh" };
        m.vertices = vertices; m.triangles = triangles; m.RecalculateNormals();
        go.AddComponent<MeshFilter>().sharedMesh = m;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(.035f,.19f,.24f,.88f);
        mat.SetFloat("_Glossiness", .82f);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void BuildMountainRanges(Transform root)
    {
        for (int i = 0; i < 32; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            float z = -45 + i * 5.7f;
            float x = side * (68 + Mathf.Sin(i * 1.7f) * 18);
            float h = 24 + Mathf.Abs(Mathf.Sin(i * 2.13f)) * 35;
            var mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mountain.name = "Jagged mountain ridge";
            mountain.transform.SetParent(root, false);
            mountain.transform.position = new Vector3(x, h * .42f, z + 45f);
            mountain.transform.rotation = Quaternion.Euler(0, i * 29f, Mathf.Sin(i) * 8f);
            mountain.transform.localScale = new Vector3(24 + (i%3)*8, h, 22 + (i%4)*5);
            mountain.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.105f,.13f,.15f));
        }
    }

    static void BuildForests(Transform root)
    {
        for (int i = 0; i < 260; i++)
        {
            float angle = i * 2.39996f;
            float radius = 18 + (i % 17) * 5.1f;
            float z = Mathf.Sin(angle) * radius + 45f;
            float x = Mathf.Cos(angle) * radius;
            if (Mathf.Abs(x - RiverX(z - 45f)) < 8f) continue;
            float scale = 1.8f + Mathf.Abs(Mathf.Sin(i * 7.1f)) * 3.4f;
            var tree = new GameObject("Dark pine");
            tree.transform.SetParent(root, false);
            tree.transform.position = new Vector3(x, 0, z);
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Pine trunk"; trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = Vector3.up * scale * .45f;
            trunk.transform.localScale = new Vector3(.23f, scale * .55f, .23f);
            trunk.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.19f,.13f,.095f));
            for (int k = 0; k < 3; k++)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crown.name = "Pine crown"; crown.transform.SetParent(tree.transform, false);
                crown.transform.localPosition = Vector3.up * (scale * (.75f + k*.37f));
                crown.transform.localScale = new Vector3(scale*(1.05f-k*.22f), scale*.7f, scale*(1.05f-k*.22f));
                crown.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.055f,.14f,.105f));
            }
        }
    }

    static void BuildTrail(Transform root)
    {
        for (int i = 0; i < 75; i++)
        {
            float z = -28 + i * 1.7f;
            float x = Mathf.Sin(i * .075f) * 4f - 5f;
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Worn trail stone"; stone.transform.SetParent(root, false);
            stone.transform.position = new Vector3(x, .22f, z + 45f);
            stone.transform.rotation = Quaternion.Euler(0, Mathf.Sin(i*.2f)*10, 0);
            stone.transform.localScale = new Vector3(2.3f,.18f,1.5f);
            stone.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.24f,.23f,.20f));
        }
    }

    static Material MaterialFor(Color color)
    {
        Material material;
        if (materialCache.TryGetValue(color, out material) && material != null)
            return material;

        material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Glossiness", .12f);
        materialCache[color] = material;
        return material;
    }
}
