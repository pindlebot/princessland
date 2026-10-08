using System;
using System.Collections;
using UnityEngine;

// The Slime King's special moves, layered on top of an ordinary EnemyAI (which still does
// the chasing and the melee hits). Composition: the boss is a normal enemy plus this.
//
//   Ground Slam  a red circle grows under the player (the "telegraph"), then the King leaps
//                and crashes down there. Anyone still inside the circle takes damage and is
//                knocked back to its edge, so the counterplay is to move out of it in time.
//   Royal Split  the first time it drops to half health, it splits off a few slimelings.
[RequireComponent(typeof(EnemyAI), typeof(Health))]
public class BossAbilities : MonoBehaviour
{
    [SerializeField] private string bossName = "The Slime King";
    [SerializeField] private float engageRange = 9f;
    [SerializeField] private AudioClip roarSound;

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

    [Header("Royal Split")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = 3;
    [SerializeField] private AudioClip summonSound;

    public string BossName => bossName;
    public Health Health => health;
    public bool IsEngaged { get; private set; }
    public bool IsSlamming { get; private set; }
    public float SlamRadius => slamRadius;

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
    private float nextSlamAt;
    private bool hasSplit;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<CharacterAnimator>();
        shadow = transform.Find("Shadow");
        if (shadow != null) shadowLocalPosition = shadow.localPosition;
        health.Damaged += OnDamaged;
    }

    private void Start()
    {
        var hero = FindAnyObjectByType<PlayerController>();
        if (hero == null) return;
        player = hero.transform;
        playerHealth = hero.GetComponent<Health>();
        nextSlamAt = Time.time + 3f; // a moment's grace before the first slam
    }

    private void Update()
    {
        if (player == null || health.IsDead || playerHealth.IsDead || DialogueController.IsOpen) return;

        float distance = FlatDistance(player.position);
        if (!IsEngaged && distance <= engageRange) Engage();
        if (IsEngaged && !IsSlamming && Time.time >= nextSlamAt && distance <= slamRange)
            StartSlam();
    }

    private void Engage()
    {
        IsEngaged = true;
        AudioManager.Play(roarSound);
        Announced?.Invoke($"{bossName} awakens!");
    }

    private void OnDamaged(Health h)
    {
        if (!IsEngaged) Engage();
        if (!hasSplit && !h.IsDead && h.Current <= h.Max / 2)
        {
            hasSplit = true;
            Split();
        }
    }

    // ---------- Royal Split ----------

    private void Split()
    {
        AudioManager.Play(summonSound);
        for (int i = 0; i < minionCount; i++)
        {
            // Evenly spaced around the King, like spokes on a wheel.
            var offset = Quaternion.Euler(0f, i * 360f / minionCount, 0f) * Vector3.forward * 2.8f;
            Instantiate(minionPrefab, transform.position + offset, Quaternion.identity);
        }
        Announced?.Invoke($"{bossName} splits off slimelings!");
    }

    // ---------- Ground Slam ----------

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
        AudioManager.Play(windupSound);
        if (animator != null) animator.PlayAction(); // the lunge animation reads as "rearing back"
        for (float t = 0f; t < windupSeconds; t += Time.deltaTime)
        {
            if (health.IsDead) { Destroy(warning); yield break; }
            warning.transform.localScale = Vector3.one * slamRadius * Mathf.Lerp(0.3f, 1f, t / windupSeconds);
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

    // Throw the player out to the edge of the impact, so they don't end up underneath the King.
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

    private float FlatDistance(Vector3 point)
    {
        var d = point - transform.position;
        d.y = 0f;
        return d.magnitude;
    }
}
