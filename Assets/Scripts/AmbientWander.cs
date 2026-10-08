using UnityEngine;

// Gentle ambient motion around a home spot: butterflies flutter in lazy figure-eights,
// magic motes float upward and start again. Purely decorative (no collider), and it
// stays near where it was placed, so it can be kept away from the fighting.
public class AmbientWander : MonoBehaviour
{
    [SerializeField] private float radius = 1.5f;      // how far it wanders from home
    [SerializeField] private float speed = 0.6f;       // how fast it loops
    [SerializeField] private float height = 1.2f;     // hover height above the floor
    [SerializeField] private float bob = 0.25f;        // up-and-down flutter
    [SerializeField] private float rise = 0f;          // > 0: drift upward this far, then restart (motes)

    private Vector3 home;
    private float phase;

    private void Start()
    {
        home = transform.position;
        phase = Random.value * 100f; // so several of them don't move in step
    }

    private void Update()
    {
        float t = Time.time * speed + phase;
        // A figure-eight (Lissajous curve): x loops once while z loops twice.
        var offset = new Vector3(Mathf.Sin(t) * radius, 0f, Mathf.Sin(t * 2f) * radius * 0.5f);
        float y = height + Mathf.Sin(t * 5f) * bob;
        if (rise > 0f) y += Mathf.Repeat(t * 0.4f, 1f) * rise;
        transform.position = new Vector3(home.x + offset.x, home.y + y, home.z + offset.z);
    }
}
