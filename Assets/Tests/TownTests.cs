using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for Hollyhock, the village east of the castle on Level 0: its buildings and
// fence, Barnaby Badger's bubble bath, the chickens, and the map file's new legend kinds.
public class TownTests
{
    [SetUp]
    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
    }

    private static void Teleport(GameObject player, Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = to;
        cc.enabled = true;
    }

    // The world position of a map tile, as DungeonBuilder places it.
    private static Vector3 TileAt(LevelMap map, int col, int row) =>
        new Vector3(col * map.TileSize, 1f, (map.Height - 1 - row) * map.TileSize);

    private static (int col, int row) Find(LevelMap map, char symbol)
    {
        for (int row = 0; row < map.Height; row++)
            for (int col = 0; col < map.Width; col++)
                if (map.At(col, row) == symbol) return (col, row);
        throw new AssertionException($"'{symbol}' isn't on the map");
    }

    private static Merchant Barnaby() => Object.FindAnyObjectByType<Merchant>();

    private static void FinishDialogue()
    {
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.IsFalse(DialogueController.IsOpen);
    }

    [UnityTest]
    public IEnumerator TheVillageHasACathedralAShopAndACottage()
    {
        yield return Load("Level0");
        var map = Object.FindAnyObjectByType<LevelMap>();
        foreach (var name in new[] { "Cathedral", "Shop", "Cottage" })
        {
            var building = GameObject.Find(name);
            Assert.IsNotNull(building, $"the village has a {name}");
            Assert.IsTrue(building.GetComponentsInChildren<Transform>().Any(t => t.name == "Roof"), $"the {name} has a roof");
            Assert.IsNotNull(building.GetComponentInChildren<HouseFixture>(), $"you can knock on the {name}'s door");
        }
        var cathedral = GameObject.Find("Cathedral");
        float spire = cathedral.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y);
        Assert.Greater(spire, 12f, "the cathedral's spire towers over the village");
        Assert.IsTrue(cathedral.GetComponentsInChildren<Transform>().Any(t => t.name == "Lancet"), "it has stained glass");

        Assert.IsTrue(map.IsSolid(map.At(Find(map, '3').col, Find(map, '3').row)), "buildings are solid (and drawn as roofs on the minimap)");
        Assert.IsTrue(LevelMap.IsWall('+'), "so is the fence");
        Assert.Greater(Object.FindObjectsByType<BoxCollider>().Count(b => b.name == "Fence"), 30, "a fence goes all round");
    }

    [UnityTest]
    public IEnumerator TheFenceStopsYouButTheGateLetsYouIn()
    {
        yield return Load("Level0");
        var map = Object.FindAnyObjectByType<LevelMap>();
        var player = LevelBootstrap.Current.Player;
        var cc = player.GetComponent<CharacterController>();
        var (gateCol, gateRow) = (23, 6); // the west gate, at the end of the path from the castle
        Assert.AreEqual('=', map.At(gateCol, gateRow), "the gate is a gap in the fence");
        Assert.AreEqual('+', map.At(gateCol, gateRow + 2));

        // Walking east into the fence beside the gate: blocked.
        Teleport(player, TileAt(map, gateCol - 1, gateRow + 2));
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            cc.Move(Vector3.right * 6f * Time.deltaTime + Vector3.down);
            yield return null;
        }
        Assert.Less(player.transform.position.x, gateCol * map.TileSize - 0.5f, "the picket fence is solid");

        // Through the gate: in you go.
        Teleport(player, TileAt(map, gateCol - 1, gateRow));
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            cc.Move(Vector3.right * 6f * Time.deltaTime + Vector3.down);
            yield return null;
        }
        Assert.Greater(player.transform.position.x, (gateCol + 1) * map.TileSize, "the gate is open");
    }

    [UnityTest]
    public IEnumerator BarnabyIntroducesHimselfThenSellsBubbleBath()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var inventory = player.GetComponent<Inventory>();
        var barnaby = Barnaby();
        Assert.IsNotNull(barnaby, "Barnaby Badger keeps a stall in the village");
        Assert.AreEqual("bubble_bath", barnaby.Ware.Id);
        Assert.IsFalse(barnaby.Ware.IsEquippable, "bubble bath is a novelty: nothing to wear");

        // He's in reach from the street in front of his stall.
        Teleport(player, barnaby.transform.position + new Vector3(0f, 1f, -1.8f));
        yield return null;
        Assert.AreEqual(barnaby, player.GetComponent<PlayerInteractor>().Current);

        // First: hello.
        Assert.AreEqual("Talk to Barnaby", barnaby.Prompt);
        player.GetComponent<PlayerInteractor>().TryInteract();
        Assert.IsTrue(DialogueController.IsOpen);
        FinishDialogue();
        Assert.IsTrue(barnaby.HasMet);
        Assert.AreEqual($"Buy Bubble Bath ({barnaby.Price} coins)", barnaby.Prompt);
        Assert.AreEqual(0, inventory.Bag.Count, "meeting him doesn't cost anything");

        // No coins, no bubbles.
        StringAssert.Contains($"{barnaby.Price} coins", barnaby.Buy(inventory));
        Assert.AreEqual(0, inventory.Bag.Count);

        // With coins: a bottle in the bag, the coins in his till, and a thank-you in the dialogue box.
        GameSession.Progress.AddGold(barnaby.Price + 5);
        player.GetComponent<PlayerInteractor>().TryInteract();
        Assert.IsTrue(DialogueController.IsOpen, "he says thank you");
        FinishDialogue();
        Assert.AreEqual(5, GameSession.Progress.Gold);
        Assert.AreEqual("bubble_bath", inventory.Bag.Single().Id);
        Assert.AreEqual(1, barnaby.Sold);
        Assert.IsFalse(inventory.Equip(inventory.Bag[0]), "you can't wear bubble bath");
    }

    [UnityTest]
    public IEnumerator AFullBagStopsTheSaleAndKeepsYourCoins()
    {
        yield return Load("Level0");
        var inventory = LevelBootstrap.Current.Player.GetComponent<Inventory>();
        var barnaby = Barnaby();
        GameSession.Progress.AddGold(barnaby.Price * (inventory.Capacity + 1));
        for (int i = 0; i < inventory.Capacity; i++) barnaby.Buy(inventory);
        Assert.AreEqual(inventory.Capacity, inventory.Bag.Count, "you can buy as many as your bag holds");
        int gold = GameSession.Progress.Gold;
        StringAssert.Contains("full", barnaby.Buy(inventory));
        Assert.AreEqual(gold, GameSession.Progress.Gold, "no coins taken for a bottle you can't carry");
        Assert.AreEqual(inventory.Capacity, barnaby.Sold);
    }

    [UnityTest]
    public IEnumerator ChickensWanderAndPeckInsideTheirPen()
    {
        yield return Load("Level0");
        var map = Object.FindAnyObjectByType<LevelMap>();
        var chickens = Object.FindObjectsByType<Chicken>();
        Assert.GreaterOrEqual(chickens.Length, 6, "four hens and two chicks");
        var start = chickens.Select(c => c.transform.position).ToArray();

        Time.timeScale = 4f; // a few seconds of chicken life, quickly
        yield return new WaitForSeconds(16f);
        Time.timeScale = 1f;

        Assert.IsTrue(chickens.Where((c, i) => (c.transform.position - start[i]).magnitude > 0.2f).Any(), "they potter about");
        foreach (var chicken in chickens)
        {
            Assert.LessOrEqual(Vector3.Distance(chicken.transform.position, chicken.Home), chicken.Radius + 0.01f);
            var tile = map.WorldToMap(chicken.transform.position);
            char under = map.At(Mathf.RoundToInt(tile.x), Mathf.RoundToInt(tile.y));
            Assert.IsFalse(map.IsSolid(under), $"a chicken wandered into '{under}' at {tile}");
        }
        var hen = chickens.First(c => c.name.StartsWith("Hen")).GetComponent<HouseFixture>();
        Assert.AreEqual("Pet the hen", hen.Prompt);
    }

    [Test]
    public void TheValidatorChecksBuildingsPropsAndNpcs()
    {
        MapFile Map(string body, string legend) =>
            MapFile.Parse("Village", "title: T\ntheme: Outdoor\n---\n" + body + "\n---\n" + legend);
        const string good = "HHHHHH\nH33.6H\nH33P!H\nHHHHHH";
        Assert.IsEmpty(MapValidator.Validate(new[] { Map(good, "3 = building shop\n6 = npc barnaby\n! = prop lamppost") }));

        var gappy = MapValidator.Validate(new[] { Map("HHHHHH\nH33..H\nH.3P.H\nHHHHHH", "3 = building cottage") });
        StringAssert.Contains("rectangle", gappy.Single());
        var unknown = MapValidator.Validate(new[] { Map(good, "3 = building castle\n6 = npc bob\n! = prop rocket") });
        Assert.AreEqual(3, unknown.Count, string.Join("\n", unknown));
        var builtIn = MapValidator.Validate(new[] { Map("HHHH\nH+PH\nHHHH", "+ = prop well") });
        StringAssert.Contains("built-in", builtIn.Single());
    }
}
