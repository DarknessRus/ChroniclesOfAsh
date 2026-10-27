using UnityEngine;

/// <summary>
/// Loads shader-backed materials from Resources first so Unity includes their shaders in Android builds.
/// Runtime Shader.Find calls alone are vulnerable to shader stripping in player builds.
/// </summary>
public static class AshMaterials
{
    static Material standardTemplate;
    static Material spriteTemplate;

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
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.0f);
        }
        return material;
    }

    public static Material CreateSpriteMaterial()
    {
        if (SpriteTemplate != null)
            return new Material(SpriteTemplate);

        Shader shader = Shader.Find("Sprites/Default");
        return shader != null ? new Material(shader) : null;
    }
}
