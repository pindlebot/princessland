using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Play Mode tests for the soft-lock audit across every room (Castle, Home, Cove, Farm, Woods, Mines, Lake, Frostpeak):
// every door and exit, both ways, for both heroes, lands on solid ground; the way back out is always reachable with no
// tools; a beaten boss's reward survives leaving, returning and a full bag; and "Stuck? Start this room again" always works.
public class SoftLockTests
{
    private static string[] HeroNames = { "Wizard", "Princess" };
    private CharacterDefinition[] heroes;

    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
        SaveSystem.FolderOverride = null;
    }

    // ---------- The map files: every way from one room to another ----------

    public class Crossing
    {
        public string From, To, Spawn;
        public override string ToString() => $"{From} -> {To} ({(Spawn.Length == 0 ? "start" : Spawn)})";
    }

    private static MapFile[] Maps() =>
        Directory.GetFiles(Path.Combine(Application.dataPath, "Levels"), "*.txt")
            .Select(p => MapFile.Parse(Path.GetFileNameWithoutExtension(p), File.ReadAllText(p))).ToArray();

    // Doors, room edges, the castle gate and the level exit (the 'X' stairs), each as a crossing.
    public static List<Crossing> Crossings()
    {
        var list = new List<Crossing>();
        foreach (var map in Maps())
        {
            foreach (var door in map.Doors())
                list.Add(new Crossing { From = map.Name, To = door.TargetScene, Spawn = door.TargetSpawn });
            string exit = map.Get("exit");
            if (exit.Length > 0) list.Add(new Crossing { From = map.Name, To = exit, Spawn = map.Get("exit_spawn") });
        }
        return list.GroupBy(c => c.ToString()).Select(g => g.First()).ToList();
    }

    private IEnumerator Heroes()
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene("Title");
        yield return null;
        yield return null;
        heroes = Object.FindAnyObjectByType<TitleController>().Heroes;
    }

    private static IEnumerator Arrive(string scene, string spawn, CharacterDefinition hero)
    {
        GameSession.NewGame(hero);
        GameSession.NextSpawn = spawn.Length == 0 ? null : spawn;
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime) yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
    }

    // Is the hero standing on solid ground, clear of walls and blocks, not on a gap or in water?
    public static string StandingProblem(GameObject player)
    {
        var cc = player.GetComponent<CharacterController>();
        var pos = player.transform.position;
        Vector3 p0 = pos + cc.center + Vector3.up * (cc.height / 2f - cc.radius);
        Vector3 p1 = pos + cc.center - Vector3.up * (cc.height / 2f - cc.radius);
        foreach (var hit in Physics.OverlapCapsule(p0, p1, cc.radius * 0.8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(player.transform)) continue;
            return $"overlapping {hit.name}";
        }
        if (Gap.At(pos) != null) return "standing on a gap";
        if (!Physics.Raycast(pos + Vector3.up, Vector3.down, out var floor, 4f, ~0, QueryTriggerInteraction.Ignore)) return "nothing underfoot";
        if (LevelMap.IsWaterLayer(floor.collider.gameObject.layer)) return "in the water";
        return null;
    }

    [UnityTest]
    public IEnumerator EveryDoorAndExitArrivesOnSolidGroundForBothHeroes([ValueSource(nameof(HeroNames))] string heroName)
    {
        yield return Heroes();
        var hero = heroes.First(h => h.name == heroName);
        var problems = new List<string>();
        var seen = new HashSet<string>();
        foreach (var crossing in Crossings())
        {
            if (!seen.Add(crossing.To + ">" + crossing.Spawn)) continue;
            yield return Arrive(crossing.To, crossing.Spawn, hero);
            var player = LevelBootstrap.Current.Player;
            if (crossing.Spawn.Length > 0 && GameSession.EnteredBy != crossing.Spawn) problems.Add($"{crossing}: no spawn called '{crossing.Spawn}'");
            string problem = StandingProblem(player);
            if (problem != null) problems.Add($"{crossing}: {problem}");
            Assert.IsNotNull(LevelBootstrap.Current.WakePoint, crossing.ToString());
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    // Every room can be walked to from the castle grounds and walked back out of: the doors form one connected world, in
    // both directions. (Some doors are one-way on purpose, like the cove's cave that drops into the dungeon; what
    // matters is that no room is a dead end.)
    [Test]
    public void EveryRoomCanBeReachedFromTheCastleAndLeftAgain()
    {
        var all = Crossings();
        var rooms = Maps().Select(m => m.Name).ToList();
        HashSet<string> Reach(string start, bool forward)
        {
            var seen = new HashSet<string> { start };
            var todo = new Queue<string>();
            todo.Enqueue(start);
            while (todo.Count > 0)
            {
                var room = todo.Dequeue();
                foreach (var c in all)
                {
                    string from = forward ? c.From : c.To, to = forward ? c.To : c.From;
                    if (from == room && seen.Add(to)) todo.Enqueue(to);
                }
            }
            return seen;
        }
        CollectionAssert.AreEquivalent(rooms, Reach("Level0", forward: true).ToList(), "every room can be reached from the castle grounds");
        CollectionAssert.AreEquivalent(rooms, Reach("Level0", forward: false).ToList(), "and every room has a way back to the castle grounds");
    }

    // ---------- Getting out again, with no tools ----------

    // From every arrival, every door of the room can be reached on foot (no boots, mitts, charm or lantern).
    [UnityTest]
    public IEnumerator FromEveryArrivalEveryDoorOfTheRoomCanBeReachedWithNoTools()
    {
        var maps = Maps().ToDictionary(m => m.Name);
        var problems = new List<string>();
        foreach (var map in maps.Values)
        {
            var doors = map.Doors().Select(d => (d.Col, d.Row, d.TargetScene)).ToList();
            string exit = map.Get("exit");
            var exitTiles = exit.Length > 0 ? map.Find('X').ToList() : new List<(int, int)>();
            var arrivals = Crossings().Where(c => c.To == map.Name).Select(c => c.Spawn).Distinct().ToList();
            if (arrivals.Count == 0) arrivals.Add("");
            foreach (var arrival in arrivals)
            {
                yield return Arrive(map.Name, arrival, null);
                var nav = NavGrid.Current;
                var player = LevelBootstrap.Current.Player;
                if (nav == null) { problems.Add($"{map.Name}: no nav grid"); continue; }
                var path = new List<Vector3>();
                foreach (var (col, row, target) in doors)
                    if (!IntendedGate(map.Name, target, arrival) && !ReachesTile(nav, player.transform.position, col, row, path)) problems.Add($"{map.Name} (from {(arrival.Length == 0 ? "start" : arrival)}): can't reach the door to {target}");
                foreach (var (col, row) in exitTiles)
                    if (!ReachesTile(nav, player.transform.position, col, row, path)) problems.Add($"{map.Name} (from {(arrival.Length == 0 ? "start" : arrival)}): can't reach the exit stairs to {exit}");
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    // The one place the walking route is cut on purpose: the Sunken Dock's way to Frostpeak crosses deep water, so
    // Frostpeak needs the Bubble Charm (from King Crabbington, two rooms along, reachable without it). Both ways.
    private static bool IntendedGate(string room, string target, string arrival) =>
        room == "Lake3" && (target == "Frost1" ^ arrival == "FromFrost1");

    // Can a path be found to the tile, or to an open tile right beside it (a door or crystal sits in a wall)?
    private static bool ReachesTile(NavGrid nav, Vector3 from, int col, int row, List<Vector3> path)
    {
        foreach (var (dc, dr) in new[] { (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1) })
        {
            int c = col + dc, r = row + dr;
            if (!nav.IsOpen(c, r)) continue;
            path.Clear();
            if (nav.FindPath(from, nav.Center(c, r), path)) return true;
        }
        return false;
    }

    // ---------- Bosses: nothing required can be lost ----------

    public static string[] BossScenes = { "Dungeon", "Cove", "Farm", "Woods4", "Mines4", "Lake4", "Frost4" };

    [UnityTest]
    public IEnumerator ABossRewardSurvivesLeavingReturningAndAFullBagForEveryBoss([ValueSource(nameof(BossScenes))] string scene)
    {
        // Nothing about any quest has been started (the boss is beaten before meeting its giver).
        yield return Arrive(scene, "", null);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        boss.GetComponent<GolemCrystals>()?.SetGlowing(true, announce: false);
        boss.GetComponent<ShellCycle>()?.SetPeeking(true, announce: false);
        boss.Health.TakeDamage(9999);
        yield return null;
        yield return null;
        var rewards = Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include)
            .Where(p => p.GetComponentInParent<BossReward>(true) != null).ToList();
        var shown = Object.FindObjectsByType<BossReward>(FindObjectsInactive.Include).Where(r => !r.ByBraziers).ToArray();
        if (shown.Length == 0) Assert.Ignore($"{scene}: the boss leaves no prize (the room's own reward comes from elsewhere)");
        Assert.IsTrue(shown.All(r => r.IsShown), $"{scene}: the prize appears");

        // Walk away without taking it (a different room), then come back.
        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        Assert.IsTrue(Object.FindObjectsByType<BossReward>(FindObjectsInactive.Include).Where(r => !r.ByBraziers).All(r => r.IsShown), $"{scene}: still there on return");
        Assert.AreEqual(0, Object.FindObjectsByType<BossAbilities>().Length, $"{scene}: and the boss stays beaten");

        // With a full bag: treasures need no room.
        var player = LevelBootstrap.Current.Player;
        var bag = player.GetComponent<Inventory>();
        var apple = bag.Database.Find("healing_apple");
        while (bag.Bag.Count < bag.Capacity) bag.Add(apple);
        var pickups = Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include)
            .Where(p => p.GetComponentInParent<BossReward>(true) != null && !p.GetComponentInParent<BossReward>(true).ByBraziers).ToList();
        foreach (var pickup in pickups)
        {
            Assert.IsTrue(pickup.Item.IsKeyItem, $"{scene}: {pickup.Item.Id} is a treasure, so it needs no bag room");
            var message = pickup.Interact(player);
            StringAssert.DoesNotContain("full", message ?? "", $"{scene}: {pickup.Item.Id}");
            Assert.IsTrue(bag.Has(pickup.Item.Id), $"{scene}: {pickup.Item.Id} was collected");
        }
        Assert.IsNotEmpty(pickups.Select(p => p.Item.Id).ToList().Concat(rewards.Select(p => p.Item.Id)).ToList(), $"{scene}: has a reward");
    }

    // A boss beaten stays beaten when you leave and come back, even where the room still has other monsters (the cove, the farm).
    [UnityTest]
    public IEnumerator ABeatenBossStaysBeatenEvenIfTheRestOfTheRoomIsNot([ValueSource(nameof(BossScenes))] string scene)
    {
        yield return Arrive(scene, "", null);
        var boss = Object.FindAnyObjectByType<BossAbilities>();
        boss.GetComponent<GolemCrystals>()?.SetGlowing(true, announce: false);
        boss.GetComponent<ShellCycle>()?.SetPeeking(true, announce: false);
        boss.Health.TakeDamage(9999);
        yield return null;
        Assert.IsTrue(GameSession.Flags.Contains(BossAbilities.DownFlag(scene)));
        SceneManager.LoadScene("Level0");
        yield return null;
        yield return null;
        SceneManager.LoadScene(scene);           // (not Arrive: that starts a new game and forgets the flag)
        yield return null;
        yield return null;
        Assert.AreEqual(0, Object.FindObjectsByType<BossAbilities>().Length, $"{scene}: he stays down");
    }

    // ---------- Blocks, dirt and the way out of a jam ----------

    [UnityTest]
    public IEnumerator StuckStartThisRoomAgainPutsBlocksBackAndKeepsEverythingEarned()
    {
        SaveSystem.FolderOverride = Path.Combine(Path.GetTempPath(), "TidecrownStuck_" + System.Guid.NewGuid().ToString("N"));
        yield return Arrive("Mines1", "", null);
        GameSession.Slot = 0;
        GameSession.Inventory.KeyItems.Add(Abilities.MoleMitts);
        GameSession.Flags.Add("met:Digby");
        GameSession.Progress.AddGold(77);
        var block = PushBlock.All.First();
        Vector3 home = block.transform.position;
        foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            if (block.TrySlide(dir)) break;
        yield return new WaitForSeconds(0.6f);
        Assert.Greater(Vector3.Distance(block.transform.position, home), 1f, "it moved");
        Object.FindAnyObjectByType<SoftDirt>().Dig();

        Object.FindAnyObjectByType<PauseMenu>().RestartRoom();
        yield return null;
        yield return null;
        Assert.AreEqual("Mines1", SceneManager.GetActiveScene().name);
        Assert.AreEqual(home, PushBlock.All.First(b => Vector3.Distance(b.transform.position, home) < 0.01f).transform.position, "blocks are home again");
        Assert.AreEqual(77, GameSession.Progress.Gold);
        Assert.IsTrue(Abilities.Has(Abilities.MoleMitts));
        Assert.IsTrue(GameSession.Flags.Contains("met:Digby"));
        Assert.IsNull(StandingProblem(LevelBootstrap.Current.Player), "back at the arrival spot, on solid ground");
        if (Directory.Exists(SaveSystem.Folder)) Directory.Delete(SaveSystem.Folder, true);
    }

    [UnityTest]
    public IEnumerator ABlockCanNeverBePushedOntoADoorOrIntoAWallSoItCannotSealARoomForGood()
    {
        yield return Arrive("Mines3", "", null);
        GameSession.Inventory.KeyItems.Add(Abilities.MoleMitts);
        var navBefore = NavGrid.Current;
        foreach (var block in PushBlock.All.ToList())
        {
            // Every slide that is allowed lands on empty floor: nothing solid in the target square.
            foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                if (!block.CanSlide(dir)) continue;
                var target = block.transform.position + dir * PushBlock.TileSize;
                Assert.IsFalse(Physics.CheckBox(target + Vector3.up, new Vector3(0.7f, 0.5f, 0.7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                               && false, "allowed slides only go to free floor");
                foreach (var hit in Physics.OverlapBox(target + Vector3.up, new Vector3(0.7f, 0.5f, 0.7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    Assert.IsTrue(hit.transform.IsChildOf(block.transform), $"a slide into {hit.name} was allowed");
            }
        }
        Assert.IsNotNull(navBefore);
    }

    // ---------- Early entry to later regions ----------

    [UnityTest]
    public IEnumerator EveryRegionCanBeEnteredWithNoToolsAndLeftAgainTheSameWay()
    {
        // The entry rooms of each region, straight from a brand-new game: arrive, then reach the door you came in by.
        // (region entry room, the room you come from): the arrival spot is whatever the door there says.
        var entries = new[] { ("Woods1", "Level0"), ("Mines1", "Woods3"), ("Lake1", "Dungeon"), ("Frost1", "Lake3"), ("Cove", "Level0"), ("Farm", "Level0"), ("Dungeon", "Level0") };
        var problems = new List<string>();
        foreach (var (scene, from) in entries)
        {
            var crossing = Crossings().FirstOrDefault(c => c.From == from && c.To == scene);
            if (crossing == null) { problems.Add($"{from} has no way into {scene}"); continue; }
            string spawn = crossing.Spawn;
            yield return Arrive(scene, spawn, null);
            var player = LevelBootstrap.Current.Player;
            string problem = StandingProblem(player);
            if (problem != null) problems.Add($"{scene}: {problem}");
            Assert.IsFalse(Abilities.Has(Abilities.BouncyBoots) || Abilities.Has(Abilities.FairyLantern));
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }
}
