using System.Collections.Generic;

// The inventory's actual contents, as plain data: item ids in the bag, the key items in the
// treasures tab, the item id in each equipment slot, and which consumable each quick slot (hotbar
// slots 2-5) holds. It lives in GameSession (not on the player), so it survives scene changes,
// and plain ids are exactly what a save file needs to store.
public class InventoryState
{
    public const int QuickSlotCount = 4;

    public readonly List<string> Bag = new List<string>();
    public readonly List<string> KeyItems = new List<string>(); // abilities, lanterns, gems: kept, never dropped, no bag room needed
    public readonly Dictionary<EquipSlot, string> Equipped = new Dictionary<EquipSlot, string>();

    // One consumable id per quick slot ("" = empty). The slot remembers its item even when the
    // last one has been eaten, so a new one lands back in the same place.
    public readonly string[] Quick = { "", "", "", "" };

    // Is this item anywhere on the hero: in the bag, the treasures tab, or being worn?
    public bool Has(string id) => Bag.Contains(id) || KeyItems.Contains(id) || Equipped.ContainsValue(id);
}
