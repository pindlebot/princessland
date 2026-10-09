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
        RockSide.png / RockSideLow.png 32x40 / 32x20  the rocky hill around Amethyra's cave (2.5m crags,
                                            and 1.2m rocks where a crag would hide the ground behind it)
        RockTop.png                  32x32  the top of a rock block, with a patch of moss
        CaveFloor.png                32x32  the cave's floor: dark packed earth with a few pebbles
        Sand_0.png / Sand_1.png      32x32  Mermaid Cove's beach: pale, calm sand (Sand_1 has a shell)
        Sea.png                      32x32  the cove's sea: a step deeper than the pond, with one glint
        SandBank.png                 32x4   the sea's bank: a sandy lip over wet sand
        Planks.png                   32x32  the cove's jetties and boardwalks: driftwood boards
        Waterfall.png                32x16  falling water, repeating downward (scrolls fast in game)
      The village (east of the castle on Level 0):
        Cobble_0.png / Cobble_1.png  32x32  the village's cobbled street and square (Cobble_1 has moss)
        Plaster.png                  32x20  one 2m x 1.2m course of a timber-framed house wall
        RoofTiles.png / Thatch.png   32x32  terracotta roof tiles (the shop) and straw thatch (the cottage)
        Window.png                   16x16  a cottage window with a box of flowers
        Lancet.png / RoseWindow.png  12x32 / 24x24  the cathedral's stained glass (it glows a little)
        TownDoor.png                 16x32  a plank door with an arched top
        Awning.png                   32x16  the shop's coral-and-cream striped awning
        ShopSign.png                 24x16  Barnaby's sign: a bottle of bubble bath and bubbles

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


def sand_tile(seed, shell):
    """Pale beach sand: broad colour, a few darker grains, and (shell=True) one small coral shell."""
    rng = random.Random(seed)
    img = Image.new("RGB", (32, 32), pal.BEACH)
    px = img.load()
    for _ in range(6):
        px[rng.randrange(32), rng.randrange(32)] = pal.BEACH_SHADE
    if shell:
        x, y = rng.randrange(6, 24), rng.randrange(6, 24)
        for dx, dy in ((0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (1, 2)):
            px[x + dx, y + dy] = pal.CORAL_LIGHT
        px[x + 1, y] = pal.CREAM
        px[x + 3, y + 1] = pal.BEACH_SHADE  # its little shadow, away from the sun
    return img


def sea_tile():
    """The open sea: the pond's calm look a step deeper, with one short glint and a faint swell."""
    img = Image.new("RGB", (32, 32), pal.SEA)
    px = img.load()
    for i in range(3):
        px[8 + i, 11] = pal.FOAM
    px[11, 11] = shade(pal.SEA, 18)
    for x in range(18, 27):
        px[x, 24] = pal.SEA_DEEP
    return img


def sand_bank():
    """32x4: the 0.25m of beach you see above the sea. A light sandy lip, then wet sand."""
    img = Image.new("RGB", (32, 4))
    px = img.load()
    for x in range(32):
        px[x, 0] = pal.BEACH
        px[x, 1] = pal.BEACH_SHADE
        px[x, 2] = pal.WET_SAND
        px[x, 3] = shade(pal.WET_SAND, -18)
    return img


def planks():
    """Four driftwood boards across the tile, with dark gaps between them and a nail at each end."""
    img = Image.new("RGB", (32, 32), pal.PLANK)
    px = img.load()
    for board in range(4):
        y0 = board * 8
        tone = (0, -6, 4, -3)[board]
        for y in range(y0, y0 + 8):
            for x in range(32):
                px[x, y] = shade(pal.PLANK, tone)
        for x in range(32):
            px[x, y0] = pal.PLANK_SHADE           # the gap between boards
            px[x, y0 + 1] = shade(pal.PLANK_LIGHT, tone)
        for nx in (3, 28):
            px[nx, y0 + 4] = pal.PLANK_SHADE
        gx = (board * 11 + 7) % 26 + 3            # a short grain line
        for i in range(4):
            px[gx + i, y0 + 5] = shade(pal.PLANK, tone - 10)
    return img


def waterfall():
    """32x16 of falling water, seamless downward: soft vertical streaks of sea, foam and glint.
    Repeats every metre on the cliff; WaterScroll slides it down quickly."""
    rng = random.Random(121)
    img = Image.new("RGB", (32, 16), pal.WATER)
    px = img.load()
    for x in range(32):
        lane = rng.choice((pal.WATER, pal.WATER, pal.SEA, pal.WATER_DEEP))
        for y in range(16):
            px[x, y] = lane
    for _ in range(7):                            # streaks of white water
        x, y, n = rng.randrange(32), rng.randrange(16), rng.randint(3, 6)
        for i in range(n):
            px[x, (y + i) % 16] = pal.FOAM if i else pal.WATER_GLINT
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


def rock_side(height, seed):
    """Lavender-grey rock in broad horizontal ledges, each lit along its top edge, with a few
    cracks. 32 wide (2m) and 16px per metre tall. Seamless across: ledges wrap at the edges."""
    rng = random.Random(seed)
    img = Image.new("RGB", (32, height), pal.STONE_SHADE)
    px = img.load()
    y = 0
    while y < height:
        h = rng.randint(6, 10)
        tone = rng.randint(-8, 6)
        wave = rng.uniform(0, 2 * math.pi)
        for x in range(32):
            top = y + round(1.2 * math.sin(x / 32 * 2 * math.pi + wave))
            for yy in range(max(0, top), min(height, y + h)):
                c = shade(pal.STONE_SHADE, tone)
                if yy == max(0, top):
                    c = shade(pal.STONE, tone)        # the lit lip of the ledge
                elif yy == y + h - 1:
                    c = shade(pal.ROCK, tone - 6)     # its shaded underside
                px[x, yy] = c
        y += h
    for _ in range(height // 10):                     # cracks
        x, yy = rng.randrange(32), rng.randrange(height - 4)
        for i in range(rng.randint(2, 4)):
            px[(x + i // 2) % 32, yy + i] = shade(pal.ROCK, -14)
    for x in range(32):                               # grime where it meets the ground
        px[x, height - 1] = shade(pal.ROCK, -10)
    return img


def rock_top():
    rng = random.Random(91)
    img = Image.new("RGB", (32, 32), pal.STONE_SHADE)
    px = img.load()
    for y in range(32):
        for x in range(32):
            if rng.random() < 0.08:
                px[x, y] = shade(pal.STONE_SHADE, rng.choice((-8, 8)))
    for cx, cy, r in ((10, 11, 6), (22, 22, 4)):      # moss patches
        for y in range(32):
            for x in range(32):
                d = (x - cx) ** 2 + (y - cy) ** 2
                if d <= r * r:  # shaded along the rim away from the sun
                    rim = d > (r - 1.5) ** 2 and (x > cx or y > cy)
                    px[x, y] = pal.SAGE_DEEP if rim else pal.SAGE_DARK
    return img


def cave_floor():
    """Dark, cool packed earth: darker than the grass outside so the cave reads as a hollow."""
    rng = random.Random(77)
    base = (98, 86, 96)
    img, px = noisy((32, 32), base, rng, spread=(-4, 0, 0, 0, 3))
    for _ in range(5):
        x, y = rng.randrange(31), rng.randrange(31)
        px[x, y] = pal.PEBBLE_SHADE
        px[x + 1, y] = shade(pal.PEBBLE_SHADE, -16)
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


# ---------- The village (Level 0, east of the castle) ----------
# Warm, lived-in materials for the village's houses, a step warmer than the castle's
# lavender stone: cobbles, cream plaster with oak beams, terracotta tiles and straw thatch.
# Windows, doors, the awning and the sign go on thin blocks just proud of a wall, so each is
# drawn at its real size (16 texels a metre) and keeps the wall's colour around its shape.

COBBLE, COBBLE_SHADE, COBBLE_GAP = (186, 172, 160), (160, 146, 138), (150, 128, 104)
OAK, OAK_SHADE = (118, 80, 56), (88, 58, 46)
PLASTER = pal.CREAM_SHADE
TERRACOTTA, TERRACOTTA_DARK, TERRACOTTA_LIGHT = (198, 112, 92), (142, 70, 66), (226, 152, 120)
STRAW, STRAW_DARK, STRAW_LIGHT = (212, 178, 108), (164, 128, 74), (236, 210, 146)
GLASS = [(96, 168, 214), (240, 112, 128), (255, 214, 110), (122, 196, 132), (170, 130, 214)]
LEAD = (60, 48, 70)


def cobble_tile(seed, moss):
    """Rounded cobbles in four staggered rows on sandy gaps: calm, a little lighter than the
    castle's flagstones. Seamless: the stones wrap around the edges."""
    rng = random.Random(seed)
    img = Image.new("RGB", (32, 32), COBBLE_GAP)
    px = img.load()
    for row in range(4):
        y0 = row * 8
        offset = 4 if row % 2 else 0
        for i in range(4):
            x0 = offset + i * 8
            tone = rng.randint(-8, 8)
            for dy in range(7):
                for dx in range(7):
                    # Round off the corners of each 7x7 stone.
                    if (dx in (0, 6)) and (dy in (0, 6)):
                        continue
                    c = shade(COBBLE, tone)
                    if dy <= 1 and dx <= 4:
                        c = shade(c, 12)                     # lit from the top-left
                    elif dy >= 5 or dx == 6:
                        c = shade(COBBLE_SHADE, tone)
                    px[(x0 + dx) % 32, (y0 + dy) % 32] = c
    if moss:  # a few tufts of moss between the stones
        for _ in range(6):
            x, y = rng.randrange(32), rng.choice((7, 15, 23, 31))
            px[x, y] = pal.SAGE_DARK
            px[(x + 1) % 32, y] = pal.SAGE
    return img


def plaster_side():
    """32x20, one 2m x 1.2m course of a house wall: cream plaster between oak beams. A beam
    runs along the top and a post up the left edge (the next block's post is its right edge),
    with a diagonal brace, so stacked courses read as one timber-framed wall."""
    rng = random.Random(91)
    img = Image.new("RGB", (32, 20), PLASTER)
    px = img.load()
    for _ in range(10):  # a little texture in the plaster
        px[rng.randrange(3, 32), rng.randrange(3, 20)] = shade(PLASTER, rng.choice((-8, 6)))
    for x in range(32):
        px[x, 0], px[x, 1] = OAK, OAK_SHADE
    for y in range(20):
        px[0, y], px[1, y] = OAK, OAK_SHADE
    for i in range(14):  # the brace, two texels thick
        x, y = 4 + i * 2, 18 - i
        for dx in (0, 1):
            px[x + dx, y] = OAK
            if y + 1 < 20:
                px[x + dx, y + 1] = OAK_SHADE
    return img


def roof_tiles():
    """Terracotta tiles: rows of rounded tiles, each row offset by half a tile (like the
    castle's shingles, in warm clay)."""
    rng = random.Random(93)
    img = Image.new("RGB", (32, 32), TERRACOTTA)
    px = img.load()
    for row in range(8):
        y0 = row * 4
        off = 3 if row % 2 else 0
        for x in range(32):
            px[x, y0 + 3] = TERRACOTTA_DARK
            if (x + off) % 6 in (1, 2, 3):
                px[x, y0] = shade(TERRACOTTA_LIGHT, rng.randint(-6, 4))
            if (x + off) % 6 == 0:
                for y in range(y0, y0 + 3):
                    px[x, y] = TERRACOTTA_DARK
    return img


def thatch():
    """Straw thatch: short vertical strands in three tones, with a darker band where each
    layer of straw overlaps the one below."""
    rng = random.Random(95)
    img = Image.new("RGB", (32, 32), STRAW)
    px = img.load()
    for x in range(32):
        for y in range(32):
            if (x * 7 + y // 3 * 5) % 5 == 0:
                px[x, y] = STRAW_LIGHT
            elif (x * 3 + y // 4) % 7 == 0:
                px[x, y] = STRAW_DARK
    for y0 in (7, 15, 23, 31):
        for x in range(32):
            px[x, y0] = STRAW_DARK
            if rng.random() < 0.5:
                px[x, y0 - 1] = shade(STRAW_DARK, 12)
    return img


def window():
    """16x16, a 1m cottage window: four panes of sky-blue glass in an oak frame, a window box
    of flowers along the bottom, on cream plaster."""
    img = Image.new("RGB", (16, 16), PLASTER)
    px = img.load()
    for y in range(1, 13):
        for x in range(2, 14):
            edge = y in (1, 12) or x in (2, 13) or x in (7, 8) or y == 6
            px[x, y] = OAK if edge else (pal.WATER_GLINT if (x - y) in (0, 1) else pal.WATER)
    for x in range(1, 15):  # the window box
        px[x, 13], px[x, 14] = OAK, OAK_SHADE
    for x in range(2, 14, 2):
        px[x, 12] = pal.CORAL if x % 4 else pal.BUTTER
    return img


def lancet():
    """12x32, a tall pointed stained-glass window (0.75m x 2m) set in the cathedral's stone:
    bright panes in lead lines, glowing a little in game (emission)."""
    img = Image.new("RGB", (12, 32), BRICK)
    px = img.load()
    for y in range(2, 31):
        if y < 8:  # the pointed arch: narrowing toward the top
            half = 1 + (y - 2) * 4 / 6
        else:
            half = 5
        for x in range(12):
            if abs(x - 5.5) <= half:
                lead = (y % 6 == 2) or abs(x - 5.5) >= half - 0.5 or x == 6
                px[x, y] = LEAD if lead else GLASS[(y // 6 + (x > 5)) % len(GLASS)]
    return img


def rose_window():
    """24x24, the round rose window over the cathedral door (1.5m): eight petals of coloured
    glass around a golden middle, in two rings, set in a stone ring."""
    img = Image.new("RGB", (24, 24), BRICK)
    px = img.load()
    for y in range(24):
        for x in range(24):
            dx, dy = x + 0.5 - 12, y + 0.5 - 12
            d = math.hypot(dx, dy)
            if d > 12:
                continue
            petal = int((math.atan2(dy, dx) + math.pi) / (2 * math.pi) * 8) % 8
            if d > 10.5:
                px[x, y] = shade(BRICK, 24)        # the stone ring
            elif d > 9.5 or 5 < d < 6:
                px[x, y] = LEAD
            elif d >= 6:
                px[x, y] = GLASS[0] if petal % 2 else GLASS[4]   # outer petals: blue and lilac
            elif d > 2.5:
                px[x, y] = GLASS[1] if petal % 2 else GLASS[3]   # inner petals: coral and green
            else:
                px[x, y] = GLASS[2]                              # the golden middle
    return img


def town_door(wood=OAK):
    """16x32, a 1m x 2m plank door with an arched top, a little round window and a brass knob."""
    img = Image.new("RGB", (16, 32), shade(wood, -30))
    px = img.load()
    for y in range(32):
        for x in range(16):
            arch = y < 4 and (x - 7.5) ** 2 / 64 + (y - 4) ** 2 / 16 > 1
            if arch:
                continue
            px[x, y] = shade(wood, 14) if x % 4 == 1 else wood
            if x % 4 == 0:
                px[x, y] = shade(wood, -22)
    for y in (9, 24):  # iron straps
        for x in range(1, 15):
            px[x, y] = (80, 80, 92)
    for y in range(5, 9):
        for x in range(6, 10):
            px[x, y] = pal.WATER_GLINT if (x, y) == (6, 5) else pal.WATER
    px[12, 17], px[12, 18] = pal.HONEY_LIGHT, pal.HONEY
    return img


def awning():
    """32x16, the shop's striped canvas awning (2m x 1m): coral and cream stripes with a
    scalloped valance along the front edge."""
    img = Image.new("RGB", (32, 16), pal.CREAM)
    px = img.load()
    for y in range(16):
        for x in range(32):
            stripe = (x // 4) % 2 == 0
            c = pal.CORAL if stripe else pal.CREAM
            if y < 3:
                c = shade(c, 10)
            if y >= 12:  # the valance: scallops hanging from y = 12
                t = ((x % 4) + 0.5) / 4 * 2 - 1
                if y - 12 > round(3 * (1 - t * t) ** 0.5):
                    c = shade(c, -60)
                else:
                    c = shade(c, -14)
            px[x, y] = c
    return img


def shop_sign():
    """24x16, Barnaby's shop sign (1.5m x 1m): a cream board in a honey frame with a bottle of
    bubble bath and three bubbles, so you can read it before you can read."""
    img = Image.new("RGB", (24, 16), pal.HONEY)
    px = img.load()
    for y in range(1, 15):
        for x in range(1, 23):
            px[x, y] = pal.CREAM
    for y in range(15):
        px[0, y] = pal.HONEY_SHADE
    for x in range(24):
        px[x, 15] = pal.HONEY_SHADE
    lilac, lilac_dark, glass, cork = (196, 150, 226), (150, 104, 186), (238, 236, 250), (176, 128, 86)
    bottle = [  # rows 3..12 of the board, from x = 3: the bottle from the bubble bath icon, in small
        "...cc...",
        "...gg...",
        "..rrrr..",
        "..gggg..",
        ".gggggg.",
        "gLLLLLLg",
        "gLLLLLLg",
        "gLLLLLLg",
        ".dddddd.",
    ]
    colors = {"c": cork, "g": glass, "r": pal.CORAL, "L": lilac, "d": lilac_dark}
    for j, row in enumerate(bottle):
        for i, ch in enumerate(row):
            if ch != ".":
                px[3 + i, 3 + j] = colors[ch]
    for cx, cy, r in ((15.5, 5.5, 2.4), (19.5, 9.5, 1.8), (15.5, 11.5, 1.3)):  # bubbles
        for y in range(16):
            for x in range(24):
                d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
                if d <= r:
                    px[x, y] = (214, 232, 252) if d < r - 0.9 else (120, 168, 216)
        px[int(cx - r / 2), int(cy - r / 2)] = (255, 255, 255)
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
    rock_side(40, 31).save(OUT / "RockSide.png")
    rock_side(20, 32).save(OUT / "RockSideLow.png")
    rock_top().save(OUT / "RockTop.png")
    cave_floor().save(OUT / "CaveFloor.png")
    spike_plate().save(OUT / "SpikePlate.png")
    sand_tile(131, shell=False).save(OUT / "Sand_0.png")
    sand_tile(132, shell=True).save(OUT / "Sand_1.png")
    sea_tile().save(OUT / "Sea.png")
    sand_bank().save(OUT / "SandBank.png")
    planks().save(OUT / "Planks.png")
    waterfall().save(OUT / "Waterfall.png")
    cobble_tile(141, moss=False).save(OUT / "Cobble_0.png")
    cobble_tile(142, moss=True).save(OUT / "Cobble_1.png")
    plaster_side().save(OUT / "Plaster.png")
    roof_tiles().save(OUT / "RoofTiles.png")
    thatch().save(OUT / "Thatch.png")
    window().save(OUT / "Window.png")
    lancet().save(OUT / "Lancet.png")
    rose_window().save(OUT / "RoseWindow.png")
    town_door().save(OUT / "TownDoor.png")
    awning().save(OUT / "Awning.png")
    shop_sign().save(OUT / "ShopSign.png")
    print("Wrote", ", ".join(sorted(p.name for p in OUT.glob("*.png"))))


if __name__ == "__main__":
    main()
