"""Generates the art for the metroidvania gates (Phase 2): brambles, braziers, hint bubbles, the pit
under a gap, and the first two key items, the Bouncy Boots and the Amethyst.

Run:  Tools/.venv/bin/python Tools/make_gate_sprites.py
Out:  Assets/Art/GateProps.png / .json      48x48 frames, bottom pivot:
        Bramble         1 frame     a wall of thorny vines with pink rosebuds: burn it or make it bloom
        Burn            4 frames    the bramble catching fire and crumbling to ash (plays once)
        Bloom           4 frames    the bramble opening into flowers, then parting (plays once)
        BrazierUnlit    1 frame     a stone basin of cold ash
        BrazierFire     2 frames    the same, lit: a dancing orange flame
        BrazierWater    2 frames    the same, filled with sparkling water
      Assets/Art/HintBubbles.png / .json    32x32 frames, center pivot: a thought bubble with a picture and a "?"
        Boots, Lantern  what you need to come back with
      Assets/Art/GateItems.png / .json      32x32 floor frames, and Assets/Art/UI/Icon<Item>.png 24x24 icons:
        BouncyBoots, Amethyst, DragonEgg (one of Amethyra's five lost eggs)
      Assets/Art/Collectibles.png / .json   32x32, bottom pivot, 2-frame glints: HeartPiece (a cracked heart: four make a heart),
        StarShard (a golden star: collected for the wardrobe at home)
      Assets/Art/EdgeArrow.png / .json      32x32, center pivot, 2-frame pulse: the arrow at a room's edge (RoomEdge)
      Assets/Art/Environment/Pit.png        32x32 the dark floor of a gap

Same conventions as make_woods_sprites.py.
"""
import math
import random

from PIL import Image

import palette as pal
from make_item_sprites import floor_frame, icon, place
from make_woods_sprites import fairy_lantern
from sprite_common import ART, CLEAR, Canvas, outline, write_sheet

UI = ART / "UI"
ENV = ART / "Environment"
S = 48

# ---------- Palette ----------
VINE, VINE_HI, VINE_SH = (74, 98, 78, 255), (112, 140, 98, 255), (46, 66, 62, 255)
THORN = (206, 196, 176, 255)
BUD, BUD_HI = (236, 110, 150, 255), (255, 182, 200, 255)
LEAF = (96, 150, 92, 255)
ASH, ASH_SH = (92, 84, 92, 255), (58, 52, 64, 255)
STONE, STONE_HI, STONE_SH = pal.rgba(pal.STONE_LIGHT), (232, 228, 240, 255), pal.rgba(pal.STONE_SHADE)
FLAME_O, FLAME_Y, FLAME_R = (255, 150, 52, 255), (255, 226, 110, 255), (226, 80, 40, 255)
WATER, WATER_HI, WATER_SH = (96, 188, 236, 255), (200, 240, 255, 255), (52, 126, 196, 255)
JELLY, JELLY_HI, JELLY_SH = (110, 214, 150, 255), (200, 248, 206, 255), (52, 158, 120, 255)
CUFF = pal.rgba(pal.BUTTER)
SOLE, SOLE_SH = pal.rgba(pal.HONEY), pal.rgba(pal.HONEY_SHADE)
AM, AM_HI, AM_SH, AM_DEEP = (176, 110, 220, 255), (222, 176, 250, 255), (122, 66, 176, 255), (84, 40, 136, 255)
GLINT = (255, 255, 235, 255)
FIRE_GLOW = (255, 210, 120, 255)


# ---------- Brambles ----------

def vines(c, seed, spread=1.0, charred=0.0, scorched_rows=0):
    """A tangle of curved vines filling a tile-wide wall, with thorns. `charred` darkens it."""
    rng = random.Random(seed)
    base, hi, sh = VINE, VINE_HI, VINE_SH
    if charred:
        mix = lambda a, b: tuple(round(a[i] + (b[i] - a[i]) * charred) for i in range(3)) + (255,)
        base, hi, sh = mix(base, ASH), mix(hi, ASH), mix(sh, ASH_SH)
    # A dense low mound: lots of short arcs.
    for _ in range(int(60 * spread)):
        x0 = rng.randint(6, S - 7)
        y0 = rng.randint(20, S - 5)
        length = rng.randint(8, 18)
        bend = rng.uniform(-0.9, 0.9)
        angle = rng.uniform(-3.0, -0.15)
        px_, py_ = x0, y0
        for k in range(length):
            angle += bend * 0.18
            px_ += math.cos(angle) * 1.2
            py_ += math.sin(angle) * 1.1
            x, y = round(px_), round(py_)
            if not (4 <= x < S - 4 and 8 <= y < S - 2):
                break
            if c.px[x, y][3] and k < 2:
                continue
            c.dot(x, y, hi if k % 5 == 1 else base)
            c.dot(x + 1, y, sh)
            if k % 4 == 2 and not charred:
                c.dot(x + 1, y - 2, THORN)
                c.dot(x + 1, y - 1, THORN)
                c.dot(x - 1, y + 1, THORN)
    return rng


def bramble(frame=0, charred=0.0, shrink=0.0, buds=True, flowers=0.0, leaf=0.0):
    c = Canvas(S)
    # A mound that fills the tile: a dark underlay, then vines.
    ground = S - 3
    UNDER = (34, 52, 52, 255)
    c.ellipse(S / 2 - 0.5, ground - 10, 21, 12, UNDER)
    c.rect(4, ground - 10, S - 5, ground, UNDER)
    rng = vines(c, 7 + frame, charred=charred)
    c.rect(5, ground - 2, S - 6, ground, VINE_SH)
    # Pink rosebuds, or (when blooming) big open blossoms.
    spots = [(12, 22), (22, 16), (33, 24), (17, 32), (29, 33), (38, 18), (9, 31)]
    if buds and not charred:
        for i, (x, y) in enumerate(spots):
            if flowers > 0:
                r = 2.0 + 2.0 * flowers
                for a in range(6):
                    ang = a * math.pi / 3
                    c.ellipse(x + math.cos(ang) * r * 0.8, y + math.sin(ang) * r * 0.6, 1.4 + flowers * 0.9, 1.3 + flowers * 0.7, BUD_HI if a % 2 else (255, 238, 244, 255))
                c.ellipse(x, y, 1.6, 1.4, (255, 214, 96, 255))
            else:
                c.ellipse(x, y, 2.3, 2.3, BUD)
                c.dot(x - 1, y - 1, BUD_HI)
                c.dot(x, y + 2, LEAF)
    if leaf:
        for x, y in ((10, 26), (20, 20), (30, 28), (38, 22), (16, 34), (26, 14)):
            c.ellipse(x, y, 2.5, 1.5, LEAF)
    return c.img


def burn_frame(stage):
    """0..3: flames licking over the vines, then the bramble charred and shrunk to a heap of ash."""
    c = Canvas(S)
    if stage < 3:
        c.img.alpha_composite(bramble(charred=0.25 * stage, buds=stage == 0))
        c.px = c.img.load()
        rng = random.Random(30 + stage)
        count = (14, 22, 12)[stage]
        for _ in range(count):
            x = rng.randint(8, S - 9)
            y = rng.randint(14, S - 6)
            h = rng.randint(5, 10) - stage
            c.ellipse(x, y - h / 2, 2.8, h / 1.6, FLAME_R)
            c.ellipse(x, y - h / 2 + 0.8, 2.0, h / 2.1, FLAME_O)
            c.ellipse(x, y - h / 2 + 1.5, 1.2, h / 3.2, FLAME_Y)
        return c.img
    # Ash heap.
    ground = S - 4
    c.ellipse(S / 2 - 0.5, ground - 2, 14, 4, ASH_SH)
    c.ellipse(S / 2 - 0.5, ground - 3, 12, 3, ASH)
    rng = random.Random(33)
    for _ in range(14):
        c.dot(rng.randint(14, 34), rng.randint(ground - 6, ground - 1), ASH_SH if rng.random() < 0.5 else (140, 132, 140, 255))
    for x, y in ((18, ground - 8), (30, ground - 9), (24, ground - 12)):   # a few last embers
        c.dot(x, y, FLAME_O)
    return c.img


def bloom_frame(stage):
    """0..3: the thorns soften, blossoms open wide, then the wall parts and sinks into a flower bed."""
    if stage < 3:
        return bramble(frame=stage, flowers=0.35 + stage * 0.35, leaf=1.0 if stage else 0.0)
    c = Canvas(S)
    ground = S - 4
    rng = random.Random(40)
    c.ellipse(S / 2 - 0.5, ground - 2, 16, 4, LEAF)
    for _ in range(9):
        x = rng.randint(10, 38)
        y = rng.randint(ground - 6, ground - 1)
        for a in range(5):
            ang = a * math.tau / 5
            c.dot(round(x + math.cos(ang) * 1.6), round(y + math.sin(ang) * 1.2), BUD_HI if a % 2 else (255, 238, 244, 255))
        c.dot(x, y, (255, 214, 96, 255))
    return c.img


# ---------- Braziers ----------

def basin(c, ground=S - 4, lit=0):
    """A stone pedestal with a wide basin on top, 3/4 view."""
    cx = S / 2 - 0.5
    c.ellipse(cx, ground - 1, 10, 3, STONE_SH)                              # the base slab
    c.rect(round(cx) - 4, ground - 14, round(cx) + 4, ground - 1, STONE)    # the stem
    c.rect(round(cx) - 4, ground - 14, round(cx) - 3, ground - 1, STONE_HI)
    c.rect(round(cx) + 3, ground - 14, round(cx) + 4, ground - 1, STONE_SH)
    c.ellipse(cx, ground - 15, 13, 5, STONE_SH)                             # the bowl: rim and inside
    c.ellipse(cx, ground - 16, 12, 4.4, STONE)
    c.ellipse(cx, ground - 16, 10, 3.4, (58, 52, 64, 255))
    return cx, ground - 16


def brazier(state, frame=0):
    c = Canvas(S)
    cx, by = basin(c)
    if state == "unlit":
        c.ellipse(cx, by, 9, 2.8, ASH_SH)
        for x, y in ((-4, 0), (2, -1), (5, 1), (-1, 1)):
            c.dot(round(cx + x), round(by + y), (120, 112, 120, 255))
        c.rect(round(cx) - 5, by - 1, round(cx) - 2, by - 1, (92, 70, 58, 255))      # a charred log
        return c.img
    if state == "fire":
        c.ellipse(cx, by + 0.5, 9, 2.8, (170, 60, 38, 255))
        sway = (0, 1, 0, -1)[frame % 4]
        for dx, h, col, w in ((-4, 9, FLAME_R, 3.2), (3, 11, FLAME_R, 3.4), (0, 15, FLAME_R, 4.2)):
            c.ellipse(cx + dx + sway * (h > 10), by - h / 2, w, h / 2, col)
        for dx, h, w in ((-3, 6, 2.2), (3, 8, 2.4), (0, 11, 3.0)):
            c.ellipse(cx + dx + sway * (h > 8), by - h / 2 + 0.5, w, h / 2 - 1, FLAME_O)
        c.ellipse(cx + sway * 0.5, by - 3, 1.6, 3.2, FLAME_Y)
        return c.img
    # water: the basin brims with sparkling water, and a few droplets leap
    c.ellipse(cx, by - 0.5, 10, 3.4, WATER_SH)
    c.ellipse(cx, by - 1, 9, 2.9, WATER)
    c.rect(round(cx) - 5, by - 2, round(cx) - 2, by - 2, WATER_HI)
    for i, (dx, dy) in enumerate(((-5, -6), (0, -9), (5, -5), (-2, -12))):
        if (i + frame) % 2 == 0:
            c.dot(round(cx + dx), round(by + dy), WATER_HI)
            c.dot(round(cx + dx), round(by + dy) + 1, WATER)
    c.rect(round(cx) - 1, by - 8, round(cx), by - 3, WATER_HI)           # a little fountain spout
    c.dot(round(cx) - 1, by - 9, WATER_HI)
    return c.img


# ---------- Hint bubbles ----------

def bubble(draw_picture, question_color=(210, 84, 120, 255)):
    """A white thought bubble (32x32) with a picture in it and a red-pink "?" beside it."""
    c = Canvas(32)
    W, EDGE = (255, 252, 244, 255), (110, 84, 130, 255)
    c.ellipse(15.5, 13, 14.5, 11.5, EDGE)
    c.ellipse(15.5, 13, 13.5, 10.5, W)
    c.ellipse(11, 25, 2.4, 2.4, EDGE)                                # the little thought-trail
    c.ellipse(11, 25, 1.5, 1.5, W)
    c.ellipse(8, 29, 1.6, 1.6, EDGE)
    c.ellipse(8, 29, 0.9, 0.9, W)
    pic = draw_picture()
    c.img.alpha_composite(pic, (4, 13 - pic.size[1] // 2 + 1))
    c.px = c.img.load()
    # The "?" on the right, 5x9 pixels with a plum shadow.
    mark = ["..XXX.", ".X...X", ".....X", "....X.", "...X..", "...X..", "......", "...X.."]
    for dy, row in enumerate(mark):
        for dx, ch in enumerate(row):
            if ch == "X":
                c.dot(21 + dx + 1, 5 + dy + 1, EDGE)
    for dy, row in enumerate(mark):
        for dx, ch in enumerate(row):
            if ch == "X":
                c.dot(21 + dx, 5 + dy, question_color)
                c.dot(21 + dx + 1, 5 + dy, question_color)
    return c.img


def boots_small():
    return bouncy_boots(False)


def lantern_small():
    return fairy_lantern(False)


# ---------- Items ----------

def bouncy_boots(big):
    """A jelly-green boot in side view, with a butter-yellow cuff and a coiled spring under the sole."""
    n = 20 if big else 14
    c = Canvas(n)
    if big:
        c.rect(3, 1, 9, 9, JELLY)                              # the shaft
        c.rect(3, 1, 4, 9, JELLY_HI)
        c.rect(8, 1, 9, 9, JELLY_SH)
        c.rect(2, 1, 10, 3, CUFF)                              # the cuff
        c.rect(2, 3, 10, 3, (222, 196, 120, 255))
        c.rect(3, 10, 15, 12, JELLY)                           # the foot, reaching to the toe
        c.ellipse(15, 11, 2.6, 2.4, JELLY)
        c.rect(3, 10, 14, 10, JELLY_HI)
        c.rect(8, 12, 15, 12, JELLY_SH)
        c.rect(2, 13, 17, 14, SOLE)                            # the sole
        c.rect(2, 14, 17, 14, SOLE_SH)
        for k, x in enumerate(range(3, 16, 3)):                # the spring: a zig-zag coil
            c.line(x, 15 + (k % 2) * 3, x + 3, 15 + ((k + 1) % 2) * 3, SOLE_SH)
            c.line(x, 16 + (k % 2) * 3, x + 3, 16 + ((k + 1) % 2) * 3, SOLE)
        c.rect(2, 19, 17, 19, SOLE_SH)
    else:
        c.rect(2, 0, 6, 6, JELLY)
        c.rect(2, 0, 2, 6, JELLY_HI)
        c.rect(1, 0, 7, 1, CUFF)
        c.rect(2, 7, 11, 8, JELLY)
        c.ellipse(10.5, 7.5, 1.8, 1.6, JELLY)
        c.rect(1, 9, 12, 9, SOLE)
        c.line(2, 10, 3, 12, SOLE_SH)
        c.line(3, 12, 5, 10, SOLE_SH)
        c.line(5, 10, 7, 12, SOLE_SH)
        c.line(7, 12, 9, 10, SOLE_SH)
        c.line(9, 10, 11, 12, SOLE_SH)
    return c.img


def amethyst(big):
    """A faceted purple gem, lit from the top left, with a few glints."""
    n = 18 if big else 12
    c = Canvas(n)
    for y in range(n):
        v = (y + 0.5) / n
        if v < 0.1:
            continue
        half = 0.5 + 0.5 * (v - 0.1) / 0.28 if v < 0.38 else 0.98 * (1 - (v - 0.38) / 0.62)
        for x in range(n):
            u = ((x + 0.5) / n) * 2 - 1
            if abs(u) <= half:
                if v < 0.38:                       # the crown: a light table, darker sides
                    col = AM_HI if abs(u) < 0.45 and v < 0.22 else AM if u < 0.35 else AM_SH
                else:                              # the pavilion: facets meeting at the point
                    col = AM if u < -0.12 else AM_SH if u < 0.4 else AM_DEEP
                    if abs(u) < 0.08:
                        col = AM_HI
                c.dot(x, y, col)
    c.dot(round(n * 0.3), round(n * 0.18), GLINT)
    if big:
        c.dot(round(n * 0.3), round(n * 0.18) + 1, GLINT)
        c.dot(round(n * 0.72), round(n * 0.62), AM_HI)
    return c.img


def dragon_egg(big):
    """One of Amethyra's lost eggs: an amethyst-purple egg with cream spots and a gold glint."""
    n = 20 if big else 13
    c = Canvas(n)
    cx = (n - 1) / 2
    ry, rx = (9.2, 6.8) if big else (6.0, 4.4)
    cy = ry + 0.5
    for y in range(n):
        for x in range(n):
            v = (y - cy) / ry
            u = (x - cx) / (rx * (1.0 - 0.18 * v) )     # narrower towards the top, like an egg
            if u * u + v * v <= 1:
                col = AM_SH if u > 0.35 or v > 0.7 else AM
                if u < -0.35 and v < -0.1:
                    col = AM_HI
                c.dot(x, y, col)
    spots = ((-2, 1, 1.4), (2, -2, 1.2), (1, 4, 1.4), (-2, 5, 1.0)) if big else ((-1, 1, 1.0), (2, -1, 0.9), (0, 4, 1.0))
    for dx, dy, r in spots:
        c.ellipse(cx + dx, cy + dy, r, r, (250, 236, 214, 255))
    c.dot(round(cx) - 2, round(cy) - 4 if big else round(cy) - 3, GLINT)
    return c.img


ITEMS = {"BouncyBoots": bouncy_boots, "Amethyst": amethyst, "DragonEgg": dragon_egg}


# ---------- Collectibles ----------
HEART, HEART_HI, HEART_SH = (232, 66, 88, 255), (255, 150, 164, 255), (170, 36, 70, 255)


def heart_piece(glint=False):
    """A red heart with a jagged chunk missing from its right side: a piece of something whole."""
    n = 20
    c = Canvas(n)
    cx, cy = 9.5, 9.0
    for y in range(n):
        for x in range(n):
            u, v = (x - cx) / 7.4, -(y - cy) / 7.4
            if (u * u + v * v - 1) ** 3 - u * u * v ** 3 <= 0:
                jag = 3.2 + (1.4 if y % 4 < 2 else 0)               # the broken edge zig-zags
                if x - cx > jag and y > 5:
                    continue
                col = HEART_SH if x - cx > 2.5 or y > 14 else HEART
                if x - cx < -3 and y < 8:
                    col = HEART_HI
                c.dot(x, y + 1, col)
    c.dot(4, 5, (255, 236, 240, 255))
    c.dot(5, 5, (255, 236, 240, 255))
    out = Image.new("RGBA", (32, 32), CLEAR)
    out.alpha_composite(outline(c.img), (6, 6))
    if glint:
        px = out.load()
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0), (0, 0)):
            px[22 + dx, 8 + dy] = GLINT
    return out


def star_shard(glint=False):
    """A gold five-pointed star with a pale core."""
    from PIL import ImageDraw
    img = Image.new("RGBA", (20, 20), CLEAR)
    d = ImageDraw.Draw(img)

    def star(cx, cy, r_out, r_in, color):
        pts = []
        for i in range(10):
            ang = -math.pi / 2 + i * math.pi / 5
            r = r_out if i % 2 == 0 else r_in
            pts.append((cx + math.cos(ang) * r, cy + math.sin(ang) * r))
        d.polygon(pts, fill=color)

    star(10, 10.5, 9, 4, pal.rgba(pal.HONEY))
    star(9.5, 10, 7, 3, pal.rgba(pal.BUTTER))
    star(9.5, 10, 3.5, 1.8, pal.rgba(pal.BUTTER_LIGHT))
    out = Image.new("RGBA", (32, 32), CLEAR)
    out.alpha_composite(outline(img), (6, 6))
    if glint:
        px = out.load()
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0), (0, 0)):
            px[23 + dx, 7 + dy] = GLINT
    return out


# ---------- The arrow at a room's edge ----------

def edge_arrow(bright):
    """Two stacked chevrons pointing up (the way out of the room), soft gold, pulsing."""
    from PIL import ImageDraw
    img = Image.new("RGBA", (32, 32), CLEAR)
    d = ImageDraw.Draw(img)
    a = 235 if bright else 150
    col = (255, 236, 150, a)
    edge = (255, 255, 255, a)
    for dy in (0, 9):
        pts = [(16, 4 + dy), (27, 15 + dy), (22, 18 + dy), (16, 11 + dy), (10, 18 + dy), (5, 15 + dy)]
        d.polygon(pts, fill=col)
        d.line([(16, 4 + dy), (27, 15 + dy)], fill=edge)
        d.line([(16, 4 + dy), (5, 15 + dy)], fill=edge)
    return img


# ---------- The pit under a gap ----------

def pit_tile():
    """Deep blue-violet nothing: a darker pool in the middle, a few faint stars far below."""
    rng = random.Random(77)
    img = Image.new("RGB", (32, 32))
    px = img.load()
    for y in range(32):
        for x in range(32):
            d = math.hypot(x - 15.5, y - 15.5) / 22
            base = (30 - round(18 * (1 - d)), 22 - round(14 * (1 - d)), 56 - round(26 * (1 - d)))
            px[x, y] = tuple(max(0, v + rng.choice((-2, 0, 0, 2))) for v in base)
    for _ in range(5):
        px[rng.randint(3, 28), rng.randint(3, 28)] = (110, 100, 170)
    return img


if __name__ == "__main__":
    write_sheet("GateProps", None, [
        ("Bramble", 1, False, [bramble()]),
        ("Burn", 8, False, [burn_frame(i) for i in range(4)]),
        ("Bloom", 6, False, [bloom_frame(i) for i in range(4)]),
        ("BrazierUnlit", 1, False, [brazier("unlit")]),
        ("BrazierFire", 6, True, [brazier("fire", i) for i in range(4)]),
        ("BrazierWater", 4, True, [brazier("water", i) for i in range(2)]),
    ], frame_size=S)
    write_sheet("HintBubbles", None, [
        ("Boots", 1, False, [bubble(boots_small)]),
        ("Lantern", 1, False, [bubble(lantern_small)]),
    ], pivot="center", frame_size=32)
    write_sheet("GateItems", None, [(name, 3, True, [floor_frame(draw, glint=i == 1) for i in range(2)])
                                    for name, draw in ITEMS.items()])
    write_sheet("EdgeArrow", None, [("Arrow", 2, True, [edge_arrow(True), edge_arrow(False)], None)], pivot="center", frame_size=32)
    write_sheet("Collectibles", None, [
        ("HeartPiece", 3, True, [heart_piece(False), heart_piece(True)], None),
        ("StarShard", 3, True, [star_shard(False), star_shard(True)], None),
    ])
    UI.mkdir(parents=True, exist_ok=True)
    for name, draw in ITEMS.items():
        icon(draw).save(UI / f"Icon{name}.png")
    ENV.mkdir(parents=True, exist_ok=True)
    pit_tile().save(ENV / "Pit.png")
    print("Wrote the gate icons and the pit texture")
