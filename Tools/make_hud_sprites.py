"""Generates the pixel-art HUD pieces.

Run:  Tools/.venv/bin/python Tools/make_hud_sprites.py
Out:  Assets/Art/UI/
        Frame.png          16x16  gold-trimmed frame (9-sliced by USS: 4px borders)
        Panel.png          16x16  same frame with a translucent interior, for text boxes
        Slot.png           16x16  stone hotbar slot (9-sliced)
        PortraitWizard.png    24x24  head-and-shoulders portraits (HUD and character select)
        PortraitPrincess.png  24x24
        IconFireball.png      24x24  hotbar icons, one per spell
        IconTidalOrb.png      24x24
        IconEmberRing.png     24x24  inventory icon
        IconCoin.png          12x12  gold counter in the HUD

      The storybook style (cream panels, honey-gold borders, plum ink):
        PanelCream.png        16x16  the panel every HUD box uses (9-sliced: 5px borders)
        Heart.png / HeartEmpty.png   13x12  health, one heart per point
        IconMagic.png         12x12  a turquoise sparkle beside the magic bar
        IconMonster.png       12x12  "monsters left" in the objective card
        PipMonster.png / PipStar.png 9x9   progress markers: a monster, then a star once defeated
        Map*.png              tiny minimap markers: crown (hero), stairs (open/locked), monster,
                              chest and dragon, each with a plum outline so they read when small

The portrait and icon are cut from the actual game sprites, so they always match.
"""
from PIL import Image

import make_prop_sprites as props
import make_spell_sprites as spell
import make_princess_sprites as princess
import make_wizard_sprites as wizard
from sprite_common import ART, CLEAR, OUTLINE, Canvas, outline

UI = ART / "UI"


def frame(trim_hi, trim_lo, interior, size=16):
    """Bevelled frame: dark outline, a light/dark trim (lit from the top-left), an inner
    shadow, then the interior. Everything that matters sits in the 4px border, so the
    middle can be stretched to any size (9-slicing)."""
    img = Image.new("RGBA", (size, size), interior)
    px = img.load()
    last = size - 1
    for i in range(size):
        for a, b in ((i, 0), (i, last), (0, i), (last, i)):
            px[a, b] = OUTLINE
    for i in range(1, last):
        px[i, 1] = px[1, i] = trim_hi  # lit top and left edges
        px[i, last - 1] = px[last - 1, i] = trim_lo  # shaded bottom and right edges
    for i in range(2, last - 1):
        px[i, 2] = px[2, i] = trim_lo
        px[i, last - 2] = px[last - 2, i] = trim_hi
        px[i, 3] = px[3, i] = (12, 10, 16, interior[3])  # inner shadow
    return img


def portrait(idle_frame, top, background):
    """Head and shoulders from a character's idle frame, on a soft gradient."""
    bust = outline(idle_frame.crop((6, top, 26, top + 21)))
    r, g, b = background
    img = Image.new("RGBA", (24, 24))
    px = img.load()
    for y in range(24):
        for x in range(24):
            px[x, y] = (r + y, g + y, b + y // 2, 255)
    img.alpha_composite(bust, (2, 3))
    return img


def spell_icon(palette, outline_color):
    """The projectile tilted to fly up and to the right, trimmed to fit a 24x24 icon."""
    fly = spell.fly_frame(0, palette).rotate(35, resample=Image.NEAREST, center=(16, 16))
    fly = fly.crop(fly.getbbox())
    img = Image.new("RGBA", (24, 24), CLEAR)
    img.alpha_composite(fly, ((24 - fly.width) // 2, (24 - fly.height) // 2))
    return outline(img, outline_color)


def coin_icon():
    c = Canvas()
    c.ellipse(5.5, 5.5, 5.5, 5.5, props.GOLD_SH)
    c.ellipse(5.5, 5.5, 4.2, 4.2, props.GOLD)
    c.rect(5, 3, 6, 8, props.GOLD_SH)  # a stamped bar across the middle
    c.dot(3, 3, props.GOLD_HI)
    return outline(c.img.crop((0, 0, 12, 12)))


# ---------- Storybook style ----------

CREAM, CREAM_SH = (248, 239, 216, 255), (232, 218, 188, 255)
HONEY, HONEY_HI, HONEY_SH = (222, 168, 70, 255), (250, 214, 120, 255), (164, 112, 40, 255)
PLUM = (74, 37, 69, 255)
HEART, HEART_HI, HEART_SH = (232, 70, 92, 255), (255, 170, 180, 255), (176, 36, 64, 255)
MAGIC, MAGIC_HI = (64, 206, 210, 255), (210, 255, 250, 255)


def pattern(rows, palette):
    """Pixel art from strings: each character is a palette key ('.' = transparent)."""
    img = Image.new("RGBA", (len(rows[0]), len(rows)), CLEAR)
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".":
                px[x, y] = palette[ch]
    return img


def cream_panel():
    """Cream paper with a plum outline, a honey-gold border (light top-left, darker
    bottom-right) and a little gold stud in each corner. 9-sliced at 5px."""
    img = Image.new("RGBA", (16, 16), CREAM)
    px = img.load()
    for i in range(16):
        for a, b in ((i, 0), (i, 15), (0, i), (15, i)):
            px[a, b] = PLUM
    for i in range(1, 15):
        px[i, 1] = px[1, i] = HONEY_HI
        px[i, 14] = px[14, i] = HONEY_SH
        px[i, 2] = px[2, i] = HONEY
        px[i, 13] = px[13, i] = HONEY
    for i in range(3, 13):
        px[i, 3] = px[3, i] = CREAM_SH  # a soft inner shadow
    for cx, cy in ((2, 2), (13, 2), (2, 13), (13, 13)):  # corner studs
        px[cx, cy] = PLUM
    # Transparent outside the rounded corners
    for x, y in ((0, 0), (15, 0), (0, 15), (15, 15)):
        px[x, y] = CLEAR
    return img


HEART_ROWS = [
    ".OOO...OOO...",
    "OHhRO.ORRRO..",
    "OhRRROORRRRO.",
    "ORRRRRRRRRRO.",
    "ORRRRRRRRRRO.",
    ".ORRRRRRRRO..",
    "..ORRRRRRSO..",
    "...ORRRRSO...",
    "....ORRSO....",
    ".....OSO.....",
    "......O......",
    ".............",
]


def heart(full):
    if full:
        pal = {"O": PLUM, "R": HEART, "H": HEART_HI, "h": HEART_HI, "S": HEART_SH}
    else:  # an empty outline with a faint pale inside
        pal = {"O": PLUM, "R": CREAM_SH, "H": CREAM_SH, "h": CREAM_SH, "S": CREAM_SH}
    return pattern(HEART_ROWS, pal)


def magic_icon():
    return pattern([
        ".....O......",
        "....OMO.....",
        "....OMO.....",
        "..OOMHMOO...",
        ".OMMHHHMMO..",
        "..OOMHMOO...",
        "....OMO..O..",
        "....OMO.OHO.",
        ".....O...O..",
        "..O.........",
        ".OHO........",
        "..O.........",
    ], {"O": PLUM, "M": MAGIC, "H": MAGIC_HI})


def monster_icon():  # a little slime face
    return pattern([
        "............",
        "....OOOO....",
        "..OOGGGGOO..",
        ".OGGHGGGGGO.",
        ".OGHGGGGGGO.",
        "OGGOGGGGOGGO",
        "OGGOGGGGOGGO",
        "OGGGGOOGGGGO",
        "OGGGGGGGGGGO",
        "OSSSSSSSSSSO",
        ".OOOOOOOOOO.",
        "............",
    ], {"O": PLUM, "G": (110, 200, 110, 255), "H": (200, 245, 190, 255), "S": (70, 150, 80, 255)})


def pip_monster():
    return pattern([
        "..OOOOO..",
        ".OGGGGGO.",
        "OGGGGGGGO",
        "OGOGGGOGO",
        "OGGGGGGGO",
        "OGGOOOGGO",
        "OSSSSSSSO",
        ".OOOOOOO.",
        ".........",
    ], {"O": PLUM, "G": (150, 140, 150, 255), "S": (120, 110, 120, 255)})


def pip_star():
    return pattern([
        "....O....",
        "...OYO...",
        "OOOOYOOOO",
        "OYYYHYYYO",
        ".OYYYYYO.",
        "..OYYYO..",
        ".OYYOYYO.",
        ".OYO.OYO.",
        ".OO...OO.",
    ], {"O": PLUM, "Y": HONEY_HI, "H": (255, 255, 230, 255)})


def pad(img, by=1):
    """A transparent border, so outline() has room to draw around the edges."""
    out = Image.new("RGBA", (img.width + 2 * by, img.height + 2 * by), CLEAR)
    out.alpha_composite(img, (by, by))
    return out


def map_markers():
    green, grey, red, purple = (120, 230, 140, 255), (176, 166, 186, 255), (240, 70, 60, 255), (200, 140, 255, 255)
    return {
        # The hero: a bold crown with three clear points, outlined so it stands out anywhere.
        "MapCrown": outline(pad(pattern([
            "Y...Y...Y",
            "YY.YYY.YY",
            "YYYYYYYYY",
            "YRYYRYYRY",
            "YYYYYYYYY",
            "SSSSSSSSS",
        ], {"Y": HONEY_HI, "R": HEART, "S": HONEY_SH})), PLUM),
        "MapStairs": pattern(["....OOO", "...OGGO", "..OGGGO", ".OGGGGO", "OGGGGGO", "OOOOOOO"], {"O": PLUM, "G": green}),
        "MapStairsLocked": pattern(["....OOO", "...OGGO", "..OGGGO", ".OGGGGO", "OGGGGGO", "OOOOOOO"], {"O": PLUM, "G": grey}),
        "MapMonster": pattern([".OOO.", "ORRRO", "ORRRO", ".OOO."], {"O": PLUM, "R": red}),
        "MapChest": pattern(["OOOOO", "OYYYO", "OYYYO", "OOOOO"], {"O": PLUM, "Y": HONEY_HI}),
        "MapDragon": pattern(["..O..", ".OPO.", "OPPPO", ".OPO.", "..O.."], {"O": PLUM, "P": purple}),
    }


def ring_icon():
    c = Canvas()
    props.draw_ring(c, 12, 14, 8, 3, glint=True)
    return outline(c.img.crop((0, 0, 24, 24)))


def main():
    UI.mkdir(parents=True, exist_ok=True)
    gold_hi, gold_lo = (226, 186, 92, 255), (146, 102, 44, 255)
    stone_hi, stone_lo = (150, 146, 162, 255), (86, 82, 98, 255)
    frame(gold_hi, gold_lo, (24, 20, 32, 255)).save(UI / "Frame.png")
    frame(gold_hi, gold_lo, (16, 14, 22, 200)).save(UI / "Panel.png")
    frame(stone_hi, stone_lo, (20, 18, 26, 255)).save(UI / "Slot.png")
    portrait(wizard.draw_wizard(), 1, (36, 40, 74)).save(UI / "PortraitWizard.png")
    portrait(princess.draw_princess(), 1, (30, 70, 80)).save(UI / "PortraitPrincess.png")
    spell_icon(spell.FIRE, (96, 24, 16, 255)).save(UI / "IconFireball.png")
    spell_icon(spell.WATER, (16, 50, 90, 255)).save(UI / "IconTidalOrb.png")
    ring_icon().save(UI / "IconEmberRing.png")
    coin_icon().save(UI / "IconCoin.png")
    cream_panel().save(UI / "PanelCream.png")
    heart(True).save(UI / "Heart.png")
    heart(False).save(UI / "HeartEmpty.png")
    magic_icon().save(UI / "IconMagic.png")
    monster_icon().save(UI / "IconMonster.png")
    pip_monster().save(UI / "PipMonster.png")
    pip_star().save(UI / "PipStar.png")
    for name, img in map_markers().items():
        img.save(UI / f"{name}.png")
    print("Wrote", ", ".join(sorted(p.name for p in UI.glob("*.png"))))


if __name__ == "__main__":
    main()
