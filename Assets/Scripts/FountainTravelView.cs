using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// The fountain menu: press T (or click the left stick) beside a fountain. "Rest here" heals you
// and saves the game; every other fountain you've touched is a destination, so the castle grounds
// and the Whispering Woods are one button apart. Choosing one saves, then loads that scene with
// you arriving at its fountain (the "Fountain" named spawn).
//
// Like the recipe card it pauses the game while open, and Esc / B (or clicking the X) closes it.
// Up / down (or the d-pad) move the highlight; E / Enter / A (or a click) picks.
[RequireComponent(typeof(UIDocument))]
public class FountainTravelView : MonoBehaviour
{
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip travelSound;

    // The scene names a fountain can take you to, in the order they appear in the menu.
    private static readonly string[] Places = { "Level0", "Woods1" };

    public static FountainTravelView Instance { get; private set; }
    public static bool BlocksInput => Instance != null && (Instance.IsOpen || Time.frameCount == Instance.closedFrame);

    public bool IsOpen => panel != null && panel.ClassListContains("open");
    public IReadOnlyList<string> Options => optionScenes;   // "" = rest here, otherwise the scene to travel to

    private VisualElement panel, list;
    private Label message;
    private GameObject player;
    private readonly List<string> optionScenes = new List<string>();
    private readonly List<VisualElement> rows = new List<VisualElement>();
    private int selected, openedFrame = -1, closedFrame = -1;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsOpen) Time.timeScale = 1f;
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q("fountain-menu");
        list = root.Q("fountain-list");
        message = root.Q<Label>("fountain-message");
        root.Q("fountain-close").RegisterCallback<ClickEvent>(_ => Close());
    }

    private void Update()
    {
        if (!IsOpen)
        {
            if (GameInput.TravelPressed && !GameInput.GameplayBlocked) TryOpen();
            return;
        }
        if (Time.frameCount == openedFrame) return;
        if (GameInput.BackPressed || GameInput.MenuPressed || GameInput.TravelPressed) { Close(); return; }
        if (GameInput.UpPressed) Select(selected - 1);
        if (GameInput.DownPressed) Select(selected + 1);
        if (GameInput.InteractPressed || GameInput.ConfirmPressed) Choose(selected);
    }

    // Opens the menu if the hero is beside a fountain.
    public bool TryOpen()
    {
        var bootstrap = LevelBootstrap.Current;
        if (IsOpen || bootstrap == null || bootstrap.Player == null) return false;
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return false;
        if (WakeFountain.Near(bootstrap.Player.transform.position) == null) return false;

        player = bootstrap.Player;
        string here = SceneManager.GetActiveScene().name;
        optionScenes.Clear();
        optionScenes.Add("");
        foreach (var place in Places)
            if (place != here && WakeFountain.Touched(place)) optionScenes.Add(place);

        selected = 0;
        openedFrame = Time.frameCount;
        message.text = optionScenes.Count == 1
            ? "Touch other fountains and you can travel between them."
            : "Pick where to go.";
        panel.EnableInClassList("open", true);
        Time.timeScale = 0f;
        AudioManager.Play(openSound, 0.6f);
        BuildRows();
        return true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        panel.EnableInClassList("open", false);
        closedFrame = Time.frameCount;
        Time.timeScale = 1f;
    }

    private void BuildRows()
    {
        list.Clear();
        rows.Clear();
        for (int i = 0; i < optionScenes.Count; i++)
        {
            int index = i;
            string scene = optionScenes[i];
            var row = new Label(scene.Length == 0 ? "Rest here (heal and save)" : $"Travel to {SaveSystem.PlaceName(scene)}");
            row.AddToClassList("fountain-option");
            row.EnableInClassList("here", scene.Length == 0);
            row.RegisterCallback<ClickEvent>(_ => Choose(index));
            list.Add(row);
            rows.Add(row);
        }
        Select(0);
    }

    private void Select(int index)
    {
        selected = Mathf.Clamp(index, 0, rows.Count - 1);
        for (int i = 0; i < rows.Count; i++) rows[i].EnableInClassList("selected", i == selected);
    }

    // Public so tests can drive it: 0 is "rest here", then each destination.
    public void Choose(int index)
    {
        if (!IsOpen || index < 0 || index >= optionScenes.Count) return;
        string scene = optionScenes[index];
        if (scene.Length == 0)
        {
            WakeFountain.Rest(player);
            message.text = "Ahh. You feel rested, and your adventure is saved!";
            AudioManager.Play(travelSound, 0.6f);
            return;
        }
        Close();
        GameSession.NextSpawn = WakeFountain.SpawnName;
        SaveSystem.Autosave(scene, WakeFountain.SpawnName);
        AudioManager.Play(travelSound);
        SceneManager.LoadScene(scene);
    }
}
