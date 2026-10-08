using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the Slime King, the dungeon's boss.
public class BossTests
{
    private GameObject player;
    private BossAbilities boss;

    [UnitySetUp]
    public IEnumerator LoadDungeon()
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene("Dungeon");
        yield return null;
        yield return null;
        player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        player.GetComponent<SpellAbility>().enabled = false;
        boss = Object.FindAnyObjectByType<BossAbilities>();
    }

    [TearDown]
    public void Reset() => GameSession.NewGame(null);

    [UnityTest]
    public IEnumerator TheKingIsBigGuardsTheExitAndIsWorthAFortuneInXp()
    {
        var skeleton = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Skeleton"));
        var slime = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Slime") && e.GetComponent<BossAbilities>() == null);

        float Width(Component c) => c.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite.bounds.size.x;
        Assert.Greater(Width(boss), Width(slime) * 1.8f, "much bigger than a normal slime");

        int bossXp = boss.GetComponent<Loot>().Experience;
        foreach (var normal in new Component[] { skeleton, slime })
        {
            int xp = normal.GetComponent<Loot>().Experience;
            Assert.That(bossXp, Is.InRange(10 * xp, 50 * xp), $"10-50x a {normal.name}'s XP");
        }

        var exit = Object.FindAnyObjectByType<ExitZone>();
        Assert.IsFalse(exit.IsOpen, "the crystal is sealed while the King lives");

        boss.Health.TakeDamage(999);
        Assert.AreEqual(bossXp, GameSession.Progress.Xp + Enumerable.Range(1, GameSession.Progress.Level - 1).Sum(Progression.XpToNext),
            "all of the King's XP was awarded");
        Assert.Greater(GameSession.Progress.Level, 3, "one boss is worth several levels");
        Assert.GreaterOrEqual(Object.FindObjectsByType<CoinPickup>().Length, 15, "a shower of coins");
        yield return null;
        Assert.IsTrue(exit.IsOpen);
    }

    [UnityTest]
    public IEnumerator GroundSlamHitsYouOnlyIfYouStayInTheCircle()
    {
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var health = player.GetComponent<Health>();

        // Stand still in the circle: get hit.
        Teleport(player, boss.transform.position + new Vector3(-5f, 0f, 0f));
        yield return null;
        Vector3 stoodAt = player.transform.position;
        boss.StartSlam();
        yield return null;
        Assert.IsTrue(boss.IsSlamming);
        Assert.IsNotNull(GameObject.Find("SlamWarning(Clone)"), "the slam is telegraphed with a warning circle");
        int before = health.Current;
        while (boss.IsSlamming) yield return null;
        Assert.LessOrEqual(health.Current, before - 2, "the landing hits for 2");
        yield return null; // Destroy() takes effect at the end of the frame
        Assert.IsNull(GameObject.Find("SlamWarning(Clone)"), "the warning circle is cleared away");
        Assert.Less(Vector3.Distance(Flat(boss.transform.position), Flat(stoodAt)), 1f, "it landed where you stood");
        Assert.Greater(Vector3.Distance(Flat(boss.transform.position), Flat(player.transform.position)), 2f,
            "and knocked you back out from under it");

        // See the circle and walk out of it: no damage.
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        health.Heal(99);
        Teleport(player, boss.transform.position + new Vector3(-5f, 0f, 0f));
        yield return null;
        boss.StartSlam();
        yield return null;
        Teleport(player, player.transform.position + new Vector3(-boss.SlamRadius - 2f, 0f, 0f)); // dodge!
        before = health.Current;
        while (boss.IsSlamming) yield return null;
        Assert.AreEqual(before, health.Current, "dodged the slam");
    }

    [UnityTest]
    public IEnumerator AtHalfHealthTheKingSplitsOffSlimelingsOnce()
    {
        int before = EnemyAI.AliveCount;
        boss.Health.TakeDamage(boss.Health.Max / 2);
        Assert.AreEqual(before + 3, EnemyAI.AliveCount, "three slimelings join the fight");
        yield return null;
        StringAssert.Contains("splits off slimelings", Hud().Q<Label>("toast").text);

        boss.Health.TakeDamage(1);
        Assert.AreEqual(before + 3, EnemyAI.AliveCount, "only the first time");
    }

    [UnityTest]
    public IEnumerator BossBarAppearsOnceTheKingNoticesYou()
    {
        Assert.IsFalse(Hud().Q("boss-bar").ClassListContains("visible"));
        Teleport(player, boss.transform.position + new Vector3(-6f, 0f, 0f));
        yield return null;
        yield return null;
        Assert.IsTrue(boss.IsEngaged);
        Assert.IsTrue(Hud().Q("boss-bar").ClassListContains("visible"));
        Assert.AreEqual("The Slime King", Hud().Q<Label>("boss-name").text);
        StringAssert.Contains("awakens", Hud().Q<Label>("toast").text);

        boss.Health.TakeDamage(3);
        yield return null;
        Assert.AreEqual(90f, Hud().Q("boss-fill").style.width.value.value, 0.01f);
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
