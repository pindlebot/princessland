using System;
using UnityEngine;

// Reusable hit-point component. Both the player and enemies use it, and other
// scripts react to damage/death by subscribing to the events below.
public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead => Current <= 0;

    public event Action<Health> Damaged;
    public event Action<Health> Died;
    public event Action<Health> Revived;
    public event Action<Health> Healed;

    // Optional: changes incoming damage (Gentle Mode halves the hero's). Returns the new amount.
    public Func<int, int> AdjustDamage;
    public bool PlayDeathSound = true;

    // No damage at all until this time (e.g. just after waking up).
    public float InvulnerableUntil;
    public bool IsInvulnerable => Time.time < InvulnerableUntil;

    private Renderer[] renderers;
    private float flashTimer;

    private void Awake()
    {
        Current = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || IsInvulnerable) return;
        if (AdjustDamage != null) amount = AdjustDamage(amount);

        // Even a hit that costs no heart still flashes and makes a sound: it was a "bump".
        Current = Mathf.Max(0, Current - amount);
        flashTimer = 0.1f;
        SetFlash(true);
        Damaged?.Invoke(this);
        AudioManager.Play(IsDead ? (PlayDeathSound ? deathSound : null) : hurtSound);

        if (IsDead) Died?.Invoke(this);
    }

    // Levels and skills raise max health. Current health never ends up above the new max.
    public void SetMax(int newMax)
    {
        maxHealth = Mathf.Max(1, newMax);
        Current = Mathf.Min(Current, maxHealth);
    }

    // Back to full health after being defeated (Gentle Mode's nap).
    public void Revive()
    {
        Current = maxHealth;
        Revived?.Invoke(this);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        Healed?.Invoke(this);
    }

    private void Update()
    {
        if (flashTimer <= 0f) return;
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f) SetFlash(false);
    }

    // Brief white flash so hits are readable. Uses emission on the Standard shader.
    private void SetFlash(bool on)
    {
        foreach (var r in renderers)
        {
            if (on) r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", on ? Color.white : Color.black);
        }
    }
}
