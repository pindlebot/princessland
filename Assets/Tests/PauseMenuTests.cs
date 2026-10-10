using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the pause menu, driven by a pretend keyboard and gamepad.
public class PauseMenuTests : InputTestFixture
{
    private Gamepad pad;
    private Keyboard keyboard;
    private string folder;

    public override void Setup()
    {
        base.Setup();
        pad = InputSystem.AddDevice<Gamepad>();
        keyboard = InputSystem.AddDevice<Keyboard>();
        folder = Path.Combine(Path.GetTempPath(), "TidecrownPause_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
    }

    public override void TearDown()
    {
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        SaveSystem.FolderOverride = null;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        Time.timeScale = 1f;
        base.TearDown();
    }

    private void Tap(ButtonControl button) => PressAndRelease(button, queueEventOnly: true);

    // Frames, not seconds: the game is paused, so WaitForSeconds would never finish.
    private static IEnumerator Frames(int n = 2)
    {
        for (int i = 0; i < n; i++) yield return null;
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        yield return WaitFor(scene);
    }

    private static IEnumerator WaitFor(string scene)
    {
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return Frames();
    }

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    [UnityTest]
    public IEnumerator EscPausesAndTheMenuChangesSettings()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var menu = Object.FindAnyObjectByType<PauseMenu>();
        var screen = Hud().Q("pause-screen");
        Assert.AreEqual(DisplayStyle.None, screen.resolvedStyle.display, "hidden while playing");

        Tap(keyboard.escapeKey);
        yield return Frames();
        Assert.IsTrue(PauseMenu.IsOpen);
        Assert.AreEqual(0f, Time.timeScale, "time stops");
        Assert.AreEqual(DisplayStyle.Flex, screen.resolvedStyle.display);
        Assert.AreEqual(0, menu.Highlighted, "starts on Keep playing");

        // Gameplay buttons do nothing while paused.
        var mana = player.GetComponent<Mana>();
        float before = mana.Current;
        Tap(pad.buttonWest);
        yield return Frames();
        Assert.AreEqual(before, mana.Current, "no casting from the pause menu");

        // Down twice to Sounds, then left: one step quieter.
        Tap(keyboard.downArrowKey);
        yield return Frames();
        Tap(pad.dpad.down);
        yield return Frames();
        Assert.AreEqual(2, menu.Highlighted);
        Tap(keyboard.leftArrowKey);
        yield return Frames();
        Assert.AreEqual(GameSettings.VolumeSteps - 1, GameSession.Settings.soundVolume);
        Assert.AreEqual(GameSettings.VolumeSteps, GameSession.Settings.musicVolume, "music untouched");
        var lit = Hud().Q("pause-sounds-value").Query(className: "pause-dot").Where(d => d.ClassListContains("on")).ToList();
        Assert.AreEqual(GameSettings.VolumeSteps - 1, lit.Count, "one dot went out");

        // Down to Mode; A switches Gentle to Adventurer.
        Assert.IsTrue(GameSession.Settings.gentle, "Gentle Mode is the default");
        Tap(pad.dpad.down);
        yield return Frames();
        Tap(pad.buttonSouth);
        yield return Frames();
        Assert.IsFalse(GameSession.Settings.gentle);
        Assert.AreEqual("Adventurer", Hud().Q<Label>("pause-mode-value").text);

        // Start closes it again and time runs.
        Tap(pad.startButton);
        yield return Frames();
        Assert.IsFalse(PauseMenu.IsOpen);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator SaveAndGoToTitleKeepsTheSettingsAndWhereYouCameIn()
    {
        GameSession.Slot = 0;
        GameSession.NextSpawn = "FromHouse"; // as if she'd just come out of her front door
        yield return Load("Level0");
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        Assert.AreEqual("FromHouse", GameSession.EnteredBy);
        GameSession.Settings.musicVolume = 2;
        GameSession.Progress.AddGold(9);

        Tap(pad.startButton);
        yield return Frames();
        Tap(pad.dpad.up); // up from the top row stays on it...
        yield return Frames();
        for (int i = 0; i < 5; i++) { Tap(pad.dpad.down); yield return Frames(); }
        Assert.AreEqual(5, Object.FindAnyObjectByType<PauseMenu>().Highlighted, "...and down five reaches Save and go to title (past World map)");
        Tap(pad.buttonSouth);
        yield return WaitFor("Title");

        Assert.AreEqual(1f, Time.timeScale, "the title screen isn't frozen");
        Assert.IsFalse(PauseMenu.IsOpen);
        var save = SaveSystem.Peek(0);
        Assert.AreEqual("Level0", save.scene);
        Assert.AreEqual("FromHouse", save.spawn, "continuing puts her back outside her front door");
        Assert.AreEqual(9, save.gold);
        Assert.AreEqual(2, save.settings.musicVolume, "the settings are saved with the slot");
        Assert.IsTrue(save.settings.gentle);
    }

    [UnityTest]
    public IEnumerator SaveAndQuitSavesThenClosesTheGame()
    {
        int quits = 0;
        AppQuit.Override = () => quits++;
        try
        {
            GameSession.Slot = 1;
            yield return Load("Dungeon");
            foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
            GameSession.Progress.AddGold(5);

            Tap(keyboard.escapeKey);
            yield return Frames();
            for (int i = 0; i < 7; i++) { Tap(keyboard.downArrowKey); yield return Frames(); }
            Assert.AreEqual(6, Object.FindAnyObjectByType<PauseMenu>().Highlighted, "the last row, past the end stays put");
            Tap(keyboard.enterKey);
            yield return Frames();

            Assert.AreEqual(1, quits, "asked to close the game");
            Assert.IsFalse(PauseMenu.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            var save = SaveSystem.Peek(1);
            Assert.AreEqual("Dungeon", save.scene, "saved first");
            Assert.AreEqual(5, save.gold);
        }
        finally
        {
            AppQuit.Override = null;
        }
    }

    [UnityTest]
    public IEnumerator TheTitleScreenHasAQuitButton()
    {
        int quits = 0;
        AppQuit.Override = () => quits++;
        try
        {
            yield return Load("Title");
            var root = Object.FindAnyObjectByType<TitleController>().GetComponent<UIDocument>().rootVisualElement;
            var quit = root.Q<Button>("quit");
            Assert.AreEqual(DisplayStyle.Flex, quit.resolvedStyle.display);
            using (var click = NavigationSubmitEvent.GetPooled())
            {
                click.target = quit;
                quit.SendEvent(click);
            }
            yield return Frames();
            Assert.AreEqual(1, quits);
        }
        finally
        {
            AppQuit.Override = null;
        }
    }
}
