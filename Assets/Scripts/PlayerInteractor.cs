using System;
using UnityEngine;

// Finds the nearest interactable within reach each frame (the HUD shows its prompt)
// and uses it when the player presses E.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float range = 1.8f;

    // The thing the player would use if they pressed E right now (null if none).
    // Computed when asked rather than cached in Update, so readers like the HUD never see
    // last frame's answer (Unity doesn't guarantee which script's Update runs first).
    public IInteractable Current =>
        (GameManager.Instance != null && GameManager.Instance.IsGameOver) || DialogueController.IsOpen
            ? null
            : FindNearest();

    // Raised with the interactable's message after using it. HudController shows it.
    public event Action<string> Interacted;

    private void Update()
    {
        if (GameInput.InteractPressed && !DialogueController.BlocksInput)
            TryInteract();
    }

    // Public so tests (or a future "use" button) can trigger it too.
    public bool TryInteract()
    {
        var target = Current;
        if (target == null) return false;

        string message = target.Interact(gameObject);
        Interacted?.Invoke(message);
        return true;
    }

    private IInteractable FindNearest()
    {
        IInteractable best = null;
        float bestDistance = range;
        foreach (var candidate in Interactables.All)
        {
            if (!candidate.CanInteract) continue;
            Vector3 offset = candidate.Position - transform.position;
            offset.y = 0f; // only horizontal distance matters
            float distance = offset.magnitude;
            if (distance <= bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }
}
