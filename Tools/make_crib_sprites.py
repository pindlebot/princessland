"""Generates the nursery crib in the castle, with the baby mermaid in it.

Run:  Tools/.venv/bin/python Tools/make_crib_sprites.py
Out:  Assets/Art/Crib.png / Crib.json   (48x48 frames, bottom pivot, one looping animation "Crib")

A wooden crib seen from the front, a little shell mobile swaying over it, and the baby mermaid
sitting up inside: coral hair with a bow, rosy cheeks, a shell top and a teal tail whose fin
flicks over the rail. Her wave and a blink loop on four frames.
"""
import palette
from sprite_common import Canvas, write_sheet

S = 48

WOOD, WOOD_SH, WOOD_HI = (160, 106, 58, 255), (112, 70, 38, 255), (200, 146, 86, 255)
GOLD = (236, 192, 70, 255)
SKIN, SKIN_SH = (244, 204, 176, 255), (222, 170, 146, 255)
BLUSH = (240, 158, 168, 255)
HAIR, HAIR_SH = palette.rgba(palette.CORAL), (196, 82, 104, 255)
BOW = (255, 240, 160, 255)
EYE = (40, 30, 50, 255)
SHELL, SHELL_SH = (255, 214, 224, 255), (226, 160, 184, 255)
TAIL, TAIL_SH, TAIL_HI = (64, 196, 180, 255), (34, 136, 134, 255), (132, 232, 214, 255)
BLANKET, BLANKET_SH = (214, 196, 240, 255), (176, 150, 214, 255)
MOBILE = (255, 255, 255, 255)
STRING = (200, 190, 200, 255)


def crib(frame):
    c = Canvas(S)
    sway = (0, 1, 0, -1)[frame]
    wave = (0, 2, 3, 0)[frame]   # how high her hand is raised
    blink = frame == 3
    fin = (0, 1, 0, 0)[frame]    # the tail fin flicks up on frame 1

    # the mobile: a bar over the crib with a star, a shell and a little fish hanging off it
    c.rect(12, 2, 36, 2, WOOD)
    c.rect(23, 0, 24, 2, WOOD_SH)
    for x, y1, kind in ((13, 8, "star"), (24, 11, "shell"), (35, 8, "fish")):
        x += sway
        c.rect(x, 3, x, y1 - 1, STRING)
        if kind == "star":
            c.rect(x - 1, y1, x + 1, y1 + 1, BOW)
            c.dot(x, y1 - 1, BOW)
            c.dot(x, y1 + 2, BOW)
        elif kind == "shell":
            c.rect(x - 2, y1, x + 2, y1 + 2, SHELL)
            c.rect(x - 1, y1 + 3, x + 1, y1 + 3, SHELL_SH)
            c.dot(x - 2, y1 + 2, SHELL_SH)
            c.dot(x + 2, y1 + 2, SHELL_SH)
        else:
            c.rect(x - 2, y1, x + 1, y1 + 2, TAIL)
            c.rect(x + 2, y1, x + 3, y1 + 2, TAIL_HI)
            c.dot(x - 1, y1 + 1, EYE)

    # posts with golden knobs, and the back rail with slats behind the baby
    for x in (6, 39):
        c.rect(x, 14, x + 2, 46, WOOD)
        c.rect(x, 14, x, 46, WOOD_HI)
        c.rect(x + 2, 14, x + 2, 46, WOOD_SH)
        c.rect(x, 11, x + 2, 13, GOLD)
    c.rect(9, 18, 38, 19, WOOD)
    for x in range(11, 38, 4):
        c.rect(x, 20, x, 36, WOOD_SH)
    c.rect(9, 18, 38, 18, WOOD_HI)

    # the baby: head with a coral tuft and a yellow bow, a sleepy-happy face, a shell top, one arm waving
    hx, hy = 24, 24
    c.ellipse(hx, hy + 1, 6, 6, HAIR)                       # hair behind and around the head
    c.ellipse(hx, hy + 2, 5, 5, SKIN)
    c.rect(hx - 4, hy - 4, hx + 4, hy - 2, HAIR)            # fringe
    c.rect(hx - 1, hy - 7, hx + 1, hy - 5, HAIR)            # a little curl on top
    c.rect(hx + 3, hy - 6, hx + 6, hy - 4, BOW)             # the bow
    c.dot(hx + 4, hy - 5, GOLD)
    if blink:
        c.rect(hx - 3, hy + 2, hx - 2, hy + 2, EYE)
        c.rect(hx + 2, hy + 2, hx + 3, hy + 2, EYE)
    else:
        c.rect(hx - 3, hy + 1, hx - 2, hy + 3, EYE)
        c.rect(hx + 2, hy + 1, hx + 3, hy + 3, EYE)
        c.dot(hx - 3, hy + 1, (255, 255, 255, 255))
        c.dot(hx + 2, hy + 1, (255, 255, 255, 255))
    c.dot(hx - 4, hy + 4, BLUSH)
    c.dot(hx + 4, hy + 4, BLUSH)
    c.rect(hx - 1, hy + 5, hx + 1, hy + 5, SKIN_SH)         # a tiny smile
    c.rect(hx - 5, hy + 7, hx + 5, hy + 10, SKIN)           # shoulders and tummy
    c.rect(hx - 5, hy + 8, hx - 2, hy + 9, SHELL)           # the shell top
    c.rect(hx + 2, hy + 8, hx + 5, hy + 9, SHELL)
    c.dot(hx - 5, hy + 9, SHELL_SH)
    c.dot(hx + 5, hy + 9, SHELL_SH)
    c.rect(hx - 9, hy + 6, hx - 7, hy + 10, SKIN)           # the left arm rests on the rail
    c.rect(hx + 7, hy + 7 - wave, hx + 9, hy + 10, SKIN)    # the right arm waves
    c.rect(hx + 7, hy + 6 - wave, hx + 10, hy + 7 - wave, SKIN)
    c.dot(hx + 10, hy + 5 - wave, SKIN_SH)

    # the tail starts behind the blanket
    for i, y in enumerate(range(hy + 11, hy + 15)):
        c.rect(hx - 4 + i, y, hx + 5 + i, y, TAIL)
        c.dot(hx - 4 + i, y, TAIL_HI)
        c.dot(hx + 5 + i, y, TAIL_SH)
    for dx in range(0, 6, 2):                               # scales
        c.dot(hx + dx, hy + 12, TAIL_SH)
        c.dot(hx + dx + 2, hy + 14, TAIL_SH)

    # front rail: a little pastel blanket folded over it, then the rail and its slats
    c.rect(9, 36, 38, 38, BLANKET)
    c.rect(9, 38, 38, 38, BLANKET_SH)
    for x in range(12, 38, 6):
        c.rect(x, 36, x + 1, 37, BLANKET_SH)
    # ...and her tail lying across the blanket, the fin flicking up at the end
    c.rect(26, 33, 36, 37, TAIL)
    c.rect(26, 33, 26, 37, TAIL_HI)
    c.rect(36, 33, 36, 37, TAIL_SH)
    for x in (29, 32, 35):
        c.dot(x, 35, TAIL_SH)
        c.dot(x - 1, 36, TAIL_HI)
    for i, y in enumerate(range(28 - fin, 33)):
        c.rect(34 - i // 2, y, 38 + i // 2, y, TAIL)
        c.dot(34 - i // 2, y, TAIL_HI)
        c.dot(38 + i // 2, y, TAIL_SH)
    c.rect(36, 29 - fin, 36, 33, TAIL_SH)
    c.rect(9, 39, 38, 40, WOOD)
    c.rect(9, 39, 38, 39, WOOD_HI)
    for x in range(11, 38, 4):
        c.rect(x, 41, x, 44, WOOD_SH)
    c.rect(9, 45, 38, 45, WOOD)
    c.rect(9, 45, 38, 45, WOOD_SH)
    for x in (7, 40):                                       # little feet
        c.rect(x - 1, 46, x + 1, 47, WOOD_SH)
    return c.img


if __name__ == "__main__":
    write_sheet("Crib", None, [("Crib", 3, True, [crib(f) for f in range(4)])], frame_size=S)
