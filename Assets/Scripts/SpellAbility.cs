using System;
using System.Collections.Generic;
using UnityEngine;

// A character's spell (the wizard's Fireball, the princess's Tidal Orb): a projectile with
// a cooldown and a mana cost. No aiming needed: it flies at the nearest enemy that's on
// screen (preferring ones it can actually reach), or straight ahead if there are none.
// Tab / RB picks a different monster (the next nearest, wrapping round) and it stays the target
// until it's defeated or leaves the screen; the HUD and a ring on the ground show which one it is.
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
    private EnemyAI cachedTarget;
    private int cachedFrame = -1;

    // Raised every time the ability fires. CharacterAnimator listens to play the cast animation.
    public event Action Cast;

    // 0 = just cast, 1 = ready. Read by the HUD.
    public float CooldownProgress => Cooldown <= 0f ? 1f : Mathf.Clamp01(1f - (readyAt - Time.time) / Cooldown);
    // Skills (the princess's Swift Tides) and equipment (the Starlight Wand) make it recharge faster.
    public float Cooldown => cooldown * GameSession.Progress.SpellCooldownFactor
                                     * (inventory != null ? inventory.SpellCooldownFactor : 1f);
    public string SpellName => spellName;
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
        if (GameInput.GameplayBlocked) return; // the buttons are for the conversation or menu
        if (movement != null && (movement.IsSeated || movement.IsSwimming)) return; // no spells from the toilet, or while swimming

        if (GameInput.TargetPressed) CycleTarget();

        // Clicks on the HUD (e.g. the inventory) are for the UI, not for casting.
        bool click = GameInput.ClickHeld && !HudController.PointerOverUi;
        if (click || GameInput.CastHeld)
            TryCast();
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

    // The monster the spell will fly at right now (what the HUD shows), or null if none is on screen.
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

    // Aim at the next monster on screen: nearest first, then each farther one, then back to the nearest.
    public void CycleTarget()
    {
        var onScreen = new List<EnemyAI>();
        foreach (var enemy in EnemyAI.Alive)
            if (IsOnScreen(enemy.transform.position)) onScreen.Add(enemy);
        if (onScreen.Count == 0)
        {
            chosen = null;
            return;
        }
        onScreen.Sort((a, b) => Distance(a).CompareTo(Distance(b)));
        int current = onScreen.IndexOf(FindTarget());
        chosen = onScreen[(current + 1) % onScreen.Count]; // nothing aimed at yet (-1) starts at the nearest
        cachedFrame = -1;
    }

    private float Distance(EnemyAI enemy) => Vector3.Distance(transform.position, enemy.transform.position);

    // The monster picked with Tab / RB while it's still alive and on screen; otherwise the nearest living
    // enemy inside the camera's view. Enemies with a clear line of fire win over closer ones hiding behind a wall.
    public EnemyAI FindTarget()
    {
        if (EnemyAI.IsAlive(chosen) && IsOnScreen(chosen.transform.position)) return chosen;
        chosen = null;

        EnemyAI bestClear = null, bestAny = null;
        float bestClearDistance = float.MaxValue, bestAnyDistance = float.MaxValue;

        foreach (var enemy in EnemyAI.Alive)
        {
            if (!IsOnScreen(enemy.transform.position)) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < bestAnyDistance)
            {
                bestAny = enemy;
                bestAnyDistance = distance;
            }
            if (distance < bestClearDistance && HasLineOfFire(enemy))
            {
                bestClear = enemy;
                bestClearDistance = distance;
            }
        }
        return bestClear != null ? bestClear : bestAny;
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
        return hit.collider.GetComponentInParent<EnemyAI>() == enemy;
    }
}
