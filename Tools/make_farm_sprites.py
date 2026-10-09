"""Generates Hollow Farm, the haunted farmland south of the castle grounds: spooky, but friendly.

Run:  Tools/.venv/bin/python Tools/make_farm_sprites.py
Out:  Assets/Art/FarmProps.png / .json    (64x64 frames, bottom pivot)
        Scarecrow    2 frames, looping  a patched straw man on a pole, swaying, a crow on his arm
        Pumpkin      1 frame            a fat orange pumpkin on its vine
        JackOLantern 2 frames, looping  a carved pumpkin grinning, its candle flickering
        Corn         2 frames, looping  a clump of tall, dry corn stalks rustling
        DeadTree     1 frame            a bare, twisty tree
        Haystack     1 frame            a round haystack with a pitchfork in it
        Gravestone   1 frame            a mossy, rounded headstone (with a smiley carved on it)
        Crow         2 frames, looping  a crow on the ground, pecking
        Ghost        2 frames, looping  a little friendly sheet ghost, waving
        Wisp         2 frames, looping  a will-o'-the-wisp: a floating, pulsing glow
        Mist         1 frame            a low, pale wisp of ground fog (it drifts; see Drift.cs)
        Signpost     1 frame            "Hollow Farm": a crooked sign with a pumpkin on top
        FarmGate     1 frame            a wooden farm gate under an arch, with jack-o'-lanterns on its posts
        The autumn festival (the farm's south side): CiderStand (Pippin's stall: a barrel of cider
        with a tap, mugs, apples, an orange-striped awning), PumpkinStack,
        Bunting (pennants on a string between two poles), FestivalArch (the way in, garlanded
        with leaves and little pumpkins), BobbingTub (a tub of water with apples bobbing in it,
        2 frames), GiantPumpkin (a huge pumpkin with a blue first-prize ribbon)
      Assets/Art/FarmDecals.png / .json   (32x32, centre pivot; laid flat on the ground)
        Leaves  fallen autumn leaves
      Assets/Art/Pippin.png / .json       (48x48)  Pippin, the little ghost who runs the cider stand
        Idle  2 frames, looping   floating, a bob;   Talk  2 frames, looping   mouth open, waving a ladle
      Assets/Art/UI/PortraitPippin.png    32x32 dialogue portrait
      Assets/Art/Stitches.png / .json     (48x48)  Old Stitches, the scarecrow who talks
        Idle  2 frames, looping   a gentle sway;   Talk  2 frames, looping   mouth stitched open, arm flapping
      Assets/Art/UI/PortraitStitches.png  32x32 dialogue portrait

The farm is lit at dusk (DungeonBuilder.SetUpLighting, "mood: dusk"), so the pumpkins' orange and
the ghosts' pale lavender carry the scene; the props stay muted otherwise (see palette.py).
"""
import math
import random

from PIL import Image

import palette as pal
from sprite_common import ART, Canvas, outline, write_sheet

S = 64
STRAW, STRAW_SH, STRAW_HI = (222, 186, 100, 255), (176, 140, 70, 255), (246, 220, 140, 255)
BURLAP, BURLAP_SH = (196, 162, 112, 255), (150, 120, 84, 255)
PATCH = pal.rgba(pal.CORAL)
PLAID, PLAID_SH = (120, 96, 160, 255), (84, 64, 120, 255)
HAT, HAT_SH = (108, 84, 64, 255), (78, 58, 46, 255)
POLE, POLE_SH = (124, 88, 60, 255), (90, 62, 46, 255)
PUMPKIN, PUMPKIN_SH, PUMPKIN_HI = (236, 130, 50, 255), (190, 90, 36, 255), (255, 176, 90, 255)
STEM, VINE = (100, 120, 60, 255), (86, 116, 70, 255)
GLOW, GLOW_HI = (255, 214, 90, 255), (255, 246, 190, 255)
INSIDE = (120, 50, 30, 255)
CORN, CORN_SH, CORN_HI = (196, 170, 104, 255), (150, 126, 80, 255), (226, 204, 140, 255)
COB = (240, 200, 90, 255)
BARK, BARK_SH = (96, 80, 86, 255), (66, 54, 62, 255)
GRAVE, GRAVE_SH, GRAVE_HI = pal.rgba(pal.STONE_SHADE), pal.rgba(pal.ROCK), pal.rgba(pal.STONE)
MOSS = pal.rgba(pal.SAGE_DARK)
CROW, CROW_HI = (52, 44, 66, 255), (96, 86, 120, 255)
BEAK = (232, 176, 70, 255)
GHOST, GHOST_SH = (246, 242, 255, 255), (206, 196, 236, 255)
EYE = (52, 28, 54, 255)
BLUSH = pal.rgba(pal.CORAL_LIGHT)
WOOD, WOOD_SH, WOOD_HI = (138, 98, 66, 255), (100, 70, 52, 255), (172, 130, 92, 255)
SIGN = pal.rgba(pal.CREAM)
CLEAR = (0, 0, 0, 0)


def pumpkin_at(c, cx, cy, r, carved=False, flicker=0):
    """A ribbed pumpkin, wider than tall; carved: a jack-o'-lantern's grin with candlelight."""
    c.ellipse(cx, cy, r * 1.25, r, PUMPKIN_SH)
    for dx in (-r * 0.55, 0, r * 0.55):                    # three ribs
        c.ellipse(cx + dx - 0.5, cy - 0.5, r * 0.55, r * 0.92, PUMPKIN)
    c.ellipse(cx - r * 0.6, cy - r * 0.45, r * 0.25, r * 0.2, PUMPKIN_HI)
    c.rect(round(cx) - 1, round(cy - r) - 3, round(cx), round(cy - r), STEM)
    if carved:
        light = GLOW_HI if flicker else GLOW
        for ex in (-1, 1):                                  # triangle eyes
            x0 = round(cx + ex * r * 0.45)
            for i in range(3):
                c.rect(x0 - i, round(cy - r * 0.35) + i, x0 + i, round(cy - r * 0.35) + i, light)
        c.dot(round(cx), round(cy), light)                  # the nose
        y = round(cy + r * 0.35)                            # a toothy grin
        c.rect(round(cx - r * 0.7), y, round(cx + r * 0.7), y + 2, light)
        for x in range(round(cx - r * 0.5), round(cx + r * 0.6), 3):
            c.dot(x, y, PUMPKIN_SH)
            c.dot(x + 1, y + 2, PUMPKIN_SH)


def scarecrow(frame, crow=True):
    c = Canvas(S)
    sway = frame
    c.rect(31, 24, 33, 62, POLE)                            # the pole
    c.rect(33, 24, 33, 62, POLE_SH)
    c.rect(12 + sway, 27, 52 + sway, 29, POLE_SH)           # the cross-bar under his arms
    for x0, x1 in ((12, 24), (40, 52)):                     # plaid sleeves, straw poking out of the cuffs
        c.rect(x0 + sway, 25, x1 + sway, 30, PLAID)
        for x in range(x0, x1 + 1, 3):
            c.rect(x + sway, 25, x + sway, 30, PLAID_SH)
    for y in range(26, 31):
        c.dot(10 + sway, y, STRAW)
        c.dot(54 + sway, y, STRAW)
    c.rect(24 + sway, 24, 40 + sway, 44, PLAID)             # the shirt
    for x in range(24, 41, 4):
        c.rect(x + sway, 24, x + sway, 44, PLAID_SH)
    c.rect(28 + sway, 32, 32 + sway, 36, PATCH)             # a patch
    c.rect(24 + sway, 38, 40 + sway, 39, BURLAP_SH)         # a rope belt
    for x in (25, 29, 34, 38):                              # straw sticking out at the bottom
        c.rect(x + sway, 45, x + sway, 48, STRAW)
    c.ellipse(32 + sway, 18, 7, 7, BURLAP)                  # a burlap sack head
    c.ellipse(34 + sway, 20, 4, 4, BURLAP_SH)
    for ex in (29, 35):                                     # button eyes and a stitched smile
        c.rect(ex + sway, 16, ex + 1 + sway, 17, EYE)
    for x in range(28, 37, 2):
        c.dot(x + sway, 21 + (1 if 30 <= x <= 34 else 0), EYE)
    c.rect(22 + sway, 10, 42 + sway, 11, HAT)               # a floppy hat
    c.rect(26 + sway, 4, 38 + sway, 10, HAT)
    c.rect(26 + sway, 8, 38 + sway, 9, HAT_SH)
    c.dot(37 + sway, 5, STRAW)
    if crow:                                                # a crow perched on his arm
        c.ellipse(49 + sway, 21, 3.5, 2.5, CROW)
        c.ellipse(52 + sway, 18.5, 2, 2, CROW)
        c.dot(53 + sway, 18, (255, 255, 255, 255))
        c.rect(54 + sway, 19, 55 + sway, 19, BEAK)
        c.rect(45 + sway, 20, 46 + sway, 22, CROW_HI)
    return c.img


def pumpkin():
    c = Canvas(S)
    c.line(14, 60, 50, 58, VINE)                            # the vine, with a curl and a leaf
    c.ellipse(46, 56, 4, 2.5, VINE)
    c.dot(20, 58, VINE)
    pumpkin_at(c, 31.5, 52, 8)
    return c.img


def jack_o_lantern(frame):
    c = Canvas(S)
    pumpkin_at(c, 31.5, 53, 8, carved=True, flicker=frame)
    return c.img


def corn(frame):
    c = Canvas(S)
    rng = random.Random(3)
    for i, x in enumerate((20, 27, 34, 41)):
        lean = (frame if i % 2 else -frame) * 0.5
        top = 8 + rng.randint(0, 8)
        for y in range(top, 62):
            t = (62 - y) / (62 - top)
            c.dot(round(x + lean * t * 3), y, CORN_SH if y > 54 else CORN_HI)
            c.dot(round(x + lean * t * 3) + 1, y, CORN_SH)
        for k in range(4):                                  # leaves drooping off each stalk
            y = top + 8 + k * 11
            side = 1 if (k + i) % 2 else -1
            lx = x + lean * 2
            for j in range(10):
                yy = round(y + j * 0.5 + (j * j) * 0.05)
                c.dot(round(lx + side * j), yy, CORN_HI if j < 3 else CORN)
                c.dot(round(lx + side * j), yy + 1, CORN_SH)
        if i in (1, 2):                                     # a cob
            c.rect(x + 1, top + 20, x + 2, top + 26, COB)
    return c.img


def dead_tree():
    c = Canvas(S)
    c.rect(28, 30, 35, 62, BARK)
    c.rect(33, 30, 35, 62, BARK_SH)
    c.rect(24, 60, 39, 62, BARK_SH)                         # roots
    branches = [((31, 32), (14, 14)), ((32, 28), (48, 10)), ((30, 40), (12, 30)), ((34, 38), (52, 26)),
                ((22, 22), (16, 6)), ((42, 18), (50, 4)), ((40, 18), (36, 6))]
    for (x0, y0), (x1, y1) in branches:
        c.line(x0, y0, x1, y1, BARK)
        c.line(x0 + 1, y0, x1 + 1, y1, BARK_SH)
    c.ellipse(31.5, 46, 2.4, 3, (40, 30, 44, 255))          # a knot-hole
    return c.img


def haystack():
    c = Canvas(S)
    c.ellipse(31.5, 50, 20, 12, STRAW_SH)
    c.ellipse(31, 47, 19, 13, STRAW)
    for x in range(14, 50, 3):                              # straw texture
        c.line(x, 38 + (x % 5), x + 2, 56, STRAW_SH)
    c.ellipse(26, 40, 7, 3, STRAW_HI)
    c.line(40, 18, 44, 44, POLE)                            # a pitchfork stuck in it
    c.rect(37, 15, 43, 16, (150, 150, 166, 255))
    for x in (37, 40, 43):
        c.rect(x, 10, x, 15, (150, 150, 166, 255))
    return c.img


def gravestone():
    c = Canvas(S)
    c.rect(22, 38, 41, 60, GRAVE_SH)
    c.ellipse(31.5, 38, 10, 8, GRAVE_SH)
    c.rect(23, 38, 40, 60, GRAVE)
    c.ellipse(31.5, 38, 9, 7, GRAVE)
    c.rect(23, 32, 26, 50, GRAVE_HI)
    for ex in (28, 35):                                     # a carved smiley: it's a friendly graveyard
        c.rect(ex, 41, ex + 1, 42, GRAVE_SH)
    for x in range(28, 37):
        c.dot(x, 46 + (1 if 30 <= x <= 33 else 0), GRAVE_SH)
    c.rect(20, 58, 28, 61, MOSS)
    c.rect(34, 59, 42, 61, MOSS)
    c.ellipse(26, 32, 3, 1.5, MOSS)
    return c.img


def crow(frame):
    c = Canvas(S)
    peck = frame
    c.line(29, 56, 28, 61, BEAK)
    c.line(33, 56, 34, 61, BEAK)
    c.ellipse(31, 52, 7, 5, CROW)                           # body
    c.line(24, 50, 18, 46, CROW)                            # tail
    c.line(24, 51, 18, 48, CROW_HI)
    c.ellipse(29, 51, 4, 2.5, CROW_HI)                      # wing
    hx, hy = (37, 45) if not peck else (39, 52)
    c.ellipse(hx, hy, 3.4, 3.2, CROW)
    c.dot(hx + 1, hy - 1, (255, 255, 255, 255))
    c.rect(hx + 3, hy, hx + 5, hy, BEAK)
    return c.img


def ghost(frame):
    c = Canvas(S)
    bob = frame
    for y in range(20, 52):                                 # a sheet, wider at the bottom
        half = 9 + max(0, y - 30) * 0.15
        c.rect(round(31.5 - half), y + bob, round(31.5 + half), y + bob, GHOST)
        c.dot(round(31.5 + half), y + bob, GHOST_SH)
    c.ellipse(31.5, 22 + bob, 9, 8, GHOST)
    for i, x in enumerate(range(21, 43, 4)):                # a wavy hem
        c.ellipse(x + 1.5, 52 + bob + (i + frame) % 2, 2, 2, GHOST)
    for ex in (27, 35):                                     # big eyes, rosy cheeks, an "oooh" mouth
        c.ellipse(ex, 25 + bob, 1.6, 2.2, EYE)
        c.dot(ex - 1, 24 + bob, (255, 255, 255, 255))
    c.dot(25, 29 + bob, BLUSH)
    c.dot(38, 29 + bob, BLUSH)
    c.ellipse(31.5, 31 + bob, 1.5, 2, EYE)
    arm = -3 if frame else 0                                # waving
    c.ellipse(43, 34 + bob + arm, 3, 2, GHOST)
    return c.img


def wisp(frame):
    c = Canvas(S)
    r = 3 + frame
    c.ellipse(31.5, 50, r + 2, r + 2, (180, 240, 220, 120))
    c.ellipse(31.5, 50, r, r, (210, 255, 236, 220))
    c.ellipse(31, 49, 1.4, 1.4, (255, 255, 255, 255))
    return c.img


def mist():
    c = Canvas(S)
    for cx, cy, rx, ry in ((18, 54, 14, 4), (34, 52, 16, 5), (48, 55, 12, 3.5)):
        for y in range(round(cy - ry), round(cy + ry) + 1):
            for x in range(round(cx - rx), round(cx + rx) + 1):
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                if d <= 1:
                    a = round(90 * (1 - d))
                    old = c.img.getpixel((x, y))
                    c.dot(x, y, (226, 220, 246, max(a, old[3])))
    return c.img


def signpost():
    c = Canvas(S)
    c.rect(30, 30, 33, 62, POLE)
    c.rect(33, 30, 33, 62, POLE_SH)
    pts = [(10, 32), (54, 28), (54, 44), (10, 46)]          # a crooked board
    for y in range(26, 48):
        for x in range(8, 57):
            t = (x - 10) / 44
            top = 32 - t * 4
            if top <= y <= top + 13:
                c.dot(x, y, SIGN if 9 < x < 55 and top + 1 < y < top + 12 else WOOD_SH)
    # "HOLLOW FARM" as little pumpkin-orange marks (too small for letters): two words of blocks.
    for i, x in enumerate(list(range(14, 31, 3)) + list(range(36, 51, 3))):
        y = round(36 - (x - 10) / 44 * 4)
        c.rect(x, y, x + 1, y + 3, PUMPKIN_SH)
    pumpkin_at(c, 46, 21, 5, carved=True)
    return c.img


def farm_gate():
    """A wooden farm gate (closed) under a rustic arch, jack-o'-lanterns on both posts."""
    c = Canvas(S)
    for x in (6, 54):                                       # posts
        c.rect(x, 14, x + 4, 62, WOOD)
        c.rect(x + 3, 14, x + 4, 62, WOOD_SH)
    c.rect(4, 10, 60, 14, WOOD)                             # the arch beam
    c.rect(4, 10, 60, 10, WOOD_HI)
    c.rect(4, 14, 60, 14, WOOD_SH)
    for y in (34, 46, 57):                                  # the gate's rails
        c.rect(11, y, 53, y + 2, WOOD_HI)
        c.rect(11, y + 2, 53, y + 2, WOOD_SH)
    for x in range(14, 52, 7):                              # and its uprights
        c.rect(x, 32, x + 2, 60, WOOD)
    c.line(12, 59, 52, 34, WOOD_SH)                         # the diagonal brace
    c.line(12, 58, 52, 33, WOOD)
    pumpkin_at(c, 8.5, 9, 4.5, carved=True)
    pumpkin_at(c, 56.5, 9, 4.5, carved=True)
    return c.img


# ---------- The autumn festival ----------

APPLE, APPLE_SH, APPLE_HI = pal.rgba(pal.CORAL), (196, 82, 104, 255), pal.rgba(pal.CORAL_LIGHT)
GREEN_APPLE = (170, 206, 100, 255)
CIDER, CIDER_SH = (214, 140, 60, 255), (170, 100, 44, 255)
AWNING, AWNING_SH = (236, 140, 60, 255), (196, 104, 40, 255)
CREAM, CREAM_SH = pal.rgba(pal.CREAM), pal.rgba(pal.CREAM_SHADE)
MUG = (196, 120, 76, 255)
WATER, WATER_HI = pal.rgba(pal.WATER), pal.rgba(pal.WATER_GLINT)
RIBBON, RIBBON_SH = (80, 130, 220, 255), (54, 92, 170, 255)
GOLD = pal.rgba(pal.HONEY)
LEAF_COLORS = [(224, 120, 50, 255), (236, 176, 60, 255), (190, 70, 50, 255), (210, 150, 70, 255)]
PENNANTS = [AWNING, pal.rgba(pal.BUTTER), APPLE, (150, 110, 190, 255), pal.rgba(pal.TURQUOISE)]


def apple_at(c, x, y, color=APPLE):
    c.ellipse(x, y, 2.2, 2, color)
    c.dot(round(x - 1), round(y - 1), APPLE_HI if color == APPLE else (210, 236, 150, 255))
    c.dot(round(x), round(y - 2), STEM)


def cider_stand():
    """Pippin's cider stand: a counter with a big barrel of cider (a tap, a mug under it), mugs
    and apples along the counter, and an orange-striped awning on two poles."""
    c = Canvas(S)
    c.rect(6, 40, 57, 60, WOOD)                             # the counter
    c.rect(6, 40, 57, 41, WOOD_HI)
    c.rect(6, 59, 57, 60, WOOD_SH)
    for x in range(10, 57, 8):
        c.rect(x, 42, x, 58, WOOD_SH)
    c.rect(18, 46, 45, 54, CREAM)                           # a sign: a mug and an apple
    c.rect(22, 48, 26, 53, MUG)
    c.rect(27, 49, 28, 51, MUG)
    c.rect(23, 48, 25, 48, CIDER)
    apple_at(c, 37, 50)
    for x in (8, 55):                                       # poles
        c.rect(x, 12, x + 1, 40, WOOD_SH)
    for y in range(8, 20):                                  # the awning, with scallops
        for x in range(4, 60):
            stripe = ((x - 4) // 6) % 2 == 0
            col = (AWNING if stripe else CREAM) if y < 15 else (AWNING_SH if stripe else CREAM_SH)
            if y >= 17:
                t = (((x - 4) % 6) + 0.5) / 6 * 2 - 1
                if y - 17 > round(2.5 * (1 - t * t) ** 0.5):
                    continue
            c.dot(x, y, col)
    c.ellipse(44, 31, 8, 9, WOOD)                           # the cider barrel, on its side-stand
    for x in (38, 44, 50):
        c.rect(x, 23, x, 39, WOOD_SH)
    c.rect(36, 26, 52, 27, (90, 90, 104, 255))
    c.rect(36, 35, 52, 36, (90, 90, 104, 255))
    c.rect(34, 31, 36, 32, GOLD)                            # its tap...
    c.rect(34, 33, 34, 34, GOLD)
    c.rect(32, 36, 36, 39, MUG)                             # ...and a mug filling up under it
    c.rect(33, 36, 35, 36, CIDER)
    for x in (12, 18):                                      # mugs on the counter
        c.rect(x, 34, x + 4, 39, MUG)
        c.rect(x + 1, 34, x + 3, 34, CIDER)
    for x, col in ((25, APPLE), (29, GREEN_APPLE), (27, APPLE)):
        apple_at(c, x, 38 if x != 27 else 35, col)
    return c.img


def pumpkin_stack():
    """Pumpkins piled on a hay bale: a big one, two little ones, and a jack-o'-lantern on top."""
    c = Canvas(S)
    c.rect(16, 52, 47, 61, STRAW)
    c.rect(16, 52, 47, 53, STRAW_HI)
    c.rect(44, 52, 47, 61, STRAW_SH)
    pumpkin_at(c, 24, 46, 6)
    pumpkin_at(c, 40, 47, 5)
    pumpkin_at(c, 32, 36, 6, carved=True)
    return c.img


def bunting():
    """A string of pennants in festival colours, sagging between two poles."""
    c = Canvas(S)
    for x in (3, 60):
        c.rect(x, 10, x + 1, 62, WOOD_SH)
        c.dot(x, 9, GOLD)
    for i, x in enumerate(range(6, 59, 5)):
        t = (x - 4) / 56
        y = round(14 + 8 * math.sin(t * math.pi))           # the string sags
        c.dot(x, y, WOOD_SH)
        c.dot(x + 1, y, WOOD_SH)
        col = PENNANTS[i % len(PENNANTS)]
        for k in range(5):
            c.rect(x - 2 + k // 2, y + 1 + k, x + 3 - k // 2, y + 1 + k, col)
        for xx in range(x + 2, x + 5):
            c.dot(xx, round(14 + 8 * math.sin(((xx) - 4) / 56 * math.pi)), WOOD_SH)
    return c.img


def festival_arch():
    """The festival's way in: a wooden arch wound with autumn leaves, little pumpkins along the
    top and a sign ("AUTUMN FESTIVAL" as little orange marks) hanging under it."""
    c = Canvas(S)
    for x in (4, 56):
        c.rect(x, 8, x + 3, 62, WOOD)
        c.rect(x + 3, 8, x + 3, 62, WOOD_SH)
    c.rect(2, 6, 61, 10, WOOD)
    c.rect(2, 6, 61, 6, WOOD_HI)
    rng = random.Random(4)
    for _ in range(60):                                     # a garland of leaves along the beam and posts
        if rng.random() < 0.55:
            x, y = rng.uniform(2, 61), rng.uniform(4, 12)
        else:
            x, y = rng.choice((rng.uniform(3, 9), rng.uniform(55, 61))), rng.uniform(10, 58)
        c.ellipse(x, y, 1.6, 1.2, LEAF_COLORS[rng.randrange(len(LEAF_COLORS))])
    for x in (16, 32, 48):
        pumpkin_at(c, x, 4, 3)
    c.rect(18, 15, 45, 23, CREAM)                           # the sign, on two cords
    c.rect(18, 23, 45, 23, CREAM_SH)
    c.line(20, 11, 20, 15, WOOD_SH)
    c.line(43, 11, 43, 15, WOOD_SH)
    for x in list(range(21, 31, 2)) + list(range(33, 43, 2)):
        c.rect(x, 18, x, 20, AWNING_SH)
    return c.img


def bobbing_tub(frame):
    """A wooden tub of water with apples bobbing in it, for bobbing for apples."""
    c = Canvas(S)
    for y in range(46, 61):
        half = 15 - (y - 46) * 0.15
        c.rect(round(31.5 - half), y, round(31.5 + half), y, WOOD)
    for x in range(18, 46, 5):
        c.rect(x, 47, x, 60, WOOD_SH)
    for y in (49, 57):
        c.rect(16, y, 47, y, (90, 90, 104, 255))
    c.ellipse(31.5, 45, 15, 3.5, WOOD_SH)
    c.ellipse(31.5, 45, 13.5, 2.6, WATER)
    c.rect(22 + frame, 45, 25 + frame, 45, WATER_HI)
    for i, (x, col) in enumerate(((24, APPLE), (31, GREEN_APPLE), (38, APPLE), (34, APPLE))):
        apple_at(c, x, 44 - ((i + frame) % 2), col)
    return c.img


def giant_pumpkin():
    """A prize-winning giant pumpkin on a pallet, with a blue first-prize rosette."""
    c = Canvas(S)
    c.rect(8, 58, 55, 61, WOOD)
    c.rect(8, 58, 55, 58, WOOD_HI)
    pumpkin_at(c, 31.5, 44, 15)
    c.ellipse(46, 40, 4.5, 4.5, RIBBON)                     # the rosette
    c.ellipse(46, 40, 2.4, 2.4, GOLD)
    c.line(44, 44, 42, 52, RIBBON_SH)
    c.line(48, 44, 50, 52, RIBBON_SH)
    c.dot(46, 40, (255, 246, 200, 255))
    return c.img


def leaves():
    """Fallen leaves, seen from above, laid flat."""
    c = Canvas()
    rng = random.Random(21)
    for _ in range(9):
        x, y = rng.uniform(3, 28), rng.uniform(3, 28)
        col = LEAF_COLORS[rng.randrange(len(LEAF_COLORS))]
        c.ellipse(x, y, 1.8, 1.1, col)
        c.dot(round(x + 1.5), round(y + 1), STEM)
    return c.img


# ---------- Pippin, the little ghost at the cider stand ----------

def pippin(bob=0, talk=False):
    c = Canvas(T)
    b = bob
    for y in range(12, 38):
        half = 8 + max(0, y - 22) * 0.15
        c.rect(round(23.5 - half), y + b, round(23.5 + half), y + b, GHOST)
        c.dot(round(23.5 + half), y + b, GHOST_SH)
    c.ellipse(23.5, 14 + b, 8, 7, GHOST)
    for i, x in enumerate(range(15, 33, 4)):
        c.ellipse(x + 1.5, 38 + b + (i + bob) % 2, 2, 2, GHOST)
    c.rect(15, 21 + b, 32, 23 + b, AWNING)                  # an orange-and-cream scarf
    for x in range(15, 33, 4):
        c.rect(x, 21 + b, x + 1, 23 + b, CREAM)
    c.rect(27, 24 + b, 29, 29 + b, AWNING)
    for ex in (20, 27):
        c.ellipse(ex, 15 + b, 1.4, 2, EYE)
        c.dot(ex - 1, 14 + b, (255, 255, 255, 255))
    c.dot(18, 18 + b, BLUSH)
    c.dot(29, 18 + b, BLUSH)
    if talk:
        c.ellipse(23.5, 19 + b, 1.6, 1.6, EYE)
        c.line(32, 26 + b, 38, 18 + b, WOOD_SH)               # waving a ladle
        c.ellipse(39, 17 + b, 2, 1.5, (150, 150, 166, 255))
    else:
        c.dot(22, 19 + b, EYE)
        c.dot(23, 20 + b, EYE)
        c.dot(24, 20 + b, EYE)
        c.dot(25, 19 + b, EYE)
    return c.img


# ---------- Old Stitches, the scarecrow who talks ----------

T = 48


def stitches(sway=0, talk=False):
    c = Canvas(T)
    s = sway
    c.rect(23, 20, 25, 47, POLE)
    c.rect(25, 20, 25, 47, POLE_SH)
    c.rect(18 + s, 18, 30 + s, 33, PLAID)                   # shirt
    for x in range(18, 31, 3):
        c.rect(x + s, 18, x + s, 33, PLAID_SH)
    c.rect(20 + s, 23, 23 + s, 26, PATCH)
    arm_y = 14 if talk else 20                              # one arm flaps while he talks
    c.rect(6 + s, 19, 18 + s, 22, PLAID)                    # the other stays put on the bar
    c.line(30 + s, 20, 40 + s, arm_y, PLAID)
    c.line(30 + s, 21, 40 + s, arm_y + 1, PLAID_SH)
    for (x, y) in ((5, 20), (5, 21), (41, arm_y), (42, arm_y + 1)):
        c.dot(x + s, y, STRAW)
    for x in (19, 22, 26, 29):
        c.rect(x + s, 34, x + s, 37, STRAW)
    c.ellipse(24 + s, 12, 6, 6, BURLAP)                     # head
    c.ellipse(26 + s, 14, 3, 3, BURLAP_SH)
    for ex in (21, 26):
        c.rect(ex + s, 10, ex + 1 + s, 11, EYE)
    if talk:
        c.rect(21 + s, 14, 27 + s, 16, EYE)
        for x in range(21, 28, 2):
            c.dot(x + s, 14, BURLAP)                        # stitches across the open mouth
    else:
        for x in range(20, 29, 2):
            c.dot(x + s, 15 + (1 if 22 <= x <= 26 else 0), EYE)
    c.rect(15 + s, 5, 33 + s, 6, HAT)                       # hat
    c.rect(19 + s, 0, 29 + s, 5, HAT)
    c.rect(19 + s, 3, 29 + s, 4, PATCH)                     # a coral band
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
    write_sheet("FarmProps", None, [
        ("Scarecrow", 1, True, [scarecrow(0), scarecrow(1)]),
        ("Pumpkin", 1, False, [pumpkin()]),
        ("JackOLantern", 3, True, [jack_o_lantern(0), jack_o_lantern(1)]),
        ("Corn", 1, True, [corn(0), corn(1)]),
        ("DeadTree", 1, False, [dead_tree()]),
        ("Haystack", 1, False, [haystack()]),
        ("Gravestone", 1, False, [gravestone()]),
        ("Crow", 2, True, [crow(0), crow(0), crow(1)]),
        ("Ghost", 2, True, [ghost(0), ghost(1)]),
        ("Wisp", 3, True, [wisp(0), wisp(1)], None),
        ("Mist", 1, False, [mist()], None),
        ("Signpost", 1, False, [signpost()]),
        ("FarmGate", 1, False, [farm_gate()]),
        ("CiderStand", 1, False, [cider_stand()]),
        ("PumpkinStack", 1, False, [pumpkin_stack()]),
        ("Bunting", 1, False, [bunting()]),
        ("FestivalArch", 1, False, [festival_arch()]),
        ("BobbingTub", 2, True, [bobbing_tub(0), bobbing_tub(1)]),
        ("GiantPumpkin", 1, False, [giant_pumpkin()]),
    ], frame_size=S)
    write_sheet("FarmDecals", None, [("Leaves", 1, False, [leaves()])], pivot="center", outline_color=None)
    write_sheet("Pippin", None, [
        ("Idle", 2, True, [pippin(0), pippin(1)]),
        ("Talk", 6, True, [pippin(0, talk=True), pippin(1)]),
    ], frame_size=T)
    portrait(pippin().crop((8, 4, 40, 36)), (90, 50, 40)).save(ART / "UI" / "PortraitPippin.png")
    write_sheet("Stitches", None, [
        ("Idle", 2, True, [stitches(0), stitches(1)]),
        ("Talk", 6, True, [stitches(0, talk=True), stitches(1)]),
    ], frame_size=T)
    portrait(stitches().crop((8, 0, 40, 32)), (60, 40, 70)).save(ART / "UI" / "PortraitStitches.png")
