using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// A developer tool for checking the look of the game: launch the player with
//   Tidecrown.app/Contents/MacOS/Tidecrown -devcapture <folder> -screen-width 1280 -screen-height 720 -screen-fullscreen 0
// and it skips the title, visits a few spots in each level with different health values on
// the HUD, saves a screenshot of each into <folder>, and quits. Without -devcapture it does nothing.
// Add -capturescene <Scene> to visit just that level (e.g. -capturescene Cove).
public class DevCapture : MonoBehaviour
{
    private string folder;
    private string only; // -capturescene: just this level's shots

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-devcapture");
        if (i < 0 || i + 1 >= args.Length) return;
        Application.runInBackground = true; // keep going even if the window isn't in front
        var go = new GameObject("DevCapture");
        DontDestroyOnLoad(go);
        var capture = go.AddComponent<DevCapture>();
        capture.folder = args[i + 1];
        int j = System.Array.IndexOf(args, "-capturescene");
        if (j >= 0 && j + 1 < args.Length) capture.only = args[j + 1];
    }

    private IEnumerator Start()
    {
        Directory.CreateDirectory(folder);
        if (only == "Phase2")
        {
            yield return Phase2();
            Application.Quit();
            yield break;
        }
        if (only == null)
        {
            yield return Menu("Title", "title");
            yield return Menu("CharacterSelect", "character_select");
        }
        GameSession.Progress.AddGold(37);
        GameSession.Progress.AddXp(Progression.FirstLevelXp + 20); // level 2, with a skill point to spend

        yield return Visit("Level0", 'P', 0, 0, "level0_spawn", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", 'm', 1, 1, "level0_pond", max: 6, hearts: 3, manaFraction: 0.5f);
        yield return Visit("Level0", 'F', 0, 2, "level0_fountain", max: 9, hearts: 4, manaFraction: 0.2f);
        yield return Visit("Level0", 'f', 2, 1, "level0_edge", max: 12, hearts: 1, manaFraction: 0f);
        yield return Visit("Level0", 'D', -1, 1, "level0_cave", max: 12, hearts: 12, manaFraction: 1f);
        yield return Visit("Level0", 'V', 1, 2, "level0_camp", max: 12, hearts: 12, manaFraction: 1f);
        yield return Visit("Level0", '3', 2, 4, "level0_village_cathedral", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", '6', 0, 2, "level0_village_stall", max: 0, hearts: 0, manaFraction: 1f);
        yield return BuyBubbleBath("level0_village_shop");
        yield return Visit("Level0", '8', 0, 2, "level0_village_square", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", '9', -2, 2, "level0_village_hens", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", '+', -1, 6, "level0_village_gate", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", ')', 0, -2, "level0_farm_gate", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", '1', 0, 2, "farm_gate", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", '4', 1, 2, "farm_pumpkins", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", '2', 0, 2, "farm_barn", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", 'c', 0, 2, "farm_bonfire", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", '?', -2, 2, "farm_graveyard", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", '(', 1, 2, "farm_festival", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", '\\', -2, 2, "farm_festival_pumpkin", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", ']', 0, 1, "farm_maze_centre", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Farm", '>', 0, 3, "farm_maze_entrance", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Dungeon", 'P', 0, 0, "dungeon_spawn", max: 20, hearts: 7, manaFraction: 0.7f);
        yield return Visit("Dungeon", 'M', -4, 1, "dungeon_king_hall", max: 20, hearts: 7, manaFraction: 0.7f);
        yield return Visit("House", 'B', 1, 2, "house", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("House", 'W', 0, 1, "house_bathroom", max: 0, hearts: 0, manaFraction: 1f);
        yield return SitOnTheToilet("house_toilet");
        yield return Visit("House", '8', 0, 1, "house_bathroom_tub", max: 0, hearts: 0, manaFraction: 1f);
        yield return TakeABath("house_bath");
        yield return Visit("House", '8', 1, 1, "house_faucet_off", max: 0, hearts: 0, manaFraction: 1f);
        yield return TurnOnTheFaucet("house_faucet_on");
        yield return Visit("House", ')', 0, 2, "house_bath_plants", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("House", '7', 1, 1, "house_courtyard", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("House", '9', 2, 1, "house_courtyard_door", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("House", '3', 0, 1, "house_stairs", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Kitchen", 'P', 0, 0, "kitchen", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Kitchen", '6', 1, 2, "kitchen_island", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Kitchen", '4', 0, 1, "kitchen_stove", max: 6, hearts: 2, manaFraction: 0.3f);
        yield return Cook("kitchen_cooking");
        yield return InventoryShot("house_inventory");
        yield return Visit("Level0", '1', 0, 1, "level0_rowboat", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Cove", 'P', 0, 0, "cove_spawn", max: 8, hearts: 6, manaFraction: 0.8f);
        yield return Visit("Cove", '1', 0, -2, "cove_jetty", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Cove", '|', 8, 2, "cove_waterfalls", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Cove", '&', 5, 4, "cove_dark_mermaid", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Cove", '@', 10, 0, "cove_ship", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Cove", 'X', 0, 3, "cove_sea_cave", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Cove", 'J', 0, 2, "cove_pirates", max: 0, hearts: 0, manaFraction: 1f);
        if (only == null)
        {
            yield return Visit("Level0", 'P', 0, -2, "walk_start", max: 0, hearts: 0, manaFraction: 1f);
            yield return Walk("walk", new Vector3(1f, 0f, 0.35f), 3);
        }
        Application.Quit();
    }

    // -capturescene Phase2: the secret workshop, a room's edge, and the world map.
    private IEnumerator Phase2()
    {
        only = null;
        GameSession.NewGame(null);
        yield return Visit("Dungeon", '0', -2, 0, "p2_fakewall", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Dungeon", '!', 0, -2, "p2_workshop", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", '(', 3, 0, "p2_edge_level0", max: 0, hearts: 0, manaFraction: 1f);
        GameSession.Flags.Add("visited:Woods1");
        GameSession.Flags.Add("visited:Woods2");
        GameSession.Flags.Add("visited:Dungeon");
        GameSession.Flags.Add("visited:House");
        GameSession.Flags.Add("fountain:Level0");
        GameSession.Flags.Add(HintBubble.SeenFlag("Dungeon/17,3"));
        GameSession.Flags.Add(HintBubble.SeenFlag("Dungeon/59,35"));
        yield return Visit("Level0", 'P', 0, 0, "p2_level0_start", max: 0, hearts: 0, manaFraction: 1f);
        WorldMapView.Instance.Open();
        yield return new WaitForSecondsRealtime(0.6f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "p2_worldmap.png"));
        yield return new WaitForSecondsRealtime(0.6f);
        WorldMapView.Instance.Close();
    }

    private IEnumerator Menu(string scene, string shot)
    {
        if (SceneManager.GetActiveScene().name != scene) SceneManager.LoadScene(scene);
        yield return new WaitForSecondsRealtime(1.5f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
    }

    // Sits the hero on the toilet (they're next to it) and saves a picture.
    private IEnumerator SitOnTheToilet(string shot)
    {
        if (only != null && only != "House") yield break;
        foreach (var fixture in FindObjectsByType<HouseFixture>())
            if (fixture.name == "Toilet")
                fixture.Interact(LevelBootstrap.Current.Player);
        yield return new WaitForSecondsRealtime(1f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().StandUp();
    }

    // Meets Barnaby Badger, buys a bottle of bubble bath (the hero is standing at his stall),
    // and saves his thank-you, then the bag with the bottle in it.
    private IEnumerator BuyBubbleBath(string shot)
    {
        if (only != null && only != "Level0") yield break;
        var player = LevelBootstrap.Current.Player;
        var barnaby = FindAnyObjectByType<Merchant>();
        barnaby.Interact(player); // the introduction
        for (int i = 0; i < 50 && DialogueController.IsOpen; i++)
        {
            yield return new WaitForSecondsRealtime(0.05f);
            DialogueController.Instance.Advance();
        }
        yield return new WaitForSecondsRealtime(0.3f);
        barnaby.Interact(player); // and a bottle, please
        yield return new WaitForSecondsRealtime(2f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
        while (DialogueController.IsOpen)
        {
            DialogueController.Instance.Advance();
            yield return null;
        }
        var hud = FindAnyObjectByType<HudController>();
        hud.SetInventoryOpen(true);
        yield return new WaitForSecondsRealtime(0.5f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + "_bag.png"));
        yield return new WaitForSecondsRealtime(0.5f);
        hud.SetInventoryOpen(false);
        GameSession.Inventory.Bag.Clear();
    }

    // Gets into the bath (the hero is standing beside it) with a bottle of bubble bath, and
    // saves a picture.
    private IEnumerator TakeABath(string shot)
    {
        if (only != null && only != "House") yield break;
        GameSession.Inventory.Bag.Add("bubble_bath");
        foreach (var fixture in FindObjectsByType<HouseFixture>())
            if (fixture.Kind == HouseFixture.Effect.Bathe)
                fixture.Interact(LevelBootstrap.Current.Player);
        yield return new WaitForSecondsRealtime(1f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().StandUp();
    }

    // Turns on the bath's faucet (the hero is standing at its end) and saves a picture of the
    // water running, then turns it off again.
    private IEnumerator TurnOnTheFaucet(string shot)
    {
        if (only != null && only != "House") yield break;
        var player = LevelBootstrap.Current.Player;
        HouseFixture tap = null;
        foreach (var fixture in FindObjectsByType<HouseFixture>())
            if (fixture.Kind == HouseFixture.Effect.Faucet) tap = fixture;
        tap.Interact(player);
        yield return new WaitForSecondsRealtime(1f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
        tap.Interact(player);
    }

    // At the stove: opens the recipe card with only some of the ingredients (a picture), then
    // with all of them, cooks (another), and opens the bag with the pancakes in it (a third).
    private IEnumerator Cook(string shot)
    {
        if (only != null && only != "Kitchen") yield break;
        var player = LevelBootstrap.Current.Player;
        GameSession.Inventory.Bag.Add("egg");
        GameSession.Inventory.Bag.Add("flour");
        var stove = FindAnyObjectByType<CraftingStation>();
        stove.Interact(player);
        yield return new WaitForSecondsRealtime(0.5f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + "_missing.png"));
        yield return new WaitForSecondsRealtime(0.5f);
        CookingView.Instance.Close();
        GameSession.Inventory.Bag.Add("strawberry");
        stove.Interact(player);
        yield return new WaitForSecondsRealtime(0.3f);
        CookingView.Instance.Cook();
        yield return new WaitForSecondsRealtime(0.6f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + "_done.png"));
        yield return new WaitForSecondsRealtime(0.5f);
        CookingView.Instance.Close();
        var hud = FindAnyObjectByType<HudController>();
        hud.SetInventoryOpen(true);
        yield return new WaitForSecondsRealtime(0.5f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + "_bag.png"));
        yield return new WaitForSecondsRealtime(0.5f);
        hud.SetInventoryOpen(false);
        GameSession.Inventory.Bag.Clear();
    }

    // Opens the bag with most of the equipment worn: the helm picked up off the bedroom floor,
    // the rest put straight into the save data, and the ring left in the bag. Then empties it again.
    private IEnumerator InventoryShot(string shot)
    {
        if (only != null && only != "House") yield break;
        var player = LevelBootstrap.Current.Player;
        var inventory = player.GetComponent<Inventory>();
        GameSession.Inventory.Equipped[EquipSlot.Weapon] = "starlight_wand";
        GameSession.Inventory.Equipped[EquipSlot.Boots] = "trailblazer_boots";
        GameSession.Inventory.Equipped[EquipSlot.Armor] = "seashell_mail";
        GameSession.Inventory.Equipped[EquipSlot.Hat] = "frog_hat";
        GameSession.Inventory.Equipped[EquipSlot.Charm] = "clover_charm";
        GameSession.Inventory.Bag.Add("pancakes");
        GameSession.Inventory.KeyItems.Add("bouncy_boots");
        GameSession.Inventory.Bag.Add("ember_ring");
        foreach (var pickup in FindObjectsByType<ItemPickup>())
            if (pickup.Interact(player) != null) inventory.Equip(pickup.Item); // the helm (this also redraws the HUD)
        var hud = FindAnyObjectByType<HudController>();
        hud.SetInventoryOpen(true);
        yield return new WaitForSecondsRealtime(0.5f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
        // Hover the wand on the paper doll, then a bag item: the tooltip appears beside the pointer.
        var root = hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
        foreach (var name in new[] { "equip-weapon", "bag-0" })
        {
            var slot = root.Q(name);
            using (var ev = UnityEngine.UIElements.PointerEnterEvent.GetPooled()) { ev.target = slot; slot.SendEvent(ev); }
            using (var ev = UnityEngine.UIElements.PointerMoveEvent.GetPooled()) { ev.target = slot; slot.SendEvent(ev); }
            yield return new WaitForSecondsRealtime(0.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + "_tip_" + name + ".png"));
            yield return new WaitForSecondsRealtime(0.5f);
            using (var ev = UnityEngine.UIElements.PointerLeaveEvent.GetPooled()) { ev.target = slot; slot.SendEvent(ev); }
        }
        hud.SetInventoryOpen(false);
        GameSession.Inventory.KeyItems.Clear();
        GameSession.Inventory.Bag.Clear();
        GameSession.Inventory.Equipped.Clear();
    }

    // Walks the hero for a while (the camera following), saving a few frames on the way, to
    // check that sprites stay crisp and in order while everything moves.
    private IEnumerator Walk(string shot, Vector3 direction, int frames)
    {
        var controller = LevelBootstrap.Current.Player.GetComponent<CharacterController>();
        for (int i = 0; i < frames; i++)
        {
            for (float t = 0f; t < 0.37f; t += Time.unscaledDeltaTime)
            {
                controller.Move(direction.normalized * 3.1f * Time.unscaledDeltaTime);
                yield return null;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"{shot}_{i}.png"));
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.5f);
    }

    // Loads a scene (if needed), puts the hero next to the first tile with this map symbol,
    // sets her health and magic, waits for things to settle, and saves a screenshot.
    private IEnumerator Visit(string scene, char symbol, int dc, int dr, string shot, int max, int hearts, float manaFraction)
    {
        if (only != null && only != scene) yield break;
        if (SceneManager.GetActiveScene().name != scene)
        {
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
        }
        var player = LevelBootstrap.Current.Player;
        var health = player.GetComponent<Health>();
        health.InvulnerableUntil = float.MaxValue; // no monster spoils the picture

        var map = FindAnyObjectByType<LevelMap>();
        for (int row = 0; row < map.Height; row++)
            for (int col = 0; col < map.Width; col++)
                if (map.At(col, row) == symbol)
                {
                    var controller = player.GetComponent<CharacterController>();
                    controller.enabled = false;
                    player.transform.position = new Vector3((col + dc) * map.TileSize, 1f, (map.Height - 1 - row - dr) * map.TileSize);
                    controller.enabled = true;
                    FindAnyObjectByType<IsoCameraFollow>().SetTarget(player.transform);
                    row = map.Height; // stop at the first one
                    break;
                }

        if (max > 0)
        {
            health.SetMax(max);
            health.Revive();
            health.InvulnerableUntil = 0f;
            health.AdjustDamage = null;
            if (hearts < max) health.TakeDamage(max - hearts);
            health.InvulnerableUntil = float.MaxValue;
        }
        var mana = player.GetComponent<Mana>();
        mana.Refill();
        mana.TrySpend(mana.Max * (1f - manaFraction));

        yield return new WaitForSecondsRealtime(2f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
    }
}
