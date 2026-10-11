using System;
using System.Collections.Generic;
using UnityEngine;

// A character's spell (the wizard's Fireball, the princess's Tidal Orb): a projectile with
// a cooldown and a mana cost. No aiming needed: it flies at the nearest enemy that's on
// screen with a clear line to it, or straight ahead if there is none. It never picks a monster behind a wall.
// The automatic pick is sticky: it keeps its monster until another is clearly closer (more than SwitchMargin
// nearer), so the ring doesn't flicker between two monsters at the same distance as you move.
// Tab / RB picks a different monster (the next nearest, wrapping round) and it stays the target
// until it's defeated or leaves the screen; the HUD, a ring and an arrow on the ground show which one it is.
[RequireComponent(typeof(Mana))]
public class SpellAbility : MonoBehaviour
{
    [SerializeField] private string spellName = "Fireball";
    [SerializeField] private Sprite icon; // shown in the HUD's hotbar
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private AudioClip castSound;
    [SerializeField] private Transform castPoint;
    [SerializeField] private float cooldown = 0.6f;
    [SerializeField] private float manaCost = 10f;
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float faceTargetSeconds = 0.35f; // how long the wizard keeps looking at his target

    private Mana mana;
    private Inventory inventory;
    private PlayerController movement;
    private Camera cam;
    private float readyAt;
    private EnemyAI chosen;               // picked with Tab / RB; null = whoever's nearest
    private float chosenBlockedSince = -1f;
    private EnemyAI auto;                 // the automatic pick, kept until another is clearly closer
    private EnemyAI cachedTarget;
    private int cachedFrame = -1;
    private bool castArmed = true, clickArmed = true; // a button held through a menu must be let go before it casts
    private bool warnedThisHold;                      // one "out of magic" cue per press, not one per frame

    // A monster must be this much nearer (in metres) than the automatic pick before the pick moves to it.
    public const float SwitchMargin = 1.5f;
    // A Tab pick that's out of sight is remembered this long before it's forgotten.
    private const float ChosenGraceSeconds = 1.5f;

    // Raised every time the ability fires. CharacterAnimator listens to play the cast animation.
    public event Action Cast;

    // 0 = just cast, 1 = ready. Read by the HUD.
    public float CooldownProgress => Cooldown <= 0f ? 1f : Mathf.Clamp01(1f - (readyAt - Time.time) / Cooldown);
    // Skills (the princess's Swift Tides) and equipment (the Starlight Wand) make it recharge faster.
    public float Cooldown => cooldown * GameSession.Progress.SpellCooldownFactor
                                     * (inventory != null ? inventory.SpellCooldownFactor : 1f);
    public string SpellName => spellName;
    public float CastHeight => castPoint.position.y; // the height the line of fire is checked at
    public Sprite Icon => icon;
    public float ManaCost => manaCost;
    public float LastCastTime { get; private set; } = -999f; // the HUD hints "Space: Magic!" if it's been a while
    public bool CanAfford => mana.CanAfford(manaCost);
    // Base damage, plus equipment (the Ember Ring, the Starlight Wand) and skills (Empowered Spells).
    public int Damage => baseDamage + (inventory != null ? inventory.SpellDamageBonus : 0)
                                    + GameSession.Progress.BonusSpellDamage;

    private void Awake()
    {
        mana = GetComponent<Mana>();
        inventory = GetComponent<Inventory>();       // optional: equipment can add damage and speed
        movement = GetComponent<PlayerController>(); // optional: lets us turn to face the target
        cam = Camera.main;
        gameObject.AddComponent<TargetMarker>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return;
        if (GameInput.ActionsBlocked) // the buttons are for the conversation or menu
        {
            // Whatever is held now was meant for the menu: it has to be let go before it can cast.
            // (Letting go while the menu is still up re-arms it, so a quick tap in a menu never costs the next press.)
            if (GameInput.CastHeld) castArmed = false; else castArmed = true;
            if (GameInput.ClickHeld) clickArmed = false; else clickArmed = true;
            return;
        }
        if (movement != null && (movement.IsSeated || movement.IsSwimming)) return; // no spells from the toilet, or while swimming

        if (!GameInput.CastHeld) castArmed = true;
        if (!GameInput.ClickHeld) clickArmed = true;
        else if (GameInput.ClickPressed && HudController.PointerOverUi) clickArmed = false; // a press that began on the HUD stays the HUD's

        if (GameInput.TargetPressed) CycleTarget();

        // Clicks on the HUD (e.g. the inventory) are for the UI, not for casting.
        bool click = clickArmed && GameInput.ClickHeld && !HudController.PointerOverUi;
        bool cast = castArmed && GameInput.CastHeld;
        if (!click && !cast)
        {
            warnedThisHold = false;
            return;
        }
        if (TryCast()) return;
        // Not ready for a reason the player can fix: say so once per press (not every frame of a held button).
        if (!CanAfford && !warnedThisHold)
        {
            warnedThisHold = true;
            ActionFeedback.Fail(FailReason.LowMana);
        }
    }

    // Public so other code (AI, tests, UI buttons) can trigger the ability too.
    public bool TryCast()
    {
        if (Time.time < readyAt) return false;
        if (!mana.TrySpend(manaCost)) return false;

        Vector3 direction = transform.forward;
        var target = FindTarget();
        if (target != null)
        {
            // Turn first: the cast point is a child of the player, so it swings around to
            // the target's side and the fireball spawns there.
            Vector3 toTarget = target.transform.position - transform.position;
            if (movement != null) movement.FaceFor(toTarget, faceTargetSeconds);
            else transform.rotation = Quaternion.LookRotation(new Vector3(toTarget.x, 0f, toTarget.z));

            direction = target.transform.position - castPoint.position;
            direction.y = 0f; // fly level, at casting height
        }

        // Instantiate clones the prefab asset into the scene.
        var projectile = Instantiate(projectilePrefab, castPoint.position, Quaternion.LookRotation(direction));
        projectile.Launch(Damage);
        AudioManager.Play(castSound);
        readyAt = Time.time + Cooldown;
        LastCastTime = Time.time;
        Cast?.Invoke();
        return true;
    }

    // The monster the spell will fly at right now (what the HUD shows), or null if none is reachable.
    // Worked out once a frame: the HUD, the ground ring and the cast all ask.
    public EnemyAI Target
    {
        get
        {
            if (cachedFrame != Time.frameCount)
            {
                cachedFrame = Time.frameCount;
                cachedTarget = FindTarget();
            }
            return cachedTarget;
        }
    }

    // True if the Tab pick (rather than the automatic one) is what the spell is aimed at.
    public bool TargetWasChosen => chosen != null && Target == chosen;

    // Aim at the next monster in reach: nearest first, then each farther one, then back to the nearest.
    public void CycleTarget()
    {
        var reachable = Reachable();
        if (reachable.Count == 0)
        {
            chosen = null;
            return;
        }
        int current = reachable.IndexOf(FindTarget());
        chosen = reachable[(current + 1) % reachable.Count]; // nothing aimed at yet (-1) starts at the nearest
        chosenBlockedSince = -1f;
        cachedFrame = -1;
    }

    // Along the floor: a monster's height (a bat, a leap) never changes who is "nearest".
    private float Distance(EnemyAI enemy)
    {
        var d = enemy.transform.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // Every living monster on screen with a clear line of fire, nearest first (ties by position: see LeftOf).
    private List<EnemyAI> Reachable()
    {
        var list = new List<EnemyAI>();
        foreach (var enemy in EnemyAI.Alive)
            if (IsReachable(enemy)) list.Add(enemy);
        list.Sort((a, b) =>
        {
            int byDistance = Distance(a).CompareTo(Distance(b));
            return byDistance != 0 ? byDistance : LeftOf(a, b);
        });
        return list;
    }

    // A tie in distance goes to whoever stands further left, then further down the screen: the same every time, whatever
    // order the game happens to list the monsters in.
    private static int LeftOf(EnemyAI a, EnemyAI b)
    {
        var pa = a.transform.position;
        var pb = b.transform.position;
        int byX = pa.x.CompareTo(pb.x);
        return byX != 0 ? byX : pa.z.CompareTo(pb.z);
    }

    private bool IsReachable(EnemyAI enemy) => IsOnScreen(enemy.transform.position) && HasLineOfFire(enemy);

    // The monster picked with Tab / RB while it's alive, on screen and not hiding behind a wall; otherwise the
    // nearest reachable one (sticky: see SwitchMargin). Null if nothing is in reach: the spell then just flies
    // straight ahead rather than into a wall.
    public EnemyAI FindTarget()
    {
        if (chosen != null)
        {
            if (!EnemyAI.IsAlive(chosen) || !IsOnScreen(chosen.transform.position)) chosen = null;
            else if (HasLineOfFire(chosen)) { chosenBlockedSince = -1f; return chosen; }
            else
            {
                // Dodged behind a pillar: the automatic pick takes over for now, and the Tab pick comes back if
                // it reappears (forgotten if it stays hidden).
                if (chosenBlockedSince < 0f) chosenBlockedSince = Time.time;
                else if (Time.time - chosenBlockedSince > ChosenGraceSeconds) chosen = null;
            }
        }

        EnemyAI best = null;
        float bestDistance = float.MaxValue;
        foreach (var enemy in EnemyAI.Alive)
        {
            if (!IsReachable(enemy)) continue;
            float distance = Distance(enemy);
            if (best == null || distance < bestDistance - 0.001f ||
                (Mathf.Abs(distance - bestDistance) <= 0.001f && LeftOf(enemy, best) < 0))
            {
                best = enemy;
                bestDistance = distance;
            }
        }
        // Stick with the current automatic pick unless the nearest is clearly nearer.
        if (auto != null && auto != best && EnemyAI.IsAlive(auto) && IsReachable(auto) && best != null &&
            Distance(auto) - bestDistance < SwitchMargin)
            return auto;
        auto = best;
        return best;
    }

    private bool IsOnScreen(Vector3 world)
    {
        // Viewport coordinates run 0..1 across the screen; z < 0 means behind the camera.
        Vector3 v = cam.WorldToViewportPoint(world);
        return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
    }

    private bool HasLineOfFire(EnemyAI enemy)
    {
        // From the player's center (a line starting inside a collider ignores it), at casting height.
        Vector3 from = transform.position;
        from.y = castPoint.position.y;
        Vector3 to = enemy.transform.position;
        to.y = from.y;
        if (!Physics.Linecast(from, to, out RaycastHit hit, ~LevelMap.WaterMask, QueryTriggerInteraction.Ignore))
            return true;
        // Another monster (or one that has just fallen and is fading away) in the way is no wall: the spell hits
        // something it should. Only scenery blocks the line.
        return hit.collider.GetComponentInParent<EnemyAI>() != null;
    }
}
