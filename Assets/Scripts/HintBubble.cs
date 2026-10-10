using System.Collections.Generic;
using UnityEngine;

// The "come back later" sign over a gate you can't open yet: a thought bubble with a picture of what
// you need (the boots, the lantern) and a "?". It fades in as you come near, and only on the closest
// gate, so a row of gap tiles shows one bubble. Once you have the ability it never appears again.
//
// The first time you meet each kind of gate you also get a toast and a sound, and every gate you
// have seen is remembered ("seen_gate:<id>"), which the world map and the checkpoint question
// ("does she remember the gap?") read.
public class HintBubble : MonoBehaviour
{
    [Tooltip("The item id of the ability that opens this gate (Abilities.BouncyBoots).")]
    [SerializeField] private string ability;
    [SerializeField] private string message = "Hmm, I can't get across. Maybe something bouncy would help?";
    [SerializeField] private string gateId = "";         // unique per gate (scene/col,row), for the "seen" flag
    [SerializeField] private SpriteRenderer bubble;
    [SerializeField] private float showRange = 7f;
    [SerializeField] private AudioClip hintSound;

    private static readonly List<HintBubble> all = new List<HintBubble>();
    private static int nearestFrame = -1;
    private static HintBubble nearest;

    private float alpha;
    private Vector3 bubbleStart;

    public string Ability => ability;
    public bool IsShown => alpha > 0.5f;
    public static string SeenFlag(string gateId) => "seen_gate:" + gateId;
    public static string FirstFlag(string ability) => "hint:" + ability;

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Start()
    {
        bubbleStart = bubble.transform.localPosition;
        SetAlpha(0f);
        if (Abilities.Has(ability)) gameObject.SetActive(false);
    }

    private void Update()
    {
        var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        bool want = player != null && !Abilities.Has(ability) && Nearest(player.transform.position) == this;
        alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, Time.deltaTime * 3f);
        SetAlpha(alpha);
        bubble.transform.localPosition = bubbleStart + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * 0.08f);
        if (want) OnSeen();
    }

    private void OnSeen()
    {
        if (gateId.Length > 0) GameSession.Flags.Add(SeenFlag(gateId));
        if (!GameSession.Flags.Add(FirstFlag(ability))) return;
        AudioManager.Play(hintSound);
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast(message);
    }

    private void SetAlpha(float a)
    {
        var c = bubble.color;
        c.a = a;
        bubble.color = c;
    }

    // The closest bubble (within range) that still needs an ability, worked out once per frame.
    private static HintBubble Nearest(Vector3 from)
    {
        if (nearestFrame == Time.frameCount) return nearest;
        nearestFrame = Time.frameCount;
        nearest = null;
        float best = float.MaxValue;
        foreach (var b in all)
        {
            var to = b.transform.position - from;
            to.y = 0f;
            float d = to.magnitude;
            if (d <= b.showRange && d < best && !Abilities.Has(b.ability)) { best = d; nearest = b; }
        }
        return nearest;
    }
}
