#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Ensures Android CI uses URP instead of silently building with the Built-in pipeline.
/// URP assets are created through the package's own factory API (not hand-written YAML).
/// </summary>
public sealed class AshUrpBuildSetup : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return -1000; } }

    [InitializeOnLoadMethod]
    static void ScheduleSetup()
    {
        EditorApplication.delayCall += EnsureUrpAsset;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureUrpAsset();
        if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
            throw new BuildFailedException("Chronicles of Ash requires an active Universal Render Pipeline asset. Check Assets/Settings/AshMobileURP.asset.");
        EnsureRuntimeMaterial();
    }

    // Keep the Resources material aligned with URP. The previous committed material
    // referenced Unity's Built-in Standard shader, even though this project builds with URP.
    // Because runtime-created objects clone this material, the mismatch could make the
    // generated world render incorrectly on Android despite a successful APK build.
    static void EnsureRuntimeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new BuildFailedException("URP/Lit shader is unavailable; refusing to build a world with incompatible runtime materials.");

        const string path = "Assets/Resources/AshRuntimeStandard.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            material.name = "AshRuntimeStandard";
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
        AssetDatabase.SaveAssets();
        Debug.Log("Chronicles of Ash: runtime material uses " + material.shader.name + " at " + path + ".");
    }

    static void EnsureUrpAsset()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
        {
            EnsureRuntimeMaterial();
            return;
        }

        const string folder = "Assets/Settings";
        const string pipelinePath = folder + "/AshMobileURP.asset";
        const string rendererPath = folder + "/AshMobileURP_Renderer.asset";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (existing == null)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
            if (renderer == null)
            {
                // Prefer URP's own renderer factory so required shader resources are reloaded.
                MethodInfo rendererFactory = null;
                MethodInfo[] rendererFactories = typeof(UniversalRenderPipelineAsset).GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (MethodInfo candidate in rendererFactories)
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (candidate.Name == "CreateRendererAsset" && parameters.Length >= 2 &&
                        parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType.IsEnum)
                    {
                        rendererFactory = candidate;
                        break;
                    }
                }
                if (rendererFactory != null)
                {
                    try
                    {
                        ParameterInfo[] parameters = rendererFactory.GetParameters();
                        object[] args = new object[parameters.Length];
                        args[0] = pipelinePath;
                        args[1] = Enum.Parse(parameters[1].ParameterType, "UniversalRenderer");
                        for (int i = 2; i < parameters.Length; i++)
                            args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue :
                                (parameters[i].ParameterType == typeof(bool) ? (object)true :
                                parameters[i].ParameterType == typeof(string) ? (object)"Renderer" : null);
                        renderer = rendererFactory.Invoke(null, args) as ScriptableRendererData;
                    }
                    catch (Exception exception) { Debug.LogWarning("URP renderer factory did not initialize: " + exception.Message); }
                }
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    renderer.name = "AshMobileURP_Renderer";
                    AssetDatabase.CreateAsset(renderer, rendererPath);
                }
            }

            UniversalRenderPipelineAsset pipeline = null;
            // URP's own factory initializes its internal renderer list and default resources.
            MethodInfo factory = null;
            MethodInfo[] factories = typeof(UniversalRenderPipelineAsset).GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (MethodInfo candidate in factories)
            {
                ParameterInfo[] parameters = candidate.GetParameters();
                if (candidate.Name == "Create" && parameters.Length == 1 &&
                    parameters[0].ParameterType.IsAssignableFrom(renderer.GetType()))
                {
                    factory = candidate;
                    break;
                }
            }
            if (factory != null)
            {
                try { pipeline = factory.Invoke(null, new object[] { renderer }) as UniversalRenderPipelineAsset; }
                catch (Exception exception) { Debug.LogWarning("URP factory did not initialize: " + exception.Message); }
            }

            if (pipeline == null)
            {
                Debug.LogError("Could not initialize the URP pipeline asset with this installed package version. The build will stop rather than silently ship without URP post-processing.");
                return;
            }

            pipeline.name = "AshMobileURP";
            pipeline.supportsHDR = true;
            pipeline.shadowDistance = 55f;
            pipeline.shadowCascadeCount = 2;
            pipeline.renderScale = 1f;
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
            AssetDatabase.SaveAssets();
            existing = pipeline;
        }

        GraphicsSettings.defaultRenderPipeline = existing;
        QualitySettings.renderPipeline = existing;
        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();
        Debug.Log("Chronicles of Ash: activated URP mobile pipeline and assigned AshMobileURP.asset.");
    }
}
#endif
