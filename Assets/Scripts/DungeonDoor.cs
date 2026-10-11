using UnityEngine;

// A wooden door between rooms (map tile 'd'), or a locked one ('k') that needs the Rusty Key.
// Press E to open it: the door swings back against its frame and stops blocking the way.
// Opened doors stay open when you come back (persistentId, like chests).
public class DungeonDoor : MonoBehaviour, IInteractable
{
    public const string KeyFlag = "key:rusty";

    [SerializeField] private bool locked;
    [SerializeField] private string persistentId;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Collider blocker;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip lockedSound;

    public bool IsOpen { get; private set; }
    public bool IsLocked => locked;
    public Vector3 Position => transform.position;
    public string Prompt => locked && !GameSession.Flags.Contains(KeyFlag) ? "Try the door" : "Open the door";
    public bool CanInteract => !IsOpen;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start()
    {
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId)) SetOpen();
    }

    public string Interact(GameObject player)
    {
        if (locked && !GameSession.Flags.Contains(KeyFlag))
        {
            ActionFeedback.Fail(FailReason.Locked, sound: lockedSound);
            return "It's locked tight. There must be a key somewhere in the maze...";
        }
        AudioManager.Play(openSound);
        SetOpen();
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        return locked ? "The Rusty Key fits! Click!" : null;
    }

    private void SetOpen()
    {
        IsOpen = true;
        sprite.sprite = openSprite;
        blocker.enabled = false;
    }
}
