"""Generates Captain Grumblebeard, the pirates' captain: the final boss of Mermaid Cove.

Run:  Tools/.venv/bin/python Tools/make_captain_sprites.py
Out:  Assets/Art/PirateCaptain.png / .json   48x48 frames (a head taller than the deckhands)

A long red coat with gold buttons, a black tricorn with a skull on it and a red plume, a big
blue-black beard in two braids, a peg leg, and a parrot on his shoulder that squawks along with
him. His cutlass is huge. Same conventions as the deckhand (make_cove_sprites.py): Front / Back for
Idle, Walk, Attack and Hurt, one Die, and "Attack" as the action state.
"""
from PIL import Image

import palette as pal
from sprite_common import CLEAR, Canvas, tint, write_sheet

S = 48

COAT, COAT_SH, COAT_HI = (186, 54, 66, 255), (134, 36, 52, 255), (226, 98, 98, 255)
GOLD, GOLD_SH = pal.rgba(pal.HONEY), (176, 124, 40, 255)
CRAVAT = (246, 240, 228, 255)
TROUSERS, TROUSERS_SH = (52, 56, 92, 255), (36, 38, 68, 255)
BOOT = (58, 38, 44, 255)
PEG, PEG_SH = (160, 112, 66, 255), (112, 74, 44, 255)
SKIN, SKIN_SH = (234, 182, 148, 255), (204, 144, 120, 255)
BEARD, BEARD_SH, BEARD_HI = (44, 52, 96, 255), (28, 32, 66, 255), (80, 92, 150, 255)
HAT, HAT_SH, HAT_HI = (40, 34, 52, 255), (26, 22, 36, 255), (70, 62, 90, 255)
PLUME = (236, 90, 90, 255)
BONE = (240, 234, 220, 255)
EYE = (255, 200, 80, 255)
WHITE = (255, 255, 255, 255)
BELT = (46, 32, 38, 255)
BLADE, BLADE_SH = (206, 216, 228, 255), (140, 152, 172, 255)
PARROT, PARROT_BLUE, BEAK = (232, 64, 70, 255), (60, 130, 220, 255), (255, 200, 70, 255)

# Cutlass poses: (hand x, hand y, blade tip x, blade tip y)
CUTLASS = {
    "rest": (34, 28, 36, 10),
    "raised": (33, 20, 41, 2),
    "swing": (35, 27, 46, 38),
    "low": (34, 32, 46, 45),
}


def draw_captain(back=False, bob=0, legs=(0, 0), cutlass="rest", hurt=False, squawk=False):
    c = Canvas(S)
    b = bob

    # The good leg (left, in a boot) and the peg leg (right).
    lift = legs[0]
    step = -1 if lift else 0
    c.rect(17 + step, 34 - lift, 21 + step, 41 - lift, TROUSERS_SH if back else TROUSERS)
    c.rect(16 + step, 42 - lift, 21 + step, 46 - lift, BOOT)
    c.dot(15 + step, 46 - lift, BOOT)
    lift = legs[1]
    step = 1 if lift else 0
    c.rect(27 + step, 34 - lift, 30 + step, 38 - lift, TROUSERS_SH if back else TROUSERS)   # a rolled trouser leg
    c.rect(28 + step, 39 - lift, 29 + step, 45 - lift, PEG)                                   # the peg
    c.rect(29 + step, 39 - lift, 29 + step, 45 - lift, PEG_SH)
    c.rect(27 + step, 46 - lift, 30 + step, 46 - lift, PEG_SH)
    c.rect(27 + step, 38 - lift, 30 + step, 38 - lift, GOLD_SH)                               # a brass band

    # The long coat: widening toward a split hem, gold trim down the front and a belt.
    for y in range(20 + b, 38 + b):
        hw = 8 + (y - 20 - b) // 6
        c.rect(24 - hw, y, 24 + hw, y, COAT_SH if back else COAT)
        c.dot(24 - hw, y, COAT_HI)
        c.dot(24 + hw, y, COAT_SH)
    c.rect(24, 35 + b, 24, 38 + b, CLEAR)                                                        # the hem's split
    c.rect(24 - 8, 38 + b, 24 + 8, 38 + b, GOLD_SH)
    c.rect(16, 29 + b, 32, 30 + b, BELT)
    if back:
        c.rect(24, 21 + b, 24, 28 + b, COAT)                                                     # the back seam
        c.rect(24, 31 + b, 24, 37 + b, COAT)
    else:
        c.rect(24, 21 + b, 24, 28 + b, GOLD)                                                     # the trim and buttons
        for y in (22, 25, 28):
            c.rect(22, y + b, 23, y + b, GOLD)
            c.rect(25, y + b, 26, y + b, GOLD)
        c.rect(22, 29 + b, 26, 30 + b, GOLD)                                                     # a big buckle
        c.rect(23, 30 + b, 25, 30 + b, GOLD_SH)
        c.rect(24, 31 + b, 24, 37 + b, COAT_SH)
        c.rect(21, 20 + b, 27, 22 + b, CRAVAT)                                                   # a frilly cravat
        for x in (21, 23, 25, 27):
            c.dot(x, 23 + b, CRAVAT)

    # Left arm hangs, a gold cuff, a fist. The parrot sits on that shoulder.
    c.rect(11, 21 + b, 14, 30 + b, COAT_SH)
    c.rect(11, 21 + b, 11, 30 + b, COAT)
    c.rect(11, 31 + b, 14, 32 + b, GOLD)
    c.rect(11, 33 + b, 14, 35 + b, SKIN_SH if back else SKIN)
    py = 14 + b + (1 if squawk else 0)
    c.rect(10, py + 2, 14, py + 8, PARROT)
    c.rect(11, py, 14, py + 3, PARROT)
    c.rect(10, py + 4, 11, py + 8, PARROT_BLUE)
    c.rect(11, py + 8, 12, py + 10, PARROT_BLUE)
    c.dot(12, py + 1, WHITE if not back else PARROT)
    if not back:
        c.rect(8, py + 1, 10, py + 2 + (1 if squawk else 0), BEAK)
        c.dot(12, py + 1, (30, 20, 40, 255))

    # The head: a round face behind a great blue-black beard in two braids, under a tricorn.
    c.rect(19, 10 + b, 29, 18 + b, SKIN)
    c.rect(20, 9 + b, 28, 9 + b, SKIN)
    c.rect(29, 11 + b, 29, 18 + b, SKIN_SH)
    if back:
        c.rect(18, 11 + b, 30, 18 + b, BEARD_SH)                                                 # hair at the back of the head...
        c.rect(19, 19 + b, 29, 19 + b, BEARD_SH)
        c.rect(22, 20 + b, 26, 26 + b, BEARD)                                                    # ...gathered in a plait
        c.rect(22, 20 + b, 22, 26 + b, BEARD_HI)
        c.rect(22, 27 + b, 26, 27 + b, GOLD)
    else:
        c.rect(17, 15 + b, 31, 22 + b, BEARD)
        c.rect(19, 23 + b, 29, 24 + b, BEARD)
        c.rect(22, 25 + b, 26, 26 + b, BEARD_SH)
        c.rect(17, 15 + b, 17, 21 + b, BEARD_HI)
        for bx in (19, 28):                                                                      # two braids, tied with gold
            c.rect(bx, 25 + b, bx + 1, 29 + b, BEARD)
            c.rect(bx, 30 + b, bx + 1, 30 + b, GOLD)
        c.rect(21, 14 + b, 27, 14 + b, BEARD_HI)                                                 # the moustache
        c.rect(20, 15 + b, 28, 16 + b, BEARD)
        c.rect(23, 17 + b, 25, 17 + b, (150, 60, 70, 255))                                       # a grumpy mouth
        c.dot(26, 17 + b, GOLD)                                                                  # a gold tooth
        for ex in (21, 26):                                                                      # glowering eyes under heavy brows
            c.rect(ex, 12 + b, ex + 1, 13 + b, WHITE if hurt else EYE)
            c.dot(ex + (1 if ex < 24 else 0), 13 + b, (40, 20, 40, 255))
        c.line(20, 10 + b, 23, 12 + b, HAT_SH)
        c.line(28, 10 + b, 25, 12 + b, HAT_SH)
        c.dot(24, 14 + b, SKIN_SH)                                                               # nose
        c.dot(19, 17 + b, GOLD)                                                                  # an earring
    # The tricorn: a wide brim turned up at the sides, a tall crown, a red plume and a little skull.
    c.rect(13, 7 + b, 35, 9 + b, HAT)
    c.rect(11, 5 + b, 14, 8 + b, HAT)
    c.rect(34, 5 + b, 37, 8 + b, HAT)
    c.rect(13, 9 + b, 35, 9 + b, HAT_SH)
    c.rect(18, 1 + b, 30, 6 + b, HAT)
    c.rect(18, 1 + b, 30, 1 + b, HAT_HI)
    c.rect(13, 7 + b, 35, 7 + b, GOLD_SH)
    c.line(30, 2 + b, 36, -1 + b, PLUME)
    c.line(30, 3 + b, 37, 0 + b, PLUME)
    if not back:
        c.rect(22, 3 + b, 26, 5 + b, BONE)                                                       # the skull
        c.dot(23, 4 + b, HAT_SH)
        c.dot(25, 4 + b, HAT_SH)
        c.rect(23, 6 + b, 25, 6 + b, BONE)

    # The right arm and the enormous cutlass.
    hx, hy, tx, ty = CUTLASS[cutlass]
    hy += b
    c.line(33, 21 + b, hx, hy, COAT)
    c.line(34, 21 + b, hx + 1, hy, COAT_SH)
    c.rect(hx - 1, hy - 1, hx + 1, hy + 1, GOLD)
    c.dot(hx, hy, SKIN)
    mx, my = (hx + tx) / 2 + 2.5, (hy + ty) / 2
    for t in range(49):
        u = t / 48
        x = round((1 - u) ** 2 * hx + 2 * (1 - u) * u * mx + u * u * tx)
        y = round((1 - u) ** 2 * hy + 2 * (1 - u) * u * my + u * u * ty)
        c.dot(x, y, BLADE)
        c.dot(x + 1, y, BLADE_SH)
        c.dot(x - 1, y, BLADE)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.4)
    return img


def topples(stage):
    """Death frames: he staggers, squashing down in a huff, then only his hat, plume and cutlass are
    left (and a puff of stars carries him off). The parrot flaps off."""
    if stage < 3:
        body = draw_captain(hurt=True)
        squash = body.resize((S, S - 6 * (stage + 1)), Image.NEAREST)
        frame = Image.new("RGBA", (S, S), CLEAR)
        frame.alpha_composite(squash, (0, 6 * (stage + 1)))
        return frame
    c = Canvas(S)
    c.line(8, 44, 28, 40, BLADE)
    c.line(8, 45, 28, 41, BLADE_SH)
    c.rect(28, 38, 28, 43, GOLD)
    c.rect(24, 36, 40, 40, HAT)                                                                  # the tricorn, upside down
    c.rect(22, 34, 25, 37, HAT)
    c.rect(39, 34, 42, 37, HAT)
    c.rect(24, 40, 40, 40, HAT_SH)
    c.line(36, 36, 44, 32, PLUME)
    c.line(36, 37, 44, 33, PLUME)
    c.rect(30, 37, 34, 39, BONE)
    return tint(c.img, (20, 10, 30), 0.15)


def captain_animations():
    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_captain(back), draw_captain(back, bob=1, squawk=True)]),
            (f"Walk_{facing}", 7, True, [
                draw_captain(back, legs=(2, 0)),
                draw_captain(back, bob=1),
                draw_captain(back, legs=(0, 2)),
                draw_captain(back, bob=1),
            ]),
            (f"Attack_{facing}", 9, False, [
                draw_captain(back, cutlass="raised", squawk=True),
                draw_captain(back, cutlass="swing"),
                draw_captain(back, bob=1, cutlass="low"),
            ]),
            (f"Hurt_{facing}", 8, False, [draw_captain(back, bob=1, hurt=True, squawk=True)]),
        ]
    anims.append(("Die", 6, False, [topples(stage) for stage in range(4)]))
    return anims


if __name__ == "__main__":
    write_sheet("PirateCaptain", "Attack", captain_animations(), frame_size=S)
