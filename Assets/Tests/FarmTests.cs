using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for Hollow Farm, the haunted farmland through the gate in the castle
// grounds' southern hedge.
public class FarmTests
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
        yield return WaitFor(scene);
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    private static IEnumerator WaitFor(string scene)
    {
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
        yield return null;
        yield return null;
    }

    private static string Title() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement
            .Q<Label>("objective-title").text;

    [UnityTest]
    public IEnumerator TheFarmGateLeadsToHollowFarmAndBack()
    {
        yield return Load("Level0");
        var gate = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Farm");
        Assert.AreEqual("Open the farm gate", gate.Prompt);
        gate.Interact(LevelBootstrap.Current.Player);
        yield return WaitFor("Farm");

        Assert.AreEqual("Hollow Farm", Title());
        var back = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Level0");
        var player = LevelBootstrap.Current.Player;
        Assert.Less(Vector3.Distance(player.transform.position, back.transform.position), 3f, "you arrive just inside the gate");

        back.Interact(player);
        yield return WaitFor("Level0");
        gate = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Farm");
        Assert.Less(Vector3.Distance(LevelBootstrap.Current.Player.transform.position, gate.transform.position), 3f,
            "and back by the farm gate on the castle grounds");
    }

    [UnityTest]
    public IEnumerator HollowFarmIsSpookyButFriendly()
    {
        yield return Load("Farm");
        var player = LevelBootstrap.Current.Player;
        Assert.IsTrue(Object.FindObjectsByType<EnemyAI>().All(e => !e.name.StartsWith("Ghost") && !e.name.StartsWith("Wisp")),
            "the monsters are pumpkins and scarecrows: the ghosts are still friendly");

        // Dusk: a violet sky and a low orange sun.
        Assert.AreEqual((Color)ArtStyle.DuskSky, Camera.main.backgroundColor);
        Assert.Less(RenderSettings.ambientLight.g, RenderSettings.ambientLight.b, "a cool violet dusk");

        int Count(string name) => Object.FindObjectsByType<Transform>().Count(t => t.name == name);
        Assert.GreaterOrEqual(Count("Pumpkin"), 10);
        Assert.GreaterOrEqual(Count("Corn"), 40);
        Assert.GreaterOrEqual(Count("Scarecrow"), 2);
        Assert.GreaterOrEqual(Count("Gravestone"), 6);
        var lanterns = Object.FindObjectsByType<Transform>().Where(t => t.name == "JackOLantern").ToList();
        Assert.GreaterOrEqual(lanterns.Count, 10);
        Assert.IsTrue(lanterns.All(j => j.GetComponentInChildren<Light>() != null), "every jack-o'-lantern glows");
        Assert.IsNotNull(GameObject.Find("Barn"));
        Assert.IsTrue(GameObject.Find("Barn").GetComponentsInChildren<Transform>().Any(t => t.name == "Roof"));

        // Pumpkins grow in tilled soil (all but a couple by the corn field).
        int onSoil = Object.FindObjectsByType<Transform>().Where(t => t.name == "Pumpkin")
            .Count(p => Physics.RaycastAll(p.position + Vector3.up * 3f, Vector3.down, 6f)
                .Select(h => h.collider.GetComponent<Renderer>()).First(r => r != null && r.name == "Floor")
                .sharedMaterial.name == "Soil");
        Assert.GreaterOrEqual(onSoil, 10);

        // The ghosts float about and like being booed.
        var ghost = Object.FindObjectsByType<HouseFixture>().First(f => f.name.StartsWith("Ghost"));
        Assert.AreEqual("Say boo to the ghost", ghost.Prompt);
        Vector3 before = ghost.transform.position;
        yield return new WaitForSeconds(0.5f);
        Assert.Greater(Vector3.Distance(before, ghost.transform.position), 0.01f, "it drifts");
        StringAssert.Contains("ghost", ghost.Interact(player));
        Assert.AreEqual("ghost_ooo", AudioManager.Instance.LastPlayed.name);

        // Old Stitches, the scarecrow who talks.
        var stitches = Object.FindObjectsByType<Npc>().Single(n => n.Name == "Old Stitches");
        stitches.Interact(player);
        Assert.IsTrue(DialogueController.IsOpen);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.IsTrue(stitches.HasMet);
        StringAssert.Contains("OUTSTANDING", stitches.Next().lines.Last().text, "then the jokes start");
    }

    // Teleports with the CharacterController off, so the move isn't undone.
    private static void Teleport(GameObject player, Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = to;
        cc.enabled = true;
    }

    private static Vector3 TileCenter(int col, int row)
    {
        var map = Object.FindAnyObjectByType<LevelMap>();
        return new Vector3(col * map.TileSize, 1f, (map.Height - 1 - row) * map.TileSize);
    }

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    [UnityTest]
    public IEnumerator TheFarmHasGourdlingsAndStrawmenAndAPumpkinKingInTheGraveyard()
    {
        yield return Load("Farm");
        var all = Object.FindObjectsByType<EnemyAI>();
        int Count(string prefix) => all.Count(e => e.name.StartsWith(prefix));
        Assert.AreEqual(4, Count("Gourdling"), "angry pumpkins in the pumpkin patch");
        Assert.AreEqual(4, Count("Strawman"), "scarecrows gone bad, in the corn and by the graveyard");
        Assert.AreEqual(1, Count("PumpkinKing"), "and the boss");
        Assert.AreEqual(9, EnemyAI.AliveCount);

        // The King sleeps in the middle of the graveyard (inside its picket fence).
        var king = Object.FindAnyObjectByType<BossAbilities>();
        Assert.AreEqual("The Pumpkin King", king.BossName);
        Assert.Less(Vector3.Distance(king.transform.position, TileCenter(31, 19)), 2f, "in the middle of the graveyard");
        Assert.IsTrue(king.HasVolley, "he throws bolts");

        // He's a real boss: far more health and XP than a Gourdling or a Strawman.
        var gourdling = all.First(e => e.name.StartsWith("Gourdling"));
        Assert.Greater(king.Health.Max, gourdling.GetComponent<Health>().Max * 8);
        int kingXp = king.GetComponent<Loot>().Experience;
        foreach (var normal in all.Where(e => e.GetComponent<BossAbilities>() == null).Select(e => e.GetComponent<Loot>().Experience).Distinct())
            Assert.That(kingXp, Is.InRange(10 * normal, 50 * normal), $"10-50x the {normal} XP of the others");

        // The HUD counts the monsters and says what to do (there's no exit crystal here: the King is the goal).
        yield return null;
        Assert.AreEqual("9 monsters left", Hud().Q<Label>("enemies-left").text);
        Assert.AreEqual("Beat the Pumpkin King!", Hud().Q<Label>("objective-hint").text);
    }

    [UnityTest]
    public IEnumerator AGourdlingHopsAfterYouBitesAndIsBeaten()
    {
        yield return Load("Farm");
        GameSession.Settings.gentle = false; // full damage, so the bite is easy to count
        var player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        // The one among the pumpkins at (10, 8), with an open row of soil running east from it.
        var gourdling = Object.FindObjectsByType<EnemyAI>().Where(e => e.name.StartsWith("Gourdling"))
            .OrderBy(e => Vector3.Distance(e.transform.position, TileCenter(10, 8))).First();
        var health = player.GetComponent<Health>();

        // It notices you from a few tiles away and hops over to bite.
        Teleport(player, gourdling.transform.position + new Vector3(5f, 0f, 0f));
        gourdling.enabled = true;
        float start = Vector3.Distance(gourdling.transform.position, player.transform.position);
        for (float t = 0f; t < 6f && health.Current == health.Max; t += Time.deltaTime) yield return null;
        Assert.Less(health.Current, health.Max, "the Gourdling bit you");
        Assert.Less(Vector3.Distance(gourdling.transform.position, player.transform.position), start, "it came after you");

        // ...and it can be beaten: XP, coins and a puff of stars.
        int xp = GameSession.Progress.Xp;
        int before = EnemyAI.AliveCount;
        gourdling.GetComponent<Health>().TakeDamage(99);
        Assert.AreEqual(xp + gourdling.GetComponent<Loot>().Experience, GameSession.Progress.Xp);
        Assert.AreEqual(before - 1, EnemyAI.AliveCount);
        yield return new WaitForSeconds(1.2f);
        Assert.IsTrue(gourdling == null, "it pops and is gone");
    }

    [UnityTest]
    public IEnumerator ThePumpkinKingThrowsAFanOfBoltsCallsGourdlingsAndBeatingHimClearsTheFarm()
    {
        yield return Load("Farm");
        GameSession.Settings.gentle = false;
        var player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        var king = Object.FindAnyObjectByType<BossAbilities>();
        var announced = new System.Collections.Generic.List<string>();
        king.Announced += announced.Add;

        // He wakes when you walk in, and a health bar shows.
        Teleport(player, king.transform.position + new Vector3(-6f, 0f, 0f));
        yield return null;
        yield return null;
        Assert.IsTrue(king.IsEngaged);
        CollectionAssert.Contains(announced, "The Pumpkin King rises from the graveyard!");

        // His Volley: he rears back, then five bolts leave him in a fan.
        king.StartVolley();
        Assert.IsTrue(king.IsVolleying);
        int bolts = 0;
        for (float t = 0f; t < 3f && bolts == 0; t += Time.deltaTime)
        {
            yield return null;
            bolts = Object.FindObjectsByType<EnemyBolt>().Length;
        }
        Assert.AreEqual(5, bolts, "a fan of five bolts");
        var directions = Object.FindObjectsByType<EnemyBolt>().Select(b => b.transform.forward).ToList();
        // (FindObjectsByType gives them in no particular order, so compare the widest pair, not the first and last.)
        Assert.Greater(directions.Max(a => directions.Max(b => Vector3.Angle(a, b))), 20f, "fanned out, so you can step between them");

        // Hurt to half, he shakes loose Gourdlings.
        int gourdlings = Object.FindObjectsByType<EnemyAI>().Count(e => e.name.StartsWith("Gourdling"));
        king.Health.TakeDamage(king.Health.Max / 2);
        Assert.AreEqual(gourdlings + 4, Object.FindObjectsByType<EnemyAI>().Count(e => e.name.StartsWith("Gourdling")));
        CollectionAssert.Contains(announced, "The Pumpkin King shakes loose angry Gourdlings!");

        // Beating him clears the farm for good: even the monsters left over are gone next time.
        Assert.IsFalse(GameSession.Flags.Contains("cleared:Farm"));
        king.Health.TakeDamage(999);
        Assert.IsTrue(GameSession.Flags.Contains("cleared:Farm"));
        CollectionAssert.Contains(announced, "The Pumpkin King is beaten! Hollow Farm is safe again!");
        yield return null;
        yield return null;
        Assert.AreEqual("Spooky, but friendly", Hud().Q<Label>("objective-hint").text, "the goal is met");

        yield return Load("Farm");
        Assert.AreEqual(0, EnemyAI.AliveCount, "the farm stays safe when you come back");
        yield return null;
        Assert.AreEqual("Spooky, but friendly", Hud().Q<Label>("objective-hint").text);
    }

    [UnityTest]
    public IEnumerator PippinSellsHotAppleCiderThatWarmsYouUp()
    {
        yield return Load("Farm");
        var player = LevelBootstrap.Current.Player;
        var inventory = player.GetComponent<Inventory>();
        var pippin = Object.FindObjectsByType<Merchant>().Single(m => m.Name == "Pippin");
        Assert.AreEqual("apple_cider", pippin.Ware.Id);
        Assert.IsTrue(pippin.Ware.IsFood, "cider is something you drink");

        Assert.AreEqual("Talk to Pippin", pippin.Prompt);
        pippin.Interact(player);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.IsTrue(pippin.HasMet);
        StringAssert.Contains("Hot Apple Cider", pippin.Prompt);

        StringAssert.Contains("coins", pippin.Buy(inventory), "no coins, no cider");
        GameSession.Progress.AddGold(pippin.Price);
        pippin.Buy(inventory);
        Assert.AreEqual(0, GameSession.Progress.Gold);
        var cider = inventory.Bag.Single(i => i.Id == "apple_cider");

        var health = player.GetComponent<Health>();
        health.TakeDamage(3);
        int before = health.Current;
        Assert.IsTrue(inventory.Eat(cider));
        Assert.Greater(health.Current, before, "a mug of cider warms you right up");
        Assert.IsEmpty(inventory.Bag);
    }

    [UnityTest]
    public IEnumerator TheCornMazeHidesTheHatAtItsCentre()
    {
        yield return Load("Farm");
        var map = Object.FindAnyObjectByType<LevelMap>();
        var player = LevelBootstrap.Current.Player;

        // Find the maze: the box around its corn walls, and the hat inside it.
        int minC = int.MaxValue, maxC = -1, minR = int.MaxValue, maxR = -1, hatC = -1, hatR = -1;
        for (int r = 0; r < map.Height; r++)
            for (int c = 0; c < map.Width; c++)
            {
                if (map.IsWallProp(map.At(c, r)))
                {
                    minC = Mathf.Min(minC, c); maxC = Mathf.Max(maxC, c);
                    minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r);
                }
                if (map.At(c, r) == ']') (hatC, hatR) = (c, r);
            }
        Assert.Greater(maxC - minC, 10, "a big maze");
        Assert.AreEqual(((minC + maxC) / 2, (minR + maxR) / 2), (hatC, hatR), "the hat is at its very centre");

        // Exactly one way in, and from it a path all the way to the hat.
        int openings = 0;
        for (int c = minC; c <= maxC; c++)
            foreach (int r in new[] { minR, maxR })
                if (!map.IsSolid(map.At(c, r))) openings++;
        for (int r = minR + 1; r < maxR; r++)
            foreach (int c in new[] { minC, maxC })
                if (!map.IsSolid(map.At(c, r))) openings++;
        Assert.AreEqual(1, openings, "one way into the maze");
        var seen = new System.Collections.Generic.HashSet<(int, int)> { (hatC, hatR) };
        var queue = new System.Collections.Generic.Queue<(int, int)>(seen);
        bool escaped = false;
        while (queue.Count > 0)
        {
            var (c, r) = queue.Dequeue();
            if (c < minC || c > maxC || r < minR || r > maxR) { escaped = true; break; }
            foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                char next = map.At(c + dc, r + dr);
                if (next == ' ' || map.IsSolid(next) || !seen.Add((c + dc, r + dr))) continue;
                queue.Enqueue((c + dc, r + dr));
            }
        }
        Assert.IsTrue(escaped, "the hat can be reached from outside the maze");

        // The corn is solid: no cutting through the walls.
        var wall = Object.FindObjectsByType<Transform>().First(t => t.name == "CornWall");
        Assert.IsNotNull(wall.GetComponentInChildren<BoxCollider>());
        Assert.Less(wall.GetComponentInChildren<Renderer>().bounds.max.y, 1.5f, "low enough to see the hero over it");

        // The prize: the Jack-o'-Lantern Hat, to wear.
        var hat = Object.FindObjectsByType<ItemPickup>().Single(p => p.Item.Id == "pumpkin_hat");
        StringAssert.Contains("Jack-o'-Lantern Hat", hat.Interact(player));
        var inventory = player.GetComponent<Inventory>();
        Assert.IsTrue(inventory.Equip(hat.Item));
        Assert.AreEqual("pumpkin_hat", inventory.Equipped(EquipSlot.Helm).Id);
    }
}
