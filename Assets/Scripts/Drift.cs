using UnityEngine;

// Slowly drifts sideways and wraps around, like clouds passing the floating island.
public class Drift : MonoBehaviour
{
    [SerializeField] private Vector3 velocity = new Vector3(0.4f, 0f, 0.2f);
    [SerializeField] private float range = 30f; // wraps after travelling this far

    private Vector3 start;

    private void Start() => start = transform.position;

    private void Update()
    {
        transform.position += velocity * Time.deltaTime;
        if (Vector3.Distance(transform.position, start) > range)
            transform.position = start - velocity.normalized * range * 0.5f;
    }
}
