using System;
using System.Collections.Generic;
using UnityEngine;

// The hero's long-term progress: level, experience, gold, skill points and learned skills.
// A plain C# class (not a MonoBehaviour), kept in GameSession.Progress so it survives
// scene loads. Components like PlayerProgression and the HUD listen to its events.
public class Progression
{
    public const int MaxLevel = 20;

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }          // progress toward the next level
    public int Gold { get; private set; }
    public int SkillPoints { get; private set; }

    private readonly HashSet<string> learned = new HashSet<string>();

    public event Action<int> LeveledUp;            // the new level
    public event Action<SkillDefinition> SkillLearned;
    public event Action Changed;                   // anything changed: redraw the HUD

    // The usual "each level takes more than the last" curve, 40 * level^1.5, except that the
    // very first level is a big step: level 1 -> 2 needs 400 XP (10x the curve's 40), then
    // 113, 208, 320, 447, ... So a hero earns level 2 over a whole adventure, not a few fights.
    public const int FirstLevelXp = 400;
    public static int XpToNext(int level) => level <= 1 ? FirstLevelXp : Mathf.RoundToInt(40f * Mathf.Pow(level, 1.5f));
    public int XpForNextLevel => XpToNext(Level);

    // ---------- What levels and skills are worth ----------
    // Per level: +1 max health and +5 max mana. The enhancement skills (the first two steps
    // of each hero's path) add their bonuses on top.
    public int BonusHealth => (Level - 1) + (Has(SkillCatalog.Toughness) ? 2 : 0);
    public float BonusMana => (Level - 1) * 5f + (Has(SkillCatalog.DeepReserves) ? 15f : 0f);
    public int BonusSpellDamage => Has(SkillCatalog.Empowered) ? 1 : 0;
    public float SpellCooldownFactor => Has(SkillCatalog.SwiftTides) ? 0.7f : 1f;

    public void AddXp(int amount)
    {
        if (Level >= MaxLevel) return;
        Xp += amount;
        // One big reward can be worth several levels, hence a loop.
        while (Level < MaxLevel && Xp >= XpToNext(Level))
        {
            Xp -= XpToNext(Level);
            Level++;
            SkillPoints++;
            LeveledUp?.Invoke(Level);
        }
        Changed?.Invoke();
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        Changed?.Invoke();
    }

    // Pay for something (a wish at the fountain, later the shop). False if there isn't enough.
    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        Changed?.Invoke();
        return true;
    }

    // ---------- Skills ----------

    public bool Has(string skillId) => learned.Contains(skillId);
    public IEnumerable<string> LearnedSkills => learned;

    // Put back a saved state (SaveSystem.Load). No events: nothing is listening yet.
    public void Restore(int level, int xp, int gold, int skillPoints, IEnumerable<string> skills)
    {
        Level = Mathf.Clamp(level, 1, MaxLevel);
        Xp = Mathf.Max(0, xp);
        Gold = Mathf.Max(0, gold);
        SkillPoints = Mathf.Max(0, skillPoints);
        learned.Clear();
        foreach (var id in skills) learned.Add(id);
    }

    // Forget any learned skill that isn't in `keep` and give its point back. Loading a save
    // uses this with the hero's own path, so skills from an older version of the tree (or
    // another hero's) turn back into points to spend.
    public void RefundSkillsNotIn(IEnumerable<string> keep)
    {
        var allowed = new HashSet<string>(keep);
        SkillPoints += learned.RemoveWhere(id => !allowed.Contains(id));
    }

    public bool MeetsRequirement(SkillDefinition skill) => skill.Requires == null || Has(skill.Requires);

    public bool CanLearn(SkillDefinition skill) => SkillPoints > 0 && !Has(skill.Id) && MeetsRequirement(skill);

    public bool Learn(SkillDefinition skill)
    {
        if (!CanLearn(skill)) return false;
        SkillPoints--;
        learned.Add(skill.Id);
        SkillLearned?.Invoke(skill);
        Changed?.Invoke();
        return true;
    }
}
