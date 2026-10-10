using UnityEngine;

// A monster's thrown spell (the dark mermaids' bolt): the mirror image of the hero's
// Projectile. It flies straight, slowly enough to step out of the way, hurts the hero if it
// touches her, passes through other monsters and over water, and bursts on anything solid.
[RequireComponent(typeof(Rigidbody))]
public class EnemyBolt : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private AudioClip impactSound;

    private int damage = 1;
    private bool exploded;

    // Called by EnemyAI right after spawning it, pointed at the hero.
    public void Launch(int damage) => this.damage = damage;

    private void Start() => Destroy(gameObject, lifetime);

    private void Update() => transform.position += transform.forward * speed * Time.deltaTime;

    private void OnTriggerEnter(Collider other)
    {
        if (exploded || other.isTrigger) return;
        if (LevelMap.IsWaterLayer(other.gameObject.layer)) return;     // skims over the sea
        if (other.GetComponentInParent<EnemyAI>() != null) return;     // never hits its own side
        exploded = true;

        var hero = other.GetComponentInParent<PlayerController>();
        if (hero != null) hero.GetComponent<Health>().TakeDamage(damage); // shields and Gentle Mode apply

        if (impactPrefab != null) Instantiate(impactPrefab, transform.position, Quaternion.identity);
        AudioManager.Play(impactSound, 0.7f);
        Destroy(gameObject);
    }
}
