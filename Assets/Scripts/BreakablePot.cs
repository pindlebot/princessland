using UnityEngine;

// A clay pot: zap it and it smashes, spilling a few coins (and now and then something good to
// eat). Pots stay broken for good, so you can't farm them. Like a chest, each one has an id
// (set by DungeonBuilder from where it stands) that goes into GameSession's "used" flags.
// Pots count toward "pots_broken" (an easter egg waits at 25).
//
// It has no Health of its own: one hit is enough, so a spell's damage doesn't matter. Loot.cs
// ("anything with Health could drop loot, a breakable pot say") was the idea; a pot is simpler
// than a monster, so it just does its own dropping.
public class BreakablePot : MonoBehaviour, ISpellTarget
{
    public const string BrokenCounter = "pots_broken";

    [Tooltip("Unique per placed pot (set by DungeonBuilder), so it stays broken when you come back.")]
    [SerializeField] private string persistentId;
    [SerializeField] private CoinPickup coinPrefab;
    [SerializeField] private int minCoins = 1;
    [SerializeField] private int maxCoins = 3;
    [Tooltip("Sometimes it hides one of these (a Healing Apple's pickup).")]
    [SerializeField] private GameObject treatPrefab;
    [Range(0f, 1f)] [SerializeField] private float treatChance = 0.2f;
    [SerializeField] private GameObject shatterEffect; // the flying pieces
    [SerializeField] private AudioClip breakSound;

    private bool broken;

    public bool IsBroken => broken;

    private void Start()
    {
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId))
        {
            broken = true; // smashed on an earlier visit
            Destroy(gameObject);
        }
    }

    public void OnSpellHit(int damage, SpellElement element) => Break();

    public void Break()
    {
        if (broken) return;
        broken = true;
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        GameSession.AddToCounter(BrokenCounter);
        AudioManager.Play(breakSound);

        Vector3 ground = new Vector3(transform.position.x, 0f, transform.position.z);
        if (shatterEffect != null) Instantiate(shatterEffect, ground, Quaternion.identity);
        if (coinPrefab != null)
        {
            int coins = Random.Range(minCoins, maxCoins + 1);
            for (int i = 0; i < coins; i++)
                Instantiate(coinPrefab, ground + Vector3.up * 0.3f, Quaternion.identity).Value = 1;
        }
        if (treatPrefab != null && Random.value < treatChance)
            Instantiate(treatPrefab, ground, Quaternion.identity);
        NavGrid.MarkDirty();
        Destroy(gameObject);
    }
}
