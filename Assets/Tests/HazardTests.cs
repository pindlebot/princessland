using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Tests for the dungeon's floor hazards: lava ('~') and spike traps ('^'), both Hazard.cs.
public class HazardTests
{
    private GameObject player;
    private Health health;

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
    }

    // The dungeon with its monsters switched off, in Adventurer Mode (one point of damage = one heart).
    private IEnumerator LoadDungeon()
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene("Dungeon");
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != "Dungeon"; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        health = player.GetComponent<Health>();
        GameSession.Settings.gentle = false;
    }

    private void Teleport(Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = new Vector3(to.x, 1f, to.z);
        cc.enabled = true;
    }

    private static Hazard[] All(Hazard.Kind kind) =>
        Object.FindObjectsByType<Hazard>().Where(h => h.Type == kind).OrderBy(h => h.transform.position.x).ToArray();

    [Test]
    public void SpikesGoHiddenThenPeekThenUpOnALoop()
    {
        Assert.AreEqual(Hazard.SpikeState.Hidden, Hazard.StateAt(0.1f));
        Assert.AreEqual(Hazard.SpikeState.Warning, Hazard.StateAt(Hazard.HiddenSeconds + 0.1f));
        Assert.AreEqual(Hazard.SpikeState.Up, Hazard.StateAt(Hazard.HiddenSeconds + Hazard.WarningSeconds + 0.1f));
        Assert.AreEqual(Hazard.SpikeState.Hidden, Hazard.StateAt(Hazard.CycleSeconds + 0.1f), "and round again");
        Assert.AreEqual(Hazard.SpikeState.Up, Hazard.StateAt(-0.1f), "negative times (phases) wrap too");
    }

    [UnityTest]
    public IEnumerator TheDungeonHasLavaPoolsAndSpikeTraps()
    {
        yield return LoadDungeon();
        Assert.AreEqual(18, All(Hazard.Kind.Lava).Length, "two lava pools of 9 tiles in the Slime King's hall");
        Assert.AreEqual(5, All(Hazard.Kind.Spikes).Length, "two traps in the tunnel, three in the treasure corridor");
    }

    [UnityTest]
    public IEnumerator LavaHurtsAtOnceThenOncePerSecondWhileYouStayIn()
    {
        yield return LoadDungeon();
        var lava = All(Hazard.Kind.Lava)[0];
        var safe = player.transform.position;
        int full = health.Current;

        Teleport(lava.transform.position);
        yield return null;
        Assert.AreEqual(full - 1, health.Current, "stepping into lava stings right away");
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(full - 1, health.Current, "...but not every frame");
        yield return new WaitForSeconds(0.7f);
        Assert.AreEqual(full - 2, health.Current, "staying in keeps hurting");

        Teleport(safe);
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(full - 2, health.Current, "out of the lava, no more hurt");
    }

    [UnityTest]
    public IEnumerator CrossingSeveralLavaTilesCostsOneHeartNotOneEach()
    {
        yield return LoadDungeon();
        var pool = All(Hazard.Kind.Lava).Take(3).ToArray();
        int full = health.Current;
        foreach (var tile in pool)
        {
            Teleport(tile.transform.position);
            yield return null;
        }
        Assert.AreEqual(full - 1, health.Current);
    }

    [UnityTest]
    public IEnumerator SpikesOnlyHurtWhileTheyAreUp()
    {
        yield return LoadDungeon();
        var trap = All(Hazard.Kind.Spikes)[0];
        var spikes = trap.transform.Find("Spikes");

        // Wait for the spikes to come up and go down again, then step on right as they hide.
        yield return new WaitForSeconds(trap.SecondsUntil(Hazard.SpikeState.Up) + 0.05f);
        yield return new WaitForSeconds(trap.SecondsUntil(Hazard.SpikeState.Hidden) + 0.05f);
        Teleport(trap.transform.position);
        int full = health.Current;

        yield return new WaitForSeconds(Hazard.HiddenSeconds + Hazard.WarningSeconds - 0.3f);
        Assert.AreNotEqual(Hazard.SpikeState.Up, trap.State);
        Assert.AreEqual(full, health.Current, "hidden and peeking spikes are safe to stand on");
        Assert.Less(spikes.localPosition.y, -0.3f, "the spikes are (mostly) under the plate");

        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(Hazard.SpikeState.Up, trap.State);
        Assert.AreEqual(full - 1, health.Current, "up spikes hurt");
        Assert.Greater(spikes.localPosition.y, -0.1f, "and they're sticking out");
    }

    [UnityTest]
    public IEnumerator SpikeTrapsInARowRippleEastward()
    {
        yield return LoadDungeon();
        // The treasure corridor's three traps share a row; each is two tiles east of the last.
        var corridor = All(Hazard.Kind.Spikes).Where(h => h.transform.position.x > 40f).ToArray();
        Assert.AreEqual(3, corridor.Length);
        // Measure just as the westmost one comes up: SecondsUntil(Up) is 0 for a trap that's already
        // up, so measuring while an eastern one is mid-way up would read the wrong gap.
        yield return new WaitForSeconds(corridor[0].SecondsUntil(Hazard.SpikeState.Hidden) + 0.05f);
        yield return new WaitForSeconds(corridor[0].SecondsUntil(Hazard.SpikeState.Up) + 0.01f);
        for (int i = 1; i < corridor.Length; i++)
        {
            float west = corridor[i - 1].SecondsUntil(Hazard.SpikeState.Up);
            float east = corridor[i].SecondsUntil(Hazard.SpikeState.Up);
            Assert.AreEqual(0.8f, Mathf.Repeat(east - west, Hazard.CycleSeconds), 0.05f,
                "each trap goes up 0.8s after the one to its west");
        }
    }

    [UnityTest]
    public IEnumerator HazardsAreOnTheMinimapAndNotArrivalSpots()
    {
        yield return LoadDungeon();
        var map = Object.FindAnyObjectByType<LevelMap>();
        var lava = All(Hazard.Kind.Lava)[0].transform.position;
        var cell = map.WorldToMap(lava);
        Assert.IsTrue(LevelMap.IsLava(map.At(Mathf.RoundToInt(cell.x), Mathf.RoundToInt(cell.y))),
            "the scene's lava sits where the map says");

        var file = MapFile.Parse("Test", string.Join("\n", "title: t", "theme: Dungeon", "---",
            "#####", "#~1^#", "#####", "---", "1 = door Dungeon"));
        Assert.IsNull(MapValidator.FloorBeside(file, 2, 1), "nobody arrives standing in lava or on spikes");
    }
}
