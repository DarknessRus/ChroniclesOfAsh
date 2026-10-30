#!/usr/bin/env python3
"""Generate tight, character-only fire outlines from the menu artwork."""
from pathlib import Path
import io

import cv2
import numpy as np
from PIL import Image
from rembg import new_session, remove

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Unity/Assets/Resources/Menu/HeroSelectionBackground.png"
OUT_DIR = ROOT / "Unity/Assets/Resources/Menu"


def make_outline(alpha_crop: np.ndarray, full_size: tuple[int, int], x_offset: int) -> Image.Image:
    width, height = full_size

    # Use the segmentation mask itself, not every contour in the crop. A high
    # threshold rejects faint background spill from the segmentation model.
    alpha_crop = cv2.medianBlur(alpha_crop, 3)
    _, binary = cv2.threshold(alpha_crop, 96, 255, cv2.THRESH_BINARY)

    # Keep the main foreground object only. This prevents nearby scenery,
    # shadows and stray segmentation islands from receiving an outline.
    count, labels, stats, _ = cv2.connectedComponentsWithStats(binary, connectivity=8)
    if count <= 1:
        raise RuntimeError("Segmentation found no hero silhouette in crop")
    areas = stats[1:, cv2.CC_STAT_AREA]
    main_label = 1 + int(np.argmax(areas))
    hero_mask = np.where(labels == main_label, 255, 0).astype(np.uint8)

    # The fiery rim is drawn INSIDE the extracted silhouette. Unlike a
    # centered/thick contour stroke, no outline pixels can spill into the
    # background or overlap an adjacent hero/object.
    inner = cv2.erode(hero_mask, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3)), iterations=1)
    edge = cv2.subtract(hero_mask, inner)

    # A restrained, clipped glow adds fire without extending beyond the body.
    glow = cv2.GaussianBlur(edge, (0, 0), 2.0)
    glow = cv2.min(glow, hero_mask)

    crop_h, crop_w = hero_mask.shape
    rgba = np.zeros((height, width, 4), dtype=np.uint8)
    x1 = min(width, x_offset + crop_w)
    x0 = max(0, x_offset)
    if x1 <= x0:
        raise RuntimeError("Invalid hero crop coordinates")
    sx0, sx1 = x0 - x_offset, x1 - x_offset
    # Put the exact mask-aligned edge into its original artwork coordinates.
    rgba[:, :, 0] = 255
    rgba[:, :, 1] = 70
    rgba[:, :, 2] = 6
    rgba[:, :, 3] = 0
    rgba[:, :, 3][:, x0:x1] = (glow[:, sx0:sx1].astype(np.float32) * 0.42).astype(np.uint8)

    # Crisp orange edge, with a narrow golden hot line; both remain inside mask.
    rgba[:, :, 0][:, x0:x1] = 255
    rgba[:, :, 1][:, x0:x1] = 105
    rgba[:, :, 2][:, x0:x1] = 8
    rgba[:, :, 3][:, x0:x1] = np.maximum(rgba[:, :, 3][:, x0:x1], edge[:, sx0:sx1])

    gold = cv2.erode(edge, np.ones((2, 2), np.uint8), iterations=1)
    gold = cv2.min(gold, hero_mask)
    rgba[:, :, 0][:, x0:x1] = np.where(gold[:, sx0:sx1] > 0, 255, rgba[:, :, 0][:, x0:x1])
    rgba[:, :, 1][:, x0:x1] = np.where(gold[:, sx0:sx1] > 0, 220, rgba[:, :, 1][:, x0:x1])
    rgba[:, :, 2][:, x0:x1] = np.where(gold[:, sx0:sx1] > 0, 120, rgba[:, :, 2][:, x0:x1])
    rgba[:, :, 3][:, x0:x1] = np.maximum(rgba[:, :, 3][:, x0:x1], gold[:, sx0:sx1])

    # All transparent pixels have zero alpha; RGB is irrelevant there.
    return Image.fromarray(rgba)


def main() -> None:
    if not SOURCE.exists():
        raise FileNotFoundError(f"Missing source artwork: {SOURCE}")
    image = Image.open(SOURCE).convert("RGB")
    width, height = image.size
    session = new_session("u2net")

    # Process each hero in its own quarter to avoid joining adjacent characters.
    for index in range(4):
        left = round(width * index / 4)
        right = round(width * (index + 1) / 4)
        crop = image.crop((left, 0, right, height))
        segmented = remove(crop, session=session)
        if not isinstance(segmented, Image.Image):
            segmented = Image.open(io.BytesIO(segmented))
        alpha = np.asarray(segmented.convert("RGBA").getchannel("A"), dtype=np.uint8)
        outline = make_outline(alpha, (width, height), left)
        path = OUT_DIR / f"HeroContour{index}.png"
        outline.save(path, optimize=True)
        print(f"Generated tight character-only contour {path.name}: {width}x{height}, crop={left}:{right}")

    print("All four tight, mask-clipped hero contour textures generated.")


if __name__ == "__main__":
    main()
