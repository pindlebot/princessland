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
            new[]
            {
                "Bed", "Toilet", "Sink", "PaperTowel", "Bathtub", "Faucet", "PottedFern", "PottedMonstera", "TowelShelf",
                "Wardrobe", "Nightstand", "Bookshelf", "ToyChest",
                "Plant", "Plant", "Plant", "Plant", "Plant", "Cat", "CourtyardDoor", "Bench", "Crib",
            },
            names, "the bedroom (with the baby mermaid's crib), the bathroom (the bath's tap, a fern, a monstera, the towel shelf " +
                   "and three more plants) and the courtyard (a potted plant, Whiskers, the old door and a bench)");

        // Out through the front door...
        player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        var door = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Level0");
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

        var toilet = fixtures.Single(f => f.name == "Toilet");
        StringAssert.Contains("sit down", toilet.Interact(player));
        StringAssert.Contains("Flush", toilet.Interact(player));
        Assert.AreEqual("flush", AudioManager.Instance.LastPlayed.name);

        var towel = Fixture(HouseFixture.Effect.DryHands);
        StringAssert.Contains("already dry", towel.Interact(player), "the towel notices you skipped the sink");
        var sink = Fixture(HouseFixture.Effect.WashHands);
        Assert.AreEqual("Pump the soap", sink.Prompt, "soap first...");
        StringAssert.Contains("soap", sink.Interact(player));
        Assert.AreEqual("Rinse your hands", sink.Prompt, "...then rinse");
        StringAssert.Contains("already dry", towel.Interact(player), "soapy hands aren't wet yet");
        StringAssert.Contains("rinse", sink.Interact(player));
        Assert.AreEqual("Pump the soap", sink.Prompt, "and next time, soap again");
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
        var toilet = Object.FindObjectsByType<HouseFixture>().Single(f => f.name == "Toilet");
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
    public IEnumerator TheBathPutsYouInYourSwimwearUntilYouGetOut()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var hero = player.GetComponent<PlayerController>();
        var animator = player.GetComponent<CharacterAnimator>().Animator;
        var tub = Object.FindObjectsByType<HouseFixture>().Single(f => f.Kind == HouseFixture.Effect.Bathe);
        Vector3 beside = tub.transform.position + new Vector3(0f, 1f, -1.5f);
        Teleport(player, beside);
        yield return null;
        Assert.AreEqual("E: Take a bath", Hud().Q<Label>("interact-prompt").text);

        var sprite = player.GetComponent<CharacterAnimator>().SpriteRenderer.transform;
        float spriteHeight = sprite.localPosition.y;
        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        yield return null;
        Assert.IsTrue(hero.IsBathing);
        Assert.IsTrue(hero.IsSeated, "no walking or spells from the bath");
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Bathe"), "lying back in their swimwear");
        Assert.IsFalse(animator.GetBool("Sitting"));
        Assert.Greater(sprite.localPosition.y, spriteHeight + 1f, "up in the tub, not on the floor");
        Assert.AreEqual("splash", AudioManager.Instance.LastPlayed.name);
        Assert.IsFalse(tub.HasBubbles, "no bubble bath in the bag, no mountain of bubbles");
        Assert.AreEqual("E: Get out of the bath", Hud().Q<Label>("interact-prompt").text);

        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        yield return null;
        Assert.IsFalse(hero.IsBathing);
        Assert.IsFalse(hero.IsSeated);
        Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("Bathe"), "dressed again");
        Assert.AreEqual(spriteHeight, sprite.localPosition.y, 0.001f);
        Assert.Less(Vector3.Distance(player.transform.position, beside), 0.1f, "back on the bath mat");

        // Out of the bath, dripping wet: the towel shelf dries you off.
        Assert.IsTrue(GameSession.Flags.Contains(HouseFixture.DrippingFlag));
        var towels = Object.FindObjectsByType<HouseFixture>().Single(f => f.Kind == HouseFixture.Effect.Towel);
        Assert.AreEqual("Grab a towel", towels.Prompt);
        StringAssert.Contains("dry off", towels.Interact(player));
        Assert.IsFalse(GameSession.Flags.Contains(HouseFixture.DrippingFlag));
        StringAssert.DoesNotContain("dry off", towels.Interact(player), "already dry: just towels");
    }

    [UnityTest]
    public IEnumerator BubbleBathMakesAMountainOfBubbles()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var hero = player.GetComponent<PlayerController>();
        var bag = player.GetComponent<Inventory>();
        var bottle = Resources.FindObjectsOfTypeAll<ItemDefinition>().First(i => i.Id == "bubble_bath");
        bag.Add(bottle);
        bag.Add(bottle);
        var tub = Object.FindObjectsByType<HouseFixture>().Single(f => f.Kind == HouseFixture.Effect.Bathe);
        Teleport(player, tub.transform.position + new Vector3(0f, 1f, -1.5f));
        yield return null;

        StringAssert.Contains("BUBBLES", tub.Interact(player));
        Assert.IsTrue(hero.IsBathing);
        Assert.IsTrue(tub.HasBubbles);
        Assert.AreEqual(1, bag.Bag.Count(i => i.Id == "bubble_bath"), "one bottle poured in, one left");
        Assert.AreEqual("bubbles", AudioManager.Instance.LastPlayed.name);
        Assert.AreEqual(1, GameSession.GetCounter("bubbleBaths"));

        tub.Interact(player); // out again: the bubbles go
        Assert.IsFalse(tub.HasBubbles);

        tub.Interact(player); // the second bottle, and this time walk away instead
        Assert.IsTrue(tub.HasBubbles);
        Assert.AreEqual(0, bag.Bag.Count(i => i.Id == "bubble_bath"));
        hero.StandUp();
        yield return null;
        Assert.IsFalse(tub.HasBubbles, "walking out pops them too");

        tub.Interact(player); // no bottles left: just a bath
        Assert.IsFalse(tub.HasBubbles);
        hero.StandUp();
    }

    [UnityTest]
    public IEnumerator TheBathsFaucetTurnsTheWaterOnAndOff()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var fixtures = Object.FindObjectsByType<HouseFixture>();
        var tub = fixtures.Single(f => f.Kind == HouseFixture.Effect.Bathe);
        var faucet = fixtures.Single(f => f.Kind == HouseFixture.Effect.Faucet);
        Assert.Greater(faucet.transform.position.x, tub.transform.position.x + 1f, "at the spout end of the tub");

        // Stand in front of the spout: E there is the faucet, not the bath.
        Teleport(player, faucet.transform.position + new Vector3(0f, 1f, -1.3f));
        yield return null;
        Assert.IsFalse(faucet.IsRunning);
        Assert.AreEqual("E: Turn on the water", Hud().Q<Label>("interact-prompt").text);
        var stream = faucet.transform.parent.Find("Stream");
        Assert.IsFalse(stream.gameObject.activeSelf);

        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        Assert.IsTrue(faucet.IsRunning);
        Assert.IsTrue(stream.gameObject.activeSelf, "the water pours from the spout");
        var water = stream.GetComponent<AudioSource>();
        Assert.IsTrue(water != null && water.loop && water.clip.name == "faucet", "and you can hear it running");
        Assert.AreEqual("tap_turn", AudioManager.Instance.LastPlayed.name);
        Assert.AreEqual("E: Turn off the water", Hud().Q<Label>("interact-prompt").text);

        StringAssert.Contains("Drip", faucet.Interact(player));
        Assert.IsFalse(faucet.IsRunning);
        Assert.IsFalse(stream.gameObject.activeSelf);

        // The middle of the tub is still the bath.
        Teleport(player, tub.transform.position + new Vector3(0f, 1f, -1.5f));
        yield return null;
        Assert.AreEqual("E: Take a bath", Hud().Q<Label>("interact-prompt").text);
    }

    [UnityTest]
    public IEnumerator TheCourtyardIsOutdoorsAndWhiskersLikesBeingPetted()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var map = Object.FindAnyObjectByType<LevelMap>();

        // Through the doorway in the bedroom's west wall (the end of the path, beside the bedroom floor)...
        int doorCol = -1, doorRow = -1;
        for (int row = 0; row < map.Height; row++)
            for (int col = 0; col < map.Width; col++)
                if (map.At(col, row) == '=' && map.At(col + 1, row) == '.') (doorCol, doorRow) = (col, row);
        Teleport(player, new Vector3((doorCol + 1) * map.TileSize, 1f, (map.Height - 1 - doorRow) * map.TileSize));
        var cc = player.GetComponent<CharacterController>();
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            cc.Move(Vector3.left * 6f * Time.deltaTime + Vector3.down);
            yield return null;
        }
        Assert.Less(player.transform.position.x, doorCol * map.TileSize - 0.5f, "out into the courtyard");

        // ...onto grass, in daylight, behind a hedge.
        var cat = Object.FindObjectsByType<HouseFixture>().Single(f => f.name.StartsWith("Cat"));
        StringAssert.StartsWith("Grass", FloorUnder(cat.transform.position).sharedMaterial.name);
        var sun = GameObject.Find("CourtyardSun").GetComponent<Light>();
        Assert.AreEqual(LightType.Spot, sun.type);
        Assert.Greater(Object.FindObjectsByType<Transform>().Count(t => t.name == "Hedge"), 8);

        Assert.AreEqual("Pet Whiskers", cat.Prompt);
        StringAssert.Contains("Purr", cat.Interact(player));
        Assert.AreEqual("meow", AudioManager.Instance.LastPlayed.name);
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
    public IEnumerator TheBedLaysYouDownTuckedInUntilYouGetUp()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var hero = player.GetComponent<PlayerController>();
        var health = player.GetComponent<Health>();
        var bed = Object.FindObjectsByType<HouseFixture>().First(f => f.Kind == HouseFixture.Effect.Rest);
        Vector3 nextToIt = bed.transform.position + new Vector3(0f, 1f, -1.5f);
        Teleport(player, nextToIt);
        health.TakeDamage(2);
        yield return null;
        Assert.AreEqual("E: Take a nap", Hud().Q<Label>("interact-prompt").text);

        var sprite = player.GetComponent<CharacterAnimator>().SpriteRenderer.transform;
        float spriteHeight = sprite.localPosition.y;
        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        yield return null;

        // Lying in the bed, on its left pillow, not standing beside it.
        Assert.IsTrue(hero.IsSleeping);
        Assert.IsTrue(hero.IsSeated, "no walking or spells while you sleep");
        Assert.AreSame(bed.transform, hero.Seat);
        Vector3 across = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
        Vector3 offset = player.transform.position - bed.transform.position;
        Assert.Less(Vector3.Dot(offset, across), -0.4f, "on the bed's left pillow");
        Assert.Less(new Vector2(offset.x, offset.z).magnitude, 1.2f, "in the bed, not beside it");
        Assert.Greater(sprite.localPosition.y, spriteHeight + 0.8f, "up on the quilt");
        Assert.AreEqual(health.Max, health.Current, "tucked in, you're all better");
        var animator = player.GetComponent<CharacterAnimator>().Animator;
        Assert.IsTrue(animator.GetBool("Sleeping"));
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Sleep"), "the hero's lying-down pose, with Zs");
        Assert.AreEqual("E: Get up", Hud().Q<Label>("interact-prompt").text);

        // Pressing E again gets you up, back where you stood.
        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        yield return null;
        Assert.IsFalse(hero.IsSleeping);
        Assert.IsFalse(hero.IsSeated);
        Assert.AreEqual(spriteHeight, sprite.localPosition.y, 0.001f);
        Assert.Less(Vector3.Distance(player.transform.position, nextToIt), 0.1f, "back where you stood");
        Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
        Assert.IsFalse(animator.GetCurrentAnimatorStateInfo(0).IsName("Sleep"));
        Assert.AreEqual("E: Take a nap", Hud().Q<Label>("interact-prompt").text);
    }

    [UnityTest]
    public IEnumerator BothHeroesHaveASleepingPoseAndWalkingAwayWakesThem()
    {
        foreach (var hero in new[] { "wizard", "princess" })
        {
            var path = hero == "wizard" ? "Assets/Art/Wizard.json" : "Assets/Art/Princess.json";
            StringAssert.Contains("\"Sleep\"", System.IO.File.ReadAllText(path), $"{hero} has a Sleep animation");
        }

        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var hero2 = player.GetComponent<PlayerController>();
        var bed = Object.FindObjectsByType<HouseFixture>().First(f => f.Kind == HouseFixture.Effect.Rest);
        bed.Interact(player);
        Assert.IsTrue(hero2.IsSleeping);
        // Walking away (any direction key) stands you up, as it does from the toilet.
        hero2.StandUp();
        Assert.IsFalse(hero2.IsSleeping);
        Assert.IsTrue(player.GetComponent<CharacterController>().enabled);

        // Sitting down somewhere else straight from bed leaves nothing stuck.
        bed.Interact(player);
        var toilet = Object.FindObjectsByType<HouseFixture>().Single(f => f.name == "Toilet");
        toilet.Interact(player);
        Assert.IsFalse(hero2.IsSleeping, "off the bed and onto the toilet");
        Assert.IsTrue(hero2.IsSeated);
        hero2.StandUp();
    }

    [UnityTest]
    public IEnumerator ABabyMermaidSleepsInACribAgainstTheBedroomWall()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var crib = Object.FindObjectsByType<HouseFixture>().Single(f => f.name == "Crib");
        StringAssert.Contains("baby mermaid", crib.Prompt);

        // She's drawn waving and blinking (a looping sprite), solid like the rest of the furniture, and she
        // stands on the bedroom's floor with room to walk up to her.
        Assert.IsNotNull(crib.GetComponentInChildren<SpriteFlipbook>(), "the baby moves");
        Assert.IsTrue(crib.GetComponent<BoxCollider>() != null && !crib.GetComponent<BoxCollider>().isTrigger);
        var map = Object.FindAnyObjectByType<LevelMap>();
        Vector2 tile = map.WorldToMap(crib.transform.position);
        int col = Mathf.RoundToInt(tile.x), row = Mathf.RoundToInt(tile.y);
        Assert.IsFalse(LevelMap.IsWall(map.At(col, row)), "the crib isn't in a wall");
        Assert.IsFalse(LevelMap.IsWall(map.At(col, row + 1)), "there's floor in front of it to stand on");

        // Each peek gives a different giggle, and she giggles.
        var said = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 5; i++)
        {
            said.Add(crib.Interact(player));
            Assert.AreEqual("baby_giggle", AudioManager.Instance.LastPlayed.name);
        }
        CollectionAssert.AllItemsAreUnique(said);
        Assert.IsTrue(said.All(m => !string.IsNullOrEmpty(m) && !m.Contains("|")));
        Assert.AreEqual(said[0], crib.Interact(player), "then round again");
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

    [UnityTest]
    public IEnumerator TheCourtyardHasIvyAWishingWellAndADoorThatWontOpen()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;

        Assert.Greater(Object.FindObjectsByType<Renderer>().Count(r => r.name == "Wall" && r.sharedMaterial.name == "WallSideIvy"), 15,
            "ivy climbs the courtyard walls");
        Assert.IsTrue(Object.FindObjectsByType<Renderer>().Any(r => r.name == "Wall" && r.sharedMaterial.name == "WallSide"),
            "but not the bedroom's");

        // The wishing well grants its own wish, separately from the fountain outside.
        var well = Object.FindAnyObjectByType<WishingFountain>();
        Assert.AreEqual("Make a wish (1 coin)", well.Prompt);
        int gold = GameSession.Progress.Gold;
        GameSession.Progress.AddGold(3);
        string said = "";
        for (int i = 0; i < 3; i++) said = well.Interact(player);
        StringAssert.Contains("The well sparkles", said);
        Assert.IsTrue(GameSession.Flags.Contains("wish:well"));
        Assert.IsFalse(GameSession.Flags.Contains(WishingFountain.GrantedFlag), "the fountain's wish is still to come");
        Assert.AreEqual(gold + WishingFountain.Reward, GameSession.Progress.Gold);
        StringAssert.Contains("The well giggles", well.Interact(player));

        // The old door at the far end: locked, even with the dungeon's Rusty Key, and solid.
        GameSession.Flags.Add(DungeonDoor.KeyFlag);
        var door = Object.FindObjectsByType<HouseFixture>().Single(f => f.name.StartsWith("CourtyardDoor"));
        Assert.AreEqual("Try the old door", door.Prompt);
        StringAssert.Contains("locked", door.Interact(player));
        Assert.AreEqual("door_locked", AudioManager.Instance.LastPlayed.name);
        Teleport(player, door.transform.position + new Vector3(2.2f, 1f, 0f));
        var cc = player.GetComponent<CharacterController>();
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            cc.Move(Vector3.left * 6f * Time.deltaTime + Vector3.down);
            yield return null;
        }
        Assert.Greater(player.transform.position.x, door.transform.position.x + 1f, "it doesn't budge");
    }

    [UnityTest]
    public IEnumerator TheSpiralStairsGoDownToTheKitchenAndBackUp()
    {
        SceneManager.LoadScene("House");
        yield return WaitForScene("House");
        var player = LevelBootstrap.Current.Player;
        var down = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Kitchen");
        Assert.AreEqual("Go down the spiral stairs", down.Prompt);
        down.Interact(player);
        yield return WaitForScene("Kitchen");
        yield return null;

        player = LevelBootstrap.Current.Player;
        var up = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "House");
        Assert.AreEqual("Go up the spiral stairs", up.Prompt);
        Assert.Less(Vector3.Distance(player.transform.position, up.transform.position), 3f, "you arrive at the foot of the stairs");
        StringAssert.Contains("Kitchen", Hud().Q<Label>("objective-title").text);
        Assert.AreEqual("KitchenTile", FloorUnder(player.transform.position).sharedMaterial.name);

        // The pantry and the island hand out ingredients; the stove cooks (CookingTests).
        var fixtures = Object.FindObjectsByType<HouseFixture>();
        CollectionAssert.AreEquivalent(new[] { "Pantry", "Island", "Plant" }, fixtures.Select(f => f.name).ToList());
        Assert.AreEqual("Cook at the stove", Object.FindAnyObjectByType<CraftingStation>().Prompt);
        StringAssert.Contains("flour", fixtures.Single(f => f.name == "Pantry").Prompt);

        up.Interact(player);
        yield return WaitForScene("House");
        yield return null;
        player = LevelBootstrap.Current.Player;
        down = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Kitchen");
        Assert.Less(Vector3.Distance(player.transform.position, down.transform.position), 3f, "back up beside the stairwell");
    }

    private static Renderer FloorUnder(Vector3 position) =>
        Physics.RaycastAll(new Vector3(position.x, 3f, position.z), Vector3.down, 6f)
            .Select(h => h.collider.GetComponent<Renderer>()).First(r => r != null && r.name == "Floor");

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
