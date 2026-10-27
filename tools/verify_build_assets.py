#!/usr/bin/env python3
"""Verify required world assets and ensure PBR JPEGs are 1K or 2K, never above 2K."""
from pathlib import Path
import json
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = [
    Path("Unity/Assets/Resources/Menu/HeroSelectionBackground.png"),
    Path("Unity/Assets/Resources/PBR/GrassGround/grass_ground_diff_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/GrassGround/grass_ground_nor_dx_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/GrassGround/grass_ground_rough_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_diff_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_nor_dx_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_rough_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_disp_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/MountainRock/aerial_rocks_02_diff_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/MountainRock/aerial_rocks_02_nor_dx_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/MountainRock/aerial_rocks_02_rough_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Bark/bark_brown_02_diff_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Bark/bark_brown_02_nor_dx_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Bark/bark_brown_02_rough_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_diff_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_nor_dx_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_rough_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Snow/snow_02_diff_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Snow/snow_02_nor_dx_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Snow/snow_02_rough_2k.jpg"),
]

CODE_REQUIRED = [
    Path("Unity/Assets/Scripts/AshDarkFantasyPresentation.cs"),
    Path("Unity/Assets/Scripts/AshOpenWorld.cs"),
    Path("Unity/Assets/Scripts/AshPlayerController.cs"),
    Path("Unity/Assets/Editor/AshUrpBuildSetup.cs"),
    Path("Unity/Packages/manifest.json"),
]

missing = []
total_bytes = 0
pbr_count = 0
for relative in REQUIRED:
    path = ROOT / relative
    if not path.is_file():
        missing.append(f"{relative} (missing)")
        continue
    size = path.stat().st_size
    if size < 1024:
        missing.append(f"{relative} (only {size} bytes)")
        continue
    if path.suffix.lower() == ".jpg":
        try:
            with Image.open(path) as image:
                width, height = image.size
                image.verify()
            if width > 2048 or height > 2048 or min(width, height) < 1024:
                missing.append(f"{relative} (expected 1K/2K, found {width}x{height})")
                continue
            pbr_count += 1
            print(f"OK <=2K {relative}: {width}x{height}, {size:,} bytes")
        except Exception as error:
            missing.append(f"{relative} (invalid image: {error})")
            continue
    else:
        print(f"OK {relative}: {size:,} bytes")
    total_bytes += size
for relative in CODE_REQUIRED:
    path = ROOT / relative
    if not path.is_file() or path.stat().st_size < 100:
        missing.append(f"{relative} (missing or unexpectedly small)")
        continue
    print(f"OK code/config {relative}: {path.stat().st_size:,} bytes")

manifest_path = ROOT / "Unity/Packages/manifest.json"
try:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if manifest.get("dependencies", {}).get("com.unity.render-pipelines.universal") != "14.0.11":
        missing.append("Unity/Packages/manifest.json (URP 14.0.11 dependency not configured)")
except Exception as error:
    missing.append(f"Unity/Packages/manifest.json (invalid JSON: {error})")

world_script = (ROOT / "Unity/Assets/Scripts/AshOpenWorld.cs").read_text(encoding="utf-8")
if "AshDarkFantasyPresentation.Apply(root);" not in world_script:
    missing.append("AshOpenWorld.cs (dark fantasy presentation hook is missing)")
combat_script = (ROOT / "Unity/Assets/Scripts/AshPlayerController.cs").read_text(encoding="utf-8")
if "AshDarkFantasyPresentation.SpawnDarkSpell(" not in combat_script:
    missing.append("AshPlayerController.cs (spell hit VFX hook is missing)")

if missing:
    print("\nRESOURCE PREFLIGHT FAILED:", file=sys.stderr)
    for item in missing:
        print(f" - {item}", file=sys.stderr)
    sys.exit(1)
print(f"\nResource preflight passed: {len(REQUIRED)} required assets, {len(CODE_REQUIRED)} required code/config files, {pbr_count} verified <=2K PBR maps, {total_bytes:,} bytes asset data.")
