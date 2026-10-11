using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for "why didn't that work?": every refused action (full bag, no magic, missing tool, locked door,
// a boss's closed shell) has its own picture and its own sound, shows a badge on the HUD, and doesn't nag when held.
// And the other half of boss feedback: a hit that lands looks and sounds different from one that bounces off.
public class FeedbackTests : InputTestFixture
{
    private Keyboard keyboard;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        ActionFeedback.Reset();
    }

    public override void TearDown()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
        base.TearDown();
    }

    private static IEnumerator Load(string scene, bool freezeEnemies = true)
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        if (freezeEnemies) foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        ActionFeedback.Reset();
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

    // ---------- The reasons themselves ----------

    [Test]
    public void EveryReasonHasItsOwnSoundAndItsOwnPicture()
    {
        var reasons = System.Enum.GetValues(typeof(FailReason)).Cast<FailReason>().ToList();
        var clips = reasons.Select(r => ActionFeedback.Clip(ActionFeedback.SoundName(r))).ToList();
        Assert.IsTrue(clips.All(c => c != null), "every sound file is there (Assets/Resources/Feedback)");
        Assert.AreEqual(reasons.Count, clips.Distinct().Count(), "no two reasons share a sound");

        var icons = reasons.Select(FeedbackArt.FailIcon).ToList();
        Assert.IsTrue(icons.All(i => i != null && i.texture != null));
        Assert.AreEqual(reasons.Count, icons.Distinct().Count(), "no two reasons share a picture");
        foreach (var r in reasons) Assert.IsNotEmpty(ActionFeedback.ShortText(r), $"{r} has a few words too");
        Assert.AreEqual(reasons.Count, reasons.Select(ActionFeedback.ShortText).Distinct().Count());
    }

    [UnityTest]
    public IEnumerator TheSameFailureDoesNotRepeatTooQuicklyButOthersStillSpeak()
    {
        yield return Load("Dungeon");
        Assert.IsTrue(ActionFeedback.Fail(FailReason.Locked));
        Assert.IsFalse(ActionFeedback.Fail(FailReason.Locked), "a too-quick repeat is quiet");
        Assert.IsTrue(ActionFeedback.Fail(FailReason.BagFull), "a different reason is not held back");
        Assert.AreEqual(2, ActionFeedback.Announced);
    }

    // ---------- Each way an action can fail ----------

    [UnityTest]
    public IEnumerator AFullBagSaysSoWithABagAndAThud()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        var bag = player.GetComponent<Inventory>();
        var filler = bag.Database.Find("healing_apple");
        Assert.IsNotNull(filler, "an ordinary bag item to fill the bag with");
        while (bag.Bag.Count < bag.Capacity) Assert.IsTrue(bag.Add(filler));
        ActionFeedback.Reset();
        Assert.IsFalse(bag.Add(filler));

        Assert.AreEqual(FailReason.BagFull, ActionFeedback.LastReason);
        Assert.AreEqual("fail_bag", ActionFeedback.LastSound.name);
        yield return null;
        var badge = Hud().Q("fail-badge");
        Assert.IsTrue(badge.ClassListContains("visible"), "the badge is up");
        Assert.AreEqual("Bag is full", Hud().Q<Label>("fail-text").text);
        Assert.IsNotNull(Hud().Q("fail-icon").style.backgroundImage.value.sprite, "with its picture");
    }

    [UnityTest]
    public IEnumerator PickingUpWithAFullBagAnnouncesIt()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        var bag = player.GetComponent<Inventory>();
        var pickup = Object.FindObjectsByType<ItemPickup>().FirstOrDefault(p => !p.Item.IsKeyItem);
        if (pickup == null) Assert.Ignore("no ordinary pickup in this scene");
        var filler = pickup.Item;
        while (bag.Bag.Count < bag.Capacity) bag.Add(filler);
        ActionFeedback.Reset();
        StringAssert.Contains("full", pickup.Interact(player));
        Assert.AreEqual(FailReason.BagFull, ActionFeedback.LastReason);
    }

    [UnityTest]
    public IEnumerator NoMagicSaysSoOncePerPressNotOncePerFrame()
    {
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        var mana = player.GetComponent<Mana>();
        mana.TrySpend(mana.Max);
        ActionFeedback.Reset();

        Press(keyboard.spaceKey);          // and hold it
        yield return new WaitForSeconds(1.0f);
        Release(keyboard.spaceKey);
        Assert.AreEqual(FailReason.LowMana, ActionFeedback.LastReason);
        Assert.AreEqual("fail_mana", ActionFeedback.LastSound.name);
        Assert.AreEqual(1, ActionFeedback.Announced, "one cue for the whole held press");
        Assert.IsTrue(Hud().Q("fail-badge").ClassListContains("visible"));
        Assert.AreEqual("Out of magic", Hud().Q<Label>("fail-text").text);
    }

    [UnityTest]
    public IEnumerator ALockedDoorRattlesAndShowsAPadlock()
    {
        yield return Load("Dungeon");
        var door = Object.FindObjectsByType<DungeonDoor>().FirstOrDefault(d => d.IsLocked);
        Assert.IsNotNull(door, "the dungeon has a locked treasure-room door");
        StringAssert.Contains("locked", door.Interact(LevelBootstrap.Current.Player));
        Assert.AreEqual(FailReason.Locked, ActionFeedback.LastReason);
        Assert.IsNotNull(ActionFeedback.LastSound);
    }

    [UnityTest]
    public IEnumerator ATrackWithoutTheToolShowsTheToolItself()
    {
        yield return Load("Dungeon");
        Assert.IsTrue(ActionFeedback.MissingTool(Abilities.BouncyBoots));
        Assert.AreEqual(FailReason.MissingTool, ActionFeedback.LastReason);
        Assert.AreEqual("fail_tool", ActionFeedback.LastSound.name);
        StringAssert.Contains("Boots", Hud().Q<Label>("fail-text").text, "it names the boots");
    }

    [UnityTest]
    public IEnumerator LeaningOnAGapWithoutBootsSaysYouNeedThem()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var gap = Gap.All.FirstOrDefault();
        if (gap == null) Assert.Ignore("no gap in the castle grounds");
        Assert.IsFalse(Abilities.Has(Abilities.BouncyBoots));

        // Find a side of the gap tile with open floor to stand on, then walk into it.
        foreach (var side in new[] { Vector3.left, Vector3.right, Vector3.back, Vector3.forward })
        {
            Vector3 stand = gap.Center + side * (Gap.TileSize * 0.5f + 0.6f);
            if (Gap.At(stand) != null) continue;
            Teleport(player, new Vector3(stand.x, player.transform.position.y, stand.z));
            yield return null;
            foreach (var k in new[] { keyboard.wKey, keyboard.aKey, keyboard.sKey, keyboard.dKey })   // (the camera is angled: try each key)
            {
                Teleport(player, new Vector3(stand.x, player.transform.position.y, stand.z));
                Press(k);
                yield return new WaitForSeconds(0.6f);
                Release(k);
                if (ActionFeedback.Announced > 0) break;
            }
            if (ActionFeedback.Announced > 0) break;
        }
        Assert.AreEqual(1, ActionFeedback.Announced, "once, however long they lean");
        Assert.AreEqual(FailReason.MissingTool, ActionFeedback.LastReason);
    }

    // ---------- Boss hits: landed vs bounced ----------

    [UnityTest]
    public IEnumerator CrabbingtonsShellBouncesHitsAndHisOpenBodyTakesThem()
    {
        yield return Load("Lake4", freezeEnemies: false);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var shell = boss.GetComponent<ShellCycle>();
        var health = boss.Health;
        int before = health.Current;

        shell.SetPeeking(false, announce: false);
        int bounced = HitFeedback.Deflections, landed = HitFeedback.Impacts;
        health.TakeDamage(3);
        Assert.AreEqual(before, health.Current, "the shell takes it all");
        Assert.AreEqual(bounced + 1, HitFeedback.Deflections);
        Assert.AreEqual(landed, HitFeedback.Impacts);
        Assert.AreEqual(FailReason.ShellClosed, ActionFeedback.LastReason);
        Assert.AreEqual("deflect", ActionFeedback.LastSound.name);
        Assert.AreEqual(1, Object.FindObjectsByType<SpriteRenderer>().Count(s => s.sprite == FeedbackArt.DeflectShield() && s.name == "Art"), "a shield pops over him");

        shell.SetPeeking(true, announce: false);
        health.TakeDamage(3);
        Assert.AreEqual(before - 3, health.Current, "while he peeks, it hurts");
        Assert.AreEqual(bounced + 1, HitFeedback.Deflections, "that one did not bounce");
        Assert.AreEqual(landed + 1, HitFeedback.Impacts, "it burst gold instead");
        Assert.IsTrue(Object.FindObjectsByType<SpriteRenderer>().Any(s => s.sprite == FeedbackArt.ImpactBurst() && s.name == "Art"));
    }

    [UnityTest]
    public IEnumerator TheGolemsDarkStoneBouncesHitsAndHisGlowTakesThem()
    {
        yield return Load("Mines4", freezeEnemies: false);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var crystals = boss.GetComponent<GolemCrystals>();
        int before = boss.Health.Current;

        crystals.SetGlowing(false, announce: false);
        int bounced = HitFeedback.Deflections;
        boss.Health.TakeDamage(2);
        Assert.AreEqual(before, boss.Health.Current);
        Assert.AreEqual(bounced + 1, HitFeedback.Deflections);

        crystals.SetGlowing(true, announce: false);
        int landed = HitFeedback.Impacts;
        boss.Health.TakeDamage(2);
        Assert.AreEqual(before - 2, boss.Health.Current);
        Assert.AreEqual(landed + 1, HitFeedback.Impacts);
        Assert.AreEqual(bounced + 1, HitFeedback.Deflections);
    }

    [UnityTest]
    public IEnumerator ABouncedHitDoesNotFlashWhiteLikeALandedOne()
    {
        yield return Load("Mines4", freezeEnemies: false);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var crystals = boss.GetComponent<GolemCrystals>();
        LogAssert.ignoreFailingMessages = true;   // (sprite materials have no emission colour to flash: the timer is what we read)
        var timer = typeof(Health).GetField("flashTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Func<bool> flashing = () => (float)timer.GetValue(boss.Health) > 0f;

        crystals.SetGlowing(false, announce: false);
        boss.Health.TakeDamage(2);
        Assert.IsFalse(flashing(), "a bounce is not a hit: no white flash");

        crystals.SetGlowing(true, announce: false);
        boss.Health.TakeDamage(2);
        Assert.IsTrue(flashing(), "a landed hit flashes");
    }

    [UnityTest]
    public IEnumerator TheBossWearsAShieldOrAStarOverHisHeadSoTheWindowCanBeReadFromAfar()
    {
        yield return Load("Mines4", freezeEnemies: false);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        var crystals = boss.GetComponent<GolemCrystals>();
        var cue = boss.GetComponent<VulnerabilityCue>();
        Assert.IsNotNull(cue);

        crystals.SetGlowing(true, announce: false);
        yield return null;
        yield return null;
        Assert.IsTrue(cue.ShowingOpen, "a gold star while the crystals glow");
        crystals.SetGlowing(false, announce: false);
        yield return null;
        yield return null;
        Assert.IsFalse(cue.ShowingOpen, "a shield while it is dark");
    }
}
