using UnityEngine;

// A flat pixel blob shadow under a character or prop. It stays lying flat and lined up with
// the world (never turning with its owner, which would resample its pixels into a different
// jagged circle), leans a little away from the sun (ArtStyle.ShadowOffset, the same for
// everything), and is drawn on the screen-pixel grid like the sprites.
[RequireComponent(typeof(SpriteRenderer))]
public class GroundShadow : MonoBehaviour
{
    [SerializeField] private float lift = 0.02f; // above the owner's origin, to sit just over the floor

    private Vector3 offset;

    private void Awake() => offset = ArtStyle.ShadowOffset(ArtStyle.OutdoorSun);
    private void OnEnable() => PixelSnap.Register(transform);
    private void OnDisable() => PixelSnap.Unregister(transform);

    private void LateUpdate()
    {
        var owner = transform.parent;
        if (owner == null) return;
        transform.SetPositionAndRotation(owner.position + offset + Vector3.up * lift, Quaternion.Euler(90f, 0f, 0f));
    }
}
