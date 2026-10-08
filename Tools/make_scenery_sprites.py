"""Generates the castle-grounds scenery: a small, coherent set of storybook props.

Run:  Tools/.venv/bin/python Tools/make_scenery_sprites.py
Out:  Assets/Art/Scenery.png / Scenery.json   (64x64 frames, bottom pivot)
        Tree      2 frames, looping  a round, friendly tree whose canopy sways a little
        Fountain  3 frames, looping  a stone fountain with splashing water
        Bush      1 frame            a soft round shrub for softening edges
        Cloud     1 frame            a puffy cloud drifting around the floating island

Colours are kept soft and close to the grass so they frame the scene without
competing with characters, enemies and spells.
"""
import math
import random

from sprite_common import Canvas, write_sheet

S = 64
LEAF, LEAF_SH, LEAF_HI = (78, 140, 66, 255), (54, 104, 52, 255), (120, 176, 90, 255)
TRUNK, TRUNK_SH = (122, 84, 52, 255), (88, 58, 36, 255)
STONE, STONE_SH, STONE_HI = (176, 170, 184, 255), (130, 124, 140, 255), (214, 210, 220, 255)
WATER, WATER_HI, WATER_SH = (110, 200, 230, 255), (220, 248, 255, 255), (70, 150, 196, 255)
CLOUD, CLOUD_SH = (255, 255, 255, 230), (220, 232, 244, 230)


def canopy(c, cx, cy, r, seed, sway):
    """A round canopy made of overlapping leafy blobs, lit from the top-left."""
    rng = random.Random(seed)
    c.ellipse(cx + sway, cy, r, r * 0.85, LEAF_SH)
    for _ in range(9):
        a = rng.uniform(0, 2 * math.pi)
        d = rng.uniform(0, r * 0.55)
        bx, by = cx + math.cos(a) * d + sway, cy + math.sin(a) * d * 0.8
        c.ellipse(bx - 1, by - 1, r * 0.45, r * 0.4, LEAF)
    for _ in range(5):  # highlights on the sunny side
        bx = cx - r * 0.35 + rng.uniform(-3, 3) + sway
        by = cy - r * 0.35 + rng.uniform(-3, 3)
        c.ellipse(bx, by, r * 0.18, r * 0.14, LEAF_HI)


def tree(frame):
    c = Canvas(S)
    sway = [0, 1][frame]
    c.rect(29, 40, 34, 61, TRUNK)  # trunk
    c.rect(33, 40, 34, 61, TRUNK_SH)
    c.rect(26, 60, 37, 61, TRUNK_SH)  # roots
    canopy(c, 31.5, 26, 20, seed=5, sway=sway)
    return c.img


def fountain(frame):
    c = Canvas(S)
    c.ellipse(31.5, 54, 24, 8, STONE_SH)  # basin rim
    c.ellipse(31.5, 53, 22, 6.5, STONE)
    c.ellipse(31.5, 53, 18, 4.5, WATER)  # the pool
    c.ellipse(26 + frame, 52, 4, 1, WATER_HI)
    c.rect(29, 34, 34, 52, STONE)  # pillar
    c.rect(33, 34, 34, 52, STONE_SH)
    c.rect(29, 34, 29, 52, STONE_HI)
    c.ellipse(31.5, 34, 9, 3, STONE_SH)  # top bowl
    c.ellipse(31.5, 33, 8, 2.2, STONE)
    # Water arcing out of the top and falling into the pool; it shifts each frame.
    for side in (-1, 1):
        for i in range(12):
            t = i / 11
            x = 31.5 + side * (2 + t * 12)
            y = 26 + (t * 2 - 0.6) ** 2 * 12 - 5
            if (i + frame) % 3 != 0:
                c.dot(round(x), round(y), WATER_HI if i < 4 else WATER)
    c.rect(31, 22 - frame, 32, 31, WATER)  # the jet in the middle
    c.dot(31, 21 - frame, WATER_HI)
    for i, x in enumerate((20, 27, 36, 43)):  # splashes in the pool
        if (i + frame) % 2 == 0:
            c.dot(x, 50, WATER_HI)
    return c.img


def bush():
    c = Canvas(S)
    canopy(c, 31.5, 52, 11, seed=9, sway=0)
    c.rect(20, 60, 43, 61, LEAF_SH)
    return c.img


def cloud():
    c = Canvas(S)
    for cx, cy, rx, ry in ((22, 44, 11, 8), (34, 38, 13, 11), (46, 45, 10, 7), (32, 48, 20, 6)):
        c.ellipse(cx, cy + 1, rx, ry, CLOUD_SH)
    for cx, cy, rx, ry in ((22, 43, 10, 7), (34, 37, 12, 10), (46, 44, 9, 6)):
        c.ellipse(cx, cy, rx, ry, CLOUD)
    return c.img


if __name__ == "__main__":
    write_sheet(
        "Scenery",
        None,
        [
            ("Tree", 1, True, [tree(f) for f in range(2)]),
            ("Fountain", 6, True, [fountain(f) for f in range(3)]),
            ("Bush", 1, False, [bush()]),
            ("Cloud", 1, False, [cloud()]),
        ],
        frame_size=S,
    )
