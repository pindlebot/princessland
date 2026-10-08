"""Generates the Slime enemy and its boss, the Slime King.

Run:  Tools/.venv/bin/python Tools/make_slime_sprites.py
Out:  Assets/Art/Slime.png / Slime.json           32x32 frames
      Assets/Art/SlimeKing.png / SlimeKing.json   64x64 frames: the same blob drawn at
                                                  2.6x the scale (not stretched pixels),
                                                  in royal purple with a gold crown
      Assets/Art/SlamWarning.png                  the red circle the King's slam telegraphs

All of the animation is squash and stretch: the blob gets wider and flatter as it lands,
taller and thinner as it springs up. The action state is "Attack" (a lunge), so both use
exactly the same state machine as the skeleton.
"""
from PIL import Image

from sprite_common import ART, Canvas, tint, write_sheet

GREEN = {"jelly": (88, 200, 96, 235), "shade": (52, 140, 64, 235), "shine": (180, 244, 170, 245),
         "eye": (24, 40, 30, 255), "mouth": (30, 70, 40, 255)}
ROYAL = {"jelly": (150, 84, 200, 240), "shade": (100, 50, 150, 240), "shine": (214, 170, 244, 245),
         "eye": (30, 16, 40, 255), "mouth": (60, 20, 60, 255)}
GLINT = (255, 255, 255, 255)
GOLD, GOLD_HI, GOLD_SH = (236, 192, 70, 255), (255, 240, 160, 255), (170, 120, 36, 255)
RUBY = (220, 40, 60, 255)


def draw_slime(back=False, w=9.0, h=7.0, lift=0, lean=0, mouth=False, hurt=False,
               size=32, k=1, colors=GREEN, crown=False):
    """w/h: half-width and height of the blob (squash/stretch); lift raises it off the
    ground (mid-hop); lean pushes the top forward (lunging). k scales everything, so the
    King (k=2 on a 64px canvas) is drawn with real detail rather than blown-up pixels."""
    c = Canvas(size)
    w, h, lift, lean = w * k, h * k, lift * k, lean * k
    ground = round(size - 2 - lift)
    cx = size / 2 - 0.5
    # The blob: a dome whose top can lean, built row by row from the ground up.
    for y in range(int(ground - 2 * h), ground + 1):
        t = min(1.0, (ground - y) / (2 * h))      # 0 at the bottom, 1 at the top
        half = w * max(0.0, 1 - t ** 2.2) ** 0.5  # rounded top, flat bottom
        shift = lean * t
        if half < 0.5:
            continue
        x0, x1 = round(cx - half + shift), round(cx + half + shift)
        c.rect(x0, y, x1, y, colors["jelly"])
        c.dot(x1, y, colors["shade"])
        if t < 0.15:
            c.rect(x0, y, x1, y, colors["shade"])  # darker where it meets the ground
    top = int(ground - 2 * h)
    c.ellipse(cx - w * 0.35 + lean * 0.6, top + h * 0.55, max(1.2, w * 0.25), max(1, h * 0.2), colors["shine"])

    if not back:
        e = max(1, round(k))  # whole-pixel size for eyes and mouth
        ey = int(ground - h * 1.05)
        for ex in (cx - w * 0.38, cx + w * 0.28):
            x = round(ex + lean * 0.5)
            if hurt:
                c.rect(x - e, ey, x + e, ey + e - 1, colors["eye"])
            else:
                c.rect(x - e, ey - e, x + e - 1, ey + e, colors["eye"])
                c.rect(x - e, ey - e, x - 1, ey - 1, GLINT)
        my = ey + 3 * e
        mx = round(cx + lean * 0.5)
        if mouth:
            c.rect(mx - 2 * e, my, mx + 2 * e, my + 2 * e, colors["mouth"])  # wide open
        else:
            for dx, dy in ((-1, 0), (0, 1), (1, 0)):  # a little smile
                c.rect(mx + dx * e, my + dy * e, mx + dx * e + e - 1, my + dy * e + e - 1, colors["mouth"])

    if crown:
        # A gold crown with three points and a ruby, riding on top of the blob.
        bx = round(cx + lean * 0.9)
        base = top + 2
        half = round(w * 0.32)
        c.rect(bx - half, base - 3, bx + half, base, GOLD)
        c.rect(bx - half, base, bx + half, base, GOLD_SH)
        for px in (bx - half, bx, bx + half):
            c.rect(px - 1, base - 8, px + 1, base - 3, GOLD)
            c.dot(px, base - 9, GOLD_HI)
        c.rect(bx - half, base - 3, bx + half, base - 3, GOLD_HI)
        c.rect(bx - 1, base - 2, bx + 1, base - 1, RUBY)

    img = c.img
    if hurt:
        img = tint(img, (255, 70, 70), 0.4)
    return img


def melt(stage, **style):
    """Death: the blob slumps into a puddle and fades."""
    shapes = [(9, 7), (11, 4.5), (13, 2.5), (14, 1.2)]
    w, h = shapes[stage]
    img = draw_slime(w=w, h=h, hurt=stage == 0, **{**style, "crown": style.get("crown") and stage < 2})
    if stage == 3:
        px = img.load()
        for y in range(img.height):
            for x in range(img.width):
                r, g, b, a = px[x, y]
                px[x, y] = (r, g, b, a // 2)  # a fading puddle
    return img


def build_animations(**style):
    def s(back, **pose):
        return draw_slime(back, **pose, **style)

    anims = []
    for facing in ("Front", "Back"):
        back = facing == "Back"
        anims += [
            # A gentle idle bounce: settle, squash, settle, a tiny hop
            (f"Idle_{facing}", 5, True, [s(back), s(back, w=9.6, h=6.6), s(back), s(back, w=8.6, h=7.4, lift=1)]),
            (f"Walk_{facing}", 8, True, [   # a hop: squash, spring up, airborne, land
                s(back, w=10.5, h=5.5),
                s(back, w=7.5, h=8.5, lift=2),
                s(back, w=8, h=8, lift=4),
                s(back, w=10, h=6),
            ]),
            (f"Attack_{facing}", 10, False, [  # rear back, lunge, recover
                s(back, w=10.5, h=5.5, lean=-2),
                s(back, w=8, h=8, lean=4, mouth=True),
                s(back, w=9, h=7, lean=1),
            ]),
            (f"Hurt_{facing}", 8, False, [s(back, w=11, h=5, hurt=True)]),
        ]
    anims.append(("Die", 8, False, [melt(stage, **style) for stage in range(4)]))
    return anims


def slam_warning():
    """A red ring with a faint fill: 'get out of here!' Laid flat on the floor in game."""
    img = Image.new("RGBA", (32, 32))
    px = img.load()
    for y in range(32):
        for x in range(32):
            d = ((x - 15.5) ** 2 + (y - 15.5) ** 2) ** 0.5
            if d <= 15.5:
                px[x, y] = (230, 40, 40, 230) if d >= 13.5 else (230, 40, 40, 70)
    return img


if __name__ == "__main__":
    write_sheet("Slime", "Attack", build_animations())
    write_sheet("SlimeKing", "Attack", build_animations(size=64, k=2.6, colors=ROYAL, crown=True), frame_size=64)
    slam_warning().save(ART / "SlamWarning.png")
