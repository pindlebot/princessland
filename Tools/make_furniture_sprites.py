"""Generates the furniture for the hero's home (inside the castle).

Run:  Tools/.venv/bin/python Tools/make_furniture_sprites.py
Out:  Assets/Art/Furniture.png / Furniture.json   (64x64 frames, bottom pivot, 1 frame each)
        Bed, Toilet, Sink, PaperTowel, Door, Wardrobe, Nightstand, Bookshelf, ToyChest, Plant
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
LAVENDER, LAVENDER_SH = (176, 150, 214, 255), (132, 108, 176, 255)
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
    c.rect(28, 42, 35, 61, PORCELAIN)  # pedestal
    c.rect(34, 42, 35, 61, PORCELAIN_SH)
    c.rect(26, 61, 37, 62, PORCELAIN_SH)
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
          ("ToyChest", toy_chest), ("Plant", plant)]],
        frame_size=S,
    )
    # The rug lies on the floor, so it gets its own sheet with a centre pivot and no outline.
    write_sheet("Rug", None, [("Rug", 1, False, [rug()])], pivot="center", outline_color=None, frame_size=S)
