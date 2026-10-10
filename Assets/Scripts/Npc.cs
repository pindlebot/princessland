using System;
using UnityEngine;

// A friendly character you walk up to and press E to talk to: Amethyra the dragon, Coralie
// the mermaid, Bonesy the skeleton. Each NPC has a list of conversations, and talking picks
// the first one whose conditions are met, so what they say depends on what you've done:
//
//   requires   a condition that must hold ("" = always ok): a flag like "found:frog", a counter like
//              "talks:Amethyra>=9", or an item like "has:fairy_lantern" (see Condition.cs)
//   notIf      a condition that must NOT hold, e.g. "met:Coralie" (so an introduction plays once)
//   sets       flags to set when the conversation ends, e.g. "met:Coralie" (comma-separated)
//   giveGold   coins handed over when it ends (a thank-you present)
//   giveItem   an item id handed over when it ends (a reward: it goes in the bag, or the treasures tab)
//   smallTalk  if the first match is small talk, the NPC takes turns through *all* the
//              matching small-talk conversations, one per visit (counted in GameSession,
//              starting from the first)
//
// Flags live in GameSession, so they're saved and survive going through doors.
//
// A Merchant is an Npc that also sells something (Barnaby Badger in the village).
public class Npc : MonoBehaviour, IInteractable
{
    [Serializable]
    public class Conversation
    {
        public string requires = "";
        public string notIf = "";
        public string sets = "";
        public int giveGold;
        public string giveItem = "";
        public bool smallTalk;
        public DialogueLine[] lines;

        public bool Matches() =>
            Condition.Met(requires) && (string.IsNullOrWhiteSpace(notIf) || !Condition.Met(notIf)) && HasRoomForGift();

        // A reward that wouldn't fit in the bag waits (this conversation doesn't match) until there's room.
        private bool HasRoomForGift()
        {
            if (string.IsNullOrEmpty(giveItem)) return true;
            var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
            if (player == null || !player.TryGetComponent(out Inventory bag)) return true;
            var item = bag.Database.Find(giveItem);
            return item == null || item.IsKeyItem || bag.Bag.Count < bag.Capacity;
        }
    }

    [SerializeField] private string npcName = "Amethyra";
    [SerializeField] protected Sprite portrait;
    [SerializeField] protected AudioClip voice;         // the little blip while they talk
    [SerializeField] private AudioClip giftSound;
    [SerializeField] private Conversation[] conversations;
    [Tooltip("Where you stand to talk, relative to the NPC (e.g. on the shore beside a mermaid).")]
    [SerializeField] private Vector3 talkOffset;

    [Header("Animation")]
    [SerializeField] private SpriteFlipbook flipbook;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] talkFrames;
    [SerializeField] private float idleFps = 3f;
    [SerializeField] private float talkFps = 8f;

    public string Name => npcName;
    public bool HasMet => GameSession.Flags.Contains("met:" + npcName);
    public bool IsTalking { get; private set; }

    public Vector3 Position => transform.position + talkOffset;
    public virtual string Prompt => $"Talk to {npcName}";
    public bool CanInteract => !DialogueController.IsOpen;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    // Which conversation talking would start right now (null if none matches).
    public Conversation Next()
    {
        var first = Array.Find(conversations, c => c.Matches());
        if (first == null || !first.smallTalk) return first;
        var pool = Array.FindAll(conversations, c => c.smallTalk && c.Matches());
        return pool[GameSession.GetCounter("smalltalk:" + npcName) % pool.Length];
    }

    public virtual string Interact(GameObject player)
    {
        var conversation = Next();
        if (conversation == null) return null;
        DialogueController.Instance.Begin(conversation.lines, npcName, portrait, LevelBootstrap.Current.Character,
                                          () => Finish(conversation), voice);
        return null; // the dialogue box does the talking, no toast needed
    }

    // One line from the NPC in the dialogue box, outside of any conversation (a shopkeeper's
    // "thank you!"). "{hero}" works here too.
    protected void Say(string text, Action finished = null)
    {
        var line = new[] { new DialogueLine { heroSpeaks = false, text = text } };
        DialogueController.Instance.Begin(line, npcName, portrait, LevelBootstrap.Current.Character, finished, voice);
    }

    private void Finish(Conversation conversation)
    {
        GameSession.AddToCounter("talks:" + npcName);
        if (conversation.smallTalk) GameSession.AddToCounter("smalltalk:" + npcName);
        foreach (var flag in (conversation.sets ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            GameSession.Flags.Add(flag.Trim());
        if (conversation.giveGold > 0)
        {
            GameSession.Progress.AddGold(conversation.giveGold);
            AudioManager.Play(giftSound);
        }
        if (!string.IsNullOrEmpty(conversation.giveItem))
        {
            var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
            if (player != null && player.TryGetComponent(out Inventory bag) && bag.Add(conversation.giveItem))
                AudioManager.Play(giftSound);
        }
    }

    private void Update()
    {
        if (flipbook == null) return;
        // Move their mouth while their lines are being typed out.
        bool talking = DialogueController.Instance != null && DialogueController.Instance.NpcIsTalking
                       && DialogueController.Instance.NpcName == npcName;
        if (talking != IsTalking)
        {
            IsTalking = talking;
            flipbook.Play(talking ? talkFrames : idleFrames, talking ? talkFps : idleFps);
        }
    }
}
