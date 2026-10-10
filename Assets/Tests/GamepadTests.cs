using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests that drive the game with a pretend gamepad (and keyboard).
// InputTestFixture swaps in a fresh, empty Input System for each test, so the devices
// added here are the only ones the game sees, and pressing them is fully scripted.
public class GamepadTests : InputTestFixture
{
    private Gamepad pad;
    private Keyboard keyboard;

    public override void Setup()
    {
        base.Setup();
        pad = InputSystem.AddDevice<Gamepad>();
        keyboard = InputSystem.AddDevice<Keyboard>();
    }

    public override void TearDown()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
        base.TearDown();
    }

    // Queue a press and release for the game's next frame, the way a real button arrives
    // (PressAndRelease on its own processes them right away, before the game can look).
    private void Tap(ButtonControl button) => PressAndRelease(button, queueEventOnly: true);

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        yield return WaitFor(scene);
    }

    // Wait for a scene the game itself is loading.
    private static IEnumerator WaitFor(string scene)
    {
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    [UnityTest]
    public IEnumerator TheLeftStickWalksAndXCasts()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;

        Vector3 start = player.transform.position;
        Set(pad.leftStick, new Vector2(1f, 0f));
        yield return new WaitForSeconds(0.4f);
        Set(pad.leftStick, Vector2.zero);
        yield return null;
        Assert.Greater(Vector3.Distance(start, player.transform.position), 0.5f, "the stick moved the hero");

        var mana = player.GetComponent<Mana>();
        float before = mana.Current;
        Press(pad.buttonWest);
        yield return null;
        Release(pad.buttonWest);
        yield return null;
        Assert.Less(mana.Current, before, "X cast a spell");
        Assert.AreEqual("X", Object.FindAnyObjectByType<HudController>().SpellKey, "the HUD names the X button");
    }

    [UnityTest]
    public IEnumerator AOpensThingsAndThePromptSaysA()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var chest = Object.FindAnyObjectByType<Chest>();
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = chest.transform.position + new Vector3(0f, 0f, -1.2f);
        cc.enabled = true;

        Tap(pad.leftShoulder); // touching any pad button switches the prompts to gamepad buttons...
        yield return null;
        yield return null;
        Assert.AreEqual("A: Open chest", Hud().Q<Label>("interact-prompt").text);
        Tap(pad.buttonSouth);
        yield return null;
        yield return null;
        Assert.IsTrue(chest.IsOpen, "...and A opens the chest");

        Tap(keyboard.spaceKey); // back on the keyboard, the prompts follow
        yield return null;
        yield return null;
        Assert.AreEqual("Space", Object.FindAnyObjectByType<HudController>().SpellKey);
    }

    [UnityTest]
    public IEnumerator TheTitleAndHeroPickerWorkWithThePad()
    {
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TidecrownPad_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        try
        {
            yield return Load("Title");
            var title = Object.FindAnyObjectByType<TitleController>();
            Assert.AreEqual(0, title.Highlighted);
            Tap(pad.dpad.right);
            yield return null;
            Assert.AreEqual(1, title.Highlighted);

            Tap(pad.buttonSouth); // an empty slot: off to pick a hero
            yield return WaitFor("CharacterSelect");
            Assert.AreEqual(1, GameSession.Slot);
            var select = Object.FindAnyObjectByType<CharacterSelectController>();
            Tap(pad.dpad.right);
            yield return null;
            Assert.AreEqual(1, select.Selected, "d-pad right picks the princess");

            Tap(pad.buttonSouth);
            yield return WaitFor("Level0");
            Assert.AreEqual("Tidal Orb", LevelBootstrap.Current.Player.GetComponent<SpellAbility>().SpellName);
            Assert.AreEqual("Princess", SaveSystem.Peek(1).hero);
        }
        finally
        {
            GameSession.Slot = -1;
            SaveSystem.FolderOverride = null;
            if (System.IO.Directory.Exists(folder)) System.IO.Directory.Delete(folder, true);
        }
    }

    [UnityTest]
    public IEnumerator APressAdvancesTheDragonsConversation()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        Object.FindObjectsByType<Npc>().First(n => n.Name == "Amethyra").Interact(player);
        yield return null;
        yield return null;
        Assert.IsTrue(DialogueController.BlocksInput, "the conversation is open");

        // Each A press finishes the current line, then moves on; keep pressing until it closes.
        for (int i = 0; i < 60 && DialogueController.BlocksInput; i++)
        {
            Tap(pad.buttonSouth);
            yield return null;
            yield return null;
        }
        Assert.IsFalse(DialogueController.BlocksInput, "A carried the whole conversation to its end");
    }

    [UnityTest]
    public IEnumerator RbCyclesTheTargetRbViewOpensHelpAndAFromTheBannerTriesAgain()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        var all = Object.FindObjectsByType<EnemyAI>()
            .OrderBy(e => e.name.StartsWith("Skeleton") ? 0 : 1).ThenBy(e => e.transform.position.x).ToList();
        foreach (var e in all) e.enabled = false;
        for (int i = 2; i < all.Count; i++) Teleport(all[i].gameObject, new Vector3(500f + i * 10f, 0f, 500f));
        Teleport(all[0].gameObject, player.transform.position + new Vector3(3f, 0f, 0f));
        Teleport(all[1].gameObject, player.transform.position + new Vector3(0f, 0f, 5f));
        yield return new WaitForSeconds(0.6f);

        var spell = player.GetComponent<SpellAbility>();
        var first = spell.Target;
        Assert.IsNotNull(first);
        Tap(pad.rightShoulder);
        yield return null;
        yield return null;
        var second = spell.Target;
        Assert.AreNotEqual(first, second, "RB picks the other one");
        Tap(pad.rightShoulder);
        yield return null;
        yield return null;
        Assert.AreEqual(first, spell.Target, "and wraps round");

        var hud = Object.FindAnyObjectByType<HudController>();
        Assert.IsFalse(hud.IsHelpOpen, "RB on its own isn't help any more");
        Assert.AreEqual("RB", Hud().Q<Label>("target-key").text, "the card names the pad's button");

        // Help: hold RB and press View. (View on its own is the skill tree.)
        Press(pad.rightShoulder, queueEventOnly: true);
        yield return null;
        Tap(pad.selectButton);
        yield return null;
        yield return null;
        Assert.IsTrue(hud.IsHelpOpen, "RB + View opens the help panel");
        Assert.AreEqual("RB+View: Help", Hud().Q<Label>("help-pill").text, "and the pill names the chord");
        Tap(pad.selectButton);
        yield return null;
        yield return null;
        Assert.IsFalse(hud.IsHelpOpen);
        Release(pad.rightShoulder, queueEventOnly: true);
        yield return null;

        GameSession.Settings.gentle = false; // Adventurer Mode, so there's a Game Over
        var health = LevelBootstrap.Current.Player.GetComponent<Health>();
        health.TakeDamage(health.Max);
        yield return null;
        yield return null;
        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.AreEqual("A: try again      Start: menu", Hud().Q<Label>("banner-subtitle").text);
        var before = GameManager.Instance;
        Tap(pad.buttonSouth);
        for (int i = 0; i < 10 && GameManager.Instance == before; i++) yield return null;
        yield return null;
        Assert.AreNotEqual(before, GameManager.Instance, "the level started again");
        Assert.IsFalse(GameManager.Instance.IsGameOver);
    }

    [UnityTest]
    public IEnumerator DownOnTheTitleReachesQuit()
    {
        int quits = 0;
        AppQuit.Override = () => quits++;
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TidecrownPad_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder; // no saves, so the highlight starts on slot 1
        try
        {
            yield return Load("Title");
            var title = Object.FindAnyObjectByType<TitleController>();
            Assert.AreEqual(0, title.Highlighted);
            Tap(pad.dpad.right);
            yield return null;
            Tap(pad.dpad.down);
            yield return null;
            Assert.IsTrue(title.QuitHighlighted, "down from the slots lands on Quit");
            Tap(pad.dpad.up);
            yield return null;
            Assert.AreEqual(1, title.Highlighted, "up goes back to the slot you came from");
            Tap(pad.dpad.down);
            yield return null;
            Tap(pad.buttonSouth);
            yield return null;
            Assert.AreEqual(1, quits, "A on Quit closes the game");
        }
        finally
        {
            AppQuit.Override = null;
            SaveSystem.FolderOverride = null;
            if (System.IO.Directory.Exists(folder)) System.IO.Directory.Delete(folder, true);
        }
    }

    [UnityTest]
    public IEnumerator WalkingIntoAGapHopsWithTheBootsAndTheBButtonHopsToo()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var cc = player.GetComponent<CharacterController>();
        void Place(Vector3 at) { cc.enabled = false; player.transform.position = at; cc.enabled = true; }

        // In the corridor, two metres south of the one-tile gap at map (17,3): push north, with and without the boots.
        Place(new Vector3(34f, 1f, 74f));
        Set(pad.leftStick, new Vector2(-0.707f, 0.707f)); // the isometric view's "up" is world north
        yield return new WaitForSeconds(1.5f);
        Assert.Less(player.transform.position.z, 77.2f, "without the boots the gap's wall just stops you");
        Assert.IsFalse(player.GetComponent<PlayerController>().IsHopping);

        GameSession.Inventory.KeyItems.Add(Abilities.BouncyBoots);
        yield return new WaitForSeconds(1.2f);
        Set(pad.leftStick, Vector2.zero);
        yield return null;
        Assert.Greater(player.transform.position.z, 79f, "pushing against the gap with the boots on hops straight across");
        Assert.AreEqual(1, GameSession.GetCounter("hops"));

        // And back again with the B button, facing the gap.
        Place(new Vector3(34f, 1f, 79.6f));
        player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        yield return null;
        Tap(pad.buttonEast);
        yield return new WaitForSeconds(1f);
        Assert.Less(player.transform.position.z, 77f, "B hops back over it");
        Assert.AreEqual(2, GameSession.GetCounter("hops"));
    }

    [UnityTest]
    public IEnumerator TheMKeyOpensTheWorldMapAndTheMenuHasAWorldMapRow()
    {
        yield return Load("Level0");
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var view = WorldMapView.Instance;

        Tap(keyboard.mKey);
        yield return null;
        yield return null;
        Assert.IsTrue(view.IsOpen, "M opens the world map");
        Assert.IsFalse(PauseMenu.IsOpen);
        Tap(keyboard.escapeKey);
        yield return null;
        yield return null;
        Assert.IsFalse(view.IsOpen, "Esc puts it away");
        Assert.IsFalse(PauseMenu.IsOpen, "and doesn't also open the pause menu");
        Assert.AreEqual(1f, Time.timeScale);

        // From the pause menu (Start), the World map row.
        Tap(pad.startButton);
        yield return null;
        yield return null;
        Assert.IsTrue(PauseMenu.IsOpen);
        for (int i = 0; i < 4; i++) { Tap(pad.dpad.down); yield return null; yield return null; }
        Assert.AreEqual(4, Object.FindAnyObjectByType<PauseMenu>().Highlighted, "World map sits between Mode and Save");
        Tap(pad.buttonSouth);
        yield return null;
        yield return null;
        Assert.IsTrue(view.IsOpen);
        Assert.IsFalse(PauseMenu.IsOpen);
        Tap(pad.buttonEast);
        yield return null;
        yield return null;
        Assert.IsFalse(view.IsOpen, "B closes it");
        Assert.AreEqual(1f, Time.timeScale);
    }
}
