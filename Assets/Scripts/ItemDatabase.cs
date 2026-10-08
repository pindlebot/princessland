using UnityEngine;

// Every item in the game, so an id (from the inventory or a save file) can be turned back
// into its ItemDefinition. DungeonBuilder keeps Assets/Items/ItemDatabase.asset up to date.
[CreateAssetMenu(menuName = "Dungeon/Item Database", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemDefinition[] items;

    public ItemDefinition Find(string id)
    {
        foreach (var item in items)
            if (item != null && item.Id == id) return item;
        Debug.LogWarning($"[ItemDatabase] No item with id '{id}'");
        return null;
    }
}
