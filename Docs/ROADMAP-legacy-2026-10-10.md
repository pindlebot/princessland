# Tidecrown Roadmap

The plan for growing Tidecrown from a three-scene demo into a small, complete metroidvania that a
5-year-old can play on her own, and that's still fun for a grown-up.

---

## 1. Where the game is today

| Area | State |
|---|---|
| **Scenes** | Character Select → Castle Grounds (Level 0) ↔ Home → Dungeon (Slime King) → win |
| **Heroes** | Aldric the Wizard (Fireball), Princess Marina (Tidal Orb). One spell each, auto-aim |
| **Enemies** | Skeleton, Slime, Slime King (Ground Slam + Royal Split) |
| **NPCs** | Amethyra the dragon (an intro and a "later" chat) |
| **Items** | Ember Ring (one Ring slot, 8-slot bag), chest, coins |
| **Progression** | XP/levels (level 2 at 400 XP), gold, one 4-step skill path per hero ending in two abilities each |
| **Systems** | `IInteractable` registry, `GameSession` static flags, typewriter dialogue, UI Toolkit HUD, minimap with fog of war, synthesized audio, Python sprite generators |
| **Pipeline** | Every scene is generated from ASCII maps in `Editor/DungeonBuilder.Levels.cs` |

### Gaps that block a bigger game
These go first because everything after depends on them:

1. **No git repository.** There's a `.gitignore` but no `.git`. Before a big expansion, `git init` and
   commit, so a bad Rebuild All Scenes or a refactor can always be undone.
2. **No saving.** Everything lives in static fields and is gone when the app quits. A metroidvania needs a
   save file, and a 5-year-old needs autosave (sessions end suddenly).
3. **The inventory doesn't survive scene changes.** `Inventory` is a component on the player prefab, so
   anything you pick up is lost at the next door. Today nobody notices because the ring is found in the last
   level. Once you can backtrack, this becomes a real bug.
4. **Gold has nothing to buy.**
5. **Maps live in C#, and the map legend is running out of characters.** Fine for 4 scenes; painful for ~30
   rooms with doors that need named destinations.
6. **Old Input Manager, keyboard and mouse only.** A gamepad is much easier for small hands than WASD + mouse.
7. **Losing means a Game Over screen and pressing R.** Too harsh for a 5-year-old (see §6, Gentle Mode).

---

## 2. Design pillars

1. **Readable without reading.** Icons, colors, sounds and pictures carry the meaning, and text is a
   bonus. If she can't work out what to do without help, the design is wrong.
2. **Gates you can see before you can open them.** The metroidvania rule: show the bramble wall, the dark
   cave or the river early, then hand out the tool that opens it later, so coming back pays off.
3. **Kindness over killing.** Enemies already *poof into stars*. Lean into that: the big bad gets cheered
   up rather than destroyed, and some "enemies" turn out to be friends.
4. **Every room hides something.** A coin under a bush, a cat, a joke. Easter eggs aren't a final phase;
   each content phase includes a few.
5. **Two heroes, both complete.** Every gate must be passable by both the Wizard and the Princess.

---

## 3. Lore

### The Kingdom of Gemhold
Gemhold is a kingdom of **floating islands** (that's why the castle grounds have earthy cliff edges)
joined by rainbow bridges. Long ago the **Prism Crown** held six gems, and each gem kept one island bright,
alive and connected to the others.

**Amethyra**, last of the amethyst dragons, was the crown's guardian. Three hundred years ago she
grew old and sleepy, and while she napped **the Grey Gloom** drifted in: a lonely storm cloud who didn't
like how cheerful everyone was. He blew the gems out of the crown and scattered them across the islands.
Each island lost its color, the rainbow bridges faded, and Gloom's helpers moved in: the Slime King in the
dungeon below, and the skeletons, who honestly just wanted somewhere to belong.

The Gloom also took Amethyra's **five dragon eggs** and hid them, one per island, which is the real reason
she never leaves the castle. She's been waiting.

### The heroes
- **Aldric the Wizard**, an apprentice with more enthusiasm than skill. Fire magic.
- **Princess Marina**, the aquamarine princess, whose family has guarded the crown's water gem for
  generations. Water magic.

Whichever hero you choose, the castle is your home (the Home scene already says "{hero}'s Home").

### The arc
| Act | Region | Gem | What's been lost |
|---|---|---|---|
| 1 | Castle Grounds & Dungeon | **Amethyst** (Slime King has it) | Dark crystals overrun the castle; Amethyra can't fly |
| 2 | Whispering Woods | **Emerald** | The trees fell asleep; the fairies are hiding |
| 3 | Glimmer Mines | **Topaz** | The lights went out; the moles are lost |
| 4 | Puddlebrook Lake | **Aquamarine** | The lake turned murky; the merfolk sank away |
| 5 | Frostpeak | **Sapphire** | It's been winter for 300 years |
| 6 | The Storm Spire | **Ruby** (the Gloom wears it) | The Gloom himself |

**Ending:** the Gloom isn't defeated, he's *cheered up*. With the crown restored, the heroes
invite him to a party in the castle garden, he rains gently on the flowers, and a rainbow appears.
Amethyra's eggs hatch and a baby dragon follows you around the post-game.

**Gems restore color as a visible reward.** Each region starts desaturated, and recovering its gem brings
the color back with a sweep across the screen. That gives her a progress meter she can see without reading
anything. (Implementation: a saturation value per theme in `SetUpLighting` plus a palette lerp on the
floor/wall materials, driven by a `gem:<name>` flag.)

---

## 4. World map and metroidvania structure

### Traversal abilities (the "keys")
Isometric top-down means no jumping, so gates are Zelda-style. Each ability opens one obstacle type on
the map. Both heroes get every ability; their spells only change how it looks.

| # | Ability | Where you get it | Opens | Map tile (proposed) |
|---|---|---|---|---|
| 1 | **Bouncy Boots** (made from the Slime King's jelly) | Dungeon boss | Small gaps and low fences: hop on **Space** | `~` gap, `f` fence |
| 2 | **Fairy Lantern** | Woods boss | Dark rooms; reveals hidden paths and ghost platforms | `d` dark area, `g` ghost tile |
| 3 | **Mole Mitts** | Mines boss | Push/pull big blocks; dig up soft dirt (secrets) | `o` block, `s` soft dirt |
| 4 | **Bubble Charm** | Lake boss | Swim through deep water | `w` water |
| 5 | **Rainbow Chalk** | Frostpeak boss | Draw rainbow bridges between rainbow posts | `r` post, `c` chasm |
| — | **Spell gates** (from the start) | — | Brambles: Fireball burns them, Tidal Orb makes them bloom and part. Braziers: fire lights them, water fills the basins. Either spell works, so neither hero gets stuck | `v` bramble, `z` brazier |

### Region graph
```
                        ┌──────────── Storm Spire (6) ────────────┐
                        │   needs Rainbow Chalk + all 5 gems      │
                        └──────────────────▲──────────────────────┘
                                           │ rainbow bridge
  Whispering Woods (2) ◀── gap ──  CASTLE GROUNDS (hub) ── chasm ──▶ Frostpeak (5)
     │  dark hollow                │      │      │                    ▲
     │  (Lantern) ─────────────┐   │    Home   Village                │ water route
     ▼                         │   ▼                                  │ (Bubble)
  Glimmer Mines (3) ◀──────────┘  Dungeon (1) ── water ──▶ Puddlebrook Lake (4)
     │  blocks (Mitts) ─── shortcut ──▶ Dungeon back room (secret)
```
- Each region is **3–5 rooms** (scenes) plus one boss room. That's ~25 scenes in total.
- Each region contains **gates for abilities you don't have yet** (backtracking payoffs: eggs, star shards,
  heart containers, shortcuts, jokes).
- **Fountains** (one per region, and there's already one in the castle grounds) are save points, full heals
  and fast-travel stations once you've touched them.
- **Shortcuts** open from the far side (one-way doors, levers), so walks back to the hub stay short.

### Backtracking payoff budget per region (rough)
- 1 dragon egg (needs that region's ability **or a later one**)
- 2–3 heart pieces (4 pieces = +1 max health)
- 5 star shards (collectible currency for cosmetics; see §5)
- 1 secret room
- 2+ easter eggs

---

## 5. Content catalog

### NPCs and quests
| NPC | Where | Role / quest | Reward |
|---|---|---|---|
| **Amethyra** (exists) | Castle Grounds | Mentor. Comments on each gem as you return it. **"The Lost Eggs"**: find all 5 | Baby dragon companion; she flies you to the Spire |
| **Barnaby Badger** | Village shop | Sells potions, hats, the world map, a bigger bag | — |
| **Pip the Baker** | Village | **"Berry Pie"**: bring 3 Glow Berries from the Woods | Pie (full heal), then a pie every visit |
| **Whiskers the Cat** | Hidden in every region | **"Where's Whiskers?"**: a cat who keeps wandering off. Find her in each region and she goes home | A cat follows you around the house; final reward is a cat hat |
| **Bonesy** | Dungeon | A friendly skeleton who doesn't want to fight. **"A Lonely Skeleton"**: introduce him to someone (Amethyra) | Skeletons in later areas wave instead of attacking (Gentle Mode only) or drop extra coins |
| **Old Moss** | Woods | Gardener. **"Wake the Trees"**: use your spell on 4 sleepy trees | Fairy Lantern hint + heart piece |
| **Queen Twinkle** | Woods | Fairy queen, tells the Gloom's story | Lore + the Woods gem's location |
| **Digby & the Mole Crew** | Mines | **"Lost Moles"**: find 3 lost moles in dark tunnels | Mine cart fast travel |
| **Captain Clamshell** | Lake | Retired crab sailor. **"Fishing Lesson"**: catch 3 fish | Fishing rod (mini-game) |
| **Mr. Frost** | Frostpeak | A snowman who's cold. **"A Scarf for Mr. Frost"**: trade chain (yarn from Village → knitting granny → scarf) | Snow Hat; he sings a song |
| **The Grey Gloom** | Storm Spire | Final "boss": a puzzle fight where you make him laugh (hit the joke bells) rather than hurt him | The Ruby, the ending |

### Bosses
Same pattern as `BossAbilities.cs`: a normal enemy plus a component with telegraphed attack coroutines.
Every attack is **telegraphed for at least a second** with a big red circle (the `SlamWarning` that exists).

| Boss | Region | Gimmick |
|---|---|---|
| Slime King (exists) | Dungeon | Slam, split. **Change:** drops the Amethyst + Bouncy Boots |
| Mother Mushroom | Woods | Puffs spore clouds you dodge; small mushrooms you bounce over |
| Crystal Golem | Mines | Only hurt when his crystals glow; lights go out between phases (Lantern) |
| King Crabbington | Lake | Hides in his shell; waves push you; hit him when he peeks out |
| The Snow Yeti | Frostpeak | Throws snowballs that roll across the ice; slippery floor |
| The Grey Gloom | Spire | Joke-bell puzzle + rain clouds to step out of |

### Regular enemies (one or two new per region)
Bats (swoop, Mines), Spore Puffs (stationary, Woods), Ghosts (only visible with the Lantern), Crabs
(sideways only, Lake), Ice Slimes (leave a slippery trail), Bouncing Slimes, and Gloomlings (tiny
clouds, Spire). Swap chasing for a **NavMeshAgent** (README exercise #4) before adding these, since
blocks and water make straight-line chasing look broken.

### Items
- **Equipment slots**: Ring (exists), **Hat**, **Charm**. Hats are partly cosmetic and *very*
  important to a 5-year-old: Party Hat, Crown (from the Slime King), Frog Hat, Snow Hat, Cat Hat,
  Wizard Hat (Princess) and Tiara (Wizard).
- **Consumables** on hotbar slots 2–5 (placeholders today): Healing Apple, Mana Berry, Pie (full heal),
  Bomb Flower (breaks cracked walls, an extra early gate type).
- **Key items** (separate "treasures" tab, can't be dropped): abilities, gems, eggs, keys, quest items.
- **Collectibles**: Heart Pieces (4 = +1 max health), Star Shards (spend at a wardrobe in the Home on
  cosmetics), plus a **Sticker Book**: every enemy type, NPC, region and secret you find earns a
  sticker. Pre-readers love this, and it doubles as a completion tracker.

### New mechanics
| Mechanic | Notes |
|---|---|
| **Hop** (Bouncy Boots) | A short scripted arc over a gap tile; no physics jumping |
| **Push blocks** | Grid-snapped, slide one tile per push, a "clunk" sound |
| **Pressure plates and levers** | Generic `Switch` + `Door` pair wired by an ID in the map file |
| **Swimming** | Water tiles become walkable with the Bubble Charm; spells disabled while swimming |
| **Ice** | Slippery floor (keep your momentum) in Frostpeak |
| **Darkness** | Rooms with no ambient light; the Lantern increases the player's torch radius and shows `g` tiles |
| **Breakables** | Pots and crates using `Health` + `Loot` (the comment in `Loot.cs` already suggests this) |
| **Shops** | A dialogue choice that opens a buy panel |
| **Fishing** (optional) | Timing mini-game at the Lake: press when the bobber dips |
| **Second spell per hero** | Wizard: Flame Wave (a short cone). Princess: Bubble Shield. Hotbar slot 2 |
| **Skill tree tiers 2–3** | Implement Twin Cast, Meteor, Mana Shield, Second Wind, Blink and Treasure Sense. Keep them *optional*: never gate progress behind a skill |
| **Companion** | The baby dragon (post-game) or Whiskers: follows you and picks up nearby coins |

---

## 6. Playing with a 5-year-old (and a spouse)

- **Gentle Mode** (default for her): no Game Over. At 0 hearts the hero "gets sleepy," the screen fades,
  and she wakes at the last fountain with everything kept. Enemies hit for half damage. Bosses' telegraphs
  last longer.
- **Adventurer Mode** for grown-ups: today's rules, plus faster enemies and fewer heals.
- **Gamepad first.** Migrate to the **Input System** package: left stick to move, **A** to talk, open or
  hop, **X** to cast, **Y** for item, **Start** for the map and menu. Keep keyboard and mouse working.
- **Pictures over words.** Quest log entries show a picture of the NPC + the item they want. Signs show
  icons. Arrows on the minimap point toward the current objective (optional, toggle in Gentle Mode).
- **Read-aloud** (stretch goal): macOS has built-in text-to-speech, and you could record your own voice
  for key lines, which would be its own easter egg.
- **Short sessions**: autosave on every door and fountain. "Continue" is the first button on the title screen.
- **Two-player "Helper" mode** (stretch goal, but probably the most fun one for your family): a second gamepad
  controls a **fairy sprite** that flies freely, collects coins, can stun enemies for a second and
  points at secrets. It's much simpler than full co-op (one camera, one HUD, one save) and it's great for a
  parent helping a kid, or a kid "helping" a parent. Full two-hero co-op is possible later but means
  reworking the camera, HUD and `GameSession` (which assume a single player).

---

## 7. Easter eggs

Personal ones (fill in the blanks):
- A statue in the village garden: **"Princess ____, Bravest in All of Gemhold."**
- Your family pet as an NPC who follows you around one room.
- A shop in the village named after your spouse, run by someone who looks suspiciously like them.
- **Birthday mode**: on her birthday (checked against the system date) every enemy wears a party hat and
  there's a cake in the Home. Also Halloween (pumpkin slimes) and December (snow on the castle grounds).
- **Dad's Workshop**: a secret room behind a fake wall in the Dungeon with a tiny desk, a computer
  showing a picture of the Unity editor, and a signed note from you.
- Credits with a family "cast list."

Game ones:
- **Wash your hands** already exists. Extend it: forget to wash and Amethyra says "...did you wash your hands?"
- **Flush the toilet 10 times**: a frog pops out and gives you the Frog Hat.
- **Sleep in the bed 3 times in a row**: the **Dream Level**, a short candy-colored room where the
  enemies are marshmallows. Wake up with a sticker.
- **Talk to Amethyra 10 times**: she tells a dragon joke. 25 times: she falls asleep and snores in
  the dialogue box.
- **Throw a coin in the fountain**: a random wish message; the 100th coin grants a heart piece.
- **Konami code** (↑↑↓↓←→←→BA) on the character select screen unlocks a secret hero: **Whiskers the
  Cat** (casts hairballs).
- **The 7-hit tree**: one tree in the castle grounds drops a golden apple after 7 spell hits.
- **Bonesy's xylophone**: hit his ribs with spells in the order of the castle music's first notes for a song.
- **Poke the sheep**: a field of sheep that follow you after you've talked to each one (a gentle cucco
  homage).
- **The mirror** in the Home shows the hero you *didn't* pick, waving.
- **Fake walls** that don't show on the minimap until you've walked through them.
- **A dance button** (hold Y): if you dance next to Amethyra, she dances too. Dance next to the Gloom in the
  final fight and he laughs (a secret alternate way to win).
- **The Slime King's Crown** as a wearable hat after you beat him.
- **The "you're not supposed to be here" room** off the edge of the map, reached by an out-of-bounds
  hop with the Bouncy Boots, containing a sign: "Hi! You found the edge of the world."

---

## 8. Technical plan

### 8.1 Save system
- A `SaveData` class (plain C#) holding: hero id, flags, `Progression` state, inventory (item ids),
  equipped items, current scene + spawn, visited rooms (for the world map), counters
  (e.g. `toilet_flushes`), settings (mode, volume).
- `JsonUtility` to `Application.persistentDataPath/save<N>.json`, with **3 slots** (one per family member)
  shown as big portraits on the title screen.
- Items need a stable id: add `[SerializeField] string id` to `ItemDefinition` and an `ItemDatabase`
  ScriptableObject that lists them all (built by `DungeonBuilder`).
- **Move the inventory's data into `GameSession`** (like `Progression`), so the component on the player
  becomes a view over session data. That fixes the lost-items bug and makes saving easy.
- Add **counters** next to flags: `GameSession.Counters["toilet_flushes"]++`. Many easter eggs need these.

### 8.2 Map files instead of C# arrays
Move maps into text files (`Assets/Levels/<Region>_<Room>.txt`) so adding a room means writing a text
file. A legend section maps digits and letters to anything, which solves the shortage of characters:

```
title: The Sleepy Glade
theme: Forest
music: music_woods
region: woods
---
HHHHHHH1HHHHHHH
H.....;;......H
H..v.....2....H
H..v..........3
HHHHHHHHHHHHHHH
---
1 = door Woods_Hollow FromGlade
2 = npc OldMoss
3 = door CastleGrounds FromWoods
```
`DungeonBuilder` parses these, generates one scene per file, auto-creates the named spawn points from
the `door` entries, and adds every scene to Build Settings. Also add a validator that checks every door's
target exists and has a matching return spawn, and run it from a Play Mode test.

### 8.3 Dialogue and NPCs
- Generalize `DragonNpc` into **`Npc`** (portrait, flipbook frames, voice clip) plus a **conversation
  list** where each conversation has a **condition** (`flag`, `!flag`, `counter>=n`, `has:item`) and
  **effects** (`set flag`, `give item`, `take item`, `start quest`, `open shop`). The first conversation
  whose condition is true plays. That one rule covers intros, "later" chats, quest steps and easter eggs.
- Add **choices** (2–3 icon buttons) to `DialogueController` for shops and yes/no questions.
- Keep the dialogue as data in the builder (like `DragonIntroduction`), or move it to text files next to
  the maps. Text files are easier to write in bulk.

### 8.4 Quests
`QuestDefinition` data: id, title, picture, steps (each step = a description + a completion
condition using the same condition language as dialogue). A quest log panel (**J**, or a tab on the
inventory) shows active quests with pictures. No separate quest state is needed: progress is derived from
flags and counters, so saving it is free.

### 8.5 Abilities and gates
- `GameSession.Abilities` (a set of ids), `PlayerAbilities` component with one method per ability.
- Gate components (`GapTile`, `Bramble`, `PushBlock`, `DarkZone`, `WaterTile`, `RainbowPost`) each
  check `GameSession.Abilities.Contains(...)`. When you don't have the ability yet, show a **hint bubble
  with the ability's icon and a "?"**, so she knows it's a "come back later" spot rather than a dead end.
- Spells hitting brambles and braziers: give `Projectile` an element (`Fire`/`Water`) and let it call an
  `ISpellTarget` interface on what it hits.

### 8.6 Rooms and the world map
- **Room edges**: walking off a marked edge loads the neighbor room (`SceneDoor` without the E press).
  Fade out and in over ~0.3 s to hide the load.
- **Enemy respawn policy**: regular enemies come back when you leave the region; bosses and "cleared"
  rooms with exits stay cleared. In Gentle Mode, cleared rooms stay cleared. (Today `ClearedFlag` removes
  enemies on every revisit, which suits Gentle Mode.)
- **World map screen** (**M**/Start; move mute to the options menu): reuse `Minimap`'s texture
  painting to draw every visited room at its world offset (add `worldX/worldY` to the map file
  header), with icons for fountains, uncollected eggs (once you've seen them) and the current objective.

### 8.7 Content pipeline (keeps the existing style)
- Each new enemy, NPC and boss gets a `Tools/make_<name>_sprites.py` using `sprite_common.py`, same as now.
- Each region gets a music track in `make_sounds.py` (Woods: flute-y 6/8; Mines: plucky pentatonic;
  Lake: slow arpeggios; Frost: celesta; Spire: minor that resolves to major at the end).
- Each region gets a `Theme` (floors, walls, lighting, saturation).
- **Tests**: keep extending the Play Mode tests per feature (there are 6 test files and ~47 tests
  now). Specifically: the save round-trip, every door's target exists, every gate is reachable *after*
  its ability is obtained (a simple flood fill over the map files with ability sets), and dialogue
  conditions.

---

## 9. Phases

Each phase ends with something playable. **Acceptance criterion for every phase: a playtest with her.**
Watch where she gets stuck without saying anything, and fix that before moving on.

### Phase 0: Foundations (no new content)
- [x] `git init`, first commit, and a `.gitattributes` for Unity YAML (`*.unity`, `*.prefab`, `*.asset` → `merge=unityyamlmerge`)
- [x] Move the inventory into `GameSession`; persist it across scenes (with a test)
- [x] Save/load (3 slots, autosave on every door), title screen with Continue / New / slot pictures
- [x] Input System + gamepad
- [x] Pause menu (resume, options, mode, quit to title)
- [x] Gentle Mode (wake at the fountain instead of Game Over)
- [x] Map text files + parser + door validator; port Level 0, Dungeon and House to the new format

### Phase 1: The systems content needs
- [ ] Generic `Npc` + condition/effect dialogue + choices; port Amethyra to it
- [x] Quest data + quest log with pictures
- [x] Counters, and the first easter eggs that use them (toilet frog, dragon jokes, washed hands)
- [x] Hat and Charm slots, consumables on quick slots (keys 2–5), key-item tab ("Treasures")
- [ ] The Village (2 rooms off the castle grounds): Barnaby's shop and Pip the Baker
  (started: Hollyhock is a fenced village beside the castle on Level 0, and Barnaby sells bubble bath at his stall;
  still to come: a buy panel with several wares, Pip the Baker)
- [x] Fountains as save points / fast travel
- [x] Breakable pots

### Phase 2: Metroidvania vertical slice (the most important phase)
Prove one full loop with **one** ability before building five regions:
- [x] Ability + gate framework, `ISpellTarget`, brambles/braziers
- [x] Slime King drops the Amethyst + Bouncy Boots; the crystal plague clears from the castle grounds
- [x] Gaps placed in the Castle Grounds and Dungeon *before* you have the boots
- [x] Bonesy in the Dungeon; Dad's Workshop secret room
- [x] Whispering Woods: 4 rooms, Spore Puffs, Old Moss, Mother Mushroom, Fairy Lantern
- [x] Room-edge transitions, world map screen, NavMesh enemies (a tile-grid pathfinder: the AI Navigation package doesn't compile on this Unity)
- [x] The first dragon egg, heart pieces and star shards
- [ ] **Checkpoint:** does she *remember* the gap and want to go back to it once she has the boots? If not,
  make the hint bubbles stronger before continuing.

### Phase 3: Regions 3–5
For each of Mines, Lake and Frostpeak: map files, theme, music, 1–2 enemies, 1–2 NPCs and quests, a boss,
an ability, an egg, heart pieces, 2+ easter eggs and a sticker set. Build them in this order, because each
ability unlocks backtracking in the regions before it.
- [x] Glimmer Mines (Mole Mitts, Crystal Golem, lost moles, a mine-cart fast-travel line) — 4 rooms (`Mines1-4`), Bats and Pebblins, Digby and three lost moles, the Topaz and its egg. Sticker set: waits for the Sticker Book (Phase 4)
- [x] Puddlebrook Lake (Bubble Charm, King Crabbington, fishing) — 4 rooms (`Lake1-4`), Crabs and Jellyfish, Captain Clamshell, swimming, the Aquamarine and its egg. Sticker set: waits for the Sticker Book (Phase 4)
- [ ] Frostpeak (Rainbow Chalk, Snow Yeti, ice, Mr. Frost's trade chain)

### Phase 4: Depth and collecting
- [x] Skill paths: one linear path per hero (2 enhancements, then 2 abilities)
- [x] Second (and third) spell per hero: Flame Wave + Meteor, Bubble Shield + Whirlpool
- [ ] Sticker Book screen
- [ ] Wardrobe in the Home (star shards → hats)
- [ ] Whiskers appearances in every region
- [ ] Dream Level

### Phase 5: The ending
- [ ] Storm Spire (rainbow bridges, Gloomlings, the Grey Gloom joke-bell fight + the dance secret)
- [ ] Ending sequence: color returns everywhere, the eggs hatch, the garden party
- [ ] Credits with your family's names
- [ ] Post-game: baby dragon companion, completion % on the save slot

### Phase 6: Polish and stretch goals
- [ ] Two-player Helper fairy
- [ ] Birthday/holiday modes
- [ ] Recorded voice lines for key moments
- [ ] Konami-code secret hero
- [ ] An iPad/tablet build with touch controls, if it turns out she'd rather play there

---

## 10. Suggested first steps
1. `git init` and commit what exists.
2. Fix the inventory persistence (small, and it unblocks everything).
3. Save system + Gentle Mode, then a playtest, so she can start playing "her" save slot right away while
   the rest gets built.
4. Map text files, because every later phase adds rooms.
5. Then the Phase 2 vertical slice. If the Bouncy Boots → gap → secret loop is fun, the rest of the plan is
   just more of it.
