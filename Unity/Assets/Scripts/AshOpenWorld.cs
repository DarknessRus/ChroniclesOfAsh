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
    const int Grid = 120;
    const float WorldSize = 280f;

    public static void Build(Transform root)
    {
        BuildTerrain(root);
        BuildRiver(root);
        BuildMountainRanges(root);
        BuildRockFields(root);
        BuildForests(root);
        BuildTrail(root);
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
            float broad = Mathf.Sin(wx * .034f) * 2.4f + Mathf.Cos(wz * .031f) * 2.2f;
            float detail = (Mathf.PerlinNoise((wx + 450f) * .035f, (wz + 450f) * .035f) - .5f) * 2.6f
                         + (Mathf.PerlinNoise((wx + 90f) * .095f, (wz + 90f) * .095f) - .5f) * .75f;
            float ridgeNoise = Mathf.PerlinNoise((wx + 350f) * .012f, (wz + 350f) * .012f);
            float ridge = Mathf.Pow(Mathf.Clamp01((ridgeNoise - .43f) / .57f), 1.4f) * 13f;
            float riverDistance = Mathf.Abs(wx - RiverX(wz));
            float bank = Mathf.Clamp01((riverDistance - 4.2f) / 10f);
            float y = Mathf.Lerp(-.08f, broad + detail + ridge, bank);
            int index = z * (Grid + 1) + x;
            vertices[index] = new Vector3(wx, y, wz);
            uv[index] = new Vector2((float)x / Grid * 18f, (float)z / Grid * 18f);
            float tint = Mathf.PerlinNoise(wx * .075f + 12f, wz * .075f + 8f);
            float damp = Mathf.PerlinNoise(wx * .12f + 70f, wz * .12f + 20f);
            Color ground = Color.Lerp(new Color(.075f,.095f,.085f), new Color(.25f,.24f,.19f), tint);
            colors[index] = Color.Lerp(ground, new Color(.20f,.21f,.20f), damp * .35f);
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
        terrain.AddComponent<MeshRenderer>().sharedMaterial = AshMaterials.Create(Color.white, .08f);
        terrain.AddComponent<MeshCollider>().sharedMesh = mesh;
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
            vertices[i*2] = new Vector3(x - 3.7f, .13f, z + 45f);
            vertices[i*2+1] = new Vector3(x + 3.7f, .13f, z + 45f);
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
        for (int i = 0; i < 38; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = -48f + i * 5.35f;
            float x = side * (67f + Mathf.Sin(i * 1.7f) * 18f);
            float h = 27f + Mathf.Abs(Mathf.Sin(i * 2.13f)) * 39f;
            float w = 21f + (i % 4) * 5.5f;
            float d = 19f + (i % 3) * 6f;
            var go = CreateJaggedRock("Jagged mountain cliff", new Vector3(x, h * .43f, z + 45f),
                new Vector3(w, h, d), new Color(.095f,.115f,.13f), 5, 2.8f);
            go.transform.rotation = Quaternion.Euler(0, i * 29f, Mathf.Sin(i) * 5f);
        }
    }

    static void BuildRockFields(Transform root)
    {
        // Clusters of boulders beside trails and along the riverbank, never forming a wall across the path.
        for (int i = 0; i < 115; i++)
        {
            float a = i * 2.39996f;
            float radius = 11f + (i % 19) * 5.2f;
            float x = Mathf.Cos(a) * radius;
            float z = Mathf.Sin(a) * radius + 45f;
            if (Mathf.Abs(x - RiverX(z - 45f)) < 6.5f) x += x < 0 ? -7f : 7f;
            // Keep the central travel corridor open.
            if (Mathf.Abs(x) < 5.5f && z > 4f && z < 86f) continue;
            float size = 0.8f + Mathf.Abs(Mathf.Sin(i * 3.7f)) * 2.4f;
            float y = -0.05f + size * .30f;
            CreateJaggedRock("Weathered boulder", new Vector3(x, y, z),
                new Vector3(size * (1.1f + (i%3)*.22f), size * (.65f + (i%4)*.1f), size),
                i%4==0 ? new Color(.23f,.22f,.20f) : new Color(.13f,.15f,.16f), 2, .45f);
        }
    }

    static GameObject CreateJaggedRock(string name, Vector3 position, Vector3 scale, Color color, int rings, float jaggedness)
    {
        // Shared low-poly rock topology with a perturbed crown and irregular footprint.
        int sides = 7;
        var verts = new Vector3[sides * rings + 2];
        var tris = new List<int>();
        verts[0] = new Vector3(0f, .5f, 0f);
        verts[1] = new Vector3(0f, -.5f, 0f);
        for (int ring = 0; ring < rings; ring++)
        {
            float t = (float)ring / Mathf.Max(1, rings - 1);
            float y = Mathf.Lerp(-.42f, .34f, t);
            float rad = Mathf.Sin((t * .88f + .12f) * Mathf.PI * .5f) * .5f;
            for (int s = 0; s < sides; s++)
            {
                float angle = (s/(float)sides) * Mathf.PI * 2f;
                float jitter = 1f + (Mathf.PerlinNoise((s+13)*.71f,(ring+7)*.53f)-.5f)*.48f;
                verts[2 + ring*sides + s] = new Vector3(Mathf.Cos(angle)*rad*jitter, y + (Mathf.PerlinNoise(s*1.3f,ring*1.7f)-.5f)*.08f, Mathf.Sin(angle)*rad*jitter);
            }
        }
        for (int s=0;s<sides;s++)
        {
            int next=(s+1)%sides;
            tris.Add(0); tris.Add(2+s); tris.Add(2+next);
            tris.Add(1); tris.Add(2+(rings-1)*sides+next); tris.Add(2+(rings-1)*sides+s);
        }
        for(int ring=0;ring<rings-1;ring++) for(int s=0;s<sides;s++)
        {
            int n=(s+1)%sides, a=2+ring*sides+s, b=2+ring*sides+n, c=2+(ring+1)*sides+s, d=2+(ring+1)*sides+n;
            tris.Add(a);tris.Add(c);tris.Add(b); tris.Add(b);tris.Add(c);tris.Add(d);
        }
        var mesh=new Mesh { name=name+" mesh" }; mesh.vertices=verts; mesh.triangles=tris.ToArray(); mesh.RecalculateNormals();
        var go=new GameObject(name); go.transform.SetParent(root,false); go.transform.position=position; go.transform.localScale=scale;
        go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=MaterialFor(color);
        var collider=go.AddComponent<MeshCollider>(); collider.sharedMesh=mesh;
        return go;
    }

    static void BuildForests(Transform root)
    {
        if (pineCrownMesh == null) pineCrownMesh = MakePineMesh();
        for (int i = 0; i < 300; i++)
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
            tree.transform.position = new Vector3(x, -0.05f, z);
            tree.transform.rotation = Quaternion.Euler(0, i * 37f, 0);
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Pine trunk"; trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = Vector3.up * scale * .39f;
            trunk.transform.localScale = new Vector3(.19f + scale*.025f, scale*.46f, .19f + scale*.025f);
            trunk.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.16f,.105f,.075f));
            for (int k = 0; k < 3; k++)
            {
                var crown = new GameObject("Layered pine boughs", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                crown.transform.SetParent(tree.transform, false);
                crown.transform.localPosition = Vector3.up * (scale * (.40f + k*.23f));
                float crownScale = scale * (1.05f-k*.20f);
                crown.transform.localScale = new Vector3(crownScale, scale*.63f, crownScale);
                crown.GetComponent<MeshFilter>().sharedMesh = pineCrownMesh;
                crown.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(k==0 ? new Color(.035f,.095f,.075f) : new Color(.045f,.13f,.09f));
                crown.GetComponent<MeshCollider>().sharedMesh = pineCrownMesh;
            }
        }
    }

    static Mesh MakePineMesh()
    {
        const int sides=9;
        var vertices=new Vector3[sides+1];
        var triangles=new int[sides*6];
        vertices[0]=new Vector3(0,.5f,0);
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2f/sides;
            vertices[i+1]=new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f);
            int next=(i+1)%sides;
            int t=i*6; triangles[t]=0;triangles[t+1]=i+1;triangles[t+2]=next+1;
            triangles[t+3]=i+1;triangles[t+4]=sides;triangles[t+5]=next+1;
        }
        // Correctly sized closed cone with a base center.
        var v2=new Vector3[sides+2]; System.Array.Copy(vertices,v2,vertices.Length); v2[sides+1]=new Vector3(0,-.5f,0);
        var t2=new int[sides*6];
        for(int i=0;i<sides;i++){int n=(i+1)%sides;int t=i*6;t2[t]=0;t2[t+1]=i+1;t2[t+2]=n+1;t2[t+3]=sides+1;t2[t+4]=n+1;t2[t+5]=i+1;}
        var mesh=new Mesh{name="Low-poly pine crown"};mesh.vertices=v2;mesh.triangles=t2;mesh.RecalculateNormals();return mesh;
    }

    static void BuildTrail(Transform root)
    {
        for (int i = 0; i < 75; i++)
        {
            float z = -28f + i * 1.7f;
            float x = Mathf.Sin(i * .075f) * 4f - 5f;
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Worn trail stone"; stone.transform.SetParent(root, false);
            stone.transform.position = new Vector3(x,.22f,z+45f);
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
