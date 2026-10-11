"""Synthesizes the sounds for failed actions, boss warnings and boss hit feedback.

Run:  python3 Tools/make_feedback_sounds.py
Out:  Assets/Resources/Feedback/*.wav   (in Resources so static code like ActionFeedback can load them by name)

Same retro building blocks as make_sounds.py (imported from it), but written to their own folder so
regenerating these never touches the other sounds or the music.

Each failed action has a sound of its own, deliberately different in pitch and rhythm so a child can learn them:
  fail_bag     two dull low thuds, "bonk bonk"        (the bag is full)
  fail_mana    a falling, sputtering fizzle           (out of magic)
  fail_tool    a hollow two-note "uh-oh", falling     (you need a tool for this)
  fail_locked  a quick metal rattle and a heavy clunk (a locked door)
  deflect      a bright double "ting"                 (a boss's shell or stone hide shrugged it off)
Boss feedback:
  weakspot_hit a crack with a rising sparkle          (his weak spot was open: that hurt him)
  warn_charge  a rising shimmer                       (a volley or a lane of snowballs is charging)
  warn_tick    three sharp beeps                      (the danger spot is locking in)
  incoming     a falling whistle                      (a cannonball is on its way)
  tide_warn    a low swelling horn                    (the tide is gathering)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_sounds as ms  # noqa: E402
from make_sounds import envelope, highpass, lowpass, midi, mix, n, noise, place, tone  # noqa: E402

ms.OUT = Path(__file__).resolve().parent.parent / "Assets" / "Resources" / "Feedback"


def fail_bag():
    out = []
    for k, f in enumerate((150, 118)):
        thud = envelope(lowpass(tone(f, 0.13, "triangle", freq_end=f * 0.7), 500), attack=0.004, curve=2.2)
        place(out, thud, k * 0.14)
        place(out, [x * 0.3 for x in envelope(lowpass(noise(0.04, seed=300 + k), 900), curve=3)], k * 0.14)
    return out


def fail_mana():
    fizz = envelope(tone(720, 0.38, "sine", freq_end=170, vibrato=0.12), attack=0.01, curve=1.6)
    spit = envelope(highpass(noise(0.34, seed=310), 3500), attack=0.01, curve=2.0)
    spit = [x * (1.0 if int(i / 700) % 2 == 0 else 0.2) for i, x in enumerate(spit)]   # sputters on and off
    return mix([x * 0.7 for x in fizz], [x * 0.35 for x in spit])


def fail_tool():
    out = []
    place(out, [x * 0.6 for x in envelope(lowpass(tone(midi(n("G4")), 0.13, "square"), 1400), curve=2)], 0.0)
    place(out, [x * 0.6 for x in envelope(lowpass(tone(midi(n("D#4")), 0.24, "square", freq_end=midi(n("D4"))), 1200), curve=1.8)], 0.15)
    return out


def fail_locked():
    out = []
    for k in range(4):
        place(out, [x * 0.5 for x in envelope(tone(1900 + 120 * (k % 2), 0.035, "square"), curve=2.5)], k * 0.055)
        place(out, [x * 0.5 for x in envelope(highpass(noise(0.03, seed=320 + k), 2500), curve=2.5)], k * 0.055)
    clunk = envelope(lowpass(tone(95, 0.22, "square", freq_end=60), 420), attack=0.003, curve=2.0)
    return place(out, [x * 1.2 for x in clunk], 0.25)


def deflect():
    out = []
    for k, (f1, f2) in enumerate(((2800, 3770), (3300, 4400))):
        ring = mix(envelope(tone(f1, 0.22, "sine"), attack=0.001, curve=2.4),
                   [x * 0.5 for x in envelope(tone(f2, 0.16, "sine"), attack=0.001, curve=2.8)])
        place(out, ring, k * 0.11)
        place(out, [x * 0.4 for x in envelope(highpass(noise(0.02, seed=330 + k), 4000), curve=3)], k * 0.11)
    return out


def weakspot_hit():
    crack = envelope(highpass(noise(0.09, seed=340), 900), attack=0.002, curve=3)
    pop = envelope(lowpass(tone(240, 0.12, "triangle", freq_end=120), 700), curve=2.5)
    out = mix(crack, [x * 0.8 for x in pop])
    for k, note in enumerate(("C6", "E6", "G6", "C7")):
        place(out, [x * 0.4 for x in envelope(tone(midi(n(note)), 0.2, "triangle"), curve=2.2)], 0.05 + k * 0.05)
    return out


def warn_charge():
    shimmer = envelope(tone(300, 0.6, "triangle", freq_end=950, vibrato=0.05), attack=0.05, release=0.55, curve=0.8)
    buzz = envelope(lowpass(tone(150, 0.6, "saw", freq_end=475), 900), attack=0.05, release=0.55, curve=0.9)
    return mix([x * 0.6 for x in shimmer], [x * 0.3 for x in buzz])


def warn_tick():
    out = []
    for k in range(3):
        place(out, [x * 0.55 for x in envelope(tone(1100, 0.045, "square"), attack=0.002, curve=1.4)], k * 0.1)
    return out


def incoming():
    whistle = envelope(lowpass(tone(1900, 0.55, "sine", freq_end=520, vibrato=0.01), 3500), attack=0.04, curve=1.2)
    air = envelope(highpass(lowpass(noise(0.5, seed=350), 5000, 2500), 1500), attack=0.05, curve=1.5)
    return mix([x * 0.55 for x in whistle], [x * 0.18 for x in air])


def tide_warn():
    horn = envelope(lowpass(tone(110, 0.8, "triangle", freq_end=165, vibrato=0.01), 700), attack=0.25, release=0.5, curve=1.2)
    swell = envelope(lowpass(noise(0.8, seed=360), 1400, 400), attack=0.3, release=0.45, curve=1.3)
    return mix([x * 0.7 for x in horn], [x * 0.35 for x in swell])


SOUNDS = {
    "fail_bag": fail_bag, "fail_mana": fail_mana, "fail_tool": fail_tool, "fail_locked": fail_locked,
    "deflect": deflect, "weakspot_hit": weakspot_hit, "warn_charge": warn_charge, "warn_tick": warn_tick,
    "incoming": incoming, "tide_warn": tide_warn,
}

if __name__ == "__main__":
    for name, make in SOUNDS.items():
        ms.save(name, make())
        print(f"{name:14} {len(make()) / ms.RATE:4.2f}s")
