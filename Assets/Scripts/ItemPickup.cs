using UnityEngine;

// An item lying on the floor. It's an IInteractable, so the existing E-to-use system
// (prompt, toast, PlayerInteractor) works for it with no extra code.
public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition item;
    [Tooltip("Unique per placed pickup (set by DungeonBuilder), so it stays picked up when you come back.")]
    [SerializeField] private string persistentId;
    [SerializeField] private Transform visual; // bobs up and down to catch the eye
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private AudioClip pickupSound;

    private Vector3 visualStart;
    private bool pickedUp; // Destroy() waits until the end of the frame; don't allow a second pickup meanwhile

    public ItemDefinition Item => item;
    public Vector3 Position => transform.position;
    public string Prompt => $"Pick up {item.DisplayName}";
    public bool CanInteract => !pickedUp;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start()
    {
        visualStart = visual.localPosition;
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId))
        {
            pickedUp = true; // already in your bag from an earlier visit
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    public string Interact(GameObject player)
    {
        if (pickedUp) return null;
        if (!player.GetComponent<Inventory>().Add(item))
            return "Your bag is full.";

        pickedUp = true;
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        AudioManager.Play(pickupSound);
        Destroy(gameObject);
        return $"Picked up {item.DisplayName}. Press I to open your inventory.";
    }
}
