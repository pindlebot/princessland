"""Generates the village east of the castle (Level 0): its shopkeeper, its chickens and its props.

Run:  Tools/.venv/bin/python Tools/make_town_sprites.py
Out:  Assets/Art/Barnaby.png / .json      (48x48)  Barnaby Badger, who sells bubble bath at his stall
        Idle  4 frames, looping   a gentle bob and a blink behind his round spectacles
        Talk  2 frames, looping   mouth open and a paw raised
      Assets/Art/Chicken.png / .json      (32x32, bottom pivot, drawn facing right)
        Walk / Peck         a white hen: two stepping frames, and three frames of pecking at grain
        WalkBrown / PeckBrown  the same hen in russet feathers
        Chick               2 frames, a fluffy yellow chick that bobs and cheeps
      Assets/Art/TownProps.png / .json    (64x64, bottom pivot)
        Well · Lamppost · Bench · Planter · NoticeBoard · Stall (Barnaby's cart of bubble bath,
        under a striped awning) · Coop (a little hen house with a ramp) · GrainSack
      Assets/Art/TownDecals.png / .json   (32x32, centre pivot; laid flat on the ground)
        Grain  scattered corn for the chickens to peck at
      Assets/Art/UI/PortraitBarnaby.png   32x32 dialogue portrait

The village is a little warmer than the castle grounds (oak, cream plaster, terracotta), but
props stay quiet next to the characters, like the rest of the scenery (see palette.py).
"""
import math
import random

from PIL import Image

import palette as pal
from sprite_common import ART, Canvas, outline, write_sheet

WHITE = (255, 255, 255, 255)
EYE = (40, 30, 60, 255)
MOUTH = (150, 60, 80, 255)

# ---------- Barnaby Badger ----------

FUR, FUR_SH, FUR_HI = (150, 146, 160, 255), (112, 108, 124, 255), (184, 180, 192, 255)
MASK = (62, 56, 70, 255)            # the black bands on a badger's face
BADGER_WHITE = (246, 242, 236, 255)  # the white stripe and cheeks
NOSE = (40, 30, 44, 255)
SHIRT, SHIRT_SH = pal.rgba(pal.CREAM), pal.rgba(pal.CREAM_SHADE)
APRON, APRON_SH, APRON_HI = (96, 156, 116, 255), (70, 120, 92, 255), (136, 190, 146, 255)
GOLD, GOLD_HI = pal.rgba(pal.HONEY), pal.rgba(pal.HONEY_LIGHT)
LILAC, LILAC_SH, GLASS = (196, 150, 226, 255), (150, 104, 186, 255), (238, 236, 250, 255)

B = 48


def draw_barnaby(bob=0, blink=False, talk=False):
    """A round, friendly badger shopkeeper in a cream shirt and a green apron, with gold
    spectacles; he holds a bottle of bubble bath, and lifts a paw while he talks."""
    c = Canvas(B)
    b = bob
    # Feet.
    for x in (18, 27):
        c.ellipse(x + 0.5, 45.5, 3.5, 1.8, MASK)
    # Body: a round grey body, the shirt and the apron over his tummy.
    c.ellipse(24, 34 + b, 11, 11, FUR_SH)
    c.ellipse(23, 33 + b, 10, 10, FUR)
    c.rect(17, 25 + b, 30, 30 + b, SHIRT)
    c.rect(28, 25 + b, 30, 30 + b, SHIRT_SH)
    for y in range(27, 45):
        half = 7 + (y - 27) * 0.18
        for x in range(round(23.5 - half), round(23.5 + half) + 1):
            c.dot(x, y + b, APRON_SH if x > 23.5 + half - 2 else APRON)
    c.rect(18, 27 + b, 29, 27 + b, APRON_HI)          # the apron's top hem
    c.rect(20, 35 + b, 26, 39 + b, APRON_SH)          # its pocket
    c.rect(20, 35 + b, 26, 35 + b, APRON_HI)
    c.dot(23, 37 + b, GOLD)                            # a coin peeking out
    # Arms: the left holds a bottle; the right rests, or waves while he talks.
    c.line(14, 28 + b, 12, 36 + b, FUR)
    c.line(15, 28 + b, 13, 36 + b, FUR_SH)
    for y, row in enumerate(("..c..", ".rrr.", "ggggg", "LLLLL", "LLLLL", ".ddd.")):
        for x, ch in enumerate(row):
            col = {"c": (176, 128, 86, 255), "r": pal.rgba(pal.CORAL), "g": GLASS, "L": LILAC, "d": LILAC_SH}.get(ch)
            if col:
                c.dot(9 + x, 32 + y + b, col)
    if talk:
        c.line(32, 28 + b, 37, 21 + b, FUR)
        c.line(33, 28 + b, 38, 21 + b, FUR_SH)
        c.ellipse(38, 19.5 + b, 2, 2, FUR)
    else:
        c.line(32, 28 + b, 34, 36 + b, FUR)
        c.line(33, 28 + b, 35, 36 + b, FUR_SH)
        c.ellipse(34.5, 37 + b, 1.6, 1.6, FUR)
    # Head: wide and low, with the white stripe down the middle and white cheeks.
    hy = 15 + b
    for ex in (15, 32):                                # little round ears
        c.ellipse(ex, hy - 6, 2.6, 2.6, FUR_SH)
        c.ellipse(ex, hy - 6, 1.4, 1.4, MASK)
    c.ellipse(23.5, hy, 10, 8.5, FUR)
    c.ellipse(23.5, hy + 2, 9, 6.5, BADGER_WHITE)            # cheeks and muzzle
    for side in (-1, 1):                               # the dark bands over each eye
        for y in range(round(hy - 8), round(hy + 5)):
            x0 = 23.5 + side * 3
            for x in range(round(x0 + side * 0), round(x0 + side * 5) + side, side):
                if (x - 23.5) ** 2 / 100 + (y - hy) ** 2 / 72 <= 1:
                    c.dot(x, y, MASK)
    c.rect(22, round(hy - 8), 25, round(hy + 4), BADGER_WHITE)  # the white stripe down the middle
    c.ellipse(23.5, hy + 5, 2.4, 1.6, NOSE)            # nose
    c.dot(23, hy + 4, (110, 100, 120, 255))
    # Spectacles: gold rings, so his eyes read against the dark bands.
    for ex in (18.5, 28.5):
        for a in range(0, 360, 30):
            c.dot(round(ex + math.cos(math.radians(a)) * 2.6), round(hy - 1 + math.sin(math.radians(a)) * 2.6), GOLD)
        if blink:
            c.rect(round(ex) - 1, round(hy - 1), round(ex) + 1, round(hy - 1), BADGER_WHITE)
        else:
            c.rect(round(ex) - 1, round(hy - 2), round(ex), round(hy), BADGER_WHITE)
            c.dot(round(ex), round(hy - 1), EYE)
    c.rect(21, round(hy - 1), 26, round(hy - 1), GOLD)  # the bridge
    if talk:
        c.rect(22, round(hy + 7), 25, round(hy + 8), MOUTH)
    else:
        c.dot(22, round(hy + 7), NOSE)
        c.dot(25, round(hy + 7), NOSE)
        c.rect(23, round(hy + 8), 24, round(hy + 8), NOSE)
    return c.img


# ---------- Chickens ----------

HEN_WHITE = {"body": (250, 246, 238, 255), "shade": (214, 206, 204, 255), "wing": (232, 224, 220, 255)}
HEN_BROWN = {"body": (190, 112, 70, 255), "shade": (146, 80, 56, 255), "wing": (214, 140, 88, 255)}
COMB = pal.rgba(pal.CORAL)
BEAK = (250, 176, 60, 255)
LEGS = (232, 160, 60, 255)
CHICK, CHICK_SH = (255, 226, 110, 255), (232, 190, 70, 255)


def draw_hen(colors, step=0, peck=0):
    """A plump hen in profile, facing right. step 0/1 moves her feet; peck 0..2 bends her
    head down to the ground (2 = beak at the grain)."""
    c = Canvas()
    body, shade, wing = colors["body"], colors["shade"], colors["wing"]
    tilt = (0, 1, 2)[peck]
    # Legs.
    if step:
        c.line(14, 26, 13, 30, LEGS)
        c.line(17, 26, 19, 30, LEGS)
    else:
        c.line(14, 26, 15, 30, LEGS)
        c.line(17, 26, 16, 30, LEGS)
    # Tail feathers up at the back.
    c.ellipse(8, 18 + tilt * 0.5, 2.6, 4, shade)
    c.ellipse(9, 17 + tilt * 0.5, 2, 3.4, body)
    # Body.
    c.ellipse(15, 22 + tilt * 0.5, 7, 5.5, shade)
    c.ellipse(14.5, 21.5 + tilt * 0.5, 6.5, 4.8, body)
    c.ellipse(14, 22 + tilt * 0.5, 3.6, 2.4, wing)       # the wing
    c.line(12, 23 + round(tilt * 0.5), 16, 23 + round(tilt * 0.5), shade)
    # Neck and head, reaching down as she pecks.
    hx, hy = [(20, 14), (22, 19), (23, 25)][peck]
    c.line(18, 19 + tilt, hx, hy + 2, body)
    c.line(19, 19 + tilt, hx + 1, hy + 2, body)
    c.ellipse(hx + 0.5, hy, 2.6, 2.4, body)
    c.rect(hx - 1, hy - 4, hx + 1, hy - 3, COMB)         # the comb
    c.dot(hx, hy - 5, COMB)
    c.dot(hx + 1, hy + 3, COMB)                           # the wattle
    c.rect(hx + 3, hy, hx + 4, hy, BEAK)                  # the beak
    c.dot(hx + 3, hy + 1, BEAK)
    c.dot(hx + 1, hy - 1, EYE)
    return c.img


def draw_chick(bob=0):
    c = Canvas()
    y = 27 - bob
    c.line(15, y + 2, 15, 30, LEGS)
    c.line(17, y + 2, 17, 30, LEGS)
    c.ellipse(16, y, 3.6, 3, CHICK_SH)
    c.ellipse(15.6, y - 0.4, 3.2, 2.5, CHICK)
    c.ellipse(18, y - 3, 2, 2, CHICK)                     # head
    c.dot(18, y - 4, EYE)
    c.dot(20, y - 3, BEAK)
    if bob:
        c.dot(21, y - 3, BEAK)                            # cheep!
    c.dot(13, y - 1, CHICK_SH)
    return c.img


# ---------- Village props ----------

S = 64
OAK, OAK_SH, OAK_HI = (128, 88, 60, 255), (94, 62, 48, 255), (164, 120, 82, 255)
STONE, STONE_SH, STONE_HI = pal.rgba(pal.STONE), pal.rgba(pal.STONE_SHADE), pal.rgba(pal.STONE_LIGHT)
IRON, IRON_HI = (70, 62, 82, 255), (112, 104, 126, 255)
WATER = pal.rgba(pal.WATER_DEEP)
LAMP, LAMP_HI = pal.rgba(pal.BUTTER), pal.rgba(pal.BUTTER_LIGHT)
TILE, TILE_SH = (198, 112, 92, 255), (150, 76, 68, 255)
CANVAS, CANVAS_SH = pal.rgba(pal.CREAM), pal.rgba(pal.CREAM_SHADE)
STRIPE, STRIPE_SH = pal.rgba(pal.CORAL), (204, 92, 108, 255)
SOIL = (110, 78, 62, 255)
LEAF, LEAF_SH = (100, 142, 88, 255), (74, 110, 74, 255)
FLOWERS = [pal.rgba(pal.CORAL), pal.rgba(pal.BUTTER), (196, 150, 226, 255), pal.rgba(pal.CORAL_LIGHT)]
PAPER = [pal.rgba(pal.CREAM), pal.rgba(pal.CORAL_LIGHT), pal.rgba(pal.TURQUOISE_LIGHT), pal.rgba(pal.BUTTER_LIGHT)]
INK = pal.rgba(pal.PLUM_SOFT)
BURLAP, BURLAP_SH = (200, 170, 120, 255), (162, 132, 92, 255)
CORN, CORN_SH = (250, 214, 96, 255), (214, 170, 60, 255)


def well():
    """A round stone well with a little shingled roof on two oak posts and a bucket on a rope."""
    c = Canvas(S)
    c.ellipse(31.5, 54, 15, 6.5, STONE_SH)                # the ring of stones, seen from the front
    c.rect(16, 44, 47, 54, STONE)
    c.ellipse(31.5, 44, 15.5, 5, STONE_HI)
    c.ellipse(31.5, 44, 12.5, 3.4, WATER)                 # dark water inside
    c.ellipse(31.5, 45, 11, 2.4, (70, 120, 160, 255))
    for row, y in enumerate((47, 51)):                    # stone courses
        for x in range(17 + (row % 2) * 4, 47, 8):
            c.rect(x, y, x, y + 3, STONE_SH)
        c.rect(16, y, 47, y, STONE_SH)
    for x in (18, 44):                                     # posts
        c.rect(x, 18, x + 2, 44, OAK)
        c.rect(x + 2, 18, x + 2, 44, OAK_SH)
    c.rect(18, 24, 46, 25, OAK_SH)                          # the winch bar
    c.rect(31, 25, 31, 34, (200, 184, 150, 255))            # the rope
    c.rect(28, 34, 34, 39, OAK)                             # the bucket
    c.rect(28, 34, 34, 34, IRON_HI)
    c.rect(28, 38, 34, 38, IRON)
    for y in range(8, 20):                                  # the little roof
        half = (y - 8) * 1.6 + 2
        for x in range(round(31.5 - half), round(31.5 + half) + 1):
            c.dot(x, y, TILE if (x + y // 3) % 4 else TILE_SH)
    c.rect(11, 19, 52, 20, TILE_SH)
    return c.img


def lamppost():
    """A tall iron lamppost with a warm lantern on top, about 3m tall."""
    c = Canvas(S)
    c.rect(28, 58, 35, 61, IRON)                            # base
    c.rect(29, 56, 34, 57, IRON_HI)
    c.rect(30, 20, 33, 56, IRON)                            # post
    c.rect(30, 20, 30, 56, IRON_HI)
    c.rect(27, 18, 36, 19, IRON)                            # the lantern's tray
    c.rect(27, 8, 36, 17, IRON)                             # the lantern
    c.rect(28, 9, 35, 16, LAMP)
    c.rect(29, 10, 30, 13, LAMP_HI)
    c.rect(31, 9, 32, 16, IRON)
    for y in range(3, 8):                                   # its little hat
        half = (y - 3) + 1
        c.rect(round(31.5 - half), y, round(31.5 + half), y, IRON)
    c.dot(31, 2, IRON_HI)
    c.rect(36, 24, 41, 24, IRON)                            # a hook for a flower basket
    c.rect(38, 25, 41, 28, OAK)
    for i, x in enumerate((38, 39, 40, 41)):
        c.dot(x, 24 - (i % 2), FLOWERS[i % len(FLOWERS)])
    return c.img


def bench():
    """A wooden park bench with iron ends, facing the viewer."""
    c = Canvas(S)
    for x in (16, 45):                                      # iron ends
        c.rect(x, 46, x + 2, 60, IRON)
        c.rect(x, 46, x, 60, IRON_HI)
    for y in (36, 40):                                      # backrest slats
        c.rect(15, y, 48, y + 2, OAK)
        c.rect(15, y, 48, y, OAK_HI)
    c.rect(16, 34, 18, 46, IRON)
    c.rect(45, 34, 47, 46, IRON)
    for y in (47, 50):                                      # seat slats
        c.rect(14, y, 49, y + 2, OAK)
        c.rect(14, y, 49, y, OAK_HI)
        c.rect(14, y + 2, 49, y + 2, OAK_SH)
    return c.img


def planter():
    """A half-barrel tub of flowers."""
    c = Canvas(S)
    rng = random.Random(7)
    for y in range(46, 61):                                 # the tub
        half = 11 - (y - 46) * 0.12
        c.rect(round(31.5 - half), y, round(31.5 + half), y, OAK)
        c.dot(round(31.5 + half), y, OAK_SH)
        c.dot(round(31.5 + half) - 1, y, OAK_SH)
    for x in range(22, 42, 4):
        c.rect(x, 47, x, 60, OAK_SH)
    for y in (49, 57):
        c.rect(20, y, 43, y, IRON)
    c.ellipse(31.5, 46, 11, 2.6, SOIL)
    for _ in range(14):                                     # leaves
        x, y = rng.uniform(22, 41), rng.uniform(37, 45)
        c.ellipse(x, y, 2.6, 1.8, LEAF_SH if y > 42 else LEAF)
    for i in range(9):                                      # flowers
        x, y = rng.randint(22, 41), rng.randint(34, 43)
        col = FLOWERS[i % len(FLOWERS)]
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            c.dot(x + dx, y + dy, col)
        c.dot(x, y, pal.rgba(pal.BUTTER_LIGHT))
    return c.img


def notice_board():
    """A notice board on two legs with a little roof, papers pinned all over it."""
    c = Canvas(S)
    for x in (17, 44):                                      # legs
        c.rect(x, 22, x + 2, 61, OAK)
        c.rect(x + 2, 22, x + 2, 61, OAK_SH)
    c.rect(14, 24, 49, 48, OAK_SH)                          # the board
    c.rect(16, 26, 47, 46, (176, 132, 92, 255))
    rng = random.Random(3)
    for i, (x, y, w, h) in enumerate(((18, 28, 9, 8), (30, 27, 8, 10), (40, 29, 6, 7), (19, 38, 8, 7), (31, 39, 11, 6))):
        c.rect(x, y, x + w, y + h, PAPER[i % len(PAPER)])
        for ly in range(y + 2, y + h - 1, 2):              # scribbles
            c.rect(x + 1, ly, x + w - 1 - rng.randint(0, 3), ly, INK)
        c.dot(x + w // 2, y, pal.rgba(pal.CORAL))          # a pin
    for y in range(14, 24):                                 # the roof
        half = (y - 14) * 1.9 + 3
        c.rect(round(31.5 - half), y, round(31.5 + half), y, TILE if y % 3 else TILE_SH)
    return c.img


def stall():
    """Barnaby's market cart: a wooden counter lined with bottles of lilac bubble bath, under a
    striped awning on two poles, with a little price board (a coin) hanging off the front."""
    c = Canvas(S)
    c.rect(8, 44, 55, 58, OAK)                              # the counter
    c.rect(8, 44, 55, 45, OAK_HI)
    c.rect(8, 57, 55, 58, OAK_SH)
    for x in range(12, 55, 8):
        c.rect(x, 46, x, 56, OAK_SH)
    for x in (13, 49):                                      # wheels
        c.ellipse(x, 58, 4, 4, OAK_SH)
        c.ellipse(x, 58, 2, 2, OAK)
    for x in (9, 53):                                       # poles
        c.rect(x, 14, x + 1, 44, OAK_SH)
    for i, x in enumerate(range(13, 52, 6)):                # bottles of bubble bath
        h = 6 if i % 2 else 7
        c.rect(x, 43 - h, x + 3, 43, (238, 236, 250, 255))
        c.rect(x, 40 - (h - 6), x + 3, 43, (196, 150, 226, 255))
        c.rect(x + 1, 43 - h - 2, x + 2, 43 - h - 1, (238, 236, 250, 255))
        c.rect(x + 1, 43 - h - 3, x + 2, 43 - h - 3, (176, 128, 86, 255))
        c.dot(x, 43 - h + 1, pal.rgba(pal.CORAL))
    for cx, cy, r in ((22, 29, 2.2), (40, 27, 1.7), (31, 32, 1.3)):  # bubbles drifting up
        c.ellipse(cx, cy, r, r, (214, 232, 252, 255))
        c.dot(round(cx - r / 2), round(cy - r / 2), WHITE)
    for y in range(8, 20):                                  # the awning, striped, with scallops
        for x in range(5, 59):
            stripe = ((x - 5) // 6) % 2 == 0
            col = (STRIPE if stripe else CANVAS) if y < 15 else (STRIPE_SH if stripe else CANVAS_SH)
            if y >= 17:
                t = (((x - 5) % 6) + 0.5) / 6 * 2 - 1
                if y - 17 > round(2.5 * (1 - t * t) ** 0.5):
                    continue
            c.dot(x, y, col)
    c.rect(24, 48, 39, 55, pal.rgba(pal.CREAM))             # the price board: one coin
    c.ellipse(28.5, 51.5, 2.6, 2.6, GOLD)
    c.dot(28, 51, GOLD_HI)
    c.rect(32, 51, 37, 51, INK)
    return c.img


def coop():
    """A little wooden hen house on stilts, with a terracotta roof, a round door and a ramp."""
    c = Canvas(S)
    for x in (16, 45):                                      # stilts
        c.rect(x, 48, x + 2, 60, OAK_SH)
    c.rect(14, 30, 49, 48, OAK)                             # the house
    for x in range(14, 50, 5):
        c.rect(x, 30, x, 48, OAK_SH)
    c.rect(14, 47, 49, 48, OAK_SH)
    c.ellipse(31.5, 41, 4, 5, (52, 28, 54, 255))            # the round door
    c.rect(28, 41, 35, 47, (52, 28, 54, 255))
    for i in range(10):                                     # the ramp down to the ground
        c.rect(31 + i, 48 + i, 34 + i, 48 + i, OAK_HI if i % 2 else OAK)
    for y in range(14, 31):                                 # the roof
        half = (y - 14) * 1.25 + 4
        c.rect(round(31.5 - half), y, round(31.5 + half), y, TILE if (y // 3) % 2 else TILE_SH)
    c.rect(20, 34, 24, 36, pal.rgba(pal.BUTTER))            # straw poking out of a window
    c.dot(22, 33, pal.rgba(pal.BUTTER_LIGHT))
    return c.img


def grain_sack():
    """A burlap sack of corn, open at the top, with a few kernels spilled in front."""
    c = Canvas(S)
    c.ellipse(31.5, 54, 10, 7, BURLAP_SH)
    c.ellipse(31, 52, 9.5, 7.5, BURLAP)
    c.rect(23, 40, 40, 52, BURLAP)
    c.rect(37, 40, 40, 58, BURLAP_SH)
    c.ellipse(31.5, 40, 9, 2.6, CORN)
    c.ellipse(31.5, 40.5, 6, 1.4, CORN_SH)
    c.rect(22, 43, 41, 44, BURLAP_SH)                       # the tie
    for x, y in ((20, 60), (24, 61), (43, 59), (46, 61)):
        c.dot(x, y, CORN)
    c.rect(27, 46, 34, 49, BURLAP_SH)                       # a stencilled patch
    c.rect(29, 47, 32, 48, BURLAP)
    return c.img


def grain():
    """Seen from above, laid flat: corn kernels scattered for the hens."""
    c = Canvas()
    rng = random.Random(17)
    for _ in range(22):
        a, d = rng.uniform(0, 2 * math.pi), rng.uniform(0, 11) ** 0.9
        x, y = round(16 + math.cos(a) * d), round(16 + math.sin(a) * d)
        c.dot(x, y, CORN)
        if rng.random() < 0.5:
            c.dot(x + 1, y, CORN_SH)
    return c.img


def portrait(bust, background):
    img = Image.new("RGBA", (32, 32))
    px = img.load()
    r, g, b = background
    for y in range(32):
        for x in range(32):
            px[x, y] = (r + y, g + y // 2, b + y, 255)
    img.alpha_composite(outline(bust))
    return img


if __name__ == "__main__":
    write_sheet("Barnaby", None, [
        ("Idle", 3, True, [draw_barnaby(0), draw_barnaby(1), draw_barnaby(1, blink=True), draw_barnaby(0)]),
        ("Talk", 8, True, [draw_barnaby(0, talk=True), draw_barnaby(1)]),
    ], frame_size=B)
    write_sheet("Chicken", None, [
        ("Walk", 6, True, [draw_hen(HEN_WHITE, step=0), draw_hen(HEN_WHITE, step=1)]),
        ("Peck", 6, True, [draw_hen(HEN_WHITE, peck=1), draw_hen(HEN_WHITE, peck=2), draw_hen(HEN_WHITE, peck=1)]),
        ("WalkBrown", 6, True, [draw_hen(HEN_BROWN, step=0), draw_hen(HEN_BROWN, step=1)]),
        ("PeckBrown", 6, True, [draw_hen(HEN_BROWN, peck=1), draw_hen(HEN_BROWN, peck=2), draw_hen(HEN_BROWN, peck=1)]),
        ("Chick", 4, True, [draw_chick(0), draw_chick(1)]),
    ])
    write_sheet("TownProps", None, [
        ("Well", 1, False, [well()]),
        ("Lamppost", 1, False, [lamppost()]),
        ("Bench", 1, False, [bench()]),
        ("Planter", 1, False, [planter()]),
        ("NoticeBoard", 1, False, [notice_board()]),
        ("Stall", 1, False, [stall()]),
        ("Coop", 1, False, [coop()]),
        ("GrainSack", 1, False, [grain_sack()]),
    ], frame_size=S)
    write_sheet("TownDecals", None, [("Grain", 1, False, [grain()])], pivot="center", outline_color=None)
    head = draw_barnaby().crop((8, 2, 40, 34))
    portrait(head, (70, 90, 70)).save(ART / "UI" / "PortraitBarnaby.png")
