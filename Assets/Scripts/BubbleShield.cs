using UnityEngine;

// The princess's step-3 ability: a bubble around her that takes the next few hits instead
// of her (monsters, traps, lava), then pops. It also pops by itself after `duration`.
// It works through Health.TryBlock, so Health doesn't need to know shields exist.
[RequireComponent(typeof(Health))]
public class BubbleShield : HeroAbility
{
    [SerializeField] private int hits = 2;
    [SerializeField] private float duration = 8f;
    [SerializeField] private GameObject bubble;    // a child of the player, hidden until used
    [SerializeField] private GameObject popPrefab; // the Tidal Orb's splash
    [SerializeField] private AudioClip blockSound;

    private Health health;
    private float endsAt;

    public int HitsLeft { get; private set; }
    public bool IsUp => HitsLeft > 0;

    protected override void Awake()
    {
        base.Awake();
        health = GetComponent<Health>();
        health.TryBlock = Block;
        if (bubble != null) bubble.SetActive(false);
    }

    protected override bool Activate()
    {
        HitsLeft = hits;
        endsAt = Time.time + duration;
        if (bubble != null) bubble.SetActive(true);
        return true;
    }

    protected override void Update()
    {
        base.Update();
        if (IsUp && Time.time >= endsAt) Pop();
    }

    private bool Block()
    {
        if (!IsUp) return false;
        HitsLeft--;
        AudioManager.Play(blockSound);
        if (HitsLeft == 0) Pop();
        return true;
    }

    private void Pop()
    {
        HitsLeft = 0;
        if (bubble != null) bubble.SetActive(false);
        if (popPrefab != null) Instantiate(popPrefab, transform.position, Quaternion.identity);
    }
}
