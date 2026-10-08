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
        var names = Object.FindObjectsByType<HouseFixture>().Select(f => f.name).ToList();
        CollectionAssert.AreEquivalent(
            new[] { "Bed", "Toilet", "Sink", "PaperTowel", "Wardrobe", "Nightstand", "Bookshelf", "ToyChest", "Plant" },
            names, "the bedroom and bathroom furniture");

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

        var toilet = Fixture(HouseFixture.Effect.Sit);
        StringAssert.Contains("sit down", toilet.Interact(player));
        StringAssert.Contains("Flush", toilet.Interact(player));
        Assert.AreEqual("flush", AudioManager.Instance.LastPlayed.name);

        var towel = Fixture(HouseFixture.Effect.DryHands);
        StringAssert.Contains("already dry", towel.Interact(player), "the towel notices you skipped the sink");
        Fixture(HouseFixture.Effect.WashHands).Interact(player);
        StringAssert.Contains("Lovely and clean", towel.Interact(player));
    }

    [UnityTest]
    public IEnumerator YouCanSitOnTheToiletAndFlushToGetUp()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var hero = player.GetComponent<PlayerController>();
        var toilet = Object.FindObjectsByType<HouseFixture>().First(f => f.Kind == HouseFixture.Effect.Sit);
        Vector3 nextToIt = toilet.transform.position + new Vector3(0f, 1f, -1.4f);
        Teleport(player, nextToIt);
        yield return null;
        Assert.AreEqual("E: Sit on the toilet", Hud().Q<Label>("interact-prompt").text);

        var sprite = player.GetComponent<CharacterAnimator>().SpriteRenderer.transform;
        float spriteHeight = sprite.localPosition.y;
        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        yield return null;
        Assert.IsTrue(hero.IsSeated);
        Assert.AreSame(toilet.transform, hero.Seat);
        Vector3 offset = player.transform.position - toilet.transform.position;
        Assert.Less(new Vector2(offset.x, offset.z).magnitude, 0.6f, "on the toilet, not beside it");
        Assert.Greater(sprite.localPosition.y, spriteHeight + 0.5f, "up on the seat, feet dangling");
        Assert.IsTrue(player.GetComponent<CharacterAnimator>().Animator.GetCurrentAnimatorStateInfo(0).IsName("Sit"));
        Assert.AreEqual("E: Flush and stand up", Hud().Q<Label>("interact-prompt").text);

        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        yield return null;
        Assert.IsFalse(hero.IsSeated);
        Assert.AreEqual("flush", AudioManager.Instance.LastPlayed.name);
        Assert.AreEqual(spriteHeight, sprite.localPosition.y, 0.001f);
        Assert.Less(Vector3.Distance(player.transform.position, nextToIt), 0.1f, "back where you stood");
        Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
        Assert.IsFalse(player.GetComponent<CharacterAnimator>().Animator.GetCurrentAnimatorStateInfo(0).IsName("Sit"));
    }

    [UnityTest]
    public IEnumerator TheBedroomLampAndToysWork()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var fixtures = Object.FindObjectsByType<HouseFixture>();

        var lamp = fixtures.First(f => f.Kind == HouseFixture.Effect.Lamp);
        var light = lamp.GetComponentInChildren<Light>();
        Assert.IsTrue(light.enabled, "the lamp starts on");
        Assert.AreEqual("Turn the lamp off", lamp.Prompt);
        StringAssert.Contains("off", lamp.Interact(player));
        Assert.IsFalse(light.enabled);
        Assert.AreEqual("Turn the lamp on", lamp.Prompt);
        lamp.Interact(player);
        Assert.IsTrue(light.enabled);

        // The toy chest has a different toy each time, then starts over.
        var toys = fixtures.First(f => f.name == "ToyChest");
        var said = Enumerable.Range(0, 4).Select(_ => toys.Interact(player)).ToList();
        Assert.AreEqual(3, said.Take(3).Distinct().Count());
        Assert.AreEqual(said[0], said[3]);
        Assert.IsFalse(said.Any(s => s.Contains("|")));
    }

    [UnityTest]
    public IEnumerator TheBedStandsClearOfTheWalls()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var map = Object.FindAnyObjectByType<LevelMap>();
        var bed = Object.FindObjectsByType<HouseFixture>().First(f => f.Kind == HouseFixture.Effect.Rest);

        // The bed is drawn upright, facing the camera, so it reaches out sideways across the
        // screen: check both ends of the drawing (the art is ~3.3 units wide), and the
        // collider's corners, all land on floor tiles.
        Vector3 across = Camera.main.transform.right;
        across.y = 0f;
        var points = new[] { bed.transform.position + across.normalized * 1.65f, bed.transform.position - across.normalized * 1.65f }
            .Concat(Corners(bed.GetComponent<BoxCollider>().bounds));
        foreach (var p in points)
        {
            Vector2 tile = map.WorldToMap(p);
            char c = map.At(Mathf.RoundToInt(tile.x), Mathf.RoundToInt(tile.y));
            Assert.IsFalse(LevelMap.IsWall(c), $"the bed reaches into a wall at {p}");
        }
    }

    private static Vector3[] Corners(Bounds b) => new[]
    {
        new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z),
        new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.max.z),
    };

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
