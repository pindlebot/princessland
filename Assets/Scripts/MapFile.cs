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
//   <symbol> = spawn <Name>             an extra named arrival spot
//
// Lines starting with "//" are comments (in the header and legend; the map is taken as is).
// This is plain C# with no Unity editor code, so both DungeonBuilder and the tests can use it.
public class MapFile
{
    // Every symbol that means something without a legend entry. Keep in sync with
    // DungeonBuilder.BuildLevel's switch.
    public const string BuiltInTiles = " .,=_#THKPELMCIDBWSRYFbQ*;Xwmfcoxjupdkyn";

    public class LegendEntry
    {
        public char Symbol;
        public string Kind;      // "door", "castle" or "spawn"
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
            if (entry.Kind != "door" && entry.Kind != "castle") continue;
            if (entry.Args.Length == 0) continue; // the validator reports it
            string target = entry.Args[0];
            string spawn = entry.Args.Length > 1 ? entry.Args[1] : SpawnNameFor(Name);
            var tiles = entry.Kind == "castle" ? Find('K').Take(1) : Find(entry.Symbol);
            foreach (var (col, row) in tiles)
                yield return new Door { Symbol = entry.Symbol, Col = col, Row = row, TargetScene = target, TargetSpawn = spawn, IsCastle = entry.Kind == "castle" };
        }
    }

    // The named arrival spots this level has: one beside each door, plus any "spawn" entries.
    public IEnumerable<string> SpawnNames()
    {
        foreach (var door in Doors()) yield return SpawnNameFor(door.TargetScene);
        foreach (var entry in Legend.Values)
            if (entry.Kind == "spawn" && entry.Args.Length > 0) yield return entry.Args[0];
    }
}
