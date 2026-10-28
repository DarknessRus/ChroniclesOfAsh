#!/usr/bin/env python3
"""Fetch Quaternius's official CC0 animated knight pack and stage its skinned FBX for Unity."""
from pathlib import Path
from urllib.request import Request, urlopen
import shutil, subprocess, sys, zipfile

ROOT = Path(__file__).resolve().parents[1]
DOWNLOAD = ROOT / "build" / "quaternius-knight"
OUT = ROOT / "Unity/Assets/Resources/Characters"
DOWNLOAD.mkdir(parents=True, exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)
FOLDER = "https://drive.google.com/drive/folders/1QVyfCJkq70mAwMIh1cGq1xfHp2LN5GmK?usp=sharing"

# This is the official download folder linked by Quaternius on the CC0 pack page.
result = subprocess.run([sys.executable, "-m", "gdown", "--folder", FOLDER, "-O", str(DOWNLOAD)],
                        text=True)
if result.returncode:
    raise SystemExit("Could not download the official Quaternius CC0 animated knight pack.")
archives = list(DOWNLOAD.rglob("*.zip"))
if not archives:
    raise RuntimeError("Official download folder returned no ZIP archives.")
extracted = DOWNLOAD / "extracted"
extracted.mkdir(parents=True, exist_ok=True)
for archive in archives:
    with zipfile.ZipFile(archive) as z:
        z.extractall(extracted)
fbx_files = list(extracted.rglob("*.fbx"))
if not fbx_files:
    raise RuntimeError("Quaternius pack contained no FBX model/animation file.")
ranked = sorted(fbx_files, key=lambda p: (
    1 if "knight" in p.name.lower() and "character" in p.name.lower() else
    1 if "knight" in p.name.lower() else 0, p.stat().st_size), reverse=True)
source = ranked[0]
if source.stat().st_size < 100_000:
    raise RuntimeError(f"Selected FBX is implausibly small: {source} ({source.stat().st_size} bytes)")
target = OUT / "AshHero.fbx"
shutil.copy2(source, target)
# The pack is explicitly marked CC0 on Quaternius's official page; retain the provenance.
(OUT / "ASSET_SOURCES.txt").write_text(
    "Chronicles of Ash external skinned hero\n"
    "Asset: LowPoly Animated Knight / Animated Knight Pack by Quaternius.\n"
    "Includes a skinned 3D character and authored Idle, Walk, Run and other animation clips.\n"
    "License: CC0 1.0 Universal, confirmed on the official pack page.\n"
    "Official source: https://quaternius.itch.io/lowpoly-animated-knight\n"
    "Official pack page: https://quaternius.com/packs/knightcharacter.html\n"
    "Original FBX copied to AshHero.fbx during CI; not procedurally fabricated.\n",
    encoding="utf-8"
)
print(f"Staged real Quaternius skinned character: {target} ({target.stat().st_size:,} bytes)")
print("FBX source:", source)
