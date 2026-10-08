"""Generates the castle-grounds scenery: a small, coherent set of storybook props.

Run:  Tools/.venv/bin/python Tools/make_scenery_sprites.py
Out:  Assets/Art/Scenery.png / Scenery.json   (64x64 frames, bottom pivot)
        Tree      2 frames, looping  a round, friendly tree whose canopy sways a little
        Fountain  3 frames, looping  a stone fountain with splashing water
        Bush      1 frame            a soft round shrub with a scalloped (garden-motif) top
        Cloud     1 frame            a flat-bottomed puffy cloud drifting around the floating island,
                                     with a soft lavender rim instead of a dark outline
        Pine      2 frames, looping  a tall tiered conifer in deep sage, swaying at the tip
        Birch     2 frames, looping  a slender white-barked birch with a light, airy canopy
        Autumn    2 frames, looping  the round tree again, in muted honey and rust leaves
        Boulder   1 frame            a big mossy lavender-grey rock
        Stones    1 frame            a few pebbles in the grass
        Stump     1 frame            an old cut stump showing its rings
        Log       1 frame            a fallen log to sit on by the campfire
        Tent      1 frame            a striped canvas camping tent with its flap tied open
        Fern      1 frame            a low fan of fronds for the woods

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
# The woods: pines a cooler, deeper green; birches lighter; autumn trees muted honey and rust,
# so the extra trees add variety without getting louder than the characters.
PINE, PINE_SH, PINE_HI = (78, 122, 94, 255), (56, 94, 80, 255), (118, 158, 112, 255)
BIRCH_LEAF, BIRCH_SH, BIRCH_HI = (146, 182, 108, 255), (112, 150, 92, 255), (190, 212, 140, 255)
BARK, BARK_MARK = (234, 228, 222, 255), (96, 84, 92, 255)
AUTUMN, AUTUMN_SH, AUTUMN_HI = (206, 150, 88, 255), (170, 108, 74, 255), (234, 194, 122, 255)
ROCK, ROCK_SH, ROCK_HI = pal.rgba(pal.STONE_SHADE), pal.rgba(pal.ROCK), pal.rgba(pal.STONE)
MOSS = pal.rgba(pal.SAGE_DARK)
WOOD, WOOD_SH, WOOD_HI = (150, 104, 70, 255), (112, 76, 58, 255), (196, 156, 112, 255)
RING = (176, 132, 92, 255)
CANVAS, CANVAS_SH, CANVAS_HI = pal.rgba(pal.CREAM), pal.rgba(pal.CREAM_SHADE), (255, 250, 236, 255)
STRIPE, STRIPE_SH = pal.rgba(pal.CORAL), (204, 92, 108, 255)
INSIDE = pal.rgba(pal.PLUM)


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


def pine(frame):
    """Four stacked tiers, narrowing to a point; each tier's bottom edge is scalloped. The tip
    leans a pixel in the second frame, the lower tiers stay put, so it sways from the top."""
    c = Canvas(S)
    c.rect(29, 52, 34, 61, TRUNK)
    c.rect(33, 52, 34, 61, TRUNK_SH)
    tiers = ((54, 22, 0), (43, 18, 0), (32, 14, frame), (21, 9, frame))  # (bottom y, half width, sway)
    for bottom, half, sway in tiers:
        height = half + 6
        for y in range(bottom - height, bottom + 1):
            t = (y - (bottom - height)) / height        # 0 at the tier's top, 1 at its bottom
            w = half * t
            x0, x1 = round(31.5 + sway - w), round(31.5 + sway + w)
            for x in range(x0, x1 + 1):
                lit = x < 31.5 + sway - w * 0.2           # the sunny (left) side is lighter
                c.dot(x, y, PINE if lit else PINE_SH)
        for x in range(round(31.5 + sway - half), round(31.5 + sway + half) + 1, 4):
            c.dot(x + 2, bottom + 1, PINE_SH)             # scalloped hem
        c.line(round(29 + sway - half * 0.3), bottom - 3, round(31 + sway), bottom - height + 4, PINE_HI)
    c.dot(31 + frame, 13, PINE_HI)
    return c.img


def birch(frame):
    """A slim white trunk with dark marks, and a smaller, airier canopy."""
    c = Canvas(S)
    c.rect(30, 30, 33, 61, BARK)
    c.rect(33, 30, 33, 61, CANVAS_SH)
    for y, x0, x1 in ((36, 30, 31), (42, 32, 33), (47, 30, 31), (53, 31, 33), (58, 30, 31)):
        c.rect(x0, y, x1, y, BARK_MARK)
    c.line(32, 36, 38, 30, BARK)                            # a branch reaching out
    rng = random.Random(11)
    sway = frame
    for cx, cy, r in ((25, 24, 9), (38, 22, 10), (31, 15, 9), (31, 27, 8)):
        c.ellipse(cx + sway, cy, r, r * 0.85, BIRCH_SH)
    for _ in range(10):
        x, y = rng.uniform(20, 43), rng.uniform(10, 30)
        c.ellipse(x + sway - 1, y - 1, 4, 3.4, BIRCH_LEAF)
    for _ in range(6):
        x, y = rng.uniform(21, 32), rng.uniform(10, 20)
        c.ellipse(x + sway, y, 1.6, 1.2, BIRCH_HI)
    return c.img


def autumn(frame):
    """The round tree's shape in autumn colours (same trunk, a different seed for the leaves)."""
    c = Canvas(S)
    sway = [0, 1][frame]
    c.rect(29, 40, 34, 61, TRUNK)
    c.rect(33, 40, 34, 61, TRUNK_SH)
    c.rect(26, 60, 37, 61, TRUNK_SH)
    rng = random.Random(23)
    cx, cy, r = 31.5, 27, 18
    c.ellipse(cx + sway, cy, r, r * 0.85, AUTUMN_SH)
    for _ in range(9):
        a = rng.uniform(0, 2 * math.pi)
        d = rng.uniform(0, r * 0.55)
        c.ellipse(cx + math.cos(a) * d + sway - 1, cy + math.sin(a) * d * 0.8 - 1, r * 0.45, r * 0.4, AUTUMN)
    for _ in range(5):
        c.ellipse(cx - r * 0.35 + rng.uniform(-3, 3) + sway, cy - r * 0.35 + rng.uniform(-3, 3), r * 0.18, r * 0.14, AUTUMN_HI)
    for x, y in ((22, 58), (40, 60), (45, 57)):           # a few fallen leaves
        c.rect(x, y, x + 1, y, AUTUMN)
    return c.img


def boulder():
    """A lumpy rock about two tiles' worth of the frame wide, moss on its sunny top."""
    c = Canvas(S)
    c.ellipse(32, 50, 17, 11, ROCK_SH)
    c.ellipse(30, 47, 15, 10, ROCK)
    c.ellipse(40, 51, 8, 7, ROCK)
    c.ellipse(27, 44, 8, 5, ROCK_HI)
    c.ellipse(26, 39, 7, 2.5, MOSS)
    c.rect(22, 40, 30, 40, MOSS)
    c.line(34, 46, 38, 53, ROCK_SH)                         # a crack
    c.line(38, 53, 41, 55, ROCK_SH)
    return c.img


def stones():
    c = Canvas(S)
    for x, y, rx, ry in ((24, 57, 3.5, 2.5), (33, 59, 2.5, 2), (39, 55, 3, 2.2), (29, 53, 2, 1.5)):
        c.ellipse(x, y, rx, ry, ROCK)
        c.ellipse(x - 0.8, y - 0.8, rx * 0.5, ry * 0.4, ROCK_HI)
    return c.img


def stump():
    c = Canvas(S)
    c.rect(24, 48, 39, 59, WOOD)
    c.rect(36, 48, 39, 59, WOOD_SH)
    c.rect(20, 58, 43, 60, WOOD_SH)                         # roots
    c.ellipse(31.5, 48, 8, 3.5, WOOD_HI)                    # the cut top, with its rings
    c.ellipse(31.5, 48, 5, 2, RING)
    c.ellipse(31.5, 48, 2, 1, WOOD_HI)
    for y in (52, 55):
        c.dot(27, y, WOOD_SH)
    return c.img


def log():
    """Lying across the frame, its cut end toward the viewer's left."""
    c = Canvas(S)
    c.rect(16, 50, 52, 60, WOOD)
    c.rect(16, 58, 52, 60, WOOD_SH)
    c.rect(16, 50, 52, 50, WOOD_HI)
    for x in (24, 33, 44):                                  # bark lines
        c.line(x, 52, x + 3, 57, WOOD_SH)
    c.ellipse(16, 55, 4, 5.5, WOOD_HI)                      # the cut end
    c.ellipse(16, 55, 2.5, 3.5, RING)
    c.dot(16, 55, WOOD_SH)
    c.rect(47, 47, 49, 49, MOSS)                            # a tuft of moss
    return c.img


def tent():
    """An A-frame tent seen from the front corner: a striped front triangle with the flap tied
    open (plum dark inside), the long side in shade, and a little pennant on the pole."""
    c = Canvas(S)
    apex_x, apex_y, base_y = 26, 20, 60
    for y in range(apex_y, base_y + 1):                     # the side, running back to the right
        t = (y - apex_y) / (base_y - apex_y)
        x_front = apex_x + t * 20
        for x in range(round(x_front), round(x_front + 22 - t * 4) + 1):
            c.dot(x, y, CANVAS_SH)
    c.line(apex_x, apex_y, apex_x + 22, apex_y + 6, CANVAS)  # the ridge
    for y in range(apex_y, base_y + 1):                     # the front triangle
        t = (y - apex_y) / (base_y - apex_y)
        half = t * 20
        for x in range(round(apex_x - half), round(apex_x + half) + 1):
            stripe = (round(x - apex_x + 40) // 5) % 2 == 0
            c.dot(x, y, (STRIPE if stripe else CANVAS) if x < apex_x else (STRIPE_SH if stripe else CANVAS_SH))
    for y in range(apex_y + 18, base_y + 1):                # the open doorway
        t = (y - apex_y - 18) / (base_y - apex_y - 18)
        half = 2 + t * 7
        for x in range(round(apex_x - half), round(apex_x + half) + 1):
            c.dot(x, y, INSIDE)
    c.line(apex_x - 9, base_y, apex_x - 3, apex_y + 18, CANVAS_HI)  # the tied-back flaps
    c.line(apex_x + 9, base_y, apex_x + 3, apex_y + 18, CANVAS)
    c.rect(apex_x, apex_y - 7, apex_x, apex_y, TRUNK_SH)    # pole and pennant
    for i in range(5):
        c.rect(apex_x + 1, apex_y - 7 + i // 2, apex_x + 5 - i, apex_y - 7 + i // 2, STRIPE)
    for x in (apex_x - 20, apex_x + 20, apex_x + 40):       # tent pegs
        c.rect(x, base_y - 1, x, base_y, TRUNK_SH)
    return c.img


def fern():
    c = Canvas(S)
    for angle in (-70, -40, -12, 15, 42, 70):
        a = math.radians(angle - 90)
        length = 13 - abs(angle) / 14
        for i in range(int(length)):
            x, y = 31.5 + math.cos(a) * i, 60 + math.sin(a) * i * 0.8
            c.dot(round(x), round(y), LEAF_SH if i < 3 else LEAF)
            if i % 2 and i > 2:                              # little leaflets along each frond
                c.dot(round(x - math.sin(a)), round(y + math.cos(a) * 0.8), LEAF)
        c.dot(round(31.5 + math.cos(a) * length), round(60 + math.sin(a) * length * 0.8), LEAF_HI)
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
            ("Pine", 1, True, [pine(f) for f in range(2)]),
            ("Birch", 1, True, [birch(f) for f in range(2)]),
            ("Autumn", 1, True, [autumn(f) for f in range(2)]),
            ("Boulder", 1, False, [boulder()]),
            ("Stones", 1, False, [stones()]),
            ("Stump", 1, False, [stump()]),
            ("Log", 1, False, [log()]),
            ("Tent", 1, False, [tent()]),
            ("Fern", 1, False, [fern()]),
        ],
        frame_size=S,
    )
