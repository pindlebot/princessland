using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Tests for the save audit: schema version, damaged / newer / unreadable slots, safe writes and the last good backup,
// defensive loading, the moments a save is made (boss reward, skill, digging, fish, closing), and what Continue restores.
public class SaveHardeningTests
{
    private string folder;

    [SetUp]
    public void UseTemporaryFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "TidecrownHardening_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        SaveSystem.SceneExistsOverride = null;
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        ActionFeedback.Reset();
    }

    [TearDown]
    public void Reset()
    {
        SaveRunner.Cancel();
        SaveSystem.FolderOverride = null;
        SaveSystem.SceneExistsOverride = null;
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        Time.timeScale = 1f;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }

    private string File1 => Path.Combine(folder, "save1.json");

    private static void Seed(string scene = "Dungeon", int gold = 5)
    {
        GameSession.NewGame(null);
        GameSession.Flags.Add("met:Coralie");
        GameSession.Progress.AddGold(gold);
    }

    // ---------- Versions and slot states ----------

    [Test]
    public void ASaveCarriesTheCurrentVersion()
    {
        Seed();
        Assert.IsTrue(SaveSystem.Save(0, "Dungeon", ""));
        StringAssert.Contains($"\"version\": {SaveSystem.CurrentVersion}", File.ReadAllText(File1));
        Assert.AreEqual(SaveSystem.SlotState.Fine, SaveSystem.StateOf(0));
        Assert.AreEqual(SaveSystem.SlotState.Empty, SaveSystem.StateOf(1));
    }

    [Test]
    public void AnOldSaveWithNoVersionAndMissingFieldsStillLoadsAndKeepsEverything()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(File1, "{\"hero\":\"Princess\",\"scene\":\"Woods2\",\"flags\":[\"met:Old Moss\"],\"gold\":12,\"level\":3}");
        var data = SaveSystem.Peek(0);
        Assert.IsNotNull(data, "a version-1 file with no newer fields");
        Assert.AreEqual(SaveSystem.CurrentVersion, data.version, "brought up to date in memory");
        Assert.AreEqual(12, data.gold);
        Assert.IsNotNull(data.bag);
        Assert.IsNotNull(data.settings);
        Assert.AreEqual("Woods2", SaveSystem.Load(0, new CharacterDefinition[0]));
        Assert.IsTrue(GameSession.Flags.Contains("met:Old Moss"));
        Assert.AreEqual(3, GameSession.Progress.Level);
    }

    [Test]
    public void ASaveFromANewerGameIsNeitherReadNorOverwritten()
    {
        Directory.CreateDirectory(folder);
        string future = "{\"version\":99,\"hero\":\"Wizard\",\"scene\":\"Level0\",\"gold\":1,\"level\":1,\"somethingNew\":true}";
        File.WriteAllText(File1, future);
        Assert.AreEqual(SaveSystem.SlotState.FromNewerGame, SaveSystem.StateOf(0));
        Assert.IsNull(SaveSystem.Peek(0));
        Assert.IsNull(SaveSystem.Load(0, new CharacterDefinition[0]));

        string heard = null;
        SaveSystem.SaveFailed += m => heard = m;
        Seed();
        Assert.IsFalse(SaveSystem.Save(0, "Dungeon", ""), "it must not clobber it");
        Assert.AreEqual(future, File.ReadAllText(File1), "untouched");
        StringAssert.Contains("newer", SaveSystem.LastError);
        Assert.IsNotNull(heard, "and the game is told");
        Assert.AreEqual(-1, SaveSystem.MostRecentSlot(), "Continue does not offer it");
    }

    [Test]
    public void AnUnreadableSaveIsDamagedNotEmptyAndOverwritingKeepsACopy()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(File1, "{ this is not json");
        Assert.AreEqual(SaveSystem.SlotState.Damaged, SaveSystem.StateOf(0), "there is a file: not an empty slot");
        Assert.IsNull(SaveSystem.Peek(0));
        Assert.AreEqual(-1, SaveSystem.MostRecentSlot());

        Seed();
        Assert.IsTrue(SaveSystem.Save(0, "Dungeon", ""), "a deliberate new save in it works");
        Assert.AreEqual("{ this is not json", File.ReadAllText(Path.Combine(folder, "save1.damaged.json")), "and the broken file is kept beside it");
        Assert.AreEqual(SaveSystem.SlotState.Fine, SaveSystem.StateOf(0));
    }

    [Test]
    public void ErasingADamagedSlotSetsItAsideInsteadOfDestroyingIt()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(File1, "garbage");
        SaveSystem.Delete(0);
        Assert.IsFalse(File.Exists(File1));
        Assert.IsTrue(File.Exists(Path.Combine(folder, "save1.damaged.json")));
        Assert.AreEqual(SaveSystem.SlotState.Empty, SaveSystem.StateOf(0));
    }

    [Test]
    public void ADamagedMainFileFallsBackToTheLastGoodSave()
    {
        Seed(gold: 1);
        SaveSystem.Save(0, "Dungeon", "");
        GameSession.Progress.AddGold(10);
        SaveSystem.Save(0, "Dungeon", "");     // the first save is now the backup
        Assert.IsTrue(File.Exists(File1 + ".bak"));

        File.WriteAllText(File1, "{ cut off in the mid");   // a crash or a bad disk
        var data = SaveSystem.Peek(0);
        Assert.IsNotNull(data, "the previous good save is offered");
        Assert.AreEqual(1, data.gold, "it is the one before the last");
        Assert.AreEqual(SaveSystem.SlotState.Fine, SaveSystem.StateOf(0));
    }

    [Test]
    public void AFailedWriteKeepsTheLastGoodSaveAndSaysSo()
    {
        Seed(gold: 1);
        Assert.IsTrue(SaveSystem.Save(0, "Dungeon", ""));
        string good = File.ReadAllText(File1);

        Directory.CreateDirectory(File1 + ".tmp");   // the temporary file can't be written: a directory is in its place
        GameSession.Progress.AddGold(50);
        int failures = SaveSystem.Failures;
        Assert.IsFalse(SaveSystem.Save(0, "Level0", ""));
        Assert.AreEqual(failures + 1, SaveSystem.Failures);
        Assert.IsNotNull(SaveSystem.LastError);
        Assert.AreEqual(good, File.ReadAllText(File1), "the last good save is exactly as it was");
        Assert.AreEqual("Dungeon", SaveSystem.Peek(0).scene);
    }

    [Test]
    public void SavingKeepsThreeSlotsApart()
    {
        for (int i = 0; i < 3; i++)
        {
            GameSession.NewGame(null);
            GameSession.Progress.AddGold(10 * (i + 1));
            GameSession.Flags.Add("slot:" + i);
            Assert.IsTrue(SaveSystem.Save(i, i == 0 ? "Level0" : i == 1 ? "Dungeon" : "Cove", ""));
        }
        for (int i = 0; i < 3; i++)
        {
            var data = SaveSystem.Peek(i);
            Assert.AreEqual(10 * (i + 1), data.gold);
            CollectionAssert.Contains(data.flags, "slot:" + i);
            for (int j = 0; j < 3; j++) if (j != i) CollectionAssert.DoesNotContain(data.flags, "slot:" + j);
        }
        SaveSystem.Delete(1);
        Assert.IsNotNull(SaveSystem.Peek(0));
        Assert.IsNull(SaveSystem.Peek(1));
        Assert.IsNotNull(SaveSystem.Peek(2));
    }

    // ---------- Defensive loading ----------

    [Test]
    public void UnknownThingsInASaveAreKeptAndDoNotBreakLoading()
    {
        SaveSystem.SceneExistsOverride = scene => scene == "Level0";
        Directory.CreateDirectory(folder);
        var data = new SaveData
        {
            hero = "RemovedHero", scene = "RemovedScene", spawn = "FromNowhere",
            flags = { "from:future" }, bag = { "healing_apple", "item_from_the_future" },
            keyItems = { "bouncy_boots", "key_from_the_future" }, skills = { "skill_from_the_future" },
            level = 4, gold = 9,
        };
        File.WriteAllText(File1, JsonUtility.ToJson(data));

        string scene = SaveSystem.Load(0, new CharacterDefinition[0]);
        Assert.AreEqual("Level0", scene, "a scene that is gone: the castle grounds");
        Assert.IsNull(GameSession.NextSpawn, "and no spawn name from the missing place");
        Assert.IsNull(GameSession.SelectedCharacter, "a removed hero: the default one");
        CollectionAssert.Contains(GameSession.Inventory.Bag, "item_from_the_future", "unknown items are kept, not thrown away");
        CollectionAssert.Contains(GameSession.Inventory.KeyItems, "key_from_the_future");
        Assert.IsTrue(GameSession.Flags.Contains("from:future"));
        Assert.AreEqual(4, GameSession.Progress.Level);
        Assert.AreEqual(9, GameSession.Progress.Gold);
    }

    [Test]
    public void EveryPlaceHasAFriendlyNameForTheSlotCard()
    {
        foreach (var path in Directory.GetFiles(Path.Combine(Application.dataPath, "Levels"), "*.txt"))
        {
            string scene = Path.GetFileNameWithoutExtension(path);
            Assert.AreNotEqual(scene, SaveSystem.PlaceName(scene), $"{scene} shows a friendly name, not the scene name");
        }
    }

    // ---------- Saves at the moments that matter ----------

    private static IEnumerator Load(string scene, int slot = 0)
    {
        GameSession.NewGame(null);
        GameSession.Slot = slot;
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    // Close the game and open it again: what's on disk, loaded into a blank session.
    private SaveData Restart()
    {
        SaveRunner.Flush();
        var data = SaveSystem.Peek(0);
        GameSession.NewGame(null);
        SaveSystem.Load(0, new CharacterDefinition[0]);
        return data;
    }

    [UnityTest]
    public IEnumerator ARewardTakenFromABossIsOnDiskWithinAMomentAndAfterARestart()
    {
        yield return Load("Dungeon");
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        boss.Health.TakeDamage(999);
        yield return null;
        var reward = Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == Abilities.BouncyBoots);
        reward.Interact(LevelBootstrap.Current.Player);
        Assert.IsTrue(SaveRunner.IsPending, "queued");
        Assert.IsFalse(File.Exists(File1), "not written for every little thing");
        yield return new WaitForSeconds(2.1f);
        Assert.IsTrue(File.Exists(File1), "written a moment later");
        var data = Restart();
        CollectionAssert.Contains(data.keyItems, Abilities.BouncyBoots);
        Assert.IsTrue(Abilities.Has(Abilities.BouncyBoots));
        Assert.IsTrue(GameSession.Flags.Contains("cleared:Dungeon"));
    }

    [UnityTest]
    public IEnumerator ABurstOfEventsIsOneWriteAndASkillIsSaved()
    {
        yield return Load("Dungeon");
        for (int i = 0; i < 20; i++) GameSession.MarkUsed("pot" + i);
        SaveRunner.Flush();
        long first = File.GetLastWriteTimeUtc(File1).Ticks;
        Assert.IsFalse(SaveRunner.IsPending);

        GameSession.Progress.AddXp(10000);
        var tree = Object.FindAnyObjectByType<SkillTreeView>();
        Assert.IsTrue(tree.TryLearn(tree.Path[0]));
        Assert.IsTrue(SaveRunner.IsPending, "buying a skill asks for a save");
        var data = Restart();
        CollectionAssert.Contains(data.skills, tree.Path[0].Id);
        Assert.IsTrue(GameSession.Flags.Contains("used:pot19"));
    }

    [UnityTest]
    public IEnumerator DiggingAFishCatchAndClosingTheGameAreAllKept()
    {
        yield return Load("Mines1");
        GameSession.Inventory.KeyItems.Add(Abilities.MoleMitts);
        var dirt = Object.FindObjectsByType<SoftDirt>().First();
        dirt.Dig();
        GameSession.AddToCounter("fish_caught");
        var data = Restart();
        Assert.IsTrue(GameSession.Counters.ContainsKey(SoftDirt.DugCounter));
        Assert.AreEqual(1, GameSession.GetCounter("fish_caught"));
        Assert.IsTrue(GameSession.Flags.Any(f => f.StartsWith("used:")), "the dug mound is remembered");

        // Closing the game with nothing queued still saves progress made since the last door (what the hero holds).
        GameSession.Flags.Add("tip:walk");
        SaveRunner.Flush();
        GameSession.Slot = 0;
        SaveSystem.AutosaveHere();
        Assert.IsTrue(SaveSystem.Peek(0).flags.Contains("tip:walk"));
    }

    [UnityTest]
    public IEnumerator TheTitleSceneNeverSavesItselfAsThePlace()
    {
        GameSession.NewGame(null);
        GameSession.Slot = 0;
        SceneManager.LoadScene("Title");
        yield return null;
        yield return null;
        SaveSystem.AutosaveHere();
        Assert.IsFalse(File.Exists(File1), "nothing is saved from the menu");
    }

    [UnityTest]
    public IEnumerator AFailedSaveShowsAKindToastAndNeverASilentSuccess()
    {
        yield return Load("Dungeon");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(File1 + ".tmp");
        Assert.IsFalse(SaveSystem.Autosave("Dungeon", ""));
        yield return null;
        var hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
        StringAssert.Contains("Couldn't save", hud.Q<Label>("toast").text);
        StringAssert.Contains("last save is safe", hud.Q<Label>("toast").text);
    }

    // ---------- Continue ----------

    [UnityTest]
    public IEnumerator ContinueStartsWithFullHeartsAndMagicAndTheTitleSaysSo()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        player.GetComponent<Health>().TakeDamage(2);
        player.GetComponent<Mana>().TrySpend(30);
        SaveSystem.Save(0, "Dungeon", "");

        GameSession.NewGame(null);
        string scene = SaveSystem.Load(0, new CharacterDefinition[0]);
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
        var again = LevelBootstrap.Current.Player;
        Assert.AreEqual(again.GetComponent<Health>().Max, again.GetComponent<Health>().Current, "hearts are full");
        Assert.AreEqual(again.GetComponent<Mana>().Max, again.GetComponent<Mana>().Current, 0.01f, "and so is magic");

        GameSession.NewGame(null);
        SceneManager.LoadScene("Title");
        yield return null;
        yield return null;
        var title = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        StringAssert.Contains("full hearts and magic", title.Q<Label>("status").text);
    }

    [UnityTest]
    public IEnumerator TheTitleScreenShowsADamagedSlotAsDamagedAndWillNotStartOverIt()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(File1, "not json at all");
        GameSession.NewGame(null);
        SceneManager.LoadScene("Title");
        yield return null;
        yield return null;
        var title = Object.FindAnyObjectByType<TitleController>();
        var root = title.GetComponent<UIDocument>().rootVisualElement;
        var card = root.Q("slot-0");
        Assert.IsTrue(card.ClassListContains("damaged"));
        Assert.IsFalse(card.ClassListContains("empty"), "not mistaken for an empty slot");
        Assert.AreEqual("Damaged save", card.Q<Label>("slot-name").text);
        Assert.AreEqual(DisplayStyle.Flex, card.Q<Button>("slot-erase").style.display.value, "it can be erased (a copy is kept)");

        title.OpenSlot(0);
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual("Title", SceneManager.GetActiveScene().name, "it did not start a new adventure over the top");
        StringAssert.Contains("can't be read", root.Q<Label>("status").text);
        Assert.AreEqual("not json at all", File.ReadAllText(File1));
    }
}
