using UnityEngine;

// The hero's home (the House scene, through the castle gate): bed, bathroom fixtures and
// the front door. Each fixture is a camera-facing sprite with a solid collider and a
// HouseFixture that says what pressing E on it does.
public static partial class DungeonBuilder
{
    private static void CreateHomePrefabs(SharedAssets assets, SpriteSheetImporter.SpriteSheet sheet, Sprite shadow)
    {
        assets.Bed = CreateFixture(sheet, "Bed", new Vector3(3f, 1f, 1.8f), shadow, 3.4f,
            "Take a nap", "You curl up for a quick nap. Health and mana restored!", HouseFixture.Effect.Rest, "rest");
        assets.Toilet = CreateFixture(sheet, "Toilet", new Vector3(1f, 1f, 1f), shadow, 1.2f,
            "Use the toilet", "*Flush!* Much better.", HouseFixture.Effect.None, "flush");
        assets.Sink = CreateFixture(sheet, "Sink", new Vector3(1.2f, 1f, 0.9f), shadow, 1.2f,
            "Wash your hands", "You wash your hands. Now they're all wet.", HouseFixture.Effect.WashHands, "water_run");
        assets.PaperTowel = CreateFixture(sheet, "PaperTowel", new Vector3(0.8f, 1f, 0.8f), shadow, 0.9f,
            "Grab a paper towel", "You dry your hands. Lovely and clean!", HouseFixture.Effect.DryHands, "paper");
        assets.HouseDoor = CreateHouseDoor(sheet);
    }

    private static GameObject CreateFixture(SpriteSheetImporter.SpriteSheet sheet, string name, Vector3 size,
                                            Sprite shadow, float shadowSize, string prompt, string message,
                                            HouseFixture.Effect effect, string sound)
    {
        var go = new GameObject(name);
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, size.y / 2f, 0f);
        box.size = size;
        AddStaticSprite(go, sheet.Frames(name)[0]);
        AddShadow(go, shadow, shadowSize);

        var fixture = go.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", prompt);
        SetString(fixture, "message", message);
        SetInt(fixture, "effect", (int)effect);
        SetRef(fixture, "sound", Sound(sound));
        return SavePrefab(go, name);
    }

    // The front door, back out to the castle grounds (arriving just outside the gate).
    private static GameObject CreateHouseDoor(SpriteSheetImporter.SpriteSheet sheet)
    {
        var go = new GameObject("HouseDoor");
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.2f, 0f);
        box.size = new Vector3(1.8f, 2.4f, 0.4f);
        AddStaticSprite(go, sheet.Frames("Door")[0]);

        var door = go.AddComponent<SceneDoor>();
        SetString(door, "prompt", "Go back outside");
        SetString(door, "targetScene", "Level0");
        SetString(door, "targetSpawn", "FromHouse");
        SetRef(door, "openSound", Sound("door_open"));
        return SavePrefab(go, "HouseDoor");
    }

    private static void AddStaticSprite(GameObject go, Sprite sprite)
    {
        var child = new GameObject("Sprite");
        child.transform.SetParent(go.transform, false);
        child.AddComponent<SpriteRenderer>().sprite = sprite;
        child.AddComponent<Billboard>();
    }
}
