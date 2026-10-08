using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests: each one loads the real Dungeon scene (Level 1) and checks a core rule.
// Run them from Window > General > Test Runner > PlayMode.
public class GameplayTests
{
    private GameObject player;

    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        GameSession.NewGame(null); // the default hero (the wizard) and no leftover flags from earlier tests
        SceneManager.LoadScene("Dungeon");
        yield return null; // scene loads at the start of the next frame
        player = Object.FindAnyObjectByType<PlayerController>().gameObject;
        // Turn off mouse/keyboard control so the tests drive the player directly.
        player.GetComponent<PlayerController>().enabled = false;
        player.GetComponent<SpellAbility>().enabled = false;
    }

    [UnityTest]
    public IEnumerator SceneHasPlayerEnemiesAndExit()
    {
        yield return null;
        Assert.AreEqual(9, EnemyAI.AliveCount, "5 skeletons, 3 slimes and the Slime King");
        Assert.IsNotNull(Object.FindAnyObjectByType<ExitZone>());
        Assert.IsFalse(GameManager.Instance.IsGameOver);
    }

    [UnityTest]
    public IEnumerator TwoFireballsKillAnEnemy()
    {
        var enemy = IsolatedEnemy(); // no other skeleton nearby, so it's unambiguously the nearest
        var enemyHealth = enemy.GetComponent<Health>();
        // Hold everyone still: otherwise another skeleton can walk up and become the nearer target.
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;

        // Stand 4m from the enemy, facing it.
        Teleport(player, enemy.transform.position + new Vector3(-4f, 0f, 0f));
        player.transform.rotation = Quaternion.LookRotation(Vector3.right);
        yield return WaitForCamera(); // auto-aim only targets enemies that are on screen

        var ability = player.GetComponent<SpellAbility>();
        Assert.IsTrue(ability.TryCast());
        Assert.IsFalse(ability.TryCast(), "cooldown should block an immediate recast");
        yield return new WaitForSeconds(0.7f);
        Assert.AreEqual(1, enemyHealth.Current);

        Assert.IsTrue(ability.TryCast());
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(enemyHealth.IsDead);
        Assert.AreEqual(8, EnemyAI.AliveCount);
    }

    [UnityTest]
    public IEnumerator EnemyDamagesPlayerInMeleeRange()
    {
        GameSession.Settings.gentle = false; // one hit = one heart
        var enemy = Object.FindAnyObjectByType<EnemyAI>();
        Teleport(player, enemy.transform.position + new Vector3(1f, 0f, 0f));
        yield return new WaitForSeconds(0.5f);
        Assert.Less(player.GetComponent<Health>().Current, 5);
    }

    [UnityTest]
    public IEnumerator FireballIsBlockedByWalls()
    {
        yield return RemoveAllEnemies(); // otherwise the fireball auto-aims at a skeleton
        // Face the start room's west wall (map column 0) and fire.
        player.transform.rotation = Quaternion.LookRotation(Vector3.left);
        player.GetComponent<SpellAbility>().TryCast();
        yield return new WaitForSeconds(0.5f);
        Assert.IsNull(Object.FindAnyObjectByType<Projectile>());
    }

    [UnityTest]
    public IEnumerator FireballHitLeavesImpactThatCleansUp()
    {
        yield return RemoveAllEnemies();
        player.transform.rotation = Quaternion.LookRotation(Vector3.left); // at the wall
        player.GetComponent<SpellAbility>().TryCast();
        yield return new WaitForSeconds(0.1f);
        Assert.IsNotNull(GameObject.Find("FireballImpact(Clone)"), "hitting the wall should spawn an impact");

        yield return new WaitForSeconds(1f); // the impact animation is ~0.36s
        Assert.IsNull(GameObject.Find("FireballImpact(Clone)"), "impact should destroy itself when done");
    }

    [UnityTest]
    public IEnumerator FireballNeedsMana()
    {
        var mana = player.GetComponent<Mana>();
        Assert.IsTrue(mana.TrySpend(45f)); // leaves 5, cost is 10
        Assert.IsFalse(player.GetComponent<SpellAbility>().TryCast());
        yield return null;
    }

    [UnityTest]
    public IEnumerator HudShowsHealthManaCooldownAndBanner()
    {
        GameSession.Settings.gentle = false; // Adventurer Mode: full damage and a Game Over banner
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        yield return null; // HudController looks its elements up in Start

        player.GetComponent<Health>().TakeDamage(2);
        player.GetComponent<SpellAbility>().TryCast();
        yield return null;

        var hearts = root.Q("hearts").Children().ToList();
        Assert.AreEqual(5, hearts.Count, "one heart per point of health");
        Assert.AreEqual(2, hearts.Count(h => h.ClassListContains("empty")), "two lost hearts show as outlines");
        Assert.AreEqual(80f, root.Q("mana-fill").style.width.value.value, 0.5f, "40 of 50 magic left");
        Assert.Greater(root.Q("spell-cooldown").style.height.value.value, 0f);
        Assert.IsFalse(root.Q("banner").ClassListContains("visible"));

        player.GetComponent<Health>().TakeDamage(99);
        yield return null;
        Assert.IsTrue(root.Q("banner").ClassListContains("visible"));
        Assert.AreEqual("Oh no! Try again?", root.Q<Label>("banner-title").text);
    }

    [UnityTest]
    public IEnumerator MinimapRevealsOnlyNearThePlayer()
    {
        yield return null;
        yield return null;
        var minimap = Object.FindAnyObjectByType<Minimap>();
        var map = Object.FindAnyObjectByType<LevelMap>();
        Vector2 p = map.WorldToMap(player.transform.position);
        Assert.IsTrue(minimap.IsExplored(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)));

        Vector2 exit = map.WorldToMap(Object.FindAnyObjectByType<ExitZone>().transform.position);
        Assert.IsFalse(minimap.IsExplored(Mathf.RoundToInt(exit.x), Mathf.RoundToInt(exit.y)));
    }

    [UnityTest]
    public IEnumerator ChestShowsPromptOpensOnceAndRestoresPlayer()
    {
        var chest = Object.FindAnyObjectByType<Chest>();
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        var health = player.GetComponent<Health>();
        var mana = player.GetComponent<Mana>();
        var interactor = player.GetComponent<PlayerInteractor>();
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false; // no interruptions

        health.TakeDamage(3);
        mana.TrySpend(30f);
        Assert.IsFalse(interactor.TryInteract(), "nothing should be in reach at the start");

        Teleport(player, chest.transform.position + new Vector3(0f, 1f, -1.5f));
        yield return null;
        yield return null;
        Assert.IsTrue(root.Q("interact-prompt").ClassListContains("visible"));
        Assert.AreEqual("E: Open chest", root.Q<Label>("interact-prompt").text);

        Assert.IsTrue(interactor.TryInteract());
        Assert.IsTrue(chest.IsOpen);
        Assert.AreEqual(health.Max, health.Current);
        Assert.AreEqual(mana.Max, mana.Current, 0.01f);

        yield return null;
        Assert.IsTrue(root.Q("toast").ClassListContains("visible"));
        Assert.IsFalse(root.Q("interact-prompt").ClassListContains("visible"), "an open chest has nothing left to do");
        Assert.IsFalse(interactor.TryInteract(), "a chest only opens once");
    }

    [UnityTest]
    public IEnumerator DecorIsPlacedFromTheMap()
    {
        yield return null;
        Assert.AreEqual(10, Object.FindObjectsByType<FlickerLight>().Length, "one light per T on the map, plus Bonesy's campfire");
        Assert.Greater(GameObject.Find("Decor").transform.childCount, 20, "grass tufts, torches and the chest");
    }

    [UnityTest]
    public IEnumerator ReachingExitWinsGame()
    {
        // The exit is sealed until the Slime King is defeated.
        Object.FindAnyObjectByType<BossAbilities>().Health.TakeDamage(999);
        Teleport(player, Object.FindAnyObjectByType<ExitZone>().transform.position);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.IsTrue(GameManager.Instance.IsGameOver);
    }

    [UnityTest]
    public IEnumerator AnimatorFollowsMovementAndActions()
    {
        var anim = player.GetComponent<CharacterAnimator>().Animator;
        yield return null;
        Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"));

        // Keep walking until the Animator switches to Walk (or give up after 1 second).
        var cc = player.GetComponent<CharacterController>();
        for (float t = 0f; t < 1f && !anim.GetCurrentAnimatorStateInfo(0).IsName("Walk"); t += Time.deltaTime)
        {
            cc.Move(new Vector3(0f, 0f, -4f) * Time.deltaTime);
            yield return null;
        }
        Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName("Walk"));

        player.GetComponent<SpellAbility>().TryCast();
        yield return null;
        yield return null;
        Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName("Cast"));

        player.GetComponent<Health>().TakeDamage(1);
        yield return null;
        yield return null;
        Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName("Hurt"));

        player.GetComponent<Health>().TakeDamage(99);
        yield return null;
        yield return null;
        Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName("Die"));
    }

    [UnityTest]
    public IEnumerator SpriteFlipsAndSwapsFacingWithAim()
    {
        var sr = player.GetComponentInChildren<SpriteRenderer>();
        var anim = player.GetComponent<CharacterAnimator>().Animator;
        var cam = Camera.main.transform;
        Vector3 screenUp = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;

        player.transform.rotation = Quaternion.LookRotation(cam.right - screenUp); // down-right
        yield return null;
        Assert.IsFalse(sr.flipX);
        Assert.AreEqual(0f, anim.GetFloat("FacingBack"));

        player.transform.rotation = Quaternion.LookRotation(-cam.right + screenUp); // up-left
        yield return null;
        Assert.IsTrue(sr.flipX);
        Assert.AreEqual(1f, anim.GetFloat("FacingBack"));
    }

    [UnityTest]
    public IEnumerator EnemyAnimatesAttackThenDiesAndIsRemoved()
    {
        var enemy = IsolatedEnemy();
        var anim = enemy.GetComponent<CharacterAnimator>().Animator;
        Teleport(player, enemy.transform.position + new Vector3(1f, 0f, 0f));

        bool sawAttack = false;
        for (float t = 0f; t < 1f && !sawAttack; t += Time.deltaTime)
        {
            yield return null;
            sawAttack = anim.GetCurrentAnimatorStateInfo(0).IsName("Attack");
        }
        Assert.IsTrue(sawAttack, "enemy should play its Attack state when it hits the player");

        enemy.GetComponent<Health>().TakeDamage(99);
        yield return null;
        yield return null;
        Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName("Die"));
        Assert.IsFalse(enemy.GetComponent<CharacterController>().enabled, "corpses shouldn't block shots");
        Assert.AreEqual(8, EnemyAI.AliveCount);

        yield return new WaitForSeconds(2.2f); // corpseLifetime is 2s
        Assert.IsTrue(enemy == null, "corpse should be removed"); // Unity overloads == for destroyed objects
    }

    [UnityTest]
    public IEnumerator FireballAutoTargetsNearestOnScreenEnemy()
    {
        var enemy = IsolatedEnemy(); // no other skeleton nearby, so it's unambiguously the nearest
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        Teleport(player, enemy.transform.position + new Vector3(-3f, 0f, 0f));
        player.transform.rotation = Quaternion.LookRotation(Vector3.left); // facing away
        yield return WaitForCamera();

        var ability = player.GetComponent<SpellAbility>();
        Assert.AreEqual(enemy, ability.FindTarget());
        Assert.IsTrue(ability.TryCast());

        var fireball = Object.FindAnyObjectByType<Projectile>();
        Vector3 toEnemy = enemy.transform.position - fireball.transform.position;
        toEnemy.y = 0f;
        Assert.Less(Vector3.Angle(fireball.transform.forward, toEnemy), 3f, "fireball should fly at the enemy");
        Assert.Greater(Vector3.Dot(player.transform.forward, Vector3.right), 0.9f, "wizard should turn to face it");
    }

    [UnityTest]
    public IEnumerator RingCanBePickedUpEquippedAndBoostsDamage()
    {
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var pickup = Object.FindAnyObjectByType<ItemPickup>();
        var inventory = player.GetComponent<Inventory>();
        var ability = player.GetComponent<SpellAbility>();
        var hud = Object.FindAnyObjectByType<HudController>();
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;

        Teleport(player, pickup.transform.position + new Vector3(0f, 1f, -1.2f));
        yield return null;
        yield return null;
        Assert.AreEqual("E: Pick up Ember Ring", root.Q<Label>("interact-prompt").text);

        Assert.IsTrue(player.GetComponent<PlayerInteractor>().TryInteract());
        yield return null;
        Assert.IsTrue(pickup == null, "pickup should be removed from the floor");
        Assert.AreEqual(1, inventory.Bag.Count);
        Assert.IsTrue(root.Q("bag-0").ClassListContains("has-item"));

        hud.SetInventoryOpen(true);
        Assert.AreEqual(1, ability.Damage);
        Assert.IsTrue(inventory.Equip(inventory.Bag[0]));
        Assert.AreEqual(2, ability.Damage);
        Assert.AreEqual(0, inventory.Bag.Count);
        Assert.IsTrue(root.Q("equip-ring").ClassListContains("has-item"));
        Assert.IsFalse(root.Q("bag-0").ClassListContains("has-item"));
        yield return null;
        Assert.AreEqual("Fireball damage: 2", root.Q<Label>("stat-damage").text); // the wizard is the default hero

        // With the ring, one fireball kills a 2 HP skeleton.
        var enemy = IsolatedEnemy(); // no other skeleton nearby, so it's unambiguously the nearest
        Teleport(player, enemy.transform.position + new Vector3(-3f, 0f, 0f));
        yield return WaitForCamera();
        Assert.IsTrue(ability.TryCast());
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(enemy.GetComponent<Health>().IsDead);

        Assert.IsTrue(inventory.Unequip(EquipSlot.Ring));
        Assert.AreEqual(1, ability.Damage);
    }

    // The skeleton farthest from any other enemy, so auto-aim can't pick a neighbour at the
    // same distance. (Skeletons only: these tests rely on their 2 health.) It also needs open
    // floor 4m to its west, where the tests stand to shoot it (the maze has narrow tunnels).
    private static EnemyAI IsolatedEnemy()
    {
        var all = Object.FindObjectsByType<EnemyAI>();
        EnemyAI best = null;
        float bestGap = -1f;
        foreach (var a in all)
        {
            if (!a.name.StartsWith("Skeleton")) continue;
            var knee = new Vector3(a.transform.position.x, 0.6f, a.transform.position.z); // below the 1.2m wall tops
            if (Physics.Raycast(knee, Vector3.left, 4.6f, ~0, QueryTriggerInteraction.Ignore))
                continue;
            float gap = float.MaxValue;
            foreach (var b in all)
                if (a != b) gap = Mathf.Min(gap, Vector3.Distance(a.transform.position, b.transform.position));
            if (gap > bestGap) { best = a; bestGap = gap; }
        }
        return best;
    }

    // The camera smoothly follows the player, so after a teleport it needs a moment to catch up.
    private static IEnumerator WaitForCamera()
    {
        yield return new WaitForSeconds(0.6f);
    }

    [UnityTest]
    public IEnumerator SoundsPlayForCastsHitsAndKills()
    {
        yield return null;
        var audio = AudioManager.Instance;
        Assert.IsNotNull(audio);
        Assert.AreEqual("music_dungeon", audio.Music.name);

        player.GetComponent<SpellAbility>().TryCast();
        Assert.AreEqual("cast_fire", audio.LastPlayed.name);

        var health = IsolatedEnemy().GetComponent<Health>();
        health.TakeDamage(1);
        Assert.AreEqual("enemy_hit", audio.LastPlayed.name);
        health.TakeDamage(1);
        Assert.AreEqual("enemy_death", audio.LastPlayed.name);
    }

    private static IEnumerator RemoveAllEnemies()
    {
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) Object.Destroy(e.gameObject);
        yield return null;
    }

    // CharacterController fights direct position changes, so disable it while moving.
    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
