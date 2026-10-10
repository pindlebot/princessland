"""Generates the art for the Whispering Woods, and for the new systems that come with it.

Run:  Tools/.venv/bin/python Tools/make_woods_sprites.py
Out:  Assets/Art/SporePuff.png / .json       32x32: a stationary puffball that coughs spores when you come close
      Assets/Art/MotherMushroom.png / .json  64x64: the Woods' boss, a big red-capped toadstool with a stern, kind face
      Assets/Art/OldMoss.png / .json         48x48 NPC: a mossy old treefolk gardener (Idle, Talk)
      Assets/Art/UI/PortraitOldMoss.png      32x32 dialogue portrait
      Assets/Art/SleepyTree.png / .json      64x64: a tree in a nightcap, asleep (Sleep) and awake (Awake)
      Assets/Art/Pot.png / .json             32x32: a clay pot (Idle)
      Assets/Art/PotShards.png / .json       32x32 effect: the pot smashing (Break)
      Assets/Art/WoodsProps.png / .json      48x48: Giant Mushroom (solid) and Glowcaps (a glowing cluster)
      Assets/Art/WoodsItems.png / .json      32x32 floor frames, and Assets/Art/UI/Icon<Item>.png 24x24 icons:
        FrogHat, HealingApple, ManaBerry, FairyLantern, CloverCharm
      Assets/Art/UI/IconFrog.png, IconTree.png, IconMonster.png   24x24 pictures for the quest log

Same conventions as the other monsters (see make_farm_monster_sprites.py): Front / Back for Idle, Walk,
Attack and Hurt, a single Die, and "Attack" as the action state, so they share the one Animator state
machine. (The Spore Puff never walks, so its Walk is a gentle wobble: it's stationary in the game.)
"""
import math

from PIL import Image

import palette as pal
from make_item_sprites import floor_frame, icon, pattern
from sprite_common import ART, CLEAR, F, Canvas, outline, tint, write_sheet

UI = ART / "UI"

# ---------- Palette: the woods are mossy greens, with plum-red and cream for the mushrooms ----------
MOSS, MOSS_HI, MOSS_SH = (110, 150, 84, 255), (158, 192, 108, 255), (72, 108, 70, 255)
BARK, BARK_HI, BARK_SH = (126, 88, 66, 255), (164, 120, 88, 255), (88, 60, 54, 255)
CAP, CAP_HI, CAP_SH = (214, 80, 96, 255), (244, 132, 140, 255), (150, 50, 78, 255)
CREAM, CREAM_SH = pal.rgba(pal.CREAM), pal.rgba(pal.CREAM_SHADE)
SPOT = (250, 240, 224, 255)
LILAC, LILAC_HI, LILAC_SH = (188, 160, 206, 255), (224, 204, 236, 255), (132, 104, 160, 255)
SPORE_G, SPORE_HI = (176, 216, 112, 255), (232, 250, 170, 255)
EYE = (52, 28, 54, 255)
BLUSH = (246, 150, 150, 255)
GLOW_BLUE, GLOW_HI = (110, 200, 236, 255), (214, 244, 255, 255)
NIGHT, NIGHT_SH = (92, 104, 190, 255), (62, 72, 144, 255)
STAR = pal.rgba(pal.BUTTER)
STAR_HI = pal.rgba(pal.BUTTER_LIGHT)


def spores(c, cx, cy, spread, seed=0, n=10, color=SPORE_G):
    """A little cloud of spore dots round a point."""
    import random
    rng = random.Random(seed)
    for _ in range(n):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(spread * 0.3, spread)
        x, y = round(cx + math.cos(a) * r), round(cy + math.sin(a) * r * 0.7)
        c.dot(x, y, color)
        if rng.random() < 0.4:
            c.dot(x + 1, y, SPORE_HI)


# ---------- Spore Puff ----------

def draw_puff(back=False, w=9.0, h=8.0, lift=0, puff=0, hurt=False, size=32):
    """A round lilac puffball with a stubby cream stalk. w/h: half-width and height (squash and
    stretch); lift raises it; puff > 0 bursts a cloud of spores round it (its attack)."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    ground = round(size - 3 - lift)
    cy = ground - h - 3
    # The stalk.
    c.rect(round(cx - 3), ground - 4, round(cx + 3), ground, CREAM)
    c.rect(round(cx + 2), ground - 4, round(cx + 3), ground, CREAM_SH)
    c.rect(round(cx - 4), ground, round(cx + 4), ground, CREAM_SH)
    # The body: a fat round puffball, lit from the top left.
    c.ellipse(cx, cy, w, h, LILAC_SH)
    c.ellipse(cx - 0.8, cy - 0.8, w - 1.2, h - 1.2, LILAC)
    c.ellipse(cx - w * 0.38, cy - h * 0.42, w * 0.28, h * 0.22, LILAC_HI)
    for dx, dy in ((0.35, -0.45), (0.5, 0.2), (-0.1, 0.5), (0.05, -0.15)):      # soft darker spots
        c.dot(round(cx + dx * w), round(cy + dy * h), LILAC_SH)
        c.dot(round(cx + dx * w) + 1, round(cy + dy * h), LILAC_SH)
    if not back:
        ey = round(cy - h * 0.05)
        eyes = (255, 255, 255, 255) if hurt else EYE
        for side in (-1, 1):                                  # two sleepy, grumpy eyes
            ex = round(cx + side * w * 0.38)
            c.rect(ex - 1, ey, ex, ey + 1, eyes)
            # a heavy brow, high on the outside and low by the middle: grumpy
            c.line(ex - 2 if side < 0 else ex - 1, ey - 3 if side < 0 else ey - 2, ex + 1 if side < 0 else ex + 2,
                   ey - 2 if side < 0 else ey - 3, EYE)
        if puff:
            c.ellipse(cx, cy + h * 0.45, 2.5, 2, EYE)          # mouth: an "O", puffing
            c.dot(round(cx), round(cy + h * 0.45), BLUSH)
        else:
            c.rect(round(cx) - 2, round(cy + h * 0.5), round(cx) + 2, round(cy + h * 0.5), EYE)
    if puff:
        spores(c, cx, cy, 11 + puff * 2, seed=puff, n=8 + puff * 3)
    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.35)
    return img


def puff_die(stage, size=32):
    """Deflates: squashes flat, then a little heap with spores floating away."""
    if stage < 2:
        return draw_puff(w=10 + stage * 2, h=6 - stage * 2.2, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 3
    c.ellipse(cx, ground - 1, 11, 2, LILAC_SH)
    c.ellipse(cx, ground - 2, 10, 2, LILAC)
    for x in (cx - 5, cx, cx + 5):
        c.dot(round(x), ground - 2, LILAC_HI)
    spores(c, cx, ground - 8, 9, seed=7 + stage, n=7)
    img = c.img
    if stage == 3:
        px = img.load()
        for y in range(img.height):
            for x in range(img.width):
                r, g, b, a = px[x, y]
                px[x, y] = (r, g, b, a // 2)
    return img


def puff_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_puff(b), draw_puff(b, w=9.5, h=7.6), draw_puff(b, w=8.6, h=8.4), draw_puff(b, w=9.2, h=7.8)]),
            (f"Walk_{facing}", 6, True, [draw_puff(b, w=9.4, h=7.6), draw_puff(b, w=8.6, h=8.4, lift=1)]),
            (f"Attack_{facing}", 8, False, [draw_puff(b, w=10.5, h=6.4), draw_puff(b, w=8, h=9.4, puff=1), draw_puff(b, w=9, h=8, puff=2),
                                           draw_puff(b, puff=3)]),
            (f"Hurt_{facing}", 8, False, [draw_puff(b, w=10.5, h=6, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [puff_die(s) for s in range(4)]))
    return anims


# ---------- Mother Mushroom ----------

def draw_mother(back=False, bob=0, squash=0.0, shake=0, burst=0, hurt=False, size=64, step=0):
    """A big toadstool: a plum-red cap with cream spots and a pale underside, on a thick cream stalk with
    a stern but kind face and two leafy arms. squash/bob move the cap; shake rattles it; burst puffs spores."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    ground = size - 3
    top = 6 + bob + round(squash * 3)
    # Feet and the stalk: wide at the bottom, like a mushroom's.
    for dx in (-8, 8):
        c.ellipse(cx + dx + (step if dx > 0 else -step), ground - 1, 5, 2.4, CREAM_SH)
    for y in range(top + 24, ground):
        t = (y - (top + 24)) / max(1, ground - (top + 24))
        half = 9 + t * 5
        c.rect(round(cx - half), y, round(cx + half), y, CREAM)
        c.dot(round(cx + half), y, CREAM_SH)
        c.dot(round(cx + half) - 1, y, CREAM_SH)
        c.dot(round(cx - half), y, (255, 252, 240, 255))
    # Leafy arms.
    for side in (-1, 1):
        ax = round(cx + side * 13)
        ay = top + 34 - (3 if burst and side else 0)
        c.line(round(cx + side * 9), top + 30, ax + side * 4, ay, MOSS_SH)
        c.line(round(cx + side * 9), top + 31, ax + side * 4, ay + 1, MOSS)
        c.ellipse(ax + side * 6, ay + 1, 3.5, 2, MOSS_HI)
        c.dot(ax + side * 6, ay + 1, MOSS)
    # The underside of the cap (gills), then the cap itself.
    sx = shake
    c.ellipse(cx + sx, top + 22, 25, 5.5, CREAM_SH)
    for gx in range(-22, 23, 3):
        c.line(round(cx + sx + gx), top + 20, round(cx + sx + gx * 0.9), top + 26, (214, 196, 170, 255))
    cap_h = 20 - squash * 4
    c.ellipse(cx + sx, top + 14, 27, cap_h, CAP_SH)
    c.ellipse(cx + sx - 1, top + 13, 26, cap_h - 1.4, CAP)
    c.ellipse(cx + sx - 11, top + 7 + squash * 2, 9, 3.2, CAP_HI)                 # a shine on the left
    for dx, dy, r in ((-16, 9, 3.4), (0, 3, 4.2), (15, 8, 3.6), (-4, 16, 2.6), (22, 15, 2.2), (-24, 16, 2.2), (9, 17, 2.4)):
        c.ellipse(cx + sx + dx, top + dy + squash * 1.5, r, r * 0.8, SPOT)
    c.rect(round(cx + sx - 27), top + 19, round(cx + sx + 27), top + 20, CAP_SH)    # the rim
    if not back:
        ey = top + 31
        eyes = (255, 255, 255, 255) if hurt else EYE
        for side in (-1, 1):                                                       # eyes under heavy brows
            ex = round(cx + side * 6)
            c.rect(ex - 2, ey, ex + 1, ey + 2, eyes)
            c.line(ex - 3 * side - 1, ey - 3 - (1 if side > 0 else 0), ex + 3 * side, ey - 2 + (1 if side < 0 else 0), EYE)
        c.rect(round(cx) - 1, ey + 5, round(cx) + 1, ey + 6, CREAM_SH)             # nose
        if burst:
            c.ellipse(cx, ey + 11, 4, 3, EYE)                                      # an open mouth, blowing
        else:
            c.rect(round(cx) - 4, ey + 11, round(cx) + 4, ey + 11, EYE)
            c.dot(round(cx) - 5, ey + 10, EYE)
            c.dot(round(cx) + 5, ey + 10, EYE)
        c.dot(round(cx) - 8, ey + 7, BLUSH)
        c.dot(round(cx) + 8, ey + 7, BLUSH)
    if burst:
        spores(c, cx, top + 8, 20 + burst * 3, seed=burst, n=14 + burst * 5)
    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.35)
    return img


def mother_die(stage, size=64):
    if stage < 2:
        return draw_mother(squash=0.6 + stage * 0.4, bob=stage * 3, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 3
    c.ellipse(cx, ground - 6, 24, 8, CAP_SH)                    # the cap, plopped down
    c.ellipse(cx - 1, ground - 7, 23, 7, CAP)
    for dx, dy in ((-12, -9), (0, -12), (12, -8), (-3, -5)):
        c.ellipse(cx + dx, ground + dy + 2, 2.6, 2, SPOT)
    c.ellipse(cx, ground - 1, 14, 3, CREAM)                     # a stump of stalk
    spores(c, cx, ground - 18, 22, seed=5 + stage, n=16)
    img = c.img
    if stage == 3:
        px = img.load()
        for y in range(img.height):
            for x in range(img.width):
                r, g, b, a = px[x, y]
                px[x, y] = (r, g, b, a // 2)
    return img


def mother_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_mother(b), draw_mother(b, bob=1, squash=-0.15), draw_mother(b, squash=0.1), draw_mother(b, bob=1)]),
            (f"Walk_{facing}", 5, True, [draw_mother(b, step=2, bob=1), draw_mother(b, squash=0.2), draw_mother(b, step=-2, bob=1), draw_mother(b, squash=0.2)]),
            (f"Attack_{facing}", 8, False, [draw_mother(b, squash=0.7, shake=-2), draw_mother(b, squash=-0.3, bob=-2, burst=1),
                                           draw_mother(b, squash=0.2, shake=2, burst=2), draw_mother(b, burst=3)]),
            (f"Hurt_{facing}", 8, False, [draw_mother(b, squash=0.7, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [mother_die(s) for s in range(4)]))
    return anims


# ---------- Old Moss, the gardener ----------
T = 48


def draw_moss(sway=0, talk=False, size=T):
    """A little old treefolk in a mossy cloak and a leaf hat, with a long beard of hanging moss, and a
    watering can in one hand. Friendly wrinkled eyes."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    s = sway
    ground = size - 3
    # Roots for feet.
    for dx in (-5, 5):
        c.ellipse(cx + dx, ground - 1, 4, 2, BARK_SH)
        c.ellipse(cx + dx, ground - 2, 3, 1.5, BARK)
    # The cloak: a mossy bell shape.
    for y in range(18, ground - 2):
        t = (y - 18) / (ground - 20)
        half = 8 + t * 7
        c.rect(round(cx - half + s * t), y, round(cx + half + s * t), y, MOSS)
        c.dot(round(cx + half + s * t), y, MOSS_SH)
        c.dot(round(cx + half + s * t) - 1, y, MOSS_SH)
        c.dot(round(cx - half + s * t), y, MOSS_HI)
        if y % 5 == 2:
            for x in range(round(cx - half + 2 + s * t), round(cx + half - 2 + s * t), 4):
                c.dot(x + (y % 3), y, MOSS_HI)
    # A woven belt with a pouch.
    c.rect(round(cx - 10 + s), 29, round(cx + 11 + s), 30, BARK_SH)
    c.rect(round(cx + 4 + s), 30, round(cx + 8 + s), 34, BARK)
    # The watering can, held out on the right arm.
    c.line(round(cx + 9 + s), 24, round(cx + 16 + s), 27, MOSS_SH)
    c.rect(round(cx + 14 + s), 25, round(cx + 21 + s), 31, (118, 170, 190, 255))
    c.rect(round(cx + 14 + s), 25, round(cx + 21 + s), 25, (170, 214, 228, 255))
    c.rect(round(cx + 21 + s), 27, round(cx + 24 + s), 27, (118, 170, 190, 255))
    c.rect(round(cx + 24 + s), 25, round(cx + 24 + s), 28, (170, 214, 228, 255))
    c.rect(round(cx + 20 + s), 25, round(cx + 21 + s), 31, (76, 130, 158, 255))
    # The other arm waves when he talks.
    arm_y = 17 if talk else 25
    c.line(round(cx - 9 + s), 22, round(cx - 15 + s), arm_y, MOSS_SH)
    c.ellipse(cx - 16 + s, arm_y, 2.5, 2.5, BARK_HI)
    # His head: a knobbly bark face, a big nose, deep friendly eyes.
    hx = cx + s * 0.6
    c.ellipse(hx, 12, 8, 8, BARK_SH)
    c.ellipse(hx - 0.5, 11.5, 7.2, 7.2, BARK)
    c.ellipse(hx - 3, 8, 2.5, 1.6, BARK_HI)
    c.ellipse(hx, 13, 2.2, 2.6, BARK_HI)                                  # the nose
    for ex in (-3, 3):
        c.rect(round(hx + ex) - 1, 10, round(hx + ex) + 1, 10, EYE)
        c.dot(round(hx + ex), 11, EYE)
        c.line(round(hx + ex) - 2, 8, round(hx + ex) + 2, 8 + (1 if ex > 0 else 0), (210, 224, 200, 255))   # white bushy brows
    # A beard of hanging moss, longer than the face.
    for i, bx in enumerate(range(-6, 7, 2)):
        ln = 7 + (3 - abs(bx) // 2) + (i % 2)
        c.line(round(hx + bx), 15, round(hx + bx + s * 0.4), 15 + ln, MOSS_HI if i % 2 else MOSS)
        c.dot(round(hx + bx + s * 0.4), 15 + ln, MOSS_SH)
    if talk:
        c.rect(round(hx) - 2, 17, round(hx) + 2, 19, EYE)
        c.rect(round(hx) - 1, 18, round(hx) + 1, 19, BLUSH)
    # A round leaf hat with a flower.
    c.ellipse(hx, 5, 11, 3, MOSS_SH)
    c.ellipse(hx, 4, 10, 2.4, MOSS)
    c.ellipse(hx, 1.5, 6, 3.5, MOSS)
    c.ellipse(hx - 2, 0.5, 2.5, 1.2, MOSS_HI)
    c.dot(round(hx + 5), 2, CAP)
    c.dot(round(hx + 6), 2, CAP)
    c.dot(round(hx + 5), 1, STAR)
    return c.img


# ---------- Sleepy trees ----------
TR = 64


def draw_tree(awake=False, sway=0, z=0, blink=False):
    """A broad tree with a face. Asleep: eyes shut, a blue nightcap with a pom-pom, and 'z's drifting up.
    Awake: eyes open, rosy cheeks, a smile, leaves rustling, and the nightcap tossed away."""
    c = Canvas(TR)
    cx = TR / 2 - 0.5
    ground = TR - 3
    # The trunk, flaring at the roots.
    for y in range(26, ground):
        t = (y - 26) / (ground - 26)
        half = 7 + t * 4 + (4 * t * t)
        c.rect(round(cx - half), y, round(cx + half), y, BARK)
        c.rect(round(cx + half) - 3, y, round(cx + half), y, BARK_SH)
        c.dot(round(cx - half), y, BARK_HI)
        c.dot(round(cx - half) + 1, y, BARK_HI)
        if y % 6 == 0:
            c.rect(round(cx - 2 + (y % 5)), y, round(cx + 1 + (y % 5)), y, BARK_SH)
    # The leafy crown: overlapping rounds of green, lit from the top left.
    blobs = [(0, 15, 21, 13), (-15, 20, 13, 9), (15, 21, 13, 9), (-7, 8, 12, 8), (8, 9, 12, 8)]
    for dx, dy, rx, ry in blobs:
        c.ellipse(cx + dx + sway * (1 - dy / 30), dy, rx, ry, MOSS_SH)
    for dx, dy, rx, ry in blobs:
        c.ellipse(cx + dx - 1 + sway * (1 - dy / 30), dy - 1, rx - 1.6, ry - 1.5, MOSS)
    for dx, dy, rx, ry in ((-12, 6, 6, 3), (-3, 2, 5, 2.5), (-20, 17, 4, 2.5)):
        c.ellipse(cx + dx + sway * 0.7, dy, rx, ry, MOSS_HI)
    # The face on the trunk.
    ey = 36
    for side in (-1, 1):
        ex = round(cx + side * 5)
        if awake and not blink:
            c.rect(ex - 2, ey - 1, ex + 1, ey + 2, (250, 252, 240, 255))
            c.rect(ex - 1 + (1 if side > 0 else 0), ey, ex + (1 if side > 0 else 0), ey + 1, EYE)
        else:
            c.line(ex - 2, ey + 1, ex + 1, ey + 1, EYE)                      # a shut eye: a gentle curve
            c.dot(ex - 3, ey, EYE)
            c.dot(ex + 2, ey, EYE)
    c.rect(round(cx) - 1, ey + 4, round(cx) + 1, ey + 5, BARK_SH)           # nose
    if awake:
        c.line(round(cx) - 4, ey + 9, round(cx) + 4, ey + 9, EYE)           # a smile
        c.dot(round(cx) - 5, ey + 8, EYE)
        c.dot(round(cx) + 5, ey + 8, EYE)
        for side in (-1, 1):
            c.rect(round(cx + side * 9) - 1, ey + 5, round(cx + side * 9) + 1, ey + 6, BLUSH)
        for i, (dx, dy) in enumerate(((-24, 2), (22, 10), (-6, -2), (26, 22))):   # sparkles of leaf-dust
            if (i + sway) % 2 == 0:
                c.dot(round(cx + dx), dy + 8, STAR_HI)
                c.dot(round(cx + dx), dy + 7, STAR)
                c.dot(round(cx + dx), dy + 9, STAR)
    else:
        c.rect(round(cx) - 2, ey + 9, round(cx) + 2, ey + 9, EYE)           # a sleepy mouth
        # The nightcap: slumped over one side, with a pom-pom.
        c.ellipse(cx + 2, 4, 11, 4.5, NIGHT_SH)
        c.ellipse(cx + 1, 3.5, 10, 3.8, NIGHT)
        c.line(round(cx + 8), 4, round(cx + 20), 10, NIGHT)
        c.line(round(cx + 8), 5, round(cx + 20), 11, NIGHT_SH)
        c.ellipse(cx + 21, 11, 2.6, 2.6, (250, 245, 235, 255))
        for k in range(z + 1):                                                # Z z Z drifting up
            zx, zy = round(cx + 22 + k * 5), 24 - k * 8 - z
            for ox, oy in ((0, 0), (1, 0), (2, 0), (1, 1), (0, 2), (1, 2), (2, 2)):
                if 0 <= zy + oy < TR:
                    c.dot(zx + ox, zy + oy, (240, 244, 255, 255))
    return c.img


# ---------- The clay pot ----------

CLAY, CLAY_HI, CLAY_SH = (200, 118, 82, 255), (232, 156, 112, 255), (150, 78, 66, 255)
CLAY_BAND = (240, 214, 160, 255)


def draw_pot():
    c = Canvas()
    cx = 15.5
    ground = 29
    for y in range(12, ground + 1):
        t = (y - 12) / (ground - 12)
        half = 4 + math.sin(t * math.pi * 0.9) * 6 - t * 1.0
        c.rect(round(cx - half), y, round(cx + half), y, CLAY)
        c.rect(round(cx + half) - 2, y, round(cx + half), y, CLAY_SH)
        c.dot(round(cx - half) + 1, y, CLAY_HI)
    c.rect(9, 11, 22, 13, CLAY_HI)                      # the rim
    c.rect(9, 13, 22, 13, CLAY_SH)
    c.rect(11, 14, 20, 14, CLAY_SH)                     # shadow under the rim
    c.rect(8, 19, 23, 20, CLAY_BAND)                    # a cream band with little diamonds
    for x in range(10, 23, 4):
        c.dot(x, 19, CLAY_SH)
    c.rect(8, 20, 23, 20, (214, 184, 130, 255))
    c.rect(11, 28, 20, 29, CLAY_SH)                     # the base
    return c.img


def pot_shards(frame):
    """The pot bursting: pieces of clay flying out and a puff of dust, then settling."""
    c = Canvas()
    cx, cy = 15.5, 18
    pieces = [(-1, -1, 2, 3), (1, -1, 2, 2), (-1.4, 0.2, 3, 2), (1.4, 0.1, 2, 3), (0, -1.4, 2, 2), (-0.5, 0.8, 3, 2), (0.6, 0.9, 2, 2)]
    for i, (dx, dy, w, h) in enumerate(pieces):
        t = frame / 3
        x = cx + dx * (4 + frame * 5)
        y = cy + dy * (3 + frame * 3) + t * t * 14
        col = (CLAY, CLAY_HI, CLAY_SH)[i % 3]
        c.rect(round(x), round(y), round(x) + w, round(y) + h, col)
        c.rect(round(x), round(y) + h, round(x) + w, round(y) + h, CLAY_SH)
    if frame < 3:                                       # a puff of dust
        r = 4 + frame * 3
        c.ellipse(cx, 22, r, r * 0.6, (236, 220, 196, 160 - frame * 40))
    return c.img


# ---------- Woods props ----------
P = 48


def draw_giant_mushroom():
    c = Canvas(P)
    cx = P / 2 - 0.5
    ground = P - 3
    c.rect(round(cx) - 5, 24, round(cx) + 5, ground, CREAM)
    c.rect(round(cx) + 3, 24, round(cx) + 5, ground, CREAM_SH)
    c.ellipse(cx, ground, 9, 3, CREAM_SH)
    c.ellipse(cx, 22, 19, 4, CREAM_SH)
    c.ellipse(cx, 15, 21, 14, CAP_SH)
    c.ellipse(cx - 1, 14, 20, 13, CAP)
    c.ellipse(cx - 8, 8, 7, 2.6, CAP_HI)
    for dx, dy, r in ((-11, 11, 3.2), (3, 6, 3.8), (11, 14, 3), (-2, 17, 2.4), (-17, 17, 2)):
        c.ellipse(cx + dx, dy, r, r * 0.8, SPOT)
    return c.img


def draw_glowcaps(frame=0):
    """A cluster of blue mushrooms that glow in the dark."""
    c = Canvas(P)
    ground = P - 4
    for x, h, r in ((15, 14, 6), (25, 20, 8), (34, 12, 5), (21, 9, 4)):
        c.rect(x - 1, ground - h, x + 1, ground, (214, 232, 240, 255))
        c.ellipse(x, ground - h, r, r * 0.6, (70, 150, 200, 255))
        c.ellipse(x - 0.5, ground - h - 0.5, r - 1, r * 0.6 - 1, GLOW_BLUE)
        c.ellipse(x - r * 0.35, ground - h - r * 0.25, r * 0.3, r * 0.15, GLOW_HI)
        c.dot(x + r // 2, ground - h, GLOW_HI)
    for i, (x, y) in enumerate(((10, 14), (38, 10), (26, 6), (18, 4))):    # floating glow-motes
        if (i + frame) % 2 == 0:
            c.dot(x, y, GLOW_HI)
            c.dot(x, y + 1, GLOW_BLUE)
    return c.img


# ---------- Items ----------
# The shapes use the same floor/icon pattern as make_item_sprites.py: draw(big) -> a small picture.

def frog_hat(big):
    """A green frog-head hat: two big round eyes on top, a wide grin across the brim."""
    n = 20 if big else 12
    c = Canvas(n)
    G, GH, GS = (112, 190, 90, 255), (170, 226, 124, 255), (62, 134, 74, 255)
    if big:
        c.ellipse(9.5, 12, 8.5, 5.5, GS)
        c.ellipse(9.5, 11.5, 8, 5, G)
        c.ellipse(5, 6, 3.6, 3.6, GS)
        c.ellipse(14, 6, 3.6, 3.6, GS)
        c.ellipse(5, 6, 3, 3, GH)
        c.ellipse(14, 6, 3, 3, GH)
        c.rect(5, 5, 6, 7, EYE)
        c.rect(14, 5, 15, 7, EYE)
        c.dot(5, 5, (255, 255, 255, 255))
        c.dot(14, 5, (255, 255, 255, 255))
        c.line(3, 13, 16, 13, EYE)                                  # the grin
        c.dot(2, 12, EYE)
        c.dot(17, 12, EYE)
        c.rect(5, 15, 14, 16, GS)                                   # the brim
        c.dot(8, 10, BLUSH)
        c.dot(11, 10, BLUSH)
    else:
        c.ellipse(5.5, 7, 5, 3.4, GS)
        c.ellipse(5.5, 6.5, 4.5, 3, G)
        c.rect(2, 2, 4, 4, GH)
        c.rect(7, 2, 9, 4, GH)
        c.dot(3, 3, EYE)
        c.dot(8, 3, EYE)
        c.line(2, 8, 9, 8, EYE)
    return c.img


def healing_apple(big):
    n = 18 if big else 11
    c = Canvas(n)
    RED, RED_HI, RED_SH = (222, 60, 70, 255), (250, 130, 130, 255), (160, 34, 60, 255)
    if big:
        c.ellipse(6.5, 10, 5, 6, RED_SH)
        c.ellipse(11, 10, 5, 6, RED_SH)
        c.ellipse(6, 9.5, 4.6, 5.6, RED)
        c.ellipse(10.5, 9.5, 4.6, 5.6, RED)
        c.ellipse(5, 7, 1.6, 2.4, RED_HI)
        c.rect(8, 2, 9, 5, BARK)
        c.ellipse(12, 3, 3.4, 2, MOSS)
        c.dot(10, 3, MOSS_HI)
        c.dot(11, 2, MOSS_HI)
    else:
        c.ellipse(5.5, 6.5, 4.6, 4, RED_SH)
        c.ellipse(5, 6, 4, 3.6, RED)
        c.dot(3, 4, RED_HI)
        c.rect(5, 1, 5, 2, BARK)
        c.rect(6, 1, 8, 1, MOSS)
    return c.img


def mana_berry(big):
    n = 18 if big else 11
    c = Canvas(n)
    B, BH, BS = (90, 120, 230, 255), (170, 196, 255, 255), (54, 66, 170, 255)
    if big:
        for x, y in ((5, 8), (11, 8), (8, 12), (4, 13), (12, 13)):
            c.ellipse(x, y, 3.4, 3.4, BS)
            c.ellipse(x - 0.4, y - 0.4, 3, 3, B)
            c.dot(x - 1, y - 1, BH)
        c.rect(8, 2, 8, 6, MOSS_SH)
        c.ellipse(11, 3, 3, 1.6, MOSS)
        c.ellipse(5, 3, 2.4, 1.4, MOSS_HI)
    else:
        for x, y in ((3, 5), (7, 5), (5, 8)):
            c.ellipse(x, y, 2.2, 2.2, BS)
            c.ellipse(x - 0.3, y - 0.3, 1.8, 1.8, B)
            c.dot(x - 1, y - 1, BH)
        c.rect(5, 1, 6, 2, MOSS)
    return c.img


def fairy_lantern(big):
    """A little glass lantern with a gold cap and a ring, and a tiny fairy light glowing inside."""
    n = 20 if big else 12
    c = Canvas(n)
    GOLDC, GOLD_H, GOLD_S = pal.rgba(pal.HONEY), pal.rgba(pal.HONEY_LIGHT), pal.rgba(pal.HONEY_SHADE)
    GLASS = (200, 236, 246, 255)
    if big:
        c.ellipse(9.5, 3, 3.2, 2.4, GOLD_S)                      # the carrying ring
        c.ellipse(9.5, 3, 2, 1.2, CLEAR)
        c.rect(5, 5, 14, 6, GOLDC)                               # the cap
        c.rect(5, 5, 14, 5, GOLD_H)
        c.rect(6, 7, 13, 15, GLASS)                              # the glass
        c.rect(6, 7, 6, 15, (232, 250, 255, 255))
        c.rect(13, 7, 13, 15, (150, 200, 220, 255))
        c.ellipse(9.5, 11.5, 2.4, 2.8, STAR)                     # the fairy light
        c.ellipse(9.5, 11.5, 1.2, 1.6, STAR_HI)
        c.dot(8, 9, STAR_HI)
        c.dot(12, 13, STAR_HI)
        c.rect(5, 16, 14, 17, GOLD_S)                            # the base
        c.rect(5, 16, 14, 16, GOLDC)
    else:
        c.rect(3, 2, 8, 2, GOLDC)
        c.rect(3, 3, 8, 8, GLASS)
        c.rect(4, 4, 7, 7, STAR)
        c.dot(5, 5, STAR_HI)
        c.rect(3, 9, 8, 9, GOLD_S)
        c.dot(5, 0, GOLD_S)
        c.dot(6, 0, GOLD_S)
    return c.img


def clover_charm(big):
    """A four-leaf clover on a gold ring: a lucky charm."""
    n = 18 if big else 11
    c = Canvas(n)
    GOLDC = pal.rgba(pal.HONEY)
    if big:
        for dx, dy in ((-3, -3), (3, -3), (-3, 3), (3, 3)):
            c.ellipse(9 + dx, 9 + dy, 3.4, 3.4, MOSS_SH)
            c.ellipse(9 + dx - 0.4, 9 + dy - 0.4, 3, 3, MOSS)
            c.dot(9 + dx - 1, 9 + dy - 1, MOSS_HI)
        c.rect(9, 9, 10, 15, MOSS_SH)
        c.ellipse(9, 3, 2.6, 2.6, GOLDC)
        c.ellipse(9, 3, 1.4, 1.4, CLEAR)
    else:
        for dx, dy in ((-2, -2), (2, -2), (-2, 2), (2, 2)):
            c.ellipse(5.5 + dx, 5.5 + dy, 2.2, 2.2, MOSS)
        c.dot(5, 5, MOSS_HI)
        c.rect(5, 6, 5, 9, MOSS_SH)
    return c.img


# ---------- Pictures for the quest log ----------

def icon_frog():
    c = Canvas(24)
    G, GH, GS = (112, 190, 90, 255), (170, 226, 124, 255), (62, 134, 74, 255)
    c.ellipse(11.5, 15, 8.5, 6, GS)
    c.ellipse(11.5, 14.5, 8, 5.5, G)
    for x in (6, 17):
        c.ellipse(x, 8, 3.8, 3.8, GS)
        c.ellipse(x, 8, 3.2, 3.2, GH)
        c.rect(x - 1, 7, x, 9, EYE)
    c.line(5, 16, 18, 16, EYE)
    c.dot(4, 15, EYE)
    c.dot(19, 15, EYE)
    c.rect(8, 3, 15, 4, STAR)                                              # a tiny crown
    for x in (8, 11, 14):
        c.dot(x, 2, STAR)
    return outline(c.img)


def icon_tree():
    return outline(draw_tree(awake=True).crop((8, 2, 56, 62)).resize((24, 24), Image.NEAREST))


def icon_monster():
    c = Canvas(24)
    c.ellipse(11.5, 13, 8, 8, (196, 86, 30, 255))
    c.ellipse(11, 12.5, 7.4, 7.4, (232, 120, 36, 255))
    for ex in (7, 15):
        c.rect(ex - 1, 10, ex + 1, 12, EYE)
        c.dot(ex, 11, (255, 224, 96, 255))
    c.rect(7, 16, 16, 17, EYE)
    for x in (8, 11, 14):
        c.dot(x, 16, (232, 120, 36, 255))
    return outline(c.img)


def portrait(bust, background):
    img = Image.new("RGBA", (32, 32))
    px = img.load()
    r, g, b = background
    for y in range(32):
        for x in range(32):
            px[x, y] = (r + y, g + y // 2, b + y, 255)
    img.alpha_composite(outline(bust))
    return img


# ---------- Writing everything ----------

WOODS_ITEMS = {
    "FrogHat": frog_hat,
    "HealingApple": healing_apple,
    "ManaBerry": mana_berry,
    "FairyLantern": fairy_lantern,
    "CloverCharm": clover_charm,
}

if __name__ == "__main__":
    write_sheet("SporePuff", "Attack", puff_animations())
    write_sheet("MotherMushroom", "Attack", mother_animations(), frame_size=64)
    write_sheet("OldMoss", None, [("Idle", 2, True, [draw_moss(0), draw_moss(1)]),
                                  ("Talk", 6, True, [draw_moss(0, talk=True), draw_moss(1)])], frame_size=T)
    portrait(draw_moss().crop((8, 0, 40, 32)), (64, 96, 70)).save(UI / "PortraitOldMoss.png")
    write_sheet("SleepyTree", None, [
        ("Sleep", 1.5, True, [draw_tree(awake=False, z=k) for k in range(3)]),
        ("Awake", 3, True, [draw_tree(awake=True, sway=s, blink=(s == 2)) for s in (0, 1, 2, 1)]),
    ], frame_size=TR)
    write_sheet("Pot", None, [("Idle", 1, False, [draw_pot()])])
    write_sheet("PotShards", None, [("Break", 12, False, [pot_shards(i) for i in range(4)])], pivot="center", outline_color=None)
    write_sheet("WoodsProps", None, [
        ("GiantMushroom", 1, False, [draw_giant_mushroom()]),
        ("Glowcaps", 2, True, [draw_glowcaps(0), draw_glowcaps(1)], None),
    ], frame_size=P)
    write_sheet("WoodsItems", None, [(name, 3, True, [floor_frame(draw, glint=i == 1) for i in range(2)])
                                     for name, draw in WOODS_ITEMS.items()])
    UI.mkdir(parents=True, exist_ok=True)
    for name, draw in WOODS_ITEMS.items():
        icon(draw).save(UI / f"Icon{name}.png")
    icon_frog().save(UI / "IconFrog.png")
    icon_tree().save(UI / "IconTree.png")
    icon_monster().save(UI / "IconMonster.png")
    print("Wrote the Woods' icons and portrait")
