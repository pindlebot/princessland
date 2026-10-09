using UnityEditor;
using UnityEngine;

// The hero's home (the House scene, through the castle gate): the bedroom furniture, the
// bathroom fixtures (soap at the sink; the big marble bath and the towel shelf are in DungeonBuilder.Bath.cs), the courtyard's cat and locked door,
// the spiral staircase down to the kitchen (the Kitchen scene) and its stove, pantry and island,
// and the front door. Each fixture is a camera-facing sprite with a solid
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
        // Soap first, then rinse (see HouseFixture).
        assets.Sink = CreateFixture(sheet, "Sink", new Vector3(1.2f, 1f, 0.9f), shadow, 1.2f,
            "Rinse your hands", "You rinse off the bubbles. Now your hands are all wet.", HouseFixture.Effect.WashHands, "water_run",
            sink =>
            {
                SetString(sink, "soapPrompt", "Pump the soap");
                SetString(sink, "soapMessage", "Squish! Lavender soap. Rub-a-dub-dub, all the way round your fingers. Now rinse!");
                SetRef(sink, "soapSound", Sound("soap"));
            });
        CreateBathPrefabs(assets, sheet, shadow);
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
        assets.PropPrefabs["cat"] = CreateCat(shadow);
        assets.PropPrefabs["lockeddoor"] = CreateLockedDoor();

        // The spiral staircase between the bedroom and the kitchen: doors in all but looks.
        assets.SpiralDown = CreateStairs(sheet, "SpiralDown", "Go down the spiral stairs", new Vector3(2.8f, 1f, 1.6f));
        assets.SpiralUp = CreateStairs(sheet, "SpiralUp", "Go up the spiral stairs", new Vector3(2.4f, 3f, 1.6f));

        // The kitchen: the stove cooks (a recipe card), and the pantry and the island's fruit
        // bowl hand out two of the ingredients (DungeonBuilder.Cooking.cs).
        assets.PropPrefabs["stove"] = CreateStove(sheet, shadow, CreateRecipes());
        assets.PropPrefabs["pantry"] = CreateFixture(sheet, "Pantry", new Vector3(2.2f, 3.4f, 1f), shadow, 2.4f,
            "Get some flour",
            "You scoop flour into a little sack. A mouse peeks out from behind the honey and squeaks hello! | " +
            "You fill a little sack with flour... and find a cookie behind the oats. Crunch! | " +
            "Past the strawberry jam and the big wheel of cheese: a sack of flour. You scoop some out. Achoo!",
            HouseFixture.Effect.Gather, "door_open",
            pantry => Gather(pantry, "Flour", "You've got some flour already. Off to the stove!"));
        assets.PropPrefabs["island"] = CreateFixture(sheet, "Island", new Vector3(3.6f, 1.2f, 1.4f), shadow, 3.8f,
            "Pick a strawberry",
            "You pick the reddest strawberry in the fruit bowl. | " +
            "You pick a strawberry. It's so juicy it nearly drips! | " +
            "One strawberry for the pancakes... and none for you. Well, maybe a tiny nibble.",
            HouseFixture.Effect.Gather, "pickup",
            island => Gather(island, "Strawberry", "You've got a strawberry already. No eating it before the pancakes!"));
    }

    // A spiral staircase: a SceneDoor whose target comes from the map's legend (BuildLevel).
    private static GameObject CreateStairs(SpriteSheetImporter.SpriteSheet sheet, string name, string prompt, Vector3 size)
    {
        var go = new GameObject(name);
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, size.y / 2f, 0f);
        box.size = size;
        AddStaticSprite(go, sheet.Frames(name)[0]);
        var door = go.AddComponent<SceneDoor>();
        SetString(door, "prompt", prompt);
        SetRef(door, "openSound", Sound("door_open"));
        return SavePrefab(go, name);
    }

    // The old door at the far end of the courtyard: barred and padlocked, and there's no key for
    // it anywhere (yet). Just a HouseFixture with something to say.
    private static GameObject CreateLockedDoor()
    {
        var props = SpriteSheetImporter.Import("DungeonProps");
        var go = new GameObject("CourtyardDoor"); // not "LockedDoor": that's the dungeon's (which a key opens)
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.2f, 0f);
        box.size = new Vector3(Tile, 2.4f, Tile);
        AddStaticSprite(go, props.Frames("Door_Locked")[0]);
        var fixture = go.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", "Try the old door");
        SetString(fixture, "message",
            "It's locked tight. The padlock is big and rusty, and none of your keys fit. | " +
            "You knock. Nobody answers... but you hear a faint jingle on the other side. | " +
            "You peek through the bars. It's much too dark to see. What could be in there?");
        SetRef(fixture, "sound", Sound("door_locked"));
        return SavePrefab(go, "CourtyardDoor");
    }

    // Whiskers, the tabby cat who lounges in the courtyard: a looping sprite (her tail swishes)
    // and a HouseFixture to pet her.
    private static GameObject CreateCat(Sprite shadow)
    {
        var sheet = SpriteSheetImporter.Import("Cat");
        var go = new GameObject("Cat");
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.4f, 0f);
        box.size = new Vector3(0.8f, 0.8f, 0.6f);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, sheet, "Idle");
        AddShadow(go, shadow, 1f);

        var fixture = go.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", "Pet Whiskers");
        SetString(fixture, "message",
            "Purrrrr... Whiskers leans into your hand. | Mrrow! Whiskers rolls over for a tummy rub. | " +
            "Whiskers blinks slowly at you. That's a cat kiss! | Whiskers sniffs your fingers. They smell of soap.");
        SetRef(fixture, "sound", Sound("meow"));
        return SavePrefab(go, "Cat");
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
        var go = CreateFurniture(sheet, name, size, shadow, shadowSize);
        var fixture = go.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", prompt);
        SetString(fixture, "message", message);
        SetInt(fixture, "effect", (int)effect);
        SetRef(fixture, "sound", sound != null ? Sound(sound) : null);
        configure?.Invoke(fixture);
        return SavePrefab(go, name);
    }

    // A piece of furniture with nothing to do yet: a solid box, its sprite and a shadow.
    private static GameObject CreateFurniture(SpriteSheetImporter.SpriteSheet sheet, string name, Vector3 size,
                                              Sprite shadow, float shadowSize)
    {
        var go = new GameObject(name);
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, size.y / 2f, 0f);
        box.size = size;
        AddStaticSprite(go, sheet.Frames(name)[0]);
        AddShadow(go, shadow, shadowSize);
        return go;
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
