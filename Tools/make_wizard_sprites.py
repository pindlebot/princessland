"""Generates the wizard sprite sheet used by the player.

Run:  Tools/.venv/bin/python Tools/make_wizard_sprites.py
Out:  Assets/Art/Wizard.png        sprite sheet (32x32 frames, one animation per row)
      Assets/Art/Wizard.json       layout: which row/frames/fps belongs to which animation
      Assets/Art/Shadow.png        soft blob shadow drawn under every character

Only two facings are drawn: "Front" (toward the camera, screen down-right) and
"Back" (away from the camera, screen up-right). Left-facing versions come from
flipping the sprite horizontally at runtime (SpriteRenderer.flipX).
"""
import math

from PIL import Image

from sprite_common import CLEAR, F, Canvas, sleeping_quilt, tint, write_shadow, write_sheet, zzz

HAT, HAT_HI = (92, 56, 160, 255), (128, 92, 200, 255)
BAND = (232, 192, 72, 255)
SKIN = (240, 196, 160, 255)
BEARD, BEARD_SH = (232, 232, 240, 255), (180, 180, 196, 255)
EYE = (30, 20, 40, 255)
ROBE, ROBE_SH, ROBE_HI = (60, 92, 196, 255), (40, 60, 144, 255), (96, 130, 224, 255)
BOOT = (74, 48, 32, 255)
STAFF = (136, 88, 48, 255)
GEM, GEM_HI = (255, 150, 40, 255), (255, 240, 160, 255)
SUIT, SUIT_STRIPE = (214, 64, 72, 255), (246, 238, 232, 255)  # an old-fashioned striped bathing suit
BUBBLE, BUBBLE_SHINE = (238, 244, 252, 255), (255, 255, 255, 255)
DUCK, BEAK = (255, 214, 60, 255), (250, 140, 40, 255)


def draw_wizard(back=False, bob=0, legs=(0, 0), staff_up=0, glow=0, hurt=False, sit=False, kick=0):
    """Draws one pose. bob lowers the upper body, legs lifts each boot,
    staff_up raises the staff, glow (0-2) brightens the gem. sit draws him sitting down
    (facing front) with his boots dangling; kick swings one boot up."""
    c = Canvas()
    b = bob + (SIT_DROP if sit else 0)
    # While a boot is lifted, the other one steps out and the robe hem swings with it.
    sway = -1 if legs[1] > legs[0] else (1 if legs[0] > legs[1] else 0)

    if sit:
        draw_lap(c, kick)
    else:
        # Boots
        c.rect(11 - (sway < 0), 28 - legs[0], 13 - (sway < 0), 30 - legs[0], BOOT)
        c.rect(17 + (sway > 0), 28 - legs[1], 19 + (sway > 0), 30 - legs[1], BOOT)

        # Robe: a trapezoid that widens toward the floor
        top = 17 + b
        for y in range(top, 29):
            hw = 4 + (y - top) // 3
            cx = 15 + (sway if y >= 26 else 0)
            c.rect(cx - hw, y, cx + hw, y, ROBE_SH if back else ROBE)
            c.dot(cx - hw, y, ROBE_HI)
            c.dot(cx + hw, y, ROBE_SH)
        if back:  # a darker cape seam down the back
            c.rect(15, top + 1, 15, 28, ROBE_SH)
            c.rect(14, top + 1, 14, 27, ROBE)
        else:
            c.rect(11, 22 + b, 19, 22 + b, BAND)  # belt

    # Left sleeve and hand (resting on his knee when he sits)
    arm = 3 if sit else 0
    c.rect(9, 18 + b - arm, 10, 22 + b - arm, ROBE_SH)
    c.dot(9, 23 + b - arm, SKIN)
    c.dot(10, 23 + b - arm, SKIN)

    # Head
    if back:
        c.rect(12, 12 + b, 18, 16 + b, BEARD_SH)  # back of white hair
        c.rect(13, 16 + b, 17, 17 + b, BEARD_SH)
    else:
        c.rect(12, 12 + b, 18, 15 + b, SKIN)
        if hurt:  # eyes squeezed shut
            c.rect(13, 13 + b, 14, 13 + b, EYE)
            c.rect(17, 13 + b, 18, 13 + b, EYE)
        else:
            c.dot(14, 13 + b, EYE)
            c.dot(17, 13 + b, EYE)
        # beard
        c.rect(13, 15 + b, 18, 17 + b, BEARD)
        c.rect(14, 18 + b, 17, 18 + b, BEARD)
        c.rect(15, 19 + b, 16, 19 + b, BEARD_SH)

    # Hat: brim, band, then a cone whose tip flops to the right
    c.rect(9, 11 + b, 21, 11 + b, HAT)
    c.rect(11, 10 + b, 19, 10 + b, BAND)
    for y in range(2, 10):
        hw = (y - 2) // 2 + 1
        lean = 2 if y < 4 else (1 if y < 6 else 0)
        cx = 15 + lean
        c.rect(cx - hw, y + b, cx + hw, y + b, HAT)
        c.dot(cx - hw, y + b, HAT_HI)

    # Staff in the right hand, raised by staff_up while casting
    s = staff_up
    c.rect(22, 8 - s + b, 22, 29 - s - (2 if sit else 0), STAFF)
    c.rect(20, 18 + b, 21, 21 + b - min(s, 2), ROBE_SH)  # right sleeve reaches up
    c.rect(21, 20 - s + b, 23, 21 - s + b, SKIN)  # hand on staff
    gy = 5 - s + b
    c.rect(21, gy, 23, gy + 2, GEM)
    c.dot(22, gy + 1, GEM_HI if glow else GEM)
    if glow >= 1:
        for dx, dy in ((0, -2), (0, 4), (-2, 1), (4, 1)):
            c.dot(21 + dx, gy + dy, GEM_HI)
    if glow >= 2:
        for a in range(0, 360, 30):
            r = 5
            c.dot(22 + round(r * math.cos(math.radians(a))), gy + 1 + round(r * math.sin(math.radians(a))), GEM)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.45)
    return img


# Sitting, everything above the waist drops this many pixels.
SIT_DROP = 5


def draw_lap(c, kick):
    """Sitting, seen from the front: a short robe over his knees (belt and all) and his
    boots dangling below, one swinging up when kick is set."""
    c.rect(11, 22, 19, 22, ROBE)  # a little body between beard and belt
    c.rect(10, 23, 20, 23, BAND)
    for y, hw in ((24, 6), (25, 7), (26, 7)):
        c.rect(15 - hw, y, 15 + hw, y, ROBE)
        c.dot(15 - hw, y, ROBE_HI)
        c.dot(15 + hw, y, ROBE_SH)
    c.rect(8, 27, 22, 27, ROBE_SH)  # the hem over his knees
    for x, lift in ((11, kick), (17, 0)):
        c.rect(x, 28 - lift, x + 2, 30 - lift, BOOT)


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


def draw_wizard_bathing(frame):
    """Aldric in the bath: his hat still on (of course), eyes shut in bliss, beard floating on
    his red-and-white striped bathing suit, one arm along the rim and his toes poking up at the
    far end. frame 1 wiggles his toes."""
    c = Canvas()
    for y in range(19, 28):  # the bathing suit, stripes across
        c.rect(10, y, 19, y, SUIT if y % 2 else SUIT_STRIPE)
    c.rect(19, 20, 25, 21, SKIN)                         # his arm resting along the rim
    c.rect(4, 19, 9, 22, SKIN)                           # and the other one, behind his head
    c.rect(5, 13, 11, 18, SKIN)                          # face, tipped back against the tub
    for ex in (6, 9):                                    # eyes shut: two happy arcs
        c.rect(ex, 15, ex + 1, 15, EYE)
    c.rect(6, 17, 11, 20, BEARD)                         # the beard, spread out on his chest
    c.rect(8, 21, 12, 23, BEARD)
    c.rect(9, 24, 11, 24, BEARD_SH)
    c.rect(3, 12, 13, 12, HAT)                           # hat brim and band
    c.rect(5, 11, 11, 11, BAND)
    for y in range(4, 11):                               # the cone, flopping to the left this time
        hw = (y - 4) // 2 + 1
        cx = 8 - (2 if y < 6 else 1 if y < 8 else 0)
        c.rect(cx - hw, y, cx + hw, y, HAT)
        c.dot(cx - hw, y, HAT_HI)
    toes = 1 if frame else 0
    c.rect(26, 20 - toes, 27, 23, SKIN)                  # two feet poking out of the bubbles
    c.rect(28, 21, 29, 23, SKIN)
    bath_bubbles(c, frame)
    return c.img


# ---------- Bedtime (the bed at home) ----------

def draw_wizard_sleeping(frame):
    """Aldric tucked up in bed: hat still on (its tip flopped over the pillow), eyes shut, his
    beard spread over the quilt. frame 1 breathes in, with the Zs drifting up."""
    c = Canvas()
    sleeping_quilt(c, frame)
    # the hat: a cone lying back across the pillow, its tip flopped to the left
    for y, (x0, x1) in zip(range(0, 5), ((3, 7), (5, 12), (7, 15), (9, 18), (10, 20))):
        c.rect(x0, y + 1, x1, y + 1, HAT)
        c.dot(x0, y + 1, HAT_HI)
    c.rect(9, 6, 21, 6, HAT)                                   # brim
    c.rect(11, 5, 19, 5, BAND)
    c.rect(11, 7, 19, 11, SKIN)                                # face
    for ex in (13, 17):                                        # eyes shut: two happy arcs
        c.rect(ex, 9, ex + 1, 9, EYE)
    c.dot(15, 11, SKIN)
    top = 12 - frame
    c.rect(10, top, 20, top + 2, BEARD)                        # the beard, over the sheet
    c.rect(11, top + 3, 19, top + 4, BEARD)
    c.rect(13, top + 5, 17, top + 5, BEARD)
    c.rect(14, top + 6, 16, top + 6, BEARD_SH)
    c.rect(22, 17, 24, 18, SKIN)                               # a hand resting on the quilt
    zzz(c, frame)
    return c.img


def fallen(angle, darken=0.0):
    """Death frames: rotate the hurt pose around the feet and re-seat it on the floor."""
    big = Image.new("RGBA", (F * 3, F * 3), CLEAR)
    big.paste(draw_wizard(hurt=True), (F, F))
    big = big.rotate(angle, resample=Image.NEAREST, center=(F + 15, F + 30))
    box = big.getbbox()
    body = big.crop(box)
    frame = Image.new("RGBA", (F, F), CLEAR)
    frame.paste(body, ((F - body.width) // 2, 31 - body.height))
    return tint(frame, (20, 10, 30), darken) if darken else frame


def build_animations():
    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        anims += [
            (f"Idle_{facing}", 4, True, [draw_wizard(back), draw_wizard(back, bob=1)]),
            (f"Walk_{facing}", 8, True, [
                draw_wizard(back, legs=(2, 0)),
                draw_wizard(back, bob=1),
                draw_wizard(back, legs=(0, 2)),
                draw_wizard(back, bob=1),
            ]),
            (f"Cast_{facing}", 12, False, [
                draw_wizard(back, staff_up=2),
                draw_wizard(back, staff_up=4, glow=1),
                draw_wizard(back, staff_up=4, glow=2),
            ]),
            (f"Hurt_{facing}", 8, False, [draw_wizard(back, bob=1, hurt=True)]),
        ]
    # Sitting on something (the toilet at home): front only, swinging his boots.
    anims.append(("Sit", 3, True, [draw_wizard(sit=True), draw_wizard(sit=True, kick=1)]))
    # Lying in the bath at home, in his bathing suit: front only.
    anims.append(("Bathe", 2, True, [draw_wizard_bathing(0), draw_wizard_bathing(1)]))
    # Tucked up in bed at home: front only.
    anims.append(("Sleep", 2, True, [draw_wizard_sleeping(0), draw_wizard_sleeping(1)]))
    anims.append(("Die", 8, False, [fallen(0), fallen(30), fallen(60), fallen(90, darken=0.35)]))
    return anims


def main():
    write_sheet("Wizard", "Cast", build_animations())
    write_shadow()


if __name__ == "__main__":
    main()
