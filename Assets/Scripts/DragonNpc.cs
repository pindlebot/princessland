using UnityEngine;

// Amethyra, the friendly dragon of the castle grounds. Walk up and press E to talk:
// the first time she introduces herself, after that she has a shorter chat.
// She's an IInteractable, so the existing prompt and E-to-use system just work.
public class DragonNpc : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcName = "Amethyra";
    [SerializeField] private Sprite portrait;
    [SerializeField] private DialogueLine[] introduction;
    [SerializeField] private DialogueLine[] laterChat;

    [Header("Animation")]
    [SerializeField] private SpriteFlipbook flipbook;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] talkFrames;
    [SerializeField] private float idleFps = 3f;
    [SerializeField] private float talkFps = 8f;

    // Stored in GameSession so she remembers you after a trip into the castle and back.
    private string MetFlag => "met:" + npcName;
    public bool HasMet => GameSession.Flags.Contains(MetFlag);
    public bool IsTalking { get; private set; }

    public Vector3 Position => transform.position;
    public string Prompt => $"Talk to {npcName}";
    public bool CanInteract => !DialogueController.IsOpen;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    public string Interact(GameObject player)
    {
        DialogueController.Instance.Begin(HasMet ? laterChat : introduction, npcName, portrait,
                                          LevelBootstrap.Current.Character, () => GameSession.Flags.Add(MetFlag));
        return null; // the dialogue box does the talking, no toast needed
    }

    private void Update()
    {
        // Move her mouth while her lines are being typed out.
        bool talking = DialogueController.Instance != null && DialogueController.Instance.NpcIsTalking
                       && DialogueController.Instance.NpcName == npcName;
        if (talking != IsTalking)
        {
            IsTalking = talking;
            flipbook.Play(talking ? talkFrames : idleFrames, talking ? talkFps : idleFps);
        }
    }
}
