using System.Collections.Generic;
using UnityEngine;

// Where monsters can walk, worked out from the level's own tiles, so they can path around walls instead
// of pushing into them. (Unity's NavMesh package would do this too, but its current release doesn't
// compile on this Unity version, and the levels are already a grid of 2m tiles.)
//
// At the start of a level it asks the physics world, tile by tile, whether anything solid stands there (a wall,
// a tree, a pot, a gap's invisible wall, the pond's bank), skipping the characters themselves. FindPath is
// then a plain A* over those tiles (eight ways, never cutting a corner). Things that disappear (a bramble
// burning, a pot smashing) call MarkDirty, and the grid is worked out again a moment later.
[RequireComponent(typeof(LevelMap))]
public class NavGrid : MonoBehaviour
{
    private const float RebakeDelay = 0.4f;

    public static NavGrid Current { get; private set; }

    private LevelMap map;
    private bool[,] open; // [col, row]
    private float rebakeAt = -1f;

    public int Width => map.Width;
    public int Height => map.Height;
    public bool IsOpen(int col, int row) => col >= 0 && row >= 0 && col < Width && row < Height && open[col, row];

    private void Awake()
    {
        map = GetComponent<LevelMap>();
        Current = this;
    }

    private void Start() => Bake();

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    private void Update()
    {
        if (rebakeAt >= 0f && Time.time >= rebakeAt) Bake();
    }

    // Something solid went away (or appeared): work the grid out again soon.
    public static void MarkDirty()
    {
        if (Current != null && Current.rebakeAt < 0f) Current.rebakeAt = Time.time + RebakeDelay;
    }

    public void Bake()
    {
        rebakeAt = -1f;
        Physics.SyncTransforms();
        open = new bool[Width, Height];
        float tile = map.TileSize;
        var half = new Vector3(0.7f, 0.7f, 0.7f);
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                if (map.At(col, row) == ' ') continue;
                open[col, row] = !SolidAt(Center(col, row) + Vector3.up, half);
            }
        }
    }

    private static bool SolidAt(Vector3 center, Vector3 half)
    {
        foreach (var hit in Physics.OverlapBox(center, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (!(hit is CharacterController)) return true; // the hero and the monsters aren't walls
        return false;
    }

    public Vector3 Center(int col, int row) => new Vector3(col * map.TileSize, 0f, (Height - 1 - row) * map.TileSize);

    public (int col, int row) TileOf(Vector3 world)
    {
        var m = map.WorldToMap(world);
        return (Mathf.RoundToInt(m.x), Mathf.RoundToInt(m.y));
    }

    // Can something walk straight from one spot to another over open tiles? Checked along the line and a
    // little to each side of it (a monster isn't a point), so it won't clip a wall's corner.
    public bool CanWalkStraight(Vector3 from, Vector3 to)
    {
        var line = to - from;
        line.y = 0f;
        float length = line.magnitude;
        if (length < 0.01f) return true;
        var dir = line / length;
        var side = new Vector3(-dir.z, 0f, dir.x) * 0.45f;
        for (float d = 0f; d <= length; d += 0.4f)
        {
            var p = from + dir * d;
            foreach (var offset in new[] { Vector3.zero, side, -side })
            {
                var tile = TileOf(p + offset);
                if (!IsOpen(tile.col, tile.row)) return false;
            }
        }
        return true;
    }

    // The nearest open tile to a spot (its own tile if that's open).
    private (int, int)? NearestOpen((int col, int row) tile)
    {
        if (IsOpen(tile.col, tile.row)) return tile;
        for (int radius = 1; radius <= 2; radius++)
            for (int dr = -radius; dr <= radius; dr++)
                for (int dc = -radius; dc <= radius; dc++)
                    if (IsOpen(tile.col + dc, tile.row + dr)) return (tile.col + dc, tile.row + dr);
        return null;
    }

    // The way from one spot to another as the centres of the tiles to walk through, nearest first (the tile
    // you're in is left out). False if there's no way at all (a gap or a wall in between).
    public bool FindPath(Vector3 from, Vector3 to, List<Vector3> path)
    {
        path.Clear();
        var s = NearestOpen(TileOf(from));
        var g = NearestOpen(TileOf(to));
        if (s == null || g == null) return false;
        var start = s.Value;
        var goal = g.Value;
        if (start == goal) return true;

        int w = Width;
        int Id((int col, int row) t) => t.row * w + t.col;
        var cost = new Dictionary<int, float> { [Id(start)] = 0f };
        var came = new Dictionary<int, int>();
        var frontier = new SortedSet<(float f, int id)> { (Heuristic(start, goal), Id(start)) };
        var closed = new HashSet<int>();

        while (frontier.Count > 0)
        {
            var current = frontier.Min;
            frontier.Remove(current);
            int cid = current.id;
            if (!closed.Add(cid)) continue;
            var c = (col: cid % w, row: cid / w);
            if (c == goal)
            {
                var tiles = new List<int> { cid };
                while (came.TryGetValue(tiles[tiles.Count - 1], out int previous)) tiles.Add(previous);
                tiles.Reverse();
                for (int i = 1; i < tiles.Count; i++) path.Add(Center(tiles[i] % w, tiles[i] / w));
                return true;
            }
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dc == 0 && dr == 0) continue;
                    var n = (col: c.col + dc, row: c.row + dr);
                    if (!IsOpen(n.col, n.row) || closed.Contains(Id(n))) continue;
                    // No squeezing diagonally between two blocked tiles' corners.
                    if (dc != 0 && dr != 0 && (!IsOpen(c.col + dc, c.row) || !IsOpen(c.col, c.row + dr))) continue;
                    float next = cost[cid] + (dc != 0 && dr != 0 ? 1.414f : 1f);
                    if (cost.TryGetValue(Id(n), out float old) && old <= next) continue;
                    cost[Id(n)] = next;
                    came[Id(n)] = cid;
                    frontier.Add((next + Heuristic(n, goal), Id(n)));
                }
            }
        }
        return false;
    }

    private static float Heuristic((int col, int row) a, (int col, int row) b)
    {
        int dx = Mathf.Abs(a.col - b.col), dy = Mathf.Abs(a.row - b.row);
        return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy);
    }
}
