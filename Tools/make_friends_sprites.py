"""Generates the friendly characters and the dungeon's extra props.

Run:  Tools/.venv/bin/python Tools/make_friends_sprites.py
Out:  Assets/Art/Mermaid.png / .json    (48x48)  Coralie on her rock in the castle-grounds pond
        Idle  4 frames, looping   hair sways, tail fin flicks, a blink
        Talk  2 frames, looping   mouth open and a little wave
      Assets/Art/Bonesy.png / .json     (32x32)  the friendly skeleton by the dungeon campfire
        Idle  2 frames, looping   a gentle bob (and a cozy red scarf)
        Talk  2 frames, looping   jaw open and a wave
      Assets/Art/Frog.png / .json       (32x32)  Sir Hopsalot, who hides in a bush (with a tiny crown)
        Idle  2 frames, looping   throat puffs
        Hop   2 frames, looping   crouch and leap
      Assets/Art/DungeonProps.png / .json (32x32, bottom pivot)
        Campfire 4 (looping) · Barrel · Crate · Bones · Mushrooms 2 (glow pulse) ·
        Door_Closed · Door_Open · Door_Locked · Key 2 (glint)
      Assets/Art/WaterDecals.png / .json (32x32, centre pivot; laid flat on the floor)
        Ripple 4 (looping) · Lily 1 · LavaBubble 6 (looping; a bubble swells and pops on the lava)
      Assets/Art/UI/PortraitMermaid.png, PortraitBonesy.png   32x32 dialogue portraits
"""
import math
import random

from PIL import Image

from sprite_common import ART, Canvas, outline, write_sheet

# ---------- Coralie the mermaid ----------

SKIN, SKIN_SH = (250, 214, 180, 255), (222, 172, 140, 255)
HAIR, HAIR_SH, HAIR_HI = (236, 96, 120, 255), (184, 60, 92, 255), (255, 150, 160, 255)
TAIL, TAIL_SH, TAIL_HI = (60, 190, 170, 255), (30, 140, 130, 255), (140, 236, 214, 255)
FIN, FIN_SH = (120, 230, 210, 255), (60, 170, 170, 255)
SHELL, SHELL_SH = (250, 170, 210, 255), (210, 120, 170, 255)
ROCK, ROCK_SH, ROCK_HI = (128, 132, 150, 255), (88, 90, 110, 255), (170, 176, 192, 255)
WATER, WATER_HI = (90, 170, 220, 210), (200, 240, 255, 230)
STAR = (255, 210, 80, 255)
EYE, BLUSH, MOUTH = (40, 30, 60, 255), (250, 150, 160, 255), (170, 60, 80, 255)
WHITE = (255, 255, 255, 255)

M = 48


def draw_mermaid(sway=0, fin=0, blink=False, talk=False):
    c = Canvas(M)
    # The rock she sits on, and the pond lapping at it.
    c.ellipse(24, 40, 15, 7, ROCK_SH)
    c.ellipse(23, 39, 14, 6, ROCK)
    c.ellipse(19, 37, 6, 2, ROCK_HI)

    # Tail: from her hips, curling over the rock to the right; the fin flicks.
    for i in range(14):
        t = i / 13
        x = 24 + t * 13
        y = 31 + math.sin(t * math.pi) * 4 - t * 2
        r = 4.2 - t * 2.6
        c.ellipse(x, y, r, r * 0.9, TAIL_SH)
        c.ellipse(x - 0.4, y - 0.5, r * 0.85, r * 0.7, TAIL)
        if i % 3 == 1:
            c.dot(round(x), round(y - 1), TAIL_HI)  # shiny scales
    fx, fy = 38, 30 - fin
    c.ellipse(fx + 2, fy - 2, 3, 1.6, FIN_SH)
    c.ellipse(fx + 2, fy + 2, 3, 1.6, FIN_SH)
    c.ellipse(fx + 1.5, fy - 2, 2.4, 1.1, FIN)
    c.ellipse(fx + 1.5, fy + 2, 2.4, 1.1, FIN)

    # Hair falls behind her shoulders (drawn before the body).
    for side in (-1, 1):
        for y in range(10, 30):
            x = 24 + side * (6 + (y - 10) * 0.12) + sway * ((y - 10) / 20)
            c.rect(round(x) - 1, y, round(x) + 1, y, HAIR_SH if y > 24 else HAIR)

    # Body: torso, shell top, arms resting on the rock (one waves while she talks).
    c.rect(20, 19, 28, 30, SKIN)
    c.rect(27, 19, 28, 30, SKIN_SH)
    c.ellipse(22, 23, 2.2, 1.8, SHELL)
    c.ellipse(26, 23, 2.2, 1.8, SHELL)
    c.dot(22, 24, SHELL_SH)
    c.dot(26, 24, SHELL_SH)
    c.line(19, 20, 16, 30, SKIN)
    c.line(18, 20, 15, 30, SKIN_SH)
    if talk:
        c.line(29, 20, 33, 13, SKIN)  # waving
        c.ellipse(33.5, 12, 1.5, 1.5, SKIN)
    else:
        c.line(29, 20, 32, 30, SKIN)
        c.line(30, 20, 33, 30, SKIN_SH)

    # Head, hair on top, a starfish clip.
    c.ellipse(24, 12, 6, 6.5, SKIN)
    c.ellipse(24, 7, 7, 4, HAIR)
    c.rect(17, 7, 19, 15, HAIR)
    c.rect(29, 7, 31, 15, HAIR)
    c.ellipse(22, 6, 3, 1.5, HAIR_HI)
    for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1), (0, 1)):
        c.dot(19 + dx, 7 + dy, STAR)
    if blink:
        c.rect(21, 12, 22, 12, EYE)
        c.rect(26, 12, 27, 12, EYE)
    else:
        for ex in (21, 26):
            c.rect(ex, 11, ex + 1, 13, EYE)
            c.dot(ex, 11, WHITE)
    c.dot(20, 14, BLUSH)
    c.dot(28, 14, BLUSH)
    if talk:
        c.rect(23, 15, 25, 16, MOUTH)
    else:
        c.dot(23, 15, MOUTH)
        c.dot(24, 16, MOUTH)
        c.dot(25, 15, MOUTH)

    # The water line in front of the rock, with a glint.
    c.ellipse(24, 45, 21, 2.6, WATER)
    c.rect(14 + sway, 44, 17 + sway, 44, WATER_HI)
    c.rect(30 - sway, 45, 32 - sway, 45, WATER_HI)
    return c.img


# ---------- Bonesy ----------

BONE, BONE_SH = (232, 226, 206, 255), (178, 170, 150, 255)
SOCKET = (34, 22, 34, 255)
KIND_EYE = (110, 200, 255, 255)
SCARF, SCARF_SH = (214, 60, 60, 255), (160, 36, 44, 255)


def draw_bonesy(bob=0, talk=False):
    c = Canvas()
    b = bob
    for x in (12, 18):  # legs
        c.rect(x, 25, x + 1, 29, BONE)
        c.rect(x - 1, 30, x + 1, 30, BONE_SH)
    c.rect(12, 21 + b, 19, 22 + b, BONE_SH)  # pelvis
    c.rect(15, 13 + b, 16, 20 + b, BONE)
    for y in (14, 16, 18):  # ribs
        c.rect(12, y + b, 19, y + b, BONE)
        c.rect(13, y + 1 + b, 18, y + 1 + b, SOCKET)
    c.rect(15, 15 + b, 16, 19 + b, BONE)
    c.rect(11, 13 + b, 20, 13 + b, BONE)
    c.rect(10, 14 + b, 10, 20 + b, BONE)  # left arm
    if talk:
        c.line(21, 13 + b, 24, 7 + b, BONE)  # waving
        c.rect(24, 5 + b, 25, 6 + b, BONE)
    else:
        c.rect(21, 14 + b, 21, 20 + b, BONE)
    # Skull with friendly blue eyes and a smile.
    c.rect(13, 4 + b, 18, 4 + b, BONE)
    c.rect(12, 5 + b, 19, 9 + b, BONE)
    c.rect(13, 10 + b, 18, 10 + b, BONE)
    for sx in (13, 17):
        c.rect(sx, 6 + b, sx + 1, 8 + b, SOCKET)
        c.dot(sx + 1, 7 + b, KIND_EYE)
    c.dot(15, 9 + b, SOCKET)
    c.dot(16, 9 + b, SOCKET)
    if talk:
        c.rect(14, 11 + b, 17, 12 + b, SOCKET)
    else:
        for x in range(13, 19):
            c.dot(x, 11 + b, BONE if x % 2 else SOCKET)
    # A cozy scarf.
    c.rect(11, 12 + b, 20, 13 + b, SCARF)
    c.rect(17, 14 + b, 18, 18 + b, SCARF)
    c.rect(17, 18 + b, 18, 18 + b, SCARF_SH)
    c.dot(13, 13 + b, SCARF_SH)
    c.dot(16, 13 + b, SCARF_SH)
    return c.img


# ---------- Sir Hopsalot ----------

FROG, FROG_SH, FROG_BELLY = (110, 190, 80, 255), (70, 140, 60, 255), (200, 230, 140, 255)
GOLD, GOLD_HI = (236, 192, 70, 255), (255, 244, 170, 255)


def draw_frog(puff=0, hop=0):
    c = Canvas()
    y = 27 - hop * 5
    if hop:  # legs stretched out behind and below
        c.line(11, y + 3, 8, 30, FROG_SH)
        c.line(21, y + 3, 24, 30, FROG_SH)
    else:
        c.ellipse(10, 29, 3, 1.5, FROG_SH)
        c.ellipse(22, 29, 3, 1.5, FROG_SH)
    c.ellipse(16, y, 6.5, 4.2, FROG_SH)
    c.ellipse(16, y - 0.5, 6, 3.6, FROG)
    c.ellipse(16, y + 1.5 + puff * 0.5, 3 + puff, 1.6 + puff * 0.6, FROG_BELLY)
    for ex in (12.5, 19.5):  # big eyes on top
        c.ellipse(ex, y - 4, 2.2, 2.2, FROG)
        c.ellipse(ex, y - 4.2, 1.4, 1.4, WHITE)
        c.dot(round(ex), round(y - 4), EYE)
    c.dot(15, y + 1, FROG_SH)
    c.dot(17, y + 1, FROG_SH)  # smile
    # The tiny crown.
    c.rect(14, round(y) - 6, 18, round(y) - 5, GOLD)
    for x in (14, 16, 18):
        c.dot(x, round(y) - 7, GOLD)
    c.dot(16, round(y) - 6, GOLD_HI)
    return c.img


# ---------- Dungeon props ----------

WOOD, WOOD_SH, WOOD_HI = (128, 80, 42, 255), (86, 50, 26, 255), (160, 106, 58, 255)
IRON, IRON_HI = (84, 84, 96, 255), (140, 140, 156, 255)
STONE, STONE_SH = (120, 116, 130, 255), (84, 80, 94, 255)
FLAME = [(255, 80, 40, 255), (255, 150, 40, 255), (255, 230, 120, 255)]
GLOW, GLOW_HI, STEM = (90, 230, 220, 255), (200, 255, 250, 255), (220, 214, 200, 255)


def campfire(frame):
    c = Canvas()
    for x in range(6, 27, 4):  # ring of stones
        c.ellipse(x, 29, 2.2, 1.6, STONE_SH)
        c.ellipse(x - 0.3, 28.6, 1.8, 1.2, STONE)
    c.line(8, 29, 24, 24, WOOD_SH)  # crossed logs
    c.line(8, 28, 24, 23, WOOD)
    c.line(8, 24, 24, 29, WOOD_SH)
    c.line(8, 23, 24, 28, WOOD)
    rng = random.Random(frame * 7 + 1)
    for layer, (color, height, width) in enumerate(zip(FLAME, (16, 12, 7), (7, 5, 3))):
        for x in range(16 - width, 16 + width + 1):
            edge = 1 - abs(x - 16) / (width + 1)
            h = round(height * edge * (0.75 + rng.random() * 0.35))
            c.rect(x, 26 - h, x, 25, color)
    for _ in range(3):  # embers drifting up
        c.dot(rng.randint(11, 21), rng.randint(3, 10), FLAME[2])
    return c.img


def barrel():
    c = Canvas()
    for y in range(11, 31):
        bulge = 7 + round(1.5 * math.sin((y - 11) / 19 * math.pi))
        c.rect(16 - bulge, y, 15 + bulge, y, WOOD)
        c.rect(16 - bulge, y, 17 - bulge, y, WOOD_SH)
        c.rect(14 + bulge, y, 15 + bulge, y, WOOD_SH)
    for x in range(10, 22, 4):
        c.rect(x, 12, x, 30, WOOD_SH)  # staves
    for y in (14, 27):
        c.rect(8, y, 23, y + 1, IRON)
        c.rect(8, y, 23, y, IRON_HI)
    c.ellipse(15.5, 11, 7, 1.6, WOOD_HI)
    return c.img


def crate():
    c = Canvas()
    c.rect(7, 13, 24, 30, WOOD)
    for y in (13, 21, 30):
        c.rect(7, y, 24, y, WOOD_SH)
    c.rect(7, 13, 8, 30, WOOD_SH)
    c.rect(23, 13, 24, 30, WOOD_SH)
    c.line(9, 14, 22, 29, WOOD_HI)  # the X brace
    c.line(22, 14, 9, 29, WOOD_HI)
    c.rect(7, 13, 24, 13, WOOD_HI)
    return c.img


def bones():
    c = Canvas()
    c.line(6, 29, 14, 26, BONE_SH)
    c.line(18, 30, 26, 27, BONE)
    c.line(10, 30, 22, 29, BONE)
    for x, y in ((6, 29), (14, 26), (18, 30), (26, 27)):
        c.ellipse(x, y, 1.2, 1.2, BONE)
    c.ellipse(16, 25, 4, 3.5, BONE)  # a skull on top
    c.rect(13, 27, 19, 28, BONE)
    c.rect(14, 24, 15, 25, SOCKET)
    c.rect(17, 24, 18, 25, SOCKET)
    return c.img


def mushrooms(frame):
    c = Canvas()
    glow = GLOW_HI if frame else GLOW
    for x, h, r in ((11, 7, 4), (18, 10, 5), (23, 5, 3)):
        c.rect(x - 1, 30 - h, x, 30, STEM)
        c.ellipse(x - 0.5, 30 - h, r, r * 0.6, glow)
        c.dot(x - 1, 30 - h - 1, WHITE)
        c.dot(x + 1, 30 - h, WHITE)
    return c.img


def door(state):
    c = Canvas()
    if state == "open":  # swung back against the frame: just its edge shows
        c.rect(2, 2, 6, 31, WOOD_SH)
        c.rect(3, 2, 5, 31, WOOD)
        c.rect(2, 8, 6, 9, IRON)
        c.rect(2, 24, 6, 25, IRON)
        return c.img
    c.rect(3, 4, 28, 31, WOOD)
    c.ellipse(15.5, 5, 12.5, 4, WOOD)  # arched top
    for x in range(7, 27, 5):
        c.rect(x, 3, x, 31, WOOD_SH)  # planks
    for y in (9, 24):
        c.rect(3, y, 28, y + 1, IRON)
        c.rect(3, y, 28, y, IRON_HI)
    c.ellipse(22, 17, 1.6, 1.6, IRON_HI)  # ring handle
    c.dot(22, 17, IRON)
    if state == "locked":
        for x in (8, 13, 18, 23):
            c.rect(x, 4, x, 31, IRON)  # iron bars
        c.rect(12, 15, 19, 21, GOLD)  # a big padlock
        c.rect(13, 12, 18, 14, IRON_HI)
        c.rect(14, 13, 17, 14, (0, 0, 0, 0))
        c.rect(15, 17, 16, 19, SOCKET)
        c.dot(12, 15, GOLD_HI)
    return c.img


def key(frame):
    c = Canvas()
    c.ellipse(11, 23, 4, 4, GOLD)
    c.ellipse(11, 23, 2, 2, (0, 0, 0, 0))
    c.rect(14, 22, 25, 24, GOLD)
    c.rect(21, 25, 22, 27, GOLD)
    c.rect(24, 25, 25, 28, GOLD)
    if frame:
        for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1), (0, 1)):
            c.dot(18 + dx, 20 + dy, WHITE)
    else:
        c.dot(9, 21, GOLD_HI)
    return c.img


# ---------- Water decals (seen from above, laid flat) ----------

def ripple(frame):
    c = Canvas()
    for k in range(2):
        r = 3 + ((frame + k * 2) % 4) * 3
        alpha = 130 - r * 8  # faint: a drip landing, not a target
        for a in range(0, 360, 8):
            x = 16 + math.cos(math.radians(a)) * r
            y = 16 + math.sin(math.radians(a)) * r
            c.dot(round(x), round(y), (200, 228, 250, max(25, alpha)))
    return c.img


def lava_bubble(frame):
    """Frames 0-1: nothing. 2-4: a bubble swelling up. 5: it pops into a ring of sparks."""
    c = Canvas()
    if frame in (2, 3, 4):
        r = (1.6, 2.8, 3.8)[frame - 2]
        c.ellipse(16, 16, r + 0.8, r + 0.8, (210, 80, 26, 255))
        c.ellipse(16, 16, r, r, (255, 170, 56, 255))
        c.dot(round(16 - r / 2), round(16 - r / 2), (255, 244, 180, 255))
    elif frame == 5:
        for a in range(0, 360, 45):
            x = 16 + math.cos(math.radians(a)) * 4.5
            y = 16 + math.sin(math.radians(a)) * 4.5
            c.dot(round(x), round(y), (255, 210, 90, 255))
    return c.img


def lily():
    c = Canvas()
    c.ellipse(16, 16, 8, 8, (70, 150, 70, 255))
    c.ellipse(15, 15, 6.5, 6.5, (96, 180, 84, 255))
    for i in range(6):  # the notch
        c.dot(16 + i, 16 - i // 2, (0, 0, 0, 0))
        c.dot(16 + i, 15 - i // 2, (0, 0, 0, 0))
    c.ellipse(13, 14, 2.5, 2.5, (250, 190, 220, 255))  # a pink flower
    c.dot(13, 14, (255, 240, 120, 255))
    return c.img


def portrait(bust, background):
    img = Image.new("RGBA", (32, 32))
    px = img.load()
    r, g, b = background
    for y in range(32):
        for x in range(32):
            px[x, y] = (r + y, g + y // 2, b + y, 255)
    img.alpha_composite(outline(bust))
    return img


if __name__ == "__main__":
    write_sheet("Mermaid", None, [
        ("Idle", 4, True, [draw_mermaid(0, 0), draw_mermaid(1, 1), draw_mermaid(1, 2, blink=True), draw_mermaid(0, 1)]),
        ("Talk", 8, True, [draw_mermaid(0, 1, talk=True), draw_mermaid(1, 0)]),
    ], frame_size=M)
    write_sheet("Bonesy", None, [
        ("Idle", 2, True, [draw_bonesy(0), draw_bonesy(1)]),
        ("Talk", 8, True, [draw_bonesy(0, talk=True), draw_bonesy(1)]),
    ])
    write_sheet("Frog", None, [
        ("Idle", 2, True, [draw_frog(0), draw_frog(1)]),
        ("Hop", 6, True, [draw_frog(0), draw_frog(0, hop=1)]),
    ])
    write_sheet("DungeonProps", None, [
        ("Campfire", 8, True, [campfire(f) for f in range(4)]),
        ("Barrel", 1, False, [barrel()]),
        ("Crate", 1, False, [crate()]),
        ("Bones", 1, False, [bones()]),
        ("Mushrooms", 2, True, [mushrooms(0), mushrooms(1)]),
        ("Door_Closed", 1, False, [door("closed")]),
        ("Door_Open", 1, False, [door("open")]),
        ("Door_Locked", 1, False, [door("locked")]),
        ("Key", 3, True, [key(0), key(1)]),
    ])
    write_sheet("WaterDecals", None, [
        ("Ripple", 5, True, [ripple(f) for f in range(4)]),
        ("Lily", 1, False, [lily()]),
        ("LavaBubble", 6, True, [lava_bubble(f) for f in range(6)]),
    ], pivot="center", outline_color=None)
    portrait(draw_mermaid().crop((8, 0, 40, 32)), (40, 90, 120)).save(ART / "UI" / "PortraitMermaid.png")
    head = draw_bonesy().crop((8, 2, 24, 18)).resize((32, 32), Image.NEAREST)  # his skull, close up
    portrait(head, (70, 40, 30)).save(ART / "UI" / "PortraitBonesy.png")
