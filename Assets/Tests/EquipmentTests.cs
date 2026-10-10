using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for the equipment slots (weapon, helm, armor, boots, ring): every kind of
// item is somewhere in the world, each one goes in its own slot, and its bonuses apply.
public class EquipmentTests
{
    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        SaveSystem.FolderOverride = null;
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    private static ItemPickup Pickup(string id) =>
        Object.FindObjectsByType<ItemPickup>().FirstOrDefault(p => p.Item.Id == id);

    // Picks the item up off the floor and puts it on.
    private static ItemDefinition PickUpAndWear(GameObject player, string id)
    {
        var pickup = Pickup(id);
        Assert.IsNotNull(pickup, $"{id} is lying in {SceneManager.GetActiveScene().name}");
        var item = pickup.Item;
        Assert.IsNotNull(pickup.Interact(player));
        Assert.IsTrue(player.GetComponent<Inventory>().Equip(item));
        return item;
    }

    [UnityTest]
    public IEnumerator EveryKindOfItemCanBeFoundSomewhere()
    {
        var slots = new Dictionary<EquipSlot, string>();
        foreach (var scene in new[] { "Level0", "House", "Cove", "Dungeon" })
        {
            GameSession.NewGame(null);
            yield return Load(scene);
            foreach (var pickup in Object.FindObjectsByType<ItemPickup>())
                slots[pickup.Item.Slot] = $"{pickup.Item.DisplayName} ({scene})";
        }
        // The Hat (the toilet frog's, after ten flushes) and the Charm (Old Moss's thank-you) are given, not found
        // lying about: WoodsAndSystemsTests checks both.
        foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            if (slot != EquipSlot.None && slot != EquipSlot.Hat && slot != EquipSlot.Charm)
                Assert.IsTrue(slots.ContainsKey(slot), $"no {slot} anywhere; found {string.Join(", ", slots.Values)}");
    }

    [UnityTest]
    public IEnumerator HelmAddsAHeartAndMagicWithoutAFreeHeal()
    {
        GameSession.NewGame(null);
        yield return Load("House");
        var player = LevelBootstrap.Current.Player;
        var health = player.GetComponent<Health>();
        var mana = player.GetComponent<Mana>();
        var inventory = player.GetComponent<Inventory>();
        int hearts = health.Max;
        float magic = mana.Max;

        var helm = PickUpAndWear(player, "plumed_helm");
        Assert.AreEqual(EquipSlot.Helm, helm.Slot);
        Assert.AreSame(helm, inventory.Equipped(EquipSlot.Helm));
        Assert.IsNull(inventory.Equipped(EquipSlot.Ring), "the helm goes on your head, not your finger");
        Assert.AreEqual(hearts + 1, health.Max);
        Assert.AreEqual(magic + 10f, mana.Max);
        Assert.AreEqual(hearts, health.Current, "an extra heart arrives empty");

        // The HUD shows it worn, and what it adds.
        yield return null;
        var root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;
        Assert.IsTrue(root.Q("equip-helm").ClassListContains("has-item"));
        Assert.IsFalse(root.Q("equip-armor").ClassListContains("has-item"));
        StringAssert.Contains("+1 heart", root.Q<Label>("stat-gear").text);
        StringAssert.Contains("+10 magic", root.Q<Label>("stat-gear").text);

        Assert.IsTrue(inventory.Unequip(EquipSlot.Helm));
        Assert.AreEqual(hearts, health.Max);
        Assert.AreEqual(magic, mana.Max);
        Assert.AreEqual(1, inventory.Bag.Count, "back in the bag");
    }

    [UnityTest]
    public IEnumerator BootsMakeYouWalkFaster()
    {
        GameSession.NewGame(null);
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var inventory = player.GetComponent<Inventory>();
        Assert.AreEqual(1f, inventory.MoveSpeedFactor);
        PickUpAndWear(player, "trailblazer_boots");
        Assert.AreEqual(1.2f, inventory.MoveSpeedFactor, 1e-4f);
    }

    [UnityTest]
    public IEnumerator ArmorAddsTwoHearts()
    {
        GameSession.NewGame(null);
        yield return Load("Cove");
        var player = LevelBootstrap.Current.Player;
        int hearts = player.GetComponent<Health>().Max;
        PickUpAndWear(player, "seashell_mail");
        Assert.AreEqual(hearts + 2, player.GetComponent<Health>().Max);
    }

    [UnityTest]
    public IEnumerator WandAndRingStackAndTheWandSpeedsUpCasting()
    {
        GameSession.NewGame(null);
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;
        var spell = player.GetComponent<SpellAbility>();
        float cooldown = spell.Cooldown;
        Assert.AreEqual(1, spell.Damage);

        PickUpAndWear(player, "starlight_wand");
        Assert.AreEqual(2, spell.Damage);
        Assert.AreEqual(cooldown * 0.85f, spell.Cooldown, 1e-4f);
        PickUpAndWear(player, "ember_ring");
        Assert.AreEqual(3, spell.Damage, "a weapon and a ring both count");
        Assert.IsEmpty(player.GetComponent<Inventory>().Bag);
    }

    [UnityTest]
    public IEnumerator WornGearSurvivesASaveAndLoad()
    {
        string folder = Path.Combine(Path.GetTempPath(), "TidecrownEquipTests_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        try
        {
            GameSession.NewGame(null);
            GameSession.Inventory.Equipped[EquipSlot.Boots] = "trailblazer_boots";
            GameSession.Inventory.Equipped[EquipSlot.Weapon] = "starlight_wand";
            GameSession.Inventory.Bag.Add("plumed_helm");
            SaveSystem.Save(0, "Dungeon", "");
            GameSession.NewGame(null);

            Assert.AreEqual("Dungeon", SaveSystem.Load(0, new CharacterDefinition[0]));
            yield return Load("Dungeon");
            var inventory = LevelBootstrap.Current.Player.GetComponent<Inventory>();
            Assert.AreEqual("trailblazer_boots", inventory.Equipped(EquipSlot.Boots).Id);
            Assert.AreEqual("starlight_wand", inventory.Equipped(EquipSlot.Weapon).Id);
            Assert.AreEqual("plumed_helm", inventory.Bag.Single().Id);
            Assert.AreEqual(2, LevelBootstrap.Current.Player.GetComponent<SpellAbility>().Damage);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    [Test]
    public void MapItemsAreCheckedAgainstKnownIds()
    {
        var map = MapFile.Parse("Shed", string.Join("\n",
            "title: The Shed", "theme: Home", "---", "####", "#P5#", "####", "---", "5 = item rusty_spoon"));
        Assert.IsEmpty(MapValidator.Validate(new[] { map }), "without a list of ids, any id is fine");
        StringAssert.Contains("there's no item 'rusty_spoon'",
            MapValidator.Validate(new[] { map }, new[] { "plumed_helm" }).Single());
        Assert.IsEmpty(MapValidator.Validate(new[] { map }, new[] { "rusty_spoon" }));
    }
}
