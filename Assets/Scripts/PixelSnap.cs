using System.Collections.Generic;
using UnityEngine;

// Keeps pixel art crisp while things move. Point-filtered sprites drawn at a fraction of a
// screen pixel come out with some art pixels a screen pixel wider than others, which
// shimmers as they move. So, just before the main camera draws, every registered transform
// is nudged onto the camera's screen-pixel grid, and put back straight afterwards: gameplay,
// physics and other scripts only ever see the real positions.
//
// Billboards (upright sprites) register themselves; flat sprites such as blob shadows get
// this component.
[DisallowMultipleComponent]
public class PixelSnap : MonoBehaviour
{
    private static readonly List<Transform> Registered = new List<Transform>();
    // Local positions, so putting things back works in any order, even for a snapped child of
    // a snapped parent.
    private static readonly List<(Transform, Vector3)> Saved = new List<(Transform, Vector3)>();
    private static bool hooked;

    private void OnEnable() => Register(transform);
    private void OnDisable() => Unregister(transform);

    public static void Register(Transform t)
    {
        if (!hooked)
        {
            Camera.onPreCull += Snap;
            Camera.onPostRender += Restore;
            hooked = true;
        }
        if (!Registered.Contains(t)) Registered.Add(t);
    }

    public static void Unregister(Transform t) => Registered.Remove(t);

    private static void Snap(Camera cam)
    {
        Saved.Clear();
        if (cam != Camera.main || !cam.orthographic) return;

        // World units per screen pixel, and the camera's on-screen axes.
        float unit = 2f * cam.orthographicSize / cam.pixelHeight;
        Transform view = cam.transform;
        Vector3 origin = view.position, right = view.right, up = view.up, forward = view.forward;
        foreach (var t in Registered)
        {
            Saved.Add((t, t.localPosition));
            Vector3 d = t.position - origin;
            float x = Mathf.Round(Vector3.Dot(d, right) / unit) * unit;
            float y = Mathf.Round(Vector3.Dot(d, up) / unit) * unit;
            t.position = origin + right * x + up * y + forward * Vector3.Dot(d, forward);
        }
    }

    private static void Restore(Camera cam)
    {
        foreach (var (t, local) in Saved)
            if (t != null) t.localPosition = local;
        Saved.Clear();
    }
}
