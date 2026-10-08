using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for what survives going through doors: the inventory and the world's
// used-up things (picked-up items, opened chests).
public class PersistenceTests
{
    [TearDown]
    public void Reset() => GameSession.NewGame(null);

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    [UnityTest]
    public IEnumerator InventoryAndUsedThingsSurviveSceneChanges()
    {
        GameSession.NewGame(null);
        yield return Load("Dungeon");
        var player = LevelBootstrap.Current.Player;

        // Pick up the ring, equip it, and open the chest.
        var pickup = Object.FindAnyObjectByType<ItemPickup>();
        Assert.IsNotNull(pickup.Interact(player));
        var inventory = player.GetComponent<Inventory>();
        Assert.IsTrue(inventory.Equip(inventory.Bag[0]));
        Assert.AreEqual(2, player.GetComponent<SpellAbility>().Damage);
        var chest = Object.FindAnyObjectByType<Chest>();
        chest.Interact(player);
        int goldAfterChest = GameSession.Progress.Gold;
        Assert.AreEqual(25, goldAfterChest);

        // Through a door to another scene: the ring is still equipped on the new player.
        yield return Load("Level0");
        player = LevelBootstrap.Current.Player;
        inventory = player.GetComponent<Inventory>();
        Assert.IsNotNull(inventory.Equipped(EquipSlot.Ring), "the equipped ring came along");
        Assert.AreEqual("ember_ring", inventory.Equipped(EquipSlot.Ring).Id);
        Assert.AreEqual(2, player.GetComponent<SpellAbility>().Damage, "and still adds its damage");

        // Back again: the ring isn't lying on the floor a second time, and the chest stays open and empty.
        yield return Load("Dungeon");
        Assert.IsNull(Object.FindAnyObjectByType<ItemPickup>(), "no second ring on the floor");
        chest = Object.FindAnyObjectByType<Chest>();
        Assert.IsTrue(chest.IsOpen, "the chest is still open");
        Assert.IsFalse(chest.CanInteract);
        Assert.AreEqual(goldAfterChest, GameSession.Progress.Gold);

        // A new game forgets all of it.
        GameSession.NewGame(null);
        yield return Load("Dungeon");
        Assert.IsNotNull(Object.FindAnyObjectByType<ItemPickup>(), "a fresh game has the ring again");
        Assert.IsFalse(Object.FindAnyObjectByType<Chest>().IsOpen);
        Assert.AreEqual(0, LevelBootstrap.Current.Player.GetComponent<Inventory>().Bag.Count);
    }
}
