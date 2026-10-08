using UnityEngine;

// The bridge between gameplay and animation, shared by the player and the enemies.
// Gameplay scripts (movement, spell, AI, health) know nothing about sprites; this
// component watches them and feeds the Animator's parameters. The Animator Controller
// asset decides which clip plays.
//
//   Speed       (float)   -> Idle <-> Walk
//   FacingBack  (float)   -> picks the Front or Back clip inside each state's blend tree
//   Action      (trigger) -> the character's action state (Cast for the wizard and
//                            princess, Attack for the skeleton), then back to Idle
//   Hurt        (trigger) -> Hurt, then back to Idle
//   Dead        (bool)    -> Die (and nothing can interrupt it)
//
// Left/right isn't an Animator parameter at all: we just mirror the sprite with flipX.
[RequireComponent(typeof(CharacterController), typeof(Health))]
public class CharacterAnimator : MonoBehaviour
{
    // Looking parameters up by hash once is cheaper than passing strings every frame.
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int FacingBackId = Animator.StringToHash("FacingBack");
    private static readonly int ActionId = Animator.StringToHash("Action");
    private static readonly int HurtId = Animator.StringToHash("Hurt");
    private static readonly int DeadId = Animator.StringToHash("Dead");

    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private CharacterController controller;
    private Camera cam;

    public Animator Animator => animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;

        var health = GetComponent<Health>();
        health.Damaged += _ => animator.SetTrigger(HurtId);
        health.Died += _ => animator.SetBool(DeadId, true);
        health.Revived += _ => animator.SetBool(DeadId, false);

        // Whichever action source this character has drives the Action state.
        if (TryGetComponent(out SpellAbility ability)) ability.Cast += PlayAction;
        foreach (var extra in GetComponents<HeroAbility>()) extra.Used += PlayAction;
        if (TryGetComponent(out EnemyAI ai)) ai.Attacked += PlayAction;
    }

    // Public so special moves (like the boss's slam) can use the action animation too.
    public void PlayAction() => animator.SetTrigger(ActionId);

    private void Update()
    {
        Vector3 velocity = controller.enabled ? controller.velocity : Vector3.zero;
        velocity.y = 0f;
        animator.SetFloat(SpeedId, velocity.magnitude);

        // Which way is the character facing *on screen*? Compare its forward vector
        // with the camera's screen-right and screen-up directions (flattened onto the floor).
        Vector3 facing = transform.forward;
        Vector3 screenUp = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        bool facingAway = Vector3.Dot(facing, screenUp) > 0f;
        bool facingLeft = Vector3.Dot(facing, cam.transform.right) < 0f;

        animator.SetFloat(FacingBackId, facingAway ? 1f : 0f);
        spriteRenderer.flipX = facingLeft; // the art is drawn facing right
    }
}
