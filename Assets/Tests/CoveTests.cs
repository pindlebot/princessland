using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for Mermaid Cove: the dark mermaids' bolts, fighting across the water, the
// rowboats between the castle grounds and the cove, and clearing the cove.
public class CoveTests
{
    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        yield return Arrive(scene);
    }

    // Waits for a scene something else is loading (a door), then switches the monsters off.
    private static IEnumerator Arrive(string scene)
    {
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

    // The middle of a map tile, at standing height.
    private static Vector3 TileCenter(int col, int row)
    {
        var map = Object.FindAnyObjectByType<LevelMap>();
        return new Vector3(col * map.TileSize, 1f, (map.Height - 1 - row) * map.TileSize);
    }

    private static EnemyAI MermaidNear(Vector3 at) =>
        Object.FindObjectsByType<EnemyAI>().Where(e => e.Stationary)
              .OrderBy(e => Vector3.Distance(e.transform.position, at)).First();

    // The end of the lagoon pier (col 11, row 5) is a couple of tiles from the dark mermaid at (8, 6).
    private static Vector3 PierEnd => TileCenter(11, 5);

    [UnityTest]
    public IEnumerator ADarkMermaidThrowsBoltsFromTheWaterThatHurt()
    {
        yield return Load("Cove");
        var player = LevelBootstrap.Current.Player;
        Teleport(player, PierEnd);
        var mermaid = MermaidNear(PierEnd);
        Vector3 start = mermaid.transform.position;
        var health = player.GetComponent<Health>();
        int hits = 0;
        health.Damaged += _ => hits++;

        mermaid.enabled = true;
        bool sawBolt = false;
        for (float t = 0f; t < 4f && hits == 0; t += Time.deltaTime)
        {
            sawBolt |= Object.FindAnyObjectByType<EnemyBolt>() != null;
            yield return null;
        }
        Assert.IsTrue(sawBolt, "she throws a bolt");
        Assert.Greater(hits, 0, "the bolt hurts the hero");
        Assert.Less(Vector3.Distance(start, mermaid.transform.position), 0.01f, "she stays in her spot in the water");
    }

    [UnityTest]
    public IEnumerator SpellsFlyOverTheWaterToReachADarkMermaid()
    {
        yield return Load("Cove");
        var player = LevelBootstrap.Current.Player;
        Teleport(player, PierEnd);
        var mermaid = MermaidNear(PierEnd);
        var mermaidHealth = mermaid.GetComponent<Health>();
        int before = mermaidHealth.Current;

        var prefab = (Projectile)typeof(SpellAbility)
            .GetField("projectilePrefab", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(player.GetComponent<SpellAbility>());
        Vector3 from = player.transform.position + Vector3.up * 0.2f;
        Vector3 aim = mermaid.transform.position - from;
        aim.y = 0f;
        Object.Instantiate(prefab, from, Quaternion.LookRotation(aim)).Launch(1);
        for (float t = 0f; t < 1.5f && mermaidHealth.Current == before; t += Time.deltaTime) yield return null;
        Assert.AreEqual(before - 1, mermaidHealth.Current, "the spell crosses the water (the shore's invisible wall doesn't stop it)");
    }

    [UnityTest]
    public IEnumerator DarkMermaidsCantBeShovedOutOfTheWater()
    {
        yield return Load("Cove");
        var mermaid = MermaidNear(PierEnd);
        Vector3 start = mermaid.transform.position;
        HeroAbility.Push(mermaid, Vector3.right, 3f); // what the Flame Wave, Meteor and Whirlpool do
        yield return null;
        Assert.Less(Vector3.Distance(start, mermaid.transform.position), 0.01f);
    }

    [UnityTest]
    public IEnumerator TheRowboatsLinkTheCastleGroundsAndTheCove()
    {
        yield return Load("Level0");
        var toCove = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Cove");
        StringAssert.Contains("Mermaid Cove", toCove.Prompt);
        toCove.Interact(LevelBootstrap.Current.Player);
        yield return Arrive("Cove");

        var back = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Level0");
        var player = LevelBootstrap.Current.Player;
        Assert.Less(Vector3.Distance(player.transform.position, back.Position), 2.5f, "you arrive on the jetty, by the boat");
        back.Interact(player);
        yield return Arrive("Level0");

        player = LevelBootstrap.Current.Player;
        var boat = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Cove");
        Assert.Less(Vector3.Distance(player.transform.position, boat.Position), 2.5f, "and back by the pond's rowboat");
    }

    [UnityTest]
    public IEnumerator ClearingTheCoveOpensTheSeaCaveStairAndPearlSaysThankYou()
    {
        yield return Load("Cove");
        var exit = Object.FindAnyObjectByType<ExitZone>();
        Assert.IsFalse(exit.IsOpen, "sealed while pirates and dark mermaids are about");
        Assert.AreEqual(12, EnemyAI.AliveCount, "six pirates and six dark mermaids");

        foreach (var enemy in EnemyAI.Alive.ToList()) enemy.GetComponent<Health>().TakeDamage(100);
        yield return null;
        yield return null;
        Assert.IsTrue(exit.IsOpen);
        Assert.IsTrue(GameSession.Flags.Contains("cleared:Cove"));

        var pearl = Object.FindObjectsByType<Npc>().Single(n => n.Name == "Pearl");
        Assert.AreEqual(30, pearl.Next().giveGold, "her thank-you comes first, with a present");
        int gold = GameSession.Progress.Gold;
        pearl.Interact(LevelBootstrap.Current.Player);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.AreEqual(gold + 30, GameSession.Progress.Gold);
    }
}
