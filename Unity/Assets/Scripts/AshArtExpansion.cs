using UnityEngine;

/// <summary>
/// Adds a second art pass to the mobile prototype using lightweight procedural meshes.
/// Kept separate from the bootstrap so the world and character art can evolve independently.
/// </summary>
public static class AshArtExpansion
{
    static readonly System.Collections.Generic.Dictionary<string, Material> materialCache = new System.Collections.Generic.Dictionary<string, Material>();

    static Material Make(Color color, float smoothness = 0.35f, bool emission = false)
    {
        string key = color.ToString() + "|" + smoothness + "|" + emission;
        Material cached;
        if (materialCache.TryGetValue(key, out cached) && cached != null) return cached;
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) throw new System.InvalidOperationException("No supported shader found for AshArtExpansion materials.");
        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        materialCache[key] = material;
        return material;
    }
    static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 position,
        Vector3 scale, Color color, Vector3? rotation = null, bool glow = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, true);
        // Character armor uses coordinates relative to the character origin; world decorations
        // remain positioned in world space. Without this, armor appears below the character.
        bool attachedActor = parent != null &&
            (parent.GetComponent<CharacterController>() != null || parent.GetComponent<AshEnemy>() != null);
        go.transform.position = attachedActor ? parent.TransformPoint(position) : position;
        go.transform.localScale = scale;
        if (rotation.HasValue)
        {
            if (attachedActor) go.transform.localRotation = Quaternion.Euler(rotation.Value);
            else go.transform.rotation = Quaternion.Euler(rotation.Value);
        }
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = Make(color, 0.72f, glow);
        // Decorative armor pieces must not add dozens of overlapping colliders to the mobile player.
        var collider = go.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        return go;
    }

    static GameObject Cone(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        const int sides = 12;
        var vertices = new Vector3[sides + 2];
        var triangles = new int[sides * 6];
        vertices[0] = new Vector3(0, .5f, 0);
        vertices[1] = new Vector3(0, -.5f, 0);
        for (int i = 0; i < sides; i++)
        {
            float angle = Mathf.PI * 2f * i / sides;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * .5f, -.5f, Mathf.Sin(angle) * .5f);
            int next = (i + 1) % sides;
            int t = i * 6;
            triangles[t] = 0; triangles[t + 1] = i + 2; triangles[t + 2] = next + 2;
            triangles[t + 3] = 1; triangles[t + 4] = next + 2; triangles[t + 5] = i + 2;
        }
        var mesh = new Mesh { name = name + " Mesh", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = Make(color, .8f);
        return go;
    }

    public static void Build(Transform root)
    {
        Color stone = new Color(.24f, .25f, .25f);
        Color darkStone = new Color(.105f, .12f, .14f);
        Color moss = new Color(.10f, .19f, .15f);
        Color ember = new Color(1f, .25f, .045f);

        // Broken fortress walls frame the road and create a readable foreground.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 8.5f;
            for (int i = 0; i < 5; i++)
            {
                float z = 4f + i * 4.4f;
                Part(root, PrimitiveType.Cube, "Ruined fortress wall", new Vector3(x, 1.15f, z),
                    new Vector3(1.15f, 2.3f + (i % 2) * .55f, 3.5f), stone,
                    new Vector3(0, side * (i * 7f), (i % 2 == 0 ? -4f : 5f)));
                Part(root, PrimitiveType.Cube, "Wall capstone", new Vector3(x, 2.45f + (i % 2) * .4f, z),
                    new Vector3(1.45f, .25f, 3.7f), darkStone, new Vector3(0, side * (i * 7f), 0));
            }
            // Jagged broken battlements.
            for (int i = 0; i < 7; i++)
                Part(root, PrimitiveType.Cube, "Broken battlement", new Vector3(x, 2.9f + (i % 3) * .35f, 1f + i * 2.5f),
                    new Vector3(.7f, 1.1f + (i % 2) * .5f, .7f), darkStone, new Vector3(i * 5f, i * 17f, i % 2 == 0 ? 8f : -10f));
        }

        // A ruined arch marks the path toward the castle.
        for (int side = -1; side <= 1; side += 2)
        {
            Part(root, PrimitiveType.Cube, "Gate pillar", new Vector3(side * 3.1f, 2.1f, 18f),
                new Vector3(.85f, 4.2f, 1.1f), darkStone);
            Part(root, PrimitiveType.Cube, "Gate capital", new Vector3(side * 3.1f, 4.25f, 18f),
                new Vector3(1.35f, .4f, 1.5f), stone);
            Cone(root, "Gothic arch peak", new Vector3(side * 1.55f, 4.45f, 18f),
                new Vector3(2.2f, 1.8f, 1.05f), darkStone);
        }
        Part(root, PrimitiveType.Cube, "Broken gate lintel", new Vector3(-.2f, 4.5f, 18f),
            new Vector3(3.8f, .42f, 1.1f), stone, new Vector3(0, 0, -7f));

        // Ruined cottages in the far sides of the valley.
        for (int i = 0; i < 5; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (15f + (i % 3) * 3f);
            float z = 14f + i * 5.5f;
            Part(root, PrimitiveType.Cube, "Abandoned cottage", new Vector3(x, 1.1f, z),
                new Vector3(4f, 2.2f, 3.5f), new Color(.20f, .17f, .14f), new Vector3(0, i * 21f, 0));
            Part(root, PrimitiveType.Cube, "Collapsed roof", new Vector3(x, 2.45f, z),
                new Vector3(4.4f, .35f, 4f), darkStone, new Vector3(0, i * 21f, 8f + i % 3 * 7f));
            Part(root, PrimitiveType.Cube, "Dark doorway", new Vector3(x, .72f, z - 1.78f),
                new Vector3(.75f, 1.45f, .08f), new Color(.035f, .03f, .035f));
        }

        // Ancient rune stones, glowing crystals and low torch lamps.
        for (int i = 0; i < 9; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (4.5f + (i % 3) * 2.8f);
            float z = -5f + i * 4.3f;
            Part(root, PrimitiveType.Cube, "Runestone", new Vector3(x, .8f, z),
                new Vector3(.65f, 1.6f + (i % 3) * .3f, .45f), darkStone, new Vector3(0, i * 29f, i % 2 == 0 ? 4f : -5f));
            Part(root, PrimitiveType.Cube, "Carved rune", new Vector3(x, 1f, z - .25f),
                new Vector3(.12f, .6f, .05f), ember, new Vector3(0, i * 29f, 0), true);
            if (i % 2 == 0)
            {
                var crystal = Part(root, PrimitiveType.Cube, "Ember crystal", new Vector3(x * 1.2f, .85f, z + 1.2f),
                    new Vector3(.42f, 1.05f, .42f), new Color(.14f, .58f, .75f), new Vector3(0, i * 31f, 12f), true);
                var lightObject = new GameObject("Crystal glow");
                lightObject.transform.SetParent(crystal.transform, false);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point; light.range = 4f; light.intensity = .65f;
                light.color = new Color(.12f, .5f, .9f);
            }
        }

        // Tattered orange standards make the faction palette visible from a distance.
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * 6.4f;
            float z = 1f + i * 7f;
            Part(root, PrimitiveType.Cylinder, "Banner pole", new Vector3(x, 2.3f, z),
                new Vector3(.07f, 2.3f, .07f), new Color(.32f, .25f, .16f));
            Part(root, PrimitiveType.Cube, "Torn ash banner", new Vector3(x + .42f, 2.65f, z),
                new Vector3(.82f, 1.05f, .06f), i % 2 == 0 ? new Color(.43f, .12f, .075f) : new Color(.20f, .22f, .22f),
                new Vector3(0, 0, (i % 2 == 0 ? 7f : -9f)));
            Part(root, PrimitiveType.Cube, "Banner sigil", new Vector3(x + .42f, 2.7f, z - .045f),
                new Vector3(.25f, .25f, .025f), ember, new Vector3(0, 0, 45f), true);
        }

        // Fallen trunks, roots, scattered slate and ash mounds add foreground texture.
        for (int i = 0; i < 34; i++)
        {
            float angle = i * 2.399f;
            float radius = 10f + (i % 5) * 3.3f;
            Vector3 p = new Vector3(Mathf.Cos(angle) * radius, .12f, Mathf.Sin(angle) * radius + 8f);
            Part(root, PrimitiveType.Sphere, "Ash-covered boulder", p,
                new Vector3(.7f + (i % 3) * .35f, .4f + (i % 4) * .15f, .65f),
                i % 3 == 0 ? moss : stone, new Vector3(i * 11f, i * 27f, i * 9f));
            if (i % 4 == 0)
            {
                Part(root, PrimitiveType.Cylinder, "Fallen dead trunk", p + new Vector3(1.2f, .25f, .6f),
                    new Vector3(.22f, 1.9f, .22f), new Color(.19f, .13f, .105f), new Vector3(0, 0, 66f));
            }
        }

        // More embers around the path; capped for mobile performance.
        var emberField = new GameObject("Ash motes near ruins");
        emberField.transform.SetParent(root, false);
        emberField.transform.position = new Vector3(0, 1.5f, 10f);
        var particles = emberField.AddComponent<ParticleSystem>();
        var main = particles.main; main.loop = true; main.startLifetime = 5f; main.startSpeed = .2f;
        main.startSize = new ParticleSystem.MinMaxCurve(.025f, .065f);
        main.startColor = new Color(1f, .32f, .08f, .8f); main.maxParticles = 120;
        var emission = particles.emission; emission.rateOverTime = 14f;
        var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(24f, 5f, 38f);
        var velocity = particles.velocityOverLifetime; velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-.12f, .12f);
        velocity.y = new ParticleSystem.MinMaxCurve(.06f, .24f);
        velocity.z = new ParticleSystem.MinMaxCurve(-.08f, .08f);
        var psRenderer = emberField.GetComponent<ParticleSystemRenderer>();
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null) psRenderer.material = new Material(spriteShader);
    }

    public static void DetailHero(Transform hero, int kind)
    {
        Color metal = new Color(.22f, .24f, .25f);
        Color leather = new Color(.19f, .115f, .075f);
        Color accent = kind == 0 ? new Color(.56f, .12f, .075f) :
            kind == 1 ? new Color(.12f, .36f, .58f) :
            kind == 2 ? new Color(.15f, .36f, .23f) : new Color(.37f, .19f, .48f);
        // Art pass sits on the existing controller capsule, preserving its collider and movement.
        Part(hero, PrimitiveType.Capsule, "Armored torso", new Vector3(0f, .06f, 0f),
            new Vector3(.52f, .62f, .39f), leather);
        Part(hero, PrimitiveType.Cube, "Chest plate", new Vector3(0f, .15f, .205f),
            new Vector3(.52f, .42f, .12f), metal);
        Part(hero, PrimitiveType.Cube, "Chest crest", new Vector3(0f, .19f, .275f),
            new Vector3(.11f, .27f, .025f), accent, null, true);
        Part(hero, PrimitiveType.Cube, "Crossed leather strap", new Vector3(-.04f, .2f, .27f),
            new Vector3(.07f, .54f, .035f), leather, new Vector3(0, 0, -18f));
        Part(hero, PrimitiveType.Cube, "Waist belt", new Vector3(0f, -.29f, .015f),
            new Vector3(.61f, .13f, .42f), leather);
        Part(hero, PrimitiveType.Cube, "Belt buckle", new Vector3(0f, -.29f, .23f),
            new Vector3(.13f, .13f, .035f), new Color(.75f, .52f, .22f), null, true);

        for (int side = -1; side <= 1; side += 2)
        {
            Part(hero, PrimitiveType.Capsule, side < 0 ? "Left armored arm" : "Right armored arm",
                new Vector3(side * .43f, -.02f, .015f), new Vector3(.19f, .38f, .2f), leather,
                new Vector3(0, 0, side * -9f));
            Part(hero, PrimitiveType.Cube, "Forearm bracer", new Vector3(side * .46f, -.16f, .045f),
                new Vector3(.2f, .22f, .23f), metal, new Vector3(0, 0, side * -8f));
            Part(hero, PrimitiveType.Sphere, "Shoulder pauldron", new Vector3(side * .39f, .34f, 0f),
                new Vector3(.31f, .25f, .31f), accent);
            Part(hero, PrimitiveType.Capsule, side < 0 ? "Left greave" : "Right greave",
                new Vector3(side * .16f, -.64f, .015f), new Vector3(.19f, .34f, .2f), metal);
            Part(hero, PrimitiveType.Cube, "Boot", new Vector3(side * .16f, -.87f, .09f),
                new Vector3(.22f, .16f, .34f), leather);
            Part(hero, PrimitiveType.Cube, "Knee guard", new Vector3(side * .16f, -.51f, .14f),
                new Vector3(.19f, .16f, .09f), accent);
        }

        // Back cloak and ragged hem give each class a distinct readable silhouette.
        Part(hero, PrimitiveType.Cube, "Weathered cloak", new Vector3(0f, -.05f, -.23f),
            new Vector3(.62f, .72f, .07f), kind == 2 ? new Color(.12f, .24f, .16f) : new Color(.16f, .14f, .16f),
            new Vector3(0, 0, 3f));
        for (int i = 0; i < 3; i++)
            Part(hero, PrimitiveType.Cube, "Cloak torn edge", new Vector3((i - 1) * .19f, -.43f, -.24f),
                new Vector3(.16f, .2f, .075f), kind == 2 ? new Color(.12f, .24f, .16f) : new Color(.16f, .14f, .16f),
                new Vector3(0, 0, (i - 1) * 8f));

        // Face details and class-specific headwear.
        Part(hero, PrimitiveType.Sphere, "Face", new Vector3(0f, .69f, .17f),
            new Vector3(.27f, .3f, .22f), new Color(.48f, .32f, .23f));
        Part(hero, PrimitiveType.Cube, "Eye shadow", new Vector3(0f, .72f, .276f),
            new Vector3(.28f, .055f, .035f), new Color(.055f, .045f, .045f));
        for (int side = -1; side <= 1; side += 2)
            Part(hero, PrimitiveType.Sphere, "Eye glint", new Vector3(side * .075f, .73f, .3f),
                new Vector3(.035f, .025f, .018f), kind == 1 ? new Color(.2f, .65f, 1f) : new Color(.95f, .45f, .15f), null, true);

        if (kind == 1 || kind == 3)
        {
            Cone(hero, "Hood crown", hero.TransformPoint(new Vector3(0f, .98f, -.015f)),
                new Vector3(.42f, .45f, .42f), kind == 1 ? new Color(.10f, .18f, .25f) : new Color(.15f, .10f, .20f));
            Part(hero, PrimitiveType.Cube, "Hood shadow", new Vector3(0f, .75f, .19f),
                new Vector3(.38f, .24f, .13f), new Color(.055f, .055f, .07f));
        }
        if (kind == 0)
        {
            Cone(hero, "Helmet crest", hero.TransformPoint(new Vector3(0f, 1.02f, 0f)),
                new Vector3(.16f, .38f, .16f), accent);
            for (int side = -1; side <= 1; side += 2)
                Cone(hero, "Helmet horn", hero.TransformPoint(new Vector3(side * .2f, .84f, -.015f)),
                    new Vector3(.12f, .25f, .12f), metal);
        }
        if (kind == 2)
        {
            Part(hero, PrimitiveType.Cube, "Ranger hood mantle", new Vector3(0f, .42f, -.12f),
                new Vector3(.75f, .32f, .18f), new Color(.12f, .22f, .15f));
            Part(hero, PrimitiveType.Cube, "Quiver", new Vector3(-.3f, .26f, -.31f),
                new Vector3(.15f, .48f, .16f), leather, new Vector3(0, 0, -12f));
            for (int i = 0; i < 3; i++)
                Part(hero, PrimitiveType.Cylinder, "Arrow shaft", new Vector3(-.3f + i * .045f, .52f, -.31f),
                    new Vector3(.018f, .28f, .018f), new Color(.67f, .54f, .35f), new Vector3(0, 0, -10f));
        }
        if (kind == 3)
        {
            Part(hero, PrimitiveType.Cube, "Necromancer bone charm", new Vector3(.25f, -.02f, .29f),
                new Vector3(.13f, .25f, .045f), new Color(.72f, .68f, .55f));
        }
    }

    public static void DetailWraith(Transform enemy)
    {
        Color shadow = new Color(.10f, .12f, .17f);
        Part(enemy, PrimitiveType.Sphere, "Wraith rib cage", new Vector3(0f, .15f, .02f),
            new Vector3(.56f, .55f, .38f), shadow);
        Cone(enemy, "Wraith crown", enemy.TransformPoint(new Vector3(0f, .85f, 0f)),
            new Vector3(.5f, .55f, .45f), new Color(.17f, .08f, .13f));
        for (int side = -1; side <= 1; side += 2)
        {
            Part(enemy, PrimitiveType.Sphere, "Wraith eye flame", new Vector3(side * .105f, .72f, .2f),
                new Vector3(.09f, .07f, .045f), new Color(1f, .12f, .035f), null, true);
            Part(enemy, PrimitiveType.Capsule, "Wraith arm", new Vector3(side * .42f, .1f, 0f),
                new Vector3(.13f, .42f, .13f), shadow, new Vector3(0, 0, side * 24f));
            Cone(enemy, "Wraith claw", enemy.TransformPoint(new Vector3(side * .53f, -.18f, .12f)),
                new Vector3(.09f, .22f, .09f), new Color(.38f, .12f, .16f));
        }
    }
}
