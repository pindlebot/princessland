using System;
using System.Collections.Generic;
using System.Linq;

// One level, as written in a text file under Assets/Levels/ (the file name is the scene name).
// Three parts separated by lines of "---":
//
//   title: The Castle Grounds        a header of "key: value" lines (see the keys below)
//   theme: Outdoor
//   ---
//   HHHHHHHHHH                        the map, one character per tile (DungeonBuilder.Levels.cs
//   H.P....1.H                        lists the built-in tiles: walls, floors, monsters, ...)
//   HHHHHHHHHH
//   ---
//   1 = door House                    a legend for everything else: what each extra symbol means
//
// Legend entries:
//   <symbol> = door <Scene> [<Spawn>]   a door to another scene. You arrive at <Spawn> there,
//                                       which defaults to "From<this scene>". Every door also makes
//                                       an arrival spot beside itself named "From<Scene>", so two
//                                       doors that lead to each other need nothing else.
//   K = castle <Scene> [<Spawn>]        the castle's gate leads to <Scene> (same rules as a door)
//   <symbol> = stairsdown <Scene> [<Spawn>]   a spiral staircase, down or up, to another scene
//   <symbol> = stairsup <Scene> [<Spawn>]     (works exactly like a door)
//   <symbol> = gate <Scene> [<Spawn>]   a farm gate to another scene (a door, outdoors, on a path)
//   <symbol> = edge <Scene> [<Spawn>]   an opening at the room's edge: walk onto it (no button) and you cross into the
//                                       neighbouring room, with a fade (RoomEdge). Put it on a floor tile in the border.
//   <symbol> = spawn <Name>             an extra named arrival spot
//   <symbol> = item <id> [hidden [braziers]]   an item lying there to pick up (ids: DungeonBuilder.Items.cs).
//                                       'I' with no legend entry is always the Ember Ring. "hidden" keeps it
//                                       out of sight until the level's boss is beaten (the Fairy Lantern, the
//                                       Slime King's Amethyst); "hidden braziers" until every brazier is lit.
//   <symbol> = building <kind>          a filled rectangle of this symbol is a building (BuildingKinds);
//                                       its door is in the middle of its south side
//   <symbol> = prop <kind>              a village prop standing there (PropKinds): a well, a hen, ...
//                                       The gates (DungeonBuilder.Gates.cs): gap (a pit tile: hop it with the Bouncy Boots),
//                                       bramble and brazier (spells), heartpiece and starshard (walk into them).
//   <symbol> = npc <id>                 a friendly character standing there (NpcIds)
//
// Header keys worth knowing about (the rest are in DungeonBuilder.Levels.cs):
//   mood: dusk | woods | dark         the light: Hollow Farm's dusk, the Whispering Woods' green dapple, a dark hollow
//   world: <x> <y>                    where the room's top-left corner sits on the world map (M), in tiles. Rooms
//                                     without it aren't on the map.
//   exit_spawn: <Name>                where to arrive through the stairs (X), a named spot in the exit's level
//   grey_until: <condition>           the level is drained of colour until it holds, e.g. has:amethyst (Condition.cs)
//   monsters: E=gourdling L=strawman M=pumpkinking
//       which monster each of the map's monster markers stands for on this level. Without it, E is a
//       skeleton, L a slime, J a pirate and M the Slime King (MonsterIds lists the others). M is always
//       the level's boss: the exit (if there is one) stays sealed until it's dead.
//
// Lines starting with "//" are comments (in the header and legend; the map is taken as is).
// This is plain C# with no Unity editor code, so both DungeonBuilder and the tests can use it.
public class MapFile
{
    // Every symbol that means something without a legend entry. Keep in sync with
    // DungeonBuilder.BuildLevel's switch.
    public const string BuiltInTiles = " .,=_#THKPELMCIDBWSRYFbQ*;Xwmfcoxjupdkyn~^ANUGvrtiaOszlVe%:hqJ&|@Z$+-/";

    // What "building", "prop" and "npc" entries can name. Keep in sync with DungeonBuilder.Town.cs
    // (and DungeonBuilder.Home.cs for the cat and the kitchen, DungeonBuilder.Bath.cs for the
    // bathtub and its plants).
    public static readonly string[] BuildingKinds = { "cathedral", "shop", "cottage", "barn" };
    public static readonly string[] PropKinds =
    {
        "stall", "well", "coop", "grainsack", "lamppost", "noticeboard", "bench", "planter",
        "hen", "brownhen", "chick", "grain", "bathtub", "cat", "crib",
        "wishingwell", "lockeddoor", "stove", "pantry", "island", "towels", "pottedfern", "pottedmonstera",
        "scarecrow", "pumpkin", "jackolantern", "corn", "deadtree", "haystack", "gravestone", "crow", "ghost",
        "wisp", "mist", "signpost",
        "ciderstand", "pumpkinstack", "bunting", "festivalarch", "bobbingtub", "giantpumpkin", "cornwall",
        "pot", "sleepytree", "giantmushroom", "glowcaps",
        "gap", "bramble", "brazier", "heartpiece", "starshard",
        "fakewall", "workshopdesk", "workshopnote",
    };

    // The monster markers on a map (E, L, J and M), and what each one means unless the level's
    // "monsters:" header says otherwise. Keep in sync with DungeonBuilder.Monsters.cs.
    public const string MonsterMarkers = "ELJM";
    public static readonly string[] MonsterIds =
        { "skeleton", "slime", "pirate", "slimeking", "gourdling", "strawman", "pumpkinking", "piratecaptain", "sporepuff", "mothermushroom" };
    private static readonly Dictionary<char, string> DefaultMonsters =
        new Dictionary<char, string> { { 'E', "skeleton" }, { 'L', "slime" }, { 'J', "pirate" }, { 'M', "slimeking" } };

    // Legend kinds that lead to another scene: a door, or a spiral staircase.
    public static bool IsDoorKind(string kind) => kind == "door" || kind == "stairsdown" || kind == "stairsup" || kind == "gate" || kind == "edge";
    public static readonly string[] NpcIds = { "barnaby", "stitches", "pippin", "oldmoss" };

    // Props that are walls (solid, drawn as walls on the minimap): the corn maze.
    public static readonly string[] WallPropKinds = { "cornwall" };

    public class LegendEntry
    {
        public char Symbol;
        public string Kind;      // "door", "stairsdown", "stairsup", "gate", "edge", "castle", "spawn", "item", "building", "prop" or "npc"
        public string[] Args;
        public int Line;         // 1-based line in the file, for error messages
    }

    // A door (or the castle gate) found on the map.
    public class Door
    {
        public char Symbol;
        public int Col, Row;
        public string TargetScene, TargetSpawn;
        public bool IsCastle;
    }

    public string Name { get; private set; }
    public string[] Rows { get; private set; }
    public readonly Dictionary<string, string> Header = new Dictionary<string, string>();
    public readonly Dictionary<char, LegendEntry> Legend = new Dictionary<char, LegendEntry>();

    // Header values, with the defaults a level gets when a key is left out.
    public string Get(string key, string fallback = "") =>
        Header.TryGetValue(key, out var value) && value.Length > 0 ? value : fallback;

    public static string SpawnNameFor(string fromScene) => "From" + fromScene;

    public static MapFile Parse(string name, string text)
    {
        var file = new MapFile { Name = name };
        var lines = text.Replace("\r", "").Split('\n');
        var sections = new List<List<(string text, int line)>> { new List<(string, int)>() };
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---") sections.Add(new List<(string, int)>());
            else sections[sections.Count - 1].Add((lines[i], i + 1));
        }
        if (sections.Count != 3)
            throw new FormatException($"{name}: expected a header, a map and a legend separated by two '---' lines, found {sections.Count} part(s)");

        foreach (var (line, number) in sections[0])
        {
            if (IsBlankOrComment(line)) continue;
            int colon = line.IndexOf(':');
            if (colon <= 0) throw new FormatException($"{name}:{number}: expected 'key: value', got '{line}'");
            file.Header[line.Substring(0, colon).Trim().ToLowerInvariant()] = line.Substring(colon + 1).Trim();
        }

        // The map keeps its leading spaces (they're "nothing" tiles); blank lines around it are dropped.
        var rows = sections[1].Select(l => l.text.TrimEnd()).ToList();
        while (rows.Count > 0 && rows[0].Length == 0) rows.RemoveAt(0);
        while (rows.Count > 0 && rows[rows.Count - 1].Length == 0) rows.RemoveAt(rows.Count - 1);
        if (rows.Count == 0) throw new FormatException($"{name}: the map is empty");
        file.Rows = rows.ToArray();

        foreach (var (line, number) in sections[2])
        {
            if (IsBlankOrComment(line)) continue;
            int equals = line.IndexOf('=');
            string symbol = equals > 0 ? line.Substring(0, equals).Trim() : "";
            var words = equals > 0 ? line.Substring(equals + 1).Split((char[])null, StringSplitOptions.RemoveEmptyEntries) : new string[0];
            if (symbol.Length != 1 || words.Length == 0)
                throw new FormatException($"{name}:{number}: expected '<symbol> = <kind> ...', got '{line}'");
            char c = symbol[0];
            if (file.Legend.ContainsKey(c))
                throw new FormatException($"{name}:{number}: '{c}' is already in the legend");
            file.Legend[c] = new LegendEntry { Symbol = c, Kind = words[0].ToLowerInvariant(), Args = words.Skip(1).ToArray(), Line = number };
        }
        return file;
    }

    private static bool IsBlankOrComment(string line)
    {
        var t = line.Trim();
        return t.Length == 0 || t.StartsWith("//");
    }

    public char At(int col, int row) =>
        row >= 0 && row < Rows.Length && col >= 0 && col < Rows[row].Length ? Rows[row][col] : ' ';

    // Where a symbol appears on the map, as (col, row) pairs.
    public IEnumerable<(int col, int row)> Find(char symbol)
    {
        for (int row = 0; row < Rows.Length; row++)
            for (int col = 0; col < Rows[row].Length; col++)
                if (Rows[row][col] == symbol) yield return (col, row);
    }

    // Every door and castle gate, one per tile (a door symbol may appear more than once).
    public IEnumerable<Door> Doors()
    {
        foreach (var entry in Legend.Values)
        {
            if (!IsDoorKind(entry.Kind) && entry.Kind != "castle") continue;
            if (entry.Args.Length == 0) continue; // the validator reports it
            string target = entry.Args[0];
            string spawn = entry.Args.Length > 1 ? entry.Args[1] : SpawnNameFor(Name);
            var tiles = entry.Kind == "castle" ? Find('K').Take(1) : Find(entry.Symbol);
            foreach (var (col, row) in tiles)
                yield return new Door { Symbol = entry.Symbol, Col = col, Row = row, TargetScene = target, TargetSpawn = spawn, IsCastle = entry.Kind == "castle" };
        }
    }

    // The "monsters:" header as (marker, monster id) pairs, e.g. "E=gourdling L=strawman". Problems (a
    // marker that isn't E, L, J or M, an unknown monster) come back as errors for the validator.
    public IEnumerable<(string marker, string id)> MonsterOverrides()
    {
        foreach (var word in Get("monsters").Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
        {
            int equals = word.IndexOf('=');
            yield return equals < 0 ? (word, "") : (word.Substring(0, equals), word.Substring(equals + 1).ToLowerInvariant());
        }
    }

    // The monster that a marker (E, L, J or M) places on this level.
    public string MonsterFor(char marker)
    {
        foreach (var (m, id) in MonsterOverrides())
            if (m.Length == 1 && m[0] == marker) return id;
        return DefaultMonsters[marker];
    }

    // Is this symbol one of the map's buildings (solid, like a wall)?
    public bool IsBuilding(char c) => Legend.TryGetValue(c, out var e) && e.Kind == "building";

    // Is this symbol a wall-like prop (a corn maze wall)?
    public bool IsWallProp(char c) =>
        Legend.TryGetValue(c, out var e) && e.Kind == "prop" && e.Args.Length > 0 && WallPropKinds.Contains(e.Args[0]);

    // Is this symbol a fake wall (looks like a wall, walks like air: a secret room's door)?
    public bool IsFakeWall(char c) =>
        Legend.TryGetValue(c, out var e) && e.Kind == "prop" && e.Args.Length > 0 && e.Args[0] == "fakewall";

    public string FakeWallSymbols => new string(Legend.Values.Where(e => IsFakeWall(e.Symbol)).Select(e => e.Symbol).ToArray());

    // The tiles of the secret room(s) behind the fake walls: walkable tiles that can only be reached through
    // one, i.e. not connected to the player's start once the fake walls count as solid.
    public List<(int col, int row)> SecretTiles()
    {
        var result = new List<(int, int)>();
        var starts = Find('P').ToList();
        if (starts.Count == 0 || FakeWallSymbols.Length == 0) return result;
        bool Open(int c, int r)
        {
            char t = At(c, r);
            return t != ' ' && !LevelMapIsWall(t) && !IsBuilding(t) && !IsWallProp(t) && !IsFakeWall(t);
        }
        var seen = new HashSet<(int, int)>();
        var queue = new Queue<(int, int)>();
        queue.Enqueue(starts[0]);
        seen.Add(starts[0]);
        while (queue.Count > 0)
        {
            var (c, r) = queue.Dequeue();
            foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (Open(c + dc, r + dr) && seen.Add((c + dc, r + dr))) queue.Enqueue((c + dc, r + dr));
        }
        // Flood from each fake wall's neighbours that the player can't reach otherwise.
        foreach (var (fc, fr) in Rows.SelectMany((row, r) => row.Select((ch, c) => (ch, c, r))).Where(t => IsFakeWall(t.ch)).Select(t => (t.c, t.r)))
        {
            foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var start = (fc + dc, fr + dr);
                if (!Open(start.Item1, start.Item2) || seen.Contains(start)) continue;
                var room = new Queue<(int, int)>();
                room.Enqueue(start);
                seen.Add(start);
                while (room.Count > 0)
                {
                    var (c, r) = room.Dequeue();
                    result.Add((c, r));
                    foreach (var (ec, er) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        if (Open(c + ec, r + er) && seen.Add((c + ec, r + er))) room.Enqueue((c + ec, r + er));
                }
            }
        }
        return result;
    }

    // MapFile has no Unity code, so it asks the same question LevelMap.IsWall answers.
    private static bool LevelMapIsWall(char c) => c == '#' || c == 'T' || c == 'H' || c == 'K' || c == '%' || c == '|' || c == '+';

    // Every wall-like prop symbol on this map.
    public string WallSymbols => new string(Legend.Values.Where(e => IsWallProp(e.Symbol)).Select(e => e.Symbol).ToArray());

    // Every building symbol on this map, e.g. "345".
    public string BuildingSymbols => new string(Legend.Values.Where(e => e.Kind == "building").Select(e => e.Symbol).ToArray());

    // The named arrival spots this level has: one beside each door, plus any "spawn" entries.
    public IEnumerable<string> SpawnNames()
    {
        foreach (var door in Doors()) yield return SpawnNameFor(door.TargetScene);
        foreach (var entry in Legend.Values)
            if (entry.Kind == "spawn" && entry.Args.Length > 0) yield return entry.Args[0];
    }
}
