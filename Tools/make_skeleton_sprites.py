"""Generates the skeleton warrior sprite sheet used by the enemies.

Run:  Tools/.venv/bin/python Tools/make_skeleton_sprites.py
Out:  Assets/Art/Skeleton.png / Skeleton.json   (same layout conventions as the wizard)

Same two drawn facings as the wizard (Front / Back), mirrored at runtime for left/right.
Its action state is "Attack": a three-frame sword swing.
"""
from PIL import Image

from sprite_common import CLEAR, F, Canvas, tint, write_sheet

BONE, BONE_SH = (228, 222, 200, 255), (168, 160, 140, 255)
SOCKET = (34, 22, 34, 255)
EYE = (255, 64, 40, 255)
CLOTH, CLOTH_SH = (120, 52, 44, 255), (84, 34, 32, 255)
BLADE, BLADE_SH = (196, 206, 216, 255), (128, 138, 154, 255)
HILT = (122, 82, 42, 255)

# Sword poses: (hand x, hand y, blade tip x, blade tip y)
SWORD = {
    "rest": (21, 20, 21, 8),     # held upright at the side
    "raised": (20, 13, 26, 3),   # wind-up over the shoulder
    "swing": (22, 19, 30, 25),   # slashing down and forward
    "low": (21, 23, 30, 29),     # follow-through
}


def draw_skeleton(back=False, bob=0, legs=(0, 0), sword="rest", hurt=False):
    c = Canvas()
    b = bob

    # Legs: two bones each, feet on the floor row. A lifted leg steps outward.
    for i, x in enumerate((12, 18)):
        lift = legs[i]
        step = (-1 if i == 0 else 1) if lift else 0
        c.rect(x + step, 25 - lift, x + 1 + step, 29 - lift, BONE)
        c.dot(x + 1 + step, 27 - lift, BONE_SH)  # knee
        c.rect(x - 1 + step, 30 - lift, x + 1 + step, 30 - lift, BONE_SH)  # foot

    # Tattered loincloth with a ragged hem
    c.rect(11, 21 + b, 20, 23 + b, CLOTH_SH if back else CLOTH)
    for x in range(11, 21, 2):
        c.dot(x, 24 + b, CLOTH_SH)
    c.rect(11, 21 + b, 20, 21 + b, BONE_SH)  # pelvis rim

    # Spine and ribcage (ribs alternate with dark gaps)
    c.rect(15, 13 + b, 16, 20 + b, BONE_SH if back else BONE)
    for y in (14, 16, 18):
        c.rect(12, y + b, 19, y + b, BONE_SH if back else BONE)
        c.rect(13, y + 1 + b, 18, y + 1 + b, SOCKET)
    c.rect(15, 15 + b, 16, 19 + b, BONE)  # spine drawn over the gaps
    c.rect(11, 13 + b, 20, 13 + b, BONE)  # shoulders

    # Left arm hangs down
    c.rect(10, 14 + b, 10, 19 + b, BONE)
    c.rect(9, 20 + b, 10, 20 + b, BONE_SH)

    # Skull
    c.rect(13, 4 + b, 18, 4 + b, BONE)
    c.rect(12, 5 + b, 19, 9 + b, BONE)
    c.rect(13, 10 + b, 18, 10 + b, BONE)
    c.dot(12, 9 + b, BONE_SH)
    c.dot(19, 9 + b, BONE_SH)
    if back:
        c.rect(13, 9 + b, 18, 10 + b, BONE_SH)  # shade the lower back of the skull for roundness
        c.rect(14, 11 + b, 17, 11 + b, BONE_SH)
    else:
        for sx in (13, 17):  # eye sockets with a red glint
            c.rect(sx, 6 + b, sx + 1, 8 + b, SOCKET)
            c.dot(sx + (0 if hurt else 1), 7 + b, (255, 255, 255, 255) if hurt else EYE)
        c.dot(15, 9 + b, SOCKET)
        c.dot(16, 9 + b, SOCKET)  # nose
        for x in range(13, 19):  # teeth
            c.dot(x, 11 + b, BONE if x % 2 else SOCKET)

    # Right arm and sword
    hx, hy, tx, ty = SWORD[sword]
    hy += b
    c.line(20, 13 + b, hx, hy, BONE)  # arm from shoulder to hand
    c.line(hx, hy, tx, ty, BLADE)
    c.line(hx + 1, hy, tx + 1, ty, BLADE_SH)  # second pixel gives the blade an edge
    gx, gy = (hx - 1, hy) if sword in ("rest", "raised") else (hx, hy - 1)
    c.rect(gx - 1, gy, gx + 2, gy, HILT)  # cross-guard
    c.dot(hx, hy, HILT)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.45)
    return img


def collapse(stage):
    """Death frames: the bones sink into a heap, then settle as a darkened pile."""
    if stage < 3:
        body = draw_skeleton(hurt=True)
        upper, lower = body.crop((0, 0, F, 21)), body.crop((0, 21, F, F))
        frame = Image.new("RGBA", (F, F), CLEAR)
        frame.paste(lower, (0, 21))
        frame.alpha_composite(upper, (stage, 4 * stage))  # upper body drops onto the legs
        return frame

    c = Canvas()
    c.line(4, 30, 20, 30, BLADE_SH)  # dropped sword
    c.line(4, 29, 19, 29, BLADE)
    c.rect(20, 28, 20, 31, HILT)
    for x0, y0, x1, y1 in ((7, 27, 15, 25), (16, 28, 25, 26), (10, 28, 18, 28), (21, 29, 27, 29), (6, 25, 11, 28)):
        c.line(x0, y0, x1, y1, BONE)  # scattered bones, two pixels thick
        c.line(x0, y0 + 1, x1, y1 + 1, BONE_SH)
    c.rect(13, 19, 20, 23, BONE)  # skull resting on top of the heap
    c.rect(14, 18, 19, 18, BONE)
    c.rect(14, 24, 19, 24, BONE_SH)
    c.rect(14, 20, 15, 21, SOCKET)
    c.rect(18, 20, 19, 21, SOCKET)
    for x in range(14, 20):
        c.dot(x, 23, BONE if x % 2 else SOCKET)
    return tint(c.img, (20, 10, 30), 0.2)


def build_animations():
    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_skeleton(back), draw_skeleton(back, bob=1)]),
            (f"Walk_{facing}", 8, True, [
                draw_skeleton(back, legs=(2, 0)),
                draw_skeleton(back, bob=1),
                draw_skeleton(back, legs=(0, 2)),
                draw_skeleton(back, bob=1),
            ]),
            (f"Attack_{facing}", 10, False, [
                draw_skeleton(back, sword="raised"),
                draw_skeleton(back, sword="swing"),
                draw_skeleton(back, bob=1, sword="low"),
            ]),
            (f"Hurt_{facing}", 8, False, [draw_skeleton(back, bob=1, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [collapse(0), collapse(1), collapse(2), collapse(3)]))
    return anims


if __name__ == "__main__":
    write_sheet("Skeleton", "Attack", build_animations())
