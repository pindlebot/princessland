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


def draw_princess(back=False, bob=0, legs=(0, 0), wand_up=0, glow=0, hurt=False):
    """Same pose controls as the wizard: bob lowers the upper body, legs lifts a foot
    (the gown sways that way), wand_up raises the wand, glow (0-2) lights the orb."""
    c = Canvas()
    b = bob
    sway = -1 if legs[1] > legs[0] else (1 if legs[0] > legs[1] else 0)

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
    anims.append(("Die", 8, False, [fallen(0), fallen(30), fallen(60), fallen(90, darken=0.35)]))
    return anims


if __name__ == "__main__":
    write_sheet("Princess", "Cast", build_animations())
