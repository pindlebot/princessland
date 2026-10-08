using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// The character select screen (Assets/UI/CharacterSelect.uxml). Click a card or press
// 1/2 (or Left/Right, d-pad) to choose, then Start or Enter (A). Esc (B) goes back. The choice goes into GameSession
// so the levels know who to spawn.
[RequireComponent(typeof(UIDocument))]
public class CharacterSelectController : MonoBehaviour
{
    [SerializeField] private CharacterDefinition[] characters;
    [SerializeField] private string firstLevel = "Level0";
    [SerializeField] private string titleScene = "Title";
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip startSound;

    private VisualElement[] cards;
    private bool starting;
    public int Selected { get; private set; }
    public CharacterDefinition[] Characters => characters;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        cards = new VisualElement[characters.Length];
        for (int i = 0; i < characters.Length; i++)
        {
            int index = i;
            var c = characters[i];
            cards[i] = root.Q($"card-{i}");
            cards[i].Q("card-portrait").style.backgroundImage = new StyleBackground(c.Portrait);
            cards[i].Q<Label>("card-name").text = c.DisplayName;
            cards[i].Q<Label>("card-description").text = c.Description;
            cards[i].RegisterCallback<ClickEvent>(_ => Select(index));
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
