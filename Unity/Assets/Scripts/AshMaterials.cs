using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Android-safe runtime materials with generated high-frequency surface detail.
/// Textures are generated locally (no network/download dependencies) and cached by tint.
/// </summary>
public static class AshMaterials
{
    static Material standardTemplate;
    static Material spriteTemplate;
    static readonly Dictionary<int, Texture2D> detailTextures = new Dictionary<int, Texture2D>();

    static Material StandardTemplate
    {
        get
        {
            if (standardTemplate == null)
                standardTemplate = Resources.Load<Material>("AshRuntimeStandard");
            return standardTemplate;
        }
    }

    static Material SpriteTemplate
    {
        get
        {
            if (spriteTemplate == null)
                spriteTemplate = Resources.Load<Material>("AshRuntimeSprites");
            return spriteTemplate;
        }
    }

    public static Material Create(Color color, float smoothness = 0.35f, bool emission = false)
    {
        Material material;
        if (StandardTemplate != null)
        {
            material = new Material(StandardTemplate);
        }
        else
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null)
                throw new System.InvalidOperationException(
                    "Ash Chronicles could not load a material shader. Ensure AshRuntimeStandard.mat is present in Assets/Resources.");
            material = new Material(shader);
        }

        material.color = color;
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);

        // A real tiled albedo pattern breaks up the flat primitive colours across terrain,
        // rocks, tree bark, ruins and armor without downloading assets or changing colliders.
        if (material.HasProperty("_MainTex"))
        {
            int key = color.GetHashCode();
            Texture2D detail;
            if (!detailTextures.TryGetValue(key, out detail) || detail == null)
            {
                detail = BuildDetailTexture(key);
                detailTextures[key] = detail;
            }
            material.mainTexture = detail;
            material.mainTextureScale = new Vector2(3.5f, 3.5f);
        }

        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.0f);
        }
        return material;
    }

    static Texture2D BuildDetailTexture(int seed)
    {
        const int size = 128;
        // Use a stable seed so each material tint gets its own repeatable surface pattern.
        float offsetX = Mathf.Abs(seed % 10007) * 0.0137f + 17.3f;
        float offsetY = Mathf.Abs((seed / 97) % 10009) * 0.0171f + 41.8f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
        texture.name = "Ash procedural surface " + seed;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 4;
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (float)x / size;
            float v = (float)y / size;
            float broad = Mathf.PerlinNoise(offsetX + u * 5.5f, offsetY + v * 5.5f);
            float grain = Mathf.PerlinNoise(offsetX + u * 29f, offsetY + v * 29f);
            float fine = Mathf.PerlinNoise(offsetX + u * 73f, offsetY + v * 73f);
            // Thin irregular dark fissures give stone and ash a worn, non-plastic surface.
            float fissure = Mathf.Pow(1f - Mathf.Abs(Mathf.PerlinNoise(offsetX + u * 17f, offsetY + v * 17f) - .5f) * 2f, 18f);
            float value = Mathf.Clamp(.58f + broad * .23f + grain * .13f + fine * .08f - fissure * .16f, .42f, 1.08f);
            pixels[y * size + x] = new Color(value, value, value, 1f);
        }

        texture.SetPixels(pixels);
        texture.Apply(true, true);
        return texture;
    }

    public static Material CreateSpriteMaterial()
    {
        if (SpriteTemplate != null)
            return new Material(SpriteTemplate);

        Shader shader = Shader.Find("Sprites/Default");
        return shader != null ? new Material(shader) : null;
    }
}
