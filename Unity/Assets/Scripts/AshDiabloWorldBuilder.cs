using UnityEngine;

/// <summary>
/// Builds the first Diablo-inspired playable area. Art is deliberately isolated here so it
/// can be replaced by authored meshes/materials without changing menu or gameplay code.
/// </summary>
public static class AshDiabloWorldBuilder
{
    public static void Build(Transform root, int heroIndex)
    {
        if (root == null) throw new System.ArgumentNullException(nameof(root));
        ConfigureAtmosphere();
        BuildGroundAndLandmarks(root);

        // Reuse existing world-art modules when present, while keeping a reliable base level.
        AshArtExpansion.Build(root);
        AshWorldUpgrade.BuildCastleAndValley(root);

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = new[] { "Warrior", "Mage", "Ranger", "Necromancer" }[Mathf.Clamp(heroIndex, 0, 3)] + " Player";
        player.transform.position = new Vector3(0, 1, -3);
        player.GetComponent<Renderer>().material = MaterialFor(new Color(.22f, .22f, .23f), .72f);
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = .35f;
        player.AddComponent<AshPlayerController>();
        AshArtExpansion.DetailHero(player.transform, Mathf.Clamp(heroIndex, 0, 3));

        var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "Ash Wraith";
        enemy.transform.position = new Vector3(2.5f, 1, 5);
        enemy.GetComponent<Renderer>().material = MaterialFor(new Color(.32f, .055f, .09f), .8f);
        enemy.AddComponent<AshEnemy>();
        AshArtExpansion.DetailWraith(enemy.transform);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 48f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 260f;
        cameraObject.transform.position = new Vector3(0, 12, -13);
        cameraObject.transform.LookAt(new Vector3(0, 0, 3));
        var follow = cameraObject.AddComponent<AshFollowCamera>();
        follow.target = player.transform;

        BuildWorldLights(root);
    }

    private static void ConfigureAtmosphere()
    {
        RenderSettings.ambientLight = new Color(.22f, .23f, .25f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.12f, .14f, .17f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .008f;
    }

    private static void BuildGroundAndLandmarks(Transform root)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ashen battlefield";
        ground.transform.SetParent(root, false);
        ground.transform.localScale = new Vector3(9, 1, 9);
        ground.GetComponent<Renderer>().material = MaterialFor(new Color(.12f, .14f, .13f), .96f);

        // A readable path leads from the starting camp towards the fortress.
        for (int i = 0; i < 24; i++)
        {
            float z = -8 + i * 1.15f;
            float x = Mathf.Sin(i * .23f) * 1.6f;
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Broken pilgrim road";
            stone.transform.SetParent(root, false);
            stone.transform.position = new Vector3(x, .06f, z);
            stone.transform.rotation = Quaternion.Euler(0, Mathf.Sin(i * .31f) * 10f, 0);
            stone.transform.localScale = new Vector3(1.55f, .12f, .94f);
            stone.GetComponent<Renderer>().material = MaterialFor(i % 3 == 0 ? new Color(.27f,.25f,.23f) : new Color(.19f,.20f,.20f), .95f);
        }

        // Broken walls and black stone monoliths establish scale and cover.
        for (int i = 0; i < 16; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float z = -5 + i * 2.6f;
            var ruin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ruin.name = "Ruined fortress wall";
            ruin.transform.SetParent(root, false);
            ruin.transform.position = new Vector3(side * (6.5f + (i % 3)), .8f + (i % 4) * .22f, z);
            ruin.transform.rotation = Quaternion.Euler(0, side * (8 + i * 7), side * 2);
            ruin.transform.localScale = new Vector3(1.1f + (i % 3) * .5f, 1.5f + (i % 4) * .7f, 2.8f);
            ruin.GetComponent<Renderer>().material = MaterialFor(new Color(.17f,.18f,.19f), .98f);
        }

        var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        altar.name = "Forgotten altar";
        altar.transform.SetParent(root, false);
        altar.transform.position = new Vector3(-4, .35f, 4);
        altar.transform.localScale = new Vector3(1.6f, .35f, 1.6f);
        altar.GetComponent<Renderer>().material = MaterialFor(new Color(.24f,.19f,.18f), .9f);

        var ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ember.name = "Cursed ember";
        ember.transform.SetParent(root, false);
        ember.transform.position = new Vector3(-4, .85f, 4);
        ember.transform.localScale = new Vector3(.25f,.5f,.25f);
        ember.GetComponent<Renderer>().material = MaterialFor(new Color(1f,.19f,.025f), .3f);
        var fireLight = ember.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = new Color(1f,.22f,.06f);
        fireLight.range = 9f;
        fireLight.intensity = 2.2f;
    }

    private static void BuildWorldLights(Transform root)
    {
        var sunObject = new GameObject("Blood moon ambient light");
        sunObject.transform.SetParent(root, false);
        sunObject.transform.rotation = Quaternion.Euler(42, -28, 0);
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = .72f;
        sun.color = new Color(.62f, .67f, .78f);
    }

    private static Material MaterialFor(Color color, float smoothness)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        return material;
    }
}
