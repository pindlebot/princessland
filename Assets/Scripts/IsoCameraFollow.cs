using UnityEngine;

// Orthographic camera at the classic isometric angle (30° down, 45° around)
// that smoothly follows a target.
[RequireComponent(typeof(Camera))]
public class IsoCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 20f;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private Vector3 angles = new Vector3(30f, 45f, 0f);

    private Vector3 velocity;
    private float shakeUntil, shakeAmount;

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
        transform.position = target.position - transform.forward * distance;
        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        // LateUpdate runs after all Update calls, so the player has already moved this frame.
        if (target == null) return;

        transform.rotation = Quaternion.Euler(angles);
        Vector3 desired = target.position - transform.forward * distance;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        // Shake: nudge the camera to a random nearby spot each frame while the shake lasts.
        if (Time.time < shakeUntil)
            transform.position += Random.insideUnitSphere * shakeAmount;
    }
}
