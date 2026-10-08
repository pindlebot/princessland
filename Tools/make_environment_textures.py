"""Generates pixel-art environment textures, matching the wizard's 16 pixels per unit.

Run:  Tools/.venv/bin/python Tools/make_environment_textures.py
Out:  Assets/Art/Environment/
        Floor_0.png .. Floor_2.png   32x32  one 2x2m floor tile (plain, cracked, mossy)
        WallSide.png                 32x20  one 2m x 1.2m wall face (bricks)
        WallTop.png                  32x32  the cap stone on top of a wall block
        Grass_0.png .. Grass_2.png   32x32  outdoor ground: three closely related sage tones (base, cool,
                                            warm), laid in large patches so characters stand out
        EarthSide.png                32x64  the floating island's exposed side (4m): a scalloped grassy
                                            lip, then three broad layers of soil fading to rock
        Path.png / Path_1.png        32x32  sandy path: broad colour, and now and then a stone
        Bank.png                     32x4   the pond's bank (0.25m): a grassy lip over damp earth
        HedgeSide.png / HedgeTop.png 32x13 / 32x32  the low hedge around the castle grounds, scalloped
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

Outdoors follows the storybook-diorama rules in palette.py: broad, quiet colour shapes
and hardly any per-pixel noise, so the chunky characters are the most detailed thing on
screen. Texels are always 1/16 m on the surface they cover (a 4m cliff face is 64px tall).
"""
import math
import random
from pathlib import Path

from PIL import Image

import palette as pal

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

BRICK = (112, 104, 124)
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
            tone = rng.randint(-6, 6)
            for y in range(y0, y0 + 5):
                for xx in range(15):
                    x = (x0 + xx) % 32
                    c = shade(BRICK, tone + rng.choice((-2, 0, 0, 0, 2)))
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

LEAF, LEAF_DARK, LEAF_LIGHT = pal.SAGE_DARK, pal.SAGE_DEEP, pal.SAGE


def noisy(size, base, rng, spread=(-6, -3, 0, 0, 3, 6)):
    img = Image.new("RGB", size, base)
    px = img.load()
    for y in range(size[1]):
        for x in range(size[0]):
            px[x, y] = shade(base, rng.choice(spread))
    return img, px


def grass_tile(seed, base):
    """Flat sage with just three or four tiny two-pixel blades, a shade darker. No noise:
    the ground should read as calm areas of colour. Seamless (strokes wrap at the edges)."""
    rng = random.Random(seed)
    img = Image.new("RGB", (32, 32), base)
    px = img.load()
    for _ in range(rng.randint(3, 4)):
        x, y = rng.randrange(32), rng.randrange(32)
        px[x % 32, y % 32] = shade(base, -10)
        px[(x + 1) % 32, (y - 1) % 32] = shade(base, -10)
    return img


def scallops(px, width, y0, depth, color, period=8):
    """The garden motif: a row of little round scallops hanging down from row y0."""
    for x in range(width):
        t = (x % period + 0.5) / period * 2 - 1           # -1..1 across one scallop
        hang = round(depth * (1 - t * t) ** 0.5)           # deepest in the middle
        for y in range(y0, y0 + hang):
            px[x, y] = color


def earth_side():
    """The island's exposed side, 32x64 for a 2m x 4m face: a scalloped grassy lip, then three
    broad layers (warm soil, deeper soil, lavender-grey rock) with gently wavy joins, and only a
    few stones. Seamless across: every wave repeats every 32px."""
    rng = random.Random(81)
    img = Image.new("RGB", (32, 64), pal.EARTH_TOP)
    px = img.load()
    for x in range(32):
        wave = math.sin(x / 32 * 2 * math.pi)
        mid = 20 + round(1.5 * wave)
        low = 40 + round(1.5 * math.sin(x / 32 * 4 * math.pi + 1))
        for y in range(64):
            if y >= low:
                c = pal.EARTH_LOW if y < 54 else pal.ROCK
            elif y >= mid:
                c = pal.EARTH_MID
            else:
                c = pal.EARTH_TOP
            if y in (mid, low):
                c = shade(c, -12)  # a thin dark seam where layers meet
            px[x, y] = c
    for x, y in ((21, 47),):  # one stone, not a scatter (it repeats every 2m)
        px[x, y] = pal.PEBBLE
        px[x + 1, y] = pal.PEBBLE_SHADE
    for x in range(32):
        for y in range(2):
            px[x, y] = pal.SAGE_DARK
    scallops(px, 32, 2, 3, pal.SAGE_DARK)
    return img


def path_tile(seed, stone):
    """Broad warm sand with a few slightly darker grains; `stone` adds one small pebble."""
    rng = random.Random(seed)
    img = Image.new("RGB", (32, 32), pal.SAND)
    px = img.load()
    for _ in range(5):
        px[rng.randrange(32), rng.randrange(32)] = pal.SAND_SHADE
    if stone:
        x, y = rng.randrange(6, 24), rng.randrange(6, 24)
        for dx, dy, c in ((0, 0, pal.PEBBLE), (1, 0, pal.PEBBLE), (0, 1, pal.PEBBLE_SHADE), (1, 1, pal.PEBBLE_SHADE)):
            px[x + dx, y + dy] = c
        px[x + 2, y + 1] = pal.SAND_SHADE  # its little shadow, away from the sun
    return img


def bank():
    """32x4: the pond's bank, the 0.25m of ground you see above the water. A grassy lip, then
    damp earth that darkens toward the waterline, so the pond reads as set into the ground."""
    img = Image.new("RGB", (32, 4))
    px = img.load()
    for x in range(32):
        px[x, 0] = pal.SAGE_DARK
        px[x, 1] = pal.EARTH_TOP
        px[x, 2] = pal.EARTH_MID
        px[x, 3] = shade(pal.EARTH_MID, -18)
    return img


def hedge(size, top=False):
    """Clipped hedge in broad sage. The top has soft scalloped leaf clusters; the side has a
    scalloped highlight under its top edge and a shadow along the ground."""
    rng = random.Random(31 if top else 32)
    w, h = size
    img = Image.new("RGB", size, LEAF)
    px = img.load()
    if top:
        for cx, cy in ((5, 6), (20, 3), (13, 18), (28, 21), (6, 27)):
            for dx in range(-3, 4):
                for dy in range(-2, 1):
                    if dx * dx / 9 + dy * dy / 4 <= 1:
                        px[(cx + dx) % w, (cy + dy) % h] = shade(LEAF, 10)
    else:
        scallops(px, w, 0, 2, shade(LEAF, 10))
        for x in range(w):
            px[x, h - 1] = LEAF_DARK
            px[x, h - 2] = shade(LEAF, -8)
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
    """Flat, calm water with just two short glints, one pixel tall like every other highlight.
    Seamless because nothing crosses the edges. WaterScroll slides it very slowly."""
    img = Image.new("RGB", (32, 32), pal.WATER)
    px = img.load()
    for x0, y, n in ((5, 9, 3), (19, 23, 2)):
        for i in range(n):
            px[x0 + i, y] = pal.WATER_GLINT
        px[x0 + n, y] = shade(pal.WATER, 18)
    for x in range(10, 18):  # one faint deeper ripple
        px[x, 16] = pal.WATER_DEEP
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
    grass_tile(61, pal.SAGE).save(OUT / "Grass_0.png")
    grass_tile(62, pal.SAGE_COOL).save(OUT / "Grass_1.png")
    grass_tile(63, pal.SAGE_WARM).save(OUT / "Grass_2.png")
    earth_side().save(OUT / "EarthSide.png")
    path_tile(21, stone=False).save(OUT / "Path.png")
    path_tile(22, stone=True).save(OUT / "Path_1.png")
    bank().save(OUT / "Bank.png")
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
