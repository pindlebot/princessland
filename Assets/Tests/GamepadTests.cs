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
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IsoDungeonPad_" + System.Guid.NewGuid().ToString("N"));
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
        Object.FindAnyObjectByType<DragonNpc>().Interact(player);
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
}
