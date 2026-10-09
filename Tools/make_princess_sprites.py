"""Generates the Aquamarine Princess sprite sheet (the second playable character).

Run:  Tools/.venv/bin/python Tools/make_princess_sprites.py
Out:  Assets/Art/Princess.png / Princess.json   (same layout conventions as the wizard)

She wears an aquamarine gown and a gold tiara, and casts with a wand topped by an
aquamarine orb. Same animation set as the wizard, so she gets the same state machine.
"""
from PIL import Image

from sprite_common import CLEAR, F, Canvas, tint, write_sheet

GOWN, GOWN_SH, GOWN_HI = (64, 196, 180, 255), (34, 136, 134, 255), (132, 232, 214, 255)
TRIM = (236, 248, 244, 255)
SKIN = (244, 204, 176, 255)
BLUSH = (240, 158, 168, 255)
HAIR, HAIR_SH = (246, 214, 120, 255), (204, 156, 70, 255)
GOLD = (236, 192, 70, 255)
GEM = (120, 240, 230, 255)
EYE = (40, 30, 50, 255)
SHOE = (90, 60, 110, 255)
ROD = (200, 206, 222, 255)
ORB, ORB_HI = (100, 228, 220, 255), (232, 255, 252, 255)
SUIT, SUIT_HI = (64, 196, 180, 255), (132, 232, 214, 255)  # an aquamarine swimsuit, to match her gown
BUBBLE, BUBBLE_SHINE = (238, 244, 252, 255), (255, 255, 255, 255)
DUCK, BEAK = (255, 214, 60, 255), (250, 140, 40, 255)


def draw_princess(back=False, bob=0, legs=(0, 0), wand_up=0, glow=0, hurt=False, sit=False, kick=0):
    """Same pose controls as the wizard: bob lowers the upper body, legs lifts a foot
    (the gown sways that way), wand_up raises the wand, glow (0-2) lights the orb.
    sit draws her sitting down (facing front), feet dangling; kick swings one foot up."""
    c = Canvas()
    b = bob + (SIT_DROP if sit else 0)
    sway = -1 if legs[1] > legs[0] else (1 if legs[0] > legs[1] else 0)

    if sit:
        draw_lap(c, kick)
    else:
        # Little shoes peek out under the hem when she steps
        if legs[0]:
            c.rect(11, 30, 13, 30, SHOE)
        if legs[1]:
            c.rect(18, 30, 20, 30, SHOE)

        # Bell-shaped gown with a white hem
        top = 17 + b
        for y in range(top, 31):
            hw = 3 + round((y - top) * 0.55)
            cx = 15 + (sway if y >= 26 else 0)
            c.rect(cx - hw, y, cx + hw, y, GOWN_SH if back else GOWN)
            c.dot(cx - hw, y, GOWN_HI)
            c.dot(cx + hw, y, GOWN_SH)
            if not back and y > top + 1:  # a lighter front panel that widens toward the hem
                pw = (y - top) // 4
                c.rect(cx - pw, y, cx + 1 + pw, y, GOWN_HI)
        c.rect(15 + sway - 10, 29, 15 + sway + 10, 29, TRIM)

    # Bodice and puffed sleeves
    c.rect(13, 13 + b, 18, 17 + b, GOWN_SH if back else GOWN)
    if back:
        for y in (14, 16):
            c.dot(15, y + b, TRIM)
            c.dot(16, y + b, TRIM)  # lacing
    else:
        c.rect(14, 13 + b, 17, 13 + b, TRIM)  # neckline
    c.rect(11, 13 + b, 12, 15 + b, GOWN_HI)  # left sleeve
    c.rect(11, 16 + b, 12, 18 + b, SKIN)  # left arm
    c.rect(19, 13 + b, 20, 15 + b, GOWN_HI)  # right sleeve

    # Head and hair (long hair falls past the shoulders)
    if back:
        c.rect(12, 5 + b, 19, 17 + b, HAIR)
        c.rect(13, 18 + b, 18, 18 + b, HAIR)  # rounded ends
        c.rect(14, 19 + b, 17, 19 + b, HAIR_SH)
        for x in (13, 15, 17):
            c.rect(x, 9 + b, x, 17 + b, HAIR_SH)  # strands
    else:
        c.rect(11, 7 + b, 12, 16 + b, HAIR)  # hair falling on each side
        c.rect(19, 7 + b, 20, 16 + b, HAIR)
        c.rect(13, 8 + b, 18, 12 + b, SKIN)
        c.rect(12, 5 + b, 19, 7 + b, HAIR)
        c.rect(13, 8 + b, 14, 8 + b, HAIR)  # fringe
        c.rect(17, 8 + b, 18, 8 + b, HAIR_SH)
        if hurt:
            c.rect(14, 10 + b, 15, 10 + b, EYE)
            c.rect(17, 10 + b, 18, 10 + b, EYE)
        else:
            c.dot(14, 10 + b, EYE)
            c.dot(17, 10 + b, EYE)
            c.dot(13, 11 + b, BLUSH)
            c.dot(18, 11 + b, BLUSH)

    # Tiara with an aquamarine gem
    c.rect(13, 5 + b, 18, 5 + b, GOLD)
    for x, y in ((13, 4), (18, 4), (15, 3), (16, 3)):
        c.dot(x, y + b, GOLD)
    if not back:
        c.rect(15, 4 + b, 16, 4 + b, GEM)

    # Wand in the right hand, raised while casting
    s = wand_up
    c.rect(21, 16 + b - s, 22, 17 + b - s, SKIN)  # hand
    c.rect(22, 10 + b - s, 22, 18 + b - s, ROD)
    oy = 7 + b - s
    c.rect(21, oy, 23, oy + 2, ORB)
    c.dot(21, oy, ORB_HI)
    if glow >= 1:
        for dx, dy in ((1, -2), (1, 4), (-2, 1), (4, 1)):
            c.dot(21 + dx, oy + dy, ORB_HI)
    if glow >= 2:
        for dx, dy in ((-2, -2), (4, -2), (-2, 4), (4, 4), (1, -4), (1, 6), (-4, 1), (6, 1)):
            c.dot(21 + dx, oy + dy, ORB)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.45)
    return img


# Sitting, everything above the waist drops this many pixels.
SIT_DROP = 6


def draw_lap(c, kick):
    """Sitting, seen from the front: the gown spreads over her knees and her legs dangle
    below the hem, one foot swinging up when kick is set."""
    for y, hw in ((23, 5), (24, 6), (25, 7), (26, 7)):
        c.rect(15 - hw, y, 16 + hw, y, GOWN)
        c.dot(15 - hw, y, GOWN_HI)
        c.dot(16 + hw, y, GOWN_SH)
        c.rect(14, y, 17, y, GOWN_HI)  # the front panel
    c.rect(8, 27, 23, 27, TRIM)  # the hem, over her knees
    for x, lift in ((12, kick), (18, 0)):
        c.rect(x, 28 - lift, x + 1, 29 - lift, SKIN)
        c.rect(x - (1 if x < 15 else 0), 30 - lift, x + 1 + (1 if x > 15 else 0), 30 - lift, SHOE)


# ---------- Bath time (the tub at home) ----------
# The hero lies back in the tub in their swimwear, seen from the front with their head at the
# left end: only what's above the water is drawn, and a row of bubbles along the bottom hides
# where the water would be. PlayerController lifts the sprite so the bubbles sit at the water
# line of the tub (Furniture.png's Bathtub), drawn just in front of it.

def bath_bubbles(c, frame):
    """The heap of bubbles along the bottom of a bathing frame, and a rubber duck bobbing on it."""
    for i, x in enumerate(range(2, 30, 4)):
        r = 2.6 if (i + frame) % 2 else 2.1
        c.ellipse(x + 0.5, 28.5 - (i % 2), r, r, BUBBLE)
        c.dot(round(x - r / 2), round(27.5 - (i % 2) - r / 2), BUBBLE_SHINE)
    c.rect(1, 29, 30, 31, BUBBLE)
    dy = frame  # the duck bobs
    c.rect(19, 24 + dy, 23, 26 + dy, DUCK)            # body
    c.rect(22, 21 + dy, 24, 23 + dy, DUCK)            # head
    c.dot(23, 22 + dy, EYE)
    c.rect(25, 22 + dy, 26, 22 + dy, BEAK)


def draw_princess_bathing(frame):
    """Marina in the bath: tiara on, golden hair spread over the rim behind her, a smile, her
    aquamarine swimsuit with its little straps, an arm along the rim and her toes at the far end."""
    c = Canvas()
    c.rect(2, 10, 5, 22, HAIR)                           # hair spilling over the tub's end
    c.rect(2, 14, 3, 22, HAIR_SH)
    for y in range(19, 28):                              # the swimsuit
        c.rect(10, y, 18, y, SUIT)
    c.rect(10, 19, 18, 19, SUIT_HI)
    c.rect(11, 17, 11, 18, SUIT)                         # its straps
    c.rect(17, 17, 17, 18, SUIT)
    c.rect(12, 17, 16, 18, SKIN)
    c.rect(18, 20, 25, 21, SKIN)                         # her arm along the rim
    c.rect(5, 11, 11, 17, SKIN)                          # face
    c.rect(5, 8, 11, 10, HAIR)                           # fringe
    c.rect(4, 9, 4, 17, HAIR)
    c.dot(7, 13, EYE)
    c.dot(10, 13, EYE)
    c.dot(6, 15, BLUSH)
    c.dot(11, 15, BLUSH)
    c.rect(8, 15, 9, 15, (200, 90, 110, 255))            # a smile
    c.rect(5, 7, 10, 7, GOLD)                            # tiara
    for x, y in ((5, 6), (10, 6), (7, 5), (8, 5)):
        c.dot(x, y, GOLD)
    c.rect(7, 6, 8, 6, GEM)
    toes = 1 if frame else 0
    c.rect(26, 20 - toes, 27, 23, SKIN)
    c.rect(28, 21, 29, 23, SKIN)
    bath_bubbles(c, frame)
    return c.img


def fallen(angle, darken=0.0):
    """Death frames: tip the hurt pose over around her feet, like the wizard."""
    big = Image.new("RGBA", (F * 3, F * 3), CLEAR)
    big.paste(draw_princess(hurt=True), (F, F))
    big = big.rotate(angle, resample=Image.NEAREST, center=(F + 15, F + 30))
    body = big.crop(big.getbbox())
    frame = Image.new("RGBA", (F, F), CLEAR)
    frame.paste(body, ((F - body.width) // 2, 31 - body.height))
    return tint(frame, (20, 10, 30), darken) if darken else frame


def build_animations():
    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        anims += [
            (f"Idle_{facing}", 4, True, [draw_princess(back), draw_princess(back, bob=1)]),
            (f"Walk_{facing}", 8, True, [
                draw_princess(back, legs=(1, 0)),
                draw_princess(back, bob=1),
                draw_princess(back, legs=(0, 1)),
                draw_princess(back, bob=1),
            ]),
            (f"Cast_{facing}", 14, False, [
                draw_princess(back, wand_up=2),
                draw_princess(back, wand_up=4, glow=1),
                draw_princess(back, wand_up=4, glow=2),
            ]),
            (f"Hurt_{facing}", 8, False, [draw_princess(back, bob=1, hurt=True)]),
        ]
    # Sitting on something (the toilet at home): front only, swinging her feet.
    anims.append(("Sit", 3, True, [draw_princess(sit=True), draw_princess(sit=True, kick=1)]))
    # Lying in the bath at home, in her swimsuit: front only.
    anims.append(("Bathe", 2, True, [draw_princess_bathing(0), draw_princess_bathing(1)]))
    anims.append(("Die", 8, False, [fallen(0), fallen(30), fallen(60), fallen(90, darken=0.35)]))
    return anims


if __name__ == "__main__":
    write_sheet("Princess", "Cast", build_animations())
