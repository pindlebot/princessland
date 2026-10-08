"""Shared helpers for the character sprite generators (make_wizard_sprites.py, make_skeleton_sprites.py).

Every sheet uses the same conventions, which CharacterSpriteBuilder.cs relies on:
  - 32x32 frames, 16 pixels per Unity unit, feet on the bottom row
  - one animation per row, named <State>_Front / <State>_Back (plus a single "Die")
  - <Name>.json beside <Name>.png says which row/frames/fps/loop belongs to each animation,
    and which state is the character's "action" (Cast, Attack, ...)
"""
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets" / "Art"
F = 32  # frame size in pixels
PPU = 16  # pixels per Unity unit -> a full-height character is ~2 units tall

CLEAR = (0, 0, 0, 0)
OUTLINE = (22, 16, 30, 255)


class Canvas:
    def __init__(self, size=F):
        self.size = size
        self.img = Image.new("RGBA", (size, size), CLEAR)
        self.px = self.img.load()

    def dot(self, x, y, c):
        if 0 <= x < self.size and 0 <= y < self.size:
            self.px[x, y] = c

    def ellipse(self, cx, cy, rx, ry, c):
        """Filled ellipse, handy for round creature shapes."""
        for y in range(int(cy - ry), int(cy + ry) + 1):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1:
                    self.dot(x, y, c)

    def rect(self, x0, y0, x1, y1, c):  # inclusive
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.dot(x, y, c)

    def line(self, x0, y0, x1, y1, c):
        """Straight pixel line (Bresenham)."""
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            self.dot(x0, y0, c)
            if x0 == x1 and y0 == y1:
                return
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy


def tint(img, rgb, amount):
    """Blend every opaque pixel toward rgb (used for hurt flashes and corpses)."""
    out = img.copy()
    px = out.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (
                    round(r + (rgb[0] - r) * amount),
                    round(g + (rgb[1] - g) * amount),
                    round(b + (rgb[2] - b) * amount),
                    a,
                )
    return out


def outline(img, color=OUTLINE):
    """1px outline around every opaque pixel. Makes sprites readable on any floor."""
    src = img.load()
    out = img.copy()
    dst = out.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3]:
                    dst[x, y] = color
                    break
    return out


def write_sheet(name, action, anims, pivot="bottom", outline_color=OUTLINE, frame_size=F):
    """anims: list of (animation name, fps, loop, [frame images]). Writes <name>.png and <name>.json.
    action: the character's Cast/Attack state (None for effects).
    pivot: "bottom" for characters standing on the floor, "center" for effects.
    outline_color: None to skip the outline.
    frame_size: 32 for people; bigger for big creatures (same pixels-per-unit, so they're bigger in game)."""
    ART.mkdir(parents=True, exist_ok=True)
    f = frame_size
    cols = max(len(frames) for *_, frames in anims)
    sheet = Image.new("RGBA", (cols * f, len(anims) * f), CLEAR)
    layout = {"frameSize": f, "pixelsPerUnit": PPU, "pivot": pivot, "animations": []}
    if action:
        layout["action"] = action

    for row, (anim, fps, loop, frames) in enumerate(anims):
        for i, frame in enumerate(frames):
            sheet.paste(outline(frame, outline_color) if outline_color else frame, (i * f, row * f))
        layout["animations"].append({"name": anim, "row": row, "frames": len(frames), "fps": fps, "loop": loop})

    sheet.save(ART / f"{name}.png")
    (ART / f"{name}.json").write_text(json.dumps(layout, indent=2) + "\n")
    print(f"{name}: {len(anims)} animations, sheet {sheet.size[0]}x{sheet.size[1]}")


def write_shadow():
    """Soft blob shadow shared by every character. Round, because it lies flat on the
    floor (the iso camera squashes it into an ellipse)."""
    shadow = Image.new("RGBA", (16, 16), CLEAR)
    sp = shadow.load()
    for y in range(16):
        for x in range(16):
            d = ((x - 7.5) / 8) ** 2 + ((y - 7.5) / 8) ** 2
            if d < 1:
                sp[x, y] = (0, 0, 0, round(130 * (1 - d) ** 0.5))
    shadow.save(ART / "Shadow.png")
