#!/usr/bin/env python3
"""Fetch high-detail CC0 2K PBR maps into Unity Resources for an offline Android build.

The Unity runtime keeps the historical resource filenames for compatibility, but
the actual downloaded source maps are 2K, not 1K.
"""
from pathlib import Path
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parent.parent
BASE = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k"
MAPS = (
    ("grass_ground", "grass_ground", "diff", "Unity/Assets/Resources/PBR/GrassGround/grass_ground_diff_1k.jpg"),
    ("grass_ground", "grass_ground", "nor_dx", "Unity/Assets/Resources/PBR/GrassGround/grass_ground_nor_dx_1k.jpg"),
    ("grass_ground", "grass_ground", "rough", "Unity/Assets/Resources/PBR/GrassGround/grass_ground_rough_1k.jpg"),
    ("rock_ground_02", "rocks_ground_02", "diff", "Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_diff_1k.jpg"),
    ("rock_ground_02", "rocks_ground_02", "nor_dx", "Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_nor_dx_1k.jpg"),
    ("rock_ground_02", "rocks_ground_02", "rough", "Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_rough_1k.jpg"),
    ("rock_ground_02", "rocks_ground_02", "disp", "Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_disp_1k.jpg"),
)

for source_asset, local_prefix, map_name, relative_path in MAPS:
    url = f"{BASE}/{source_asset}/{source_asset}_{map_name}_2k.jpg"
    destination = ROOT / relative_path
    destination.parent.mkdir(parents=True, exist_ok=True)
    request = Request(url, headers={"User-Agent": "ChroniclesOfAsh-Android-Build/1.0"})
    with urlopen(request, timeout=60) as response:
        data = response.read()
    if len(data) < 1024 or not data.startswith(bytes((0xFF, 0xD8, 0xFF))):
        raise RuntimeError(f"Texture download was invalid: {url}")
    destination.write_bytes(data)
    print(f"Downloaded {local_prefix} {map_name} from 2K source: {len(data)} bytes")
