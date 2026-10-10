// The traversal abilities, the metroidvania "keys". Each one is a key item in the treasures tab, so
// they're saved, shown and carried with no extra state: having the item IS having the ability.
//
//   Bouncy Boots   hop over gaps (HopAbility)                         from the Slime King
//   Fairy Lantern  light dark hollows (LanternLight)                  from Mother Mushroom
//
// The two spells that every hero starts with (fire and water) open the spell gates (brambles and
// braziers) from the start, so those need no ability.
public static class Abilities
{
    public const string BouncyBoots = "bouncy_boots";
    public const string FairyLantern = "fairy_lantern";

    public static bool Has(string itemId) => GameSession.Inventory.Has(itemId);
}
