using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// The world map (press M, or "World map" in the pause menu): every room you've been in, laid out where it
// sits in the world, with a dot for where you are, a blue mark on each fountain, a "?" on every gap you
// have seen but can't cross yet (so you remember to come back with the Bouncy Boots), and a purple
// egg where you've spotted one you haven't taken. Rooms you've never visited aren't drawn.
//
// Like the fountain menu it stops time while open, and Esc / B / M closes it. The rooms come from
// WorldMapData (built from the map files); this view only paints them.
[RequireComponent(typeof(UIDocument))]
public class WorldMapView : MonoBehaviour
{
    [SerializeField] private WorldMapData data;
    [SerializeField] private AudioClip openSound;

    private const float CanvasWidth = 760f, CanvasHeight = 380f;
    private const float MaxScale = 6f;

    public static WorldMapView Instance { get; private set; }
    public static bool BlocksInput => Instance != null && (Instance.IsOpen || Time.frameCount == Instance.closedFrame);

    public bool IsOpen => panel != null && panel.ClassListContains("open");
    // What's on the map right now, for tests: "room:<scene>", "you", "fountain:<scene>", "gap:<id>", "egg:<id>".
    public IReadOnlyList<string> Drawn => drawn;

    private VisualElement panel, canvas;
    private Label legend;
    private readonly List<string> drawn = new List<string>();
    private int openedFrame = -1, closedFrame = -1;
    private readonly List<Texture2D> textures = new List<Texture2D>();

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsOpen) Time.timeScale = 1f;
        foreach (var t in textures) if (t != null) Destroy(t);
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q("worldmap");
        canvas = root.Q("world-canvas");
        legend = root.Q<Label>("world-legend");
        panel.Q(className: "inventory-close").RegisterCallback<ClickEvent>(_ => Close());
    }

    private void Update()
    {
        if (!IsOpen)
        {
            if (GameInput.MapPressed && !GameInput.GameplayBlocked) Open();
            return;
        }
        if (Time.frameCount == openedFrame) return;
        if (GameInput.MapPressed || GameInput.BackPressed || GameInput.MenuPressed) Close();
    }

    public void Open()
    {
        if (IsOpen || panel == null || PauseMenu.IsOpen) return;
        openedFrame = Time.frameCount;
        Time.timeScale = 0f;
        Build();
        panel.EnableInClassList("open", true);
        AudioManager.Play(openSound, 0.6f);
    }

    public void Close()
    {
        if (!IsOpen) return;
        closedFrame = Time.frameCount;
        panel.EnableInClassList("open", false);
        Time.timeScale = 1f;
        AudioManager.Play(openSound, 0.6f);
    }

    // ---------- Drawing ----------

    private static bool Visited(string scene) => GameSession.Flags.Contains("visited:" + scene);

    private void Build()
    {
        canvas.Clear();
        drawn.Clear();
        foreach (var t in textures) if (t != null) Destroy(t);
        textures.Clear();
        if (data == null) return;

        var shown = new List<WorldMapData.Room>();
        foreach (var room in data.rooms) if (Visited(room.scene)) shown.Add(room);
        if (shown.Count == 0) return;

        // Fit every visited room into the canvas, with a little room for the titles.
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var r in shown)
        {
            minX = Mathf.Min(minX, r.x); minY = Mathf.Min(minY, r.y);
            maxX = Mathf.Max(maxX, r.x + r.width); maxY = Mathf.Max(maxY, r.y + r.height);
        }
        float scale = Mathf.Min(MaxScale, (CanvasWidth - 20f) / (maxX - minX), (CanvasHeight - 40f) / (maxY - minY));
        float offsetX = (CanvasWidth - (maxX - minX) * scale) / 2f, offsetY = (CanvasHeight - (maxY - minY) * scale) / 2f;
        Vector2 Place(WorldMapData.Room r, float col, float row) =>
            new Vector2(offsetX + (r.x - minX + col) * scale, offsetY + (r.y - minY + row) * scale);

        string here = SceneManager.GetActiveScene().name;
        foreach (var room in shown)
        {
            var origin = Place(room, 0, 0);
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList("world-room");
            if (room.scene == here) element.AddToClassList("here");
            element.style.left = origin.x;
            element.style.top = origin.y;
            element.style.width = room.width * scale;
            element.style.height = room.height * scale;
            element.style.backgroundImage = new StyleBackground(Paint(room));
            canvas.Add(element);
            drawn.Add("room:" + room.scene);

            string hero = LevelBootstrap.Current != null ? LevelBootstrap.Current.Character.DisplayName.Split(new[] { " the " }, System.StringSplitOptions.None)[0] : "Your";
            var title = new Label(room.title.Replace("{hero}", hero)) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("world-title");
            if (room.scene == here) title.AddToClassList("here");
            title.style.left = origin.x;
            title.style.top = origin.y + room.height * scale + 1f;
            canvas.Add(title);

            AddMarkers(room, scale, Place);
        }

        // The quest being followed: a star on the room where its next step is (only once that room is on the map).
        var followed = QuestCatalog.Tracked();
        string goalScene = followed != null ? QuestCatalog.WhereNext(followed) : "";
        var goalRoom = goalScene.Length > 0 ? shown.Find(r => r.scene == goalScene) : null;
        if (goalRoom != null)
        {
            AddDot(Place(goalRoom, goalRoom.width / 2f, goalRoom.height / 2f), "world-goal", 18f, "*");
            drawn.Add("goal:" + goalRoom.scene);
        }

        // You are here.
        var current = data.Find(here);
        var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        var levelMap = Object.FindAnyObjectByType<LevelMap>();
        if (current != null && player != null && levelMap != null && shown.Contains(current))
        {
            var m = levelMap.WorldToMap(player.transform.position);
            AddDot(Place(current, m.x + 0.5f, m.y + 0.5f), "world-you", 12f, "");
            drawn.Add("you");
        }
        legend.text = "You are here · ? a gap to come back to · a purple egg is one of Amethyra's · blue marks a fountain · " +
                      "small rings are doors (gold: somewhere new, grey x: stairs waiting for the monsters to be beaten)";
        if (followed != null)
        {
            string place = goalScene.Length > 0 ? SaveSystem.PlaceName(goalScene) : "";
            legend.text += $"\nFollowing: {followed.Title}" + (place.Length > 0 ? $" · go to {place}" + (goalRoom != null ? " (the star)" : "") : "");
        }
    }

    private void AddMarkers(WorldMapData.Room room, float scale, System.Func<WorldMapData.Room, float, float, Vector2> place)
    {
        foreach (var marker in room.markers)
        {
            string kind = null;
            string text = "";
            if (marker.kind == "fountain")
            {
                kind = GameSession.Flags.Contains(WakeFountain.FlagFor(room.scene)) ? "world-fountain" : "world-fountain dim";
                drawn.Add("fountain:" + room.scene);
            }
            else if (marker.kind == "gap" && GameSession.Flags.Contains(HintBubble.SeenFlag(marker.id)) && !Abilities.Has(Abilities.BouncyBoots))
            {
                kind = "world-gap";
                text = "?";
                drawn.Add("gap:" + marker.id);
            }
            else if (marker.kind == "egg" && !Abilities.Has(marker.id) && SeenAGateIn(room.scene))
            {
                kind = "world-egg";
                drawn.Add("egg:" + marker.id);
            }
            else if (marker.kind == "exit" || marker.kind == "lockedexit")
            {
                // A way into another room: open (white), leading somewhere new (gold ring), or stairs still waiting for
                // the room to be cleared (grey with a cross). Only the doors themselves: nothing about what is behind them.
                bool waiting = marker.kind == "lockedexit" && !GameSession.Flags.Contains("cleared:" + room.scene);
                bool isNew = !Visited(marker.id);
                kind = waiting ? "world-exit locked" : isNew ? "world-exit new" : "world-exit";
                text = waiting ? "x" : "";
                drawn.Add((waiting ? "locked:" : "exit:") + room.scene + ">" + marker.id + (isNew && !waiting ? ":new" : ""));
            }
            if (kind == null) continue;
            AddDot(place(room, marker.col + 0.5f, marker.row + 0.5f), kind, Mathf.Max(11f, scale * 1.8f), text);
        }
    }

    // You only know there's an egg in a room once you've seen a gap there (that's how you'd spot it).
    private static bool SeenAGateIn(string scene)
    {
        string prefix = "seen_gate:" + scene + "/";
        foreach (var flag in GameSession.Flags) if (flag.StartsWith(prefix)) return true;
        return false;
    }

    private void AddDot(Vector2 centre, string classes, float size, string text)
    {
        var dot = new Label(text) { pickingMode = PickingMode.Ignore };
        foreach (var c in classes.Split(' ')) dot.AddToClassList(c);
        dot.AddToClassList("world-dot");
        dot.style.width = size;
        dot.style.height = size;
        dot.style.left = centre.x - size / 2f;
        dot.style.top = centre.y - size / 2f;
        canvas.Add(dot);
    }

    // One pixel per tile, drawn crisply when the element stretches it.
    private Texture2D Paint(WorldMapData.Room room)
    {
        var texture = new Texture2D(room.width, room.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var clear = new Color32(0, 0, 0, 0);
        var floor = (Color32)room.floorColor;
        var wall = (Color32)room.wallColor;
        var hedge = new Color32(96, 140, 90, 255);
        var path = new Color32(214, 190, 140, 255);
        var water = new Color32(110, 170, 220, 255);
        var lava = new Color32(226, 100, 40, 255);
        var roof = new Color32(200, 110, 100, 255);
        var secret = new HashSet<string>(room.secretTiles);
        bool secretFound = GameSession.Flags.Contains(FakeWall.FoundFlag(room.scene));
        var pixels = new Color32[room.width * room.height];
        for (int row = 0; row < room.height; row++)
        {
            for (int col = 0; col < room.width; col++)
            {
                char c = col < room.rows[row].Length ? room.rows[row][col] : ' ';
                if (!secretFound && secret.Contains($"{col},{row}")) c = ' '; // a secret room stays secret
                Color32 color = c == ' ' ? clear : c == '#' ? wall : c == 'H' ? hedge : c == '=' ? path
                              : c == 'w' ? water : c == '~' ? lava : c == 'b' ? roof : floor;
                pixels[(room.height - 1 - row) * room.width + col] = color; // texture rows count from the bottom
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
        textures.Add(texture);
        return texture;
    }
}
