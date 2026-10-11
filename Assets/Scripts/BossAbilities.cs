using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

// A boss's special moves, layered on top of an ordinary EnemyAI (which still does the chasing and
// the melee hits). Composition: the boss is a normal enemy plus this. Each boss uses the moves
// its prefab fills in; the others are left empty and never fire.
//
//   Ground Slam  a red circle grows under the player (the "telegraph"), then the boss leaps and
//                crashes down there. Anyone still inside the circle takes damage and is knocked
//                back to its edge, so the counterplay is to move out of it in time.
//   Split        the first time it drops to half health, it calls in helpers (the Slime King's
//                slimelings, the Pumpkin King's Gourdlings, the Captain's crew).
//   Volley       (needs a bolt prefab) it stops, rears back, and throws a fan of slow bolts at the
//                player: step between them. The Pumpkin King.
//   Barrage      (needs a barrage prefab) red circles appear around the player one after another,
//                and each is hit by a cannonball a moment later: keep moving. Captain Grumblebeard.
//
// Only one special move at a time; each has its own cooldown, so the fight has a rhythm.
//
// Every move warns first, on the floor, with a shape of its own (see Telegraph): a ring and stripes for the slam and each
// cannon shell, a fan of lanes for a volley (the bolts then fly where the fan settled, so a last-moment sidestep works),
// and a sound of its own too. No warning is shorter than Telegraph.MinWarningSeconds. Docs/BOSS_TELEGRAPHS.md has the
// numbers for every boss.
[RequireComponent(typeof(EnemyAI), typeof(Health))]
public class BossAbilities : MonoBehaviour
{
    [SerializeField] private string bossName = "The Slime King";
    [SerializeField] private float engageRange = 9f;
    [SerializeField] private AudioClip roarSound;

    [Header("Announcements ({0} is the boss's name)")]
    [SerializeField] private string engageMessage = "{0} awakens!";
    [SerializeField] private string splitMessage = "{0} splits off slimelings!";
    [SerializeField] private string volleyMessage = "";
    [SerializeField] private string barrageMessage = "";
    [SerializeField] private string defeatMessage = "";
    [Tooltip("For a level with no exit crystal (Hollow Farm): the level counts as cleared once this boss falls.")]
    [SerializeField] private bool clearsLevel;

    [Header("Ground Slam")]
    [SerializeField] private float slamCooldown = 6f;
    [SerializeField] private float slamRange = 10f;
    [SerializeField] private float slamRadius = 3f;
    [SerializeField] private int slamDamage = 2;
    [SerializeField] private float windupSeconds = 1f;
    [SerializeField] private float airSeconds = 0.7f;
    [SerializeField] private float leapHeight = 4f;
    [SerializeField] private GameObject warningPrefab;   // flat red circle (2 units across at scale 1)
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private AudioClip windupSound;
    [SerializeField] private AudioClip landSound;

    [Header("Split")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = 3;
    [SerializeField] private AudioClip summonSound;

    [Header("Volley (optional)")]
    [SerializeField] private EnemyBolt boltPrefab;
    [SerializeField] private int volleyBolts = 5;
    [SerializeField] private float volleyArc = 50f;      // degrees across the whole fan
    [SerializeField] private float volleyCooldown = 7f;
    [SerializeField] private float volleyRange = 11f;
    [SerializeField] private float volleyWindup = 0.7f;
    [SerializeField] private int volleyDamage = 1;
    [SerializeField] private AudioClip volleySound;

    [Header("Barrage (optional)")]
    [SerializeField] private GameObject barrageImpactPrefab;
    [SerializeField] private int barrageShots = 4;
    [SerializeField] private float barrageCooldown = 8f;
    [SerializeField] private float barrageRange = 12f;
    [SerializeField] private float barrageRadius = 1.8f;
    [SerializeField] private float barrageSeconds = 1.3f;   // warning time before a shot lands
    [SerializeField] private float barrageStagger = 0.4f;   // gap between one shot's warning and the next
    [SerializeField] private float barrageSpread = 4f;      // how far from the player the later shots may fall
    [SerializeField] private int barrageDamage = 1;
    [SerializeField] private AudioClip barrageSound;

    // "This room's boss has been beaten": it doesn't come back when you leave and return, even if other monsters remain.
    public static string DownFlag(string scene) => "boss_down:" + scene;

    public string BossName => bossName;
    public Health Health => health;
    public bool IsEngaged { get; private set; }
    public bool IsSlamming { get; private set; }
    public bool IsVolleying { get; private set; }
    public bool IsBarraging { get; private set; }
    // Another component (King Crabbington's ShellCycle) can stop the special moves for a while.
    public bool HoldSpecials { get; set; }
    public float SlamRadius => slamRadius;
    public bool HasVolley => boltPrefab != null;
    public bool HasBarrage => barrageImpactPrefab != null;

    // Big moments for the HUD to announce ("The Slime King awakens!").
    public event Action<string> Announced;

    private EnemyAI ai;
    private Health health;
    private CharacterController controller;
    private CharacterAnimator animator;
    private Transform shadow;
    private Vector3 shadowLocalPosition;
    private Transform player;
    private Health playerHealth;
    private float nextSlamAt, nextVolleyAt, nextBarrageAt;
    private bool hasSplit;

    // True while any special move is under way (so they take turns).
    private bool Busy => IsSlamming || IsVolleying || IsBarraging;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<CharacterAnimator>();
        shadow = transform.Find("Shadow");
        if (shadow != null) shadowLocalPosition = shadow.localPosition;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void Start()
    {
        var hero = FindAnyObjectByType<PlayerController>();
        if (hero == null) return;
        player = hero.transform;
        playerHealth = hero.GetComponent<Health>();
        // A moment's grace before the first move, and the moves staggered so they don't all come at once.
        nextSlamAt = Time.time + 3f;
        nextVolleyAt = Time.time + 4.5f;
        nextBarrageAt = Time.time + 6f;
    }

    private void Update()
    {
        if (player == null || health.IsDead || playerHealth.IsDead || DialogueController.IsOpen) return;

        float distance = FlatDistance(player.position);
        if (!IsEngaged && distance <= engageRange) Engage();
        if (!IsEngaged || Busy || HoldSpecials) return;

        if (Time.time >= nextSlamAt && distance <= slamRange) StartSlam();
        else if (HasVolley && Time.time >= nextVolleyAt && distance <= volleyRange) StartVolley();
        else if (HasBarrage && Time.time >= nextBarrageAt && distance <= barrageRange) StartBarrage();
    }

    private void Engage()
    {
        IsEngaged = true;
        AudioManager.Play(roarSound);
        Announce(engageMessage);
    }

    private void Announce(string message)
    {
        if (!string.IsNullOrEmpty(message)) Announced?.Invoke(string.Format(message, bossName));
    }

    private void OnDamaged(Health h)
    {
        if (!IsEngaged) Engage();
        if (!hasSplit && minionPrefab != null && !h.IsDead && h.Current <= h.Max / 2)
        {
            hasSplit = true;
            Split();
        }
    }

    private void OnDied(Health h)
    {
        Announce(defeatMessage);
        if (clearsLevel) GameSession.Flags.Add(LevelBootstrap.ClearedFlag); // the level stays safe when you come back
        GameSession.Flags.Add(DownFlag(SceneManager.GetActiveScene().name));   // a beaten boss stays beaten, even if the rest of the room isn't cleared
        SaveSystem.AutosaveSoon();                                         // and so does a beaten boss: the reward is safe too
    }

    // ---------- Split ----------

    private void Split()
    {
        AudioManager.Play(summonSound);
        for (int i = 0; i < minionCount; i++)
        {
            // Evenly spaced around the boss, like spokes on a wheel.
            var offset = Quaternion.Euler(0f, i * 360f / minionCount, 0f) * Vector3.forward * 2.8f;
            Instantiate(minionPrefab, transform.position + offset, Quaternion.identity);
        }
        Announce(splitMessage);
    }

    // ---------- Ground Slam ----------

    // Gentle Mode gives more time to see the warning circle and step out of it.
    public const float GentleWindupFactor = 1.6f;
    public float WindupSeconds => windupSeconds * (GameSession.Settings.gentle ? GentleWindupFactor : 1f);

    // Public so tests (or a scripted moment) can trigger it on demand.
    public void StartSlam()
    {
        if (IsSlamming || health.IsDead || player == null) return;
        StartCoroutine(Slam());
    }

    // A coroutine is a natural fit for a move with phases: wind up, fly, land.
    private IEnumerator Slam()
    {
        IsSlamming = true;
        ai.enabled = false; // the normal chase-and-hit AI pauses while we take over
        Vector3 target = new Vector3(player.position.x, transform.position.y, player.position.z);
        var toTarget = target - transform.position;
        if (toTarget.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toTarget); // glare at the spot

        // 1. Wind up: the warning circle grows where the player is standing.
        var warning = Instantiate(warningPrefab, new Vector3(target.x, 0.03f, target.z), Quaternion.Euler(90f, 0f, 0f));
        float windup = Telegraph.Clamp(WindupSeconds);
        var outline = Telegraph.Circle(target, slamRadius, windup);   // the exact edge of the danger, in a shape as well as a colour
        AudioManager.Play(windupSound);
        if (animator != null) animator.PlayAction(); // the lunge animation reads as "rearing back"
        for (float t = 0f; t < windup; t += Time.deltaTime)
        {
            if (health.IsDead) { Destroy(warning); outline.Finish(); yield break; }
            warning.transform.localScale = Vector3.one * slamRadius * Mathf.Lerp(0.3f, 1f, t / windup);
            yield return null;
        }

        // 2. Leap: an arc to the target. No collider in the air, so spells pass under it.
        Vector3 start = transform.position;
        controller.enabled = false;
        for (float t = 0f; t < airSeconds; t += Time.deltaTime)
        {
            float f = t / airSeconds;
            var p = Vector3.Lerp(start, target, f);
            p.y += Mathf.Sin(f * Mathf.PI) * leapHeight;
            transform.position = p;
            if (shadow != null) shadow.position = new Vector3(p.x, 0.02f, p.z); // shadow stays on the floor
            yield return null;
        }
        transform.position = target;
        if (shadow != null) shadow.localPosition = shadowLocalPosition;
        Destroy(warning);
        outline.Finish();

        // 3. Land.
        controller.enabled = true;
        Land(target);
        ai.enabled = true;
        nextSlamAt = Time.time + slamCooldown;
        IsSlamming = false;
    }

    private void Land(Vector3 at)
    {
        AudioManager.Play(landSound);
        if (shockwavePrefab != null)
            Instantiate(shockwavePrefab, new Vector3(at.x, 0.05f, at.z), Quaternion.Euler(90f, 0f, 0f));
        var cameraFollow = FindAnyObjectByType<IsoCameraFollow>();
        if (cameraFollow != null) cameraFollow.Shake(0.35f, 0.4f);

        if (FlatDistance(player.position) <= slamRadius)
        {
            playerHealth.TakeDamage(slamDamage);
            KnockBack();
        }
    }

    // Throw the player out to the edge of the impact, so they don't end up underneath the boss.
    // CharacterController.Move (rather than setting the position) means walls still stop them.
    private void KnockBack()
    {
        var away = player.position - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward; // landed dead on top: push back the way it came
        float push = slamRadius + 0.3f - away.magnitude;
        var body = player.GetComponent<CharacterController>();
        if (push > 0f && body != null && body.enabled)
            body.Move(away.normalized * push);
    }

    // ---------- Volley ----------

    // Public so tests can trigger it on demand.
    public void StartVolley()
    {
        if (Busy || health.IsDead || player == null || !HasVolley) return;
        StartCoroutine(Volley());
    }

    // The boss plants its feet and rears back (the attack animation), so you can see it coming, then a
    // fan of bolts leaves it, centred on where you stand: step between them.
    private IEnumerator Volley()
    {
        IsVolleying = true;
        ai.enabled = false;
        Announce(volleyMessage);
        AudioManager.Play(ActionFeedback.Clip("warn_charge"));   // a rising shimmer: different from the slam's rumble
        if (animator != null) animator.PlayAction();
        float windup = Telegraph.Clamp(volleyWindup * (GameSession.Settings.gentle ? GentleWindupFactor : 1f));

        // A fan of lanes on the floor, following the player until the last third of a second, then holding still:
        // the bolts fly down those lanes, so the gaps between them are where to stand.
        Vector3 aim = FlatToward(player.position);
        var fan = Telegraph.Fan(transform, () => aim, volleyBolts, volleyArc, volleyRange + 1f, windup);
        for (float t = 0f; t < windup; t += Time.deltaTime)
        {
            if (health.IsDead) { fan.Finish(); yield break; }
            if (!fan.IsLocked) aim = FlatToward(player.position);
            Face(transform.position + aim);
            yield return null;
        }
        fan.Finish();

        Vector3 from = transform.position + Vector3.up * 0.8f;
        AudioManager.Play(volleySound);
        for (int i = 0; i < volleyBolts; i++)
        {
            float f = volleyBolts == 1 ? 0f : i / (float)(volleyBolts - 1) - 0.5f;   // -0.5 .. 0.5 across the fan
            var bolt = Instantiate(boltPrefab, from, Quaternion.LookRotation(Quaternion.Euler(0f, f * volleyArc, 0f) * aim));
            bolt.Launch(volleyDamage);
        }

        yield return new WaitForSeconds(0.4f); // a beat to recover before moving again
        ai.enabled = !health.IsDead;
        nextVolleyAt = Time.time + volleyCooldown;
        IsVolleying = false;
    }

    // The flat direction from the boss to a point (never zero).
    private Vector3 FlatToward(Vector3 point)
    {
        var aim = point - transform.position;
        aim.y = 0f;
        return aim.sqrMagnitude < 0.01f ? transform.forward : aim.normalized;
    }

    // ---------- Barrage ----------

    public void StartBarrage()
    {
        if (Busy || health.IsDead || player == null || !HasBarrage) return;
        StartCoroutine(Barrage());
    }

    // Warning circles pop up round the player (the first right under them), one after another; each is
    // hit by a cannonball when its time is up. The boss keeps chasing while it calls the shots.
    private IEnumerator Barrage()
    {
        IsBarraging = true;
        Announce(barrageMessage);
        AudioManager.Play(roarSound);
        if (animator != null) animator.PlayAction();
        float seconds = barrageSeconds * (GameSession.Settings.gentle ? GentleWindupFactor : 1f);
        for (int i = 0; i < barrageShots; i++)
        {
            if (health.IsDead) break;
            Vector3 at = player.position;
            if (i > 0)
            {
                var jitter = UnityEngine.Random.insideUnitCircle * barrageSpread;
                at += new Vector3(jitter.x, 0f, jitter.y);
            }
            StartCoroutine(CannonShot(new Vector3(at.x, 0f, at.z), seconds));
            yield return new WaitForSeconds(barrageStagger);
        }
        yield return new WaitForSeconds(seconds);
        nextBarrageAt = Time.time + barrageCooldown;
        IsBarraging = false;
    }

    private IEnumerator CannonShot(Vector3 at, float seconds)
    {
        var warning = Instantiate(warningPrefab, new Vector3(at.x, 0.03f, at.z), Quaternion.Euler(90f, 0f, 0f));
        seconds = Telegraph.Clamp(seconds);
        var outline = Telegraph.Circle(at, barrageRadius, seconds, tick: false);   // (each shell whistles instead of ticking)
        AudioManager.Play(ActionFeedback.Clip("incoming"), 0.7f);                  // a falling whistle: a shell is coming down here
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (health.IsDead) { Destroy(warning); outline.Finish(); yield break; } // beaten mid-barrage: the cannons go quiet
            warning.transform.localScale = Vector3.one * barrageRadius * Mathf.Lerp(0.3f, 1f, t / seconds);
            yield return null;
        }
        Destroy(warning);
        outline.Finish();

        AudioManager.Play(barrageSound, 0.8f);
        Instantiate(barrageImpactPrefab, new Vector3(at.x, 0.4f, at.z), Quaternion.identity);
        var cameraFollow = FindAnyObjectByType<IsoCameraFollow>();
        if (cameraFollow != null) cameraFollow.Shake(0.15f, 0.25f);
        if (player != null && !playerHealth.IsDead)
        {
            var d = player.position - at;
            d.y = 0f;
            if (d.magnitude <= barrageRadius) playerHealth.TakeDamage(barrageDamage);
        }
    }

    // ---------- Helpers ----------

    private void Face(Vector3 point)
    {
        var to = point - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
    }

    private float FlatDistance(Vector3 point)
    {
        var d = point - transform.position;
        d.y = 0f;
        return d.magnitude;
    }
}
