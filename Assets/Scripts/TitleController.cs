using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// The title screen (Assets/UI/Title.uxml): the first scene in the build.
// "Continue" picks up the most recently played save. Below it are three slot cards:
// a used slot shows its hero, where they are and their level; click it to carry on.
// An empty slot starts a new adventure (character select) that will save into it.
// Keys: 1/2/3 open that slot. Left/Right (d-pad, stick) move the highlight, which starts on
// the most recent save, and Enter (A) opens the highlighted slot, so Enter alone continues.
// "Quit game" at the bottom closes the game; Down (d-pad, stick) moves the highlight onto it.
// Continue is the big gold button (shown when any save exists) and says who and where. A line under it
// says what Enter will do for the highlighted card: continue it, start a new adventure, or (after one
// click on Erase) warn that erasing is for good. Moving away or waiting a few seconds takes the erase back.
[RequireComponent(typeof(UIDocument))]
public class TitleController : MonoBehaviour
{
    [SerializeField] private CharacterDefinition[] heroes;
    [SerializeField] private string characterSelectScene = "CharacterSelect";
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip startSound;

    public CharacterDefinition[] Heroes => heroes;

    private VisualElement root;
    private Button continueButton, quitButton;
    private Label status;
    private const float EraseArmedSeconds = 4f;
    private float eraseDisarmAt = float.MaxValue;
    private readonly int[] eraseArmed = { 0, 0, 0 }; // erasing takes two clicks
    private bool leaving;
    public int Highlighted { get; private set; }

    // The highlight goes 0-2 over the slots, then QuitIndex for the Quit button below them.
    public const int QuitIndex = SaveSystem.SlotCount;
    public bool QuitHighlighted => Highlighted == QuitIndex;
    private int lastSlot; // where Up from the Quit button goes back to

    private void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        continueButton = root.Q<Button>("continue");
        continueButton.clicked += Continue;
        status = root.Q<Label>("status");
        quitButton = root.Q<Button>("quit");
        quitButton.clicked += AppQuit.Quit;
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            int slot = i;
            var card = root.Q($"slot-{i}");
            card.RegisterCallback<ClickEvent>(_ => OpenSlot(slot));
            var erase = card.Q<Button>("slot-erase");
            erase.clicked += () => Erase(slot);
            // Don't let the erase click also open the slot.
            erase.RegisterCallback<ClickEvent>(e => e.StopPropagation());
        }
        Highlight(Mathf.Max(0, SaveSystem.MostRecentSlot()));
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime >= eraseDisarmAt && DisarmErase()) Refresh();
        if (GameInput.ConfirmPressed)
        {
            if (QuitHighlighted) AppQuit.Quit();
            else OpenSlot(Highlighted);
        }
        if (QuitHighlighted)
        {
            if (GameInput.UpPressed) Highlight(lastSlot, sound: true);
        }
        else
        {
            if (GameInput.LeftPressed) Highlight(Highlighted - 1, sound: true);
            if (GameInput.RightPressed) Highlight(Highlighted + 1, sound: true);
            if (GameInput.DownPressed) Highlight(QuitIndex, sound: true);
        }
        for (int i = 0; i < SaveSystem.SlotCount; i++)
            if (GameInput.NumberPressed(i + 1)) OpenSlot(i);
    }

    public void Highlight(int index, bool sound = false)
    {
        index = Mathf.Clamp(index, 0, QuitIndex);
        if (sound && index != Highlighted) AudioManager.Play(selectSound);
        bool moved = index != Highlighted;
        Highlighted = index;
        if (!QuitHighlighted) lastSlot = index;
        for (int i = 0; i < SaveSystem.SlotCount; i++)
            root.Q($"slot-{i}").EnableInClassList("selected", i == Highlighted);
        quitButton.EnableInClassList("selected", QuitHighlighted);
        if (moved && DisarmErase()) Refresh();
        UpdateStatus();
    }

    // Redraw the cards from the save files.
    private void Refresh()
    {
        continueButton.style.display = SaveSystem.MostRecentSlot() >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            var card = root.Q($"slot-{i}");
            var data = SaveSystem.Peek(i);
            var state = SaveSystem.StateOf(i);
            bool unusable = state == SaveSystem.SlotState.Damaged || state == SaveSystem.SlotState.FromNewerGame;
            var hero = data == null ? null : System.Array.Find(heroes, h => h != null && h.name == data.hero);
            card.EnableInClassList("empty", data == null && !unusable);
            card.EnableInClassList("damaged", unusable);
            card.EnableInClassList("erasing", eraseArmed[i] > 0);
            card.Q<Label>("slot-tag").text = eraseArmed[i] > 0 ? "Erase for good?" : unusable ? "Can't open this" : data == null ? "+ New adventure" : "Continue";

            var portrait = card.Q("slot-portrait");
            portrait.style.backgroundImage = hero != null ? new StyleBackground(hero.Portrait) : new StyleBackground();
            card.Q<Label>("slot-name").text = unusable ? (state == SaveSystem.SlotState.Damaged ? "Damaged save" : "Newer save")
                                              : data == null ? "New adventure" : hero != null ? hero.DisplayName : data.hero;
            card.Q<Label>("slot-place").text = unusable ? "A copy is kept" : data == null ? "" : SaveSystem.PlaceName(data.scene);
            card.Q<Label>("slot-stats").text = data == null ? "" : $"Level {data.level}   {data.gold} gold";

            var erase = card.Q<Button>("slot-erase");
            erase.style.display = data == null && !unusable ? DisplayStyle.None : DisplayStyle.Flex;
            erase.text = eraseArmed[i] > 0 ? "Really erase?" : "Erase";
        }
        if (continueButton.style.display == DisplayStyle.Flex) continueButton.text = ContinueText();
        UpdateStatus();
    }

    // "Continue: Aldric the Wizard, Castle Grounds". Continue always picks up the most recent save.
    private string ContinueText()
    {
        var data = SaveSystem.Peek(SaveSystem.MostRecentSlot());
        var hero = data == null ? null : System.Array.Find(heroes, h => h != null && h.name == data.hero);
        string who = hero != null ? hero.DisplayName : data?.hero;
        return $"Continue: {who}, {SaveSystem.PlaceName(data?.scene)}   [Enter]";
    }

    // What Enter (A) does right now, in words, for the highlighted card.
    private void UpdateStatus()
    {
        if (status == null) return;
        bool warn = Highlighted < SaveSystem.SlotCount && eraseArmed[Highlighted] > 0;
        string text;
        if (QuitHighlighted) text = "Enter closes the game. Your adventures are already saved.";
        else if (warn) text = $"Adventure {Highlighted + 1} will be gone for good. Click Erase again to be sure, or move away to keep it.";
        else if (SaveSystem.StateOf(Highlighted) == SaveSystem.SlotState.Damaged)
            text = $"Adventure {Highlighted + 1} can't be read, so it hasn't been touched. Erase it to start again (a copy is kept next to it).";
        else if (SaveSystem.StateOf(Highlighted) == SaveSystem.SlotState.FromNewerGame)
            text = $"Adventure {Highlighted + 1} was made by a newer version of the game, so it's being left alone. Use the newer game to play it.";
        else if (SaveSystem.Peek(Highlighted) != null) text = $"Enter continues adventure {Highlighted + 1}. You start with full hearts and magic. (Nothing is lost.)";
        else text = $"Enter starts a new adventure in slot {Highlighted + 1}.";
        status.text = text;
        status.EnableInClassList("warning", warn);
        continueButton.EnableInClassList("selected", !QuitHighlighted && Highlighted == SaveSystem.MostRecentSlot());
    }

    // Take back any armed "Really erase?". True if there was one.
    private bool DisarmErase()
    {
        bool any = false;
        for (int i = 0; i < eraseArmed.Length; i++)
        {
            any |= eraseArmed[i] > 0;
            eraseArmed[i] = 0;
        }
        eraseDisarmAt = float.MaxValue;
        return any;
    }

    public void Continue()
    {
        int slot = SaveSystem.MostRecentSlot();
        if (slot >= 0) OpenSlot(slot);
    }

    public void OpenSlot(int slot)
    {
        if (leaving) return;
        var state = SaveSystem.StateOf(slot);
        if (state == SaveSystem.SlotState.Damaged || state == SaveSystem.SlotState.FromNewerGame)
        {
            Highlight(slot);   // never start a new adventure on top of something unreadable: say what's wrong instead
            UpdateStatus();
            return;
        }
        string scene = SaveSystem.Load(slot, heroes);
        if (scene == null)
        {
            // An empty slot: pick a hero, then the adventure saves here.
            GameSession.NewGame(null);
            GameSession.Slot = slot;
            scene = characterSelectScene;
        }
        leaving = true;
        StartCoroutine(LeaveAfterSound(scene));
    }

    public void Erase(int slot)
    {
        if (eraseArmed[slot] == 0)
        {
            eraseArmed[slot] = 1;
            eraseDisarmAt = Time.unscaledTime + EraseArmedSeconds;
            AudioManager.Play(selectSound);
        }
        else
        {
            eraseArmed[slot] = 0;
            SaveSystem.Delete(slot);
        }
        Refresh();
    }

    // Same as character select: let the "start" blip play before the scene changes.
    private IEnumerator LeaveAfterSound(string scene)
    {
        AudioManager.Play(startSound);
        yield return new WaitForSeconds(0.25f);
        SceneManager.LoadScene(scene);
    }
}
