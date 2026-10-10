using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the first session (ROADMAP B1): the title screen's Continue and erase safety, the hero
// cards' hearts and orbs, the one-step-at-a-time coach, the visible goal, and the coach never leaking
// from one save into another. Saves go to a temporary folder.
public class FirstSessionTests
{
    private string folder;

    [SetUp]
    public void UseTemporaryFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "TidecrownFirstSession_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        GameSession.NewGame(null);
    }

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        SaveSystem.FolderOverride = null;
        HudController.IdleSeconds = 40f;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
    }

    private static VisualElement Root() => Object.FindAnyObjectByType<UIDocument>().rootVisualElement;

    private static void Teleport(GameObject player, Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = to;
        cc.enabled = true;
    }

    // ---------- The steps themselves ----------

    [Test]
    public void TheStepsGoInOrderAndTurningTipsBackOnStartsOver()
    {
        Assert.AreEqual(FirstStep.Walk, FirstSteps.Current);
        FirstSteps.Complete(FirstStep.Spell); // out of order is fine: the coach just skips what's done
        FirstSteps.Complete(FirstStep.Walk);
        Assert.AreEqual(FirstStep.Talk, FirstSteps.Current);
        FirstSteps.Complete(FirstStep.Talk);
        Assert.AreEqual(FirstStep.Treasure, FirstSteps.Current, "the spell was already done");
        FirstSteps.Complete(FirstStep.Treasure);
        FirstSteps.Complete(FirstStep.Obstacle);
        Assert.AreEqual(FirstStep.Done, FirstSteps.Current);

        FirstSteps.SetEnabled(false);
        Assert.IsFalse(FirstSteps.Enabled);
        FirstSteps.SetEnabled(true);
        Assert.IsTrue(FirstSteps.Enabled);
        Assert.AreEqual(FirstStep.Walk, FirstSteps.Current, "replaying the tips starts again from the first step");
    }

    [Test]
    public void ANewGameNeverInheritsTheLastGamesCoachState()
    {
        FirstSteps.Complete(FirstStep.Walk);
        FirstSteps.Complete(FirstStep.Talk);
        FirstSteps.SetEnabled(false);
        GameSession.NewGame(null); // what picking a hero for a second save does
        Assert.IsTrue(FirstSteps.Enabled);
        Assert.AreEqual(FirstStep.Walk, FirstSteps.Current);
    }

    [Test]
    public void TheCoachStateIsSavedWithTheSlotAndStaysApartFromOtherSlots()
    {
        FirstSteps.Complete(FirstStep.Walk);
        FirstSteps.Complete(FirstStep.Talk);
        SaveSystem.Save(0, "Level0", "");
        GameSession.NewGame(null);
        FirstSteps.SetEnabled(false);
        SaveSystem.Save(1, "Level0", "");

        GameSession.NewGame(null);
        SaveSystem.Load(0, new CharacterDefinition[0]);
        Assert.IsTrue(FirstSteps.Enabled);
        Assert.AreEqual(FirstStep.Spell, FirstSteps.Current, "slot 1 remembers where its coach was");
        SaveSystem.Load(1, new CharacterDefinition[0]);
        Assert.IsFalse(FirstSteps.Enabled, "slot 2 had its tips turned off");
        Assert.AreEqual(FirstStep.Walk, FirstSteps.Current, "and none of slot 1's steps leaked in");
    }

    [Test]
    public void AnAdventureAlreadyUnderWayIsNotCoachedFromTheTop()
    {
        GameSession.Flags.Add("met:Coralie"); // a save from before the coach existed
        Assert.IsTrue(FirstSteps.SkipIfExperienced());
        Assert.AreEqual(FirstStep.Done, FirstSteps.Current);

        GameSession.NewGame(null);
        Assert.IsFalse(FirstSteps.SkipIfExperienced(), "a brand new game still gets the coach");
        Assert.AreEqual(FirstStep.Walk, FirstSteps.Current);
    }

    [Test]
    public void TheGoalSaysWhoToGreetAndFallsBackWhenNobodyIsLeft()
    {
        Assert.AreEqual("Say hello to Coralie", FirstSteps.Goal(FirstStep.Talk, "Coralie"));
        Assert.AreEqual("Say hello to someone friendly", FirstSteps.Goal(FirstStep.Talk, ""));
        Assert.AreEqual("", FirstSteps.Goal(FirstStep.Done, ""));
    }

    // ---------- Title ----------

    private static void SaveHero(int slot, string hero, string scene, int gold, CharacterDefinition[] heroes)
    {
        GameSession.NewGame(heroes.First(h => h.name == hero));
        GameSession.Progress.AddGold(gold);
        SaveSystem.Save(slot, scene, "");
    }

    [UnityTest]
    public IEnumerator ContinueNamesTheHeroAndPlaceAndTheStatusLineSaysWhatEnterDoes()
    {
        yield return Load("Title");
        var heroes = Object.FindAnyObjectByType<TitleController>().Heroes;
        SaveHero(0, "Wizard", "Level0", 5, heroes);
        yield return new WaitForSecondsRealtime(0.05f);
        SaveHero(2, "Princess", "Dungeon", 25, heroes);
        GameSession.NewGame(null);

        yield return Load("Title");
        var title = Object.FindAnyObjectByType<TitleController>();
        var root = Root();
        StringAssert.Contains("Princess Marina", root.Q<Button>("continue").text);
        StringAssert.Contains("The Dungeon", root.Q<Button>("continue").text);
        Assert.AreEqual(2, title.Highlighted, "the first focus is on the adventure Continue would open");
        Assert.IsTrue(root.Q("slot-2").ClassListContains("selected"), "...and it's visibly highlighted");
        Assert.IsTrue(root.Q("continue").ClassListContains("selected"));
        StringAssert.Contains("continues adventure 3", root.Q<Label>("status").text);
        Assert.AreEqual("Continue", root.Q("slot-2").Q<Label>("slot-tag").text);
        Assert.AreEqual("+ New adventure", root.Q("slot-1").Q<Label>("slot-tag").text);

        title.Highlight(1);
        StringAssert.Contains("starts a new adventure in slot 2", root.Q<Label>("status").text);
        Assert.IsFalse(root.Q("continue").ClassListContains("selected"), "Enter wouldn't continue from an empty slot");
    }

    [UnityTest]
    public IEnumerator EraseIsTwoStepsWithAWarningAndMovingAwayTakesItBack()
    {
        yield return Load("Title");
        var heroes = Object.FindAnyObjectByType<TitleController>().Heroes;
        SaveHero(0, "Wizard", "Level0", 5, heroes);
        GameSession.NewGame(null);
        yield return Load("Title");
        var title = Object.FindAnyObjectByType<TitleController>();
        var root = Root();
        var erase = root.Q("slot-0").Q<Button>("slot-erase");

        title.Erase(0); // the Erase button's click
        yield return null;
        Assert.AreEqual("Really erase?", erase.text);
        Assert.IsTrue(root.Q("slot-0").ClassListContains("erasing"));
        StringAssert.Contains("gone for good", root.Q<Label>("status").text);
        Assert.IsTrue(SaveSystem.Exists(0), "one click erases nothing");

        title.Highlight(1); // look somewhere else: the question is withdrawn
        Assert.AreEqual("Erase", erase.text);
        Assert.IsFalse(root.Q("slot-0").ClassListContains("erasing"));
        Assert.IsTrue(SaveSystem.Exists(0));
    }

    // ---------- Hero select ----------

    [UnityTest]
    public IEnumerator HeroCardsShowHealthAsHeartsAndMagicAsOrbsAndSayBothCanGoEverywhere()
    {
        yield return Load("CharacterSelect");
        var select = Object.FindAnyObjectByType<CharacterSelectController>();
        var root = Root();
        for (int i = 0; i < select.Characters.Length; i++)
        {
            var hero = select.Characters[i];
            var health = hero.Prefab.GetComponent<Health>();
            var mana = hero.Prefab.GetComponent<Mana>();
            Assert.AreEqual(health.Max, root.Q($"card-{i}").Q("card-hearts").childCount, hero.DisplayName + " hearts");
            Assert.AreEqual(Mathf.RoundToInt(mana.Max / 10f), root.Q($"card-{i}").Q("card-magic").childCount, hero.DisplayName + " orbs");
            Assert.IsNotEmpty(root.Q($"card-{i}").Q<Label>("card-spell-name").text);
        }
        var counts = select.Characters.Select((_, i) => root.Q($"card-{i}").Q("card-hearts").childCount).Distinct();
        Assert.Greater(counts.Count(), 1, "the heroes really do differ at a glance");
        StringAssert.Contains("everywhere", root.Q<Label>("both-heroes").text);

        select.Select(1); // the chosen hero's spell icon pulses
        bool pulsed = false;
        var icon = root.Q("card-1").Q("card-spell-icon");
        for (float t = 0f; t < 2f && !pulsed; t += Time.unscaledDeltaTime)
        {
            pulsed = icon.ClassListContains("cast");
            yield return null;
        }
        Assert.IsTrue(pulsed);
    }

    // ---------- In the castle grounds ----------

    private IEnumerator LoadCastle()
    {
        yield return Load("Level0");
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
    }

    [UnityTest]
    public IEnumerator TheObjectiveCardShowsTheGoalAndWhyTheStairsAreClosed()
    {
        yield return LoadCastle();
        FirstSteps.Complete(FirstStep.Walk);
        yield return null;
        var root = Root();
        StringAssert.StartsWith("Say hello to ", root.Q<Label>("objective-goal").text);
        Assert.AreEqual("The stairs open when every monster is beaten", root.Q<Label>("objective-hint").text);

        FirstSteps.Complete(FirstStep.Talk);
        yield return null;
        Assert.AreEqual("Zap a monster with your magic", root.Q<Label>("objective-goal").text);

        FirstSteps.SetEnabled(false);
        yield return null;
        Assert.AreEqual(DisplayStyle.None, root.Q<Label>("objective-goal").resolvedStyle.display, "tips off: no goal line");
    }

    [UnityTest]
    public IEnumerator DoingEachThingFinishesItsStep()
    {
        yield return LoadCastle();
        var player = LevelBootstrap.Current.Player;
        Assert.AreEqual(FirstStep.Walk, FirstSteps.Current);

        Object.FindObjectsByType<Chest>().First(c => !c.IsOpen).Interact(player);
        Assert.IsTrue(FirstSteps.IsDone(FirstStep.Treasure), "a chest is the first treasure");

        Object.FindAnyObjectByType<Bramble>().Clear(SpellElement.Fire);
        Assert.IsTrue(FirstSteps.IsDone(FirstStep.Obstacle), "clearing the thorns is the first obstacle");

        var npc = Object.FindObjectsByType<Npc>().First(n => n.Next() != null);
        npc.Interact(player);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.IsTrue(FirstSteps.IsDone(FirstStep.Talk), "any conversation is the first hello");

        var spell = player.GetComponent<SpellAbility>();
        Assert.IsTrue(spell.TryCast());
        Assert.IsTrue(FirstSteps.IsDone(FirstStep.Spell));
    }

    [UnityTest]
    public IEnumerator CoraliesIntroductionExplainsWhyTheStairsAreShut()
    {
        yield return LoadCastle();
        var coralie = Object.FindObjectsByType<Npc>().First(n => n.Name == "Coralie");
        var lines = coralie.Next().lines;
        Assert.IsTrue(lines.Any(l => l.text.Contains("stairs") && l.text.Contains("beaten")));
    }

    // ---------- Idle help ----------

    [UnityTest]
    public IEnumerator AfterAWhileWithNoProgressAHintAppearsAndGHidesItForGood()
    {
        HudController.IdleSeconds = 0.2f;
        yield return Load("House");
        var player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        var hud = Object.FindAnyObjectByType<HudController>();
        var hint = Root().Q<Label>("context-hint");

        Teleport(player, player.transform.position + new Vector3(3f, 0f, 3f)); // so the "walk" hint is out of the way
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(hint.ClassListContains("visible"), "nothing happened for a while, so the coach speaks up");
        StringAssert.Contains("hide tips", hint.text);

        hud.ToggleTips(); // G
        yield return null;
        Assert.IsFalse(hint.ClassListContains("visible"), "dismissed");
        Assert.IsFalse(FirstSteps.Enabled);
        SaveSystem.Save(0, "House", "");
        Assert.IsTrue(SaveSystem.Peek(0).flags.Contains(FirstSteps.OffFlag), "and it stays dismissed in the save");

        hud.ToggleTips(); // G again: replay
        Assert.IsTrue(FirstSteps.Enabled);
        Assert.AreEqual(FirstStep.Walk, FirstSteps.Current);
    }
}
