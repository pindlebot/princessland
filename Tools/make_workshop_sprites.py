"""Generates the art for Dad's Workshop, the secret room behind a fake wall in the dungeon.

Run:  Tools/.venv/bin/python Tools/make_workshop_sprites.py
Out:  Assets/Art/WorkshopProps.png / .json   48x48 frames, bottom pivot:
        WorkshopDesk 2 frames, looping  a wooden desk with a computer showing a tiny picture of the Unity editor,
                                  a steaming coffee mug and sticky notes
        WorkshopNote 1 frame      a paper pinned on a little easel: a signed note, with a heart
Same conventions as make_woods_sprites.py.
"""
import palette as pal
from sprite_common import Canvas, write_sheet

S = 48
WOOD, WOOD_HI, WOOD_SH = (150, 98, 62, 255), (190, 132, 86, 255), (104, 66, 50, 255)
BEZEL, BEZEL_HI = (58, 56, 66, 255), (92, 90, 104, 255)
UI_DARK, UI_MID, UI_LIGHT = (44, 44, 50, 255), (62, 62, 70, 255), (150, 150, 160, 255)
SKY, SKY_LO = (110, 150, 196, 255), (150, 186, 214, 255)
BLUE = (66, 140, 230, 255)
CUBE_T, CUBE_L, CUBE_R = (236, 236, 244, 255), (188, 188, 204, 255), (138, 138, 160, 255)
STICKY, STICKY_SH = (255, 226, 96, 255), (222, 190, 70, 255)
MUG, MUG_HI = (232, 232, 240, 255), (255, 255, 255, 255)
COFFEE = (96, 56, 40, 255)
STEAM = (240, 240, 250, 200)
PAPER, PAPER_SH, INK = (252, 244, 222, 255), (222, 208, 176, 255), (92, 70, 110, 255)
HEART = (226, 70, 96, 255)


def draw_desk(frame=0):
    c = Canvas(S)
    # The desk: a slab on four legs, with a drawer.
    c.rect(4, 29, 43, 31, WOOD_HI)
    c.rect(4, 31, 43, 32, WOOD)
    c.rect(4, 32, 43, 32, WOOD_SH)
    c.rect(6, 33, 9, 44, WOOD)
    c.rect(38, 33, 41, 44, WOOD)
    c.rect(6, 33, 6, 44, WOOD_HI)
    c.rect(38, 33, 38, 44, WOOD_HI)
    c.rect(9, 34, 38, 38, WOOD_SH)
    c.rect(16, 35, 31, 37, WOOD)
    c.rect(23, 36, 24, 36, WOOD_HI)
    # The monitor: dark bezel, a stand, and the editor on screen.
    c.rect(11, 5, 35, 24, BEZEL)
    c.rect(11, 5, 35, 5, BEZEL_HI)
    c.rect(21, 25, 25, 28, BEZEL)
    c.rect(18, 28, 28, 29, BEZEL)
    sx0, sy0, sx1, sy1 = 13, 7, 33, 22
    c.rect(sx0, sy0, sx1, sy1, UI_DARK)
    c.rect(sx0, sy0, sx1, sy0 + 1, UI_MID)                          # the toolbar...
    c.dot(sx0 + 2, sy0, UI_LIGHT); c.dot(sx0 + 4, sy0, UI_LIGHT)
    c.rect(sx0 + 9, sy0, sx0 + 10, sy0 + 1, BLUE)                   # ...with a blue play button
    c.rect(sx0, sy0 + 2, sx0 + 4, sy1, UI_MID)                      # the hierarchy
    for y in range(sy0 + 3, sy1, 2):
        c.rect(sx0 + 1, y, sx0 + 1 + (y % 3) + 1, y, UI_LIGHT)
    c.rect(sx1 - 4, sy0 + 2, sx1, sy1, UI_MID)                      # the inspector
    for y in range(sy0 + 3, sy1, 3):
        c.rect(sx1 - 3, y, sx1 - 1, y, UI_LIGHT)
    c.rect(sx0 + 5, sy0 + 2, sx1 - 5, sy1, SKY)                     # the scene view: sky, a grid floor and a cube
    c.rect(sx0 + 5, sy0 + 10, sx1 - 5, sy1, SKY_LO)
    for x in range(sx0 + 5, sx1 - 4, 3):
        c.line(x, sy0 + 11, x + 2, sy1, (190, 210, 230, 255))
    cx, cy = (sx0 + sx1) // 2, sy0 + 8
    c.rect(cx - 1, cy - 2, cx + 1, cy - 2, CUBE_T)
    c.rect(cx - 2, cy - 1, cx, cy + 2, CUBE_L)
    c.rect(cx + 1, cy - 1, cx + 2, cy + 2, CUBE_R)
    # A keyboard, sticky notes and a steaming mug.
    c.rect(15, 29, 29, 29, UI_LIGHT)
    c.rect(36, 24, 40, 29, MUG)
    c.rect(36, 24, 36, 29, MUG_HI)
    c.rect(37, 24, 39, 24, COFFEE)
    c.rect(41, 25, 42, 27, MUG)
    for k, (dx, dy) in enumerate(((0, 0), (1, -3), (-1, -6))):
        if (k + frame) % 2 == 0:
            c.dot(38 + dx, 22 + dy, STEAM)
            c.dot(38 + dx, 21 + dy, STEAM)
    c.rect(5, 23, 9, 27, STICKY)
    c.rect(5, 27, 9, 27, STICKY_SH)
    c.line(6, 25, 8, 25, STICKY_SH)
    c.rect(6, 28, 8, 28, STICKY)
    return c.img


def draw_note():
    c = Canvas(S)
    ground = 44
    for x0, x1, y0, y1 in ((14, 18, 30, ground), (30, 34, 30, ground)):          # the easel's legs
        c.line(x0 + 2, y0, x0, y1, WOOD)
        c.line(x1 - 2, y0, x1, y1, WOOD)
    c.line(14, ground, 18, 30, WOOD)
    c.line(34, ground, 30, 30, WOOD)
    c.rect(11, 29, 37, 31, WOOD_HI)                                              # the ledge
    c.rect(11, 31, 37, 31, WOOD_SH)
    c.rect(13, 6, 35, 28, PAPER)                                                  # the paper
    c.rect(13, 28, 35, 28, PAPER_SH)
    c.rect(35, 6, 35, 28, PAPER_SH)
    c.rect(22, 4, 26, 7, (210, 70, 70, 255))                                      # a red pin
    c.dot(24, 5, (255, 170, 160, 255))
    for i, (y, w) in enumerate(((11, 17), (14, 15), (17, 18), (20, 9))):          # lines of handwriting
        x = 15
        while x < 15 + w:
            seg = 2 + (x + i) % 3
            c.rect(x, y, min(x + seg, 15 + w), y, INK)
            x += seg + 2
    c.ellipse(20.5, 23.5, 1.7, 1.5, HEART)
    c.ellipse(23.5, 23.5, 1.7, 1.5, HEART)
    c.rect(19, 24, 25, 25, HEART)
    c.rect(20, 26, 24, 26, HEART)
    c.rect(21, 27, 23, 27, HEART)
    c.dot(22, 28, HEART)
    c.line(27, 25, 33, 23, INK)                                                   # a signature squiggle
    c.line(27, 26, 31, 26, INK)
    return c.img


if __name__ == "__main__":
    write_sheet("WorkshopProps", None, [
        ("WorkshopDesk", 2, True, [draw_desk(0), draw_desk(1)]),
        ("WorkshopNote", 1, False, [draw_note()]),
    ], frame_size=S)
