using UnityEngine;

// For projectile sprites: face the camera, then spin around the view axis so the
// art (drawn pointing right) lines up with the direction the parent is flying,
// as it appears on screen.
public class FaceTravelDirection : MonoBehaviour
{
    private Camera cam;

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;

        // Express the flight direction in screen terms: how far right, how far up.
        Vector3 dir = transform.parent.forward;
        float right = Vector3.Dot(dir, cam.transform.right);
        float up = Vector3.Dot(dir, cam.transform.up);
        float angle = Mathf.Atan2(up, right) * Mathf.Rad2Deg;

        transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, angle);
    }
}
