using System.Collections;
using System.Collections.Generic;
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
        SceneManager.LoadScene("Level0");
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

    // Spots `radius` metres from the hero (as offsets) with a clear line of fire at casting height: low walls near the
    // spawn hide a monster from the spell even when they don't from above. Angles already used are skipped by 50 degrees.
    private List<Vector3> OpenSpots(float radius, int count, params float[] avoidDegrees)
    {
        var spots = new List<Vector3>();
        var used = new List<float>(avoidDegrees);
        var from = player.transform.position;
        from.y = spell.CastHeight;
        for (int a = 0; a < 360 && spots.Count < count; a += 15)
        {
            if (used.Any(u => Mathf.Abs(Mathf.DeltaAngle(u, a)) < 50f)) continue;
            var offset = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad)) * radius;
            var to = from + offset;
            if (Physics.Linecast(from, to, ~LevelMap.WaterMask, QueryTriggerInteraction.Ignore)) continue;
            if (!Physics.Raycast(to + Vector3.up * 2f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore)) continue; // floor there
            spots.Add(offset);
            used.Add(a);
        }
        return spots;
    }

    private Vector3? OpenSpotAt(float radius, float degrees)
    {
        var from = player.transform.position;
        from.y = spell.CastHeight;
        var offset = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0f, Mathf.Sin(degrees * Mathf.Deg2Rad)) * radius;
        if (Physics.Linecast(from, from + offset, ~LevelMap.WaterMask, QueryTriggerInteraction.Ignore)) return null;
        if (!Physics.Raycast(from + offset + Vector3.up, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore)) return null;
        return offset;
    }

    private static float AngleOf(Vector3 offset) => Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;

    // Three skeletons at about 3, 5 and 7 metres from the hero, each in the open and in view; every other monster far away and still.
    private IEnumerator ArrangeThree(System.Action<EnemyAI, EnemyAI, EnemyAI> done)
    {
        var all = Object.FindObjectsByType<EnemyAI>()
            .OrderBy(e => e.name.StartsWith("Skeleton") ? 0 : 1).ThenBy(e => e.transform.position.x).ToList(); // skeletons first
        foreach (var e in all) e.enabled = false;
        for (int i = 3; i < all.Count; i++) Teleport(all[i].gameObject, new Vector3(500f + i * 10f, 0f, 500f));
        var near = OpenSpots(3f, 1);
        var middle = OpenSpots(5f, 1, near.Select(AngleOf).ToArray());
        var far = OpenSpots(7f, 1, near.Concat(middle).Select(AngleOf).ToArray());
        if (near.Count == 0 || middle.Count == 0 || far.Count == 0) Assert.Ignore("no open ground round the hero's spawn for three monsters");
        Vector3 here = player.transform.position;
        Teleport(all[0].gameObject, here + near[0]);
        Teleport(all[1].gameObject, here + middle[0]);
        Teleport(all[2].gameObject, here + far[0]);
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

    // ---------- Reach: no aiming through walls, a steady pick, a clear marker ----------

    // Two or three monsters at given offsets from the hero (every other monster far away and still), with the camera caught up.
    private IEnumerator Arrange(System.Action<EnemyAI[]> done, params Vector3[] offsets)
    {
        var all = Object.FindObjectsByType<EnemyAI>().OrderBy(e => e.name).ToList();
        foreach (var e in all) e.enabled = false;
        for (int i = offsets.Length; i < all.Count; i++) Teleport(all[i].gameObject, new Vector3(500f + i * 10f, 0f, 500f));
        Vector3 here = player.transform.position;
        for (int i = 0; i < offsets.Length; i++) Teleport(all[i].gameObject, here + offsets[i]);
        yield return new WaitForSeconds(0.6f);
        done(all.Take(offsets.Length).ToArray());
    }

    [UnityTest]
    public IEnumerator ThereIsNoTargetBehindAWallAndTheSpellFliesStraightInstead()
    {
        EnemyAI enemy = null;
        var spot = OpenSpots(3f, 1);
        if (spot.Count == 0) Assert.Ignore("no open ground");
        yield return Arrange(e => enemy = e[0], spot[0]);
        Assert.AreEqual(enemy, spell.Target, "in the open, it is the target");

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = player.transform.position + spot[0] * 0.5f + Vector3.up;
        wall.transform.rotation = Quaternion.LookRotation(spot[0]);
        wall.transform.localScale = new Vector3(6f, 4f, 0.4f);
        yield return new WaitForFixedUpdate();   // (a new collider only exists for queries after a physics step)
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsNull(spell.Target, "hidden behind a wall: not a target");
        Assert.IsNull(spell.FindTarget());
        spell.CycleTarget();
        Assert.IsNull(spell.Target, "and Tab cannot pick it either");

        var facing = Quaternion.AngleAxis(180f, Vector3.up) * spot[0];   // away from the hidden monster
        player.transform.rotation = Quaternion.LookRotation(facing);
        Assert.IsTrue(spell.TryCast());
        var fireball = Object.FindAnyObjectByType<Projectile>();
        Assert.Less(Vector3.Angle(fireball.transform.forward, facing), 5f, "it flies the way the hero faces, not at the hidden monster");

        Object.Destroy(wall);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.AreEqual(enemy, spell.Target, "the wall is gone: it is the target again");
    }

    [UnityTest]
    public IEnumerator TheAutomaticPickKeepsItsMonsterUntilAnotherIsClearlyCloser()
    {
        EnemyAI far = null, near = null;
        var a = OpenSpots(3f, 1);
        var b = OpenSpots(6f, 1, a.Select(AngleOf).ToArray());
        if (a.Count == 0 || b.Count == 0) Assert.Ignore("no open ground");
        yield return Arrange(e => { far = e[0]; near = e[1]; }, a[0], b[0]);
        Assert.AreEqual(far, spell.Target);

        // A little closer than the pick: no change (it would flicker as they shuffle about).
        Teleport(near.gameObject, player.transform.position + b[0].normalized * 2.4f);
        yield return null;
        yield return null;
        Assert.AreEqual(far, spell.Target, "0.6 m closer is not enough to switch");

        // Clearly closer: switch.
        Teleport(near.gameObject, player.transform.position + b[0].normalized * 1.2f);
        yield return null;
        yield return null;
        Assert.AreEqual(near, spell.Target, "1.8 m closer is");
    }

    [UnityTest]
    public IEnumerator EqualDistancesAlwaysGoToTheMonsterOnTheLeft()
    {
        // Two open spots on opposite sides of the hero, the same distance away.
        Vector3? left = null, right = null;
        for (int deg = 0; deg < 180 && left == null; deg += 15)
        {
            var one = OpenSpotAt(4f, deg);
            var other = OpenSpotAt(4f, deg + 180);
            if (one == null || other == null || Mathf.Abs(one.Value.x - other.Value.x) < 1f) continue;
            left = one.Value.x < other.Value.x ? one : other;
            right = one.Value.x < other.Value.x ? other : one;
        }
        if (left == null) Assert.Ignore("no two open spots on opposite sides here");

        for (int round = 0; round < 2; round++)
        {
            EnemyAI a = null, b = null;
            yield return Arrange(_ => { });          // everyone away first, so no earlier pick is being kept
            Assert.IsNull(spell.Target);
            // The same two spots, the monsters swapped between them the second time round.
            var offsets = round == 0 ? new[] { left.Value, right.Value } : new[] { right.Value, left.Value };
            yield return Arrange(e => { a = e[0]; b = e[1]; }, offsets);
            var leftOne = round == 0 ? a : b;
            Assert.AreEqual(leftOne, spell.Target, $"round {round}: the one on the left");
        }
    }

    [UnityTest]
    public IEnumerator TheTargetWearsARingAndAnArrowAndTheArrowShowsWhoPickedIt()
    {
        EnemyAI one = null;
        var m1 = OpenSpots(3f, 1);
        var m2 = OpenSpots(5f, 1, m1.Select(AngleOf).ToArray());
        if (m1.Count == 0 || m2.Count == 0) Assert.Ignore("no open ground");
        yield return Arrange(e => one = e[0], m1[0], m2[0]);
        yield return null;
        var marker = player.GetComponent<TargetMarker>();
        Assert.IsTrue(marker.IsShown, "a ring under it");
        Assert.IsTrue(marker.Arrow.GetComponent<SpriteRenderer>().enabled, "and an arrow over it");
        Assert.Greater(marker.Arrow.position.y, spell.Target.transform.position.y + 1f, "the arrow floats above its head");

        var auto = marker.Arrow.GetComponent<SpriteRenderer>().sprite;
        Assert.IsFalse(spell.TargetWasChosen);
        spell.CycleTarget();
        yield return null;
        Assert.IsTrue(spell.TargetWasChosen);
        Assert.AreNotEqual(auto, marker.Arrow.GetComponent<SpriteRenderer>().sprite, "a solid arrow for a Tab pick");

        foreach (var e in Object.FindObjectsByType<EnemyAI>()) Teleport(e.gameObject, new Vector3(500f, 0f, 500f));
        yield return new WaitForSeconds(0.3f);
        Assert.IsFalse(marker.IsShown);
        Assert.IsFalse(marker.Arrow.GetComponent<SpriteRenderer>().enabled);
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
