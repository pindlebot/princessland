# Tidecrown — phased development plan

Written October 10, 2026. This is a short, ordered plan for improving **playability first**, then cohesion and art style, for a **general audience** (pictures and sound still carry essential meaning, but the game should also feel responsive and rewarding to adults).

It condenses and sequences three existing documents; it does not replace them. Go to them for detail:

- [ROADMAP.md](ROADMAP.md): milestones A–E, task checklists and the verification matrix.
- [Docs/ATMOSPHERE_STYLE_DEVELOPMENT_PLAN.md](Docs/ATMOSPHERE_STYLE_DEVELOPMENT_PLAN.md): art direction, regional mood bible, lighting and sound.
- [Docs/CURRENT_STATE-2026-10-10.md](Docs/CURRENT_STATE-2026-10-10.md): inventory of implemented content (stale in places, see below).

## Baseline (measured October 10, 2026)

Measured on a clean copy of commit `49292ba` (all uncommitted work stashed), Unity 6000.6.4f1:

- The macOS player builds from the committed scenes with no errors.
- Play Mode tests: **241 passed, 0 failed, 0 skipped**, run with `-testFilter "!ZzVisualCheck"` in about 2.4 minutes.
- 59 baseline screenshots at 1280x720 from `DevCapture`, covering Title, CharacterSelect, Castle Grounds, Home, Kitchen, Farm, Cove and Dungeon. **Woods, Mines, Lake and Frost are not covered by `DevCapture`.**

Not measured: how the game feels to play, controller behaviour, audio, performance, or anything in the uncommitted HUD and auto-aim work. The uncommitted working tree did not compile its tests when last run (obsolete `GetInstanceID()`, and a missing `Teleport` helper in `GamepadTests.cs`), so that work has no test result yet.

## Known issues to resolve early

- **Docs are stale about Frostpeak.** `CURRENT_STATE` and `ROADMAP.md` list Frostpeak and the Sticker Book as future work, but `Frost1-4` scenes are built, registered and committed, Frost is a fountain-travel destination, and `StickerBook.cs` exists. Treat Frost as in scope and fix the docs.
- **Scene rebuilds create huge diffs.** Rebuilding rewrites every scene, animator controller and some prefabs (about 71k changed lines in `Cove.unity`). Decide what to commit before a rebuild lands in a PR.
- **Leftovers:** `Assets/Editor/TmpShot.cs` (temporary screenshot helper) and `Assets/Tests/ZzVisualCheck.cs` (hangs in batch mode).
- **Dark rooms:** the Dungeon spawn capture is mostly black. Check whether this is intended mood or a readability problem.

## Phase 0: clean baseline (about 1-2 days)

1. Finish or shelve the in-progress HUD and auto-aim work; get its tests compiling and passing.
2. Remove `TmpShot.cs`; remove or fix `ZzVisualCheck`.
3. Correct the stale Frost, Sticker Book and scene-count lines in `ROADMAP.md` and `CURRENT_STATE`.
4. Extend `DevCapture` to cover Woods, Mines, Lake and Frost; re-capture to complete the "before" set; build a contact sheet of heroes, enemies, props and terrain.
5. Write a short issue list of things actually seen (scale mismatches, doubled shadows, unreadable rooms, cluttered paths), with a screenshot for each.

**Done when:** a clean checkout builds and passes tests including the new work, and every region has baseline screenshots.

## Phase 1: feel and feedback (playability, P1)

Roadmap milestones B2, C1, C2, C3.

- **Failed-action feedback:** full bag, low mana, missing tool, locked door, closed boss shell each get an icon and a distinct sound.
- **Targeting:** finish the auto-aim marker; clear target indication, predictable choice among several enemies, no attacks through walls.
- **Input and hotbar:** separate ability slots 2/3 from consumable slots 2-5; verify keyboard/mouse and controller; no input leaking from menus into attacks.
- **Boss telegraphs:** audit all six bosses for warning time, danger area and safe space. Warnings readable by shape and sound as well as color, including on snow and water and under player effects.
- **Hit feedback:** distinct feedback for vulnerable impact versus deflection (Golem glow, Crabbington shell).

**Done when:** a player can tell why an action failed, which enemy will be targeted, and what each boss attack will do before it lands.

## Phase 2: guidance and trust (playability, P0/P1)

Roadmap milestones A2, A3, B1, B3.

- **First 15 minutes:** teach one action at a time (move, talk, cast, first chest, first obstacle); make the first objective visible; clarify why the Dungeon stairs stay closed.
- **Quest and map guidance:** current objective, giver portrait, progress and return destination together; map shows current location and unlocked routes without spoiling secrets.
- **Save audit:** three slots, door, boss reward, quit and Continue; unreadable-save handling; schema version.
- **Soft-lock audit across all five regions including Frost:** both heroes, both directions on every door, push blocks and dirt, gaps, early entry to later regions.
- **Gentle recovery:** verify it works in rooms without a fountain.

**Done when:** a new player reaches a meaningful objective unaided, and neither hero can lose a required item or get stuck.

## Phase 3: cohesive art slice (P1/P2)

Roadmap milestone D1 and atmosphere workstreams 1-3. Pick one slice (Castle arrival, one Home corner, the Woods hollow, one boss) and finish it before rolling out.

- Quiet ground, expressive actors: floors and walls lower in contrast than the hero, NPCs and magic.
- Check pixel consistency (16 PPU, point filtering, pivots, outlines) and one sun direction for baked and blob shadows.
- Unify palette across `Tools/palette.py`, the C# code and the HUD `.uss` with one source or a check script.
- Add 5-6 reusable lighting profiles to the builder (day, interior, dusk, woods, mine, snow). No global post-processing.
- Make the Fairy Lantern visibly valuable, and keep dark rooms navigable.
- Fix art in the generators (`Tools/`, `DungeonBuilder*.cs`), not in generated scenes.

**Done when:** the slice looks clearly better in before/after captures, and a clean rebuild reproduces it without manual scene fixes.

## Phase 4: regional identity, sound and economy (P2)

Roadmap milestones D2, D3 and atmosphere workstreams 3-6.

- Each region gets one landmark, one resting spot and one small story prop cluster, applied from the mood bible.
- Restrained ambient motion (foliage, motes, water, fire) with bounded counts.
- Per-region ambience, consistent success and failure sounds, smoother music transitions, comfortable combat mix.
- Backtracking: each traversal tool has a memorable nearby payoff; check gold sources against sinks; give Star Shards a finished purpose.
- UI screens share colors, borders and icon rules.

**Done when:** every region is distinct but related, and busy combat stays readable and audibly comfortable.

## Phase 5: finish and verify (P2)

Roadmap milestone E and the verification matrix.

- Frame a chapter ending for the current scope; make egg and gem dialogue counter-aware; give each boss and region a short resolution beat.
- Playtest with at least one child and one adult; compare against the Phase 0 captures.
- Measure performance on the target hardware.

**Done when:** the verification gate in `ROADMAP.md` passes. New regions (Storm Spire and others) stay deferred until then.

## Working notes

- Verify changes with a clean-copy build and test run (copy the repo, `git stash -u` in the copy, build and test there) so in-progress work doesn't mask or cause failures.
- Run tests with `-testFilter "!ZzVisualCheck"`. Only one Unity process can have a project open at a time.
- Record each completed task with its evidence and remaining issues in the `ROADMAP.md` progress log.
