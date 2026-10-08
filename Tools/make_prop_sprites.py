"""Generates the environment props: treasure chest, wall torch and grass tufts.

Run:  Tools/.venv/bin/python Tools/make_prop_sprites.py
Out:  Assets/Art/Props.png / Props.json   (32x32 frames, bottom pivot)
        Chest_Closed   1 frame
        Chest_Open     4 frames, once     lid lifts, gold glints inside
        Sparkle        5 frames, once     gold stars rising out of the chest
        Torch          4 frames, looping  flickering flame on an iron wall bracket
        Grass_A/B/C    2 frames, looping  tufts swaying (C has little flowers)
        Ring           2 frames, looping  the Ember Ring lying on the floor, glinting
        Flag           3 frames, looping  aquamarine banner waving on a pole (castle keep)
        Coin           4 frames, looping  a spinning gold coin (enemies drop these)
        Butterfly      2 frames, looping  wings open/closed, for ambient life
        Mote           3 frames, looping  a tiny drifting magical sparkle
        Stairs         1 frame            stone steps leading down (flat on the floor, under the exit)
"""
import math
import random

from make_spell_sprites import paint
from sprite_common import Canvas, write_sheet

WOOD, WOOD_SH, WOOD_HI = (128, 80, 42, 255), (86, 50, 26, 255), (160, 106, 58, 255)
IRON, IRON_HI = (84, 84, 96, 255), (140, 140, 156, 255)
GOLD, GOLD_HI, GOLD_SH = (236, 192, 70, 255), (255, 244, 170, 255), (170, 120, 36, 255)
INSIDE = (34, 20, 14, 255)
# Tufts are only a shade off the ground (78, 132, 58), so they add texture without noise.
GRASS = [(62, 112, 50, 255), (70, 122, 54, 255), (88, 142, 64, 255)]
FLOWER = (240, 214, 90, 255)

# Flame colors, hottest first (same idea as the fireball, a little more orange)
FLAME = [
    (0.80, (255, 248, 200, 255)),
    (0.60, (255, 214, 90, 255)),
    (0.38, (255, 146, 40, 255)),
    (0.18, (214, 70, 30, 255)),
]


# ---------- Chest ----------

def chest_body(c):
    """The box part, shared by every frame: planks, iron bands, gold lock plate."""
    c.rect(6, 19, 25, 30, WOOD)
    for y in (22, 26):
        c.rect(6, y, 25, y, WOOD_SH)  # plank seams
    c.rect(6, 30, 25, 30, WOOD_SH)
    c.rect(6, 19, 6, 30, WOOD_HI)
    for x in (9, 22):  # iron bands
        c.rect(x, 19, x + 1, 30, IRON)
        c.rect(x, 19, x, 30, IRON_HI)


def lock_plate(c, y):
    c.rect(14, y, 17, y + 4, GOLD)
    c.rect(14, y, 17, y, GOLD_HI)
    c.rect(15, y + 2, 16, y + 3, INSIDE)  # keyhole


def chest_closed():
    c = Canvas()
    chest_body(c)
    # Rounded lid
    c.rect(7, 12, 24, 12, WOOD_HI)
    c.rect(6, 13, 25, 18, WOOD)
    c.rect(6, 13, 25, 13, WOOD_HI)
    c.rect(6, 18, 25, 18, WOOD_SH)
    for x in (9, 22):
        c.rect(x, 12, x + 1, 18, IRON)
    lock_plate(c, 16)
    return c.img


def chest_open(stage):
    """stage 0..3: the lid tips back on its hinge, showing its darker underside,
    while the gap above the box fills with glowing gold."""
    c = Canvas()
    chest_body(c)
    gap = [1, 3, 5, 5][stage]  # rows of interior visible above the box
    lid_h = [6, 5, 5, 5][stage]  # the lid gets shorter as it tilts toward edge-on
    interior_top = 19 - gap
    lid_bottom = interior_top - 1  # hinged: the lid always touches the interior's back edge

    c.rect(7, interior_top, 24, 18, INSIDE)
    if stage >= 2:
        rng = random.Random(stage)
        for x in range(8, 24):
            top = interior_top + 1 + (x * 7) % 3 // 2
            c.rect(x, top, x, 18, GOLD if x % 3 else GOLD_SH)
            if rng.random() < 0.3:
                c.dot(x, top, GOLD_HI)

    lid_top = lid_bottom - lid_h + 1
    c.rect(6, lid_top, 25, lid_bottom, WOOD_SH if stage else WOOD)  # underside is in shadow
    c.rect(6, lid_top, 25, lid_top, WOOD if stage else WOOD_HI)
    for x in (9, 22):
        c.rect(x, lid_top, x + 1, lid_bottom, IRON)
    lock_plate(c, 20)  # the plate stays on the box's front
    if stage == 3:  # a glint on the gold
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0), (0, 0)):
            c.dot(19 + dx, interior_top + 1 + dy, GOLD_HI)
    return c.img


def sparkle(frame):
    """Gold four-point stars drifting up out of the chest and shrinking."""
    c = Canvas()
    rng = random.Random(7)
    for _ in range(6):
        x = rng.randint(8, 23)
        y0 = rng.randint(18, 26)
        speed = rng.uniform(2.5, 4)
        y = round(y0 - frame * speed)
        size = 2 if frame < 2 else (1 if frame < 4 else 0)
        if y < 1:
            continue
        c.dot(x, y, GOLD_HI)
        for r in range(1, size + 1):
            for dx, dy in ((r, 0), (-r, 0), (0, r), (0, -r)):
                c.dot(x + dx, y + dy, GOLD if r == size else GOLD_HI)
    return c.img


# ---------- Torch ----------

def torch(frame):
    rng = random.Random(30 + frame)
    noise = {(x, y): rng.uniform(-0.1, 0.1) for x in range(32) for y in range(32)}
    sway = math.sin(frame * math.pi / 2) * 0.8

    def heat(x, y):
        # A teardrop: round at the bottom (y ~ 15), pointed and swaying at the top
        up = max(0.0, 15 - y)
        cx = 15.5 + sway * up / 8
        width = 3.4 * (1 - up / 11) if up < 11 else 0
        if y > 15:
            d = math.dist((x, y), (15.5, 15)) / 3.4
        elif width > 0:
            d = abs(x - cx) / width
        else:
            return 0.0
        base = (1 - d) * (1.1 - up / 14)
        return base + noise[x, y] if base > 0.05 else 0.0

    img = paint(heat, FLAME)
    c = Canvas()
    c.img.alpha_composite(img)
    c.px = c.img.load()
    c.rect(14, 17, 17, 18, IRON)  # cup holding the flame
    c.rect(14, 17, 17, 17, IRON_HI)
    c.rect(15, 19, 16, 26, WOOD)  # handle
    c.rect(15, 19, 15, 26, WOOD_HI)
    c.rect(12, 24, 19, 25, IRON)  # wall bracket
    c.rect(12, 24, 19, 24, IRON_HI)
    return c.img


# ---------- Ember Ring ----------

GEM, GEM_HI, GEM_SH = (230, 60, 36, 255), (255, 170, 120, 255), (150, 30, 24, 255)


def draw_ring(c, cx, cy, radius, band, glint=False):
    """A gold band (lit from the top-left) with a red ember gem set on top.
    Shared with make_hud_sprites.py for the inventory icon."""
    for y in range(32):
        for x in range(32):
            d = math.dist((x + 0.5, y + 0.5), (cx, cy))
            if radius - band <= d <= radius:
                lit = (cx - x) + (cy - y) > 0  # top-left half catches the light
                c.dot(x, y, GOLD_HI if lit and d > radius - 1 else GOLD if lit else GOLD_SH)
    gx, gy = int(cx) - 1, int(cy - radius) - 1
    gem = max(2, band)
    c.rect(gx, gy, gx + gem, gy + gem, GEM)
    c.rect(gx + gem, gy + 1, gx + gem, gy + gem, GEM_SH)
    c.dot(gx, gy, GEM_HI)
    if glint:
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0), (0, 0)):
            c.dot(int(cx + radius * 0.7) + dx, int(cy - radius * 0.7) + dy, (255, 255, 230, 255))


def ring(frame):
    c = Canvas()
    draw_ring(c, 16, 24, 4.5, 2, glint=frame == 1)
    return c.img


# ---------- Flag ----------

def flag(frame):
    """A pole with a banner whose rows ripple: each column is shifted up/down by a sine
    wave that travels along the cloth from frame to frame."""
    c = Canvas()
    c.rect(9, 4, 9, 31, (150, 150, 162, 255))  # pole
    c.rect(8, 3, 10, 3, GOLD)  # finial
    cloth, dark, emblem = (64, 196, 180, 255), (34, 136, 134, 255), (240, 248, 244, 255)
    for x in range(10, 24):
        t = x - 10
        dy = round(math.sin(t * 0.55 - frame * 2.1) * 1.3 * (t / 13))  # pinned at the pole
        for y in range(5, 13):
            c.dot(x, y + dy, dark if y == 12 else cloth)
        if 14 <= x <= 17:
            for y in range(7, 11):
                if (x, y) in ((15, 7), (16, 7), (14, 8), (17, 8), (14, 9), (17, 9), (15, 10), (16, 10)):
                    c.dot(x, y + dy, emblem)  # a little white gem shape
    return c.img


# ---------- Coin ----------

def coin(frame):
    """A spinning coin: as it turns, its visible width shrinks to an edge and back."""
    c = Canvas()
    half = [4.5, 3, 1, 3][frame]
    c.ellipse(15.5, 26, half, 4.5, GOLD_SH)  # the rim, slightly darker
    if half > 1.5:
        c.ellipse(15.5, 26, half - 1, 3.5, GOLD)
        c.dot(round(15.5 - half / 2), 24, GOLD_HI)  # glint on the face
    else:
        c.rect(15, 22, 16, 30, GOLD_HI)  # edge-on
    return c.img


# ---------- Ambient life and the stairs ----------

def butterfly(frame):
    c = Canvas()
    wing, spot = (250, 200, 230, 255), (240, 140, 190, 255)
    body = (74, 37, 69, 255)
    c.rect(15, 14, 16, 18, body)
    if frame == 0:  # wings open
        for side in (-1, 1):
            x0 = 15 + (2 if side > 0 else -6)
            c.ellipse(x0 + 2, 14, 2.5, 2.5, wing)
            c.ellipse(x0 + 2, 18, 2, 1.8, wing)
            c.dot(x0 + 2, 14, spot)
    else:  # wings folded up
        c.rect(13, 11, 14, 15, wing)
        c.rect(17, 11, 18, 15, wing)
    return c.img


def mote(frame):
    c = Canvas()
    glow, core = (170, 255, 240, 200), (255, 255, 255, 255)
    r = [1, 2, 1][frame]
    c.dot(15, 16, core)
    for d in range(1, r + 1):
        for dx, dy in ((d, 0), (-d, 0), (0, d), (0, -d)):
            c.dot(15 + dx, 16 + dy, glow)
    return c.img


def stairs():
    """Steps going down into the dark, seen from above: drawn to lie flat on the floor."""
    c = Canvas()
    stone, edge, dark = (150, 144, 160, 255), (196, 190, 204, 255), (40, 30, 44, 255)
    c.rect(3, 3, 28, 28, (96, 90, 104, 255))  # stone surround
    c.rect(5, 5, 26, 26, dark)
    for k, y in enumerate(range(6, 26, 4)):  # each step a little narrower and darker going down
        inset = k
        shadeK = -18 * k
        c.rect(6 + inset, y, 25 - inset, y + 2, tuple(max(0, v + shadeK) for v in stone[:3]) + (255,))
        c.rect(6 + inset, y, 25 - inset, y, edge if k < 2 else stone)
    return c.img


# ---------- Grass ----------

def grass(seed, frame, flowers=False):
    rng = random.Random(seed)
    c = Canvas()
    for _ in range(rng.randint(6, 9)):
        x = rng.randint(11, 20)
        height = rng.randint(4, 9)
        lean = rng.choice((-1, 0, 0, 1))
        color = rng.choice(GRASS)
        for i in range(height):
            t = i / height
            # Tips sway one pixel between the two frames; bases stay put.
            dx = round(lean * t * 2 + (t > 0.5) * (1 if frame else 0) * (1 if lean >= 0 else -1))
            c.dot(x + dx, 30 - i, color)
        if flowers and rng.random() < 0.5:
            c.dot(x + round(lean * 2) + (1 if frame and lean >= 0 else 0), 30 - height, FLOWER)
    return c.img


if __name__ == "__main__":
    write_sheet(
        "Props",
        None,
        [
            ("Chest_Closed", 1, False, [chest_closed()]),
            ("Chest_Open", 12, False, [chest_open(s) for s in range(4)]),
            ("Sparkle", 10, False, [sparkle(f) for f in range(5)]),
            ("Torch", 8, True, [torch(f) for f in range(4)]),
            ("Grass_A", 2, True, [grass(1, f) for f in range(2)]),
            ("Grass_B", 2, True, [grass(2, f) for f in range(2)]),
            ("Grass_C", 2, True, [grass(3, f, flowers=True) for f in range(2)]),
            ("Ring", 3, True, [ring(f) for f in range(2)]),
            ("Flag", 6, True, [flag(f) for f in range(3)]),
            ("Coin", 10, True, [coin(f) for f in range(4)]),
            ("Butterfly", 6, True, [butterfly(f) for f in range(2)]),
            ("Mote", 5, True, [mote(f) for f in range(3)]),
            ("Stairs", 1, False, [stairs()]),
        ],
    )
