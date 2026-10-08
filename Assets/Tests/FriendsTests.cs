using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for Level 0's new friends and easter eggs: Coralie the mermaid,
// Sir Hopsalot the frog, and the wishing fountain.
public class FriendsTests
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
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
    }

    private static void Teleport(GameObject player, Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = to;
        cc.enabled = true;
    }

    // Talk to an NPC and click through the whole conversation. Returns its first line.
    private static string TalkTo(Npc npc)
    {
        string first = npc.Next().lines[0].text;
        npc.Interact(LevelBootstrap.Current.Player);
        for (int i = 0; i < 100 && DialogueController.IsOpen; i++) DialogueController.Instance.Advance();
        Assert.IsFalse(DialogueController.IsOpen);
        return first;
    }

    private static Npc Coralie() => Object.FindObjectsByType<Npc>().First(n => n.Name == "Coralie");

    [UnityTest]
    public IEnumerator CoralieCanBeReachedFromTheShoreAndTheFrogQuestWorks()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var coralie = Coralie();

        // Walk up to her from dry land: the pond stops you, and she's in reach to talk.
        Teleport(player, coralie.transform.position + new Vector3(4f, 1f, 0f));
        var cc = player.GetComponent<CharacterController>();
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            cc.Move(Vector3.left * 6f * Time.deltaTime + Vector3.down);
            yield return null;
        }
        Assert.Greater(player.transform.position.x, coralie.transform.position.x + 0.8f, "you can't walk into the pond");
        Assert.AreEqual(coralie, player.GetComponent<PlayerInteractor>().Current, "but you can talk to her from the shore");

        StringAssert.Contains("I'm Coralie", TalkTo(coralie));
        Assert.IsTrue(coralie.HasMet);
        StringAssert.Contains("Still no sign", TalkTo(coralie), "until the frog is found, she gives a hint");

        // Rustle the right bush: out hops Sir Hopsalot.
        var bush = Object.FindAnyObjectByType<FrogBush>();
        Assert.AreEqual("Rustle the bush", bush.Prompt);
        StringAssert.Contains("frog", bush.Interact(player));
        Assert.IsFalse(bush.CanInteract);
        yield return null;
        var frog = Object.FindAnyObjectByType<Frog>();
        Assert.IsNotNull(frog);
        Assert.AreEqual("Ribbit!", frog.Interact(player));
        Assert.AreEqual("ribbit", AudioManager.Instance.LastPlayed.name);

        // Tell Coralie: a thank-you and a present.
        int gold = GameSession.Progress.Gold;
        StringAssert.Contains("little frog", TalkTo(coralie));
        Assert.AreEqual(gold + 20, GameSession.Progress.Gold, "her sea-coins");
        var smallTalk = new[] { TalkTo(coralie), TalkTo(coralie), TalkTo(coralie) };
        Assert.AreEqual(3, smallTalk.Distinct().Count(), "after that she takes turns between her chats");

        // He's still there after leaving and coming back.
        yield return Load("Dungeon");
        yield return Load("Level0");
        Assert.IsNotNull(Object.FindAnyObjectByType<Frog>(), "Sir Hopsalot stays out of his bush");
        Assert.IsFalse(Object.FindAnyObjectByType<FrogBush>().CanInteract);
    }

    [UnityTest]
    public IEnumerator TheThirdWishComesTrue()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var fountain = Object.FindAnyObjectByType<WishingFountain>();
        StringAssert.Contains("need a coin", fountain.Interact(player), "no coins, no wish");

        GameSession.Progress.AddGold(3);
        StringAssert.Contains("pony", fountain.Interact(player));
        Assert.AreEqual("plink", AudioManager.Instance.LastPlayed.name);
        StringAssert.Contains("cake", fountain.Interact(player));
        StringAssert.Contains("comes true", fountain.Interact(player));
        Assert.AreEqual(WishingFountain.Reward, GameSession.Progress.Gold, "3 coins in, 15 back");
        Assert.IsTrue(GameSession.Flags.Contains(WishingFountain.GrantedFlag));
        Assert.AreEqual(3, GameSession.GetCounter("wishes"));
        StringAssert.StartsWith("Plink!", fountain.Interact(player), "afterwards it just plinks happily");

        // The dragon noticed.
        var amethyra = Object.FindObjectsByType<Npc>().First(n => n.Name == "Amethyra");
        GameSession.Flags.Add("met:Amethyra");
        bool mentioned = false;
        for (int i = 0; i < 5; i++) mentioned |= TalkTo(amethyra).Contains("fountain sparkle");
        Assert.IsTrue(mentioned, "Amethyra mentions the wish in her small talk");
    }
}
