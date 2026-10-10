using System;
using UnityEngine;

// The pictures the quest log shows: each NPC's portrait ("npc:Pearl") and a few small icons
// ("icon:frog", "icon:tree", "icon:monster"). Item pictures ("item:egg") come from the item
// database instead. DungeonBuilder fills this asset in (Assets/Items/QuestPictures.asset).
[CreateAssetMenu(menuName = "Dungeon/Quest Pictures", fileName = "QuestPictures")]
public class QuestPictures : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string key;
        public Sprite sprite;
    }

    [SerializeField] private Entry[] entries;

    public Sprite Find(string key)
    {
        if (entries == null) return null;
        foreach (var entry in entries)
            if (entry.key == key) return entry.sprite;
        return null;
    }
}
