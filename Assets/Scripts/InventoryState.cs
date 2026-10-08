using System.Collections.Generic;

// The inventory's actual contents, as plain data: item ids in the bag, and the item id in
// each equipment slot. It lives in GameSession (not on the player), so it survives scene
// changes, and plain ids are exactly what a save file needs to store.
public class InventoryState
{
    public readonly List<string> Bag = new List<string>();
    public readonly Dictionary<EquipSlot, string> Equipped = new Dictionary<EquipSlot, string>();
}
