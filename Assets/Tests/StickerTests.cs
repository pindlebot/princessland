using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Tests for the sticker sets (Mines, Lake and Frostpeak): a sticker is earned when its condition holds, and told once.
public class StickerTests
{
    private string folder;

    [SetUp]
    public void UseTemporaryFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "TidecrownStickerTests_" + System.Guid.NewGuid().ToString("N"));
        SaveSystem.FolderOverride = folder;
        GameSession.NewGame(null);
    }

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        GameSession.Slot = -1;
        SaveSystem.FolderOverride = null;
        Time.timeScale = 1f;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }

    [Test]
    public void EachRegionHasAWholeSetOfUniqueStickers()
    {
        CollectionAssert.AllItemsAreUnique(StickerBook.All.Select(s => s.Id));
        foreach (var region in new[] { StickerBook.Mines, StickerBook.Lake, StickerBook.Frost })
            Assert.GreaterOrEqual(StickerBook.Total(region), 10, region + " has a set of stickers");
        foreach (var sticker in StickerBook.All)
        {
            Assert.IsNotEmpty(sticker.Name, sticker.Id);
            Assert.IsFalse(Condition.Met(sticker.Condition), sticker.Id + " isn't free from the start");
        }
    }

    [Test]
    public void AStickerIsAwardedOnceItsConditionHolds()
    {
        Assert.AreEqual(0, StickerBook.Award().Count);
        GameSession.Flags.Add("met:Digby");
        GameSession.AddToCounter("moles_found", 3);
        var fresh = StickerBook.Award();
        CollectionAssert.AreEquivalent(new[] { "digby", "lost_moles" }, fresh.Select(s => s.Id));
        Assert.AreEqual(2, GameSession.GetCounter(StickerBook.CountCounter));
        Assert.AreEqual(2, StickerBook.Earned(StickerBook.Mines));
        Assert.AreEqual(0, StickerBook.Earned(StickerBook.Lake));
        Assert.AreEqual(0, StickerBook.Award().Count, "told only once");
    }

    [Test]
    public void ItemsAndSecretsAreStickersToo()
    {
        GameSession.Inventory.KeyItems.Add(Abilities.BubbleCharm);
        GameSession.Inventory.KeyItems.Add("dragon_egg_frost");
        GameSession.Flags.Add("found:goldencarp");
        GameSession.Flags.Add("found:snowman");
        var ids = StickerBook.Award().Select(s => s.Id).ToList();
        CollectionAssert.IsSupersetOf(ids, new[] { "bubble_charm", "sapphire_egg", "golden_carp", "snowman" });
    }

    [UnityTest]
    public IEnumerator BeatingAMonsterCountsItAndTheWatcherHandsOutTheSticker()
    {
        SceneManager.LoadScene("Mines1");
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != "Mines1"; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        var bat = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Bat"));
        bat.GetComponent<Health>().TakeDamage(99);
        Assert.AreEqual(1, GameSession.GetCounter("defeated:Bat"));
        Assert.IsTrue(Object.FindAnyObjectByType<StickerWatcher>() != null, "the HUD has a watcher");
        yield return new WaitForSeconds(1.8f);
        Assert.IsTrue(GameSession.Flags.Contains("sticker:bat"), "a sticker for the Bat");
        Assert.IsTrue(GameSession.Flags.Contains("sticker:mines_visit"), "and one for coming to the Mines");
    }
}
