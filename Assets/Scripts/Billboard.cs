using UnityEngine;

// Keeps a sprite facing the camera even though its parent rotates to aim.
// It only turns around the vertical axis and stays upright, so the sprite never
// leans back into walls. Standing upright makes the tilted camera squash it
// vertically, so the height is stretched by 1/cos(pitch) to compensate.
public class Billboard : MonoBehaviour
{
    private Camera cam;

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;

        Vector3 angles = cam.transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, angles.y, 0f);
        transform.localScale = new Vector3(1f, 1f / Mathf.Cos(angles.x * Mathf.Deg2Rad), 1f);
    }
}
