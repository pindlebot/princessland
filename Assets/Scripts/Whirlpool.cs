using UnityEngine;

// The princess's step-4 ability: opens a whirlpool under the nearest monster (or a few
// steps ahead). The pool itself (WhirlpoolZone) does the pulling and splashing.
public class Whirlpool : HeroAbility
{
    [SerializeField] private WhirlpoolZone poolPrefab;
    [SerializeField] private float reach = 3.5f;

    protected override bool Activate()
    {
        var target = AimAtTarget();
        var pool = Instantiate(poolPrefab, GroundPoint(target, reach) + Vector3.up * 0.04f, Quaternion.Euler(90f, 0f, 0f));
        pool.Launch(SpellDamage);
        return true;
    }
}
