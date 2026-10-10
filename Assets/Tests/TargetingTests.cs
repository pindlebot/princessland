using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for hitting monsters from afar (they come for you) and for choosing which monster
// the spell is aimed at (Tab / RB; the HUD's target card and the ring on the ground follow it).
public class TargetingTests
{
    private GameObject player;
    private SpellAbility spell;

    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene("Dungeon");
        yield return null;
        player = Object.FindAnyObjectByType<PlayerController>().gameObject;
        player.GetComponent<PlayerController>().enabled = false;
        spell = player.GetComponent<SpellAbility>();
        spell.enabled = false; // the tests call CycleTarget themselves
    }

    [UnityTest]
    public IEnumerator AMonsterHitFromFarAwayWakesUpAndComesForTheHero()
    {
        // A skeleton with open floor to its left (below the 1.2m wall tops).
        var enemy = Object.FindObjectsByType<EnemyAI>().Where(e => e.name.StartsWith("Skeleton"))
            .First(e => !Physics.Raycast(new Vector3(e.transform.position.x, 0.6f, e.transform.position.z), Vector3.left, 7f, ~0,
                                         QueryTriggerInteraction.Ignore));
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) if (e != enemy) e.enabled = false;
        // Shrink its aggro range so the hero 6m away is "afar" without needing a huge clear space.
        typeof(EnemyAI).GetField("aggroRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(enemy, 3f);
        Teleport(player, enemy.transform.position + new Vector3(-6f, 0f, 0f));
        yield return new WaitForSeconds(0.5f);
        Vector3 before = enemy.transform.position;
        yield return new WaitForSeconds(0.5f);
        Assert.Less(Vector3.Distance(before, enemy.transform.position), 0.05f, "it hasn't noticed the hero");

        enemy.Health.TakeDamage(1); // what a fireball does when it lands
        Assert.IsFalse(enemy.Health.IsDead, "a skeleton survives one hit");
        yield return new WaitForSeconds(1f);
        Assert.Greater(Vector3.Distance(before, enemy.transform.position), 1f, "the wounded skeleton sets off after the hero");
    }

    [UnityTest]
    public IEnumerator ACrackedStationaryMonsterStaysPut()
    {
        // (Dark mermaids only watch from the water; they never chase.)
        var cove = new GameObject("Stationary", typeof(CharacterController), typeof(Health));
        var ai = cove.AddComponent<EnemyAI>();
        typeof(EnemyAI).GetField("stationary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(ai, true);
        yield return null;
        cove.transform.position = player.transform.position + new Vector3(20f, 0f, 0f);
        Vector3 before = cove.transform.position;
        cove.GetComponent<Health>().TakeDamage(1);
        yield return new WaitForSeconds(0.5f);
        Assert.Less(Vector3.Distance(before, cove.transform.position), 0.05f);
        Object.Destroy(cove);
    }

    // Three skeletons 3, 5 and 7 metres from the hero, in view; every other monster far away and still.
    private IEnumerator ArrangeThree(System.Action<EnemyAI, EnemyAI, EnemyAI> done)
    {
        var all = Object.FindObjectsByType<EnemyAI>()
            .OrderBy(e => e.name.StartsWith("Skeleton") ? 0 : 1).ThenBy(e => e.transform.position.x).ToList(); // skeletons first
        foreach (var e in all) e.enabled = false;
        for (int i = 3; i < all.Count; i++) Teleport(all[i].gameObject, new Vector3(500f + i * 10f, 0f, 500f));
        Vector3 here = player.transform.position;
        Teleport(all[0].gameObject, here + new Vector3(3f, 0f, 0f));
        Teleport(all[1].gameObject, here + new Vector3(0f, 0f, 5f));
        Teleport(all[2].gameObject, here + new Vector3(-4f, 0f, -5.7f));
        yield return new WaitForSeconds(0.6f); // the camera catches up
        done(all[0], all[1], all[2]);
    }

    [UnityTest]
    public IEnumerator CyclingPicksTheNextNearestMonsterAndWrapsAround()
    {
        EnemyAI near = null, middle = null, far = null;
        yield return ArrangeThree((a, b, c) => { near = a; middle = b; far = c; });
        var byDistance = new[] { near, middle, far };

        // Wherever it starts (a wall can hide the nearest from the auto-aim), each press goes one farther out, then round.
        var picks = new System.Collections.Generic.List<EnemyAI>();
        for (int i = 0; i < 4; i++)
        {
            spell.CycleTarget();
            picks.Add(spell.Target);
        }
        CollectionAssert.AreEquivalent(byDistance, picks.Take(3), "all three get a turn");
        Assert.AreEqual(picks[0], picks[3], "and then round again");
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(byDistance[(System.Array.IndexOf(byDistance, picks[i]) + 1) % 3], picks[i + 1], "next one out");

        // The fireball flies at the picked monster, not the nearest.
        var picked = spell.Target;
        spell.CycleTarget();
        picked = spell.Target;
        Assert.AreEqual(picked, spell.FindTarget());
        Assert.IsTrue(spell.TryCast());
        var fireball = Object.FindAnyObjectByType<Projectile>();
        Vector3 toPicked = picked.transform.position - fireball.transform.position;
        toPicked.y = 0f;
        Assert.Less(Vector3.Angle(fireball.transform.forward, toPicked), 3f);
    }

    [UnityTest]
    public IEnumerator ThePickedMonsterIsForgottenWhenItIsDefeated()
    {
        yield return ArrangeThree((a, b, c) => { });
        spell.CycleTarget();
        var picked = spell.Target;
        Assert.IsNotNull(picked);

        picked.Health.TakeDamage(99);
        yield return null;
        yield return null;
        Assert.IsNotNull(spell.Target);
        Assert.AreNotEqual(picked, spell.Target, "on to whoever is left");
        Assert.IsTrue(EnemyAI.IsAlive(spell.Target));
    }

    [UnityTest]
    public IEnumerator CyclingWithNobodyInViewDoesNothing()
    {
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) Teleport(e.gameObject, new Vector3(500f, 0f, 500f));
        yield return new WaitForSeconds(0.3f);
        spell.CycleTarget();
        Assert.IsNull(spell.Target);
    }

    [UnityTest]
    public IEnumerator TheHudShowsTheTargetAndItsHealth()
    {
        yield return ArrangeThree((a, b, c) => { });
        var hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
        var card = hud.Q("target-card");

        yield return null;
        Assert.IsTrue(card.ClassListContains("visible"));
        Assert.AreEqual("Skeleton", hud.Q<Label>("target-name").text);
        Assert.AreEqual("Tab", hud.Q<Label>("target-key").text);
        Assert.AreEqual(100f, hud.Q("target-fill").style.width.value.value, 0.01f, "a full health bar");

        spell.Target.Health.TakeDamage(1); // one of a skeleton's two hearts
        yield return null;
        Assert.AreEqual(50f, hud.Q("target-fill").style.width.value.value, 0.01f);

        foreach (var e in Object.FindObjectsByType<EnemyAI>()) Teleport(e.gameObject, new Vector3(500f, 0f, 500f));
        yield return new WaitForSeconds(0.3f);
        yield return null;
        Assert.IsFalse(card.ClassListContains("visible"), "nothing in view, nothing to show");
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
