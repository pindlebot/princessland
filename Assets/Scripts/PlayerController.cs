using UnityEngine;

// WASD movement on the isometric grid. The player turns to face the way they're
// walking; abilities can briefly take over facing (e.g. to look at a spell's target).
// The hero can also sit down on something (the toilet at home): SitOn puts them on it,
// facing the camera, until they walk away or StandUp is called.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float turnSpeed = 900f; // degrees per second

    private CharacterController controller;
    private Camera cam;
    private float verticalVelocity;
    private float holdFacingUntil;
    private Vector3 standingPosition; // where the hero stood before sitting down
    private float spriteHeight;       // the sprite's usual height above the root

    // What the hero is sitting on (null when standing).
    public Transform Seat { get; private set; }
    public bool IsSeated => Seat != null;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return;

        if (IsSeated)
        {
            // Pushing the stick or a direction key gets up again.
            if (GameInput.Move.sqrMagnitude > 0.25f && !GameInput.GameplayBlocked) StandUp();
            return;
        }

        Vector3 move = Move();
        if (Time.time >= holdFacingUntil)
            FaceTowards(move);
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
    public void SitOn(Transform seat, Vector3 seatPosition, float height)
    {
        if (IsSeated) StandUp();
        Seat = seat;
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
        Vector3 move = (forward * input.y + right * input.x) * moveSpeed;

        verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
        controller.Move((move + Vector3.up * verticalVelocity) * Time.deltaTime);
        return move;
    }

    private void FaceTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return; // standing still: keep the current facing
        var target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }
}
