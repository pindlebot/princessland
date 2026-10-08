"""The game's shared art direction: a pixel-art storybook diorama, a tiny floating fairy-tale
kingdom. Every generator in Tools/ takes its shared colours and sizes from here, and the
same values are mirrored for the game itself in Assets/Scripts/ArtStyle.cs (world) and the
:root variables at the top of Assets/UI/Hud.uss (interface). Change one, change the others.

The rules:
  - The environment is quiet (muted sage, warm sand, lavender-grey stone); characters,
    pickups and magic are louder (turquoise, coral pink, butter yellow). Calm, empty space
    is part of the look: no filling gaps with decoration.
  - One pixel size everywhere: art is drawn at 16 pixels per unit and placed at scale 1,
    never stretched. A bigger thing gets more pixels, not bigger ones.
  - Outlines and text are deep plum, never black. Shadows are plum too, lying flat on the
    ground, offset a little away from the one sun (see SHADOW_* below and ArtStyle.cs).
  - The interface is warm cream paper with a restrained honey-gold trim and plum ink.
  - The signature motif, used sparingly: four-point stars, an embroidered running stitch,
    and scalloped garden edges.
"""

PPU = 16  # pixels per Unity unit, for every sprite and texture

# ---------- Ink ----------
PLUM = (74, 37, 69)           # interface text, panel outlines
PLUM_DEEP = (52, 28, 54)      # sprite outlines: dark enough to read, softer than black
PLUM_SOFT = (128, 88, 118)    # secondary text

# ---------- Environment (quiet) ----------
SAGE = (126, 160, 104)        # grass, base
SAGE_COOL = (121, 155, 102)   # grass patches, a touch cooler (close: patches, not tiles)
SAGE_WARM = (131, 164, 104)   # grass patches, a touch warmer
SAGE_DARK = (92, 126, 82)     # tufts, hedge shade, the island's grassy lip
SAGE_DEEP = (70, 100, 70)     # hedge shadow, darkest leaf
SAGE_LIGHT = (160, 188, 124)  # leaf highlights

SAND = (214, 184, 136)        # paths
SAND_SHADE = (190, 158, 114)
PEBBLE = (176, 166, 170)      # the odd stone on a path
PEBBLE_SHADE = (138, 126, 136)

EARTH_TOP = (156, 112, 80)    # the island's exposed side, in three layers
EARTH_MID = (128, 90, 70)
EARTH_LOW = (104, 80, 84)     # fading toward lavender-grey rock at the bottom
ROCK = (118, 108, 128)

STONE = (166, 158, 180)       # lavender-grey stone (castle, fountain, walls)
STONE_SHADE = (126, 118, 142)
STONE_LIGHT = (200, 194, 212)

WATER = (112, 182, 206)       # the pond: calm, a little quieter than the turquoise magic
WATER_DEEP = (92, 160, 192)
WATER_GLINT = (214, 240, 244)

# Mermaid Cove: pale beach sand (a little lighter and cooler than the paths), the open sea a
# step deeper than the pond, sun-bleached driftwood planks, and the white of falling water.
BEACH = (228, 208, 164)
BEACH_SHADE = (208, 186, 142)
WET_SAND = (192, 174, 146)
SEA = (98, 170, 200)
SEA_DEEP = (80, 148, 186)
FOAM = (236, 246, 246)
PLANK = (170, 132, 96)
PLANK_SHADE = (132, 98, 78)
PLANK_LIGHT = (198, 164, 124)

SKY = (176, 212, 228)         # around the floating island
CLOUD = (250, 247, 252)
CLOUD_SHADE = (222, 220, 240)
CLOUD_EDGE = (198, 196, 224)  # a soft lavender rim instead of a dark outline

# ---------- Characters and magic (loud) ----------
TURQUOISE = (64, 206, 210)
TURQUOISE_LIGHT = (196, 250, 246)
CORAL = (240, 112, 128)
CORAL_LIGHT = (255, 186, 192)
BUTTER = (255, 222, 120)
BUTTER_LIGHT = (255, 246, 200)

# ---------- Interface ----------
CREAM = (248, 239, 216)
CREAM_SHADE = (232, 218, 188)
HONEY = (222, 168, 70)
HONEY_LIGHT = (250, 214, 120)
HONEY_SHADE = (164, 112, 40)

# ---------- Shadows ----------
SHADOW = PLUM_DEEP + (96,)     # flat blob shadows: plum, never black
SHADOW_EDGE = PLUM_DEEP + (56,)
# Sizes of the pixel shadows (diameters in pixels). AddShadow picks the nearest one, so a
# shadow is never scaled and its pixels match everything else.
SHADOW_SIZES = (12, 16, 20, 28, 36, 48, 56)


def rgba(rgb, a=255):
    return tuple(rgb[:3]) + (a,)
