"""Generates the monsters of Hollow Farm: the Gourdling, the Strawman and the Pumpkin King.

Run:  Tools/.venv/bin/python Tools/make_farm_monster_sprites.py
Out:  Assets/Art/Gourdling.png / .json     32x32 frames: an angry little jack-o'-lantern that hops after you
      Assets/Art/Strawman.png / .json      32x32 frames: a scarecrow gone bad, swinging a pitchfork
      Assets/Art/PumpkinKing.png / .json   64x64 frames: the Gourdling at 2.6x the scale (real detail,
                                           not blown-up pixels), in deep orange with a thorny vine crown
                                           and purple fire licking off his head

Same conventions as the slime and the skeleton: Front / Back for Idle, Walk, Attack and Hurt, a single
Die, and "Attack" as the action state, so they all share the one Animator state machine. The Gourdlings
(and the King) animate by squash and stretch; the Strawman is drawn pose by pose like the skeleton.
"""
from PIL import Image

from sprite_common import CLEAR, F, Canvas, tint, write_sheet

# ---------- Gourdlings and the King ----------
ORANGE = {"skin": (232, 120, 36, 255), "shade": (184, 80, 28, 255), "shine": (255, 188, 96, 255),
          "face": (58, 22, 26, 255), "glow": (255, 224, 96, 255), "stem": (92, 112, 52, 255), "stem_hi": (130, 156, 74, 255)}
DEEP = {"skin": (196, 86, 30, 255), "shade": (140, 52, 30, 255), "shine": (240, 150, 80, 255),
        "face": (40, 14, 30, 255), "glow": (200, 120, 255, 255), "stem": (70, 90, 56, 255), "stem_hi": (104, 128, 78, 255)}
VINE, VINE_HI = (60, 110, 60, 255), (120, 176, 88, 255)
FLAME, FLAME_HI = (160, 90, 230, 255), (226, 190, 255, 255)
GEM = (200, 120, 255, 255)
SEED = (250, 236, 190, 255)


def draw_pumpkin(back=False, w=9.0, h=7.0, lift=0, lean=0, mouth=False, hurt=False,
                 size=32, k=1, colors=ORANGE, king=False, flicker=0):
    """w/h: half-width and height of the pumpkin (squash/stretch); lift raises it off the ground (mid-hop);
    lean pushes the top forward (lunging). k scales everything, so the King (k=2 on a 64px canvas) is drawn
    with real detail rather than blown-up pixels."""
    c = Canvas(size)
    w, h, lift, lean = w * k, h * k, lift * k, lean * k
    ground = round(size - 2 - lift)
    cx = size / 2 - 0.5
    top = int(ground - 2 * h)

    # The body: a fat ellipse with a flat bottom, built row by row from the ground up.
    for y in range(top, ground + 1):
        t = min(1.0, (ground - y) / (2 * h))          # 0 at the bottom, 1 at the top
        u = 2 * t - 1
        half = w * max(0.0, 1 - u * u) ** 0.5
        if t < 0.1:
            half = max(half, w * 0.8)                  # a flat base
        shift = lean * t
        if half < 0.5:
            continue
        x0, x1 = round(cx - half + shift), round(cx + half + shift)
        c.rect(x0, y, x1, y, colors["skin"])
        c.dot(x1, y, colors["shade"])
        c.dot(x1 - 1, y, colors["shade"])
        if y == ground:
            c.rect(x0, y, x1, y, colors["shade"])       # just the bottom row is in shadow
        for r in (-0.62, -0.22, 0.22, 0.62):           # the ribs: darker lines that bow with the body
            rx = round(cx + shift + r * half)
            if x0 < rx < x1:
                c.dot(rx, y, colors["shade"])
    c.ellipse(cx - w * 0.5 + lean * 0.6, top + h * 0.55, max(1.4, w * 0.2), max(1, h * 0.17), colors["shine"])

    # The stem, with a curl of vine beside it.
    sx = round(cx + lean * 1.0)
    sw = max(1, round(k * 1.2))
    sh = max(3, round(k * 3.2))
    c.rect(sx - sw, top - sh, sx + sw, top + 1, colors["stem"])
    c.rect(sx - sw, top - sh, sx - sw, top, colors["stem_hi"])
    c.rect(sx + sw, top - sh - 1, sx + sw + max(1, round(k)), top - sh, colors["stem"])
    if not king:
        c.line(sx + sw + 1, top, sx + sw + round(2 * k), top - round(k), VINE_HI)
        c.dot(sx + sw + round(2 * k), top - round(k) - 1, VINE_HI)

    if not back:
        e = max(1, round(k))                                   # whole-pixel size for the face
        ey = int(ground - h * 1.15)
        fx = cx + lean * 0.5
        # Eyes: carved triangles with a glow inside, each under a heavy brow that slopes down toward the nose.
        # They're sized from the body, so the King's are as fierce as a Gourdling's, only bigger.
        eh = max(4, round(w * 0.34))
        reach = max(2, round(w * 0.2))
        glow = colors["glow"] if not hurt else (255, 255, 255, 255)
        for side, ex in ((-1, fx - w * 0.4), (1, fx + w * 0.4)):
            x = round(ex)
            for row in range(eh):
                half_w = round(row * reach / (eh - 1))
                c.rect(x - half_w, ey + row, x + half_w, ey + row, colors["face"])
            for row in range(2, eh - 1):
                half_w = round(row * reach / (eh - 1)) - 1
                c.rect(x - half_w, ey + row, x + half_w, ey + row, glow)
            span = reach + e
            for i in range(2 * span + 1):                       # the brow: high on the outside, low by the nose
                bx = x - side * span + side * i
                by = ey - e - 1 + round(i * e / (2 * span))
                c.rect(bx, by, bx, by + e - 1, colors["face"])
        # Mouth: a jagged grin, wide open when it attacks.
        my = ey + eh + 2 * e
        mw = round(w * (0.55 if mouth else 0.42))
        mh = (3 if mouth else 2) * e
        c.rect(round(fx) - mw, my, round(fx) + mw, my + mh, colors["face"])
        for i, tx in enumerate(range(round(fx) - mw, round(fx) + mw + 1, max(2, 2 * e))):
            c.rect(tx, my, tx + e - 1, my + e - 1, colors["skin"] if i % 2 == 0 else colors["face"])      # top teeth
            if i % 2 == 1:
                c.rect(tx, my + mh - e + 1, tx + e - 1, my + mh, colors["skin"])                          # bottom teeth
        if mouth:
            c.rect(round(fx) - mw + e, my + e, round(fx) + mw - e, my + 2 * e - 1, colors["glow"])

    if king:
        # A crown of thorny vine round his brow, with a purple gem, and purple fire flickering up.
        bx = round(cx + lean * 0.9)
        base = top + round(1.5 * k)
        half = round(w * 0.5)
        c.rect(bx - half, base - 2, bx + half, base, VINE)
        c.rect(bx - half, base - 2, bx + half, base - 2, VINE_HI)
        for i, px in enumerate(range(bx - half, bx + half + 1, max(2, half // 2))):
            spike = 4 + (i % 2) * 2
            c.rect(px - 1, base - 2 - spike, px, base - 2, VINE)
            c.dot(px - 1, base - 3 - spike, VINE_HI)
        c.rect(bx - 2, base - 1, bx + 2, base + 1, GEM)
        c.dot(bx - 1, base - 1, FLAME_HI)
        for i, fxp in enumerate(range(bx - half + 2, bx + half - 1, max(3, half // 2))):
            fh = 5 + ((i + flicker) % 3) * 2
            c.rect(fxp, base - 8 - fh, fxp + 1, base - 8, FLAME)
            c.dot(fxp, base - 9 - fh, FLAME_HI)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.4)
    return img


def smash(stage, **style):
    """Death: the pumpkin squashes, bursts into chunks with seeds flying, and settles as a pulpy pile."""
    colors = style.get("colors", ORANGE)
    size, k = style.get("size", 32), style.get("k", 1)
    if stage < 2:
        shapes = [(9.5, 5.5), (12, 3)]
        w, h = shapes[stage]
        return draw_pumpkin(w=w, h=h, hurt=True, **{**style, "king": style.get("king") and stage < 1})
    c = Canvas(size)
    ground = size - 2
    cx = size / 2 - 0.5
    rng_x = [(-0.55, 0.0, 1.0), (0.05, 0.1, 1.2), (0.5, 0.0, 0.9), (-0.2, 0.0, 0.8), (0.3, 0.35, 0.7)]
    for dx, dy, scale in rng_x:                                    # chunks of pumpkin with a rind
        cw, ch = max(2, round(5 * k * scale)), max(2, round(3 * k * scale))
        x = round(cx + dx * 12 * k)
        y = round(ground - ch - dy * 6 * k)
        c.rect(x - cw, y, x + cw, y + ch, colors["skin"])
        c.rect(x - cw, y + ch, x + cw, y + ch, colors["shade"])
        c.rect(x - cw, y, x - cw, y + ch, colors["shine"])
    c.rect(round(cx - 7 * k), ground - 1, round(cx + 7 * k), ground, colors["shade"])    # pulp on the floor
    for dx, dy in ((-3, -3), (2, -4), (6, -2), (-6, -1), (0, -1), (4, -1)):                # seeds
        c.dot(round(cx + dx * k), round(ground + dy * k), SEED)
    sx = round(cx - 2 * k)
    c.rect(sx - 1, ground - round(7 * k), sx + 1, ground - round(4 * k), colors["stem"])   # the stem, tossed on top
    img = c.img
    if stage == 3:
        px = img.load()
        for y in range(img.height):
            for x in range(img.width):
                r, g, b, a = px[x, y]
                px[x, y] = (r, g, b, a // 2)
    return tint(img, (20, 10, 30), 0.15) if stage == 3 else img


def pumpkin_animations(**style):
    def s(back, **pose):
        return draw_pumpkin(back, **pose, **style)

    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        flick = lambda i: {"flicker": i} if style.get("king") else {}
        anims += [
            (f"Idle_{facing}", 5, True, [s(back, **flick(0)), s(back, w=9.6, h=6.6, **flick(1)), s(back, **flick(2)),
                                         s(back, w=8.6, h=7.4, lift=1, **flick(0))]),
            (f"Walk_{facing}", 8, True, [   # a hop: squash, spring up, airborne, land
                s(back, w=10.5, h=5.5),
                s(back, w=7.5, h=8.5, lift=2, flicker=1),
                s(back, w=8, h=8, lift=4, flicker=2),
                s(back, w=10, h=6),
            ]),
            (f"Attack_{facing}", 10, False, [  # rear back, lunge with the mouth wide, recover
                s(back, w=10.5, h=5.5, lean=-2),
                s(back, w=8, h=8, lean=4, mouth=True),
                s(back, w=9, h=7, lean=1),
            ]),
            (f"Hurt_{facing}", 8, False, [s(back, w=11, h=5, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [smash(stage, **style) for stage in range(4)]))
    return anims


# ---------- The Strawman ----------
SACK, SACK_SH = (214, 176, 116, 255), (168, 128, 82, 255)
STRAW, STRAW_SH = (240, 210, 100, 255), (200, 164, 60, 255)
SHIRT, SHIRT_SH = (170, 60, 54, 255), (122, 40, 44, 255)
PATCH = (96, 130, 160, 255)
DENIM, DENIM_SH = (84, 94, 132, 255), (58, 66, 98, 255)
HAT, HAT_SH = (86, 62, 52, 255), (58, 40, 40, 255)
ROPE = (150, 110, 70, 255)
STITCH = (52, 28, 40, 255)
EYE_GLOW = (255, 128, 40, 255)
BOOTS = (74, 48, 36, 255)
SHAFT, TINE = (146, 100, 56, 255), (196, 204, 214, 255)

# Pitchfork poses: (hand x, hand y, tip x, tip y)
FORK = {
    "rest": (21, 20, 21, 6),
    "raised": (20, 14, 26, 2),
    "thrust": (22, 19, 31, 24),
    "low": (21, 23, 30, 29),
}


def draw_strawman(back=False, bob=0, legs=(0, 0), fork="rest", hurt=False):
    c = Canvas()
    b = bob

    # Legs: patched denim, straw poking from the cuffs.
    for i, x in enumerate((12, 18)):
        lift = legs[i]
        step = (-1 if i == 0 else 1) if lift else 0
        c.rect(x + step, 24 - lift, x + 2 + step, 28 - lift, DENIM_SH if back else DENIM)
        c.dot(x + 2 + step, 26 - lift, PATCH)
        c.rect(x - 1 + step, 29 - lift, x + 2 + step, 30 - lift, BOOTS)
        c.dot(x + step, 28 - lift, STRAW)
        c.dot(x + 2 + step, 28 - lift, STRAW_SH)

    # Torso: a checked shirt with a patch and straw at the hem.
    for y in range(14 + b, 25 + b):
        for x in range(10, 22):
            check = ((x // 2) + (y // 2)) % 2 == 0
            c.dot(x, y, (SHIRT_SH if check else SHIRT) if not back else (SHIRT_SH if check else (150, 50, 48, 255)))
    if not back:
        c.rect(14, 19 + b, 17, 22 + b, PATCH)
        c.dot(15, 20 + b, STITCH)
        c.dot(16, 21 + b, STITCH)
    for x in range(10, 22, 2):
        c.dot(x, 25 + b, STRAW if x % 4 else STRAW_SH)
        c.dot(x + 1, 26 + b, STRAW_SH)

    # Left arm hanging, with a tuft of straw for a hand.
    c.rect(8, 15 + b, 9, 22 + b, SHIRT_SH)
    c.rect(7, 23 + b, 9, 24 + b, STRAW)
    c.dot(6, 24 + b, STRAW_SH)

    # Head: a burlap sack tied at the neck, under a floppy hat, straw sticking out.
    c.rect(11, 5 + b, 20, 12 + b, SACK)
    c.rect(10, 7 + b, 21, 10 + b, SACK)
    c.rect(20, 6 + b, 21, 11 + b, SACK_SH)
    c.rect(12, 13 + b, 19, 13 + b, ROPE)
    for x in (10, 21):
        c.dot(x - (1 if x < 15 else -1), 11 + b, STRAW)
        c.dot(x, 12 + b, STRAW_SH)
    c.rect(9, 4 + b, 22, 4 + b, HAT)
    c.rect(12, 0 + b, 19, 3 + b, HAT)
    c.rect(12, 3 + b, 19, 3 + b, HAT_SH)
    c.dot(11, 5 + b, HAT)
    c.dot(21, 5 + b, HAT_SH)
    if back:
        c.rect(15, 6 + b, 15, 12 + b, SACK_SH)        # the seam up the back of the sack
        for y in (7, 9, 11):
            c.rect(14, y + b, 16, y + b, STITCH)
    else:
        for ex in (13, 17):                            # glowing, stitched-on button eyes
            c.rect(ex, 7 + b, ex + 1, 8 + b, (255, 255, 255, 255) if hurt else EYE_GLOW)
            c.dot(ex - 1, 6 + b, STITCH)
            c.dot(ex + 2, 9 + b, STITCH)
        c.rect(13, 10 + b, 18, 10 + b, STITCH)         # a stitched, crooked mouth
        for x in range(13, 19, 2):
            c.rect(x, 9 + b, x, 11 + b, STITCH)

    # The pitchfork in the right hand.
    hx, hy, tx, ty = FORK[fork]
    hy += b
    c.line(20, 14 + b, hx, hy, SHIRT_SH)               # the arm from the shoulder
    c.line(hx, hy, tx, ty, SHAFT)
    c.line(hx + 1, hy, tx + 1, ty, HAT_SH)
    dx, dy = tx - hx, ty - hy                           # three tines at the tip, pointing along the shaft
    n = max(abs(dx), abs(dy)) or 1
    ux, uy = dx / n, dy / n
    px, py = -uy, ux                                    # perpendicular
    for off in (-2, 0, 2):
        bx, by = tx + px * off, ty + py * off
        c.line(round(bx), round(by), round(bx + ux * 3), round(by + uy * 3), TINE)
    c.dot(hx, hy, STRAW)
    c.dot(hx + 1, hy, STRAW_SH)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.45)
    return img


def unravel(stage):
    """Death: the stuffing spills out and the Strawman slumps into a heap of straw, rags and a hat."""
    if stage < 3:
        body = draw_strawman(hurt=True)
        upper, lower = body.crop((0, 0, F, 21)), body.crop((0, 21, F, F))
        frame = Image.new("RGBA", (F, F), CLEAR)
        frame.paste(lower, (0, 21))
        frame.alpha_composite(upper, (stage, 4 * stage))
        c = Canvas()
        for i in range(stage * 3):                       # straw spilling out
            c.dot(8 + i * 2, 27 + (i % 2), STRAW)
        frame.alpha_composite(c.img)
        return frame
    c = Canvas()
    for x0, y0, x1, y1 in ((5, 28, 25, 28), (7, 27, 22, 25), (9, 29, 26, 29), (6, 26, 14, 24), (16, 26, 24, 27)):
        c.line(x0, y0, x1, y1, STRAW)
        c.line(x0, y0 + 1, x1, y1 + 1, STRAW_SH)
    c.rect(8, 25, 22, 28, STRAW_SH)
    c.rect(12, 21, 19, 25, SHIRT_SH)                     # a rag
    c.rect(13, 22, 15, 23, PATCH)
    c.rect(10, 17, 21, 18, HAT)                          # the hat on top, brim and crown
    c.rect(13, 14, 18, 17, HAT)
    c.rect(13, 17, 18, 17, HAT_SH)
    c.line(3, 30, 18, 24, SHAFT)                         # the dropped pitchfork
    for dy in (-1, 0, 1):
        c.line(3, 30 + dy, 1, 30 + dy * 2, TINE)
    return tint(c.img, (20, 10, 30), 0.25)


def strawman_animations():
    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        anims += [
            (f"Idle_{facing}", 3, True, [draw_strawman(back), draw_strawman(back, bob=1)]),
            (f"Walk_{facing}", 7, True, [
                draw_strawman(back, legs=(2, 0)),
                draw_strawman(back, bob=1),
                draw_strawman(back, legs=(0, 2)),
                draw_strawman(back, bob=1),
            ]),
            (f"Attack_{facing}", 10, False, [
                draw_strawman(back, fork="raised"),
                draw_strawman(back, fork="thrust"),
                draw_strawman(back, bob=1, fork="low"),
            ]),
            (f"Hurt_{facing}", 8, False, [draw_strawman(back, bob=1, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [unravel(stage) for stage in range(4)]))
    return anims


if __name__ == "__main__":
    write_sheet("Gourdling", "Attack", pumpkin_animations())
    write_sheet("Strawman", "Attack", strawman_animations())
    write_sheet("PumpkinKing", "Attack", pumpkin_animations(size=64, k=2.6, colors=DEEP, king=True), frame_size=64)
