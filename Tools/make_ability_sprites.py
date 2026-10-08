"""Generates the art for the heroes' unlockable abilities (skill tree steps 3 and 4).

Run:  Tools/.venv/bin/python Tools/make_ability_sprites.py
Out:  Assets/Art/Meteor.png / .json      Impact  5 frames, once   a big fire burst (64px), where the meteor lands
      Assets/Art/Whirlpool.png / .json   Spin    4 frames, loop   a swirling pool, lying flat on the floor (64px)
      Assets/Art/Bubble.png / .json      Wobble  4 frames, loop   the princess's shield bubble (40px)
      Assets/Art/UI/IconFlameWave.png    24x24  hotbar icons, one per ability
      Assets/Art/UI/IconMeteor.png
      Assets/Art/UI/IconBubbleShield.png
      Assets/Art/UI/IconWhirlpool.png

Flame Wave needs no sheet of its own: it's a fan of the Fireball's impact bursts.
Uses the same "heat" painting as make_spell_sprites.py, so the effects match the spells.
"""
import math
import random

from PIL import Image

import make_spell_sprites as spell
from sprite_common import ART, CLEAR, outline, write_sheet

UI = ART / "UI"


def whirl_frame(i, size=64):
    """A spiral of water seen from above: three arms winding in to a bright eye."""
    rng = random.Random(300 + i)
    noise = {(x, y): rng.uniform(-0.1, 0.1) for x in range(size) for y in range(size)}
    c = (size - 1) / 2
    turn = i * math.pi / 6  # each frame spins the arms a little further

    def heat(x, y):
        d = math.dist((x, y), (c, c))
        r = d / (size / 2)
        if r > 1:
            return 0.0
        a = math.atan2(y - c, x - c)
        arms = 0.5 + 0.5 * math.cos(3 * (a - turn) + r * 7)  # the spiral
        rim = max(0.0, 1 - abs(r - 0.92) / 0.08) * 0.5
        eye = max(0.0, 1 - r / 0.18)
        base = max(arms * (1 - r) * 1.1 + 0.15 * (1 - r), rim, eye)
        return base + noise[x, y] if base > 0.08 else 0.0

    return spell.paint(heat, spell.WATER, size)


def bubble_frame(i, size=40):
    """A see-through bubble: a thin turquoise rim, a soft inner tint and a highlight."""
    img = Image.new("RGBA", (size, size), CLEAR)
    px = img.load()
    c = (size - 1) / 2
    wobble = 0.04 * math.sin(i * math.pi / 2)
    rx, ry = (size / 2 - 2) * (1 + wobble), (size / 2 - 2) * (1 - wobble)
    for y in range(size):
        for x in range(size):
            r = math.hypot((x - c) / rx, (y - c) / ry)
            if r > 1:
                continue
            if r > 0.9:
                px[x, y] = (150, 240, 232, 230)
            elif r > 0.8:
                px[x, y] = (64, 202, 198, 140)
            else:
                px[x, y] = (120, 220, 230, 46)
    # A curved highlight in the top-left, the classic "it's a bubble" glint.
    for t in range(14):
        a = math.radians(200 + t * 4)
        for k in (0.66, 0.7):
            x, y = round(c + rx * k * math.cos(a)), round(c + ry * k * math.sin(a))
            px[x, y] = (240, 255, 252, 240)
    return img


def icon_from(img, outline_color, size=24):
    img = img.crop(img.getbbox())
    img.thumbnail((size - 2, size - 2), Image.NEAREST)
    out = Image.new("RGBA", (size, size), CLEAR)
    out.alpha_composite(img, ((size - img.width) // 2, (size - img.height) // 2))
    return outline(out, outline_color)


def flame_wave_icon():
    """Three fireballs fanning out to the right."""
    img = Image.new("RGBA", (40, 40), CLEAR)
    for angle in (28, 0, -28):
        fly = spell.fly_frame(0, spell.FIRE).rotate(angle, resample=Image.NEAREST, center=(16, 16))
        img.alpha_composite(fly, (4, 4))
    return icon_from(img, spell.EMBER_OUTLINE)


def meteor_icon():
    """A big fireball diving down to the right."""
    fly = spell.fly_frame(1, spell.FIRE).rotate(-50, resample=Image.NEAREST, center=(16, 16))
    return icon_from(fly, spell.EMBER_OUTLINE)


def bubble_icon(size=24):
    """The bubble drawn small but bolder than the in-game one, so it reads on the cream slot:
    a deep-water rim, a light turquoise fill and a big glint."""
    img = Image.new("RGBA", (size, size), CLEAR)
    px = img.load()
    c, r = (size - 1) / 2, size / 2 - 2
    for y in range(size):
        for x in range(size):
            d = math.dist((x, y), (c, c)) / r
            if d <= 1:
                px[x, y] = (36, 140, 172, 255) if d > 0.82 else (150, 240, 232, 255) if d > 0.62 else (200, 248, 244, 255)
    for t in range(9):  # the glint, top-left
        a = math.radians(196 + t * 8)
        px[round(c + r * 0.7 * math.cos(a)), round(c + r * 0.7 * math.sin(a))] = (255, 255, 255, 255)
    return outline(img, (16, 50, 90, 255))


def main():
    write_sheet("Meteor", None, [("Impact", 14, False, [spell.impact_frame(i, spell.FIRE, spell.SMOKE, 64) for i in range(5)])],
                pivot="center", outline_color=None, frame_size=64)
    write_sheet("Whirlpool", None, [("Spin", 10, True, [whirl_frame(i) for i in range(4)])],
                pivot="center", outline_color=None, frame_size=64)
    write_sheet("Bubble", None, [("Wobble", 6, True, [bubble_frame(i) for i in range(4)])],
                pivot="center", outline_color=None, frame_size=40)

    water_outline = (16, 50, 90, 255)
    flame_wave_icon().save(UI / "IconFlameWave.png")
    meteor_icon().save(UI / "IconMeteor.png")
    bubble_icon().save(UI / "IconBubbleShield.png")
    icon_from(whirl_frame(0), water_outline).save(UI / "IconWhirlpool.png")
    print("Wrote the ability icons to", UI)


if __name__ == "__main__":
    main()
