"""Generates the furniture for the hero's home (inside the castle).

Run:  Tools/.venv/bin/python Tools/make_furniture_sprites.py
Out:  Assets/Art/Furniture.png / Furniture.json   (64x64 frames, bottom pivot, 1 frame each)
        Bed, Toilet, Sink (with a pump of hand soap), PaperTowel, Door, Wardrobe, Nightstand, Bookshelf,
        ToyChest, Plant (the bath and its bubbles are in make_bath_sprites.py)
        SpiralDown (the bedroom's spiral staircase, winding down to the kitchen), SpiralUp (its foot,
        in the kitchen, winding back up), Stove, Pantry, Island (the kitchen's island counter),
        TowelShelf (an open shelf of folded towels, by the bath)
      Assets/Art/Rug.png / Rug.json                 (the bedroom rug, seen from above: centre pivot)

All drawn front-on in the same 3/4 style as the chest: lit from the top-left, dark outline.
64px frames because the bed is wide; the small fixtures just use part of the frame.
"""
import palette
from sprite_common import Canvas, write_sheet

S = 64

WOOD, WOOD_SH, WOOD_HI = (128, 80, 42, 255), (86, 50, 26, 255), (160, 106, 58, 255)
SHEET, SHEET_SH = (240, 240, 232, 255), (200, 200, 196, 255)
BLANKET, BLANKET_SH, BLANKET_HI = (64, 196, 180, 255), (34, 136, 134, 255), (132, 232, 214, 255)
GOLD = (236, 192, 70, 255)
PORCELAIN, PORCELAIN_SH, PORCELAIN_HI = (236, 240, 244, 255), (180, 188, 200, 255), (255, 255, 255, 255)
METAL, METAL_HI = (150, 156, 170, 255), (210, 216, 228, 255)
WATER = (120, 200, 240, 255)
GLASS, GLASS_HI = (150, 200, 220, 255), (220, 240, 248, 255)
DARK = (40, 30, 40, 255)
STONE, STONE_SH = (128, 122, 138, 255), (86, 82, 98, 255)
CORAL, CORAL_HI = palette.rgba(palette.CORAL), palette.rgba(palette.CORAL_LIGHT)
CORAL_SH = (196, 82, 104, 255)
BUTTER, BUTTER_HI = palette.rgba(palette.BUTTER), palette.rgba(palette.BUTTER_LIGHT)
TURQ, TURQ_HI = palette.rgba(palette.TURQUOISE), palette.rgba(palette.TURQUOISE_LIGHT)
LEAF, LEAF_SH, LEAF_HI = palette.rgba(palette.SAGE_DARK), palette.rgba(palette.SAGE_DEEP), palette.rgba(palette.SAGE_LIGHT)
LAVENDER, LAVENDER_SH, LAVENDER_HI = (176, 150, 214, 255), (132, 108, 176, 255), (214, 196, 240, 255)
TEDDY, TEDDY_SH = (196, 140, 88, 255), (150, 100, 60, 255)


def bed():
    """A big four-poster-ish bed seen from its foot: headboard, pillows, aquamarine quilt."""
    c = Canvas(S)
    c.rect(8, 16, 55, 34, WOOD_SH)  # headboard
    c.rect(10, 18, 53, 32, WOOD)
    c.rect(10, 18, 53, 18, WOOD_HI)
    for x in (6, 56):  # posts with gold finials
        c.rect(x, 12, x + 2, 60, WOOD)
        c.rect(x, 12, x, 60, WOOD_HI)
        c.rect(x, 10, x + 2, 11, GOLD)
    c.rect(12, 28, 30, 35, SHEET)  # two pillows
    c.rect(33, 28, 51, 35, SHEET)
    c.rect(12, 35, 51, 35, SHEET_SH)
    c.rect(9, 36, 54, 50, BLANKET)  # quilt, folded back at the top
    c.rect(9, 36, 54, 38, SHEET)
    c.rect(9, 39, 54, 39, BLANKET_HI)
    for x in range(14, 54, 8):  # quilt stitching
        c.rect(x, 41, x, 49, BLANKET_SH)
    c.rect(9, 50, 54, 51, BLANKET_SH)
    c.rect(8, 52, 55, 58, WOOD)  # footboard
    c.rect(8, 52, 55, 52, WOOD_HI)
    c.rect(8, 58, 55, 58, WOOD_SH)
    for x in (10, 52):  # little feet
        c.rect(x, 59, x + 1, 62, WOOD_SH)
    return c.img


def toilet():
    c = Canvas(S)
    c.rect(24, 30, 39, 42, PORCELAIN)  # cistern
    c.rect(24, 30, 39, 30, PORCELAIN_HI)
    c.rect(39, 30, 39, 42, PORCELAIN_SH)
    c.rect(36, 33, 38, 34, METAL)  # flush handle
    c.ellipse(31.5, 48, 10, 5, PORCELAIN)  # bowl
    c.ellipse(31.5, 47, 7, 3, PORCELAIN_SH)  # seat opening
    c.ellipse(31.5, 47, 5, 2, WATER)
    c.rect(26, 52, 37, 61, PORCELAIN)  # pedestal
    c.rect(36, 52, 37, 61, PORCELAIN_SH)
    c.rect(24, 61, 39, 62, PORCELAIN_SH)
    return c.img


def sink():
    c = Canvas(S)
    c.rect(22, 8, 41, 28, METAL)  # mirror on the wall above
    c.rect(24, 10, 39, 26, GLASS)
    c.line(26, 24, 36, 12, GLASS_HI)
    c.rect(20, 34, 43, 41, PORCELAIN)  # basin
    c.rect(20, 34, 43, 34, PORCELAIN_HI)
    c.rect(22, 35, 41, 37, PORCELAIN_SH)
    c.rect(23, 35, 40, 36, WATER)
    c.rect(30, 30, 33, 33, METAL)  # tap
    c.rect(31, 33, 32, 34, METAL_HI)
    c.rect(22, 29, 25, 33, LAVENDER)  # a pump bottle of lavender hand soap on the basin's edge
    c.rect(22, 29, 22, 33, LAVENDER_HI)
    c.rect(25, 29, 25, 33, LAVENDER_SH)
    c.rect(23, 31, 24, 32, PORCELAIN_HI)  # its label
    c.rect(23, 27, 24, 28, METAL)  # the pump, its nozzle pointing at the tap
    c.rect(25, 27, 26, 27, METAL)
    c.dot(23, 26, METAL_HI)
    c.rect(28, 42, 35, 61, PORCELAIN)  # pedestal
    c.rect(34, 42, 35, 61, PORCELAIN_SH)
    c.rect(26, 61, 37, 62, PORCELAIN_SH)
    return c.img


TOWELS = [TURQ, CORAL, (236, 228, 210, 255), LAVENDER, BUTTER]


def towel_shelf():
    """An open shelf unit (no doors) stacked with fluffy folded towels in soft colours, rolled
    hand towels on top, and a wicker basket at the bottom."""
    c = Canvas(S)
    c.rect(14, 10, 49, 61, WOOD_SH)                       # the frame and back
    c.rect(16, 12, 47, 59, (176, 132, 92, 255))
    for y in (24, 38, 52):                                # shelves
        c.rect(14, y, 49, y + 2, WOOD)
        c.rect(14, y, 49, y, WOOD_HI)
    for shelf, y_top in enumerate((13, 27, 41)):          # folded towels, two stacks a shelf
        for k, x0 in enumerate((17, 32)):
            for j in range(3):
                col = TOWELS[(shelf * 2 + k + j) % len(TOWELS)]
                y = y_top + 7 - j * 3
                c.rect(x0, y, x0 + 13, y + 2, col)
                c.rect(x0, y, x0 + 13, y, PORCELAIN_HI)
    for x0, col in ((16, CORAL), (26, TURQ), (36, LAVENDER)):  # rolled hand towels on top
        c.ellipse(x0 + 4, 7, 4, 2.5, col)
        c.ellipse(x0 + 2, 7, 1.2, 1.2, PORCELAIN_HI)
    c.rect(18, 54, 45, 59, (196, 160, 104, 255))          # the wicker basket
    for x in range(19, 45, 3):
        c.rect(x, 54, x, 59, (160, 124, 80, 255))
    c.rect(18, 54, 45, 54, (226, 196, 140, 255))
    return c.img


IRON, IRON_SH, IRON_HI = (66, 60, 76, 255), (44, 40, 52, 255), (108, 102, 120, 255)
FIRE, FIRE_HI = (255, 150, 60, 255), (255, 226, 140, 255)
STEP, STEP_SH, STEP_HI = (150, 144, 160, 255), (110, 104, 124, 255), (186, 180, 198, 255)
VOID = (34, 24, 38, 255)


def spiral(down):
    """A stone spiral staircase with a wooden banister. down: seen from the bedroom, a round
    stairwell in the floor whose wedge steps wind down into the dark around a centre post.
    Otherwise (the kitchen end): the steps wind up around the post and out of sight."""
    import math
    c = Canvas(S)
    cx = 31.5
    if down:
        c.ellipse(cx, 48, 24, 11, STONE_SH)              # the rim of the stairwell
        c.ellipse(cx, 47, 22, 9.5, VOID)
        for k in range(10):                               # wedge steps, darker as they go down
            a0 = math.radians(200 + k * 32)
            depth = k * 1.1
            shade = tuple(max(0, round(v * (1.1 - k * 0.06))) for v in STEP_HI[:3]) + (255,)
            for r in range(5, 21):
                for da in range(0, 30, 3):
                    a = a0 + math.radians(da)
                    x, y = cx + math.cos(a) * r, 47 + math.sin(a) * r * 0.42 + depth
                    if (x - cx) ** 2 / 22 ** 2 + (y - 47) ** 2 / 9.5 ** 2 <= 1:
                        c.dot(round(x), round(y), shade if da < 27 else STEP_SH)
        c.rect(30, 38, 33, 56, STEP_SH)                    # the centre post, down into the dark
        c.rect(30, 38, 30, 56, STEP_HI)
        for i in range(40):                                # the banister curving round the rim
            a = math.radians(160 + i * 5.5)
            x, y = cx + math.cos(a) * 23, 47 + math.sin(a) * 10 - 12
            c.dot(round(x), round(y), WOOD)
            c.dot(round(x), round(y) + 1, WOOD_SH)
            if i % 10 == 0:                                # balusters down to the rim
                c.rect(round(x), round(y) + 2, round(x), round(47 + math.sin(a) * 10), WOOD_SH)
        c.rect(7, 24, 9, 46, WOOD)                         # the newel post, with a gold knob
        c.ellipse(8, 23, 2, 2, GOLD)
    else:
        # Wedge steps on a helix round the centre post, climbing up and out of the frame: the
        # ones behind the post first, then the post, then the ones in front.
        steps = []
        for k in range(12):
            a = math.radians(90 + k * 38)
            steps.append((math.sin(a) >= 0, k, a, 58 - k * 4.6))
        def wedge(k, a, y):
            for r in range(3, 20):
                for da in range(0, 26, 2):
                    aa = a + math.radians(da)
                    x, yy = cx + math.cos(aa) * r, y + math.sin(aa) * r * 0.38
                    edge = da >= 24 or r == 19
                    c.rect(round(x), round(yy), round(x), round(yy) + 2, STEP_SH if edge else (STEP_HI if da < 6 else STEP))
        for front, k, a, y in steps:
            if not front:
                wedge(k, a, y)
        c.rect(30, 0, 33, 61, STONE)
        c.rect(32, 0, 33, 61, STONE_SH)
        for front, k, a, y in steps:
            if front:
                wedge(k, a, y)
        prev = None
        for i in range(70):                                # the banister, following the outer edge
            t = i / 69 * 11
            a = math.radians(90 + t * 38 + 12)
            x, y = cx + math.cos(a) * 20, 58 - t * 4.6 + math.sin(a) * 20 * 0.38 - 9
            if prev and math.sin(a) >= 0:
                c.line(prev[0], prev[1], round(x), round(y), WOOD)
            prev = (round(x), round(y))
            if i % 9 == 0 and math.sin(a) >= 0:
                c.rect(round(x), round(y), round(x), round(y) + 8, WOOD_SH)
        c.rect(8, 44, 10, 62, WOOD)                        # the newel post at the bottom
        c.ellipse(9, 43, 2, 2, GOLD)
    return c.img


def stove():
    """A black iron kitchen range: oven door, firebox glowing, a pot of soup on top with steam,
    and its stovepipe going up."""
    c = Canvas(S)
    c.rect(12, 34, 51, 60, IRON)
    c.rect(12, 34, 51, 35, IRON_HI)
    c.rect(50, 34, 51, 60, IRON_SH)
    c.rect(16, 42, 31, 56, IRON_SH)                     # the oven door
    c.rect(18, 44, 29, 54, IRON)
    c.rect(22, 47, 26, 48, METAL_HI)                     # its handle
    c.rect(35, 46, 47, 52, IRON_SH)                     # the firebox, glowing
    c.rect(36, 49, 46, 51, FIRE)
    for x in (38, 42, 45):
        c.dot(x, 48, FIRE_HI)
    for x in (13, 49):                                   # little feet
        c.rect(x, 60, x + 2, 62, IRON_SH)
    c.rect(44, 6, 47, 33, IRON)                          # the stovepipe
    c.rect(44, 6, 44, 33, IRON_HI)
    c.ellipse(26, 32, 9, 2.5, METAL)                     # a pot of soup
    c.rect(17, 24, 35, 32, METAL)
    c.rect(17, 24, 18, 32, METAL_HI)
    c.ellipse(26, 24, 9, 2, (214, 120, 70, 255))         # carrot-coloured soup
    c.rect(14, 26, 16, 27, METAL)                        # handles
    c.rect(36, 26, 38, 27, METAL)
    for x, y in ((22, 18), (27, 14), (31, 19), (24, 10)):  # steam
        c.dot(x, y, PORCELAIN_HI)
        c.dot(x + 1, y - 1, PORCELAIN_SH)
    return c.img


def pantry():
    """A tall pantry cupboard with its doors open: shelves of jam jars, a wheel of cheese, a
    loaf, apples, a sack of flour at the bottom."""
    c = Canvas(S)
    c.rect(14, 6, 49, 61, WOOD_SH)
    c.rect(16, 8, 47, 59, (96, 60, 34, 255))             # the inside, in shadow
    for y in (22, 36, 50):                               # shelves
        c.rect(16, y, 47, y + 1, WOOD_HI)
    for x in (19, 24, 29):                               # jam jars
        c.rect(x, 15, x + 3, 21, CORAL)
        c.rect(x, 14, x + 3, 14, PORCELAIN)
        c.dot(x, 16, CORAL_HI)
    c.ellipse(40, 18, 5, 3.5, BUTTER)                    # a wheel of cheese
    c.rect(36, 18, 44, 21, BUTTER)
    c.dot(38, 16, BUTTER_HI)
    c.ellipse(24, 32, 7, 3, (196, 140, 80, 255))         # a loaf
    c.rect(19, 30, 22, 30, (226, 180, 120, 255))
    for x in (36, 40, 44):                               # apples
        c.ellipse(x, 33, 2, 2, CORAL_SH if x == 40 else CORAL)
    c.rect(19, 41, 28, 49, PORCELAIN)                    # a sack of flour
    c.rect(21, 39, 26, 40, PORCELAIN_SH)
    c.rect(36, 44, 44, 49, LAVENDER)                     # a crock of lavender honey
    c.rect(36, 44, 44, 44, LAVENDER_HI)
    for x0 in (6, 49):                                   # the two doors, swung open
        c.rect(x0, 7, x0 + 8, 60, WOOD)
        c.rect(x0 + 1, 10, x0 + 7, 30, WOOD_HI)
        c.rect(x0 + 1, 34, x0 + 7, 57, WOOD_HI)
        c.rect(x0 + 2, 11, x0 + 6, 29, WOOD)
        c.rect(x0 + 2, 35, x0 + 6, 56, WOOD)
    c.rect(12, 4, 51, 6, WOOD)                           # the top
    return c.img


def island():
    """The kitchen island: a wide butcher-block counter on cupboards, with a bowl of fruit, a
    loaf of bread, a rolling pin and a dusting of flour."""
    c = Canvas(S)
    c.rect(4, 38, 59, 60, WOOD)                          # cupboards
    c.rect(4, 38, 59, 39, WOOD_SH)
    for x in (6, 24, 42):
        c.rect(x, 42, x + 15, 58, WOOD_HI)
        c.rect(x + 1, 43, x + 14, 57, WOOD)
        c.dot(x + 13, 50, GOLD)
    c.rect(2, 32, 61, 37, (196, 150, 100, 255))          # the butcher-block top
    c.rect(2, 32, 61, 32, (226, 186, 132, 255))
    for x in range(6, 60, 6):
        c.rect(x, 33, x, 37, (170, 124, 82, 255))
    c.ellipse(15, 29, 7, 3, PORCELAIN)                    # a bowl of fruit
    c.ellipse(12, 27, 2.2, 2.2, CORAL)
    c.ellipse(16, 26, 2.2, 2.2, BUTTER)
    c.ellipse(19, 27, 2, 2, (120, 180, 90, 255))
    c.ellipse(35, 29, 7, 3.4, (196, 140, 80, 255))        # a loaf of bread
    c.rect(31, 27, 39, 27, (226, 180, 120, 255))
    c.rect(44, 30, 56, 31, WOOD_HI)                       # a rolling pin
    c.rect(42, 30, 43, 31, WOOD_SH)
    c.rect(57, 30, 58, 31, WOOD_SH)
    for x, y in ((24, 31), (27, 30), (48, 29), (52, 31)):  # flour
        c.dot(x, y, PORCELAIN_HI)
    return c.img


def paper_towel():
    """A standing holder with a roll of paper towel, one sheet hanging down."""
    c = Canvas(S)
    c.ellipse(31.5, 61, 7, 2, METAL)  # base
    c.rect(31, 32, 32, 60, METAL)  # pole
    c.rect(31, 32, 31, 60, METAL_HI)
    c.rect(22, 26, 41, 37, PORCELAIN)  # the roll
    c.rect(22, 26, 41, 26, PORCELAIN_HI)
    c.rect(22, 37, 41, 37, PORCELAIN_SH)
    for x in range(25, 41, 5):
        c.rect(x, 27, x, 36, PORCELAIN_SH)  # perforations
    c.rect(20, 30, 21, 33, METAL)  # end caps
    c.rect(42, 30, 43, 33, METAL)
    c.rect(34, 37, 40, 46, PORCELAIN)  # hanging sheet
    for x in (34, 36, 38, 40):
        c.dot(x, 47, PORCELAIN)  # torn, wavy edge
    return c.img


def door():
    """The house's front door: a rounded oak door in a stone frame."""
    c = Canvas(S)
    c.rect(18, 26, 45, 62, STONE)  # stone frame
    c.ellipse(31.5, 26, 13.5, 8, STONE)
    c.rect(21, 28, 42, 62, WOOD)  # door
    c.ellipse(31.5, 28, 10.5, 6, WOOD)
    for x in (26, 31, 36):
        c.rect(x, 24, x, 62, WOOD_SH)  # planks
    c.rect(21, 28, 21, 62, WOOD_HI)
    for y in (34, 52):
        c.rect(21, y, 42, y, METAL)  # iron bands
    c.rect(38, 43, 39, 44, GOLD)  # handle
    c.rect(18, 62, 45, 62, STONE_SH)
    return c.img


def wardrobe():
    """A tall two-door wardrobe with a little crown carved on top."""
    c = Canvas(S)
    c.rect(18, 22, 45, 62, WOOD_SH)  # body
    c.rect(19, 23, 44, 60, WOOD)
    c.rect(19, 23, 44, 23, WOOD_HI)
    for x0 in (21, 33):  # two doors, each with a panel
        c.rect(x0, 26, x0 + 9, 57, WOOD_HI)
        c.rect(x0 + 1, 27, x0 + 9, 57, WOOD)
        c.rect(x0 + 2, 29, x0 + 7, 40, WOOD_SH)
        c.rect(x0 + 2, 44, x0 + 7, 55, WOOD_SH)
    c.rect(31, 39, 31, 41, GOLD)  # knobs
    c.rect(33, 39, 33, 41, GOLD)
    c.rect(16, 20, 47, 22, WOOD_HI)  # cornice
    c.rect(16, 22, 47, 22, WOOD_SH)
    for x, y in ((27, 19), (31, 17), (32, 17), (36, 19)):  # crown
        c.dot(x, y, GOLD)
    c.rect(27, 18, 36, 19, GOLD)
    c.dot(31, 18, CORAL)
    c.dot(32, 18, CORAL)
    for x in (19, 42):  # feet
        c.rect(x, 61, x + 2, 62, WOOD_SH)
    return c.img


def nightstand():
    """A little bedside table with a lamp (a pink shade) and a book."""
    c = Canvas(S)
    c.rect(22, 48, 41, 62, WOOD_SH)  # table
    c.rect(23, 49, 40, 60, WOOD)
    c.rect(22, 47, 41, 48, WOOD_HI)
    c.rect(25, 52, 38, 56, WOOD_SH)  # drawer
    c.rect(31, 54, 32, 54, GOLD)
    c.rect(36, 44, 40, 46, LAVENDER)  # a book lying on top
    c.rect(36, 46, 40, 46, LAVENDER_SH)
    c.rect(28, 42, 31, 46, PORCELAIN_SH)  # lamp base and stem
    c.rect(29, 36, 30, 42, METAL)
    c.rect(24, 30, 35, 36, CORAL)  # shade
    c.rect(25, 29, 34, 29, CORAL)
    c.rect(24, 36, 35, 36, CORAL_SH)
    c.rect(25, 30, 26, 35, CORAL_HI)
    return c.img


def bookshelf():
    """A bookshelf full of colourful books, with a teddy bear sitting on top."""
    c = Canvas(S)
    c.rect(18, 26, 45, 62, WOOD_SH)  # case
    c.rect(19, 27, 44, 61, WOOD)
    colours = [CORAL, TURQ, BUTTER, LAVENDER, LEAF_HI, CORAL_HI, TURQ_HI]
    for shelf, y in enumerate((29, 40, 51)):
        c.rect(21, y, 42, y + 9, WOOD_SH)  # the back of the shelf
        x, i = 21, shelf * 2
        while x < 42:
            w = 2 + (i % 2)
            h = 7 + (i % 3) - 1
            col = colours[i % len(colours)]
            c.rect(x, y + 9 - h, min(x + w - 1, 42), y + 9, col)
            c.rect(x, y + 9 - h, x, y + 9, WOOD_SH) if w > 2 else None
            x += w + (1 if i % 4 == 3 else 0)
            i += 1
        c.rect(19, y + 10, 44, y + 10, WOOD_HI)  # shelf board
    c.rect(18, 26, 45, 26, WOOD_HI)
    # Teddy bear on top
    c.ellipse(31.5, 21, 5, 4.5, TEDDY)  # body
    c.ellipse(31.5, 14, 4, 3.5, TEDDY)  # head
    c.dot(28, 11, TEDDY_SH)  # ears
    c.dot(35, 11, TEDDY_SH)
    c.rect(28, 10, 28, 11, TEDDY)
    c.rect(35, 10, 35, 11, TEDDY)
    c.dot(30, 14, DARK)  # eyes
    c.dot(33, 14, DARK)
    c.rect(31, 15, 32, 16, TEDDY_SH)  # snout
    c.rect(29, 19, 34, 19, CORAL)  # a bow
    c.rect(26, 24, 28, 25, TEDDY_SH)  # paws
    c.rect(35, 24, 37, 25, TEDDY_SH)
    return c.img


def toy_chest():
    """A coral toy box with a gold star, a ball and a duck peeking out over the rim."""
    c = Canvas(S)
    c.ellipse(26, 41, 4, 4, TURQ)  # a ball
    c.rect(25, 39, 26, 39, TURQ_HI)
    c.ellipse(37, 42, 3, 2.5, BUTTER)  # a rubber duck
    c.ellipse(39, 39, 2, 2, BUTTER)
    c.dot(40, 38, DARK)
    c.rect(41, 39, 42, 39, CORAL)
    c.rect(18, 44, 45, 62, CORAL_SH)  # box
    c.rect(19, 45, 44, 60, CORAL)
    c.rect(18, 43, 45, 45, CORAL_HI)  # rim
    c.rect(19, 46, 19, 60, CORAL_HI)
    for x, y in ((31, 49), (32, 49), (30, 51), (31, 51), (32, 51), (33, 51), (29, 52), (34, 52),
                 (31, 50), (32, 50), (30, 52), (31, 52), (32, 52), (33, 52), (30, 53), (33, 53),
                 (31, 53), (32, 53), (30, 54), (33, 54), (29, 55), (34, 55)):
        c.dot(x, y, GOLD)  # star
    for x in (20, 42):
        c.rect(x, 61, x + 1, 62, CORAL_SH)
    return c.img


def plant():
    """A leafy plant in a terracotta pot."""
    c = Canvas(S)
    for (x0, y0, x1, y1) in ((31, 52, 22, 36), (32, 52, 41, 35), (31, 52, 27, 30), (32, 52, 36, 29), (31, 52, 31, 33)):
        c.line(x0, y0, x1, y1, LEAF_SH)
    for cx, cy, rx, ry in ((23, 37, 3, 2), (40, 36, 3, 2), (27, 31, 2, 3), (36, 30, 2, 3), (31, 33, 2, 3),
                           (26, 43, 3, 2), (37, 42, 3, 2)):
        c.ellipse(cx, cy, rx, ry, LEAF)
        c.dot(cx - 1, cy - 1, LEAF_HI)
    c.rect(25, 51, 38, 53, CORAL_SH)  # pot rim
    c.rect(26, 54, 37, 62, (190, 110, 74, 255))
    c.rect(26, 54, 27, 62, (214, 136, 96, 255))
    c.rect(26, 62, 37, 62, CORAL_SH)
    return c.img


def rug():
    """The bedroom rug, seen from above: a soft lavender rectangle with a scalloped cream
    border and a turquoise four-point star in the middle (centre pivot, 4 x 3 units)."""
    c = Canvas(S)
    x0, y0, x1, y1 = 4, 10, 59, 53
    c.rect(x0, y0, x1, y1, LAVENDER_SH)
    c.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, LAVENDER)
    c.rect(x0 + 3, y0 + 3, x1 - 3, y0 + 3, PORCELAIN)  # inner border
    c.rect(x0 + 3, y1 - 3, x1 - 3, y1 - 3, PORCELAIN)
    c.rect(x0 + 3, y0 + 3, x0 + 3, y1 - 3, PORCELAIN)
    c.rect(x1 - 3, y0 + 3, x1 - 3, y1 - 3, PORCELAIN)
    for x in range(x0 + 6, x1 - 4, 4):  # little dots along the border
        c.dot(x, y0 + 5, CORAL_HI)
        c.dot(x, y1 - 5, CORAL_HI)
    cx, cy = 31, 31
    for r in range(0, 9):  # a four-point star
        w = max(0, 2 - r // 3)
        c.rect(cx - w, cy - r, cx + 1 + w, cy - r, TURQ)
        c.rect(cx - w, cy + 1 + r, cx + 1 + w, cy + 1 + r, TURQ)
        c.rect(cx - r, cy - w, cx - r, cy + 1 + w, TURQ)
        c.rect(cx + 1 + r, cy - w, cx + 1 + r, cy + 1 + w, TURQ)
    c.rect(cx, cy, cx + 1, cy + 1, TURQ_HI)
    for x in range(x0, x1 + 1, 3):  # tassels
        c.dot(x, y0 - 1, PORCELAIN_SH)
        c.dot(x, y1 + 1, PORCELAIN_SH)
    return c.img


if __name__ == "__main__":
    write_sheet(
        "Furniture",
        None,
        [(name, 1, False, [draw()]) for name, draw in
         [("Bed", bed), ("Toilet", toilet), ("Sink", sink), ("PaperTowel", paper_towel), ("Door", door),
          ("Wardrobe", wardrobe), ("Nightstand", nightstand), ("Bookshelf", bookshelf),
          ("ToyChest", toy_chest), ("Plant", plant)]]
        + [(name, 1, False, [draw()]) for name, draw in
           [("SpiralDown", lambda: spiral(True)), ("SpiralUp", lambda: spiral(False)),
            ("Stove", stove), ("Pantry", pantry), ("Island", island), ("TowelShelf", towel_shelf)]],
        frame_size=S,
    )
    # The rug lies on the floor, so it gets its own sheet with a centre pivot and no outline.
    write_sheet("Rug", None, [("Rug", 1, False, [rug()])], pivot="center", outline_color=None, frame_size=S)
