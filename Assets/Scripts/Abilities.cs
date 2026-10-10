// The traversal abilities, the metroidvania "keys". Each one is a key item in the treasures tab, so
// they're saved, shown and carried with no extra state: having the item IS having the ability.
//
//   Bouncy Boots   hop over gaps (HopAbility)                         from the Slime King
//   Fairy Lantern  light dark hollows (LanternLight)                  from Mother Mushroom
//   Mole Mitts     push stone blocks, dig soft dirt (PushAbility)     from the Crystal Golem
//   Bubble Charm   swim across deep water (SwimAbility)               from King Crabbington
//   Rainbow Chalk  draw rainbow bridges between posts (RainbowPost)   from the Snow Yeti
//
// The two spells that every hero starts with (fire and water) open the spell gates (brambles and
// braziers) from the start, so those need no ability.
public static class Abilities
{
    public const string BouncyBoots = "bouncy_boots";
    public const string FairyLantern = "fairy_lantern";
    public const string MoleMitts = "mole_mitts";
    public const string BubbleCharm = "bubble_charm";
    public const string RainbowChalk = "rainbow_chalk";

    public static bool Has(string itemId) => GameSession.Inventory.Has(itemId);
}
