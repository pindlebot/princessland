using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the storybook HUD: hearts, objectives, the spell slot, hints,
// help, minimap markers, and the layout at several screen sizes.
public class HudTests
{
    private GameObject player;
    private VisualElement hud;

    private IEnumerator Load(string scene)
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
        player = LevelBootstrap.Current.Player;
        player.GetComponent<PlayerController>().enabled = false;
        player.GetComponent<SpellAbility>().enabled = false;
        hud = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;
    }

    [TearDown]
    public void Reset() => GameSession.NewGame(null);

    [UnityTest]
    public IEnumerator HeartsEmptyWhenHurtAndPopWhenHealed()
    {
        yield return Load("Dungeon");
        GameSession.Settings.gentle = false; // one point of damage = one heart
        var health = player.GetComponent<Health>();
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;

        health.TakeDamage(3);
        yield return null;
        var hearts = hud.Q("hearts").Children().ToList();
        Assert.AreEqual(health.Max, hearts.Count);
        Assert.AreEqual(3, hearts.Count(h => h.ClassListContains("empty")));

        health.Heal(2);
        yield return null;
        Assert.AreEqual(1, hearts.Count(h => h.ClassListContains("empty")));
        Assert.AreEqual(2, hearts.Count(h => h.ClassListContains("pop")), "the refilled hearts pop");
        yield return new WaitForSeconds(0.35f);
        Assert.AreEqual(0, hearts.Count(h => h.ClassListContains("pop")), "...just briefly");
    }

    [UnityTest]
    public IEnumerator ObjectiveCountsMonstersWithPipsThenCelebratesTheStairs()
    {
        yield return Load("Level0");
        Assert.AreEqual("6 monsters left", hud.Q<Label>("enemies-left").text);
        var pips = hud.Q("objective-pips").Children().ToList();
        Assert.AreEqual(6, pips.Count, "one marker per monster");
        Assert.AreEqual(0, pips.Count(p => p.ClassListContains("defeated")));
        Assert.AreEqual(DisplayStyle.None, hud.Q<Label>("objective-hint").style.display.value, "no extra words needed yet");

        var enemies = Object.FindObjectsByType<EnemyAI>();
        enemies[0].GetComponent<Health>().TakeDamage(99);
        yield return null;
        Assert.AreEqual("5 monsters left", hud.Q<Label>("enemies-left").text);
        Assert.AreEqual(1, hud.Q("objective-pips").Children().Count(p => p.ClassListContains("defeated")), "a star for each defeat");

        foreach (var e in enemies.Skip(1)) e.GetComponent<Health>().TakeDamage(99);
        yield return null;
        Assert.AreEqual(DisplayStyle.None, hud.Q("objective-monsters").style.display.value);
        Assert.AreEqual("The stairs are open!", hud.Q<Label>("objective-hint").text);
        Assert.IsTrue(hud.Q<Label>("objective-hint").ClassListContains("open"));
        Assert.AreEqual("The stairs are open!", hud.Q<Label>("toast").text);
        Assert.IsTrue(hud.Q("objective-pips").Children().All(p => p.ClassListContains("defeated")));

        // The celebration bounce comes and goes.
        bool bounced = false;
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            bounced |= hud.Q("objective").ClassListContains("celebrate");
            yield return null;
        }
        Assert.IsTrue(bounced);
        Assert.IsFalse(hud.Q("objective").ClassListContains("celebrate"));
    }

    [UnityTest]
    public IEnumerator OneBigSpellSlotWithItsKeyAndNoEmptySlots()
    {
        yield return Load("Dungeon");
        yield return null;
        Assert.AreEqual("Space", hud.Q<Label>("spell-key").text);
        foreach (var name in new[] { "slot-2", "slot-3" })
            Assert.AreEqual(DisplayStyle.None, hud.Q(name).resolvedStyle.display, $"{name} stays hidden until an ability needs it");
        Assert.AreEqual(DisplayStyle.Flex, hud.Q("slot-spell").resolvedStyle.display);
        Assert.IsTrue(hud.Q("slot-spell").ClassListContains("ready"), "glows when ready");

        player.GetComponent<SpellAbility>().TryCast();
        yield return null;
        Assert.IsFalse(hud.Q("slot-spell").ClassListContains("ready"));
        Assert.Greater(hud.Q("spell-cooldown").style.height.value.value, 0f, "a shade shows it recharging");
    }

    [UnityTest]
    public IEnumerator HintsGuideANewPlayerOneAtATime()
    {
        yield return Load("Dungeon");
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var hint = hud.Q<Label>("context-hint");
        Assert.AreEqual("W A S D: Walk", hint.text, "first, how to walk");
        Assert.IsTrue(hint.ClassListContains("visible"));

        // Walk over near a monster: now it suggests magic.
        var skeleton = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Skeleton"));
        Teleport(player, skeleton.transform.position + new Vector3(-4f, 0f, 0f));
        yield return new WaitForSeconds(0.6f); // let the camera catch up
        Assert.AreEqual("Space: Magic!", hint.text);

        player.GetComponent<SpellAbility>().TryCast();
        yield return null;
        Assert.IsFalse(hint.ClassListContains("visible"), "no nagging right after casting");
    }

    [UnityTest]
    public IEnumerator HelpPanelHoldsTheFullControls()
    {
        yield return Load("Dungeon");
        var hudController = Object.FindAnyObjectByType<HudController>();
        Assert.IsFalse(hudController.IsHelpOpen);
        Assert.AreEqual("H: Help", hud.Q<Label>("help-pill").text);
        hudController.SetHelpOpen(true);
        Assert.IsTrue(hud.Q("help").ClassListContains("open"));
        Assert.GreaterOrEqual(hud.Q("help").Query<Label>(className: "help-line").ToList().Count, 5);
    }

    [UnityTest]
    public IEnumerator MinimapShowsACrownAndTheStairs()
    {
        yield return Load("Level0");
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        yield return null;
        Assert.AreEqual(1, hud.Query(className: "map-crown").ToList().Count, "the hero is a crown");

        var exit = Object.FindAnyObjectByType<ExitZone>();
        Teleport(player, exit.transform.position + new Vector3(-2f, 0f, -2f));
        yield return null;
        yield return null;
        Assert.IsTrue(hud.Query(className: "map-stairs-locked").ToList().Any(m => m.style.display.value == DisplayStyle.Flex),
            "grey stairs while locked");

        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.GetComponent<Health>().TakeDamage(99);
        yield return null;
        yield return null;
        Assert.IsTrue(hud.Query(className: "map-stairs").ToList().Any(m => m.style.display.value == DisplayStyle.Flex),
            "green stairs once open");
    }

    // The HUD at several screen sizes: everything stays on screen, and nothing important overlaps.
    [UnityTest]
    public IEnumerator HudFitsAtSeveralScreenSizes([Values(1280, 1920, 1024, 2560)] int width)
    {
        int height = width == 1024 ? 768 : width == 2560 ? 1080 : width * 9 / 16;
        yield return Load("Dungeon");
        var doc = Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>();
        var panel = Object.Instantiate(doc.panelSettings);
        panel.targetTexture = new RenderTexture(width, height, 24); // lay the HUD out at this size
        doc.panelSettings = panel;
        hud = doc.rootVisualElement;
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        boss.Health.TakeDamage(1); // engage it, so its bar is on screen too
        for (int i = 0; i < 3; i++) yield return null;

        var screen = hud.worldBound;
        string[] names = { "objective", "minimap-frame", "status", "slot-spell", "spell-key", "help-pill", "boss-bar" };
        foreach (var name in names)
        {
            var box = hud.Q(name).worldBound;
            Assert.IsTrue(box.width > 0 && box.xMin >= screen.xMin - 0.5f && box.xMax <= screen.xMax + 0.5f
                          && box.yMin >= screen.yMin - 0.5f && box.yMax <= screen.yMax + 0.5f,
                $"{name} should be fully on screen at {width}x{height} (was {box})");
        }
        void NoOverlap(string a, string b) =>
            Assert.IsFalse(hud.Q(a).worldBound.Overlaps(hud.Q(b).worldBound), $"{a} and {b} overlap at {width}x{height}");
        NoOverlap("status", "slot-spell");
        NoOverlap("slot-spell", "help-pill");
        NoOverlap("objective", "boss-bar");
        NoOverlap("boss-bar", "minimap-frame");
        NoOverlap("objective", "minimap-frame");
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }
}
