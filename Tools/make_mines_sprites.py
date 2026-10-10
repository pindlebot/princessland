"""Generates the art for the Glimmer Mines (Assets/Levels/Mines1-4.txt).

Run:  Tools/.venv/bin/python Tools/make_mines_sprites.py
Out:  Assets/Art/Bat.png / .json            32x32: a cave bat (a fast flier; it hangs high above its shadow)
      Assets/Art/Pebblin.png / .json        32x32: a stubby rock creature with a little amber crystal on its head
      Assets/Art/CrystalGolem.png / .json   64x64: the Mines' boss, a rock giant with topaz crystals on his back
      Assets/Art/Digby.png / .json          48x48 NPC: Digby the mole foreman, in a hard hat with a lamp (Idle, Talk)
      Assets/Art/Molly.png, Mortimer.png, Mo.png   48x48 NPCs: the three lost moles (Idle, Talk)
      Assets/Art/UI/PortraitDigby.png, PortraitMole.png   32x32 dialogue portraits
      Assets/Art/MinesProps.png / .json     48x48, bottom pivot:
        Block         a heavy carved stone block (push it with the Mole Mitts)
        SoftDirt      a mound of loose soil with a little tuft (dig it with the Mole Mitts)
        WallCrystal   2 frames: a cluster of glowing blue crystals
        MineCart      a little iron cart on a stub of track
        MineSign      a "no bats" warning sign     MoleDoor   a tiny round door in a rock     PetRock   a rock with googly eyes
      Assets/Art/UI/IconMole.png            24x24 quest-log picture of a mole

Same conventions as make_woods_sprites.py: Front / Back for Idle, Walk, Attack and Hurt, a single Die, and
"Attack" as the action state.
"""
import math
import random

from PIL import Image

from make_item_sprites import icon
from make_woods_sprites import portrait
from sprite_common import ART, CLEAR, Canvas, outline, tint, write_sheet

UI = ART / "UI"

# ---------- Palette: slate and sandstone, with topaz amber and glowing blue crystals ----------
ROCK, ROCK_HI, ROCK_SH = (132, 122, 134, 255), (178, 168, 178, 255), (84, 76, 98, 255)
DEEP = (56, 48, 68, 255)
SAND, SAND_HI, SAND_SH = (176, 146, 112, 255), (212, 184, 146, 255), (122, 96, 80, 255)
AMBER, AMBER_HI, AMBER_SH = (244, 176, 52, 255), (255, 226, 130, 255), (186, 112, 38, 255)
ICE, ICE_HI, ICE_SH = (110, 200, 236, 255), (214, 244, 255, 255), (60, 130, 190, 255)
FUR, FUR_HI, FUR_SH = (126, 98, 86, 255), (166, 136, 118, 255), (86, 64, 62, 255)
SNOUT, SNOUT_HI = (240, 156, 156, 255), (255, 200, 196, 255)
HAT, HAT_HI, HAT_SH = (244, 200, 60, 255), (255, 232, 130, 255), (190, 140, 40, 255)
BAT, BAT_HI, BAT_SH = (96, 78, 120, 255), (140, 118, 164, 255), (62, 48, 84, 255)
EYE = (52, 28, 54, 255)
WHITE = (255, 252, 244, 255)
IRON, IRON_HI, IRON_SH = (110, 112, 128, 255), (170, 172, 188, 255), (66, 66, 84, 255)
WOOD, WOOD_SH = (150, 104, 70, 255), (100, 68, 52, 255)
RED = (214, 80, 96, 255)


def fade(img, keep):
    """Multiplies the opacity (the last Die frame)."""
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, int(a * keep))
    return img


def crystal(c, x, base, w, h, body, hi, sh, lean=0):
    """One pointed crystal standing on row `base` (the shape of the plague crystals, in any colour)."""
    for dy in range(h):
        y = base - dy
        t = dy / h
        half = w / 2 if t < 0.6 else w / 2 * (1 - (t - 0.6) / 0.4)
        cx = x + lean * t
        left, right = round(cx - half), round(cx + half - 0.01)
        for px in range(left, right + 1):
            u = (px - left) / max(1, right - left)
            col = hi if u < 0.3 else body if u < 0.55 else sh
            if t > 0.6 and u > 0.45:
                col = sh
            c.dot(px, y, col)


# ---------- Bat ----------

def draw_bat(back=False, flap=0, swoop=False, hurt=False, size=32):
    """A little purple bat, high in the frame (so its shadow sits well below it). flap 0 wings up, 1 level,
    2 down; swoop tucks the wings and shows the fangs."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    cy = 12 + (3 if swoop else 0)
    tilt = {0: -5, 1: 0, 2: 5}[flap] if not swoop else 4
    for side in (-1, 1):                                       # the wings: scalloped
        tip = (cx + side * 13, cy + tilt - 3)
        mid = (cx + side * 8, cy + tilt * 0.4 - 2)
        if swoop:
            tip, mid = (cx + side * 8, cy + 6), (cx + side * 5, cy + 2)
        for t in range(0, 11):
            f = t / 10
            x = cx + side * 3 + (tip[0] - cx - side * 3) * f
            y0 = cy - 1 + (tip[1] - cy + 1) * f
            y1 = y0 + 6 - 3 * f + (1 if t % 3 == 0 else 0)
            c.line(round(x), round(y0), round(x), round(y1), BAT_SH if t % 2 else BAT)
        c.line(round(cx + side * 3), round(cy), round(tip[0]), round(tip[1]), BAT_HI)
        c.dot(round(mid[0]), round(mid[1]) + 3, BAT_HI)
    c.ellipse(cx, cy + 1, 4.2, 4.6, BAT_SH)                     # the body
    c.ellipse(cx - 0.4, cy + 0.6, 3.6, 4.0, BAT)
    for side in (-1, 1):                                       # tall pointed ears
        c.line(round(cx + side * 3), round(cy - 3), round(cx + side * 4), round(cy - 7), BAT_SH)
        c.line(round(cx + side * 2), round(cy - 3), round(cx + side * 3), round(cy - 6), BAT_HI)
    if not back:
        eyes = WHITE if hurt else (255, 214, 90, 255)
        for side in (-1, 1):
            c.dot(round(cx + side * 2), round(cy), eyes)
            c.dot(round(cx + side * 2), round(cy) + 1, EYE)
        c.dot(round(cx), round(cy) + 2, SNOUT)
        if swoop:
            c.dot(round(cx) - 1, round(cy) + 3, WHITE)
            c.dot(round(cx) + 1, round(cy) + 3, WHITE)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def bat_die(stage, size=32):
    c = Canvas(size)
    cx = size / 2 - 0.5
    y = 14 + stage * 4
    c.ellipse(cx, y, 4, 4, BAT_SH)
    c.ellipse(cx - 0.4, y - 0.4, 3.4, 3.4, BAT)
    for side in (-1, 1):
        c.line(round(cx + side * 3), y, round(cx + side * 8), y - 2 + stage * 2, BAT_HI)
    for dx, dy in ((-6, -6), (6, -5), (0, -9)):                 # a puff of stars
        c.dot(round(cx + dx), y + dy + stage, AMBER_HI)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def bat_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 7, True, [draw_bat(b, flap=f) for f in (0, 1, 2, 1)]),
            (f"Walk_{facing}", 9, True, [draw_bat(b, flap=f) for f in (0, 1, 2, 1)]),
            (f"Attack_{facing}", 8, False, [draw_bat(b, flap=2), draw_bat(b, swoop=True), draw_bat(b, swoop=True), draw_bat(b, flap=0)]),
            (f"Hurt_{facing}", 8, False, [draw_bat(b, flap=1, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [bat_die(s) for s in range(4)]))
    return anims


# ---------- Pebblin ----------

def draw_pebblin(back=False, step=0, squash=0, lunge=0, hurt=False, size=32):
    """A knobbly grey rock with stubby legs, a grumpy face and a little amber crystal sprouting from its head."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    ground = size - 3
    top = 9 + squash
    for dx, st in ((-5, step), (5, -step)):                     # stumpy feet
        c.ellipse(cx + dx + st, ground - 1, 3.5, 2, ROCK_SH)
    c.ellipse(cx + lunge, (top + ground) / 2 - 1, 10.5, (ground - top) / 2 + 1, ROCK_SH)
    c.ellipse(cx + lunge - 0.8, (top + ground) / 2 - 1.8, 9.6, (ground - top) / 2 + 0.2, ROCK)
    c.ellipse(cx + lunge - 4, top + 3, 4, 2, ROCK_HI)
    for dx, dy in ((4, 6), (-6, 9), (1, 11), (6, 12)):          # cracks and pits
        c.dot(round(cx + lunge + dx), top + dy, ROCK_SH)
        c.dot(round(cx + lunge + dx) + 1, top + dy + 1, ROCK_SH)
    crystal(c, round(cx + lunge) + 2, top + 2, 4, 8, AMBER, AMBER_HI, AMBER_SH, lean=1)
    crystal(c, round(cx + lunge) - 2, top + 3, 3, 5, AMBER, AMBER_HI, AMBER_SH, lean=-1)
    if not back:
        ey = top + 7
        eyes = WHITE if hurt else EYE
        for side in (-1, 1):
            ex = round(cx + lunge + side * 4)
            c.rect(ex - 1, ey, ex, ey + 1, eyes)
            c.line(ex - 2 if side < 0 else ex - 1, ey - 2 if side < 0 else ey - 1, ex + 1 if side < 0 else ex + 2,
                   ey - 1 if side < 0 else ey - 2, EYE)
        c.rect(round(cx + lunge) - 2, ey + 4, round(cx + lunge) + 2, ey + 4, EYE)
        if lunge:
            c.rect(round(cx + lunge) - 2, ey + 4, round(cx + lunge) + 2, ey + 5, EYE)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def pebblin_die(stage, size=32):
    if stage < 2:
        return draw_pebblin(squash=2 + stage * 3, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 3
    for dx, dy, r in ((-7, -2, 3), (0, -3, 4), (7, -2, 3), (-3, -6, 2.4), (4, -7, 2)):    # a heap of pebbles
        c.ellipse(cx + dx, ground + dy, r, r * 0.8, ROCK_SH)
        c.ellipse(cx + dx - 0.5, ground + dy - 0.5, r - 0.8, (r - 0.8) * 0.8, ROCK)
    crystal(c, round(cx), ground - 6, 3, 5, AMBER, AMBER_HI, AMBER_SH)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def pebblin_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_pebblin(b), draw_pebblin(b, squash=1)]),
            (f"Walk_{facing}", 6, True, [draw_pebblin(b, step=2), draw_pebblin(b, squash=1), draw_pebblin(b, step=-2), draw_pebblin(b, squash=1)]),
            (f"Attack_{facing}", 8, False, [draw_pebblin(b, squash=3), draw_pebblin(b, lunge=3), draw_pebblin(b, lunge=2), draw_pebblin(b)]),
            (f"Hurt_{facing}", 8, False, [draw_pebblin(b, squash=2, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [pebblin_die(s) for s in range(4)]))
    return anims


# ---------- Crystal Golem ----------

def draw_golem(back=False, bob=0, step=0, raise_arms=0, shake=0, hurt=False, size=64):
    """A hulking rock giant: boulder shoulders, a small stern head with a heavy brow, and a spray of topaz
    crystals sprouting from his back and shoulders (the thing to hit when they glow)."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + shake
    ground = size - 3
    top = 8 + bob
    for dx in (-10, 10):                                        # stone legs
        s = step if dx > 0 else -step
        c.rect(round(cx + dx - 6 + s), ground - 14, round(cx + dx + 6 + s), ground, ROCK_SH)
        c.rect(round(cx + dx - 5 + s), ground - 14, round(cx + dx + 4 + s), ground - 1, ROCK)
        c.rect(round(cx + dx - 5 + s), ground - 14, round(cx + dx - 3 + s), ground - 1, ROCK_HI)
        c.ellipse(cx + dx + s, ground - 1, 8, 2.4, DEEP)
    c.ellipse(cx, top + 26, 21, 17, ROCK_SH)                    # the torso
    c.ellipse(cx - 1, top + 25, 20, 16, ROCK)
    c.ellipse(cx - 8, top + 17, 8, 4, ROCK_HI)
    for dx, dy in ((-9, 27), (6, 21), (11, 30), (-3, 33), (2, 27)):    # cracks with amber light in them
        c.line(round(cx + dx), top + dy, round(cx + dx + 3), top + dy + 3, ROCK_SH)
        c.dot(round(cx + dx + 1), top + dy + 1, AMBER)
    for side in (-1, 1):                                        # shoulders and fists
        sx = cx + side * 21
        c.ellipse(sx, top + 15, 9, 8, ROCK_SH)
        c.ellipse(sx - side * 0.8, top + 14, 8, 7, ROCK)
        c.ellipse(sx - 2, top + 11, 3, 2, ROCK_HI)
        ay = top + 22 - raise_arms * 9
        c.rect(round(sx - 4), min(top + 20, ay), round(sx + 4), max(top + 20, ay) + 12, ROCK_SH)
        c.rect(round(sx - 3 - side), min(top + 20, ay), round(sx + 3 - side), max(top + 20, ay) + 12, ROCK)
        c.ellipse(sx, ay + 14, 6.5, 5.5, ROCK_SH)
        c.ellipse(sx - 0.6, ay + 13.4, 5.8, 4.8, ROCK)
    crystal(c, round(cx - 12), top + 12, 6, 15, AMBER, AMBER_HI, AMBER_SH, lean=-4)      # the crystals
    crystal(c, round(cx + 12), top + 12, 6, 17, AMBER, AMBER_HI, AMBER_SH, lean=4)
    crystal(c, round(cx - 3), top + 10, 8, 21, AMBER, AMBER_HI, AMBER_SH, lean=-1)
    crystal(c, round(cx + 4), top + 11, 5, 14, AMBER, AMBER_HI, AMBER_SH, lean=2)
    c.ellipse(cx, top + 14, 8.5, 8.2, ROCK_SH)                  # the head
    c.ellipse(cx - 0.6, top + 13.4, 7.8, 7.4, ROCK)
    c.ellipse(cx - 3, top + 10, 3, 1.6, ROCK_HI)
    if not back:
        ey = top + 14
        eyes = WHITE if hurt else AMBER_HI
        for side in (-1, 1):
            ex = round(cx + side * 4)
            c.rect(ex - 1, ey, ex + 1, ey + 1, eyes)
            c.line(ex - 3 * side, ey - 3 + (1 if side < 0 else 0), ex + 2 * side, ey - 2 - (1 if side < 0 else 0), DEEP)
        c.rect(round(cx) - 3, ey + 5, round(cx) + 3, ey + 5, DEEP)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def golem_die(stage, size=64):
    if stage < 2:
        return draw_golem(bob=stage * 3, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 3
    for dx, dy, r in ((-18, -4, 8), (-6, -6, 10), (10, -5, 9), (21, -3, 6), (0, -14, 7), (-12, -13, 5), (13, -12, 5)):
        c.ellipse(cx + dx, ground + dy, r, r * 0.75, ROCK_SH)
        c.ellipse(cx + dx - 0.8, ground + dy - 0.8, r - 1.2, (r - 1.2) * 0.75, ROCK)
    for dx, h, lean in ((-14, 12, -2), (-2, 16, 0), (11, 11, 2)):                           # the crystals, cracked loose
        crystal(c, round(cx + dx), ground - 8, 5, h, AMBER, AMBER_HI, AMBER_SH, lean=lean)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def golem_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_golem(b), draw_golem(b, bob=1), draw_golem(b), draw_golem(b, bob=1, shake=1)]),
            (f"Walk_{facing}", 4, True, [draw_golem(b, step=3, bob=1), draw_golem(b), draw_golem(b, step=-3, bob=1), draw_golem(b)]),
            (f"Attack_{facing}", 7, False, [draw_golem(b, raise_arms=1, bob=-1), draw_golem(b, raise_arms=1, bob=-2, shake=-1),
                                           draw_golem(b, raise_arms=0, bob=2), draw_golem(b)]),
            (f"Hurt_{facing}", 8, False, [draw_golem(b, hurt=True, shake=-1)]),
        ]
    anims.append(("Die", 6, False, [golem_die(s) for s in range(4)]))
    return anims


# ---------- Moles ----------
T = 48


def draw_mole(fur=FUR, fur_hi=FUR_HI, fur_sh=FUR_SH, hat=None, lamp=False, tool="pick", talk=False, sway=0, scale=1.0,
              scarf=None, size=T):
    """A round, short-sighted mole on hind legs: a pink star-nose, tiny eyes behind little round spectacles, big pink
    digging paws. `hat` is a colour (a hard hat); `lamp` puts a lamp on it; `tool` is a pickaxe, or None."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + sway * 0.5
    ground = size - 3
    h = 1.0 + (scale - 1.0)
    body_top = ground - round(24 * scale)
    for dx in (-5, 5):                                          # feet
        c.ellipse(cx + dx * scale, ground - 1, 4.2 * scale, 2, fur_sh)
        c.ellipse(cx + dx * scale, ground - 2, 3.4 * scale, 1.5, SNOUT)
    bw = 10 * scale
    c.ellipse(cx, body_top + 13 * scale, bw, 11.5 * scale, fur_sh)                    # round tum
    c.ellipse(cx - 0.8, body_top + 12.4 * scale, bw - 1.2, 10.8 * scale, fur)
    c.ellipse(cx - 3.5 * scale, body_top + 7 * scale, 3.5 * scale, 2.4 * scale, fur_hi)
    c.ellipse(cx + 1, body_top + 16 * scale, 5.6 * scale, 5 * scale, fur_hi)         # paler belly
    if scarf:
        c.rect(round(cx - 8 * scale), round(body_top + 5 * scale), round(cx + 8 * scale), round(body_top + 7 * scale), scarf)
    arm_y = body_top + (4 if talk else 12) * scale
    for side in (-1, 1):                                        # arms with big pink digging paws
        ax = cx + side * (bw + 1)
        ay = arm_y if side < 0 or tool is None else body_top + 12 * scale
        c.line(round(cx + side * (bw - 2)), round(body_top + 10 * scale), round(ax), round(ay + 2), fur_sh)
        c.ellipse(ax, ay + 2, 3.6 * scale, 3.2 * scale, SNOUT_HI)
        for k in (-1, 0, 1):
            c.dot(round(ax + k * 1.5), round(ay + 5 * scale), SNOUT)
    if tool == "pick":                                          # a pickaxe over the right shoulder
        px = cx + bw + 1
        c.line(round(px), round(body_top + 14 * scale), round(px - 2), round(body_top - 2), WOOD)
        c.line(round(px - 1), round(body_top + 14 * scale), round(px - 3), round(body_top - 2), WOOD_SH)
        c.line(round(px - 9), round(body_top - 3), round(px + 5), round(body_top - 1), IRON)
        c.line(round(px - 9), round(body_top - 2), round(px + 5), round(body_top), IRON_SH)
        c.dot(round(px - 10), round(body_top - 2), IRON_HI)
    hx, hy = cx, body_top + 3 * scale                           # the head
    c.ellipse(hx, hy + 1, 8.4 * scale, 7.4 * scale, fur_sh)
    c.ellipse(hx - 0.6, hy + 0.4, 7.7 * scale, 6.8 * scale, fur)
    c.ellipse(hx - 3, hy - 2, 2.6, 1.4, fur_hi)
    c.ellipse(hx, hy + 5.2 * scale, 4 * scale, 3.4 * scale, fur_hi)                   # the muzzle
    c.ellipse(hx, hy + 4.2 * scale, 2.6 * scale, 2.2 * scale, SNOUT)                  # the star-nose
    c.dot(round(hx - 1), round(hy + 3.4 * scale), SNOUT_HI)
    for k in range(5):
        a = k * math.tau / 5 - math.pi / 2
        c.dot(round(hx + math.cos(a) * 3.2 * scale), round(hy + 4.2 * scale + math.sin(a) * 3 * scale), SNOUT)
    for side in (-1, 1):                                        # tiny eyes behind round glasses
        ex = round(hx + side * 4 * scale)
        c.ellipse(ex, hy + 0.6, 2.7, 2.7, (96, 110, 130, 255))
        c.ellipse(ex, hy + 0.6, 1.9, 1.9, (210, 232, 244, 255))
        c.dot(ex, round(hy + 1), EYE)
    c.dot(round(hx), round(hy + 1), (96, 110, 130, 255))
    if talk:
        c.rect(round(hx) - 1, round(hy + 7.4 * scale), round(hx) + 1, round(hy + 8.4 * scale), EYE)
    if hat:                                                     # a hard hat, brim and ridge
        hi = tuple(min(255, v + 36) for v in hat[:3]) + (255,)
        sh = tuple(max(0, v - 52) for v in hat[:3]) + (255,)
        c.ellipse(hx, hy - 5 * scale, 9.4 * scale, 5.4 * scale, sh)
        c.ellipse(hx - 0.4, hy - 5.6 * scale, 8.8 * scale, 4.8 * scale, hat)
        c.rect(round(hx - 11 * scale), round(hy - 2.4 * scale), round(hx + 11 * scale), round(hy - 1 * scale), sh)
        c.rect(round(hx - 10 * scale), round(hy - 2.8 * scale), round(hx + 10 * scale), round(hy - 2.2 * scale), hat)
        c.rect(round(hx - 1), round(hy - 10 * scale), round(hx), round(hy - 6 * scale), hi)
        if lamp:
            c.ellipse(hx, hy - 6 * scale, 2.4, 2.4, IRON_SH)
            c.ellipse(hx, hy - 6 * scale, 1.6, 1.6, (255, 244, 170, 255))
    return c.img


MOLES = {
    # name: (fur, fur_hi, fur_sh, hat, scarf)
    "Molly": ((176, 140, 120, 255), (214, 182, 160, 255), (128, 94, 88, 255), None, (214, 80, 96, 255)),
    "Mortimer": ((96, 92, 112, 255), (140, 136, 156, 255), (62, 60, 82, 255), (96, 168, 214, 255), None),
    "Mo": ((146, 120, 88, 255), (188, 162, 124, 255), (102, 80, 66, 255), None, (110, 190, 120, 255)),
}


def mole_frames(name, talk, k):
    fur, hi, sh, hat, scarf = MOLES[name]
    return draw_mole(fur, hi, sh, hat=hat, tool=None, talk=talk, sway=k, scarf=scarf, scale=0.85)


# ---------- Props ----------
P = 48


def draw_block():
    """A heavy, square stone block with chiselled corners and a pale scuffed top."""
    c = Canvas(P)
    cx, ground = 23.5, P - 4
    c.rect(8, ground - 25, 39, ground, ROCK_SH)
    c.rect(8, ground - 25, 38, ground - 1, ROCK)
    c.rect(8, ground - 25, 39, ground - 21, ROCK_HI)                                  # the top face, lit
    c.rect(9, ground - 21, 38, ground - 20, ROCK_SH)
    c.rect(8, ground - 20, 10, ground, ROCK_HI)
    c.rect(37, ground - 20, 39, ground, ROCK_SH)
    c.line(14, ground - 12, 20, ground - 8, ROCK_SH)                                  # cracks
    c.line(20, ground - 8, 19, ground - 3, ROCK_SH)
    c.line(28, ground - 17, 33, ground - 14, ROCK_SH)
    for x, y in ((13, ground - 23), (25, ground - 22), (33, ground - 24)):            # scuffs on the top
        c.dot(x, y, WHITE)
    for dx in (-8, 8):                                                               # two chiselled arrow notches
        c.line(round(cx + dx) - 2, ground - 8, round(cx + dx), ground - 11, DEEP)
        c.line(round(cx + dx) + 2, ground - 8, round(cx + dx), ground - 11, DEEP)
    c.ellipse(cx, ground, 17, 2.4, DEEP)
    return c.img


def draw_softdirt(frame=0):
    """A mound of loose, dark soil with a few pebbles and one wilting tuft: it looks dug before."""
    c = Canvas(P)
    cx, ground = 23.5, P - 4
    soil, hi, sh = (122, 84, 66, 255), (166, 118, 90, 255), (82, 54, 50, 255)
    c.ellipse(cx, ground, 16, 3, sh)
    c.ellipse(cx, ground - 6, 15, 9, sh)
    c.ellipse(cx - 0.6, ground - 7, 14, 8.2, soil)
    c.ellipse(cx - 5, ground - 11, 6, 2.6, hi)
    for dx, dy in ((-6, -4), (4, -6), (8, -2), (-1, -9), (-10, -1)):
        c.dot(round(cx + dx), ground + dy, sh)
        c.dot(round(cx + dx) + 1, ground + dy, hi)
    for dx, dy in ((-3, -3), (6, -9)):                                               # pebbles
        c.ellipse(cx + dx, ground + dy, 1.8, 1.3, ROCK_HI)
    c.line(round(cx + 2), ground - 12, round(cx + 2 + (1 if frame else 0)), ground - 17, (110, 160, 90, 255))     # a tuft
    c.line(round(cx + 3), ground - 12, round(cx + 5 - (1 if frame else 0)), ground - 16, (150, 196, 108, 255))
    c.line(round(cx + 1), ground - 12, round(cx - 1), ground - 15, (110, 160, 90, 255))
    return c.img


def draw_wall_crystal(frame=0):
    """A cluster of glowing blue crystals growing from the floor. Frame 1 moves the glint."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 13, 2.6, DEEP)
    crystal(c, 14, g, 5, 13, ICE, ICE_HI, ICE_SH, lean=-2)
    crystal(c, 33, g, 5, 11, ICE, ICE_HI, ICE_SH, lean=2)
    crystal(c, 23, g, 8, 24, ICE, ICE_HI, ICE_SH, lean=-1)
    crystal(c, 29, g, 5, 15, ICE, ICE_HI, ICE_SH, lean=2)
    gx, gy = (22, g - 19) if frame else (24, g - 11)
    c.dot(gx, gy, WHITE)
    c.dot(gx, gy + 1, ICE_HI)
    if frame:
        c.dot(gx - 1, gy, ICE_HI)
        c.dot(gx + 1, gy, ICE_HI)
    return c.img


def draw_minecart():
    """A little iron mine cart on a stub of track, heaped with amber ore."""
    c = Canvas(P)
    g = P - 4
    for x in range(2, 46, 6):                                                        # sleepers
        c.rect(x, g - 1, x + 3, g, WOOD_SH)
    c.rect(1, g - 3, 46, g - 2, IRON_SH)                                             # rails
    c.rect(1, g - 4, 46, g - 4, IRON_HI)
    for x in (13, 34):                                                               # wheels
        c.ellipse(x, g - 5, 4.4, 4.4, IRON_SH)
        c.ellipse(x, g - 5, 3.4, 3.4, IRON)
        c.dot(x, g - 5, IRON_HI)
    c.rect(7, g - 20, 40, g - 8, IRON_SH)                                            # the box, tapering to the bottom
    c.rect(8, g - 20, 39, g - 9, IRON)
    c.rect(8, g - 20, 39, g - 18, IRON_HI)
    c.rect(8, g - 14, 39, g - 13, IRON_SH)                                           # a rivet band
    for x in range(11, 38, 6):
        c.dot(x, g - 16, IRON_HI)
        c.dot(x, g - 11, IRON_HI)
    for dx, dy, r in ((-8, -22, 4), (-1, -24, 5), (7, -22, 4), (14, -21, 3)):         # the ore heaped on top
        c.ellipse(24 + dx, g + dy + 2, r, r * 0.8, AMBER_SH)
        c.ellipse(24 + dx - 0.6, g + dy + 1.4, r - 1, (r - 1) * 0.8, AMBER)
        c.dot(24 + dx - 1, g + dy, AMBER_HI)
    return c.img


def draw_sign():
    """A wooden warning sign on a post, with a bat drawn on it and a red cross over the bat."""
    c = Canvas(P)
    g = P - 4
    c.rect(22, g - 14, 25, g, WOOD_SH)
    c.rect(22, g - 14, 23, g, WOOD)
    c.rect(10, g - 32, 38, g - 14, WOOD_SH)
    c.rect(11, g - 31, 37, g - 15, (214, 176, 120, 255))
    c.rect(11, g - 31, 37, g - 30, (236, 204, 150, 255))
    c.ellipse(24, g - 23, 5, 3, BAT)                                   # a little bat
    for side in (-1, 1):
        c.line(24 + side * 3, g - 24, 24 + side * 8, g - 27, BAT_SH)
        c.line(24 + side * 3, g - 23, 24 + side * 8, g - 21, BAT_SH)
    c.dot(22, g - 24, (255, 214, 90, 255))
    c.dot(26, g - 24, (255, 214, 90, 255))
    c.line(13, g - 29, 35, g - 17, RED)                               # NO!
    c.line(13, g - 28, 35, g - 16, RED)
    c.line(35, g - 29, 13, g - 17, RED)
    c.line(35, g - 28, 13, g - 16, RED)
    c.ellipse(24, g, 7, 1.6, DEEP)
    return c.img


def draw_moledoor():
    """A tiny round door set into a rock, with a welcome mat and a little brass knob."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g - 8, 14, 11, ROCK_SH)                              # the boulder around it
    c.ellipse(23, g - 9, 13, 10, ROCK)
    c.ellipse(19, g - 14, 5, 2.4, ROCK_HI)
    c.ellipse(24, g - 7, 7.5, 7.5, WOOD_SH)                            # the door
    c.ellipse(24, g - 7, 6.5, 6.5, WOOD)
    for x in (20, 22, 24, 26, 28):
        c.line(x, g - 13, x, g - 1, WOOD_SH)
    c.dot(27, g - 6, (255, 226, 130, 255))
    c.dot(27, g - 5, (196, 118, 36, 255))
    c.rect(18, g, 30, g, (196, 80, 96, 255))                           # the welcome mat
    c.rect(19, g + 1, 29, g + 1, (150, 50, 78, 255))
    return c.img


def draw_petrock():
    """A smooth grey pebble with two googly eyes, and a tiny flower beside it."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 10, 2.4, DEEP)
    c.ellipse(24, g - 6, 9, 6.5, ROCK_SH)
    c.ellipse(23.4, g - 6.6, 8.2, 5.8, ROCK)
    c.ellipse(20, g - 9, 3.2, 1.6, ROCK_HI)
    for ex in (21, 27):
        c.ellipse(ex, g - 8, 2.6, 2.6, WHITE)
        c.dot(ex + 1, g - 7, EYE)
        c.dot(ex + 1, g - 8, EYE)
    c.line(23, g - 3, 26, g - 3, EYE)
    c.line(35, g, 35, g - 7, (110, 160, 90, 255))                     # the flower
    c.ellipse(35, g - 8, 2, 2, (246, 150, 150, 255))
    c.dot(35, g - 8, (255, 226, 130, 255))
    return c.img


# ---------- Portraits and icons ----------

def mole_icon(big):
    return draw_mole(talk=False, tool=None, scale=0.9).crop((8, 1, 40, 33)).resize((20, 20), Image.NEAREST)


def make_portraits():
    UI.mkdir(parents=True, exist_ok=True)
    digby = draw_mole(hat=HAT, lamp=True, tool=None, talk=True).crop((8, 0, 40, 32))
    portrait(digby, (60, 48, 70)).save(UI / "PortraitDigby.png")
    mole = mole_frames("Molly", True, 0).crop((8, 0, 40, 32))
    portrait(mole, (56, 56, 76)).save(UI / "PortraitMole.png")
    icon(mole_icon).save(UI / "IconMole.png")


if __name__ == "__main__":
    write_sheet("Bat", "Attack", bat_animations())
    write_sheet("Pebblin", "Attack", pebblin_animations())
    write_sheet("CrystalGolem", "Attack", golem_animations(), frame_size=64)
    write_sheet("Digby", None, [("Idle", 2, True, [draw_mole(hat=HAT, lamp=True, sway=s) for s in (0, 1)]),
                                ("Talk", 6, True, [draw_mole(hat=HAT, lamp=True, talk=True, sway=s) for s in (0, 1)])], frame_size=T)
    for name in MOLES:
        write_sheet(name, None, [("Idle", 2, True, [mole_frames(name, False, k) for k in (0, 1)]),
                                 ("Talk", 6, True, [mole_frames(name, True, k) for k in (0, 1)])], frame_size=T)
    write_sheet("MinesProps", None, [
        ("Block", 1, False, [draw_block()]),
        ("SoftDirt", 2, True, [draw_softdirt(0), draw_softdirt(1)], None),
        ("WallCrystal", 2, True, [draw_wall_crystal(0), draw_wall_crystal(1)]),
        ("MineCart", 1, False, [draw_minecart()]),
        ("MineSign", 1, False, [draw_sign()]),
        ("MoleDoor", 1, False, [draw_moledoor()]),
        ("PetRock", 1, False, [draw_petrock()]),
    ], frame_size=P)
    make_portraits()
    print("Wrote the Mines' portraits and icon")
