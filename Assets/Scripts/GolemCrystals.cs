using UnityEngine;

// The Crystal Golem's weak spot. His topaz crystals take turns: while they GLOW (and a warm light shines from them)
// he can be hurt; while they're DIM the room goes dark and his stone hide shrugs every spell off with a "tink".
// The crystals light up for a few seconds, then go dark again, so the fight is: dodge while it's dark (the Fairy
// Lantern helps you see), and strike while it glows.
[RequireComponent(typeof(Health), typeof(BossAbilities))]
public class GolemCrystals : MonoBehaviour
{
    [SerializeField] private float glowSeconds = 6f;
    [SerializeField] private float dimSeconds = 5f;
    [SerializeField] private float startDimFor = 3f;     // a moment of dark first, so the first glow is a surprise
    [SerializeField] private Light glowLight;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AudioClip glowSound;
    [SerializeField] private AudioClip dimSound;
    [SerializeField] private AudioClip clinkSound;

    private static readonly Color DimTint = new Color(0.55f, 0.55f, 0.72f);

    private Health health;
    private BossAbilities boss;
    private Light sun;
    private Color ambientBefore;
    private float sunBefore;
    private float phaseEnds;

    public bool IsGlowing { get; private set; }
    public float GlowSeconds => glowSeconds;
    public float DimSeconds => dimSeconds;

    private void Awake()
    {
        health = GetComponent<Health>();
        boss = GetComponent<BossAbilities>();
        health.AdjustDamage = amount => IsGlowing ? amount : 0;   // a hit while it's dark costs him nothing
        health.Damaged += _ => { if (!IsGlowing && !health.IsDead) AudioManager.Play(clinkSound, 0.7f); };
        health.Died += _ => RestoreLight();
    }

    private void Start()
    {
        foreach (var light in FindObjectsByType<Light>())
            if (light.type == LightType.Directional) sun = light;
        ambientBefore = RenderSettings.ambientLight;
        sunBefore = sun != null ? sun.intensity : 0f;
        SetGlowing(false, announce: false);
        phaseEnds = Time.time + startDimFor;
    }

    private void Update()
    {
        if (health.IsDead || DialogueController.IsOpen) return;
        if (!boss.IsEngaged) { phaseEnds = Mathf.Max(phaseEnds, Time.time + 0.5f); return; } // the clock starts when he wakes
        if (Time.time >= phaseEnds) SetGlowing(!IsGlowing, announce: true);
    }

    private void OnDestroy() => RestoreLight();

    // Public so tests can step through the phases.
    public void SetGlowing(bool glowing, bool announce)
    {
        IsGlowing = glowing;
        phaseEnds = Time.time + (glowing ? glowSeconds : dimSeconds);
        if (glowLight != null) glowLight.enabled = glowing;
        if (sprite != null) sprite.color = glowing ? Color.white : DimTint;
        RenderSettings.ambientLight = glowing ? ambientBefore : ambientBefore * 0.35f;
        if (sun != null) sun.intensity = glowing ? sunBefore : sunBefore * 0.25f;
        if (!announce) return;
        AudioManager.Play(glowing ? glowSound : dimSound);
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast(glowing ? "His crystals are glowing! Hit him now!" : "The crystals go dark... the cave goes dark too!");
    }

    private void RestoreLight()
    {
        if (sun == null && sunBefore <= 0f) return;
        RenderSettings.ambientLight = ambientBefore;
        if (sun != null) sun.intensity = sunBefore;
    }
}
