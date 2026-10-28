using UnityEditor;
using UnityEngine;

/// <summary>Import the assembled CC0 FBX as a legacy Animation rig for runtime clip switching.</summary>
public sealed class AshHeroModelImportSettings : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.EndsWith("/Resources/Characters/AshHero.fbx", System.StringComparison.OrdinalIgnoreCase)) return;
        var importer = (ModelImporter)assetImporter;
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importMaterials = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.isReadable = false;
    }
}
