"""Synthesizes every sound effect and both music loops as 16-bit mono WAV files.

Run:  Tools/.venv/bin/python Tools/make_sounds.py
Out:  Assets/Audio/*.wav

Retro "chiptune" style: a few basic waveforms (sine, square, triangle, saw, noise),
shaped by envelopes and simple filters. Uses only the Python standard library.
"""
import math
import random
import struct
import wave
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "Assets" / "Audio"
RATE = 22050  # samples per second: plenty for retro sounds, and small files

# ---------- Building blocks ----------
# A sound is just a list of floats between -1 and 1, one per sample.


def silence(seconds):
    return [0.0] * int(seconds * RATE)


def tone(freq, seconds, wave_type="sine", volume=1.0, duty=0.5, freq_end=None, vibrato=0.0):
    """One oscillator. freq_end makes the pitch slide; vibrato wobbles it."""
    n = int(seconds * RATE)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / n
        f = freq if freq_end is None else freq + (freq_end - freq) * t
        if vibrato:
            f *= 1 + vibrato * math.sin(2 * math.pi * 6 * i / RATE)
        phase = (phase + f / RATE) % 1.0
        if wave_type == "sine":
            v = math.sin(2 * math.pi * phase)
        elif wave_type == "square":
            v = 1.0 if phase < duty else -1.0
        elif wave_type == "triangle":
            v = 4 * abs(phase - 0.5) - 1
        else:  # saw
            v = 2 * phase - 1
        out.append(v * volume)
    return out


def noise(seconds, volume=1.0, seed=0):
    rng = random.Random(seed)
    return [rng.uniform(-1, 1) * volume for _ in range(int(seconds * RATE))]


def envelope(samples, attack=0.005, release=None, curve=2.0):
    """Fade in over `attack` seconds, then decay to silence by the end (or over `release`)."""
    n = len(samples)
    a = max(1, int(attack * RATE))
    r = n - a if release is None else int(release * RATE)
    out = []
    for i, s in enumerate(samples):
        if i < a:
            g = i / a
        else:
            g = max(0.0, 1 - (i - a) / max(1, r)) ** curve
        out.append(s * g)
    return out


def lowpass(samples, cutoff, cutoff_end=None):
    """One-pole low-pass filter: keeps the rumble, removes hiss. Cutoff can sweep."""
    out, y, n = [], 0.0, len(samples)
    for i, x in enumerate(samples):
        fc = cutoff if cutoff_end is None else cutoff + (cutoff_end - cutoff) * i / n
        alpha = 1 - math.exp(-2 * math.pi * fc / RATE)
        y += alpha * (x - y)
        out.append(y)
    return out


def highpass(samples, cutoff):
    low = lowpass(samples, cutoff)
    return [x - l for x, l in zip(samples, low)]


def mix(*tracks):
    n = max(len(t) for t in tracks)
    return [sum(t[i] for t in tracks if i < len(t)) for i in range(n)]


def place(track, samples, at_seconds):
    """Adds `samples` into `track` starting at a time, growing the track if needed."""
    start = int(at_seconds * RATE)
    if len(track) < start + len(samples):
        track.extend([0.0] * (start + len(samples) - len(track)))
    for i, s in enumerate(samples):
        track[start + i] += s
    return track


def midi(note):
    """MIDI note number to frequency: 69 = A4 = 440 Hz, +12 = one octave up."""
    return 440.0 * 2 ** ((note - 69) / 12)


NOTE = {n: i for i, n in enumerate(["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"])}


def n(name):
    """'C5' -> MIDI number. Lets the melodies below be written as note names."""
    return 12 * (int(name[-1]) + 1) + NOTE[name[:-1]]


def save(name, samples, peak=0.8):
    biggest = max(1e-9, max(abs(s) for s in samples))
    scale = peak / biggest  # normalize so every sound has a similar loudness
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / f"{name}.wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * scale)) * 32767)) for s in samples))


# ---------- Sound effects ----------

def cast_fire():  # a whoosh: noise swept through an opening filter, plus a low thump
    whoosh = envelope(lowpass(noise(0.4, seed=1), 600, 3500), attack=0.06, curve=1.5)
    thump = envelope(tone(220, 0.25, freq_end=90), curve=3)
    return mix(whoosh, [s * 0.6 for s in thump])


def cast_water():  # a rising, wobbling bubble
    bubble = envelope(tone(380, 0.35, freq_end=900, vibrato=0.08), attack=0.02, curve=1.6)
    sparkle = envelope(tone(1600, 0.12), curve=3)
    return place(list(bubble), [s * 0.4 for s in sparkle], 0.18)


def impact_fire():  # a crackling burst with a boom underneath
    crackle = envelope(lowpass(noise(0.45, seed=2), 2500, 400), curve=2.5)
    boom = envelope(tone(110, 0.35, freq_end=50), curve=2)
    return mix(crackle, [s * 0.8 for s in boom])


def impact_water():  # a splash, then droplets
    splash = envelope(highpass(noise(0.3, seed=3), 900), curve=2.5)
    out = list(splash)
    rng = random.Random(4)
    for k in range(4):
        drop = envelope(tone(rng.uniform(1100, 2200), 0.07), curve=2)
        place(out, [s * 0.35 for s in drop], 0.08 + k * 0.07)
    return out


def bone_clack(pitch=900, seed=5):
    click = envelope(tone(pitch, 0.05, "square", duty=0.3), curve=4)
    tick = envelope(highpass(noise(0.03, seed=seed), 2000), curve=3)
    return mix(click, tick)


def enemy_hit():  # two quick bony clacks
    return place(bone_clack(950), bone_clack(700, 6), 0.05)


def enemy_attack():  # a sword swish: noise swept downwards
    return envelope(highpass(lowpass(noise(0.22, seed=7), 4000, 900), 400), attack=0.04, curve=1.5)


def enemy_death():  # a collapsing pile of bones: clacks falling in pitch and speeding up
    out = []
    rng = random.Random(8)
    t = 0.0
    for k in range(7):
        place(out, [s * (1 - k * 0.1) for s in bone_clack(1000 - k * 90, 10 + k)], t)
        t += rng.uniform(0.05, 0.11)
    return out


def player_hurt():  # a low "oof" with a buzzy edge
    oof = envelope(tone(320, 0.25, "square", duty=0.25, freq_end=140), curve=2)
    thud = envelope(lowpass(noise(0.12, seed=9), 600), curve=3)
    return mix([s * 0.5 for s in oof], thud)


def player_death():  # a sad falling arpeggio
    out = []
    for k, note in enumerate(["E4", "C4", "A3"]):
        place(out, envelope(tone(midi(n(note)), 0.4, "triangle"), curve=1.5), k * 0.22)
    return out


def chest_open():  # a creaky hinge, then a sparkle
    creak = envelope(lowpass(tone(110, 0.32, "saw", freq_end=150, vibrato=0.15), 900), attack=0.03, curve=1.2)
    out = [s * 0.6 for s in creak]
    for k, note in enumerate(["C6", "E6", "G6", "C7"]):
        place(out, [s * 0.5 for s in envelope(tone(midi(n(note)), 0.25), curve=2)], 0.3 + k * 0.06)
    return out


def pickup():  # a bright two-note chime
    out = [s * 0.6 for s in envelope(tone(midi(n("E6")), 0.12), curve=2)]
    return place(out, envelope(tone(midi(n("B6")), 0.3), curve=2), 0.08)


def arpeggio(notes, step, length, wave_type="square", volume=0.5):
    out = []
    for k, note in enumerate(notes):
        place(out, [s * volume for s in envelope(tone(midi(n(note)), length, wave_type, duty=0.25), curve=1.5)], k * step)
    return out


def exit_open():  # "the way is open!" fanfare
    return arpeggio(["C5", "E5", "G5", "C6", "E6"], 0.08, 0.35)


def ui_select():
    return envelope(tone(880, 0.06, "square", duty=0.25), curve=2)


def ui_start():
    return place(list(envelope(tone(midi(n("A5")), 0.08, "square", duty=0.25), curve=2)),
                 envelope(tone(midi(n("E6")), 0.2, "square", duty=0.25), curve=2), 0.07)


def voice_dragon():  # a low, rounded blip, played every few letters as she "speaks"
    return envelope(lowpass(tone(170, 0.06, "square", duty=0.4, freq_end=140), 900), attack=0.004, curve=1.5)


def voice_hero():  # a lighter, higher blip for the hero's lines
    return envelope(tone(560, 0.04, "square", duty=0.25, freq_end=600), attack=0.003, curve=1.5)


def door_open():  # a creaking hinge and a wooden thunk
    creak = envelope(lowpass(tone(90, 0.45, "saw", freq_end=170, vibrato=0.2), 800), attack=0.05, curve=1.2)
    thunk = envelope(lowpass(noise(0.15, seed=21), 400), curve=3)
    out = [s * 0.6 for s in creak]
    return place(out, thunk, 0.42)


def flush():  # a gurgling swirl that drains away
    swirl = envelope(lowpass(noise(1.2, seed=22), 1800, 300), attack=0.05, curve=1.3)
    gurgle = envelope(tone(140, 1.0, "sine", freq_end=60, vibrato=0.3), attack=0.1, curve=1.5)
    return mix(swirl, [s * 0.4 for s in gurgle])


def water_run():  # a tap running for a moment
    return envelope(highpass(lowpass(noise(0.8, seed=23), 3000), 600), attack=0.06, curve=0.8)


def paper():  # a quick rip
    out = []
    for k in range(5):
        place(out, envelope(highpass(noise(0.035, seed=30 + k), 2500), curve=2), k * 0.03)
    return out


def rest():  # a gentle lullaby arpeggio
    return arpeggio(["F4", "A4", "C5", "F5", "A5"], 0.16, 0.6, "triangle", 0.5)


def coin():  # the classic two-note "ding-ding" pickup
    first = envelope(tone(midi(n("B5")), 0.06, "square", duty=0.5), curve=1.5)
    return place([s * 0.5 for s in first], [s * 0.5 for s in envelope(tone(midi(n("E6")), 0.22, "square", duty=0.5), curve=2)], 0.05)


def level_up():  # a bright rising fanfare with a held top note
    out = arpeggio(["C5", "E5", "G5", "C6"], 0.07, 0.2, "square")
    return place(out, [s * 0.6 for s in envelope(tone(midi(n("E6")), 0.7, "triangle", vibrato=0.01), curve=1.2)], 0.28)


def skill_learn():  # a magical shimmer
    return arpeggio(["G5", "D6", "G6", "B6"], 0.05, 0.3, "triangle", 0.5)


def slime_hit():  # a wet squelch
    squish = envelope(lowpass(noise(0.15, seed=40), 1200, 300), curve=2)
    wobble = envelope(tone(240, 0.15, "sine", freq_end=120, vibrato=0.25), curve=2)
    return mix(squish, [s * 0.6 for s in wobble])


def slime_attack():  # a springy "boing"
    return envelope(tone(180, 0.25, "sine", freq_end=520, vibrato=0.12), attack=0.01, curve=1.6)


def slime_death():  # a splat that drains away
    splat = envelope(lowpass(noise(0.35, seed=41), 2200, 200), curve=2.2)
    sag = envelope(tone(300, 0.4, "triangle", freq_end=60), curve=1.5)
    return mix(splat, [s * 0.5 for s in sag])


def boss_roar():  # a deep, wobbling bellow when the Slime King notices you
    growl = envelope(lowpass(tone(75, 1.1, "saw", freq_end=55, vibrato=0.08), 700), attack=0.08, curve=1.3)
    rumble = envelope(lowpass(noise(1.1, seed=50), 300), attack=0.1, curve=1.5)
    return mix(growl, [s * 0.6 for s in rumble])


def slam_windup():  # a rising rumble: something big is about to jump
    return envelope(lowpass(tone(60, 0.9, "saw", freq_end=160), 600, 1200), attack=0.6, release=0.3, curve=1.0)


def slam_land():  # a heavy boom with a wet splat on top
    boom = envelope(tone(70, 0.6, "sine", freq_end=35), curve=1.8)
    splat = envelope(lowpass(noise(0.5, seed=51), 2400, 300), curve=2)
    return mix(boom, [s * 0.8 for s in splat])


def summon():  # bubbly pops as slimelings split off
    out = []
    for k, pitch in enumerate((420, 520, 640)):
        place(out, envelope(tone(pitch, 0.12, "sine", freq_end=pitch * 1.8), curve=2), k * 0.11)
    return out


def stairs_open():  # a cheerful, bell-like chime with a shimmer on top
    out = []
    for k, note in enumerate(["C6", "E6", "G6", "C7"]):
        bell = mix(envelope(tone(midi(n(note)), 0.7, "sine"), curve=2.2),
                   [x * 0.3 for x in envelope(tone(midi(n(note)) * 2, 0.4, "sine"), curve=3)])
        place(out, [x * 0.5 for x in bell], k * 0.09)
    return out


def poof():  # a soft, friendly puff
    puff = envelope(lowpass(noise(0.25, seed=60), 1800, 500), attack=0.02, curve=1.8)
    twinkle = envelope(tone(midi(n("E6")), 0.15, "sine"), curve=2.5)
    return place([x * 0.7 for x in puff], [x * 0.35 for x in twinkle], 0.06)


def ribbit():  # Sir Hopsalot: two croaky, buzzy chirps
    croak = lambda f: envelope(lowpass(tone(f, 0.09, "square", duty=0.3, freq_end=f * 0.8, vibrato=0.15), 1400), curve=1.4)
    return place(list(croak(300)), croak(360), 0.13)


def plink():  # a coin dropping into the fountain: a high drip and a little splash
    drip = envelope(tone(1400, 0.12, "sine", freq_end=2200), curve=2.5)
    splash = envelope(lowpass(noise(0.15, seed=70), 3000), attack=0.003, curve=2)
    return place([x * 0.7 for x in drip], [x * 0.3 for x in splash], 0.05)


def wish():  # a wish coming true: a sparkly rising harp run
    out = []
    for k, note in enumerate(["C6", "E6", "G6", "B6", "D7", "G7"]):
        place(out, [x * 0.45 for x in envelope(tone(midi(n(note)), 0.5, "sine"), curve=2)], k * 0.06)
    return out


def voice_mermaid():  # a bubbly, bright blip with a wobble
    return envelope(tone(720, 0.05, "sine", freq_end=860, vibrato=0.3), attack=0.003, curve=1.5)


def voice_bonesy():  # a woody, rattly clack
    return envelope(highpass(tone(420, 0.045, "square", duty=0.2, freq_end=380), 300), attack=0.002, curve=2)


# ---------- The village ----------

def voice_badger():  # Barnaby Badger: a warm, gruff little hum, between the dragon's and the hero's
    return envelope(lowpass(tone(260, 0.06, "triangle", freq_end=230, vibrato=0.08), 1600), attack=0.004, curve=1.6)


def cluck():  # a hen's "buk-buk-BAWK": two short nasal clucks and a longer squawk that rises
    buk = lambda f, t: envelope(lowpass(tone(f, t, "square", duty=0.2, freq_end=f * 0.85), 2200), attack=0.003, curve=1.6)
    out = list(buk(520, 0.05))
    place(out, buk(500, 0.05), 0.09)
    place(out, envelope(lowpass(tone(640, 0.2, "square", duty=0.25, freq_end=900, vibrato=0.12), 2600),
                        attack=0.01, curve=1.3), 0.2)
    return [x * 0.6 for x in out]


def bell():  # the cathedral bell: a deep strike with its ringing overtones, fading slowly
    partials = [(1.0, 0.6), (2.0, 0.35), (2.4, 0.25), (3.0, 0.15), (4.2, 0.08)]
    out = mix(*[[x * volume for x in envelope(tone(196 * ratio, 2.2, "sine"), attack=0.004, curve=3)]
                for ratio, volume in partials])
    return place(out, [x * 0.2 for x in envelope(lowpass(noise(0.03, seed=91), 2500), curve=2)], 0)


def purchase():  # buying something: a cheerful coin "ding" and three bubbles popping upward
    out = list(coin())
    for k, f in enumerate((900, 1200, 1500)):
        place(out, [x * 0.35 for x in envelope(tone(f, 0.06, "sine", freq_end=f * 1.6), curve=2)], 0.18 + k * 0.07)
    return out


def door_locked():  # a rattle against a heavy lock
    out = []
    for k in range(3):
        place(out, [x * 0.6 for x in envelope(lowpass(noise(0.05, seed=80 + k), 2400), curve=2)], k * 0.07)
    return place(out, envelope(lowpass(tone(110, 0.15, "square"), 600), curve=2), 0.2)


def spikes():  # a quick metallic "shing" as a spike trap shoots up
    scrape = envelope(highpass(noise(0.14, seed=90), 3000), attack=0.004, curve=2.5)
    ring = mix(envelope(tone(2350, 0.3), curve=2.2), [x * 0.6 for x in envelope(tone(3170, 0.22), curve=2.6)])
    return mix([x * 0.7 for x in scrape], [x * 0.35 for x in ring])


def sizzle():  # stepping in lava: a hot hiss with a low pop
    hiss = envelope(highpass(lowpass(noise(0.45, seed=91), 7000, 2500), 1200), attack=0.01, curve=1.6)
    pop = envelope(tone(180, 0.12, freq_end=90), curve=3)
    return mix([x * 0.8 for x in hiss], [x * 0.5 for x in pop])


def victory():
    out = arpeggio(["C5", "E5", "G5"], 0.12, 0.25, "square")
    return place(out, [s * 0.6 for s in envelope(tone(midi(n("C6")), 1.0, "triangle"), curve=1.2)], 0.36)


def defeat():
    return arpeggio(["G4", "F#4", "F4", "E4"], 0.25, 0.4, "triangle", 0.6)


def pirate_hit():  # a gruff "oof!": a short, low buzzy grunt
    return envelope(lowpass(tone(170, 0.14, "saw", freq_end=120), 900), attack=0.005, curve=2)


def pirate_death():  # "arrr...": a falling, wobbling growl
    return envelope(lowpass(tone(220, 0.5, "saw", freq_end=90, vibrato=0.05), 1100), attack=0.01, curve=1.4)


def siren_cast():  # a dark, bubbly whoosh sinking in pitch
    bubble = envelope(tone(700, 0.3, freq_end=260, vibrato=0.12), attack=0.02, curve=1.6)
    hiss = envelope(lowpass(noise(0.3, seed=21), 1800, 500), curve=2)
    return mix(bubble, [s * 0.4 for s in hiss])


def siren_hit():  # a wet slap with a sharp splash
    return mix(envelope(highpass(noise(0.18, seed=22), 1200), curve=3),
               [s * 0.5 for s in envelope(tone(300, 0.12, freq_end=160), curve=3)])


def bolt_impact():  # a soft, inky splat
    return mix(envelope(lowpass(noise(0.25, seed=23), 1400, 300), curve=2.2),
               [s * 0.6 for s in envelope(tone(180, 0.2, freq_end=70), curve=2)])


def oars():  # climbing into the rowboat: a wooden knock, then two oar splashes
    knock = envelope(tone(260, 0.06, "triangle", freq_end=200), curve=3)
    out = list(knock)
    for k in range(2):
        place(out, [s * 0.5 for s in envelope(highpass(noise(0.22, seed=24 + k), 700), attack=0.03, curve=2)], 0.2 + k * 0.35)
    return out


# ---------- Music ----------
# Each song: tempo, a melody as (note or None for a rest, length in beats), and a bass
# line. Both parts add up to exactly the same number of beats, so the loop is seamless.

def render_part(notes, beat_seconds, wave_type, volume, duty=0.5, gap=0.9):
    out = []
    t = 0.0
    for note, beats in notes:
        length = beats * beat_seconds
        if note:
            # Each note is a little shorter than its slot (gap), so repeated notes are distinct.
            s = envelope(tone(midi(n(note)), length * gap, wave_type, duty=duty), attack=0.01, curve=0.7)
            place(out, [x * volume for x in s], t)
        t += length
    total = int(t * RATE)
    return out[:total] + [0.0] * max(0, total - len(out))


def hihats(beats, beat_seconds, volume=0.12):
    """A soft tick on every off-beat."""
    out = silence(beats * beat_seconds)
    tick = envelope(highpass(noise(0.04, seed=11), 5000), curve=3)
    for b in range(beats):
        place(out, [s * volume for s in tick], (b + 0.5) * beat_seconds)
    return out[: int(beats * beat_seconds * RATE)]


def castle_theme():
    """Cheerful C major, 120 bpm, 8 bars: C - Am - F - G, twice."""
    beat = 60 / 120
    melody = [
        ("E5", .5), ("G5", .5), ("C6", 1), ("B5", .5), ("G5", .5), ("E5", 1),   # C
        ("A4", .5), ("C5", .5), ("E5", 1), ("D5", .5), ("C5", .5), ("A4", 1),   # Am
        ("F4", .5), ("A4", .5), ("C5", 1), ("D5", .5), ("C5", .5), ("A4", 1),   # F
        ("G4", .5), ("B4", .5), ("D5", 1), ("B4", .5), ("D5", .5), ("G5", 1),   # G
        ("E5", 1), ("G5", .5), ("E5", .5), ("C5", 1), ("E5", 1),               # C
        ("C5", 1), ("E5", .5), ("C5", .5), ("A4", 2),                          # Am
        ("F4", .5), ("A4", .5), ("C5", .5), ("F5", .5), ("E5", 1), ("D5", 1),  # F
        ("D5", .5), ("E5", .5), ("D5", .5), ("B4", .5), ("G4", 2),             # G
    ]
    bass = []
    for root, fifth in [("C3", "G3"), ("A2", "E3"), ("F2", "C3"), ("G2", "D3")] * 2:
        bass += [(root, 1), (root, 1), (fifth, 1), (root, 1)]
    beats = 32
    return mix(render_part(melody, beat, "square", 0.32, duty=0.25),
               render_part(bass, beat, "triangle", 0.5),
               hihats(beats, beat))


def dungeon_theme():
    """Brooding A minor, 84 bpm, 8 bars: Am - F - Dm - E, with slow bass and dripping water."""
    beat = 60 / 84
    melody = [
        ("A4", 2), ("C5", 1), ("B4", 1),                    # Am
        ("A4", 2), ("F4", 2),                               # F
        ("D5", 1.5), ("C5", .5), ("A4", 1), ("F4", 1),      # Dm
        ("E4", 2), ("G#4", 2),                              # E
        ("A4", 1), ("B4", 1), ("C5", 1), ("E5", 1),         # Am
        ("F5", 2), ("E5", 1), ("C5", 1),                    # F
        ("D5", 1), ("C5", 1), ("A4", 1), ("F4", 1),         # Dm
        ("E4", 3), (None, 1),                               # E
    ]
    bass = [(r, 2) for r in ["A2", "E2", "F2", "C3", "D2", "A2", "E2", "B2"] * 2]
    beats = 32
    out = mix(render_part(melody, beat, "triangle", 0.45, gap=0.95),
              render_part(bass, beat, "sine", 0.6, gap=0.98))
    # Water drips in the dark: a high, soft blip now and then.
    rng = random.Random(12)
    for b in range(0, beats, 3):
        drip = envelope(tone(rng.uniform(1400, 1900), 0.08, freq_end=900), curve=3)
        place(out, [s * 0.15 for s in drip], (b + rng.uniform(0.2, 0.8)) * beat)
    return out[: int(beats * beat * RATE)]


def home_theme():
    """A cozy little waltz in F major (3/4 time), 100 bpm, 8 bars: F - C - Dm - Bb, F - C - Bb - C."""
    beat = 60 / 100
    melody = [
        ("A4", 1), ("C5", 1), ("F5", 1),       # F
        ("E5", 2), ("C5", 1),                  # C
        ("D5", 1), ("F5", 1), ("A5", 1),       # Dm
        ("G5", 2), ("F5", 1),                  # Bb
        ("A4", 1), ("C5", 1), ("F5", 1),       # F
        ("G5", 1), ("E5", 1), ("C5", 1),       # C
        ("D5", 1.5), ("C5", .5), ("A#4", 1),   # Bb
        ("C5", 3),                             # C
    ]
    # Waltz accompaniment: a low root on beat 1 ("oom"), a soft chord note on 2 and 3 ("pah pah").
    bass = []
    for root, chord in [("F2", "A3"), ("C3", "E3"), ("D3", "F3"), ("A#2", "D3"),
                        ("F2", "A3"), ("C3", "E3"), ("A#2", "D3"), ("C3", "E3")]:
        bass += [(root, 1), (chord, 1), (chord, 1)]
    return mix(render_part(melody, beat, "triangle", 0.45, gap=0.92),
               render_part(bass, beat, "sine", 0.45, gap=0.7))


def cove_theme():
    """A sea shanty in D minor, 6/8 (two swaying beats a bar, each split in three), 100 bpm,
    8 bars: Dm - C - Bb - A, twice, with waves washing in and out underneath."""
    beat = 60 / 100 / 3  # an eighth note: three to a swaying beat
    melody = [
        ("D5", 2), ("A4", 1), ("D5", 2), ("E5", 1),              # Dm
        ("F5", 2), ("E5", 1), ("D5", 2), ("C5", 1),              # C
        ("D5", 2), ("A#4", 1), ("A4", 2), ("G4", 1),             # Bb
        ("A4", 3), ("C#5", 2), ("E5", 1),                        # A
        ("F5", 2), ("E5", 1), ("D5", 2), ("F5", 1),              # Dm
        ("G5", 2), ("E5", 1), ("C5", 3),                         # C
        ("D5", 2), ("A#4", 1), ("G4", 2), ("A#4", 1),            # Bb
        ("A4", 3), ("D5", 3),                                    # A -> home
    ]
    bass = []
    for root, fifth in [("D3", "A3"), ("C3", "G3"), ("A#2", "F3"), ("A2", "E3")] * 2:
        bass += [(root, 2), (fifth, 1), (root, 2), (fifth, 1)]
    eighths = 48
    out = mix(render_part(melody, beat, "square", 0.3, duty=0.3, gap=0.85),
              render_part(bass, beat, "triangle", 0.5, gap=0.8))
    # Waves: filtered noise swelling in and out once every two bars.
    total = int(eighths * beat * RATE)
    surf = lowpass(noise(eighths * beat, seed=25), 700)
    period = 12 * beat * RATE
    for i in range(min(total, len(surf))):
        swell = 0.5 - 0.5 * math.cos(2 * math.pi * i / period)
        out[i] += surf[i] * 0.12 * swell
    return out[:total]


SOUNDS = {
    "ribbit": ribbit, "plink": plink, "wish": wish, "voice_mermaid": voice_mermaid,
    "voice_bonesy": voice_bonesy, "door_locked": door_locked,
    "cast_fire": cast_fire, "cast_water": cast_water,
    "impact_fire": impact_fire, "impact_water": impact_water,
    "enemy_hit": enemy_hit, "enemy_attack": enemy_attack, "enemy_death": enemy_death,
    "player_hurt": player_hurt, "player_death": player_death,
    "chest_open": chest_open, "pickup": pickup, "exit_open": exit_open,
    "ui_select": ui_select, "ui_start": ui_start, "victory": victory, "defeat": defeat,
    "voice_dragon": voice_dragon, "voice_hero": voice_hero,
    "door_open": door_open, "flush": flush, "water_run": water_run, "paper": paper, "rest": rest,
    "coin": coin, "level_up": level_up, "skill_learn": skill_learn,
    "slime_hit": slime_hit, "slime_attack": slime_attack, "slime_death": slime_death,
    "boss_roar": boss_roar, "slam_windup": slam_windup, "slam_land": slam_land, "summon": summon,
    "stairs_open": stairs_open, "poof": poof, "spikes": spikes, "sizzle": sizzle,
    "pirate_hit": pirate_hit, "pirate_death": pirate_death, "siren_cast": siren_cast, "siren_hit": siren_hit,
    "bolt_impact": bolt_impact, "oars": oars,
    "voice_badger": voice_badger, "cluck": cluck, "bell": bell, "purchase": purchase,
}

if __name__ == "__main__":
    for name, make in SOUNDS.items():
        save(name, make())
    save("music_castle", castle_theme(), peak=0.7)
    save("music_dungeon", dungeon_theme(), peak=0.7)
    save("music_home", home_theme(), peak=0.65)
    save("music_cove", cove_theme(), peak=0.65)
    for f in sorted(OUT.glob("*.wav")):
        with wave.open(str(f)) as w:
            print(f"{f.name:20} {w.getnframes() / w.getframerate():5.2f}s")
