"""Generates the art for Puddlebrook Lake (Assets/Levels/Lake1-4.txt).

Run:  Tools/.venv/bin/python Tools/make_lake_sprites.py
Out:  Assets/Art/Crab.png / .json             32x32: a scuttling shore crab (it only ever walks sideways)
      Assets/Art/Jelly.png / .json            32x32: a drifting jellyfish that spits bubbles
      Assets/Art/KingCrabbington.png / .json  64x64: the Lake's boss, a great blue crab with a golden crown
      Assets/Art/KingShell.png / .json        64x64: his shell, shown while he's hiding (Shell)
      Assets/Art/Clamshell.png / .json        48x48 NPC: Captain Clamshell, a retired crab sailor (Idle, Talk)
      Assets/Art/UI/PortraitClamshell.png     32x32 dialogue portrait
      Assets/Art/UI/IconFish.png              24x24 quest-log picture of a fish
      Assets/Art/LakeProps.png / .json        48x48, bottom pivot:
        FishingSpot  a bucket and a rod on the planks     Reeds   a clump of cattails     Anchor  a rusty anchor
        LakeSign     a signpost                           Buoy    a red-and-white buoy
      Assets/Art/Bobber.png / .json           16x16 centre pivot: Float (2 frames) and Dip (2 frames)
      Assets/Art/BubbleBolt.png / .json       made by make_spell_sprites.py: the jellyfish's bubbles

Same conventions as make_woods_sprites.py: Front / Back for Idle, Walk, Attack and Hurt, a single Die, and
"Attack" as the action state.
"""
import math

from PIL import Image

from make_item_sprites import icon
from make_woods_sprites import portrait
from sprite_common import ART, CLEAR, Canvas, outline, tint, write_sheet

UI = ART / "UI"

# ---------- Palette: lake blues, coral and sand ----------
SHELL, SHELL_HI, SHELL_SH = (230, 108, 76, 255), (255, 160, 120, 255), (160, 60, 60, 255)
SAND, SAND_HI, SAND_SH = (226, 196, 140, 255), (246, 226, 176, 255), (170, 140, 100, 255)
INDIGO, INDIGO_HI, INDIGO_SH = (84, 104, 190, 255), (130, 156, 232, 255), (52, 62, 130, 255)
GOLD, GOLD_HI, GOLD_SH = (250, 204, 70, 255), (255, 238, 150, 255), (190, 136, 40, 255)
JELLY, JELLY_HI, JELLY_SH = (240, 170, 214, 255), (255, 224, 242, 255), (190, 110, 170, 255)
AQUA, AQUA_HI, AQUA_SH, AQUA_DEEP = (96, 214, 204, 255), (190, 250, 240, 255), (46, 150, 164, 255), (28, 96, 124, 255)
WATER, WATER_HI = (96, 188, 236, 255), (214, 244, 255, 255)
WOOD, WOOD_SH = (150, 104, 70, 255), (100, 68, 52, 255)
EYE = (52, 28, 54, 255)
WHITE = (255, 252, 244, 255)
IRON, IRON_SH = (130, 126, 140, 255), (84, 80, 98, 255)
RED = (214, 80, 96, 255)
REED, REED_HI, REED_SH = (118, 160, 84, 255), (170, 204, 112, 255), (74, 112, 66, 255)
CATTAIL = (128, 84, 62, 255)
NAVY, NAVY_HI = (52, 70, 120, 255), (84, 108, 164, 255)


def fade(img, keep):
    px = img.load()
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, int(a * keep))
    return img


# ---------- Crab ----------

def draw_crab(back=False, step=0, claws=0, squash=0, hurt=False, size=32):
    """A round orange shore crab, drawn side-on-ish: a wide shell, two stalk eyes, six legs and two claws.
    step shuffles the legs; claws 0 down, 1 up, 2 snapping wide."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    ground = size - 4
    top = ground - 11 + squash
    for k, side in enumerate((-1, 1)):                                    # legs: three a side
        for j in range(3):
            lx = cx + side * (6 + j * 2)
            sway = step if (j + k) % 2 == 0 else -step
            c.line(round(lx), round(ground - 5), round(lx + side * 3 + sway), ground, SHELL_SH)
    c.ellipse(cx, (top + ground) / 2, 11.5, (ground - top) / 2 + 1, SHELL_SH)       # the shell
    c.ellipse(cx - 0.6, (top + ground) / 2 - 0.8, 10.6, (ground - top) / 2, SHELL)
    c.ellipse(cx - 4, top + 2.5, 4.4, 1.8, SHELL_HI)
    for dx, dy in ((-4, 5), (3, 6), (0, 8)):                                  # speckles
        c.dot(round(cx + dx), top + dy, SHELL_SH)
    for side in (-1, 1):                                                  # claws
        ax = cx + side * 12
        ay = top + 3 - (4 if claws else 0)
        c.line(round(cx + side * 9), top + 6, round(ax), round(ay + 3), SHELL_SH)
        c.ellipse(ax + side * 2, ay, 4.2, 3.4, SHELL_SH)
        c.ellipse(ax + side * 2 - 0.4, ay - 0.4, 3.6, 2.8, SHELL)
        if claws == 2:                                                      # snapping wide: a notch cut in
            c.line(round(ax + side * 2), round(ay - 4), round(ax + side * 2), round(ay), CLEAR)
            c.dot(round(ax + side * 2), round(ay - 3), SHELL_SH)
        else:
            c.dot(round(ax + side * 3), round(ay + 1), SHELL_SH)
    if not back:
        for side in (-1, 1):                                              # stalk eyes
            ex = round(cx + side * 3)
            c.line(ex, top + 1, ex, top - 3, SHELL_SH)
            c.ellipse(ex, top - 4, 2.2, 2.2, WHITE)
            c.dot(ex, top - 4, EYE)
        c.line(round(cx) - 2, top + 8, round(cx) + 2, top + 8, EYE)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def crab_die(stage, size=32):
    if stage < 2:
        return draw_crab(squash=1 + stage * 3, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 4
    c.ellipse(cx, ground - 3, 11, 4, SHELL_SH)                              # flipped over, legs in the air
    c.ellipse(cx - 0.5, ground - 3.5, 10, 3.4, SHELL_HI)
    for k in range(-3, 4):
        c.line(round(cx + k * 3), ground - 5, round(cx + k * 3 + (1 if k % 2 else -1)), ground - 9, SHELL_SH)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def crab_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_crab(b), draw_crab(b, claws=1)]),
            (f"Walk_{facing}", 8, True, [draw_crab(b, step=2), draw_crab(b, step=-2, squash=1), draw_crab(b, step=2), draw_crab(b, step=-2, squash=1)]),
            (f"Attack_{facing}", 8, False, [draw_crab(b, claws=1), draw_crab(b, claws=2), draw_crab(b, claws=2), draw_crab(b)]),
            (f"Hurt_{facing}", 8, False, [draw_crab(b, squash=2, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [crab_die(s) for s in range(4)]))
    return anims


# ---------- Jelly ----------

def draw_jelly(back=False, pulse=0, spit=False, hurt=False, size=32):
    """A pale pink jellyfish: a glassy dome, a frill, and trailing tentacles. pulse 0..2 squeezes and relaxes the
    dome; spit puckers it up to blow a bubble."""
    c = Canvas(size)
    cx = size / 2 - 0.5
    top = 7 + (1 if pulse == 1 else 0)
    w = 9.5 - (1.5 if pulse == 2 else 0)
    h = 7.5 + (1 if pulse == 2 else 0) - (1 if pulse == 1 else 0)
    for k in range(-3, 4):                                                # tentacles, trailing and wavy
        tx = cx + k * 2.4
        for y in range(round(top + h), round(top + h + 12 - abs(k))):
            wob = math.sin((y + k * 2 + pulse * 2) * 0.8) * 1.2
            c.dot(round(tx + wob), y, JELLY_SH if (y + k) % 3 == 0 else JELLY)
    c.ellipse(cx, top + h - 1, w + 1, 3, JELLY_SH)                           # the frill
    c.ellipse(cx, top + h / 2, w, h, JELLY_SH)                               # the dome
    c.ellipse(cx - 0.6, top + h / 2 - 0.6, w - 1.2, h - 1.2, JELLY)
    c.ellipse(cx - 3.5, top + 2.5, 3, 1.5, JELLY_HI)
    if not back:
        eyes = WHITE if hurt else EYE
        for side in (-1, 1):
            c.dot(round(cx + side * 3), round(top + h / 2 + 1), eyes)
            c.dot(round(cx + side * 3), round(top + h / 2 + 2), eyes)
        if spit:
            c.ellipse(cx, top + h / 2 + 4, 1.6, 1.6, EYE)
        else:
            c.line(round(cx) - 1, round(top + h / 2 + 4), round(cx) + 1, round(top + h / 2 + 4), EYE)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def jelly_die(stage, size=32):
    if stage < 2:
        return draw_jelly(pulse=2, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 4
    c.ellipse(cx, ground - 1, 11, 2.4, JELLY_SH)                           # a puddle
    c.ellipse(cx, ground - 2, 9, 2, JELLY)
    for dx, dy in ((-6, -8), (4, -10), (0, -14), (8, -7)):                  # a few popped bubbles
        c.ellipse(cx + dx, ground + dy, 1.6, 1.6, WATER_HI)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def jelly_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_jelly(b, pulse=p) for p in (0, 1, 2, 1)]),
            (f"Walk_{facing}", 5, True, [draw_jelly(b, pulse=p) for p in (0, 1, 2, 1)]),
            (f"Attack_{facing}", 8, False, [draw_jelly(b, pulse=1), draw_jelly(b, spit=True), draw_jelly(b, spit=True, pulse=2), draw_jelly(b)]),
            (f"Hurt_{facing}", 8, False, [draw_jelly(b, pulse=2, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [jelly_die(s) for s in range(4)]))
    return anims


# ---------- King Crabbington ----------

def draw_king(back=False, bob=0, step=0, claws=0, shake=0, hurt=False, size=64):
    """A huge indigo crab with a golden crown, a stern brow and two great claws. claws 0 down, 1 up, 2 slammed."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + shake
    ground = size - 4
    top = 16 + bob
    for k, side in enumerate((-1, 1)):                                    # four legs a side
        for j in range(4):
            lx = cx + side * (12 + j * 3)
            sway = step if (j + k) % 2 == 0 else -step
            c.line(round(lx), round(ground - 10), round(lx + side * 5 + sway), ground, INDIGO_SH)
            c.line(round(lx) + 1, round(ground - 10), round(lx + side * 5 + sway) + 1, ground, INDIGO)
    c.ellipse(cx, top + 22, 25, 17, INDIGO_SH)                              # the carapace
    c.ellipse(cx - 1, top + 21, 24, 16, INDIGO)
    c.ellipse(cx - 9, top + 12, 10, 4, INDIGO_HI)
    for dx, dy in ((-14, 22), (-5, 28), (7, 24), (15, 29), (0, 16)):         # barnacles
        c.ellipse(cx + dx, top + dy, 2.4, 1.8, SAND)
        c.dot(round(cx + dx), top + dy, SAND_SH)
    for side in (-1, 1):                                                  # the great claws
        up = {0: 0, 1: 8, 2: -5}[claws]
        ax = cx + side * 22
        ay = top + 14 - up
        c.line(round(cx + side * 17), top + 24, round(ax), round(ay + 6), INDIGO_SH)
        c.line(round(cx + side * 17), top + 25, round(ax), round(ay + 7), INDIGO)
        c.ellipse(ax, ay, 8.4, 7.2, INDIGO_SH)
        c.ellipse(ax - side * 0.6, ay - 0.6, 7.6, 6.4, INDIGO)
        c.ellipse(ax - 2, ay - 3, 3, 1.6, INDIGO_HI)
        c.line(round(ax + side * 7), round(ay - 4), round(ax + side * 2), round(ay + 1), INDIGO_SH)   # the pincer's notch
    if not back:
        for side in (-1, 1):                                              # stalk eyes with heavy brows
            ex = round(cx + side * 12)
            c.line(ex, top + 6, ex, top - 1, INDIGO_SH)
            c.ellipse(ex, top - 3, 3.6, 3.6, WHITE if not hurt else (255, 255, 255, 255))
            c.dot(ex, top - 2, EYE)
            c.dot(ex, top - 3, EYE)
            c.line(ex - 3, top - 7 + (1 if side > 0 else 0), ex + 3, top - 6 - (1 if side > 0 else 0), NAVY)
        c.line(round(cx) - 6, top + 17, round(cx) + 6, top + 17, EYE)           # a stern mouth
        c.dot(round(cx) - 7, top + 16, EYE)
        c.dot(round(cx) + 7, top + 16, EYE)
    # The crown, balanced between the eyes.
    base = top - 2
    c.rect(round(cx) - 7, base - 3, round(cx) + 7, base, GOLD_SH)
    c.rect(round(cx) - 7, base - 3, round(cx) + 6, base - 1, GOLD)
    for dx in (-7, -3, 1, 5):
        c.rect(round(cx) + dx, base - 8, round(cx) + dx + 2, base - 3, GOLD)
        c.dot(round(cx) + dx + 1, base - 9, GOLD_HI)
    c.dot(round(cx), base - 2, RED)
    img = c.img
    return tint(img, (255, 70, 70), 0.35) if hurt else img


def king_die(stage, size=64):
    if stage < 2:
        return draw_king(bob=stage * 3, hurt=True, size=size)
    c = Canvas(size)
    cx, ground = size / 2 - 0.5, size - 4
    c.ellipse(cx, ground - 8, 25, 9, INDIGO_SH)                            # flipped over
    c.ellipse(cx - 1, ground - 9, 24, 8, INDIGO_HI)
    for k in range(-5, 6):
        c.line(round(cx + k * 4), ground - 12, round(cx + k * 4 + (2 if k % 2 else -2)), ground - 20, INDIGO_SH)
    c.rect(round(cx) - 7, ground - 22, round(cx) + 7, ground - 18, GOLD)       # the crown, fallen off
    for dx in (-7, -3, 1, 5):
        c.rect(round(cx) + dx, ground - 26, round(cx) + dx + 2, ground - 22, GOLD)
    img = c.img
    return fade(img, 0.5) if stage == 3 else img


def king_animations():
    anims = []
    for facing in ("Front", "Back"):
        b = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_king(b), draw_king(b, bob=1, claws=1), draw_king(b), draw_king(b, bob=1)]),
            (f"Walk_{facing}", 5, True, [draw_king(b, step=2, bob=1), draw_king(b), draw_king(b, step=-2, bob=1), draw_king(b)]),
            (f"Attack_{facing}", 7, False, [draw_king(b, claws=1, bob=-1), draw_king(b, claws=1, bob=-2, shake=-1),
                                           draw_king(b, claws=2, bob=2), draw_king(b)]),
            (f"Hurt_{facing}", 8, False, [draw_king(b, hurt=True, shake=-1)]),
        ]
    anims.append(("Die", 6, False, [king_die(s) for s in range(4)]))
    return anims


def draw_shell(size=64, frame=0):
    """His shell, closed: a big spiral conch with the golden crown poking out of the top. Frame 1 rocks a little."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + (1 if frame else -1)
    ground = size - 4
    c.ellipse(cx, ground - 1, 24, 4, (60, 60, 90, 255))
    c.ellipse(cx, ground - 22, 24, 21, INDIGO_SH)
    c.ellipse(cx - 0.8, ground - 22.8, 23, 20, SAND)
    c.ellipse(cx - 0.8, ground - 22.8, 17, 14.5, SAND_HI)
    c.ellipse(cx - 0.8, ground - 22.8, 11, 9.5, SAND)
    c.ellipse(cx - 0.8, ground - 22.8, 6, 5, SAND_SH)
    for a in range(0, 360, 40):                                           # spiral ridges
        ang = math.radians(a)
        c.line(round(cx - 0.8 + math.cos(ang) * 11), round(ground - 22.8 + math.sin(ang) * 9.5),
               round(cx - 0.8 + math.cos(ang) * 23), round(ground - 22.8 + math.sin(ang) * 20), SAND_SH)
    c.ellipse(cx - 10, ground - 33, 8, 3, WHITE)
    base = ground - 40                                                    # the crown on top
    c.rect(round(cx) - 7, base - 3, round(cx) + 7, base, GOLD_SH)
    c.rect(round(cx) - 7, base - 3, round(cx) + 6, base - 1, GOLD)
    for dx in (-7, -3, 1, 5):
        c.rect(round(cx) + dx, base - 8, round(cx) + dx + 2, base - 3, GOLD)
        c.dot(round(cx) + dx + 1, base - 9, GOLD_HI)
    c.dot(round(cx), base - 2, RED)
    return c.img


# ---------- Captain Clamshell ----------
T = 48


def draw_clamshell(sway=0, talk=False, size=T):
    """A retired crab sailor: a round orange crab in a navy captain's coat and cap, a corncob pipe, one big claw
    raised in a wave when he talks."""
    c = Canvas(size)
    cx = size / 2 - 0.5 + sway * 0.5
    ground = size - 3
    for k, side in enumerate((-1, 1)):
        for j in range(3):
            lx = cx + side * (6 + j * 3)
            c.line(round(lx), ground - 7, round(lx + side * 3), ground, SHELL_SH)
    c.ellipse(cx, ground - 12, 17, 11, SHELL_SH)                           # the body
    c.ellipse(cx - 0.8, ground - 12.8, 16, 10, SHELL)
    c.rect(round(cx) - 13, ground - 18, round(cx) + 13, ground - 6, NAVY)    # the captain's coat, over the shell
    c.rect(round(cx) - 13, ground - 18, round(cx) - 11, ground - 6, NAVY_HI)
    for y in (ground - 15, ground - 11, ground - 7):                        # gold buttons
        c.dot(round(cx), y, GOLD)
    c.line(round(cx) - 13, ground - 18, round(cx) + 13, ground - 18, GOLD_SH)
    wave = 8 if talk else 0
    for side in (-1, 1):                                                  # claws
        up = wave if side > 0 else 0
        ax, ay = cx + side * 16, ground - 16 - up
        c.line(round(cx + side * 12), ground - 14, round(ax), round(ay + 2), SHELL_SH)
        c.ellipse(ax + side * 1.5, ay - 1, 4.8, 4, SHELL_SH)
        c.ellipse(ax + side * 1.5 - 0.4, ay - 1.4, 4.2, 3.4, SHELL)
        c.line(round(ax + side * 5), round(ay - 3), round(ax + side * 2), round(ay), SHELL_SH)
    for side in (-1, 1):                                                  # stalk eyes, kindly
        ex = round(cx + side * 4)
        c.line(ex, ground - 22, ex, ground - 27, SHELL_SH)
        c.ellipse(ex, ground - 28, 2.8, 2.8, WHITE)
        c.dot(ex, ground - 27, EYE)
    c.line(round(cx) - 3, ground - 21, round(cx) + 3, ground - 21, EYE)      # a smile under a bushy moustache
    c.rect(round(cx) - 5, ground - 22, round(cx) + 5, ground - 22, (240, 236, 224, 255))
    c.line(round(cx) + 4, ground - 21, round(cx) + 8, ground - 23, WOOD)        # the pipe
    c.ellipse(cx + 9, ground - 24, 1.8, 1.8, WOOD_SH)
    c.ellipse(cx, ground - 33, 11, 3, NAVY_HI)                              # the cap: peak and crown
    c.ellipse(cx, ground - 35, 8, 4, NAVY)
    c.rect(round(cx) - 3, ground - 35, round(cx) + 3, ground - 34, GOLD)
    c.dot(round(cx), ground - 36, WHITE)
    if talk:
        c.dot(round(cx) + 7, ground - 27, (240, 240, 240, 140))               # a puff of pipe smoke
        c.dot(round(cx) + 8, ground - 30, (240, 240, 240, 100))
    return c.img


# ---------- Props ----------
P = 48


def draw_fishing_spot():
    """A tin bucket with a fish in it and a bamboo rod leaning on it, on a bit of rope."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 9, 2.4, (60, 50, 70, 255))
    c.rect(15, g - 10, 29, g - 1, IRON_SH)                                    # the bucket
    c.rect(16, g - 10, 28, g - 2, IRON)
    c.rect(16, g - 10, 18, g - 2, (190, 188, 204, 255))
    c.rect(14, g - 11, 30, g - 10, IRON_SH)
    c.line(16, g - 11, 24, g - 18, IRON_SH)                                    # the handle
    c.line(28, g - 11, 24, g - 18, IRON_SH)
    c.ellipse(22, g - 11, 4, 2, AQUA)                                         # a fish tail and back sticking out
    c.dot(25, g - 12, AQUA_SH)
    c.line(30, g - 2, 38, g - 34, (190, 150, 90, 255))                         # the rod
    c.line(31, g - 2, 39, g - 34, WOOD)
    c.line(38, g - 34, 44, g - 30, (230, 230, 240, 255))                       # the line
    c.line(44, g - 30, 44, g - 22, (230, 230, 240, 255))
    c.dot(44, g - 21, RED)
    return c.img


def draw_reeds(frame=0):
    """A clump of green reeds with brown cattails, swaying a little."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 10, 2.2, (70, 90, 80, 255))
    for i, (dx, h, tail) in enumerate(((-8, 20, True), (-3, 28, False), (2, 24, True), (7, 18, False), (11, 22, True))):
        lean = 1 if frame and i % 2 else 0
        x = 24 + dx
        c.line(x, g, x + lean + (1 if dx > 0 else -1), g - h, REED_SH)
        c.line(x + 1, g, x + lean + 2 + (1 if dx > 0 else -1), g - h, REED)
        if tail:
            tx = x + lean + (1 if dx > 0 else -1)
            c.rect(tx, g - h - 6, tx + 2, g - h, CATTAIL)
            c.dot(tx, g - h - 6, (84, 52, 44, 255))
        c.dot(x + lean, g - h + 4, REED_HI)
    return c.img


def draw_anchor():
    """A rusty anchor half-sunk in the sand."""
    c = Canvas(P)
    g = P - 4
    rust, rust_sh = (156, 96, 70, 255), (104, 60, 54, 255)
    c.ellipse(24, g, 11, 2.6, SAND_SH)
    c.rect(22, g - 26, 25, g - 3, rust_sh)
    c.rect(22, g - 26, 23, g - 3, rust)
    c.ellipse(24, g - 28, 3, 3, rust_sh)
    c.ellipse(24, g - 28, 1.6, 1.6, CLEAR)
    c.rect(16, g - 20, 31, g - 18, rust_sh)
    for side in (-1, 1):                                                    # the curved arms
        for t in range(0, 10):
            a = math.radians(t * 12)
            x = 24 + side * math.sin(a) * 11
            y = g - 3 - (1 - math.cos(a)) * 9 - 4
            c.dot(round(x), round(y), rust_sh)
            c.dot(round(x), round(y) - 1, rust)
        c.line(24 + side * 11, g - 8, 24 + side * 13, g - 12, rust_sh)
    c.dot(23, g - 24, (214, 140, 100, 255))
    return c.img


def draw_lake_sign():
    """A wooden signpost with an arrow pointing at the water."""
    c = Canvas(P)
    g = P - 4
    c.rect(22, g - 30, 25, g, WOOD_SH)
    c.rect(22, g - 30, 23, g, WOOD)
    c.ellipse(24, g, 7, 1.6, (60, 50, 70, 255))
    c.rect(10, g - 32, 36, g - 22, WOOD_SH)
    c.rect(11, g - 31, 35, g - 23, (214, 176, 120, 255))
    c.line(14, g - 27, 32, g - 27, (110, 80, 60, 255))
    c.line(28, g - 30, 33, g - 27, (110, 80, 60, 255))
    c.line(28, g - 24, 33, g - 27, (110, 80, 60, 255))
    return c.img


def draw_buoy():
    """A red-and-white striped buoy with a little flag."""
    c = Canvas(P)
    g = P - 4
    c.ellipse(24, g, 8, 2, (60, 50, 70, 255))
    c.ellipse(24, g - 9, 8.5, 9, (170, 50, 70, 255))
    c.ellipse(23.4, g - 9.6, 7.8, 8.4, RED)
    c.rect(16, g - 11, 32, g - 8, WHITE)
    c.ellipse(21, g - 14, 2.4, 1.6, (255, 180, 190, 255))
    c.line(24, g - 18, 24, g - 28, IRON_SH)
    c.rect(25, g - 28, 30, g - 24, (250, 204, 70, 255))
    return c.img


def draw_bobber(dip=False, frame=0):
    """A little red-and-white fishing float (16x16). dip pulls it half under with a ring of ripples."""
    c = Canvas(16)
    y = 9 + (3 if dip else 0)
    c.ellipse(7.5, 13, 6.5 if dip else 5, 1.6, (140, 210, 240, 200))             # a ripple ring
    if dip and frame:
        c.ellipse(7.5, 13, 7.5, 2.2, (190, 236, 252, 160))
    c.ellipse(7.5, y, 3.4, 3.4, WHITE)
    c.rect(4, y, 11, y + 4, RED)
    c.rect(4, y - 1, 11, y - 1, (170, 50, 70, 255))
    if not dip:
        c.line(8, y - 3, 8, y - 7, (230, 230, 240, 255))
    c.dot(6, y - 1, WHITE)
    return c.img


# ---------- Portrait and icon ----------

def fish_icon(big):
    n = 20
    c = Canvas(n)
    c.ellipse(9, 10, 7.4, 4.6, AQUA_SH)
    c.ellipse(8.6, 9.6, 6.8, 4, AQUA)
    c.ellipse(7, 8, 3.4, 1.4, AQUA_HI)
    c.line(14, 10, 18, 6, AQUA_SH)
    c.line(14, 10, 18, 14, AQUA_SH)
    c.line(18, 6, 18, 14, AQUA)
    c.dot(4, 9, EYE)
    c.dot(3, 9, WHITE)
    c.line(8, 12, 11, 14, AQUA_SH)
    return c.img


def make_portraits():
    UI.mkdir(parents=True, exist_ok=True)
    portrait(draw_clamshell(talk=True).crop((8, 0, 40, 32)), (40, 70, 100)).save(UI / "PortraitClamshell.png")
    icon(fish_icon).save(UI / "IconFish.png")


if __name__ == "__main__":
    write_sheet("Crab", "Attack", crab_animations())
    write_sheet("Jelly", "Attack", jelly_animations())
    write_sheet("KingCrabbington", "Attack", king_animations(), frame_size=64)
    write_sheet("KingShell", None, [("Shell", 3, True, [draw_shell(frame=f) for f in (0, 1)])], frame_size=64)
    write_sheet("Clamshell", None, [("Idle", 2, True, [draw_clamshell(sway=s) for s in (0, 1)]),
                                    ("Talk", 6, True, [draw_clamshell(sway=s, talk=True) for s in (0, 1)])], frame_size=T)
    write_sheet("LakeProps", None, [
        ("FishingSpot", 1, False, [draw_fishing_spot()]),
        ("Reeds", 2, True, [draw_reeds(0), draw_reeds(1)]),
        ("Anchor", 1, False, [draw_anchor()]),
        ("LakeSign", 1, False, [draw_lake_sign()]),
        ("Buoy", 1, False, [draw_buoy()]),
    ], frame_size=P)
    write_sheet("Bobber", None, [
        ("Float", 3, True, [draw_bobber(False, 0), draw_bobber(False, 1)]),
        ("Dip", 6, True, [draw_bobber(True, 0), draw_bobber(True, 1)]),
    ], pivot="center", frame_size=16, outline_color=None)
    make_portraits()
    print("Wrote the Lake's portrait and icon")
