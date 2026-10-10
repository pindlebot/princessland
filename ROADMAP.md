# Tidecrown — polish roadmap

Updated **October 10, 2026**. This is the active plan for making the existing game feel coherent, readable, dependable and delightful. The [current-state snapshot](Docs/CURRENT_STATE-2026-10-10.md) inventories implemented content. The [previous roadmap](Docs/ROADMAP-legacy-2026-10-10.md) is preserved verbatim for its lore, expansion ideas and historical checkboxes; its introductory state description is obsolete.

## Baseline and direction

The working tree contains 18 playable map scenes, two menu scenes, two heroes, six combat abilities, four traversal tools, 27 item definitions, 15 quests and six bosses. Saving, inventory persistence, gamepad input, Gentle Mode, map files, generic NPC conversations, fountain travel, cooking and fishing are already implemented. Mines and Lake content is present, including untracked files. These are source-audited implementation statuses, not claims that every experience has been playtested or every test currently passes.

The immediate goal is **a polished, independently playable version of the existing adventure**. Further regions should wait until the first session, full existing progression loop, recovery and return visits work well. For a child, polish means understanding what to do, seeing why something did not work, and recovering without losing confidence. For an adult, it also means responsive movement, satisfying combat, coherent rewards and respect for time.

Keep these design pillars:

1. Pictures, motion and sound carry essential meaning; text adds detail.
2. Both heroes can complete every required encounter and gate.
3. Discovering a tool makes an earlier obstacle memorable and worth revisiting.
4. Gentle Mode is a complete experience with kind recovery and clear guidance.
5. Rooms have personality and small discoveries; decoration must preserve navigation.
6. Save reliability and a clear ending matter more than the number of regions.

## How to use this plan

- **P0:** blocks reliable play, progress, recovery or preservation of work.
- **P1:** directly improves understanding, controls, feedback or the main loop.
- **P2:** adds depth, completion goals or finishing touches after the loop is sound.
- **P3:** expansion and optional features; defer until the polish gates pass.
- Tasks below are unchecked work proposals. Existing features are listed in the snapshot; an unchecked audit task does not mean its underlying feature is missing.
- Work one milestone at a time. Record a short result, test evidence and remaining issues as each task is completed. Split large tasks into reviewable changes.
- Effort sizes are planning estimates: **S** ≈ half a day, **M** ≈ 1–3 days, **L** ≈ 4–7 days of focused implementation/verification. Art, iteration and child playtests can extend these; there are no committed calendar dates.
- Each milestone ends with a build and a playtest. Capture the scene, hero, mode, input device, what happened and whether a hint was needed. Address repeated confusion before adding more features.

## Milestone A — establish a trustworthy build (P0)

**Outcome:** the existing adventure can be built, resumed and traversed without lost progress or soft locks. **Estimate:** L. **Dependency:** none.

### A1. Preserve and validate the content baseline (M)

- [ ] Review the current modified/untracked Mines, Lake, art, prefab, map and builder files; preserve a coherent revision in version control when ready.
- [ ] Identify generated versus authored assets and document the rebuild order. Confirm rebuilding does not overwrite intended manual content edits.
- [ ] Build from a clean checkout using Unity 6000.6.4f1; verify all 20 enabled scenes and their dependencies are included.
- [ ] Run the existing relevant Unity test suites and record date, editor version, totals and failures. Resolve compile errors and progression failures first; do not infer success from old logs.
- [ ] Check map links, named spawns, item IDs, quest conditions, prefab references and scene/build registration. Extend the validator only where existing coverage misses real content mistakes.
- [ ] Classify `InitTestScene` artifacts; remove/exclude generated leftovers after confirming they are not needed. Keep player content out of test-only scenes.

**Done when:** a fresh checkout produces a launchable build, no missing-reference/compile errors occur, all intended maps load, and the preserved revision contains the dependencies needed to reproduce it.

### A2. Save durability and recovery (M)

- [ ] Exercise three independent slots: new game → equipment/food/quest progress → door → close application → Continue. Repeat after each boss/tool reward, digging, a fish catch and an NPC gift.
- [ ] Document the actual save moments. Decide whether critical rewards, skill purchases and quit/suspend should save immediately; implement missing moments so long rooms do not lose valuable progress.
- [ ] Test full-disk/write-denied/interrupted-save behavior using isolated test data. Handle failures without crashing or presenting success; retain the last valid save.
- [ ] Distinguish an unreadable save from an empty slot. Offer a recoverable backup if feasible and avoid silently encouraging overwrite of a damaged slot.
- [ ] Add explicit schema version handling and defensive defaults for absent fields, unknown items/skills, missing scene/spawn and a removed hero. Preserve compatible old data.
- [ ] Verify old IsoDungeon migration leaves originals intact and does not mix slots. Add Kitchen/Farm friendly save-location names.
- [ ] Decide/document whether Continue restores resources or refills them; the current saved schema does not record current health/mana. Match the menu promise to actual behavior.

**Done when:** completed critical actions survive a restart, slots remain isolated, interrupted/corrupt data does not destroy the last usable state, and save/recovery feedback is understandable.

### A3. Traversal, gates and soft-lock audit (M)

- [ ] Walk every declared connection in both directions with Wizard and Princess, before and after room clear. Include Cove’s alternate Dungeon entrance and Dungeon → Lake.
- [ ] Test entry/return spawn alignment, repeated door activation and scene-edge crossings during movement/hop/swim; prevent transition loops and spawning inside collision.
- [ ] Verify a boss defeated before accepting its quest, leaving before picking up its reward, a full bag, returning after clear and reloading all retain a viable progression route.
- [ ] Test one- and two-tile gaps at different approach angles; reject invalid landings safely. Check water borders, shore exits and no casting while swimming.
- [ ] Attempt to strand the hero with push blocks or undug soil. Provide local reset/recovery for irreversible puzzle configurations if necessary.
- [ ] Explore Mines/Lake before obtaining earlier traversal tools. Choose intentional flexible exploration or explicit region gates; align maps, hints and narrative with that decision.
- [ ] Verify Gentle recovery at arrival spawns and fountains, including rooms without fountains. Decide whether cross-scene last-fountain recovery is desired; do not advertise it until implemented.

**Done when:** every route has a safe return, neither hero can lose a required tool/reward, and recovery or local resets resolve failed attempts without sacrificing progression.

## Milestone B — make the first 15 minutes understandable (P1)

**Outcome:** a first-time player can start, move, cast, interact, find a goal and understand an obstacle. **Estimate:** L. **Dependency:** A’s playable baseline; final acceptance follows A.

### B1. First-session flow (M)

- [ ] Observe a new player from Title through CharacterSelect and Castle Grounds without coaching. Note hesitation, missed prompts, aim confusion and unnoticed paths.
- [ ] Make Continue visually primary when a save exists; clearly distinguish slot selection, new game and replacement. Ensure first focus is visible on gamepad.
- [ ] Present hero differences with hearts, magic and spell demonstrations rather than only stat prose. Explain that both heroes have complete traversal access.
- [ ] Teach one action at a time: movement → talk → spell → first chest/food → first obstacle. Reuse existing world interactions rather than adding a long mandatory tutorial.
- [ ] Make the first immediate objective visible on HUD/map and in dialogue. Clarify why Castle Dungeon stairs remain closed when all monsters are required.
- [ ] Give an optional help/hint path after inactivity or repeated unsuccessful interaction; make it dismissible and avoid covering combat.
- [ ] Ensure skipping/replaying tutorials or starting a second save does not trap the player in stale tutorial state.

**Done when:** a new player reaches a meaningful objective and can explain or demonstrate what to do next, with essential instructions understandable through pictures/actions.

### B2. Interactions and nonreader support (M)

- [ ] Establish one visual language for talkable NPCs, readable props, cooking, healing, shopping, loot and traversal obstacles. Highlight only the interaction that will activate.
- [ ] Use portraits and objective/item pictures for critical dialogue. Break long exchanges into shorter beats; show the reward and next destination clearly at completion.
- [ ] Add optional replay of important quest instructions. Reserve voice/read-aloud work for high-value instructions after text and pictures are correct.
- [ ] Explain unsuccessful actions: full bag, insufficient coins/magic, missing tool, closed boss shell, unlit brazier and locked door. Pair short text with an icon and distinct cue.
- [ ] Give the House courtyard’s inaccessible door a clear “future adventure” or intentional secret message, or remove its misleading interaction until it has a purpose.
- [ ] Separate decorative sparkle from collectible/interactable sparkle. Ensure cliffs, walkable bridges and blocked deep water read consistently.

**Done when:** the player can distinguish action targets and reasons for failure without repeated adult explanation; locked future content feels intentional.

### B3. Quest and map guidance (M)

- [ ] Show current objective, giver portrait, progress count and return destination together; make the active goal easy to reopen.
- [ ] Add optional objective tracking/map hints without revealing every secret. Use visited destinations and discovered obstacles; avoid sending players to inaccessible objectives without explaining the tool.
- [ ] Add explicit Topaz/Aquamarine return objectives or unify gem-return tracking. Make the five-egg counter display honest current progress and future scope.
- [ ] Show new → active → return to giver → complete feedback once per change. Verify repeated dialogue/reload does not replay rewards.
- [ ] Audit catalog conditions against every dialogue branch, including completing objectives before meeting the giver, out-of-order eggs and full-bag gifts.
- [ ] On world/fountain maps, distinguish current location, visited destination, selected destination, unlocked route and unvisited area. Validate gamepad selection order.

**Done when:** players know where their chosen task is, whom to return to, and why a return visit is useful; map hints do not expose unexplored secrets unnecessarily.

## Milestone C — improve feel, fairness and interface consistency (P1)

**Outcome:** controls respond predictably, combat reads clearly, and menus work comfortably on keyboard and controller. **Estimate:** L–2L. **Dependency:** A; informed by B observations.

### C1. Movement, targeting and traversal feel (M)

- [ ] Test diagonal speed, small stick movements, wall sliding, narrow paths and sprite facing while casting. Tune dead zone and acceleration only from observed problems.
- [x] Target indication and cycling (Tab / RB; a ring on the ground and a HUD target card) are in. [ ] Still to check: prefer visible/reachable threats and predictable behavior when multiple enemies/props compete; check attacks through walls.
- [ ] Make hop eligibility/landing readable before movement commits. Add a consistent blocked-hop cue and avoid double activation at scene edges.
- [ ] Match push/dig/swim timing to animation and sound. Ensure pushing a block feels deliberate rather than accidental during combat.
- [ ] Check shore-to-water sprite/ripple changes, pause, recovery and scene transition interruption for visual or collision leftovers.
- [ ] Add reduced-motion control for camera shake and large effects; avoid shake making warning shapes unreadable.

**Done when:** movement and targeting feel consistent across frame rates and both devices; traversal actions communicate start, success and failure clearly.

### C2. Combat feedback and boss fairness (L)

- [ ] Audit every boss: Slime King, Captain Grumblebeard, Pumpkin King, Mother Mushroom, Crystal Golem and King Crabbington. Record warning duration, active danger, safe space and recovery window.
- [ ] Use shape/motion/sound as well as color for slam circles, tide bands, projectiles and vulnerability. Keep warnings visible under water, scenery and player spell effects.
- [ ] Give Golem glow and Crabbington shell states distinct hit feedback: vulnerable impact versus deflection. Teach the timing safely before requiring mastery.
- [ ] Review spikes/lava, knockback, enemy spawn fans and stacked damage for unavoidable hits. Keep safe routes identifiable, especially in Gentle Mode.
- [ ] Distinguish low mana, cooldown and invalid target failures in HUD/audio. Keep the hero responsive while waiting and avoid repeated warning spam when cast is held.
- [ ] Tune hero output and survivability using completion attempts and resource use, rather than assuming equal cooldown means equal power. Include equipment and all learned abilities.
- [ ] Test Bubble Shield against enemy hits and hazards; test area abilities versus walls, boss invulnerability, pots, brambles and braziers.
- [ ] Make defeat, reward appearance and collection a satisfying sequence with clear tool demonstration and a nearby backtracking suggestion.

**Done when:** a player can recognize each dangerous attack and vulnerability window before reacting; failed attempts teach a solution and neither hero faces unfair required damage.

### C3. Menu, inventory and hotbar polish (L)

- [ ] Audit focus/default selection, confirm/back, scrolling and restoration of focus for every panel. Eliminate input leaking from a menu into attacks/interactions.
- [ ] Test controller disconnect/reconnect and keyboard ↔ controller switches while a panel is open. Update prompts consistently; verify actual left-click casting versus the input documentation.
- [ ] Resolve ambiguity between ability slots 2/3 and consumable slots 2–5 using distinct layout and button labels. Keep learned abilities discoverable without displacing food unexpectedly.
- [ ] Show item type, slot, equipped comparison and restoration values. Explain Helm versus Hat; decide whether the distinction benefits players enough to keep.
- [ ] Make full-bag behavior and reward waiting explicit. Check equipment swaps/unequipping with a full bag and consuming only when appropriate.
- [ ] Review eight-entry capacity with normal gathering/cooking/food use; adjust capacity or stacking if friction repeatedly interrupts exploration. Include save migration if storage changes.
- [ ] Increase minimum readable text and selection outlines. Check long item names, quest instructions, numeric changes, different aspect ratios and handheld-size UI.
- [ ] Keep pause/settings accessible during ordinary play; confirm time-based fishing, spell zones and dialogue behave correctly around pause.

**Done when:** the main loop and every menu can be completed using only a controller or only keyboard/mouse, with clear focus, readable content and no accidental actions.

## Milestone D — make the existing world feel cohesive (P1/P2)

**Outcome:** current regions have deliberate composition, consistent presentation and rewarding return visits. **Estimate:** 2L. **Dependency:** A–C for final sign-off; art inventory can begin earlier.

### D1. Room composition, readability and camera (L)

- [ ] Capture representative screenshots for every room at arrival, combat, secret entrance and reward. Evaluate on the actual target screen size.
- [ ] Define palette, light level and silhouette rules for castle, home, Cove, Farm, Woods, Mines and Lake. Preserve regional identities with consistent prop scale, shadows and outlines.
- [ ] Remove visual noise along critical paths. Verify trees, rocks, buildings and effects do not obscure the hero, NPC approach positions, loot or hazards.
- [ ] Audit sorting/billboards, character animation transitions, prop intersections, wall/cliff seams, water edges and camera bounds. Fix conspicuous artifacts before adding decoration.
- [ ] Ensure dark rooms are atmospheric yet navigable before Lantern acquisition if early entry remains allowed. Make the lantern upgrade visibly valuable.
- [ ] Review world-map layout against actual connectivity and route direction so the displayed geography helps rather than confuses.

**Done when:** every room has a readable entry, route and focal point; essential gameplay remains visible during crowded combat and dark scenes.

### D2. Sound and transitions (M)

- [ ] Normalize levels across music, voice blips, pickups, combat and ambience. Prevent rapid impacts or multi-target abilities from overwhelming the mix.
- [ ] Add consistent success/failure sounds for traversal, shopping, quest progression, boss deflection and save confirmation; avoid using the same cue for conflicting meanings.
- [ ] Smooth scene music changes and fade timing. Check repeat transitions, pause, recovery and fountain travel for overlapping loops or abrupt silence.
- [ ] Verify separate music/effect volumes persist per slot and no essential information depends on audio alone.
- [ ] Favor a few bespoke region/boss cues over expanding the entire audio library immediately.

**Done when:** transitions feel intentional, frequent effects are comfortable, and muted play remains understandable.

### D3. Backtracking and reward economy (L)

- [ ] Inventory every gap/block/dirt/water secret with its required tool, reward and travel distance. Ensure each tool unlock has a memorable nearby payoff.
- [ ] Place a visual reminder or map annotation when an obstacle is discovered. On acquiring its tool, suggest one earlier location without listing all secrets.
- [ ] Measure travel time between boss, giver, fountain and return treasure. Add shortcuts or travel access only where repeated walks are dull.
- [ ] Audit gold sources against bubble bath/cider/wishes, heart-piece placement and available food. Avoid plentiful rewards with no understandable purpose.
- [ ] Give Star Shards a modest, finished purpose: a small home wardrobe with a few cosmetics, previews and clear prices. Prefer completing this loop to adding another currency.
- [ ] Examine skill unlock pacing, especially the 400-XP first level versus cheaper subsequent levels. Make the first new ability arrive at a satisfying point in normal play.
- [ ] Decide what happens after four skills: stop unused skill-point accrual, provide a modest repeatable upgrade or clearly mark the completed path. Avoid creating a large tree solely to fill the level cap.

**Done when:** each traversal tool produces a visible return reward, currencies have communicated uses, and normal progression exposes the hero’s abilities without grinding.

## Milestone E — resolve the current adventure’s story (P2)

**Outcome:** players know what they have accomplished and receive a satisfying stopping point. **Estimate:** L–2L for current-scope closure; full campaign is separate. **Dependency:** A–D core loop.

- [ ] Reconcile the narrative promise with implemented content: three eggs, three obtainable crown gems and no final region. Choose a clearly framed chapter ending for the current release or commit to completing the full campaign before presenting it as finished.
- [ ] Replace fixed “two of five / three of five” egg dialogue with counter-aware lines so early Lake/Mines collection does not contradict the actual order.
- [ ] Align Amethyst restoration dialogue and `has:amethyst` plague trigger. Either acknowledge immediate restoration or change the trigger to match the return scene.
- [ ] Add visible Topaz/Aquamarine restoration if promised, or revise dialogue so it describes the action the player actually sees. Do not reintroduce a global desaturation effect without checking readability/art goals.
- [ ] Give each current boss/region a short resolution beat and clear next destination. Check Pearl, Stitches, Moss, Digby, Clamshell and Amethyra after relevant milestones.
- [ ] Add a short chapter-complete celebration, credits and Continue Exploring choice. Preserve save state and explain any future regions honestly.
- [ ] Add a compact discovered-treasures/quests summary. Count only obtainable current content in current-release completion; do not require absent eggs/gems.

**Done when:** the release has a clear beginning, progression and satisfying ending; dialogue is correct under different collection orders and does not imply unavailable actions can be completed now.

## Milestone F — release and regression pass (P0/P1)

**Outcome:** a reproducible, tested build that is comfortable to share and easy to diagnose. **Estimate:** L. **Dependency:** A–E, or a deliberately reduced release scope recorded here.

- [ ] Select actual release platforms and minimum target hardware. Keep Steam Deck/handheld verification if it is a real target; defer touch/tablet work otherwise.
- [ ] Profile the busiest rooms and effects on target hardware: lights, shadows, transparent sprites, enemy pathfinding, allocations, loads and repeated scene travel. Fix measured bottlenecks before architectural rewrites.
- [ ] Set performance budgets after measurement; proposed baseline for a 60 Hz target is stable 60 FPS with ordinary gameplay p95 frame time ≤16.7ms. Record intentional exceptions and lower-hardware targets separately.
- [ ] Run the full relevant regression suite once the release candidate is stable; add focused tests for actual fixes, especially save migration and irreversible progression changes.
- [ ] Play an uninterrupted fresh-save route and a resumed-save route; test alternate order, both heroes, both modes and both input types. Use the matrix below.
- [ ] Verify launch, quit, audio, controller reconnect, resolution/fullscreen, resume and data paths in a standalone build, not just editor play mode.
- [ ] Write player-facing controls, build version, current scope and known issues; include a simple way to identify the save slot/build in bug reports.
- [ ] Run one child/nonreader session and one adult session without coaching. Address every repeated navigation/control misunderstanding and serious loss-of-progress issue.

**Done when:** no known blocker remains, release scope is honest, normal and alternate routes pass, saves survive restart, and performance/usability results are recorded for target devices.

## Verification matrix and evidence

Use risk-based automated coverage and representative manual combinations; do not blindly multiply every test by every configuration. Full route checks must cover both heroes, while visual/controller checks must run in standalone builds.

| Scenario | Required variations | Evidence / pass condition |
|---|---|---|
| Start and resume | All three slots; fresh/old save; both heroes | Correct slot/hero/location, understandable first focus, retained progress |
| Main route | Both heroes; Gentle and Adventurer represented | All six existing bosses, rewards, return visits and safe exits |
| Out-of-order exploration | Early Woods/Mines/Lake; eggs in different orders | Reachable recovery; correct dialogue and quest states |
| Persistence | Door, fountain, quit, boss reward, gift, food, skill, dig | Restart retains intended state; no reward duplication or lost required item |
| Bag pressure | Full bag; equip/unequip; cooking; NPC gift; quick slots | Clear feedback; no item loss; treasures never blocked by capacity |
| Controls/UI | Keyboard/mouse, controller, device switch/reconnect | Every panel actionable; no leaked inputs; accurate prompts |
| Traversal | Gaps, blocks, dirt, water, edges; with/without tools | Safe collision/landings; no soft lock; consistent hint language |
| Combat | All bosses/hazards; base and upgraded heroes | Warnings visible, vulnerability understandable, fair recovery |
| Presentation | Every playable room; target aspect ratios/screen sizes | Essential actors/loot/hazards visible, readable menus, coherent sound |
| Long session | Repeated travel, deaths/recovery, menus and reloads | No growing errors/leaked audio/effects; stable performance |

Track issues with: ID, priority, scene/system, reproduction steps, expected/actual result, hero/mode/device, screenshot if useful, fix revision and verification result. Keep save fixtures isolated from family saves.

Suggested usability measures (targets to confirm through playtesting): first meaningful objective within 10–15 minutes; no repeated adult intervention for the same control; a player can recognize a blocked route’s tool; return travel is purposeful; no essential clue relies on color or reading alone. These are goals, not current measured results.

## Deferred expansion (P3)

Preserve the original Gemhold, Prism Crown, Amethyra and Grey Gloom direction. Expansion should reuse polished systems and end with complete region loops rather than isolated assets.

| Work | Scope / dependencies | Completion bar |
|---|---|---|
| Emerald / Woods story completion | Decide Emerald acquisition/restoration and placement of the remaining egg(s); avoid conflicting with the existing Lantern loop | Gem, quest, visible restoration and return payoff are coherent |
| Frostpeak | Maps, ice, Snow Yeti, Mr. Frost scarf chain, Sapphire, Rainbow Chalk, egg, secrets; depends on polished traversal/save pipeline | Both heroes finish; new gate has an earlier visible payoff; no slippery soft locks |
| Storm Spire / full ending | Rainbow bridge prerequisites, Ruby, Gloomlings, joke-bell Gloom fight, five-egg completion, garden party, credits | Complete campaign from a fresh save; kindness theme and nonreader goal are clear |
| Sticker Book | Discovery tracking, illustrated book, completion data and migration | Existing discoveries backfill where possible; obtainable targets only |
| Companions / Whiskers | Regional appearances and post-game baby dragon; navigation/interaction rules | Never blocks the player, steals interaction focus or exposes secrets unintentionally |
| Larger shop / village | More wares, purchase preview, optional Baker quest; only if current economy benefits | Useful spending, full-bag handling, controller-friendly UI |
| Helper fairy co-op | Second controller, limited helper actions, disconnect rules, one-camera design | Supports the child without compromising solo play or save ownership |
| Voice, holidays, Dream Level, secret hero, tablet | Optional delight/platform work after release stability | Small isolated additions with clear maintenance cost and no required progression |

Do not treat rainbow bridges, ghost platforms, pullable blocks, trade chains, wardrobe, Sticker Book or the full garden-party ending as already available merely because they were proposed in the legacy roadmap.

## Recommended next work package

1. Complete A1 baseline validation and A2 critical-save audit; record the first reproducible build/test result.
2. Run one fresh controller playtest through Castle → Dungeon → boots → return secret, and one early-entry Mines/Lake check. Turn observed confusion into a short P0/P1 issue list.
3. Fix the most frequent blockers and comprehension problems, then implement B3 quest/map guidance and C3 hotbar clarity.
4. Polish combat telegraphs and region presentation with a chapter ending as the current-scope release target.
5. Reassess expansion only after the release verification gate passes.

## Progress log

| Date | Milestone | Evidence / decision | Remaining work |
|---|---|---|---|
| 2026-10-10 | Planning baseline | Source/asset snapshot created; prior roadmap preserved; polish roadmap refreshed. No Unity runtime/test certification performed. | All verification and implementation tasks above remain proposed work |
