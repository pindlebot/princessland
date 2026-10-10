using UnityEngine;

// A ring on the ground under the monster the hero's spell is aimed at (SpellAbility.Target), so you can
// see which one Tab / RB picked. It's a flat pixel-art sprite drawn in code (honey gold with a plum
// outline, like the HUD), lying on the floor like a shadow and pulsing gently.
public class TargetMarker : MonoBehaviour
{
    private const int Diameter = 28;   // art pixels
    private const float Lift = 0.03f;  // above the floor, just over the shadows
    private static Sprite ring;

    private SpellAbility spell;
    private Transform marker;
    private SpriteRenderer sprite;

    private void Start()
    {
        spell = GetComponent<SpellAbility>();
        var go = new GameObject("TargetMarker");
        marker = go.transform;
        marker.rotation = Quaternion.Euler(90f, 0f, 0f);
        sprite = go.AddComponent<SpriteRenderer>();
        sprite.sprite = RingSprite();
        sprite.sortingOrder = -1; // under characters, level with their shadows
        sprite.enabled = false;
        PixelSnap.Register(marker);
    }

    private void OnDestroy()
    {
        if (marker == null) return;
        PixelSnap.Unregister(marker);
        Destroy(marker.gameObject);
    }

    private void LateUpdate()
    {
        var target = spell != null && !GameInput.GameplayBlocked ? spell.Target : null;
        sprite.enabled = target != null;
        if (target == null) return;
        var p = target.transform.position;
        marker.position = new Vector3(p.x, Lift, p.z);
        float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
        marker.localScale = new Vector3(pulse, pulse, 1f);
    }

    private static Sprite RingSprite()
    {
        if (ring != null) return ring;
        var texture = new Texture2D(Diameter, Diameter, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "TargetRing" };
        var gold = new Color32(250, 214, 120, 255);   // the HUD's --honey-light
        var outline = new Color32(74, 37, 69, 255);   // --ink
        float middle = (Diameter - 1) / 2f;
        for (int y = 0; y < Diameter; y++)
            for (int x = 0; x < Diameter; x++)
            {
                float r = Mathf.Sqrt((x - middle) * (x - middle) + (y - middle) * (y - middle));
                Color32 c = new Color32(0, 0, 0, 0);
                if (r >= 11f && r <= 12.5f) c = gold;
                else if ((r >= 9.8f && r < 11f) || (r > 12.5f && r <= 13.7f)) c = outline;
                // Gaps at the four compass points make it read as a reticle rather than a plain circle.
                if (Mathf.Abs(x - middle) < 1.6f || Mathf.Abs(y - middle) < 1.6f) c = new Color32(0, 0, 0, 0);
                texture.SetPixel(x, y, c);
            }
        texture.Apply();
        ring = Sprite.Create(texture, new Rect(0, 0, Diameter, Diameter), new Vector2(0.5f, 0.5f), ArtStyle.PixelsPerUnit);
        return ring;
    }
}
