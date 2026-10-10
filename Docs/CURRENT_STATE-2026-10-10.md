# Tidecrown — current-state snapshot

Snapshot date: **October 10, 2026 (America/New_York)**. This describes the files currently in the working tree, including uncommitted and untracked game content; it is not limited to the last commit. Baseline commit: `e5c086b`.

## Scope and confidence

This is a source-and-asset audit of maps, enabled build scenes, serialized items and hero prefabs, gameplay scripts, builders, quest catalog, and existing tests. “Implemented” means the code/content exists and is wired into the project; it does **not** certify runtime behavior or visual quality. No Unity playthrough, rebuild, or test run was performed for this documentation task. Temporary `Assets/InitTestScene*.unity` files are excluded from the level inventory.

Primary sources: [maps](../Assets/Levels), [build settings](../ProjectSettings/EditorBuildSettings.asset), [items](../Assets/Items), [item builder](../Assets/Editor/DungeonBuilder.Items.cs), [quest catalog](../Assets/Scripts/QuestCatalog.cs), [skill catalog](../Assets/Scripts/SkillCatalog.cs), [hero prefabs](../Assets/Prefabs), and [tests](../Assets/Tests). Unity editor version: **6000.6.4f1**.

## At a glance

- **20 enabled scenes:** 18 playable map scenes plus Title and CharacterSelect.
- **Two heroes**, six active combat spells/abilities in total, four shared traversal tools, and eight skill definitions (four per hero).
- **27 ItemDefinition assets:** eight equipment pieces, four foods, four ordinary items, eleven treasures. Other pickups and fish are tracked outside this item catalog.
- **15 cataloged quests**, six bosses, and three implemented dragon eggs out of the five promised in dialogue.
- Save slots, persistent inventory, Gentle Mode, gamepad input, quest log, skill screen, world map, fountains, cooking, fishing, mining secrets, and home interactions already exist.
- Frostpeak, Storm Spire, the full ending, Emerald/Sapphire/Ruby progression, wardrobe spending, and Sticker Book remain future work.

## Levels that exist

Scene IDs below are filenames under `Assets/Scenes`; each playable scene has a matching text map. Hollyhock is part of Level0, and Dad’s Workshop is part of Dungeon, rather than separate scenes.

| Scene ID | Display title | Connections declared in map | Content / purpose |
|---|---|---|---|
| `Cove` | Mermaid Cove | `Dungeon`, `Level0` | Rowboat destination, Pearl, pirates/dark mermaids, Captain Grumblebeard, Seashell Mail, swimmer heart-piece island; secret Dungeon exit requires all monsters. |
| `Dungeon` | Escape the dungeon | `Lake1`, `Level0` | Slime King; Ember Ring; key/locked wand room; Bonesy; spikes/lava; Amethyst and boots; gap-gated egg; braziers and Workshop secrets. |
| `Farm` | Hollow Farm | `Level0` | Old Stitches, Gourdlings, Strawmen, Pumpkin King, corn maze/hat, Pippin/cider festival. |
| `House` | {hero}'s Home | `Kitchen`, `Level0` | Bedroom, bathroom/bath, courtyard, Whiskers, Plumed Helm; locked courtyard door with no available key. |
| `Kitchen` | {hero}'s Kitchen | `House` | Pantry, fruit bowl, stove and Strawberry Pancakes recipe. |
| `Lake1` | Puddlebrook Lake: The Shore | `Dungeon`, `Lake2` | Captain Clamshell, fountain, fishing, Crabs/Jellyfish; swimmer islands. |
| `Lake2` | Puddlebrook Lake: The Murky Reeds | `Lake1`, `Lake3` | Murky Reeds; fishing; swimmer Aquamarine Egg, heart and shard islands. |
| `Lake3` | Puddlebrook Lake: The Sunken Dock | `Lake2`, `Lake4` | Sunken Dock; fishing; swimmer chest and heart-piece islands. |
| `Lake4` | Puddlebrook Lake: King Crabbington's Court | `Lake3` | King Crabbington boss, shell cycle and tide bands; Bubble Charm and Aquamarine. |
| `Level0` | The Castle Grounds | `Cove`, `Dungeon`, `Farm`, `House`, `Woods1` | Hub, castle, Hollyhock shop/coop, Amethyra, Coralie/frog, fountain, crystal plague, camp, bramble chest and boot-gated heart piece. Dungeon stairs require all monsters. |
| `Mines1` | Glimmer Mines: The Entrance | `Mines2`, `Woods3` | Digby, fountain, mine-cart station; block/dirt-gated treasures. |
| `Mines2` | Glimmer Mines: The Mole Tunnels | `Mines1`, `Mines3` | Dark tunnels; Mo, Mortimer and Molly; cart; buried heart and pet rock. |
| `Mines3` | Glimmer Mines: The Crystal Cavern | `Mines2`, `Mines4` | Crystal cavern; cart; block-gated Topaz Egg; buried heart/secret room. |
| `Mines4` | Glimmer Mines: The Golem's Chamber | `Mines3` | Crystal Golem boss; glowing vulnerability cycle; Mole Mitts and Topaz. |
| `Woods1` | Whispering Woods: The Sleepy Glade | `Level0`, `Woods2` | Old Moss, four sleepy trees, fountain, apples, Mitts-gated buried heart. |
| `Woods2` | Whispering Woods: The Spore Meadow | `Woods1`, `Woods3`, `Woods4` | Spore Puffs, food, pots, buried star shard; branch to Woods3. |
| `Woods3` | Whispering Woods: The Mushroom Hollow | `Mines1`, `Woods2` | Dark hollow, glowcaps, chest, food; onward to Mines. |
| `Woods4` | Whispering Woods: Mother Mushroom's Grove | `Woods2` | Mother Mushroom boss; Fairy Lantern reward. |

Title provides save-slot/start flow; CharacterSelect selects Wizard or Princess. Both are enabled build scenes with no playable map file.

### World structure and gate behavior

- Level0 connects to Home, Cove, Farm, Woods and Dungeon. Woods2 branches to its boss grove and the dark hollow/Mines route. Dungeon connects to Lake. Mines and Lake each have four linked rooms.
- Current region entrances use plain map edge links; the intended earlier roadmap’s strict boots → lantern → mitts → charm region order is not enforced by those links. Darkness is a visibility challenge, and the lantern supplies light; do not describe it as a mandatory locked entrance without runtime verification.
- Both starting spell elements interact with brambles and braziers. Gaps, heavy blocks, soft dirt and deep-water treasure islands create return visits after their tools are acquired.
- The Castle Grounds plague checks `has:amethyst`, so crystals can clear before the “bring it to Amethyra” quest step finishes. Topaz/Aquamarine conversations describe restored lamps/water; equivalent region-wide restoration effects have not been established by this audit.

## Abilities and progression

Serialized player prefabs supply the combat tuning below. Ranges and durations come from ability scripts and associated prefabs; health is measured in hearts, mana in magic points.

| Hero | Starting health | Starting magic | Magic regeneration | Starting spell |
|---|---:|---:|---:|---|
| Aldric the Wizard (`Wizard`) | 5 | 50 | 8/s | Fireball |
| Princess Marina (`Princess`) | 6 | 40 | 9/s | Tidal Orb |

| Ability | Who / unlock | Mana | Base cooldown | Implemented effect |
|---|---|---:|---:|---|
| Fireball | Wizard, immediately | 10 | 0.6s | Projectile; base damage 1 plus equipment/Empowered Spells; auto-targeting |
| Tidal Orb | Princess, immediately | 8 | 0.45s | Water projectile; base damage 1 plus equipment; auto-targeting |
| Flame Wave | Wizard skill step 3 | 20 | 4s | 4.5-unit fan, 50° half-angle; spell damage +1; 1.5-unit knockback; also hits spell targets |
| Meteor | Wizard skill step 4 | 30 | 9s | Spell damage +2 within 2.5-unit radius; 0.7s fall; knockback and shake; also hits spell targets |
| Bubble Shield | Princess skill step 3 | 15 | 12s | Blocks two hits; expires after 8s |
| Whirlpool | Princess skill step 4 | 25 | 9s | Radius 3, lifetime 3.5s; pulls enemies at 2.5 units/s; spell-damage ticks every 0.7s; water interaction on spawn |

Starting-spell recharge is modified by skills and equipment. Learned-ability cooldowns use their own fixed cooldown field; do not assume recharge equipment applies to all six abilities.

| Shared tool | Acquisition | Behavior |
|---|---|---|
| Bouncy Boots | Slime King | Walk into gap (or request hop with Shift / gamepad B); up to two gap tiles; automatic hop requires a short push |
| Fairy Lantern | Mother Mushroom | Automatic warm following light in dark areas; no equipment slot or cast cost |
| Mole Mitts | Crystal Golem | Lean on heavy blocks to push; interact with loose soil to dig |
| Bubble Charm | King Crabbington | Walk into deep water to swim; casting is disabled while swimming; water at the island rim remains blocked |

All four are possession-based treasures shared by both heroes. Rainbow Chalk, ice traversal and rainbow bridges are not implemented content.

### Skill paths

Each level-up grants one point. Each path is linear; each skill requires the previous skill. Max level is 20. The first level-up requires 400 XP, then `round(40 × level^1.5)` XP. Each level adds one max heart and five max magic.

| Hero | Step 1 | Step 2 | Step 3 | Step 4 |
|---|---|---|---|---|
| Wizard | Empowered Spells: +1 spell damage | Deep Reserves: +15 max magic | Flame Wave | Meteor |
| Princess | Toughness: +2 max hearts | Swift Tides: starting-spell cooldown ×0.7 | Bubble Shield | Whirlpool |

Four points are enough to exhaust a hero’s path (earliest level 5 if all are spent). Further levels continue granting points; no later skills are defined.

## Items and their attributes

These rows are extracted from serialized `ItemDefinition` assets. Omitted numeric fields mean zero. Equipment bonuses apply while equipped; food restores current resources when eaten. Treasures use no bag space and are retained. Every definition has an icon reference and flavor description; the table records gameplay attributes and acquisition/use rather than duplicating art GUIDs.

| Item / stable ID | Type / slot | Gameplay attributes | Acquisition / use |
|---|---|---|---|
| The Amethyst (`amethyst`) | Treasure | Clears Castle Grounds crystal plague on possession | Slime King, Dungeon; Amethyra acknowledgement: 50 gold |
| Hot Apple Cider (`apple_cider`) | Food | +2 hearts restored; +25 magic restored | Pippin at Farm festival; 3 gold |
| The Aquamarine (`aquamarine`) | Treasure | No numeric bonuses | King Crabbington, Lake4; Amethyra: 60 gold |
| Bouncy Boots (`bouncy_boots`) | Treasure | Hop gaps up to 2 tiles | Slime King, Dungeon |
| Bubble Bath (`bubble_bath`) | Ordinary item | Bath interaction supplies bubbles; no food/equipment bonus | Barnaby in Hollyhock; 10 gold; bath at home |
| Bubble Charm (`bubble_charm`) | Treasure | Swim deep water; casting blocked while swimming | King Crabbington, Lake4 |
| Lucky Clover Charm (`clover_charm`) | Charm | +5% movement speed; 10% faster spell recharge | Old Moss after 4 trees and return conversation |
| Amethyst Dragon Egg (`dragon_egg_castle`) | Treasure | No numeric bonuses | Dungeon, two-tile gap; Amethyra: 30 gold |
| Aquamarine Dragon Egg (`dragon_egg_lake`) | Treasure | No numeric bonuses | Lake2 island; swim; Amethyra: 30 gold |
| Topaz Dragon Egg (`dragon_egg_mines`) | Treasure | No numeric bonuses | Mines3, block-gated chamber; Amethyra: 30 gold |
| Egg (`egg`) | Ordinary item | No numeric bonuses | Hollyhock hens’ coop; cooking ingredient |
| Ember Ring (`ember_ring`) | Ring | +1 spell damage | Dungeon, opening room |
| Fairy Lantern (`fairy_lantern`) | Treasure | Automatic carried light (range 11) | Mother Mushroom, Woods4 |
| Fishing Rod (`fishing_rod`) | Treasure | Bite window +0.5s; rare-fish weights tripled | Captain Clamshell after 3 catches |
| Flour (`flour`) | Ordinary item | No numeric bonuses | Kitchen pantry; cooking ingredient |
| Frog Hat (`frog_hat`) | Hat | +1 max hearts | House toilet; tenth flush (reward waits if bag full) |
| Healing Apple (`healing_apple`) | Food | +2 hearts restored | Woods pickups |
| Mana Berry (`mana_berry`) | Food | +40 magic restored | Woods pickups |
| Mole Mitts (`mole_mitts`) | Treasure | Push stone blocks; dig soft dirt | Crystal Golem, Mines4 |
| Strawberry Pancakes (`pancakes`) | Food | +3 hearts restored; +50 magic restored | Kitchen stove: 1 egg + 1 flour + 1 strawberry |
| Plumed Helm (`plumed_helm`) | Helm | +1 max hearts; +10 max magic | House, beside wardrobe |
| Jack-o'-Lantern Hat (`pumpkin_hat`) | Helm | +1 spell damage; +15 max magic | Farm corn-maze center |
| Seashell Mail (`seashell_mail`) | Armor | +2 max hearts | Cove, sea cave |
| Starlight Wand (`starlight_wand`) | Weapon | +1 spell damage; 15% faster spell recharge | Dungeon, Rusty Key treasure room |
| Strawberry (`strawberry`) | Ordinary item | No numeric bonuses | Kitchen fruit bowl; cooking ingredient |
| The Topaz (`topaz`) | Treasure | No numeric bonuses | Crystal Golem, Mines4; Amethyra: 60 gold |
| Trailblazer Boots (`trailblazer_boots`) | Boots | +20% movement speed | Level0, camp |

Equipment slots are Ring, Helm, Armor, Weapon, Boots, Hat and Charm. Pumpkin Hat occupies **Helm**, while Frog Hat occupies **Hat**, so their slot identities differ despite both names saying “hat.” The base bag capacity is eight entries; duplicate food/ingredients occupy bag entries. Four consumable quick bindings exist, and learned abilities also appear in hotbar slots 2/3 with distinct activation inputs. WorldMap, QuestPictures and ItemDatabase `.asset` files are supporting data, not obtainable ItemDefinitions.

### Other pickups, currencies and catches

| Content | Attributes / behavior |
|---|---|
| Coins / gold | Persistent spending currency; enemy/loot rewards, dialogue rewards, wishes and shops |
| Rusty Key | Flag-based dungeon key; unlocks the locked treasure door; not an ItemDefinition |
| Heart Piece | Persistent counter; every four gives +1 max heart; scattered behind traversal gates |
| Star Shard | Persistent counter; collected now, wardrobe spending remains future work |
| Chests / pots | Containers / breakables with loot, rather than bag items |
| Minnow / Perch / Bluegill / Rainbow Trout / Golden Carp | Fishing outcomes worth 1 / 3 / 5 / 10 / 30 gold immediately; base weights 45 / 30 / 17 / 7 / 1. Fish do not become inventory items. Golden Carp sets a discovery flag |

Fishing needs no rod to start. Base reaction window is 1s; Gentle Mode multiplies it by 1.6; the Fishing Rod adds 0.5s and triples weights for Trout/Carp.

## Quests that exist

The following **15** definitions are the entire `QuestCatalog.All` array. Quests read flags, counters and permanent items rather than having separate saved quest records. A quest can appear completed before its starting conversation if all objective conditions already hold. Rewards below come from gameplay/dialogue, rather than a generic quest payout system.

| ID / title | Start condition / giver | Ordered objectives | Reward / outcome |
|---|---|---|---|
| `frog` — Where’s Sir Hopsalot? | Meet Coralie | Find frog in bush → tell Coralie | 20 gold |
| `cove` — The Pirates’ Spell | Meet Pearl | Clear Cove → tell Pearl | 30 gold; sea-spell story resolution |
| `pancakes` — Pancake Breakfast | Visit Kitchen | Cook Strawberry Pancakes once | Food produced by recipe; no extra quest payout |
| `maze` — The Glowing Hat | Meet Pippin | Obtain Pumpkin Hat from Farm maze | Jack-o’-Lantern Hat |
| `farm` — The Haunted Farm | Meet Old Stitches | Clear Farm / defeat Pumpkin King | Farm clear; no separate catalog reward |
| `amethyst` — The Lost Amethyst | `told:amethyst`, Amethyra | Clear Dungeon → take Amethyst → tell Amethyra | 50 gold acknowledgement; Slime King also leaves boots |
| `egg` — The Lost Egg | `thanked:amethyst`, Amethyra | Hop Dungeon gap → obtain castle egg → show Amethyra | 30 gold |
| `trees` — Wake the Trees | Meet Old Moss | Wake four trees with magic → tell Old Moss | 25 gold + Lucky Clover Charm |
| `mushroom` — Mother Mushroom | `thanked:moss`, Old Moss | Clear Woods4 → take Fairy Lantern | Fairy Lantern |
| `moles` — Lost Moles | Meet Digby | Find three moles → tell Digby | 40 gold; mine-cart route unlocked |
| `golem` — The Crystal Golem | `thanked:digby`, Digby | Clear Mines4 → take Mole Mitts | Mole Mitts; boss also leaves Topaz |
| `fishing` — Fishing Lesson | `met:Clamshell`, Captain Clamshell | Catch three fish → tell Captain | 20 gold + Fishing Rod; catch gold paid separately |
| `king` — King Crabbington | `thanked:clamshell`, Captain Clamshell | Clear Lake4 → take Bubble Charm | Bubble Charm; boss also leaves Aquamarine |
| `egg_lake` — The Aquamarine Egg | Possess Bubble Charm, Amethyra | Swim to Lake2 island → obtain egg → show Amethyra | 30 gold |
| `egg_mines` — The Topaz Egg | Possess Mole Mitts, Amethyra | Obtain block-gated Mines3 egg → show Amethyra | 30 gold |

Topaz/Aquamarine acknowledgements give 60 gold each but have no separate QuestCatalog entries. Bonesy’s chats, wishes, bathing, jokes, Whiskers and the toilet frog are interactions/secrets rather than cataloged quests. The five-egg promise is only partially implemented: castle, Mines and Lake eggs exist; two are absent. The older roadmap’s Berry Pie, Lonely Skeleton introduction quest, region-wide Whiskers search and Mr. Frost scarf quest are proposals.

## Other implemented systems

- Three JSON save slots with autosaves at scene transitions, fountain interactions, and pause-menu quit actions; temporary-file replacement, older IsoDungeon save-folder migration, and out-of-path skill refunds on load. Saves hold hero, scene/spawn, flags/counters, progression, items/equipment/quick bindings and per-slot settings.
- Generic condition-based NPC conversations, one-time item/gold rewards and rotating small talk. Merchants exist for bubble bath and cider; a broader multi-item shop UI is future work.
- Gentle Mode defaults on; recovery uses a wake point in the current scene (arrival spawn until a fountain is used). Adventurer mode and volume settings exist. Cross-scene “last fountain” recovery should not be assumed from the older roadmap wording.
- Keyboard/mouse and Input System gamepad device handling; dialogue, inventory, skills, quests, help, pause, world map and fountain-travel screens.
- Grid-based enemy navigation (`NavGrid`), projectile line-of-sight checks, telegraphed bosses/hazards, auto-aim, camera follow/shake, screen fades and synthesized sound/music.
- Generated scenes/prefabs from map text, editor builders, Python sprite/audio generators and a map validator. Numerous existing tests cover saving, flow, controls, UI, cooking, gates and the regions; their presence is not evidence of a fresh passing run.

## Known seams to polish (findings, not all reproduced bugs)

1. The older roadmap opens with obsolete “no git/no saves/lost inventory” claims despite later completed checkboxes. The refreshed roadmap resolves that drift and preserves the prior plan separately.
2. Narrative restoration differs from triggers: Amethyst possession clears crystals before Amethyra thanks the player; no matching global color restoration is established for other gems.
3. Gems Topaz/Aquamarine lack explicit return quests; five-egg story and six-gem crown have no implemented conclusion.
4. Region order is mostly suggested rather than gated. Test deliberate early entry before deciding whether to enforce a sequence.
5. Seven equipment slot types and eight bag entries may create friction; consumable and ability hotbar numbering needs usability review.
6. Four-step skill trees finish long before the level-20 cap. Measure pacing and explain unused points before adding more combat systems.
7. Save-slot friendly names omit Kitchen and Farm (`PlaceName` falls back to internal IDs); unreadable saves currently look like empty slots.
8. House contains an intentionally inaccessible courtyard door. Give it a clear purpose or friendly feedback so it does not look broken.
9. New Mines/Lake assets, scenes and code exist as untracked files alongside many modifications. Validate and preserve a coherent content revision before distributing a build.

For prioritized next steps and acceptance criteria, see [the polish roadmap](../ROADMAP.md). For the previous long-term lore and expansion plan, see [the preserved roadmap](ROADMAP-legacy-2026-10-10.md).
