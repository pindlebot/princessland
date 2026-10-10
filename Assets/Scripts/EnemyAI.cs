using System;
using System.Collections.Generic;
using UnityEngine;

// A simple state machine: Idle until the player is close and visible,
// then Chase, then Attack when in melee range.
// Optional extras: a bolt prefab makes it a ranged monster (it throws bolts from up to
// attackRange away, when it can see the hero), and "stationary" keeps it on its spot: it turns
// to watch, but never chases and can't be shoved (the dark mermaids, in the water).
[RequireComponent(typeof(CharacterController), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Idle, Chase, Attack }

    // Static = shared by all enemies. Added in Awake, removed on death or when the
    // scene unloads, so lingering corpses and disabled AIs still count correctly.
    private static readonly HashSet<EnemyAI> alive = new HashSet<EnemyAI>();
    public static int AliveCount => alive.Count;
    public static IReadOnlyCollection<EnemyAI> Alive => alive; // read-only view, for the minimap

    [SerializeField] private float moveSpeed = 3.2f;
    private const float AdventurerSpeedBoost = 1.15f;
    [SerializeField] private float aggroRange = 8f;
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private LayerMask sightBlockers = ~0;
    [SerializeField] private float corpseLifetime = 0.6f; // long enough to see the death animation
    [SerializeField] private GameObject defeatEffect;     // a friendly puff of stars as it vanishes
    [SerializeField] private AudioClip defeatSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private EnemyBolt boltPrefab;   // set = throws these instead of a melee hit
    [SerializeField] private Transform throwPoint;   // where bolts leave from (default: the middle)
    [SerializeField] private float throwDelay = 0.3f; // the wind-up: arms raised, so you can see it coming
    [SerializeField] private bool stationary;

    public bool Stationary => stationary;

    // Raised each time this enemy swings. CharacterAnimator listens to play the attack.
    public event Action Attacked;

    private CharacterController controller;
    private Health health;
    private Transform player;
    private Health playerHealth;
    private State state = State.Idle;
    private float nextAttackTime;

    // Chasing around walls: when the way to the hero isn't clear, follow a path over the level's tiles (NavGrid).
    private const float RepathSeconds = 0.4f;
    private readonly List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private float repathAt;
    private NavGrid nav;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        health.Died += _ => Die();
        alive.Add(this);
    }

    private void OnDestroy() => alive.Remove(this);

    // Stop thinking and stop being solid, play the death animation, then vanish in a puff
    // of stars. (Coroutines keep running when a component is disabled, so we start it first.)
    private void Die()
    {
        alive.Remove(this);
        StartCoroutine(Vanish());
        enabled = false;
        controller.enabled = false;
    }

    private System.Collections.IEnumerator Vanish()
    {
        yield return new WaitForSeconds(corpseLifetime);
        if (defeatEffect != null)
            Instantiate(defeatEffect, new Vector3(transform.position.x, 0f, transform.position.z), Quaternion.identity);
        AudioManager.Play(defeatSound, 0.6f);
        Destroy(gameObject);
    }

    private void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p == null) return;
        player = p.transform;
        playerHealth = p.GetComponent<Health>();
        nav = NavGrid.Current;
    }

    private void Update()
    {
        if (player == null || playerHealth.IsDead) return;
        if (DialogueController.IsOpen) return; // no ambushes mid-conversation

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // A thrower needs a clear shot too (no bolts through walls).
        bool inReach = dist <= attackRange && (boltPrefab == null || CanSeePlayer());
        state = inReach ? State.Attack
              // Chasers keep chasing once roused; a stationary one only watches while you're near.
              : ((state != State.Idle && !stationary) || (dist <= aggroRange && CanSeePlayer())) ? State.Chase
              : State.Idle;

        if (state == State.Idle) return;

        transform.rotation = Quaternion.LookRotation(toPlayer);

        if (state == State.Chase && !stationary)
        {
            // Adventurer Mode's monsters are a little quicker.
            float speed = moveSpeed * (GameSession.Settings.gentle ? 1f : AdventurerSpeedBoost);
            Vector3 move = ChaseDirection(toPlayer, dist) * speed + Vector3.down;
            controller.Move(move * Time.deltaTime);
        }
        else if (state == State.Attack && Time.time >= nextAttackTime)
        {
            if (boltPrefab != null) StartCoroutine(Throw());
            else playerHealth.TakeDamage(attackDamage);
            nextAttackTime = Time.time + attackCooldown;
            Attacked?.Invoke();
            AudioManager.Play(attackSound, 0.7f);
        }
    }

    // Straight at the hero when the way is clear; otherwise along a path around whatever is in the way.
    private Vector3 ChaseDirection(Vector3 toPlayer, float dist)
    {
        var direct = toPlayer.normalized;
        if (nav == null || ClearWalk(direct, dist))
        {
            path.Clear();
            return direct;
        }
        if (Time.time >= repathAt || pathIndex >= path.Count)
        {
            repathAt = Time.time + RepathSeconds;
            if (!nav.FindPath(transform.position, player.position, path)) path.Clear();
            pathIndex = 0;
        }
        while (pathIndex < path.Count && FlatDistance(path[pathIndex]) < 0.6f) pathIndex++;
        if (pathIndex >= path.Count) return direct;
        var step = path[pathIndex] - transform.position;
        step.y = 0f;
        return step.normalized;
    }

    private float FlatDistance(Vector3 point)
    {
        var d = point - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // Is there nothing solid between here and the hero (to walk into)? Other characters don't count.
    private bool ClearWalk(Vector3 direction, float dist)
    {
        if (nav != null) return nav.CanWalkStraight(transform.position, player.position);
        var origin = transform.position + Vector3.up * 0.6f;
        if (!Physics.SphereCast(origin, 0.4f, direction, out RaycastHit hit, Mathf.Max(0f, dist - 0.4f), ~0, QueryTriggerInteraction.Ignore))
            return true;
        return hit.collider is CharacterController || hit.transform == player;
    }

    // The wind-up, then the bolt flies at where the hero is now (flat, at throwing height).
    private System.Collections.IEnumerator Throw()
    {
        yield return new WaitForSeconds(throwDelay);
        if (!enabled || player == null || playerHealth.IsDead) yield break; // defeated mid-throw
        Vector3 from = throwPoint != null ? throwPoint.position : transform.position;
        Vector3 aim = player.position - from;
        aim.y = 0f;
        if (aim.sqrMagnitude < 0.01f) yield break;
        var bolt = Instantiate(boltPrefab, from, Quaternion.LookRotation(aim));
        bolt.Launch(attackDamage);
    }

    // Line-of-sight check so enemies don't aggro through walls. Water isn't in the way: you
    // can see (and throw) across the sea.
    private bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * 0.5f;
        Vector3 target = player.position + Vector3.up * 0.5f;
        int blockers = sightBlockers & ~(1 << LevelMap.WaterLayer);
        if (Physics.Linecast(eye, target, out RaycastHit hit, blockers, QueryTriggerInteraction.Ignore))
            return hit.transform == player;
        return true;
    }

    // Gizmos draw only in the Scene view: handy for tuning ranges.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
