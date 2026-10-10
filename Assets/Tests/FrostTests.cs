using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for Frostpeak: the four rooms, the ice you slide on, ice slimes' trails, Mr. Frost's scarf chain (with
// Barnaby's yarn and Granny Purl), the Snow Yeti and his rolling snowballs, and the Rainbow Chalk's bridges over chasms.
public class FrostTests : InputTestFixture
{
    private string folder;
    private Gamepad pad;

    public override void Setup()
    {
        base.Setup();
        pad = InputSystem.AddDevice<Gamepad>();
        folder = Path.Combine(Path.GetTempPath(), "TidecrownFrostTests_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        GameSession.NewGame(null);
    }

    public override void TearDown()
    {
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        SaveSystem.FolderOverride = null;
        Time.timeScale = 1f;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        base.TearDown();
    }

    private static IEnumerator Load(string scene, bool freezeEnemies = true, bool freezeHero = true)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        if (freezeEnemies) foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        if (freezeHero) LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
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

    private static Vector3 TileCentre(LevelMap map, int col, int row) => new Vector3(col * map.TileSize, 0f, (map.Height - 1 - row) * map.TileSize);

    // ---------- The rooms ----------

    [UnityTest]
    public IEnumerator AllFourRoomsLoadAndConnectUp()
    {
        foreach (var room in new[] { "Frost1", "Frost2", "Frost3", "Frost4" })
        {
            yield return Load(room);
            Assert.AreEqual(room, SceneManager.GetActiveScene().name);
            Assert.IsTrue(GameSession.Flags.Contains("visited:" + room));
            Assert.GreaterOrEqual(Object.FindObjectsByType<RoomEdge>().Length, 1, room + " has a way out");
        }
        yield return Load("Frost1");
        CollectionAssert.AreEquivalent(new[] { "Lake3", "Frost2" }, Object.FindObjectsByType<RoomEdge>().Select(d => d.TargetScene));
        yield return Load("Lake3");
        CollectionAssert.Contains(Object.FindObjectsByType<RoomEdge>().Select(d => d.TargetScene).ToList(), "Frost1");
    }

    [UnityTest]
    public IEnumerator TheCampHasMrFrostGrannyPurlAndAFountain()
    {
        yield return Load("Frost1");
        Assert.IsNotNull(NpcNamed("Mr. Frost"));
        Assert.IsNotNull(NpcNamed("Granny Purl"));
        Assert.IsNotNull(Object.FindAnyObjectByType<WakeFountain>());
        Assert.GreaterOrEqual(Object.FindObjectsByType<RainbowPost>().Length, 2, "a pair of rainbow posts");
    }

    // ---------- Ice ----------

    [UnityTest]
    public IEnumerator OnIceTheHeroKeepsSlidingAfterTheyLetGo()
    {
        yield return Load("Frost2", freezeHero: false);
        var player = LevelBootstrap.Current.Player;
        var map = Object.FindAnyObjectByType<LevelMap>();
        var movement = player.GetComponent<PlayerController>();
        var ice = TileCentre(map, 16, 15);
        var snow = TileCentre(map, 4, 12);
        Assert.IsTrue(map.IsIceAt(ice), "(16,15) is ice");
        Assert.IsFalse(map.IsIceAt(snow), "(4,12) is snow");

        // On snow: stop the moment the stick is released.
        Teleport(player, snow + Vector3.up * 0.1f);
        yield return new WaitForSeconds(0.2f);
        Assert.IsFalse(movement.IsOnIce);
        Set(pad.leftStick, new Vector2(1f, 0f));
        yield return new WaitForSeconds(0.5f);
        Set(pad.leftStick, Vector2.zero);
        yield return null;
        var stopped = player.transform.position;
        yield return new WaitForSeconds(0.4f);
        Assert.Less(Vector3.Distance(stopped, player.transform.position), 0.2f, "on snow you stop when you let go");

        // On ice: you carry on.
        Teleport(player, ice + Vector3.up * 0.1f);
        yield return new WaitForSeconds(0.2f);
        Assert.IsTrue(movement.IsOnIce);
        Set(pad.leftStick, new Vector2(1f, 0f));
        yield return new WaitForSeconds(0.6f);
        Set(pad.leftStick, Vector2.zero);
        yield return null;
        var letGo = player.transform.position;
        yield return new WaitForSeconds(0.5f);
        Assert.Greater(Vector3.Distance(letGo, player.transform.position), 0.8f, "on ice you keep sliding");
    }

    [UnityTest]
    public IEnumerator AnIceSlimeLeavesASlipperyTrail()
    {
        yield return Load("Frost2", freezeEnemies: false, freezeHero: false);
        foreach (var e in Object.FindObjectsByType<EnemyAI>().Where(e => !e.name.StartsWith("IceSlime"))) e.enabled = false;
        var slime = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("IceSlime"));
        var trail = slime.GetComponent<IceTrail>();
        Assert.IsNotNull(trail);
        var player = LevelBootstrap.Current.Player;
        var map = Object.FindAnyObjectByType<LevelMap>();
        var snow = TileCentre(map, 4, 12);
        Teleport(player, snow + Vector3.up * 0.1f);         // far away: the slime just stands about
        yield return null;
        Assert.AreEqual(0, IceZone.Count);

        var cc = slime.GetComponent<CharacterController>();
        cc.enabled = false; slime.transform.position = snow + new Vector3(6f, 1f, 6f); cc.enabled = true;
        yield return new WaitForSeconds(0.2f);
        cc.enabled = false; slime.transform.position = snow + new Vector3(9f, 1f, 6f); cc.enabled = true;
        yield return new WaitForSeconds(0.2f);
        Assert.GreaterOrEqual(trail.Patches, 1, "it left a patch");
        Assert.GreaterOrEqual(IceZone.Count, 1);
        Assert.IsTrue(IceZone.Covers(slime.transform.position + new Vector3(-3f, -1f, 0f)) || IceZone.Covers(snow + new Vector3(6f, 0f, 6f)), "the patch is where it was");

        // Stand on one: slippery.
        var patch = Object.FindObjectsByType<IceZone>().First();
        Teleport(player, patch.transform.position + Vector3.up * 0.1f);
        yield return new WaitForSeconds(0.2f);
        Assert.IsTrue(player.GetComponent<PlayerController>().IsOnIce);
    }

    // ---------- The scarf chain ----------

    [UnityTest]
    public IEnumerator BarnabySellsYarnOnlyOnceMrFrostHasAskedForAScarf()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var barnaby = Object.FindObjectsByType<Merchant>().First();
        Assert.AreEqual("bubble_bath", barnaby.Ware.Id, "usually: bubble bath");
        GameSession.Flags.Add("met:Mr. Frost");
        GameSession.Flags.Add("met:Barnaby"); // (he's introduced himself: the prompt names his ware)
        Assert.AreEqual("ball_of_yarn", barnaby.Ware.Id, "after Mr. Frost: yarn");
        Assert.AreEqual(12, barnaby.Price);
        StringAssert.Contains("Ball of Yarn", barnaby.Prompt);

        GameSession.Progress.AddGold(40);
        var bag = player.GetComponent<Inventory>();
        int gold = GameSession.Progress.Gold;
        StringAssert.Contains("Granny Purl", barnaby.Buy(bag));
        Assert.IsTrue(Abilities.Has("ball_of_yarn"));
        Assert.AreEqual(gold - 12, GameSession.Progress.Gold);
        Assert.AreEqual(1, GameSession.GetCounter("bought:ball_of_yarn"));
        Assert.AreEqual("bubble_bath", barnaby.Ware.Id, "one is plenty: back to the bubble bath");
    }

    [UnityTest]
    public IEnumerator TheScarfChainEndsWithASnowHat()
    {
        yield return Load("Frost1");
        var player = LevelBootstrap.Current.Player;
        var frost = NpcNamed("Mr. Frost");
        var purl = NpcNamed("Granny Purl");

        TalkTo(frost);
        Assert.IsTrue(GameSession.Flags.Contains("met:Mr. Frost"));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("scarf")));
        TalkTo(purl);
        Assert.IsTrue(GameSession.Flags.Contains("met:Granny Purl"));
        Assert.IsFalse(Abilities.Has("warm_scarf"));
        StringAssert.Contains("Barnaby", purl.Next().lines.Select(l => l.text).Aggregate((a, b) => a + " " + b), "she sends you to Barnaby for yarn");

        // The yarn (bought at Barnaby's, here given).
        GameSession.Inventory.KeyItems.Add("ball_of_yarn");
        GameSession.AddToCounter("bought:ball_of_yarn");
        TalkTo(purl);
        Assert.IsTrue(GameSession.Flags.Contains("purl:knit"));
        Assert.IsFalse(Abilities.Has("ball_of_yarn"), "she keeps the yarn");
        Assert.IsTrue(Abilities.Has("warm_scarf"), "and gives you a scarf");

        TalkTo(frost);
        Assert.IsTrue(GameSession.Flags.Contains("thanked:frost"));
        Assert.IsFalse(Abilities.Has("warm_scarf"), "he keeps the scarf");
        Assert.IsTrue(Abilities.Has("snow_hat"), "and you get a Snow Hat");
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("scarf")));
    }

    // ---------- Chasms and rainbow bridges ----------

    [UnityTest]
    public IEnumerator ARainbowPostNeedsTheChalkThenDrawsABridgeAcrossTheChasm()
    {
        yield return Load("Woods2");
        var player = LevelBootstrap.Current.Player;
        var posts = Object.FindObjectsByType<RainbowPost>();
        Assert.AreEqual(2, posts.Length);
        Assert.AreSame(posts[0].Bridge, posts[1].Bridge, "both posts share the one bridge");
        var bridge = posts[0].Bridge;
        Assert.AreEqual(3, bridge.Length, "three chasm tiles between them");
        Assert.IsFalse(bridge.IsDrawn);
        Assert.IsTrue(Object.FindObjectsByType<HintBubble>().Any(h => h.Ability == Abilities.RainbowChalk), "a come-back-later bubble");

        StringAssert.Contains("something to draw with", posts[0].Interact(player));
        Assert.IsFalse(bridge.IsDrawn, "no chalk, no bridge");

        GameSession.Inventory.KeyItems.Add(Abilities.RainbowChalk);
        Assert.AreEqual("Draw a rainbow bridge", posts[0].Prompt);
        StringAssert.Contains("draw a rainbow bridge", posts[0].Interact(player).ToLowerInvariant());
        Assert.IsTrue(bridge.IsDrawn);
        yield return new WaitForSeconds(1.2f);
        Assert.AreEqual(3, bridge.GetComponentsInChildren<Transform>().Count(t => t.name == "Plank" && t.gameObject.activeSelf), "every plank appeared");
        Assert.AreEqual(1, GameSession.GetCounter(RainbowBridge.DrawnCounter));

        // It stays drawn.
        yield return Load("Woods2");
        Assert.IsTrue(Object.FindAnyObjectByType<RainbowPost>().Bridge.IsDrawn);
    }

    [UnityTest]
    public IEnumerator YouCanOnlyWalkAcrossTheChasmOnceItsBridgeIsDrawn()
    {
        GameSession.Inventory.KeyItems.Add(Abilities.RainbowChalk);
        yield return Load("Woods2");
        var player = LevelBootstrap.Current.Player;
        var map = Object.FindAnyObjectByType<LevelMap>();
        var cc = player.GetComponent<CharacterController>();
        Teleport(player, TileCentre(map, 6, 1) + Vector3.up * 0.1f);   // beside the outer post, on the bank
        yield return null;

        // Before the bridge: the chasm's edge stops you.
        for (int i = 0; i < 30; i++) { cc.Move(Vector3.left * 0.3f + Vector3.down * 0.1f); yield return null; }
        Assert.Greater(player.transform.position.x, TileCentre(map, 5, 1).x, "stopped at the brink");

        // After: straight across.
        Object.FindAnyObjectByType<RainbowPost>().Interact(player);
        yield return new WaitForSeconds(1.2f);
        Teleport(player, TileCentre(map, 6, 1) + Vector3.up * 0.1f);
        yield return null;
        for (int i = 0; i < 60; i++) { cc.Move(Vector3.left * 0.3f + Vector3.down * 0.1f); yield return null; }
        Assert.Less(player.transform.position.x, TileCentre(map, 3, 1).x, "all the way over");
        Assert.Greater(player.transform.position.y, -0.5f, "on the planks, not down in the pit");
    }

    [UnityTest]
    public IEnumerator AChasmCannotBeHoppedAndSwimmersCannotWalkIntoIt()
    {
        GameSession.Inventory.KeyItems.Add(Abilities.BouncyBoots);
        GameSession.Inventory.KeyItems.Add(Abilities.BubbleCharm);
        yield return Load("Woods2");
        Assert.AreEqual(0, Object.FindObjectsByType<Gap>().Length, "a chasm is no Gap: the boots can't hop it");
        var player = LevelBootstrap.Current.Player;
        var map = Object.FindAnyObjectByType<LevelMap>();
        var cc = player.GetComponent<CharacterController>();
        Teleport(player, TileCentre(map, 6, 3) + Vector3.up * 0.1f);
        yield return null;
        for (int i = 0; i < 30; i++) { cc.Move(Vector3.left * 0.3f + Vector3.down * 0.1f); yield return null; }
        Assert.Greater(player.transform.position.x, TileCentre(map, 5, 3).x, "even a swimmer stops at the brink");
        Assert.Greater(player.transform.position.y, -0.5f);
    }

    // ---------- The Snow Yeti ----------

    [UnityTest]
    public IEnumerator TheYetiRollsSnowballsDownRedLanes()
    {
        yield return Load("Frost4", freezeEnemies: false);
        var player = LevelBootstrap.Current.Player;
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        Assert.AreEqual("The Snow Yeti", boss.BossName);
        var lanes = boss.GetComponent<SnowballLanes>();
        Assert.IsNotNull(lanes);
        boss.GetComponent<EnemyAI>().enabled = false;
        Teleport(player, boss.transform.position + Vector3.back * 7f);
        int hearts = player.GetComponent<Health>().Current;
        lanes.StartLanes();
        Assert.IsTrue(lanes.IsRolling);
        yield return new WaitForSeconds(6f);
        Assert.IsFalse(lanes.IsRolling);
        Assert.AreEqual(1, lanes.BallsHit, "the lane under your feet gets you");
        Assert.Less(player.GetComponent<Health>().Current, hearts);
        Assert.AreEqual(0, Object.FindObjectsByType<SpriteRenderer>().Count(s => s.name == "Snowball" || s.name == "SnowLane"), "the snowballs are gone");
    }

    [UnityTest]
    public IEnumerator BeatingTheYetiRevealsTheChalkAndTheSapphire()
    {
        GameSession.Flags.Add("met:Granny Purl");
        yield return Load("Frost4", freezeEnemies: false);
        var player = LevelBootstrap.Current.Player;
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var rewards = Object.FindObjectsByType<BossReward>(FindObjectsInactive.Include);
        Assert.AreEqual(2, rewards.Length);
        Assert.IsTrue(rewards.All(r => !r.IsShown));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("yeti")));

        boss.Health.TakeDamage(999);
        yield return null;
        Assert.IsTrue(rewards.All(r => r.IsShown));
        Assert.IsTrue(GameSession.Flags.Contains("cleared:Frost4"));
        foreach (var id in new[] { Abilities.RainbowChalk, "sapphire" })
            Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == id).Interact(player);
        Assert.IsTrue(Abilities.Has(Abilities.RainbowChalk));
        Assert.AreEqual(QuestState.Done, QuestCatalog.StateOf(QuestCatalog.Find("yeti")));
        Assert.AreEqual(QuestState.Active, QuestCatalog.StateOf(QuestCatalog.Find("egg_frost")));
    }

    [UnityTest]
    public IEnumerator TheEggWaitsBehindAChasmInTheFrozenPass()
    {
        yield return Load("Frost3");
        var egg = Object.FindObjectsByType<ItemPickup>().First(p => p.Item.Id == "dragon_egg_frost");
        Assert.IsNotNull(egg);
        Assert.AreEqual(2, Object.FindObjectsByType<RainbowPost>().Length);
        Assert.AreEqual(1, Object.FindObjectsByType<RainbowBridge>().Length);
    }

    [UnityTest]
    public IEnumerator TheWaterRouteToFrostpeakStartsFromAnIslandInTheLake()
    {
        yield return Load("Lake3");
        Assert.IsTrue(Object.FindObjectsByType<RoomEdge>().Any(e => e.TargetScene == "Frost1"));
        Assert.IsTrue(Object.FindObjectsByType<HintBubble>().Any(h => h.Ability == Abilities.BubbleCharm));
    }

    [Test]
    public void FrostpeakHasItsQuests()
    {
        foreach (var id in new[] { "scarf", "yeti", "egg_frost" }) Assert.IsNotNull(QuestCatalog.Find(id), id);
    }
}
