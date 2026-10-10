using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only tool (anything under an "Editor" folder is stripped from builds).
// Generates every material, prefab and scene in the game from the ASCII maps and the
// art in Assets/Art. Run it from the menu: Dungeon > Rebuild All Scenes.
//
// It's a "partial" class split across several files by topic:
//   DungeonBuilder.cs                  this file: entry point + shared helpers
//   DungeonBuilder.Characters.cs       players, enemies, spells
//   DungeonBuilder.Props.cs            chest, torch, grass, item pickup, flag
//   DungeonBuilder.Levels.cs           how a level scene is assembled from Assets/Levels/<Scene>.txt
//   DungeonBuilder.Castle.cs           the decorative castle in Level 0
//   DungeonBuilder.Dragon.cs           the NPC recipe, Amethyra the dragon and her dialogue
//   DungeonBuilder.Friends.cs          Coralie, Bonesy, the frog, the wishing fountain, dungeon props
//   DungeonBuilder.Home.cs             the furniture and front door of the hero's home
//   DungeonBuilder.Hazards.cs          lava and spike traps
//   DungeonBuilder.Cove.cs             Mermaid Cove: pirates, dark mermaids, Pearl, waterfalls, the rowboat
//   DungeonBuilder.Town.cs             Hollyhock, the village: buildings, the picket fence, Barnaby, chickens
//   DungeonBuilder.Farm.cs             Hollow Farm: pumpkins, scarecrows, the barn, ghosts, Old Stitches
//   DungeonBuilder.Monsters.cs         the farm's Gourdlings, Strawmen and Pumpkin King, and Captain Grumblebeard
//   DungeonBuilder.Woods.cs            Whispering Woods: Spore Puffs, Mother Mushroom, Old Moss, sleepy trees, pots
//   DungeonBuilder.CharacterSelect.cs  the character select screen
//   DungeonBuilder.Title.cs            the title screen and its save slots
public static partial class DungeonBuilder
{
    private const float Tile = 2f;
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string CharacterSelectScenePath = "Assets/Scenes/CharacterSelect.unity";

    // Everything the scenes are assembled from, created once per rebuild.
    private class SharedAssets
    {
        public Dictionary<string, Material> Materials;
        public GameObject Skeleton, Slime, SlimeKing, Chest, Torch, Flag, Dragon, Sparkle;
        public Dictionary<string, GameObject> ItemPickups; // by item id (DungeonBuilder.Items.cs)
        public GameObject Bed, Toilet, Sink, PaperTowel, HouseDoor, SpiralDown, SpiralUp;
        public GameObject Wardrobe, Nightstand, Bookshelf, ToyChest, Plant, Rug;
        public GameObject Tree, Fountain, Bush, Butterfly, Mote, Cloud, Stairs;
        public GameObject Pine, Birch, AutumnTree, Boulder, Stones, Stump, Log, Tent, Fern;
        public GameObject Mermaid, Bonesy, Frog, FrogBush;
        public GameObject Campfire, Barrel, Crate, Bones, Mushrooms, Door, LockedDoor, Key, Ripple, Lily;
        public GameObject SpikeTrap, LavaBubble, Ember;
        public GameObject[] PlagueCrystals; // small, cluster, spire (DungeonBuilder.Gates.cs)
        public GameObject Bat, Pebblin, CrystalGolem, Digby, Molly, Mortimer, Mo, HintMitts; // DungeonBuilder.Mines.cs
        public GameObject Crab, Jelly, KingCrabbington, Clamshell, HintCharm; // DungeonBuilder.Lake.cs
        public GameObject IceSlime, SnowImp, SnowYeti, MrFrost, Purl, HintChalk; // DungeonBuilder.Frost.cs
        public GameObject Gourdling, Strawman, PumpkinKing, PirateCaptain; // DungeonBuilder.Monsters.cs
        public GameObject Pirate, DarkMermaid, Pearl, Palm, Treasure, Rowboat, Ship, Splash, Shell, Starfish, Foam;
        public GameObject Barnaby, Stitches, FarmGate, Pippin;
        public GameObject SporePuff, MotherMushroom, OldMoss; // DungeonBuilder.Woods.cs
        public GameObject HintBoots, HintLantern, RoomEdge;             // DungeonBuilder.Gates.cs
        // Legend "prop" entries, by kind (MapFile.PropKinds): the village's, and the home's bathtub and cat.
        public Dictionary<string, GameObject> PropPrefabs = new Dictionary<string, GameObject>();
        public SpriteSheetImporter.SpriteSheet Props;
        public GameObject[] Grass;

        // Opening a new scene makes Unity unload assets that no open scene uses, which
        // includes freshly made ScriptableObjects like these. A C# reference to an unloaded
        // asset reads as null, so we keep the *paths* and load them fresh each time.
        public string WizardPath, PrincessPath;
        public CharacterDefinition Wizard => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(WizardPath);
        public CharacterDefinition Princess => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(PrincessPath);
    }

    [MenuItem("Dungeon/Rebuild All Scenes")]
    public static void BuildAll()
    {
        var maps = LoadMaps(); // first, so a mistake in a level file stops the build before anything changes
        var assets = CreateAssets();
        CreateWorldMap(maps); // the world map screen's rooms (read by each scene's HUD)
        AssetDatabase.SaveAssets(); // write everything to disk before scenes start changing

        var specs = maps.Select(SpecFor).ToList();
        foreach (var spec in specs) BuildLevel(spec, assets);
        BuildCharacterSelect(assets);
        BuildTitle(assets);

        // The order here is the order scenes have in a build: index 0 is what the game starts with.
        EditorBuildSettings.scenes = new[] { TitleScenePath, CharacterSelectScenePath }
            .Concat(specs.Select(s => s.ScenePath))
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(TitleScenePath);
        Debug.Log($"[DungeonBuilder] Built Title, CharacterSelect, {string.Join(", ", maps.Select(m => m.Name))}");
    }

    private static SharedAssets CreateAssets()
    {
        shadowSheet = SpriteSheetImporter.Import("Shadows");
        var props = SpriteSheetImporter.Import("Props");
        var furniture = SpriteSheetImporter.Import("Furniture");
        var skeleton = CharacterSpriteBuilder.Build("Skeleton");
        var slime = CharacterSpriteBuilder.Build("Slime");
        var slimeKing = CharacterSpriteBuilder.Build("SlimeKing");
        var wizardArt = CharacterSpriteBuilder.Build("Wizard");
        var princessArt = CharacterSpriteBuilder.Build("Princess");
        var sparkle = CreateSparklePrefab(props);
        var coin = CreateCoinPrefab(props);

        var slimePrefab = CreateEnemyPrefab(slime, SlimeStats, coin, sparkle);
        var items = SpriteSheetImporter.Import("Items");
        var woodsItems = SpriteSheetImporter.Import("WoodsItems");
        var gateItems = SpriteSheetImporter.Import("GateItems");
        var itemDefinitions = ItemSpecs.Select(CreateItem).ToList();
        var itemDatabase = CreateItemDatabase(itemDefinitions.ToArray());
        CreateQuestPictures();
        var assets = new SharedAssets
        {
            Sparkle = sparkle,
            Materials = CreateMaterials(),
            Skeleton = CreateEnemyPrefab(skeleton, SkeletonStats, coin, sparkle),
            Slime = slimePrefab,
            SlimeKing = CreateSlimeKingPrefab(slimeKing, coin, slimePrefab, sparkle),
            Chest = CreateChestPrefab(props, wizardArt.Shadow, sparkle),
            Torch = CreateTorchPrefab(props),
            ItemPickups = itemDefinitions.Zip(ItemSpecs, (item, spec) => (item, spec))
                .ToDictionary(p => p.item.Id, p => CreateItemPickupPrefab(p.spec.FloorSheet == "Props" ? props : p.spec.FloorSheet == "WoodsItems" ? woodsItems : p.spec.FloorSheet == "GateItems" ? gateItems : items, p.spec, p.item, wizardArt.Shadow)),
            Flag = CreateFlagPrefab(props),
            Dragon = CreateDragonPrefab(wizardArt.Shadow),
            Grass = new[] { "Grass_A", "Grass_B", "Grass_C" }.Select(g => CreateGrassPrefab(props, g)).ToArray(),
            WizardPath = AssetDatabase.GetAssetPath(CreateWizard(wizardArt, sparkle, itemDatabase)),
            PrincessPath = AssetDatabase.GetAssetPath(CreatePrincess(princessArt, sparkle, itemDatabase)),
        };
        CreateHomePrefabs(assets, furniture, wizardArt.Shadow);
        CreateSceneryPrefabs(assets, props, wizardArt.Shadow);
        CreateFriendsAndDungeonProps(assets, wizardArt.Shadow, sparkle);
        CreateHazardPrefabs(assets, props);
        CreateCovePrefabs(assets, coin, sparkle, wizardArt.Shadow);
        CreateTownPrefabs(assets, wizardArt.Shadow, sparkle);
        CreateFarmPrefabs(assets, wizardArt.Shadow);
        CreateMonsterPrefabs(assets, coin, sparkle);
        CreateWoodsPrefabs(assets, wizardArt.Shadow, coin, sparkle);
        CreateGatePrefabs(assets, wizardArt.Shadow, sparkle);
        CreateMinesPrefabs(assets, wizardArt.Shadow, coin, sparkle);
        CreateLakePrefabs(assets, wizardArt.Shadow, coin, sparkle);
        CreateFrostPrefabs(assets, wizardArt.Shadow, coin, sparkle);
        assets.Props = props;
        return assets;
    }

    // ---------- Building blocks ----------

    private static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.isStatic = true;
        return go;
    }

    // A cube primitive uses one material on every face, so a block's top gets its own
    // quad (e.g. the cap stone texture) sitting just above the cube's top face.
    private static void AddCap(GameObject block, Material mat)
    {
        var cap = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cap.name = "Cap";
        Object.DestroyImmediate(cap.GetComponent<MeshCollider>());
        cap.transform.SetParent(block.transform, false);
        cap.transform.localPosition = new Vector3(0f, 0.501f, 0f); // tiny gap avoids z-fighting
        cap.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face up
        cap.GetComponent<Renderer>().sharedMaterial = mat;
        cap.isStatic = true;
    }

    // Places a prefab instance (still linked to its prefab) in the scene being built.
    private static GameObject Place(GameObject prefab, Transform parent, Vector3 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        return go;
    }

    private static T SavePrefab<T>(GameObject go, string name) where T : Component =>
        SavePrefab(go, name).GetComponent<T>();

    private static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // A sound from Assets/Audio (made by Tools/make_sounds.py).
    private static AudioClip Sound(string name) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{name}.wav");

    // Each scene gets its own AudioManager playing that scene's music loop.
    private static void AddAudio(string music)
    {
        var audio = new GameObject("Audio").AddComponent<AudioManager>();
        SetRef(audio, "music", Sound(music));
    }

    // Loads (or creates) a ScriptableObject asset at a path, e.g. an item or character.
    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    // ---------- Materials ----------

    private static Dictionary<string, Material> CreateMaterials()
    {
        var mats = new Dictionary<string, Material>();
        // Textures come from Tools/make_environment_textures.py.
        foreach (var name in new[]
                 {
                     "Floor_0", "Floor_1", "Floor_2", "WallSide", "WallTop",
                     "Grass_0", "Grass_1", "Grass_2", "Path", "Path_1", "Bank", "HedgeSide", "HedgeTop", "Roof", "Gate", "EarthSide",
                     "WoodFloor", "BathTile", "Water", "Puddle", "SpikePlate", "RockSide", "RockSideLow", "RockTop", "CaveFloor",
                     "Sand_0", "Sand_1", "Sea", "SandBank", "Planks", "Waterfall",
                     "Cobble_0", "Cobble_1", "Plaster", "RoofTiles", "Thatch", "Window", "TownDoor", "Awning", "ShopSign",
                     "WallSideIvy", "KitchenTile", "Soil", "BarnSide", "BarnDoor", "BarnRoof", "CornSide", "CornTop", "Pit",
                     "Snow_0", "Snow_1", "Ice", "SnowSide", "SnowTop", "SnowBank",
                 })
            mats[name] = Mat(name, Color.white, texture: PixelTexture(name));
        // The cathedral's stained glass glows a little, as if lit from inside.
        foreach (var name in new[] { "Lancet", "RoseWindow" })
        {
            var glass = PixelTexture(name);
            mats[name] = Mat(name, Color.white, new Color(0.45f, 0.45f, 0.45f), glass, emissionMap: glass);
        }
        // A rainbow bridge glows: the texture is also its emission map.
        var rainbow = PixelTexture("RainbowBridge");
        mats["RainbowBridge"] = Mat("RainbowBridge", Color.white, new Color(0.9f, 0.9f, 0.9f), rainbow, emissionMap: rainbow);
        mats["Picket"] = Mat("Picket", new Color(0.97f, 0.94f, 0.88f)); // the village's white picket fence
        mats["Gold"] = Mat("Gold", new Color(1f, 0.8f, 0.35f), new Color(0.5f, 0.35f, 0.1f)); // the star on the spire
        mats["Belfry"] = Mat("Belfry", new Color(0.2f, 0.11f, 0.21f)); // the dark openings at the top of the bell tower
        mats["Exit"] = Mat("Exit", new Color(0.2f, 0.9f, 0.3f), new Color(0.2f, 1.2f, 0.3f));
        // Lava lights itself: the texture is also its emission map, so the cracks glow and the crust stays dark.
        var lava = PixelTexture("Lava");
        mats["Lava"] = Mat("Lava", Color.white, new Color(1.3f, 1.1f, 1f), lava, emissionMap: lava);
        // The inside walls of a gap's pit: the dungeon's bricks or the island's earth, in shadow.
        mats["PitWallStone"] = Mat("PitWallStone", new Color(0.55f, 0.5f, 0.66f), texture: PixelTexture("WallSide"));
        mats["PitWallEarth"] = Mat("PitWallEarth", new Color(0.62f, 0.52f, 0.5f), texture: PixelTexture("EarthSide"));
        mats["PitWallEarth"].mainTextureScale = new Vector2(1f, 0.25f); // EarthSide is 4m tall; show its top metre
        EditorUtility.SetDirty(mats["PitWallEarth"]);
        var spike = Mat("Spike", new Color(0.78f, 0.78f, 0.86f)); // shiny steel
        spike.SetFloat("_Metallic", 0.6f);
        spike.SetFloat("_Glossiness", 0.6f);
        mats["Spike"] = spike;
        return mats;
    }

    private static Material Mat(string name, Color color, Color? emission = null, Texture2D texture = null,
                                Texture2D emissionMap = null)
    {
        string path = $"Assets/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color; // multiplied with the texture, so white = texture as drawn
        mat.mainTexture = texture;
        mat.SetFloat("_Glossiness", 0.15f);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.SetTexture("_EmissionMap", emissionMap); // null = glow evenly
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Same pixel-art import settings as the sprites, but as a regular texture
    // (for materials and USS backgrounds) rather than a Sprite.
    private static Texture2D PixelTexture(string name, string folder = "Environment")
    {
        string path = $"Assets/Art/{folder}/{name}.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;         // crisp pixels, no blur
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = false;                 // the camera never zooms out far enough to need them
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // For art drawn smooth at a higher resolution than it's shown (the HUD's hearts, panel and
    // icons): smooth filtering and mipmaps, so it shrinks cleanly to any screen size.
    private static Texture2D SmoothTexture(string name, string folder = "UI")
    {
        string path = $"Assets/Art/{folder}/{name}.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.alphaIsTransparency = true;            // no dark fringes around the shapes
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------- Helpers for [SerializeField] private fields ----------
    // SerializedObject is how the Inspector edits fields; using it here
    // means the values are saved into the scene/prefab exactly like manual edits.

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefs(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var list = so.FindProperty(field);
        list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Fills a SpriteFlipbook from one animation row of a sprite sheet.
    private static void SetFlipbook(SpriteFlipbook flipbook, SpriteSheetImporter.SpriteSheet sheet, string anim, bool destroyWhenDone)
    {
        var info = sheet.Anim(anim);
        var frames = sheet.Frames(anim);
        SetRefs(flipbook, "frames", frames);
        var so = new SerializedObject(flipbook);
        so.FindProperty("fps").floatValue = info.fps;
        so.FindProperty("loop").boolValue = info.loop;
        so.FindProperty("destroyWhenDone").boolValue = destroyWhenDone;
        so.ApplyModifiedPropertiesWithoutUndo();
        flipbook.GetComponent<SpriteRenderer>().sprite = frames[0]; // so it shows in the editor too
    }

    private static void SetStrings(Object target, string field, string[] values)
    {
        var so = new SerializedObject(target);
        var list = so.FindProperty(field);
        list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            list.GetArrayElementAtIndex(i).stringValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector3(Object target, string field, Vector3 value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).vector3Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetColor(Object target, string field, Color value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
