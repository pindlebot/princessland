"""Generates the art for Frostpeak (Assets/Levels/Frost1-4.txt).

Run:  Tools/.venv/bin/python Tools/make_frost_sprites.py     (after make_slime_sprites.py: it recolours the slime)
Out:  Assets/Art/IceSlime.png / .json     32x32: the slime, but blue and frosty (it leaves a slippery trail)
      Assets/Art/SnowImp.png / .json      32x32: a small furry imp that lobs snowballs
      Assets/Art/SnowYeti.png / .json     64x64: Frostpeak's boss, a great white yeti with a blue face
      Assets/Art/MrFrost.png / .json      48x48 NPC: Mr. Frost, a shivering snowman (Idle, Talk)
      Assets/Art/Purl.png / .json         48x48 NPC: Granny Purl, a knitting granny in a woolly shawl (Idle, Talk)
      Assets/Art/UI/PortraitMrFrost.png, PortraitPurl.png   32x32 dialogue portraits
      Assets/Art/FrostProps.png / .json   48x48, bottom pivot:
        RainbowPost   2 frames: a post wound with colour (plain, then glowing once its bridge is drawn)
        Igloo         a snowy dome with a door     Snowman   a plain snowman with a carrot nose (scenery)
        IceSpire      a tall pale-blue crystal      Drift     a heap of snow with a ski pole stuck in it
      Assets/Art/Snowball.png / .json     32x32 centre pivot: a big rolling snowball (Roll, 4 frames)
      Assets/Art/IcePatch.png / .json     32x32 centre pivot, flat: a patch of ice an ice slime leaves behind

Same conventions as make_woods_sprites.py: Front / Back for Idle, Walk, Attack and Hurt, a single Die, and
"Attack" as the action state.
"""
import colorsys
import json
import math
import shutil

from PIL import Image

from make_item_sprites import icon
from make_woods_sprites import portrait
from sprite_common import ART, CLEAR, Canvas, outline, tint, write_sheet

UI = ART / "UI"

# ---------- Palette: snow white, ice blue, and warm woollen colours ----------
SNOW, SNOW_HI, SNOW_SH = (240, 246, 252, 255), (255, 255, 255, 255), (178, 200, 228, 255)
FUR, FUR_HI, FUR_SH = (226, 232, 244, 255), (250, 252, 255, 255), (158, 176, 212, 255)
FACE, FACE_HI, FACE_SH = (96, 150, 206, 255), (150, 200, 240, 255), (58, 96, 160, 255)
ICE, ICE_HI, ICE_SH = (176, 218, 240, 255), (226, 246, 255, 255), (110, 168, 214, 255)
EYE = (40, 36, 70, 255)
WHITE = (255, 255, 255, 255)
CARROT, CARROT_SH = (240, 140, 50, 255), (190, 96, 40, 255)
COAL = (60, 56, 76, 255)
SCARF_RED, SCARF_SH = (214, 80, 96, 255), (160, 50, 78, 255)
WOOD, WOOD_SH = (150, 104, 70, 255), (100, 68, 52, 255)
WOOL, WOOL_SH = (186, 130, 190, 255), (130, 86, 146, 255)
SHAWL, SHAWL_SH, SHAWL_HI = (120, 160, 196, 255), (76, 108, 150, 255), (170, 204, 230, 255)
HAIR = (236, 232, 240, 255)
SKIN, SKIN_SH = (240, 200, 170, 255), (200, 150, 130, 255)
IMP, IMP_HI, IMP_SH = (150, 130, 190, 255), (196, 180, 230, 255), (96, 80, 140, 255)
RAINBOW = [(224, 70, 80, 255), (250, 150, 60, 255), (250, 220, 80, 255), (90, 200, 110, 255), (80, 150, 240, 255), (160, 100, 220, 255)]
GLOW = (255, 250, 200, 255)


def fade(img, keep):
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, int(a * keep))
    return img


# ---------- Ice slime: the slime, recoloured ----------

def make_ice_slime():
    """Copies Slime.png/.json as IceSlime and shifts every green pixel to a frosty blue."""
    src = Image.open(ART / "Slime.png").convert("RGBA")
    px = src.load()
    for y in range(src.height):
        for x in range(src.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            if 0.18 < h < 0.5 and s > 0.15:                 # the greens
                h = 0.56 + (h - 0.33) * 0.15
                s = min(1.0, s * 0.8)
                v = min(1.0, v * 1.08 + 0.05)
                r, g, b = (int(c * 255) for c in colorsys.hsv_to_rgb(h, s, v))
                px[x, y] = (r, g, b, a)
    src.save(ART / "IceSlime.png")
    shutil.copy(ART / "Slime.json", ART / "IceSlime.json")
    print("IceSlime: recoloured from Slime")


# ---------- Snow imp ----------

def draw_imp(back=False, step=0, throw=0, squash=0, hurt=False, size=32):
    """A small furry lilac imp with pointed ears and a woolly bobble hat. throw 1 winds up (a snowball overhead),
    2 flings."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    ground = size - 3
    top = 10 + squash
    for dx, st in ((-4, step), (4, -step)):                                 # stubby feet
        c.ellipse(cx + dx + st, ground - 1, 3.2, 1.8, IMP_SH)
    c.ellipse(cx, (top + ground) / 2 + 1, 8, (ground - top) / 2 + 1, IMP_SH)   # the round body
    c.ellipse(cx - 0.6, (top + ground) / 2 + 0.4, 7.4, (ground - top) / 2 + 0.4, IMP)
    c.ellipse(cx - 3, top + 4, 3, 1.8, IMP_HI)
    for side in (-1, 1):                                                    # ears
        c.line(round(cx + side * 6), top + 3, round(cx + side * 10), top - 2, IMP_SH)
        c.line(round(cx + side * 5), top + 3, round(cx + side * 9), top - 1, IMP)
    for side in (-1, 1):                                                    # arms
        ay = top + 8 - (7 if throw == 1 and side > 0 else 0)
        c.line(round(cx + side * 6), top + 8, round(cx + side * 9), ay, IMP_SH)
    if throw:                                                               # the snowball
        bx, by = (round(cx + 10), top - 2) if throw == 1 else (round(cx + 13), top + 4)
        c.ellipse(bx, by, 3, 3, SNOW_SH)
        c.ellipse(bx - 0.4, by - 0.4, 2.4, 2.4, SNOW)
    c.ellipse(cx, top - 2, 7, 3, SCARF_SH)                                  # the bobble hat
    c.ellipse(cx, top - 3, 6, 2.6, SCARF_RED)
    c.ellipse(cx, top - 6, 2, 2, WHITE)
    if not back:
        eyes = WHITE if hurt else EYE
        for side in (-1, 1):
            c.dot(round(cx + side * 3), top + 4, eyes)
            c.dot(round(cx + side * 3), top + 5, eyes)
        c.line(round(cx) - 2, top + 8, round(cx) + 2, top + 8, EYE)
        c.dot(round(cx) - 3, top + 7, EYE)
        c.dot(round(cx) + 3, top + 7, EYE)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def imp_die(stage, size=32):
    if stage < 2:
        return draw_imp(squash=2 + stage * 3, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 3
    c.ellipse(cx, ground - 2, 9, 3, IMP_SH)
    c.ellipse(cx, ground - 3, 8, 2.6, IMP)
    c.ellipse(cx, ground - 6, 5, 3, SCARF_RED)                              # the hat, fallen over
    c.ellipse(cx + 2, ground - 8, 2, 2, WHITE)
    for dx, dy in ((-7, -9), (6, -8), (0, -12)):
        c.dot(round(cx + dx), ground + dy, SNOW_HI)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def imp_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_imp(b), draw_imp(b, squash=1)]),
            (f"Walk_{facing}", 6, True, [draw_imp(b, step=2), draw_imp(b, squash=1), draw_imp(b, step=-2), draw_imp(b, squash=1)]),
            (f"Attack_{facing}", 8, False, [draw_imp(b, throw=1, squash=1), draw_imp(b, throw=1), draw_imp(b, throw=2), draw_imp(b)]),
            (f"Hurt_{facing}", 8, False, [draw_imp(b, squash=2, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [imp_die(s) for s in range(4)]))
    return anims


# ---------- Snow yeti ----------

def draw_yeti(back=False, bob=0, step=0, raise_arms=0, shake=0, hurt=False, size=64):
    """A hulking white yeti with shaggy fur, long arms, a small blue face with a heavy brow, and two big fangs."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + shake
    ground = size - 3
    top = 9 + bob
    for dx in (-10, 10):                                                    # thick legs and big feet
        s = step if dx > 0 else -step
        c.ellipse(cx + dx + s, ground - 8, 8, 9, FUR_SH)
        c.ellipse(cx + dx + s - 0.6, ground - 8.6, 7.4, 8.4, FUR)
        c.ellipse(cx + dx + s, ground - 1, 9, 2.6, FUR_SH)
    c.ellipse(cx, top + 26, 22, 18, FUR_SH)                                 # the torso
    c.ellipse(cx - 1, top + 25, 21, 17, FUR)
    c.ellipse(cx - 8, top + 17, 8, 4, FUR_HI)
    for k in range(-4, 5):                                                  # shaggy fur tufts hanging down
        x = round(cx + k * 5)
        c.line(x, top + 38, x + (1 if k % 2 else -1), top + 44, FUR_SH)
    for side in (-1, 1):                                                    # arms and fists
        sx = cx + side * 21
        c.ellipse(sx, top + 15, 9, 9, FUR_SH)
        c.ellipse(sx - side * 0.8, top + 14, 8, 8, FUR)
        ay = top + 22 - raise_arms * 12
        c.rect(round(sx - 4), min(top + 20, ay), round(sx + 4), max(top + 20, ay) + 14, FUR_SH)
        c.rect(round(sx - 3 - side), min(top + 20, ay), round(sx + 3 - side), max(top + 20, ay) + 14, FUR)
        c.ellipse(sx, ay + 16, 7, 6, FUR_SH)
        c.ellipse(sx - 0.6, ay + 15.4, 6.2, 5.2, FUR)
    c.ellipse(cx, top + 12, 12, 11, FUR_SH)                                 # the head, in a ruff of fur
    c.ellipse(cx - 0.6, top + 11.4, 11.4, 10.4, FUR)
    c.ellipse(cx - 4, top + 6, 4, 2, FUR_HI)
    c.ellipse(cx, top + 13, 7.4, 6.6, FACE_SH)                              # the blue face
    c.ellipse(cx - 0.4, top + 12.6, 6.8, 6, FACE)
    if not back:
        eyes = WHITE if hurt else (255, 240, 120, 255)
        for side in (-1, 1):
            ex = round(cx + side * 3)
            c.rect(ex - 1, top + 11, ex, top + 12, eyes)
            c.dot(ex, top + 12, EYE)
            c.line(ex - 3 * side, top + 8 + (1 if side < 0 else 0), ex + 2 * side, top + 9 - (1 if side < 0 else 0), FACE_SH)
        c.rect(round(cx) - 3, top + 16, round(cx) + 3, top + 17, EYE)         # the mouth, with two fangs
        c.rect(round(cx) - 3, top + 17, round(cx) - 2, top + 19, WHITE)
        c.rect(round(cx) + 2, top + 17, round(cx) + 3, top + 19, WHITE)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def yeti_die(stage, size=64):
    if stage < 2:
        return draw_yeti(bob=stage * 3, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 3
    c.ellipse(cx, ground - 9, 26, 10, FUR_SH)                               # flopped over in a heap
    c.ellipse(cx - 1, ground - 10, 25, 9, FUR)
    c.ellipse(cx - 8, ground - 14, 8, 3, FUR_HI)
    c.ellipse(cx + 14, ground - 12, 8, 6, FACE_SH)
    c.ellipse(cx + 14, ground - 12.4, 7, 5.2, FACE)
    c.dot(round(cx + 12), ground - 13, EYE)
    c.dot(round(cx + 16), ground - 13, EYE)
    for dx, dy in ((-16, -20), (0, -24), (14, -22), (-6, -16)):             # a flurry of snow
        c.dot(round(cx + dx), ground + dy, SNOW_HI)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def yeti_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_yeti(b), draw_yeti(b, bob=1), draw_yeti(b), draw_yeti(b, bob=1, shake=1)]),
            (f"Walk_{facing}", 4, True, [draw_yeti(b, step=3, bob=1), draw_yeti(b), draw_yeti(b, step=-3, bob=1), draw_yeti(b)]),
            (f"Attack_{facing}", 7, False, [draw_yeti(b, raise_arms=1, bob=-1), draw_yeti(b, raise_arms=1, bob=-2, shake=-1),
                                           draw_yeti(b, bob=2), draw_yeti(b)]),
            (f"Hurt_{facing}", 8, False, [draw_yeti(b, hurt=True, shake=-1)]),
        ]
    anims.append(("Die", 6, False, [yeti_die(s) for s in range(4)]))
    return anims


# ---------- Mr. Frost ----------
T = 48


def draw_frost(sway=0, talk=False, scarf=False, size=T):
    """A shivering snowman: three stacked snowballs, a carrot nose, coal eyes, stick arms, a little top hat.
    He's hunched against the cold and trembling (sway). With the scarf on he's rather pleased."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + sway * 0.6
    ground = size - 3
    c.ellipse(cx, ground - 9, 14, 10, SNOW_SH)                              # bottom ball
    c.ellipse(cx - 0.6, ground - 9.6, 13, 9.2, SNOW)
    c.ellipse(cx - 5, ground - 13, 4, 2.4, SNOW_HI)
    c.ellipse(cx + sway * 0.3, ground - 24, 10.4, 8.6, SNOW_SH)             # middle ball
    c.ellipse(cx + sway * 0.3 - 0.5, ground - 24.6, 9.6, 7.9, SNOW)
    c.ellipse(cx + sway * 0.6, ground - 35, 8.4, 7.4, SNOW_SH)              # head
    c.ellipse(cx + sway * 0.6 - 0.5, ground - 35.6, 7.7, 6.8, SNOW)
    c.ellipse(cx + sway * 0.6 - 3, ground - 38, 3, 1.6, SNOW_HI)
    hx = cx + sway * 0.6
    for side in (-1, 1):                                                    # stick arms
        ay = ground - 28 - (5 if talk and side > 0 else 0)
        c.line(round(cx + side * 9), ground - 24, round(cx + side * 19), round(ay), WOOD)
        c.line(round(cx + side * 9), ground - 23, round(cx + side * 19), round(ay) + 1, WOOD_SH)
        c.line(round(cx + side * 17), round(ay), round(cx + side * 20), round(ay) - 3, WOOD)
    for dy in (-27, -23, -19):                                              # coal buttons
        c.dot(round(cx + sway * 0.3), ground + dy - 3, COAL)
    for side in (-1, 1):                                                    # coal eyes
        c.dot(round(hx + side * 3), ground - 36, COAL)
        c.dot(round(hx + side * 3), ground - 35, COAL)
    c.line(round(hx), ground - 34, round(hx) + 6, ground - 33, CARROT)       # the carrot nose
    c.line(round(hx), ground - 33, round(hx) + 5, ground - 33, CARROT_SH)
    mouth = ground - 31
    if talk:
        c.rect(round(hx) - 2, mouth, round(hx) + 2, mouth + 1, COAL)
    else:
        for k in (-3, -1, 1, 3):                                            # a wobbly line of coal: chattering teeth
            c.dot(round(hx) + k, mouth + (1 if k in (-3, 3) else 0), COAL)
    c.rect(round(hx) - 7, ground - 41, round(hx) + 7, ground - 40, COAL)       # the top hat
    c.rect(round(hx) - 4, ground - 48, round(hx) + 4, ground - 41, COAL)
    c.rect(round(hx) - 4, ground - 43, round(hx) + 4, ground - 42, SCARF_RED)
    if scarf:                                                               # the red scarf, long and woolly
        c.rect(round(hx) - 7, ground - 30, round(hx) + 7, ground - 28, SCARF_RED)
        c.rect(round(hx) + 3, ground - 28, round(hx) + 6, ground - 20, SCARF_RED)
        for k in range(0, 8, 2):
            c.dot(round(hx) - 6 + k * 2, ground - 29, SCARF_SH)
    return c.img


# ---------- Granny Purl ----------

def draw_purl(sway=0, talk=False, size=T):
    """A knitting granny in a blue shawl and a woolly hat, round spectacles, grey hair in a bun, and a ball of wool
    with two needles in her lap."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + sway * 0.5
    ground = size - 3
    for dx in (-5, 5):
        c.ellipse(cx + dx, ground - 1, 4, 2, WOOD_SH)                          # sensible shoes
    for y in range(ground - 22, ground - 2):                                # the dress: a long bell
        t = (y - (ground - 22)) / 20
        half = 9 + t * 7
        c.rect(round(cx - half), y, round(cx + half), y, SHAWL_SH)
        c.rect(round(cx - half + 1), y, round(cx + half - 1), y, SHAWL)
        c.dot(round(cx - half + 1), y, SHAWL_HI)
    for y in range(ground - 24, ground - 14):                               # the shawl, over the shoulders
        half = 11 - (y - (ground - 24)) * 0.5
        c.rect(round(cx - half), y, round(cx + half), y, WOOL_SH)
        c.rect(round(cx - half + 1), y, round(cx + half - 1), y, WOOL)
    for x in range(round(cx - 9), round(cx + 9), 3):                        # fringe
        c.dot(x, ground - 13, WOOL_SH)
    c.ellipse(cx - 6, ground - 11, 5, 4, SCARF_SH)                           # the ball of wool in her lap
    c.ellipse(cx - 6.4, ground - 11.4, 4.4, 3.4, SCARF_RED)
    c.line(round(cx - 8), ground - 12, round(cx - 4), ground - 10, SCARF_SH)
    c.line(round(cx - 3), ground - 17, round(cx + 7), ground - 9, WOOD)         # the needles
    c.line(round(cx - 1), ground - 17, round(cx + 9), ground - 9, WOOD_SH)
    hx, hy = cx, ground - 28                                                # her head
    c.ellipse(hx, hy, 7, 7, SKIN_SH)
    c.ellipse(hx - 0.4, hy - 0.4, 6.4, 6.4, SKIN)
    for side in (-1, 1):                                                    # round spectacles
        c.ellipse(hx + side * 3, hy + 0.4, 2.6, 2.6, (130, 130, 160, 255))
        c.ellipse(hx + side * 3, hy + 0.4, 1.8, 1.8, (214, 236, 246, 255))
        c.dot(round(hx + side * 3), round(hy + 1), EYE)
    c.dot(round(hx), round(hy + 1), (130, 130, 160, 255))
    if talk:
        c.rect(round(hx) - 1, round(hy + 4), round(hx) + 1, round(hy + 5), SKIN_SH)
    else:
        c.line(round(hx) - 2, round(hy + 4), round(hx) + 2, round(hy + 4), SKIN_SH)
    c.ellipse(hx + 7, hy - 5, 3, 3, HAIR)                                    # a bun
    c.ellipse(hx, hy - 6, 8, 4, WOOL_SH)                                     # the woolly hat
    c.ellipse(hx, hy - 7, 7.4, 3.4, WOOL)
    c.rect(round(hx) - 7, round(hy - 4), round(hx) + 7, round(hy - 3), SCARF_RED)
    c.ellipse(hx, hy - 12, 2.4, 2.4, WHITE)
    return c.img


# ---------- Props ----------
P = 48


def draw_post(lit=False):
    """A tall wooden post wound with six bands of rainbow colour, with a ball on top. Lit, it glows and sparkles."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 7, 2.2, (60, 70, 100, 255))
    c.rect(21, g - 38, 26, g, WOOD_SH)
    c.rect(21, g - 38, 24, g, WOOD)
    for i, col in enumerate(RAINBOW):                                       # the coloured bands
        y = g - 6 - i * 5
        c.rect(20, y - 3, 27, y, col)
        c.rect(20, y, 27, y, tuple(max(0, v - 50) for v in col[:3]) + (255,))
    c.ellipse(23.5, g - 41, 4, 4, RAINBOW[5] if not lit else GLOW)
    c.dot(22, g - 42, WHITE)
    if lit:
        for dx, dy in ((-8, -30), (9, -34), (-6, -12), (10, -20), (0, -46)):
            c.dot(24 + dx, g + dy, GLOW)
            c.dot(24 + dx + 1, g + dy, GLOW)
            c.dot(24 + dx, g + dy + 1, GLOW)
            c.dot(24 + dx - 1, g + dy, WHITE)
            c.dot(24 + dx, g + dy - 1, WHITE)
    return c.img


def draw_igloo():
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 18, 3, SNOW_SH)
    c.ellipse(24, g - 9, 17, 13, SNOW_SH)
    c.ellipse(23.4, g - 9.6, 16.4, 12.4, SNOW)
    c.ellipse(17, g - 16, 6, 3, SNOW_HI)
    for y in (g - 6, g - 12, g - 17):                                       # block seams
        c.line(8, y, 40, y, SNOW_SH)
    for x in range(10, 40, 8):
        c.line(x, g - 12, x + 2, g - 6, SNOW_SH)
    c.ellipse(24, g - 4, 6, 4.4, (40, 56, 100, 255))                         # the door
    c.rect(18, g - 4, 30, g, (40, 56, 100, 255))
    c.ellipse(24, g - 4, 4.6, 3.4, (24, 34, 70, 255))
    return c.img


def draw_snowman():
    """A plain snowman, scenery: no hat, a carrot, and a smile."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g - 7, 11, 7.6, SNOW_SH)
    c.ellipse(23.6, g - 7.6, 10.4, 7, SNOW)
    c.ellipse(24, g - 18, 8, 6.4, SNOW_SH)
    c.ellipse(23.6, g - 18.6, 7.4, 5.8, SNOW)
    c.ellipse(24, g - 27, 6, 5.4, SNOW_SH)
    c.ellipse(23.6, g - 27.6, 5.4, 4.8, SNOW)
    for dx in (-2, 2):
        c.dot(24 + dx, g - 28, COAL)
    c.line(24, g - 26, 29, g - 25, CARROT)
    for k in (-2, -1, 0, 1, 2):
        c.dot(24 + k, g - 23 + (1 if abs(k) == 2 else 0), COAL)
    for dy in (-20, -16):
        c.dot(24, g + dy, COAL)
    c.line(14, g - 18, 6, g - 24, WOOD)
    c.line(34, g - 18, 42, g - 22, WOOD)
    return c.img


def draw_icespire():
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 13, 2.6, SNOW_SH)
    for x, base, w, h, lean in ((15, g, 5, 13, -2), (33, g, 5, 11, 2), (24, g, 8, 28, -1), (29, g, 5, 16, 2)):
        for dy in range(h):
            y = base - dy
            t = dy / h
            half = w / 2 if t < 0.6 else w / 2 * (1 - (t - 0.6) / 0.4)
            cxx = x + lean * t
            left, right = round(cxx - half), round(cxx + half - 0.01)
            for px in range(left, right + 1):
                u = (px - left) / max(1, right - left)
                col = ICE_HI if u < 0.3 else ICE if u < 0.55 else ICE_SH
                if t > 0.6 and u > 0.45:
                    col = ICE_SH
                c.dot(px, y, col)
    c.dot(22, g - 20, WHITE)
    c.dot(22, g - 19, WHITE)
    return c.img


def draw_drift():
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g - 1, 15, 3, SNOW_SH)
    c.ellipse(22, g - 6, 14, 8, SNOW_SH)
    c.ellipse(21.4, g - 6.6, 13.2, 7.4, SNOW)
    c.ellipse(17, g - 10, 5, 2, SNOW_HI)
    c.line(32, g - 4, 36, g - 28, WOOD)                                      # a ski pole, with a basket
    c.line(33, g - 4, 37, g - 28, WOOD_SH)
    c.ellipse(34.4, g - 9, 3, 1.2, COAL)
    c.rect(35, g - 30, 38, g - 27, SCARF_RED)
    return c.img


def draw_icepatch():
    """A glossy patch of blue ice lying flat on the ground (32x32, centre pivot): a puddle of ice a slime left behind."""
    c = Canvas(32)
    c.ellipse(15.5, 15.5, 14, 14, ICE_SH)
    c.ellipse(15, 15, 13, 13, ICE)
    c.ellipse(11, 10, 6, 3, ICE_HI)
    c.line(8, 20, 14, 23, ICE_HI)
    c.line(18, 8, 24, 12, ICE_SH)
    c.line(20, 22, 25, 18, ICE_SH)
    c.dot(22, 9, WHITE)
    c.dot(23, 9, WHITE)
    return c.img


def draw_snowball(frame):
    """A big lumpy snowball (32x32, centre pivot), rolling: the shadowed lumps turn."""
    c = Canvas(32)
    c.ellipse(15.5, 17, 13, 12, SNOW_SH)
    c.ellipse(15, 16, 12.2, 11.2, SNOW)
    c.ellipse(11, 11, 5, 3, SNOW_HI)
    a = frame * math.tau / 4
    for k in range(4):
        ang = a + k * math.tau / 4
        x, y = 15.5 + math.cos(ang) * 7, 16.5 + math.sin(ang) * 6
        c.ellipse(x, y, 2.6, 2.2, SNOW_SH)
        c.dot(round(x - 1), round(y - 1), SNOW_HI)
    return c.img


# ---------- Portraits ----------

def make_portraits():
    UI.mkdir(parents=True, exist_ok=True)
    portrait(draw_frost(talk=True).crop((8, 0, 40, 32)), (60, 84, 130)).save(UI / "PortraitMrFrost.png")
    portrait(draw_purl(talk=True).crop((8, 4, 40, 36)), (74, 70, 116)).save(UI / "PortraitPurl.png")


if __name__ == "__main__":
    make_ice_slime()
    write_sheet("SnowImp", "Attack", imp_animations())
    write_sheet("SnowYeti", "Attack", yeti_animations(), frame_size=64)
    write_sheet("MrFrost", None, [("Idle", 5, True, [draw_frost(sway=s) for s in (0, 1, 0, -1)]),
                                  ("Talk", 6, True, [draw_frost(sway=s, talk=True) for s in (0, 1)])], frame_size=T)
    write_sheet("Purl", None, [("Idle", 2, True, [draw_purl(sway=s) for s in (0, 1)]),
                               ("Talk", 6, True, [draw_purl(sway=s, talk=True) for s in (0, 1)])], frame_size=T)
    write_sheet("FrostProps", None, [
        ("RainbowPost", 3, True, [draw_post(False)]),
        ("RainbowPostLit", 3, True, [draw_post(True)]),
        ("Igloo", 1, False, [draw_igloo()]),
        ("Snowman", 1, False, [draw_snowman()]),
        ("IceSpire", 1, False, [draw_icespire()]),
        ("Drift", 1, False, [draw_drift()]),
    ], frame_size=P)
    write_sheet("IcePatch", None, [("Patch", 1, False, [draw_icepatch()])], pivot="center", frame_size=32, outline_color=None)
    write_sheet("Snowball", None, [("Roll", 8, True, [draw_snowball(f) for f in range(4)])], pivot="center", frame_size=32, outline_color=None)
    make_portraits()
    print("Wrote the Frost portraits")
