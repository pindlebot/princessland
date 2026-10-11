# Phase 2 audit: saves, soft-locks, recovery and guidance

Written with the Phase 2 work (roadmap A2, A3, B1, B3). What was checked, how, what was found and what is still open.
Everything below is covered by tests (`SaveHardeningTests`, `SoftLockTests`, `RecoveryTests`, `GuidanceTests`) unless
marked **not tested**. None of it has been played by a person.

## Saves (A2)

| Question | Answer now |
|---|---|
| Slots | Three, kept apart (tested). |
| When does it save? | Every door, room edge, stairs and fountain, as before. **New:** a beaten boss, any treasure or pickup taken (`GameSession.MarkUsed`: chests, pots, dirt dug, doors opened), a skill bought, a conversation finished, fish caught / moles found / trees woken / things bought / heart pieces and star shards, and when the game is paused, loses focus or quits. Events are coalesced: one write about 1.5 s after the last one (`SaveRunner`). Saves never run from the title or character-select scenes. |
| What does Continue restore? | Everything in the save, **but arriving at the door the hero last came through, with full hearts and magic** (health and magic are not saved). The title screen says so. |
| Schema | `SaveData.version` is 2. Old files (no version, missing fields) load and are brought up to date in memory. A file with a higher version is **left alone**: shown as "Newer save", never overwritten, never offered by Continue. |
| Unreadable file | Shown as **"Damaged save"**, not as an empty slot. Opening it does nothing (it does not start a new adventure over it). Erase sets it aside as `save<N>.damaged.json`; saving over it keeps a copy there too. |
| Interrupted / failed write | The new file is written to `.tmp`, read back and checked, then swapped in; the previous good file stays as `save<N>.bak`. If the main file is damaged but the backup is fine, the backup is what loads. A failed save (full disk, no permission) returns false, raises `SaveFailed`, the HUD says "Couldn't save your adventure just now. Your last save is safe." and the last good file is untouched (tested with an unwritable temp path). |
| Unknown things in a save | Unknown items, skills, flags and counters are **kept** (they may be from a newer game) and ignored. An unknown hero falls back to the default hero. A scene that isn't in the build falls back to the castle grounds with no spawn. |
| Friendly names | Added Kitchen ("The Kitchen") and Farm ("Hollow Farm"). A test checks every room has one. |
| IsoDungeon migration | Unchanged (copies, originals stay, only when no new saves exist). **Not tested** (it reads the real data folder). |

Still open: a real disk-full and a real power-cut have not been tried; only the failure paths in code are tested.
Windows/Linux paths and cloud sync are not considered. Health and magic are deliberately not saved.

## Soft-lock audit (A3)

Method: the level files give every door, room edge and stairs. For each crossing, for **both heroes**, the game is
loaded as if the hero had come that way, and the hero must be on solid ground (clear of walls and blocks, not on a
gap, not in water, floor underneath) at the named spawn. Then, for every arrival in every room, a path must exist on
foot **with no tools** (no boots, mitts, charm or lantern) to every door and to the exit stairs.

| Finding | Result |
|---|---|
| Every door, edge and exit, in both directions, both heroes | Lands on solid ground at a real spawn (tested over ~45 crossings x 2 heroes). |
| World graph | Every room can be reached from the castle grounds and has a way back to it (the cove's cave into the dungeon is one-way by design). |
| On-foot reachability from each arrival | Every door and exit is reachable with no tools **except one deliberate gate**: the Sunken Dock (Lake3) to Frostpeak crosses deep water, so **Frostpeak needs the Bubble Charm**. The charm comes from King Crabbington one room further along (Lake4), reachable without crossing that gate, so nothing required is cut off. |
| Boss rewards | For every boss that leaves a prize (Slime King, Mother Mushroom, Crystal Golem, King Crabbington, Snow Yeti): beaten with no quest started, left before taking the prize, returned to, and taken **with a full bag**: all still there and all treasures (no bag room needed). |
| **Bug found and fixed** | A boss beaten in a room that still had other monsters (the cove, the farm) **came back** when you left and returned. `boss_down:<scene>` now remembers it (`BossAbilities.DownFlag`); the boss stays beaten even if the room is not cleared. |
| Stone blocks | A block only slides onto free floor with floor beneath it, and goes home when you leave the room. Tested that allowed slides never enter a solid square. **Not proven:** that no sequence of pushes can jam a one-wide corridor. |
| **Escape hatch added** | Pause menu: **"Stuck? Start this room again"**. It saves, reloads the room and puts the hero back at the door they came in by. Earned things (items, gold, flags, dug dirt) are kept; the room's blocks and unbeaten monsters start fresh. This is the recovery for any jam, including blocks. |
| Undug soil / gaps | Soil is solid until dug (needs the Mitts), gaps are walls until hopped. Neither sits on a required route (covered by the on-foot reachability test). Hop landing rules are covered by the existing `GatesTests`. |
| Early entry to later regions | Every region entry (Woods1, Mines1, Lake1, Frost1, Cove, Farm, Dungeon) arrives on solid ground with no tools, and the way back is reachable. Regions are **not** locked against early entry except Frostpeak (charm); the dark rooms (Woods3, Mines) are a visibility challenge only. |

Still open: the reachability check uses the game's own navigation grid, so a door that the grid thinks is reachable but
isn't (a thin gap, a prop on the path) would pass; push-block jams are covered only by the escape hatch; one-way
traps that need a tool *mid-room* (e.g. a gap hopped into) are covered by the existing gate tests, not here.

## Gentle recovery (A3)

For **every room** (all 22, with and without a fountain), the hero is wandered off the arrival spot, taken to 0
hearts, and must nap and wake: at the wake point (the arrival spot, or the fountain once touched in that room), on solid
ground, with full hearts and magic, gold intact, and a few safe seconds. All 22 pass. Rooms with no fountain simply wake
at the arrival spot. Cross-scene "last fountain" recovery is **not** implemented and is not advertised.

## Quest and map guidance (B3)

* **Tracker card** (HUD, under the objective): the giver's portrait, the quest, the step with its count ("(2/4)"), a dot
  per step, **where to go** ("Go to: Whispering Woods", or "You're here!"), and a picture of the thing to find or person
  to see. Click it (or press J) for the quest log. In the log, click a quest to follow it (saved with the slot). With no
  choice, the newest quest is followed. It hides when nothing is active.
* Every quest step now names its room (`QuestStep.Where`; a test checks each is a real room).
* **World map**: doors are small rings (gold = leads somewhere not visited yet, white = visited, grey with a cross =
  stairs still waiting for the room to be cleared); a star sits on the followed quest's room **only once that room has
  been visited**; the legend line says "Following: ... go to ...". Eggs and secrets are still only marked by the old
  rules (a seen gap), never because of a quest (tested).
* Still open: a marker for the goal on the **minimap**; the world map's fountain-travel selection order on a gamepad;
  Topaz/Aquamarine return objectives; the five-egg counter's honesty.

## First fifteen minutes (B1)

The coach (`FirstSteps`), the objective card's "why the stairs stay closed" line and Coralie's explanation were
already in and are covered by `FirstSessionTests` (which still pass). Phase 2 added the tracker, the stuck hatch and
the fixes above; **no unobserved-player playtest has been done**, so "a new player reaches a meaningful objective
unaided" is **unverified**.
