using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for Puddlebrook Lake: the four rooms, Captain Clamshell's fishing lesson and the fishing game, the Bubble
// Charm (swimming), the crabs that only walk sideways, and King Crabbington, who can only be hurt while he peeks out of his shell.
public class LakeTests
{
    private string folder;

    [SetUp]
    public void UseTemporaryFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "TidecrownLakeTests_" + System.Guid.NewGuid().ToString("N"));
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
        Physics.IgnoreLayerCollision(LevelMap.SwimmerLayer, LevelMap.WaterLayer, false);
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

    private static void Teleport(GameObject player, Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = to;
        cc.enabled = true;
    }

    // A land tile (in world coordinates) with water directly north of it, and the water's tile centre.
    private static (Vector3 land, Vector3 water) ShoreWithWaterToTheNorth()
    {
        var map = Object.FindAnyObjectByType<LevelMap>();
        float tile = map.TileSize;
        for (int row = map.Height - 1; row >= 1; row--)
        {
            for (int col = 4; col < map.Width - 4; col++)
            {
                var land = new Vector3(col * tile, 0f, (map.Height - 1 - row) * tile);
                var north = land + Vector3.forward * tile;
                var far = land + Vector3.forward * tile * 2;
                char t = map.TileAt(land);
                if ((t == '.' || t == ',') && LevelMap.IsWater(map.TileAt(north)) && LevelMap.IsWater(map.TileAt(far))) return (land, north);
            }
        }
        Assert.Fail("no shore found");
        return default;
    }

    // ---------- The rooms ----------

    [UnityTest]
    public IEnumerator AllFourRoomsLoadAndConnectUp()
    {
        foreach (var room in new[] { "Lake1", "Lake2", "Lake3", "Lake4" })
        {
            yield return Load(room);
            Assert.AreEqual(room, SceneManager.GetActiveScene().name);
            Assert.IsTrue(GameSession.Flags.Contains("visited:" + room));
            Assert.GreaterOrEqual(Object.FindObjectsByType<RoomEdge>().Length, 1, room + " has a way out");
        }
        yield return Load("Lake1");
        CollectionAssert.AreEquivalent(new[] { "Dungeon", "Lake2" }, Object.FindObjectsByType<RoomEdge>().Select(d => d.TargetScene));
        yield return Load("Dungeon");
        CollectionAssert.Contains(Object.FindObjectsByType<RoomEdge>().Select(d => d.TargetScene).ToList(), "Lake1");
    }

    [UnityTest]
    public IEnumerator TheShoreHasClamshellAFountainAndFishingSpots()
    {
        yield return Load("Lake1");
        Assert.IsNotNull(NpcNamed("Captain Clamshell"));
        Assert.IsNotNull(Object.FindAnyObjectByType<WakeFountain>());
        Assert.GreaterOrEqual(Object.FindObjectsByType<FishingSpot>().Length, 2);
        Assert.GreaterOrEqual(Object.FindObjectsByType<HintBubble>().Count(h => h.Ability == Abilities.BubbleCharm), 2, "come-back-later bubbles for the islands");
        Assert.GreaterOrEqual(Object.FindObjectsByType<Collectible>().Length, 2, "a heart piece and a star shard on the islands");
    }

    // ---------- Fishing ----------

    [UnityTest]
    public IEnumerator FishingTakesACastABiteAndAWellTimedPress()
    {
        yield return Load("Lake1");
        var player = LevelBootstrap.Current.Player;
        var spot = Object.FindObjectsByType<FishingSpot>().First();
        Teleport(player, spot.transform.position + Vector3.back * 1.5f);
        Assert.AreEqual(FishingSpot.Phase.Idle, spot.State);
        Assert.AreEqual("Cast your line", spot.Prompt);

        StringAssert.Contains("cast your line", spot.Interact(player).ToLowerInvariant());
        Assert.AreEqual(FishingSpot.Phase.Waiting, spot.State);
        // Too soon: pulling in early scares it off.
        StringAssert.Contains("Too soon", spot.Interact(player));
        Assert.AreEqual(FishingSpot.Phase.Idle, spot.State);
        Assert.AreEqual(0, GameSession.GetCounter(FishingSpot.CaughtCounter));

        // Cast again; the bite comes; press in time.
        spot.Interact(player);
        spot.Bite();
        Assert.AreEqual(FishingSpot.Phase.Bite, spot.State);
        Assert.AreEqual("Reel in!", spot.Prompt);
        int gold = GameSession.Progress.Gold;
        StringAssert.Contains("You caught a", spot.Interact(player));
        Assert.AreEqual(FishingSpot.Phase.Idle, spot.State);
        Assert.AreEqual(1, GameSession.GetCounter(FishingSpot.CaughtCounter));
        Assert.Greater(GameSession.Progress.Gold, gold, "fish sell for coins");
    }

    [UnityTest]
    public IEnumerator AFishThatIsntReeledInInTimeGetsAway()
    {
        yield return Load("Lake1");
        var player = LevelBootstrap.Current.Player;
        var spot = Object.FindObjectsByType<FishingSpot>().First();
        Teleport(player, spot.transform.position + Vector3.back * 1.5f);
        spot.Interact(player);
        spot.Bite();
        yield return new WaitForSeconds(spot.BiteWindow + 0.4f);
        Assert.AreEqual(FishingSpot.Phase.Idle, spot.State, "it got away");
        Assert.AreEqual(0, GameSession.GetCounter(FishingSpot.CaughtCounter));
    }

    [UnityTest]
    public IEnumerator WalkingAwayPutsTheLineAway()
    {
        yield return Load("Lake1");
        var player = LevelBootstrap.Current.Player;
        var spot = Object.FindObjectsByType<FishingSpot>().First();
        Teleport(player, spot.transform.position + Vector3.back * 1.5f);
        spot.Interact(player);
        Teleport(player, spot.transform.position + Vector3.back * 12f);
        yield return null;
        yield return null;
        Assert.AreEqual(FishingSpot.Phase.Idle, spot.State);
    }

    [UnityTest]
    public IEnumerator ThreeFishEarnTheFishingRod()
    {
        yield return Load("Lake1");
        var player = LevelBootstrap.Current.Player;
        var clam = NpcNamed("Captain Clamshell");
        TalkTo(clam);
        Assert.IsTrue(GameSession.Flags.Contains("met:Clamshell"));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("fishing")));

        var spot = Object.FindObjectsByType<FishingSpot>().First();
        Teleport(player, spot.transform.position + Vector3.back * 1.5f);
        for (int i = 0; i < QuestCatalog.FishingGoal; i++)
        {
            spot.Interact(player);
            spot.Bite();
            spot.Interact(player);
        }
        Assert.AreEqual(3, GameSession.GetCounter(FishingSpot.CaughtCounter));
        StringAssert.Contains("(3/3)", QuestCatalog.StepText(QuestCatalog.Find("fishing").Steps[0]));

        int gold = GameSession.Progress.Gold;
        TalkTo(clam);
        Assert.IsTrue(GameSession.Flags.Contains("thanked:clamshell"));
        Assert.IsTrue(Abilities.Has(FishingSpot.RodItem), "the rod is a treasure");
        Assert.GreaterOrEqual(GameSession.Progress.Gold, gold + 20);
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("fishing")));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("king")));
        Assert.Greater(spot.BiteWindow, 1.0f, "the rod gives a longer window");
    }

    // ---------- Swimming ----------

    [UnityTest]
    public IEnumerator WithoutTheCharmTheWaterIsAWall()
    {
        yield return Load("Lake1");
        var player = LevelBootstrap.Current.Player;
        var (land, water) = ShoreWithWaterToTheNorth();
        Teleport(player, land);
        yield return null;
        var cc = player.GetComponent<CharacterController>();
        for (int i = 0; i < 40; i++) { cc.Move(Vector3.forward * 0.3f + Vector3.down * 0.1f); yield return null; }
        Assert.IsFalse(LevelMap.IsWater(Object.FindAnyObjectByType<LevelMap>().TileAt(player.transform.position)), "stopped at the bank");
        Assert.IsFalse(player.GetComponent<PlayerController>().IsSwimming);
    }

    [UnityTest]
    public IEnumerator WithTheCharmTheHeroSwimsAndCannotCast()
    {
        GameSession.Inventory.KeyItems.Add(Abilities.BubbleCharm);
        yield return Load("Lake1");
        var player = LevelBootstrap.Current.Player;
        var movement = player.GetComponent<PlayerController>();
        var swim = player.GetComponent<SwimAbility>();
        Assert.IsNotNull(swim);
        var map = Object.FindAnyObjectByType<LevelMap>();
        var (land, water) = ShoreWithWaterToTheNorth();
        Teleport(player, land);
        yield return null;
        Assert.IsFalse(movement.IsSwimming, "on the bank");

        var cc = player.GetComponent<CharacterController>();
        for (int i = 0; i < 40; i++) { cc.Move(Vector3.forward * 0.3f + Vector3.down * 0.1f); yield return null; }
        Assert.IsTrue(LevelMap.IsWater(map.TileAt(player.transform.position)), "out in the water");
        Assert.IsTrue(movement.IsSwimming);
        Assert.IsTrue(swim.IsSwimming);
        Assert.AreEqual(1, swim.Swims);
        Assert.AreEqual(LevelMap.SwimmerLayer, player.layer);

        // Back to shore: it stops again.
        for (int i = 0; i < 80; i++) { cc.Move(Vector3.back * 0.3f + Vector3.down * 0.1f); yield return null; }
        Assert.IsFalse(movement.IsSwimming);
    }

    [UnityTest]
    public IEnumerator WaterOnTheIslandsRimStaysAWallEvenForASwimmer()
    {
        yield return Load("Cove");
        var rimWalls = Object.FindObjectsByType<BoxCollider>().Count(c => c.gameObject.layer == LevelMap.WaterRimLayer);
        var innerWalls = Object.FindObjectsByType<BoxCollider>().Count(c => c.gameObject.layer == LevelMap.WaterLayer);
        Assert.Greater(rimWalls, 20, "the sea's edge");
        Assert.Greater(innerWalls, 20, "and its middle");
        yield return Load("Lake1");
        Assert.AreEqual(0, Object.FindObjectsByType<BoxCollider>().Count(c => c.gameObject.layer == LevelMap.WaterRimLayer), "the lake never touches the edge");
    }

    [UnityTest]
    public IEnumerator TheCoveHasASwimmersIsland()
    {
        yield return Load("Cove");
        Assert.AreEqual(1, Object.FindObjectsByType<Collectible>().Count(c => c.Type == Collectible.Kind.HeartPiece));
        Assert.IsTrue(Object.FindObjectsByType<HintBubble>().Any(h => h.Ability == Abilities.BubbleCharm));
    }

    // ---------- Crabs and jellyfish ----------

    [UnityTest]
    public IEnumerator CrabsOnlyWalkSidewaysAcrossTheScreen()
    {
        yield return Load("Lake1", freezeEnemies: false);
        foreach (var e in Object.FindObjectsByType<EnemyAI>().Where(e => !e.name.StartsWith("Crab"))) e.enabled = false;
        var crab = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Crab"));
        foreach (var other in Object.FindObjectsByType<EnemyAI>().Where(e => e != crab)) other.enabled = false;
        var field = typeof(EnemyAI).GetField("sidewaysOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsTrue((bool)field.GetValue(crab));

        // Put the hero up the screen and to the right of the crab, in the open; the crab can only close the sideways gap.
        var player = LevelBootstrap.Current.Player;
        var cam = Camera.main;
        var right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
        var up = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        var shore = ShoreWithWaterToTheNorth().land + Vector3.back * 6f;
        var cc = crab.GetComponent<CharacterController>();
        cc.enabled = false; crab.transform.position = shore + Vector3.up; cc.enabled = true;
        Teleport(player, shore + right * 5f + up * 5f);
        var start = crab.transform.position;
        yield return new WaitForSeconds(0.8f);
        var moved = crab.transform.position - start;
        moved.y = 0f;
        Assert.Greater(Vector3.Dot(moved, right), 0.8f, "it scuttled towards you, across the screen");
        Assert.Less(Mathf.Abs(Vector3.Dot(moved, up)), 0.35f, "but never up it");
    }

    [UnityTest]
    public IEnumerator JellyfishSpitBubblesFromAFarAndCrabsAreInTheLake()
    {
        yield return Load("Lake1", freezeEnemies: false);
        var names = Object.FindObjectsByType<EnemyAI>().Select(e => e.name).ToList();
        Assert.IsTrue(names.Any(n => n.StartsWith("Crab")));
        Assert.IsTrue(names.Any(n => n.StartsWith("Jelly")));
    }

    // ---------- King Crabbington ----------

    [UnityTest]
    public IEnumerator TheKingCanOnlyBeHurtWhilePeekingOutOfHisShell()
    {
        yield return Load("Lake4", freezeEnemies: false);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        Assert.AreEqual("King Crabbington", boss.BossName);
        var shell = boss.GetComponent<ShellCycle>();
        Assert.IsNotNull(shell);
        Assert.IsNotNull(boss.GetComponent<TideWaves>());
        int before = boss.Health.Current;

        shell.SetPeeking(false, announce: false);
        Assert.IsTrue(boss.HoldSpecials, "no slamming from inside the shell");
        boss.Health.TakeDamage(6);
        Assert.AreEqual(before, boss.Health.Current, "spells tink off the shell");

        shell.SetPeeking(true, announce: false);
        Assert.IsFalse(boss.HoldSpecials);
        boss.Health.TakeDamage(6);
        Assert.AreEqual(before - 6, boss.Health.Current, "peeking out: it hurts");
        Assert.Greater(shell.PeekSeconds, 3f);
    }

    [UnityTest]
    public IEnumerator TheTideKnocksBackAHeroWhoStaysInItsPath()
    {
        yield return Load("Lake4", freezeEnemies: false);
        var player = LevelBootstrap.Current.Player;
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var tide = boss.GetComponent<TideWaves>();
        boss.GetComponent<EnemyAI>().enabled = false;
        boss.GetComponent<ShellCycle>().SetPeeking(false, announce: false);
        Teleport(player, boss.transform.position + Vector3.back * 6f);
        int hearts = player.GetComponent<Health>().Current;
        tide.StartWave();
        Assert.IsTrue(tide.IsWaving);
        yield return new WaitForSeconds(5.5f);
        Assert.IsFalse(tide.IsWaving);
        Assert.AreEqual(1, tide.WavesHit, "standing in the red band, you get splashed");
        Assert.Less(player.GetComponent<Health>().Current, hearts);
    }

    [UnityTest]
    public IEnumerator BeatingTheKingRevealsTheCharmAndTheAquamarine()
    {
        GameSession.Flags.Add("thanked:clamshell");
        yield return Load("Lake4", freezeEnemies: false);
        var player = LevelBootstrap.Current.Player;
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var rewards = Object.FindObjectsByType<BossReward>(FindObjectsInactive.Include);
        Assert.AreEqual(2, rewards.Length);
        Assert.IsTrue(rewards.All(r => !r.IsShown));

        boss.GetComponent<ShellCycle>().SetPeeking(true, announce: false);
        boss.Health.TakeDamage(999);
        yield return null;
        Assert.IsTrue(rewards.All(r => r.IsShown));
        Assert.IsTrue(GameSession.Flags.Contains("cleared:Lake4"));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("king")));

        foreach (var id in new[] { Abilities.BubbleCharm, "aquamarine" })
            Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == id).Interact(player);
        Assert.IsTrue(Abilities.Has(Abilities.BubbleCharm));
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("king")));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("egg_lake")));
    }

    [UnityTest]
    public IEnumerator TheLakeEggWaitsOnTheMiddleIslandForASwimmer()
    {
        yield return Load("Lake2");
        var egg = Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == "dragon_egg_lake");
        var map = Object.FindAnyObjectByType<LevelMap>();
        // It stands on land that's surrounded by water: nothing but water to its north, south, east and west for a couple of tiles.
        var p = egg.transform.position;
        foreach (var dir in new[] { Vector3.forward, Vector3.back })
            Assert.IsTrue(LevelMap.IsWater(map.TileAt(p + dir * map.TileSize * 3)), "water all round the island");
        Assert.IsTrue(Object.FindObjectsByType<HintBubble>().Any(h => h.Ability == Abilities.BubbleCharm));
    }

    [Test]
    public void TheLakeHasItsQuests()
    {
        foreach (var id in new[] { "fishing", "king", "egg_lake" }) Assert.IsNotNull(QuestCatalog.Find(id), id);
    }
}
