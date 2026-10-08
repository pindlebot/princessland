using UnityEngine;

// Applies GameSession.Progress (level and skills) to the spawned player: sets max health
// and mana, and celebrates level-ups with a full heal, a fanfare and sparkles.
//
// Progression is static data that outlives this scene, so we must unsubscribe from its
// events in OnDestroy, or it would keep calling into this destroyed player forever.
[RequireComponent(typeof(Health), typeof(Mana))]
public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private GameObject levelUpEffect;

    private Health health;
    private Mana mana;
    private int baseHealth;
    private float baseMana;
    private Progression progress;

    private void Awake()
    {
        health = GetComponent<Health>();
        mana = GetComponent<Mana>();
        baseHealth = health.Max; // the hero's level-1 values, from the prefab
        baseMana = mana.Max;
    }

    private void Start()
    {
        progress = GameSession.Progress;
        progress.LeveledUp += OnLevelUp;
        progress.Changed += ApplyStats;
        ApplyStats();
        health.Heal(health.Max); // arrive in each scene at full strength
        mana.Refill();
    }

    private void OnDestroy()
    {
        if (progress == null) return;
        progress.LeveledUp -= OnLevelUp;
        progress.Changed -= ApplyStats;
    }

    private void ApplyStats()
    {
        health.SetMax(baseHealth + progress.BonusHealth);
        mana.SetMax(baseMana + progress.BonusMana);
    }

    private void OnLevelUp(int level)
    {
        ApplyStats();
        health.Heal(health.Max);
        mana.Refill();
        AudioManager.Play(levelUpSound);
        if (levelUpEffect != null)
            Instantiate(levelUpEffect, transform.position + Vector3.down * 0.9f, Quaternion.identity);
    }
}
