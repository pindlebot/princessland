using UnityEngine;

// The wizard's step-4 ability: calls a meteor down on the nearest monster (or a few steps
// ahead if there's none). The actual falling and smashing is MeteorStrike's job, on its own
// object, so it carries on even if the wizard moves away.
public class Meteor : HeroAbility
{
    [SerializeField] private MeteorStrike strikePrefab;
    [SerializeField] private float reach = 4f;      // where it lands with no monster about
    [SerializeField] private int bonusDamage = 2;   // on top of the Fireball's damage

    protected override bool Activate()
    {
        var target = AimAtTarget();
        var strike = Instantiate(strikePrefab, GroundPoint(target, reach), Quaternion.identity);
        strike.Launch(SpellDamage + bonusDamage);
        return true;
    }
}
