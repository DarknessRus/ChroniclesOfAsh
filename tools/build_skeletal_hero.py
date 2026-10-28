#!/usr/bin/env python3
"""Fetch Quaternius's official CC0 knight FBX and stage it for Unity."""
from pathlib import Path, PurePosixPath
from html.parser import HTMLParser
import hashlib
import io
import json
import zipfile
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode, urlparse
from urllib.request import HTTPCookieProcessor, Request, build_opener

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Unity/Assets/Resources/Characters"
OUT.mkdir(parents=True, exist_ok=True)
PACK_PAGE = "https://quaternius.itch.io/lowpoly-animated-knight"
PURCHASE_PAGE = PACK_PAGE + "/purchase"

class DownloadPageParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.csrf_token = None
        self.upload_id = None

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "meta" and attrs.get("name") == "csrf_token":
            self.csrf_token = attrs.get("value")
        elif tag == "a" and "download_btn" in attrs.get("class", "").split():
            self.upload_id = attrs.get("data-upload_id")

def page_details(url, opener, referer=None):
    headers = {"User-Agent": "ChroniclesOfAsh asset downloader/1.0"}
    if referer:
        headers["Referer"] = referer
    request = Request(url, headers=headers)
    with opener.open(request, timeout=60) as response:
        final_url = response.geturl()
        body = response.read()
    parser = DownloadPageParser()
    parser.feed(body.decode("utf-8", errors="replace"))
    return final_url, parser

def post_json(url, fields, opener, referer):
    data = urlencode(fields).encode("utf-8")
    request = Request(url, data=data, headers={
        "User-Agent": "ChroniclesOfAsh asset downloader/1.0",
        "Referer": referer,
        "X-Requested-With": "XMLHttpRequest",
        "Content-Type": "application/x-www-form-urlencoded",
    })
    with opener.open(request, timeout=60) as response:
        result = json.loads(response.read().decode("utf-8"))
    if result.get("errors"):
        raise RuntimeError("Quaternius download service returned: " + "; ".join(result["errors"]))
    return result

def download_pack(opener):
    purchase_url, purchase_page = page_details(PURCHASE_PAGE, opener)
    if not purchase_page.csrf_token:
        raise RuntimeError("Could not read the CSRF token from Quaternius's free download page.")

    download_info = post_json(
        PACK_PAGE + "/download_url",
        {"csrf_token": purchase_page.csrf_token},
        opener,
        purchase_url,
    )
    download_page_url = download_info.get("url")
    if not download_page_url or urlparse(download_page_url).scheme != "https":
        raise RuntimeError("Quaternius did not return a secure download page URL.")

    download_page_url, download_page = page_details(download_page_url, opener, PURCHASE_PAGE)
    if not download_page.csrf_token or not download_page.upload_id:
        raise RuntimeError("Could not identify the Knight Character archive on Quaternius's download page.")

    file_info = post_json(
        PACK_PAGE + "/file/" + download_page.upload_id + "?source=game_download",
        {"csrf_token": download_page.csrf_token},
        opener,
        download_page_url,
    )
    archive_url = file_info.get("url")
    if not archive_url or urlparse(archive_url).scheme != "https":
        raise RuntimeError("Quaternius did not return a secure archive URL.")

    request = Request(archive_url, headers={"User-Agent": "ChroniclesOfAsh asset downloader/1.0"})
    with opener.open(request, timeout=120) as response:
        archive_bytes = response.read()
    if len(archive_bytes) > 50_000_000:
        raise RuntimeError(f"Quaternius archive is unexpectedly large: {len(archive_bytes):,} bytes")
    return archive_bytes


try:
    opener = build_opener(HTTPCookieProcessor())
    archive_bytes = download_pack(opener)
except (HTTPError, URLError, TimeoutError) as error:
    raise SystemExit(f"Could not download the official Quaternius CC0 animated knight pack: {error}")

try:
    with zipfile.ZipFile(io.BytesIO(archive_bytes)) as archive:
        source_member = next(
            (member for member in archive.infolist()
             if PurePosixPath(member.filename).name.lower() == "knightcharacter.fbx"
             and PurePosixPath(member.filename).parent.name.lower() == "fbx"
             and not member.is_dir()),
            None,
        )
        if source_member is None:
            raise RuntimeError("Quaternius archive contained no FBX/KnightCharacter.fbx.")
        if source_member.file_size < 100_000:
            raise RuntimeError(f"Selected FBX is implausibly small: {source_member.file_size:,} bytes")
        with archive.open(source_member) as source:
            fbx_bytes = source.read()
except zipfile.BadZipFile as error:
    raise RuntimeError("Quaternius returned an invalid ZIP archive.") from error

target = OUT / "AshHero.fbx"
target.write_bytes(fbx_bytes)
source_sha256 = hashlib.sha256(fbx_bytes).hexdigest()
(OUT / "ASSET_SOURCES.txt").write_text(
    "Chronicles of Ash external skinned hero\n"
    "Asset: LowPoly Animated Knight / Animated Knight Pack by Quaternius.\n"
    "License: CC0 1.0 Universal, confirmed on the official pack page.\n"
    "Official source: https://quaternius.itch.io/lowpoly-animated-knight\n"
    "Official pack page: https://quaternius.com/packs/knightcharacter.html\n"
    f"Original FBX: {source_member.filename}\n"
    f"Original FBX bytes: {len(fbx_bytes)}\n"
    f"Original FBX SHA-256: {source_sha256}\n"
    "Original FBX copied from Quaternius's official free download during CI; not procedurally fabricated.\n",
    encoding="utf-8",
)
print(f"Staged Quaternius FBX: {target} ({len(fbx_bytes):,} bytes; SHA-256 {source_sha256})")
print("FBX source:", source_member.filename)
