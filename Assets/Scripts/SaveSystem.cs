using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Three save slots (one per family member), each a small JSON file in the game's data folder.
// The game autosaves into the current slot on every door, so a session can end at any moment.
//
// Saving copies GameSession into a SaveData; loading copies it back and says which scene to
// open. Files are written to a temporary name first, checked, and then swapped in (the previous good file is kept
// as save<N>.bak), so quitting or a full disk mid-save can't cost the last good save.
//
// A slot is in one of four states (StateOf): empty, fine, damaged (there's a file but it can't be read, and no
// good backup) or from a newer game (its version is higher than this game knows). A damaged or newer slot is
// never treated as empty and never overwritten silently: it is set aside as save<N>.damaged.json / left alone.
//
// Versions: SaveData.version 1 was the original layout; 2 adds explicit version handling and defensive loading.
// Missing fields get sensible defaults (JsonUtility keeps the field initialisers), unknown items, skills and flags
// are kept as they are (they might be from a future game) and ignored by the game, an unknown hero falls back to the
// default one and an unknown scene falls back to the castle grounds.
public static class SaveSystem
{
    public const int SlotCount = 3;
    public const int CurrentVersion = 2;
    public const string FallbackScene = "Level0";

    public enum SlotState { Empty, Fine, Damaged, FromNewerGame }

    // Why the last Save failed (null if it didn't). The HUD says "Couldn't save" when a save fails.
    public static string LastError { get; private set; }
    public static int Failures { get; private set; }
    public static event Action<string> SaveFailed;

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
        ["Kitchen"] = "The Kitchen",
        ["Farm"] = "Hollow Farm",
        ["Cove"] = "Mermaid Cove",
        ["Woods1"] = "Whispering Woods",
        ["Woods2"] = "Spore Meadow",
        ["Woods3"] = "Mushroom Hollow",
        ["Woods4"] = "Mother Mushroom's Grove",
        ["Mines1"] = "Glimmer Mines",
        ["Mines2"] = "Mole Tunnels",
        ["Mines3"] = "Crystal Cavern",
        ["Mines4"] = "The Golem's Chamber",
        ["Lake1"] = "Puddlebrook Shore",
        ["Lake2"] = "The Murky Reeds",
        ["Lake3"] = "The Sunken Dock",
        ["Lake4"] = "King Crabbington's Court",
        ["Frost1"] = "Frostpeak Camp",
        ["Frost2"] = "The Icy Slopes",
        ["Frost3"] = "The Frozen Pass",
        ["Frost4"] = "The Yeti's Den",
    };

    public static string PlaceName(string scene) =>
        scene != null && PlaceNames.TryGetValue(scene, out var name) ? name : scene;

    public static bool Exists(int slot) => Peek(slot) != null;

    // Read a slot without loading it (for the title screen). Null if it is empty, damaged or from a newer game
    // (see StateOf). If the main file is damaged but the last good copy (.bak) is fine, that copy is what's read.
    public static SaveData Peek(int slot) => Read(slot, out _);

    public static SlotState StateOf(int slot)
    {
        Read(slot, out var state);
        return state;
    }

    private static SaveData Read(int slot, out SlotState state)
    {
        string path = PathFor(slot);
        bool anyFile = File.Exists(path) || File.Exists(path + ".bak");
        state = anyFile ? SlotState.Damaged : SlotState.Empty;
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            var data = TryParse(candidate, slot);
            if (data == null) continue;
            if (data.version > CurrentVersion)
            {
                state = SlotState.FromNewerGame;
                return null;
            }
            state = SlotState.Fine;
            return Migrate(data);
        }
        return null;
    }

    private static SaveData TryParse(string path, int slot = -1)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return data != null && !string.IsNullOrEmpty(data.scene) ? data : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Couldn't read slot {slot + 1} ({Path.GetFileName(path)}): {e.Message}");
            return null;
        }
    }

    // Bring an older save up to the current layout, never losing anything. (Version 1 -> 2 only adds the version.)
    private static SaveData Migrate(SaveData data)
    {
        data.flags ??= new List<string>();
        data.counters ??= new List<SaveData.Counter>();
        data.skills ??= new List<string>();
        data.bag ??= new List<string>();
        data.keyItems ??= new List<string>();
        data.quick ??= new List<string>();
        data.equipped ??= new List<SaveData.EquippedItem>();
        data.settings ??= new GameSettings();
        data.level = Mathf.Max(1, data.level);
        data.xp = Mathf.Max(0, data.xp);
        data.gold = Mathf.Max(0, data.gold);
        data.skillPoints = Mathf.Max(0, data.skillPoints);
        data.version = CurrentVersion;
        return data;
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
    public static bool Autosave(string scene, string spawn) => GameSession.Slot < 0 || Save(GameSession.Slot, scene, spawn);

    // Save into the current slot, as it was left when the hero last came through a door (or a fountain), so loading
    // arrives somewhere sensible. For the moments in the middle of a room: a boss reward, a skill, a catch, a gift,
    // closing the game. Coalesced (see SaveRunner), so a burst of pots breaking is one write.
    public static void AutosaveSoon() => SaveRunner.Request();

    public static void AutosaveHere()
    {
        if (GameSession.Slot < 0) return;
        if (LevelBootstrap.Current == null || LevelBootstrap.Current.Player == null) return;   // only from inside a level
        Autosave(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, GameSession.EnteredBy);
    }

    // Returns false (and says why in LastError, and raises SaveFailed) if it couldn't: a full disk, no permission, a slot
    // from a newer game. The slot keeps its last good save either way.
    public static bool Save(int slot, string scene, string spawn)
    {
        LastError = null;
        var p = GameSession.Progress;
        var inv = GameSession.Inventory;
        var data = new SaveData
        {
            version = CurrentVersion,
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
            keyItems = inv.KeyItems.ToList(),
            quick = inv.Quick.ToList(),
            equipped = inv.Equipped.Select(e => new SaveData.EquippedItem { slot = e.Key, item = e.Value }).ToList(),
            settings = GameSession.Settings,
        };

        try
        {
            var state = StateOf(slot);
            if (state == SlotState.FromNewerGame)
                throw new IOException("This adventure was saved by a newer version of the game, so it is being left alone.");

            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Folder);
            string path = PathFor(slot), temp = path + ".tmp", backup = path + ".bak";
            File.WriteAllText(temp, json);
            if (JsonUtility.FromJson<SaveData>(File.ReadAllText(temp))?.scene != scene)   // what was written must read back
                throw new IOException("The save file didn't read back correctly.");

            if (File.Exists(path) && TryParse(path) != null) File.Replace(temp, path, backup);   // the old good file becomes the backup
            else
            {
                if (File.Exists(path)) File.Copy(path, DamagedPath(slot), true);               // never overwrite a damaged file unseen
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            }
            return true;
        }
        catch (Exception e)
        {
            LastError = e.Message;
            Failures++;
            Debug.LogWarning($"[SaveSystem] Couldn't save slot {slot + 1}: {e.Message}");
            try { File.Delete(PathFor(slot) + ".tmp"); } catch (Exception) { /* nothing more to do */ }
            SaveFailed?.Invoke(e.Message);
            return false;
        }
    }

    private static string DamagedPath(int slot) => Path.Combine(Folder, $"save{slot + 1}.damaged.json");

    // Restore a slot into GameSession. Returns the scene to open (null if the slot is empty).
    // heroes: every playable CharacterDefinition, to turn the saved name back into an asset.
    public static string Load(int slot, IEnumerable<CharacterDefinition> heroes)
    {
        var data = Peek(slot);
        if (data == null) return null;

        // An unknown hero (removed from the game) falls back to the default one: NewGame(null) picks the wizard.
        var hero = heroes.FirstOrDefault(h => h != null && h.name == data.hero);
        GameSession.NewGame(hero);
        GameSession.Slot = slot;
        foreach (var flag in data.flags) if (flag != null) GameSession.Flags.Add(flag);
        foreach (var c in data.counters) if (!string.IsNullOrEmpty(c.key)) GameSession.Counters[c.key] = c.value;
        GameSession.Progress.Restore(data.level, data.xp, data.gold, data.skillPoints, data.skills);
        GameSession.Progress.RefundSkillsNotIn(SkillCatalog.PathFor(SkillCatalog.CurrentHero).Select(s => s.Id));
        GameSession.Inventory.Bag.AddRange(data.bag);
        GameSession.Inventory.KeyItems.AddRange(data.keyItems ?? new List<string>());
        for (int i = 0; i < InventoryState.QuickSlotCount && data.quick != null && i < data.quick.Count; i++)
            GameSession.Inventory.Quick[i] = data.quick[i] ?? "";
        foreach (var e in data.equipped) if (!string.IsNullOrEmpty(e.item)) GameSession.Inventory.Equipped[e.slot] = e.item;
        GameSession.Settings = data.settings ?? new GameSettings();

        // A scene that isn't in this build (renamed or removed): start in the castle grounds instead of failing to load.
        if (!SceneExists(data.scene))
        {
            Debug.LogWarning($"[SaveSystem] Slot {slot + 1} is in '{data.scene}', which this game doesn't have: starting at the castle grounds.");
            data.scene = FallbackScene;
            data.spawn = "";
        }
        GameSession.NextSpawn = string.IsNullOrEmpty(data.spawn) ? null : data.spawn;
        return data.scene;
    }

    // Tests can pretend scenes exist (or don't) without a build.
    public static Func<string, bool> SceneExistsOverride;

    public static bool SceneExists(string scene) =>
        !string.IsNullOrEmpty(scene) && (SceneExistsOverride != null ? SceneExistsOverride(scene)
                                         : UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{scene}.unity") >= 0);

    // Erase a slot. A readable save is deleted (with its backup); a damaged one is set aside as save<N>.damaged.json
    // instead, so even "erase" doesn't destroy something that might still be recoverable by hand.
    public static void Delete(int slot)
    {
        string path = PathFor(slot);
        bool damaged = StateOf(slot) == SlotState.Damaged;
        try
        {
            if (damaged && File.Exists(path)) File.Copy(path, DamagedPath(slot), true);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Couldn't erase slot {slot + 1}: {e.Message}");
        }
    }
}
