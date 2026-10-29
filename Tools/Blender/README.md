# Ash Warrior — free 3D asset generation

This folder contains a Blender Python generator for the first articulated Warrior asset in Chronicles of Ash.

## What it creates

- A real polygonal 3D character built from separately articulated armor pieces: dark steel plate, aged brass trim, ember-lit visor/rune, leather, and a segmented cloak.
- A named armature with root, spine, head, arms, and legs, plus weighted skinned meshes for Unity.
- Three animation actions: `Idle`, `Walk`, and `Run`.
- A Unity FBX at `Unity/Assets/Resources/Characters/AshHero.fbx`, matching the existing `Resources.Load<GameObject>("Characters/AshHero")` path.

This is a free procedural first pass, not an image-to-3D reconstruction and not a finished sculpt. It intentionally avoids paid model-generation services and third-party assets. Compare the generated silhouette in Unity with `Unity/Assets/Menu/HeroSelectionBackground.png`; tune armor shapes and proportions if they differ from the reference artwork.

## Generate locally

Install Blender 3.6 or newer, then from the repository root run:

```bash
blender --background --python Tools/Blender/create_ash_warrior.py
```

The script creates the output directory and writes `ASSET_SOURCES.txt` with the generated FBX size and SHA-256 expected by the Unity import verifier. In Unity, keep both files together, allow the FBX to import, then inspect the imported hierarchy and the `Idle`, `Walk`, and `Run` clips in the Animation tab.

## Generate a downloadable FBX with GitHub Actions

Open **Actions → Generate Warrior 3D Model → Run workflow**. The workflow is manual-only and uploads `AshHero.fbx` plus `ASSET_SOURCES.txt` as an artifact; it does not trigger on every push. Download the artifact and copy both files to `Unity/Assets/Resources/Characters/` in your working checkout, then commit the binary and manifest through your normal review flow.

## Important limitations

- This procedural asset is an articulated armor model rather than a photorealistic organic character sculpt.
- The script produces no texture maps; it creates embedded PBR-style material settings. Dedicated 1K/2K texture maps can be added after the first import is verified.
- A successful export is not proof of correct Unity import or in-game animation. Verify those stages in the Unity Editor and on Android before replacing any existing working asset.
