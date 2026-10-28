#!/usr/bin/env python3
"""Fetch Quaternius's official CC0 knight pack and stage its FBX for Unity."""
from pathlib import Path
import shutil, subprocess, sys, zipfile

ROOT = Path(__file__).resolve().parents[1]
DOWNLOAD = ROOT / "build" / "quaternius-knight"
OUT = ROOT / "Unity/Assets/Resources/Characters"
DOWNLOAD.mkdir(parents=True, exist_ok=True)
OUT.mkdir(parents=True, exist_ok=True)
FOLDER = "https://drive.google.com/drive/folders/1QVyfCJkq70mAwMIh1cGq1xfHp2LN5GmK?usp=sharing"

# Quaternius links this official Drive folder from the CC0 pack page.
result = subprocess.run(
    [sys.executable, "-m", "gdown", "--folder", FOLDER, "-O", str(DOWNLOAD)],
    text=True,
)
if result.returncode:
    raise SystemExit("Could not download the official Quaternius CC0 animated knight pack.")

# The official Drive folder contains individual FBX files directly (not a ZIP).
# Still support ZIP archives if Quaternius changes the folder layout later.
archives = list(DOWNLOAD.rglob("*.zip"))
extracted = DOWNLOAD / "extracted"
if archives:
    extracted.mkdir(parents=True, exist_ok=True)
    for archive in archives:
        with zipfile.ZipFile(archive) as z:
            z.extractall(extracted)

fbx_files = list(DOWNLOAD.rglob("*.fbx"))
fbx_files = [p for p in fbx_files if extracted not in p.parents or p.is_file()]
if not fbx_files:
    raise RuntimeError("Quaternius download contained no FBX model/animation file.")

def rank(path):
    name = path.name.lower()
    return (
        1 if "knightcharacter" in name or ("knight" in name and "character" in name) else
        1 if "knight" in name else 0,
        path.stat().st_size,
    )

source = sorted(fbx_files, key=rank, reverse=True)[0]
if source.stat().st_size < 100_000:
    raise RuntimeError(f"Selected FBX is implausibly small: {source} ({source.stat().st_size} bytes)")

target = OUT / "AshHero.fbx"
shutil.copy2(source, target)
(OUT / "ASSET_SOURCES.txt").write_text(
    "Chronicles of Ash external skinned hero\n"
    "Asset: LowPoly Animated Knight / Animated Knight Pack by Quaternius.\n"
    "License: CC0 1.0 Universal, confirmed on the official pack page.\n"
    "Official source: https://quaternius.itch.io/lowpoly-animated-knight\n"
    "Official pack page: https://quaternius.com/packs/knightcharacter.html\n"
    "Original FBX copied to AshHero.fbx during CI; not procedurally fabricated.\n",
    encoding="utf-8",
)
print(f"Staged Quaternius FBX: {target} ({target.stat().st_size:,} bytes)")
print("FBX source:", source)
