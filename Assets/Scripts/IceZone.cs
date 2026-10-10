using System.Collections.Generic;
using UnityEngine;

// A patch of ice on the ground that an ice slime left behind (IceTrail): it's slippery for the hero for a few seconds,
// then melts away. PlayerController asks IceZone.Covers(position) as well as the map's own ice tiles.
public class IceZone : MonoBehaviour
{
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float radius = 1.1f;

    private static readonly List<IceZone> all = new List<IceZone>();
    private SpriteRenderer sprite;
    private float born;

    public static int Count => all.Count;

    private void OnEnable() { all.Add(this); born = Time.time; sprite = GetComponent<SpriteRenderer>(); }
    private void OnDisable() => all.Remove(this);

    private void Update()
    {
        float age = Time.time - born;
        if (age >= lifetime) { Destroy(gameObject); return; }
        if (sprite != null && age > lifetime - 1.5f)
        {
            var c = sprite.color;
            c.a = (lifetime - age) / 1.5f;
            sprite.color = c;
        }
    }

    // Is this point on one of the patches?
    public static bool Covers(Vector3 world)
    {
        foreach (var zone in all)
        {
            var d = world - zone.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude <= zone.radius * zone.radius) return true;
        }
        return false;
    }
}
