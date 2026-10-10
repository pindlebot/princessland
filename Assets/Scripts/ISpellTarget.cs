using System.Collections.Generic;
using UnityEngine;

// What a spell is made of: the wizard's Fireball and Flame Wave are Fire, the princess's Tidal Orb
// and Whirlpool are Water. Things that react to spells can tell them apart (a bramble burns or
// blooms; a brazier flames or fills with water), but the gates are built so that EITHER one
// opens them: neither hero should ever get stuck.
public enum SpellElement { Arcane, Fire, Water }

// Anything a spell can hit besides a monster: a pot that smashes, a sleepy tree that wakes up, a
// bramble that burns away, a brazier that lights. Projectile calls OnSpellHit on the first one it
// touches (it needs a solid, non-trigger collider); the area spells go through SpellTargets.
public interface ISpellTarget
{
    void OnSpellHit(int damage, SpellElement element);
}

public static class SpellTargets
{
    // Hits every spell target within `radius` of `center` (on the floor plane), optionally only inside
    // a fan: `forward` and `halfAngle` (degrees). Returns how many it hit. Flame Wave, Meteor and the
    // Whirlpool use this, so they open the spell gates too.
    public static int HitNear(Vector3 center, float radius, int damage, SpellElement element,
                              Vector3 forward = default, float halfAngle = 180f)
    {
        var targets = new List<ISpellTarget>();
        foreach (var collider in Physics.OverlapSphere(center, radius + 1f, ~0, QueryTriggerInteraction.Ignore))
        {
            var target = collider.GetComponentInParent<ISpellTarget>();
            if (target == null || targets.Contains(target)) continue;
            var at = collider.bounds.center;
            var to = at - center;
            to.y = 0f;
            // A target counts if any part of it is in reach (its collider is a few tiles of wall, say).
            float reach = to.magnitude - Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z);
            if (reach > radius) continue;
            if (halfAngle < 180f && to.sqrMagnitude > 0.01f && Vector3.Angle(forward, to) > halfAngle) continue;
            targets.Add(target);
        }
        foreach (var target in targets) target.OnSpellHit(damage, element);
        return targets.Count;
    }
}
