using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Mobile dark-fantasy presentation layer: balanced brighter lighting, restrained bloom/vignette,
/// warm torch pools, cold fill light, drifting embers and a reusable spell burst.
/// No third-party package assets are required; all effects are generated in code.
/// </summary>
public static class AshDarkFantasyPresentation
{
    static bool applied;
    static Material glowMaterial;
    static Material stoneMaterial;
    static Volume volume;

    public static void Apply(Transform root)
    {
        if (applied) return;
        applied = true;
        SetupLighting();
        SetupPostProcessing(Camera.main);
        SetupAmbientEmbers(root);
        BuildGothicRuins(root);
    }

    public static void ConfigureCamera(Camera cam)
    {
        if (cam == null) return;
        SetupLighting();
        SetupPostProcessing(cam);
    }

    static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        // Lift shadow detail so terrain, trees and the hero remain readable on phone screens.
        RenderSettings.ambientSkyColor = new Color(.30f, .34f, .41f);
        RenderSettings.ambientEquatorColor = new Color(.19f, .21f, .24f);
        RenderSettings.ambientGroundColor = new Color(.075f, .078f, .085f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.12f, .15f, .19f);
        RenderSettings.fogDensity = .0022f;

        Light key = RenderSettings.sun;
        foreach (Light candidate in Object.FindObjectsOfType<Light>())
            if (key == null && candidate.type == LightType.Directional) { key = candidate; break; }
        if (key == null)
        {
            var go = new GameObject("Ash - cold moon key light");
            key = go.AddComponent<Light>();
            key.type = LightType.Directional;
            go.transform.rotation = Quaternion.Euler(37f, -28f, 0f);
        }
        key.color = new Color(.78f, .83f, .94f);
        key.intensity = 1.15f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = .62f;
        key.renderMode = LightRenderMode.Auto;
    }

    static void SetupPostProcessing(Camera cam)
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindObjectOfType<Camera>();
                if (cam != null && cam.tag != "MainCamera") cam.tag = "MainCamera";
            }
        }

        if (cam != null)
        {
            var additional = cam.GetComponent<UniversalAdditionalCameraData>();
            if (additional == null) additional = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            additional.renderPostProcessing = true;
            additional.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        if (volume != null) return;
        var go = new GameObject("Ash - Global Volume - Ashen Dread");
        Object.DontDestroyOnLoad(go);
        volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.weight = 1f;
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = volume.profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(.5f);
        bloom.scatter.Override(.65f);
        bloom.highQualityFiltering.Override(false);

        var vignette = volume.profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.color.Override(new Color(.012f, .016f, .025f));
        vignette.intensity.Override(.16f);
        vignette.smoothness.Override(.55f);
        vignette.rounded.Override(true);

        var color = volume.profile.Add<ColorAdjustments>(true);
        color.active = true;
        // Positive exposure and gentler contrast brighten dark surfaces without washing out highlights.
        color.postExposure.Override(.25f);
        color.contrast.Override(8f);
        color.saturation.Override(-8f);
        color.colorFilter.Override(new Color(.91f, .94f, 1f));

        var balance = volume.profile.Add<WhiteBalance>(true);
        balance.active = true;
        balance.temperature.Override(-3f);
        balance.tint.Override(1f);

        var tone = volume.profile.Add<Tonemapping>(true);
        tone.active = true;
        tone.mode.Override(TonemappingMode.ACES);

        var grain = volume.profile.Add<FilmGrain>(true);
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(.045f);
        grain.response.Override(.75f);
    }

    static Material MakeGlowMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var material = new Material(shader);
        material.name = "Ash VFX - emissive " + color;
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.2f);
        }
        return material;
    }

    static void SetupAmbientEmbers(Transform root)
    {
        var go = new GameObject("Ash - drifting ember motes");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 1.2f, 45f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.08f, .32f);
        main.startSize = new ParticleSystem.MinMaxCurve(.025f, .075f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f,.24f,.055f,.85f), new Color(.35f,.58f,1f,.65f));
        main.maxParticles = 180;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 16f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(55f, 9f, 110f);
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-.12f, .12f);
        velocity.y = new ParticleSystem.MinMaxCurve(.05f, .24f);
        velocity.z = new ParticleSystem.MinMaxCurve(-.08f, .08f);
        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f,.26f,.06f),0f), new GradientColorKey(new Color(.28f,.48f,1f),1f) },
            new[] { new GradientAlphaKey(0f,0f), new GradientAlphaKey(.85f,.18f), new GradientAlphaKey(.45f,.75f), new GradientAlphaKey(0f,1f) });
        colorLife.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = MakeGlowMaterial(new Color(1f,.38f,.12f,.9f));
        ps.Play();
    }

    static void BuildGothicRuins(Transform root)
    {
        stoneMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        stoneMaterial.name = "Ash Gothic - charcoal stone";
        stoneMaterial.color = new Color(.105f,.12f,.145f);
        if (stoneMaterial.HasProperty("_Smoothness")) stoneMaterial.SetFloat("_Smoothness", .22f);
        if (stoneMaterial.HasProperty("_Glossiness")) stoneMaterial.SetFloat("_Glossiness", .22f);

        for (int i = 0; i < 8; i++)
        {
            float z = -15f + i * 24f;
            float side = (i % 2 == 0) ? -1f : 1f;
            Vector3 position = new Vector3(side * (13f + (i % 3) * 2.2f), .5f, z + 45f);
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Ash Gothic - broken roadside pillar";
            pillar.transform.SetParent(root, false);
            pillar.transform.position = position;
            pillar.transform.localScale = new Vector3(.8f, 2.5f + (i % 3) * .35f, .8f);
            pillar.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
            if (i % 2 == 0) AddTorch(root, position + new Vector3(side * .9f, 2.3f, 0f), i);
        }

        Vector3 archCenter = new Vector3(0f, 0f, 105f);
        CreateStoneBlock(root, "Ash Gothic - arch left pier", archCenter + new Vector3(-4.1f, 2.2f, 0f), new Vector3(1.05f, 4.4f, 1.1f));
        CreateStoneBlock(root, "Ash Gothic - arch right pier", archCenter + new Vector3(4.1f, 2.2f, 0f), new Vector3(1.05f, 4.4f, 1.1f));
        CreateStoneBlock(root, "Ash Gothic - broken lintel", archCenter + new Vector3(0f, 4.45f, 0f), new Vector3(8.3f, .8f, 1.15f));
        AddTorch(root, archCenter + new Vector3(-3.4f, 3.3f, -.7f), 9);
        AddTorch(root, archCenter + new Vector3(3.4f, 3.3f, -.7f), 10);
    }

    static void CreateStoneBlock(Transform root, string name, Vector3 position, Vector3 scale)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(root, false);
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
    }

    static void AddTorch(Transform root, Vector3 position, int index)
    {
        var torch = new GameObject("Ash Gothic - orange torch light " + index);
        torch.transform.SetParent(root, false);
        torch.transform.position = position;
        var light = torch.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f,.29f,.075f);
        light.intensity = 1.45f;
        light.range = 9f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;

        var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Torch ember";
        flame.transform.SetParent(torch.transform, false);
        flame.transform.localPosition = new Vector3(0f,.16f,0f);
        flame.transform.localScale = new Vector3(.16f,.32f,.16f);
        Object.Destroy(flame.GetComponent<Collider>());
        if (glowMaterial == null) glowMaterial = MakeGlowMaterial(new Color(1f,.21f,.035f,1f));
        flame.GetComponent<Renderer>().sharedMaterial = glowMaterial;
    }

    public static void AttachHeroAura(Transform target)
    {
        if (target == null || target.Find("Ash VFX - spectral hero aura") != null) return;
        var go = new GameObject("Ash VFX - spectral hero aura");
        go.transform.SetParent(target, false);
        go.transform.localPosition = new Vector3(0f, .9f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .22f);
        main.startSize = new ParticleSystem.MinMaxCurve(.035f, .09f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(.28f,.48f,1f,.7f), new Color(.62f,.12f,.95f,.65f));
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission;
        emission.rateOverTime = 10f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .52f;
        shape.radiusThickness = 0f;
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(.08f,.28f);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = MakeGlowMaterial(new Color(.42f,.28f,1f,.8f));
        ps.Play();
    }

    public static ParticleSystem SpawnDarkSpell(Vector3 position, Transform parent = null)
    {
        var go = new GameObject("Ash VFX - dark spell burst");
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = position;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = 1.15f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(.07f, .22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(.28f,.45f,1f,1f), new Color(.72f,.12f,.95f,1f));
        main.maxParticles = 96;
        main.gravityModifier = -.08f;
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 56) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .18f;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f,.25f), new Keyframe(.22f,1f), new Keyframe(1f,0f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(.25f,.45f,1f),0f), new GradientColorKey(new Color(.8f,.1f,.95f),.55f), new GradientColorKey(new Color(.05f,.02f,.16f),1f) },
            new[] { new GradientAlphaKey(0f,0f), new GradientAlphaKey(1f,.12f), new GradientAlphaKey(.8f,.55f), new GradientAlphaKey(0f,1f) });
        color.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = MakeGlowMaterial(new Color(.42f,.24f,1f,1f));
        ps.Play();
        Object.Destroy(go, 2.5f);
        return ps;
    }
}