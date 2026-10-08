using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// An ability a hero learns from her skill path (steps 3 and 4): Flame Wave and Meteor for
// the wizard, Bubble Shield and Whirlpool for the princess. Each has its own button and its
// own hotbar slot. This base class does what they all share (the skill that unlocks it, the
// button, cooldown and mana, aiming); each subclass only says what happens in Activate.
// Like SpellAbility, the components sit on the player prefab from the start, and simply
// do nothing until their skill is learned.
[RequireComponent(typeof(Mana))]
public abstract class HeroAbility : MonoBehaviour
{
    [SerializeField] private string abilityName = "Ability";
    [SerializeField] private Sprite icon;
    [SerializeField] private string skillId;      // the SkillCatalog skill that unlocks it
    [SerializeField] private int slot = 2;         // hotbar slot 2 or 3, which also picks the button
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private float manaCost = 20f;
    [SerializeField] private AudioClip useSound;

    protected Mana Mana { get; private set; }
    protected SpellAbility Spell { get; private set; } // for its damage (bonuses included) and auto-aim
    private PlayerController movement;
    private float readyAt;

    // Raised every time it's used. The HUD flashes the slot; CharacterAnimator plays the cast.
    public event Action Used;

    public string AbilityName => abilityName;
    public Sprite Icon => icon;
    public string SkillId => skillId;
    public int Slot => slot;
    public float ManaCost => manaCost;
    public bool Unlocked => GameSession.Progress.Has(skillId);
    public bool CanAfford => Mana.CanAfford(manaCost);
    public float CooldownProgress => cooldown <= 0f ? 1f : Mathf.Clamp01(1f - (readyAt - Time.time) / cooldown);

    protected virtual void Awake()
    {
        Mana = GetComponent<Mana>();
        Spell = GetComponent<SpellAbility>();
        movement = GetComponent<PlayerController>();
    }

    protected virtual void Update()
    {
        if (!Unlocked) return;
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return;
        if (GameInput.GameplayBlocked) return;
        if (movement != null && movement.IsSeated) return; // no abilities from the toilet either
        if (GameInput.AbilityPressed(slot)) TryUse();
    }

    // Public so tests (or the HUD) can trigger it too.
    public bool TryUse()
    {
        if (!Unlocked || Time.time < readyAt || !CanAfford) return false;
        if (!Activate()) return false;
        Mana.TrySpend(manaCost);
        readyAt = Time.time + cooldown;
        AudioManager.Play(useSound);
        Used?.Invoke();
        return true;
    }

    // Do the thing. Return false if it couldn't happen (then no mana or cooldown is used).
    protected abstract bool Activate();

    // ---------- Helpers for the subclasses ----------

    // The spell's auto-aim target (nearest monster on screen), turning the hero to face it.
    protected EnemyAI AimAtTarget()
    {
        var target = Spell != null ? Spell.FindTarget() : null;
        if (target != null) Face(target.transform.position);
        return target;
    }

    protected void Face(Vector3 point)
    {
        var to = point - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.01f) return;
        if (movement != null) movement.FaceFor(to, 0.4f);
        else transform.rotation = Quaternion.LookRotation(to);
    }

    // A spot on the floor: the target's feet, or `distance` ahead of the hero if there's none.
    protected Vector3 GroundPoint(EnemyAI target, float distance)
    {
        var p = target != null ? target.transform.position : transform.position + transform.forward * distance;
        return new Vector3(p.x, 0f, p.z);
    }

    // The spell's damage, so Empowered Spells and the Ember Ring make abilities stronger too.
    protected int SpellDamage => Spell != null ? Spell.Damage : 1;

    // Living monsters within `radius` of `center` (on the floor plane). A copy, not the live
    // set: hurting a monster can defeat it, which removes it from EnemyAI.Alive mid-loop.
    public static List<EnemyAI> EnemiesNear(Vector3 center, float radius) =>
        EnemyAI.Alive.Where(e => FlatDistance(e.transform.position, center) <= radius).ToList();

    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Shove a monster along the floor. CharacterController.Move, so walls still stop it.
    public static void Push(EnemyAI enemy, Vector3 direction, float distance)
    {
        direction.y = 0f;
        var body = enemy.GetComponent<CharacterController>();
        if (body != null && body.enabled && direction.sqrMagnitude > 0.0001f)
            body.Move(direction.normalized * distance);
    }
}
