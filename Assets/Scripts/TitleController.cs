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
[RequireComponent(typeof(UIDocument))]
public class TitleController : MonoBehaviour
{
    [SerializeField] private CharacterDefinition[] heroes;
    [SerializeField] private string characterSelectScene = "CharacterSelect";
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip startSound;

    public CharacterDefinition[] Heroes => heroes;

    private VisualElement root;
    private Button continueButton;
    private readonly int[] eraseArmed = { 0, 0, 0 }; // erasing takes two clicks
    private bool leaving;
    public int Highlighted { get; private set; }

    private void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        continueButton = root.Q<Button>("continue");
        continueButton.clicked += Continue;
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
        if (GameInput.ConfirmPressed) OpenSlot(Highlighted);
        if (GameInput.LeftPressed) Highlight(Highlighted - 1, sound: true);
        if (GameInput.RightPressed) Highlight(Highlighted + 1, sound: true);
        for (int i = 0; i < SaveSystem.SlotCount; i++)
            if (GameInput.NumberPressed(i + 1)) OpenSlot(i);
    }

    public void Highlight(int slot, bool sound = false)
    {
        slot = Mathf.Clamp(slot, 0, SaveSystem.SlotCount - 1);
        if (sound && slot != Highlighted) AudioManager.Play(selectSound);
        Highlighted = slot;
        for (int i = 0; i < SaveSystem.SlotCount; i++)
            root.Q($"slot-{i}").EnableInClassList("selected", i == Highlighted);
    }

    // Redraw the cards from the save files.
    private void Refresh()
    {
        continueButton.style.display = SaveSystem.MostRecentSlot() >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            var card = root.Q($"slot-{i}");
            var data = SaveSystem.Peek(i);
            var hero = data == null ? null : System.Array.Find(heroes, h => h != null && h.name == data.hero);
            card.EnableInClassList("empty", data == null);

            var portrait = card.Q("slot-portrait");
            portrait.style.backgroundImage = hero != null ? new StyleBackground(hero.Portrait) : new StyleBackground();
            card.Q<Label>("slot-name").text = data == null ? "New adventure" : hero != null ? hero.DisplayName : data.hero;
            card.Q<Label>("slot-place").text = data == null ? "" : SaveSystem.PlaceName(data.scene);
            card.Q<Label>("slot-stats").text = data == null ? "" : $"Level {data.level}   {data.gold} gold";

            var erase = card.Q<Button>("slot-erase");
            erase.style.display = data == null ? DisplayStyle.None : DisplayStyle.Flex;
            erase.text = eraseArmed[i] > 0 ? "Really erase?" : "Erase";
        }
    }

    public void Continue()
    {
        int slot = SaveSystem.MostRecentSlot();
        if (slot >= 0) OpenSlot(slot);
    }

    public void OpenSlot(int slot)
    {
        if (leaving) return;
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

    private void Erase(int slot)
    {
        if (eraseArmed[slot] == 0)
        {
            eraseArmed[slot] = 1;
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
