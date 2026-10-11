using UnityEngine;

// What marks the monster the hero's spell is aimed at (SpellAbility.Target): a ring on the ground under it, sized to
// fit however big it is, and a little arrow bobbing over its head, so it can be found in a crowd and at a glance.
// The arrow is hollow while the spell has picked it by itself, and solid once the player has picked it with Tab / RB.
// Both are pixel art drawn in code (honey gold with a plum outline, like the HUD).
public class TargetMarker : MonoBehaviour
{
    private const int Diameter = 28;   // art pixels
    private const float Lift = 0.03f;  // above the floor, just over the shadows
    private static Sprite ring, arrowAuto, arrowChosen;

    private SpellAbility spell;
    private Transform marker, arrow;
    private SpriteRenderer sprite, arrowSprite;

    public bool IsShown => sprite != null && sprite.enabled;
    public Transform Arrow => arrow;

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

        var arrowGo = new GameObject("TargetArrow");
        arrow = arrowGo.transform;
        arrowSprite = arrowGo.AddComponent<SpriteRenderer>();
        arrowSprite.sortingOrder = 20; // over everything: it has to be findable behind scenery
        arrowSprite.enabled = false;
        arrowGo.AddComponent<Billboard>();
    }

    private void OnDestroy()
    {
        if (marker == null) return;
        PixelSnap.Unregister(marker);
        Destroy(marker.gameObject);
        if (arrow != null) Destroy(arrow.gameObject);
    }

    private void LateUpdate()
    {
        var target = spell != null && !GameInput.GameplayBlocked ? spell.Target : null;
        sprite.enabled = target != null;
        arrowSprite.enabled = target != null;
        if (target == null) return;

        var p = target.transform.position;
        float radius = 0.5f, height = 2f;
        if (target.TryGetComponent(out CharacterController body))
        {
            radius = body.radius;
            height = body.height;
        }
        marker.position = new Vector3(p.x, Lift, p.z);
        float fit = Mathf.Max(1f, (radius * 2f + 0.5f) / (Diameter / ArtStyle.PixelsPerUnit)); // a boss gets a bigger ring
        float pulse = fit * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f));
        marker.localScale = new Vector3(pulse, pulse, 1f);

        arrowSprite.sprite = ArrowSprite(spell.TargetWasChosen);
        float bob = Mathf.Sin(Time.unscaledTime * 5f) * 0.12f;
        arrow.position = new Vector3(p.x, p.y + height + 0.55f + bob, p.z);
    }

    // A downward arrowhead: gold with a plum outline. Solid for a Tab pick, an outline for the automatic one.
    private static Sprite ArrowSprite(bool chosen)
    {
        if (chosen && arrowChosen != null) return arrowChosen;
        if (!chosen && arrowAuto != null) return arrowAuto;
        const int w = 13, h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = chosen ? "TargetArrowChosen" : "TargetArrow" };
        var gold = new Color32(250, 214, 120, 255);
        var outline = new Color32(74, 37, 69, 255);
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // A triangle pointing down: it narrows by one pixel each side per row from the top.
                float half = (y + 0.5f) * (w / 2f) / h;   // grows with y: the tip is at the bottom (y = 0)
                float dx = Mathf.Abs(x + 0.5f - w / 2f);
                bool inside = dx <= half;
                bool edge = inside && (dx > half - 1.4f || y == h - 1 || y == 0);
                texture.SetPixel(x, y, !inside ? clear : edge ? outline : chosen ? gold : new Color32(255, 246, 224, 255));
            }
        texture.Apply();
        var made = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ArtStyle.PixelsPerUnit);
        if (chosen) arrowChosen = made; else arrowAuto = made;
        return made;
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
