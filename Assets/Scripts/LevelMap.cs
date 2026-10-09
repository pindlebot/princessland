using UnityEngine;

// The level's ASCII layout, saved into the scene by DungeonBuilder so runtime code
// (the minimap) can ask "what's at this tile?" without scanning the scene's objects.
public class LevelMap : MonoBehaviour
{
    [SerializeField] private string[] rows;
    [SerializeField] private float tileSize = 2f;
    [SerializeField] private Color floorColor = new Color32(78, 70, 62, 255); // minimap colors
    [SerializeField] private Color wallColor = new Color32(170, 164, 184, 255);
    [Tooltip("The map's building symbols (from its legend, e.g. \"345\"): solid, drawn as roofs on the minimap.")]
    [SerializeField] private string buildings = "";

    public int Height => rows.Length;
    public int Width => rows[0].Length;
    public float TileSize => tileSize;
    public Color FloorColor => floorColor;
    public Color WallColor => wallColor;

    // Walls: '#' stone ('T' with a torch), 'H' hedge, 'K' castle, '%' rock ('|' with a waterfall
    // pouring down its front), '+' the village's picket fence. Anything else except
    // ' ' (nothing) is walkable ground: '.', ',' grass, '=' path, '-' cobbles, '_' bathroom tiles,
    // and the markers P/E/C/I/D/X/B/W/S/R/O (and the bedroom's A/N/U/G/v/r).
    // (Buildings are walls too, but their symbols come from each map's legend: see IsSolid.)
    public static bool IsWall(char c) => c == '#' || c == 'T' || c == 'H' || c == 'K' || c == '%' || c == '|' || c == '+';

    public static bool IsFence(char c) => c == '+';
    public static bool IsCobbles(char c) => c == '-';

    // A building's tile (this map's legend says which symbols are buildings).
    public bool IsBuilding(char c) => c != ' ' && buildings.IndexOf(c) >= 0;

    // Does this tile block the way: a wall, or part of a building?
    public bool IsSolid(char c) => IsWall(c) || IsBuilding(c);

    // Water: 'w', and the water under 'm' Coralie, 'Z' Pearl, '&' a dark mermaid and '@' the
    // pirate ship. Not walkable, drawn blue on the minimap.
    public static bool IsWater(char c) => c == 'w' || c == 'm' || c == '&' || c == '@' || c == 'Z';

    // Beach sand ('g') and wooden planks ('h', jetties over the sea): walkable ground.
    public static bool IsSand(char c) => c == 'g';
    public static bool IsPlanks(char c) => c == 'h';

    // The physics layer of the invisible walls that keep everyone out of the water. Spells,
    // bolts and line-of-sight checks ignore it, so you can fight across the water.
    // (Layer 4 is Unity's built-in "Water" layer.)
    public const int WaterLayer = 4;

    // Hazards: walkable, but they hurt. '~' lava, '^' a spike trap (see Hazard.cs).
    public static bool IsLava(char c) => c == '~';
    public static bool IsSpikes(char c) => c == '^';
    public static bool IsHazard(char c) => IsLava(c) || IsSpikes(c);

    public char At(int col, int row) =>
        row >= 0 && row < Height && col >= 0 && col < rows[row].Length ? rows[row][col] : ' ';

    // Fractional map coordinates of a world position: (column, row), with row 0 at the top.
    // Mirrors the placement in DungeonBuilder: x = col * tile, z = (Height - 1 - row) * tile.
    public Vector2 WorldToMap(Vector3 world) =>
        new Vector2(world.x / tileSize, Height - 1 - world.z / tileSize);
}
