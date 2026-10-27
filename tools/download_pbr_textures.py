#!/usr/bin/env python3
from pathlib import Path
from urllib.request import Request, urlopen
ROOT=Path(__file__).resolve().parent.parent
BASE="https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k"
MAPS=(("grass_ground","diff","Unity/Assets/Resources/PBR/GrassGround/grass_ground_diff_1k.jpg"),("grass_ground","nor_dx","Unity/Assets/Resources/PBR/GrassGround/grass_ground_nor_dx_1k.jpg"),("grass_ground","rough","Unity/Assets/Resources/PBR/GrassGround/grass_ground_rough_1k.jpg"),("rocks_ground_02","diff","Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_diff_1k.jpg"),("rocks_ground_02","nor_dx","Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_nor_dx_1k.jpg"),("rocks_ground_02","rough","Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_rough_1k.jpg"),("rocks_ground_02","disp","Unity/Assets/Resources/PBR/RockGround/rocks_ground_02_disp_1k.jpg"))
for asset,kind,relative in MAPS:
 url=f"{BASE}/{asset}/{asset}_{kind}_1k.jpg"
 path=ROOT/relative; path.parent.mkdir(parents=True,exist_ok=True)
 with urlopen(Request(url,headers={"User-Agent":"ChroniclesOfAsh-Android-Build/1.0"}),timeout=60) as response: data=response.read()
 if len(data)<1024 or not data.startswith(bytes((255,216,255))): raise RuntimeError(f"Invalid texture response: {url}")
 path.write_bytes(data); print(f"Downloaded {asset} {kind}: {len(data)} bytes")
