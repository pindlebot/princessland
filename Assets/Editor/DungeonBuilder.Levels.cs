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
    //   Legend   doors, the castle gate, named arrival spots and items are defined per file (MapFile.cs)
    //   Scenery  Y  tree    F  fountain (make a wish!)    b  bush    Q  banner    *  butterflies    ;  flowers
    //   Pond     w  water (not walkable)    m  Coralie the mermaid, in the water    f  the bush hiding the frog
    //   Dungeon  c  campfire (warm up: full health)    n  Bonesy, the friendly skeleton    o  barrel    x  crate
    //            j  bones    u  glowing mushrooms    p  puddle    d  wooden door    k  locked door    y  the Rusty Key
    //   Woods    t  pine    i  birch    a  autumn tree    O  boulder    z  stump    l  log    V  tent
    //            s  pebbles    e  fern    (and the dungeon's c campfire works outdoors too)
    //   Cave     %  rock (a crag, or a low rock where a crag would hide what's behind it)    :  cave floor
    //            (trees, boulders, stumps, logs, the tent and the fountain are solid, so keep them off the walking routes)
    //   Hazards  ~  lava (hurts while you stand in it)    ^  spike trap (hurts while its spikes are up)
    //   Cove     J  pirate    &  dark mermaid, in the water (throws bolts)    Z  Pearl, in the water
    //            q  palm    @  the pirates' ship, on the water    $  treasure heap    h  wooden planks (jetties, bridges)
    //            |  rock with a waterfall pouring down its front (put water below it)
    //   Village  +  white picket fence (solid)    -  cobbles
    //   Farm     /  tilled soil (pumpkin patches); "mood: dusk" in the header lights it at dusk
    //            (buildings, props and Barnaby come from the legend: see DungeonBuilder.Town.cs)
    //            (header "water: sea" makes 'w' the sea; "ground: sand" makes '.' and the markers beach sand.
    //             On outdoor levels, water on the map's edge spills off the island, and doors are rowboats.)
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
        public bool Sea, SandGround;       // "water: sea", "ground: sand" (Mermaid Cove)
        public string Floor;               // "floor: kitchen": indoors, '.' is tiled (the kitchen)
        public bool Dusk;                  // "mood: dusk": low orange sun, a violet sky (Hollow Farm)
    }

    private const string LevelsFolder = "Assets/Levels";

    // Every Assets/Levels/*.txt file, parsed and checked. Throws (building nothing) if any has a mistake.
    private static List<MapFile> LoadMaps()
    {
        var maps = Directory.GetFiles(LevelsFolder, "*.txt").OrderBy(p => p)
            .Select(p => MapFile.Parse(Path.GetFileNameWithoutExtension(p), File.ReadAllText(p)))
            .ToList();
        var errors = MapValidator.Validate(maps, ItemSpecs.Select(i => i.Id).ToList());
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
            Sea = file.Get("water") == "sea",
            SandGround = file.Get("ground") == "sand",
            Floor = file.Get("floor"),
            Dusk = file.Get("mood") == "dusk",
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
        SetUpLighting(spec.Theme, spec.Dusk);

        var level = new GameObject("Level").transform;
        // Save the layout into the scene too, so runtime code (the minimap) can read it.
        var levelMap = level.gameObject.AddComponent<LevelMap>();
        SetStrings(levelMap, "rows", map);
        SetColor(levelMap, "floorColor", spec.MinimapFloor);
        SetColor(levelMap, "wallColor", spec.MinimapWall);
        SetString(levelMap, "buildings", spec.File.BuildingSymbols);
        SetString(levelMap, "walls", spec.File.WallSymbols);

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

                // On the island's rim: water spills over the edge, everything else gets the earthy
                // cliff under it (hedges add their own).
                if (spec.Theme == Theme.Outdoor && c != 'H' && OnMapEdge(map, col, row))
                {
                    if (LevelMap.IsWater(c)) AddEdgeFall(level, map, col, row, pos, mats);
                    else AddCliff(level, pos, mats["EarthSide"]);
                }

                if (c == '#' || c == 'T')
                {
                    // Ivy climbs the home courtyard's walls (any wall beside its grass or hedge).
                    bool ivy = spec.Theme == Theme.Home && new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }
                        .Any(d => ",;H".IndexOf(MapAt(map, col + d.Item1, row + d.Item2)) >= 0);
                    var wall = Block("Wall", level, pos + Vector3.up * WallHeight * 0.5f,
                                     new Vector3(Tile, WallHeight, Tile), mats[ivy ? "WallSideIvy" : "WallSide"]);
                    AddCap(wall, mats["WallTop"]);
                    if (c == 'T') PlaceTorch(assets.Torch, decor, map, col, row, pos);
                    continue;
                }
                if (c == '%' || c == '|')
                {
                    BuildRock(level, map, col, row, pos, mats, tall: c == '|');
                    if (c == '|') AddWaterfall(level, decor, assets, pos);
                    continue;
                }
                if (c == 'H')
                {
                    var hedge = Block("Hedge", level, pos + Vector3.up * HedgeHeight * 0.5f,
                                      new Vector3(Tile, HedgeHeight, Tile), mats["HedgeSide"]);
                    AddCap(hedge, mats["HedgeTop"]);
                    if (spec.Theme == Theme.Outdoor) AddCliff(level, pos, mats["EarthSide"]); // the floating island's earthy edge
                    continue;
                }

                if (LevelMap.IsWater(c))
                {
                    var friend = c == 'm' ? assets.Mermaid : c == 'Z' ? assets.Pearl : null;
                    BuildWater(level, decor, assets, map, col, row, pos, rng, friend, spec.Sea);
                    if (c == '&') Place(assets.DarkMermaid, enemies, pos + Vector3.up);
                    if (c == '@') Place(assets.Ship, decor, pos);
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

                bool isDoor = spec.File.Legend.TryGetValue(c, out var legendEntry) && MapFile.IsDoorKind(legendEntry.Kind);
                var floor = Block("Floor", level, pos + Vector3.down * 0.25f, new Vector3(Tile, 0.5f, Tile),
                                  mats[FloorMaterial(spec, map, c, col, row, rng.Next(100), isDoor)]);
                // Turning tiles in 90° steps hides the repetition of only three textures.
                floor.transform.rotation = Quaternion.Euler(0f, 90f * rng.Next(4), 0f);

                switch (c)
                {
                    case 'P': spawn.position = pos + Vector3.up; break;
                    case 'E':
                    case 'L':
                    case 'J':
                        var e = Place(c == 'E' ? assets.Skeleton : c == 'L' ? assets.Slime : assets.Pirate, enemies, pos + Vector3.up);
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
                        SetString(Place(assets.ItemPickups[EmberRingId], decor, pos).GetComponent<ItemPickup>(), "persistentId", $"{sceneName}/{col},{row}");
                        break;
                    case 'D': Place(assets.Dragon, decor, pos); break;
                    case 'B': Place(assets.Bed, decor, pos + Vector3.right); break; // the bed is ~1.5 tiles wide
                    case 'W': Place(assets.Toilet, decor, pos); break;
                    case 'S': Place(assets.Sink, decor, pos); break;
                    case 'R': Place(assets.PaperTowel, decor, pos); break;
                    case 'A': Place(assets.Wardrobe, decor, pos); break;
                    case 'N': Place(assets.Nightstand, decor, pos); break;
                    case 'U': Place(assets.Bookshelf, decor, pos); break;
                    case 'G': Place(assets.ToyChest, decor, pos); break;
                    case 'v': Place(assets.Plant, decor, pos); break;
                    // Half a tile north, so a rug on the tile in front of the bed lines up with it.
                    case 'r': Place(assets.Rug, decor, pos + new Vector3(0f, 0.01f, Tile / 2f)); break;
                    case ',':
                        if (spec.Dusk) // autumn: fallen leaves instead of tufts of grass
                            Place(assets.PropPrefabs["leaves"], decor, pos + new Vector3(rng.Next(-4, 5) * 0.1f, 0.01f, rng.Next(-4, 5) * 0.1f));
                        else PlaceGrass(assets.Grass, decor, rng, pos, flowers: false);
                        break;
                    case ';': PlaceGrass(assets.Grass, decor, rng, pos, flowers: true); break;
                    case 'Y': Place(assets.Tree, decor, pos); break;
                    case 'F':
                        Place(assets.Fountain, decor, pos);
                        AddMotes(decor, pos, assets.Mote);
                        break;
                    case 'b': Place(assets.Bush, decor, pos); break;
                    case 't': Place(assets.Pine, decor, pos); break;
                    case 'i': Place(assets.Birch, decor, pos); break;
                    case 'a': Place(assets.AutumnTree, decor, pos); break;
                    case 'O': Place(assets.Boulder, decor, pos); break;
                    case 'z': Place(assets.Stump, decor, pos); break;
                    case 'l': Place(assets.Log, decor, pos); break;
                    case 'V': Place(assets.Tent, decor, pos); break;
                    case 's': Place(assets.Stones, decor, pos + new Vector3(rng.Next(-4, 5) * 0.1f, 0f, rng.Next(-4, 5) * 0.1f)); break;
                    case 'e': Place(assets.Fern, decor, pos + new Vector3(rng.Next(-4, 5) * 0.1f, 0f, rng.Next(-4, 5) * 0.1f)); break;
                    case 'Q': Place(assets.Flag, decor, pos); break; // a banner on a pole
                    case 'q': Place(assets.Palm, decor, pos); break;
                    case '$': Place(assets.Treasure, decor, pos); break;
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
                    case '+': BuildFence(level, map, col, row, pos, mats["Picket"]); break;
                }

                // Now and then a shell or a starfish on the beach.
                if (spec.SandGround && c == '.' && rng.Next(18) == 0)
                    Place(rng.Next(2) == 0 ? assets.Shell : assets.Starfish, decor,
                          pos + new Vector3(rng.Next(-5, 6) * 0.1f, 0.01f, rng.Next(-5, 6) * 0.1f));

                // Symbols from the file's legend: doors to other scenes and named arrival spots.
                // Outdoors, a door is a rowboat at the end of a jetty.
                if (spec.File.Legend.TryGetValue(c, out var entry))
                {
                    if (entry.Kind == "spawn")
                        namedSpawns.Add(SpawnPoint(entry.Args[0], pos + Vector3.up, Quaternion.identity));
                    else if (entry.Kind == "item")
                        SetString(Place(assets.ItemPickups[entry.Args[0]], decor, pos).GetComponent<ItemPickup>(),
                                  "persistentId", $"{sceneName}/{col},{row}");
                    else if (MapFile.IsDoorKind(entry.Kind))
                        PlaceDoor(spec.File, entry.Kind == "stairsdown" ? assets.SpiralDown
                                           : entry.Kind == "stairsup" ? assets.SpiralUp
                                           : entry.Kind == "gate" ? assets.FarmGate
                                           : spec.Theme == Theme.Outdoor ? assets.Rowboat : assets.HouseDoor,
                                  decor, namedSpawns, col, row, pos);
                    else if (entry.Kind == "prop" || entry.Kind == "npc")
                        PlaceProp(entry, assets, decor, pos, rng); // buildings are built below, whole
                }
            }
        }

        // A boss guards the exit: it stays sealed until the boss is dead.
        if (boss != null && exit != null)
            SetRef(exit, "guardian", boss);

        if (spec.Theme == Theme.Outdoor)
            AddClouds(decor, map, assets.Cloud);
        BuildBuildings(spec.File, level, assets); // the village's houses (legend "building" entries)
        if (spec.Theme == Theme.Home) AddCourtyardSun(level, map);

        // The castle (only on maps with 'K' tiles) also gives us a spawn point outside its gate.
        var castleDoor = spec.File.Doors().FirstOrDefault(d => d.IsCastle);
        var outsideGate = BuildCastle(map, level, assets, castleDoor);
        if (outsideGate != null) namedSpawns.Add(outsideGate);
        var camera = CreateCamera(spec.Theme, spec.Dusk);
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
        if (prefab.name == "Rowboat") MoorBoat(door.gameObject, -awayFromDoor, info.TargetScene);
        spawns.Add(SpawnPoint(MapFile.SpawnNameFor(info.TargetScene), arrive, Quaternion.LookRotation(awayFromDoor)));
    }

    // A water tile: water a little below the ground, an invisible wall so nobody walks in (on
    // the Water layer, so spells and bolts fly over it), and on 'm' / 'Z' a mermaid friend
    // (talked to from the nearest shore). The pond gets the odd lily pad; the sea, foam
    // lapping at the rocks.
    private static void BuildWater(Transform level, Transform decor, SharedAssets assets, string[] map,
                                   int col, int row, Vector3 pos, System.Random rng, GameObject friend, bool sea)
    {
        var water = Block("Water", level, pos + Vector3.down * 0.45f, new Vector3(Tile, 0.5f, Tile),
                          assets.Materials[sea ? "Sea" : "Water"]);
        water.isStatic = false; // its texture drifts
        water.AddComponent<WaterScroll>();
        var bank = new GameObject("Bank").AddComponent<BoxCollider>();
        bank.gameObject.layer = LevelMap.WaterLayer;
        bank.transform.SetParent(water.transform, false);
        bank.transform.position = pos + Vector3.up;
        bank.size = new Vector3(1f, 4f, 1f); // in the water block's scaled space (2 x 0.5 x 2): a 2m-tall wall
        AddBanks(water.transform.parent, map, col, row, pos, assets.Materials[sea ? "SandBank" : "Bank"]);
        bool open = friend == null && "&@".IndexOf(MapAt(map, col, row)) < 0;
        if (!sea && open && rng.Next(4) == 0) // the odd lily pad, not a carpet of them
            Place(assets.Lily, decor, pos + new Vector3(rng.Next(-5, 6) * 0.1f, -0.18f, rng.Next(-5, 6) * 0.1f));
        if (sea && open && CountAround(map, col, row, '%') + CountAround(map, col, row, '|') > 0 && rng.Next(2) == 0)
            Place(assets.Foam, decor, pos + new Vector3(rng.Next(-3, 4) * 0.1f, -0.18f, rng.Next(-3, 4) * 0.1f));
        if (friend == null) return;

        var coralie = Place(friend, decor, pos + Vector3.down * 0.15f);
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

    private static string FloorMaterial(LevelSpec spec, string[] map, char c, int col, int row, int roll, bool door)
    {
        var theme = spec.Theme;
        // A monster standing in a puddle stands in water too (not on a dry square).
        if ("EL".IndexOf(c) >= 0 && MapAt(map, col - 1, row) == 'p' && MapAt(map, col + 1, row) == 'p') c = 'p';
        if (theme == Theme.Home)
        {
            // The home's courtyard: grass (',' and ';' and anything standing among them), with a
            // path ('=') through its doorway. The bathroom: tiles, under its fixtures too.
            bool lockedDoor = spec.File.Legend.TryGetValue(c, out var prop) && prop.Kind == "prop" && prop.Args[0] == "lockeddoor";
            if (c == '=' || lockedDoor) return "Path"; // the path runs right up to the courtyard's locked door
            if (c == ',' || c == ';' || (c != '.' && CountAround(map, col, row, ',') + CountAround(map, col, row, ';') >= 1))
                return GrassPatch(col, row);
            if ("_WSR".IndexOf(c) >= 0 || (c != '.' && CountAround(map, col, row, '_') >= 2)) return "BathTile";
            return spec.Floor == "kitchen" ? "KitchenTile" : "WoodFloor";
        }
        if (c == 'p') return "Puddle";
        bool gate = spec.File.Legend.TryGetValue(c, out var gateEntry) && gateEntry.Kind == "gate";
        if (gate) return "Path"; // a farm gate stands on the end of a path
        // A prop standing across a path (the festival's arch) keeps the path going under it.
        if (spec.File.Legend.TryGetValue(c, out var across) && across.Kind == "prop" && CountAround(map, col, row, '=') >= 2)
            return roll < 80 ? "Path" : "Path_1";
        if (c == 'h' || (door && theme == Theme.Outdoor)) return "Planks"; // jetties, bridges, a rowboat's mooring
        // Hollow Farm's tilled soil, and anything growing in it (a pumpkin with soil on two sides).
        if (c == '/' || (c != '.' && CountAround(map, col, row, '/') >= 2)) return "Soil";
        // The cave floor, and anything standing on it (Amethyra, mushrooms): a marker with cave floor
        // on two sides is inside the cave too.
        if (c == ':' || (c != '.' && CountAround(map, col, row, ':') >= 2)) return "CaveFloor";
        if (theme == Theme.Dungeon)
            return roll < 65 ? "Floor_0" : roll < 85 ? "Floor_1" : "Floor_2";
        if (c == '=' || c == 'X') return roll < 80 ? "Path" : "Path_1"; // now and then, a stone
        if (c == 'K') return "Floor_0"; // the castle courtyard is paved
        // The village's cobbles, under its buildings, and under anything standing in the square
        // (a prop with cobbles on two sides, like the well or a bench).
        bool town = spec.File.Legend.TryGetValue(c, out var thing) && thing.Kind is "building" or "prop" or "npc";
        if (LevelMap.IsCobbles(c) || (town && (thing.Kind == "building" || CountAround(map, col, row, '-') >= 2)))
            return roll < 85 ? "Cobble_0" : "Cobble_1";
        if (spec.SandGround) return roll < 92 ? "Sand_0" : "Sand_1"; // the beach (now and then, a shell)
        return GrassPatch(col, row);
    }

    // Large, quiet patches: smooth Perlin noise across the map rather than a random pick per
    // tile, so neighbouring tiles usually match and the ground reads as calm areas of green.
    private static string GrassPatch(int col, int row)
    {
        float patch = Mathf.PerlinNoise(col * 0.16f + 3.7f, row * 0.16f + 9.1f);
        return patch < 0.4f ? "Grass_1" : patch > 0.62f ? "Grass_2" : "Grass_0";
    }

    // Indoors the light is low and warm, but the home's courtyard is open to the sky: a broad
    // spotlight straight down over its grass makes it a patch of daylight.
    private static void AddCourtyardSun(Transform parent, string[] map)
    {
        var grass = new List<Vector3>();
        for (int row = 0; row < map.Length; row++)
            for (int col = 0; col < map[row].Length; col++)
                if (map[row][col] == ',' || map[row][col] == ';')
                    grass.Add(new Vector3(col * Tile, 0f, (map.Length - 1 - row) * Tile));
        if (grass.Count == 0) return;
        var min = grass.Aggregate(Vector3.Min);
        var max = grass.Aggregate(Vector3.Max);
        float size = Mathf.Max(max.x - min.x, max.z - min.z) + Tile * 2f;
        const float height = 10f;

        var sun = new GameObject("CourtyardSun").AddComponent<Light>();
        sun.transform.SetParent(parent);
        sun.transform.position = (min + max) / 2f + Vector3.up * height;
        sun.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        sun.type = LightType.Spot;
        sun.spotAngle = 2f * Mathf.Atan(size * 0.6f / height) * Mathf.Rad2Deg;
        sun.innerSpotAngle = sun.spotAngle * 0.8f;
        sun.range = height * 2f;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.intensity = 2.2f;
        sun.shadows = LightShadows.Hard;
        sun.shadowStrength = 0.45f;
    }

    private static void SetUpLighting(Theme theme, bool dusk = false)
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
        if (dusk)
        {
            // Dusk: a cool violet ambient and a low, orange sun, so the ground goes moody and the
            // jack-o'-lanterns' and wisps' own lights carry the scene.
            RenderSettings.ambientLight = new Color(0.33f, 0.29f, 0.45f);
            sun.color = new Color(1f, 0.62f, 0.42f);
            sun.intensity = 0.5f;
            sun.shadowStrength = 0.55f;
        }
    }

    private static int CountAround(string[] map, int col, int row, char c) =>
        new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Count(d => MapAt(map, col + d.Item1, row + d.Item2) == c);

    private static char MapAt(string[] map, int col, int row) =>
        row >= 0 && row < map.Length && col >= 0 && col < map[row].Length ? map[row][col] : ' ';

    private static bool IsFloor(char c) => c != ' ' && !LevelMap.IsWall(c);

    // Is this tile on the rim of the map (next to nothing, or the map's border)?
    private static bool OnMapEdge(string[] map, int col, int row) =>
        new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Any(d => MapAt(map, col + d.Item1, row + d.Item2) == ' ');

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

    private static IsoCameraFollow CreateCamera(Theme theme, bool dusk = false)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        // What you see past the edge of the map: night-dark indoors, a soft sky around the
        // floating island outside.
        cam.backgroundColor = dusk ? ArtStyle.DuskSky : theme == Theme.Outdoor ? ArtStyle.Sky : ArtStyle.Night;
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
        SetRef(controller, "eatSound", Sound("munch"));
        SetRef(hud.AddComponent<SkillTreeView>(), "learnSound", Sound("skill_learn"));
        var cooking = hud.AddComponent<CookingView>(); // the stove's recipe card
        SetRef(cooking, "cookSound", Sound("cook"));
        SetRef(cooking, "missingSound", Sound("door_locked"));
        SetRef(cooking, "closeSound", Sound("ui_select"));
        SetRef(hud.AddComponent<PauseMenu>(), "clickSound", Sound("ui_select"));
        var dialogue = hud.AddComponent<DialogueController>();
        SetRef(dialogue, "npcVoice", Sound("voice_dragon"));
        SetRef(dialogue, "heroVoice", Sound("voice_hero"));
        return (controller, minimap);
    }
}
