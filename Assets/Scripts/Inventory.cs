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
    private readonly List<ItemDefinition> keyItems = new List<ItemDefinition>(); // the treasures tab's, resolved

    public event Action Changed;

    public int Capacity => capacity;
    public IReadOnlyList<ItemDefinition> Bag => bag;
    public IReadOnlyList<ItemDefinition> KeyItems => keyItems;
    public ItemDatabase Database => database;

    private void Awake()
    {
        state = GameSession.Inventory;
        Resolve();
    }

    public ItemDefinition Equipped(EquipSlot slot) =>
        state.Equipped.TryGetValue(slot, out var id) ? database.Find(id) : null;

    // Totals over everything equipped, read by the scripts each bonus affects.
    public int SpellDamageBonus => Total(item => item.SpellDamageBonus);       // SpellAbility
    public int MaxHealthBonus => Total(item => item.MaxHealthBonus);           // PlayerProgression
    public int MaxManaBonus => Total(item => item.MaxManaBonus);               // PlayerProgression
    public float MoveSpeedFactor => 1f + Total(item => item.MoveSpeedPercent) / 100f; // PlayerController
    // Multiplies the spell's cooldown: 20% faster recharge = 0.8. Never below a fifth.
    public float SpellCooldownFactor => Mathf.Max(0.2f, 1f - Total(item => item.SpellRechargePercent) / 100f);

    private int Total(Func<ItemDefinition, int> bonus)
    {
        int total = 0;
        foreach (var id in state.Equipped.Values)
        {
            var item = database.Find(id);
            if (item != null) total += bonus(item);
        }
        return total;
    }

    public bool Add(ItemDefinition item)
    {
        if (item.IsKeyItem)
        {
            // Treasures need no bag room, and you only ever carry one of each.
            if (!state.KeyItems.Contains(item.Id)) state.KeyItems.Add(item.Id);
            OnChanged();
            return true;
        }
        if (state.Bag.Count >= capacity) return false;
        state.Bag.Add(item.Id);
        if (item.IsConsumable) AutoAssignQuickSlot(item);
        OnChanged();
        return true;
    }

    // Gives an item by id (a conversation's reward). False if the id is unknown or the bag is full.
    public bool Add(string itemId)
    {
        var item = database.Find(itemId);
        return item != null && Add(item);
    }

    // Is this item anywhere on the hero (bag, treasures, worn)?
    public bool Has(string itemId) => state.Has(itemId);
    public bool HasKeyItem(string itemId) => state.KeyItems.Contains(itemId);

    // ---------- Quick slots (hotbar slots 2-5) ----------

    // The consumable a quick slot holds (even if you've run out of it: it shows empty).
    public ItemDefinition Quick(int slot) =>
        slot >= 0 && slot < InventoryState.QuickSlotCount && state.Quick[slot].Length > 0 ? database.Find(state.Quick[slot]) : null;

    // Puts a consumable on a quick slot. It leaves any other slot it was on, so it's never twice.
    public bool AssignQuick(int slot, ItemDefinition item)
    {
        if (slot < 0 || slot >= InventoryState.QuickSlotCount || item == null || !item.IsConsumable) return false;
        for (int i = 0; i < InventoryState.QuickSlotCount; i++)
            if (state.Quick[i] == item.Id) state.Quick[i] = "";
        state.Quick[slot] = item.Id;
        OnChanged();
        return true;
    }

    public void ClearQuick(int slot)
    {
        if (slot < 0 || slot >= InventoryState.QuickSlotCount || state.Quick[slot].Length == 0) return;
        state.Quick[slot] = "";
        OnChanged();
    }

    // Which quick slot holds this item (-1 = none).
    public int QuickSlotOf(ItemDefinition item) => item == null ? -1 : System.Array.IndexOf(state.Quick, item.Id);

    // Eats (uses) one of whatever the slot holds. False if the slot is empty or you've run out.
    public bool UseQuick(int slot)
    {
        var item = Quick(slot);
        return item != null && Eat(item);
    }

    // The first time you pick up a consumable, it takes the first free quick slot.
    private void AutoAssignQuickSlot(ItemDefinition item)
    {
        if (QuickSlotOf(item) >= 0) return;
        for (int i = 0; i < InventoryState.QuickSlotCount; i++)
            if (state.Quick[i].Length == 0)
            {
                state.Quick[i] = item.Id;
                return;
            }
    }

    // Takes one of an item out of the bag (used up, like a bottle of bubble bath).
    public bool Remove(ItemDefinition item)
    {
        if (!state.Bag.Remove(item.Id)) return false;
        OnChanged();
        return true;
    }

    // How many of an item are in the bag (ingredients for a recipe).
    public int Count(ItemDefinition item)
    {
        int n = 0;
        foreach (var id in state.Bag)
            if (id == item.Id) n++;
        return n;
    }

    // Eats one food from the bag: it gives back hearts and magic. Returns whether it did.
    public bool Eat(ItemDefinition item)
    {
        if (item == null || !item.IsFood || !Remove(item)) return false;
        if (item.HealthRestore > 0 && TryGetComponent(out Health health)) health.Heal(item.HealthRestore);
        if (item.ManaRestore > 0 && TryGetComponent(out Mana mana)) mana.Restore(item.ManaRestore);
        GameSession.AddToCounter("ate:" + item.Id);
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
        keyItems.Clear();
        foreach (var id in state.KeyItems)
        {
            var item = database.Find(id);
            if (item != null) keyItems.Add(item);
        }
    }
}
