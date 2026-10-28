#!/usr/bin/env python3
"""Download a mobile-conscious set of CC0 environment models from Poly Haven's API."""
from __future__ import annotations

import json
import sys
import urllib.error
import urllib.request
from pathlib import Path
from urllib.parse import urlparse

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "Unity/Assets/Resources/EnvironmentModels"
API = "https://api.polyhaven.com/files/"
ASSETS = (
    ("fir_tree_01", "conifer"),
    ("pine_tree_01", "conifer"),
    ("rock_moss_set_01", "rocks"),
    ("rock_face_02", "cliffs"),
    ("dead_tree_trunk_02", "deadwood"),
    ("moss_01", "groundcover"),
)
USER_AGENT = "ChroniclesOfAsh-EnvironmentAssetPrep/1.0"
IMAGE_EXTS = {".jpg", ".jpeg", ".png", ".tif", ".tiff", ".tga", ".exr"}
MAX_FILE_BYTES = 90 * 1024 * 1024


def fetch(url: str, timeout: int = 120) -> bytes:
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(req, timeout=timeout) as response:
        return response.read()


def collect_records(value, path=()):
    """Yield nested Poly Haven terminal download records and their key paths."""
    if isinstance(value, dict):
        if isinstance(value.get("url"), str) and value["url"].startswith("https://"):
            yield path, value
        else:
            for key, child in value.items():
                yield from collect_records(child, path + (str(key).lower(),))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from collect_records(child, path + (str(index),))


def select_model_and_maps(payload: dict):
    records = list(collect_records(payload))
    fbx = []
    maps = []
    for path, record in records:
        path_text = "/".join(path).lower()
        url = record["url"]
        ext = Path(urlparse(url).path).suffix.lower()
        if "fbx" in path_text and ext == ".fbx":
            score = 100 if "1k" in path_text else 80 if "2k" in path_text else 10
            fbx.append((score, record))
        elif ext in IMAGE_EXTS and "1k" in path_text:
            # The Android build uses 1K maps to control APK size and memory use.
            maps.append((100, path, record))
    if not fbx:
        raise RuntimeError("Poly Haven API returned no direct FBX file URL")
    fbx.sort(key=lambda item: item[0], reverse=True)
    maps.sort(key=lambda item: (item[0], "/".join(item[1])), reverse=True)
    return fbx[0][1], maps


def save_download(url: str, path: Path, expected_size=None) -> int:
    size_hint = expected_size if isinstance(expected_size, int) else None
    if size_hint is not None and size_hint > MAX_FILE_BYTES:
        raise RuntimeError(f"refusing oversized file ({size_hint:,} bytes): {url}")
    data = fetch(url)
    if len(data) < 1024:
        raise RuntimeError(f"download unexpectedly small ({len(data)} bytes): {url}")
    if len(data) > MAX_FILE_BYTES:
        raise RuntimeError(f"refusing oversized file ({len(data):,} bytes): {url}")
    # HTML error pages occasionally arrive with a successful status; reject them.
    if data[:64].lstrip().lower().startswith((b"<!doctype html", b"<html")):
        raise RuntimeError(f"server returned HTML instead of an asset: {url}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    return len(data)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    total_files = 0
    total_bytes = 0
    failures = []
    for asset_id, category in ASSETS:
        target = DEST / category / asset_id
        target.mkdir(parents=True, exist_ok=True)
        print(f"Resolving CC0 model: {asset_id}", flush=True)
        try:
            payload = json.loads(fetch(API + asset_id).decode("utf-8"))
            model_record, maps = select_model_and_maps(payload)
            model_path = target / f"{asset_id}.fbx"
            model_size = save_download(model_record["url"], model_path, model_record.get("size"))
            total_files += 1
            total_bytes += model_size
            print(f"  FBX: {model_size:,} bytes -> {model_path}", flush=True)

            # The FBX references companion images by basename. Keep their original names
            # inside this model-specific folder so the Unity importer can resolve them.
            downloaded_names = set()
            for _, _, record in maps:
                url = record["url"]
                filename = Path(urlparse(url).path).name
                if not filename or filename in downloaded_names:
                    continue
                downloaded_names.add(filename)
                size = save_download(url, target / filename, record.get("size"))
                total_files += 1
                total_bytes += size
            print(f"  Companion texture maps: {len(downloaded_names)}", flush=True)
        except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError,
                json.JSONDecodeError, RuntimeError) as error:
            failures.append(f"{asset_id}: {error}")
            print(f"FAILED {asset_id}: {error}", file=sys.stderr, flush=True)

    (DEST / "ASSET_SOURCES.txt").write_text(
        "Chronicles of Ash environment models\n"
        "Source: Poly Haven official public API (https://api.polyhaven.com/)\n"
        "License: CC0 1.0 Universal. The API requires a clear Poly Haven credit in the product.\n"
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
          f"{total_files} files, {total_bytes:,} downloaded bytes.")


if __name__ == "__main__":
    main()
