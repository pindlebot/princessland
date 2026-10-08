"""Generates the pixel-art HUD pieces.

Run:  Tools/.venv/bin/python Tools/make_hud_sprites.py
Out:  Assets/Art/UI/
        Frame.png          16x16  gold-trimmed frame (9-sliced by USS: 4px borders)
        Panel.png          16x16  same frame with a translucent interior, for text boxes
        Slot.png           16x16  stone hotbar slot (9-sliced)
        PortraitWizard.png    24x24  head-and-shoulders portraits (HUD and character select)
        PortraitPrincess.png  24x24
        IconFireball.png      24x24  hotbar icons, one per spell
        IconTidalOrb.png      24x24
        IconEmberRing.png     24x24  inventory icon

      The storybook style (cream panels, honey-gold borders, plum ink). These are drawn
      smooth rather than as pixel art, at 4x the size they're shown at (1280x720), so they
      stay sharp on big and high-DPI screens. Sizes below are on-screen sizes:
        PanelCream.png        32x34  the panel every HUD box uses: plum outline, honey frame, an
                                     embroidered running stitch around the paper, and a soft plum
                                     shadow baked in below (9-sliced: 10px borders, 12px at the bottom)
        PanelStar.png         32x34  the same with two little four-point stars on its top corners,
                                     only for the main cards (status, objective, dialogue, pause)
        StitchRule.png        6x2    one stitch of the dashed divider (tiled by USS)
        StarBurst.png         32x32  a four-point star flashed over the spell slot when it's cast
        MinimapRing.png       140x140 the minimap's frame: honey ring, stitch, a star at the top
                                     (the way the camera looks), and a soft shadow
        Heart.png / HeartHalf.png / HeartEmpty.png   30x28  health, one heart per point
                                     (Gentle Mode's half hits show as a half heart)
        IconMagic.png         24x24  a turquoise sparkle beside the magic bar
        IconCoin.png          18x18  the coin counter
        IconMonster.png       24x24  "monsters left" in the objective card
        PipMonster.png / PipStar.png 18x18  progress markers: a monster, then a four-point star once defeated
        HurtVignette.png      a soft red glow around the screen's edges, flashed when the hero is hit
      The app icon (Assets/Art/AppIcon.png, 1024x1024): a gold crown on a plum tile, shaped like
      a macOS icon. Player Settings uses it as the default icon (CommandLineBuild.ApplyAppIcon).
      Still pixel art:
        Map*.png              tiny minimap markers: crown (hero), stairs (open/locked), monster,
                              chest and dragon, each with a plum outline so they read when small

The portrait and icon are cut from the actual game sprites, so they always match.
"""
import math

from PIL import Image, ImageChops, ImageDraw

import make_prop_sprites as props
import make_spell_sprites as spell
import make_princess_sprites as princess
import make_wizard_sprites as wizard
from sprite_common import ART, CLEAR, OUTLINE, Canvas, outline

UI = ART / "UI"


def frame(trim_hi, trim_lo, interior, size=16):
    """Bevelled frame: dark outline, a light/dark trim (lit from the top-left), an inner
    shadow, then the interior. Everything that matters sits in the 4px border, so the
    middle can be stretched to any size (9-slicing)."""
    img = Image.new("RGBA", (size, size), interior)
    px = img.load()
    last = size - 1
    for i in range(size):
        for a, b in ((i, 0), (i, last), (0, i), (last, i)):
            px[a, b] = OUTLINE
    for i in range(1, last):
        px[i, 1] = px[1, i] = trim_hi  # lit top and left edges
        px[i, last - 1] = px[last - 1, i] = trim_lo  # shaded bottom and right edges
    for i in range(2, last - 1):
        px[i, 2] = px[2, i] = trim_lo
        px[i, last - 2] = px[last - 2, i] = trim_hi
        px[i, 3] = px[3, i] = (12, 10, 16, interior[3])  # inner shadow
    return img


def portrait(idle_frame, top, background):
    """Head and shoulders from a character's idle frame, on a soft gradient."""
    bust = outline(idle_frame.crop((6, top, 26, top + 21)))
    r, g, b = background
    img = Image.new("RGBA", (24, 24))
    px = img.load()
    for y in range(24):
        for x in range(24):
            px[x, y] = (r + y, g + y, b + y // 2, 255)
    img.alpha_composite(bust, (2, 3))
    return img


def spell_icon(palette, outline_color):
    """The projectile tilted to fly up and to the right, trimmed to fit a 24x24 icon."""
    fly = spell.fly_frame(0, palette).rotate(35, resample=Image.NEAREST, center=(16, 16))
    fly = fly.crop(fly.getbbox())
    img = Image.new("RGBA", (24, 24), CLEAR)
    img.alpha_composite(fly, ((24 - fly.width) // 2, (24 - fly.height) // 2))
    return outline(img, outline_color)


# ---------- Storybook style ----------

CREAM, CREAM_SH = (248, 239, 216, 255), (232, 218, 188, 255)
HONEY, HONEY_HI, HONEY_SH = (222, 168, 70, 255), (250, 214, 120, 255), (164, 112, 40, 255)
PLUM = (74, 37, 69, 255)
HEART, HEART_HI, HEART_SH = (232, 70, 92, 255), (255, 170, 180, 255), (176, 36, 64, 255)
MAGIC, MAGIC_HI = (64, 206, 210, 255), (210, 255, 250, 255)


def pattern(rows, palette):
    """Pixel art from strings: each character is a palette key ('.' = transparent)."""
    img = Image.new("RGBA", (len(rows[0]), len(rows)), CLEAR)
    px = img.load()
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".":
                px[x, y] = palette[ch]
    return img


# ---------- Smooth drawing ----------
# The storybook pieces are drawn as shapes rather than pixel by pixel. Coordinates are in
# on-screen pixels (at 1280x720); the canvas is SCALE x that for the texture, and SS x more
# again while drawing. Shrinking by SS at the end averages each SS x SS block, which gives
# soft, anti-aliased edges.

SCALE = 4  # texture pixels per on-screen pixel: sharp up to 4K
SS = 4     # supersampling while drawing


class Smooth:
    def __init__(self, w, h):
        self.k = SCALE * SS
        self.size = (round(w * self.k), round(h * self.k))
        self.img = Image.new("RGBA", self.size, CLEAR)

    def _pts(self, points):
        return [(x * self.k, y * self.k) for x, y in points]

    def poly(self, points, grow=0.0):
        """A closed shape's coverage mask, grown (grow > 0) or shrunk (grow < 0) by `grow` pixels."""
        mask = Image.new("L", self.size, 0)
        d = ImageDraw.Draw(mask)
        p = self._pts(points)
        d.polygon(p, fill=255)
        if grow:
            d.line(p + [p[0]], fill=255 if grow > 0 else 0, width=round(abs(grow) * 2 * self.k), joint="curve")
        return mask

    def rounded(self, x0, y0, x1, y1, radius):
        mask = Image.new("L", self.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle(
            [x0 * self.k, y0 * self.k, x1 * self.k - 1, y1 * self.k - 1], radius=radius * self.k, fill=255)
        return mask

    def ellipse(self, cx, cy, rx, ry):
        mask = Image.new("L", self.size, 0)
        ImageDraw.Draw(mask).ellipse([(cx - rx) * self.k, (cy - ry) * self.k, (cx + rx) * self.k, (cy + ry) * self.k], fill=255)
        return mask

    def shift(self, mask, dx, dy):
        return ImageChops.offset(mask, round(dx * self.k), round(dy * self.k))

    def fill(self, mask, color):
        layer = Image.new("RGBA", self.size, color[:3] + (0,))
        layer.putalpha(mask if color[3] == 255 else mask.point(lambda v: v * color[3] // 255))
        self.img.alpha_composite(layer)

    def gradient(self, mask, top, bottom, y0, y1):
        """Fills `mask` with a vertical blend from `top` (at y0) to `bottom` (at y1)."""
        column = Image.new("RGBA", (1, self.size[1]))
        for y in range(self.size[1]):
            t = min(1.0, max(0.0, (y / self.k - y0) / max(1e-6, y1 - y0)))
            column.putpixel((0, y), tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)))
        layer = column.resize(self.size)
        layer.putalpha(ImageChops.multiply(layer.getchannel("A"), mask))
        self.img.alpha_composite(layer)

    def done(self):
        # Average in premultiplied alpha, so transparent pixels don't darken the edges.
        return self.img.convert("RGBa").reduce(SS).convert("RGBA")


def both(a, b):
    return ImageChops.multiply(a, b)


def minus(a, b):
    return ImageChops.subtract(a, b)


def star_points(cx, cy, outer, inner, points, turn=-90):
    pts = []
    for i in range(points * 2):
        r = outer if i % 2 == 0 else inner
        a = math.radians(turn + i * 180 / points)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


# ---------- Storybook style ----------

CREAM, CREAM_SH = (248, 239, 216, 255), (232, 218, 188, 255)
HONEY, HONEY_HI, HONEY_SH = (222, 168, 70, 255), (250, 214, 120, 255), (164, 112, 40, 255)
PLUM, SOFT_PLUM = (74, 37, 69, 255), (128, 88, 118, 255)
HEART, HEART_HI, HEART_SH = (232, 70, 92, 255), (255, 170, 180, 255), (176, 36, 64, 255)
MAGIC, MAGIC_HI = (64, 206, 210, 255), (210, 255, 250, 255)
GOLD, GOLD_HI, GOLD_SH = (246, 196, 64, 255), (255, 240, 170, 255), (196, 132, 30, 255)
BUTTER_HI = (255, 246, 200, 255)
WHITE = (255, 255, 255, 255)


def cream_panel(stars=False):
    """Cream paper in a honey-gold frame with a plum outline and rounded corners, an embroidered
    running stitch around the paper, and a soft plum shadow underneath. 32x34: the shadow takes
    the bottom 2px, so it's 9-sliced at 10px (12px at the bottom; everything but the paper is in
    the border). `stars` adds a four-point star on each top corner (the main cards only)."""
    c = Smooth(32, 34)
    c.fill(c.rounded(0.5, 2, 31.5, 34, 8), (52, 28, 54, 70))           # the shadow, 2px down
    c.fill(c.rounded(0, 0, 32, 32, 8), PLUM)
    frame = c.rounded(1.5, 1.5, 30.5, 30.5, 6.5)
    c.gradient(frame, HONEY_HI, HONEY_SH, 2, 30)
    c.fill(minus(frame, c.shift(frame, 0, 1)), (255, 244, 200, 200))  # a lit top edge
    c.fill(c.rounded(4.5, 4.5, 27.5, 27.5, 4), HONEY_SH)                # a fine line around the paper
    paper = c.rounded(5.5, 5.5, 26.5, 26.5, 3)
    c.fill(paper, CREAM)
    c.fill(minus(paper, c.shift(paper, 1.2, 1.2)), CREAM_SH)          # the paper sits a little sunk in
    # A fine embroidered thread 2px in from the paper's edge. Continuous rather than dashed, so it
    # survives 9-slicing; the dashed running stitch is StitchRule.png, which tiles instead.
    thread = c.rounded(7.2, 7.2, 24.8, 24.8, 1.6)
    c.fill(minus(thread, c.rounded(7.9, 7.9, 24.1, 24.1, 1.0)), (222, 186, 128, 255))
    if stars:
        for x, y in ((4.4, 4.4), (27.6, 4.4)):
            c.fill(c.poly(star_points(x, y, 3.6, 1.1, 4), grow=0.9), PLUM)
            c.gradient(c.poly(star_points(x, y, 3.6, 1.1, 4)), BUTTER_HI, HONEY_HI, y - 3, y + 3)
    else:
        for x, y in ((4.4, 4.4), (27.6, 4.4), (4.4, 27.6), (27.6, 27.6)):  # small studs where the frame curves
            c.fill(c.ellipse(x, y, 1.0, 1.0), PLUM)
            c.fill(c.ellipse(x - 0.3, y - 0.3, 0.35, 0.35), HONEY_HI)
    return c.done()


def stitch_rule():
    """One stitch of the running-stitch divider: 6x2 on screen (a 4px dash, a 2px gap). USS tiles it
    along a row with background-repeat, so it's the same at any width."""
    c = Smooth(6, 2)
    c.fill(c.rounded(0, 0.4, 4, 1.6, 0.6), (210, 160, 92, 255))
    return c.done()


def star_burst():
    """A four-point star with a soft glow, flashed over the spell slot as the spell goes off."""
    c = Smooth(32, 32)
    c.fill(c.ellipse(16, 16, 9, 9), (255, 246, 200, 90))
    c.fill(c.poly(star_points(16, 16, 15, 3.6, 4), grow=1.2), PLUM)
    c.gradient(c.poly(star_points(16, 16, 15, 3.6, 4)), WHITE, BUTTER_HI, 2, 30)
    c.fill(c.ellipse(16, 16, 2.6, 2.6), WHITE)
    return c.done()


def minimap_ring():
    """The minimap's round frame, laid over the map: a plum-outlined honey ring with the running
    stitch, a soft shadow, and a four-point star at the top for "this way is up the screen".
    140x140 on screen; the map shows through the 120px hole."""
    c = Smooth(140, 140)
    cx = cy = 69
    ring = minus(c.ellipse(cx, cy, 67, 67), c.ellipse(cx, cy, 60, 60))
    c.fill(minus(c.ellipse(cx, cy + 2, 68, 68), c.ellipse(cx, cy, 60, 60)), (52, 28, 54, 70))  # shadow
    c.fill(minus(c.ellipse(cx, cy, 68.5, 68.5), c.ellipse(cx, cy, 58.8, 58.8)), PLUM)
    c.gradient(ring, HONEY_HI, HONEY_SH, 2, 136)
    c.fill(minus(c.ellipse(cx, cy, 61.4, 61.4), c.ellipse(cx, cy, 60, 60)), HONEY_SH)  # inner line
    for i in range(48):  # the stitch, every 7.5 degrees
        a = math.radians(i * 7.5 + 3.75)
        x, y = cx + 64 * math.cos(a), cy + 64 * math.sin(a)
        c.fill(c.ellipse(x, y, 0.8, 0.8), (255, 240, 196, 220))
    top = star_points(cx, 3.5, 7, 2.2, 4)
    c.fill(c.poly(top, grow=1.4), PLUM)
    c.gradient(c.poly(top), WHITE, BUTTER_HI, -3, 10)
    return c.done()


def heart_points(x0, y0, w, h, n=240):
    """The classic heart curve, fitted into a box."""
    raw = []
    for i in range(n):
        t = 2 * math.pi * i / n
        raw.append((16 * math.sin(t) ** 3,
                    -(13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t))))
    xs, ys = [p[0] for p in raw], [p[1] for p in raw]
    sx, sy = w / (max(xs) - min(xs)), h / (max(ys) - min(ys))
    return [(x0 + (x - min(xs)) * sx, y0 + (y - min(ys)) * sy) for x, y in raw]


def heart(kind):
    """kind: "full", "empty" or "half" (the left half full). 30x28 with a 2px plum outline."""
    c = Smooth(30, 28)
    shape = heart_points(3, 3, 24, 22)
    body = c.poly(shape)
    c.fill(c.poly(shape, grow=2), PLUM)

    # Empty: pale and a little sunken (shadow along the top-left inside edge).
    c.fill(body, (236, 222, 196, 255))
    c.fill(minus(body, c.shift(body, 1.4, 1.6)), (212, 192, 166, 255))
    if kind == "empty":
        return c.done()

    full = body if kind == "full" else both(body, c.rounded(0, 0, 15, 28, 0))
    c.gradient(full, (255, 128, 146, 255), (212, 44, 76, 255), 4, 24)
    c.fill(both(full, minus(body, c.shift(body, -1.6, -1.8))), HEART_SH)    # shade along the bottom right
    c.fill(both(full, c.ellipse(9.5, 8.6, 3.4, 2.3)), (255, 232, 236, 220))  # a glossy highlight
    c.fill(both(full, c.ellipse(6.6, 12.2, 1.0, 1.0)), (255, 232, 236, 200))
    if kind == "half":
        c.fill(both(body, c.rounded(14.4, 0, 15.6, 28, 0)), SOFT_PLUM)        # where it broke
    return c.done()


def magic_icon():
    """A four-pointed turquoise sparkle with a little one beside it. 24x24."""
    c = Smooth(24, 24)
    big = star_points(10.5, 11, 9.5, 3.2, 4)
    c.fill(c.poly(big, grow=1.5), PLUM)
    star = c.poly(big)
    c.gradient(star, MAGIC_HI, MAGIC, 3, 16)
    c.fill(both(star, c.ellipse(10.5, 11, 2.3, 2.3)), WHITE)
    small = star_points(19.5, 19, 4, 1.4, 4)
    c.fill(c.poly(small, grow=1.2), PLUM)
    c.fill(c.poly(small), MAGIC_HI)
    return c.done()


def coin_icon():
    """A gold coin with a stamped rim and a shine. 18x18."""
    c = Smooth(18, 18)
    c.fill(c.ellipse(9, 9, 8.2, 8.2), PLUM)
    face = c.ellipse(9, 9, 6.8, 6.8)
    c.gradient(face, GOLD_HI, GOLD_SH, 3, 16)
    c.fill(minus(c.ellipse(9, 9, 5.2, 5.2), c.ellipse(9, 9, 4.2, 4.2)), GOLD_SH)  # the stamped rim
    c.fill(c.rounded(8, 5.5, 10, 12.5, 1), GOLD_SH)                                # a bar across the middle
    c.fill(c.ellipse(6, 5.8, 1.5, 1.1), (255, 252, 230, 230))
    return c.done()


MONSTER_GREEN = ((150, 226, 140, 255), (84, 170, 92, 255), (214, 250, 200, 255))
MONSTER_GREY = ((176, 166, 180, 255), (124, 114, 128, 255), (220, 214, 224, 255))


def slime_icon(size, colors):
    """A little slime face (top-left "monsters left" icon, and the grey progress pips)."""
    light, dark, shine = colors
    c = Smooth(size, size)
    u = size / 24  # drawn at 24x24, scaled to fit
    # A dome with a flat, slightly wavy bottom.
    dome = [(12 * u + 10 * u * math.cos(math.radians(a)), 14 * u - 10 * u * math.sin(math.radians(a))) for a in range(0, 181, 3)]
    bottom = [(2 * u + 20 * u * i / 8, 20 * u + (0.8 * u if i % 2 else 0)) for i in range(9)]
    shape = dome + [(2 * u, 20 * u)] + bottom[1:-1] + [(22 * u, 20 * u)]
    c.fill(c.poly(shape, grow=1.6 * u), PLUM)
    body = c.poly(shape)
    c.gradient(body, light, dark, 5 * u, 20 * u)
    c.fill(c.ellipse(7.5 * u, 8 * u, 2.6 * u, 1.7 * u), shine)
    for x in (8.5 * u, 15.5 * u):  # eyes, with a glint
        c.fill(c.ellipse(x, 12.5 * u, 1.6 * u, 2.2 * u), PLUM)
        c.fill(c.ellipse(x - 0.5 * u, 11.7 * u, 0.6 * u, 0.6 * u), WHITE)
    smile = [(10 * u + 4 * u * i / 10, 16.2 * u + 1.2 * u * math.sin(math.pi * i / 10)) for i in range(11)]
    c.fill(c.poly(smile + [(14 * u, 16.2 * u)], grow=0.6 * u), PLUM)
    return c.done()


def pip_star():
    """A chubby four-point star (the signature motif): one monster defeated. 18x18."""
    c = Smooth(18, 18)
    pts = star_points(9, 9, 8.2, 3.2, 4)
    c.fill(c.poly(pts, grow=1.4), PLUM)
    star = c.poly(pts)
    c.gradient(star, (255, 236, 150, 255), HONEY, 3, 16)
    c.fill(c.ellipse(9, 9, 1.6, 1.6), (255, 255, 236, 230))
    return c.done()


def hurt_vignette():
    """Transparent in the middle, warm red toward the edges. Stretched over the whole screen,
    so it's small and soft (no detail to lose)."""
    w, h = 256, 144
    img = Image.new("RGBA", (w, h))
    px = img.load()
    for y in range(h):
        for x in range(w):
            dx, dy = (x + 0.5) / w * 2 - 1, (y + 0.5) / h * 2 - 1
            d = math.sqrt(dx * dx * 0.85 + dy * dy)  # a slightly wide oval
            t = min(1.0, max(0.0, (d - 0.75) / 0.5))  # clear over most of the screen
            px[x, y] = (214, 36, 64, round(170 * t * t * (3 - 2 * t)))
    return img


def app_icon():
    """A gold crown with a heart gem on a plum rounded square, on Apple's 1024 icon grid (the
    tile is 824px with a margin, so it lines up with other Mac icons). Drawn at 256 units."""
    c = Smooth(256, 256)
    c.fill(c.rounded(25, 28, 231, 234, 46), (40, 18, 38, 110))           # a soft drop shadow
    tile = c.rounded(25, 25, 231, 231, 46)
    c.gradient(tile, (150, 82, 150, 255), (62, 28, 66, 255), 25, 231)
    c.fill(minus(tile, c.shift(tile, 0, 3)), (255, 220, 250, 90))         # a lit top edge
    c.fill(minus(c.rounded(33, 33, 223, 223, 39), c.rounded(37, 37, 219, 219, 35)), HONEY)

    crown = [(76, 160), (68, 96), (104, 126), (128, 72), (152, 126), (188, 96), (180, 160)]
    band = c.rounded(70, 152, 186, 182, 6)
    c.fill(c.poly(crown, grow=5), PLUM)
    c.fill(c.rounded(65, 147, 191, 187, 10), PLUM)
    for x, y in ((68, 96), (128, 72), (188, 96)):
        c.fill(c.ellipse(x, y, 13, 13), PLUM)
    body = c.poly(crown)
    c.gradient(body, GOLD_HI, GOLD, 70, 160)
    c.fill(minus(body, c.shift(body, -3, -3)), GOLD_SH)                   # shade along the right
    c.gradient(band, GOLD, GOLD_SH, 152, 184)
    c.fill(minus(band, c.shift(band, 0, 3)), GOLD_HI)
    for x, y in ((68, 96), (128, 72), (188, 96)):
        c.gradient(c.ellipse(x, y, 8, 8), GOLD_HI, GOLD, y - 8, y + 8)
        c.fill(c.ellipse(x - 2.5, y - 2.5, 2.5, 2.5), WHITE)

    gem = heart_points(114, 134, 28, 25)
    c.fill(c.poly(heart_points(109, 129.5, 38, 34)), PLUM)  # (a bigger heart: grow= frays on tiny curves)
    c.gradient(c.poly(gem), (255, 128, 146, 255), (212, 44, 76, 255), 136, 158)
    c.fill(c.ellipse(122, 141, 3.5, 2.5), (255, 232, 236, 220))
    for x in (90, 166):
        c.fill(c.ellipse(x, 167, 6.5, 6.5), PLUM)
        c.fill(c.ellipse(x, 167, 4.5, 4.5), MAGIC)
        c.fill(c.ellipse(x - 1.2, 165.8, 1.6, 1.6), MAGIC_HI)
    return c.done()


def pad(img, by=1):
    """A transparent border, so outline() has room to draw around the edges."""
    out = Image.new("RGBA", (img.width + 2 * by, img.height + 2 * by), CLEAR)
    out.alpha_composite(img, (by, by))
    return out


def map_markers():
    green, grey, red, purple = (120, 230, 140, 255), (176, 166, 186, 255), (240, 70, 60, 255), (200, 140, 255, 255)
    return {
        # The hero: a bold crown with three clear points, outlined so it stands out anywhere.
        "MapCrown": outline(pad(pattern([
            "Y...Y...Y",
            "YY.YYY.YY",
            "YYYYYYYYY",
            "YRYYRYYRY",
            "YYYYYYYYY",
            "SSSSSSSSS",
        ], {"Y": HONEY_HI, "R": HEART, "S": HONEY_SH})), PLUM),
        "MapStairs": pattern(["....OOO", "...OGGO", "..OGGGO", ".OGGGGO", "OGGGGGO", "OOOOOOO"], {"O": PLUM, "G": green}),
        "MapStairsLocked": pattern(["....OOO", "...OGGO", "..OGGGO", ".OGGGGO", "OGGGGGO", "OOOOOOO"], {"O": PLUM, "G": grey}),
        "MapMonster": pattern([".OOO.", "ORRRO", "ORRRO", ".OOO."], {"O": PLUM, "R": red}),
        "MapChest": pattern(["OOOOO", "OYYYO", "OYYYO", "OOOOO"], {"O": PLUM, "Y": HONEY_HI}),
        "MapDragon": pattern(["..O..", ".OPO.", "OPPPO", ".OPO.", "..O.."], {"O": PLUM, "P": purple}),
    }


def ring_icon():
    c = Canvas()
    props.draw_ring(c, 12, 14, 8, 3, glint=True)
    return outline(c.img.crop((0, 0, 24, 24)))


def main():
    UI.mkdir(parents=True, exist_ok=True)
    gold_hi, gold_lo = (226, 186, 92, 255), (146, 102, 44, 255)
    stone_hi, stone_lo = (150, 146, 162, 255), (86, 82, 98, 255)
    frame(gold_hi, gold_lo, (24, 20, 32, 255)).save(UI / "Frame.png")
    frame(gold_hi, gold_lo, (16, 14, 22, 200)).save(UI / "Panel.png")
    frame(stone_hi, stone_lo, (20, 18, 26, 255)).save(UI / "Slot.png")
    portrait(wizard.draw_wizard(), 1, (36, 40, 74)).save(UI / "PortraitWizard.png")
    portrait(princess.draw_princess(), 1, (30, 70, 80)).save(UI / "PortraitPrincess.png")
    spell_icon(spell.FIRE, (96, 24, 16, 255)).save(UI / "IconFireball.png")
    spell_icon(spell.WATER, (16, 50, 90, 255)).save(UI / "IconTidalOrb.png")
    ring_icon().save(UI / "IconEmberRing.png")
    coin_icon().save(UI / "IconCoin.png")
    cream_panel().save(UI / "PanelCream.png")
    cream_panel(stars=True).save(UI / "PanelStar.png")
    star_burst().save(UI / "StarBurst.png")
    stitch_rule().save(UI / "StitchRule.png")
    minimap_ring().save(UI / "MinimapRing.png")
    heart("full").save(UI / "Heart.png")
    heart("half").save(UI / "HeartHalf.png")
    heart("empty").save(UI / "HeartEmpty.png")
    magic_icon().save(UI / "IconMagic.png")
    slime_icon(24, MONSTER_GREEN).save(UI / "IconMonster.png")
    slime_icon(18, MONSTER_GREY).save(UI / "PipMonster.png")
    pip_star().save(UI / "PipStar.png")
    hurt_vignette().save(UI / "HurtVignette.png")
    app_icon().save(ART / "AppIcon.png")
    for name, img in map_markers().items():
        img.save(UI / f"{name}.png")
    print("Wrote", ", ".join(sorted(p.name for p in UI.glob("*.png"))))


if __name__ == "__main__":
    main()
