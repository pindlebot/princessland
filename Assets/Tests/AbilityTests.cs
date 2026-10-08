using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the skill paths and the abilities at the end of them:
// Flame Wave and Meteor (the wizard), Bubble Shield and Whirlpool (the princess).
// The fights happen in the dungeon's first room, which has open floor to the east and south
// of the start, with every monster's AI switched off so they stand where they're put.
public class AbilityTests
{
    private GameObject player;

    [TearDown]
    public void Reset() => GameSession.NewGame(null);

    // A plain [Test]: each hero's path is one straight line of 2 enhancements then 2 abilities.
    [Test]
    public void EachHeroHasOneLinearPathOfTwoEnhancementsThenTwoAbilities()
    {
        foreach (var hero in new[] { SkillCatalog.Wizard, SkillCatalog.Princess })
        {
            var path = SkillCatalog.PathFor(hero);
            Assert.AreEqual(4, path.Length, hero);
            CollectionAssert.AreEqual(new[] { false, false, true, true }, path.Select(s => s.IsAbility).ToArray(), hero);
            Assert.IsNull(path[0].Requires, $"{hero}'s first step needs nothing");
            for (int i = 1; i < path.Length; i++)
                Assert.AreEqual(path[i - 1].Id, path[i].Requires, $"{hero}: {path[i].Name} needs the step above it");
        }
        Assert.AreEqual(SkillCatalog.PathFor(SkillCatalog.Wizard), SkillCatalog.PathFor("Nobody"), "unknown heroes get the wizard's");
    }

    [UnityTest]
    public IEnumerator FlameWaveIsLockedUntilLearnedThenBurnsTheMonstersInFront()
    {
        yield return StartIn(null); // the wizard
        var wave = player.GetComponent<FlameWave>();
        Assert.IsFalse(wave.Unlocked);
        Assert.IsFalse(wave.TryUse(), "not learned yet");
        Assert.IsFalse(Hud().Q("ability-2").ClassListContains("unlocked"), "its hotbar slot is hidden");

        LearnWholePath();
        yield return null;
        Assert.IsTrue(Hud().Q("ability-2").ClassListContains("unlocked"), "the slot appears once learned");
        StringAssert.Contains("Meteor", Hud().Q<Label>("toast").text, "a toast announces the new ability");

        var monsters = Slimes(3);
        var inFront = Put(monsters[0], new Vector3(2.5f, 0f, 0f));
        var beside = Put(monsters[1], new Vector3(0f, 0f, -3f));   // 90° off: outside the fan
        var farAway = Put(monsters[2], new Vector3(9f, 0f, 0f));   // in line, but out of range
        float pushedFrom = HeroAbility.FlatDistance(player.transform.position, inFront.transform.position);
        float mana = player.GetComponent<Mana>().Current;

        Assert.IsTrue(wave.TryUse());
        Assert.Less(Hp(inFront), 3, "the monster in front is burned");
        Assert.AreEqual(3, Hp(beside), "the fan doesn't reach to the side");
        Assert.AreEqual(3, Hp(farAway), "nor that far");
        if (Hp(inFront) > 0)
            Assert.Greater(HeroAbility.FlatDistance(player.transform.position, inFront.transform.position), pushedFrom, "and pushed back");
        Assert.AreEqual(mana - wave.ManaCost, player.GetComponent<Mana>().Current, 0.5f, "it costs mana");
        Assert.IsFalse(wave.TryUse(), "then it needs to recharge");
    }

    [UnityTest]
    public IEnumerator MeteorSmashesEverythingInItsCircle()
    {
        yield return StartIn(null);
        LearnWholePath();
        var meteor = player.GetComponent<Meteor>();

        var monsters = Slimes(3);
        var target = Put(monsters[0], new Vector3(4f, 0f, 0f));
        var nextToIt = Put(monsters[1], new Vector3(4f, 0f, -1.5f));
        var elsewhere = Put(monsters[2], new Vector3(0f, 0f, -6f));

        Assert.IsTrue(meteor.TryUse());
        var strike = Object.FindAnyObjectByType<MeteorStrike>();
        Assert.IsNotNull(strike, "the meteor is on its way");
        Assert.AreEqual(3, Hp(target), "nothing happens until it lands");
        yield return new WaitForSeconds(strike.FallSeconds + 0.2f);

        Assert.Less(Hp(target), 3);
        Assert.Less(Hp(nextToIt), 3, "it hits everything in the circle");
        Assert.AreEqual(3, Hp(elsewhere));
    }

    [UnityTest]
    public IEnumerator BubbleShieldBlocksTwoHitsThenPops()
    {
        yield return StartAsPrincess();
        var shield = player.GetComponent<BubbleShield>();
        var health = player.GetComponent<Health>();
        Assert.IsFalse(shield.TryUse(), "not learned yet");
        LearnWholePath();

        Assert.IsTrue(shield.TryUse());
        Assert.IsTrue(shield.IsUp);
        Assert.IsTrue(player.transform.Find("Bubble").gameObject.activeSelf, "the bubble shows");
        int before = health.Current;
        health.TakeDamage(2);
        health.TakeDamage(2);
        Assert.AreEqual(before, health.Current, "two hits blocked");
        Assert.IsFalse(shield.IsUp, "and popped");
        Assert.IsFalse(player.transform.Find("Bubble").gameObject.activeSelf);
        health.TakeDamage(2);
        Assert.Less(health.Current, before, "the third hit lands");
        yield return null;
    }

    [UnityTest]
    public IEnumerator WhirlpoolPullsMonstersInAndSplashesThem()
    {
        yield return StartAsPrincess();
        LearnWholePath();
        var whirlpool = player.GetComponent<Whirlpool>();

        var monsters = Slimes(2);
        var target = Put(monsters[0], new Vector3(4f, 0f, 0f));
        var edge = Put(monsters[1], new Vector3(4f, 0f, -2.5f));

        Assert.IsTrue(whirlpool.TryUse());
        var pool = Object.FindAnyObjectByType<WhirlpoolZone>();
        Assert.IsNotNull(pool);
        float startDistance = HeroAbility.FlatDistance(pool.transform.position, edge.transform.position);
        yield return new WaitForSeconds(pool.TickSeconds + 0.3f);

        Assert.Less(HeroAbility.FlatDistance(pool.transform.position, edge.transform.position), startDistance - 0.5f,
            "the monster at the edge is dragged in");
        Assert.Less(Hp(target), 3, "monsters in the pool get splashed");
        Assert.Less(Hp(edge), 3);
    }

    [UnityTest]
    public IEnumerator SwiftTidesMakesTheTidalOrbRechargeFaster()
    {
        yield return StartAsPrincess();
        var spell = player.GetComponent<SpellAbility>();
        float before = spell.Cooldown;
        GameSession.Progress.AddXp(10000);
        GameSession.Progress.Learn(SkillCatalog.Find(SkillCatalog.Toughness));
        GameSession.Progress.Learn(SkillCatalog.Find(SkillCatalog.SwiftTides));
        Assert.AreEqual(before * 0.7f, spell.Cooldown, 0.001f);
    }

    // ---------- Helpers ----------

    private IEnumerator StartIn(CharacterDefinition hero)
    {
        GameSession.NewGame(hero);
        SceneManager.LoadScene("Dungeon");
        yield return null;
        yield return null;
        player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        player.GetComponent<SpellAbility>().enabled = false; // no auto-casting with stray input
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    private IEnumerator StartAsPrincess()
    {
        SceneManager.LoadScene("Title");
        yield return null;
        var princess = Object.FindAnyObjectByType<TitleController>().Heroes.First(h => h.name == "Princess");
        yield return StartIn(princess);
    }

    // Enough XP for every step, learned in order through the skill tree.
    private void LearnWholePath()
    {
        GameSession.Progress.AddXp(10000);
        var tree = Object.FindAnyObjectByType<SkillTreeView>();
        foreach (var skill in tree.Path) Assert.IsTrue(tree.TryLearn(skill), skill.Name);
        player.GetComponent<Mana>().Refill();
    }

    private static EnemyAI[] Slimes(int count)
    {
        var slimes = Object.FindObjectsByType<EnemyAI>()
            .Where(e => e.name.StartsWith("Slime") && e.GetComponent<BossAbilities>() == null)
            .Take(count).ToArray();
        Assert.AreEqual(count, slimes.Length, "the dungeon has enough slimes");
        return slimes;
    }

    // Move a monster next to the hero (offset in world metres, on the floor plane).
    private EnemyAI Put(EnemyAI enemy, Vector3 offset)
    {
        var body = enemy.GetComponent<CharacterController>();
        body.enabled = false;
        var p = player.transform.position + offset;
        enemy.transform.position = new Vector3(p.x, enemy.transform.position.y, p.z);
        body.enabled = true;
        return enemy;
    }

    private static int Hp(EnemyAI enemy) => enemy.GetComponent<Health>().Current;

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
}
