using UnityEngine;

// King Crabbington's weak spot. He hides in his great shell, where spells just tink off and the tide does his
// fighting for him (TideWaves), then peeks out, and only while he's out can he be hurt (and he slams the ground,
// as bosses do). Hit him when he peeks out.
[RequireComponent(typeof(Health), typeof(BossAbilities), typeof(EnemyAI))]
public class ShellCycle : MonoBehaviour
{
    [SerializeField] private float peekSeconds = 6f;
    [SerializeField] private float hideSeconds = 6f;
    [SerializeField] private float startHiddenFor = 3f;
    [SerializeField] private GameObject shell;          // the shell sprite, shown while he hides
    [SerializeField] private SpriteRenderer body;       // his own sprite, hidden while he hides
    [SerializeField] private AudioClip hideSound;
    [SerializeField] private AudioClip peekSound;
    [SerializeField] private AudioClip clinkSound;   // (the old "tink"; HitFeedback now rings the deflect sound. Kept so the builder's reference stays valid.)

    private Health health;
    private BossAbilities boss;
    private EnemyAI ai;
    private float phaseEnds;

    public bool IsPeeking { get; private set; }
    public float PeekSeconds => peekSeconds;
    public float HideSeconds => hideSeconds;

    private void Awake()
    {
        health = GetComponent<Health>();
        boss = GetComponent<BossAbilities>();
        ai = GetComponent<EnemyAI>();
        health.AdjustDamage = amount => IsPeeking ? amount : 0;    // a hit on the shell costs him nothing
        // A hit on the shell bounces (a grey shield and a "ting", no flash); a hit while he peeks lands (a gold burst).
        // The cue also keeps a picture over him: a shield while he hides, a star while he peeks.
        health.DeflectsNoDamageHits = true;
        gameObject.AddComponent<VulnerabilityCue>().Bind(health, () => IsPeeking, "Wait till he peeks out!", shell != null ? shell.transform : null);
        health.Died += _ => { if (shell != null) shell.SetActive(false); if (body != null) body.enabled = true; };
    }

    private void Start()
    {
        SetPeeking(false, announce: false);
        phaseEnds = Time.time + startHiddenFor;
    }

    private void Update()
    {
        if (health.IsDead || DialogueController.IsOpen) return;
        if (!boss.IsEngaged) { phaseEnds = Mathf.Max(phaseEnds, Time.time + 0.5f); return; } // the clock starts when he wakes
        if (Time.time < phaseEnds) return;
        if (IsPeeking && (boss.IsSlamming || boss.IsVolleying)) return; // finish the slam before clamming up
        SetPeeking(!IsPeeking, announce: true);
    }

    // Public so tests can step through the phases.
    public void SetPeeking(bool peeking, bool announce)
    {
        IsPeeking = peeking;
        phaseEnds = Time.time + (peeking ? peekSeconds : hideSeconds);
        if (shell != null) shell.SetActive(!peeking);
        if (body != null) body.enabled = peeking;
        ai.enabled = peeking && !health.IsDead;
        boss.HoldSpecials = !peeking;
        if (!announce) return;
        AudioManager.Play(peeking ? peekSound : hideSound);
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast(peeking ? "He's peeking out! Hit him now!" : "He clams up! Watch out for the tide!");
    }
}
