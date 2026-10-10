# Tidecrown

A tiny isometric dungeon crawler for learning Unity (6000.6.4f1, Built-in Render Pipeline).

**Play:** open in Unity Hub → open `Assets/Scenes/Title.unity` → press ▶.
(Pressing ▶ in a level scene directly also works; you play as the wizard.)

**Flow:** Title (3 save slots) → Character Select (for a new slot) → **Level 0** (the castle grounds) → **Level 1** (the dungeon).
A gate in Level 0's southern hedge leads to **Hollow Farm**, a haunted (but friendly) farmland.
Level 0's exit only appears once its 4 skeletons are defeated. A rowboat on Level 0's pond goes to **Mermaid Cove**, a
seaside side-quest whose sea cave has a secret stair down into the dungeon too (see below). Esc / Start opens the pause menu (music and sound volume are there). M opens the world map.

**Saving:** three slots, each a JSON file in `Application.persistentDataPath` (`save1.json` ...). Every door, the
stairs and starting a new adventure autosave; the pause menu's "Save and go to title" and "Save and quit game" save at
the door you came in by. The title screen has a "Quit game" button too.
"Continue" (or just Enter / A) on the title screen picks up the most recent save.

**Modes** (pause menu, saved per slot): **Gentle** (the default) has no Game Over: monsters hit for half damage,
and at 0 hearts the hero naps and wakes by the last fountain she passed (or where she came in) with everything kept;
the boss's warning circle lasts 1.6× longer. **Adventurer** is the classic rules, with slightly faster monsters.
Level 0 is a big island to explore: a pine wood, birches and autumn trees, a camp with a tent and a campfire
(warm up there), and a rocky hill in the far corner. **Amethyra the dragon** hides in the cave inside it; walk up and press **E** to talk (E / Space / Enter / click to continue).
**Coralie the mermaid** sits in the pond and has lost her frog. Easter eggs: one bush hides **Sir Hopsalot** (find him
and tell Coralie), the fountain takes wishes (1 coin each; the third comes true), and Amethyra tells jokes.
**Hollyhock**, a little fenced village, sits a short walk east of the castle, across a meadow (follow the path from the gate): a cathedral
with a stained-glass rose window and a spire, a thatched cottage, a cobbled square with a well, benches, lampposts
and a notice board, and a pen of hens and chicks pecking at corn. **Barnaby Badger** keeps a stall in front of his
shop: talk to him once, then press **E** to buy **Bubble Bath** (10 coins; it goes in your bag, and the bath at
home turns it into a mountain of bubbles). The doors, the well, the notice board, the benches and the hens all have something to say.
Press **E** at the **castle gate** to go inside to the hero's home. The bedroom has a bed (a nap restores health
and mana), a lamp to switch on and off, and a wardrobe, bookshelf, toy chest and plant that say something different
each time. In the big bathroom you can sit on the toilet (E again flushes and stands you up; so does walking away),
pump the lavender soap and rinse your hands at the sink, then dry them on the paper towel. The big **marble bath**, three
tiles wide and open all round on a stepped marble plinth, with a gold swan-neck tap, candles and a vase of lavender,
puts the hero into their swimwear (Aldric's striped
bathing suit, Marina's aquamarine swimsuit) lying back in the water with a rubber duck, until E (or walking) gets
them out, dripping wet: grab a towel from the open **towel shelf** to dry off. Stand at its east end and press E on the gold **tap** to turn the water on (it pours and splashes, and
you can hear it running) and off again. A tall monstera and a big fern stand either side of it, with more potted
plants about the room. With a bottle of **Bubble Bath** in the bag, getting
in pours it in (one bottle per bath) and foam heaps up on the water. A doorway in the bedroom's west wall opens onto a
big, sunny **courtyard**: ivy on its walls, hedges, trees, bushes and flower tubs, a bench, a **wishing well** (like the
fountain, its third wish comes true), **Whiskers** the tabby cat, who purrs when you pet her, and a stone path to an
old barred **door** at the far end that's locked tight (there's no key for it yet). A **spiral staircase** in the
bedroom winds down to the **kitchen** (`Levels/Kitchen.txt`): a stove, a pantry and a kitchen island.

**Cooking** (after Stardew Valley's kitchen): gather ingredients, then cook them from a recipe card at the
**stove**, the only place you can cook. The first recipe is **Strawberry Pancakes**: 1 **Egg** (look in the nest box of
the hens' coop in Hollyhock), 1 **Flour** (the kitchen pantry) and 1 **Strawberry** (the fruit bowl on the kitchen
island). Each place hands out one at a time. At the stove, E opens the card (the game pauses): the dish and what it
does (+3 hearts, +50 magic), then each ingredient with how many you have, a gold star once you have it and, until you
do, where to find it. E / A (or clicking the button) cooks; Esc / B closes it. The pancakes go in the bag: click them
there to eat them, at home or in the middle of the dungeon. The front door brings you back out by the gate. Cleared levels and finished conversations are remembered between scenes.

**Quests, quick slots and fountains.** **J** (or a click of the right stick) opens the **quest log**: a card per quest with the
giver's picture, the step you're on and a picture of what to do next (`QuestCatalog.cs`; quests only *read* flags, counters
and items, via the little condition language in `Condition.cs`, so they need no saving). The bag has **Hat** and **Charm** slots,
a **paper doll** (the worn slots sit on the hero's own picture: hat and helm on the head, charm at the neck, armor on the chest, wand and ring in the hands, boots at the feet), item **tooltips** (hover any slot for its description, each attribute and what a click does), a **Treasures** tab for key items (like the Fairy Lantern: no bag room needed), and four **quick slots**, keys **2-5** (right stick
on a gamepad): food you pick up lands on the first free one; hover an item in the bag and press 2-5 to move it. Every
**fountain** is a save point: walk up and it remembers you (healing and saving); press **T** (left stick click) beside one to rest
or travel to another fountain you've touched. **Clay pots** (zap them) break for coins and now and then an apple. Easter eggs with
counters: flush the toilet ten times (a frog hands you his hat), skip washing your hands and Amethyra notices, and talk to her
10 and 25 times.

**The Whispering Woods** (`Levels/Woods1-4.txt`): a gate in the castle grounds' west hedge leads in. Room 1, the Sleepy Glade, has a
fountain, **Old Moss** and four sleepy trees to wake with your magic; room 2, the Spore Meadow, is full of rooted **Spore Puffs**;
room 3, the Mushroom Hollow, is dark (glowcaps, a chest, and the Fairy Lantern lights it); room 4 is **Mother Mushroom's Grove**.
Beating her makes the **Fairy Lantern** appear. Art: `Tools/make_woods_sprites.py`.

**The Glimmer Mines** (`Levels/Mines1-4.txt`, the third region): the Mushroom Hollow's south hedge leads down into them. Room 1, the
Entrance, has a fountain and **Digby**, the mole foreman, who has lost three of his crew (**Molly, Mortimer and Mo**) in the dark
**Mole Tunnels** (room 2): find all three, tell Digby, and the **mine-cart line** opens (a cart at each of the first three rooms:
press **E** and it rattles you to the next station; the rails are jammed with rubble until then). Room 3 is the **Crystal Cavern**;
room 4 is the **Crystal Golem**'s chamber. The Golem's topaz crystals take turns **glowing**: he can only be hurt while they glow, and the
whole cave goes dark when they dim. Beating him drops the **Mole Mitts** and the **Topaz**. The Mitts let you **push stone blocks** (lean on one:
it slides a tile, with a clunk; `PushBlock`, `PushAbility`) and **dig soft dirt** (**E**; `SoftDirt`). Blocks and mounds are
everywhere you couldn't deal with them before (the Mines have heart pieces, a star shard, a chest and the **Topaz Dragon Egg**
behind them, all marked with a "come back later" bubble). New monsters: **Bats** (fast fliers) and **Pebblins** (slow, tough rocks).
Easter eggs: Digby's jokes, a "no bats" sign, a tiny mole door, a pet rock with googly eyes. Art: `Tools/make_mines_sprites.py`; builder:
`DungeonBuilder.Mines.cs` (rooms: `Levels/Mines*.txt`; header keys `floor: cave` and `walls: rock`, `mood: mines`).

**Puddlebrook Lake** (`Levels/Lake1-4.txt`, the fourth region): a crack in the south wall of the Slime King's hall in the dungeon leads out to
its shore. Room 1, the Shore, has a fountain and **Captain Clamshell**, a retired crab sailor, who teaches **fishing**: stand at a
fishing spot and press **E** to cast, wait for the bobber to dip (a plip and a ring), then press **E** again before the fish gets away (too
early scares it off; `FishingSpot`). Catch three fish for his **Fishing Rod** (a longer window, and the big fish like it better); fish sell for
coins, and a Golden Carp is worth a lot. Room 2 is the **Murky Reeds**, room 3 the **Sunken Dock**, and room 4 is **King Crabbington's Court**: he
hides in a great shell (spells tink off) while a **tide** sweeps the court in red-warned bands (`TideWaves`: step out of the band), then peeks out and
slams the ground; hit him only while he peeks out (`ShellCycle`). Beating him drops the **Bubble Charm** and the **Aquamarine**. The Charm lets you
**swim**: walk right into deep water (`SwimAbility`; no spells while swimming; the water on an island's rim stays a wall). Islands in the
lake, the castle's cove and (later) elsewhere hold heart pieces, a star shard, a chest and the **Aquamarine Dragon Egg**, all marked by a
"come back later" bubble. New monsters: **Crabs** (they only ever walk sideways across the screen, so sidestep them) and **Jellyfish** (they drift
and spit bubbles). Easter eggs: Clamshell's jokes, the Golden Carp, a "fishing / no fishing" sign, a buoy that is not a toy. Art:
`Tools/make_lake_sprites.py`; builder: `DungeonBuilder.Lake.cs`.

**Frostpeak** (`Levels/Frost1-4.txt`, the fifth region): the way in is a **water route**: a pond in the Sunken Dock (Lake room 3) has a landing at its
far end with a doorway to the mountain, so you have to **swim** there (Bubble Charm). Room 1, the Camp, has a fountain, **Mr. Frost**, a shivering
snowman who wants a scarf, and **Granny Purl** knitting by her campfire. The trade chain: Mr. Frost sends you to **Barnaby Badger** (his stall in
Hollyhock sells a ball of **yarn** once Mr. Frost has asked; `Merchant`'s second ware) > Granny Purl knits it into a **Warm Scarf** (`Npc` conversations can now *take* an item
too) > Mr. Frost is so pleased he sings you a song and gives you a **Snow Hat** (a hat: +1 heart, +10 magic). The mountain is **slippery**: ice tiles
(`floor ice` in a map's legend) and the patches an **Ice Slime** leaves behind (`IceTrail`, `IceZone`) make the hero keep their momentum
(`PlayerController.IsOnIce`). **Snow Imps** lob snowballs. Room 4 is the **Snow Yeti**'s den, an arena of ice: he slams the ground, and rolls **snowballs**
along red lanes that light up first (`SnowballLanes`); he shakes loose ice slimes when hurt. Beating him drops the **Rainbow Chalk** and the **Sapphire**.
The Chalk draws **rainbow bridges**: a **chasm** (`prop chasm`, too wide to hop) with a **rainbow post** (`prop rainbowpost`) at each end; stand
at a post and press **E** and a bridge of rainbow planks appears (`RainbowPost`, `RainbowBridge`; builders pair up posts that face each other
across chasm tiles). Chasms with a heart piece, a star shard or the **Sapphire Dragon Egg** behind them are in the Camp, the Frozen Pass, the Whispering
Woods (north-west of the Spore Meadow) and the Murky Reeds. Easter eggs: Mr. Frost's song and jokes, a plain snowman, a drift with a ski pole.
Art: `Tools/make_frost_sprites.py` (it recolours the slime, so run `make_slime_sprites.py` first); builder: `DungeonBuilder.Frost.cs`;
header keys `ground: snow`, `walls: snow`, `mood: snow`.

**Stickers** (`StickerBook`, `StickerWatcher`): each of the Mines, the Lake and Frostpeak has a set of stickers (every monster, friend, treasure and secret:
beat a Bat, meet Digby, catch the Golden Carp, draw a rainbow bridge...). A sticker is earned when its condition holds (the same little language as quests),
and a toast tells you once ("New sticker: Bat! (2/11)"). The Sticker Book screen that shows them all is still to come (Phase 4).

**The Bouncy Boots loop (Phase 2).** The castle grounds start **overrun with dark green crystals**, a plague the Grey Gloom let loose when he stole
Amethyra's **Amethyst** (`CrystalPlague`; they are walk-through scenery). After you clear the grounds she tells you the Slime King has it. Beat him (his stairs now lead back
up to the grounds instead of ending the game) and he leaves two treasures where he fell: **the Amethyst** and the **Bouncy
Boots** (made from his jelly). Bring the Amethyst to Amethyra and the crystals shatter away. The boots let you **hop over gaps**: walk
into a pit tile (or press **Shift** / **B**) and you spring over it, one or two tiles wide. Gaps are placed *before* you have the boots,
each with a thought bubble showing the boots and a "?" (`HintBubble`) so you remember to come back: a secret garden with a
**heart piece** in the castle grounds' south-east corner, a star-shard alcove above the dungeon's first corridor, and an
egg chamber behind a two-tile gap in the Slime King's hall: **one of Amethyra's five dragon eggs**. Whatever you
find in the **Treasures** tab (boots, Amethyst, egg, lantern) is an *ability* just by being there: `Abilities.Has(id)`.
Both heroes also open the **spell gates** from the start: **brambles** (a thorny wall: fire burns it, water makes it bloom, a
thorn nook with a chest sits beside your start) and **braziers** (fire lights them, water fills the basin: light both in the
dungeon's mushroom grotto and a star shard appears). Four **heart pieces** make one more heart; **star shards** are kept for the
wardrobe at home (coming later). Art: `Tools/make_gate_sprites.py`; code: `Gap`, `HopAbility`, `HintBubble`, `Bramble`,
`Brazier`, `Collectible`, `CrystalPlague`; builder: `DungeonBuilder.Gates.cs`. Abilities so far: Bouncy Boots, Fairy Lantern, **Mole Mitts**, **Bubble Charm**, **Rainbow Chalk**.

**Rooms, the world map, secrets and smarter monsters.** In the Whispering Woods (and the castle grounds' west side) the
rooms connect by **room edges** (`<symbol> = edge <Scene>` in the map): an arrow on the floor, and walking onto it crosses into
the next room with a quick fade, no button (`RoomEdge`, `ScreenFade`). Press **M** (or **World map** in the pause menu: music
is turned down there now) for the **world map**: every room you've visited, laid out by its `world: x y` header, with you as
a white dot, blue marks on fountains, a **?** on every gap you've seen but can't cross yet, and a purple egg where you've
spotted one (`WorldMapView`, drawn from `WorldMapData`, which the builder makes from the map files). **Dad's Workshop** is a
secret room in the dungeon: a **fake wall** (`prop fakewall`) in the start room's east wall looks like the rest but you can
walk through it (the minimap draws it as a wall and keeps the room hidden until you do, and the room's contents stay out of sight
until then): a computer showing the Unity editor, a signed note (the words are Dad's: change them in `DungeonBuilder.Gates.cs`)
and a star shard. **Monsters path around walls** now: when the way to you isn't clear they follow an A* path over the level's 2m
tiles (`NavGrid`, baked from the physics world at the start of each level, and again when a bramble burns or a pot breaks).

**Progression:** enemies drop gold coins (walk near them) and give XP (skeleton 15, slime 20; the chest holds
25 gold). Level 2 is a big step at **400 XP** (about a whole adventure, Slime King included); after that levels need
40 × level^1.5 XP (113, 208, 320, ...). Each level gives +1 max health, +5 max mana and a **skill point**.
Press **K** for the skill path: each hero has one straight line of four steps, learned in order, one point each:
two enhancements, then two new abilities that appear in the hotbar (slot 2: **Q** / LB, slot 3: **F** / LT).

| Step | Aldric the Wizard | Princess Marina |
|---|---|---|
| 1 | Empowered Spells: +1 spell damage | Toughness: +2 max health |
| 2 | Deep Reserves: +15 max mana | Swift Tides: Tidal Orb recharges 30% faster |
| 3 | **Flame Wave** (Q, 20 mana, 4s): a fan of fire in front, burns and pushes back every monster in it | **Bubble Shield** (Q, 15 mana, 12s): blocks the next 2 hits (monsters, traps, lava) for 8s |
| 4 | **Meteor** (F, 30 mana, 9s): a warning circle, then a meteor smashes everything in it | **Whirlpool** (F, 25 mana, 9s): a pool that drags monsters to its middle and splashes them for 3.5s |

Abilities aim like the spell (the nearest monster on screen) and scale with its damage, so Empowered Spells and the
Ember Ring help them too. Level, XP, gold and skills carry across scenes and reset when you pick a hero; loading an
older save refunds any skill that isn't on the hero's path.

**The dungeon** is a labyrinth: wooden doors (E to open), a long hall to a storeroom of barrels and crates, **Bonesy** the friendly
skeleton at his campfire (warm up there for full health), a great puddle hall full of slimes with a mushroom grotto off it,
long twisty tunnels hiding the **Rusty Key**, and a locked treasure room it opens.

**Hazards** (Level 1): **spike traps** (`^`) shoot up on a loop: hidden, then a warning peek, then up for a
second, which hurts. A row of traps ripples from west to east, so follow the wave across. **Lava** (`~`) glows and
bubbles in two pools in the Slime King's hall: it stings at once and again every second you stay in, so watch where
his Ground Slam knocks you. A run across several lava tiles still costs one heart, not one per tile, and in Gentle
Mode every hazard does half damage like the monsters. `Scripts/Hazard.cs` does the hurting; `Editor/DungeonBuilder.Hazards.cs` builds them.

**Boss:** the dungeon's last room holds **the Slime King** (30 HP, hits for 2, worth 400 XP and 15–20 coins).
The crystal is sealed until he falls, and he leaves the **Amethyst** and the **Bouncy Boots** behind; the stairs lead back up to the castle grounds. *Ground Slam*: a red circle grows under you, then he leaps and lands
there: 2 damage, knockback and a screen shake if you're still inside, so step out of the circle. *Royal Split*:
at half health he splits off 3 slimelings. A boss health bar appears once he notices you.

| Hero | Spell | Health | Mana | Cast |
|---|---|---|---|---|
| Aldric the Wizard | Fireball | 5 | 50 (+8/s) | every 0.6s, 10 mana |
| Princess Marina (aquamarine) | Tidal Orb | 6 | 40 (+9/s) | every 0.45s, 8 mana |

Any controller the Input System knows works (Xbox, PlayStation, Switch Pro...), and the on-screen prompts switch
to its buttons as soon as you touch it.

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Walk | WASD / arrows | left stick / d-pad |
| Magic (auto-aims) | Space / click | X / right trigger |
| Aim at the next monster | Tab | RB |
| Abilities (once learned) | Q · F | LB · LT |
| Hop (Bouncy Boots) | walk into a gap, or Shift | walk into a gap, or B |
| Talk, open, pick up | E | A |
| Bag · skills | I · K | Y · View |
| Pause menu · back | Esc | Start · B |
| Menus | arrows · Enter | d-pad / stick · A |
| Help | H | hold RB, press View |
| Try again | R | A (after a Game Over) |
| World map | M | Start > World map |

## Where things live
| Path | What it teaches |
|---|---|
| `Scripts/PlayerController.cs` | `Update`, `CharacterController`, camera-relative input, facing the walk direction |
| `Scripts/SpellAbility.cs` + `Projectile.cs` | Prefabs, `Instantiate`/`Destroy`, cooldowns, trigger colliders, auto-targeting (viewport + line-of-fire checks). One script, two spells |
| `Scripts/CharacterDefinition.cs`, `GameSession.cs` | Heroes as data assets; carrying a choice between scenes with a static |
| `Scripts/Progression.cs`, `SkillCatalog.cs` | Plain C# data that outlives scenes: XP curve, level-ups, gold, each hero's skill path |
| `Scripts/HeroAbility.cs` + `FlameWave`, `Meteor`, `BubbleShield`, `Whirlpool` | An abstract base class (button, cooldown, mana, aiming) with one small subclass per ability; area effects as their own objects (`MeteorStrike`, `WhirlpoolZone`) |
| `Scripts/PlayerProgression.cs`, `Loot.cs`, `CoinPickup.cs` | Applying levels to the player; drops; static events (and unsubscribing in `OnDestroy`) |
| `Scripts/SkillTreeView.cs` | Building UI Toolkit elements from code instead of UXML |
| `Scripts/BossAbilities.cs` | A boss as "normal enemy + extra component"; telegraphed attacks as coroutine phases |
| `Scripts/SceneDoor.cs`, `GameSession.cs` | Doors between scenes, named arrival points, flags that survive scene loads |
| `Scripts/HouseFixture.cs` | One data-driven component for several simple interactables (an enum picks the effect) |
| `Scripts/DialogueController.cs`, `Npc.cs` | Conversations: typewriter text, pausing with `Time.timeScale`, unscaled time, serialized structs |
| `Scripts/Recipe.cs`, `CraftingStation.cs`, `CookingView.cs` | Crafting: recipes as data assets (`Create > Dungeon > Recipe`), static rules shared by the stove, the HUD and the tests, a recipe card built from code |
| `Scripts/AmbientLoop.cs` | A looping sound that plays exactly while its object is active (the bath's running faucet) |
| `Scripts/Merchant.cs` | Inheritance: a shopkeeper is an `Npc` that overrides its prompt and what talking does (spend gold, add to the bag) |
| `Scripts/Chicken.cs` | A tiny two-state machine (walk / peck) on timers, flipping a sprite to face its direction on screen |
| `Scripts/AudioManager.cs` | Music loop + a pool of `AudioSource`s for overlapping effects, random pitch variation |
| `Scripts/LevelBootstrap.cs` | Spawning the chosen hero and wiring scene objects to it at runtime; `DefaultExecutionOrder` |
| `Scripts/CharacterSelectController.cs` | A menu scene in UI Toolkit; `SceneManager.LoadScene` |
| `Scripts/GameInput.cs` | Every button in one place: the Input System's keyboard, mouse and gamepad devices |
| `Scripts/SaveSystem.cs`, `SaveData.cs`, `TitleController.cs` | Save slots with `JsonUtility`, safe file writes, a title screen |
| `Scripts/PauseMenu.cs`, `GameSettings.cs` | A menu that stops time (`Time.timeScale = 0`) and works with mouse, keys and pad |
| `Scripts/ItemDefinition.cs` | ScriptableObjects: items as data assets (`Create > Dungeon > Item`) |
| `Scripts/Inventory.cs`, `ItemPickup.cs` | A bag + five equipment slots that add up bonuses; pickups reuse the `IInteractable` system |
| `Scripts/EnemyAI.cs` | A small state machine, `Physics.Linecast` line-of-sight, gizmos; optional ranged and stationary modes |
| `Scripts/EnemyBolt.cs` | A monster's projectile (the dark mermaids' bolt); physics layers (it ignores the Water layer) |
| `Scripts/Health.cs` | Reusable components + C# events (`Damaged`, `Died`, `Healed`) |
| `Scripts/Hazard.cs` | Lava and spike traps: a timed loop (`Mathf.Repeat`), per-tile phase offsets, a shared damage cooldown |
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
| `DungeonBuilder.Abilities.cs` | The skill path abilities on each hero, and the meteor, whirlpool and bubble |
| `DungeonBuilder.Props.cs` | Chest, torch, grass, flag |
| `DungeonBuilder.Items.cs` | **Every item's stats** and its floor pickup (edit bonuses here) |
| `Levels/*.txt`, `Scripts/MapFile.cs`, `MapValidator.cs` | **The maps** as text files, their parser and the door checker |
| `DungeonBuilder.Levels.cs` | How a scene is assembled from a map file (`LevelSpec`, the tile switch) |
| `Scripts/Npc.cs` | Any friendly character: conversations picked by story flags, small talk that takes turns |
| `DungeonBuilder.Town.cs` | Hollyhock: buildings with gable roofs (a mesh with UVs in metres), the picket fence, Barnaby, the props and chickens |
| `DungeonBuilder.Dragon.cs` | The NPC recipe, Amethyra's prefab **and her dialogue lines** (edit them here) |
| `DungeonBuilder.Friends.cs` | Coralie's and Bonesy's lines, the frog, the wishing fountain, the dungeon props |
| `DungeonBuilder.Home.cs` | The home's furniture and front door (prompts and messages live here) |
| `DungeonBuilder.Bath.cs` | The sauna bath: its faucet and running water, steam, the bubble heap, the fern and monstera |
| `DungeonBuilder.Cooking.cs` | **The recipes** (ingredients and messages; add a dish here) and the stove |
| `DungeonBuilder.Castle.cs` | The castle from stacked wall blocks, a hand-built pyramid mesh for the tower roofs, and the village's gable roof for the keep |
| `DungeonBuilder.CharacterSelect.cs` | The select screen scene |
| `DungeonBuilder.Title.cs` | The title screen scene |
| `DungeonBuilder.Cove.cs` | Mermaid Cove: pirate and dark mermaid stats, Pearl's lines, the rowboat, waterfalls |
| `DungeonBuilder.Gates.cs` | The gates: gaps (pits), brambles, braziers, hint bubbles, heart pieces and star shards, the crystal plague, room edges, Dad's Workshop |
| `DungeonBuilder.Mines.cs` | The Glimmer Mines: bat, pebblin and Crystal Golem stats, Digby's and the moles' lines, blocks, soft dirt, the mine cart, the signs |
| `DungeonBuilder.Lake.cs` | Puddlebrook Lake: crab, jellyfish and King Crabbington stats, Captain Clamshell's lines, the fishing spot, scenery |
| `DungeonBuilder.Frost.cs` | Frostpeak: ice slime, snow imp and Snow Yeti stats, Mr. Frost's and Granny Purl's lines, rainbow posts, chasms and bridges |
| `DungeonBuilder.WorldMap.cs` | The world map's data (every room's place, tiles and markers) |

**Levels are text files** in `Assets/Levels/<Scene>.txt`: a header (`title`, `theme`: Outdoor/Dungeon/Home, `music`,
`exit`, `exit_needs: all_monsters`, hints, minimap colors), `---`, the map, `---`, and a legend for doors
(`1 = door Level0`), the castle gate (`K = castle House`), named arrival spots (`s = spawn ByTheTree`) and items
lying on the floor (`2 = item plumed_helm`; the builder refuses unknown ids). Each door
also makes an arrival spot beside itself called `From<OtherScene>`, so two doors that lead to each other need nothing
else. `MapValidator` checks every door's target and arrival spot (the builder refuses to build if anything's wrong,
and `MapFileTests` runs it too). Adding a room = writing a file and running **Dungeon > Rebuild All Scenes**.

Built-in tiles: `.` ground · `,` grass tufts · `=` path · `#` stone wall · `T` wall + torch · `H` hedge ·
`K` castle (a rectangle) · `P` start · `E` skeleton · `L` slime · `M` Slime King (boss) · `C` chest · `I` Ember Ring · `D` dragon · `X` exit ·
`_` bathroom tiles · `Y` tree · `F` fountain · `b` bush · `Q` banner · `*` butterflies · `;` flowers · `B` bed · `W` toilet · `S` sink · `R` paper towel ·
`A` wardrobe · `N` nightstand and lamp · `U` bookshelf · `G` toy chest · `v` plant · `r` rug ·
`w` pond · `m` mermaid · `f` frog bush · `c` campfire · `n` Bonesy · `o` barrel · `x` crate · `j` bones · `u` mushrooms · `p` puddle · `d` door · `k` locked door · `y` key ·
`~` lava · `^` spike trap ·
`t` pine · `i` birch · `a` autumn tree · `O` boulder · `z` stump · `l` log · `V` tent · `s` pebbles · `e` fern · `%` rock · `:` cave floor ·
`J` pirate · `&` dark mermaid (in the water) · `Z` Pearl (in the water) · `q` palm · `@` pirate ship (on the water) · `$` treasure heap ·
`h` wooden planks · `|` rock with a waterfall (water below it) · `+` picket fence · `-` cobbles.
Indoors (`theme: Home`), `,`/`;` and anything among them are the courtyard's grass (walls beside it grow ivy) and `=`
a path; `floor: kitchen` tiles the floor.
Legend extras for villages: `3 = building cathedral|shop|cottage` (a filled rectangle of that symbol; its front
door is in the middle of its south side), `6 = npc barnaby`, and `! = prop <kind>` for `stall`, `well`, `coop`,
`grainsack`, `lamppost`, `noticeboard`, `bench`, `planter`, `hen`, `brownhen`, `chick` and `grain`, and at home
`bathtub`, `pottedfern`, `pottedmonstera`, `cat`, `wishingwell`, `lockeddoor`, `stove`, `pantry` and `island` (the lists live in `MapFile.cs`).
`3 = stairsdown Kitchen` / `stairsup House` are doors drawn as a spiral staircase.
The gates: `\ = prop gap` (a pit tile: hop it with the boots), `` ` = prop bramble``, `6 = prop brazier`, `} = prop heartpiece`,
`9 = prop starshard` (add `hidden braziers` to make it wait for the braziers), and `item <id> hidden [braziers]` for a treasure
that waits for the boss or the braziers (`MapFile.cs` has the full list; `GatesTests` checks that every gap really seals its treasure
until you have the boots).
Header extras: `exit_spawn: <Name>` (where the stairs arrive), `plague_until: <condition>` (a level overrun with dark green crystals until e.g.
`has:amethyst` holds), `water: sea` (the sea instead of the pond), `ground: sand` (`.` and the markers are beach sand). On an
Outdoor level, a door is a **rowboat** at the end of a jetty, and water on the map's edge spills off the island.

## Character sprites
Each character is a sprite sheet (one animation per row) plus a JSON layout, drawn by a script in `Tools/`:

| Sheet | Script | Action state |
|---|---|---|
| `Assets/Art/Wizard.png` (player) | `make_wizard_sprites.py` | `Cast`, plus `Sit` and `Bathe` (front only) |
| `Assets/Art/Princess.png` (player) | `make_princess_sprites.py` | `Cast`, plus `Sit` and `Bathe` (front only) |
| `Assets/Art/Skeleton.png` (enemy) | `make_skeleton_sprites.py` | `Attack` |
| `Assets/Art/Slime.png` (enemy) | `make_slime_sprites.py` | `Attack` (squash-and-stretch lunge) |
| `Assets/Art/SlimeKing.png` (boss, 64×64) | `make_slime_sprites.py` (same code at 2.6× scale) | `Attack` |
| `Assets/Art/Furniture.png` (64×64 frames) | `make_furniture_sprites.py` | (static: bed, toilet, sink, paper towel, door, wardrobe, nightstand, bookshelf, toy chest, plant; the rug is `Rug.png`) |
| `Assets/Art/Dragon.png` (NPC, 64×64 frames) | `make_dragon_sprites.py` | (Idle/Talk via `SpriteFlipbook`) |
| `Assets/Art/Pirate.png` (enemy) | `make_cove_sprites.py` | `Attack` (a cutlass swing) |
| `Assets/Art/DarkMermaid.png` (enemy) | `make_cove_sprites.py` | `Attack` (raises an orb, throws it; Walk = Idle, she never moves) |
| `Assets/Art/Pearl.png` (NPC, 48×48) | `make_cove_sprites.py` | (Coralie's drawing in Pearl's colours) |
| `Assets/Art/Items.png` + `Art/UI/Icon<Item>.png` | `make_item_sprites.py` | (items on the floor, glinting, and their 24×24 inventory icons; the cooking ingredients and pancakes too) |
| `Assets/Art/Bath.png` (96×96 frames) | `make_bath_sprites.py` | (the sauna bath, its running stream, bubble heap, steam, and the fern and monstera) |
| `Assets/Art/Barnaby.png` (NPC, 48×48) | `make_town_sprites.py` | (Idle/Talk; plus `Chicken.png`, `TownProps.png`, `TownDecals.png` and Whiskers' `Cat.png`) |

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
- **Choosing a target**: **Tab** (RB on a gamepad) aims at the next monster in view, nearest first and then each farther
  one, round and round; it stays picked until it's defeated or leaves the screen. A gold ring on the ground and a
  target card at the top of the HUD (its name and health, under the boss bar) show which one it is.
- **Monsters hit from afar** wake up and come for the hero, however far off they were (a stationary one just watches).
- **Inventory** (**I**): five worn slots (weapon, helm, armor, boots, ring) and an 8-slot bag. Hover a slot for
  details; click (or tap, on a touch screen) to put an item on or take it off. Putting on something for a slot
  that's already filled swaps them. Extra hearts from gear arrive empty: a nap or a campfire fills them.

  | Slot | Item | Bonus | Where it lies |
  |---|---|---|---|
  | Weapon | **Starlight Wand** | +1 spell damage, spells recharge 15% faster | the dungeon's locked treasure room |
  | Helm | **Plumed Helm** | +1 heart, +10 magic | the bedroom at home, by the wardrobe |
  | Armor | **Seashell Mail** | +2 hearts | Mermaid Cove's sea cave, by the pirates' treasure |
  | Boots | **Trailblazer Boots** | walk 20% faster | the camp on the castle grounds, by the tent |
  | Ring | **Ember Ring** | +1 spell damage | the dungeon's first room (with it, fireballs kill skeletons in one hit) |
  | Helm | **Jack-o'-Lantern Hat** | +1 spell damage, +15 magic | the centre of Hollow Farm's corn maze |
  | (none) | **Bubble Bath** | used up in the bath at home: a mountain of bubbles | Barnaby's stall in Hollyhock, 10 coins a bottle |
  | (none) | **Egg**, **Flour**, **Strawberry** | cooking ingredients | the hens' coop, the kitchen pantry, the kitchen island's fruit bowl |
  | (food) | **Strawberry Pancakes** | click in the bag to eat: +3 hearts, +50 magic | cooked at the kitchen stove |

  Items are `ItemDefinition` assets whose stats are set in `Editor/DungeonBuilder.Items.cs`; `Inventory` adds up
  the bonuses, and `SpellAbility`, `PlayerController` and `PlayerProgression` read the totals. To add one: draw it
  in `Tools/make_item_sprites.py`, add an `ItemSpec`, put it on a map with a legend line (`5 = item <id>`), and
  **Rebuild All Scenes**. A new *slot* goes at the end of the `EquipSlot` enum (saves store slots as numbers), plus
  an `equip-<slot>` element in `Hud.uxml` and an entry in `HudController.WornSlots`.

## Props and decoration
`Tools/make_prop_sprites.py` draws `Assets/Art/Props.png`: chest (closed + 4-frame opening), gold sparkles,
a wall torch (4-frame flame) and three grass tufts that sway. They're placed from the map in `DungeonBuilder`:

| Map char | What | Notes |
|---|---|---|
| `T` | Wall with a torch | Goes on the wall's south face if there's floor below it, else its west face (the faces the camera sees) |
| `I` | Item pickup | The Ember Ring (`Assets/Items/EmberRing.asset`); other items go in a map's legend (`2 = item <id>`) |
| `C` | Chest | Press **E** nearby: opens, restores health + mana, shows a HUD message; gold dot on the minimap until opened |
| `,` | Floor with grass | 3–5 tufts, seeded by tile so rebuilds are identical |

Prefabs (`Chest`, `WallTorch`, `GrassA/B/C`) are in `Assets/Prefabs`, so you can also drag them into the scene
by hand. (Hand-placed objects are lost on **Rebuild All Scenes**, which regenerates every scene from its map.)

## Sound
`Tools/make_sounds.py` synthesizes everything into `Assets/Audio/` (Python standard library only: sine/square/
triangle/saw/noise waves, envelopes and simple filters). Regenerate, then **Dungeon → Rebuild All Scenes**.

- **Music**: `music_castle` (cheerful C major, 120 bpm, select screen + Level 0), `music_dungeon` (brooding
  A minor, 84 bpm, with dripping water), `music_home` (a cozy F major waltz) and `music_cove` (a D minor sea shanty in 6/8, with waves). Each is 8 bars, written as note names in the script, and loops seamlessly.
- **Effects**: Barnaby's voice, a hen's cluck, the cathedral bell and a "ka-ching" for buying, casts and impacts per spell, spike traps and lava, skeleton clack/swish/death rattle, player hurt/death, chest, pickup,
  the "way down opened" fanfare, UI blips, victory/defeat jingles.
- Each component has its own sound field (e.g. `SpellAbility.castSound`, `Health.hurtSound`), so you can swap a
  sound in the Inspector; they all call `AudioManager.Play(clip)`.

## Environment textures
`Tools/make_environment_textures.py` draws seamless 16 px/unit pixel-art textures into `Assets/Art/Environment/`:
three floor variants (plain / cracked / mossy), brick wall sides (32×20, matching the 2×1.2 m face so nothing
stretches) and a wall-top cap stone, plus the hazards' **lava** (seamless glowing cracks, also used as its emission map)
and **spike plate**. `DungeonBuilder` imports them with Point filtering, picks a floor variant
and 90° rotation per tile (seeded by grid position, so rebuilds are stable), and adds a cap quad to each wall.
Regenerate with `Tools/.venv/bin/python Tools/make_environment_textures.py`, then **Dungeon → Rebuild All Scenes**.

## HUD (storybook style)
Built with **UI Toolkit** (`Assets/UI/Hud.uxml` + `Hud.uss`, driven by `HudController.cs`), designed for a young
player: cream panels with honey-gold borders and plum text (one 9-sliced `PanelCream.png` for every box).

- **Status card** (top left): portrait, **one heart per health point**, a turquoise **magic bar**, and a
  smaller row for level, XP and coins (the coin pops when gold comes in). A heart you lose jumps and tilts before
  it empties, a healed one pops, and when only one is left it beats. In Gentle Mode a hit that costs half a
  heart shows as a **half heart**; healing mends it.
- **Hurt flash**: any hit makes the screen's edges glow red for a moment, even a bump that costs no heart.
- **Objective card** (top right, under the minimap): location, a monster icon with "6 monsters left", and
  **progress pips** that turn into gold stars; when the exit unlocks it shows "The stairs are open!" with a little bounce.
- **One spell slot** (bottom middle, compact) with its key ("Space", or "Click" if you cast with the mouse) and a recharge shade;
  unused slots stay hidden until abilities exist for them.
- **One hint at a time**: "E: Open chest", "W A S D: Walk" (until you've moved), "Space: Magic!" (monster nearby,
  not cast lately). The full controls list lives in a **help panel (H)**.
- **Minimap** (top right): small and round, upright markers over the turning terrain: a crown for the hero,
  stairs (grey locked / green open), monsters, chest, dragon.
- Panels for inventory (I), skills (K), dialogue, the boss bar and the win/lose banner share the same style.
HUD art comes from `Tools/make_hud_sprites.py`. The storybook pieces (panel, hearts, icons, pips) are drawn as smooth
shapes at 4× their on-screen size and imported with mipmaps, so they stay sharp from 720p to 4K; portraits, spell icons
and minimap markers stay pixel art. The same script draws the app icon (`Assets/Art/AppIcon.png`, a gold crown on a
plum tile); `CommandLineBuild.ApplyAppIcon` makes it the default icon in Player Settings, and every build reapplies it.

## Castle grounds atmosphere
Quiet, low-contrast grass laid in large patches (Perlin noise), fewer tufts, flowers only at points of interest
(`;`). A small set of props from `Tools/make_scenery_sprites.py`: swaying **trees** (`Y`), **pines** (`t`),
**birches** (`i`) and **autumn trees** (`a`), a **fountain** with magic motes (`F`), **bushes** (`b`), **boulders** (`O`),
**stumps** (`z`), **logs** (`l`), a **tent** (`V`), pebbles (`s`) and ferns (`e`), **banners** by the gate (`Q`),
**butterflies** (`*`). The **cave** is a hill of rock blocks (`%`, textures `RockSide`/`RockSideLow`/`RockTop`) around a
dark floor (`:`, `CaveFloor`): rocks are 2.5m crags, except where a crag would hide walkable ground behind it (north or
east, away from the camera), where they stay low so you can always see into the cave. The floating island has
layered earth edges and clouds drifting beneath, warm sunlight and cooler shadows. Defeated monsters vanish in a
**puff of stars**, coins fly to the hero, and the exit's **stairs** glow, sparkle and chime when they open.

## Mermaid Cove
Row there from the boat at the south-west corner of Level 0's pond (Coralie asks you to check on her sister). The cove
(`Levels/Cove.txt`) is a beach split by a stream, with a lagoon where the pirates' ship is anchored, waterfalls pouring
off the northern cliffs, and the sea spilling over the island's south and west edges.

- **Pirates** (`J`, 3 HP, 20 XP): skeleton-style melee with a cutlass, a little tougher.
- **Dark mermaids** (`&`, 3 HP, 25 XP): under the pirates' sea-spell. They stay in the water, rise up, raise a dark orb
  (0.3s wind-up) and throw a slow bolt from up to 8m away whenever they can see you: step aside. Bolts are blocked by
  the Bubble Shield and halved in Gentle Mode like any hit. Pushes (Flame Wave, Meteor, Whirlpool) don't move them.
- **Fighting across water**: the invisible walls that keep everyone out of water are on Unity's built-in *Water*
  layer, which spells, bolts and line-of-sight checks ignore, so you can hit a dark mermaid from the shore. (This also
  means spells now fly over Level 0's pond rather than splashing at its edge.)
- **Pearl** (`Z`), Coralie's big sister, explains the trouble, and thanks you with 30 coins once the cove is clear.
- **The sea cave** (north-east) holds a chest, the pirates' gold and a secret stair down into the dungeon that opens
  when all 12 monsters are beaten (`exit: Dungeon`, `exit_needs: all_monsters`).
- Art: `Tools/make_cove_sprites.py` (pirate, dark mermaid, Pearl, palms, rowboat, ship, splash, shells), the bolt in
  `make_spell_sprites.py`, sand/sea/planks/waterfall textures in `make_environment_textures.py`, and a 6/8 sea
  shanty (`music_cove`) plus pirate, siren, bolt and oar sounds in `make_sounds.py`.

## Hollow Farm
A farm gate in the castle grounds' southern hedge (follow the path south from the fountain) opens onto **Hollow Farm**
(`Levels/Farm.txt`), a haunted farmland that's spooky but friendly, lit at dusk (`mood: dusk`: a violet sky, a low
orange sun, and props painted a little dimmer, so the glowing things shine). No monsters here.

- A **pumpkin patch** in tilled soil (`/`), with jack-o'-lanterns that glow and flicker, crows, and a scarecrow.
- **Old Stitches**, the scarecrow who talks, by the red **barn** (`building barn`: peek through its doors), with haystacks.
- A **corn field** with a way through the middle, and a **bonfire** ringed with jack-o'-lanterns (warm up there).
- The old **graveyard** behind a picket fence: gravestones with silly epitaphs, dead trees, ground mist, will-o'-the-wisps,
  and little **ghosts** floating about (say boo to them).
- The **autumn festival** in the farm's south-west corner, through a leafy arch south of the bonfire: **Pippin** the ghost
  sells **Hot Apple Cider** at his stand (3 coins; drink it from the bag for +2 hearts and +25 magic), with bunting,
  pumpkin stacks, a tub for **bobbing for apples**, a first-prize **giant pumpkin** ringed with haystacks, autumn trees
  and fallen leaves. Beside it, a **corn maze** (`prop cornwall`: low corn walls, so you can always see the hero; drawn as
  walls on the minimap) with the **Jack-o'-Lantern Hat** (a helm: +1 spell damage, +15 magic) in a clearing at its centre.
- Art: `Tools/make_farm_sprites.py`; soil, barn and corn-maze textures in `make_environment_textures.py`; a spooky 3/4 tune
  (`music_farm`) plus a crow's caw, a ghost's "oooo" and Stitches' voice in `make_sounds.py`. Builder: `DungeonBuilder.Farm.cs`.
  Gates between levels are legend `gate <Scene>` entries (a door drawn as a farm gate).

## Exercises to try
1. Select an Enemy prefab and tweak `Move Speed`/`Aggro Range` in the Inspector while playing.
2. Edit a map in `DungeonBuilder.Levels.cs`, then **Dungeon → Rebuild All Scenes** (this overwrites the scenes).
3. Fill hotbar slot 2 with a new ability, e.g. a mana potion on key `2` with a charge count label.
4. Monsters already path around walls with `NavGrid` (A* over the level's tiles). Try swapping it for a `NavMeshAgent` (the AI Navigation package didn't compile on this Unity when I tried 2.0.9), or give them a "give up" timer.
5. Add a second ability (dash on Shift) — then switch input to the new Input System package.

## Version control
The project is a git repository. `.gitattributes` routes Unity's YAML files (scenes, prefabs, assets,
materials, animations, `.meta`) through Unity's **Smart Merge**. Register the merge driver once per clone:

```bash
git config merge.unityyamlmerge.name "Unity Smart Merge"
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Helpers/UnityYAMLMerge' merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

## Playing on a Steam Deck
The Deck runs the **native Linux build** (no Proton). Unity needs Hub's *Linux Build Support (Mono)* module.

1. On the Deck, once (Desktop Mode, Konsole): `passwd`, then `sudo systemctl enable --now sshd`. From the Mac,
   `ssh-copy-id deck@steamdeck.local` so copying doesn't ask for a password.
2. On the Mac, with the editor closed: `~/scripts/deploy-tidecrown-to-deck.sh`. It rebuilds the scenes, builds
   `Builds/Linux/` (`CommandLineBuild.RebuildLinux`) and rsyncs it to `~/Games/Tidecrown` on the Deck. Use
   `--no-scenes` to skip the scene rebuild, `--no-build` to only copy, `--host user@ip` (or `DECK_HOST`) for another address.
3. On the Deck, once: Steam > Games > *Add a Non-Steam Game*, Browse (file type: All Files), pick
   `~/Games/Tidecrown/Tidecrown.x86_64`. Leave Properties > Compatibility's Proton box **unchecked**.
4. Game Mode > Library > Non-Steam > Tidecrown. Steam Input presents the Deck's controls as an Xbox pad, so
   the gamepad bindings above apply; the touch screen works for the bag and skill panels.

After that, every update is just step 2. The Deck's screen is 1280×800 (16:10).
