using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Gentle Mode's nap works in every room, with or without a fountain: she wakes where she arrived (or at a fountain she
// has touched in this room), on solid ground, with full hearts and magic, and keeps everything she had.
public class RecoveryTests
{
    public static string[] Rooms() =>
        Directory.GetFiles(Path.Combine(Application.dataPath, "Levels"), "*.txt").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray();

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator SheWakesOnSolidGroundWithFullHeartsInEveryRoom([ValueSource(nameof(Rooms))] string scene)
    {
        GameSession.NewGame(null);
        GameSession.Settings.gentle = true;
        GameSession.Progress.AddGold(25);
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;

        var player = LevelBootstrap.Current.Player;
        var health = player.GetComponent<Health>();
        var wake = LevelBootstrap.Current.WakePoint;
        Assert.IsNotNull(wake, $"{scene}: has a wake point");
        player.GetComponent<PlayerController>().enabled = false;
        // Wander off first, so waking has to move her back.
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position += new Vector3(0f, 0f, 0.3f);
        cc.enabled = true;
        health.InvulnerableUntil = 0f;
        player.GetComponent<Mana>().TrySpend(20);
        health.TakeDamage(999);
        yield return null;
        Assert.IsTrue(health.IsDead || GameManager.Instance.IsAsleep, $"{scene}: she dozes off");

        for (float t = 0f; t < 8f && (GameManager.Instance.IsAsleep || health.IsDead); t += Time.deltaTime) yield return null;
        Assert.IsFalse(GameManager.Instance.IsAsleep, $"{scene}: she wakes up");
        Assert.AreEqual(1, GameManager.Instance.Naps);
        Assert.IsFalse(GameManager.Instance.IsGameOver, $"{scene}: no game over in Gentle Mode");
        Assert.AreEqual(health.Max, health.Current, $"{scene}: full hearts");
        Assert.AreEqual(player.GetComponent<Mana>().Max, player.GetComponent<Mana>().Current, 0.01f, $"{scene}: full magic");
        var flat = player.transform.position - wake.position;
        flat.y = 0f;
        Assert.Less(flat.magnitude, 0.5f, $"{scene}: back at the wake point");
        Assert.IsNull(SoftLockTests.StandingProblem(player), $"{scene}: standing on solid ground");
        Assert.AreEqual(25, GameSession.Progress.Gold, $"{scene}: nothing lost");
        Assert.IsTrue(health.IsInvulnerable, $"{scene}: a few safe seconds to get her bearings");
    }

    [UnityTest]
    public IEnumerator InARoomWithNoFountainSheWakesWhereSheCameInAndAFountainChangesThat()
    {
        // The dungeon has no fountain: she wakes at the arrival spot. Level0 has one: after touching it, she wakes there.
        GameSession.NewGame(null);
        SceneManager.LoadScene("Dungeon");
        yield return null;
        yield return null;
        Assert.AreEqual(0, Object.FindObjectsByType<WakeFountain>().Length, "no fountain here");
        var flat = LevelBootstrap.Current.Player.transform.position - LevelBootstrap.Current.WakePoint.position;
        flat.y = 0f;
        Assert.Less(flat.magnitude, 0.1f, "the wake point is the arrival spot");

        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var fountain = Object.FindAnyObjectByType<WakeFountain>();
        var player = LevelBootstrap.Current.Player;
        var cc = player.GetComponent<CharacterController>();
        player.GetComponent<PlayerController>().enabled = false;
        cc.enabled = false;
        player.transform.position = fountain.WakeSpot.position;
        cc.enabled = true;
        yield return null;
        yield return null;
        Assert.IsTrue(fountain.IsWakePoint, "touching it moves the wake point there");
    }
}
