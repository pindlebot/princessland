"""Generates Mermaid Cove: its pirates, the dark mermaids, Pearl, and the seaside props.

Run:  Tools/.venv/bin/python Tools/make_cove_sprites.py
Out:  Assets/Art/Pirate.png / .json       (32x32, enemy)  a deckhand with a bandana, an eye patch and a cutlass
        Idle / Walk / Attack (a three-frame cutlass swing) / Hurt, Front and Back, and Die
      Assets/Art/DarkMermaid.png / .json  (32x32, enemy)  a mermaid under the pirates' grumpy sea-spell: she
        rises out of the water and throws dark bolts. Attack: arms up, an orb grows, she throws it.
        Die: she sinks away (the spell breaks in a puff of stars, and she swims off happy).
        Walk is the same as Idle: she never leaves her spot in the water.
      Assets/Art/Pearl.png / .json        (48x48, NPC)    Coralie's big sister, on her rock: Coralie's
        drawing in butter-yellow hair and a coral tail
      Assets/Art/Coast.png / .json        (64x64 frames, bottom pivot)
        Palm 2 (sway) · Rowboat 2 (bob) · Splash 3 (foam where a waterfall lands) · Treasure (a heap of gold)
      Assets/Art/Ship.png / .json         (96x96, bottom pivot)  the pirates' little ship, bobbing at anchor
      Assets/Art/CoveDecals.png / .json   (32x32, centre pivot; laid flat) Shell · Starfish · Foam 4
      Assets/Art/UI/PortraitPearl.png     32x32 dialogue portrait

Colours follow palette.py: the pirates and mermaids are characters, so they get the louder
coral, turquoise and butter accents; the ship, palms and rowboat stay in quiet driftwood and sage.
"""
import math
import random

from PIL import Image

import palette as pal
import make_friends_sprites as friends
from sprite_common import ART, CLEAR, F, Canvas, tint, write_sheet

# ---------- The pirate ----------

SKIN, SKIN_SH = (238, 196, 160, 255), (206, 156, 126, 255)
BANDANA, BANDANA_SH = pal.rgba(pal.CORAL), (196, 76, 96, 255)
STRIPE_A, STRIPE_B = pal.rgba(pal.CREAM), (196, 76, 96, 255)
VEST, VEST_SH = (64, 68, 110, 255), (46, 48, 84, 255)
TROUSERS, TROUSERS_SH = (120, 96, 80, 255), (92, 72, 64, 255)
BOOT = (70, 46, 56, 255)
BEARD, BEARD_SH = (126, 76, 52, 255), (96, 56, 42, 255)
PATCH = (40, 26, 40, 255)
EYE = (40, 30, 60, 255)
BELT, BUCKLE = (74, 46, 50, 255), pal.rgba(pal.BUTTER)
BLADE, BLADE_SH = (214, 220, 230, 255), (140, 148, 168, 255)
GUARD = pal.rgba(pal.HONEY)

# Cutlass poses: (hand x, hand y, blade tip x, blade tip y)
CUTLASS = {
    "rest": (22, 21, 24, 11),    # held up at the side, curving a little outward
    "raised": (21, 13, 27, 5),   # wind-up over the shoulder
    "swing": (23, 19, 30, 24),   # slashing down and forward
    "low": (22, 23, 29, 29),     # follow-through
}


def draw_pirate(back=False, bob=0, legs=(0, 0), cutlass="rest", hurt=False):
    c = Canvas()
    b = bob

    # Legs: striped-free trousers tucked into boots; a lifted leg steps outward.
    for i, x in enumerate((12, 17)):
        lift = legs[i]
        step = (-1 if i == 0 else 1) if lift else 0
        c.rect(x + step, 22 - lift, x + 2 + step, 27 - lift, TROUSERS_SH if back else TROUSERS)
        c.rect(x + step, 28 - lift, x + 2 + step, 30 - lift, BOOT)
        c.dot(x + 3 + step if i else x - 1 + step, 30 - lift, BOOT)  # the toe

    # Torso: a striped shirt under an open navy vest, and a belt with a gold buckle.
    for y in range(14 + b, 21 + b):
        stripe = STRIPE_A if (y - b) % 2 == 0 else STRIPE_B
        c.rect(11, y, 20, y, stripe)
    if back:
        c.rect(11, 14 + b, 20, 20 + b, VEST)      # the vest covers the back
        c.rect(15, 14 + b, 16, 20 + b, VEST_SH)   # its seam
    else:
        c.rect(11, 14 + b, 12, 20 + b, VEST)
        c.rect(19, 14 + b, 20, 20 + b, VEST_SH)
    c.rect(11, 21 + b, 20, 21 + b, BELT)
    if not back:
        c.rect(15, 21 + b, 16, 21 + b, BUCKLE)

    # Left arm hangs down, a fist at the end.
    c.rect(9, 14 + b, 10, 19 + b, STRIPE_A)
    c.rect(9, 16 + b, 10, 16 + b, STRIPE_B)
    c.rect(9, 18 + b, 10, 18 + b, STRIPE_B)
    c.rect(9, 20 + b, 10, 21 + b, SKIN_SH if back else SKIN)

    # Head: a round face with a big bushy beard, under a coral bandana knotted at the side.
    c.rect(12, 6 + b, 19, 12 + b, SKIN)
    c.rect(13, 5 + b, 18, 5 + b, SKIN)
    c.rect(19, 7 + b, 19, 12 + b, SKIN_SH)
    if back:
        c.rect(12, 7 + b, 19, 12 + b, BEARD_SH)   # hair at the back of the head
        c.rect(11, 9 + b, 20, 12 + b, BEARD_SH)   # beard poking out at the sides
    else:
        c.rect(11, 10 + b, 20, 13 + b, BEARD)     # the beard
        c.rect(12, 14 + b, 19, 14 + b, BEARD)
        c.rect(13, 13 + b, 18, 13 + b, BEARD_SH)
        c.rect(14, 11 + b, 17, 11 + b, SKIN_SH)   # mouth gap, then the mouth
        c.rect(15, 11 + b, 16, 11 + b, (150, 60, 70, 255))
        c.rect(13, 9 + b, 18, 9 + b, BEARD)       # a big moustache
        c.rect(13, 7 + b, 14, 8 + b, (255, 255, 255, 255) if hurt else EYE)  # the good eye
        c.dot(13, 7 + b, (255, 255, 255, 255))
        c.rect(17, 7 + b, 18, 8 + b, PATCH)       # the eye patch and its strap
        c.line(12, 5 + b, 17, 7 + b, PATCH)
        c.dot(16, 8 + b, SKIN_SH)                 # nose
    c.rect(12, 3 + b, 19, 5 + b, BANDANA)
    c.rect(13, 2 + b, 18, 2 + b, BANDANA)
    c.rect(12, 5 + b, 19, 5 + b, BANDANA_SH)
    for x in (13, 16):                            # white polka dots
        c.dot(x + (1 if back else 0), 3 + b, pal.rgba(pal.CREAM))
    kx = 20 if back else 11                       # the knot and its tails
    c.rect(kx, 4 + b, kx, 6 + b, BANDANA_SH)
    c.dot(kx + (1 if back else -1), 7 + b, BANDANA_SH)

    # Right arm and the cutlass (a curved blade with a gold guard).
    hx, hy, tx, ty = CUTLASS[cutlass]
    hy += b
    c.line(20, 14 + b, hx, hy, STRIPE_A)
    c.line(21, 14 + b, hx + 1, hy, STRIPE_B)
    mx, my = (hx + tx) / 2 + 1.5, (hy + ty) / 2  # bow the blade outward a little
    for t in range(9):
        u = t / 8
        x = round((1 - u) ** 2 * hx + 2 * (1 - u) * u * mx + u * u * tx)
        y = round((1 - u) ** 2 * hy + 2 * (1 - u) * u * my + u * u * ty)
        c.dot(x, y, BLADE)
        c.dot(x + 1, y, BLADE_SH)
    c.rect(hx - 1, hy, hx + 1, hy, GUARD)
    c.dot(hx, hy, SKIN)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.45)
    return img


def pirate_falls(stage):
    """Death frames: he topples back (squashing down), then only his bandana and cutlass are left
    (and the puff of stars carries him off)."""
    if stage < 3:
        body = draw_pirate(hurt=True)
        squash = body.resize((F, F - 4 * (stage + 1)), Image.NEAREST)
        frame = Image.new("RGBA", (F, F), CLEAR)
        frame.alpha_composite(squash, (0, 4 * (stage + 1)))
        return frame
    c = Canvas()
    c.line(6, 29, 18, 27, BLADE)
    c.line(6, 30, 18, 28, BLADE_SH)
    c.rect(18, 26, 18, 29, GUARD)
    c.rect(19, 27, 25, 29, BANDANA)
    c.rect(20, 26, 24, 26, BANDANA)
    c.rect(19, 29, 25, 29, BANDANA_SH)
    c.dot(21, 27, pal.rgba(pal.CREAM))
    return tint(c.img, (20, 10, 30), 0.15)


def pirate_animations():
    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_pirate(back), draw_pirate(back, bob=1)]),
            (f"Walk_{facing}", 8, True, [
                draw_pirate(back, legs=(2, 0)),
                draw_pirate(back, bob=1),
                draw_pirate(back, legs=(0, 2)),
                draw_pirate(back, bob=1),
            ]),
            (f"Attack_{facing}", 10, False, [
                draw_pirate(back, cutlass="raised"),
                draw_pirate(back, cutlass="swing"),
                draw_pirate(back, bob=1, cutlass="low"),
            ]),
            (f"Hurt_{facing}", 8, False, [draw_pirate(back, bob=1, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [pirate_falls(s) for s in range(4)]))
    return anims


# ---------- The dark mermaid ----------

DM_SKIN, DM_SKIN_SH = (204, 190, 222, 255), (168, 150, 192, 255)
DM_HAIR, DM_HAIR_SH, DM_HAIR_HI = (40, 88, 104, 255), (26, 60, 76, 255), (76, 140, 150, 255)
DM_TAIL, DM_TAIL_SH, DM_TAIL_HI = (92, 64, 136, 255), (64, 42, 104, 255), (150, 112, 196, 255)
DM_TOP = (126, 72, 150, 255)
DM_EYE = (255, 112, 168, 255)
DM_CROWN = pal.rgba(pal.PLUM_DEEP)
ORB, ORB_CORE = (150, 80, 210, 255), (255, 170, 230, 255)
SEA, SEA_HI = pal.rgba(pal.SEA), pal.rgba(pal.FOAM)


def draw_dark_mermaid(back=False, bob=0, sway=0, arms="down", orb=0, hurt=False, sink=0):
    """She rises from the sea, the water line across the bottom rows. sink lowers her into it."""
    c = Canvas()
    b = bob + sink
    # The fin flicks up out of the water behind her.
    fx = 9 if back else 23
    c.ellipse(fx, 21 + b - sway, 2.6, 1.6, DM_TAIL_SH)
    c.ellipse(fx, 20 + b - sway, 2, 1.2, DM_TAIL_HI)

    # Hair falls behind her shoulders, swaying.
    for side in (-1, 1):
        for y in range(8 + b, 22 + b):
            x = 16 + side * (5 + (y - 8 - b) * 0.1) + sway * ((y - 8 - b) / 14)
            c.rect(round(x) - 1, y, round(x), y, DM_HAIR_SH if y > 18 + b else DM_HAIR)

    # Tail and torso, rising from the water.
    c.rect(12, 21 + b, 19, 27 + b, DM_TAIL)
    c.rect(18, 21 + b, 19, 27 + b, DM_TAIL_SH)
    for x, y in ((13, 23), (16, 25), (14, 26), (17, 22)):
        c.dot(x, y + b, DM_TAIL_HI)               # scales
    c.rect(13, 14 + b, 18, 21 + b, DM_SKIN)
    c.rect(18, 14 + b, 18, 21 + b, DM_SKIN_SH)
    if not back:
        c.rect(13, 16 + b, 15, 17 + b, DM_TOP)    # a dark shell top
        c.rect(16, 16 + b, 18, 17 + b, DM_TOP)
    else:
        c.rect(13, 16 + b, 18, 16 + b, DM_TOP)    # its strap

    # Arms: down at her sides, raised with an orb between her hands, or thrown forward.
    if arms == "down":
        c.line(12, 15 + b, 10, 21 + b, DM_SKIN)
        c.line(19, 15 + b, 21, 21 + b, DM_SKIN_SH)
    elif arms == "up":
        c.line(12, 15 + b, 10, 8 + b, DM_SKIN)
        c.line(19, 15 + b, 21, 8 + b, DM_SKIN_SH)
    else:  # "throw": both hands pushed forward and out
        c.line(12, 15 + b, 8, 13 + b, DM_SKIN)
        c.line(19, 15 + b, 24, 13 + b, DM_SKIN_SH)

    # Head, hair on top, a little crown of black coral with a glowing gem.
    c.ellipse(16, 9 + b, 4.5, 5, DM_SKIN)
    c.ellipse(16, 5 + b, 5.2, 3, DM_HAIR)
    c.rect(11, 5 + b, 12, 12 + b, DM_HAIR)
    c.rect(20, 5 + b, 21, 12 + b, DM_HAIR)
    c.ellipse(14, 4 + b, 2, 1, DM_HAIR_HI)
    if back:
        c.ellipse(16, 8 + b, 4.8, 5, DM_HAIR)
        c.rect(14, 9 + b, 18, 13 + b, DM_HAIR_SH)
    else:
        for ex in (14, 17):                       # glowing, grumpy eyes under slanted brows
            c.rect(ex, 9 + b, ex + 1, 10 + b, (255, 255, 255, 255) if hurt else DM_EYE)
        c.line(13, 7 + b, 15, 8 + b, DM_HAIR_SH)
        c.line(19, 7 + b, 17, 8 + b, DM_HAIR_SH)
        c.rect(15, 12 + b, 17, 12 + b, (110, 60, 96, 255))  # a frown
    for x, h in ((13, 2), (16, 3), (19, 2)):      # the crown's spikes
        c.rect(x, 2 + b - h, x, 2 + b, DM_CROWN)
    c.rect(13, 2 + b, 19, 2 + b, DM_CROWN)
    c.dot(16, 1 + b, ORB_CORE)
    if orb:  # held up over her crown, or flying from her hands
        ox, oy = (16, max(3, 4 - orb) + b) if arms == "up" else (26, 12 + b)
        c.ellipse(ox, oy, orb + 0.6, orb + 0.6, ORB)
        c.ellipse(ox - 0.5, oy - 0.5, max(0.8, orb - 1), max(0.8, orb - 1), ORB_CORE)

    # The water: everything below her waterline is hidden, then the lapping sea and a glint.
    for y in range(27, 32):
        for x in range(32):
            c.dot(x, y, CLEAR)
    c.ellipse(16, 28, 10, 2.2, SEA)
    c.rect(9 + sway, 28, 11 + sway, 28, SEA_HI)
    c.rect(20 - sway, 29, 22 - sway, 29, SEA_HI)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.45)
    return img


def mermaid_sinks(stage):
    """She sinks out of sight, leaving rings on the water."""
    if stage < 3:
        return draw_dark_mermaid(hurt=stage == 0, sink=4 * (stage + 1))
    c = Canvas()
    c.ellipse(16, 28, 10, 2.2, SEA)
    c.ellipse(16, 28, 7, 1.4, SEA_HI)
    c.ellipse(16, 28, 5.5, 0.8, SEA)
    return c.img


def dark_mermaid_animations():
    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        idle = [draw_dark_mermaid(back), draw_dark_mermaid(back, bob=1, sway=1)]
        anims += [
            (f"Idle_{facing}", 3, True, idle),
            (f"Walk_{facing}", 3, True, idle),  # she never swims off her spot
            (f"Attack_{facing}", 8, False, [
                draw_dark_mermaid(back, arms="up", orb=1),
                draw_dark_mermaid(back, arms="up", orb=3),
                draw_dark_mermaid(back, bob=1, arms="throw"),
            ]),
            (f"Hurt_{facing}", 8, False, [draw_dark_mermaid(back, bob=1, hurt=True)]),
        ]
    anims.append(("Die", 6, False, [mermaid_sinks(s) for s in range(4)]))
    return anims


# ---------- Pearl, Coralie's big sister ----------

def draw_pearl(*args, **kwargs):
    """Coralie's drawing (make_friends_sprites.draw_mermaid) with Pearl's colours swapped in:
    butter-yellow hair and a coral tail."""
    saved = {k: getattr(friends, k) for k in ("HAIR", "HAIR_SH", "HAIR_HI", "TAIL", "TAIL_SH", "TAIL_HI", "FIN", "FIN_SH")}
    friends.HAIR, friends.HAIR_SH, friends.HAIR_HI = pal.rgba(pal.BUTTER), pal.rgba(pal.HONEY), pal.rgba(pal.BUTTER_LIGHT)
    friends.TAIL, friends.TAIL_SH, friends.TAIL_HI = pal.rgba(pal.CORAL), (196, 76, 96, 255), pal.rgba(pal.CORAL_LIGHT)
    friends.FIN, friends.FIN_SH = pal.rgba(pal.CORAL_LIGHT), pal.rgba(pal.CORAL)
    try:
        return friends.draw_mermaid(*args, **kwargs)
    finally:
        for k, v in saved.items():
            setattr(friends, k, v)


# ---------- Coast props (64x64) ----------

S = 64
WOOD, WOOD_SH, WOOD_HI = pal.rgba(pal.PLANK), pal.rgba(pal.PLANK_SHADE), pal.rgba(pal.PLANK_LIGHT)
FROND, FROND_SH, FROND_HI = (104, 150, 92, 255), (76, 116, 78, 255), pal.rgba(pal.SAGE_LIGHT)
TRUNK, TRUNK_SH = (168, 128, 90, 255), (130, 96, 72, 255)
COCONUT = (110, 74, 58, 255)
GOLD, GOLD_SH, GOLD_HI = pal.rgba(pal.BUTTER), pal.rgba(pal.HONEY), pal.rgba(pal.BUTTER_LIGHT)


def palm(sway):
    c = Canvas(S)
    # A gently curving trunk in rings, leaning right, from the sand up to the crown.
    top = (36 + sway, 22)
    for i in range(24):
        t = i / 23
        x = 30 + (top[0] - 30) * t + math.sin(t * math.pi) * 3
        y = 62 - (62 - top[1]) * t
        c.rect(round(x) - 2, round(y), round(x) + 1, round(y), TRUNK if i % 4 else TRUNK_SH)
        c.dot(round(x) + 1, round(y), TRUNK_SH)
    cx, cy = top
    c.ellipse(cx - 2, cy + 3, 2.2, 2.2, COCONUT)
    c.ellipse(cx + 2, cy + 3, 2.2, 2.2, COCONUT)
    # Seven fronds arcing out of the crown, each a drooping curve of leaflets.
    for k, angle in enumerate((-170, -140, -110, -70, -40, -10, 20)):
        a = math.radians(angle + sway * 4)
        length = 18 if k in (0, 6) else 21
        for i in range(length):
            t = i / length
            x = cx + math.cos(a) * i
            y = cy + math.sin(a) * i * 0.7 + (t ** 2) * 9   # droops toward the tip
            w = 2.4 * (1 - t) + 0.6
            c.ellipse(x, y + 1, w, w * 0.7, FROND_SH)
            c.ellipse(x, y, w, w * 0.6, FROND)
            if i % 5 == 2:
                c.dot(round(x), round(y) - 1, FROND_HI)
    return c.img


def rowboat(bob):
    c = Canvas(S)
    y0 = 40 + bob
    # The hull, in profile: a curved wooden boat with a coral stripe and an oar resting across it.
    for x in range(10, 54):
        t = (x - 10) / 43
        depth = round(9 * math.sin(t * math.pi) ** 0.7)
        rise = round(4 * (abs(t - 0.5) * 2) ** 3)          # bow and stern sweep up
        c.rect(x, y0 - rise, x, y0 + depth, WOOD)
        c.dot(x, y0 + depth, WOOD_SH)
        c.dot(x, y0 - rise, WOOD_HI)
        c.dot(x, y0 - rise + 2, pal.rgba(pal.CORAL))
        if x % 9 == 0:
            c.rect(x, y0 - rise + 3, x, y0 + depth - 1, WOOD_SH)  # rib lines
    c.line(14, y0 - 9, 50, y0 + 3, WOOD_SH)                    # the oar
    c.line(14, y0 - 10, 50, y0 + 2, WOOD_HI)
    c.ellipse(52, y0 + 3, 3, 1.4, WOOD)
    # A rope to its mooring post, and a little waterline.
    c.rect(6, y0 - 10, 8, y0 + 8, WOOD_SH)
    c.rect(6, y0 - 11, 8, y0 - 11, WOOD_HI)
    c.line(8, y0 - 6, 12, y0 - 2, pal.rgba(pal.CREAM_SHADE))
    c.ellipse(32, y0 + 10, 24, 2, pal.rgba(pal.SEA))
    c.rect(18 - bob, y0 + 10, 22 - bob, y0 + 10, pal.rgba(pal.FOAM))
    c.rect(40 + bob, y0 + 11, 43 + bob, y0 + 11, pal.rgba(pal.FOAM))
    return c.img


def splash(frame):
    """White water where a waterfall lands: a low churning heap of foam with spray above."""
    rng = random.Random(40 + frame)
    c = Canvas(S)
    for i in range(12):
        x = 14 + i * 3 + rng.randint(-1, 1)
        h = rng.randint(3, 7)
        c.ellipse(x, 58, 3, h * 0.6, pal.rgba(pal.WATER_GLINT))
        c.ellipse(x, 59, 2.4, h * 0.4, pal.rgba(pal.FOAM))
    for _ in range(9):
        x, y = rng.randint(12, 52), rng.randint(44, 54)
        c.dot(x, y, pal.rgba(pal.FOAM))
    return c.img


def treasure():
    """The pirates' hoard: a heap of gold coins with a string of pearls and a four-point glint."""
    c = Canvas(S)
    c.ellipse(32, 56, 16, 6, GOLD_SH)
    c.ellipse(32, 54, 14, 6, GOLD)
    c.ellipse(32, 50, 9, 5, GOLD)
    c.ellipse(30, 48, 5, 2.5, GOLD_HI)
    for x, y in ((22, 55), (27, 52), (36, 51), (40, 56), (33, 57), (30, 49)):
        c.rect(x, y, x + 1, y, GOLD_SH)                     # coin edges
    for i in range(10):                                     # pearls
        c.dot(18 + i * 3, 58 - round(math.sin(i / 9 * math.pi) * 3), pal.rgba(pal.CREAM))
    for d in range(-3, 4):                                  # the glint
        c.dot(38 + d, 44, pal.rgba(pal.BUTTER_LIGHT))
        c.dot(38, 44 + d, pal.rgba(pal.BUTTER_LIGHT))
    return c.img


# ---------- The pirate ship (96x96) ----------

SAIL, SAIL_SH = pal.rgba(pal.CREAM), pal.rgba(pal.CREAM_SHADE)
FLAG = (52, 40, 60, 255)


def ship(bob):
    c = Canvas(96)
    y0 = 70 + bob
    # Masts first, then the sails on them, then the hull in front.
    for mx, top in ((40, 12), (62, 22)):
        c.rect(mx, top + bob, mx + 1, y0, WOOD_SH)
    for mx, top, w, h in ((40, 16, 14, 26), (62, 26, 11, 20)):
        for y in range(top + bob, top + bob + h):
            t = (y - top - bob) / h
            bulge = round(3 * math.sin(t * math.pi))      # the sail fills with wind
            c.rect(mx - w + bulge, y, mx + w // 3 + bulge, y, SAIL)
            c.dot(mx - w + bulge, y, SAIL_SH)
        c.rect(mx - w, top + bob + h, mx + w // 3 + 2, top + bob + h, WOOD_SH)  # the boom
    c.rect(37, 34 + bob, 40, 34 + bob, pal.rgba(pal.CORAL))   # a patch on the big sail
    c.rect(37, 35 + bob, 40, 36 + bob, pal.rgba(pal.CORAL_LIGHT))
    # The flag: plum with a cream skull-and-star (a friendly sort of Jolly Roger).
    c.rect(41, 8 + bob, 52, 14 + bob, FLAG)
    c.ellipse(46, 10 + bob, 2, 1.8, pal.rgba(pal.CREAM))
    c.dot(45, 10 + bob, FLAG)
    c.dot(47, 10 + bob, FLAG)
    c.dot(50, 12 + bob, pal.rgba(pal.BUTTER))
    # The hull: wooden planks, a coral stripe, portholes, a raised stern on the right.
    for x in range(10, 88):
        t = (x - 10) / 77
        depth = round(14 * math.sin(t * math.pi) ** 0.5)
        rise = 8 if x > 70 else round(5 * (1 - t) ** 3 * 2)   # bowsprit end and the raised stern
        c.rect(x, y0 - rise, x, y0 + depth, WOOD)
        c.dot(x, y0 + depth, WOOD_SH)
        c.dot(x, y0 - rise, WOOD_HI)
        c.dot(x, y0 - rise + 3, pal.rgba(pal.CORAL))
        if y0 + 8 <= y0 + depth:
            c.dot(x, y0 + 8, WOOD_SH)                         # one long plank seam
    for px_ in (30, 44, 58):
        c.ellipse(px_, y0 + 4, 2, 2, pal.rgba(pal.PLUM_DEEP))
        c.dot(px_ - 1, y0 + 3, pal.rgba(pal.BUTTER))          # lamplight inside
    c.line(10, y0 - 5, 2, y0 - 12, WOOD_SH)                   # the bowsprit
    # Water around the hull.
    c.ellipse(48, y0 + 15, 44, 3, pal.rgba(pal.SEA))
    for x in (16, 40, 70):
        c.rect(x + bob * 2, y0 + 15, x + 4 + bob * 2, y0 + 15, pal.rgba(pal.FOAM))
    return c.img


# ---------- Flat decals (32x32, laid on the sand) ----------

def shell():
    c = Canvas()
    for i in range(5):                                         # a fan scallop shell
        a = math.radians(200 + i * 35)
        c.line(16, 20, round(16 + math.cos(a) * 7), round(20 + math.sin(a) * 6), pal.rgba(pal.CORAL_LIGHT))
    c.ellipse(16, 16, 6, 4, pal.rgba(pal.CORAL_LIGHT))
    for i in range(5):
        a = math.radians(200 + i * 35)
        c.line(16, 20, round(16 + math.cos(a) * 6), round(20 + math.sin(a) * 5), pal.rgba(pal.CORAL))
    c.rect(14, 20, 18, 21, pal.rgba(pal.CORAL))
    return c.img


def starfish():
    c = Canvas()
    for k in range(5):
        a = math.radians(-90 + k * 72)
        for r in range(7):
            w = 1.6 * (1 - r / 7) + 0.5
            c.ellipse(16 + math.cos(a) * r, 16 + math.sin(a) * r, w, w, pal.rgba(pal.HONEY))
    c.ellipse(16, 16, 2, 2, pal.rgba(pal.BUTTER))
    return c.img


def foam(frame):
    """A soft ring of foam lapping around a rock or post in the sea."""
    c = Canvas()
    r = 8 + frame
    for a in range(0, 360, 12):
        if (a // 12 + frame) % 3 == 0:
            continue
        c.dot(round(16 + math.cos(math.radians(a)) * r), round(16 + math.sin(math.radians(a)) * r * 0.9), pal.rgba(pal.FOAM))
    return c.img


if __name__ == "__main__":
    write_sheet("Pirate", "Attack", pirate_animations())
    write_sheet("DarkMermaid", "Attack", dark_mermaid_animations())
    write_sheet("Pearl", None, [
        ("Idle", 4, True, [draw_pearl(0, 0), draw_pearl(1, 1), draw_pearl(1, 2, blink=True), draw_pearl(0, 1)]),
        ("Talk", 8, True, [draw_pearl(0, 1, talk=True), draw_pearl(1, 0)]),
    ], frame_size=friends.M)
    write_sheet("Coast", None, [
        ("Palm", 2, True, [palm(0), palm(1)]),
        ("Rowboat", 2, True, [rowboat(0), rowboat(1)]),
        ("Splash", 8, True, [splash(f) for f in range(3)], None),   # foam has no outline
        ("Treasure", 1, False, [treasure()]),
    ], frame_size=S)
    write_sheet("Ship", None, [("Bob", 2, True, [ship(0), ship(1)])], frame_size=96)
    write_sheet("CoveDecals", None, [
        ("Shell", 1, False, [shell()]),
        ("Starfish", 1, False, [starfish()]),
        ("Foam", 4, True, [foam(f) for f in range(4)]),
    ], pivot="center", outline_color=None)
    friends.portrait(draw_pearl().crop((8, 0, 40, 32)), (120, 70, 90)).save(ART / "UI" / "PortraitPearl.png")
