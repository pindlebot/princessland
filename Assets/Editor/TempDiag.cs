using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TempDiag
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level0.unity", OpenSceneMode.Single);
        var groups = new Dictionary<string, List<string>>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
            {
                string key = (m == null ? "NULL" : m.shader.name) + " | " + r.GetType().Name + (m != null ? " q=" + m.renderQueue : "");
                if (!groups.ContainsKey(key)) groups[key] = new List<string>();
                groups[key].Add(r.name);
            }
        foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r => r.transform.parent != null && (r.transform.parent.name.Contains("Tree") || r.transform.parent.name.Contains("Birch") || r.transform.parent.name.Contains("Grass") || r.transform.parent.name.Contains("Chest") || r.transform.parent.name.Contains("Rock"))).GroupBy(r => r.transform.parent.name.Split(' ')[0]).Select(g => g.First()))
            Debug.Log($"DIAG2 {r.transform.parent.name}/{r.name} sprite={(r.sprite ? r.sprite.name : "null")} tex={(r.sprite ? r.sprite.texture.name : "")} order={r.sortingOrder} layer={r.sortingLayerName} mat={r.sharedMaterial.name} zw={r.sharedMaterial.GetFloat("_ZWrite")} enabled={r.enabled} pos={r.transform.position} scale={r.transform.lossyScale} bounds={r.bounds.size}");
        foreach (var g in groups) Debug.Log($"DIAG {g.Key} x{g.Value.Count} e.g. {string.Join(",", g.Value.Distinct().Take(5))}");
    }
}
