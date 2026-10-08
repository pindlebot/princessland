using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// A developer tool for checking the look of the game: launch the player with
//   IsoDungeon.app/Contents/MacOS/IsoDungeon -devcapture <folder> -screen-width 1280 -screen-height 720 -screen-fullscreen 0
// and it skips the title, visits a few spots in each level with different health values on
// the HUD, saves a screenshot of each into <folder>, and quits. Without -devcapture it does nothing.
public class DevCapture : MonoBehaviour
{
    private string folder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-devcapture");
        if (i < 0 || i + 1 >= args.Length) return;
        Application.runInBackground = true; // keep going even if the window isn't in front
        var go = new GameObject("DevCapture");
        DontDestroyOnLoad(go);
        go.AddComponent<DevCapture>().folder = args[i + 1];
    }

    private IEnumerator Start()
    {
        Directory.CreateDirectory(folder);
        yield return Menu("Title", "title");
        yield return Menu("CharacterSelect", "character_select");
        GameSession.Progress.AddGold(37);
        GameSession.Progress.AddXp(Progression.FirstLevelXp + 20); // level 2, with a skill point to spend

        yield return Visit("Level0", 'P', 0, 0, "level0_spawn", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", 'm', 1, 1, "level0_pond", max: 6, hearts: 3, manaFraction: 0.5f);
        yield return Visit("Level0", 'F', 0, 2, "level0_fountain", max: 9, hearts: 4, manaFraction: 0.2f);
        yield return Visit("Level0", 'f', -2, 0, "level0_edge", max: 12, hearts: 1, manaFraction: 0f);
        yield return Visit("Dungeon", 'P', 0, 0, "dungeon_spawn", max: 20, hearts: 7, manaFraction: 0.7f);
        yield return Visit("House", 'B', 1, 2, "house", max: 0, hearts: 0, manaFraction: 1f);
        yield return Visit("Level0", 'P', 0, -2, "walk_start", max: 0, hearts: 0, manaFraction: 1f);
        yield return Walk("walk", new Vector3(1f, 0f, 0.35f), 3);
        Application.Quit();
    }

    private IEnumerator Menu(string scene, string shot)
    {
        if (SceneManager.GetActiveScene().name != scene) SceneManager.LoadScene(scene);
        yield return new WaitForSecondsRealtime(1.5f);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, shot + ".png"));
        yield return new WaitForSecondsRealtime(0.5f);
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
