using UnityEngine;

// The hero's home (the House scene, through the castle gate): the bedroom furniture, the
// bathroom fixtures and the front door. Each fixture is a camera-facing sprite with a solid
// collider and a HouseFixture that says what pressing E on it does. Messages separated by
// '|' take turns, one per use.
public static partial class DungeonBuilder
{
    private static void CreateHomePrefabs(SharedAssets assets, SpriteSheetImporter.SpriteSheet sheet, Sprite shadow)
    {
        assets.Bed = CreateFixture(sheet, "Bed", new Vector3(3f, 1f, 1.8f), shadow, 3.4f,
            "Take a nap", "You curl up for a quick nap. Health and mana restored!", HouseFixture.Effect.Rest, "rest");
        // The toilet sits you down; using it again flushes and stands you back up.
        assets.Toilet = CreateFixture(sheet, "Toilet", new Vector3(1f, 1f, 1f), shadow, 1.2f,
            "Sit on the toilet", "You sit down on the toilet. Ahh.", HouseFixture.Effect.Sit, null,
            toilet =>
            {
                SetString(toilet, "standMessage", "*Flush!* Much better. Now wash your hands!");
                SetRef(toilet, "standSound", Sound("flush"));
            });
        assets.Sink = CreateFixture(sheet, "Sink", new Vector3(1.2f, 1f, 0.9f), shadow, 1.2f,
            "Wash your hands", "You wash your hands. Now they're all wet.", HouseFixture.Effect.WashHands, "water_run");
        assets.PaperTowel = CreateFixture(sheet, "PaperTowel", new Vector3(0.8f, 1f, 0.8f), shadow, 0.9f,
            "Grab a paper towel", "You dry your hands. Lovely and clean!", HouseFixture.Effect.DryHands, "paper");
        assets.HouseDoor = CreateHouseDoor(sheet);

        // The bedroom.
        assets.Wardrobe = CreateFixture(sheet, "Wardrobe", new Vector3(1.6f, 2f, 1f), shadow, 1.8f,
            "Look in the wardrobe",
            "You try on a sparkly cape. Twirl! | You find your fluffy slippers. So cozy! | A moth flutters out. Hello, moth!",
            HouseFixture.Effect.None, "door_open");
        assets.Bookshelf = CreateFixture(sheet, "Bookshelf", new Vector3(1.6f, 2f, 1f), shadow, 1.8f,
            "Read a book",
            "You read a story about a brave little dragon. | You read a book all about frogs. Ribbit! | You look at the pictures in a book of castles.",
            HouseFixture.Effect.None, "paper");
        assets.ToyChest = CreateFixture(sheet, "ToyChest", new Vector3(1.6f, 1f, 1f), shadow, 1.8f,
            "Play with your toys",
            "Squeak! You play with your rubber duck. | You roll your ball across the floor. Wheee! | You give your teddy bear a big hug.",
            HouseFixture.Effect.None, "chest_open");
        assets.Plant = CreateFixture(sheet, "Plant", new Vector3(0.8f, 1f, 0.8f), shadow, 1.2f,
            "Water the plant", "You water the plant. It looks happy! | The plant is still nice and wet.",
            HouseFixture.Effect.None, "water_run");
        // The bedside lamp switches on and off (a small warm light).
        assets.Nightstand = CreateFixture(sheet, "Nightstand", new Vector3(1.2f, 1f, 0.8f), shadow, 1.2f,
            "Turn the lamp off", "", HouseFixture.Effect.Lamp, "plink", AddLamp);
        assets.Rug = CreateRug();
    }

    private static void AddLamp(HouseFixture nightstand)
    {
        var light = new GameObject("LampLight").AddComponent<Light>();
        light.transform.SetParent(nightstand.transform, false);
        light.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.7f, 0.6f); // through a pink shade
        light.range = 5f;
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;
        SetRef(nightstand, "lamp", light);
    }

    // The rug lies flat on the floor in front of the bed: nothing to bump into or use.
    private static GameObject CreateRug()
    {
        var sheet = SpriteSheetImporter.Import("Rug");
        var go = new GameObject("Rug");
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sheet.Frames("Rug")[0];
        sr.sortingOrder = -2; // under the furniture's shadows
        return SavePrefab(go, "Rug");
    }

    private static GameObject CreateFixture(SpriteSheetImporter.SpriteSheet sheet, string name, Vector3 size,
                                            Sprite shadow, float shadowSize, string prompt, string message,
                                            HouseFixture.Effect effect, string sound,
                                            System.Action<HouseFixture> configure = null)
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
        SetRef(fixture, "sound", sound != null ? Sound(sound) : null);
        configure?.Invoke(fixture);
        return SavePrefab(go, name);
    }

    // The front door. Where it leads is set per door from the map's legend (BuildLevel).
    private static GameObject CreateHouseDoor(SpriteSheetImporter.SpriteSheet sheet)
    {
        var go = new GameObject("HouseDoor");
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.2f, 0f);
        box.size = new Vector3(1.8f, 2.4f, 0.4f);
        AddStaticSprite(go, sheet.Frames("Door")[0]);

        var door = go.AddComponent<SceneDoor>();
        SetString(door, "prompt", "Go back outside");
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
