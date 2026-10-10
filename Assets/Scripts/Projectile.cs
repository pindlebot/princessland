using UnityEngine;

// A spell projectile (the Fireball, the Tidal Orb, ...): flies forward, damages the first
// enemy it touches (or wakes/breaks an ISpellTarget), and bursts (spawning its impact effect) on hitting anything solid.
// The spells differ only in their prefab's sprites, lights and impact effect.
// Trigger events need a Rigidbody on at least one side, so the prefab
// has a kinematic Rigidbody plus a trigger SphereCollider.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 14f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private SpellElement element = SpellElement.Arcane; // Fire or Water: brambles and braziers care

    private bool exploded;

    // Called by SpellAbility right after spawning, so bonuses (e.g. an equipped ring) apply.
    public void Launch(int damage) => this.damage = damage;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Destroy() only happens at the end of the frame, so without this guard a projectile
        // touching two colliders at once would hit twice.
        if (exploded) return;
        if (other.GetComponentInParent<PlayerController>() != null) return;
        if (other.isTrigger) return;
        if (LevelMap.IsWaterLayer(other.gameObject.layer)) return; // flies over the pond and the sea
        exploded = true;

        var enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy != null)
            enemy.GetComponent<Health>().TakeDamage(damage);
        // Pots, sleepy trees, brambles, braziers and the like.
        var target = other.GetComponentInParent<ISpellTarget>();
        if (target != null) target.OnSpellHit(damage, element);

        // The impact is its own object, so it keeps playing after the projectile is gone.
        if (impactPrefab != null)
            Instantiate(impactPrefab, transform.position, Quaternion.identity);
        AudioManager.Play(impactSound);
        Destroy(gameObject);
    }
}
