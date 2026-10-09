using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Tests for the level text files (Assets/Levels/*.txt), their parser and the door validator.
public class MapFileTests
{
    private static MapFile[] RealMaps() =>
        Directory.GetFiles(Path.Combine(Application.dataPath, "Levels"), "*.txt")
            .Select(p => MapFile.Parse(Path.GetFileNameWithoutExtension(p), File.ReadAllText(p)))
            .ToArray();

    [Test]
    public void EveryLevelFileIsValidAndBuilt()
    {
        var maps = RealMaps();
        CollectionAssert.IsSupersetOf(maps.Select(m => m.Name).ToList(), new[] { "Level0", "Dungeon", "House" });
        var errors = MapValidator.Validate(maps);
        Assert.IsEmpty(errors, string.Join("\n", errors));
        foreach (var map in maps)
            Assert.GreaterOrEqual(SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{map.Name}.unity"), 0,
                $"{map.Name} is in Build Settings (rebuild with Dungeon > Rebuild All Scenes)");
    }

    [Test]
    public void ParserReadsHeaderMapAndLegend()
    {
        var map = MapFile.Parse("Glade", string.Join("\n",
            "// a comment",
            "title: The Sleepy Glade",
            "theme: Outdoor",
            "---",
            "",
            "  HHHHH",
            "  H.P1H",
            "  HHHHH",
            "",
            "---",
            "1 = door Hollow FromGlade",
            "2 = spawn ByTheTree"));
        Assert.AreEqual("The Sleepy Glade", map.Get("title"));
        Assert.AreEqual("music_x", map.Get("music", "music_x"), "missing keys fall back");
        Assert.AreEqual(3, map.Rows.Length, "blank lines around the map are dropped");
        Assert.AreEqual("  H.P1H", map.Rows[1], "leading spaces are kept: they're empty tiles");
        Assert.AreEqual('P', map.At(4, 1));
        var door = map.Doors().Single();
        Assert.AreEqual(("Hollow", "FromGlade", 5, 1), (door.TargetScene, door.TargetSpawn, door.Col, door.Row));
        CollectionAssert.AreEquivalent(new[] { "FromHollow", "ByTheTree" }, map.SpawnNames().ToList());

        var broken = Assert.Throws<System.FormatException>(() => MapFile.Parse("Oops", "title: x\n---\nH.P\n---\nnot a legend line"));
        StringAssert.Contains("Oops:5", broken.Message, "errors name the file and line");
        Assert.Throws<System.FormatException>(() => MapFile.Parse("NoLegend", "title: x\n---\nH.P"));
    }

    private static MapFile Map(string name, string body, string legend, string header = "title: T\ntheme: Outdoor") =>
        MapFile.Parse(name, header + "\n---\n" + body + "\n---\n" + legend);

    [Test]
    public void ValidatorCatchesBrokenDoors()
    {
        var a = Map("A", "HHHH\nH.P1\nHHHH", "1 = door B");                 // arrives at "FromA" in B
        var b = Map("B", "HHHH\nH.P2\nHHHH", "2 = door A");                 // has "FromA" (its door back)
        Assert.IsEmpty(MapValidator.Validate(new[] { a, b }), "two doors that lead to each other just work");

        var oneWay = Map("B", "HHHH\nH.P.\nHHHH", "");                       // no way back: no "FromA"
        StringAssert.Contains("arrives at 'FromA' in B, which has no such spot",
            MapValidator.Validate(new[] { a, oneWay }).Single());

        var namedSpot = Map("B", "HHHH\nH.Ps\nHHHH", "s = spawn FromA");    // ...unless it names one
        Assert.IsEmpty(MapValidator.Validate(new[] { a, namedSpot }));

        StringAssert.Contains("leads to 'B', which has no map file", MapValidator.Validate(new[] { a }).Single());
    }

    [Test]
    public void ValidatorCatchesOtherMistakes()
    {
        string Only(MapFile m) => string.Join(" | ", MapValidator.Validate(new[] { m }));
        StringAssert.Contains("'?' is on the map", Only(Map("A", "H.P?H", "")));
        StringAssert.Contains("exactly one 'P'", Only(Map("A", "H.P.PH", "")));
        StringAssert.Contains("exactly one 'P'", Only(Map("A", "H...H", "")));
        StringAssert.Contains("the door isn't on the map", Only(Map("A", "H.PH", "1 = door A")));
        StringAssert.Contains("already a built-in tile", Only(Map("A", "H.PEH", "E = door A")));
        StringAssert.Contains("unknown kind 'portal'", Only(Map("A", "H.P1H", "1 = portal A")));
        StringAssert.Contains("the exit leads to 'Nowhere'", Only(Map("A", "H.PXH", "", "title: T\ntheme: Outdoor\nexit: Nowhere")));
        StringAssert.Contains("theme 'Space'", Only(Map("A", "H.PH", "", "title: T\ntheme: Space")));
        StringAssert.Contains("no floor next to it", Only(Map("A", "HHH\nH1H\nHHH\nH.P", "1 = door A FromA\nx = spawn FromA")));
    }

    [UnityTest]
    public IEnumerator DoorsInTheBuiltScenesMatchTheFiles()
    {
        SceneManager.LoadScene("House");
        yield return null;
        yield return null;
        var door = Object.FindObjectsByType<SceneDoor>().Single(d => d.TargetScene == "Level0"); // (not the stairs)
        var arrival = GameObject.Find("FromLevel0");
        Assert.IsNotNull(arrival, "the door made an arrival spot beside itself");
        Assert.Less(Vector3.Distance(arrival.transform.position, door.transform.position), 3f);
        GameSession.NewGame(null);
    }
}
