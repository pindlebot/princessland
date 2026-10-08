using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for moving between scenes: character select -> Level 0 -> the dungeon.
public class LevelFlowTests
{
    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f; // in case a test stopped mid-conversation
    }

    [UnityTest]
    public IEnumerator ChoosingThePrincessStartsLevel0AsHer()
    {
        SceneManager.LoadScene("CharacterSelect");
        yield return null;
        yield return null;

        var select = Object.FindAnyObjectByType<CharacterSelectController>();
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        Assert.IsTrue(root.Q("card-0").ClassListContains("selected"), "the first hero starts selected");

        select.Select(1);
        Assert.IsTrue(root.Q("card-1").ClassListContains("selected"));
        Assert.IsFalse(root.Q("card-0").ClassListContains("selected"));
        Assert.AreEqual("Princess Marina", root.Q("card-1").Q<Label>("card-name").text);

        select.StartGame();
        yield return WaitForScene("Level0");
        yield return null; // let the HUD's Start run

        var player = LevelBootstrap.Current.Player;
        Assert.AreEqual("Tidal Orb", player.GetComponent<SpellAbility>().SpellName);
        Assert.AreEqual(6, player.GetComponent<Health>().Max);
        Assert.IsFalse(player.transform.Find("Torch").gameObject.activeSelf, "no carried torch in daylight");

        var hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
        Assert.AreEqual("Princess Marina", hud.Q<Label>("player-name").text);
        Assert.AreEqual("The Castle Grounds", hud.Q<Label>("objective-title").text);
        Assert.AreEqual("8", hud.Q<Label>("spell-cost").text);
        Assert.AreEqual("music_castle", AudioManager.Instance.Music.name);
    }

    [UnityTest]
    public IEnumerator Level0ExitOpensOnlyOnceTheSkeletonsAreDefeated()
    {
        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;

        var player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        var exit = Object.FindAnyObjectByType<ExitZone>();
        var crystal = exit.transform.Find("Crystal").gameObject;
        var hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

        Assert.AreEqual(6, EnemyAI.AliveCount, "4 skeletons and 2 slimes");
        Assert.IsFalse(exit.IsOpen);
        Assert.IsFalse(crystal.activeSelf, "the crystal is hidden while the way is locked");

        // Standing on the locked exit does nothing.
        Teleport(player, exit.transform.position);
        for (int i = 0; i < 10; i++) yield return null;
        Assert.AreEqual("Level0", SceneManager.GetActiveScene().name);
        Assert.AreEqual("6 monsters left", hud.Q<Label>("enemies-left").text);

        foreach (var enemy in Object.FindObjectsByType<EnemyAI>())
            enemy.GetComponent<Health>().TakeDamage(99);
        yield return null;
        Assert.IsTrue(exit.IsOpen);
        Assert.IsTrue(hud.Q("toast").ClassListContains("visible"), "the HUD announces the way is open");
        Assert.AreEqual("stairs_open", AudioManager.Instance.LastPlayed.name, "with a cheerful chime");

        // Still standing in it, so it takes us down into the dungeon.
        yield return WaitForScene("Dungeon");
    }

    [UnityTest]
    public IEnumerator Level0HasTheCastle()
    {
        SceneManager.LoadScene("Level0");
        yield return null;

        var castle = GameObject.Find("Castle");
        Assert.IsNotNull(castle);
        int roofs = 0, flags = 0;
        foreach (Transform part in castle.transform)
        {
            if (part.name == "Roof") roofs++;
            if (part.name.StartsWith("Flag")) flags++;
        }
        Assert.AreEqual(5, roofs, "four towers and the keep");
        Assert.AreEqual(1, flags);
    }

    [UnityTest]
    public IEnumerator TalkingToTheDragonPausesTheGameAndPlaysHerIntroduction()
    {
        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;

        var player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var dragon = Object.FindObjectsByType<Npc>().First(n => n.Name == "Amethyra");
        var hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

        float dragonHeight = dragon.GetComponentInChildren<SpriteRenderer>().bounds.size.y;
        float heroHeight = player.transform.Find("Sprite").GetComponent<SpriteRenderer>().bounds.size.y;
        Assert.Greater(dragonHeight, heroHeight * 1.8f, "the dragon should tower over the hero");

        Teleport(player, dragon.transform.position + new Vector3(0f, 1f, -1.6f));
        yield return null;
        yield return null;
        Assert.AreEqual("E: Talk to Amethyra", hud.Q<Label>("interact-prompt").text);

        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        Assert.IsTrue(DialogueController.IsOpen);
        Assert.AreEqual(0f, Time.timeScale, "the game pauses during the conversation");
        yield return null;
        Assert.IsTrue(hud.Q("dialogue").ClassListContains("open"));
        Assert.AreEqual("Amethyra", hud.Q<Label>("dialogue-speaker").text);
        Assert.IsTrue(dragon.IsTalking, "her mouth moves while her line types out");
        Assert.IsFalse(hud.Q("interact-prompt").ClassListContains("visible"));

        // Read the whole conversation: the first Advance finishes typing, the second moves on.
        int lines = 0;
        bool heroSpoke = false;
        string transcript = "";
        while (DialogueController.IsOpen && lines < 20)
        {
            DialogueController.Instance.Advance();
            transcript += hud.Q<Label>("dialogue-text").text + "\n";
            heroSpoke |= hud.Q<Label>("dialogue-speaker").text == "Aldric the Wizard";
            DialogueController.Instance.Advance();
            lines++;
            yield return null;
        }
        Assert.AreEqual(6, lines);
        Assert.IsTrue(heroSpoke, "the hero answers back");
        StringAssert.Contains("I am Amethyra", transcript);
        StringAssert.Contains("Aldric", transcript);
        StringAssert.DoesNotContain("{hero}", transcript);
        Assert.AreEqual(1f, Time.timeScale, "unpaused afterwards");
        Assert.IsTrue(dragon.HasMet);

        // Talking again gives the short version.
        yield return null;
        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        DialogueController.Instance.Advance();
        DialogueController.Instance.Advance();
        Assert.IsFalse(DialogueController.IsOpen, "the later chat is a single line");
    }

    private static IEnumerator WaitForScene(string name)
    {
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != name; t += Time.unscaledDeltaTime)
            yield return null;
        Assert.AreEqual(name, SceneManager.GetActiveScene().name);
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
