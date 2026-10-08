using System;
using UnityEngine;
using UnityEngine.UIElements;

// One line of a conversation. [Serializable] lets Unity show and save arrays of these
// in the Inspector (see DragonNpc's Introduction).
[Serializable]
public struct DialogueLine
{
    public bool heroSpeaks;        // false = the NPC is speaking
    [TextArea] public string text; // "{hero}" is replaced with the hero's name
}

// Shows a conversation in the HUD's dialogue box (Hud.uxml #dialogue), one line at a
// time, typing each line out letter by letter. E / Space / Enter / click finishes the
// line, then moves to the next. The game pauses while it's open (Time.timeScale = 0),
// so typing and blinking use *unscaled* time, which keeps running during a pause.
[RequireComponent(typeof(UIDocument))]
public class DialogueController : MonoBehaviour
{
    [SerializeField] private float lettersPerSecond = 45f;
    [SerializeField] private AudioClip npcVoice;
    [SerializeField] private AudioClip heroVoice;
    [SerializeField] private int lettersPerBlip = 3;

    public static DialogueController Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.lines != null;

    // True while open, and on the frame it closes: the key press that closed the dialogue
    // shouldn't also cast a spell or start the conversation again.
    public static bool BlocksInput => IsOpen || (Instance != null && Time.frameCount == Instance.closedFrame);

    public string NpcName { get; private set; }
    public bool NpcIsTalking => IsOpen && !lines[index].heroSpeaks && Typing;

    private VisualElement box, portrait;
    private Label speaker, text, hint;
    private DialogueLine[] lines;
    private int index;
    private float lettersShown;
    private int blipsPlayed;
    private int openedFrame, closedFrame = -1;
    private string heroName, heroShortName;
    private Sprite npcPortrait, heroPortrait;
    private Action onFinished;

    private string FullText => lines[index].text.Replace("{hero}", heroShortName);
    private bool Typing => (int)lettersShown < FullText.Length;

    private void Awake() => Instance = this;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        box = root.Q("dialogue");
        portrait = root.Q("dialogue-portrait");
        speaker = root.Q<Label>("dialogue-speaker");
        text = root.Q<Label>("dialogue-text");
        hint = root.Q<Label>("dialogue-hint");
    }

    public void Begin(DialogueLine[] conversation, string npc, Sprite npcPicture,
                      CharacterDefinition hero, Action finished = null)
    {
        if (IsOpen || conversation == null || conversation.Length == 0) return;
        lines = conversation;
        index = 0;
        NpcName = npc;
        npcPortrait = npcPicture;
        heroName = hero.DisplayName;
        heroShortName = heroName.Split(" the ")[0]; // "Aldric the Wizard" -> "Aldric"
        heroPortrait = hero.Portrait;
        onFinished = finished;
        openedFrame = Time.frameCount;

        Time.timeScale = 0f; // pause the world
        box.AddToClassList("open");
        ShowLine();
    }

    // Finish typing the current line, or move on to the next one (closing after the last).
    public void Advance()
    {
        if (!IsOpen) return;
        if (Typing)
        {
            lettersShown = FullText.Length;
            text.text = FullText;
            UpdateHint();
            return;
        }
        index++;
        if (index >= lines.Length) Close();
        else ShowLine();
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Typing)
        {
            lettersShown += lettersPerSecond * Time.unscaledDeltaTime;
            int shown = Mathf.Min((int)lettersShown, FullText.Length);
            text.text = FullText.Substring(0, shown);
            if (shown / lettersPerBlip > blipsPlayed) // a little voice blip every few letters
            {
                blipsPlayed = shown / lettersPerBlip;
                AudioManager.Play(lines[index].heroSpeaks ? heroVoice : npcVoice, 0.5f);
            }
            UpdateHint();
        }

        // Ignore the press that opened the dialogue (it's the same E that talked to the NPC).
        if (Time.frameCount != openedFrame && AdvancePressed())
            Advance();
    }

    private static bool AdvancePressed() =>
        Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
        Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
        Input.GetMouseButtonDown(0);

    private void ShowLine()
    {
        var line = lines[index];
        lettersShown = 0f;
        blipsPlayed = 0;
        text.text = "";
        hint.text = "";
        speaker.text = line.heroSpeaks ? heroName : NpcName;
        portrait.style.backgroundImage = new StyleBackground(line.heroSpeaks ? heroPortrait : npcPortrait);
        box.EnableInClassList("hero-speaking", line.heroSpeaks); // Hud.uss puts the portrait on the right
    }

    private void UpdateHint()
    {
        hint.text = Typing ? "" : index < lines.Length - 1 ? "[E] continue" : "[E] close";
    }

    private void Close()
    {
        lines = null;
        closedFrame = Time.frameCount;
        box.RemoveFromClassList("open");
        Time.timeScale = 1f; // unpause
        var finished = onFinished;
        onFinished = null;
        finished?.Invoke();
    }

    // If the scene changes mid-conversation, don't leave the next scene paused.
    private void OnDestroy()
    {
        if (lines != null) Time.timeScale = 1f;
    }
}
