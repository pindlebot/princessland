// One node in the skill tree.
public class SkillDefinition
{
    public string Id, Name, Branch, Description;
    public int Tier;          // 1 (top) to 3 (bottom)
    public string Requires;   // the skill above it in its branch (null for tier 1)
    public bool Implemented;  // false = a placeholder for an ability that isn't built yet

    public SkillDefinition(string id, string name, string branch, int tier, string requires, bool implemented, string description)
    {
        Id = id; Name = name; Branch = branch; Tier = tier; Requires = requires;
        Implemented = implemented; Description = description;
    }
}

// The whole skill tree: three branches, three tiers each. Each skill needs the one above it.
// Only the tier-1 skills have real effects so far (see Progression's Bonus* properties);
// the rest are placeholders you can learn, ready for their abilities to be built.
public static class SkillCatalog
{
    public const string Empowered = "empowered";
    public const string Toughness = "toughness";
    public const string DeepReserves = "deep_reserves";

    public static readonly string[] Branches = { "Destruction", "Warding", "Arcana" };

    public static readonly SkillDefinition[] All =
    {
        new SkillDefinition(Empowered, "Empowered Spells", "Destruction", 1, null, true, "+1 damage to your spell."),
        new SkillDefinition("twin_cast", "Twin Cast", "Destruction", 2, Empowered, false, "Casts a second projectile at another nearby enemy."),
        new SkillDefinition("meteor", "Meteor", "Destruction", 3, "twin_cast", false, "Calls down a meteor that scorches everything in an area."),

        new SkillDefinition(Toughness, "Toughness", "Warding", 1, null, true, "+2 max health."),
        new SkillDefinition("mana_shield", "Mana Shield", "Warding", 2, Toughness, false, "Damage drains your mana before your health."),
        new SkillDefinition("second_wind", "Second Wind", "Warding", 3, "mana_shield", false, "Once per level, survive a killing blow with 1 health."),

        new SkillDefinition(DeepReserves, "Deep Reserves", "Arcana", 1, null, true, "+15 max mana."),
        new SkillDefinition("blink", "Blink", "Arcana", 2, DeepReserves, false, "Teleport a short distance in the direction you're walking."),
        new SkillDefinition("treasure_sense", "Treasure Sense", "Arcana", 3, "blink", false, "Enemies drop twice as much gold."),
    };
}
