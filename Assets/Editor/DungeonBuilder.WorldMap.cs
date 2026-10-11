using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The data behind the world map screen (WorldMapView): where each room sits ("world: x y" in its map file),
// its tiles boiled down to a few kinds of ground, and the places to mark. The maps themselves aren't in the
// game, so this is how the rooms you've visited can be drawn from anywhere.
public static partial class DungeonBuilder
{
    private const string WorldMapPath = "Assets/Items/WorldMap.asset";

    private static void CreateWorldMap(List<MapFile> maps)
    {
        var data = LoadOrCreateAsset<WorldMapData>(WorldMapPath);
        var rooms = new List<WorldMapData.Room>();
        foreach (var file in maps)
        {
            var world = file.Get("world").Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            if (world.Length != 2) continue;
            var spec = SpecFor(file);
            int width = file.Rows.Max(r => r.Length);
            var markers = new List<WorldMapData.Marker>();
            for (int row = 0; row < file.Rows.Length; row++)
            {
                for (int col = 0; col < file.Rows[row].Length; col++)
                {
                    char c = file.Rows[row][col];
                    if (c == 'F') markers.Add(new WorldMapData.Marker { kind = "fountain", col = col, row = row });
                    else if (IsGapTile(file, c))
                        markers.Add(new WorldMapData.Marker { kind = "gap", col = col, row = row, id = $"{file.Name}/{col},{row}" });
                    else if (file.Legend.TryGetValue(c, out var e) && e.Kind == "item" && e.Args[0].StartsWith("dragon_egg"))
                        markers.Add(new WorldMapData.Marker { kind = "egg", col = col, row = row, id = e.Args[0] });
                }
            }
            // The ways out: a mark on each door or room edge, and on the stairs (the 'X' tile) when the room has an exit. The
            // stairs of a room that has to be cleared first are "lockedexit": the map shows them waiting until it is.
            foreach (var door in file.Doors())
                markers.Add(new WorldMapData.Marker { kind = "exit", col = door.Col, row = door.Row, id = door.TargetScene });
            if (!string.IsNullOrEmpty(spec.NextScene))
                foreach (var (col, row) in file.Find('X'))
                    markers.Add(new WorldMapData.Marker
                    {
                        kind = spec.ExitNeedsAllEnemiesDefeated || !string.IsNullOrEmpty(spec.LockedHint) ? "lockedexit" : "exit",
                        col = col, row = row, id = spec.NextScene,
                    });
            rooms.Add(new WorldMapData.Room
            {
                scene = file.Name,
                title = spec.Title,
                x = int.Parse(world[0]),
                y = int.Parse(world[1]),
                width = width,
                height = file.Rows.Length,
                rows = file.Rows.Select(r => new string(r.Select(c => WorldTile(file, c)).ToArray())).ToArray(),
                floorColor = spec.MinimapFloor,
                wallColor = spec.MinimapWall,
                markers = markers.ToArray(),
                secretTiles = file.SecretTiles().Select(t => $"{t.col},{t.row}").ToArray(),
            });
        }
        data.rooms = rooms.ToArray();
        EditorUtility.SetDirty(data);
    }

    // The kind of ground a tile is on the world map (see WorldMapData): a fake wall looks like a wall, a gap (or a chasm) like nothing.
    private static char WorldTile(MapFile file, char c)
    {
        if (c == ' ' || IsGapTile(file, c) || file.IsChasm(c)) return ' ';
        if (file.IsFakeWall(c)) return '#';
        if (c == 'K' || file.IsBuilding(c)) return 'b';
        if (c == 'H') return 'H';
        if (LevelMap.IsWall(c) || file.IsWallProp(c)) return '#';
        if (LevelMap.IsWater(c)) return 'w';
        if (LevelMap.IsLava(c)) return '~';
        if (c == '=' || c == '-' || c == 'X') return '=';
        return '.';
    }
}
