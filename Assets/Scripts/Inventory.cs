using System;
using System.Collections.Generic;
using UnityEngine;

// The player's bag (a fixed number of slots) plus whatever is equipped.
//
// The contents live in GameSession.Inventory as item ids, so they survive going through
// doors; this component is a view over that data, turning ids back into ItemDefinitions
// through the ItemDatabase. Other scripts ask it for totals (e.g. SpellDamageBonus)
// instead of tracking items themselves, and the HUD redraws when Changed fires.
public class Inventory : MonoBehaviour
{
    [SerializeField] private int capacity = 8;
    [SerializeField] private ItemDatabase database;

    private InventoryState state;
    private readonly List<ItemDefinition> bag = new List<ItemDefinition>(); // the bag's ids, resolved

    public event Action Changed;

    public int Capacity => capacity;
    public IReadOnlyList<ItemDefinition> Bag => bag;

    private void Awake()
    {
        state = GameSession.Inventory;
        Resolve();
    }

    public ItemDefinition Equipped(EquipSlot slot) =>
        state.Equipped.TryGetValue(slot, out var id) ? database.Find(id) : null;

    public int SpellDamageBonus
    {
        get
        {
            int total = 0;
            foreach (var id in state.Equipped.Values)
            {
                var item = database.Find(id);
                if (item != null) total += item.SpellDamageBonus;
            }
            return total;
        }
    }

    public bool Add(ItemDefinition item)
    {
        if (state.Bag.Count >= capacity) return false;
        state.Bag.Add(item.Id);
        OnChanged();
        return true;
    }

    // Moves an item from the bag into its slot; whatever was there goes back into the bag.
    public bool Equip(ItemDefinition item)
    {
        if (!item.IsEquippable || !state.Bag.Remove(item.Id)) return false;
        if (state.Equipped.TryGetValue(item.Slot, out var previous)) state.Bag.Add(previous);
        state.Equipped[item.Slot] = item.Id;
        OnChanged();
        return true;
    }

    public bool Unequip(EquipSlot slot)
    {
        if (!state.Equipped.TryGetValue(slot, out var id) || state.Bag.Count >= capacity) return false;
        state.Equipped.Remove(slot);
        state.Bag.Add(id);
        OnChanged();
        return true;
    }

    private void OnChanged()
    {
        Resolve();
        Changed?.Invoke();
    }

    private void Resolve()
    {
        bag.Clear();
        foreach (var id in state.Bag)
        {
            var item = database.Find(id);
            if (item != null) bag.Add(item);
        }
    }
}
