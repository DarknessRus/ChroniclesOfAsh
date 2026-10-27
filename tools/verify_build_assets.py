#!/usr/bin/env python3
"""Fail fast if the reference artwork and real 4K world maps are present."""
from pathlib import Path
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = [
    Path("Unity/Assets/Resources/Menu/HeroSelectionBackground.png"),
    Path("Unity/Assets/Resources/PBR/GrassGround/grass_ground_diff_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/GrassGround/grass_ground_nor_dx_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_diff_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_nor_dx_1k.jpg"),
    Path("Unity/Assets/Resources/PBR/Bark/bark_brown_02_diff_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Bark/bark_brown_02_nor_dx_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_diff_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_nor_dx_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_rough_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Snow/snow_02_diff_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Snow/snow_02_nor_dx_2k.jpg"),
    Path("Unity/Assets/Resources/PBR/Snow/snow_02_rough_2k.jpg"),
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
            if width < 3840 or height < 3840:
                missing.append(f"{relative} (expected 4K, found {width}x{height})")
                continue
            pbr_count += 1
            print(f"OK 4K {relative}: {width}x{height}, {size:,} bytes")
        except Exception as error:
            missing.append(f"{relative} (invalid image: {error})")
            continue
    else:
        print(f"OK {relative}: {size:,} bytes")
    total_bytes += size

if missing:
    print("\nRESOURCE PREFLIGHT FAILED:", file=sys.stderr)
    for item in missing:
        print(f" - {item}", file=sys.stderr)
    sys.exit(1)

print(f"\nResource preflight passed: {len(REQUIRED)} required files, {pbr_count} verified 4K PBR maps, {total_bytes:,} bytes total.")
