using UnityEngine;
using UnityEngine.Serialization;

public enum EquipSlot { None, Ring }

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
    [TextArea] [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private EquipSlot slot = EquipSlot.None;

    [Header("Bonuses while equipped")]
    // FormerlySerializedAs: this field used to be called fireballDamageBonus. The attribute
    // tells Unity to load old saved values into the new name instead of losing them.
    [FormerlySerializedAs("fireballDamageBonus")]
    [SerializeField] private int spellDamageBonus;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public EquipSlot Slot => slot;
    public bool IsEquippable => slot != EquipSlot.None;
    public int SpellDamageBonus => spellDamageBonus;
}
