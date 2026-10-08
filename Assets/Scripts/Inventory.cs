using System;
using System.Collections.Generic;
using UnityEngine;

// The player's bag (a fixed number of slots) plus whatever is equipped.
// Other scripts ask it for totals (e.g. SpellDamageBonus) instead of
// tracking items themselves, and the HUD redraws when Changed fires.
public class Inventory : MonoBehaviour
{
    [SerializeField] private int capacity = 8;

    private readonly List<ItemDefinition> bag = new List<ItemDefinition>();
    private readonly Dictionary<EquipSlot, ItemDefinition> equipped = new Dictionary<EquipSlot, ItemDefinition>();

    public event Action Changed;

    public int Capacity => capacity;
    public IReadOnlyList<ItemDefinition> Bag => bag;

    public ItemDefinition Equipped(EquipSlot slot) =>
        equipped.TryGetValue(slot, out var item) ? item : null;

    public int SpellDamageBonus
    {
        get
        {
            int total = 0;
            foreach (var item in equipped.Values) total += item.SpellDamageBonus;
            return total;
        }
    }

    public bool Add(ItemDefinition item)
    {
        if (bag.Count >= capacity) return false;
        bag.Add(item);
        Changed?.Invoke();
        return true;
    }

    // Moves an item from the bag into its slot; whatever was there goes back into the bag.
    public bool Equip(ItemDefinition item)
    {
        if (!item.IsEquippable || !bag.Remove(item)) return false;
        var previous = Equipped(item.Slot);
        if (previous != null) bag.Add(previous);
        equipped[item.Slot] = item;
        Changed?.Invoke();
        return true;
    }

    public bool Unequip(EquipSlot slot)
    {
        var item = Equipped(slot);
        if (item == null || bag.Count >= capacity) return false;
        equipped.Remove(slot);
        bag.Add(item);
        Changed?.Invoke();
        return true;
    }
}
