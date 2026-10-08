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

from sprite_common import CLEAR, F, Canvas, tint, write_shadow, write_sheet

HAT, HAT_HI = (92, 56, 160, 255), (128, 92, 200, 255)
BAND = (232, 192, 72, 255)
SKIN = (240, 196, 160, 255)
BEARD, BEARD_SH = (232, 232, 240, 255), (180, 180, 196, 255)
EYE = (30, 20, 40, 255)
ROBE, ROBE_SH, ROBE_HI = (60, 92, 196, 255), (40, 60, 144, 255), (96, 130, 224, 255)
BOOT = (74, 48, 32, 255)
STAFF = (136, 88, 48, 255)
GEM, GEM_HI = (255, 150, 40, 255), (255, 240, 160, 255)


def draw_wizard(back=False, bob=0, legs=(0, 0), staff_up=0, glow=0, hurt=False):
    """Draws one pose. bob lowers the upper body, legs lifts each boot,
    staff_up raises the staff, glow (0-2) brightens the gem."""
    c = Canvas()
    b = bob
    # While a boot is lifted, the other one steps out and the robe hem swings with it.
    sway = -1 if legs[1] > legs[0] else (1 if legs[0] > legs[1] else 0)

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

    # Left sleeve and hand
    c.rect(9, 18 + b, 10, 22 + b, ROBE_SH)
    c.dot(9, 23 + b, SKIN)
    c.dot(10, 23 + b, SKIN)

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
    c.rect(22, 8 - s + b, 22, 29 - s, STAFF)
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
    anims.append(("Die", 8, False, [fallen(0), fallen(30), fallen(60), fallen(90, darken=0.35)]))
    return anims


def main():
    write_sheet("Wizard", "Cast", build_animations())
    write_shadow()


if __name__ == "__main__":
    main()
