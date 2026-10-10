using System.Collections;
using UnityEngine;

// A treasure chest: opens once, plays its lid animation, and restores the
// player's health and mana.
public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] openFrames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private GameObject openEffectPrefab; // sparkles + a flash of light
    [SerializeField] private AudioClip openSound;
    [SerializeField] private int gold = 25;
    [Tooltip("Unique per placed chest (set by DungeonBuilder), so it stays open when you come back.")]
    [SerializeField] private string persistentId;

    public bool IsOpen { get; private set; }

    public Vector3 Position => transform.position;
    public string Prompt => "Open chest";
    public bool CanInteract => !IsOpen;

    private void OnEnable() => Interactables.Register(this);

    private void Start()
    {
        // Opened on an earlier visit: show it open (the last frame of the lid animation), already empty.
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId))
        {
            IsOpen = true;
            spriteRenderer.sprite = openFrames[openFrames.Length - 1];
        }
    }
    private void OnDisable() => Interactables.Unregister(this);

    public string Interact(GameObject player)
    {
        if (IsOpen) return null;
        IsOpen = true;
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        FirstSteps.Complete(FirstStep.Treasure);

        StartCoroutine(PlayOpening());
        AudioManager.Play(openSound);
        if (openEffectPrefab != null)
            // Nudged toward the camera so the sparkles draw in front of the chest.
            Instantiate(openEffectPrefab, transform.position - Camera.main.transform.forward * 0.4f, Quaternion.identity);

        var health = player.GetComponent<Health>();
        var mana = player.GetComponent<Mana>();
        health.Heal(health.Max);
        mana.Refill();
        GameSession.Progress.AddGold(gold);
        return $"Found {gold} gold and a healing draught! Health and mana restored.";
    }

    // A coroutine runs across several frames: each "yield" pauses it until later.
    // Handy for a short one-off sequence like this, without needing an Animator.
    private IEnumerator PlayOpening()
    {
        foreach (var frame in openFrames)
        {
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(1f / fps);
        }
    }
}
