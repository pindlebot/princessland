using System.Collections;
using UnityEngine;

// One meteor, from warning to crater. Lives on its own object at the landing spot:
//   1. a circle on the floor grows to show where it will land (like the Slime King's slam,
//      but this time it's the monsters who should worry);
//   2. a fireball drops out of the sky onto it;
//   3. boom: everything inside the circle is hurt and thrown outward, and the screen shakes.
public class MeteorStrike : MonoBehaviour
{
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float fallSeconds = 0.7f;
    [SerializeField] private float fallHeight = 9f;
    [SerializeField] private float knockback = 1.2f;
    [SerializeField] private Transform warning;   // a flat circle, 2 units across at scale 1
    [SerializeField] private Transform rock;      // the falling fireball
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private AudioClip impactSound;

    private int damage = 3;

    public float Radius => radius;
    public float FallSeconds => fallSeconds;

    public void Launch(int damage) => this.damage = damage;

    private IEnumerator Start()
    {
        Vector3 ground = transform.position;
        // It comes in at a slant (from the top-left of the screen), which reads better than
        // straight down in an isometric view.
        Vector3 sky = ground + new Vector3(-3f, fallHeight, 3f);
        rock.position = sky;
        rock.rotation = Quaternion.LookRotation(ground - sky); // FaceTravelDirection points the art along this

        for (float t = 0f; t < fallSeconds; t += Time.deltaTime)
        {
            float f = t / fallSeconds;
            warning.localScale = Vector3.one * radius * Mathf.Lerp(0.3f, 1f, f);
            rock.position = Vector3.Lerp(sky, ground + Vector3.up * 0.5f, f * f); // speeding up as it falls
            yield return null;
        }
        Smash(ground);
        Destroy(gameObject);
    }

    private void Smash(Vector3 at)
    {
        foreach (var enemy in HeroAbility.EnemiesNear(at, radius))
        {
            enemy.GetComponent<Health>().TakeDamage(damage);
            HeroAbility.Push(enemy, enemy.transform.position - at, knockback);
        }
        SpellTargets.HitNear(at, radius, damage, SpellElement.Fire);
        if (impactPrefab != null) Instantiate(impactPrefab, at + Vector3.up * 0.8f, Quaternion.identity);
        AudioManager.Play(impactSound);
        var cameraFollow = FindAnyObjectByType<IsoCameraFollow>();
        if (cameraFollow != null) cameraFollow.Shake(0.25f, 0.3f);
    }
}
