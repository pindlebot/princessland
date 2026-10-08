using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for the bigger dungeon: doors, the Rusty Key and the treasure room,
// Bonesy and his campfire.
public class DungeonTests
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
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    [UnityTest]
    public IEnumerator DoorsOpenAndTheKeyOpensTheTreasureRoom()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        var doors = Object.FindObjectsByType<DungeonDoor>();
        Assert.AreEqual(3, doors.Length, "two wooden doors and the locked one");
        var wooden = doors.First(d => !d.IsLocked);
        var locked = doors.Single(d => d.IsLocked);

        Assert.IsTrue(wooden.GetComponent<Collider>().enabled, "a closed door blocks the way");
        wooden.Interact(player);
        Assert.IsTrue(wooden.IsOpen);
        Assert.IsFalse(wooden.GetComponent<Collider>().enabled);

        Assert.AreEqual("Try the door", locked.Prompt);
        StringAssert.Contains("locked", locked.Interact(player));
        Assert.IsFalse(locked.IsOpen);
        Assert.AreEqual("door_locked", AudioManager.Instance.LastPlayed.name);

        var key = Object.FindAnyObjectByType<KeyPickup>();
        Assert.IsNotNull(key, "the key is somewhere in the maze");
        Assert.Greater(Vector3.Distance(key.transform.position, player.transform.position), 30f, "far from the start");
        StringAssert.Contains("Rusty Key", key.Interact(player));
        Assert.AreEqual("Open the door", locked.Prompt);
        StringAssert.Contains("fits", locked.Interact(player));
        Assert.IsTrue(locked.IsOpen);

        // Coming back: doors stay open and the key doesn't reappear.
        yield return Load("Level0");
        yield return Load("Dungeon");
        var again = Object.FindObjectsByType<DungeonDoor>();
        Assert.IsTrue(again.Single(d => d.IsLocked).IsOpen);
        Assert.AreEqual(1, again.Count(d => d.IsOpen && !d.IsLocked));
        Assert.IsNull(Object.FindAnyObjectByType<KeyPickup>());
        Assert.AreEqual(2, Object.FindObjectsByType<Chest>().Length, "the storeroom chest and the treasure chest");
    }

    [UnityTest]
    public IEnumerator BonesyIsFriendlyAndTheCampfireWarmsYouUp()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        var bonesy = Object.FindObjectsByType<Npc>().Single(n => n.Name == "Bonesy");
        Assert.IsNull(bonesy.GetComponent<EnemyAI>(), "he doesn't fight");
        Assert.IsFalse(Object.FindObjectsByType<EnemyAI>().Any(e => Vector3.Distance(e.transform.position, bonesy.transform.position) < 8f),
            "and no monsters near his campfire");
        StringAssert.Contains("don't zap me", bonesy.Next().lines[0].text);

        var fire = Object.FindObjectsByType<HouseFixture>().Single();
        Assert.AreEqual("Warm up by the fire", fire.Prompt);
        var health = player.GetComponent<Health>();
        GameSession.Settings.gentle = false;
        health.TakeDamage(3);
        player.GetComponent<Mana>().TrySpend(20f);
        StringAssert.Contains("toasty", fire.Interact(player));
        Assert.AreEqual(health.Max, health.Current);
        Assert.AreEqual(player.GetComponent<Mana>().Max, player.GetComponent<Mana>().Current, 0.01f);

        // Once you have the key, he tells you where it goes.
        GameSession.Flags.Add("met:Bonesy");
        GameSession.Flags.Add(DungeonDoor.KeyFlag);
        StringAssert.Contains("rusty key", bonesy.Next().lines[0].text);
    }
}
