using System;
using System.Collections.Generic;

// Everything a save file holds, as plain data that JsonUtility can write and read.
// (JsonUtility can't do dictionaries, so equipped items are a list of slot/item pairs.)
[Serializable]
public class SaveData
{
    public int version = 1;
    public string savedAt;        // ISO 8601, for "most recent" and the slot card

    public string hero;           // CharacterDefinition asset name, e.g. "Princess"
    public string scene;          // where to continue
    public string spawn;          // the named spawn point there ("" = the level start)

    public List<string> flags = new List<string>();

    public int level = 1, xp, gold, skillPoints;
    public List<string> skills = new List<string>();

    public List<string> bag = new List<string>();
    public List<EquippedItem> equipped = new List<EquippedItem>();

    public GameSettings settings = new GameSettings();

    [Serializable]
    public class EquippedItem
    {
        public EquipSlot slot;
        public string item;
    }
}
