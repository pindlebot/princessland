"""Generates the equipment items: how each looks lying on the floor, and its inventory icon.

Run:  Tools/.venv/bin/python Tools/make_item_sprites.py
Out:  Assets/Art/Items.png / Items.json   (32x32 frames, bottom pivot)
        PlumedHelm        2 frames, looping  a steel helm with a coral plume, glinting
        SeashellMail      2 frames, looping  a tunic of aquamarine shell scales with a pearl
        StarlightWand     2 frames, looping  a wooden wand tipped with a gold star
        TrailblazerBoots  2 frames, looping  a pair of leather boots with cream cuffs
        BubbleBath        2 frames, looping  a round bottle of lilac bubble bath with a cork and a bow
                                             (sold at Barnaby's stall in the village)
        Egg, Flour, Strawberry               cooking ingredients (the hens' coop, the pantry, the fruit bowl)
        Pancakes          2 frames, looping  strawberry pancakes: what the stove cooks from them
        AppleCider        2 frames, looping  a mug of hot apple cider (Pippin's cider stand at Hollow Farm)
        PumpkinHat        2 frames, looping  the Jack-o'-Lantern Hat (at the centre of the farm's corn maze)
      Assets/Art/UI/Icon<Item>.png   24x24  the same items drawn bigger for the inventory

Each item is drawn twice, small for the floor and big for the icon, rather than scaled, so
every pixel stays the same size as the rest of the art. (The Ember Ring lives in
make_prop_sprites.py and make_hud_sprites.py.)
"""
from PIL import Image

import palette as pal
from sprite_common import ART, CLEAR, Canvas, outline, write_sheet

UI = ART / "UI"
GLINT = (255, 255, 230, 255)

STEEL, STEEL_HI, STEEL_SH = pal.rgba(pal.STONE_LIGHT), (238, 236, 246, 255), pal.rgba(pal.STONE_SHADE)
VISOR = (52, 40, 66, 255)
PLUME, PLUME_SH = pal.rgba(pal.CORAL), (196, 76, 98, 255)
GOLD, GOLD_HI = pal.rgba(pal.HONEY), pal.rgba(pal.HONEY_LIGHT)
SCALE, SCALE_HI, SCALE_SH = pal.rgba(pal.TURQUOISE), pal.rgba(pal.TURQUOISE_LIGHT), (34, 140, 150, 255)
PEARL = pal.rgba(pal.CREAM)
STAR, STAR_HI = pal.rgba(pal.BUTTER), pal.rgba(pal.BUTTER_LIGHT)
WOOD, WOOD_HI = (128, 80, 42, 255), (170, 116, 66, 255)
LEATHER, LEATHER_HI, LEATHER_SH = (150, 92, 54, 255), (186, 124, 74, 255), (104, 62, 38, 255)
SOLE = (78, 52, 44, 255)
CUFF = pal.rgba(pal.CREAM_SHADE)


def pattern(rows, colors):
    """Pixel art from strings: each character is a colour key ('.' = transparent)."""
    assert len({len(r) for r in rows}) == 1, f"ragged pattern: {[len(r) for r in rows]}"
    img = Image.new("RGBA", (len(rows[0]), len(rows)), CLEAR)
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".":
                px[x, y] = colors[ch]
    return img


def place(canvas, img, x, y):
    canvas.img.alpha_composite(img, (x, y))
    canvas.px = canvas.img.load()


def scaled(mask_rows, extra=None):
    """A silhouette ('X') filled with overlapping shell scales: rows of little arcs, each row
    shifted by half a scale, lit from the top-left. `extra` colours other characters."""
    colors = {"X": SCALE}
    colors.update(extra or {})
    img = pattern(mask_rows, colors)
    px = img.load()
    for y, row in enumerate(mask_rows):
        for x, ch in enumerate(row):
            if ch != "X":
                continue
            phase = (x + (y // 2 % 2) * 2) % 4
            if y % 2 == 1 and phase == 3:
                px[x, y] = SCALE_SH  # the bottom edge of a scale
            elif y % 2 == 0 and phase in (1, 2):
                px[x, y] = SCALE_HI  # its shiny top
            if x == row.index("X"):
                px[x, y] = SCALE_HI  # the left side catches the light...
            elif x == row.rindex("X"):
                px[x, y] = SCALE_SH  # ...the right side doesn't
    return img


# ---------- Plumed Helm ----------

HELM_COLORS = {"P": PLUME, "p": PLUME_SH, "G": GOLD, "H": STEEL, "h": STEEL_HI, "s": STEEL_SH, "V": VISOR}


def helm(big):
    if big:
        return pattern([
            "......pPPp......",
            ".....pPPPPp.....",
            ".......PP.......",
            "......GGGG......",
            "....hhHHHHHs....",
            "...hHHHHHHHHs...",
            "..hHHHHHHHHHHs..",
            "..hHHHHHHHHHHs..",
            ".hHHHHHHHHHHHHs.",
            ".GGGGGGGGGGGGGG.",
            ".HHVVVVVVVVVVHs.",
            ".hHHHHHVVHHHHHs.",
            ".hHHHHHVVHHHHHs.",
            ".hHHHHHHHHHHHHs.",
            "..hHHHHHHHHHHs..",
            "...ssssssssss...",
        ], HELM_COLORS)
    return pattern([
        "...pPP....",
        "....GG....",
        "..hHHHHs..",
        ".hHHHHHHs.",
        ".GGGGGGGG.",
        ".HVVVVVVs.",
        ".hHHVVHHs.",
        "..ssssss..",
    ], HELM_COLORS)


# ---------- Seashell Mail ----------

def armor(big):
    extra = {"G": GOLD, "W": PEARL}
    if big:
        return scaled([
            "...XXXX....XXXX...",
            "..XXXXXX..XXXXXX..",
            ".XXXXXXXXXXXXXXXX.",
            "XXXXXXXXXXXXXXXXXX",
            "XXXXXXXXWWXXXXXXXX",
            "XXX.XXXXWWXXXX.XXX",
            "XX..XXXXXXXXXX..XX",
            "....XXXXXXXXXX....",
            "....XXXXXXXXXX....",
            "....XXXXXXXXXX....",
            "....XXXXXXXXXX....",
            "....XXXXXXXXXX....",
            "....GGGGGGGGGG....",
            "....XXXXXXXXXX....",
            "...XXXXXXXXXXXX...",
            "...XXXXXXXXXXXX...",
        ], extra)
    return scaled([
        ".XXX...XXX.",
        "XXXXXXXXXXX",
        "XXXXXWXXXXX",
        "XX.XXXXX.XX",
        "...XXXXX...",
        "...XXXXX...",
        "...GGGGG...",
        "..XXXXXXX..",
    ], extra)


# ---------- Starlight Wand ----------

STAR_COLORS = {"Y": STAR, "W": STAR_HI}


def wand(big):
    """A shaft running up to the right, a gold band, and a star on the end."""
    c = Canvas(24)
    if big:
        for i in range(11):  # two pixels thick: a lit edge and a shaded one
            c.dot(3 + i, 20 - i, WOOD_HI)
            c.dot(4 + i, 20 - i, WOOD)
        c.rect(12, 10, 13, 11, GOLD)
        c.dot(12, 10, GOLD_HI)
        star = pattern([
            "....Y....",
            "....Y....",
            "...YWY...",
            "YYYYWYYYY",
            ".YYWWWYY.",
            "..YYYYY..",
            "..YY.YY..",
            ".YY...YY.",
            ".Y.....Y.",
        ], STAR_COLORS)
        place(c, star, 12, 1)
        return c.img.crop((1, 0, 23, 22))
    # On the floor it lies nearly flat.
    for i in range(9):
        c.dot(2 + i, 9 - i // 3, WOOD_HI)
        c.dot(2 + i, 10 - i // 3, WOOD)
    c.dot(10, 7, GOLD)
    c.dot(10, 8, GOLD)
    star = pattern([
        "..Y..",
        "YYWYY",
        ".YWY.",
        ".Y.Y.",
    ], STAR_COLORS)
    place(c, star, 10, 4)
    return c.img.crop((2, 4, 15, 11))


# ---------- Trailblazer Boots ----------

BOOT_COLORS = {"C": CUFF, "L": LEATHER, "l": LEATHER_HI, "d": LEATHER_SH, "G": GOLD, "S": SOLE}


def boots(big):
    """Two boots, toes to the right: the far one a step behind and in shadow."""
    if big:
        boot = pattern([
            "CCCCCC.....",
            "CCCCCC.....",
            "lLLLLd.....",
            "lLLLLd.....",
            "lLGGLd.....",
            "lLLLLd.....",
            "lLLLLd.....",
            "lLLLLd.....",
            "lLLLLLd....",
            "lLLLLLLLd..",
            "lLLLLLLLLd.",
            "lLLLLLLLLLd",
            "SSSSSSSSSSS",
        ], BOOT_COLORS)
        far, near, size = (6, 0), (0, 3), (17, 16)
    else:
        boot = pattern([
            "CCCC...",
            "lLLd...",
            "lGLd...",
            "lLLd...",
            "lLLLd..",
            "lLLLLLd",
            "SSSSSSS",
        ], BOOT_COLORS)
        far, near, size = (4, 0), (0, 2), (11, 9)
    img = Image.new("RGBA", size, CLEAR)
    img.alpha_composite(shade(boot), far)
    img.alpha_composite(outline(boot), near)  # its own outline separates it from the far boot
    return img


def shade(img, amount=0.72):
    """A darker copy: the far boot sits in its partner's shadow."""
    out = img.copy()
    px = out.load()
    for y in range(out.size[1]):
        for x in range(out.size[0]):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (int(r * amount), int(g * amount), int(b * amount), a)
    return out


# ---------- Bubble Bath ----------

BOTTLE, BOTTLE_HI, BOTTLE_SH = (238, 236, 250, 255), (255, 255, 255, 255), (176, 170, 204, 255)
SOAP, SOAP_HI, SOAP_SH = (196, 150, 226, 255), (226, 196, 246, 255), (150, 104, 186, 255)
CORK, CORK_SH = (196, 150, 104, 255), (150, 106, 70, 255)
BOW = pal.rgba(pal.CORAL)
BUBBLE = (214, 232, 252, 255)
BATH_COLORS = {"B": BOTTLE, "b": BOTTLE_HI, "s": BOTTLE_SH, "L": SOAP, "l": SOAP_HI, "d": SOAP_SH,
               "C": CORK, "c": CORK_SH, "R": BOW, "W": BUBBLE}  # b doubles as a bubble's shine


def bubble_bath(big):
    """A round-bellied glass bottle, half full of lilac soap, a cork, a coral bow round its neck
    and a couple of bubbles floating off the top."""
    if big:
        return pattern([
            "...........WW...",
            "..........WbWW..",
            "..........WWWW..",
            "......CCc..WW...",
            "......CCc.......",
            "......bBs....WW.",
            ".....RRbRR..WbW.",
            "....RR.B.RR..W..",
            "....bBBBBBs.....",
            "...bBBBBBBBs....",
            "..bBBBBBBBBBs...",
            "..bLlLLLLLLds...",
            "..blLLLLLLLds...",
            "..bLLLLLLLLds...",
            "..bLLLLLLLLds...",
            "...dLLLLLLdd....",
            "....ssssss......",
        ], BATH_COLORS)
    return pattern([
        ".....WW",
        "..Cc.WW",
        "..bs...",
        ".RRRR..",
        ".bBBs..",
        "bBBBBs.",
        "blLLds.",
        "bLLLds.",
        ".dddd..",
    ], BATH_COLORS)


# ---------- Cooking: ingredients and the dish ----------
# Drawn with shapes on a small square canvas, sitting on its bottom edge: 20px for the icon,
# 11-12px for the floor. Lit from the top-left like everything else.

EGG, EGG_HI, EGG_SH = (252, 244, 226, 255), (255, 255, 255, 255), (214, 196, 168, 255)
SACK, SACK_HI, SACK_SH = (236, 222, 192, 255), (250, 244, 226, 255), (192, 168, 128, 255)
TWINE = (168, 112, 64, 255)
BERRY, BERRY_HI, BERRY_SH = (226, 52, 72, 255), (255, 120, 120, 255), (164, 28, 52, 255)
SEED = (255, 226, 120, 255)
STALK, STALK_HI = (70, 150, 70, 255), (120, 196, 96, 255)
CAKE, CAKE_HI, CAKE_SH = (232, 170, 84, 255), (250, 206, 120, 255), (184, 116, 50, 255)
BUTTER = (255, 236, 150, 255)
SYRUP = (176, 92, 40, 255)
PLATE, PLATE_SH = (240, 240, 248, 255), (190, 194, 214, 255)


def egg(big):
    """A speckled brown-cream hen's egg, standing up."""
    n = 20 if big else 11
    c = Canvas(n)
    rx, ry = (6, 8) if big else (3.3, 4.6)
    cx, cy = n / 2 - 0.5, n - 1 - ry
    c.ellipse(cx, cy, rx, ry, EGG_SH)
    c.ellipse(cx - 0.6, cy - 0.6, rx - 0.8, ry - 0.8, EGG)
    c.ellipse(cx - rx * 0.4, cy - ry * 0.45, rx * 0.25, ry * 0.2, EGG_HI)
    if big:
        for x, y in ((12, 10), (8, 14), (13, 15), (10, 7)):  # speckles
            c.dot(x, y, EGG_SH)
    return c.img


def flour(big):
    """A little cloth sack of flour, tied with twine, a puff of flour on top."""
    n = 20 if big else 12
    c = Canvas(n)
    if big:
        c.ellipse(9.5, 13, 7, 6, SACK_SH)                      # the sack's round belly
        c.ellipse(9, 12.5, 6.4, 5.6, SACK)
        c.rect(6, 4, 13, 8, SACK)                               # its gathered neck
        c.rect(13, 4, 13, 8, SACK_SH)
        c.rect(5, 7, 14, 7, TWINE)                              # twine
        c.dot(15, 8, TWINE)
        c.rect(7, 2, 12, 3, EGG_HI)                             # flour poking out
        c.ellipse(6, 11, 1.5, 2, SACK_HI)
        c.rect(7, 13, 12, 13, SACK_SH)                          # a stitched band
    else:
        c.ellipse(5.5, 8, 4.5, 3.6, SACK_SH)
        c.ellipse(5.2, 7.7, 4, 3.2, SACK)
        c.rect(4, 2, 7, 5, SACK)
        c.rect(3, 4, 8, 4, TWINE)
        c.rect(4, 1, 7, 1, EGG_HI)
        c.dot(3, 7, SACK_HI)
    return c.img


def strawberry(big):
    """A plump strawberry with yellow seeds and a leafy green top."""
    n = 20 if big else 11
    c = Canvas(n)
    if big:
        for y in range(5, 19):  # a heart-ish berry, narrowing to the tip
            half = round(7 * (1 - ((y - 7) / 12) ** 2)) if y >= 7 else 6
            c.rect(9 - half, y, 10 + half, y, BERRY)
            c.dot(10 + half, y, BERRY_SH)
        c.ellipse(6, 9, 1.5, 2, BERRY_HI)
        for x, y in ((8, 9), (12, 8), (5, 12), (10, 12), (14, 12), (8, 15), (12, 15), (10, 17)):
            c.dot(x, y, SEED)
        for x0, x1 in ((4, 9), (10, 15)):  # leaves
            c.line(x0, 5, x1, 4, STALK)
        c.rect(7, 3, 12, 5, STALK)
        c.rect(9, 0, 10, 3, STALK_HI)
    else:
        for y in range(3, 11):
            half = round(4 * (1 - ((y - 4) / 7) ** 2)) if y >= 4 else 3
            c.rect(5 - half, y, 5 + half, y, BERRY)
            c.dot(5 + half, y, BERRY_SH)
        c.dot(3, 5, BERRY_HI)
        for x, y in ((5, 6), (3, 8), (7, 7), (5, 9)):
            c.dot(x, y, SEED)
        c.rect(3, 2, 7, 3, STALK)
        c.dot(5, 1, STALK_HI)
    return c.img


def pancakes(big):
    """A stack of three golden pancakes on a plate: a pat of butter melting, syrup dripping
    down the side, and a strawberry on top."""
    n = 20 if big else 12
    c = Canvas(n)
    if big:
        c.ellipse(9.5, 17.5, 9.5, 2.2, PLATE_SH)                 # the plate
        c.ellipse(9.5, 17, 9, 1.8, PLATE)
        for k, y in enumerate((15, 12, 9)):                      # three pancakes, bottom up
            c.ellipse(9.5, y, 8, 2.4, CAKE_SH)
            c.ellipse(9.5, y - 0.6, 7.6, 1.9, CAKE)
            c.rect(3, y - 1, 15, y - 1, CAKE_HI) if k == 2 else None
        c.ellipse(9.5, 7.6, 7, 1.6, CAKE_HI)                    # the top one's golden face
        c.ellipse(9.5, 7.8, 5, 1.1, SYRUP)                      # syrup pooling...
        c.rect(14, 8, 14, 13, SYRUP)                            # ...and dripping down the side
        c.dot(14, 14, SYRUP)
        c.rect(5, 7, 5, 10, SYRUP)
        c.rect(7, 6, 9, 7, BUTTER)                              # a pat of butter
        c.ellipse(12, 4.5, 2.2, 2.4, BERRY)                     # and a strawberry on top
        c.dot(11, 4, BERRY_HI)
        c.dot(12, 5, SEED)
        c.rect(11, 1, 13, 2, STALK)
    else:
        c.ellipse(5.5, 10.5, 5.5, 1.4, PLATE)
        for y in (9, 7, 5):
            c.ellipse(5.5, y, 4.6, 1.5, CAKE_SH)
            c.ellipse(5.5, y - 0.4, 4.3, 1.1, CAKE)
        c.ellipse(5.5, 4.5, 3, 0.8, SYRUP)
        c.rect(9, 5, 9, 8, SYRUP)
        c.ellipse(7, 2.5, 1.4, 1.4, BERRY)
        c.dot(7, 1, STALK)
        c.rect(4, 4, 5, 4, BUTTER)
    return c.img


# ---------- Hollow Farm's autumn festival ----------

MUG, MUG_SH, MUG_HI = (196, 120, 76, 255), (150, 84, 56, 255), (226, 156, 106, 255)
CIDER, CIDER_HI = (214, 140, 60, 255), (246, 196, 110, 255)
CINNAMON = (128, 74, 46, 255)
STEAM = (246, 244, 255, 255)
APPLE, APPLE_HI = pal.rgba(pal.CORAL), pal.rgba(pal.CORAL_LIGHT)
PUMPKIN, PUMPKIN_SH, PUMPKIN_HI = (236, 130, 50, 255), (190, 90, 36, 255), (255, 176, 90, 255)
CANDLE_GLOW, CANDLE_HI = (255, 214, 90, 255), (255, 246, 190, 255)
VINE = (100, 140, 70, 255)


def apple_cider(big):
    """A clay mug of hot apple cider: a cinnamon stick, an apple slice on the rim, and steam."""
    n = 18 if big else 11
    c = Canvas(n)
    if big:
        for x, y in ((6, 3), (7, 1), (10, 2), (11, 0)):        # steam curling up
            c.dot(x, y, STEAM)
        c.rect(3, 6, 13, 16, MUG)                               # the mug
        c.rect(3, 6, 4, 16, MUG_HI)
        c.rect(12, 6, 13, 16, MUG_SH)
        c.rect(4, 17, 12, 17, MUG_SH)
        c.rect(14, 8, 16, 8, MUG)                               # its handle
        c.rect(16, 9, 16, 12, MUG)
        c.rect(14, 13, 16, 13, MUG)
        c.rect(4, 6, 12, 7, CIDER)                              # the cider inside
        c.rect(5, 6, 8, 6, CIDER_HI)
        c.line(9, 2, 11, 8, CINNAMON)                           # a cinnamon stick
        c.ellipse(4, 6, 2, 1.5, APPLE)                          # an apple slice on the rim
        c.dot(4, 6, (250, 236, 200, 255))
        c.rect(5, 10, 11, 11, MUG_SH)                           # a band round the mug
    else:
        c.dot(5, 0, STEAM)
        c.dot(7, 1, STEAM)
        c.rect(2, 3, 8, 10, MUG)
        c.rect(2, 3, 2, 10, MUG_HI)
        c.rect(3, 3, 7, 4, CIDER)
        c.rect(9, 5, 10, 5, MUG)
        c.rect(10, 6, 10, 7, MUG)
        c.rect(9, 8, 10, 8, MUG)
        c.line(6, 0, 7, 4, CINNAMON)
    return c.img


def pumpkin_hat(big):
    """A carved pumpkin to wear on your head: a jolly grin glowing from inside, a curly stem."""
    n = 20 if big else 12
    c = Canvas(n)
    if big:
        cx, cy, r = 9.5, 11, 7
        c.ellipse(cx, cy, r * 1.25, r, PUMPKIN_SH)
        for dx in (-r * 0.55, 0, r * 0.55):
            c.ellipse(cx + dx - 0.5, cy - 0.5, r * 0.55, r * 0.92, PUMPKIN)
        c.ellipse(cx - 4.5, cy - 3.5, 1.5, 1.2, PUMPKIN_HI)
        c.rect(9, 1, 10, 4, VINE)                               # the stem, with a curl
        c.dot(11, 1, VINE)
        c.dot(12, 2, VINE)
        for ex in (6, 13):                                      # triangle eyes, glowing
            for i in range(3):
                c.rect(ex - i, 8 + i, ex + i, 8 + i, CANDLE_GLOW)
        c.dot(6, 9, CANDLE_HI)
        c.rect(5, 13, 14, 14, CANDLE_GLOW)                      # the grin
        for x in (7, 10, 13):
            c.dot(x, 13, PUMPKIN_SH)
        c.rect(3, 17, 16, 17, PUMPKIN_SH)                        # the brim cut out underneath
    else:
        c.ellipse(5.5, 7, 5.5, 4, PUMPKIN_SH)
        c.ellipse(5.5, 6.5, 5, 3.5, PUMPKIN)
        c.rect(5, 1, 6, 3, VINE)
        c.dot(3, 6, CANDLE_GLOW)
        c.dot(8, 6, CANDLE_GLOW)
        c.rect(3, 8, 8, 8, CANDLE_GLOW)
    return c.img


# ---------- Floor frames and icons ----------

ITEMS = {
    "PlumedHelm": helm,
    "SeashellMail": armor,
    "StarlightWand": wand,
    "TrailblazerBoots": boots,
    "BubbleBath": bubble_bath,
    "Egg": egg,
    "Flour": flour,
    "Strawberry": strawberry,
    "Pancakes": pancakes,
    "AppleCider": apple_cider,
    "PumpkinHat": pumpkin_hat,
}


def floor_frame(draw, glint):
    """The small drawing resting on the floor, centred, its bottom 4px above the frame's
    bottom (like the Ember Ring), with a glint on every other frame."""
    c = Canvas()
    art = draw(False)
    w, h = art.size
    x, y = 16 - w // 2, 28 - h
    place(c, art, x, y)
    if glint:
        gx, gy = x + w - 2, y + 1
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0), (0, 0)):
            c.dot(gx + dx, gy + dy, GLINT)
    return c.img


def icon(draw):
    """The big drawing centred in a 24x24 icon, with the plum outline the other icons have."""
    art = draw(True)
    img = Image.new("RGBA", (24, 24), CLEAR)
    img.alpha_composite(art, ((24 - art.size[0]) // 2, (24 - art.size[1]) // 2))
    return outline(img)


def main():
    write_sheet("Items", None, [(name, 3, True, [floor_frame(draw, glint=f == 1) for f in range(2)])
                                for name, draw in ITEMS.items()])
    UI.mkdir(parents=True, exist_ok=True)
    for name, draw in ITEMS.items():
        icon(draw).save(UI / f"Icon{name}.png")
    print("Wrote", ", ".join(f"Icon{name}.png" for name in ITEMS))


if __name__ == "__main__":
    main()
