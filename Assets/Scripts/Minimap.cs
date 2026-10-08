using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The small round minimap in the HUD, in two layers:
//   1. Terrain: the level drawn into a small texture (a few pixels per tile), rotated to
//      match the isometric camera. Tiles appear once you've been near them (fog of war).
//   2. Markers: little upright icons laid on top: a gold crown for the hero (always in the
//      middle), stairs for the exit (grey while locked, green when open), monsters nearby,
//      the chest and the dragon. They're separate UI elements rather than pixels in the
//      texture, so they stay upright and crisp while the terrain turns underneath.
[RequireComponent(typeof(UIDocument))]
public class Minimap : MonoBehaviour
{
    [SerializeField] private LevelMap map;
    private Transform player; // set by LevelBootstrap
    [SerializeField] private int pixelsPerTile = 4;
    [SerializeField] private float displayScale = 2f; // screen pixels per texture pixel
    [SerializeField] private float revealRadius = 4.5f; // in tiles
    [SerializeField] private float enemySenseRadius = 7f; // in tiles

    private static readonly Color32 Hidden = new Color32(0, 0, 0, 0);

    private Texture2D texture;
    private Color32[] pixels;
    private bool[,] explored;
    private VisualElement frame, image, markers;
    private Camera cam;
    private ExitZone exit;
    private VisualElement crown;
    private readonly Dictionary<string, List<VisualElement>> pools = new Dictionary<string, List<VisualElement>>();
    private readonly Dictionary<string, int> used = new Dictionary<string, int>();

    public bool IsExplored(int col, int row) => explored[col, row];

    public void SetPlayer(Transform newPlayer) => player = newPlayer;

    private void Start()
    {
        cam = Camera.main;
        exit = FindAnyObjectByType<ExitZone>();
        int w = map.Width * pixelsPerTile, h = map.Height * pixelsPerTile;
        texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, // crisp, like the rest of the pixel art
            wrapMode = TextureWrapMode.Clamp,
        };
        pixels = new Color32[w * h];
        explored = new bool[map.Width, map.Height];

        var root = GetComponent<UIDocument>().rootVisualElement;
        frame = root.Q("minimap-frame");
        image = root.Q("minimap");
        image.style.backgroundImage = new StyleBackground(texture);
        image.style.width = w * displayScale;
        image.style.height = h * displayScale;

        markers = new VisualElement { name = "minimap-markers", pickingMode = PickingMode.Ignore };
        markers.AddToClassList("minimap-markers");
        frame.Add(markers);
        crown = Marker("map-crown");
    }

    private void LateUpdate()
    {
        Vector2 p = map.WorldToMap(player.position);
        Reveal(p);
        DrawTerrain();
        Place(p);
        PlaceMarkers(p);
    }

    private void Reveal(Vector2 p)
    {
        int r = Mathf.CeilToInt(revealRadius);
        for (int row = (int)p.y - r; row <= (int)p.y + r + 1; row++)
            for (int col = (int)p.x - r; col <= (int)p.x + r + 1; col++)
                if (col >= 0 && row >= 0 && col < map.Width && row < map.Height
                    && Vector2.Distance(new Vector2(col, row), p) <= revealRadius)
                    explored[col, row] = true;
    }

    private void DrawTerrain()
    {
        for (int row = 0; row < map.Height; row++)
        {
            for (int col = 0; col < map.Width; col++)
            {
                char c = map.At(col, row);
                Color32 color = !explored[col, row] || c == ' ' ? Hidden
                              : LevelMap.IsWall(c) ? (Color32)map.WallColor
                              : (Color32)map.FloorColor;
                FillTile(col, row, color);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    // Keep the hero at the center of the round frame and turn the map so that "up" on the
    // minimap is the direction the camera looks. Rotating around the hero's own pixel
    // (transform-origin) keeps it centered while it turns.
    private void Place(Vector2 p)
    {
        Rect box = frame.contentRect;
        if (float.IsNaN(box.width)) return; // not laid out yet (first frame)

        float px = (p.x + 0.5f) * pixelsPerTile * displayScale;
        float py = (p.y + 0.5f) * pixelsPerTile * displayScale;
        image.style.left = box.width / 2f - px;
        image.style.top = box.height / 2f - py;
        image.style.transformOrigin = new TransformOrigin(px, py);
        image.style.rotate = new Rotate(-cam.transform.eulerAngles.y);
    }

    // ---------- Markers ----------

    private void PlaceMarkers(Vector2 hero)
    {
        Rect box = frame.contentRect;
        if (float.IsNaN(box.width)) return;
        foreach (var key in pools.Keys) used[key] = 0;

        if (exit != null)
        {
            Vector2 x = map.WorldToMap(exit.transform.position);
            if (IsExploredAt(x)) Show(exit.IsOpen ? "map-stairs" : "map-stairs-locked", x, hero, box);
        }

        foreach (var interactable in Interactables.All)
        {
            bool isChest = interactable is Chest chest && !chest.IsOpen;
            bool isNpc = interactable is DragonNpc;
            if (!isChest && !isNpc) continue;
            Vector2 c = map.WorldToMap(interactable.Position);
            if (IsExploredAt(c)) Show(isNpc ? "map-dragon" : "map-chest", c, hero, box);
        }

        foreach (var enemy in EnemyAI.Alive)
        {
            Vector2 e = map.WorldToMap(enemy.transform.position);
            if (Vector2.Distance(e, hero) <= enemySenseRadius) Show("map-monster", e, hero, box);
        }

        // Hide any pooled markers not used this frame.
        foreach (var pair in pools)
            for (int i = 0; i < pair.Value.Count; i++)
                pair.Value[i].style.display = i < used[pair.Key] ? DisplayStyle.Flex : DisplayStyle.None;

        // The hero's crown: always dead center, always on top.
        crown.style.left = Mathf.Round(box.width / 2f);
        crown.style.top = Mathf.Round(box.height / 2f);
        crown.BringToFront();
    }

    // Where a map position lands inside the frame: its offset from the hero, scaled to screen
    // pixels and turned by the same angle as the terrain image.
    private void Show(string kind, Vector2 mapPos, Vector2 hero, Rect box)
    {
        Vector2 offset = (mapPos - hero) * pixelsPerTile * displayScale;
        float angle = -cam.transform.eulerAngles.y * Mathf.Deg2Rad;
        // UI coordinates point down, so this rotation turns the same way (clockwise) as style.rotate.
        Vector2 turned = new Vector2(offset.x * Mathf.Cos(angle) - offset.y * Mathf.Sin(angle),
                                     offset.x * Mathf.Sin(angle) + offset.y * Mathf.Cos(angle));
        var marker = NextMarker(kind);
        marker.style.left = Mathf.Round(box.width / 2f + turned.x); // whole pixels keep pixel art crisp
        marker.style.top = Mathf.Round(box.height / 2f + turned.y);
    }

    // Markers are pooled: made once and reused, rather than created every frame.
    private VisualElement NextMarker(string kind)
    {
        if (!pools.TryGetValue(kind, out var pool))
        {
            pool = new List<VisualElement>();
            pools[kind] = pool;
            used[kind] = 0;
        }
        if (used[kind] >= pool.Count) pool.Add(Marker(kind));
        return pool[used[kind]++];
    }

    private VisualElement Marker(string kind)
    {
        var marker = new VisualElement { pickingMode = PickingMode.Ignore };
        marker.AddToClassList("map-marker");
        marker.AddToClassList(kind);
        markers.Add(marker);
        return marker;
    }

    private bool IsExploredAt(Vector2 mapPos)
    {
        int col = Mathf.RoundToInt(mapPos.x), row = Mathf.RoundToInt(mapPos.y);
        return col >= 0 && row >= 0 && col < map.Width && row < map.Height && explored[col, row];
    }

    // Texture rows count up from the bottom, map rows count down from the top.
    private void FillTile(int col, int row, Color32 color)
    {
        int x0 = col * pixelsPerTile;
        int y0 = (map.Height - 1 - row) * pixelsPerTile;
        for (int y = y0; y < y0 + pixelsPerTile; y++)
            for (int x = x0; x < x0 + pixelsPerTile; x++)
                pixels[y * texture.width + x] = color;
    }
}
