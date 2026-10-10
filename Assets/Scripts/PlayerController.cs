using UnityEngine;

// WASD movement on the isometric grid. The player turns to face the way they're
// walking; abilities can briefly take over facing (e.g. to look at a spell's target).
// The hero can also sit down on something (the toilet at home): SitOn puts them on it,
// facing the camera, until they walk away or StandUp is called. Lying in the bath is the
// same, with bathing set: the animator shows them in their swimwear instead of sitting. Going to
// bed is the same again, with sleeping set: they lie tucked under the quilt, with Zs floating up.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float turnSpeed = 900f; // degrees per second
    [SerializeField] private float iceAcceleration = 7f; // on ice you speed up and slow down slowly: you slide

    private CharacterController controller;
    private Inventory inventory; // optional: boots make you faster
    private Camera cam;
    private float verticalVelocity;
    private float holdFacingUntil;
    private Vector3 standingPosition; // where the hero stood before sitting down
    private float spriteHeight;       // the sprite's usual height above the root
    private Vector3 slide;            // the hero's actual velocity on ice (otherwise it just follows the keys)
    private LevelMap map;
    private bool searchedForMap;

    // What the hero is sitting on (null when standing).
    public Transform Seat { get; private set; }
    public bool IsSeated => Seat != null;
    public bool IsBathing { get; private set; } // seated in the bath, in their swimwear
    public bool IsSleeping { get; private set; } // tucked up in bed
    public bool IsHopping { get; private set; }  // mid-air over a gap (HopAbility)
    public bool IsSwimming { get; set; }         // in the water with the Bubble Charm (SwimAbility): no spells
    public bool IsOnIce { get; private set; }    // on an ice tile or a slime's ice patch: keep your momentum
    public Vector3 MoveDirection { get; private set; } // where the stick/keys point now, on the ground (zero when still)

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<Inventory>();
        cam = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return;
        if (IsHopping) return; // in the air: HopTo moves us

        if (IsSeated)
        {
            // Pushing the stick or a direction key gets up again.
            if (GameInput.Move.sqrMagnitude > 0.25f && !GameInput.GameplayBlocked) StandUp();
            return;
        }

        Vector3 move = Move();
        MoveDirection = move.sqrMagnitude > 0.01f ? move.normalized : Vector3.zero;
        if (Time.time >= holdFacingUntil)
            FaceTowards(move);
    }

    // A hop over a gap: a short arc to `target` (on the ground), with the CharacterController off so
    // nothing in the way (the gap's invisible wall) stops us. `onLanded` runs as we touch down.
    public void HopTo(Vector3 target, float seconds, float height, System.Action onLanded = null)
    {
        if (!IsHopping) StartCoroutine(Hop(target, seconds, height, onLanded));
    }

    private System.Collections.IEnumerator Hop(Vector3 target, float seconds, float height, System.Action onLanded)
    {
        IsHopping = true;
        controller.enabled = false;
        Vector3 from = transform.position;
        target.y = from.y;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float f = t / seconds;
            var p = Vector3.Lerp(from, target, f);
            p.y = from.y + Mathf.Sin(f * Mathf.PI) * height;
            transform.position = p;
            yield return null;
        }
        transform.position = target;
        controller.enabled = true;
        verticalVelocity = -1f;
        IsHopping = false;
        onLanded?.Invoke();
    }

    // Turn to face a direction now and keep facing it for a moment, even while walking.
    public void FaceFor(Vector3 direction, float seconds)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(direction);
        holdFacingUntil = Time.time + seconds;
    }

    // Sits on seat at seatPosition (only its x and z are used; the hero's feet stay at floor
    // level), facing the camera. height lifts the sprite so the hero sits on top of the
    // seat with their feet dangling. The CharacterController is off while seated, so it
    // doesn't bump into the seat's collider.
    public void SitOn(Transform seat, Vector3 seatPosition, float height, bool bathing = false, bool sleeping = false)
    {
        if (IsSeated) StandUp();
        Seat = seat;
        IsBathing = bathing;
        IsSleeping = sleeping;
        standingPosition = transform.position;
        controller.enabled = false;
        transform.position = new Vector3(seatPosition.x, transform.position.y, seatPosition.z);
        transform.rotation = Quaternion.LookRotation(-Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up));
        var sprite = SpriteTransform();
        if (sprite != null)
        {
            spriteHeight = sprite.localPosition.y;
            sprite.localPosition += Vector3.up * height;
        }
    }

    // Gets up and steps back to where the hero was standing before sitting down.
    public void StandUp()
    {
        if (!IsSeated) return;
        Seat = null;
        IsBathing = false;
        IsSleeping = false;
        var sprite = SpriteTransform();
        if (sprite != null)
        {
            var p = sprite.localPosition;
            sprite.localPosition = new Vector3(p.x, spriteHeight, p.z);
        }
        transform.position = standingPosition;
        controller.enabled = true;
    }

    private Transform SpriteTransform() =>
        TryGetComponent(out CharacterAnimator visuals) && visuals.SpriteRenderer != null
            ? visuals.SpriteRenderer.transform
            : null;

    private Vector3 Move()
    {
        Vector2 input = GameInput.Move; // keys, stick or d-pad

        // In an isometric view "up" on screen isn't world +Z. Flatten the camera's
        // forward/right vectors onto the ground so W moves toward the top of the screen.
        Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
        Vector3 move = (forward * input.y + right * input.x) * moveSpeed * (inventory != null ? inventory.MoveSpeedFactor : 1f);

        IsOnIce = OnIce();
        Vector3 moving = move;
        if (IsOnIce)
        {
            slide = Vector3.MoveTowards(slide, move, iceAcceleration * Time.deltaTime);
            moving = slide;
        }
        else slide = move;

        verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
        controller.Move((moving + Vector3.up * verticalVelocity) * Time.deltaTime);
        return move;
    }

    private bool OnIce()
    {
        if (!searchedForMap) { map = FindAnyObjectByType<LevelMap>(); searchedForMap = true; }
        return (map != null && map.IsIceAt(transform.position)) || (IceZone.Count > 0 && IceZone.Covers(transform.position));
    }

    private void FaceTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return; // standing still: keep the current facing
        var target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }
}
