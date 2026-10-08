"""Generates the castle-grounds scenery: a small, coherent set of storybook props.

Run:  Tools/.venv/bin/python Tools/make_scenery_sprites.py
Out:  Assets/Art/Scenery.png / Scenery.json   (64x64 frames, bottom pivot)
        Tree      2 frames, looping  a round, friendly tree whose canopy sways a little
        Fountain  3 frames, looping  a stone fountain with splashing water
        Bush      1 frame            a soft round shrub with a scalloped (garden-motif) top
        Cloud     1 frame            a flat-bottomed puffy cloud drifting around the floating island,
                                     with a soft lavender rim instead of a dark outline

Colours come from palette.py and stay soft and close to the grass, so they frame the
scene without competing with characters, enemies and spells. Everything is placed at
scale 1 (no stretched pixels); the ground shadows come from Shadows.png, not the art.
"""
import math
import random

import palette as pal
from sprite_common import Canvas, write_sheet

S = 64
# Trees and bushes are a little deeper than the grass so they read as objects, but stay sage.
LEAF, LEAF_SH, LEAF_HI = pal.rgba((100, 142, 88)), pal.rgba((74, 110, 74)), pal.rgba(pal.SAGE_LIGHT)
TRUNK, TRUNK_SH = (134, 94, 66, 255), (98, 66, 52, 255)
STONE, STONE_SH, STONE_HI = pal.rgba(pal.STONE), pal.rgba(pal.STONE_SHADE), pal.rgba(pal.STONE_LIGHT)
WATER, WATER_HI, WATER_SH = pal.rgba(pal.WATER), pal.rgba(pal.WATER_GLINT), pal.rgba(pal.WATER_DEEP)
CLOUD, CLOUD_SH = pal.rgba(pal.CLOUD), pal.rgba(pal.CLOUD_SHADE)


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
    """A low, round mound whose top is three scallops (the garden motif), lit from the top-left."""
    c = Canvas(S)
    c.ellipse(31.5, 55, 13, 6.5, LEAF_SH)                            # the shaded body
    for x, y, r in ((21.5, 52, 5.5), (31.5, 49, 6.5), (41.5, 52, 5.5)):
        c.ellipse(x, y, r, r * 0.9, LEAF)                            # three scallops along the top
    c.ellipse(31.5, 54, 11, 4, LEAF)
    for x, y in ((19, 49), (29, 45), (39, 49)):                      # sun on each scallop
        c.rect(x, y, x + 2, y, LEAF_HI)
        c.dot(x - 1, y + 1, LEAF_HI)
    return c.img


def cloud():
    """Wide and flat-bottomed: three puffs over a long base, a lilac shade along the bottom.
    About 3.5 units wide, so it frames the island without crowding it."""
    c = Canvas(S)
    for cx, cy, rx, ry in ((18, 46, 10, 7), (32, 41, 13, 10), (46, 46, 10, 7)):
        c.ellipse(cx, cy, rx, ry, CLOUD)
    c.rect(6, 46, 57, 52, CLOUD)
    c.rect(6, 50, 57, 52, CLOUD_SH)
    for x in (14, 30, 44):  # a few soft hollows between the puffs
        c.rect(x, 49, x + 3, 49, CLOUD_SH)
    return c.img


if __name__ == "__main__":
    write_sheet(
        "Scenery",
        None,
        [
            ("Tree", 1, True, [tree(f) for f in range(2)]),
            ("Fountain", 6, True, [fountain(f) for f in range(3)]),
            ("Bush", 1, False, [bush()]),
            ("Cloud", 1, False, [cloud()], pal.rgba(pal.CLOUD_EDGE)),
        ],
        frame_size=S,
    )
