using UnityEngine;

// A floor tile that hurts the hero: lava ('~') always, a spike trap ('^') only while its
// spikes are up. Spike traps run on a loop (hidden, a warning peek, up) so you can time
// your run past them, and neighbouring traps are a little out of step, like a wave.
//
// Like WakeFountain, it compares the hero's position with its own square every frame
// instead of using trigger colliders: simple, and it can't miss a hero who was teleported.
// One shared cooldown means a dash across three lava tiles costs one heart, not three.
public class Hazard : MonoBehaviour
{
    public enum Kind { Lava, Spikes }
    public enum SpikeState { Hidden, Warning, Up }

    // One spike cycle, in seconds. Public so tests (and level designers) can reason about it.
    public const float HiddenSeconds = 1.8f, WarningSeconds = 0.6f, UpSeconds = 1.0f;
    public const float CycleSeconds = HiddenSeconds + WarningSeconds + UpSeconds;
    public const float HurtEverySeconds = 1f; // how often a hazard can sting while you stay on it

    [SerializeField] private Kind kind = Kind.Lava;
    [SerializeField] private int damage = 1;
    [Tooltip("Half the width of the square that hurts. A bit under the 1m half-tile, so brushing the edge is free.")]
    [SerializeField] private float reach = 0.8f;
    [SerializeField] private AudioClip hurtSound; // a sizzle, on top of the hero's own "oof"

    [Header("Spikes")]
    [SerializeField] private Transform spikes;   // slides up through the plate's holes
    [SerializeField] private float phase;        // seconds into the cycle at time 0
    [SerializeField] private AudioClip riseSound;
    [SerializeField] private float hiddenY = -1f, warningY = -0.72f, upY = 0f; // the spikes are 0.95m tall

    // Who was stung last, and when (shared by every hazard).
    private static Health lastVictim;
    private static float lastHurtTime;

    private SpikeState lastState;

    public Kind Type => kind;
    public SpikeState State => kind == Kind.Lava ? SpikeState.Up : StateAt(Time.time + phase);
    public bool IsDangerous => kind == Kind.Lava || State == SpikeState.Up;

    // Where in the cycle a spike trap is at a given (phase-adjusted) time.
    public static SpikeState StateAt(float time)
    {
        float t = Mathf.Repeat(time, CycleSeconds);
        return t < HiddenSeconds ? SpikeState.Hidden
             : t < HiddenSeconds + WarningSeconds ? SpikeState.Warning
             : SpikeState.Up;
    }

    // How long until the spikes are next in `state` (0 if they already are).
    public float SecondsUntil(SpikeState state)
    {
        for (float wait = 0f; wait < CycleSeconds; wait += 0.02f)
            if (StateAt(Time.time + phase + wait) == state) return wait;
        return 0f;
    }

    public bool Contains(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        return Mathf.Abs(offset.x) < reach && Mathf.Abs(offset.z) < reach;
    }

    private void Start()
    {
        lastState = State;
        if (spikes != null) spikes.localPosition = new Vector3(0f, TargetY(lastState), 0f);
    }

    private void Update()
    {
        var state = State;
        if (spikes != null) MoveSpikes(state);

        var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        if (player != null && state != lastState && state == SpikeState.Up)
            PlayNearby(riseSound, player.transform.position);
        lastState = state;

        if (player == null || !IsDangerous || !Contains(player.transform.position)) return;
        var health = player.GetComponent<Health>();
        if (health == null || health.IsDead) return;
        if (health == lastVictim && Time.time < lastHurtTime + HurtEverySeconds) return;

        lastVictim = health;
        lastHurtTime = Time.time;
        health.TakeDamage(damage);
        AudioManager.Play(hurtSound, 0.8f);
    }

    // Spikes snap up quickly (that's the danger) and sink back slowly.
    private void MoveSpikes(SpikeState state)
    {
        var p = spikes.localPosition;
        float speed = state == SpikeState.Up ? 12f : state == SpikeState.Warning ? 2f : 1.5f;
        p.y = Mathf.MoveTowards(p.y, TargetY(state), speed * Time.deltaTime);
        spikes.localPosition = p;
    }

    private float TargetY(SpikeState state) =>
        state == SpikeState.Up ? upY : state == SpikeState.Warning ? warningY : hiddenY;

    // Effects play at full volume wherever they come from, so only traps near the hero
    // make a sound (otherwise a corridor of them would clatter constantly).
    private void PlayNearby(AudioClip clip, Vector3 hero)
    {
        Vector3 offset = hero - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 9f * 9f) AudioManager.Play(clip, 0.5f);
    }
}
