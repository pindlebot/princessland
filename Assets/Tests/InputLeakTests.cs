using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the hotbar's layout and for input staying where it was meant: a button or click that belongs to a
// menu (or to closing one) never turns into a spell, an ability, a hop or a "talk".
public class InputLeakTests : InputTestFixture
{
    private Keyboard keyboard;
    private Gamepad pad;
    private Mouse mouse;
    private GameObject player;
    private Mana mana;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        pad = InputSystem.AddDevice<Gamepad>();
        mouse = InputSystem.AddDevice<Mouse>();
        ActionFeedback.Reset();
    }

    public override void TearDown()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
        GameInput.OverlayOpen = false;
        base.TearDown();
    }

    private IEnumerator Start(string scene = "Dungeon")
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        player = LevelBootstrap.Current.Player;
        mana = player.GetComponent<Mana>();
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        mana.Refill();
    }

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    private static IEnumerator Frames(int n)
    {
        for (int i = 0; i < n; i++) yield return null;
    }

    private void LearnWholePath()
    {
        GameSession.Progress.AddXp(10000);
        var tree = Object.FindAnyObjectByType<SkillTreeView>();
        foreach (var skill in tree.Path) Assert.IsTrue(tree.TryLearn(skill), skill.Name);
        mana.Refill();
    }

    // Spells cast so far, read from the magic spent (a fireball that hits a wall at once is gone before anyone counts it).
    private int Fireballs() => mana.Current < mana.Max - 1f ? 1 : 0;

    // ---------- The hotbar: spells, abilities and food are three different things ----------

    [Test]
    public void AbilitiesAreOnQAndFAndFoodIsOnTheNumberKeys()
    {
        Assert.AreEqual("Q", GameInput.AbilityKey(2));
        Assert.AreEqual("F", GameInput.AbilityKey(3));
        for (int i = 0; i < 4; i++) Assert.AreEqual((i + 2).ToString(), GameInput.QuickKey(i));
        var keys = new[] { GameInput.AbilityKey(2), GameInput.AbilityKey(3) }.Concat(Enumerable.Range(0, 4).Select(GameInput.QuickKey));
        Assert.AreEqual(6, keys.Distinct().Count(), "no key is used for two slots");
    }

    [UnityTest]
    public IEnumerator TheHotbarKeepsAbilitiesAndFoodInTwoSeparateGroupsThatAppearWhenUsed()
    {
        yield return Start();
        var hud = Hud();
        var abilities = hud.Q("abilities-group");
        var items = hud.Q("items-group");
        Assert.IsNotNull(abilities);
        Assert.IsNotNull(items);
        Assert.AreEqual(abilities, hud.Q("ability-2").parent);
        Assert.AreEqual(abilities, hud.Q("ability-3").parent);
        foreach (var name in new[] { "quick-0", "quick-1", "quick-2", "quick-3" }) Assert.AreEqual(items, hud.Q(name).parent, name);
        Assert.IsFalse(abilities.parent == items || items.parent == abilities, "side by side, not nested");

        Assert.IsFalse(abilities.ClassListContains("unlocked"), "no abilities yet: no empty gap");
        Assert.IsFalse(items.ClassListContains("unlocked"), "no food yet either");

        player.GetComponent<Inventory>().Add("healing_apple");
        yield return null;
        yield return null;
        Assert.IsTrue(items.ClassListContains("unlocked"), "food in the bag: the food group appears");
        Assert.IsFalse(abilities.ClassListContains("unlocked"));

        LearnWholePath();
        yield return null;
        Assert.IsTrue(abilities.ClassListContains("unlocked"));
        Assert.AreEqual("Q", hud.Q("ability-2").Q<Label>(className: "spell-key").text);
        Assert.AreEqual("F", hud.Q("ability-3").Q<Label>(className: "spell-key").text);
        Assert.AreEqual("2", hud.Q("quick-0").Q<Label>(className: "spell-key").text);
    }

    [UnityTest]
    public IEnumerator OnAControllerTheLabelsChangeTogether()
    {
        yield return Start();
        LearnWholePath();
        player.GetComponent<Inventory>().Add("healing_apple");
        Press(pad.buttonSouth);
        yield return null;
        Release(pad.buttonSouth);
        yield return Frames(2);
        var hud = Hud();
        Assert.AreEqual("LB", hud.Q("ability-2").Q<Label>(className: "spell-key").text);
        Assert.AreEqual("LT", hud.Q("ability-3").Q<Label>(className: "spell-key").text);
        Assert.AreEqual("R Up", hud.Q("quick-0").Q<Label>(className: "spell-key").text);

        Press(keyboard.pKey);   // any key: back to the keyboard
        yield return null;
        Release(keyboard.pKey);
        yield return Frames(2);
        Assert.AreEqual("Q", hud.Q("ability-2").Q<Label>(className: "spell-key").text);
    }

    [UnityTest]
    public IEnumerator NumberKeysEatFoodAndQCastsTheAbilityNeverTheOtherWay()
    {
        yield return Start();
        var bag = player.GetComponent<Inventory>();
        var apple = bag.Database.Find("healing_apple");
        bag.Add(apple);
        bag.Add(apple);
        LearnWholePath();
        mana.Refill();

        PressAndRelease(keyboard.digit2Key, queueEventOnly: true);
        yield return Frames(2);
        Assert.AreEqual(1, bag.Count(apple), "2 ate one apple");
        Assert.AreEqual(mana.Max, mana.Current, 0.5f, "and used no magic");

        float before = mana.Current;
        PressAndRelease(keyboard.qKey, queueEventOnly: true);
        yield return Frames(2);
        Assert.Less(mana.Current, before, "Q spent magic on the first ability");
        Assert.AreEqual(1, bag.Count(apple), "and ate nothing");
    }

    // ---------- Menus keep their buttons ----------

    [UnityTest]
    public IEnumerator ACastButtonHeldThroughThePauseMenuDoesNotFireWhenItCloses()
    {
        yield return Start();
        var pause = Object.FindAnyObjectByType<PauseMenu>();
        pause.Open();
        yield return Frames(2);

        Press(keyboard.spaceKey);                  // held down while the menu is open...
        yield return Frames(3);
        Assert.AreEqual(mana.Max, mana.Current, 0.01f, "nothing fires inside the menu");
        pause.Close();
        yield return Frames(6);                    // ...and still held after it shuts
        Assert.AreEqual(0, Fireballs(), "still nothing: it has to be let go first");
        Assert.AreEqual(mana.Max, mana.Current, 0.5f);

        Release(keyboard.spaceKey);
        yield return Frames(2);
        Press(keyboard.spaceKey);
        yield return Frames(3);
        Release(keyboard.spaceKey);
        Assert.Greater(Fireballs(), 0, "a fresh press casts");
    }

    [UnityTest]
    public IEnumerator TheFrameAMenuClosesStillCountsAsBlockedSoResumeDoesNotAlsoHopOrTalk()
    {
        yield return Start();
        var pause = Object.FindAnyObjectByType<PauseMenu>();
        pause.Open();
        yield return Frames(2);
        pause.Close();
        Assert.IsTrue(GameInput.GameplayBlocked, "the closing frame is blocked (B / A both close and act)");
        Assert.IsTrue(GameInput.ActionsBlocked);
        yield return Frames(2);
        Assert.IsFalse(GameInput.GameplayBlocked, "and the next one is free");
    }

    [UnityTest]
    public IEnumerator WhileTheBagIsOpenCastsAbilitiesAndTalkingWaitUntilItCloses()
    {
        yield return Start();
        LearnWholePath();
        var hud = Object.FindAnyObjectByType<HudController>();
        hud.SetInventoryOpen(true);
        yield return Frames(2);
        Assert.IsTrue(GameInput.OverlayOpen);
        Assert.IsTrue(GameInput.ActionsBlocked);
        Assert.IsFalse(GameInput.GameplayBlocked, "(walking and the menu's own keys still work)");

        Press(keyboard.spaceKey);
        yield return Frames(3);
        Release(keyboard.spaceKey);
        yield return Frames(2);                       // (let go for real before the next key, or the test keyboard drops it)
        PressAndRelease(keyboard.qKey, queueEventOnly: true);
        yield return Frames(2);
        Assert.AreEqual(0, Fireballs());
        Assert.AreEqual(mana.Max, mana.Current, 0.5f, "no magic spent");
        hud.SetInventoryOpen(false);
        yield return Frames(2);
        Assert.IsFalse(GameInput.OverlayOpen);
        Press(keyboard.spaceKey);
        yield return Frames(3);
        Release(keyboard.spaceKey);
        Assert.Greater(Fireballs(), 0, "closed: casting works again");
    }

    [UnityTest]
    public IEnumerator TheQuestLogSkillTreeAndHelpAlsoKeepTheirButtons()
    {
        yield return Start();
        var hud = Object.FindAnyObjectByType<HudController>();
        var quests = hud.GetComponent<QuestLogView>();
        var skills = hud.GetComponent<SkillTreeView>();

        quests.SetOpen(true);
        yield return Frames(2);
        Assert.IsTrue(GameInput.ActionsBlocked, "quest log");
        quests.SetOpen(false);
        skills.SetOpen(true);
        yield return Frames(2);
        Assert.IsTrue(GameInput.ActionsBlocked, "skill tree");
        skills.SetOpen(false);
        hud.SetHelpOpen(true);
        yield return Frames(2);
        Assert.IsTrue(GameInput.ActionsBlocked, "help");
        hud.SetHelpOpen(false);
        yield return Frames(2);
        Assert.IsFalse(GameInput.ActionsBlocked);
    }

    [UnityTest]
    public IEnumerator AClickThatStartedOnTheHudDoesNotCastWhenItDragsOntoTheWorld()
    {
        yield return Start();
        // Mouse held down while the bag is open (as if pressed on a slot): the spell must not fire when it closes.
        var hud = Object.FindAnyObjectByType<HudController>();
        hud.SetInventoryOpen(true);
        yield return Frames(2);
        Press(mouse.leftButton);
        yield return Frames(3);
        hud.SetInventoryOpen(false);
        yield return Frames(5);
        Assert.AreEqual(0, Fireballs(), "a held click does not become a spell");
        Release(mouse.leftButton);
        yield return Frames(2);
        Press(mouse.leftButton);
        yield return Frames(4);
        Release(mouse.leftButton);
        Assert.Greater(Fireballs(), 0, "a fresh click in the world casts");
    }
}
