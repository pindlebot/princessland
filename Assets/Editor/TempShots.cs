using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// TEMPORARY (render pipeline migration): opens each scene, frames the hero's spawn, renders a PNG. Delete when done.
public static class TempShots
{
    private static readonly string[] Scenes = { "Level0", "Dungeon", "Woods3", "Mines4", "Lake4", "Frost4", "Cove", "Farm", "House", "Mines2" };

    public static void Capture()
    {
        string dir = "/tmp/shots";
        foreach (var arg in System.Environment.GetCommandLineArgs()) if (arg.StartsWith("-shotdir=")) dir = arg.Substring(9);
        Directory.CreateDirectory(dir);
        foreach (var scene in Scenes)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity", OpenSceneMode.Single);
            var cam = Camera.main;
            var boot = Object.FindFirstObjectByType<LevelBootstrap>();
            var spawn = (Transform)new SerializedObject(boot).FindProperty("spawnPoint").objectReferenceValue;
            cam.transform.position = spawn.position - cam.transform.forward * 40f;
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            foreach (var b in Object.FindObjectsByType<Billboard>(FindObjectsSortMode.None))
                typeof(Billboard).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(b, null);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, scene + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
        Debug.Log("[TempShots] done " + dir);
    }
}
