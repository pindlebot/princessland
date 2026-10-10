using System.Collections;
using UnityEngine;

// A wall of thorny brambles across the way. Either spell clears it: the wizard's fire burns it to
// ash, the princess's water makes it bloom into flowers and part. (Both heroes always have a way
// through; only the look differs.) A cleared bramble stays cleared: it remembers by where it stands,
// like a chest or a pot.
public class Bramble : MonoBehaviour, ISpellTarget
{
    public const string ClearedCounter = "brambles_cleared";

    [Tooltip("Unique per placed bramble (set by DungeonBuilder), so it stays cleared when you come back.")]
    [SerializeField] private string persistentId;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Collider solid;
    [SerializeField] private Sprite[] burnFrames;
    [SerializeField] private Sprite[] bloomFrames;
    [SerializeField] private float burnFps = 8f;
    [SerializeField] private float bloomFps = 6f;
    [SerializeField] private GameObject sparkle;
    [SerializeField] private AudioClip burnSound;
    [SerializeField] private AudioClip bloomSound;

    public bool IsCleared { get; private set; }
    public SpellElement ClearedBy { get; private set; }

    private void Start()
    {
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId))
        {
            IsCleared = true; // cleared on an earlier visit
            Destroy(gameObject);
        }
    }

    public void OnSpellHit(int damage, SpellElement element) => Clear(element);

    // Water makes it bloom; anything else (fire, plain magic) burns it.
    public void Clear(SpellElement element)
    {
        if (IsCleared) return;
        IsCleared = true;
        ClearedBy = element == SpellElement.Water ? SpellElement.Water : SpellElement.Fire;
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        GameSession.AddToCounter(ClearedCounter);
        FirstSteps.Complete(FirstStep.Obstacle);
        if (solid != null) solid.enabled = false; // the way is open straight away
        NavGrid.MarkDirty(); // and monsters can path through it
        AudioManager.Play(ClearedBy == SpellElement.Water ? bloomSound : burnSound);
        if (sparkle != null && ClearedBy == SpellElement.Water)
            Instantiate(sparkle, transform.position + Vector3.up, Quaternion.identity);
        StartCoroutine(Fade(ClearedBy == SpellElement.Water ? bloomFrames : burnFrames, ClearedBy == SpellElement.Water ? bloomFps : burnFps));
    }

    private IEnumerator Fade(Sprite[] frames, float fps)
    {
        foreach (var frame in frames)
        {
            sprite.sprite = frame;
            yield return new WaitForSeconds(1f / fps);
        }
        yield return new WaitForSeconds(0.8f); // the ash heap or flower bed lingers a moment
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            var c = sprite.color;
            c.a = 1f - t / 0.5f;
            sprite.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
