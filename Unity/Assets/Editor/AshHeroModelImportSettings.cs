using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>Import the assembled CC0 FBX as a legacy Animation rig for runtime clip switching.</summary>
public sealed class AshHeroModelImportSettings : AssetPostprocessor
{
    const string HeroAssetPath = "Assets/Resources/Characters/AshHero.fbx";

    void OnPreprocessModel()
    {
        if (!assetPath.EndsWith("/Resources/Characters/AshHero.fbx", StringComparison.OrdinalIgnoreCase)) return;
        var importer = (ModelImporter)assetImporter;
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.isReadable = false;
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (!importedAssets.Any(path => string.Equals(path, HeroAssetPath, StringComparison.OrdinalIgnoreCase))) return;
        WriteVerificationReport();
    }

    static void WriteVerificationReport()
    {
        var report = new StringBuilder();
        var failures = new StringBuilder();
        var fbxPath = Path.GetFullPath(Path.Combine(Application.dataPath, "Resources/Characters/AshHero.fbx"));
        report.AppendLine("Chronicles of Ash — KnightCharacter FBX import verification");
        report.AppendLine("Imported asset: " + HeroAssetPath);

        if (File.Exists(fbxPath))
        {
            var info = new FileInfo(fbxPath);
            report.AppendLine("FBX bytes: " + info.Length);
            string importedSha256;
            using (var stream = File.OpenRead(fbxPath))
            using (var sha = SHA256.Create())
                importedSha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            report.AppendLine("Imported FBX SHA-256: " + importedSha256);

            string sourceManifest = Path.Combine(Path.GetDirectoryName(fbxPath), "ASSET_SOURCES.txt");
            if (File.Exists(sourceManifest))
            {
                var sourceLines = File.ReadAllLines(sourceManifest);
                foreach (var line in sourceLines.Where(value => value.StartsWith("Original FBX:", StringComparison.Ordinal) ||
                    value.StartsWith("Original FBX bytes:", StringComparison.Ordinal) ||
                    value.StartsWith("Original FBX SHA-256:", StringComparison.Ordinal)))
                    report.AppendLine(line);
                var expectedHashLine = sourceLines.FirstOrDefault(value => value.StartsWith("Original FBX SHA-256:", StringComparison.Ordinal));
                if (expectedHashLine != null)
                {
                    var expectedHash = expectedHashLine.Substring("Original FBX SHA-256:".Length).Trim();
                    if (!string.Equals(importedSha256, expectedHash, StringComparison.OrdinalIgnoreCase))
                        failures.AppendLine("The staged AshHero.fbx SHA-256 differs from the downloaded source FBX.");
                }
                else
                {
                    failures.AppendLine("The source manifest has no original FBX SHA-256.");
                }
            }
            else
            {
                failures.AppendLine("The generated character asset source manifest is missing.");
            }
        }
        else
        {
            failures.AppendLine("The imported FBX file is missing on disk.");
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(HeroAssetPath);
        if (model == null)
        {
            failures.AppendLine("Unity could not load the imported FBX as a GameObject.");
        }
        else
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            report.AppendLine("SkinnedMeshRenderer count: " + renderers.Length);
            bool hasWeightedRig = false;
            foreach (var renderer in renderers)
            {
                var mesh = renderer.sharedMesh;
                int vertices = mesh != null ? mesh.vertexCount : 0;
                int bindPoses = mesh != null ? mesh.bindposes.Length : 0;
                int bones = renderer.bones != null ? renderer.bones.Length : 0;
                report.AppendLine("  Renderer: " + renderer.name + "; mesh=" +
                    (mesh != null ? mesh.name : "<none>") + "; vertices=" + vertices +
                    "; bones=" + bones + "; bind poses=" + bindPoses +
                    "; root bone=" + (renderer.rootBone != null ? renderer.rootBone.name : "<none>"));
                if (vertices > 0 && bones > 0 && bindPoses > 0) hasWeightedRig = true;
            }
            if (!hasWeightedRig)
                failures.AppendLine("No SkinnedMeshRenderer has a mesh, bones, and skin bind poses.");

            var importedClips = AssetDatabase.LoadAllAssetsAtPath(HeroAssetPath)
                .OfType<AnimationClip>()
                .ToArray();
            report.AppendLine("Imported AnimationClip subassets: " + importedClips.Length);
            foreach (var clip in importedClips)
            {
                int curves = AnimationUtility.GetCurveBindings(clip).Length + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
                report.AppendLine("  Clip asset: " + clip.name + "; length=" + clip.length.ToString("0.###") +
                    "s; frame rate=" + clip.frameRate.ToString("0.###") + "; keyframe curves=" + curves +
                    "; legacy=" + clip.legacy);
            }

            var animationStates = model.GetComponentsInChildren<Animation>(true)
                .SelectMany(animation => EnumerateStates(animation))
                .ToArray();
            report.AppendLine("Legacy Animation state count: " + animationStates.Length);
            foreach (var state in animationStates)
            {
                var clip = state.clip;
                int curves = clip != null
                    ? AnimationUtility.GetCurveBindings(clip).Length + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length
                    : 0;
                report.AppendLine("  State: " + state.name + "; clip=" +
                    (clip != null ? clip.name : "<none>") + "; length=" +
                    (clip != null ? clip.length.ToString("0.###") : "0") +
                    "s; frame rate=" + (clip != null ? clip.frameRate.ToString("0.###") : "0") +
                    "; keyframe curves=" + curves + "; legacy=" + (clip != null && clip.legacy));
            }

            foreach (var required in new[] { "Idle", "Walk", "Run" })
            {
                var state = animationStates.FirstOrDefault(item =>
                    item.name.IndexOf(required, StringComparison.OrdinalIgnoreCase) >= 0);
                var clip = state != null ? state.clip : null;
                int curves = clip != null
                    ? AnimationUtility.GetCurveBindings(clip).Length + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length
                    : 0;
                bool valid = state != null && clip != null && clip.legacy && clip.length > 0f && curves > 0;
                report.AppendLine(required + " clip: " + (valid ? "PASS — " + state.name : "FAIL — no usable legacy Animation state with keyed curves"));
                if (!valid) failures.AppendLine("Missing usable " + required + " clip in the imported legacy Animation states.");
            }
        }

        report.AppendLine("Result: " + (failures.Length == 0 ? "PASS" : "FAIL"));
        if (failures.Length > 0)
        {
            report.AppendLine("Failures:");
            report.Append(failures);
        }

        var reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/KnightCharacter-verification.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, report.ToString());
        if (failures.Length == 0)
            Debug.Log("[KnightCharacter verification]\n" + report);
        else
            Debug.LogError("[KnightCharacter verification]\n" + report);
    }

    static System.Collections.Generic.IEnumerable<AnimationState> EnumerateStates(Animation animation)
    {
        foreach (AnimationState state in animation) yield return state;
    }
}

