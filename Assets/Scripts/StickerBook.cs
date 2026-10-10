using System.Collections.Generic;
using System.Linq;

// One sticker: a little collectible picture for something you've found or done. It's earned when its condition holds (the
// same little language as quests and conversations: Condition.cs), so nothing about a sticker needs saving except that you've
// been told about it ("sticker:<id>").
public class Sticker
{
    public string Id;
    public string Name;
    public string Group;       // the region it belongs to: "Glimmer Mines", "Puddlebrook Lake", "Frostpeak"
    public string Condition;
}

// The sticker sets for the regions built in Phase 3 (the Mines, the Lake and Frostpeak): every monster, friend, treasure and secret
// of the region has one. The Sticker Book screen that shows them (Phase 4) isn't built yet; until then a toast announces each
// new sticker, and the count is kept ("stickers"), ready for the book.
public static class StickerBook
{
    public const string Mines = "Glimmer Mines";
    public const string Lake = "Puddlebrook Lake";
    public const string Frost = "Frostpeak";

    public static readonly Sticker[] All =
    {
        // The Glimmer Mines
        new Sticker { Id = "mines_visit", Name = "Glimmer Mines", Group = Mines, Condition = "visited:Mines1" },
        new Sticker { Id = "bat", Name = "Bat", Group = Mines, Condition = "defeated:Bat>=1" },
        new Sticker { Id = "pebblin", Name = "Pebblin", Group = Mines, Condition = "defeated:Pebblin>=1" },
        new Sticker { Id = "crystal_golem", Name = "Crystal Golem", Group = Mines, Condition = "defeated:CrystalGolem>=1" },
        new Sticker { Id = "digby", Name = "Digby", Group = Mines, Condition = "met:Digby" },
        new Sticker { Id = "lost_moles", Name = "Three Lost Moles", Group = Mines, Condition = "moles_found>=3" },
        new Sticker { Id = "mole_mitts", Name = "Mole Mitts", Group = Mines, Condition = "has:mole_mitts" },
        new Sticker { Id = "mine_cart", Name = "Mine Cart", Group = Mines, Condition = "cart_rides>=1" },
        new Sticker { Id = "pet_rock", Name = "Pet Rock", Group = Mines, Condition = "found:petrock" },
        new Sticker { Id = "topaz", Name = "The Topaz", Group = Mines, Condition = "has:topaz" },
        new Sticker { Id = "topaz_egg", Name = "Topaz Egg", Group = Mines, Condition = "has:dragon_egg_mines" },

        // Puddlebrook Lake
        new Sticker { Id = "lake_visit", Name = "Puddlebrook Lake", Group = Lake, Condition = "visited:Lake1" },
        new Sticker { Id = "crab", Name = "Crab", Group = Lake, Condition = "defeated:Crab>=1" },
        new Sticker { Id = "jelly", Name = "Jellyfish", Group = Lake, Condition = "defeated:Jelly>=1" },
        new Sticker { Id = "king_crabbington", Name = "King Crabbington", Group = Lake, Condition = "defeated:KingCrabbington>=1" },
        new Sticker { Id = "clamshell", Name = "Captain Clamshell", Group = Lake, Condition = "met:Clamshell" },
        new Sticker { Id = "first_fish", Name = "First Fish", Group = Lake, Condition = "fish_caught>=1" },
        new Sticker { Id = "golden_carp", Name = "Golden Carp", Group = Lake, Condition = "found:goldencarp" },
        new Sticker { Id = "swimmer", Name = "Swimmer", Group = Lake, Condition = "swims>=1" },
        new Sticker { Id = "bubble_charm", Name = "Bubble Charm", Group = Lake, Condition = "has:bubble_charm" },
        new Sticker { Id = "buoy", Name = "Buoy (Not a Toy)", Group = Lake, Condition = "found:buoy" },
        new Sticker { Id = "aquamarine", Name = "The Aquamarine", Group = Lake, Condition = "has:aquamarine" },
        new Sticker { Id = "aquamarine_egg", Name = "Aquamarine Egg", Group = Lake, Condition = "has:dragon_egg_lake" },

        // Frostpeak
        new Sticker { Id = "frost_visit", Name = "Frostpeak", Group = Frost, Condition = "visited:Frost1" },
        new Sticker { Id = "ice_slime", Name = "Ice Slime", Group = Frost, Condition = "defeated:IceSlime>=1" },
        new Sticker { Id = "snow_imp", Name = "Snow Imp", Group = Frost, Condition = "defeated:SnowImp>=1" },
        new Sticker { Id = "snow_yeti", Name = "Snow Yeti", Group = Frost, Condition = "defeated:SnowYeti>=1" },
        new Sticker { Id = "mr_frost", Name = "Mr. Frost", Group = Frost, Condition = "met:Mr. Frost" },
        new Sticker { Id = "granny_purl", Name = "Granny Purl", Group = Frost, Condition = "met:Granny Purl" },
        new Sticker { Id = "warm_scarf", Name = "A Scarf for Mr. Frost", Group = Frost, Condition = "thanked:frost" },
        new Sticker { Id = "rainbow_chalk", Name = "Rainbow Chalk", Group = Frost, Condition = "has:rainbow_chalk" },
        new Sticker { Id = "rainbow_bridge", Name = "Rainbow Bridge", Group = Frost, Condition = "bridges_drawn>=1" },
        new Sticker { Id = "snowman", Name = "Plain Snowman", Group = Frost, Condition = "found:snowman" },
        new Sticker { Id = "sapphire", Name = "The Sapphire", Group = Frost, Condition = "has:sapphire" },
        new Sticker { Id = "sapphire_egg", Name = "Sapphire Egg", Group = Frost, Condition = "has:dragon_egg_frost" },
    };

    public const string CountCounter = "stickers";

    public static string FlagFor(Sticker sticker) => "sticker:" + sticker.Id;
    public static bool IsEarned(Sticker sticker) => GameSession.Flags.Contains(FlagFor(sticker));
    public static Sticker Find(string id) => All.FirstOrDefault(s => s.Id == id);
    public static int Earned(string group = null) => All.Count(s => IsEarned(s) && (group == null || s.Group == group));
    public static int Total(string group = null) => All.Count(s => group == null || s.Group == group);

    // Hands out every sticker whose condition now holds and that you haven't been given yet; returns the new ones.
    public static List<Sticker> Award()
    {
        var fresh = new List<Sticker>();
        foreach (var sticker in All)
        {
            if (IsEarned(sticker) || !Condition.Met(sticker.Condition)) continue;
            GameSession.Flags.Add(FlagFor(sticker));
            GameSession.AddToCounter(CountCounter);
            fresh.Add(sticker);
        }
        return fresh;
    }
}
