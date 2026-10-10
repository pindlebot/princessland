using System.Collections;
using UnityEngine;

// The wizard's step-3 ability: a fan of fire in front of him. Every monster inside the fan
// (in range, and within `halfAngle` of where he's facing) is burned and pushed back.
// No projectile: the hit is instant, and the bursts are just for show.
public class FlameWave : HeroAbility
{
    [SerializeField] private float range = 4.5f;
    [SerializeField] private float halfAngle = 50f;  // degrees either side of straight ahead
    [SerializeField] private int bonusDamage = 1;     // on top of the Fireball's damage
    [SerializeField] private float knockback = 1.5f;
    [SerializeField] private GameObject burstPrefab;  // the Fireball's impact burst

    public float Range => range;
    public float HalfAngle => halfAngle;

    protected override bool Activate()
    {
        // Point the fan at the nearest monster. (FaceFor turns the hero over the next few
        // frames, so use the direction to the target now rather than transform.forward.)
        var target = AimAtTarget();
        Vector3 forward = target != null ? target.transform.position - transform.position : transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.01f ? forward.normalized : transform.forward;

        foreach (var enemy in EnemiesNear(transform.position, range))
        {
            var to = enemy.transform.position - transform.position;
            to.y = 0f;
            if (Vector3.Angle(forward, to) > halfAngle) continue;
            enemy.GetComponent<Health>().TakeDamage(SpellDamage + bonusDamage);
            Push(enemy, to, knockback);
        }
        SpellTargets.HitNear(transform.position, range, SpellDamage, SpellElement.Fire, forward, halfAngle); // brambles, braziers, pots
        StartCoroutine(ShowFan(forward));
        return true;
    }

    // Two arcs of bursts rippling outward: near, then far.
    private IEnumerator ShowFan(Vector3 forward)
    {
        if (burstPrefab == null) yield break;
        float[] distances = { range * 0.45f, range * 0.85f };
        for (int ring = 0; ring < distances.Length; ring++)
        {
            int count = 3 + ring * 2;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, count == 1 ? 0.5f : i / (count - 1f)) * 0.85f;
                var p = transform.position + Quaternion.Euler(0f, angle, 0f) * forward * distances[ring];
                Instantiate(burstPrefab, new Vector3(p.x, 0.6f, p.z), Quaternion.identity);
            }
            yield return new WaitForSeconds(0.08f);
        }
    }
}
