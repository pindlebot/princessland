using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the save slots, autosave on doors and the title screen.
// Every test saves into a temporary folder, never the real save files.
public class SaveTests
{
    private string folder;

    [SetUp]
    public void UseTemporaryFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "IsoDungeonSaveTests_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
    }

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        SaveSystem.FolderOverride = null;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
    }

    private static CharacterDefinition[] Heroes() => Object.FindAnyObjectByType<TitleController>().Heroes;

    [UnityTest]
    public IEnumerator SaveAndLoadRoundTripTheWholeSession()
    {
        yield return Load("Title");
        var heroes = Heroes();
        var princess = heroes.First(h => h.name == "Princess");

        GameSession.NewGame(princess);
        GameSession.Flags.Add("met_dragon");
        GameSession.MarkUsed("Dungeon/5,2");
        GameSession.Progress.AddXp(200);   // a couple of levels
        GameSession.Progress.AddGold(37);
        GameSession.Progress.Learn(SkillCatalog.All.First(s => s.Requires == null));
        GameSession.Inventory.Bag.Add("mystery_pebble");
        GameSession.Inventory.Equipped[EquipSlot.Ring] = "ember_ring";
        var p = GameSession.Progress;
        int level = p.Level, xp = p.Xp, points = p.SkillPoints;
        var skills = p.LearnedSkills.ToList();

        SaveSystem.Save(1, "Dungeon", "FromCastle");
        Assert.IsTrue(File.Exists(Path.Combine(folder, "save2.json")));
        Assert.IsFalse(File.Exists(Path.Combine(folder, "save2.json.tmp")), "no leftover temp file");

        GameSession.NewGame(null); // forget everything...
        Assert.AreEqual(0, GameSession.Progress.Gold);

        Assert.AreEqual("Dungeon", SaveSystem.Load(1, heroes)); // ...and get it back
        Assert.AreEqual(princess, GameSession.SelectedCharacter);
        Assert.AreEqual(1, GameSession.Slot, "carries on saving into the same slot");
        Assert.AreEqual("FromCastle", GameSession.NextSpawn);
        Assert.IsTrue(GameSession.Flags.Contains("met_dragon"));
        Assert.IsTrue(GameSession.IsUsed("Dungeon/5,2"));
        Assert.AreEqual(level, GameSession.Progress.Level);
        Assert.Greater(level, 1);
        Assert.AreEqual(xp, GameSession.Progress.Xp);
        Assert.AreEqual(37, GameSession.Progress.Gold);
        Assert.AreEqual(points, GameSession.Progress.SkillPoints);
        CollectionAssert.AreEquivalent(skills, GameSession.Progress.LearnedSkills.ToList());
        CollectionAssert.AreEqual(new[] { "mystery_pebble" }, GameSession.Inventory.Bag);
        Assert.AreEqual("ember_ring", GameSession.Inventory.Equipped[EquipSlot.Ring]);

        // Empty and damaged slots read as "no save", not as errors that break the title screen.
        Assert.IsNull(SaveSystem.Load(0, heroes));
        File.WriteAllText(Path.Combine(folder, "save3.json"), "{ this is not json");
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Couldn't read slot 3"));
        Assert.IsNull(SaveSystem.Peek(2));
        Assert.AreEqual(1, SaveSystem.MostRecentSlot());
    }

    [UnityTest]
    public IEnumerator GoingThroughADoorAutosaves()
    {
        GameSession.Slot = 0;
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        GameSession.Progress.AddGold(12);

        var gate = Object.FindObjectsByType<SceneDoor>().First(d => d.TargetScene == "House");
        gate.Interact(player);
        var save = SaveSystem.Peek(0);
        Assert.IsNotNull(save, "the door saved the game");
        Assert.AreEqual("House", save.scene, "continuing puts you on the far side of the door");
        Assert.AreEqual(12, save.gold);
        yield return Load("House");
    }

    [UnityTest]
    public IEnumerator TitleScreenShowsSlotsAndContinuesTheLatest()
    {
        // Two saves: slot 3 is the most recent.
        yield return Load("Title");
        var heroes = Heroes();
        GameSession.NewGame(heroes.First(h => h.name == "Wizard"));
        GameSession.Progress.AddGold(5);
        SaveSystem.Save(0, "Level0", "");
        yield return new WaitForSecondsRealtime(0.05f);
        GameSession.NewGame(heroes.First(h => h.name == "Princess"));
        GameSession.Progress.AddGold(25);
        GameSession.Inventory.Equipped[EquipSlot.Ring] = "ember_ring";
        SaveSystem.Save(2, "Dungeon", "");
        GameSession.NewGame(null);

        yield return Load("Title"); // reload so the cards are drawn from the files
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        Assert.AreEqual(DisplayStyle.Flex, root.Q("continue").resolvedStyle.display);
        Assert.AreEqual("Aldric", root.Q("slot-0").Q<Label>("slot-name").text.Split(' ')[0]);
        Assert.AreEqual("Castle Grounds", root.Q("slot-0").Q<Label>("slot-place").text);
        Assert.IsTrue(root.Q("slot-1").ClassListContains("empty"));
        Assert.AreEqual("New adventure", root.Q("slot-1").Q<Label>("slot-name").text);
        Assert.AreEqual("Princess Marina", root.Q("slot-2").Q<Label>("slot-name").text);
        Assert.AreEqual("Level 1   25 gold", root.Q("slot-2").Q<Label>("slot-stats").text);

        Object.FindAnyObjectByType<TitleController>().Continue();
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        Assert.AreEqual("Tidal Orb", player.GetComponent<SpellAbility>().SpellName, "she's back as the princess");
        Assert.AreEqual(25, GameSession.Progress.Gold);
        Assert.AreEqual("ember_ring", player.GetComponent<Inventory>().Equipped(EquipSlot.Ring).Id);
        Assert.AreEqual(2, GameSession.Slot);
    }

    [UnityTest]
    public IEnumerator AnEmptySlotStartsANewAdventureThatSavesThere()
    {
        yield return Load("Title");
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        Assert.AreEqual(DisplayStyle.None, root.Q("continue").resolvedStyle.display, "nothing to continue yet");

        Object.FindAnyObjectByType<TitleController>().OpenSlot(1);
        yield return Load("CharacterSelect");
        Assert.AreEqual(1, GameSession.Slot);
        var select = Object.FindAnyObjectByType<CharacterSelectController>();
        select.Select(1);
        select.StartGame();
        Assert.AreEqual(1, GameSession.Slot, "picking a hero keeps the slot");
        var save = SaveSystem.Peek(1);
        Assert.IsNotNull(save, "the new adventure is saved straight away");
        Assert.AreEqual("Princess", save.hero);
        Assert.AreEqual("Level0", save.scene);
        yield return Load("Level0");
    }
}
