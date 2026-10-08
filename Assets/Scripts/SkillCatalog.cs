using System.Linq;

// One step on a hero's skill path.
public class SkillDefinition
{
    public string Id, Name, Hero, Description;
    public int Step;          // 1 (top) to 4 (bottom)
    public string Requires;   // the step above it (null for step 1)
    public bool IsAbility;    // a new ability with its own button, rather than an enhancement

    public SkillDefinition(string id, string name, string hero, int step, string requires, bool isAbility, string description)
    {
        Id = id; Name = name; Hero = hero; Step = step; Requires = requires;
        IsAbility = isAbility; Description = description;
    }
}

// Each hero has one straight path of four skills: two enhancements, then two new abilities.
// Every skill needs the one above it, so they're learned in order, one per level-up.
// The enhancements are bonuses Progression adds up (its Bonus* properties); the abilities are
// HeroAbility components on the player prefab that switch on once their skill is learned.
//
// Heroes are named by their CharacterDefinition asset ("Wizard", "Princess"), the same name
// the save files use.
public static class SkillCatalog
{
    public const string Wizard = "Wizard", Princess = "Princess";

    // Aldric the Wizard: Fire
    public const string Empowered = "empowered";
    public const string DeepReserves = "deep_reserves";
    public const string FlameWave = "flame_wave";
    public const string Meteor = "meteor";

    // Princess Marina: Water
    public const string Toughness = "toughness";
    public const string SwiftTides = "swift_tides";
    public const string BubbleShield = "bubble_shield";
    public const string Whirlpool = "whirlpool";

    public static readonly SkillDefinition[] All =
    {
        new SkillDefinition(Empowered, "Empowered Spells", Wizard, 1, null, false, "+1 damage to your spells."),
        new SkillDefinition(DeepReserves, "Deep Reserves", Wizard, 2, Empowered, false, "+15 max mana."),
        new SkillDefinition(FlameWave, "Flame Wave", Wizard, 3, DeepReserves, true,
            "New ability: a fan of fire in front of you that burns every monster it reaches and pushes them back."),
        new SkillDefinition(Meteor, "Meteor", Wizard, 4, FlameWave, true,
            "New ability: call down a meteor on a monster. It smashes everything in a wide circle."),

        new SkillDefinition(Toughness, "Toughness", Princess, 1, null, false, "+2 max health."),
        new SkillDefinition(SwiftTides, "Swift Tides", Princess, 2, Toughness, false, "Tidal Orb recharges 30% faster."),
        new SkillDefinition(BubbleShield, "Bubble Shield", Princess, 3, SwiftTides, true,
            "New ability: a bubble around you that pops instead of you getting hurt (blocks 2 hits)."),
        new SkillDefinition(Whirlpool, "Whirlpool", Princess, 4, BubbleShield, true,
            "New ability: a swirling pool that pulls monsters in and keeps splashing them."),
    };

    // A hero's path, top to bottom. An unknown hero (e.g. pressing Play in a level scene with
    // no hero picked) gets the wizard's, like LevelBootstrap's default.
    public static SkillDefinition[] PathFor(string hero)
    {
        var path = All.Where(s => s.Hero == hero).OrderBy(s => s.Step).ToArray();
        return path.Length > 0 ? path : PathFor(Wizard);
    }

    public static SkillDefinition Find(string id) => System.Array.Find(All, s => s.Id == id);

    // The path of whoever is being played right now.
    public static string CurrentHero =>
        GameSession.SelectedCharacter != null ? GameSession.SelectedCharacter.name : Wizard;
}
