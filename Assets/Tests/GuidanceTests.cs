using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Tests for quest and map guidance: the HUD tracker (giver portrait, step, progress, where to go), choosing which
// quest to follow, and the world map's doors, locked stairs and the star on the followed quest's room, none of which
// may give away what hasn't been found.
public class GuidanceTests
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
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    // ---------- Quest data ----------

    [Test]
    public void EveryQuestStepSaysWhereItHappensAndThePlaceIsARealRoom()
    {
        var rooms = System.IO.Directory.GetFiles(System.IO.Path.Combine(Application.dataPath, "Levels"), "*.txt")
            .Select(System.IO.Path.GetFileNameWithoutExtension).ToList();
        foreach (var quest in QuestCatalog.All)
            foreach (var step in quest.Steps)
            {
                Assert.IsNotEmpty(step.Where, $"{quest.Id}: '{step.Text}' has a place");
                CollectionAssert.Contains(rooms, step.Where, $"{quest.Id}: {step.Where} is a room");
            }
    }

    [Test]
    public void TheTrackedQuestIsTheOneChosenElseTheNewest()
    {
        GameSession.NewGame(null);
        Assert.IsNull(QuestCatalog.Tracked(), "nothing started");
        GameSession.Flags.Add("met:Coralie");
        Assert.AreEqual("frog", QuestCatalog.Tracked().Id);
        GameSession.Flags.Add("met:Pearl");
        Assert.AreEqual("cove", QuestCatalog.Tracked().Id, "the newest in the log's order");
        QuestCatalog.Track(QuestCatalog.Find("frog"));
        Assert.AreEqual("frog", QuestCatalog.Tracked().Id, "unless the player chose");
        QuestCatalog.Track(QuestCatalog.Find("cove"));
        Assert.AreEqual(1, GameSession.Flags.Count(f => f.StartsWith(QuestCatalog.TrackFlagPrefix)), "only one is followed");
        Assert.AreEqual("Cove", QuestCatalog.WhereNext(QuestCatalog.Find("cove")));
    }

    // ---------- The tracker on the HUD ----------

    [UnityTest]
    public IEnumerator TheTrackerStaysHiddenUntilAQuestStartsThenShowsEverythingTogether()
    {
        GameSession.NewGame(null);
        yield return Load("Level0");
        var hud = Hud();
        var tracker = hud.Q("tracker");
        Assert.IsFalse(tracker.ClassListContains("visible"), "no quests, no tracker");

        GameSession.Flags.Add("met:Coralie");
        yield return null;
        yield return null;
        Assert.IsTrue(tracker.ClassListContains("visible"));
        Assert.AreEqual("Where's Sir Hopsalot?", hud.Q<Label>("tracker-title").text);
        StringAssert.Contains("frog", hud.Q<Label>("tracker-step").text);
        Assert.AreEqual("You're here!", hud.Q<Label>("tracker-where").text, "the frog is in this room");
        Assert.IsNotNull(hud.Q("tracker-portrait").style.backgroundImage.value.sprite, "Coralie's portrait");
        Assert.IsNotNull(hud.Q("tracker-next").style.backgroundImage.value.sprite, "a picture of what to do");
        var pips = hud.Q("tracker-pips").Children().ToList();
        Assert.AreEqual(2, pips.Count);
        Assert.IsFalse(pips[0].ClassListContains("done"));

        // The first step done: the pips, the step and the place move on, to the way back to the giver.
        GameSession.Flags.Add(FrogBush.FoundFlag);
        yield return null;
        yield return null;
        pips = hud.Q("tracker-pips").Children().ToList();
        Assert.IsTrue(pips[0].ClassListContains("done"));
        StringAssert.Contains("Tell Coralie", hud.Q<Label>("tracker-step").text);

        // Finished: it goes away (nothing left to do), or moves to the next quest.
        GameSession.Flags.Add("thanked:frog");
        yield return null;
        yield return null;
        Assert.IsFalse(tracker.ClassListContains("visible"));
    }

    [UnityTest]
    public IEnumerator TheTrackerPointsToAnotherRoomAndShowsCounterProgress()
    {
        GameSession.NewGame(null);
        GameSession.Flags.Add("met:Old Moss");
        yield return Load("Level0");
        var hud = Hud();
        Assert.AreEqual("Go to: Whispering Woods", hud.Q<Label>("tracker-where").text.Replace("Go to: Whispering Woods", "Go to: Whispering Woods"));
        GameSession.AddToCounter("trees_woken", 2);
        yield return null;
        yield return null;
        StringAssert.Contains("(2/4)", hud.Q<Label>("tracker-step").text, "progress count shows");
        Assert.AreEqual("Go to: Whispering Woods", hud.Q<Label>("tracker-where").text);
    }

    [UnityTest]
    public IEnumerator ClickingAQuestInTheLogFollowsItAndClickingTheTrackerOpensTheLog()
    {
        GameSession.NewGame(null);
        GameSession.Flags.Add("met:Coralie");
        GameSession.Flags.Add("met:Pearl");
        yield return Load("Level0");
        var hud = Hud();
        var log = Object.FindAnyObjectByType<QuestLogView>();
        Assert.AreEqual("The Pirates' Spell", hud.Q<Label>("tracker-title").text, "newest first");

        log.SetOpen(true);
        yield return null;
        yield return null;
        var cards = hud.Query(className: "quest-card").ToList();
        Assert.AreEqual(2, cards.Count);
        var frogCard = cards.First(c => c.Q<Label>(className: "quest-title").text.StartsWith("Where"));
        using (var e = ClickEvent.GetPooled())
        {
            e.target = frogCard;
            frogCard.SendEvent(e);
        }
        yield return null;
        yield return null;
        Assert.AreEqual("frog", QuestCatalog.Tracked().Id);
        Assert.AreEqual("Where's Sir Hopsalot?", hud.Q<Label>("tracker-title").text);
        Assert.IsTrue(GameSession.Flags.Contains("track:frog"), "remembered (flags are saved)");
        log.SetOpen(false);

        using (var e = ClickEvent.GetPooled())
        {
            e.target = hud.Q("tracker");
            hud.Q("tracker").SendEvent(e);
        }
        yield return null;
        Assert.IsTrue(log.IsOpen);
    }

    // ---------- The world map ----------

    [UnityTest]
    public IEnumerator TheMapMarksDoorsGoldWhereTheyLeadSomewhereNewAndGreyTheStairsThatAreWaiting()
    {
        GameSession.NewGame(null);
        yield return Load("Level0");
        var map = WorldMapView.Instance;
        map.Open();
        yield return null;
        Assert.IsTrue(map.Drawn.Contains("room:Level0"));
        Assert.IsTrue(map.Drawn.Any(d => d.StartsWith("exit:Level0>Cove") && d.EndsWith(":new")), "the way to the cove: somewhere new");
        Assert.IsTrue(map.Drawn.Contains("locked:Level0>Dungeon"), "the stairs wait for the monsters");
        map.Close();
        yield return null;

        GameSession.Flags.Add("visited:Cove");
        GameSession.Flags.Add("cleared:Level0");
        map.Open();
        yield return null;
        Assert.IsFalse(map.Drawn.Any(d => d.StartsWith("exit:Level0>Cove") && d.EndsWith(":new")), "visited: no longer new");
        Assert.IsFalse(map.Drawn.Contains("locked:Level0>Dungeon"), "cleared: the stairs are open");
        Assert.IsTrue(map.Drawn.Contains("exit:Level0>Dungeon:new"));
        map.Close();
    }

    [UnityTest]
    public IEnumerator TheMapOnlyStarsAFollowedQuestsRoomOnceYouHaveBeenThere()
    {
        GameSession.NewGame(null);
        GameSession.Flags.Add("met:Pearl");
        yield return Load("Level0");
        var map = WorldMapView.Instance;
        var legend = Hud().Q<Label>("world-legend");

        map.Open();
        yield return null;
        Assert.IsFalse(map.Drawn.Any(d => d.StartsWith("goal:")), "the cove has not been seen: no star, nothing given away");
        StringAssert.Contains("Following: The Pirates' Spell", legend.text);
        map.Close();
        yield return null;

        GameSession.Flags.Add("visited:Cove");
        map.Open();
        yield return null;
        Assert.IsTrue(map.Drawn.Contains("goal:Cove"), "a star on the room");
        StringAssert.Contains("Mermaid Cove", legend.text);
        map.Close();
    }

    [UnityTest]
    public IEnumerator TheMapNeverMarksEggsOrSecretsBecauseOfAQuest()
    {
        GameSession.NewGame(null);
        GameSession.Flags.Add("thanked:amethyst");   // the egg quest starts
        yield return Load("Level0");
        var map = WorldMapView.Instance;
        map.Open();
        yield return null;
        Assert.IsFalse(map.Drawn.Any(d => d.StartsWith("egg:")), "an egg is only marked after you have seen a gap in its room");
        map.Close();
    }
}
