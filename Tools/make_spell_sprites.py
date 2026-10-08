"""Generates the spell effects: the wizard's Fireball and the princess's Tidal Orb.

Run:  Tools/.venv/bin/python Tools/make_spell_sprites.py
Out:  Assets/Art/Fireball.png / Fireball.json, Assets/Art/TidalOrb.png / TidalOrb.json
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


def paint(heat_at, palette=FIRE):
    c = Canvas()
    for y in range(32):
        for x in range(32):
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


def impact_frame(i, palette=FIRE, last=SMOKE):
    rng = random.Random(200 + i)
    noise = {(x, y): rng.uniform(-0.15, 0.15) for x in range(32) for y in range(32)}
    cx, cy = 15.5, 15.5

    if i == 4:  # last frame: drifting smoke
        def smoke(x, y):
            d = math.dist((x, y), (cx, cy))
            return (1 - abs(d - 10) / 3) * 0.8 + noise[x, y] * 3 - 0.2 if d < 14 else 0.0
        return paint(smoke, last)

    def heat(x, y):
        d = math.dist((x, y), (cx, cy))
        if i == 0:  # bright flash
            return 1.1 - d / 4
        if i == 1:  # fireball expands
            base = 1.05 - d / 7
        elif i == 2:  # becomes a ring, center cooling
            base = (1 - abs(d - 7) / 4) * 0.95
        else:  # i == 3: breaking into embers
            return (1 - abs(d - 10) / 3) * 0.55 + noise[x, y] * 2.2 if d < 14 else 0.0
        return base + noise[x, y] if base > 0.05 else 0.0

    img = paint(heat, palette)
    if i in (1, 2):  # eight sparks flying outward
        c_px = img.load()
        for a in range(0, 360, 45):
            r = 9 + i * 2
            x = round(cx + r * math.cos(math.radians(a + 22 * i)))
            y = round(cy + r * math.sin(math.radians(a + 22 * i)))
            if 0 <= x < 32 and 0 <= y < 32:
                c_px[x, y] = palette[1][1]
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
    # Not a spell, but the same expanding-ring burst suits the King's landing.
    write_sheet("Shockwave", None, [("Impact", 14, False, [impact_frame(i, GOO, GOO_SPLATS) for i in range(5)])],
                pivot="center", outline_color=None)
