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
    public event Action<Health> Blocked; // a hit that a shield took instead
    // A hit that did no damage at all because this thing is shielded right now (a boss's closed shell, his dim crystals).
    // Only raised when DeflectsNoDamageHits is on; the hit still counts as "something hit me" (Damaged) so monsters wake
    // up, but there's no white flash and no hurt sound: it has to look and sound different from a hit that landed.
    public event Action<Health> Deflected;
    public bool DeflectsNoDamageHits;
    public int LastDamage { get; private set; }

    // Optional: changes incoming damage (Gentle Mode halves the hero's). Returns the new amount.
    public Func<int, int> AdjustDamage;

    // Optional: a shield that can take a hit instead (the princess's Bubble Shield).
    // Returns true if it blocked this one.
    public Func<bool> TryBlock;
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
        if (TryBlock != null && TryBlock())
        {
            Blocked?.Invoke(this);
            return;
        }
        if (AdjustDamage != null) amount = AdjustDamage(amount);
        LastDamage = amount;
        if (DeflectsNoDamageHits && amount <= 0)
        {
            Deflected?.Invoke(this);
            Damaged?.Invoke(this);
            return;
        }

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

    // Brief white flash so hits are readable. A sprite (every hero and monster) is swapped for a flat white copy of itself
    // for a moment (the SpriteFlash shader); a lit 3D object glows white through its emission instead.
    private Material[] before;

    private void SetFlash(bool on)
    {
        var flashMaterial = SpriteMaterial.Flash;
        if (before == null) before = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            if (r is SpriteRenderer)
            {
                if (flashMaterial == null) continue;
                if (on) { if (before[i] == null) before[i] = r.sharedMaterial; r.sharedMaterial = flashMaterial; }
                else if (before[i] != null) { r.sharedMaterial = before[i]; before[i] = null; }
            }
            else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_EmissionColor"))
            {
                if (on) r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", on ? Color.white : Color.black);
            }
        }
    }
}
