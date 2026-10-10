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
        Assert.AreEqual("The stairs open when every monster is beaten", hud.Q<Label>("objective-hint").text,
                        "the card says why the stairs are shut");

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
        foreach (var name in new[] { "ability-2", "ability-3" })
            Assert.AreEqual(DisplayStyle.None, hud.Q(name).resolvedStyle.display, $"{name} stays hidden until its skill is learned");
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

    [UnityTest]
    public IEnumerator GentleBumpsShowAsHalfHeartsAndHealingMendsThem()
    {
        yield return Load("Dungeon");
        Assert.IsTrue(GameSession.Settings.gentle, "Gentle Mode is the default");
        var health = player.GetComponent<Health>();
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var hearts = hud.Q("hearts").Children().ToList();

        health.TakeDamage(1); // half a heart
        yield return null;
        Assert.AreEqual(0, hearts.Count(h => h.ClassListContains("empty")));
        Assert.AreEqual(1, hearts.Count(h => h.ClassListContains("half")));
        Assert.IsTrue(hearts[health.Current - 1].ClassListContains("half"), "the last full heart is the broken one");

        health.TakeDamage(1); // the other half
        yield return null;
        Assert.AreEqual(1, hearts.Count(h => h.ClassListContains("empty")));
        Assert.AreEqual(0, hearts.Count(h => h.ClassListContains("half")));

        health.TakeDamage(1);
        health.Heal(1);
        yield return null;
        Assert.AreEqual(0, hearts.Count(h => h.ClassListContains("empty") || h.ClassListContains("half")),
            "healing mends the broken heart too");
    }

    [UnityTest]
    public IEnumerator LosingAHeartMakesItJumpAndTheScreenFlashRed()
    {
        yield return Load("Dungeon");
        GameSession.Settings.gentle = false;
        var health = player.GetComponent<Health>();
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var hearts = hud.Q("hearts").Children().ToList();
        var flash = hud.Q("hurt-flash");

        health.TakeDamage(1);
        Assert.IsTrue(flash.ClassListContains("visible"), "the edges flash right away");
        yield return null;
        Assert.IsTrue(hearts[health.Current].ClassListContains("hurt"), "the lost heart jumps");
        yield return new WaitForSeconds(0.4f);
        Assert.IsFalse(hearts[health.Current].ClassListContains("hurt"));
        Assert.IsFalse(flash.ClassListContains("visible"), "...both just for a moment");
    }

    [UnityTest]
    public IEnumerator TheLastHeartLeftBeats()
    {
        yield return Load("Dungeon");
        GameSession.Settings.gentle = false;
        var health = player.GetComponent<Health>();
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var hearts = hud.Q("hearts").Children().ToList();

        health.TakeDamage(health.Max - 1);
        bool beat = false;
        for (float t = 0f; t < 1.5f && !beat; t += Time.deltaTime)
        {
            yield return null;
            beat = hearts[0].ClassListContains("beat");
        }
        Assert.IsTrue(beat);
        Assert.IsFalse(hearts.Skip(1).Any(h => h.ClassListContains("beat")), "only the last one");
    }

    // Health never wraps onto a second row: hearts shrink past 8, and past 12 there's one heart
    // and a count. The heart stands for the hero's state either way.
    [UnityTest]
    public IEnumerator HealthStaysOnOneRowAtAnySize()
    {
        yield return Load("Dungeon");
        GameSession.Settings.gentle = false;
        var health = player.GetComponent<Health>();
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var row = hud.Q("hearts");
        var count = hud.Q<Label>("hearts-count");

        foreach (int max in new[] { 5, 8, 10, 12 })
        {
            health.SetMax(max);
            health.Revive();
            yield return null;
            yield return null; // a frame for layout
            var hearts = row.Children().ToList();
            Assert.AreEqual(max, hearts.Count);
            Assert.AreEqual(max > 8, row.ClassListContains("compact"), $"{max} hearts: compact only past 8");
            Assert.IsFalse(count.ClassListContains("visible"));
            // layout, not worldBound: refilled hearts are mid-pop (scaled), which worldBound includes.
            Assert.IsTrue(hearts.All(h => Mathf.Approximately(h.layout.y, hearts[0].layout.y)), $"{max} hearts on one row");
            Assert.LessOrEqual(row.worldBound.xMin + hearts.Last().layout.xMax, hud.Q("status").worldBound.xMax,
                $"{max} hearts fit the card");
        }

        health.SetMax(20);
        health.Revive();
        health.TakeDamage(13);
        yield return null;
        Assert.IsTrue(count.ClassListContains("visible"));
        Assert.AreEqual("7 / 20", count.text);
        Assert.AreEqual(1, row.Children().Count(h => h.resolvedStyle.display == DisplayStyle.Flex), "one heart and a count");
        Assert.IsFalse(row.Children().First().ClassListContains("empty"), "still standing: the heart is full");

        health.TakeDamage(7);
        yield return null;
        Assert.AreEqual("0 / 20", count.text);
        Assert.IsTrue(row.Children().First().ClassListContains("empty"));
    }

    [UnityTest]
    public IEnumerator CastingFlashesAStarOverTheSpellSlot()
    {
        yield return Load("Dungeon");
        var spell = player.GetComponent<SpellAbility>();
        spell.enabled = true;
        Assert.IsTrue(spell.TryCast());
        Assert.IsTrue(hud.Q("spell-flash").ClassListContains("visible"));
        Assert.IsTrue(hud.Q("slot-spell").ClassListContains("cast"));
        yield return new WaitForSeconds(0.3f);
        Assert.IsFalse(hud.Q("spell-flash").ClassListContains("visible"), "just a flash");
        Assert.IsFalse(hud.Q("slot-spell").ClassListContains("cast"));
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
        NoOverlap("status", "boss-bar");
        NoOverlap("status", "slot-spell");
        NoOverlap("slot-spell", "help-pill");
        NoOverlap("objective", "boss-bar");
        NoOverlap("boss-bar", "minimap-frame");
        NoOverlap("objective", "minimap-frame");
        Assert.Less(hud.Q("status").worldBound.yMax, screen.height / 3f, "the status card is at the top");
        Assert.Greater(hud.Q("objective").worldBound.yMin, hud.Q("minimap-frame").worldBound.yMax, "the quest card is under the minimap");
    }

    private static void Teleport(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        cc.enabled = false;
        go.transform.position = pos;
        cc.enabled = true;
    }

    private static void Hover(VisualElement slot)
    {
        using (var enter = PointerEnterEvent.GetPooled()) { enter.target = slot; slot.SendEvent(enter); }
    }

    private static void Unhover(VisualElement slot)
    {
        using (var leave = PointerLeaveEvent.GetPooled()) { leave.target = slot; slot.SendEvent(leave); }
    }

    [UnityTest]
    public IEnumerator HoveringAnItemShowsATooltipWithItsDescriptionAndAttributes()
    {
        yield return Load("Dungeon");
        var controller = Object.FindAnyObjectByType<HudController>();
        GameSession.Inventory.Bag.Add("starlight_wand");
        GameSession.Inventory.Bag.Add("pancakes");
        GameSession.Inventory.KeyItems.Add("bouncy_boots");
        controller.SetInventoryOpen(true);
        player.GetComponent<Inventory>().Add("ember_ring"); // redraws the bag
        yield return null;

        Assert.IsFalse(controller.TooltipShown);
        var tip = hud.Q("item-tooltip");
        Assert.IsFalse(tip.ClassListContains("visible"));

        Hover(hud.Q("bag-0"));
        Assert.IsTrue(controller.TooltipShown);
        StringAssert.Contains("Starlight Wand", controller.TooltipText);
        StringAssert.Contains("fallen star", controller.TooltipText, "the description");
        StringAssert.Contains("+1 spell damage", controller.TooltipText, "each attribute on its own line");
        StringAssert.Contains("spells recharge 15% faster", controller.TooltipText);
        StringAssert.Contains("Weapon", hud.Q<Label>("tip-kind").text);
        StringAssert.Contains("Click to wear", hud.Q<Label>("tip-action").text);
        StringAssert.DoesNotContain("fallen star", hud.Q<Label>("item-details").text, "no longer down at the bottom of the inventory");
        Unhover(hud.Q("bag-0"));
        Assert.IsFalse(controller.TooltipShown);

        Hover(hud.Q("bag-1"));
        StringAssert.Contains("+3 hearts", controller.TooltipText, "food says what it gives back");
        Assert.AreEqual("Food", hud.Q<Label>("tip-kind").text);
        Unhover(hud.Q("bag-1"));

        controller.ShowTreasures(true);
        Hover(hud.Q("key-0"));
        StringAssert.Contains("Bouncy Boots", controller.TooltipText);
        StringAssert.Contains("hop right over", controller.TooltipText);
        Assert.AreEqual("Treasure", hud.Q<Label>("tip-kind").text);
        controller.SetInventoryOpen(false);
        Assert.IsFalse(controller.TooltipShown, "closing the inventory puts it away");
    }

    [UnityTest]
    public IEnumerator EmptyAndWornSlotsOnTheDollHaveTooltipsToo()
    {
        yield return Load("Dungeon");
        var controller = Object.FindAnyObjectByType<HudController>();
        controller.SetInventoryOpen(true);
        yield return null;

        Hover(hud.Q("equip-helm"));
        StringAssert.Contains("Empty slot", controller.TooltipText);
        Unhover(hud.Q("equip-helm"));

        var inventory = player.GetComponent<Inventory>();
        inventory.Add("plumed_helm");
        inventory.Equip(inventory.Bag[0]);
        yield return null;
        Hover(hud.Q("equip-helm"));
        StringAssert.Contains("Plumed Helm", controller.TooltipText);
        StringAssert.Contains("worn", hud.Q<Label>("tip-kind").text);
        StringAssert.Contains("Click to take it off", controller.TooltipText);
    }

    [UnityTest]
    public IEnumerator TheWornSlotsSitOnAPaperDollOfTheHero()
    {
        yield return Load("Dungeon");
        var doll = hud.Q("doll");
        Assert.IsNotNull(doll);
        Assert.IsNotNull(doll.Q("doll-figure").resolvedStyle.backgroundImage.sprite, "the hero's own picture is the body");
        foreach (var slot in new[] { "hat", "helm", "charm", "armor", "weapon", "ring", "boots" })
            Assert.IsNotNull(doll.Q($"equip-{slot}"), $"{slot} is on the doll");
        yield return null;
        Object.FindAnyObjectByType<HudController>().SetInventoryOpen(true);
        yield return null;
        yield return null;
        var head = doll.Q("equip-helm").worldBound;
        var feet = doll.Q("equip-boots").worldBound;
        var torso = doll.Q("equip-armor").worldBound;
        Assert.Less(head.y, torso.y, "the helm is above the chest...");
        Assert.Less(torso.y, feet.y, "...and the boots are at the bottom");
        Assert.Less(doll.Q("equip-ring").worldBound.x, torso.x, "one hand each side of the body");
        Assert.Greater(doll.Q("equip-weapon").worldBound.x, torso.x);
    }
}
