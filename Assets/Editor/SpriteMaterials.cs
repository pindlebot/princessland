using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Makes every sprite in the project use the game's own sprite material (Tidecrown/Sprite shader) instead of the
// render pipeline's default: URP's default sprite material only exists in the editor, and failed the depth test against
// the floor in this project's angled-camera 3D scenes. Run at the end of DungeonBuilder.BuildAll; safe to run again.
public static class SpriteMaterials
{
    public const string MaterialPath = "Assets/Resources/Materials/SpriteUnlit.mat";

    public static Material Asset()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var shader = Shader.Find("Tidecrown/Sprite");
        if (material == null)
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Materials");
            material = new Material(shader) { name = "SpriteUnlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = shader;
        material.SetFloat("_Flash", 0f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    [MenuItem("Dungeon/Apply Sprite Material")]
    public static void ApplyEverywhere()
    {
        var material = Asset();
        int prefabs = 0, scenes = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sharedMaterial != material) { sr.sharedMaterial = material; changed = true; }
            if (changed) { PrefabUtility.SaveAsPrefabAsset(root, path); prefabs++; }
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            bool changed = false;
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(sr)) continue;   // it follows its prefab
                if (sr.sharedMaterial != material) { sr.sharedMaterial = material; changed = true; }
            }
            if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); scenes++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[SpriteMaterials] sprite material applied: {prefabs} prefabs, {scenes} scenes");
    }
}
