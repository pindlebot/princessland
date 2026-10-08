# IsoDungeon

A tiny isometric dungeon crawler for learning Unity (6000.6.4f1, Built-in Render Pipeline).

**Play:** open in Unity Hub → open `Assets/Scenes/Title.unity` → press ▶.
(Pressing ▶ in a level scene directly also works; you play as the wizard.)

**Flow:** Title (3 save slots) → Character Select (for a new slot) → **Level 0** (the castle grounds) → **Level 1** (the dungeon).
Level 0's exit only appears once its 4 skeletons are defeated. Esc / Start opens the pause menu. M mutes the music.

**Saving:** three slots, each a JSON file in `Application.persistentDataPath` (`save1.json` ...). Every door, the
stairs and starting a new adventure autosave; the pause menu's "Save and go to title" saves at the door you came in by.
"Continue" (or just Enter / A) on the title screen picks up the most recent save.

**Modes** (pause menu, saved per slot): **Gentle** (the default) has no Game Over: monsters hit for half damage,
and at 0 hearts the hero naps and wakes by the last fountain she passed (or where she came in) with everything kept;
the boss's warning circle lasts 1.6× longer. **Adventurer** is the classic rules, with slightly faster monsters.
In Level 0, walk up to **Amethyra the dragon** and press **E** to talk (E / Space / Enter / click to continue).
**Coralie the mermaid** sits in the pond and has lost her frog. Easter eggs: one bush hides **Sir Hopsalot** (find him
and tell Coralie), the fountain takes wishes (1 coin each; the third comes true), and Amethyra tells jokes.
Press **E** at the **castle gate** to go inside to the hero's home (bed, toilet, sink, paper towel); the front
door brings you back out by the gate. Cleared levels and finished conversations are remembered between scenes.

**Progression:** enemies drop gold coins (walk near them) and give XP (skeleton 15, slime 20; the chest holds
25 gold). Levels need 40 × level^1.5 XP (40, 113, 208, 320, ...); each level gives +1 max health, +5 max mana
and a **skill point**. Press **K** for the skill tree: 3 branches × 3 tiers, each skill needs the one above it.
Tier 1 works (+1 spell damage / +2 max health / +15 max mana); tiers 2–3 are "coming soon" placeholders.
Level, XP, gold and skills carry across scenes and reset when you pick a hero.

**The dungeon** is a labyrinth: wooden doors (E to open), a storeroom of barrels and crates, **Bonesy** the friendly
skeleton at his campfire (warm up there for full health), a great puddle hall full of slimes, twisty tunnels hiding
the **Rusty Key**, and a locked treasure room it opens.

**Boss:** the dungeon's last room holds **the Slime King** (30 HP, hits for 2, worth 400 XP and 15–20 coins).
The crystal is sealed until he falls. *Ground Slam*: a red circle grows under you, then he leaps and lands
there: 2 damage, knockback and a screen shake if you're still inside, so step out of the circle. *Royal Split*:
at half health he splits off 3 slimelings. A boss health bar appears once he notices you.

| Hero | Spell | Health | Mana | Cast |
|---|---|---|---|---|
| Aldric the Wizard | Fireball | 5 | 50 (+8/s) | every 0.6s, 10 mana |
| Princess Marina (aquamarine) | Tidal Orb | 6 | 40 (+9/s) | every 0.45s, 8 mana |
| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Walk | WASD / arrows | left stick / d-pad |
| Magic (auto-aims) | Space / click | X / right trigger |
| Talk, open, pick up | E | A |
| Bag · skills | I · K | Y · View |
| Pause menu | Esc | Start |
| Help · music · restart | H · M · R | |

## Where things live
| Path | What it teaches |
|---|---|
| `Scripts/PlayerController.cs` | `Update`, `CharacterController`, camera-relative input, facing the walk direction |
| `Scripts/SpellAbility.cs` + `Projectile.cs` | Prefabs, `Instantiate`/`Destroy`, cooldowns, trigger colliders, auto-targeting (viewport + line-of-fire checks). One script, two spells |
| `Scripts/CharacterDefinition.cs`, `GameSession.cs` | Heroes as data assets; carrying a choice between scenes with a static |
| `Scripts/Progression.cs`, `SkillCatalog.cs` | Plain C# data that outlives scenes: XP curve, level-ups, gold, the skill tree's data |
| `Scripts/PlayerProgression.cs`, `Loot.cs`, `CoinPickup.cs` | Applying levels to the player; drops; static events (and unsubscribing in `OnDestroy`) |
| `Scripts/SkillTreeView.cs` | Building UI Toolkit elements from code instead of UXML |
| `Scripts/BossAbilities.cs` | A boss as "normal enemy + extra component"; telegraphed attacks as coroutine phases |
| `Scripts/SceneDoor.cs`, `GameSession.cs` | Doors between scenes, named arrival points, flags that survive scene loads |
| `Scripts/HouseFixture.cs` | One data-driven component for several simple interactables (an enum picks the effect) |
| `Scripts/DialogueController.cs`, `Npc.cs` | Conversations: typewriter text, pausing with `Time.timeScale`, unscaled time, serialized structs |
| `Scripts/AudioManager.cs` | Music loop + a pool of `AudioSource`s for overlapping effects, random pitch variation |
| `Scripts/LevelBootstrap.cs` | Spawning the chosen hero and wiring scene objects to it at runtime; `DefaultExecutionOrder` |
| `Scripts/CharacterSelectController.cs` | A menu scene in UI Toolkit; `SceneManager.LoadScene` |
| `Scripts/GameInput.cs` | Every button in one place: the Input System's keyboard, mouse and gamepad devices |
| `Scripts/SaveSystem.cs`, `SaveData.cs`, `TitleController.cs` | Save slots with `JsonUtility`, safe file writes, a title screen |
| `Scripts/PauseMenu.cs`, `GameSettings.cs` | A menu that stops time (`Time.timeScale = 0`) and works with mouse, keys and pad |
| `Scripts/ItemDefinition.cs` | ScriptableObjects: items as data assets (`Create > Dungeon > Item`) |
| `Scripts/Inventory.cs`, `ItemPickup.cs` | A bag + equipment slots; pickups reuse the `IInteractable` system |
| `Scripts/EnemyAI.cs` | A small state machine, `Physics.Linecast` line-of-sight, gizmos |
| `Scripts/Health.cs` | Reusable components + C# events (`Damaged`, `Died`) |
| `Scripts/IsoCameraFollow.cs` | Orthographic iso camera, `LateUpdate`, `SmoothDamp` |
| `Scripts/GameManager.cs` | Singleton, win/lose state, scene reload |
| `Scripts/Mana.cs` | A regenerating resource that abilities spend |
| `UI/Hud.uxml`, `UI/Hud.uss` | UI Toolkit layout (HTML-like) and styles (CSS-like), 9-sliced pixel frames |
| `Scripts/HudController.cs` | Binding game state to UI elements by name and toggling USS classes |
| `Scripts/Minimap.cs` | Drawing into a `Texture2D` at runtime, fog of war, rotating UI with `transform-origin` |
| `Scripts/LevelMap.cs` | Keeping level data in the scene for runtime systems |
| `Scripts/IInteractable.cs`, `PlayerInteractor.cs` | An interface + registry so the player can use any nearby object with E |
| `Scripts/Chest.cs` | Implementing an interface, coroutines for a one-off animation, rewarding the player |
| `Scripts/FlickerLight.cs` | Perlin noise for natural-looking flicker |
| `Scripts/CharacterAnimator.cs` | Mapping gameplay → Animator parameters (`Speed`, `FacingBack`, `Action`, `Hurt`, `Dead`) and `flipX`, shared by player and enemies |
| `Scripts/Billboard.cs` | Upright sprites in a 3D iso world |
| `Scripts/SpriteFlipbook.cs` | Playing a single sprite animation without an Animator (spell effects) |
| `Scripts/FaceTravelDirection.cs` | Rotating a camera-facing projectile sprite to match its on-screen direction |
| `Editor/SpriteSheetImporter.cs` | Importing pixel art and slicing a sheet into Sprites (Sprite Editor data provider API) |
| `Editor/CharacterSpriteBuilder.cs` | Slicing a sprite sheet, sprite-swap `AnimationClip`s, building one Animator state machine per character |
| `Editor/DungeonBuilder*.cs` (6 files) | Editor scripting: generates the scene/prefabs from an ASCII map |
| `Tests/GameplayTests.cs` | Play Mode tests (Window → General → Test Runner) |

## Levels and the builder
`Editor/DungeonBuilder*.cs` generates every scene from ASCII maps (menu **Dungeon → Rebuild All Scenes**):

| File | What it builds |
|---|---|
| `DungeonBuilder.cs` | Entry point, materials, shared helpers |
| `DungeonBuilder.Characters.cs` | Both heroes (stats live here), the skeleton, spell prefabs |
| `DungeonBuilder.Props.cs` | Chest, torch, grass, Ember Ring pickup, flag |
| `Levels/*.txt`, `Scripts/MapFile.cs`, `MapValidator.cs` | **The maps** as text files, their parser and the door checker |
| `DungeonBuilder.Levels.cs` | How a scene is assembled from a map file (`LevelSpec`, the tile switch) |
| `Scripts/Npc.cs` | Any friendly character: conversations picked by story flags, small talk that takes turns |
| `DungeonBuilder.Dragon.cs` | The NPC recipe, Amethyra's prefab **and her dialogue lines** (edit them here) |
| `DungeonBuilder.Friends.cs` | Coralie's and Bonesy's lines, the frog, the wishing fountain, the dungeon props |
| `DungeonBuilder.Home.cs` | The home's furniture and front door (prompts and messages live here) |
| `DungeonBuilder.Castle.cs` | The castle from stacked wall blocks + a hand-built pyramid mesh for roofs |
| `DungeonBuilder.CharacterSelect.cs` | The select screen scene |
| `DungeonBuilder.Title.cs` | The title screen scene |

**Levels are text files** in `Assets/Levels/<Scene>.txt`: a header (`title`, `theme`: Outdoor/Dungeon/Home, `music`,
`exit`, `exit_needs: all_monsters`, hints, minimap colors), `---`, the map, `---`, and a legend for doors
(`1 = door Level0`), the castle gate (`K = castle House`) and named arrival spots (`s = spawn ByTheTree`). Each door
also makes an arrival spot beside itself called `From<OtherScene>`, so two doors that lead to each other need nothing
else. `MapValidator` checks every door's target and arrival spot (the builder refuses to build if anything's wrong,
and `MapFileTests` runs it too). Adding a room = writing a file and running **Dungeon > Rebuild All Scenes**.

Built-in tiles: `.` ground · `,` grass tufts · `=` path · `#` stone wall · `T` wall + torch · `H` hedge ·
`K` castle (a rectangle) · `P` start · `E` skeleton · `L` slime · `M` Slime King (boss) · `C` chest · `I` Ember Ring · `D` dragon · `X` exit ·
`_` bathroom tiles · `Y` tree · `F` fountain · `b` bush · `Q` banner · `*` butterflies · `;` flowers · `B` bed · `W` toilet · `S` sink · `R` paper towel ·
`w` pond · `m` mermaid · `f` frog bush · `c` campfire · `n` Bonesy · `o` barrel · `x` crate · `j` bones · `u` mushrooms · `p` puddle · `d` door · `k` locked door · `y` key.

## Character sprites
Each character is a sprite sheet (one animation per row) plus a JSON layout, drawn by a script in `Tools/`:

| Sheet | Script | Action state |
|---|---|---|
| `Assets/Art/Wizard.png` (player) | `make_wizard_sprites.py` | `Cast` |
| `Assets/Art/Princess.png` (player) | `make_princess_sprites.py` | `Cast` |
| `Assets/Art/Skeleton.png` (enemy) | `make_skeleton_sprites.py` | `Attack` |
| `Assets/Art/Slime.png` (enemy) | `make_slime_sprites.py` | `Attack` (squash-and-stretch lunge) |
| `Assets/Art/SlimeKing.png` (boss, 64×64) | `make_slime_sprites.py` (same code at 2.6× scale) | `Attack` |
| `Assets/Art/Furniture.png` (64×64 frames) | `make_furniture_sprites.py` | (static: bed, toilet, sink, paper towel, door) |
| `Assets/Art/Dragon.png` (NPC, 64×64 frames) | `make_dragon_sprites.py` | (Idle/Talk via `SpriteFlipbook`) |

Shared drawing helpers live in `Tools/sprite_common.py`. Regenerate with e.g.
`Tools/.venv/bin/python Tools/make_skeleton_sprites.py`, then **Dungeon → Rebuild All Scenes**.

Both characters get the same state machine (`Assets/Art/Animations/<Sheet>/<Sheet>.controller`):
```
                 ┌──── Speed > 0.1 ───▶┐
  Entry ─▶ Idle ◀┴──── Speed < 0.1 ────┴ Walk
             ▲
             └── (clip finishes) ── Cast|Attack / Hurt ◀── Any State (Action / Hurt trigger, not Dead)
                                    Die                ◀── Any State (Dead == true, checked first)
```
Each of Idle/Walk/action/Hurt is a 1D **blend tree** on `FacingBack` choosing the `_Front` or `_Back` clip.
Left vs right is just `SpriteRenderer.flipX`. `CharacterAnimator` fires `Action` from `SpellAbility.Cast`
(player) or `EnemyAI.Attacked` (enemy). To see it live: select a character's **Sprite** child while playing
and open **Window → Animation → Animator**; the active state highlights as it moves and acts.

Dead enemies stop moving and stop blocking shots, then are removed after `corpseLifetime` seconds so the
Die animation has time to play.

## Spell effects
`Tools/make_spell_sprites.py` draws `Assets/Art/Fireball.png` and `TidalOrb.png` (same shapes, fire vs water palette): a 4-frame looping `Fly` animation (drawn
pointing right) and a 5-frame `Impact` burst. Pixels come from a "heat" value mapped onto a fire palette, with
per-frame noise for flicker. The **Fireball** prefab plays `Fly` with `SpriteFlipbook`, and `FaceTravelDirection`
spins it to point along its flight path on screen. On hitting anything solid it spawns **FireballImpact**, which
plays once with a fading flash of light (`FadeOutLight`) and then destroys itself.

## Auto-aim and inventory
- **Auto-aim**: casting targets the nearest skeleton inside the camera's view, preferring ones with a clear line of
  fire; with none on screen it fires the way you're facing. The wizard faces his walking direction and turns to his
  target while casting. Clicks on pickable HUD elements (inventory slots, hotbar, bars) don't cast.
- **Inventory** (**I**): one ring slot and an 8-slot bag. Hover a slot for details; click to equip/unequip. The
  **Ember Ring** (+1 fireball damage) lies in the first room, so equipped, fireballs kill skeletons in one hit.

## Props and decoration
`Tools/make_prop_sprites.py` draws `Assets/Art/Props.png`: chest (closed + 4-frame opening), gold sparkles,
a wall torch (4-frame flame) and three grass tufts that sway. They're placed from the map in `DungeonBuilder`:

| Map char | What | Notes |
|---|---|---|
| `T` | Wall with a torch | Goes on the wall's south face if there's floor below it, else its west face (the faces the camera sees) |
| `I` | Item pickup | Currently always the Ember Ring (`Assets/Items/EmberRing.asset`) |
| `C` | Chest | Press **E** nearby: opens, restores health + mana, shows a HUD message; gold dot on the minimap until opened |
| `,` | Floor with grass | 3–5 tufts, seeded by tile so rebuilds are identical |

Prefabs (`Chest`, `WallTorch`, `GrassA/B/C`) are in `Assets/Prefabs`, so you can also drag them into the scene
by hand. (Hand-placed objects are lost on **Rebuild All Scenes**, which regenerates every scene from its map.)

## Sound
`Tools/make_sounds.py` synthesizes everything into `Assets/Audio/` (Python standard library only: sine/square/
triangle/saw/noise waves, envelopes and simple filters). Regenerate, then **Dungeon → Rebuild All Scenes**.

- **Music**: `music_castle` (cheerful C major, 120 bpm, select screen + Level 0), `music_dungeon` (brooding
  A minor, 84 bpm, with dripping water) and `music_home` (a cozy F major waltz). Each is 8 bars, written as note names in the script, and loops seamlessly.
- **Effects**: casts and impacts per spell, skeleton clack/swish/death rattle, player hurt/death, chest, pickup,
  the "way down opened" fanfare, UI blips, victory/defeat jingles.
- Each component has its own sound field (e.g. `SpellAbility.castSound`, `Health.hurtSound`), so you can swap a
  sound in the Inspector; they all call `AudioManager.Play(clip)`.

## Environment textures
`Tools/make_environment_textures.py` draws seamless 16 px/unit pixel-art textures into `Assets/Art/Environment/`:
three floor variants (plain / cracked / mossy), brick wall sides (32×20, matching the 2×1.2 m face so nothing
stretches) and a wall-top cap stone. `DungeonBuilder` imports them with Point filtering, picks a floor variant
and 90° rotation per tile (seeded by grid position, so rebuilds are stable), and adds a cap quad to each wall.
Regenerate with `Tools/.venv/bin/python Tools/make_environment_textures.py`, then **Dungeon → Rebuild All Scenes**.

## HUD (storybook style)
Built with **UI Toolkit** (`Assets/UI/Hud.uxml` + `Hud.uss`, driven by `HudController.cs`), designed for a young
player: cream panels with honey-gold borders and plum text (one 9-sliced `PanelCream.png` for every box).

- **Status card** (bottom left): portrait, **one heart per health point** (empty outlines when hurt, a pop when
  healed), a turquoise **magic bar**, and a smaller row for level, XP and coins.
- **Objective card** (top left): location, a monster icon with "6 monsters left", and **progress pips** that turn
  into gold stars; when the exit unlocks it shows "The stairs are open!" with a little bounce.
- **One big spell slot** with its key ("Space", or "Click" if you cast with the mouse) and a recharge shade;
  unused slots stay hidden until abilities exist for them.
- **One hint at a time**: "E: Open chest", "W A S D: Walk" (until you've moved), "Space: Magic!" (monster nearby,
  not cast lately). The full controls list lives in a **help panel (H)**.
- **Minimap** (top right): small and round, upright markers over the turning terrain: a crown for the hero,
  stairs (grey locked / green open), monsters, chest, dragon.
- Panels for inventory (I), skills (K), dialogue, the boss bar and the win/lose banner share the same style.
HUD art comes from `Tools/make_hud_sprites.py`.

## Castle grounds atmosphere
Quiet, low-contrast grass laid in large patches (Perlin noise), fewer tufts, flowers only at points of interest
(`;`). A small set of props from `Tools/make_scenery_sprites.py`: swaying **trees** (`Y`), a **fountain** with
magic motes (`F`), **bushes** (`b`), **banners** by the gate (`Q`), **butterflies** (`*`). The floating island has
layered earth edges and clouds drifting beneath, warm sunlight and cooler shadows. Defeated monsters vanish in a
**puff of stars**, coins fly to the hero, and the exit's **stairs** glow, sparkle and chime when they open.

## Exercises to try
1. Select an Enemy prefab and tweak `Move Speed`/`Aggro Range` in the Inspector while playing.
2. Edit a map in `DungeonBuilder.Levels.cs`, then **Dungeon → Rebuild All Scenes** (this overwrites the scenes).
3. Fill hotbar slot 2 with a new ability, e.g. a mana potion on key `2` with a charge count label.
4. Swap enemy chasing for a `NavMeshAgent` (AI Navigation package) so they path around walls.
5. Add a second ability (dash on Shift) — then switch input to the new Input System package.

## Version control
The project is a git repository. `.gitattributes` routes Unity's YAML files (scenes, prefabs, assets,
materials, animations, `.meta`) through Unity's **Smart Merge**. Register the merge driver once per clone:

```bash
git config merge.unityyamlmerge.name "Unity Smart Merge"
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Helpers/UnityYAMLMerge' merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

