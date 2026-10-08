using System;
using System.Collections.Generic;
using UnityEngine;

// A simple state machine: Idle until the player is close and visible,
// then Chase, then Attack when in melee range.
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

    // Raised each time this enemy swings. CharacterAnimator listens to play the attack.
    public event Action Attacked;

    private CharacterController controller;
    private Health health;
    private Transform player;
    private Health playerHealth;
    private State state = State.Idle;
    private float nextAttackTime;

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
    }

    private void Update()
    {
        if (player == null || playerHealth.IsDead) return;
        if (DialogueController.IsOpen) return; // no ambushes mid-conversation

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        state = dist <= attackRange ? State.Attack
              : (state != State.Idle || (dist <= aggroRange && CanSeePlayer())) ? State.Chase
              : State.Idle;

        if (state == State.Idle) return;

        transform.rotation = Quaternion.LookRotation(toPlayer);

        if (state == State.Chase)
        {
            // Adventurer Mode's monsters are a little quicker.
            float speed = moveSpeed * (GameSession.Settings.gentle ? 1f : AdventurerSpeedBoost);
            Vector3 move = toPlayer.normalized * speed + Vector3.down;
            controller.Move(move * Time.deltaTime);
        }
        else if (Time.time >= nextAttackTime)
        {
            playerHealth.TakeDamage(attackDamage);
            nextAttackTime = Time.time + attackCooldown;
            Attacked?.Invoke();
            AudioManager.Play(attackSound, 0.7f);
        }
    }

    // Line-of-sight check so enemies don't aggro through walls.
    private bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * 0.5f;
        Vector3 target = player.position + Vector3.up * 0.5f;
        if (Physics.Linecast(eye, target, out RaycastHit hit, sightBlockers, QueryTriggerInteraction.Ignore))
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
