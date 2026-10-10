using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for the Glimmer Mines: the four rooms, the lost moles and Digby's mine-cart line, the Mole Mitts
// (push blocks, dig soft dirt), the Crystal Golem that can only be hurt while his crystals glow, and the egg.
public class MinesTests
{
    private string folder;

    [SetUp]
    public void UseTemporaryFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "TidecrownMinesTests_" + System.Guid.NewGuid().ToString("N"));
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

    private static Npc NpcNamed(string name) => Object.FindObjectsByType<Npc>().First(n => n.Name == name);

    private static void TalkTo(Npc npc)
    {
        npc.Interact(LevelBootstrap.Current.Player);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.IsFalse(DialogueController.IsOpen);
    }

    private static void GiveMitts() => GameSession.Inventory.KeyItems.Add(Abilities.MoleMitts);

    // ---------- The rooms ----------

    [UnityTest]
    public IEnumerator AllFourRoomsLoadAndConnectUp()
    {
        foreach (var room in new[] { "Mines1", "Mines2", "Mines3", "Mines4" })
        {
            yield return Load(room);
            Assert.AreEqual(room, SceneManager.GetActiveScene().name);
            Assert.IsTrue(GameSession.Flags.Contains("visited:" + room));
            Assert.GreaterOrEqual(Object.FindObjectsByType<RoomEdge>().Length, 1, room + " has a way out");
        }
        yield return Load("Mines1");
        CollectionAssert.AreEquivalent(new[] { "Woods3", "Mines2" }, Object.FindObjectsByType<RoomEdge>().Select(d => d.TargetScene));
        yield return Load("Woods3");
        CollectionAssert.Contains(Object.FindObjectsByType<RoomEdge>().Select(d => d.TargetScene).ToList(), "Mines1");
    }

    [UnityTest]
    public IEnumerator TheEntranceHasDigbyAFountainAndACart()
    {
        yield return Load("Mines1");
        Assert.IsNotNull(NpcNamed("Digby"));
        Assert.IsNotNull(Object.FindAnyObjectByType<WakeFountain>());
        Assert.IsNotNull(Object.FindAnyObjectByType<MineCart>());
        Assert.GreaterOrEqual(Object.FindObjectsByType<PushBlock>().Length, 1);
        Assert.GreaterOrEqual(Object.FindObjectsByType<SoftDirt>().Length, 2);
    }

    // ---------- Lost moles and the cart ----------

    [UnityTest]
    public IEnumerator FindingTheThreeMolesOpensTheMineCartLine()
    {
        yield return Load("Mines1");
        var digby = NpcNamed("Digby");
        Assert.AreEqual(QuestState.Hidden, QuestCatalog.StateOf(QuestCatalog.Find("moles")));
        Assert.IsFalse(digby.Next().smallTalk, "he introduces himself first");
        TalkTo(digby);
        Assert.IsTrue(GameSession.Flags.Contains("met:Digby"));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("moles")));

        // The cart's rails are jammed until he has his crew back.
        var cart = Object.FindAnyObjectByType<MineCart>();
        Assert.IsFalse(MineCart.IsOpen);
        StringAssert.Contains("rubble", cart.Interact(LevelBootstrap.Current.Player));
        Assert.IsTrue(cart.CanInteract, "still there to try again");

        yield return Load("Mines2");
        foreach (var name in new[] { "Molly", "Mortimer", "Mo" })
        {
            TalkTo(NpcNamed(name));
            Assert.IsTrue(GameSession.Flags.Contains("found:" + name.ToLowerInvariant()));
        }
        Assert.AreEqual(3, GameSession.GetCounter("moles_found"));
        TalkTo(NpcNamed("Mo")); // chatting again doesn't count again
        Assert.AreEqual(3, GameSession.GetCounter("moles_found"));
        StringAssert.Contains("(3/3)", QuestCatalog.StepText(QuestCatalog.Find("moles").Steps[0]));

        yield return Load("Mines1");
        digby = NpcNamed("Digby");
        int gold = GameSession.Progress.Gold;
        TalkTo(digby);
        Assert.IsTrue(GameSession.Flags.Contains("thanked:digby"));
        Assert.AreEqual(gold + 40, GameSession.Progress.Gold);
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("moles")));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("golem")));
        Assert.IsTrue(MineCart.IsOpen);

        // Ride it: Mines1 > Mines2, arriving at the cart station there.
        Object.FindAnyObjectByType<MineCart>().Interact(LevelBootstrap.Current.Player);
        for (float t = 0f; t < 4f && SceneManager.GetActiveScene().name != "Mines2"; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        Assert.AreEqual("Mines2", SceneManager.GetActiveScene().name);
        Assert.AreEqual(MineCart.SpawnName, GameSession.EnteredBy);
        var cart2 = Object.FindAnyObjectByType<MineCart>();
        var to = LevelBootstrap.Current.Player.transform.position - cart2.transform.position;
        to.y = 0f;
        Assert.Less(to.magnitude, 3f, "you step out beside the cart");
        Assert.AreEqual(1, GameSession.GetCounter("cart_rides"));
    }

    [Test]
    public void TheCartLineLoopsThroughThreeStations()
    {
        CollectionAssert.AreEqual(new[] { "Mines1", "Mines2", "Mines3" }, MineCart.Stations);
    }

    // ---------- The Mole Mitts ----------

    [UnityTest]
    public IEnumerator ABlockSlidesOneTileWithAClunkAndStopsAtAWall()
    {
        yield return Load("Mines1");
        GiveMitts();
        var block = Object.FindAnyObjectByType<PushBlock>();
        var start = block.transform.position;
        Assert.IsTrue(block.CanSlide(Vector3.left), "the corridor is open to the west");
        Assert.IsTrue(block.TrySlide(Vector3.left));
        Assert.IsTrue(block.IsMoving);
        Assert.IsFalse(block.TrySlide(Vector3.left), "one push at a time");
        yield return new WaitForSeconds(0.6f);
        Assert.IsFalse(block.IsMoving);
        Assert.AreEqual(start.x - PushBlock.TileSize, block.transform.position.x, 0.01f);
        Assert.AreEqual(1, block.Pushes);
        Assert.AreEqual(1, GameSession.GetCounter("blocks_pushed"));

        // Straight on into the closet: the block keeps going until it meets the far wall.
        int pushes = 1;
        for (int i = 0; i < 10; i++)
        {
            if (!block.TrySlide(Vector3.left)) break;
            pushes++;
            yield return new WaitForSeconds(0.5f);
        }
        Assert.AreEqual(8, pushes, "five pushes take it into the closet (and clear of the corridor), then it slides on to the far wall");
        Assert.IsFalse(block.CanSlide(Vector3.left));
    }

    [UnityTest]
    public IEnumerator TheMittsLetTheHeroShoveABlockButOnlyOnceTheyHaveThem()
    {
        yield return Load("Mines1");
        var player = LevelBootstrap.Current.Player;
        var push = player.GetComponent<PushAbility>();
        Assert.IsNotNull(push);
        var block = Object.FindAnyObjectByType<PushBlock>();
        var hint = block.GetComponentInChildren<HintBubble>();
        Assert.IsNotNull(hint, "a thought bubble tells you to come back later");
        Assert.AreEqual(Abilities.MoleMitts, hint.Ability);

        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = block.transform.position + Vector3.right * 1.5f; // just east of it
        cc.enabled = true;
        Assert.AreSame(block, push.BlockAhead(Vector3.left));
        Assert.IsNull(push.BlockAhead(Vector3.right), "not the one behind you");
        Assert.IsNull(push.BlockAhead(Vector3.forward), "nor one off to the side");
    }

    [UnityTest]
    public IEnumerator SoftDirtNeedsTheMittsThenDigsAwayAndStaysDug()
    {
        yield return Load("Mines1");
        int moundsBefore = Object.FindObjectsByType<SoftDirt>().Length;
        var dirt = Object.FindObjectsByType<SoftDirt>().First();
        Assert.IsFalse(dirt.CanInteract, "no digging with bare paws");
        GiveMitts();
        Assert.IsTrue(dirt.CanInteract);
        Assert.AreEqual("Dig", dirt.Prompt);
        dirt.Dig();
        Assert.IsTrue(dirt.IsDug);
        Assert.IsFalse(dirt.CanInteract);
        Assert.AreEqual(1, GameSession.GetCounter(SoftDirt.DugCounter));
        yield return new WaitForSeconds(0.7f);
        Assert.IsTrue(dirt == null, "the mound crumbles away");

        yield return Load("Mines1");
        Assert.AreEqual(moundsBefore - 1, Object.FindObjectsByType<SoftDirt>().Length, "it stays dug; the other mound is still there");
    }

    [UnityTest]
    public IEnumerator ADirtMoundHidesAHeartPieceUntilYouDigIt()
    {
        yield return Load("Mines2");
        GiveMitts();
        var dirt = Object.FindAnyObjectByType<SoftDirt>();
        var buried = Object.FindObjectsByType<Collectible>(FindObjectsInactive.Include).Single(c => c.Type == Collectible.Kind.HeartPiece);
        Assert.IsFalse(buried.gameObject.activeSelf, "buried");
        dirt.Dig();
        yield return null;
        Assert.IsTrue(buried.gameObject.activeSelf, "unearthed");
        int pieces = Collectible.HeartPieces;
        buried.Collect();
        Assert.AreEqual(pieces + 1, Collectible.HeartPieces);

        // Back later: the mound is gone, and the heart piece doesn't come back.
        yield return Load("Mines2");
        Assert.AreEqual(0, Object.FindObjectsByType<SoftDirt>().Length);
        Assert.AreEqual(0, Object.FindObjectsByType<Collectible>(FindObjectsInactive.Include).Count(c => c.Type == Collectible.Kind.HeartPiece));
    }

    [UnityTest]
    public IEnumerator TheEggChamberOpensOnceTheBlockIsPushedAllTheWayIn()
    {
        yield return Load("Mines3");
        GiveMitts();
        var block = Object.FindAnyObjectByType<PushBlock>();
        var egg = Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == "dragon_egg_mines");
        Assert.IsNotNull(egg);
        int pushes = 0;
        for (int i = 0; i < 12; i++)
        {
            if (!block.TrySlide(Vector3.forward)) break; // north
            pushes++;
            yield return new WaitForSeconds(0.5f);
        }
        Assert.GreaterOrEqual(pushes, 5, "up the corridor and into the chamber");
        var to = block.transform.position - egg.transform.position;
        to.y = 0f;
        Assert.Greater(to.magnitude, 1f, "it doesn't land on the egg");
    }

    // ---------- The Crystal Golem ----------

    [UnityTest]
    public IEnumerator TheGolemCanOnlyBeHurtWhileHisCrystalsGlow()
    {
        yield return Load("Mines4", freezeEnemies: false);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        Assert.AreEqual("The Crystal Golem", boss.BossName);
        var crystals = boss.GetComponent<GolemCrystals>();
        Assert.IsNotNull(crystals);
        var health = boss.Health;

        crystals.SetGlowing(false, announce: false);
        int before = health.Current;
        health.TakeDamage(5);
        Assert.AreEqual(before, health.Current, "dark crystals: his hide shrugs it off");

        crystals.SetGlowing(true, announce: false);
        health.TakeDamage(5);
        Assert.AreEqual(before - 5, health.Current, "glowing crystals: it hurts");
    }

    [UnityTest]
    public IEnumerator TheGolemsLightGoesOutWithHisCrystalsAndComesBack()
    {
        yield return Load("Mines4", freezeEnemies: false);
        var crystals = Object.FindAnyObjectByType<GolemCrystals>();
        crystals.SetGlowing(true, announce: false);
        var ambient = RenderSettings.ambientLight;
        crystals.SetGlowing(false, announce: false);
        Assert.Less(RenderSettings.ambientLight.grayscale, ambient.grayscale, "the cave goes dark");
        crystals.SetGlowing(true, announce: false);
        Assert.AreEqual(ambient.grayscale, RenderSettings.ambientLight.grayscale, 0.001f, "and lights up again");
        Assert.Greater(crystals.GlowSeconds, 3f, "enough time to land a few hits");
    }

    [UnityTest]
    public IEnumerator BeatingTheGolemRevealsTheMittsAndTheTopazAndTheyAreTreasures()
    {
        GameSession.Flags.Add("thanked:digby");
        yield return Load("Mines4", freezeEnemies: false);
        var player = LevelBootstrap.Current.Player;
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var rewards = Object.FindObjectsByType<BossReward>(FindObjectsInactive.Include);
        Assert.AreEqual(2, rewards.Length);
        Assert.IsTrue(rewards.All(r => !r.IsShown), "the prizes wait");

        Object.FindAnyObjectByType<GolemCrystals>().SetGlowing(true, announce: false);
        boss.Health.TakeDamage(999);
        yield return null;
        Assert.IsTrue(rewards.All(r => r.IsShown));
        Assert.IsTrue(GameSession.Flags.Contains("cleared:Mines4"));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("golem")));

        foreach (var id in new[] { Abilities.MoleMitts, "topaz" })
            StringAssert.Contains("", Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == id).Interact(player));
        Assert.IsTrue(Abilities.Has(Abilities.MoleMitts));
        Assert.IsTrue(player.GetComponent<Inventory>().HasKeyItem("topaz"));
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("golem")));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("egg_mines")), "Amethyra wants the egg now");
    }

    [UnityTest]
    public IEnumerator DigbyCongratulatesYouWhenTheGolemIsBeaten()
    {
        GameSession.Flags.Add("met:Digby");
        GameSession.Flags.Add("thanked:digby");
        GameSession.Flags.Add("digby:golem");
        GameSession.Flags.Add("cleared:Mines4");
        yield return Load("Mines1");
        var digby = NpcNamed("Digby");
        StringAssert.Contains("Mole Mitts", digby.Next().lines.Select(l => l.text).Aggregate((a, b) => a + " " + b));
        TalkTo(digby);
        Assert.IsTrue(GameSession.Flags.Contains("digby:cheered"));
    }

    // ---------- The things to find ----------

    [UnityTest]
    public IEnumerator TheEasterEggsAreAllThere()
    {
        yield return Load("Mines1");
        var player = LevelBootstrap.Current.Player;
        var names = Object.FindObjectsByType<HouseFixture>().Select(f => f.name).ToList();
        Assert.IsTrue(names.Any(n => n.StartsWith("MineSign")), "the no-bats sign");
        Assert.IsTrue(names.Any(n => n.StartsWith("MoleDoor")), "the tiny mole door");
        yield return Load("Mines2");
        var rock = Object.FindObjectsByType<HouseFixture>().First(f => f.name.StartsWith("PetRock"));
        rock.Interact(player);
        Assert.IsTrue(GameSession.Flags.Contains("found:petrock"));
    }

    [Test]
    public void TheMinesHaveTheirQuests()
    {
        foreach (var id in new[] { "moles", "golem", "egg_mines" }) Assert.IsNotNull(QuestCatalog.Find(id), id);
        Assert.AreEqual(QuestState.Hidden, QuestCatalog.StateOf(QuestCatalog.Find("moles")));
    }

    [UnityTest]
    public IEnumerator BatsAndPebblinsAreInTheMines()
    {
        yield return Load("Mines1", freezeEnemies: false);
        var names = Object.FindObjectsByType<EnemyAI>().Select(e => e.name).ToList();
        Assert.IsTrue(names.Any(n => n.StartsWith("Bat")), "bats");
        Assert.IsTrue(names.Any(n => n.StartsWith("Pebblin")), "pebblins");
    }
}
