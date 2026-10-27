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
    }

    static void EnsureUrpAsset()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            return;

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
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "AshMobileURP_Renderer";
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            UniversalRenderPipelineAsset pipeline = null;
            // URP's own factory initializes its internal renderer list and default resources.
            MethodInfo factory = typeof(UniversalRenderPipelineAsset).GetMethod(
                "Create", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, new Type[] { typeof(ScriptableRendererData) }, null);
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
            pipeline.supportsMainLightShadows = true;
            pipeline.supportsAdditionalLightShadows = false;
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
