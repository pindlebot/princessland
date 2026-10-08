using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

// How a level scene is assembled from its map file (Assets/Levels/<Scene>.txt).
public static partial class DungeonBuilder
{
    // Map legend (one character per 2x2m tile; row 0 is the north/top edge):
    //   Ground   .  floor/grass     ,  with grass tufts     =  dirt path     _  bathroom tiles
    //            (space) nothing
    //   Walls    #  stone wall      T  stone wall + torch   H  hedge         K  castle (Level 0)
    //   Markers  P  player start    E  skeleton    L  slime    M  the Slime King (boss)
    //            C  chest    I  Ember Ring    D  dragon    X  exit (sealed until the boss, if any, is dead)
    //   Home     B  bed    W  toilet    S  sink    R  paper towel
    //   Legend   doors, the castle gate and named arrival spots are defined per file (MapFile.cs)
    //   Scenery  Y  tree    F  fountain (make a wish!)    b  bush    Q  banner    *  butterflies    ;  flowers
    //   Pond     w  water (not walkable)    m  Coralie the mermaid, in the water    f  the bush hiding the frog
    //   Dungeon  c  campfire (warm up: full health)    n  Bonesy, the friendly skeleton    o  barrel    x  crate
    //            j  bones    u  glowing mushrooms    p  puddle    d  wooden door    k  locked door    y  the Rusty Key
    //            (trees and the fountain are solid, so keep them off the walking routes)
    //   Hazards  ~  lava (hurts while you stand in it)    ^  spike trap (hurts while its spikes are up)
    // Torches go on the side of the wall that faces the camera (south or west), so put
    // them on walls with floor directly below (south) or to the left (west).

    private enum Theme { Dungeon, Outdoor, Home } // floors, lighting and music

    // Everything that differs between levels, read from the level's text file (see MapFile.cs).
    private class LevelSpec
    {
        public MapFile File;
        public string ScenePath;
        public string[] Map;
        public Theme Theme;
        public string Music;
        public bool ShowEnemyCount = true;
        public string Title, LockedHint, OpenHint;
        public string NextScene;         // where the exit leads ("" = final exit, wins the game)
        public bool ExitNeedsAllEnemiesDefeated;
        public Color MinimapFloor, MinimapWall;
    }

    private const string LevelsFolder = "Assets/Levels";

    // Every Assets/Levels/*.txt file, parsed and checked. Throws (building nothing) if any has a mistake.
    private static List<MapFile> LoadMaps()
    {
        var maps = Directory.GetFiles(LevelsFolder, "*.txt").OrderBy(p => p)
            .Select(p => MapFile.Parse(Path.GetFileNameWithoutExtension(p), File.ReadAllText(p)))
            .ToList();
        var errors = MapValidator.Validate(maps);
        if (errors.Count > 0)
            throw new System.Exception("[DungeonBuilder] The level files have problems:\n  " + string.Join("\n  ", errors));
        return maps;
    }

    private static LevelSpec SpecFor(MapFile file)
    {
        var theme = (Theme)System.Enum.Parse(typeof(Theme), file.Get("theme"));
        return new LevelSpec
        {
            File = file,
            ScenePath = $"Assets/Scenes/{file.Name}.unity",
            Map = file.Rows,
            Theme = theme,
            Music = file.Get("music", theme == Theme.Outdoor ? "music_castle" : theme == Theme.Home ? "music_home" : "music_dungeon"),
            Title = file.Get("title"),
            LockedHint = file.Get("locked_hint"),
            OpenHint = file.Get("open_hint", "The stairs are open!"),
            NextScene = file.Get("exit"),
            ExitNeedsAllEnemiesDefeated = file.Get("exit_needs") == "all_monsters",
            ShowEnemyCount = file.Get("enemy_count", "show") != "hide",
            MinimapFloor = HexColor(file.Get("minimap_floor", "#7A6E62")),
            MinimapWall = HexColor(file.Get("minimap_wall", "#AAA4B8")),
        };
    }

    private static Color HexColor(string hex) =>
        ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.magenta;

    private const float WallHeight = 1.2f;  // low, so walls nearest the camera don't hide the player
    private const float HedgeHeight = 0.8f;

    private static void BuildLevel(LevelSpec spec, SharedAssets assets)
    {
        var mats = assets.Materials;
        var map = spec.Map;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(spec.ScenePath);
        SetUpLighting(spec.Theme);

        var level = new GameObject("Level").transform;
        // Save the layout into the scene too, so runtime code (the minimap) can read it.
        var levelMap = level.gameObject.AddComponent<LevelMap>();
        SetStrings(levelMap, "rows", map);
        SetColor(levelMap, "floorColor", spec.MinimapFloor);
        SetColor(levelMap, "wallColor", spec.MinimapWall);

        var enemies = new GameObject("Enemies").transform;
        var decor = new GameObject("Decor").transform;
        var spawn = new GameObject("PlayerSpawn").transform;
        Health boss = null;
        ExitZone exit = null;
        var namedSpawns = new List<Transform>();

        for (int row = 0; row < map.Length; row++)
        {
            for (int col = 0; col < map[row].Length; col++)
            {
                char c = map[row][col];
                if (c == ' ') continue;

                // Row 0 is the top of the map, so it maps to the largest Z.
                var pos = new Vector3(col * Tile, 0f, (map.Length - 1 - row) * Tile);
                // Seeding by grid position gives the same "random" layout on every rebuild.
                var rng = new System.Random(row * 1000 + col);

                if (c == '#' || c == 'T')
                {
                    var wall = Block("Wall", level, pos + Vector3.up * WallHeight * 0.5f,
                                     new Vector3(Tile, WallHeight, Tile), mats["WallSide"]);
                    AddCap(wall, mats["WallTop"]);
                    if (c == 'T') PlaceTorch(assets.Torch, decor, map, col, row, pos);
                    continue;
                }
                if (c == 'H')
                {
                    var hedge = Block("Hedge", level, pos + Vector3.up * HedgeHeight * 0.5f,
                                      new Vector3(Tile, HedgeHeight, Tile), mats["HedgeSide"]);
                    AddCap(hedge, mats["HedgeTop"]);
                    AddCliff(level, pos, mats["EarthSide"]); // the floating island's earthy edge
                    continue;
                }

                if (LevelMap.IsWater(c))
                {
                    BuildWater(level, decor, assets, map, col, row, pos, rng, c == 'm');
                    continue;
                }
                if (LevelMap.IsLava(c))
                {
                    BuildLava(level, decor, assets, col, row, pos, rng);
                    continue;
                }
                if (LevelMap.IsSpikes(c))
                {
                    PlaceSpikeTrap(level, assets, col, pos);
                    continue;
                }

                var floor = Block("Floor", level, pos + Vector3.down * 0.25f, new Vector3(Tile, 0.5f, Tile),
                                  mats[FloorMaterial(spec.Theme, map, c, col, row, rng.Next(100))]);
                // Turning tiles in 90° steps hides the repetition of only three textures.
                floor.transform.rotation = Quaternion.Euler(0f, 90f * rng.Next(4), 0f);

                switch (c)
                {
                    case 'P': spawn.position = pos + Vector3.up; break;
                    case 'E':
                    case 'L':
                        var e = Place(c == 'E' ? assets.Skeleton : assets.Slime, enemies, pos + Vector3.up);
                        e.transform.rotation = Quaternion.Euler(0f, rng.Next(360), 0f);
                        break;
                    case 'M': boss = Place(assets.SlimeKing, enemies, pos + Vector3.up).GetComponent<Health>(); break;
                    case 'X':
                        exit = CreateExit(pos, mats["Exit"], spec, assets.Props);
                        Place(assets.Stairs, decor, pos + Vector3.up * 0.02f);
                        break;
                    // Chests and pickups get a stable id (scene/col,row) so they stay used when you return.
                    case 'C':
                        SetString(Place(assets.Chest, decor, pos).GetComponent<Chest>(), "persistentId", $"{sceneName}/{col},{row}");
                        break;
                    case 'I':
                        SetString(Place(assets.RingPickup, decor, pos).GetComponent<ItemPickup>(), "persistentId", $"{sceneName}/{col},{row}");
                        break;
                    case 'D': Place(assets.Dragon, decor, pos); break;
                    case 'B': Place(assets.Bed, decor, pos + Vector3.right); break; // the bed is ~1.5 tiles wide
                    case 'W': Place(assets.Toilet, decor, pos); break;
                    case 'S': Place(assets.Sink, decor, pos); break;
                    case 'R': Place(assets.PaperTowel, decor, pos); break;
                    case ',': PlaceGrass(assets.Grass, decor, rng, pos, flowers: false); break;
                    case ';': PlaceGrass(assets.Grass, decor, rng, pos, flowers: true); break;
                    case 'Y': Place(assets.Tree, decor, pos); break;
                    case 'F':
                        Place(assets.Fountain, decor, pos);
                        AddMotes(decor, pos, assets.Mote);
                        break;
                    case 'b': Place(assets.Bush, decor, pos); break;
                    case 'Q': Place(assets.Flag, decor, pos); break; // a banner on a pole
                    case '*': Place(assets.Butterfly, decor, pos); break;
                    case 'f': Place(assets.FrogBush, decor, pos); break;
                    case 'c': Place(assets.Campfire, decor, pos); break;
                    case 'n': Place(assets.Bonesy, decor, pos); break;
                    case 'o': Place(assets.Barrel, decor, pos); break;
                    case 'x': Place(assets.Crate, decor, pos); break;
                    case 'j': Place(assets.Bones, decor, pos + new Vector3(rng.Next(-4, 5) * 0.1f, 0f, rng.Next(-4, 5) * 0.1f)); break;
                    case 'u': Place(assets.Mushrooms, decor, pos); break;
                    case 'p':
                        if (rng.Next(6) == 0) // now and then, a drip landing in the puddle
                            Place(assets.Ripple, decor, pos + new Vector3(rng.Next(-5, 6) * 0.1f, 0.02f, rng.Next(-5, 6) * 0.1f));
                        break;
                    // Doors and the key remember being used (scene/col,row), like chests.
                    case 'd':
                    case 'k':
                        SetString(Place(c == 'd' ? assets.Door : assets.LockedDoor, decor, pos).GetComponent<DungeonDoor>(),
                                  "persistentId", $"{sceneName}/{col},{row}");
                        break;
                    case 'y':
                        SetString(Place(assets.Key, decor, pos).GetComponent<KeyPickup>(), "persistentId", $"{sceneName}/{col},{row}");
                        break;
                }

                // Symbols from the file's legend: doors to other scenes and named arrival spots.
                if (spec.File.Legend.TryGetValue(c, out var entry))
                {
                    if (entry.Kind == "spawn")
                        namedSpawns.Add(SpawnPoint(entry.Args[0], pos + Vector3.up, Quaternion.identity));
                    else if (entry.Kind == "door")
                        PlaceDoor(spec.File, assets.HouseDoor, decor, namedSpawns, col, row, pos);
                }
            }
        }

        // A boss guards the exit: it stays sealed until the boss is dead.
        if (boss != null && exit != null)
            SetRef(exit, "guardian", boss);

        if (spec.Theme == Theme.Outdoor)
            AddClouds(decor, map, assets.Cloud);

        // The castle (only on maps with 'K' tiles) also gives us a spawn point outside its gate.
        var castleDoor = spec.File.Doors().FirstOrDefault(d => d.IsCastle);
        var outsideGate = BuildCastle(map, level, assets, castleDoor);
        if (outsideGate != null) namedSpawns.Add(outsideGate);
        var camera = CreateCamera(spec.Theme);
        var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        SetRef(gameManager, "winSound", Sound("victory"));
        SetRef(gameManager, "loseSound", Sound("defeat"));
        SetRef(gameManager, "sleepSound", Sound("rest"));
        SetRef(gameManager, "wakeSound", Sound("pickup"));
        AddAudio(spec.Music);
        var (hud, minimap) = CreateHud(levelMap);

        // The player isn't in the scene: LevelBootstrap spawns whichever hero was chosen.
        var bootstrap = new GameObject("LevelBootstrap").AddComponent<LevelBootstrap>();
        SetRef(bootstrap, "defaultCharacter", assets.Wizard);
        SetRef(bootstrap, "spawnPoint", spawn);
        SetRefs(bootstrap, "namedSpawns", namedSpawns.Cast<Object>().ToArray());
        SetBool(bootstrap, "playerTorch", spec.Theme == Theme.Dungeon);
        SetBool(bootstrap, "showEnemyCount", spec.ShowEnemyCount);
        SetString(bootstrap, "title", spec.Title);
        SetString(bootstrap, "lockedHint", spec.LockedHint);
        SetString(bootstrap, "openHint", spec.OpenHint);
        SetRef(bootstrap, "cameraFollow", camera);
        SetRef(bootstrap, "gameManager", gameManager);
        SetRef(bootstrap, "hud", hud);
        SetRef(bootstrap, "minimap", minimap);

        // Start the camera over the spawn point so the scene looks right in the editor too.
        camera.transform.position = spawn.position - camera.transform.forward * 20f;

        EditorSceneManager.SaveScene(scene, spec.ScenePath);
    }

    private static Transform SpawnPoint(string name, Vector3 position, Quaternion rotation)
    {
        var spawn = new GameObject(name).transform;
        spawn.SetPositionAndRotation(position, rotation);
        return spawn;
    }

    // A door from the legend: it leads to its target scene, and the floor tile beside it
    // becomes this scene's arrival spot for people coming back the other way ("From<Target>").
    private static void PlaceDoor(MapFile file, GameObject prefab, Transform parent, List<Transform> spawns,
                                  int col, int row, Vector3 pos)
    {
        var info = file.Doors().First(d => d.Col == col && d.Row == row);
        var door = Place(prefab, parent, pos).GetComponent<SceneDoor>();
        SetString(door, "targetScene", info.TargetScene);
        SetString(door, "targetSpawn", info.TargetSpawn);

        var (fc, fr) = MapValidator.FloorBeside(file, col, row).Value; // the validator made sure there is one
        var arrive = new Vector3(fc * Tile, 1f, (file.Rows.Length - 1 - fr) * Tile);
        var awayFromDoor = new Vector3(arrive.x - pos.x, 0f, arrive.z - pos.z);
        spawns.Add(SpawnPoint(MapFile.SpawnNameFor(info.TargetScene), arrive, Quaternion.LookRotation(awayFromDoor)));
    }

    // A pond tile: water a little below the ground, an invisible wall so nobody walks in,
    // the odd lily pad, and on 'm' Coralie the mermaid (talked to from the nearest shore).
    private static void BuildWater(Transform level, Transform decor, SharedAssets assets, string[] map,
                                   int col, int row, Vector3 pos, System.Random rng, bool mermaid)
    {
        var water = Block("Water", level, pos + Vector3.down * 0.45f, new Vector3(Tile, 0.5f, Tile), assets.Materials["Water"]);
        water.isStatic = false; // its texture drifts
        water.AddComponent<WaterScroll>();
        var bank = new GameObject("Bank").AddComponent<BoxCollider>();
        bank.transform.SetParent(water.transform, false);
        bank.transform.position = pos + Vector3.up;
        bank.size = new Vector3(1f, 4f, 1f); // in the water block's scaled space (2 x 0.5 x 2): a 2m-tall wall
        AddBanks(water.transform.parent, map, col, row, pos, assets.Materials["Bank"]);
        if (!mermaid && rng.Next(4) == 0) // the odd lily pad, not a carpet of them
            Place(assets.Lily, decor, pos + new Vector3(rng.Next(-5, 6) * 0.1f, -0.18f, rng.Next(-5, 6) * 0.1f));
        if (!mermaid) return;

        var coralie = Place(assets.Mermaid, decor, pos + Vector3.down * 0.15f);
        coralie.transform.rotation = Quaternion.identity;
        // Talk from whichever side has dry land.
        foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            char next = MapAt(map, col + dc, row + dr);
            if (next == ' ' || LevelMap.IsWall(next) || LevelMap.IsWater(next)) continue;
            SetVector3(coralie.GetComponent<Npc>(), "talkOffset", new Vector3(dc, 0f, -dr) * 1.1f);
            break;
        }
        Place(assets.Ripple, decor, pos + new Vector3(0.3f, -0.17f, -0.5f));
    }

    // The pond sits 0.2m below the grass. Where it meets dry ground, a strip of bank (a grassy
    // lip over damp earth) covers the ground block's side, so the water looks set into the
    // ground rather than cut out of it. Each strip faces into the pond.
    private static void AddBanks(Transform parent, string[] map, int col, int row, Vector3 pos, Material bank)
    {
        foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            char next = MapAt(map, col + dc, row + dr);
            if (LevelMap.IsWater(next) || next == ' ') continue;
            var outward = new Vector3(dc, 0f, -dr); // map rows go down the screen, world z goes up
            var strip = GameObject.CreatePrimitive(PrimitiveType.Quad);
            strip.name = "Bank";
            Object.DestroyImmediate(strip.GetComponent<MeshCollider>());
            strip.transform.SetParent(parent);
            strip.transform.position = pos + outward * (Tile * 0.5f - 0.005f) + Vector3.down * 0.125f;
            strip.transform.rotation = Quaternion.LookRotation(outward); // a quad shows its back: it faces the water
            strip.transform.localScale = new Vector3(Tile, 0.25f, 1f);   // 32 x 4 texels: 16 per unit, like everything
            strip.GetComponent<Renderer>().sharedMaterial = bank;
            strip.isStatic = true;
        }
    }

    private static string FloorMaterial(Theme theme, string[] map, char c, int col, int row, int roll)
    {
        // A monster standing in a puddle stands in water too (not on a dry square).
        if ("EL".IndexOf(c) >= 0 && MapAt(map, col - 1, row) == 'p' && MapAt(map, col + 1, row) == 'p') c = 'p';
        if (theme == Theme.Home)
            return "_WSR".IndexOf(c) >= 0 ? "BathTile" : "WoodFloor";
        if (c == 'p') return "Puddle";
        if (theme == Theme.Dungeon)
            return roll < 65 ? "Floor_0" : roll < 85 ? "Floor_1" : "Floor_2";
        if (c == '=' || c == 'X') return roll < 80 ? "Path" : "Path_1"; // now and then, a stone
        if (c == 'K') return "Floor_0"; // the castle courtyard is paved
        // Large, quiet patches: smooth Perlin noise across the map rather than a random pick per
        // tile, so neighbouring tiles usually match and the ground reads as calm areas of green.
        float patch = Mathf.PerlinNoise(col * 0.16f + 3.7f, row * 0.16f + 9.1f);
        return patch < 0.4f ? "Grass_1" : patch > 0.62f ? "Grass_2" : "Grass_0";
    }

    private static void SetUpLighting(Theme theme)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        // Outdoors: tuned so the tops of things (sun 0.7 x 0.77 + ambient) come out at about 1x,
        // showing the palette as drawn; walls and cliffs facing the camera fall into cooler shade.
        RenderSettings.ambientLight = theme == Theme.Outdoor ? new Color(0.46f, 0.47f, 0.53f)
                                    : theme == Theme.Home ? new Color(0.32f, 0.27f, 0.24f) // warm, cozy
                                    : new Color(0.18f, 0.18f, 0.24f);

        var sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = theme == Theme.Outdoor ? 0.7f : theme == Theme.Home ? 0.35f : 0.6f;
        sun.color = theme == Theme.Outdoor ? new Color(1f, 0.95f, 0.86f)
                  : theme == Theme.Home ? new Color(1f, 0.85f, 0.7f) : Color.white;
        sun.shadows = LightShadows.Hard; // crisp edges, like the pixel art
        sun.shadowStrength = theme == Theme.Outdoor ? 0.45f : 1f; // softer, friendlier shadows outside
        sun.transform.rotation = theme == Theme.Outdoor ? ArtStyle.OutdoorSun : ArtStyle.IndoorSun; // blob shadows lean the same way
    }

    private static char MapAt(string[] map, int col, int row) =>
        row >= 0 && row < map.Length && col >= 0 && col < map[row].Length ? map[row][col] : ' ';

    private static bool IsFloor(char c) => c != ' ' && !LevelMap.IsWall(c);

    // A torch hangs on the face of its wall block that looks into a room *and* toward the
    // camera: the south face (floor below it on the map) or else the west face.
    private static void PlaceTorch(GameObject prefab, Transform parent, string[] map, int col, int row, Vector3 wallPos)
    {
        Vector3 outward = IsFloor(MapAt(map, col, row + 1)) ? Vector3.back
                        : IsFloor(MapAt(map, col - 1, row)) ? Vector3.left
                        : Vector3.zero;
        if (outward == Vector3.zero)
        {
            Debug.LogWarning($"[DungeonBuilder] Torch at row {row}, col {col} has no visible face; skipped.");
            return;
        }
        Place(prefab, parent, wallPos + outward * (Tile * 0.5f + 0.3f) + Vector3.up * 0.37f);
    }

    // A few tufts scattered around the tile. rng is the tile's own seeded random, so the
    // layout is the same on every rebuild.
    // variants: [plain, plain, flowery]. Flowers are kept for points of interest (';' tiles).
    private static void PlaceGrass(GameObject[] variants, Transform parent, System.Random rng, Vector3 tilePos, bool flowers)
    {
        int count = 1 + rng.Next(2); // just one or two tufts: texture without clutter
        for (int i = 0; i < count; i++)
        {
            float dx = (float)(rng.NextDouble() - 0.5) * Tile * 0.8f;
            float dz = (float)(rng.NextDouble() - 0.5) * Tile * 0.8f;
            var variant = flowers ? variants[2] : variants[rng.Next(2)];
            var g = Place(variant, parent, tilePos + new Vector3(dx, 0.01f, dz));
            g.GetComponent<SpriteRenderer>().flipX = rng.Next(2) == 0;
        }
    }

    private static IsoCameraFollow CreateCamera(Theme theme)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        // What you see past the edge of the map: night-dark indoors, a soft sky around the
        // floating island outside.
        cam.backgroundColor = theme == Theme.Outdoor ? ArtStyle.Sky : ArtStyle.Night;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
        return camGo.AddComponent<IsoCameraFollow>();
    }

    // The exit: stairs with a spinning green crystal above them. While locked, the crystal is
    // hidden; once open it glows, sparkles, and plays a cheerful chime.
    private static ExitZone CreateExit(Vector3 pos, Material mat, LevelSpec spec, SpriteSheetImporter.SpriteSheet props)
    {
        var exit = new GameObject("Exit");
        exit.transform.position = pos + Vector3.up;
        var zone = exit.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(2f, 2f, 2f);

        var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = "Crystal";
        Object.DestroyImmediate(crystal.GetComponent<BoxCollider>());
        crystal.transform.SetParent(exit.transform, false);
        crystal.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        crystal.transform.localScale = Vector3.one * 0.8f;
        crystal.GetComponent<Renderer>().sharedMaterial = mat;

        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(crystal.transform, false);
        light.type = LightType.Point;
        light.color = new Color(0.3f, 1f, 0.4f);
        light.range = 6f;
        light.intensity = 2.4f;
        var pulse = light.gameObject.AddComponent<FlickerLight>(); // a gentle pulsing glow
        SetFloat(pulse, "baseIntensity", 2.4f);
        SetFloat(pulse, "flickerAmount", 0.8f);
        SetFloat(pulse, "speed", 2.5f);

        // Looping sparkles around the crystal (only seen once it's open, like the crystal itself).
        var sparkles = new GameObject("Sparkles");
        sparkles.transform.SetParent(crystal.transform, false);
        sparkles.AddComponent<SpriteRenderer>();
        var flipbook = sparkles.AddComponent<SpriteFlipbook>();
        SetFlipbook(flipbook, props, "Sparkle", destroyWhenDone: false);
        SetBool(flipbook, "loop", true);
        sparkles.AddComponent<Billboard>();

        var exitZone = exit.AddComponent<ExitZone>();
        SetString(exitZone, "nextScene", spec.NextScene);
        SetBool(exitZone, "requireAllEnemiesDefeated", spec.ExitNeedsAllEnemiesDefeated);
        SetRef(exitZone, "visual", crystal);
        SetRef(exitZone, "openSound", Sound("stairs_open"));
        return exitZone;
    }

    // ---------- HUD ----------

    // A UIDocument shows a UXML layout on screen. PanelSettings controls how that UI is
    // scaled: here it's designed at 1280x720 and scaled up or down to fit the window.
    private static PanelSettings HudPanelSettings()
    {
        foreach (var image in new[]
                 {
                     "Frame", "Panel", "Slot", "MapCrown", "MapStairs", "MapStairsLocked",
                     "MapMonster", "MapChest", "MapDragon",
                 })
            PixelTexture(image, "UI"); // point filtering keeps the pixel art crisp when scaled
        foreach (var image in new[]
                 {
                     "PanelCream", "PanelStar", "StitchRule", "StarBurst", "MinimapRing",
                     "Heart", "HeartHalf", "HeartEmpty", "IconMagic", "IconCoin",
                     "IconMonster", "PipMonster", "PipStar", "HurtVignette",
                 })
            SmoothTexture(image); // drawn at 4x, shrunk smoothly

        var panel = LoadOrCreateAsset<PanelSettings>("Assets/UI/HudPanelSettings.asset");
        panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/HudTheme.tss");
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1280, 720);
        panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panel.match = 0.5f;
        EditorUtility.SetDirty(panel);
        return panel;
    }

    private static (HudController, Minimap) CreateHud(LevelMap map)
    {
        var hud = new GameObject("HUD");
        var doc = hud.AddComponent<UIDocument>();
        doc.panelSettings = HudPanelSettings();
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Hud.uxml");

        var minimap = hud.AddComponent<Minimap>();
        SetRef(minimap, "map", map);
        var controller = hud.AddComponent<HudController>();
        SetRef(controller, "clickSound", Sound("ui_select"));
        SetRef(hud.AddComponent<SkillTreeView>(), "learnSound", Sound("skill_learn"));
        SetRef(hud.AddComponent<PauseMenu>(), "clickSound", Sound("ui_select"));
        var dialogue = hud.AddComponent<DialogueController>();
        SetRef(dialogue, "npcVoice", Sound("voice_dragon"));
        SetRef(dialogue, "heroVoice", Sound("voice_hero"));
        return (controller, minimap);
    }
}
