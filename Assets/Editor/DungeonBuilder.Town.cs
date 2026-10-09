using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Hollyhock, the little village east of the castle on Level 0 (a nod to Stardew Valley's
// Pelican Town): a picket fence ('+') around cobbled streets ('-'), three buildings, Barnaby
// Badger selling bubble bath at his stall, and props from the map's legend:
//
//   <symbol> = building cathedral|shop|cottage   a rectangle of that symbol (BuildBuildings)
//   <symbol> = npc barnaby                       the shopkeeper (a Merchant)
//   <symbol> = prop <kind>                       see TownPropKinds below
//
// Buildings are stacked 2 x 1.2 x 2 blocks like the castle, so wall textures are never
// stretched, under a gable roof made as a mesh whose UVs are measured in metres. Windows, doors,
// the awning and the sign are thin blocks standing just proud of a wall. Each front door is a
// HouseFixture with something to say. Art: Tools/make_town_sprites.py and the village textures
// in make_environment_textures.py.
public static partial class DungeonBuilder
{
    // ---------- Barnaby Badger ----------

    public const int BubbleBathPrice = 10;

    private static Talk[] BarnabyTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Barnaby", Sets = "met:Barnaby",
            Lines = new[]
            {
                N("Well, hello there! Welcome to Hollyhock! I'm Barnaby Badger, and this is my little stall."),
                H("What are you selling?"),
                N("Only the finest bubble bath in the whole kingdom! Lavender and honey. One bottle makes a mountain of bubbles."),
                H("What does it do?"),
                N("Pour a bottle into a warm bath and you'll see! Bubbles up to your nose! Ten coins a bottle, whenever you like, {hero}."),
            },
        },
    };

    private const string BarnabyThanks =
        "A splendid choice! Lavender and honey, with a bow on top. Enjoy, {hero}! | " +
        "Another one? You must be the cleanest hero in the land! | " +
        "Bubbles for everybody! Thank you kindly, {hero}. | " +
        "Ho ho! At this rate you'll need a bigger bathtub!";

    // ---------- Prefabs ----------

    private static void CreateTownPrefabs(SharedAssets assets, Sprite shadow, GameObject sparkle)
    {
        var props = SpriteSheetImporter.Import("TownProps");
        var chickens = SpriteSheetImporter.Import("Chicken");
        var decals = SpriteSheetImporter.Import("TownDecals");
        var town = assets.PropPrefabs;

        town["stall"] = Scenic("Stall", props, "Stall", new Vector3(3.4f, 1.2f, 1.2f), shadow, 3.6f);
        // The hens lay eggs for cooking: check the nest box (DungeonBuilder.Cooking.cs).
        town["coop"] = CreateFixture(props, "Coop", new Vector3(2.2f, 1.6f, 1.6f), shadow, 2.6f,
            "Look in the nest box",
            "You find a warm brown egg in the straw. Thank you, hens! | " +
            "A hen clucks proudly at you. She's laid another egg! | " +
            "Tucked in the straw: one perfect egg. You carry it very, very carefully.",
            HouseFixture.Effect.Gather, "cluck",
            coop => Gather(coop, "Egg", "You've already got an egg. Let the hens have a rest!"));
        town["grainsack"] = Scenic("GrainSack", props, "GrainSack", new Vector3(1f, 0.8f, 1f), shadow, 1.2f);
        town["planter"] = Scenic("Planter", props, "Planter", new Vector3(1.3f, 0.8f, 1.3f), shadow, 1.4f);
        town["lamppost"] = Scenic("Lamppost", props, "Lamppost", new Vector3(0.4f, 3f, 0.4f), shadow, 0.8f);
        AddLanternLight(town["lamppost"]);
        town["well"] = CreateFixture(props, "Well", new Vector3(2f, 1.2f, 1.6f), shadow, 2.4f,
            "Look down the well",
            "You call down the well: \"Hello!\" ... \"Hello... hello... hello!\" | " +
            "You drop in a pebble. ...Plip! | A frog at the bottom says \"Ribbit.\" It's not Sir Hopsalot. | " +
            "Your reflection waves back at you.",
            HouseFixture.Effect.None, "plink");
        // The castle courtyard's wishing well: the village well's art, with the fountain's wishing
        // (its own count, so its third wish comes true separately).
        town["wishingwell"] = Scenic("WishingWell", props, "Well", new Vector3(2f, 1.2f, 1.6f), shadow, 2.4f);
        AddWishing(town["wishingwell"], sparkle, thing: "well", counter: "wishes:well", grantedFlag: "wish:well");
        town["noticeboard"] = CreateFixture(props, "NoticeBoard", new Vector3(2.2f, 2f, 0.5f), shadow, 2f,
            "Read the notice board",
            "LOST: one frog. Small, green, wears a tiny crown. Please tell Coralie at the pond! | " +
            "BARNABY'S BITS & BOBS: Bubble bath now in stock! Only 10 coins! | " +
            "Choir practice at the cathedral on Sundays. Frogs and dragons welcome. | " +
            "WANTED: someone brave to chase the skeletons off the castle grounds. Reward: everybody's thanks!",
            HouseFixture.Effect.None, "paper");
        town["bench"] = CreateFixture(props, "Bench", new Vector3(2f, 0.7f, 0.7f), shadow, 2.2f,
            "Sit on the bench", "You sit down for a rest. What a lovely day in Hollyhock!", HouseFixture.Effect.Sit, null,
            bench =>
            {
                SetString(bench, "standPrompt", "Stand up");
                SetString(bench, "standMessage", "");
                SetFloat(bench, "seatHeight", 0.45f);
            });

        town["hen"] = CreateChicken("Hen", chickens, "Walk", "Peck", "Pet the hen",
            "Bawk! | Cluck cluck! | The hen fluffs up her feathers. | She pecks at your shoe. Rude!", shadow);
        town["brownhen"] = CreateChicken("BrownHen", chickens, "WalkBrown", "PeckBrown", "Pet the hen",
            "Buk buk BAWK! | She lays... oh. No egg today. | Cluck! She likes you.", shadow);
        town["chick"] = CreateChicken("Chick", chickens, "Chick", "Chick", "Pet the chick",
            "Cheep! | Cheep cheep! | So fluffy!", shadow, radius: 0.8f, speed: 1.2f);

        // Corn scattered on the ground: lies flat, under everything else.
        var grain = new GameObject("Grain");
        grain.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var sr = grain.AddComponent<SpriteRenderer>();
        sr.sprite = decals.Frames("Grain")[0];
        sr.sortingOrder = -2;
        town["grain"] = SavePrefab(grain, "Grain");

        assets.Barnaby = CreateNpcPrefab("Barnaby", "Barnaby", "Barnaby", "Assets/Art/UI/PortraitBarnaby.png",
            "voice_badger", BarnabyTalks(), new Vector3(1f, 2f, 0.8f), shadow, 1.6f, typeof(Merchant), npc =>
            {
                SetRef(npc, "ware", AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/BubbleBath.asset"));
                SetInt(npc, "price", BubbleBathPrice);
                SetString(npc, "thanks", BarnabyThanks);
                SetString(npc, "tooPoor",
                    "Ah, that's {price} coins, I'm afraid. Monsters drop lots of coins. Come back when your purse jingles!");
                SetString(npc, "bagFull", "Goodness, your bag is full to bursting! Make a little room and come back.");
                SetRef(npc, "saleSound", Sound("purchase"));
                SetVector3(npc, "talkOffset", new Vector3(0f, 0f, -1f)); // talk to him from the street
            });
    }

    // A hen or a chick: a Chicken that wanders and pecks, and a HouseFixture to pet her.
    private static GameObject CreateChicken(string name, SpriteSheetImporter.SpriteSheet sheet, string walk, string peck,
                                            string prompt, string message, Sprite shadow, float radius = 1.1f, float speed = 0.9f)
    {
        var go = new GameObject(name);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.AddComponent<SpriteRenderer>();
        var flipbook = sprite.AddComponent<SpriteFlipbook>();
        SetFlipbook(flipbook, sheet, walk, destroyWhenDone: false);
        sprite.AddComponent<Billboard>();
        AddShadow(go, shadow, name == "Chick" ? 0.6f : 1f);

        var chicken = go.AddComponent<Chicken>();
        SetRef(chicken, "flipbook", flipbook);
        SetRefs(chicken, "walkFrames", sheet.Frames(walk));
        SetRefs(chicken, "peckFrames", sheet.Frames(peck));
        SetFloat(chicken, "fps", sheet.Anim(walk).fps);
        SetFloat(chicken, "radius", radius);
        SetFloat(chicken, "speed", speed);

        var fixture = go.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", prompt);
        SetString(fixture, "message", message);
        SetRef(fixture, "sound", Sound("cluck"));
        return SavePrefab(go, name);
    }

    // A small warm glow in the lamppost's lantern (no shadows: just a pool of light).
    private static void AddLanternLight(GameObject lamppostPrefab)
    {
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(lamppostPrefab));
        var light = new GameObject("Lantern").AddComponent<Light>();
        light.transform.SetParent(root.transform, false);
        light.transform.localPosition = new Vector3(0f, 3.1f, -0.3f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.55f);
        light.range = 3.5f;
        light.intensity = 0.8f;
        light.shadows = LightShadows.None;
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(lamppostPrefab));
        PrefabUtility.UnloadPrefabContents(root);
    }

    // A "prop" or "npc" from the map's legend, standing on its tile.
    private static void PlaceProp(MapFile.LegendEntry entry, SharedAssets assets, Transform parent, Vector3 pos,
                                  System.Random rng)
    {
        string kind = entry.Args[0];
        if (entry.Kind == "npc")
        {
            Place(kind == "stitches" ? assets.Stitches : kind == "pippin" ? assets.Pippin : assets.Barnaby, parent, pos);
            return;
        }
        if (kind == "grain")
        {
            Place(assets.PropPrefabs[kind], parent, pos + new Vector3(rng.Next(-3, 4) * 0.1f, 0.01f, rng.Next(-3, 4) * 0.1f));
            return;
        }
        // The kitchen island is long: it reaches a little into the tile east of its own. (The
        // marble bath is three tiles wide, centred on its own tile: one either side.)
        Place(assets.PropPrefabs[kind], parent, kind is "island" ? pos + Vector3.right * 0.6f : pos);
    }

    // ---------- The picket fence ('+') ----------

    private const float FenceHeight = 0.8f;

    // One fence tile: a post in the middle, and rails and pickets reaching toward each
    // neighbouring fence tile (so corners, ends and gate posts all come out right). It's solid:
    // one invisible box covers the tile, like a wall.
    private static void BuildFence(Transform parent, string[] map, int col, int row, Vector3 pos, Material wood)
    {
        var fence = new GameObject("Fence");
        fence.transform.SetParent(parent);
        fence.transform.position = pos;
        fence.isStatic = true;
        var box = fence.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.75f, 0f);
        box.size = new Vector3(Tile, 1.5f, Tile);

        FencePiece(fence.transform, pos + Vector3.up * 0.5f, new Vector3(0.24f, 1f, 0.24f), wood);         // the post
        FencePiece(fence.transform, pos + Vector3.up * 1.02f, new Vector3(0.3f, 0.06f, 0.3f), wood);        // its cap
        foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            if (!LevelMap.IsFence(MapAt(map, col + dc, row + dr))) continue;
            var along = new Vector3(dc, 0f, -dr); // map rows go down the screen, world z goes up
            var across = new Vector3(Mathf.Abs(along.z), 0f, Mathf.Abs(along.x));
            foreach (float y in new[] { 0.3f, 0.62f }) // two rails, half a tile long
                FencePiece(fence.transform, pos + along * (Tile / 4f) + Vector3.up * y,
                           new Vector3(Mathf.Abs(along.x), 0f, Mathf.Abs(along.z)) * (Tile / 2f) + across * 0.06f + Vector3.up * 0.08f, wood);
            foreach (float d in new[] { 0.4f, 0.8f }) // two pickets on each half
                FencePiece(fence.transform, pos + along * d + Vector3.up * (FenceHeight / 2f),
                           new Vector3(Mathf.Abs(along.x), 0f, Mathf.Abs(along.z)) * 0.16f + across * 0.08f + Vector3.up * FenceHeight, wood);
        }
    }

    private static void FencePiece(Transform parent, Vector3 pos, Vector3 size, Material wood)
    {
        var piece = Block("Picket", parent, pos, size, wood);
        Object.DestroyImmediate(piece.GetComponent<BoxCollider>()); // the tile's box does the blocking
    }

    // ---------- Buildings ----------

    // The extent of a building on the map, and where that is in the world.
    private struct Footprint
    {
        public int MinCol, MaxCol, MinRow, MaxRow;
        public float West, East, South, North; // world edges
        public float Width => East - West;
        public float Depth => North - South;
        public Vector3 Center => new Vector3((West + East) / 2f, 0f, (South + North) / 2f);
    }

    private static Footprint FootprintOf(MapFile file, char symbol)
    {
        var tiles = file.Find(symbol).ToList();
        var f = new Footprint
        {
            MinCol = tiles.Min(t => t.col), MaxCol = tiles.Max(t => t.col),
            MinRow = tiles.Min(t => t.row), MaxRow = tiles.Max(t => t.row),
        };
        int height = file.Rows.Length;
        f.West = f.MinCol * Tile - Tile / 2f;
        f.East = f.MaxCol * Tile + Tile / 2f;
        f.South = (height - 1 - f.MaxRow) * Tile - Tile / 2f;
        f.North = (height - 1 - f.MinRow) * Tile + Tile / 2f;
        return f;
    }

    // Builds every building in the map's legend.
    private static void BuildBuildings(MapFile file, Transform parent, SharedAssets assets)
    {
        foreach (var entry in file.Legend.Values.Where(e => e.Kind == "building"))
        {
            var f = FootprintOf(file, entry.Symbol);
            string kind = entry.Args[0];
            var building = new GameObject(char.ToUpperInvariant(kind[0]) + kind.Substring(1)).transform;
            building.SetParent(parent);
            switch (kind)
            {
                case "cathedral": BuildCathedral(building, f, assets); break;
                case "shop": BuildShop(building, f, assets.Materials); break;
                case "cottage": BuildCottage(building, f, assets.Materials); break;
                case "barn": BuildBarn(building, f, assets.Materials); break;
            }
        }
    }

    // Every tile of the footprint gets a stack of wall blocks (solid, like the castle).
    private static void Walls(Transform parent, Footprint f, int courses, Material side)
    {
        for (float x = f.West + Tile / 2f; x < f.East; x += Tile)
            for (float z = f.South + Tile / 2f; z < f.North; z += Tile)
                for (int k = 0; k < courses; k++)
                    Block("Wall", parent, new Vector3(x, BlockHeight * (k + 0.5f), z), new Vector3(Tile, BlockHeight, Tile), side);
    }

    // A thin panel just proud of a wall (a window, a door, a sign). outward is Vector3.back
    // for the south face (the one facing the camera) or Vector3.left for the west face.
    private static GameObject Panel(string name, Transform parent, Vector3 center, float width, float height,
                                    Vector3 outward, Material mat)
    {
        var size = outward.x != 0f ? new Vector3(0.08f, height, width) : new Vector3(width, height, 0.08f);
        var panel = Block(name, parent, center + outward * 0.04f, size, mat);
        Object.DestroyImmediate(panel.GetComponent<BoxCollider>());
        return panel;
    }

    // A front door that says something when you knock (a HouseFixture with no special effect).
    private static void FrontDoor(Transform parent, Vector3 center, Material mat, string prompt, string message, string sound,
                                  float width = 1f)
    {
        var door = Panel("Door", parent, center, width, 2f, Vector3.back, mat);
        var fixture = door.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", prompt);
        SetString(fixture, "message", message);
        SetRef(fixture, "sound", Sound(sound));
    }

    private static void BuildShop(Transform shop, Footprint f, Dictionary<string, Material> mats)
    {
        const int courses = 3;
        Walls(shop, f, courses, mats["Plaster"]);
        GableRoof(shop, f, courses * BlockHeight, 2.2f, mats["RoofTiles"], mats["Plaster"], ridgeAlongZ: false);

        // The door on the west end of the front, windows along the rest under a striped awning,
        // and Barnaby's sign in the middle above it all.
        float front = f.South;
        FrontDoor(shop, new Vector3(f.West + Tile / 2f, 1f, front), mats["TownDoor"], "Knock on the shop door",
            "A note on the door: \"Back soon! At my stall, just out front. -- Barnaby\" | The shop smells of soap and cinnamon.",
            "door_open");
        for (float x = f.West + Tile * 1.5f; x < f.East; x += Tile)
        {
            Panel("Window", shop, new Vector3(x, 1.2f, front), 1f, 1f, Vector3.back, mats["Window"]);
            var awning = Panel("Awning", shop, new Vector3(x, 2.45f, front - 0.47f), Tile, 1f, Vector3.back, mats["Awning"]);
            awning.transform.rotation = Quaternion.Euler(60f, 0f, 0f); // its top edge against the wall, sloping down and out
        }
        Panel("Sign", shop, new Vector3(f.Center.x, 3.05f, front), 1.5f, 1f, Vector3.back, mats["ShopSign"]);
        Panel("Window", shop, new Vector3(f.West, 1.2f, f.Center.z), 1f, 1f, Vector3.left, mats["Window"]);
    }

    private static void BuildCottage(Transform cottage, Footprint f, Dictionary<string, Material> mats)
    {
        const int courses = 2;
        Walls(cottage, f, courses, mats["Plaster"]);
        GableRoof(cottage, f, courses * BlockHeight, 2.4f, mats["Thatch"], mats["Plaster"], ridgeAlongZ: false);

        FrontDoor(cottage, new Vector3(f.Center.x, 1f, f.South), mats["TownDoor"], "Knock on the door",
            "Knock knock! Nobody's home. A note says: \"Gone to the pond to visit Coralie.\" | " +
            "You hear a cat purring inside. | Knock knock! Still nobody home.", "door_open");
        for (float x = f.West + Tile / 2f; x < f.East; x += Tile)
            if (Mathf.Abs(x - f.Center.x) > 0.5f)
                Panel("Window", cottage, new Vector3(x, 1.3f, f.South), 1f, 1f, Vector3.back, mats["Window"]);
        Panel("Window", cottage, new Vector3(f.West, 1.3f, f.Center.z), 1f, 1f, Vector3.left, mats["Window"]);

        // A stone chimney poking up through the thatch at the east end.
        var chimney = Block("Chimney", cottage, new Vector3(f.East - 1.2f, courses * BlockHeight + 1.6f, f.Center.z + 0.6f),
                            new Vector3(0.7f, 2f, 0.7f), mats["WallSide"]);
        AddCap(chimney, mats["WallTop"]);
    }

    // The cathedral: a lavender-stone nave (its gable facing the street) with a tall bell tower
    // in the middle of the front, a pointed spire with a gold star on top, a rose window over
    // the doors, and stained-glass lancet windows that glow a little.
    private static void BuildCathedral(Transform cathedral, Footprint f, SharedAssets assets)
    {
        var mats = assets.Materials;
        const int courses = 3, towerCourses = 7;
        Walls(cathedral, f, courses, mats["WallSide"]);
        GableRoof(cathedral, f, courses * BlockHeight, 3.6f, mats["Roof"], mats["WallSide"], ridgeAlongZ: true);

        // The tower: the middle tile of the front row, carried on up.
        float towerX = f.Center.x, towerZ = f.South + Tile / 2f;
        for (int k = courses; k < towerCourses; k++)
        {
            var block = Block("Tower", cathedral, new Vector3(towerX, BlockHeight * (k + 0.5f), towerZ),
                              new Vector3(Tile, BlockHeight, Tile), mats["WallSide"]);
            if (k == towerCourses - 1) AddCap(block, mats["WallTop"]);
        }
        float top = towerCourses * BlockHeight;
        Roof(cathedral, PyramidMesh(), new Vector3(towerX, top, towerZ), Tile + 0.4f, Tile + 0.4f, 4.2f, mats);
        var star = Block("Star", cathedral, new Vector3(towerX, top + 4.45f, towerZ), Vector3.one * 0.4f, mats["Gold"]);
        star.transform.rotation = Quaternion.Euler(0f, 45f, 45f);
        Object.DestroyImmediate(star.GetComponent<BoxCollider>());
        foreach (var (outward, center) in new[]
                 {
                     (Vector3.back, new Vector3(towerX, top - 0.8f, f.South)),
                     (Vector3.left, new Vector3(towerX - Tile / 2f, top - 0.8f, towerZ)),
                 })
            Panel("Belfry", cathedral, center, 0.7f, 1.1f, outward, mats["Belfry"]);

        FrontDoor(cathedral, new Vector3(towerX, 1f, f.South), mats["TownDoor"], "Peek inside the cathedral",
            "The organ is playing a gentle tune. | Sunlight pours through the stained glass and makes rainbows on the floor! | " +
            "A sign says: \"Choir practice on Sundays. Frogs welcome.\" | Dong! Dong! The bell rings out over Hollyhock.",
            "bell", width: 1.4f);
        Panel("RoseWindow", cathedral, new Vector3(towerX, 4.6f, f.South), 1.5f, 1.5f, Vector3.back, mats["RoseWindow"]);

        // Lancets: on the front either side of the tower, and all along the west side.
        for (float x = f.West + Tile / 2f; x < f.East; x += Tile)
            if (Mathf.Abs(x - towerX) > 0.5f)
                Panel("Lancet", cathedral, new Vector3(x, 1.9f, f.South), 0.75f, 2f, Vector3.back, mats["Lancet"]);
        for (float z = f.South + Tile / 2f; z < f.North; z += Tile)
            Panel("Lancet", cathedral, new Vector3(f.West, 1.9f, z), 0.75f, 2f, Vector3.left, mats["Lancet"]);
    }

    // A gable roof sitting on walls `wallTop` high: two slopes meeting at a ridge `height` above,
    // running east-west (or north-south, so the gable faces the street), reaching a little past
    // the walls, with triangular gable ends in the wall's material.
    private static void GableRoof(Transform parent, Footprint f, float wallTop, float height, Material roof, Material wall,
                                  bool ridgeAlongZ)
    {
        float along = ridgeAlongZ ? f.Depth : f.Width, across = ridgeAlongZ ? f.Width : f.Depth;
        var go = new GameObject("Roof");
        go.transform.SetParent(parent);
        go.transform.position = f.Center + Vector3.up * wallTop;
        if (ridgeAlongZ) go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = GableMesh(along, across, height, 0.35f);
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { roof, wall };
        go.isStatic = true;
    }

    // The gable roof's mesh, ridge along local x: `length` along the ridge, `width` across it at
    // the walls. Submesh 0 is the two slopes (they carry on `overhang` past the walls, so the eaves
    // hang over them), submesh 1 the two gable triangles. UVs are in metres / 2 (and / 1.2 up a
    // gable, like a course of wall), so the 16-texels-a-metre art is never stretched, whatever
    // the size. Saved as an asset per size, like the castle's pyramid.
    private static Mesh GableMesh(float length, float width, float height, float overhang)
    {
        string path = $"Assets/Meshes/Gable_{length}x{width}x{height}.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(path) };
        else mesh.Clear();

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var slopes = new List<int>();
        var gables = new List<int>();
        float half = width / 2f, ends = length / 2f + overhang;
        float drop = height * overhang / half; // how far the slope carries on below the wall top

        foreach (float side in new[] { -1f, 1f })
        {
            var eaveA = new Vector3(-ends, -drop, side * (half + overhang));
            var eaveB = new Vector3(ends, -drop, side * (half + overhang));
            var ridgeB = new Vector3(ends, height, 0f);
            var ridgeA = new Vector3(-ends, height, 0f);
            float slant = Vector3.Distance(eaveA, ridgeA);
            var outward = new Vector3(0f, half, side * height); // up and out from this slope
            AddQuad(vertices, uvs, slopes, outward,
                    (eaveA, new Vector2(eaveA.x / 2f, 0f)), (eaveB, new Vector2(eaveB.x / 2f, 0f)),
                    (ridgeB, new Vector2(ridgeB.x / 2f, slant / 2f)), (ridgeA, new Vector2(ridgeA.x / 2f, slant / 2f)));
        }
        foreach (float end in new[] { -1f, 1f })
        {
            var a = new Vector3(end * length / 2f, 0f, -half);
            var b = new Vector3(end * length / 2f, 0f, half);
            var c = new Vector3(end * length / 2f, height, 0f);
            AddTriangle(vertices, uvs, gables, new Vector3(end, 0f, 0f),
                        (a, new Vector2(a.z / 2f, 0f)), (b, new Vector2(b.z / 2f, 0f)), (c, new Vector2(0f, height / 1.2f)));
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(slopes, 0);
        mesh.SetTriangles(gables, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (isNew)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Meshes")) AssetDatabase.CreateFolder("Assets", "Meshes");
            AssetDatabase.CreateAsset(mesh, path);
        }
        else EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static void AddQuad(List<Vector3> vertices, List<Vector2> uvs, List<int> tris, Vector3 outward,
                                (Vector3, Vector2) a, (Vector3, Vector2) b, (Vector3, Vector2) c, (Vector3, Vector2) d)
    {
        AddTriangle(vertices, uvs, tris, outward, a, b, c);
        AddTriangle(vertices, uvs, tris, outward, a, c, d);
    }

    // Unity draws the side of a triangle its corners go clockwise around, which is the side
    // Cross(b - a, c - a) points to. Swap two corners if that's not the outside.
    private static void AddTriangle(List<Vector3> vertices, List<Vector2> uvs, List<int> tris, Vector3 outward,
                                    (Vector3 pos, Vector2 uv) a, (Vector3 pos, Vector2 uv) b, (Vector3 pos, Vector2 uv) c)
    {
        if (Vector3.Dot(Vector3.Cross(b.pos - a.pos, c.pos - a.pos), outward) < 0f) (b, c) = (c, b);
        foreach (var (pos, uv) in new[] { a, b, c })
        {
            tris.Add(vertices.Count);
            vertices.Add(pos);
            uvs.Add(uv);
        }
    }
}
