using UnityEngine;

// Applies GameSession.Progress (level and skills) and the equipment worn to the spawned player:
// sets max health and mana, and celebrates level-ups with a full heal, a fanfare and sparkles.
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
    private Inventory inventory; // optional: armor and helms add hearts and magic
    private int baseHealth;
    private float baseMana;
    private Progression progress;

    private void Awake()
    {
        health = GetComponent<Health>();
        mana = GetComponent<Mana>();
        inventory = GetComponent<Inventory>();
        baseHealth = health.Max; // the hero's level-1 values, from the prefab
        baseMana = mana.Max;
    }

    private void Start()
    {
        progress = GameSession.Progress;
        progress.LeveledUp += OnLevelUp;
        progress.Changed += ApplyStats;
        if (inventory != null) inventory.Changed += ApplyStats;
        ApplyStats();
        health.Heal(health.Max); // arrive in each scene at full strength
        mana.Refill();
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= ApplyStats;
        if (progress == null) return;
        progress.LeveledUp -= OnLevelUp;
        progress.Changed -= ApplyStats;
    }

    private void ApplyStats()
    {
        // Taking armor off can't leave you with more hearts than you can hold (SetMax clamps),
        // and putting it on gives empty hearts rather than a free heal.
        int gear = inventory != null ? inventory.MaxHealthBonus : 0;
        float gearMana = inventory != null ? inventory.MaxManaBonus : 0;
        health.SetMax(baseHealth + progress.BonusHealth + gear);
        mana.SetMax(baseMana + progress.BonusMana + gearMana);
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
