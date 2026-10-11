# Boss telegraph audit

Audit of every boss attack for **warning time, danger area, safe space and recovery**, written alongside the Phase 1
feel-and-feedback work (roadmap C2). The numbers are read from the code (`BossAbilities`, `TideWaves`, `SnowballLanes`,
`ShellCycle`, `GolemCrystals`) and the builder files, not from playtests: they say what the game does, not that it is
fun or fair. `BossTelegraphTests` checks the rules in the last section.

There are **seven** bosses with special moves, not six: the roadmap's six plus the Snow Yeti (Frostpeak).

## What every warning now is

| Attack | Warning shape | Direction / area cue | Sound |
|---|---|---|---|
| Ground slam, cannon shell | ring of chunky plum-and-cream dashes round a striped disc (`Telegraph.Circle`), on top of the old red growing circle | the ring is the exact danger radius | `slam_windup` (slam), `incoming` whistle (each shell) |
| Volley | a fan on the floor with one solid lane per bolt and dashed edges (`Telegraph.Fan`) | the lanes are where the bolts fly; the gaps between them are safe. Follows the player, then **locks** for the last 0.35 s and the bolts fly down the locked lanes | `warn_charge` rising shimmer |
| Tide, snowball lanes | strip of arrowheads with plum rails along both edges (`Telegraph.Band`) | arrowheads point the way it will roll | `tide_warn` horn (tide), `warn_charge` (lanes) |

All of them: in the last 0.35 s the outline blinks fast and swells and a three-beep `warn_tick` plays ("now"). None of
this depends on colour: each has dark plum marks (read on snow) and light marks (read on water and shadow), and a
different silhouette from the others. Warnings are drawn with the default unlit sprite material, so the Golem's dark
phase (which dims the scene lights) does not dim them. The ring and the blink are drawn above ground spell effects;
the strips sit under characters so they never hide the hero.

## Per boss

Slam numbers are shared (`BossAbilities` defaults) unless the builder overrides them. Gentle Mode multiplies every
warning by 1.6 and halves damage.

| Boss (scene) | Moves | Warning before it lands | Danger area | Safe space | Recovery / gap |
|---|---|---|---|---|---|
| **Slime King** (Dungeon) | slam; splits at half health | slam 1.0 s windup + 0.7 s leap = **1.7 s** (Gentle 2.3 s) | circle r 3 m at where you stood when it began; 2 damage and a shove to the edge | anywhere outside r 3 m | none after landing (the chase resumes at once); slam cooldown 6 s |
| **Captain Grumblebeard** (Cove) | slam; cannon barrage; crew at half health | slam 1.7 s; each shell 1.3 s (Gentle 2.1 s), shells 0.4 s apart | slam r 3 m; 4 shells r 1.8 m, the first on you, the rest within 4 m of you; 1 damage each | outside the rings; keep moving | he keeps chasing while the barrage runs; slam cooldown 8 s, barrage 9 s |
| **Pumpkin King** (Farm) | slam; bolt volley; Gourdlings at half health | slam 1.7 s; volley **0.8 s** (Gentle 1.3 s), aim locks for the last 0.35 s | slam r 3 m; 5 bolts in a 60 deg fan, 7 m/s, 1 damage | gaps between the lanes; a sidestep after the lock | 0.4 s after the volley; slam 7 s, volley 8 s |
| **Mother Mushroom** (Woods4) | slam; spore volley; Spore Puffs at half health | slam 1.7 s; volley 0.8 s | slam r 3 m; 5 bolts in a 70 deg fan | gaps between the lanes (wider fan, wider gaps) | 0.4 s; slam 7 s, volley 8 s |
| **Crystal Golem** (Mines4) | slam; shard volley; Pebblins at half health; **glow window** | slam 1.7 s; volley 0.8 s | slam r 3 m; 5 bolts in a 60 deg fan | as above; dark phase lasts 5 s, so keep out of the dark | 0.4 s; slam 8 s, volley 9 s. **Vulnerable only while the crystals glow (6 s, then 5 s dark)** |
| **King Crabbington** (Lake4) | slam; tide; crew at half health; **shell window** | tide band 1.6 s (Gentle 2.6 s); slam 1.7 s | tide: a 4 m wide band across the whole court, 2 damage and a shove 4 m; slam r 3 m | outside the band (the band is centred on where you stood) | the tide runs while he hides (6 s), roughly once per hide; slam 7 s. **Vulnerable only while he peeks (6 s)** |
| **Snow Yeti** (Frost4) | slam; snowball lanes; Ice Slimes at half health | lanes 1.5 s (Gentle 2.4 s); slam 1.7 s | 3 lanes 2.2 m wide, the first under you, 2 damage and a shove 3 m; ice makes you slide | between the lanes | lanes cooldown 7 s, slam 8 s |

## Hit feedback on the two bosses with a weak spot

* **Landed** (his weak spot was open): gold starburst over him, `weakspot_hit` crack-and-sparkle, a small hop, the usual
  white flash and hurt sound.
* **Deflected** (shell closed or crystals dark): grey shield with a red slash, a double `deflect` "ting", a sideways
  shudder, **no** white flash, no hurt sound, and the HUD badge "Wait till he peeks out!" / "Wait for the glow!".
* While the window is open a gold star floats over his head; while closed, a shield (`VulnerabilityCue`).

## Rules the code enforces (and `BossTelegraphTests` checks)

1. Every slam, volley, cannon shell, tide and snowball lane shows a marker of its own kind before it lands.
2. No warning is shorter than `Telegraph.MinWarningSeconds` (0.8 s). The Pumpkin King, Mother Mushroom and Golem volleys
   were 0.7 s before; they are now 0.8 s.
3. A circle marker's radius equals the slam radius.
4. A volley's fan locks before the bolts leave, and the bolts follow the locked lanes (so a late sidestep works).
5. Gentle Mode only lengthens a warning.
6. A boss beaten mid-attack takes its warning with it.

## Still open (not done in Phase 1)

* Nobody has watched a child play these fights; the numbers above are an audit, not a verdict.
* The slam leaves **no recovery window** after landing: the boss chases straight away. Worth a playtest before
  adding one.
* Spikes, lava, knockback, enemy spawn fans and stacked damage (roadmap C2, "unavoidable hits") are not audited here.
* The Bubble Shield against hazards, and area abilities against walls and pots, are not covered here.
* The dark phase of the Golem is readable for the warnings; whether the *player* can see the arena is not tested.
