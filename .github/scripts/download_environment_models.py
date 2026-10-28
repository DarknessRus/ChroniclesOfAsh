#!/usr/bin/env python3
"""Download a small, mobile-conscious set of CC0 environment models from Poly Haven.

The official Poly Haven files API supplies direct FBX download URLs. Model archives are
kept at 1K texture resolution where available, unpacked into Unity Resources so Unity's
FBX importer brings the meshes and their companion maps into the Android build.
"""
from __future__ import annotations

import json
import shutil
import sys
import urllib.error
import urllib.request
import zipfile
from pathlib import Path
from urllib.parse import urlparse

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Unity/Assets/Resources/EnvironmentModels"
API = "https://api.polyhaven.com/files/"
ASSETS = (
    # Evergreen trees suitable for the valley forests.
    ("fir_tree_01", "conifer"),
    ("pine_tree_01", "conifer"),
    # Mossy boulders and a broken rock face for the cliffs and riverbanks.
    ("rock_moss_set_01", "rocks"),
    ("rock_face_02", "cliffs"),
    # Dead wood and ground cover for forest-floor variation.
    ("dead_tree_trunk_02", "deadwood"),
    ("moss_01", "groundcover"),
)
USER_AGENT = "ChroniclesOfAsh-EnvironmentAssetPrep/1.0"


def fetch(url: str, timeout: int = 120) -> bytes:
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(req, timeout=timeout) as response:
        return response.read()


def collect_urls(value, path=()):
    """Yield (key path, URL) pairs from Poly Haven's nested API response."""
    if isinstance(value, dict):
        for key, child in value.items():
            next_path = path + (str(key).lower(),)
            if key == "url" and isinstance(child, str) and child.startswith("https://"):
                yield next_path, child
            else:
                yield from collect_urls(child, next_path)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from collect_urls(child, path + (str(index),))


def choose_fbx_url(payload: dict) -> str:
    candidates = []
    for key_path, url in collect_urls(payload):
        joined = "/".join(key_path)
        if "fbx" not in joined:
            continue
        # Prefer the 1K download, then 2K; prefer an archive URL if multiple are listed.
        score = 100 if "1k" in joined else 70 if "2k" in joined else 10
        suffix = urlparse(url).path.lower()
        if suffix.endswith(".zip"):
            score += 15
        candidates.append((score, url))
    if not candidates:
        raise RuntimeError("Poly Haven API returned no FBX download URL")
    candidates.sort(reverse=True)
    return candidates[0][1]


def extract_asset(asset_id: str, category: str, archive_data: bytes) -> int:
    target = DEST / category / asset_id
    target.mkdir(parents=True, exist_ok=True)
    archive_path = target / "_download.zip"
    archive_path.write_bytes(archive_data)
    extracted = 0
    try:
        with zipfile.ZipFile(archive_path) as archive:
            for item in archive.infolist():
                if item.is_dir():
                    continue
                name = Path(item.filename).name
                suffix = Path(name).suffix.lower()
                # Ignore hidden metadata, previews and unrelated formats; retain FBX,
                # image maps and material sidecars needed by Unity's importer.
                if name.startswith(".") or suffix not in {
                    ".fbx", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".tga",
                    ".mtl", ".txt", ".json"
                }:
                    continue
                # Flatten each archive into its own isolated model folder, avoiding
                # duplicate texture names colliding between different assets.
                output_name = f"{asset_id}_{name}" if suffix != ".fbx" else f"{asset_id}.fbx"
                out = target / output_name
                with archive.open(item) as source, out.open("wb") as destination:
                    shutil.copyfileobj(source, destination)
                extracted += 1
    finally:
        archive_path.unlink(missing_ok=True)
    if not any(p.suffix.lower() == ".fbx" for p in target.iterdir()):
        raise RuntimeError(f"{asset_id}: archive contained no FBX file")
    return extracted


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    total_files = 0
    total_bytes = 0
    failures = []
    for asset_id, category in ASSETS:
        print(f"Resolving CC0 model: {asset_id}", flush=True)
        try:
            payload = json.loads(fetch(API + asset_id).decode("utf-8"))
            url = choose_fbx_url(payload)
            archive = fetch(url)
            if len(archive) < 1024:
                raise RuntimeError(f"download unexpectedly small ({len(archive)} bytes)")
            if not zipfile.is_zipfile(__import__("io").BytesIO(archive)):
                raise RuntimeError("the selected FBX download is not a ZIP archive")
            count = extract_asset(asset_id, category, archive)
            total_files += count
            total_bytes += len(archive)
            print(f"OK {asset_id}: {count} model/texture files, {len(archive):,} downloaded bytes")
        except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError,
                json.JSONDecodeError, RuntimeError, zipfile.BadZipFile) as error:
            failures.append(f"{asset_id}: {error}")
            print(f"FAILED {asset_id}: {error}", file=sys.stderr, flush=True)

    # Preserve license and provenance beside the downloaded resources.
    (DEST / "ASSET_SOURCES.txt").write_text(
        "Chronicles of Ash environment models\n"
        "Source: Poly Haven official public files API (https://api.polyhaven.com/)\n"
        "License: CC0 1.0 Universal; commercial use and modification permitted.\n"
        "Assets: fir_tree_01, pine_tree_01, rock_moss_set_01, rock_face_02, "
        "dead_tree_trunk_02, moss_01\n"
        "Asset pages: https://polyhaven.com/a/fir_tree_01 ; "
        "https://polyhaven.com/a/pine_tree_01 ; "
        "https://polyhaven.com/a/rock_moss_set_01 ; "
        "https://polyhaven.com/a/rock_face_02 ; "
        "https://polyhaven.com/a/dead_tree_trunk_02 ; "
        "https://polyhaven.com/a/moss_01\n",
        encoding="utf-8",
    )
    if failures:
        print("\nEnvironment asset download incomplete:", file=sys.stderr)
        for failure in failures:
            print(" - " + failure, file=sys.stderr)
        raise SystemExit(1)
    print(f"\nEnvironment download complete: {len(ASSETS)} CC0 models, "
          f"{total_files} model/texture files, {total_bytes:,} archive bytes.")


if __name__ == "__main__":
    main()
