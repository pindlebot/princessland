using UnityEngine;

// WASD movement on the isometric grid. The player turns to face the way they're
// walking; abilities can briefly take over facing (e.g. to look at a spell's target).
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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

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

    private Vector3 Move()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

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
