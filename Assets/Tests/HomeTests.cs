using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the hero's home inside the castle.
public class HomeTests
{
    [TearDown]
    public void Reset() => GameSession.NewGame(null);

    [UnityTest]
    public IEnumerator CastleGateLeadsHomeAndTheFrontDoorLeadsBackToTheGate()
    {
        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;

        var gate = Object.FindObjectsByType<SceneDoor>().First(d => d.TargetScene == "House");
        Vector3 gatePosition = gate.transform.position;
        Teleport(player, gatePosition + new Vector3(0f, 0f, -1.2f));
        yield return null;
        yield return null;
        Assert.AreEqual("E: Enter the castle", Hud().Q<Label>("interact-prompt").text);

        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return WaitForScene("House");
        yield return null;

        Assert.AreEqual("Aldric's Home", Hud().Q<Label>("objective-title").text);
        Assert.AreEqual(DisplayStyle.None, Hud().Q("objective-monsters").style.display.value, "no monster count indoors");
        var kinds = Object.FindObjectsByType<HouseFixture>().Select(f => f.Kind).ToList();
        CollectionAssert.AreEquivalent(
            new[] { HouseFixture.Effect.Rest, HouseFixture.Effect.None, HouseFixture.Effect.WashHands, HouseFixture.Effect.DryHands },
            kinds, "a bed, a toilet, a sink and a paper towel");

        // Out through the front door...
        player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        var door = Object.FindAnyObjectByType<SceneDoor>();
        Teleport(player, door.transform.position + new Vector3(0f, 1f, 1f));
        yield return null;
        yield return null;
        Assert.AreEqual("E: Go back outside", Hud().Q<Label>("interact-prompt").text);
        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return WaitForScene("Level0");

        // ...arriving just outside the castle gate, not back at the start.
        var outside = LevelBootstrap.Current.Player.transform.position;
        Assert.Less(Vector3.Distance(outside, gatePosition), 3f);
    }

    [UnityTest]
    public IEnumerator BedRestsYouAndTheBathroomWorks()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var fixtures = Object.FindObjectsByType<HouseFixture>();
        HouseFixture Fixture(HouseFixture.Effect kind) => fixtures.First(f => f.Kind == kind);

        var health = player.GetComponent<Health>();
        var mana = player.GetComponent<Mana>();
        health.TakeDamage(3);
        mana.TrySpend(30f);
        StringAssert.Contains("nap", Fixture(HouseFixture.Effect.Rest).Interact(player));
        Assert.AreEqual(health.Max, health.Current);
        Assert.AreEqual(mana.Max, mana.Current, 0.01f);

        StringAssert.Contains("Flush", Fixture(HouseFixture.Effect.None).Interact(player));
        Assert.AreEqual("flush", AudioManager.Instance.LastPlayed.name);

        var towel = Fixture(HouseFixture.Effect.DryHands);
        StringAssert.Contains("already dry", towel.Interact(player), "the towel notices you skipped the sink");
        Fixture(HouseFixture.Effect.WashHands).Interact(player);
        StringAssert.Contains("Lovely and clean", towel.Interact(player));
    }

    [UnityTest]
    public IEnumerator ClearedGroundsStayClearedAfterAVisitHome()
    {
        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;
        foreach (var enemy in Object.FindObjectsByType<EnemyAI>())
            enemy.GetComponent<Health>().TakeDamage(99);
        yield return null;
        Assert.IsTrue(Object.FindAnyObjectByType<ExitZone>().IsOpen);

        SceneManager.LoadScene("House");
        yield return WaitForScene("House");
        GameSession.NextSpawn = "FromHouse";
        SceneManager.LoadScene("Level0");
        yield return WaitForScene("Level0");
        yield return null;
        yield return null;

        Assert.AreEqual(0, EnemyAI.AliveCount, "the skeletons don't come back");
        Assert.IsTrue(Object.FindAnyObjectByType<ExitZone>().IsOpen);
        Assert.IsFalse(Hud().Q("toast").ClassListContains("visible"), "no second 'the way opened!' announcement");
    }

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    private static IEnumerator WaitForScene(string name)
    {
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != name; t += Time.unscaledDeltaTime)
            yield return null;
        Assert.AreEqual(name, SceneManager.GetActiveScene().name);
        yield return null;
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
