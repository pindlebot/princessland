using UnityEditor;
using UnityEngine;

// Storybook scenery for the castle grounds: a small, coherent set of props (trees, a fountain,
// bushes, banners), gentle ambient life (butterflies, magic motes, drifting clouds), the
// floating island's earthy edges, and the stairs under each level's exit.
//
// Solid props (tree trunks, the fountain) get colliders and are placed off the walking
// routes; soft ones (bushes, butterflies, motes, clouds) have no collider at all.
public static partial class DungeonBuilder
{
    private static void CreateSceneryPrefabs(SharedAssets assets, SpriteSheetImporter.SpriteSheet props, Sprite shadow)
    {
        var scenery = SpriteSheetImporter.Import("Scenery");
        assets.Tree = Scenic("Tree", scenery, "Tree", new Vector3(0.8f, 2f, 0.8f), shadow, 3f);
        assets.Fountain = Scenic("Fountain", scenery, "Fountain", new Vector3(2.6f, 1f, 2f), shadow, 3.6f);
        AddWakeSpot(assets.Fountain);
        assets.Bush = Scenic("Bush", scenery, "Bush", null, shadow, 1.8f);

        var butterfly = new GameObject("Butterfly");
        AddLoopingSprite(butterfly, props, "Butterfly");
        var wander = butterfly.AddComponent<AmbientWander>();
        SetFloat(wander, "radius", 1.6f);
        SetFloat(wander, "speed", 0.5f);
        assets.Butterfly = SavePrefab(butterfly, "Butterfly");

        var mote = new GameObject("Mote");
        AddLoopingSprite(mote, props, "Mote");
        var drift = mote.AddComponent<AmbientWander>();
        SetFloat(drift, "radius", 0.7f);
        SetFloat(drift, "speed", 0.35f);
        SetFloat(drift, "height", 0.5f);
        SetFloat(drift, "bob", 0.1f);
        SetFloat(drift, "rise", 1.8f);
        assets.Mote = SavePrefab(mote, "Mote");

        var cloud = new GameObject("Cloud");
        AddStaticSprite(cloud, scenery.Frames("Cloud")[0]);
        cloud.transform.localScale = Vector3.one * 2f;
        cloud.AddComponent<Drift>();
        assets.Cloud = SavePrefab(cloud, "Cloud");

        // Stairs lie flat on the floor under each exit's crystal.
        var stairs = new GameObject("Stairs").AddComponent<SpriteRenderer>();
        stairs.sprite = props.Frames("Stairs")[0];
        stairs.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        stairs.sortingOrder = -2;
        assets.Stairs = SavePrefab(stairs.gameObject, "Stairs");
    }

    // A camera-facing scenery sprite with a ground shadow, and a collider if it's solid.
    private static GameObject Scenic(string name, SpriteSheetImporter.SpriteSheet sheet, string anim,
                                     Vector3? solidSize, Sprite shadow, float shadowSize)
    {
        var go = new GameObject(name);
        if (solidSize.HasValue)
        {
            var box = go.AddComponent<BoxCollider>();
            box.size = solidSize.Value;
            box.center = new Vector3(0f, solidSize.Value.y / 2f, 0f);
        }
        if (sheet.Anim(anim).frames > 1)
        {
            var sprite = new GameObject("Sprite"); // gentle sway / splashing
            sprite.transform.SetParent(go.transform, false);
            AddLoopingSprite(sprite, sheet, anim);
        }
        else
        {
            AddStaticSprite(go, sheet.Frames(anim)[0]);
        }
        AddShadow(go, shadow, shadowSize);
        return SavePrefab(go, name);
    }

    // Gentle Mode wakes the hero by the last fountain she passed: a spot just in front of it.
    private static void AddWakeSpot(GameObject fountainPrefab)
    {
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(fountainPrefab));
        var spot = new GameObject("WakeSpot").transform;
        spot.SetParent(root.transform, false);
        spot.localPosition = new Vector3(0f, 0f, -2.2f); // toward the camera, clear of the basin
        spot.localRotation = Quaternion.Euler(0f, 180f, 0f);
        SetRef(root.AddComponent<WakeFountain>(), "wakeSpot", spot);
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(fountainPrefab));
        PrefabUtility.UnloadPrefabContents(root);
    }

    // Layered earth under the island's edge (the hedge tiles), so it reads as deliberate.
    private static void AddCliff(Transform parent, Vector3 tilePos, Material earth)
    {
        Block("Cliff", parent, tilePos + Vector3.down * 2.25f, new Vector3(Tile, 3.5f, Tile), earth);
    }

    // A few clouds well below the island, so they drift past underneath it on screen.
    private static void AddClouds(Transform parent, string[] map, GameObject cloud)
    {
        float w = map[0].Length * Tile, h = map.Length * Tile;
        var spots = new[]
        {
            new Vector3(-6f, -7f, h * 0.35f), new Vector3(w * 0.25f, -8f, -7f), new Vector3(w * 0.7f, -6f, -6f),
            new Vector3(w + 5f, -7f, h * 0.55f), new Vector3(-4f, -6f, h * 0.85f), new Vector3(w * 0.5f, -9f, -12f),
        };
        foreach (var spot in spots)
            Place(cloud, parent, spot);
    }

    // A few motes rising around the fountain: a small touch of magic at a point of interest.
    private static void AddMotes(Transform parent, Vector3 fountainPos, GameObject mote)
    {
        foreach (var offset in new[] { new Vector3(-1.2f, 0f, 0.4f), new Vector3(1.1f, 0f, -0.6f), new Vector3(0.2f, 0f, 1.2f) })
            Place(mote, parent, fountainPos + offset);
    }
}
