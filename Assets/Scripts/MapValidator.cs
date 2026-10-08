using System.Collections.Generic;
using System.Linq;

// Checks a set of map files against each other: every door leads to a level that exists and
// has the arrival spot the door names, every symbol on a map means something, and so on.
// DungeonBuilder refuses to build with errors, and a Play Mode test runs it on Assets/Levels,
// so a typo in a door can't sneak into the game.
public static class MapValidator
{
    private static readonly string[] Themes = { "Dungeon", "Outdoor", "Home" };

    // Returns a list of problems (empty = all good). Each one names the file it's in.
    public static List<string> Validate(IList<MapFile> maps)
    {
        var errors = new List<string>();
        var byName = maps.ToDictionary(m => m.Name);

        foreach (var map in maps)
        {
            void Error(string message) => errors.Add($"{map.Name}: {message}");

            if (string.IsNullOrEmpty(map.Get("title"))) Error("the header needs a 'title'");
            string theme = map.Get("theme");
            if (!Themes.Contains(theme)) Error($"theme '{theme}' should be one of {string.Join(", ", Themes)}");

            int starts = map.Find('P').Count();
            if (starts != 1) Error($"needs exactly one 'P' (player start), found {starts}");

            // Every symbol on the map is a built-in tile or in the legend.
            var unknown = map.Rows.SelectMany(r => r).Distinct()
                .Where(c => MapFile.BuiltInTiles.IndexOf(c) < 0 && !map.Legend.ContainsKey(c));
            foreach (char c in unknown) Error($"'{c}' is on the map but isn't a built-in tile or in the legend");

            foreach (var entry in map.Legend.Values)
            {
                string where = $"legend line {entry.Line} ('{entry.Symbol}')";
                switch (entry.Kind)
                {
                    case "door":
                        if (MapFile.BuiltInTiles.IndexOf(entry.Symbol) >= 0)
                            Error($"{where}: '{entry.Symbol}' is already a built-in tile; pick another symbol (digits are good)");
                        else if (!map.Find(entry.Symbol).Any())
                            Error($"{where}: the door isn't on the map");
                        if (entry.Args.Length < 1 || entry.Args.Length > 2) Error($"{where}: write 'door <Scene> [<Spawn>]'");
                        break;
                    case "castle":
                        if (entry.Symbol != 'K') Error($"{where}: the castle is always 'K'");
                        else if (!map.Find('K').Any()) Error($"{where}: there are no 'K' castle tiles on the map");
                        if (entry.Args.Length < 1 || entry.Args.Length > 2) Error($"{where}: write 'K = castle <Scene> [<Spawn>]'");
                        break;
                    case "spawn":
                        if (entry.Args.Length != 1) Error($"{where}: write 'spawn <Name>'");
                        if (!map.Find(entry.Symbol).Any()) Error($"{where}: the spawn isn't on the map");
                        break;
                    default:
                        Error($"{where}: unknown kind '{entry.Kind}' (door, castle or spawn)");
                        break;
                }
            }

            // Doors: the other side exists and has somewhere to arrive.
            foreach (var door in map.Doors())
            {
                string what = door.IsCastle ? "the castle gate" : $"door '{door.Symbol}' at ({door.Col},{door.Row})";
                if (!byName.TryGetValue(door.TargetScene, out var target))
                {
                    Error($"{what} leads to '{door.TargetScene}', which has no map file");
                    continue;
                }
                if (!target.SpawnNames().Contains(door.TargetSpawn))
                    Error($"{what} arrives at '{door.TargetSpawn}' in {door.TargetScene}, which has no such spot " +
                          $"(add a door back to {map.Name} there, or '<symbol> = spawn {door.TargetSpawn}')");
                if (!door.IsCastle && !HasFloorBeside(map, door.Col, door.Row))
                    Error($"{what} has no floor next to it to arrive on");
            }

            // The stairs.
            string exit = map.Get("exit");
            if (exit.Length > 0 && !byName.ContainsKey(exit)) Error($"the exit leads to '{exit}', which has no map file");
            if (exit.Length > 0 && !map.Find('X').Any()) Error($"'exit: {exit}' is set but there's no 'X' on the map");
        }
        return errors;
    }

    // Is one of the four neighbours walkable ground (where the arrival spot goes)?
    // Lava and spikes don't count: nobody should arrive standing in them.
    public static bool HasFloorBeside(MapFile map, int col, int row) => FloorBeside(map, col, row).HasValue;

    // The first walkable neighbour, looking north, south, west, then east. Null if none.
    public static (int col, int row)? FloorBeside(MapFile map, int col, int row)
    {
        foreach (var (dc, dr) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
        {
            char c = map.At(col + dc, row + dr);
            bool walkable = c != ' ' && !LevelMap.IsWall(c) && !LevelMap.IsHazard(c) && !IsDoor(map, c);
            if (walkable) return (col + dc, row + dr);
        }
        return null;
    }

    private static bool IsDoor(MapFile map, char c) => map.Legend.TryGetValue(c, out var e) && e.Kind == "door";
}
