using UnityEditor;
using UnityEngine;

// Hollow Farm (Levels/Farm.txt), the haunted farmland through the gate in the castle grounds'
// southern hedge: spooky, but friendly. Lit at dusk ("mood: dusk", SetUpLighting), with
// pumpkin patches on tilled soil ('/'), jack-o'-lanterns that glow, scarecrows, corn, a red
// barn (legend "building barn"), haystacks, crows, and a little graveyard where friendly ghosts
// and will-o'-the-wisps float about in the ground mist. Old Stitches, the scarecrow who talks,
// stands by the barn.
//
// Its legend props: scarecrow, pumpkin, jackolantern, corn, deadtree, haystack, gravestone,
// crow, ghost, wisp, mist, signpost; and "npc stitches".
// Off in its south-west corner, the autumn festival: Pippin's cider stand ("npc pippin", a
// Merchant selling hot apple cider), bunting, a festival arch, pumpkin stacks, a tub for bobbing
// for apples, a prize-winning giant pumpkin, and a corn maze ("cornwall": low walls of corn, so
// you can always see the hero) with the Jack-o'-Lantern Hat at its centre. Grass tufts (',') on a
// dusk level are fallen leaves instead. The gates between the castle grounds
// and the farm are legend "gate" entries (a door drawn as a farm gate).
// Art: Tools/make_farm_sprites.py; the barn's textures are in make_environment_textures.py.
public static partial class DungeonBuilder
{
    // Ordinary props at dusk: a little dimmer and cooler than by day (sprites aren't lit by the
    // scene's lights, so the dusk is painted on). Glowing things (jack-o'-lanterns, ghosts,
    // wisps) keep their full colour, so they shine out of the gloom.
    private static readonly Color Dusk = new Color(0.82f, 0.78f, 0.92f);

    private static Talk[] StitchesTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Old Stitches", Sets = "met:Old Stitches",
            Lines = new[]
            {
                N("Boo! ...Ha! Got you! Don't worry, I'm only a scarecrow. Old Stitches, they call me."),
                H("A talking scarecrow?!"),
                N("Welcome to Hollow Farm, {hero}! Some folk say it's haunted. They're right! But our ghosts are ever so friendly."),
                N("They float about the old graveyard at dusk, playing hide-and-seek in the mist. Say boo to them. They love it."),
                N("Just don't mind the crows. I'm supposed to scare them, but we're good friends really."),
            },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Why did the scarecrow win a prize?"), H("Why?"), N("Because he was OUTSTANDING in his field! Ha!") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Those pumpkins grew all by themselves. The ghosts carve the faces at night. Nobody knows how they hold the knives!") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("I'd offer you a hug, but I'm mostly straw. Very scratchy. Here, have a wave instead!") },
        },
        new Talk
        {
            SmallTalk = true, Requires = "met:Barnaby",
            Lines = new[] { N("Barnaby Badger came by for pumpkins once. He said they'd make lovely soup. I said they make lovely FACES.") },
        },
    };

    private static void CreateFarmPrefabs(SharedAssets assets, Sprite shadow)
    {
        var farm = SpriteSheetImporter.Import("FarmProps");
        var props = assets.PropPrefabs;

        props["scarecrow"] = DuskFixture(farm, "Scarecrow", new Vector3(1f, 2.4f, 0.6f), shadow, 1.6f,
            "Look at the scarecrow",
            "Its button eyes seem to follow you... | The crow on its arm caws at you. Caw! | " +
            "Straw pokes out of its sleeves. It doesn't look very scary, really.", "caw");
        props["pumpkin"] = Dusky(Scenic("Pumpkin", farm, "Pumpkin", new Vector3(1.2f, 0.7f, 1f), shadow, 1.4f));
        props["jackolantern"] = Scenic("JackOLantern", farm, "JackOLantern", new Vector3(1.1f, 0.8f, 1f), shadow, 1.2f);
        AddGlow(props["jackolantern"], new Vector3(0f, 0.6f, -0.3f), new Color(1f, 0.6f, 0.25f), 3.5f, 1.6f);
        props["corn"] = Dusky(Scenic("Corn", farm, "Corn", new Vector3(1.6f, 3f, 1.4f), shadow, 1.8f));
        props["deadtree"] = Dusky(Scenic("DeadTree", farm, "DeadTree", new Vector3(0.8f, 3f, 0.8f), shadow, 2.4f));
        props["haystack"] = Dusky(Scenic("Haystack", farm, "Haystack", new Vector3(2.2f, 1.4f, 1.6f), shadow, 2.8f));
        props["gravestone"] = DuskFixture(farm, "Gravestone", new Vector3(1.2f, 1.4f, 0.5f), shadow, 1.4f,
            "Read the gravestone",
            "Here lies Mr. Bones. He was a very good skeleton. | Here lies a turnip. It was delicious. | " +
            "\"Back in five minutes.\" -- The Ghost", null);
        props["crow"] = DuskFixture(farm, "Crow", new Vector3(0.6f, 0.6f, 0.6f), shadow, 0.8f,
            "Shoo the crow", "Caw! The crow hops away... then hops right back. | Caw caw! It's not scared of you one bit.", "caw");
        props["signpost"] = DuskFixture(farm, "Signpost", new Vector3(0.6f, 2f, 0.5f), shadow, 1f,
            "Read the sign",
            "HOLLOW FARM. Beware of... the friendliest ghosts in the kingdom! | " +
            "Pumpkins for sale: ask the scarecrow. (He never answers. Except the one by the barn.)", "paper");
        props["ghost"] = CreateGhost(farm);
        props["wisp"] = CreateWisp(farm);
        props["mist"] = CreateMist(farm);

        assets.FarmGate = CreateFarmGate(farm);

        // The autumn festival.
        props["ciderstand"] = Dusky(Scenic("CiderStand", farm, "CiderStand", new Vector3(3.4f, 1.4f, 1.2f), shadow, 3.6f));
        props["pumpkinstack"] = Scenic("PumpkinStack", farm, "PumpkinStack", new Vector3(2f, 1.2f, 1.2f), shadow, 2.2f);
        AddGlow(props["pumpkinstack"], new Vector3(0f, 1.4f, -0.3f), new Color(1f, 0.6f, 0.25f), 3f, 1.2f);
        props["bunting"] = Dusky(Scenic("Bunting", farm, "Bunting", null, null, 0f));
        props["festivalarch"] = Dusky(Scenic("FestivalArch", farm, "FestivalArch", null, shadow, 3.6f));
        props["bobbingtub"] = DuskFixture(farm, "BobbingTub", new Vector3(1.8f, 1f, 1.4f), shadow, 2f,
            "Bob for apples",
            "Splash! You dunk your face in... and come up with an apple in your teeth! | " +
            "Splosh! The apples bob away. So slippery! | Glug! You get a nose full of water. Achoo!", "splash");
        props["giantpumpkin"] = DuskFixture(farm, "GiantPumpkin", new Vector3(3f, 2f, 2f), shadow, 3.4f,
            "Admire the giant pumpkin",
            "FIRST PRIZE! It's as big as a pony. | Someone has drawn a little smiley face on it. | " +
            "You knock on it. Bonk! It's very hollow-sounding.", null);
        props["cornwall"] = CreateCornWall(assets.Materials);
        var decals = SpriteSheetImporter.Import("FarmDecals");
        var leaves = new GameObject("Leaves");
        leaves.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var leafSprite = leaves.AddComponent<SpriteRenderer>();
        leafSprite.sprite = decals.Frames("Leaves")[0];
        leafSprite.sortingOrder = -2;
        props["leaves"] = SavePrefab(leaves, "Leaves");

        assets.Pippin = CreateNpcPrefab("Pippin", "Pippin", "Pippin", "Assets/Art/UI/PortraitPippin.png",
            "voice_mermaid", PippinTalks(), new Vector3(1f, 2f, 0.6f), null, 0f, typeof(Merchant), npc =>
            {
                SetRef(npc, "ware", AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/AppleCider.asset"));
                SetInt(npc, "price", CiderPrice);
                SetString(npc, "thanks",
                    "One hot apple cider, extra cinnamon! Drink it up when you need a pick-me-up. | " +
                    "Careful, it's warm! Boo-tiful, isn't it? Hee hee! | " +
                    "Another cider? You've got good taste, {hero}!");
                SetString(npc, "tooPoor", "Oooh, it's {price} coins a mug. Come back when your pockets jingle!");
                SetString(npc, "bagFull", "Your bag's too full for a mug! Drink something first, maybe?");
                SetRef(npc, "saleSound", Sound("purchase"));
                SetVector3(npc, "talkOffset", new Vector3(0f, 0f, -1f));
            });
        assets.Stitches = CreateNpcPrefab("Stitches", "Stitches", "Old Stitches", "Assets/Art/UI/PortraitStitches.png",
            "voice_stitches", StitchesTalks(), new Vector3(1f, 2.4f, 0.6f), shadow, 1.4f);
    }

    public const int CiderPrice = 3;

    private static Talk[] PippinTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Pippin", Sets = "met:Pippin",
            Lines = new[]
            {
                N("Ooooh! A visitor! Welcome to the Autumn Festival! I'm Pippin."),
                H("You're a ghost... running a cider stand?"),
                N("Best hot apple cider in the whole kingdom! I stir it with my ghostly ladle. Three coins a mug."),
                N("Drink it whenever you're tired. It'll warm you right up! Oh, and have you tried the corn maze?"),
                N("They say there's a glowing hat at the very middle. Nobody's found it yet... nobody ALIVE, anyway! Hee hee!"),
            },
        },
    };

    // One wall tile of the corn maze: a block of corn, low like the dungeon's walls so the hero
    // is never hidden behind it, its stalks on the sides and leafy tassels on top.
    private static GameObject CreateCornWall(System.Collections.Generic.Dictionary<string, Material> mats)
    {
        var wall = Block("CornWall", null, Vector3.up * WallHeight * 0.5f, new Vector3(Tile, WallHeight, Tile), mats["CornSide"]);
        AddCap(wall, mats["CornTop"]);
        var root = new GameObject("CornWall");
        wall.name = "Corn";
        wall.transform.SetParent(root.transform, true);
        return SavePrefab(root, "CornWall");
    }

    private static GameObject Dusky(GameObject prefab)
    {
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
            if (sr.name != "Shadow") sr.color = Dusk;
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(prefab));
        PrefabUtility.UnloadPrefabContents(root);
        return prefab;
    }

    // A dusky prop with something to say when you press E (a HouseFixture with no effect).
    private static GameObject DuskFixture(SpriteSheetImporter.SpriteSheet sheet, string name, Vector3 size, Sprite shadow,
                                          float shadowSize, string prompt, string message, string sound)
    {
        var go = Scenic(name, sheet, name, size, shadow, shadowSize);
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(go));
        var fixture = root.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", prompt);
        SetString(fixture, "message", message);
        SetRef(fixture, "sound", sound != null ? Sound(sound) : null);
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(go));
        PrefabUtility.UnloadPrefabContents(root);
        return Dusky(go);
    }

    // A small coloured light inside a prop (a jack-o'-lantern's candle), flickering a little.
    private static void AddGlow(GameObject prefab, Vector3 at, Color color, float range, float intensity)
    {
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.transform.localPosition = at;
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity * LightBoost;
        light.shadows = LightShadows.None;
        var flicker = light.gameObject.AddComponent<FlickerLight>();
        SetFloat(flicker, "baseIntensity", intensity * LightBoost);
        SetFloat(flicker, "flickerAmount", intensity * 0.3f);
        SetFloat(flicker, "speed", 6f);
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(prefab));
        PrefabUtility.UnloadPrefabContents(root);
    }

    // A little sheet ghost floating in lazy loops over the graveyard; say boo to it.
    private static GameObject CreateGhost(SpriteSheetImporter.SpriteSheet sheet)
    {
        var go = new GameObject("Ghost");
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.transform.localPosition = new Vector3(0f, -1f, 0f); // AmbientWander lifts the whole ghost
        AddLoopingSprite(sprite, sheet, "Ghost");
        var wander = go.AddComponent<AmbientWander>();
        SetFloat(wander, "radius", 1.6f);
        SetFloat(wander, "speed", 0.3f);
        SetFloat(wander, "height", 1.3f);
        SetFloat(wander, "bob", 0.3f);
        var fixture = go.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", "Say boo to the ghost");
        SetString(fixture, "message",
            "Boo! ...Ooooooh! The little ghost giggles and twirls round you. | " +
            "Boo! The ghost turns pink and hides behind a gravestone. Then it peeks out and waves. | " +
            "Ooooo! The ghost says ooooo back. You're friends now.");
        SetRef(fixture, "sound", Sound("ghost_ooo"));
        return SavePrefab(go, "Ghost");
    }

    // A will-o'-the-wisp: a floating, pulsing glow with a pale green light of its own.
    private static GameObject CreateWisp(SpriteSheetImporter.SpriteSheet sheet)
    {
        var go = new GameObject("Wisp");
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.transform.localPosition = new Vector3(0f, -0.7f, 0f);
        AddLoopingSprite(sprite, sheet, "Wisp");
        var wander = go.AddComponent<AmbientWander>();
        SetFloat(wander, "radius", 1.2f);
        SetFloat(wander, "speed", 0.45f);
        SetFloat(wander, "height", 1f);
        SetFloat(wander, "bob", 0.2f);
        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.type = LightType.Point;
        light.color = new Color(0.55f, 1f, 0.8f);
        light.range = 3f;
        light.intensity = 1.2f * LightBoost;
        light.shadows = LightShadows.None;
        return SavePrefab(go, "Wisp");
    }

    // Low ground mist drifting slowly across the graveyard (see-through, no outline).
    private static GameObject CreateMist(SpriteSheetImporter.SpriteSheet sheet)
    {
        var go = new GameObject("Mist");
        AddStaticSprite(go, sheet.Frames("Mist")[0]);
        var drift = go.AddComponent<Drift>();
        SetVector3(drift, "velocity", new Vector3(0.15f, 0f, 0.05f));
        SetFloat(drift, "range", 5f);
        return SavePrefab(go, "Mist");
    }

    // The farm gate: a door between the castle grounds and the farm (where it leads comes from
    // the map's legend), solid so nobody walks off the edge of the island past it.
    private static GameObject CreateFarmGate(SpriteSheetImporter.SpriteSheet sheet)
    {
        var go = new GameObject("FarmGate");
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.5f, 0f);
        box.size = new Vector3(Tile, 3f, Tile);
        AddStaticSprite(go, sheet.Frames("FarmGate")[0]);
        var door = go.AddComponent<SceneDoor>();
        SetString(door, "prompt", "Open the farm gate");
        SetRef(door, "openSound", Sound("door_open"));
        return SavePrefab(go, "FarmGate");
    }

    // The barn: red boards three courses high under a dark shingle roof whose gable faces the
    // yard, big X-braced doors you can peek through, and a dark hayloft opening above them.
    private static void BuildBarn(Transform barn, Footprint f, System.Collections.Generic.Dictionary<string, Material> mats)
    {
        const int courses = 3;
        Walls(barn, f, courses, mats["BarnSide"]);
        GableRoof(barn, f, courses * BlockHeight, 3.4f, mats["BarnRoof"], mats["BarnSide"], ridgeAlongZ: true);
        var doors = Panel("Door", barn, new Vector3(f.Center.x, 1.2f, f.South), 2.4f, 2.4f, Vector3.back, mats["BarnDoor"]);
        var fixture = doors.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", "Peek into the barn");
        SetString(fixture, "message",
            "No cows... just a very surprised owl. Hoo! | Hay bales stacked to the rafters. It smells sweet in here. | " +
            "Something rustles in the hayloft... it's a mouse in a tiny witch's hat!");
        SetRef(fixture, "sound", Sound("door_open"));
        Panel("Hayloft", barn, new Vector3(f.Center.x, 4.4f, f.South), 1.2f, 1.2f, Vector3.back, mats["Belfry"]);
        Panel("Window", barn, new Vector3(f.West, 1.6f, f.Center.z), 1f, 1f, Vector3.left, mats["Belfry"]);
    }
}
