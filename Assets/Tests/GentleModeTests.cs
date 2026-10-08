using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for Gentle Mode (the default) and Adventurer Mode.
public class GentleModeTests
{
    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
    }

    private static IEnumerator Load(string scene)
    {
        GameSession.NewGame(null);
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

    [UnityTest]
    public IEnumerator MonstersHitForHalfDamage()
    {
        yield return Load("Dungeon");
        Assert.IsTrue(GameSession.Settings.gentle, "Gentle Mode is the default");
        var health = LevelBootstrap.Current.Player.GetComponent<Health>();
        int full = health.Current;

        health.TakeDamage(1);
        Assert.AreEqual(full, health.Current, "the first bump costs no heart...");
        health.TakeDamage(1);
        Assert.AreEqual(full - 1, health.Current, "...the second one does: half damage");
        health.TakeDamage(4);
        Assert.AreEqual(full - 3, health.Current);
    }

    [UnityTest]
    public IEnumerator AtZeroHeartsSheNapsAndWakesByTheFountainWithEverythingKept()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var health = player.GetComponent<Health>();
        var start = LevelBootstrap.Current.WakePoint;
        Assert.IsNotNull(start, "she wakes where she came in until she finds a fountain");

        // Walk past the fountain: it becomes the wake point.
        var fountain = Object.FindAnyObjectByType<WakeFountain>();
        Assert.IsNotNull(fountain, "Level 0 has a fountain");
        Teleport(player, fountain.transform.position + new Vector3(3f, 0f, -1f));
        yield return null;
        Assert.IsTrue(fountain.IsWakePoint);

        // Beat a monster (its coins fly to her), then wander off and get worn out.
        var skeletons = Object.FindObjectsByType<EnemyAI>();
        int monsters = skeletons.Length;
        skeletons[0].GetComponent<Health>().TakeDamage(99);
        yield return new WaitForSeconds(2f); // its star-puff finishes and the coins arrive
        GameSession.Progress.AddGold(17);
        int gold = GameSession.Progress.Gold;
        Teleport(player, fountain.transform.position + new Vector3(-10f, 0f, -6f));
        yield return null;
        player.GetComponent<Mana>().TrySpend(30f);
        health.TakeDamage(999);
        yield return null;

        var game = GameManager.Instance;
        Assert.IsTrue(game.IsAsleep);
        Assert.IsFalse(game.IsGameOver, "no Game Over in Gentle Mode");
        var hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
        Assert.IsFalse(hud.Q("banner").ClassListContains("visible"), "no 'Oh no!' banner");
        Assert.AreEqual("rest", AudioManager.Instance.LastPlayed.name, "a lullaby, not the sad tune");
        Assert.IsFalse(player.GetComponent<PlayerInteractor>().TryInteract(), "asleep: can't do things");

        // Partway through, the screen has faded to the dreamy purple.
        float deepest = 0f;
        for (float t = 0f; t < 6f && game.IsAsleep; t += Time.deltaTime)
        {
            deepest = Mathf.Max(deepest, hud.Q("sleep-fade").resolvedStyle.opacity);
            yield return null;
        }
        Assert.IsFalse(game.IsAsleep, "she woke up");
        Assert.Greater(deepest, 0.95f, "the screen faded out fully while she napped");
        Assert.AreEqual(0f, hud.Q("sleep-fade").resolvedStyle.opacity, 0.01f, "and back in");

        Assert.AreEqual(health.Max, health.Current, "all hearts back");
        var mana = player.GetComponent<Mana>();
        Assert.AreEqual(mana.Max, mana.Current, 0.01f, "and all her magic");
        Assert.Less(Vector3.Distance(player.transform.position, fountain.transform.position), 3f, "by the fountain");
        Assert.AreEqual(gold, GameSession.Progress.Gold, "coins kept");
        Assert.AreEqual(monsters - 1, Object.FindObjectsByType<EnemyAI>().Length, "the beaten monster stays beaten");
        Assert.AreEqual(1, game.Naps);

        // A moment of safety right after waking up.
        health.TakeDamage(2);
        Assert.AreEqual(health.Max, health.Current);
        Assert.IsTrue(game.PlayerCanAct);
    }

    [UnityTest]
    public IEnumerator AdventurerModeKeepsTheClassicRules()
    {
        yield return Load("Dungeon");
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        float gentleWindup = boss.WindupSeconds;

        GameSession.Settings.gentle = false;
        Assert.AreEqual(gentleWindup / BossAbilities.GentleWindupFactor, boss.WindupSeconds, 0.001f,
            "the boss's warning circle is quicker for grown-ups");

        var health = LevelBootstrap.Current.Player.GetComponent<Health>();
        int full = health.Current;
        health.TakeDamage(1);
        Assert.AreEqual(full - 1, health.Current, "full damage");
        health.TakeDamage(99);
        yield return null;
        Assert.IsTrue(GameManager.Instance.IsGameOver);
        Assert.IsFalse(GameManager.Instance.IsAsleep);
        Assert.IsFalse(GameManager.Instance.PlayerWon);
    }
}
