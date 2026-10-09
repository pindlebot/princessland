"""Generates the equipment items: how each looks lying on the floor, and its inventory icon.

Run:  Tools/.venv/bin/python Tools/make_item_sprites.py
Out:  Assets/Art/Items.png / Items.json   (32x32 frames, bottom pivot)
        PlumedHelm        2 frames, looping  a steel helm with a coral plume, glinting
        SeashellMail      2 frames, looping  a tunic of aquamarine shell scales with a pearl
        StarlightWand     2 frames, looping  a wooden wand tipped with a gold star
        TrailblazerBoots  2 frames, looping  a pair of leather boots with cream cuffs
        BubbleBath        2 frames, looping  a round bottle of lilac bubble bath with a cork and a bow
                                             (sold at Barnaby's stall in the village; it has no use yet)
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


# ---------- Floor frames and icons ----------

ITEMS = {
    "PlumedHelm": helm,
    "SeashellMail": armor,
    "StarlightWand": wand,
    "TrailblazerBoots": boots,
    "BubbleBath": bubble_bath,
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
