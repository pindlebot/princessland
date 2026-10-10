using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Where an item is worn. Saves store these as numbers, so add new slots at the end: never
// reorder or remove one, or old saves would put their items in the wrong place.
public enum EquipSlot { None, Ring, Helm, Armor, Weapon, Boots, Hat, Charm }

// What an item *is*: name, art and stats, saved as an asset (Assets/Items/*.asset).
// A ScriptableObject is a data container that lives in the project rather than in a
// scene, so many pickups (or a shop, or loot tables) can all point at the same item.
// Create more from the Project window: Create > Dungeon > Item.
[CreateAssetMenu(menuName = "Dungeon/Item", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    [Tooltip("A stable, unique id (e.g. \"ember_ring\"). Saves and the inventory store this, not the asset.")]
    [SerializeField] private string id = "new_item";
    [SerializeField] private string displayName = "New Item";
    [Tooltip("Flavour text. The bonuses are listed after it automatically.")]
    [TextArea] [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private EquipSlot slot = EquipSlot.None;
    [Tooltip("A treasure: it goes in the treasures tab (no bag room needed) and can't be lost or dropped.")]
    [SerializeField] private bool keyItem;

    [Header("Bonuses while equipped")]
    // FormerlySerializedAs: this field used to be called fireballDamageBonus. The attribute
    // tells Unity to load old saved values into the new name instead of losing them.
    [FormerlySerializedAs("fireballDamageBonus")]
    [SerializeField] private int spellDamageBonus;
    [Tooltip("Extra hearts.")]
    [SerializeField] private int maxHealthBonus;
    [Tooltip("Extra magic (mana).")]
    [SerializeField] private int maxManaBonus;
    [Tooltip("Walk this much faster, in percent.")]
    [SerializeField] private int moveSpeedPercent;
    [Tooltip("The spell recharges this much faster, in percent.")]
    [SerializeField] private int spellRechargePercent;

    [Header("Food (eaten from the bag)")]
    [Tooltip("Hearts it gives back when eaten. Food is anything that restores hearts or magic.")]
    [SerializeField] private int healthRestore;
    [Tooltip("Magic it gives back when eaten.")]
    [SerializeField] private int manaRestore;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public EquipSlot Slot => slot;
    public bool IsEquippable => slot != EquipSlot.None;
    public bool IsKeyItem => keyItem;
    public int SpellDamageBonus => spellDamageBonus;
    public int MaxHealthBonus => maxHealthBonus;
    public int MaxManaBonus => maxManaBonus;
    public int MoveSpeedPercent => moveSpeedPercent;
    public int SpellRechargePercent => spellRechargePercent;
    public int HealthRestore => healthRestore;
    public int ManaRestore => manaRestore;
    public bool IsFood => healthRestore > 0 || manaRestore > 0;
    public bool IsConsumable => IsFood; // can sit on a quick slot (hotbar 2-5)

    // "+3 hearts, +50 magic", like the numbers under a recipe card in Stardew Valley.
    public string FoodText
    {
        get
        {
            var parts = new List<string>();
            if (healthRestore > 0) parts.Add($"+{healthRestore} {(healthRestore == 1 ? "heart" : "hearts")}");
            if (manaRestore > 0) parts.Add($"+{manaRestore} magic");
            return string.Join(", ", parts);
        }
    }

    // One line per thing it does, for the tooltip: "+1 spell damage", "+2 hearts", or what eating it gives back.
    public string[] BonusLines
    {
        get
        {
            string text = BonusText.Length > 0 ? BonusText : IsFood ? FoodText : "";
            return text.Length == 0 ? new string[0] : text.Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }

    // "Helm", "Food", "Treasure": what sort of thing it is, for the tooltip.
    public string KindText =>
        IsEquippable ? Slot.ToString() : IsFood ? "Food" : IsKeyItem ? "Treasure" : "Item";

    // "+1 spell damage, +2 hearts", for the inventory's details line.
    public string BonusText
    {
        get
        {
            var parts = new List<string>();
            if (spellDamageBonus != 0) parts.Add($"{spellDamageBonus:+0;-0} spell damage");
            if (maxHealthBonus != 0) parts.Add($"{maxHealthBonus:+0;-0} {(Mathf.Abs(maxHealthBonus) == 1 ? "heart" : "hearts")}");
            if (maxManaBonus != 0) parts.Add($"{maxManaBonus:+0;-0} magic");
            if (moveSpeedPercent != 0) parts.Add($"walk {Mathf.Abs(moveSpeedPercent)}% {(moveSpeedPercent > 0 ? "faster" : "slower")}");
            if (spellRechargePercent != 0) parts.Add($"spells recharge {Mathf.Abs(spellRechargePercent)}% {(spellRechargePercent > 0 ? "faster" : "slower")}");
            return string.Join(", ", parts);
        }
    }
}
