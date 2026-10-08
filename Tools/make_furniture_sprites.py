"""Generates the furniture for the hero's home (inside the castle).

Run:  Tools/.venv/bin/python Tools/make_furniture_sprites.py
Out:  Assets/Art/Furniture.png / Furniture.json   (64x64 frames, bottom pivot, 1 frame each)
        Bed, Toilet, Sink, PaperTowel, Door

All drawn front-on in the same 3/4 style as the chest: lit from the top-left, dark outline.
64px frames because the bed is wide; the small fixtures just use part of the frame.
"""
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


if __name__ == "__main__":
    write_sheet(
        "Furniture",
        None,
        [(name, 1, False, [draw()]) for name, draw in
         [("Bed", bed), ("Toilet", toilet), ("Sink", sink), ("PaperTowel", paper_towel), ("Door", door)]],
        frame_size=S,
    )
