#!/usr/bin/env python3
"""Generate per-hero fire outlines from the actual menu artwork using foreground segmentation."""
from pathlib import Path
import io
import sys

import cv2
import numpy as np
from PIL import Image, ImageFilter
from rembg import new_session, remove

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Unity/Assets/Resources/Menu/HeroSelectionBackground.png"
OUT_DIR = ROOT / "Unity/Assets/Resources/Menu"


def make_outline(alpha_crop: np.ndarray, full_size: tuple[int, int], x_offset: int) -> Image.Image:
    width, height = full_size
    canvas = np.zeros((height, width), dtype=np.uint8)
    crop_h, crop_w = alpha_crop.shape
    # Suppress isolated segmentation speckles without erasing narrow edges.
    alpha_crop = cv2.medianBlur(alpha_crop, 3)
    _, binary = cv2.threshold(alpha_crop, 24, 255, cv2.THRESH_BINARY)
    contours, _ = cv2.findContours(binary, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)

    # Draw the true extracted silhouette edges in full-image coordinates.
    shifted = []
    for contour in contours:
        if cv2.contourArea(contour) < 20:
            continue
        contour = contour.copy()
        contour[:, 0, 0] += x_offset
        shifted.append(contour)

    if not shifted:
        raise RuntimeError("Segmentation found no hero silhouette in crop")

    glow = np.zeros_like(canvas)
    orange = np.zeros_like(canvas)
    gold = np.zeros_like(canvas)
    cv2.drawContours(glow, shifted, -1, 255, 15, cv2.LINE_AA)
    cv2.drawContours(orange, shifted, -1, 255, 6, cv2.LINE_AA)
    cv2.drawContours(gold, shifted, -1, 255, 2, cv2.LINE_AA)
    glow = cv2.GaussianBlur(glow, (0, 0), 5.0)

    result = np.zeros((height, width, 4), dtype=np.uint8)
    result[:, :, 0] = 255
    result[:, :, 1] = 45
    result[:, :, 2] = 4
    result[:, :, 3] = (glow.astype(np.float32) * 0.48).astype(np.uint8)

    # Compose a saturated orange rim and a hot golden center.
    orange_layer = np.zeros_like(result)
    orange_layer[:, :, 0] = 255
    orange_layer[:, :, 1] = 74
    orange_layer[:, :, 2] = 8
    orange_layer[:, :, 3] = orange
    gold_layer = np.zeros_like(result)
    gold_layer[:, :, 0] = 255
    gold_layer[:, :, 1] = 218
    gold_layer[:, :, 2] = 115
    gold_layer[:, :, 3] = gold

    base = Image.fromarray(result, "RGBA")
    base.alpha_composite(Image.fromarray(orange_layer, "RGBA"))
    base.alpha_composite(Image.fromarray(gold_layer, "RGBA"))
    return base


def main() -> None:
    if not SOURCE.exists():
        raise FileNotFoundError(f"Missing source artwork: {SOURCE}")
    image = Image.open(SOURCE).convert("RGB")
    width, height = image.size
    session = new_session("u2net")

    # Segment each hero independently so neighboring heroes are not treated as one object.
    for index in range(4):
        left = round(width * index / 4)
        right = round(width * (index + 1) / 4)
        crop = image.crop((left, 0, right, height))
        segmented = remove(crop, session=session)
        if not isinstance(segmented, Image.Image):
            segmented = Image.open(io.BytesIO(segmented))
        rgba = segmented.convert("RGBA")
        alpha = np.asarray(rgba.getchannel("A"), dtype=np.uint8)
        # Reconstruct the outline in the exact original image coordinate system.
        outline = make_outline(alpha, (width, height), left)
        path = OUT_DIR / f"HeroContour{index}.png"
        outline.save(path, optimize=True)
        print(f"Generated {path.name}: source={width}x{height}, crop={left}:{right}")

    print("All four image-derived hero contour textures generated.")


if __name__ == "__main__":
    main()
