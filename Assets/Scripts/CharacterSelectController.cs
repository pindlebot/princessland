using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// The character select screen (Assets/UI/CharacterSelect.uxml). Click a card or press
// 1/2 (or Left/Right, d-pad) to choose, then Start or Enter (A). Esc (B) goes back. The choice goes into GameSession
// so the levels know who to spawn.
// Each card shows the hero's health as hearts and magic as orbs (one per 10 mana), and the chosen hero's
// spell flashes at its own casting speed, so the difference reads without reading.
[RequireComponent(typeof(UIDocument))]
public class CharacterSelectController : MonoBehaviour
{
    [SerializeField] private CharacterDefinition[] characters;
    [SerializeField] private string firstLevel = "Level0";
    [SerializeField] private string titleScene = "Title";
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip startSound;

    private const float ManaPerOrb = 10f;
    private VisualElement[] cards, spellIcons;
    private float nextFlash;
    private bool starting;
    public int Selected { get; private set; }
    public CharacterDefinition[] Characters => characters;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        cards = new VisualElement[characters.Length];
        spellIcons = new VisualElement[characters.Length];
        for (int i = 0; i < characters.Length; i++)
        {
            int index = i;
            var c = characters[i];
            cards[i] = root.Q($"card-{i}");
            cards[i].Q("card-portrait").style.backgroundImage = new StyleBackground(c.Portrait);
            cards[i].Q<Label>("card-name").text = c.DisplayName;
            cards[i].Q<Label>("card-description").text = c.Description;
            cards[i].RegisterCallback<ClickEvent>(_ => Select(index));
            ShowStats(cards[i], c, out spellIcons[i]);
        }
        root.Q<Button>("start").clicked += StartGame;
        Select(0, silent: true);
    }

    private void Update()
    {
        if (GameInput.NumberPressed(1)) Select(0);
        if (GameInput.NumberPressed(2)) Select(1);
        if (GameInput.LeftPressed) Select(Selected - 1);
        if (GameInput.RightPressed) Select(Selected + 1);
        if (GameInput.ConfirmPressed) StartGame();
        if (GameInput.BackPressed && !starting) SceneManager.LoadScene(titleScene); // back to the slots
        FlashSpell();
    }

    // Hearts for health, orbs for magic, and the spell's picture and name, from the hero's player prefab.
    private static void ShowStats(VisualElement card, CharacterDefinition hero, out VisualElement spellIcon)
    {
        spellIcon = card.Q("card-spell-icon");
        if (hero.Prefab == null) return;
        if (hero.Prefab.TryGetComponent(out Health health))
            for (int i = 0; i < health.Max; i++) card.Q("card-hearts").Add(Pip("heart"));
        if (hero.Prefab.TryGetComponent(out Mana mana))
            for (int i = 0; i < Mathf.RoundToInt(mana.Max / ManaPerOrb); i++) card.Q("card-magic").Add(Pip("magic-icon"));
        if (hero.Prefab.TryGetComponent(out SpellAbility spell))
        {
            spellIcon.style.backgroundImage = new StyleBackground(spell.Icon);
            card.Q<Label>("card-spell-name").text = spell.SpellName;
        }
    }

    private static VisualElement Pip(string className)
    {
        var pip = new VisualElement { pickingMode = PickingMode.Ignore };
        pip.AddToClassList(className);
        return pip;
    }

    // The chosen hero's spell icon pulses every time its spell would be ready again: Marina's is quicker.
    private void FlashSpell()
    {
        if (Time.unscaledTime < nextFlash || spellIcons == null || spellIcons[Selected] == null) return;
        var icon = spellIcons[Selected];
        var spell = characters[Selected].Prefab != null ? characters[Selected].Prefab.GetComponent<SpellAbility>() : null;
        nextFlash = Time.unscaledTime + Mathf.Max(0.3f, spell != null ? spell.Cooldown : 0.6f) + 0.4f;
        icon.AddToClassList("cast");
        icon.schedule.Execute(() => icon.RemoveFromClassList("cast")).StartingIn(110);
    }

    public void Select(int index) => Select(index, silent: false);

    private void Select(int index, bool silent)
    {
        index = Mathf.Clamp(index, 0, characters.Length - 1);
        if (!silent && index != Selected) AudioManager.Play(selectSound);
        Selected = index;
        for (int i = 0; i < cards.Length; i++)
            cards[i].EnableInClassList("selected", i == Selected); // Hud.uss-style class toggling
    }

    public void StartGame()
    {
        if (starting) return;
        starting = true;
        GameSession.NewGame(characters[Selected]);
        SaveSystem.Autosave(firstLevel, ""); // a new adventure gets its save slot right away
        StartCoroutine(StartAfterSound());
    }

    // Loading a scene destroys this scene's AudioManager, which would cut the sound off,
    // so let the "start" blip play for a moment first.
    private IEnumerator StartAfterSound()
    {
        AudioManager.Play(startSound);
        yield return new WaitForSeconds(0.25f);
        SceneManager.LoadScene(firstLevel);
    }
}
