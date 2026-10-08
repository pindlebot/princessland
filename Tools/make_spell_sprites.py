"""Generates the spell effects: the wizard's Fireball and the princess's Tidal Orb.

Run:  Tools/.venv/bin/python Tools/make_spell_sprites.py
Out:  Assets/Art/Fireball.png / Fireball.json, Assets/Art/TidalOrb.png / TidalOrb.json,
      Assets/Art/DarkBolt.png / DarkBolt.json (the dark mermaids' bolt, thrown at the hero)
        Fly     4 frames, looping   the projectile, drawn pointing RIGHT (the game rotates it)
        Impact  5 frames, once      the burst where it hits something

Both spells share the same shapes; only the palettes differ.

Effects use a centered pivot. Instead of drawing shapes, each pixel gets a "heat"
value (0 = nothing, 1 = white hot) that is mapped onto a fire palette. Random noise
per frame makes the flames flicker.
"""
import math
import random

from sprite_common import Canvas, write_sheet

# Hottest first: (minimum heat, color)
FIRE = [
    (0.80, (255, 252, 220, 255)),
    (0.62, (255, 222, 96, 255)),
    (0.42, (255, 150, 40, 255)),
    (0.24, (222, 72, 30, 255)),
    (0.10, (150, 40, 30, 255)),
]
SMOKE = [(0.5, (110, 100, 104, 220)), (0.2, (78, 70, 76, 170))]

# Water, coldest-white first, then a fine mist instead of smoke
WATER = [
    (0.80, (240, 255, 252, 255)),
    (0.62, (150, 240, 232, 255)),
    (0.42, (64, 202, 198, 255)),
    (0.24, (36, 140, 172, 255)),
    (0.10, (28, 84, 132, 255)),
]
MIST = [(0.5, (184, 236, 240, 210)), (0.2, (120, 190, 214, 150))]

# The dark mermaids' sea-spell: deep violet ink with a pink heart (a monster's attack, so it
# reads clearly apart from both heroes' spells)
INK = [
    (0.80, (255, 214, 240, 255)),
    (0.62, (240, 140, 210, 255)),
    (0.42, (164, 86, 206, 255)),
    (0.24, (104, 52, 150, 255)),
    (0.10, (64, 32, 96, 255)),
]
INK_MIST = [(0.5, (150, 110, 180, 200)), (0.2, (104, 78, 136, 140))]

# Royal slime goo, for the Slime King's ground-slam shockwave
GOO = [
    (0.80, (250, 230, 255, 255)),
    (0.62, (210, 160, 240, 255)),
    (0.42, (160, 90, 210, 255)),
    (0.24, (110, 50, 160, 255)),
    (0.10, (70, 30, 110, 255)),
]
GOO_SPLATS = [(0.5, (170, 110, 210, 210)), (0.2, (120, 70, 170, 150))]
EMBER_OUTLINE = (96, 24, 16, 255)


def paint(heat_at, palette=FIRE, size=32):
    c = Canvas(size)
    for y in range(size):
        for x in range(size):
            h = heat_at(x, y)
            for threshold, color in palette:
                if h >= threshold:
                    c.dot(x, y, color)
                    break
    return c.img


def fly_frame(i, palette=FIRE):
    rng = random.Random(100 + i)
    noise = {(x, y): rng.uniform(-0.12, 0.12) for x in range(32) for y in range(32)}
    hx, hy = 21, 16  # head of the fireball
    sparks = {(rng.randint(3, 13), rng.randint(11, 21)) for _ in range(3)}

    def heat(x, y):
        head = 1 - math.dist((x, y), (hx, hy)) / 5.5
        tail = 0.0
        if x < hx:
            t = (hx - x) / 17  # 0 at the head, 1 at the end of the tail
            if t < 1:
                width = 4.6 * (1 - t) ** 0.8
                wobble = math.sin(x * 0.75 + i * 1.6) * 1.4 * t  # flames lick up and down
                tail = (1 - abs(y - hy - wobble) / width) * (1 - t) * 0.95
        if (x, y) in sparks:
            return 0.7
        base = max(head, tail)
        return base + noise[x, y] if base > 0.05 else 0.0  # flicker only where there's fire

    return paint(heat, palette)


def four_point_star(px, x, y, color, size, arm=2):
    """The signature motif in its tiniest form: a 1px centre with four arms."""
    for d in range(-arm, arm + 1):
        for sx, sy in ((x + d, y), (x, y + d)):
            if 0 <= sx < size and 0 <= sy < size:
                px[sx, sy] = color


def impact_frame(i, palette=FIRE, last=SMOKE, size=32):
    """The burst where a spell lands. `size` lets the same burst be drawn bigger *in pixels*
    (the Slime King's 96px shockwave) instead of being scaled up in game."""
    k = size / 32
    rng = random.Random(200 + i)
    noise = {(x, y): rng.uniform(-0.15, 0.15) for x in range(size) for y in range(size)}
    cx = cy = (size - 1) / 2

    if i == 4:  # last frame: drifting smoke
        def smoke(x, y):
            d = math.dist((x, y), (cx, cy)) / k
            return (1 - abs(d - 10) / 3) * 0.8 + noise[x, y] * 3 - 0.2 if d < 14 else 0.0
        return paint(smoke, last, size)

    def heat(x, y):
        d = math.dist((x, y), (cx, cy)) / k
        if i == 0:  # bright flash
            return 1.1 - d / 4
        if i == 1:  # the burst expands
            base = 1.05 - d / 7
        elif i == 2:  # becomes a ring, center cooling
            base = (1 - abs(d - 7) / 4) * 0.95
        else:  # i == 3: breaking into embers
            return (1 - abs(d - 10) / 3) * 0.55 + noise[x, y] * 2.2 if d < 14 else 0.0
        return base + noise[x, y] if base > 0.05 else 0.0

    img = paint(heat, palette, size)
    c_px = img.load()
    if i in (1, 2):  # eight sparks flying outward
        for a in range(0, 360, 45):
            r = (9 + i * 2) * k
            x = round(cx + r * math.cos(math.radians(a + 22 * i)))
            y = round(cy + r * math.sin(math.radians(a + 22 * i)))
            if 0 <= x < size and 0 <= y < size:
                c_px[x, y] = palette[1][1]
    if i == 3:  # as it fades, a few four-point glints (the game's star motif)
        for a in (45, 165, 285):
            r = 11 * k
            four_point_star(c_px, round(cx + r * math.cos(math.radians(a))),
                            round(cy + r * math.sin(math.radians(a))), palette[0][1], size)
    return img


def write_spell(name, palette, last):
    write_sheet(
        name,
        None,
        [
            ("Fly", 12, True, [fly_frame(i, palette) for i in range(4)]),
            ("Impact", 14, False, [impact_frame(i, palette, last) for i in range(5)]),
        ],
        pivot="center",
        outline_color=None,
    )


if __name__ == "__main__":
    write_spell("Fireball", FIRE, SMOKE)
    write_spell("TidalOrb", WATER, MIST)
    write_spell("DarkBolt", INK, INK_MIST)
    # Not a spell, but the same expanding-ring burst suits the King's landing.
    # Drawn at 96px (three tiles across) rather than scaled up in game, so its pixels match.
    write_sheet("Shockwave", None, [("Impact", 14, False, [impact_frame(i, GOO, GOO_SPLATS, 96) for i in range(5)])],
                pivot="center", outline_color=None, frame_size=96)
