using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The quest log (press J, or click the right stick): every quest that has started, as a card with
// the giver's picture, the title, the step you're on, and a picture of what to do next, so a
// player who can't read yet can still tell who to go back to and what to look for. Finished
// quests stay at the bottom with a gold star.
//
// Like the skill tree, the cards are built here from QuestCatalog (Hud.uxml only has the empty
// panel). Quests have no state of their own: QuestCatalog works progress out from flags,
// counters and items, so this view just draws it, and toasts "New quest!" / "Quest complete!"
// the first time a quest starts or finishes (remembered in a flag so it never repeats).
[RequireComponent(typeof(UIDocument), typeof(HudController))]
public class QuestLogView : MonoBehaviour
{
    [SerializeField] private QuestPictures pictures;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip newQuestSound;
    [SerializeField] private AudioClip doneSound;

    private const float ToastGap = 3.4f; // a touch longer than a toast stays up
    private const string EmptyText = "No quests yet. Talk to the people you meet: they might need a hand!";

    private VisualElement panel, list;
    private Label empty;
    private HudController hud;
    private Inventory inventory;
    private readonly Queue<(string text, AudioClip sound)> announcements = new Queue<(string, AudioClip)>();
    private float nextAnnouncementAt;
    private string shown = "";  // what the cards currently show, to rebuild only when something changed

    public bool IsOpen => panel != null && panel.ClassListContains("open");
    public int CardCount => list != null ? list.childCount : 0;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q("quests");
        list = root.Q("quest-list");
        empty = root.Q<Label>("quest-empty");
        hud = GetComponent<HudController>();
        var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        inventory = player != null ? player.GetComponent<Inventory>() : null;
        panel.Q(className: "inventory-close").RegisterCallback<ClickEvent>(_ => SetOpen(false));
        Refresh();
    }

    private void Update()
    {
        if (GameInput.QuestLogPressed && !GameInput.GameplayBlocked)
        {
            SetOpen(!IsOpen);
            AudioManager.Play(openSound, 0.6f);
        }
        Announce();
        if (IsOpen) Refresh();
    }

    public void SetOpen(bool open)
    {
        panel.EnableInClassList("open", open);
        if (open) Refresh();
    }

    // The first time each quest starts or finishes, say so (one message at a time).
    private void Announce()
    {
        foreach (var quest in QuestCatalog.All)
        {
            var state = QuestCatalog.StateOf(quest);
            string seen = "quest:seen:" + quest.Id, done = "quest:done:" + quest.Id;
            if (state != QuestState.Hidden && GameSession.Flags.Add(seen))
                announcements.Enqueue(($"New quest: {quest.Title}! Press {GameInput.QuestLogKeyName} to see it.", newQuestSound));
            if (state == QuestState.Done && GameSession.Flags.Add(done))
                announcements.Enqueue(($"Quest complete: {quest.Title}!", doneSound));
        }
        if (announcements.Count > 0 && Time.unscaledTime >= nextAnnouncementAt && !DialogueController.IsOpen)
        {
            var (text, sound) = announcements.Dequeue();
            hud.ShowToast(text);
            AudioManager.Play(sound);
            nextAnnouncementAt = Time.unscaledTime + ToastGap;
        }
    }

    // Redraws the cards, but only when what they say has changed (this runs every frame while open).
    private void Refresh()
    {
        var quests = QuestCatalog.Listed();
        string signature = "";
        foreach (var quest in quests)
        {
            var step = QuestCatalog.CurrentStep(quest);
            signature += $"{quest.Id}:{(step != null ? QuestCatalog.StepText(step) : "done")}|";
        }
        if (signature == shown) return;
        shown = signature;

        list.Clear();
        empty.style.display = quests.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        empty.text = EmptyText;
        foreach (var quest in quests) list.Add(BuildCard(quest));
    }

    private VisualElement BuildCard(QuestDefinition quest)
    {
        var step = QuestCatalog.CurrentStep(quest);
        bool done = step == null;

        var card = new VisualElement { pickingMode = PickingMode.Ignore };
        card.AddToClassList("quest-card");
        card.EnableInClassList("done", done);

        // Who gave it: their portrait (or, for a quest nobody gave, what it's about).
        var who = new VisualElement { pickingMode = PickingMode.Ignore };
        who.AddToClassList("quest-portrait");
        var sprite = quest.Giver.Length > 0 ? PictureFor("npc:" + quest.Giver) : PictureFor(quest.Steps[0].Icon);
        if (sprite != null) who.style.backgroundImage = new StyleBackground(sprite);
        card.Add(who);

        var body = new VisualElement { pickingMode = PickingMode.Ignore };
        body.AddToClassList("quest-body");
        body.Add(Text(quest.Title, "quest-title"));
        body.Add(Text(done ? "All done! Well done!" : QuestCatalog.StepText(step), "quest-step"));

        // One pip per step: gold once it's done, so progress shows without any reading.
        var pips = new VisualElement { pickingMode = PickingMode.Ignore };
        pips.AddToClassList("quest-pips");
        for (int i = 0; i < quest.Steps.Length; i++)
        {
            var pip = new VisualElement { pickingMode = PickingMode.Ignore };
            pip.AddToClassList("quest-pip");
            pip.EnableInClassList("done", Condition.Met(quest.Steps[i].Done));
            pips.Add(pip);
        }
        body.Add(pips);
        card.Add(body);

        // What to do next, as a picture (a star once everything's finished).
        var next = new VisualElement { pickingMode = PickingMode.Ignore };
        next.AddToClassList(done ? "quest-star" : "quest-next");
        var nextSprite = done ? null : PictureFor(step.Icon);
        if (nextSprite != null) next.style.backgroundImage = new StyleBackground(nextSprite);
        card.Add(next);
        return card;
    }

    // "item:egg", "npc:Pearl" or "icon:frog" -> its picture.
    private Sprite PictureFor(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (key.StartsWith("item:") && inventory != null)
            return inventory.Database.Find(key.Substring(5))?.Icon;
        return pictures != null ? pictures.Find(key) : null;
    }

    private static Label Text(string text, string cls)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.AddToClassList(cls);
        return label;
    }
}
