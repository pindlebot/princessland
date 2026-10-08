using UnityEngine;

// Orthographic camera at the classic isometric angle (30° down, 45° around)
// that smoothly follows a target.
//
// Pixel-perfect: the view is sized so each art pixel (1/16 unit) covers a whole number of
// screen pixels (3 at 720p, 4 at 1080p, 6 at 1440p...), as close to TargetOrthoSize as that
// allows, and the camera sits on the screen-pixel grid. The smoothing runs on an unsnapped
// position, so following still feels soft; only what's drawn is snapped.
[RequireComponent(typeof(Camera))]
public class IsoCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 20f;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector3 angles = new Vector3(30f, 45f, 0f);
    [SerializeField] private bool pixelPerfect = true;

    private Camera cam;
    private Vector3 smoothPosition; // where the camera would be without snapping
    private Vector3 velocity;
    private float shakeUntil, shakeAmount;

    // Screen pixels per art pixel right now (for anything that wants to match it).
    public int PixelScale { get; private set; } = 1;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        smoothPosition = transform.position;
    }

    // A short screen shake for big impacts (the boss landing).
    public void Shake(float amount, float seconds)
    {
        shakeAmount = amount;
        shakeUntil = Time.time + seconds;
    }

    // Follow a new target, jumping straight to it (no swoop across the level).
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        transform.rotation = Quaternion.Euler(angles);
        smoothPosition = target.position - transform.forward * distance;
        velocity = Vector3.zero;
        Apply();
    }

    private void LateUpdate()
    {
        // LateUpdate runs after all Update calls, so the player has already moved this frame.
        if (target == null) return;

        transform.rotation = Quaternion.Euler(angles);
        Vector3 desired = target.position - transform.forward * distance;
        smoothPosition = Vector3.SmoothDamp(smoothPosition, desired, ref velocity, smoothTime);
        Apply();

        // Shake: nudge the camera to a random nearby spot each frame while the shake lasts.
        if (Time.time < shakeUntil)
            transform.position += Random.insideUnitSphere * shakeAmount;
    }

    private void Apply()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (!pixelPerfect || !cam.orthographic)
        {
            transform.position = smoothPosition;
            return;
        }

        // A whole number of screen pixels per art pixel.
        float height = cam.pixelHeight;
        PixelScale = Mathf.Max(1, Mathf.RoundToInt(height / (2f * ArtStyle.TargetOrthoSize * ArtStyle.PixelsPerUnit)));
        cam.orthographicSize = height / (2f * PixelScale * ArtStyle.PixelsPerUnit);

        // Snap across the screen (right/up); depth along the view doesn't move anything on screen.
        float unit = 2f * cam.orthographicSize / height;
        Vector3 right = transform.right, up = transform.up, forward = transform.forward;
        float x = Mathf.Round(Vector3.Dot(smoothPosition, right) / unit) * unit;
        float y = Mathf.Round(Vector3.Dot(smoothPosition, up) / unit) * unit;
        transform.position = right * x + up * y + forward * Vector3.Dot(smoothPosition, forward);
    }
}
