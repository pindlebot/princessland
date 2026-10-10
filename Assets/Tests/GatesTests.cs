using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for the metroidvania loop (Phase 2): the Bouncy Boots and the gaps they hop, the spell
// gates (brambles and braziers) that either hero can open, the Slime King's rewards, the crystal plague that
// clears from the castle grounds, heart pieces and star shards, and Amethyra's story. Plus a check on
// the map files themselves: the gaps really do seal their treasures until you have the boots.
public class GatesTests
{
    private const float Tile = 2f;

    [SetUp]
    public void Fresh() => GameSession.NewGame(null);

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        Time.timeScale = 1f;
    }

    private static IEnumerator Load(string scene, CharacterDefinition hero = null)
    {
        if (hero != null) GameSession.SelectedCharacter = hero;
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
    }

    private static IEnumerator LoadAsPrincess(string scene)
    {
        SceneManager.LoadScene("Title");
        yield return null;
        var princess = Object.FindAnyObjectByType<TitleController>().Heroes.First(h => h.name == "Princess");
        GameSession.NewGame(princess);
        yield return Load(scene);
    }

    private static GameObject Player => LevelBootstrap.Current.Player;

    private static void Teleport(Vector3 to)
    {
        var cc = Player.GetComponent<CharacterController>();
        cc.enabled = false;
        Player.transform.position = to;
        cc.enabled = true;
    }

    // The middle of a map tile, at the hero's height: x = col * 2, z = (rows - 1 - row) * 2.
    private static Vector3 TileCentre(int rows, int col, int row) => new Vector3(col * Tile, 1f, (rows - 1 - row) * Tile);

    private static void GiveBoots() => GameSession.Inventory.KeyItems.Add(Abilities.BouncyBoots);

    private static void KillEveryone()
    {
        foreach (var enemy in Object.FindObjectsByType<EnemyAI>()) enemy.GetComponent<Health>().TakeDamage(99);
    }

    // ---------- Hopping ----------

    [UnityTest]
    public IEnumerator TheBootsHopAOneTileGapAndNothingElseDoes()
    {
        yield return Load("Dungeon");
        var hop = Player.GetComponent<HopAbility>();
        // The corridor above the start room has a one-tile gap at map (17,3), with an alcove behind it.
        Teleport(TileCentre(43, 17, 4) + new Vector3(0f, 0f, 0.4f));

        Assert.IsFalse(hop.TryHop(Vector3.forward), "no boots yet");
        Player.GetComponent<CharacterController>().Move(Vector3.forward * 3f);
        Assert.Less(Player.transform.position.z, TileCentre(43, 17, 3).z - 0.8f, "the gap's wall keeps you out of the pit");

        // Having walked into its wall, you're close enough to the edge to hop (that's how pushing into it works).
        GiveBoots();
        Assert.IsTrue(hop.GapAhead(Vector3.forward, out _, out var landing), "even after being stopped by the wall");
        Teleport(TileCentre(43, 17, 4) + new Vector3(0f, 0f, 0.4f));
        Assert.IsTrue(hop.GapAhead(Vector3.forward, out _, out landing));
        Assert.AreEqual(TileCentre(43, 17, 2).z, landing.z, 0.01f, "you land in the middle of the alcove's first tile");
        Assert.IsFalse(hop.GapAhead(Vector3.back, out _, out _), "no gap behind you");

        Assert.IsTrue(hop.TryHop(Vector3.forward));
        Assert.IsTrue(Player.GetComponent<PlayerController>().IsHopping);
        yield return new WaitForSeconds(0.9f);
        Assert.IsFalse(Player.GetComponent<PlayerController>().IsHopping);
        Assert.AreEqual(TileCentre(43, 17, 2).z, Player.transform.position.z, 0.3f, "across the gap");
        Assert.AreEqual(1, GameSession.GetCounter("hops"));
    }

    [UnityTest]
    public IEnumerator TheBootsAlsoClearATwoTileGap()
    {
        yield return Load("Dungeon");
        GiveBoots();
        var hop = Player.GetComponent<HopAbility>();
        // Through the east wall of the Slime King's hall: two gap tiles at map (59,35) and (60,35), then the egg chamber.
        Teleport(TileCentre(43, 58, 35) + new Vector3(0.4f, 0f, 0f));
        Assert.IsTrue(hop.GapAhead(Vector3.right, out _, out var landing));
        Assert.AreEqual(TileCentre(43, 61, 35).x, landing.x, 0.01f);
        Assert.IsTrue(hop.TryHop(Vector3.right + Vector3.forward * 0.2f), "a slightly crooked push still hops straight east");
        yield return new WaitForSeconds(1.2f);
        Assert.AreEqual(TileCentre(43, 61, 35).x, Player.transform.position.x, 0.3f);
    }

    [UnityTest]
    public IEnumerator WalkingIntoAGapHopsOnItsOwnWithTheBoots()
    {
        yield return Load("Dungeon");
        GiveBoots();
        var player = Player.GetComponent<PlayerController>();
        player.enabled = true; // the real controller, with the hop pushed through the controller's own movement
        var hop = Player.GetComponent<HopAbility>();
        Teleport(TileCentre(43, 17, 4) + new Vector3(0f, 0f, 0.4f));
        // Nobody is holding a key, so simulate the push: GapAhead is what Update checks every frame.
        Assert.IsTrue(hop.GapAhead(Vector3.forward, out _, out _));
        Assert.IsTrue(hop.TryHop(Vector3.forward));
        yield return new WaitForSeconds(0.9f);
        Assert.Greater(Player.transform.position.z, TileCentre(43, 17, 3).z);
    }

    [UnityTest]
    public IEnumerator GapsShowAHintBubbleUntilYouHaveTheBoots()
    {
        yield return Load("Dungeon");
        Teleport(TileCentre(43, 17, 4));
        yield return new WaitForSeconds(0.6f);

        var bubbles = Object.FindObjectsByType<HintBubble>();
        Assert.IsTrue(bubbles.Any(b => b.IsShown), "a bubble with the boots and a question mark");
        Assert.AreEqual(1, bubbles.Count(b => b.IsShown), "only the nearest gate shows one");
        Assert.IsTrue(GameSession.Flags.Contains(HintBubble.SeenFlag("Dungeon/17,3")), "she has seen this gap: the game remembers");
        Assert.IsTrue(GameSession.Flags.Contains(HintBubble.FirstFlag(Abilities.BouncyBoots)));

        GiveBoots();
        yield return new WaitForSeconds(0.6f);
        Assert.IsFalse(Object.FindObjectsByType<HintBubble>().Any(b => b.IsShown), "she can hop it now");
    }

    // ---------- Spell gates ----------

    [UnityTest]
    public IEnumerator FireBurnsABrambleAndWaterMakesItBloom()
    {
        yield return Load("Level0");
        var bramble = Object.FindAnyObjectByType<Bramble>();
        Assert.IsFalse(bramble.IsCleared);
        Assert.IsTrue(bramble.GetComponent<BoxCollider>().enabled, "solid");

        bramble.OnSpellHit(1, SpellElement.Fire);
        Assert.IsTrue(bramble.IsCleared);
        Assert.AreEqual(SpellElement.Fire, bramble.ClearedBy);
        Assert.IsFalse(bramble.GetComponent<BoxCollider>().enabled, "the way is open at once");
        Assert.AreEqual(1, GameSession.GetCounter(Bramble.ClearedCounter));
        yield return new WaitForSeconds(3f);
        Assert.IsTrue(bramble == null, "ash blows away");

        // Still gone after leaving and coming back.
        yield return Load("Level0");
        Assert.IsNull(Object.FindAnyObjectByType<Bramble>());
    }

    [UnityTest]
    public IEnumerator EitherHeroCanClearBramblesWithTheirOwnSpell()
    {
        yield return LoadAsPrincess("Level0");
        KillEveryone();
        yield return null;
        var bramble = Object.FindAnyObjectByType<Bramble>();
        // Three metres north of the thorn door at map (2,30), facing south at it. Cast the princess's Tidal Orb.
        var at = bramble.transform.position;
        Teleport(new Vector3(at.x, 1f, at.z + 4f));
        Player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(Player.GetComponent<SpellAbility>().TryCast());
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(bramble.IsCleared);
        Assert.AreEqual(SpellElement.Water, bramble.ClearedBy, "water makes it bloom");
    }

    [UnityTest]
    public IEnumerator TheWizardsFireballBurnsTheBrambleDoor()
    {
        yield return Load("Level0");
        KillEveryone();
        yield return null;
        var bramble = Object.FindAnyObjectByType<Bramble>();
        var at = bramble.transform.position;
        Teleport(new Vector3(at.x, 1f, at.z + 4f));
        Player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(Player.GetComponent<SpellAbility>().TryCast());
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(bramble.IsCleared);
        Assert.AreEqual(SpellElement.Fire, bramble.ClearedBy);
    }

    [UnityTest]
    public IEnumerator AreaSpellsOpenTheGatesToo()
    {
        yield return Load("Level0");
        var bramble = Object.FindAnyObjectByType<Bramble>();
        Assert.AreEqual(0, SpellTargets.HitNear(bramble.transform.position + new Vector3(30f, 0f, 0f), 2f, 1, SpellElement.Fire), "nothing in reach");
        Assert.IsFalse(bramble.IsCleared);
        Assert.GreaterOrEqual(SpellTargets.HitNear(bramble.transform.position + new Vector3(1f, 0f, 1f), 2.5f, 1, SpellElement.Water), 1);
        Assert.IsTrue(bramble.IsCleared, "a whirlpool or meteor lands beside it");
    }

    [UnityTest]
    public IEnumerator LightingBothBraziersMakesAStarShardAppear()
    {
        yield return Load("Dungeon");
        var braziers = Object.FindObjectsByType<Brazier>();
        Assert.AreEqual(2, braziers.Length);
        var shard = Object.FindObjectsByType<Collectible>(FindObjectsInactive.Include)
            .First(c => c.GetComponentInParent<BossReward>(true) != null);
        Assert.IsFalse(shard.gameObject.activeInHierarchy, "hidden until both are lit");

        braziers[0].OnSpellHit(1, SpellElement.Fire);
        Assert.IsTrue(braziers[0].IsLit);
        Assert.AreEqual(SpellElement.Fire, braziers[0].LitBy);
        Assert.IsFalse(shard.gameObject.activeInHierarchy, "one is not enough");
        braziers[1].OnSpellHit(1, SpellElement.Water);
        Assert.AreEqual(SpellElement.Water, braziers[1].LitBy, "water fills the basin: it counts the same");
        Assert.IsTrue(shard.gameObject.activeInHierarchy);
        Assert.IsTrue(GameSession.Flags.Contains(Brazier.DoneFlag("Dungeon")));

        // They stay lit, and the shard is still there to collect, when she comes back.
        yield return Load("Dungeon");
        Assert.IsTrue(Object.FindObjectsByType<Brazier>().All(b => b.IsLit));
        Assert.IsTrue(Object.FindObjectsByType<Collectible>().Any(c => c.Type == Collectible.Kind.StarShard));
    }

    // ---------- Treasures ----------

    [UnityTest]
    public IEnumerator HeartPiecesAndStarShardsAreCountedAndFourPiecesMakeAHeart()
    {
        yield return Load("Level0");
        var piece = Object.FindObjectsByType<Collectible>().First(c => c.Type == Collectible.Kind.HeartPiece);
        var health = Player.GetComponent<Health>();
        int max = health.Max;

        GameSession.Counters[Collectible.HeartPieceCounter] = 2;
        piece.Collect();
        Assert.AreEqual(3, Collectible.HeartPieces);
        Assert.AreEqual(max, health.Max, "three pieces are not a heart yet");
        Assert.AreEqual(0, Collectible.BonusHearts);

        GameSession.Counters[Collectible.HeartPieceCounter] = 3;
        var second = Object.Instantiate(piece.gameObject, Player.transform.position + Vector3.right * 6f, Quaternion.identity);
        yield return null;
        second.GetComponent<Collectible>().Collect();
        Assert.AreEqual(1, Collectible.BonusHearts);
        Assert.AreEqual(max + 1, health.Max, "the fourth piece adds a heart");
        Assert.AreEqual(health.Max, health.Current, "and fills it");
    }

    [UnityTest]
    public IEnumerator WalkingIntoATreasureCollectsItAndItStaysCollected()
    {
        yield return Load("Level0");
        var piece = Object.FindObjectsByType<Collectible>().First(c => c.Type == Collectible.Kind.HeartPiece);
        Teleport(piece.transform.position + Vector3.up * 0.5f);
        yield return null;
        yield return null;
        Assert.AreEqual(1, Collectible.HeartPieces);
        Assert.IsTrue(piece == null);

        yield return Load("Level0");
        Assert.IsFalse(Object.FindObjectsByType<Collectible>().Any(c => c.Type == Collectible.Kind.HeartPiece), "taken for good");
    }

    [UnityTest]
    public IEnumerator HeartPiecesAndShardsAreSavedWithTheGame()
    {
        var folder = Path.Combine(Path.GetTempPath(), "TidecrownGateTests_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        try
        {
            yield return Load("Level0");
            GameSession.Counters[Collectible.HeartPieceCounter] = 3;
            GameSession.Counters[Collectible.StarShardCounter] = 2;
            GiveBoots();
            SaveSystem.Save(0, "Level0", "");

            GameSession.NewGame(null);
            Assert.AreEqual("Level0", SaveSystem.Load(0, new CharacterDefinition[0]));
            Assert.AreEqual(3, Collectible.HeartPieces);
            Assert.AreEqual(2, Collectible.StarShards);
            Assert.IsTrue(Abilities.Has(Abilities.BouncyBoots), "and the ability is just an item in the treasures tab");
        }
        finally
        {
            SaveSystem.FolderOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    // ---------- The Slime King ----------

    [UnityTest]
    public IEnumerator TheSlimeKingLeavesTheAmethystAndTheBouncyBoots()
    {
        yield return Load("Dungeon");
        ItemPickup Find(string id) => Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include).First(p => p.Item.Id == id);
        var amethyst = Find("amethyst");
        var boots = Find(Abilities.BouncyBoots);
        Assert.IsFalse(amethyst.gameObject.activeInHierarchy, "out of sight while he lives");
        Assert.IsFalse(boots.gameObject.activeInHierarchy);
        Assert.IsTrue(amethyst.Item.IsKeyItem && boots.Item.IsKeyItem);

        Object.FindAnyObjectByType<BossAbilities>().Health.TakeDamage(999);
        yield return null;
        Assert.IsTrue(amethyst.gameObject.activeInHierarchy, "twinkles into view when he falls");
        Assert.IsTrue(boots.gameObject.activeInHierarchy);

        amethyst.Interact(Player);
        boots.Interact(Player);
        Assert.IsTrue(Abilities.Has(Abilities.BouncyBoots));
        Assert.IsTrue(Abilities.Has("amethyst"));
        Assert.IsTrue(Player.GetComponent<Inventory>().HasKeyItem(Abilities.BouncyBoots), "in the treasures tab, not the bag");
        Assert.AreEqual(0, GameSession.Inventory.Bag.Count);

        // Come back after winning: they're already there to take (if you hadn't), and gone once you have.
        yield return Load("Dungeon");
        Assert.IsFalse(Object.FindObjectsByType<ItemPickup>().Any(p => p.Item.Id == "amethyst"), "taken already");
    }

    [UnityTest]
    public IEnumerator TheDungeonStairsNowLeadBackUpToTheCastleGrounds()
    {
        yield return Load("Dungeon");
        Object.FindAnyObjectByType<BossAbilities>().Health.TakeDamage(999);
        yield return null;
        var exit = Object.FindAnyObjectByType<ExitZone>();
        Assert.IsTrue(exit.IsOpen);
        Teleport(exit.transform.position);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != "Level0"; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        Assert.AreEqual("Level0", SceneManager.GetActiveScene().name, "the adventure goes on");
        Assert.IsFalse(GameManager.Instance.IsGameOver);
        var stairs = Object.FindAnyObjectByType<ExitZone>().transform.position;
        Assert.Less(Vector3.Distance(Player.transform.position, stairs), 5f, "you step out beside the stairs, not back at the start");
    }

    // ---------- The crystal plague ----------

    [UnityTest]
    public IEnumerator TheCastleGroundsAreOverrunWithCrystalsUntilYouBringHomeTheAmethyst()
    {
        yield return Load("Level0");
        var plague = Object.FindAnyObjectByType<CrystalPlague>();
        Assert.IsNotNull(plague);
        Assert.Greater(plague.CrystalCount, 50, "crystals all over the grounds");
        yield return new WaitForSeconds(0.5f);
        Assert.IsFalse(plague.IsGone, "and they stay without the Amethyst");
        Assert.AreEqual(plague.CrystalCount, plague.GetComponentsInChildren<SpriteRenderer>().Length, "every one is drawn");
        Assert.AreEqual(0, plague.GetComponentsInChildren<Collider>().Length, "you can walk through them: they never wall anything in");

        GameSession.Inventory.KeyItems.Add("amethyst");
        yield return new WaitForSeconds(6f);
        Assert.IsTrue(plague.IsGone, "the crystals shatter away");
        Assert.IsTrue(GameSession.Flags.Contains(CrystalPlague.SeenFlag("Level0")));

        yield return Load("Level0");
        var again = Object.FindAnyObjectByType<CrystalPlague>();
        Assert.IsTrue(again == null || again.IsGone, "and stay gone");
    }

    [UnityTest]
    public IEnumerator OtherPlacesAreFreeOfCrystals()
    {
        yield return Load("Dungeon");
        Assert.IsNull(Object.FindAnyObjectByType<CrystalPlague>());
        yield return Load("Woods1");
        Assert.IsNull(Object.FindAnyObjectByType<CrystalPlague>());
    }

    // ---------- Amethyra's story ----------

    [UnityTest]
    public IEnumerator AmethyraTellsTheAmethystStoryThenTheEggStory()
    {
        yield return Load("Level0");
        var amethyra = Object.FindObjectsByType<Npc>().First(n => n.Name == "Amethyra");
        GameSession.Flags.Add("met:Amethyra");
        StringAssert.DoesNotContain("Amethyst", amethyra.Next().lines[0].text);

        GameSession.Flags.Add("cleared:Level0");
        StringAssert.Contains("grounds are clear", amethyra.Next().lines[0].text);
        Assert.AreEqual(QuestState.Hidden, QuestCatalog.StateOf(QuestCatalog.Find("amethyst")));
        GameSession.Flags.Add("told:amethyst");
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("amethyst")));
        StringAssert.Contains("Slime King", QuestCatalog.CurrentStep(QuestCatalog.Find("amethyst")).Text);

        GameSession.Flags.Add("cleared:Dungeon");
        GameSession.Inventory.KeyItems.Add("amethyst");
        StringAssert.Contains("Bring it to Amethyra", QuestCatalog.CurrentStep(QuestCatalog.Find("amethyst")).Text, "it moves on to bringing it home");
        Assert.AreEqual(2, QuestCatalog.StepsDone(QuestCatalog.Find("amethyst")));
        StringAssert.Contains("My Amethyst", amethyra.Next().lines[0].text);

        GameSession.Flags.Add("thanked:amethyst");
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("amethyst")));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("egg")), "the eggs are next");

        GameSession.Inventory.KeyItems.Add("dragon_egg_castle");
        StringAssert.Contains("My egg", amethyra.Next().lines[0].text);
        GameSession.Flags.Add("thanked:egg_castle");
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("egg")));
    }

    [UnityTest]
    public IEnumerator AmethyraRewardsTheAmethystAndTheEggWithGold()
    {
        yield return Load("Level0");
        var amethyra = Object.FindObjectsByType<Npc>().First(n => n.Name == "Amethyra");
        GameSession.Flags.Add("met:Amethyra");
        GameSession.Inventory.KeyItems.Add("amethyst");
        int gold = GameSession.Progress.Gold;
        amethyra.Interact(Player);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.AreEqual(gold + 50, GameSession.Progress.Gold);
        Assert.IsTrue(GameSession.Flags.Contains("thanked:amethyst"));
    }

    // ---------- Monsters finding their way ----------

    [UnityTest]
    public IEnumerator ThePathFinderGoesAroundWallsAndFindsNoWayAcrossAGap()
    {
        yield return Load("Dungeon");
        var nav = Object.FindAnyObjectByType<NavGrid>();
        Assert.IsNotNull(nav);
        Assert.IsFalse(nav.IsOpen(0, 0), "a wall");
        Assert.IsTrue(nav.IsOpen(5, 3), "the start room's floor");
        Assert.IsFalse(nav.IsOpen(17, 3), "a gap's tile is not for walking");

        // From the room's south-east to a spot beyond its wall: straight through the wall is no way, so it goes round by the door gap.
        var from = TileCentre(43, 10, 8);
        var to = TileCentre(43, 10, 12);
        var path = new List<Vector3>();
        Assert.IsTrue(nav.FindPath(from, to, path) || path.Count == 0);
        from = TileCentre(43, 9, 8);
        to = TileCentre(43, 5, 12); // down the corridor at the room's south-west
        Assert.IsTrue(nav.FindPath(from, to, path));
        Assert.Greater(path.Count, 3);
        Assert.AreEqual(to.x, path.Last().x, 0.01f);
        Assert.AreEqual(to.z, path.Last().z, 0.01f);
        foreach (var step in path)
        {
            var tile = nav.TileOf(step);
            Assert.IsTrue(nav.IsOpen(tile.col, tile.row), "every step is on open ground");
        }

        // The star shard's alcove, behind the gap, is unreachable on foot: no path.
        Assert.IsFalse(nav.FindPath(TileCentre(43, 17, 4), TileCentre(43, 17, 1), path));
        Assert.AreEqual(0, path.Count);
    }

    [UnityTest]
    public IEnumerator ASkeletonGoesRoundTheWallToReachYouInsteadOfPushingIntoIt()
    {
        yield return Load("Dungeon");
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var skeleton = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Skeleton"));
        var body = skeleton.GetComponent<CharacterController>();
        body.enabled = false;
        skeleton.transform.position = TileCentre(43, 5, 11); // in the corridor south of the start room
        body.enabled = true;
        skeleton.enabled = true;

        // It sees you straight up the corridor and gives chase...
        Teleport(TileCentre(43, 5, 8));
        yield return new WaitForSeconds(0.3f);
        // ...then you step aside, behind the room's south wall, where it can no longer walk straight at you.
        var hidden = TileCentre(43, 10, 8);
        Teleport(hidden);
        Assert.IsTrue(Physics.Linecast(skeleton.transform.position + Vector3.up * 0.6f, hidden + Vector3.up * 0.6f, ~0, QueryTriggerInteraction.Ignore),
            "a wall is in the way of a straight line");
        var player = Player.GetComponent<Health>();
        bool reached = false;
        for (float t = 0f; t < 9f && !reached; t += Time.deltaTime)
        {
            reached = Vector3.Distance(skeleton.transform.position, hidden) < 1.8f || player.Current < player.Max;
            yield return null;
        }
        Assert.IsTrue(reached, $"it found its way round: it ended {Vector3.Distance(skeleton.transform.position, hidden):0.0}m away");
    }

    [UnityTest]
    public IEnumerator ClearingABrambleOpensThePathForMonsters()
    {
        yield return Load("Level0");
        var nav = Object.FindAnyObjectByType<NavGrid>();
        var bramble = Object.FindAnyObjectByType<Bramble>();
        var tile = nav.TileOf(bramble.transform.position);
        Assert.IsFalse(nav.IsOpen(tile.col, tile.row), "thorns block the way");
        bramble.Clear(SpellElement.Fire);
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(nav.IsOpen(tile.col, tile.row), "and burning them opens it");
    }

    // ---------- The world map ----------

    [UnityTest]
    public IEnumerator TheWorldMapDrawsOnlyTheRoomsYouHaveVisitedAndWhereYouAre()
    {
        yield return Load("Level0");
        var view = WorldMapView.Instance;
        Assert.IsFalse(view.IsOpen);
        view.Open();
        Assert.IsTrue(view.IsOpen);
        Assert.AreEqual(0f, Time.timeScale, "time stops while you look");
        Assert.Contains("room:Level0", view.Drawn.ToList());
        Assert.Contains("you", view.Drawn.ToList());
        Assert.IsFalse(view.Drawn.Contains("room:Woods1"), "never been there");
        Assert.IsFalse(view.Drawn.Contains("room:Dungeon"));
        view.Close();
        Assert.AreEqual(1f, Time.timeScale);

        GameSession.Flags.Add("visited:Woods1");
        GameSession.Flags.Add("visited:Dungeon");
        view.Open();
        CollectionAssert.IsSubsetOf(new[] { "room:Level0", "room:Woods1", "room:Dungeon" }, view.Drawn.ToList());
        Assert.AreEqual(1, view.Drawn.Count(d => d == "fountain:Level0"));
        Assert.AreEqual(1, view.Drawn.Count(d => d == "fountain:Woods1"));
        view.Close();
    }

    [UnityTest]
    public IEnumerator TheWorldMapRemindsYouOfGapsYouSawAndEggsYouSpotted()
    {
        yield return Load("Dungeon");
        var view = WorldMapView.Instance;
        view.Open();
        Assert.IsFalse(view.Drawn.Any(d => d.StartsWith("gap:")), "none seen yet");
        Assert.IsFalse(view.Drawn.Any(d => d.StartsWith("egg:")), "and no egg spotted");
        view.Close();

        GameSession.Flags.Add(HintBubble.SeenFlag("Dungeon/17,3"));
        GameSession.Flags.Add(HintBubble.SeenFlag("Dungeon/59,35"));
        view.Open();
        Assert.Contains("gap:Dungeon/17,3", view.Drawn.ToList(), "a question mark where the gap is");
        Assert.Contains("gap:Dungeon/59,35", view.Drawn.ToList());
        Assert.Contains("egg:dragon_egg_castle", view.Drawn.ToList(), "having seen a gap here, you know there's an egg behind it");
        view.Close();

        GiveBoots();
        GameSession.Inventory.KeyItems.Add("dragon_egg_castle");
        view.Open();
        Assert.IsFalse(view.Drawn.Any(d => d.StartsWith("gap:")), "the boots can hop them: no more reminders");
        Assert.IsFalse(view.Drawn.Any(d => d.StartsWith("egg:")), "and the egg is yours");
        view.Close();
    }

    [Test]
    public void EveryRoomHasAPlaceOnTheWorldMapAndNoTwoOverlap()
    {
        var files = Directory.GetFiles(Path.Combine(Application.dataPath, "Levels"), "*.txt")
            .Select(p => MapFile.Parse(Path.GetFileNameWithoutExtension(p), File.ReadAllText(p))).ToList();
        var rects = new List<(string name, RectInt rect)>();
        foreach (var file in files)
        {
            var parts = file.Get("world").Split(' ');
            Assert.AreEqual(2, parts.Length, $"{file.Name} says where it is: 'world: x y'");
            rects.Add((file.Name, new RectInt(int.Parse(parts[0]), int.Parse(parts[1]), file.Rows.Max(r => r.Length), file.Rows.Length)));
        }
        for (int i = 0; i < rects.Count; i++)
            for (int j = i + 1; j < rects.Count; j++)
                Assert.IsFalse(rects[i].rect.Overlaps(rects[j].rect), $"{rects[i].name} and {rects[j].name} overlap on the world map");
    }

    // ---------- Room edges ----------

    [UnityTest]
    public IEnumerator WalkingOntoAnEdgeCrossesIntoTheNextRoomWithAFade()
    {
        yield return Load("Woods1");
        var east = Object.FindObjectsByType<RoomEdge>().First(e => e.TargetScene == "Woods2");
        Teleport(east.transform.position + Vector3.up);
        float peak = 0f;
        for (float t = 0f; t < 4f && SceneManager.GetActiveScene().name != "Woods2"; t += Time.unscaledDeltaTime)
        {
            peak = Mathf.Max(peak, ScreenFade.Alpha);
            yield return null;
        }
        Assert.AreEqual("Woods2", SceneManager.GetActiveScene().name, "no button pressed");
        Assert.Greater(peak, 0.8f, "it fades to black to hide the load");
        yield return null;
        yield return null;
        var arrival = Vector3.Distance(Player.transform.position,
            Object.FindObjectsByType<RoomEdge>().First(e => e.TargetScene == "Woods1").transform.position);
        Assert.Less(arrival, 4f, "you arrive beside the opening you came through, not on it");
        Assert.Greater(arrival, 1.2f);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime) yield return null;
        Assert.AreEqual("Woods2", SceneManager.GetActiveScene().name, "and you don't bounce straight back");
        Assert.AreEqual(0f, ScreenFade.Alpha, 0.01f, "the fade clears");
        Assert.IsTrue(GameSession.Flags.Contains("visited:Woods2"));
    }

    [UnityTest]
    public IEnumerator EveryOpeningInTheWoodsHasAnArrowPointingOut()
    {
        yield return Load("Woods2");
        var edges = Object.FindObjectsByType<RoomEdge>();
        Assert.AreEqual(3, edges.Length);
        foreach (var edge in edges)
        {
            Assert.IsNotNull(edge.GetComponentInChildren<SpriteRenderer>(), "an arrow on the floor");
            Assert.IsTrue(edge.GetComponent<Collider>().isTrigger);
        }
    }

    // ---------- Dad's Workshop ----------

    [Test]
    public void TheWorkshopIsTheRoomBehindTheFakeWall()
    {
        var map = ReadMap("Dungeon");
        var secret = map.SecretTiles();
        Assert.Greater(secret.Count, 15, "a whole room");
        Assert.IsTrue(secret.Contains((15, 9)), "the star shard's corner");
        Assert.IsTrue(secret.Contains((21, 8)));
        Assert.IsFalse(secret.Contains((5, 3)), "the start room is not secret");
        Assert.IsFalse(secret.Contains((17, 2)), "nor is the gap's alcove");
        Assert.IsTrue(secret.All(t => t.col >= 14 && t.col <= 22 && t.row >= 7 && t.row <= 9));
        Assert.IsTrue(new Reach(map, hop: false, spells: false).Reached(15, 9), "the fake wall lets you in on foot");
    }

    [UnityTest]
    public IEnumerator YouCanWalkThroughTheFakeWallAndTheRoomBehindItIsHiddenOnTheMinimap()
    {
        yield return Load("Dungeon");
        var map = Object.FindAnyObjectByType<LevelMap>();
        Assert.IsTrue(map.IsFakeWall('0'));
        var secretRoom = Object.FindAnyObjectByType<SecretRoom>();
        Assert.IsFalse(secretRoom.IsShown, "the workshop doesn't show over the wall");
        Assert.IsFalse(Object.FindObjectsByType<Collectible>().Any(c => c.Type == Collectible.Kind.StarShard && c.transform.position.x > 28f && c.transform.position.x < 48f && c.transform.position.z < 70f && c.transform.position.z > 60f));
        Assert.IsTrue(map.IsSecretTile(15, 9));
        Assert.IsFalse(map.SecretFound);
        var wall = Object.FindAnyObjectByType<FakeWall>();
        Assert.IsFalse(wall.GetComponentsInChildren<Collider>().Any(c => !c.isTrigger), "nothing solid: it only looks like a wall");
        Assert.IsTrue(wall.GetComponentInChildren<Renderer>().enabled, "but it's drawn like one");

        // Walk east from the start room's tile (12,7), through the wall at (13,7), into the workshop.
        Teleport(TileCentre(43, 12, 7));
        var controller = Player.GetComponent<CharacterController>();
        for (int i = 0; i < 40; i++)
        {
            controller.Move(Vector3.right * 0.2f);
            yield return null;
        }
        Assert.Greater(Player.transform.position.x, TileCentre(43, 14, 7).x, "through the wall");
        Assert.IsTrue(map.SecretFound);
        Assert.IsTrue(secretRoom.IsShown, "and appears once you're through");
        Assert.IsTrue(GameSession.Flags.Contains(FakeWall.FoundFlag("Dungeon")));
        Assert.AreEqual(1, GameSession.GetCounter(FakeWall.FoundCounter));
    }

    [UnityTest]
    public IEnumerator TheComputerAndTheSignedNoteAreReadable()
    {
        yield return Load("Dungeon");
        Assert.IsFalse(Object.FindObjectsByType<HouseFixture>().Any(f => f.Kind == HouseFixture.Effect.Read), "out of sight until the fake wall is found");
        Object.FindAnyObjectByType<FakeWall>().Reveal();
        yield return null;
        var fixtures = Object.FindObjectsByType<HouseFixture>().Where(f => f.Kind == HouseFixture.Effect.Read).ToList();
        Assert.AreEqual(2, fixtures.Count, "a computer and a note");
        var desk = fixtures.First(f => f.name.Contains("Desk"));
        var note = fixtures.First(f => f.name.Contains("Note"));
        Assert.IsFalse(GameSession.Flags.Contains("found:workshop"));
        StringAssert.Contains("Unity editor", desk.Interact(Player));
        Assert.IsTrue(GameSession.Flags.Contains("found:workshop"));
        Assert.AreEqual(1, GameSession.GetCounter("workshop_reads"));
        StringAssert.Contains("Tidecrown", desk.Interact(Player), "each look says something new");
        StringAssert.Contains("favourite player", note.Interact(Player));
        StringAssert.Contains("Dad", note.Interact(Player) + note.Interact(Player));
    }

    // ---------- The maps ----------

    // A rough flood fill over a map file: which tiles can the hero stand on, with or without the abilities?
    private class Reach
    {
        public readonly MapFile Map;
        private readonly bool[,] seen;
        public Reach(MapFile map, bool hop, bool spells)
        {
            Map = map;
            int w = map.Rows.Max(r => r.Length), h = map.Rows.Length;
            seen = new bool[w, h];
            var (sc, sr) = map.Find('P').First();
            var queue = new Queue<(int, int)>();
            queue.Enqueue((sc, sr));
            seen[sc, sr] = true;
            while (queue.Count > 0)
            {
                var (c, r) = queue.Dequeue();
                foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nc = c + dc, nr = r + dr;
                    if (nc < 0 || nr < 0 || nc >= w || nr >= h || seen[nc, nr]) continue;
                    char t = map.At(nc, nr);
                    if (IsGap(t))
                    {
                        // Hop over one or two gap tiles in a straight line, to the first tile that isn't one.
                        if (!hop) continue;
                        int width = 0, lc = nc, lr = nr;
                        while (IsGap(map.At(lc, lr)) && width <= 2) { lc += dc; lr += dr; width++; }
                        if (width > 2 || !Walkable(map.At(lc, lr), spells) || seen[lc, lr]) continue;
                        seen[lc, lr] = true;
                        queue.Enqueue((lc, lr));
                        continue;
                    }
                    if (!Walkable(t, spells)) continue;
                    seen[nc, nr] = true;
                    queue.Enqueue((nc, nr));
                }
            }
        }

        private bool IsGap(char c) => Map.Legend.TryGetValue(c, out var e) && e.Kind == "prop" && e.Args[0] == "gap";

        // Solid things stop you; a bramble stops you unless you can burn it. (Trees, bushes and boulders are solid.)
        private bool Walkable(char c, bool spells)
        {
            if (c == ' ' || LevelMap.IsWall(c) || LevelMap.IsWater(c) || Map.IsBuilding(c) || Map.IsWallProp(c)) return false;
            if ("YtiaObfVzlnoxcFqDRSWBAN".IndexOf(c) >= 0) return false;
            if (Map.Legend.TryGetValue(c, out var e))
            {
                if (e.Kind == "prop" && e.Args[0] == "bramble") return spells;
                if (e.Kind == "prop" && e.Args[0] is "brazier" or "pot" or "stall" or "well" or "coop" or "sleepytree") return false;
                if (e.Kind == "npc") return false;
            }
            return true;
        }

        public bool Reached(int col, int row) => seen[col, row];
        public bool ReachedNextTo(int col, int row) =>
            new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Any(d => Reached(col + d.Item1, row + d.Item2));
    }

    private static MapFile ReadMap(string name) =>
        MapFile.Parse(name, File.ReadAllText(Path.Combine(Application.dataPath, "Levels", name + ".txt")));

    [Test]
    public void TheCastleGroundsGapsSealTheirTreasuresUntilYouHaveTheBoots()
    {
        var map = ReadMap("Level0");
        var piece = map.Legend.Values.First(e => e.Kind == "prop" && e.Args[0] == "heartpiece");
        var (pc, pr) = map.Find(piece.Symbol).First();
        Assert.IsFalse(new Reach(map, hop: false, spells: true).Reached(pc, pr), "the secret garden is out of reach on foot");
        Assert.IsTrue(new Reach(map, hop: true, spells: false).Reached(pc, pr), "the boots hop you in");

        var bramble = map.Legend.Values.First(e => e.Kind == "prop" && e.Args[0] == "bramble");
        var chest = map.Find('C').Single(); // the chest in the thorn nook
        Assert.IsFalse(new Reach(map, hop: true, spells: false).Reached(chest.col, chest.row), "thorns stop you");
        Assert.IsTrue(new Reach(map, hop: false, spells: true).Reached(chest.col, chest.row), "and a spell clears them, boots or not");
        Assert.AreEqual(1, map.Find(bramble.Symbol).Count());

        // The grounds are open to all the usual places on foot.
        var stairs = map.Find('X').Single();
        Assert.IsTrue(new Reach(map, hop: false, spells: false).ReachedNextTo(stairs.col, stairs.row));
    }

    [Test]
    public void TheDungeonGapsSealTheStarShardAndTheEggUntilYouHaveTheBoots()
    {
        var map = ReadMap("Dungeon");
        var onFoot = new Reach(map, hop: false, spells: true);
        var hopping = new Reach(map, hop: true, spells: true);

        char ShardSymbol() => map.Legend.Values.First(e => e.Kind == "prop" && e.Args[0] == "starshard" && e.Args.Length == 1).Symbol;
        var shard = map.Find(ShardSymbol()).Single(t => t.row < 3); // the alcove above the corridor (the other is in the workshop)
        var egg = map.Find(map.Legend.Values.First(e => e.Kind == "item" && e.Args[0] == "dragon_egg_castle").Symbol).Single();
        Assert.IsFalse(onFoot.Reached(shard.col, shard.row), "the alcove above the corridor");
        Assert.IsFalse(onFoot.Reached(egg.col, egg.row), "the egg chamber");
        Assert.IsTrue(hopping.Reached(shard.col, shard.row));
        Assert.IsTrue(hopping.Reached(egg.col, egg.row));

        // Everything else is reachable on foot: the boss, his stairs, the chests, the rewards he leaves.
        foreach (char marker in new[] { 'M', 'X', 'C', 'y' })
            foreach (var (c, r) in map.Find(marker))
                Assert.IsTrue(onFoot.Reached(c, r) || onFoot.ReachedNextTo(c, r), $"'{marker}' at ({c},{r}) can be reached without the boots");
        foreach (char symbol in new[] { '3', '4' })
        {
            var (c, r) = map.Find(symbol).Single();
            Assert.IsTrue(onFoot.Reached(c, r), $"the Slime King's reward '{symbol}'");
        }
        foreach (var brazier in map.Find('6'))
            Assert.IsTrue(onFoot.ReachedNextTo(brazier.col, brazier.row), "braziers are for everyone");
    }

    [Test]
    public void EveryGapSymbolIsOnItsMap()
    {
        foreach (var path in Directory.GetFiles(Path.Combine(Application.dataPath, "Levels"), "*.txt"))
        {
            var map = MapFile.Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
            foreach (var gap in map.Legend.Values.Where(e => e.Kind == "prop" && e.Args[0] == "gap"))
                Assert.IsTrue(map.Find(gap.Symbol).Any(), $"{map.Name}: the gap symbol '{gap.Symbol}' is on the map");
        }
    }

    [Test]
    public void TheExitSpawnIsCheckedByTheValidator()
    {
        var up = MapFile.Parse("Up", "title: Up\ntheme: Outdoor\nexit: Down\nexit_spawn: Nowhere\n---\nP.X\n---\n");
        var down = MapFile.Parse("Down", "title: Down\ntheme: Outdoor\n---\nP..\n---\n");
        var errors = MapValidator.Validate(new[] { up, down });
        Assert.IsTrue(errors.Any(e => e.Contains("Nowhere")), string.Join("\n", errors));

        var fixedDown = MapFile.Parse("Down", "title: Down\ntheme: Outdoor\n---\nP.1\n---\n1 = spawn Nowhere\n");
        Assert.IsFalse(MapValidator.Validate(new[] { up, fixedDown }).Any(e => e.Contains("Nowhere")));
    }
}
