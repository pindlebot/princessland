"""Generates Amethyra, the friendly dragon of the castle grounds (Level 0).

Run:  Tools/.venv/bin/python Tools/make_dragon_sprites.py
Out:  Assets/Art/Dragon.png / Dragon.json   (64x64 frames: twice the heroes' size)
        Idle  4 frames, looping   breathing, wings shifting, tail swaying, a blink
        Talk  2 frames, looping   mouth opening and closing while she speaks
      Assets/Art/UI/PortraitDragon.png   32x32 head portrait for the dialogue box

She sits facing the camera, turned slightly to the right, so (unlike the heroes) she
needs no back view or mirroring. Built from layered ellipses, back to front.
"""
import math

from PIL import Image

from sprite_common import ART, Canvas, outline, write_sheet

D = 64  # frame size

BODY, BODY_SH, BODY_HI = (150, 98, 200, 255), (104, 64, 152, 255), (190, 146, 228, 255)
BELLY, BELLY_SH = (242, 222, 182, 255), (206, 180, 140, 255)
WING, WING_BONE = (176, 128, 214, 255), (110, 66, 158, 255)
HORN, HORN_SH = (240, 212, 140, 255), (190, 152, 84, 255)
EYE, PUPIL = (255, 214, 84, 255), (40, 20, 40, 255)
MOUTH, TONGUE = (70, 26, 50, 255), (232, 120, 140, 255)
CLAW = (246, 240, 228, 255)


def draw_dragon(breath=0, wing=0, tail=0, blink=False, mouth=False):
    """breath (0-1) swells the chest and lifts the head; wing (0-2) lifts the wings;
    tail (-1..1) swings the tail tip; mouth opens the jaw."""
    c = Canvas(D)
    b = breath

    # Wings (behind everything): a bony leading edge and a membrane hanging below it.
    for side, shoulder, tip, low in ((-1, (22, 34), (7, 12), (10, 42)), (1, (40, 33), (56, 11), (54, 40))):
        tx, ty = tip[0], tip[1] - wing
        for y in range(ty, low[1] + 1):
            t = (y - ty) / max(1, low[1] - ty)
            x_edge = round(tx + (low[0] - tx) * t)       # outer edge of the membrane
            x_inner = round(tx + (shoulder[0] - tx) * min(1.0, t * 1.6))
            lo, hi = sorted((x_edge, x_inner))
            c.rect(lo, y, hi, y, WING)
        c.line(shoulder[0], shoulder[1], tx, ty, WING_BONE)   # wing arm
        for k in (0.35, 0.65):  # finger bones fanning down
            c.line(tx, ty, round(tx + (low[0] - tx) * k * 1.4 + side * 2), low[1] - 2, WING_BONE)

    # Tail: a chain of shrinking circles curling round to the right, ending in a spade.
    points = [(40, 58), (47, 59), (53, 57), (57, 53), (58 + tail, 48), (57 + 2 * tail, 44)]
    for i, (x, y) in enumerate(points):
        r = 5 - i * 0.7
        c.ellipse(x, y, r, r, BODY if i % 2 else BODY_SH)
    sx, sy = points[-1]
    c.ellipse(sx, sy - 3, 2.5, 3, BODY_SH)  # spade tip

    # Body (sitting), hind legs and claws
    c.ellipse(30, 46 - b, 14, 14 + b, BODY)
    c.ellipse(26, 42 - b, 8, 7, BODY_HI)  # light on the upper-left
    for lx in (20, 40):
        c.ellipse(lx, 56, 6, 5, BODY_SH)
        for k in range(3):
            c.dot(lx - 3 + k * 3, 61, CLAW)

    # Belly plates
    c.ellipse(29, 48 - b, 8, 11 + b, BELLY)
    for y in range(40 - b, 58, 3):
        c.rect(23, y, 35, y, BELLY_SH)

    # Forearms resting on the belly
    for ax in (21, 37):
        c.ellipse(ax, 50, 3, 5, BODY_SH)
        c.dot(ax - 1, 55, CLAW)
        c.dot(ax + 1, 55, CLAW)

    # Neck, rising to the head
    hy = -b  # the head lifts slightly as she breathes in
    c.ellipse(31, 30 + hy, 6, 9, BODY)
    c.ellipse(32, 31 + hy, 3, 7, BELLY)

    # Head (turned a little to the right) and snout
    c.ellipse(32, 17 + hy, 9, 7, BODY)
    c.ellipse(29, 15 + hy, 5, 3, BODY_HI)
    c.ellipse(40, 20 + hy, 7, 4, BODY)
    c.dot(45, 18 + hy, PUPIL)  # nostril
    if mouth:
        c.rect(36, 22 + hy, 45, 23 + hy, MOUTH)
        c.rect(38, 23 + hy, 42, 23 + hy, TONGUE)
        c.rect(36, 24 + hy, 44, 25 + hy, BODY)  # the lower jaw, dropped open
        c.dot(37, 22 + hy, CLAW)  # a little fang
    else:
        c.line(36, 22 + hy, 45, 22 + hy, BODY_SH)  # closed jaw line

    # Eye
    if blink:
        c.rect(33, 15 + hy, 36, 15 + hy, PUPIL)
    else:
        c.rect(33, 14 + hy, 36, 16 + hy, EYE)
        c.rect(35, 14 + hy, 35, 16 + hy, PUPIL)  # slit pupil

    # Horns sweeping back, and a frill
    for base, tip in (((27, 11), (21, 2)), ((33, 10), (30, 1))):
        c.line(base[0], base[1] + hy, tip[0], tip[1] + hy, HORN)
        c.line(base[0] + 1, base[1] + hy, tip[0] + 1, tip[1] + 1 + hy, HORN_SH)
    for k in range(3):
        c.dot(24 - k, 16 + k * 2 + hy, WING_BONE)

    return c.img


def portrait():
    """Her head on a dusky violet background, for the dialogue box."""
    head = outline(draw_dragon().crop((16, 0, 48, 32)))
    img = Image.new("RGBA", (32, 32))
    px = img.load()
    for y in range(32):
        for x in range(32):
            px[x, y] = (48 + y, 30 + y // 2, 70 + y, 255)
    img.alpha_composite(head)
    return img


def build_animations():
    idle = [
        draw_dragon(),
        draw_dragon(breath=1, wing=1, tail=1),
        draw_dragon(breath=1, wing=2, tail=0),
        draw_dragon(wing=1, tail=-1, blink=True),
    ]
    talk = [draw_dragon(mouth=True), draw_dragon(breath=1)]
    return [("Idle", 3, True, idle), ("Talk", 8, True, talk)]


if __name__ == "__main__":
    write_sheet("Dragon", None, build_animations(), frame_size=D)
    portrait().save(ART / "UI" / "PortraitDragon.png")
