using System;
using UnityEngine;

// The warning on the floor before a boss attack lands. Every boss attack gets one, and each kind has its own SHAPE, so it
// can be told apart without colour (on snow, over water, under a spell's glow, or by a player who can't tell red from green):
//
//   Circle  a ring of chunky dashes (plum + cream) round a striped disc    slams and cannon shells: "this spot"
//   Fan     a wedge with one solid lane per bolt and dashed edges          volleys: "the lanes; the gaps are safe"
//   Band    a strip of arrowheads pointing the way it will travel, with    the tide, the snowball lanes: "this lane"
//           plum rails along both edges
//
// Each also *moves* and *sounds* as time runs out: in the last third of a second the outline blinks and swells, and a
// three-beep tick (warn_tick) says "now". Gentle Mode lengthens the warning (BossAbilities.GentleWindupFactor); no
// warning is ever shorter than MinWarningSeconds.
public static class Telegraph
{
    // No boss attack may hit sooner than this after its warning appears (Gentle Mode only lengthens it).
    public const float MinWarningSeconds = 0.8f;
    // The blink and the tick start this long before the attack.
    public const float FinalSeconds = 0.35f;

    private const float Lift = 0.06f;

    public static float Clamp(float seconds) => Mathf.Max(seconds, MinWarningSeconds);

    // A danger circle of `radius` on the floor at `center`.
    public static TelegraphMarker Circle(Vector3 center, float radius, float seconds, bool tick = true)
    {
        var marker = Make("TelegraphCircle", new Vector3(center.x, Lift, center.z), seconds, tick);
        var fill = Flat(marker.transform, "Stripes", FeedbackArt.StripedDisc(), -2);
        fill.transform.localScale = Vector3.one * radius;
        var ring = Flat(marker.transform, "Ring", FeedbackArt.DashedRing(), 4);
        ring.transform.localScale = Vector3.one * radius;
        marker.Pulse(ring.transform, radius);
        marker.Blink(ring);
        marker.Kind = TelegraphKind.Circle;
        marker.Radius = radius;
        return marker;
    }

    // A fan of lanes from `origin` along `aim()` (re-read every frame until it locks, in the final moments, so the player
    // sees where it settles). `bolts` lanes spread across `arcDegrees`, `length` metres long.
    public static TelegraphMarker Fan(Transform origin, Func<Vector3> aim, int bolts, float arcDegrees, float length, float seconds)
    {
        var marker = Make("TelegraphFan", new Vector3(origin.position.x, Lift, origin.position.z), seconds, true);
        var wedge = Flat(marker.transform, "Fan", FeedbackArt.Fan(bolts, arcDegrees), -1);
        wedge.transform.localScale = new Vector3(length, length, 1f);
        marker.Blink(wedge);
        marker.Kind = TelegraphKind.Fan;
        marker.Radius = length;
        marker.Follow = () =>
        {
            if (origin == null) return;
            marker.transform.position = new Vector3(origin.position.x, Lift, origin.position.z);
            Aim(marker.transform, aim());
        };
        Aim(marker.transform, aim());
        return marker;
    }

    // A strip `length` long and `width` across, centred at `center`, with arrowheads pointing along `travel`.
    public static TelegraphMarker Band(Vector3 center, Vector3 travel, float length, float width, float seconds)
    {
        var marker = Make("TelegraphBand", new Vector3(center.x, Lift, center.z), seconds, true);
        var strip = Flat(marker.transform, "Chevrons", FeedbackArt.Chevrons(), -1);
        strip.drawMode = SpriteDrawMode.Tiled;
        strip.tileMode = SpriteTileMode.Continuous;
        strip.size = new Vector2(length, width);
        marker.Blink(strip);
        marker.Kind = TelegraphKind.Band;
        marker.Radius = width / 2f;
        Aim(marker.transform, travel);
        return marker;
    }

    // Lay a flat object so its local +x points along `direction` on the floor.
    private static void Aim(Transform t, Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        float yaw = Mathf.Atan2(-direction.z, direction.x) * Mathf.Rad2Deg;
        t.rotation = Quaternion.Euler(90f, yaw, 0f);
    }

    private static TelegraphMarker Make(string name, Vector3 at, float seconds, bool tick)
    {
        var go = new GameObject(name);
        go.transform.position = at;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var marker = go.AddComponent<TelegraphMarker>();
        marker.Begin(Clamp(seconds), tick);
        return marker;
    }

    private static SpriteRenderer Flat(Transform parent, string name, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = SpriteMaterial.Apply(go.AddComponent<SpriteRenderer>());
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }
}

public enum TelegraphKind { Circle, Fan, Band }

// One live warning. It blinks and ticks as its time runs out, and removes itself when told (Finish) or when its time is
// up plus a short grace (so a boss that is beaten mid-attack never leaves a warning behind).
public class TelegraphMarker : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<TelegraphMarker> active = new System.Collections.Generic.List<TelegraphMarker>();
    public static System.Collections.Generic.IReadOnlyList<TelegraphMarker> Active => active;
    public static int Created { get; private set; }

    public TelegraphKind Kind { get; set; }
    public float Radius { get; set; }          // circle: its radius; fan: its length; band: half its width
    public float Seconds { get; private set; } // the warning time it was given
    public bool IsLocked { get; private set; } // in its final moments: blinking, ticked
    public Action Follow;                       // a fan re-aims every frame until it locks

    private float born;
    private bool tick, ticked;
    private SpriteRenderer blinker;
    private Transform pulser;
    private float pulseBase;

    public float Age => Time.time - born;
    public float Remaining => Mathf.Max(0f, Seconds - Age);

    public void Begin(float seconds, bool tick)
    {
        Seconds = seconds;
        this.tick = tick;
        born = Time.time;
        Created++;
        active.Add(this);
    }

    public void Blink(SpriteRenderer renderer) => blinker = renderer;
    public void Pulse(Transform t, float baseScale) { pulser = t; pulseBase = baseScale; }

    private void OnDestroy() => active.Remove(this);

    // The attack is happening (or was cancelled): take the warning away.
    public void Finish() { if (this != null) Destroy(gameObject); }

    private void Update()
    {
        if (Age > Seconds + 1.5f) { Destroy(gameObject); return; }   // never left behind
        if (!IsLocked && Remaining <= Telegraph.FinalSeconds)
        {
            IsLocked = true;
            Follow = null;   // the aim settles here
        }
        Follow?.Invoke();
        if (IsLocked && tick && !ticked)
        {
            ticked = true;
            AudioManager.Play(ActionFeedback.Clip("warn_tick"), 0.8f);
        }

        if (blinker != null)
        {
            // Calm and steady, then in the last moments a fast blink between plain and bright.
            bool bright = IsLocked && Mathf.FloorToInt(Time.time * 14f) % 2 == 0;
            blinker.color = bright ? new Color(1f, 0.95f, 0.55f, 1f) : Color.white;
        }
        if (pulser != null)
        {
            float swell = IsLocked ? 1f + 0.07f * Mathf.Sin(Time.time * 30f) : 1f;
            pulser.localScale = Vector3.one * (pulseBase * swell);
        }
    }
}
