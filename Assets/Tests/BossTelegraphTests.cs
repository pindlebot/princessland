using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for the floor warnings before boss attacks, across every boss (docs/BOSS_TELEGRAPHS.md has the numbers):
// each attack shows a warning of its own shape for at least Telegraph.MinWarningSeconds before it lands, the warning
// matches the real danger area, it is cleaned up, and Gentle Mode only ever makes it longer.
public class BossTelegraphTests
{
    // One scene per boss: the Slime King, Captain Grumblebeard, the Pumpkin King, Mother Mushroom, the Crystal Golem,
    // King Crabbington and the Snow Yeti.
    public static readonly string[] BossScenes = { "Dungeon", "Cove", "Farm", "Woods4", "Mines4", "Lake4", "Frost4" };

    private GameObject player;
    private BossAbilities boss;

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
    }

    private IEnumerator Load(string scene, bool gentle = false)
    {
        GameSession.NewGame(null);
        GameSession.Settings.gentle = gentle;
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        player.GetComponent<SpellAbility>().enabled = false;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        boss = Object.FindAnyObjectByType<BossAbilities>();
        boss.HoldSpecials = true;   // only the moves the test starts
        player.GetComponent<Health>().InvulnerableUntil = Time.time + 999f;
        var open = boss.transform.position + new Vector3(-5f, 0f, 0f);
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = open;
        cc.enabled = true;
        yield return null;
    }

    private static TelegraphMarker[] Live() => Object.FindObjectsByType<TelegraphMarker>().ToArray();

    [UnityTest]
    public IEnumerator EveryBossGroundSlamIsWarnedWithARingMatchingItsDangerAreaAndThenClearedAway([ValueSource(nameof(BossScenes))] string scene)
    {
        yield return Load(scene);
        Assert.IsEmpty(Live());
        boss.StartSlam();
        yield return null;
        var rings = Live().Where(m => m.Kind == TelegraphKind.Circle).ToArray();
        Assert.AreEqual(1, rings.Length, $"{boss.BossName}: a ring for the slam");
        Assert.AreEqual(boss.SlamRadius, rings[0].Radius, 0.01f, "the ring is exactly the danger area");
        Assert.GreaterOrEqual(rings[0].Seconds, Telegraph.MinWarningSeconds, "at least this much warning");
        Assert.GreaterOrEqual(rings[0].Seconds, 1f - 0.001f, "and the slam's own windup");

        float shown = 0f;
        while (boss.IsSlamming)
        {
            if (rings[0] != null) shown += Time.deltaTime;
            yield return null;
        }
        Assert.GreaterOrEqual(shown, 0.9f, "it stays up the whole windup");
        yield return null;
        Assert.IsEmpty(Live(), "and goes when the boss lands");
    }

    [UnityTest]
    public IEnumerator EveryVolleyShowsAFanOfLanesThatSettlesBeforeTheBoltsLeave([ValueSource(nameof(BossScenes))] string scene)
    {
        yield return Load(scene);
        if (!boss.HasVolley) Assert.Ignore($"{boss.BossName} has no volley");
        boss.StartVolley();
        yield return null;
        var fan = Live().Single(m => m.Kind == TelegraphKind.Fan);
        Assert.GreaterOrEqual(fan.Seconds, Telegraph.MinWarningSeconds);
        Assert.IsFalse(fan.IsLocked, "it follows you at first");

        // It locks, and the bolts have not left yet.
        while (fan != null && !fan.IsLocked) yield return null;
        Assert.IsNotNull(fan, "it locks before it ends");
        Assert.AreEqual(0, Object.FindObjectsByType<EnemyBolt>().Length, "no bolts while the fan is still warning");
        var lockedAim = fan.transform.rotation;
        Teleport(player, player.transform.position + new Vector3(0f, 0f, 3f));   // sidestep at the last moment
        yield return null;
        Assert.AreEqual(lockedAim, fan.transform.rotation, "a locked fan does not chase");
        while (boss.IsVolleying && Object.FindObjectsByType<EnemyBolt>().Length == 0) yield return null;
        Assert.GreaterOrEqual(Object.FindObjectsByType<EnemyBolt>().Length, 3, "the bolts fly");
        yield return null;
        Assert.IsEmpty(Live().Where(m => m.Kind == TelegraphKind.Fan), "the fan is gone once they have");
    }

    [UnityTest]
    public IEnumerator CaptainGrumblebeardsCannonShellsEachGetARingAndAWhistle()
    {
        yield return Load("Cove");
        Assert.IsTrue(boss.HasBarrage);
        boss.StartBarrage();
        int most = 0;
        float firstSeconds = 0f;
        for (float t = 0f; t < 6f && boss.IsBarraging; t += Time.deltaTime)
        {
            var rings = Live().Where(m => m.Kind == TelegraphKind.Circle).ToArray();
            most = Mathf.Max(most, rings.Length);
            if (rings.Length > 0 && firstSeconds == 0f) firstSeconds = rings[0].Seconds;
            foreach (var r in rings) Assert.AreEqual(1.8f, r.Radius, 0.01f, "each ring is its shell's blast radius");
            yield return null;
        }
        Assert.GreaterOrEqual(most, 2, "several shells are warned about at once");
        Assert.GreaterOrEqual(firstSeconds, Telegraph.MinWarningSeconds);
        yield return null;
        Assert.IsEmpty(Live());
    }

    [UnityTest]
    public IEnumerator KingCrabbingtonsTideIsAStripOfArrowsAcrossTheCourtThatPointsTheWayItRolls()
    {
        yield return Load("Lake4");
        var tide = boss.GetComponent<TideWaves>();
        tide.StartWave();
        yield return null;
        var band = Live().Single(m => m.Kind == TelegraphKind.Band);
        Assert.GreaterOrEqual(band.Seconds, Telegraph.MinWarningSeconds);
        Assert.AreEqual(2f, band.Radius, 0.01f, "half of the 4 m band");
        var arrow = band.transform.rotation * Vector3.right;   // the way the arrowheads point on the floor
        Assert.AreEqual(0f, arrow.y, 0.01f + 0.001f);
        Assert.IsTrue(Mathf.Abs(arrow.x) > 0.99f || Mathf.Abs(arrow.z) > 0.99f, "along one axis of the court");
        while (tide.IsWaving) yield return null;
        yield return null;
        Assert.IsEmpty(Live());
    }

    [UnityTest]
    public IEnumerator TheYetisSnowballLanesAreThreeStripsEachWithArrows()
    {
        yield return Load("Frost4");
        var lanes = boss.GetComponent<SnowballLanes>();
        lanes.StartLanes();
        yield return null;
        var bands = Live().Where(m => m.Kind == TelegraphKind.Band).ToArray();
        Assert.AreEqual(3, bands.Length);
        Assert.IsTrue(bands.All(b => b.Seconds >= Telegraph.MinWarningSeconds));
        while (lanes.IsRolling) yield return null;
        yield return null;
        Assert.IsEmpty(Live());
    }

    [UnityTest]
    public IEnumerator GentleModeOnlyEverLengthensAWarning([ValueSource(nameof(BossScenes))] string scene)
    {
        yield return Load(scene, gentle: false);
        float normal = boss.WindupSeconds;
        yield return Load(scene, gentle: true);
        Assert.AreEqual(normal * BossAbilities.GentleWindupFactor, boss.WindupSeconds, 0.001f);
        boss.StartSlam();
        yield return null;
        Assert.GreaterOrEqual(Live().Single(m => m.Kind == TelegraphKind.Circle).Seconds, normal * 1.5f);
    }

    [UnityTest]
    public IEnumerator ABossBeatenMidAttackLeavesNoWarningBehind([ValueSource(nameof(BossScenes))] string scene)
    {
        yield return Load(scene);
        boss.GetComponent<GolemCrystals>()?.SetGlowing(true, announce: false);   // (a shielded boss shrugs off even a huge hit)
        boss.GetComponent<ShellCycle>()?.SetPeeking(true, announce: false);
        boss.StartSlam();
        yield return null;
        Assert.IsNotEmpty(Live());
        boss.Health.TakeDamage(9999);
        yield return new WaitForSeconds(0.5f);
        Assert.IsEmpty(Live(), "the warning is cleared away with him");
    }

    // ---------- What the warnings look like ----------

    // A warning has to read on white snow and on dark water, and not by colour alone.
    [Test]
    public void EveryWarningPictureHasBothADarkOutlineAndALightMarkSoItReadsOnSnowAndWater()
    {
        foreach (var sprite in new[] { FeedbackArt.DashedRing(), FeedbackArt.Chevrons(), FeedbackArt.Fan(5, 60f) })
        {
            var pixels = sprite.texture.GetPixels32().Where(p => p.a > 200).ToArray();
            Assert.Greater(pixels.Length, 50);
            float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
            Assert.IsTrue(pixels.Any(p => Luma(p) < 0.3f), $"{sprite.texture.width}x{sprite.texture.height}: dark marks, for snow");
            Assert.IsTrue(pixels.Any(p => Luma(p) > 0.75f), $"{sprite.texture.width}x{sprite.texture.height}: light marks, for water and shadow");
        }
    }

    [Test]
    public void TheThreeWarningShapesAreDifferentFromEachOther()
    {
        var ring = FeedbackArt.DashedRing();
        var band = FeedbackArt.Chevrons();
        var fan = FeedbackArt.Fan(5, 60f);
        Assert.AreNotEqual(ring, band);
        Assert.AreNotEqual(ring, fan);
        Assert.AreNotEqual(band, fan);
        // The ring is hollow in the middle, the fan is not a square, the strip has rails: different silhouettes.
        var r = ring.texture;
        Assert.AreEqual(0, r.GetPixel(r.width / 2, r.height / 2).a, 0.01f, "the ring is hollow");
        Assert.AreNotEqual(fan.texture.width, fan.texture.height, "the fan is a wedge, not a square");
        Assert.AreEqual(fan.texture.height, FeedbackArt.Fan(5, 60f).texture.height);
        Assert.AreNotEqual(FeedbackArt.Fan(5, 60f), FeedbackArt.Fan(5, 70f), "a wider volley gets a wider fan");
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
