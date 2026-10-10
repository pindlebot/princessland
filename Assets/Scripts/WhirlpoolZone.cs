using UnityEngine;

// A swirling pool on the floor that lasts a few seconds. Monsters inside are dragged toward
// the middle (so they struggle to reach the princess) and splashed for damage every
// `tickSeconds`. The sprite lies flat and slowly turns, on top of its own spin animation.
public class WhirlpoolZone : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float lifetime = 3.5f;
    [SerializeField] private float pullSpeed = 2.5f;   // metres per second toward the middle
    [SerializeField] private float tickSeconds = 0.7f;
    [SerializeField] private float turnSpeed = 120f;   // degrees per second
    [SerializeField] private GameObject splashPrefab;  // the Tidal Orb's splash, on each hit
    [SerializeField] private AudioClip splashSound;

    private int damage = 1;
    private float nextTick, endsAt;

    public float Radius => radius;
    public float TickSeconds => tickSeconds;

    public void Launch(int damage) => this.damage = damage;

    private void Start()
    {
        endsAt = Time.time + lifetime;
        nextTick = Time.time + tickSeconds;
        transform.localScale = Vector3.one * radius / 2f; // the sprite is 4 units across at scale 1
        SpellTargets.HitNear(transform.position, radius, damage, SpellElement.Water); // brambles bloom, braziers fill
    }

    private void Update()
    {
        // Lying flat (rotated 90° about x), so spinning around the floor's up axis is world-space.
        transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime, Space.World);

        bool tick = Time.time >= nextTick;
        if (tick) nextTick += tickSeconds;
        foreach (var enemy in HeroAbility.EnemiesNear(transform.position, radius))
        {
            var toMiddle = transform.position - enemy.transform.position;
            if (HeroAbility.FlatDistance(transform.position, enemy.transform.position) > 0.3f)
                HeroAbility.Push(enemy, toMiddle, pullSpeed * Time.deltaTime);
            if (tick) Splash(enemy);
        }

        // Shrinking away in the last half second.
        float left = endsAt - Time.time;
        if (left < 0.5f) transform.localScale = Vector3.one * radius / 2f * Mathf.Max(0f, left / 0.5f);
        if (left <= 0f) Destroy(gameObject);
    }

    private void Splash(EnemyAI enemy)
    {
        enemy.GetComponent<Health>().TakeDamage(damage);
        if (splashPrefab != null) Instantiate(splashPrefab, enemy.transform.position, Quaternion.identity);
        AudioManager.Play(splashSound, 0.6f);
    }
}
