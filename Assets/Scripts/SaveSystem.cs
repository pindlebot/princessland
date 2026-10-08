using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Three save slots (one per family member), each a small JSON file in the game's data folder.
// The game autosaves into the current slot on every door, so a session can end at any moment.
//
// Saving copies GameSession into a SaveData; loading copies it back and says which scene to
// open. Files are written to a temporary name first and then swapped in, so quitting mid-save
// can't leave a half-written file; a damaged file just shows up as an empty slot.
public static class SaveSystem
{
    public const int SlotCount = 3;

    // Tests point this at a temporary folder so they never touch real saves.
    public static string FolderOverride;
    public static string Folder => FolderOverride ?? DataFolder();

    private static string FileName(int slot) => $"save{slot + 1}.json";

    // The game used to be called IsoDungeon, and Unity names the data folder after the game,
    // so saves from before the rename are in .../DefaultCompany/IsoDungeon. The first time the
    // game looks for saves, it copies them across if there are none here yet. The old files stay.
    private const string OldFolderName = "IsoDungeon";
    private static bool checkedOldSaves;

    private static string DataFolder()
    {
        string folder = Application.persistentDataPath;
        if (checkedOldSaves) return folder;
        checkedOldSaves = true;
        try
        {
            string old = Path.Combine(Path.GetDirectoryName(folder), OldFolderName);
            bool haveSaves = Enumerable.Range(0, SlotCount).Any(s => File.Exists(Path.Combine(folder, FileName(s))));
            if (old != folder && Directory.Exists(old) && !haveSaves)
            {
                Directory.CreateDirectory(folder);
                for (int s = 0; s < SlotCount; s++)
                {
                    string from = Path.Combine(old, FileName(s));
                    if (File.Exists(from)) File.Copy(from, Path.Combine(folder, FileName(s)));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Couldn't bring over saves from {OldFolderName}: {e.Message}");
        }
        return folder;
    }

    private static string PathFor(int slot) => Path.Combine(Folder, FileName(slot));

    // Friendly names for the slot cards.
    private static readonly Dictionary<string, string> PlaceNames = new Dictionary<string, string>
    {
        ["Level0"] = "Castle Grounds",
        ["Dungeon"] = "The Dungeon",
        ["House"] = "Home",
        ["Cove"] = "Mermaid Cove",
    };

    public static string PlaceName(string scene) =>
        scene != null && PlaceNames.TryGetValue(scene, out var name) ? name : scene;

    public static bool Exists(int slot) => Peek(slot) != null;

    // Read a slot without loading it (for the title screen). Null if empty or unreadable.
    public static SaveData Peek(int slot)
    {
        try
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return data != null && !string.IsNullOrEmpty(data.scene) ? data : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Couldn't read slot {slot + 1}: {e.Message}");
            return null;
        }
    }

    // The slot played most recently, or -1 if there are no saves.
    public static int MostRecentSlot()
    {
        int best = -1;
        string bestTime = null;
        for (int i = 0; i < SlotCount; i++)
        {
            var data = Peek(i);
            if (data != null && (bestTime == null || string.CompareOrdinal(data.savedAt, bestTime) > 0))
            {
                best = i;
                bestTime = data.savedAt;
            }
        }
        return best;
    }

    // Save into the current slot, if there is one (tests and Play-in-editor have none).
    public static void Autosave(string scene, string spawn)
    {
        if (GameSession.Slot >= 0) Save(GameSession.Slot, scene, spawn);
    }

    public static void Save(int slot, string scene, string spawn)
    {
        var p = GameSession.Progress;
        var inv = GameSession.Inventory;
        var data = new SaveData
        {
            savedAt = DateTime.UtcNow.ToString("o"),
            hero = GameSession.SelectedCharacter != null ? GameSession.SelectedCharacter.name : "",
            scene = scene,
            spawn = spawn ?? "",
            flags = GameSession.Flags.ToList(),
            counters = GameSession.Counters.Select(c => new SaveData.Counter { key = c.Key, value = c.Value }).ToList(),
            level = p.Level,
            xp = p.Xp,
            gold = p.Gold,
            skillPoints = p.SkillPoints,
            skills = p.LearnedSkills.ToList(),
            bag = inv.Bag.ToList(),
            equipped = inv.Equipped.Select(e => new SaveData.EquippedItem { slot = e.Key, item = e.Value }).ToList(),
            settings = GameSession.Settings,
        };

        Directory.CreateDirectory(Folder);
        string path = PathFor(slot), temp = path + ".tmp";
        File.WriteAllText(temp, JsonUtility.ToJson(data, true));
        if (File.Exists(path)) File.Replace(temp, path, null);
        else File.Move(temp, path);
    }

    // Restore a slot into GameSession. Returns the scene to open (null if the slot is empty).
    // heroes: every playable CharacterDefinition, to turn the saved name back into an asset.
    public static string Load(int slot, IEnumerable<CharacterDefinition> heroes)
    {
        var data = Peek(slot);
        if (data == null) return null;

        var hero = heroes.FirstOrDefault(h => h != null && h.name == data.hero);
        GameSession.NewGame(hero);
        GameSession.Slot = slot;
        foreach (var flag in data.flags) GameSession.Flags.Add(flag);
        foreach (var c in data.counters ?? new List<SaveData.Counter>()) GameSession.Counters[c.key] = c.value;
        GameSession.Progress.Restore(data.level, data.xp, data.gold, data.skillPoints, data.skills);
        GameSession.Progress.RefundSkillsNotIn(SkillCatalog.PathFor(SkillCatalog.CurrentHero).Select(s => s.Id));
        GameSession.Inventory.Bag.AddRange(data.bag);
        foreach (var e in data.equipped) GameSession.Inventory.Equipped[e.slot] = e.item;
        GameSession.NextSpawn = string.IsNullOrEmpty(data.spawn) ? null : data.spawn;
        GameSession.Settings = data.settings ?? new GameSettings();
        return data.scene;
    }

    public static void Delete(int slot)
    {
        string path = PathFor(slot);
        if (File.Exists(path)) File.Delete(path);
    }
}
