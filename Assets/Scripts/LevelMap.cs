using UnityEngine;

// The level's ASCII layout, saved into the scene by DungeonBuilder so runtime code
// (the minimap) can ask "what's at this tile?" without scanning the scene's objects.
public class LevelMap : MonoBehaviour
{
    [SerializeField] private string[] rows;
    [SerializeField] private float tileSize = 2f;
    [SerializeField] private Color floorColor = new Color32(78, 70, 62, 255); // minimap colors
    [SerializeField] private Color wallColor = new Color32(170, 164, 184, 255);

    public int Height => rows.Length;
    public int Width => rows[0].Length;
    public float TileSize => tileSize;
    public Color FloorColor => floorColor;
    public Color WallColor => wallColor;

    // Walls: '#' stone ('T' with a torch), 'H' hedge, 'K' castle. Anything else except
    // ' ' (nothing) is walkable ground: '.', ',' grass, '=' path, '_' bathroom tiles,
    // and the markers P/E/C/I/D/X/B/W/S/R/O.
    public static bool IsWall(char c) => c == '#' || c == 'T' || c == 'H' || c == 'K';

    // Pond water ('w', and 'm' where the mermaid sits): not walkable, drawn blue on the minimap.
    public static bool IsWater(char c) => c == 'w' || c == 'm';

    public char At(int col, int row) =>
        row >= 0 && row < Height && col >= 0 && col < rows[row].Length ? rows[row][col] : ' ';

    // Fractional map coordinates of a world position: (column, row), with row 0 at the top.
    // Mirrors the placement in DungeonBuilder: x = col * tile, z = (Height - 1 - row) * tile.
    public Vector2 WorldToMap(Vector3 world) =>
        new Vector2(world.x / tileSize, Height - 1 - world.z / tileSize);
}
