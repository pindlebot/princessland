using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for gold, experience, levels and the skill tree.
public class ProgressionTests
{
    [SetUp]
    public void FreshGame() => GameSession.NewGame(null);

    [TearDown]
    public void Reset() => GameSession.NewGame(null);

    // A plain [Test] (no frames needed): the XP curve and level-up rules on their own.
    [Test]
    public void ExperienceCurveGrowsAndBigRewardsCanGiveSeveralLevels()
    {
        Assert.AreEqual(40, Progression.XpToNext(1));
        Assert.AreEqual(113, Progression.XpToNext(2));
        Assert.Greater(Progression.XpToNext(5), Progression.XpToNext(4));

        var progress = new Progression();
        int levelUps = 0;
        progress.LeveledUp += _ => levelUps++;
        progress.AddXp(39);
        Assert.AreEqual(1, progress.Level);
        progress.AddXp(1);
        Assert.AreEqual(2, progress.Level);
        Assert.AreEqual(0, progress.Xp);
        Assert.AreEqual(1, progress.SkillPoints);

        progress.AddXp(113 + 208 + 10); // straight through levels 3 and 4
        Assert.AreEqual(4, progress.Level);
        Assert.AreEqual(10, progress.Xp);
        Assert.AreEqual(3, progress.SkillPoints);
        Assert.AreEqual(3, levelUps);
    }

    [UnityTest]
    public IEnumerator KillsGiveExperienceAndGoldAndLevelUpsRaiseStats()
    {
        SceneManager.LoadScene("Dungeon");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var progress = GameSession.Progress;

        var skeleton = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Skeleton"));
        Vector3 where = skeleton.transform.position;
        skeleton.GetComponent<Health>().TakeDamage(99);
        Assert.AreEqual(15, progress.Xp, "a skeleton is worth 15 XP");
        int dropped = Object.FindObjectsByType<CoinPickup>().Length;
        Assert.That(dropped, Is.InRange(2, 4), "it drops a little burst of coins");

        // Walk over to the coins: they pop out, then fly to you and are collected.
        Teleport(player, where);
        yield return new WaitForSeconds(1.2f);
        Assert.AreEqual(0, Object.FindObjectsByType<CoinPickup>().Length);
        Assert.Greater(progress.Gold, 0);
        Assert.AreEqual(progress.Gold.ToString(), Hud().Q<Label>("gold-text").text);

        // Level up: +1 max health, +5 max mana, a skill point, and the HUD shows it.
        var health = player.GetComponent<Health>();
        var mana = player.GetComponent<Mana>();
        health.TakeDamage(2);
        progress.AddXp(progress.XpForNextLevel - progress.Xp);
        yield return null;
        Assert.AreEqual(2, progress.Level);
        Assert.AreEqual(6, health.Max);
        Assert.AreEqual(6, health.Current, "levelling up heals you");
        Assert.AreEqual(55f, mana.Max);
        Assert.AreEqual("2", Hud().Q<Label>("level-badge").text);
        StringAssert.Contains("Level up", Hud().Q<Label>("toast").text);
        StringAssert.Contains("1 skill point", Hud().Q<Label>("points-hint").text);
    }

    [UnityTest]
    public IEnumerator SkillTreeNeedsPointsAndPrerequisites()
    {
        SceneManager.LoadScene("Dungeon");
        yield return null;
        yield return null;
        var player = LevelBootstrap.Current.Player;
        var tree = Object.FindAnyObjectByType<SkillTreeView>();
        var progress = GameSession.Progress;
        SkillDefinition Skill(string id) => SkillCatalog.All.First(s => s.Id == id);

        Assert.IsFalse(tree.TryLearn(Skill(SkillCatalog.Toughness)), "no points at level 1");
        progress.AddXp(40 + 113); // level 3: two points
        Assert.AreEqual(2, progress.SkillPoints);

        Assert.IsFalse(tree.TryLearn(Skill("twin_cast")), "needs Empowered Spells first");
        int healthBefore = player.GetComponent<Health>().Max;
        Assert.IsTrue(tree.TryLearn(Skill(SkillCatalog.Toughness)));
        Assert.AreEqual(healthBefore + 2, player.GetComponent<Health>().Max);
        Assert.IsTrue(tree.NodeFor(SkillCatalog.Toughness).ClassListContains("learned"));

        int damageBefore = player.GetComponent<SpellAbility>().Damage;
        Assert.IsTrue(tree.TryLearn(Skill(SkillCatalog.Empowered)));
        Assert.AreEqual(damageBefore + 1, player.GetComponent<SpellAbility>().Damage);
        Assert.AreEqual(0, progress.SkillPoints);
        Assert.IsFalse(tree.TryLearn(Skill(SkillCatalog.DeepReserves)), "out of points");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProgressCarriesIntoTheNextScene()
    {
        GameSession.Progress.AddGold(42);
        GameSession.Progress.AddXp(50); // level 2
        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;

        Assert.AreEqual("42", Hud().Q<Label>("gold-text").text);
        Assert.AreEqual("2", Hud().Q<Label>("level-badge").text);
        Assert.AreEqual(6, LevelBootstrap.Current.Player.GetComponent<Health>().Max, "the wizard's 5, +1 for level 2");
    }

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
