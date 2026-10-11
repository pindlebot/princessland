using System;
using System.Collections;
using UnityEngine;

// How a hit on a boss with a weak spot LOOKS and SOUNDS, so "that worked" and "that bounced off" are never the same:
//
//   Impact   his weak spot was open: a gold starburst over him, a crisp crack-and-sparkle, and a little hop.
//   Deflect  his shell / stone hide shrugged it off: a grey shield with a red slash, a bright double "ting", a sideways
//            shudder, and the "not yet" badge on the HUD (ActionFeedback.Fail(ShellClosed)). No white flash, no hurt sound.
//
// Crabbington's shell (ShellCycle) and the Crystal Golem's glow (GolemCrystals) both use this through VulnerabilityCue,
// which also keeps a small picture over the boss's head the whole time: a gold star while he can be hurt, a shield
// while he can't. That way the window can be read from across the room, not just from the toast.
public static class HitFeedback
{
    public static int Impacts { get; private set; }
    public static int Deflections { get; private set; }

    public static void Impact(Vector3 at)
    {
        Impacts++;
        AudioManager.Play(ActionFeedback.Clip("weakspot_hit"));
        Pop(FeedbackArt.ImpactBurst(), at, 0.5f, 1.5f, 0.35f, rise: 0.4f);
    }

    public static void Deflect(Vector3 at, string message)
    {
        Deflections++;
        AudioManager.Play(ActionFeedback.Clip("deflect"));   // every bounce rings; the badge below is rate-limited
        ActionFeedback.Fail(FailReason.ShellClosed, message, playSound: false);
        Pop(FeedbackArt.DeflectShield(), at, 0.8f, 1.2f, 0.5f, rise: 0.2f);
    }

    // A picture that grows, rises and fades over `seconds`, always facing the camera.
    private static void Pop(Sprite sprite, Vector3 at, float from, float to, float seconds, float rise)
    {
        var root = new GameObject("HitPop");
        root.transform.position = at;
        root.AddComponent<Billboard>();
        var art = new GameObject("Art");
        art.transform.SetParent(root.transform, false);
        var renderer = SpriteMaterial.Apply(art.AddComponent<SpriteRenderer>());
        renderer.sprite = sprite;
        renderer.sortingOrder = 25;
        root.AddComponent<PopRunner>().Begin(art.transform, renderer, from, to, seconds, rise);
    }

    private sealed class PopRunner : MonoBehaviour
    {
        public void Begin(Transform art, SpriteRenderer renderer, float from, float to, float seconds, float rise) =>
            StartCoroutine(Run(art, renderer, from, to, seconds, rise));

        private IEnumerator Run(Transform art, SpriteRenderer renderer, float from, float to, float seconds, float rise)
        {
            var start = transform.position;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float f = t / seconds;
                art.localScale = Vector3.one * Mathf.Lerp(from, to, 1f - (1f - f) * (1f - f));   // eases out
                transform.position = start + Vector3.up * (rise * f);
                var c = renderer.color;
                c.a = f < 0.6f ? 1f : 1f - (f - 0.6f) / 0.4f;
                renderer.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}

// The boss's own half of it: listens to his Health, reacts to each hit, and shows the picture over his head.
// Added at runtime by the component that knows when he is vulnerable (ShellCycle, GolemCrystals).
public class VulnerabilityCue : MonoBehaviour
{
    private Health health;
    private Func<bool> isVulnerable;
    private string deflectMessage;
    private Transform shaker;
    private Vector3 appliedOffset;
    private Transform cue;
    private SpriteRenderer cueSprite;
    private bool lastState;
    private float jiggleUntil;
    private float hopUntil;

    public bool ShowingOpen => cueSprite != null && cueSprite.sprite == FeedbackArt.ImpactBurst();

    // `shaker` is what shudders or hops (his sprite, or his shell): it keeps its own position the rest of the time.
    public void Bind(Health health, Func<bool> isVulnerable, string deflectMessage, Transform shaker)
    {
        this.health = health;
        this.isVulnerable = isVulnerable;
        this.deflectMessage = deflectMessage;
        this.shaker = shaker;
        health.Damaged += OnDamaged;
        health.Deflected += OnDeflected;

        var go = new GameObject("WeakSpotCue");
        cue = go.transform;
        go.AddComponent<Billboard>();
        cueSprite = SpriteMaterial.Apply(go.AddComponent<SpriteRenderer>());
        cueSprite.sortingOrder = 22;
        lastState = !isVulnerable();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Deflected -= OnDeflected;
        }
        if (cue != null) Destroy(cue.gameObject);
    }

    private Vector3 Chest => transform.position + Vector3.up * 1.2f;

    private void OnDamaged(Health h)
    {
        if (!isVulnerable() || h.IsDead) return;   // (a deflected hit arrives as Deflected too: only real damage counts here)
        if (h.LastDamage <= 0) return;
        HitFeedback.Impact(Chest);
        hopUntil = Time.time + 0.15f;
    }

    private void OnDeflected(Health h)
    {
        HitFeedback.Deflect(Chest, deflectMessage);
        jiggleUntil = Time.time + 0.18f;
    }

    private void LateUpdate()
    {
        if (cue == null || health == null) return;
        bool open = isVulnerable();
        if (open != lastState || cueSprite.sprite == null)
        {
            lastState = open;
            cueSprite.sprite = open ? FeedbackArt.ImpactBurst() : FeedbackArt.DeflectShield();
        }
        cueSprite.enabled = !health.IsDead;

        float height = 3.4f;
        if (TryGetComponent(out CharacterController body)) height = body.height + body.center.y + 0.9f;
        cue.position = transform.position + Vector3.up * (height + (open ? Mathf.Sin(Time.time * 6f) * 0.1f : 0f));
        cueSprite.color = open ? Color.white : new Color(1f, 1f, 1f, 0.75f);

        if (shaker == null) return;
        var offset = Vector3.zero;
        if (Time.time < jiggleUntil) offset += new Vector3(Mathf.Sin(Time.time * 90f) * 0.12f, 0f, 0f);
        if (Time.time < hopUntil) offset += Vector3.up * 0.12f;
        // Relative to wherever its own animation has put it this frame, so the two never fight.
        shaker.localPosition += offset - appliedOffset;
        appliedOffset = offset;
    }
}
