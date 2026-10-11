using System.Collections.Generic;
using System.Linq;

// Things that need to survive loading a new scene. Every object in a scene is destroyed
// when the next scene loads, but static fields live on, so this is the simplest way to
// carry state between scenes: which hero was picked, where to arrive, what's been done.
public static class GameSession
{
    // Null until the player picks someone; levels then fall back to their default (the wizard),
    // so pressing Play directly in a level scene still works.
    public static CharacterDefinition SelectedCharacter;

    // Which save slot (0-2) this game autosaves into; -1 = not saving (tests, Play in a level scene).
    public static int Slot = -1;

    // Which named spawn point to arrive at in the next scene (e.g. "FromHouse" puts you
    // outside the castle gate instead of at the level start). Used once, then cleared.
    public static string NextSpawn;

    // The spawn point the hero arrived at in the current scene ("" = the level start), so
    // "save and quit" can put her back by the same door.
    public static string EnteredBy = "";

    // Volume and Gentle/Adventurer mode, saved with each slot.
    public static GameSettings Settings = new GameSettings();

    // Simple facts about this playthrough, e.g. "cleared:Level0" or "met:Amethyra",
    // so leaving and re-entering a scene doesn't undo them.
    public static readonly HashSet<string> Flags = new HashSet<string>();

    // Things worth counting, e.g. "wishes" made at the fountain or "talks:Coralie".
    public static readonly Dictionary<string, int> Counters = new Dictionary<string, int>();
    public static int GetCounter(string key) => Counters.TryGetValue(key, out int n) ? n : 0;
    public static int AddToCounter(string key, int amount = 1)
    {
        int value = Counters[key] = GetCounter(key) + amount;
        if (SavedCounters.Any(prefix => key.StartsWith(prefix))) SaveSystem.AutosaveSoon();
        return value;
    }

    // Counters whose changes are progress worth saving at once: fish caught, moles found, trees woken, things bought, dirt dug.
    private static readonly string[] SavedCounters = { "fish_caught", "moles_found", "trees_woken", "bought:", "dirt_dug", "heart", "star" };

    // Level, experience, gold and skills.
    public static Progression Progress = new Progression();

    // What's in the bag and what's equipped (as item ids), so it survives going through doors.
    public static InventoryState Inventory = new InventoryState();

    // Things in the world that have been used up for good: an opened chest, a picked-up item.
    // Keyed by each object's persistent id (scene/col,row), so they stay used when you come back.
    public static bool IsUsed(string persistentId) => Flags.Contains("used:" + persistentId);
    // (Using something up is worth keeping: a chest, a pickup, a treasure, a patch of dirt dug.)
    public static void MarkUsed(string persistentId)
    {
        if (Flags.Add("used:" + persistentId)) SaveSystem.AutosaveSoon();
    }

    // Called when a hero is picked (or a save is loaded): forget the previous playthrough.
    // The save slot is kept: the title screen picks it before the hero is chosen.
    public static void NewGame(CharacterDefinition hero)
    {
        SelectedCharacter = hero;
        NextSpawn = null;
        EnteredBy = "";
        SaveRunner.Cancel(); // nothing from the old game is waiting to be written into the new one
        Settings = new GameSettings();
        Flags.Clear();
        Counters.Clear();
        Progress = new Progression();
        Inventory = new InventoryState();
    }
}
