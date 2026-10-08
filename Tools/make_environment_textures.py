"""Generates pixel-art environment textures, matching the wizard's 16 pixels per unit.

Run:  Tools/.venv/bin/python Tools/make_environment_textures.py
Out:  Assets/Art/Environment/
        Floor_0.png .. Floor_2.png   32x32  one 2x2m floor tile (plain, cracked, mossy)
        WallSide.png                 32x20  one 2m x 1.2m wall face (bricks)
        WallTop.png                  32x32  the cap stone on top of a wall block
        Grass_0.png .. Grass_2.png   32x32  outdoor ground: three *quiet* tones (base, cool, warm),
                                            laid in large patches so characters stand out
        EarthSide.png                32x32  layered earth for the floating island's cliff edges
        Path.png                     32x32  dirt path
        HedgeSide.png / HedgeTop.png 32x13 / 32x32  the low hedge around the castle grounds
        Roof.png                     32x32  aquamarine roof shingles for the castle towers
        Gate.png                     32x32  the castle's wooden gate
        WoodFloor.png                32x32  floorboards for the hero's home
        BathTile.png                 32x32  blue-and-white bathroom tiles
        Water.png                    32x32  the castle-grounds pond (scrolls slowly in game)
        Puddle.png                   32x32  dungeon flagstones under a shallow sheet of water
        Lava.png                     32x32  crusted lava with glowing cracks (hurts; glows and drifts in game)
        SpikePlate.png               32x32  an iron plate with nine holes for a spike trap's spikes

Every texture is seamless: mortar lines sit on the left/top edge only, so two
tiles placed side by side share a single 1px joint.
"""
import math
import random
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parent.parent / "Assets" / "Art" / "Environment"


def shade(rgb, amount):
    """Lighten (amount > 0) or darken (amount < 0) a color."""
    return tuple(max(0, min(255, round(c + amount))) for c in rgb)


def stone_rect(px, x0, y0, w, h, base, rng):
    """Fills one stone with per-pixel noise, lit from the top-left like the scene."""
    tone = rng.randint(-10, 10)  # each stone is slightly different
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            c = shade(base, tone + rng.choice((-6, -3, 0, 0, 0, 3)))
            if y == y0 or x == x0:
                c = shade(c, 14)
            elif y == y0 + h - 1 or x == x0 + w - 1:
                c = shade(c, -12)
            px[x, y] = c


# ---------- Floor ----------

FLOOR = (92, 84, 74)
FLOOR_MORTAR = (40, 36, 34)
MOSS = (70, 104, 52)


def floor_tile(seed, crack=False, moss=False):
    rng = random.Random(seed)
    img = Image.new("RGB", (32, 32), FLOOR_MORTAR)
    px = img.load()
    # Two rows of flagstones; each row splits at a different x so seams don't line up.
    for row_y, split in ((1, rng.randint(12, 19)), (17, rng.randint(12, 19))):
        for x0, w in ((1, split - 1), (split + 1, 31 - split)):
            stone_rect(px, x0, row_y, w, 15, FLOOR, rng)
    if crack:
        x, y = rng.randint(4, 10), rng.randint(3, 6)
        for _ in range(9):
            px[x, y] = FLOOR_MORTAR
            x = max(1, min(30, x + rng.choice((0, 1, 1))))
            y = min(30, y + 1)
    if moss:
        for _ in range(28):  # moss creeps along the mortar lines
            x, y = rng.randint(0, 31), rng.choice((0, 1, 16, 17, 15))
            px[x, y] = shade(MOSS, rng.randint(-12, 12))
        for _ in range(10):
            px[rng.randint(0, 31), rng.randint(0, 31)] = shade(MOSS, rng.randint(-8, 8))
    return img


# ---------- Walls ----------

BRICK = (104, 98, 116)
WALL_MORTAR = (46, 42, 54)
CAP = (128, 122, 138)


def wall_side():
    """32x20: three courses of bricks in a running bond, plus a dark grimy base."""
    rng = random.Random(7)
    img = Image.new("RGB", (32, 20), WALL_MORTAR)
    px = img.load()
    for course in range(3):
        y0 = 1 + course * 6
        offset = 8 if course % 2 else 0
        for i in range(2):
            x0 = offset + i * 16 + 1
            # Bricks that cross the right edge wrap to the left, so the face tiles seamlessly.
            tone = rng.randint(-10, 10)
            for y in range(y0, y0 + 5):
                for xx in range(15):
                    x = (x0 + xx) % 32
                    c = shade(BRICK, tone + rng.choice((-6, -3, 0, 0, 3)))
                    if y == y0:
                        c = shade(c, 14)
                    elif y == y0 + 4:
                        c = shade(c, -12)
                    px[x, y] = c
    for y in (18, 19):  # base course: darker, where the wall meets the floor
        for x in range(32):
            px[x, y] = shade(WALL_MORTAR, rng.randint(-4, 6) + (0 if y == 19 else 8))
    return img


def wall_top():
    """32x32 cap stone: one big slab with a bevelled rim and a few chips."""
    rng = random.Random(11)
    img = Image.new("RGB", (32, 32), WALL_MORTAR)
    px = img.load()
    stone_rect(px, 1, 1, 31, 31, CAP, rng)
    for _ in range(6):  # chips and pits
        x, y = rng.randint(4, 27), rng.randint(4, 27)
        px[x, y] = shade(CAP, -30)
        px[x + 1, y] = shade(CAP, -18)
    return img


# ---------- Outdoors ----------

GRASS = (78, 132, 58)
GRASS_DARK = (56, 104, 46)
GRASS_LIGHT = (108, 160, 74)
DIRT = (132, 104, 72)
LEAF, LEAF_DARK, LEAF_LIGHT = (52, 108, 50), (34, 76, 38), (82, 144, 66)


def noisy(size, base, rng, spread=(-6, -3, 0, 0, 3, 6)):
    img = Image.new("RGB", size, base)
    px = img.load()
    for y in range(size[1]):
        for x in range(size[0]):
            px[x, y] = shade(base, rng.choice(spread))
    return img, px


def grass_tile(seed, tone=(0, 0, 0)):
    """Quiet, low-contrast grass so characters, enemies and paths stand out against it.
    A handful of soft blades only (no flowers: those are saved for points of interest).
    Seamless: every stroke wraps around the edges (x % 32, y % 32)."""
    rng = random.Random(seed)
    base = tuple(c + t for c, t in zip(GRASS, tone))
    img, px = noisy((32, 32), base, rng, (-3, -2, 0, 0, 0, 2, 3))
    for _ in range(18):  # a few soft blades, just a shade off the ground
        x, y = rng.randrange(32), rng.randrange(32)
        color = shade(base, rng.choice((-12, -9, 8)))
        for i in range(rng.randint(1, 2)):
            px[x % 32, (y - i) % 32] = color
    return img


def earth_side():
    """Layers of soil and stone, for the cliff edge under the floating island."""
    rng = random.Random(81)
    bands = [(122, 88, 58), (104, 74, 50), (132, 98, 66), (92, 66, 46)]
    img = Image.new("RGB", (32, 32))
    px = img.load()
    for y in range(32):
        band = bands[(y // 6) % len(bands)]
        for x in range(32):
            px[x, y] = shade(band, rng.choice((-6, -3, 0, 0, 3)))
    for _ in range(10):  # pebbles
        x, y = rng.randrange(31), rng.randrange(31)
        px[x, y] = (150, 142, 132)
        px[x + 1, y] = (110, 104, 98)
    for x in range(32):  # a grassy lip along the top
        for y in range(rng.randint(1, 3)):
            px[x, y] = shade(GRASS, rng.choice((-10, -5, 0)))
    return img


def path_tile():
    rng = random.Random(21)
    img, px = noisy((32, 32), DIRT, rng, (-10, -5, 0, 0, 4, 8))
    for _ in range(14):  # pebbles
        x, y = rng.randrange(32), rng.randrange(32)
        px[x, y] = (150, 144, 136)
        px[(x + 1) % 32, y] = (112, 106, 100)
    return img


def hedge(size, top=False):
    rng = random.Random(31 if top else 32)
    img, px = noisy(size, LEAF, rng, (-8, -4, 0, 4, 8))
    w, h = size
    for _ in range(40 if top else 22):  # leaf clusters: light on top-left, dark below-right
        x, y = rng.randrange(w), rng.randrange(h)
        px[x, y] = LEAF_LIGHT
        px[(x + 1) % w, (y + 1) % h] = LEAF_DARK
    if not top:
        for x in range(w):  # shadow where the hedge meets the ground
            px[x, h - 1] = LEAF_DARK
    return img


def roof():
    """Rows of rounded aquamarine shingles, each row offset by half a shingle."""
    rng = random.Random(41)
    base, dark, light = (52, 168, 160), (26, 104, 104), (110, 210, 196)
    img = Image.new("RGB", (32, 32), base)
    px = img.load()
    for row in range(8):
        y0 = row * 4
        off = 4 if row % 2 else 0
        for x in range(32):
            px[x, y0 + 3] = dark  # bottom edge of the row
            px[x, y0] = shade(light, rng.randint(-8, 4)) if (x + off) % 8 in (2, 3, 4, 5) else base
            if (x + off) % 8 == 0:
                for y in range(y0, y0 + 3):
                    px[x, y] = dark  # gap between shingles
    return img


def gate():
    """Vertical oak planks held by two iron bands with rivets."""
    rng = random.Random(51)
    wood, dark = (110, 70, 38), (70, 42, 22)
    img, px = noisy((32, 32), wood, rng, (-6, -3, 0, 3))
    for x in range(0, 32, 6):
        for y in range(32):
            px[x, y] = dark
    for y0 in (7, 22):
        for y in (y0, y0 + 1):
            for x in range(32):
                px[x, y] = (80, 80, 92) if y == y0 else (54, 54, 64)
        for x in range(3, 32, 6):
            px[x, y0] = (170, 170, 184)
    return img


# ---------- Indoors (the hero's home) ----------

def wood_floor():
    """Four staggered boards per tile, with seams and the odd knot."""
    rng = random.Random(71)
    base, dark, light = (150, 104, 62), (96, 62, 34), (176, 128, 80)
    img, px = noisy((32, 32), base, rng, (-6, -3, 0, 3))
    for row in range(4):
        y0 = row * 8
        tone = rng.randint(-12, 10)
        for y in range(y0, y0 + 8):
            for x in range(32):
                px[x, y] = shade(px[x, y], tone)
        for x in range(32):
            px[x, y0] = dark  # seam between boards
            px[x, y0 + 1] = shade(light, tone)
        end = (row * 13 + 5) % 32  # board ends, staggered so they don't line up
        for y in range(y0, y0 + 8):
            px[end, y] = dark
        kx, ky = rng.randrange(32), y0 + rng.randint(3, 6)
        px[kx, ky] = dark
    return img


def bath_tile():
    """Glossy 8px tiles: white with a blue checker, grout lines on the top/left edges."""
    img = Image.new("RGB", (32, 32))
    px = img.load()
    for y in range(32):
        for x in range(32):
            blue = ((x // 8) + (y // 8)) % 2 == 1
            base = (120, 176, 214) if blue else (232, 236, 240)
            if x % 8 == 0 or y % 8 == 0:
                base = (160, 166, 176)  # grout
            elif x % 8 == 1 and y % 8 == 1:
                base = shade(base, 20)  # shine
            px[x, y] = base
    return img


def water_tile():
    """Soft blue with a few wavy highlight dashes; seamless because nothing crosses the edges."""
    rng = random.Random(81)
    img, px = noisy((32, 32), (78, 156, 206), rng, (-4, -2, 0, 2, 4))
    for _ in range(9):
        x, y = rng.randrange(2, 26), rng.randrange(1, 31)
        for i in range(rng.randint(3, 5)):
            px[x + i, y] = (196, 236, 250)
            if i == 0:
                px[x + i, y] = (140, 200, 236)
    return img


def puddle_tile():
    """The dungeon floor, darkened and tinted blue, with glints: a shallow, walkable puddle."""
    rng = random.Random(91)
    img = floor_tile(4)
    px = img.load()
    for y in range(32):
        for x in range(32):
            r, g, b = px[x, y]
            px[x, y] = (round(r * 0.55 + 20), round(g * 0.62 + 34), round(b * 0.7 + 62))
    for _ in range(6):
        x, y = rng.randrange(2, 27), rng.randrange(2, 30)
        for i in range(rng.randint(2, 4)):
            px[x + i, y] = (170, 210, 236)
    return img


# ---------- Hazards ----------

def lava_tile():
    """Dark crust plates floating on glowing lava. The plates are a Voronoi pattern measured
    around the wrapped tile (so it's seamless); the thin gaps between plates are the hot cracks,
    and a few plates have melted into bright pools."""
    rng = random.Random(101)
    seeds = [(rng.uniform(0, 32), rng.uniform(0, 32)) for _ in range(8)]
    molten = {i for i in range(len(seeds)) if rng.random() < 0.25}
    img = Image.new("RGB", (32, 32))
    px = img.load()
    for y in range(32):
        for x in range(32):
            dists = []
            for i, (sx, sy) in enumerate(seeds):
                dx = min(abs(x + 0.5 - sx), 32 - abs(x + 0.5 - sx))
                dy = min(abs(y + 0.5 - sy), 32 - abs(y + 0.5 - sy))
                dists.append((math.hypot(dx, dy), i))
            dists.sort()
            gap = dists[1][0] - dists[0][0]  # 0 right on a crack
            plate = dists[0][1]
            if gap < 0.9:
                c = (255, 214, 96)
            elif gap < 2.0:
                c = (246, 120, 34)
            elif plate in molten:
                c = shade((214, 74, 26), rng.choice((-12, -6, 0, 6, 12)))
            else:
                warm = max(0.0, 1 - (gap - 2.0) / 3)  # crust glows a little near the cracks
                c = shade((64 + round(60 * warm), 24 + round(14 * warm), 20), rng.choice((-6, -3, 0, 3)))
            px[x, y] = c
    return img


def spike_plate():
    """An iron plate with nine holes (3x3, 9px apart) where the spikes come up. The holes are
    symmetrical, so the plate looks the same however the floor block turns it."""
    rng = random.Random(111)
    img, px = noisy((32, 32), (78, 76, 90), rng, (-5, -3, 0, 0, 3, 5))
    for i in range(32):
        px[i, 0] = px[0, i] = (40, 38, 48)        # the joint with the next tile
        px[i, 1] = px[1, i] = (116, 114, 130)     # lit top and left edges
        px[i, 31] = px[31, i] = (52, 50, 62)      # shaded bottom and right edges
    for x, y in ((3, 3), (28, 3), (3, 28), (28, 28)):  # rivets
        px[x, y] = (150, 148, 164)
        px[x + 1, y + 1] = (46, 44, 56)
    for hx in (5, 14, 23):
        for hy in (5, 14, 23):
            for y in range(hy, hy + 4):
                for x in range(hx, hx + 4):
                    edge = x == hx or y == hy
                    px[x, y] = (34, 30, 40) if edge else (16, 14, 20)
            for k in range(4):  # a worn, shiny rim along the bottom and right of each hole
                px[hx + k, hy + 4] = (124, 122, 138)
                px[hx + 4, hy + k] = (124, 122, 138)
    return img


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    floor_tile(1).save(OUT / "Floor_0.png")
    floor_tile(2, crack=True).save(OUT / "Floor_1.png")
    floor_tile(3, moss=True).save(OUT / "Floor_2.png")
    wall_side().save(OUT / "WallSide.png")
    wall_top().save(OUT / "WallTop.png")
    grass_tile(61).save(OUT / "Grass_0.png")
    grass_tile(62, tone=(-6, -4, 0)).save(OUT / "Grass_1.png")   # a touch cooler and darker
    grass_tile(63, tone=(6, 6, -2)).save(OUT / "Grass_2.png")    # a touch warmer and lighter
    earth_side().save(OUT / "EarthSide.png")
    path_tile().save(OUT / "Path.png")
    hedge((32, 13)).save(OUT / "HedgeSide.png")
    hedge((32, 32), top=True).save(OUT / "HedgeTop.png")
    roof().save(OUT / "Roof.png")
    gate().save(OUT / "Gate.png")
    wood_floor().save(OUT / "WoodFloor.png")
    bath_tile().save(OUT / "BathTile.png")
    water_tile().save(OUT / "Water.png")
    puddle_tile().save(OUT / "Puddle.png")
    lava_tile().save(OUT / "Lava.png")
    spike_plate().save(OUT / "SpikePlate.png")
    print("Wrote", ", ".join(sorted(p.name for p in OUT.glob("*.png"))))


if __name__ == "__main__":
    main()
