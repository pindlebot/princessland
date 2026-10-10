using UnityEditor;
using UnityEngine;

// Coralie the mermaid ('m', in Level 0's pond), Sir Hopsalot the frog (hidden in the 'f'
// bush), the wishing fountain, Bonesy the friendly skeleton ('n', by the dungeon campfire)
// and the dungeon's extra props: campfire, barrels, crates, bones, mushrooms, doors, the key.
// Art comes from Tools/make_friends_sprites.py.
public static partial class DungeonBuilder
{
    private static Talk[] MermaidTalks() => new[]
    {
        // Finding her frog (Level 0's bush) gets a thank-you present, even before you've met.
        new Talk
        {
            Requires = FrogBush.FoundFlag, NotIf = "thanked:frog", Sets = "thanked:frog,met:Coralie", Gold = 20,
            Lines = new[]
            {
                H("Excuse me, Miss Mermaid! Is this little frog yours? He was hiding in a bush."),
                N("Sir Hopsalot! You found him! He's my very best friend, and he ALWAYS hides too well."),
                N("I'm Coralie. Thank you so much, {hero}! Please take these sea-coins. They're extra shiny."),
            },
        },
        new Talk
        {
            NotIf = "met:Coralie", Sets = "met:Coralie",
            Lines = new[]
            {
                N("Splish splash! Oh, hello there! I'm Coralie. I swam all the way up the river to visit this pond."),
                H("A real mermaid! Do you live here?"),
                N("Only for the summer. The lily pads are ever so comfy."),
                N("Say, {hero}... have you seen my friend Sir Hopsalot? He's a little green frog with a tiny crown."),
                N("He LOVES hiding in bushes. If you find him, will you tell me?"),
                H("I'll keep my eyes open!"),
                N("Oh, and those stairs by the fountain? They stay shut until every skeleton and slime in the grounds is beaten. Zap them with your magic!"),
            },
        },
        new Talk
        {
            NotIf = FrogBush.FoundFlag,
            Lines = new[] { N("Still no sign of Sir Hopsalot? Try rustling the bushes. He likes the ones in the corners.") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Did you know? If you toss coins into the fountain and make a wish, sometimes wishes come true!") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Mermaid tip: always brush your hair with a fork. Wait... that's not right, is it? Hee hee!") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Sir Hopsalot says ribbit. That means 'thank you' in Frog."), H("Ribbit!"), N("Oh! You speak Frog too!") },
        },
        new Talk
        {
            SmallTalk = true, Requires = WishingFountain.GrantedFlag,
            Lines = new[] { N("A wish came true at the fountain! The water sparkled all the way into my pond!") },
        },
        new Talk
        {
            SmallTalk = true, NotIf = "met:Pearl",
            Lines = new[]
            {
                N("My big sister Pearl lives in Mermaid Cove, down the river. I haven't heard from her in ages..."),
                N("The rowboat at the corner of my pond goes all the way there. Would you check on her, {hero}?"),
            },
        },
        new Talk
        {
            SmallTalk = true, Requires = "thanked:cove",
            Lines = new[] { N("Pearl told me everything! You broke the sea-spell AND scared off the pirates. You're my hero, {hero}!") },
        },
    };

    private static Talk[] BonesyTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Bonesy", Sets = "met:Bonesy",
            Lines = new[]
            {
                N("Eek! Please don't zap me! I'm not like the others, honest!"),
                H("You're... a friendly skeleton?"),
                N("I'm Bonesy. I don't like fighting. I just like campfires, and humming, and my scarf."),
                N("Warm up by the fire whenever you like. It makes you feel all better."),
                N("Oh, and somebody dropped a rusty key down in the twisty tunnels below. I wonder what it opens..."),
            },
        },
        new Talk
        {
            Requires = DungeonDoor.KeyFlag, NotIf = "bonesy:key",
            Sets = "bonesy:key",
            Lines = new[] { N("You found the rusty key! I think it opens the big door at the end of the long hall, past the barrels.") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Why didn't the skeleton cross the road?"), H("Why?"), N("He didn't have the GUTS! Hee hee hee!") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Rattle rattle. Sorry, that's just my knees.") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("The Slime King lives past the great big puddle. He's very wobbly, and very grumpy.") },
        },
    };

    // The tenth flush brings out a frog (HouseFixture.TrySurprise): hook the toilet up to him.
    private static void AttachToiletFrog(SharedAssets assets)
    {
        string path = AssetDatabase.GetAssetPath(assets.Toilet);
        var root = PrefabUtility.LoadPrefabContents(path);
        SetRef(root.GetComponent<HouseFixture>(), "surprisePrefab", assets.Frog);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // Everything here is built after the scenery (it changes the fountain prefab).
    private static void CreateFriendsAndDungeonProps(SharedAssets assets, Sprite shadow, GameObject sparkle)
    {
        assets.Mermaid = CreateNpcPrefab("Mermaid", "Mermaid", "Coralie", "Assets/Art/UI/PortraitMermaid.png",
                                         "voice_mermaid", MermaidTalks(), null, null, 0f);
        assets.Bonesy = CreateNpcPrefab("Bonesy", "Bonesy", "Bonesy", "Assets/Art/UI/PortraitBonesy.png",
                                        "voice_bonesy", BonesyTalks(), new Vector3(0.8f, 2f, 0.8f), shadow, 1.4f);
        assets.Frog = CreateFrogPrefab(shadow);
        assets.FrogBush = CreateFrogBushPrefab(assets);
        AttachToiletFrog(assets);
        AddWishing(assets.Fountain, sparkle);

        var props = SpriteSheetImporter.Import("DungeonProps");
        var water = SpriteSheetImporter.Import("WaterDecals");
        assets.Campfire = CreateCampfirePrefab(props);
        assets.Barrel = CreateSolidProp("Barrel", props, "Barrel", shadow);
        assets.Crate = CreateSolidProp("Crate", props, "Crate", shadow);
        assets.Bones = CreateDecorProp("Bones", props, "Bones", null);
        assets.Mushrooms = CreateDecorProp("Mushrooms", props, "Mushrooms", new Color(0.4f, 1f, 0.95f));
        assets.Door = CreateDoorPrefab(props, locked: false);
        assets.LockedDoor = CreateDoorPrefab(props, locked: true);
        assets.Key = CreateKeyPrefab(props, shadow);
        assets.Ripple = CreateFlatDecal("Ripple", water, "Ripple", 1f);
        assets.Lily = CreateFlatDecal("Lily", water, "Lily", 1f);
    }

    // ---------- Level 0 easter eggs ----------

    private static GameObject CreateFrogPrefab(Sprite shadow)
    {
        var sheet = SpriteSheetImporter.Import("Frog");
        var go = new GameObject("SirHopsalot");
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.AddComponent<SpriteRenderer>();
        var flipbook = sprite.AddComponent<SpriteFlipbook>();
        SetFlipbook(flipbook, sheet, "Idle", destroyWhenDone: false);
        sprite.AddComponent<Billboard>();
        AddShadow(go, shadow, 0.9f);

        var frog = go.AddComponent<Frog>();
        SetRef(frog, "flipbook", flipbook);
        SetRefs(frog, "idleFrames", sheet.Frames("Idle"));
        SetRefs(frog, "hopFrames", sheet.Frames("Hop"));
        SetRef(frog, "ribbitSound", Sound("ribbit"));
        return SavePrefab(go, "SirHopsalot");
    }

    // An ordinary-looking bush (a copy of the Bush prefab) with the frog inside.
    private static GameObject CreateFrogBushPrefab(SharedAssets assets)
    {
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(assets.Bush));
        var bush = root.AddComponent<FrogBush>();
        SetRef(bush, "frogPrefab", assets.Frog);
        SetRef(bush, "rustleSound", Sound("poof"));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/FrogBush.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        return prefab;
    }

    private static void AddWishing(GameObject fountainPrefab, GameObject sparkle, string thing = "fountain",
                                   string counter = "wishes", string grantedFlag = WishingFountain.GrantedFlag)
    {
        string path = AssetDatabase.GetAssetPath(fountainPrefab);
        var root = PrefabUtility.LoadPrefabContents(path);
        var wishing = root.AddComponent<WishingFountain>();
        SetString(wishing, "thing", thing);
        SetString(wishing, "counter", counter);
        SetString(wishing, "grantedFlag", grantedFlag);
        SetRef(wishing, "plinkSound", Sound("plink"));
        SetRef(wishing, "wishSound", Sound("wish"));
        SetRef(wishing, "sparkle", sparkle);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---------- Dungeon props ----------

    // Bonesy's campfire: flickering light, and pressing E warms you up (full hearts and magic).
    private static GameObject CreateCampfirePrefab(SpriteSheetImporter.SpriteSheet props)
    {
        var go = new GameObject("Campfire");
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.4f, 0f);
        box.size = new Vector3(1.2f, 0.8f, 1.2f);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, props, "Campfire");

        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.25f);
        light.range = 9f;
        light.shadows = LightShadows.None;
        var flicker = light.gameObject.AddComponent<FlickerLight>();
        SetFloat(flicker, "baseIntensity", 2f);

        var fire = go.AddComponent<HouseFixture>();
        SetString(fire, "prompt", "Warm up by the fire");
        SetString(fire, "message", "Ahh, toasty! You feel all better.");
        SetInt(fire, "effect", (int)HouseFixture.Effect.Rest);
        SetRef(fire, "sound", Sound("rest"));
        return SavePrefab(go, "Campfire");
    }

    // Barrels and crates: you bump into them (and spells hit them).
    private static GameObject CreateSolidProp(string name, SpriteSheetImporter.SpriteSheet props, string anim, Sprite shadow)
    {
        var go = new GameObject(name);
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.8f, 0f);
        box.size = new Vector3(1.2f, 1.6f, 1.2f);
        AddStaticSprite(go, props.Frames(anim)[0]);
        AddShadow(go, shadow, 1.8f);
        return SavePrefab(go, name);
    }

    // Bones and mushrooms: just to look at (the mushrooms glow a little).
    private static GameObject CreateDecorProp(string name, SpriteSheetImporter.SpriteSheet props, string anim, Color? glow)
    {
        var go = new GameObject(name);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, props, anim);
        if (glow.HasValue)
        {
            var light = new GameObject("Glow").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            light.type = LightType.Point;
            light.color = glow.Value;
            light.range = 3.5f;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;
        }
        return SavePrefab(go, name);
    }

    private static GameObject CreateDoorPrefab(SpriteSheetImporter.SpriteSheet props, bool locked)
    {
        string name = locked ? "LockedDoor" : "DungeonDoor";
        var go = new GameObject(name);
        var box = go.AddComponent<BoxCollider>(); // fills the doorway until it's opened
        box.center = new Vector3(0f, 1.2f, 0f);
        box.size = new Vector3(Tile, 2.4f, Tile);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        var sr = sprite.AddComponent<SpriteRenderer>();
        sr.sprite = props.Frames(locked ? "Door_Locked" : "Door_Closed")[0];
        sprite.AddComponent<Billboard>();

        var door = go.AddComponent<DungeonDoor>();
        SetBool(door, "locked", locked);
        SetRef(door, "sprite", sr);
        SetRef(door, "openSprite", props.Frames("Door_Open")[0]);
        SetRef(door, "blocker", box);
        SetRef(door, "openSound", Sound("door_open"));
        SetRef(door, "lockedSound", Sound("door_locked"));
        return SavePrefab(go, name);
    }

    private static GameObject CreateKeyPrefab(SpriteSheetImporter.SpriteSheet props, Sprite shadow)
    {
        var go = new GameObject("RustyKey");
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, props, "Key");
        AddShadow(go, shadow, 1f);
        var light = new GameObject("Glint").AddComponent<Light>(); // so it can be spotted in the dark
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0f, 1f, 0f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.4f);
        light.range = 3f;
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;
        SetRef(go.AddComponent<KeyPickup>(), "pickupSound", Sound("pickup"));
        return SavePrefab(go, "RustyKey");
    }

    // Ripples and lily pads lie flat on the water, like the blob shadows.
    private static GameObject CreateFlatDecal(string name, SpriteSheetImporter.SpriteSheet sheet, string anim, float size)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        go.transform.localScale = Vector3.one * size;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var flipbook = go.AddComponent<SpriteFlipbook>();
        SetFlipbook(flipbook, sheet, anim, destroyWhenDone: false);
        SetBool(flipbook, "randomStart", true);
        sr.sortingOrder = -1;
        return SavePrefab(go, name);
    }
}
