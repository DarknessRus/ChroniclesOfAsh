#!/usr/bin/env python3
"""Fetch CC0 2K PBR maps into Unity Resources for the Android build.

Local filenames retain legacy suffixes because AshRealisticWorldVisuals loads
these paths by name. Downloaded texture images are 1K/2K, never above 2048px.
"""
from pathlib import Path
from urllib.error import HTTPError
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
    ("aerial_rocks_02", "aerial_rocks_02", "diff", "Unity/Assets/Resources/PBR/MountainRock/aerial_rocks_02_diff_2k.jpg"),
    ("aerial_rocks_02", "aerial_rocks_02", "nor_dx", "Unity/Assets/Resources/PBR/MountainRock/aerial_rocks_02_nor_dx_2k.jpg"),
    ("aerial_rocks_02", "aerial_rocks_02", "rough", "Unity/Assets/Resources/PBR/MountainRock/aerial_rocks_02_rough_2k.jpg"),
    ("bark_brown_02", "bark_brown_02", "diff", "Unity/Assets/Resources/PBR/Bark/bark_brown_02_diff_2k.jpg"),
    ("bark_brown_02", "bark_brown_02", "nor_dx", "Unity/Assets/Resources/PBR/Bark/bark_brown_02_nor_dx_2k.jpg"),
    ("bark_brown_02", "bark_brown_02", "rough", "Unity/Assets/Resources/PBR/Bark/bark_brown_02_rough_2k.jpg"),
    ("forest_ground_01", "forest_ground_01", "diff", "Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_diff_2k.jpg"),
    ("forest_ground_01", "forest_ground_01", "nor_dx", "Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_nor_dx_2k.jpg"),
    ("forest_ground_01", "forest_ground_01", "rough", "Unity/Assets/Resources/PBR/ForestGround/forest_ground_01_rough_2k.jpg"),
    ("snow_02", "snow_02", "diff", "Unity/Assets/Resources/PBR/Snow/snow_02_diff_2k.jpg"),
    ("snow_02", "snow_02", "nor_dx", "Unity/Assets/Resources/PBR/Snow/snow_02_nor_dx_2k.jpg"),
    ("snow_02", "snow_02", "rough", "Unity/Assets/Resources/PBR/Snow/snow_02_rough_2k.jpg"),
)

downloaded = 0
downloaded_bytes = 0
failed = []

for source_asset, local_prefix, map_name, relative_path in MAPS:
    url = f"{BASE}/{source_asset}/{map_name}_2k.jpg"
    destination = ROOT / relative_path
    destination.parent.mkdir(parents=True, exist_ok=True)
    request = Request(url, headers={"User-Agent": "ChroniclesOfAsh-Android-Build/1.0"})
    try:
        with urlopen(request, timeout=90) as response:
            data = response.read()
    except HTTPError as error:
        if error.code == 404:
            failed.append(f"{url} (HTTP 404)")
            continue
        raise
    if len(data) < 1024 or not data.startswith(bytes((0xFF, 0xD8, 0xFF))):
        failed.append(f"{url} (not a valid JPEG or unexpectedly small)")
        continue
    destination.write_bytes(data)
    downloaded += 1
    downloaded_bytes += len(data)
    print(f"Downloaded 2K {local_prefix} {map_name}: {len(data):,} bytes -> {relative_path}")

if failed:
    print("\nSome 2K PBR maps could not be downloaded:")
    for item in failed:
        print(f" - {item}")
    raise SystemExit("PBR download incomplete; refusing to silently build with stale maps.")

print(f"\nDownloaded {downloaded}/{len(MAPS)} 2K PBR maps ({downloaded_bytes:,} bytes total).")
