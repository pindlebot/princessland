using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// TEMPORARY: renders the main camera to PNGs so the new art can be looked at. Deleted after use.
public class ZzVisualCheck
{
    private const string Out = "/private/tmp/claude-501/-Users-ben/c3aa729d-2fa8-4e88-9fab-00d1bc1ac7de/scratchpad/shots/";

    private static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var cam = Camera.main;
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        File.WriteAllBytes(Out + name + ".png", tex.EncodeToPNG());
    }

    private static void Teleport(GameObject player, Vector3 to)
    {
        var cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = to;
        cc.enabled = true;
    }

    private static IEnumerator Settle()
    {
        for (float t = 0f; t < 1.2f; t += Time.deltaTime) yield return null;
    }

    private static IEnumerator Open(string scene)
    {
        GameSession.NewGame(null);
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
    }

    [UnityTest]
    public IEnumerator House()
    {
        yield return Open("House");
        var player = LevelBootstrap.Current.Player;
        var bed = Object.FindObjectsByType<HouseFixture>().First(f => f.Kind == HouseFixture.Effect.Rest);
        Teleport(player, bed.transform.position + new Vector3(0f, 1f, -2.5f));
        yield return Settle();
        yield return Shot("house_bed_before");
        bed.Interact(player);
        yield return Settle();
        yield return Shot("house_bed_asleep");
        // the second frame of the breathing animation
        yield return new WaitForSeconds(0.5f);
        yield return Shot("house_bed_asleep2");
        bed.Interact(player);
        var crib = Object.FindObjectsByType<HouseFixture>().Single(f => f.name == "Crib");
        Teleport(player, crib.transform.position + new Vector3(0f, 1f, -2.2f));
        yield return Settle();
        yield return Shot("house_crib");
    }

    [UnityTest]
    public IEnumerator Farm()
    {
        yield return Open("Farm");
        var player = LevelBootstrap.Current.Player;
        var king = Object.FindAnyObjectByType<BossAbilities>();
        Teleport(player, king.transform.position + new Vector3(-5f, 0f, -3f));
        yield return Settle();
        yield return Shot("farm_king");
        var g = Object.FindObjectsByType<EnemyAI>().Where(e => e.name.StartsWith("Gourdling")).OrderBy(e => e.transform.position.x).First();
        Teleport(player, g.transform.position + new Vector3(3f, 0f, -3f));
        yield return Settle();
        yield return Shot("farm_gourdlings");
        var s = Object.FindObjectsByType<EnemyAI>().First(e => e.name.StartsWith("Strawman"));
        Teleport(player, s.transform.position + new Vector3(3f, 0f, -3f));
        yield return Settle();
        yield return Shot("farm_strawman");
        // the King's volley
        GameSession.Settings.gentle = false;
        Teleport(player, king.transform.position + new Vector3(-6f, 0f, -1f));
        yield return Settle();
        king.StartVolley();
        for (float t = 0f; t < 1.6f; t += Time.deltaTime) yield return null;
        yield return Shot("farm_volley");
    }

    [UnityTest]
    public IEnumerator Cove()
    {
        yield return Open("Cove");
        var player = LevelBootstrap.Current.Player;
        var captain = Object.FindAnyObjectByType<BossAbilities>();
        Teleport(player, captain.transform.position + new Vector3(-5f, 0f, -2f));
        yield return Settle();
        yield return Shot("cove_captain");
        GameSession.Settings.gentle = false;
        captain.StartBarrage();
        for (float t = 0f; t < 1.0f; t += Time.deltaTime) yield return null;
        yield return Shot("cove_barrage");
    }
}
