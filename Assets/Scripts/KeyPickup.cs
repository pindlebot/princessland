using UnityEngine;

// The Rusty Key (map tile 'y'), hidden deep in the dungeon maze. It opens the locked
// treasure-room door. Kept as a flag ("key:rusty") rather than a bag item, so it can't
// be dropped or lost.
public class KeyPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string persistentId;
    [SerializeField] private AudioClip pickupSound;

    public Vector3 Position => transform.position;
    public string Prompt => "Pick up the key";
    public bool CanInteract => true;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start()
    {
        if (GameSession.Flags.Contains(DungeonDoor.KeyFlag)) Destroy(gameObject);
    }

    public string Interact(GameObject player)
    {
        GameSession.Flags.Add(DungeonDoor.KeyFlag);
        AudioManager.Play(pickupSound);
        Destroy(gameObject);
        return "You found the Rusty Key! I wonder which door it opens...";
    }
}
