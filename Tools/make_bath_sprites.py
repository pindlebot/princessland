"""Generates the bathroom's big, luxurious bath and the plants beside it.

Run:  Tools/.venv/bin/python Tools/make_bath_sprites.py
Out:  Assets/Art/Bath.png / Bath.json   (96x96 frames, bottom pivot)
        Bathtub         a large white-marble bath, three tiles wide, open all round: lavender veins
                        and carved panels, a gold band under its broad rim, a stepped marble plinth,
                        a gold swan-neck tap standing on the back rim (hot and cold knobs), candles
                        at both ends, a vase of lavender and a rolled towel
        Stream          3 frames, looping: the tap running (water pouring from the spout, a
                        splash where it lands). Drawn in the tub's own frame coordinates, so it
                        lines up when it sits at the tub's position.
        BubbleFoam      3 frames, looping: Barnaby's bubble bath at work, foam heaped on the water
        Bubble          one bubble drifting up out of the tub
        PottedFern      a big Boston fern in a white ceramic pot
        PottedMonstera  a monstera with split leaves in a woven basket

The same 3/4 style as the rest of the furniture (make_furniture_sprites.py): lit from the
top-left, dark plum outline. 96px frames because the tub is six units wide.
"""
import math
import random

import palette
from sprite_common import Canvas, write_sheet

S = 96

CEDAR_DEEP = (118, 66, 40, 255)
BRASS, BRASS_HI, BRASS_SH = (226, 176, 70, 255), (255, 228, 140, 255), (168, 118, 40, 255)
WATER, WATER_HI, WATER_SH = (110, 206, 222, 255), (196, 244, 250, 255), (64, 160, 186, 255)
FOAM = (236, 250, 255, 255)
GLOW, GLOW_HI = (255, 214, 140, 255), (255, 246, 210, 255)
HOT, COLD = palette.rgba(palette.CORAL), palette.rgba(palette.TURQUOISE)
LEAF, LEAF_SH, LEAF_HI = palette.rgba(palette.SAGE_DARK), palette.rgba(palette.SAGE_DEEP), palette.rgba(palette.SAGE_LIGHT)
MONSTERA, MONSTERA_SH, MONSTERA_HI = (52, 140, 84, 255), (30, 96, 62, 255), (110, 196, 120, 255)
CERAMIC, CERAMIC_HI, CERAMIC_SH = (240, 240, 244, 255), (255, 255, 255, 255), (190, 194, 210, 255)
WICKER, WICKER_HI, WICKER_SH = (204, 160, 100, 255), (232, 196, 140, 255), (156, 112, 64, 255)
SOIL = (90, 60, 44, 255)
BUBBLE, BUBBLE_SH, BUBBLE_TINT = (246, 248, 255, 255), (206, 214, 240, 255), (226, 206, 246, 255)
MARBLE, MARBLE_HI, MARBLE_SH = (238, 234, 242, 255), (252, 250, 255, 255), (200, 194, 214, 255)
VEIN = (184, 176, 204, 255)
GOLD, GOLD_HI, GOLD_SH = BRASS, BRASS_HI, BRASS_SH
CANDLE = (250, 240, 214, 255)
LAVENDER, LAVENDER_HI = (176, 140, 220, 255), (214, 190, 246, 255)

# Where things are in the tub's frame (shared by the tub and the overlays drawn over it).
TUB_X0, TUB_X1 = 12, 83          # the tub's sides
RIM_BACK, WATER_Y, RIM_FRONT = 60, 62, 65   # back rim, water surface, front rim (top rows)
TUB_BOTTOM = 92
SPOUT_X, SPOUT_Y = 68, 49        # where water leaves the spout


def faucet(c):
    """A gold swan-neck tap standing up from the back rim, curving over the water to the spout,
    with a hot and a cold knob beside it."""
    c.rect(74, 42, 77, RIM_BACK, GOLD)                          # the pillar
    c.rect(74, 42, 74, RIM_BACK, GOLD_HI)
    c.rect(77, 42, 77, RIM_BACK, GOLD_SH)
    for x in range(66, 78):                                     # the neck, arching over...
        y = 41 - round(3 * math.sin((x - 66) / 11 * math.pi))
        c.rect(x, y, x, y + 2, GOLD)
        c.dot(x, y, GOLD_HI)
    c.rect(66, 43, 69, SPOUT_Y - 1, GOLD)                       # ...and down to the spout
    c.rect(66, 43, 66, SPOUT_Y - 1, GOLD_HI)
    for x, dot in ((81, HOT), (86, COLD)):                      # the knobs, on the rim
        c.rect(x - 1, RIM_BACK - 3, x + 1, RIM_BACK, GOLD_SH)
        c.ellipse(x, RIM_BACK - 4, 2, 1.5, GOLD)
        c.dot(x, RIM_BACK - 4, dot)


def tub_body(c):
    """The marble bath on its stepped plinth: carved panels, lavender veins, a gold band under
    the broad rim, and warm water filling it nearly to the brim."""
    c.rect(2, 89, 93, 95, MARBLE_SH)                            # the plinth's lower step...
    c.rect(2, 89, 93, 89, MARBLE_HI)
    c.rect(6, 84, 89, 88, MARBLE)                               # ...and the upper one
    c.rect(6, 84, 89, 84, MARBLE_HI)
    c.rect(86, 84, 89, 88, MARBLE_SH)
    c.rect(TUB_X0, RIM_FRONT + 3, TUB_X1, 83, MARBLE)           # the tub's side
    c.rect(TUB_X1 - 4, RIM_FRONT + 3, TUB_X1, 83, MARBLE_SH)
    for x0 in range(TUB_X0 + 4, TUB_X1 - 10, 17):               # carved panels
        c.rect(x0, 71, x0 + 13, 80, MARBLE_SH)
        c.rect(x0 + 1, 72, x0 + 12, 79, MARBLE_HI)
        c.rect(x0 + 2, 73, x0 + 11, 78, MARBLE)
    for (x0, y0), (x1, y1) in (((15, 70), (24, 82)), ((44, 69), (39, 78)), ((63, 72), (72, 83)), ((80, 69), (77, 76))):
        c.line(x0, y0, x1, y1, VEIN)                            # veins
    c.rect(TUB_X0 - 2, RIM_FRONT + 2, TUB_X1 + 2, RIM_FRONT + 3, GOLD)   # the gold band
    c.rect(TUB_X0 - 2, RIM_FRONT + 2, TUB_X1 + 2, RIM_FRONT + 2, GOLD_HI)
    # the rim: a broad marble top all round, the water set into it
    c.rect(TUB_X0 - 3, RIM_BACK - 1, TUB_X1 + 3, RIM_FRONT + 1, MARBLE_HI)
    c.rect(TUB_X0 - 3, RIM_FRONT + 1, TUB_X1 + 3, RIM_FRONT + 1, MARBLE_SH)
    c.rect(TUB_X0 + 2, WATER_Y, TUB_X1 - 2, RIM_FRONT - 1, WATER)
    for x in range(TUB_X0 + 4, TUB_X1 - 3, 7):                   # ripples catching the light
        c.rect(x, WATER_Y, x + 2, WATER_Y, WATER_HI)
    c.rect(TUB_X0 + 2, RIM_FRONT - 1, TUB_X1 - 2, RIM_FRONT - 1, WATER_SH)


def rim_things(c):
    """Candles at both ends, a little vase of lavender and a rolled towel on the near end."""
    for x in (TUB_X0 - 1, TUB_X1 - 1):
        c.rect(x, RIM_BACK - 7, x + 2, RIM_BACK - 1, CANDLE)
        c.rect(x + 1, RIM_BACK - 9, x + 1, RIM_BACK - 8, GLOW)
        c.dot(x + 1, RIM_BACK - 10, GLOW_HI)
    c.rect(TUB_X0 + 5, RIM_BACK - 6, TUB_X0 + 8, RIM_BACK - 1, CERAMIC)   # the vase
    c.rect(TUB_X0 + 5, RIM_BACK - 6, TUB_X0 + 5, RIM_BACK - 1, CERAMIC_HI)
    for dx, h in ((-1, 9), (1, 12), (3, 10), (5, 8)):                     # lavender sprigs
        x = TUB_X0 + 5 + dx
        c.line(x + 1, RIM_BACK - 6, x, RIM_BACK - 6 - h, LEAF)
        for k in range(3):
            c.dot(x, RIM_BACK - 6 - h + k, LAVENDER_HI if k == 0 else LAVENDER)
    c.ellipse(TUB_X0 + 14, RIM_FRONT, 4, 2, CERAMIC)              # a rolled towel
    c.ellipse(TUB_X0 + 11, RIM_FRONT, 1.5, 1.5, CERAMIC_SH)
    c.dot(TUB_X0 + 13, RIM_FRONT - 1, CERAMIC_HI)


def bathtub():
    c = Canvas(S)
    faucet(c)
    tub_body(c)
    rim_things(c)
    return c.img


def stream(frame):
    """The faucet running: a column of water from the spout into the tub, its highlights
    sliding down frame by frame, and a splash ring where it lands."""
    c = Canvas(S)
    for y in range(SPOUT_Y, WATER_Y + 1):
        c.rect(SPOUT_X - 1, y, SPOUT_X + 1, y, WATER)
        c.dot(SPOUT_X + 1, y, WATER_SH)
        if (y + frame * 2) % 5 == 0:
            c.dot(SPOUT_X - 1, y, WATER_HI)
            c.dot(SPOUT_X, y, FOAM)
    c.ellipse(SPOUT_X, WATER_Y + 0.5, 4 + frame % 2, 1.2, FOAM)          # the splash
    c.ellipse(SPOUT_X, WATER_Y + 0.5, 2, 0.6, WATER_HI)
    rng = random.Random(frame)
    for _ in range(4):                                                    # droplets jumping out
        dx = rng.choice((-1, 1)) * rng.randint(3, 7)
        c.dot(SPOUT_X + dx, WATER_Y - rng.randint(1, 4), FOAM if rng.random() < 0.5 else WATER_HI)
    return c.img


def bubble_foam(frame):
    """Lavender bubble bath at work: foam heaped along the water (behind the bather, who lies
    in front of it), a few clumps a little higher, and some spilling over the rim's ends."""
    c = Canvas(S)
    rng = random.Random(5)
    blobs = []
    for _ in range(46):
        x = rng.uniform(TUB_X0 + 3, TUB_X1 - 3)
        clump = 6 if (24 < x < 32 or 58 < x < 66) else 0
        y = rng.uniform(WATER_Y - 6 - clump, RIM_FRONT - 1)
        blobs.append((x, y, rng.uniform(2.5, 5)))
    blobs.sort(key=lambda b: b[1])  # the lower ones drawn last, in front
    for i, (x, y, r) in enumerate(blobs):
        wobble = ((i + frame) % 3 - 1) * 0.4
        rr = r + wobble * 0.4
        c.ellipse(x, y + wobble, rr, rr, BUBBLE_SH)
        c.ellipse(x - 0.4, y + wobble - 0.4, rr - 0.6, rr - 0.6, BUBBLE_TINT if i % 5 == 0 else BUBBLE)
        c.dot(round(x - rr / 2), round(y + wobble - rr / 2), CERAMIC_HI)
    for x, y in ((TUB_X0 - 3, RIM_FRONT + 5), (TUB_X1 + 3, RIM_FRONT + 4)):  # foam spilling over the ends
        c.ellipse(x, y + frame % 2, 3, 2.5, BUBBLE)
    return c.img


def bubble():
    """A single bubble, rising: drawn near the bottom-middle of the frame."""
    c = Canvas(S)
    c.ellipse(47.5, 90, 3, 3, BUBBLE_SH)
    c.ellipse(47.5, 90, 2.2, 2.2, (232, 240, 255, 255))
    c.dot(46, 89, CERAMIC_HI)
    return c.img


def pot(c, cx, top, w, h, body, hi, sh):
    """A round-shouldered pot standing on the bottom of the frame."""
    for y in range(top, top + h):
        t = (y - top) / h
        half = round(w / 2 * (1 - 0.18 * t * t))
        c.rect(cx - half, y, cx + half, y, body)
        c.dot(cx - half, y, hi)
        c.dot(cx - half + 1, y, hi)
        c.dot(cx + half, y, sh)
    c.rect(cx - w // 2 - 1, top - 2, cx + w // 2 + 1, top - 1, hi)   # rim
    c.rect(cx - w // 2 + 1, top - 2, cx + w // 2 - 1, top - 2, SOIL)


def potted_fern():
    """A big Boston fern spilling out of a white ceramic pot: arching fronds all round."""
    c = Canvas(S)
    cx, base = 47, 79
    rng = random.Random(11)
    fronds = []
    for i in range(22):  # fronds arch up and out from the middle, their tips drooping
        a = math.radians(-90 + rng.uniform(-75, 75))
        fronds.append((a, rng.uniform(22, 32), rng.uniform(0.3, 0.6), i))
    fronds.sort(key=lambda f: -abs(math.cos(f[0])))  # the upright ones last, in front
    for a, length, droop, i in fronds:
        for s in range(int(length)):
            t = s / length
            x = cx + math.cos(a) * s * 1.1
            y = base + math.sin(a) * s * 1.6 * (1 - 0.4 * t) + droop * 12 * t * t
            col = LEAF_HI if (i + s) % 6 == 0 else LEAF if i % 3 else LEAF_SH
            c.dot(round(x), round(y), col)
            w = 2 if 0.2 < t < 0.8 else 1  # leaflets either side, fullest in the middle
            for k in range(1, w + 1):
                c.dot(round(x), round(y) - k, LEAF if k == 1 else LEAF_HI)
                c.dot(round(x), round(y) + k, LEAF_SH)
    pot(c, cx, 80, 18, 15, CERAMIC, CERAMIC_HI, CERAMIC_SH)
    c.rect(cx - 7, 86, cx + 7, 86, COLD)                         # a turquoise band
    return c.img


def potted_monstera():
    """A monstera in a woven basket: tall stems on a moss pole, big split heart-shaped leaves."""
    c = Canvas(S)
    cx = 47
    c.rect(cx - 1, 38, cx, 80, CEDAR_DEEP)                       # the moss pole
    leaves = ((cx - 10, 50, 9, 7, -1), (cx + 10, 46, 9, 7, 1), (cx - 6, 36, 8, 6, -1),
              (cx + 7, 62, 8, 6, 1), (cx - 11, 66, 7, 5, -1), (cx + 3, 30, 7, 5, 1))
    for lx, ly, rx, ry, side in leaves:
        c.line(cx, 80, lx - side * 2, ly + 2, MONSTERA_SH)      # stem
    for lx, ly, rx, ry, side in leaves:
        c.ellipse(lx, ly, rx, ry, MONSTERA_SH)
        c.ellipse(lx - 0.6, ly - 0.6, rx - 1, ry - 1, MONSTERA)
        c.line(lx - side * (rx - 2), ly, lx + side * (rx - 1), ly - 1, MONSTERA_SH)  # the midrib
        for k in (-3, 0, 3):                                     # the famous splits, cut in from the edge
            for d in range(4):
                c.dot(lx + side * (rx - d), ly + k - (d // 2), (0, 0, 0, 0))
        c.dot(lx - side * 3, ly - ry + 2, MONSTERA_HI)
    # the woven basket
    for y in range(80, 95):
        half = 9 - (y - 80) // 5
        for x in range(cx - half, cx + half + 1):
            c.dot(x, y, WICKER_HI if (x + y) % 4 == 0 else WICKER_SH if (x - y) % 4 == 0 else WICKER)
    c.rect(cx - 10, 79, cx + 10, 80, WICKER_SH)
    return c.img


if __name__ == "__main__":
    write_sheet(
        "Bath",
        None,
        [("Bathtub", 1, False, [bathtub()]),
         ("Stream", 10, True, [stream(f) for f in range(3)]),
         ("BubbleFoam", 3, True, [bubble_foam(f) for f in range(3)]),
         ("Bubble", 1, False, [bubble()]),
         ("PottedFern", 1, False, [potted_fern()]),
         ("PottedMonstera", 1, False, [potted_monstera()])],
        frame_size=S,
    )
