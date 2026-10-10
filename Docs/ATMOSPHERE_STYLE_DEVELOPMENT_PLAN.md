# Tidecrown — atmosphere and stylistic cohesion development plan

Prepared October 10, 2026. This is an implementation brief for improving the existing game’s presentation, suitable for handing to Claude alongside the [metroidvania plan](CLAUDE_METROIDVANIA_IMPLEMENTATION.md). It proposes art direction and concrete work; it does not claim that the visual problems listed below have all been reproduced in a running build.

## Objective

Make Tidecrown feel like **one illustrated fairy-tale kingdom with distinct places**, rather than a collection of separately generated scenes. Preserve its pixel-art storybook diorama, floating islands, gentle humor, cozy home and approachable adventure. Atmosphere should support navigation, discovery and emotion while keeping the hero, obstacles and danger readable.

Improve the existing assets and presentation pipeline before commissioning a new art style or adding extensive decoration. This assignment changes presentation, not progression. Coordinate map composition with the metroidvania redesign; do not move a required gate, alter collision or relocate a persistent pickup merely to improve a screenshot.

## Source-audited baseline

The project already has a meaningful style foundation:

- [ArtStyle.cs](../Assets/Scripts/ArtStyle.cs) defines a storybook diorama, 16 pixels per unit, shared sun directions, sky colors and shadow lean.
- [palette.py](../Tools/palette.py) defines shared generator colors: quiet environments, brighter characters/magic and deep-plum outlines. [sprite_common.py](../Tools/sprite_common.py) supplies common character-sheet conventions.
- [Billboard](../Assets/Scripts/Billboard.cs), [PixelSnap](../Assets/Scripts/PixelSnap.cs) and camera follow support consistent pixel appearance during movement. Preserve their separation of rendered position from gameplay position.
- [DungeonBuilder.Levels.cs](../Assets/Editor/DungeonBuilder.Levels.cs) supplies themed floors/materials, ambient light, directional light, dark/woods/mines/snow moods, Farm dusk and sky backgrounds.
- Region-specific generators, props, animated sprites, water, shadows and synthesized audio already exist. Extend these rather than replacing the entire library.
- [AudioManager](../Assets/Scripts/AudioManager.cs) is scene-owned, with a music loop and a small round-robin effect pool. [AmbientLoop](../Assets/Scripts/AmbientLoop.cs) currently supports an object-owned loop such as the running bath faucet; it is not a full region ambience system.
- The HUD, Title and CharacterSelect have separate stylesheets. World code, Python colors and HUD tokens are manually coordinated, creating a possible drift point.

These observations come from source inspection. Phase 0 must establish what is actually visible/audible in standalone play, including current Frostpeak content and any concurrently added work.

## Art direction requirements

### Shared visual language

**AD-01 — Quiet ground, expressive actors.** Keep floors, distant scenery and large walls lower in contrast and saturation than the hero, NPCs, important objects and magic. Avoid uniform brightness that makes everything compete for attention.

**AD-02 — Consistent craft.** Use comparable pixel density, outline treatment, silhouette simplicity, material texture frequency, sprite grounding and animation timing. Keep 16 PPU as the world default; document deliberate exceptions such as UI artwork or large bosses.

**AD-03 — Warmth with mystery.** Interiors feel protected; forests feel alive; caves feel uncertain; haunted spaces feel playful. Danger comes from readable attacks and environment cues, not sustained visual discomfort or horror imagery.

**AD-04 — One kingdom, several moods.** Reuse architectural details, carved symbols, fountain shapes, plants and gem motifs across regions. Change palette, materials, sound and composition by region instead of changing rendering conventions.

**AD-05 — Resting places matter.** Every region should have a quiet visual/audio beat near its fountain or friendly NPC. Contrast safe shelter with exploration and boss tension.

**AD-06 — Gameplay silhouettes win.** Decoration cannot obscure the hero, interactable approach, narrow route, pickup, water boundary or attack warning. Important tool gates must read clearly before the player has the tool.

### Proposed regional mood bible

The palettes below are semantic directions, not final replacement hex values. Reuse current palette values where they already work; approve changes through comparison captures.

| Place | Desired feeling | Palette / material direction | Motion and sound | Signature focal point |
|---|---|---|---|---|
| Castle Grounds / Hollyhock | Welcoming, inhabited, quietly magical | Sage grass, warm stone, coral banners, lavender shade | Sparse butterflies, birds, fountain water, village detail | Castle silhouette and fountain; Amethyra’s cave as a quieter discovery |
| Home / Kitchen | Cozy refuge, playful domestic detail | Warm wood, cream textiles, terracotta; cool marble bathroom | Gentle lamp warmth, soft house sounds, tap/soup/cooking detail | Bed, bath and stove; avoid all fixtures flashing at once |
| Dungeon | Forgotten royal rooms, secret warmth | Lavender-gray masonry, subdued brown floor, amber campfires | Sparse drips, soft hollow ambience, fire crackle | Bonesy’s campfire against cooler halls; distinct Slime King chamber |
| Mermaid Cove | Breezy, sparkling, adventurous | Warm sand, teal sea, chalky rock, restrained gold | Surf, gulls, flowing water, gentle ship/foliage movement | Lagoon/ship, sea cave and a visibly inviting swim island |
| Hollow Farm | Harvest festival with friendly spookiness | Rust/ochre plants, violet dusk, warm pumpkin light | Low wind, crows, flickering lanterns, distant festive motif | Festival bonfire and maze; graveyard is a contrasting quiet pocket |
| Whispering Woods | Drowsy wonder becoming lively | Muted greens, plum shadows, cream/coral mushrooms | Leaves, sparse motes, birds/insects; quieter hollow | Sleepy trees, Mother Mushroom grove, lantern-lit tunnel |
| Glimmer Mines | Hidden craftsmanship and crystal mystery | Slate/plum rock, amber lamps, selective topaz/cool crystal accents | Drips, soft clinks, distant cart rattle, occasional dust | Digby’s station and Golem glow; avoid illuminating every crystal equally |
| Puddlebrook Lake | Calm surface, curious hidden depth | Reed green, muted blue water, pale wood, aquamarine accents | Water lap, frogs/insects, bobber rings, slow reeds | Shore dock and fountain; Crabbington’s court has a clearer combat silhouette |
| Frostpeak | Crisp isolation with pockets of kindness | Blue-white snow, plum-blue shadows, restrained golden shelter | Light wind, sparse snow, soft chimes, campfire warmth | Mr. Frost/Granny camp and rainbow posts; preserve visibility on white ice |

## Workstreams and implementation requirements

### 1. Style tokens and asset consistency

- Inventory palette, sprite import, frame sizes, pivots, scales, outlines, shadow treatment, material/shader choices and UI tokens. Record mismatches with screenshots before changing them.
- Create a short versioned style guide with approved swatches, hero/prop scale examples, a lighting example and UI examples. Store reference captures in the repository’s chosen documentation/art location.
- Reduce drift between Python, runtime and UI colors. Prefer a small shared palette source with generated adapters if justified; otherwise add an explicit synchronization workflow/check. Avoid a large asset-management rewrite.
- Audit point filtering, compression, mipmaps and texture sizes for world pixel assets. Keep intentional smooth UI artwork distinct; do not force every asset to point filtering.
- Check foot pivots, billboard scaling, moving sprites and pixel snapping at supported resolutions. Preserve physics positions while adjusting rendering.
- Audit baked sprite shadows versus blob/real shadows to prevent objects appearing doubled, floating or lit from inconsistent directions. Keep one coherent sun direction per presentation context.

**Acceptance:** a contact sheet of representative heroes, enemies, props, loot and terrain looks related in scale and craft; exceptions are documented; rebuilding the assets reproduces the accepted style.

### 2. Lighting and materials

- Establish a few reusable lighting profiles: daylight, cozy interior, dusk, wooded shade, dark hollow, lamp-lit mine and snow. Keep regional accents within these profiles.
- Prefer a small profile/configuration structure feeding the builder over proliferating scene-specific hardcoded numbers. Preserve existing map-header behavior and default output during migration.
- Review shader/material response before increasing light intensity. Protect pale surfaces and water from blown-out highlights, and dark floors from featureless black.
- Add focused pools of light around safe spots and authored focal points. Warm/cool contrast should indicate shelter and depth without recoloring every asset.
- Make Fairy Lantern’s improvement clear: a revealed passage, readable nearby silhouettes and a warm radius. Darkness must not hide an unavoidable hazard or required explanation.
- Keep damage warnings visible in every profile, especially on bright snow, water, dark rock and beneath player effects.
- Avoid global blur, heavy bloom, chromatic aberration, depth-of-field or aggressive desaturation as default fixes. Evaluate any post-processing change against pixel clarity and gameplay captures.

**Acceptance:** the hero, nearest route and danger boundaries remain recognizable at default brightness in every region; light communicates mood without relying on maximum monitor brightness or an unreadable black room.

### 3. Composition, landmarks and environment storytelling

- For every room, identify its arrival view, route, focal point, quiet area and optional discovery. Choose one primary focal point rather than increasing detail everywhere.
- Establish foreground/midground/background separation using value, height and density. Maintain clear paths and safe approach space around interactables.
- Use prop clusters with intent: a worker’s station, an abandoned meal, a tiny picnic, a mermaid keepsake or repaired cart track. Prefer recognizable small stories to random scattering.
- Introduce region landmarks that aid memory: crooked tree, unusual crystal arch, broken dock, warm tent, carved tower. Echo their silhouettes in map landmarks where feasible.
- At metroidvania shortcuts, show material transitions and shared motifs from both regions. A Mines–Farm tunnel should look excavated and agricultural at opposite ends rather than like an unexplained portal.
- Check scenery intersections, walls/cliff seams, water banks, oversized foliage and camera occlusion. Improve staging first; add a narrowly scoped fade/occlusion solution only for persistent visibility problems.
- Preserve existing persistent object IDs. If art staging requires a pickup to move, coordinate its identity migration with gameplay work.

**Acceptance:** each room is distinguishable in an unlabelled capture; arrival views suggest a route; landmarks help players recall a blocked passage; decorations do not reduce usable space or hide essential objects.

### 4. Ambient movement and effects

- Build a small reusable vocabulary: slow foliage movement, water ripples/foam, dust, firefly-like motes, sparse drifting snow, steam and fire flicker.
- Use low density and varied timing. Seed static placement and avoid synchronized animation across all props. Separate ambient movement from urgent danger cues.
- Prefer existing flipbooks, lightweight transforms and region generators. Do not add dynamic lights to every decorative mote.
- Keep ambient effects behind gameplay where possible. Do not let bright particles resemble pickups or outline a false interaction target.
- Give important moments stronger effects: tool acquisition, route revelation, boss resolution and gem restoration. Limit ordinary sparkle so these moments stand out.
- Add reduced-motion/effects controls where materially useful; reduced settings must preserve all functional warnings and tool-gate feedback.

**Acceptance:** scenes feel alive during a still moment, while combat remains readable; effects have bounded lifetime/count and no growing accumulation after repeated travel.

### 5. Soundscape and music continuity

- Listen to all regional music/effects at common settings. Normalize perceived loudness and review harshness, repetition and overlap; raw peak normalization alone is insufficient.
- Layer one restrained environmental bed per region with occasional detail cues. Use short loops with unobtrusive seams; add local sounds near water/fire/stations only where location improves meaning.
- Preserve critical cue priority. The current round-robin pool can replace an important effect during dense combat; add modest prioritization/coalescing if listening tests reveal that problem.
- Avoid stacking full-volume effects for every area-ability target. Preserve attack impact without making the mix painful.
- Smooth music transitions across room boundaries. Do not restart the same regional loop at every adjacent room if continuity better serves exploration.
- If a persistent music/ambience owner is introduced, prevent duplicate owners, reconnect per-scene settings correctly, and preserve pause/mute/title behavior and existing test expectations.
- Use a recurring melodic idea across regions with different instruments/timbres to connect the kingdom. Keep boss tension readable without making Gentle Mode oppressive.
- Let friendly spaces briefly soften the soundscape. Avoid mandatory long musical stingers that block control.
- Match sliders to actual sound categories. If adding ambience volume, migrate existing saves safely; otherwise route ambience through Effects and communicate that choice.

**Acceptance:** travel produces no abrupt cut/overlapping loops, repeated effects remain comfortable, key cues can be heard during combat, and muted play remains understandable.

### 6. UI and world presentation

- Align HUD, Title, CharacterSelect, dialogue, inventory, quest log, skill screen, maps and cooking through shared color, border, spacing and icon rules.
- Preserve readable text, controller focus and distinct interaction states. Theme consistency does not justify making every label decorative or low contrast.
- Give maps a storybook treatment while retaining clear current-location, discovered-gate and unlocked-route states. Match tool icons to the objects players see in the world.
- Use consistent transition rhythm: short fade, stable arrival view, restrained location title. Do not repeatedly interrupt travel with large region banners.
- Match the vocabulary of save/quest/reward sounds and visuals. Decorative UI animation should not resemble a selectable or newly completed objective.

**Acceptance:** all screens look like part of the same game, work at handheld-scale sizes, and retain unambiguous selection/status feedback.

### 7. Restoration and emotional payoff

- Coordinate with current quest triggers. Amethyst possession currently clears Castle Grounds crystals; presentation must not imply that a later conversation caused an already completed effect.
- Plan small, visible restoration states: fewer plague crystals, warmer lamps, clearer lake accents, awake trees or friendlier ambient detail. Implement only states supported by gameplay flags and agreed narrative.
- Use targeted environmental changes rather than replacing every region with a dramatically saturated palette. Preserve navigation and enemy/warning contrast before and after restoration.
- Make boss/tool moments feel different from ordinary pickups: clear reward appearance, brief visual/audio punctuation, tool icon and a reminder of one known obstacle.
- Keep restored-state output deterministic and saved. Re-entering a room should not repeat the full celebration unless intended.

**Acceptance:** the player can see a consequence of progress, and dialogue, saved flags and environmental state agree after reload and alternate collection order.

## Development phases

| Phase | Deliverables | Acceptance gate | Rough effort |
|---|---|---|---|
| 0 — Capture and diagnose | Asset/import audit; baseline screenshots and audio recordings; prioritized issue list; style bible draft | Separate observed defects from proposals; choose representative target hardware/resolutions | 1–2 days |
| 1 — Prove a cohesive slice | Castle arrival, cozy Home corner, Woods gate/hollow and one boss capture; shared token/import fixes | Clear before/after improvement in craft, mood and readability; no gameplay regression | 3–5 days |
| 2 — Profiles and generation | Lighting profiles, palette synchronization, consistent sprite/material/shadow rules; rebuilt slice | Clean rebuild reproduces the accepted slice without manual scene fixes | 2–4 days |
| 3 — Regional atmosphere | Apply mood bible to existing regions; landmark/prop staging; restrained ambient effects | Every region distinct yet related; all entrances, tools and warnings visible | 5–10 days |
| 4 — Sound and transitions | Regional ambience, mix fixes, music continuity, arrival rhythm | No overlapping music/abrupt same-region restarts; comfortable busy-combat mix | 3–5 days |
| 5 — UI and progress payoff | Screen cohesion, gate/reward presentation, supported restoration moments | Consistent screens and truthful progress feedback; saved states survive restart | 3–5 days |
| 6 — Verification and polish | Performance measurements, resolution/device review, child/adult playtests, final captures/docs | No readability, progression or save regressions; documented target-device results | 2–4 days |

Estimates assume incremental changes by one developer with existing generators. They are not deadlines. Reuse current assets, and stop expanding scope when the representative slice has not yet proven the direction.

Prioritize first: visible sprite/scale/shadow inconsistencies, unreadable dark/snow scenes, obscured actors or gates, disruptive audio transitions, then missing decorative detail. More particles and props should be the final response to a room that feels empty, after composition and sound are considered.

## Claude implementation instructions

1. Read applicable project instructions and inspect current Git status. Preserve ongoing work; do not reset or overwrite other edits. Reconfirm available regions before changing content.
2. Complete the baseline capture and propose a small set of style rules with concrete examples. Do not claim source inspection proves an asset looks wrong.
3. Implement the representative slice through authored generators/builders. Avoid treating generated Unity scenes as the primary source of an art fix.
4. Make routine choices within this brief autonomously. Record any change of art direction or substantive scope substitution; do not silently replace pixel art, camera perspective or renderer.
5. Keep new systems small and reusable. Use existing rendering/audio systems unless measured limitations require a change.
6. Coordinate with the metroidvania route manifest. Record presentation-driven map edits, collisions and persistent IDs; verify that paths/gates still behave correctly.
7. Preserve asset GUIDs/import metadata when replacing an image or audio clip. Regenerate targeted assets where possible; explain broad generated diffs.
8. Test meaningful risks: generator reproducibility, profile/default compatibility, audio lifecycle, saved settings and persistent restoration. Use visual/listening QA for aesthetic judgments rather than tests that mirror arbitrary RGB constants.
9. Finish with before/after captures, an implemented style guide, a change list, actual verification results and remaining issues. Clearly distinguish approved visual targets from measured outcomes.

## Verification matrix

Capture comparisons with the same location, hero, camera framing, resolution, game state and volume settings. Use an unmodified build or preserved baseline captures for comparison; do not reset the working tree to recreate one.

| Review | Required coverage | Pass condition |
|---|---|---|
| World readability | All playable rooms: arrival, ordinary route, gate/secret, busy combat | Hero, path, interaction and warnings remain visible |
| Pixel consistency | Standing/moving hero, props, water, camera movement; common supported resolutions | No new shimmer, stretched art pixels, mismatched scale or blurred world sprites |
| Dark / bright extremes | Woods hollow, Mines tunnels/Golem cycle, Frost snow/ice | Atmosphere remains readable; Lantern and vulnerability states are clear |
| UI | All panels with keyboard and controller; long names and objectives | Consistent style, readable text, visible focus; no cropped content |
| Audio | Quiet exploration, clustered attacks, boss, regional/same-region travel, pause/title | No clipping, duplicate loops, harsh repetition or lost essential cues |
| Persistent states | Before/after restoration, discovered/opened gates, reload and old save | Correct saved presentation; no duplicated reward celebration or lost items |
| Performance | Busy rooms/effects; repeated travel on actual target hardware | Measured acceptable frame time/memory; no unbounded effect/audio growth |
| Accessibility | Muted play, reduced motion, both heroes, Gentle Mode | Functional signals survive settings; no essential information solely in color/audio |
| Emotional usability | One child/nonreader and one adult session | Places feel distinct; players recall landmarks and recognize safe spots without coaching |

Set performance budgets from the baseline and actual release target. If aiming for 60 FPS, review p95 frame time against 16.7ms, but do not claim that budget is met without measurement. Document hardware, build, settings and intentional exceptions.

## Definition of done

The existing regions share a documented visual/audio language, each has a deliberate mood and memorable landmark, required gameplay remains readable, and the generator/build pipeline reproduces the final result. Representative before/after evidence demonstrates improvement. All changes preserve progression, save compatibility and both input paths, with measured performance and explicit verification limitations.

The handoff should include a short list of deferred presentation ideas. Do not leave an unbounded “add more atmosphere everywhere” task: each remaining item needs a location, intended emotional/gameplay effect and acceptance criterion.
