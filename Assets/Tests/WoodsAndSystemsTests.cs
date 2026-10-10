using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the systems behind the Whispering Woods: the condition language and quests (and
// the quest log), counters and the easter eggs that use them, the hat and charm slots, quick slots and
// treasures tab, fountains as save points and fast travel, breakable pots, and the four Woods rooms.
public class WoodsAndSystemsTests
{
    private string folder;

    [SetUp]
    public void UseTemporaryFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "TidecrownWoodsTests_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        GameSession.NewGame(null);
    }

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        SaveSystem.FolderOverride = null;
        Time.timeScale = 1f;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }

    private static IEnumerator Load(string scene, bool freezeEnemies = true)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        if (freezeEnemies) foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
    }

    private static void Teleport(GameObject player, Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = to;
        cc.enabled = true;
    }

    private static string TalkTo(Npc npc)
    {
        string first = npc.Next().lines[0].text;
        npc.Interact(LevelBootstrap.Current.Player);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.IsFalse(DialogueController.IsOpen);
        return first;
    }

    private static Npc NpcNamed(string name) => Object.FindObjectsByType<Npc>().First(n => n.Name == name);

    // ---------- Conditions and quests ----------

    [Test]
    public void ConditionsReadFlagsCountersAndItems()
    {
        Assert.IsTrue(Condition.Met(""));
        Assert.IsFalse(Condition.Met("met:Pearl"));
        GameSession.Flags.Add("met:Pearl");
        Assert.IsTrue(Condition.Met("met:Pearl"));
        Assert.IsFalse(Condition.Met("!met:Pearl"));
        Assert.IsTrue(Condition.Met("!found:frog"));

        Assert.IsFalse(Condition.Met("talks:Amethyra>=2"));
        GameSession.AddToCounter("talks:Amethyra", 2);
        Assert.IsTrue(Condition.Met("talks:Amethyra>=2"));
        Assert.IsTrue(Condition.Met("talks:Amethyra==2"));
        Assert.IsFalse(Condition.Met("talks:Amethyra>2"));
        Assert.IsTrue(Condition.Met("talks:Amethyra<3"));

        Assert.IsFalse(Condition.Met("has:egg"));
        GameSession.Inventory.Bag.Add("egg");
        Assert.IsTrue(Condition.Met("has:egg"));
        GameSession.Inventory.Equipped[EquipSlot.Hat] = "frog_hat";
        Assert.IsTrue(Condition.Met("has:frog_hat"), "worn things count too");

        Assert.IsTrue(Condition.Met("met:Pearl, talks:Amethyra>=2, has:egg"), "commas mean all of them");
        Assert.IsFalse(Condition.Met("met:Pearl, found:frog"));
    }

    [Test]
    public void QuestsHideStartAndFinishFromFlagsAndCounters()
    {
        var frog = QuestCatalog.Find("frog");
        Assert.AreEqual(QuestState.Hidden, QuestCatalog.StateOf(frog));
        Assert.AreEqual(0, QuestCatalog.Listed().Count);

        GameSession.Flags.Add("met:Coralie");
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(frog));
        StringAssert.Contains("little frog", QuestCatalog.CurrentStep(frog).Text);

        GameSession.Flags.Add(FrogBush.FoundFlag);
        StringAssert.Contains("Tell Coralie", QuestCatalog.CurrentStep(frog).Text);
        Assert.AreEqual(1, QuestCatalog.StepsDone(frog));

        GameSession.Flags.Add("thanked:frog");
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(frog));
        Assert.IsNull(QuestCatalog.CurrentStep(frog));
    }

    [Test]
    public void TheTreesQuestShowsItsProgress()
    {
        var quest = QuestCatalog.Find("trees");
        GameSession.Flags.Add("met:Old Moss");
        GameSession.AddToCounter(SleepyTree.WokenCounter, 2);
        StringAssert.EndsWith("(2/4)", QuestCatalog.StepText(QuestCatalog.CurrentStep(quest)));
        GameSession.AddToCounter(SleepyTree.WokenCounter, 5);
        StringAssert.Contains("Tell Old Moss", QuestCatalog.CurrentStep(quest).Text, "past the goal, on to the next step");
    }

    [UnityTest]
    public IEnumerator TheQuestLogShowsCardsWithPicturesAndAnnouncesNewQuests()
    {
        yield return Load("Level0");
        var log = Object.FindAnyObjectByType<QuestLogView>();
        log.SetOpen(true);
        yield return null;
        Assert.AreEqual(0, log.CardCount, "no quests yet");

        GameSession.Flags.Add("met:Coralie");
        GameSession.Flags.Add("met:Pearl");
        GameSession.Flags.Add("cleared:Cove");
        GameSession.Flags.Add("thanked:cove");
        yield return null;
        yield return null;
        Assert.AreEqual(2, log.CardCount, "Coralie's quest and Pearl's (finished)");
        Assert.IsTrue(GameSession.Flags.Contains("quest:seen:frog"), "announced once");
        Assert.IsTrue(GameSession.Flags.Contains("quest:done:cove"));

        var doc = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        var cards = doc.Q("quest-list").Children().ToList();
        Assert.IsTrue(cards[1].ClassListContains("done"), "finished quests sink to the bottom");
        Assert.IsNotNull(cards[0].Q(className: "quest-portrait").style.backgroundImage.value.sprite, "the giver's picture");
    }

    // ---------- Easter eggs with counters ----------

    [UnityTest]
    public IEnumerator FlushingTenTimesBringsOutAFrogWithAHat()
    {
        yield return Load("House");
        var player = LevelBootstrap.Current.Player;
        var hero = player.GetComponent<PlayerController>();
        var toilet = Object.FindObjectsByType<HouseFixture>().Single(f => f.name == "Toilet");
        var bag = player.GetComponent<Inventory>();

        for (int i = 0; i < 10; i++)
        {
            toilet.Interact(player); // sit
            yield return null;
            Assert.IsTrue(hero.IsSeated);
            toilet.Interact(player); // flush and stand
            yield return null;
            yield return null;
            Assert.IsFalse(hero.IsSeated);
            if (i < 9) Assert.IsFalse(bag.Has("frog_hat"), "not until the tenth");
        }
        Assert.AreEqual(10, GameSession.GetCounter("toilet_flushes"));
        Assert.IsTrue(bag.Has("frog_hat"));
        Assert.IsTrue(GameSession.Flags.Contains(HouseFixture.ToiletFrogFlag));
        Assert.IsTrue(GameSession.Flags.Contains(HouseFixture.UnwashedFlag), "and you still haven't washed up");
        yield return null;
        Assert.IsNotNull(Object.FindAnyObjectByType<Frog>(), "the frog hopped out");

        // He only comes once.
        toilet.Interact(player);
        yield return null;
        toilet.Interact(player);
        yield return null;
        yield return null;
        Assert.AreEqual(1, bag.Bag.Count(i => i.Id == "frog_hat"));

        // Wear it in the hat slot.
        Assert.IsTrue(bag.Equip(bag.Bag.First(i => i.Id == "frog_hat")));
        Assert.AreEqual("frog_hat", bag.Equipped(EquipSlot.Hat).Id);
        Assert.AreEqual(1, bag.MaxHealthBonus);
    }

    [UnityTest]
    public IEnumerator WashingHandsClearsTheUnwashedFlagAndAmethyraNotices()
    {
        yield return Load("House");
        var player = LevelBootstrap.Current.Player;
        var sink = Object.FindObjectsByType<HouseFixture>().Single(f => f.name == "Sink");
        GameSession.Flags.Add(HouseFixture.UnwashedFlag);
        sink.Interact(player); // soap
        sink.Interact(player); // rinse
        Assert.IsFalse(GameSession.Flags.Contains(HouseFixture.UnwashedFlag));
        Assert.AreEqual(1, GameSession.GetCounter(HouseFixture.HandsWashedCounter));

        yield return Load("Level0");
        var amethyra = NpcNamed("Amethyra");
        TalkTo(amethyra);                                 // introductions
        GameSession.Flags.Add(HouseFixture.UnwashedFlag); // ...and then she smells the toilet
        StringAssert.Contains("wash your hands", TalkTo(amethyra));
        GameSession.Flags.Remove(HouseFixture.UnwashedFlag);
        Assert.AreNotEqual("...{hero}. Did you wash your hands?", amethyra.Next().lines[0].text);
    }

    [UnityTest]
    public IEnumerator AmethyraTellsABestJokeOnTheTenthChatAndSnoresOnThe25th()
    {
        yield return Load("Level0");
        var amethyra = NpcNamed("Amethyra");
        for (int i = 0; i < 9; i++) TalkTo(amethyra);
        Assert.AreEqual(9, GameSession.GetCounter("talks:Amethyra"));
        int gold = GameSession.Progress.Gold;
        StringAssert.Contains("Ten chats", TalkTo(amethyra), "the 10th chat");
        Assert.AreEqual(gold + 10, GameSession.Progress.Gold);
        StringAssert.DoesNotContain("Ten chats", TalkTo(amethyra), "only once");

        for (int i = GameSession.GetCounter("talks:Amethyra"); i < 24; i++) TalkTo(amethyra);
        StringAssert.Contains("yawn", TalkTo(amethyra), "the 25th chat: she falls asleep");
        Assert.IsTrue(GameSession.Flags.Contains("snored"));
        StringAssert.DoesNotContain("yawn", TalkTo(amethyra));
    }

    // ---------- Inventory: hat and charm slots, quick slots, treasures ----------

    [UnityTest]
    public IEnumerator HatAndCharmSlotsQuickSlotsAndTreasuresWork()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var bag = player.GetComponent<Inventory>();
        var db = bag.Database;

        // Treasures take no bag room.
        Assert.IsTrue(bag.Add(db.Find("fairy_lantern")));
        Assert.AreEqual(0, bag.Bag.Count);
        Assert.AreEqual(1, bag.KeyItems.Count);
        Assert.IsTrue(bag.HasKeyItem("fairy_lantern"));
        Assert.IsTrue(bag.Add(db.Find("fairy_lantern")));
        Assert.AreEqual(1, bag.KeyItems.Count, "only ever one");

        // Hat and charm.
        bag.Add(db.Find("frog_hat"));
        bag.Add(db.Find("clover_charm"));
        Assert.IsTrue(bag.Equip(bag.Bag.First(i => i.Id == "frog_hat")));
        Assert.IsTrue(bag.Equip(bag.Bag.First(i => i.Id == "clover_charm")));
        Assert.AreEqual("clover_charm", bag.Equipped(EquipSlot.Charm).Id);
        Assert.Less(bag.SpellCooldownFactor, 1f, "the charm recharges spells faster");

        // Quick slots: the first consumables take the first free slots, in order.
        var apple = db.Find("healing_apple");
        var berry = db.Find("mana_berry");
        bag.Add(apple);
        bag.Add(apple);
        bag.Add(berry);
        Assert.AreEqual(apple, bag.Quick(0));
        Assert.AreEqual(berry, bag.Quick(1));
        Assert.IsNull(bag.Quick(2));

        var health = player.GetComponent<Health>();
        health.SetMax(6);
        health.TakeDamage(3);
        int before = health.Current;
        Assert.IsTrue(bag.UseQuick(0));
        Assert.AreEqual(before + 2, health.Current, "an apple heals two hearts");
        Assert.AreEqual(1, bag.Count(apple));
        Assert.IsTrue(bag.UseQuick(0));
        Assert.IsFalse(bag.UseQuick(0), "none left");
        Assert.AreEqual(apple, bag.Quick(0), "the slot remembers its item");

        // Reassigning moves it, never duplicates it.
        Assert.IsTrue(bag.AssignQuick(3, berry));
        Assert.IsNull(bag.Quick(1));
        Assert.AreEqual(berry, bag.Quick(3));
        Assert.IsFalse(bag.AssignQuick(0, db.Find("frog_hat")), "only consumables");
    }

    [UnityTest]
    public IEnumerator TreasuresQuickSlotsHatAndCharmSurviveSavingAndDoors()
    {
        yield return Load("Level0");
        var bag = LevelBootstrap.Current.Player.GetComponent<Inventory>();
        var db = bag.Database;
        bag.Add(db.Find("fairy_lantern"));
        bag.Add(db.Find("mana_berry"));
        bag.Add(db.Find("clover_charm"));
        bag.Equip(bag.Bag.First(i => i.Id == "clover_charm"));

        SaveSystem.Save(1, "Level0", "");
        var data = SaveSystem.Peek(1);
        CollectionAssert.AreEqual(new[] { "fairy_lantern" }, data.keyItems);
        Assert.AreEqual("mana_berry", data.quick[0]);

        GameSession.NewGame(null);
        var heroes = Resources.FindObjectsOfTypeAll<CharacterDefinition>();
        SaveSystem.Load(1, heroes);
        Assert.IsTrue(GameSession.Inventory.KeyItems.Contains("fairy_lantern"));
        Assert.AreEqual("mana_berry", GameSession.Inventory.Quick[0]);
        Assert.AreEqual("clover_charm", GameSession.Inventory.Equipped[EquipSlot.Charm]);

        yield return Load("Woods1");
        bag = LevelBootstrap.Current.Player.GetComponent<Inventory>();
        Assert.IsTrue(bag.HasKeyItem("fairy_lantern"), "through the gate to the woods");
        Assert.IsTrue(LevelBootstrap.Current.Player.GetComponent<LanternLight>().IsLit, "and the lantern lights up");
    }

    // ---------- Pots ----------

    [UnityTest]
    public IEnumerator PotsBreakWhenZappedDropCoinsAndStayBroken()
    {
        yield return Load("Level0");
        var pots = Object.FindObjectsByType<BreakablePot>();
        Assert.GreaterOrEqual(pots.Length, 4);
        var pot = pots[0];
        var target = pot.GetComponent<ISpellTarget>();
        Assert.IsNotNull(target);

        target.OnSpellHit(1, SpellElement.Fire);
        yield return null;
        Assert.AreEqual(1, GameSession.GetCounter(BreakablePot.BrokenCounter));
        Assert.IsTrue(pot == null, "the pot is gone");
        Assert.GreaterOrEqual(Object.FindObjectsByType<CoinPickup>().Length, 1, "it spilled coins");

        yield return Load("Level0");
        Assert.AreEqual(pots.Length - 1, Object.FindObjectsByType<BreakablePot>().Length, "still broken after a visit elsewhere");
    }

    [UnityTest]
    public IEnumerator ASpellProjectileBreaksAPot()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var pot = Object.FindObjectsByType<BreakablePot>()[0];
        var spell = player.GetComponent<SpellAbility>();
        var projectile = spell.GetType().GetField("projectilePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(spell) as Projectile;
        Assert.IsNotNull(projectile, "the hero's projectile prefab");
        Vector3 from = pot.transform.position + new Vector3(-3f, 1f, 0f);
        Teleport(player, from + Vector3.back * 6f);
        var shot = Object.Instantiate(projectile, new Vector3(from.x, 0.6f, pot.transform.position.z), Quaternion.LookRotation(Vector3.right));
        for (float t = 0f; t < 1f && pot != null; t += Time.deltaTime) yield return null;
        Assert.IsTrue(pot == null, "the spell smashed it");
        Assert.AreEqual(1, GameSession.GetCounter(BreakablePot.BrokenCounter));
    }

    // ---------- Fountains ----------

    [UnityTest]
    public IEnumerator FountainsRememberYouHealAndSaveThenTravelBetweenEachOther()
    {
        GameSession.Slot = 0;
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var health = player.GetComponent<Health>();
        health.TakeDamage(2);
        var fountain = Object.FindAnyObjectByType<WakeFountain>();
        Assert.IsFalse(WakeFountain.Touched("Level0"));

        Teleport(player, fountain.WakeSpot.position + Vector3.up);
        yield return null;
        yield return null;
        Assert.IsTrue(WakeFountain.Touched("Level0"), "touched");
        Assert.AreEqual(health.Max, health.Current, "healed");
        var saved = SaveSystem.Peek(0);
        Assert.IsNotNull(saved, "saved");
        Assert.AreEqual("Level0", saved.scene);
        Assert.AreEqual(WakeFountain.SpawnName, saved.spawn);

        // The menu offers just "rest" until another fountain has been touched.
        var travel = Object.FindAnyObjectByType<FountainTravelView>();
        Assert.IsTrue(travel.TryOpen());
        CollectionAssert.AreEqual(new[] { "" }, travel.Options);
        travel.Close();
        yield return null;
        yield return null;

        GameSession.Flags.Add(WakeFountain.FlagFor("Woods1"));
        Assert.IsTrue(travel.TryOpen());
        CollectionAssert.AreEqual(new[] { "", "Woods1" }, travel.Options);
        travel.Choose(1);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != "Woods1"; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        Assert.AreEqual("Woods1", SceneManager.GetActiveScene().name);
        Assert.AreEqual(1f, Time.timeScale, "unpaused again");
        player = LevelBootstrap.Current.Player;
        var arrivedAt = Object.FindAnyObjectByType<WakeFountain>();
        Assert.Less(Vector3.Distance(player.transform.position, arrivedAt.WakeSpot.position), 2f, "you arrive at the Woods' fountain");
        Assert.AreEqual("Woods1", SaveSystem.Peek(0).scene);
    }

    [UnityTest]
    public IEnumerator TheFountainMenuIsClosedAwayFromFountains()
    {
        yield return Load("Level0");
        var travel = Object.FindAnyObjectByType<FountainTravelView>();
        Assert.IsFalse(travel.TryOpen(), "the hero starts nowhere near a fountain");
    }

    // ---------- The Whispering Woods ----------

    [UnityTest]
    public IEnumerator TheOpeningInTheWesternHedgeLeadsToTheWoodsAndBack()
    {
        yield return Load("Level0");
        var edge = Object.FindObjectsByType<RoomEdge>().First(d => d.TargetScene == "Woods1");
        Teleport(LevelBootstrap.Current.Player, edge.transform.position + Vector3.up); // just walk onto it: no button
        for (float t = 0f; t < 4f && SceneManager.GetActiveScene().name != "Woods1"; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        Assert.AreEqual("Woods1", SceneManager.GetActiveScene().name);
        var back = Object.FindObjectsByType<RoomEdge>().First(d => d.TargetScene == "Level0");
        Assert.Less(Vector3.Distance(LevelBootstrap.Current.Player.transform.position, back.transform.position), 5f, "you arrive by the opening");
    }

    [UnityTest]
    public IEnumerator TheGlade_OldMossAndTheFourSleepyTrees()
    {
        yield return Load("Woods1");
        var player = LevelBootstrap.Current.Player;
        var moss = NpcNamed("Old Moss");
        var trees = Object.FindObjectsByType<SleepyTree>();
        Assert.AreEqual(4, trees.Length);
        Assert.IsTrue(trees.All(t => !t.IsAwake));
        Assert.IsNotNull(Object.FindAnyObjectByType<WakeFountain>(), "the Woods have a fountain");

        StringAssert.Contains("Old Moss", TalkTo(moss));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("trees")));
        StringAssert.Contains("Four sleepy trees", TalkTo(moss), "until they're woken, he reminds you");

        // A spell hits a tree: it wakes up, once.
        trees[0].GetComponent<ISpellTarget>().OnSpellHit(1, SpellElement.Fire);
        trees[0].OnSpellHit(1, SpellElement.Fire);
        Assert.IsTrue(trees[0].IsAwake);
        Assert.AreEqual(1, GameSession.GetCounter(SleepyTree.WokenCounter));
        foreach (var tree in trees.Skip(1)) tree.Wake();
        Assert.AreEqual(4, GameSession.GetCounter(SleepyTree.WokenCounter));

        int gold = GameSession.Progress.Gold;
        StringAssert.Contains("wood waking up", TalkTo(moss));
        Assert.AreEqual(gold + 25, GameSession.Progress.Gold);
        Assert.IsTrue(player.GetComponent<Inventory>().Has("clover_charm"), "the Lucky Clover Charm");
        Assert.IsTrue(GameSession.Flags.Contains("thanked:moss"));
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("trees")));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("mushroom")));

        // They stay awake after leaving and coming back.
        yield return Load("Woods2");
        yield return Load("Woods1");
        Assert.IsTrue(Object.FindObjectsByType<SleepyTree>().All(t => t.IsAwake));
    }

    [UnityTest]
    public IEnumerator TheMeadowIsFullOfRootedSporePuffs()
    {
        yield return Load("Woods2", freezeEnemies: false);
        var puffs = Object.FindObjectsByType<EnemyAI>();
        Assert.GreaterOrEqual(puffs.Length, 6);
        Assert.IsTrue(puffs.All(p => p.Stationary), "Spore Puffs never chase");
        Assert.IsTrue(puffs.All(p => p.name.StartsWith("SporePuff")));
        Vector3 start = puffs[0].transform.position;
        var player = LevelBootstrap.Current.Player;
        Teleport(player, start + new Vector3(3f, 1f, 0f));
        for (float t = 0f; t < 1.5f; t += Time.deltaTime) yield return null;
        Assert.AreEqual(start.x, puffs[0].transform.position.x, 0.05f, "it didn't walk over");
        Assert.GreaterOrEqual(Object.FindObjectsByType<BreakablePot>().Length, 5);
    }

    [UnityTest]
    public IEnumerator TheHollowIsDarkAndTheLanternLightsIt()
    {
        yield return Load("Woods3");
        Assert.Less(RenderSettings.ambientLight.b, 0.3f, "dark");
        var player = LevelBootstrap.Current.Player;
        Assert.IsFalse(player.GetComponent<LanternLight>().IsLit);
        Assert.IsNotNull(Object.FindObjectsByType<Chest>().FirstOrDefault(), "a chest waits in the dark");
        player.GetComponent<Inventory>().Add(player.GetComponent<Inventory>().Database.Find("fairy_lantern"));
        yield return null;
        yield return null;
        Assert.IsTrue(player.GetComponent<LanternLight>().IsLit);
    }

    [UnityTest]
    public IEnumerator MotherMushroomGuardsTheLanternUntilSheIsBeaten()
    {
        GameSession.Flags.Add("thanked:moss"); // Old Moss has told you about her
        yield return Load("Woods4", freezeEnemies: false);
        var player = LevelBootstrap.Current.Player;
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        Assert.IsNotNull(boss);
        Assert.AreEqual("Mother Mushroom", boss.BossName);
        var reward = Object.FindAnyObjectByType<BossReward>(FindObjectsInactive.Include);
        Assert.IsFalse(reward.IsShown, "the lantern waits");

        boss.Health.TakeDamage(999);
        yield return null;
        Assert.IsTrue(reward.IsShown, "it appears when she falls");
        Assert.IsTrue(GameSession.Flags.Contains("cleared:Woods4"));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("mushroom")));

        var pickup = Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == "fairy_lantern");
        StringAssert.Contains("Fairy Lantern", pickup.Interact(player));
        Assert.IsTrue(player.GetComponent<Inventory>().HasKeyItem("fairy_lantern"), "a treasure");
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("mushroom")));
        yield return null;
        Assert.IsTrue(player.GetComponent<LanternLight>().IsLit);

        // Back later: she's gone, the lantern's taken, and it doesn't come back.
        yield return Load("Woods4", freezeEnemies: false);
        Assert.AreEqual(0, Object.FindObjectsByType<BossAbilities>().Length);
        Assert.AreEqual(0, Object.FindObjectsByType<ItemPickup>().Count(p => p.Item.Id == "fairy_lantern"));
    }

    [UnityTest]
    public IEnumerator AllFourRoomsConnectUpAndTheBossRoomHasNoExit()
    {
        string[] rooms = { "Woods1", "Woods2", "Woods3", "Woods4" };
        foreach (var room in rooms)
        {
            yield return Load(room);
            Assert.AreEqual(room, SceneManager.GetActiveScene().name);
            Assert.IsTrue(GameSession.Flags.Contains("visited:" + room));
            Assert.IsNotNull(LevelBootstrap.Current.Player);
            Assert.GreaterOrEqual(Object.FindObjectsByType<RoomEdge>().Length, 1);
        }
        var doorsFromMeadow = new[] { "Woods1", "Woods3", "Woods4" };
        yield return Load("Woods2");
        CollectionAssert.AreEquivalent(doorsFromMeadow, Object.FindObjectsByType<RoomEdge>().Select(d => d.TargetScene));
    }
}
